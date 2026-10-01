namespace Resonance.Sim.Core;

/// <summary>
/// PCG-XSH-RR 32-bit generator (O'Neill). One encounter seed, one stream id per roll family
/// so a new roll type cannot shift hit or crit results (02 §2.16.1).
/// </summary>
public struct Pcg32
{
    private ulong _state;
    private ulong _inc;

    public Pcg32(ulong seed, ulong stream)
    {
        _state = 0;
        _inc = (stream << 1) | 1UL;
        NextUInt();
        _state += seed;
        NextUInt();
    }

    public uint NextUInt()
    {
        ulong old = _state;
        _state = unchecked((old * 6364136223846793005UL) + _inc);
        uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
        int rot = (int)(old >> 59);
        return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
    }

    public int NextD10000() => (int)(NextUInt() % 10_000u);
}

public enum RngStream
{
    Hit = 0,
    Crit = 1,
    MultiAttack = 2,
    Interrupt = 3,
    Proc = 4,
    Ai = 5,
}

public interface IRng
{
    int RollD10000(RngStream stream, string reason);
}

public sealed class EncounterRng : IRng
{
    private readonly Pcg32[] _streams;

    public EncounterRng(ulong seed)
    {
        _streams = new Pcg32[6];
        for (int i = 0; i < _streams.Length; i++)
        {
            _streams[i] = new Pcg32(seed, (ulong)(i + 1));
        }
    }

    public int RollD10000(RngStream stream, string reason) => _streams[(int)stream].NextD10000();
}

/// <summary>Replay of published d10000 rolls. The worked log does not publish a PCG seed.</summary>
public sealed class ScriptedRng : IRng
{
    private readonly Queue<int>[] _queues;
    private readonly List<string> _trace = new();

    public ScriptedRng(IReadOnlyDictionary<RngStream, IReadOnlyList<int>> rolls)
    {
        _queues = new Queue<int>[6];
        for (int i = 0; i < _queues.Length; i++)
        {
            _queues[i] = new Queue<int>();
        }

        foreach (var pair in rolls)
        {
            foreach (int roll in pair.Value)
            {
                _queues[(int)pair.Key].Enqueue(roll);
            }
        }
    }

    public IReadOnlyList<string> Trace => _trace;

    public bool IsExhausted
    {
        get
        {
            for (int i = 0; i < _queues.Length; i++)
            {
                if (_queues[i].Count > 0)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public int RollD10000(RngStream stream, string reason)
    {
        var queue = _queues[(int)stream];
        if (queue.Count == 0)
        {
            throw new InvalidOperationException($"RNG underrun on {stream} during {reason}. Trace: {string.Join(" | ", _trace)}");
        }

        int roll = queue.Dequeue();
        _trace.Add($"{stream}:{roll}:{reason}");
        return roll;
    }
}
