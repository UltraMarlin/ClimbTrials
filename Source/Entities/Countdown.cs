using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

public class Countdown : Entity
{
  private static readonly (float Beat, float Go)[] Pace =
  [
    (0.6f, 0.4f),
    (0.3f, 0.25f),
    (0.2f, 0.2f),
    (0.0f, 0.0f),
  ];
  public const string BeatSound = "event:/game/general/cassette_block_switch_1";
  private const string GoSound = "event:/game/general/cassette_block_switch_2";
  private static readonly Color GoColor = Calc.HexToColor("6ded87");
  private const float RestScale = 3f,
    PeakScale = 5.0f;

  private readonly Action onDone;
  private readonly float beat,
    go;
  private int remaining;
  private float beatAge;
  private bool showingGo;
  private bool countdownEnabled;
  private Level level;

  public Countdown(Action onDone)
    : base()
  {
    this.onDone = onDone;
    countdownEnabled = ClimbTrialsModule.Settings.CountdownMode != CountdownMode.Off;
    (beat, go) = Pace[(int)ClimbTrialsModule.Settings.CountdownMode];
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
    if (!countdownEnabled)
    {
      CountdownDone();
      RemoveSelf();
      yield break;
    }

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
    float t = Math.Clamp(beatAge / (beat * 0.6f), 0f, 1f);
    float scale = PeakScale - (PeakScale - RestScale) * Ease.CubeOut(t);
    string text = showingGo ? "GO" : remaining.ToString();
    ActiveFont.DrawOutline(
      text,
      new Vector2(960f, 324f),
      new Vector2(0.5f, 0.5f),
      Vector2.One * scale,
      showingGo ? GoColor : Color.White,
      4f,
      Color.Black
    );
  }

  public static void SetWorldAudioPaused(bool paused)
  {
    Audio.BusPaused(Buses.CHAPTERS, paused);
    Audio.BusPaused(Buses.AMBIENCE, paused);
  }
}
