using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class ModCheck
{
  private static readonly HashSet<string> Allowed = ["Celeste", "Everest", "ClimbTrials"];

  public static List<string> Modules() =>
    Everest.Modules.Select(m => $"{m.Metadata.Name} {m.Metadata.Version}").ToList();

  public static List<string> Others() =>
    Everest
      .Modules.Where(m => !Allowed.Contains(m.Metadata.Name))
      .Select(m => m.Metadata.Name)
      .ToList();

  public static bool OthersLoaded => Others().Count > 0;

  public static void LogModules()
  {
    Logger.Log(LogLevel.Info, "ClimbTrials/Mods", "loaded: " + string.Join(", ", Modules()));
  }
}
