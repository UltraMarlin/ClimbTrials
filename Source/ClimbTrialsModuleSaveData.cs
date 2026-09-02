using System;
using System.Collections.Generic;

namespace Celeste.Mod.ClimbTrials;

public class TrialRecord
{
  public long BestTicks { get; set; }
  public DateTime AchievedAtUtc { get; set; }
}

public class ClimbTrialsModuleSaveData : EverestModuleSaveData
{
  public Dictionary<string, TrialRecord> PersonalBests { get; set; } = [];
}
