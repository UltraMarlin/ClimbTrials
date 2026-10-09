using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.Core;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public sealed class MenuKeys
{
  public static readonly MenuKeys Live = new(raw: false);
  public static readonly MenuKeys Watcher = new(raw: true);

  public bool Raw { get; }

  private MenuKeys(bool raw) => Raw = raw;

  private static Settings Binds => Settings.Instance;

  public bool Up => Replayer.MenuPressed(Binds.MenuUp);
  public bool Down => Replayer.MenuPressed(Binds.MenuDown);
  public bool Left => Replayer.MenuPressed(Binds.MenuLeft);
  public bool Right => Replayer.MenuPressed(Binds.MenuRight);

  public bool Confirm => Raw ? Pressed(Binds.Confirm) : Input.MenuConfirm.Pressed;

  public bool Close =>
    Raw
      ? Pressed(Binds.Cancel) || Pressed(Binds.Journal) || Pressed(Replayer.EscapeBinding())
      : Input.MenuCancel.Pressed || Input.ESC.Pressed || Input.MenuJournal.Pressed;

  public bool Back =>
    Pressed(Binds.Cancel) || Pressed(Binds.Pause) || Pressed(Replayer.EscapeBinding());

  public bool PageDown => Pressed(CoreModule.Settings.MenuPageDown);
  public bool PageUp => Pressed(CoreModule.Settings.MenuPageUp);
  public bool ToggleComparison => Pressed(ClimbTrialsModule.Settings.ToggleComparison);
  public bool ClearComparisons => Pressed(ClimbTrialsModule.Settings.ClearComparisons);

  private bool Pressed(ButtonBinding bind) =>
    bind != null && (Raw ? Pressed(RunBinds.Unblanked(bind)) : RunBinds.Pressed(bind));

  private static bool Pressed(Binding binding) =>
    binding != null && Replayer.WatcherPressed(binding);
}
