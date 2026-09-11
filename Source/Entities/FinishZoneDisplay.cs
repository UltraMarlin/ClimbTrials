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
    Position = new Vector2(worldRect.X, worldRect.Y);
    width = worldRect.Width;
    height = worldRect.Height;
    Depth = Depths.Below;
  }

  public override void Render()
  {
    base.Render();
    bool finished = RunManager.Attempt.State == AttemptState.Finished;
    Color light =
      (finished ? Color.Lerp(tint.Light, Color.Gold, FinishedGold) : tint.Light) * BoardAlpha;
    Color dark =
      (finished ? Color.Lerp(tint.Dark, Color.Gold, FinishedGold) : tint.Dark) * BoardAlpha;
    for (int y = 0; y < height; y += Cell)
    for (int x = 0; x < width; x += Cell)
      Draw.Rect(
        X + x,
        Y + y,
        Math.Min(Cell, width - x),
        Math.Min(Cell, height - y),
        ((x / Cell + y / Cell) % 2 == 0) ? light : dark
      );
  }
}
