using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class ReplayBanner : Entity
{
  private const float TitleY = 48f;
  private const float TimeBaselineY = 128f;
  private const float InfoY = 138f;
  private static readonly TimeStyle TimeStyle = TimeText.Hud.At(0.8f);
  private static readonly Color TitleColor = Calc.HexToColor("ffd54f");
  private static readonly Color DesyncColor = Calc.HexToColor("f0777a");
  private static readonly Color Dim = Color.White * 0.7f;

  private const float PlayingAlpha = 0.3f;
  private const float FadeTime = 0.15f;
  private float alpha = TargetAlpha();
  private readonly FadeBuffer layer;

  public ReplayBanner()
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate | Tags.FrozenUpdate;
    Add(layer = new FadeBuffer("climbtrials-replay-banner", 1920, 1080, DrawContent));
  }

  public override void Update()
  {
    base.Update();
    if (!RunManager.Replaying)
      RemoveSelf();
  }

  private static float TargetAlpha() => Replayer.State == ReplayState.Playing ? PlayingAlpha : 1f;

  public override void Render()
  {
    alpha = Calc.Approach(alpha, TargetAlpha(), Engine.RawDeltaTime / FadeTime);
    if (RunManager.Replay == null || SceneAs<Level>().Paused)
      return;
    layer.Alpha = alpha;
    base.Render();
  }

  private void DrawContent()
  {
    Replay replay = RunManager.Replay;
    if (replay == null)
      return;

    string titleKey =
      Replayer.Desynced ? "climbtrials_replay_desynced"
      : Replayer.State == ReplayState.Paused ? "climbtrials_replay_paused"
      : "climbtrials_replay_title";
    Outline(Dialog.Clean(titleKey), TitleY, 1.1f, Replayer.Desynced ? DesyncColor : TitleColor);

    string info =
      $"{replay.PlayerName},  {Dialog.Clean("climbtrials_replay_played_date")} {TimeText.FormatDate(replay.AchievedAtUtc)}";
    if (!replay.MatchesGame())
      info += $",  {Dialog.Clean("climbtrials_replay_recorded_on")} {replay.CelesteVersion}";
    Outline(info, InfoY, 0.5f, Dim);

    string time = TimeText.Format(replay.Ticks);
    float width = TimeText.Width(time, TimeStyle);
    TimeText.Draw(new Vector2(960f - width / 2f, TimeBaselineY), time, TimeStyle, Color.White);

    if (Replayer.State != ReplayState.Idle)
      RenderPrompts(1f);
  }

  private static void RenderPrompts(float alpha)
  {
    string pauseKey =
      Replayer.State == ReplayState.Paused
        ? "climbtrials_replay_resume"
        : "climbtrials_replay_pause";
    List<(string, VirtualButton)> prompts =
    [
      (Dialog.Clean("climbtrials_finish_back"), Input.MenuCancel),
    ];

    prompts.Add((Dialog.Clean("climbtrials_replay_restart"), Input.MenuJournal));
    prompts.Add((Dialog.Clean("climbtrials_replay_play"), ClimbTrialsModule.Settings.Retry.Button));
    if (Replayer.State != ReplayState.Finished)
      prompts.Add((Dialog.Clean(pauseKey), Input.MenuConfirm));
    ButtonRow.DrawRight(alpha, prompts);
  }

  private static void Outline(string text, float y, float scale, Color color)
  {
    ActiveFont.DrawOutline(
      text,
      new Vector2(960f, y),
      new Vector2(0.5f, 0.5f),
      Vector2.One * scale,
      color,
      2f,
      TimeText.StrokeFor(color)
    );
  }
}
