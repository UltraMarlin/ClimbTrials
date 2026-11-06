using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Celeste.Mod.ClimbTrials.Replays;
using Celeste.Mod.ClimbTrials.UI;
using Celeste.Mod.Helpers;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Online;

public enum ServerError
{
  None,
  Offline,
  PlayerDeleted,
  UpdateRequired,
  NameTaken,
  NameInvalid,
  NameReserved,
  Rejected,
  RateLimited,
  Banned,
  SubmissionsPaused,
  EventClosed,
  NotInvited,
  NotFound,
  CodeInvalid,
  CodeUsedUp,
  NotAdmin,
  Refused,
}

public readonly record struct ServerResult<T>(T Value, ServerError Error, string Message)
{
  public bool Ok => Error == ServerError.None;
}

public sealed class DownloadedReplay
{
  public byte[] Bytes { get; init; }
  public string OwnerName { get; init; }
}

public sealed class AdminReplays
{
  public List<AdminReplay> Replays { get; set; } = [];
  public int NotVisible { get; set; }
  public int WithoutReplay { get; set; }
}

public sealed class AdminReplay
{
  public string TrialId { get; set; }
  public int? Rank { get; set; }
  public string Player { get; set; }
  public long Ticks { get; set; }
  public string Bytes { get; set; }
}

public static class ServerClient
{
  private const string LogTag = "ClimbTrials/Server";
  private const double TimeoutSeconds = 5;
  private const double AdminReplaysTimeoutSeconds = 120;
  private const string VersionHeader = "X-ClimbTrials-Version";
  private const string ClientIdHeader = "X-Client-Id";
  private const string PlayerNameHeader = "X-Player-Name";
  private const string UrlRefused = "server URL refused";
  private const string OfflineModeMessage = "offline mode";
  private const string BuiltInServerUrl = "https://climb-trials-server.mabidev.de";

  private static string ResolvedServerUrl =>
    string.IsNullOrEmpty(Settings.ServerUrl) ? BuiltInServerUrl : Settings.ServerUrl;

  private static readonly HttpClient http = Build(TimeoutSeconds);
  private static readonly HttpClient adminReplaysHttp = Build(AdminReplaysTimeoutSeconds);

  private const int PassiveIconDelayMs = 300;
  private static int loadsShowingIcon;
  private static bool iconSuppressed;
  private static LoadingIcon icon;
  private static readonly JsonSerializerOptions json = new()
  {
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
  };

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  private sealed class ErrorBody
  {
    public string Error { get; set; }
    public string Message { get; set; }
  }

  private static HttpClient Build(double timeoutSeconds)
  {
    HttpClient client = new CompressedHttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
    string version = ClimbTrialsModule.Instance.Metadata.Version.ToString();
    client.DefaultRequestHeaders.Add(VersionHeader, version);
    return client;
  }

  public static void Load()
  {
    if (TryBaseUri(out _))
      Logger.Log(
        LogLevel.Info,
        LogTag,
        $"server {ResolvedServerUrl}, offline mode {Settings.OfflineMode}"
      );
    else
      Logger.Log(
        LogLevel.Warn,
        LogTag,
        $"ServerUrl '{ResolvedServerUrl}' refused: https, or http on localhost only"
      );
  }

  public static void Register(Action<ServerResult<Registration>> onDone) =>
    Send("/v1/register", HttpMethod.Post, Json("{}"), onDone);

  public static void Rename(string name, Action<ServerResult<RenameAnswer>> onDone) =>
    Send("/v1/me/name", HttpMethod.Put, Json(JsonSerializer.Serialize(new { name })), onDone);

  public static void JoinEvent(string code, Action<ServerResult<JoinAnswer>> onDone) =>
    Send("/v1/events/join", HttpMethod.Post, Json(JsonSerializer.Serialize(new { code })), onDone);

  public static void GetHub(
    IEnumerable<(string Id, string Hash)> heldEvents,
    Action<ServerResult<HubAnswer>> onDone
  )
  {
    string body = JsonSerializer.Serialize(
      new { have = heldEvents.Select(held => new { id = held.Id, hash = held.Hash }) }
    );
    Send("/v1/hub", HttpMethod.Post, Json(body), onDone, passive: true);
  }

  public static void GetRecords(string campaignId, Action<ServerResult<CampaignRecords>> onDone) =>
    Send($"/v1/campaigns/{campaignId}/records", HttpMethod.Get, null, onDone, passive: true);

  public static void GetTrialStanding(string trialId, Action<ServerResult<TrialStanding>> onDone) =>
    Send($"/v1/trials/{trialId}/standing", HttpMethod.Get, null, onDone, passive: true);

  public static void GetBoard(
    string trialId,
    int offset,
    int limit,
    Action<ServerResult<Board>> onDone
  ) =>
    Send(
      $"/v1/trials/{trialId}/board?offset={offset}&limit={limit}",
      HttpMethod.Get,
      null,
      onDone,
      passive: true
    );

  public static void GetCampaignBoard(
    string campaignId,
    int offset,
    int limit,
    Action<ServerResult<Board>> onDone
  ) =>
    Send(
      $"/v1/campaigns/{campaignId}/board?offset={offset}&limit={limit}",
      HttpMethod.Get,
      null,
      onDone,
      passive: true
    );

  public static void Download(string replayId, Action<ServerResult<DownloadedReplay>> onDone) =>
    Send($"/v1/replays/{replayId}", HttpMethod.Get, null, onDone, read: ReadReplay);

  public static void Submit(
    string trialId,
    byte[] replay,
    Action<ServerResult<SubmissionAnswer>> onDone
  )
  {
    ByteArrayContent body = new(replay);
    body.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
    Send($"/v1/trials/{trialId}/submissions", HttpMethod.Post, body, onDone);
  }

  public static void SubmitTime(
    string trialId,
    TimeSubmission time,
    Action<ServerResult<SubmissionAnswer>> onDone
  )
  {
    string body = JsonSerializer.Serialize(
      new
      {
        ticks = time.Ticks,
        achievedAtUtcTicks = time.AchievedAtUtcTicks,
        trialHash = time.TrialHash,
      }
    );
    Send($"/v1/trials/{trialId}/times", HttpMethod.Post, Json(body), onDone);
  }

  public static void GetAdminReplays(
    string scope,
    string id,
    int? maxRank,
    Action<ServerResult<AdminReplays>> onDone
  )
  {
    string body = JsonSerializer.Serialize(
      new
      {
        scope,
        id,
        maxRank,
      }
    );
    Send("/v1/admin/replays", HttpMethod.Post, Json(body), onDone, client: adminReplaysHttp);
  }

  private static StringContent Json(string text) => new(text, Encoding.UTF8, "application/json");

  private static bool TryBaseUri(out Uri uri) =>
    Uri.TryCreate(ResolvedServerUrl, UriKind.Absolute, out uri)
    && (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback));

  private static void Send<T>(
    string path,
    HttpMethod method,
    HttpContent body,
    Action<ServerResult<T>> onDone,
    bool passive = false,
    Func<HttpResponseMessage, Task<T>> read = null,
    HttpClient client = null
  )
  {
    if (Settings.OfflineMode)
    {
      RunCallback(
        path,
        new ServerResult<T>(default, ServerError.Offline, OfflineModeMessage),
        onDone
      );
      return;
    }
    if (!TryBaseUri(out Uri baseUri))
    {
      RunCallback(path, new ServerResult<T>(default, ServerError.Offline, UrlRefused), onDone);
      return;
    }

    string clientId = Identity.ClientId;
    Uri url = new(baseUri, path);

    bool showsIcon = BeginRequest(passive);

    Task.Run(async () =>
    {
      ServerResult<T> result = await Request(
        client ?? http,
        url,
        method,
        body,
        clientId,
        read ?? ReadJson<T>
      );
      await MainThreadHelper.Schedule(() =>
      {
        if (showsIcon)
          ReleaseIcon();
        if (!result.Ok)
          Logger.Log(LogLevel.Info, LogTag, $"{path}: {result.Error}, {result.Message}");
        Identity.UpdateFromAnswer(result.Error, clientId);
        RunCallback(path, result, onDone);
      });
    });
  }

  public static void RunWithoutIcon(Action startRequests)
  {
    bool suppressedBefore = iconSuppressed;
    iconSuppressed = true;
    try
    {
      startRequests();
    }
    finally
    {
      iconSuppressed = suppressedBefore;
    }
  }

  private static bool BeginRequest(bool passive)
  {
    if (iconSuppressed)
      return false;
    loadsShowingIcon++;
    if (!passive)
    {
      ShowIconWhileLoading();
      return true;
    }
    Task.Run(async () =>
    {
      await Task.Delay(PassiveIconDelayMs);
      await MainThreadHelper.Schedule(ShowIconWhileLoading);
    });
    return true;
  }

  public static void HoldIcon()
  {
    loadsShowingIcon++;
    ShowIconWhileLoading();
  }

  public static void ReleaseIcon()
  {
    if (--loadsShowingIcon > 0)
      return;
    icon?.Hide();
  }

  private static void ShowIconWhileLoading()
  {
    if (loadsShowingIcon == 0 || icon?.TryToKeep() == true)
      return;
    icon = new LoadingIcon();
    (WatcherOverlay.Scene ?? Engine.Scene).Add(icon);
  }

  private static async Task<ServerResult<T>> Request<T>(
    HttpClient client,
    Uri url,
    HttpMethod method,
    HttpContent body,
    string clientId,
    Func<HttpResponseMessage, Task<T>> read
  )
  {
    try
    {
      using HttpRequestMessage request = new(method, url) { Content = body };
      if (clientId != null)
        request.Headers.Add(ClientIdHeader, clientId);
      using HttpResponseMessage response = await client.SendAsync(request);
      if (!response.IsSuccessStatusCode)
        return ReadError<T>(await response.Content.ReadAsStringAsync());

      T value = await read(response);
      if (value == null)
        return new ServerResult<T>(default, ServerError.Offline, "empty answer");

      return new ServerResult<T>(value, ServerError.None, null);
    }
    catch (Exception e)
    {
      return new ServerResult<T>(default, ServerError.Offline, e.Message);
    }
  }

  private static async Task<T> ReadJson<T>(HttpResponseMessage response) =>
    JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync(), json);

  private static async Task<DownloadedReplay> ReadReplay(HttpResponseMessage response) =>
    new()
    {
      Bytes = await response.Content.ReadAsByteArrayAsync(),
      OwnerName = response.Headers.TryGetValues(PlayerNameHeader, out IEnumerable<string> values)
        ? values.FirstOrDefault()
        : null,
    };

  private static ServerResult<T> ReadError<T>(string text)
  {
    ErrorBody error = null;
    try
    {
      error = JsonSerializer.Deserialize<ErrorBody>(text, json);
    }
    catch (JsonException) { }
    ServerError code = error?.Error switch
    {
      "player_deleted" => ServerError.PlayerDeleted,
      "update_required" => ServerError.UpdateRequired,
      "name_taken" => ServerError.NameTaken,
      "name_invalid" => ServerError.NameInvalid,
      "name_reserved" => ServerError.NameReserved,
      "rejected" => ServerError.Rejected,
      "rate_limited" => ServerError.RateLimited,
      "banned" => ServerError.Banned,
      "submissions_paused" => ServerError.SubmissionsPaused,
      "event_closed" => ServerError.EventClosed,
      "not_invited" => ServerError.NotInvited,
      "not_found" => ServerError.NotFound,
      "code_invalid" => ServerError.CodeInvalid,
      "code_used_up" => ServerError.CodeUsedUp,
      "not_admin" => ServerError.NotAdmin,
      "payload_too_large" or "bad_request" => ServerError.Refused,
      _ => ServerError.Offline,
    };
    return new ServerResult<T>(default, code, error?.Message);
  }

  private static void RunCallback<T>(
    string path,
    ServerResult<T> result,
    Action<ServerResult<T>> onDone
  )
  {
    try
    {
      onDone(result);
    }
    catch (Exception e)
    {
      Logger.Log(LogLevel.Error, LogTag, $"{path}: callback threw {e}");
    }
  }
}
