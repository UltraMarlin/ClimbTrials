using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste.Mod.ClimbTrials.Entities;
using Celeste.Mod.ClimbTrials.Replays;
using MonoMod.ModInterop;

namespace Celeste.Mod.ClimbTrials.Runs;

public static class SpeedrunToolBridge
{
  [ModImportName("SpeedrunTool.SaveLoad")]
  private static class SaveLoadImports
  {
    public static Func<
      Action<Dictionary<Type, Dictionary<string, object>>, Level>,
      Action<Dictionary<Type, Dictionary<string, object>>, Level>,
      Action,
      Action<Level>,
      Action<Level>,
      Action,
      object
    > RegisterSaveLoadAction = null;
    public static Action<object> Unregister = null;
    public static Action<Type, bool> IgnoreSaveState = null;
  }

  private const string LogTag = "ClimbTrials/SpeedrunTool";
  private const string ModuleName = "SpeedrunTool";
  private const string RoomTimerManagerType = "Celeste.Mod.SpeedrunTool.RoomTimer.RoomTimerManager";
  private const string EndPointType = "Celeste.Mod.SpeedrunTool.RoomTimer.EndPoint";
  private const string DeathStatisticsType =
    "Celeste.Mod.SpeedrunTool.DeathStatistics.DeathStatisticsManager";
  private const string SnapshotKey = "RunSnapshot";
  private const string ToggleOnSound = SFX.ui_main_button_toggle_on;
  private const string ToggleOffSound = SFX.ui_main_button_toggle_off;

  private static readonly Type[] HudTypes =
  [
    typeof(GhostLayer),
    typeof(GhostTags),
    typeof(RunNotice),
    typeof(RunTimerDisplay),
    typeof(RunInfoDisplay),
    typeof(GoalListDisplay),
    typeof(LeaderboardDisplay),
    typeof(GoalIndicator),
    typeof(GhostDeltaDisplay),
    typeof(ComparisonDisplay),
    typeof(ReplayBanner),
    typeof(ReplayInputDisplay),
  ];

  private static object registration;
  private static EverestModule module;
  private static PropertyInfo enabledProperty;
  private static PropertyInfo roomTimerTypeProperty;
  private static MethodInfo clearPbTimesMethod;
  private static MethodInfo updateTimerStateMethod;
  private static MethodInfo resetTimeMethod;
  private static MethodInfo clearDeathStatisticsMethod;
  private static PropertyInfo endPointExistsProperty;
  private static readonly HashSet<string> reportedMissing = [];
  private static bool noticePending;
  private static bool suspendedForReplay;
  private static EverestModuleSaveData saveDataBeforeLoad;

  public static bool Loaded => FindModule() != null;

  public static bool Registered => registration != null;

  public static bool Enabled => Loaded && ReadBool(ref enabledProperty, "Enabled", true);

  public static bool RoomTimerShown
  {
    get
    {
      if (!Enabled)
        return false;
      object roomTimerType = ReadSetting(ref roomTimerTypeProperty, "RoomTimerType");
      return roomTimerType is Enum && Convert.ToInt32(roomTimerType) != 0;
    }
  }

  public static void Initialize()
  {
    typeof(SaveLoadImports).ModInterop();
    if (FindModule() == null)
      return;

    LoadMembers();
    if (SaveLoadImports.RegisterSaveLoadAction == null)
    {
      ReportMissing("SpeedrunTool.SaveLoad export");
      return;
    }

    registration = SaveLoadImports.RegisterSaveLoadAction(
      OnSaveState,
      OnLoadState,
      null,
      OnBeforeSaveState,
      OnBeforeLoadState,
      null
    );

    if (SaveLoadImports.IgnoreSaveState != null)
      foreach (Type hudType in HudTypes)
        SaveLoadImports.IgnoreSaveState(hudType, true);
  }

  public static void Unload()
  {
    if (registration != null)
      SaveLoadImports.Unregister?.Invoke(registration);
    registration = null;
  }

  private static EverestModule FindModule() =>
    module ??= Everest.Modules.FirstOrDefault(everestModule =>
      everestModule.Metadata?.Name == ModuleName
    );

  private static void LoadMembers()
  {
    Assembly assembly = module.GetType().Assembly;
    Type roomTimerManager = assembly.GetType(RoomTimerManagerType);
    clearPbTimesMethod = roomTimerManager?.GetMethod("ClearPbTimes", [typeof(bool)]);
    updateTimerStateMethod = roomTimerManager?.GetMethod("UpdateTimerState", [typeof(bool)]);
    resetTimeMethod = roomTimerManager?.GetMethod("ResetTime", Type.EmptyTypes);
    endPointExistsProperty = assembly.GetType(EndPointType)?.GetProperty("IsExist");
    clearDeathStatisticsMethod = assembly
      .GetType(DeathStatisticsType)
      ?.GetMethod("Clear", Type.EmptyTypes);
  }

  private static object FindSettingsWithProperty(ref PropertyInfo property, string settingName)
  {
    object speedrunToolSettings = FindModule()?._Settings;
    property ??= speedrunToolSettings?.GetType().GetProperty(settingName);
    if (property == null)
      ReportMissing(settingName);
    return property == null ? null : speedrunToolSettings;
  }

  private static object ReadSetting(ref PropertyInfo property, string settingName)
  {
    object speedrunToolSettings = FindSettingsWithProperty(ref property, settingName);
    return speedrunToolSettings == null ? null : property.GetValue(speedrunToolSettings);
  }

  private static bool ReadBool(ref PropertyInfo property, string settingName, bool whenUnreadable)
  {
    object setting = ReadSetting(ref property, settingName);
    return setting is bool ? (bool)setting : whenUnreadable;
  }

  private static void ReportMissing(string member)
  {
    if (reportedMissing.Add(member))
      Logger.Log(LogLevel.Warn, LogTag, $"Speedrun Tool member not found: {member}");
  }

  private static bool CallIfFound(MethodInfo method, string memberName, params object[] arguments)
  {
    if (method == null)
    {
      ReportMissing(memberName);
      return false;
    }

    method.Invoke(null, arguments);
    return true;
  }

  public static void SuspendForReplay()
  {
    if (suspendedForReplay || !Enabled || !WriteEnabled(false))
      return;

    suspendedForReplay = true;
  }

  public static void ResumeAfterReplay()
  {
    if (!suspendedForReplay)
      return;

    suspendedForReplay = false;
    WriteEnabled(true);
  }

  private static bool WriteEnabled(bool enabled)
  {
    object speedrunToolSettings = FindSettingsWithProperty(ref enabledProperty, "Enabled");
    if (speedrunToolSettings == null)
      return false;
    enabledProperty.SetValue(speedrunToolSettings, enabled);
    return true;
  }

  public static bool Toggle(out string noticeKey)
  {
    if (!TryToggle(out bool enabled))
    {
      noticeKey = "climbtrials_notice_speedruntool_options";
      return false;
    }
    Audio.Play(enabled ? ToggleOnSound : ToggleOffSound);
    noticeKey = enabled
      ? "climbtrials_notice_speedruntool_on"
      : "climbtrials_notice_speedruntool_off";
    return true;
  }

  private static bool TryToggle(out bool enabled)
  {
    enabled = false;
    object speedrunToolSettings = FindSettingsWithProperty(ref enabledProperty, "Enabled");
    if (speedrunToolSettings == null || clearPbTimesMethod == null)
    {
      ReportMissing("Enabled toggle");
      return false;
    }
    enabled = !(bool)enabledProperty.GetValue(speedrunToolSettings);
    enabledProperty.SetValue(speedrunToolSettings, enabled);
    if (!enabled)
      clearPbTimesMethod.Invoke(null, [true]);
    module.SaveSettings();
    return true;
  }

  public static void OnRunLevelLoaded(bool practice)
  {
    if (!Loaded)
      return;

    if (practice)
      CallIfFound(resetTimeMethod, "ResetTime");
    else
      CallIfFound(clearPbTimesMethod, "ClearPbTimes", true);
  }

  public static void ClearCarriedState()
  {
    if (Loaded)
      CallIfFound(clearDeathStatisticsMethod, "DeathStatisticsManager.Clear");
  }

  public static void CountGoalAsRoom()
  {
    if (endPointExistsProperty?.PropertyType != typeof(bool))
    {
      ReportMissing("EndPoint.IsExist");
      return;
    }
    bool endPointExists = (bool)endPointExistsProperty.GetValue(null);
    if (!endPointExists)
      CallIfFound(updateTimerStateMethod, "UpdateTimerState", true);
  }

  private static void RunWithoutThrowing(string callbackName, Action action)
  {
    try
    {
      action();
    }
    catch (Exception e)
    {
      Logger.Log(LogLevel.Error, LogTag, $"{callbackName} failed: {e.GetType().Name}: {e.Message}");
    }
  }

  private static void OnBeforeSaveState(Level level) =>
    RunWithoutThrowing(nameof(OnBeforeSaveState), NoteSaveStateUse);

  private static void OnBeforeLoadState(Level level) =>
    RunWithoutThrowing(
      nameof(OnBeforeLoadState),
      () =>
      {
        saveDataBeforeLoad = ClimbTrialsModule.Instance._SaveData;
        NoteSaveStateUse();
      }
    );

  private static void NoteSaveStateUse()
  {
    if (!RunManager.Practice && DebugUse.Record(DebugCause.SaveStateUsed))
      noticePending = true;
  }

  private static void OnSaveState(
    Dictionary<Type, Dictionary<string, object>> slotValues,
    Level level
  ) =>
    RunWithoutThrowing(
      nameof(OnSaveState),
      () =>
      {
        ShowPendingNotice();
        if (RunManager.Run != null && !RunManager.Replaying)
          StoreSnapshot(slotValues, RunManager.TakeSnapshot());
      }
    );

  private static void OnLoadState(
    Dictionary<Type, Dictionary<string, object>> slotValues,
    Level level
  ) =>
    RunWithoutThrowing(
      nameof(OnLoadState),
      () =>
      {
        EverestModuleSaveData beforeLoad = saveDataBeforeLoad;
        saveDataBeforeLoad = null;
        KeepRecordsData(beforeLoad);
        if (RunManager.Run == null)
          return;

        RunManager.AdoptSaveDataAfterLoad();
        if (RunManager.Replaying)
          return;

        ShowPendingNotice();
        if (RunManager.Practice)
          RunManager.RestoreAfterLoad(level, FindSnapshot(slotValues));
      }
    );

  private static void KeepRecordsData(EverestModuleSaveData beforeLoad)
  {
    if (TrialRecords.IsRecordsData(beforeLoad))
      ClimbTrialsModule.Instance._SaveData = beforeLoad;
    else if (TrialRecords.IsRecordsData(ClimbTrialsModule.Instance._SaveData))
      TrialRecords.ReloadFromDisk();
  }

  private static void StoreSnapshot(
    Dictionary<Type, Dictionary<string, object>> slotValues,
    RunSnapshot snapshot
  )
  {
    Dictionary<string, object> bridgeValues = new();
    bridgeValues[SnapshotKey] = snapshot;
    slotValues[typeof(SpeedrunToolBridge)] = bridgeValues;
  }

  private static RunSnapshot FindSnapshot(Dictionary<Type, Dictionary<string, object>> slotValues)
  {
    if (!slotValues.ContainsKey(typeof(SpeedrunToolBridge)))
      return null;

    Dictionary<string, object> bridgeValues = slotValues[typeof(SpeedrunToolBridge)];
    if (!bridgeValues.ContainsKey(SnapshotKey))
      return null;

    return bridgeValues[SnapshotKey] as RunSnapshot;
  }

  private static void ShowPendingNotice()
  {
    if (!noticePending)
      return;
    noticePending = false;
    DebugUse.Notify(DebugCause.SaveStateUsed);
  }
}
