using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class CampaignRow : SlidingRow
{
  public readonly Campaign Campaign;

  public CampaignRow(Campaign campaign)
  {
    Campaign = campaign;
  }

  public virtual bool ShowsDescription => true;

  public virtual bool ShowsCount => true;

  private string CountText =>
    TrialText.Pluralize(
      Campaign.Trials.Count,
      null,
      "climbtrials_campaign_trial",
      "climbtrials_campaign_trials"
    );

  public override float LeftWidth() =>
    RowLayout.CampaignNameX
    + ActiveFont.Measure(Campaign.Name).X
    + (
      ShowsCount ? RowLayout.CountGap + ActiveFont.Measure(CountText).X * RowLayout.CountScale : 0f
    );

  public override float RightWidth() => RowLayout.RightColumnWidth;

  public override void Render(Vector2 position, bool highlighted)
  {
    float alpha = Container.Alpha;
    Vector2 rowStart = RowStart(position);
    float rightEdge = rowStart.X + Container.Width - RowLayout.HoveredRowShift;

    Color nameColor = highlighted ? Container.HighlightColor : Color.White;
    ActiveFont.DrawOutline(
      Campaign.Name,
      rowStart + new Vector2(RowLayout.CampaignNameX, 0f),
      new Vector2(0f, 0.5f),
      Vector2.One,
      nameColor * alpha,
      2f,
      TextOutline.StrokeFor(nameColor * alpha)
    );
    if (ShowsCount)
    {
      float countX =
        RowLayout.CampaignNameX + ActiveFont.Measure(Campaign.Name).X + RowLayout.CountGap;
      ActiveFont.Draw(
        CountText,
        rowStart + new Vector2(countX, 0f),
        new Vector2(0f, 0.5f),
        Vector2.One * RowLayout.CountScale,
        RowLayout.Dim * alpha
      );
    }

    CampaignRecord record = TrialRecords.Totals(Campaign);
    string text = record.TotalTicks is long total ? TimeText.Format(total) : null;
    float width = TimeText.Width(
      text ?? RowLayout.RankSlotTime,
      TimeText.Hud.At(RowLayout.PbScale)
    );
    float medalCenter = rightEdge - width - RowLayout.MedalGap;
    if (text != null)
    {
      if (record.Medal != Medal.None)
        MedalIcon.Draw(
          new Vector2(medalCenter, rowStart.Y),
          record.Medal,
          RowLayout.MedalIconHeight,
          alpha
        );

      TimeText.Draw(
        new Vector2(rightEdge - width, rowStart.Y + RowLayout.PbBaseline),
        text,
        TimeText.Hud.At(RowLayout.PbScale),
        MedalIcon.TextColor(record.Medal) * alpha
      );
    }

    RankText.Draw(
      Standings.CampaignStandingFor(Campaign)?.Rank,
      new Vector2(medalCenter, rowStart.Y + RowLayout.RankLift),
      alpha
    );
  }
}
