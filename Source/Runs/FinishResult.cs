using Celeste.Mod.ClimbTrials.Content;

namespace Celeste.Mod.ClimbTrials.Runs;

public sealed class FinishResult
{
  public long Ticks { get; init; }
  public long? PreviousBestTicks { get; init; }
  public bool IsPersonalBest { get; init; }
  public Medal NewMedal { get; init; }
}
