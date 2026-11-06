using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.UI;

public static class NewMark
{
  private const float Scale = 0.8f;
  private const float Gap = 16f;

  public static void Draw(Vector2 textEnd, float alpha) =>
    GFX.Gui["areas/new"]
      .DrawJustified(
        textEnd + new Vector2(Gap, 0f),
        new Vector2(0f, 0.5f),
        Color.White * alpha,
        Scale
      );
}
