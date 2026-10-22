using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class RowLayout
{
  public const float RowHeight = 90f;
  public const float IconX = 28f;
  public const float NameX = 86f;
  public const float CampaignNameX = 0f;
  public const float PbScale = 0.85f;
  public const float PbBaseline = 30f;
  public const float MedalGap = 46f;
  public const float MedalIconHeight = 64f;
  public const float RankScale = 0.6f;
  public const float RankLift = 3f;
  public const string RankSlotTime = "0:00.000";
  public const float HoveredRowShift = 32f;
  public const float CountGap = 24f;
  public const float CountScale = 0.8f;
  public const float WordScale = 0.8f;
  public const float MarkGap = 12f;
  public const string Mark = "!";
  public const float EventRowHeight = 140f;
  public const float TimingLineY = 90f;
  public const float TimingScale = 0.6f;
  public static readonly Color Dim = Color.White * 0.7f;

  public static float MarkWidth => ActiveFont.Measure(Mark).X * PbScale;

  public static float RightColumnWidth =>
    HoveredRowShift
    + MedalIconHeight
    + MedalGap
    + TimeText.Width(RankSlotTime, TimeText.Hud.At(PbScale))
    + MarkGap
    + MarkWidth;
}
