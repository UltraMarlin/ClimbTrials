using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.ClimbTrials;

public enum CountdownMode
{
  Off,
  VeryFast,
  Fast,
  Slow,
}

public enum GoalIndicatorMode
{
  Off,
  Subtle,
  Strong,
}

public enum GoalIndicatorTargets
{
  Nearest,
  EachCondition,
}

public enum InputDisplayMode
{
  Off,
  Normal,
  Detailed,
}

public enum LeaderboardMode
{
  Off,
  Subtle,
  Normal,
}

public enum GoalTimesDisplayMode
{
  Off,
  PbOnly,
  All,
}

public enum GhostVisibility
{
  Subtle,
  Strong,
}

public class ClimbTrialsModuleSettings : EverestModuleSettings
{
  [SettingRange(min: 1, max: 5)]
  public int CountdownLength { get; set; } = 3;
  public CountdownMode CountdownMode { get; set; } = CountdownMode.Fast;
  public bool ResetOnDeath { get; set; } = false;
  public bool ShowTimer { get; set; } = true;

  [SettingSubText("modoptions_climbtrialsmodule_pbautosubmit_desc")]
  public bool PbAutoSubmit { get; set; } = true;

  [SettingSubText("modoptions_climbtrialsmodule_goalindicator_desc")]
  public GoalIndicatorMode GoalIndicator { get; set; } = GoalIndicatorMode.Subtle;
  public GoalIndicatorTargets GoalIndicatorTargets { get; set; } =
    GoalIndicatorTargets.EachCondition;

  [SettingSubText("modoptions_climbtrialsmodule_showgoaltimes_desc")]
  public GoalTimesDisplayMode ShowGoalTimes { get; set; } = GoalTimesDisplayMode.All;

  [SettingSubText("modoptions_climbtrialsmodule_showleaderboard_desc")]
  public LeaderboardMode ShowLeaderboard { get; set; } = LeaderboardMode.Normal;

  [SettingSubText("modoptions_climbtrialsmodule_ghostvisibility_desc")]
  public GhostVisibility GhostVisibility { get; set; } = GhostVisibility.Strong;

  [SettingSubText("modoptions_climbtrialsmodule_replayinputdisplay_desc")]
  public InputDisplayMode ReplayInputDisplay { get; set; } = InputDisplayMode.Normal;

  [SettingSubText("modoptions_climbtrialsmodule_offlinemode_desc")]
  public bool OfflineMode { get; set; } = false;

  [SettingIgnore]
  [SettingMaxLength(20)]
  public string PlayerName { get; set; } = "";

  [SettingIgnore]
  public bool PlayerNameChosen { get; set; }

  [SettingIgnore]
  public string ClientId { get; set; } = "";

  [SettingIgnore]
  [SettingMaxLength(16)]
  public string Key { get; set; } = "";

  [SettingIgnore]
  public string ServerUrl { get; set; } = "http://localhost:3000";

  [DefaultButtonBinding(Buttons.Back, Keys.F7)]
  public ButtonBinding Retry { get; set; }
}
