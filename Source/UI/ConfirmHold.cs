using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class ConfirmHold : Entity
{
  private const float HoldSeconds = 1.5f;
  private const int DepthAboveMenus = -100;

  private readonly TextMenu menu;
  private readonly string heldSound;
  private readonly Action onHeld;
  private float secondsHeld;

  private ConfirmHold(TextMenu menu, string heldSound, Action onHeld)
  {
    this.menu = menu;
    this.heldSound = heldSound;
    this.onHeld = onHeld;
    Tag = Tags.HUD;
    Depth = DepthAboveMenus;
  }

  public static void Start(TextMenu menu, string heldSound, Action onHeld)
  {
    menu.RenderAsFocused = true;
    menu.Focused = false;
    menu.Scene.Add(new ConfirmHold(menu, heldSound, onHeld));
  }

  public override void Update()
  {
    base.Update();
    secondsHeld += Engine.RawDeltaTime;
    bool held = secondsHeld > HoldSeconds;
    bool released = !Input.MenuConfirm.Check && secondsHeld > 0.3f;
    if (!held && !released)
      return;

    menu.Focused = true;
    RemoveSelf();
    Audio.Play(held ? heldSound : SFX.ui_main_button_invalid);
    if (held)
      onHeld();
  }

  public override void Render()
  {
    float fade = Ease.CubeInOut(Math.Min(1f, secondsHeld / 0.2f));
    float filled = Ease.SineOut(Math.Min(1f, secondsHeld / HoldSeconds));
    Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * 0.95f * fade);
    Draw.Rect(720f, 530f, 480f, 20f, Color.White * 0.25f * fade);
    Draw.Rect(720f, 530f, 480f * filled, 20f, Color.White * fade);
  }
}
