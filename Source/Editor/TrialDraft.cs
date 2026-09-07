using System;
using System.Collections.Generic;
using System.Globalization;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Editor;

public static class TrialDraft
{
  public readonly record struct PlacedCheckpoint(string Room, Vector2 Position, Vector2? Spawn);

  public readonly record struct PlacedFinish(string Room, Rectangle Rect);

  public static string Id = "id";
  public static string Name = "Untitled";
  public static Difficulty Difficulty = Difficulty.Normal;
  public static double Bronze = 25,
    Silver = 20,
    Gold = 15,
    Grandmaster = 10;
  public static int ReplayLimitSeconds = Trial.DefaultReplayLimitSeconds;
  public static int? Dashes;
  public static bool Grabless;
  public static int RequiredStrawberries;

  public static string Sid;
  public static AreaMode Mode;
  public static string StartRoom;
  public static Vector2? Spawn;
  public static readonly List<PlacedCheckpoint> Checkpoints = [];
  public static PlacedFinish? Finish;

  public static void Reset()
  {
    Id = "id";
    Name = "Untitled";
    Difficulty = Difficulty.Normal;
    (Bronze, Silver, Gold, Grandmaster) = (25, 20, 15, 10);
    ReplayLimitSeconds = Trial.DefaultReplayLimitSeconds;
    Dashes = null;
    Grabless = false;
    RequiredStrawberries = 0;
    Sid = null;
    Mode = AreaMode.Normal;
    StartRoom = null;
    Spawn = null;
    Checkpoints.Clear();
    Finish = null;
  }

  public static void Load(Trial trial)
  {
    Reset();
    Id = trial.Id;
    Name = trial.Name;
    Difficulty = trial.Difficulty;
    if (trial.Goals is GoalTimes goals)
    {
      Bronze = ToSeconds(goals.BronzeTicks);
      Silver = ToSeconds(goals.SilverTicks);
      Gold = ToSeconds(goals.GoldTicks);
      Grandmaster = ToSeconds(goals.GrandmasterTicks);
    }
    ReplayLimitSeconds = trial.ReplayLimitSeconds;
    RunDefinition run = trial.Run;
    Dashes = run.Dashes;
    Grabless = run.Grabless;
    RequiredStrawberries = run.RequiredStrawberries;
    Sid = run.Sid;
    Mode = run.Mode;
    StartRoom = run.StartRoom;
    Spawn = run.Spawn;
    foreach (CheckpointDef checkpoint in run.Checkpoints)
      Checkpoints.Add(new PlacedCheckpoint(checkpoint.Room, checkpoint.Position, checkpoint.Spawn));
    Finish = new PlacedFinish(run.FinishRoom, run.Finish);
  }

  public static void SetSpawn(Level level, Vector2 position)
  {
    Sid = level.Session.Area.SID;
    Mode = level.Session.Area.Mode;
    StartRoom = level.Session.Level;
    Spawn = position;
  }

  public static string CheckpointCode(PlacedCheckpoint checkpoint)
  {
    string spawn = checkpoint.Spawn is Vector2 s ? $", {SpawnCode(s)}" : "";
    return $"new(\"{checkpoint.Room}\", {SpawnCode(checkpoint.Position)}{spawn})";
  }

  public static string FinishCode(Rectangle rect) =>
    $"new({rect.X}, {rect.Y}, {rect.Width}, {rect.Height})";

  public static string SpawnCode(Vector2 spawn) =>
    $"new({spawn.X.ToString(CultureInfo.InvariantCulture)}f, {spawn.Y.ToString(CultureInfo.InvariantCulture)}f)";

  public static string TrialCode()
  {
    PlacedFinish? finish = Finish;
    List<string> run =
    [
      $"Sid = \"{Sid}\"",
      $"Mode = AreaMode.{Mode}",
      $"StartRoom = \"{StartRoom}\"",
      $"Spawn = {SpawnCode(Spawn ?? Vector2.Zero)}",
      $"FinishRoom = \"{finish?.Room}\"",
      $"Finish = {FinishCode(finish?.Rect ?? new Rectangle(0, 0, 16, 24))}",
    ];
    if (Checkpoints.Count > 0)
      run.Add($"Checkpoints = [ {string.Join(", ", Checkpoints.ConvertAll(CheckpointCode))} ]");
    if (RequiredStrawberries > 0)
      run.Add($"RequiredStrawberries = {RequiredStrawberries}");
    if (Dashes is int dashes)
      run.Add($"Dashes = {dashes}");
    if (Grabless)
      run.Add("Grabless = true");

    List<string> trial =
    [
      $"Id = \"{Id}\"",
      $"Name = \"{Name}\"",
      $"Difficulty = Difficulty.{Difficulty}",
      $"Run = new() {{ {string.Join(", ", run)} }}",
      "Goals = GoalTimes.FromSeconds("
        + $"bronze: {SecondsToString(Bronze)}, "
        + $"silver: {SecondsToString(Silver)}, "
        + $"gold: {SecondsToString(Gold)}, "
        + $"grandmaster: {SecondsToString(Grandmaster)})",
    ];
    if (ReplayLimitSeconds != Trial.DefaultReplayLimitSeconds)
      trial.Add($"ReplayLimitSeconds = {ReplayLimitSeconds}");
    return $"new Trial {{ {string.Join(", ", trial)} }},";
  }

  private static double ToSeconds(long ticks) => (double)ticks / TimeSpan.TicksPerSecond;

  private static string SecondsToString(double seconds) =>
    seconds.ToString(CultureInfo.InvariantCulture);
}
