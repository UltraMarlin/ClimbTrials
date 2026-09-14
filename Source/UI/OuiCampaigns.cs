using System.Collections.Generic;
using System.Text.RegularExpressions;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.UI;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiCampaigns : ListScreen
{
  private const int NameMaxLength = 20;
  private const float NoticeSeconds = 5f;

  private static readonly Regex NameRule = new($"^[A-Za-z0-9 _.-]{{1,{NameMaxLength}}}$");

  private readonly List<CampaignRow> rows = [];
  private NameRow nameRow;

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

  protected override void FetchOnline() => Identity.EnsureRegistered();

  protected override int InitialSelection(Oui from)
  {
    if (from is OuiModOptionString)
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
