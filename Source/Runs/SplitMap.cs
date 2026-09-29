using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Runs;

public enum SplitKind
{
  RoomTransition,
  Waypoint,
  Collectible,
  Rect,
}

public readonly record struct SplitKey(SplitKind Kind, string Room, string ToRoom, int Index);

public readonly record struct SplitBox(float Left, float Top, float Right, float Bottom)
{
  public static SplitBox Centered(Vector2 center, float size) =>
    new(center.X - size / 2f, center.Y - size / 2f, center.X + size / 2f, center.Y + size / 2f);

  public static SplitBox Of(Rectangle rect) => new(rect.Left, rect.Top, rect.Right, rect.Bottom);

  public bool Overlaps(SplitBox other) =>
    Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;
}

public readonly record struct SplitPoint(SplitKey Key, SplitBox Box);

public sealed class SplitMap
{
  private static readonly SplitPoint[] NoPoints = [];

  private readonly Dictionary<string, SplitPoint[]> pointsByRoom;

  public RunDefinition Run { get; }
  public MapData Map { get; }
  public bool SplitsOnRoomTransitions { get; }
  public string FinishOnEnterRoom { get; }

  private SplitMap(RunDefinition run, Dictionary<string, SplitPoint[]> pointsByRoom)
  {
    Run = run;
    Map = run.Map;
    this.pointsByRoom = pointsByRoom;
    SplitsOnRoomTransitions = run.HasSplitCategory(SplitCategory.RoomTransition);
    FinishOnEnterRoom = run.FinishKind == FinishKind.RoomEnter ? run.FinishRoom : null;
  }

  public SplitPoint[] PointsIn(string room) =>
    pointsByRoom.TryGetValue(room, out SplitPoint[] points) ? points : NoPoints;

  public static bool IsInsideItsRoom(SplitRect rect, MapData map) =>
    map.Get(rect.Room) is LevelData room && room.Bounds.Contains(rect.Rect);

  public static SplitMap Build(RunDefinition run)
  {
    MapData map = run.Map;
    Dictionary<string, List<SplitPoint>> rooms = [];
    void AddPoint(string room, SplitKey key, SplitBox box)
    {
      if (room == null)
        return;
      if (!rooms.TryGetValue(room, out List<SplitPoint> points))
        rooms[room] = points = [];
      points.Add(new SplitPoint(key, box));
    }

    if (run.HasSplitCategory(SplitCategory.Waypoint))
      for (int i = 0; i < run.Waypoints.Count; i++)
      {
        Vector2 position = run.Waypoints[i].Position;
        string room = map.GetAt(position)?.Name;
        AddPoint(
          room,
          new SplitKey(SplitKind.Waypoint, room, null, i),
          SplitBox.Centered(position, 16f)
        );
      }

    bool berries = run.HasSplitCategory(SplitCategory.Strawberry);
    bool gems = run.HasSplitCategory(SplitCategory.Gem);
    if (berries || gems)
      foreach (Collectible item in run.FindCollectibles(map))
      {
        string room = item.Id.Level;
        SplitKey key = new(SplitKind.Collectible, room, null, item.Id.ID);
        switch (item.Kind)
        {
          case CollectibleKind.Strawberry
            when berries && RunDefinition.IsStationaryBerry(item.Entity):
            AddPoint(room, key, SplitBox.Centered(item.World, 14f));
            break;
          case CollectibleKind.SummitGem when gems:
            AddPoint(room, key, SplitBox.Centered(item.World, 12f));
            break;
          case CollectibleKind.Heart when gems && !run.IsFinishHeart(item):
            AddPoint(room, key, SplitBox.Centered(item.World, 16f));
            break;
        }
      }

    for (int i = 0; i < run.SplitRects.Count; i++)
    {
      SplitRect rect = run.SplitRects[i];
      if (IsInsideItsRoom(rect, map))
        AddPoint(
          rect.Room,
          new SplitKey(SplitKind.Rect, rect.Room, null, i),
          SplitBox.Of(rect.Rect)
        );
    }

    Dictionary<string, SplitPoint[]> pointsByRoom = [];
    foreach ((string room, List<SplitPoint> points) in rooms)
      pointsByRoom[room] = points.ToArray();
    return new SplitMap(run, pointsByRoom);
  }
}
