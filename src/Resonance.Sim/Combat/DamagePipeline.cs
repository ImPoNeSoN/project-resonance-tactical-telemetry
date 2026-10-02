using System.Numerics;
using Resonance.Sim.Core;

namespace Resonance.Sim.Combat;

public readonly struct DamageRequest
{
    public int Power { get; init; }
    public int MultiplierBp { get; init; }
    public int MitigationStat { get; init; }
    public int MitigationConstant { get; init; }
    public int EffectiveResistanceBp { get; init; }
    public bool BypassPositiveResistance { get; init; }
    public bool Resisted { get; init; }
    public int CritMultiplierBp { get; init; }
    public int WeaponSkillBp { get; init; }
    public int BurstBucketBp { get; init; }
    public int DamageDealtBuffBp { get; init; }
    public int BurstDiminishBp { get; init; }
    public int DamageTakenReductionBp { get; init; }
    public int TrueBonusBp { get; init; }
}

public readonly struct DamageResult
{
    public DamageResult(int dealt, int trueDamage)
    {
        Dealt = dealt;
        TrueDamage = trueDamage;
    }

    public int Dealt { get; }
    public int TrueDamage { get; }
    public int Total => Dealt + TrueDamage;
}

public static class DamagePipeline
{
    public static DamageResult Resolve(in DamageRequest request)
    {
        if (request.Power <= 0 || request.MultiplierBp <= 0)
        {
            return new DamageResult(0, 0);
        }

        int dr = Formulas.RatioBp(request.MitigationStat, request.MitigationConstant);
        int resistance = request.EffectiveResistanceBp;
        if (request.BypassPositiveResistance && resistance > 0)
        {
            resistance = 0;
        }

        int takenBp = request.DamageTakenReductionBp;
        if (takenBp > 6_000)
        {
            takenBp = 6_000;
        }

        if (takenBp < 0)
        {
            takenBp = 0;
        }

        // BigInteger keeps the product exact. Ten basis-point factors overflow Int128, and
        // flooring between factors moves results like Seismic Maul off the worked log.
        BigInteger numerator = request.Power;
        int factors = 0;
        void Multiply(int bp)
        {
            numerator *= bp;
            factors++;
        }

        Multiply(request.MultiplierBp);
        Multiply(SimConst.Bp - dr);
        Multiply(SimConst.Bp - resistance);
        Multiply(request.Resisted ? 5_000 : SimConst.Bp);
        Multiply(request.CritMultiplierBp <= 0 ? SimConst.Bp : request.CritMultiplierBp);
        Multiply(SimConst.Bp + request.WeaponSkillBp);
        Multiply(SimConst.Bp + request.BurstBucketBp);
        Multiply(SimConst.Bp + request.DamageDealtBuffBp);
        Multiply(request.BurstDiminishBp <= 0 ? SimConst.Bp : request.BurstDiminishBp);
        Multiply(SimConst.Bp - takenBp);

        BigInteger denominator = BigInteger.Pow(SimConst.Bp, factors);
        BigInteger quotient = numerator / denominator;
        int step9 = quotient > int.MaxValue ? int.MaxValue : (int)quotient;
        if (step9 < 0)
        {
            step9 = 0;
        }

        int trueDamage = 0;
        if (request.TrueBonusBp > 0)
        {
            trueDamage = (int)((long)step9 * request.TrueBonusBp / SimConst.Bp);
        }

        return new DamageResult(step9, trueDamage);
    }

    public static int ApplyAbsorb(int damage, ref int shieldPool, out int absorbed)
    {
        absorbed = 0;
        if (damage <= 0 || shieldPool <= 0)
        {
            return damage;
        }

        absorbed = shieldPool < damage ? shieldPool : damage;
        shieldPool -= absorbed;
        return damage - absorbed;
    }
}
