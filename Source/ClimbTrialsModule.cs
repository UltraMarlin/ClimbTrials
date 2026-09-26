using System;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Editor;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;

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
    RunManager.Load();
    Determinism.Load();
    Recorder.Load();
    Replayer.Load();
    RunGuards.Load();
    HeartGoal.Load();
    RoomGoal.Load();
    DebugUse.Load();
    TrialDesigner.Load();
    ModCheck.LogModules();
  }

  public override void Initialize()
  {
    HotReload.Recover();
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
    RunManager.Unload();
    Determinism.Unload();
    Recorder.Unload();
    Replayer.Unload();
    RunGuards.Unload();
    HeartGoal.Unload();
    RoomGoal.Unload();
    DebugUse.Unload();
    RunBinds.Unload();
    TrialDesigner.Unload();
  }
}
