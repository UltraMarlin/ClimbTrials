using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Replays;

namespace Celeste.Mod.ClimbTrials.Runs;

public sealed class GhostSplits
{
  private readonly Dictionary<SplitKey, long> ticksByKey = [];
  private readonly List<SplitKey> reachOrder = [];

  public Replay Replay { get; }
  public SplitMap Course { get; }
  public long FinalTicks => Replay.Ticks;

  private GhostSplits(Replay replay, SplitMap course)
  {
    Replay = replay;
    Course = course;
  }

  public IReadOnlyList<SplitKey> ReachOrder => reachOrder;

  public bool TryGetTicks(SplitKey key, out long ticks) => ticksByKey.TryGetValue(key, out ticks);

  public static GhostSplits Compute(Replay replay, SplitMap course)
  {
    GhostSplits splits = new(replay, course);
    SplitDetector detector = new(course);
    int[] shown = replay.ShownFrames;
    for (int k = 0; k < shown.Length; k++)
    {
      InputFrame frame = replay.Frames[shown[k]];
      if (!frame.HasPlayer)
        continue;
      foreach (SplitKey key in detector.FindReachedSplits(frame.Position))
        if (splits.ticksByKey.TryAdd(key, replay.TicksOfShownFrame(k)))
          splits.reachOrder.Add(key);
    }
    return splits;
  }
}
