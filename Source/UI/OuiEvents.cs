using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiEvents : CampaignListScreen
{
  private static readonly float[] OpeningRetryWaits = [2f, 5f, 10f];

  private readonly HashSet<string> askedOpenings = [];
  private IReadOnlyList<UpcomingEvent> builtUpcoming = [];
  private DateTime nextUnaskedOpening = DateTime.MaxValue;
  private Coroutine openingCheck;

  protected override string TitleKey => "climbtrials_events_title";

  public override IEnumerator Enter(Oui from)
  {
    askedOpenings.Clear();
    Events.ApplyPending();
    return base.Enter(from);
  }

  protected override IEnumerable<SlidingRow> ListedRows() => Events.OrderedEntries().Select(RowFor);

  private static SlidingRow RowFor(EventListEntry entry) =>
    entry.Event != null ? new EventRow(entry.Event.Campaign) : new UpcomingEventRow(entry.Upcoming);

  protected override TrialsMenu BuildMenu()
  {
    TrialsMenu built = base.BuildMenu();
    builtUpcoming = Events.Upcoming;
    nextUnaskedOpening = FindNextUnaskedOpening(built);
    return built;
  }

  private DateTime FindNextUnaskedOpening(TrialsMenu built) =>
    built
      .Items.OfType<UpcomingEventRow>()
      .Where(row => !askedOpenings.Contains(row.Entry.Id))
      .Select(row => row.Entry.OpensAt)
      .DefaultIfEmpty(DateTime.MaxValue)
      .Min();

  public override void Update()
  {
    base.Update();
    if (menu?.Focused != true || BoardOpen)
      return;

    MarkSeen(menu.Current);

    if (Events.HasPending || UpcomingChangedSinceBuild())
    {
      ApplyPendingAndRebuild();
      return;
    }

    if (openingCheck == null && Events.Now >= nextUnaskedOpening)
      StartOpeningCheck();
  }

  private bool UpcomingChangedSinceBuild()
  {
    IReadOnlyList<UpcomingEvent> upcoming = Events.Upcoming;
    if (upcoming.Count != builtUpcoming.Count)
      return true;

    for (int i = 0; i < upcoming.Count; i++)
      if (upcoming[i] != builtUpcoming[i])
        return true;

    return false;
  }

  private void StartOpeningCheck()
  {
    DateTime now = Events.Now;
    List<string> openedIds = menu
      .Items.OfType<UpcomingEventRow>()
      .Where(row => row.Entry.OpensAt <= now && !askedOpenings.Contains(row.Entry.Id))
      .Select(row => row.Entry.Id)
      .ToList();
    askedOpenings.UnionWith(openedIds);
    nextUnaskedOpening = FindNextUnaskedOpening(menu);
    openingCheck = new Coroutine(AskUntilOpened(openedIds));
    Add(openingCheck);
  }

  protected override void CancelTransitions()
  {
    base.CancelTransitions();
    openingCheck?.RemoveSelf();
    openingCheck = null;
  }

  private IEnumerator AskUntilOpened(List<string> openedIds)
  {
    for (int retries = 0; ; retries++)
    {
      Standings.RefetchHub();
      while (Standings.HubFetching)
        yield return null;

      if (!Events.Upcoming.Any(entry => openedIds.Contains(entry.Id)))
        break;

      bool offline =
        ClimbTrialsModule.Settings.OfflineMode || Standings.HubState == StandingsState.Offline;
      if (offline || retries == OpeningRetryWaits.Length)
        break;

      yield return OpeningRetryWaits[retries];
    }
    openingCheck = null;
  }

  private void ApplyPendingAndRebuild()
  {
    string selectedId = IdOf(menu.Current);
    SlidingRow selected = menu.Current as SlidingRow;
    Events.ApplyPending();
    TrialsMenu previous = menu;
    previous.Focused = false;
    menu = BuildMenu();
    int index = selectedId == null ? -1 : menu.Items.FindIndex(item => IdOf(item) == selectedId);
    menu.Selection = index < 0 ? 0 : index;
    menu.X = previous.X;
    menu.Alpha = previous.Alpha;
    previous.RemoveSelf();
    Scene.Add(menu);
    if (selected != null && menu.Current is SlidingRow current)
      current.TakeSlideFrom(selected);
    menu.Current?.OnEnter?.Invoke();
    menu.Focused = true;
  }

  private static void MarkSeen(TextMenu.Item item)
  {
    switch (item)
    {
      case EventRow row when Events.Of(row.Campaign) is EventCampaign entry:
        Events.MarkSeen(entry.Id, entry.Phase(Events.Now));
        break;
      case UpcomingEventRow row:
        Events.MarkSeen(row.Entry.Id, EventPhase.Upcoming);
        break;
    }
  }

  private static string IdOf(TextMenu.Item item) =>
    item switch
    {
      CampaignRow row => row.Campaign.Id,
      UpcomingEventRow row => row.Entry.Id,
      _ => null,
    };
}
