using Celeste.Mod.ClimbTrials.Content;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public abstract class SlidingRow : TextMenu.Item
{
  private float ease;

  protected SlidingRow()
  {
    Selectable = true;
  }

  public override float Height() => RowLayout.RowHeight;

  public override void Update()
  {
    bool selected = Container.Current == this;
    ease = Calc.Approach(ease, selected ? 1f : 0f, 6f * Engine.DeltaTime);
  }

  public void ShowSelectedAtOnce() => ease = 1f;

  protected Vector2 RowStart(Vector2 position) =>
    position + new Vector2(Ease.CubeInOut(ease) * RowLayout.HoveredRowShift, 0f);

  protected static void DrawRightColumn(
    string time,
    Medal medal,
    int? rank,
    float rightEdge,
    float centerY,
    float alpha
  )
  {
    TimeStyle style = TimeText.Hud.At(RowLayout.PbScale);
    float width = TimeText.Width(time ?? TimeText.TypicalTime, style);
    float medalCenter = rightEdge - width - RowLayout.MedalGap;
    if (time != null)
    {
      MedalIcon.Draw(new Vector2(medalCenter, centerY), medal, RowLayout.MedalIconHeight, alpha);
      TimeText.Draw(
        new Vector2(rightEdge - width, centerY + RowLayout.PbBaseline),
        time,
        style,
        MedalIcon.TextColor(medal) * alpha
      );
    }
    RankText.Draw(rank, new Vector2(medalCenter, centerY + RowLayout.RankDrop), alpha);
  }
}
