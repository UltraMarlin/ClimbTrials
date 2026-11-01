using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.Runs;
using Celeste.Mod.ClimbTrials.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Entities;

[Tracked]
public class ReplayInputDisplay : Entity
{
  private const float Square = 52f;
  private const float Gap = 8f;
  private const float PlusSquare = 36f;
  private const float PlusGap = 4f;
  private const float PlusSize = 3f * PlusSquare + 2f * PlusGap;
  private const float PaddingX = 20f;
  private const float PaddingY = 14f;
  private const float PlusLeft = PaddingX;
  private const float RowsLeft = PlusLeft + PlusSize + 2f * Gap;

  private const float ScreenBottom = 1080f;
  private const float BottomRowTop = ScreenBottom - PaddingY - Square;
  private const float TopRowTop = BottomRowTop - Square - Gap;
  private const float PlusTop = BottomRowTop + Square - PlusSize;
  private const float BoxAlpha = 0.5f;
  private const int LayerWidth = 900;
  private const float IdleAlpha = 0.35f;
  private const float LabelScale = 0.45f;
  private const float LabelPadding = 8f;
  private const float ArrowFill = 0.7f;
  private const float BarHeight = 5f;
  private const float BarGap = 3f;

  private const float StickRadius = Square * 0.55f;
  private const float DotSize = 30f;
  private const float DotInset = DotSize / 6f;
  private const float DirectionTolerance = 0.02f;

  private readonly record struct Button(
    string Key,
    Actions Flags,
    Func<InputFrame, bool> HiddenWhen,
    bool WhilePaused = false
  );

  private static readonly Button[] NormalButtons =
  [
    new("climbtrials_input_jump", Actions.Jump, null),
    new("climbtrials_input_dash", Actions.Dash, null),
    new("climbtrials_input_grab", Actions.Grab, null),
  ];
  private static readonly Button CrouchDash = new(
    "climbtrials_input_demo",
    Actions.CrouchDash,
    null
  );
  private static readonly Button[] DetailedButtons =
  [
    new("climbtrials_input_talk", Actions.Talk, Holding(Actions.Dash)),
    new("climbtrials_pause", Actions.Pause | Actions.Esc, null),
    new("climbtrials_input_restart", Actions.QuickRestart, null),
    new("climbtrials_input_menu_up", Actions.MenuUp, null, WhilePaused: true),
    new("climbtrials_input_menu_down", Actions.MenuDown, null, WhilePaused: true),
    new("climbtrials_input_confirm", Actions.MenuConfirm, Holding(Actions.Jump)),
    new("climbtrials_input_cancel", Actions.MenuCancel, Holding(Actions.Dash)),
    new("climbtrials_input_journal", Actions.MenuJournal, Holding(Actions.Grab)),
  ];

  private static readonly Vector2[] Directions =
  [
    new(-1f, 0f),
    new(1f, 0f),
    new(0f, -1f),
    new(0f, 1f),
  ];

  private static Replay scannedReplay;
  private static List<Button> scannedNormalRow;
  private static List<Button> scannedDetailedRow;
  private static bool scannedFeatherShown;
  private static bool scannedDashOnlyUpUsed;

  private readonly List<Button> normalRow;
  private readonly List<Button> detailedRow;
  private readonly bool featherShown;
  private readonly bool dashOnlyUpUsed;
  private readonly Color boxColor = Colors.ForChapter(RunManager.Run.Area).Dark;
  private readonly FadeBuffer idleLayer;
  private InputFrame? replayed;
  private InputFrame? previous;

  public ReplayInputDisplay(Replay replay)
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate | Tags.FrozenUpdate;
    if (replay != scannedReplay)
      ScanReplay(replay);
    normalRow = scannedNormalRow;
    detailedRow = scannedDetailedRow;
    featherShown = scannedFeatherShown;
    dashOnlyUpUsed = scannedDashOnlyUpUsed;
    float layerTop = TopOfBlock(TopRowTop) - PaddingY;
    int layerHeight = (int)(ScreenBottom - layerTop);
    idleLayer = new FadeBuffer("climbtrials-input-display", LayerWidth, layerHeight, DrawIdle)
    {
      Origin = new Vector2(0f, layerTop),
      Alpha = IdleAlpha,
    };
    Add(idleLayer);
  }

  private static void ScanReplay(Replay replay)
  {
    List<InputFrame> frames = replay.Frames;
    scannedNormalRow = [.. NormalButtons];
    if (HeldOnSomeFrame(frames, CrouchDash))
      scannedNormalRow.Add(CrouchDash);
    scannedDetailedRow = [];
    foreach (Button button in DetailedButtons)
      if (HeldOnSomeFrame(frames, button))
        scannedDetailedRow.Add(button);
    scannedFeatherShown = frames.Exists(frame => PointsOffDigital(frame.Feather, frame));
    scannedDashOnlyUpUsed = frames.Exists(frame => (frame.Binds & DirectionBinds.DashOnlyUp) != 0);
    scannedReplay = replay;
  }

  private static Func<InputFrame, bool> Holding(Actions flags) =>
    frame => (frame.Held & flags) != 0;

  private static bool HeldOnSomeFrame(List<InputFrame> frames, Button button) =>
    frames.Exists(frame => IsHeldIn(frame, button) && !(button.HiddenWhen?.Invoke(frame) ?? false));

  private static bool IsHeldIn(InputFrame frame, Button button) =>
    (frame.Held & button.Flags) != 0 && (!button.WhilePaused || frame.Paused);

  private static bool PointsOffDigital(Vector2 value, InputFrame frame)
  {
    if (value == Vector2.Zero)
      return false;
    Vector2 digital = new(frame.MoveX, frame.MoveY);
    if (digital == Vector2.Zero)
      return true;
    return Vector2.Distance(value.SafeNormalize(), digital.SafeNormalize()) > DirectionTolerance;
  }

  private static InputDisplayMode Mode => ClimbTrialsModule.Settings.HudSettings.ReplayInputDisplay;

  private bool DetailedRowShown => Mode == InputDisplayMode.Detailed && detailedRow.Count > 0;

  private float TopOfBlock(float topRowTop) =>
    Math.Min(PlusTop - (dashOnlyUpUsed ? BarGap + BarHeight : 0f), topRowTop - BarGap - BarHeight);

  public override void Update()
  {
    base.Update();
    if (!RunManager.Replaying)
    {
      RemoveSelf();
      return;
    }
    idleLayer.Visible = Mode != InputDisplayMode.Off;
  }

  public override void Render()
  {
    if (Mode == InputDisplayMode.Off || RunManager.Replay == null || Replayer.MenuOpen)
      return;

    replayed = Replayer.ReplayedFrame();
    previous = Replayer.ReplayedFrame(1);
    float boxTop = TopOfBlock(DetailedRowShown ? TopRowTop : BottomRowTop) - PaddingY;
    Draw.Rect(0f, boxTop, RightEdge() + PaddingX, ScreenBottom - boxTop, boxColor * BoxAlpha);
    base.Render();
    DrawLayout(idle: false);
  }

  private float RightEdge()
  {
    float normal = RowRightEdge(normalRow, featherShown);
    float detailed = DetailedRowShown ? RowRightEdge(detailedRow, false) : 0f;
    return Math.Max(PlusLeft + PlusSize, Math.Max(normal, detailed));
  }

  private static float RowRightEdge(List<Button> row, bool stick) =>
    RowsLeft + (row.Count + (stick ? 1 : 0)) * (Square + Gap) - Gap;

  private void DrawIdle() => DrawLayout(idle: true);

  private void DrawLayout(bool idle)
  {
    bool detailed = DetailedRowShown;
    float normalTop = detailed ? TopRowTop : BottomRowTop;
    for (int i = 0; i < Directions.Length; i++)
      DrawArrowSquare(i, idle);
    float left = DrawRow(normalRow, normalTop, idle);
    if (featherShown)
      DrawFeatherRing(RingCenter(left, normalTop), idle);
    if (detailed)
      DrawRow(detailedRow, BottomRowTop, idle);
  }

  private static Vector2 RingCenter(float left, float top) =>
    new(left + Square / 2f, top + Square / 2f);

  private float DrawRow(List<Button> buttons, float top, bool idle)
  {
    float left = RowsLeft;
    foreach (Button button in buttons)
      left = DrawButton(button, left, top, idle);
    return left;
  }

  private float DrawButton(Button button, float left, float top, bool idle)
  {
    string label = Dialog.Clean(button.Key);
    Vector2 center = new(left + Square / 2f, top + Square / 2f);
    bool held = replayed.HasValue && IsHeldIn(replayed.Value, button);
    if (idle)
    {
      Draw.HollowRect(left, top, Square, Square, Color.White);
      DrawLabel(label, center);
    }
    else if (held)
    {
      Draw.Rect(left, top, Square, Square, Color.White);
      DrawLabel(label, center);
      if (PressedWhileHeld(button.Flags))
        Draw.Rect(left, top - BarGap - BarHeight, Square, BarHeight, Color.White);
    }
    return left + Square + Gap;
  }

  private bool PressedWhileHeld(Actions flags)
  {
    if (!replayed.HasValue || !previous.HasValue)
      return false;
    return (replayed.Value.Pressed & flags) != 0 && (previous.Value.Held & flags) != 0;
  }

  private static void DrawLabel(string label, Vector2 center)
  {
    ActiveFont.DrawOutline(
      label,
      center,
      new Vector2(0.5f, 0.5f),
      Vector2.One * FitScale(label),
      Color.White,
      1.2f,
      Color.Black
    );
  }

  private static float FitScale(string label)
  {
    Vector2 size = ActiveFont.Measure(label) * LabelScale;
    float room = Square - LabelPadding;
    float fit = Math.Min(room / size.X, room / size.Y);
    return fit >= 1f ? LabelScale : LabelScale * fit;
  }

  private void DrawArrowSquare(int directionIndex, bool idle)
  {
    Vector2 direction = Directions[directionIndex];
    Vector2 topLeft = new(
      PlusLeft + (direction.X + 1f) * (PlusSquare + PlusGap),
      PlusTop + (direction.Y + 1f) * (PlusSquare + PlusGap)
    );
    if (idle)
    {
      Draw.HollowRect(topLeft.X, topLeft.Y, PlusSquare, PlusSquare, Color.White);
      DrawArrowGlyph(direction, topLeft, Color.White);
      return;
    }

    if (!replayed.HasValue)
      return;

    InputFrame frame = replayed.Value;
    DirectionBinds moveOnly = (DirectionBinds)(1 << directionIndex);
    DirectionBinds dashOnly = (DirectionBinds)(1 << (directionIndex + 4));
    if ((frame.Binds & moveOnly) != 0)
      Draw.Rect(topLeft.X, topLeft.Y + PlusSquare + BarGap, PlusSquare, BarHeight, Color.White);
    if ((frame.Binds & dashOnly) != 0)
      Draw.Rect(topLeft.X, topLeft.Y - BarGap - BarHeight, PlusSquare, BarHeight, Color.White);
    if (!IsMoving(frame, direction))
      return;

    Draw.Rect(topLeft.X, topLeft.Y, PlusSquare, PlusSquare, Color.White);
    DrawArrowGlyph(direction, topLeft, Color.Black);
  }

  private static void DrawArrowGlyph(Vector2 direction, Vector2 topLeft, Color color)
  {
    MTexture arrow = Input.GuiDirection(direction);
    if (arrow == null)
      return;

    float scale = ArrowFill * PlusSquare / Math.Max(arrow.Width, arrow.Height);
    arrow.DrawCentered(topLeft + new Vector2(PlusSquare / 2f), color, scale);
  }

  private static bool IsMoving(InputFrame frame, Vector2 direction)
  {
    if (direction.X != 0f)
      return frame.MoveX == (int)direction.X;
    return frame.MoveY == (int)direction.Y;
  }

  private void DrawFeatherRing(Vector2 center, bool idle)
  {
    if (idle)
    {
      Draw.Circle(center, StickRadius, Color.White, 2f, 24);
      DrawLabel(Dialog.Clean("climbtrials_input_fly"), center);
      return;
    }

    if (!replayed.HasValue)
      return;

    Vector2 stick = replayed.Value.Feather;
    if (stick == Vector2.Zero)
      return;

    Draw.Circle(center, StickRadius, Color.White, 2f, 24);
    MTexture dot = GFX.Gui["dot"];
    Vector2 position = center + stick.SafeNormalize() * (StickRadius - DotInset);
    dot.DrawCentered(position, Color.White, DotSize / dot.Width);
  }
}
