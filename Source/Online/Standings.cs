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
  public string ReplayId { get; set; }
  public int? ReplayFormat { get; set; }
  public Medal? Medal { get; set; }
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

public sealed class CampaignTotal
{
  public string CampaignId { get; set; }
  public int Total { get; set; }
  public int? Rank { get; set; }
  public long? Ticks { get; set; }
  public Medal? Medal { get; set; }
  public DateTime? CompletedAt { get; set; }
}

public sealed class CampaignTotals
{
  public string Name { get; set; }
  public List<CampaignTotal> Campaigns { get; set; } = [];
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
  public const double FreshSeconds = 60;

  private sealed class Entry<T>
  {
    public T Value;
    public DateTime FetchedAt;
    public StandingsState State;
    public bool Fetching;
    public DateTime MiniBoardTriedAt;
  }

  private delegate void BoardRequest(int offset, int limit, Action<ServerResult<Board>> onDone);

  private static readonly Dictionary<string, Entry<CampaignStandings>> entries = [];

  private static readonly Entry<CampaignTotals> totals = new();

  public const int ChunkRows = 150;

  private static readonly Dictionary<
    (string Key, int Chunk),
    (DateTime FetchedAt, Board Board)
  > chunks = [];

  private static readonly Dictionary<
    (string Key, int Chunk),
    List<Action<ServerResult<Board>>>
  > waiting = [];

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  public static StandingsState State(Campaign campaign) =>
    campaign == null ? StandingsState.Idle : EntryOf(campaign).State;

  public static StandingsState TotalsState => totals.State;

  public static string OfflineNote =>
    Dialog.Clean(Settings.OfflineMode ? "climbtrials_offline_mode_lower" : "climbtrials_offline");

  private static bool NeedsFetch<T>(Entry<T> entry) =>
    !entry.Fetching
    && (
      entry.State != StandingsState.Ready
      || (DateTime.UtcNow - entry.FetchedAt).TotalSeconds >= FreshSeconds
      || Settings.OfflineMode
    );

  public static void EnsureFresh(Campaign campaign)
  {
    Entry<CampaignStandings> entry = EntryOf(campaign);
    if (!NeedsFetch(entry))
      return;

    entry.Fetching = true;
    ServerClient.GetStandings(campaign.Id, result => Receive(entry, result));
  }

  public static void RefreshMiniBoard(Campaign campaign)
  {
    if (campaign == null || Settings.OfflineMode)
      return;

    Entry<CampaignStandings> entry = EntryOf(campaign);
    DateTime now = DateTime.UtcNow;
    bool fresh =
      entry.State == StandingsState.Ready && (now - entry.FetchedAt).TotalSeconds < FreshSeconds;
    if (entry.Fetching || fresh || (now - entry.MiniBoardTriedAt).TotalSeconds < FreshSeconds)
      return;

    entry.Fetching = true;
    entry.MiniBoardTriedAt = now;
    ServerClient.GetStandings(
      campaign.Id,
      result =>
      {
        if (result.Ok)
          Receive(entry, result);
        else
          entry.Fetching = false;
      }
    );
  }

  public static void EnsureFreshTotals()
  {
    if (!NeedsFetch(totals))
      return;

    totals.Fetching = true;
    ServerClient.GetCampaignTotals(ReceiveTotals);
  }

  private static bool Accept<T>(Entry<T> entry, ServerResult<T> result)
  {
    entry.Fetching = false;
    if (!result.Ok)
    {
      entry.Value = default;
      entry.State = StandingsState.Offline;
      return false;
    }
    entry.Value = result.Value;
    entry.FetchedAt = DateTime.UtcNow;
    entry.State = StandingsState.Ready;
    return true;
  }

  private static void Receive(
    Entry<CampaignStandings> entry,
    ServerResult<CampaignStandings> result
  )
  {
    if (!Accept(entry, result))
      return;

    AdoptName(result.Value.Name);
    TrialRecords.Adopt(
      result
        .Value.Trials.Where(s => s.Ticks != null)
        .Select(s => (s.TrialId, s.Ticks.Value, s.AchievedAt ?? DateTime.UtcNow))
    );
  }

  private static void ReceiveTotals(ServerResult<CampaignTotals> result)
  {
    if (Accept(totals, result))
      AdoptName(result.Value.Name);
  }

  private static void AdoptName(string name)
  {
    if (name != null && name != Settings.PlayerName)
      Identity.AdoptName(name);
  }

  private static string TrialKey(string trialId) => $"trial:{trialId}";

  private static string CampaignKey(string campaignId) => $"campaign:{campaignId}";

  public static void FetchTrialBoard(
    Trial trial,
    int chunk,
    double maxAgeSeconds,
    Action<ServerResult<Board>> onDone
  ) =>
    FetchChunk(
      TrialKey(trial.Id),
      chunk,
      maxAgeSeconds,
      (offset, limit, done) => ServerClient.GetBoard(trial.Id, offset, limit, done),
      onDone
    );

  public static void DropTrialBoard(Trial trial) => DropChunks(TrialKey(trial.Id));

  public static void FetchCampaignBoard(
    Campaign campaign,
    int chunk,
    double maxAgeSeconds,
    Action<ServerResult<Board>> onDone
  ) =>
    FetchChunk(
      CampaignKey(campaign.Id),
      chunk,
      maxAgeSeconds,
      (offset, limit, done) => ServerClient.GetCampaignBoard(campaign.Id, offset, limit, done),
      onDone
    );

  private static void FetchChunk(
    string key,
    int chunk,
    double maxAgeSeconds,
    BoardRequest request,
    Action<ServerResult<Board>> onDone
  )
  {
    (string, int) at = (key, chunk);
    bool hit = chunks.TryGetValue(at, out (DateTime FetchedAt, Board Board) cached);
    bool fresh = hit && (DateTime.UtcNow - cached.FetchedAt).TotalSeconds < maxAgeSeconds;
    if (fresh && !Settings.OfflineMode)
    {
      onDone(new ServerResult<Board>(cached.Board, ServerError.None, null));
      return;
    }
    if (waiting.TryGetValue(at, out List<Action<ServerResult<Board>>> waiters))
    {
      waiters.Add(onDone);
      return;
    }
    waiting[at] = [onDone];
    request(
      chunk * ChunkRows,
      ChunkRows,
      result =>
      {
        if (result.Ok)
          chunks[at] = (DateTime.UtcNow, result.Value);
        List<Action<ServerResult<Board>>> done = waiting[at];
        waiting.Remove(at);
        foreach (Action<ServerResult<Board>> callback in done)
          callback(result);
      }
    );
  }

  public static TrialStanding StandingFor(Trial trial)
  {
    Campaign campaign = Campaigns.CampaignOf(trial);
    if (campaign == null)
      return null;
    return EntryOf(campaign).Value?.Trials.Find(s => s.TrialId == trial.Id);
  }

  public static CampaignTotal TotalFor(Campaign campaign) =>
    campaign == null ? null : totals.Value?.Campaigns.Find(c => c.CampaignId == campaign.Id);

  public static void Replace(TrialStanding standing)
  {
    if (standing == null)
      return;
    totals.State = StandingsState.Idle;
    Trial trial = Campaigns.FindTrial(standing.TrialId);
    Campaign campaign = trial == null ? null : Campaigns.CampaignOf(trial);
    DropChunks(TrialKey(standing.TrialId));
    if (campaign != null)
      DropChunks(CampaignKey(campaign.Id));
    CampaignStandings board = campaign == null ? null : EntryOf(campaign).Value;
    if (board == null)
      return;
    int index = board.Trials.FindIndex(s => s.TrialId == standing.TrialId);
    if (index < 0)
      board.Trials.Add(standing);
    else
      board.Trials[index] = standing;
  }

  private static void DropChunks(string key)
  {
    foreach ((string Key, int Chunk) at in chunks.Keys.Where(at => at.Key == key).ToList())
      chunks.Remove(at);
  }

  private static Entry<CampaignStandings> EntryOf(Campaign campaign)
  {
    if (!entries.TryGetValue(campaign.Id, out Entry<CampaignStandings> entry))
    {
      entry = new Entry<CampaignStandings>();
      entries[campaign.Id] = entry;
    }
    return entry;
  }
}
