using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Online;

public static class GhostCache
{
  private const string LogTag = "ClimbTrials/Ghost";
  private const int Keep = 20;

  private static readonly Dictionary<string, Replay> replays = [];
  private static readonly List<string> order = [];
  private static readonly Dictionary<string, long> olderTrial = [];

  public static bool IsOlderTrial(string replayId, long ticks) =>
    replayId != null && olderTrial.TryGetValue(replayId, out long stale) && stale == ticks;

  public static void Fetch(
    string replayId,
    long ticks,
    bool own,
    Trial trial,
    Action<Replay> onDone
  )
  {
    if (string.IsNullOrEmpty(replayId))
    {
      onDone(null);
      return;
    }
    if (replays.TryGetValue(replayId, out Replay cached) && cached.Ticks == ticks)
    {
      onDone(cached);
      return;
    }

    ServerClient.Download(
      replayId,
      result =>
      {
        if (!result.Ok)
        {
          onDone(null);
          return;
        }
        onDone(Read(replayId, own, trial, result.Value));
      }
    );
  }

  private static Replay Read(string replayId, bool own, Trial trial, DownloadedReplay downloaded)
  {
    try
    {
      Replay replay = Replay.FromBytes(downloaded.Bytes);
      if (!replay.MatchesTrialHash(trial.AcceptedTrialHashes))
      {
        olderTrial[replayId] = replay.Ticks;
        return null;
      }
      replay.IsOwn = own;
      replay.IsPersonalBest = own;
      replay.OwnerName = downloaded.OwnerName;
      replay.ServerReplayId = replayId;
      Store(replayId, replay);
      return replay;
    }
    catch (Exception e)
    {
      Logger.Log(LogLevel.Warn, LogTag, $"replay {replayId} could not be read: {e.Message}");
      return null;
    }
  }

  private static void Store(string replayId, Replay replay)
  {
    replays[replayId] = replay;
    order.Remove(replayId);
    order.Add(replayId);
    if (order.Count <= Keep)
      return;
    replays.Remove(order[0]);
    order.RemoveAt(0);
  }
}
