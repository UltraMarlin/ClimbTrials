using System;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Editor;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using FMOD.Studio;
using Monocle;

namespace Celeste.Mod.ClimbTrials;

public class ClimbTrialsModule : EverestModule
{
  public static ClimbTrialsModule Instance { get; private set; }

  public override Type SettingsType => typeof(ClimbTrialsModuleSettings);
  public static ClimbTrialsModuleSettings Settings => (ClimbTrialsModuleSettings)Instance._Settings;

  public override Type SessionType => typeof(ClimbTrialsModuleSession);
  public static ClimbTrialsModuleSession Session => (ClimbTrialsModuleSession)Instance._Session;

  public override Type SaveDataType => typeof(ClimbTrialsModuleSaveData);
  public static ClimbTrialsModuleSaveData SaveData => (ClimbTrialsModuleSaveData)Instance._SaveData;

  public ClimbTrialsModule()
  {
    Instance = this;
#if DEBUG
    Logger.SetLogLevel(nameof(ClimbTrialsModule), LogLevel.Verbose);
#else
    Logger.SetLogLevel(nameof(ClimbTrialsModule), LogLevel.Info);
#endif
  }

  public override void Load()
  {
    Campaigns.Load();
    ServerClient.Load();
    MainMenuEntry.Load();
    PauseMenu.Load();
    MenuWrap.Load();
    RunManager.Load();
    Determinism.Load();
    Recorder.Load();
    Replayer.Load();
    ReplayVerifier.Load();
    PausedEffects.Load();
    RunGuards.Load();
    RunBinds.Load();
    HeartGoal.Load();
    RoomGoal.Load();
    DebugUse.Load();
    TrialDesigner.Load();
    ModCheck.LogModules();
  }

  public override void Initialize()
  {
    ReplayStore.DeleteOutdatedReplays();
    TrialRecords.DeleteOutdatedRecords();
    Events.LoadCopyFromDisk();
    SpeedrunToolBridge.Initialize();
    HotReload.Recover();
  }

  public override void OnInputInitialize()
  {
    if (_Settings != null)
      Settings.CopyMissingBinds();
    base.OnInputInitialize();
  }

  protected override void CreateModMenuSectionKeyBindings(
    TextMenu menu,
    bool inGame,
    EventInstance snapshot
  )
  {
    menu.Add(
      new TextMenu.Button(Dialog.Clean("options_keyconfig")).Pressed(() =>
        OpenBindsScreen(menu, new KeyboardBindsScreen(this))
      )
    );
    menu.Add(
      new TextMenu.Button(Dialog.Clean("options_btnconfig")).Pressed(() =>
        OpenBindsScreen(menu, new ControllerBindsScreen(this))
      )
    );
  }

  private static void OpenBindsScreen(TextMenu menu, TextMenu screen)
  {
    menu.Focused = false;
    screen.OnClose = () => menu.Focused = true;
    Engine.Scene.Add(screen);
    Engine.Scene.OnEndOfFrame += () => Engine.Scene.Entities.UpdateLists();
  }

  public override void SaveSettings()
  {
    if (_Settings != null && !Settings.HasChangedOptions && Settings.DiffersFromDefaults())
      Settings.HasChangedOptions = true;
    base.SaveSettings();
  }

  public override void Unload()
  {
    RunManager.Stop();
    MainMenuEntry.Unload();
    PauseMenu.Unload();
    MenuWrap.Unload();
    RunManager.Unload();
    Determinism.Unload();
    Recorder.Unload();
    Replayer.Unload();
    ReplayVerifier.Unload();
    PausedEffects.Unload();
    RunGuards.Unload();
    HeartGoal.Unload();
    RoomGoal.Unload();
    DebugUse.Unload();
    SpeedrunToolBridge.Unload();
    RunBinds.Unload();
    TrialDesigner.Unload();
  }
}
