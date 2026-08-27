using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Runs;

public sealed class RunDefinition
{
  public string Id { get; init; }
  public string Sid { get; init; }
  public AreaMode Mode { get; init; } = AreaMode.Normal;
  public string StartRoom { get; init; }
  public Vector2 Spawn { get; init; }
  public string FinishRoom { get; init; }
  public Rectangle Finish { get; init; }

  public AreaKey Area => AreaData.Get(Sid).ToKey(Mode);

  public static readonly Dictionary<string, RunDefinition> All = new()
  {
    ["test"] = new()
    {
      Id = "test",
      Sid = "Celeste/1-ForsakenCity",
      StartRoom = "6b",
      Spawn = new(2283f, -1232f),
      FinishRoom = "6a",
      Finish = new(1864, -1168, 16, 24)
    },
    ["resort"] = new()
    {
      Id = "resort",
      Sid = "Celeste/3-CelestialResort",
      StartRoom = "09-b",
      Spawn = new(5070f, -248f),
      FinishRoom = "09-b",
      Finish = new(4838, -48, 16, 24)
    }
  };
}