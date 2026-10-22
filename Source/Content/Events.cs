using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Celeste.Mod.ClimbTrials.Replays;

namespace Celeste.Mod.ClimbTrials.Content;

public enum Participation
{
  Everyone,
  EveryoneSeesInvitedSubmit,
  InvitedOnly,
}

public enum BoardMode
{
  FullTimes,
  RanksOnly,
  OwnTimeOnly,
}

public enum ReplayMode
{
  Anyone,
  OrganizersOnly,
}

public enum EventPhase
{
  Upcoming,
  Open,
  Closed,
}

public enum SubmitRefusal
{
  None,
  EventClosed,
  NotInvited,
  EventGone,
}

public sealed record EventInfo
{
  public string Id { get; init; }
  public string DefinitionHash { get; init; }
  public string Definition { get; init; }
  public DateTime OpensAt { get; init; }
  public DateTime? ClosesAt { get; init; }
  public DateTime? RevealAt { get; init; }
  public Participation Participation { get; init; }
  public bool Organizer { get; init; }
  public bool Invited { get; init; }
  public BoardMode BoardMode { get; init; }
  public ReplayMode ReplayMode { get; init; }
  public BoardMode BoardBeforeReveal { get; init; }
  public ReplayMode ReplaysBeforeReveal { get; init; }
  public int? Total { get; init; }
  public int? Rank { get; init; }
}

public readonly record struct EventsReceived(IReadOnlyList<string> ModesChanged, bool Missing);

public sealed class EventCampaign(Campaign campaign, string definitionHash)
{
  public Campaign Campaign { get; } = campaign;
  public string DefinitionHash { get; } = definitionHash;

  public string Id => Campaign.Id;

  public EventInfo Info => Events.InfoOf(Id);

  public EventPhase Phase(DateTime now)
  {
    EventInfo info = Info;
    if (now < info.OpensAt)
      return EventPhase.Upcoming;
    if (info.ClosesAt.HasValue && now >= info.ClosesAt.Value)
      return EventPhase.Closed;
    return EventPhase.Open;
  }

  public SubmitRefusal Refusal(DateTime now)
  {
    if (!Events.IsListed(Id))
      return SubmitRefusal.EventGone;
    EventInfo info = Info;
    return Phase(now) switch
    {
      EventPhase.Closed => SubmitRefusal.EventClosed,
      EventPhase.Upcoming => info.Organizer ? SubmitRefusal.None : SubmitRefusal.NotInvited,
      _ when info.Participation == Participation.Everyone || info.Invited => SubmitRefusal.None,
      _ => SubmitRefusal.NotInvited,
    };
  }
}

public static class Events
{
  private const string LogTag = "ClimbTrials/Events";
  private const int CacheVersion = 1;
  private const string CacheFileName = "events.json";

  private sealed record Definition(string Hash, string Text, Campaign Campaign);

  private sealed class CacheContent
  {
    public int Version { get; set; }
    public double ServerTimeOffsetSeconds { get; set; }
    public DateTime AnsweredAt { get; set; }
    public List<EventInfo> Events { get; set; } = [];
  }

  private static readonly JsonSerializerOptions cacheJson = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
  };

  private static readonly Dictionary<string, Definition> definitions = [];
  private static readonly Dictionary<string, EventInfo> infos = [];
  private static readonly Dictionary<string, string> unreadable = [];
  private static List<string> listed = [];
  private static List<EventCampaign> shown = [];
  private static TimeSpan serverOffset;
  private static DateTime answeredAt = DateTime.MinValue;
  private static bool loaded;
  private static string writtenEvents;

  private static string CachePath => Path.Combine(ReplayStore.Folder, CacheFileName);

  public static DateTime Now
  {
    get
    {
      EnsureLoaded();
      return DateTime.UtcNow + serverOffset;
    }
  }

  public static IReadOnlyList<EventCampaign> All
  {
    get
    {
      EnsureLoaded();
      return shown;
    }
  }

  public static bool HasPending
  {
    get
    {
      EnsureLoaded();
      return !SameAsShown();
    }
  }

  public static EventInfo InfoOf(string id) => infos.GetValueOrDefault(id);

  public static bool IsListed(string id) => listed.Contains(id);

  public static IEnumerable<string> TrialIdsOf(string id) =>
    definitions.TryGetValue(id, out Definition definition)
      ? definition.Campaign.Trials.Select(t => t.Id)
      : [];

  public static EventCampaign Of(Campaign campaign) =>
    campaign == null ? null : All.FirstOrDefault(e => e.Campaign == campaign);

  public static EventCampaign Of(Trial trial) =>
    trial == null ? null : All.FirstOrDefault(e => e.Campaign.Trials.Contains(trial));

  public static SubmitRefusal RefusalFor(Trial trial) =>
    Of(trial)?.Refusal(Now) ?? SubmitRefusal.None;

  public static Trial FindTrial(string id) =>
    All.SelectMany(e => e.Campaign.Trials).FirstOrDefault(t => t.Id == id);

  public static List<Campaign> Ordered()
  {
    DateTime now = Now;
    return All.OrderBy(e => PhaseOrder(e.Phase(now)))
      .ThenBy(e => e.Info.OpensAt)
      .ThenBy(e => e.Id, StringComparer.Ordinal)
      .Select(e => e.Campaign)
      .ToList();
  }

  private static int PhaseOrder(EventPhase phase) =>
    phase switch
    {
      EventPhase.Open => 0,
      EventPhase.Upcoming => 1,
      _ => 2,
    };

  public static List<(string Id, string Hash)> HeldDefinitions()
  {
    EnsureLoaded();
    return listed
      .Select(id => (id, definitions[id].Hash))
      .Concat(unreadable.Select(pair => (pair.Key, pair.Value)))
      .ToList();
  }

  public static bool CrossedSinceAnswer()
  {
    EnsureLoaded();
    DateTime now = Now;
    foreach (string id in listed)
    {
      EventInfo info = infos[id];
      if (Crossed(info.OpensAt, now) || Crossed(info.ClosesAt, now) || Crossed(info.RevealAt, now))
        return true;
    }
    return false;
  }

  private static bool Crossed(DateTime? moment, DateTime now) =>
    moment.HasValue && moment.Value > answeredAt && moment.Value <= now;

  public static EventsReceived Receive(List<EventInfo> answer, DateTime serverTime)
  {
    EnsureLoaded();
    serverOffset = serverTime - DateTime.UtcNow;
    answeredAt = serverTime;
    List<string> ids = [];
    List<string> modesChanged = [];
    bool missing = false;
    foreach (EventInfo info in answer ?? [])
    {
      if (info?.Id == null || info.DefinitionHash == null)
        continue;
      EventInfo before = infos.GetValueOrDefault(info.Id);
      switch (Accept(info))
      {
        case Acceptance.Taken:
          ids.Add(info.Id);
          bool changed =
            before == null
            || before.BoardMode != info.BoardMode
            || before.ReplayMode != info.ReplayMode;
          if (changed)
            modesChanged.Add(info.Id);
          break;
        case Acceptance.Missing:
          missing = true;
          break;
      }
    }
    listed = ids;
    HashSet<string> answered = (answer ?? [])
      .Where(i => i?.Id != null)
      .Select(i => i.Id)
      .ToHashSet();
    foreach (string id in unreadable.Keys.Where(id => !answered.Contains(id)).ToList())
      unreadable.Remove(id);
    WriteCache();
    return new EventsReceived(modesChanged, missing);
  }

  private enum Acceptance
  {
    Taken,
    Unreadable,
    Missing,
  }

  private static Acceptance Accept(EventInfo info)
  {
    if (info.Definition != null)
    {
      Definition parsed = Parse(info.Id, info.DefinitionHash, info.Definition);
      if (parsed == null)
      {
        unreadable[info.Id] = info.DefinitionHash;
        return Acceptance.Unreadable;
      }
      unreadable.Remove(info.Id);
      definitions[info.Id] = parsed;
    }
    else if (unreadable.GetValueOrDefault(info.Id) == info.DefinitionHash)
      return Acceptance.Unreadable;
    else if (
      !definitions.TryGetValue(info.Id, out Definition held)
      || held.Hash != info.DefinitionHash
    )
      return Acceptance.Missing;
    infos[info.Id] = info with { Definition = null };
    return Acceptance.Taken;
  }

  public static void ApplyPending()
  {
    EnsureLoaded();
    if (SameAsShown())
      return;
    List<EventCampaign> next = [];
    foreach (string id in listed)
    {
      Definition definition = definitions[id];
      EventCampaign kept = shown.Find(e => e.Id == id && e.DefinitionHash == definition.Hash);
      next.Add(kept ?? new EventCampaign(definition.Campaign, definition.Hash));
    }
    shown = next;
    foreach (string id in definitions.Keys.Except(listed).ToList())
    {
      definitions.Remove(id);
      infos.Remove(id);
    }
  }

  private static bool SameAsShown() =>
    listed.Count == shown.Count
    && shown.All(e => listed.Contains(e.Id) && definitions[e.Id].Hash == e.DefinitionHash);

  private static Definition Parse(string id, string hash, string text)
  {
    string actual = Convert
      .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))
      .ToLowerInvariant();
    if (actual != hash)
    {
      Logger.Log(LogLevel.Warn, LogTag, $"event '{id}' skipped: its definition hash differs");
      return null;
    }
    try
    {
      Campaign campaign = TrialFile.ParseCampaign(text, $"event '{id}'");
      Check(campaign, id);
      return new Definition(hash, text, campaign);
    }
    catch (Exception e) when (e is TrialFileException or InvalidOperationException)
    {
      Logger.Log(LogLevel.Warn, LogTag, $"event '{id}' skipped: {e.Message}");
      return null;
    }
  }

  private static void Check(Campaign campaign, string id)
  {
    if (campaign.Id != id)
      throw new InvalidOperationException($"its campaign id is '{campaign.Id}'");
    HashSet<string> taken = new(StringComparer.OrdinalIgnoreCase);
    foreach (Campaign official in Campaigns.All)
    {
      taken.Add(official.Id);
      taken.UnionWith(official.Trials.Select(t => t.Id));
    }
    foreach ((string otherId, Definition other) in definitions)
    {
      if (otherId == id)
        continue;
      taken.Add(otherId);
      taken.UnionWith(other.Campaign.Trials.Select(t => t.Id));
    }
    if (taken.Contains(campaign.Id))
      throw new InvalidOperationException("its id is taken");
    Campaigns.CheckTrials(campaign, taken);
  }

  private static void EnsureLoaded()
  {
    if (loaded)
      return;
    loaded = true;
    ReadCache();
    ApplyPending();
  }

  private static void ReadCache()
  {
    string path = CachePath;
    if (!File.Exists(path))
      return;
    try
    {
      string text = File.ReadAllText(path);
      CacheContent content = JsonSerializer.Deserialize<CacheContent>(text, cacheJson);
      if (content == null || content.Version != CacheVersion)
        return;
      serverOffset = TimeSpan.FromSeconds(content.ServerTimeOffsetSeconds);
      answeredAt = content.AnsweredAt;
      foreach (EventInfo info in content.Events ?? [])
        if (info?.Id != null && info.Definition != null && Accept(info) == Acceptance.Taken)
          listed.Add(info.Id);
      writtenEvents = JsonSerializer.Serialize(content.Events, cacheJson);
      unreadable.Clear();
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
    {
      Logger.Log(LogLevel.Warn, LogTag, $"the events copy on disk was not read: {e.Message}");
    }
  }

  private static void WriteCache()
  {
    CacheContent content = new()
    {
      Version = CacheVersion,
      ServerTimeOffsetSeconds = serverOffset.TotalSeconds,
      AnsweredAt = answeredAt,
      Events = listed
        .Select(id =>
          infos[id] with
          {
            Definition = definitions[id].Text,
            Total = null,
            Rank = null,
          }
        )
        .ToList(),
    };
    string events = JsonSerializer.Serialize(content.Events, cacheJson);
    if (events == writtenEvents)
      return;
    string text = JsonSerializer.Serialize(content, cacheJson);
    string path = CachePath;
    string temporary = path + ".tmp";
    try
    {
      Directory.CreateDirectory(ReplayStore.Folder);
      File.WriteAllText(temporary, text);
      File.Move(temporary, path, overwrite: true);
      writtenEvents = events;
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
    {
      Logger.Log(LogLevel.Warn, LogTag, $"the events copy on disk was not written: {e.Message}");
    }
  }
}
