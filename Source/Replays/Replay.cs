using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays.Codec;
using Celeste.Mod.ClimbTrials.Runs;

namespace Celeste.Mod.ClimbTrials.Replays;

public sealed class Replay
{
  public const int FormatVersion = 13;
  private const int ChecksumBytes = 8;

  public string TrialId;
  public byte[] TrialHash;
  public string CelesteVersion;
  public List<ModuleRef> Modules = [];
  public string Language;
  public GrabModes GrabMode;
  public CrouchDashModes CrouchDashMode;
  public ScreenshakeAmount ScreenShake;
  public DebugCause DebugCauses;
  public Player.IntroTypes IntroType;
  public float TimeActive;
  public float RawTimeActive;
  public float RawDeltaTime;
  public int LoadSeed;
  public int RunSeed;
  public ButtonState[] Buttons = new ButtonState[HeaderCoding.ButtonCount];
  public long Ticks;
  public DateTime AchievedAtUtc;
  public long WallMilliseconds;
  public List<string> ExtraAnimations = [];
  public List<PauseMenuRecord> PauseMenus = [];
  public List<InputFrame> Frames = [];

  public bool IsOwn { get; set; }
  public bool IsPersonalBest { get; set; }

  public byte[] SourceBytes { get; private set; }

  private int[] shownFrames;

  public long TicksPerFrame => Attempt.SecondsToTicks(RawDeltaTime);

  public int[] ShownFrames => shownFrames ??= FindShownFrames();

  public long TicksOfShownFrame(int shownIndex) => shownIndex * TicksPerFrame;

  public long ShownFrameAt(long ticks) => ticks / TicksPerFrame;

  private int[] FindShownFrames()
  {
    if (TicksPerFrame <= 0)
      return [];
    List<int> shown = new(Frames.Count);
    for (int i = 0; i < Frames.Count; i++)
      if (Frames[i].LevelUpdated)
        shown.Add(i);
    return shown.ToArray();
  }

  public string OwnerName { get; set; }
  public string ServerReplayId { get; set; }

  public bool MatchesGame() => CelesteVersion == Celeste.Instance.Version.ToString();

  public static byte[] ShortTrialHash(byte[] definitionHash) =>
    definitionHash[..HeaderCoding.TrialHashBytes];

  public bool MatchesTrialHash(IEnumerable<byte[]> acceptedTrialHashes) =>
    acceptedTrialHashes.Any(acceptedHash => TrialHash.AsSpan().SequenceEqual(acceptedHash));

  public byte AddAnimation(string animation)
  {
    if (string.IsNullOrEmpty(animation))
      return 0;

    int id = PlayerAnimations.IdOf(animation);
    if (id > 0)
      return (byte)id;

    int index = ExtraAnimations.IndexOf(animation);
    if (index < 0)
    {
      if (PlayerAnimations.Names.Length + ExtraAnimations.Count >= HeaderCoding.MaxAnimations)
        return 0;

      ExtraAnimations.Add(animation);
      index = ExtraAnimations.Count - 1;
    }
    return (byte)(PlayerAnimations.Names.Length + index + 1);
  }

  public string AnimationAt(byte id)
  {
    if (id == 0)
      return null;

    if (id <= PlayerAnimations.Names.Length)
      return PlayerAnimations.Names[id - 1];

    int extraIndex = id - PlayerAnimations.Names.Length - 1;
    return extraIndex < ExtraAnimations.Count ? ExtraAnimations[extraIndex] : null;
  }

  public byte[] ToBytes()
  {
    byte[] coded = ReplayCodec.Encode(ToCodecReplay());
    int payloadLength = 1 + coded.Length;
    byte[] bytes = new byte[payloadLength + ChecksumBytes];
    bytes[0] = FormatVersion;
    coded.CopyTo(bytes, 1);
    SHA256
      .HashData(bytes.AsSpan(0, payloadLength))
      .AsSpan(0, ChecksumBytes)
      .CopyTo(bytes.AsSpan(payloadLength));
    return bytes;
  }

  public static Replay FromBytes(byte[] bytes)
  {
    int payloadLength = bytes.Length - ChecksumBytes;
    if (payloadLength <= 1)
      throw new InvalidDataException("replay file is too short");
    if (bytes[0] != FormatVersion)
      throw new InvalidDataException($"unsupported replay format {bytes[0]}");
    VerifyChecksum(bytes, payloadLength);
    Replay replay = FromCodecReplay(ReplayCodec.Decode(bytes[1..payloadLength]));
    replay.SourceBytes = bytes;
    return replay;
  }

  private static void VerifyChecksum(byte[] bytes, int payloadLength)
  {
    ReadOnlySpan<byte> checksum = bytes.AsSpan(payloadLength);
    ReadOnlySpan<byte> hash = SHA256
      .HashData(bytes.AsSpan(0, payloadLength))
      .AsSpan(0, checksum.Length);
    if (!CryptographicOperations.FixedTimeEquals(hash, checksum))
      throw new InvalidDataException("replay file is corrupted");
  }

  private CodecReplay ToCodecReplay()
  {
    CodecHeader header = new()
    {
      TrialId = TrialId,
      TrialHash = TrialHash,
      Modules = Modules.ConvertAll(module => new CodecModule(module.Name, module.Version)),
      Language = Language,
      GrabMode = (int)GrabMode,
      CrouchDashMode = (int)CrouchDashMode,
      ScreenShake = (int)ScreenShake,
      DebugCauses = (int)DebugCauses,
      IntroType = (int)IntroType,
      TimeActive = TimeActive,
      RawTimeActive = RawTimeActive,
      RawDeltaTime = RawDeltaTime,
      LoadSeed = LoadSeed,
      RunSeed = RunSeed,
      Buttons = Buttons
        .Select(button => new CodecButton(button.Buffer, button.Repeat, button.Repeating))
        .ToList(),
      Ticks = Ticks,
      AchievedAtUtcTicks = AchievedAtUtc.Ticks,
      WallMilliseconds = (int)Math.Min(WallMilliseconds, int.MaxValue),
      ExtraAnimations = ExtraAnimations,
      PauseMenus = PauseMenus.ConvertAll(PauseRows.ToCodecMenu),
    };
    return new CodecReplay(header, RecordedFrames.ToFrameTable(Frames));
  }

  private static Replay FromCodecReplay(CodecReplay coded)
  {
    CodecHeader header = coded.Header;
    Replay replay = new()
    {
      TrialId = header.TrialId,
      TrialHash = header.TrialHash,
      Modules = header.Modules.ConvertAll(module => new ModuleRef(module.Name, module.Version)),
      Language = header.Language,
      GrabMode = (GrabModes)header.GrabMode,
      CrouchDashMode = (CrouchDashModes)header.CrouchDashMode,
      ScreenShake = (ScreenshakeAmount)header.ScreenShake,
      DebugCauses = (DebugCause)header.DebugCauses,
      IntroType = ToIntroType(header.IntroType),
      TimeActive = header.TimeActive,
      RawTimeActive = header.RawTimeActive,
      RawDeltaTime = header.RawDeltaTime,
      LoadSeed = header.LoadSeed,
      RunSeed = header.RunSeed,
      Buttons = header
        .Buttons.Select(button => new ButtonState
        {
          Buffer = button.Buffer,
          Repeat = button.Repeat,
          Repeating = button.Repeating,
        })
        .ToArray(),
      Ticks = header.Ticks,
      AchievedAtUtc = ToUtc(header.AchievedAtUtcTicks),
      WallMilliseconds = header.WallMilliseconds,
      ExtraAnimations = header.ExtraAnimations,
      PauseMenus = header.PauseMenus.ConvertAll(PauseRows.FromCodecMenu),
      Frames = RecordedFrames.FromFrameTable(coded.Frames),
    };
    replay.CelesteVersion = replay.Modules.Find(module => module.Name == "Celeste").Version;
    return replay;
  }

  private static Player.IntroTypes ToIntroType(int value)
  {
    Player.IntroTypes introType = (Player.IntroTypes)value;
    if (Array.IndexOf(RunDefinition.AllowedIntroTypes, introType) < 0)
      throw new InvalidDataException("unknown intro type");
    return introType;
  }

  private static DateTime ToUtc(long ticks)
  {
    if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
      throw new InvalidDataException("timestamp out of range");
    return new DateTime(ticks, DateTimeKind.Utc);
  }
}
