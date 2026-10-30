using System;
using System.Threading;
using System.Threading.Tasks;

namespace Celeste.Mod.ClimbTrials.Replays.Codec;

internal sealed record CodecReplay(CodecHeader Header, FrameTable Frames);

internal static class ReplayCodec
{
  private static readonly Lazy<ChanceTable> startingValues = new(
    StartingValues.ReadEmbedded,
    LazyThreadSafetyMode.ExecutionAndPublication
  );

  [ThreadStatic]
  private static ChanceTable workingTable;

  private static bool loadingStarted;

  public static void PreloadStartingValues()
  {
    if (loadingStarted)
      return;
    loadingStarted = true;
    Task.Run(() => startingValues.Value);
  }

  public static byte[] Encode(CodecReplay replay)
  {
    Coder coder = new(RefillWorkingTable());
    WalkReplay(coder, replay, HeaderCoding.MaxFrames);
    return coder.Finish();
  }

  public static CodecReplay Decode(byte[] coded, int maxFrames = HeaderCoding.MaxFrames)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(maxFrames);
    Coder coder = new(RefillWorkingTable(), coded);
    CodecReplay replay = WalkReplay(coder, null, maxFrames);
    coder.CheckEnd();
    return replay;
  }

  private static ChanceTable RefillWorkingTable()
  {
    workingTable ??= new ChanceTable();
    startingValues.Value.CopyTo(workingTable);
    return workingTable;
  }

  private static CodecReplay WalkReplay(Coder coder, CodecReplay given, int maxFrames)
  {
    CodecHeader header = HeaderCoding.WalkHeader(
      coder,
      given?.Header,
      given?.Frames.FrameCount,
      maxFrames,
      out int frameCount
    );
    FrameTable table = new(frameCount);
    new FrameWalk(coder, table, given?.Frames).Run();
    header.Ticks = HeaderCoding.CodeTicks(coder, table, given?.Header.Ticks);
    return given ?? new CodecReplay(header, table);
  }
}
