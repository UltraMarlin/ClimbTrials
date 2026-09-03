using System.Linq;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Runs;

public static class RunManager
{
  private const string LogTag = "ClimbTrials/Run";

  public static Trial Trial { get; private set; }
  public static RunDefinition Run { get; private set; }
  public static Attempt Attempt { get; } = new();
  public static FinishResult LastFinish { get; private set; }

  private static bool awaitingControl;

  private const string TrialFlagPrefix = "ClimbTrials:trial:";

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

  private static LevelLoader CreateRunLoader(RunDefinition run)
  {
    Run = run;
    LastFinish = null;
    Attempt.Reset();
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
    Logger.Log(LogLevel.Info, LogTag, $"StartRun: starting trial with Id '{Trial?.Id}'");
    return new LevelLoader(session, run.Spawn);
  }

  public static void StartRun(RunDefinition run)
  {
    Engine.Scene = CreateRunLoader(run);
  }

  public static LevelLoader RestartFromSession(Session session)
  {
    string flag = session.Flags.FirstOrDefault(f => f.StartsWith(TrialFlagPrefix));
    Trial trial = flag == null ? null : Campaigns.FindTrial(flag[TrialFlagPrefix.Length..]);
    if (trial == null)
      return null;
    Trial = trial;
    return CreateRunLoader(trial.Run);
  }

  public static void Stop()
  {
    Trial = null;
    Run = null;
    LastFinish = null;
    Attempt.Reset();
    Countdown.SetWorldAudioPaused(false);
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

    if (room == Run.FinishRoom)
    {
      level.Add(new FinishZone(Run.Finish));
    }
  }

  private static void OnAfterUpdate(Level level)
  {
    if (Run == null)
      return;
    Attempt.Tick(Engine.RawDeltaTime);
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
      Attempt.Start();
      return;
    }
    Attempt.BeginCountdown();
    level.Add(new Countdown(onDone: Attempt.Start));
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
    if (Trial != null)
      LastFinish = TrialRecords.Record(Trial, Attempt.ElapsedTicks);
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
