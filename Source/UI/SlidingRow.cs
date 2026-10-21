using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public abstract class SlidingRow : TextMenu.Item
{
  private float ease;

  protected SlidingRow()
  {
    Selectable = true;
  }

  public override float Height() => RowLayout.RowHeight;

  public override void Update()
  {
    bool selected = Container.Focused && Container.Current == this;
    ease = Calc.Approach(ease, selected ? 1f : 0f, 6f * Engine.DeltaTime);
  }

  protected Vector2 RowStart(Vector2 position) =>
    position + new Vector2(Ease.CubeInOut(ease) * RowLayout.HoveredRowShift, 0f);
}
