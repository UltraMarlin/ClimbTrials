using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Runs;

public readonly record struct WaypointDef(
  string Room,
  Vector2 Position,
  Vector2? Spawn = null,
  bool SetsSpawn = true
)
{
  public Vector2? SpawnPoint => SetsSpawn ? Spawn ?? Position + new Vector2(0f, 12f) : null;
}

public readonly record struct CameraOffsetDef(string Room, Vector2 Offset);

public enum SplitCategory
{
  Waypoint,
  RoomTransition,
  Strawberry,
  Gem,
  Boss,
}

public readonly record struct SplitRect(string Room, Rectangle Rect);

public enum CollectibleKind
{
  Strawberry,
  SummitGem,
  Heart,
}

public readonly record struct Collectible(
  CollectibleKind Kind,
  EntityID Id,
  Vector2 World,
  EntityData Entity
);

public sealed class RunDefinition
{
  public string Sid { get; init; }
  public AreaMode Mode { get; init; } = AreaMode.Normal;
  public string StartRoom { get; init; }
  public Vector2 Spawn { get; init; }
  public Facings? SpawnFacing { get; init; }
  public string FinishRoom { get; init; }
  public FinishKind FinishKind { get; init; } = FinishKind.Zone;
  public Rectangle Finish { get; init; }

  public IReadOnlyList<WaypointDef> Waypoints { get; init; } = [];
  public IReadOnlyList<CameraOffsetDef> CameraOffsets { get; init; } = [];
  public IReadOnlyList<SplitCategory> SplitCategories { get; init; } = [];
  public IReadOnlyList<SplitRect> SplitRects { get; init; } = [];

  public float? BloomBaseAdd { get; init; }
  public float? LightingAlphaAdd { get; init; }

  public IReadOnlyList<EntityID> IgnoredStrawberries { get; init; } = [];
  public int RequiredStrawberries { get; init; }
  public int RequiredSummitGems { get; init; }

  public int? Dashes
  {
    get => dashes;
    init
    {
      if (value is < 0 or > 2)
        throw new ArgumentOutOfRangeException(nameof(Dashes), value, "trial dashes must be 0 to 2");
      dashes = value;
    }
  }

  private readonly int? dashes;
  public bool Grabless { get; init; }
  public bool NoRefills { get; init; }

  public Session.CoreModes? CoreMode { get; init; }
  public bool? DreamDash { get; init; }
  public Player.IntroTypes IntroType { get; init; } = Player.IntroTypes.Respawn;

  public static readonly Player.IntroTypes[] AllowedIntroTypes =
  [
    Player.IntroTypes.Respawn,
    Player.IntroTypes.WalkInRight,
    Player.IntroTypes.WalkInLeft,
    Player.IntroTypes.Jump,
    Player.IntroTypes.WakeUp,
    Player.IntroTypes.None,
  ];

  public AreaKey Area => AreaData.Get(Sid).ToKey(Mode);
  public MapData Map => AreaData.Get(Sid).Mode[(int)Mode].MapData;
  public Vector2 FinishCenter => new(Finish.Center.X, Finish.Center.Y);
  public bool HasModifiers => Dashes != null || Grabless || NoRefills;

  public Vector2? CameraOffsetFor(string room)
  {
    foreach (CameraOffsetDef def in CameraOffsets)
      if (def.Room == room)
        return def.Offset;
    return null;
  }

  public bool HasSplitCategory(SplitCategory category) => SplitCategories.Contains(category);

  public static bool IsTargetBerry(EntityData entity) =>
    entity.Name == "strawberry" && !entity.Bool("moon");

  public static bool IsStationaryBerry(EntityData entity) =>
    !entity.Bool("winged") && (entity.Nodes == null || entity.Nodes.Length == 0);

  public bool IsFinishHeart(Collectible item) =>
    item.Kind == CollectibleKind.Heart
    && FinishKind == FinishKind.Heart
    && item.Id.Level == FinishRoom;

  public IEnumerable<Collectible> FindCollectibles(MapData map)
  {
    foreach (LevelData room in map.Levels)
    {
      foreach (EntityData entity in room.Entities)
      {
        Vector2 world = entity.Position + room.Position;
        EntityID id = new(room.Name, entity.ID);
        if (entity.Name == "summitgem")
          yield return new Collectible(CollectibleKind.SummitGem, id, world, entity);
        else if (entity.Name == "blackGem" && !entity.Bool("fake"))
          yield return new Collectible(CollectibleKind.Heart, id, world, entity);
        else if (IsTargetBerry(entity) && !IgnoredStrawberries.Contains(id))
          yield return new Collectible(CollectibleKind.Strawberry, id, world, entity);
      }
    }
  }
}
