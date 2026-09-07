using System;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Celeste.Mod.ClimbTrials.Editor;

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
    // debug builds use verbose logging
    Logger.SetLogLevel(nameof(ClimbTrialsModule), LogLevel.Verbose);
#else
    // release builds use info logging to reduce spam in log files
    Logger.SetLogLevel(nameof(ClimbTrialsModule), LogLevel.Info);
#endif
  }

  public override void Load()
  {
    MainMenuEntry.Load();
    Identity.CreateClientIdIfMissing();
    PauseMenu.Load();
    RunManager.Load();
    Determinism.Load();
    Recorder.Load();
    Replayer.Load();
    RunGuards.Load();
    TrialDesigner.Load();
    ModCheck.LogModules();
  }

  public override void Initialize()
  {
    HotReload.Recover();
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
    TrialDesigner.Unload();
  }
}
