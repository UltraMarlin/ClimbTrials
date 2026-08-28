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
        Id = "c1-01",
        Name = "City Crossing",
        Difficulty = Difficulty.Normal,
        Run = new()
        {
          Sid = "Celeste/1-ForsakenCity",
          StartRoom = "6b",
          Spawn = new(2283f, -1232f),
          FinishRoom = "6a",
          Finish = new(1864, -1168, 16, 24),
        },
      },
      new Trial
      {
        Id = "c1-02",
        Name = "Resort Mess",
        Difficulty = Difficulty.Hard,
        Run = new()
        {
          Sid = "Celeste/3-CelestialResort",
          StartRoom = "09-b",
          Spawn = new(5070f, -248f),
          FinishRoom = "09-b",
          Finish = new(4838, -48, 16, 24),
        },
      },
    ],
  };
}
