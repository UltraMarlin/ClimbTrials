using System;
using Celeste.Mod.ClimbTrials.Replays;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public sealed class FadeBuffer : Component
{
  private readonly string name;
  private readonly int width;
  private readonly int height;
  private readonly Action draw;
  private VirtualRenderTarget target;
  private VirtualRenderTarget mask;
  private BeforeRenderHook hook;

  private static readonly BlendState Flatten = new()
  {
    ColorSourceBlend = Blend.DestinationAlpha,
    AlphaSourceBlend = Blend.DestinationAlpha,
  };
  private static readonly Vector2[] OutlineOffsets =
  [
    new(-1f, 0f),
    new(1f, 0f),
    new(0f, -1f),
    new(0f, 1f),
  ];

  public Vector2 Origin;
  public float Alpha = 1f;
  public Color? SilhouetteFill;
  public Color SilhouetteOutline;

  public FadeBuffer(string name, int width, int height, Action draw)
    : base(active: false, visible: true)
  {
    this.name = name;
    this.width = width;
    this.height = height;
    this.draw = draw;
  }

  public override void Added(Entity entity)
  {
    base.Added(entity);
    entity.Add(hook = new BeforeRenderHook(Capture));
  }

  public override void Removed(Entity entity)
  {
    hook?.RemoveSelf();
    hook = null;
    Dispose();
    base.Removed(entity);
  }

  public override void EntityRemoved(Scene scene)
  {
    base.EntityRemoved(scene);
    Dispose();
  }

  public override void SceneEnd(Scene scene)
  {
    base.SceneEnd(scene);
    Dispose();
  }

  private void Dispose()
  {
    target?.Dispose();
    target = null;
    mask?.Dispose();
    mask = null;
  }

  public void Capture()
  {
    if (!Visible || !Entity.Visible)
      return;
    target ??= VirtualContent.CreateRenderTarget(name, width, height);
    Vector2 origin = Origin.Floor();
    SamplerState sampler = Entity.TagCheck(Tags.HUD)
      ? SamplerState.LinearClamp
      : SamplerState.PointClamp;
    if (SilhouetteFill is not Color fill)
    {
      DrawContent(target, origin, sampler);
      return;
    }
    mask ??= VirtualContent.CreateRenderTarget(name + "-mask", width, height);
    DrawContent(mask, origin, sampler);
    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, Flatten);
    Draw.Rect(0f, 0f, width, height, Color.White);
    Draw.SpriteBatch.End();

    Engine.Graphics.GraphicsDevice.SetRenderTarget(target);
    Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
    Draw.SpriteBatch.Begin(
      SpriteSortMode.Deferred,
      BlendState.AlphaBlend,
      sampler,
      DepthStencilState.None,
      RasterizerState.CullNone
    );
    foreach (Vector2 offset in OutlineOffsets)
      Draw.SpriteBatch.Draw((RenderTarget2D)mask, offset, SilhouetteOutline);
    Draw.SpriteBatch.Draw((RenderTarget2D)mask, Vector2.Zero, fill);
    Draw.SpriteBatch.End();
  }

  private void DrawContent(VirtualRenderTarget into, Vector2 origin, SamplerState sampler)
  {
    Engine.Graphics.GraphicsDevice.SetRenderTarget(into);
    Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
    Draw.SpriteBatch.Begin(
      SpriteSortMode.Deferred,
      BlendState.AlphaBlend,
      sampler,
      DepthStencilState.None,
      RasterizerState.CullNone,
      null,
      Matrix.CreateTranslation(-origin.X, -origin.Y, 0f)
    );
    Replayer.UseWatcherLanguage();
    draw();
    Replayer.UseReplayLanguage();
    Draw.SpriteBatch.End();
  }

  public override void Render()
  {
    if (target == null)
      return;
    Draw.SpriteBatch.Draw((RenderTarget2D)target, Origin.Floor(), Color.White * Alpha);
  }
}
