using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public static class ButtonRow
{
  public const float Y = 1024f;
  public const float RightEdge = 1880f;
  public const float CenterX = 960f;
  public const float Scale = 0.5f;
  private const float Spacing = 32f;

  public static (string Label, VirtualButton Button) MenuPrompt =>
    (Dialog.Clean("climbtrials_open_menu"), Input.GuiInputController() ? Input.Pause : Input.ESC);

  public static float Width(string label, VirtualButton button) =>
    Scale * ButtonUI.Width(label, button);

  public static void DrawCenter(string label, VirtualButton button, float alpha, float x = CenterX)
  {
    ButtonUI.Render(new Vector2(x, Y), label, button, Scale, 0.5f, 0f, alpha);
  }

  public static void DrawRight(
    float alpha,
    IEnumerable<(string Label, VirtualButton Button)> prompts
  )
  {
    Vector2 position = new(RightEdge, Y);
    foreach ((string label, VirtualButton button) in prompts)
    {
      ButtonUI.Render(position, label, button, Scale, 1f, 0f, alpha);
      position.X -= Width(label, button) + Spacing;
    }
  }
}
