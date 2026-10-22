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
  public int? Rank { get; set; }
  public string Name { get; set; }
  public long? Ticks { get; set; }
  public DateTime? AchievedAt { get; set; }
  public bool Me { get; set; }
  public string ReplayId { get; set; }
  public int? ReplayFormat { get; set; }
  public Medal? Medal { get; set; }
  public int RankOrLast => Rank ?? int.MaxValue;
}

public sealed class TrialStanding
{
  public string TrialId { get; set; }
  public BoardMode BoardMode { get; set; }
  public int? Total { get; set; }
  public int? Rank { get; set; }
  public long? Ticks { get; set; }
  public DateTime? AchievedAt { get; set; }
  public List<BoardRow> Window { get; set; } = [];
}

public sealed class Board
{
  public BoardMode BoardMode { get; set; }
  public ReplayMode ReplayMode { get; set; }
  public int? Total { get; set; }
  public int Offset { get; set; }
  public List<BoardRow> Rows { get; set; } = [];
  public BoardRow Me { get; set; }
}

public sealed class ServerRecord
{
  public string TrialId { get; set; }
  public int? Rank { get; set; }
  public long? Ticks { get; set; }
  public DateTime? AchievedAt { get; set; }
}

public sealed class CampaignRecords
{
  public string Name { get; set; }
  public bool SubmissionsPaused { get; set; }
  public List<ServerRecord> Trials { get; set; } = [];
}

public sealed class CampaignStanding
{
  public string CampaignId { get; set; }
  public int? Total { get; set; }
  public int? Rank { get; set; }
}

public sealed class HubAnswer
{
  public string Name { get; set; }
  public DateTime ServerTime { get; set; }
  public bool SubmissionsPaused { get; set; }
  public List<CampaignStanding> Campaigns { get; set; } = [];
  public List<EventInfo> Events { get; set; } = [];
}

public sealed class SubmissionAnswer
{
  public bool Accepted { get; set; }
  public TrialStanding Standing { get; set; }
  public CampaignStanding Campaign { get; set; }
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
    public DateTime TriedAt;
  }

  private delegate void BoardRequest(int offset, int limit, Action<ServerResult<Board>> onDone);

  private static readonly Entry<HubAnswer> hub = new();

  private static readonly Dictionary<string, Entry<CampaignRecords>> records = [];

  private static readonly Dictionary<string, Entry<TrialStanding>> standings = [];

  public const int ChunkRows = 150;

  private static readonly Dictionary<
    (string Key, int Chunk),
    (DateTime FetchedAt, Board Board)
  > chunks = [];

  private static readonly Dictionary<
    (string Key, int Chunk),
    List<Action<ServerResult<Board>>>
  > waiting = [];

  private static int identityGeneration;

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  public static StandingsState State(Campaign campaign) =>
    campaign == null ? StandingsState.Idle : RecordsOf(campaign).State;

  public static StandingsState HubState => hub.State;

  public static StandingsState MiniBoardState(Trial trial) => StandingOf(trial.Id).State;

  public static bool SubmissionsPaused { get; set; }

  private static bool NeedsFetch<T>(Entry<T> entry) =>
    !entry.Fetching
    && (
      entry.State != StandingsState.Ready
      || (DateTime.UtcNow - entry.FetchedAt).TotalSeconds >= FreshSeconds
      || Settings.OfflineMode
    );

  public static void EnsureFreshHub()
  {
    bool crossed = !hub.Fetching && Events.CrossedSinceAnswer();
    if (!NeedsFetch(hub) && !crossed)
      return;

    hub.Fetching = true;
    Identity.EnsureRegistered(() => FetchHub(retried: false));
  }

  private static void FetchHub(bool retried) =>
    AskCurrent<HubAnswer>(
      done => ServerClient.GetHub(Events.HeldDefinitions(), done),
      result => ReceiveHub(result, retried),
      passDeleted: !retried
    );

  private static void ReceiveHub(ServerResult<HubAnswer> result, bool retried)
  {
    if (result.Error == ServerError.PlayerDeleted && !retried)
    {
      Identity.EnsureRegistered(() => FetchHub(retried: true));
      return;
    }
    if (!Accept(hub, result))
      return;
    HubAnswer answer = result.Value;
    AdoptName(answer.Name);
    SubmissionsPaused = answer.SubmissionsPaused;
    EventsReceived received = Events.Receive(answer.Events, answer.ServerTime);
    Uploader.ForgetRefusals();
    if (received.Missing)
      MarkHubStale();
    foreach (string id in received.ModesChanged)
      DropEventBoards(id);
    foreach (EventInfo info in answer.Events ?? [])
      answer.Campaigns.Add(
        new CampaignStanding
        {
          CampaignId = info.Id,
          Total = info.Total,
          Rank = info.Rank,
        }
      );
  }

  public static void MarkHubStale() => hub.FetchedAt = DateTime.MinValue;

  private static void DropEventBoards(string eventId)
  {
    DropChunks(CampaignKey(eventId));
    foreach (string trialId in Events.TrialIdsOf(eventId))
    {
      DropChunks(TrialKey(trialId));
      if (standings.TryGetValue(trialId, out Entry<TrialStanding> entry))
      {
        entry.FetchedAt = DateTime.MinValue;
        entry.TriedAt = DateTime.MinValue;
      }
    }
  }

  private static void AskCurrent<T>(
    Action<Action<ServerResult<T>>> request,
    Action<ServerResult<T>> receive,
    bool passDeleted = false
  )
  {
    int asked = identityGeneration;
    request(result =>
    {
      bool deleted = passDeleted && result.Error == ServerError.PlayerDeleted;
      if (asked != identityGeneration && !deleted)
        AskCurrent(request, receive, passDeleted);
      else
        receive(result);
    });
  }

  public static void EnsureFresh(Campaign campaign)
  {
    Entry<CampaignRecords> entry = RecordsOf(campaign);
    if (!NeedsFetch(entry))
      return;

    entry.Fetching = true;
    AskCurrent<CampaignRecords>(
      done => ServerClient.GetRecords(campaign.Id, done),
      result => ReceiveRecords(entry, result)
    );
  }

  private static void ReceiveRecords(
    Entry<CampaignRecords> entry,
    ServerResult<CampaignRecords> result
  )
  {
    if (result.Error == ServerError.NotFound)
      MarkHubStale();
    if (!Accept(entry, result))
      return;

    AdoptName(result.Value.Name);
    SubmissionsPaused = result.Value.SubmissionsPaused;
    TrialRecords.Adopt(
      result
        .Value.Trials.Where(r => r.Ticks != null)
        .Select(r => (r.TrialId, r.Ticks.Value, r.AchievedAt ?? DateTime.UtcNow))
    );
  }

  public static void RefreshMiniBoard(Trial trial)
  {
    if (
      trial == null
      || Settings.OfflineMode
      || Settings.HudSettings.Leaderboard == LeaderboardMode.Off
    )
      return;

    Entry<TrialStanding> entry = StandingOf(trial.Id);
    DateTime now = DateTime.UtcNow;
    bool fresh =
      entry.State == StandingsState.Ready && (now - entry.FetchedAt).TotalSeconds < FreshSeconds;
    if (entry.Fetching || fresh || (now - entry.TriedAt).TotalSeconds < FreshSeconds)
      return;

    entry.Fetching = true;
    entry.TriedAt = now;
    AskCurrent<TrialStanding>(
      done => ServerClient.GetTrialStanding(trial.Id, done),
      result => ReceiveStanding(entry, result)
    );
  }

  private static void ReceiveStanding(
    Entry<TrialStanding> entry,
    ServerResult<TrialStanding> result
  )
  {
    entry.Fetching = false;
    if (!result.Ok)
    {
      if (result.Error == ServerError.NotFound)
        MarkHubStale();
      if (entry.Value == null)
        entry.State = StandingsState.Offline;
      return;
    }
    StoreStanding(result.Value);
    if (result.Value.Ticks is long ticks)
      TrialRecords.Adopt([
        (result.Value.TrialId, ticks, result.Value.AchievedAt ?? DateTime.UtcNow),
      ]);
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

  private static void AdoptName(string name)
  {
    if (name != null && name != Settings.PlayerName)
      Identity.AdoptName(name);
  }

  public static void MarkStale()
  {
    identityGeneration++;
    hub.FetchedAt = DateTime.MinValue;
    foreach (Entry<CampaignRecords> entry in records.Values)
      entry.FetchedAt = DateTime.MinValue;
    foreach (Entry<TrialStanding> entry in standings.Values)
    {
      entry.FetchedAt = DateTime.MinValue;
      entry.TriedAt = DateTime.MinValue;
    }
    chunks.Clear();
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

  public static TrialStanding StandingFor(Trial trial) => StandingOf(trial.Id).Value;

  public static ServerRecord RecordFor(Trial trial)
  {
    Campaign campaign = Campaigns.CampaignOf(trial);
    if (campaign == null)
      return null;
    return RecordsOf(campaign).Value?.Trials.Find(r => r.TrialId == trial.Id);
  }

  public static CampaignStanding CampaignStandingFor(Campaign campaign) =>
    campaign == null ? null : hub.Value?.Campaigns.Find(c => c.CampaignId == campaign.Id);

  public static void Replace(SubmissionAnswer answer)
  {
    if (answer?.Standing == null)
      return;
    string trialId = answer.Standing.TrialId;
    Trial trial = Campaigns.FindTrial(trialId);
    Campaign campaign = trial == null ? null : Campaigns.CampaignOf(trial);
    DropChunks(TrialKey(trialId));
    if (campaign != null)
      DropChunks(CampaignKey(campaign.Id));
    StoreStanding(answer.Standing);
    if (answer.Campaign != null)
      StoreCampaignStanding(answer.Campaign);
  }

  private static void StoreStanding(TrialStanding standing)
  {
    Entry<TrialStanding> entry = StandingOf(standing.TrialId);
    entry.Value = standing;
    entry.FetchedAt = DateTime.UtcNow;
    entry.State = StandingsState.Ready;

    Trial trial = Campaigns.FindTrial(standing.TrialId);
    Campaign campaign = trial == null ? null : Campaigns.CampaignOf(trial);
    List<ServerRecord> rows = campaign == null ? null : RecordsOf(campaign).Value?.Trials;
    if (rows == null)
      return;
    ServerRecord record = new()
    {
      TrialId = standing.TrialId,
      Rank = standing.Rank,
      Ticks = standing.Ticks,
      AchievedAt = standing.AchievedAt,
    };
    int index = rows.FindIndex(r => r.TrialId == standing.TrialId);
    if (index < 0)
      rows.Add(record);
    else
      rows[index] = record;
  }

  private static void StoreCampaignStanding(CampaignStanding standing)
  {
    List<CampaignStanding> campaigns = hub.Value?.Campaigns;
    if (campaigns == null)
      return;
    int index = campaigns.FindIndex(c => c.CampaignId == standing.CampaignId);
    if (index < 0)
      campaigns.Add(standing);
    else
      campaigns[index] = standing;
  }

  private static void DropChunks(string key)
  {
    foreach ((string Key, int Chunk) at in chunks.Keys.Where(at => at.Key == key).ToList())
      chunks.Remove(at);
  }

  private static Entry<CampaignRecords> RecordsOf(Campaign campaign) =>
    EntryIn(records, campaign.Id);

  private static Entry<TrialStanding> StandingOf(string trialId) => EntryIn(standings, trialId);

  private static Entry<T> EntryIn<T>(Dictionary<string, Entry<T>> entries, string id)
  {
    if (!entries.TryGetValue(id, out Entry<T> entry))
    {
      entry = new Entry<T>();
      entries[id] = entry;
    }
    return entry;
  }
}
