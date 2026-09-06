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

  private static bool awaitingLoad;
  private static bool awaitingControl;
  private static int loadSeed;
  private static string pendingNotice;
  private static EventInstance finishSnapshot;

  public static void Load()
  {
    Everest.Events.Level.OnLoadLevel += OnLoadLevel;
    Everest.Events.Level.OnAfterUpdate += OnAfterUpdate;
    Everest.Events.Level.OnExit += OnExit;
    On.Celeste.Player.Die += OnPlayerDie;
    On.Celeste.Strawberry.OnCollect += OnStrawberryCollect;
    On.Celeste.SpeedrunTimerDisplay.Render += SuppressVanillaTimer;
    Everest.Events.Player.OnAfterUpdate += OnPlayerAfterUpdate;
  }

  public static void Unload()
  {
    Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
    Everest.Events.Level.OnAfterUpdate -= OnAfterUpdate;
    Everest.Events.Level.OnExit -= OnExit;
    On.Celeste.Player.Die -= OnPlayerDie;
    On.Celeste.Strawberry.OnCollect -= OnStrawberryCollect;
    On.Celeste.SpeedrunTimerDisplay.Render -= SuppressVanillaTimer;
    Everest.Events.Player.OnAfterUpdate -= OnPlayerAfterUpdate;
  }

  private static void SwitchScene(Scene next)
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
    Run = run;
    Replay = replay;

    SaveData.Start(new SaveData { Name = "ClimbTrials" }, -1);
    SaveData.Instance.Assists.NoGrabbing = run.Grabless;
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
    LastFinish = null;
    Attempt.Reset();
    Progress.Reset();
    awaitingLoad = false;
    awaitingControl = false;
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
    string room = level.Session.Level;

    if (level.Tracker.GetEntity<RunTimerDisplay>() == null)
      level.Add(new RunTimerDisplay());

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

    if (room == Run.FinishRoom)
    {
      level.Add(new FinishZone(Run.Finish));
    }
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
    Attempt.Tick(Engine.RawDeltaTime);
    if (
      Recorder.Recording
      && !level.Paused
      && Attempt.State == AttemptState.Running
      && Attempt.ElapsedTicks >= ReplayLimitSeconds * TimeSpan.TicksPerSecond
    )
    {
      Recorder.StopAtLimit();
      Audio.Play(ReplayLimitSound);
      Logger.Log(LogLevel.Info, LogTag, $"recording stopped at {ReplayLimitSeconds} s");
    }
    if (
      !Replaying
      && Attempt.State is AttemptState.Countdown or AttemptState.Running
      && ClimbTrialsModule.Settings.Retry.Pressed
    )
      StartRun(Run);
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
    if (recorded != null && Trial != null)
    {
      ValidationResult check = ReplayValidator.Check(
        recorded,
        TrialSpec.From(Trial),
        ModCheck.Allowed
      );
      Logger.Log(LogLevel.Info, LogTag, $"replay check: {check.Describe()}");
    }
    if (Trial != null)
    {
      LastFinish = TrialRecords.Record(Trial, Attempt.ElapsedTicks);
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

  private static PlayerDeadBody OnPlayerDie(
    On.Celeste.Player.orig_Die orig,
    Player self,
    Vector2 direction,
    bool evenIfInvincible,
    bool registerDeathInStats
  )
  {
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
