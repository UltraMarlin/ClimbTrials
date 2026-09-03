using System;
using System.IO;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class ReplayStore
{
  private const string LogTag = "ClimbTrials/Replay";

  private static string Folder => Path.Combine(UserIO.GetSaveFilePath(), "ClimbTrials");

  private static string PathFor(string trialId) => Path.Combine(Folder, trialId + ".ctreplay");

  public static bool Exists(string trialId) => File.Exists(PathFor(trialId));

  public static void Save(Replay replay)
  {
    Directory.CreateDirectory(Folder);
    using FileStream stream = File.Create(PathFor(replay.TrialId));
    using BinaryWriter writer = new(stream);
    replay.Write(writer);
    Logger.Log(LogLevel.Info, LogTag, $"saved replay {PathFor(replay.TrialId)}");
  }

  public static Replay Load(string trialId)
  {
    if (!Exists(trialId))
      return null;
    try
    {
      using FileStream stream = File.OpenRead(PathFor(trialId));
      using BinaryReader reader = new(stream);
      return Replay.Read(reader);
    }
    catch (Exception e) when (e is IOException or InvalidDataException)
    {
      Logger.Log(LogLevel.Warn, LogTag, $"could not read replay for '{trialId}': {e.Message}");
      return null;
    }
  }

  public static void DeleteAll()
  {
    if (!Directory.Exists(Folder))
      return;
    string[] files = Directory.GetFiles(Folder, "*.ctreplay");
    foreach (string file in files)
      File.Delete(file);
    Logger.Log(LogLevel.Info, LogTag, $"deleted {files.Length} replay files");
  }
}
