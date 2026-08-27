using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

using Celeste.Mod.ClimbTrials.Runs;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class RunTimerDisplay : Entity
{
  private const float TimerY = 60f;
  private const float TextHeight = 38f, TextMarginLeft = 32f, Baseline = 44f;
  private static readonly Color RunColor1 = Color.White;
  private static readonly Color RunColor2 = Color.LightGray;
  private static readonly Color DoneColor1 = Calc.HexToColor("fad768");
  private static readonly Color DoneColor2 = Calc.HexToColor("cfa727");

  private float drawLerp;
  private float numberWidth, spacerWidth;

  public RunTimerDisplay() : base()
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate;
  }

  public override void Added(Scene scene)
  {
    base.Added(scene);
    PixelFontSize size = Dialog.Languages["english"].Font.Get(Dialog.Languages["english"].FontFaceSize);
    numberWidth = "0123456789".Max(c => size.Measure(c.ToString()).X);
    spacerWidth = size.Measure(":").X;
  }

  private static bool ShouldShow => RunManager.Attempt.State switch
  {
    AttemptState.Finished => true,
    AttemptState.Running => ClimbTrialsModule.Settings.ShowTimerWhileRunning,
    _ => false,
  };

  public override void Update()
  {
    base.Update();
    drawLerp = Calc.Approach(drawLerp, ShouldShow ? 1f : 0f, Engine.DeltaTime * 4f);
  }

  public override void Render()
  {
    if (drawLerp <= 0f) return;
    Attempt attempt = RunManager.Attempt;
    string text = Attempt.Format(attempt.ElapsedTicks);
    float x = -300f * Ease.CubeIn(1f - drawLerp);

    MTexture bg = GFX.Gui["strawberryCountBG"];
    float textWidth = text.Sum(c => Advance(c));
    float extra = System.Math.Max(0f, textWidth + TextMarginLeft + 8f - bg.Width);
    Draw.Rect(x, TimerY, extra + 2f, TextHeight, Color.Black);
    bg.Draw(new Vector2(x + extra, TimerY));

    bool done = attempt.State == AttemptState.Finished;
    DrawTime(new Vector2(x + TextMarginLeft, TimerY + Baseline), text, done ? DoneColor1 : RunColor1, done ? DoneColor2 : RunColor2);
  }

  private float Advance(char c) => (c is ':' or '.' ? spacerWidth : numberWidth) + 4f;

  private void DrawTime(Vector2 position, string text, Color digits, Color separators)
  {
    PixelFont font = Dialog.Languages["english"].Font;
    float faceSize = Dialog.Languages["english"].FontFaceSize;
    float x = position.X, y = position.Y, scale = 1f;
    foreach (char c in text)
    {
      if (c == '.') { scale = 0.7f; y -= 5f; }
      float w = Advance(c) * scale;
      font.DrawOutline(faceSize, c.ToString(), new Vector2(x + w / 2f, y), new Vector2(0.5f, 1f),
        Vector2.One * scale, (c is ':' or '.') ? separators : digits, 2f, Color.Black);
      x += w;
    }
  }
}