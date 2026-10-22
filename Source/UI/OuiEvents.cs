using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiEvents : CampaignListScreen
{
  protected override string TitleKey => "climbtrials_events_title";

  protected override IReadOnlyList<Campaign> ListedCampaigns() => Events.Ordered();

  protected override CampaignRow RowFor(Campaign campaign) => new EventRow(campaign);
}
