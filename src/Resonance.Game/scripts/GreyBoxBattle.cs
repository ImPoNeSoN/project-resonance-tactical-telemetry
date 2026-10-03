using Godot;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Sim;

namespace Resonance.Game;

/// <summary>
/// Grey-box Carapace Engine fight. Plain Control nodes and flat colors only.
/// Full-rect columns: a lane timeline, party, boss parts, a scrolling resonance readout,
/// and a pinned ability panel, then the action bar.
/// </summary>
public partial class GreyBoxBattle : Control
{
    private const int Swatch = 12;
    private const int Pip = 10;
    private const int NameCol = 168;
    private const int ApCol = 72;
    private const int Gap = 4;

    private static readonly Color[] PortraitColors =
    [
        new("c4a574"),
        new("7dcea0"),
        new("85c1e9"),
        new("f5b7b1"),
        new("e67e22"),
    ];

    private SimBridge _bridge = null!;
    private Label _status = null!;
    private Label _bossTitle = null!;
    private Label _resonance = null!;
    private Label _abilityDetail = null!;
    private Label _order = null!;
    private Label _actorLabel = null!;
    private VBoxContainer _laneBox = null!;
    private readonly List<TimelineLane> _lanes = new();
    private readonly List<HeroRow> _heroes = new();
    private readonly List<PartRow> _parts = new();
    private HBoxContainer _abilities = null!;
    private HBoxContainer _allies = null!;
    private RichTextLabel _log = null!;
    private ColorRect _background = null!;
    private MarginContainer _margin = null!;
    private Control _overlay = null!;
    private Control _picker = null!;
    private Label _overlayTitle = null!;
    private Label _overlayDetail = null!;
    private Button _auto = null!;
    private Button _pause = null!;
    private int _actor;
    private int _part;
    private int _ally = 2;
    private int _abilityActor = -1;
    private int _selectedAbility = -1;
    private int _hoveredAbility = -1;
    private bool _fitting;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        ConfigureWindowStretch();
        FillViewport(this);

        _background = new ColorRect { Color = new Color("12161c"), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_background);
        FillViewport(_background);

        _margin = new MarginContainer();
        _margin.AddThemeConstantOverride("margin_left", 6);
        _margin.AddThemeConstantOverride("margin_top", 4);
        _margin.AddThemeConstantOverride("margin_right", 6);
        _margin.AddThemeConstantOverride("margin_bottom", 4);
        AddChild(_margin);
        FillViewport(_margin);

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddThemeConstantOverride("separation", 4);
        _margin.AddChild(root);

        root.AddChild(BuildHeader());
        root.AddChild(BuildTimeline());
        root.AddChild(BuildBody());
        root.AddChild(BuildAction());

        _log = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            FitContent = false,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1,
            CustomMinimumSize = new Vector2(0, 52),
        };
        _log.AddThemeStyleboxOverride("normal", Flat(new Color("0e1218"), 4));
        _log.AddThemeFontSizeOverride("normal_font_size", 12);
        _log.AddThemeColorOverride("default_color", new Color("c5d0dc"));
        root.AddChild(_log);

        BuildOverlay();
        BuildPicker();

        _bridge.LogLine += line => _log.AppendText($"[color=#9fd0ff]{line}[/color]\n");
        _bridge.LogCleared += () => _log.Clear();
        _bridge.StateChanged += Refresh;
        Refresh();
        _picker.Visible = true;
        GetTree().Root.SizeChanged += FitToViewport;
    }

    /// <summary>
    /// Base layout is 1280×720. Canvas-item stretch with expand scales that layout to the
    /// window and grows the viewport on the extra axis, so a maximize actually changes the UI.
    /// </summary>
    private void ConfigureWindowStretch()
    {
        Window window = GetTree().Root;
        window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
        window.ContentScaleSize = new Vector2I(1280, 720);
        window.ContentScaleFactor = 1f;
    }

    private void FitToViewport()
    {
        if (_fitting)
        {
            return;
        }

        _fitting = true;
        FillViewport(this);
        FillViewport(_background);
        FillViewport(_margin);
        if (IsInstanceValid(_overlay))
        {
            FillViewport(_overlay);
        }

        if (IsInstanceValid(_picker))
        {
            FillViewport(_picker);
        }

        _fitting = false;
    }

    private static void FillViewport(Control control)
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

    public override void _Input(InputEvent @event)
    {
        if (_picker.Visible)
        {
            return;
        }

        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Space)
        {
            if (_bridge.Simulation.Outcome == FightOutcome.Ongoing)
            {
                _bridge.TogglePause();
            }

            GetViewport().SetInputAsHandled();
        }
    }


    private Control BuildHeader()
    {
        var header = new VBoxContainer();
        header.AddThemeConstantOverride("separation", 0);

        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 12);
        var title = new Label { Text = "Project Resonance  ·  Grey-Box Battle  ·  Carapace Engine" };
        title.AddThemeFontSizeOverride("font_size", 15);
        top.AddChild(title);

        _status = new Label
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _status.AddThemeFontSizeOverride("font_size", 13);
        _status.AddThemeColorOverride("font_color", new Color("d5dde8"));
        top.AddChild(_status);
        header.AddChild(top);

        var hint = new Label
        {
            Text = "Space pauses. Click a hero, click a part, then pick an ability.",
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        hint.AddThemeFontSizeOverride("font_size", 12);
        hint.AddThemeColorOverride("font_color", new Color("8ea0b5"));
        header.AddChild(hint);
        return header;
    }

    private Control BuildTimeline()
    {
        var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("181e28"),
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 3,
            ContentMarginBottom = 3,
        });

        var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", 1);
        panel.AddChild(box);

        var scale = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        scale.AddThemeConstantOverride("separation", Gap);
        int gutter = Swatch + Pip + NameCol + ApCol + (Gap * 3);
        scale.AddChild(new Control
        {
            CustomMinimumSize = new Vector2(gutter, 1),
            MouseFilter = MouseFilterEnum.Ignore,
        });
        scale.AddChild(new ApRuler());
        box.AddChild(scale);

        _laneBox = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _laneBox.AddThemeConstantOverride("separation", 1);
        box.AddChild(_laneBox);
        return panel;
    }

    private Control BuildBody()
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 5,
        };
        row.AddThemeConstantOverride("separation", 6);

        BattleSimulator sim = _bridge.Simulation;
        row.AddChild(BuildHeroColumn(sim));
        row.AddChild(BuildPartColumn(sim));
        row.AddChild(BuildInfoColumn());
        return row;
    }

    private Control BuildHeroColumn(BattleSimulator sim)
    {
        var column = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(300, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        column.AddThemeConstantOverride("separation", 2);
        column.AddChild(ColumnTitle("Party"));

        var heroScroll = VerticalScroll();
        column.AddChild(heroScroll);
        var heroBox = Stack();
        heroScroll.AddChild(heroBox);

        for (int i = 0; i < sim.Heroes.Count; i++)
        {
            int slot = i;
            var row = new HeroRow(PortraitColors[i % 4]);
            row.Panel.GuiInput += @event => ClickHero(@event, slot);
            _heroes.Add(row);
            heroBox.AddChild(row.Panel);
        }

        return column;
    }

    private Control BuildPartColumn(BattleSimulator sim)
    {
        var column = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        column.AddThemeConstantOverride("separation", 2);
        _bossTitle = ColumnTitle(sim.Boss.Name);
        column.AddChild(_bossTitle);

        var partScroll = VerticalScroll();
        column.AddChild(partScroll);
        var partBox = Stack();
        partScroll.AddChild(partBox);

        for (int i = 0; i < sim.Boss.Parts.Length; i++)
        {
            int index = i;
            var row = new PartRow(PortraitColors);
            row.Panel.GuiInput += @event => ClickPart(@event, index);
            _parts.Add(row);
            partBox.AddChild(row.Panel);
        }

        return column;
    }

    private Control BuildInfoColumn()
    {
        var column = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(268, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        column.AddThemeConstantOverride("separation", 2);
        column.AddChild(ColumnTitle("Resonance"));

        var resonanceScroll = VerticalScroll();
        resonanceScroll.SizeFlagsStretchRatio = 1;
        column.AddChild(resonanceScroll);
        var resonanceBox = Stack();
        resonanceScroll.AddChild(resonanceBox);

        _resonance = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _resonance.AddThemeFontSizeOverride("font_size", 12);
        _resonance.AddThemeColorOverride("font_color", new Color("d5dde8"));
        resonanceBox.AddChild(_resonance);

        column.AddChild(ColumnTitle("Ability"));
        var abilityPanel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1,
        };
        abilityPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("1b2230"),
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        });
        var abilityScroll = VerticalScroll();
        abilityPanel.AddChild(abilityScroll);
        _abilityDetail = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            Text = "Select an ability.",
        };
        _abilityDetail.AddThemeFontSizeOverride("font_size", 12);
        _abilityDetail.AddThemeColorOverride("font_color", new Color("d5dde8"));
        abilityScroll.AddChild(_abilityDetail);
        column.AddChild(abilityPanel);
        return column;
    }

    private Control BuildAction()
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("181e28"),
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        });

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 3);
        panel.AddChild(box);

        _order = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MaxLinesVisible = 2,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _order.AddThemeFontSizeOverride("font_size", 12);
        _order.AddThemeColorOverride("font_color", new Color("d5dde8"));
        box.AddChild(_order);

        var abilityRow = new HBoxContainer();
        abilityRow.AddThemeConstantOverride("separation", 6);
        _actorLabel = new Label
        {
            CustomMinimumSize = new Vector2(148, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _actorLabel.AddThemeFontSizeOverride("font_size", 13);
        _actorLabel.AddThemeColorOverride("font_color", new Color("ffe08a"));
        abilityRow.AddChild(_actorLabel);

        var abilityScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        abilityScroll.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        _abilities = new HBoxContainer();
        _abilities.AddThemeConstantOverride("separation", 4);
        abilityScroll.AddChild(_abilities);
        abilityRow.AddChild(abilityScroll);
        box.AddChild(abilityRow);

        var controls = new HBoxContainer();
        controls.AddThemeConstantOverride("separation", 4);
        var allyLabel = new Label
        {
            Text = "Ally",
            VerticalAlignment = VerticalAlignment.Center,
        };
        allyLabel.AddThemeFontSizeOverride("font_size", 12);
        allyLabel.AddThemeColorOverride("font_color", new Color("8ea0b5"));
        controls.AddChild(allyLabel);

        _allies = new HBoxContainer();
        _allies.AddThemeConstantOverride("separation", 4);
        controls.AddChild(_allies);
        BattleSimulator sim = _bridge.Simulation;
        for (int i = 0; i < sim.Heroes.Count; i++)
        {
            int slot = i;
            var button = new Button
            {
                Text = ShortName(sim.Heroes[i].Name),
                CustomMinimumSize = new Vector2(78, 0),
            };
            button.AddThemeFontSizeOverride("font_size", 13);
            button.Pressed += () =>
            {
                _ally = slot;
                Refresh();
            };
            _allies.AddChild(button);
        }

        controls.AddChild(new Control
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        });

        var step = new Button { Text = "Step" };
        step.Pressed += () => _bridge.StepOnce();
        controls.AddChild(step);

        _pause = new Button { Text = "Pause" };
        _pause.Pressed += () => _bridge.TogglePause();
        controls.AddChild(_pause);

        _auto = new Button { Text = "Auto-run" };
        _auto.Pressed += () => _bridge.ToggleAuto();
        controls.AddChild(_auto);

        var restart = new Button { Text = "Restart" };
        restart.Pressed += Restart;
        controls.AddChild(restart);
        box.AddChild(controls);
        return panel;
    }

    private static ScrollContainer VerticalScroll()
    {
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        scroll.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        return scroll;
    }

    private static VBoxContainer Stack()
    {
        var box = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        box.AddThemeConstantOverride("separation", 4);
        return box;
    }

    private static Label ColumnTitle(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 11);
        label.AddThemeColorOverride("font_color", new Color("8ea0b5"));
        return label;
    }

    private static StyleBoxFlat Flat(Color color, int margin)
    {
        return new StyleBoxFlat
        {
            BgColor = color,
            ContentMarginLeft = margin,
            ContentMarginRight = margin,
            ContentMarginTop = margin,
            ContentMarginBottom = margin,
        };
    }

    private void BuildOverlay()
    {
        _overlay = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.72f),
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
        };
        AddChild(_overlay);
        FillViewport(_overlay);

        var center = new CenterContainer();
        _overlay.AddChild(center);
        FillViewport(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(420, 0) };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("222836"),
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 20,
            ContentMarginBottom = 20,
        });
        center.AddChild(panel);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        panel.AddChild(box);

        _overlayTitle = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _overlayTitle.AddThemeFontSizeOverride("font_size", 28);
        box.AddChild(_overlayTitle);
        _overlayDetail = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        box.AddChild(_overlayDetail);
        var again = new Button { Text = "Replay this party" };
        again.Pressed += Replay;
        box.AddChild(again);
        var change = new Button { Text = "Change party" };
        change.Pressed += ShowPicker;
        box.AddChild(change);
    }

    private void BuildPicker()
    {
        _picker = new ColorRect
        {
            Color = new Color("12161c"),
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
        };
        AddChild(_picker);
        FillViewport(_picker);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        _picker.AddChild(margin);
        FillViewport(margin);

        var scroll = VerticalScroll();
        margin.AddChild(scroll);

        var center = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(center);
        center.AddChild(new Control
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        });

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(720, 0) };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("1b2230"),
            ContentMarginLeft = 28,
            ContentMarginRight = 28,
            ContentMarginTop = 22,
            ContentMarginBottom = 22,
        });
        center.AddChild(panel);
        center.AddChild(new Control
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        });

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        panel.AddChild(box);

        var heading = new Label
        {
            Text = "Choose a party",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        heading.AddThemeFontSizeOverride("font_size", 28);
        box.AddChild(heading);

        var hint = new Label
        {
            Text = "Each deck walks a different chain. The Carapace parts are the reason.",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        box.AddChild(hint);

        for (int preset = 0; preset < PartyPreset.Count; preset++)
        {
            int chosen = preset;
            var choice = new Button { Text = PartyPreset.Name(preset), Alignment = HorizontalAlignment.Left };
            choice.AddThemeFontSizeOverride("font_size", 18);
            choice.Pressed += () => ChooseParty(chosen);
            box.AddChild(choice);
            var summary = new Label
            {
                Text = PartyPreset.Summary(preset),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            box.AddChild(summary);
        }
    }

    private void ShowPicker()
    {
        if (_bridge.AutoRunning)
        {
            _bridge.ToggleAuto();
        }

        _picker.Visible = true;
    }

    private void ChooseParty(int preset)
    {
        _actor = 0;
        _part = 0;
        _ally = 2;
        _abilityActor = -1;
        _picker.Visible = false;
        _bridge.StartEncounter(preset);
    }

    private void Replay()
    {
        _actor = 0;
        _part = 0;
        _ally = 2;
        _abilityActor = -1;
        _picker.Visible = false;
        _bridge.StartEncounter(_bridge.Preset);
    }

    private void Restart() => ShowPicker();

    private void ClickHero(InputEvent @event, int slot)
    {
        if (@event is not InputEventMouseButton mouse || !mouse.Pressed || mouse.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        _actor = slot;
        _bridge.Simulation.Heroes[slot].CurrentTargetPart = _part;
        Refresh();
        GetViewport().SetInputAsHandled();
    }

    private void ClickPart(InputEvent @event, int index)
    {
        if (@event is not InputEventMouseButton mouse || !mouse.Pressed || mouse.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        _part = index;
        _bridge.Simulation.Heroes[_actor].CurrentTargetPart = index;
        Refresh();
        GetViewport().SetInputAsHandled();
    }

    private void Queue(int abilityId)
    {
        BattleSimulator sim = _bridge.Simulation;
        if (sim.Outcome != FightOutcome.Ongoing)
        {
            return;
        }

        if (!sim.Paused)
        {
            _bridge.TogglePause();
        }

        _selectedAbility = abilityId;
        AbilityDef ability = AbilityCatalog.Get(abilityId);
        sim.Heroes[_actor].CurrentTargetPart = _part;
        int ally = ability.Kind == AbilityKind.Healing || ability.Effect == SupportEffect.CircuitBenediction ? _ally : -1;
        _bridge.QueueAbility(_actor, abilityId, _part, ally);
    }

    private void Refresh()
    {
        BattleSimulator sim = _bridge.Simulation;
        EnsureLanes(sim);
        EnsureAbilities(sim);
        UpdateAbilityDetail();

        bool ended = sim.Outcome != FightOutcome.Ongoing;
        string paused = sim.Paused ? "PAUSED" : "running";
        string frenzy = sim.Boss.FrenzyBp > 0 ? $"   Frenzy +{sim.Boss.FrenzyBp}bp" : "";
        string party = PartyPreset.Name(_bridge.Preset);
        _status.Text = ended
            ? $"{party}   Tick {sim.Tick}   {sim.Outcome}   Seed {_bridge.Seed}"
            : $"{party}   Tick {sim.Tick}   {paused}{frenzy}   Seed {_bridge.Seed}";
        _auto.Text = _bridge.AutoRunning ? "Stop" : "Auto-run";
        _pause.Text = sim.Paused ? "Resume" : "Pause";
        _bossTitle.Text = sim.Boss.Name;
        _resonance.Text = ResonanceReadout.Summarize(sim);
        _order.Text = OrderText(sim);
        if ((uint)_actor < (uint)sim.Heroes.Count)
        {
            _actorLabel.Text = sim.Heroes[_actor].Name;
        }

        for (int i = 0; i < sim.Heroes.Count && i < _lanes.Count; i++)
        {
            HeroState hero = sim.Heroes[i];
            string cast = hero.Casting ? AbilityCatalog.Get(hero.CastAbilityId).Name : "";
            _lanes[i].Set(hero.Name, ApGauge.Format(hero.Ap.Centi), hero.Ap.Centi, hero.Casting, hero.CastStartTick, hero.CastResolveTick, sim.Tick, cast);
            if (i < _heroes.Count)
            {
                _heroes[i].Set(hero, i == _actor);
            }
        }

        BossState boss = sim.Boss;
        string bossCast = boss.Casting ? AbilityCatalog.Get(boss.CastAbilityId).Name : "";
        int bossLane = sim.Heroes.Count;
        if (bossLane < _lanes.Count)
        {
            _lanes[bossLane].Set(boss.Name, ApGauge.Format(boss.Ap.Centi), boss.Ap.Centi, boss.Casting, boss.CastStartTick, boss.CastResolveTick, sim.Tick, bossCast);
        }

        for (int i = 0; i < boss.Parts.Length && i < _parts.Count; i++)
        {
            _parts[i].Set(boss.Parts[i], sim.Heroes, i == _part, boss.Def);
        }

        for (int i = 0; i < _allies.GetChildCount(); i++)
        {
            if (_allies.GetChild(i) is Button button)
            {
                if (i < sim.Heroes.Count)
                {
                    button.Text = ShortName(sim.Heroes[i].Name);
                }

                button.Modulate = i == _ally ? new Color("ffe08a") : Colors.White;
            }
        }

        _overlay.Visible = ended && !_picker.Visible;
        if (ended)
        {
            _overlayTitle.Text = sim.Outcome == FightOutcome.Victory ? "Victory" : "Defeat";
            int coreHp = boss.Parts.Length > 0 ? boss.Parts[0].Hp : 0;
            _overlayDetail.Text = sim.Outcome == FightOutcome.Victory
                ? $"The Core is destroyed. Tick {sim.Tick}."
                : $"The party failed. Tick {sim.Tick}. Core HP {coreHp}.";
        }
    }

    private void EnsureLanes(BattleSimulator sim)
    {
        int needed = sim.Heroes.Count + 1;
        if (_lanes.Count == needed)
        {
            return;
        }

        foreach (TimelineLane lane in _lanes)
        {
            lane.Root.QueueFree();
        }

        _lanes.Clear();
        for (int i = 0; i < needed; i++)
        {
            var lane = new TimelineLane(PortraitColors[i % PortraitColors.Length]);
            _lanes.Add(lane);
            _laneBox.AddChild(lane.Root);
        }
    }

    private void EnsureAbilities(BattleSimulator sim)
    {
        if (_abilityActor == _actor || (uint)_actor >= (uint)sim.Heroes.Count)
        {
            return;
        }

        _abilityActor = _actor;
        _hoveredAbility = -1;
        int[] kit = sim.Heroes[_actor].Kit;
        bool keep = false;
        for (int i = 0; i < kit.Length; i++)
        {
            if (kit[i] == _selectedAbility)
            {
                keep = true;
                break;
            }
        }

        if (!keep)
        {
            _selectedAbility = kit.Length > 0 ? kit[0] : -1;
        }

        while (_abilities.GetChildCount() > 0)
        {
            Node child = _abilities.GetChild(0);
            _abilities.RemoveChild(child);
            child.QueueFree();
        }

        foreach (int abilityId in kit)
        {
            AbilityDef ability = AbilityCatalog.Get(abilityId);
            int id = abilityId;
            var button = new Button
            {
                Text = ability.MpCost > 0 ? $"{ability.Name}  ({ability.MpCost} MP)" : ability.Name,
                Alignment = HorizontalAlignment.Left,
                CustomMinimumSize = new Vector2(0, 28),
            };
            button.AddThemeFontSizeOverride("font_size", 13);
            button.MouseEntered += () =>
            {
                _hoveredAbility = id;
                UpdateAbilityDetail();
            };
            button.MouseExited += () =>
            {
                if (_hoveredAbility == id)
                {
                    _hoveredAbility = -1;
                    UpdateAbilityDetail();
                }
            };
            button.Pressed += () =>
            {
                _selectedAbility = id;
                _hoveredAbility = -1;
                Queue(id);
            };
            _abilities.AddChild(button);
        }
    }

    private void UpdateAbilityDetail()
    {
        int id = _hoveredAbility >= 0 ? _hoveredAbility : _selectedAbility;
        if (id < 0)
        {
            _abilityDetail.Text = "Select an ability.";
        }
        else
        {
            BattleSimulator sim = _bridge.Simulation;
            int actor = (uint)_actor < (uint)sim.Heroes.Count ? _actor : 0;
            int ally = (uint)_ally < (uint)sim.Heroes.Count ? _ally : actor;
            int part = sim.Boss.Parts.Length == 0 || (uint)_part >= (uint)sim.Boss.Parts.Length ? 0 : _part;
            _abilityDetail.Text = AbilityBrief.Format(
                AbilityCatalog.Get(id),
                sim.Heroes[actor],
                sim.Heroes[ally],
                sim.Boss,
                part,
                sim.Tick,
                sim.Heroes);
        }
        for (int i = 0; i < _abilities.GetChildCount(); i++)
        {
            if (_abilities.GetChild(i) is Button button)
            {
                button.Modulate = ShownAbility(button.Text) ? new Color("ffe08a") : Colors.White;
            }
        }
    }

    private bool ShownAbility(string buttonText)
    {
        int id = _hoveredAbility >= 0 ? _hoveredAbility : _selectedAbility;
        if (id < 0)
        {
            return false;
        }

        return buttonText.StartsWith(AbilityCatalog.Get(id).Name, StringComparison.Ordinal);
    }

    private string OrderText(BattleSimulator sim)
    {
        string aim = sim.Boss.Parts.Length > _part ? sim.Boss.Parts[_part].Name : "Core";
        string actor = sim.Heroes[_actor].Name;
        string ally = sim.Heroes[_ally].Name;
        string queued = "No manual order yet.";
        if (sim.Commands.Count > 0)
        {
            RecordedCommand command = sim.Commands[sim.Commands.Count - 1];
            string name = AbilityCatalog.Get(command.AbilityId).Name;
            string part = command.TargetPart >= 0 && command.TargetPart < sim.Boss.Parts.Length
                ? sim.Boss.Parts[command.TargetPart].Name
                : "no part";
            queued = $"Last order: {sim.Heroes[command.HeroSlot].Name} {name} → {part} at tick {command.ApplyAtTick}.";
        }

        return $"{actor} aims at {aim}. Ally {ally}.  {queued}";
    }

    private static string ShortName(string name)
    {
        int space = name.IndexOf(' ');
        return space > 0 ? name[..space] : name;
    }

    private static float ApRatio(int centi)
    {
        int shown = centi / SimConst.CentiPerAp;
        if (shown < 0)
        {
            shown = 0;
        }

        if (shown > 10_000)
        {
            shown = 10_000;
        }

        return shown / 10_000f;
    }

    private sealed partial class ApRuler : Control
    {
        public ApRuler()
        {
            CustomMinimumSize = new Vector2(0, 14);
            MouseFilter = MouseFilterEnum.Ignore;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            for (int i = 0; i <= 4; i++)
            {
                var label = new Label
                {
                    Text = (i * 2_500).ToString(),
                    MouseFilter = MouseFilterEnum.Ignore,
                    VerticalAlignment = VerticalAlignment.Bottom,
                };
                label.AddThemeFontSizeOverride("font_size", 11);
                label.AddThemeColorOverride("font_color", new Color("8ea0b5"));
                float anchor = i / 4f;
                if (i == 0)
                {
                    label.AnchorLeft = 0;
                    label.AnchorRight = 0;
                    label.OffsetLeft = 0;
                    label.OffsetRight = 48;
                    label.HorizontalAlignment = HorizontalAlignment.Left;
                }
                else if (i == 4)
                {
                    label.AnchorLeft = 1;
                    label.AnchorRight = 1;
                    label.OffsetLeft = -56;
                    label.OffsetRight = 0;
                    label.HorizontalAlignment = HorizontalAlignment.Right;
                }
                else
                {
                    label.AnchorLeft = anchor;
                    label.AnchorRight = anchor;
                    label.OffsetLeft = -28;
                    label.OffsetRight = 28;
                    label.HorizontalAlignment = HorizontalAlignment.Center;
                }

                label.AnchorTop = 0;
                label.AnchorBottom = 1;
                AddChild(label);
            }
        }
    }

    private sealed class TimelineLane
    {
        private readonly Label _name;
        private readonly Label _detail;
        private readonly ColorRect _pip;
        private readonly ColorRect _marker;
        private readonly ColorRect _castFill;
        private readonly Label _castName;
        private int _centi;
        private bool _casting;
        private int _elapsed;
        private int _span;

        public TimelineLane(Color color)
        {
            Root = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            Root.AddThemeConstantOverride("separation", Gap);

            Root.AddChild(new ColorRect
            {
                Color = color,
                CustomMinimumSize = new Vector2(Swatch, Swatch),
                MouseFilter = MouseFilterEnum.Ignore,
            });

            var pipSlot = new Control
            {
                CustomMinimumSize = new Vector2(Pip, Pip),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _pip = new ColorRect
            {
                Color = new Color("e74c3c"),
                Visible = false,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _pip.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            pipSlot.AddChild(_pip);
            Root.AddChild(pipSlot);

            _name = new Label
            {
                CustomMinimumSize = new Vector2(NameCol, 0),
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _name.AddThemeFontSizeOverride("font_size", 12);
            Root.AddChild(_name);

            _detail = new Label
            {
                CustomMinimumSize = new Vector2(ApCol, 0),
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            _detail.AddThemeFontSizeOverride("font_size", 12);
            _detail.AddThemeColorOverride("font_color", new Color("d5dde8"));
            Root.AddChild(_detail);

            Track = new Control
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(0, 14),
                ClipContents = true,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            var backdrop = new ColorRect
            {
                Color = new Color("10151c"),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            Track.AddChild(backdrop);

            for (int i = 0; i <= 4; i++)
            {
                float anchor = i / 4f;
                var tick = new ColorRect
                {
                    Color = new Color("2c3848"),
                    MouseFilter = MouseFilterEnum.Ignore,
                    AnchorLeft = anchor,
                    AnchorRight = anchor,
                    AnchorTop = 0,
                    AnchorBottom = 1,
                    OffsetLeft = 0,
                    OffsetRight = 1,
                };
                Track.AddChild(tick);
            }

            _castFill = new ColorRect
            {
                Color = new Color(color.R, color.G, color.B, 0.38f),
                Visible = false,
                MouseFilter = MouseFilterEnum.Ignore,
                AnchorLeft = 0,
                AnchorRight = 0,
                AnchorTop = 0,
                AnchorBottom = 1,
                OffsetTop = 1,
                OffsetBottom = -1,
            };
            Track.AddChild(_castFill);

            _castName = new Label
            {
                Visible = false,
                MouseFilter = MouseFilterEnum.Ignore,
                VerticalAlignment = VerticalAlignment.Center,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                AnchorLeft = 0,
                AnchorRight = 1,
                AnchorTop = 0,
                AnchorBottom = 1,
                OffsetLeft = 6,
                OffsetRight = -6,
            };
            _castName.AddThemeFontSizeOverride("font_size", 11);
            _castName.AddThemeColorOverride("font_color", new Color("f4f7fb"));
            Track.AddChild(_castName);

            _marker = new ColorRect
            {
                Color = color,
                MouseFilter = MouseFilterEnum.Ignore,
                AnchorLeft = 0,
                AnchorRight = 0,
                AnchorTop = 0,
                AnchorBottom = 1,
                OffsetTop = 1,
                OffsetBottom = -1,
            };
            Track.AddChild(_marker);
            Root.AddChild(Track);
            Track.Resized += Place;
        }

        public HBoxContainer Root { get; }

        public Control Track { get; }

        public void Set(string name, string ap, int centi, bool casting, int start, int resolve, int tick, string castName)
        {
            _name.Text = name;
            _detail.Text = ap;
            _centi = centi;
            _casting = casting;
            _pip.Visible = casting;
            _castName.Visible = casting;
            _castName.Text = castName;
            _span = resolve - start;
            if (_span < 1)
            {
                _span = 1;
            }

            _elapsed = tick - start;
            if (_elapsed < 0)
            {
                _elapsed = 0;
            }

            if (_elapsed > _span)
            {
                _elapsed = _span;
            }

            Place();
        }

        private void Place()
        {
            float width = Track.Size.X;
            if (width < 4)
            {
                return;
            }

            float ratio = ApRatio(_centi);
            float x = ratio * (width - 3f);
            _marker.OffsetLeft = x;
            _marker.OffsetRight = x + 3f;
            _castFill.Visible = _casting;
            if (!_casting)
            {
                return;
            }

            float progress = _span <= 0 ? 0f : _elapsed / (float)_span;
            _castFill.OffsetRight = Mathf.Max(2f, width * progress);
        }
    }

    private sealed class HeroRow
    {
        private readonly StyleBoxFlat _style;
        private readonly Label _name;
        private readonly ProgressBar _hp;
        private readonly ProgressBar _mp;
        private readonly Label _meta;

        public HeroRow(Color color)
        {
            _style = new StyleBoxFlat
            {
                BgColor = new Color("1b2230"),
                ContentMarginLeft = 6,
                ContentMarginRight = 6,
                ContentMarginTop = 3,
                ContentMarginBottom = 3,
                BorderColor = new Color("00000000"),
            };
            Panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Stop,
                MouseDefaultCursorShape = CursorShape.PointingHand,
            };
            Panel.AddThemeStyleboxOverride("panel", _style);
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 6);
            Panel.AddChild(row);
            row.AddChild(new ColorRect
            {
                Color = color,
                CustomMinimumSize = new Vector2(8, 24),
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            });
            var box = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            box.AddThemeConstantOverride("separation", 1);
            row.AddChild(box);
            _name = new Label
            {
                MouseFilter = MouseFilterEnum.Ignore,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            _name.AddThemeFontSizeOverride("font_size", 14);
            box.AddChild(_name);
            _hp = Bar(new Color("c0392b"), 7);
            box.AddChild(_hp);
            _mp = Bar(new Color("2980b9"), 7);
            box.AddChild(_mp);
            _meta = new Label
            {
                MouseFilter = MouseFilterEnum.Ignore,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            _meta.AddThemeFontSizeOverride("font_size", 12);
            _meta.AddThemeColorOverride("font_color", new Color("c5d0dc"));
            box.AddChild(_meta);
        }

        public PanelContainer Panel { get; }

        public void Set(HeroState hero, bool selected)
        {
            _style.BorderColor = selected ? new Color("ffe08a") : new Color("00000000");
            int width = selected ? 2 : 0;
            _style.BorderWidthLeft = width;
            _style.BorderWidthRight = width;
            _style.BorderWidthTop = width;
            _style.BorderWidthBottom = width;
            _style.BgColor = selected ? new Color("243044") : new Color("1b2230");
            string down = hero.IsAlive ? "" : "  DOWN";
            _name.Text = hero.Name + down;
            _hp.MaxValue = hero.MaxHp;
            _hp.Value = hero.Hp < 0 ? 0 : hero.Hp;
            _mp.MaxValue = hero.MaxMp <= 0 ? 1 : hero.MaxMp;
            _mp.Value = hero.Mp;
            string cast = hero.Casting ? $"  cast → {hero.CastResolveTick}" : "";
            _meta.Text = $"HP {hero.Hp}/{hero.MaxHp}  MP {hero.Mp}/{hero.MaxMp}  AP {ApGauge.Format(hero.Ap.Centi)}{cast}";
        }

        private static ProgressBar Bar(Color fill, int height)
        {
            var bar = new ProgressBar
            {
                MinValue = 0,
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(0, height),
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            bar.AddThemeStyleboxOverride("background", Flat(new Color("0c1016"), 0));
            bar.AddThemeStyleboxOverride("fill", Flat(fill, 0));
            return bar;
        }
    }

    private sealed class PartRow
    {
        private readonly StyleBoxFlat _style;
        private readonly Label _name;
        private readonly ProgressBar _hp;
        private readonly Label _resist;
        private readonly Label _pressure;
        private readonly ProgressBar[] _threat;
        private readonly Label[] _threatLabel;

        public PartRow(Color[] colors)
        {
            _style = new StyleBoxFlat
            {
                BgColor = new Color("1b2230"),
                ContentMarginLeft = 6,
                ContentMarginRight = 6,
                ContentMarginTop = 3,
                ContentMarginBottom = 3,
                BorderColor = new Color("00000000"),
            };
            Panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Stop,
                MouseDefaultCursorShape = CursorShape.PointingHand,
            };
            Panel.AddThemeStyleboxOverride("panel", _style);
            var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            box.AddThemeConstantOverride("separation", 2);
            Panel.AddChild(box);
            _name = new Label
            {
                MouseFilter = MouseFilterEnum.Ignore,
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            _name.AddThemeFontSizeOverride("font_size", 13);
            box.AddChild(_name);
            _hp = new ProgressBar
            {
                MinValue = 0,
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(0, 10),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            _hp.AddThemeStyleboxOverride("background", Flat(new Color("0c1016"), 0));
            _hp.AddThemeStyleboxOverride("fill", Flat(new Color("a04040"), 0));
            box.AddChild(_hp);
            _resist = Note(new Color("d5e2c4"));
            box.AddChild(_resist);
            _pressure = Note(new Color("b7c4d4"));
            box.AddChild(_pressure);

            var threats = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            threats.AddThemeConstantOverride("separation", 6);
            box.AddChild(threats);
            _threat = new ProgressBar[4];
            _threatLabel = new Label[4];
            for (int i = 0; i < 4; i++)
            {
                var column = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                column.AddThemeConstantOverride("separation", 1);
                threats.AddChild(column);
                _threatLabel[i] = new Label
                {
                    MouseFilter = MouseFilterEnum.Ignore,
                    ClipText = true,
                    TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
                };
                _threatLabel[i].AddThemeFontSizeOverride("font_size", 11);
                column.AddChild(_threatLabel[i]);
                _threat[i] = new ProgressBar
                {
                    MinValue = 0,
                    MaxValue = 30_000,
                    ShowPercentage = false,
                    CustomMinimumSize = new Vector2(0, 6),
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                _threat[i].AddThemeStyleboxOverride("background", Flat(new Color("0c1016"), 0));
                _threat[i].AddThemeStyleboxOverride("fill", Flat(colors[i % colors.Length], 0));
                column.AddChild(_threat[i]);
            }
        }

        public PanelContainer Panel { get; }

        public void Set(BossPartState part, IReadOnlyList<HeroState> heroes, bool selected, int bossDef)
        {
            _style.BorderColor = selected ? new Color("ffe08a") : new Color("00000000");
            int width = selected ? 2 : 0;
            _style.BorderWidthLeft = width;
            _style.BorderWidthRight = width;
            _style.BorderWidthTop = width;
            _style.BorderWidthBottom = width;
            _style.BgColor = selected ? new Color("243044") : new Color("1b2230");
            string state = part.Hp <= 0 ? "destroyed" : part.Active ? "up" : "down";
            int def = bossDef + part.DefBonus;
            _name.Text = $"{part.Name}  {part.Hp}/{part.MaxHp}  DEF {def}  {state}";
            string resist = ResonanceReadout.ResistLine(part);
            _resist.Visible = resist.Length > 0;
            _resist.Text = resist;
            _pressure.Visible = part.Pressure.Length > 0;
            _pressure.Text = part.Pressure;
            _hp.MaxValue = part.MaxHp <= 0 ? 1 : part.MaxHp;
            _hp.Value = part.Hp < 0 ? 0 : part.Hp;
            for (int i = 0; i < _threat.Length; i++)
            {
                if (i >= heroes.Count || i >= part.Enmity.Length)
                {
                    _threat[i].Visible = false;
                    _threatLabel[i].Visible = false;
                    continue;
                }

                int total = part.Enmity[i].Total;
                if (total > 30_000)
                {
                    total = 30_000;
                }

                _threat[i].Visible = true;
                _threat[i].Value = total;
                _threatLabel[i].Visible = true;
                _threatLabel[i].Text = $"{heroes[i].Name.Split(' ')[0]} {total}";
            }
        }

        private static Label Note(Color color)
        {
            var label = new Label
            {
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MaxLinesVisible = 2,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            label.AddThemeFontSizeOverride("font_size", 12);
            label.AddThemeColorOverride("font_color", color);
            return label;
        }
    }
}
