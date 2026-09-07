using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public readonly record struct TimeStyle(
  float Scale,
  float DigitGap,
  float Stroke,
  float FractionScale
)
{
  public TimeStyle At(float scale) => this with { Scale = scale };
}

public static class TimeText
{
  public static readonly TimeStyle Hud = new(1f, 4f, 2f, 0.7f);
  public static readonly TimeStyle Large = new(2.8f, 0f, 5f, 0.7f);

  private static float numberWidth;
  private static float spacerWidth;
  private static float numberHeight;

  private static void MeasureFontWidth()
  {
    if (numberWidth > 0f)
      return;
    PixelFontSize size = Dialog
      .Languages["english"]
      .Font.Get(Dialog.Languages["english"].FontFaceSize);
    numberWidth = "0123456789".Max(c => size.Measure(c.ToString()).X);
    spacerWidth = size.Measure(":").X;
    numberHeight = size.Measure("0").Y;
  }

  private static float Advance(char c, TimeStyle style) =>
    (char.IsDigit(c) ? numberWidth : spacerWidth) + style.DigitGap;

  public static float Width(string text, TimeStyle style)
  {
    MeasureFontWidth();
    float width = 0f;
    float charScale = 1f;
    foreach (char c in text)
    {
      if (c == '.')
        charScale = style.FractionScale;
      width += Advance(c, style) * charScale;
    }
    return width * style.Scale;
  }

  public static float Height(TimeStyle style)
  {
    MeasureFontWidth();
    return numberHeight * style.Scale;
  }

  public static Color StrokeFor(Color color) => Color.Black * (color.A / 255f);

  public static void Draw(
    Vector2 baseline,
    string text,
    TimeStyle style,
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
        charScale = style.FractionScale;
        y -= 5f * style.Scale;
      }
      float w = Advance(c, style) * charScale * style.Scale;
      bool separator = c is ':' or '.';
      font.DrawOutline(
        faceSize,
        c.ToString(),
        new Vector2(x + w / 2f, y),
        new Vector2(0.5f, 1f),
        Vector2.One * charScale * style.Scale,
        separator ? separators : digits,
        style.Stroke,
        StrokeFor(separator ? separators : digits)
      );
      x += w;
    }
  }
}
