using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Celeste.Mod.ClimbTrials.Replays.Codec;
using Celeste.Mod.ClimbTrials.Runs;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ClimbTrials.Content;

public sealed class TrialFileException(string message) : Exception(message);

public static class TrialFile
{
  public const string LogTag = "ClimbTrials/Trials";
  public const string Folder = "ClimbTrials";

  private static readonly Dictionary<string, AreaMode> Modes = new()
  {
    ["Normal"] = AreaMode.Normal,
    ["BSide"] = AreaMode.BSide,
    ["CSide"] = AreaMode.CSide,
  };

  private static readonly Dictionary<string, Facings> FacingNames = new()
  {
    ["Left"] = Facings.Left,
    ["Right"] = Facings.Right,
  };

  private static readonly Dictionary<string, FinishKind> FinishKinds = new()
  {
    ["zone"] = FinishKind.Zone,
    ["heart"] = FinishKind.Heart,
    ["roomEnter"] = FinishKind.RoomEnter,
  };

  private static readonly Dictionary<string, Session.CoreModes> CoreModeNames = new()
  {
    ["Hot"] = Session.CoreModes.Hot,
    ["Cold"] = Session.CoreModes.Cold,
  };

  private static readonly Dictionary<string, Player.IntroTypes> IntroNames =
    RunDefinition.AllowedIntroTypes.ToDictionary(intro => intro.ToString());

  private static readonly Dictionary<string, Difficulty> DifficultyNames =
    Enum.GetValues<Difficulty>().ToDictionary(difficulty => difficulty.ToString());

  private static readonly Dictionary<string, SplitCategory> SplitCategoryNames =
    Enum.GetValues<SplitCategory>().ToDictionary(category => category.ToString());

  public static IReadOnlyList<Campaign> Load()
  {
    if (!Everest.Content.TryGet(Folder, out ModAsset folder))
      throw new TrialFileException($"The mod's campaign folder '{Folder}' is missing");

    List<(Campaign Campaign, int Order)> loaded = [];
    foreach (ModAsset asset in folder.Children)
    {
      if (!asset.PathVirtual.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        continue;
      string file = asset.PathVirtual[(Folder.Length + 1)..];
      using JsonDocument document = Parse(asset.Data, file);
      loaded.Add(ReadCampaign(document.RootElement, file));
    }
    if (loaded.Count == 0)
      throw new TrialFileException($"Folder '{Folder}' holds no campaign file");

    List<Campaign> campaigns = loaded
      .OrderBy(entry => entry.Order)
      .ThenBy(entry => entry.Campaign.Id, StringComparer.Ordinal)
      .Select(entry => entry.Campaign)
      .ToList();
    int trials = campaigns.Sum(campaign => campaign.Trials.Count);
    Logger.Info(LogTag, $"loaded {campaigns.Count} campaigns, {trials} trials");
    return campaigns;
  }

  public static Campaign ParseCampaign(string text, string source)
  {
    using JsonDocument document = Parse(Encoding.UTF8.GetBytes(text), source);
    return ReadCampaign(document.RootElement, source).Campaign;
  }

  private static JsonDocument Parse(byte[] bytes, string file)
  {
    try
    {
      return JsonDocument.Parse(bytes);
    }
    catch (JsonException e)
    {
      throw new TrialFileException($"{file}: not valid JSON, {e.Message}");
    }
  }

  private static (Campaign Campaign, int Order) ReadCampaign(JsonElement element, string file)
  {
    Object(element, file);
    string id = String(element, "id", file);
    string at = $"{file}: campaign '{id}'";
    JsonElement trials = Field(element, "trials", at);
    if (trials.ValueKind != JsonValueKind.Array || trials.GetArrayLength() == 0)
      throw new TrialFileException($"{at}, field 'trials': expected a non-empty array");
    List<Trial> list = [];
    int index = 0;
    foreach (JsonElement trial in trials.EnumerateArray())
      list.Add(ReadTrial(trial, $"{at}, trial [{index++}]", file));
    Campaign campaign = new()
    {
      Id = id,
      Name = String(element, "name", at),
      Description = OptionalString(element, "description", at) ?? "",
      Trials = list,
    };
    return (campaign, OptionalInt(element, "order", at) ?? 0);
  }

  private static Trial ReadTrial(JsonElement element, string where, string file)
  {
    Object(element, where);
    string id = String(element, "id", where);
    string at = $"{file}: trial '{id}'";
    string difficultyName = String(element, "difficulty", at);
    if (!DifficultyNames.TryGetValue(difficultyName, out Difficulty difficulty))
      throw new TrialFileException($"{at}, field 'difficulty': expected Normal, Hard or Expert");
    JsonElement goals = Field(element, "goals", at);
    Object(goals, $"{at}, field 'goals'");
    return new Trial
    {
      Id = id,
      Name = String(element, "name", at),
      Difficulty = difficulty,
      ReplayLimitSeconds =
        OptionalInt(element, "replayLimitSeconds", at) ?? Trial.DefaultReplayLimitSeconds,
      PreviousTrialHashes = ReadPreviousTrialHashes(element, at),
      Run = ReadRun(Field(element, "run", at), at),
      Goals = GoalTimes.FromSeconds(
        bronze: Seconds(goals, "bronze", at),
        silver: Seconds(goals, "silver", at),
        gold: Seconds(goals, "gold", at),
        diamond: Seconds(goals, "diamond", at),
        grandmaster: Seconds(goals, "grandmaster", at)
      ),
    };
  }

  private static List<byte[]> ReadPreviousTrialHashes(JsonElement trial, string at)
  {
    const string key = "previousTrialHashes";
    if (!trial.TryGetProperty(key, out JsonElement array) || array.ValueKind == JsonValueKind.Null)
      return [];

    if (array.ValueKind != JsonValueKind.Array)
      throw new TrialFileException($"{at}, field '{key}': expected an array");

    List<byte[]> hashes = [];
    foreach (JsonElement element in array.EnumerateArray())
    {
      string text = element.ValueKind == JsonValueKind.String ? element.GetString() : "";
      bool isShortHash =
        text.Length == HeaderCoding.TrialHashBytes * 2 && text.All(char.IsAsciiHexDigitLower);
      if (!isShortHash)
        throw new TrialFileException(
          $"{at}, field '{key}[{hashes.Count}]': expected 16 lowercase hex characters"
        );
      hashes.Add(Convert.FromHexString(text));
    }
    return hashes;
  }

  private static RunDefinition ReadRun(JsonElement run, string at)
  {
    Object(run, $"{at}, field 'run'");
    string modeName = OptionalString(run, "mode", at) ?? "Normal";
    if (!Modes.TryGetValue(modeName, out AreaMode mode))
      throw new TrialFileException($"{at}, field 'run.mode': expected Normal, BSide or CSide");
    string facingName = OptionalString(run, "spawnFacing", at);
    Facings? facing = null;
    if (facingName != null)
    {
      if (!FacingNames.TryGetValue(facingName, out Facings parsed))
        throw new TrialFileException(
          $"{at}, field 'run.spawnFacing': expected Left, Right or null"
        );
      facing = parsed;
    }
    string kindName = OptionalString(run, "finishKind", at) ?? "zone";
    if (!FinishKinds.TryGetValue(kindName, out FinishKind kind))
      throw new TrialFileException(
        $"{at}, field 'run.finishKind': expected zone, heart or roomEnter"
      );
    Rectangle finishRect = Rectangle.Empty;
    bool finishSet =
      run.TryGetProperty("finish", out JsonElement finishValue)
      && finishValue.ValueKind != JsonValueKind.Null;
    if (kind == FinishKind.Zone || (kind == FinishKind.RoomEnter && finishSet))
    {
      int[] finish = Integers(Field(run, "finish", at), 4, $"{at}, field 'run.finish'");
      finishRect = new Rectangle(finish[0], finish[1], finish[2], finish[3]);
    }
    else if (finishSet)
      throw new TrialFileException($"{at}, field 'run.finish': a heart goal has no rectangle");
    int? dashes = OptionalInt(run, "dashes", at);
    if (dashes is < 0 or > 2)
      throw new TrialFileException($"{at}, field 'run.dashes': expected 0, 1, 2 or null");
    int gems = OptionalInt(run, "requiredSummitGems", at) ?? 0;
    if (gems is < 0 or > 6)
      throw new TrialFileException($"{at}, field 'run.requiredSummitGems': expected 0 to 6");
    string coreModeName = OptionalString(run, "coreMode", at);
    Session.CoreModes? coreMode = null;
    if (coreModeName != null)
    {
      if (!CoreModeNames.TryGetValue(coreModeName, out Session.CoreModes parsedMode))
        throw new TrialFileException($"{at}, field 'run.coreMode': expected Hot, Cold or null");
      coreMode = parsedMode;
    }
    string introName = OptionalString(run, "introType", at);
    Player.IntroTypes intro = Player.IntroTypes.Respawn;
    if (introName != null && !IntroNames.TryGetValue(introName, out intro))
      throw new TrialFileException(
        $"{at}, field 'run.introType': expected {string.Join(", ", IntroNames.Keys)} or null"
      );
    return new RunDefinition
    {
      Sid = String(run, "sid", at),
      Mode = mode,
      StartRoom = String(run, "startRoom", at),
      Spawn = Vector(Field(run, "spawn", at), $"{at}, field 'run.spawn'"),
      SpawnFacing = facing,
      FinishRoom = String(run, "finishRoom", at),
      FinishKind = kind,
      Finish = finishRect,
      Waypoints = List(run, "waypoints", at, ReadWaypoint),
      CameraOffsets = List(
        run,
        "cameraOffsets",
        at,
        (element, path) =>
          new CameraOffsetDef(
            String(element, "room", path),
            Vector(Field(element, "offset", path), $"{path}.offset")
          )
      ),
      BloomBaseAdd = OptionalFloat(run, "bloomBaseAdd", at),
      LightingAlphaAdd = OptionalFloat(run, "lightingAlphaAdd", at),
      IgnoredStrawberries = List(
        run,
        "ignoredStrawberries",
        at,
        (element, path) => new EntityID(String(element, "room", path), Int(element, "id", path))
      ),
      RequiredStrawberries = OptionalInt(run, "requiredStrawberries", at) ?? 0,
      RequiredSummitGems = gems,
      Dashes = dashes,
      Grabless = OptionalBool(run, "grabless", at) ?? false,
      NoRefills = OptionalBool(run, "noRefills", at) ?? false,
      CoreMode = coreMode,
      DreamDash = OptionalBool(run, "dreamDash", at),
      IntroType = intro,
      SplitCategories = ReadSplitCategories(run, at),
      SplitRects = List(run, "splitRects", at, ReadSplitRect),
    };
  }

  private static WaypointDef ReadWaypoint(JsonElement element, string where)
  {
    Vector2? spawn = null;
    if (
      element.TryGetProperty("spawn", out JsonElement spawnElement)
      && spawnElement.ValueKind != JsonValueKind.Null
    )
      spawn = Vector(spawnElement, $"{where}.spawn");
    return new WaypointDef(
      String(element, "room", where),
      Vector(Field(element, "position", where), $"{where}.position"),
      spawn,
      OptionalBool(element, "setsSpawn", where) ?? true
    );
  }

  private static IReadOnlyList<SplitCategory> ReadSplitCategories(JsonElement run, string at)
  {
    const string key = "splitCategories";
    if (!run.TryGetProperty(key, out JsonElement array) || array.ValueKind == JsonValueKind.Null)
      return [];
    string failure =
      $"{at}, field 'run.{key}': expected an array of {string.Join(", ", SplitCategoryNames.Keys)}";
    if (array.ValueKind != JsonValueKind.Array)
      throw new TrialFileException(failure);
    HashSet<SplitCategory> categories = [];
    foreach (JsonElement element in array.EnumerateArray())
    {
      if (
        element.ValueKind != JsonValueKind.String
        || !SplitCategoryNames.TryGetValue(element.GetString(), out SplitCategory category)
      )
        throw new TrialFileException(failure);
      categories.Add(category);
    }
    return [.. categories];
  }

  private static SplitRect ReadSplitRect(JsonElement element, string where)
  {
    int[] rect = Integers(Field(element, "rect", where), 4, $"{where}.rect");
    if (rect[2] <= 0 || rect[3] <= 0)
      throw new TrialFileException($"{where}.rect: expected a width and a height greater than 0");
    return new SplitRect(
      String(element, "room", where),
      new Rectangle(rect[0], rect[1], rect[2], rect[3])
    );
  }

  private static List<T> List<T>(
    JsonElement parent,
    string key,
    string at,
    Func<JsonElement, string, T> read
  )
  {
    if (!parent.TryGetProperty(key, out JsonElement array) || array.ValueKind == JsonValueKind.Null)
      return [];
    if (array.ValueKind != JsonValueKind.Array)
      throw new TrialFileException($"{at}, field 'run.{key}': expected an array");
    List<T> list = [];
    int index = 0;
    foreach (JsonElement element in array.EnumerateArray())
    {
      string where = $"{at}, field 'run.{key}[{index++}]'";
      Object(element, where);
      list.Add(read(element, where));
    }
    return list;
  }

  private static void Object(JsonElement element, string where)
  {
    if (element.ValueKind != JsonValueKind.Object)
      throw new TrialFileException($"{where}: expected an object");
  }

  private static JsonElement Field(JsonElement parent, string key, string at)
  {
    if (!parent.TryGetProperty(key, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
      throw new TrialFileException($"{at}, field '{key}': missing");
    return value;
  }

  private static string String(JsonElement parent, string key, string at)
  {
    JsonElement value = Field(parent, key, at);
    if (value.ValueKind != JsonValueKind.String || value.GetString().Length == 0)
      throw new TrialFileException($"{at}, field '{key}': expected a non-empty string");
    return value.GetString();
  }

  private static string OptionalString(JsonElement parent, string key, string at)
  {
    if (!parent.TryGetProperty(key, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
      return null;
    if (value.ValueKind != JsonValueKind.String)
      throw new TrialFileException($"{at}, field '{key}': expected a string or null");
    return value.GetString();
  }

  private const double MaxMagnitude = 1_000_000;

  private static float Number(JsonElement value, string where)
  {
    if (
      value.ValueKind != JsonValueKind.Number
      || !value.TryGetDouble(out double number)
      || Math.Abs(number) > MaxMagnitude
    )
      throw new TrialFileException($"{where}: expected a number within ±{MaxMagnitude:F0}");
    return (float)number;
  }

  private static int RequireInRange(int number, string where) =>
    Math.Abs(number) <= MaxMagnitude
      ? number
      : throw new TrialFileException($"{where}: expected a number within ±{MaxMagnitude:F0}");

  private static int[] Integers(JsonElement value, int count, string where)
  {
    if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != count)
      throw new TrialFileException($"{where}: expected {count} whole numbers");
    int[] numbers = new int[count];
    int index = 0;
    foreach (JsonElement element in value.EnumerateArray())
    {
      if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out numbers[index]))
        throw new TrialFileException($"{where}: expected {count} whole numbers");
      RequireInRange(numbers[index++], where);
    }
    return numbers;
  }

  private static float[] Numbers(JsonElement value, int count, string where)
  {
    if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != count)
      throw new TrialFileException($"{where}: expected {count} numbers");
    float[] numbers = new float[count];
    int index = 0;
    foreach (JsonElement element in value.EnumerateArray())
      numbers[index++] = Number(element, where);
    return numbers;
  }

  private static Vector2 Vector(JsonElement value, string where)
  {
    float[] xy = Numbers(value, 2, where);
    return new Vector2(xy[0], xy[1]);
  }

  private static int Int(JsonElement parent, string key, string at)
  {
    JsonElement value = Field(parent, key, at);
    if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int number))
      throw new TrialFileException($"{at}, field '{key}': expected a whole number");
    return RequireInRange(number, $"{at}, field '{key}'");
  }

  private static int? OptionalInt(JsonElement parent, string key, string at)
  {
    if (!parent.TryGetProperty(key, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
      return null;
    return Int(parent, key, at);
  }

  private static float? OptionalFloat(JsonElement parent, string key, string at)
  {
    if (!parent.TryGetProperty(key, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
      return null;
    return Number(value, $"{at}, field '{key}'");
  }

  private static bool? OptionalBool(JsonElement parent, string key, string at)
  {
    if (!parent.TryGetProperty(key, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
      return null;
    if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
      throw new TrialFileException($"{at}, field '{key}': expected true or false");
    return value.GetBoolean();
  }

  private static double Seconds(JsonElement goals, string key, string at)
  {
    JsonElement value = Field(goals, key, at);
    if (
      value.ValueKind != JsonValueKind.Number
      || !value.TryGetDouble(out double seconds)
      || seconds <= 0
    )
      throw new TrialFileException($"{at}, field 'goals.{key}': expected seconds above 0");
    return seconds;
  }
}
