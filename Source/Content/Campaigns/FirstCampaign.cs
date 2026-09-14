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
        Goals = GoalTimes.FromSeconds(
          bronze: 54,
          silver: 48,
          gold: 44,
          diamond: 40,
          grandmaster: 34
        ),
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
          Waypoints =
          [
            new("6b", new(2168f, -1200f), SetsSpawn: false),
            new("6a", new(1700f, -1052f)),
          ],
        },
        Goals = GoalTimes.FromSeconds(
          bronze: 25,
          silver: 20,
          gold: 15,
          diamond: 12,
          grandmaster: 8
        ),
      },
      new Trial
      {
        Id = "c1-3",
        Name = "Shrine Waypoint Frenzy",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/4-GoldenRidge",
          Mode = AreaMode.Normal,
          StartRoom = "b-00",
          Spawn = new(4416f, -1016f),
          FinishRoom = "b-00",
          Finish = new(4384, -1040, 64, 24),
          Waypoints =
          [
            new("b-00", new(4312f, -1068f)),
            new("b-00", new(4428f, -1370f)),
            new("b-00", new(4550f, -1298f)),
            new("b-00", new(4512f, -1216f)),
            new("b-00", new(4314f, -1246f)),
            new("b-00", new(4374f, -1288f)),
          ],
          CameraOffsets = [new("b-00", new(0f, 0f))],
          Dashes = 2,
        },
        Goals = GoalTimes.FromSeconds(
          bronze: 26,
          silver: 22,
          gold: 20,
          diamond: 18,
          grandmaster: 12
        ),
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
        Goals = GoalTimes.FromSeconds(
          bronze: 18,
          silver: 16,
          gold: 14,
          diamond: 12,
          grandmaster: 10
        ),
      },
    ],
  };
}
