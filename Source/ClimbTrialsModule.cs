using System;
using Celeste.Mod.ClimbTrials.Entities;
using Microsoft.Xna.Framework;

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
    Everest.Events.Level.OnLoadLevel += LoadLevel;
  }

  public override void Unload()
  {
    Everest.Events.Level.OnLoadLevel -= LoadLevel;
  }

  private static void LoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
  {
    level.Add(new Countdown());
    Logger.Log(LogLevel.Info, "ClimbTrialsModule/LoadLevel", "Level Loaded, spawned Countdown");
  }
}