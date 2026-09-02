using System;
using System.Collections;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

public class FinishSequence : Entity
{
  private const float FreezeDelay = 0.2f;
  private const float TimeFadeDuration = 0.3f;
  private const float NoPanelPromptDelay = 0.4f;
  private const float PromptFadeDuration = 0.3f;
  private const float TimeBaselineY = 470f;
  private const float TimeScale = 2.8f;
  private const float DeltaBaselineY = 542f;
  private const float DeltaScale = 1.4f;
  private const float PromptY = 1000f;
  private const float MedalDelay = 0.2f;
  private const float MedalPopDuration = 2f;
  private const float MedalY = 640f;
  private const float MedalHeight = 256f;
  private const string MedalSound = "event:/ui/main/postcard_ch1_in";

  private float medalAge = -1f;
  private static readonly Color DeltaColor = Calc.HexToColor("6ded87");

  private readonly FinishResult result;
  private Level level;
  private float timeAge = -1f;
  private float promptAge = -1f;

  public FinishSequence(FinishResult result)
  {
    this.result = result;
    Tag = Tags.HUD | Tags.FrozenUpdate;
    Add(new Coroutine(Routine()));
  }

  public override void Added(Scene scene)
  {
    base.Added(scene);
    level = SceneAs<Level>();
  }

  private IEnumerator Routine()
  {
    yield return FreezeDelay;
    level.Frozen = true;
    if (result.IsPersonalBest)
    {
      timeAge = 0f;
      yield return TimeFadeDuration;
      if (result.NewMedal != Medal.None)
      {
        Audio.Play(MedalSound);
        yield return MedalDelay;
        medalAge = 0f;
        yield return MedalPopDuration;
      }
    }
    else
    {
      yield return NoPanelPromptDelay;
    }
    promptAge = 0f;
  }

  public override void Update()
  {
    base.Update();
    if (RunManager.Attempt.State != AttemptState.Finished)
    {
      level.Frozen = false;
      RemoveSelf();
      return;
    }

    if (timeAge >= 0f)
      timeAge += Engine.RawDeltaTime;

    if (medalAge >= 0f)
      medalAge += Engine.RawDeltaTime;

    if (promptAge >= 0f)
      promptAge += Engine.RawDeltaTime;

    if (promptAge < 0f || level.Paused)
      return;

    if (Input.MenuConfirm.Pressed || ClimbTrialsModule.Settings.Retry.Pressed)
    {
      RunManager.StartRun(RunManager.Run);
      return;
    }

    if (Input.MenuCancel.Pressed)
      UI.PauseMenu.ReturnToTrials();
  }

  public override void Render()
  {
    if (level.Paused)
      return;
    if (timeAge >= 0f)
      RenderResult();
    if (medalAge >= 0f)
      RenderMedal();
    if (promptAge >= 0f)
      RenderPrompts();
  }

  private void RenderResult()
  {
    float alpha = Ease.CubeOut(Math.Clamp(timeAge / TimeFadeDuration, 0f, 1f));
    string time = Attempt.Format(result.Ticks);
    float width = TimeText.Width(time, TimeScale);
    TimeText.Draw(
      new Vector2(960f - width / 2f, TimeBaselineY),
      time,
      TimeScale,
      Color.White * alpha,
      Color.LightGray * alpha
    );
    if (result.PreviousBestTicks is not long previous)
      return;
    string delta = "-" + Attempt.Format(previous - result.Ticks);
    float deltaWidth = TimeText.Width(delta, DeltaScale);
    TimeText.Draw(
      new Vector2(960f - deltaWidth / 2f, DeltaBaselineY),
      delta,
      DeltaScale,
      DeltaColor * alpha,
      DeltaColor * 0.7f * alpha
    );
  }

  private void RenderMedal()
  {
    float t = Math.Clamp(medalAge / MedalPopDuration, 0f, 1f);
    float pop = 1f + 0.6f * (1f - Ease.CubeOut(t));
    MedalIcon.Draw(new Vector2(960f, MedalY), result.NewMedal, MedalHeight * pop);
  }

  private void RenderPrompts()
  {
    float alpha = Ease.CubeOut(Math.Clamp(promptAge / PromptFadeDuration, 0f, 1f));
    DrawPrompt(
      Input.MenuCancel,
      Dialog.Clean("climbtrials_finish_back"),
      new Vector2(120f, PromptY),
      rightAlign: false,
      alpha
    );
    DrawPrompt(
      Input.MenuConfirm,
      Dialog.Clean("climbtrials_finish_retry"),
      new Vector2(1800f, PromptY),
      rightAlign: true,
      alpha
    );
  }

  private static void DrawPrompt(
    VirtualButton button,
    string label,
    Vector2 iconCenter,
    bool rightAlign,
    float alpha
  )
  {
    MTexture icon = Input.GuiButton(button, Input.PrefixMode.Latest);
    icon.DrawCentered(iconCenter, Color.White * alpha, Vector2.One);
    float gap = icon.Width / 2f + 16f;
    Vector2 textPosition = iconCenter + new Vector2(rightAlign ? -gap : gap, 0f);
    ActiveFont.DrawOutline(
      label,
      textPosition,
      new Vector2(rightAlign ? 1f : 0f, 0.5f),
      Vector2.One * 0.8f,
      Color.White * alpha,
      2f,
      Color.Black * alpha
    );
  }
}
