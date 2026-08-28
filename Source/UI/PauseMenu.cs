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
    Replace(menu, "menu_pause_restartarea", "climbtrials_pause_restart", RestartTrial);
    Replace(menu, "menu_pause_return", "climbtrials_pause_return", ReturnToTrials);
  }

  private static void Replace(
    TextMenu menu,
    string vanillaKey,
    string ourKey,
    System.Action onPressed
  )
  {
    string vanillaLabel = Dialog.Clean(vanillaKey);
    TextMenu.Button old = menu
      .Items.OfType<TextMenu.Button>()
      .FirstOrDefault(b => b.Label == vanillaLabel);
    if (old == null)
      return;
    int index = menu.Items.IndexOf(old);
    menu.Remove(old);
    menu.Insert(index, new TextMenu.Button(Dialog.Clean(ourKey)).Pressed(onPressed));
  }

  public static void RestartTrial()
  {
    RunManager.StartRun(RunManager.Run);
  }

  public static void ReturnToTrials()
  {
    RunManager.Stop();
    OuiTrials.ReturnPending = true;
    Engine.Scene = new OverworldLoader(Overworld.StartMode.MainMenu);
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
