using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class TextOutline
{
  public const float DimStroke = 1.7f;

  public static Color StrokeFor(Color color)
  {
    float alpha = color.A / 255f;
    return Color.Black * (alpha * alpha);
  }

  public static void Draw(
    string text,
    Vector2 position,
    Vector2 justify,
    float scale,
    Color color
  ) =>
    ActiveFont.DrawOutline(
      text,
      position,
      justify,
      Vector2.One * scale,
      color,
      2f,
      StrokeFor(color)
    );

  public static void DrawDim(
    string text,
    Vector2 position,
    Vector2 justify,
    float scale,
    float alpha
  ) => DrawDim(text, position, justify, scale, Colors.DimText * alpha);

  public static void DrawDim(
    string text,
    Vector2 position,
    Vector2 justify,
    float scale,
    Color color
  )
  {
    ActiveFont.DrawOutline(
      text,
      position,
      justify,
      Vector2.One * scale,
      color,
      DimStroke,
      StrokeFor(color)
    );
  }

  public static void DrawRoundOutline(
    string text,
    Vector2 position,
    Vector2 justify,
    Vector2 scale,
    Color color,
    float stroke,
    Color strokeColor
  ) =>
    DrawRoundOutline(
      ActiveFont.Font,
      ActiveFont.BaseSize,
      text,
      position,
      justify,
      scale,
      color,
      stroke,
      strokeColor
    );

  public static void DrawRoundOutline(
    PixelFont font,
    float baseSize,
    string text,
    Vector2 position,
    Vector2 justify,
    Vector2 scale,
    Color color,
    float stroke,
    Color strokeColor
  )
  {
    const int Copies = 16;
    for (int i = 0; i < Copies; i++)
    {
      Vector2 offset = Calc.AngleToVector(i * MathF.Tau / Copies, stroke);
      font.Draw(baseSize, text, position + offset, justify, scale, strokeColor);
    }
    font.Draw(baseSize, text, position, justify, scale, color);
  }
}
