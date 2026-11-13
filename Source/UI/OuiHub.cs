using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Replays.Codec;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiHub : ListScreen
{
  private const int NameMaxLength = 20;
  private const float LoadErrorScale = 0.5f;
  private const float CampaignsIconScale = 1.1f;

  private const string EventsIcon = "ClimbTrials/Custom/Trophy_Icon";
  private const string OptionsIcon = "ClimbTrials/Custom/Settings_Icon";
  private const string InviteCodeIcon = "ClimbTrials/Custom/Letter_Icon";
  private const string NameIcon = "ClimbTrials/Custom/Name_Icon";
  private const string RestartIcon = "menu/exit";
  private const int InviteCodeEntryMaxLength = 24;
  private const int InviteCodeMinLength = 6;
  private const int InviteCodeMaxLength = 12;

  private static readonly Regex NameRule = new($"^[A-Za-z0-9 _.-]{{1,{NameMaxLength}}}$");

  private HubRow campaignsRow;
  private HubRow eventsRow;
  private HubRow optionsRow;
  private HubRow inviteCodeRow;
  private HubRow nameRow;
  private int textEntryRowIndex;

  protected override int ScreenDepth => 1;

  protected override string TitleKey => "climbtrials_trials_title";

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  public override IEnumerator Enter(Oui from)
  {
    hintKey = Campaigns.LoadError == null ? Hints.Pick() : null;
    Events.ApplyPending();
    ReplayCodec.PreloadStartingValues();
    return base.Enter(from);
  }

  protected override TrialsMenu BuildMenu()
  {
    TrialsMenu built = [];

    campaignsRow = new HubRow(
      () => Dialog.Clean("climbtrials_hub_campaigns"),
      () => Campaigns.LoadError == null,
      "menu/play",
      outlineIcon: false,
      iconScale: CampaignsIconScale
    );
    campaignsRow.Pressed(OpenCampaigns);
    built.Add(campaignsRow);

    eventsRow = new HubRow(
      () => Dialog.Clean("climbtrials_hub_events"),
      AnyEventKnown,
      EventsIcon,
      showsNewMark: Events.AnyNew
    );
    eventsRow.Pressed(OpenEvents);
    built.Add(eventsRow);

    optionsRow = new HubRow(() => Dialog.Clean("climbtrials_hub_options"), () => true, OptionsIcon);
    optionsRow.Pressed(OpenOptions);
    built.Add(optionsRow);

    inviteCodeRow = new HubRow(
      () => Dialog.Clean("climbtrials_hub_invite_code"),
      InviteCodeLive,
      InviteCodeIcon
    );
    inviteCodeRow.Pressed(OpenInviteCodeEntry);
    built.Add(inviteCodeRow);

    nameRow = new HubRow(NameText, NameLive, NameIcon);
    nameRow.Pressed(OnNameRow);
    built.Add(nameRow);

    if (ModCheck.OthersLoaded)
    {
      HubRow restartRow = new(
        () => Dialog.Clean("climbtrials_restart_allowed_mods"),
        () => true,
        RestartIcon
      );
      restartRow.Pressed(StartRestartHold);
      built.Add(restartRow);
    }

    built.OnCancel = GoBackTo<OuiMainMenu>;
    return built;
  }

  protected override void RenderHeader()
  {
    if (menu == null)
      return;

    if (Campaigns.LoadError == null || notice.Showing)
      return;

    Color color = Colors.Warning * menu.Alpha;
    TextOutline.Draw(
      Campaigns.LoadError,
      new Vector2(menu.X, ScreenLayout.NoticeY),
      new Vector2(0.5f, 0.5f),
      LoadErrorScale,
      color
    );
  }

  protected override int InitialSelection(Oui from)
  {
    if (from is OuiModOptionString)
      return textEntryRowIndex;
    HubRow row = from switch
    {
      OuiClimbTrialsOptions => optionsRow,
      OuiEvents => eventsRow,
      _ when Campaigns.LoadError != null => optionsRow,
      _ => campaignsRow,
    };
    return menu.Items.IndexOf(row);
  }

  public override void Update()
  {
    base.Update();
    bool focused = menu?.Focused == true;
    if (focused && Events.HasPending)
      Events.ApplyPending();
    if (focused && Standings.HubState == StandingsState.Ready && Events.CrossedSinceAnswer())
      Standings.EnsureFreshHub();
    if (menu?.Current is HubRow row && row.Disabled)
      SelectRow(optionsRow);
  }

  public override bool IsStart(Overworld overworld, Overworld.StartMode start)
  {
    if (start != OuiTrials.ReturnStartMode || OuiTrials.ReturnsToList)
      return false;
    OuiTrials.ReturnTrial = null;
    Add(new Coroutine(Enter(null)));
    return true;
  }

  private void OpenCampaigns()
  {
    if (Campaigns.LoadError != null)
    {
      Audio.Play(SFX.ui_main_button_invalid);
      return;
    }
    Audio.Play(SFX.ui_main_button_select);
    menu.Focused = false;
    Overworld.Goto<OuiCampaigns>();
  }

  private static bool AnyEventKnown() => Events.All.Count > 0 || Events.Upcoming.Count > 0;

  private void OpenEvents()
  {
    if (!AnyEventKnown())
    {
      Audio.Play(SFX.ui_main_button_invalid);
      return;
    }
    Audio.Play(SFX.ui_main_button_select);
    menu.Focused = false;
    Overworld.Goto<OuiEvents>();
  }

  private void OpenOptions()
  {
    Audio.Play(SFX.ui_main_button_select);
    menu.Focused = false;
    Overworld.Goto<OuiClimbTrialsOptions>();
  }

  private void StartRestartHold()
  {
    Audio.Play(SFX.ui_main_button_select);
    ConfirmHold.Start(menu, SFX.ui_main_button_select, AllowedModsRestart.Restart);
  }

  private static string NameText()
  {
    if (Identity.Registering)
      return Dialog.Clean("climbtrials_connecting");
    if (Identity.Registered)
      return Dialog.Get("climbtrials_name_row").Replace("{0}", Settings.PlayerName);
    if (Settings.OfflineMode)
      return Dialog.Clean("climbtrials_offline_mode");
    return Dialog.Clean("climbtrials_not_connected");
  }

  private static bool NameLive() =>
    !Settings.OfflineMode && (Identity.Registered || Identity.Registering);

  private static bool InviteCodeLive() => !Settings.OfflineMode && Identity.Registered;

  private void OnNameRow()
  {
    if (Settings.OfflineMode || !Identity.Registered)
    {
      Audio.Play(SFX.ui_main_button_invalid);
      return;
    }

    string chosenName = Settings.PlayerNameChosen ? Settings.PlayerName : "";
    OpenTextEntry("climbtrials_name_headline", chosenName, 1, NameMaxLength, Rename);
  }

  private void OpenTextEntry(
    string headlineKey,
    string text,
    int minLength,
    int maxLength,
    Action<string> onConfirm,
    MTexture letter = null
  )
  {
    Audio.Play(SFX.ui_main_savefile_rename_start);
    menu.Focused = false;
    textEntryRowIndex = menu.Selection;
    OuiHubTextEntry input = Overworld.Goto<OuiHubTextEntry>();
    input.Headline = Dialog.Clean(headlineKey);
    input.Letter = letter;
    input.Init<OuiHub>(
      text,
      null,
      confirmed =>
      {
        if (confirmed)
          onConfirm(input.Value);
      },
      maxLength,
      minLength
    );
  }

  private void Rename(string name)
  {
    if (!NameRule.IsMatch(name) || name.Trim() != name)
    {
      notice.Show(Dialog.Clean("climbtrials_name_invalid"), ScreenLayout.NoticeSeconds);
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
          ServerError.NameReserved => Dialog.Clean("climbtrials_name_reserved"),
          _ => Dialog.Clean("climbtrials_name_offline"),
        };
        if (text != null)
          notice.Show(text, ScreenLayout.NoticeSeconds);
      }
    );
  }

  private void OpenInviteCodeEntry()
  {
    if (!InviteCodeLive())
    {
      Audio.Play(SFX.ui_main_button_invalid);
      return;
    }

    OpenTextEntry(
      "climbtrials_invite_code_headline",
      "",
      InviteCodeMinLength,
      InviteCodeEntryMaxLength,
      JoinWithCode,
      GFX.Gui[InviteCodeIcon]
    );
  }

  private void JoinWithCode(string typed)
  {
    string code = CleanInviteCode(typed);
    if (code.Length < InviteCodeMinLength || code.Length > InviteCodeMaxLength)
    {
      notice.Show(Dialog.Clean("climbtrials_invite_code_invalid"), ScreenLayout.NoticeSeconds);
      return;
    }

    ServerClient.JoinEvent(
      code,
      result =>
      {
        if (result.Ok)
          Standings.RefetchHub();
        if (!IsCurrentScreen)
          return;

        string text = result.Error switch
        {
          ServerError.None => JoinedNotice(result.Value),
          ServerError.CodeInvalid => Dialog.Clean("climbtrials_invite_code_invalid"),
          ServerError.CodeUsedUp => Dialog.Clean("climbtrials_invite_code_used_up"),
          ServerError.RateLimited => Dialog.Clean("climbtrials_join_too_many_tries"),
          ServerError.UpdateRequired => Dialog.Clean("climbtrials_join_needs_update"),
          _ => Dialog.Clean("climbtrials_join_failed"),
        };
        notice.Show(text, ScreenLayout.NoticeSeconds, warning: !result.Ok);
      }
    );
  }

  private static string CleanInviteCode(string typed) =>
    string.Concat(typed.Where(char.IsAsciiLetterOrDigit)).ToUpperInvariant();

  private static string JoinedNotice(JoinAnswer answer)
  {
    if (answer.Already)
      return Dialog.Get("climbtrials_join_already_in").Replace("{0}", answer.Name);

    DateTime now = Events.Now;
    if (answer.OpensAt <= now)
      return Dialog.Get("climbtrials_joined").Replace("{0}", answer.Name);

    return Dialog
      .Get("climbtrials_joined_opens_in")
      .Replace("{1}", EventTiming.Duration(answer.OpensAt - now))
      .Replace("{0}", answer.Name);
  }
}
