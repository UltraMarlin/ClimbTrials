using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class ConfirmButton : DimButton
{
  private const float ConfirmDelaySeconds = 1.5f;
  private const float WaitingGrey = 0.5f;

  private readonly string label;
  private readonly string confirmLabel;
  private float secondsAsked = -1f;

  public bool Asking => secondsAsked >= 0f;
  private bool Confirmable => secondsAsked >= ConfirmDelaySeconds;

  public ConfirmButton(string label, string confirmLabel, Func<bool> canAct, Action onConfirmed)
    : base(label, canAct, onPressed: null)
  {
    this.label = label;
    this.confirmLabel = confirmLabel;

    Pressed(() =>
    {
      if (!canAct())
        return;
      if (!Asking)
        Ask();
      else if (Confirmable)
        onConfirmed();
    });
  }

  public void StopAsking()
  {
    secondsAsked = -1f;
    Label = label;
    Container?.RecalculateSize();
  }

  private void Ask()
  {
    secondsAsked = 0f;
    Label = confirmLabel;
    Container.RecalculateSize();
  }

  public override void Update()
  {
    base.Update();
    if (Asking && (Container.Current != this || !canAct()))
      StopAsking();
    if (Asking)
      secondsAsked += Engine.RawDeltaTime;
    if (canAct() && Asking)
      ConfirmSfx = Confirmable ? SFX.ui_main_savefile_delete : SFX.ui_main_button_invalid;
  }

  public override void Render(Vector2 position, bool highlighted)
  {
    if (!Asking)
    {
      base.Render(position, highlighted);
      return;
    }
    Color color = Confirmable
      ? Colors.Warning
      : Color.Lerp(Colors.Warning, Color.DarkSlateGray, WaitingGrey);
    DrawLabel(position, color * Container.Alpha);
  }
}
