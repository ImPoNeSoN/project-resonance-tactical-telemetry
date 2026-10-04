using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;

namespace Resonance.Sim.Tests;

public class FormulaTests
{
    [Theory]
    [InlineData(358, 140, 9_500)]
    [InlineData(300, 263, 9_350)]
    [InlineData(220, 140, 9_500)]
    [InlineData(340, 240, 9_500)]
    [InlineData(200, 200, 7_500)]
    [InlineData(0, 10_000, 500)]
    [InlineData(10_000, 0, 9_500)]
    public void Hit_chance_matches_the_clamp(int accuracy, int evasion, int expectedBp)
    {
        Assert.Equal(expectedBp, Formulas.HitChanceBp(accuracy, evasion));
    }

    [Fact]
    public void Physical_and_magical_dr_use_the_documented_constants()
    {
        Assert.Equal(4_565, Formulas.PhysicalDrBp(420));
        Assert.Equal(3_333, Formulas.MagicalDrBp(300));
        Assert.Equal(0, Formulas.PhysicalDrBp(0));
        Assert.Equal(5_000, Formulas.PhysicalDrBp(500));
    }

    [Fact]
    public void Worked_physical_hits_floor_once()
    {
        Assert.Equal(505, Hit(465, 20_000, 420, 0, crit: false));
        Assert.Equal(758, Hit(465, 20_000, 420, 0, crit: true, critMult: 15_000));
        Assert.Equal(540, Hit(520, 16_000, 270, 0, crit: false));
        Assert.Equal(374, Hit(313, 22_000, 420, 0, crit: false));
        Assert.Equal(387, Hit(440, 18_000, 420, 1_000, crit: false));
    }

    [Fact]
    public void Worked_magical_hits_floor_once()
    {
        Assert.Equal(3_849, Spell(666, 30_000, 300, resisted: false, critMult: 17_000, bucket: 7_000, diminish: 10_000));
        Assert.Equal(1_924, Spell(666, 30_000, 300, resisted: false, critMult: 10_000, bucket: 7_000, diminish: 8_500));
        Assert.Equal(597, Spell(380, 22_000, 240, resisted: false, critMult: 10_000, bucket: 0, diminish: 10_000));
    }

    [Fact]
    public void Resisted_spells_deal_half_and_true_bonus_is_separate()
    {
        int full = Spell(666, 30_000, 300, resisted: false, critMult: 10_000, bucket: 0, diminish: 10_000);
        int resisted = Spell(666, 30_000, 300, resisted: true, critMult: 10_000, bucket: 0, diminish: 10_000);
        Assert.Equal(full / 2, resisted);

        DamageResult burst = DamagePipeline.Resolve(new DamageRequest
        {
            Power = 666,
            MultiplierBp = 30_000,
            MitigationStat = 300,
            MitigationConstant = SimConst.MagicalDrConstant,
            CritMultiplierBp = SimConst.Bp,
            BurstBucketBp = 0,
            BurstDiminishBp = SimConst.Bp,
            TrueBonusBp = 7_000,
        });
        Assert.True(burst.TrueDamage > 0);
        Assert.Equal(burst.Dealt * 7_000 / 10_000, burst.TrueDamage);
    }

    [Fact]
    public void Crit_chance_caps_then_burst_can_pass_them()
    {
        Assert.Equal(1_790, Formulas.PhysicalCritChanceBp(358, 140, 50, 0, bursting: false));
        Assert.Equal(900, Formulas.PhysicalCritChanceBp(220, 140, 0, 0, bursting: false));
        Assert.Equal(5_000, Formulas.PhysicalCritChanceBp(5_000, 0, 500, 4_000, bursting: false));
        Assert.Equal(10_000, Formulas.PhysicalCritChanceBp(5_000, 0, 500, 4_000, bursting: true));
        Assert.Equal(500, Formulas.MagicalCritChanceBp(0, bursting: false));
        Assert.Equal(5_500, Formulas.MagicalCritChanceBp(0, bursting: true));
        Assert.Equal(10_000, Formulas.MagicalCritChanceBp(6_000, bursting: true));
        Assert.Equal(17_000, Formulas.CritMultiplierBp(2_000));
        Assert.Equal(25_000, Formulas.CritMultiplierBp(20_000));
        Assert.Equal(29_000, Formulas.CritMultiplierBp(20_000, capBp: 29_000));
    }

    [Fact]
    public void Interrupt_matches_the_worked_examples_and_clamps()
    {
        int kith = Formulas.InterruptChanceBp(900, 3_710, 20);
        Assert.InRange(kith, 4_060, 4_070);
        Assert.Equal(0, Formulas.InterruptChanceBp(1_924, 120_000, 40));
        Assert.Equal(0, Formulas.InterruptChanceBp(0, 3_710, 0));
        Assert.Equal(9_500, Formulas.InterruptChanceBp(10_000, 100, 0));
        Assert.Equal(6_000, Formulas.AbilityInterruptChanceBp(6_000, 1_000, 0, forceCertain: false));
        Assert.Equal(9_500, Formulas.AbilityInterruptChanceBp(6_000, 1_000, 5_000, forceCertain: false));
        Assert.Equal(10_000, Formulas.AbilityInterruptChanceBp(0, 0, 0, forceCertain: true));
    }

    [Theory]
    [InlineData(1_000, 3_000, 0, 0, 700)]
    [InlineData(900, 3_000, 1_500, 0, 495)]
    [InlineData(1_500, 3_000, 1_500, 0, 825)]
    [InlineData(400, 3_000, 1_500, 0, 220)]
    [InlineData(0, 6_500, 0, 0, 0)]
    public void Chant_time_ceils_after_fast_cast(int chant, int gear, int race, int buff, int expected)
    {
        int fc = Formulas.TotalFastCastBp(gear, race, buff);
        Assert.Equal(expected, Formulas.EffectiveChantTicks(chant, fc));
    }

    [Fact]
    public void Fast_cast_caps_gear_at_50_and_total_at_65()
    {
        Assert.Equal(6_500, Formulas.TotalFastCastBp(8_000, 1_500, 2_000));
        Assert.Equal(5_000, Formulas.TotalFastCastBp(8_000, 0, 0));
        Assert.Equal(6_500, Formulas.TotalFastCastBp(5_000, 1_500, 0));
    }

    [Fact]
    public void Effective_attack_follows_the_property_table()
    {
        Assert.Equal(465, Formulas.EffectiveAttack(415, 0, 0, 50, WeaponProperty.Piercing));
        Assert.Equal(440, Formulas.EffectiveAttack(415, 0, 0, 50, WeaponProperty.ElementalPhysical));
        Assert.Equal(313, Formulas.EffectiveAttack(273, 0, 40, 0, WeaponProperty.Blunt));
        Assert.Equal(115, Formulas.EffectiveAttack(100, 1_000, 0, 10, WeaponProperty.Slashing));
        // Odd inputs truncate. 1/2 → 0, (1+2)/2 → 1.
        Assert.Equal(51, Formulas.EffectiveAttack(0, 0, 1, 51, WeaponProperty.Piercing));
        Assert.Equal(0, Formulas.EffectiveAttack(0, 0, 0, 1, WeaponProperty.Blunt));
        Assert.Equal(1, Formulas.EffectiveAttack(0, 0, 1, 2, WeaponProperty.ElementalPhysical));
    }

    [Fact]
    public void Magic_accuracy_truncates_odd_sums()
    {
        Assert.Equal(3, Formulas.MagicAccuracy(5, 2, 0));
        Assert.Equal(13, Formulas.MagicAccuracy(5, 2, 10));
    }

    [Fact]
    public void Level3_bonus_is_true_damage_outside_the_burst_bucket()
    {
        BurstProfile magma = ResonanceTable.Profile(ApexId.MagmaCore);
        BurstProfile tempest = ResonanceTable.Profile(ApexId.TempestCrown);
        Assert.Equal(0, magma.BucketBonusBp);
        Assert.Equal(11_000, magma.TrueBonusBp);
        Assert.Equal(0, tempest.BucketBonusBp);
        Assert.Equal(11_000, tempest.TrueBonusBp);

        var asWritten = DamagePipeline.Resolve(new DamageRequest
        {
            Power = 1_000,
            MultiplierBp = 10_000,
            MitigationStat = 0,
            MitigationConstant = SimConst.MagicalDrConstant,
            CritMultiplierBp = 10_000,
            BurstBucketBp = 2_000,
            BurstDiminishBp = 10_000,
            TrueBonusBp = magma.TrueBonusBp,
        });
        Assert.Equal(1_200, asWritten.Dealt);
        Assert.Equal(1_320, asWritten.TrueDamage);

        var foldedIntoBucket = DamagePipeline.Resolve(new DamageRequest
        {
            Power = 1_000,
            MultiplierBp = 10_000,
            MitigationStat = 0,
            MitigationConstant = SimConst.MagicalDrConstant,
            CritMultiplierBp = 10_000,
            BurstBucketBp = 2_000 + 11_000,
            BurstDiminishBp = 10_000,
            TrueBonusBp = 0,
        });
        Assert.Equal(2_300, foldedIntoBucket.Dealt);
        Assert.Equal(0, foldedIntoBucket.TrueDamage);
        Assert.NotEqual(asWritten.Total, foldedIntoBucket.Total);
    }

    [Fact]
    public void Resistance_penetration_never_deepens_a_weakness()
    {
        Assert.Equal(0, Formulas.EffectiveResistanceBp(1_000, 75));
        Assert.Equal(1_000, Formulas.EffectiveResistanceBp(2_500, 75));
        Assert.Equal(-2_000, Formulas.EffectiveResistanceBp(-2_000, 150));
        Assert.Equal(0, Formulas.EffectiveResistanceBp(100, 150));
        Assert.Equal(6_000, Formulas.EffectiveResistanceBp(9_000, 150));
    }

    [Fact]
    public void Heat_gain_matches_varra_examples()
    {
        Assert.Equal(7, Formulas.HeatGain(200, 8_740, fire: false));
        Assert.Equal(30, Formulas.HeatGain(874, 8_740, fire: false));
        Assert.Equal(45, Formulas.HeatGain(874, 8_740, fire: true));
        Assert.Equal(104, Formulas.HeatGain(2_000, 8_740, fire: true));
        Assert.Equal(3, Formulas.HeatGain(40, 8_740, fire: false));
        Assert.Equal(100, Formulas.ApplyHeat(0, 104));
        Assert.Equal(PrimedStrike.ForcedLiquefaction, ThermalBattery.Decide(validL2OrL3: false));
        Assert.Equal(PrimedStrike.NaturalWithBonus, ThermalBattery.Decide(validL2OrL3: true));
        Assert.Equal(125, ThermalBattery.BonusDetonation(100));
    }

    [Fact]
    public void Enmity_decay_shed_and_generation()
    {
        Assert.Equal(1_980, Formulas.DecayVolatile(2_200, heavyBucket: false));
        Assert.Equal(1_120, Formulas.DecayVolatile(1_400, heavyBucket: true));
        Assert.Equal(32, Formulas.CeShed(540, 540, 4_480));
        Assert.Equal(76, Formulas.CeShed(2_379, 597, 9_240));
        Assert.Equal(250, Formulas.CeShed(1_000, 9_000, 1_000));
        Assert.Equal(0, Formulas.CeShed(1_000, 0, 1_000));
        Assert.Equal(330, Formulas.AbilityCe(300, 1_000, shelter: false, aethelBorn: false));
        Assert.Equal(60, Formulas.AbilityCe(100, 0, shelter: false, aethelBorn: true));
        Assert.Equal(120, Formulas.AbilityCe(200, 0, shelter: false, aethelBorn: true));
        Assert.Equal(307, Formulas.DamageCe(3_849, aethelBorn: false));
        Assert.Equal(216, Formulas.HealVe(540));
        Assert.Equal(180, Formulas.ChitinousVe(1_200));
        Assert.Equal(117, Formulas.MpRefund(180, Formulas.BurstRefundBp(kithLir: true, 0)));
        Assert.Equal(90, Formulas.MpRefund(180, Formulas.BurstRefundBp(kithLir: false, 0)));
        Assert.Equal(8_000, Formulas.BurstRefundBp(kithLir: true, 3_000));
    }

    [Fact]
    public void Volatile_cap_and_argmax_ties()
    {
        var table = new EnmitySlot[3];
        EnmityMath.AddVolatile(ref table[0], 20_000, heavy: false);
        EnmityMath.AddVolatile(ref table[0], 15_000, heavy: true);
        Assert.Equal(30_000, table[0].Ve);

        EnmityMath.AddCumulative(ref table[0], 40_000);
        Assert.Equal(30_000, table[0].Ce);

        table[1].NormalVe = 100;
        table[1].Ce = 50;
        table[2].NormalVe = 80;
        table[2].Ce = 70;
        bool[] alive = [true, true, true];
        Assert.Equal(0, EnmityMath.ArgMax(table, alive));

        table[0] = default;
        table[1].NormalVe = 100;
        table[1].Ce = 40;
        table[2].NormalVe = 80;
        table[2].Ce = 60;
        Assert.Equal(2, EnmityMath.ArgMax(table, alive));

        table[1].Ce = 60;
        table[1].NormalVe = 40;
        table[2].Ce = 60;
        table[2].NormalVe = 40;
        Assert.Equal(1, EnmityMath.ArgMax(table, alive));

        alive[1] = false;
        Assert.Equal(2, EnmityMath.ArgMax(table, alive));
    }

    [Fact]
    public void Gauge_keeps_overshoot_allows_debt_and_caps_refunds()
    {
        var gauge = new ApGauge { Centi = 999_500 };
        gauge.Gain(1_800, 1);
        Assert.Equal(1_001_300, gauge.Centi);
        gauge.PayRecovery(14_000);
        Assert.Equal(-398_700, gauge.Centi);
        gauge.PayRecovery(14_000);
        Assert.Equal(SimConst.DebtFloorCenti, gauge.Centi);

        gauge.Centi = 900_000;
        Assert.Equal(1_000, gauge.Refund(2_000));
        Assert.Equal(SimConst.RefundCeilingCenti, gauge.Centi);
        Assert.Equal(0, gauge.Refund(500));

        Assert.Equal(270_000, ApGauge.InitialCenti(9, ambushed: false));
        Assert.Equal(540_000, ApGauge.InitialCenti(18, ambushed: false));
        Assert.Equal(600_000, ApGauge.InitialCenti(40, ambushed: false));
        Assert.Equal(0, ApGauge.InitialCenti(18, ambushed: true));
        Assert.Equal(256, ApGauge.TicksUntilReady(540_000, 1_800));
    }

    [Fact]
    public void Charge_fills_from_ticks_until_the_next_action()
    {
        int full = ApGauge.TicksUntilReady(0, 1_800);
        int left = ApGauge.TicksUntilReady(540_000, 1_800);
        Assert.Equal(256, left);
        Assert.Equal(0, ApGauge.ChargeBp(full, full));
        Assert.Equal(10_000, ApGauge.ChargeBp(0, full));
        Assert.Equal(0, ApGauge.ChargeBp(full + 40, full));
        Assert.Equal((full - left) * 10_000 / full, ApGauge.ChargeBp(left, full));
        Assert.Equal(5_395, ApGauge.NextActionChargeBp(540_000, 1_800, casting: false, 0, 0, 0));
        Assert.Equal(10_000, ApGauge.NextActionChargeBp(SimConst.ReadyCenti, 1_800, casting: false, 0, 0, 0));
        Assert.Equal(0, ApGauge.NextActionChargeBp(-50_000, 1_800, casting: false, 0, 0, 0));
        Assert.Equal(0, ApGauge.NextActionChargeBp(0, 0, casting: false, 0, 0, 0));
        Assert.Equal(10_000, ApGauge.NextActionChargeBp(SimConst.ReadyCenti, 0, casting: false, 0, 0, 0));

        Assert.Equal(0, ApGauge.NextActionChargeBp(SimConst.ReadyCenti, 1_800, casting: true, 100, 300, 100));
        Assert.Equal(5_000, ApGauge.NextActionChargeBp(SimConst.ReadyCenti, 1_800, casting: true, 100, 300, 200));
        Assert.Equal(10_000, ApGauge.NextActionChargeBp(SimConst.ReadyCenti, 1_800, casting: true, 100, 300, 300));
    }

    [Fact]
    public void Haste_and_slow_change_centi_ap_and_cap()
    {
        Assert.Equal(840, Formulas.AgiCentiPerTick(12, 0, 3_000));
        Assert.Equal(1_800, Formulas.AgiCentiPerTick(12, 5_000, 0));
        Assert.Equal(1_800, Formulas.AgiCentiPerTick(12, 9_000, 0));
        Assert.Equal(600, Formulas.AgiCentiPerTick(12, 0, 9_000));
    }

    [Fact]
    public void Burst_diminish_and_healing()
    {
        Assert.Equal(10_000, Formulas.BurstDiminishBp(1));
        Assert.Equal(8_500, Formulas.BurstDiminishBp(2));
        Assert.Equal(5_000, Formulas.BurstDiminishBp(12));
        Assert.Equal(1_193, Formulas.HealAmount(373, 32_000, 0));
        Assert.Equal(55, FixedMath.MulBp(373, 1_500));
    }

    [Fact]
    public void Absorb_shields_block_partial_hits()
    {
        int shield = 100;
        int taken = DamagePipeline.ApplyAbsorb(250, ref shield, out int absorbed);
        Assert.Equal(100, absorbed);
        Assert.Equal(150, taken);
        Assert.Equal(0, shield);
    }

    [Fact]
    public void Resonance_matrix_matches_the_documented_routes()
    {
        Assert.Equal(15, ResonanceTable.CountL2Routes());
        Assert.Equal(8, ResonanceTable.CountL3Routes());
        Assert.Equal(ResonanceId.Induration, ResonanceTable.LookupL2(ChainProperty.Piercing, ChainProperty.Ice));
        Assert.Equal(ResonanceId.Liquefaction, ResonanceTable.LookupL2(ChainProperty.Blunt, ChainProperty.Fire));
        Assert.Equal(ResonanceId.Distortion, ResonanceTable.LookupL2(ChainProperty.Blunt, ChainProperty.Slashing));
        Assert.Equal(ResonanceId.Fragmentation, ResonanceTable.LookupL2(ChainProperty.Slashing, ChainProperty.Wind));
        Assert.Equal(ResonanceId.None, ResonanceTable.LookupL2(ChainProperty.Ice, ChainProperty.Fire));
        Assert.Equal(ResonanceId.None, ResonanceTable.LookupL2(ChainProperty.Ice, ChainProperty.Blunt));
        Assert.Equal(ApexId.UmbralZero, ResonanceTable.LookupL3(ResonanceId.Induration, ChainProperty.Darkness));
        Assert.Equal(ApexId.None, ResonanceTable.LookupL3(ResonanceId.Induration, ChainProperty.Blunt));
        Assert.Equal(ApexId.SolarApex, ResonanceTable.LookupL3(ResonanceId.Fragmentation, ChainProperty.Light));
        Assert.Equal(0, ResonanceTable.Profile(ResonanceId.Distortion).BucketBonusBp);
        Assert.Equal(7_000, ResonanceTable.Profile(ResonanceId.Distortion).TrueBonusBp);
        Assert.Equal(12_000, ResonanceTable.Profile(ApexId.SolarApex).TrueBonusBp);
        Assert.Equal(5_000, ResonanceTable.Profile(ResonanceId.Induration).BucketBonusBp);
    }

    [Fact]
    public void Seeded_rng_is_stable_per_stream()
    {
        var left = new EncounterRng(0x5EED_1234UL);
        var right = new EncounterRng(0x5EED_1234UL);
        for (int i = 0; i < 32; i++)
        {
            Assert.Equal(left.RollD10000(RngStream.Hit, "h"), right.RollD10000(RngStream.Hit, "h"));
        }

        var other = new EncounterRng(0x5EED_1234UL);
        int hit = other.RollD10000(RngStream.Hit, "h");
        int crit = other.RollD10000(RngStream.Crit, "c");
        Assert.InRange(hit, 0, 9_999);
        Assert.InRange(crit, 0, 9_999);

        var hits = new EncounterRng(0x5EED_1234UL);
        var crits = new EncounterRng(0x5EED_1234UL);
        bool diverged = false;
        for (int i = 0; i < 8; i++)
        {
            if (hits.RollD10000(RngStream.Hit, "h") != crits.RollD10000(RngStream.Crit, "c"))
            {
                diverged = true;
            }
        }

        Assert.True(diverged);
    }

    private static int Hit(int attack, int multiplier, int defense, int resistance, bool crit, int critMult = 15_000)
    {
        return DamagePipeline.Resolve(new DamageRequest
        {
            Power = attack,
            MultiplierBp = multiplier,
            MitigationStat = defense,
            MitigationConstant = SimConst.PhysicalDrConstant,
            EffectiveResistanceBp = resistance,
            CritMultiplierBp = crit ? critMult : SimConst.Bp,
            BurstDiminishBp = SimConst.Bp,
        }).Dealt;
    }

    private static int Spell(int intel, int multiplier, int meva, bool resisted, int critMult, int bucket, int diminish)
    {
        return DamagePipeline.Resolve(new DamageRequest
        {
            Power = intel,
            MultiplierBp = multiplier,
            MitigationStat = meva,
            MitigationConstant = SimConst.MagicalDrConstant,
            Resisted = resisted,
            CritMultiplierBp = critMult,
            BurstBucketBp = bucket,
            BurstDiminishBp = diminish,
            BypassPositiveResistance = true,
        }).Dealt;
    }
}
