using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Runs;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public abstract class BoardSubject
{
  public abstract string Id { get; }
  public abstract string Name { get; }
  public abstract MTexture Skull { get; }
  public abstract string CountKey { get; }
  public abstract IReadOnlyList<BoardSubject> Siblings { get; }
  public abstract void Fetch(int chunk, Action<ServerResult<Board>> onDone);
  public abstract Medal MedalFor(BoardRow row);
  public virtual Trial Trial => null;

  public static BoardSubject Of(Trial trial) => new TrialSubject(trial);

  public static BoardSubject Of(Campaign campaign) => new CampaignSubject(campaign);

  private sealed class TrialSubject(Trial trial) : BoardSubject
  {
    public override string Id => trial.Id;
    public override string Name => trial.Name;
    public override MTexture Skull => GFX.Gui[trial.SkullTexture];
    public override string CountKey => "climbtrials_board_runners";
    public override Trial Trial => trial;

    public override IReadOnlyList<BoardSubject> Siblings =>
      Campaigns.CampaignOf(trial) is Campaign campaign
        ? campaign.Trials.Select(t => Of(t)).ToList()
        : [Of(trial)];

    public override void Fetch(int chunk, Action<ServerResult<Board>> onDone) =>
      Standings.FetchTrialBoard(trial, chunk, onDone);

    public override Medal MedalFor(BoardRow row) =>
      trial.Goals?.VisibleMedalFor(
        row.Ticks,
        TrialRecords.Get(trial.Id)?.BestTicks ?? long.MaxValue
      )
      ?? Medal.None;
  }

  private sealed class CampaignSubject(Campaign campaign) : BoardSubject
  {
    public override string Id => campaign.Id;
    public override string Name => campaign.Name;
    public override MTexture Skull => null;
    public override string CountKey => "climbtrials_board_runners";

    public override IReadOnlyList<BoardSubject> Siblings =>
      Campaigns.All.Select(c => Of(c)).ToList();

    public override void Fetch(int chunk, Action<ServerResult<Board>> onDone) =>
      Standings.FetchCampaignBoard(campaign, chunk, onDone);

    public override Medal MedalFor(BoardRow row) =>
      GoalTimes.VisibleFor(row.Medal ?? Medal.None, TrialRecords.Get(campaign).Medal);
  }
}
