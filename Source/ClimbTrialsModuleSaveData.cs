using System;
using System.Collections.Generic;

namespace Celeste.Mod.ClimbTrials;

public class TrialRecord
{
  public long BestTicks { get; set; }
  public DateTime AchievedAtUtc { get; set; }
  public bool Submittable { get; set; } = true;
}

public class ClimbTrialsModuleSaveData : EverestModuleSaveData
{
  public int Version { get; set; } = 2;

  public Dictionary<string, TrialRecord> PersonalBests { get; set; } = [];
}
