using Godot;
using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Sim;

namespace Resonance.Game;

/// <summary>
/// Grey-box Carapace Engine fight. Plain Control nodes and flat colors only.
/// In the 3D arena the resonance readout and ability detail are a full-height
/// column beside the view. The four party cards stack on the view's other side,
/// and the boss parts sit under the view. Nothing in those panels scrolls.
/// Each card keeps its CTB charge bar.
/// </summary>
public partial class GreyBoxBattle : Control
{
    private const int PartyColumnWidth = 336;
    private const int InfoColumnWidth = 340;
    private const int ResonanceBlock = 280;
    private const int ActionBarHeight = 108;
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
    private PanelContainer _abilityPanel = null!;
    private Label _abilityDetail = null!;
    private Label _order = null!;
    private Label _actorLabel = null!;
    private readonly List<HeroRow> _heroes = new();
    private readonly List<PartRow> _parts = new();
    private HBoxContainer _partBox = null!;
    private GridContainer _heroBox = null!;
    private HBoxContainer _viewRow = null!;
    private Container _abilities = null!;
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
    private VBoxContainer _left = null!;
    private VBoxContainer _stage = null!;
    private VBoxContainer _band = null!;
    private VBoxContainer _info = null!;
    private Label _hint = null!;
    private Button _viewToggle = null!;
    private PanelContainer _action = null!;
    private bool _layoutLock;
    private int _layoutLockFrame;
    private int _layoutLockSamples;
    private readonly List<(string Name, int X, int Y, int W, int H)> _layoutLockBase = new();
    private readonly System.Text.StringBuilder _layoutLockErrors = new();

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

        var root = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddThemeConstantOverride("separation", 6);
        _margin.AddChild(root);

        _left = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _left.AddThemeConstantOverride("separation", 2);
        root.AddChild(_left);
        _left.AddChild(BuildHeader());
        _left.AddChild(BuildStage());
        _left.AddChild(BuildAction());

        _log = new RichTextLabel
        {
            Name = "CombatLog",
            BbcodeEnabled = true,
            ScrollFollowing = true,
            FitContent = false,
            ClipContents = true,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 52),
        };
        _log.AddThemeStyleboxOverride("normal", HudSkin.Ability());
        _log.AddThemeFontSizeOverride("normal_font_size", 12);
        _log.AddThemeColorOverride("default_color", new Color("c5d0dc"));
        _left.AddChild(_log);
        root.AddChild(BuildInfoColumn());

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
    /// The resonance column stays full height. In the arena the party stacks beside
    /// the view; on the grey-box board it returns to a row above the boss parts.
    /// </summary>
    private void SetArenaView(bool show)
    {
        _view3d = show;
        _arenaHost.Visible = _view3d;
        _arenaViewport.RenderTargetUpdateMode = _view3d
            ? SubViewport.UpdateMode.Always
            : SubViewport.UpdateMode.Disabled;
        _arena.SetActive(_view3d);
        if (DisplayServer.GetName() == "headless")
        {
            _arenaViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
        }

        _background.Color = new Color("12161c");
        if (_hint != null)
        {
            _hint.Visible = !_view3d;
        }

        _band.Visible = true;
        _band.SizeFlagsVertical = _view3d ? SizeFlags.ShrinkEnd : SizeFlags.ExpandFill;
        _stage.SizeFlagsVertical = SizeFlags.ExpandFill;
        if (_view3d)
        {
            if (_heroBox.GetParent() != _viewRow)
            {
                _heroBox.Reparent(_viewRow, false);
                _viewRow.MoveChild(_heroBox, 0);
            }

            _heroBox.Columns = 1;
            _heroBox.CustomMinimumSize = new Vector2(PartyColumnWidth, 0);
            _heroBox.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        }
        else
        {
            if (_heroBox.GetParent() != _band)
            {
                _heroBox.Reparent(_band, false);
                _band.MoveChild(_heroBox, 0);
            }

            _heroBox.Columns = 4;
            _heroBox.CustomMinimumSize = new Vector2(0, 0);
            _heroBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        }
        _log.SizeFlagsVertical = _view3d ? SizeFlags.ShrinkEnd : SizeFlags.ExpandFill;
        _log.CustomMinimumSize = new Vector2(0, _view3d ? 22 : 52);
        _log.ClipContents = true;
        StyleBox logStyle = HudSkin.Ability();
        if (_view3d)
        {
            logStyle.ContentMarginLeft = 8;
            logStyle.ContentMarginRight = 8;
            logStyle.ContentMarginTop = 2;
            logStyle.ContentMarginBottom = 2;
        }

        _log.AddThemeStyleboxOverride("normal", logStyle);
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

        if (_layoutLock)
        {
            AdvanceLayoutLock();
        }
    }

    public static bool LayoutLockRequested(out int width, out int height)
    {
        width = 1280;
        height = 720;
        string raw = OS.GetEnvironment("HUD_LOCK");
        if (raw.Length == 0)
        {
            return false;
        }

        int split = raw.IndexOf('x');
        if (split > 0
            && int.TryParse(raw[..split], out int parsedWidth)
            && int.TryParse(raw[(split + 1)..], out int parsedHeight))
        {
            width = parsedWidth;
            height = parsedHeight;
        }

        return true;
    }

    public void BeginLayoutLock()
    {
        _layoutLock = true;
    }

    private void AdvanceLayoutLock()
    {
        _layoutLockFrame++;
        if (_layoutLockFrame < 4 || !_view3d)
        {
            return;
        }

        if (_layoutLockBase.Count == 0)
        {
            if (!_bridge.AutoRunning)
            {
                _bridge.ToggleAuto();
            }

            Refresh();
            CaptureLayoutLock(baseline: true);
            SaveLayoutShot("a");
            return;
        }

        BattleSimulator sim = _bridge.Simulation;
        if (sim.Outcome == FightOutcome.Ongoing)
        {
            for (int i = 0; i < 4; i++)
            {
                _bridge.StepOnce();
            }

            _actor = _layoutLockSamples % sim.Heroes.Count;
            _part = _layoutLockSamples % Math.Max(1, sim.Boss.Parts.Length);
            _abilityActor = -1;
            int[] kit = sim.Heroes[_actor].Kit;
            if (kit.Length > 0)
            {
                int abilityId = kit[_layoutLockSamples % kit.Length];
                _selectedAbility = abilityId;
                _hoveredAbility = -1;
                AbilityDef chosen = AbilityCatalog.Get(abilityId);
                if (_layoutLockSamples % 3 == 0 && chosen.Kind != AbilityKind.Support)
                {
                    _ally = (_actor + 1) % sim.Heroes.Count;
                    Queue(abilityId);
                }
            }

            Refresh();
        }

        CaptureLayoutLock(baseline: false);
        _layoutLockSamples++;
        if (_layoutLockSamples == 20)
        {
            SaveLayoutShot("b");
        }

        if (_layoutLockSamples >= 40 || sim.Outcome != FightOutcome.Ongoing)
        {
            FinishLayoutLock();
        }
    }

    private void CaptureLayoutLock(bool baseline)
    {
        (string Name, Control Control)[] panels =
        [
            ("arena", _arenaHost),
            ("party", _heroBox),
            ("band", _band),
            ("boss", _bossCard.Panel),
            ("parts", _partBox),
            ("resonance", _info),
            ("ability", _abilityPanel),
            ("action", _action),
            ("log", _log),
        ];

        var current = new List<(string Name, int X, int Y, int W, int H)>();
        void Add(string name, Control control)
        {
            Rect2 rect = control.GetGlobalRect();
            current.Add((name, Mathf.RoundToInt(rect.Position.X), Mathf.RoundToInt(rect.Position.Y), Mathf.RoundToInt(rect.Size.X), Mathf.RoundToInt(rect.Size.Y)));
        }

        foreach ((string name, Control control) in panels)
        {
            Add(name, control);
        }

        for (int i = 0; i < _heroes.Count; i++)
        {
            Add($"hero{i}", _heroes[i].Panel);
        }

        for (int i = 0; i < _parts.Count; i++)
        {
            Add($"part{i}", _parts[i].Panel);
        }

        if (baseline)
        {
            _layoutLockBase.AddRange(current);
            return;
        }

        int count = Math.Min(_layoutLockBase.Count, current.Count);
        for (int i = 0; i < count; i++)
        {
            (string name, int x, int y, int w, int h) = _layoutLockBase[i];
            (string _, int cx, int cy, int cw, int ch) = current[i];
            if (x != cx || y != cy || w != cw || h != ch)
            {
                _layoutLockErrors.Append(name)
                    .Append(" baseline ")
                    .Append(w).Append('x').Append(h).Append('+').Append(x).Append(',').Append(y)
                    .Append(" now ")
                    .Append(cw).Append('x').Append(ch).Append('+').Append(cx).Append(',').Append(cy)
                    .Append(" sample ").Append(_layoutLockSamples)
                    .Append(" tick ").Append(_bridge.Simulation.Tick)
                    .Append('\n');
            }
        }
    }

    private void SaveLayoutShot(string tag)
    {
        if (OS.GetEnvironment("HUD_LOCK_SHOT") != "1" || DisplayServer.GetName() == "headless")
        {
            return;
        }

        Image? image = GetViewport().GetTexture().GetImage();
        if (image == null)
        {
            return;
        }

        string path = $"/tmp/hud-lock-{tag}-tick{_bridge.Simulation.Tick}.png";
        image.SavePng(path);
        GD.Print($"HUD_LOCK shot {path} tick {_bridge.Simulation.Tick}");
    }

    private void FinishLayoutLock()
    {
        _layoutLock = false;
        var report = new System.Text.StringBuilder();
        report.Append("HUD_LOCK ").Append(GetWindow().Size.X).Append('x').Append(GetWindow().Size.Y);
        report.Append(" samples=").Append(_layoutLockSamples);
        report.Append(" tick=").Append(_bridge.Simulation.Tick);
        report.Append(_layoutLockErrors.Length == 0 ? " OK" : " FAIL");
        report.Append('\n');
        foreach ((string name, int x, int y, int w, int h) in _layoutLockBase)
        {
            report.Append(name).Append(' ').Append(w).Append('x').Append(h).Append('+').Append(x).Append(',').Append(y).Append('\n');
        }

        if (_layoutLockErrors.Length > 0)
        {
            report.Append(_layoutLockErrors);
        }

        GD.Print(report.ToString());
        GetTree().Quit(_layoutLockErrors.Length == 0 ? 0 : 1);
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
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _status.AddThemeFontSizeOverride("font_size", 13);
        _status.AddThemeColorOverride("font_color", new Color("d5dde8"));
        top.AddChild(ClipHost(_status, 0, 0, HostFit.Expand, HostFit.Fill));
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

    /// <summary>
    /// The 3D view keeps the spare height. In the arena, the four party cards stack
    /// beside the view. Boss parts stay in the band under that row.
    /// </summary>
    private Control BuildStage()
    {
        _stage = new VBoxContainer
        {
            Name = "Stage",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _stage.AddThemeConstantOverride("separation", 4);

        _viewRow = new HBoxContainer
        {
            Name = "ViewRow",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _viewRow.AddThemeConstantOverride("separation", 6);
        _arenaHost.Name = "Arena";
        _viewRow.AddChild(_arenaHost);
        _stage.AddChild(_viewRow);

        _band = new VBoxContainer
        {
            Name = "Band",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkEnd,
        };
        _band.AddThemeConstantOverride("separation", 4);
        BattleSimulator sim = _bridge.Simulation;
        _band.AddChild(BuildHeroColumn(sim));
        _band.AddChild(BuildPartColumn(sim));
        _stage.AddChild(_band);
        return _stage;
    }

    private Control BuildHeroColumn(BattleSimulator sim)
    {
        _heroBox = new GridContainer
        {
            Name = "Party",
            Columns = 1,
            CustomMinimumSize = new Vector2(PartyColumnWidth, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        _heroBox.AddThemeConstantOverride("h_separation", 4);
        _heroBox.AddThemeConstantOverride("v_separation", 4);

        for (int i = 0; i < sim.Heroes.Count; i++)
        {
            int slot = i;
            var row = new HeroRow(PortraitColors[i % PortraitColors.Length]);
            row.Panel.Name = $"Hero{i}";
            row.Panel.GuiInput += @event => ClickHero(@event, slot);
            _heroes.Add(row);
            _heroBox.AddChild(row.Panel);
        }

        return _heroBox;
    }

    private Control BuildPartColumn(BattleSimulator sim)
    {
        var column = new VBoxContainer
        {
            Name = "Boss",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        column.AddThemeConstantOverride("separation", 4);
        _bossCard = new BossCard();
        column.AddChild(_bossCard.Panel);

        _partBox = new HBoxContainer
        {
            Name = "Parts",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _partBox.AddThemeConstantOverride("separation", 4);
        column.AddChild(_partBox);

        for (int i = 0; i < sim.Boss.Parts.Length; i++)
        {
            int index = i;
            var row = new PartRow(PortraitColors);
            row.Panel.Name = $"Part{i}";
            row.Panel.GuiInput += @event => ClickPart(@event, index);
            _parts.Add(row);
            _partBox.AddChild(row.Panel);
        }

        return column;
    }

    private Control BuildInfoColumn()
    {
        _info = new VBoxContainer
        {
            Name = "ResonanceColumn",
            ClipContents = true,
            CustomMinimumSize = new Vector2(InfoColumnWidth, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _info.AddThemeConstantOverride("separation", 2);
        _info.AddChild(ColumnTitle("Resonance"));

        _resonance = new Label
        {
            Name = "ResonanceReadout",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        Reserve(_resonance, 12, -1, 0, wrap: true);
        _resonance.AddThemeColorOverride("font_color", new Color("d5dde8"));
        _info.AddChild(ClipHost(_resonance, 0, ResonanceBlock, HostFit.Expand, HostFit.Shrink));

        _info.AddChild(ColumnTitle("Ability"));
        _abilityPanel = new PanelContainer
        {
            Name = "AbilityPanel",
            ClipContents = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _abilityPanel.AddThemeStyleboxOverride("panel", Pad(HudSkin.Ability(), 8, 6));
        _abilityDetail = new Label
        {
            Name = "AbilityDetail",
            MouseFilter = MouseFilterEnum.Ignore,
            Text = "Select an ability.",
        };
        Reserve(_abilityDetail, 12, -1, 0, wrap: true);
        _abilityDetail.AddThemeConstantOverride("line_spacing", 1);
        _abilityDetail.AddThemeColorOverride("font_color", new Color("d5dde8"));
        _abilityPanel.AddChild(ClipHost(_abilityDetail, 0, 0, HostFit.Expand, HostFit.Expand));
        _info.AddChild(_abilityPanel);
        return _info;
    }

    private Control BuildAction()
    {
        _action = new PanelContainer
        {
            Name = "ActionBar",
            ClipContents = true,
            CustomMinimumSize = new Vector2(0, ActionBarHeight),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkEnd,
        };
        PanelContainer panel = _action;
        panel.AddThemeStyleboxOverride("panel", Pad(HudSkin.Ability(), 8, 4));

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 2);
        panel.AddChild(box);

        _order = new Label { MouseFilter = MouseFilterEnum.Ignore };
        Reserve(_order, 12, 1, 0, wrap: false);
        _order.AddThemeColorOverride("font_color", new Color("d5dde8"));
        box.AddChild(ClipHost(_order, 0, 17, HostFit.Expand, HostFit.Shrink));

        var abilityRow = new HBoxContainer
        {
            ClipContents = true,
            CustomMinimumSize = new Vector2(0, 38),
        };
        abilityRow.AddThemeConstantOverride("separation", 6);
        _actorLabel = new Label
        {
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        _actorLabel.AddThemeFontSizeOverride("font_size", 13);
        _actorLabel.AddThemeColorOverride("font_color", new Color("ffe08a"));
        abilityRow.AddChild(ClipHost(_actorLabel, 196, 0, HostFit.Shrink, HostFit.Fill));

        _abilities = new HBoxContainer
        {
            ClipContents = true,
            CustomMinimumSize = new Vector2(0, 38),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _abilities.AddThemeConstantOverride("separation", 4);
        abilityRow.AddChild(_abilities);
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

        _pause = new Button { Text = "Pause", ClipText = true };
        _pause.Pressed += () => _bridge.TogglePause();
        controls.AddChild(ClipHost(_pause, 96, 0, HostFit.Shrink, HostFit.Fill));

        _auto = new Button { Text = "Auto-run", ClipText = true };
        _auto.Pressed += () => _bridge.ToggleAuto();
        controls.AddChild(ClipHost(_auto, 112, 0, HostFit.Shrink, HostFit.Fill));

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

    private static Label ColumnTitle(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", new Color("8ea0b5"));
        Reserve(label, 12, 1, 17, wrap: false);
        return label;
    }

    private enum HostFit
    {
        Shrink,
        Fill,
        Expand,
    }

    /// <summary>
    /// A plain Control ignores its child's minimum size. Text, numbers, and button labels
    /// stay inside this slot and cannot grow the panel around them.
    /// </summary>
    private static Control ClipHost(Control child, int minWidth, int minHeight, HostFit horizontal, HostFit vertical)
    {
        var slot = new Control
        {
            ClipContents = true,
            CustomMinimumSize = new Vector2(minWidth, minHeight),
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = FitFlags(horizontal),
            SizeFlagsVertical = FitFlags(vertical),
        };
        slot.AddChild(child);
        child.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return slot;
    }

    private static SizeFlags FitFlags(HostFit fit) => fit switch
    {
        HostFit.Expand => SizeFlags.ExpandFill,
        HostFit.Fill => SizeFlags.Fill,
        _ => SizeFlags.ShrinkBegin,
    };

    /// <summary>
    /// Keeps a label from resizing its parent. The reserved height is the floor and the cap:
    /// shorter copy stays in the slot, longer copy wraps or ellipsizes inside it.
    /// </summary>
    private static void Reserve(Label label, int fontSize, int lines, int blockPx, bool wrap)
    {
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off;
        label.MaxLinesVisible = lines;
        label.ClipText = true;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        label.VerticalAlignment = VerticalAlignment.Top;
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        label.CustomMinimumSize = new Vector2(0, blockPx);
    }

    private static string Digits(int value, int width)
    {
        if (value < 0)
        {
            value = 0;
        }

        string text = value.ToString();
        return text.Length >= width ? text : text.PadLeft(width);
    }

    private static StyleBox Pad(StyleBox style, int x, int y)
    {
        style.ContentMarginLeft = x;
        style.ContentMarginRight = x;
        style.ContentMarginTop = y;
        style.ContentMarginBottom = y;
        return style;
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
        string paused = sim.Paused ? "PAUSED " : "running";
        const string frenzySlot = "   Frenzy +0000bp";
        string frenzy = sim.Boss.FrenzyBp > 0
            ? $"   Frenzy +{Digits(sim.Boss.FrenzyBp, 4)}bp"
            : new string(' ', frenzySlot.Length);
        string party = _bridge.PartyLabel;
        string tick = Digits(sim.Tick, 6);
        _status.Text = ended
            ? $"{party}   Tick {tick}   {sim.Outcome}   Seed {_bridge.Seed}"
            : $"{party}   Tick {tick}   {paused}{frenzy}   Seed {_bridge.Seed}";
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
            row.Panel.Name = $"Part{i}";
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
                ClipText = true,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
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
            _abilities.AddChild(ClipHost(button, 0, 0, HostFit.Expand, HostFit.Fill));
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
            if (AbilityButton(_abilities.GetChild(i)) is Button button)
            {
                button.Modulate = ShownAbility(button.Text) ? new Color("ffe08a") : Colors.White;
            }
        }
    }

    private static Button? AbilityButton(Node node)
    {
        if (node is Button button)
        {
            return button;
        }

        return node.GetChildCount() > 0 ? node.GetChild(0) as Button : null;
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

    private static ChargeWidgets BuildChargeRow(Container box, bool boss)
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
        caption.AddThemeFontSizeOverride("font_size", 11);
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
            MouseFilter = MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(42, 16),
        };
        turn.AddThemeFontSizeOverride("font_size", 11);
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
        turn.Visible = true;
        turn.Modulate = ready ? Colors.White : Colors.Transparent;
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
                ClipContents = true,
                CustomMinimumSize = new Vector2(0, 80),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Stop,
                MouseDefaultCursorShape = CursorShape.PointingHand,
            };
            Panel.AddThemeStyleboxOverride("panel", Pad(HudSkin.PartyCard(), 8, 4));
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 6);
            Panel.AddChild(row);
            row.AddChild(new ColorRect
            {
                Color = color,
                CustomMinimumSize = new Vector2(6, 18),
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            });
            _icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(18, 18),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
                TextureFilter = TextureFilterEnum.Linear,
                Modulate = Colors.Transparent,
            };
            row.AddChild(_icon);
            var box = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            box.AddThemeConstantOverride("separation", 1);
            row.AddChild(box);
            _name = new Label { MouseFilter = MouseFilterEnum.Ignore };
            Reserve(_name, 14, 1, 0, wrap: false);
            box.AddChild(ClipHost(_name, 0, 20, HostFit.Expand, HostFit.Shrink));
            ChargeWidgets charge = BuildChargeRow(box, boss: false);
            _chargeRow = charge.Row;
            _charge = charge.Bar;
            _turn = charge.Turn;
            _hp = Bar(new Color("c0392b"), 7);
            box.AddChild(_hp);
            _mp = Bar(new Color("2980b9"), 7);
            box.AddChild(_mp);
            _meta = new Label { MouseFilter = MouseFilterEnum.Ignore };
            Reserve(_meta, 12, 1, 0, wrap: false);
            _meta.AddThemeColorOverride("font_color", new Color("c5d0dc"));
            box.AddChild(ClipHost(_meta, 0, 17, HostFit.Expand, HostFit.Shrink));
        }

        public PanelContainer Panel { get; }

        public void Set(HeroState hero, bool selected, int tick, int chargeBp)
        {
            Panel.Modulate = selected ? new Color("ffe08a") : Colors.White;
            string down = hero.IsAlive ? "" : "  DOWN";
            bool shielded = hero.Absorb > 0 && tick < hero.AbsorbExpires;
            bool regen = hero.RegenPerPulse > 0 && tick < hero.RegenExpires;
            _icon.Texture = shielded ? HudSkin.Status("shield") : regen ? HudSkin.Status("regen") : null;
            _icon.Modulate = shielded || regen ? Colors.White : Colors.Transparent;
            bool turn = hero.IsAlive && chargeBp >= SimConst.Bp;
            _name.AddThemeColorOverride("font_color", turn ? new Color("d7fbff") : new Color("f4f7fb"));
            _name.Text = hero.Name + down;
            UntintCharge(_chargeRow, selected);
            PaintCharge(_charge, _turn, chargeBp, boss: false);
            _hp.MaxValue = hero.MaxHp;
            _hp.Value = hero.Hp < 0 ? 0 : hero.Hp;
            _mp.MaxValue = hero.MaxMp <= 0 ? 1 : hero.MaxMp;
            _mp.Value = hero.Mp;
            string cast = hero.Casting ? $"  c{Digits(hero.CastResolveTick, 5)}" : "        ";
            string shield = shielded ? $"  Sh{Digits(hero.Absorb, 5)}" : "         ";
            string regenText = regen ? "  Rg" : "    ";
            string ap = ApGauge.Format(hero.Ap.Centi);
            if (ap.Length < 6)
            {
                ap = ap.PadLeft(6);
            }

            _meta.Text = $"HP {Digits(hero.Hp, 5)}/{Digits(hero.MaxHp, 5)}  MP {Digits(hero.Mp, 4)}/{Digits(hero.MaxMp, 4)}  AP {ap}{shield}{regenText}{cast}";
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
                ClipContents = true,
                CustomMinimumSize = new Vector2(0, 164),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Stop,
                MouseDefaultCursorShape = CursorShape.PointingHand,
            };
            Panel.AddThemeStyleboxOverride("panel", Pad(HudSkin.BossPart(), 8, 4));
            var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            box.AddThemeConstantOverride("separation", 1);
            Panel.AddChild(box);
            var title = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            title.AddThemeConstantOverride("separation", 4);
            box.AddChild(title);
            _icon = StatusIcon();
            title.AddChild(_icon);
            _chain = StatusIcon();
            title.AddChild(_chain);
            _name = new Label { MouseFilter = MouseFilterEnum.Ignore };
            Reserve(_name, 13, 1, 0, wrap: false);
            title.AddChild(ClipHost(_name, 0, 18, HostFit.Expand, HostFit.Shrink));
            ChargeWidgets charge = BuildChargeRow(box, boss: true);
            _chargeRow = charge.Row;
            _charge = charge.Bar;
            _turn = charge.Turn;
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
            _resist = Note(new Color("d5e2c4"), 1);
            box.AddChild(ClipHost(_resist, 0, 17, HostFit.Expand, HostFit.Shrink));
            _pressure = Note(new Color("b7c4d4"), 2);
            box.AddChild(ClipHost(_pressure, 0, 37, HostFit.Expand, HostFit.Shrink));

            var threats = new GridContainer
            {
                Columns = 2,
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            threats.AddThemeConstantOverride("h_separation", 8);
            threats.AddThemeConstantOverride("v_separation", 2);
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
                _threatLabel[i] = new Label { MouseFilter = MouseFilterEnum.Ignore };
                Reserve(_threatLabel[i], 12, 1, 17, wrap: false);
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
            _icon.Texture = stunned ? HudSkin.Status("stun") : null;
            _icon.Modulate = stunned ? Colors.White : Colors.Transparent;
            Texture2D? chain = HudSkin.Chain(part.Property);
            _chain.Texture = chain;
            _chain.Modulate = chain != null ? Colors.White : Colors.Transparent;
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
            _name.Text = $"{part.Name}  {Digits(part.Hp, 6)}/{Digits(part.MaxHp, 6)}  DEF {Digits(def, 4)}  {state}";
            _resist.Text = ResonanceReadout.ResistLine(part);
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
                _threatLabel[i].Text = $"{heroes[i].Name.Split(' ')[0]} {Digits(total, 5)}";
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
                Modulate = Colors.Transparent,
                TextureFilter = TextureFilterEnum.Linear,
            };
        }

        private static Label Note(Color color, int lines)
        {
            var label = new Label { MouseFilter = MouseFilterEnum.Ignore };
            Reserve(label, 12, lines, lines == 1 ? 17 : 37, wrap: true);
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
                Name = "BossCard",
                ClipContents = true,
                CustomMinimumSize = new Vector2(0, 32),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkBegin,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            Panel.AddThemeStyleboxOverride("panel", _panelStyle);
            var box = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            box.AddThemeConstantOverride("separation", 8);
            Panel.AddChild(box);
            _name = new Label
            {
                MouseFilter = MouseFilterEnum.Ignore,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Reserve(_name, 14, 1, 0, wrap: false);
            _name.AddThemeColorOverride("font_color", new Color("e7d5ee"));
            box.AddChild(ClipHost(_name, 0, 20, HostFit.Expand, HostFit.Fill));
            ChargeWidgets charge = BuildChargeRow(box, boss: true);
            charge.Row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            charge.Row.CustomMinimumSize = new Vector2(220, 0);
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
