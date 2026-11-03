using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.UI;

public class DimButton : TextMenu.Button
{
  protected readonly Func<bool> canAct;

  public DimButton(string label, bool canAct, Action onPressed)
    : this(label, () => canAct, onPressed) { }

  public DimButton(string label, Func<bool> canAct, Action onPressed)
    : base(label)
  {
    this.canAct = canAct;
    Pressed(() =>
    {
      if (canAct())
        onPressed();
    });
  }

  public override void Update()
  {
    base.Update();
    ConfirmSfx = canAct() ? SFX.ui_main_button_select : SFX.ui_main_button_invalid;
  }

  public override void Render(Vector2 position, bool highlighted)
  {
    if (canAct())
    {
      base.Render(position, highlighted);
      return;
    }

    DrawLabel(position, Color.DarkSlateGray * Container.Alpha);
  }

  protected void DrawLabel(Vector2 position, Color color)
  {
    bool leftAligned =
      Container.InnerContent == TextMenu.InnerContentMode.TwoColumn && !AlwaysCenter;
    Vector2 at = position + (leftAligned ? Vector2.Zero : new Vector2(Container.Width * 0.5f, 0f));
    Vector2 justify = leftAligned ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
    ActiveFont.DrawOutline(
      Label,
      at,
      justify,
      Vector2.One,
      color,
      2f,
      Color.Black * (Container.Alpha * Container.Alpha * Container.Alpha)
    );
  }
}
