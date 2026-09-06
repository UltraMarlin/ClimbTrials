using System;

namespace Celeste.Mod.ClimbTrials.Online;

public static class Identity
{
  private const string LogTag = "ClimbTrials/Identity";

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  public static string ClientId => Settings.ClientId;

  public static string Key => string.IsNullOrEmpty(Settings.Key) ? null : Settings.Key;

  public static string DisplayName =>
    string.IsNullOrEmpty(Settings.PlayerName)
      ? Dialog.Clean("climbtrials_replay_you")
      : Settings.PlayerName;

  public static void CreateClientIdIfMissing()
  {
    if (!string.IsNullOrEmpty(Settings.ClientId))
      return;

    Settings.ClientId = Guid.NewGuid().ToString("N");
    ClimbTrialsModule.Instance.SaveSettings();
    Logger.Log(LogLevel.Info, LogTag, "no client id in the settings, generated one");
  }
}
