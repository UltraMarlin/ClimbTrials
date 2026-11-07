using Microsoft.Xna.Framework;

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
    TextOutline.Draw(text, center, new Vector2(0.5f, 0.5f), RowLayout.RankScale * fit, color);
  }
}
