using System;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class PlayerAnimations
{
  private static readonly PlayerAnimation[] Table =
  [
    new("idle_carry"),
    new("runSlow_carry"),
    new("jumpSlow_carry"),
    new("fallSlow_carry"),
    new("pickUp"),
    new("throw"),
    new("idle", 60, 9, AnimationEnd.PlayNext, "idle"),
    new("idleA"),
    new("idleB"),
    new("idleC"),
    new("lookUp", 60, 6, AnimationEnd.Stop),
    new("walk", 36, 12, AnimationEnd.Loop),
    new("push", 60, 16, AnimationEnd.Loop),
    new("runSlow", 42, 12, AnimationEnd.PlayNext, "runFast"),
    new("runFast", 30, 12, AnimationEnd.Loop),
    new("runStumble"),
    new("runWind", 57, 12, AnimationEnd.Loop),
    new("dash", 54, 4, AnimationEnd.Loop),
    new("dreamDashIn", 24, 4, AnimationEnd.PlayNext, "dreamDashLoop"),
    new("dreamDashLoop", 18, 13, AnimationEnd.Loop),
    new("dreamDashOut", 24, 4, AnimationEnd.Stop),
    new("slide"),
    new("jumpSlow", 60, 2, AnimationEnd.Loop),
    new("jumpFast", 60, 2, AnimationEnd.Loop),
    new("fallSlow", 60, 2, AnimationEnd.Stop),
    new("fallFast", 60, 2, AnimationEnd.Stop),
    new("tired"),
    new("tiredStill"),
    new("wallslide"),
    new("climbLookBackStart", 48, 3, AnimationEnd.PlayNext, "climbLookBack"),
    new("climbLookBack"),
    new("climbUp", 24, 6, AnimationEnd.Loop),
    new("climbPush"),
    new("climbPull"),
    new("duck"),
    new("fallPose"),
    new("edge", 150, 14, AnimationEnd.Loop),
    new("edgeBack", 150, 14, AnimationEnd.Loop),
    new("faint"),
    new("fainted"),
    new("flip", 24, 8, AnimationEnd.PlayNext, "runFast"),
    new("skid", 24, 1, AnimationEnd.Loop),
    new("dangling", 66, 10, AnimationEnd.Loop),
    new("deadside"),
    new("deadup"),
    new("deaddown"),
    new("swimIdle", 48, 6, AnimationEnd.Loop),
    new("swimUp", 48, 6, AnimationEnd.Loop),
    new("swimDown", 48, 6, AnimationEnd.Loop),
    new("startStarFly", 48, 4, AnimationEnd.PlayNext, "starFly"),
    new("starFly", 48, 1, AnimationEnd.Loop),
    new("bubble"),
    new("fall"),
    new("bigFall"),
    new("bigFallRecover"),
    new("sleep"),
    new("bagdown"),
    new("asleep"),
    new("wakeUp"),
    new("halfWakeUp"),
    new("spin"),
    new("shaking"),
    new("hug"),
    new("starMorph"),
    new("starMorphIdle"),
    new("carryTheoWalk"),
    new("carryTheoCollapse"),
    new("tentacle_grab"),
    new("tentacle_grabbed"),
    new("tentacle_pull"),
    new("tentacle_dangling"),
    new("sitDown"),
    new("launch", 36, 8, AnimationEnd.Loop),
    new("launchRecover"),
  ];

  public static readonly string[] Names = Array.ConvertAll(Table, animation => animation.Name);

  public static int IdOf(string name) => Array.IndexOf(Names, name) + 1;

  public static PlayerAnimation Find(int id) =>
    id >= 1 && id <= Table.Length ? Table[id - 1] : null;
}

public enum AnimationEnd
{
  Loop,
  Stop,
  PlayNext,
}

public sealed record PlayerAnimation(
  string Name,
  int DelayUnits = 0,
  int FrameCount = 1,
  AnimationEnd End = AnimationEnd.Loop,
  string NextName = null
)
{
  public bool StepsOnItsOwn => DelayUnits > 0;
}
