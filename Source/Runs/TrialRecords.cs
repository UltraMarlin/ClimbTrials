using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;

namespace Celeste.Mod.ClimbTrials.Runs;

public readonly record struct CampaignRecord(Medal Medal, long? TotalTicks);

public static class TrialRecords
{
  private const int Slot = -1;

  public static TrialRecord Get(string trialId) =>
    ClimbTrialsModule.SaveData?.PersonalBests.GetValueOrDefault(trialId);

  public static CampaignRecord Get(Campaign campaign)
  {
    long total = 0;
    Medal medal = Medal.Grandmaster;
    foreach (Trial trial in campaign.Trials)
    {
      TrialRecord record = Get(trial.Id);
      if (record == null)
        return new CampaignRecord(Medal.None, null);
      total += record.BestTicks;
      Medal earned = trial.Goals?.MedalFor(record.BestTicks) ?? Medal.None;
      if (earned < medal)
        medal = earned;
    }
    return new CampaignRecord(medal, total);
  }

  public static void EnsureLoaded()
  {
    if (ClimbTrialsModule.SaveData?.Index == Slot)
      return;
    ClimbTrialsModule module = ClimbTrialsModule.Instance;
    module.DeserializeSaveData(Slot, module.ReadSaveData(Slot));
  }

  public static void Clear()
  {
    EnsureLoaded();
    ClimbTrialsModule.SaveData.PersonalBests.Clear();
    Write();
  }

  public static FinishResult Record(Trial trial, long ticks)
  {
    Dictionary<string, TrialRecord> bests = ClimbTrialsModule.SaveData.PersonalBests;
    TrialRecord previous = bests.GetValueOrDefault(trial.Id);

    bool isPb = previous == null || ticks < previous.BestTicks;
    if (isPb)
    {
      bests[trial.Id] = new TrialRecord { BestTicks = ticks, AchievedAtUtc = DateTime.UtcNow };
      Write();
    }

    Medal before = Medal.None;
    Medal newMedal = Medal.None;
    if (trial.Goals != null)
    {
      before = previous == null ? Medal.None : trial.Goals.MedalFor(previous.BestTicks);
      Medal after = trial.Goals.MedalFor(ticks);
      if (isPb && after > before)
        newMedal = after;
    }

    return new FinishResult
    {
      Ticks = ticks,
      PreviousBestTicks = previous?.BestTicks,
      IsPersonalBest = isPb,
      PreviousMedal = before,
      NewMedal = newMedal,
    };
  }

  private static void Write()
  {
    ClimbTrialsModule module = ClimbTrialsModule.Instance;
    module.WriteSaveData(Slot, module.SerializeSaveData(Slot));
  }
}
