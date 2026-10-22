using System;
using Celeste.Mod.ClimbTrials.Content;

namespace Celeste.Mod.ClimbTrials.UI;

public static class EventTiming
{
  public static string PhaseText(EventCampaign campaign)
  {
    DateTime now = Events.Now;
    EventInfo info = campaign.Info;
    return campaign.Phase(now) switch
    {
      EventPhase.Upcoming => Until("climbtrials_event_opens", info.OpensAt, now),
      EventPhase.Closed => Dialog.Clean("climbtrials_event_closed"),
      _ when info.ClosesAt.HasValue => Until("climbtrials_event_closes", info.ClosesAt.Value, now),
      _ => Dialog.Clean("climbtrials_event_open"),
    };
  }

  public static string BoardNote(EventCampaign campaign, BoardMode shown)
  {
    EventInfo info = campaign.Info;
    DateTime now = Events.Now;
    if (info.RevealAt.HasValue && now >= info.RevealAt.Value)
      return null;

    if (info.Organizer)
      return OrganizerNote(info, now);

    if (shown == BoardMode.FullTimes)
      return null;

    string key =
      shown == BoardMode.OwnTimeOnly ? "climbtrials_board_other_times" : "climbtrials_board_times";
    return info.RevealAt.HasValue
      ? Until(key + "_show", info.RevealAt.Value, now)
      : Dialog.Clean(key + "_hidden");
  }

  private static string OrganizerNote(EventInfo info, DateTime now)
  {
    string key = info.BoardBeforeReveal switch
    {
      BoardMode.RanksOnly => "climbtrials_board_players_see_ranks",
      BoardMode.OwnTimeOnly => "climbtrials_board_players_see_own",
      _ when info.ReplaysBeforeReveal == ReplayMode.OrganizersOnly =>
        "climbtrials_board_players_no_replays",
      _ => null,
    };
    if (key == null)
      return null;
    return info.RevealAt.HasValue
      ? Until(key + "_until", info.RevealAt.Value, now)
      : Dialog.Clean(key);
  }

  public static string LockedReplays(EventCampaign campaign)
  {
    EventInfo info = campaign.Info;
    return info.RevealAt.HasValue
      ? Until("climbtrials_ghost_replays_show", info.RevealAt.Value, Events.Now)
      : Dialog.Clean("climbtrials_ghost_replays_hidden");
  }

  private static string Until(string key, DateTime moment, DateTime now) =>
    Dialog.Get(key).Replace("{0}", Duration(moment - now));

  public static string Duration(TimeSpan span)
  {
    int minutes = Math.Max(1, (int)Math.Ceiling(span.TotalMinutes));
    int days = minutes / (24 * 60);
    int hours = minutes / 60 % 24;
    int rest = minutes % 60;
    if (days > 0)
      return Dialog
        .Get("climbtrials_duration_days")
        .Replace("{0}", $"{days}")
        .Replace("{1}", $"{hours}");
    if (hours > 0)
      return Dialog
        .Get("climbtrials_duration_hours")
        .Replace("{0}", $"{hours}")
        .Replace("{1}", $"{rest}");
    return Dialog.Get("climbtrials_duration_minutes").Replace("{0}", $"{rest}");
  }
}
