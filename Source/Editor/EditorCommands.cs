using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Commands;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Editor;

public static class EditorCommands
{
  private const string LogTag = "ClimbTrials/Def";

  [Command(
    "ct_edit",
    "ClimbTrials: load a trial as draft and teleport to the start room. ct_edit [trialId], default: the loaded trial"
  )]
  public static void DraftEdit(string trialId = null)
  {
    trialId ??= RunManager.Trial?.Id;
    if (trialId == null)
    {
      Engine.Commands.Log("no trial loaded. ct_edit <trialId>, or start a trial first");
      return;
    }
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
    if (ConsoleOutput.TryGetLevel(out Level level))
      Begin(
        TrialDesigner.GetOrAddDesigner(level).BeginSpawn,
        "left click to place the spawn point"
      );
  }

  [Command(
    "ct_spawn_facing",
    "ClimbTrials: set the direction the player faces at the trial spawn. ct_spawn_facing left|right|default"
  )]
  public static void DraftSpawnFacing(string facing = null)
  {
    switch (facing?.ToLowerInvariant())
    {
      case "left":
        TrialDraft.SpawnFacing = Facings.Left;
        break;
      case "right":
        TrialDraft.SpawnFacing = Facings.Right;
        break;
      case "default":
        TrialDraft.SpawnFacing = null;
        break;
      default:
        Engine.Commands.Log("usage: ct_spawn_facing left|right|default");
        return;
    }
    Engine.Commands.Log($"spawn facing: {TrialDraft.SpawnFacing?.ToString() ?? "default"}");
  }

  [Command("ct_waypoint", "ClimbTrials: place a waypoint in a trial draft")]
  public static void DraftWaypoint()
  {
    if (ConsoleOutput.TryGetLevel(out Level level))
      Begin(
        TrialDesigner.GetOrAddDesigner(level).BeginWaypoint,
        "left click to place the waypoint, shift-click to save the player position as its spawn, control-click for no spawn"
      );
  }

  [Command("ct_waypoint_rm", "ClimbTrials: remove the last placed waypoint")]
  public static void DraftWaypointRemove()
  {
    if (TrialDraft.Waypoints.Count == 0)
    {
      Engine.Commands.Log("no waypoints placed");
      return;
    }
    TrialDraft.PlacedWaypoint removed = TrialDraft.Waypoints[^1];
    TrialDraft.Waypoints.RemoveAt(TrialDraft.Waypoints.Count - 1);
    Engine.Commands.Log($"removed waypoint {TrialDraft.WaypointText(removed)}");
  }

  [Command(
    "ct_ignore_berry",
    "ClimbTrials: hide a room's berries from the goal indicator in the trial draft. ct_ignore_berry [room]"
  )]
  public static void DraftIgnoreBerries(string room = null)
  {
    if (!ConsoleOutput.TryGetLevel(out Level level))
      return;

    room ??= level.Session.Level;
    LevelData data = level.Session.MapData.Get(room);
    if (data == null)
    {
      Engine.Commands.Log($"unknown room '{room}'");
      return;
    }

    int found = 0;
    foreach (EntityData entity in data.Entities)
    {
      if (!RunDefinition.IsTargetBerry(entity))
        continue;
      found++;
      EntityID id = new(room, entity.ID);
      if (TrialDraft.IgnoredStrawberries.Contains(id))
      {
        Engine.Commands.Log($"already listed {TrialDraft.BerryText(id)}");
        continue;
      }
      TrialDraft.IgnoredStrawberries.Add(id);
      Engine.Commands.Log($"ignoring {TrialDraft.BerryText(id)}");
    }
    if (found == 0)
      Engine.Commands.Log($"no berries in room '{room}'");
  }

  [Command("ct_ignore_berry_rm", "ClimbTrials: remove the last ignored berry from the trial draft")]
  public static void DraftIgnoreBerryRemove()
  {
    if (TrialDraft.IgnoredStrawberries.Count == 0)
    {
      Engine.Commands.Log("no ignored berries");
      return;
    }
    EntityID removed = TrialDraft.IgnoredStrawberries[^1];
    TrialDraft.IgnoredStrawberries.RemoveAt(TrialDraft.IgnoredStrawberries.Count - 1);
    Engine.Commands.Log($"removed {TrialDraft.BerryText(removed)}");
  }

  [Command("ct_fin", "ClimbTrials: draw the finish in a trial draft")]
  public static void DraftFinish()
  {
    if (ConsoleOutput.TryGetLevel(out Level level))
      Begin(
        TrialDesigner.GetOrAddDesigner(level).BeginFinish,
        "draw the finish rectangle with the mouse"
      );
  }

  [Command("ct_split", "ClimbTrials: draw a split rectangle in a trial draft")]
  public static void DraftSplit()
  {
    if (ConsoleOutput.TryGetLevel(out Level level))
      Begin(
        TrialDesigner.GetOrAddDesigner(level).BeginSplit,
        "draw the split rectangle with the mouse"
      );
  }

  [Command("ct_split_rm", "ClimbTrials: remove the last drawn split rectangle")]
  public static void DraftSplitRemove()
  {
    if (TrialDraft.SplitRects.Count == 0)
    {
      Engine.Commands.Log("no split rectangles drawn");
      return;
    }
    SplitRect removed = TrialDraft.SplitRects[^1];
    TrialDraft.SplitRects.RemoveAt(TrialDraft.SplitRects.Count - 1);
    Engine.Commands.Log($"removed split rectangle {TrialDraft.SplitRectText(removed)}");
  }

  [Command(
    "ct_fin_heart",
    "ClimbTrials: make the current room's heart the finish of a trial draft"
  )]
  public static void DraftFinishHeart()
  {
    if (!ConsoleOutput.TryGetLevel(out Level level))
      return;
    string room = level.Session.Level;
    List<EntityData> hearts = level.Session.LevelData.Entities.FindAll(entity =>
      entity.Name == "blackGem" && !entity.Bool("fake")
    );
    if (hearts.Count != 1)
    {
      Engine.Commands.Log(
        hearts.Count == 0
          ? $"no heart in room '{room}'"
          : $"{hearts.Count} hearts in room '{room}', draw a zone instead"
      );
      return;
    }
    TrialDraft.Finish = new TrialDraft.PlacedFinish(room, FinishKind.Heart, null);
    Engine.Commands.Log($"finish: the heart in '{room}'");
  }

  [Command(
    "ct_fin_enter",
    "ClimbTrials: make entering the current room the finish of a trial draft; optionally a finish drawn with ct_fin is used as the goal indicator target"
  )]
  public static void DraftFinishEnter()
  {
    if (!ConsoleOutput.TryGetLevel(out Level level))
      return;
    string room = level.Session.Level;
    Rectangle? target =
      TrialDraft.Finish is { Rect: Rectangle rect } drawn && drawn.Room == room ? rect : null;
    TrialDraft.Finish = new TrialDraft.PlacedFinish(room, FinishKind.RoomEnter, target);
    Engine.Commands.Log(
      target == null
        ? $"finish: entering '{room}', indicator points at the room's centre"
        : $"finish: entering '{room}', indicator points at the finish rectangle"
    );
  }

  [Command(
    "ct_exp",
    "ClimbTrials: export the trial draft as JSON, copy it from log.txt. ct_exp [id] [name]"
  )]
  public static void DraftExport(string id = null, string name = null)
  {
    TrialDraft.Id = id ?? TrialDraft.Id ?? Campaigns.NextTrialId();
    TrialDraft.Name = name ?? TrialDraft.Name ?? TrialDraft.Id;
    ConsoleOutput.Log(LogTag, TrialDraft.TrialJson());
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
      RespawnPoint = run.Spawn,
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
}
