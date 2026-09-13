using System;
using System.Collections;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class LeaderboardView : Entity
{
  public const float SlideDistance = 1080f;
  private const float MessageScale = 0.8f;

  private enum BoardState
  {
    Loading,
    Ready,
    Offline,
  }

  private sealed class Shown(Trial trial)
  {
    public readonly Trial Trial = trial;
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

  private readonly Campaign campaign;
  private readonly Action<float> slid;
  private readonly Action<Trial> closed;
  private Shown current;
  private Shown outgoing;
  private int trialIndex;
  private int page = 1;
  private int requestSequence;
  private int selectAfter;
  private int pageDirection;
  private bool sliding;
  private float slideY = -SlideDistance;

  private bool Switchable => campaign.Trials.Count > 1;

  public LeaderboardView(Trial trial, Action<float> slid, Action<Trial> closed)
  {
    this.slid = slid;
    this.closed = closed;
    campaign = Campaigns.CampaignOf(trial);
    trialIndex = IndexOf(trial);
    current = new Shown(trial);
    Tag = Tags.HUD;
    Depth = -1;
  }

  private int IndexOf(Trial trial)
  {
    for (int i = 0; i < campaign.Trials.Count; i++)
      if (campaign.Trials[i] == trial)
        return i;
    return 0;
  }

  public override void Added(Scene scene)
  {
    base.Added(scene);
    Fetch(FirstEntry);
    ShowHeaderWhileLoading(current);
    Add(new Coroutine(Slide(opening: true)));
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
    closed(current.Trial);
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
      shown.Menu.Alpha = shown.Alpha;
      shown.Menu.Focused = !sliding && shown == current;
    }
    if (shown.Leaving != null)
    {
      shown.Leaving.Position = at;
      shown.Leaving.Alpha = shown.Alpha;
      shown.Leaving.Focused = false;
    }
  }

  private void ShowMenu(Shown shown, int select, int turned = 0)
  {
    int index = select == KeepSelection ? shown.Menu?.Selection ?? HeaderRow : select;
    BoardPage shownPage = shown.Chunk == null ? null : BoardPage.From(shown.Chunk, page);
    BoardMenu previous = shown.Menu;
    shown.Menu = new BoardMenu(shown.Trial, shownPage, Switchable)
    {
      OnPage = TurnPage,
      OnSwitch = Switch,
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
    Standings.FetchBoard(
      target.Trial,
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

    Standings.FetchBoard(current.Trial, next, _ => { });
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
    int count = campaign.Trials.Count;
    trialIndex = (trialIndex + direction + count) % count;
    Audio.Play(
      direction > 0 ? "event:/ui/main/button_toggle_on" : "event:/ui/main/button_toggle_off"
    );
    outgoing = current;
    current = new Shown(campaign.Trials[trialIndex]) { Alpha = 0f };
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
    Place();
    if (sliding)
      return;

    if (Input.MenuCancel.Pressed || Input.ESC.Pressed || Input.MenuJournal.Pressed)
    {
      Close();
      return;
    }

    if (current.State == BoardState.Offline && Input.MenuConfirm.Pressed)
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
    if (!sliding && outgoing == null && current.State == BoardState.Offline)
      ButtonRow.DrawRight(1f, [(Dialog.Clean("climbtrials_board_retry"), Input.MenuConfirm)]);
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
    Color rule = Color.White * (BoardLayout.RuleAlpha * shown.Alpha);
    Draw.Rect(left, ruleY, shown.Menu.Width, 1f, rule);
    Vector2 rowLeft = new(left, ruleY + BoardLayout.RuleGap + BoardLayout.RowHeight / 2f);
    BoardMenu.DrawRow(shownPage.Me, shown.Trial, rowLeft, shown.Menu.Width, shown.Alpha, false);
  }

  private void DrawMessage(string key, Shown shown)
  {
    Color color = Color.White * shown.Alpha;
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
