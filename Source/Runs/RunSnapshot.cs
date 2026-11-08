using Celeste.Mod.ClimbTrials.Replays;

namespace Celeste.Mod.ClimbTrials.Runs;

public sealed record RunSnapshot(
  string TrialId,
  AttemptState State,
  long ElapsedTicks,
  bool AwaitingControl,
  Replay RaceReplay,
  RunProgress Progress,
  SplitComparison Comparison
);
