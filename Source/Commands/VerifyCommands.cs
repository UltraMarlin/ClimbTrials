using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Commands;

public static class VerifyCommands
{
  private const string AllCampaigns = "all-campaigns";
  private const string Usage =
    "ct_verify_replays <trialId|campaignId|eventId|all-campaigns> [maxRank]";

  [Command(
    "ct_verify_replays",
    "ClimbTrials: admin only command to verify batches of replays at once. " + Usage
  )]
  public static void VerifyReplays(string target = null, int maxRank = 0)
  {
    if (target == null)
    {
      Engine.Commands.Log($"usage: {Usage}");
      Engine.Commands.Log("runs every replay on leaderboards, or only up to maxRank");
      return;
    }

    bool started;
    if (target == AllCampaigns)
      started = ReplayVerifier.Start("allCampaigns", null, maxRank, "all campaigns");
    else if (Campaigns.FindTrial(target) != null)
      started = ReplayVerifier.Start("trial", target, maxRank, target);
    else
      started = ReplayVerifier.Start("campaign", target, maxRank, target);

    if (started)
      ConsoleOutput.Close();
  }
}
