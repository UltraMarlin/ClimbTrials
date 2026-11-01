using System;

namespace Celeste.Mod.ClimbTrials.Replays.Codec;

internal delegate uint Predict(FrameTable table, int frame, Trackers trackers);

internal static class GameRules
{
  private const uint ClimbState = 1;
  private const uint FloatOne = 0x3f800000;
  private const uint FloatMinusOne = 0xbf800000;
  private const int ScaleSumInSixteenths = 32;
  private const int JumpScaleInSixteenths = 10;
  private const float ClimbJumpStamina = 27.5f;
  private const int DashTakeDelay = 4;
  private const int DashStep = 4;
  private const int DiagonalDashStep = 3;

  private static readonly uint[] DashDirections = [1, 2, 3, 4, 0, 5, 6, 7, 8];
  private static readonly uint DashId = (uint)PlayerAnimations.IdOf("dash");
  private static readonly uint DuckId = (uint)PlayerAnimations.IdOf("duck");

  private static int SignOf(float value)
  {
    if (value < 0)
      return -1;
    return value > 0 ? 1 : 0;
  }

  public static Predict PredictFrameBefore(Column column, int axis = 0) =>
    (table, frame, trackers) => table.Get(column, frame - 1, axis);

  private static uint MenuButtonsOf(FrameTable table, int frame)
  {
    sbyte moveX = table.GetSigned(Column.MoveX, frame);
    sbyte moveY = table.GetSigned(Column.MoveY, frame);
    uint buttons = 0;
    if (moveY < 0)
      buttons |= (uint)Actions.MenuUp;
    if (moveY > 0)
      buttons |= (uint)Actions.MenuDown;
    if (moveX < 0)
      buttons |= (uint)Actions.MenuLeft;
    if (moveX > 0)
      buttons |= (uint)Actions.MenuRight;
    return buttons;
  }

  public static uint PredictHeld(FrameTable table, int frame, Trackers trackers) =>
    table.Get(Column.Held, frame - 1)
    ^ MenuButtonsOf(table, frame - 1)
    ^ MenuButtonsOf(table, frame);

  public static uint PredictPressed(FrameTable table, int frame, Trackers trackers) =>
    table.Get(Column.Held, frame) & ~table.Get(Column.Held, frame - 1);

  public static uint PredictReleased(FrameTable table, int frame, Trackers trackers) =>
    table.Get(Column.Held, frame - 1) & ~table.Get(Column.Held, frame);

  public static uint PredictGliderMoveY(FrameTable table, int frame, Trackers trackers) =>
    table.Get(Column.MoveY, frame);

  private static uint UnitFloatOf(sbyte move) =>
    move < 0 ? FloatMinusOne
    : move > 0 ? FloatOne
    : 0;

  public static uint PredictAimX(FrameTable table, int frame, Trackers trackers) =>
    UnitFloatOf(table.GetSigned(Column.MoveX, frame));

  public static uint PredictAimY(FrameTable table, int frame, Trackers trackers) =>
    UnitFloatOf(table.GetSigned(Column.MoveY, frame));

  public static uint PredictFeatherX(FrameTable table, int frame, Trackers trackers) =>
    table.Get(Column.Aim, frame, FrameTable.XAxis);

  public static uint PredictFeatherY(FrameTable table, int frame, Trackers trackers) =>
    table.Get(Column.Aim, frame, FrameTable.YAxis);

  public static uint PredictDashes(FrameTable table, int frame, Trackers trackers)
  {
    uint dashes = table.Get(Column.Dashes, frame - 1);
    return table.DashStarts(frame) && dashes > 0 ? dashes - 1 : dashes;
  }

  public static uint PredictDashDir(FrameTable table, int frame, Trackers trackers)
  {
    if (table.DashStarts(frame))
      return 0;

    if (!table.DashStarts(frame - DashTakeDelay))
      return table.Get(Column.DashDir, frame - 1);

    int signX = SignOf(table.GetSingle(Column.Aim, frame - 1, FrameTable.XAxis));
    int signY = SignOf(table.GetSingle(Column.Aim, frame - 1, FrameTable.YAxis));
    return DashDirections[(signY + 1) * 3 + signX + 1];
  }

  public static uint PredictFlags(FrameTable table, int frame, Trackers trackers)
  {
    uint flags = table.Get(Column.Flags, frame - 1);
    bool frozen =
      table.DashStarts(frame) || table.DashStarts(frame - 1) || table.DashStarts(frame - 2);

    if (frozen)
      flags &= ~(uint)FrameFlags.LevelUpdated;
    else if (table.DashStarts(frame - 3))
      flags |= (uint)FrameFlags.LevelUpdated;

    if (table.Transitioning(frame - 1))
      return flags;

    sbyte moveX = table.GetSigned(Column.MoveX, frame - 1);
    if (moveX < 0)
      flags |= (uint)FrameFlags.FacingLeft;
    if (moveX > 0)
      flags &= ~(uint)FrameFlags.FacingLeft;
    return flags;
  }

  public static uint PredictPositionX(FrameTable table, int frame, Trackers trackers) =>
    unchecked((uint)PredictCoordinate(table, frame, FrameTable.XAxis));

  public static uint PredictPositionY(FrameTable table, int frame, Trackers trackers) =>
    unchecked((uint)PredictCoordinate(table, frame, FrameTable.YAxis));

  private static int PredictCoordinate(FrameTable table, int frame, int axis)
  {
    int? before = Coordinates.WholeCoordinate(table, frame - 1, axis);
    if (!before.HasValue)
      return 0;

    if (!table.LevelUpdated(frame))
      return before.Value;

    if (!table.DashStarts(frame - DashTakeDelay))
      return before.Value + Coordinates.LastStep(table, frame, axis);

    int signX = SignOf(table.GetSingle(Column.Aim, frame - 1, FrameTable.XAxis));
    int signY = SignOf(table.GetSingle(Column.Aim, frame - 1, FrameTable.YAxis));
    if (signX == 0 && signY == 0)
      signX = table.HasFlag(frame - 1, FrameFlags.FacingLeft) ? -1 : 1;
    int step = signX != 0 && signY != 0 ? DiagonalDashStep : DashStep;
    return before.Value + (axis == FrameTable.XAxis ? signX : signY) * step;
  }

  public static uint PredictScaleX(FrameTable table, int frame, Trackers trackers)
  {
    if (table.Get(Column.HasPlayer, frame) == 0)
      return 0;
    return (byte)Trackers.ToSixteenths(trackers.ExpectedScale);
  }

  public static uint PredictScaleY(FrameTable table, int frame, Trackers trackers) =>
    (byte)(ScaleSumInSixteenths - table.GetSigned(Column.ScaleX, frame));

  public static uint PredictStamina(FrameTable table, int frame, Trackers trackers)
  {
    uint before = table.Get(Column.Stamina, frame - 1);
    float stamina = BitConverter.UInt32BitsToSingle(before);
    if (!float.IsFinite(stamina))
      return before;

    bool climbJump =
      Jumped(table, frame) && (table.Get(Column.Held, frame - 1) & (uint)Actions.Grab) != 0;
    if (climbJump)
      return BitConverter.SingleToUInt32Bits(stamina - ClimbJumpStamina);

    bool climbing =
      table.Get(Column.State, frame) == ClimbState
      && table.Get(Column.State, frame - 1) == ClimbState;
    if (!climbing)
      return before;

    float twoBefore = table.GetSingle(Column.Stamina, frame - 2, 0);
    if (!float.IsFinite(twoBefore) || stamina >= twoBefore)
      return before;

    return BitConverter.SingleToUInt32Bits(stamina + (stamina - twoBefore));
  }

  private static bool Jumped(FrameTable table, int frame) =>
    frame >= 1
    && table.GetSigned(Column.ScaleX, frame) == JumpScaleInSixteenths
    && table.GetSigned(Column.ScaleX, frame - 1) != JumpScaleInSixteenths;

  private static Animation AnimationAt(FrameTable table, int frame, Trackers trackers)
  {
    Animation expected = trackers.ExpectedAnimation;
    if (!table.DashStarts(frame))
      return expected;

    uint startAnimationId = table.GetSigned(Column.MoveY, frame - 1) == 1 ? DuckId : DashId;
    return startAnimationId == expected.Id ? expected : new(startAnimationId, 0, 0);
  }

  public static uint PredictAnimId(FrameTable table, int frame, Trackers trackers) =>
    AnimationAt(table, frame, trackers).Id;

  public static uint PredictAnimFrame(FrameTable table, int frame, Trackers trackers)
  {
    Animation predicted = AnimationAt(table, frame, trackers);
    return table.Get(Column.AnimId, frame) == predicted.Id ? predicted.AnimationFrame : 0;
  }
}
