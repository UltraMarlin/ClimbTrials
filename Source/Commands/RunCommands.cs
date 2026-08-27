using System.Globalization;
using Microsoft.Xna.Framework;
using Monocle;

using Celeste.Mod.ClimbTrials.Runs;

namespace Celeste.Mod.ClimbTrials.Commands;

public static class RunCommands
{
  private static string sid, startRoom, finishRoom;
  private static Vector2 spawn;
  private static Rectangle finish;

  [Command("ct_start", "ClimbTrials: start or restart a run. ct_start [runId]")]
  public static void Start(string runId = "test")
  {
    if (!RunDefinition.All.TryGetValue(runId, out RunDefinition run))
    {
      Engine.Commands.Log($"unknown run '{runId}'. known: {string.Join(", ", RunDefinition.All.Keys)}");
      return;
    }
    RunManager.StartRun(run);
  }

  [Command("ct_stop", "ClimbTrials: abandon the current run")]
  public static void Stop() => RunManager.Stop();

  [Command("ct_def_spawn", "ClimbTrials: record chapter, room and player position as the run's spawn")]
  public static void DefSpawn()
  {
    if (!TryGetPlayer(out Level level, out Player player)) return;
    sid = level.Session.Area.SID;
    startRoom = level.Session.Level;
    spawn = player.Position;
    Engine.Commands.Log($"spawn: {sid} room {startRoom} at {spawn}");
  }

  [Command("ct_def_finish", "ClimbTrials: record a finish rectangle centred on the player's feet. ct_def_finish [w] [h]")]
  public static void DefFinish(int w = 16, int h = 24)
  {
    if (!TryGetPlayer(out Level level, out Player player)) return;
    finishRoom = level.Session.Level;
    finish = new Rectangle((int)player.X - w / 2, (int)player.Y - h, w, h);
    Engine.Commands.Log($"finish: room {finishRoom} rect {finish}");
  }

  [Command("ct_def_dump", "ClimbTrials: print a RunDefinition dump. ct_def_dump [id]")]
  public static void DefDump(string id = "new")
  {
    static string F(float v) => v.ToString(CultureInfo.InvariantCulture) + "f";
    string code = $"[\"{id}\"] = new() {{ Id = \"{id}\", Sid = \"{sid}\", StartRoom = \"{startRoom}\", "
      + $"Spawn = new({F(spawn.X)}, {F(spawn.Y)}), FinishRoom = \"{finishRoom}\", "
      + $"Finish = new({finish.X}, {finish.Y}, {finish.Width}, {finish.Height}) }},";
    Engine.Commands.Log(code);
    Logger.Log(LogLevel.Info, "ClimbTrials/Def", code);
  }

  private static bool TryGetPlayer(out Level level, out Player player)
  {
    level = Engine.Scene as Level;
    player = level?.Tracker.GetEntity<Player>();
    if (player != null) return true;
    Engine.Commands.Log("not in a level with a player");
    return false;
  }
}