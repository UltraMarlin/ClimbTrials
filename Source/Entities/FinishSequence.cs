using System;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

public class FinishSequence : Entity
{
  private const float FreezeDelay = 0.5f;
  private const float TimeFadeDuration = 0.1f;
  private const float PromptFadeDuration = 0.3f;
  private const float TimeBaselineY = 470f;
  private const float DeltaBaselineY = 500f;
  private const float DeltaStagger = 0.1f;
  private const float BoxPaddingX = 80f;
  private const float BoxPaddingTop = 16f;
  private const float BoxPaddingBottom = 20f;
  private const float BoxAlpha = 0.7f;
  private const float MedalsVisualDelay = 0.36f;
  private const float MedalStagger = 0.3f;
  private const float MedalPopDuration = 0.56f;
  private const float MedalY = 640f;
  private const float MedalHeight = 256f;
  private const float MedalSpacing = 110f;
  private const string BronzeMedalSound = "event:/game/07_summit/gem_unlock_1";
  private const string SilverMedalSound = "event:/game/07_summit/gem_unlock_3";
  private const string GoldMedalSound = "event:/game/07_summit/gem_unlock_5";
  private const string MasterMedalSound = "event:/game/07_summit/gem_unlock_6";
  private const string PbSound = "event:/ui/postgame/strawberry_total";

  private static readonly Color DeltaColor = Calc.HexToColor("6ded87");

  private readonly FinishResult result;
  private readonly Medal[] medals;
  private readonly float[] medalAges;

  private readonly bool watcher;
  private Level level;
  private float timeAge = -1f;
  private float promptAge = -1f;

  public FinishSequence(FinishResult result)
  {
    this.result = result;
    watcher = RunManager.Replaying;
    medals = [.. result.UnlockedMedals()];
    medalAges = new float[medals.Length];
    Array.Fill(medalAges, -1f);
    Tag = Tags.HUD | Tags.FrozenUpdate | Tags.Persistent;
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
    if (watcher)
      yield break;
    if (result.IsPersonalBest)
    {
      Audio.Play(PbSound);
      timeAge = 0f;
      yield return TimeFadeDuration;
      if (medals.Length > 0)
      {
        for (int i = 0; i < medals.Length; i++)
        {
          if (i > 0)
            yield return MedalStagger;
          Audio.Play(SoundFor(medals[i]));
          yield return MedalsVisualDelay;
          medalAges[i] = 0f;
        }
        yield return MedalPopDuration / 2;
      }
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
    if (watcher)
      return;

    if (timeAge >= 0f)
      timeAge += Engine.RawDeltaTime;

    for (int i = 0; i < medalAges.Length; i++)
      if (medalAges[i] >= 0f)
        medalAges[i] += Engine.RawDeltaTime;

    if (promptAge >= 0f)
      promptAge += Engine.RawDeltaTime;

    if (promptAge < 0f || level.Paused)
      return;

    if (Input.MenuConfirm.Pressed || ClimbTrialsModule.Settings.Retry.Pressed)
    {
      RunManager.StartRun(RunManager.Run);
      return;
    }

    if (Input.MenuJournal.Pressed && RunManager.LastReplay != null)
    {
      Audio.Play("event:/ui/main/button_select");
      RunManager.StartReplay(RunManager.LastReplay);
      return;
    }

    if (Input.MenuCancel.Pressed)
      PauseMenu.ReturnToTrials();
  }

  public override void Render()
  {
    if (watcher || level.Paused)
      return;
    if (timeAge >= 0f)
      RenderResult();
    RenderMedals();
    if (promptAge >= 0f)
      RenderPrompts();
  }

  private static float Pop(float age) =>
    1f + 0.6f * (1f - Ease.BackIn(Math.Clamp(age / MedalPopDuration, 0f, 1f)));

  private void RenderResult()
  {
    string time = Attempt.Format(result.Ticks);
    string delta = result.PreviousBestTicks is long previous
      ? "-" + Attempt.Format(previous - result.Ticks)
      : null;
    float deltaAge = timeAge - DeltaStagger;

    float boxAlpha = BoxAlpha * Ease.CubeOut(Math.Clamp(timeAge / MedalPopDuration, 0f, 1f));
    float boxWidth = TimeText.Width(time, TimeText.Large) + 2f * BoxPaddingX;
    float boxTop = TimeBaselineY - TimeText.Height(TimeText.Large) - BoxPaddingTop;
    float boxBottom = (delta == null ? TimeBaselineY : DeltaBaselineY) + BoxPaddingBottom;
    Draw.Rect(960f - boxWidth / 2f, boxTop, boxWidth, boxBottom - boxTop, Color.Black * boxAlpha);

    float alpha = Ease.CubeOut(Math.Clamp(timeAge / TimeFadeDuration, 0f, 1f));
    DrawAnimated(
      time,
      TimeText.Large,
      TimeBaselineY,
      timeAge,
      Color.White * alpha,
      Color.LightGray * alpha
    );

    if (delta == null || deltaAge < 0f)
      return;
    float deltaAlpha = Ease.CubeOut(Math.Clamp(deltaAge / TimeFadeDuration, 0f, 1f));
    DrawAnimated(
      delta,
      TimeText.Hud,
      DeltaBaselineY,
      deltaAge,
      DeltaColor * deltaAlpha,
      DeltaColor * 0.7f * deltaAlpha
    );
  }

  private static void DrawAnimated(
    string text,
    TimeStyle style,
    float baselineY,
    float age,
    Color digits,
    Color separators
  )
  {
    TimeStyle popped = style.At(style.Scale * Pop(age));
    float width = TimeText.Width(text, popped);
    TimeText.Draw(new Vector2(960f - width / 2f, baselineY), text, popped, digits, separators);
  }

  private void RenderMedals()
  {
    float firstX = 960f - (medals.Length - 1) * MedalSpacing / 2f;
    for (int i = 0; i < medals.Length; i++)
    {
      if (medalAges[i] < 0f)
        continue;
      Vector2 center = new(firstX + i * MedalSpacing, MedalY);
      MedalIcon.Draw(center, medals[i], MedalHeight * Pop(medalAges[i]));
    }
  }

  private static string SoundFor(Medal medal) =>
    medal switch
    {
      Medal.Bronze => BronzeMedalSound,
      Medal.Silver => SilverMedalSound,
      Medal.Gold => GoldMedalSound,
      Medal.Grandmaster => MasterMedalSound,
      _ => PbSound,
    };

  private void RenderPrompts()
  {
    float alpha = Ease.CubeOut(Math.Clamp(promptAge / PromptFadeDuration, 0f, 1f));
    List<(string, VirtualButton)> prompts =
    [
      (Dialog.Clean("climbtrials_finish_back"), Input.MenuCancel),
      (Dialog.Clean("climbtrials_finish_retry"), Input.MenuConfirm),
    ];
    if (RunManager.LastReplay != null)
      prompts.Add((Dialog.Clean("climbtrials_finish_watch"), Input.MenuJournal));
    ButtonRow.DrawRight(alpha, prompts);
  }
}
