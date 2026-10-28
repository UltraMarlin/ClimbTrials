using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.ClimbTrials.Content;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiCampaigns : CampaignListScreen
{
  protected override IEnumerable<SlidingRow> ListedRows() =>
    Campaigns.All.Select(campaign => new CampaignRow(campaign));
}
