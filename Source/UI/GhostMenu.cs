using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.UI;

public readonly record struct GhostOption(string Label, Func<string> Reason, Action Chosen);

public class GhostMenu : MenuOnTop
{
  private const float HeaderScale = 1f;
  private const float HeaderHeight = 90f;
  private const float LabelGap = 24f;

  private readonly MenuKeys keys;
  private readonly Action closed;
  private bool discarded;

  public GhostMenu(
    BoardRow row,
    BoardSubject subject,
    BoardMode mode,
    IEnumerable<GhostOption> options,
    MenuKeys keys,
    bool inLevel,
    Action closed
  )
    : base(inLevel)
  {
    this.keys = keys;
    this.closed = closed;
    Position.Y -= MenuLift;
    if (keys.Raw)
    {
      Focused = false;
      RenderAsFocused = true;
    }

    Add(new HeaderItem(row, subject, mode));
    Add(new Gap(OptionsGap));
    foreach (GhostOption option in options)
      Add(new OptionButton(option));

    OnCancel = Back;
    OnESC = Back;
    OnPause = Back;
  }

  public void Discard()
  {
    discarded = true;
    Focused = false;
    Visible = false;
    RemoveSelf();
  }

  public override void Added(Scene scene)
  {
    base.Added(scene);
    if (discarded)
      RemoveSelf();
  }

  private void Back()
  {
    Audio.Play(SFX.ui_main_button_back);
    Dismiss();
  }

  public override void Update()
  {
    if (keys.Raw && !Closing && !discarded)
      ReadKeys(keys, Back);
    base.Update();
    if (!Closed)
      return;
    RemoveSelf();
    closed?.Invoke();
  }

  public override void Render()
  {
    base.Render();
    if (Current is OptionButton button && button.Reason() is string text)
      DrawReason(text);
  }

  private class OptionButton(GhostOption option)
    : DimButton(option.Label, () => option.Reason() == null, option.Chosen)
  {
    public Func<string> Reason => option.Reason;
  }

  private class HeaderItem(BoardRow row, BoardSubject subject, BoardMode mode) : Item
  {
    private readonly string label = row.Rank is int rank
      ? $"{RankText.Format(rank)} {row.Name}"
      : row.Name;
    private readonly string time = TimeText.Format(row.Ticks.GetValueOrDefault());
    private static TimeStyle Style => TimeText.Hud.At(HeaderScale);
    private float LabelWidth => ActiveFont.Measure(label).X * HeaderScale;

    public override float Height() => HeaderHeight;

    public override float LeftWidth() => LabelWidth + LabelGap + TimeText.Width(time, Style);

    public override void Render(Vector2 position, bool highlighted)
    {
      float alpha = Container.Alpha;
      float left = position.X + (Container.Width - LeftWidth()) / 2f;
      Color color = Color.White * alpha;
      ActiveFont.DrawOutline(
        label,
        new Vector2(left, position.Y),
        new Vector2(0f, 0.5f),
        Vector2.One * HeaderScale,
        color,
        2f,
        TextOutline.StrokeFor(color)
      );
      Vector2 baseline = new(
        left + LabelWidth + LabelGap,
        position.Y + TimeText.Height(Style) / 2f
      );
      Color timeColor = MedalIcon.TextColor(subject.ShownMedal(row, mode)) * alpha;
      TimeText.Draw(baseline, time, Style, timeColor);
      Vector2 watchStatusAt = new(
        position.X + Container.Width / 2f,
        position.Y + HeaderHeight / 2f
      );
      BoardMenu.DrawWatchStatus(row, watchStatusAt, new Vector2(0.5f, 0.5f), alpha);
    }
  }
}
