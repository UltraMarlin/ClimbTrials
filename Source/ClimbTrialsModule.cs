using System;

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
    private static int jumps;

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
        On.Celeste.Player.Jump += Player_Jump;
    }

    public override void Unload()
    {
        On.Celeste.Player.Jump -= Player_Jump;
    }

    private static void Player_Jump(On.Celeste.Player.orig_Jump orig, Player self, bool particles, bool playSfx)
    {
        orig(self, particles, playSfx);
        jumps++;
        Logger.Log(LogLevel.Info, "ClimbTrialsModule/Jump", $"jump #{jumps} at {self.Position}");
    }
}