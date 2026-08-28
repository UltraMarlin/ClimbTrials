using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.ClimbTrials;

public enum CountdownMode
{
  Slow,
  Fast,
  VeryFast,
  Off,
}

public class ClimbTrialsModuleSettings : EverestModuleSettings
{
  [SettingRange(min: 1, max: 5)]
  public int CountdownLength { get; set; } = 3;
  public CountdownMode CountdownMode { get; set; } = CountdownMode.Fast;
  public bool ResetOnDeath { get; set; } = false;
  public bool ShowTimerWhileRunning { get; set; } = true;

  [DefaultButtonBinding(0, Keys.F7)]
  public ButtonBinding Retry { get; set; }
}
