using System;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays;

namespace Celeste.Mod.ClimbTrials.Online;

public sealed record TimeSubmission(long Ticks, string AchievedAtUtcTicks, string TrialHash);

public sealed class Submission
{
  public byte[] ReplayBytes { get; init; }
  public TimeSubmission Time { get; init; }

  public static bool TryBuild(
    Replay replay,
    Trial trial,
    out Submission submission,
    out ValidationResult check
  )
  {
    submission = null;
    check = ReplayValidator.Check(replay, TrialSpec.From(trial), ModCheck.Allowed);
    if (!check.Passes)
      return false;
    submission = new Submission { ReplayBytes = replay.ToBytes() };
    return true;
  }

  public static Submission ForTime(Trial trial, TrialRecord record)
  {
    DateTime achieved = record.AchievedAtUtc;
    if (achieved.Kind == DateTimeKind.Local)
      achieved = achieved.ToUniversalTime();
    return new()
    {
      Time = new TimeSubmission(
        record.BestTicks,
        achieved.Ticks.ToString(),
        Convert.ToHexString(trial.DefinitionHash).ToLowerInvariant()
      ),
    };
  }
}
