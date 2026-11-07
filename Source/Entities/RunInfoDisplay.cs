using System.Collections.Generic;
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
  private const string NoRefillsIcon = "ClimbTrials/mod_norefills";

  // TODO: Replace with custom icon?
  private const string SummitGemSprite = "collectables/summitgems/0/gem";

  public RunInfoDisplay()
  {
    Tag = Tags.HUD | Tags.Persistent;
  }

  public override void Render()
  {
    Trial trial = RunManager.Trial;
    if (trial == null || RunManager.Run == null || GhostComparisons.Showing)
      return;
    RunDefinition run = trial.Run;
    RunProgress progress = RunManager.Progress;
    float top = RunHudLayout.ConditionsY;

    if (run.HasModifiers)
    {
      float left = ModifierListX;
      if (run.Grabless)
        left = DrawModifierIcon(GFX.Gui[GrablessIcon], left, top);
      if (run.NoRefills)
        left = DrawModifierIcon(GFX.Gui[NoRefillsIcon], left, top);
      if (run.Dashes is int dashes)
        DrawModifierIcon(GFX.Gui[DashIcons[dashes]], left, top);
      top += ModifierListHeight;
    }
    if (run.Waypoints.Count > 0)
    {
      DrawIcon(GFX.Gui[WaypointIcon], top);
      DrawText($"{progress.WaypointsTaken}/{run.Waypoints.Count}", top);
      top += RunHudLayout.RowHeight;
    }
    if (run.RequiredStrawberries > 0)
    {
      DrawIcon(GFX.Gui[StrawberryIcon], top);
      DrawText($"{progress.Strawberries}/{run.RequiredStrawberries}", top);
      top += RunHudLayout.RowHeight;
    }
    if (run.RequiredSummitGems > 0)
    {
      List<MTexture> frames = GFX.Game.GetAtlasSubtextures(SummitGemSprite);
      if (frames.Count > 0)
        DrawIcon(frames[0], top);
      DrawText($"{progress.SummitGems}/{run.RequiredSummitGems}", top);
    }
  }

  private static float DrawModifierIcon(MTexture texture, float left, float top)
  {
    float scale = RunHudLayout.IconHeight / texture.Height;
    float width = texture.Width * scale;
    Vector2 center = new(left + width / 2f, top + RunHudLayout.RowHeight / 2f);
    texture.DrawCentered(center, Color.White, scale);
    return left + width + ModifierListGap;
  }

  private static void DrawIcon(MTexture texture, float top)
  {
    float scale = RunHudLayout.IconHeight / texture.Height;
    Vector2 center = new(RunHudLayout.IconX, top + RunHudLayout.RowHeight / 2f);
    texture.DrawCentered(center, Color.White, scale);
  }

  private static void DrawText(string text, float top)
  {
    TextOutline.Draw(
      text,
      new Vector2(RunHudLayout.TimeX, top + RunHudLayout.RowHeight / 2f),
      new Vector2(0f, 0.5f),
      RunHudLayout.TimeScale,
      Color.White
    );
  }
}
