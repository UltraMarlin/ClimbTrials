using Celeste.Mod.ClimbTrials.Content;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class UpcomingEventRow(UpcomingEvent upcoming) : SlidingRow
{
  public UpcomingEvent Entry { get; } = upcoming;

  public override float Height() => RowLayout.EventRowHeight;

  public override float LeftWidth() => RowLayout.CampaignNameX + ActiveFont.Measure(Entry.Name).X;

  public override float RightWidth() => RowLayout.RightColumnWidth;

  public override void Render(Vector2 position, bool highlighted)
  {
    float alpha = Container.Alpha;
    float top = position.Y - Height() / 2f;
    Vector2 nameAt =
      RowStart(new Vector2(position.X, top + RowLayout.RowHeight / 2f))
      + new Vector2(RowLayout.CampaignNameX, 0f);
    Color color = Color.DarkSlateGray * alpha;
    ActiveFont.DrawOutline(
      Entry.Name,
      nameAt,
      new Vector2(0f, 0.5f),
      Vector2.One,
      color,
      2f,
      TextOutline.StrokeFor(color)
    );
    EventRow.DrawTimingLine(EventTiming.FormatOpensIn(Entry.OpensAt), nameAt.X, top, alpha);
  }
}
