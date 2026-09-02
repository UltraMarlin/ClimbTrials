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
    TitleY = 200f,
    ListPaddingX = 360f;
  private const float PbScale = 0.85f;
  private const float PbBaseline = 30f;
  private const float MedalGap = 34f;
  private const float MedalIconHeight = 64f;
  private static readonly Color Dim = Color.White * 0.7f;
  private static readonly Color Highlight = Calc.HexToColor("6ded87");

  private readonly Campaign campaign = Campaigns.All[0];

  private readonly bool ShowDeletePbsRow = true;
  private int DeleteRow => ShowDeletePbsRow ? campaign.Trials.Count : -1;
  private int RowCount => campaign.Trials.Count + (ShowDeletePbsRow ? 1 : 0);
  private int selected;
  private float x = OffScreenX;
  private float alpha;
  private bool starting;

  public override IEnumerator Enter(Oui from)
  {
    TrialRecords.EnsureLoaded();
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

  public override void Update()
  {
    base.Update();
    if (!Focused || !Selected || starting)
      return;

    if (Input.MenuDown.Pressed && selected < RowCount - 1)
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
    else if (Input.MenuConfirm.Pressed && selected == DeleteRow)
    {
      Audio.Play("event:/ui/main/button_back");
      TrialRecords.Clear();
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
      float xListStart = ListPaddingX;
      float xListEnd = 1920f - ListPaddingX;

      GFX.Gui[trial.SkullTexture]
        .DrawCentered(
          new Vector2(xListStart + 28f, y),
          Color.White * alpha,
          Vector2.One * (isSelected ? 1f : 0.9f)
        );

      ActiveFont.DrawOutline(
        trial.Name,
        new Vector2(xListStart + 86f, y),
        new Vector2(0f, 0.5f),
        Vector2.One * (isSelected ? 1f : 0.9f),
        (isSelected ? Highlight : Color.White) * alpha,
        2f,
        Color.Black * alpha
      );

      TrialRecord record = TrialRecords.Get(trial.Id);
      if (record == null)
      {
        ActiveFont.Draw(
          "new",
          new Vector2(xListEnd - 4f, y),
          new Vector2(1f, 0.5f),
          Vector2.One * (isSelected ? 0.9f : 0.8f),
          Dim * alpha
        );
      }
      else
      {
        string best = Attempt.Format(record.BestTicks);
        float scale = isSelected ? PbScale + 0.1f : PbScale;
        float width = TimeText.Width(best, scale);
        Medal medal = trial.Goals?.MedalFor(record.BestTicks) ?? Medal.None;
        if (medal != Medal.None)
          MedalIcon.Draw(
            new Vector2(xListEnd - width - MedalGap, y),
            medal,
            MedalIconHeight,
            alpha
          );
        TimeText.Draw(
          new Vector2(xListEnd - width, y + PbBaseline),
          best,
          scale,
          Color.White * alpha,
          Color.LightGray * alpha
        );
      }
    }

    if (!ShowDeletePbsRow)
      return;
    bool deleteSelected = selected == DeleteRow;
    ActiveFont.DrawOutline(
      Dialog.Clean("climbtrials_trials_delete_pbs"),
      new Vector2(ListPaddingX + 86f, FirstRowY + DeleteRow * RowHeight + RowHeight / 2f),
      new Vector2(0f, 0.5f),
      Vector2.One * (deleteSelected ? 0.9f : 0.8f),
      (deleteSelected ? Highlight : Dim) * alpha,
      2f,
      Color.Black * alpha
    );
  }
}
