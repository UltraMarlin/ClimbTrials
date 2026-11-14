using System;
using System.Collections.Generic;
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
  private const float RadiusNear = 20f;
  private const float RingGap = 12f;
  private const float FadeFar = 120f;
  private const float FadeNear = 20f;
  private const float MinAlpha = 0.1f;
  private const float EdgeMargin = 10f;
  private static readonly Color WaypointColor = Calc.HexToColor("b2ebbe");
  private static readonly Color StrawberryColor = Calc.HexToColor("f76f6f");
  private static readonly Color SummitGemColor = Calc.HexToColor("c2f0ff");
  private static readonly Color FinishColor = Calc.HexToColor("f7d472");

  private readonly record struct IndicatorLook(float Alpha, float RadiusFar);

  private static readonly IndicatorLook StrongLook = new(Alpha: 1f, RadiusFar: 48f);
  private static readonly IndicatorLook SubtleLook = new(Alpha: 0.6f, RadiusFar: 80f);

  private readonly record struct IndicatorMotion(float TurnTime, float FollowTime);

  private static readonly IndicatorMotion SnappyMotion = new(TurnTime: 0.2f, FollowTime: 0.1f);
  private static readonly IndicatorMotion SmoothMotion = new(TurnTime: 0.5f, FollowTime: 0.2f);

  private static GoalIndicatorSettings Settings => ClimbTrialsModule.Settings.GoalIndicatorSettings;

  private static bool Shown =>
    Settings.Style != GoalIndicatorMode.Off
    && !(Settings.HideAfterFirstFinish && RunManager.FinishedBefore);

  private static IndicatorLook Look =>
    Settings.Style == GoalIndicatorMode.Strong ? StrongLook : SubtleLook;

  private enum TargetKind
  {
    Waypoint,
    Strawberry,
    SummitGem,
    Finish,
  }

  private static readonly TargetKind[] Rings =
  [
    TargetKind.Waypoint,
    TargetKind.Strawberry,
    TargetKind.SummitGem,
  ];

  private readonly record struct Target(Vector2 Position, TargetKind Kind);

  private readonly record struct Berry(EntityID Id, Vector2 World);

  private readonly record struct Gem(int Id, Vector2 World);

  private sealed class Arrow
  {
    public bool Visible;
    public float Angle;
    public float Alpha;
    public Color Color = WaypointColor;
    public Vector2 Position;
    public Target? Aim;
    public IndicatorLook PlacedLook;
    public FadeBuffer Layer;
  }

  private readonly MTexture texture = GFX.Game[ArrowTexture];
  private readonly List<Berry> berries = [];
  private readonly List<Gem> gems = [];
  private Vector2? heart;
  private Vector2? goalRoomCenter;
  private readonly Arrow[] arrows = new Arrow[Rings.Length];
  private readonly float arrowHalf;
  private readonly Target?[] targets = new Target?[Rings.Length];
  private readonly HashSet<EntityID> carried = [];

  public static void Load()
  {
    On.Celeste.LightingRenderer.Render += OnLightingRender;
  }

  public static void Unload()
  {
    On.Celeste.LightingRenderer.Render -= OnLightingRender;
  }

  private static void OnLightingRender(
    On.Celeste.LightingRenderer.orig_Render orig,
    LightingRenderer self,
    Scene scene
  )
  {
    orig(self, scene);
    if (scene.Tracker.GetEntity<GoalIndicator>() is not { Visible: true } indicator)
      return;
    GameplayRenderer.Begin();
    indicator.Components.Render();
    GameplayRenderer.End();
  }

  public GoalIndicator()
  {
    Tag = Tags.Persistent | Tags.TransitionUpdate | Tags.PauseUpdate;
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
    RunDefinition run = RunManager.Run;
    if (run.FinishKind == FinishKind.RoomEnter && map.Get(run.FinishRoom) is LevelData goal)
      goalRoomCenter = goal.Bounds.Center.ToVector2();
    foreach (Collectible item in run.FindCollectibles(map))
    {
      if (item.Kind == CollectibleKind.SummitGem)
        gems.Add(new Gem(item.Entity.Int("gem"), item.World));
      else if (run.IsFinishHeart(item))
        heart = item.World;
      else if (item.Kind == CollectibleKind.Strawberry)
        berries.Add(new Berry(item.Id, item.World));
    }
  }

  public override void Update()
  {
    base.Update();
    RunDefinition run = RunManager.Run;
    if (run == null)
    {
      RemoveSelf();
      return;
    }

    Player player = Scene.Tracker.GetEntity<Player>();
    Array.Fill(targets, null);
    if (Shown && player != null && !player.Dead)
      FillTargets(run, player);

    int shownCount = 0;
    foreach (Target? target in targets)
      if (target.HasValue)
        shownCount++;
    Visible = shownCount > 0;

    Arrow shownBefore = Array.Find(arrows, arrow => arrow.Visible);
    bool paused = SceneAs<Level>().Paused;
    int shownIndex = 0;
    for (int ring = 0; ring < arrows.Length; ring++)
    {
      Arrow arrow = arrows[ring];
      if (targets[ring] is not Target target)
      {
        arrow.Visible = false;
        arrow.Aim = null;
        continue;
      }
      bool snap = EnterArrow(arrow, target, shownBefore, paused);
      float offset = (shownIndex - (shownCount - 1) / 2f) * RingGap;
      shownIndex++;
      Steer(arrow, target, player, offset, snap, paused);
    }

    foreach (Arrow arrow in arrows)
    {
      arrow.Layer.Visible = arrow.Visible;
      arrow.Layer.Origin = arrow.Position.Round() - Vector2.One * arrowHalf;
      arrow.Layer.Alpha = arrow.Alpha < MinAlpha ? 0f : arrow.Alpha;
    }
  }

  private void Steer(
    Arrow arrow,
    Target target,
    Player player,
    float offset,
    bool snap,
    bool paused
  )
  {
    arrow.Color = target.Kind switch
    {
      TargetKind.Strawberry => StrawberryColor,
      TargetKind.SummitGem => SummitGemColor,
      TargetKind.Finish => FinishColor,
      _ => WaypointColor,
    };
    Vector2 center = player.Center - Vector2.UnitY * CenterLift;
    Vector2 toTarget = target.Position - center;
    float fade = Calc.ClampedMap(toTarget.Length(), FadeNear, FadeFar);
    IndicatorLook look = Look;
    arrow.Alpha = fade * look.Alpha;
    if (paused && !snap)
      return;

    float radius = MathHelper.Lerp(RadiusNear, look.RadiusFar, fade) + offset;
    if (snap || Settings.Motion == GoalIndicatorMotion.Instant)
    {
      arrow.Angle = toTarget.Angle();
      arrow.Position = ClampToCamera(center + Calc.AngleToVector(arrow.Angle, radius));
      return;
    }

    IndicatorMotion motion =
      Settings.Motion == GoalIndicatorMotion.Snappy ? SnappyMotion : SmoothMotion;
    float turn = 1f - (float)Math.Pow(0.01, Engine.DeltaTime / motion.TurnTime);
    arrow.Angle = Calc.AngleLerp(arrow.Angle, toTarget.Angle(), turn);
    Vector2 wanted = center + Calc.AngleToVector(arrow.Angle, radius);
    float follow = 1f - (float)Math.Pow(0.01, Engine.DeltaTime / motion.FollowTime);
    arrow.Position = ClampToCamera(Vector2.Lerp(arrow.Position, wanted, follow));
  }

  private Vector2 ClampToCamera(Vector2 position)
  {
    Camera camera = SceneAs<Level>().Camera;
    return position.Clamp(
      camera.Left + EdgeMargin,
      camera.Top + EdgeMargin,
      camera.Right - EdgeMargin,
      camera.Bottom - EdgeMargin
    );
  }

  private void FillTargets(RunDefinition run, Player player)
  {
    RunProgress progress = RunManager.Progress;
    if (progress.Met(run))
    {
      if (FinishTarget(run) is Vector2 finish)
        targets[0] = new Target(finish, TargetKind.Finish);
      return;
    }
    if (Settings.Targets == GoalIndicatorTargets.Nearest)
    {
      targets[0] = Nearest(run, player, progress, null);
      return;
    }
    for (int ring = 0; ring < Rings.Length; ring++)
      targets[ring] = Nearest(run, player, progress, Rings[ring]);
  }

  private Vector2? FinishTarget(RunDefinition run) =>
    run.FinishKind switch
    {
      FinishKind.Heart => heart,
      FinishKind.RoomEnter when run.Finish.IsEmpty => goalRoomCenter,
      _ => run.FinishCenter,
    };

  private Target? Nearest(RunDefinition run, Player player, RunProgress progress, TargetKind? only)
  {
    Vector2 from = player.Center;
    Target? best = null;
    float bestDistance = float.MaxValue;

    void Consider(Vector2 point, TargetKind kind)
    {
      float distance = Vector2.DistanceSquared(from, point);
      if (distance < bestDistance)
      {
        bestDistance = distance;
        best = new Target(point, kind);
      }
    }

    if (only is null or TargetKind.Waypoint)
      for (int i = 0; i < run.Waypoints.Count; i++)
        if (!progress.IsTaken(i))
          Consider(run.Waypoints[i].Position, TargetKind.Waypoint);

    if (only is null or TargetKind.Strawberry)
    {
      carried.Clear();
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

    if (only is null or TargetKind.SummitGem && progress.SummitGems < run.RequiredSummitGems)
    {
      bool[] taken = SceneAs<Level>().Session.SummitGems;
      foreach (Gem gem in gems)
      {
        bool collected = taken.IsInRange(gem.Id) && taken[gem.Id];
        if (!collected)
          Consider(gem.World, TargetKind.SummitGem);
      }
    }

    return best;
  }

  private static bool EnterArrow(Arrow arrow, Target target, Arrow shownBefore, bool paused)
  {
    bool spotChanged = arrow.Aim != target || arrow.PlacedLook != Look;
    bool entering = !arrow.Visible || (paused && spotChanged);
    arrow.Visible = true;
    arrow.Aim = target;
    arrow.PlacedLook = Look;

    if (!entering)
      return false;

    if (paused || shownBefore == null)
      return true;

    arrow.Angle = shownBefore.Angle;
    arrow.Position = shownBefore.Position;
    return false;
  }

  private void DrawArrow(Arrow arrow)
  {
    texture.DrawOutlineCentered(arrow.Position.Round(), arrow.Color, 1f, arrow.Angle);
  }

  public override void Render() { }
}
