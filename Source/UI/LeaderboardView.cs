using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class LeaderboardView : Entity
{
  public const float SlideDistance = 1080f;
  private const float MessageScale = 0.8f;
  private const float NoticeY = 975f;
  private const float NoticeScale = 0.6f;
  private const float NoticeSeconds = 5f;

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

  private const int KeepSelection = -1;
  private const int HeaderRow = 0;
  private const int FirstEntry = 1;

  private readonly IReadOnlyList<BoardSubject> siblings;
  private readonly Action<float> slid;
  private readonly Action<BoardSubject> closed;
  private readonly Action<Trial, Replay, bool> ghost;
  private readonly TransientText notice = new();
  private readonly Bookmark? resume;
  private Shown current;
  private Shown outgoing;
  private GhostMenu ghostMenu;
  private float dim = 1f;
  private bool downloading;
  private int subjectIndex;
  private int page = 1;
  private int requestSequence;
  private int selectAfter;
  private int pageDirection;
  private bool sliding;
  private float slideY = -SlideDistance;

  public bool Focused { get; set; } = true;

  private bool Switchable => siblings.Count > 1;

  private static bool RetryAllowed => !ClimbTrialsModule.Settings.OfflineMode;

  public LeaderboardView(
    BoardSubject subject,
    Action<float> slid,
    Action<BoardSubject> closed,
    Action<Trial, Replay, bool> ghost = null,
    Bookmark? resume = null
  )
  {
    this.slid = slid;
    this.closed = closed;
    this.ghost = ghost;
    this.resume = resume;
    siblings = subject.Siblings;
    subjectIndex = IndexOf(subject);
    current = new Shown(subject);
    page = resume?.Page ?? 1;
    Tag = Tags.HUD;
    Depth = -1;
  }

  public Bookmark Showing => new(current.Subject.Id, page, current.Menu?.Selection ?? HeaderRow);

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
    Fetch(resume?.Selection ?? FirstEntry);
    ShowHeaderWhileLoading(current);
    if (resume == null)
      Add(new Coroutine(Slide(opening: true)));
    else
      SetSlide(1f);
  }

  private IEnumerator Slide(bool opening)
  {
    sliding = true;
    for (float t = 0f; t < OuiTrials.SwitchDuration; t += Engine.DeltaTime)
    {
      float ease = Ease.CubeOut(t / OuiTrials.SwitchDuration);
      SetSlide(opening ? ease : 1f - ease);
      yield return null;
    }
    SetSlide(opening ? 1f : 0f);
    sliding = false;
    if (opening)
      yield break;
    Discard();
    closed(current.Subject);
  }

  private void SetSlide(float progress)
  {
    slideY = (progress - 1f) * SlideDistance;
    Place();
    slid(progress);
  }

  private void Place()
  {
    PlaceMenu(current);
    if (outgoing != null)
      PlaceMenu(outgoing);
  }

  private void PlaceMenu(Shown shown)
  {
    Vector2 at = new(shown.X, BoardLayout.MenuTop + slideY);
    if (shown.Menu != null)
    {
      shown.Menu.Position = at;
      shown.Menu.Alpha = shown.Alpha * dim;
      shown.Menu.Focused = Focused && !sliding && ghostMenu == null && shown == current;
    }
    if (shown.Leaving != null)
    {
      shown.Leaving.Position = at;
      shown.Leaving.Alpha = shown.Alpha * dim;
      shown.Leaving.Focused = false;
    }
  }

  private void ShowMenu(Shown shown, int select, int turned = 0)
  {
    int index = select == KeepSelection ? shown.Menu?.Selection ?? HeaderRow : select;
    BoardPage shownPage = shown.Chunk == null ? null : BoardPage.From(shown.Chunk, page);
    BoardMenu previous = shown.Menu;
    shown.Menu = new BoardMenu(shown.Subject, shownPage, Switchable)
    {
      OnPage = TurnPage,
      OnSwitch = Switch,
      OnEntry = OpenGhostMenu,
    };
    shown.Menu.Select(index);
    Scene.Add(shown.Menu);
    if (turned != 0 && previous?.HasEntries == true)
    {
      shown.Leaving?.Discard();
      shown.Leaving = previous;
      previous.RenderAsFocused = true;
      shown.Menu.EntryOffsetX = turned * BoardLayout.PageSlideDistance;
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
    float distance = BoardLayout.PageSlideDistance;
    for (float t = 0f; t < BoardLayout.PageSlideDuration; t += Engine.DeltaTime)
    {
      float ease = Ease.CubeOut(t / BoardLayout.PageSlideDuration);
      leaving.EntryOffsetX = -direction * distance * ease;
      leaving.EntryAlpha = 1f - ease;
      if (shown.Menu == entering)
      {
        entering.EntryOffsetX = direction * distance * (1f - ease);
        entering.EntryAlpha = ease;
      }
      yield return null;
    }
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
      ShowMenu(shown, HeaderRow);
  }

  private void Fetch(int select)
  {
    Shown target = current;
    int sequence = ++requestSequence;
    selectAfter = select;
    target.State = BoardState.Loading;
    target.Subject.Fetch(
      BoardPage.ChunkOf(page),
      result =>
      {
        if (sequence != requestSequence || Scene == null)
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
      ShowMenu(target, HeaderRow);
      return;
    }

    target.Chunk = result.Value;
    target.State = BoardState.Ready;
    int pages = BoardPage.CountFor(result.Value.Total);
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

    current.Subject.Fetch(next, _ => { });
  }

  private void TurnPage(int direction)
  {
    if (current.Menu?.HasPage(direction) != true)
      return;
    page += direction;
    pageDirection = direction;
    Audio.Play(direction > 0 ? "event:/ui/main/rollover_down" : "event:/ui/main/rollover_up");
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
    Audio.Play(
      direction > 0 ? "event:/ui/main/button_toggle_on" : "event:/ui/main/button_toggle_off"
    );
    outgoing = current;
    current = new Shown(siblings[subjectIndex]) { Alpha = 0f };
    page = 1;
    Fetch(HeaderRow);
    ShowHeaderWhileLoading(current);
    Add(new Coroutine(SwitchRoutine(direction)));
  }

  private IEnumerator SwitchRoutine(int direction)
  {
    yield return OuiTrials.SwitchSlide(
      direction,
      (outX, inX, ease) =>
      {
        outgoing.X = outX;
        outgoing.Alpha = 1f - ease;
        current.X = inX;
        current.Alpha = ease;
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

  public void Discard()
  {
    requestSequence++;
    ghostMenu?.Discard();
    ghostMenu = null;
    current.Menu?.Discard();
    current.Leaving?.Discard();
    outgoing?.Menu?.Discard();
    outgoing?.Leaving?.Discard();
    RemoveSelf();
  }

  private void Close()
  {
    Audio.Play(SFX.ui_main_button_back);
    requestSequence++;
    Add(new Coroutine(Slide(opening: false)));
  }

  public override void Update()
  {
    base.Update();
    notice.Update(Engine.RawDeltaTime);
    dim = ghostMenu?.DimUnder ?? 1f;
    Place();
    if (!Focused || sliding || ghostMenu != null)
      return;

    if (Input.MenuCancel.Pressed || Input.ESC.Pressed || Input.MenuJournal.Pressed)
    {
      Close();
      return;
    }

    if (current.State == BoardState.Offline && RetryAllowed && Input.MenuConfirm.Pressed)
    {
      Audio.Play(SFX.ui_main_button_select);
      Fetch(FirstEntry);
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
  }

  private void OpenGhostMenu(BoardRow row)
  {
    if (ghost == null || current.Subject.Trial == null || ghostMenu != null)
      return;

    Audio.Play(SFX.ui_main_button_select);
    Replay local = LocalPb(row);
    ghostMenu = new GhostMenu(
      row,
      current.Subject,
      () => GhostReason(row, local),
      watch: () => ChooseGhost(row, local, race: false),
      race: () => ChooseGhost(row, local, race: true),
      closed: () => ghostMenu = null
    );
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
    if (row.ReplayId == null || row.ReplayFormat != Replay.FormatVersion)
      return Dialog.Clean("climbtrials_ghost_old_format");
    if (ClimbTrialsModule.Settings.OfflineMode)
      return Dialog.Clean("climbtrials_offline_mode");
    return downloading ? Dialog.Clean("climbtrials_ghost_downloading") : null;
  }

  private void ChooseGhost(BoardRow row, Replay local, bool race)
  {
    Trial trial = current.Subject.Trial;
    if (local != null)
    {
      Chosen(trial, local, race);
      return;
    }
    downloading = true;
    GhostCache.Get(
      row.ReplayId,
      row.Me,
      replay =>
      {
        downloading = false;
        if (Scene == null || ghostMenu == null || ghostMenu.Closing)
          return;

        if (replay == null)
        {
          notice.Show(Dialog.Clean("climbtrials_ghost_failed"), NoticeSeconds);
          return;
        }

        Chosen(trial, replay, race);
      }
    );
  }

  private void Chosen(Trial trial, Replay replay, bool race)
  {
    ghostMenu?.Dismiss();
    ghost(trial, replay, race);
  }

  private void DrawUnderMenu(Shown shown)
  {
    if (shown.State == BoardState.Offline)
    {
      DrawMessage("climbtrials_board_unavailable", shown);
      return;
    }
    BoardPage shownPage = shown.Menu?.Page;
    if (shownPage == null)
      return;
    if (shownPage.Rows.Count == 0)
    {
      DrawMessage("climbtrials_board_no_times", shown);
      return;
    }
    if (shownPage.Me == null)
      return;
    float pageBottom = shown.Menu.Position.Y + BoardLayout.HeaderHeight + BoardMenu.EntriesSpan;
    float ruleY = pageBottom + BoardLayout.RuleGap;
    float left = shown.X - shown.Menu.Width / 2f;
    float alpha = shown.Alpha * dim;
    Color rule = Color.White * (BoardLayout.RuleAlpha * alpha);
    Draw.Rect(left, ruleY, shown.Menu.Width, 1f, rule);
    Vector2 rowLeft = new(left, ruleY + BoardLayout.RuleGap + BoardLayout.RowHeight / 2f);
    BoardMenu.DrawRow(shownPage.Me, shown.Subject, rowLeft, shown.Menu.Width, alpha, false);
  }

  private void DrawMessage(string key, Shown shown)
  {
    Color color = Color.White * (shown.Alpha * dim);
    ActiveFont.DrawOutline(
      Dialog.Clean(key),
      new Vector2(shown.X, BoardLayout.MessageY + slideY),
      new Vector2(0.5f, 0.5f),
      Vector2.One * MessageScale,
      color,
      2f,
      TextOutline.StrokeFor(color)
    );
  }
}
