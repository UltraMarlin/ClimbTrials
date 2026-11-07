using System;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

// Based on vanilla TouchSwitch.cs
public class TrialWaypoint : Entity
{
  private const string ContainerTexture = "objects/touchswitch/container";
  private const string IconTexture = "objects/touchswitch/icon";
  private const string TouchSound = SFX.game_gen_touchswitch_any;
  private static readonly Color IdleColor = Calc.HexToColor("6ded87");
  private static readonly Color TakenColor = Color.White;
  private const int BufferSize = 32;

  public readonly int Index;
  private readonly WaypointDef definition;
  private readonly Sprite icon = new(GFX.Game, IconTexture);
  private readonly Wiggler wiggler;
  private readonly BloomPoint bloom;
  private readonly FadeBuffer layer;
  private Vector2 pulse = Vector2.One;
  private float timer;
  private float ease;
  private bool taken;

  public TrialWaypoint(int index, WaypointDef definition)
    : base(definition.Position)
  {
    Index = index;
    this.definition = definition;
    Depth = 2000;
    Collider = new Hitbox(16f, 16f, -8f, -8f);
    Add(new PlayerCollider(OnPlayer, null, new Hitbox(30f, 30f, -15f, -15f)));
    Add(icon);
    icon.Visible = false;
    icon.Add("idle", "", 0f, default(int));
    icon.Add("spin", "", 0.1f, new Chooser<string>("spin", 1f), 0, 1, 2, 3, 4, 5);
    icon.CenterOrigin();
    Add(bloom = new BloomPoint(0f, 16f));
    Add(new VertexLight(Color.White, 0.8f, 16, 32));
    Add(
      wiggler = Wiggler.Create(0.5f, 4f, progress => pulse = Vector2.One * (1f + progress * 0.25f))
    );
    Add(
      layer = new FadeBuffer("climbtrials-waypoint", BufferSize, BufferSize, DrawOpaque)
      {
        Origin = Position - Vector2.One * (BufferSize / 2f),
      }
    );
    taken = RunManager.Progress.IsTaken(index);
    if (taken)
    {
      icon.Play("idle");
      icon.Rate = 0.1f;
      ease = 1f;
      bloom.Alpha = 1f;
    }
    else
    {
      icon.Play("spin");
    }
  }

  private void OnPlayer(Player player)
  {
    if (taken || RunManager.Attempt.State != AttemptState.Running)
      return;
    if (!RunManager.Progress.TakeWaypoint(Index))
      return;
    taken = true;
    if (definition.SpawnPoint is Vector2 spawn)
      SceneAs<Level>().Session.RespawnPoint = spawn;
    wiggler.Start();
    icon.Rate = 4f;
    Audio.Play(TouchSound, Position);
    Level level = SceneAs<Level>();
    Calc.PushRandom();
    try
    {
      for (int i = 0; i < 32; i++)
      {
        float angle = Calc.Random.NextAngle();
        level.Particles.Emit(
          TouchSwitch.P_FireWhite,
          Position + Calc.AngleToVector(angle, 6f),
          angle
        );
      }
    }
    finally
    {
      Calc.PopRandom();
    }
  }

  public override void Update()
  {
    if (RunManager.Run == null)
    {
      RemoveSelf();
      return;
    }
    ease = Calc.Approach(ease, taken ? 1f : 0f, Engine.DeltaTime * 2f);
    timer += Engine.DeltaTime * 8f;
    bloom.Alpha = ease;
    float swing = ((float)Math.Sin(timer) + 1f) / 2f;
    layer.Alpha = 0.5f + swing * (1f - ease) * 0.5f + 0.5f * ease;
    if (taken && icon.Rate > 0.1f)
    {
      icon.Rate -= 2f * Engine.DeltaTime;
      if (icon.Rate <= 0.1f)
      {
        icon.Rate = 0.1f;
        icon.Play("idle");
      }
    }
    base.Update();
  }

  public static void Draw(
    Vector2 position,
    MTexture iconFrame,
    Color color,
    float shadowAlpha,
    Vector2 borderScale
  )
  {
    MTexture border = GFX.Game[ContainerTexture];
    if (shadowAlpha > 0f)
      border.DrawCentered(position + new Vector2(0f, -1f), Color.Black * shadowAlpha);
    border.DrawCentered(position, color, borderScale);
    iconFrame.DrawCentered(position, color);
  }

  public static void DrawPreview(Vector2 position, float alpha) =>
    Draw(
      position,
      GFX.Game.GetAtlasSubtextures(IconTexture)[0],
      IdleColor * alpha,
      alpha,
      Vector2.One
    );

  private void DrawOpaque() =>
    Draw(Position, icon.Texture, Color.Lerp(IdleColor, TakenColor, ease), 0f, pulse);

  public override void Render()
  {
    GFX.Game[ContainerTexture].DrawCentered(Position + new Vector2(0f, -1f), Color.Black);
    base.Render();
  }
}
