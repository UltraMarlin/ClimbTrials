using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Commands;

public static class RecordCommands
{
  [Command(
    "ct_delete_records",
    "ClimbTrials: delete PBs and replays, all or one campaign. ct_delete_records [campaignId]"
  )]
  public static void DeleteRecords(string campaignId = null)
  {
    if (campaignId == null)
    {
      int pbs = TrialRecords.Clear();
      int replays = ReplayStore.DeleteAll();
      Engine.Commands.Log($"deleted {pbs} PBs and {replays} replays");
      return;
    }
    Campaign campaign = Campaigns.All.FirstOrDefault(campaign => campaign.Id == campaignId);
    if (campaign == null)
    {
      string known = string.Join(", ", Campaigns.All.Select(campaign => campaign.Id));
      Engine.Commands.Log($"unknown campaign '{campaignId}'. Known: {known}");
      return;
    }
    List<string> ids = campaign.Trials.Select(trial => trial.Id).ToList();
    int removed = TrialRecords.Remove(ids);
    int deleted = 0;
    foreach (string id in ids)
      if (ReplayStore.Delete(id))
        deleted++;
    Engine.Commands.Log($"campaign {campaign.Id}: deleted {removed} PBs and {deleted} replays");
  }
}
