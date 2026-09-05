using System.Collections.Generic;

namespace Celeste.Mod.ClimbTrials.Runs;

public sealed class RunProgress
{
  private readonly HashSet<int> taken = [];

  public int CheckpointsTaken => taken.Count;
  public int Strawberries { get; private set; }

  public bool IsTaken(int index) => taken.Contains(index);

  public bool TakeCheckpoint(int index) => taken.Add(index);

  public void AddStrawberry() => Strawberries++;

  public bool Met(RunDefinition run) =>
    CheckpointsTaken >= run.Checkpoints.Count && Strawberries >= run.RequiredStrawberries;

  public void Reset()
  {
    taken.Clear();
    Strawberries = 0;
  }
}
