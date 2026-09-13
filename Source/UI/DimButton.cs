using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.UI;

public class DimButton : TextMenu.Button
{
  private readonly bool live;

  public DimButton(string label, bool live, Action onPressed)
    : base(label)
  {
    this.live = live;
    if (live)
      Pressed(onPressed);
    else
      ConfirmSfx = "event:/ui/main/button_invalid";
  }

  public override void Render(Vector2 position, bool highlighted)
  {
    if (live)
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
