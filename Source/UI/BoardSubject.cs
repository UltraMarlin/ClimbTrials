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
  public abstract bool IsChunkCached(int chunk);
  public abstract Campaign Campaign { get; }
  public virtual Trial Trial => null;

  public EventCampaign Event => Events.Of(Campaign);

  public Medal ShownMedal(BoardRow row, BoardMode mode) =>
    mode == BoardMode.FullTimes ? MedalOf(row) : Medal.None;

  protected abstract Medal MedalOf(BoardRow row);

  public static BoardSubject ForTrial(Trial trial) => new TrialSubject(trial, alone: false);

  public static BoardSubject ForTrialAlone(Trial trial) => new TrialSubject(trial, alone: true);

  public static BoardSubject ForCampaign(Campaign campaign) => new CampaignSubject(campaign);

  private sealed class TrialSubject(Trial trial, bool alone) : BoardSubject
  {
    public override string Id => trial.Id;
    public override string Name => trial.Name;
    public override MTexture Skull => GFX.Gui[trial.SkullTexture];
    public override string CountKey => "climbtrials_board_runners";
    public override Trial Trial => trial;
    public override Campaign Campaign => Campaigns.CampaignOf(trial);

    public override IReadOnlyList<BoardSubject> Siblings =>
      !alone && Campaign is Campaign campaign ? campaign.Trials.Select(ForTrial).ToList() : [this];

    public override void Fetch(int chunk, Action<ServerResult<Board>> onDone) =>
      Standings.FetchTrialBoard(trial, chunk, onDone);

    public override bool IsChunkCached(int chunk) => Standings.IsTrialChunkCached(trial, chunk);

    protected override Medal MedalOf(BoardRow row) =>
      row.Ticks is not long ticks
        ? Medal.None
        : GoalTimes.HideUnreachedGrandmaster(
          trial.Goals.MedalReached(ticks),
          trial.Goals.MedalReached(TrialRecords.Get(trial.Id)?.BestTicks ?? long.MaxValue)
        );
  }

  private sealed class CampaignSubject(Campaign campaign) : BoardSubject
  {
    public override string Id => campaign.Id;
    public override string Name => campaign.Name;
    public override MTexture Skull => null;
    public override string CountKey => "climbtrials_board_runners";
    public override Campaign Campaign => campaign;
    public override IReadOnlyList<BoardSubject> Siblings =>
      (Campaigns.IsEvent(campaign) ? Events.Ordered() : Campaigns.All).Select(ForCampaign).ToList();

    public override void Fetch(int chunk, Action<ServerResult<Board>> onDone) =>
      Standings.FetchCampaignBoard(campaign, chunk, onDone);

    public override bool IsChunkCached(int chunk) =>
      Standings.IsCampaignChunkCached(campaign, chunk);

    protected override Medal MedalOf(BoardRow row) =>
      GoalTimes.HideUnreachedGrandmaster(
        row.Medal ?? Medal.None,
        TrialRecords.Totals(campaign).Medal
      );
  }
}
