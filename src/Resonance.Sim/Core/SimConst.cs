namespace Resonance.Sim.Core;

/// <summary>
/// Fixed-point scales from 02 §2.16. 10_000 bp = 100%. 100 centi-AP = 1 AP. 1_000_000 centi-AP = ready.
/// </summary>
public static class SimConst
{
    public const int Bp = 10_000;
    public const int CentiPerAp = 100;
    public const int ReadyCenti = 1_000_000;
    public const int DebtFloorCenti = -400_000;
    public const int RefundCeilingCenti = 1_000_000;

    public const int RecoveryStance = 4_000;
    public const int RecoveryStandard = 10_000;
    public const int RecoveryHeavy = 14_000;

    public const int VeCap = 30_000;
    public const int CeCap = 30_000;

    public const int ChainWindowTicks = 4_000;
    public const int BurstWindowTicks = 1_500;
    public const int BurstExtensionCap = 1_200;
    public const int KithExtensionTicks = 600;
    public const int EarlyWindowTicks = 300;
    public const int EarlyWindowExtension = 900;

    public const int GlobalPhase = 500;
    public const int HitFloorBp = 500;
    public const int HitCeilBp = 9_500;
    public const int InterruptCeilBp = 9_500;
    public const int CritCapBp = 5_000;
    public const int CritHardCapBp = 10_000;
    public const int BurstCritBonusBp = 5_000;
    public const int BaseCritBp = 500;
    public const int BaseCritMultiplierBp = 15_000;
    public const int CritMultiplierCapBp = 25_000;

    public const int HasteCapBp = 5_000;
    public const int SlowCapBp = 5_000;
    public const int GearFastCastCapBp = 5_000;
    public const int TotalFastCastCapBp = 6_500;
    public const int GearMbdCapBp = 4_000;

    public const int PhysicalDrConstant = 500;
    public const int MagicalDrConstant = 600;

    public const int AethelCeBp = 6_000;
    public const int AethelFastCastBp = 1_500;
    public const int VeDecayNumerator = 90;
    public const int HeavyVeDecayNumerator = 80;
    public const int DamageCeBp = 800;
    public const int HealVeBp = 4_000;
    public const int HeavyHealThreshold = 1_500;
    public const int HeavyVeThreshold = 800;

    public const int InitialApCap = 6_000;
    public const int InitialApPerAgi = 300;

    public const int CadenceRefundAp = 1_200;
    public const int PounceDelayAp = 1_500;
    public const int PounceWindowMinAp = 8_000;
    public const int IndurationSlowBp = 3_000;
    public const int IndurationSlowTicks = 8_000;
    public const int KithRefundBp = 6_500;
    public const int StandardBurstRefundBp = 5_000;
    public const int RefundCapBp = 8_000;

    public const int ShelterDefBp = 2_500;
    public const int ShelterCeBp = 3_000;
    public const int ShelterCastingAllies = 2;
}
