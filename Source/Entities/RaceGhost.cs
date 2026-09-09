using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class RaceGhost : Entity
{
  public const float MaxAlpha = 0.6f;
  public const float MinAlpha = 0.1f;
  public const float DuckNear = 14f;
  public const float DuckFar = 32f;
  public const float HoldSeconds = 1.5f;
  public const float FadeSeconds = 1f;
  public const float TagLift = 20f;
  public const float TagScale = 0.5f;
  private const int HairSettleSteps = 10;
  private const int BufferSize = 96;
  private static readonly Vector2 BufferAnchor = new(48f, 72f);

  public readonly string Name;
  public float Alpha { get; private set; }

  private readonly Replay replay;
  private readonly int[] shownFrames;
  private readonly long ticksPerFrame;
  private readonly PlayerSprite sprite;
  private readonly PlayerHair hair;
  private RaceGhostTag nameTag;
  private VirtualRenderTarget buffer;
  private long ticks;
  private float fadeAlpha = 1f;
  private bool hairSettled;

  public RaceGhost(Replay replay, PlayerSpriteMode mode)
  {
    this.replay = replay;
    Name = replay.PlayerName;
    ticksPerFrame = Attempt.SecondsToTicks(replay.RawDeltaTime);
    shownFrames = ticksPerFrame > 0 ? LevelUpdatedFrames(replay.Frames) : [];
    Tag = Tags.Persistent;
    Depth = Depths.Player + 1;
    sprite = new PlayerSprite(mode);
    Add(hair = new PlayerHair(sprite));
    Add(sprite);
    sprite.Active = false;
    hair.Active = false;
    sprite.Play(FirstKnownAnimation());
    Add(new BeforeRenderHook(BeforeRender));
    Visible = shownFrames.Length > 0;
  }

  public override void Added(Scene scene)
  {
    base.Added(scene);
    nameTag = new RaceGhostTag(this);
    scene.Add(nameTag);
  }

  public override void Removed(Scene scene)
  {
    base.Removed(scene);
    nameTag?.RemoveSelf();
    DisposeBuffer();
  }

  public override void SceneEnd(Scene scene)
  {
    base.SceneEnd(scene);
    DisposeBuffer();
  }

  private void DisposeBuffer()
  {
    buffer?.Dispose();
    buffer = null;
  }

  private Vector2 BufferOrigin => (Position - BufferAnchor).Floor();

  private void BeforeRender()
  {
    if (!Visible)
      return;

    buffer ??= VirtualContent.CreateRenderTarget("climbtrials-ghost", BufferSize, BufferSize);
    Vector2 origin = BufferOrigin;
    Engine.Graphics.GraphicsDevice.SetRenderTarget(buffer);
    Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
    Draw.SpriteBatch.Begin(
      SpriteSortMode.Deferred,
      BlendState.AlphaBlend,
      SamplerState.PointClamp,
      DepthStencilState.None,
      RasterizerState.CullNone,
      null,
      Matrix.CreateTranslation(-origin.X, -origin.Y, 0f)
    );
    sprite.Color = Color.White;
    hair.Alpha = 1f;
    hair.Render();
    sprite.Render();
    Draw.SpriteBatch.End();
  }

  public void Sync(long elapsedTicks)
  {
    ticks = elapsedTicks;
    Apply();
  }

  public void Advance(float rawDeltaSeconds)
  {
    ticks += Attempt.SecondsToTicks(rawDeltaSeconds);
    Apply();
  }

  private void Apply()
  {
    if (shownFrames.Length == 0)
      return;
    int last = shownFrames.Length - 1;
    int index = (int)Math.Clamp(ticks / ticksPerFrame - 1, 0, last);
    long pastEnd = ticks - (last + 1) * ticksPerFrame - Attempt.SecondsToTicks(HoldSeconds);
    fadeAlpha = 1f - Calc.Clamp((float)pastEnd / Attempt.SecondsToTicks(FadeSeconds), 0f, 1f);

    InputFrame frame = replay.Frames[shownFrames[index]];
    Visible = frame.HasPlayer && fadeAlpha > 0f;
    if (!frame.HasPlayer)
    {
      hairSettled = false;
      return;
    }
    Position = frame.Position;
    float facing = frame.FacingLeft ? -1f : 1f;
    sprite.Scale = new Vector2(
      frame.ScaleX / ReplayColumns.ScaleUnit * facing,
      frame.ScaleY / ReplayColumns.ScaleUnit
    );
    string animation = replay.AnimationAt(frame.AnimId);
    if (animation != null && animation != sprite.CurrentAnimationID && sprite.Has(animation))
      sprite.Play(animation, restart: true);
    sprite.SetAnimationFrame(frame.AnimFrame);
    hair.Facing = (Facings)(int)facing;
    hair.Color = HairColorFor(frame.Dashes);
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

  public override void Render()
  {
    Player player = Scene.Tracker.GetEntity<Player>();
    float distance = player == null ? DuckFar : (player.Position - Position).Length();
    Alpha = Calc.ClampedMap(distance, DuckNear, DuckFar, MinAlpha, MaxAlpha) * fadeAlpha;
    if (buffer != null)
      Draw.SpriteBatch.Draw((RenderTarget2D)buffer, BufferOrigin, Color.White * Alpha);
  }

  private string FirstKnownAnimation()
  {
    string first =
      shownFrames.Length > 0 ? replay.AnimationAt(replay.Frames[shownFrames[0]].AnimId) : null;
    return first != null && sprite.Has(first) ? first : PlayerSprite.Idle;
  }

  private Color HairColorFor(int dashes)
  {
    bool badeline = sprite.Mode == PlayerSpriteMode.MadelineAsBadeline;
    if (dashes == 0)
      return badeline ? Player.UsedBadelineHairColor : Player.UsedHairColor;
    if (dashes == 2)
      return badeline ? Player.TwoDashesBadelineHairColor : Player.TwoDashesHairColor;
    return badeline ? Player.NormalBadelineHairColor : Player.NormalHairColor;
  }

  private static int[] LevelUpdatedFrames(List<InputFrame> frames)
  {
    List<int> shown = new(frames.Count);
    for (int i = 0; i < frames.Count; i++)
      if (frames[i].LevelUpdated)
        shown.Add(i);
    return shown.ToArray();
  }
}
