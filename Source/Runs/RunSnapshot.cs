using Celeste.Mod.ClimbTrials.Replays;

namespace Celeste.Mod.ClimbTrials.Runs;

public sealed record RunSnapshot(
  bool Practice,
  string TrialId,
  AttemptState State,
  long ElapsedTicks,
  bool AwaitingControl,
  bool GoalReached,
  Replay RaceReplay,
  RunProgress Progress,
  SplitComparison Comparison
);
