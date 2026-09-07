using Celeste.Mod.ClimbTrials.Content;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class MedalIcon
{
  private const float TextBrighten = 0.25f;
  private const float SeparatorDim = 0.7f;

  public static Color Tint(Medal medal) =>
    medal switch
    {
      Medal.Bronze => Calc.HexToColor("ab5e24"),
      Medal.Silver => Calc.HexToColor("85aabf"),
      Medal.Gold => Calc.HexToColor("facb32"),
      Medal.Grandmaster => Calc.HexToColor("ff54b5"),
      _ => Color.White,
    };

  public static Color TextColor(Medal medal) =>
    medal == Medal.None ? Color.White : Color.Lerp(Tint(medal), Color.White, TextBrighten);

  public static Color SeparatorColor(Medal medal) =>
    medal == Medal.None ? Color.LightGray : TextColor(medal) * SeparatorDim;

  public static void Draw(Vector2 center, Medal medal, float height, float alpha = 1f)
  {
    MTexture texture = GFX.Gui["collectables/heartgem/3/FakeHeart0000"];
    float scale = height / texture.Height;
    texture.DrawCentered(center, Tint(medal) * alpha, Vector2.One * scale);
  }
}
