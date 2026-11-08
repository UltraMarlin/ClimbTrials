using Celeste.Mod.ClimbTrials.Runs;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class RunGuards
{
  private const string LogTag = "ClimbTrials/Guards";

  public static void Load()
  {
    On.Celeste.UserIO.SaveHandler += OnSaveHandler;
    On.Celeste.SaveData.SetCheckpoint += OnSetCheckpoint;
    On.Celeste.Level.AutoSave += OnAutoSave;
    On.Celeste.Level.CompleteArea_bool_bool_bool += OnCompleteArea;
    On.Celeste.FinalBoss.Added += OnFinalBossAdded;
    On.Celeste.SummitCheckpoint.Update += OnSummitCheckpointUpdate;
  }

  public static void Unload()
  {
    On.Celeste.UserIO.SaveHandler -= OnSaveHandler;
    On.Celeste.SaveData.SetCheckpoint -= OnSetCheckpoint;
    On.Celeste.Level.AutoSave -= OnAutoSave;
    On.Celeste.Level.CompleteArea_bool_bool_bool -= OnCompleteArea;
    On.Celeste.FinalBoss.Added -= OnFinalBossAdded;
    On.Celeste.SummitCheckpoint.Update -= OnSummitCheckpointUpdate;
  }

  private static void OnSaveHandler(
    On.Celeste.UserIO.orig_SaveHandler orig,
    bool file,
    bool settings
  )
  {
    if (RunManager.OwnsSaveData)
      file = false;
    if (!file && !settings)
      return;

    if (Replayer.IsReplayingInputs && settings)
    {
      Replayer.RestorePlayerSettings();
      orig(file, settings);
      Replayer.ApplyRecordedSettings();
      return;
    }

    orig(file, settings);
    if (settings)
      AfterSettingsSave();
  }

  private static void AfterSettingsSave()
  {
    if (RunManager.RealAttemptInProgress)
      RunManager.RestartAttemptAfterSave("climbtrials_notice_settings");
  }

  private static bool OnSetCheckpoint(
    On.Celeste.SaveData.orig_SetCheckpoint orig,
    SaveData self,
    AreaKey area,
    string level
  )
  {
    if (RunManager.Run != null)
      return false;
    return orig(self, area, level);
  }

  private static void OnAutoSave(On.Celeste.Level.orig_AutoSave orig, Level self)
  {
    if (RunManager.Run != null)
      return;
    orig(self);
  }

  private static void OnSummitCheckpointUpdate(
    On.Celeste.SummitCheckpoint.orig_Update orig,
    SummitCheckpoint self
  )
  {
    if (RunManager.Run != null && RunManager.Attempt.State == AttemptState.Finished)
      return;
    orig(self);
  }

  private static ScreenWipe OnCompleteArea(
    On.Celeste.Level.orig_CompleteArea_bool_bool_bool orig,
    Level self,
    bool spotlightWipe,
    bool skipScreenWipe,
    bool skipCompleteScreen
  )
  {
    if (RunManager.Run == null)
      return orig(self, spotlightWipe, skipScreenWipe, skipCompleteScreen);

    Logger.Info(
      LogTag,
      $"chapter end reached in '{RunManager.Trial?.Id}' room '{self.Session.Level}', "
        + "returning to trials"
    );
    RunManager.LeaveToTrials();
    return null;
  }

  // Unsure if this ever ends up mattering, but added just in case.
  // Based on CelesteTAS (Source/EverestInterop/DesyncFixer.cs FixFinalBossDesync)
  private static void OnFinalBossAdded(
    On.Celeste.FinalBoss.orig_Added orig,
    FinalBoss self,
    Monocle.Scene scene
  )
  {
    orig(self, scene);
    self.Add(
      new PostUpdateHook(() =>
      {
        if (RunManager.Run == null || self.Sprite == null)
          return;
        self.Sprite.Scale.X = self.facing;
        self.Sprite.Scale.Y = 1f;
        self.Sprite.Scale *= 1f + self.scaleWiggler.Value * 0.2f;
      })
    );
  }
}
