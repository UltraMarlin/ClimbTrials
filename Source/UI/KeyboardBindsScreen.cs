namespace Celeste.Mod.ClimbTrials.UI;

public class KeyboardBindsScreen(EverestModule module) : ModuleSettingsKeyboardConfigUI(module)
{
  public override void Reset()
  {
    base.Reset();
    ClimbTrialsModule.Settings.RecopyKeyboardBinds();
    Reload(Selection);
  }
}
