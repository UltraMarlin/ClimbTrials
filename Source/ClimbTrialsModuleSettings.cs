using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Input;
using Monocle;

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

public enum GoalIndicatorMotion
{
  Instant,
  Snappy,
  Smooth,
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

public enum HideVfxMode
{
  Never,
  WithHitboxes,
  Always,
}

public enum GhostVisibility
{
  Hidden,
  Subtle,
  Strong,
}

public enum RunTimerMode
{
  Off,
  TimeOnly,
  Detailed,
}

[SettingSubMenu]
public class HudSettings
{
  public RunTimerMode RunTimer { get; set; } = RunTimerMode.Detailed;

  [SettingSubText("modoptions_climbtrialsmodule_showgoaltimes_desc")]
  public GoalTimesDisplayMode GoalTimes { get; set; } = GoalTimesDisplayMode.All;

  [SettingSubText("modoptions_climbtrialsmodule_showleaderboard_desc")]
  public LeaderboardMode Leaderboard { get; set; } = LeaderboardMode.Subtle;

  [SettingSubText("modoptions_climbtrialsmodule_replayinputdisplay_desc")]
  public InputDisplayMode ReplayInputDisplay { get; set; } = InputDisplayMode.Normal;
}

[SettingSubMenu]
public class GoalIndicatorSettings
{
  public GoalIndicatorMode Style { get; set; } = GoalIndicatorMode.Subtle;
  public GoalIndicatorTargets Targets { get; set; } = GoalIndicatorTargets.EachCondition;
  public GoalIndicatorMotion Motion { get; set; } = GoalIndicatorMotion.Smooth;

  [SettingSubText("modoptions_climbtrialsmodule_hideafterfirstfinish_desc")]
  public bool HideAfterFirstFinish { get; set; } = true;
}

public class ClimbTrialsModuleSettings : EverestModuleSettings
{
  [SettingIgnore]
  public string PlayerName { get; set; } = "";

  [SettingIgnore]
  public bool PlayerNameChosen { get; set; }

  [SettingIgnore]
  public string ClientId { get; set; } = "";

  [SettingIgnore]
  public string ServerUrl { get; set; } = "";

  [SettingRange(min: 1, max: 5)]
  public int CountdownLength { get; set; } = 3;
  public CountdownMode CountdownMode { get; set; } = CountdownMode.Off;
  public bool ResetOnDeath { get; set; } = false;

  [SettingSubText("modoptions_climbtrialsmodule_pbautosubmit_desc")]
  public bool PbAutoSubmit { get; set; } = true;

  [SettingSubText("modoptions_climbtrialsmodule_ghostvisibility_desc")]
  public GhostVisibility GhostVisibility { get; set; } = GhostVisibility.Subtle;

  [SettingSubText("modoptions_climbtrialsmodule_hidevfxwhenpaused_desc")]
  public HideVfxMode HideVfxWhenPaused { get; set; } = HideVfxMode.Always;

  [SettingSubText("modoptions_climbtrialsmodule_offlinemode_desc")]
  public bool OfflineMode { get; set; } = false;

  [SettingSubHeader("modoptions_climbtrialsmodule_runbinds")]
  public ButtonBinding Retry { get; set; }

  [SettingSubHeader("modoptions_climbtrialsmodule_replaybinds")]
  public ButtonBinding ReplayPlayPause { get; set; }

  public ButtonBinding ReplayRestart { get; set; }

  public ButtonBinding ReplayFrameAdvance { get; set; }

  [DefaultButtonBinding(0, Keys.B)]
  public ButtonBinding ReplayShowHitboxes { get; set; }

  [DefaultButtonBinding(Buttons.Y, Keys.G)]
  public ButtonBinding ToggleComparison { get; set; }

  [DefaultButtonBinding(Buttons.Back, Keys.Delete)]
  public ButtonBinding ClearComparisons { get; set; }

  [SettingSubHeader("climbtrials_debug_speedruntool")]
  public ButtonBinding ToggleSpeedrunTool { get; set; }

  [SettingIgnore]
  public bool HasChangedOptions { get; set; }

  public HudSettings HudSettings { get; set; } = new();

  public GoalIndicatorSettings GoalIndicatorSettings { get; set; } = new();

  public bool DiffersFromDefaults() => Differs(this, new ClimbTrialsModuleSettings());

  private static bool Differs(object current, object defaults)
  {
    IEnumerable<PropertyInfo> rows = current
      .GetType()
      .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
      .Where(property => property.GetCustomAttribute<SettingIgnoreAttribute>() == null);
    foreach (PropertyInfo property in rows)
    {
      object value = property.GetValue(current);
      if (property.PropertyType == typeof(ButtonBinding))
      {
        if (!IsDefaultBind((ButtonBinding)value, property))
          return true;
      }
      else if (property.PropertyType.GetCustomAttribute<SettingSubMenuAttribute>() != null)
      {
        if (Differs(value, property.GetValue(defaults)))
          return true;
      }
      else if (!Equals(value, property.GetValue(defaults)))
        return true;
    }
    return false;
  }

  private static Binding VanillaSourceOf(PropertyInfo property) =>
    property.Name switch
    {
      nameof(ReplayPlayPause) => Settings.Instance.Confirm,
      nameof(ReplayRestart) => Settings.Instance.Journal,
      nameof(ReplayFrameAdvance) => Settings.Instance.MenuRight,
      _ => null,
    };

  private static IEnumerable<PropertyInfo> CopiedBinds() =>
    typeof(ClimbTrialsModuleSettings)
      .GetProperties()
      .Where(property => VanillaSourceOf(property) != null);

  public void CopyMissingBinds()
  {
    foreach (PropertyInfo property in CopiedBinds())
    {
      if (property.GetValue(this) != null)
        continue;
      ButtonBinding bind = new();
      CopyKeyboard(bind, VanillaSourceOf(property));
      CopyController(bind, VanillaSourceOf(property));
      property.SetValue(this, bind);
    }
  }

  public void RecopyKeyboardBinds()
  {
    foreach (PropertyInfo property in CopiedBinds())
      CopyKeyboard((ButtonBinding)property.GetValue(this), VanillaSourceOf(property));
  }

  public void RecopyControllerBinds()
  {
    foreach (PropertyInfo property in CopiedBinds())
      CopyController((ButtonBinding)property.GetValue(this), VanillaSourceOf(property));
  }

  private static void CopyKeyboard(ButtonBinding bind, Binding vanilla)
  {
    bind.Keys = [.. vanilla.Keyboard];
    bind.MouseButtons = [.. vanilla.Mouse];
  }

  private static void CopyController(ButtonBinding bind, Binding vanilla) =>
    bind.Buttons = [.. vanilla.Controller];

  private static bool IsDefaultBind(ButtonBinding bind, PropertyInfo property)
  {
    Binding vanilla = VanillaSourceOf(property);
    if (vanilla != null)
      return bind.Keys.SequenceEqual(vanilla.Keyboard)
        && bind.Buttons.SequenceEqual(vanilla.Controller)
        && bind.MouseButtons.SequenceEqual(vanilla.Mouse);
    DefaultButtonBindingAttribute expected =
      property.GetCustomAttribute<DefaultButtonBindingAttribute>();
    Buttons[] buttons = expected != null && expected.Button != 0 ? [expected.Button] : [];
    Keys[] keys = expected != null && expected.Key != Keys.None ? [expected.Key] : [];
    return bind.Buttons.SequenceEqual(buttons)
      && bind.Keys.SequenceEqual(keys)
      && bind.MouseButtons.Count == 0;
  }
}
