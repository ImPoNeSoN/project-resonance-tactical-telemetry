using Godot;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Sim;

namespace Resonance.Game;

/// <summary>
/// Grey-box Carapace Engine fight. Plain Control nodes and flat colors only.
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
    private Label _resonance = null!;
    private Label _order = null!;
    private Control _timeline = null!;
    private readonly List<TimelineMarker> _markers = new();
    private readonly List<HeroRow> _heroes = new();
    private readonly List<PartRow> _parts = new();
    private VBoxContainer _abilities = null!;
    private HBoxContainer _allies = null!;
    private RichTextLabel _log = null!;
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

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var background = new ColorRect { Color = new Color("12161c"), MouseFilter = MouseFilterEnum.Ignore };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        var root = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        root.AddThemeConstantOverride("separation", 6);
        margin.AddChild(root);

        var title = new Label { Text = "Project Resonance  ·  Grey-Box Battle  ·  Carapace Engine" };
        title.AddThemeFontSizeOverride("font_size", 18);
        root.AddChild(title);

        _status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        root.AddChild(_status);

        _timeline = new Control { CustomMinimumSize = new Vector2(0, 78) };
        var track = new ColorRect { Color = new Color("1c2430"), MouseFilter = MouseFilterEnum.Ignore };
        track.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _timeline.AddChild(track);
        root.AddChild(_timeline);

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 8);
        root.AddChild(body);

        var heroColumn = new VBoxContainer { CustomMinimumSize = new Vector2(340, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
        heroColumn.AddThemeConstantOverride("separation", 4);
        body.AddChild(heroColumn);

        var bossColumn = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        bossColumn.AddThemeConstantOverride("separation", 4);
        body.AddChild(bossColumn);

        var side = new VBoxContainer { CustomMinimumSize = new Vector2(280, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
        side.AddThemeConstantOverride("separation", 4);
        body.AddChild(side);

        BattleSimulator sim = _bridge.Simulation;
        for (int i = 0; i < sim.Heroes.Count; i++)
        {
            int slot = i;
            var row = new HeroRow(PortraitColors[i % 4]);
            row.Panel.GuiInput += @event => ClickHero(@event, slot);
            _heroes.Add(row);
            heroColumn.AddChild(row.Panel);
        }

        for (int i = 0; i < sim.Boss.Parts.Length; i++)
        {
            int index = i;
            var row = new PartRow();
            row.Panel.GuiInput += @event => ClickPart(@event, index);
            _parts.Add(row);
            bossColumn.AddChild(row.Panel);
        }

        _resonance = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        side.AddChild(_resonance);
        _order = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        side.AddChild(_order);

        var allyLabel = new Label { Text = "Ally target (heals and Benediction)" };
        side.AddChild(allyLabel);
        _allies = new HBoxContainer();
        _allies.AddThemeConstantOverride("separation", 4);
        side.AddChild(_allies);
        for (int i = 0; i < sim.Heroes.Count; i++)
        {
            int slot = i;
            var button = new Button { Text = ShortName(sim.Heroes[i].Name), SizeFlagsHorizontal = SizeFlags.ExpandFill };
            button.Pressed += () =>
            {
                _ally = slot;
                Refresh();
            };
            _allies.AddChild(button);
        }

        _abilities = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _abilities.AddThemeConstantOverride("separation", 4);
        side.AddChild(_abilities);

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 8);
        root.AddChild(buttons);

        var step = new Button { Text = "Step" };
        step.Pressed += () => _bridge.StepOnce();
        buttons.AddChild(step);

        _pause = new Button { Text = "Pause" };
        _pause.Pressed += () => _bridge.TogglePause();
        buttons.AddChild(_pause);

        _auto = new Button { Text = "Auto-run" };
        _auto.Pressed += () => _bridge.ToggleAuto();
        buttons.AddChild(_auto);

        var restart = new Button { Text = "Restart" };
        restart.Pressed += Restart;
        buttons.AddChild(restart);

        _log = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 96),
        };
        _log.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color("0e1218") });
        root.AddChild(_log);

        BuildOverlay();
        BuildPicker();

        _bridge.LogLine += line => _log.AppendText($"[color=#9fd0ff]{line}[/color]\n");
        _bridge.LogCleared += () => _log.Clear();
        _bridge.StateChanged += Refresh;
        Refresh();
        _picker.Visible = true;
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

    private void BuildOverlay()
    {
        _overlay = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.72f),
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop,
        };
        _overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_overlay);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _overlay.AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(420, 180) };
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
        _picker.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_picker);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _picker.AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(760, 0) };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("1b2230"),
            ContentMarginLeft = 28,
            ContentMarginRight = 28,
            ContentMarginTop = 22,
            ContentMarginBottom = 22,
        });
        center.AddChild(panel);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 12);
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

        AbilityDef ability = AbilityCatalog.Get(abilityId);
        sim.Heroes[_actor].CurrentTargetPart = _part;
        int ally = ability.Kind == AbilityKind.Healing || ability.Effect == SupportEffect.CircuitBenediction ? _ally : -1;
        _bridge.QueueAbility(_actor, abilityId, _part, ally);
    }

    private void Refresh()
    {
        BattleSimulator sim = _bridge.Simulation;
        EnsureMarkers(sim);
        EnsureAbilities(sim);

        bool ended = sim.Outcome != FightOutcome.Ongoing;
        string paused = sim.Paused ? "PAUSED" : "running";
        string frenzy = sim.Boss.FrenzyBp > 0 ? $"    Frenzy +{sim.Boss.FrenzyBp}bp" : "";
        string party = PartyPreset.Name(_bridge.Preset);
        _status.Text = ended
            ? $"{party}    Tick {sim.Tick}    {sim.Outcome}    Seed {_bridge.Seed}"
            : $"{party}    Tick {sim.Tick}    {paused}{frenzy}    Seed {_bridge.Seed}    Space pauses. Click a hero, click a part, then pick an ability.";
        _auto.Text = _bridge.AutoRunning ? "Stop" : "Auto-run";
        _pause.Text = sim.Paused ? "Resume" : "Pause";
        _resonance.Text = ResonanceReadout.Summarize(sim);
        _order.Text = OrderText(sim);

        float width = _timeline.Size.X;
        if (width < 200)
        {
            width = 1200;
        }

        const float markerWidth = 168;
        for (int i = 0; i < sim.Heroes.Count; i++)
        {
            HeroState hero = sim.Heroes[i];
            string cast = hero.Casting ? AbilityCatalog.Get(hero.CastAbilityId).Name : "";
            _markers[i].Set(hero.Name, ApGauge.Format(hero.Ap.Centi), hero.Ap.Centi, hero.Casting, hero.CastStartTick, hero.CastResolveTick, sim.Tick, cast);
            Place(_markers[i], hero.Ap.Centi, width, markerWidth, i);
            _heroes[i].Set(hero, i == _actor);
        }

        BossState boss = sim.Boss;
        string bossCast = boss.Casting ? AbilityCatalog.Get(boss.CastAbilityId).Name : "";
        _markers[sim.Heroes.Count].Set(boss.Name, ApGauge.Format(boss.Ap.Centi), boss.Ap.Centi, boss.Casting, boss.CastStartTick, boss.CastResolveTick, sim.Tick, bossCast);
        Place(_markers[sim.Heroes.Count], boss.Ap.Centi, width, markerWidth, sim.Heroes.Count);

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

    private void EnsureMarkers(BattleSimulator sim)
    {
        int needed = sim.Heroes.Count + 1;
        while (_markers.Count < needed)
        {
            int index = _markers.Count;
            var marker = new TimelineMarker(PortraitColors[index % PortraitColors.Length]);
            _markers.Add(marker);
            _timeline.AddChild(marker.Root);
        }
    }

    private void EnsureAbilities(BattleSimulator sim)
    {
        if (_abilityActor == _actor || (uint)_actor >= (uint)sim.Heroes.Count)
        {
            return;
        }

        _abilityActor = _actor;
        while (_abilities.GetChildCount() > 0)
        {
            Node child = _abilities.GetChild(0);
            _abilities.RemoveChild(child);
            child.QueueFree();
        }

        foreach (int abilityId in sim.Heroes[_actor].Kit)
        {
            AbilityDef ability = AbilityCatalog.Get(abilityId);
            int id = abilityId;
            var button = new Button
            {
                Text = ability.MpCost > 0 ? $"{ability.Name}  ({ability.MpCost} MP)" : ability.Name,
                Alignment = HorizontalAlignment.Left,
            };
            button.Pressed += () => Queue(id);
            _abilities.AddChild(button);
        }
    }

    private static void Place(TimelineMarker marker, int centi, float width, float markerWidth, int lane)
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

        float x = shown / 10_000f * (width - markerWidth);
        marker.Root.Position = new Vector2(x, 4 + (lane % 2) * 2);
        marker.Root.Size = new Vector2(markerWidth, 70);
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

        return $"{actor} aims at {aim}. Ally {ally}.\n{queued}";
    }

    private static string ShortName(string name)
    {
        int space = name.IndexOf(' ');
        return space > 0 ? name[..space] : name;
    }

    private sealed class TimelineMarker
    {
        public TimelineMarker(Color color)
        {
            Root = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            Root.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color("222836"),
                ContentMarginLeft = 4,
                ContentMarginRight = 4,
                ContentMarginTop = 2,
                ContentMarginBottom = 2,
            });
            var box = new VBoxContainer();
            Root.AddChild(box);
            var head = new HBoxContainer();
            box.AddChild(head);
            head.AddChild(new ColorRect
            {
                Color = color,
                CustomMinimumSize = new Vector2(16, 16),
                MouseFilter = MouseFilterEnum.Ignore,
            });
            Name = new Label();
            Name.AddThemeFontSizeOverride("font_size", 12);
            head.AddChild(Name);
            Detail = new Label();
            Detail.AddThemeFontSizeOverride("font_size", 11);
            box.AddChild(Detail);
            var castRow = new HBoxContainer();
            box.AddChild(castRow);
            Cast = new ProgressBar
            {
                MinValue = 0,
                MaxValue = 1,
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(0, 10),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Visible = false,
            };
            castRow.AddChild(Cast);
            Interrupt = new ColorRect
            {
                Color = new Color("e74c3c"),
                CustomMinimumSize = new Vector2(12, 12),
                Visible = false,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            castRow.AddChild(Interrupt);
        }

        public PanelContainer Root { get; }

        private Label Name { get; }

        private Label Detail { get; }

        private ProgressBar Cast { get; }

        private ColorRect Interrupt { get; }

        public void Set(string name, string ap, int centi, bool casting, int start, int resolve, int tick, string castName)
        {
            Name.Text = name;
            Detail.Text = casting ? $"{castName}  AP {ap}" : $"AP {ap}";
            Cast.Visible = casting;
            Interrupt.Visible = casting;
            if (!casting)
            {
                return;
            }

            int span = resolve - start;
            if (span < 1)
            {
                span = 1;
            }

            Cast.MaxValue = span;
            int elapsed = tick - start;
            if (elapsed < 0)
            {
                elapsed = 0;
            }

            if (elapsed > span)
            {
                elapsed = span;
            }

            Cast.Value = elapsed;
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
                ContentMarginTop = 4,
                ContentMarginBottom = 4,
                BorderColor = new Color("00000000"),
            };
            Panel = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Stop };
            Panel.AddThemeStyleboxOverride("panel", _style);
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 6);
            Panel.AddChild(row);
            row.AddChild(new ColorRect
            {
                Color = color,
                CustomMinimumSize = new Vector2(28, 28),
                MouseFilter = MouseFilterEnum.Ignore,
            });
            var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            row.AddChild(box);
            _name = new Label { MouseFilter = MouseFilterEnum.Ignore };
            box.AddChild(_name);
            _hp = Bar(new Color("c0392b"));
            box.AddChild(_hp);
            _mp = Bar(new Color("2980b9"));
            box.AddChild(_mp);
            _meta = new Label { MouseFilter = MouseFilterEnum.Ignore };
            _meta.AddThemeFontSizeOverride("font_size", 12);
            box.AddChild(_meta);
        }

        public PanelContainer Panel { get; }

        public void Set(HeroState hero, bool selected)
        {
            _style.BorderColor = selected ? new Color("ffe08a") : new Color("00000000");
            _style.BorderWidthLeft = selected ? 3 : 0;
            _style.BorderWidthRight = selected ? 3 : 0;
            _style.BorderWidthTop = selected ? 3 : 0;
            _style.BorderWidthBottom = selected ? 3 : 0;
            string down = hero.IsAlive ? "" : "  DOWN";
            _name.Text = hero.Name + down;
            _hp.MaxValue = hero.MaxHp;
            _hp.Value = hero.Hp < 0 ? 0 : hero.Hp;
            _mp.MaxValue = hero.MaxMp <= 0 ? 1 : hero.MaxMp;
            _mp.Value = hero.Mp;
            string cast = hero.Casting ? $"  cast → {hero.CastResolveTick}" : "";
            _meta.Text = $"HP {hero.Hp}/{hero.MaxHp}   MP {hero.Mp}/{hero.MaxMp}   AP {ApGauge.Format(hero.Ap.Centi)}{cast}";
        }

        private static ProgressBar Bar(Color _)
        {
            return new ProgressBar
            {
                MinValue = 0,
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(0, 10),
                MouseFilter = MouseFilterEnum.Ignore,
            };
        }
    }

    private sealed class PartRow
    {
        private readonly StyleBoxFlat _style;
        private readonly Label _name;
        private readonly ProgressBar _hp;
        private readonly ProgressBar[] _threat;
        private readonly Label[] _threatLabel;
        private readonly Label _note;

        public PartRow()
        {
            _style = new StyleBoxFlat
            {
                BgColor = new Color("1b2230"),
                ContentMarginLeft = 8,
                ContentMarginRight = 8,
                ContentMarginTop = 4,
                ContentMarginBottom = 4,
                BorderColor = new Color("00000000"),
            };
            Panel = new PanelContainer { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Stop };
            Panel.AddThemeStyleboxOverride("panel", _style);
            var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            Panel.AddChild(box);
            _name = new Label { MouseFilter = MouseFilterEnum.Ignore };
            box.AddChild(_name);
            _hp = new ProgressBar
            {
                MinValue = 0,
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(0, 14),
                MouseFilter = MouseFilterEnum.Ignore,
            };
            box.AddChild(_hp);
            _note = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = MouseFilterEnum.Ignore };
            _note.AddThemeFontSizeOverride("font_size", 12);
            box.AddChild(_note);
            var threats = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            threats.AddThemeConstantOverride("separation", 6);
            box.AddChild(threats);
            _threat = new ProgressBar[4];
            _threatLabel = new Label[4];
            for (int i = 0; i < 4; i++)
            {
                var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
                threats.AddChild(column);
                _threatLabel[i] = new Label { MouseFilter = MouseFilterEnum.Ignore };
                _threatLabel[i].AddThemeFontSizeOverride("font_size", 11);
                column.AddChild(_threatLabel[i]);
                _threat[i] = new ProgressBar
                {
                    MinValue = 0,
                    MaxValue = 30_000,
                    ShowPercentage = false,
                    CustomMinimumSize = new Vector2(0, 8),
                    MouseFilter = MouseFilterEnum.Ignore,
                };
                column.AddChild(_threat[i]);
            }
        }

        public PanelContainer Panel { get; }

        public void Set(BossPartState part, IReadOnlyList<HeroState> heroes, bool selected, int bossDef)
        {
            _style.BorderColor = selected ? new Color("ffe08a") : new Color("00000000");
            _style.BorderWidthLeft = selected ? 3 : 0;
            _style.BorderWidthRight = selected ? 3 : 0;
            _style.BorderWidthTop = selected ? 3 : 0;
            _style.BorderWidthBottom = selected ? 3 : 0;
            string state = part.Hp <= 0 ? "destroyed" : part.Active ? "up" : "down";
            int def = bossDef + part.DefBonus;
            _name.Text = $"{part.Name}  {part.Hp}/{part.MaxHp}  DEF {def}  {state}";
            string resist = ResonanceReadout.ResistLine(part);
            _note.Text = resist.Length == 0 ? part.Pressure : $"{resist}\n{part.Pressure}";
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
    }
}
