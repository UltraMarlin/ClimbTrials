using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class GhostDeltaDisplay : Entity
{
  private const float HeadlineLeft = 16f;
  private const float HeadlineRightPad = 24f;
  private const float HeadlineTextBottom = 27f;
  private const float HeadlineScale = 0.45f;
  private const float NameMinFit = 0.65f;
  private const float DeltaTextBottom = 35f;
  private const float DeltaScale = 0.6f;
  private const float DeltaLeft = 16f;
  private const float DeltaBackgroundWidth = 231f;
  private const float DeltaChangeScale = 0.4f;
  private const float DeltaChangeStroke = 1.5f;
  private const float DeltaChangeGap = 10f;

  private static readonly TimeStyle HeadlineTime = TimeText.Hud.At(HeadlineScale) with
  {
    Stroke = TextOutline.DimStroke,
  };
  private static readonly TimeStyle DeltaStyle = TimeText.Hud.At(DeltaScale);
  private static readonly TimeStyle DeltaChangeStyle = TimeText.Hud.At(DeltaChangeScale) with
  {
    Stroke = DeltaChangeStroke,
  };

  private Replay headlineGhost;
  private string words;
  private float wordsScale;
  private string ghostTime;
  private float ghostTimeLeft;
  private float headlineWidth;
  private long? shownDelta;
  private string deltaText;
  private float deltaWidth;
  private long? shownDeltaChange;
  private string deltaChangeText;

  public GhostDeltaDisplay()
  {
    Tag = Tags.HUD | Tags.Persistent;
  }

  public override void Render()
  {
    SplitComparison comparison = RunManager.Comparison;
    if (!RunManager.GhostBlockVisible || comparison == null)
      return;
    float top = RunHudLayout.GhostBlockTop;
    RenderHeadline(comparison.Ghost.Replay, top);
    RenderDelta(
      comparison.TotalDelta,
      comparison.DeltaChange,
      top + RunHudLayout.HeadlineHeight - RunHudLayout.BackgroundOverlap
    );
  }

  private void RenderHeadline(Replay ghost, float top)
  {
    if (ghost != headlineGhost)
      BuildHeadline(ghost);
    TimerBackground.Draw(top, RunHudLayout.HeadlineHeight, headlineWidth);

    float bottom = top + HeadlineTextBottom;
    float baseline = bottom - TimeText.LineBelowBaseline * HeadlineScale;
    TextOutline.DrawDim(
      words,
      new Vector2(HeadlineLeft, baseline + TimeText.LineBelowBaseline * wordsScale),
      new Vector2(0f, 1f),
      wordsScale,
      1f
    );
    TimeText.Draw(new Vector2(ghostTimeLeft, bottom), ghostTime, HeadlineTime, Colors.DimText);
  }

  private void BuildHeadline(Replay ghost)
  {
    headlineGhost = ghost;
    string name = ghost.IsPersonalBest ? Dialog.Clean("climbtrials_pb") : Identity.NameOf(ghost);
    if (string.IsNullOrEmpty(name))
      name = Dialog.Clean("climbtrials_unnamed_ghost");
    string line = Dialog.Get("climbtrials_ghost_versus");
    words = line.Replace("{0}", name);
    wordsScale = HeadlineScale * TextFit.FitName(line, name, NameMinFit);
    ghostTime = TimeText.Format(ghost.Ticks);
    float space = ActiveFont.Measure(" ").X * HeadlineScale;
    ghostTimeLeft = HeadlineLeft + ActiveFont.Measure(words).X * wordsScale + space;
    headlineWidth = ghostTimeLeft + TimeText.Width(ghostTime, HeadlineTime) + HeadlineRightPad;
  }

  private void RenderDelta(long delta, long change, float top)
  {
    if (delta != shownDelta)
    {
      shownDelta = delta;
      deltaText = TimeText.FormatDelta(delta);
      deltaWidth = TimeText.Width(deltaText, DeltaStyle);
    }
    if (change != shownDeltaChange)
    {
      shownDeltaChange = change;
      deltaChangeText = TimeText.FormatDelta(change);
    }

    TimerBackground.Draw(top, RunHudLayout.DeltaHeight, DeltaBackgroundWidth);

    float bottom = top + DeltaTextBottom;
    Color color =
      delta < 0 ? Colors.Faster
      : delta > 0 ? Colors.Slower
      : Color.White;
    TimeText.Draw(new Vector2(DeltaLeft, bottom), deltaText, DeltaStyle, color);

    float baseline = bottom - TimeText.LineBelowBaseline * DeltaScale;
    float changeLeft = DeltaLeft + deltaWidth + DeltaChangeGap;
    Color changeColor =
      change < 0 ? Colors.FasterDim
      : change > 0 ? Colors.SlowerDim
      : Colors.DimText;
    TimeText.Draw(
      new Vector2(changeLeft, baseline + TimeText.LineBelowBaseline * DeltaChangeScale),
      deltaChangeText,
      DeltaChangeStyle,
      changeColor
    );
  }
}
