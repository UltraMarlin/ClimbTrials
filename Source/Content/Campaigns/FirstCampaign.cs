namespace Celeste.Mod.ClimbTrials.Content;

public static class FirstCampaign
{
  public static readonly Campaign Definition = new()
  {
    Id = "c1",
    Name = "First Climb",
    Trials =
    [
      new Trial
      {
        Id = "c1n-01",
        Name = "City Crossing",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/1-ForsakenCity",
          Mode = AreaMode.Normal,
          StartRoom = "6b",
          Spawn = new(2283f, -1232f),
          FinishRoom = "6a",
          Finish = new(1864, -1168, 16, 24),
        },
        Goals = GoalTimes.FromSeconds(bronze: 5, silver: 4, gold: 3.2, grandmaster: 3),
      },
      new Trial
      {
        Id = "c1n-01b",
        Name = "City Crossing Checkpoints",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/1-ForsakenCity",
          Mode = AreaMode.Normal,
          StartRoom = "6b",
          Spawn = new(2283f, -1232f),
          FinishRoom = "6a",
          Finish = new(1864, -1168, 16, 24),
          Checkpoints = [new("6a", new(1700f, -1052f))],
        },
        Goals = GoalTimes.FromSeconds(bronze: 10, silver: 8, gold: 7, grandmaster: 5),
      },
      new Trial
      {
        Id = "c1n-02",
        Name = "Resort Mess",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/3-CelestialResort",
          Mode = AreaMode.Normal,
          StartRoom = "09-b",
          Spawn = new(5070f, -248f),
          FinishRoom = "09-b",
          Finish = new(4838, -48, 16, 24),
        },
        Goals = GoalTimes.FromSeconds(bronze: 6, silver: 4, gold: 3, grandmaster: 2.8),
      },
      new Trial
      {
        Id = "c1n-03",
        Name = "Epilogue Bridge",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/8-Epilogue",
          Mode = AreaMode.Normal,
          StartRoom = "outside",
          Spawn = new(136f, 40f),
          FinishRoom = "secret",
          Finish = new(1320, 34, 16, 24),
        },
        Goals = GoalTimes.FromSeconds(bronze: 8, silver: 6.5, gold: 5.7, grandmaster: 5.4),
      },
      new Trial
      {
        Id = "c1n-04",
        Name = "Block Breaker",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/6-Reflection",
          Mode = AreaMode.Normal,
          StartRoom = "13",
          Spawn = new(4408f, -3008f),
          FinishRoom = "13",
          Finish = new(4464, -2848, 40, 16),
        },
        Goals = GoalTimes.FromSeconds(bronze: 8, silver: 6.5, gold: 5, grandmaster: 4.3),
      },
      new Trial
      {
        Id = "feather-test",
        Name = "Feather Test",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/6-Reflection",
          Mode = AreaMode.Normal,
          StartRoom = "04",
          Spawn = new(-833f, -2936f),
          FinishRoom = "04",
          Finish = new(-891, -2952, 16, 24),
        },
        Goals = GoalTimes.FromSeconds(bronze: 25, silver: 20, gold: 15, grandmaster: 10),
      },
    ],
  };
}
