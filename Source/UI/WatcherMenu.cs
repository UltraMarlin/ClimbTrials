using System;
using Celeste.Mod.Core;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class WatcherMenu : FadingMenu
{
  private const float HighlightInterval = 0.1f;

  private float highlightTime;

  public WatcherMenu(Action resume, Action play, Action race, Action leave)
  {
    Focused = false;
    RenderAsFocused = true;
    Add(new Button(Dialog.Clean("climbtrials_watcher_resume")).Pressed(resume));
    Add(new Button(Dialog.Clean("climbtrials_play_trial")).Pressed(play));
    Add(new Button(Dialog.Clean("climbtrials_race_this_ghost")).Pressed(race));
    Add(new Button(Dialog.Clean("climbtrials_return_to_trials")).Pressed(leave));
  }

  public void MoveUp() => MoveSelection(-1, wiggle: true);

  public void MoveDown() => MoveSelection(1, wiggle: true);

  public void Confirm()
  {
    Current.ConfirmPressed();
    Current.OnPressed?.Invoke();
  }

  public override void Update()
  {
    base.Update();
    highlightTime += Engine.RawDeltaTime;
    bool second = (int)(highlightTime / HighlightInterval) % 2 == 1;
    HighlightColor =
      second && CoreModule.Settings.AllowTextHighlight ? HighlightColorB : HighlightColorA;
  }
}
