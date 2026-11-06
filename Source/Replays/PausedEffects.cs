using Monocle;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class PausedEffects
{
  public static void Load()
  {
    On.Celeste.DisplacementRenderer.Burst.Render += OnBurstRender;
    On.Celeste.Glitch.Apply += OnGlitchApply;
    On.Celeste.Level.Render += OnLevelRender;
  }

  public static void Unload()
  {
    On.Celeste.DisplacementRenderer.Burst.Render -= OnBurstRender;
    On.Celeste.Glitch.Apply -= OnGlitchApply;
    On.Celeste.Level.Render -= OnLevelRender;
  }

  private static bool Hidden =>
    Replayer.State == ReplayState.Paused
    && ClimbTrialsModule.Settings.HideVfxWhenPaused switch
    {
      HideVfxMode.Always => true,
      HideVfxMode.WithHitboxes => GameplayRenderer.RenderDebug,
      _ => false,
    };

  private static void OnBurstRender(
    On.Celeste.DisplacementRenderer.Burst.orig_Render orig,
    DisplacementRenderer.Burst self
  )
  {
    if (!Hidden)
      orig(self);
  }

  private static void OnGlitchApply(
    On.Celeste.Glitch.orig_Apply orig,
    VirtualRenderTarget source,
    float timer,
    float seed,
    float amplitude
  )
  {
    if (!Hidden)
      orig(source, timer, seed, amplitude);
  }

  private static void OnLevelRender(On.Celeste.Level.orig_Render orig, Level self)
  {
    if (!Hidden)
    {
      orig(self);
      return;
    }
    float flash = self.flash;
    self.flash = 0f;
    try
    {
      orig(self);
    }
    finally
    {
      self.flash = flash;
    }
  }
}
