using System;

namespace Celeste.Mod.ClimbTrials.Replays.Codec;

internal enum FieldKind
{
  Bits,
  Number,
  Float,
  Coordinate,
}

internal sealed record Field(string Name, Column Column, int Lane, FieldKind Kind, Predict Predict);

internal static class Fields
{
  public static readonly Field[] InCodingOrder =
  [
    new("MoveX", Column.MoveX, 0, FieldKind.Number, GameRules.PredictFrameBefore(Column.MoveX)),
    new("MoveY", Column.MoveY, 0, FieldKind.Number, GameRules.PredictFrameBefore(Column.MoveY)),
    new("Held", Column.Held, 0, FieldKind.Bits, GameRules.PredictHeld),
    new("Pressed", Column.Pressed, 0, FieldKind.Bits, GameRules.PredictPressed),
    new("Released", Column.Released, 0, FieldKind.Bits, GameRules.PredictReleased),
    new(
      "GrabToggle",
      Column.GrabToggle,
      0,
      FieldKind.Number,
      GameRules.PredictFrameBefore(Column.GrabToggle)
    ),
    new("GliderMoveY", Column.GliderMoveY, 0, FieldKind.Number, GameRules.PredictGliderMoveY),
    new("Binds", Column.Binds, 0, FieldKind.Bits, GameRules.PredictFrameBefore(Column.Binds)),
    new("AimX", Column.Aim, FrameTable.XAxis, FieldKind.Float, GameRules.PredictAimX),
    new("AimY", Column.Aim, FrameTable.YAxis, FieldKind.Float, GameRules.PredictAimY),
    new("FeatherX", Column.Feather, FrameTable.XAxis, FieldKind.Float, GameRules.PredictFeatherX),
    new("FeatherY", Column.Feather, FrameTable.YAxis, FieldKind.Float, GameRules.PredictFeatherY),
    new(
      "HasPlayer",
      Column.HasPlayer,
      0,
      FieldKind.Number,
      GameRules.PredictFrameBefore(Column.HasPlayer)
    ),
    new("State", Column.State, 0, FieldKind.Number, GameRules.PredictFrameBefore(Column.State)),
    new("Dashes", Column.Dashes, 0, FieldKind.Number, GameRules.PredictDashes),
    new("DashDir", Column.DashDir, 0, FieldKind.Number, GameRules.PredictDashDir),
    new("Flags", Column.Flags, 0, FieldKind.Bits, GameRules.PredictFlags),
    new(
      "PositionX",
      Column.Position,
      FrameTable.XAxis,
      FieldKind.Coordinate,
      GameRules.PredictPositionX
    ),
    new(
      "PositionY",
      Column.Position,
      FrameTable.YAxis,
      FieldKind.Coordinate,
      GameRules.PredictPositionY
    ),
    new("ScaleX", Column.ScaleX, 0, FieldKind.Number, GameRules.PredictScaleX),
    new("ScaleY", Column.ScaleY, 0, FieldKind.Number, GameRules.PredictScaleY),
    new("Stamina", Column.Stamina, 0, FieldKind.Float, GameRules.PredictStamina),
    new("AnimId", Column.AnimId, 0, FieldKind.Number, GameRules.PredictAnimId),
    new("AnimFrame", Column.AnimFrame, 0, FieldKind.Number, GameRules.PredictAnimFrame),
  ];

  private const uint PredictionCap = 256;
  private const uint StateCap = 31;

  public static uint ValueContextOf(FrameTable table, int frame, int field, uint prediction)
  {
    uint context = Coder.Extend((uint)ContextKind.Value, field);
    Field row = InCodingOrder[field];
    int state = (int)Math.Min(table.Get(Column.State, frame), StateCap);
    if (row.Kind != FieldKind.Coordinate)
    {
      uint predictionContext = Coder.Extend(context, (int)Math.Min(prediction, PredictionCap));
      bool seesState = row.Column == Column.AnimId || row.Column == Column.ScaleX;
      return seesState ? Coder.Extend(predictionContext, state) : predictionContext;
    }
    int stepToFrameBefore = Coordinates.ClampStep(Coordinates.LastStep(table, frame, row.Lane));
    int stepBefore = Coordinates.ClampStep(Coordinates.LastStep(table, frame - 1, row.Lane));
    int stepBeforeThat = Coordinates.ClampStep(Coordinates.LastStep(table, frame - 2, row.Lane));
    context = Coder.Extend(Coder.Extend(context, stepToFrameBefore), stepBefore);
    return Coder.Extend(Coder.Extend(context, state), stepBeforeThat);
  }

  public static uint HitContextOf(
    FrameTable table,
    int frame,
    uint valueContext,
    int missAge,
    bool predictionDiffers
  )
  {
    uint context = Coder.Extend((uint)ContextKind.Hit, unchecked((int)valueContext));
    context = Coder.Extend(Coder.Extend(context, missAge), predictionDiffers ? 1 : 0);
    return Coder.Extend(context, table.LevelUpdated(frame - 1) ? 1 : 0);
  }

  public static uint RepeatContextOf(uint hitContext) =>
    Coder.Extend((uint)ContextKind.Repeat, unchecked((int)hitContext));
}
