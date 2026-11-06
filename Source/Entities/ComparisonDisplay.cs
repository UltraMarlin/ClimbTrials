using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class ComparisonDisplay : Entity
{
  private const int MaxRows = 10;
  private const float RowHeight = 30f;
  private const float TextBottom = 29f;
  private const float TextLeft = 16f;
  private const float ColumnGap = 18f;
  private const float DeltaGap = 14f;
  private const float RightPad = 24f;
  private const float TextScale = 0.45f;
  private const float NameMinFit = 0.65f;
  private const float BehindAlpha = 0.45f;
  private const int BufferWidth = 720;
  private const int BufferHeight = 320;

  private static readonly TimeStyle Style = TimeText.Hud.At(TextScale) with
  {
    Stroke = TextOutline.DimStroke,
  };

  private readonly FadeBuffer layer;
  private float duck = 1f;
  private float width;
  private float height;

  private readonly record struct Line(
    string Name,
    Color NameColor,
    int Split,
    long Ticks,
    long Delta,
    int Rank,
    bool Watched
  );

  public ComparisonDisplay()
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate | Tags.PauseUpdate;
    Add(layer = new FadeBuffer("climbtrials-comparisons", BufferWidth, BufferHeight, DrawRows));
    layer.Origin = new Vector2(0f, RunHudLayout.GhostBlockTop);
    layer.Visible = false;
  }

  public override void Update()
  {
    base.Update();
    Level level = SceneAs<Level>();
    Player player = level.Tracker.GetEntity<Player>();
    float target = 1f;
    if (player != null)
    {
      Vector2 screen = level.WorldToScreen(player.Center);
      target = RunHudLayout.DuckTarget(screen, width, RunHudLayout.GhostBlockTop + height);
    }
    duck = Calc.Approach(duck, target, Engine.RawDeltaTime / RunHudLayout.DuckTime);
  }

  public override void Render()
  {
    layer.Visible = GhostComparisons.Showing;
    if (!layer.Visible)
      return;
    layer.Alpha = duck;
    base.Render();
  }

  private void DrawRows()
  {
    if (!GhostComparisons.Showing)
      return;
    List<Line> lines = Lines();
    if (lines.Count == 0)
      return;
    int watchedSplit = lines.Find(line => line.Watched).Split;
    float nameWidth = 0f;
    float timeWidth = 0f;
    float deltaWidth = 0f;
    foreach (Line line in lines)
    {
      nameWidth = Math.Max(nameWidth, ActiveFont.Measure(line.Name).X * NameScale(line.Name));
      timeWidth = Math.Max(timeWidth, TimeText.Width(TimeText.Format(line.Ticks), Style));
      deltaWidth = Math.Max(deltaWidth, TimeText.Width(TimeText.FormatDelta(line.Delta), Style));
    }
    float timeLeft = TextLeft + nameWidth + ColumnGap;
    float deltaRight = timeLeft + timeWidth + DeltaGap + deltaWidth;
    width = deltaRight + RightPad;
    height = lines.Count * (RowHeight - RunHudLayout.BackgroundOverlap);

    float top = RunHudLayout.GhostBlockTop;
    foreach (Line line in lines)
    {
      TimerBackground.Draw(top, RowHeight, width);
      float alpha = !line.Watched && line.Split < watchedSplit ? BehindAlpha : 1f;
      float bottom = top + TextBottom;
      float scale = NameScale(line.Name);
      Color nameColor = line.NameColor * alpha;
      ActiveFont.DrawOutline(
        line.Name,
        new Vector2(TextLeft, bottom - TimeText.LineBelowBaseline * (TextScale - scale)),
        new Vector2(0f, 1f),
        Vector2.One * scale,
        nameColor,
        TextOutline.DimStroke,
        TextOutline.StrokeFor(nameColor)
      );
      Color timeColor = (line.Watched ? Color.White : Colors.DimText) * alpha;
      TimeText.Draw(new Vector2(timeLeft, bottom), TimeText.Format(line.Ticks), Style, timeColor);
      if (!line.Watched)
      {
        string delta = TimeText.FormatDelta(line.Delta);
        Color deltaColor =
          line.Delta < 0 ? Colors.Faster
          : line.Delta > 0 ? Colors.Slower
          : Color.White;
        float deltaLeft = deltaRight - TimeText.Width(delta, Style);
        TimeText.Draw(new Vector2(deltaLeft, bottom), delta, Style, deltaColor * alpha);
      }
      top += RowHeight - RunHudLayout.BackgroundOverlap;
    }
  }

  private static float NameScale(string name) =>
    TextScale * TextFit.FitName("{0}", name, NameMinFit);

  private List<Line> Lines()
  {
    GhostSplits watched = GhostComparisons.WatchedSplits();
    if (watched == null || Scene.Tracker.GetEntity<GhostLayer>() is not GhostLayer ghosts)
      return [];
    List<Line> lines = [WatchedLine(watched)];
    foreach (ComparedGhost ghost in GhostComparisons.Shown)
      lines.Add(GhostLine(ghost, watched, ghosts.Ticks));
    lines.Sort(
      (a, b) =>
        a.Split != b.Split ? b.Split.CompareTo(a.Split)
        : a.Ticks != b.Ticks ? a.Ticks.CompareTo(b.Ticks)
        : a.Rank.CompareTo(b.Rank)
    );
    return Nearest(lines);
  }

  private static Line WatchedLine(GhostSplits watched)
  {
    long now =
      RunManager.Attempt.State == AttemptState.Finished
        ? watched.FinalTicks
        : RunManager.Attempt.ElapsedTicks;
    (int split, long ticks) = Furthest(watched, watched, now);
    return new Line(
      Identity.NameOf(watched.Replay),
      Color.White,
      split,
      ticks,
      0,
      int.MinValue,
      true
    );
  }

  private static Line GhostLine(ComparedGhost ghost, GhostSplits watched, long now)
  {
    (int split, long ticks) = Furthest(ghost.Splits, watched, now);
    long watchedTicks =
      split < 0 ? 0
      : split == watched.ReachOrder.Count ? watched.FinalTicks
      : TicksAt(watched, watched.ReachOrder[split]);
    return new Line(
      Identity.NameOf(ghost.Replay),
      GhostComparisons.NameColor(ghost.Color),
      split,
      ticks,
      split < 0 ? 0 : ticks - watchedTicks,
      ghost.Rank,
      false
    );
  }

  private static (int Split, long Ticks) Furthest(GhostSplits run, GhostSplits watched, long now)
  {
    IReadOnlyList<SplitKey> order = watched.ReachOrder;
    if (now >= run.FinalTicks)
      return (order.Count, run.FinalTicks);
    for (int i = order.Count - 1; i >= 0; i--)
      if (run.TryGetTicks(order[i], out long ticks) && ticks <= now)
        return (i, ticks);
    return (-1, 0);
  }

  private static long TicksAt(GhostSplits splits, SplitKey key) =>
    splits.TryGetTicks(key, out long ticks) ? ticks : 0;

  private static List<Line> Nearest(List<Line> lines)
  {
    if (lines.Count <= MaxRows)
      return lines;
    int watched = lines.FindIndex(line => line.Watched);
    int from = Math.Clamp(watched - (MaxRows - 1) / 2, 0, lines.Count - MaxRows);
    return lines.GetRange(from, MaxRows);
  }
}
