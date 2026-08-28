using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Entities;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Runs;

public static class RunManager
{
  private const string LogTag = "ClimbTrials/Run";

  public static Trial Trial { get; private set; }
  public static RunDefinition Run { get; private set; }
  public static Attempt Attempt { get; } = new();

  private static bool awaitingControl;

  public static void Load()
  {
    Everest.Events.Level.OnLoadLevel += OnLoadLevel;
    Everest.Events.Level.OnAfterUpdate += OnAfterUpdate;
    Everest.Events.Level.OnExit += OnExit;
    Everest.Events.Player.OnDie += OnDie;
    On.Celeste.SpeedrunTimerDisplay.Render += SuppressVanillaTimer;
    Everest.Events.Player.OnAfterUpdate += OnPlayerAfterUpdate;
  }

  public static void Unload()
  {
    Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
    Everest.Events.Level.OnAfterUpdate -= OnAfterUpdate;
    Everest.Events.Level.OnExit -= OnExit;
    Everest.Events.Player.OnDie -= OnDie;
    On.Celeste.SpeedrunTimerDisplay.Render -= SuppressVanillaTimer;
    Everest.Events.Player.OnAfterUpdate -= OnPlayerAfterUpdate;
  }

  public static void StartTrial(Trial trial)
  {
    Trial = trial;
    StartRun(trial.Run);
  }

  public static void StartRun(RunDefinition run)
  {
    Run = run;
    Attempt.Reset();
    Countdown.SetWorldAudioPaused(false);

    SaveData.Start(new SaveData { Name = "ClimbTrials" }, -1);
    Session session = new(run.Area)
    {
      Level = run.StartRoom,
      RespawnPoint = run.Spawn,
      FirstLevel = false,
    };
    SaveData.Instance.StartSession(session);
    awaitingControl = true;
    Engine.Scene = new LevelLoader(session, run.Spawn);
    Logger.Log(LogLevel.Info, LogTag, $"starting trial with Id '{Trial.Id}'");
  }

  public static void Stop()
  {
    Trial = null;
    Run = null;
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
    if (Attempt.State != AttemptState.Idle && ClimbTrialsModule.Settings.Retry.Pressed)
      StartRun(Run);
  }

  private static void OnPlayerAfterUpdate(Player player)
  {
    if (!awaitingControl || Run == null || player.IsIntroState)
      return;
    awaitingControl = false;
    BeginAttempt(player.SceneAs<Level>());
  }

  private static void BeginAttempt(Level level)
  {
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
    Logger.Log(
      LogLevel.Info,
      LogTag,
      $"run '{Trial.Id}' finished in {Attempt.Format(Attempt.ElapsedTicks)}"
    );
    return true;
  }

  private static void OnDie(Player player)
  {
    if (Run == null || Attempt.State is AttemptState.Idle or AttemptState.Finished)
      return;
    if (ClimbTrialsModule.Settings.ResetOnDeath)
      StartRun(Run);
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
