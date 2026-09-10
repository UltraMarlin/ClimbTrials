using Monocle;

namespace Celeste.Mod.ClimbTrials.Commands;

public static class ConsoleOutput
{
  public static bool TryGetLevel(out Level level)
  {
    level = Engine.Scene as Level;
    if (level != null)
      return true;
    Engine.Commands.Log("not in a level");
    return false;
  }

  public static void Log(string tag, string line)
  {
    Engine.Commands.Log(line);
    Logger.Log(LogLevel.Info, tag, line);
  }
}
