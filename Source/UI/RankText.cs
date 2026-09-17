using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class RankText
{
  public static string Format(int rank) => $"#{rank}";

  private const string WidestOnMedal = "#400";

  public static void Draw(int? rank, Vector2 center, float alpha)
  {
    if (rank is not int place)
      return;

    string text = Format(place);
    float fit = TextFit.Scale(text, ActiveFont.Measure(WidestOnMedal).X);
    Color color = Color.White * alpha;
    ActiveFont.DrawOutline(
      text,
      center,
      new Vector2(0.5f, 0.5f),
      Vector2.One * (RowLayout.RankScale * fit),
      color,
      2f,
      TextOutline.StrokeFor(color)
    );
  }
}
