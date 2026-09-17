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
  private readonly Color boxColor = Colors.ForChapter(RunManager.Run.Area).Dark;
  private const float MedalsVisualDelay = 0.36f;
  private const float MedalStagger = 0.3f;
  private const float MedalPopDuration = 0.56f;
  private const float MedalY = 640f;
  private const float MedalHeight = 256f;
  private const float MedalSpacing = 110f;
  private const string BronzeMedalSound = "event:/game/07_summit/gem_unlock_1";
  private const string SilverMedalSound = "event:/game/07_summit/gem_unlock_3";
  private const string GoldMedalSound = "event:/game/07_summit/gem_unlock_4";
  private const string DiamondMedalSound = "event:/game/07_summit/gem_unlock_5";
  private const string MasterMedalSound = "event:/game/07_summit/gem_unlock_6";
  private const string PbSound = "event:/ui/postgame/strawberry_total";
  private const string FinishSound = "event:/game/07_summit/checkpoint_confetti";
  private const float ConfettiLift = 8f;

  private static readonly Color DeltaColor = Calc.HexToColor("6ded87");

  private const float GamePixel = 6f;
  private const float SparkSpeedMin = 70f;
  private const float SparkLifeMin = 0.4f;
  private const float SparkSlowdown = 0.06f;
  private const float SparkFadeShare = 0.25f;
  private const float SparkBlinkInterval = 0.2f;
  private const float SparkLargeShare = 0.3f;

  private struct Spark
  {
    public Vector2 Position;
    public Vector2 Speed;
    public float Life;
    public float StartLife;
    public Color Color;
    public bool Blink;
    public int Size;
  }

  private readonly FinishResult result;
  private readonly Medal[] medals;
  private readonly float[] medalAges;

  private readonly List<Spark>[] bursts;

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
    bursts = new List<Spark>[medals.Length];
    for (int i = 0; i < bursts.Length; i++)
      bursts[i] = [];
    Tag = Tags.HUD | Tags.FrozenUpdate | Tags.Persistent;
    Add(new Coroutine(Routine()));
  }

  public override void Added(Scene scene)
  {
    base.Added(scene);
    level = SceneAs<Level>();
  }

  public static void Confetti(Level level, Player player)
  {
    Vector2 position = player.TopCenter + new Vector2(0f, -ConfettiLift);
    Audio.Play(FinishSound, position);
    SummitCheckpoint.ConfettiRenderer confetti = new(position);
    confetti.Tag |= Tags.FrozenUpdate | Tags.Persistent | Tags.TransitionUpdate;
    confetti.Depth = player.Depth - 1;
    level.Add(confetti);
  }

  public static void Begin(Level level, Player player)
  {
    player.StateMachine.State = Player.StDummy;
    if (RunManager.LastFinish != null)
      level.Add(new FinishSequence(RunManager.LastFinish));
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
    {
      if (medalAges[i] < 0f)
        continue;
      float before = medalAges[i];
      medalAges[i] += Engine.RawDeltaTime;
      if (before < MedalPopDuration && medalAges[i] >= MedalPopDuration)
        Burst(i);
      UpdateSparks(bursts[i]);
    }

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
      RunManager.LeaveToTrials();
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
    string time = TimeText.Format(result.Ticks);
    string delta = result.PreviousBestTicks is long previous
      ? "-" + TimeText.Format(previous - result.Ticks)
      : null;
    float deltaAge = timeAge - DeltaStagger;

    float boxAlpha = BoxAlpha * Ease.CubeOut(Math.Clamp(timeAge / MedalPopDuration, 0f, 1f));
    float boxWidth = TimeText.Width(time, TimeText.Large) + 2f * BoxPaddingX;
    float boxTop = TimeBaselineY - TimeText.Height(TimeText.Large) - BoxPaddingTop;
    float boxBottom = (delta == null ? TimeBaselineY : DeltaBaselineY) + BoxPaddingBottom;
    Draw.Rect(960f - boxWidth / 2f, boxTop, boxWidth, boxBottom - boxTop, boxColor * boxAlpha);

    float alpha = Ease.CubeOut(Math.Clamp(timeAge / TimeFadeDuration, 0f, 1f));
    DrawAnimated(time, TimeText.Large, TimeBaselineY, timeAge, Color.White * alpha);

    if (delta == null || deltaAge < 0f)
      return;
    float deltaAlpha = Ease.CubeOut(Math.Clamp(deltaAge / TimeFadeDuration, 0f, 1f));
    DrawAnimated(delta, TimeText.Hud, DeltaBaselineY, deltaAge, DeltaColor * deltaAlpha);
  }

  private static void DrawAnimated(
    string text,
    TimeStyle style,
    float baselineY,
    float age,
    Color color
  )
  {
    TimeStyle popped = style.At(style.Scale * Pop(age));
    float width = TimeText.Width(text, popped);
    TimeText.Draw(new Vector2(960f - width / 2f, baselineY), text, popped, color);
  }

  private void RenderMedals()
  {
    for (int i = 0; i < medals.Length; i++)
    {
      if (medalAges[i] < 0f)
        continue;
      RenderSparks(bursts[i]);
      MedalIcon.Draw(MedalCenter(i), medals[i], MedalHeight * Pop(medalAges[i]));
    }
  }

  private Vector2 MedalCenter(int i)
  {
    float firstX = 960f - (medals.Length - 1) * MedalSpacing / 2f;
    return new Vector2(firstX + i * MedalSpacing, MedalY);
  }

  private void Burst(int i)
  {
    Medal medal = medals[i];
    MedalIcon.Burst burst = MedalIcon.BurstFor(medal);
    Vector2 center = MedalCenter(i) / GamePixel;
    Color tint = MedalIcon.Tint(medal);
    Color shine = MedalIcon.Shine(medal);
    for (int n = 0; n < burst.Count; n++)
      bursts[i].Add(NewSpark(center, burst, Calc.Random.Choose(tint, shine), false));
    for (int n = 0; n < burst.ShineCount; n++)
      bursts[i].Add(NewSpark(center, burst, shine, true));
  }

  private static Spark NewSpark(Vector2 center, MedalIcon.Burst burst, Color color, bool blink)
  {
    float life = Calc.Random.Range(SparkLifeMin, burst.LifeMax);
    float speed = Calc.Random.Range(SparkSpeedMin, burst.SpeedMax);
    return new Spark
    {
      Position = center,
      Speed = Calc.AngleToVector(Calc.Random.NextAngle(), speed),
      Life = life,
      StartLife = life,
      Color = color,
      Blink = blink,
      Size = Calc.Random.Chance(SparkLargeShare) ? 2 : 1,
    };
  }

  private static void UpdateSparks(List<Spark> sparks)
  {
    float dt = Engine.RawDeltaTime;
    float decay = (float)Math.Pow(SparkSlowdown, dt);
    for (int n = sparks.Count - 1; n >= 0; n--)
    {
      Spark spark = sparks[n];
      spark.Life -= dt;
      if (spark.Life <= 0f)
      {
        sparks.RemoveAt(n);
        continue;
      }
      spark.Speed *= decay;
      spark.Position += spark.Speed * dt;
      sparks[n] = spark;
    }
  }

  private static void RenderSparks(List<Spark> sparks)
  {
    foreach (Spark spark in sparks)
    {
      float remaining = spark.Life / spark.StartLife;
      float alpha = Math.Min(1f, remaining / SparkFadeShare);
      bool white = spark.Blink && Calc.BetweenInterval(spark.Life, SparkBlinkInterval);
      Color color = (white ? Color.White : spark.Color) * alpha;
      float x = MathF.Floor(spark.Position.X) * GamePixel;
      float y = MathF.Floor(spark.Position.Y) * GamePixel;
      float size = spark.Size * GamePixel;
      Draw.Rect(x, y, size, size, color);
    }
  }

  private static string SoundFor(Medal medal) =>
    medal switch
    {
      Medal.Bronze => BronzeMedalSound,
      Medal.Silver => SilverMedalSound,
      Medal.Gold => GoldMedalSound,
      Medal.Diamond => DiamondMedalSound,
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
