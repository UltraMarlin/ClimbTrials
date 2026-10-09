using System;
using System.Collections.Generic;
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

  private const float MarkerScale = 0.5f;
  private const float MarkerGap = 24f;

  public Action<int> OnPage;
  public Action<int> OnSwitch;
  public Action<BoardRow> OnEntry;
  public bool HeaderVisible = true;
  public float EntryOffsetX;
  public float EntryAlpha = 1f;

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
    foreach (BoardRow row in page.Rows)
    {
      EntryRow entry = new(row, subject, pinned: false);
      entries.Add(entry);
      Add(entry);
    }
    if (page.Me == null || page.Rows.Count == 0)
      return;
    int emptyRows = BoardLayout.PageRows - page.Rows.Count;
    Add(new Gap(emptyRows * BoardLayout.RowHeight + 2f * BoardLayout.RuleGap));
    Add(own = new EntryRow(page.Me, subject, pinned: true));
  }

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
    if (!keys.Raw)
    {
      Focused = focus;
      return;
    }
    Focused = false;
    RenderAsFocused = focus;
    driven = focus;
  }

  public void Discard()
  {
    discarded = true;
    Focused = false;
    driven = false;
    Visible = false;
    RemoveSelf();
  }

  public override void Added(Scene scene)
  {
    base.Added(scene);
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
    if (keys.Down)
      MoveSelection(1, wiggle: true);
    else if (keys.Up)
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

  public override void Render()
  {
    base.Render();
    if (!HasEntries || !HeaderVisible)
      return;

    float top = Position.Y + BoardLayout.HeaderHeight + ItemSpacing;
    float centerY = top + EntriesSpan / 2f;
    float half = Math.Max(BoardLayout.PageArrowX, Width / 2f + BoardLayout.PageArrowGap);
    MTexture arrow = GFX.Gui["dotarrow_outline"];
    Color active = (HasFocus && Current is EntryRow ? TrialsMenu.Highlight : Color.White) * Alpha;
    Color disabled = Color.DarkSlateGray * Alpha;
    Color right = HasPage(1) ? active : disabled;
    Color left = HasPage(-1) ? active : disabled;
    Vector2 scale = Vector2.One * BoardLayout.PageArrowScale;
    arrow.DrawCentered(new Vector2(Position.X + half, centerY), right, scale);
    arrow.DrawCentered(new Vector2(Position.X - half, centerY), left, scale * new Vector2(-1f, 1f));
  }

  public static void DrawRow(
    BoardRow row,
    BoardSubject subject,
    Vector2 left,
    float width,
    float alpha,
    bool highlighted
  )
  {
    Color color = (highlighted ? TrialsMenu.Highlight : Color.White) * alpha;
    Vector2 justify = new(0f, 0.5f);
    Vector2 scale = Vector2.One * BoardLayout.TextScale;
    Color stroke = TextOutline.StrokeFor(color);
    Vector2 rankAt = left + new Vector2(BoardLayout.RankX, 0f);
    ActiveFont.DrawOutline(RankText.Format(row.Rank), rankAt, justify, scale, color, 2f, stroke);
    Vector2 nameAt = left + new Vector2(BoardLayout.NameX, 0f);
    ActiveFont.DrawOutline(row.Name, nameAt, justify, scale, color, 2f, stroke);
    DrawMarker(row, left, alpha);

    string time = TimeText.Format(row.Ticks);
    Medal medal = subject.ShownMedal(row);
    float timeWidth = TimeText.Width(time, Style);
    Vector2 baseline = new(left.X + width - timeWidth, left.Y + TimeText.Height(Style) / 2f);
    TimeText.Draw(baseline, time, Style, MedalIcon.TextColor(medal) * alpha);
  }

  private static void DrawMarker(BoardRow row, Vector2 left, float alpha)
  {
    if (!RunManager.Replaying)
      return;
    string marker;
    Color color;
    if (GhostComparisons.IsWatched(row))
    {
      marker = Dialog.Clean("climbtrials_marker_watching");
      color = Color.White;
    }
    else if (GhostComparisons.Find(row) is ComparedGhost ghost)
    {
      marker = Dialog.Clean("climbtrials_marker_ghost");
      color = GhostComparisons.NameColor(ghost.Color);
    }
    else
      return;
    Color shown = color * alpha;
    ActiveFont.DrawOutline(
      marker,
      left + new Vector2(BoardLayout.RankX - MarkerGap, 0f),
      new Vector2(1f, 0.5f),
      Vector2.One * MarkerScale,
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

    public HeaderRow(BoardSubject subject, BoardPage page, bool switchable)
    {
      this.subject = subject;
      this.page = page;
      this.switchable = switchable;
      Selectable = switchable;
    }

    public override float Height() => BoardLayout.HeaderHeight;

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
      string runners = Dialog.Get(subject.CountKey).Replace("{0}", page.Total.ToString());
      Vector2 runnersAt = new(centerX, top + BoardLayout.RunnersY);
      Vector2 runnersScale = Vector2.One * BoardLayout.RunnersScale;
      ActiveFont.Draw(
        runners,
        runnersAt,
        new Vector2(0.5f, 0.5f),
        runnersScale,
        Colors.DimText * alpha
      );
    }
  }

  private class EntryRow : Item
  {
    public readonly BoardRow Row;
    private readonly BoardSubject subject;
    private readonly bool pinned;
    private readonly string time;

    public EntryRow(BoardRow row, BoardSubject subject, bool pinned)
    {
      Row = row;
      this.subject = subject;
      this.pinned = pinned;
      time = TimeText.Format(row.Ticks);
      Selectable = true;

      if (subject.Trial != null)
        Pressed(() => (Container as BoardMenu)?.OnEntry?.Invoke(row));
    }

    public override float Height() => BoardLayout.RowHeight;

    public override float LeftWidth() =>
      BoardLayout.NameX + ActiveFont.Measure(Row.Name).X * BoardLayout.TextScale;

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

        float ruleY = position.Y - Height() / 2f - BoardLayout.RuleGap;
        Color rule = Color.White * (BoardLayout.RuleAlpha * Container.Alpha);
        Draw.Rect(position.X, ruleY, Container.Width, 1f, rule);
        DrawRow(Row, subject, position, Container.Width, Container.Alpha, highlighted);
        return;
      }
      Vector2 left = position + new Vector2(menu.EntryOffsetX, 0f);
      DrawRow(Row, subject, left, Container.Width, Container.Alpha * menu.EntryAlpha, highlighted);
    }
  }
}
