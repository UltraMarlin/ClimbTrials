using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.UI;

public class Gap(float height) : TextMenu.Item
{
  public override float Height() => height;

  public override void Render(Vector2 position, bool highlighted) { }
}
