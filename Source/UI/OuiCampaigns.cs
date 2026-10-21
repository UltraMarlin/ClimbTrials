using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class OuiCampaigns : ListScreen
{
  private readonly List<CampaignRow> rows = [];
  private LeaderboardView view;

  protected override int ScreenDepth => 2;

  protected override TrialsMenu BuildMenu()
  {
    TrialsMenu built = [];
    rows.Clear();
    foreach (Campaign campaign in Campaigns.All)
    {
      CampaignRow row = new(campaign);
      row.Pressed(() => Open(campaign));
      built.Add(row);
      if (!string.IsNullOrEmpty(campaign.Description))
        RowDescription.Attach(built, row, campaign.Description);
      rows.Add(row);
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

  protected override void RenderHeader()
  {
    if (menu != null)
      DrawOfflineNote(Standings.HubState);
  }

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
    if (
      view == null
      && menu != null
      && menu.Focused
      && Input.MenuJournal.Pressed
      && menu.Current is CampaignRow row
    )
    {
      Audio.Play(SFX.ui_main_button_select);
      OpenBoard(row.Campaign);
    }
  }

  private void OpenBoard(Campaign campaign)
  {
    menu.Focused = false;
    Focused = false;
    view = new LeaderboardView(
      BoardSubject.Of(campaign),
      opened => SetDim(Motion.DimUnder(opened, Motion.BoardUnderDim)),
      OnBoardClosed
    );
    Scene.Add(view);
  }

  private void OnBoardClosed(BoardSubject shown)
  {
    view = null;
    int index = rows.FindIndex(r => r.Campaign.Id == shown.Id);
    if (index >= 0 && menu.Current != rows[index])
    {
      menu.Current?.OnLeave?.Invoke();
      menu.Selection = menu.Items.IndexOf(rows[index]);
      menu.Current?.OnEnter?.Invoke();
    }
    Standings.EnsureFreshHub();
    menu.Focused = true;
    Focused = true;
  }

  protected override int InitialSelection(Oui from)
  {
    OuiTrials.ReturnBoard = null;
    int index = from is OuiTrials trials ? rows.FindIndex(r => r.Campaign == trials.Campaign) : 0;
    return menu.Items.IndexOf(rows[index < 0 ? 0 : index]);
  }

  private void Open(Campaign campaign)
  {
    Audio.Play(SFX.ui_main_button_select);
    menu.Focused = false;
    Overworld.Goto<OuiTrials>().Campaign = campaign;
  }
}
