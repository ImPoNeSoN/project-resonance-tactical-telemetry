using Resonance.Sim.Core;

namespace Resonance.Sim.Combat;

public enum DamageFamily
{
    Physical,
    Magical,
}

public enum WeaponProperty
{
    Blunt,
    Slashing,
    Piercing,
    ElementalPhysical,
    None,
}

public static class Formulas
{
    public static int AgiCentiPerTick(int agi, int hasteBp, int slowBp)
    {
        hasteBp = FixedMath.Clamp(hasteBp, 0, SimConst.HasteCapBp);
        slowBp = FixedMath.Clamp(slowBp, 0, SimConst.SlowCapBp);
        int factor = SimConst.Bp + hasteBp - slowBp;
        if (factor < 0)
        {
            factor = 0;
        }

        return (int)((long)agi * SimConst.CentiPerAp * factor / SimConst.Bp);
    }

    public static int TotalFastCastBp(int gearBp, int raceBp, int buffBp)
    {
        int gear = gearBp > SimConst.GearFastCastCapBp ? SimConst.GearFastCastCapBp : gearBp;
        if (gear < 0)
        {
            gear = 0;
        }

        int total = gear + raceBp + buffBp;
        return total > SimConst.TotalFastCastCapBp ? SimConst.TotalFastCastCapBp : total;
    }

    public static int EffectiveChantTicks(int chantTicks, int fastCastBp)
    {
        if (chantTicks <= 0)
        {
            return 0;
        }

        int remain = SimConst.Bp - FixedMath.Clamp(fastCastBp, 0, SimConst.Bp);
        return (int)FixedMath.CeilDiv((long)chantTicks * remain, SimConst.Bp);
    }

    /// <summary>Hit% = clamp(0.75 + (ACC − EVA) / 200, 0.05, 0.95). Returns basis points.</summary>
    public static int HitChanceBp(int accuracy, int evasion)
    {
        int hit = 7_500 + ((accuracy - evasion) * 50);
        return FixedMath.Clamp(hit, SimConst.HitFloorBp, SimConst.HitCeilBp);
    }

    public static int MagicAccuracy(int intelligence, int accuracy, int gearMacc)
    {
        return ((intelligence + accuracy) / 2) + gearMacc;
    }

    public static int RatioBp(int stat, int constant)
    {
        if (stat <= 0)
        {
            return 0;
        }

        return (int)((long)stat * SimConst.Bp / (stat + constant));
    }

    public static int PhysicalDrBp(int defense) => RatioBp(defense, SimConst.PhysicalDrConstant);

    public static int MagicalDrBp(int meva) => RatioBp(meva, SimConst.MagicalDrConstant);

    public static int EffectiveDefense(int defense, int defBuffBp, int shatterBp, int armorIgnoreBp)
    {
        long value = defense;
        value = value * (SimConst.Bp + defBuffBp) / SimConst.Bp;
        value = value * (SimConst.Bp - FixedMath.Clamp(shatterBp, 0, SimConst.Bp)) / SimConst.Bp;
        value = value * (SimConst.Bp - FixedMath.Clamp(armorIgnoreBp, 0, SimConst.Bp)) / SimConst.Bp;
        return value < 0 ? 0 : (int)value;
    }

    /// <summary>
    /// Penetration reduces positive resistance by 0.2 percentage points per EPEN and never deepens a weakness.
    /// </summary>
    public static int EffectiveResistanceBp(int resistanceBp, int epen)
    {
        if (epen > 150)
        {
            epen = 150;
        }

        if (epen < 0)
        {
            epen = 0;
        }

        if (resistanceBp <= 0)
        {
            return resistanceBp;
        }

        int reduced = resistanceBp - (epen * 20);
        return reduced < 0 ? 0 : reduced;
    }

    public static int PhysicalCritChanceBp(int accuracy, int evasion, int dex, int gearBp, bool bursting)
    {
        int margin = accuracy - evasion;
        if (margin < 0)
        {
            margin = 0;
        }

        int chance = SimConst.BaseCritBp + (margin * 5) + (dex * 4) + gearBp;
        if (chance > SimConst.CritCapBp)
        {
            chance = SimConst.CritCapBp;
        }

        if (bursting)
        {
            chance += SimConst.BurstCritBonusBp;
        }

        return chance > SimConst.CritHardCapBp ? SimConst.CritHardCapBp : chance;
    }

    public static int MagicalCritChanceBp(int gearBp, bool bursting)
    {
        int chance = SimConst.BaseCritBp + gearBp;
        if (chance > SimConst.CritCapBp)
        {
            chance = SimConst.CritCapBp;
        }

        if (bursting)
        {
            chance += SimConst.BurstCritBonusBp;
        }

        return chance > SimConst.CritHardCapBp ? SimConst.CritHardCapBp : chance;
    }

    public static int CritMultiplierBp(int critDamageGearBp, int capBp = SimConst.CritMultiplierCapBp)
    {
        int multiplier = SimConst.BaseCritMultiplierBp + critDamageGearBp;
        return multiplier > capBp ? capBp : multiplier;
    }

    /// <summary>Interrupt% = clamp((Damage / MaxHP) × 2.5 − Concentration × 0.01, 0, 0.95).</summary>
    public static int InterruptChanceBp(int damageTaken, int maxHp, int concentration)
    {
        if (maxHp <= 0 || damageTaken <= 0)
        {
            return 0;
        }

        int raw = (int)((long)damageTaken * 25_000 / maxHp) - (concentration * 100);
        return FixedMath.Clamp(raw, 0, SimConst.InterruptCeilBp);
    }

    public static int AbilityInterruptChanceBp(int flatBp, int damageInterruptBp, int additiveBp, bool forceCertain)
    {
        if (forceCertain)
        {
            return SimConst.Bp;
        }

        int combined = Math.Max(flatBp, damageInterruptBp) + additiveBp;
        return FixedMath.Clamp(combined, 0, SimConst.InterruptCeilBp);
    }

    public static int EffectiveAttack(int attack, int attackBuffBp, int strength, int dexterity, WeaponProperty property)
    {
        long scaled = (long)attack * (SimConst.Bp + attackBuffBp) / SimConst.Bp;
        long bonus = property switch
        {
            WeaponProperty.Blunt or WeaponProperty.Slashing => strength + (dexterity / 2),
            WeaponProperty.Piercing => dexterity + (strength / 2),
            WeaponProperty.ElementalPhysical => (strength + dexterity) / 2,
            _ => 0,
        };
        return (int)(scaled + bonus);
    }

    public static int HeatGain(int elementalDamageTaken, int maxHp, bool fire)
    {
        if (maxHp <= 0 || elementalDamageTaken <= 0)
        {
            return 0;
        }

        int gain = (int)FixedMath.CeilDiv(300L * elementalDamageTaken, maxHp);
        if (gain < 3)
        {
            gain = 3;
        }

        if (fire)
        {
            gain = (int)FixedMath.CeilDiv(gain * 3L, 2);
        }

        return gain;
    }

    public static int ApplyHeat(int current, int gain)
    {
        int next = current + gain;
        return next > 100 ? 100 : next;
    }

    public static int BurstDiminishBp(int burstIndex)
    {
        if (burstIndex <= 1)
        {
            return SimConst.Bp;
        }

        long value = SimConst.Bp;
        for (int i = 1; i < burstIndex; i++)
        {
            value = value * 8_500 / SimConst.Bp;
        }

        return value < 5_000 ? 5_000 : (int)value;
    }

    public static int MpRefund(int mpCost, int refundBp)
    {
        if (refundBp > SimConst.RefundCapBp)
        {
            refundBp = SimConst.RefundCapBp;
        }

        if (refundBp < 0 || mpCost <= 0)
        {
            return 0;
        }

        return (int)((long)mpCost * refundBp / SimConst.Bp);
    }

    public static int BurstRefundBp(bool kithLir, int chipRefundBp)
    {
        int baseRefund = kithLir ? SimConst.KithRefundBp : SimConst.StandardBurstRefundBp;
        int total = baseRefund + chipRefundBp;
        return total > SimConst.RefundCapBp ? SimConst.RefundCapBp : total;
    }

    public static int AbilityCe(int baseCe, int enmityPlusBp, bool shelter, bool aethelBorn)
    {
        long value = (long)baseCe * (SimConst.Bp + enmityPlusBp) / SimConst.Bp;
        if (shelter)
        {
            value = value * (SimConst.Bp + SimConst.ShelterCeBp) / SimConst.Bp;
        }

        if (aethelBorn)
        {
            value = value * SimConst.AethelCeBp / SimConst.Bp;
        }

        return (int)value;
    }

    public static int DamageCe(int finalDamage, bool aethelBorn)
    {
        long value = (long)finalDamage * SimConst.DamageCeBp / SimConst.Bp;
        if (aethelBorn)
        {
            value = value * SimConst.AethelCeBp / SimConst.Bp;
        }

        return (int)value;
    }

    public static int HealAmount(int intelligence, int multiplierBp, int potencyBp)
    {
        long value = (long)intelligence * multiplierBp / SimConst.Bp;
        value = value * (SimConst.Bp + potencyBp) / SimConst.Bp;
        return (int)value;
    }

    public static int CeShed(int cumulativeEnmity, int damageTaken, int maxHp)
    {
        if (cumulativeEnmity <= 0 || damageTaken <= 0 || maxHp <= 0)
        {
            return 0;
        }

        long ratio = 5_000L * damageTaken / maxHp;
        if (ratio > 2_500)
        {
            ratio = 2_500;
        }

        return (int)((long)cumulativeEnmity * ratio / SimConst.Bp);
    }

    public static int DecayVolatile(int volatileEnmity, bool heavyBucket)
    {
        int numerator = heavyBucket ? SimConst.HeavyVeDecayNumerator : SimConst.VeDecayNumerator;
        return volatileEnmity * numerator / 100;
    }

    public static int ChitinousVe(int physicalDamageTaken) => physicalDamageTaken * 15 / 100;

    public static int HealVe(int hpRestored) => (int)((long)hpRestored * SimConst.HealVeBp / SimConst.Bp);

    public static bool IsHeavyVeSource(int baseVe, int recoveryAp, int hpRestored)
    {
        return hpRestored >= SimConst.HeavyHealThreshold
            || baseVe >= SimConst.HeavyVeThreshold
            || recoveryAp >= SimConst.RecoveryHeavy;
    }
}
