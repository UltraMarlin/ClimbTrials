using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Editor;

public static class EditorCommands
{
  private const string LogTag = "ClimbTrials/Def";

  [Command(
    "ct_edit",
    "ClimbTrials: load a trial as draft and teleport to the start room. ct_edit <trialId>"
  )]
  public static void DraftEdit(string trialId)
  {
    Trial trial = Campaigns.FindTrial(trialId);
    if (trial == null)
    {
      Engine.Commands.Log($"unknown trial '{trialId}'.");
      return;
    }
    TrialDraft.Load(trial);
    EnterRoomOnDebugSave(trial.Run);
    Engine.Commands.Log($"editing '{trial.Id}'");
  }

  [Command("ct_spawn", "ClimbTrials: place the spawn point in a trial draft")]
  public static void DraftSpawn()
  {
    if (TryGetLevel(out Level level))
      Begin(TrialDesigner.Get(level).BeginSpawn, "left click to place the spawn point");
  }

  [Command("ct_check", "ClimbTrials: place a checkpoint in a trial draft")]
  public static void DraftCheckpoint()
  {
    if (TryGetLevel(out Level level))
      Begin(
        TrialDesigner.Get(level).BeginCheckpoint,
        "left click to place the checkpoint, shift-click to also save the current player position as its spawn"
      );
  }

  [Command("ct_check_rm", "ClimbTrials: remove the last placed checkpoint")]
  public static void DraftCheckpointRemove()
  {
    if (TrialDraft.Checkpoints.Count == 0)
    {
      Engine.Commands.Log("no checkpoints placed");
      return;
    }
    TrialDraft.PlacedCheckpoint removed = TrialDraft.Checkpoints[^1];
    TrialDraft.Checkpoints.RemoveAt(TrialDraft.Checkpoints.Count - 1);
    Engine.Commands.Log($"removed checkpoint {TrialDraft.CheckpointCode(removed)}");
  }

  [Command("ct_fin", "ClimbTrials: draw the finish in a trial draft")]
  public static void DraftFinish()
  {
    if (TryGetLevel(out Level level))
      Begin(TrialDesigner.Get(level).BeginFinish, "draw the finish rectangle with the mouse");
  }

  [Command(
    "ct_exp",
    "ClimbTrials: export the trial draft and copy it from the log.txt file. ct_exp [id] [name]"
  )]
  public static void DraftExport(string id = null, string name = null)
  {
    if (id != null)
      TrialDraft.Id = id;
    if (name != null)
      TrialDraft.Name = name;
    Log(TrialDraft.TrialCode());
    TrialDraft.Reset();
  }

  private static void EnterRoomOnDebugSave(RunDefinition run)
  {
    RunManager.Stop();
    SaveData.InitializeDebugMode();
    AreaKey area = run.Area;
    SaveData.Instance.LastArea_Safe = area;
    Session session = new(area)
    {
      Level = run.StartRoom,
      FirstLevel = false,
      StartedFromBeginning = false,
    };
    RunManager.SwitchScene(new LevelLoader(session, run.Spawn));
  }

  private static void Begin(System.Action begin, string hint)
  {
    begin();
    Engine.Commands.Log($"{hint}, right click to cancel");
    Engine.Commands.Open = false;
  }

  private static bool TryGetLevel(out Level level)
  {
    level = Engine.Scene as Level;
    if (level != null)
      return true;
    Engine.Commands.Log("not in a level");
    return false;
  }

  private static void Log(string line)
  {
    Engine.Commands.Log(line);
    Logger.Log(LogLevel.Info, LogTag, line);
  }
}
