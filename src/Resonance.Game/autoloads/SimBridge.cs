using Godot;
using Resonance.Sim.Sim;

namespace Resonance.Game;

/// <summary>
/// Autoload that owns the pure <see cref="BattleSimulator"/> and drains its log once per step.
/// Presentation listens to the signals; the sim itself has no Godot types.
/// </summary>
public partial class SimBridge : Node
{
    private BattleSimulator _sim = CarapaceEncounter.Create(CarapaceEncounter.ShowcaseSeed);
    private int _shown;
    private bool _auto;
    private double _wait;
    private FloorRun? _floor;
    private readonly List<string> _runLog = new();
    private ulong _runStartedMs;
    private bool _runLogged;

    [Signal]
    public delegate void LogLineEventHandler(string line);

    [Signal]
    public delegate void StateChangedEventHandler();

    [Signal]
    public delegate void LogClearedEventHandler();

    [Signal]
    public delegate void ResonanceChangedEventHandler(string summary);

    [Signal]
    public delegate void FloorChangedEventHandler();

    public BattleSimulator Simulation => _sim;

    public FloorRun? Floor => _floor;

    public bool InFloorFight { get; private set; }

    public bool BossOnly { get; private set; }

    public ulong Seed { get; private set; } = CarapaceEncounter.ShowcaseSeed;

    public int Preset { get; private set; } = PartyPreset.IceLattice;

    public int[] PartyIds { get; private set; } = HeroRoster.PresetIds(PartyPreset.IceLattice);

    public string PartyLabel
    {
        get
        {
            if (_floor != null)
            {
                return _floor.PartyLabel;
            }

            if (Preset >= 0 && Preset < PartyPreset.Count)
            {
                return PartyPreset.Name(Preset);
            }

            return HeroRoster.Label(PartyIds);
        }
    }

    public bool AutoRunning => _auto;

    public bool Paused => _sim.Paused;

    public override void _Ready()
    {
        Publish();
    }

    public override void _Process(double delta)
    {
        if (!_auto || _sim.Paused)
        {
            return;
        }

        _wait += delta;
        if (_wait < 0.2)
        {
            return;
        }

        _wait = 0;
        int speed = PlaytestSettings.BattleSpeed;
        for (int i = 0; i < speed; i++)
        {
            if (!Step())
            {
                _auto = false;
                Publish();
                return;
            }
        }
    }

    /// <summary>Reloads the §2.15 training arena. The debug console calls this.</summary>
    public void Reset() => LoadGolden();

    public void LoadGolden()
    {
        Seed = TrainingArena.GoldenSeed;
        Swap(TrainingArena.CreateGolden());
    }

    /// <summary>Restarts the current party preset on the showcase seed.</summary>
    public void StartEncounter() => StartEncounter(Preset);

    /// <summary>Starts one of the default parties on the showcase seed.</summary>
    public void StartEncounter(int preset)
    {
        if (preset < 0 || preset >= PartyPreset.Count)
        {
            preset = PartyPreset.IceLattice;
        }

        Preset = preset;
        Seed = CarapaceEncounter.ShowcaseSeed;
        InFloorFight = false;
        Swap(CarapaceEncounter.Create(preset, Seed));
    }

    /// <summary>Opens a linear Cinder Throat floor on the showcase seed.</summary>
    public void StartFloor(int preset)
    {
        if (preset < 0 || preset >= PartyPreset.Count)
        {
            preset = PartyPreset.IceLattice;
        }

        StartFloor(HeroRoster.PresetIds(preset));
    }

    /// <summary>Opens the floor for a chosen four. A preset match keeps that preset's decks.</summary>
    public void StartFloor(IReadOnlyList<int> ids)
    {
        if (!HeroRoster.Check(ids).CanStart)
        {
            return;
        }

        PartyIds = CopyIds(ids);
        Preset = HeroRoster.MatchPreset(PartyIds);
        BossOnly = false;
        InFloorFight = false;
        Seed = CarapaceEncounter.ShowcaseSeed;
        _floor = FloorRun.StartCustom(PartyIds, Seed);
        BeginRunLog();
        EmitSignal(SignalName.FloorChanged);
    }

    /// <summary>Boss only, on the showcase seed, for the chosen four.</summary>
    public void StartBoss(IReadOnlyList<int> ids)
    {
        if (!HeroRoster.Check(ids).CanStart)
        {
            return;
        }

        PartyIds = CopyIds(ids);
        Preset = HeroRoster.MatchPreset(PartyIds);
        _floor = null;
        InFloorFight = false;
        BossOnly = true;
        Seed = CarapaceEncounter.ShowcaseSeed;
        HeroState[] heroes = Preset >= 0
            ? HeroRoster.PresetParty(Preset)
            : HeroRoster.CustomParty(PartyIds);
        Swap(CarapaceEncounter.Create(heroes, Seed));
        EmitSignal(SignalName.FloorChanged);
    }

    private static int[] CopyIds(IReadOnlyList<int> ids)
    {
        var copy = new int[ids.Count];
        for (int i = 0; i < ids.Count; i++)
        {
            copy[i] = ids[i];
        }

        return copy;
    }

    public void LeaveFloor()
    {
        if (_floor != null && !_floor.Finished)
        {
            FinishRunLog("abandoned");
        }

        _floor = null;
        InFloorFight = false;
        BossOnly = false;
        EmitSignal(SignalName.FloorChanged);
    }

    /// <summary>Boss only. The floor map stays behind the existing party picker.</summary>
    public void StartBossOnly()
    {
        _floor = null;
        InFloorFight = false;
        BossOnly = true;
        EmitSignal(SignalName.FloorChanged);
    }

    public void LoadFloorFight()
    {
        if (_floor == null || _floor.Finished || _floor.Current.Kind == FloorRoomKind.Rest)
        {
            return;
        }

        InFloorFight = true;
        BossOnly = false;
        Preset = _floor.Preset;
        Seed = _floor.FightSeed;
        _runLog.Add($"-- {_floor.Current.Name} --");
        Swap(_floor.BeginFight());
        EmitSignal(SignalName.FloorChanged);
    }

    public void CommitFloorFight()
    {
        if (_floor == null || !InFloorFight)
        {
            return;
        }

        _floor.Commit(_sim);
        InFloorFight = false;
        if (_floor.Finished)
        {
            FinishRunLog(_floor.Cleared ? "cleared" : "wiped");
        }

        EmitSignal(SignalName.FloorChanged);
    }

    public void TakeFloorRest()
    {
        if (_floor == null || _floor.Finished || _floor.Current.Kind != FloorRoomKind.Rest)
        {
            return;
        }

        _floor.Rest();
        InFloorFight = false;
        EmitSignal(SignalName.FloorChanged);
    }

    /// <summary>Gambit auto-run, instantly. Used by the floor's To end button.</summary>
    public void RunToEnd()
    {
        _auto = false;
        _wait = 0;
        if (_sim.Paused)
        {
            _sim.Resume();
        }

        _sim.RunToEnd();
        Flush();
        Publish();
    }

    private void Swap(BattleSimulator sim)
    {
        _sim = sim;
        _shown = 0;
        _auto = false;
        _wait = 0;
        EmitSignal(SignalName.LogCleared);
        Publish();
    }

    public bool Step()
    {
        if (_sim.Paused)
        {
            return false;
        }

        bool advanced = _sim.TryStep();
        Flush();
        Publish();
        return advanced;
    }

    /// <summary>One tick. Works while paused and leaves the pause flag set.</summary>
    public bool StepOnce()
    {
        bool advanced = _sim.StepOne();
        Flush();
        Publish();
        return advanced;
    }

    public void TogglePause()
    {
        if (_sim.Paused)
        {
            _sim.Resume();
        }
        else
        {
            _sim.Pause();
            _auto = false;
        }

        _wait = 0;
        Publish();
    }

    public void QueueAbility(int heroSlot, int abilityId, int targetPart, int targetHero)
    {
        _sim.QueueManual(heroSlot, abilityId, targetPart, targetHero);
        Publish();
    }

    public void ToggleAuto()
    {
        _auto = !_auto;
        _wait = 0;
        Publish();
    }

    private void Publish()
    {
        EmitSignal(SignalName.ResonanceChanged, ResonanceReadout.Summarize(_sim));
        EmitSignal(SignalName.StateChanged);
    }

    private void Flush()
    {
        var events = _sim.Events;
        while (_shown < events.Count)
        {
            var evt = events[_shown];
            _shown++;
            string line = $"t={evt.Tick}  {evt.Text}";
            _runLog.Add(line);
            EmitSignal(SignalName.LogLine, line);
        }
    }

    /// <summary>
    /// Plays the current floor to a result. Editor screenshots use this. Release builds do not.
    /// </summary>
    public void FastForwardFloor()
    {
        if (!BuildStamp.DevSession || _floor == null)
        {
            return;
        }

        while (_floor != null && !_floor.Finished)
        {
            if (_floor.Current.Kind == FloorRoomKind.Rest)
            {
                TakeFloorRest();
                continue;
            }

            LoadFloorFight();
            RunToEnd();
            CommitFloorFight();
        }
    }

    private void BeginRunLog()
    {
        _runLog.Clear();
        _runLogged = false;
        _runStartedMs = Time.GetTicksMsec();
    }

    private void FinishRunLog(string result)
    {
        if (_runLogged || _floor == null)
        {
            return;
        }

        _runLogged = true;
        ulong now = Time.GetTicksMsec();
        ulong duration = now >= _runStartedMs ? now - _runStartedMs : 0;
        RunLog.WriteRun(
            result,
            _floor.Seed,
            _floor.PartyLabel,
            _floor.RoomsCleared,
            FloorRun.RoomCount,
            _floor.Ticks,
            duration,
            _runLog);
    }
}
