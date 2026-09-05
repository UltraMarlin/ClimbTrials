using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.ClimbTrials;

public enum CountdownMode
{
  Slow,
  Fast,
  VeryFast,
  Off,
}

public enum GoalIndicatorMode
{
  Subtle,
  Strong,
  Off,
}

public class ClimbTrialsModuleSettings : EverestModuleSettings
{
  [SettingRange(min: 1, max: 5)]
  public int CountdownLength { get; set; } = 3;
  public CountdownMode CountdownMode { get; set; } = CountdownMode.Fast;
  public bool ResetOnDeath { get; set; } = false;
  public bool ShowTimerWhileRunning { get; set; } = true;
  public GoalIndicatorMode GoalIndicator { get; set; } = GoalIndicatorMode.Subtle;

  [DefaultButtonBinding(0, Keys.F7)]
  public ButtonBinding Retry { get; set; }
}
