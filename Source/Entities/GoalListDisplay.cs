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
  private const float ReachedAlpha = 0.6f;
  private static readonly TimeStyle TimeStyle = TimeText.Hud.At(TimeScale);
  private const float IconHeight = 48f;
  private const string CheckpointIcon = "ClimbTrials/Checkpoint_Icon";
  private const string StrawberryIcon = "collectables/strawberry";

  private readonly record struct Row(
    Medal Medal,
    long Ticks,
    bool IsPb,
    Medal TextMedal,
    bool Reached
  );

  public GoalListDisplay()
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate;
  }

  private static List<Row> Rows(Trial trial)
  {
    GoalTimes goals = trial.Goals;
    TrialRecord record = TrialRecords.Get(trial.Id);
    long best = record?.BestTicks ?? long.MaxValue;
    List<Row> rows =
    [
      Goal(Medal.Bronze, goals.BronzeTicks, best),
      Goal(Medal.Silver, goals.SilverTicks, best),
      Goal(Medal.Gold, goals.GoldTicks, best),
    ];
    if (record != null)
    {
      if (best <= goals.GoldTicks)
        rows.Add(Goal(Medal.Grandmaster, goals.GrandmasterTicks, best));
      rows.Add(new Row(Medal.None, best, true, goals.MedalFor(best), false));
    }
    return rows.OrderBy(r => r.Ticks).ThenBy(r => r.IsPb).ToList();
  }

  private static Row Goal(Medal medal, long ticks, long best) =>
    new(medal, ticks, false, medal, best <= ticks);

  public override void Render()
  {
    Trial trial = RunManager.Trial;
    if (trial == null || RunManager.Attempt.State == AttemptState.Idle)
      return;
    if (RunManager.Replaying && !RunManager.Replay.IsOwn)
      return;
    RunDefinition run = trial.Run;
    RunProgress progress = RunManager.Progress;
    float y = ListY;

    if (run.HasModifiers)
    {
      DrawText(TrialText.Modifiers(run), y);
      y += RowHeight;
    }
    if (run.Checkpoints.Count > 0)
    {
      DrawIcon(GFX.Gui[CheckpointIcon], y);
      DrawText($"{progress.CheckpointsTaken}/{run.Checkpoints.Count}", y);
      y += RowHeight;
    }
    if (run.RequiredStrawberries > 0)
    {
      DrawIcon(GFX.Gui[StrawberryIcon], y);
      DrawText($"{progress.Strawberries}/{run.RequiredStrawberries}", y);
      y += RowHeight;
    }
    if (trial.Goals == null || !RunTimerDisplay.ShouldShow)
      return;

    foreach (Row row in Rows(trial))
    {
      Vector2 iconCenter = new(IconX, y + RowHeight / 2f);
      float alpha = row.Reached ? ReachedAlpha : 1f;
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
        MedalIcon.Draw(iconCenter, row.Medal, IconHeight, alpha);
      TimeText.Draw(
        new Vector2(TimeX, y + RowHeight),
        Attempt.Format(row.Ticks),
        TimeStyle,
        MedalIcon.TextColor(row.TextMedal) * alpha,
        MedalIcon.SeparatorColor(row.TextMedal) * alpha
      );
      y += RowHeight;
    }
  }

  private static void DrawIcon(MTexture texture, float y)
  {
    float scale = IconHeight / texture.Height;
    texture.DrawCentered(new Vector2(IconX, y + RowHeight / 2f), Color.White, scale);
  }

  private static void DrawText(string text, float y)
  {
    ActiveFont.DrawOutline(
      text,
      new Vector2(TimeX, y + RowHeight / 2f),
      new Vector2(0f, 0.5f),
      Vector2.One * TimeScale,
      Color.White,
      2f,
      Color.Black
    );
  }
}
