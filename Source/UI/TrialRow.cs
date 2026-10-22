using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

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
    string best = record == null ? null : TimeText.Format(record.BestTicks);
    float width = TimeText.Width(best ?? TimeText.TypicalTime, TimeText.Hud.At(RowLayout.PbScale));
    float medalCenter = timeRight - width - RowLayout.MedalGap;
    if (record == null)
      DrawWord("new", timeRight, rowStart.Y, alpha);
    else
    {
      Medal medal = Trial.Goals?.MedalReached(record.BestTicks) ?? Medal.None;
      if (medal != Medal.None)
        MedalIcon.Draw(
          new Vector2(medalCenter, rowStart.Y),
          medal,
          RowLayout.MedalIconHeight,
          alpha
        );
      TimeText.Draw(
        new Vector2(timeRight - width, rowStart.Y + RowLayout.PbBaseline),
        best,
        TimeText.Hud.At(RowLayout.PbScale),
        MedalIcon.TextColor(medal) * alpha
      );
    }

    RankText.Draw(
      Standings.RecordFor(Trial)?.Rank,
      new Vector2(medalCenter, rowStart.Y + RowLayout.RankDrop),
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

  private static void DrawWord(string word, float rightEdge, float y, float alpha)
  {
    ActiveFont.Draw(
      word,
      new Vector2(rightEdge, y),
      new Vector2(1f, 0.5f),
      Vector2.One * RowLayout.WordScale,
      RowLayout.Dim * alpha
    );
  }
}
