using System;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class TextFit
{
  public static float Scale(string text, float maxWidth, float floor = 0f)
  {
    float width = ActiveFont.Measure(text).X;
    return width <= maxWidth ? 1f : Math.Max(floor, maxWidth / width);
  }
}
