using System.Collections;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiClimbTrialsOptions : Oui
{
  public const int ScreenDepth = 2;

  private TextMenu menu;

  public override IEnumerator Enter(Oui from)
  {
    menu = BuildMenu();
    menu.Focused = false;
    Scene.Add(menu);
    Visible = true;
    float side = ListScreen.DepthOf(from) > ScreenDepth ? -1f : 1f;
    Place(side * Motion.SlideDistance, 0f);
    yield return Motion.EnterScreen(side, Place);
    Place(0f, 1f);
    menu.Focused = true;
    Focused = true;
  }

  public override IEnumerator Leave(Oui next)
  {
    Focused = false;
    menu.Focused = false;
    yield return Everest.SaveSettings();
    float side = ListScreen.DepthOf(next) > ScreenDepth ? -1f : 1f;
    TextMenu leaving = menu;
    yield return Motion.LeaveScreen(
      this,
      side,
      (offset, alpha) => Place(leaving, offset, alpha),
      () => Gone(leaving)
    );
  }

  private void Gone(TextMenu leaving)
  {
    leaving.RemoveSelf();
    if (menu != leaving)
      return;
    menu = null;
    Visible = false;
  }

  private void Place(float offset, float alpha) => Place(menu, offset, alpha);

  private static void Place(TextMenu target, float offset, float alpha)
  {
    target.X = ListScreen.OnScreenX + offset;
    target.Alpha = alpha;
  }

  private TextMenu BuildMenu()
  {
    TextMenu built = new() { CompactWidthMode = true, BatchMode = true };
    built.Add(new TextMenu.Header(Dialog.Clean("climbtrials_options_title")));
    ClimbTrialsModule.Instance.CreateModMenuSection(built, false, null);
    if (built.Height > built.ScrollableMinSize)
      built.Position.Y = built.ScrollTargetY;
    built.BatchMode = false;
    built.OnCancel = () =>
    {
      built.Focused = false;
      Audio.Play(SFX.ui_main_button_back);
      Overworld.Goto<OuiHub>();
    };
    return built;
  }
}
