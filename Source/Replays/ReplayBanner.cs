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
  private const float TimeScale = 0.8f;
  private static readonly Color TitleColor = Calc.HexToColor("ffd54f");
  private static readonly Color DesyncColor = Calc.HexToColor("f0777a");
  private static readonly Color Dim = Color.White * 0.7f;

  public ReplayBanner()
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate | Tags.FrozenUpdate;
  }

  public override void Update()
  {
    base.Update();
    if (!RunManager.Replaying)
      RemoveSelf();
  }

  public override void Render()
  {
    Replay replay = RunManager.Replay;
    if (replay == null || SceneAs<Level>().Paused)
      return;

    string titleKey =
      Replayer.Desynced ? "climbtrials_replay_desynced"
      : Replayer.Paused ? "climbtrials_replay_paused"
      : "climbtrials_replay_title";
    Outline(Dialog.Clean(titleKey), TitleY, 1.1f, Replayer.Desynced ? DesyncColor : TitleColor);

    string info =
      $"{replay.PlayerName},  {Dialog.Clean("climbtrials_replay_played_date")} {replay.AchievedAtUtc.ToLocalTime():yyyy-MM-dd}";
    if (!replay.MatchesGame())
      info += $",  {Dialog.Clean("climbtrials_replay_recorded_on")} {replay.CelesteVersion}";
    Outline(info, InfoY, 0.5f, Dim);

    string time = Attempt.Format(replay.Ticks);
    float width = TimeText.Width(time, TimeScale);
    TimeText.Draw(
      new Vector2(960f - width / 2f, TimeBaselineY),
      time,
      TimeScale,
      Color.White,
      Color.LightGray
    );

    if (Replayer.Feeding)
      RenderPrompts();
  }

  private static void RenderPrompts()
  {
    ButtonPrompt.Draw(
      Settings.Instance.Cancel,
      Dialog.Clean("climbtrials_finish_back"),
      new Vector2(ButtonPrompt.LeftX, ButtonPrompt.RowY),
      ButtonPrompt.Align.Left,
      1f
    );
    ButtonPrompt.Draw(
      Settings.Instance.Confirm,
      Dialog.Clean(Replayer.Paused ? "climbtrials_replay_resume" : "climbtrials_replay_pause"),
      new Vector2(ButtonPrompt.CenterX, ButtonPrompt.RowY),
      ButtonPrompt.Align.Center,
      1f
    );
    if (Replayer.Paused)
      return;
    ButtonPrompt.Draw(
      ClimbTrialsModule.Settings.Retry.Binding,
      Dialog.Clean("climbtrials_replay_play"),
      new Vector2(ButtonPrompt.RightX, ButtonPrompt.RowY),
      ButtonPrompt.Align.Right,
      1f
    );
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
      Color.Black
    );
  }
}
