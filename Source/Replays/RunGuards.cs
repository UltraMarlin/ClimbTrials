using System.Collections;
using Celeste.Mod.ClimbTrials.Runs;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class RunGuards
{
  public static bool RenderFixes = true;

  public static void Load()
  {
    On.Celeste.UserIO.SaveHandler += OnSaveHandler;
    On.Celeste.UserIO.SaveHandlerLegacy += OnSaveHandlerLegacy;
    On.Celeste.FinalBoss.Added += OnFinalBossAdded;
  }

  public static void Unload()
  {
    On.Celeste.UserIO.SaveHandler -= OnSaveHandler;
    On.Celeste.UserIO.SaveHandlerLegacy -= OnSaveHandlerLegacy;
    On.Celeste.FinalBoss.Added -= OnFinalBossAdded;
  }

  private static void OnSaveHandler(
    On.Celeste.UserIO.orig_SaveHandler orig,
    bool file,
    bool settings
  )
  {
    if (Replayer.Feeding && settings)
    {
      Replayer.UnpinSettings();
      orig(file, settings);
      Replayer.PinSettings();
      return;
    }
    orig(file, settings);
    if (settings)
      AfterSettingsSave();
  }

  private static IEnumerator OnSaveHandlerLegacy(
    On.Celeste.UserIO.orig_SaveHandlerLegacy orig,
    bool file,
    bool settings
  )
  {
    IEnumerator routine = orig(file, settings);
    if (settings)
      AfterSettingsSave();
    return routine;
  }

  private static void AfterSettingsSave()
  {
    if (RunManager.AttemptInProgress && !RunManager.Replaying)
      RunManager.RestartAttempt("climbtrials_notice_settings");
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
        if (!RenderFixes || RunManager.Run == null || self.Sprite == null)
          return;
        self.Sprite.Scale.X = self.facing;
        self.Sprite.Scale.Y = 1f;
        self.Sprite.Scale *= 1f + self.scaleWiggler.Value * 0.2f;
      })
    );
  }
}
