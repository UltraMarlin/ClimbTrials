using System.Collections.Generic;
using Celeste.Mod.ClimbTrials.Runs;

namespace Celeste.Mod.ClimbTrials.Content;

public static class TrialText
{
  public static string Modifiers(RunDefinition run)
  {
    List<string> parts = [];
    if (run.Grabless)
      parts.Add(Dialog.Clean("climbtrials_mod_grabless"));
    if (run.Dashes is int dashes)
      parts.Add(
        Count(dashes, "climbtrials_mod_dashless", "climbtrials_mod_dash", "climbtrials_mod_dashes")
      );
    return string.Join(", ", parts);
  }

  public static string Description(RunDefinition run)
  {
    List<string> parts = [];
    if (run.HasModifiers)
      parts.Add(Modifiers(run));
    if (run.Waypoints.Count > 0)
      parts.Add(
        Count(run.Waypoints.Count, null, "climbtrials_req_waypoint", "climbtrials_req_waypoints")
      );
    if (run.RequiredStrawberries > 0)
      parts.Add(
        Count(
          run.RequiredStrawberries,
          null,
          "climbtrials_req_strawberry",
          "climbtrials_req_strawberries"
        )
      );
    parts.Add(Dialog.Clean("climbtrials_req_goal"));
    return string.Join(". ", parts) + ".";
  }

  public static string Count(int n, string zeroKey, string oneKey, string manyKey)
  {
    if (n == 0 && zeroKey != null)
      return Dialog.Clean(zeroKey);
    if (n == 1)
      return Dialog.Clean(oneKey);
    return Dialog.Get(manyKey).Replace("{0}", n.ToString());
  }
}
