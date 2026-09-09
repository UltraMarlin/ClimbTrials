using System;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

public class TrialWaypoint : Entity
{
  private const string ContainerTexture = "objects/touchswitch/container";
  private const string IconTexture = "objects/touchswitch/icon";
  private const string TouchSound = "event:/game/general/touchswitch_any";
  private static readonly Color IdleColor = Calc.HexToColor("6ded87");
  private static readonly Color TakenColor = Calc.HexToColor("6ded87") * 0.3f;

  public readonly int Index;
  private readonly WaypointDef definition;
  private readonly Sprite icon = new(GFX.Game, IconTexture);
  private readonly Wiggler wiggler;
  private Vector2 pulse = Vector2.One;
  private float timer;
  private float ease;
  private bool taken;

  // Based on vanilla TouchSwitch.cs
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
    Add(wiggler = Wiggler.Create(0.5f, 4f, v => pulse = Vector2.One * (1f + v * 0.25f)));
    taken = RunManager.Progress.IsTaken(index);
    if (taken)
    {
      icon.Color = TakenColor;
      icon.Play("idle");
      icon.Rate = 0.1f;
      ease = 1f;
    }
    else
    {
      icon.Color = IdleColor;
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
  }

  public override void Update()
  {
    if (RunManager.Run == null)
    {
      RemoveSelf();
      return;
    }
    timer += Engine.DeltaTime * 8f;
    ease = Calc.Approach(ease, taken ? 1f : 0f, Engine.DeltaTime * 2f);
    float pulseAlpha = 0.5f + ((float)Math.Sin(timer) + 1f) / 2f * (1f - ease) * 0.5f + 0.5f * ease;
    icon.Color = Color.Lerp(IdleColor, TakenColor, ease) * pulseAlpha;
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

  public override void Render()
  {
    Draw(Position, icon.Texture, icon.Color, 1f, pulse);
    base.Render();
  }
}
