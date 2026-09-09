using System;
using System.Linq;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Entities;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.UI;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Runs;

public static class RunManager
{
  private const string LogTag = "ClimbTrials/Run";
  private const string ReplayLimitSound = "event:/ui/game/lookout_off";
  private const string TrialFlagPrefix = "ClimbTrials:trial:";

  public static Trial Trial { get; private set; }
  public static RunDefinition Run { get; private set; }
  public static Attempt Attempt { get; } = new();
  public static RunProgress Progress { get; } = new();
  public static FinishResult LastFinish { get; private set; }
  public static Replay Replay { get; private set; }
  public static bool Replaying => Replay != null;
  public static Replay LastReplay => Replay ?? Recorder.Last;
  public static bool AttemptInProgress =>
    Run != null && Attempt.State is AttemptState.Countdown or AttemptState.Running;
  public static bool OwnsSaveData =>
    SaveData.Instance != null && ReferenceEquals(SaveData.Instance, ownSaveData);

  private static bool awaitingLoad;
  private static bool awaitingControl;
  private static int loadSeed;
  private static string pendingNotice;
  private static string pendingRestart;
  private static EventInstance finishSnapshot;
  private static SaveData ownSaveData;

  public static void Load()
  {
    Everest.Events.Level.OnLoadLevel += OnLoadLevel;
    Everest.Events.Level.OnAfterUpdate += OnAfterUpdate;
    Everest.Events.Level.OnPause += OnPause;
    Everest.Events.Level.OnExit += OnExit;
    On.Celeste.Player.Die += OnPlayerDie;
    On.Celeste.SaveData.AssistModeChecks += OnAssistModeChecks;
    On.Celeste.Strawberry.OnCollect += OnStrawberryCollect;
    On.Celeste.SpeedrunTimerDisplay.Render += SuppressVanillaTimer;
    On.Celeste.TotalStrawberriesDisplay.Render += SuppressVanillaBerryCounter;
    Everest.Events.Player.OnAfterUpdate += OnPlayerAfterUpdate;
  }

  public static void Unload()
  {
    Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
    Everest.Events.Level.OnAfterUpdate -= OnAfterUpdate;
    Everest.Events.Level.OnPause -= OnPause;
    Everest.Events.Level.OnExit -= OnExit;
    On.Celeste.Player.Die -= OnPlayerDie;
    On.Celeste.SaveData.AssistModeChecks -= OnAssistModeChecks;
    On.Celeste.Strawberry.OnCollect -= OnStrawberryCollect;
    On.Celeste.SpeedrunTimerDisplay.Render -= SuppressVanillaTimer;
    On.Celeste.TotalStrawberriesDisplay.Render -= SuppressVanillaBerryCounter;
    Everest.Events.Player.OnAfterUpdate -= OnPlayerAfterUpdate;
  }

  public static void SwitchScene(Scene next)
  {
    Engine.Scene = next;
  }

  public static void StartTrial(Trial trial)
  {
    Trial = trial;
    StartRun(trial.Run);
  }

  private static LevelLoader CreateRunLoader(RunDefinition run, Replay replay)
  {
    ResetRunState();
    RunBinds.Blank();
    Run = run;
    Replay = replay;

    ownSaveData = new SaveData { Name = "ClimbTrials" };
    SaveData.Start(ownSaveData, -1);
    ApplyAssists(SaveData.Instance);
    Session session = new(run.Area)
    {
      Level = run.StartRoom,
      RespawnPoint = run.Spawn,
      FirstLevel = false,
    };
    if (run.Dashes is int dashes)
    {
      PlayerInventory inventory = session.Inventory;
      inventory.Dashes = dashes;
      session.Inventory = inventory;
    }
    session.SetFlag(TrialFlagPrefix + Trial?.Id);
    foreach (CheckpointDef checkpoint in run.Checkpoints)
    {
      string actual = session.MapData.GetAt(checkpoint.Position)?.Name;
      if (actual != checkpoint.Room)
        Logger.Log(
          LogLevel.Warn,
          LogTag,
          $"checkpoint at {checkpoint.Position} says room '{checkpoint.Room}' "
            + $"but lies in '{actual ?? "no room"}'"
        );
    }
    SaveData.Instance.StartSession(session);
    awaitingLoad = true;

    loadSeed = replay?.LoadSeed ?? Calc.Random.Next();
    Logger.Log(LogLevel.Info, LogTag, $"StartRun: trial '{Trial?.Id}', replay {replay != null}");
    return Determinism.CreateSeededLoader(loadSeed, session, run.Spawn);
  }

  public static void StartRun(RunDefinition run)
  {
    SwitchScene(CreateRunLoader(run, null));
  }

  public static void LeaveToTrials()
  {
    OuiTrials.ReturnTrial = Trial;
    Stop();
    SwitchScene(new OverworldLoader(OuiTrials.ReturnStartMode));
  }

  public static void StartReplay(Replay replay)
  {
    Trial trial = Campaigns.FindTrial(replay.TrialId);
    if (trial == null)
      return;
    Trial = trial;
    SwitchScene(CreateRunLoader(trial.Run, replay));
  }

  public static void RestartAttempt(string noticeKey)
  {
    pendingNotice = noticeKey;
    StartRun(Run);
  }

  public static void RestartAttemptAfterSave(string noticeKey)
  {
    pendingRestart = noticeKey;
  }

  public static void ShowNotice(string noticeKey)
  {
    if (Engine.Scene is Level level && level.Tracker.GetEntity<RunNotice>() is RunNotice notice)
      notice.Show(noticeKey);
    else
      pendingNotice = noticeKey;
  }

  private static void RunPendingRestart()
  {
    if (pendingRestart == null || Run == null)
      return;

    if (Replaying)
    {
      pendingRestart = null;
      return;
    }

    string notice = pendingRestart;
    pendingRestart = null;
    RestartAttempt(notice);
  }

  private static void OnPause(Level level, int startIndex, bool minimal, bool quickReset)
  {
    RunPendingRestart();
  }

  public static LevelLoader RestartFromSession(Session session)
  {
    string flag = session.Flags.FirstOrDefault(f => f.StartsWith(TrialFlagPrefix));
    Trial trial = flag == null ? null : Campaigns.FindTrial(flag[TrialFlagPrefix.Length..]);
    if (trial == null)
      return null;
    Trial = trial;
    return CreateRunLoader(trial.Run, null);
  }

  public static void Stop()
  {
    ResetRunState();
    RunBinds.Restore();
    if (Run != null && SaveData.Instance?.FileSlot == -1)
      SaveData.Instance.Assists.NoGrabbing = false;
    Trial = null;
    Run = null;
    pendingNotice = null;
  }

  public static void DiscardAttemptAfterDesync()
  {
    Attempt.Reset();
    Determinism.StopSeeding();
  }

  private static void ResetRunState()
  {
    ClearReplayState();
    DebugUse.Reset();
    LastFinish = null;
    Attempt.Reset();
    Progress.Reset();
    awaitingLoad = false;
    awaitingControl = false;
    pendingRestart = null;
    Countdown.SetWorldAudioPaused(false);
    ReleaseFinishSnapshot();
  }

  private static void ReleaseFinishSnapshot()
  {
    if (finishSnapshot == null)
      return;
    Audio.ReleaseSnapshot(finishSnapshot);
    finishSnapshot = null;
  }

  private static void ClearReplayState()
  {
    Replay = null;
    Determinism.StopSeeding();
    Recorder.Abort();
    Replayer.Clear();
  }

  private static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
  {
    if (Run == null)
      return;
    if (isFromLoader && awaitingLoad)
    {
      awaitingLoad = false;
      awaitingControl = true;
    }
    else if (isFromLoader && !Replaying && (AttemptInProgress || awaitingControl))
    {
      StopRecording(StopReason.Reload);
      if (Attempt.State != AttemptState.Running)
        awaitingControl = true;
    }
    string room = level.Session.Level;

    if (level.Tracker.GetEntity<RunTimerDisplay>() == null)
      level.Add(new RunTimerDisplay());

    if (Trial != null && level.Tracker.GetEntity<RunInfoDisplay>() == null)
      level.Add(new RunInfoDisplay());

    if (Trial != null && level.Tracker.GetEntity<GoalListDisplay>() == null)
      level.Add(new GoalListDisplay());

    if (!Replaying && level.Tracker.GetEntity<GoalIndicator>() == null)
      level.Add(new GoalIndicator());

    if (Replaying && level.Tracker.GetEntity<ReplayBanner>() == null)
      level.Add(new ReplayBanner());

    RunNotice notice = level.Tracker.GetEntity<RunNotice>();
    if (notice == null)
    {
      notice = new RunNotice();
      level.Add(notice);
    }
    if (pendingNotice != null)
    {
      notice.Show(pendingNotice);
      pendingNotice = null;
    }

    for (int i = 0; i < Run.Checkpoints.Count; i++)
      if (Run.Checkpoints[i].Room == room)
        level.Add(new TrialCheckpoint(i, Run.Checkpoints[i]));

    if (room == Run.FinishRoom && level.Tracker.GetEntity<FinishZone>() == null)
      level.Add(new FinishZone(Run.Finish));
  }

  public static int ReplayLimitSeconds => ReplayLimitFor(Trial);

  public static int ReplayLimitFor(Trial trial) =>
    Math.Min(
      trial?.ReplayLimitSeconds ?? Trial.DefaultReplayLimitSeconds,
      Trial.DefaultReplayLimitSeconds
    );

  private static void OnAfterUpdate(Level level)
  {
    if (Run == null)
      return;

    if (pendingRestart != null && !level.Paused)
      RunPendingRestart();
    DebugUse.ObserveFrame();
    if (
      !Replaying
      && AttemptInProgress
      && Recorder.Stopped == StopReason.None
      && !SaveData.Instance.Assists.Equals(ExpectedAssists())
    )
      StopRecording(StopReason.State);
    Attempt.Tick(Engine.RawDeltaTime);
    if (
      Recorder.Recording
      && !level.Paused
      && Attempt.State == AttemptState.Running
      && Attempt.ElapsedTicks >= ReplayLimitSeconds * TimeSpan.TicksPerSecond
    )
      StopRecording(StopReason.Limit);
    if (
      !Replaying
      && Attempt.State is AttemptState.Countdown or AttemptState.Running
      && ClimbTrialsModule.Settings.Retry.Pressed
    )
      StartRun(Run);
  }

  private static void StopRecording(StopReason reason)
  {
    Recorder.Stop(reason);
    Audio.Play(ReplayLimitSound);
    Logger.Log(LogLevel.Info, LogTag, $"recording stopped: {reason}, attempt {Attempt.State}");
  }

  private static void OnPlayerAfterUpdate(Player player)
  {
    if (!awaitingControl || Run == null || player.Dead || player.IsIntroState)
      return;
    awaitingControl = false;
    BeginAttempt(player.SceneAs<Level>());
  }

  private static void BeginAttempt(Level level)
  {
    if (ClimbTrialsModule.Settings.CountdownMode == CountdownMode.Off)
    {
      OnAttemptStart();
      return;
    }
    Attempt.BeginCountdown();
    level.Add(new Countdown(onDone: OnAttemptStart));
  }

  private static void OnAttemptStart()
  {
    Attempt.Start();
    Level level = Engine.Scene as Level;
    if (Replay != null)
    {
      Determinism.Reseed(Replay.RunSeed);
      Replayer.Begin(Replay, level);
      return;
    }
    int runSeed = Calc.Random.Next();
    Determinism.Reseed(runSeed);
    Recorder.Begin(Trial?.Id, loadSeed, runSeed, level);
  }

  private static void SuppressVanillaTimer(
    On.Celeste.SpeedrunTimerDisplay.orig_Render orig,
    SpeedrunTimerDisplay self
  )
  {
    if (Run != null)
      return;
    orig(self);
  }

  private static void SuppressVanillaBerryCounter(
    On.Celeste.TotalStrawberriesDisplay.orig_Render orig,
    TotalStrawberriesDisplay self
  )
  {
    if (Run != null)
      return;
    orig(self);
  }

  public static bool Finish()
  {
    if (Attempt.State != AttemptState.Running || !Progress.Met(Run))
      return false;

    Attempt.Finish();
    Determinism.StopSeeding();
    finishSnapshot ??= Audio.CreateSnapshot("snapshot:/pause_menu");
    if (Replay != null)
    {
      bool inSync = Replayer.Finish(Attempt.ElapsedTicks);
      LastFinish = new FinishResult { Ticks = Attempt.ElapsedTicks };
      Logger.Log(LogLevel.Info, LogTag, $"replay of '{Trial?.Id}' finished, in sync: {inSync}");
      return true;
    }

    Replay recorded = Recorder.Finish(Attempt.ElapsedTicks);
    bool submittable = false;
    if (recorded != null && Trial != null)
    {
      ValidationResult check = ReplayValidator.Check(
        recorded,
        TrialSpec.From(Trial),
        ModCheck.Allowed
      );
      submittable = check.Passes;
      Logger.Log(LogLevel.Info, LogTag, $"replay check: {check.Describe()}");
    }
    if (Trial != null)
    {
      LastFinish = TrialRecords.Record(Trial, Attempt.ElapsedTicks, submittable);
      if (LastFinish.IsPersonalBest && recorded != null)
        ReplayStore.Save(recorded);
      else if (LastFinish.IsPersonalBest)
        ReplayStore.Delete(Trial.Id);
    }
    Logger.Log(
      LogLevel.Info,
      LogTag,
      $"run '{Trial?.Id}' finished in {Attempt.Format(Attempt.ElapsedTicks)}"
    );
    return true;
  }

  private static void ApplyAssists(SaveData saveData)
  {
    if (Run == null || saveData.FileSlot != -1)
      return;
    saveData.Assists.NoGrabbing = Run.Grabless;
  }

  private static Assists ExpectedAssists()
  {
    Assists expected = Assists.Default;
    expected.NoGrabbing = Run.Grabless;
    return expected;
  }

  private static void OnAssistModeChecks(
    On.Celeste.SaveData.orig_AssistModeChecks orig,
    SaveData self
  )
  {
    orig(self);
    ApplyAssists(self);
  }

  private static PlayerDeadBody OnPlayerDie(
    On.Celeste.Player.orig_Die orig,
    Player self,
    Vector2 direction,
    bool evenIfInvincible,
    bool registerDeathInStats
  )
  {
    if (Run != null && Attempt.State == AttemptState.Finished)
      return null;
    PlayerDeadBody body = orig(self, direction, evenIfInvincible, registerDeathInStats);
    if (
      body != null
      && Run != null
      && !Replaying
      && Attempt.State is AttemptState.Countdown or AttemptState.Running
      && ClimbTrialsModule.Settings.ResetOnDeath
    )
    {
      RunDefinition run = Run;
      body.DeathAction = () => StartRun(run);
    }
    return body;
  }

  private static void OnStrawberryCollect(
    On.Celeste.Strawberry.orig_OnCollect orig,
    Strawberry self
  )
  {
    orig(self);
    if (Run == null || Attempt.State != AttemptState.Running || self.Golden || self.Moon)
      return;
    Progress.AddStrawberry();
  }

  private static void OnExit(
    Level level,
    LevelExit exit,
    LevelExit.Mode mode,
    Session session,
    HiresSnow snow
  )
  {
    Stop();
  }
}
