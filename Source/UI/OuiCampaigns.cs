using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiCampaigns : CampaignListScreen
{
  protected override IReadOnlyList<Campaign> ListedCampaigns() => Campaigns.All;

  protected override CampaignRow RowFor(Campaign campaign) => new(campaign);
}
