using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;

namespace Celeste.Mod.ClimbTrials.UI;

public abstract class CampaignListScreen : ListScreen
{
  private readonly List<CampaignRow> rows = [];
  private LeaderboardView view;

  protected override int ScreenDepth => 2;
  protected abstract IEnumerable<SlidingRow> ListedRows();

  protected override TrialsMenu BuildMenu()
  {
    TrialsMenu built = [];
    rows.Clear();
    foreach (SlidingRow row in ListedRows())
    {
      built.Add(row);
      if (row is not CampaignRow campaignRow)
      {
        row.Pressed(() => Audio.Play(SFX.ui_main_button_invalid));
        continue;
      }
      Campaign campaign = campaignRow.Campaign;
      campaignRow.Pressed(() => Open(campaign));
      if (campaignRow.ShowsDescription && !string.IsNullOrEmpty(campaign.Description))
        RowDescription.Attach(built, campaignRow, campaign.Description);
      rows.Add(campaignRow);
    }
    built.OnCancel = () =>
    {
      built.Focused = false;
      Audio.Play(SFX.ui_main_button_back);
      Overworld.Goto<OuiHub>();
    };
    return built;
  }

  protected override void FetchOnline()
  {
    Identity.EnsureRegistered();
    Standings.EnsureFreshHub();
  }

  protected override StandingsState ServerState => Standings.HubState;

  protected bool BoardOpen => view != null;

  protected override void RenderPrompts(float alpha)
  {
    if (view != null || menu?.Current is not CampaignRow)
      return;
    ButtonRow.DrawCenter(Dialog.Clean("climbtrials_trials_leaderboard"), Input.MenuJournal, alpha);
  }

  protected override void CancelTransitions()
  {
    if (view == null)
      return;
    view.Discard();
    view = null;
    SetDim(1f);
  }

  public override void Update()
  {
    base.Update();
    if (view != null || menu == null || !menu.Focused || !Input.MenuJournal.Pressed)
      return;
    if (menu.Current is CampaignRow row)
    {
      Audio.Play(SFX.ui_main_button_select);
      OpenBoard(row.Campaign);
    }
    else if (menu.Current != null)
      Audio.Play(SFX.ui_main_button_invalid);
  }

  private void OpenBoard(Campaign campaign)
  {
    menu.Focused = false;
    Focused = false;
    view = new LeaderboardView(
      BoardSubject.ForCampaign(campaign),
      opened => SetDim(Motion.DimUnder(opened, Motion.BoardUnderDim)),
      OnBoardClosed
    )
    {
      OnSubjectChanged = SelectRowOf,
    };
    Scene.Add(view);
  }

  private void OnBoardClosed(BoardSubject shown)
  {
    view = null;
    Standings.EnsureFreshHub();
    menu.Focused = true;
    Focused = true;
  }

  private void SelectRowOf(BoardSubject shown)
  {
    int index = rows.FindIndex(row => row.Campaign.Id == shown.Id);
    if (index >= 0)
      SelectRow(rows[index]);
  }

  protected override int InitialSelection(Oui from)
  {
    OuiTrials.ReturnBoard = null;
    int index = -1;
    if (from is OuiTrials trials)
      index = rows.FindIndex(row => row.Campaign.Id == trials.Campaign.Id);
    return index < 0 ? 0 : menu.Items.IndexOf(rows[index]);
  }

  private void Open(Campaign campaign)
  {
    Audio.Play(SFX.ui_main_button_select);
    menu.Focused = false;
    Overworld.Goto<OuiTrials>().Campaign = campaign;
  }
}
