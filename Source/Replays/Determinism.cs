using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class Determinism
{
  public const ulong FrameBase = 1UL << 40;

  private static bool pending;
  private static bool active;
  private static bool pushed;
  private static bool loadingInline;
  private static Random random;
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

  public static LevelLoader CreateLoader(int loadSeed, Session session, Vector2 spawn)
  {
    Calc.PushRandom(new Random(loadSeed));
    loadingInline = true;
    try
    {
      return new LevelLoader(session, spawn);
    }
    finally
    {
      loadingInline = false;
      Calc.PopRandom();
    }
  }

  public static void Begin(int loadSeed)
  {
    random = new Random(loadSeed);
    frame = FrameBase;
    pending = true;
  }

  public static void Reseed(int runSeed)
  {
    random = new Random(runSeed);
  }

  public static void End()
  {
    pending = false;
    active = false;
  }

  private static void OnEngineUpdate(
    On.Monocle.Engine.orig_Update orig,
    Engine self,
    GameTime gameTime
  )
  {
    if (pending)
    {
      pending = false;
      active = true;
    }
    if (active)
    {
      Calc.PushRandom(random);
      savedFrameCounter = Engine.FrameCounter;
      Engine.FrameCounter = frame;
      pushed = true;
    }

    orig(self, gameTime);

    if (!pushed)
      return;
    pushed = false;
    Calc.PopRandom();
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
