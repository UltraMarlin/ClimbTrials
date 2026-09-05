using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class GoalIndicator : Entity
{
  private const string ArrowTexture = "util/dasharrow/dasharrow00";
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
  private static readonly Color ArrowColor = Calc.HexToColor("6ded87");

  private readonly record struct Berry(EntityID Id, Vector2 World);

  private readonly MTexture arrow = GFX.Game[ArrowTexture];
  private readonly List<Berry> berries = [];
  private float angle;
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

    Vector2? target =
      ClimbTrialsModule.Settings.GoalIndicator != GoalIndicatorMode.Off
      && player != null
      && !player.Dead
        ? Target(run, player)
        : null;
    bool appearing = !Visible;
    Visible = target != null;
    if (target == null)
      return;

    Vector2 center = player.Center - Vector2.UnitY * CenterLift;
    Vector2 toTarget = target.Value - center;
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

  private Vector2? Target(RunDefinition run, Player player)
  {
    RunProgress progress = RunManager.Progress;
    if (progress.Met(run))
      return run.FinishCenter;

    Vector2 from = player.Center;
    Vector2? best = null;
    float bestDistance = float.MaxValue;

    void Consider(Vector2 point)
    {
      float d = Vector2.DistanceSquared(from, point);
      if (d < bestDistance)
      {
        bestDistance = d;
        best = point;
      }
    }

    for (int i = 0; i < run.Checkpoints.Count; i++)
      if (!progress.IsTaken(i))
        Consider(run.Checkpoints[i].Position);

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
          Consider(berry.World);
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
    arrow.DrawCentered(drawAt, ArrowColor * alpha, 1f, angle);
  }
}
