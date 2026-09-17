using Celeste.Mod.ClimbTrials.Entities;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.ClimbTrials.Runs;

public static class HeartGoal
{
  private const string BounceSound = SFX.game_gen_crystalheart_bounce;
  private static HeartGem pendingCut;
  private static bool poemPending;

  public static void Load()
  {
    On.Celeste.HeartGem.Collect += OnCollect;
    On.Celeste.HeartGem.IsCompleteArea += OnIsCompleteArea;
    On.Celeste.HeartGem.RegisterAsCollected += OnRegisterAsCollected;
  }

  public static void Unload()
  {
    On.Celeste.HeartGem.Collect -= OnCollect;
    On.Celeste.HeartGem.IsCompleteArea -= OnIsCompleteArea;
    On.Celeste.HeartGem.RegisterAsCollected -= OnRegisterAsCollected;
  }

  public static void Reset()
  {
    pendingCut = null;
    poemPending = false;
  }

  private static bool IsGoalHeart(HeartGem heart) =>
    RunManager.Run is { FinishKind: FinishKind.Heart } run
    && RunManager.AttemptInProgress
    && !heart.IsFake
    && heart.Scene is Level level
    && level.Session.Level == run.FinishRoom;

  private static void OnCollect(On.Celeste.HeartGem.orig_Collect orig, HeartGem self, Player player)
  {
    if (!IsGoalHeart(self))
    {
      orig(self, player);
      return;
    }
    if (!RunManager.Progress.Met(RunManager.Run))
    {
      if (!player.DashAttacking)
        return;
      player.PointBounce(self.Center);
      Audio.Play(BounceSound, self.Position);
      Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
      return;
    }

    Audio.SetMusic(null);
    Audio.SetAmbience(null);
    orig(self, player);
  }

  private static bool OnIsCompleteArea(
    On.Celeste.HeartGem.orig_IsCompleteArea orig,
    HeartGem self,
    bool value
  ) => !IsGoalHeart(self) && orig(self, value);

  private static void OnRegisterAsCollected(
    On.Celeste.HeartGem.orig_RegisterAsCollected orig,
    HeartGem self,
    Level level,
    string poemID
  )
  {
    orig(self, level, poemID);
    if (!IsGoalHeart(self) || !RunManager.Finish())
      return;

    Player player = level.Tracker.GetEntity<Player>();
    if (player == null)
      return;

    FinishSequence.Confetti(level, player);
    FinishSequence.Begin(level, player);
    pendingCut = self;
  }

  public static void AfterUpdate(Level level)
  {
    if (pendingCut != null)
    {
      DynamicData.For(pendingCut).Get<SoundEmitter>("sfx")?.Source.Param("end", 1f);
      pendingCut.RemoveSelf();
      pendingCut = null;
      poemPending = true;
      return;
    }
    if (!poemPending)
      return;
    level.Entities.FindFirst<Poem>()?.RemoveSelf();
    poemPending = false;
  }
}
