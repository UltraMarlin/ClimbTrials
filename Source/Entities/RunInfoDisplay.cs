using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class RunInfoDisplay : Entity
{
  private const float ModifierListX = 18f;
  private const float ModifierListGap = 10f;
  private const float ModifierListHeight = 54f;
  private const string WaypointIcon = "ClimbTrials/Waypoint_Icon";
  private const string StrawberryIcon = "collectables/strawberry";
  private static readonly string[] DashIcons =
  [
    "ClimbTrials/mod_dashless",
    "ClimbTrials/mod_dash1",
    "ClimbTrials/mod_dash2",
  ];
  private const string GrablessIcon = "ClimbTrials/mod_grabless";

  public RunInfoDisplay()
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate;
  }

  public override void Render()
  {
    Trial trial = RunManager.Trial;
    if (trial == null || RunManager.Run == null)
      return;
    RunDefinition run = trial.Run;
    RunProgress progress = RunManager.Progress;
    float y = RunHudLayout.ConditionsY;

    if (run.HasModifiers)
    {
      float x = ModifierListX;
      if (run.Grabless)
        x = DrawModifierIcon(GFX.Gui[GrablessIcon], x, y);
      if (run.Dashes is int dashes)
        DrawModifierIcon(GFX.Gui[DashIcons[dashes]], x, y);
      y += ModifierListHeight;
    }
    if (run.Waypoints.Count > 0)
    {
      DrawIcon(GFX.Gui[WaypointIcon], y);
      DrawText($"{progress.WaypointsTaken}/{run.Waypoints.Count}", y);
      y += RunHudLayout.RowHeight;
    }
    if (run.RequiredStrawberries > 0)
    {
      DrawIcon(GFX.Gui[StrawberryIcon], y);
      DrawText($"{progress.Strawberries}/{run.RequiredStrawberries}", y);
    }
  }

  private static float DrawModifierIcon(MTexture texture, float x, float y)
  {
    float scale = RunHudLayout.IconHeight / texture.Height;
    float width = texture.Width * scale;
    Vector2 center = new(x + width / 2f, y + RunHudLayout.RowHeight / 2f);
    texture.DrawCentered(center, Color.White, scale);
    return x + width + ModifierListGap;
  }

  private static void DrawIcon(MTexture texture, float y)
  {
    float scale = RunHudLayout.IconHeight / texture.Height;
    Vector2 center = new(RunHudLayout.IconX, y + RunHudLayout.RowHeight / 2f);
    texture.DrawCentered(center, Color.White, scale);
  }

  private static void DrawText(string text, float y)
  {
    ActiveFont.DrawOutline(
      text,
      new Vector2(RunHudLayout.TimeX, y + RunHudLayout.RowHeight / 2f),
      new Vector2(0f, 0.5f),
      Vector2.One * RunHudLayout.TimeScale,
      Color.White,
      2f,
      TextOutline.StrokeFor(Color.White)
    );
  }
}
