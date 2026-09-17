using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class LeaderboardDisplay : Entity
{
  private const float NormalTextScale = 0.46f;
  private const float SubtleTextScale = 0.38f;
  private const float NormalTextAlpha = 0.9f;
  private const float SubtleTextAlpha = 0.5f;
  private const float RowHeightPerScale = 64f;
  private const float GapBelowGoals = 12f;
  private const float RankX = 16f;
  private const float ColumnGapPerScale = 36f;
  private const int NameFullChars = 10;
  private const float NameMinFit = 0.6f;
  private const string Ellipsis = "...";

  private readonly Color boxColor = Colors.ForChapter(RunManager.Run.Area).Dark;
  private static readonly Color myTimeColor = Calc.HexToColor("fad768");
  private readonly List<FadeBuffer> layers = [];
  private List<Row> rows = [];
  private TrialStanding cachedStanding;
  private StandingsState cachedState;
  private LeaderboardMode cachedMode;
  private long cachedBest;
  private bool cachedGoalsShown;
  private bool built;
  private float rowsTop = RunHudLayout.ListY;
  private float rowHeight;
  private float textScale;
  private float textAlpha;
  private TimeStyle timeStyle;
  private float widest;
  private float nameX;
  private float duck = 1f;

  private readonly record struct Row(
    string Rank,
    string Name,
    string Time,
    Color Color,
    bool Message = false,
    float NameFit = 1f
  );

  public LeaderboardDisplay()
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate;
  }

  private static LeaderboardMode Mode => ClimbTrialsModule.Settings.ShowLeaderboard;

  private static bool Shown =>
    Mode != LeaderboardMode.Off
    && RunManager.Run != null
    && RunManager.Trial != null
    && (!RunManager.Replaying || RunManager.Replay.IsOwn);

  private float BoxBottom => rowsTop + rows.Count * rowHeight + RunHudLayout.BoxPaddingBottom;

  public override void Added(Scene scene)
  {
    base.Added(scene);
    Refresh();
  }

  public override void Update()
  {
    base.Update();
    Level level = SceneAs<Level>();
    Player player = level.Tracker.GetEntity<Player>();
    float target = 1f;
    if (player != null)
    {
      Vector2 screen = level.WorldToScreen(player.Center);
      target = RunHudLayout.DuckTarget(screen, widest + RunHudLayout.BoxPaddingRight, BoxBottom);
    }
    duck = Calc.Approach(duck, target, Engine.RawDeltaTime / RunHudLayout.DuckTime);
    foreach (FadeBuffer layer in layers)
      layer.Visible = Shown;
    Refresh();
  }

  private void Refresh()
  {
    Trial trial = RunManager.Trial;
    if (trial == null)
      return;
    TrialStanding standing = Standings.For(trial);
    StandingsState state = Standings.State(Campaigns.CampaignOf(trial));
    LeaderboardMode mode = Mode;
    long best = TrialRecords.Get(trial.Id)?.BestTicks ?? long.MaxValue;
    bool goalsShown = GoalListDisplay.Shown;
    bool same =
      built
      && ReferenceEquals(standing, cachedStanding)
      && state == cachedState
      && mode == cachedMode
      && best == cachedBest
      && goalsShown == cachedGoalsShown;
    if (same)
      return;
    built = true;
    cachedStanding = standing;
    cachedState = state;
    cachedMode = mode;
    cachedBest = best;
    cachedGoalsShown = goalsShown;

    textScale = mode == LeaderboardMode.Subtle ? SubtleTextScale : NormalTextScale;
    textAlpha = mode == LeaderboardMode.Subtle ? SubtleTextAlpha : NormalTextAlpha;
    rowHeight = textScale * RowHeightPerScale;
    timeStyle = TimeText.SmallHud.At(textScale);
    rowsTop = RowsTop(trial, best, goalsShown);
    rows = RowsOf(standing, state);
    MeasureColumns();
    RebuildLayers();
  }

  private static float RowsTop(Trial trial, long best, bool goalsShown)
  {
    int goalRows = goalsShown ? GoalListDisplay.RowCount(trial, best) : 0;
    if (goalRows == 0)
      return RunHudLayout.ListY;
    return RunHudLayout.GoalListBottom(goalRows) + GapBelowGoals + RunHudLayout.BoxPaddingTop;
  }

  private static List<Row> RowsOf(TrialStanding standing, StandingsState state)
  {
    if (standing == null || state == StandingsState.Offline)
      return
      [
        Message(
          ClimbTrialsModule.Settings.OfflineMode
            ? "climbtrials_offline_mode"
            : "climbtrials_board_unavailable"
        ),
      ];
    if (standing.Total == 0)
      return [Message("climbtrials_board_no_times")];
    List<Row> rows = [];
    foreach (BoardRow row in standing.Window)
    {
      if (rows.Count == 3 && row.Rank > 4)
        rows.Add(new Row(Ellipsis, "", "", Color.White));
      Color color = row.Me ? myTimeColor : Color.White;
      rows.Add(
        new Row(
          $"#{row.Rank}",
          row.Name,
          TimeText.Format(row.Ticks),
          color,
          NameFit: NameFitFor(row.Name)
        )
      );
    }
    return rows;
  }

  private static float NameFitFor(string name) =>
    name.Length <= NameFullChars
      ? 1f
      : TextFit.Scale(name, ActiveFont.Measure(name[..NameFullChars]).X, NameMinFit);

  private static Row Message(string key) =>
    new(Dialog.Clean(key), "", "", Colors.DimText, Message: true);

  private void MeasureColumns()
  {
    float rankColumn = 0f;
    float widestName = 0f;
    float widestTime = 0f;
    widest = 0f;
    float columnGap = textScale * ColumnGapPerScale;
    foreach (Row row in rows)
    {
      float rank = ActiveFont.Measure(row.Rank).X * textScale;
      widest = Math.Max(widest, RankX + rank);
      if (!row.Message)
        rankColumn = Math.Max(rankColumn, rank);
      widestName = Math.Max(widestName, ActiveFont.Measure(row.Name).X * textScale * row.NameFit);
      widestTime = Math.Max(widestTime, TimeText.Width(row.Time, timeStyle));
    }
    nameX = RankX + rankColumn + columnGap;
    float timeX = nameX + widestName + columnGap;
    if (widestTime > 0f)
      widest = Math.Max(widest, timeX + widestTime);
  }

  private void RebuildLayers()
  {
    foreach (FadeBuffer layer in layers)
      Remove(layer);
    layers.Clear();
    int width = (int)Math.Ceiling(widest + RunHudLayout.BoxPaddingRight);
    for (int i = 0; i < rows.Count; i++)
    {
      int index = i;
      FadeBuffer layer = new(
        "climbtrials-board-row",
        width,
        (int)Math.Ceiling(rowHeight) + 2 * RunHudLayout.RowPad,
        () => DrawRow(index)
      )
      {
        Origin = new Vector2(0f, rowsTop + index * rowHeight - RunHudLayout.RowPad),
      };
      Add(layer);
      layers.Add(layer);
    }
  }

  private void DrawRow(int index)
  {
    Row row = rows[index];
    float y = rowsTop + index * rowHeight + rowHeight / 2f;
    Vector2 justify = new(0f, 0.5f);
    Vector2 scale = Vector2.One * textScale;
    ActiveFont.DrawOutline(
      row.Rank,
      new Vector2(RankX, y),
      justify,
      scale,
      row.Color,
      1.35f,
      Color.Black
    );
    if (row.Name.Length > 0)
      ActiveFont.DrawOutline(
        row.Name,
        new Vector2(nameX, y),
        justify,
        scale * row.NameFit,
        row.Color,
        1.35f,
        Color.Black
      );
    if (row.Time.Length > 0)
    {
      float right = widest - TimeText.Width(row.Time, timeStyle);
      Vector2 baseline = new(right, y + TimeText.Height(timeStyle) / 2f);
      TimeText.Draw(baseline, row.Time, timeStyle, row.Color);
    }
  }

  public override void Render()
  {
    if (!Shown || rows.Count == 0)
      return;

    Draw.Rect(
      0f,
      rowsTop - RunHudLayout.BoxPaddingTop,
      widest + RunHudLayout.BoxPaddingRight,
      rows.Count * rowHeight + RunHudLayout.BoxPaddingTop + RunHudLayout.BoxPaddingBottom,
      boxColor * (RunHudLayout.BoxAlpha * duck)
    );

    foreach (FadeBuffer layer in layers)
      layer.Alpha = textAlpha * duck;
    base.Render();
  }
}
