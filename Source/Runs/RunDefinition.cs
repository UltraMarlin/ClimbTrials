using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Runs;

public sealed class RunDefinition
{
  public string Sid { get; init; }
  public AreaMode Mode { get; init; } = AreaMode.Normal;
  public string StartRoom { get; init; }
  public Vector2 Spawn { get; init; }
  public string FinishRoom { get; init; }
  public Rectangle Finish { get; init; }

  public AreaKey Area => AreaData.Get(Sid).ToKey(Mode);
}
