using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public abstract class MenuOnTop : TextMenu
{
  private const float ReasonGap = 40f;
  private const float ReasonScale = 0.6f;

  private readonly bool inLevel;
  private float timer;
  private float rows;
  private float backdrop;
  private bool closing;
  private bool handedOver;

  public float DimUnder =>
    inLevel
      ? Motion.DimUnder(rows, Motion.LevelMenuUnderDim)
      : Motion.DimUnder(backdrop, Motion.MenuUnderDim);
  public bool Closing => closing;
  public bool Closed => closing && timer >= Duration;

  public bool HandedOver => handedOver;

  public float DrawnBackdrop =>
    (inLevel ? Motion.LevelMenuBackdropAlpha : Motion.MenuBackdropAlpha) * backdrop;

  private float Duration => inLevel ? Motion.LevelBackdropDuration : Motion.MenuCover.Duration;

  protected MenuOnTop(bool inLevel)
  {
    this.inLevel = inLevel;
    Alpha = 0f;
    AutoScroll = false;
  }

  public void Dismiss()
  {
    if (closing)
      return;
    Focused = false;
    closing = true;
    timer = inLevel ? Duration * (1f - backdrop) : Duration - timer;
  }

  public float HandOver()
  {
    Focused = false;
    handedOver = true;
    Alpha = 0f;
    return backdrop;
  }

  public void Release()
  {
    Visible = false;
    RemoveSelf();
  }

  public override void Update()
  {
    base.Update();
    if (handedOver)
      return;
    timer = Math.Min(timer + Engine.RawDeltaTime, Duration);
    if (inLevel)
    {
      float progress = timer / Duration;
      backdrop = closing ? 1f - progress : progress;
      rows = closing ? 0f : 1f;
    }
    else
    {
      float along = closing ? Duration - timer : timer;
      backdrop = Motion.MenuCover.Leaving(along);
      rows = Motion.MenuCover.Arriving(along);
    }
    Alpha = rows;
  }

  public override void Render()
  {
    Draw.Rect(-1f, -1f, 1922f, 1082f, Color.Black * DrawnBackdrop);
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
