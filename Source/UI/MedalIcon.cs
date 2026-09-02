using Celeste.Mod.ClimbTrials.Content;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class MedalIcon
{
  public static Color Tint(Medal medal) =>
    medal switch
    {
      Medal.Bronze => Calc.HexToColor("cd7532"),
      Medal.Silver => Calc.HexToColor("d3d9ef"),
      Medal.Gold => Calc.HexToColor("ead70a"),
      Medal.Grandmaster => Calc.HexToColor("ff56c8"),
      _ => Color.White,
    };

  public static void Draw(Vector2 center, Medal medal, float height, float alpha = 1f)
  {
    MTexture texture = GFX.Gui["collectables/heartgem/3/FakeHeart0000"];
    float scale = height / texture.Height;
    texture.DrawCentered(center, Tint(medal) * alpha, Vector2.One * scale);
  }
}
