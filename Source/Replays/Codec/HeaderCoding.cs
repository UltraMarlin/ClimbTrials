using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Celeste.Mod.ClimbTrials.Replays.Codec;

internal sealed record CodecModule(string Name, string Version);

internal sealed record CodecButton(float Buffer, float Repeat, bool Repeating);

internal sealed record CodecPauseRow(int Kind, bool Hoverable, string Label);

internal sealed record CodecPauseMenu(int SelectedRow, List<CodecPauseRow> Rows);

internal sealed class CodecHeader
{
  public string TrialId;
  public byte[] TrialHash;
  public List<CodecModule> Modules = [];
  public string Language;
  public int GrabMode;
  public int CrouchDashMode;
  public int ScreenShake;
  public int DebugCauses;
  public int IntroType;
  public float TimeActive;
  public float RawTimeActive;
  public float RawDeltaTime;
  public int LoadSeed;
  public int RunSeed;
  public List<CodecButton> Buttons = [];
  public long Ticks;
  public long AchievedAtUtcTicks;
  public int WallMilliseconds;
  public List<string> ExtraAnimations = [];
  public List<CodecPauseMenu> PauseMenus = [];
}

internal static class HeaderCoding
{
  public const int MaxFrames = 216_000;
  public const int MaxTextLength = 128;
  public const int MaxModules = 4096;
  public const int MaxAnimations = 255;
  public const int ButtonCount = 15;
  public const int TrialHashBytes = 8;

  private const int MaxTextBytes = 3 * MaxTextLength;
  private const int MaxHeaderTextBytes = 64 * 1024;
  public const int MaxPauseRows = 64;
  private const int PauseBytesPerFrame = 16;
  private const int MaxVersionParts = 4;
  private const int MaxExtraFrames = 1_000_000;
  private const long TicksPerFrame = 170_000;
  private const float RawDeltaTime = 0.0166667f;

  private static readonly Regex NumberedVersionForm = new(
    @"^(0|[1-9][0-9]{0,5})(\.(0|[1-9][0-9]{0,5})){1,3}\z"
  );

  private static readonly UTF8Encoding StrictUtf8 = new(
    encoderShouldEmitUTF8Identifier: false,
    throwOnInvalidBytes: true
  );

  private enum HeaderField
  {
    TrialId = 1,
    TrialHash = 2,
    ModuleCount = 3,
    ModuleName = 4,
    ModuleVersionParts = 5,
    ModuleVersionPart = 6,
    ModuleVersion = 7,
    Language = 8,
    GrabMode = 9,
    CrouchDashMode = 10,
    ScreenShake = 11,
    DebugCauses = 12,
    IntroType = 13,
    RawTimesImplied = 14,
    TimeActive = 15,
    RawTimeActive = 16,
    RawDeltaTime = 17,
    LoadSeed = 18,
    RunSeed = 19,
    ButtonCount = 20,
    ButtonBuffer = 21,
    ButtonRepeat = 22,
    ButtonRepeating = 23,
    AchievedAt = 24,
    WallMilliseconds = 25,
    AnimationCount = 26,
    AnimationName = 27,
    PauseMenuCount = 28,
    PauseRowCount = 29,
    PauseSelectedRow = 30,
    PauseRowKind = 31,
    PauseRowHoverable = 32,
    PauseRowLabel = 33,
    FrameCount = 34,
    ExtraFrames = 35,
    Ticks = 36,
  }

  private sealed class HeaderWalk(Coder coder)
  {
    private int decodedTextBytes;

    public bool Decoding => coder.Decoding;

    public uint CodeNumber(HeaderField field, uint? value, uint max = uint.MaxValue)
    {
      uint context = Coder.Extend((uint)ContextKind.HeaderNumber, (int)field);
      uint result = coder.CodeNumber(context, value ?? 0);
      if (coder.Decoding && result > max)
        throw new InvalidDataException($"the header's {field} is out of range");
      return result;
    }

    public int CodeCount(HeaderField field, int? value, int max) =>
      (int)CodeNumber(field, (uint?)value, (uint)max);

    public int CodeSigned(HeaderField field, int? value) =>
      unchecked((int)CodeNumber(field, unchecked((uint?)value)));

    public bool CodeFlag(HeaderField field, bool? value) =>
      CodeNumber(field, value == true ? 1u : 0u, 1) == 1;

    public float CodeFloat(HeaderField field, float? value)
    {
      uint? bits = value.HasValue ? BitConverter.SingleToUInt32Bits(value.Value) : null;
      return BitConverter.UInt32BitsToSingle(CodeNumber(field, bits));
    }

    public long CodeLong(HeaderField field, long? value)
    {
      ulong bits = unchecked((ulong)(value ?? 0));
      ulong low = CodeNumber(field, (uint)bits);
      uint highContext = Coder.Extend((uint)ContextKind.HeaderHighHalf, (int)field);
      ulong high = coder.CodeNumber(highContext, (uint)(bits >> 32));
      return unchecked((long)(high << 32 | low));
    }

    public byte[] CodeBytes(HeaderField field, byte[] value, int maxLength)
    {
      if (!coder.Decoding)
        ArgumentNullException.ThrowIfNull(value, field.ToString());
      int length = CodeCount(field, value?.Length, maxLength);
      uint fieldContext = Coder.Extend((uint)ContextKind.HeaderByte, (int)field);
      byte[] result = new byte[length];
      byte before = 0;
      for (int index = 0; index < length; index++)
      {
        byte given = value == null ? (byte)0 : value[index];
        result[index] = coder.CodeByte(Coder.Extend(fieldContext, before), given);
        before = result[index];
      }
      return result;
    }

    public string CodeText(HeaderField field, string value)
    {
      string text = CodeText(field, value, MaxTextBytes, out int byteCount);
      decodedTextBytes += byteCount;
      if (coder.Decoding && decodedTextBytes > MaxHeaderTextBytes)
        throw new InvalidDataException(
          $"the header's texts are longer than {MaxHeaderTextBytes} bytes"
        );
      return text;
    }

    public string CodeText(HeaderField field, string value, int maxBytes, out int byteCount)
    {
      if (!coder.Decoding)
        ArgumentNullException.ThrowIfNull(value, field.ToString());
      byte[] bytes = value == null ? null : Encoding.UTF8.GetBytes(value);
      byte[] coded = CodeBytes(field, bytes, maxBytes);
      byteCount = coded.Length;
      if (!coder.Decoding)
        return value;
      string text;
      try
      {
        text = StrictUtf8.GetString(coded);
      }
      catch (DecoderFallbackException)
      {
        throw new InvalidDataException($"the header's {field} is not valid UTF-8");
      }
      if (text.Length > MaxTextLength)
        throw new InvalidDataException(
          $"the header's {field} is longer than {MaxTextLength} characters"
        );
      return text;
    }
  }

  public static CodecHeader WalkHeader(
    Coder coder,
    CodecHeader given,
    int? givenFrameCount,
    int maxFrames,
    out int frameCount
  )
  {
    HeaderWalk walk = new(coder);
    CodecHeader header = new();
    header.TrialId = walk.CodeText(HeaderField.TrialId, given?.TrialId);
    header.TrialHash = walk.CodeBytes(HeaderField.TrialHash, given?.TrialHash, TrialHashBytes);
    if (walk.Decoding && header.TrialHash.Length != TrialHashBytes)
      throw new InvalidDataException($"the trial hash has {header.TrialHash.Length} bytes");
    int moduleCount = walk.CodeCount(HeaderField.ModuleCount, given?.Modules.Count, MaxModules);
    for (int index = 0; index < moduleCount; index++)
    {
      string name = walk.CodeText(HeaderField.ModuleName, given?.Modules[index].Name);
      string version = CodeModuleVersion(walk, given?.Modules[index].Version);
      header.Modules.Add(new CodecModule(name, version));
    }
    header.Language = walk.CodeText(HeaderField.Language, given?.Language);
    header.GrabMode = walk.CodeCount(HeaderField.GrabMode, given?.GrabMode, byte.MaxValue);
    header.CrouchDashMode = walk.CodeCount(
      HeaderField.CrouchDashMode,
      given?.CrouchDashMode,
      byte.MaxValue
    );
    header.ScreenShake = walk.CodeCount(HeaderField.ScreenShake, given?.ScreenShake, byte.MaxValue);
    header.DebugCauses = walk.CodeCount(HeaderField.DebugCauses, given?.DebugCauses, byte.MaxValue);
    header.IntroType = walk.CodeCount(HeaderField.IntroType, given?.IntroType, byte.MaxValue);
    bool givenRawTimesImplied =
      given != null
      && SameBits(given.RawTimeActive, given.TimeActive)
      && SameBits(given.RawDeltaTime, RawDeltaTime);
    bool rawTimesImplied = walk.CodeFlag(HeaderField.RawTimesImplied, givenRawTimesImplied);
    header.TimeActive = walk.CodeFloat(HeaderField.TimeActive, given?.TimeActive);
    if (rawTimesImplied)
    {
      header.RawTimeActive = header.TimeActive;
      header.RawDeltaTime = RawDeltaTime;
    }
    else
    {
      header.RawTimeActive = walk.CodeFloat(HeaderField.RawTimeActive, given?.RawTimeActive);
      header.RawDeltaTime = walk.CodeFloat(HeaderField.RawDeltaTime, given?.RawDeltaTime);
    }
    header.LoadSeed = walk.CodeSigned(HeaderField.LoadSeed, given?.LoadSeed);
    header.RunSeed = walk.CodeSigned(HeaderField.RunSeed, given?.RunSeed);
    int buttonCount = walk.CodeCount(HeaderField.ButtonCount, given?.Buttons.Count, ButtonCount);
    if (walk.Decoding && buttonCount != ButtonCount)
      throw new InvalidDataException($"the header has {buttonCount} buttons, not {ButtonCount}");
    for (int index = 0; index < buttonCount; index++)
    {
      CodecButton button = given?.Buttons[index];
      float buffer = walk.CodeFloat(HeaderField.ButtonBuffer, button?.Buffer);
      float repeat = walk.CodeFloat(HeaderField.ButtonRepeat, button?.Repeat);
      bool repeating = walk.CodeFlag(HeaderField.ButtonRepeating, button?.Repeating);
      header.Buttons.Add(new CodecButton(buffer, repeat, repeating));
    }
    header.AchievedAtUtcTicks = walk.CodeLong(HeaderField.AchievedAt, given?.AchievedAtUtcTicks);
    header.WallMilliseconds = walk.CodeCount(
      HeaderField.WallMilliseconds,
      given?.WallMilliseconds,
      int.MaxValue
    );
    int animationCount = walk.CodeCount(
      HeaderField.AnimationCount,
      given?.ExtraAnimations.Count,
      MaxAnimations - PlayerAnimations.Names.Length
    );
    for (int index = 0; index < animationCount; index++)
    {
      string name = walk.CodeText(HeaderField.AnimationName, given?.ExtraAnimations[index]);
      header.ExtraAnimations.Add(name);
    }
    header.PauseMenus = CodePauseMenus(walk, given?.PauseMenus, maxFrames);
    frameCount = walk.CodeCount(HeaderField.FrameCount, givenFrameCount, maxFrames);
    return header;
  }

  private static List<CodecPauseMenu> CodePauseMenus(
    HeaderWalk walk,
    List<CodecPauseMenu> given,
    int maxFrames
  )
  {
    int menuCount = walk.CodeCount(HeaderField.PauseMenuCount, given?.Count, maxFrames);
    int bytesLeft = (int)Math.Min((long)maxFrames * PauseBytesPerFrame, int.MaxValue);
    List<CodecPauseMenu> menus = new(menuCount);
    for (int menuIndex = 0; menuIndex < menuCount; menuIndex++)
    {
      CodecPauseMenu givenMenu = given?[menuIndex];
      int rowCount = walk.CodeCount(HeaderField.PauseRowCount, givenMenu?.Rows.Count, MaxPauseRows);
      int selectedRowPlusOne = walk.CodeCount(
        HeaderField.PauseSelectedRow,
        givenMenu?.SelectedRow + 1,
        rowCount
      );
      bytesLeft -= 1 + rowCount;
      List<CodecPauseRow> rows = new(rowCount);
      for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
      {
        CodecPauseRow givenRow = givenMenu?.Rows[rowIndex];
        int kind = walk.CodeCount(HeaderField.PauseRowKind, givenRow?.Kind, byte.MaxValue);
        bool hoverable = walk.CodeFlag(HeaderField.PauseRowHoverable, givenRow?.Hoverable);
        string label = walk.CodeText(
          HeaderField.PauseRowLabel,
          givenRow?.Label,
          Math.Max(0, Math.Min(MaxTextBytes, bytesLeft)),
          out int labelBytes
        );
        bytesLeft -= labelBytes;
        rows.Add(new CodecPauseRow(kind, hoverable, label));
      }
      if (walk.Decoding && bytesLeft < 0)
        throw new InvalidDataException("the pause menus are too long");

      menus.Add(new CodecPauseMenu(selectedRowPlusOne - 1, rows));
    }
    return menus;
  }

  private static string CodeModuleVersion(HeaderWalk walk, string version)
  {
    string[] parts =
      version != null && NumberedVersionForm.IsMatch(version) ? version.Split('.') : null;
    int partCount = walk.CodeCount(
      HeaderField.ModuleVersionParts,
      parts?.Length ?? 0,
      MaxVersionParts
    );
    if (partCount == 0)
      return walk.CodeText(HeaderField.ModuleVersion, version);
    string[] numbers = new string[partCount];
    for (int index = 0; index < partCount; index++)
    {
      uint? given = parts == null ? null : uint.Parse(parts[index]);
      numbers[index] = walk.CodeNumber(HeaderField.ModuleVersionPart, given).ToString();
    }
    return string.Join('.', numbers);
  }

  private static bool SameBits(float a, float b) =>
    BitConverter.SingleToUInt32Bits(a) == BitConverter.SingleToUInt32Bits(b);

  public static long CodeTicks(Coder coder, FrameTable table, long? ticks)
  {
    HeaderWalk walk = new(coder);
    int updates = 0;
    for (int frame = 0; frame < table.FrameCount; frame++)
      if (table.LevelUpdated(frame))
        updates++;
    bool wholeFrames = ticks.HasValue && ticks.Value % TicksPerFrame == 0;
    long extraFrames = wholeFrames ? ticks.Value / TicksPerFrame - updates : -1;
    bool derivable = extraFrames >= 0 && extraFrames < MaxExtraFrames;
    int givenExtraFrames = derivable ? (int)extraFrames + 1 : 0;
    int extraFramesPlusOne = walk.CodeCount(
      HeaderField.ExtraFrames,
      givenExtraFrames,
      MaxExtraFrames
    );
    if (extraFramesPlusOne == 0)
      return walk.CodeLong(HeaderField.Ticks, ticks);
    return (updates + (long)extraFramesPlusOne - 1) * TicksPerFrame;
  }
}
