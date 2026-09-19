using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Celeste.Mod.Helpers;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Online;

public enum ServerError
{
  None,
  Offline,
  UnknownClient,
  UpdateRequired,
  NameTaken,
  NameInvalid,
  NameReserved,
  Rejected,
  RateLimited,
  Banned,
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

public static class ServerClient
{
  private const string LogTag = "ClimbTrials/Server";
  private const double TimeoutSeconds = 5;
  private const string VersionHeader = "X-ClimbTrials-Version";
  private const string ClientIdHeader = "X-Client-Id";
  private const string PlayerNameHeader = "X-Player-Name";
  private const string UrlRefused = "server URL refused";
  private const string OfflineModeMessage = "offline mode";

  private static readonly HttpClient http = Build();

  private const int PassiveIconDelayMs = 300;
  private static int pending;
  private static bool iconShown;
  private static readonly JsonSerializerOptions json = new() { PropertyNameCaseInsensitive = true };

  private static ClimbTrialsModuleSettings Settings => ClimbTrialsModule.Settings;

  private sealed class ErrorBody
  {
    public string Error { get; set; }
    public string Message { get; set; }
  }

  private static HttpClient Build()
  {
    HttpClient client = new CompressedHttpClient { Timeout = TimeSpan.FromSeconds(TimeoutSeconds) };
    string version = ClimbTrialsModule.Instance.Metadata.Version.ToString();
    client.DefaultRequestHeaders.Add(VersionHeader, version);
    return client;
  }

  public static void Load()
  {
    if (!TryBaseUri(out _))
      Logger.Log(
        LogLevel.Warn,
        LogTag,
        $"ServerUrl '{Settings.ServerUrl}' refused: https, or http on localhost only"
      );
  }

  public static void Register(Action<ServerResult<Registration>> onDone) =>
    Send("/v1/register", HttpMethod.Post, Json("{}"), onDone);

  public static void Rename(string name, Action<ServerResult<RenameAnswer>> onDone) =>
    Send("/v1/me/name", HttpMethod.Put, Json(JsonSerializer.Serialize(new { name })), onDone);

  public static void GetStandings(
    string campaignId,
    Action<ServerResult<CampaignStandings>> onDone
  ) => Send($"/v1/campaigns/{campaignId}/standings", HttpMethod.Get, null, onDone, passive: true);

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

  public static void GetCampaignTotals(Action<ServerResult<CampaignTotals>> onDone) =>
    Send("/v1/campaigns/totals", HttpMethod.Get, null, onDone, passive: true);

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

  private static StringContent Json(string text) => new(text, Encoding.UTF8, "application/json");

  private static bool TryBaseUri(out Uri uri) =>
    Uri.TryCreate(Settings.ServerUrl, UriKind.Absolute, out uri)
    && (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback));

  private static void Send<T>(
    string path,
    HttpMethod method,
    HttpContent body,
    Action<ServerResult<T>> onDone,
    bool passive = false,
    Func<HttpResponseMessage, Task<T>> read = null
  )
  {
    if (Settings.OfflineMode)
    {
      Deliver(path, new ServerResult<T>(default, ServerError.Offline, OfflineModeMessage), onDone);
      return;
    }
    if (!TryBaseUri(out Uri baseUri))
    {
      Deliver(path, new ServerResult<T>(default, ServerError.Offline, UrlRefused), onDone);
      return;
    }

    string clientId = Identity.ClientId;
    Uri url = new(baseUri, path);

    BeginRequest(passive);

    Task.Run(async () =>
    {
      ServerResult<T> result = await Request(url, method, body, clientId, read ?? ReadJson<T>);
      await MainThreadHelper.Schedule(() =>
      {
        EndRequest();
        if (!result.Ok)
          Logger.Log(LogLevel.Info, LogTag, $"{path}: {result.Error}, {result.Message}");
        Deliver(path, result, onDone);
      });
    });
  }

  private static void BeginRequest(bool passive)
  {
    pending++;
    if (!passive)
    {
      ShowIconWhilePending();
      return;
    }
    Task.Run(async () =>
    {
      await Task.Delay(PassiveIconDelayMs);
      await MainThreadHelper.Schedule(ShowIconWhilePending);
    });
  }

  private static void ShowIconWhilePending()
  {
    if (pending == 0 || iconShown)
      return;
    iconShown = true;
    SaveLoadIcon.Show(Engine.Scene);
  }

  private static void EndRequest()
  {
    if (--pending > 0 || !iconShown)
      return;
    iconShown = false;
    SaveLoadIcon.Hide();
  }

  private static async Task<ServerResult<T>> Request<T>(
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
      using HttpResponseMessage response = await http.SendAsync(request);
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
      "unknown_client" => ServerError.UnknownClient,
      "update_required" => ServerError.UpdateRequired,
      "name_taken" => ServerError.NameTaken,
      "name_invalid" => ServerError.NameInvalid,
      "name_reserved" => ServerError.NameReserved,
      "rejected" => ServerError.Rejected,
      "rate_limited" => ServerError.RateLimited,
      "banned" => ServerError.Banned,
      _ => ServerError.Offline,
    };
    return new ServerResult<T>(default, code, error?.Message);
  }

  private static void Deliver<T>(
    string path,
    ServerResult<T> result,
    Action<ServerResult<T>> onDone
  )
  {
    if (result.Error == ServerError.UnknownClient)
      Identity.Forget();
    if (result.Error == ServerError.Banned)
      Identity.Banned = true;
    else if (result.Ok)
      Identity.Banned = false;
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
