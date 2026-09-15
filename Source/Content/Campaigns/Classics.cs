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
        Name = "Resort Start Coins Room",
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
        Name = "Big Chaos",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/3-CelestialResort",
          Mode = AreaMode.Normal,
          StartRoom = "08-a",
          Spawn = new(4360f, 0f),
          FinishRoom = "09-b",
          Finish = new(5248, 52, 32, 16),
        },
        Goals = GoalTimes.FromSeconds(
          bronze: 20,
          silver: 18,
          gold: 16.4,
          diamond: 15.5,
          grandmaster: 15
        ),
      },
      new Trial
      {
        Id = "cl1-3",
        Name = "Depths Cornerboost",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/5-MirrorTemple",
          Mode = AreaMode.Normal,
          StartRoom = "b-00",
          Spawn = new(2296f, 32f),
          FinishRoom = "b-01b",
          Finish = new(3832, -240, 16, 32),
        },
        Goals = GoalTimes.FromSeconds(
          bronze: 15,
          silver: 13,
          gold: 11,
          diamond: 10,
          grandmaster: 9.6
        ),
      },
      new Trial
      {
        Id = "cl1-4",
        Name = "7A 1000M the fun part",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/7-Summit",
          Mode = AreaMode.Normal,
          StartRoom = "e-02",
          Spawn = new(15120f, -8888f),
          SpawnFacing = Facings.Right,
          FinishRoom = "e-05",
          Finish = new(16928, -8968, 56, 24),
        },
        Goals = GoalTimes.FromSeconds(
          bronze: 16,
          silver: 14,
          gold: 12,
          diamond: 11.5,
          grandmaster: 11
        ),
      },
      new Trial
      {
        Id = "cl1-5",
        Name = "No throwing allowed",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/7-Summit",
          Mode = AreaMode.Normal,
          StartRoom = "g-03",
          Spawn = new(26752f, -24688f),
          FinishRoom = "g-03",
          Finish = new(27728, -25648, 56, 80),
        },
        Goals = GoalTimes.FromSeconds(
          bronze: 12,
          silver: 10.5,
          gold: 9.1,
          diamond: 8.7,
          grandmaster: 8.5
        ),
      },
      new Trial
      {
        Id = "cl1-6",
        Name = "It's just 7B Flag 10",
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
