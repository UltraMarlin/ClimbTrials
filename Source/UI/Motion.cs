using System;
using System.Collections;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class Motion
{
  public static readonly Transition ScreenSlide = new(0.35f, 0.2f, 0.2f);
  public const float SlideDistance = 120f;
  public static readonly Transition PageTurn = new(0.2f, 0.12f, 0.12f);
  public const float PageDistance = 60f;
  public static readonly Transition BoardCover = new(0.35f, 0.2f, 0.2f);
  public static readonly Transition MenuCover = new(0.25f, 0.15f, 0.15f);
  public static readonly Transition LevelBoardOpen = new(0.25f, 0.25f, 0.25f);
  public const float OpenRise = 40f;
  public const float LevelBackdropDuration = 1f / 8f;
  public const float MenuBackdropAlpha = 0.4f;
  public const float MenuUnderDim = 0.03f;
  public const float BoardBackdropAlpha = 0.3f;
  public const float BoardUnderDim = 0f;
  public const float LevelMenuBackdropAlpha = 0.6f;
  public const float LevelMenuUnderDim = 0.05f;
  public const float LevelBoardBackdropAlpha = 0.7f;

  public static float Rise(float arrived) => OpenRise * (1f - arrived);

  public static float DimUnder(float covered, float floor) => 1f - (1f - floor) * covered;

  public static IEnumerator LeaveScreen(
    Entity host,
    float side,
    Action<float, float> place,
    Action done
  )
  {
    Transition slide = ScreenSlide;
    float elapsed = 0f;
    for (; elapsed < slide.InStart; elapsed += Engine.RawDeltaTime)
    {
      PlaceLeaving(slide, elapsed, side, place);
      yield return null;
    }
    host.Add(new Coroutine(FinishLeaving(slide, elapsed, side, place, done)));
  }

  private static IEnumerator FinishLeaving(
    Transition slide,
    float elapsed,
    float side,
    Action<float, float> place,
    Action done
  )
  {
    for (; elapsed < slide.OutSeconds; elapsed += Engine.RawDeltaTime)
    {
      PlaceLeaving(slide, elapsed, side, place);
      yield return null;
    }
    done();
  }

  private static void PlaceLeaving(
    Transition slide,
    float elapsed,
    float side,
    Action<float, float> place
  ) => place(side * SlideDistance * slide.LeavingMove(elapsed), 1f - slide.Leaving(elapsed));

  public static IEnumerator EnterScreen(float side, Action<float, float> place)
  {
    Transition slide = ScreenSlide;
    for (float elapsed = slide.InStart; elapsed < slide.Duration; elapsed += Engine.RawDeltaTime)
    {
      place(side * SlideDistance * (1f - slide.ArrivingMove(elapsed)), slide.Arriving(elapsed));
      yield return null;
    }
  }

  public static IEnumerator Slide(
    Transition transition,
    int direction,
    float distance,
    Action<float, float, float, float> place
  )
  {
    for (float elapsed = 0f; elapsed < transition.Duration; elapsed += Engine.RawDeltaTime)
    {
      place(
        -direction * distance * transition.LeavingMove(elapsed),
        1f - transition.Leaving(elapsed),
        direction * distance * (1f - transition.ArrivingMove(elapsed)),
        transition.Arriving(elapsed)
      );
      yield return null;
    }
    place(-direction * distance, 0f, 0f, 1f);
  }
}

public readonly record struct Transition(float Duration, float OutSeconds, float InSeconds)
{
  public float InStart => Duration - InSeconds;

  public float Leaving(float elapsed) => Gentle(OutProgress(elapsed));

  public float Arriving(float elapsed) => Gentle(InProgress(elapsed));

  public float LeavingMove(float elapsed) => Ease.CubeIn(OutProgress(elapsed));

  public float ArrivingMove(float elapsed) => Ease.CubeOut(InProgress(elapsed));

  private float OutProgress(float elapsed) => Math.Clamp(elapsed / OutSeconds, 0f, 1f);

  private float InProgress(float elapsed) => Math.Clamp((elapsed - InStart) / InSeconds, 0f, 1f);

  private static float Gentle(float ease) => (ease + Ease.SineInOut(ease)) / 2f;
}
