using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Monocle;

namespace Celeste.Mod.ClimbTrials;

public static class HotReload
{
  public static void Recover()
  {
    if (!AssetReloadHelper.IsReloading)
      return;

    MainThreadHelper
      .Schedule(() =>
      {
        Scene replacement = AssetReloadHelper.ReturnToScene switch
        {
          Overworld => MainMenuEntry.RebuildOverworld(),
          Level level => RunManager.RestartFromSession(level.Session),
          _ => null,
        };
        if (replacement == null)
          return;

        AssetReloadHelper.ReturnToScene = replacement;
      })
      .AsTask();
  }
}
