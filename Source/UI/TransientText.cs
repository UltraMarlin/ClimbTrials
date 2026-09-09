using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public sealed class TransientText
{
  private const float FadeTime = 0.5f;
  private const string Sound = "event:/ui/main/button_invalid";

  private string text;
  private float duration;
  private float age = -1f;

  public void Show(string shown, float seconds)
  {
    text = shown;
    duration = seconds;
    age = 0f;
    Audio.Play(Sound);
  }

  public void Clear() => age = -1f;

  public void Update(float deltaTime)
  {
    if (age >= 0f)
      age += deltaTime;
  }

  public void Render(Vector2 center, float scale, Color color, float alpha)
  {
    if (age < 0f || age >= duration)
      return;
    Color shown = color * (alpha * Math.Clamp((duration - age) / FadeTime, 0f, 1f));
    ActiveFont.DrawOutline(
      text,
      center,
      new Vector2(0.5f, 0.5f),
      Vector2.One * scale,
      shown,
      2f,
      TimeText.StrokeFor(shown)
    );
  }
}
