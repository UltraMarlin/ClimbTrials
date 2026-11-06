using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class RunNotice : Entity
{
  private const float NoticeDuration = 3f;
  private const float NoticeY = 200f;
  private const float NoticeScale = 0.8f;
  private const int StatusHeight = 80;
  private const float StatusAlpha = 0.7f;

  private readonly TransientText notice = new();

  public RunNotice()
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate | Tags.FrozenUpdate;
    Add(
      new FadeBuffer("climbtrials-run-status", 1920, StatusHeight, DrawStatus)
      {
        Origin = new Vector2(0f, ScreenLayout.StatusY - StatusHeight / 2f),
        Alpha = StatusAlpha,
      }
    );
  }

  public void Show(string dialogKey)
  {
    notice.Show(Dialog.Clean(dialogKey), NoticeDuration);
  }

  public void ShowText(string text, bool warning)
  {
    notice.Show(text, NoticeDuration, warning);
  }

  public override void Update()
  {
    base.Update();
    if (RunManager.Run == null)
    {
      RemoveSelf();
      return;
    }
    notice.Update(Engine.RawDeltaTime);
  }

  public override void Render()
  {
    if (RunManager.Run == null || SceneAs<Level>().Paused)
      return;

    notice.Render(new Vector2(960f, NoticeY), NoticeScale, 1f);
    base.Render();
  }

  private void DrawStatus()
  {
    string text = StatusText();
    if (text == null)
      return;
    ActiveFont.DrawOutline(
      text,
      new Vector2(960f, ScreenLayout.StatusY),
      new Vector2(0.5f, 0.5f),
      Vector2.One * 0.45f,
      Color.White,
      2f,
      Color.Black
    );
  }

  private static string StatusText()
  {
    if (RunManager.Replaying)
      return null;

    if (RunManager.Practice)
      return Dialog.Clean("climbtrials_practice_status");

    if (Recorder.Stopped == StopReason.Limit)
      return Dialog.Clean("climbtrials_replay_limit")
        + " "
        + TimeText.FormatSeconds(RunManager.Trial.ReplayLimitSeconds);

    if (Recorder.Stopped == StopReason.Reload)
      return Dialog.Clean("climbtrials_replay_reloaded");

    if (Recorder.Stopped == StopReason.State)
      return Dialog.Clean("climbtrials_replay_state");

    if (DebugUse.Causes != DebugCause.None)
      return DebugUse.RunWarning();

    if (ModCheck.OthersLoaded)
      return ModCheck.RunWarning();

    return null;
  }
}
