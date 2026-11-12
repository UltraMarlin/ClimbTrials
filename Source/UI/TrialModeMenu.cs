using System;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class TrialModeMenu : MenuOnTop
{
  private const float TitleScale = 1.2f;
  private const float TitleHeight = 100f;
  private const float PbScale = 1.0f;
  private const float PbHeight = 42f;
  private const float DateScale = 0.45f;
  private const float DateHeight = 32f;
  private const float IconGap = 16f;
  private const float LabelGap = 16f;

  private readonly Action closed;

  public TrialModeMenu(
    Trial trial,
    Action play,
    Action watch,
    Action race,
    Action submit,
    Action board,
    Action deletePb,
    Action closed
  )
    : base(inLevel: false)
  {
    this.closed = closed;
    Position.Y -= MenuLift;

    Campaign campaign = Campaigns.CampaignOf(trial);
    TrialRecord record = TrialRecords.Get(trial.Id);
    ServerRecord serverRecord = Standings.RecordFor(trial);
    StandingsState state = Standings.State(campaign);

    Add(new TitleItem(trial));
    if (record != null)
    {
      Add(new PbItem(record, serverRecord));
      Add(new DateItem(record));
    }

    Add(new Gap(OptionsGap));
    Add(new Button(Dialog.Clean("climbtrials_play_trial")).Pressed(play));
    if (ReplayStore.Exists(trial.Id))
    {
      Add(new Button(Dialog.Clean("climbtrials_trials_watch_pb")).Pressed(watch));
      Add(new Button(Dialog.Clean("climbtrials_race_pb_ghost")).Pressed(race));
    }
    bool pbOffBoard = record != null && !OnBoard(record, serverRecord);
    if (pbOffBoard)
      AddSubmitRow(trial, record, state, submit);
    Add(new Button(Dialog.Clean("climbtrials_trials_leaderboard")).Pressed(board));
    if (pbOffBoard)
      Add(
        new DimButton(
          Dialog.Clean("climbtrials_trials_delete_pb"),
          canAct: () =>
            Uploader.UploadFor(trial.Id).State
              is not (UploadState.Submitting or UploadState.Accepted),
          onPressed: () => ConfirmHold.Start(this, SFX.ui_main_savefile_delete, deletePb)
        )
      );

    OnCancel = Back;
    OnESC = Back;
    OnPause = Back;
  }

  private static bool OnBoard(TrialRecord record, ServerRecord serverRecord) =>
    serverRecord?.Ticks is long serverTicks && serverTicks <= record.BestTicks;

  private void AddSubmitRow(Trial trial, TrialRecord record, StandingsState state, Action submit)
  {
    Upload upload = Uploader.UploadFor(trial.Id);
    string submitBlockedReason = SubmitBlockedReason(trial, record, state, upload);
    DimButton row = new(
      Dialog.Clean("climbtrials_trials_submit_pb"),
      submitBlockedReason == null,
      submit
    );
    Add(row);
    string reason = submitBlockedReason;
    if (reason == null && upload.State == UploadState.Failed)
      reason =
        upload.Refusal != SubmitRefusal.None
          ? Uploader.RefusalReason(upload.Refusal)
          : upload.Message;
    if (reason != null)
      RowDescription.Attach(this, row, reason);
  }

  private static string SubmitBlockedReason(
    Trial trial,
    TrialRecord record,
    StandingsState state,
    Upload upload
  )
  {
    SubmitRefusal refusal = Events.RefusalFor(trial);
    if (Identity.Banned)
      return Dialog.Clean("climbtrials_submit_banned");
    if (!record.Submittable)
      return Dialog.Clean("climbtrials_submit_unsubmittable");
    if (!ReplayStore.Exists(trial.Id) && trial.FitsReplayLimit(record.BestTicks))
      return Dialog.Clean("climbtrials_submit_no_replay");
    if (refusal != SubmitRefusal.None)
      return Uploader.RefusalReason(refusal);
    if (ClimbTrialsModule.Settings.OfflineMode)
      return Dialog.Clean("climbtrials_offline_mode");
    if (state == StandingsState.Offline)
      return Dialog.Clean("climbtrials_offline");
    if (state == StandingsState.Idle)
      return Dialog.Clean("climbtrials_submit_loading");
    if (Identity.Registering)
      return Dialog.Clean("climbtrials_connecting");
    if (!Identity.Registered)
      return Dialog.Clean("climbtrials_not_connected");
    if (Standings.SubmissionsPaused)
      return Dialog.Clean("climbtrials_submit_paused");
    if (upload.State == UploadState.Submitting)
      return Dialog.Clean("climbtrials_submitting");
    return null;
  }

  private void Back()
  {
    Audio.Play(SFX.ui_main_button_back);
    Dismiss();
  }

  public override void Update()
  {
    base.Update();
    if (!Closed)
      return;
    RemoveSelf();
    closed?.Invoke();
  }

  private class TitleItem(Trial trial) : Item
  {
    private MTexture Skull => GFX.Gui[trial.SkullTexture];

    public override float Height() => TitleHeight;

    public override float LeftWidth() =>
      Skull.Width + IconGap + ActiveFont.Measure(trial.Name).X * TitleScale;

    public override void Render(Vector2 position, bool highlighted)
    {
      float alpha = Container.Alpha;
      float left = position.X + (Container.Width - LeftWidth()) / 2f;
      Skull.DrawCentered(new Vector2(left + Skull.Width / 2f, position.Y), Color.White * alpha);
      Vector2 at = new(left + Skull.Width + IconGap, position.Y);
      TextOutline.Draw(trial.Name, at, new Vector2(0f, 0.5f), TitleScale, Color.White * alpha);
    }
  }

  private class PbItem(TrialRecord record, ServerRecord serverRecord) : Item
  {
    private readonly string label = $"{Dialog.Clean("climbtrials_pb")}:";
    private readonly string rankText = serverRecord?.Rank is int rank
      ? $"({RankText.Format(rank)})"
      : "";
    private readonly string time = TimeText.Format(record.BestTicks);
    private static TimeStyle Style => TimeText.Hud.At(PbScale);

    private float LabelWidth => ActiveFont.Measure(label).X * PbScale;

    private float RankWidth =>
      rankText.Length == 0 ? 0f : LabelGap + ActiveFont.Measure(rankText).X * PbScale;

    public override float Height() => PbHeight;

    public override float LeftWidth() =>
      LabelWidth + LabelGap + TimeText.Width(time, Style) + RankWidth;

    public override void Render(Vector2 position, bool highlighted)
    {
      float alpha = Container.Alpha;
      float left = position.X + (Container.Width - LeftWidth()) / 2f;
      TextOutline.Draw(
        label,
        new Vector2(left, position.Y),
        new Vector2(0f, 0.5f),
        PbScale,
        Color.White * alpha
      );
      float timeLeft = left + LabelWidth + LabelGap;
      Vector2 baseline = new(timeLeft, position.Y + TimeText.Height(Style) / 2f);
      TimeText.Draw(baseline, time, Style, Color.White * alpha);
      if (rankText.Length == 0)
        return;
      float rankLeft = timeLeft + TimeText.Width(time, Style) + LabelGap;
      TextOutline.Draw(
        rankText,
        new Vector2(rankLeft, position.Y),
        new Vector2(0f, 0.5f),
        PbScale,
        Color.White * alpha
      );
    }
  }

  private class DateItem(TrialRecord record) : Item
  {
    private readonly string text =
      $"{Dialog.Clean("climbtrials_replay_played_date")} {TimeText.FormatDate(record.AchievedAtUtc)}";

    public override float Height() => DateHeight;

    public override float LeftWidth() => ActiveFont.Measure(text).X * DateScale;

    public override void Render(Vector2 position, bool highlighted)
    {
      float left = position.X + (Container.Width - LeftWidth()) / 2f;
      TextOutline.DrawDim(
        text,
        new Vector2(left, position.Y),
        new Vector2(0f, 0.5f),
        DateScale,
        Container.Alpha
      );
    }
  }
}
