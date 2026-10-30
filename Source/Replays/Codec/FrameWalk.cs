using System;
using System.IO;

namespace Celeste.Mod.ClimbTrials.Replays.Codec;

internal sealed class FrameWalk(Coder coder, FrameTable table, FrameTable source)
{
  private readonly Trackers trackers = new(Fields.InCodingOrder.Length);

  public void Run()
  {
    bool[] missed = new bool[Fields.InCodingOrder.Length];
    for (int frame = 0; frame < table.FrameCount; frame++)
    {
      for (int fieldIndex = 0; fieldIndex < missed.Length; fieldIndex++)
      {
        missed[fieldIndex] =
          Fields.InCodingOrder[fieldIndex].Kind == FieldKind.Coordinate
            ? CodeCoordinate(frame, fieldIndex)
            : CodeValue(frame, fieldIndex);
      }
      trackers.Advance(table, frame, missed);
    }
  }

  private bool CodeValue(int frame, int fieldIndex)
  {
    Field field = Fields.InCodingOrder[fieldIndex];
    uint prediction = Cut(
      field.Predict(table, frame, trackers),
      FrameTable.ValueBytesOf(field.Column)
    );
    uint before = table.Get(field.Column, frame - 1, field.Lane);
    uint valueContext = Fields.ValueContextOf(table, frame, fieldIndex, prediction);
    int missAge = trackers.MissAge(fieldIndex, frame);
    uint hitContext = Fields.HitContextOf(
      table,
      frame,
      valueContext,
      missAge,
      prediction != before
    );
    uint value = source == null ? 0 : source.Get(field.Column, frame, field.Lane);
    uint result;

    if (prediction == before)
      result = CodeDifference(valueContext, hitContext, value, prediction, field);
    else if (coder.CodeBit(hitContext, value != prediction ? 1 : 0) == 0)
      result = prediction;
    else if (coder.CodeBit(Fields.RepeatContextOf(hitContext), value == before ? 1 : 0) == 1)
      result = before;
    else
      result = CodeDifference(valueContext, valueContext, value, prediction, field);

    table.Set(field.Column, frame, field.Lane, result);
    return result != prediction;
  }

  private uint CodeDifference(
    uint valueContext,
    uint firstBitContext,
    uint value,
    uint prediction,
    Field field
  )
  {
    int valueBytes = FrameTable.ValueBytesOf(field.Column);
    uint recordedDifference = DifferenceOf(field.Kind, value, prediction, valueBytes);
    uint difference = coder.CodeNumber(valueContext, recordedDifference, firstBitContext);
    if (difference > MaxOf(valueBytes))
      throw new InvalidDataException($"{field.Name} is out of range");

    return ValueFrom(field.Kind, difference, prediction, valueBytes);
  }

  private bool CodeCoordinate(int frame, int fieldIndex)
  {
    Field field = Fields.InCodingOrder[fieldIndex];
    uint word = source == null ? 0 : source.Get(Column.Position, frame, field.Lane);
    float recorded = BitConverter.UInt32BitsToSingle(word);
    uint formContext = Coder.Extend((uint)ContextKind.CoordinateForm, fieldIndex);
    if (coder.CodeBit(formContext, Coordinates.IsWhole(recorded) ? 1 : 0) == 0)
    {
      uint floatBitsContext = Coder.Extend((uint)ContextKind.CoordinateFloatBits, fieldIndex);
      table.Set(Column.Position, frame, field.Lane, coder.CodeNumber(floatBitsContext, word));
      return true;
    }

    int prediction = unchecked((int)field.Predict(table, frame, trackers));
    int before = Coordinates.WholeCoordinate(table, frame - 1, field.Lane) ?? 0;
    uint valueContext = Fields.ValueContextOf(
      table,
      frame,
      fieldIndex,
      unchecked((uint)prediction)
    );
    int missAge = trackers.MissAge(fieldIndex, frame);
    uint hitContext = Fields.HitContextOf(
      table,
      frame,
      valueContext,
      missAge,
      prediction != before
    );

    int recordedStep = source == null ? 0 : (int)recorded - prediction;
    int step = FromZigzag(coder.CodeNumber(valueContext, ToZigzag(recordedStep), hitContext));
    long coordinate = prediction + (long)step;
    if (Math.Abs(coordinate) > Coordinates.MaxWholeCoordinate)
      throw new InvalidDataException($"{field.Name} is out of range");

    table.Set(Column.Position, frame, field.Lane, BitConverter.SingleToUInt32Bits(coordinate));
    return step != 0;
  }

  private static uint MaxOf(int valueBytes) =>
    valueBytes == 4 ? uint.MaxValue : (1u << (8 * valueBytes)) - 1;

  private static uint Cut(uint value, int valueBytes) => value & MaxOf(valueBytes);

  private static int SignedDifference(uint value, uint prediction, int valueBytes) =>
    valueBytes switch
    {
      1 => (sbyte)(value - prediction),
      2 => (short)(value - prediction),
      _ => unchecked((int)(value - prediction)),
    };

  private static uint ToZigzag(int value) => (uint)((value << 1) ^ (value >> 31));

  private static int FromZigzag(uint zigzag) => (int)(zigzag >> 1) ^ -(int)(zigzag & 1);

  private static uint DifferenceOf(FieldKind kind, uint value, uint prediction, int valueBytes)
  {
    if (kind == FieldKind.Bits)
      return Cut(value ^ prediction, valueBytes);
    return ToZigzag(SignedDifference(value, prediction, valueBytes));
  }

  private static uint ValueFrom(FieldKind kind, uint difference, uint prediction, int valueBytes)
  {
    if (kind == FieldKind.Bits)
      return Cut(difference ^ prediction, valueBytes);
    return Cut(prediction + (uint)FromZigzag(difference), valueBytes);
  }
}
