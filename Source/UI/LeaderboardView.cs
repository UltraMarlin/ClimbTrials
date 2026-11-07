using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class LeaderboardView : Entity
{
  private const float MessageScale = 0.8f;
  private const float PromptGap = 48f;
  private const float BlockedPromptAlpha = 0.4f;
  private const int FailedFetchesBeforeNotice = 3;
  private const float FailedFetchDim = 0.5f;
  private const int PrefetchPages = 3;

  public readonly record struct Bookmark(string SubjectId, int Page, int Selection);

  private enum BoardState
  {
    Loading,
    Ready,
    Offline,
    EventGone,
  }

  private sealed class Shown(BoardSubject subject)
  {
    public readonly BoardSubject Subject = subject;
    public Board Chunk;
    public BoardState State = BoardState.Loading;
    public BoardMenu Menu;
    public readonly List<BoardMenu> Leaving = [];
    public int Turns;
    public float X = ListScreen.OnScreenX;
    public float Alpha = 1f;
  }

  private const int KeepSelection = int.MinValue;

  private readonly IReadOnlyList<BoardSubject> siblings;
  private readonly Action<float> slid;
  private readonly Action<BoardSubject> closed;
  private readonly Action<Trial, Replay, bool> ghost;
  private readonly BoardOptions options;
  private readonly TransientText notice = new();
  private readonly Bookmark? resume;
  private readonly HashSet<string> pendingAdds = [];
  private Shown current;
  private Shown outgoing;
  private GhostMenu ghostMenu;
  private Backdrop backdrop;
  private float dim = 1f;
  private bool downloading;
  private int subjectIndex;
  private int page = 1;
  private int requestSequence;
  private int selectAfter;
  private int pageDirection;
  private int loadingDirection;
  private int waits;
  private bool sliding;
  private bool ended;
  private readonly float coveredFrom;
  private float backdropFrom;
  private float opened;
  private float coveredBeneath;
  private float rise;

  public bool Focused { get; set; } = true;

  public Action<BoardSubject> OnSubjectChanged;

  private bool Switchable => siblings.Count > 1;

  private MenuKeys Keys => options.Keys;

  private static bool RetryAllowed => !ClimbTrialsModule.Settings.OfflineMode;

  private bool BoardFetchesFailing =>
    current.State == BoardState.Ready
    && Standings.FailedBoardFetchesInARow >= FailedFetchesBeforeNotice;

  public LeaderboardView(
    BoardSubject subject,
    Action<float> slid,
    Action<BoardSubject> closed,
    Action<Trial, Replay, bool> ghost = null,
    Bookmark? resume = null,
    BoardOptions options = null,
    float coveredFrom = 0f,
    float backdropFrom = 0f
  )
  {
    this.coveredFrom = coveredFrom;
    this.backdropFrom = backdropFrom;
    this.slid = slid;
    this.closed = closed;
    this.ghost = ghost;
    this.resume = resume;
    this.options = options ?? BoardOptions.Overworld;
    siblings = subject.Siblings;
    subjectIndex = IndexOf(subject);
    current = new Shown(subject);
    page = resume?.Page ?? 1;
    Tag = Tags.HUD | Tags.PauseUpdate;
    Depth = this.options.DepthBase - 1;
  }

  public Bookmark CurrentBookmark =>
    new(current.Subject.Id, page, current.Menu?.Slot ?? BoardMenu.HeaderSlot);

  private bool Gone => ended || Scene == null;

  private int IndexOf(BoardSubject subject)
  {
    for (int i = 0; i < siblings.Count; i++)
      if (siblings[i].Id == subject.Id)
        return i;
    return 0;
  }

  public override void Added(Scene scene)
  {
    base.Added(scene);
    if (options.BackdropAlpha > 0f)
    {
      backdrop = new Backdrop(this);
      scene.Add(backdrop);
    }
    if (coveredFrom > 0f)
      SetOpened(0f, coveredFrom, Motion.OpenRise);
    Fetch(resume?.Selection ?? 0);
    ShowHeaderWhileLoading(current);

    if (resume == null)
      Add(new Coroutine(Open()));
    else
      SetOpened(1f, 1f, 0f);
  }

  private IEnumerator Open()
  {
    sliding = true;
    bool overworld = !options.InsideLevel;
    if (overworld)
    {
      Transition transition = Motion.BoardCover;
      float start = transition.InStart * coveredFrom;
      for (float elapsed = start; elapsed < transition.Duration; elapsed += Engine.RawDeltaTime)
      {
        SetCover(transition, elapsed, Math.Max(coveredFrom, transition.Leaving(elapsed)));
        yield return null;
      }
    }
    else
    {
      Transition transition = Motion.LevelBoardOpen;
      for (float elapsed = 0f; elapsed < transition.Duration; elapsed += Engine.RawDeltaTime)
      {
        SetCover(transition, elapsed, transition.Leaving(elapsed));
        yield return null;
      }
    }
    SetOpened(1f, 1f, 0f);
    backdropFrom = 0f;
    sliding = false;
  }

  private IEnumerator FadeOut()
  {
    sliding = true;
    Transition transition = Motion.BoardCover;
    for (float elapsed = 0f; elapsed < transition.Duration; elapsed += Engine.RawDeltaTime)
    {
      float along = transition.Duration - elapsed;
      SetCover(transition, along, transition.Leaving(along));
      yield return null;
    }
    SetOpened(0f, 0f, 0f);
    Discard();
    closed(current.Subject);
  }

  private void SetCover(Transition transition, float elapsed, float beneath) =>
    SetOpened(transition.Arriving(elapsed), beneath, Motion.Rise(transition.ArrivingMove(elapsed)));

  private void SetOpened(float rows, float under, float lift)
  {
    opened = rows;
    coveredBeneath = under;
    rise = lift;
    Place();
    slid(coveredBeneath);
  }

  private void Place()
  {
    PlaceMenu(current);
    if (outgoing != null)
      PlaceMenu(outgoing);
  }

  private void PlaceMenu(Shown shown)
  {
    float lift = shown.Menu?.TopLift ?? 0f;
    Vector2 at = new(shown.X, BoardLayout.MenuTop - lift + rise);
    float alpha = shown.Alpha * dim * opened;
    if (shown.Menu != null)
    {
      shown.Menu.Position = at;
      shown.Menu.Alpha = alpha;
      shown.Menu.SetFocus(Focused && !sliding && ghostMenu == null && shown == current);
      shown.Menu.LoadingDirection = shown == current ? loadingDirection : 0;
      bool failing = shown == current && BoardFetchesFailing;
      shown.Menu.RowsAlpha = failing ? FailedFetchDim : 1f;
    }
    foreach (BoardMenu leaving in shown.Leaving)
    {
      leaving.Position = at;
      leaving.Alpha = alpha;
    }
  }

  private void ShowMenu(Shown shown, int select, int turned = 0)
  {
    int slot = select == KeepSelection ? shown.Menu?.Slot ?? BoardMenu.HeaderSlot : select;
    BoardPage shownPage = shown.Chunk == null ? null : BoardPage.From(shown.Chunk, page);
    BoardMenu previous = shown.Menu;
    shown.Menu = new BoardMenu(shown.Subject, shownPage, Switchable, Keys)
    {
      OnPage = TurnPage,
      OnSwitch = Switch,
      OnEntry = OpenGhostMenu,
      Depth = options.DepthBase,
    };
    shown.Menu.SelectSlot(slot);
    Scene.Add(shown.Menu);
    previous?.StopInput();
    if (turned != 0 && previous?.HasEntries == true)
    {
      shown.Leaving.Add(previous);
      shown.Menu.MenuLosingHeader = previous;
      shown.Menu.EntryOffsetX = turned * Motion.PageDistance;
      shown.Menu.EntryAlpha = 0f;
      Add(new Coroutine(PageRoutine(shown, previous, turned, ++shown.Turns)));
    }
    else
    {
      shown.Menu.MenuToDiscard = previous;
      if (previous != null)
      {
        shown.Menu.EntryOffsetX = previous.EntryOffsetX;
        shown.Menu.EntryAlpha = previous.EntryAlpha;
      }
    }
    Place();
  }

  private IEnumerator PageRoutine(Shown shown, BoardMenu leaving, int direction, int turn)
  {
    while (shown.Menu.Scene == null)
      yield return null;
    Transition pageTurn = Motion.PageTurn;
    float startOffset = leaving.EntryOffsetX;
    float startAlpha = leaving.EntryAlpha;
    float leaveTo = -direction * Motion.PageDistance;
    for (float elapsed = 0f; elapsed < pageTurn.Duration; elapsed += Engine.RawDeltaTime)
    {
      leaving.EntryOffsetX = MathHelper.Lerp(startOffset, leaveTo, pageTurn.LeavingMove(elapsed));
      bool faded = elapsed >= pageTurn.OutSeconds;
      float leavingAlpha = Motion.PageAlphaAboveFloor(1f - pageTurn.Leaving(elapsed));
      leaving.EntryAlpha = faded ? 0f : startAlpha * leavingAlpha;
      if (turn == shown.Turns)
      {
        float arrived = pageTurn.ArrivingMove(elapsed);
        shown.Menu.EntryOffsetX = direction * Motion.PageDistance * (1f - arrived);
        bool appeared = elapsed >= pageTurn.InStart;
        float arrivingAlpha = Motion.PageAlphaAboveFloor(pageTurn.Arriving(elapsed));
        shown.Menu.EntryAlpha = appeared ? arrivingAlpha : 0f;
      }
      yield return null;
    }
    leaving.Discard();
    shown.Leaving.Remove(leaving);
    if (turn != shown.Turns)
      yield break;
    shown.Menu.EntryOffsetX = 0f;
    shown.Menu.EntryAlpha = 1f;
  }

  private void ShowHeaderWhileLoading(Shown shown)
  {
    if (shown.Menu == null)
      ShowMenu(shown, BoardMenu.HeaderSlot);
  }

  private void Fetch(int select)
  {
    Shown target = current;
    int sequence = ++requestSequence;
    selectAfter = select;
    SetLoadingDirection(0);
    target.State = BoardState.Loading;
    target.Subject.Fetch(
      BoardPage.ChunkOf(page),
      result =>
      {
        if (sequence != requestSequence || Gone)
          return;
        Receive(target, result);
      }
    );
  }

  private void Receive(Shown target, ServerResult<Board> result)
  {
    if (!result.Ok)
    {
      bool gone = result.Error == ServerError.NotFound && target.Subject.Event != null;
      if (gone)
        Standings.MarkHubStale();
      else
        Standings.CountFailedBoardFetch();
      pageDirection = 0;
      if (!gone && target.Chunk != null && BoardPage.Covers(target.Chunk, page))
      {
        target.State = BoardState.Ready;
        return;
      }

      target.State = gone ? BoardState.EventGone : BoardState.Offline;
      target.Chunk = null;
      ShowMenu(target, BoardMenu.HeaderSlot);
      return;
    }

    target.Chunk = result.Value;
    target.State = BoardState.Ready;
    int pages = BoardPage.PageCount(result.Value.Total);
    if (page > pages)
    {
      page = pages;
      if (!BoardPage.Covers(result.Value, page))
      {
        Fetch(selectAfter);
        return;
      }
    }

    ShowMenu(target, selectAfter, pageDirection);
    selectAfter = KeepSelection;
    pageDirection = 0;
    PrefetchNearbyChunks();
  }

  private void PrefetchNearbyChunks()
  {
    Board chunk = current.Chunk;
    if (chunk == null)
      return;

    int shownChunk = BoardPage.ChunkOf(page);
    int nextChunk = BoardPage.ChunkOf(page + PrefetchPages);
    int previousChunk = BoardPage.ChunkOf(Math.Max(1, page - PrefetchPages));
    BoardSubject subject = current.Subject;
    ServerClient.RunWithoutIcon(() =>
    {
      if (
        nextChunk != shownChunk
        && chunk.Total is int total
        && nextChunk * Standings.ChunkRows < total
      )
        subject.Fetch(nextChunk, _ => { });
      if (previousChunk != shownChunk)
        subject.Fetch(previousChunk, _ => { });
    });
  }

  private void TurnPage(int direction)
  {
    if (current.Menu?.HasPage(direction) != true || loadingDirection != 0)
      return;

    int target = page + direction;
    int chunk = BoardPage.ChunkOf(target);
    bool targetInShownChunk = current.Chunk != null && BoardPage.Covers(current.Chunk, target);
    if (!targetInShownChunk && !current.Subject.IsChunkCached(chunk))
    {
      WaitForChunk(chunk, direction);
      return;
    }

    page = target;
    PlayPageSound(direction);
    if (!targetInShownChunk)
    {
      pageDirection = TurnDirectionFor(direction);
      Fetch(KeepSelection);
      return;
    }

    ShowMenu(current, KeepSelection, TurnDirectionFor(direction));
    PrefetchNearbyChunks();
  }

  private void WaitForChunk(int chunk, int direction)
  {
    SetLoadingDirection(direction);
    int wait = waits;
    current.Subject.Fetch(
      chunk,
      _ =>
      {
        if (wait == waits)
          SetLoadingDirection(0);
      }
    );
  }

  private void SetLoadingDirection(int direction)
  {
    if (loadingDirection == 0 && direction != 0)
      ServerClient.HoldIcon();
    else if (loadingDirection != 0 && direction == 0)
      ServerClient.ReleaseIcon();
    if (direction != loadingDirection)
      waits++;
    loadingDirection = direction;
  }

  private int TurnDirectionFor(int direction) => Keys.SideRepeating ? 0 : direction;

  private static void PlayPageSound(int direction) =>
    Audio.Play(direction > 0 ? SFX.ui_main_roll_down : SFX.ui_main_roll_up);

  private void Switch(int direction)
  {
    if (outgoing != null || !Switchable)
      return;
    int count = siblings.Count;
    subjectIndex = (subjectIndex + direction + count) % count;
    Audio.Play(direction > 0 ? SFX.ui_main_button_toggle_on : SFX.ui_main_button_toggle_off);
    outgoing = current;
    current = new Shown(siblings[subjectIndex]) { Alpha = 0f };
    OnSubjectChanged?.Invoke(current.Subject);
    page = 1;
    Fetch(BoardMenu.HeaderSlot);
    ShowHeaderWhileLoading(current);
    Add(new Coroutine(SwitchRoutine(direction)));
  }

  private IEnumerator SwitchRoutine(int direction)
  {
    yield return Motion.Slide(
      Motion.ScreenSlide,
      direction,
      Motion.SlideDistance,
      (outX, outAlpha, inX, inAlpha) =>
      {
        outgoing.X = ListScreen.OnScreenX + outX;
        outgoing.Alpha = outAlpha;
        current.X = ListScreen.OnScreenX + inX;
        current.Alpha = inAlpha;
        Place();
      }
    );
    outgoing.Menu?.Discard();
    DiscardLeaving(outgoing);
    outgoing = null;
    current.X = ListScreen.OnScreenX;
    current.Alpha = 1f;
    Place();
  }

  private static void DiscardLeaving(Shown shown)
  {
    foreach (BoardMenu leaving in shown.Leaving)
      leaving.Discard();
    shown.Leaving.Clear();
  }

  public override void SceneEnd(Scene scene)
  {
    base.SceneEnd(scene);
    ended = true;
    SetLoadingDirection(0);
  }

  public void Discard()
  {
    ended = true;
    requestSequence++;
    SetLoadingDirection(0);
    ghostMenu?.Discard();
    ghostMenu = null;
    current.Menu?.Discard();
    DiscardLeaving(current);
    outgoing?.Menu?.Discard();
    if (outgoing != null)
      DiscardLeaving(outgoing);
    backdrop?.RemoveSelf();
    RemoveSelf();
  }

  private void Close()
  {
    Audio.Play(SFX.ui_main_button_back);
    requestSequence++;
    if (!options.InsideLevel)
    {
      Add(new Coroutine(FadeOut()));
      return;
    }
    slid(0f);
    Discard();
    closed(current.Subject);
  }

  public override void Update()
  {
    base.Update();
    notice.Update(Engine.RawDeltaTime);
    dim = ghostMenu?.DimUnder ?? 1f;
    Place();
    if (!Focused || sliding || ghostMenu != null)
      return;

    if (Keys.Close)
    {
      Close();
      return;
    }

    if (current.State == BoardState.Offline && RetryAllowed && Keys.Confirm)
    {
      Audio.Play(SFX.ui_main_button_select);
      Fetch(0);
      return;
    }

    if (Comparing && Keys.ToggleComparison)
      ToggleHovered();
    else if (Comparing && Keys.ClearComparisons && GhostComparisons.All.Count > 0)
    {
      GhostComparisons.Clear();
      Audio.Play(SFX.ui_main_button_toggle_off);
    }
  }

  public override void Render()
  {
    DrawUnderMenu(current);
    if (outgoing != null)
      DrawUnderMenu(outgoing);
    if (sliding)
      return;

    notice.Render(new Vector2(current.X, ScreenLayout.NoticeY), ScreenLayout.NoticeScale, 1f);
    if (BoardFetchesFailing && !notice.Showing)
      TransientText.DrawLine(
        Dialog.Clean("climbtrials_board_update_failed"),
        new Vector2(current.X, ScreenLayout.NoticeY),
        ScreenLayout.NoticeScale,
        Color.White * current.Alpha
      );
    if (outgoing == null && current.State == BoardState.Offline && RetryAllowed)
      ButtonRow.DrawCenter(Dialog.Clean("climbtrials_board_retry"), Input.MenuConfirm, dim);
    else if (Comparing)
      DrawComparisonPrompts();
  }

  private void DrawComparisonPrompts()
  {
    ClimbTrialsModuleSettings binds = ClimbTrialsModule.Settings;
    if (current.Menu?.HoveredRow is BoardRow row)
    {
      bool added = GhostComparisons.Find(row) != null;
      string label = Dialog.Clean(
        added ? "climbtrials_remove_comparison" : "climbtrials_add_comparison"
      );
      bool blocked = !added && (ComparisonReason(row) != null || pendingAdds.Contains(Key(row)));
      float alpha = dim * (blocked ? BlockedPromptAlpha : 1f);
      ButtonRow.DrawCenter(label, binds.ToggleComparison.Button, alpha);
    }
    if (GhostComparisons.All.Count == 0)
      return;
    VirtualButton toggle = binds.ToggleComparison.Button;
    float removeWidth = ButtonRow.Width(Dialog.Clean("climbtrials_remove_comparison"), toggle);
    float addWidth = ButtonRow.Width(Dialog.Clean("climbtrials_add_comparison"), toggle);
    string clear = Dialog.Clean("climbtrials_clear_comparisons");
    float toggleHalf = Math.Max(removeWidth, addWidth) / 2f;
    float clearHalf = ButtonRow.Width(clear, binds.ClearComparisons.Button) / 2f;
    float centerX = ButtonRow.CenterX + toggleHalf + PromptGap + clearHalf;
    ButtonRow.DrawCenter(clear, binds.ClearComparisons.Button, dim, centerX);
  }

  private bool Comparing => options.Comparing || (options.Plans && GhostComparisons.Planning);

  private enum GhostChoice
  {
    Watch,
    Race,
    Plan,
  }

  private void OpenGhostMenu(BoardRow row)
  {
    bool timed = row.Ticks.HasValue;
    if (ghost == null || current.Subject.Trial == null || ghostMenu != null || !timed)
      return;

    Audio.Play(SFX.ui_main_button_select);
    Replay local = LocalPb(row);
    bool watched = GhostComparisons.IsWatched(row);
    List<GhostOption> choices = [];
    if (!watched)
      choices.Add(
        new(
          Dialog.Clean(Comparing ? "climbtrials_switch_replay" : "climbtrials_watch_replay"),
          () => GhostReason(row, local),
          () => ChooseGhost(row, local, GhostChoice.Watch)
        )
      );
    choices.Add(
      new(
        Dialog.Clean("climbtrials_race_this_ghost"),
        () => GhostReason(row, local),
        () => ChooseGhost(row, local, GhostChoice.Race)
      )
    );
    if (GhostComparisons.Planning && watched)
      choices.Add(
        new(Dialog.Clean("climbtrials_cancel_comparison"), () => null, () => CancelPlan())
      );
    else if (Comparing && GhostComparisons.Find(row) != null)
      choices.Add(
        new(Dialog.Clean("climbtrials_remove_comparison"), () => null, () => RemoveFromMenu(row))
      );
    else if (Comparing && !watched)
      choices.Add(
        new(
          Dialog.Clean("climbtrials_add_comparison"),
          () => GhostReason(row, local),
          () => AddFromMenu(row, local)
        )
      );
    else if (options.Plans)
      choices.Add(
        new(
          Dialog.Clean("climbtrials_start_comparison"),
          () => GhostReason(row, local),
          () => ChooseGhost(row, local, GhostChoice.Plan)
        )
      );
    ghostMenu = new GhostMenu(
      row,
      current.Subject,
      current.Chunk?.BoardMode ?? BoardMode.FullTimes,
      choices,
      Keys,
      options.InsideLevel,
      () => ghostMenu = null
    )
    {
      Depth = options.DepthBase - 2,
    };
    Scene.Add(ghostMenu);
    Place();
  }

  private Replay LocalPb(BoardRow row)
  {
    Trial trial = current.Subject.Trial;
    if (!row.Me || !ReplayStore.Exists(trial.Id))
      return null;
    if (!ReplayStore.TryLoad(trial, out Replay replay, out string failureNotice))
    {
      notice.Show(failureNotice, ScreenLayout.NoticeSeconds);
      return null;
    }
    return replay.Ticks == row.Ticks ? replay : null;
  }

  private string GhostReason(BoardRow row, Replay local)
  {
    if (local != null)
      return null;
    if (!row.Me && row.ReplayId == null && ReplaysLocked)
      return EventTiming.LockedReplays(current.Subject.Event);
    if (row.ReplayId == null)
      return Dialog.Clean("climbtrials_ghost_no_replay");
    if (row.ReplayFormat != Replay.FormatVersion)
      return Dialog.Clean("climbtrials_ghost_old_format");
    if (GhostCache.IsOlderTrial(row.ReplayId, row.Ticks.GetValueOrDefault()))
      return Dialog.Clean("climbtrials_ghost_old_trial");
    if (ClimbTrialsModule.Settings.OfflineMode)
      return Dialog.Clean("climbtrials_offline_mode");
    return downloading ? Dialog.Clean("climbtrials_ghost_downloading") : null;
  }

  private bool ReplaysLocked =>
    current.Chunk?.ReplayMode == ReplayMode.OrganizersOnly && current.Subject.Event != null;

  private static string WatchedReason(BoardRow row) =>
    GhostComparisons.IsWatched(row) ? Dialog.Clean("climbtrials_ghost_watching") : null;

  private string ComparisonReason(BoardRow row) =>
    (row.Me && ReplayStore.Exists(current.Subject.Trial.Id) ? null : GhostReason(row, null))
    ?? WatchedReason(row);

  private static string Key(BoardRow row) => GhostComparisons.KeyOf(row) ?? "";

  private void ChooseGhost(BoardRow row, Replay local, GhostChoice choice) =>
    FetchGhostThen(row, local, (trial, replay) => UseGhost(row, trial, replay, choice));

  private void FetchGhostThen(BoardRow row, Replay local, Action<Trial, Replay> useReplay)
  {
    Trial trial = current.Subject.Trial;
    if (local != null)
    {
      useReplay(trial, local);
      return;
    }
    downloading = true;
    GhostCache.Fetch(
      row.ReplayId,
      row.Ticks.GetValueOrDefault(),
      row.Me,
      trial,
      replay =>
      {
        downloading = false;
        if (Gone || ghostMenu == null || ghostMenu.Closing)
          return;

        if (replay == null)
        {
          if (!GhostCache.IsOlderTrial(row.ReplayId, row.Ticks.GetValueOrDefault()))
            notice.Show(Dialog.Clean("climbtrials_ghost_failed"), ScreenLayout.NoticeSeconds);
          return;
        }

        RefreshIfOutdated(row, replay);
        useReplay(trial, replay);
      }
    );
  }

  private void UseGhost(BoardRow row, Trial trial, Replay replay, GhostChoice choice)
  {
    ghostMenu?.Dismiss();
    if (choice == GhostChoice.Plan)
      GhostComparisons.Plan(row, replay, trial);
    else if (choice == GhostChoice.Watch && Comparing)
      GhostComparisons.Switch(row, replay, trial, WatchedRank());
    else
      ghost(trial, replay, choice == GhostChoice.Race);
  }

  private void CancelPlan()
  {
    GhostComparisons.Forget();
    ghostMenu?.Dismiss();
  }

  private int WatchedRank()
  {
    Board chunk = current.Chunk;
    if (chunk?.Rows.Find(GhostComparisons.IsWatched) is BoardRow row)
      return row.RankOrLast;
    return chunk?.Me is BoardRow me && GhostComparisons.IsWatched(me)
      ? me.RankOrLast
      : int.MaxValue;
  }

  private void AddFromMenu(BoardRow row, Replay local) =>
    FetchGhostThen(
      row,
      local,
      (trial, replay) =>
      {
        GhostComparisons.Add(row, replay, trial);
        ghostMenu?.Dismiss();
      }
    );

  private void RemoveFromMenu(BoardRow row)
  {
    GhostComparisons.Remove(row);
    ghostMenu?.Dismiss();
  }

  private void ToggleHovered()
  {
    if (current.Menu?.HoveredRow is not BoardRow row || pendingAdds.Contains(Key(row)))
      return;
    if (GhostComparisons.Find(row) != null)
    {
      GhostComparisons.Remove(row);
      Audio.Play(SFX.ui_main_button_toggle_off);
      return;
    }
    Replay local = LocalPb(row);
    string reason = GhostReason(row, local) ?? WatchedReason(row);
    if (reason != null)
    {
      notice.Show(reason, ScreenLayout.NoticeSeconds);
      return;
    }
    Trial trial = current.Subject.Trial;
    if (local != null)
    {
      AddWithToggleSound(row, local, trial);
      return;
    }
    string key = Key(row);
    pendingAdds.Add(key);
    GhostCache.Fetch(
      row.ReplayId,
      row.Ticks.GetValueOrDefault(),
      row.Me,
      trial,
      replay =>
      {
        pendingAdds.Remove(key);
        if (Gone)
          return;
        if (replay == null)
        {
          notice.Show(
            GhostReason(row, null) ?? Dialog.Clean("climbtrials_ghost_failed"),
            ScreenLayout.NoticeSeconds
          );
          return;
        }
        RefreshIfOutdated(row, replay);
        AddWithToggleSound(row, replay, trial);
      }
    );
  }

  private static void AddWithToggleSound(BoardRow row, Replay replay, Trial trial)
  {
    GhostComparisons.Add(row, replay, trial);
    Audio.Play(SFX.ui_main_button_toggle_on);
  }

  private void RefreshIfOutdated(BoardRow row, Replay replay)
  {
    if (replay.Ticks == row.Ticks || current.Subject.Trial is not Trial trial)
      return;
    notice.Show(
      Dialog.Clean("climbtrials_board_outdated"),
      ScreenLayout.NoticeSeconds,
      warning: false
    );
    Standings.DropTrialBoard(trial);
    Fetch(KeepSelection);
  }

  private void DrawUnderMenu(Shown shown)
  {
    if (shown.State == BoardState.Offline)
    {
      DrawMessage("climbtrials_board_unavailable", shown);
      return;
    }
    if (shown.State == BoardState.EventGone)
    {
      DrawMessage("climbtrials_event_gone", shown);
      return;
    }
    BoardPage shownPage = shown.Menu?.Page;
    if (shownPage == null || shownPage.Rows.Count > 0)
      return;
    if (shownPage.BoardMode != BoardMode.OwnTimeOnly)
      DrawMessage("climbtrials_board_no_times", shown);
    else if (shownPage.Me == null)
      DrawMessage("climbtrials_board_no_own_time", shown);
  }

  private void DrawMessage(string key, Shown shown)
  {
    Color color = Color.White * (shown.Alpha * dim * opened);
    ActiveFont.DrawOutline(
      Dialog.Clean(key),
      new Vector2(shown.X, BoardLayout.MessageY + rise),
      new Vector2(0.5f, 0.5f),
      Vector2.One * MessageScale,
      color,
      2f,
      TextOutline.StrokeFor(color)
    );
  }

  private sealed class Backdrop : Entity
  {
    private readonly LeaderboardView view;

    public Backdrop(LeaderboardView view)
    {
      this.view = view;
      Tag = Tags.HUD | Tags.PauseUpdate;
      Depth = view.options.DepthBase + 1;
    }

    public override void Render()
    {
      float alpha = view.options.BackdropAlpha * view.coveredBeneath;
      if (view.backdropFrom > 0f)
        alpha = MathHelper.Lerp(view.backdropFrom, alpha, view.opened);
      Draw.Rect(-1f, -1f, 1922f, 1082f, Color.Black * alpha);
    }
  }
}
