using Celeste.Mod.ClimbTrials.Online;

namespace Celeste.Mod.ClimbTrials.UI;

public sealed record BoardOptions(
  MenuKeys Keys,
  double MaxAgeSeconds,
  float BackdropAlpha,
  bool Comparing,
  int DepthBase,
  bool InsideLevel,
  bool Plans = false
)
{
  private const double InLevelMaxAgeSeconds = 20;
  private const int InLevelDepthBase = -100000;
  private const int OverworldDepthBase = -10;

  public static readonly BoardOptions Overworld = new(
    MenuKeys.Live,
    Standings.FreshSeconds,
    Motion.BoardBackdropAlpha,
    false,
    OverworldDepthBase,
    false
  );

  public static BoardOptions InLevel(MenuKeys keys, bool comparing, bool plans = false) =>
    new(
      keys,
      InLevelMaxAgeSeconds,
      Motion.LevelBoardBackdropAlpha,
      comparing,
      InLevelDepthBase,
      true,
      plans
    );
}
