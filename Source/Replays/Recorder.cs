using System;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class Recorder
{
  public static Replay Last { get; private set; }
  public static bool StoppedAtLimit { get; private set; }
  private static Replay current;

  public static bool Recording => current != null;

  public static void Load()
  {
    On.Monocle.MInput.Update += OnMInputUpdate;
  }

  public static void Unload()
  {
    On.Monocle.MInput.Update -= OnMInputUpdate;
  }

  public static void Begin(string trialId, int loadSeed, int runSeed, Level level)
  {
    StoppedAtLimit = false;
    if (ModCheck.OthersLoaded)
      return;
    current = new Replay
    {
      TrialId = trialId,
      CelesteVersion = Celeste.Instance.Version.ToString(),
      EverestVersion = Everest.VersionString,
      Modules = ModCheck.Modules(),
      OtherModsLoaded = false,
      PlayerName = Dialog.Clean("climbtrials_replay_you"),
      Language = Settings.Instance.Language,
      GrabMode = Settings.Instance.GrabMode,
      CrouchDashMode = Settings.Instance.CrouchDashMode,
      ScreenShake = Settings.Instance.ScreenShake,
      LastAim = Input.LastAim,
      TimeActive = level.TimeActive,
      RawTimeActive = level.RawTimeActive,
      RawDeltaTime = Engine.RawDeltaTime,
      LoadSeed = loadSeed,
      RunSeed = runSeed,
      Buttons = InputCapture.ButtonStates(),
    };
  }

  public static Replay Finish(long ticks)
  {
    if (current == null)
      return null;
    current.Ticks = ticks;
    current.AchievedAtUtc = DateTime.UtcNow;
    Last = current;
    current = null;
    return Last;
  }

  public static void StopAtLimit()
  {
    current = null;
    StoppedAtLimit = true;
  }

  public static void Abort()
  {
    current = null;
    Last = null;
    StoppedAtLimit = false;
  }

  private static void OnMInputUpdate(On.Monocle.MInput.orig_Update orig)
  {
    orig();
    if (current == null || !InputCapture.Counts())
      return;
    current.Frames.Add(InputCapture.Sample());
  }
}
