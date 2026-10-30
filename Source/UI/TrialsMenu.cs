using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class TrialsMenu : TextMenu
{
  public const float ListMinWidth = 1200f;

  public static readonly Color Highlight = HighlightColorA;

  public float ListTop = 240f;
  public float ListBottom = 960f;

  public Action OnSwitchLeft;
  public Action OnSwitchRight;

  public TrialsMenu()
  {
    AutoScroll = false;
    InnerContent = InnerContentMode.TwoColumn;
    Justify = new Vector2(0.5f, 0f);
    HighlightColor = Highlight;
    MinWidth = ListMinWidth;
  }

  private float TargetY
  {
    get
    {
      float viewport = ListBottom - ListTop;
      float height = SettledHeight();
      if (height <= viewport)
        return ListTop;
      float centered = ListTop + viewport / 2f - SettledOffsetOf(Current);
      return Calc.Clamp(centered, ListBottom - height, ListTop);
    }
  }

  private float SettledHeight()
  {
    float height = 0f;
    foreach (Item item in Items)
      height += SettledSlot(item);
    return height;
  }

  private float SettledOffsetOf(Item target)
  {
    if (target == null)
      return 0f;
    float offset = 0f;
    foreach (Item item in Items)
    {
      offset += SettledSlot(item);
      if (item == target)
        break;
    }
    return offset - target.Height() * 0.5f - ItemSpacing;
  }

  private float SettledSlot(Item item)
  {
    if (item is TextMenuExt.EaseInSubHeaderExt description)
    {
      if (!description.FadeVisible)
        return 0f;
      float text =
        description.Title.Length > 0 ? ActiveFont.HeightOf(description.Title) * 0.6f : 0f;
      return text + description.HeightExtra + ItemSpacing;
    }
    return item.Visible ? item.Height() + ItemSpacing : 0f;
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

  private float EdgeFade(float rowY, float height)
  {
    float inside = Math.Min(rowY - ListTop, ListBottom - rowY);
    return Calc.Clamp(inside / (height * 0.5f), 0f, 1f);
  }
}
