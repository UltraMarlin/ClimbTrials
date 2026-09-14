using System;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.Core;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class BoardMenu : TextMenu
{
  public Action<int> OnPage;
  public Action<int> OnSwitch;
  public bool HeaderVisible = true;
  public float EntryOffsetX;
  public float EntryAlpha = 1f;

  private static TimeStyle Style => TimeText.Hud.At(BoardLayout.TextScale);
  private readonly BoardPage page;
  private bool discarded;

  public BoardMenu(Trial trial, BoardPage page, bool switchable)
  {
    this.page = page;
    AutoScroll = false;
    InnerContent = InnerContentMode.TwoColumn;
    Justify = new Vector2(0.5f, 0f);
    HighlightColor = TrialsMenu.Highlight;
    MinWidth = TrialsMenu.ListMinWidth;
    ItemSpacing = BoardLayout.ItemSpacing;

    Add(new HeaderRow(trial, page, switchable));
    if (page == null)
      return;
    foreach (BoardRow row in page.Rows)
      Add(new EntryRow(row, trial));
  }

  public BoardPage Page => page;

  public bool HasEntries => Items.Count > 1;

  public static float EntriesSpan => BoardLayout.PageRows * BoardLayout.RowHeight;

  public bool HasPage(int direction) =>
    page != null && (direction > 0 ? page.Number < page.Count : page.Number > 1);

  public void Select(int index) => Selection = Calc.Clamp(index, 0, Items.Count - 1);

  public void Discard()
  {
    discarded = true;
    Focused = false;
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
    if (Focused)
    {
      if (CoreModule.Settings.MenuPageDown.Pressed)
      {
        CoreModule.Settings.MenuPageDown.ConsumePress();
        if (HasPage(1))
        {
          OnPage?.Invoke(1);
          return;
        }
      }
      else if (CoreModule.Settings.MenuPageUp.Pressed)
      {
        CoreModule.Settings.MenuPageUp.ConsumePress();
        if (HasPage(-1))
        {
          OnPage?.Invoke(-1);
          return;
        }
      }
    }
    base.Update();
  }

  public override void Render()
  {
    base.Render();
    if (!HasEntries || !HeaderVisible)
      return;

    float top = Position.Y + BoardLayout.HeaderHeight + ItemSpacing;
    float centerY = top + EntriesSpan / 2f;
    float half = Width / 2f + BoardLayout.PageArrowGap;
    MTexture arrow = GFX.Gui["dotarrow_outline"];
    Color active = (Focused && Selection > 0 ? TrialsMenu.Highlight : Color.White) * Alpha;
    Color disabled = Color.DarkSlateGray * Alpha;
    Color right = HasPage(1) ? active : disabled;
    Color left = HasPage(-1) ? active : disabled;
    Vector2 scale = Vector2.One * BoardLayout.PageArrowScale;
    arrow.DrawCentered(new Vector2(Position.X + half, centerY), right, scale);
    arrow.DrawCentered(new Vector2(Position.X - half, centerY), left, scale * new Vector2(-1f, 1f));
  }

  public static void DrawRow(
    BoardRow row,
    Trial trial,
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
    ActiveFont.DrawOutline($"#{row.Rank}", rankAt, justify, scale, color, 2f, stroke);
    Vector2 nameAt = left + new Vector2(BoardLayout.NameX, 0f);
    ActiveFont.DrawOutline(row.Name, nameAt, justify, scale, color, 2f, stroke);

    string time = TimeText.Format(row.Ticks);
    long viewerBest = TrialRecords.Get(trial.Id)?.BestTicks ?? long.MaxValue;
    Medal medal = trial.Goals?.VisibleMedalFor(row.Ticks, viewerBest) ?? Medal.None;
    float timeWidth = TimeText.Width(time, Style);
    Vector2 baseline = new(left.X + width - timeWidth, left.Y + TimeText.Height(Style) / 2f);
    TimeText.Draw(baseline, time, Style, MedalIcon.TextColor(medal) * alpha);
  }

  private class HeaderRow : Item
  {
    private readonly Trial trial;
    private readonly BoardPage page;
    private readonly bool switchable;

    private MTexture Skull => GFX.Gui[trial.SkullTexture];

    public HeaderRow(Trial trial, BoardPage page, bool switchable)
    {
      this.trial = trial;
      this.page = page;
      this.switchable = switchable;
      Selectable = true;
    }

    public override float Height() => BoardLayout.HeaderHeight;

    public override float LeftWidth() =>
      Skull.Width + BoardLayout.IconGap + ActiveFont.Measure(trial.Name).X * BoardLayout.NameScale;

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
      Skull.DrawCentered(new Vector2(left + Skull.Width / 2f, nameY), Color.White * alpha);
      Color nameColor = (highlighted ? Container.HighlightColor : Color.White) * alpha;
      ActiveFont.DrawOutline(
        trial.Name,
        new Vector2(left + Skull.Width + BoardLayout.IconGap, nameY),
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
      string runners = Dialog
        .Get("climbtrials_board_runners")
        .Replace("{0}", page.Total.ToString());
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
    private readonly BoardRow row;
    private readonly Trial trial;
    private readonly string time;

    public EntryRow(BoardRow row, Trial trial)
    {
      this.row = row;
      this.trial = trial;
      time = TimeText.Format(row.Ticks);
      Selectable = true;
    }

    public override float Height() => BoardLayout.RowHeight;

    public override float LeftWidth() =>
      BoardLayout.NameX + ActiveFont.Measure(row.Name).X * BoardLayout.TextScale;

    public override float RightWidth() => TimeText.Width(time, Style);

    public override void LeftPressed() => (Container as BoardMenu)?.OnPage?.Invoke(-1);

    public override void RightPressed() => (Container as BoardMenu)?.OnPage?.Invoke(1);

    public override void Render(Vector2 position, bool highlighted)
    {
      BoardMenu menu = (BoardMenu)Container;
      Vector2 left = position + new Vector2(menu.EntryOffsetX, 0f);
      DrawRow(row, trial, left, Container.Width, Container.Alpha * menu.EntryAlpha, highlighted);
    }
  }
}
