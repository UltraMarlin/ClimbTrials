using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

public class GhostTags : Entity
{
  private const float OutlineStroke = 2f;
  private readonly GhostLayer ghosts;

  public GhostTags(GhostLayer ghosts)
  {
    this.ghosts = ghosts;
    Tag = Tags.HUD | Tags.Persistent;
  }

  public override void Render()
  {
    if (Scene is not Level level)
      return;
    foreach (PlaybackGhost ghost in ghosts.Ghosts)
    {
      if (!ghost.Visible || string.IsNullOrEmpty(ghost.Name))
        continue;
      Vector2 at = level.WorldToScreen(ghost.Position - Vector2.UnitY * PlaybackGhost.TagLift);
      Vector2 justify = new(0.5f, 1f);
      Vector2 scale = Vector2.One * PlaybackGhost.TagScale;
      Color color = ghost.TagColor * ghost.TagAlpha;
      if (ghost.TagOutline is Color outline)
      {
        Color stroke = outline * ghost.TagAlpha;
        ActiveFont.DrawOutline(ghost.Name, at, justify, scale, color, OutlineStroke, stroke);
      }
      else
        ActiveFont.Draw(ghost.Name, at, justify, scale, color);
    }
  }
}
