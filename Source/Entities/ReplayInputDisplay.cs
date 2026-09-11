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
  private const float StickRadius = Square / 2f - 4f;
  private const float DotSize = 15f;
  private const float DirectionTolerance = 0.02f;

  private readonly record struct Button(
    string Key,
    Actions Flags,
    Func<InputFrame, bool> Shadow,
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
    new("climbtrials_input_talk", Actions.Talk, Holds(Actions.Dash)),
    new("climbtrials_input_pause", Actions.Pause | Actions.Esc, null),
    new("climbtrials_input_restart", Actions.QuickRestart, null),
    new("climbtrials_input_menu_up", Actions.MenuUp, null, WhilePaused: true),
    new("climbtrials_input_menu_down", Actions.MenuDown, null, WhilePaused: true),
    new("climbtrials_input_confirm", Actions.MenuConfirm, Holds(Actions.Jump)),
    new("climbtrials_input_cancel", Actions.MenuCancel, Holds(Actions.Dash)),
    new("climbtrials_input_journal", Actions.MenuJournal, Holds(Actions.Grab)),
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
  private static bool scannedAimShown;

  private readonly List<Button> normalRow;
  private readonly List<Button> detailedRow;
  private readonly bool featherShown;
  private readonly bool aimShown;
  private readonly FadeBuffer idleLayer;
  private InputFrame? fed;
  private InputFrame? previous;

  public ReplayInputDisplay(Replay replay)
  {
    Tag = Tags.HUD | Tags.Persistent | Tags.TransitionUpdate | Tags.FrozenUpdate;
    if (replay != scannedReplay)
      Scan(replay);
    normalRow = scannedNormalRow;
    detailedRow = scannedDetailedRow;
    featherShown = scannedFeatherShown;
    aimShown = scannedAimShown;
    float layerTop = BlockTop(TopRowTop) - PaddingY;
    int layerHeight = (int)(ScreenBottom - layerTop);
    idleLayer = new FadeBuffer("climbtrials-input-display", LayerWidth, layerHeight, DrawIdle)
    {
      Origin = new Vector2(0f, layerTop),
      Alpha = IdleAlpha,
    };
    Add(idleLayer);
  }

  private static void Scan(Replay replay)
  {
    List<InputFrame> frames = replay.Frames;
    scannedNormalRow = [.. NormalButtons];
    if (Used(frames, CrouchDash))
      scannedNormalRow.Add(CrouchDash);
    scannedDetailedRow = [];
    foreach (Button button in DetailedButtons)
      if (button.WhilePaused || Used(frames, button))
        scannedDetailedRow.Add(button);
    scannedFeatherShown = frames.Exists(frame => DiffersFromDigital(frame.Feather, frame));
    scannedAimShown = frames.Exists(frame => frame.Aim != frame.Feather);
    scannedReplay = replay;
  }

  private static Func<InputFrame, bool> Holds(Actions flags) => frame => (frame.Held & flags) != 0;

  private static bool Used(List<InputFrame> frames, Button button) =>
    frames.Exists(frame =>
      (frame.Held & button.Flags) != 0 && !(button.Shadow?.Invoke(frame) ?? false)
    );

  private static bool DiffersFromDigital(Vector2 value, InputFrame frame)
  {
    if (value == Vector2.Zero)
      return false;
    Vector2 digital = new(frame.MoveX, frame.MoveY);
    if (digital == Vector2.Zero)
      return true;
    return Vector2.Distance(value.SafeNormalize(), digital.SafeNormalize()) > DirectionTolerance;
  }

  private static InputDisplayMode Mode => ClimbTrialsModule.Settings.ReplayInputDisplay;

  private bool RecordingPaused => Scene is Level { Paused: true };

  private bool DetailedRowShown =>
    Mode == InputDisplayMode.Detailed && (detailedRow.Count > 0 || aimShown);

  private static float BlockTop(float topRowTop) =>
    Math.Min(PlusTop, topRowTop - BarGap - BarHeight);

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
    fed = Replayer.FedFrame();
    previous = Replayer.FedFrame(1);
    float boxTop = BlockTop(DetailedRowShown ? TopRowTop : BottomRowTop) - PaddingY;
    Draw.Rect(0f, boxTop, RightEdge() + PaddingX, ScreenBottom - boxTop, Color.Black * BoxAlpha);
    base.Render();
    DrawLayout(idle: false);
  }

  private float RightEdge()
  {
    float normal = RowEnd(normalRow, featherShown);
    float detailed = DetailedRowShown ? RowEnd(detailedRow, aimShown) : 0f;
    return Math.Max(PlusLeft + PlusSize, Math.Max(normal, detailed));
  }

  private static float RowEnd(List<Button> row, bool stick) =>
    RowsLeft + (row.Count + (stick ? 1 : 0)) * (Square + Gap) - Gap;

  private void DrawIdle() => DrawLayout(idle: true);

  private void DrawLayout(bool idle)
  {
    bool detailed = DetailedRowShown;
    float normalTop = detailed ? TopRowTop : BottomRowTop;
    foreach (Vector2 direction in Directions)
      DrawDirection(direction, idle);
    float x = DrawRow(normalRow, normalTop, idle);
    if (featherShown)
      DrawStick(StickCenter(x, normalTop), fed?.Feather, idle);
    if (!detailed)
      return;
    x = DrawRow(detailedRow, BottomRowTop, idle);
    if (aimShown)
      DrawStick(StickCenter(x, BottomRowTop), fed?.Aim, idle);
  }

  private static Vector2 StickCenter(float x, float top) => new(x + Square / 2f, top + Square / 2f);

  private float DrawRow(List<Button> buttons, float top, bool idle)
  {
    float x = RowsLeft;
    foreach (Button button in buttons)
      x = DrawButton(button, x, top, idle);
    return x;
  }

  private float DrawButton(Button button, float x, float top, bool idle)
  {
    string label = Dialog.Clean(button.Key);
    bool active = !button.WhilePaused || RecordingPaused;
    bool held = fed.HasValue && (fed.Value.Held & button.Flags) != 0;
    if (idle)
    {
      Draw.HollowRect(x, top, Square, Square, Color.White);
      DrawLabel(label, x, top);
    }
    else if (held && active)
    {
      Draw.Rect(x, top, Square, Square, Color.White);
      DrawLabel(label, x, top);
      if (Repressed(button.Flags))
        Draw.Rect(x, top - BarGap - BarHeight, Square, BarHeight, Color.White);
    }
    return x + Square + Gap;
  }

  private bool Repressed(Actions flags)
  {
    if (!fed.HasValue || !previous.HasValue)
      return false;
    return (fed.Value.Pressed & flags) != 0 && (previous.Value.Held & flags) != 0;
  }

  private static void DrawLabel(string label, float x, float top)
  {
    ActiveFont.DrawOutline(
      label,
      new Vector2(x + Square / 2f, top + Square / 2f),
      new Vector2(0.5f, 0.5f),
      Vector2.One * FitScale(label),
      Color.White,
      2f,
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

  private void DrawDirection(Vector2 direction, bool idle)
  {
    Vector2 topLeft = new(
      PlusLeft + (direction.X + 1f) * (PlusSquare + PlusGap),
      PlusTop + (direction.Y + 1f) * (PlusSquare + PlusGap)
    );
    bool held = fed.HasValue && HoldsDirection(fed.Value, direction);
    if (!idle && !held)
      return;
    if (idle)
      Draw.HollowRect(topLeft.X, topLeft.Y, PlusSquare, PlusSquare, Color.White);
    else
      Draw.Rect(topLeft.X, topLeft.Y, PlusSquare, PlusSquare, Color.White);
    MTexture arrow = Input.GuiDirection(direction);
    if (arrow == null)
      return;
    float scale = ArrowFill * PlusSquare / Math.Max(arrow.Width, arrow.Height);
    Vector2 center = topLeft + new Vector2(PlusSquare / 2f);
    arrow.DrawCentered(center, idle ? Color.White : Color.Black, scale);
  }

  private static bool HoldsDirection(InputFrame frame, Vector2 direction)
  {
    if (direction.X != 0f)
      return frame.MoveX == (int)direction.X;
    return frame.MoveY == (int)direction.Y;
  }

  private void DrawStick(Vector2 center, Vector2? value, bool idle)
  {
    if (idle)
    {
      Draw.Circle(center, StickRadius, Color.White, 2f, 24);
      return;
    }
    if (!fed.HasValue || !value.HasValue)
      return;
    Vector2 stick = value.Value;
    if (!DiffersFromDigital(stick, fed.Value))
      return;
    Draw.Circle(center, StickRadius, Color.White, 2f, 24);
    MTexture dot = GFX.Gui["dot"];
    Vector2 position = center + stick.SafeNormalize() * (StickRadius - DotSize / 2f);
    dot.DrawCentered(position, Color.White, DotSize / dot.Width);
  }
}
