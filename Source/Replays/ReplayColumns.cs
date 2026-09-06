using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Replays;

public delegate void ColumnReader(BinaryReader r, ref InputFrame f);

public sealed class ReplayColumn
{
  public int Bytes;
  public Action<BinaryWriter, InputFrame> Write;
  public ColumnReader Read;
}

public static class ReplayColumns
{
  public const float ScaleUnit = 16f;

  public static readonly ReplayColumn[] All =
  [
    Column(
      2,
      (w, f) => w.Write((ushort)f.Held),
      (BinaryReader r, ref InputFrame f) => f.Held = (Actions)r.ReadUInt16()
    ),
    Column(
      2,
      (w, f) => w.Write((ushort)f.Pressed),
      (BinaryReader r, ref InputFrame f) => f.Pressed = (Actions)r.ReadUInt16()
    ),
    Column(
      2,
      (w, f) => w.Write((ushort)f.Released),
      (BinaryReader r, ref InputFrame f) => f.Released = (Actions)r.ReadUInt16()
    ),
    Column(
      1,
      (w, f) => w.Write(f.GrabToggle),
      (BinaryReader r, ref InputFrame f) => f.GrabToggle = r.ReadBoolean()
    ),
    Column(
      1,
      (w, f) => w.Write(f.HasPlayer),
      (BinaryReader r, ref InputFrame f) => f.HasPlayer = r.ReadBoolean()
    ),
    Column(
      1,
      (w, f) => w.Write(f.MoveX),
      (BinaryReader r, ref InputFrame f) => f.MoveX = r.ReadSByte()
    ),
    Column(
      1,
      (w, f) => w.Write(f.MoveY),
      (BinaryReader r, ref InputFrame f) => f.MoveY = r.ReadSByte()
    ),
    Column(
      1,
      (w, f) => w.Write(f.GliderMoveY),
      (BinaryReader r, ref InputFrame f) => f.GliderMoveY = r.ReadSByte()
    ),
    Column(
      8,
      (w, f) => WriteVector(w, f.Aim),
      (BinaryReader r, ref InputFrame f) => f.Aim = ReadVector(r)
    ),
    Column(
      8,
      (w, f) => WriteVector(w, f.Feather),
      (BinaryReader r, ref InputFrame f) => f.Feather = ReadVector(r)
    ),
    Column(
      8,
      (w, f) => WriteVector(w, f.Position),
      (BinaryReader r, ref InputFrame f) => f.Position = ReadVector(r)
    ),
    Column(
      4,
      (w, f) => w.Write(f.State),
      (BinaryReader r, ref InputFrame f) => f.State = r.ReadInt32()
    ),
    Column(
      4,
      (w, f) => w.Write(f.Dashes),
      (BinaryReader r, ref InputFrame f) => f.Dashes = r.ReadInt32()
    ),
    Column(
      4,
      (w, f) => w.Write(f.Stamina),
      (BinaryReader r, ref InputFrame f) => f.Stamina = r.ReadSingle()
    ),
    Column(
      1,
      (w, f) => w.Write(f.AnimId),
      (BinaryReader r, ref InputFrame f) => f.AnimId = r.ReadByte()
    ),
    Column(
      1,
      (w, f) => w.Write(f.AnimFrame),
      (BinaryReader r, ref InputFrame f) => f.AnimFrame = r.ReadByte()
    ),
    Column(
      1,
      (w, f) => w.Write(f.ScaleX),
      (BinaryReader r, ref InputFrame f) => f.ScaleX = r.ReadSByte()
    ),
    Column(
      1,
      (w, f) => w.Write(f.ScaleY),
      (BinaryReader r, ref InputFrame f) => f.ScaleY = r.ReadSByte()
    ),
    Column(
      1,
      (w, f) => w.Write((byte)f.Flags),
      (BinaryReader r, ref InputFrame f) => f.Flags = (FrameFlags)r.ReadByte()
    ),
  ];

  public static readonly int FrameBytes = All.Sum(c => c.Bytes);

  public static void WriteVector(BinaryWriter w, Vector2 v)
  {
    w.Write(v.X);
    w.Write(v.Y);
  }

  public static Vector2 ReadVector(BinaryReader r) => new(r.ReadSingle(), r.ReadSingle());

  public static sbyte ScaleToSByte(float scale) =>
    (sbyte)Math.Clamp(Math.Round(scale * ScaleUnit), sbyte.MinValue, sbyte.MaxValue);

  private static ReplayColumn Column(
    int bytes,
    Action<BinaryWriter, InputFrame> write,
    ColumnReader read
  ) =>
    new()
    {
      Bytes = bytes,
      Write = write,
      Read = read,
    };
}
