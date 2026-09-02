using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class TimeText
{
  private static float numberWidth;
  private static float spacerWidth;

  private static void MeasureFontWidth()
  {
    if (numberWidth > 0f)
      return;
    PixelFontSize size = Dialog
      .Languages["english"]
      .Font.Get(Dialog.Languages["english"].FontFaceSize);
    numberWidth = "0123456789".Max(c => size.Measure(c.ToString()).X);
    spacerWidth = size.Measure(":").X;
  }

  private static float Advance(char c) => (char.IsDigit(c) ? numberWidth : spacerWidth) + 4f;

  public static float Width(string text, float scale = 1f)
  {
    MeasureFontWidth();
    float width = 0f;
    float charScale = 1f;
    foreach (char c in text)
    {
      if (c == '.')
        charScale = 0.7f;
      width += Advance(c) * charScale;
    }
    return width * scale;
  }

  public static void Draw(
    Vector2 baseline,
    string text,
    float scale,
    Color digits,
    Color separators
  )
  {
    MeasureFontWidth();
    PixelFont font = Dialog.Languages["english"].Font;
    float faceSize = Dialog.Languages["english"].FontFaceSize;
    float x = baseline.X;
    float y = baseline.Y;
    float charScale = 1f;
    foreach (char c in text)
    {
      if (c == '.')
      {
        charScale = 0.7f;
        y -= 5f * scale;
      }
      float w = Advance(c) * charScale * scale;
      bool separator = c is ':' or '.';
      font.DrawOutline(
        faceSize,
        c.ToString(),
        new Vector2(x + w / 2f, y),
        new Vector2(0.5f, 1f),
        Vector2.One * charScale * scale,
        separator ? separators : digits,
        2f,
        Color.Black
      );
      x += w;
    }
  }
}
