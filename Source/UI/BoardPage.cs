using System;
using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Online;

namespace Celeste.Mod.ClimbTrials.UI;

public sealed class BoardPage
{
  public int Number { get; }
  public int Count { get; }
  public int Total { get; }
  public IReadOnlyList<BoardRow> Rows { get; }
  public BoardRow Me { get; }

  private BoardPage(int number, int count, int total, IReadOnlyList<BoardRow> rows, BoardRow me)
  {
    Number = number;
    Count = count;
    Total = total;
    Rows = rows;
    Me = me;
  }

  public static int PageCount(int total) =>
    Math.Max(1, (total + BoardLayout.PageRows - 1) / BoardLayout.PageRows);

  public static int ChunkOf(int page) => (page - 1) * BoardLayout.PageRows / Standings.ChunkRows;

  public static bool LastOfChunk(int page) => ChunkOf(page + 1) != ChunkOf(page);

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
    return new BoardPage(page, PageCount(chunk.Total), chunk.Total, rows, chunk.Me);
  }
}
