using System.Collections.Generic;
using System.Text.RegularExpressions;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiCampaigns : ListScreen
{
  private const int NameMaxLength = 20;
  private const float NoticeSeconds = 5f;
  private const float LoadErrorY = 240f;
  private const float LoadErrorScale = 0.5f;
  private const float OfflineY = 190f;
  private const float OfflineScale = 0.45f;

  private static readonly Regex NameRule = new($"^[A-Za-z0-9 _.-]{{1,{NameMaxLength}}}$");

  private readonly List<CampaignRow> rows = [];
  private NameRow nameRow;
  private LeaderboardView view;

  protected override int ScreenDepth => 1;

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  protected override TrialsMenu BuildMenu()
  {
    TrialsMenu built = [];
    rows.Clear();
    foreach (Campaign campaign in Campaigns.All)
    {
      CampaignRow row = new(campaign);
      row.Pressed(() => Open(campaign));
      built.Add(row);
      if (!string.IsNullOrEmpty(campaign.Description))
        RowDescription.Attach(built, row, campaign.Description);
      rows.Add(row);
    }
    built.Add(new Gap(RowLayout.RowHeight / 2f));
    nameRow = new NameRow();
    nameRow.Pressed(OnNameRow);
    built.Add(nameRow);
    built.OnCancel = () =>
    {
      built.Focused = false;
      Audio.Play(SFX.ui_main_button_back);
      Overworld.Goto<OuiMainMenu>();
    };
    return built;
  }

  protected override void FetchOnline()
  {
    Identity.EnsureRegistered();
    Standings.EnsureFreshTotals();
  }

  protected override void RenderHeader()
  {
    if (menu == null)
      return;
    if (Standings.TotalsState == StandingsState.Offline)
      ActiveFont.Draw(
        Standings.OfflineNote,
        new Vector2(menu.X, OfflineY + yOffset),
        new Vector2(0.5f, 0.5f),
        Vector2.One * OfflineScale,
        RowLayout.Dim * menu.Alpha
      );
    if (Campaigns.LoadError == null)
      return;
    Color color = Colors.Warning * menu.Alpha;
    ActiveFont.DrawOutline(
      Campaigns.LoadError,
      new Vector2(menu.X, LoadErrorY + yOffset),
      new Vector2(0.5f, 0.5f),
      Vector2.One * LoadErrorScale,
      color,
      2f,
      TextOutline.StrokeFor(color)
    );
  }

  protected override void RenderPrompts(float alpha)
  {
    if (menu?.Current is not CampaignRow)
      return;
    float shown = alpha * (1f - yOffset / LeaderboardView.SlideDistance);
    ButtonRow.DrawCenter(Dialog.Clean("climbtrials_trials_leaderboard"), Input.MenuJournal, shown);
  }

  protected override void CancelTransitions()
  {
    if (view == null)
      return;
    view.Discard();
    view = null;
    SetBoardSlide(0f);
  }

  public override void Update()
  {
    base.Update();
    if (
      view == null
      && menu != null
      && menu.Focused
      && Input.MenuJournal.Pressed
      && menu.Current is CampaignRow row
    )
    {
      Audio.Play(SFX.ui_main_button_select);
      OpenBoard(row.Campaign);
    }
  }

  private void OpenBoard(Campaign campaign)
  {
    menu.Focused = false;
    Focused = false;
    view = new LeaderboardView(BoardSubject.Of(campaign), SetBoardSlide, OnBoardClosed);
    Scene.Add(view);
  }

  private void SetBoardSlide(float progress)
  {
    yOffset = progress * LeaderboardView.SlideDistance;
    menu.YOffset = yOffset;
  }

  private void OnBoardClosed(BoardSubject shown)
  {
    view = null;
    int index = rows.FindIndex(r => r.Campaign.Id == shown.Id);
    if (index >= 0 && menu.Current != rows[index])
    {
      menu.Current?.OnLeave?.Invoke();
      menu.Selection = menu.Items.IndexOf(rows[index]);
      menu.Current?.OnEnter?.Invoke();
    }
    Standings.EnsureFreshTotals();
    menu.Focused = true;
    Focused = true;
  }

  protected override int InitialSelection(Oui from)
  {
    OuiTrials.ReturnBoard = null;
    if (from is OuiModOptionString || rows.Count == 0)
      return menu.Items.IndexOf(nameRow);
    int index = from is OuiTrials trials ? rows.FindIndex(r => r.Campaign == trials.Campaign) : 0;
    return menu.Items.IndexOf(rows[index < 0 ? 0 : index]);
  }

  public override bool IsStart(Overworld overworld, Overworld.StartMode start)
  {
    if (start != OuiTrials.ReturnStartMode || OuiTrials.ReturnTrial != null)
      return false;
    Add(new Coroutine(Enter(null)));
    return true;
  }

  private void Open(Campaign campaign)
  {
    Audio.Play("event:/ui/main/button_select");
    menu.Focused = false;
    Overworld.Goto<OuiTrials>().Campaign = campaign;
  }

  private void OnNameRow()
  {
    if (Settings.OfflineMode)
    {
      Audio.Play("event:/ui/main/button_invalid");
      return;
    }
    if (!Identity.Registered)
    {
      Audio.Play("event:/ui/main/button_select");
      Identity.EnsureRegistered();
      return;
    }
    Audio.Play("event:/ui/main/savefile_rename_start");
    menu.Focused = false;
    OuiModOptionString input = Overworld.Goto<OuiModOptionString>();
    input.Init<OuiCampaigns>(
      Settings.PlayerNameChosen ? Settings.PlayerName : "",
      null,
      confirmed =>
      {
        if (confirmed)
          Rename(input.Value);
      },
      NameMaxLength,
      1
    );
  }

  private void Rename(string name)
  {
    if (!NameRule.IsMatch(name) || name.Trim() != name)
    {
      notice.Show(Dialog.Clean("climbtrials_name_invalid"), NoticeSeconds);
      return;
    }
    ServerClient.Rename(
      name,
      result =>
      {
        if (result.Ok)
          Identity.AdoptChosenName(result.Value.Name);
        if (!IsCurrentScreen)
          return;
        string text = result.Error switch
        {
          ServerError.None => null,
          ServerError.NameTaken => Dialog.Clean("climbtrials_name_taken"),
          ServerError.NameInvalid => Dialog.Clean("climbtrials_name_invalid"),
          _ => Dialog.Clean("climbtrials_name_offline"),
        };
        if (text != null)
          notice.Show(text, NoticeSeconds);
      }
    );
  }
}
