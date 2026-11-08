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
    TimerY + TimerBackgroundHeight + GhostBlockGap + SpeedrunToolTimerExtra;
  public const float GhostBlockHeight =
    GhostBlockGap + HeadlineHeight + DeltaHeight - BackgroundOverlap;

  public static bool PauseScreenOpen =>
    Engine.Scene is Level { Paused: true, PauseMainMenuOpen: false };

  public static float GhostBlockShift => RunManager.GhostBlockVisible ? GhostBlockHeight : 0f;

  public static float ColumnShift => GhostBlockShift + SpeedrunToolTimerExtra;

  public static bool SpeedrunToolTimerShown { get; private set; }

  public static float SpeedrunToolTimerExtra { get; private set; }

  public static void UpdateSpeedrunToolTimer(Level level)
  {
    SpeedrunToolTimerShown =
      level != null && RunManager.Practice && SpeedrunToolBridge.RoomTimerShown;
    SpeedrunToolTimerExtra = 0f;
    if (
      SpeedrunToolTimerShown
      && level.Entities.FindFirst<SpeedrunTimerDisplay>() is SpeedrunTimerDisplay timerDisplay
    )
      SpeedrunToolTimerExtra =
        GFX.Gui[TimerBackground.Texture].Height * 0.6f * timerDisplay.DrawLerp;
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

  public static float ApproachDuck(float duck, Level level, float boxRight, float boxBottom)
  {
    Player player = level.Tracker.GetEntity<Player>();
    float target = 1f;
    if (player != null)
    {
      Vector2 screen = level.WorldToScreen(player.Center);
      if (screen.X < boxRight + DuckMargin && screen.Y < boxBottom + DuckMargin)
        target = DuckAlpha;
    }
    return Calc.Approach(duck, target, Engine.RawDeltaTime / DuckTime);
  }

  public static float GoalListBottom(int rowCount) =>
    ListY + rowCount * RowHeight + BoxPaddingBottom;
}
