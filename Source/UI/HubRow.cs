using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class HubRow(
  Func<string> text,
  Func<bool> live,
  string icon = null,
  bool outlineIcon = true,
  float iconScale = 1f,
  Func<bool> showsNewMark = null
) : SlidingRow
{
  public override void Update()
  {
    Disabled = !live();
    base.Update();
  }

  public override float LeftWidth() => 84f + ActiveFont.Measure(text()).X;

  public override void Render(Vector2 position, bool highlighted)
  {
    float alpha = Container.Alpha;
    bool active = live();
    Vector2 start = RowStart(position);
    if (icon != null)
    {
      MTexture texture = GFX.Gui[icon];
      Color tint = active ? Color.White : Color.DarkSlateGray;
      DrawIcon(
        texture,
        start + new Vector2(32f, 0f),
        tint * alpha,
        iconScale * 64f / texture.Width,
        outlineIcon
      );
    }
    Color color =
      highlighted ? Container.HighlightColor
      : active ? Color.White
      : Color.DarkSlateGray;
    TextOutline.Draw(
      text(),
      start + new Vector2(84f, 0f),
      new Vector2(0f, 0.5f),
      1f,
      color * alpha
    );
    if (showsNewMark != null && showsNewMark())
      NewMark.Draw(start + new Vector2(LeftWidth(), 0f), alpha);
  }

  private static void DrawIcon(
    MTexture texture,
    Vector2 at,
    Color color,
    float scale,
    bool outlined
  )
  {
    Vector2 justify = new(0.5f, 0.5f);
    Color outline = TextOutline.StrokeFor(color);
    if (outlined)
      for (int offsetX = -1; offsetX <= 1; offsetX++)
      for (int offsetY = -1; offsetY <= 1; offsetY++)
        if (offsetX != 0 || offsetY != 0)
          texture.DrawJustified(at + new Vector2(offsetX, offsetY), justify, outline, scale);
    texture.DrawJustified(at, justify, color, scale);
  }
}
