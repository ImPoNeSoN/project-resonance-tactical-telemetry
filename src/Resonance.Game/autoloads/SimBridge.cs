using Godot;
using Resonance.Sim.Sim;

namespace Resonance.Game;

/// <summary>
/// Autoload that owns the pure <see cref="BattleSimulator"/> and drains its log once per step.
/// Presentation listens to the signals; the sim itself has no Godot types.
/// </summary>
public partial class SimBridge : Node
{
    private BattleSimulator _sim = TrainingArena.CreateGolden();
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

    public bool AutoRunning => _auto;

    public override void _Ready()
    {
        Publish();
    }

    public override void _Process(double delta)
    {
        if (!_auto)
        {
            return;
        }

        _wait += delta;
        if (_wait < 0.45)
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

    public void Reset()
    {
        _sim = TrainingArena.CreateGolden();
        _shown = 0;
        _auto = false;
        _wait = 0;
        EmitSignal(SignalName.LogCleared);
        Publish();
    }

    public bool Step()
    {
        bool advanced = _sim.TryStep();
        Flush();
        Publish();
        return advanced;
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
