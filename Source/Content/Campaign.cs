using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.ClimbTrials.Runs;

namespace Celeste.Mod.ClimbTrials.Content;

public enum Difficulty
{
  Normal,
  Hard,
  Expert,
}

public sealed class Trial
{
  public const int DefaultReplayLimitSeconds = 120;

  public string Id { get; init; }
  public string Name { get; init; }
  public Difficulty Difficulty { get; init; }
  public RunDefinition Run { get; init; }
  public GoalTimes Goals { get; init; }
  public int ReplayLimitSeconds { get; init; } = DefaultReplayLimitSeconds;

  public string SkullTexture =>
    Difficulty switch
    {
      Difficulty.Normal => "collectables/skullBlue",
      Difficulty.Hard => "collectables/skullRed",
      Difficulty.Expert => "collectables/skullGold",
      _ => "collectables/skullBlue",
    };
}

public sealed class Campaign
{
  public string Id { get; init; }
  public string Name { get; init; }
  public string Description { get; init; }
  public IReadOnlyList<Trial> Trials { get; init; }
}

public static class Campaigns
{
  public static readonly IReadOnlyList<Campaign> All =
  [
    FirstCampaign.Definition,
    SecondCampaign.Definition,
  ];

  public static Trial FindTrial(string id) =>
    All.SelectMany(c => c.Trials).FirstOrDefault(t => t.Id == id);

  public static Campaign CampaignOf(Trial trial) =>
    All.FirstOrDefault(c => c.Trials.Contains(trial));

  public static int IndexOf(Campaign campaign)
  {
    for (int i = 0; i < All.Count; i++)
      if (All[i] == campaign)
        return i;
    return 0;
  }

  public static void Validate()
  {
    HashSet<string> campaignIds = [];
    HashSet<string> trialIds = [];
    foreach (Campaign campaign in All)
    {
      if (!campaignIds.Add(campaign.Id))
        throw new InvalidOperationException($"ClimbTrials: duplicate campaign id '{campaign.Id}'");
      if (campaign.Trials.Count == 0)
        throw new InvalidOperationException($"ClimbTrials: campaign '{campaign.Id}' has no trials");
      foreach (Trial trial in campaign.Trials)
      {
        if (!trialIds.Add(trial.Id))
          throw new InvalidOperationException($"ClimbTrials: duplicate trial id '{trial.Id}'");
        if (trial.ReplayLimitSeconds < 1)
          throw new InvalidOperationException(
            $"ClimbTrials: trial '{trial.Id}' has a replay limit below 1 s"
          );
      }
    }
  }
}
