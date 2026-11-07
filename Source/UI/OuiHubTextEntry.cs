using System;
using System.Collections;
using Celeste.Mod.Core;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiHubTextEntry : OuiModOptionString
{
  private const float LetterGap = 66f;
  private const float LetterYOffset = 4f;
  private const float LetterTilt = 7f;
  private const float LetterDelay = 0.35f;
  private const float GlideDuration = 1.35f;
  private const float LeanDuration = 1.3f;
  private const float GlideStartSideways = 670f;
  private const float GlideStartY = -90f;
  private const float GlideStartLean = 75f;
  private const float WobbleAngle = 1.5f;
  private const float WobbleDuration = 0.3f;
  private const float WobbleFrequency = 5f;
  private const float ExitDuration = 0.4f;
  private const float ExitLift = 340f;
  private const float ExitTurn = 60f;

  private readonly Wiggler wobble = Wiggler.Create(WobbleDuration, WobbleFrequency);
  private Vector2 letterRestPosition;
  private float letterStartTime;
  private float? leaveStartTime;

  public OuiHubTextEntry()
  {
    wobble.StartZero = true;
    Add(wobble);
  }

  public string Headline { get; set; }

  public MTexture Letter { get; set; }

  private bool OpenedByHub => Headline != null;

  public override IEnumerator Enter(Oui from)
  {
    letterStartTime = Scene.TimeActive + LetterDelay;
    leaveStartTime = null;
    OnValueChange += _ => wobble.Start();
    if (Letter != null)
      letterRestPosition = new Vector2(
        ListScreen.OnScreenX
          - ActiveFont.Measure(Headline).X * ListScreen.TitleScale / 2f
          - LetterGap
          - Letter.Width / 2f,
        ListScreen.TitleY + LetterYOffset
      );
    return base.Enter(from);
  }

  public override IEnumerator Leave(Oui next)
  {
    leaveStartTime = Scene.TimeActive;
    yield return new SwapImmediately(base.Leave(next));
    Headline = null;
    Letter = null;
  }

  public override void Update() => RunForDeviceInHand(base.Update);

  public override void Render()
  {
    RunForDeviceInHand(base.Render);
    if (!OpenedByHub)
      return;
    ActiveFont.DrawOutline(
      Headline,
      Position + new Vector2(ListScreen.OnScreenX, ListScreen.TitleY),
      new Vector2(0.5f, 0.5f),
      Vector2.One * ListScreen.TitleScale,
      Color.White,
      2f,
      TextOutline.StrokeFor(Color.White)
    );
    if (Letter != null && Scene.TimeActive >= letterStartTime)
      DrawLetter();
  }

  private void DrawLetter()
  {
    float glideSeconds = (leaveStartTime ?? Scene.TimeActive) - letterStartTime;
    float glideProgress = Math.Min(glideSeconds / GlideDuration, 1f);
    float leanProgress = Math.Min(glideSeconds / LeanDuration, 1f);
    Vector2 rest = letterRestPosition;
    Vector2 position = new(
      MathHelper.Lerp(rest.X - GlideStartSideways, rest.X, Ease.CubeOut(glideProgress)),
      MathHelper.Lerp(GlideStartY, rest.Y, Ease.SineOut(glideProgress))
    );
    float angleDegrees =
      GlideStartLean * (1f - Ease.BackOut(leanProgress)) - LetterTilt + WobbleAngle * wobble.Value;
    if (leaveStartTime.HasValue)
    {
      float leaveSeconds = Scene.TimeActive - leaveStartTime.Value;
      float exitEase = Ease.BackIn(Math.Min(leaveSeconds / ExitDuration, 1f));
      position.Y -= ExitLift * exitEase;
      angleDegrees += ExitTurn * exitEase;
    }
    Letter.DrawCentered(position, Color.White, 1f, MathHelper.ToRadians(angleDegrees));
  }

  private void RunForDeviceInHand(Action step)
  {
    bool typing = CoreModule.Settings.UseKeyboardForTextInput;
    if (OpenedByHub && MInput.ControllerHasFocus)
      CoreModule.Settings.UseKeyboardForTextInput = false;
    try
    {
      step();
    }
    finally
    {
      CoreModule.Settings.UseKeyboardForTextInput = typing;
    }
  }
}
