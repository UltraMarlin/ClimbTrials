using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class WatcherOverlay
{
  private static Scene scene;

  public static Scene Scene => scene;

  public static void Begin() => scene ??= new Scene();

  public static void End()
  {
    scene?.End();
    scene = null;
  }

  public static void Add(Entity entity) => scene?.Add(entity);

  public static void Update(GameTime gameTime, bool engineSkipped)
  {
    Replayer.UseWatcherLanguage();
    if (engineSkipped)
      MainThreadHelper.Instance?.Update(gameTime);

    if (scene is Scene current)
    {
      MenuKeys.Watcher.Tick(Engine.RawDeltaTime);

      current.BeforeUpdate();
      current.Update();
      current.AfterUpdate();
    }
    Replayer.UseReplayLanguage();
  }

  public static void Render()
  {
    Replayer.UseWatcherLanguage();
    scene?.Entities.Render();
    Replayer.UseReplayLanguage();
  }
}
