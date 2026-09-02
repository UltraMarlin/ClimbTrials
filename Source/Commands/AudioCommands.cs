using FMOD.Studio;
using Monocle;

namespace Celeste.Mod.ClimbTrials.Commands;

public static class AudioCommands
{
  private static EventInstance last;

  [Command("ct_sfx", "ClimbTrials: play an FMOD event. ct_sfx <path or name after event:/>")]
  public static void Play(string path)
  {
    if (string.IsNullOrWhiteSpace(path))
    {
      Engine.Commands.Log("usage: ct_sfx ui/main/button_select");
      return;
    }
    if (!path.StartsWith("event:/"))
      path = "event:/" + path;

    Stop();
    last = Audio.Play(path);
    Engine.Commands.Log(last == null ? $"no event '{path}'" : $"playing {path}");
  }

  [Command("ct_sfx_stop", "ClimbTrials: stop the event started by ct_sfx")]
  public static void Stop()
  {
    if (last == null)
      return;
    last.stop(STOP_MODE.ALLOWFADEOUT);
    last.release();
    last = null;
  }
}
