using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Replays;

public sealed class Replay
{
  public const int FormatVersion = 3;
  public const int ButtonCount = 15;
  public const int MaxFrames = 2_000_000;
  public const int FrameBytes = 55;
  private const string Magic = "CTRP";

  public string TrialId;
  public string CelesteVersion;
  public string EverestVersion;
  public List<string> Modules = [];
  public bool OtherModsLoaded;
  public string PlayerName;
  public string Language;
  public GrabModes GrabMode;
  public CrouchDashModes CrouchDashMode;
  public ScreenshakeAmount ScreenShake;
  public Vector2 LastAim;
  public float TimeActive;
  public float RawTimeActive;
  public float RawDeltaTime;
  public int LoadSeed;
  public int RunSeed;
  public ButtonState[] Buttons = new ButtonState[ButtonCount];
  public long Ticks;
  public DateTime AchievedAtUtc;
  public List<InputFrame> Frames = [];

  public bool MatchesGame() => CelesteVersion == Celeste.Instance.Version.ToString();

  public bool IsOwn => true;

  public void Write(BinaryWriter w)
  {
    w.Write(Magic);
    w.Write(FormatVersion);
    w.Write(TrialId);
    w.Write(CelesteVersion);
    w.Write(EverestVersion);
    w.Write(Modules.Count);
    foreach (string module in Modules)
      w.Write(module);
    w.Write(OtherModsLoaded);
    w.Write(PlayerName);
    w.Write(Language);
    w.Write((int)GrabMode);
    w.Write((int)CrouchDashMode);
    w.Write((int)ScreenShake);
    WriteVector(w, LastAim);
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
    w.Write(AchievedAtUtc.Ticks);
    w.Write(Frames.Count);
    byte[] packed = PackFrames();
    w.Write(packed.Length);
    w.Write(packed);
  }

  public static Replay Read(BinaryReader r)
  {
    if (r.ReadString() != Magic)
      throw new InvalidDataException("not a ClimbTrials replay");
    int version = r.ReadInt32();
    if (version != FormatVersion)
      throw new InvalidDataException($"replay format {version}, expected {FormatVersion}");

    Replay replay = new()
    {
      TrialId = r.ReadString(),
      CelesteVersion = r.ReadString(),
      EverestVersion = r.ReadString(),
    };
    int moduleCount = Math.Min(r.ReadInt32(), 10_000);
    for (int i = 0; i < moduleCount; i++)
      replay.Modules.Add(r.ReadString());
    replay.OtherModsLoaded = r.ReadBoolean();
    replay.PlayerName = r.ReadString();
    replay.Language = r.ReadString();
    replay.GrabMode = (GrabModes)r.ReadInt32();
    replay.CrouchDashMode = (CrouchDashModes)r.ReadInt32();
    replay.ScreenShake = (ScreenshakeAmount)r.ReadInt32();
    replay.LastAim = ReadVector(r);
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
    replay.AchievedAtUtc = new DateTime(r.ReadInt64(), DateTimeKind.Utc);

    int count = r.ReadInt32();
    if (count < 0 || count > MaxFrames)
      throw new InvalidDataException($"replay has {count} frames");
    int packedLength = r.ReadInt32();
    if (packedLength < 0 || packedLength > (long)MaxFrames * FrameBytes)
      throw new InvalidDataException($"replay frame block is {packedLength} bytes");
    byte[] packed = r.ReadBytes(packedLength);
    if (packed.Length != packedLength)
      throw new InvalidDataException("replay file is truncated");
    replay.Frames = UnpackFrames(packed, count);
    return replay;
  }

  private byte[] PackFrames()
  {
    using MemoryStream output = new();
    using (BrotliStream brotli = new(output, CompressionLevel.Optimal, leaveOpen: true))
    using (BinaryWriter w = new(brotli))
    {
      WriteColumns(w, Frames);
    }
    return output.ToArray();
  }

  private static List<InputFrame> UnpackFrames(byte[] packed, int count)
  {
    using MemoryStream input = new(packed);
    using BrotliStream brotli = new(input, CompressionMode.Decompress);
    using BinaryReader r = new(brotli);
    return ReadColumns(r, count);
  }

  public static void WriteColumns(BinaryWriter w, List<InputFrame> frames)
  {
    foreach (InputFrame f in frames)
      w.Write((ushort)f.Held);
    foreach (InputFrame f in frames)
      w.Write((ushort)f.Pressed);
    foreach (InputFrame f in frames)
      w.Write((ushort)f.Released);
    foreach (InputFrame f in frames)
      w.Write(f.GrabToggle);
    foreach (InputFrame f in frames)
      w.Write(f.HasPlayer);
    foreach (InputFrame f in frames)
      w.Write(f.MoveX);
    foreach (InputFrame f in frames)
      w.Write(f.MoveY);
    foreach (InputFrame f in frames)
      w.Write(f.GliderMoveY);
    foreach (InputFrame f in frames)
      WriteVector(w, f.Aim);
    foreach (InputFrame f in frames)
      WriteVector(w, f.Feather);
    foreach (InputFrame f in frames)
      WriteVector(w, f.Position);
    foreach (InputFrame f in frames)
      WriteVector(w, f.Speed);
    foreach (InputFrame f in frames)
      w.Write(f.State);
    foreach (InputFrame f in frames)
      w.Write(f.Dashes);
    foreach (InputFrame f in frames)
      w.Write(f.Stamina);
  }

  public static List<InputFrame> ReadColumns(BinaryReader r, int count)
  {
    InputFrame[] frames = new InputFrame[count];
    for (int i = 0; i < count; i++)
      frames[i].Held = (Actions)r.ReadUInt16();
    for (int i = 0; i < count; i++)
      frames[i].Pressed = (Actions)r.ReadUInt16();
    for (int i = 0; i < count; i++)
      frames[i].Released = (Actions)r.ReadUInt16();
    for (int i = 0; i < count; i++)
      frames[i].GrabToggle = r.ReadBoolean();
    for (int i = 0; i < count; i++)
      frames[i].HasPlayer = r.ReadBoolean();
    for (int i = 0; i < count; i++)
      frames[i].MoveX = r.ReadSByte();
    for (int i = 0; i < count; i++)
      frames[i].MoveY = r.ReadSByte();
    for (int i = 0; i < count; i++)
      frames[i].GliderMoveY = r.ReadSByte();
    for (int i = 0; i < count; i++)
      frames[i].Aim = ReadVector(r);
    for (int i = 0; i < count; i++)
      frames[i].Feather = ReadVector(r);
    for (int i = 0; i < count; i++)
      frames[i].Position = ReadVector(r);
    for (int i = 0; i < count; i++)
      frames[i].Speed = ReadVector(r);
    for (int i = 0; i < count; i++)
      frames[i].State = r.ReadInt32();
    for (int i = 0; i < count; i++)
      frames[i].Dashes = r.ReadInt32();
    for (int i = 0; i < count; i++)
      frames[i].Stamina = r.ReadSingle();
    return [.. frames];
  }

  private static void WriteVector(BinaryWriter w, Vector2 v)
  {
    w.Write(v.X);
    w.Write(v.Y);
  }

  private static Vector2 ReadVector(BinaryReader r) => new(r.ReadSingle(), r.ReadSingle());
}
