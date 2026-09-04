using System;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class RunNotice : Entity
{
  private const float NoticeDuration = 3f;
  private const float NoticeY = 200f;
  private const float StatusY = 1040f;
  private const string NoticeSound = "event:/ui/main/button_invalid";
  private static readonly Color NoticeColor = Calc.HexToColor("f0777a");

  private string notice;
  private float noticeAge = -1f;

  public RunNotice()
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate | Tags.FrozenUpdate;
  }

  public void Show(string dialogKey)
  {
    notice = Dialog.Clean(dialogKey);
    noticeAge = 0f;
    Audio.Play(NoticeSound);
  }

  public override void Update()
  {
    base.Update();
    if (RunManager.Run == null)
    {
      RemoveSelf();
      return;
    }
    if (noticeAge >= 0f)
      noticeAge += Engine.RawDeltaTime;
  }

  public override void Render()
  {
    if (SceneAs<Level>().Paused)
      return;
    if (noticeAge >= 0f && noticeAge < NoticeDuration)
    {
      float alpha = Math.Clamp((NoticeDuration - noticeAge) / 0.5f, 0f, 1f);
      ActiveFont.DrawOutline(
        notice,
        new Vector2(960f, NoticeY),
        new Vector2(0.5f, 0.5f),
        Vector2.One * 0.8f,
        NoticeColor * alpha,
        2f,
        Color.Black * alpha
      );
    }
    string status = StatusText();
    if (status == null)
      return;
    ActiveFont.DrawOutline(
      status,
      new Vector2(960f, StatusY),
      new Vector2(0.5f, 0.5f),
      Vector2.One * 0.5f,
      Color.White * 0.7f,
      2f,
      Color.Black * 0.7f
    );
  }

  private static string StatusText()
  {
    if (RunManager.Replaying)
      return null;
    if (!RunManager.RecordingEnabled)
      return Dialog.Clean("climbtrials_recording_off") + " " + string.Join(", ", ModCheck.Others());
    if (Recorder.StoppedAtLimit)
      return Dialog.Clean("climbtrials_replay_limit")
        + " "
        + Attempt.FormatSeconds(RunManager.ReplayLimitSeconds);
    return null;
  }
}
