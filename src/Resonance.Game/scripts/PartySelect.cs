using Godot;
using Resonance.Sim.Sim;

namespace Resonance.Game;

/// <summary>
/// Pick four of the six heroes. Presets fill a party. Exactly four can start.
/// </summary>
public partial class PartySelect : Control
{
    private static readonly Color Page = new("12161c");
    private static readonly Color Panel = new("1b2230");
    private static readonly Color Ink = new("d5dde8");
    private static readonly Color Dim = new("8ea0b5");
    private static readonly Color Current = new("ffe08a");

    private readonly bool[] _selected = new bool[HeroRoster.Count];
    private readonly PanelContainer[] _cards = new PanelContainer[HeroRoster.Count];
    private readonly Label[] _marks = new Label[HeroRoster.Count];
    private readonly Button[] _toggles = new Button[HeroRoster.Count];

    private SimBridge _bridge = null!;
    private Label _status = null!;
    private Button _enterFloor = null!;
    private Button? _bossOnly;

    [Signal]
    public delegate void FloorChosenEventHandler();

    [Signal]
    public delegate void BossChosenEventHandler();

    [Signal]
    public delegate void TitleRequestedEventHandler();

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        Fill(this);

        var background = new ColorRect { Color = Page, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(background);
        Fill(background);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        AddChild(margin);
        Fill(margin);

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        root.AddChild(LabelOf("Choose a party", 28, Ink));
        root.AddChild(LabelOf("Pick four heroes. A preset fills the four. Missing a tank or a healer warns, and still lets you start.", 14, Dim));

        var presets = new HBoxContainer();
        presets.AddThemeConstantOverride("separation", 8);
        root.AddChild(presets);
        for (int preset = 0; preset < PartyPreset.Count; preset++)
        {
            int chosen = preset;
            var button = new Button { Text = PartyPreset.Name(preset) };
            button.AddThemeFontSizeOverride("font_size", 16);
            button.Pressed += () => ApplyPreset(chosen);
            presets.AddChild(button);
        }

        _status = LabelOf("", 16, Current);
        root.AddChild(_status);

        var grid = new GridContainer
        {
            Columns = 3,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        grid.AddThemeConstantOverride("h_separation", 10);
        grid.AddThemeConstantOverride("v_separation", 10);
        root.AddChild(grid);

        for (int i = 0; i < HeroRoster.Count; i++)
        {
            int id = HeroRoster.Ids[i];
            grid.AddChild(BuildCard(id));
        }

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        root.AddChild(actions);
        _enterFloor = new Button { Text = "Enter floor" };
        _enterFloor.AddThemeFontSizeOverride("font_size", 16);
        _enterFloor.Pressed += ChooseFloor;
        actions.AddChild(_enterFloor);
        if (BuildStamp.DevSession)
        {
            _bossOnly = new Button { Text = "Boss only" };
            _bossOnly.AddThemeFontSizeOverride("font_size", 16);
            _bossOnly.Pressed += ChooseBoss;
            actions.AddChild(_bossOnly);
        }

        var title = new Button { Text = "Title" };
        title.AddThemeFontSizeOverride("font_size", 16);
        title.Pressed += () => EmitSignal(SignalName.TitleRequested);
        actions.AddChild(title);

        Refresh();
    }

    private Control BuildCard(int id)
    {
        var card = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 150),
        };
        _cards[id] = card;

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);
        card.AddChild(box);
        box.AddChild(LabelOf(HeroRoster.Name(id), 18, Ink));
        box.AddChild(LabelOf($"{HeroRoster.RoleName(id)}  ·  {HeroRoster.RaceName(id)}", 14, Dim));
        var kit = LabelOf(HeroRoster.KitSummary(id), 14, Ink);
        kit.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        kit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(kit);

        var row = new HBoxContainer();
        box.AddChild(row);
        var mark = LabelOf("", 14, Current);
        mark.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _marks[id] = mark;
        row.AddChild(mark);

        var toggle = new Button { Text = $"Add {FirstName(id)}" };
        toggle.AddThemeFontSizeOverride("font_size", 14);
        int captured = id;
        toggle.Pressed += () => Toggle(captured);
        _toggles[id] = toggle;
        row.AddChild(toggle);
        return card;
    }

    private void ApplyPreset(int preset)
    {
        for (int i = 0; i < _selected.Length; i++)
        {
            _selected[i] = false;
        }

        int[] ids = HeroRoster.PresetIds(preset);
        for (int i = 0; i < ids.Length; i++)
        {
            _selected[ids[i]] = true;
        }

        Refresh();
    }

    private void Toggle(int id)
    {
        if (_selected[id])
        {
            _selected[id] = false;
            Refresh();
            return;
        }

        if (CountSelected() >= 4)
        {
            return;
        }

        _selected[id] = true;
        Refresh();
    }

    private void ChooseFloor()
    {
        int[] ids = Selected();
        if (!HeroRoster.Check(ids).CanStart)
        {
            return;
        }

        _bridge.StartFloor(ids);
        EmitSignal(SignalName.FloorChosen);
    }

    private void ChooseBoss()
    {
        int[] ids = Selected();
        if (!HeroRoster.Check(ids).CanStart)
        {
            return;
        }

        _bridge.StartBoss(ids);
        EmitSignal(SignalName.BossChosen);
    }

    private void Refresh()
    {
        int[] ids = Selected();
        PartyCheck check = HeroRoster.Check(ids);
        _status.Text = check.Status;
        _enterFloor.Disabled = !check.CanStart;
        if (_bossOnly != null)
        {
            _bossOnly.Disabled = !check.CanStart;
        }
        for (int id = 0; id < HeroRoster.Count; id++)
        {
            bool on = _selected[id];
            _cards[id].AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = on ? new Color("2a3142") : Panel,
                ContentMarginLeft = 12,
                ContentMarginRight = 12,
                ContentMarginTop = 10,
                ContentMarginBottom = 10,
                BorderColor = on ? Current : new Color("3a4558"),
                BorderWidthLeft = on ? 4 : 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
            });
            _marks[id].Text = on ? "In party" : "";
            _toggles[id].Text = on ? $"Remove {FirstName(id)}" : $"Add {FirstName(id)}";
            _toggles[id].Disabled = !on && CountSelected() >= 4;
        }
    }

    private int CountSelected()
    {
        int count = 0;
        for (int i = 0; i < _selected.Length; i++)
        {
            if (_selected[i])
            {
                count++;
            }
        }

        return count;
    }

    private int[] Selected()
    {
        var ids = new int[CountSelected()];
        int n = 0;
        for (int i = 0; i < _selected.Length; i++)
        {
            if (_selected[i])
            {
                ids[n++] = i;
            }
        }

        return ids;
    }

    private static string FirstName(int id)
    {
        string name = HeroRoster.Name(id);
        int space = name.IndexOf(' ');
        return space < 0 ? name : name[..space];
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
