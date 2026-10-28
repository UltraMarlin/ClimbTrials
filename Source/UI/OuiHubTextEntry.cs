using System;
using System.Collections;
using Celeste.Mod.Core;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiHubTextEntry : OuiModOptionString
{
  public string Headline { get; set; }

  private bool OpenedByHub => Headline != null;

  public override IEnumerator Leave(Oui next)
  {
    IEnumerator leaving = base.Leave(next);
    while (leaving.MoveNext())
      yield return leaving.Current;
    Headline = null;
  }

  public override void Update() => RunForDeviceInHand(base.Update);

  public override void Render()
  {
    RunForDeviceInHand(base.Render);
    if (!OpenedByHub)
      return;
    ActiveFont.DrawOutline(
      Headline,
      Position + new Vector2(ListScreen.OnScreenX, ListScreen.TitleY),
      new Vector2(0.5f, 0.5f),
      Vector2.One * ListScreen.TitleScale,
      Color.White,
      2f,
      TextOutline.StrokeFor(Color.White)
    );
  }

  private void RunForDeviceInHand(Action step)
  {
    bool typing = CoreModule.Settings.UseKeyboardForTextInput;
    if (OpenedByHub && MInput.ControllerHasFocus)
      CoreModule.Settings.UseKeyboardForTextInput = false;
    try
    {
      step();
    }
    finally
    {
      CoreModule.Settings.UseKeyboardForTextInput = typing;
    }
  }
}
