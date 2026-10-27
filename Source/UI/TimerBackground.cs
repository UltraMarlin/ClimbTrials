using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class TimerBackground
{
  public const string Texture = "strawberryCountBG";

  public static void Draw(float top, float height, float width)
  {
    MTexture bg = GFX.Gui[Texture];
    float scale = height / bg.Height;
    float left = width - bg.Width * scale;
    if (left > 0f)
      Monocle.Draw.Rect(0f, top, left + 2f, height, Color.Black);
    bg.Draw(new Vector2(left, top), Vector2.Zero, Color.White, scale);
  }
}
