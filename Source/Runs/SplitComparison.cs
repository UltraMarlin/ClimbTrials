using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Runs;

public sealed class SplitComparison(GhostSplits ghost)
{
  private readonly SplitDetector detector = new(ghost.Course);
  private long? transitionStartTicks;

  public GhostSplits Ghost { get; } = ghost;
  public long TotalDelta { get; private set; }
  public long DeltaChange { get; private set; }

  public void StartTransition(long ticks) => transitionStartTicks = ticks;

  public void CompareAt(Vector2 position, int state, long ticks, bool transitioning)
  {
    if (!transitioning)
      transitionStartTicks = null;
    long before = TotalDelta;
    long latestGhostTicks = long.MinValue;
    foreach (
      ReachedSplit split in detector.FindReachedSplits(position, state, ticks, transitionStartTicks)
    )
    {
      if (!Ghost.TryGetTicks(split.Key, out long ghostTicks) || ghostTicks < latestGhostTicks)
        continue;
      latestGhostTicks = ghostTicks;
      TotalDelta = split.Ticks - ghostTicks;
    }
    if (latestGhostTicks != long.MinValue)
      DeltaChange = TotalDelta - before;
  }

  public SplitComparison Copy()
  {
    SplitComparison copy = new(Ghost)
    {
      TotalDelta = TotalDelta,
      DeltaChange = DeltaChange,
      transitionStartTicks = transitionStartTicks,
    };
    copy.detector.CopyFrom(detector);
    return copy;
  }

  public void Finish(long ticks)
  {
    long before = TotalDelta;
    TotalDelta = ticks - Ghost.FinalTicks;
    DeltaChange = TotalDelta - before;
  }
}
