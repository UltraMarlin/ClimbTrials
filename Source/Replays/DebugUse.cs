using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.ClimbTrials.Runs;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Replays;

[Flags]
public enum DebugCause
{
  None = 0,
  ConsoleUsed = 1,
  SaveStateUsed = 2,
  SpeedrunToolEnabled = 4,
}

public static class DebugUse
{
  private const string LogTag = "ClimbTrials/Debug";

  public static DebugCause Causes { get; private set; }
  private static readonly List<DebugCause> order = [];

  public static void Load()
  {
    On.Monocle.Commands.ExecuteCommand += OnExecuteCommand;
  }

  public static void Unload()
  {
    On.Monocle.Commands.ExecuteCommand -= OnExecuteCommand;
  }

  public static void Reset()
  {
    Causes = DebugCause.None;
    order.Clear();
  }

  public static void CheckFrame()
  {
    if (Engine.Commands.Open)
      Add(DebugCause.ConsoleUsed);
  }

  public static void Add(DebugCause cause)
  {
    if (Record(cause))
      Notify(cause);
  }

  public static bool Record(DebugCause cause)
  {
    if (RunManager.Run == null || RunManager.Replaying || Causes.HasFlag(cause))
      return false;
    Causes |= cause;
    order.Add(cause);
    Logger.Log(LogLevel.Info, LogTag, $"run '{RunManager.Trial?.Id}' debug use: {cause}");
    return true;
  }

  public static void Notify(DebugCause cause) =>
    RunManager.ShowNotice("climbtrials_notice_debug_" + Key(cause));

  public static string RunWarning() =>
    Dialog.Clean("climbtrials_recording_debug")
    + " "
    + string.Join(", ", order.Select(cause => Dialog.Clean("climbtrials_debug_" + Key(cause))))
    + ". ";

  private static string Key(DebugCause cause) =>
    cause switch
    {
      DebugCause.ConsoleUsed => "console",
      DebugCause.SaveStateUsed => "savestate",
      DebugCause.SpeedrunToolEnabled => "speedruntool",
      _ => throw new ArgumentOutOfRangeException(nameof(cause), cause, null),
    };

  private static void OnExecuteCommand(
    On.Monocle.Commands.orig_ExecuteCommand orig,
    Monocle.Commands self,
    string command,
    string[] args
  )
  {
    Add(DebugCause.ConsoleUsed);
    orig(self, command, args);
  }
}
