using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class FinishZone : Entity
{
  private const string FinishSound = "event:/game/07_summit/checkpoint_confetti";
  private readonly Rectangle worldRect;
  private FinishZoneDisplay display;

  public FinishZone(Rectangle worldRect)
  {
    this.worldRect = worldRect;
    Position = new Vector2(worldRect.X, worldRect.Y);
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
    player.StateMachine.State = Player.StDummy;
    Audio.Play(FinishSound, TopCenter);
    SummitCheckpoint.ConfettiRenderer confetti = new(TopCenter);
    confetti.Tag |= Tags.FrozenUpdate | Tags.Persistent;
    Scene.Add(confetti);
    if (RunManager.LastFinish != null)
      Scene.Add(new FinishSequence(RunManager.LastFinish));
  }

  public override void Update()
  {
    base.Update();
    if (RunManager.Run == null)
      RemoveSelf();
  }
}
