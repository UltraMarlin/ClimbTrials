using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.UI;

public static class MainMenuEntry
{
  public static void Load() => Everest.Events.MainMenu.OnCreateButtons += OnCreateButtons;

  public static void Unload() => Everest.Events.MainMenu.OnCreateButtons -= OnCreateButtons;

  private static void OnCreateButtons(OuiMainMenu menu, List<MenuButton> buttons)
  {
    buttons.Insert(
      1,
      new MainMenuSmallButton(
        "climbtrials_menu",
        "menu/options",
        menu,
        Vector2.Zero,
        Vector2.Zero,
        () => menu.Overworld.Goto<OuiTrials>()
      )
    );
  }
}
