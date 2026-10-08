using System.Text;
using Godot;

namespace Resonance.Game;

/// <summary>
/// Playtest run JSON under user://runs/ and unhandled-exception files under user://crash/.
/// </summary>
public static class RunLog
{
    public static void InstallCrashHandler()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            try
            {
                WriteCrash(args.ExceptionObject?.ToString() ?? "unknown");
            }
            catch (Exception)
            {
                // The crash folder is best-effort. A second failure must not loop.
            }
        };
    }

    public static void WriteCrash(string text)
    {
        Ensure("user://crash");
        string name = $"crash-{Stamp()}.log";
        using Godot.FileAccess file = Godot.FileAccess.Open($"user://crash/{name}", Godot.FileAccess.ModeFlags.Write);
        file.StoreString($"Project Resonance {BuildStamp.Version}\n{text}\n");
    }

    public static void WriteRun(
        string result,
        ulong seed,
        string party,
        int roomsCleared,
        int roomCount,
        int combatTicks,
        ulong durationMs,
        IReadOnlyList<string> combatLog)
    {
        Ensure("user://runs");
        string name = $"run-{seed}-{Stamp()}.json";
        var json = new StringBuilder();
        json.Append("{\n");
        Field(json, "version", BuildStamp.Version, comma: true);
        json.Append("  \"seed\": ").Append(seed).Append(",\n");
        Field(json, "party", party, comma: true);
        json.Append("  \"roomsCleared\": ").Append(roomsCleared).Append(",\n");
        json.Append("  \"roomCount\": ").Append(roomCount).Append(",\n");
        Field(json, "result", result, comma: true);
        json.Append("  \"durationMs\": ").Append(durationMs).Append(",\n");
        json.Append("  \"combatTicks\": ").Append(combatTicks).Append(",\n");
        json.Append("  \"combatLog\": [\n");
        for (int i = 0; i < combatLog.Count; i++)
        {
            json.Append("    ").Append(Quote(combatLog[i]));
            if (i + 1 < combatLog.Count)
            {
                json.Append(',');
            }

            json.Append('\n');
        }

        json.Append("  ]\n}\n");
        using Godot.FileAccess file = Godot.FileAccess.Open($"user://runs/{name}", Godot.FileAccess.ModeFlags.Write);
        file.StoreString(json.ToString());
        GD.Print($"RUN_LOG user://runs/{name} result={result}");
    }

    public static void OpenFolder()
    {
        Ensure("user://runs");
        string path = ProjectSettings.GlobalizePath("user://runs");
        OS.ShellOpen(path);
    }

    private static void Ensure(string godotPath)
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(godotPath));
    }

    private static string Stamp()
    {
        return DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
    }

    private static void Field(StringBuilder json, string name, string value, bool comma)
    {
        json.Append("  \"").Append(name).Append("\": ").Append(Quote(value));
        if (comma)
        {
            json.Append(',');
        }

        json.Append('\n');
    }

    private static string Quote(string value)
    {
        var text = new StringBuilder(value.Length + 2);
        text.Append('"');
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            switch (c)
            {
                case '\\':
                    text.Append("\\\\");
                    break;
                case '"':
                    text.Append("\\\"");
                    break;
                case '\n':
                    text.Append("\\n");
                    break;
                case '\r':
                    text.Append("\\r");
                    break;
                case '\t':
                    text.Append("\\t");
                    break;
                default:
                    if (c < ' ')
                    {
                        text.Append("\\u").Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        text.Append(c);
                    }

                    break;
            }
        }

        text.Append('"');
        return text.ToString();
    }
}
