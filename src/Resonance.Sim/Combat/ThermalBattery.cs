namespace Resonance.Sim.Combat;

public enum PrimedStrike
{
    /// <summary>The weapon skill's property is already a valid L2 or L3 transition. Detonation damage +25%.</summary>
    NaturalWithBonus,
    /// <summary>Otherwise the strike forces Liquefaction, opens that window, and still gains +25% detonation damage.</summary>
    ForcedLiquefaction,
}

/// <summary>Ash-Dravan primed weapon-skill rule (02 §2.12.4). Heat math lives on <see cref="Formulas"/>.</summary>
public static class ThermalBattery
{
    public static PrimedStrike Decide(bool validL2OrL3)
    {
        return validL2OrL3 ? PrimedStrike.NaturalWithBonus : PrimedStrike.ForcedLiquefaction;
    }

    public static int BonusDetonation(int detonationDamage)
    {
        return (int)((long)detonationDamage * 12_500 / 10_000);
    }
}
