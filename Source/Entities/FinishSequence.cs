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
  private const float NoPanelPromptDelay = 0.4f;
  private const float PromptFadeDuration = 0.3f;
  private const float TimeBaselineY = 470f;
  private const float TimeScale = 2.8f;
  private const float DeltaBaselineY = 542f;
  private const float DeltaScale = 1.4f;
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
  private Level level;
  private float timeAge = -1f;
  private float promptAge = -1f;

  public FinishSequence(FinishResult result)
  {
    this.result = result;
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
    if (level.Paused)
      return;
    if (timeAge >= 0f)
      RenderResult();
    RenderMedals();
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

  private void RenderMedals()
  {
    float firstX = 960f - (medals.Length - 1) * MedalSpacing / 2f;
    for (int i = 0; i < medals.Length; i++)
    {
      if (medalAges[i] < 0f)
        continue;
      float t = Math.Clamp(medalAges[i] / MedalPopDuration, 0f, 1f);
      float pop = 1f + 0.6f * (1f - Ease.BackIn(t));
      Vector2 center = new(firstX + i * MedalSpacing, MedalY);
      MedalIcon.Draw(center, medals[i], MedalHeight * pop);
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
