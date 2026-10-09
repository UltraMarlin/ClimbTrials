using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class LevelBoard
{
  public static void OpenFromPause(Level level, TextMenu pauseMenu)
  {
    if (RunManager.Trial == null)
      return;
    pauseMenu.Focused = false;
    level.PauseMainMenuOpen = false;
    level.Add(
      new LeaderboardView(
        BoardSubject.Alone(RunManager.Trial),
        progress => pauseMenu.Visible = progress < 1f,
        _ => ClosedOverPause(level, pauseMenu),
        StartGhost,
        options: BoardOptions.InLevel(MenuKeys.Live, comparing: false)
      )
    );
  }

  private static void ClosedOverPause(Level level, TextMenu pauseMenu)
  {
    if (
      RunManager.AttemptInProgress
      && !RunManager.Replaying
      && RunManager.RestartAttempt("climbtrials_notice_leaderboard")
    )
      return;
    pauseMenu.Visible = true;
    pauseMenu.Focused = true;
    level.PauseMainMenuOpen = true;
  }

  public static void StartGhost(Trial trial, Replay replay, bool race)
  {
    if (race)
      RunManager.StartRace(trial, replay);
    else
      RunManager.StartReplay(replay);
  }
}
