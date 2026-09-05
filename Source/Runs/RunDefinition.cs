using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Runs;

public readonly record struct CheckpointDef(string Room, Vector2 Position, Vector2? Spawn = null)
{
  public Vector2 SpawnPoint => Spawn ?? Position + new Vector2(0f, 12f);
}

public sealed class RunDefinition
{
  public string Sid { get; init; }
  public AreaMode Mode { get; init; } = AreaMode.Normal;
  public string StartRoom { get; init; }
  public Vector2 Spawn { get; init; }
  public string FinishRoom { get; init; }
  public Rectangle Finish { get; init; }

  public IReadOnlyList<CheckpointDef> Checkpoints { get; init; } = [];
  public int RequiredStrawberries { get; init; }
  public int? Dashes { get; init; }
  public bool Grabless { get; init; }

  public AreaKey Area => AreaData.Get(Sid).ToKey(Mode);
  public Vector2 FinishCenter => new(Finish.Center.X, Finish.Center.Y);
  public bool HasModifiers => Dashes != null || Grabless;
}
