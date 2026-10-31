using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays.Codec;

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
    byte[] bytes;
    try
    {
      bytes = replay.ToBytes();
    }
    catch (StartingValuesException e)
    {
      Logger.Log(
        LogLevel.Error,
        LogTag,
        $"did not save the replay for '{replay.TrialId}', the old file stays: " + e.Message
      );
      return;
    }
    Directory.CreateDirectory(Folder);
    File.WriteAllBytes(PathFor(replay.TrialId), bytes);
    Present.Add(replay.TrialId);
    int frames = replay.Frames.Count;
    Logger.Log(
      LogLevel.Info,
      LogTag,
      $"saved replay {Path.GetFileName(PathFor(replay.TrialId))}: {frames} frames, "
        + $"{frames * FrameTable.FrameBytes} raw bytes, {bytes.Length} bytes compressed "
        + $"in {clock.ElapsedMilliseconds} ms"
    );
  }

  public static bool TryLoadPb(Trial trial, out Replay replay, out string failureNotice)
  {
    replay = null;
    failureNotice = null;
    if (!Exists(trial.Id))
    {
      Audio.Play(SFX.ui_main_button_invalid);
      return false;
    }
    return TryLoad(trial, out replay, out failureNotice);
  }

  public static bool TryLoad(Trial trial, out Replay replay, out string failureNotice)
  {
    replay = null;
    string trialId = trial.Id;
    if (!File.Exists(PathFor(trialId)))
    {
      failureNotice = Dialog.Clean("climbtrials_replay_deleted");
      Delete(trialId);
      return false;
    }
    try
    {
      Stopwatch clock = Stopwatch.StartNew();
      byte[] bytes = File.ReadAllBytes(PathFor(trialId));
      replay = Replay.FromBytes(bytes);
      if (!replay.MatchesTrialHash(trial.AcceptedTrialHashes))
      {
        replay = null;
        failureNotice = Dialog.Clean("climbtrials_replay_deleted_trial_changed");
        Logger.Log(
          LogLevel.Info,
          LogTag,
          $"deleting replay for '{trialId}', the trial has changed"
        );
        Delete(trialId);
        return false;
      }
      replay.IsOwn = true;
      replay.IsPersonalBest = true;
      Logger.Log(
        LogLevel.Info,
        LogTag,
        $"loaded replay {Path.GetFileName(PathFor(trialId))}: {replay.Frames.Count} frames, "
          + $"{replay.Frames.Count * FrameTable.FrameBytes} raw bytes, "
          + $"{bytes.Length} bytes compressed in {clock.ElapsedMilliseconds} ms"
      );
      failureNotice = null;
      return true;
    }
    catch (InvalidDataException e)
    {
      replay = null;
      failureNotice = Dialog.Clean("climbtrials_replay_deleted");
      Logger.Log(LogLevel.Warn, LogTag, $"deleting unreadable replay for '{trialId}': {e.Message}");
      Delete(trialId);
      return false;
    }
    catch (Exception e)
    {
      replay = null;
      failureNotice = Dialog.Clean("climbtrials_replay_kept_unreadable");
      Logger.Log(
        LogLevel.Error,
        LogTag,
        $"kept replay for '{trialId}' that could not be read: {e.GetType().Name}: {e.Message}"
      );
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

  public static void DeleteOutdatedReplays()
  {
    if (!Directory.Exists(Folder))
      return;
    int deletedCount = 0;
    try
    {
      foreach (string path in Directory.GetFiles(Folder, "*.ctreplay"))
      {
        if (!IsOlderFormat(path))
          continue;
        File.Delete(path);
        deletedCount++;
      }
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
    {
      Logger.Log(LogLevel.Warn, LogTag, $"older replay files left for the next start: {e.Message}");
    }
    if (deletedCount == 0)
      return;
    Refresh();
    Logger.Log(LogLevel.Info, LogTag, $"deleted {deletedCount} replay files of an older format");
  }

  private static bool IsOlderFormat(string path)
  {
    using FileStream stream = File.OpenRead(path);
    return stream.ReadByte() < Replay.FormatVersion;
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
