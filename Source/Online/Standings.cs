using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;

namespace Celeste.Mod.ClimbTrials.Online;

public sealed class Registration
{
  public string ClientId { get; set; }
  public string Name { get; set; }
}

public sealed class RenameAnswer
{
  public string Name { get; set; }
}

public sealed class BoardRow
{
  public int Rank { get; set; }
  public string Name { get; set; }
  public long Ticks { get; set; }
  public DateTime AchievedAt { get; set; }
  public bool Me { get; set; }
}

public sealed class TrialStanding
{
  public string TrialId { get; set; }
  public int Total { get; set; }
  public int? Rank { get; set; }
  public long? Ticks { get; set; }
  public DateTime? AchievedAt { get; set; }
  public List<BoardRow> Window { get; set; } = [];
}

public sealed class Board
{
  public string TrialId { get; set; }
  public int Total { get; set; }
  public int Offset { get; set; }
  public List<BoardRow> Rows { get; set; } = [];
  public BoardRow Me { get; set; }
}

public sealed class CampaignStandings
{
  public string Name { get; set; }
  public List<TrialStanding> Trials { get; set; } = [];
}

public sealed class SubmissionAnswer
{
  public bool Accepted { get; set; }
  public TrialStanding Standing { get; set; }
}

public enum StandingsState
{
  Idle,
  Ready,
  Offline,
}

public static class Standings
{
  private const double FreshSeconds = 60;

  private sealed class Entry
  {
    public CampaignStandings Value;
    public DateTime FetchedAt;
    public StandingsState State;
    public bool Fetching;
  }

  private static readonly Dictionary<string, Entry> entries = [];

  public const int ChunkRows = 150;

  private static readonly Dictionary<
    (string TrialId, int Chunk),
    (DateTime FetchedAt, Board Board)
  > chunks = [];

  private static readonly Dictionary<
    (string TrialId, int Chunk),
    List<Action<ServerResult<Board>>>
  > waiting = [];

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  public static StandingsState State(Campaign campaign) =>
    campaign == null ? StandingsState.Idle : EntryOf(campaign).State;

  public static string OfflineNote =>
    Dialog.Clean(Settings.OfflineMode ? "climbtrials_offline_mode_lower" : "climbtrials_offline");

  public static void EnsureFresh(Campaign campaign)
  {
    Entry entry = EntryOf(campaign);
    if (entry.Fetching)
      return;

    bool fresh = (DateTime.UtcNow - entry.FetchedAt).TotalSeconds < FreshSeconds;
    if (entry.State == StandingsState.Ready && fresh && !Settings.OfflineMode)
      return;

    entry.Fetching = true;
    ServerClient.GetStandings(campaign.Id, result => Receive(entry, result));
  }

  private static void Receive(Entry entry, ServerResult<CampaignStandings> result)
  {
    entry.Fetching = false;
    if (!result.Ok)
    {
      entry.Value = null;
      entry.State = StandingsState.Offline;
      return;
    }

    entry.Value = result.Value;
    entry.FetchedAt = DateTime.UtcNow;
    entry.State = StandingsState.Ready;
    string name = result.Value.Name;
    if (name != null && name != Settings.PlayerName)
      Identity.AdoptName(name);

    TrialRecords.Adopt(
      result
        .Value.Trials.Where(s => s.Ticks != null)
        .Select(s => (s.TrialId, s.Ticks.Value, s.AchievedAt ?? DateTime.UtcNow))
    );
  }

  public static void FetchBoard(Trial trial, int chunk, Action<ServerResult<Board>> onDone)
  {
    (string, int) key = (trial.Id, chunk);
    bool hit = chunks.TryGetValue(key, out (DateTime FetchedAt, Board Board) cached);
    bool fresh = hit && (DateTime.UtcNow - cached.FetchedAt).TotalSeconds < FreshSeconds;
    if (fresh && !Settings.OfflineMode)
    {
      onDone(new ServerResult<Board>(cached.Board, ServerError.None, null));
      return;
    }
    if (waiting.TryGetValue(key, out List<Action<ServerResult<Board>>> waiters))
    {
      waiters.Add(onDone);
      return;
    }
    waiting[key] = [onDone];
    ServerClient.GetBoard(
      trial.Id,
      chunk * ChunkRows,
      ChunkRows,
      result =>
      {
        if (result.Ok)
          chunks[key] = (DateTime.UtcNow, result.Value);
        List<Action<ServerResult<Board>>> done = waiting[key];
        waiting.Remove(key);
        foreach (Action<ServerResult<Board>> callback in done)
          callback(result);
      }
    );
  }

  public static TrialStanding For(Trial trial)
  {
    Campaign campaign = Campaigns.CampaignOf(trial);
    if (campaign == null)
      return null;
    return EntryOf(campaign).Value?.Trials.Find(s => s.TrialId == trial.Id);
  }

  public static void Replace(TrialStanding standing)
  {
    if (standing == null)
      return;
    Trial trial = Campaigns.FindTrial(standing.TrialId);
    Campaign campaign = trial == null ? null : Campaigns.CampaignOf(trial);
    CampaignStandings board = campaign == null ? null : EntryOf(campaign).Value;
    if (board == null)
      return;
    int index = board.Trials.FindIndex(s => s.TrialId == standing.TrialId);
    if (index < 0)
      board.Trials.Add(standing);
    else
      board.Trials[index] = standing;
  }

  private static Entry EntryOf(Campaign campaign)
  {
    if (!entries.TryGetValue(campaign.Id, out Entry entry))
    {
      entry = new Entry();
      entries[campaign.Id] = entry;
    }
    return entry;
  }
}
