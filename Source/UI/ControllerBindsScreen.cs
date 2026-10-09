namespace Celeste.Mod.ClimbTrials.UI;

public class ControllerBindsScreen(EverestModule module) : ModuleSettingsButtonConfigUI(module)
{
  public override void Reset()
  {
    base.Reset();
    ClimbTrialsModule.Settings.RecopyControllerBinds();
    Reload(Selection);
  }
}
