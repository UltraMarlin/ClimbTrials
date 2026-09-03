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
  private const float ListY = 240f;
  private const float RowHeight = 46f;
  private const float IconX = 36f;
  private const float TimeX = 64f;
  private const float TimeScale = 0.7f;
  private const float IconHeight = 48f;

  private readonly record struct Row(Medal Medal, long Ticks, bool IsPb);

  public GoalListDisplay()
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate;
  }

  private static List<Row> Rows(Trial trial)
  {
    GoalTimes goals = trial.Goals;
    List<Row> rows =
    [
      new Row(Medal.Bronze, goals.BronzeTicks, false),
      new Row(Medal.Silver, goals.SilverTicks, false),
      new Row(Medal.Gold, goals.GoldTicks, false),
    ];
    TrialRecord record = TrialRecords.Get(trial.Id);
    if (record != null)
    {
      if (record.BestTicks <= goals.GoldTicks)
        rows.Add(new Row(Medal.Grandmaster, goals.GrandmasterTicks, false));
      rows.Add(new Row(Medal.None, record.BestTicks, true));
    }
    return rows.OrderBy(r => r.Ticks).ThenBy(r => r.IsPb).ToList();
  }

  public override void Render()
  {
    Trial trial = RunManager.Trial;
    if (trial?.Goals == null || !RunTimerDisplay.ShouldShow)
      return;
    if (RunManager.Replaying && !RunManager.Replay.IsOwn)
      return;
    float y = ListY;
    foreach (Row row in Rows(trial))
    {
      Vector2 iconCenter = new(IconX, y + RowHeight / 2f);
      if (row.IsPb)
        ActiveFont.DrawOutline(
          "PB",
          iconCenter,
          new Vector2(0.5f, 0.5f),
          Vector2.One * 0.5f,
          Color.White,
          2f,
          Color.Black
        );
      else
        MedalIcon.Draw(iconCenter, row.Medal, IconHeight);
      TimeText.Draw(
        new Vector2(TimeX, y + RowHeight),
        Attempt.Format(row.Ticks),
        TimeScale,
        Color.White,
        Color.LightGray
      );
      y += RowHeight;
    }
  }
}
