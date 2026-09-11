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

public enum GoalIndicatorTargets
{
  Nearest,
  EachCondition,
}

public enum InputDisplayMode
{
  Normal,
  Detailed,
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
  public GoalIndicatorTargets GoalIndicatorTargets { get; set; } =
    GoalIndicatorTargets.EachCondition;
  public InputDisplayMode ReplayInputDisplay { get; set; } = InputDisplayMode.Normal;

  [SettingMaxLength(20)]
  public string PlayerName { get; set; } = "";

  [SettingIgnore]
  public string ClientId { get; set; } = "";

  [SettingIgnore]
  [SettingMaxLength(16)]
  public string Key { get; set; } = "";

  [DefaultButtonBinding(Buttons.Back, Keys.F7)]
  public ButtonBinding Retry { get; set; }
}
