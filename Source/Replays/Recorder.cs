using System;
using System.Diagnostics;
using System.IO;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays.Codec;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Replays;

public enum StopReason
{
  None,
  Limit,
  Reload,
  State,
}

public static class Recorder
{
  private const string LogTag = "ClimbTrials/Replay";

  public static Replay Last { get; private set; }
  public static StopReason Stopped { get; private set; }
  private static Replay current;
  private static readonly Stopwatch wall = new();

  public static bool Recording => current != null;

  public static void Load()
  {
    On.Monocle.MInput.Update += OnMInputUpdate;
  }

  public static void Unload()
  {
    On.Monocle.MInput.Update -= OnMInputUpdate;
  }

  public static void Begin(
    Trial trial,
    int loadSeed,
    int runSeed,
    Player.IntroTypes introType,
    Level level
  )
  {
    if (Stopped != StopReason.None)
      return;
    current = new Replay
    {
      TrialId = trial?.Id,
      TrialHash = Replay.ShortTrialHash(
        trial?.DefinitionHash ?? new byte[Trial.DefinitionHashBytes]
      ),
      CelesteVersion = Celeste.Instance.Version.ToString(),
      Modules = ModCheck.Modules(),
      Language = Settings.Instance.Language,
      GrabMode = Settings.Instance.GrabMode,
      CrouchDashMode = Settings.Instance.CrouchDashMode,
      ScreenShake = Settings.Instance.ScreenShake,
      IntroType = introType,
      TimeActive = level.TimeActive,
      RawTimeActive = level.RawTimeActive,
      RawDeltaTime = Engine.RawDeltaTime,
      LoadSeed = loadSeed,
      RunSeed = runSeed,
      Buttons = InputCapture.ButtonStates(),
      IsOwn = true,
    };
    wall.Restart();
  }

  public static Replay Finish(long ticks)
  {
    if (current == null)
      return null;
    wall.Stop();
    current.Ticks = ticks;
    current.WallMilliseconds = wall.ElapsedMilliseconds;
    current.AchievedAtUtc = DateTime.UtcNow;
    current.DebugCauses = DebugUse.Causes;
    string limitViolation = EncodeAndFindLimitViolation(current);
    if (limitViolation != null)
    {
      Logger.Warn(
        LogTag,
        $"the run on '{current.TrialId}' has no replay, it violates the format's limits: "
          + limitViolation
      );
      current = null;
      return null;
    }
    Last = current;
    current = null;
    return Last;
  }

  private static string EncodeAndFindLimitViolation(Replay replay)
  {
    try
    {
      replay.ToBytes();
      return null;
    }
    catch (InvalidDataException e)
    {
      return e.Message;
    }
    catch (StartingValuesException)
    {
      return null;
    }
  }

  public static void RecordPauseMenu(TextMenu menu)
  {
    current?.PauseMenus.Add(PauseRows.Describe(menu));
  }

  public static void Stop(StopReason reason)
  {
    current = null;
    Stopped = reason;
  }

  public static void Abort()
  {
    current = null;
    Last = null;
    Stopped = StopReason.None;
    wall.Reset();
  }

  private static void OnMInputUpdate(On.Monocle.MInput.orig_Update orig)
  {
    orig();
    if (current == null || !InputCapture.Counts())
      return;
    current.Frames.Add(InputCapture.Sample(current));
  }
}
