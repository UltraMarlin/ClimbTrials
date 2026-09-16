using System;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;

namespace Celeste.Mod.ClimbTrials.Online;

public enum RegistrationState
{
  NotRegistered,
  Registering,
  Registered,
  Failed,
}

public static class Identity
{
  private const string LogTag = "ClimbTrials/Identity";

  private static bool registering;
  private static bool failed;
  private static bool saveDue;

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  public static bool Registered =>
    Settings.ClientId?.StartsWith("ct_", StringComparison.Ordinal) == true;

  public static string ClientId => Registered ? Settings.ClientId : null;

  public static string Key => string.IsNullOrEmpty(Settings.Key) ? null : Settings.Key;

  public static bool Banned { get; set; }

  public static RegistrationState Registration =>
    Registered ? RegistrationState.Registered
    : registering ? RegistrationState.Registering
    : failed ? RegistrationState.Failed
    : RegistrationState.NotRegistered;

  public static string NameOf(Replay replay) =>
    replay.IsOwn ? Settings.PlayerName : replay.OwnerName;

  public static void EnsureRegistered()
  {
    if (saveDue)
      Save();
    if (Registered || registering)
      return;
    registering = true;
    failed = false;
    ServerClient.Register(result =>
    {
      registering = false;
      if (result.Ok)
        Store(result.Value.ClientId, result.Value.Name);
      else
        failed = true;
    });
  }

  public static void Store(string clientId, string name)
  {
    Settings.ClientId = clientId;
    Settings.PlayerName = name;
    Settings.PlayerNameChosen = false;
    Save();
  }

  public static void AdoptName(string name)
  {
    Settings.PlayerName = name;
    Save();
  }

  public static void AdoptChosenName(string name)
  {
    Settings.PlayerName = name;
    Settings.PlayerNameChosen = true;
    Save();
  }

  public static void Forget()
  {
    Logger.Log(LogLevel.Info, LogTag, "the server did not know this client id, dropped it");
    Store("", "");
  }

  private static void Save()
  {
    if (RunManager.AttemptInProgress)
    {
      saveDue = true;
      return;
    }
    saveDue = false;
    ClimbTrialsModule.Instance.SaveSettings();
  }
}
