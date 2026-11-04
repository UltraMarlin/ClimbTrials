using System;
using Celeste.Mod.Core;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class WatcherMenu : MenuOnTop
{
  private const float HighlightInterval = 0.1f;

  private readonly DimButton leaderboard;
  private readonly Action resume;
  private float highlightTime;
  private bool updatedOnce;

  public float RowsAlpha = 1f;
  public bool Covered;

  public WatcherMenu(
    Action resume,
    Action modOptions,
    Action play,
    Action race,
    Action board,
    Action leave
  )
    : base(inLevel: true)
  {
    this.resume = resume;
    Focused = false;
    RenderAsFocused = true;
    Add(new Button(Dialog.Clean("climbtrials_watcher_resume")).Pressed(resume));
    Add(new Button(Dialog.Clean("menu_pause_modoptions")).Pressed(modOptions));
    Add(new Button(Dialog.Clean("climbtrials_play_trial")).Pressed(play));
    Add(new Button(Dialog.Clean("climbtrials_race_this_ghost")).Pressed(race));
    Add(
      leaderboard = new DimButton(
        Dialog.Clean("climbtrials_trials_leaderboard"),
        () => !ClimbTrialsModule.Settings.OfflineMode,
        board
      )
    );
    Add(new Button(Dialog.Clean("climbtrials_return_to_trials")).Pressed(leave));
  }

  public override void Update()
  {
    if (updatedOnce && !Closing && !Covered)
      ReadKeys(MenuKeys.Watcher, resume);
    updatedOnce = true;
    base.Update();
    highlightTime += Engine.RawDeltaTime;
    bool second = (int)(highlightTime / HighlightInterval) % 2 == 1;
    HighlightColor =
      second && CoreModule.Settings.AllowTextHighlight ? HighlightColorB : HighlightColorA;
    if (Closed)
      RemoveSelf();
  }

  public override void Render()
  {
    float alpha = Alpha;
    Alpha = alpha * RowsAlpha;
    base.Render();
    if (Current == leaderboard && ClimbTrialsModule.Settings.OfflineMode)
      DrawReason(Dialog.Clean("climbtrials_offline_mode"));
    Alpha = alpha;
  }
}
