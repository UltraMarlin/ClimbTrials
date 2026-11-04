using Microsoft.Xna.Framework;
using MonoMod.Utils;

namespace Celeste.Mod.ClimbTrials.UI;

public static class RowDescription
{
  private const float BelowRowCentre = 29f;

  public static void Attach(TextMenu menu, TextMenu.Item row, string text)
  {
    row.AddDescription(menu, text);
    TextMenuExt.EaseInSubHeaderExt description = (TextMenuExt.EaseInSubHeaderExt)
      menu.Items[menu.Items.IndexOf(row) + 1];
    description.IncludeWidthInMeasurement = false;
    description.Offset = new Vector2(0f, BelowRowCentre - row.Height() / 2f);
  }

  public static void OpenAtOnce(TextMenu.Item item)
  {
    if (item is not TextMenuExt.EaseInSubHeaderExt description || !description.FadeVisible)
      return;
    DynamicData.For(description).Set("uneasedAlpha", 1f);
    description.Alpha = 1f;
  }
}
