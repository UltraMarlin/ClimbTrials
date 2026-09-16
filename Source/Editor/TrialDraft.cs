using System;
using System.Collections.Generic;
using System.Text.Json;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Editor;

public static class TrialDraft
{
  public readonly record struct PlacedWaypoint(
    string Room,
    Vector2 Position,
    Vector2? Spawn,
    bool SetsSpawn = true
  );

  public readonly record struct PlacedFinish(string Room, Rectangle Rect);

  public static string Id;
  public static string Name;
  public static Difficulty Difficulty = Difficulty.Normal;
  public static double Bronze = 25,
    Silver = 20,
    Gold = 15,
    Diamond = 10,
    Grandmaster = 9;
  public static int ReplayLimitSeconds = Trial.DefaultReplayLimitSeconds;
  public static int? Dashes;
  public static bool Grabless;
  public static int RequiredStrawberries;

  public static string Sid;
  public static AreaMode Mode;
  public static string StartRoom;
  public static Vector2? Spawn;
  public static Facings? SpawnFacing;
  public static readonly List<PlacedWaypoint> Waypoints = [];
  public static PlacedFinish? Finish;
  public static readonly List<EntityID> IgnoredStrawberries = [];
  public static readonly List<CameraOffsetDef> CameraOffsets = [];
  public static float? BloomBaseAdd;
  public static float? LightingAlphaAdd;

  public static void Reset()
  {
    Id = null;
    Name = null;
    Difficulty = Difficulty.Normal;
    (Bronze, Silver, Gold, Diamond, Grandmaster) = (25, 20, 15, 10, 9);
    ReplayLimitSeconds = Trial.DefaultReplayLimitSeconds;
    Dashes = null;
    Grabless = false;
    RequiredStrawberries = 0;
    Sid = null;
    Mode = AreaMode.Normal;
    StartRoom = null;
    Spawn = null;
    SpawnFacing = null;
    Waypoints.Clear();
    Finish = null;
    IgnoredStrawberries.Clear();
    CameraOffsets.Clear();
    BloomBaseAdd = null;
    LightingAlphaAdd = null;
  }

  public static void Load(Trial trial)
  {
    Reset();
    Id = trial.Id;
    Name = trial.Name;
    Difficulty = trial.Difficulty;
    if (trial.Goals is GoalTimes goals)
    {
      Bronze = ToSeconds(goals.BronzeTicks);
      Silver = ToSeconds(goals.SilverTicks);
      Gold = ToSeconds(goals.GoldTicks);
      Diamond = ToSeconds(goals.DiamondTicks);
      Grandmaster = ToSeconds(goals.GrandmasterTicks);
    }
    ReplayLimitSeconds = trial.ReplayLimitSeconds;
    RunDefinition run = trial.Run;
    Dashes = run.Dashes;
    Grabless = run.Grabless;
    RequiredStrawberries = run.RequiredStrawberries;
    Sid = run.Sid;
    Mode = run.Mode;
    StartRoom = run.StartRoom;
    Spawn = run.Spawn;
    SpawnFacing = run.SpawnFacing;
    foreach (WaypointDef waypoint in run.Waypoints)
      Waypoints.Add(
        new PlacedWaypoint(waypoint.Room, waypoint.Position, waypoint.Spawn, waypoint.SetsSpawn)
      );
    Finish = new PlacedFinish(run.FinishRoom, run.Finish);
    IgnoredStrawberries.AddRange(run.IgnoredStrawberries);
    CameraOffsets.AddRange(run.CameraOffsets);
    BloomBaseAdd = run.BloomBaseAdd;
    LightingAlphaAdd = run.LightingAlphaAdd;
  }

  public static void SetSpawn(Level level, Vector2 position)
  {
    Sid = level.Session.Area.SID;
    Mode = level.Session.Area.Mode;
    StartRoom = level.Session.Level;
    Spawn = position;
  }

  private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

  public static string TrialJson()
  {
    PlacedFinish? finish = Finish;
    Rectangle rect = finish?.Rect ?? new Rectangle(0, 0, 16, 24);
    var trial = new
    {
      id = Id,
      name = Name,
      difficulty = Difficulty.ToString(),
      replayLimitSeconds = ReplayLimitSeconds,
      run = new
      {
        sid = Sid,
        mode = Mode.ToString(),
        startRoom = StartRoom,
        spawn = Vec(Spawn ?? Vector2.Zero),
        spawnFacing = SpawnFacing?.ToString(),
        finishRoom = finish?.Room,
        finish = new[] { rect.X, rect.Y, rect.Width, rect.Height },
        waypoints = Waypoints.ConvertAll(w => new
        {
          room = w.Room,
          position = Vec(w.Position),
          spawn = w.Spawn is Vector2 s ? Vec(s) : null,
          setsSpawn = w.SetsSpawn,
        }),
        cameraOffsets = CameraOffsets.ConvertAll(c => new
        {
          room = c.Room,
          offset = Vec(c.Offset),
        }),
        bloomBaseAdd = BloomBaseAdd,
        lightingAlphaAdd = LightingAlphaAdd,
        ignoredStrawberries = IgnoredStrawberries.ConvertAll(b => new
        {
          room = b.Level,
          id = b.ID,
        }),
        requiredStrawberries = RequiredStrawberries,
        dashes = Dashes,
        grabless = Grabless,
      },
      goals = new
      {
        bronze = Bronze,
        silver = Silver,
        gold = Gold,
        diamond = Diamond,
        grandmaster = Grandmaster,
      },
    };
    return JsonSerializer.Serialize(trial, JsonOptions) + ",";
  }

  private static float[] Vec(Vector2 v) => [v.X, v.Y];

  public static string WaypointText(PlacedWaypoint waypoint) =>
    $"{waypoint.Room} [{waypoint.Position.X}, {waypoint.Position.Y}]"
    + (waypoint.Spawn is Vector2 s ? $" spawn [{s.X}, {s.Y}]" : "")
    + (waypoint.SetsSpawn ? "" : " setsSpawn false");

  public static string BerryText(EntityID id) => $"{id.Level} #{id.ID}";

  private static double ToSeconds(long ticks) => (double)ticks / TimeSpan.TicksPerSecond;
}
