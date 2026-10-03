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

    [Signal]
    public delegate void LogLineEventHandler(string line);

    [Signal]
    public delegate void StateChangedEventHandler();

    [Signal]
    public delegate void LogClearedEventHandler();

    [Signal]
    public delegate void ResonanceChangedEventHandler(string summary);

    public BattleSimulator Simulation => _sim;

    public ulong Seed { get; private set; } = CarapaceEncounter.ShowcaseSeed;

    public int Preset { get; private set; } = PartyPreset.IceLattice;

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
        if (!Step())
        {
            _auto = false;
            Publish();
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
        Swap(CarapaceEncounter.Create(preset, Seed));
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
            EmitSignal(SignalName.LogLine, $"t={evt.Tick}  {evt.Text}");
        }
    }
}
