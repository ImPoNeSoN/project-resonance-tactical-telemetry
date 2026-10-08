using Godot;

namespace Resonance.Game;

/// <summary>
/// Playtest 0.1 identity. The project setting is the source of the version string.
/// </summary>
public static class BuildStamp
{
    public const string FallbackVersion = "0.1.0";

    public static string Version
    {
        get
        {
            string version = ProjectSettings.GetSetting("application/config/version").AsString();
            return version.Length == 0 ? FallbackVersion : version;
        }
    }

    /// <summary>
    /// Editor runs and debug exports. A release export, which is what players unzip, is false.
    /// </summary>
    public static bool DevSession => OS.HasFeature("editor") || OS.IsDebugBuild();
}
