using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Replays;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

public class GhostTags : Entity
{
  private const float OutlineStroke = 1.6f;
  private const float MinRise = 0.5f;
  private const float StackGap = 4f;
  private const float EaseRate = 10f;
  private const float SortBand = 80f;
  private const float HoldMargin = 64f;
  private readonly GhostLayer ghosts;
  private readonly Dictionary<PlaybackGhost, float> shownLifts = [];
  private readonly Dictionary<PlaybackGhost, float> sortHeights = [];
  private readonly Dictionary<PlaybackGhost, TagBounds> tagBounds = [];
  private readonly List<NameTag> tags = [];
  private readonly List<NameTag> placed = [];
  private readonly Dictionary<PlaybackGhost, float> targets = [];
  private readonly HashSet<(PlaybackGhost Below, PlaybackGhost Above)> stacked = [];
  private readonly HashSet<(PlaybackGhost Below, PlaybackGhost Above)> stackedBefore = [];
  private readonly List<PlaybackGhost> gone = [];
  private readonly Comparison<NameTag> bottomFirst;
  private Language measuredIn;

  private readonly record struct NameTag(
    PlaybackGhost Ghost,
    Vector2 Anchor,
    float Width,
    float VisibleTop,
    float VisibleBottom,
    int Order
  );

  public GhostTags(GhostLayer ghosts)
  {
    this.ghosts = ghosts;
    Tag = Tags.HUD | Tags.Persistent;
    bottomFirst = (a, b) =>
      sortHeights[a.Ghost] != sortHeights[b.Ghost]
        ? sortHeights[b.Ghost].CompareTo(sortHeights[a.Ghost])
        : a.Order.CompareTo(b.Order);
  }

  public override void Render()
  {
    if (Scene is not Level level)
      return;
    Replayer.UseWatcherLanguage();
    Vector2 scale = Vector2.One * PlaybackGhost.TagScale;
    if (measuredIn != Dialog.Language)
      tagBounds.Clear();
    measuredIn = Dialog.Language;
    tags.Clear();
    for (int i = 0; i < ghosts.Ghosts.Count; i++)
    {
      PlaybackGhost ghost = ghosts.Ghosts[i];
      if (!ghost.Visible || string.IsNullOrEmpty(ghost.Name))
        continue;
      Vector2 anchor = level.WorldToScreen(ghost.Position - Vector2.UnitY * PlaybackGhost.TagLift);
      if (!tagBounds.TryGetValue(ghost, out TagBounds bounds))
        tagBounds[ghost] = bounds = Measure(ghost.Name, scale.X);
      tags.Add(new NameTag(ghost, anchor, bounds.Width, bounds.Top, bounds.Bottom, i));
    }
    Stack();
    float step =
      Replayer.State == ReplayState.Paused ? 1f : 1f - MathF.Exp(-EaseRate * Engine.RawDeltaTime);
    foreach (NameTag tag in tags)
    {
      float target = targets[tag.Ghost];
      float lift = shownLifts.TryGetValue(tag.Ghost, out float shown)
        ? MathHelper.Lerp(shown, target, step)
        : target;
      shownLifts[tag.Ghost] = lift;
      Draw(tag.Ghost, tag.Anchor - Vector2.UnitY * lift, scale);
    }
    Forget(shownLifts);
    Forget(sortHeights);
    Forget(tagBounds);
    Replayer.UseReplayLanguage();
  }

  private readonly record struct TagBounds(float Width, float Top, float Bottom);

  private static TagBounds Measure(string name, float scale)
  {
    PixelFontSize font = ActiveFont.FontSize;
    float top = font.LineHeight;
    float bottom = 0f;
    foreach (char character in name)
    {
      if (!font.Characters.TryGetValue(character, out PixelFontCharacter glyph))
        continue;
      top = MathF.Min(top, glyph.YOffset);
      bottom = MathF.Max(bottom, glyph.YOffset + glyph.Texture.Height);
    }
    if (bottom <= top)
    {
      top = 0f;
      bottom = font.LineHeight;
    }
    float width = ActiveFont.Measure(name).X * scale + OutlineStroke * 2f;
    float visibleTop = (font.LineHeight - top) * scale + OutlineStroke;
    float visibleBottom = (font.LineHeight - bottom) * scale - OutlineStroke;
    return new TagBounds(width, visibleTop, visibleBottom);
  }

  private void Stack()
  {
    foreach (NameTag tag in tags)
      if (
        !sortHeights.TryGetValue(tag.Ghost, out float height)
        || MathF.Abs(tag.Anchor.Y - height) > SortBand
      )
        sortHeights[tag.Ghost] = tag.Anchor.Y;
    tags.Sort(bottomFirst);
    stackedBefore.Clear();
    stackedBefore.UnionWith(stacked);
    stacked.Clear();
    placed.Clear();
    targets.Clear();
    foreach (NameTag tag in tags)
    {
      float lift = 0f;
      bool moved = true;
      while (moved)
      {
        moved = false;
        foreach (NameTag other in placed)
        {
          float margin = stackedBefore.Contains((other.Ghost, tag.Ghost)) ? HoldMargin : 0f;
          float otherTop = other.Anchor.Y - targets[other.Ghost] - other.VisibleTop;
          float otherBottom = other.Anchor.Y - targets[other.Ghost] - other.VisibleBottom;
          float top = tag.Anchor.Y - lift - tag.VisibleTop;
          float bottom = tag.Anchor.Y - lift - tag.VisibleBottom;
          bool sideBySide =
            MathF.Abs(tag.Anchor.X - other.Anchor.X) * 2f < tag.Width + other.Width + margin * 2f;
          bool level = bottom > otherTop - margin && top < otherBottom + margin;
          if (!sideBySide || !level)
            continue;
          stacked.Add((other.Ghost, tag.Ghost));
          float above = tag.Anchor.Y - tag.VisibleBottom - otherTop + StackGap;
          if (above <= lift + MinRise)
            continue;
          lift = above;
          moved = true;
        }
      }
      targets[tag.Ghost] = lift;
      placed.Add(tag);
    }
  }

  private void Forget<T>(Dictionary<PlaybackGhost, T> perGhost)
  {
    gone.Clear();
    foreach (PlaybackGhost ghost in perGhost.Keys)
      if (!targets.ContainsKey(ghost))
        gone.Add(ghost);
    foreach (PlaybackGhost ghost in gone)
      perGhost.Remove(ghost);
  }

  private static void Draw(PlaybackGhost ghost, Vector2 at, Vector2 scale)
  {
    Vector2 justify = new(0.5f, 1f);
    Color color = ghost.TagColor * ghost.TagAlpha;
    if (ghost.TagOutline is Color outline)
    {
      Color stroke = outline * ghost.TagAlpha;
      ActiveFont.DrawOutline(ghost.Name, at, justify, scale, color, OutlineStroke, stroke);
    }
    else
      ActiveFont.Draw(ghost.Name, at, justify, scale, color);
  }
}
