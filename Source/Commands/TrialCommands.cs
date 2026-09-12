using System.IO;
using System.Linq;
using System.Text.Json;
using Celeste.Mod.ClimbTrials.Content;
using Celeste.Mod.ClimbTrials.Replays;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Commands;

public static class TrialCommands
{
  private const string LogTag = "ClimbTrials/Trials";

  private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

  [Command(
    "ct_trials_json",
    "ClimbTrials: write the trial table for the server next to the replays"
  )]
  public static void WriteTrialsJson()
  {
    var trials = Campaigns
      .All.SelectMany(campaign =>
        campaign.Trials.Select(trial => new
        {
          id = trial.Id,
          campaignId = campaign.Id,
          spawnX = trial.Run.Spawn.X,
          spawnY = trial.Run.Spawn.Y,
          replayLimitSeconds = trial.ReplayLimitSeconds,
        })
      )
      .ToList();
    Directory.CreateDirectory(ReplayStore.Folder);
    string path = Path.Combine(ReplayStore.Folder, "trials.json");
    File.WriteAllText(path, JsonSerializer.Serialize(trials, JsonOptions));
    ConsoleOutput.Log(LogTag, $"wrote {trials.Count} trials to {path}");
  }
}
