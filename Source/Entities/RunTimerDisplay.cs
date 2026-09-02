using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class RunTimerDisplay : Entity
{
  private const float TimerY = 60f;
  private const float TextHeight = 38f,
    TextMarginLeft = 32f,
    Baseline = 44f;
  private static readonly Color RunColor1 = Color.White;
  private static readonly Color RunColor2 = Color.LightGray;
  private static readonly Color DoneColor1 = Calc.HexToColor("fad768");
  private static readonly Color DoneColor2 = Calc.HexToColor("cfa727");

  public RunTimerDisplay()
    : base()
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate;
  }

  public static bool ShouldShow =>
    RunManager.Attempt.State switch
    {
      AttemptState.Idle => false,
      AttemptState.Running => ClimbTrialsModule.Settings.ShowTimerWhileRunning,
      _ => true,
    };

  public override void Render()
  {
    if (!ShouldShow)
      return;

    Attempt attempt = RunManager.Attempt;

    string text = Attempt.Format(attempt.ElapsedTicks);
    float textWidth = TimeText.Width(text);

    MTexture bg = GFX.Gui["strawberryCountBG"];
    float extra = System.Math.Max(0f, textWidth + TextMarginLeft + 8f - bg.Width);
    Draw.Rect(0f, TimerY, extra + 2f, TextHeight, Color.Black);
    bg.Draw(new Vector2(extra, TimerY));

    bool done = attempt.State == AttemptState.Finished;
    TimeText.Draw(
      new Vector2(TextMarginLeft, TimerY + Baseline),
      text,
      1f,
      done ? DoneColor1 : RunColor1,
      done ? DoneColor2 : RunColor2
    );
  }
}
