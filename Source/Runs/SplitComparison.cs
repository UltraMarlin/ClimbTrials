using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Runs;

public sealed class SplitComparison(GhostSplits ghost)
{
  private readonly SplitDetector detector = new(ghost.Course);

  public GhostSplits Ghost { get; } = ghost;
  public long TotalDelta { get; private set; }
  public long DeltaChange { get; private set; }

  public void CompareAt(Vector2 position, int state, long ticks)
  {
    long before = TotalDelta;
    long latestGhostTicks = long.MinValue;
    foreach (SplitKey key in detector.FindReachedSplits(position, state))
    {
      if (!Ghost.TryGetTicks(key, out long ghostTicks) || ghostTicks < latestGhostTicks)
        continue;
      latestGhostTicks = ghostTicks;
      TotalDelta = ticks - ghostTicks;
    }
    if (latestGhostTicks != long.MinValue)
      DeltaChange = TotalDelta - before;
  }

  public void Finish(long ticks)
  {
    long before = TotalDelta;
    TotalDelta = ticks - Ghost.FinalTicks;
    DeltaChange = TotalDelta - before;
  }
}
