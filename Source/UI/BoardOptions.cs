using Celeste.Mod.ClimbTrials.Online;

namespace Celeste.Mod.ClimbTrials.UI;

public sealed record BoardOptions(
  MenuKeys Keys,
  double MaxAgeSeconds,
  float BackdropAlpha,
  bool Comparing,
  int DepthBase,
  bool Slides
)
{
  private const double InLevelMaxAgeSeconds = 20;
  private const float InLevelBackdropAlpha = 0.7f;
  private const int InLevelDepthBase = -100000;

  public static readonly BoardOptions Overworld = new(
    MenuKeys.Live,
    Standings.FreshSeconds,
    0f,
    false,
    0,
    true
  );

  public static BoardOptions InLevel(MenuKeys keys, bool comparing) =>
    new(keys, InLevelMaxAgeSeconds, InLevelBackdropAlpha, comparing, InLevelDepthBase, false);
}
