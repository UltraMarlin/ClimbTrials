using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class ReplayStore
{
  private const string LogTag = "ClimbTrials/Replay";

  public static string Folder => Path.Combine(UserIO.GetSaveFilePath(), "ClimbTrials");

  private static string PathFor(string trialId) => Path.Combine(Folder, trialId + ".ctreplay");

  private static HashSet<string> present;

  public static void Refresh()
  {
    present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (!Directory.Exists(Folder))
      return;
    foreach (string file in Directory.GetFiles(Folder, "*.ctreplay"))
      present.Add(Path.GetFileNameWithoutExtension(file));
  }

  private static HashSet<string> Present
  {
    get
    {
      if (present == null)
        Refresh();
      return present;
    }
  }

  public static bool Exists(string trialId) => Present.Contains(trialId);

  public static void Save(Replay replay)
  {
    Stopwatch clock = Stopwatch.StartNew();
    Directory.CreateDirectory(Folder);
    byte[] bytes = replay.ToBytes();
    File.WriteAllBytes(PathFor(replay.TrialId), bytes);
    Present.Add(replay.TrialId);
    int frames = replay.Frames.Count;
    Logger.Log(
      LogLevel.Info,
      LogTag,
      $"saved replay {PathFor(replay.TrialId)}: {frames} frames, "
        + $"{frames * Replay.FrameBytes} raw bytes, {bytes.Length} bytes on disk, "
        + $"{clock.ElapsedMilliseconds} ms"
    );
  }

  public static bool TryLoad(string trialId, out Replay replay, out string failure)
  {
    replay = null;
    if (!File.Exists(PathFor(trialId)))
    {
      failure = "replay file is missing";
      Delete(trialId);
      return false;
    }
    try
    {
      Stopwatch clock = Stopwatch.StartNew();
      replay = Replay.FromBytes(File.ReadAllBytes(PathFor(trialId)));
      replay.IsOwn = true;
      Logger.Log(
        LogLevel.Info,
        LogTag,
        $"loaded replay for '{trialId}': {replay.Frames.Count} frames, "
          + $"{clock.ElapsedMilliseconds} ms"
      );
      failure = null;
      return true;
    }
    catch (Exception e)
    {
      failure = e.Message;
      Logger.Log(LogLevel.Warn, LogTag, $"deleting unreadable replay for '{trialId}': {failure}");
      Delete(trialId);
      return false;
    }
  }

  public static bool Delete(string trialId)
  {
    bool removed = Present.Remove(trialId);
    if (File.Exists(PathFor(trialId)))
    {
      File.Delete(PathFor(trialId));
      removed = true;
    }
    if (!removed)
      return false;
    Logger.Log(LogLevel.Info, LogTag, $"deleted replay for '{trialId}'");
    return true;
  }

  public static int DeleteAll()
  {
    Present.Clear();
    if (!Directory.Exists(Folder))
      return 0;
    string[] files = Directory.GetFiles(Folder, "*.ctreplay");
    foreach (string file in files)
      File.Delete(file);
    Logger.Log(LogLevel.Info, LogTag, $"deleted {files.Length} replay files");
    return files.Length;
  }
}
