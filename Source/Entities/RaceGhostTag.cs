using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

public class RaceGhostTag : Entity
{
  private const float AlphaMultiplier = 0.5f;
  private readonly RaceGhost ghost;

  public RaceGhostTag(RaceGhost ghost)
  {
    this.ghost = ghost;
    Tag = Tags.HUD | Tags.Persistent;
  }

  public override void Render()
  {
    if (!ghost.Visible || string.IsNullOrEmpty(ghost.Name) || Scene is not Level level)
      return;
    Vector2 at = level.WorldToScreen(ghost.Position - Vector2.UnitY * RaceGhost.TagLift);
    ActiveFont.Draw(
      ghost.Name,
      at,
      new Vector2(0.5f, 1f),
      Vector2.One * RaceGhost.TagScale,
      Color.White * ghost.Alpha * ghost.Alpha * AlphaMultiplier
    );
  }
}
