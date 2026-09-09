using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
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
  private readonly FadeBuffer layer;
  private RaceGhostTag nameTag;
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
    sprite.Visible = false;
    hair.Visible = false;
    sprite.Play(FirstKnownAnimation());
    Add(layer = new FadeBuffer("climbtrials-ghost", BufferSize, BufferSize, DrawOpaque));
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
    layer.Origin = Position - BufferAnchor;
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

  private void DrawOpaque()
  {
    sprite.Color = Color.White;
    hair.Alpha = 1f;
    hair.Render();
    sprite.Render();
  }

  public override void Render()
  {
    Player player = Scene.Tracker.GetEntity<Player>();
    float distance = player == null ? DuckFar : (player.Position - Position).Length();
    Alpha = Calc.ClampedMap(distance, DuckNear, DuckFar, MinAlpha, MaxAlpha) * fadeAlpha;
    layer.Alpha = Alpha;
    base.Render();
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
