using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Celeste.Mod.ClimbTrials.Replays.Codec;

internal sealed class StartingValuesException(string message) : Exception(message);

internal static class StartingValues
{
  public const string ResourceName = "ClimbTrials.StartingValues.bin";
  private const string FileSha256 =
    "2b216876700a2d751b94d3fe2de46ef02ab0232aa4aaf02b5e4d5301d2ad0054";
  private const int ChanceCount = 64;
  private const byte TrainedUpdateCount = 5;

  public static ChanceTable ReadEmbedded()
  {
    using Stream resource = typeof(StartingValues).Assembly.GetManifestResourceStream(ResourceName);
    if (resource == null)
      throw new StartingValuesException("the starting values file is missing");

    using MemoryStream file = new();
    resource.CopyTo(file);
    string sha256 = Convert.ToHexString(SHA256.HashData(file.ToArray())).ToLowerInvariant();
    if (sha256 != FileSha256)
      throw new StartingValuesException($"the starting values file has the wrong SHA-256 {sha256}");

    file.Position = 0;
    return Read(file);
  }

  private static ChanceTable Read(Stream file)
  {
    using BrotliStream decompressed = new(file, CompressionMode.Decompress);
    using MemoryStream contents = new();
    decompressed.CopyTo(contents);
    byte[] bytes = contents.ToArray();
    if (bytes.Length != 2 * ChanceCount + ChanceTable.TableSize)
      throw new StartingValuesException("the starting values file has the wrong size");

    ushort[] chances = new ushort[ChanceCount];
    for (int index = 0; index < ChanceCount; index++)
      chances[index] = (ushort)(bytes[2 * index] | bytes[2 * index + 1] << 8);
    ChanceTable table = new();
    for (int place = 0; place < ChanceTable.TableSize; place++)
    {
      int chanceNumber = bytes[2 * ChanceCount + place];
      if (chanceNumber == 0)
        continue;

      if (chanceNumber > ChanceCount)
        throw new StartingValuesException("the starting values file is invalid");

      table.ChanceOfOne[place] = chances[chanceNumber - 1];
      table.UpdateCounts[place] = TrainedUpdateCount;
    }
    return table;
  }
}
