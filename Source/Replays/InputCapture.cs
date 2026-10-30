using System;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class InputCapture
{
  public static readonly Actions[] Flags =
  [
    Actions.Jump,
    Actions.Dash,
    Actions.Grab,
    Actions.CrouchDash,
    Actions.Talk,
    Actions.Pause,
    Actions.QuickRestart,
    Actions.Esc,
    Actions.MenuUp,
    Actions.MenuDown,
    Actions.MenuLeft,
    Actions.MenuRight,
    Actions.MenuConfirm,
    Actions.MenuCancel,
    Actions.MenuJournal,
  ];

  public static VirtualButton[] Buttons() =>
    [
      Input.Jump,
      Input.Dash,
      Input.Grab,
      Input.CrouchDash,
      Input.Talk,
      Input.Pause,
      Input.QuickRestart,
      Input.ESC,
      Input.MenuUp,
      Input.MenuDown,
      Input.MenuLeft,
      Input.MenuRight,
      Input.MenuConfirm,
      Input.MenuCancel,
      Input.MenuJournal,
    ];

  public static bool Counts() => Engine.Scene is Level && Engine.OverloadGameLoop == null;

  public static Player FindRecordedPlayer() => (Engine.Scene as Level)?.Tracker.GetEntity<Player>();

  private static void ReadButtons(out Actions held, out Actions pressed, out Actions released)
  {
    held = Actions.None;
    pressed = Actions.None;
    released = Actions.None;
    VirtualButton[] buttons = Buttons();
    for (int i = 0; i < buttons.Length; i++)
    {
      VirtualButton button = buttons[i];
      if (button.Binding.Check(button.GamepadIndex, button.Threshold))
        held |= Flags[i];
      if (button.Binding.Pressed(button.GamepadIndex, button.Threshold))
        pressed |= Flags[i];
      if (button.Binding.Released(button.GamepadIndex, button.Threshold))
        released |= Flags[i];
    }
  }

  public static ButtonState[] ButtonStates()
  {
    VirtualButton[] buttons = Buttons();
    ButtonState[] states = new ButtonState[buttons.Length];
    for (int i = 0; i < buttons.Length; i++)
    {
      states[i].Buffer = buttons[i].bufferCounter;
      states[i].Repeat = buttons[i].repeatCounter;
      states[i].Repeating = buttons[i].Repeating;
    }
    return states;
  }

  public static void RestoreButtonStates(ButtonState[] states)
  {
    VirtualButton[] buttons = Buttons();
    for (int i = 0; i < buttons.Length; i++)
    {
      buttons[i].bufferCounter = states[i].Buffer;
      buttons[i].repeatCounter = states[i].Repeat;
      buttons[i].Repeating = states[i].Repeating;
    }
  }

  private static DirectionBinds ReadDirectionBinds()
  {
    Settings settings = Settings.Instance;
    int pad = Input.Gamepad;
    DirectionBinds binds = DirectionBinds.None;
    if (settings.LeftMoveOnly.Check(pad, 0.3f))
      binds |= DirectionBinds.MoveOnlyLeft;
    if (settings.RightMoveOnly.Check(pad, 0.3f))
      binds |= DirectionBinds.MoveOnlyRight;
    if (settings.UpMoveOnly.Check(pad, 0.7f))
      binds |= DirectionBinds.MoveOnlyUp;
    if (settings.DownMoveOnly.Check(pad, 0.7f))
      binds |= DirectionBinds.MoveOnlyDown;
    if (settings.LeftDashOnly.Check(pad, 0.25f))
      binds |= DirectionBinds.DashOnlyLeft;
    if (settings.RightDashOnly.Check(pad, 0.25f))
      binds |= DirectionBinds.DashOnlyRight;
    if (settings.UpDashOnly.Check(pad, 0.25f))
      binds |= DirectionBinds.DashOnlyUp;
    if (settings.DownDashOnly.Check(pad, 0.25f))
      binds |= DirectionBinds.DashOnlyDown;
    return binds;
  }

  public static InputFrame Sample(Replay replay)
  {
    ReadButtons(out Actions held, out Actions pressed, out Actions released);
    InputFrame frame = new()
    {
      Held = held,
      Pressed = pressed,
      Released = released,
      GrabToggle = Input.grabToggle,
      MoveX = (sbyte)Input.MoveX.Value,
      MoveY = (sbyte)Input.MoveY.Value,
      GliderMoveY = (sbyte)Input.GliderMoveY.Value,
      Binds = ReadDirectionBinds(),
      Aim = Input.Aim.Value,
      Feather = Input.Feather.Value,
    };
    if (Engine.FreezeTimer <= 0f)
      frame.Flags |= FrameFlags.LevelUpdated;
    if (Engine.Scene is Level { Paused: true })
      frame.Flags |= FrameFlags.Paused;
    if (Engine.Scene is Level { Transitioning: true })
      frame.Flags |= FrameFlags.Transitioning;
    Player player = FindRecordedPlayer();
    if (player == null)
      return frame;

    frame.HasPlayer = true;
    frame.Position = player.Position;
    frame.State = player.StateMachine.State;
    frame.Dashes = player.Dashes;
    frame.DashDir = InputFrame.DashDirToByte(player.DashDir);
    frame.Stamina = player.Stamina;
    frame.AnimId = replay.AddAnimation(player.Sprite.CurrentAnimationID);
    frame.AnimFrame = (byte)Math.Min(player.Sprite.CurrentAnimationFrame, byte.MaxValue);
    frame.ScaleX = InputFrame.ScaleToSByte(Math.Abs(player.Sprite.Scale.X));
    frame.ScaleY = InputFrame.ScaleToSByte(player.Sprite.Scale.Y);
    if (player.Facing == Facings.Left)
      frame.Flags |= FrameFlags.FacingLeft;
    return frame;
  }
}
