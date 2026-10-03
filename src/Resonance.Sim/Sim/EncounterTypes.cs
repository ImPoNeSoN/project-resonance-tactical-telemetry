namespace Resonance.Sim.Sim;

public enum LoadoutKind
{
    Idle,
    FastCast,
    MidCast,
    Weapon,
}

public enum FightOutcome
{
    Ongoing,
    Victory,
    Defeat,
}

/// <summary>
/// Deterministic boss rotation. Ability choice does not draw RNG.
/// Frenzy starts when the Core is at or under <see cref="PhaseHpBp"/> of its max HP,
/// or when the clock reaches <see cref="EnrageTick"/>. A later hard enrage is a loss.
/// </summary>
public sealed class BossScript
{
    public int Cursor;
    public bool Frenzy;
    public int PhaseHpBp = 5_000;
    public int EnrageTick = 70_000;
    public int HardEnrageTick = 110_000;
    public int FrenzyBonusBp = 6_000;
    public int[] Phase1 = [];
    public int[] Phase1Part = [];
    public int[] Phase2 = [];
    public int[] Phase2Part = [];
}
