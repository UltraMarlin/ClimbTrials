using System;
using System.Linq;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Entities;
using Celeste.Mod.ClimbTrials.Replays;
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
  public static FinishResult LastFinish { get; private set; }
  public static Replay Replay { get; private set; }
  public static bool Replaying => Replay != null;
  public static Replay LastReplay => Replay ?? Recorder.Last;
  public static bool AttemptInProgress =>
    Run != null && Attempt.State is AttemptState.Countdown or AttemptState.Running;
  public static bool RecordingEnabled => !ModCheck.OthersLoaded;

  private static bool awaitingControl;
  private static int loadSeed;
  private static string pendingNotice;

  public static void Load()
  {
    Everest.Events.Level.OnLoadLevel += OnLoadLevel;
    Everest.Events.Level.OnAfterUpdate += OnAfterUpdate;
    Everest.Events.Level.OnExit += OnExit;
    On.Celeste.Player.Die += OnPlayerDie;
    On.Celeste.SpeedrunTimerDisplay.Render += SuppressVanillaTimer;
    Everest.Events.Player.OnAfterUpdate += OnPlayerAfterUpdate;
  }

  public static void Unload()
  {
    Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
    Everest.Events.Level.OnAfterUpdate -= OnAfterUpdate;
    Everest.Events.Level.OnExit -= OnExit;
    On.Celeste.Player.Die -= OnPlayerDie;
    On.Celeste.SpeedrunTimerDisplay.Render -= SuppressVanillaTimer;
    Everest.Events.Player.OnAfterUpdate -= OnPlayerAfterUpdate;
  }

  public static void StartTrial(Trial trial)
  {
    Trial = trial;
    StartRun(trial.Run);
  }

  private static LevelLoader CreateRunLoader(RunDefinition run, Replay replay)
  {
    Run = run;
    LastFinish = null;
    Attempt.Reset();
    ClearReplayState();
    Replay = replay;
    Countdown.SetWorldAudioPaused(false);

    SaveData.Start(new SaveData { Name = "ClimbTrials" }, -1);
    Session session = new(run.Area)
    {
      Level = run.StartRoom,
      RespawnPoint = run.Spawn,
      FirstLevel = false,
    };
    session.SetFlag(TrialFlagPrefix + Trial?.Id);
    SaveData.Instance.StartSession(session);
    awaitingControl = true;

    loadSeed = replay?.LoadSeed ?? Calc.Random.Next();
    Determinism.Begin(loadSeed);
    Logger.Log(LogLevel.Info, LogTag, $"StartRun: trial '{Trial?.Id}', replay {replay != null}");
    return Determinism.CreateLoader(loadSeed, session, run.Spawn);
  }

  public static void StartRun(RunDefinition run)
  {
    Engine.Scene = CreateRunLoader(run, null);
  }

  public static void StartReplay(Replay replay)
  {
    Trial trial = Campaigns.FindTrial(replay.TrialId);
    if (trial == null)
      return;
    Trial = trial;
    Engine.Scene = CreateRunLoader(trial.Run, replay);
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
    Trial = null;
    Run = null;
    LastFinish = null;
    Attempt.Reset();
    ClearReplayState();
    Countdown.SetWorldAudioPaused(false);
  }

  private static void ClearReplayState()
  {
    Replay = null;
    Determinism.End();
    Recorder.Abort();
    Replayer.Clear();
  }

  private static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
  {
    if (Run == null)
      return;
    string room = level.Session.Level;

    if (level.Tracker.GetEntity<RunTimerDisplay>() == null)
      level.Add(new RunTimerDisplay());

    if (Trial?.Goals != null && level.Tracker.GetEntity<GoalListDisplay>() == null)
      level.Add(new GoalListDisplay());

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

    if (room == Run.FinishRoom)
    {
      level.Add(new FinishZone(Run.Finish));
    }
  }

  public static int ReplayLimitSeconds =>
    Math.Min(
      Trial?.ReplayLimitSeconds ?? Trial.DefaultReplayLimitSeconds,
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
      Attempt.State is AttemptState.Countdown or AttemptState.Running
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
    if (Attempt.State != AttemptState.Running)
      return false;
    Attempt.Finish();
    Determinism.End();
    if (Replay != null)
    {
      bool inSync = Replayer.Finish();
      LastFinish = new FinishResult { Ticks = Attempt.ElapsedTicks };
      Logger.Log(LogLevel.Info, LogTag, $"replay of '{Trial?.Id}' finished, in sync: {inSync}");
      return true;
    }
    Replay recorded = Recorder.Finish(Attempt.ElapsedTicks);
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
