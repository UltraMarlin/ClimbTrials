using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class MainMenuEntry
{
  public static void Load() => Everest.Events.MainMenu.OnCreateButtons += OnCreateButtons;

  public static void Unload() => Everest.Events.MainMenu.OnCreateButtons -= OnCreateButtons;

  public static Scene RebuildOverworld()
  {
    return new OverworldLoader(OuiTrials.ReturnStartMode);
  }

  private static void OnCreateButtons(OuiMainMenu menu, List<MenuButton> buttons)
  {
    buttons.Insert(
      1,
      new MainMenuSmallButton(
        "climbtrials_menu",
        "ClimbTrials/Time_Icon",
        menu,
        Vector2.Zero,
        Vector2.Zero,
        () =>
        {
          Audio.Play(SFX.ui_main_button_select);
          Audio.Play(SFX.ui_main_whoosh_large_in);
          menu.Overworld.Goto<OuiCampaigns>();
        }
      )
    );
  }
}
