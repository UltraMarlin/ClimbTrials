using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public sealed class TransientText
{
  private const float FadeTime = 0.5f;

  private string text;
  private Color color;
  private float duration;
  private float age = -1f;

  public void Show(string shown, float seconds, bool warning = true)
  {
    text = shown;
    duration = seconds;
    age = 0f;
    color = warning ? Colors.Warning : Color.White;

    if (warning)
      Audio.Play(SFX.ui_main_button_invalid);
  }

  public void Clear() => age = -1f;

  public void Update(float deltaTime)
  {
    if (age >= 0f)
      age += deltaTime;
  }

  public void Render(Vector2 center, float scale, float alpha)
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
      TextOutline.StrokeFor(shown)
    );
  }
}
