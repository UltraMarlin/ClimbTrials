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

  public readonly record struct PlacedFinish(string Room, FinishKind Kind, Rectangle? Rect);

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
  public static bool NoRefills;
  public static Session.CoreModes? CoreMode;
  public static bool? DreamDash;
  public static Player.IntroTypes IntroType = Player.IntroTypes.Respawn;
  public static int RequiredStrawberries;
  public static int RequiredSummitGems;

  public static string Sid;
  public static AreaMode Mode;
  public static string StartRoom;
  public static Vector2? Spawn;
  public static Facings? SpawnFacing;
  public static readonly List<PlacedWaypoint> Waypoints = [];
  public static PlacedFinish? Finish;
  public static readonly List<EntityID> IgnoredStrawberries = [];
  public static readonly List<CameraOffsetDef> CameraOffsets = [];
  public static readonly List<SplitCategory> SplitCategories = [];
  public static readonly List<SplitRect> SplitRects = [];
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
    NoRefills = false;
    CoreMode = null;
    DreamDash = null;
    IntroType = Player.IntroTypes.Respawn;
    RequiredStrawberries = 0;
    RequiredSummitGems = 0;
    Sid = null;
    Mode = AreaMode.Normal;
    StartRoom = null;
    Spawn = null;
    SpawnFacing = null;
    Waypoints.Clear();
    Finish = null;
    IgnoredStrawberries.Clear();
    CameraOffsets.Clear();
    SplitCategories.Clear();
    SplitRects.Clear();
    BloomBaseAdd = null;
    LightingAlphaAdd = null;
  }

  public static void Load(Trial trial)
  {
    Reset();
    Id = trial.Id;
    Name = trial.Name;
    Difficulty = trial.Difficulty;
    GoalTimes goals = trial.Goals;
    Bronze = ToSeconds(goals.BronzeTicks);
    Silver = ToSeconds(goals.SilverTicks);
    Gold = ToSeconds(goals.GoldTicks);
    Diamond = ToSeconds(goals.DiamondTicks);
    Grandmaster = ToSeconds(goals.GrandmasterTicks);
    ReplayLimitSeconds = trial.ReplayLimitSeconds;
    RunDefinition run = trial.Run;
    Dashes = run.Dashes;
    Grabless = run.Grabless;
    NoRefills = run.NoRefills;
    CoreMode = run.CoreMode;
    DreamDash = run.DreamDash;
    IntroType = run.IntroType;
    RequiredStrawberries = run.RequiredStrawberries;
    RequiredSummitGems = run.RequiredSummitGems;
    Sid = run.Sid;
    Mode = run.Mode;
    StartRoom = run.StartRoom;
    Spawn = run.Spawn;
    SpawnFacing = run.SpawnFacing;
    foreach (WaypointDef waypoint in run.Waypoints)
      Waypoints.Add(
        new PlacedWaypoint(waypoint.Room, waypoint.Position, waypoint.Spawn, waypoint.SetsSpawn)
      );
    Finish = new PlacedFinish(
      run.FinishRoom,
      run.FinishKind,
      run.FinishKind == FinishKind.Heart || run.Finish.IsEmpty ? null : run.Finish
    );
    IgnoredStrawberries.AddRange(run.IgnoredStrawberries);
    CameraOffsets.AddRange(run.CameraOffsets);
    SplitCategories.AddRange(run.SplitCategories);
    SplitRects.AddRange(run.SplitRects);
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
    FinishKind kind = finish?.Kind ?? FinishKind.Zone;
    Rectangle? drawn = finish?.Rect;
    Rectangle rect = drawn ?? new Rectangle(0, 0, 16, 24);
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
        spawn = ToPair(Spawn ?? Vector2.Zero),
        spawnFacing = SpawnFacing?.ToString(),
        finishRoom = finish?.Room,
        finishKind = JsonNamingPolicy.CamelCase.ConvertName(kind.ToString()),
        finish = kind == FinishKind.Heart || (kind == FinishKind.RoomEnter && drawn == null)
          ? null
          : new[] { rect.X, rect.Y, rect.Width, rect.Height },
        splitCategories = SplitCategories.ConvertAll(category => category.ToString()),
        splitRects = SplitRects.ConvertAll(splitRect => new
        {
          room = splitRect.Room,
          rect = new[]
          {
            splitRect.Rect.X,
            splitRect.Rect.Y,
            splitRect.Rect.Width,
            splitRect.Rect.Height,
          },
        }),
        waypoints = Waypoints.ConvertAll(waypoint => new
        {
          room = waypoint.Room,
          position = ToPair(waypoint.Position),
          spawn = waypoint.Spawn is Vector2 spawn ? ToPair(spawn) : null,
          setsSpawn = waypoint.SetsSpawn,
        }),
        cameraOffsets = CameraOffsets.ConvertAll(offset => new
        {
          room = offset.Room,
          offset = ToPair(offset.Offset),
        }),
        bloomBaseAdd = BloomBaseAdd,
        lightingAlphaAdd = LightingAlphaAdd,
        ignoredStrawberries = IgnoredStrawberries.ConvertAll(strawberry => new
        {
          room = strawberry.Level,
          id = strawberry.ID,
        }),
        requiredStrawberries = RequiredStrawberries,
        requiredSummitGems = RequiredSummitGems,
        dashes = Dashes,
        grabless = Grabless,
        noRefills = NoRefills,
        coreMode = CoreMode?.ToString(),
        dreamDash = DreamDash,
        introType = IntroType.ToString(),
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

  private static float[] ToPair(Vector2 vector) => [vector.X, vector.Y];

  public static string WaypointText(PlacedWaypoint waypoint) =>
    $"{waypoint.Room} [{waypoint.Position.X}, {waypoint.Position.Y}]"
    + (waypoint.Spawn is Vector2 spawn ? $" spawn [{spawn.X}, {spawn.Y}]" : "")
    + (waypoint.SetsSpawn ? "" : " setsSpawn false");

  public static string BerryText(EntityID id) => $"{id.Level} #{id.ID}";

  public static string SplitRectText(SplitRect split) =>
    $"{split.Room} [{split.Rect.X}, {split.Rect.Y}, {split.Rect.Width}, {split.Rect.Height}]";

  private static double ToSeconds(long ticks) => (double)ticks / TimeSpan.TicksPerSecond;
}
