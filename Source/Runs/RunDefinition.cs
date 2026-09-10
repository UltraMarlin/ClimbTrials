using System;
using System.Collections.Generic;
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

public sealed class RunDefinition
{
  public string Sid { get; init; }
  public AreaMode Mode { get; init; } = AreaMode.Normal;
  public string StartRoom { get; init; }
  public Vector2 Spawn { get; init; }
  public Facings? SpawnFacing { get; init; }
  public string FinishRoom { get; init; }
  public Rectangle Finish { get; init; }

  public IReadOnlyList<WaypointDef> Waypoints { get; init; } = [];
  public IReadOnlyList<CameraOffsetDef> CameraOffsets { get; init; } = [];

  public float? BloomBaseAdd { get; init; }
  public float? LightingAlphaAdd { get; init; }

  public IReadOnlyList<EntityID> IgnoredStrawberries { get; init; } = [];
  public int RequiredStrawberries { get; init; }

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

  public AreaKey Area => AreaData.Get(Sid).ToKey(Mode);
  public Vector2 FinishCenter => new(Finish.Center.X, Finish.Center.Y);
  public bool HasModifiers => Dashes != null || Grabless;

  public Vector2? CameraOffsetFor(string room)
  {
    foreach (CameraOffsetDef def in CameraOffsets)
      if (def.Room == room)
        return def.Offset;
    return null;
  }

  public static bool IsTargetBerry(EntityData entity) =>
    entity.Name == "strawberry" && !entity.Bool("moon");
}
