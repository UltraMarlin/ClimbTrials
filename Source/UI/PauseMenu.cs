using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;

namespace Celeste.Mod.ClimbTrials.UI;

public static class PauseMenu
{
  private static readonly Dictionary<TextMenu.Item, PauseRowKind> ownRows = [];

  public static void Load()
  {
    Everest.Events.Level.OnCreatePauseMenuButtons += OnCreatePauseMenuButtons;
    Everest.Events.Level.OnPause += OnPause;
  }

  public static void Unload()
  {
    Everest.Events.Level.OnCreatePauseMenuButtons -= OnCreatePauseMenuButtons;
    Everest.Events.Level.OnPause -= OnPause;
  }

  private static void OnCreatePauseMenuButtons(Level level, TextMenu menu, bool minimal)
  {
    if (RunManager.Run == null)
      return;
    ownRows.Clear();

    TextMenu.Item restart = new TextMenu.Button(Dialog.Clean("climbtrials_pause_restart")).Pressed(
      RestartTrial
    );
    TextMenu.Item back = new TextMenu.Button(Dialog.Clean("climbtrials_return_to_trials")).Pressed(
      RunManager.LeaveToTrials
    );

    int restartIndex = Replace(menu, "menu_pause_restartarea", restart);
    if (Replace(menu, "menu_pause_return", back) < 0)
      menu.Insert(restartIndex + 1, back);
    ownRows[restart] = PauseRowKind.RestartTrial;
    ownRows[back] = PauseRowKind.ReturnToTrials;

    TextMenu.Item race = BuildRaceRow();
    menu.Insert(menu.Items.IndexOf(back), race);
    ownRows[race] = PauseRowKind.Race;

    DimButton leaderboard = AddLeaderboardRow(level, menu, menu.Items.IndexOf(back));
    ownRows[leaderboard] = PauseRowKind.Leaderboard;

    if (Replayer.IsReplayingInputs)
      LockRowsExcept(
        menu,
        Dialog.Clean("menu_pause_resume"),
        Dialog.Clean("menu_pause_retry"),
        Dialog.Clean("menu_pause_skip_cutscene")
      );
    else if (RunManager.Replaying)
      LockRowsExcept(
        menu,
        Dialog.Clean("menu_pause_resume"),
        Dialog.Clean("climbtrials_return_to_trials")
      );
    else if (
      RunManager.Attempt.State == AttemptState.Finished
      && Find(menu, "menu_pause_retry") is TextMenu.Button retry
    )
      LockRow(retry);
  }

  public static PauseRowKind? FindOwnRowKind(TextMenu.Item item) =>
    ownRows.TryGetValue(item, out PauseRowKind kind) ? kind : null;

  public static string LabelOf(PauseRowKind kind) =>
    kind switch
    {
      PauseRowKind.RestartTrial => Dialog.Clean("climbtrials_pause_restart"),
      PauseRowKind.ReturnToTrials => Dialog.Clean("climbtrials_return_to_trials"),
      PauseRowKind.Race => Dialog.Clean(
        RunManager.Racing ? "climbtrials_pause_race_off" : "climbtrials_race_pb_ghost"
      ),
      PauseRowKind.Leaderboard => Dialog.Clean("climbtrials_trials_leaderboard"),
      _ => "",
    };

  private static void LockRowsExcept(TextMenu menu, params string[] exemptLabels)
  {
    foreach (TextMenu.Button button in menu.Items.OfType<TextMenu.Button>())
      if (!exemptLabels.Contains(button.Label))
        LockRow(button);
  }

  private static void LockRow(TextMenu.Button button)
  {
    button.OnPressed = null;
    if (!Replayer.IsReplayingInputs)
      button.Disabled = true;
  }

  private static TextMenu.Button Find(TextMenu menu, string vanillaKey)
  {
    string label = Dialog.Clean(vanillaKey);
    return menu.Items.OfType<TextMenu.Button>().FirstOrDefault(button => button.Label == label);
  }

  private static int Replace(TextMenu menu, string vanillaKey, TextMenu.Item replacement)
  {
    TextMenu.Button old = Find(menu, vanillaKey);
    if (old == null)
      return -1;
    int index = menu.Items.IndexOf(old);
    menu.Remove(old);
    menu.Insert(index, replacement);
    return index;
  }

  public static void RestartTrial()
  {
    RunManager.StartRun(RunManager.Run);
  }

  private static DimButton AddLeaderboardRow(Level level, TextMenu menu, int index)
  {
    bool online = !ClimbTrialsModule.Settings.OfflineMode;
    DimButton row = new(
      Dialog.Clean("climbtrials_trials_leaderboard"),
      online,
      () => LevelBoard.OpenFromPause(level, menu)
    );
    menu.Insert(index, row);
    if (!online)
      row.AddDescription(menu, Dialog.Clean("climbtrials_offline_mode"));
    return row;
  }

  private static TextMenu.Button BuildRaceRow()
  {
    if (RunManager.Racing)
    {
      TextMenu.Button stop = new(Dialog.Clean("climbtrials_pause_race_off"));
      stop.Pressed(RaceWithoutGhost);
      return stop;
    }
    TextMenu.Button race = new(Dialog.Clean("climbtrials_race_pb_ghost"));
    race.Pressed(RaceAgainstPb);
    if (!ReplayStore.Exists(RunManager.Trial.Id))
      race.ConfirmSfx = null;
    return race;
  }

  private static void RaceAgainstPb()
  {
    Trial trial = RunManager.Trial;
    if (ReplayStore.TryLoadPb(trial, out Replay replay, out string failureNotice))
    {
      RunManager.StartRace(trial, replay);
      return;
    }
    if (failureNotice != null)
      RunManager.ShowText(failureNotice, warning: true);
  }

  private static void RaceWithoutGhost()
  {
    RunManager.StartTrial(RunManager.Trial);
  }

  private static void OnPause(Level level, int startIndex, bool minimal, bool quickReset)
  {
    if (!quickReset || RunManager.Run == null)
      return;
    level.OnEndOfFrame += () =>
    {
      TextMenu menu = level.Entities.FindFirst<TextMenu>();
      if (menu == null)
        return;

      TextMenu.Header header = menu.Items.OfType<TextMenu.Header>().FirstOrDefault();
      header?.Title = Dialog.Clean("climbtrials_restart_title");

      TextMenu.Button yes = menu
        .Items.OfType<TextMenu.Button>()
        .FirstOrDefault(button => button.Label == Dialog.Clean("menu_restart_continue"));
      if (yes == null)
        return;

      int index = menu.Items.IndexOf(yes);
      menu.Remove(yes);
      TextMenu.Button restart = new(yes.Label);
      restart.Pressed(RestartTrial);
      if (Replayer.IsReplayingInputs)
        LockRow(restart);
      menu.Insert(index, restart);
    };
  }
}
