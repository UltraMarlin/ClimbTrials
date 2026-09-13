using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class CampaignRow : TextMenu.Item
{
  public readonly Campaign Campaign;
  private float ease;

  public CampaignRow(Campaign campaign)
  {
    Campaign = campaign;
    Selectable = true;
  }

  private string CountText =>
    TrialText.Count(
      Campaign.Trials.Count,
      null,
      "climbtrials_campaign_trial",
      "climbtrials_campaign_trials"
    );

  public override float Height() => RowLayout.RowHeight;

  public override float LeftWidth() =>
    RowLayout.CampaignNameX
    + ActiveFont.Measure(Campaign.Name).X
    + RowLayout.CountGap
    + ActiveFont.Measure(CountText).X * RowLayout.CountScale;

  public override float RightWidth() => RowLayout.RightColumnWidth;

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
    float countX =
      RowLayout.CampaignNameX + ActiveFont.Measure(Campaign.Name).X + RowLayout.CountGap;
    ActiveFont.Draw(
      CountText,
      rowStart + new Vector2(countX, 0f),
      new Vector2(0f, 0.5f),
      Vector2.One * RowLayout.CountScale,
      RowLayout.Dim * alpha
    );

    CampaignRecord record = TrialRecords.Get(Campaign);
    if (record.TotalTicks is not long total)
      return;

    string text = TimeText.Format(total);
    float width = TimeText.Width(text, TimeText.Hud.At(RowLayout.PbScale));
    if (record.Medal != Medal.None)
      MedalIcon.Draw(
        new Vector2(rightEdge - width - RowLayout.MedalGap, rowStart.Y),
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
}
