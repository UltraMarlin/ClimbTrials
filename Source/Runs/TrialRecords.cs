using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays;

namespace Celeste.Mod.ClimbTrials.Runs;

public readonly record struct CampaignRecord(Medal Medal, long? TotalTicks);

public static class TrialRecords
{
  private const int Slot = -1;

  public static TrialRecord Get(string trialId) =>
    ClimbTrialsModule.SaveData?.PersonalBests.GetValueOrDefault(trialId);

  public static CampaignRecord Totals(Campaign campaign)
  {
    long total = 0;
    Medal medal = Medal.Grandmaster;
    foreach (Trial trial in campaign.Trials)
    {
      TrialRecord record = Get(trial.Id);
      if (record == null)
        return new CampaignRecord(Medal.None, null);
      total += record.BestTicks;
      Medal earned = trial.Goals?.MedalReached(record.BestTicks) ?? Medal.None;
      if (earned < medal)
        medal = earned;
    }
    return new CampaignRecord(medal, total);
  }

  public static bool IsRecordsData(EverestModuleSaveData saveData) => saveData?.Index == Slot;

  public static void EnsureLoaded()
  {
    if (!IsRecordsData(ClimbTrialsModule.SaveData))
      ReloadFromDisk();
  }

  public static void ReloadFromDisk()
  {
    ClimbTrialsModule module = ClimbTrialsModule.Instance;
    module.DeserializeSaveData(Slot, module.ReadSaveData(Slot));
  }

  public static int Clear()
  {
    EnsureLoaded();
    Dictionary<string, TrialRecord> bests = ClimbTrialsModule.SaveData.PersonalBests;
    int count = bests.Count;
    bests.Clear();
    Write();
    return count;
  }

  public static int Remove(IEnumerable<string> trialIds)
  {
    EnsureLoaded();
    Dictionary<string, TrialRecord> bests = ClimbTrialsModule.SaveData.PersonalBests;
    int removed = 0;
    foreach (string trialId in trialIds)
      if (bests.Remove(trialId))
        removed++;
    Write();
    return removed;
  }

  public static FinishResult Record(Trial trial, long ticks, bool submittable)
  {
    EnsureLoaded();
    Dictionary<string, TrialRecord> bests = ClimbTrialsModule.SaveData.PersonalBests;
    TrialRecord previous = bests.GetValueOrDefault(trial.Id);

    bool faster = previous != null && ticks < previous.BestTicks;
    bool isPb =
      previous == null
      || (submittable && !previous.Submittable)
      || (submittable == previous.Submittable && faster);
    if (isPb)
    {
      bests[trial.Id] = new TrialRecord
      {
        BestTicks = ticks,
        AchievedAtUtc = DateTime.UtcNow,
        Submittable = submittable,
      };
      Write();
    }

    Medal before = Medal.None;
    Medal newMedal = Medal.None;
    if (trial.Goals != null)
    {
      before = previous == null ? Medal.None : trial.Goals.MedalReached(previous.BestTicks);
      Medal after = trial.Goals.MedalReached(ticks);
      if (isPb && after > before)
        newMedal = after;
    }

    return new FinishResult
    {
      Ticks = ticks,
      PreviousBestTicks = faster ? previous.BestTicks : null,
      IsPersonalBest = isPb,
      PreviousMedal = before,
      NewMedal = newMedal,
    };
  }

  public static int Adopt(IEnumerable<(string TrialId, long Ticks, DateTime AchievedAtUtc)> times)
  {
    EnsureLoaded();
    Dictionary<string, TrialRecord> bests = ClimbTrialsModule.SaveData?.PersonalBests;
    if (bests == null)
      return 0;

    int adopted = 0;
    foreach ((string trialId, long ticks, DateTime achievedAtUtc) in times)
    {
      TrialRecord local = bests.GetValueOrDefault(trialId);
      if (local != null && local.BestTicks <= ticks)
        continue;

      if (local != null)
        ReplayStore.Delete(trialId);
      bests[trialId] = new TrialRecord
      {
        BestTicks = ticks,
        AchievedAtUtc = achievedAtUtc,
        Submittable = true,
      };
      adopted++;
    }

    if (adopted > 0)
      Write();

    return adopted;
  }

  private static void Write()
  {
    ClimbTrialsModule module = ClimbTrialsModule.Instance;
    module.WriteSaveData(Slot, module.SerializeSaveData(Slot));
  }
}
