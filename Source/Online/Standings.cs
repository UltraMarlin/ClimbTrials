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
  private const double FreshSeconds = 30;

  private sealed class Entry
  {
    public CampaignStandings Value;
    public DateTime FetchedAt;
    public StandingsState State;
    public bool Fetching;
  }

  private static readonly Dictionary<string, Entry> entries = [];

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  public static StandingsState State(Campaign campaign) =>
    campaign == null ? StandingsState.Idle : EntryOf(campaign).State;

  public static void EnsureFresh(Campaign campaign)
  {
    Entry entry = EntryOf(campaign);
    if (entry.Fetching)
      return;

    bool fresh = (DateTime.UtcNow - entry.FetchedAt).TotalSeconds < FreshSeconds;
    if (entry.State == StandingsState.Ready && fresh)
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
