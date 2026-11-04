using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.ClimbTrials.UI;

public static class Hints
{
  private sealed record Hint(Func<bool> Applies, string DialogKey);

  private static readonly Random random = new();

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  private static bool OptionsUntouched => !Settings.HasChangedOptions;

  private static bool RetryUnbound =>
    Settings.Retry.Keys.Count == 0
    && Settings.Retry.Buttons.Count == 0
    && Settings.Retry.MouseButtons.Count == 0;

  private static readonly Hint[] All =
  [
    new(() => OptionsUntouched, "climbtrials_hint_hud"),
    new(() => OptionsUntouched, "climbtrials_hint_ghost"),
    new(() => OptionsUntouched, "climbtrials_hint_reset_on_death"),
    new(() => RetryUnbound, "climbtrials_hint_retry_bind"),
  ];

  public static string Pick()
  {
    List<string> keys = All.Where(hint => hint.Applies()).Select(hint => hint.DialogKey).ToList();
    return keys.Count == 0 ? null : keys[random.Next(keys.Count)];
  }
}
