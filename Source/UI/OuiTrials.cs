using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiTrials : Oui
{
  private static int lastSelected;
  public const Overworld.StartMode ReturnStartMode = (Overworld.StartMode)5602393;

  private const float OnScreenX = 960f,
    OffScreenX = 2880f;
  private const float StatusY = 1060f;
  private const float TitleY = 120f;
  private static readonly Color Dim = Color.White * 0.7f;

  private readonly Campaign campaign = Campaigns.All[0];

  private TrialsMenu menu;
  private float x = OffScreenX;
  private float alpha;
  private bool selectedHasReplay;
  private readonly List<TrialRow> rows = [];

  private void OnSelect(int index)
  {
    lastSelected = index;
    selectedHasReplay = menu?.Current is TrialRow row && ReplayStore.Exists(row.Trial.Id);
  }

  private TrialsMenu BuildMenu()
  {
    TrialsMenu built = new TrialsMenu();
    rows.Clear();
    for (int i = 0; i < campaign.Trials.Count; i++)
    {
      Trial trial = campaign.Trials[i];
      int index = i;
      TrialRow row = new(trial);
      row.Enter(() => OnSelect(index)).Pressed(() => Start(trial)).AltPressed(() => WatchPb(trial));
      built.Add(row);
      row.AddDescription(built, TrialText.Description(trial.Run));
      built.Items[built.Items.IndexOf(row) + 1].IncludeWidthInMeasurement = false;
      rows.Add(row);
    }
    int deleteIndex = campaign.Trials.Count;
    built.Add(
      new TextMenu.Button(Dialog.Clean("climbtrials_trials_delete_pbs"))
      {
        ConfirmSfx = "event:/ui/main/button_back",
      }
        .Enter(() => OnSelect(deleteIndex))
        .Pressed(DeleteAll)
    );
    built.OnCancel = () =>
    {
      built.Focused = false;
      Audio.Play(SFX.ui_main_button_back);
      Overworld.Goto<OuiMainMenu>();
    };
    return built;
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
    int remembered = lastSelected;
    menu = BuildMenu();
    menu.Selection =
      remembered < rows.Count ? menu.Items.IndexOf(rows[remembered]) : menu.LastPossibleSelection;
    menu.Focused = false;
    Scene.Add(menu);
    menu.Current?.OnEnter?.Invoke();
    Visible = true;
    for (float ease = 0f; ease < 1f; ease += Engine.DeltaTime * 4f)
    {
      x = OffScreenX - 1920f * Ease.CubeOut(ease);
      alpha = Ease.CubeOut(ease);
      menu.X = x;
      menu.Alpha = alpha;
      yield return null;
    }
    x = OnScreenX;
    alpha = 1f;
    menu.X = x;
    menu.Alpha = alpha;
    menu.Focused = true;
    Focused = true;
  }

  public override bool IsStart(Overworld overworld, Overworld.StartMode start)
  {
    if (start != ReturnStartMode)
      return false;
    Add(new Coroutine(Enter(null)));
    return true;
  }

  public override IEnumerator Leave(Oui next)
  {
    Focused = false;
    menu.Focused = false;
    for (float ease = 0f; ease < 1f; ease += Engine.DeltaTime * 4f)
    {
      x = OnScreenX + 1920f * Ease.CubeIn(ease);
      alpha = 1f - Ease.CubeIn(ease);
      menu.X = x;
      menu.Alpha = alpha;
      yield return null;
    }
    menu.RemoveSelf();
    menu = null;
    Visible = false;
  }

  private void Start(Trial trial)
  {
    Audio.Play("event:/ui/main/button_select");
    Add(new Coroutine(StartRoutine(trial)));
  }

  private void WatchPb(Trial trial)
  {
    Replay replay = ReplayStore.Load(trial.Id);
    if (replay == null)
    {
      Audio.Play("event:/ui/main/button_invalid");
      return;
    }
    Audio.Play("event:/ui/main/button_select");
    Add(new Coroutine(StartRoutine(trial, replay)));
  }

  private void DeleteAll()
  {
    TrialRecords.Clear();
    ReplayStore.DeleteAll();
    OnSelect(menu.Selection);
  }

  private IEnumerator StartRoutine(Trial trial, Replay replay = null)
  {
    menu.Focused = false;
    Focused = false;
    AreaData area = AreaData.Get(trial.Run.Sid);
    area.Wipe(Overworld, false, null);
    Audio.SetMusic(null, true, true);
    Audio.SetAmbience(null, true);
    yield return 0.5f;
    if (replay != null)
      RunManager.StartReplay(replay);
    else
      RunManager.StartTrial(trial);
  }

  public override void Render()
  {
    if (alpha <= 0f)
      return;
    ActiveFont.DrawOutline(
      Dialog.Clean("climbtrials_trials_title"),
      new Vector2(x, TitleY),
      new Vector2(0.5f, 0.5f),
      Vector2.One * 1.6f,
      Color.White * alpha,
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
    if (selectedHasReplay)
      ButtonRow.DrawCenter(
        Dialog.Clean("climbtrials_trials_watch_pb"),
        Input.MenuJournal,
        alpha,
        x
      );
    if (ModCheck.OthersLoaded)
      ActiveFont.DrawOutline(
        ModCheck.MenuWarning(),
        new Vector2(x, StatusY),
        new Vector2(0.5f, 0.5f),
        Vector2.One * 0.45f,
        Color.White * 0.7f * alpha,
        2f,
        Color.Black * 0.7f * alpha
      );
  }
}
