using System.Collections;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public class LoadingIcon : Entity
{
  private static readonly Vector2 ScreenPosition = new(1760f, 920f);

  private readonly Sprite icon;
  private readonly Wiggler wiggler;
  private bool display = true;
  private bool ending;

  public bool Gone { get; private set; }

  public LoadingIcon()
  {
    Tag = Tags.HUD | Tags.FrozenUpdate | Tags.PauseUpdate | Tags.Global;
    Depth = -1000000;
    Position = ScreenPosition;
    Add(icon = GFX.GuiSpriteBank.Create("save"));
    icon.UseRawDeltaTime = true;
    Add(wiggler = Wiggler.Create(0.4f, 4f, f => icon.Rotation = f * 0.1f));
    wiggler.UseRawDeltaTime = true;
    Add(new Coroutine(Animate()) { UseRawDeltaTime = true });
    icon.Visible = false;
  }

  public bool TryToKeep()
  {
    if (ending || Gone)
      return false;
    display = true;
    return true;
  }

  public void Hide() => display = false;

  public override void Removed(Scene scene)
  {
    Gone = true;
    base.Removed(scene);
  }

  public override void SceneEnd(Scene scene)
  {
    Gone = true;
    base.SceneEnd(scene);
  }

  private IEnumerator Animate()
  {
    icon.Play("start", restart: true);
    icon.Visible = true;
    yield return 0.25f;
    float timer = 1f;
    while (display)
    {
      timer -= Engine.DeltaTime;
      if (timer <= 0f)
      {
        wiggler.Start();
        timer = 1f;
      }
      yield return null;
    }
    ending = true;
    icon.Play("end");
    yield return 0.5f;
    icon.Visible = false;
    yield return null;
    RemoveSelf();
  }
}
