using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

public class Countdown : Entity
{
  private int totalSeconds;
  private int remainingSeconds;
  private string startText = "GO";
  private string displayedText => remainingSeconds > 0 ? $"{remainingSeconds}" : startText;
  private Vector2 size => ActiveFont.Measure(displayedText);

  public Countdown() : base()
  {
    totalSeconds = ClimbTrialsModule.Settings.CountdownSeconds;
    remainingSeconds = totalSeconds;
    Tag = Tags.HUD;
    Add(new Coroutine(Routine()));
  }

  private IEnumerator Routine()
  {
    do
    {
      yield return 1f;
      remainingSeconds--;
    } while (remainingSeconds > 0);

    yield return 0.5f;
    Logger.Log(LogLevel.Info, "ClimbTrials/Countdown", "Countdown done.");

    RemoveSelf();
  }

  public override void Render()
  {
    if (Scene.Paused)
      return;

    Vector2 center = new Vector2(1920f, 1080f) / 2f;

    Draw.Rect(center.X - (size.X + 64f) * 0.5f, center.Y - (size.Y + 32f) * 0.5f, (size.X + 64f), (size.Y + 32f), Color.Black);

    Vector2 textPosition = center + new Vector2(-size.X * 0.5f, 0f);
    Color textColor = Color.White;
    ActiveFont.Draw(displayedText, textPosition, new Vector2(0f, 0.5f), new Vector2(1f, 1f), textColor);
  }
}