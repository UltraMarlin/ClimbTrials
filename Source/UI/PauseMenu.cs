using System.Linq;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class PauseMenu
{
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

    TextMenu.Item restart = new TextMenu.Button(Dialog.Clean("climbtrials_pause_restart")).Pressed(
      RestartTrial
    );
    TextMenu.Item back = new TextMenu.Button(Dialog.Clean("climbtrials_pause_return")).Pressed(
      RunManager.LeaveToTrials
    );

    int restartIndex = Replace(menu, "menu_pause_restartarea", restart);
    if (Replace(menu, "menu_pause_return", back) < 0)
      menu.Insert(restartIndex + 1, back);

    if (Replayer.Silenced)
      LockRows(
        menu,
        Dialog.Clean("menu_pause_resume"),
        Dialog.Clean("menu_pause_retry"),
        Dialog.Clean("menu_pause_skip_cutscene")
      );
    else if (RunManager.Replaying)
      LockRows(menu, Dialog.Clean("menu_pause_resume"), Dialog.Clean("climbtrials_pause_return"));
    else if (
      RunManager.Attempt.State == AttemptState.Finished
      && Find(menu, "menu_pause_retry") is TextMenu.Button retry
    )
      Lock(retry);
  }

  private static void LockRows(TextMenu menu, params string[] exemptLabels)
  {
    foreach (TextMenu.Button button in menu.Items.OfType<TextMenu.Button>())
      if (!exemptLabels.Contains(button.Label))
        Lock(button);
  }

  private static void Lock(TextMenu.Button button)
  {
    button.OnPressed = null;
    if (!Replayer.Silenced)
      button.Disabled = true;
  }

  private static TextMenu.Button Find(TextMenu menu, string vanillaKey)
  {
    string label = Dialog.Clean(vanillaKey);
    return menu.Items.OfType<TextMenu.Button>().FirstOrDefault(b => b.Label == label);
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
        .FirstOrDefault(b => b.Label == Dialog.Clean("menu_restart_continue"));
      if (yes == null)
        return;

      int index = menu.Items.IndexOf(yes);
      menu.Remove(yes);
      TextMenu.Button restart = new(yes.Label);
      restart.Pressed(RestartTrial);
      if (Replayer.Silenced)
        Lock(restart);
      menu.Insert(index, restart);
    };
  }
}
