using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Celeste.Mod.ClimbTrials.Replays.Codec;
using Celeste.Mod.ClimbTrials.UI;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Replays;

public enum PauseRowKind : byte
{
  OtherMod = 0,
  Resume = 1,
  SkipCutscene = 2,
  Retry = 3,
  Assist = 4,
  Variants = 5,
  Options = 6,
  SaveAndQuit = 7,
  RestartChapter = 8,
  ReturnToMap = 9,
  ModOptions = 10,
  RestartTrial = 11,
  ReturnToTrials = 12,
  Race = 13,
  Leaderboard = 14,
}

public readonly record struct PauseRow(PauseRowKind Kind, bool Hoverable, string Label);

public sealed class PauseMenuRecord
{
  public const int NoSelectedRow = -1;
  public List<PauseRow> Rows = [];
  public int SelectedRow = NoSelectedRow;
}

public static class PauseRows
{
  private static readonly Dictionary<string, PauseRowKind> vanillaKeys = new()
  {
    ["menu_pause_resume"] = PauseRowKind.Resume,
    ["menu_pause_skip_cutscene"] = PauseRowKind.SkipCutscene,
    ["menu_pause_retry"] = PauseRowKind.Retry,
    ["menu_pause_assist"] = PauseRowKind.Assist,
    ["menu_pause_variant"] = PauseRowKind.Variants,
    ["menu_pause_options"] = PauseRowKind.Options,
    ["menu_pause_savequit"] = PauseRowKind.SaveAndQuit,
    ["menu_pause_restartarea"] = PauseRowKind.RestartChapter,
    ["menu_pause_return"] = PauseRowKind.ReturnToMap,
    ["menu_pause_modoptions"] = PauseRowKind.ModOptions,
  };

  public static PauseMenuRecord Describe(TextMenu menu)
  {
    PauseMenuRecord record = new();
    List<TextMenu.Item> selectable = SelectableRows(menu);
    foreach (TextMenu.Item item in selectable)
    {
      PauseRowKind kind = KindOf(item);
      string label = kind == PauseRowKind.OtherMod ? LabelOf(item) : null;
      record.Rows.Add(new PauseRow(kind, item.Hoverable, label));
    }
    if (menu.Items.IsInRange(menu.Selection))
      record.SelectedRow = selectable.IndexOf(menu.Items[menu.Selection]);
    return record;
  }

  public static void Rebuild(TextMenu menu, PauseMenuRecord record)
  {
    List<TextMenu.Item> present = SelectableRows(menu);
    List<TextMenu.Item> wanted = MatchRows(present, record.Rows);
    if (!wanted.SequenceEqual(present))
      ReplaceRows(menu, present, wanted);
    for (int i = 0; i < wanted.Count; i++)
      if (wanted[i].Hoverable != record.Rows[i].Hoverable)
        wanted[i].Disabled = !record.Rows[i].Hoverable;
    if (wanted.IsInRange(record.SelectedRow))
      menu.Selection = menu.Items.IndexOf(wanted[record.SelectedRow]);
  }

  internal static CodecPauseMenu ToCodecMenu(PauseMenuRecord menu) =>
    new(
      menu.SelectedRow,
      menu.Rows.ConvertAll(row => new CodecPauseRow((int)row.Kind, row.Hoverable, row.Label ?? ""))
    );

  internal static PauseMenuRecord FromCodecMenu(CodecPauseMenu menu)
  {
    PauseMenuRecord record = new() { SelectedRow = menu.SelectedRow };
    foreach (CodecPauseRow row in menu.Rows)
    {
      PauseRowKind kind = (PauseRowKind)row.Kind;
      if (!Enum.IsDefined(kind))
        throw new InvalidDataException($"unknown pause row {row.Kind}");
      string label = kind == PauseRowKind.OtherMod ? row.Label : null;
      record.Rows.Add(new PauseRow(kind, row.Hoverable, label));
    }
    return record;
  }

  private static List<TextMenu.Item> SelectableRows(TextMenu menu) =>
    menu.Items.Where(item => item.Selectable).Take(HeaderCoding.MaxPauseRows).ToList();

  private static PauseRowKind KindOf(TextMenu.Item item)
  {
    PauseRowKind? own = PauseMenu.FindOwnRowKind(item);
    if (own.HasValue)
      return own.Value;
    if (item is not TextMenu.Button button)
      return PauseRowKind.OtherMod;
    foreach ((string key, PauseRowKind kind) in vanillaKeys)
      if (button.Label == Dialog.Clean(key))
        return kind;
    return PauseRowKind.OtherMod;
  }

  private static string LabelOf(TextMenu.Item item)
  {
    string label = (item as TextMenu.Button)?.Label ?? "";
    if (label.Length <= HeaderCoding.MaxTextLength)
      return label;
    int length = HeaderCoding.MaxTextLength;
    if (char.IsHighSurrogate(label[length - 1]))
      length--;
    return label[..length];
  }

  private static List<TextMenu.Item> MatchRows(List<TextMenu.Item> present, List<PauseRow> rows)
  {
    List<TextMenu.Item> unclaimed = [.. present];
    List<TextMenu.Item> wanted = [];
    foreach (PauseRow row in rows)
    {
      TextMenu.Item match = unclaimed.Find(item => Matches(item, row));
      if (match == null)
        match = BuildStandIn(row);
      else
        unclaimed.Remove(match);
      wanted.Add(match);
    }
    return wanted;
  }

  private static bool Matches(TextMenu.Item item, PauseRow row) =>
    KindOf(item) == row.Kind && (row.Kind != PauseRowKind.OtherMod || LabelOf(item) == row.Label);

  private static TextMenu.Button BuildStandIn(PauseRow row)
  {
    string key = vanillaKeys.FirstOrDefault(pair => pair.Value == row.Kind).Key;
    string label =
      row.Kind == PauseRowKind.OtherMod ? row.Label
      : key != null ? Dialog.Clean(key)
      : PauseMenu.LabelOf(row.Kind);
    return new TextMenu.Button(label);
  }

  private static void ReplaceRows(
    TextMenu menu,
    List<TextMenu.Item> present,
    List<TextMenu.Item> wanted
  )
  {
    int firstRowIndex = present.Count == 0 ? menu.Items.Count : menu.Items.IndexOf(present[0]);
    List<TextMenu.Item> fromFirstRow = menu.Items.Skip(firstRowIndex).ToList();

    Dictionary<TextMenu.Item, List<TextMenu.Item>> gapsBefore = [];
    List<TextMenu.Item> gaps = [];
    foreach (TextMenu.Item item in fromFirstRow)
    {
      if (!present.Contains(item))
        gaps.Add(item);
      else if (wanted.Contains(item))
      {
        gapsBefore[item] = gaps;
        gaps = [];
      }
    }

    foreach (TextMenu.Item item in fromFirstRow)
      menu.Remove(item);
    foreach (TextMenu.Item row in wanted)
    {
      if (gapsBefore.TryGetValue(row, out List<TextMenu.Item> rowGaps))
        foreach (TextMenu.Item gap in rowGaps)
          menu.Add(gap);
      menu.Add(row);
    }
    foreach (TextMenu.Item gap in gaps)
      menu.Add(gap);
  }
}
