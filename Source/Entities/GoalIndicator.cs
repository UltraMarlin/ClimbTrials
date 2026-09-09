using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
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
  private const float RingGap = 12f;
  private const float FadeFar = 120f;
  private const float FadeNear = 20f;
  private const float StrongAlpha = 1f;
  private const float SubtleAlpha = 0.4f;
  private const float MinAlpha = 0.1f;
  private const float TurnTime = 0.3f;
  private const float FollowTime = 0.2f;
  private const float EdgeMargin = 10f;
  private static readonly Color WaypointColor = Calc.HexToColor("b2ebbe");
  private static readonly Color StrawberryColor = Calc.HexToColor("f76f6f");
  private static readonly Color FinishColor = Calc.HexToColor("f7d472");

  private enum TargetKind
  {
    Waypoint,
    Strawberry,
    Finish,
  }

  private static readonly TargetKind[] Rings = [TargetKind.Waypoint, TargetKind.Strawberry];

  private readonly record struct Target(Vector2 Position, TargetKind Kind);

  private readonly record struct Berry(EntityID Id, Vector2 World);

  private sealed class Arrow
  {
    public bool Visible;
    public float Angle;
    public float Alpha;
    public Color Color = WaypointColor;
    public Vector2 Position;
    public FadeBuffer Layer;
  }

  private readonly MTexture texture = GFX.Game[ArrowTexture];
  private readonly List<Berry> berries = [];
  private readonly Arrow[] arrows = new Arrow[Rings.Length];
  private readonly float arrowHalf;
  private readonly Target?[] targets = new Target?[Rings.Length];

  public GoalIndicator()
  {
    Tag = Tags.Persistent | Tags.TransitionUpdate | Tags.FrozenUpdate;
    Depth = Depths.Top;
    Visible = false;
    float diagonal = new Vector2(texture.Width, texture.Height).Length();
    int side = (int)Math.Ceiling(diagonal) + 4;
    arrowHalf = side / 2f;
    for (int ring = 0; ring < arrows.Length; ring++)
    {
      Arrow arrow = new();
      arrow.Layer = new FadeBuffer("climbtrials-goal-arrow", side, side, () => DrawArrow(arrow));
      Add(arrow.Layer);
      arrows[ring] = arrow;
    }
  }

  public override void Added(Scene scene)
  {
    base.Added(scene);
    MapData map = SceneAs<Level>().Session.MapData;
    foreach (LevelData room in map.Levels)
    {
      foreach (EntityData entity in room.Entities)
      {
        if (!RunDefinition.IsTargetBerry(entity))
          continue;
        EntityID id = new(room.Name, entity.ID);
        if (RunManager.Run.IgnoredStrawberries.Contains(id))
          continue;
        berries.Add(new Berry(id, entity.Position + room.Position));
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

    Array.Fill(targets, null);
    bool active =
      ClimbTrialsModule.Settings.GoalIndicator != GoalIndicatorMode.Off
      && player != null
      && !player.Dead;
    if (active)
      FillTargets(run, player);

    Arrow shown = Array.Find(arrows, a => a.Visible);
    int shownCount = Array.FindAll(targets, t => t != null).Length;
    int shownIndex = 0;
    Visible = false;
    for (int ring = 0; ring < arrows.Length; ring++)
    {
      Arrow arrow = arrows[ring];
      bool appearing = !arrow.Visible;
      arrow.Visible = targets[ring] != null;
      if (targets[ring] is not Target target)
        continue;
      Visible = true;
      if (appearing && shown != null)
      {
        arrow.Angle = shown.Angle;
        arrow.Position = shown.Position;
        appearing = false;
      }
      float offset = (shownIndex - (shownCount - 1) / 2f) * RingGap;
      shownIndex++;
      Steer(arrow, target, player, offset, appearing);
    }
    foreach (Arrow arrow in arrows)
    {
      arrow.Layer.Visible = arrow.Visible;
      arrow.Layer.Origin = arrow.Position.Round() - Vector2.One * arrowHalf;
    }
  }

  private void Steer(Arrow arrow, Target target, Player player, float offset, bool appearing)
  {
    arrow.Color = target.Kind switch
    {
      TargetKind.Strawberry => StrawberryColor,
      TargetKind.Finish => FinishColor,
      _ => WaypointColor,
    };
    Vector2 center = player.Center - Vector2.UnitY * CenterLift;
    Vector2 toTarget = target.Position - center;
    float t = Calc.ClampedMap(toTarget.Length(), FadeNear, FadeFar);
    arrow.Alpha = t * BaseAlpha();
    float radius = MathHelper.Lerp(RadiusNear, RadiusFar(), t) + offset;
    if (appearing)
    {
      arrow.Angle = toTarget.Angle();
      arrow.Position = ClampToCamera(center + Calc.AngleToVector(arrow.Angle, radius));
      return;
    }

    float turn = 1f - (float)Math.Pow(0.01, Engine.DeltaTime / TurnTime);
    arrow.Angle = Calc.AngleLerp(arrow.Angle, toTarget.Angle(), turn);
    Vector2 wanted = center + Calc.AngleToVector(arrow.Angle, radius);
    float follow = 1f - (float)Math.Pow(0.01, Engine.DeltaTime / FollowTime);
    arrow.Position = ClampToCamera(Vector2.Lerp(arrow.Position, wanted, follow));
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

  private void FillTargets(RunDefinition run, Player player)
  {
    RunProgress progress = RunManager.Progress;
    if (progress.Met(run))
    {
      targets[0] = new Target(run.FinishCenter, TargetKind.Finish);
      return;
    }
    if (ClimbTrialsModule.Settings.GoalIndicatorTargets == GoalIndicatorTargets.Nearest)
    {
      targets[0] = Nearest(run, player, progress, null);
      return;
    }
    for (int ring = 0; ring < Rings.Length; ring++)
      targets[ring] = Nearest(run, player, progress, Rings[ring]);
  }

  private Target? Nearest(RunDefinition run, Player player, RunProgress progress, TargetKind? only)
  {
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

    if (only is null or TargetKind.Waypoint)
      for (int i = 0; i < run.Waypoints.Count; i++)
        if (!progress.IsTaken(i))
          Consider(run.Waypoints[i].Position, TargetKind.Waypoint);

    if (only is null or TargetKind.Strawberry)
    {
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
    }

    return best;
  }

  private void DrawArrow(Arrow arrow)
  {
    Vector2 drawAt = arrow.Position.Round();
    for (int i = -1; i <= 1; i++)
    {
      for (int j = -1; j <= 1; j++)
      {
        if (i != 0 || j != 0)
          texture.DrawCentered(drawAt + new Vector2(i, j), Color.Black, 1f, arrow.Angle);
      }
    }
    texture.DrawCentered(drawAt, arrow.Color, 1f, arrow.Angle);
  }

  public override void Render()
  {
    foreach (Arrow arrow in arrows)
      arrow.Layer.Alpha = arrow.Alpha < MinAlpha ? 0f : arrow.Alpha;
    base.Render();
  }
}
