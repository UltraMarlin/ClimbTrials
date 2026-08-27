using Monocle;

using Celeste.Mod.ClimbTrials.Entities;

namespace Celeste.Mod.ClimbTrials.Runs;

public static class RunManager
{
  private const string LogTag = "ClimbTrials/Run";

  public static RunDefinition Run { get; private set; }
  public static Attempt Attempt { get; } = new();

  public static void Load()
  {
    Everest.Events.Level.OnLoadLevel += OnLoadLevel;
    Everest.Events.Level.OnAfterUpdate += OnAfterUpdate;
    Everest.Events.Level.OnExit += OnExit;
    Everest.Events.Player.OnDie += OnDie;
    On.Celeste.SpeedrunTimerDisplay.Render += SuppressVanillaTimer;
  }

  public static void Unload()
  {
    Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
    Everest.Events.Level.OnAfterUpdate -= OnAfterUpdate;
    Everest.Events.Level.OnExit -= OnExit;
    Everest.Events.Player.OnDie -= OnDie;
    On.Celeste.SpeedrunTimerDisplay.Render -= SuppressVanillaTimer;
  }

  public static void StartRun(RunDefinition run)
  {
    Run = run;
    Attempt.Reset();
    Countdown.SetWorldAudioPaused(false);

    SaveData.InitializeDebugMode();
    Session session = new(run.Area)
    {
      Level = run.StartRoom,
      RespawnPoint = run.Spawn,
      FirstLevel = false,
    };
    SaveData.Instance.StartSession(session);
    Engine.Scene = new LevelLoader(session, run.Spawn);
    Logger.Log(LogLevel.Info, LogTag, $"starting run '{run.Id}' in {run.Sid} room {run.StartRoom}");
  }

  public static void Stop()
  {
    Run = null;
    Attempt.Reset();
    Countdown.SetWorldAudioPaused(false);
  }

  private static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
  {
    if (Run == null) return;
    string room = level.Session.Level;
    Logger.Log(LogLevel.Debug, LogTag, $"room '{room}' loaded (fromLoader={isFromLoader}, intro={playerIntro}, state={Attempt.State})");

    if (level.Tracker.GetEntity<RunTimerDisplay>() == null)
      level.Add(new RunTimerDisplay());
    if (room == Run.FinishRoom)
      level.Add(new FinishZone(Run.Finish));
    if (isFromLoader && Attempt.State == AttemptState.Idle)
      BeginAttempt(level);
  }

  private static void OnAfterUpdate(Level level)
  {
    if (Run == null) return;
    Attempt.Tick(Engine.RawDeltaTime);
    if (Attempt.State != AttemptState.Idle && ClimbTrialsModule.Settings.Retry.Pressed)
      StartRun(Run);
  }

  private static void BeginAttempt(Level level)
  {
    Attempt.BeginCountdown();
    level.Add(new Countdown(onDone: () =>
    {
      Attempt.Start();
      Logger.Log(LogLevel.Info, LogTag, $"run '{Run.Id}' started");
    }));
  }

  private static void SuppressVanillaTimer(On.Celeste.SpeedrunTimerDisplay.orig_Render orig, SpeedrunTimerDisplay self)
  {
    if (Run != null) return;
    orig(self);
  }

  public static bool Finish()
  {
    if (Attempt.State != AttemptState.Running) return false;
    Attempt.Finish();
    Logger.Log(LogLevel.Info, LogTag, $"run '{Run.Id}' finished in {Attempt.Format(Attempt.ElapsedTicks)}");
    return true;
  }

  private static void OnDie(Player player)
  {
    if (Run == null || Attempt.State is AttemptState.Idle or AttemptState.Finished) return;
    if (ClimbTrialsModule.Settings.ResetOnDeath)
      StartRun(Run);
  }

  private static void OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow)
  {
    Stop();
  }
}