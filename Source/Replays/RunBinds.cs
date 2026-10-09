using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Core;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Replays;

public static class RunBinds
{
  private static readonly List<(VirtualButton Button, Binding Binding)> saved = [];

  public static void Load() => Everest.Events.Input.OnInitialize += OnInputInitialize;

  public static void Unload()
  {
    Everest.Events.Input.OnInitialize -= OnInputInitialize;
    Restore();
  }

  private static void OnInputInitialize()
  {
    if (saved.Count == 0)
      return;
    Restore();
    Blank();
  }

  public static void Blank()
  {
    if (saved.Count > 0)
      return;
    foreach (VirtualButton button in PageButtons())
    {
      saved.Add((button, button.Binding));
      button.Binding = new Binding();
    }
  }

  public static void Restore()
  {
    foreach ((VirtualButton button, Binding binding) in saved)
      button.Binding = binding;
    saved.Clear();
  }

  private static IEnumerable<VirtualButton> PageButtons() =>
    new[] { CoreModule.Settings.MenuPageUp, CoreModule.Settings.MenuPageDown }
      .Select(bind => bind?.Button)
      .Where(button => button != null);
}
