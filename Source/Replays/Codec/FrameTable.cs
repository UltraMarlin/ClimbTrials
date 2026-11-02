using System;
using System.Linq;

namespace Celeste.Mod.ClimbTrials.Replays.Codec;

internal enum Column
{
  Held,
  Pressed,
  Released,
  GrabToggle,
  HasPlayer,
  MoveX,
  MoveY,
  GliderMoveY,
  Binds,
  Aim,
  Feather,
  Position,
  State,
  Dashes,
  DashDir,
  Stamina,
  AnimId,
  AnimFrame,
  ScaleX,
  ScaleY,
  Flags,
}

internal sealed class FrameTable
{
  public const int XAxis = 0;
  public const int YAxis = 1;
  private const uint DashState = 2;

  public static readonly Column[] Columns = Enum.GetValues<Column>();
  public static readonly int FrameBytes = Columns.Sum(WidthOf);

  public readonly int FrameCount;
  private readonly uint[][][] values = new uint[Columns.Length][][];

  public FrameTable(int frameCount)
  {
    FrameCount = frameCount;
    foreach (Column column in Columns)
    {
      values[(int)column] = new uint[AxesOf(column)][];
      for (int axis = 0; axis < AxesOf(column); axis++)
        values[(int)column][axis] = new uint[frameCount];
    }
  }

  public static int WidthOf(Column column) =>
    column switch
    {
      Column.Held or Column.Pressed or Column.Released => 2,
      Column.Aim or Column.Feather or Column.Position => 8,
      Column.State or Column.Dashes or Column.Stamina => 4,
      _ => 1,
    };

  private static int AxesOf(Column column) => WidthOf(column) == 8 ? 2 : 1;

  public static int ValueBytesOf(Column column) => WidthOf(column) / AxesOf(column);

  public uint Get(Column column, int frame, int axis = 0) =>
    frame < 0 ? 0 : values[(int)column][axis][frame];

  public void Set(Column column, int frame, int axis, uint value)
  {
    int valueBits = 8 * ValueBytesOf(column);
    if (valueBits < 32 && value >> valueBits != 0)
      throw new ArgumentOutOfRangeException(nameof(value), $"{value} does not fit {column}");
    values[(int)column][axis][frame] = value;
  }

  public void ClearFrame(int frame)
  {
    foreach (uint[][] axes in values)
    foreach (uint[] frames in axes)
      frames[frame] = 0;
  }

  public sbyte GetSigned(Column column, int frame) => (sbyte)Get(column, frame);

  public float GetSingle(Column column, int frame, int axis) =>
    BitConverter.UInt32BitsToSingle(Get(column, frame, axis));

  public bool DashStarts(int frame) =>
    frame > 0 && Get(Column.State, frame) == DashState && Get(Column.State, frame - 1) != DashState;

  public bool HasFlag(int frame, FrameFlags flag) => (Get(Column.Flags, frame) & (uint)flag) != 0;

  public bool LevelUpdated(int frame) => HasFlag(frame, FrameFlags.LevelUpdated);

  public bool Transitioning(int frame) => HasFlag(frame, FrameFlags.Transitioning);

  public bool PlayerUpdated(int frame) =>
    LevelUpdated(frame)
    && !HasFlag(frame, FrameFlags.Paused)
    && Get(Column.HasPlayer, frame) != 0
    && !Transitioning(frame);
}
