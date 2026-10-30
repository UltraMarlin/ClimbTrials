using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Online;
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
  private const float TitleStroke = 3f;

  private const float PlayingAlpha = 0.3f;
  private const float FadeTime = 0.15f;
  private float alpha = TargetAlpha();
  private readonly FadeBuffer layer;

  public ReplayBanner()
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate | Tags.FrozenUpdate;
    Depth = Depths.Top;
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
    if (RunManager.Replay == null)
      return;
    if (!Replayer.MenuOpen)
    {
      layer.Alpha = alpha;
      base.Render();
    }
    WatcherOverlay.Render();
  }

  private void DrawContent()
  {
    Replay replay = RunManager.Replay;
    if (replay == null)
      return;

    bool stopped = Replayer.Desynced || Replayer.StoppedByReload;
    string titleKey =
      Replayer.StoppedByReload ? "climbtrials_replay_stopped_reload"
      : Replayer.Desynced ? "climbtrials_replay_desynced"
      : Replayer.State == ReplayState.Paused || Replayer.PauseQueued ? "climbtrials_replay_paused"
      : "climbtrials_replay_title";
    Outline(Dialog.Clean(titleKey), TitleY, 1.1f, stopped ? DesyncColor : TitleColor, TitleStroke);

    string played = Dialog.Clean("climbtrials_replay_played_date");
    string owner = Identity.NameOf(replay);
    string info = string.IsNullOrEmpty(owner) ? "" : $"{owner}, ";
    info += $"{played} {TimeText.FormatDate(replay.AchievedAtUtc)}";
    if (!replay.MatchesGame())
      info += $",  {Dialog.Clean("climbtrials_replay_recorded_on")} {replay.CelesteVersion}";
    TextOutline.DrawDim(info, new Vector2(960f, InfoY), new Vector2(0.5f, 0.5f), 0.5f, 1f);

    string time = TimeText.Format(replay.Ticks);
    float width = TimeText.Width(time, TimeStyle);
    TimeText.Draw(new Vector2(960f - width / 2f, TimeBaselineY), time, TimeStyle, Color.White);

    if (Replayer.State != ReplayState.Idle || Replayer.WaitingToStart)
      RenderPrompts(1f);
  }

  private static void RenderPrompts(float alpha)
  {
    ClimbTrialsModuleSettings binds = ClimbTrialsModule.Settings;
    bool paused = Replayer.State == ReplayState.Paused || Replayer.PauseQueued;
    string pauseKey = paused ? "climbtrials_replay_resume" : "climbtrials_pause";
    List<(string, VirtualButton)> prompts = [ButtonRow.MenuPrompt];
    if (Replayer.State == ReplayState.Finished)
      prompts.Add((Dialog.Clean("climbtrials_return_to_trials"), Input.MenuCancel));
    prompts.Add((Dialog.Clean("climbtrials_replay_restart"), binds.ReplayRestart.Button));
    if (Replayer.State != ReplayState.Finished)
      prompts.Add((Dialog.Clean(pauseKey), binds.ReplayPlayPause.Button));
    if (Replayer.State == ReplayState.Paused)
      prompts.Add(
        (Dialog.Clean("climbtrials_replay_frame_advance"), binds.ReplayFrameAdvance.Button)
      );
    ButtonRow.DrawRight(alpha, prompts);
  }

  private static void Outline(
    string text,
    float centerY,
    float scale,
    Color color,
    float stroke = 2f
  )
  {
    Vector2 position = new(960f, centerY);
    Vector2 justify = new(0.5f, 0.5f);
    Vector2 size = Vector2.One * scale;
    Color strokeColor = TextOutline.StrokeFor(color);
    if (stroke > 2f)
      TextOutline.DrawRoundOutline(text, position, justify, size, color, stroke, strokeColor);
    else
      ActiveFont.DrawOutline(text, position, justify, size, color, stroke, strokeColor);
  }
}
