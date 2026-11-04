using System.Collections;
using Celeste.Mod.ClimbTrials.Online;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public abstract class ListScreen : Oui
{
  public const float OnScreenX = 960f;
  public const float TitleY = 120f;
  public const float TitleScale = 1.6f;
  private const float OfflineX = 60f;
  private const float OfflineScale = 0.5f;
  private const float PollSeconds = 60f;

  protected TrialsMenu menu;
  protected readonly TransientText notice = new();
  protected string hintKey;
  protected float screenX = OnScreenX;
  protected float alpha;
  private float dim = 1f;
  private float sinceFetch;
  protected abstract int ScreenDepth { get; }
  protected virtual string TitleKey => "climbtrials_campaigns_title";
  protected virtual float TitleLift => 0f;

  public static int DepthOf(Oui other) =>
    other switch
    {
      ListScreen screen => screen.ScreenDepth,
      OuiClimbTrialsOptions => OuiClimbTrialsOptions.ScreenDepth,
      _ => 0,
    };

  protected bool IsCurrentScreen => Overworld.Current == this || Overworld.Next == this;

  protected abstract TrialsMenu BuildMenu();

  protected abstract int InitialSelection(Oui from);

  protected virtual void RenderHeader() { }

  protected virtual void RenderPrompts(float alpha) { }

  protected virtual void CancelTransitions() { }

  protected virtual void FetchOnline() { }

  protected virtual bool Reopen() => false;

  protected void SelectRow(TextMenu.Item row)
  {
    if (menu.Current == row)
      return;
    menu.Current?.OnLeave?.Invoke();
    menu.Selection = menu.Items.IndexOf(row);
    menu.Current?.OnEnter?.Invoke();
  }

  protected void SetDim(float value)
  {
    dim = value;
    menu?.Alpha = alpha * dim;
  }

  protected abstract StandingsState ServerState { get; }

  private void DrawOfflineLabel()
  {
    string key =
      ClimbTrialsModule.Settings.OfflineMode ? "climbtrials_offline_mode"
      : ServerState == StandingsState.Offline ? "climbtrials_offline"
      : null;
    if (key != null)
      TextOutline.DrawDim(
        Dialog.Clean(key),
        new Vector2(OfflineX, ButtonRow.Y),
        new Vector2(0f, 0.5f),
        OfflineScale,
        alpha
      );
  }

  public override IEnumerator Enter(Oui from)
  {
    if (from == null)
    {
      Overworld.Mountain.SnapCamera(
        -1,
        new MountainCamera(new Vector3(0f, 6f, 12f), MountainRenderer.RotateLookAt)
      );
      Overworld.Mountain.GotoRotationMode();
      Overworld.Maddy.Hide();
    }
    TrialRecords.EnsureLoaded();
    ReplayStore.Refresh();
    FetchOnline();
    sinceFetch = 0f;
    menu = BuildMenu();
    menu.Selection = InitialSelection(from);
    menu.Focused = false;
    Scene.Add(menu);
    menu.Current?.OnEnter?.Invoke();
    Visible = true;
    bool handedOver = Reopen();
    float side = DepthOf(from) > ScreenDepth ? -1f : 1f;

    if (from != null)
    {
      PlaceSlide(side * Motion.SlideDistance, 0f);
      yield return Motion.EnterScreen(side, PlaceSlide);
    }

    screenX = OnScreenX;
    alpha = 1f;
    menu.X = screenX;
    menu.Alpha = alpha * dim;
    if (handedOver)
      yield break;

    menu.Focused = true;
    Focused = true;
  }

  public override IEnumerator Leave(Oui next)
  {
    Focused = false;
    notice.Clear();
    CancelTransitions();
    menu.Focused = false;
    float side = DepthOf(next) > ScreenDepth ? -1f : 1f;
    TrialsMenu leaving = menu;
    yield return Motion.LeaveScreen(
      this,
      side,
      (offset, shown) =>
      {
        if (menu == leaving)
          PlaceSlide(offset, shown);
      },
      () => Gone(leaving)
    );
  }

  private void Gone(TrialsMenu leaving)
  {
    leaving.RemoveSelf();
    if (menu != leaving)
      return;
    menu = null;
    Visible = false;
  }

  private void PlaceSlide(float offset, float shown)
  {
    screenX = OnScreenX + offset;
    alpha = shown;
    menu.X = screenX;
    menu.Alpha = alpha * dim;
  }

  public override void Update()
  {
    base.Update();
    notice.Update(Engine.RawDeltaTime);
    if (!Focused || menu?.Focused != true)
      return;
    if (ClimbTrialsModule.Settings.ToggleSpeedrunTool.Pressed && SpeedrunToolBridge.Loaded)
      ToggleSpeedrunTool();
    sinceFetch += Engine.RawDeltaTime;
    if (sinceFetch < PollSeconds)
      return;
    sinceFetch = 0f;
    ServerClient.RunWithoutIcon(FetchOnline);
  }

  private void ToggleSpeedrunTool()
  {
    ClimbTrialsModule.Settings.ToggleSpeedrunTool.ConsumePress();
    SpeedrunToolBridge.Toggle(out string noticeKey);
    notice.Show(Dialog.Clean(noticeKey), ScreenLayout.NoticeSeconds, warning: false);
  }

  public override void Render()
  {
    if (alpha <= 0f)
      return;
    float totalAlpha = alpha * dim;
    ActiveFont.DrawOutline(
      Dialog.Clean(TitleKey),
      new Vector2(screenX, TitleY - TitleLift),
      new Vector2(0.5f, 0.5f),
      Vector2.One * TitleScale,
      Color.White * totalAlpha,
      2f,
      TextOutline.StrokeFor(Color.White * totalAlpha)
    );
    RenderHeader();
    RenderPrompts(totalAlpha);
    DrawOfflineLabel();
    notice.Render(new Vector2(screenX, ScreenLayout.NoticeY), ScreenLayout.NoticeScale, totalAlpha);
    if (hintKey != null && !notice.Showing)
      TextOutline.DrawDim(
        $"{Dialog.Clean("climbtrials_hint_prefix")} {Dialog.Clean(hintKey)}",
        new Vector2(screenX, ScreenLayout.NoticeY),
        new Vector2(0.5f, 0.5f),
        ScreenLayout.NoticeScale,
        totalAlpha
      );
    string status =
      SpeedrunToolBridge.Enabled ? Dialog.Clean("climbtrials_practice_status")
      : ModCheck.OthersLoaded ? ModCheck.MenuWarning()
      : null;
    if (status != null)
      ActiveFont.DrawOutline(
        status,
        new Vector2(screenX, ScreenLayout.StatusY),
        new Vector2(0.5f, 0.5f),
        Vector2.One * 0.45f,
        Color.White * 0.7f * totalAlpha,
        2f,
        TextOutline.StrokeFor(Color.White * 0.7f * totalAlpha)
      );
  }
}
