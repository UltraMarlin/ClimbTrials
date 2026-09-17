using Celeste.Mod.ClimbTrials.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Runs;

public static class RoomGoal
{
  private static bool sequencePending;

  public static void Load()
  {
    Everest.Events.Level.OnTransitionTo += OnTransitionTo;
  }

  public static void Unload()
  {
    Everest.Events.Level.OnTransitionTo -= OnTransitionTo;
  }

  public static void Reset() => sequencePending = false;

  private static void OnTransitionTo(Level level, LevelData next, Vector2 direction)
  {
    if (RunManager.Run is not { FinishKind: FinishKind.RoomEnter } run)
      return;

    if (next.Name != run.FinishRoom)
      return;

    Player player = level.Tracker.GetEntity<Player>();
    if (player == null || !RunManager.Finish())
      return;

    FinishSequence.Confetti(level, player);
    sequencePending = true;
  }

  public static void AfterUpdate(Level level)
  {
    if (!sequencePending || level.Transitioning)
      return;

    sequencePending = false;
    Player player = level.Tracker.GetEntity<Player>();
    if (player != null)
      FinishSequence.Begin(level, player);
  }
}
