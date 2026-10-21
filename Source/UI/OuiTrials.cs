using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiTrials : ListScreen
{
  public const Overworld.StartMode ReturnStartMode = (Overworld.StartMode)5602393;
  public static Trial ReturnTrial;

  public static LeaderboardView.Bookmark? ReturnBoard;

  private const float NameY = TitleY + 70f;
  private const float NameScale = 0.8f;
  private const float ArrowGap = 50f;
  private const float TotalY = TitleY + 115f;
  private const float TotalScale = 0.6f;
  private const float TotalLabelGap = 16f;
  private const float OfflineY = TotalY + 34f;
  private const float OfflineScale = 0.45f;
  private const float TrialsListTop = 280f;
  private const float NoticeSeconds = 5f;

  private int campaignIndex;
  private Campaign outgoingCampaign;
  private TrialsMenu outgoing;
  private Coroutine switching;
  private TrialModeMenu open;
  private LeaderboardView view;
  private readonly List<TrialRow> rows = [];
  private LeaderboardView.Bookmark? resumeBoard;

  protected override int ScreenDepth => 3;

  public Campaign Campaign
  {
    get => Campaigns.All[campaignIndex];
    set => campaignIndex = Campaigns.IndexOf(value);
  }

  protected override TrialsMenu BuildMenu()
  {
    TrialsMenu built = new() { ListTop = TrialsListTop };
    rows.Clear();
    foreach (Trial trial in Campaign.Trials)
    {
      TrialRow row = new(trial);
      row.Pressed(() => Choose(trial));
      built.Add(row);
      RowDescription.Attach(built, row, TrialText.Description(trial.Run));
      rows.Add(row);
    }
    built.OnSwitchLeft = () => Switch(-1);
    built.OnSwitchRight = () => Switch(1);
    built.OnCancel = () =>
    {
      built.Focused = false;
      Audio.Play(SFX.ui_main_button_back);
      Overworld.Goto<OuiCampaigns>();
    };
    return built;
  }

  protected override int InitialSelection(Oui from)
  {
    Trial trial = ReturnTrial;
    ReturnTrial = null;
    resumeBoard = ReturnBoard;
    ReturnBoard = null;
    int index = rows.FindIndex(r => r.Trial == trial);
    return menu.Items.IndexOf(rows[index < 0 ? 0 : index]);
  }

  public override bool IsStart(Overworld overworld, Overworld.StartMode start)
  {
    if (start != ReturnStartMode || ReturnTrial == null)
      return false;
    Campaign = Campaigns.CampaignOf(ReturnTrial);
    Add(new Coroutine(Enter(null)));
    return true;
  }

  private void Switch(int direction)
  {
    if (outgoing != null)
      return;

    int count = Campaigns.All.Count;
    if (count < 2)
      return;

    outgoingCampaign = Campaign;
    campaignIndex = (campaignIndex + direction + count) % count;
    Standings.EnsureFresh(Campaign);
    Audio.Play(direction > 0 ? SFX.ui_main_button_toggle_on : SFX.ui_main_button_toggle_off);
    outgoing = menu;
    outgoing.Focused = false;
    menu = BuildMenu();
    menu.Selection = menu.Items.IndexOf(rows[0]);
    menu.X = OnScreenX + direction * Motion.SlideDistance;
    menu.Alpha = 0f;
    Scene.Add(menu);
    menu.Current?.OnEnter?.Invoke();
    menu.Focused = true;
    switching = new Coroutine(SwitchRoutine(direction));
    Add(switching);
  }

  private IEnumerator SwitchRoutine(int direction)
  {
    yield return Motion.Slide(
      Motion.ScreenSlide,
      direction,
      Motion.SlideDistance,
      (outX, outAlpha, inX, inAlpha) =>
      {
        outgoing.X = OnScreenX + outX;
        outgoing.Alpha = outAlpha;
        menu.X = OnScreenX + inX;
        menu.Alpha = inAlpha;
      }
    );
    switching = null;
    FinishSwitch();
  }

  private void FinishSwitch()
  {
    if (outgoing == null)
      return;

    outgoing.RemoveSelf();
    outgoing = null;
    menu.X = OnScreenX;
    menu.Alpha = 1f;
    switching?.Cancel();
    switching?.RemoveSelf();
    switching = null;
  }

  protected override void CancelTransitions()
  {
    FinishSwitch();
    if (open?.HandedOver == true)
    {
      open.Release();
      open = null;
    }
    if (view == null)
      return;
    view.Discard();
    view = null;
    SetDim(1f);
  }

  protected override void FetchOnline()
  {
    Identity.EnsureRegistered();
    Standings.EnsureFresh(Campaign);
  }

  private void Choose(Trial trial)
  {
    Audio.Play(SFX.ui_main_button_select);
    OpenMenu(trial);
  }

  private void OpenMenu(Trial trial)
  {
    menu.Focused = false;
    open = new TrialModeMenu(
      trial,
      play: () => CloseThen(() => Start(trial)),
      watch: () => CloseThen(() => WatchPb(trial)),
      race: () => CloseThen(() => Race(trial)),
      submit: () => CloseThen(() => SubmitPb(trial)),
      board: () => HandOverToBoard(trial),
      closed: OnMenuClosed
    );
    Scene.Add(open);
  }

  private void CloseThen(Action action)
  {
    open.Dismiss();
    action();
  }

  private void OnMenuClosed()
  {
    open = null;
    SetDim(1f);
    Standings.EnsureFresh(Campaign);
    if (Focused)
      menu.Focused = true;
  }

  public override void Update()
  {
    base.Update();
    if (open != null)
      SetDim(open.DimUnder);
    if (
      BoardPromptShown
      && outgoing == null
      && menu.Focused
      && Input.MenuJournal.Pressed
      && menu.Current is TrialRow row
    )
    {
      Audio.Play(SFX.ui_main_button_select);
      OpenBoard(row.Trial);
    }
  }

  private bool BoardPromptShown => menu != null && open == null && view == null;

  private void HandOverToBoard(Trial trial)
  {
    float drawn = open.DrawnBackdrop;
    float covered = open.HandOver();
    OpenBoard(trial, coveredFrom: covered, backdropFrom: drawn);
  }

  private void OpenBoard(
    Trial trial,
    LeaderboardView.Bookmark? resume = null,
    float coveredFrom = 0f,
    float backdropFrom = 0f
  )
  {
    menu.Focused = false;
    Focused = false;
    view = new LeaderboardView(
      BoardSubject.Of(trial),
      SetBoardDim,
      OnBoardClosed,
      StartGhost,
      resume,
      coveredFrom: coveredFrom,
      backdropFrom: backdropFrom
    );
    Scene.Add(view);
  }

  private void SetBoardDim(float opened)
  {
    if (open?.HandedOver == true)
    {
      open.Release();
      open = null;
    }
    SetDim(Motion.DimUnder(opened, Motion.BoardUnderDim));
  }

  private void StartGhost(Trial trial, Replay replay, bool race)
  {
    view.Focused = false;
    ReturnBoard = view.Showing;
    Add(new Coroutine(StartRoutine(trial, replay, race, fromBoard: true)));
  }

  protected override bool Reopen()
  {
    if (
      resumeBoard is not LeaderboardView.Bookmark place
      || menu.Current is not TrialRow row
      || place.SubjectId != row.Trial.Id
    )
      return false;
    resumeBoard = null;
    OpenBoard(row.Trial, place);
    return true;
  }

  private void OnBoardClosed(BoardSubject shown)
  {
    view = null;
    int index = rows.FindIndex(r => r.Trial.Id == shown.Id);
    if (index >= 0 && menu.Current != rows[index])
    {
      menu.Current?.OnLeave?.Invoke();
      menu.Selection = menu.Items.IndexOf(rows[index]);
      menu.Current?.OnEnter?.Invoke();
    }
    Standings.EnsureFresh(Campaign);
    menu.Focused = true;
    Focused = true;
  }

  private void Start(Trial trial)
  {
    Add(new Coroutine(StartRoutine(trial)));
  }

  private void WatchPb(Trial trial)
  {
    if (TryLoadPb(trial, out Replay replay))
      Add(new Coroutine(StartRoutine(trial, replay)));
  }

  private void Race(Trial trial)
  {
    if (TryLoadPb(trial, out Replay replay))
      Add(new Coroutine(StartRoutine(trial, replay, race: true)));
  }

  private void SubmitPb(Trial trial)
  {
    TrialRecord record = TrialRecords.Get(trial.Id);
    bool bareTime =
      record != null && !ReplayStore.Exists(trial.Id) && !trial.FitsReplayLimit(record.BestTicks);
    if (bareTime)
      Uploader.Start(trial, null, ReportUpload);
    else if (TryLoadPb(trial, out Replay replay))
      Uploader.Start(trial, replay, ReportUpload);
  }

  private void ReportUpload(Upload upload)
  {
    if (!IsCurrentScreen)
      return;
    (string text, bool warning) = Uploader.Describe(upload);
    notice.Show(text, NoticeSeconds, warning);
  }

  private bool TryLoadPb(Trial trial, out Replay replay)
  {
    if (ReplayStore.TryLoadPb(trial, out replay, out string deleted))
      return true;
    if (deleted != null)
      notice.Show(deleted, NoticeSeconds);
    return false;
  }

  private IEnumerator StartRoutine(
    Trial trial,
    Replay replay = null,
    bool race = false,
    bool fromBoard = false
  )
  {
    if (!fromBoard)
      ReturnBoard = null;
    if (replay == null || race)
      Standings.RefreshMiniBoard(trial);
    FinishSwitch();
    menu.Focused = false;
    Focused = false;
    AreaData area = AreaData.Get(trial.Run.Sid);
    area.Wipe(Overworld, false, null);
    Audio.SetMusic(null, true, true);
    Audio.SetAmbience(null, true);
    yield return 0.5f;
    if (race)
      RunManager.StartRace(trial, replay);
    else if (replay != null)
      RunManager.StartReplay(replay);
    else
      RunManager.StartTrial(trial);
  }

  protected override void RenderHeader()
  {
    if (menu != null)
      DrawCampaignHeader(Campaign, menu.X, menu.Alpha, menu.Focused);
    if (outgoing != null)
      DrawCampaignHeader(outgoingCampaign, outgoing.X, outgoing.Alpha, false);
  }

  protected override void RenderPrompts(float alpha)
  {
    if (!BoardPromptShown)
      return;
    ButtonRow.DrawCenter(Dialog.Clean("climbtrials_trials_leaderboard"), Input.MenuJournal, alpha);
  }

  private static void DrawCampaignHeader(Campaign campaign, float atX, float alpha, bool active)
  {
    Color dim = RowLayout.Dim * alpha;
    float nameY = NameY;
    float totalY = TotalY;
    ActiveFont.Draw(
      campaign.Name,
      new Vector2(atX, nameY),
      new Vector2(0.5f, 0.5f),
      Vector2.One * NameScale,
      dim
    );
    if (Campaigns.All.Count > 1)
    {
      Color arrows = active ? TrialsMenu.Highlight * alpha : dim;
      MTexture arrow = GFX.Gui["dotarrow_outline"];
      float half = ActiveFont.Measure(campaign.Name).X * NameScale / 2f + ArrowGap;
      arrow.DrawCentered(new Vector2(atX + half, nameY), arrows);
      arrow.DrawCentered(new Vector2(atX - half, nameY), arrows, new Vector2(-1f, 1f));
    }
    CampaignRecord record = TrialRecords.Totals(campaign);
    if (Standings.State(campaign) == StandingsState.Offline)
      ActiveFont.Draw(
        Standings.OfflineNote,
        new Vector2(atX, record.TotalTicks == null ? totalY : OfflineY),
        new Vector2(0.5f, 0.5f),
        Vector2.One * OfflineScale,
        dim
      );
    if (record.TotalTicks is not long total)
      return;

    string label = Dialog.Clean("climbtrials_trials_campaign_total");
    string text = TimeText.Format(total);
    float labelWidth = ActiveFont.Measure(label).X * TotalScale;
    float timeWidth = TimeText.Width(text, TimeText.Hud.At(TotalScale));
    float left = atX - (labelWidth + TotalLabelGap + timeWidth) / 2f;
    ActiveFont.Draw(
      label,
      new Vector2(left, totalY),
      new Vector2(0f, 0.5f),
      Vector2.One * TotalScale,
      dim
    );
    float baseline = totalY + TimeText.Height(TimeText.Hud.At(TotalScale)) / 2f;
    TimeText.Draw(
      new Vector2(left + labelWidth + TotalLabelGap, baseline),
      text,
      TimeText.Hud.At(TotalScale),
      MedalIcon.TextColor(record.Medal) * alpha
    );
  }
}
