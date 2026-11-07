using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.Core;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class BoardMenu : TextMenu
{
  public const int HeaderSlot = -1;
  public const int OwnSlot = -2;

  private const float WatchStatusScale = 0.5f;
  private const float WatchStatusGap = 24f;
  private const float TeamTagScaleOfName = 0.8f;
  private const float TeamTagGap = 16f;
  private const float TeamTagScale = BoardLayout.TextScale * TeamTagScaleOfName;

  public Action<int> OnPage;
  public Action<int> OnSwitch;
  public Action<BoardRow> OnEntry;
  public bool HeaderVisible = true;
  public float EntryOffsetX;
  public float EntryAlpha = 1f;
  public int LoadingDirection;
  public float RowsAlpha = 1f;
  public BoardMenu MenuToDiscard;
  public BoardMenu MenuLosingHeader;

  private static TimeStyle Style => TimeText.Hud.At(BoardLayout.TextScale);
  private readonly BoardPage page;
  private readonly MenuKeys keys;
  private readonly HeaderRow header;
  private readonly List<EntryRow> entries = [];
  private readonly EntryRow own;
  private bool driven;
  private bool discarded;

  public BoardMenu(BoardSubject subject, BoardPage page, bool switchable, MenuKeys keys)
  {
    this.page = page;
    this.keys = keys;
    AutoScroll = false;
    InnerContent = InnerContentMode.TwoColumn;
    Justify = new Vector2(0.5f, 0f);
    HighlightColor = TrialsMenu.Highlight;
    MinWidth = BoardLayout.MinWidth;
    ItemSpacing = BoardLayout.ItemSpacing;

    Add(header = new HeaderRow(subject, page, switchable));
    if (page == null)
      return;
    float tagColumnWidth = MeasureTagColumnWidth(page);
    foreach (BoardRow row in page.Rows)
    {
      EntryRow entry = new(row, subject, page.BoardMode, tagColumnWidth, pinned: false);
      entries.Add(entry);
      Add(entry);
    }
    if (page.BoardMode == BoardMode.OwnTimeOnly && page.Rows.Count == 0 && page.Me != null)
    {
      EntryRow alone = new(page.Me, subject, page.BoardMode, tagColumnWidth, pinned: false);
      entries.Add(alone);
      Add(alone);
      return;
    }
    if (page.Me == null || page.Rows.Count == 0)
      return;
    int emptyRows = BoardLayout.PageRows - page.Rows.Count;
    Add(new Gap(emptyRows * BoardLayout.RowHeight + 2f * BoardLayout.RuleGap));
    Add(own = new EntryRow(page.Me, subject, page.BoardMode, tagColumnWidth, pinned: true));
  }

  private static float MeasureTagColumnWidth(BoardPage page)
  {
    List<BoardRow> rows = [.. page.Rows];
    if (page.Me != null)
      rows.Add(page.Me);
    float widestTag = rows.Where(row => row.ShowsTeamTag)
      .Select(row => ActiveFont.Measure(row.TeamTag).X * TeamTagScale)
      .DefaultIfEmpty(0f)
      .Max();
    return widestTag > 0f ? widestTag + TeamTagGap : 0f;
  }

  public float TopLift => header.HasSecondLine ? BoardLayout.SecondLineGrowth : 0f;

  public BoardPage Page => page;

  public bool HasEntries => entries.Count > 0;

  public bool HasFocus => keys.Raw ? driven : Focused;

  private bool HasHoverable => Items.Exists(item => item.Hoverable);

  public static float EntriesSpan => BoardLayout.PageRows * BoardLayout.RowHeight;

  public BoardRow HoveredRow => HasFocus && Current is EntryRow entry ? entry.Row : null;

  public int Slot =>
    Current is EntryRow entry ? (entry == own ? OwnSlot : entries.IndexOf(entry)) : HeaderSlot;

  public bool HasPage(int direction) =>
    page != null && (direction > 0 ? page.Number < page.Count : page.Number > 1);

  public void SelectSlot(int slot)
  {
    Item target = slot switch
    {
      OwnSlot => (Item)own ?? LastEntry,
      >= 0 when entries.Count > 0 => entries[Math.Min(slot, entries.Count - 1)],
      _ => null,
    };
    if (target == null || slot == HeaderSlot)
      target = header.Selectable ? header : FirstEntry ?? own;
    Selection = target == null ? 0 : Items.IndexOf(target);
  }

  private Item FirstEntry => entries.Count > 0 ? entries[0] : null;

  private Item LastEntry => entries.Count > 0 ? entries[^1] : null;

  public void SetFocus(bool focus)
  {
    focus &= HasHoverable;
    RenderAsFocused = HasHoverable;
    if (!keys.Raw)
    {
      Focused = focus;
      return;
    }
    Focused = false;
    driven = focus;
  }

  public void StopInput()
  {
    Focused = false;
    driven = false;
  }

  public void Discard()
  {
    discarded = true;
    StopInput();
    Visible = false;
    RemoveSelf();
    MenuToDiscard?.Discard();
    MenuToDiscard = null;
  }

  public override void Added(Scene scene)
  {
    base.Added(scene);
    MenuToDiscard?.Discard();
    MenuToDiscard = null;
    if (MenuLosingHeader != null)
      MenuLosingHeader.HeaderVisible = false;
    MenuLosingHeader = null;
    if (discarded)
      RemoveSelf();
  }

  public override void Update()
  {
    HighlightColor = TrialsMenu.Highlight;
    if (HasFocus)
    {
      if (keys.PageDown)
      {
        CoreModule.Settings.MenuPageDown.ConsumePress();
        if (HasPage(1))
        {
          OnPage?.Invoke(1);
          return;
        }
      }
      else if (keys.PageUp)
      {
        CoreModule.Settings.MenuPageUp.ConsumePress();
        if (HasPage(-1))
        {
          OnPage?.Invoke(-1);
          return;
        }
      }
      if (keys.Raw)
        Drive();
    }
    base.Update();
  }

  private void Drive()
  {
    if (keys.StepDown(this))
      MoveSelection(1, wiggle: true);
    else if (keys.StepUp(this))
      MoveSelection(-1, wiggle: true);
    if (Current == null)
      return;
    if (keys.Left)
      Current.LeftPressed();
    if (keys.Right)
      Current.RightPressed();
    if (keys.Confirm)
    {
      Current.ConfirmPressed();
      Current.OnPressed?.Invoke();
    }
  }

  private Color ArrowColor(int direction)
  {
    if (!HasPage(direction))
      return Color.DarkSlateGray * Alpha;
    bool highlighted = RenderAsFocused && Current is EntryRow && LoadingDirection != direction;
    return (highlighted ? TrialsMenu.Highlight : Color.White) * Alpha;
  }

  public override void Render()
  {
    base.Render();
    if (!HasEntries || !HeaderVisible)
      return;

    float top = Position.Y + header.Height() + ItemSpacing;
    float centerY = top + EntriesSpan / 2f;
    float half = Math.Max(BoardLayout.PageArrowX, Width / 2f + BoardLayout.PageArrowGap);
    MTexture arrow = GFX.Gui["dotarrow_outline"];
    Color right = ArrowColor(1);
    Color left = ArrowColor(-1);
    Vector2 scale = Vector2.One * BoardLayout.PageArrowScale;
    arrow.DrawCentered(new Vector2(Position.X + half, centerY), right, scale);
    arrow.DrawCentered(new Vector2(Position.X - half, centerY), left, scale * new Vector2(-1f, 1f));
  }

  private static float NameColumnX(BoardMode mode) =>
    mode == BoardMode.OwnTimeOnly ? BoardLayout.RankX : BoardLayout.NameX;

  private static void DrawTeamTag(BoardRow row, Vector2 tagAt, float alpha)
  {
    Color color = row.TeamTagColor.Value * alpha;
    float baselineShift = TimeText.LineBelowBaseline * (BoardLayout.TextScale - TeamTagScale);
    ActiveFont.DrawOutline(
      row.TeamTag,
      tagAt + new Vector2(0f, baselineShift),
      new Vector2(0f, 0.5f),
      Vector2.One * TeamTagScale,
      color,
      2f,
      TextOutline.StrokeFor(color)
    );
  }

  public static void DrawWatchStatus(BoardRow row, Vector2 at, Vector2 justify, float alpha)
  {
    if (!RunManager.Replaying && !GhostComparisons.Planning)
      return;
    string watchStatus;
    Color color;
    if (GhostComparisons.IsWatched(row))
    {
      watchStatus = Dialog.Clean("climbtrials_marker_watching");
      color = Color.White;
    }
    else if (GhostComparisons.Find(row) is ComparedGhost ghost)
    {
      watchStatus = Dialog.Clean("climbtrials_marker_ghost");
      color = GhostComparisons.NameColor(ghost.Color);
    }
    else
      return;
    Color shown = color * alpha;
    ActiveFont.DrawOutline(
      watchStatus,
      at,
      justify,
      Vector2.One * WatchStatusScale,
      shown,
      TextOutline.DimStroke,
      TextOutline.StrokeFor(shown)
    );
  }

  private class HeaderRow : Item
  {
    private readonly BoardSubject subject;
    private readonly BoardPage page;
    private readonly bool switchable;

    private MTexture Skull => subject.Skull;

    private float SkullSpan => Skull == null ? 0f : Skull.Width + BoardLayout.IconGap;

    private readonly BoardMode mode;
    private readonly bool runnersShown;
    private readonly bool noteShown;

    public HeaderRow(BoardSubject subject, BoardPage page, bool switchable)
    {
      this.subject = subject;
      this.page = page;
      this.switchable = switchable;
      Selectable = switchable;
      mode = page?.BoardMode ?? subject.Event?.Info.BoardMode ?? BoardMode.FullTimes;
      runnersShown = mode != BoardMode.OwnTimeOnly;
      noteShown = NoteText() != null;
    }

    public bool HasSecondLine => runnersShown && noteShown;

    public override float Height() =>
      BoardLayout.HeaderHeight + (HasSecondLine ? BoardLayout.SecondLineGrowth : 0f);

    private string NoteText() =>
      subject.Event is EventCampaign campaign ? EventTiming.BoardNote(campaign, mode) : null;

    public override float LeftWidth() =>
      SkullSpan + ActiveFont.Measure(subject.Name).X * BoardLayout.NameScale;

    public override void LeftPressed() => (Container as BoardMenu)?.OnSwitch?.Invoke(-1);

    public override void RightPressed() => (Container as BoardMenu)?.OnSwitch?.Invoke(1);

    public override void Render(Vector2 position, bool highlighted)
    {
      if (!((BoardMenu)Container).HeaderVisible)
        return;
      float alpha = Container.Alpha;
      float centerX = position.X + Container.Width / 2f;
      float top = position.Y - Height() / 2f;
      float nameY = top + BoardLayout.NameY;
      float width = LeftWidth();
      float left = centerX - width / 2f;
      Skull?.DrawCentered(new Vector2(left + Skull.Width / 2f, nameY), Color.White * alpha);
      Color nameColor = (highlighted ? Container.HighlightColor : Color.White) * alpha;
      ActiveFont.DrawOutline(
        subject.Name,
        new Vector2(left + SkullSpan, nameY),
        new Vector2(0f, 0.5f),
        Vector2.One * BoardLayout.NameScale,
        nameColor,
        2f,
        TextOutline.StrokeFor(nameColor)
      );
      Color dim = RowLayout.Dim * alpha;
      if (switchable)
      {
        Color arrows = highlighted ? nameColor : dim;
        MTexture arrow = GFX.Gui["dotarrow_outline"];
        float half = width / 2f + BoardLayout.ArrowGap;
        arrow.DrawCentered(new Vector2(centerX + half, nameY), arrows);
        arrow.DrawCentered(new Vector2(centerX - half, nameY), arrows, new Vector2(-1f, 1f));
      }

      if (page == null)
        return;
      float lineY = top + BoardLayout.RunnersY;
      if (runnersShown && page.Total.HasValue)
      {
        DrawHeaderLine(
          Dialog.Get("climbtrials_board_runners").Replace("{0}", $"{page.Total}"),
          centerX,
          lineY,
          alpha
        );
        lineY += BoardLayout.SecondLineGap;
      }
      if (noteShown && NoteText() is string note)
        DrawHeaderLine(note, centerX, lineY, alpha);
    }

    private static void DrawHeaderLine(string text, float centerX, float centerY, float alpha) =>
      ActiveFont.Draw(
        text,
        new Vector2(centerX, centerY),
        new Vector2(0.5f, 0.5f),
        Vector2.One * BoardLayout.RunnersScale,
        Colors.DimText * alpha
      );
  }

  private class EntryRow : Item
  {
    public readonly BoardRow Row;
    private readonly BoardSubject subject;
    private readonly BoardMode mode;
    private readonly float tagColumnWidth;
    private readonly bool pinned;
    private readonly string time;

    public EntryRow(
      BoardRow row,
      BoardSubject subject,
      BoardMode mode,
      float tagColumnWidth,
      bool pinned
    )
    {
      Row = row;
      this.subject = subject;
      this.mode = mode;
      this.tagColumnWidth = tagColumnWidth;
      this.pinned = pinned;
      time = row.Ticks is long ticks ? TimeText.Format(ticks) : TimeText.TypicalTime;
      Selectable = true;
      bool interactive = row.Me || row.Ticks.HasValue || mode != BoardMode.RanksOnly;
      if (subject.Trial != null && interactive)
        Pressed(() => (Container as BoardMenu)?.OnEntry?.Invoke(row));
    }

    public override float Height() => BoardLayout.RowHeight;

    public override float LeftWidth() =>
      NameColumnX(mode) + tagColumnWidth + ActiveFont.Measure(Row.Name).X * BoardLayout.TextScale;

    public override float RightWidth() => TimeText.Width(time, Style);

    public override void LeftPressed() => (Container as BoardMenu)?.OnPage?.Invoke(-1);

    public override void RightPressed() => (Container as BoardMenu)?.OnPage?.Invoke(1);

    public override void Render(Vector2 position, bool highlighted)
    {
      BoardMenu menu = (BoardMenu)Container;
      if (pinned)
      {
        if (!menu.HeaderVisible)
          return;

        float bounce = SelectWiggler.Value * 8f;
        float ruleY = position.Y - bounce - Height() / 2f - BoardLayout.RuleGap;
        Color rule = Color.White * (BoardLayout.RuleAlpha * Container.Alpha);
        Draw.Rect(position.X, ruleY, Container.Width, 1f, rule);
        DrawRow(position, Container.Alpha, highlighted);
        return;
      }
      Vector2 left = position + new Vector2(menu.EntryOffsetX, 0f);
      DrawRow(left, Container.Alpha * menu.EntryAlpha * menu.RowsAlpha, highlighted);
    }

    private void DrawRow(Vector2 left, float alpha, bool highlighted)
    {
      Color color = (highlighted ? TrialsMenu.Highlight : Colors.DimText) * alpha;
      Vector2 justify = new(0f, 0.5f);
      Vector2 scale = Vector2.One * BoardLayout.TextScale;
      Color stroke = TextOutline.StrokeFor(color);
      if (Row.Rank is int rank)
      {
        Vector2 rankAt = left + new Vector2(BoardLayout.RankX, 0f);
        ActiveFont.DrawOutline(RankText.Format(rank), rankAt, justify, scale, color, 2f, stroke);
      }
      float nameX = NameColumnX(mode);
      if (Row.ShowsTeamTag)
        DrawTeamTag(Row, left + new Vector2(nameX, 0f), alpha);
      Vector2 nameAt = left + new Vector2(nameX + tagColumnWidth, 0f);
      ActiveFont.DrawOutline(Row.Name, nameAt, justify, scale, color, 2f, stroke);
      DrawWatchStatus(
        Row,
        left + new Vector2(BoardLayout.RankX - WatchStatusGap, 0f),
        new Vector2(1f, 0.5f),
        alpha
      );

      Vector2 baseline = new(
        left.X + Container.Width - TimeText.Width(time, Style),
        left.Y + TimeText.Height(Style) / 2f
      );
      if (!Row.Ticks.HasValue)
      {
        Color hidden = Colors.DimText * (BoardLayout.HiddenTimeAlpha * alpha);
        TimeText.DrawHidden(baseline, Style, hidden);
        return;
      }
      Medal medal = subject.ShownMedal(Row, mode);
      Color timeColor = medal == Medal.None ? Colors.DimText : MedalIcon.TextColor(medal);
      TimeText.Draw(baseline, time, Style, timeColor * alpha);
    }
  }
}
