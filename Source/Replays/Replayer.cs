using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.Core;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Replays;

public enum ReplayState
{
  Idle,
  Playing,
  Paused,
  Finished,
}

public static class Replayer
{
  private const string LogTag = "ClimbTrials/Replay";
  private const string DesyncSound = "event:/ui/main/button_invalid";
  private const string PauseSound = "event:/game/general/cassette_block_switch_1";
  private const string ResumeSound = "event:/game/general/cassette_block_switch_2";
  private const string RestartSound = "event:/ui/main/button_select";
  private const float WatcherThreshold = 0.2f;

  public static ReplayState State { get; private set; }
  public static bool Desynced { get; private set; }
  public static int Frame { get; private set; }
  public static bool Silenced => State is ReplayState.Playing or ReplayState.Paused;

  private static Replay current;

  private static readonly ButtonNode[] buttonNodes = new ButtonNode[Replay.ButtonCount];
  private static readonly AxisNode moveX = new();
  private static readonly AxisNode moveY = new();
  private static readonly AxisNode gliderMoveY = new();
  private static readonly StickNode aim = new();
  private static readonly StickNode feather = new();

  private static readonly List<(VirtualButton Button, Binding Binding)> savedButtons = [];
  private static readonly List<(VirtualIntegerAxis Axis, Binding[] Bindings)> savedAxes = [];
  private static readonly List<(VirtualJoystick Stick, Binding[] Bindings)> savedSticks = [];

  private static GrabModes savedGrabMode;
  private static CrouchDashModes savedCrouchDashMode;
  private static ScreenshakeAmount savedScreenShake;
  private static Language savedLanguage;

  public static void Load()
  {
    for (int i = 0; i < buttonNodes.Length; i++)
      buttonNodes[i] = new ButtonNode();
    On.Monocle.MInput.Update += OnMInputUpdate;
    On.Monocle.Engine.Update += OnEngineUpdate;
    On.Celeste.Level.Pause += OnLevelPause;
    On.Celeste.Input.GuiButton_VirtualButton_PrefixMode_string += OnGuiButton;
  }

  public static void Unload()
  {
    Clear();
    On.Monocle.MInput.Update -= OnMInputUpdate;
    On.Monocle.Engine.Update -= OnEngineUpdate;
    On.Celeste.Level.Pause -= OnLevelPause;
    On.Celeste.Input.GuiButton_VirtualButton_PrefixMode_string -= OnGuiButton;
  }

  public static void Begin(Replay replay, Level level)
  {
    RestoreInputs();
    current = replay;
    Frame = 0;
    Desynced = false;

    savedGrabMode = Settings.Instance.GrabMode;
    savedCrouchDashMode = Settings.Instance.CrouchDashMode;
    savedScreenShake = Settings.Instance.ScreenShake;
    PinSettings();

    savedLanguage = Dialog.Language;
    if (Dialog.Languages.TryGetValue(replay.Language, out Language language))
    {
      Fonts.Load(language.FontFace);
      Dialog.Language = language;
    }

    Input.LastAim = replay.LastAim;
    level.TimeActive = replay.TimeActive;
    level.RawTimeActive = replay.RawTimeActive;

    VirtualButton[] buttons = InputCapture.Buttons();
    for (int i = 0; i < buttons.Length; i++)
    {
      savedButtons.Add((buttons[i], buttons[i].Binding));
      buttons[i].Binding = new Binding();
      buttons[i].Nodes.Add(buttonNodes[i]);
    }
    InputCapture.RestoreButtonStates(replay.Buttons);
    ClearFed();
    SilenceAxis(Input.MoveX, moveX);
    SilenceAxis(Input.MoveY, moveY);
    SilenceAxis(Input.GliderMoveY, gliderMoveY);
    SilenceStick(Input.Aim, aim);
    SilenceStick(Input.Feather, feather);

    State = ReplayState.Playing;
    Logger.Log(
      LogLevel.Info,
      LogTag,
      $"replay '{replay.TrialId}' started, {replay.Frames.Count} frames"
    );
  }

  public static bool Finish(long elapsedTicks)
  {
    if (current == null)
      return false;

    int missing = current.Frames.Count - Frame;
    bool alreadyDesynced = Desynced;
    RestoreInputs();
    State = ReplayState.Finished;
    if (alreadyDesynced)
      return false;

    if (missing != 0)
    {
      Desync($"finished {missing} frames early");
      return false;
    }
    if (elapsedTicks != current.Ticks)
    {
      Desync(
        $"finished at {Attempt.Format(elapsedTicks)}, "
          + $"the header says {Attempt.Format(current.Ticks)}"
      );
      return false;
    }
    return true;
  }

  public static void Clear()
  {
    RestoreInputs();
    State = ReplayState.Idle;
    current = null;
    Desynced = false;
  }

  public static void PinSettings()
  {
    if (current == null)
      return;
    Settings.Instance.GrabMode = current.GrabMode;
    Settings.Instance.CrouchDashMode = current.CrouchDashMode;
    Settings.Instance.ScreenShake = current.ScreenShake;
  }

  public static void UnpinSettings()
  {
    Settings.Instance.GrabMode = savedGrabMode;
    Settings.Instance.CrouchDashMode = savedCrouchDashMode;
    Settings.Instance.ScreenShake = savedScreenShake;
  }

  private static void OnLevelPause(
    On.Celeste.Level.orig_Pause orig,
    Level self,
    int startIndex,
    bool minimal,
    bool quickReset
  )
  {
    if (current == null)
    {
      orig(self, startIndex, minimal, quickReset);
      return;
    }
    bool watcherValue = CoreModule.Settings.ShowModOptionsInGame;
    CoreModule.Settings.ShowModOptionsInGame = current.ModOptionsInPauseMenu;
    try
    {
      orig(self, startIndex, minimal, quickReset);
    }
    finally
    {
      CoreModule.Settings.ShowModOptionsInGame = watcherValue;
    }
  }

  private static void RestoreInputs()
  {
    if (!Silenced)
      return;

    foreach ((VirtualButton button, Binding binding) in savedButtons)
    {
      button.Binding = binding;
      button.Nodes.RemoveAll(n => n is ButtonNode);
    }
    savedButtons.Clear();
    foreach ((VirtualIntegerAxis axis, Binding[] b) in savedAxes)
    {
      axis.Negative = b[0];
      axis.NegativeAlt = b[1];
      axis.Positive = b[2];
      axis.PositiveAlt = b[3];
      axis.Nodes.RemoveAll(n => n is AxisNode);
    }
    savedAxes.Clear();
    foreach ((VirtualJoystick stick, Binding[] b) in savedSticks)
    {
      stick.Up = b[0];
      stick.UpAlt = b[1];
      stick.Down = b[2];
      stick.DownAlt = b[3];
      stick.Left = b[4];
      stick.LeftAlt = b[5];
      stick.Right = b[6];
      stick.RightAlt = b[7];
      stick.Nodes.RemoveAll(n => n is StickNode);
    }
    savedSticks.Clear();

    UnpinSettings();
    if (savedLanguage != null)
      Dialog.Language = savedLanguage;
    savedLanguage = null;
    State = ReplayState.Idle;
  }

  private static void OnEngineUpdate(
    On.Monocle.Engine.orig_Update orig,
    Engine self,
    GameTime gameTime
  )
  {
    if (State != ReplayState.Paused)
    {
      orig(self, gameTime);
      return;
    }

    if (AssetReloadHelper.IsReloading || Engine.OverloadGameLoop != null)
    {
      orig(self, gameTime);
      return;
    }

    if (Engine.Instance.IsActive && MInput.Active && !Engine.Commands.Open)
    {
      MInput.Keyboard.Update();
      MInput.GamePads[Input.Gamepad].Update();
      HandleWatcherControls();
    }

    if (State == ReplayState.Idle)
      orig(self, gameTime);
  }

  private static void OnMInputUpdate(On.Monocle.MInput.orig_Update orig)
  {
    if (State == ReplayState.Playing && InputCapture.Counts())
      Step();
    orig();
    if (State != ReplayState.Idle)
      HandleWatcherControls();
  }

  private static MTexture OnGuiButton(
    On.Celeste.Input.orig_GuiButton_VirtualButton_PrefixMode_string orig,
    VirtualButton button,
    Input.PrefixMode mode,
    string fallback
  )
  {
    Binding original = Silenced ? SavedBinding(button) : null;
    if (original == null)
      return orig(button, mode, fallback);
    Binding silenced = button.Binding;
    button.Binding = original;
    try
    {
      return orig(button, mode, fallback);
    }
    finally
    {
      button.Binding = silenced;
    }
  }

  private static Binding SavedBinding(VirtualButton button)
  {
    foreach ((VirtualButton saved, Binding binding) in savedButtons)
      if (saved == button)
        return binding;
    return null;
  }

  private static bool WatcherPressed(Binding binding) =>
    binding.Pressed(Input.Gamepad, WatcherThreshold);

  private static void HandleWatcherControls()
  {
    if (Engine.Scene is Level { Paused: true })
      return;
    if (WatcherPressed(Settings.Instance.Cancel))
    {
      RunManager.LeaveToTrials();
      return;
    }
    if (WatcherPressed(ClimbTrialsModule.Settings.Retry.Button.Binding))
    {
      RunManager.StartRun(RunManager.Run);
      return;
    }
    if (WatcherPressed(Settings.Instance.Journal))
    {
      Audio.Play(RestartSound);
      RunManager.StartReplay(current);
      return;
    }
    if (State != ReplayState.Finished && WatcherPressed(Settings.Instance.Confirm))
    {
      if (State == ReplayState.Paused)
      {
        State = ReplayState.Playing;
        Audio.Play(PauseSound);
      }
      else
      {
        State = ReplayState.Paused;
        Audio.Play(ResumeSound);
      }
    }
  }

  private static void Step()
  {
    if (Engine.RawDeltaTime != current.RawDeltaTime)
    {
      StopOnDesync($"frame time {Engine.RawDeltaTime}, recorded {current.RawDeltaTime}");
      return;
    }
    if (Frame >= current.Frames.Count)
    {
      StopOnDesync("ran out of frames before the finish");
      return;
    }
    InputFrame frame = current.Frames[Frame];
    Player player = (Engine.Scene as Level)?.Tracker.GetEntity<Player>();
    if (frame.HasPlayer != (player != null))
    {
      StopOnDesync($"frame {Frame}: player presence differs");
      return;
    }
    if (player != null)
    {
      if (player.Position != frame.Position)
      {
        StopOnDesync($"frame {Frame}: at {player.Position}, recorded {frame.Position}");
        return;
      }
      if (
        player.StateMachine.State != frame.State
        || player.Dashes != frame.Dashes
        || player.Stamina != frame.Stamina
        || (player.Facing == Facings.Left) != frame.FacingLeft
      )
      {
        StopOnDesync($"frame {Frame}: state, dashes, stamina or facing differ");
        return;
      }
    }

    for (int i = 0; i < buttonNodes.Length; i++)
    {
      Actions flag = InputCapture.Flags[i];
      buttonNodes[i]
        .Feed((frame.Held & flag) != 0, (frame.Pressed & flag) != 0, (frame.Released & flag) != 0);
    }
    Input.grabToggle = frame.GrabToggle;
    moveX.Fed = frame.MoveX;
    moveY.Fed = frame.MoveY;
    gliderMoveY.Fed = frame.GliderMoveY;
    aim.Fed = frame.Aim;
    feather.Fed = frame.Feather;
    Frame++;
  }

  private static void StopOnDesync(string reason)
  {
    Desync(reason);
    RestoreInputs();
    State = ReplayState.Finished;
    if (Engine.Scene is Level level)
      level.Frozen = true;
    RunManager.DiscardAttemptAfterDesync();
  }

  private static void Desync(string reason)
  {
    Desynced = true;
    ClearFed();
    Audio.Play(DesyncSound);
    Logger.Log(LogLevel.Warn, LogTag, $"replay desynced: {reason}");
  }

  private static void ClearFed()
  {
    foreach (ButtonNode node in buttonNodes)
      node.Clear();
    moveX.Fed = 0f;
    moveY.Fed = 0f;
    gliderMoveY.Fed = 0f;
    aim.Fed = Vector2.Zero;
    feather.Fed = Vector2.Zero;
  }

  private static void SilenceAxis(VirtualIntegerAxis axis, AxisNode node)
  {
    savedAxes.Add((axis, [axis.Negative, axis.NegativeAlt, axis.Positive, axis.PositiveAlt]));
    axis.Negative = new Binding();
    axis.NegativeAlt = new Binding();
    axis.Positive = new Binding();
    axis.PositiveAlt = new Binding();
    axis.Nodes.Add(node);
  }

  private static void SilenceStick(VirtualJoystick stick, StickNode node)
  {
    savedSticks.Add(
      (
        stick,
        [
          stick.Up,
          stick.UpAlt,
          stick.Down,
          stick.DownAlt,
          stick.Left,
          stick.LeftAlt,
          stick.Right,
          stick.RightAlt,
        ]
      )
    );
    stick.Up = new Binding();
    stick.UpAlt = new Binding();
    stick.Down = new Binding();
    stick.DownAlt = new Binding();
    stick.Left = new Binding();
    stick.LeftAlt = new Binding();
    stick.Right = new Binding();
    stick.RightAlt = new Binding();
    stick.Nodes.Add(node);
  }

  private sealed class ButtonNode : VirtualButton.Node
  {
    private bool held;
    private bool pressed;
    private bool released;

    public ButtonNode()
    {
      Bufferable = true;
    }

    public override bool Check => held;
    public override bool Pressed => pressed;
    public override bool Released => released;

    public void Clear()
    {
      held = false;
      pressed = false;
      released = false;
    }

    public void Feed(bool isHeld, bool isPressed, bool isReleased)
    {
      held = isHeld;
      pressed = isPressed;
      released = isReleased;
    }
  }

  private sealed class AxisNode : VirtualAxis.Node
  {
    public float Fed;
    public override float Value => Fed;
  }

  private sealed class StickNode : VirtualJoystick.Node
  {
    public Vector2 Fed;
    public override Vector2 Value => Fed;
  }
}
