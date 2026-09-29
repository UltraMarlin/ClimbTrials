using System;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class TextFit
{
  public const int NameFullChars = 10;

  public static float Scale(string text, float maxWidth, float floor = 0f)
  {
    float width = ActiveFont.Measure(text).X;
    return width <= maxWidth ? 1f : Math.Max(floor, maxWidth / width);
  }

  public static float FitName(string line, string name, float floor)
  {
    if (name.Length <= NameFullChars)
      return 1f;
    float capped = ActiveFont.Measure(line.Replace("{0}", name[..NameFullChars])).X;
    return Scale(line.Replace("{0}", name), capped, floor);
  }
}
