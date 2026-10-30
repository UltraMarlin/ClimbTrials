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
      values[(int)column] = new uint[LanesOf(column)][];
      for (int lane = 0; lane < LanesOf(column); lane++)
        values[(int)column][lane] = new uint[frameCount];
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

  private static int LanesOf(Column column) => WidthOf(column) == 8 ? 2 : 1;

  public static int ValueBytesOf(Column column) => WidthOf(column) / LanesOf(column);

  public uint Get(Column column, int frame, int lane = 0) =>
    frame < 0 ? 0 : values[(int)column][lane][frame];

  public void Set(Column column, int frame, int lane, uint value)
  {
    int valueBits = 8 * ValueBytesOf(column);
    if (valueBits < 32 && value >> valueBits != 0)
      throw new ArgumentOutOfRangeException(nameof(value), $"{value} does not fit {column}");
    values[(int)column][lane][frame] = value;
  }

  public sbyte GetSigned(Column column, int frame) => (sbyte)Get(column, frame);

  public float GetSingle(Column column, int frame, int lane) =>
    BitConverter.UInt32BitsToSingle(Get(column, frame, lane));

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
