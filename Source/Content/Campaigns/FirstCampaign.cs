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
        Id = "c1n-01c",
        Name = "City Crossing Checkpoints + Berries",
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
          RequiredStrawberries = 2,
        },
        Goals = GoalTimes.FromSeconds(bronze: 10, silver: 8, gold: 7, grandmaster: 5),
      },
      new Trial
      {
        Id = "c1n-01d",
        Name = "City Crossing Dashless",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/1-ForsakenCity",
          Mode = AreaMode.Normal,
          StartRoom = "6b",
          Spawn = new(2283f, -1232f),
          FinishRoom = "6a",
          Finish = new(1864, -1168, 16, 24),
          Dashes = 0,
        },
        Goals = GoalTimes.FromSeconds(bronze: 10, silver: 8, gold: 7, grandmaster: 5),
      },
      new Trial
      {
        Id = "c1n-01e",
        Name = "City Crossing Grabless + Dashless",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/1-ForsakenCity",
          Mode = AreaMode.Normal,
          StartRoom = "6b",
          Spawn = new(2283f, -1232f),
          FinishRoom = "6a",
          Finish = new(1864, -1168, 16, 24),
          Dashes = 0,
          Grabless = true,
        },
        Goals = GoalTimes.FromSeconds(bronze: 10, silver: 8, gold: 7, grandmaster: 5),
      },
      new Trial
      {
        Id = "c1n-01f",
        Name = "City Crossing 2 Dashes",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/1-ForsakenCity",
          Mode = AreaMode.Normal,
          StartRoom = "6b",
          Spawn = new(2283f, -1232f),
          FinishRoom = "6a",
          Finish = new(1864, -1168, 16, 24),
          Dashes = 2,
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
        Id = "mirror-test",
        Name = "Mirror",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/2-OldSite",
          Mode = AreaMode.Normal,
          StartRoom = "d3",
          Spawn = new(1624f, 600f),
          FinishRoom = "d3",
          Finish = new(1666, 464, 16, 24),
        },
        Goals = GoalTimes.FromSeconds(bronze: 25, silver: 20, gold: 15, grandmaster: 10),
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
        Id = "prologue-bridge",
        Name = "Prologue Bridge",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/0-Intro",
          Mode = AreaMode.Normal,
          StartRoom = "2",
          Spawn = new(1167f, 40f),
          FinishRoom = "3",
          Finish = new(1967, 16, 16, 24),
        },
        Goals = GoalTimes.FromSeconds(bronze: 25, silver: 20, gold: 15, grandmaster: 10),
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
        Id = "c1n-test",
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
      new Trial
      {
        Id = "c1n-watchtowertest",
        Name = "Watch Tower Test",
        Difficulty = Difficulty.Hard,
        Run = new()
        {
          Sid = "Celeste/3-CelestialResort",
          Mode = AreaMode.BSide,
          StartRoom = "back",
          Spawn = new(-392f, 80f),
          FinishRoom = "02",
          Finish = new(1555, 72, 16, 24),
        },
        Goals = GoalTimes.FromSeconds(bronze: 25, silver: 20, gold: 15, grandmaster: 10),
      },
      new Trial
      {
        Id = "c1e-01",
        Name = "Backwards Bubbles",
        Difficulty = Difficulty.Expert,
        Run = new()
        {
          Sid = "Celeste/7-Summit",
          Mode = AreaMode.BSide,
          StartRoom = "f-01",
          Spawn = new(12046f, -9456f),
          FinishRoom = "f-01",
          Finish = new(11448, -9550, 16, 24),
        },
        Goals = GoalTimes.FromSeconds(bronze: 20, silver: 10, gold: 8, grandmaster: 7),
      },
    ],
  };
}
