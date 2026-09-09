using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays;

namespace Celeste.Mod.ClimbTrials.Online;

public sealed class Submission
{
  public string ClientId { get; init; }
  public string Key { get; init; }
  public string ClimbTrialsVersion { get; init; }
  public byte[] ReplayBytes { get; init; }

  public static bool TryBuild(
    Replay replay,
    Trial trial,
    out Submission submission,
    out ValidationResult check
  )
  {
    submission = null;
    check = ReplayValidator.Check(replay, TrialSpec.From(trial), ModCheck.Allowed);
    if (!check.Passes)
      return false;
    submission = new Submission
    {
      ClientId = Identity.ClientId,
      Key = Identity.Key,
      ClimbTrialsVersion = ClimbTrialsModule.Instance.Metadata.Version.ToString(),
      ReplayBytes = replay.ToBytes(),
    };
    return true;
  }
}
