using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Celeste.Mod.ClimbTrials.Replays.Codec;

internal sealed class ChanceTable
{
  public const int TableBits = 20;
  public const int TableSize = 1 << TableBits;
  private const ushort Half = 32768;

  internal readonly ushort[] ChanceOfOne = new ushort[TableSize];
  internal readonly byte[] UpdateCounts = new byte[TableSize];

  public ChanceTable()
  {
    Array.Fill(ChanceOfOne, Half);
  }

  public void CopyTo(ChanceTable target)
  {
    ChanceOfOne.CopyTo(target.ChanceOfOne, 0);
    UpdateCounts.CopyTo(target.UpdateCounts, 0);
  }
}

internal enum ContextKind : uint
{
  HeaderNumber = 1,
  HeaderHighHalf = 2,
  HeaderByte = 3,
  Value = 4,
  Hit = 5,
  CoordinateForm = 6,
  CoordinateFloatBits = 7,
  Repeat = 8,
  GroupFlag = 9,
}

internal sealed class Coder
{
  private const int UpdatesAtSlowestRate = 250;
  private static readonly int[] UpdateRates = BuildUpdateRates();
  private const int LengthPart = 0;
  private const int BitsPart = 1;
  private const int MaxBitLength = 32;
  private const int DerivedBytes = 3;

  public readonly bool Decoding;
  private readonly ChanceTable chances;
  private readonly byte[] input;
  private int bytesRead;
  private readonly List<byte> output = [];
  private uint low;
  private uint high = 0xffffffff;
  private uint code;

  public Coder(ChanceTable chances)
  {
    this.chances = chances;
  }

  public Coder(ChanceTable chances, byte[] codedBytes)
  {
    this.chances = chances;
    input = codedBytes;
    Decoding = true;
    for (int read = 0; read < 4; read++)
      code = (code << 8) | ReadByte();
  }

  public static uint Extend(uint context, int part)
  {
    uint hash = unchecked((context ^ 0x5bd1e995) * 0x9e3779b1) ^ (uint)part;
    hash = unchecked((hash ^ (hash >> 15)) * 0x85ebca6b);
    return hash ^ (hash >> 13);
  }

  public int CodeBit(uint context, int bit)
  {
    int place = (int)(context >> (32 - ChanceTable.TableBits));
    uint chance = Math.Max(1u, (uint)chances.ChanceOfOne[place] >> 4);
    uint split = low + (uint)(((ulong)(high - low) * chance) >> 12);
    if (Decoding)
      bit = code <= split ? 1 : 0;
    if (bit == 1)
      high = split;
    else
      low = split + 1;
    Learn(place, bit);
    while (((low ^ high) & 0xff000000) == 0)
      ShiftOutTopByte();
    return bit;
  }

  public uint CodeNumber(uint context, uint value) => CodeNumber(context, value, context);

  public uint CodeNumber(uint context, uint value, uint firstBitContext)
  {
    int length = Decoding ? 0 : 32 - BitOperations.LeadingZeroCount(value);
    if (CodeBit(firstBitContext, length > 0 ? 1 : 0) == 0)
      return 0;
    uint lengthContext = Extend(context, LengthPart);
    int codedLength = 1;
    while (
      codedLength < MaxBitLength
      && CodeBit(Extend(lengthContext, codedLength), codedLength < length ? 1 : 0) == 1
    )
      codedLength++;
    uint bitsContext = Extend(Extend(context, BitsPart), codedLength);
    uint bitsSoFar = 1;
    for (int bitIndex = codedLength - 2; bitIndex >= 0; bitIndex--)
    {
      int bit = (int)(value >> bitIndex) & 1;
      bitsSoFar = bitsSoFar * 2 + (uint)CodeBit(Extend(bitsContext, (int)bitsSoFar), bit);
    }
    return bitsSoFar;
  }

  public byte CodeByte(uint context, byte value)
  {
    int bitsSoFar = 1;
    for (int bitIndex = 7; bitIndex >= 0; bitIndex--)
      bitsSoFar = bitsSoFar * 2 + CodeBit(Extend(context, bitsSoFar), (value >> bitIndex) & 1);
    return (byte)(bitsSoFar - 256);
  }

  public byte[] Finish()
  {
    output.Add((byte)(low >> 24));
    return output.ToArray();
  }

  public void CheckEnd()
  {
    if (bytesRead != input.Length + DerivedBytes)
      throw new InvalidDataException("the coded replay has the wrong length");
  }

  private static int[] BuildUpdateRates()
  {
    int[] rates = new int[UpdatesAtSlowestRate + 1];
    for (int updates = 0; updates <= UpdatesAtSlowestRate; updates++)
      rates[updates] = (int)Math.Floor(65536 / (updates + 1.5));
    return rates;
  }

  private uint ReadByte()
  {
    if (bytesRead >= input.Length + DerivedBytes)
      throw new InvalidDataException("the coded replay ends too early");
    uint value = bytesRead < input.Length ? input[bytesRead] : 255u;
    bytesRead++;
    return value;
  }

  private void Learn(int place, int bit)
  {
    int chance = chances.ChanceOfOne[place];
    int updates = chances.UpdateCounts[place];
    int distance = (bit == 1 ? 65535 : 0) - chance;
    chances.ChanceOfOne[place] = (ushort)(chance + (long)distance * UpdateRates[updates] / 65536);
    if (updates < UpdatesAtSlowestRate)
      chances.UpdateCounts[place] = (byte)(updates + 1);
  }

  private void ShiftOutTopByte()
  {
    if (Decoding)
      code = (code << 8) | ReadByte();
    else
      output.Add((byte)(high >> 24));
    low <<= 8;
    high = (high << 8) | 255;
  }
}
