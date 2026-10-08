using Godot;
using Resonance.Sim.Sim;

namespace Resonance.Game;

/// <summary>
/// Grey-box floor map for the linear Cinder Throat slice. Fights stay in GreyBoxBattle.
/// </summary>
public partial class FloorBoard : Control
{
    private static readonly Color Page = new("12161c");
    private static readonly Color Panel = new("1b2230");
    private static readonly Color Ink = new("d5dde8");
    private static readonly Color Dim = new("8ea0b5");
    private static readonly Color Current = new("ffe08a");

    private SimBridge _bridge = null!;
    private VBoxContainer _body = null!;

    [Signal]
    public delegate void FightRequestedEventHandler();

    [Signal]
    public delegate void PartyRequestedEventHandler();

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
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        AddChild(margin);
        Fill(margin);

        _body = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _body.AddThemeConstantOverride("separation", 8);
        margin.AddChild(_body);

        _bridge.FloorChanged += () => CallDeferred(MethodName.Rebuild);
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (Node child in _body.GetChildren())
        {
            child.QueueFree();
        }

        FloorRun? run = _bridge.Floor;
        if (run != null && run.Finished)
        {
            BuildResult(run);
            return;
        }

        if (run != null && run.Current.Kind == FloorRoomKind.Rest)
        {
            BuildRest(run);
            return;
        }

        BuildMap(run);
    }

    private void BuildMap(FloorRun? run)
    {
        _body.AddChild(Heading("Floor map", 28, Ink));
        _body.AddChild(Heading("Cinder Throat · Floor 1", 16, Dim));
        string blurb = run == null
            ? "Five rooms. Two trash fights, an elite, a rest, then the Carapace Engine. HP and MP carry."
            : $"{run.PartyLabel} · seed {run.Seed} · combat ticks {run.Ticks}";
        _body.AddChild(Wrap(blurb, 14, Ink));

        var columns = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        columns.AddThemeConstantOverride("separation", 12);
        _body.AddChild(columns);

        var rooms = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        rooms.AddThemeConstantOverride("separation", 6);
        columns.AddChild(rooms);
        for (int i = 0; i < FloorRun.Rooms.Length; i++)
        {
            rooms.AddChild(RoomRow(FloorRun.Rooms[i], Mark(run, i)));
        }

        var side = new VBoxContainer { CustomMinimumSize = new Vector2(360, 0) };
        side.AddThemeConstantOverride("separation", 6);
        columns.AddChild(side);
        side.AddChild(PanelBlock(PartyBlock(run)));

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        _body.AddChild(actions);
        if (run != null)
        {
            var enter = new Button { Text = $"Enter {run.Current.Name}" };
            enter.Pressed += () => EmitSignal(SignalName.FightRequested);
            actions.AddChild(enter);
        }

        var back = new Button { Text = "Back to party" };
        back.Pressed += LeaveToParty;
        actions.AddChild(back);
    }

    private void LeaveToParty()
    {
        _bridge.LeaveFloor();
        EmitSignal(SignalName.PartyRequested);
    }

    private void BuildRest(FloorRun run)
    {
        _body.AddChild(Heading("Rest point", 28, Ink));
        _body.AddChild(Heading("Kiln Cool · Cinder Throat · Floor 1", 16, Dim));
        int hpPart = FloorRun.RestHpOfMissingBp / 100;
        int mpPart = FloorRun.RestMpOfMissingBp / 100;
        _body.AddChild(Wrap(
            $"Regain {hpPart}% of missing HP and {mpPart}% of missing MP. A fallen hero is raised to {hpPart}% HP and {mpPart}% MP.",
            14,
            Ink));

        var list = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 6);
        _body.AddChild(list);
        for (int i = 0; i < run.Party.Length; i++)
        {
            HeroVitals before = run.Party[i];
            HeroVitals after = FloorRun.Restore(before);
            list.AddChild(PanelBlock(Wrap(
                $"{before.Name}\nHP {before.Hp} -> {after.Hp}    MP {before.Mp} -> {after.Mp}",
                16,
                Ink)));
        }

        var cont = new Button { Text = "Continue" };
        cont.Pressed += () => _bridge.TakeFloorRest();
        _body.AddChild(cont);
    }

    private void BuildResult(FloorRun run)
    {
        _body.AddChild(Heading(run.Cleared ? "Victory" : "Defeat", 28, Ink));
        _body.AddChild(Heading("Cinder Throat · Floor 1", 16, Dim));
        _body.AddChild(Wrap(run.ResultBody, 16, Ink));
        _body.AddChild(Wrap($"Build {BuildStamp.Version}. A run log was written to the log folder.", 14, Dim));

        var list = new VBoxContainer();
        list.AddThemeConstantOverride("separation", 4);
        _body.AddChild(list);
        for (int i = 0; i < run.Party.Length; i++)
        {
            HeroVitals hero = run.Party[i];
            string state = hero.Alive ? "standing" : "down";
            list.AddChild(Wrap($"{hero.Name}  {state}  HP {hero.Hp}/{hero.MaxHp}  MP {hero.Mp}/{hero.MaxMp}", 14, Ink));
        }

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        _body.AddChild(actions);
        var logs = new Button { Text = "Open log folder" };
        logs.Pressed += () => RunLog.OpenFolder();
        actions.AddChild(logs);
        var title = new Button { Text = "Back to title" };
        title.Pressed += () => EmitSignal(SignalName.TitleRequested);
        actions.AddChild(title);
    }

    private static string Mark(FloorRun? run, int index)
    {
        if (run == null)
        {
            return index == 0 ? "first" : "ahead";
        }

        if (run.Cleared)
        {
            return "cleared";
        }

        if (index == run.RoomIndex)
        {
            return "current";
        }

        return index < run.RoomIndex ? "cleared" : "ahead";
    }

    private static Control RoomRow(FloorRoom room, string mark)
    {
        bool current = mark == "current";
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = current ? new Color("2a3142") : Panel,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            BorderColor = current ? Current : new Color("00000000"),
            BorderWidthBottom = current ? 2 : 0,
        });
        var row = new HBoxContainer();
        panel.AddChild(row);
        row.AddChild(Heading($"{room.Index + 1}", 18, current ? Current : Dim));
        row.AddChild(new Control { CustomMinimumSize = new Vector2(12, 0), MouseFilter = MouseFilterEnum.Ignore });
        var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddChild(text);
        text.AddChild(Heading($"{room.Name}   {KindLabel(room.Kind)}", 16, current ? Current : Ink));
        text.AddChild(Heading(room.Detail, 12, Dim));
        row.AddChild(Heading(mark, 14, current ? Current : Dim));
        return panel;
    }

    private static string KindLabel(FloorRoomKind kind) => kind switch
    {
        FloorRoomKind.Trash => "Trash",
        FloorRoomKind.Elite => "Elite",
        FloorRoomKind.Rest => "Rest",
        _ => "Boss",
    };

    private Control PartyBlock(FloorRun? run)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);
        box.AddChild(Heading(run == null ? "Party" : "Carried vitals", 14, Dim));
        if (run == null)
        {
            box.AddChild(Heading("Choose four heroes on the party screen.", 13, Ink));
            return box;
        }

        for (int i = 0; i < run.Party.Length; i++)
        {
            HeroVitals hero = run.Party[i];
            box.AddChild(Heading($"{hero.Name}  HP {hero.Hp}/{hero.MaxHp}  MP {hero.Mp}/{hero.MaxMp}", 13, Ink));
        }

        return box;
    }

    private static Control PanelBlock(Control inner)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = Panel,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 10,
            ContentMarginBottom = 10,
        });
        panel.AddChild(inner);
        return panel;
    }

    private static Label Heading(string text, int size, Color color)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Label Wrap(string text, int size, Color color)
    {
        var label = Heading(text, size, color);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
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
