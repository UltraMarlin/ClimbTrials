using System;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

public class FinishZoneDisplay : Entity
{
  private const int Cell = 4;
  private readonly int width;
  private readonly int height;

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
    Color light = (finished ? Color.Gold : Color.White) * 0.75f;
    Color dark = Color.Black * 0.75f;
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
