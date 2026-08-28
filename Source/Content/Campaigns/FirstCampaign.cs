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
          Mode = AreaMode.Normal,
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
          Mode = AreaMode.Normal,
          StartRoom = "09-b",
          Spawn = new(5070f, -248f),
          FinishRoom = "09-b",
          Finish = new(4838, -48, 16, 24),
        },
      },
      new Trial
      {
        Id = "c1-03",
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
      },
    ],
  };
}
