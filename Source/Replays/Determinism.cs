using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class Determinism
{
  public const ulong FrameBase = 1UL << 40;

  private enum SeedingState
  {
    Off,
    WaitingForLoader,
    SeedingFrames,
  }

  private static SeedingState state;
  private static LevelLoader seededLoader;
  private static bool loadingInline;
  private static Random random;
  private static Random pushedRandom;
  private static ulong frame;
  private static ulong savedFrameCounter;

  public static void Load()
  {
    On.Monocle.Engine.Update += OnEngineUpdate;
    On.Celeste.RunThread.Start += OnRunThreadStart;
  }

  public static void Unload()
  {
    On.Monocle.Engine.Update -= OnEngineUpdate;
    On.Celeste.RunThread.Start -= OnRunThreadStart;
  }

  public static LevelLoader CreateSeededLoader(int loadSeed, Session session, Vector2 spawn)
  {
    random = new Random(loadSeed);
    frame = FrameBase;
    Calc.PushRandom(new Random(loadSeed));
    loadingInline = true;
    try
    {
      seededLoader = new LevelLoader(session, spawn);
    }
    finally
    {
      loadingInline = false;
      Calc.PopRandom();
    }
    state = SeedingState.WaitingForLoader;
    return seededLoader;
  }

  public static void Reseed(int runSeed)
  {
    random = new Random(runSeed);
  }

  public static void StopSeeding()
  {
    state = SeedingState.Off;
    seededLoader = null;
    if (pushedRandom == null)
      return;

    if (Calc.Random == pushedRandom)
      Calc.PopRandom();
    pushedRandom = null;
    Engine.FrameCounter = savedFrameCounter + 1;
  }

  private static void OnEngineUpdate(
    On.Monocle.Engine.orig_Update orig,
    Engine self,
    GameTime gameTime
  )
  {
    if (state == SeedingState.WaitingForLoader && Engine.Scene == seededLoader)
    {
      state = SeedingState.SeedingFrames;
      seededLoader = null;
    }
    if (state == SeedingState.SeedingFrames)
    {
      pushedRandom = random;
      Calc.PushRandom(pushedRandom);
      savedFrameCounter = Engine.FrameCounter;
      Engine.FrameCounter = frame;
    }

    orig(self, gameTime);

    if (pushedRandom == null)
      return;
    if (Calc.Random == pushedRandom)
      Calc.PopRandom();
    pushedRandom = null;
    frame = Engine.FrameCounter;
    Engine.FrameCounter = savedFrameCounter + 1;
  }

  private static void OnRunThreadStart(
    On.Celeste.RunThread.orig_Start orig,
    Action method,
    string name,
    bool highPriority
  )
  {
    if (!loadingInline)
    {
      orig(method, name, highPriority);
      return;
    }
    RunThread.RunThreadWithLogging(method);
    RunThread.Current = null;
  }
}
