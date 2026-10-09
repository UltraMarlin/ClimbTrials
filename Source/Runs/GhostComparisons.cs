using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Runs;

public sealed record ComparedGhost(
  string Key,
  Replay Replay,
  Color Color,
  GhostSplits Splits,
  int Rank
);

public static class GhostComparisons
{
  private const string OwnKey = "own";
  private const float NameLighten = 0.5f;

  private static readonly Color[] Palette =
  [
    Calc.HexToColor("43bccd"),
    Calc.HexToColor("662e9b"),
    Calc.HexToColor("ea3546"),
    Calc.HexToColor("227702"),
    Calc.HexToColor("f9c80e"),
    Calc.HexToColor("f791fd"),
    Calc.HexToColor("2e79f5"),
    Calc.HexToColor("62b811"),
    Calc.HexToColor("be47b8"),
    Calc.HexToColor("f86624"),
  ];

  private static readonly List<ComparedGhost> ghosts = [];
  private static string trialId;
  private static SplitMap course;
  private static GhostSplits watchedSplits;

  public static IReadOnlyList<ComparedGhost> All => ghosts;

  public static bool Showing => RunManager.Replaying && ghosts.Count > 0;

  public static Color NameColor(Color color) => Color.Lerp(color, Color.White, NameLighten);

  public static string KeyOf(BoardRow row) => row.Me ? OwnKey : row.ReplayId;

  private static string KeyOf(Replay replay) => replay.IsOwn ? OwnKey : replay.ServerReplayId;

  public static ComparedGhost Find(BoardRow row)
  {
    string key = KeyOf(row);
    return key == null ? null : ghosts.Find(g => g.Key == key);
  }

  public static bool IsWatched(BoardRow row)
  {
    string key = KeyOf(row);
    return key != null && RunManager.Replay is Replay watched && KeyOf(watched) == key;
  }

  public static void Add(BoardRow row, Replay replay, Trial trial)
  {
    if (!RunManager.Replaying || Find(row) != null || IsWatched(row))
      return;
    if (trial.Id != trialId)
      Forget();
    trialId = trial.Id;
    GhostSplits splits = GhostSplits.Compute(replay, CourseOf(trial));
    ghosts.Add(new ComparedGhost(KeyOf(row), replay, NextColor(), splits, row.Rank));
    RunManager.ShowGhosts();
  }

  public static void Remove(BoardRow row)
  {
    if (Find(row) is not ComparedGhost ghost)
      return;
    ghosts.Remove(ghost);
    RunManager.ShowGhosts();
  }

  public static void Clear()
  {
    ghosts.Clear();
    RunManager.ShowGhosts();
  }

  public static void Forget()
  {
    ghosts.Clear();
    trialId = null;
    course = null;
    watchedSplits = null;
  }

  public static void KeepFor(Trial trial, Replay watched)
  {
    watchedSplits = null;
    if (trial.Id != trialId)
    {
      Forget();
      return;
    }
    string key = KeyOf(watched);
    ghosts.RemoveAll(g => g.Key == key);
  }

  public static GhostSplits WatchedSplits()
  {
    if (RunManager.Replay is not Replay watched || RunManager.Trial is not Trial trial)
      return null;
    if (watchedSplits?.Replay != watched)
      watchedSplits = GhostSplits.Compute(watched, CourseOf(trial));
    return watchedSplits;
  }

  private static SplitMap CourseOf(Trial trial)
  {
    if (course?.Run != trial.Run)
      course = SplitMap.Build(trial.Run);
    return course;
  }

  private static Color NextColor()
  {
    foreach (Color color in Palette)
      if (!ghosts.Exists(g => g.Color == color))
        return color;
    return Palette[ghosts.Count % Palette.Length];
  }
}
