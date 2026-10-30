using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Replays.Codec;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Replays;

internal static class RecordedFrames
{
  public static FrameTable ToFrameTable(List<InputFrame> frames)
  {
    FrameTable table = new(frames.Count);
    for (int i = 0; i < frames.Count; i++)
    {
      InputFrame frame = frames[i];
      table.Set(Column.Held, i, 0, (uint)frame.Held);
      table.Set(Column.Pressed, i, 0, (uint)frame.Pressed);
      table.Set(Column.Released, i, 0, (uint)frame.Released);
      table.Set(Column.GrabToggle, i, 0, frame.GrabToggle ? 1u : 0u);
      table.Set(Column.HasPlayer, i, 0, frame.HasPlayer ? 1u : 0u);
      table.Set(Column.MoveX, i, 0, (byte)frame.MoveX);
      table.Set(Column.MoveY, i, 0, (byte)frame.MoveY);
      table.Set(Column.GliderMoveY, i, 0, (byte)frame.GliderMoveY);
      table.Set(Column.Binds, i, 0, (uint)frame.Binds);
      SetVector(table, Column.Aim, i, frame.Aim);
      SetVector(table, Column.Feather, i, frame.Feather);
      SetVector(table, Column.Position, i, frame.Position);
      table.Set(Column.State, i, 0, unchecked((uint)frame.State));
      table.Set(Column.Dashes, i, 0, unchecked((uint)frame.Dashes));
      table.Set(Column.DashDir, i, 0, frame.DashDir);
      table.Set(Column.Stamina, i, 0, BitConverter.SingleToUInt32Bits(frame.Stamina));
      table.Set(Column.AnimId, i, 0, frame.AnimId);
      table.Set(Column.AnimFrame, i, 0, frame.AnimFrame);
      table.Set(Column.ScaleX, i, 0, (byte)frame.ScaleX);
      table.Set(Column.ScaleY, i, 0, (byte)frame.ScaleY);
      table.Set(Column.Flags, i, 0, (uint)frame.Flags);
    }
    return table;
  }

  public static List<InputFrame> FromFrameTable(FrameTable table)
  {
    List<InputFrame> frames = new(table.FrameCount);
    for (int i = 0; i < table.FrameCount; i++)
      frames.Add(
        new InputFrame
        {
          Held = (Actions)table.Get(Column.Held, i),
          Pressed = (Actions)table.Get(Column.Pressed, i),
          Released = (Actions)table.Get(Column.Released, i),
          GrabToggle = table.Get(Column.GrabToggle, i) != 0,
          HasPlayer = table.Get(Column.HasPlayer, i) != 0,
          MoveX = table.GetSigned(Column.MoveX, i),
          MoveY = table.GetSigned(Column.MoveY, i),
          GliderMoveY = table.GetSigned(Column.GliderMoveY, i),
          Binds = (DirectionBinds)table.Get(Column.Binds, i),
          Aim = GetVector(table, Column.Aim, i),
          Feather = GetVector(table, Column.Feather, i),
          Position = GetVector(table, Column.Position, i),
          State = unchecked((int)table.Get(Column.State, i)),
          Dashes = unchecked((int)table.Get(Column.Dashes, i)),
          DashDir = (byte)table.Get(Column.DashDir, i),
          Stamina = table.GetSingle(Column.Stamina, i, 0),
          AnimId = (byte)table.Get(Column.AnimId, i),
          AnimFrame = (byte)table.Get(Column.AnimFrame, i),
          ScaleX = table.GetSigned(Column.ScaleX, i),
          ScaleY = table.GetSigned(Column.ScaleY, i),
          Flags = (FrameFlags)table.Get(Column.Flags, i),
        }
      );
    return frames;
  }

  private static void SetVector(FrameTable table, Column column, int frame, Vector2 vector)
  {
    table.Set(column, frame, FrameTable.XAxis, BitConverter.SingleToUInt32Bits(vector.X));
    table.Set(column, frame, FrameTable.YAxis, BitConverter.SingleToUInt32Bits(vector.Y));
  }

  private static Vector2 GetVector(FrameTable table, Column column, int frame) =>
    new(
      table.GetSingle(column, frame, FrameTable.XAxis),
      table.GetSingle(column, frame, FrameTable.YAxis)
    );
}
