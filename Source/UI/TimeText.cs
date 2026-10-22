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
  public static readonly TimeStyle SmallHud = new(0.4f, 1f, 1.35f, 0.85f);
  public static readonly TimeStyle Hud = new(1f, 4f, 2f, 0.7f);
  public static readonly TimeStyle Large = new(2.8f, 0f, 3.5f, 0.7f, RoundStroke: true);
  public const float SignGap = 4f;
  public const float LineBelowBaseline = 16f;
  public const string TypicalTime = "0:00.000";

  private static float numberWidth;
  private static float spacerWidth;
  private static float signWidth;
  private static float numberHeight;

  public static string Format(long ticks)
  {
    TimeSpan time = TimeSpan.FromTicks(ticks);
    return time.ToString(time.TotalHours >= 1 ? "h\\:mm\\:ss\\.fff" : "m\\:ss\\.fff");
  }

  public static string FormatDelta(long ticks)
  {
    char sign = ticks < 0 ? '-' : '+';
    TimeSpan time = TimeSpan.FromTicks(Math.Abs(ticks));
    if (time.TotalMinutes >= 1)
      return sign + Format(Math.Abs(ticks));
    return $"{sign}{(int)time.TotalSeconds}.{time.Milliseconds:000}";
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
    signWidth = Math.Max(size.Measure("+").X, size.Measure("-").X);
    numberHeight = size.Measure("0").Y;
  }

  private static bool IsSign(char c) => c is '-' or '+';

  private static float Advance(char c, TimeStyle style)
  {
    if (IsSign(c))
      return signWidth + style.DigitGap + SignGap;
    return (char.IsDigit(c) ? numberWidth : spacerWidth) + style.DigitGap;
  }

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

  public static void DrawHidden(Vector2 baseline, TimeStyle style, Color color) =>
    Draw(baseline, TypicalTime, style, color, digitsAsDashes: true);

  public static void Draw(
    Vector2 baseline,
    string text,
    TimeStyle style,
    Color color,
    bool digitsAsDashes = false
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
      bool sign = IsSign(c);
      Vector2 position = sign
        ? new(x + w - SignGap * charScale * style.Scale, y)
        : new(x + w / 2f, y);
      Vector2 justify = sign ? new(1f, 1f) : new(0.5f, 1f);
      Vector2 scale = Vector2.One * charScale * style.Scale;
      Color stroke = TextOutline.StrokeFor(color);
      string glyph = digitsAsDashes && char.IsDigit(c) ? "-" : c.ToString();
      if (style.RoundStroke)
        TextOutline.DrawRoundOutline(
          font,
          faceSize,
          glyph,
          position,
          justify,
          scale,
          color,
          style.Stroke,
          stroke
        );
      else
        font.DrawOutline(faceSize, glyph, position, justify, scale, color, style.Stroke, stroke);
      x += w;
    }
  }
}
