namespace Celeste.Mod.ClimbTrials.Content;

public static class Classics
{
  public static readonly Campaign Definition = new()
  {
    Id = "cl1",
    Name = "Classics",
    Description = "These might feel familiar.",
    Trials =
    [
      new Trial
      {
        Id = "cl1-1",
        Name = "3A Start Coins Room",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/3-CelestialResort",
          Mode = AreaMode.Normal,
          StartRoom = "06-c",
          Spawn = new(4208f, -328f),
          SpawnFacing = Facings.Right,
          FinishRoom = "08-c",
          Finish = new(4616, -320, 16, 8),
        },
        Goals = GoalTimes.FromSeconds(
          bronze: 7,
          silver: 6.5,
          gold: 6,
          diamond: 5.8,
          grandmaster: 5.6
        ),
      },
      new Trial
      {
        Id = "cl1-2",
        Name = "7B Flag 10",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/7-Summit",
          Mode = AreaMode.BSide,
          StartRoom = "g-01",
          Spawn = new(14336f, -13920f),
          FinishRoom = "g-01",
          Finish = new(14040, -14520, 32, 72),
        },
        Goals = GoalTimes.FromSeconds(
          bronze: 14,
          silver: 12,
          gold: 10,
          diamond: 9,
          grandmaster: 8.5
        ),
      },
    ],
  };
}
