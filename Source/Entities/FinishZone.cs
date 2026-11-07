using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class FinishZone : Entity
{
  private readonly Rectangle worldRect;
  private FinishZoneDisplay display;

  public FinishZone(Rectangle worldRect)
  {
    this.worldRect = worldRect;
    Position = worldRect.Location.ToVector2();
    Collider = new Hitbox(worldRect.Width, worldRect.Height, 0f, 0f);
    Add(new PlayerCollider(OnPlayer));
    Depth = Depths.Top;
    Visible = false;
  }

  public override void Added(Scene scene)
  {
    base.Added(scene);
    display = new FinishZoneDisplay(worldRect);
    scene.Add(display);
  }

  public override void Removed(Scene scene)
  {
    base.Removed(scene);
    display?.RemoveSelf();
  }

  private void OnPlayer(Player player)
  {
    if (!RunManager.Finish())
      return;
    Level level = SceneAs<Level>();
    FinishSequence.Confetti(level, player);
    FinishSequence.Begin(level, player);
  }

  public override void Update()
  {
    base.Update();
    if (RunManager.Run == null)
      RemoveSelf();
  }
}
