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
  public string Id { get; init; }
  public string Name { get; init; }
  public Difficulty Difficulty { get; init; }
  public RunDefinition Run { get; init; }

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
  public IReadOnlyList<Trial> Trials { get; init; }
}

public static class Campaigns
{
  public static readonly IReadOnlyList<Campaign> All = [FirstCampaign.Definition];

  public static Trial FindTrial(string id) =>
    All.SelectMany(c => c.Trials).FirstOrDefault(t => t.Id == id);
}
