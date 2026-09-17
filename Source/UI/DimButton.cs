using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.UI;

public class DimButton : TextMenu.Button
{
  private readonly Func<bool> live;

  public DimButton(string label, bool live, Action onPressed)
    : this(label, () => live, onPressed) { }

  public DimButton(string label, Func<bool> live, Action onPressed)
    : base(label)
  {
    this.live = live;
    Pressed(() =>
    {
      if (live())
        onPressed();
    });
  }

  public override void Update()
  {
    base.Update();
    ConfirmSfx = live() ? SFX.ui_main_button_select : SFX.ui_main_button_invalid;
  }

  public override void Render(Vector2 position, bool highlighted)
  {
    if (live())
    {
      base.Render(position, highlighted);
      return;
    }

    Color color = Color.DarkSlateGray * Container.Alpha;
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
