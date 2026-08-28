using System.Globalization;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Commands;

public static class RunCommands
{
  private static string sid,
    startRoom,
    finishRoom;
  private static Vector2 spawn;
  private static Rectangle finish;

  [Command("ct_start", "ClimbTrials: start or restart a trial. ct_start [trialId]")]
  public static void Start(string trialId = "c1-01")
  {
    Trial trial = Campaigns.FindTrial(trialId);
    if (trial == null)
    {
      Engine.Commands.Log($"unknown trial '{trialId}'.");
      return;
    }
    RunManager.StartTrial(trial);
  }

  [Command("ct_stop", "ClimbTrials: abandon the current run")]
  public static void Stop() => RunManager.Stop();

  [Command(
    "ct_def_spawn",
    "ClimbTrials: record chapter, room and player position as the run's spawn"
  )]
  public static void DefSpawn()
  {
    if (!TryGetPlayer(out Level level, out Player player))
      return;
    sid = level.Session.Area.SID;
    startRoom = level.Session.Level;
    spawn = player.Position;
    Engine.Commands.Log($"spawn: {sid} room {startRoom} at {spawn}");
  }

  [Command(
    "ct_def_finish",
    "ClimbTrials: record a finish rectangle centred on the player's feet. ct_def_finish [w] [h]"
  )]
  public static void DefFinish(int w = 16, int h = 24)
  {
    if (!TryGetPlayer(out Level level, out Player player))
      return;
    finishRoom = level.Session.Level;
    finish = new Rectangle((int)player.X - w / 2, (int)player.Y - h, w, h);
    Engine.Commands.Log($"finish: room {finishRoom} rect {finish}");
  }

  [Command("ct_def_dump", "ClimbTrials: print a Trial initializer. ct_def_dump [id] [name]")]
  public static void DefDump(string id = "c1-xx", string name = "Untitled")
  {
    static string F(float v) => v.ToString(CultureInfo.InvariantCulture) + "f";
    string[] lines =
    [
      "new Trial",
      "{",
      $"  Id = \"{id}\",",
      $"  Name = \"{name}\",",
      "  Difficulty = Difficulty.Normal,",
      "  Run = new()",
      "  {",
      $"    Sid = \"{sid}\",",
      $"    StartRoom = \"{startRoom}\",",
      $"    Spawn = new({F(spawn.X)}, {F(spawn.Y)}),",
      $"    FinishRoom = \"{finishRoom}\",",
      $"    Finish = new({finish.X}, {finish.Y}, {finish.Width}, {finish.Height}),",
      "  },",
      "},",
    ];
    foreach (string line in lines)
      Engine.Commands.Log(line);
    Logger.Log(LogLevel.Info, "ClimbTrials/Def", "\n" + string.Join("\n", lines));
  }

  private static bool TryGetPlayer(out Level level, out Player player)
  {
    level = Engine.Scene as Level;
    player = level?.Tracker.GetEntity<Player>();
    if (player != null)
      return true;
    Engine.Commands.Log("not in a level with a player");
    return false;
  }
}
