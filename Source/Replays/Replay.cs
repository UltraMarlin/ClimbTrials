using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Replays;

public sealed class Replay
{
  public const int FormatVersion = 11;
  public const int ButtonCount = 15;
  public const int MaxFrames = 216_000;
  public const int MaxModules = 256;
  public const int MaxAnimations = 255;
  public const int MaxStringLength = 128;
  public static int FrameBytes => ReplayColumns.FrameBytes;
  private const string Magic = "CTRP";
  private const int HashBytes = 32;

  public string TrialId;
  public byte[] TrialHash = new byte[Trial.DefinitionHashBytes];
  public string CelesteVersion;
  public string EverestVersion;
  public List<ModuleRef> Modules = [];
  public string Language;
  public GrabModes GrabMode;
  public CrouchDashModes CrouchDashMode;
  public ScreenshakeAmount ScreenShake;
  public bool ModOptionsInPauseMenu;
  public DebugCause DebugCauses;
  public bool DebugMode;
  public Vector2 LastAim;
  public Player.IntroTypes IntroType = Player.IntroTypes.Respawn;
  public float TimeActive;
  public float RawTimeActive;
  public float RawDeltaTime;
  public int LoadSeed;
  public int RunSeed;
  public ButtonState[] Buttons = new ButtonState[ButtonCount];
  public long Ticks;
  public DateTime RunStartedUtc;
  public DateTime AchievedAtUtc;
  public long WallMilliseconds;
  public List<string> Animations = [];
  public List<InputFrame> Frames = [];

  public bool IsOwn { get; set; }
  public string OwnerName { get; set; }

  public bool MatchesGame() => CelesteVersion == Celeste.Instance.Version.ToString();

  public byte AddAnimation(string animation)
  {
    if (string.IsNullOrEmpty(animation))
      return 0;
    int index = Animations.IndexOf(animation);
    if (index < 0)
    {
      if (Animations.Count >= MaxAnimations)
        return 0;
      Animations.Add(animation);
      index = Animations.Count - 1;
    }
    return (byte)(index + 1);
  }

  public string AnimationAt(byte id) =>
    id == 0 || id > Animations.Count ? null : Animations[id - 1];

  public byte[] ToBytes()
  {
    using MemoryStream stream = new();
    using (BinaryWriter w = new(stream))
      Write(w);
    byte[] payload = stream.ToArray();
    byte[] bytes = new byte[payload.Length + HashBytes];
    payload.CopyTo(bytes, 0);
    SHA256.HashData(payload, bytes.AsSpan(payload.Length));
    return bytes;
  }

  public static Replay FromBytes(byte[] bytes)
  {
    if (bytes.Length <= HashBytes)
      throw new InvalidDataException("replay file is too short");
    int payloadLength = bytes.Length - HashBytes;
    using MemoryStream stream = new(bytes, 0, payloadLength);
    using BinaryReader r = new(stream);
    CheckFormat(r);
    ReadOnlySpan<byte> payload = bytes.AsSpan(0, payloadLength);
    ReadOnlySpan<byte> hash = bytes.AsSpan(payloadLength);
    if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(payload), hash))
      throw new InvalidDataException("replay file is corrupted");
    return Read(r);
  }

  private void Write(BinaryWriter w)
  {
    w.Write(Magic);
    w.Write(FormatVersion);
    w.Write(TrialId);
    w.Write(TrialHash);
    w.Write(CelesteVersion);
    w.Write(EverestVersion);
    w.Write(Modules.Count);
    foreach (ModuleRef module in Modules)
    {
      w.Write(module.Name);
      w.Write(module.Version);
    }
    w.Write(Language);
    w.Write((int)GrabMode);
    w.Write((int)CrouchDashMode);
    w.Write((int)ScreenShake);
    w.Write(ModOptionsInPauseMenu);
    w.Write((int)DebugCauses);
    w.Write(DebugMode);
    ReplayColumns.WriteVector(w, LastAim);
    w.Write((byte)IntroType);
    w.Write(TimeActive);
    w.Write(RawTimeActive);
    w.Write(RawDeltaTime);
    w.Write(LoadSeed);
    w.Write(RunSeed);
    foreach (ButtonState b in Buttons)
    {
      w.Write(b.Buffer);
      w.Write(b.Repeat);
      w.Write(b.Repeating);
    }
    w.Write(Ticks);
    w.Write(RunStartedUtc.Ticks);
    w.Write(AchievedAtUtc.Ticks);
    w.Write(WallMilliseconds);
    w.Write(Animations.Count);
    foreach (string animation in Animations)
      w.Write(animation);
    w.Write(Frames.Count);
    byte[] packed = Pack(writer => WriteColumns(writer, Frames));
    w.Write(packed.Length);
    w.Write(packed);
  }

  private static void CheckFormat(BinaryReader r)
  {
    if (ReadString(r, 4) != Magic)
      throw new InvalidDataException("not a ClimbTrials replay");
    int version = r.ReadInt32();
    if (version < FormatVersion)
      throw new InvalidDataException("outdated replay format");
    if (version > FormatVersion)
      throw new InvalidDataException("replay format from a future release");
  }

  private static Replay Read(BinaryReader r)
  {
    Replay replay = new()
    {
      TrialId = ReadString(r, MaxStringLength),
      TrialHash = r.ReadBytes(Trial.DefinitionHashBytes),
      CelesteVersion = ReadString(r, MaxStringLength),
      EverestVersion = ReadString(r, MaxStringLength),
    };
    int moduleCount = ReadCount(r, MaxModules, "modules");
    for (int i = 0; i < moduleCount; i++)
    {
      string name = ReadString(r, MaxStringLength);
      string moduleVersion = ReadString(r, MaxStringLength);
      replay.Modules.Add(new ModuleRef(name, moduleVersion));
    }
    replay.Language = ReadString(r, MaxStringLength);
    replay.GrabMode = (GrabModes)r.ReadInt32();
    replay.CrouchDashMode = (CrouchDashModes)r.ReadInt32();
    replay.ScreenShake = (ScreenshakeAmount)r.ReadInt32();
    replay.ModOptionsInPauseMenu = r.ReadBoolean();
    replay.DebugCauses = (DebugCause)r.ReadInt32();
    replay.DebugMode = r.ReadBoolean();
    replay.LastAim = ReplayColumns.ReadVector(r);
    replay.IntroType = (Player.IntroTypes)r.ReadByte();
    if (Array.IndexOf(RunDefinition.AllowedIntroTypes, replay.IntroType) < 0)
      throw new InvalidDataException("unknown intro type");
    replay.TimeActive = r.ReadSingle();
    replay.RawTimeActive = r.ReadSingle();
    replay.RawDeltaTime = r.ReadSingle();
    replay.LoadSeed = r.ReadInt32();
    replay.RunSeed = r.ReadInt32();
    for (int i = 0; i < ButtonCount; i++)
    {
      replay.Buttons[i].Buffer = r.ReadSingle();
      replay.Buttons[i].Repeat = r.ReadSingle();
      replay.Buttons[i].Repeating = r.ReadBoolean();
    }
    replay.Ticks = r.ReadInt64();
    replay.RunStartedUtc = ReadUtc(r);
    replay.AchievedAtUtc = ReadUtc(r);
    replay.WallMilliseconds = r.ReadInt64();
    int animationCount = ReadCount(r, MaxAnimations, "animations");
    for (int i = 0; i < animationCount; i++)
      replay.Animations.Add(ReadString(r, MaxStringLength));

    int count = ReadCount(r, MaxFrames, "frames");
    int packedLength = ReadCount(r, MaxFrames * FrameBytes, "packed bytes");
    byte[] packed = r.ReadBytes(packedLength);
    if (packed.Length != packedLength)
      throw new InvalidDataException("replay file is truncated");
    replay.Frames = UnpackFrames(packed, count);
    return replay;
  }

  private static byte[] Pack(Action<BinaryWriter> write)
  {
    using MemoryStream output = new();
    using (BrotliStream brotli = new(output, CompressionLevel.Optimal, leaveOpen: true))
    using (BinaryWriter w = new(brotli))
    {
      write(w);
    }
    return output.ToArray();
  }

  private static List<InputFrame> UnpackFrames(byte[] packed, int count)
  {
    byte[] raw = new byte[count * FrameBytes];
    using (MemoryStream input = new(packed))
    using (BrotliStream brotli = new(input, CompressionMode.Decompress))
    {
      brotli.ReadExactly(raw);
      if (brotli.ReadByte() != -1)
        throw new InvalidDataException("frame block is longer than the frame count says");
    }
    using MemoryStream frames = new(raw);
    using BinaryReader r = new(frames);
    return ReadColumns(r, count);
  }

  public static void WriteColumns(BinaryWriter w, List<InputFrame> frames)
  {
    foreach (ReplayColumn column in ReplayColumns.All)
    foreach (InputFrame f in frames)
      column.Write(w, f);
  }

  public static List<InputFrame> ReadColumns(BinaryReader r, int count)
  {
    InputFrame[] frames = new InputFrame[count];
    foreach (ReplayColumn column in ReplayColumns.All)
      for (int i = 0; i < count; i++)
        column.Read(r, ref frames[i]);
    return [.. frames];
  }

  private static string ReadString(BinaryReader r, int maxLength)
  {
    string s = r.ReadString();
    if (s.Length > maxLength)
      throw new InvalidDataException($"a string of {s.Length} chars, the limit is {maxLength}");
    return s;
  }

  private static int ReadCount(BinaryReader r, int max, string what)
  {
    int count = r.ReadInt32();
    if (count < 0 || count > max)
      throw new InvalidDataException($"{count} {what}, the limit is {max}");
    return count;
  }

  private static DateTime ReadUtc(BinaryReader r)
  {
    long ticks = r.ReadInt64();
    if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
      throw new InvalidDataException("timestamp out of range");
    return new DateTime(ticks, DateTimeKind.Utc);
  }
}
