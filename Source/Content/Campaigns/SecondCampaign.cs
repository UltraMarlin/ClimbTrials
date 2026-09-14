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
        Id = "test-1",
        Name = "Test",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/8-Epilogue",
          Mode = AreaMode.Normal,
          StartRoom = "bridge",
          Spawn = new(200f, 40f),
          FinishRoom = "bridge",
          Finish = new(304, 24, 16, 16),
        },
        Goals = GoalTimes.FromSeconds(
          bronze: 25,
          silver: 20,
          gold: 15,
          diamond: 10,
          grandmaster: 8
        ),
      },
      new Trial
      {
        Id = "c2-1",
        Name = "Temple Start Hub Exploration (WIP)",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/5-MirrorTemple",
          Mode = AreaMode.Normal,
          StartRoom = "a-01",
          Spawn = new(1728f, 152f),
          FinishRoom = "a-01",
          Finish = new(1752, -328, 24, 16),
          Waypoints =
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
        Goals = GoalTimes.FromSeconds(
          bronze: 25,
          silver: 20,
          gold: 15,
          diamond: 10,
          grandmaster: 9
        ),
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
          Waypoints =
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
          CameraOffsets = [new("end_3b", new(-0.3f, 0f)), new("end_4", new(-0.3f, 0f))],
          SpawnFacing = Facings.Right,
        },
        Goals = GoalTimes.FromSeconds(
          bronze: 15,
          silver: 13,
          gold: 11,
          diamond: 10,
          grandmaster: 9.8
        ),
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
          RequiredStrawberries = 2,
          IgnoredStrawberries = [new("b-08", 129), new("b-09", 167)],
          Dashes = 1,
        },
        Goals = GoalTimes.FromSeconds(
          bronze: 45,
          silver: 40,
          gold: 36,
          diamond: 31,
          grandmaster: 28
        ),
      },
      new Trial
      {
        Id = "c2-4",
        Name = "I have never seen this room before",
        Difficulty = Difficulty.Hard,
        Run = new()
        {
          Sid = "Celeste/6-Reflection",
          Mode = AreaMode.Normal,
          StartRoom = "b-00",
          Spawn = new(9440f, -2784f),
          FinishRoom = "b-00c",
          Finish = new(9296, -3256, 24, 40),
        },
        Goals = GoalTimes.FromSeconds(
          bronze: 15,
          silver: 12,
          gold: 10,
          diamond: 8.5,
          grandmaster: 8
        ),
      },
      new Trial
      {
        Id = "c2-6",
        Name = "That's not how you play 1B",
        Difficulty = Difficulty.Expert,
        Run = new()
        {
          Sid = "Celeste/1-ForsakenCity",
          Mode = AreaMode.BSide,
          StartRoom = "00",
          Spawn = new(0f, 144f),
          FinishRoom = "02b",
          Finish = new(1080, -528, 32, 24),
          Waypoints =
          [
            new("00", new(80f, 24f), SetsSpawn: false),
            new("00", new(176f, 48f), SetsSpawn: false),
            new("01", new(296f, -144f), SetsSpawn: false),
            new("01", new(504f, -48f), SetsSpawn: false),
            new("02", new(576f, -280f), SetsSpawn: false),
            new("02", new(792f, -216f), SetsSpawn: false),
            new("02b", new(872f, -504f), SetsSpawn: false),
            new("02b", new(992f, -384f), SetsSpawn: false),
          ],
        },
        Goals = GoalTimes.FromSeconds(
          bronze: 42,
          silver: 30,
          gold: 26,
          diamond: 24,
          grandmaster: 22.5
        ),
      },
      new Trial
      {
        Id = "c2-8",
        Name = "Why do I hear 5B music?",
        Difficulty = Difficulty.Hard,
        Run = new()
        {
          Sid = "Celeste/5-MirrorTemple",
          Mode = AreaMode.BSide,
          StartRoom = "start",
          Spawn = new(-536f, 88f),
          FinishRoom = "start",
          Finish = new(-312, -56, 24, 48),
          Waypoints =
          [
            new("start", new(-512f, -16f), SetsSpawn: false),
            new("start", new(-584f, 8f), SetsSpawn: false),
            new("start", new(-632f, 80f), SetsSpawn: false),
            new("start", new(-440f, 8f), SetsSpawn: false),
            new("start", new(-392f, 80f), SetsSpawn: false),
          ],
          BloomBaseAdd = 0.25f,
          LightingAlphaAdd = -0.1f,
        },
        Goals = GoalTimes.FromSeconds(bronze: 10, silver: 8, gold: 6, diamond: 5, grandmaster: 4.5),
      },
      new Trial
      {
        Id = "c2-9",
        Name = "1 Dash, 2 Berries, 1 Hotel",
        Difficulty = Difficulty.Hard,
        Run = new()
        {
          Sid = "Celeste/7-Summit",
          Mode = AreaMode.Normal,
          StartRoom = "d-01b",
          Spawn = new(10552f, -7400f),
          FinishRoom = "d-03",
          Finish = new(12072, -7392, 16, 48),
          Dashes = 1,
          RequiredStrawberries = 2,
        },
        Goals = GoalTimes.FromSeconds(
          bronze: 40,
          silver: 30,
          gold: 26,
          diamond: 22,
          grandmaster: 21
        ),
      },
    ],
  };
}
