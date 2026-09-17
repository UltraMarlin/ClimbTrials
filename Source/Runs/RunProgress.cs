using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Runs;

public sealed class RunProgress
{
  private readonly HashSet<int> touched = [];
  private readonly HashSet<int> kept = [];
  private Vector2? respawnSeen;

  public int WaypointsTaken => touched.Count;
  public int Strawberries { get; private set; }
  public int SummitGems { get; private set; }
  public bool RespawnMoved { get; private set; }

  public bool IsTaken(int index) => touched.Contains(index);

  public bool TakeWaypoint(int index) => touched.Add(index);

  public void AddStrawberry() => Strawberries++;

  public void AddSummitGem() => SummitGems++;

  public bool Met(RunDefinition run) =>
    WaypointsTaken >= run.Waypoints.Count
    && Strawberries >= run.RequiredStrawberries
    && SummitGems >= run.RequiredSummitGems;

  public void ObserveRespawn(Vector2? respawnPoint)
  {
    if (respawnSeen.HasValue && respawnPoint != respawnSeen)
    {
      kept.UnionWith(touched);
      RespawnMoved = true;
    }
    respawnSeen = respawnPoint;
  }

  public void DropUnkept() => touched.IntersectWith(kept);

  public void Reset()
  {
    touched.Clear();
    kept.Clear();
    respawnSeen = null;
    RespawnMoved = false;
    Strawberries = 0;
    SummitGems = 0;
  }
}
