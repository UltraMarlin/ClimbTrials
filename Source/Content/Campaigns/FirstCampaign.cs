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
        Id = "c1-1",
        Name = "Berry City",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/1-ForsakenCity",
          Mode = AreaMode.Normal,
          StartRoom = "1",
          Spawn = new(19f, 144f),
          FinishRoom = "5",
          Finish = new(1220, -896, 16, 24),
          RequiredStrawberries = 2,
        },
        Goals = GoalTimes.FromSeconds(bronze: 54, silver: 48, gold: 44, grandmaster: 40),
      },
      new Trial
      {
        Id = "c1-2",
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
          Checkpoints =
          [
            new("6b", new(2168f, -1200f), new(2238f, -1264f)),
            new("6a", new(1700f, -1052f)),
          ],
        },
        Goals = GoalTimes.FromSeconds(bronze: 25, silver: 20, gold: 15, grandmaster: 12),
      },
      new Trial
      {
        Id = "c1-3",
        Name = "Shrine Checkpoint Frenzy",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/4-GoldenRidge",
          Mode = AreaMode.Normal,
          StartRoom = "b-00",
          Spawn = new(4416f, -1016f),
          FinishRoom = "b-00",
          Finish = new(4408, -1040, 16, 24),
          Checkpoints =
          [
            new("b-00", new(4312f, -1068f)),
            new("b-00", new(4428f, -1370f)),
            new("b-00", new(4550f, -1298f)),
            new("b-00", new(4512f, -1216f)),
            new("b-00", new(4314f, -1246f)),
            new("b-00", new(4374f, -1288f)),
          ],
          Dashes = 2,
        },
        Goals = GoalTimes.FromSeconds(bronze: 26, silver: 22, gold: 20, grandmaster: 18),
      },
      new Trial
      {
        Id = "c1-4",
        Name = "Wake Up",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/2-OldSite",
          Mode = AreaMode.Normal,
          StartRoom = "end_3",
          Spawn = new(1015f, 1888f),
          FinishRoom = "end_1",
          Finish = new(414, 1656, 16, 24),
          Dashes = 0,
        },
        Goals = GoalTimes.FromSeconds(bronze: 18, silver: 16, gold: 14, grandmaster: 12),
      },
      new Trial
      {
        Id = "c1-5",
        Name = "Temple Start Hub Exploration",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/5-MirrorTemple",
          Mode = AreaMode.Normal,
          StartRoom = "a-01",
          Spawn = new(1728f, 152f),
          FinishRoom = "a-01",
          Finish = new(1752, -328, 24, 16),
          Checkpoints =
          [
            new("a-01", new(1584f, -24f)),
            new("a-01", new(1664f, -160f)),
            new("a-01", new(1824f, -152f)),
            new("a-01", new(1704f, -24f)),
            new("a-01", new(1688f, 184f)),
            new("a-01", new(1824f, 104f)),
            new("a-01", new(1520f, -160f)),
            new("a-01", new(1616f, 24f)),
          ],
        },
        Goals = GoalTimes.FromSeconds(bronze: 25, silver: 20, gold: 15, grandmaster: 10),
      },
      new Trial
      {
        Id = "c1-6",
        Name = "Temple Start Hub Exploration (2)",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/5-MirrorTemple",
          Mode = AreaMode.Normal,
          StartRoom = "a-01",
          Spawn = new(1648f, 152f),
          FinishRoom = "a-01",
          Finish = new(1744, -312, 40, 32),
          Checkpoints =
          [
            new("a-01", new(1584f, -24f)),
            new("a-01", new(1664f, -160f)),
            new("a-01", new(1824f, -152f)),
            new("a-01", new(1704f, -24f)),
            new("a-01", new(1688f, 184f)),
            new("a-01", new(1824f, 104f)),
            new("a-01", new(1520f, -160f)),
            new("a-01", new(1552f, 104f), new(1584f, 136f)),
          ],
        },
        Goals = GoalTimes.FromSeconds(bronze: 25, silver: 20, gold: 15, grandmaster: 10),
      },
      new Trial
      {
        Id = "c1-7",
        Name = "Checking out Awake",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/2-OldSite",
          Mode = AreaMode.Normal,
          StartRoom = "end_3b",
          Spawn = new(2144f, 1888f),
          FinishRoom = "end_3",
          Finish = new(1136, 1808, 32, 56),
          Checkpoints =
          [
            new("end_4", new(1552f, 1752f), new(1552f, 1776f)),
            new("end_4", new(1584f, 1808f), new(1584f, 1832f)),
            new("end_4", new(1464f, 1864f), new(1464f, 1888f)),
            new("end_4", new(1680f, 1808f), new(1680f, 1880f)),
            new("end_3b", new(1968f, 1776f), new(1968f, 1800f)),
            new("end_3b", new(2160f, 1768f), new(2160f, 1792f)),
            new("end_3b", new(2264f, 1784f), new(2264f, 1808f)),
            new("end_3b", new(1984f, 1864f), new(1984f, 1888f)),
          ],
        },
        Goals = GoalTimes.FromSeconds(bronze: 16, silver: 13, gold: 11, grandmaster: 10),
      },
      new Trial
      {
        Id = "c1-8",
        Name = "1 Dash, 2 Berries, 1 Dream",
        Difficulty = Difficulty.Hard,
        Run = new()
        {
          Sid = "Celeste/7-Summit",
          Mode = AreaMode.Normal,
          StartRoom = "c-03",
          Spawn = new(6800f, -5152f),
          FinishRoom = "c-07",
          Finish = new(8168, -6104, 24, 64),
          Checkpoints = [new("c-06b", new(8336f, -5656f))],
          RequiredStrawberries = 2,
          Dashes = 1,
        },
        Goals = GoalTimes.FromSeconds(bronze: 35, silver: 30, gold: 25, grandmaster: 20),
      },
    ],
  };
}
