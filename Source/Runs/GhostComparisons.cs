using System.Collections.Generic;
using System.Linq;
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
  private static Watched switchingTo;
  private static ComparedGhost playingAsGhost;
  private static ComparedGhost chosenWas;
  private static int rankOfPlaying;

  private sealed record Watched(string Key, Replay Replay, int Rank);

  public static IReadOnlyList<ComparedGhost> All => ghosts;

  public static IEnumerable<ComparedGhost> Shown =>
    ghosts.Where(g => g.Replay != RunManager.Replay);

  public static bool Showing
  {
    get
    {
      if (!RunManager.Replaying)
        return false;
      foreach (ComparedGhost ghost in ghosts)
        if (ghost.Replay != RunManager.Replay)
          return true;
      return false;
    }
  }

  public static Replay SwitchingTo => switchingTo?.Replay;

  public static bool Planning => !RunManager.Replaying && switchingTo != null;

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
    return key != null && WatchedKey() == key;
  }

  private static string WatchedKey() =>
    switchingTo?.Key ?? (RunManager.Replay is Replay watched ? KeyOf(watched) : null);

  public static void Plan(BoardRow row, Replay replay, Trial trial)
  {
    if (RunManager.Replaying)
      return;
    Forget();
    trialId = trial.Id;
    switchingTo = new Watched(KeyOf(row), replay, row.RankOrLast);
  }

  public static void Switch(BoardRow row, Replay replay, Trial trial, int watchedRank)
  {
    if (Planning)
    {
      SwitchPlan(row, replay, trial);
      return;
    }
    if (!RunManager.Replaying || RunManager.Replay is not Replay playing || IsWatched(row))
      return;
    if (trial.Id != trialId)
      Forget();
    trialId = trial.Id;
    int playingRank = switchingTo == null ? watchedRank : rankOfPlaying;
    rankOfPlaying = playingRank;
    UndoSwitch();
    if (KeyOf(row) == KeyOf(playing))
    {
      RunManager.ShowGhosts();
      return;
    }

    ComparedGhost chosen = Find(row);
    if (chosen != null)
    {
      ghosts.Remove(chosen);
      GhostSplits splits = GhostSplits.Compute(playing, CourseOf(trial));
      playingAsGhost = new(KeyOf(playing), playing, chosen.Color, splits, playingRank);
      ghosts.Add(playingAsGhost);
    }
    chosenWas = chosen;
    switchingTo = new Watched(KeyOf(row), replay, row.RankOrLast);
    RunManager.ShowGhosts();
  }

  private static void SwitchPlan(BoardRow row, Replay replay, Trial trial)
  {
    if (IsWatched(row) || trial.Id != trialId)
      return;
    Watched planned = switchingTo;
    ComparedGhost chosen = Find(row);
    if (chosen != null)
    {
      ghosts.Remove(chosen);
      GhostSplits splits = GhostSplits.Compute(planned.Replay, CourseOf(trial));
      ghosts.Add(new(planned.Key, planned.Replay, chosen.Color, splits, planned.Rank));
    }
    switchingTo = new Watched(KeyOf(row), replay, row.RankOrLast);
  }

  public static void CancelSwitch()
  {
    UndoSwitch();
    RunManager.ShowGhosts();
  }

  private static void UndoSwitch()
  {
    if (switchingTo == null)
      return;
    ghosts.Remove(playingAsGhost);
    if (chosenWas != null && !ghosts.Exists(g => g.Key == chosenWas.Key))
      ghosts.Add(chosenWas);
    ForgetSwitch();
  }

  private static void ForgetSwitch()
  {
    switchingTo = null;
    playingAsGhost = null;
    chosenWas = null;
  }

  public static void Add(BoardRow row, Replay replay, Trial trial)
  {
    if ((!RunManager.Replaying && !Planning) || Find(row) != null || IsWatched(row))
      return;
    if (trial.Id != trialId)
      Forget();
    trialId = trial.Id;
    GhostSplits splits = GhostSplits.Compute(replay, CourseOf(trial));
    ComparedGhost added = new(KeyOf(row), replay, NextColor(), splits, row.RankOrLast);
    ghosts.Add(added);
    if (switchingTo != null && RunManager.Replay is Replay playing && added.Key == KeyOf(playing))
      playingAsGhost = added;
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
    chosenWas = null;
    RunManager.ShowGhosts();
  }

  public static void Forget()
  {
    ghosts.Clear();
    trialId = null;
    course = null;
    watchedSplits = null;
    ForgetSwitch();
  }

  public static void KeepFor(Trial trial, Replay watched)
  {
    watchedSplits = null;
    ForgetSwitch();
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
