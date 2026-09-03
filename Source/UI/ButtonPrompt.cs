using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class ButtonPrompt
{
  public enum Align
  {
    Left,
    Center,
    Right,
  }

  public const float RowY = 1000f;
  public const float LeftX = 120f;
  public const float CenterX = 960f;
  public const float RightX = 1800f;

  private const float LabelScale = 0.8f;
  private const float Gap = 16f;

  public static void Draw(
    VirtualButton button,
    string label,
    Vector2 anchor,
    Align align,
    float alpha
  )
  {
    Draw(Input.GuiButton(button, Input.PrefixMode.Latest), label, anchor, align, alpha);
  }

  public static void Draw(Binding binding, string label, Vector2 anchor, Align align, float alpha)
  {
    Draw(IconFor(binding), label, anchor, align, alpha);
  }

  public static MTexture IconFor(Binding binding)
  {
    if (Input.GuiInputController(Input.PrefixMode.Latest))
      return binding.Controller.Count > 0
        ? Input.GuiSingleButton(binding.Controller[0], Input.PrefixMode.Attached)
        : Input.GuiKey(Keys.None);
    return Input.GuiKey(binding.Keyboard.Count > 0 ? binding.Keyboard[0] : Keys.None);
  }

  private static void Draw(MTexture icon, string label, Vector2 anchor, Align align, float alpha)
  {
    Vector2 iconCenter = anchor;
    if (align == Align.Center)
    {
      float labelWidth = ActiveFont.Measure(label).X * LabelScale;
      float total = icon.Width + Gap + labelWidth;
      iconCenter = new Vector2(anchor.X - total / 2f + icon.Width / 2f, anchor.Y);
    }
    float offset = icon.Width / 2f + Gap;
    Vector2 labelPosition = iconCenter + new Vector2(align == Align.Right ? -offset : offset, 0f);
    icon.DrawCentered(iconCenter, Color.White * alpha, Vector2.One);
    ActiveFont.DrawOutline(
      label,
      labelPosition,
      new Vector2(align == Align.Right ? 1f : 0f, 0.5f),
      Vector2.One * LabelScale,
      Color.White * alpha,
      2f,
      Color.Black * alpha
    );
  }
}
