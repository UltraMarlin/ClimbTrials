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

  private static bool IsFinishRoom(string room) =>
    RunManager.Run?.FinishKind == FinishKind.RoomEnter && room == RunManager.Run.FinishRoom;

  private static void OnTransitionTo(Level level, LevelData next, Vector2 direction)
  {
    if (IsFinishRoom(next.Name))
      TryToFinish(level);
  }

  private static void TryToFinish(Level level)
  {
    Player player = level.Tracker.GetEntity<Player>();
    if (player == null || !RunManager.Finish())
      return;

    FinishSequence.Confetti(level, player);
    sequencePending = true;
  }

  public static void AfterUpdate(Level level)
  {
    if (!level.Transitioning && IsFinishRoom(level.Session.Level))
      TryToFinish(level);

    if (!sequencePending || level.Transitioning)
      return;

    sequencePending = false;
    Player player = level.Tracker.GetEntity<Player>();
    if (player != null)
      FinishSequence.Begin(level, player);
  }
}
