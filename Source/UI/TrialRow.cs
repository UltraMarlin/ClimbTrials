using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class TrialRow : TextMenu.Item
{
  private const float RowHeight = 90f;
  private const float IconX = 28f;
  private const float NameX = 86f;
  private const float PbScale = 0.85f;
  private const float PbBaseline = 30f;
  private const float MedalGap = 38f;
  private const float MedalIconHeight = 64f;
  private const float SlideDistance = 32f;
  private static readonly Color Dim = Color.White * 0.7f;

  public readonly Trial Trial;
  private float ease;

  public TrialRow(Trial trial)
  {
    Trial = trial;
    Selectable = true;
  }

  public override float Height() => RowHeight;

  public override float LeftWidth() => NameX + ActiveFont.Measure(Trial.Name).X;

  public override float RightWidth() =>
    SlideDistance
    + MedalIconHeight
    + MedalGap
    + TimeText.Width("0:00.000", TimeText.Hud.At(PbScale));

  public override void Update()
  {
    bool selected = Container.Focused && Container.Current == this;
    ease = Calc.Approach(ease, selected ? 1f : 0f, 6f * Engine.DeltaTime);
  }

  public override void Render(Vector2 position, bool highlighted)
  {
    float alpha = Container.Alpha;
    Vector2 slid = position + new Vector2(Ease.CubeInOut(ease) * SlideDistance, 0f);
    float rightEdge = slid.X + Container.Width - SlideDistance;

    GFX.Gui[Trial.SkullTexture]
      .DrawCentered(slid + new Vector2(IconX, 0f), Color.White * alpha, Vector2.One);
    Color nameColor = highlighted ? Container.HighlightColor : Color.White;
    ActiveFont.DrawOutline(
      Trial.Name,
      slid + new Vector2(NameX, 0f),
      new Vector2(0f, 0.5f),
      Vector2.One,
      nameColor * alpha,
      2f,
      Color.Black * alpha
    );

    TrialRecord record = TrialRecords.Get(Trial.Id);
    if (record == null)
    {
      ActiveFont.Draw(
        "new",
        new Vector2(rightEdge, slid.Y),
        new Vector2(1f, 0.5f),
        Vector2.One * 0.8f,
        Dim * alpha
      );
      return;
    }

    string best = Attempt.Format(record.BestTicks);
    float width = TimeText.Width(best, TimeText.Hud.At(PbScale));
    Medal medal = Trial.Goals?.MedalFor(record.BestTicks) ?? Medal.None;
    if (medal != Medal.None)
      MedalIcon.Draw(
        new Vector2(rightEdge - width - MedalGap, slid.Y),
        medal,
        MedalIconHeight,
        alpha
      );
    TimeText.Draw(
      new Vector2(rightEdge - width, slid.Y + PbBaseline),
      best,
      TimeText.Hud.At(PbScale),
      Color.White * alpha,
      Color.LightGray * alpha
    );
  }
}
