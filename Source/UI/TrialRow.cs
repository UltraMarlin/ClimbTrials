using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.UI;

public class TrialRow : SlidingRow
{
  public readonly Trial Trial;

  public TrialRow(Trial trial)
  {
    Trial = trial;
  }

  public override float LeftWidth() => RowLayout.NameX + ActiveFont.Measure(Trial.Name).X;

  public override float RightWidth() => RowLayout.RightColumnWidth;

  public override void LeftPressed() => (Container as TrialsMenu)?.OnSwitchLeft?.Invoke();

  public override void RightPressed() => (Container as TrialsMenu)?.OnSwitchRight?.Invoke();

  public override void Render(Vector2 position, bool highlighted)
  {
    float alpha = Container.Alpha;
    Vector2 rowStart = RowStart(position);
    float rightEdge = rowStart.X + Container.Width - RowLayout.HoveredRowShift;

    GFX.Gui[Trial.SkullTexture]
      .DrawCentered(rowStart + new Vector2(RowLayout.IconX, 0f), Color.White * alpha, Vector2.One);
    Color nameColor = highlighted ? Container.HighlightColor : Color.White;
    ActiveFont.DrawOutline(
      Trial.Name,
      rowStart + new Vector2(RowLayout.NameX, 0f),
      new Vector2(0f, 0.5f),
      Vector2.One,
      nameColor * alpha,
      2f,
      TextOutline.StrokeFor(nameColor * alpha)
    );

    TrialRecord record = TrialRecords.Get(Trial.Id);
    float timeRight = rightEdge - RowLayout.MarkGap - RowLayout.MarkWidth;
    if (record == null)
      DrawWord("new", timeRight, rowStart.Y, alpha);
    DrawRightColumn(
      record == null ? null : TimeText.Format(record.BestTicks),
      record == null ? Medal.None : Trial.Goals.MedalReached(record.BestTicks),
      Standings.RecordFor(Trial)?.Rank,
      timeRight,
      rowStart.Y,
      alpha
    );

    if (record == null)
      return;

    bool needsReplay = Trial.FitsReplayLimit(record.BestTicks);
    if (record.Submittable && (!needsReplay || ReplayStore.Exists(Trial.Id)))
      return;

    if (Events.RefusalFor(Trial) != SubmitRefusal.None)
      return;

    Color mark = Colors.Warning * alpha;
    ActiveFont.DrawOutline(
      RowLayout.Mark,
      new Vector2(rightEdge, rowStart.Y + RowLayout.PbBaseline),
      new Vector2(1f, 1f),
      Vector2.One * RowLayout.PbScale,
      mark,
      2f,
      TextOutline.StrokeFor(mark)
    );
  }

  private static void DrawWord(string word, float rightEdge, float centerY, float alpha)
  {
    ActiveFont.Draw(
      word,
      new Vector2(rightEdge, centerY),
      new Vector2(1f, 0.5f),
      Vector2.One * RowLayout.WordScale,
      RowLayout.Dim * alpha
    );
  }
}
