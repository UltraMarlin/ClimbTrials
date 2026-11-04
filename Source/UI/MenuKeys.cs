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

  private const float RepeatDelay = 0.4f;
  private const float RepeatInterval = 0.1f;

  private readonly KeyRepeat up = new();
  private readonly KeyRepeat down = new();
  private readonly KeyRepeat left = new();
  private readonly KeyRepeat right = new();

  public bool Up => Raw ? up.Fired : Replayer.MenuPressed(Binds.MenuUp);
  public bool Down => Raw ? down.Fired : Replayer.MenuPressed(Binds.MenuDown);
  public bool Left => Raw ? left.Fired : Replayer.MenuPressed(Binds.MenuLeft);
  public bool Right => Raw ? right.Fired : Replayer.MenuPressed(Binds.MenuRight);

  public bool StepUp(TextMenu menu) =>
    Up && !(Raw && up.Repeating && menu.Selection == menu.FirstPossibleSelection);

  public bool StepDown(TextMenu menu) =>
    Down && !(Raw && down.Repeating && menu.Selection == menu.LastPossibleSelection);

  public void Tick(float deltaSeconds)
  {
    up.Tick(Binds.MenuUp, deltaSeconds);
    down.Tick(Binds.MenuDown, deltaSeconds);
    left.Tick(Binds.MenuLeft, deltaSeconds);
    right.Tick(Binds.MenuRight, deltaSeconds);
  }

  public bool Confirm => Raw ? Pressed(Binds.Confirm) : Input.MenuConfirm.Pressed;

  public bool Close =>
    Raw
      ? Pressed(Binds.Cancel) || Pressed(Binds.Journal) || Pressed(Replayer.EscapeBinding())
      : Input.MenuCancel.Pressed || Input.ESC.Pressed || Input.MenuJournal.Pressed;

  public bool Back =>
    Pressed(Binds.Cancel) || Pressed(Binds.Pause) || Pressed(Replayer.EscapeBinding());

  public bool SideRepeating =>
    Raw ? left.Repeating || right.Repeating : Input.MenuLeft.Repeating || Input.MenuRight.Repeating;

  public bool PageDown => Pressed(CoreModule.Settings.MenuPageDown);
  public bool PageUp => Pressed(CoreModule.Settings.MenuPageUp);
  public bool ToggleComparison => Pressed(ClimbTrialsModule.Settings.ToggleComparison);
  public bool ClearComparisons => Pressed(ClimbTrialsModule.Settings.ClearComparisons);

  private bool Pressed(ButtonBinding bind) =>
    bind != null && (Raw ? Pressed(RunBinds.Unblanked(bind)) : RunBinds.Pressed(bind));

  private static bool Pressed(Binding binding) =>
    binding != null && Replayer.WatcherPressed(binding);

  private sealed class KeyRepeat
  {
    private float wait;

    public bool Fired { get; private set; }
    public bool Repeating { get; private set; }

    public void Tick(Binding binding, float deltaSeconds)
    {
      Repeating = false;
      if (Replayer.MenuPressed(binding))
      {
        Fired = true;
        wait = RepeatDelay;
        return;
      }
      if (!Replayer.MenuHeld(binding))
      {
        Fired = false;
        return;
      }
      wait -= deltaSeconds;
      Fired = wait <= 0f;
      Repeating = Fired;
      if (Fired)
        wait += RepeatInterval;
    }
  }
}
