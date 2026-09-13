using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public readonly record struct TimeStyle(
  float Scale,
  float DigitGap,
  float Stroke,
  float FractionScale,
  bool RoundStroke = false
)
{
  public TimeStyle At(float scale) => this with { Scale = scale };
}

public static class TimeText
{
  public static readonly TimeStyle Hud = new(1f, 4f, 2f, 0.7f);
  public static readonly TimeStyle Large = new(2.8f, 0f, 3.5f, 0.7f, RoundStroke: true);

  private static float numberWidth;
  private static float spacerWidth;
  private static float numberHeight;

  public static string Format(long ticks)
  {
    TimeSpan time = TimeSpan.FromTicks(ticks);
    return time.ToString(time.TotalHours >= 1 ? "h\\:mm\\:ss\\.fff" : "m\\:ss\\.fff");
  }

  public static string FormatDate(DateTime utc) => utc.ToLocalTime().ToString("yyyy-MM-dd");

  public static string FormatSeconds(int seconds)
  {
    TimeSpan time = TimeSpan.FromSeconds(seconds);
    return time.ToString(time.TotalHours >= 1 ? "h\\:mm\\:ss" : "m\\:ss");
  }

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

  public static void Draw(Vector2 baseline, string text, TimeStyle style, Color color)
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
      Vector2 position = new(x + w / 2f, y);
      Vector2 justify = new(0.5f, 1f);
      Vector2 scale = Vector2.One * charScale * style.Scale;
      Color stroke = TextOutline.StrokeFor(color);
      if (style.RoundStroke)
        TextOutline.DrawRoundOutline(
          font,
          faceSize,
          c.ToString(),
          position,
          justify,
          scale,
          color,
          style.Stroke,
          stroke
        );
      else
        font.DrawOutline(
          faceSize,
          c.ToString(),
          position,
          justify,
          scale,
          color,
          style.Stroke,
          stroke
        );
      x += w;
    }
  }
}
