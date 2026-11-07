using System;
using System.Collections;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

public class Countdown : Entity
{
  private static (float Beat, float Go) PaceFor(CountdownMode mode)
  {
    return mode switch
    {
      CountdownMode.Slow => (0.6f, 0.4f),
      CountdownMode.Fast => (0.35f, 0.25f),
      CountdownMode.VeryFast => (0.2f, 0.2f),
      _ => (0.35f, 0.25f),
    };
  }

  private const string BeatSound = SFX.game_gen_cassetteblock_switch_1;
  private const string GoSound = SFX.game_gen_cassetteblock_switch_2;
  private static readonly Color GoColor = Calc.HexToColor("6ded87");
  private const float RestScale = 3f,
    PeakScale = 5.0f;

  private readonly Action onDone;
  private readonly float beat,
    go;
  private int remaining;
  private float beatAge;
  private bool showingGo;
  private Level level;

  public Countdown(Action onDone)
    : base()
  {
    this.onDone = onDone;
    (beat, go) = PaceFor(ClimbTrialsModule.Settings.CountdownMode);
    remaining = ClimbTrialsModule.Settings.CountdownLength;
    Tag = Tags.HUD | Tags.FrozenUpdate;
    Add(new Coroutine(Routine()));
  }

  public override void Added(Scene scene)
  {
    base.Added(scene);
    level = SceneAs<Level>();
    level.Frozen = true;
    SetWorldAudioPaused(true);
  }

  private void CountdownDone()
  {
    level.Frozen = false;
    SetWorldAudioPaused(false);
    onDone?.Invoke();
  }

  private IEnumerator Routine()
  {
    while (remaining > 0)
    {
      beatAge = 0f;
      Audio.Play(BeatSound);
      yield return beat;
      remaining--;
    }

    showingGo = true;
    beatAge = 0f;
    Audio.Play(GoSound);
    CountdownDone();

    yield return go;
    RemoveSelf();
  }

  public override void Update()
  {
    base.Update();
    beatAge += Engine.RawDeltaTime;
  }

  public override void Render()
  {
    if (Scene.Paused)
      return;
    float fade = Calc.ClampedMap(beatAge, 0f, beat * 0.6f);
    float scale = PeakScale - (PeakScale - RestScale) * Ease.CubeOut(fade);
    string text = showingGo ? "GO" : remaining.ToString();
    Color color = showingGo ? GoColor : Color.White;
    TextOutline.DrawRoundOutline(
      text,
      new Vector2(960f, 324f),
      new Vector2(0.5f, 0.5f),
      Vector2.One * scale,
      color,
      4f,
      TextOutline.StrokeFor(color)
    );
  }

  public static void SetWorldAudioPaused(bool paused)
  {
    Audio.BusPaused(Buses.CHAPTERS, paused);
    Audio.BusPaused(Buses.AMBIENCE, paused);
  }
}
