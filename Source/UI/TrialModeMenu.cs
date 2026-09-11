using System;
using Celeste.Mod.ClimbTrials.Content;
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
  private const float DateScale = 0.45f;
  private const float DateHeight = 32f;
  private const float OptionsGap = 40f;
  private const float IconGap = 16f;
  private const float LabelGap = 16f;

  private readonly Action closed;

  public TrialModeMenu(Trial trial, Action play, Action watch, Action race, Action closed)
  {
    this.closed = closed;
    Position.Y -= MenuLift;

    Add(new TitleItem(trial));
    if (TrialRecords.Get(trial.Id) is TrialRecord record)
    {
      Add(new PbItem(trial, record));
      Add(new DateItem(record));
    }
    Add(new Gap(OptionsGap));
    Add(new Button(Dialog.Clean("climbtrials_trials_play")).Pressed(play));
    Add(new Button(Dialog.Clean("climbtrials_trials_watch_pb")).Pressed(watch));
    Add(new Button(Dialog.Clean("climbtrials_trials_race_pb")).Pressed(race));

    OnCancel = Back;
    OnESC = Back;
    OnPause = Back;
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
      TimeText.StrokeFor(color)
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

  private class PbItem(Trial trial, TrialRecord record) : Item
  {
    private readonly string label = Dialog.Clean("climbtrials_trials_pb");
    private readonly string time = TimeText.Format(record.BestTicks);
    private readonly Medal medal = trial.Goals?.MedalFor(record.BestTicks) ?? Medal.None;
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

  private class DateItem(TrialRecord record) : Item
  {
    private readonly string text =
      $"{Dialog.Clean("climbtrials_replay_played_date")} {TimeText.FormatDate(record.AchievedAtUtc)}";

    public override float Height() => DateHeight;

    public override float LeftWidth() => ActiveFont.Measure(text).X * DateScale;

    public override void Render(Vector2 position, bool highlighted)
    {
      float left = position.X + (Container.Width - LeftWidth()) / 2f;
      Outline(text, new Vector2(left, position.Y), DateScale, RowLayout.Dim * Container.Alpha);
    }
  }

  private class Gap(float height) : Item
  {
    public override float Height() => height;

    public override void Render(Vector2 position, bool highlighted) { }
  }
}
