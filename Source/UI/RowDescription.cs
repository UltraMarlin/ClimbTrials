using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.UI;

public static class RowDescription
{
  public const float Lift = 16f;

  public static void Attach(TextMenu menu, TextMenu.Item row, string text)
  {
    row.AddDescription(menu, text);
    TextMenuExt.EaseInSubHeaderExt description = (TextMenuExt.EaseInSubHeaderExt)
      menu.Items[menu.Items.IndexOf(row) + 1];
    description.IncludeWidthInMeasurement = false;
    description.Offset = new Vector2(0f, -Lift);
  }
}
