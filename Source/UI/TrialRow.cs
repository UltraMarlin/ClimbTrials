using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class TrialRow : TextMenu.Item
{
  public readonly Trial Trial;
  private float ease;

  public TrialRow(Trial trial)
  {
    Trial = trial;
    Selectable = true;
  }

  public override float Height() => RowLayout.RowHeight;

  public override float LeftWidth() => RowLayout.NameX + ActiveFont.Measure(Trial.Name).X;

  public override float RightWidth() => RowLayout.RightColumnWidth;

  public override void LeftPressed() => (Container as TrialsMenu)?.OnSwitchLeft?.Invoke();

  public override void RightPressed() => (Container as TrialsMenu)?.OnSwitchRight?.Invoke();

  public override void Update()
  {
    bool selected = Container.Focused && Container.Current == this;
    ease = Calc.Approach(ease, selected ? 1f : 0f, 6f * Engine.DeltaTime);
  }

  public override void Render(Vector2 position, bool highlighted)
  {
    float alpha = Container.Alpha;
    Vector2 rowStart = position + new Vector2(Ease.CubeInOut(ease) * RowLayout.SlideDistance, 0f);
    float rightEdge = rowStart.X + Container.Width - RowLayout.SlideDistance;

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
    {
      DrawWord("new", timeRight, rowStart.Y, alpha);
      return;
    }

    string best = TimeText.Format(record.BestTicks);
    float width = TimeText.Width(best, TimeText.Hud.At(RowLayout.PbScale));
    Medal medal = Trial.Goals?.MedalFor(record.BestTicks) ?? Medal.None;
    float medalCenter = timeRight - width - RowLayout.MedalGap;
    if (medal != Medal.None)
      MedalIcon.Draw(new Vector2(medalCenter, rowStart.Y), medal, RowLayout.MedalIconHeight, alpha);
    TimeText.Draw(
      new Vector2(timeRight - width, rowStart.Y + RowLayout.PbBaseline),
      best,
      TimeText.Hud.At(RowLayout.PbScale),
      MedalIcon.TextColor(medal) * alpha
    );
    DrawRank(new Vector2(medalCenter, rowStart.Y + 3f), alpha);

    if (record.Submittable && ReplayStore.Exists(Trial.Id))
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

  private void DrawRank(Vector2 position, float alpha)
  {
    if (Standings.For(Trial)?.Rank is not int rank)
      return;
    Color color = Color.White * alpha;
    ActiveFont.DrawOutline(
      "#" + rank,
      position,
      new Vector2(0.5f, 0.5f),
      Vector2.One * RowLayout.RankScale,
      color,
      2f,
      TextOutline.StrokeFor(color)
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
