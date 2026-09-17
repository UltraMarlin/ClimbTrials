using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Entities;

public static class RunHudLayout
{
  public const float ConditionsY = 110f;
  public const float RowHeight = 46f;
  public const float ListY = 280f;
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
