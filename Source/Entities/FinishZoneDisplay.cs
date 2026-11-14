using System;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

public class FinishZoneDisplay : Entity
{
  private const int Cell = 4;
  private const float BoardAlpha = 0.75f;
  private const float FinishedGold = 0.25f;
  private readonly int width;
  private readonly int height;
  private readonly ChapterTint tint = Colors.ForChapter(RunManager.Run.Area);

  public FinishZoneDisplay(Rectangle worldRect)
  {
    Position = worldRect.Location.ToVector2();
    width = worldRect.Width;
    height = worldRect.Height;
    Depth = Depths.Below + 1;
  }

  public override void Render()
  {
    base.Render();
    bool finished = RunManager.Attempt.State == AttemptState.Finished;
    Color light =
      (finished ? Color.Lerp(tint.Light, Color.Gold, FinishedGold) : tint.Light) * BoardAlpha;
    Color dark =
      (finished ? Color.Lerp(tint.Dark, Color.Gold, FinishedGold) : tint.Dark) * BoardAlpha;
    for (int offsetY = 0; offsetY < height; offsetY += Cell)
    for (int offsetX = 0; offsetX < width; offsetX += Cell)
      Draw.Rect(
        X + offsetX,
        Y + offsetY,
        Math.Min(Cell, width - offsetX),
        Math.Min(Cell, height - offsetY),
        ((offsetX / Cell + offsetY / Cell) % 2 == 0) ? light : dark
      );
  }
}
