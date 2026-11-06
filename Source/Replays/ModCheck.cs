using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.ClimbTrials.Runs;

namespace Celeste.Mod.ClimbTrials.Replays;

public readonly record struct ModuleRef(string Name, string Version)
{
  public override string ToString() => $"{Name} {Version}";
}

public static class ModCheck
{
  public const int NamesShown = 3;

  private static readonly IReadOnlySet<string> AlwaysAllowed = new HashSet<string>
  {
    "Celeste",
    "Everest",
    "ClimbTrials",
  };

  private static readonly IReadOnlySet<string> AlwaysAllowedAndSpeedrunTool = new HashSet<string>(
    AlwaysAllowed
  )
  {
    "SpeedrunTool",
  };

  public static IReadOnlySet<string> Allowed =>
    SpeedrunToolBridge.Registered ? AlwaysAllowedAndSpeedrunTool : AlwaysAllowed;

  public static List<ModuleRef> Modules() =>
    Everest
      .Modules.Select(module => new ModuleRef(
        module.Metadata.Name,
        module.Metadata.Version.ToString()
      ))
      .ToList();

  public static List<string> Others() =>
    Everest
      .Modules.Where(module => !Allowed.Contains(module.Metadata.Name))
      .Select(module => module.Metadata.Name)
      .ToList();

  public static bool OthersLoaded => Others().Count > 0;

  public static string Describe(IReadOnlyList<string> names)
  {
    if (names.Count <= NamesShown)
      return string.Join(", ", names);
    return string.Join(", ", names.Take(NamesShown)) + $" and {names.Count - NamesShown} more";
  }

  public static string RunWarning() => Warning("climbtrials_recording_mods");

  public static string MenuWarning() => Warning("climbtrials_menu_mods");

  private static string Warning(string leadKey) =>
    Dialog.Clean(leadKey) + " " + Describe(Others()) + ". ";
}
