using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;

namespace Celeste.Mod.ClimbTrials.Runs;

public sealed class FinishResult
{
  public long Ticks { get; init; }
  public long? PreviousBestTicks { get; init; }
  public bool IsPersonalBest { get; init; }
  public bool TiesPersonalBest { get; init; }
  public Medal PreviousMedal { get; init; }
  public Medal NewMedal { get; init; }

  public IEnumerable<Medal> UnlockedMedals()
  {
    for (Medal medal = PreviousMedal + 1; medal <= NewMedal; medal++)
      yield return medal;
  }
}
