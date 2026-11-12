using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Celeste.Mod.ClimbTrials.Replays;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Runs;

public static class AllowedModsRestart
{
  private const string BlacklistName = "climbtrials-blacklist.txt";

  public static void Restart()
  {
    WriteBlacklist();
    Everest.Events.Celeste.OnShutdown += StartGame;
    Engine.Scene = new Scene();
    Engine.Instance.Exit();
  }

  private static void WriteBlacklist()
  {
    IEnumerable<string> allowedModFiles = Everest
      .Modules.Where(module => ModCheck.Allowed.Contains(module.Metadata.Name))
      .Select(module => FileNameOf(module.Metadata));

    IEnumerable<string> otherModFiles = Directory
      .GetDirectories(Everest.Loader.PathMods)
      .Concat(Directory.GetFiles(Everest.Loader.PathMods, "*.zip"))
      .Select(Path.GetFileName)
      .Except(allowedModFiles.Append("Cache"));

    File.WriteAllLines(Path.Combine(Everest.Loader.PathMods, BlacklistName), otherModFiles);
  }

  private static string FileNameOf(EverestModuleMetadata metadata) =>
    Path.GetFileName(
      string.IsNullOrEmpty(metadata.PathArchive) ? metadata.PathDirectory : metadata.PathArchive
    );

  private static void StartGame()
  {
    string executable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
      ? "Celeste.exe"
      : "Celeste";
    ProcessStartInfo startInfo = new(Path.Combine(AppContext.BaseDirectory, executable))
    {
      WorkingDirectory = AppContext.BaseDirectory,
    };
    foreach (string argument in Environment.GetCommandLineArgs().Skip(1))
      startInfo.ArgumentList.Add(argument);
    startInfo.ArgumentList.Add("--blacklist");
    startInfo.ArgumentList.Add(BlacklistName);
    foreach (string variable in startInfo.Environment.Keys.ToArray())
      if (variable.StartsWith("FNA_") || variable.StartsWith("FNA3D_"))
        startInfo.Environment.Remove(variable);
    Process.Start(startInfo);
  }
}
