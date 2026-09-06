namespace Celeste.Mod.ClimbTrials.Online;

public enum ReportReason
{
  Desynced,
}

public sealed class Report
{
  public string ReporterClientId { get; init; }
  public string ReporterKey { get; init; }
  public string ReplayId { get; init; }
  public ReportReason Reason { get; init; }
}
