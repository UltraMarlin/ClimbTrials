using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;

namespace Celeste.Mod.ClimbTrials.Online;

public static class Identity
{
  private const string LogTag = "ClimbTrials/Identity";

  private static readonly List<Action> afterRegistration = [];
  private static bool saveDue;

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  public static bool Registered =>
    Settings.ClientId?.StartsWith("ct_", StringComparison.Ordinal) == true;

  public static string ClientId => Registered ? Settings.ClientId : null;

  public static bool Banned { get; set; }

  public static bool Registering { get; private set; }

  public static string NameOf(Replay replay) =>
    replay.IsOwn ? Settings.PlayerName : replay.OwnerName;

  public static void EnsureRegistered(Action then = null)
  {
    if (saveDue)
      Save();
    if (Registered)
    {
      then?.Invoke();
      return;
    }
    if (then != null)
      afterRegistration.Add(then);
    if (Registering)
      return;
    Registering = true;
    ServerClient.Register(result =>
    {
      Registering = false;
      if (result.Ok)
        Store(result.Value.ClientId, result.Value.Name);
      List<Action> waiting = [.. afterRegistration];
      afterRegistration.Clear();
      foreach (Action action in waiting)
        action();
    });
  }

  public static void Store(string clientId, string name)
  {
    Settings.ClientId = clientId;
    Settings.PlayerName = name;
    Settings.PlayerNameChosen = false;
    Save();
    Standings.ForgetPlayer();
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
    Standings.MarkStale();
  }

  public static void UpdateFromAnswer(ServerError error, string sentClientId)
  {
    if (error == ServerError.PlayerDeleted && sentClientId == ClientId)
      Forget();
    if (error == ServerError.Banned)
      Banned = true;
    else if (error == ServerError.None)
      Banned = false;
  }

  private static void Forget()
  {
    Logger.Info(LogTag, "the server deleted this player, dropped the client id");
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
