using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;

namespace Celeste.Mod.ClimbTrials.Replays;

public sealed record TrialSpec(string Id, float SpawnX, float SpawnY, int ReplayLimitSeconds)
{
  public static TrialSpec From(Trial trial) =>
    trial == null
      ? null
      : new(trial.Id, trial.Run.Spawn.X, trial.Run.Spawn.Y, RunManager.ReplayLimitFor(trial));
}

public sealed class ValidationResult
{
  public List<string> Rejections { get; } = [];
  public List<string> Flags { get; } = [];

  public bool Passes => Rejections.Count == 0;

  public string Describe()
  {
    string verdict = Passes ? "passes" : "rejected: " + string.Join("; ", Rejections);
    if (Flags.Count > 0)
      verdict += ", flags: " + string.Join("; ", Flags);
    return verdict;
  }
}

public static class ReplayValidator
{
  public const long FrameTicks = 17 * TimeSpan.TicksPerMillisecond;
  public const double WallRejectBelow = 0.9;
  public const double WallFlagAbove = 1.5;
  public const double FrozenFlagAbove = 0.5;

  public static ValidationResult Check(
    Replay replay,
    TrialSpec trial,
    IReadOnlySet<string> allowedModules
  )
  {
    ValidationResult result = new();
    if (trial == null)
    {
      result.Rejections.Add($"unknown trial '{replay.TrialId}'");
      return result;
    }

    if (replay.Frames.Count == 0)
    {
      result.Rejections.Add("no frames");
      return result;
    }

    if (Attempt.SecondsToTicks(replay.RawDeltaTime) != FrameTicks)
      result.Rejections.Add($"frame time {replay.RawDeltaTime} s is not 1/60 s");

    int counted = 0;
    foreach (InputFrame frame in replay.Frames)
      if (frame.LevelUpdated)
        counted++;
    int frozen = replay.Frames.Count - counted;
    long expected = counted * FrameTicks;
    if (Math.Abs(replay.Ticks - expected) > FrameTicks)
      result.Rejections.Add(
        $"header time {Attempt.Format(replay.Ticks)}, "
          + $"but {counted} counted frames add up to {Attempt.Format(expected)}"
      );
    if (frozen > counted * FrozenFlagAbove)
      result.Flags.Add($"{frozen} of {replay.Frames.Count} frames are frozen");

    for (int i = 0; i + 1 < replay.Frames.Count; i++)
    {
      if (replay.Frames[i].LevelUpdated)
        continue;
      if (!SamePlayerState(replay.Frames[i], replay.Frames[i + 1]))
      {
        result.Rejections.Add(
          $"frame {i} is marked frozen, but the player changed on the next frame"
        );
        break;
      }
    }

    if (replay.Ticks > trial.ReplayLimitSeconds * TimeSpan.TicksPerSecond)
      result.Rejections.Add(
        $"time is over the trial's recording limit of {trial.ReplayLimitSeconds} s"
      );

    InputFrame first = replay.Frames[0];
    if (!first.HasPlayer || first.Position.X != trial.SpawnX || first.Position.Y != trial.SpawnY)
      result.Rejections.Add(
        $"first frame is at {first.Position}, the spawn is at ({trial.SpawnX}, {trial.SpawnY})"
      );

    double expectedWallMs = replay.Frames.Count * replay.RawDeltaTime * 1000.0;
    if (replay.WallMilliseconds < WallRejectBelow * expectedWallMs)
      result.Rejections.Add(
        $"real time {replay.WallMilliseconds} ms is too short for {replay.Frames.Count} frames, "
          + $"expected about {expectedWallMs:F0} ms"
      );
    else if (replay.WallMilliseconds > WallFlagAbove * expectedWallMs)
      result.Flags.Add(
        $"real time {replay.WallMilliseconds} ms is too long for {replay.Frames.Count} frames, "
          + $"expected about {expectedWallMs:F0} ms"
      );

    List<string> others = [];
    foreach (ModuleRef module in replay.Modules)
      if (!allowedModules.Contains(module.Name))
        others.Add(module.Name);
    if (others.Count > 0)
      result.Rejections.Add("mods outside the allowlist: " + string.Join(", ", others));

    if (replay.DebugCauses != DebugCause.None)
      result.Rejections.Add("debug features used: " + replay.DebugCauses);

    return result;
  }

  private static bool SamePlayerState(InputFrame a, InputFrame b) =>
    a.HasPlayer == b.HasPlayer
    && a.Position == b.Position
    && a.State == b.State
    && a.Dashes == b.Dashes
    && a.Stamina == b.Stamina;
}
