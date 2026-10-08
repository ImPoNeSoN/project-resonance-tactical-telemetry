using Godot;

namespace Resonance.Game;

/// <summary>
/// Window, view, and battle speed. Stored at user://settings.cfg.
/// </summary>
public static class PlaytestSettings
{
    public const string Windowed = "windowed";
    public const string Borderless = "borderless";
    public const string Fullscreen = "fullscreen";

    public static readonly (int Width, int Height)[] Resolutions =
    [
        (1280, 720),
        (1600, 900),
        (1920, 1080),
        (2560, 1440),
    ];

    public static string WindowMode { get; private set; } = Windowed;

    public static int Width { get; private set; } = 1280;

    public static int Height { get; private set; } = 720;

    public static bool View3d { get; private set; } = true;

    public static int BattleSpeed { get; private set; } = 1;

    public static void Load()
    {
        var file = new ConfigFile();
        if (file.Load("user://settings.cfg") != Error.Ok)
        {
            return;
        }

        WindowMode = NormalizeMode(file.GetValue("display", "mode", Windowed).AsString());
        Width = (int)file.GetValue("display", "width", 1280);
        Height = (int)file.GetValue("display", "height", 720);
        if (!KnownResolution(Width, Height))
        {
            Width = 1280;
            Height = 720;
        }

        View3d = file.GetValue("display", "view3d", true).AsBool();
        BattleSpeed = NormalizeSpeed((int)file.GetValue("battle", "speed", 1));
    }

    public static void Save()
    {
        var file = new ConfigFile();
        file.SetValue("display", "mode", WindowMode);
        file.SetValue("display", "width", Width);
        file.SetValue("display", "height", Height);
        file.SetValue("display", "view3d", View3d);
        file.SetValue("battle", "speed", BattleSpeed);
        file.Save("user://settings.cfg");
    }

    public static void SetWindow(string mode, int width, int height)
    {
        WindowMode = NormalizeMode(mode);
        if (KnownResolution(width, height))
        {
            Width = width;
            Height = height;
        }

        Save();
        Apply(Engine.GetMainLoop() is SceneTree tree ? tree.Root : null);
    }

    public static void SetView3d(bool view3d)
    {
        View3d = view3d;
        Save();
    }

    public static void SetBattleSpeed(int speed)
    {
        BattleSpeed = NormalizeSpeed(speed);
        Save();
    }

    public static void Apply(Window? window)
    {
        if (window == null)
        {
            return;
        }

        window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
        window.ContentScaleSize = new Vector2I(1280, 720);
        window.ContentScaleFactor = 1f;
        if (WindowMode == Fullscreen)
        {
            window.Borderless = false;
            window.Mode = Window.ModeEnum.Fullscreen;
            return;
        }

        window.Mode = Window.ModeEnum.Windowed;
        window.Borderless = WindowMode == Borderless;
        window.Size = new Vector2I(Width, Height);
    }

    private static bool KnownResolution(int width, int height)
    {
        for (int i = 0; i < Resolutions.Length; i++)
        {
            if (Resolutions[i].Width == width && Resolutions[i].Height == height)
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeMode(string mode)
    {
        if (mode == Borderless || mode == Fullscreen)
        {
            return mode;
        }

        return Windowed;
    }

    private static int NormalizeSpeed(int speed)
    {
        if (speed == 2 || speed == 4)
        {
            return speed;
        }

        return 1;
    }
}
