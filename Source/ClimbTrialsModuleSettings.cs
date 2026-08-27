using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.ClimbTrials;

public class ClimbTrialsModuleSettings : EverestModuleSettings
{
  [SettingRange(min: 1, max: 5)] public int CountdownSeconds { get; set; } = 3;
}