using System;

namespace Celeste.Mod.ClimbTrials.Content;

public enum Medal
{
  None,
  Bronze,
  Silver,
  Gold,
  Grandmaster,
}

public sealed class GoalTimes
{
  public long BronzeTicks { get; init; }
  public long SilverTicks { get; init; }
  public long GoldTicks { get; init; }
  public long GrandmasterTicks { get; init; }

  public static GoalTimes FromSeconds(
    double bronze,
    double silver,
    double gold,
    double grandmaster
  ) =>
    new()
    {
      BronzeTicks = Ticks(bronze),
      SilverTicks = Ticks(silver),
      GoldTicks = Ticks(gold),
      GrandmasterTicks = Ticks(grandmaster),
    };

  private static long Ticks(double seconds) => (long)(seconds * TimeSpan.TicksPerSecond);

  public Medal MedalFor(long ticks)
  {
    if (ticks <= GrandmasterTicks)
      return Medal.Grandmaster;
    if (ticks <= GoldTicks)
      return Medal.Gold;
    if (ticks <= SilverTicks)
      return Medal.Silver;
    if (ticks <= BronzeTicks)
      return Medal.Bronze;
    return Medal.None;
  }
}
