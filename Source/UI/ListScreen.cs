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
  private const float StatusY = 1060f;
  protected const float NoticeY = 975f;
  private const float OfflineY = 190f;
  private const float OfflineScale = 0.45f;
  private const float NoticeScale = 0.6f;

  protected TrialsMenu menu;
  protected readonly TransientText notice = new();
  protected string hintKey;
  protected float x = OnScreenX;
  protected float alpha;
  private float dim = 1f;
  protected abstract int ScreenDepth { get; }
  protected virtual string TitleKey => "climbtrials_campaigns_title";

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

  protected void SetDim(float value)
  {
    dim = value;
    menu?.Alpha = alpha * dim;
  }

  protected void DrawOfflineNote(StandingsState state)
  {
    if (state != StandingsState.Offline)
      return;
    ActiveFont.Draw(
      Standings.OfflineNote,
      new Vector2(menu.X, OfflineY),
      new Vector2(0.5f, 0.5f),
      Vector2.One * OfflineScale,
      RowLayout.Dim * menu.Alpha
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

    x = OnScreenX;
    alpha = 1f;
    menu.X = x;
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
    x = OnScreenX + offset;
    alpha = shown;
    menu.X = x;
    menu.Alpha = alpha * dim;
  }

  public override void Update()
  {
    base.Update();
    notice.Update(Engine.RawDeltaTime);
  }

  public override void Render()
  {
    if (alpha <= 0f)
      return;
    float totalAlpha = alpha * dim;
    ActiveFont.DrawOutline(
      Dialog.Clean(TitleKey),
      new Vector2(x, TitleY),
      new Vector2(0.5f, 0.5f),
      Vector2.One * 1.6f,
      Color.White * totalAlpha,
      2f,
      TextOutline.StrokeFor(Color.White * totalAlpha)
    );
    RenderHeader();
    RenderPrompts(totalAlpha);
    notice.Render(new Vector2(x, NoticeY), NoticeScale, totalAlpha);
    if (hintKey != null && !notice.Showing)
      TextOutline.DrawDim(
        $"{Dialog.Clean("climbtrials_hint_prefix")} {Dialog.Clean(hintKey)}",
        new Vector2(x, NoticeY),
        new Vector2(0.5f, 0.5f),
        NoticeScale,
        totalAlpha
      );
    if (ModCheck.OthersLoaded)
      ActiveFont.DrawOutline(
        ModCheck.MenuWarning(),
        new Vector2(x, StatusY),
        new Vector2(0.5f, 0.5f),
        Vector2.One * 0.45f,
        Color.White * 0.7f * totalAlpha,
        2f,
        TextOutline.StrokeFor(Color.White * 0.7f * totalAlpha)
      );
  }
}
