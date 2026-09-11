using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Replays;

[Flags]
public enum Actions : ushort
{
  None = 0,
  Jump = 1 << 0,
  Dash = 1 << 1,
  Grab = 1 << 2,
  CrouchDash = 1 << 3,
  Talk = 1 << 4,
  Pause = 1 << 5,
  QuickRestart = 1 << 6,
  Esc = 1 << 7,
  MenuUp = 1 << 8,
  MenuDown = 1 << 9,
  MenuLeft = 1 << 10,
  MenuRight = 1 << 11,
  MenuConfirm = 1 << 12,
  MenuCancel = 1 << 13,
  MenuJournal = 1 << 14,
}

[Flags]
public enum FrameFlags : byte
{
  None = 0,
  FacingLeft = 1 << 0,
  LevelUpdated = 1 << 1,
  Paused = 1 << 2,
}

[Flags]
public enum DirectionBinds : byte
{
  None = 0,
  MoveOnlyLeft = 1 << 0,
  MoveOnlyRight = 1 << 1,
  MoveOnlyUp = 1 << 2,
  MoveOnlyDown = 1 << 3,
  DashOnlyLeft = 1 << 4,
  DashOnlyRight = 1 << 5,
  DashOnlyUp = 1 << 6,
  DashOnlyDown = 1 << 7,
}

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
}
