using System;

namespace Celeste.Mod.ClimbTrials.Replays.Codec;

internal static class Coordinates
{
  public const int MaxWholeCoordinate = 10_000_000;

  private const int StepLimit = 8;

  public static bool IsWhole(float value)
  {
    bool isNegativeZero = value == 0 && float.IsNegative(value);
    return float.IsInteger(value) && MathF.Abs(value) <= MaxWholeCoordinate && !isNegativeZero;
  }

  public static int? WholeCoordinate(FrameTable table, int frame, int axis)
  {
    float value = table.GetSingle(Column.Position, frame, axis);
    return IsWhole(value) ? (int)value : null;
  }

  public static int LastStep(FrameTable table, int frame, int axis)
  {
    int? before = WholeCoordinate(table, frame - 1, axis);
    int? twoBefore = WholeCoordinate(table, frame - 2, axis);
    if (frame < 2 || !before.HasValue || !twoBefore.HasValue)
      return 0;
    return before.Value - twoBefore.Value;
  }

  public static int ClampStep(int step) => Math.Clamp(step, -StepLimit, StepLimit);
}
