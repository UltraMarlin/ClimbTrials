using System.Collections;
using System.Text.RegularExpressions;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiHub : ListScreen
{
  private const int NameMaxLength = 20;
  private const float NoticeSeconds = 5f;
  private const float LoadErrorScale = 0.5f;
  private const float CampaignsIconScale = 1.1f;
  private const string EventsIcon = "collectables/goldberry";

  private static readonly Regex NameRule = new($"^[A-Za-z0-9 _.-]{{1,{NameMaxLength}}}$");

  private HubRow campaignsRow;
  private HubRow eventsRow;
  private HubRow optionsRow;
  private HubRow nameRow;

  protected override int ScreenDepth => 1;

  protected override string TitleKey => "climbtrials_trials_title";

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  public override IEnumerator Enter(Oui from)
  {
    hintKey = Campaigns.LoadError == null ? Hints.Pick() : null;
    Events.ApplyPending();
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
      () => Events.All.Count > 0,
      EventsIcon,
      outlineIcon: false
    );
    eventsRow.Pressed(OpenEvents);
    built.Add(eventsRow);
    optionsRow = new HubRow(
      () => Dialog.Clean("climbtrials_hub_options"),
      () => true,
      "menu/options"
    );
    optionsRow.Pressed(OpenOptions);
    built.Add(optionsRow);
    nameRow = new HubRow(NameText, NameLive);
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
    Standings.EnsureFreshHub();
  }

  protected override StandingsState ServerState => Standings.HubState;

  protected override void RenderHeader()
  {
    if (menu == null)
      return;

    if (Campaigns.LoadError == null || notice.Showing)
      return;

    Color color = Colors.Warning * menu.Alpha;
    ActiveFont.DrawOutline(
      Campaigns.LoadError,
      new Vector2(menu.X, NoticeY),
      new Vector2(0.5f, 0.5f),
      Vector2.One * LoadErrorScale,
      color,
      2f,
      TextOutline.StrokeFor(color)
    );
  }

  protected override int InitialSelection(Oui from)
  {
    HubRow row = from switch
    {
      OuiModOptionString => nameRow,
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
    if (menu?.Focused == true && Events.HasPending)
      Events.ApplyPending();
    if (menu?.Current is not HubRow row || !row.Disabled)
      return;
    menu.Current.OnLeave?.Invoke();
    menu.Selection = menu.Items.IndexOf(optionsRow);
    menu.Current.OnEnter?.Invoke();
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

  private void OpenEvents()
  {
    if (Events.All.Count == 0)
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

  private static string NameText() =>
    Identity.Registration switch
    {
      RegistrationState.Registering => Dialog.Clean("climbtrials_connecting"),
      RegistrationState.Registered => Dialog
        .Get("climbtrials_name_row")
        .Replace("{0}", Settings.PlayerName),
      _ when Settings.OfflineMode => Dialog.Clean("climbtrials_offline_mode"),
      _ => Dialog.Clean("climbtrials_not_connected"),
    };

  private static bool NameLive() =>
    !Settings.OfflineMode
    && Identity.Registration is RegistrationState.Registered or RegistrationState.Registering;

  private void OnNameRow()
  {
    if (Settings.OfflineMode)
    {
      Audio.Play(SFX.ui_main_button_invalid);
      return;
    }

    if (!Identity.Registered)
    {
      Audio.Play(SFX.ui_main_button_invalid);
      return;
    }

    Audio.Play(SFX.ui_main_savefile_rename_start);
    menu.Focused = false;
    OuiModOptionString input = Overworld.Goto<OuiModOptionString>();
    input.Init<OuiHub>(
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
          ServerError.NameReserved => Dialog.Clean("climbtrials_name_reserved"),
          _ => Dialog.Clean("climbtrials_name_offline"),
        };
        if (text != null)
          notice.Show(text, NoticeSeconds);
      }
    );
  }
}
