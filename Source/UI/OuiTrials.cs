using System.Collections;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiTrials : Oui
{
  public static bool ReturnPending;
  private static int lastSelected;

  private const float OnScreenX = 960f,
    OffScreenX = 2880f;
  private const float RowHeight = 90f,
    FirstRowY = 360f,
    TitleY = 200f;
  private static readonly Color Dim = Color.White * 0.55f;
  private static readonly Color Highlight = Calc.HexToColor("6ded87");

  private readonly Campaign campaign = Campaigns.All[0];
  private int selected;
  private float x = OffScreenX;
  private float alpha;
  private bool starting;

  public override IEnumerator Enter(Oui from)
  {
    selected = lastSelected;
    starting = false;
    Visible = true;
    for (float ease = 0f; ease < 1f; ease += Engine.DeltaTime * 4f)
    {
      x = OffScreenX - 1920f * Ease.CubeOut(ease);
      alpha = Ease.CubeOut(ease);
      yield return null;
    }
    x = OnScreenX;
    alpha = 1f;
    Focused = true;
  }

  public override IEnumerator Leave(Oui next)
  {
    Focused = false;
    for (float ease = 0f; ease < 1f; ease += Engine.DeltaTime * 4f)
    {
      x = OnScreenX + 1920f * Ease.CubeIn(ease);
      alpha = 1f - Ease.CubeIn(ease);
      yield return null;
    }
    Visible = false;
  }

  public override bool IsStart(Overworld overworld, Overworld.StartMode start)
  {
    if (!ReturnPending)
      return false;
    ReturnPending = false;
    Add(new Coroutine(Enter(null)));
    return true;
  }

  public override void Update()
  {
    base.Update();
    if (!Focused || !Selected || starting)
      return;

    if (Input.MenuDown.Pressed && selected < campaign.Trials.Count - 1)
    {
      selected++;
      Audio.Play("event:/ui/main/rollover_down");
    }
    if (Input.MenuUp.Pressed && selected > 0)
    {
      selected--;
      Audio.Play("event:/ui/main/rollover_up");
    }
    lastSelected = selected;

    if (Input.MenuCancel.Pressed)
    {
      Audio.Play(SFX.ui_main_button_back);
      Overworld.Goto<OuiMainMenu>();
    }
    else if (Input.MenuConfirm.Pressed)
    {
      Audio.Play("event:/ui/main/button_select");
      starting = true;
      Add(new Coroutine(StartRoutine(campaign.Trials[selected])));
    }
  }

  private IEnumerator StartRoutine(Trial trial)
  {
    Focused = false;
    AreaData area = AreaData.Get(trial.Run.Sid);
    area.Wipe(Overworld, false, null);
    Audio.SetMusic(null, true, true);
    Audio.SetAmbience(null, true);
    yield return 0.5f;
    RunManager.StartTrial(trial);
  }

  public override void Render()
  {
    if (alpha <= 0f)
      return;
    Color title = Color.White * alpha;
    ActiveFont.DrawOutline(
      Dialog.Clean("climbtrials_trials_title"),
      new Vector2(x, TitleY),
      new Vector2(0.5f, 0.5f),
      Vector2.One * 1.6f,
      title,
      2f,
      Color.Black * alpha
    );
    ActiveFont.Draw(
      campaign.Name,
      new Vector2(x, TitleY + 70f),
      new Vector2(0.5f, 0.5f),
      Vector2.One * 0.8f,
      Dim * alpha
    );

    for (int i = 0; i < campaign.Trials.Count; i++)
    {
      Trial trial = campaign.Trials[i];
      float y = FirstRowY + i * RowHeight;
      bool isSelected = i == selected;

      GFX.Gui[trial.SkullTexture]
        .DrawCentered(
          new Vector2(x - 420f, y),
          Color.White,
          Vector2.One * (isSelected ? 1.1f : 1f)
        );

      ActiveFont.DrawOutline(
        trial.Name,
        new Vector2(x - 360f, y),
        new Vector2(0f, 0.5f),
        Vector2.One * (isSelected ? 1.1f : 1f),
        (isSelected ? Highlight : Color.White) * alpha,
        2f,
        Color.Black * alpha
      );

      ActiveFont.Draw(
        "--:--.---",
        new Vector2(x + 420f, y),
        new Vector2(1f, 0.5f),
        Vector2.One * 0.9f,
        Dim * alpha
      );
    }
  }
}
