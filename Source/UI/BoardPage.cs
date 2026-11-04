using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Online;

namespace Celeste.Mod.ClimbTrials.UI;

public sealed class BoardPage
{
  public int Number { get; }
  public int Count { get; }
  public int? Total { get; }
  public IReadOnlyList<BoardRow> Rows { get; }
  public BoardRow Me { get; }
  public BoardMode BoardMode { get; }

  private BoardPage(int number, Board chunk, IReadOnlyList<BoardRow> rows)
  {
    Number = number;
    Count = PageCount(chunk.Total);
    Total = chunk.Total;
    Rows = rows;
    Me = chunk.Me;
    BoardMode = chunk.BoardMode;
  }

  public static int PageCount(int? total) =>
    total is int count ? Math.Max(1, (count + BoardLayout.PageRows - 1) / BoardLayout.PageRows) : 1;

  public static int ChunkOf(int page) => (page - 1) * BoardLayout.PageRows / Standings.ChunkRows;

  public static bool Covers(Board chunk, int page)
  {
    int start = (page - 1) * BoardLayout.PageRows;
    return start >= chunk.Offset && start < chunk.Offset + Standings.ChunkRows;
  }

  public static BoardPage From(Board chunk, int page)
  {
    int start = Math.Clamp((page - 1) * BoardLayout.PageRows - chunk.Offset, 0, chunk.Rows.Count);
    int count = Math.Clamp(chunk.Rows.Count - start, 0, BoardLayout.PageRows);
    List<BoardRow> rows = chunk.Rows.GetRange(start, count);
    return new BoardPage(page, chunk, rows);
  }
}
