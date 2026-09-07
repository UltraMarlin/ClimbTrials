namespace Celeste.Mod.ClimbTrials.Content;

public static class SecondCampaign
{
  public static readonly Campaign Definition = new()
  {
    Id = "c2",
    Name = "Experimental Climb",
    Description = "More advanced Trials to test the Trial Editor.",
    Trials =
    [
      new Trial
      {
        Id = "c2-1",
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
        Id = "c2-2",
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
        Id = "c2-3",
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
        Goals = GoalTimes.FromSeconds(bronze: 45, silver: 40, gold: 35, grandmaster: 30),
      },
    ],
  };
}
