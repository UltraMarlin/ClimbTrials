using System.Collections;
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
  private const float NoticeY = 975f;
  private const float NoticeScale = 0.6f;

  private const float SlideWidth = 1920f;

  protected TrialsMenu menu;
  protected readonly TransientText notice = new();
  protected float x = OnScreenX;
  protected float alpha;
  protected float yOffset;
  private float dim = 1f;
  protected abstract int ScreenDepth { get; }

  private static int DepthOf(Oui other) => (other as ListScreen)?.ScreenDepth ?? 0;

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

    for (float ease = 0f; ease < 1f; ease += Engine.DeltaTime * 4f)
    {
      x = OnScreenX + side * SlideWidth * (1f - Ease.CubeOut(ease));
      alpha = Ease.CubeOut(ease);
      menu.X = x;
      menu.Alpha = alpha;
      yield return null;
    }

    x = OnScreenX;
    alpha = 1f;
    menu.X = x;
    menu.Alpha = alpha;
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
    for (float ease = 0f; ease < 1f; ease += Engine.DeltaTime * 4f)
    {
      x = OnScreenX + side * SlideWidth * Ease.CubeIn(ease);
      alpha = 1f - Ease.CubeIn(ease);
      menu.X = x;
      menu.Alpha = alpha;
      yield return null;
    }
    menu.RemoveSelf();
    menu = null;
    Visible = false;
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
      Dialog.Clean("climbtrials_trials_title"),
      new Vector2(x, TitleY + yOffset),
      new Vector2(0.5f, 0.5f),
      Vector2.One * 1.6f,
      Color.White * totalAlpha,
      2f,
      TextOutline.StrokeFor(Color.White * totalAlpha)
    );
    RenderHeader();
    RenderPrompts(totalAlpha);
    notice.Render(new Vector2(x, NoticeY + yOffset), NoticeScale, totalAlpha);
    if (ModCheck.OthersLoaded)
      ActiveFont.DrawOutline(
        ModCheck.MenuWarning(),
        new Vector2(x, StatusY + yOffset),
        new Vector2(0.5f, 0.5f),
        Vector2.One * 0.45f,
        Color.White * 0.7f * totalAlpha,
        2f,
        TextOutline.StrokeFor(Color.White * 0.7f * totalAlpha)
      );
  }
}
