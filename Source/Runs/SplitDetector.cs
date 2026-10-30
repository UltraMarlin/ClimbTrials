using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Runs;

public readonly record struct ReachedSplit(SplitKey Key, long Ticks);

public sealed class SplitDetector(SplitMap course)
{
  private readonly HashSet<SplitKey> taken = [];
  private readonly Dictionary<(string From, string To), int> crossings = [];
  private readonly List<ReachedSplit> reached = [];
  private LevelData room;
  private SplitPoint[] points = [];
  private int previousState = -1;
  private int bossHitsInRoom;

  public void CopyFrom(SplitDetector other)
  {
    taken.Clear();
    taken.UnionWith(other.taken);
    crossings.Clear();
    foreach (KeyValuePair<(string From, string To), int> crossing in other.crossings)
      crossings[crossing.Key] = crossing.Value;
    room = other.room;
    points = other.points;
    previousState = other.previousState;
    bossHitsInRoom = other.bossHitsInRoom;
  }

  public List<ReachedSplit> FindReachedSplits(
    Vector2 position,
    int state,
    long ticks,
    long? transitionStartTicks
  )
  {
    reached.Clear();
    UpdateRoom(position, transitionStartTicks ?? ticks);
    if (room != null)
    {
      AddTouchedPoints(position, ticks);
      AddBossHit(state, ticks);
    }
    previousState = state;
    return reached;
  }

  private void UpdateRoom(Vector2 position, long ticks)
  {
    if (room != null && room.Check(position))
      return;
    LevelData next = course.Map.GetAt(position);
    if (next == null)
      return;
    LevelData previous = room;
    room = next;
    points = course.PointsIn(next.Name);
    bossHitsInRoom = 0;
    if (
      previous == null
      || !course.SplitsOnRoomTransitions
      || next.Name == course.FinishOnEnterRoom
    )
      return;
    (string, string) pair = (previous.Name, next.Name);
    int count = crossings.GetValueOrDefault(pair) + 1;
    crossings[pair] = count;
    reached.Add(
      new ReachedSplit(
        new SplitKey(SplitKind.RoomTransition, previous.Name, next.Name, count),
        ticks
      )
    );
  }

  private void AddTouchedPoints(Vector2 position, long ticks)
  {
    SplitBox body = new(position.X - 4f, position.Y - 11f, position.X + 4f, position.Y - 2f);
    foreach (SplitPoint point in points)
      if (point.Box.Overlaps(body) && taken.Add(point.Key))
        reached.Add(new ReachedSplit(point.Key, ticks));
  }

  private void AddBossHit(int state, long ticks)
  {
    if (state == Player.StIntroRespawn)
      bossHitsInRoom = 0;
    if (!course.SplitsOnBossHits || state != Player.StAttract || previousState == Player.StAttract)
      return;
    bossHitsInRoom++;
    SplitKey key = new(SplitKind.Boss, room.Name, null, bossHitsInRoom);
    if (taken.Add(key))
      reached.Add(new ReachedSplit(key, ticks));
  }
}
