using System;
using System.Linq;
using Celeste.Editor;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Entities;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.UI;
using Celeste.Mod.Core;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Runs;

public static class RunManager
{
  private const string LogTag = "ClimbTrials/Run";
  private const string ReplayLimitSound = SFX.ui_game_lookout_off;
  private const string TrialFlagPrefix = "ClimbTrials:trial:";

  public static Trial Trial { get; private set; }
  public static RunDefinition Run { get; private set; }
  public static Attempt Attempt { get; } = new();
  public static RunProgress Progress { get; } = new();
  public static FinishResult LastFinish { get; private set; }
  public static Replay Replay { get; private set; }
  public static bool Replaying => Replay != null;
  public static Replay LastReplay => Replay ?? Recorder.Last;
  public static bool Racing { get; private set; }
  public static Replay RaceReplay { get; private set; }
  public static SplitComparison Comparison { get; private set; }
  public static bool GhostBlockVisible { get; private set; }
  public static bool FinishedBefore { get; private set; }
  public static bool RetryPressed { get; private set; }
  public static bool AttemptInProgress =>
    Run != null && Attempt.State is AttemptState.Countdown or AttemptState.Running;
  public static bool OwnsSaveData =>
    SaveData.Instance != null && ReferenceEquals(SaveData.Instance, ownSaveData);

  private static bool awaitingLoad;
  private static bool awaitingControl;
  private static bool countdownOnControl;
  private static Player.IntroTypes introType;
  private static int loadSeed;
  private static string pendingNotice;
  private static string pendingRestart;
  private static EventInstance finishSnapshot;
  private static SaveData ownSaveData;
  private static GhostSplits raceSplits;

  public static void Load()
  {
    Everest.Events.Level.OnLoadLevel += OnLoadLevel;
    Everest.Events.Level.OnBeforeUpdate += OnBeforeUpdate;
    Everest.Events.Level.OnAfterUpdate += OnAfterUpdate;
    Everest.Events.Level.OnPause += OnPause;
    Everest.Events.Celeste.OnSceneTransition += OnSceneTransition;
    On.Celeste.Player.Added += OnPlayerAdded;
    On.Celeste.Player.Die += OnPlayerDie;
    On.Celeste.Level.Reload += OnLevelReload;
    On.Celeste.SaveData.AssistModeChecks += OnAssistModeChecks;
    On.Celeste.Strawberry.OnCollect += OnStrawberryCollect;
    On.Celeste.SaveData.RegisterSummitGem += OnSummitGemCollect;
    On.Celeste.SpeedrunTimerDisplay.Render += SuppressVanillaTimer;
    On.Celeste.TotalStrawberriesDisplay.Render += SuppressVanillaBerryCounter;
    Everest.Events.Player.OnAfterUpdate += OnPlayerAfterUpdate;
  }

  public static void Unload()
  {
    Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
    Everest.Events.Level.OnBeforeUpdate -= OnBeforeUpdate;
    Everest.Events.Level.OnAfterUpdate -= OnAfterUpdate;
    Everest.Events.Level.OnPause -= OnPause;
    Everest.Events.Celeste.OnSceneTransition -= OnSceneTransition;
    On.Celeste.Player.Added -= OnPlayerAdded;
    On.Celeste.Player.Die -= OnPlayerDie;
    On.Celeste.Level.Reload -= OnLevelReload;
    On.Celeste.SaveData.AssistModeChecks -= OnAssistModeChecks;
    On.Celeste.Strawberry.OnCollect -= OnStrawberryCollect;
    On.Celeste.SaveData.RegisterSummitGem -= OnSummitGemCollect;
    On.Celeste.SpeedrunTimerDisplay.Render -= SuppressVanillaTimer;
    On.Celeste.TotalStrawberriesDisplay.Render -= SuppressVanillaBerryCounter;
    Everest.Events.Player.OnAfterUpdate -= OnPlayerAfterUpdate;
  }

  private static bool SceneSwitchPending => Engine.NextScene != Engine.Scene;

  public static void SwitchScene(Scene next)
  {
    Engine.Scene = next;
    MInput.Keyboard.PreviousState = MInput.Keyboard.CurrentState;
    CoreModule.Settings.DebugMap.ConsumePress();
  }

  public static void StartTrial(Trial trial)
  {
    if (SceneSwitchPending)
      return;
    StopRacing();
    GhostComparisons.Forget();
    Trial = trial;
    StartRun(trial.Run);
  }

  public static void StartRace(Trial trial, Replay replay)
  {
    if (SceneSwitchPending)
      return;
    GhostComparisons.Forget();
    Racing = true;
    RaceReplay = replay;
    Trial = trial;
    StartRun(trial.Run);
  }

  private static LevelLoader CreateRunLoader(
    RunDefinition run,
    Replay replay,
    bool countdown = true
  )
  {
    ResetRunState();
    RunBinds.Blank();
    if (replay == null)
      GhostComparisons.Forget();
    Run = run;
    TrialRecords.EnsureLoaded();
    FinishedBefore = Trial != null && TrialRecords.Get(Trial.Id) != null;
    Replay = replay;
    if (replay == null)
      GameplayRenderer.RenderDebug = false;
    countdownOnControl = countdown && replay == null;

    ownSaveData = new SaveData { Name = "ClimbTrials" };
    SaveData.Start(ownSaveData, -1);
    ApplyAssists(SaveData.Instance);
    Session session = new(run.Area)
    {
      Level = run.StartRoom,
      RespawnPoint = run.Spawn,
      FirstLevel = false,
    };
    PlayerInventory inventory = session.Inventory;
    if (run.Dashes is int dashes)
      inventory.Dashes = dashes;
    if (run.NoRefills)
      inventory.NoRefills = true;
    if (run.DreamDash is bool dreamDash)
      inventory.DreamDash = dreamDash;
    session.Inventory = inventory;
    if (run.CoreMode is Session.CoreModes coreMode)
      session.CoreMode = coreMode;
    if (run.BloomBaseAdd is float bloomAdd)
      session.BloomBaseAdd = bloomAdd;
    if (run.LightingAlphaAdd is float lightingAdd)
      session.LightingAlphaAdd = lightingAdd;
    session.SetFlag(TrialFlagPrefix + Trial?.Id);
    foreach (CameraOffsetDef def in run.CameraOffsets)
      if (session.MapData.Get(def.Room) == null)
        Logger.Log(LogLevel.Warn, LogTag, $"camera offset has room that doesnt exist '{def.Room}'");
    foreach (WaypointDef waypoint in run.Waypoints)
    {
      string actual = session.MapData.GetAt(waypoint.Position)?.Name;
      if (actual != waypoint.Room)
        Logger.Log(
          LogLevel.Warn,
          LogTag,
          $"waypoint at {waypoint.Position} has room '{waypoint.Room}' but is in '{actual ?? "no room"}'"
        );
    }
    foreach (SplitRect rect in run.SplitRects)
      if (!SplitMap.IsInsideItsRoom(rect, session.MapData))
        Logger.Log(
          LogLevel.Warn,
          LogTag,
          $"Discarded: split rectangle {rect.Rect} is not inside room '{rect.Room}'"
        );
    Comparison = CreateSplitComparison(run);
    SaveData.Instance.StartSession(session);
    awaitingLoad = true;

    loadSeed = replay?.LoadSeed ?? Calc.Random.Next();
    introType = replay?.IntroType ?? (countdown ? run.IntroType : Player.IntroTypes.Respawn);
    Logger.Log(LogLevel.Info, LogTag, $"StartRun: trial '{Trial?.Id}', replay {replay != null}");
    LevelLoader loader = Determinism.CreateSeededLoader(loadSeed, session, run.Spawn);
    loader.PlayerIntroTypeOverride = introType;
    return loader;
  }

  private static SplitComparison CreateSplitComparison(RunDefinition run)
  {
    if (!Racing || RaceReplay == null)
      return null;
    if (raceSplits?.Replay != RaceReplay || raceSplits.Course.Run != run)
      raceSplits = GhostSplits.Compute(RaceReplay, SplitMap.Build(run));
    return new SplitComparison(raceSplits);
  }

  private static void UpdateGhostBlockVisibility() =>
    GhostBlockVisible =
      Racing
      && !Replaying
      && Comparison != null
      && ClimbTrialsModule.Settings.HudSettings.RunTimer == RunTimerMode.Detailed;

  public static bool StartRun(RunDefinition run, bool countdown = true)
  {
    if (SceneSwitchPending)
      return false;
    SwitchScene(CreateRunLoader(run, null, countdown));
    return true;
  }

  public static void LeaveToTrials()
  {
    if (SceneSwitchPending)
      return;
    OuiTrials.ReturnTrial = Trial;
    Stop();
    SwitchScene(new OverworldLoader(OuiTrials.ReturnStartMode));
  }

  public static bool StartReplay(Replay replay)
  {
    Trial trial = Campaigns.FindTrial(replay.TrialId);
    if (trial == null || SceneSwitchPending)
      return false;
    Trial = trial;
    StopRacing();
    GhostComparisons.KeepFor(trial, replay);
    SwitchScene(CreateRunLoader(trial.Run, replay));
    return true;
  }

  public static bool RestartAttempt(string noticeKey)
  {
    if (SceneSwitchPending)
      return false;
    pendingNotice = noticeKey;
    return StartRun(Run);
  }

  public static void RestartAttemptAfterSave(string noticeKey)
  {
    pendingRestart = noticeKey;
  }

  public static void ShowText(string text, bool warning)
  {
    if (Engine.Scene is Level level && level.Tracker.GetEntity<RunNotice>() is RunNotice notice)
      notice.ShowText(text, warning);
  }

  public static void ShowNotice(string noticeKey)
  {
    if (Engine.Scene is Level level && level.Tracker.GetEntity<RunNotice>() is RunNotice notice)
      notice.Show(noticeKey);
    else
      pendingNotice = noticeKey;
  }

  private static void ReportUpload(Trial trial, Upload upload)
  {
    if (Trial != trial || Attempt.State != AttemptState.Finished)
      return;

    (string text, bool warning) = Uploader.Describe(upload);
    ShowText(text, warning);
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

    if (RestartAttempt(pendingRestart))
      pendingRestart = null;
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
    GhostComparisons.Forget();
    return CreateRunLoader(trial.Run, null);
  }

  public static void Stop()
  {
    ResetRunState();
    RunBinds.Restore();
    GameplayRenderer.RenderDebug = false;
    if (Run != null && SaveData.Instance?.FileSlot == -1)
      SaveData.Instance.Assists.NoGrabbing = false;
    Trial = null;
    Run = null;
    pendingNotice = null;
    StopRacing();
    GhostComparisons.Forget();
  }

  private static void StopRacing()
  {
    Racing = false;
    RaceReplay = null;
    raceSplits = null;
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
    Comparison = null;
    GhostBlockVisible = false;
    HeartGoal.Reset();
    RoomGoal.Reset();
    awaitingLoad = false;
    awaitingControl = false;
    countdownOnControl = true;
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
    else if (isFromLoader && Replaying)
    {
      awaitingControl = false;
      Replayer.StopOnReload(level);
    }
    else if (isFromLoader && (AttemptInProgress || awaitingControl))
    {
      StopRecording(StopReason.Reload);
      if (Attempt.State != AttemptState.Running)
        awaitingControl = true;
    }
    string room = level.Session.Level;
    ApplyCameraOffset(level, room, playerIntro);

    if (level.Tracker.GetEntity<RunTimerDisplay>() == null)
      level.Add(new RunTimerDisplay());

    if (Trial != null && level.Tracker.GetEntity<RunInfoDisplay>() == null)
      level.Add(new RunInfoDisplay());

    if (Trial != null && level.Tracker.GetEntity<GoalListDisplay>() == null)
      level.Add(new GoalListDisplay());

    if (Trial != null && level.Tracker.GetEntity<LeaderboardDisplay>() == null)
      level.Add(new LeaderboardDisplay());

    if (!Replaying && level.Tracker.GetEntity<GoalIndicator>() == null)
      level.Add(new GoalIndicator());

    if (Replaying)
      WatcherOverlay.Begin();

    if (Replaying && level.Tracker.GetEntity<ReplayBanner>() == null)
      level.Add(new ReplayBanner());

    if (Replaying && level.Tracker.GetEntity<ReplayInputDisplay>() == null)
      level.Add(new ReplayInputDisplay(Replay));

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

    if (level.Tracker.GetEntity<GhostLayer>() == null)
    {
      if (Racing && !Replaying && RaceReplay == null)
      {
        Racing = false;
        notice.Show("climbtrials_race_no_replay");
      }
      GhostLayer ghosts = new(Attempt.ElapsedTicks);
      level.Add(ghosts);
      ghosts.Show(GhostSpecs());
    }

    if (Trial != null && level.Tracker.GetEntity<ComparisonDisplay>() == null)
      level.Add(new ComparisonDisplay());

    UpdateGhostBlockVisibility();
    if (Trial != null && level.Tracker.GetEntity<GhostDeltaDisplay>() == null)
      level.Add(new GhostDeltaDisplay());

    for (int i = 0; i < Run.Waypoints.Count; i++)
      if (Run.Waypoints[i].Room == room)
        level.Add(new TrialWaypoint(i, Run.Waypoints[i]));

    if (room == Run.FinishRoom)
      SpawnFinish(level);
  }

  private static GhostSpec[] GhostSpecs()
  {
    if (Racing && !Replaying && RaceReplay != null)
      return [new GhostSpec(RaceReplay, PlaybackGhost.RaceColor, Color.White, Comparison: false)];
    if (!Replaying)
      return [];
    return
    [
      .. GhostComparisons.Shown.Select(g => new GhostSpec(
        g.Replay,
        g.Color,
        GhostComparisons.NameColor(g.Color),
        Comparison: true
      )),
    ];
  }

  public static void ShowGhosts()
  {
    if (Engine.Scene is Level level && level.Tracker.GetEntity<GhostLayer>() is GhostLayer ghosts)
      ghosts.Show(GhostSpecs());
  }

  private static void ApplyCameraOffset(Level level, string room, Player.IntroTypes playerIntro)
  {
    if (Run.CameraOffsetFor(room) is not Vector2 offset)
      return;

    level.CameraOffset =
      new Vector2(Level.CameraOffsetXInterval, Level.CameraOffsetYInterval) * offset;
    if (playerIntro == Player.IntroTypes.Transition)
      return;

    Player player = level.Tracker.GetEntity<Player>();
    if (player == null)
      return;

    Level.CameraLockModes lockMode = level.CameraLockMode;
    level.CameraLockMode = Level.CameraLockModes.None;
    level.Camera.Position = level.GetFullCameraTargetAt(player, player.Position);
    level.CameraLockMode = lockMode;
    level.CameraUpwardMaxY = level.Camera.Y + 180f;
    if (level.Session.RespawnPoint is Vector2 respawn)
      SpotlightWipe.FocusPoint = respawn - level.Camera.Position;
  }

  private static void SpawnFinish(Level level)
  {
    switch (Run.FinishKind)
    {
      case FinishKind.Zone when level.Tracker.GetEntity<FinishZone>() == null:
        level.Add(new FinishZone(Run.Finish));
        break;
      case FinishKind.Heart
        when !level.Session.LevelData.Entities.Exists(e => e.Name == "blackGem" && !e.Bool("fake")):
        break;
    }
  }

  private static void OnBeforeUpdate(Level level)
  {
    if (Run != null)
    {
      Attempt.Tick(Engine.RawDeltaTime);
      if (
        Recorder.Recording
        && !level.Paused
        && Attempt.State == AttemptState.Running
        && !Trial.FitsReplayLimit(Attempt.ElapsedTicks)
      )
        StopRecording(StopReason.Limit);
    }

    if (Replaying)
      CoreModule.Settings.DebugMap.ConsumePress();
    UpdateGhostBlockVisibility();
    RetryPressed = Run != null && !Replaying && ClimbTrialsModule.Settings.Retry.Pressed;
    if (RetryPressed)
      Input.QuickRestart.ConsumePress();
  }

  private static void OnAfterUpdate(Level level)
  {
    if (Run == null)
      return;

    Replayer.ApplyRecordedTime(level);
    if (pendingRestart != null && !level.Paused)
      RunPendingRestart();
    DebugUse.ObserveFrame();
    if (!awaitingLoad)
      Progress.ObserveRespawn(level.Session.RespawnPoint);
    if (
      !Replaying
      && AttemptInProgress
      && Recorder.Stopped is StopReason.None or StopReason.Limit
      && !SaveData.Instance.Assists.Equals(ExpectedAssists())
    )
      StopRecording(StopReason.State);
    if (Attempt.State == AttemptState.Running && InputCapture.FindRecordedPlayer() is Player runner)
      Comparison?.CompareAt(runner.Position, Attempt.ElapsedTicks);
    HeartGoal.AfterUpdate(level);
    RoomGoal.AfterUpdate(level);
    if (level.Tracker.GetEntity<GhostLayer>() is GhostLayer ghosts)
    {
      if (Attempt.State != AttemptState.Finished)
        ghosts.Sync(Attempt.ElapsedTicks);
      else if (!level.Paused)
        ghosts.Advance(Engine.RawDeltaTime);
    }
    if (
      !Replaying
      && Attempt.State is AttemptState.Countdown or AttemptState.Running
      && RetryPressed
    )
      StartRun(Run);
  }

  private static void StopRecording(StopReason reason)
  {
    if (Recorder.Recording)
      Audio.Play(ReplayLimitSound);
    Recorder.Stop(reason);
    Logger.Log(LogLevel.Info, LogTag, $"recording stopped: {reason}, attempt {Attempt.State}");
  }

  private static void OnPlayerAdded(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
  {
    orig(self, scene);
    if (scene is Level level && level.Session.RespawnPoint == Run?.Spawn)
      ApplySpawnFacing(self);
  }

  private static void ApplySpawnFacing(Player player)
  {
    if (Run?.SpawnFacing is not Facings facing)
      return;
    player.Facing = facing;
    player.Hair.Facing = facing;
    player.Hair.Start();
  }

  private static void OnPlayerAfterUpdate(Player player)
  {
    if (!awaitingControl || Run == null || player.Dead || player.IsIntroState)
      return;
    awaitingControl = false;
    ApplySpawnFacing(player);
    BeginAttempt(player.SceneAs<Level>());
  }

  private static void BeginAttempt(Level level)
  {
    if (ClimbTrialsModule.Settings.CountdownMode == CountdownMode.Off || !countdownOnControl)
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
    Recorder.Begin(Trial, loadSeed, runSeed, introType, level);
    Standings.RefreshMiniBoard(Campaigns.CampaignOf(Trial));
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
    Comparison?.Finish(Attempt.ElapsedTicks);
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
    if (Trial != null)
    {
      if (recorded != null)
      {
        ValidationResult check = ReplayValidator.Check(
          recorded,
          TrialSpec.From(Trial),
          ModCheck.Allowed
        );
        submittable = check.Passes;
        Logger.Log(LogLevel.Info, LogTag, $"replay check: {check.Describe()}");
      }
      else if (Recorder.Stopped == StopReason.Limit)
      {
        ValidationResult check = ReplayValidator.CheckAttempt(ModCheck.Allowed);
        submittable = check.Passes;
        Logger.Log(LogLevel.Info, LogTag, $"attempt check without replay: {check.Describe()}");
      }
      LastFinish = TrialRecords.Record(Trial, Attempt.ElapsedTicks, submittable);
      if (LastFinish.IsPersonalBest)
      {
        if (recorded != null)
          ReplayStore.Save(recorded);
        else
          ReplayStore.Delete(Trial.Id);
        ClimbTrialsModuleSettings settings = ClimbTrialsModule.Settings;
        if (settings.PbAutoSubmit && submittable && !settings.OfflineMode)
        {
          if (Identity.Registered)
            Uploader.Start(Trial, recorded, upload => ReportUpload(Trial, upload));
          else
            ShowNotice("climbtrials_not_connected");
        }
        if (RaceReplay?.IsOwn == true)
          RaceReplay = recorded;
      }
    }
    Logger.Log(
      LogLevel.Info,
      LogTag,
      $"run '{Trial?.Id}' finished in {TimeText.Format(Attempt.ElapsedTicks)}"
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

  private static void OnLevelReload(On.Celeste.Level.orig_Reload orig, Level self)
  {
    if (Run != null && !self.Completed)
      Progress.DropUnkept();
    orig(self);
  }

  private static bool NothingPermanentYet(Session session) =>
    !Progress.RespawnMoved
    && Progress.Strawberries == 0
    && session.DoNotLoad.Count == 0
    && !session.Cassette
    && !session.HeartGem;

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
      && (
        ClimbTrialsModule.Settings.ResetOnDeath
        || NothingPermanentYet(self.SceneAs<Level>().Session)
      )
    )
    {
      RunDefinition run = Run;
      body.DeathAction = () => StartRun(run, countdown: false);
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

  private static void OnSummitGemCollect(
    On.Celeste.SaveData.orig_RegisterSummitGem orig,
    SaveData self,
    int id
  )
  {
    orig(self, id);
    if (Run == null || Attempt.State != AttemptState.Running)
      return;
    Progress.AddSummitGem();
  }

  private static void OnSceneTransition(Scene last, Scene next)
  {
    if (Run == null || next is Level or LevelLoader or AssetReloadHelper)
      return;
    if (next is MapEditor && !Replaying)
      return;
    Stop();
  }
}
