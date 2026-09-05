using System.Linq;
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
      ReturnToTrials
    );

    int restartIndex = Replace(menu, "menu_pause_restartarea", restart);
    if (Replace(menu, "menu_pause_return", back) >= 0)
      return;
    menu.Insert(restartIndex + 1, back);
  }

  private static int Replace(TextMenu menu, string vanillaKey, TextMenu.Item replacement)
  {
    string vanillaLabel = Dialog.Clean(vanillaKey);
    TextMenu.Button old = menu
      .Items.OfType<TextMenu.Button>()
      .FirstOrDefault(b => b.Label == vanillaLabel);
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

  public static void ReturnToTrials()
  {
    RunManager.LeaveToTrials();
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
      menu.Insert(index, new TextMenu.Button(yes.Label).Pressed(RestartTrial));
    };
  }
}
