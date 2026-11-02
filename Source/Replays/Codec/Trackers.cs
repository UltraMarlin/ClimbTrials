using System;
using System.Numerics;

namespace Celeste.Mod.ClimbTrials.Replays.Codec;

internal readonly record struct Animation(uint Id, uint AnimationFrame, int Timer);

internal sealed class Trackers
{
  private const int AnimationUnitsPerUpdate = 10;
  public const int MissAgeCap = 4;
  private const int ScaleOne = 960;
  private const int ScaleSixteenth = 60;
  private const int ScaleRecoveryPerUpdate = 28;
  private static readonly int[] RecordedResetScales = Array.ConvertAll(
    [576, 1344, 480, 1440, 768, 1152, 1200, 672],
    ApproachOne
  );
  private static readonly int RecordedJumpScale = RecordedResetScales[0];

  public Animation ExpectedAnimation;
  public int ExpectedScale = ScaleOne;
  private readonly int[] lastMiss;

  public Trackers(int fieldCount)
  {
    lastMiss = new int[fieldCount];
    Array.Fill(lastMiss, -1);
  }

  public int MissAge(int field, int frame) => MissAgeAfter(frame - lastMiss[field]);

  public static int MissAgeAfter(int framesSinceMiss) =>
    Math.Min(32 - BitOperations.LeadingZeroCount((uint)framesSinceMiss), MissAgeCap);

  public void Advance(FrameTable table, int frame, bool[] missed)
  {
    for (int field = 0; field < missed.Length; field++)
      if (missed[field])
        lastMiss[field] = frame;
    ExpectedAnimation = NextAnimation(table, frame, ExpectedAnimation);
    ExpectedScale = NextScale(table, frame, ExpectedScale);
  }

  private static Animation NextAnimation(FrameTable table, int frame, Animation expected)
  {
    uint id = table.Get(Column.AnimId, frame);
    uint animationFrame = table.Get(Column.AnimFrame, frame);
    bool followed = id == expected.Id && animationFrame == expected.AnimationFrame;
    Animation recorded = new(id, animationFrame, followed ? expected.Timer : 0);
    return table.PlayerUpdated(frame) ? StepSpriteClock(recorded) : recorded;
  }

  private static Animation StepSpriteClock(Animation animation)
  {
    PlayerAnimation clock = PlayerAnimations.Find((int)animation.Id);
    if (clock == null || !clock.StepsOnItsOwn)
      return animation;

    int timer = animation.Timer + AnimationUnitsPerUpdate;
    if (timer < clock.DelayUnits)
      return animation with { Timer = timer };

    timer -= clock.DelayUnits;
    uint nextAnimationFrame = animation.AnimationFrame + 1;
    if (nextAnimationFrame < clock.FrameCount)
      return new(animation.Id, nextAnimationFrame, timer);

    if (clock.End == AnimationEnd.Loop)
      return new(animation.Id, 0, timer);

    if (clock.End == AnimationEnd.Stop)
      return new(0, (uint)clock.FrameCount - 1, 0);

    return new((uint)PlayerAnimations.IdOf(clock.NextName), 0, 0);
  }

  private static int NextScale(FrameTable table, int frame, int expected)
  {
    int recorded = table.GetSigned(Column.ScaleX, frame);
    int scale;

    if (table.Get(Column.HasPlayer, frame) == 0)
      scale = ScaleOne;
    else if (recorded == ToSixteenths(expected))
      scale = expected;
    else
      scale = RestartedScale(recorded);

    if (!table.PlayerUpdated(frame))
      return scale;

    bool jumped = (table.Get(Column.Pressed, frame) & (uint)Actions.Jump) != 0;
    return jumped ? RecordedJumpScale : ApproachOne(scale);
  }

  private static int ApproachOne(int scale) =>
    scale > ScaleOne
      ? Math.Max(scale - ScaleRecoveryPerUpdate, ScaleOne)
      : Math.Min(scale + ScaleRecoveryPerUpdate, ScaleOne);

  public static int ToSixteenths(int scale) =>
    (int)Math.Floor((scale + ScaleSixteenth / 2) / (double)ScaleSixteenth);

  private static int RestartedScale(int recorded)
  {
    foreach (int resetScale in RecordedResetScales)
      if (ToSixteenths(resetScale) == recorded)
        return resetScale;

    return recorded * ScaleSixteenth;
  }
}
