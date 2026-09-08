using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiCampaigns : ListScreen
{
  private readonly List<CampaignRow> rows = [];

  protected override int ScreenDepth => 1;

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
    built.OnCancel = () =>
    {
      built.Focused = false;
      Audio.Play(SFX.ui_main_button_back);
      Overworld.Goto<OuiMainMenu>();
    };
    return built;
  }

  protected override int InitialSelection(Oui from)
  {
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
}
