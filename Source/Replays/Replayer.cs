using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays.Codec;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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
  private const string DesyncSound = SFX.ui_main_button_invalid;
  private const string PauseSound = SFX.game_gen_cassetteblock_switch_1;
  private const string ResumeSound = SFX.game_gen_cassetteblock_switch_2;
  private const string RestartSound = SFX.ui_main_button_select;
  private const float WatcherThreshold = 0.2f;
  private const float StepThreshold = 0.4f;
  private const float MenuThreshold = 0.4f;
  private const int StepDelayTicks = 30;
  private const int StepRepeatTicks = 4;

  public static ReplayState State { get; private set; }
  public static bool Desynced { get; private set; }
  public static bool StoppedByReload { get; private set; }
  public static bool PauseQueued { get; private set; }
  public static int Frame { get; private set; }
  public static bool PausedOnFinish { get; private set; }
  public static bool Silenced =>
    (State is ReplayState.Playing or ReplayState.Paused) && !PausedOnFinish;
  public static bool MenuOpen => menu != null || modOptions != null;

  private static Replay current;
  private static bool timePending;
  private static int pauseMenusShown;
  private static bool stepping;
  private static int stepHeldTicks;
  private static WatcherMenu menu;
  private static LeaderboardView board;
  private static bool resumeOnClose;
  private static bool pauseEffectsHeld;
  private static TextMenu recordedMenu;
  private static TextMenu modOptions;
  private static float recordedMenuAlpha;
  private static bool menuStarted;
  private static Level watchedLevel;

  private static readonly ButtonNode[] buttonNodes = new ButtonNode[HeaderCoding.ButtonCount];
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
    pauseMenusShown = 0;
    Frame = 0;
    Desynced = false;
    StoppedByReload = false;
    PausedOnFinish = false;
    stepping = false;
    stepHeldTicks = 0;

    savedGrabMode = Settings.Instance.GrabMode;
    savedCrouchDashMode = Settings.Instance.CrouchDashMode;
    savedScreenShake = Settings.Instance.ScreenShake;
    ApplyRecordedSettings();

    savedLanguage = Dialog.Language;
    if (Dialog.Languages.TryGetValue(replay.Language, out Language language))
    {
      Fonts.Load(language.FontFace);
      Dialog.Language = language;
    }

    watchedLevel = level;
    WatcherOverlay.Begin();
    timePending = true;
    level.AllowHudHide = false;

    VirtualButton[] buttons = InputCapture.Buttons();
    for (int i = 0; i < buttons.Length; i++)
    {
      savedButtons.Add((buttons[i], buttons[i].Binding));
      buttons[i].Binding = new Binding();
      buttons[i].Nodes.Add(buttonNodes[i]);
    }
    InputCapture.RestoreButtonStates(replay.Buttons);
    ClearReplayedInputs();
    SilenceAxis(Input.MoveX, moveX);
    SilenceAxis(Input.MoveY, moveY);
    SilenceAxis(Input.GliderMoveY, gliderMoveY);
    SilenceStick(Input.Aim, aim);
    SilenceStick(Input.Feather, feather);

    State = PauseQueued ? ReplayState.Paused : ReplayState.Playing;
    PauseQueued = false;
  }

  public static void Finish(long elapsedTicks)
  {
    if (current == null)
      return;

    int missing = current.Frames.Count - Frame;
    RestoreInputs();
    State = ReplayState.Finished;
    if (Desynced)
      return;

    if (missing != 0)
    {
      Desync($"finished {missing} frames early");
      return;
    }
    if (elapsedTicks != current.Ticks)
    {
      Desync(
        $"finished at {TimeText.Format(elapsedTicks)}, "
          + $"the recording is {TimeText.Format(current.Ticks)}"
      );
      return;
    }
    if (stepping)
    {
      PausedOnFinish = true;
      State = ReplayState.Paused;
    }
  }

  private static void ReleaseFinish()
  {
    PausedOnFinish = false;
    State = ReplayState.Finished;
  }

  public static void Clear()
  {
    RestoreInputs();
    State = ReplayState.Idle;
    current = null;
    timePending = false;
    Desynced = false;
    StoppedByReload = false;
    PauseQueued = false;
    PausedOnFinish = false;
    stepping = false;
    ReleasePauseAudio();
    RestoreRecordedMenu();
    WatcherOverlay.End();
    menu = null;
    board = null;
    DropModOptions();
    resumeOnClose = false;
    watchedLevel = null;
  }

  public static void ApplyRecordedTime(Level level)
  {
    if (!timePending || current == null)
      return;
    timePending = false;
    level.TimeActive = current.TimeActive;
    level.RawTimeActive = current.RawTimeActive;
  }

  public static InputFrame? ReplayedFrame(int framesBack = 0)
  {
    int index = Frame - 1 - framesBack;
    if ((!Silenced && !PausedOnFinish) || index < 0 || index >= current.Frames.Count)
      return null;
    return current.Frames[index];
  }

  public static void ApplyRecordedSettings()
  {
    if (current == null)
      return;
    Settings.Instance.GrabMode = current.GrabMode;
    Settings.Instance.CrouchDashMode = current.CrouchDashMode;
    Settings.Instance.ScreenShake = current.ScreenShake;
  }

  public static void RestorePlayerSettings()
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
    PauseMenu.ResetBuiltMenu();
    if (quickReset && RunManager.Replaying && !Silenced)
      return;
    if (current == null && RunManager.Replaying)
    {
      OpenMenu();
      return;
    }
    if (current == null)
    {
      orig(self, startIndex, minimal, quickReset);
      TextMenu pauseMenu = PauseMenu.GetAndResetBuiltMenu();
      if (pauseMenu != null)
        Recorder.RecordPauseMenu(pauseMenu);
      return;
    }
    orig(self, startIndex, minimal, quickReset);
    RebuildRecordedPauseMenu(PauseMenu.GetAndResetBuiltMenu());
  }

  private static void RebuildRecordedPauseMenu(TextMenu built)
  {
    if (built == null || !Silenced)
      return;
    if (pauseMenusShown < current.PauseMenus.Count)
      PauseRows.Rebuild(built, current.PauseMenus[pauseMenusShown]);
    pauseMenusShown++;
  }

  private static void RestoreInputs()
  {
    if (!Silenced)
      return;

    foreach ((VirtualButton button, Binding binding) in savedButtons)
    {
      button.Binding = binding;
      button.Nodes.RemoveAll(node => node is ButtonNode);
    }
    savedButtons.Clear();
    foreach ((VirtualIntegerAxis axis, Binding[] bindings) in savedAxes)
    {
      axis.Negative = bindings[0];
      axis.NegativeAlt = bindings[1];
      axis.Positive = bindings[2];
      axis.PositiveAlt = bindings[3];
      axis.Nodes.RemoveAll(node => node is AxisNode);
    }
    savedAxes.Clear();
    foreach ((VirtualJoystick stick, Binding[] bindings) in savedSticks)
    {
      stick.Up = bindings[0];
      stick.UpAlt = bindings[1];
      stick.Down = bindings[2];
      stick.DownAlt = bindings[3];
      stick.Left = bindings[4];
      stick.LeftAlt = bindings[5];
      stick.Right = bindings[6];
      stick.RightAlt = bindings[7];
      stick.Nodes.RemoveAll(node => node is StickNode);
    }
    savedSticks.Clear();

    RestorePlayerSettings();
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
    if (State != ReplayState.Paused && menu == null)
    {
      stepHeldTicks = 0;
      orig(self, gameTime);
      WatcherOverlay.Update(gameTime, engineSkipped: false);
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
      MInput.Mouse.Update();
      MInput.GamePads[Input.Gamepad].Update();
      bool menuWasOpen = menu != null;
      if (menuWasOpen && !menuStarted)
        StartMenu();
      WatcherOverlay.Update(gameTime, engineSkipped: true);
      if (menuWasOpen)
        FollowMenu();
      else
      {
        HandleWatcherControls();
        if (menu == null && State == ReplayState.Paused && StepRequested())
        {
          if (!PausedOnFinish)
            StepOnce(orig, self, gameTime);
          else if (stepHeldTicks == 1)
            ReleaseFinish();
          return;
        }
      }
    }
    else
    {
      stepHeldTicks = 0;
    }

    if (State == ReplayState.Idle && menu == null)
      orig(self, gameTime);
    else if (Engine.Commands.Open)
      Engine.Commands.UpdateOpen();
    else if (Engine.Commands.Enabled)
      Engine.Commands.UpdateClosed();
  }

  private static void FollowMenu()
  {
    if (menu == null)
      return;
    recordedMenu?.Alpha = recordedMenuAlpha * menu.DimUnder;
    if (!menu.Closed)
      return;

    RestoreRecordedMenu();
    menu = null;
    if (resumeOnClose && State == ReplayState.Paused)
      State = ReplayState.Playing;
  }

  private static void OpenMenu()
  {
    if (menu != null)
      return;
    resumeOnClose = State == ReplayState.Playing;
    if (resumeOnClose)
      State = ReplayState.Paused;
    stepHeldTicks = 0;
    if (State == ReplayState.Finished || PausedOnFinish)
    {
      Input.Pause.ConsumePress();
      Input.ESC.ConsumePress();
    }
    menuStarted = false;
    menu = new WatcherMenu(
      ResumeReplay,
      PlayTrial,
      RaceGhost,
      OpenBoard,
      OpenModOptions,
      LeaveReplay
    );
    WatcherOverlay.Add(menu);
  }

  private static void OpenBoard()
  {
    if (menu == null || menu.Covered || RunManager.Trial == null)
      return;
    menu.Covered = true;
    menu.RowsAlpha = 0f;
    WatcherOverlay.Add(
      board = new LeaderboardView(
        BoardSubject.ForTrialAlone(RunManager.Trial),
        _ => { },
        _ => CloseBoard(),
        LeaveForGhost,
        options: BoardOptions.InLevel(MenuKeys.Watcher, comparing: true)
      )
    );
  }

  private static void CloseBoard()
  {
    board = null;
    if (menu == null)
      return;
    menu.RowsAlpha = 0f;
    if (GhostComparisons.SwitchingTo is Replay next)
    {
      bool paused = current == null ? PauseQueued : State == ReplayState.Paused && !resumeOnClose;
      if (RestartReplay(next, paused))
        return;
      GhostComparisons.CancelSwitch();
    }
    ResumeReplay();
  }

  private static void LeaveForGhost(Trial trial, Replay replay, bool race)
  {
    DropMenu();
    LevelBoard.StartGhost(trial, replay, race);
  }

  private static void DropMenu()
  {
    ReleasePauseAudio();
    RestoreRecordedMenu();
    board?.Discard();
    board = null;
    menu?.RemoveSelf();
    menu = null;
  }

  private static void StartMenu()
  {
    menuStarted = true;
    Level level = Engine.Scene as Level;
    pauseEffectsHeld = level != null && !level.Paused;
    if (pauseEffectsHeld)
      level.StartPauseEffects();
    else
      Audio.Play(SFX.ui_game_pause);
    recordedMenu = level?.Paused == true ? level.Entities.FindFirst<TextMenu>() : null;
    recordedMenuAlpha = recordedMenu?.Alpha ?? 1f;
  }

  private static void RestoreRecordedMenu()
  {
    recordedMenu?.Alpha = recordedMenuAlpha;
    recordedMenu = null;
  }

  private static void ResumeReplay()
  {
    ReleasePauseAudio();
    Audio.Play(SFX.ui_game_unpause);
    menu.Dismiss();
  }

  private static void PlayTrial()
  {
    DropMenu();
    RunManager.StartTrial(RunManager.Trial);
  }

  private static void RaceGhost()
  {
    DropMenu();
    RunManager.StartRace(RunManager.Trial, RunManager.Replay);
  }

  private static void LeaveReplay()
  {
    DropMenu();
    RunManager.LeaveToTrials();
  }

  private static void OpenModOptions()
  {
    if (Engine.Scene is not Level level)
      return;
    menu.RemoveSelf();
    menu = null;
    recordedMenu?.RemoveSelf();
    recordedMenu = null;
    EndPlaybackEarly(level);
    SpeedrunToolBridge.ResumeAfterReplay();
    RunBinds.Restore();
    level.PauseMainMenuOpen = false;
    level.Paused = true;
    Replay replay = RunManager.Replay;
    TextMenu options = OuiModOptions.CreateMenu(inGame: true, Level._PauseSnapshot);
    Action startSearching = OuiModOptions.AddSearchBox(options);
    options.OnUpdate = () =>
    {
      if (options.Focused && Input.QuickRestart.Pressed)
        startSearching();
    };
    Action close = () => CloseModOptions(options, replay);
    options.OnESC = close;
    options.OnCancel = close;
    options.OnPause = close;
    modOptions = options;
    level.Add(options);
  }

  private static void CloseModOptions(TextMenu options, Replay replay)
  {
    if (options.Focused)
      options.CloseAndRun(Everest.SaveSettings(), () => RestartAfterModOptions(replay));
  }

  private static void RestartAfterModOptions(Replay replay)
  {
    modOptions = null;
    ReleasePauseAudio();
    if (!RestartReplay(replay, paused: false))
      RunManager.LeaveToTrials();
  }

  private static void DropModOptions()
  {
    modOptions?.RemoveSelf();
    modOptions = null;
  }

  private static void ReleasePauseAudio()
  {
    Level level = Engine.Scene as Level ?? watchedLevel;
    if (pauseEffectsHeld && level != null)
      level.EndPauseEffects();
    pauseEffectsHeld = false;
  }

  public static Binding EscapeBinding() => SavedBinding(Input.ESC) ?? Input.ESC.Binding;

  public static bool MenuPressed(Binding binding) => binding.Pressed(Input.Gamepad, MenuThreshold);

  public static bool MenuHeld(Binding binding) => binding.Check(Input.Gamepad, MenuThreshold);

  private static void StepOnce(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime)
  {
    stepping = true;
    try
    {
      orig(self, gameTime);
    }
    finally
    {
      stepping = false;
    }
  }

  private static bool StepRequested()
  {
    Binding step = ClimbTrialsModule.Settings.ReplayFrameAdvance.Binding;
    if (step.Pressed(Input.Gamepad, StepThreshold))
    {
      stepHeldTicks = 1;
      return true;
    }
    if (stepHeldTicks == 0 || !step.Check(Input.Gamepad, StepThreshold))
    {
      stepHeldTicks = 0;
      return false;
    }
    stepHeldTicks++;
    int pastDelay = stepHeldTicks - StepDelayTicks - 1;
    return pastDelay >= 0 && pastDelay % StepRepeatTicks == 0;
  }

  private static void OnMInputUpdate(On.Monocle.MInput.orig_Update orig)
  {
    if ((State == ReplayState.Playing || stepping) && InputCapture.Counts())
      Step();

    orig();

    if (Engine.Commands.Open || Engine.Scene is not Level)
      return;

    if (State != ReplayState.Idle)
      HandleWatcherControls();
    else if (WaitingToStart)
      HandleControlsBeforeStart();
  }

  public static bool WaitingToStart =>
    RunManager.Replaying && Engine.Scene is Level level && !level.Paused;

  private static void HandleControlsBeforeStart()
  {
    ToggleHitboxesOnPress();
    if (WatcherPressed(ClimbTrialsModule.Settings.ReplayRestart))
    {
      RestartReplay(RunManager.Replay, PauseQueued);
      return;
    }
    if (WatcherPressed(ClimbTrialsModule.Settings.ReplayPlayPause))
    {
      PauseQueued = !PauseQueued;
      Audio.Play(PauseQueued ? PauseSound : ResumeSound);
    }
  }

  private static bool RestartReplay(Replay replay, bool paused)
  {
    if (!RunManager.StartReplay(replay))
      return false;
    Audio.Play(RestartSound);
    PauseQueued = paused;
    return true;
  }

  private static void ToggleHitboxesOnPress()
  {
    if (WatcherPressed(ClimbTrialsModule.Settings.ReplayShowHitboxes))
      GameplayRenderer.RenderDebug = !GameplayRenderer.RenderDebug;
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

  public static bool WatcherPressed(Binding binding) =>
    binding.Pressed(Input.Gamepad, WatcherThreshold);

  private static bool WatcherPressed(ButtonBinding bind) => WatcherPressed(bind.Binding);

  private static void HandleWatcherControls()
  {
    if (!RunManager.Replaying || modOptions != null)
      return;

    ToggleHitboxesOnPress();
    if (WatcherPressed(Settings.Instance.Pause) || WatcherPressed(EscapeBinding()))
    {
      OpenMenu();
      return;
    }

    if (WatcherPressed(ClimbTrialsModule.Settings.ReplayRestart))
    {
      RestartReplay(RunManager.Replay, State == ReplayState.Paused);
      return;
    }

    if (State == ReplayState.Finished && WatcherPressed(Settings.Instance.Cancel))
    {
      LeaveReplay();
      return;
    }

    if (State != ReplayState.Finished && WatcherPressed(ClimbTrialsModule.Settings.ReplayPlayPause))
    {
      if (PausedOnFinish)
      {
        ReleaseFinish();
        Audio.Play(ResumeSound);
      }
      else if (State == ReplayState.Paused)
      {
        State = ReplayState.Playing;
        Audio.Play(ResumeSound);
      }
      else
      {
        State = ReplayState.Paused;
        Audio.Play(PauseSound);
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
        .SetReplayedState(
          (frame.Held & flag) != 0,
          (frame.Pressed & flag) != 0,
          (frame.Released & flag) != 0
        );
    }
    Input.grabToggle = frame.GrabToggle;
    moveX.Replayed = frame.MoveX;
    moveY.Replayed = frame.MoveY;
    gliderMoveY.Replayed = frame.GliderMoveY;
    aim.Replayed = frame.Aim;
    feather.Replayed = frame.Feather;
    Frame++;
  }

  public static void StopOnReload(Level level)
  {
    StoppedByReload = true;
    DropModOptions();
    AnnounceStop("replay stopped: the level was reloaded");
    EndPlaybackEarly(level);
  }

  private static void StopOnDesync(string reason)
  {
    Desync(reason);
    EndPlaybackEarly(Engine.Scene as Level);
  }

  private static void EndPlaybackEarly(Level level)
  {
    RestoreInputs();
    PausedOnFinish = false;
    State = ReplayState.Finished;
    if (level != null)
      level.Frozen = true;
    RunManager.DiscardAttemptAfterDesync();
  }

  private static void Desync(string reason)
  {
    Desynced = true;
    AnnounceStop($"replay desynced: {reason}");
  }

  private static void AnnounceStop(string message)
  {
    ClearReplayedInputs();
    Audio.Play(DesyncSound);
    Logger.Log(LogLevel.Warn, LogTag, message);
  }

  private static void ClearReplayedInputs()
  {
    foreach (ButtonNode node in buttonNodes)
      node.Clear();
    moveX.Replayed = 0f;
    moveY.Replayed = 0f;
    gliderMoveY.Replayed = 0f;
    aim.Replayed = Vector2.Zero;
    feather.Replayed = Vector2.Zero;
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

    public void SetReplayedState(bool isHeld, bool isPressed, bool isReleased)
    {
      held = isHeld;
      pressed = isPressed;
      released = isReleased;
    }
  }

  private sealed class AxisNode : VirtualAxis.Node
  {
    public float Replayed;
    public override float Value => Replayed;
  }

  private sealed class StickNode : VirtualJoystick.Node
  {
    public Vector2 Replayed;
    public override Vector2 Value => Replayed;
  }
}
