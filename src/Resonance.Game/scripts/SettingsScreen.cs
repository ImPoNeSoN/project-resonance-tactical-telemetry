using Godot;

namespace Resonance.Game;

/// <summary>
/// Window, resolution, 3D view, and battle speed. Changes save immediately.
/// </summary>
public partial class SettingsScreen : Control
{
    private static readonly Color Page = new("12161c");
    private static readonly Color Ink = new("f4f7fb");
    private static readonly Color Dim = new("8ea0b5");
    private static readonly Color Gold = new("ffe08a");

    private readonly List<Button> _modes = new();
    private readonly List<Button> _resolutions = new();
    private readonly List<Button> _speeds = new();
    private Button _view3d = null!;
    private Button _grey = null!;

    [Signal]
    public delegate void ClosedEventHandler();

    public override void _Ready()
    {
        Fill(this);
        var background = new ColorRect { Color = Page, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(background);
        Fill(background);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 48);
        margin.AddThemeConstantOverride("margin_right", 48);
        margin.AddThemeConstantOverride("margin_top", 28);
        margin.AddThemeConstantOverride("margin_bottom", 28);
        AddChild(margin);
        Fill(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 14);
        margin.AddChild(root);
        root.AddChild(LabelOf("Settings", 32, Ink));
        root.AddChild(LabelOf($"Project Resonance {BuildStamp.Version}", 14, Dim));

        root.AddChild(LabelOf("Window", 16, Dim));
        var modes = Row();
        root.AddChild(modes);
        modes.AddChild(ModeButton(PlaytestSettings.Windowed, "Windowed"));
        modes.AddChild(ModeButton(PlaytestSettings.Borderless, "Borderless"));
        modes.AddChild(ModeButton(PlaytestSettings.Fullscreen, "Fullscreen"));

        root.AddChild(LabelOf("Resolution", 16, Dim));
        var resolutions = Row();
        root.AddChild(resolutions);
        for (int i = 0; i < PlaytestSettings.Resolutions.Length; i++)
        {
            (int width, int height) = PlaytestSettings.Resolutions[i];
            var button = Choice($"{width}×{height}");
            int w = width;
            int h = height;
            button.Pressed += () =>
            {
                PlaytestSettings.SetWindow(PlaytestSettings.WindowMode, w, h);
                Paint();
            };
            _resolutions.Add(button);
            resolutions.AddChild(button);
        }

        root.AddChild(LabelOf("View", 16, Dim));
        var views = Row();
        root.AddChild(views);
        _view3d = Choice("3D arena");
        _view3d.Pressed += () =>
        {
            PlaytestSettings.SetView3d(true);
            Paint();
        };
        _grey = Choice("Grey-box");
        _grey.Pressed += () =>
        {
            PlaytestSettings.SetView3d(false);
            Paint();
        };
        views.AddChild(_view3d);
        views.AddChild(_grey);

        root.AddChild(LabelOf("Battle speed", 16, Dim));
        var speeds = Row();
        root.AddChild(speeds);
        speeds.AddChild(SpeedButton(1));
        speeds.AddChild(SpeedButton(2));
        speeds.AddChild(SpeedButton(4));

        root.AddChild(new Control
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        });
        var back = new Button
        {
            Text = "Back",
            CustomMinimumSize = new Vector2(180, 40),
        };
        back.AddThemeFontSizeOverride("font_size", 18);
        back.Pressed += () =>
        {
            PlaytestSettings.Save();
            EmitSignal(SignalName.Closed);
        };
        root.AddChild(back);
        Paint();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged && Visible)
        {
            Paint();
        }
    }

    private Button ModeButton(string mode, string label)
    {
        var button = Choice(label);
        button.Pressed += () =>
        {
            PlaytestSettings.SetWindow(mode, PlaytestSettings.Width, PlaytestSettings.Height);
            Paint();
        };
        _modes.Add(button);
        return button;
    }

    private Button SpeedButton(int speed)
    {
        var button = Choice($"{speed}×");
        button.Pressed += () =>
        {
            PlaytestSettings.SetBattleSpeed(speed);
            Paint();
        };
        _speeds.Add(button);
        return button;
    }

    private void Paint()
    {
        Mark(_modes[0], PlaytestSettings.WindowMode == PlaytestSettings.Windowed);
        Mark(_modes[1], PlaytestSettings.WindowMode == PlaytestSettings.Borderless);
        Mark(_modes[2], PlaytestSettings.WindowMode == PlaytestSettings.Fullscreen);
        for (int i = 0; i < _resolutions.Count; i++)
        {
            (int width, int height) = PlaytestSettings.Resolutions[i];
            Mark(_resolutions[i], PlaytestSettings.Width == width && PlaytestSettings.Height == height);
        }

        Mark(_view3d, PlaytestSettings.View3d);
        Mark(_grey, !PlaytestSettings.View3d);
        Mark(_speeds[0], PlaytestSettings.BattleSpeed == 1);
        Mark(_speeds[1], PlaytestSettings.BattleSpeed == 2);
        Mark(_speeds[2], PlaytestSettings.BattleSpeed == 4);
    }

    private static void Mark(Button button, bool selected)
    {
        button.Modulate = selected ? Gold : Colors.White;
    }

    private static HBoxContainer Row()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        return row;
    }

    private static Button Choice(string text)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(150, 36),
        };
        button.AddThemeFontSizeOverride("font_size", 16);
        return button;
    }

    private static Label LabelOf(string text, int size, Color color)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static void Fill(Control control)
    {
        control.AnchorLeft = 0f;
        control.AnchorTop = 0f;
        control.AnchorRight = 1f;
        control.AnchorBottom = 1f;
        control.OffsetLeft = 0f;
        control.OffsetTop = 0f;
        control.OffsetRight = 0f;
        control.OffsetBottom = 0f;
        control.GrowHorizontal = GrowDirection.Both;
        control.GrowVertical = GrowDirection.Both;
        control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        control.SizeFlagsVertical = SizeFlags.ExpandFill;
    }
}
