using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Runs;

public sealed class SplitDetector(SplitMap course)
{
  private readonly HashSet<SplitKey> taken = [];
  private readonly Dictionary<(string From, string To), int> crossings = [];
  private readonly List<SplitKey> reached = [];
  private LevelData room;
  private SplitPoint[] points = [];

  public List<SplitKey> FindReachedSplits(Vector2 position)
  {
    reached.Clear();
    UpdateRoom(position);
    if (room != null)
      AddTouchedPoints(position);
    return reached;
  }

  private void UpdateRoom(Vector2 position)
  {
    if (room != null && room.Check(position))
      return;
    LevelData next = course.Map.GetAt(position);
    if (next == null)
      return;
    LevelData previous = room;
    room = next;
    points = course.PointsIn(next.Name);
    if (
      previous == null
      || !course.SplitsOnRoomTransitions
      || next.Name == course.FinishOnEnterRoom
    )
      return;
    (string, string) pair = (previous.Name, next.Name);
    int count = crossings.GetValueOrDefault(pair) + 1;
    crossings[pair] = count;
    reached.Add(new SplitKey(SplitKind.RoomTransition, previous.Name, next.Name, count));
  }

  private void AddTouchedPoints(Vector2 position)
  {
    SplitBox body = new(position.X - 4f, position.Y - 11f, position.X + 4f, position.Y - 2f);
    foreach (SplitPoint point in points)
      if (point.Box.Overlaps(body) && taken.Add(point.Key))
        reached.Add(point.Key);
  }
}
