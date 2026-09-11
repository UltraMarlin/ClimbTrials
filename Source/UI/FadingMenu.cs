using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public abstract class FadingMenu : TextMenu
{
  private const float BackdropAlpha = 0.6f;
  private const float FadeSpeed = 5f;

  private float fade;
  private bool closing;

  public float Fade => fade;
  public bool Closing => closing;
  public bool Closed => closing && fade <= 0f;

  protected FadingMenu()
  {
    Alpha = 0f;
    AutoScroll = false;
  }

  public void Dismiss()
  {
    Focused = false;
    closing = true;
  }

  public override void Update()
  {
    base.Update();
    fade = Calc.Approach(fade, closing ? 0f : 1f, FadeSpeed * Engine.RawDeltaTime);
    Alpha = fade;
  }

  public override void Render()
  {
    Draw.Rect(-1f, -1f, 1922f, 1082f, Color.Black * (BackdropAlpha * fade));
    base.Render();
  }
}
