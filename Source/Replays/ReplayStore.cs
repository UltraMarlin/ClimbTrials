using System;
using System.Collections.Generic;
using System.IO;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays.Codec;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class ReplayStore
{
  private const string LogTag = "ClimbTrials/Replay";

  public static string Folder => Path.Combine(UserIO.GetSaveFilePath(), "ClimbTrials");

  private static string PathFor(string trialId) => Path.Combine(Folder, trialId + ".ctreplay");

  private static HashSet<string> trialsWithReplayFile;

  public static void Refresh()
  {
    trialsWithReplayFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (!Directory.Exists(Folder))
      return;
    foreach (string file in Directory.GetFiles(Folder, "*.ctreplay"))
      trialsWithReplayFile.Add(Path.GetFileNameWithoutExtension(file));
  }

  private static HashSet<string> TrialsWithReplayFile
  {
    get
    {
      if (trialsWithReplayFile == null)
        Refresh();
      return trialsWithReplayFile;
    }
  }

  public static bool Exists(string trialId) => TrialsWithReplayFile.Contains(trialId);

  public static void Save(Replay replay)
  {
    byte[] bytes;
    try
    {
      bytes = replay.ToBytes();
    }
    catch (StartingValuesException e)
    {
      Logger.Error(
        LogTag,
        $"did not save the replay for '{replay.TrialId}', the old file stays: " + e.Message
      );
      return;
    }
    Directory.CreateDirectory(Folder);
    File.WriteAllBytes(PathFor(replay.TrialId), bytes);
    TrialsWithReplayFile.Add(replay.TrialId);
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
      byte[] bytes = File.ReadAllBytes(PathFor(trialId));
      replay = Replay.FromBytes(bytes);
      if (!replay.MatchesTrialHash(trial.AcceptedTrialHashes))
      {
        replay = null;
        failureNotice = Dialog.Clean("climbtrials_replay_deleted_trial_changed");
        Logger.Info(LogTag, $"deleting replay for '{trialId}', the trial has changed");
        Delete(trialId);
        return false;
      }
      replay.IsOwn = true;
      replay.IsPersonalBest = true;
      failureNotice = null;
      return true;
    }
    catch (InvalidDataException e)
    {
      replay = null;
      failureNotice = Dialog.Clean("climbtrials_replay_deleted");
      Logger.Warn(LogTag, $"deleting unreadable replay for '{trialId}': {e.Message}");
      Delete(trialId);
      return false;
    }
    catch (Exception e)
    {
      replay = null;
      failureNotice = Dialog.Clean("climbtrials_replay_kept_unreadable");
      Logger.Error(
        LogTag,
        $"kept replay for '{trialId}' that could not be read: {e.GetType().Name}: {e.Message}"
      );
      return false;
    }
  }

  public static bool Delete(string trialId)
  {
    bool removed = false;
    if (File.Exists(PathFor(trialId)))
    {
      File.Delete(PathFor(trialId));
      removed = true;
    }
    if (TrialsWithReplayFile.Remove(trialId))
      removed = true;
    if (!removed)
      return false;
    Logger.Info(LogTag, $"deleted replay for '{trialId}'");
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
      Logger.Warn(LogTag, $"older replay files left for the next start: {e.Message}");
    }
    if (deletedCount == 0)
      return;
    Refresh();
    Logger.Info(LogTag, $"deleted {deletedCount} replay files of an older format");
  }

  private static bool IsOlderFormat(string path)
  {
    using FileStream stream = File.OpenRead(path);
    return stream.ReadByte() < Replay.FormatVersion;
  }
}
