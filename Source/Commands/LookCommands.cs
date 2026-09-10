using System.Collections.Generic;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Commands;

public static class LookCommands
{
  private const string LogTag = "ClimbTrials/Look";

  private static readonly Vector2 Cell = new(
    Level.CameraOffsetXInterval,
    Level.CameraOffsetYInterval
  );

  [Command(
    "ct_look",
    "ClimbTrials: print the room's look and camera values, for a trial's BloomBaseAdd, LightingAlphaAdd and CameraOffsets"
  )]
  public static void Look()
  {
    if (!ConsoleOutput.TryGetLevel(out Level level))
      return;
    Session session = level.Session;
    Log(
      $"room '{session.Level}', map camera offset {Xy(session.LevelData.CameraOffset)}, "
        + $"level camera offset {Xy(level.CameraOffset / Cell)}"
    );
    Log(
      $"bloom base {F(level.Bloom.Base)}, strength {F(level.Bloom.Strength)}, "
        + $"session add {F(session.BloomBaseAdd)}"
    );
    Log(
      $"lighting base {F(level.BaseLightingAlpha)}, session add {F(session.LightingAlphaAdd)}, "
        + $"current {F(level.Lighting.Alpha)}, dark room {level.DarkRoom}"
    );
    Log($"color grade '{session.ColorGrade}'");
    Log("camera triggers: " + CameraTriggers(level));
  }

  private static string CameraTriggers(Level level)
  {
    Player player = level.Tracker.GetEntity<Player>();
    List<string> parts = [];
    foreach (Trigger trigger in level.Tracker.GetEntities<Trigger>())
    {
      string values = trigger switch
      {
        CameraOffsetTrigger offset => $"offset {Xy(offset.CameraOffset / Cell)}",
        CameraAdvanceTargetTrigger advance => $"camera target {Xy(advance.Target)}, "
          + $"strength {Xy(advance.LerpStrength)}{Axes(advance.XOnly, advance.YOnly)}",
        CameraTargetTrigger target => $"camera target {Xy(target.Target)}, "
          + $"strength {F(target.LerpStrength)}{Axes(target.XOnly, target.YOnly)}",
        SmoothCameraOffsetTrigger => "Everest smooth offset",
        _ => null,
      };
      if (values == null)
        continue;
      string inside = player != null && player.CollideCheck(trigger) ? ", inside" : "";
      parts.Add(
        $"{trigger.GetType().Name} at {Xy(trigger.Position)} "
          + $"size {Xy(new Vector2(trigger.Width, trigger.Height))}: {values}{inside}"
      );
    }
    return parts.Count == 0 ? "none" : string.Join("; ", parts);
  }

  private static string Axes(bool xOnly, bool yOnly) =>
    xOnly ? ", xOnly"
    : yOnly ? ", yOnly"
    : "";

  private static string F(float value) => value.ToString("0.###");

  private static string Xy(Vector2 v) => $"({F(v.X)}, {F(v.Y)})";

  private static void Log(string line) => ConsoleOutput.Log(LogTag, line);
}
