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
  private const float NoticeY = 975f;
  private const float NoticeScale = 0.6f;
  private const float NoticeSeconds = 5f;
  private const float PromptGap = 48f;
  private const float BlockedPromptAlpha = 0.4f;

  public readonly record struct Bookmark(string SubjectId, int Page, int Selection);

  private enum BoardState
  {
    Loading,
    Ready,
    Offline,
  }

  private sealed class Shown(BoardSubject subject)
  {
    public readonly BoardSubject Subject = subject;
    public Board Chunk;
    public BoardState State = BoardState.Loading;
    public BoardMenu Menu;
    public BoardMenu Leaving;
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
  private bool sliding;
  private bool ended;
  private readonly float coveredFrom;
  private float backdropFrom;
  private float opened;
  private float cover;
  private float rise;

  public bool Focused { get; set; } = true;

  private bool Switchable => siblings.Count > 1;

  private MenuKeys Keys => options.Keys;

  private static bool RetryAllowed => !ClimbTrialsModule.Settings.OfflineMode;

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

  public Bookmark Showing =>
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
      Transition cover = Motion.BoardCover;
      float start = cover.InStart * coveredFrom;
      for (float elapsed = start; elapsed < cover.Duration; elapsed += Engine.RawDeltaTime)
      {
        SetCover(cover, elapsed, Math.Max(coveredFrom, cover.Leaving(elapsed)));
        yield return null;
      }
    }
    else
    {
      Transition open = Motion.LevelBoardOpen;
      for (float elapsed = 0f; elapsed < open.Duration; elapsed += Engine.RawDeltaTime)
      {
        SetCover(open, elapsed, open.Leaving(elapsed));
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
    Transition cover = Motion.BoardCover;
    for (float elapsed = 0f; elapsed < cover.Duration; elapsed += Engine.RawDeltaTime)
    {
      float along = cover.Duration - elapsed;
      SetCover(cover, along, cover.Leaving(along));
      yield return null;
    }
    SetOpened(0f, 0f, 0f);
    Discard();
    closed(current.Subject);
  }

  private void SetCover(Transition cover, float elapsed, float beneath) =>
    SetOpened(cover.Arriving(elapsed), beneath, Motion.Rise(cover.ArrivingMove(elapsed)));

  private void SetOpened(float rows, float under, float lift)
  {
    opened = rows;
    cover = under;
    rise = lift;
    Place();
    slid(cover);
  }

  private void Place()
  {
    PlaceMenu(current);
    if (outgoing != null)
      PlaceMenu(outgoing);
  }

  private void PlaceMenu(Shown shown)
  {
    Vector2 at = new(shown.X, BoardLayout.MenuTop + rise);
    float alpha = shown.Alpha * dim * opened;
    if (shown.Menu != null)
    {
      shown.Menu.Position = at;
      shown.Menu.Alpha = alpha;
      shown.Menu.SetFocus(Focused && !sliding && ghostMenu == null && shown == current);
    }
    if (shown.Leaving != null)
    {
      shown.Leaving.Position = at;
      shown.Leaving.Alpha = alpha;
      shown.Leaving.Focused = false;
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
    if (turned != 0 && previous?.HasEntries == true)
    {
      shown.Leaving?.Discard();
      shown.Leaving = previous;
      previous.RenderAsFocused = true;
      shown.Menu.EntryOffsetX = turned * Motion.PageDistance;
      shown.Menu.EntryAlpha = 0f;
      Add(new Coroutine(PageRoutine(shown, previous, shown.Menu, turned)));
    }
    else
      previous?.Discard();
    Place();
  }

  private static IEnumerator PageRoutine(
    Shown shown,
    BoardMenu leaving,
    BoardMenu entering,
    int direction
  )
  {
    while (entering.Scene == null)
      yield return null;
    leaving.HeaderVisible = false;
    leaving.RenderAsFocused = false;
    yield return Motion.Slide(
      Motion.PageTurn,
      direction,
      Motion.PageDistance,
      (outX, outAlpha, inX, inAlpha) =>
      {
        leaving.EntryOffsetX = outX;
        leaving.EntryAlpha = outAlpha;
        if (shown.Menu != entering)
          return;
        entering.EntryOffsetX = inX;
        entering.EntryAlpha = inAlpha;
      }
    );
    leaving.Discard();
    if (shown.Leaving == leaving)
      shown.Leaving = null;
    if (shown.Menu == entering)
    {
      entering.EntryOffsetX = 0f;
      entering.EntryAlpha = 1f;
    }
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
    target.State = BoardState.Loading;
    target.Subject.Fetch(
      BoardPage.ChunkOf(page),
      options.MaxAgeSeconds,
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
      target.State = BoardState.Offline;
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
    pageDirection = 0;
    PrefetchNextChunk();
  }

  private void PrefetchNextChunk()
  {
    Board chunk = current.Chunk;
    if (chunk == null || !BoardPage.LastOfChunk(page))
      return;

    int next = BoardPage.ChunkOf(page) + 1;
    if (next * Standings.ChunkRows >= chunk.Total)
      return;

    current.Subject.Fetch(next, options.MaxAgeSeconds, _ => { });
  }

  private void TurnPage(int direction)
  {
    if (current.Menu?.HasPage(direction) != true)
      return;
    page += direction;
    pageDirection = direction;
    Audio.Play(direction > 0 ? SFX.ui_main_roll_down : SFX.ui_main_roll_up);
    if (current.Chunk != null && BoardPage.Covers(current.Chunk, page))
    {
      ShowMenu(current, KeepSelection, direction);
      pageDirection = 0;
      PrefetchNextChunk();
      return;
    }
    Fetch(KeepSelection);
  }

  private void Switch(int direction)
  {
    if (outgoing != null || !Switchable)
      return;
    int count = siblings.Count;
    subjectIndex = (subjectIndex + direction + count) % count;
    Audio.Play(direction > 0 ? SFX.ui_main_button_toggle_on : SFX.ui_main_button_toggle_off);
    outgoing = current;
    current = new Shown(siblings[subjectIndex]) { Alpha = 0f };
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
    outgoing.Leaving?.Discard();
    outgoing = null;
    current.X = ListScreen.OnScreenX;
    current.Alpha = 1f;
    Place();
  }

  public override void SceneEnd(Scene scene)
  {
    base.SceneEnd(scene);
    ended = true;
  }

  public void Discard()
  {
    ended = true;
    requestSequence++;
    ghostMenu?.Discard();
    ghostMenu = null;
    current.Menu?.Discard();
    current.Leaving?.Discard();
    outgoing?.Menu?.Discard();
    outgoing?.Leaving?.Discard();
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

    notice.Render(new Vector2(current.X, NoticeY), NoticeScale, 1f);
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
    string longest = Dialog.Clean("climbtrials_remove_comparison");
    string clear = Dialog.Clean("climbtrials_clear_comparisons");
    float toggleHalf = ButtonRow.Width(longest, binds.ToggleComparison.Button) / 2f;
    float clearHalf = ButtonRow.Width(clear, binds.ClearComparisons.Button) / 2f;
    float x = ButtonRow.CenterX + toggleHalf + PromptGap + clearHalf;
    ButtonRow.DrawCenter(clear, binds.ClearComparisons.Button, dim, x);
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
    if (ghost == null || current.Subject.Trial == null || ghostMenu != null)
      return;

    Audio.Play(SFX.ui_main_button_select);
    Replay local = LocalPb(row);
    List<GhostOption> choices =
    [
      new(
        Dialog.Clean(Comparing ? "climbtrials_switch_replay" : "climbtrials_watch_replay"),
        () => GhostReason(row, local) ?? WatchedReason(row),
        () => ChooseGhost(row, local, GhostChoice.Watch)
      ),
      new(
        Dialog.Clean("climbtrials_race_this_ghost"),
        () => GhostReason(row, local),
        () => ChooseGhost(row, local, GhostChoice.Race)
      ),
    ];
    if (GhostComparisons.Planning && GhostComparisons.IsWatched(row))
      choices.Add(
        new(Dialog.Clean("climbtrials_cancel_comparison"), () => null, () => CancelPlan())
      );
    else if (Comparing && GhostComparisons.Find(row) != null)
      choices.Add(
        new(Dialog.Clean("climbtrials_remove_comparison"), () => null, () => RemoveFromMenu(row))
      );
    else if (Comparing)
      choices.Add(
        new(
          Dialog.Clean("climbtrials_add_comparison"),
          () => GhostReason(row, local) ?? WatchedReason(row),
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
    if (!ReplayStore.TryLoad(trial, out Replay replay, out string failure))
    {
      notice.Show(Dialog.Get("climbtrials_replay_deleted").Replace("{0}", failure), NoticeSeconds);
      return null;
    }
    return replay.Ticks == row.Ticks ? replay : null;
  }

  private string GhostReason(BoardRow row, Replay local)
  {
    if (local != null)
      return null;
    if (row.ReplayId == null)
      return Dialog.Clean("climbtrials_ghost_no_replay");
    if (row.ReplayFormat != Replay.FormatVersion)
      return Dialog.Clean("climbtrials_ghost_old_format");
    if (GhostCache.IsOlderTrial(row.ReplayId, row.Ticks))
      return Dialog.Clean("climbtrials_ghost_old_trial");
    if (ClimbTrialsModule.Settings.OfflineMode)
      return Dialog.Clean("climbtrials_offline_mode");
    return downloading ? Dialog.Clean("climbtrials_ghost_downloading") : null;
  }

  private static string WatchedReason(BoardRow row) =>
    GhostComparisons.IsWatched(row) ? Dialog.Clean("climbtrials_ghost_watching") : null;

  private string ComparisonReason(BoardRow row) =>
    (row.Me && ReplayStore.Exists(current.Subject.Trial.Id) ? null : GhostReason(row, null))
    ?? WatchedReason(row);

  private static string Key(BoardRow row) => GhostComparisons.KeyOf(row) ?? "";

  private void ChooseGhost(BoardRow row, Replay local, GhostChoice choice)
  {
    Trial trial = current.Subject.Trial;
    if (local != null)
    {
      Chosen(row, trial, local, choice);
      return;
    }
    downloading = true;
    GhostCache.Fetch(
      row.ReplayId,
      row.Ticks,
      row.Me,
      trial,
      replay =>
      {
        downloading = false;
        if (Gone || ghostMenu == null || ghostMenu.Closing)
          return;

        if (replay == null)
        {
          if (!GhostCache.IsOlderTrial(row.ReplayId, row.Ticks))
            notice.Show(Dialog.Clean("climbtrials_ghost_failed"), NoticeSeconds);
          return;
        }

        RefreshIfOutdated(row, replay);
        Chosen(row, trial, replay, choice);
      }
    );
  }

  private void Chosen(BoardRow row, Trial trial, Replay replay, GhostChoice choice)
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
      return row.Rank;
    return chunk?.Me is BoardRow me && GhostComparisons.IsWatched(me) ? me.Rank : int.MaxValue;
  }

  private void AddFromMenu(BoardRow row, Replay local)
  {
    Trial trial = current.Subject.Trial;
    if (local != null)
    {
      GhostComparisons.Add(row, local, trial);
      ghostMenu?.Dismiss();
      return;
    }
    downloading = true;
    GhostCache.Fetch(
      row.ReplayId,
      row.Ticks,
      row.Me,
      trial,
      replay =>
      {
        downloading = false;
        if (Gone || ghostMenu == null || ghostMenu.Closing)
          return;
        if (replay == null)
        {
          if (!GhostCache.IsOlderTrial(row.ReplayId, row.Ticks))
            notice.Show(Dialog.Clean("climbtrials_ghost_failed"), NoticeSeconds);
          return;
        }
        RefreshIfOutdated(row, replay);
        GhostComparisons.Add(row, replay, trial);
        ghostMenu?.Dismiss();
      }
    );
  }

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
      notice.Show(reason, NoticeSeconds);
      return;
    }
    Trial trial = current.Subject.Trial;
    if (local != null)
    {
      Toggled(row, local, trial);
      return;
    }
    string key = Key(row);
    pendingAdds.Add(key);
    GhostCache.Fetch(
      row.ReplayId,
      row.Ticks,
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
            NoticeSeconds
          );
          return;
        }
        RefreshIfOutdated(row, replay);
        Toggled(row, replay, trial);
      }
    );
  }

  private static void Toggled(BoardRow row, Replay replay, Trial trial)
  {
    GhostComparisons.Add(row, replay, trial);
    Audio.Play(SFX.ui_main_button_toggle_on);
  }

  private void RefreshIfOutdated(BoardRow row, Replay replay)
  {
    if (replay.Ticks == row.Ticks || current.Subject.Trial is not Trial trial)
      return;
    notice.Show(Dialog.Clean("climbtrials_board_outdated"), NoticeSeconds, warning: false);
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
    BoardPage shownPage = shown.Menu?.Page;
    if (shownPage != null && shownPage.Rows.Count == 0)
      DrawMessage("climbtrials_board_no_times", shown);
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
      float alpha = view.options.BackdropAlpha * view.cover;
      if (view.backdropFrom > 0f)
        alpha = MathHelper.Lerp(view.backdropFrom, alpha, view.opened);
      Draw.Rect(-1f, -1f, 1922f, 1082f, Color.Black * alpha);
    }
  }
}
