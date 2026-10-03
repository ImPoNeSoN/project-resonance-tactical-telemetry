using System.Collections.Generic;
using System.Text;
using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Sim;

namespace Resonance.Sim.Data;

/// <summary>
/// Read-only preview of what an ability does for a specific hero, ally, and part.
/// Numbers come from <see cref="Formulas"/> and <see cref="DamagePipeline"/>. This does not
/// resolve combat, roll RNG, or add effects the simulator does not model.
/// </summary>
public static class AbilityForecast
{
    public static string Effect(
        AbilityDef ability,
        HeroState actor,
        HeroState ally,
        BossState boss,
        int partIndex,
        int tick,
        IReadOnlyList<HeroState> party)
    {
        partIndex = ClampPart(boss, partIndex);
        var sentences = new List<string>();
        if (ability.Kind == AbilityKind.Healing)
        {
            sentences.Add(HealSentence(ability, actor, ally));
        }
        else if (ability.Effect != SupportEffect.None)
        {
            sentences.AddRange(SupportSentences(ability, actor, ally));
        }
        else if (ability.DealsDamage)
        {
            sentences.AddRange(DamageSentences(ability, actor, boss, partIndex, tick));
        }
        else
        {
            sentences.Add("No damage.");
        }

        if (ability.FlatInterruptBp > 0)
        {
            sentences.Add($"{Percent(ability.FlatInterruptBp)}% chance to interrupt a chant on that part when the hit lands.");
        }

        if (ability.Id == AbilityCatalog.ShieldBash)
        {
            sentences.Add(
                $"Stuns that part for {ForTicks(SimConst.StunBaseTicks, actor)} when the hit lands. A repeat within {SimConst.StunDrWindowTicks} ticks halves the duration. After the stun, the part is immune for {SimConst.StunImmuneTicks} ticks.");
        }

        if (ability.Id == AbilityCatalog.RavelExecution)
        {
            sentences.Add(ExecutionFrameSentence(boss.Parts[partIndex]));
        }

        if (ability.AppliesBurn)
        {
            int pulses = PulseCount(SimConst.BurnDuration, SimConst.GlobalPhase);
            sentences.Add(
                $"Burns the struck hero for {Percent(SimConst.HeroBurnRatioBp)}% of the hit every {SimConst.GlobalPhase} ticks for {SimConst.BurnDuration} ticks ({pulses} pulses).");
        }

        if (ability.ChantTicks > 0)
        {
            sentences.Add(ChantSentence(ability, actor));
        }

        bool beyondDamage = ability.Kind == AbilityKind.Healing
            || ability.Effect != SupportEffect.None
            || ability.FlatInterruptBp > 0
            || ability.Id == AbilityCatalog.ShieldBash
            || ability.Id == AbilityCatalog.RavelExecution
            || ability.AppliesBurn
            || ability.Property != ChainProperty.None
            || HasExtraHit(ability, actor);
        if (ability.DealsDamage && !beyondDamage)
        {
            sentences.Add("No other effect.");
        }

        sentences.Add(ThreatSentence(ability, actor, ally, boss, partIndex, party));
        sentences.Add(ability.Property == ChainProperty.None
            ? "Applies no chain property."
            : $"Applies {ability.Property}.");

        var text = new StringBuilder("Effect: ");
        for (int i = 0; i < sentences.Count; i++)
        {
            if (i > 0)
            {
                text.Append(' ');
            }

            text.Append(sentences[i]);
        }

        return text.ToString();
    }

    private static string HealSentence(AbilityDef ability, HeroState actor, HeroState ally)
    {
        GearMods mid = Loadout(actor, LoadoutKind.MidCast);
        int intel = actor.Intel + mid.MidInt;
        int raw = Formulas.HealAmount(intel, ability.MultiplierBp, mid.MidHealingPotencyBp);
        return $"Restores {raw} HP to {ally.Name}, capped by missing HP.";
    }

    private static IEnumerable<string> SupportSentences(AbilityDef ability, HeroState actor, HeroState ally)
    {
        switch (ability.Effect)
        {
            case SupportEffect.KeratinBastion:
                yield return $"−35% physical damage taken on self for {ForTicks(3_000, actor)}.";
                yield break;
            case SupportEffect.TimelineStalk:
                yield return $"Next recovery −4,000 AP for {ForTicks(6_000, actor)} or until used.";
                yield break;
            case SupportEffect.CircuitBenediction:
                int intel = actor.Intel + Loadout(actor, LoadoutKind.MidCast).MidInt;
                int per = FixedMath.MulBp(intel, ability.MultiplierBp);
                int duration = 4_000;
                int pulses = PulseCount(duration, SimConst.GlobalPhase);
                yield return $"Restores {per} HP every {SimConst.GlobalPhase} ticks for {duration} ticks (total {per * pulses}) to {ally.Name}.";
                yield return $"Concentration +20 for {ForTicks(duration, actor)}.";
                yield break;
            case SupportEffect.PhaseSanctuary:
                yield return $"−25% damage taken for the whole party for {ForTicks(3_000, actor)}.";
                yield break;
            case SupportEffect.ChoirAegis:
                int shieldPower = actor.Intel + Loadout(actor, LoadoutKind.MidCast).MidInt;
                int absorb = FixedMath.MulBp(shieldPower, ability.MultiplierBp);
                yield return $"Grants {absorb} absorb to {ally.Name} for {ForTicks(SimConst.ChoirAegisTicks, actor)}. A later cast replaces this pool only when it is larger.";
                yield break;
            default:
                yield return "No damage.";
                yield break;
        }
    }

    private static IEnumerable<string> DamageSentences(
        AbilityDef ability,
        HeroState actor,
        BossState boss,
        int partIndex,
        int tick)
    {
        HitSpan span = ability.Kind == AbilityKind.Magical
            ? SpellSpan(ability, actor, boss, partIndex, tick)
            : WeaponSpan(ability, actor, boss, partIndex, tick);
        string partName = boss.Parts[partIndex].Name;
        string mitigation = ability.Kind == AbilityKind.Magical ? "MEVA" : "DEF";
        string resist = ResistClause(ability, actor, boss, partIndex, span.BurstIgnoresPositiveResist);
        string where = resist.Length == 0 ? $"after {mitigation}" : $"after {mitigation} and {resist}";
        string lead = span.Burst ? "Inside the open burst, about" : "About";
        yield return $"{lead} {span.Low}–{span.High} damage to {partName} {where}.";
        yield return span.Certain ? "Hit is certain inside the open burst." : $"Hit {Percent(span.HitBp)}%.";
        if (ability.UsesWeaponLoadout)
        {
            string extra = ExtraHitSentence(actor, span.PreCrit);
            if (extra.Length > 0)
            {
                yield return extra;
            }
        }

        if (span.MpRefund > 0)
        {
            yield return $"Restores {span.MpRefund} MP from the open burst.";
        }
    }

    private static HitSpan WeaponSpan(AbilityDef ability, HeroState actor, BossState boss, int partIndex, int tick)
    {
        BossPartState part = boss.Parts[partIndex];
        WeaponProperty property = ability.Kind == AbilityKind.ElementalPhysical
            ? WeaponProperty.ElementalPhysical
            : ability.Property switch
            {
                ChainProperty.Piercing => WeaponProperty.Piercing,
                ChainProperty.Slashing => WeaponProperty.Slashing,
                _ => WeaponProperty.Blunt,
            };
        GearMods weapon = Loadout(actor, LoadoutKind.Weapon);
        int attack = Formulas.EffectiveAttack(actor.Atk, 0, weapon.WsStr, weapon.WsDex, property);
        int accuracy = actor.Acc + weapon.WsDex;
        bool burst = part.BurstActive
            && tick < part.BurstExpires
            && ability.UsesWeaponLoadout
            && (part.BurstMask & ElementMask.Physical) != 0;
        int resistance = 0;
        if (ability.Element != ElementId.None)
        {
            resistance = Formulas.EffectiveResistanceBp(PartResistance(boss, partIndex, ability.Element), HeroEpen(actor));
        }

        int bucket = 0;
        int trueBonus = 0;
        if (burst)
        {
            if (part.BurstTrueBp == 0)
            {
                bucket = part.BurstBucketBp;
            }

            trueBonus = part.BurstTrueBp;
            if (resistance > 0)
            {
                resistance = 0;
            }
        }

        int crit = Formulas.WeaponCritMultiplierBp(
            weapon.WsCritDamageBp,
            ability.Id == AbilityCatalog.RavelExecution && Formulas.BelowExecutionThreshold(part.Hp, part.MaxHp));
        DamageRequest low = HitRequest(attack, ability.MultiplierBp, PartDefense(boss, partIndex, tick), SimConst.PhysicalDrConstant, resistance, burst, critMultiplier: SimConst.Bp, weapon.WsDamageBp, bucket, dealtBuff: 0, diminish: SimConst.Bp, trueBonus);
        DamageRequest high = HitRequest(attack, ability.MultiplierBp, PartDefense(boss, partIndex, tick), SimConst.PhysicalDrConstant, resistance, burst, crit, weapon.WsDamageBp, bucket, dealtBuff: 0, diminish: SimConst.Bp, trueBonus);
        return new HitSpan(
            DamagePipeline.Resolve(low).Total,
            DamagePipeline.Resolve(high).Total,
            burst ? SimConst.Bp : Formulas.HitChanceBp(accuracy, boss.Eva),
            burst,
            burst,
            burst && resistance == 0,
            DamagePipeline.PreCritDealt(low),
            mpRefund: 0);
    }

    private static HitSpan SpellSpan(AbilityDef ability, HeroState actor, BossState boss, int partIndex, int tick)
    {
        BossPartState part = boss.Parts[partIndex];
        GearMods mid = Loadout(actor, LoadoutKind.MidCast);
        int intel = actor.Intel + mid.MidInt;
        bool burst = part.BurstActive
            && tick < part.BurstExpires
            && ability.Kind == AbilityKind.Magical
            && (part.BurstMask & ElementMaps.Mask(ability.Element)) != 0;
        int elapsed = part.BurstActive ? tick - part.BurstOpened : int.MaxValue;
        bool early = burst && actor.EarlyWindowCartography && elapsed <= SimConst.EarlyWindowTicks;
        int dealtBuff = early ? 2_500 : 0;
        int burstIndex = burst ? part.BurstsLanded + 1 : 1;
        int mbd = mid.MidMbdBp > SimConst.GearMbdCapBp ? SimConst.GearMbdCapBp : mid.MidMbdBp;
        int bucket = burst && part.BurstTrueBp == 0 ? part.BurstBucketBp + mbd : (burst ? mbd : 0);
        int trueBonus = burst ? part.BurstTrueBp : 0;
        int resistance = Formulas.EffectiveResistanceBp(PartResistance(boss, partIndex, ability.Element), HeroEpen(actor));
        int diminish = burst ? Formulas.BurstDiminishBp(burstIndex) : SimConst.Bp;
        int crit = Formulas.CritMultiplierBp(mid.MidCritDamageBp);
        int meva = BossMagicEvasion(boss, tick);
        DamageRequest low = HitRequest(intel, ability.MultiplierBp, meva, SimConst.MagicalDrConstant, resistance, burst, SimConst.Bp, weaponSkill: 0, bucket, dealtBuff, diminish, trueBonus);
        DamageRequest high = HitRequest(intel, ability.MultiplierBp, meva, SimConst.MagicalDrConstant, resistance, burst, crit, weaponSkill: 0, bucket, dealtBuff, diminish, trueBonus);
        int refund = 0;
        if (burst)
        {
            int refundBp = Formulas.BurstRefundBp(actor.Race == RaceId.KithLir, 0);
            refund = Formulas.MpRefund(ability.MpCost, refundBp);
        }

        int macc = Formulas.MagicAccuracy(intel, actor.Acc, 0);
        return new HitSpan(
            DamagePipeline.Resolve(low).Total,
            DamagePipeline.Resolve(high).Total,
            burst ? SimConst.Bp : Formulas.HitChanceBp(macc, meva),
            burst,
            burst,
            burst,
            DamagePipeline.PreCritDealt(low),
            refund);
    }

    private static DamageRequest HitRequest(
        int power,
        int multiplier,
        int mitigation,
        int constant,
        int resistance,
        bool bypassPositiveResistance,
        int critMultiplier,
        int weaponSkill,
        int bucket,
        int dealtBuff,
        int diminish,
        int trueBonus)
    {
        return new DamageRequest
        {
            Power = power,
            MultiplierBp = multiplier,
            MitigationStat = mitigation,
            MitigationConstant = constant,
            EffectiveResistanceBp = resistance,
            BypassPositiveResistance = bypassPositiveResistance,
            CritMultiplierBp = critMultiplier,
            WeaponSkillBp = weaponSkill,
            BurstBucketBp = bucket,
            DamageDealtBuffBp = dealtBuff,
            BurstDiminishBp = diminish,
            TrueBonusBp = trueBonus,
        };
    }

    private static string ExtraHitSentence(HeroState actor, int preCrit)
    {
        GearMods weapon = Loadout(actor, LoadoutKind.Weapon);
        int triple = weapon.WsTripleAttackBp > 2_500 ? 2_500 : weapon.WsTripleAttackBp;
        int doubles = weapon.WsDoubleAttackBp > 5_000 ? 5_000 : weapon.WsDoubleAttackBp;
        if (triple <= 0 && doubles <= 0)
        {
            return "";
        }

        int half = preCrit * SimConst.ExtraHitBp / SimConst.Bp;
        if (doubles > 0 && triple > 0)
        {
            return $"Weapon gear may add one extra hit of {half} (double attack {Percent(doubles)}%) or two (triple attack {Percent(triple)}%).";
        }

        if (triple > 0)
        {
            return $"Weapon gear may add two extra hits of {half} each (triple attack {Percent(triple)}%).";
        }

        return $"Weapon gear may add one extra hit of {half} (double attack {Percent(doubles)}%).";
    }

    private static bool HasExtraHit(AbilityDef ability, HeroState actor)
    {
        if (!ability.UsesWeaponLoadout)
        {
            return false;
        }

        GearMods weapon = Loadout(actor, LoadoutKind.Weapon);
        return weapon.WsDoubleAttackBp > 0 || weapon.WsTripleAttackBp > 0;
    }

    private static string ThreatSentence(
        AbilityDef ability,
        HeroState actor,
        HeroState ally,
        BossState boss,
        int partIndex,
        IReadOnlyList<HeroState> party)
    {
        _ = ally;
        int threatPart = ability.Kind == AbilityKind.Healing ? 0 : partIndex;
        string where = boss.Parts.Length == 0 ? "target" : boss.Parts[ClampPart(boss, threatPart)].Name;
        bool idle = UseIdleEnmity(ability, actor);
        int enmityPlus = idle ? Loadout(actor, LoadoutKind.Idle).IdleEnmityBp : 0;
        bool aethel = actor.Race == RaceId.AethelBorn;
        bool shelter = actor.Slot == 0 && ShelterActive(party, boss);
        int flat = Formulas.AbilityCe(ability.BaseCe, enmityPlus, shelter, aethel);
        if (ability.DealsDamage)
        {
            int per = Formulas.DamageCe(10_000, aethel);
            return $"Threat on {where}: VE {ability.BaseVe}, CE {flat}, plus {per} CE per 10,000 damage.";
        }

        if (ability.Kind == AbilityKind.Healing)
        {
            GearMods mid = Loadout(actor, LoadoutKind.MidCast);
            int raw = Formulas.HealAmount(actor.Intel + mid.MidInt, ability.MultiplierBp, mid.MidHealingPotencyBp);
            int healVe = Formulas.HealVe(raw);
            bool heavy = aethel && Formulas.IsHeavyVeSource(ability.BaseVe, ability.RecoveryAp, raw);
            string heavyNote = heavy ? ", heavy VE" : "";
            return $"Threat on {where}: VE {ability.BaseVe + healVe} if the full heal lands (otherwise {Percent(SimConst.HealVeBp)}% of HP restored), CE {flat}{heavyNote}.";
        }

        bool heavyAbility = aethel && Formulas.IsHeavyVeSource(ability.BaseVe, ability.RecoveryAp, 0);
        string heavyAbilityNote = heavyAbility ? ", heavy VE" : "";
        return $"Threat on {where}: VE {ability.BaseVe}, CE {flat}{heavyAbilityNote}.";
    }

    private static string ChantSentence(AbilityDef ability, HeroState actor)
    {
        int race = actor.Race == RaceId.AethelBorn ? SimConst.AethelFastCastBp : 0;
        int fast = Formulas.TotalFastCastBp(Loadout(actor, LoadoutKind.FastCast).FastCastBp, race, 0);
        int ticks = Formulas.EffectiveChantTicks(ability.ChantTicks, fast);
        return ticks == ability.ChantTicks
            ? $"Chant {ticks} ticks."
            : $"Chant resolves in {ticks} ticks after fast cast.";
    }

    private static string ResistClause(AbilityDef ability, HeroState actor, BossState boss, int partIndex, bool burstIgnoresPositive)
    {
        if (ability.Element == ElementId.None)
        {
            return "";
        }

        int raw = PartResistance(boss, partIndex, ability.Element);
        int effective = Formulas.EffectiveResistanceBp(raw, HeroEpen(actor));
        if (burstIgnoresPositive && effective > 0)
        {
            return $"{ability.Element} resist ignored by the burst";
        }

        if (effective == 0 && raw == 0)
        {
            return "";
        }

        if (effective == 0)
        {
            return $"{ability.Element} resist 0% after penetration";
        }

        return $"{ability.Element} resist {SignedPercent(effective)}%";
    }

    private static bool UseIdleEnmity(AbilityDef ability, HeroState actor)
    {
        if (ability.UsesWeaponLoadout || ability.Kind == AbilityKind.Magical)
        {
            return false;
        }

        if (ability.Kind == AbilityKind.Healing)
        {
            return !actor.Casting;
        }

        return true;
    }

    private static bool ShelterActive(IReadOnlyList<HeroState> party, BossState boss)
    {
        if (party.Count == 0 || boss.Parts.Length == 0)
        {
            return false;
        }

        int casting = 0;
        for (int i = 1; i < party.Count; i++)
        {
            if (party[i].Casting)
            {
                casting++;
            }
        }

        if (casting < SimConst.ShelterCastingAllies)
        {
            return false;
        }

        EnmitySlot[] table = boss.Parts[0].Enmity;
        if (table.Length == 0)
        {
            return false;
        }

        var alive = new bool[table.Length];
        int count = party.Count < table.Length ? party.Count : table.Length;
        for (int i = 0; i < count; i++)
        {
            alive[i] = party[i].IsAlive;
        }

        return EnmityMath.ArgMax(table, alive) == 0;
    }

    private static int PartDefense(BossState boss, int partIndex, int tick)
    {
        int def = boss.Def;
        if ((uint)partIndex < (uint)boss.Parts.Length)
        {
            def += boss.Parts[partIndex].DefBonus;
        }

        if (boss.Beneficial.Contains(SimConst.FrenzyPlatingName))
        {
            def = (int)((long)def * (SimConst.Bp + SimConst.FrenzyPlatingBp) / SimConst.Bp);
        }

        if (boss.ShatterBp > 0 && tick < boss.ShatterExpires)
        {
            def = (int)((long)def * (SimConst.Bp - boss.ShatterBp) / SimConst.Bp);
        }

        return def < 0 ? 0 : def;
    }

    private static int BossMagicEvasion(BossState boss, int tick)
    {
        int meva = boss.Meva;
        if (boss.Beneficial.Contains(SimConst.FrenzyPlatingName))
        {
            meva = (int)((long)meva * (SimConst.Bp + SimConst.FrenzyPlatingBp) / SimConst.Bp);
        }

        if (boss.MevaDownBp <= 0 || tick >= boss.MevaDownExpires)
        {
            return meva;
        }

        return (int)((long)meva * (SimConst.Bp - boss.MevaDownBp) / SimConst.Bp);
    }

    private static int PartResistance(BossState boss, int partIndex, ElementId element)
    {
        if (element < 0)
        {
            return 0;
        }

        int index = (int)element;
        if ((uint)partIndex < (uint)boss.Parts.Length)
        {
            int[]? local = boss.Parts[partIndex].ResistBp;
            if (local != null && index < local.Length)
            {
                return local[index];
            }
        }

        if (index >= boss.ResistBp.Length)
        {
            return 0;
        }

        return boss.ResistBp[index];
    }

    private static int HeroEpen(HeroState hero)
    {
        int epen = hero.Epen;
        if (hero.Race == RaceId.AshDravan)
        {
            epen += SimConst.AshDravanEpen;
        }

        return epen;
    }

    private static GearMods Loadout(HeroState hero, LoadoutKind kind)
    {
        if (!hero.SplitLoadouts)
        {
            return hero.Gear;
        }

        return kind switch
        {
            LoadoutKind.FastCast => hero.FastCast,
            LoadoutKind.MidCast => hero.MidCast,
            LoadoutKind.Weapon => hero.Weapon,
            _ => hero.Idle,
        };
    }

    private static int ClampPart(BossState boss, int partIndex)
    {
        if (boss.Parts.Length == 0 || (uint)partIndex >= (uint)boss.Parts.Length)
        {
            return 0;
        }

        return partIndex;
    }

    private static int PulseCount(int durationTicks, int phase)
    {
        if (durationTicks <= 0 || phase <= 0)
        {
            return 0;
        }

        int count = 0;
        int next = phase;
        while (next <= durationTicks)
        {
            count++;
            next += phase;
        }

        return count;
    }

    private static string ExecutionFrameSentence(BossPartState part)
    {
        int percent = part.MaxHp <= 0 ? 0 : (int)((long)part.Hp * 100 / part.MaxHp);
        int capWhole = SimConst.ExecutionFrameCapBp / SimConst.Bp;
        int capFrac = (SimConst.ExecutionFrameCapBp % SimConst.Bp) / 100;
        string cap = $"{capWhole}.{capFrac:00}×";
        bool active = Formulas.BelowExecutionThreshold(part.Hp, part.MaxHp);
        string now = active
            ? $"{part.Name} is at {percent}% HP, so the high end above includes the frame."
            : $"{part.Name} is at {percent}% HP, so this hit uses the normal crit cap.";
        return $"Execution Frame: below {SimConst.ExecutionFrameHpPercent}% HP, crit damage +{SimConst.ExecutionFrameCritDamageBp / 100}% and the crit cap is {cap}. {now}";
    }

    private static string ForTicks(int ticks, HeroState actor)
    {
        string turns = AboutTurns(ticks, actor.Agi);
        return turns.Length == 0 ? $"{ticks} ticks" : $"{ticks} ticks ({turns})";
    }

    private static string AboutTurns(int ticks, int agi)
    {
        int perTick = Formulas.AgiCentiPerTick(agi, 0, 0);
        if (perTick <= 0)
        {
            return "";
        }

        int ticksPerTurn = SimConst.ReadyCenti / perTick;
        if (ticksPerTurn <= 0)
        {
            return "";
        }

        int tenths = (ticks * 10 + (ticksPerTurn / 2)) / ticksPerTurn;
        int whole = tenths / 10;
        int frac = tenths % 10;
        return frac == 0 ? $"about {whole} turns" : $"about {whole}.{frac} turns";
    }

    private static string Percent(int bp)
    {
        if (bp < 0)
        {
            bp = -bp;
        }

        int whole = bp / 100;
        int frac = bp % 100;
        if (frac == 0)
        {
            return whole.ToString();
        }

        if (frac % 10 == 0)
        {
            return $"{whole}.{frac / 10}";
        }

        return $"{whole}.{frac:00}";
    }

    private static string SignedPercent(int bp)
    {
        string body = Percent(bp);
        if (bp > 0)
        {
            return "+" + body;
        }

        if (bp < 0)
        {
            return "-" + body;
        }

        return "0";
    }

    private readonly struct HitSpan
    {
        public HitSpan(int low, int high, int hitBp, bool certain, bool burst, bool burstIgnoresPositiveResist, int preCrit, int mpRefund)
        {
            Low = low;
            High = high;
            HitBp = hitBp;
            Certain = certain;
            Burst = burst;
            BurstIgnoresPositiveResist = burstIgnoresPositiveResist;
            PreCrit = preCrit;
            MpRefund = mpRefund;
        }

        public int Low { get; }
        public int High { get; }
        public int HitBp { get; }
        public bool Certain { get; }
        public bool Burst { get; }
        public bool BurstIgnoresPositiveResist { get; }
        public int PreCrit { get; }
        public int MpRefund { get; }
    }
}
