using Celeste.Mod.ClimbTrials.Online;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.UI;

public class NameRow : TextMenu.Item
{
  private const float RowHeight = 60f;

  public NameRow()
  {
    Selectable = true;
  }

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  private static string Text =>
    Identity.Registration switch
    {
      RegistrationState.Registering => Dialog.Clean("climbtrials_connecting"),
      RegistrationState.Registered => Dialog
        .Get("climbtrials_name_row")
        .Replace("{0}", Settings.PlayerName),
      _ when Settings.OfflineMode => Dialog.Clean("climbtrials_offline_mode"),
      _ => Dialog.Clean("climbtrials_not_connected"),
    };

  public override float Height() => RowHeight;

  public override float LeftWidth() => RowLayout.CampaignNameX + ActiveFont.Measure(Text).X;

  public override void Render(Vector2 position, bool highlighted)
  {
    float alpha = Container.Alpha;
    string text = Text;
    bool live =
      !Settings.OfflineMode
      && Identity.Registration is RegistrationState.Registered or RegistrationState.Registering;
    Color color =
      highlighted ? Container.HighlightColor
      : live ? Color.White
      : RowLayout.Dim;
    Vector2 at = position + new Vector2(RowLayout.CampaignNameX, 0f);
    ActiveFont.DrawOutline(
      text,
      at,
      new Vector2(0f, 0.5f),
      Vector2.One,
      color * alpha,
      2f,
      TextOutline.StrokeFor(color * alpha)
    );
  }
}
