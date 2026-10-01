using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;

namespace Resonance.Sim.Sim;

/// <summary>
/// Event-skipping CTB driver (02 §2.2.3, §2.16.2). Systems read and write the state in this class;
/// roll streams come from <see cref="IRng"/>.
/// </summary>
public sealed class BattleSimulator
{
    public const int Core = 0;
    public const int WeaponArm = 1;

    private readonly HeroState[] _heroes;
    private readonly BossState _boss;
    private readonly Queue<BattleAction>[] _heroPlans;
    private readonly Queue<BattleAction> _bossPlan;
    private readonly IRng _rng;
    private readonly List<CombatEvent> _events = new();
    private readonly List<int> _turnOrder = new();
    private int _tick;
    private bool _shelterWasActive;
    private HeroState? _pendingCadence;

    public BattleSimulator(HeroState[] heroes, BossState boss, Queue<BattleAction>[] heroPlans, Queue<BattleAction> bossPlan, IRng rng)
    {
        _heroes = heroes;
        _boss = boss;
        _heroPlans = heroPlans;
        _bossPlan = bossPlan;
        _rng = rng;
    }

    public int Tick => _tick;

    public IReadOnlyList<HeroState> Heroes => _heroes;

    public BossState Boss => _boss;

    public IReadOnlyList<CombatEvent> Events => _events;

    public bool TryStep()
    {
        int next = ScheduleNext();
        if (next < 0)
        {
            return false;
        }

        int delta = next - _tick;
        if (delta > 0)
        {
            GainAp(delta);
        }

        _tick = next;
        OnTick();
        return true;
    }

    public void RunUntil(int tick)
    {
        int guard = 0;
        while (_tick < tick && TryStep())
        {
            if (++guard > 100_000)
            {
                throw new InvalidOperationException("Timeline did not reach the requested tick.");
            }
        }
    }

    public void RunToEnd()
    {
        int guard = 0;
        while (TryStep())
        {
            if (++guard > 100_000)
            {
                throw new InvalidOperationException("Timeline did not terminate.");
            }
        }
    }

    private int ScheduleNext()
    {
        if (!HasWork())
        {
            return -1;
        }

        int next = int.MaxValue;
        void Consider(int at)
        {
            if (at > _tick && at < next)
            {
                next = at;
            }
        }

        int phase = _tick % SimConst.GlobalPhase;
        Consider(phase == 0 ? _tick + SimConst.GlobalPhase : _tick + (SimConst.GlobalPhase - phase));

        for (int i = 0; i < _heroes.Length; i++)
        {
            HeroState hero = _heroes[i];
            if (hero.Casting)
            {
                Consider(hero.CastResolveTick);
            }
            else if (_heroPlans[i].Count > 0 && hero.IsAlive)
            {
                int need = ApGauge.TicksUntilReady(hero.Ap.Centi, HeroGain(hero));
                if (need <= 0)
                {
                    throw new InvalidOperationException($"{hero.Name} is still ready after tick {_tick}.");
                }

                Consider(_tick + need);
            }

            if (hero.RegenPerPulse > 0)
            {
                Consider(hero.RegenNextTick);
            }

            Consider(hero.PhysDtExpires);
            Consider(hero.AllDtExpires);
            Consider(hero.ConcentrationBuffExpires);
            Consider(hero.RecoveryCutExpires);
        }

        if (_boss.Casting)
        {
            Consider(_boss.CastResolveTick);
        }
        else if (_bossPlan.Count > 0)
        {
            int need = ApGauge.TicksUntilReady(_boss.Ap.Centi, BossGain());
            if (need <= 0)
            {
                throw new InvalidOperationException($"Boss is still ready after tick {_tick}.");
            }

            Consider(_tick + need);
        }

        Consider(_boss.SlowExpires);
        for (int p = 0; p < _boss.Parts.Length; p++)
        {
            BossPartState part = _boss.Parts[p];
            if (part.Tier != 0)
            {
                Consider(part.ChainExpires);
            }

            if (part.BurstActive)
            {
                Consider(part.BurstExpires);
            }
        }

        return next == int.MaxValue ? -1 : next;
    }

    private bool HasWork()
    {
        if (_boss.Casting || _bossPlan.Count > 0)
        {
            return true;
        }

        for (int i = 0; i < _heroes.Length; i++)
        {
            if (_heroes[i].Casting || _heroPlans[i].Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private void GainAp(int ticks)
    {
        for (int i = 0; i < _heroes.Length; i++)
        {
            HeroState hero = _heroes[i];
            if (!hero.Casting && hero.IsAlive)
            {
                hero.Ap.Gain(HeroGain(hero), ticks);
            }
        }

        if (!_boss.Casting)
        {
            _boss.Ap.Gain(BossGain(), ticks);
        }
    }

    private int HeroGain(HeroState hero) => Formulas.AgiCentiPerTick(hero.Agi, 0, 0);

    private int BossGain() => Formulas.AgiCentiPerTick(_boss.Agi, 0, _boss.SlowNow(_tick));

    private void OnTick()
    {
        if (_tick % SimConst.GlobalPhase == 0)
        {
            DecayEnmity();
        }

        PulseRegen();
        ExpireStatuses();
        ResolveChants();
        ResolveReadyActions();
        NoteShelter();
    }

    private void DecayEnmity()
    {
        for (int p = 0; p < _boss.Parts.Length; p++)
        {
            EnmitySlot[] table = _boss.Parts[p].Enmity;
            for (int i = 0; i < table.Length; i++)
            {
                int before = table[i].Ve;
                EnmityMath.Decay(ref table[i]);
                if (before != table[i].Ve)
                {
                    Log($"{_heroes[i].Name} {_boss.Parts[p].Name} VE {before} → {table[i].Ve} (normal {table[i].NormalVe}, heavy {table[i].HeavyVe}).");
                }
            }
        }
    }

    private void PulseRegen()
    {
        for (int i = 0; i < _heroes.Length; i++)
        {
            HeroState hero = _heroes[i];
            if (hero.RegenPerPulse <= 0 || hero.RegenNextTick != _tick)
            {
                continue;
            }

            int missing = hero.MaxHp - hero.Hp;
            int restored = missing < hero.RegenPerPulse ? missing : hero.RegenPerPulse;
            hero.Hp += restored;
            Log($"{hero.Name} regen {hero.RegenPerPulse} (restored {restored}).");
            int next = _tick + SimConst.GlobalPhase;
            if (next > hero.RegenExpires)
            {
                hero.RegenPerPulse = 0;
                hero.RegenNextTick = 0;
            }
            else
            {
                hero.RegenNextTick = next;
            }
        }
    }

    private void ExpireStatuses()
    {
        if (_boss.SlowBp > 0 && _tick >= _boss.SlowExpires)
        {
            _boss.SlowBp = 0;
            Log("Induration slow expired.");
        }

        for (int i = 0; i < _heroes.Length; i++)
        {
            HeroState hero = _heroes[i];
            if (hero.PhysicalHitsMitigated && _tick >= hero.PhysDtExpires)
            {
                hero.PhysicalHitsMitigated = false;
                hero.PhysDtBp = 0;
                Log($"{hero.Name} Keratin Bastion expired.");
            }

            if (hero.AllDtBp > 0 && _tick >= hero.AllDtExpires)
            {
                hero.AllDtBp = 0;
                Log("Phase Sanctuary expired.");
            }

            if (hero.ConcentrationBuff > 0 && _tick >= hero.ConcentrationBuffExpires)
            {
                hero.ConcentrationBuff = 0;
            }

            if (hero.RecoveryCut > 0 && _tick >= hero.RecoveryCutExpires)
            {
                hero.RecoveryCut = 0;
            }
        }

        for (int p = 0; p < _boss.Parts.Length; p++)
        {
            BossPartState part = _boss.Parts[p];
            if (part.Tier != 0 && _tick >= part.ChainExpires)
            {
                part.Tier = 0;
                part.Property = ChainProperty.None;
                part.Resonance = ResonanceId.None;
                Log($"{part.Name} chain window expired.");
            }

            if (part.BurstActive && _tick >= part.BurstExpires)
            {
                part.BurstActive = false;
                Log($"{part.Name} burst window expired.");
            }
        }
    }

    private void ResolveChants()
    {
        var due = new List<(int Order, int Start, bool Boss, int Index)>();
        for (int i = 0; i < _heroes.Length; i++)
        {
            if (_heroes[i].Casting && _heroes[i].CastResolveTick == _tick)
            {
                due.Add((0, _heroes[i].CastStartTick, false, i));
            }
        }

        if (_boss.Casting && _boss.CastResolveTick == _tick)
        {
            due.Add((1, _boss.CastStartTick, true, 0));
        }

        due.Sort(static (a, b) =>
        {
            int start = a.Start.CompareTo(b.Start);
            if (start != 0)
            {
                return start;
            }

            int side = a.Boss.CompareTo(b.Boss);
            if (side != 0)
            {
                return side;
            }

            return a.Index.CompareTo(b.Index);
        });

        foreach ((int _, int _, bool boss, int index) in due)
        {
            if (boss)
            {
                ResolveBossCast();
            }
            else
            {
                HeroState hero = _heroes[index];
                AbilityDef ability = AbilityCatalog.Get(hero.CastAbilityId);
                int part = hero.CastTargetPart;
                int ally = hero.CastTargetHero;
                hero.Casting = false;
                ResolveHeroAbility(hero, ability, part, ally, fromChant: true);
            }
        }
    }

    private void ResolveReadyActions()
    {
        _turnOrder.Clear();
        for (int i = 0; i < _heroes.Length; i++)
        {
            HeroState hero = _heroes[i];
            if (!hero.Casting && hero.IsAlive && hero.Ap.IsReady && _heroPlans[i].Count > 0)
            {
                _turnOrder.Add(i);
            }
        }

        if (!_boss.Casting && _boss.Ap.IsReady && _bossPlan.Count > 0)
        {
            _turnOrder.Add(-1);
        }

        _turnOrder.Sort(CompareTurn);

        for (int n = 0; n < _turnOrder.Count; n++)
        {
            int index = _turnOrder[n];
            if (index < 0)
            {
                if (_boss.Casting || !_boss.Ap.IsReady || _bossPlan.Count == 0)
                {
                    continue;
                }

                BattleAction action = _bossPlan.Dequeue();
                ResolveBossAction(action);
            }
            else
            {
                HeroState hero = _heroes[index];
                if (hero.Casting || !hero.Ap.IsReady || _heroPlans[index].Count == 0)
                {
                    continue;
                }

                BattleAction action = _heroPlans[index].Dequeue();
                AbilityDef ability = AbilityCatalog.Get(action.AbilityId);
                if (ability.ChantTicks > 0)
                {
                    BeginHeroChant(hero, ability, action.TargetPart, action.TargetHero);
                }
                else
                {
                    ResolveHeroAbility(hero, ability, action.TargetPart, action.TargetHero, fromChant: false);
                }
            }
        }
    }

    private int CompareTurn(int a, int b)
    {
        int apA = a < 0 ? _boss.Ap.Centi : _heroes[a].Ap.Centi;
        int apB = b < 0 ? _boss.Ap.Centi : _heroes[b].Ap.Centi;
        int byAp = apB.CompareTo(apA);
        if (byAp != 0)
        {
            return byAp;
        }

        int agiA = a < 0 ? BossGain() : HeroGain(_heroes[a]);
        int agiB = b < 0 ? BossGain() : HeroGain(_heroes[b]);
        int byAgi = agiB.CompareTo(agiA);
        if (byAgi != 0)
        {
            return byAgi;
        }

        int enemyA = a < 0 ? 1 : 0;
        int enemyB = b < 0 ? 1 : 0;
        int bySide = enemyA.CompareTo(enemyB);
        if (bySide != 0)
        {
            return bySide;
        }

        int slotA = a < 0 ? 99 : _heroes[a].Slot;
        int slotB = b < 0 ? 99 : _heroes[b].Slot;
        return slotA.CompareTo(slotB);
    }

    private void BeginHeroChant(HeroState hero, AbilityDef ability, int part, int ally)
    {
        int fastCast = Formulas.TotalFastCastBp(hero.Gear.FastCastBp, RaceFastCast(hero), 0);
        int chant = Formulas.EffectiveChantTicks(ability.ChantTicks, fastCast);
        hero.Mp -= ability.MpCost;
        if (hero.Mp < 0)
        {
            hero.Mp = 0;
        }

        hero.Casting = true;
        hero.CastAbilityId = ability.Id;
        hero.CastTargetPart = part;
        hero.CastTargetHero = ally;
        hero.CastStartTick = _tick;
        hero.CastResolveTick = _tick + chant;
        Log($"{hero.Name} starts {ability.Name}. CT_eff {chant} (FC {fastCast}bp) → resolves {_tick + chant}. MP {hero.Mp}. AP frozen at {ApGauge.Format(hero.Ap.Centi)}.");
        NoteShelter();
    }

    private void ResolveHeroAbility(HeroState hero, AbilityDef ability, int part, int ally, bool fromChant)
    {
        if (ability.UsesWeaponLoadout)
        {
            ResolveWeaponSkill(hero, ability, part);
        }
        else if (ability.Kind == AbilityKind.Magical)
        {
            ResolveSpell(hero, ability, part);
        }
        else if (ability.Kind == AbilityKind.Healing)
        {
            ResolveHeal(hero, ability, ally);
        }
        else
        {
            ResolveSupport(hero, ability, part, ally);
        }

        int recovery = ability.RecoveryAp;
        if (hero.RecoveryCut > 0 && _tick < hero.RecoveryCutExpires && ability.Effect != SupportEffect.TimelineStalk)
        {
            recovery -= hero.RecoveryCut;
            if (recovery < 0)
            {
                recovery = 0;
            }

            hero.RecoveryCut = 0;
            Log($"{hero.Name} Timeline Stalk consumed. Recovery {recovery}.");
        }

        hero.Ap.PayRecovery(recovery);
        Log($"{hero.Name} pays {recovery} recovery. AP {ApGauge.Format(hero.Ap.Centi)}.");
        if (ReferenceEquals(_pendingCadence, hero))
        {
            int refund = hero.Ap.Refund(SimConst.CadenceRefundAp);
            _pendingCadence = null;
            Log($"{hero.Name} Cadence Surge refunds {refund}. AP {ApGauge.Format(hero.Ap.Centi)}.");
        }
        if (fromChant)
        {
            NoteShelter();
        }
    }

    private void ResolveWeaponSkill(HeroState hero, AbilityDef ability, int partIndex)
    {
        BossPartState part = _boss.Parts[partIndex];
        WeaponProperty property = ability.Kind == AbilityKind.ElementalPhysical
            ? WeaponProperty.ElementalPhysical
            : ability.Property switch
            {
                ChainProperty.Piercing => WeaponProperty.Piercing,
                ChainProperty.Slashing => WeaponProperty.Slashing,
                _ => WeaponProperty.Blunt,
            };

        hero.Mp -= ability.MpCost;
        if (hero.Mp < 0)
        {
            hero.Mp = 0;
        }

        int attack = Formulas.EffectiveAttack(hero.Atk, 0, hero.Gear.WsStr, hero.Gear.WsDex, property);
        int accuracy = hero.Acc + hero.Gear.WsDex;
        bool physicalBurst = PhysicalBurst(part, ability);
        int hitBp = Formulas.HitChanceBp(accuracy, _boss.Eva);
        int hitRoll = _rng.RollD10000(RngStream.Hit, ability.Name + " hit");
        bool hit = hitRoll < hitBp;
        int critBp = Formulas.PhysicalCritChanceBp(accuracy, _boss.Eva, hero.Gear.WsDex, hero.Gear.WsCritBp, physicalBurst);
        bool crit = false;
        int critRoll = -1;
        if (hit)
        {
            critRoll = _rng.RollD10000(RngStream.Crit, ability.Name + " crit");
            crit = critRoll < critBp;
        }

        int resistance = 0;
        if (ability.Element != ElementId.None)
        {
            resistance = Formulas.EffectiveResistanceBp(PartResistance(partIndex, ability.Element), hero.Epen);
        }

        if (physicalBurst && resistance > 0)
        {
            resistance = 0;
        }

        var profile = ActiveProfile(part);
        int bucket = 0;
        if (physicalBurst && profile.True == 0)
        {
            bucket = profile.Bucket;
        }

        var request = new DamageRequest
        {
            Power = attack,
            MultiplierBp = ability.MultiplierBp,
            MitigationStat = _boss.Def,
            MitigationConstant = SimConst.PhysicalDrConstant,
            EffectiveResistanceBp = resistance,
            BypassPositiveResistance = physicalBurst,
            CritMultiplierBp = crit ? Formulas.CritMultiplierBp(hero.Gear.WsCritDamageBp) : SimConst.Bp,
            WeaponSkillBp = hero.Gear.WsDamageBp,
            BurstBucketBp = bucket,
            BurstDiminishBp = SimConst.Bp,
            DamageTakenReductionBp = 0,
            TrueBonusBp = physicalBurst ? profile.True : 0,
        };

        // Pre-crit value is only needed for multi-attack. Recompute without the crit factor by
        // asking the pipeline with a 1.00× multiplier when the hit crits.
        DamageResult result = hit ? DamagePipeline.Resolve(request) : new DamageResult(0, 0);
        int dealt = result.Dealt;
        bool linked = false;
        int detonation = 0;
        string chainNote = "no chain";
        if (hit && ability.Property != ChainProperty.None)
        {
            (linked, detonation, chainNote) = ApplyChain(part, ability.Property, dealt, physicalLink: true);
        }

        if (hit)
        {
            part.Hp -= dealt + detonation + result.TrueDamage;
            if (part.Hp < 0)
            {
                part.Hp = 0;
            }
        }

        TryPounce(hero, ability);
        RollMultiAttack(hero, ability, hit);

        int damageForCe = hit ? dealt + detonation + result.TrueDamage : 0;
        GrantEnmity(partIndex, hero, ability, damageForCe, hpRestored: 0, useIdleEnmity: false);
        if (linked || crit)
        {
            _pendingCadence = hero;
        }

        Log($"{hero.Name} {ability.Name} → {part.Name}. ATK_eff {attack}. Hit {hitBp}bp roll {hitRoll} {(hit ? "hit" : "miss")}. Crit {critBp}bp roll {critRoll}. Dmg {dealt}. {chainNote} Det {detonation}. {part.Name} HP {part.Hp}.");
    }

    private void ResolveSpell(HeroState hero, AbilityDef ability, int partIndex)
    {
        BossPartState part = _boss.Parts[partIndex];
        bool magicBurst = MagicBurst(part, ability);
        int intel = hero.Intel + hero.Gear.MidInt;
        int elapsed = part.BurstActive ? _tick - part.BurstOpened : int.MaxValue;
        bool early = magicBurst && hero.EarlyWindowCartography && elapsed <= SimConst.EarlyWindowTicks;
        int dealtBuff = early ? 2_500 : 0;
        int burstIndex = magicBurst ? part.BurstsLanded + 1 : 1;
        var profile = ActiveProfile(part);
        int mbd = hero.Gear.MidMbdBp;
        if (mbd > SimConst.GearMbdCapBp)
        {
            mbd = SimConst.GearMbdCapBp;
        }

        int bucket = magicBurst && profile.True == 0 ? profile.Bucket + mbd : (magicBurst ? mbd : 0);
        bool resisted = false;
        int hitRoll = -1;
        int hitBp = SimConst.Bp;
        if (!magicBurst)
        {
            int macc = Formulas.MagicAccuracy(intel, hero.Acc, 0);
            hitBp = Formulas.HitChanceBp(macc, _boss.Meva);
            hitRoll = _rng.RollD10000(RngStream.Hit, ability.Name + " magic hit");
            resisted = hitRoll >= hitBp;
        }

        int critBp = Formulas.MagicalCritChanceBp(0, magicBurst);
        int critRoll = _rng.RollD10000(RngStream.Crit, ability.Name + " magic crit");
        bool crit = critRoll < critBp;
        int resistance = Formulas.EffectiveResistanceBp(PartResistance(partIndex, ability.Element), hero.Epen);
        var request = new DamageRequest
        {
            Power = intel,
            MultiplierBp = ability.MultiplierBp,
            MitigationStat = _boss.Meva,
            MitigationConstant = SimConst.MagicalDrConstant,
            EffectiveResistanceBp = resistance,
            BypassPositiveResistance = magicBurst,
            Resisted = resisted,
            CritMultiplierBp = crit ? Formulas.CritMultiplierBp(hero.Gear.MidCritDamageBp) : SimConst.Bp,
            BurstBucketBp = bucket,
            DamageDealtBuffBp = dealtBuff,
            BurstDiminishBp = magicBurst ? Formulas.BurstDiminishBp(burstIndex) : SimConst.Bp,
            TrueBonusBp = magicBurst ? profile.True : 0,
        };
        DamageResult result = DamagePipeline.Resolve(request);
        int detonation = 0;
        string chainNote = magicBurst ? "Magic Burst does not rewrite the chain" : "no chain";
        if (!magicBurst && ability.Property != ChainProperty.None)
        {
            (_, detonation, chainNote) = ApplyChain(part, ability.Property, result.Dealt, physicalLink: false);
        }

        int inflicted = result.Dealt + detonation + result.TrueDamage;
        part.Hp -= inflicted;
        if (part.Hp < 0)
        {
            part.Hp = 0;
        }

        if (magicBurst)
        {
            part.BurstsLanded = burstIndex;
            int refundBp = Formulas.BurstRefundBp(hero.Race == RaceId.KithLir, 0);
            int refund = Formulas.MpRefund(ability.MpCost, refundBp);
            hero.Mp += refund;
            if (hero.Race == RaceId.KithLir)
            {
                ExtendBurst(part, early);
            }

            Log($"{hero.Name} Magic Burst #{burstIndex} refunds {refund} MP → {hero.Mp}. Window ends {part.BurstExpires}.");
        }

        TryInterruptBoss(partIndex, result.Dealt);
        GrantEnmity(partIndex, hero, ability, result.Dealt + detonation + result.TrueDamage, hpRestored: 0, useIdleEnmity: false);
        Log($"{hero.Name} {ability.Name} → {part.Name}. INT {intel}. Hit {hitBp}bp roll {hitRoll}{(resisted ? " resisted" : " hit")}. Crit {critBp}bp roll {critRoll} {(crit ? "crit" : "no crit")}. Dmg {result.Dealt}. {chainNote}. {part.Name} HP {part.Hp}.");
    }

    private void ResolveHeal(HeroState hero, AbilityDef ability, int allyIndex)
    {
        HeroState target = _heroes[allyIndex];
        int intel = hero.Intel + hero.Gear.MidInt;
        int raw = Formulas.HealAmount(intel, ability.MultiplierBp, hero.Gear.MidHealingPotencyBp);
        int missing = target.MaxHp - target.Hp;
        int restored = raw < missing ? raw : missing;
        target.Hp += restored;
        GrantEnmity(Core, hero, ability, damageForCe: 0, restored, useIdleEnmity: !hero.Casting);
        Log($"{hero.Name} {ability.Name} → {target.Name}. Heal {raw}, restored {restored}. {target.Name} HP {target.Hp}/{target.MaxHp}.");
    }

    private void ResolveSupport(HeroState hero, AbilityDef ability, int part, int ally)
    {
        switch (ability.Effect)
        {
            case SupportEffect.KeratinBastion:
                hero.PhysDtBp = 3_500;
                hero.PhysicalHitsMitigated = true;
                hero.PhysDtExpires = _tick + 3_000;
                hero.Mp -= ability.MpCost;
                Log($"{hero.Name} Keratin Bastion. −35% physical taken until {hero.PhysDtExpires}. MP {hero.Mp}.");
                break;
            case SupportEffect.TimelineStalk:
                hero.RecoveryCut = 4_000;
                hero.RecoveryCutExpires = _tick + 6_000;
                hero.Mp -= ability.MpCost;
                Log($"{hero.Name} Timeline Stalk. Next recovery −4,000 until used or {hero.RecoveryCutExpires}. MP {hero.Mp}.");
                break;
            case SupportEffect.CircuitBenediction:
                HeroState blessed = _heroes[ally];
                int intel = hero.Intel + hero.Gear.MidInt;
                blessed.RegenPerPulse = FixedMath.MulBp(intel, ability.MultiplierBp);
                blessed.RegenExpires = _tick + 4_000;
                blessed.RegenNextTick = _tick + SimConst.GlobalPhase;
                blessed.ConcentrationBuff = 20;
                blessed.ConcentrationBuffExpires = _tick + 4_000;
                Log($"{hero.Name} Circuit Benediction → {blessed.Name}. Regen {blessed.RegenPerPulse}/500 until {blessed.RegenExpires}. Concentration +20.");
                break;
            case SupportEffect.PhaseSanctuary:
                for (int i = 0; i < _heroes.Length; i++)
                {
                    _heroes[i].AllDtBp = 2_500;
                    _heroes[i].AllDtExpires = _tick + 3_000;
                }

                Log($"{hero.Name} Phase Sanctuary. Party −25% damage taken until {_tick + 3_000}.");
                break;
            default:
                Log($"{hero.Name} {ability.Name}.");
                break;
        }

        int enmityPart = part >= 0 ? part : Core;
        GrantEnmity(enmityPart, hero, ability, damageForCe: 0, hpRestored: 0, useIdleEnmity: !ability.UsesWeaponLoadout);
    }

    private void ResolveBossAction(BattleAction action)
    {
        AbilityDef ability = AbilityCatalog.Get(action.AbilityId);
        int partIndex = action.TargetPart;
        int target = SelectBossTarget(partIndex);
        if (ability.ChantTicks > 0)
        {
            _boss.Casting = true;
            _boss.CastAbilityId = ability.Id;
            _boss.CastPart = partIndex;
            _boss.CastTargetHero = target;
            _boss.CastStartTick = _tick;
            _boss.CastResolveTick = _tick + ability.ChantTicks;
            Log($"Boss {ability.Name} ({_boss.Parts[partIndex].Name}) starts on {_heroes[target].Name}. Resolves {_boss.CastResolveTick}. AP frozen at {ApGauge.Format(_boss.Ap.Centi)}.");
            return;
        }

        ResolveBossStrike(ability, partIndex, target);
    }

    private void ResolveBossCast()
    {
        AbilityDef ability = AbilityCatalog.Get(_boss.CastAbilityId);
        int part = _boss.CastPart;
        int target = _boss.CastTargetHero;
        _boss.Casting = false;
        ResolveBossStrike(ability, part, target);
    }

    private void ResolveBossStrike(AbilityDef ability, int partIndex, int targetIndex)
    {
        HeroState target = _heroes[targetIndex];
        bool magical = ability.Kind == AbilityKind.Magical;
        int accuracy = magical
            ? Formulas.MagicAccuracy(_boss.Intel, _boss.Acc, 0)
            : _boss.Acc;
        int evasion = magical ? EffectiveMeva(target) : EffectiveEva(target);
        int hitBp = Formulas.HitChanceBp(accuracy, evasion);
        int hitRoll = _rng.RollD10000(RngStream.Hit, "boss " + ability.Name + " hit");
        bool hit = hitRoll < hitBp;
        int critBp = magical
            ? Formulas.MagicalCritChanceBp(0, bursting: false)
            : Formulas.PhysicalCritChanceBp(accuracy, evasion, 0, 0, bursting: false);
        int critRoll = -1;
        bool crit = false;
        if (hit)
        {
            critRoll = _rng.RollD10000(RngStream.Crit, "boss " + ability.Name + " crit");
            crit = critRoll < critBp;
        }

        int reduction = 0;
        bool blocksShed = false;
        if (!magical && target.PhysicalHitsMitigated && _tick < target.PhysDtExpires)
        {
            reduction += target.PhysDtBp;
            blocksShed = true;
        }

        if (target.AllDtBp > 0 && _tick < target.AllDtExpires)
        {
            reduction += target.AllDtBp;
            blocksShed = true;
        }

        if (reduction > 6_000)
        {
            reduction = 6_000;
        }

        int resistance = 0;
        if (ability.Element != ElementId.None)
        {
            resistance = Formulas.EffectiveResistanceBp(0, 0);
        }

        var request = new DamageRequest
        {
            Power = magical ? _boss.Intel : _boss.Atk,
            MultiplierBp = ability.MultiplierBp,
            MitigationStat = magical ? EffectiveMeva(target) : EffectiveDef(target),
            MitigationConstant = magical ? SimConst.MagicalDrConstant : SimConst.PhysicalDrConstant,
            EffectiveResistanceBp = resistance,
            Resisted = magical && !hit,
            CritMultiplierBp = crit ? Formulas.CritMultiplierBp(0) : SimConst.Bp,
            DamageTakenReductionBp = reduction,
        };

        // A physical miss deals nothing. A resisted spell still deals 50%.
        DamageResult result = (!magical && !hit) ? new DamageResult(0, 0) : DamagePipeline.Resolve(request);
        int absorbed = 0;
        int taken = DamagePipeline.ApplyAbsorb(result.Total, ref target.Absorb, out absorbed);
        if (absorbed > 0)
        {
            blocksShed = true;
        }

        target.Hp -= taken;
        if (target.Hp < 0)
        {
            target.Hp = 0;
        }

        // The worked log sheds the table that actually selected the target (Core, when the
        // Weapon Arm table is empty). See the pilot report: §2.9.4 names the attacking part.
        if (taken > 0 && !blocksShed)
        {
            Shed(SelectTable(partIndex), targetIndex, taken, target.MaxHp);
        }

        if (!magical && taken > 0 && target.Race == RaceId.VethKari)
        {
            int ve = Formulas.ChitinousVe(taken);
            EnmityMath.AddVolatile(ref _boss.Parts[partIndex].Enmity[targetIndex], ve, heavy: false);
            Log($"{target.Name} Chitinous Grounding +{ve} VE on {_boss.Parts[partIndex].Name}.");
        }

        _boss.Ap.PayRecovery(ability.RecoveryAp);
        _boss.PounceAvailable = true;
        Log($"Boss {ability.Name} → {target.Name}. Hit {hitBp}bp roll {hitRoll} {(hit ? "hit" : "miss")}. Crit {critBp}bp roll {critRoll}. Dmg {taken}. {target.Name} HP {target.Hp}. Boss AP {ApGauge.Format(_boss.Ap.Centi)}.");
    }

    private void Shed(int tableIndex, int heroIndex, int damage, int maxHp)
    {
        ref EnmitySlot slot = ref _boss.Parts[tableIndex].Enmity[heroIndex];
        int shed = Formulas.CeShed(slot.Ce, damage, maxHp);
        slot.Ce -= shed;
        Log($"{_heroes[heroIndex].Name} sheds {shed} CE on {_boss.Parts[tableIndex].Name}. CE {slot.Ce}.");
    }

    private (bool Linked, int Detonation, string Note) ApplyChain(BossPartState part, ChainProperty incoming, int closingDamage, bool physicalLink)
    {
        if (part.Tier == 0)
        {
            OpenL1(part, incoming);
            return (false, 0, $"chain L1({incoming}) until {part.ChainExpires}");
        }

        if (part.Tier == 1)
        {
            ResonanceId resonance = ResonanceTable.LookupL2(part.Property, incoming);
            if (resonance == ResonanceId.None)
            {
                OpenL1(part, incoming);
                return (false, 0, $"chain restarts L1({incoming}) until {part.ChainExpires}");
            }

            int detonation = closingDamage * 5_000 / SimConst.Bp;
            OpenL2(part, resonance);
            return (physicalLink, detonation, $"{resonance} detonation {detonation}. L2 until {part.ChainExpires}. Burst until {part.BurstExpires}");
        }

        ApexId apex = ResonanceTable.LookupL3(part.Resonance, incoming);
        if (apex == ApexId.None)
        {
            OpenL1(part, incoming);
            return (false, 0, $"chain restarts L1({incoming}) until {part.ChainExpires}");
        }

        ClearChain(part);
        OpenBurst(part, ResonanceTable.Profile(apex));
        return (physicalLink, closingDamage, $"{apex} true detonation {closingDamage}");
    }

    private void OpenL1(BossPartState part, ChainProperty property)
    {
        part.Tier = 1;
        part.Property = property;
        part.Resonance = ResonanceId.None;
        part.ChainExpires = _tick + SimConst.ChainWindowTicks;
    }

    private void OpenL2(BossPartState part, ResonanceId resonance)
    {
        part.Tier = 2;
        part.Property = ChainProperty.None;
        part.Resonance = resonance;
        part.ChainExpires = _tick + SimConst.ChainWindowTicks;
        OpenBurst(part, ResonanceTable.Profile(resonance));
        if (resonance == ResonanceId.Induration)
        {
            // One Induration source refreshes. It does not stack with itself; other slows still cap at −50%.
            _boss.SlowBp = SimConst.IndurationSlowBp;
            _boss.SlowExpires = _tick + SimConst.IndurationSlowTicks;
        }
    }

    private static void ClearChain(BossPartState part)
    {
        part.Tier = 0;
        part.Property = ChainProperty.None;
        part.Resonance = ResonanceId.None;
        part.ChainExpires = 0;
    }

    private void OpenBurst(BossPartState part, BurstProfile profile)
    {
        part.BurstActive = true;
        part.BurstOpened = _tick;
        part.BurstExpires = _tick + SimConst.BurstWindowTicks;
        part.BurstExtension = 0;
        part.BurstsLanded = 0;
        part.BurstMask = profile.Mask;
        part.BurstBucketBp = profile.BucketBonusBp;
        part.BurstTrueBp = profile.TrueBonusBp;
    }

    private void ExtendBurst(BossPartState part, bool early)
    {
        int want = early ? SimConst.EarlyWindowExtension : SimConst.KithExtensionTicks;
        int room = SimConst.BurstExtensionCap - part.BurstExtension;
        if (room < 0)
        {
            room = 0;
        }

        int add = want < room ? want : room;
        part.BurstExtension += add;
        part.BurstExpires += add;
    }

    private void TryPounce(HeroState hero, AbilityDef ability)
    {
        if (hero.Race != RaceId.SylvariMor || ability.Property != ChainProperty.Piercing || !_boss.PounceAvailable)
        {
            return;
        }

        int ap = _boss.Ap.Centi;
        int min = SimConst.PounceWindowMinAp * SimConst.CentiPerAp;
        int ready = SimConst.ReadyCenti;
        if (ap < min || ap >= ready)
        {
            return;
        }

        _boss.Ap.Delay(SimConst.PounceDelayAp);
        _boss.PounceAvailable = false;
        Log($"{hero.Name} Pounce on the Upbeat. Boss AP {ApGauge.Format(_boss.Ap.Centi)}.");
    }

    private void TryInterruptBoss(int partIndex, int damage)
    {
        if (!_boss.Casting || _boss.CastPart != partIndex)
        {
            return;
        }

        int chance = Formulas.InterruptChanceBp(damage, _boss.Parts[partIndex].MaxHp, _boss.Concentration);
        Log($"Interrupt% {chance}bp against {_boss.Parts[partIndex].Name} (damage {damage}, concentration {_boss.Concentration}).");
        if (chance <= 0)
        {
            return;
        }

        int roll = _rng.RollD10000(RngStream.Interrupt, "boss interrupt");
        if (roll < chance)
        {
            _boss.Casting = false;
            _boss.Ap.PayRecovery(SimConst.RecoveryStance);
            Log($"Boss chant interrupted (roll {roll}).");
        }
    }

    private void GrantEnmity(int partIndex, HeroState hero, AbilityDef ability, int damageForCe, int hpRestored, bool useIdleEnmity)
    {
        bool shelter = ShelterActive() && hero.Slot == 0;
        int enmityPlus = useIdleEnmity ? hero.Gear.IdleEnmityBp : 0;
        bool aethel = hero.Race == RaceId.AethelBorn;
        int flat = Formulas.AbilityCe(ability.BaseCe, enmityPlus, shelter, aethel);
        int fromDamage = Formulas.DamageCe(damageForCe, aethel);
        ref EnmitySlot slot = ref _boss.Parts[partIndex].Enmity[hero.Slot];
        EnmityMath.AddCumulative(ref slot, flat + fromDamage);

        int baseVe = ability.BaseVe;
        int healVe = Formulas.HealVe(hpRestored);
        bool heavy = aethel && Formulas.IsHeavyVeSource(ability.BaseVe, ability.RecoveryAp, hpRestored);
        EnmityMath.AddVolatile(ref slot, baseVe + healVe, heavy);
        Log($"{hero.Name} {_boss.Parts[partIndex].Name} enmity +VE {baseVe + healVe}{(heavy ? " heavy" : "")} +CE {flat + fromDamage}. Now VE {slot.Ve} CE {slot.Ce}.");
    }

    private void RollMultiAttack(HeroState hero, AbilityDef ability, bool hit)
    {
        if (!hit || !ability.UsesWeaponLoadout)
        {
            return;
        }

        int triple = hero.Gear.WsTripleAttackBp;
        if (triple > 2_500)
        {
            triple = 2_500;
        }

        int extra = 0;
        if (triple > 0)
        {
            int roll = _rng.RollD10000(RngStream.MultiAttack, ability.Name + " triple");
            if (roll < triple)
            {
                extra = 2;
            }
        }

        int doubles = hero.Gear.WsDoubleAttackBp;
        if (doubles > 5_000)
        {
            doubles = 5_000;
        }

        if (extra == 0 && doubles > 0)
        {
            int roll = _rng.RollD10000(RngStream.MultiAttack, ability.Name + " double");
            Log($"{hero.Name} Double Attack roll {roll} vs {doubles}bp.");
            if (roll < doubles)
            {
                extra = 1;
            }
        }

        if (extra > 0)
        {
            Log($"{hero.Name} multi-attack +{extra}.");
        }
    }

    private bool MagicBurst(BossPartState part, AbilityDef ability)
    {
        return part.BurstActive
            && _tick < part.BurstExpires
            && ability.Kind == AbilityKind.Magical
            && (part.BurstMask & ElementMaps.Mask(ability.Element)) != 0;
    }

    private bool PhysicalBurst(BossPartState part, AbilityDef ability)
    {
        return part.BurstActive
            && _tick < part.BurstExpires
            && ability.UsesWeaponLoadout
            && (part.BurstMask & ElementMask.Physical) != 0;
    }

    private static (int Bucket, int True) ActiveProfile(BossPartState part)
    {
        if (!part.BurstActive)
        {
            return (0, 0);
        }

        return (part.BurstBucketBp, part.BurstTrueBp);
    }

    private int SelectBossTarget(int partIndex)
    {
        int table = SelectTable(partIndex);
        int target = EnmityMath.ArgMax(_boss.Parts[table].Enmity, AliveFlags());
        if (target < 0)
        {
            target = 0;
        }

        Log($"Boss {_boss.Parts[partIndex].Name} targets {_heroes[target].Name} (table {_boss.Parts[table].Name}, total {_boss.Parts[table].Enmity[target].Total}).");
        return target;
    }

    private int SelectTable(int partIndex)
    {
        if (EnmityMath.HasThreat(_boss.Parts[partIndex].Enmity))
        {
            return partIndex;
        }

        return Core;
    }

    private bool[] AliveFlags()
    {
        var alive = new bool[_heroes.Length];
        for (int i = 0; i < _heroes.Length; i++)
        {
            alive[i] = _heroes[i].IsAlive;
        }

        return alive;
    }

    private bool ShelterActive()
    {
        int casting = 0;
        for (int i = 0; i < _heroes.Length; i++)
        {
            if (i != 0 && _heroes[i].Casting)
            {
                casting++;
            }
        }

        if (casting < SimConst.ShelterCastingAllies)
        {
            return false;
        }

        return EnmityMath.ArgMax(_boss.Parts[Core].Enmity, AliveFlags()) == 0;
    }

    private void NoteShelter()
    {
        bool active = ShelterActive();
        if (active == _shelterWasActive)
        {
            return;
        }

        _shelterWasActive = active;
        if (active)
        {
            int def = EffectiveDef(_heroes[0]);
            Log($"Shelter of the Chanters active. Korrith DEF {def}, CE generation +30%.");
        }
        else
        {
            Log("Shelter of the Chanters ends.");
        }
    }

    private int EffectiveDef(HeroState hero)
    {
        int def = hero.Def + (hero.Casting ? 0 : hero.Gear.IdleDef);
        if (hero.Slot == 0 && ShelterActive())
        {
            def = (int)((long)def * (SimConst.Bp + SimConst.ShelterDefBp) / SimConst.Bp);
        }

        return def;
    }

    private int EffectiveEva(HeroState hero) => hero.Eva + (hero.Casting ? 0 : hero.Gear.IdleEva);

    private int EffectiveMeva(HeroState hero) => hero.Meva + (hero.Casting ? 0 : hero.Gear.IdleMeva);

    private static int RaceFastCast(HeroState hero) => hero.Race == RaceId.AethelBorn ? SimConst.AethelFastCastBp : 0;

    private static int PartResistance(int partIndex, ElementId element)
    {
        // Carapace Engine Mk. II: Ice +10%, Fire +30%, shared by every part in the worked log.
        _ = partIndex;
        return element switch
        {
            ElementId.Ice => 1_000,
            ElementId.Fire => 3_000,
            _ => 0,
        };
    }

    private void Log(string text)
    {
        _events.Add(new CombatEvent(_tick, text));
    }
}
