using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

public static class RunHudLayout
{
  public const float TimerY = 60f;
  public const float TimerBackgroundHeight = 38f;
  public const float TimerTextLeft = 32f;

  public const float GhostBlockGap = 6f;
  public const float HeadlineHeight = 25f;
  public const float DeltaHeight = 32f;
  public const float BackgroundOverlap = 1f;
  public static float GhostBlockTop =>
    TimerY + TimerBackgroundHeight + GhostBlockGap + PracticeTimerExtra;
  public const float GhostBlockHeight =
    GhostBlockGap + HeadlineHeight + DeltaHeight - BackgroundOverlap;

  public static bool PauseScreenOpen =>
    Engine.Scene is Level { Paused: true, PauseMainMenuOpen: false };

  public static float GhostBlockShift => RunManager.GhostBlockVisible ? GhostBlockHeight : 0f;

  public static float ColumnShift => GhostBlockShift + PracticeTimerExtra;

  public static float PracticeTimerExtra { get; private set; }

  public static void MeasureTimer(Level level)
  {
    PracticeTimerExtra = 0f;
    if (level == null || !RunManager.Practice)
      return;
    if (!SpeedrunToolBridge.RoomTimerShown && Settings.Instance.SpeedrunClock != SpeedrunType.File)
      return;
    if (level.Entities.FindFirst<SpeedrunTimerDisplay>() is SpeedrunTimerDisplay timerDisplay)
      PracticeTimerExtra = GFX.Gui[TimerBackground.Texture].Height * 0.6f * timerDisplay.DrawLerp;
  }

  private const float ConditionsTop = 110f;
  private const float ListTop = 280f;
  public static float ConditionsY => ConditionsTop + ColumnShift;
  public static float ListY => ListTop + ColumnShift;
  public const float RowHeight = 46f;
  public const float IconX = 36f;
  public const float TimeX = 64f;
  public const float TimeScale = 0.7f;
  public const float IconHeight = 48f;

  public const float DuckMargin = 120f;
  public const float DuckAlpha = 0.1f;
  public const float DuckTime = 0.1f;

  public const float BoxAlpha = 0.5f;
  public const float BoxPaddingTop = 14f;
  public const float BoxPaddingRight = 20f;
  public const float BoxPaddingBottom = 14f;
  public const int RowPad = 4;

  public static float DuckTarget(Vector2 screen, float boxRight, float boxBottom) =>
    screen.X < boxRight + DuckMargin && screen.Y < boxBottom + DuckMargin ? DuckAlpha : 1f;

  public static float GoalListBottom(int rowCount) =>
    ListY + rowCount * RowHeight + BoxPaddingBottom;
}
