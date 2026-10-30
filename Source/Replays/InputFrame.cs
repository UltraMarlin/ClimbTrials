using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Replays;

public struct ButtonState
{
  public float Buffer;
  public float Repeat;
  public bool Repeating;
}

public struct InputFrame
{
  public Actions Held;
  public Actions Pressed;
  public Actions Released;
  public bool GrabToggle;
  public bool HasPlayer;
  public sbyte MoveX;
  public sbyte MoveY;
  public sbyte GliderMoveY;
  public DirectionBinds Binds;
  public Vector2 Aim;
  public Vector2 Feather;
  public Vector2 Position;
  public int State;
  public int Dashes;
  public byte DashDir;
  public float Stamina;
  public byte AnimId;
  public byte AnimFrame;
  public sbyte ScaleX;
  public sbyte ScaleY;
  public FrameFlags Flags;

  public bool LevelUpdated => (Flags & FrameFlags.LevelUpdated) != 0;
  public bool FacingLeft => (Flags & FrameFlags.FacingLeft) != 0;
  public bool Paused => (Flags & FrameFlags.Paused) != 0;
  public bool Transitioning => (Flags & FrameFlags.Transitioning) != 0;

  public const float ScaleUnit = 16f;

  private static readonly Vector2[] DashDirections =
  [
    new(-1f, -1f),
    new(0f, -1f),
    new(1f, -1f),
    new(-1f, 0f),
    new(1f, 0f),
    new(-1f, 1f),
    new(0f, 1f),
    new(1f, 1f),
  ];

  public static sbyte ScaleToSByte(float scale) =>
    (sbyte)Math.Clamp(Math.Round(scale * ScaleUnit), sbyte.MinValue, sbyte.MaxValue);

  public static byte DashDirToByte(Vector2 direction)
  {
    Vector2 signs = new(Math.Sign(direction.X), Math.Sign(direction.Y));
    int index = Array.IndexOf(DashDirections, signs);
    return index < 0 ? (byte)0 : (byte)(index + 1);
  }

  public static Vector2 DashDirFromByte(byte value) =>
    value == 0 ? Vector2.Zero : Vector2.Normalize(DashDirections[value - 1]);
}
