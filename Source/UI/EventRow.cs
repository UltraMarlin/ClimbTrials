using Celeste.Mod.ClimbTrials.Content;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class EventRow(Campaign campaign) : CampaignRow(campaign)
{
  public override bool ShowsDescription => false;
  public override bool ShowsCount => false;

  public override float Height() => RowLayout.EventRowHeight;

  public override void Render(Vector2 position, bool highlighted)
  {
    float top = position.Y - Height() / 2f;
    base.Render(new Vector2(position.X, top + RowLayout.RowHeight / 2f), highlighted);
    if (Events.Of(Campaign) is not EventCampaign eventCampaign)
      return;
    float nameX = RowStart(position).X + RowLayout.CampaignNameX;
    DrawTimingLine(EventTiming.PhaseText(eventCampaign), nameX, top, Container.Alpha);
  }

  public static void DrawTimingLine(string text, float nameX, float top, float alpha)
  {
    TextOutline.DrawDim(
      text,
      new Vector2(nameX, top + RowLayout.TimingLineY),
      new Vector2(0f, 0.5f),
      RowLayout.TimingScale,
      RowLayout.Dim * alpha
    );
  }
}
