using System;
using System.IO;
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
  Rejected,
  RateLimited,
}

public readonly record struct ServerResult<T>(T Value, ServerError Error, string Message)
{
  public bool Ok => Error == ServerError.None;
}

public static class ServerClient
{
  private const string LogTag = "ClimbTrials/Server";
  private const double TimeoutSeconds = 2;
  private const string VersionHeader = "X-ClimbTrials-Version";
  private const string ClientIdHeader = "X-Client-Id";
  private const string UrlRefused = "server URL refused";

  private static readonly HttpClient http = Build();
  private static int pending;
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
  ) => Send($"/v1/campaigns/{campaignId}/standings", HttpMethod.Get, null, onDone);

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

  private static StringContent Json(string text) => new(text, Encoding.UTF8, "application/json");

  private static bool TryBaseUri(out Uri uri) =>
    Uri.TryCreate(Settings.ServerUrl, UriKind.Absolute, out uri)
    && (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback));

  private static void Send<T>(
    string path,
    HttpMethod method,
    HttpContent body,
    Action<ServerResult<T>> onDone
  )
  {
    if (!TryBaseUri(out Uri baseUri))
    {
      Deliver(path, new ServerResult<T>(default, ServerError.Offline, UrlRefused), onDone);
      return;
    }

    string clientId = Identity.ClientId;
    Uri url = new(baseUri, path);

    if (pending++ == 0)
      SaveLoadIcon.Show(Engine.Scene);

    Task.Run(async () =>
    {
      ServerResult<T> result = await Request<T>(url, method, body, clientId);
      await MainThreadHelper.Schedule(() =>
      {
        if (--pending == 0)
          SaveLoadIcon.Hide();
        Deliver(path, result, onDone);
      });
    });
  }

  private static async Task<ServerResult<T>> Request<T>(
    Uri url,
    HttpMethod method,
    HttpContent body,
    string clientId
  )
  {
    try
    {
      using HttpRequestMessage request = new(method, url) { Content = body };
      if (clientId != null)
        request.Headers.Add(ClientIdHeader, clientId);
      using HttpResponseMessage response = await http.SendAsync(request);
      string text = await response.Content.ReadAsStringAsync();
      if (!response.IsSuccessStatusCode)
        return ReadError<T>(text);

      T value = JsonSerializer.Deserialize<T>(text, json);
      if (value == null)
        return new ServerResult<T>(default, ServerError.Offline, "empty answer");

      return new ServerResult<T>(value, ServerError.None, null);
    }
    catch (Exception e)
    {
      return new ServerResult<T>(default, ServerError.Offline, e.Message);
    }
  }

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
      "rejected" => ServerError.Rejected,
      "rate_limited" => ServerError.RateLimited,
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
    if (!result.Ok)
      Logger.Log(LogLevel.Info, LogTag, $"{path}: {result.Error}, {result.Message}");
    if (result.Error == ServerError.UnknownClient)
      Identity.Forget();
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
