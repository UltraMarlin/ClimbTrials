using System;
using Celeste.Mod.ClimbTrials.Replays;

namespace Celeste.Mod.ClimbTrials.Online;

public static class Identity
{
  private const string LogTag = "ClimbTrials/Identity";

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  public static bool Registered =>
    Settings.ClientId?.StartsWith("ct_", StringComparison.Ordinal) == true;

  public static string ClientId => Registered ? Settings.ClientId : null;

  public static string Key => string.IsNullOrEmpty(Settings.Key) ? null : Settings.Key;

  public static string NameOf(Replay replay) =>
    replay.IsOwn ? Settings.PlayerName : replay.OwnerName;

  public static void Store(string clientId, string name)
  {
    Settings.ClientId = clientId;
    Settings.PlayerName = name;
    ClimbTrialsModule.Instance.SaveSettings();
  }

  public static void Forget()
  {
    Logger.Log(LogLevel.Info, LogTag, "the server did not know this client id, dropped it");
    Store("", "");
  }
}
