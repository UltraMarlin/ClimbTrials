using System;
using System.Collections.Generic;
using System.IO;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays;

namespace Celeste.Mod.ClimbTrials.Runs;

public readonly record struct CampaignRecord(Medal Medal, long? TotalTicks);

public static class TrialRecords
{
  public const int Slot = -1;
  private const string LogTag = "ClimbTrials/Records";

  private static ClimbTrialsModuleSaveData records;

  private static ClimbTrialsModuleSaveData Records
  {
    get
    {
      if (records == null)
      {
        ClimbTrialsModule module = ClimbTrialsModule.Instance;
        module.DeserializeSaveData(Slot, module.ReadSaveData(Slot));
        records = ClimbTrialsModule.SaveData;
      }
      return records;
    }
  }

  public static TrialRecord Get(string trialId) => Records.PersonalBests.GetValueOrDefault(trialId);

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

  public static void PutIntoModuleSaveData() => ClimbTrialsModule.Instance._SaveData = Records;

  public static int Remove(IEnumerable<string> trialIds)
  {
    Dictionary<string, TrialRecord> bests = Records.PersonalBests;
    int removed = 0;
    foreach (string trialId in trialIds)
      if (bests.Remove(trialId))
        removed++;
    Write();
    return removed;
  }

  public static bool Delete(string trialId)
  {
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
    Dictionary<string, TrialRecord> bests = Records.PersonalBests;
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
    Dictionary<string, TrialRecord> bests = Records.PersonalBests;
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
