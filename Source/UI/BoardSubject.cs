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
  public abstract Medal ShownMedal(BoardRow row);
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

    public override Medal ShownMedal(BoardRow row) =>
      trial.Goals == null
        ? Medal.None
        : GoalTimes.HideUnreachedGrandmaster(
          trial.Goals.MedalReached(row.Ticks),
          trial.Goals.MedalReached(TrialRecords.Get(trial.Id)?.BestTicks ?? long.MaxValue)
        );
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

    public override Medal ShownMedal(BoardRow row) =>
      GoalTimes.HideUnreachedGrandmaster(
        row.Medal ?? Medal.None,
        TrialRecords.Totals(campaign).Medal
      );
  }
}
