using System.Diagnostics;

namespace Resonance.Sim.Tests;

/// <summary>
/// The 3D boss HUD must keep the arena and every panel at one size while text,
/// ability selection, and the auto-run change. Godot runs the check headlessly.
/// </summary>
public class HudLayoutLockTests
{
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1920, 1080)]
    public void Panels_stay_fixed_while_the_boss_fight_advances(int width, int height)
    {
        string? godot = FindGodot();
        if (godot == null)
        {
            return;
        }

        string project = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Resonance.Game"));
        if (!File.Exists(Path.Combine(project, "project.godot")))
        {
            project = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "src", "Resonance.Game"));
        }

        Assert.True(File.Exists(Path.Combine(project, "project.godot")), $"Godot project not found from {project}");

        var start = new ProcessStartInfo
        {
            FileName = godot,
            Arguments = $"--headless --path \"{project}\" --quit-after 800",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.Environment["HUD_LOCK"] = $"{width}x{height}";
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";

        using Process process = Process.Start(start)!;
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        string combined = stdout + "\n" + stderr;
        Assert.True(process.ExitCode == 0, combined);
        Assert.Contains($"HUD_LOCK {width}x{height}", combined);
        Assert.Contains(" OK", combined);
        Assert.DoesNotContain(" FAIL", combined);
    }

    private static string? FindGodot()
    {
        string env = Environment.GetEnvironmentVariable("GODOT") ?? "";
        if (env.Length > 0 && File.Exists(env))
        {
            return env;
        }

        const string bundled = "/home/ubuntu/godot/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64";
        if (File.Exists(bundled))
        {
            return bundled;
        }

        return null;
    }
}
