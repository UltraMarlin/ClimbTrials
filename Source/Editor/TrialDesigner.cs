using Celeste.Mod.ClimbTrials.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Editor;

[Tracked]
public class TrialDesigner : Entity
{
  private enum Mode
  {
    Idle,
    PlacingSpawn,
    PlacingWaypoint,
    DrawingFinish,
  }

  private const float Grid = 8f;
  private const float KeptAlpha = 0.5f;
  private static readonly Color FinishColor = Color.White;
  private static readonly Color SpawnColor = Color.Red;
  private static readonly Color IgnoreColor = Color.Orange;

  private Mode mode;
  private Vector2? dragStart;

  public TrialDesigner()
  {
    Tag = Tags.Global;
    Depth = -1000000;
  }

  public static void Load()
  {
    Everest.Events.Level.OnLoadLevel += OnLoadLevel;
    On.Celeste.OverworldLoader.ctor += OnOverworldLoad;
  }

  public static void Unload()
  {
    Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
    On.Celeste.OverworldLoader.ctor -= OnOverworldLoad;
  }

  private static void OnOverworldLoad(
    On.Celeste.OverworldLoader.orig_ctor orig,
    OverworldLoader self,
    Overworld.StartMode startMode,
    HiresSnow snow
  )
  {
    orig(self, startMode, snow);
    TrialDraft.Reset();
  }

  private static void OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
  {
    if (TrialDraft.Sid != null)
      Get(level);
  }

  public static TrialDesigner Get(Level level)
  {
    TrialDesigner designer = level.Tracker.GetEntity<TrialDesigner>();
    if (designer == null)
    {
      designer = [];
      level.Add(designer);
    }
    return designer;
  }

  public void BeginSpawn() => Begin(Mode.PlacingSpawn);

  public void BeginWaypoint() => Begin(Mode.PlacingWaypoint);

  public void BeginFinish() => Begin(Mode.DrawingFinish);

  private void Begin(Mode next)
  {
    mode = next;
    dragStart = null;
  }

  private static Vector2 Snap(Vector2 world) =>
    new(System.MathF.Round(world.X / Grid) * Grid, System.MathF.Round(world.Y / Grid) * Grid);

  private static float SnapDown(float value) => System.MathF.Floor(value / Grid) * Grid;

  private Vector2 RawMouseWorld() => SceneAs<Level>().ScreenToWorld(MInput.Mouse.Position);

  private Vector2 MouseWorld() => Snap(RawMouseWorld());

  public override void Update()
  {
    base.Update();
    if (mode == Mode.Idle)
      return;

    if (MInput.Mouse.PressedRightButton)
    {
      Begin(Mode.Idle);
      return;
    }

    Level level = SceneAs<Level>();
    string room = level.Session.Level;
    Vector2 at = MouseWorld();
    switch (mode)
    {
      case Mode.PlacingSpawn when MInput.Mouse.PressedLeftButton:
        TrialDraft.SetSpawn(level, at);
        Begin(Mode.Idle);
        break;
      case Mode.PlacingWaypoint when MInput.Mouse.PressedLeftButton:
        bool shift =
          MInput.Keyboard.Check(Keys.LeftShift) || MInput.Keyboard.Check(Keys.RightShift);
        bool control =
          MInput.Keyboard.Check(Keys.LeftControl) || MInput.Keyboard.Check(Keys.RightControl);
        Player player = level.Tracker.GetEntity<Player>();
        Vector2? spawn = shift && player != null ? Snap(player.Position) : null;
        bool setsSpawn = spawn != null || !control;
        TrialDraft.PlacedWaypoint waypoint = new(room, at, spawn, setsSpawn);
        TrialDraft.Waypoints.Add(waypoint);
        Begin(Mode.Idle);
        break;
      case Mode.DrawingFinish:
        Vector2 raw = RawMouseWorld();
        if (MInput.Mouse.PressedLeftButton)
          dragStart = raw;
        if (dragStart is Vector2 start && MInput.Mouse.ReleasedLeftButton)
        {
          Rectangle rect = SquaresBetween(start, raw);
          TrialDraft.Finish = new TrialDraft.PlacedFinish(room, rect);
          Begin(Mode.Idle);
        }
        break;
    }
  }

  private static Rectangle SquaresBetween(Vector2 a, Vector2 b)
  {
    float left = SnapDown(System.Math.Min(a.X, b.X));
    float top = SnapDown(System.Math.Min(a.Y, b.Y));
    float right = SnapDown(System.Math.Max(a.X, b.X)) + Grid;
    float bottom = SnapDown(System.Math.Max(a.Y, b.Y)) + Grid;
    return new Rectangle((int)left, (int)top, (int)(right - left), (int)(bottom - top));
  }

  private static Rectangle SpawnBox(Vector2 spawn) =>
    new((int)spawn.X - 8, (int)spawn.Y - 16, 16, 16);

  private void DrawIgnoredBerry(EntityID id)
  {
    LevelData data = SceneAs<Level>().Session.LevelData;
    EntityData entity = data.Entities.Find(e => e.ID == id.ID);
    if (entity == null)
      return;
    Vector2 world = entity.Position + data.Position;
    Color color = IgnoreColor * KeptAlpha;
    Draw.HollowRect(world.X - 6f, world.Y - 6f, 12f, 12f, color);
    Draw.Line(world + new Vector2(-6f, -6f), world + new Vector2(6f, 6f), color);
    Draw.Line(world + new Vector2(-6f, 6f), world + new Vector2(6f, -6f), color);
  }

  public override void Render()
  {
    base.Render();
    string room = SceneAs<Level>().Session.Level;
    if (TrialDraft.Finish is TrialDraft.PlacedFinish finish && finish.Room == room)
      Draw.HollowRect(finish.Rect, FinishColor * KeptAlpha);
    foreach (TrialDraft.PlacedWaypoint waypoint in TrialDraft.Waypoints)
      if (waypoint.Room == room)
        TrialWaypoint.DrawPreview(waypoint.Position, KeptAlpha);
    foreach (EntityID id in TrialDraft.IgnoredStrawberries)
      if (id.Level == room)
        DrawIgnoredBerry(id);
    if (TrialDraft.Spawn is Vector2 spawn && TrialDraft.StartRoom == room)
      Draw.HollowRect(SpawnBox(spawn), SpawnColor * KeptAlpha);
    if (mode == Mode.Idle)
      return;

    Vector2 at = MouseWorld();
    switch (mode)
    {
      case Mode.PlacingSpawn:
        Draw.HollowRect(SpawnBox(at), SpawnColor);
        break;
      case Mode.PlacingWaypoint:
        TrialWaypoint.DrawPreview(at, 1f);
        break;
      case Mode.DrawingFinish when dragStart is Vector2 start:
        Draw.HollowRect(SquaresBetween(start, RawMouseWorld()), FinishColor);
        break;
      case Mode.DrawingFinish:
        Vector2 raw = RawMouseWorld();
        Draw.HollowRect(SquaresBetween(raw, raw), FinishColor);
        break;
    }
  }
}
