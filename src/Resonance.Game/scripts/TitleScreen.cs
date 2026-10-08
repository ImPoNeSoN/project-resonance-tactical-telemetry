using Godot;

namespace Resonance.Game;

/// <summary>
/// Front door for a playtest build. Play, settings, or quit.
/// </summary>
public partial class TitleScreen : Control
{
    private static readonly Color Page = new("12161c");
    private static readonly Color Ink = new("f4f7fb");
    private static readonly Color Dim = new("8ea0b5");
    private static readonly Color Gold = new("ffe08a");

    [Signal]
    public delegate void PlayPressedEventHandler();

    [Signal]
    public delegate void SettingsPressedEventHandler();

    public override void _Ready()
    {
        Fill(this);
        var background = new ColorRect { Color = Page, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(background);
        Fill(background);

        var center = new CenterContainer();
        AddChild(center);
        Fill(center);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 12);
        center.AddChild(box);

        var title = LabelOf("Project Resonance", 42, Ink);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(title);
        var subtitle = LabelOf("Tactical Telemetry", 18, Dim);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(subtitle);
        var version = LabelOf($"Playtest {BuildStamp.Version}", 16, Gold);
        version.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(version);

        box.AddChild(new Control { CustomMinimumSize = new Vector2(0, 18) });
        box.AddChild(Action("Play", () => EmitSignal(SignalName.PlayPressed)));
        box.AddChild(Action("Settings", () => EmitSignal(SignalName.SettingsPressed)));
        box.AddChild(Action("Quit", () => GetTree().Quit()));
    }

    private static Button Action(string text, Action pressed)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(280, 44),
        };
        button.AddThemeFontSizeOverride("font_size", 20);
        button.Pressed += pressed;
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
