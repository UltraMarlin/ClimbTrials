using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class GoalListDisplay : Entity
{
  private const float ReachedAlpha = 0.2f;
  private static readonly TimeStyle TimeStyle = TimeText.Hud.At(RunHudLayout.TimeScale);

  private readonly Color boxColor = Colors.ForChapter(RunManager.Run.Area).Dark;

  private Trial cachedTrial;
  private long cachedBest;
  private GoalTimesDisplayMode cachedMode;
  private float cachedShift;
  private List<Row> rows = [];
  private readonly List<FadeBuffer> layers = [];
  private float widest;

  private float duck = 1f;

  private readonly record struct Row(
    Medal Medal,
    long Ticks,
    string Text,
    bool IsPb,
    Medal TextMedal,
    bool Reached
  );

  public GoalListDisplay()
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate | Tags.PauseUpdate;
    Add(new BeforeRenderHook(PlaceLayers));
  }

  private static GoalTimesDisplayMode Mode => ClimbTrialsModule.Settings.HudSettings.GoalTimes;

  private static List<Row> Rows(Trial trial, long best, GoalTimesDisplayMode mode)
  {
    GoalTimes goals = trial.Goals;
    bool hasPb = best != long.MaxValue;
    if (mode == GoalTimesDisplayMode.Off)
      return [];
    if (mode == GoalTimesDisplayMode.PbOnly)
      return hasPb ? [Pb(best, Medal.None)] : [];
    List<Row> rows =
    [
      Goal(Medal.Bronze, goals.BronzeTicks, best),
      Goal(Medal.Silver, goals.SilverTicks, best),
      Goal(Medal.Gold, goals.GoldTicks, best),
    ];
    if (hasPb)
    {
      if (best <= goals.GoldTicks)
        rows.Add(Goal(Medal.Diamond, goals.DiamondTicks, best));
      if (best <= goals.GrandmasterTicks)
        rows.Add(Goal(Medal.Grandmaster, goals.GrandmasterTicks, best));
      rows.Add(Pb(best, goals.MedalReached(best)));
    }
    return rows.OrderBy(row => row.Ticks).ThenByDescending(row => row.IsPb).ToList();
  }

  private static Row Goal(Medal medal, long ticks, long best) =>
    new(medal, ticks, TimeText.Format(ticks), false, medal, best <= ticks);

  private static Row Pb(long best, Medal textMedal) =>
    new(Medal.None, best, TimeText.Format(best), true, textMedal, false);

  public override void Update()
  {
    base.Update();
    float boxRight = widest + RunHudLayout.BoxPaddingRight;
    float boxBottom = RunHudLayout.GoalListBottom(rows.Count);
    duck = RunHudLayout.ApproachDuck(duck, SceneAs<Level>(), boxRight, boxBottom);
    foreach (FadeBuffer layer in layers)
      layer.Visible = Shown;

    Trial trial = RunManager.Trial;
    if (trial == null)
      return;
    long best = TrialRecords.Get(trial.Id)?.BestTicks ?? long.MaxValue;
    GoalTimesDisplayMode mode = Mode;
    float shift = RunHudLayout.GhostBlockShift;
    if (trial == cachedTrial && best == cachedBest && mode == cachedMode && shift == cachedShift)
      return;
    cachedTrial = trial;
    cachedBest = best;
    cachedMode = mode;
    cachedShift = shift;
    rows = Rows(trial, best, mode);
    widest = WidestRow(rows);
    RebuildLayers();
  }

  private void RebuildLayers()
  {
    foreach (FadeBuffer layer in layers)
      Remove(layer);
    layers.Clear();
    int width = (int)Math.Ceiling(widest + RunHudLayout.BoxPaddingRight);
    for (int i = 0; i < rows.Count; i++)
    {
      int index = i;
      FadeBuffer layer = new(
        "climbtrials-goal-row",
        width,
        (int)RunHudLayout.RowHeight + 2 * RunHudLayout.RowPad,
        () => DrawRow(index)
      )
      {
        Origin = new Vector2(0f, RowTop(index)),
      };
      Add(layer);
      layers.Add(layer);
    }
  }

  private void PlaceLayers()
  {
    for (int i = 0; i < layers.Count; i++)
      layers[i].Origin = new Vector2(0f, RowTop(i));
  }

  private static float RowTop(int index) =>
    RunHudLayout.ListY + index * RunHudLayout.RowHeight - RunHudLayout.RowPad;

  private void DrawRow(int index)
  {
    Row row = rows[index];
    float rowY = RunHudLayout.ListY + index * RunHudLayout.RowHeight;
    Vector2 iconCenter = new(RunHudLayout.IconX, rowY + RunHudLayout.RowHeight / 2f);
    Color textColor = MedalIcon.TextColor(row.TextMedal);
    if (row.IsPb)
      TextOutline.Draw(
        Dialog.Clean("climbtrials_pb"),
        iconCenter,
        new Vector2(0.5f, 0.5f),
        0.5f,
        textColor
      );
    else
      MedalIcon.Draw(iconCenter, row.Medal, RunHudLayout.IconHeight);
    TimeText.Draw(
      new Vector2(RunHudLayout.TimeX, rowY + RunHudLayout.RowHeight),
      row.Text,
      TimeStyle,
      textColor
    );
  }

  public static int RowCount(Trial trial, long best) => Rows(trial, best, Mode).Count;

  public static bool Shown =>
    Mode != GoalTimesDisplayMode.Off
    && RunManager.Trial != null
    && RunManager.Run != null
    && !RunManager.Replaying
    && !RunHudLayout.PauseScreenOpen;

  public override void Render()
  {
    if (!Shown || rows.Count == 0)
      return;
    float rowHeight = RunHudLayout.RowHeight;
    Draw.Rect(
      0f,
      RunHudLayout.ListY - RunHudLayout.BoxPaddingTop,
      widest + RunHudLayout.BoxPaddingRight,
      rows.Count * rowHeight + RunHudLayout.BoxPaddingTop + RunHudLayout.BoxPaddingBottom,
      boxColor * (RunHudLayout.BoxAlpha * duck)
    );
    for (int i = 0; i < layers.Count; i++)
      layers[i].Alpha = (rows[i].Reached ? ReachedAlpha : 1f) * duck;
    base.Render();
  }

  private static float WidestRow(List<Row> rows)
  {
    float widest = RunHudLayout.IconX + RunHudLayout.IconHeight / 2f;
    foreach (Row row in rows)
      widest = Math.Max(widest, RunHudLayout.TimeX + TimeText.Width(row.Text, TimeStyle));
    return widest;
  }
}
