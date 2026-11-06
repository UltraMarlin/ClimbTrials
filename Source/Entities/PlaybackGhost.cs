using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

public class PlaybackGhost : Entity
{
  public const float StrongFarAlpha = 0.8f;
  public const float StrongNearAlpha = 0.4f;
  public const float StrongDuckNear = 14f;
  public const float StrongDuckFar = 24f;
  public const float SubtleFarAlpha = 0.6f;
  public const float SubtleNearAlpha = 0.12f;
  public const float SubtleDuckNear = 14f;
  public const float SubtleDuckFar = 32f;

  public const float ComparisonFarAlpha = 0.75f;
  public const float ComparisonNearAlpha = 0.6f;
  public const float ComparisonDuckNear = 14f;
  public const float ComparisonDuckFar = 24f;
  private const float RaceTagAlpha = 0.4f;
  private const float ComparisonOutlineDarken = 0.8f;

  public static readonly Color RaceColor = Calc.HexToColor("364a59");

  public const float HoldSeconds = 1.5f;
  public const float FadeSeconds = 1f;
  public const float TagLift = 20f;
  public const float TagScale = 0.5f;
  private const int HairSettleSteps = 10;
  private const int BufferSize = 96;
  private static readonly Vector2 BufferAnchor = new(48f, 72f);

  public readonly Replay Replay;
  public readonly string Name;
  public readonly Color TagColor;
  public readonly Color? TagOutline;
  private readonly bool comparison;
  private float Alpha { get; set; }

  private readonly Color color;
  private readonly int[] shownFrames;
  private readonly PlayerSprite sprite;
  private readonly PlayerHair hair;
  private readonly FadeBuffer layer;
  private long ticks;
  private float fadeAlpha = 1f;
  private bool hairSettled;
  private int shownIndex = -1;
  private int slashStart = -1;
  private Vector2 slashOrigin;
  private Vector2 slashDirection;

  public PlaybackGhost(Replay replay, Color color, Color tagColor, bool comparison)
  {
    this.comparison = comparison;
    Replay = replay;
    this.color = color;
    TagColor = tagColor;
    Name = Identity.NameOf(replay);
    shownFrames = replay.ShownFrames;
    sprite = new PlayerSprite(comparison ? PlayerSpriteMode.Madeline : PlayerSpriteMode.Playback);
    Add(hair = new PlayerHair(sprite));
    Add(sprite);
    sprite.Visible = false;
    hair.Visible = false;
    sprite.Play(FirstKnownAnimation());
    Add(layer = new FadeBuffer("climbtrials-ghost", BufferSize, BufferSize, DrawOpaque));
    if (comparison)
    {
      Color outline = Color.Lerp(color, Color.Black, ComparisonOutlineDarken);
      hair.Border = Color.Transparent;
      layer.SilhouetteFill = color;
      layer.SilhouetteOutline = outline;
      TagOutline = outline;
    }
    Visible = shownFrames.Length > 0;
  }

  public void Sync(long elapsedTicks)
  {
    ticks = elapsedTicks;
    Apply();
  }

  public void Capture() => layer.Capture();

  public void Dispose() => Remove(layer);

  private void Apply()
  {
    if (shownFrames.Length == 0)
      return;

    int last = shownFrames.Length - 1;
    int index = (int)Math.Clamp(Replay.ShownFrameAt(ticks), 0, last);
    for (int i = shownIndex + 1; i <= index; i++)
      DetectDash(i);
    shownIndex = index;
    long pastEnd = ticks - Replay.TicksOfShownFrame(last) - Attempt.SecondsToTicks(HoldSeconds);
    fadeAlpha = 1f - Calc.Clamp((float)pastEnd / Attempt.SecondsToTicks(FadeSeconds), 0f, 1f);

    InputFrame frame = Replay.Frames[shownFrames[index]];
    Visible = frame.HasPlayer && fadeAlpha > 0f;
    if (!frame.HasPlayer)
    {
      hairSettled = false;
      slashStart = -1;
      return;
    }

    Position = frame.Position;
    layer.Origin = Position - BufferAnchor;
    float facing = frame.FacingLeft ? -1f : 1f;
    sprite.Scale = new Vector2(
      frame.ScaleX / InputFrame.ScaleUnit * facing,
      frame.ScaleY / InputFrame.ScaleUnit
    );
    string animation = Replay.AnimationAt(frame.AnimId);
    if (animation != null && animation != sprite.CurrentAnimationID && sprite.Has(animation))
      sprite.Play(animation);
    sprite.SetAnimationFrame(frame.AnimFrame);
    hair.Facing = (Facings)(int)facing;

    if (frame.State == Player.StRedDash)
      sprite.HairCount = 1;
    else if (frame.State != Player.StStarFly)
      sprite.HairCount = frame.Dashes > 1 ? 5 : 4;

    hair.AfterUpdate();
    if (hairSettled)
      return;

    hairSettled = true;
    for (int i = 0; i < HairSettleSteps; i++)
      hair.AfterUpdate();
  }

  private void DetectDash(int shown)
  {
    Vector2 SlashOffset = new(0f, -6f);
    if (shown < 1 || shown + 1 >= shownFrames.Length)
      return;

    InputFrame current = Replay.Frames[shownFrames[shown]];
    InputFrame before = Replay.Frames[shownFrames[shown - 1]];
    if (current.State != Player.StDash || before.State == Player.StDash)
      return;

    byte direction = Replay.Frames[shownFrames[shown + 1]].DashDir;
    if (direction == 0)
      return;

    slashStart = shown;
    slashOrigin = current.Position + SlashOffset;
    slashDirection = InputFrame.DashDirFromByte(direction);
  }

  private static bool Subtle =>
    ClimbTrialsModule.Settings.GhostVisibility == GhostVisibility.Subtle;

  private float FarAlpha =>
    comparison ? ComparisonFarAlpha
    : Subtle ? SubtleFarAlpha
    : StrongFarAlpha;
  private float NearAlpha =>
    comparison ? ComparisonNearAlpha
    : Subtle ? SubtleNearAlpha
    : StrongNearAlpha;
  private float DuckNear =>
    comparison ? ComparisonDuckNear
    : Subtle ? SubtleDuckNear
    : StrongDuckNear;
  private float DuckFar =>
    comparison ? ComparisonDuckFar
    : Subtle ? SubtleDuckFar
    : StrongDuckFar;

  public float TagAlpha => comparison ? Alpha : Alpha * Alpha * RaceTagAlpha;

  private void DrawOpaque()
  {
    sprite.Color = color;
    hair.Color = color;
    hair.Render();
    sprite.Render();
  }

  private void DrawSlash()
  {
    const int SlashFrameTicks = 6;
    if (slashStart < 0)
      return;

    List<MTexture> frames = GFX.Game.GetAtlasSubtextures("effects/slash/");
    int elapsed = shownIndex - slashStart;
    if (elapsed < 0 || elapsed >= 4 * SlashFrameTicks)
    {
      slashStart = -1;
      return;
    }

    Vector2 at = slashOrigin + slashDirection * (8f * elapsed * Replay.RawDeltaTime);
    float angle = slashDirection.Angle();
    float rotation = Math.Abs(angle - MathF.PI) > 0.01f ? angle : 0f;
    frames[elapsed / SlashFrameTicks].DrawCentered(at, Color.White * Alpha, 1f, rotation);
  }

  public void Render(Player player)
  {
    float distance = player == null ? DuckFar : (player.Position - Position).Length();
    Alpha = Calc.ClampedMap(distance, DuckNear, DuckFar, NearAlpha, FarAlpha) * fadeAlpha;
    layer.Alpha = Alpha;
    base.Render();
    DrawSlash();
  }

  private string FirstKnownAnimation()
  {
    string first =
      shownFrames.Length > 0 ? Replay.AnimationAt(Replay.Frames[shownFrames[0]].AnimId) : null;
    return first != null && sprite.Has(first) ? first : PlayerSprite.Idle;
  }
}
