using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Gambits;

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
    private readonly List<RecordedCommand> _commands = new();
    private int _commandCursor;
    private int _tick;
    private int _windowsOpened;
    private bool _shelterWasActive;
    private bool _paused;
    private bool _ticked;
    private HeroState? _pendingCadence;

    public int AetherDensity;

    public int GlobalBurstMask;

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

    public IReadOnlyList<RecordedCommand> Commands => _commands;

    public bool Paused => _paused;

    public void Pause() => _paused = true;

    public void Resume() => _paused = false;

    /// <summary>
    /// Records a manual command and leaves it for <see cref="DrainCommands"/>.
    /// A command issued before any tick is stamped 0. A command issued after tick T is stamped T + 1.
    /// </summary>
    public void QueueManual(int heroSlot, int abilityId, int targetPart, int targetHero)
    {
        int stamp = _ticked ? _tick + 1 : 0;
        _commands.Add(new RecordedCommand(stamp, heroSlot, abilityId, targetPart, targetHero));
    }

    /// <summary>Replays a recorded stream. Does not also enqueue; the next ticks drain it.</summary>
    public void LoadCommandStream(IReadOnlyList<RecordedCommand> commands)
    {
        _commands.Clear();
        for (int i = 0; i < commands.Count; i++)
        {
            _commands.Add(commands[i]);
        }

        _commandCursor = 0;
    }

    public bool TryStep()
    {
        if (_paused)
        {
            return false;
        }

        return StepCore();
    }

    /// <summary>Advances one tick even while paused, then restores the pause flag.</summary>
    public bool StepOne()
    {
        bool paused = _paused;
        _paused = false;
        bool advanced = StepCore();
        _paused = paused;
        return advanced;
    }

    private bool StepCore()
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
            else if (hero.IsAlive && (_heroPlans[i].Count > 0 || hero.Deck != null))
            {
                int need = ApGauge.TicksUntilReady(hero.Ap.Centi, HeroGain(hero));
                if (need <= 0)
                {
                    if (_heroPlans[i].Count > 0 && hero.Deck == null && hero.DeferUntilTick <= _tick)
                    {
                        throw new InvalidOperationException($"{hero.Name} is still ready after tick {_tick}.");
                    }

                    Consider(_tick + 1);
                }
                else
                {
                    Consider(_tick + need);
                }
            }

            if (hero.BurnPulse > 0)
            {
                Consider(hero.BurnNextTick);
            }

            if (hero.RegenPerPulse > 0)
            {
                Consider(hero.RegenNextTick);
            }

            Consider(hero.PhysDtExpires);
            Consider(hero.AllDtExpires);
            Consider(hero.ConcentrationBuffExpires);
            Consider(hero.RecoveryCutExpires);
            if (hero.Heat > 0)
            {
                Consider(hero.HeatDecayTick);
            }
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
        if (_boss.ShatterBp > 0)
        {
            Consider(_boss.ShatterExpires);
        }

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

            if (part.BurnPulse > 0)
            {
                Consider(part.BurnNextTick);
            }
        }

        if (_commandCursor < _commands.Count)
        {
            int at = _commands[_commandCursor].ApplyAtTick;
            Consider(at <= _tick ? _tick + 1 : at);
        }

        return next == int.MaxValue ? -1 : next;
    }

    private bool HasWork()
    {
        if (_boss.Casting || _bossPlan.Count > 0 || _commandCursor < _commands.Count)
        {
            return true;
        }

        for (int i = 0; i < _heroes.Length; i++)
        {
            HeroState hero = _heroes[i];
            if (hero.Casting || _heroPlans[i].Count > 0 || (hero.IsAlive && hero.Deck != null))
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
                if (hero.DeferUntilTick > _tick && hero.Ap.Centi > SimConst.DeferApCapCenti)
                {
                    hero.Ap.Centi = SimConst.DeferApCapCenti;
                }
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
        DrainCommands();
        if (_tick % SimConst.GlobalPhase == 0)
        {
            DecayEnmity();
        }

        PulseRegen();
        PulseBurns();
        PulseHeroBurns();
        DecayHeat();
        ExpireStatuses();
        ResolveChants();
        ResolveReadyActions();
        NoteShelter();
        _ticked = true;
    }

    private void DrainCommands()
    {
        while (_commandCursor < _commands.Count && _commands[_commandCursor].ApplyAtTick <= _tick)
        {
            RecordedCommand command = _commands[_commandCursor];
            _commandCursor++;
            if ((uint)command.HeroSlot >= (uint)_heroPlans.Length)
            {
                continue;
            }

            _heroPlans[command.HeroSlot].Enqueue(new BattleAction(command.AbilityId, command.TargetPart, command.TargetHero));
        }
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

        if (_boss.ShatterBp > 0 && _tick >= _boss.ShatterExpires)
        {
            _boss.ShatterBp = 0;
            Log("DEF Shatter expired.");
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
                part.Contributors = 0;
                Log($"{part.Name} chain window expired.");
            }

            if (part.BurstActive && _tick >= part.BurstExpires)
            {
                part.BurstActive = false;
                Log($"{part.Name} burst window expired.");
            }

            if (part.BurnPulse > 0 && _tick > part.BurnExpires)
            {
                part.BurnPulse = 0;
                part.BurnIii = false;
            }
        }

        for (int i = 0; i < _heroes.Length; i++)
        {
            HeroState burned = _heroes[i];
            if (burned.BurnPulse > 0 && _tick > burned.BurnExpires)
            {
                burned.BurnPulse = 0;
            }
        }
    }

    private void PulseBurns()
    {
        for (int p = 0; p < _boss.Parts.Length; p++)
        {
            BossPartState part = _boss.Parts[p];
            if (part.BurnPulse <= 0 || part.BurnNextTick != _tick)
            {
                continue;
            }

            part.Hp -= part.BurnPulse;
            if (part.Hp < 0)
            {
                part.Hp = 0;
            }

            Log($"{part.Name} {(part.BurnIii ? "Burn III" : "Burn")} {part.BurnPulse}. HP {part.Hp}.");
            int next = _tick + SimConst.GlobalPhase;
            if (next > part.BurnExpires)
            {
                part.BurnPulse = 0;
                part.BurnNextTick = 0;
                part.BurnIii = false;
            }
            else
            {
                part.BurnNextTick = next;
            }
        }
    }

    private void PulseHeroBurns()
    {
        for (int i = 0; i < _heroes.Length; i++)
        {
            HeroState hero = _heroes[i];
            if (hero.BurnPulse <= 0 || hero.BurnNextTick != _tick)
            {
                continue;
            }

            if (hero.Race == RaceId.AshDravan)
            {
                Log($"{hero.Name} is immune to Burn.");
            }
            else
            {
                hero.Hp -= hero.BurnPulse;
                if (hero.Hp < 0)
                {
                    hero.Hp = 0;
                }

                Log($"{hero.Name} Burn {hero.BurnPulse}. HP {hero.Hp}.");
            }

            int next = _tick + SimConst.GlobalPhase;
            if (next > hero.BurnExpires)
            {
                hero.BurnPulse = 0;
                hero.BurnNextTick = 0;
            }
            else
            {
                hero.BurnNextTick = next;
            }
        }
    }

    private void DecayHeat()
    {
        for (int i = 0; i < _heroes.Length; i++)
        {
            HeroState hero = _heroes[i];
            if (hero.Heat <= 0 || hero.HeatDecayTick != _tick)
            {
                continue;
            }

            hero.Heat -= SimConst.HeatDecay;
            if (hero.Heat < 0)
            {
                hero.Heat = 0;
            }

            Log($"{hero.Name} Heat decays to {hero.Heat}.");
            hero.HeatDecayTick = hero.Heat > 0 ? _tick + SimConst.HeatDecayTicks : 0;
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
            if (!hero.Casting && hero.IsAlive && hero.Ap.IsReady && (_heroPlans[i].Count > 0 || hero.Deck != null))
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
                if (hero.Casting || !hero.Ap.IsReady || (_heroPlans[index].Count == 0 && hero.Deck == null))
                {
                    continue;
                }

                if (_heroPlans[index].Count > 0)
                {
                    BattleAction action = _heroPlans[index].Dequeue();
                    Act(hero, AbilityCatalog.Get(action.AbilityId), action.TargetPart, action.TargetHero, spendHeat: false);
                }
                else
                {
                    var view = new GambitView(_heroes, _boss, index, _tick, AetherDensity, GlobalBurstMask);
                    GambitDecision decision = GambitMachine.Evaluate(hero.Deck!, view);
                    if (decision.Outcome == GambitOutcome.Defer)
                    {
                        Defer(hero, decision);
                    }
                    else if (decision.Outcome == GambitOutcome.Wait)
                    {
                        Wait(hero, decision);
                    }
                    else
                    {
                        Act(hero, AbilityCatalog.Get(decision.AbilityId), decision.TargetPart, decision.TargetHero, decision.SpendHeat);
                    }
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

    private void Act(HeroState hero, AbilityDef ability, int part, int ally, bool spendHeat)
    {
        if (spendHeat)
        {
            hero.Heat -= SimConst.SpendHeatCost;
            if (hero.Heat < 0)
            {
                hero.Heat = 0;
            }
        }

        if (ability.ChantTicks > 0)
        {
            BeginHeroChant(hero, ability, part, ally);
        }
        else
        {
            ResolveHeroAbility(hero, ability, part, ally, fromChant: false);
        }
    }

    private void Defer(HeroState hero, GambitDecision decision)
    {
        if (decision.SpendHeat)
        {
            hero.Heat -= SimConst.SpendHeatCost;
            if (hero.Heat < 0)
            {
                hero.Heat = 0;
            }
        }

        if (hero.DeferUntilTick <= _tick)
        {
            hero.DeferStartedTick = _tick;
            hero.DeferUntilTick = _tick + decision.DeferTicks;
        }

        if (hero.Ap.Centi > SimConst.DeferApCapCenti)
        {
            hero.Ap.Centi = SimConst.DeferApCapCenti;
        }

        Log($"{hero.Name} defers until {hero.DeferUntilTick}. AP {ApGauge.Format(hero.Ap.Centi)}.");
    }

    private void Wait(HeroState hero, GambitDecision decision)
    {
        if (decision.SpendHeat)
        {
            hero.Heat -= SimConst.SpendHeatCost;
            if (hero.Heat < 0)
            {
                hero.Heat = 0;
            }
        }

        hero.DeferUntilTick = 0;
        hero.DeferStartedTick = 0;
        hero.WaitDtBp = SimConst.WaitMitigationBp;
        hero.Ap.PayRecovery(SimConst.RecoveryStance);
        Log($"{hero.Name} waits. Damage taken −{SimConst.WaitMitigationBp}bp until the next action. AP {ApGauge.Format(hero.Ap.Centi)}.");
    }

    private void BeginHeroChant(HeroState hero, AbilityDef ability, int part, int ally)
    {
        ClearHold(hero);
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

    private void ClearHold(HeroState hero)
    {
        hero.WaitDtBp = 0;
        hero.DeferUntilTick = 0;
        hero.DeferStartedTick = 0;
    }

    private void ResolveHeroAbility(HeroState hero, AbilityDef ability, int part, int ally, bool fromChant)
    {
        if (!fromChant)
        {
            ClearHold(hero);
        }

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
            int amount = hero.CadenceRefundAp > 0 ? hero.CadenceRefundAp : SimConst.CadenceRefundAp;
            int refund = hero.Ap.Refund(amount);
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
        int hitBp = SimConst.Bp;
        int hitRoll = -1;
        bool hit = true;
        if (!physicalBurst)
        {
            hitBp = Formulas.HitChanceBp(accuracy, _boss.Eva);
            hitRoll = _rng.RollD10000(RngStream.Hit, ability.Name + " hit");
            hit = hitRoll < hitBp;
        }
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
            resistance = Formulas.EffectiveResistanceBp(PartResistance(partIndex, ability.Element), HeroEpen(hero));
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
            MitigationStat = BossDef(),
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
        int preCrit = hit ? DamagePipeline.PreCritDealt(request) : 0;
        int dealt = result.Dealt;
        bool linked = false;
        int detonation = 0;
        string chainNote = "no chain";
        bool primed = hit && hero.Race == RaceId.AshDravan && hero.Heat >= SimConst.HeatCap;
        bool naturalPrime = primed && IsValidTransition(part, ability.Property);
        if (primed && !naturalPrime)
        {
            int baseDetonation = dealt * 5_000 / SimConst.Bp;
            detonation = ThermalBattery.BonusDetonation(baseDetonation);
            OpenL2(part, ResonanceId.Liquefaction, hero, attack > hero.Intel ? attack : hero.Intel);
            chainNote = $"forced Liquefaction detonation {detonation}. L2 until {part.ChainExpires}. Burst until {part.BurstExpires}";
            ConsumeHeat(hero);
        }
        else if (hit && ability.Property != ChainProperty.None)
        {
            (linked, detonation, chainNote) = ApplyChain(part, ability.Property, dealt, physicalLink: true, hero, attack > hero.Intel ? attack : hero.Intel, openerIsBurst: physicalBurst);
            if (naturalPrime)
            {
                detonation = ThermalBattery.BonusDetonation(detonation);
                ConsumeHeat(hero);
                chainNote = $"primed +25% detonation {detonation}. {chainNote}";
                linked = false;
            }
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
        int extraDamage = ApplyExtraHits(hero, ability, part, hit, preCrit, hitBp, critBp);
        TryFlatInterrupt(ability, hit, partIndex);
        int damageForCe = hit ? dealt + detonation + result.TrueDamage + extraDamage : 0;
        GrantEnmity(partIndex, hero, ability, damageForCe, hpRestored: 0, useIdleEnmity: false);
        if (hero.Race == RaceId.SylvariMor && (linked || crit))
        {
            _pendingCadence = hero;
        }

        Log($"{hero.Name} {ability.Name} → {part.Name}. ATK_eff {attack}. Hit {hitBp}bp {RollText(hitRoll)} {(hit ? "hit" : "miss")}. Crit {critBp}bp {RollText(critRoll)}. Dmg {dealt}. {chainNote} Det {detonation}. {part.Name} HP {part.Hp}.");
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
        int resistance = Formulas.EffectiveResistanceBp(PartResistance(partIndex, ability.Element), HeroEpen(hero));
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
        int windowsBefore = _windowsOpened;
        if (magicBurst)
        {
            // Count this spell against the window it qualified in. A closing burst does not replace that window.
            part.BurstsLanded = burstIndex;
        }

        int detonation = 0;
        string chainNote = "no chain";
        if (ability.Property != ChainProperty.None)
        {
            int power = intel > hero.Atk ? intel : hero.Atk;
            (_, detonation, chainNote) = ApplyChain(part, ability.Property, result.Dealt, physicalLink: false, hero, power, openerIsBurst: magicBurst);
        }

        bool windowReplaced = _windowsOpened != windowsBefore;
        int inflicted = result.Dealt + detonation + result.TrueDamage;
        part.Hp -= inflicted;
        if (part.Hp < 0)
        {
            part.Hp = 0;
        }

        if (magicBurst)
        {
            int refundBp = Formulas.BurstRefundBp(hero.Race == RaceId.KithLir, 0);
            int refund = Formulas.MpRefund(ability.MpCost, refundBp);
            hero.Mp += refund;
            if (hero.Race == RaceId.KithLir && !windowReplaced)
            {
                ExtendBurst(part, early);
            }

            string windowNote = windowReplaced
                ? $"detonation replaced the burst window, now ends {part.BurstExpires}"
                : $"window ends {part.BurstExpires}";
            Log($"{hero.Name} Magic Burst #{burstIndex} refunds {refund} MP → {hero.Mp}. {windowNote}.");
        }

        TryInterruptBoss(partIndex, result.Dealt);
        GrantEnmity(partIndex, hero, ability, result.Dealt + detonation + result.TrueDamage, hpRestored: 0, useIdleEnmity: false);
        Log($"{hero.Name} {ability.Name} → {part.Name}. INT {intel}. Hit {hitBp}bp {RollText(hitRoll)}{(resisted ? " resisted" : " hit")}. Crit {critBp}bp {RollText(critRoll)} {(crit ? "crit" : "no crit")}. Dmg {result.Dealt}. {chainNote}. {part.Name} HP {part.Hp}.");
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

        if (target.WaitDtBp > 0)
        {
            reduction += target.WaitDtBp;
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

        if (target.Race == RaceId.AshDravan && ability.Element != ElementId.None && (magical || hit))
        {
            int gain = Formulas.HeatGain(result.Total, target.MaxHp, ability.Element == ElementId.Fire);
            target.Heat = Formulas.ApplyHeat(target.Heat, gain);
            target.HeatDecayTick = _tick + SimConst.HeatDecayTicks;
            Log($"{target.Name} Heat +{gain} → {target.Heat}.");
        }

        _boss.Ap.PayRecovery(ability.RecoveryAp);
        _boss.PounceAvailable = true;
        Log($"Boss {ability.Name} → {target.Name}. Hit {hitBp}bp {RollText(hitRoll)} {(hit ? "hit" : "miss")}. Crit {critBp}bp {RollText(critRoll)}. Dmg {taken}. {target.Name} HP {target.Hp}. Boss AP {ApGauge.Format(_boss.Ap.Centi)}.");
    }

    private void Shed(int tableIndex, int heroIndex, int damage, int maxHp)
    {
        ref EnmitySlot slot = ref _boss.Parts[tableIndex].Enmity[heroIndex];
        int shed = Formulas.CeShed(slot.Ce, damage, maxHp);
        slot.Ce -= shed;
        Log($"{_heroes[heroIndex].Name} sheds {shed} CE on {_boss.Parts[tableIndex].Name}. CE {slot.Ce}.");
    }

    private (bool Linked, int Detonation, string Note) ApplyChain(
        BossPartState part,
        ChainProperty incoming,
        int closingDamage,
        bool physicalLink,
        HeroState closer,
        int attackOrInt,
        bool openerIsBurst)
    {
        if (part.Tier == 0 || _tick >= part.ChainExpires)
        {
            OpenL1(part, incoming, closer);
            return (false, 0, $"chain L1({incoming}) until {part.ChainExpires}");
        }

        if (part.Tier == 1)
        {
            ResonanceId resonance = ResonanceTable.LookupL2(part.Property, incoming);
            if (resonance == ResonanceId.None)
            {
                OpenL1(part, incoming, closer);
                return (false, 0, $"chain restarts L1({incoming}) until {part.ChainExpires}");
            }

            int detonation = closingDamage * 5_000 / SimConst.Bp;
            if (openerIsBurst)
            {
                // Detonate and spend the chain. Do not open the L2 resonance window or a new burst window.
                ApplyResonanceEffect(resonance, part, closer, attackOrInt);
                ClearChain(part);
                return (physicalLink, detonation, $"{resonance} detonation {detonation}. Chain closed; burst closer opens no resonance window and no burst window");
            }

            OpenL2(part, resonance, closer, attackOrInt);
            return (physicalLink, detonation, $"{resonance} detonation {detonation}. L2 until {part.ChainExpires}. Burst until {part.BurstExpires}");
        }

        ApexId apex = ResonanceTable.LookupL3(part.Resonance, incoming);
        if (apex == ApexId.None)
        {
            OpenL1(part, incoming, closer);
            return (false, 0, $"chain restarts L1({incoming}) until {part.ChainExpires}");
        }

        ClearChain(part);
        ApplyApexEffect(apex, part, closer, attackOrInt);
        if (!openerIsBurst)
        {
            OpenBurst(part, ResonanceTable.Profile(apex));
        }

        string note = openerIsBurst
            ? $"{apex} true detonation {closingDamage}. Chain closed; burst closer opens no burst window"
            : $"{apex} true detonation {closingDamage}";
        return (physicalLink, closingDamage, note);
    }

    private bool IsValidTransition(BossPartState part, ChainProperty incoming)
    {
        if (incoming == ChainProperty.None || part.Tier == 0 || _tick >= part.ChainExpires)
        {
            return false;
        }

        if (part.Tier == 1)
        {
            return ResonanceTable.LookupL2(part.Property, incoming) != ResonanceId.None;
        }

        return ResonanceTable.LookupL3(part.Resonance, incoming) != ApexId.None;
    }

    private void OpenL1(BossPartState part, ChainProperty property, HeroState closer)
    {
        part.Tier = 1;
        part.Property = property;
        part.Resonance = ResonanceId.None;
        part.ChainExpires = _tick + SimConst.ChainWindowTicks;
        part.Contributors = 1 << closer.Slot;
    }

    private void OpenL2(BossPartState part, ResonanceId resonance, HeroState closer, int attackOrInt)
    {
        part.Contributors |= 1 << closer.Slot;
        part.Tier = 2;
        part.Property = ChainProperty.None;
        part.Resonance = resonance;
        part.ChainExpires = _tick + SimConst.ChainWindowTicks;
        OpenBurst(part, ResonanceTable.Profile(resonance));
        ApplyResonanceEffect(resonance, part, closer, attackOrInt);
    }

    private static void ClearChain(BossPartState part)
    {
        part.Tier = 0;
        part.Property = ChainProperty.None;
        part.Resonance = ResonanceId.None;
        part.ChainExpires = 0;
        part.Contributors = 0;
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
        _windowsOpened++;
    }

    private void ApplyResonanceEffect(ResonanceId resonance, BossPartState part, HeroState closer, int attackOrInt)
    {
        switch (resonance)
        {
            case ResonanceId.Liquefaction:
                ApplyBurn(part, closer, attackOrInt, burnIii: false);
                break;
            case ResonanceId.Induration:
                _boss.SlowBp = SimConst.IndurationSlowBp;
                _boss.SlowExpires = _tick + SimConst.IndurationSlowTicks;
                Log($"Induration slow −30% AGI until {_boss.SlowExpires}.");
                break;
            case ResonanceId.Fragmentation:
                ApplyShatter(SimConst.ShatterFragmentationBp, SimConst.ShatterFragmentationTicks);
                break;
            case ResonanceId.Distortion:
                PurgeBeneficial();
                break;
        }
    }

    private void ApplyApexEffect(ApexId apex, BossPartState part, HeroState closer, int attackOrInt)
    {
        switch (apex)
        {
            case ApexId.SolarApex:
                ResetVolatileToAnchor(part);
                break;
            case ApexId.MagmaCore:
                ApplyBurn(part, closer, attackOrInt, burnIii: true);
                ApplyShatter(SimConst.ShatterMagmaBp, SimConst.ShatterMagmaTicks);
                break;
        }
    }

    private void ApplyBurn(BossPartState part, HeroState closer, int attackOrInt, bool burnIii)
    {
        int power = attackOrInt;
        int ratio = burnIii ? SimConst.BurnIiiRatioBp : SimConst.BurnRatioBp;
        int raw = (int)((long)power * ratio / SimConst.Bp);
        int resistance = Formulas.EffectiveResistanceBp(PartResistance(0, ElementId.Fire), HeroEpen(closer));
        int kept = SimConst.Bp - resistance;
        if (kept < 0)
        {
            kept = 0;
        }

        part.BurnPulse = (int)((long)raw * kept / SimConst.Bp);
        part.BurnIii = burnIii;
        part.BurnExpires = _tick + SimConst.BurnDuration;
        part.BurnNextTick = _tick + SimConst.GlobalPhase;
        Log($"{part.Name} {(burnIii ? "Burn III" : "Burn")} pulse {part.BurnPulse} until {part.BurnExpires}.");
    }

    private void ApplyShatter(int bp, int ticks)
    {
        bool active = _boss.ShatterBp > 0 && _tick < _boss.ShatterExpires;
        if (active && _boss.ShatterBp > bp)
        {
            Log($"DEF Shatter stays {_boss.ShatterBp}bp until {_boss.ShatterExpires}.");
            return;
        }

        _boss.ShatterBp = bp;
        _boss.ShatterExpires = _tick + ticks;
        Log($"DEF Shatter {_boss.ShatterBp}bp until {_boss.ShatterExpires}. DEF {BossDef()}.");
    }

    private void PurgeBeneficial()
    {
        int removed = 0;
        while (removed < SimConst.PurgeCount && _boss.Beneficial.Count > 0)
        {
            int last = _boss.Beneficial.Count - 1;
            string name = _boss.Beneficial[last];
            _boss.Beneficial.RemoveAt(last);
            removed++;
            Log($"Buff Purge removes {name}.");
        }

        if (removed == 0)
        {
            Log("Buff Purge finds no beneficial effects.");
        }
    }

    private void ResetVolatileToAnchor(BossPartState part)
    {
        int sum = 0;
        for (int i = 0; i < part.Enmity.Length; i++)
        {
            sum += part.Enmity[i].Ve;
            part.Enmity[i].NormalVe = 0;
            part.Enmity[i].HeavyVe = 0;
        }

        int tank = AnchorIndex(part);
        if (tank >= 0 && sum > 0)
        {
            EnmityMath.AddVolatile(ref part.Enmity[tank], sum, heavy: false);
        }

        string tankName = tank >= 0 ? _heroes[tank].Name : "nobody";
        Log($"Solar Apex VE reset. {sum} VE moved to {tankName}.");
    }

    private int AnchorIndex(BossPartState part)
    {
        for (int i = 0; i < _heroes.Length; i++)
        {
            if (_heroes[i].IsAnchor && _heroes[i].IsAlive)
            {
                return i;
            }
        }

        int best = -1;
        int bestCe = -1;
        for (int i = 0; i < _heroes.Length; i++)
        {
            if (!_heroes[i].IsAlive || i >= part.Enmity.Length)
            {
                continue;
            }

            int ce = part.Enmity[i].Ce;
            if (ce > bestCe)
            {
                bestCe = ce;
                best = i;
            }
        }

        return best;
    }

    private int BossDef()
    {
        if (_boss.ShatterBp <= 0 || _tick >= _boss.ShatterExpires)
        {
            return _boss.Def;
        }

        return (int)((long)_boss.Def * (SimConst.Bp - _boss.ShatterBp) / SimConst.Bp);
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

    private int ApplyExtraHits(HeroState hero, AbilityDef ability, BossPartState part, bool hit, int preCrit, int hitBp, int critBp)
    {
        if (!hit || !ability.UsesWeaponLoadout)
        {
            return 0;
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

        if (extra <= 0)
        {
            return 0;
        }

        Log($"{hero.Name} multi-attack +{extra}.");
        int total = 0;
        int critMultiplier = Formulas.CritMultiplierBp(hero.Gear.WsCritDamageBp);
        for (int i = 0; i < extra; i++)
        {
            int extraHitRoll = _rng.RollD10000(RngStream.Hit, ability.Name + " extra hit");
            if (extraHitRoll >= hitBp)
            {
                Log($"{hero.Name} extra hit {i + 1} miss (roll {extraHitRoll}).");
                continue;
            }

            int extraCritRoll = _rng.RollD10000(RngStream.Crit, ability.Name + " extra crit");
            int damage = preCrit * SimConst.ExtraHitBp / SimConst.Bp;
            bool extraCrit = extraCritRoll < critBp;
            if (extraCrit)
            {
                damage = (int)((long)damage * critMultiplier / SimConst.Bp);
                if (hero.Race == RaceId.SylvariMor)
                {
                    _pendingCadence = hero;
                }
            }

            part.Hp -= damage;
            if (part.Hp < 0)
            {
                part.Hp = 0;
            }

            total += damage;
            Log($"{hero.Name} extra hit {i + 1} {damage}{(extraCrit ? " crit" : "")}.");
        }

        return total;
    }

    private void TryFlatInterrupt(AbilityDef ability, bool hit, int partIndex)
    {
        if (!hit || ability.FlatInterruptBp <= 0 || !_boss.Casting || _boss.CastPart != partIndex)
        {
            return;
        }

        int roll = _rng.RollD10000(RngStream.Interrupt, ability.Name + " flat interrupt");
        Log($"Flat interrupt {ability.FlatInterruptBp}bp roll {roll}.");
        if (roll < ability.FlatInterruptBp)
        {
            _boss.Casting = false;
            _boss.Ap.PayRecovery(SimConst.RecoveryStance);
            Log($"Boss chant interrupted (roll {roll}).");
        }
    }

    private void ConsumeHeat(HeroState hero)
    {
        hero.Heat = hero.HeatAfterPrime;
        hero.HeatDecayTick = hero.Heat > 0 ? _tick + SimConst.HeatDecayTicks : 0;
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

    private int PartResistance(int partIndex, ElementId element)
    {
        _ = partIndex;
        if (element < 0 || _boss.ResistBp.Length <= (int)element)
        {
            return 0;
        }

        return _boss.ResistBp[(int)element];
    }

    private static string RollText(int roll) => roll < 0 ? "no roll" : $"roll {roll}";

    private void Log(string text)
    {
        _events.Add(new CombatEvent(_tick, text));
    }
}
