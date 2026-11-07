using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Celeste.Mod.ClimbTrials.Commands;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class ReplayVerifier
{
  private const string LogTag = "ClimbTrials/Verify";
  private const int MostUpdatesPerDraw = 30;
  private const double BatchBudgetMs = 12;

  private static readonly Queue<AdminReplay> waiting = new();
  private static string targetLabel;
  private static bool replaysReceived;
  private static int fetched;
  private static int notVisible;
  private static int withoutReplay;
  private static int inSync;
  private static int desynced;
  private static int unreadable;
  private static int notPlayed;
  private static AdminReplay playingEntry;
  private static Replay playingReplay;
  private static Replay lastStartedReplay;
  private static bool playingOlderTrial;
  private static int updatesSinceDraw;
  private static readonly Stopwatch checkTime = new();
  private static long playedTicks;
  private static bool ticksBackToBack;
  private static bool savedFixedTimeStep;
  private static bool savedVSync;

  public static void Load()
  {
    On.Celeste.Celeste.Update += OnCelesteUpdate;
    On.Monocle.Engine.Draw += OnEngineDraw;
  }

  public static void Unload()
  {
    if (targetLabel != null)
      End("the mod was reloaded");
    On.Celeste.Celeste.Update -= OnCelesteUpdate;
    On.Monocle.Engine.Draw -= OnEngineDraw;
  }

  public static bool Start(string scope, string id, int maxRank, string label)
  {
    if (targetLabel != null)
    {
      Engine.Commands.Log($"a replay check of {targetLabel} is already running");
      return false;
    }
    if (RunManager.AttemptInProgress && !RunManager.Replaying)
    {
      Engine.Commands.Log("finish or leave the run first");
      return false;
    }
    targetLabel = label;
    replaysReceived = false;
    fetched = 0;
    notVisible = 0;
    withoutReplay = 0;
    inSync = 0;
    desynced = 0;
    unreadable = 0;
    notPlayed = 0;
    lastStartedReplay = null;
    playedTicks = 0;
    checkTime.Restart();
    Engine.Commands.Log($"fetching the replays of {label}");
    ServerClient.GetAdminReplays(scope, id, maxRank > 0 ? maxRank : null, ReceiveReplays);
    return true;
  }

  private static void ReceiveReplays(ServerResult<AdminReplays> result)
  {
    if (targetLabel == null)
      return;

    if (!result.Ok)
    {
      string reason = result.Error == ServerError.NotAdmin ? "not an admin" : result.Message;
      ConsoleOutput.Log(
        LogTag,
        $"replay check of {targetLabel} failed: {reason ?? result.Error.ToString()}"
      );
      targetLabel = null;
      return;
    }

    fetched = result.Value.Replays.Count;
    notVisible = result.Value.NotVisible;
    withoutReplay = result.Value.WithoutReplay;

    foreach (AdminReplay entry in result.Value.Replays.OrderBy(ChapterOf))
      waiting.Enqueue(entry);

    Audio.MusicVolume = 0f;
    Audio.SfxVolume = 0f;
    RunTicksBackToBack(true);
    replaysReceived = true;
  }

  private static void RunTicksBackToBack(bool backToBack)
  {
    if (backToBack == ticksBackToBack)
      return;

    if (backToBack)
    {
      savedFixedTimeStep = Engine.Instance.IsFixedTimeStep;
      savedVSync = Engine.Graphics.SynchronizeWithVerticalRetrace;
    }
    ticksBackToBack = backToBack;
    Engine.Instance.IsFixedTimeStep = !backToBack && savedFixedTimeStep;
    Engine.Graphics.SynchronizeWithVerticalRetrace = !backToBack && savedVSync;
    Engine.Graphics.ApplyChanges();
  }

  private static string ChapterOf(AdminReplay entry)
  {
    RunDefinition run = Campaigns.FindTrial(entry.TrialId)?.Run;
    return run == null ? "" : $"{run.Sid} {run.Mode}";
  }

  private static void PlayNext()
  {
    while (waiting.Count > 0)
    {
      if (Engine.NextScene != Engine.Scene)
        return;
      AdminReplay entry = waiting.Dequeue();
      Replay replay;
      try
      {
        replay = Replay.FromBytes(Convert.FromBase64String(entry.Bytes));
      }
      catch (Exception e)
      {
        unreadable++;
        LogResult(entry, $"unreadable, {e.Message}", olderTrial: false);
        continue;
      }
      Trial trial = Campaigns.FindTrial(replay.TrialId);
      replay.OwnerName = entry.Player;
      if (trial == null || !RunManager.StartReplay(replay))
      {
        notPlayed++;
        LogResult(entry, "not played, the trial is not loaded", olderTrial: false);
        continue;
      }
      playingEntry = entry;
      playingReplay = replay;
      lastStartedReplay = replay;
      playingOlderTrial = !replay.MatchesTrialHash(trial.AcceptedTrialHashes);
      return;
    }
    End(stopReason: null);
  }

  private static void FollowPlayback()
  {
    if (playingReplay == null)
    {
      if (replaysReceived)
        PlayNext();
      return;
    }
    if (RunManager.Replay != playingReplay)
    {
      End("the replay was left");
      return;
    }
    if (Replayer.State != ReplayState.Finished)
      return;

    if (Replayer.Desynced)
    {
      desynced++;
      LogResult(
        playingEntry,
        $"desynced at frame {Replayer.Frame} of {playingReplay.Frames.Count}, {Replayer.DesyncReason}",
        playingOlderTrial
      );
    }
    else if (RunManager.LastFinish != null)
    {
      inSync++;
      playedTicks += playingEntry.Ticks;
      LogResult(playingEntry, "in sync", playingOlderTrial);
    }
    else
    {
      End(Replayer.StoppedByReload ? "the level was reloaded" : "the replay was stopped");
      return;
    }
    playingReplay = null;
    PlayNext();
  }

  private static void LogResult(AdminReplay entry, string result, bool olderTrial)
  {
    string rank = entry.Rank is int place ? RankText.Format(place) : "#-";
    string older = olderTrial ? " (older trial)" : "";
    Logger.Log(
      LogLevel.Info,
      LogTag,
      $"{entry.TrialId} {rank} {entry.Player} {TimeText.Format(entry.Ticks)} {result}{older}"
    );
  }

  private static void End(string stopReason)
  {
    bool finished = stopReason == null;
    int checkedCount = inSync + desynced + unreadable + notPlayed;
    List<string> parts = [$"{inSync} in sync", $"{desynced} desynced"];
    if (unreadable > 0)
      parts.Add($"{unreadable} unreadable");
    if (notPlayed > 0)
      parts.Add($"{notPlayed} not played");
    string counts = finished
      ? $"{fetched} replays, {string.Join(", ", parts)}"
      : $"stopped after {checkedCount} of {fetched} replays ({stopReason}), {string.Join(", ", parts)}";
    ConsoleOutput.Log(LogTag, $"replay check of {targetLabel}: {counts}");
    double seconds = checkTime.Elapsed.TotalSeconds;
    double playedSeconds = playedTicks / (double)TimeSpan.TicksPerSecond;
    ConsoleOutput.Log(
      LogTag,
      $"took {seconds:0.0} s, {playedSeconds:0.0} s of replays in sync, "
        + $"{playedSeconds / Math.Max(seconds, 0.001):0} times real time"
    );
    if (notVisible > 0)
      ConsoleOutput.Log(LogTag, $"{notVisible} replays not visible to you");
    if (withoutReplay > 0)
      ConsoleOutput.Log(LogTag, $"{withoutReplay} times without a replay");

    bool leaveCheckedReplay =
      finished && lastStartedReplay != null && RunManager.Replay == lastStartedReplay;
    waiting.Clear();
    replaysReceived = false;
    playingReplay = null;
    playingEntry = null;
    lastStartedReplay = null;
    targetLabel = null;
    RunTicksBackToBack(false);
    Settings.Instance.ApplyVolumes();
    if (leaveCheckedReplay)
      RunManager.LeaveToTrials();
  }

  private static void OnCelesteUpdate(
    On.Celeste.Celeste.orig_Update orig,
    Celeste self,
    GameTime gameTime
  )
  {
    if (targetLabel == null)
    {
      orig(self, gameTime);
      return;
    }
    FollowPlayback();

    if (updatesSinceDraw >= MostUpdatesPerDraw)
      return;

    GameTime frameTime = ticksBackToBack
      ? new GameTime(gameTime.TotalGameTime, Engine.Instance.TargetElapsedTime)
      : gameTime;
    long batchStart = Stopwatch.GetTimestamp();
    Scene scene = Engine.Scene;
    do
    {
      orig(self, frameTime);
      updatesSinceDraw++;
      FollowPlayback();
    } while (
      CanRunAhead(scene)
      && updatesSinceDraw < MostUpdatesPerDraw
      && Stopwatch.GetElapsedTime(batchStart).TotalMilliseconds < BatchBudgetMs
    );
  }

  private static bool CanRunAhead(Scene batchScene) =>
    playingReplay != null
    && Engine.Scene == batchScene
    && Engine.NextScene == batchScene
    && batchScene is Level level
    && !level.Paused
    && Replayer.State is ReplayState.Idle or ReplayState.Playing
    && !Replayer.MenuOpen
    && !Engine.Commands.Open;

  private static void OnEngineDraw(On.Monocle.Engine.orig_Draw orig, Engine self, GameTime gameTime)
  {
    updatesSinceDraw = 0;
    orig(self, gameTime);
  }
}
