using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Replays;

public sealed class Replay
{
  public const int FormatVersion = 2;
  public const int ButtonCount = 15;
  public const int MaxFrames = 2_000_000;
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
    foreach (InputFrame f in Frames)
    {
      w.Write((ushort)f.Held);
      w.Write((ushort)f.Pressed);
      w.Write((ushort)f.Released);
      w.Write(f.GrabToggle);
      w.Write(f.HasPlayer);
      w.Write(f.MoveX);
      w.Write(f.MoveY);
      w.Write(f.GliderMoveY);
      WriteVector(w, f.Aim);
      WriteVector(w, f.Feather);
      WriteVector(w, f.Position);
      WriteVector(w, f.Speed);
      w.Write(f.State);
      w.Write(f.Dashes);
      w.Write(f.Stamina);
    }
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
    replay.Frames = new List<InputFrame>(count);
    for (int i = 0; i < count; i++)
    {
      InputFrame f = new()
      {
        Held = (Actions)r.ReadUInt16(),
        Pressed = (Actions)r.ReadUInt16(),
        Released = (Actions)r.ReadUInt16(),
        GrabToggle = r.ReadBoolean(),
        HasPlayer = r.ReadBoolean(),
        MoveX = r.ReadSByte(),
        MoveY = r.ReadSByte(),
        GliderMoveY = r.ReadSByte(),
      };
      f.Aim = ReadVector(r);
      f.Feather = ReadVector(r);
      f.Position = ReadVector(r);
      f.Speed = ReadVector(r);
      f.State = r.ReadInt32();
      f.Dashes = r.ReadInt32();
      f.Stamina = r.ReadSingle();
      replay.Frames.Add(f);
    }
    return replay;
  }

  private static void WriteVector(BinaryWriter w, Vector2 v)
  {
    w.Write(v.X);
    w.Write(v.Y);
  }

  private static Vector2 ReadVector(BinaryReader r) => new(r.ReadSingle(), r.ReadSingle());
}
