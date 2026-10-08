using Godot;

namespace Resonance.Game;

/// <summary>
/// Loads saved settings and installs the crash log before the main scene starts.
/// </summary>
public partial class PlaytestHost : Node
{
    public override void _Ready()
    {
        RunLog.InstallCrashHandler();
        PlaytestSettings.Load();
        if (!GreyBoxBattle.LayoutLockRequested(out _, out _))
        {
            PlaytestSettings.Apply(GetWindow());
        }
    }
}
