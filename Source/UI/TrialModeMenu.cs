using System;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class TrialModeMenu : FadingMenu
{
  private const float MenuLift = 80f;
  private const float TitleScale = 1.2f;
  private const float TitleHeight = 100f;
  private const float PbScale = 1.0f;
  private const float PbHeight = 42f;
  private const float NoteHeight = 42f;
  private const float DateScale = 0.45f;
  private const float DateHeight = 32f;
  private const float OptionsGap = 40f;
  private const float IconGap = 16f;
  private const float LabelGap = 16f;

  private readonly Action closed;

  public TrialModeMenu(
    Trial trial,
    Action play,
    Action watch,
    Action race,
    Action submit,
    Action closed
  )
  {
    this.closed = closed;
    Position.Y -= MenuLift;

    Campaign campaign = Campaigns.CampaignOf(trial);
    TrialRecord record = TrialRecords.Get(trial.Id);
    TrialStanding standing = Standings.For(trial);
    StandingsState board = Standings.State(campaign);

    Add(new TitleItem(trial));
    if (record != null)
      Add(new PbItem(record, standing));
    if (board == StandingsState.Offline)
      Add(new NoteItem(Dialog.Clean("climbtrials_offline"), RowLayout.Dim));
    if (record != null)
      Add(new DateItem(record));

    Add(new Gap(OptionsGap));
    Add(new Button(Dialog.Clean("climbtrials_trials_play")).Pressed(play));
    if (ReplayStore.Exists(trial.Id))
    {
      Add(new Button(Dialog.Clean("climbtrials_trials_watch_pb")).Pressed(watch));
      Add(new Button(Dialog.Clean("climbtrials_trials_race_pb")).Pressed(race));
      AddSubmitRow(trial, record, standing, board, submit);
    }

    OnCancel = Back;
    OnESC = Back;
    OnPause = Back;
  }

  private void AddSubmitRow(
    Trial trial,
    TrialRecord record,
    TrialStanding standing,
    StandingsState board,
    Action submit
  )
  {
    Upload upload = Uploader.For(trial.Id);
    bool connected = Identity.Registered && board != StandingsState.Offline;
    bool submittable = record?.Submittable == true;
    bool submitting = upload.State == UploadState.Submitting;
    bool faster = standing?.Ticks is not long serverTicks || serverTicks > record?.BestTicks;
    bool live = connected && submittable && !submitting && faster;

    DimButton row = new(Dialog.Clean("climbtrials_trials_submit_pb"), live, submit);
    Add(row);
    string reason = null;
    if (!connected)
      reason = Dialog.Clean("climbtrials_submit_offline");
    else if (!submittable)
      reason = Dialog.Clean("climbtrials_submit_unsubmittable");
    else if (submitting)
      reason = Dialog.Clean("climbtrials_submitting");
    else if (!faster)
      reason = Dialog.Clean("climbtrials_submit_already");
    else if (upload.State == UploadState.Failed)
      reason = upload.Message;
    if (reason != null)
      RowDescription.Attach(this, row, reason);
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

  private static void Outline(string text, Vector2 at, float scale, Color color) =>
    ActiveFont.DrawOutline(
      text,
      at,
      new Vector2(0f, 0.5f),
      Vector2.One * scale,
      color,
      2f,
      TextOutline.StrokeFor(color)
    );

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
      Outline(trial.Name, at, TitleScale, Color.White * alpha);
    }
  }

  private class PbItem(TrialRecord record, TrialStanding standing) : Item
  {
    private readonly string label = standing?.Rank is int rank
      ? $"{Dialog.Clean("climbtrials_trials_pb")} #{rank}:"
      : Dialog.Clean("climbtrials_trials_pb");
    private readonly string time = TimeText.Format(record.BestTicks);
    private static TimeStyle Style => TimeText.Hud.At(PbScale);

    private float LabelWidth => ActiveFont.Measure(label).X * PbScale;

    public override float Height() => PbHeight;

    public override float LeftWidth() => LabelWidth + LabelGap + TimeText.Width(time, Style);

    public override void Render(Vector2 position, bool highlighted)
    {
      float alpha = Container.Alpha;
      float left = position.X + (Container.Width - LeftWidth()) / 2f;
      Outline(label, new Vector2(left, position.Y), PbScale, Color.White * alpha);
      Vector2 baseline = new(
        left + LabelWidth + LabelGap,
        position.Y + TimeText.Height(Style) / 2f
      );
      TimeText.Draw(baseline, time, Style, Color.White * alpha);
    }
  }

  private class NoteItem(string text, Color color) : Item
  {
    public override float Height() => NoteHeight;

    public override float LeftWidth() => ActiveFont.Measure(text).X * PbScale;

    public override void Render(Vector2 position, bool highlighted)
    {
      float left = position.X + (Container.Width - LeftWidth()) / 2f;
      Outline(text, new Vector2(left, position.Y), PbScale, color * Container.Alpha);
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
