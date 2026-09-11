using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public readonly record struct ChapterTint(Color Dark, Color Light);

public static class Colors
{
  public static readonly Color Warning = Calc.HexToColor("f0777a");
  public static readonly Color DimText = Color.LightGray;

  private static readonly Dictionary<int, ChapterTint> Chapters = new()
  {
    [0] = Tint("060c11", "acb0a4"), //  Prologue
    [1] = Tint("060c11", "acb0a4"), //  Forsaken City
    [2] = Tint("05070e", "6a8480"), //  Old Site
    [3] = Tint("0a0006", "e0a24e"), //  Celestial Resort
    [4] = Tint("100207", "c684d9"), //  Golden Ridge
    [5] = Tint("05070e", "ad57a1"), //  Mirror Temple
    [6] = Tint("081453", "d1ecd2"), //  Reflection
    [7] = Tint("060c11", "acb0a4"), //  The Summit
    [8] = Tint("060c11", "acb0a4"), //  Epilogue
    [9] = Tint("070705", "c6c1b4"), //  Core
    [10] = Tint("1e1d3d", "d0d3d9"), // Farewell
  };
  private static readonly ChapterTint DefaultTint = Tint("000000", "ffffff");

  public static ChapterTint ForChapter(AreaKey area) =>
    Chapters.TryGetValue(area.ID, out ChapterTint tint) ? tint : DefaultTint;

  private static ChapterTint Tint(string dark, string light) =>
    new(Calc.HexToColor(dark), Calc.HexToColor(light));
}
