using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;

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
  public const int DefinitionHashBytes = 32;

  public string Id { get; init; }
  public string Name { get; init; }
  public Difficulty Difficulty { get; init; }
  public RunDefinition Run { get; init; }
  public GoalTimes Goals { get; init; }
  public int ReplayLimitSeconds { get; init; } = DefaultReplayLimitSeconds;
  public IReadOnlyList<byte[]> PreviousTrialHashes { get; init; } = [];

  public bool FitsReplayLimit(long ticks) => TrialSpec.FitsReplayLimit(ReplayLimitSeconds, ticks);

  public byte[] DefinitionHash =>
    definitionHash ??= SHA256.HashData(Encoding.UTF8.GetBytes(DefinitionText()));
  private byte[] definitionHash;

  public IReadOnlyList<byte[]> AcceptedTrialHashes =>
    acceptedTrialHashes ??= [Replay.ShortTrialHash(DefinitionHash), .. PreviousTrialHashes];
  private byte[][] acceptedTrialHashes;

  public string SkullTexture =>
    Difficulty switch
    {
      Difficulty.Normal => "collectables/skullBlue",
      Difficulty.Hard => "collectables/skullRed",
      Difficulty.Expert => "collectables/skullGold",
      _ => "collectables/skullBlue",
    };

  public string DefinitionText()
  {
    RunDefinition run = Run;
    string[] lines =
    [
      run.Sid,
      Int((int)run.Mode),
      run.StartRoom,
      Vec(run.Spawn),
      run.SpawnFacing is Facings facing ? Int((int)facing) : "",
      run.FinishRoom,
      Int((int)run.FinishKind),
      $"{Int(run.Finish.X)},{Int(run.Finish.Y)},{Int(run.Finish.Width)},{Int(run.Finish.Height)}",
      string.Join(
        ";",
        run.Waypoints.Select(waypoint =>
          $"{waypoint.Room}|{Vec(waypoint.Position)}|{(waypoint.Spawn is Vector2 spawn ? Vec(spawn) : "")}|{Bool(waypoint.SetsSpawn)}"
        )
      ),
      string.Join(";", run.CameraOffsets.Select(offset => $"{offset.Room}|{Vec(offset.Offset)}")),
      Int(run.RequiredStrawberries),
      Int(run.RequiredSummitGems),
      run.Dashes is int dashes ? Int(dashes) : "",
      Bool(run.Grabless),
      Bool(run.NoRefills),
      run.CoreMode is Session.CoreModes coreMode ? Int((int)coreMode) : "",
      run.DreamDash is bool dreamDash ? Bool(dreamDash) : "",
      Int((int)run.IntroType),
    ];
    return string.Join("\n", lines);
  }

  private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

  private static string Vec(Vector2 vector) => $"{Float(vector.X)},{Float(vector.Y)}";

  private static string Bool(bool value) => value ? "true" : "false";

  private static string Float(float value) =>
    (value == 0f ? 0f : value).ToString("R", CultureInfo.InvariantCulture);
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
  public static IReadOnlyList<Campaign> All { get; private set; } = [];
  public static string LoadError { get; private set; }
  private static int lastIssued;
  private static readonly string[] RetiredTrialIds = ["t08", "t10", "t11", "t13"];

  public static void Load()
  {
    try
    {
      All = TrialFile.Load();
      Validate();
      LoadError = null;
      lastIssued = 0;
    }
    catch (Exception e)
    {
      All = [];
      LoadError = e.Message;
      Logger.Log(LogLevel.Error, TrialFile.LogTag, e.Message);
    }
  }

  public static Trial FindTrial(string id) =>
    All.SelectMany(campaign => campaign.Trials).FirstOrDefault(trial => trial.Id == id)
    ?? Events.FindTrial(id);

  public static Campaign CampaignOf(Trial trial) =>
    All.FirstOrDefault(campaign => campaign.Trials.Contains(trial)) ?? Events.Of(trial)?.Campaign;

  public static bool IsEvent(Campaign campaign) => Events.Of(campaign) != null;

  public static string NextTrialId()
  {
    int highest = lastIssued;
    IEnumerable<string> taken = All.SelectMany(campaign => campaign.Trials)
      .Select(trial => trial.Id)
      .Concat(RetiredTrialIds);
    foreach (string id in taken)
      if (TrialNumber(id) is int number && number > highest)
        highest = number;
    lastIssued = highest + 1;
    return $"t{lastIssued:00}";
  }

  private static int? TrialNumber(string id)
  {
    if (id == null || id.Length < 2 || id[0] != 't')
      return null;
    for (int i = 1; i < id.Length; i++)
      if (!char.IsAsciiDigit(id[i]))
        return null;
    return int.TryParse(id[1..], out int number) ? number : null;
  }

  public static void Validate()
  {
    HashSet<string> campaignIds = [];
    HashSet<string> trialIds = [];
    foreach (Campaign campaign in All)
    {
      if (!campaignIds.Add(campaign.Id))
        throw new InvalidOperationException($"duplicate campaign id '{campaign.Id}'");
      CheckTrials(campaign, trialIds);
    }
  }

  public static void CheckTrials(Campaign campaign, HashSet<string> trialIds)
  {
    if (campaign.Trials.Count == 0)
      throw new InvalidOperationException($"campaign '{campaign.Id}' has no trials");
    foreach (Trial trial in campaign.Trials)
    {
      if (!trialIds.Add(trial.Id))
        throw new InvalidOperationException($"duplicate trial id '{trial.Id}'");
      if (trial.ReplayLimitSeconds < 1)
        throw new InvalidOperationException($"trial '{trial.Id}' has a replay limit below 1 s");
    }
  }
}
