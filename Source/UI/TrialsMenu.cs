using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class TrialsMenu : TextMenu
{
  public const float ListTop = 240f;
  public const float ListBottom = 960f;
  public static readonly Color Highlight = Calc.HexToColor("6ded87");

  public TrialsMenu()
  {
    AutoScroll = false;
    InnerContent = InnerContentMode.TwoColumn;
    Justify = new Vector2(0.5f, 0f);
    HighlightColor = Highlight;
  }

  private float TargetY
  {
    get
    {
      float viewport = ListBottom - ListTop;
      if (Height <= viewport)
        return ListTop;
      float centered = ListTop + viewport / 2f - GetYOffsetOf(Current);
      return Calc.Clamp(centered, ListBottom - Height, ListTop);
    }
  }

  public override void Added(Scene scene)
  {
    base.Added(scene);
    Position.Y = TargetY;
  }

  public override void Update()
  {
    base.Update();
    HighlightColor = Highlight;
    float lerp = 1f - (float)Math.Pow(0.01, Engine.RawDeltaTime);
    Position.Y += (TargetY - Position.Y) * lerp;
  }

  public override void Render()
  {
    RecalculateSize();
    Vector2 cursor = Position - Justify * new Vector2(Width, Height);
    float menuAlpha = Alpha;
    foreach (Item item in Items)
    {
      if (!item.Visible)
        continue;
      float height = item.Height();
      float bounce = item.SelectWiggler.Value * 8f;
      Vector2 center = cursor + new Vector2(0f, height * 0.5f + bounce);
      float fade = EdgeFade(center.Y, height);
      if (fade > 0f)
      {
        Alpha = menuAlpha * fade;
        item.Render(center, Focused && Current == item);
      }
      cursor.Y += height + ItemSpacing;
    }
    Alpha = menuAlpha;
  }

  private static float EdgeFade(float y, float height)
  {
    float inside = Math.Min(y - ListTop, ListBottom - y);
    return Calc.Clamp(inside / (height * 0.5f), 0f, 1f);
  }
}
