using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays;

namespace Celeste.Mod.ClimbTrials.Online;

public enum UploadState
{
  None,
  Submitting,
  Accepted,
  Failed,
}

public readonly record struct Upload(UploadState State, int? Rank, string Message);

public static class Uploader
{
  private const int Tries = 3;
  private const int RetryDelayMs = 1000;

  private static readonly Dictionary<string, Upload> uploads = [];
  private static readonly Dictionary<string, int> versions = [];

  public static Upload UploadFor(string trialId) => uploads.GetValueOrDefault(trialId);

  public static void Start(Trial trial, Replay replay, Action<Upload> report)
  {
    int version = versions.GetValueOrDefault(trial.Id) + 1;
    versions[trial.Id] = version;
    bool built = Submission.TryBuild(
      replay,
      trial,
      out Submission submission,
      out ValidationResult check
    );

    if (!built)
    {
      StoreAndReport(trial.Id, new Upload(UploadState.Failed, null, check.Describe()), report);
      return;
    }

    StoreAndReport(trial.Id, new Upload(UploadState.Submitting, null, null), report);
    TryToPublish(trial.Id, submission.ReplayBytes, attempt: 1, version, report);
  }

  public static (string Text, bool Warning) Describe(Upload upload) =>
    upload.State switch
    {
      UploadState.Submitting => (Dialog.Clean("climbtrials_submitting"), false),
      UploadState.Accepted => (Dialog.Clean("climbtrials_submitted"), false),
      _ => (upload.Message, true),
    };

  private static void StoreAndReport(string trialId, Upload upload, Action<Upload> report)
  {
    uploads[trialId] = upload;
    report(upload);
  }

  private static void TryToPublish(
    string trialId,
    byte[] bytes,
    int attempt,
    int version,
    Action<Upload> report
  )
  {
    ServerClient.Submit(
      trialId,
      bytes,
      result =>
      {
        if (versions[trialId] != version)
          return;

        if (result.Ok)
        {
          Standings.Replace(result.Value.Standing);
          StoreAndReport(
            trialId,
            new Upload(UploadState.Accepted, result.Value.Standing?.Rank, null),
            report
          );
          return;
        }

        if (result.Error == ServerError.Offline && attempt < Tries)
        {
          Task.Delay(RetryDelayMs)
            .ContinueWith(_ =>
              MainThreadHelper.Schedule(() =>
                TryToPublish(trialId, bytes, attempt + 1, version, report)
              )
            );
          return;
        }

        StoreAndReport(trialId, new Upload(UploadState.Failed, null, MessageFor(result)), report);
      }
    );
  }

  private static string MessageFor(ServerResult<SubmissionAnswer> result) =>
    result.Error switch
    {
      ServerError.Rejected when result.Message != null => result.Message,
      ServerError.UpdateRequired => Dialog.Clean("climbtrials_update_required"),
      ServerError.Banned => Dialog.Clean("climbtrials_submit_banned"),
      _ => Dialog.Clean("climbtrials_submit_failed"),
    };
}
