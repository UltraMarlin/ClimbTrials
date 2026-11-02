using System;

namespace Celeste.Mod.ClimbTrials.Replays.Codec;

internal sealed class GroupFlags
{
  private const uint StateCap = 31;
  private const int OlderMissCombinations = 4;
  
  private static readonly int FlagCount = 1 + Fields.GroupCount;
  private static readonly int EveryGroupHitBits = (1 << Fields.GroupCount) - 1;

  private static readonly int OwnPartCombinations =
    (1 << (Fields.GroupCount - 1)) * OlderMissCombinations * Trackers.MissAgeCap;

  public readonly bool[] GroupHits = new bool[Fields.GroupCount];

  public int HitBits { get; private set; }
  private int hitBitsBefore;
  private readonly int[] missHistories = new int[FlagCount];
  private readonly int[] lastMisses = new int[FlagCount];

  public GroupFlags()
  {
    Array.Fill(lastMisses, -1);
  }

  public void Code(
    Coder coder,
    FrameTable table,
    int frame,
    bool levelUpdatedBefore,
    int missedGroups
  )
  {
    int stateBefore = (int)Math.Min(table.Get(Column.State, frame - 1), StateCap);
    int frameParts =
      ((stateBefore * 2 + (levelUpdatedBefore ? 1 : 0)) << Fields.GroupCount) + hitBitsBefore;
    HitBits = 0;
    bool everyGroupHits = CodeFlag(coder, 0, frameParts, frame, missedGroups == 0);
    for (int group = 0; group < Fields.GroupCount; group++)
    {
      bool lastGroup = group == Fields.GroupCount - 1;
      bool everyGroupBeforeHit = HitBits == (1 << group) - 1;
      bool hit;
      if (everyGroupHits)
        hit = true;
      else if (lastGroup && everyGroupBeforeHit)
        hit = false;
      else
        hit = CodeFlag(coder, 1 + group, frameParts, frame, (missedGroups & (1 << group)) == 0);
      GroupHits[group] = hit;
      HitBits = HitBits * 2 + (hit ? 1 : 0);
    }
    RecordFrame(frame);
  }
  
  private bool CodeFlag(Coder coder, int flag, int frameParts, int frame, bool hit)
  {
    int missAge = Trackers.MissAgeAfter(frame - lastMisses[flag]);
    int olderMisses = (missHistories[flag] >> 1) & 3;
    int ownParts =
      (HitBits * OlderMissCombinations + olderMisses) * Trackers.MissAgeCap + missAge - 1;
    uint flagContext = Coder.Extend((uint)ContextKind.GroupFlag, flag);
    uint context = Coder.Extend(flagContext, frameParts * OwnPartCombinations + ownParts);
    return coder.CodeBit(context, hit ? 1 : 0) == 1;
  }

  private void RecordFrame(int frame)
  {
    for (int flag = 0; flag < FlagCount; flag++)
    {
      bool missed = flag == 0 ? HitBits != EveryGroupHitBits : !GroupHits[flag - 1];
      missHistories[flag] = (missHistories[flag] << 1) | (missed ? 1 : 0);
      if (missed)
        lastMisses[flag] = frame;
    }
    hitBitsBefore = HitBits;
  }
}
