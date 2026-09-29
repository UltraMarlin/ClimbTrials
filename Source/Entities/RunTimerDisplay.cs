using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class RunTimerDisplay : Entity
{
  private const float Baseline = 44f;
  private const float BackgroundWidth = 288f;
  private static readonly Color RunColor = Color.White;
  private static readonly Color DoneColor = Calc.HexToColor("fad768");

  public RunTimerDisplay()
    : base()
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate;
  }

  public static bool ShouldShow =>
    RunManager.Attempt.State switch
    {
      AttemptState.Idle => RunManager.Run != null,
      AttemptState.Running => RunManager.Replaying
        || ClimbTrialsModule.Settings.ShowRunTimer != RunTimerMode.Off,
      _ => true,
    };

  public override void Render()
  {
    if (!ShouldShow)
      return;

    Attempt attempt = RunManager.Attempt;

    string text = TimeText.Format(attempt.ElapsedTicks);
    TimerBackground.Draw(RunHudLayout.TimerY, RunHudLayout.TimerBackgroundHeight, BackgroundWidth);

    bool done = attempt.State == AttemptState.Finished;
    TimeText.Draw(
      new Vector2(RunHudLayout.TimerTextLeft, RunHudLayout.TimerY + Baseline),
      text,
      TimeText.Hud,
      done ? DoneColor : RunColor
    );
  }
}
