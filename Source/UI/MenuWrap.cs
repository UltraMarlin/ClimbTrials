using System.Linq;

namespace Celeste.Mod.ClimbTrials.UI;

public static class MenuWrap
{
  public static void Load() => On.Celeste.TextMenu.MoveSelection += OnMoveSelection;

  public static void Unload() => On.Celeste.TextMenu.MoveSelection -= OnMoveSelection;

  private static void OnMoveSelection(
    On.Celeste.TextMenu.orig_MoveSelection orig,
    TextMenu self,
    int direction,
    bool wiggle
  )
  {
    if (
      !IsModMenu(self)
      || self.Items.Count(item => item.Hoverable) != 2
      || !AtEdge(self, direction)
    )
    {
      orig(self, direction, wiggle);
      return;
    }
    orig(self, -direction, false);
    if (!wiggle)
      return;
    Audio.Play(direction > 0 ? SFX.ui_main_roll_down : SFX.ui_main_roll_up);
    self.Current.SelectWiggler.Start();
  }

  private static bool IsModMenu(TextMenu menu) =>
    menu.GetType().Assembly == typeof(MenuWrap).Assembly;

  private static bool AtEdge(TextMenu menu, int direction) =>
    direction > 0
      ? menu.Selection == menu.LastPossibleSelection
      : menu.Selection == menu.FirstPossibleSelection;
}
