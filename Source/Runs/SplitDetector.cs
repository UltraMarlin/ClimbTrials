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
  private int previousState = -1;
  private int bossHitsInRoom;

  public List<SplitKey> FindReachedSplits(Vector2 position, int state)
  {
    reached.Clear();
    UpdateRoom(position);
    if (room != null)
    {
      AddTouchedPoints(position);
      AddBossHit(state);
    }
    previousState = state;
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
    reached.Add(new SplitKey(SplitKind.RoomTransition, previous.Name, next.Name, count));
  }

  private void AddTouchedPoints(Vector2 position)
  {
    SplitBox body = new(position.X - 4f, position.Y - 11f, position.X + 4f, position.Y - 2f);
    foreach (SplitPoint point in points)
      if (point.Box.Overlaps(body) && taken.Add(point.Key))
        reached.Add(point.Key);
  }

  private void AddBossHit(int state)
  {
    if (state == Player.StIntroRespawn)
      bossHitsInRoom = 0;
    if (!course.SplitsOnBossHits || state != Player.StAttract || previousState == Player.StAttract)
      return;
    bossHitsInRoom++;
    SplitKey key = new(SplitKind.Boss, room.Name, null, bossHitsInRoom);
    if (taken.Add(key))
      reached.Add(key);
  }
}
