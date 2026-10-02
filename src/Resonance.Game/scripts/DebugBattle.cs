using Godot;
using Resonance.Sim.Core;
using Resonance.Sim.Sim;

namespace Resonance.Game;

/// <summary>
/// Grey-box math console for the §2.15 training arena. Control nodes only.
/// </summary>
public partial class DebugBattle : Control
{
    private SimBridge _bridge = null!;
    private readonly List<UnitCard> _cards = new();
    private RichTextLabel _log = null!;
    private Label _boss = null!;
    private Label _resonance = null!;
    private Label _status = null!;
    private Button _auto = null!;

    public override void _Ready()
    {
        _bridge = GetNode<SimBridge>("/root/SimBridge");
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var background = new ColorRect
        {
            Color = new Color("141820"),
        };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        AddChild(margin);

        var root = new VBoxContainer();
        root.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        var title = new Label
        {
            Text = "Project Resonance  ·  Combat Math Console  ·  Carapace Engine Mk. II",
        };
        title.AddThemeFontSizeOverride("font_size", 20);
        root.AddChild(title);

        _status = new Label
        {
            Text = "Training arena from docs/02 §2.15. Step or auto-run the CTB timeline.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        root.AddChild(_status);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        root.AddChild(row);
        foreach (HeroState hero in _bridge.Simulation.Heroes)
        {
            var card = new UnitCard(hero.Name);
            _cards.Add(card);
            row.AddChild(card.Root);
        }

        _boss = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        root.AddChild(_boss);

        _resonance = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        root.AddChild(_resonance);

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 8);
        root.AddChild(buttons);

        var step = new Button { Text = "Step" };
        step.Pressed += () => _bridge.Step();
        buttons.AddChild(step);

        _auto = new Button { Text = "Auto-run" };
        _auto.Pressed += () => _bridge.ToggleAuto();
        buttons.AddChild(_auto);

        var reset = new Button { Text = "Reset" };
        reset.Pressed += () => _bridge.Reset();
        buttons.AddChild(reset);

        _log = new RichTextLabel
        {
            BbcodeEnabled = true,
            ScrollFollowing = true,
            FitContent = false,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _log.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = new Color("0e1218") });
        root.AddChild(_log);
        _log.CustomMinimumSize = new Vector2(0, 280);

        _bridge.LogLine += OnLogLine;
        _bridge.LogCleared += OnLogCleared;
        _bridge.ResonanceChanged += OnResonance;
        _bridge.StateChanged += Refresh;
        Refresh();
    }

    private void OnLogCleared()
    {
        _log.Clear();
    }

    private void OnResonance(string summary)
    {
        _resonance.Text = summary;
    }

    private void OnLogLine(string line)
    {
        _log.AppendText($"[color=#9fd0ff]{line}[/color]\n");
    }

    private void Refresh()
    {
        BattleSimulator sim = _bridge.Simulation;
        _auto.Text = _bridge.AutoRunning ? "Stop" : "Auto-run";
        _status.Text = $"Tick {sim.Tick}    Boss AP {ApGauge.Format(sim.Boss.Ap.Centi)}    AGI eff {sim.Boss.Agi}{(sim.Boss.SlowBp > 0 && sim.Tick < sim.Boss.SlowExpires ? " slowed" : "")}";

        for (int i = 0; i < sim.Heroes.Count; i++)
        {
            HeroState hero = sim.Heroes[i];
            var core = sim.Boss.Parts[0].Enmity[i];
            int shownAp = hero.Ap.Centi / SimConst.CentiPerAp;
            int bar = shownAp < 0 ? 0 : shownAp;
            if (bar > 10_000)
            {
                bar = 10_000;
            }

            string casting = hero.Casting ? $"  casting → {hero.CastResolveTick}" : "";
            _cards[i].Set(
                $"{hero.Name}{casting}",
                $"HP {hero.Hp}/{hero.MaxHp}    MP {hero.Mp}/{hero.MaxMp}",
                $"AP {ApGauge.Format(hero.Ap.Centi)}    VE {core.Ve}    CE {core.Ce}",
                bar);
        }

        var corePart = sim.Boss.Parts[0];
        var arm = sim.Boss.Parts[1];
        string chain = corePart.Tier switch
        {
            1 => $"L1 {corePart.Property} until {corePart.ChainExpires}",
            2 => $"L2 {corePart.Resonance} until {corePart.ChainExpires}",
            _ => "empty",
        };
        string burst = corePart.BurstActive ? $"{corePart.BurstMask} until {corePart.BurstExpires}" : "no burst";
        _boss.Text = $"{sim.Boss.Name}    Core {corePart.Hp}/{corePart.MaxHp}    Arm {arm.Hp}/{arm.MaxHp}    Chain {chain}    {burst}";
        _resonance.Text = ResonanceReadout.Summarize(sim);
    }

    private sealed class UnitCard
    {
        public UnitCard(string name)
        {
            Root = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            var panel = new PanelContainer();
            panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = new Color("222836"),
                ContentMarginLeft = 8,
                ContentMarginRight = 8,
                ContentMarginTop = 6,
                ContentMarginBottom = 6,
            });
            Root.AddChild(panel);
            var box = new VBoxContainer();
            panel.AddChild(box);
            _name = new Label();
            _name.AddThemeFontSizeOverride("font_size", 15);
            box.AddChild(_name);
            _hp = new Label();
            box.AddChild(_hp);
            _ap = new Label();
            box.AddChild(_ap);
            _bar = new ProgressBar
            {
                MinValue = 0,
                MaxValue = 10_000,
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(0, 16),
            };
            box.AddChild(_bar);
            Set(name, "", "", 0);
        }

        public VBoxContainer Root { get; }

        private readonly Label _name;
        private readonly Label _hp;
        private readonly Label _ap;
        private readonly ProgressBar _bar;

        public void Set(string name, string hp, string ap, int bar)
        {
            _name.Text = name;
            _hp.Text = hp;
            _ap.Text = ap;
            _bar.Value = bar;
        }
    }
}
