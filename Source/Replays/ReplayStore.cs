using System;
using System.Diagnostics;
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
    Stopwatch clock = Stopwatch.StartNew();
    Directory.CreateDirectory(Folder);
    using FileStream stream = File.Create(PathFor(replay.TrialId));
    using BinaryWriter writer = new(stream);
    replay.Write(writer);
    writer.Flush();
    int frames = replay.Frames.Count;
    Logger.Log(
      LogLevel.Info,
      LogTag,
      $"saved replay {PathFor(replay.TrialId)}: {frames} frames, "
        + $"{frames * Replay.FrameBytes} raw bytes, {stream.Length} bytes on disk, "
        + $"{clock.ElapsedMilliseconds} ms"
    );
  }

  public static Replay Load(string trialId)
  {
    if (!Exists(trialId))
      return null;
    try
    {
      Stopwatch clock = Stopwatch.StartNew();
      using FileStream stream = File.OpenRead(PathFor(trialId));
      using BinaryReader reader = new(stream);
      Replay replay = Replay.Read(reader);
      Logger.Log(
        LogLevel.Info,
        LogTag,
        $"loaded replay for '{trialId}': {replay.Frames.Count} frames, "
          + $"{clock.ElapsedMilliseconds} ms"
      );
      return replay;
    }
    catch (Exception e) when (e is IOException or InvalidDataException)
    {
      Logger.Log(LogLevel.Warn, LogTag, $"deleting unreadable replay for '{trialId}': {e.Message}");
      Delete(trialId);
      return null;
    }
  }

  public static void Delete(string trialId)
  {
    if (!Exists(trialId))
      return;
    File.Delete(PathFor(trialId));
    Logger.Log(LogLevel.Info, LogTag, $"deleted replay for '{trialId}'");
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
