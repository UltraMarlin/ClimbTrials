using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays.Codec;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Replays;

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
  public const double WallRejectBelow = 0.9;
  public const double WallFlagAbove = 1.5;
  public const double FrozenFlagAbove = 0.25;
  public const double MaxFrameToTimerRatio = 1.5;
  public const int FreezeBufferFrames = 60;

  public const string TrialHashMismatch = "trial definition differs from the server's";

  public static ValidationResult Check(Replay replay, Trial trial)
  {
    ValidationResult result = new();
    if (trial == null)
    {
      result.Rejections.Add($"unknown trial '{replay.TrialId}'");
      return result;
    }
    if (!replay.MatchesTrialHash(trial.AcceptedTrialHashes))
    {
      result.Rejections.Add(TrialHashMismatch);
      return result;
    }

    if (replay.Frames.Count == 0)
    {
      result.Rejections.Add("no frames");
      return result;
    }

    if (Attempt.SecondsToTicks(replay.RawDeltaTime) != HeaderCoding.TicksPerFrame)
      result.Rejections.Add($"frame time {replay.RawDeltaTime} s is not 1/60 s");

    int counted = 0;
    foreach (InputFrame frame in replay.Frames)
      if (frame.LevelUpdated)
        counted++;
    int frozen = replay.Frames.Count - counted;
    long expected = counted * HeaderCoding.TicksPerFrame;
    if (replay.Ticks != expected)
      result.Rejections.Add(
        $"header time {TimeText.Format(replay.Ticks)}, "
          + $"but {counted} counted frames add up to {TimeText.Format(expected)}"
      );
    double timerFrames = (double)replay.Ticks / HeaderCoding.TicksPerFrame;
    long frameLimit = (long)Math.Floor(MaxFrameToTimerRatio * timerFrames) + FreezeBufferFrames;
    if (replay.Frames.Count > frameLimit)
      result.Rejections.Add(
        $"the replay has {replay.Frames.Count} frames, more than {frameLimit} for its time"
      );
    if (frozen > replay.Frames.Count * FrozenFlagAbove)
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

    if (!trial.FitsReplayLimit(replay.Ticks))
      result.Rejections.Add(
        $"time is over the trial's recording limit of {trial.ReplayLimitSeconds} s"
      );

    InputFrame first = replay.Frames[0];
    Vector2 spawn = trial.Run.Spawn;
    if (!first.HasPlayer || first.Position.X != spawn.X || first.Position.Y != spawn.Y)
      result.Rejections.Add(
        $"first frame is at {first.Position}, the spawn is at ({spawn.X}, {spawn.Y})"
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

    CheckModules(replay.Modules, result);
    CheckDebug(replay.DebugCauses, result);

    return result;
  }

  public static ValidationResult CheckAttempt()
  {
    ValidationResult result = new();
    CheckModules(ModCheck.Modules(), result);
    CheckDebug(DebugUse.Causes, result);
    return result;
  }

  private static void CheckModules(IEnumerable<ModuleRef> modules, ValidationResult result)
  {
    List<string> others = [];
    foreach (ModuleRef module in modules)
      if (!ModCheck.Allowed.Contains(module.Name))
        others.Add(module.Name);
    if (others.Count > 0)
      result.Rejections.Add("mods outside the allowlist: " + string.Join(", ", others));
  }

  private static void CheckDebug(DebugCause causes, ValidationResult result)
  {
    if (causes != DebugCause.None)
      result.Rejections.Add("debug features used: " + causes);
  }

  private static bool SamePlayerState(InputFrame a, InputFrame b) =>
    a.HasPlayer == b.HasPlayer
    && a.Position == b.Position
    && a.State == b.State
    && a.Dashes == b.Dashes
    && a.Stamina == b.Stamina;
}
