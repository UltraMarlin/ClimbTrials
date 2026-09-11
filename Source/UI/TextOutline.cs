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

  public static void DrawDim(
    string text,
    Vector2 position,
    Vector2 justify,
    float scale,
    float alpha
  )
  {
    Color color = Colors.DimText * alpha;
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
  )
  {
    const int Copies = 16;
    for (int i = 0; i < Copies; i++)
    {
      Vector2 offset = Calc.AngleToVector(i * MathF.Tau / Copies, stroke);
      ActiveFont.Draw(text, position + offset, justify, scale, strokeColor);
    }
    ActiveFont.Draw(text, position, justify, scale, color);
  }
}
