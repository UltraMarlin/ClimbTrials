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
  private const float ReachedAlpha = 0.25f;
  private static readonly TimeStyle TimeStyle = TimeText.Hud.At(RunHudLayout.TimeScale);

  private const float DuckX = 380f;
  private const float DuckFloorY = 760f;
  private const float DuckAlpha = 0.25f;
  private const float DuckTime = 0.2f;

  private const float ListBoxAlpha = 0.35f;
  private const float BoxPaddingTop = 14f;
  private const float BoxPaddingRight = 20f;
  private const float BoxPaddingBottom = 14f;

  private Trial cachedTrial;
  private long cachedBest;
  private List<Row> rows = [];
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
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate;
  }

  private static List<Row> Rows(Trial trial, long best)
  {
    GoalTimes goals = trial.Goals;
    List<Row> rows =
    [
      Goal(Medal.Bronze, goals.BronzeTicks, best),
      Goal(Medal.Silver, goals.SilverTicks, best),
      Goal(Medal.Gold, goals.GoldTicks, best),
    ];
    if (best != long.MaxValue)
    {
      if (best <= goals.GoldTicks)
        rows.Add(Goal(Medal.Grandmaster, goals.GrandmasterTicks, best));
      rows.Add(new Row(Medal.None, best, Attempt.Format(best), true, goals.MedalFor(best), false));
    }
    return rows.OrderBy(r => r.Ticks).ThenBy(r => r.IsPb).ToList();
  }

  private static Row Goal(Medal medal, long ticks, long best) =>
    new(medal, ticks, Attempt.Format(ticks), false, medal, best <= ticks);

  public override void Update()
  {
    base.Update();
    Level level = SceneAs<Level>();
    Player player = level.Tracker.GetEntity<Player>();
    bool inTheWay = false;
    if (player != null)
    {
      Vector2 screen = level.WorldToScreen(player.Center);
      inTheWay = screen.X < DuckX && screen.Y < DuckFloorY;
    }
    float target = inTheWay ? DuckAlpha : 1f;
    duck = Calc.Approach(duck, target, Engine.RawDeltaTime / DuckTime);
  }

  public override void Render()
  {
    Trial trial = RunManager.Trial;
    if (trial == null || RunManager.Run == null)
      return;
    if (RunManager.Replaying && !RunManager.Replay.IsOwn)
      return;
    if (trial.Goals == null || !RunTimerDisplay.ShouldShow)
      return;

    long best = TrialRecords.Get(trial.Id)?.BestTicks ?? long.MaxValue;
    if (trial != cachedTrial || best != cachedBest)
    {
      cachedTrial = trial;
      cachedBest = best;
      rows = Rows(trial, best);
      widest = WidestRow(rows);
    }
    float rowHeight = RunHudLayout.RowHeight;
    Draw.Rect(
      0f,
      RunHudLayout.ListY - BoxPaddingTop,
      widest + BoxPaddingRight,
      rows.Count * rowHeight + BoxPaddingTop + BoxPaddingBottom,
      Color.Black * (ListBoxAlpha * duck)
    );

    float y = RunHudLayout.ListY;
    foreach (Row row in rows)
    {
      Vector2 iconCenter = new(RunHudLayout.IconX, y + rowHeight / 2f);
      float alpha = (row.Reached ? ReachedAlpha : 1f) * duck;
      Color textColor = MedalIcon.TextColor(row.TextMedal) * alpha;
      if (row.IsPb)
        ActiveFont.DrawOutline(
          "PB",
          iconCenter,
          new Vector2(0.5f, 0.5f),
          Vector2.One * 0.5f,
          textColor,
          2f,
          TimeText.StrokeFor(textColor)
        );
      else
        MedalIcon.Draw(iconCenter, row.Medal, RunHudLayout.IconHeight, alpha);
      TimeText.Draw(new Vector2(RunHudLayout.TimeX, y + rowHeight), row.Text, TimeStyle, textColor);
      y += rowHeight;
    }
  }

  private static float WidestRow(List<Row> rows)
  {
    float widest = RunHudLayout.IconX + RunHudLayout.IconHeight / 2f;
    foreach (Row row in rows)
      widest = Math.Max(widest, RunHudLayout.TimeX + TimeText.Width(row.Text, TimeStyle));
    return widest;
  }
}
