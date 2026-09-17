using Celeste.Mod.ClimbTrials.Content;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class MedalIcon
{
  private const float TextBrighten = 0.25f;

  public static Color Tint(Medal medal) =>
    medal switch
    {
      Medal.Bronze => Calc.HexToColor("ab5e24"),
      Medal.Silver => Calc.HexToColor("85aabf"),
      Medal.Gold => Calc.HexToColor("facb32"),
      Medal.Diamond => Calc.HexToColor("12c8ff"),
      Medal.Grandmaster => Calc.HexToColor("ff54b5"),
      _ => Color.White,
    };

  public static Color TextColor(Medal medal) =>
    medal == Medal.None ? Color.White : Color.Lerp(Tint(medal), Color.White, TextBrighten);

  public readonly record struct Burst(int Count, int ShineCount, float SpeedMax, float LifeMax);

  public static Burst PickBurst(Medal medal) =>
    medal switch
    {
      Medal.Silver => new Burst(28, 10, 180f, 1.0f),
      Medal.Gold => new Burst(40, 18, 220f, 1.3f),
      Medal.Diamond => new Burst(52, 30, 245f, 1.5f),
      Medal.Grandmaster => new Burst(72, 32, 270f, 1.8f),
      _ => new Burst(0, 0, 0f, 0f),
    };

  public static Color Shine(Medal medal) => Color.Lerp(Tint(medal), Color.White, 0.4f);

  public static void Draw(Vector2 center, Medal medal, float height, float alpha = 1f)
  {
    MTexture texture = GFX.Gui["collectables/heartgem/3/FakeHeart0000"];
    float scale = height / texture.Height;
    texture.DrawCentered(center, Tint(medal) * alpha, Vector2.One * scale);
  }
}
