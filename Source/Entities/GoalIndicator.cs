using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class GoalIndicator : Entity
{
  private const string ArrowTexture = "ClimbTrials/goal_arrow";
  private const float CenterLift = 4f;
  private const float StrongRadiusFar = 48f;
  private const float SubtleRadiusFar = 80f;
  private const float RadiusNear = 20f;
  private const float FadeFar = 120f;
  private const float FadeNear = 20f;
  private const float StrongAlpha = 1f;
  private const float SubtleAlpha = 0.4f;
  private const float MinAlpha = 0.1f;
  private const float TurnTime = 0.3f;
  private const float FollowTime = 0.2f;
  private const float EdgeMargin = 10f;
  private static readonly Color CheckpointColor = Calc.HexToColor("b2ebbe");
  private static readonly Color StrawberryColor = Calc.HexToColor("f76f6f");
  private static readonly Color FinishColor = Calc.HexToColor("f7d472");

  private enum TargetKind
  {
    Checkpoint,
    Strawberry,
    Finish,
  }

  private readonly record struct Target(Vector2 Position, TargetKind Kind);

  private readonly record struct Berry(EntityID Id, Vector2 World);

  private readonly MTexture arrow = GFX.Game[ArrowTexture];
  private readonly List<Berry> berries = [];
  private float angle;
  private Color color = CheckpointColor;
  private float alpha;

  public GoalIndicator()
  {
    Tag = Tags.Persistent | Tags.TransitionUpdate | Tags.FrozenUpdate;
    Depth = Depths.Top;
    Visible = false;
  }

  public override void Added(Scene scene)
  {
    base.Added(scene);
    MapData map = SceneAs<Level>().Session.MapData;
    foreach (LevelData room in map.Levels)
    {
      foreach (EntityData entity in room.Entities)
      {
        if (entity.Name == "strawberry" && !entity.Bool("moon"))
          berries.Add(
            new Berry(new EntityID(room.Name, entity.ID), entity.Position + room.Position)
          );
      }
    }
  }

  public override void Update()
  {
    base.Update();
    RunDefinition run = RunManager.Run;
    Player player = Scene.Tracker.GetEntity<Player>();
    if (run == null)
    {
      RemoveSelf();
      return;
    }

    Target? target =
      ClimbTrialsModule.Settings.GoalIndicator != GoalIndicatorMode.Off
      && player != null
      && !player.Dead
        ? FindTarget(run, player)
        : null;
    bool appearing = !Visible;
    Visible = target != null;
    if (target == null)
      return;

    color = target.Value.Kind switch
    {
      TargetKind.Strawberry => StrawberryColor,
      TargetKind.Finish => FinishColor,
      _ => CheckpointColor,
    };
    Vector2 center = player.Center - Vector2.UnitY * CenterLift;
    Vector2 toTarget = target.Value.Position - center;
    float t = Calc.ClampedMap(toTarget.Length(), FadeNear, FadeFar);
    alpha = t * BaseAlpha();
    float radius = MathHelper.Lerp(RadiusNear, RadiusFar(), t);
    if (appearing)
    {
      angle = toTarget.Angle();
      Position = ClampToCamera(center + Calc.AngleToVector(angle, radius));
      return;
    }

    float turn = 1f - (float)Math.Pow(0.01, Engine.DeltaTime / TurnTime);
    angle = Calc.AngleLerp(angle, toTarget.Angle(), turn);
    Vector2 wanted = center + Calc.AngleToVector(angle, radius);
    float follow = 1f - (float)Math.Pow(0.01, Engine.DeltaTime / FollowTime);
    Position = ClampToCamera(Vector2.Lerp(Position, wanted, follow));
  }

  private Vector2 ClampToCamera(Vector2 position)
  {
    Camera camera = SceneAs<Level>().Camera;
    return new Vector2(
      Calc.Clamp(position.X, camera.Left + EdgeMargin, camera.Right - EdgeMargin),
      Calc.Clamp(position.Y, camera.Top + EdgeMargin, camera.Bottom - EdgeMargin)
    );
  }

  private static float BaseAlpha() =>
    ClimbTrialsModule.Settings.GoalIndicator == GoalIndicatorMode.Strong
      ? StrongAlpha
      : SubtleAlpha;

  private static float RadiusFar() =>
    ClimbTrialsModule.Settings.GoalIndicator == GoalIndicatorMode.Strong
      ? StrongRadiusFar
      : SubtleRadiusFar;

  private Target? FindTarget(RunDefinition run, Player player)
  {
    RunProgress progress = RunManager.Progress;
    if (progress.Met(run))
      return new Target(run.FinishCenter, TargetKind.Finish);

    Vector2 from = player.Center;
    Target? best = null;
    float bestDistance = float.MaxValue;

    void Consider(Vector2 point, TargetKind kind)
    {
      float d = Vector2.DistanceSquared(from, point);
      if (d < bestDistance)
      {
        bestDistance = d;
        best = new Target(point, kind);
      }
    }

    for (int i = 0; i < run.Checkpoints.Count; i++)
      if (!progress.IsTaken(i))
        Consider(run.Checkpoints[i].Position, TargetKind.Checkpoint);

    HashSet<EntityID> carried = [];
    foreach (Follower follower in player.Leader.Followers)
    {
      if (follower.Entity is Strawberry berry)
        carried.Add(berry.ID);
    }

    if (progress.Strawberries + carried.Count < run.RequiredStrawberries)
    {
      Session session = SceneAs<Level>().Session;
      foreach (Berry berry in berries)
      {
        if (!session.Strawberries.Contains(berry.Id) && !carried.Contains(berry.Id))
          Consider(berry.World, TargetKind.Strawberry);
      }
    }

    return best;
  }

  public override void Render()
  {
    if (alpha < MinAlpha)
      return;

    Vector2 drawAt = Position.Round();
    Color outline = Color.Black * alpha;
    for (int i = -1; i <= 1; i++)
    {
      for (int j = -1; j <= 1; j++)
      {
        if (i != 0 || j != 0)
          arrow.DrawCentered(drawAt + new Vector2(i, j), outline, 1f, angle);
      }
    }
    arrow.DrawCentered(drawAt, color * alpha, 1f, angle);
  }
}
