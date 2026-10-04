using Godot;
using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Sim;

namespace Resonance.Game;

/// <summary>
/// Grey-box Carapace Engine fight. Plain Control nodes and flat colors only.
/// Full-rect columns: party, boss parts, a scrolling resonance readout,
/// and a pinned ability panel, then the action bar. Each card shows a CTB charge bar.
/// </summary>
public partial class GreyBoxBattle : Control
{
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
    private Label _banner = null!;
    private BossCard _bossCard = null!;
    private Label _resonance = null!;
    private Label _abilityDetail = null!;
    private Label _order = null!;
    private Label _actorLabel = null!;
    private readonly List<HeroRow> _heroes = new();
    private readonly List<PartRow> _parts = new();
    private VBoxContainer _partBox = null!;
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
    private Button _replay = null!;
    private Button _change = null!;
    private Button _continue = null!;
    private int _actor;
    private int _part;
    private int _ally = 2;
    private int _abilityActor = -1;
    private int _selectedAbility = -1;
    private int _hoveredAbility = -1;
    private bool _fitting;
    private bool _view3d;
    private SubViewportContainer _arenaHost = null!;
    private SubViewport _arenaViewport = null!;
    private ArenaView _arena = null!;
    private HBoxContainer _bodyHost = null!;
    private HBoxContainer _columns = null!;
    private HBoxContainer _band = null!;
    private Label _hint = null!;
    private Button _viewToggle = null!;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        Theme = HudSkin.BattleTheme();
        ConfigureWindowStretch();
        FillViewport(this);
        BuildArena();

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
        _log.AddThemeStyleboxOverride("normal", HudSkin.Ability());
        _log.AddThemeFontSizeOverride("normal_font_size", 12);
        _log.AddThemeColorOverride("default_color", new Color("c5d0dc"));
        root.AddChild(_log);

        BuildOverlay();
        BuildPicker();

        _bridge.LogLine += line => _log.AppendText($"[color=#9fd0ff]{line}[/color]\n");
        _bridge.LogCleared += () => _log.Clear();
        _bridge.StateChanged += Refresh;
        Refresh();
        _picker.Visible = false;
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

    private void BuildArena()
    {
        _arenaHost = new SubViewportContainer
        {
            Stretch = true,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
        _arenaHost.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _arenaHost.SizeFlagsVertical = SizeFlags.ExpandFill;
        _arenaHost.Visible = false;
        _arenaViewport = new SubViewport
        {
            OwnWorld3D = true,
            TransparentBg = false,
            HandleInputLocally = false,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
            Size = new Vector2I(1280, 720),
            Msaa3D = SubViewport.Msaa.Disabled,
        };
        _arenaHost.AddChild(_arenaViewport);
        _arena = new ArenaView();
        _arenaViewport.AddChild(_arena);
    }

    /// <summary>
    /// Boss fights default to the 3D arena. Floor fights stay on the grey-box board.
    /// Party, parts, and the ability panel sit in the band under the view.
    /// </summary>
    private void SetArenaView(bool show)
    {
        _view3d = show;
        _arenaHost.Visible = _view3d;
        _arenaViewport.RenderTargetUpdateMode = _view3d
            ? SubViewport.UpdateMode.Always
            : SubViewport.UpdateMode.Disabled;
        _arena.SetActive(_view3d);
        _background.Color = new Color("12161c");
        if (_hint != null)
        {
            _hint.Visible = !_view3d;
        }

        if (_view3d)
        {
            if (_columns.GetParent() != _band)
            {
                _columns.Reparent(_band, false);
            }

            _band.Visible = true;
        }
        else if (_columns.GetParent() != _bodyHost)
        {
            _columns.Reparent(_bodyHost, false);
            _band.Visible = false;
        }
        else
        {
            _band.Visible = false;
        }

        _columns.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _columns.SizeFlagsVertical = SizeFlags.ExpandFill;
        _log.SizeFlagsVertical = _view3d ? SizeFlags.ShrinkEnd : SizeFlags.ExpandFill;
        _log.CustomMinimumSize = new Vector2(0, _view3d ? 40 : 52);
        if (_viewToggle != null)
        {
            _viewToggle.Visible = _bridge.BossOnly || _view3d;
            _viewToggle.Text = _view3d ? "Grey-box" : "3D arena";
        }
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

    public override void _Process(double delta)
    {
        if (ArenaVfx.HoldFrames && _overlay != null && _overlay.Visible)
        {
            _overlay.Visible = false;
        }
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
        _banner = new Label { Text = "Project Resonance  ·  Grey-Box Battle" };
        _banner.AddThemeFontSizeOverride("font_size", 15);
        top.AddChild(_banner);

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
        _viewToggle = new Button { Text = "3D arena", Visible = false };
        _viewToggle.Pressed += () =>
        {
            if (!_bridge.BossOnly)
            {
                return;
            }

            SetArenaView(!_view3d);
            Refresh();
        };
        top.AddChild(_viewToggle);
        header.AddChild(top);

        _hint = new Label
        {
            Text = "Space pauses. Boss fights open in the 3D arena. Click a hero, a part, then an ability.",
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        _hint.AddThemeFontSizeOverride("font_size", 12);
        _hint.AddThemeColorOverride("font_color", new Color("8ea0b5"));
        header.AddChild(_hint);
        return header;
    }

    private Control BuildBody()
    {
        var wrap = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 5,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        wrap.AddThemeConstantOverride("separation", 4);

        _bodyHost = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _bodyHost.AddThemeConstantOverride("separation", 6);
        _columns = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _columns.AddThemeConstantOverride("separation", 6);
        _bodyHost.AddChild(_arenaHost);
        _bodyHost.AddChild(_columns);

        BattleSimulator sim = _bridge.Simulation;
        _columns.AddChild(BuildHeroColumn(sim));
        _columns.AddChild(BuildPartColumn(sim));
        _columns.AddChild(BuildInfoColumn());
        wrap.AddChild(_bodyHost);

        _band = new HBoxContainer
        {
            Visible = false,
            CustomMinimumSize = new Vector2(0, 168),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        wrap.AddChild(_band);
        return wrap;
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
        _bossCard = new BossCard();
        column.AddChild(_bossCard.Panel);

        var partScroll = VerticalScroll();
        column.AddChild(partScroll);
        _partBox = Stack();
        partScroll.AddChild(_partBox);

        for (int i = 0; i < sim.Boss.Parts.Length; i++)
        {
            int index = i;
            var row = new PartRow(PortraitColors);
            row.Panel.GuiInput += @event => ClickPart(@event, index);
            _parts.Add(row);
            _partBox.AddChild(row.Panel);
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
        resonanceScroll.SizeFlagsStretchRatio = 0.65f;
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
            SizeFlagsStretchRatio = 2.6f,
        };
        abilityPanel.AddThemeStyleboxOverride("panel", HudSkin.Ability());
        var abilityScroll = VerticalScroll();
        abilityPanel.AddChild(abilityScroll);
        _abilityDetail = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            Text = "Select an ability.",
        };
        _abilityDetail.AddThemeFontSizeOverride("font_size", 11);
        _abilityDetail.AddThemeConstantOverride("line_spacing", 0);
        _abilityDetail.AddThemeColorOverride("font_color", new Color("d5dde8"));
        abilityScroll.AddChild(_abilityDetail);
        column.AddChild(abilityPanel);
        return column;
    }

    private Control BuildAction()
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", HudSkin.Ability());

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

        var finish = new Button { Text = "To end" };
        finish.Pressed += () => _bridge.RunToEnd();
        controls.AddChild(finish);

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
        panel.AddThemeStyleboxOverride("panel", HudSkin.Ability());
        center.AddChild(panel);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        panel.AddChild(box);

        _overlayTitle = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _overlayTitle.AddThemeFontSizeOverride("font_size", 28);
        box.AddChild(_overlayTitle);
        _overlayDetail = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        box.AddChild(_overlayDetail);
        _replay = new Button { Text = "Replay this party" };
        _replay.Pressed += Replay;
        box.AddChild(_replay);
        _change = new Button { Text = "Change party" };
        _change.Pressed += LeaveToParty;
        box.AddChild(_change);
        _continue = new Button { Text = "Continue", Visible = false };
        _continue.Pressed += ContinueFloor;
        box.AddChild(_continue);
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

        var back = new Button { Text = "Back to floor" };
        back.Pressed += ReturnToFloor;
        box.AddChild(back);
    }

    [Signal]
    public delegate void ReturnedToFloorEventHandler();

    [Signal]
    public delegate void ReturnedToPartyEventHandler();

    public void OpenFloorFight()
    {
        _actor = 0;
        _part = 0;
        _ally = 2;
        _abilityActor = -1;
        _picker.Visible = false;
        _overlay.Visible = false;
        SetArenaView(false);
        _bridge.LoadFloorFight();
    }

    public void OpenBossFight()
    {
        _actor = 0;
        _part = 0;
        _ally = 2;
        _abilityActor = -1;
        _picker.Visible = false;
        _overlay.Visible = false;
        SetArenaView(true);
        _bridge.StartBoss(_bridge.PartyIds);
    }

    public void ShowBossPicker() => OpenBossFight();

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
        _bridge.StartBoss(_bridge.PartyIds);
    }

    private void Restart()
    {
        if (_bridge.InFloorFight && _bridge.Simulation.Outcome == FightOutcome.Ongoing)
        {
            _actor = 0;
            _part = 0;
            _ally = 2;
            _abilityActor = -1;
            _bridge.LoadFloorFight();
            return;
        }

        if (_bridge.BossOnly && _bridge.Simulation.Outcome == FightOutcome.Ongoing)
        {
            _actor = 0;
            _part = 0;
            _ally = 2;
            _abilityActor = -1;
            _bridge.StartBoss(_bridge.PartyIds);
            return;
        }

        LeaveToParty();
    }

    private void LeaveToParty()
    {
        if (_bridge.AutoRunning)
        {
            _bridge.ToggleAuto();
        }

        _picker.Visible = false;
        _overlay.Visible = false;
        EmitSignal(SignalName.ReturnedToParty);
    }

    private void ContinueFloor()
    {
        if (_bridge.AutoRunning)
        {
            _bridge.ToggleAuto();
        }

        _bridge.CommitFloorFight();
        EmitSignal(SignalName.ReturnedToFloor);
    }

    private void ReturnToFloor()
    {
        if (_bridge.AutoRunning)
        {
            _bridge.ToggleAuto();
        }

        _picker.Visible = false;
        _overlay.Visible = false;
        EmitSignal(SignalName.ReturnedToFloor);
    }

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
        EnsureParts(sim);
        EnsureAbilities(sim);
        UpdateAbilityDetail();

        if (_view3d && !_bridge.BossOnly)
        {
            SetArenaView(false);
        }

        bool ended = sim.Outcome != FightOutcome.Ongoing;
        string paused = sim.Paused ? "PAUSED" : "running";
        string frenzy = sim.Boss.FrenzyBp > 0 ? $"   Frenzy +{sim.Boss.FrenzyBp}bp" : "";
        string party = _bridge.PartyLabel;
        _status.Text = ended
            ? $"{party}   Tick {sim.Tick}   {sim.Outcome}   Seed {_bridge.Seed}"
            : $"{party}   Tick {sim.Tick}   {paused}{frenzy}   Seed {_bridge.Seed}";
        _auto.Text = _bridge.AutoRunning ? "Stop" : "Auto-run";
        _pause.Text = sim.Paused ? "Resume" : "Pause";
        string viewName = _view3d ? "3D Arena" : "Grey-Box Battle";
        _banner.Text = $"Project Resonance  ·  {viewName}  ·  {sim.Boss.Name}";
        if (_viewToggle != null)
        {
            _viewToggle.Visible = _bridge.BossOnly;
            _viewToggle.Text = _view3d ? "Grey-box" : "3D arena";
        }
        _resonance.Text = ResonanceReadout.Summarize(sim);
        _order.Text = OrderText(sim);
        if ((uint)_actor < (uint)sim.Heroes.Count)
        {
            _actorLabel.Text = sim.Heroes[_actor].Name;
        }

        for (int i = 0; i < sim.Heroes.Count && i < _heroes.Count; i++)
        {
            HeroState hero = sim.Heroes[i];
            _heroes[i].Set(hero, i == _actor, sim.Tick, HeroChargeBp(hero, sim.Tick));
        }

        BossState boss = sim.Boss;
        int bossCharge = BossChargeBp(boss, sim.Tick);
        _bossCard.Set(boss.Name, bossCharge);

        for (int i = 0; i < boss.Parts.Length && i < _parts.Count; i++)
        {
            _parts[i].Set(boss.Parts[i], sim.Heroes, i == _part, boss.Def, sim.Tick, bossCharge);
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

        bool floorFight = _bridge.InFloorFight;
        _replay.Visible = !floorFight;
        _change.Visible = !floorFight;
        _continue.Visible = floorFight;
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

    private void EnsureParts(BattleSimulator sim)
    {
        if (_parts.Count == sim.Boss.Parts.Length)
        {
            return;
        }

        foreach (PartRow row in _parts)
        {
            row.Panel.QueueFree();
        }

        _parts.Clear();
        for (int i = 0; i < sim.Boss.Parts.Length; i++)
        {
            int index = i;
            var row = new PartRow(PortraitColors);
            row.Panel.GuiInput += @event => ClickPart(@event, index);
            _parts.Add(row);
            _partBox.AddChild(row.Panel);
        }

        if (_part >= sim.Boss.Parts.Length)
        {
            _part = 0;
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

    private static int HeroChargeBp(HeroState hero, int tick)
    {
        if (!hero.IsAlive)
        {
            return 0;
        }

        int gain = Formulas.AgiCentiPerTick(hero.Agi, 0, 0);
        return ApGauge.NextActionChargeBp(hero.Ap.Centi, gain, hero.Casting, hero.CastStartTick, hero.CastResolveTick, tick);
    }

    private static int BossChargeBp(BossState boss, int tick)
    {
        int gain = Formulas.AgiCentiPerTick(boss.Agi, 0, boss.SlowNow(tick));
        return ApGauge.NextActionChargeBp(boss.Ap.Centi, gain, boss.Casting, boss.CastStartTick, boss.CastResolveTick, tick);
    }

    private readonly record struct ChargeWidgets(Control Row, ProgressBar Bar, Label Turn);

    private static readonly Color SelectedTint = new("ffe08a");

    private static ChargeWidgets BuildChargeRow(VBoxContainer box, bool boss)
    {
        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 6);
        var caption = new Label
        {
            Text = "CTB",
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(28, 0),
        };
        caption.AddThemeFontSizeOverride("font_size", 10);
        caption.AddThemeColorOverride("font_color", boss ? new Color("e7b4ee") : new Color("9ee7f2"));
        row.AddChild(caption);

        var bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = SimConst.Bp,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 9),
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        row.AddChild(bar);

        var turn = new Label
        {
            Text = "TURN",
            Visible = false,
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
        };
        turn.AddThemeFontSizeOverride("font_size", 10);
        row.AddChild(turn);
        box.AddChild(row);
        PaintCharge(bar, turn, 0, boss);
        return new ChargeWidgets(row, bar, turn);
    }

    /// <summary>
    /// Selection tints the whole card gold. The charge row cancels that tint so cyan and magenta stay readable.
    /// </summary>
    private static void UntintCharge(Control row, bool selected)
    {
        row.Modulate = selected
            ? new Color(1f / SelectedTint.R, 1f / SelectedTint.G, 1f / SelectedTint.B)
            : Colors.White;
    }

    private static void PaintCharge(ProgressBar bar, Label turn, int chargeBp, bool boss)
    {
        if (chargeBp < 0)
        {
            chargeBp = 0;
        }

        if (chargeBp > SimConst.Bp)
        {
            chargeBp = SimConst.Bp;
        }

        bool ready = chargeBp >= SimConst.Bp;
        Color fill = boss
            ? (ready ? new Color("ffd6fb") : new Color("e13cff"))
            : (ready ? new Color("d7fbff") : new Color("00e5ff"));
        Color edge = boss
            ? (ready ? new Color("ff7af0") : new Color("5c2a62"))
            : (ready ? new Color("7af6ff") : new Color("1a5566"));
        bar.MaxValue = SimConst.Bp;
        bar.Value = chargeBp;
        bar.AddThemeStyleboxOverride("background", ChargeTrack(new Color("070b10"), edge));
        bar.AddThemeStyleboxOverride("fill", ChargeFill(fill));
        turn.Visible = ready;
        turn.AddThemeColorOverride("font_color", fill);
    }

    private static StyleBoxFlat ChargeTrack(Color bg, Color edge)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = edge,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            ContentMarginLeft = 1,
            ContentMarginTop = 1,
            ContentMarginRight = 1,
            ContentMarginBottom = 1,
        };
    }

    private static StyleBoxFlat ChargeFill(Color fill)
    {
        return new StyleBoxFlat { BgColor = fill };
    }

    private sealed class HeroRow
    {
        private readonly Label _name;
        private readonly ProgressBar _hp;
        private readonly ProgressBar _mp;
        private readonly Control _chargeRow;
        private readonly ProgressBar _charge;
        private readonly Label _turn;
        private readonly Label _meta;
        private readonly TextureRect _icon;

        public HeroRow(Color color)
        {
            Panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Stop,
                MouseDefaultCursorShape = CursorShape.PointingHand,
            };
            Panel.AddThemeStyleboxOverride("panel", HudSkin.PartyCard());
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
            _icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(18, 18),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = false,
                TextureFilter = TextureFilterEnum.Linear,
            };
            row.AddChild(_icon);
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
            ChargeWidgets charge = BuildChargeRow(box, boss: false);
            _chargeRow = charge.Row;
            _charge = charge.Bar;
            _turn = charge.Turn;
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

        public void Set(HeroState hero, bool selected, int tick, int chargeBp)
        {
            Panel.Modulate = selected ? new Color("ffe08a") : Colors.White;
            string down = hero.IsAlive ? "" : "  DOWN";
            bool shielded = hero.Absorb > 0 && tick < hero.AbsorbExpires;
            bool regen = hero.RegenPerPulse > 0 && tick < hero.RegenExpires;
            _icon.Visible = shielded || regen;
            _icon.Texture = shielded ? HudSkin.Status("shield") : HudSkin.Status("regen");
            bool turn = hero.IsAlive && chargeBp >= SimConst.Bp;
            _name.AddThemeColorOverride("font_color", turn ? new Color("d7fbff") : new Color("f4f7fb"));
            _name.Text = shielded ? $"shield {hero.Absorb}  {hero.Name}{down}" : hero.Name + down;
            UntintCharge(_chargeRow, selected);
            PaintCharge(_charge, _turn, chargeBp, boss: false);
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
        private readonly Label _name;
        private readonly TextureRect _icon;
        private readonly TextureRect _chain;
        private readonly ProgressBar _hp;
        private readonly Control _chargeRow;
        private readonly ProgressBar _charge;
        private readonly Label _turn;
        private readonly Label _resist;
        private readonly Label _pressure;
        private readonly ProgressBar[] _threat;
        private readonly Label[] _threatLabel;

        public PartRow(Color[] colors)
        {
            Panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Stop,
                MouseDefaultCursorShape = CursorShape.PointingHand,
            };
            Panel.AddThemeStyleboxOverride("panel", HudSkin.BossPart());
            var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            box.AddThemeConstantOverride("separation", 2);
            Panel.AddChild(box);
            var title = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            title.AddThemeConstantOverride("separation", 4);
            box.AddChild(title);
            _icon = StatusIcon();
            title.AddChild(_icon);
            _chain = StatusIcon();
            title.AddChild(_chain);
            _name = new Label
            {
                MouseFilter = MouseFilterEnum.Ignore,
                ClipText = true,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            };
            _name.AddThemeFontSizeOverride("font_size", 13);
            title.AddChild(_name);
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
            ChargeWidgets charge = BuildChargeRow(box, boss: true);
            _chargeRow = charge.Row;
            _charge = charge.Bar;
            _turn = charge.Turn;
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

        public void Set(BossPartState part, IReadOnlyList<HeroState> heroes, bool selected, int bossDef, int tick, int chargeBp)
        {
            Panel.Modulate = selected ? new Color("ffe08a") : Colors.White;
            bool turn = chargeBp >= SimConst.Bp;
            _name.AddThemeColorOverride("font_color", turn ? new Color("ffd6fb") : new Color("f4f7fb"));
            UntintCharge(_chargeRow, selected);
            PaintCharge(_charge, _turn, chargeBp, boss: true);
            bool stunned = part.Hp > 0 && tick < part.StunExpires;
            _icon.Visible = stunned;
            _icon.Texture = HudSkin.Status("stun");
            Texture2D? chain = HudSkin.Chain(part.Property);
            _chain.Visible = chain != null;
            _chain.Texture = chain;
            string state = part.Hp <= 0 ? "destroyed" : part.Active ? "up" : "down";
            if (part.Hp > 0 && tick < part.StunExpires)
            {
                state = $"stunned {part.StunExpires - tick}";
            }
            else if (part.Hp > 0 && tick < part.StunImmuneUntil)
            {
                state = $"stun immune {part.StunImmuneUntil - tick}";
            }

            int def = bossDef + part.DefBonus;
            _name.Text = part.Hp > 0 && (tick < part.StunExpires || tick < part.StunImmuneUntil)
                ? $"{part.Name}  {state}  {part.Hp}/{part.MaxHp}  DEF {def}"
                : $"{part.Name}  {part.Hp}/{part.MaxHp}  DEF {def}  {state}";
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

        private static TextureRect StatusIcon()
        {
            return new TextureRect
            {
                CustomMinimumSize = new Vector2(18, 18),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
                Visible = false,
                TextureFilter = TextureFilterEnum.Linear,
            };
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

    private sealed class BossCard
    {
        private readonly Label _name;
        private readonly ProgressBar _charge;
        private readonly Label _turn;
        private readonly StyleBoxFlat _panelStyle;

        public BossCard()
        {
            _panelStyle = new StyleBoxFlat
            {
                BgColor = new Color("141018"),
                BorderColor = new Color("3a2a44"),
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                ContentMarginLeft = 8,
                ContentMarginTop = 4,
                ContentMarginRight = 8,
                ContentMarginBottom = 5,
                CornerRadiusTopLeft = 2,
                CornerRadiusTopRight = 2,
                CornerRadiusBottomRight = 2,
                CornerRadiusBottomLeft = 2,
            };
            Panel = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            Panel.AddThemeStyleboxOverride("panel", _panelStyle);
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
            _name.AddThemeColorOverride("font_color", new Color("e7d5ee"));
            box.AddChild(_name);
            ChargeWidgets charge = BuildChargeRow(box, boss: true);
            _charge = charge.Bar;
            _turn = charge.Turn;
        }

        public PanelContainer Panel { get; }

        public void Set(string name, int chargeBp)
        {
            bool turn = chargeBp >= SimConst.Bp;
            _name.Text = name;
            _name.AddThemeColorOverride("font_color", turn ? new Color("ffd6fb") : new Color("e7d5ee"));
            _panelStyle.BgColor = turn ? new Color("241428") : new Color("141018");
            _panelStyle.BorderColor = turn ? new Color("ff7af0") : new Color("3a2a44");
            Panel.AddThemeStyleboxOverride("panel", _panelStyle);
            PaintCharge(_charge, _turn, chargeBp, boss: true);
        }
    }
}
