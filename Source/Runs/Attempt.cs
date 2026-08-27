using System;

namespace Celeste.Mod.ClimbTrials.Runs;

public enum AttemptState { Idle, Countdown, Running, Finished }

public sealed class Attempt
{
  public AttemptState State { get; private set; } = AttemptState.Idle;
  public long ElapsedTicks { get; private set; }

  public void BeginCountdown()
  {
    State = AttemptState.Countdown;
  }

  public void Start()
  {
    ElapsedTicks = 0;
    State = AttemptState.Running;
  }

  public void Finish()
  {
    State = AttemptState.Finished;
  }

  public void Tick(float rawDeltaSeconds)
  {
    if (State == AttemptState.Running)
      ElapsedTicks += SecondsToTicks(rawDeltaSeconds);
  }

  private static long SecondsToTicks(float seconds)
  {
    return ((long)(seconds * 1000 + 0.5)) * TimeSpan.TicksPerMillisecond;
  }

  public void Reset()
  {
    State = AttemptState.Idle;
  }

  public static string Format(long ticks)
  {
    TimeSpan t = TimeSpan.FromTicks(ticks);
    return t.ToString(t.TotalSeconds < 60 ? "s\\.fff" : "m\\:ss\\.fff");
  }
}