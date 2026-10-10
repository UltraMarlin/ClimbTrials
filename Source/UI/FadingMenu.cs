using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public abstract class FadingMenu : TextMenu
{
  private const float BackdropAlpha = 0.6f;
  private const float FadeSpeed = 5f;
  private const float UnderDim = 0.05f;
  private const float ReasonGap = 40f;
  private const float ReasonScale = 0.6f;

  private float fade;
  private bool closing;

  public float Fade => fade;
  public float DimUnder => 1f - (1f - UnderDim) * fade;
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

  protected void ReadKeys(MenuKeys keys, Action back)
  {
    if (keys.Confirm && Current != null)
    {
      Current.ConfirmPressed();
      Current.OnPressed?.Invoke();
    }
    else if (keys.Back)
      back();
    else if (keys.StepUp(this))
      MoveSelection(-1, wiggle: true);
    else if (keys.StepDown(this))
      MoveSelection(1, wiggle: true);
  }

  protected void DrawReason(string text)
  {
    Vector2 at = Position + new Vector2(0f, Height / 2f + ReasonGap);
    TextOutline.DrawDim(text, at, new Vector2(0.5f, 0.5f), ReasonScale, Alpha);
  }
}
