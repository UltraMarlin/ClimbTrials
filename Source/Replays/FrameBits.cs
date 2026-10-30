using System;

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
  Transitioning = 1 << 3,
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
