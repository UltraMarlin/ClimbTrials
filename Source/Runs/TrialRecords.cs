using System;
using System.Collections.Generic;
using System.IO;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays;

namespace Celeste.Mod.ClimbTrials.Runs;

public readonly record struct CampaignRecord(Medal Medal, long? TotalTicks);

public static class TrialRecords
{
  private const int Slot = -1;
  private const string LogTag = "ClimbTrials/Records";
  private const int CurrentVersion = 2;

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
      Medal earned = trial.Goals.MedalReached(record.BestTicks);
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

  public static void DeleteOutdatedRecords()
  {
    EnsureLoaded();
    int version = ClimbTrialsModule.SaveData.Version;
    if (version >= CurrentVersion)
      return;
    int cleared;
    try
    {
      ReplayStore.DeleteAll();
      cleared = Clear();
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
    {
      Logger.Warn(LogTag, $"records of version {version} left for the next start: {e.Message}");
      return;
    }
    Logger.Info(LogTag, $"records of version {version}: cleared {cleared} PBs");
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

  public static bool Delete(string trialId)
  {
    EnsureLoaded();
    try
    {
      ReplayStore.Delete(trialId);
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
    {
      Logger.Warn(
        LogTag,
        $"Delete PB of '{trialId}' stopped, its replay could not be deleted: {e.Message}"
      );
      return false;
    }
    Remove([trialId]);
    return true;
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

    Medal before = previous == null ? Medal.None : trial.Goals.MedalReached(previous.BestTicks);
    Medal after = trial.Goals.MedalReached(ticks);
    Medal newMedal = isPb && after > before ? after : Medal.None;

    return new FinishResult
    {
      Ticks = ticks,
      PreviousBestTicks = faster ? previous.BestTicks : null,
      IsPersonalBest = isPb,
      TiesPersonalBest = !isPb && ticks == previous.BestTicks,
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
    ClimbTrialsModule.SaveData.Version = CurrentVersion;
    ClimbTrialsModule module = ClimbTrialsModule.Instance;
    module.WriteSaveData(Slot, module.SerializeSaveData(Slot));
  }
}
