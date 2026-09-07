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
    PlacingCheckpoint,
    DrawingFinish,
  }

  private const float Grid = 8f;
  private const float KeptAlpha = 0.5f;
  private static readonly Color FinishColor = Color.White;
  private static readonly Color SpawnColor = Color.Red;

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
  }

  public static void Unload()
  {
    Everest.Events.Level.OnLoadLevel -= OnLoadLevel;
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

  public void BeginCheckpoint() => Begin(Mode.PlacingCheckpoint);

  public void BeginFinish() => Begin(Mode.DrawingFinish);

  private void Begin(Mode next)
  {
    mode = next;
    dragStart = null;
  }

  private static Vector2 Snap(Vector2 world) =>
    new(System.MathF.Round(world.X / Grid) * Grid, System.MathF.Round(world.Y / Grid) * Grid);

  private Vector2 MouseWorld() => Snap(SceneAs<Level>().ScreenToWorld(MInput.Mouse.Position));

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
      case Mode.PlacingCheckpoint when MInput.Mouse.PressedLeftButton:
        bool shift =
          MInput.Keyboard.Check(Keys.LeftShift) || MInput.Keyboard.Check(Keys.RightShift);
        Player player = level.Tracker.GetEntity<Player>();
        Vector2? spawn = shift && player != null ? Snap(player.Position) : null;
        TrialDraft.PlacedCheckpoint checkpoint = new(room, at, spawn);
        TrialDraft.Checkpoints.Add(checkpoint);
        Begin(Mode.Idle);
        break;
      case Mode.DrawingFinish:
        if (MInput.Mouse.PressedLeftButton)
          dragStart = at;
        if (dragStart is Vector2 start && MInput.Mouse.ReleasedLeftButton)
        {
          Rectangle rect = RectBetween(start, at);
          TrialDraft.Finish = new TrialDraft.PlacedFinish(room, rect);
          Begin(Mode.Idle);
        }
        break;
    }
  }

  private static Rectangle RectBetween(Vector2 a, Vector2 b)
  {
    int x = (int)System.Math.Min(a.X, b.X);
    int y = (int)System.Math.Min(a.Y, b.Y);
    int width = (int)System.Math.Max(System.Math.Abs(a.X - b.X), Grid);
    int height = (int)System.Math.Max(System.Math.Abs(a.Y - b.Y), Grid);
    return new Rectangle(x, y, width, height);
  }

  private static Rectangle SpawnBox(Vector2 spawn) =>
    new((int)spawn.X - 8, (int)spawn.Y - 16, 16, 16);

  public override void Render()
  {
    base.Render();
    string room = SceneAs<Level>().Session.Level;
    if (TrialDraft.Finish is TrialDraft.PlacedFinish finish && finish.Room == room)
      Draw.HollowRect(finish.Rect, FinishColor * KeptAlpha);
    foreach (TrialDraft.PlacedCheckpoint checkpoint in TrialDraft.Checkpoints)
      if (checkpoint.Room == room)
        TrialCheckpoint.DrawPreview(checkpoint.Position, KeptAlpha);
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
      case Mode.PlacingCheckpoint:
        TrialCheckpoint.DrawPreview(at, 1f);
        break;
      case Mode.DrawingFinish when dragStart is Vector2 start:
        Draw.HollowRect(RectBetween(start, at), FinishColor);
        break;
      case Mode.DrawingFinish:
        Draw.HollowRect(new Rectangle((int)at.X, (int)at.Y, (int)Grid, (int)Grid), FinishColor);
        break;
    }
  }
}
