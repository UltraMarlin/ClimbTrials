using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

public readonly record struct GhostSpec(
  Replay Replay,
  Color Color,
  Color TagColor,
  bool Comparison
);

[Tracked]
public class GhostLayer : Entity
{
  private readonly List<PlaybackGhost> ghosts = [];
  private GhostTags tags;
  private long ticks;

  public GhostLayer(long ticks)
  {
    this.ticks = ticks;
    Tag = Tags.Persistent;
    Depth = Depths.Player + 1;
    Add(new BeforeRenderHook(Capture));
  }

  public IReadOnlyList<PlaybackGhost> Ghosts => ghosts;

  public long Ticks => ticks;

  public override void Added(Scene scene)
  {
    base.Added(scene);
    tags = new GhostTags(this);
    scene.Add(tags);
  }

  public override void Removed(Scene scene)
  {
    base.Removed(scene);
    tags?.RemoveSelf();
    DisposeAll();
  }

  public override void SceneEnd(Scene scene)
  {
    base.SceneEnd(scene);
    DisposeAll();
  }

  public void Show(IReadOnlyList<GhostSpec> specs)
  {
    List<PlaybackGhost> kept = [];
    foreach (GhostSpec spec in specs)
    {
      PlaybackGhost ghost = ghosts.Find(candidate => candidate.Replay == spec.Replay);
      if (ghost == null)
      {
        ghost = new PlaybackGhost(spec.Replay, spec.Color, spec.TagColor, spec.Comparison);
        ghost.Sync(ticks);
      }
      kept.Add(ghost);
    }
    foreach (PlaybackGhost ghost in ghosts)
      if (!kept.Contains(ghost))
        ghost.RemoveBuffer();
    ghosts.Clear();
    ghosts.AddRange(kept);
  }

  public void Sync(long elapsedTicks)
  {
    ticks = elapsedTicks;
    foreach (PlaybackGhost ghost in ghosts)
      ghost.Sync(ticks);
  }

  public void Advance(float rawDeltaSeconds) =>
    Sync(ticks + Attempt.SecondsToTicks(rawDeltaSeconds));

  private void DisposeAll()
  {
    foreach (PlaybackGhost ghost in ghosts)
      ghost.RemoveBuffer();
    ghosts.Clear();
  }

  private void Capture()
  {
    foreach (PlaybackGhost ghost in ghosts)
      ghost.Capture();
  }

  public override void Render()
  {
    Player player = Scene.Tracker.GetEntity<Player>();
    foreach (PlaybackGhost ghost in ghosts)
      if (ghost.Visible)
        ghost.Render(player);
  }
}
