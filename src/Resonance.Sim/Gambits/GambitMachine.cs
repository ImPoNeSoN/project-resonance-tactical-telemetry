using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Sim;

namespace Resonance.Sim.Gambits;

public readonly struct GambitView
{
    public GambitView(HeroState[] heroes, BossState boss, int actor, int tick, int aetherDensity, int globalBurstMask)
    {
        Heroes = heroes;
        Boss = boss;
        Actor = actor;
        Tick = tick;
        AetherDensity = aetherDensity;
        GlobalBurstMask = globalBurstMask;
    }

    public HeroState[] Heroes { get; }

    public BossState Boss { get; }

    public int Actor { get; }

    public int Tick { get; }

    public int AetherDensity { get; }

    public int GlobalBurstMask { get; }
}

/// <summary>
/// Evaluates a compiled deck against the sim state. No allocations and no RNG.
/// </summary>
public static class GambitMachine
{
    public static GambitDecision Evaluate(GambitProgram program, in GambitView view)
    {
        HeroState actor = view.Heroes[view.Actor];
        bool retargeted = false;
        bool deferOpen = actor.DeferUntilTick > view.Tick;
        for (int s = 0; s < program.Slots.Length; s++)
        {
            GambitSlot slot = program.Slots[s];
            if (slot.Verb == GambitVerb.Defer && !deferOpen && actor.DeferUntilTick != 0 && view.Tick >= actor.DeferUntilTick)
            {
                continue;
            }

            if (slot.Verb == GambitVerb.Retarget && retargeted)
            {
                continue;
            }

            if (!Conditions(slot, view))
            {
                continue;
            }

            if (slot.Verb == GambitVerb.Retarget)
            {
                actor.CurrentTargetPart = slot.Number;
                retargeted = true;
                continue;
            }

            if (!Executable(slot, view, actor))
            {
                continue;
            }

            if (slot.Verb == GambitVerb.Defer)
            {
                return new GambitDecision
                {
                    Outcome = GambitOutcome.Defer,
                    DeferTicks = slot.Number,
                    Slot = s,
                };
            }

            if (slot.Verb == GambitVerb.Wait)
            {
                return new GambitDecision
                {
                    Outcome = GambitOutcome.Wait,
                    Slot = s,
                    SpendHeat = (slot.Flags & GambitFlags.SpendHeat) != 0,
                };
            }

            ResolveTargets(slot, view, actor, out int part, out int ally);
            return new GambitDecision
            {
                Outcome = GambitOutcome.Act,
                AbilityId = slot.AbilityId,
                TargetPart = part,
                TargetHero = ally,
                Slot = s,
                SpendHeat = (slot.Flags & GambitFlags.SpendHeat) != 0,
            };
        }

        return new GambitDecision
        {
            Outcome = GambitOutcome.Act,
            AbilityId = AbilityCatalog.Attack,
            TargetPart = actor.CurrentTargetPart,
            TargetHero = -1,
            Slot = -1,
        };
    }

    private static bool Conditions(in GambitSlot slot, in GambitView view)
    {
        for (int i = 0; i < slot.Count; i++)
        {
            GambitCondition condition = slot.Condition(i);
            bool value = Predicate(condition, view);
            if (condition.Not)
            {
                value = !value;
            }

            if (!value)
            {
                return false;
            }
        }

        return true;
    }

    private static bool Executable(in GambitSlot slot, in GambitView view, HeroState actor)
    {
        if (slot.Verb == GambitVerb.Defer || slot.Verb == GambitVerb.Wait || slot.Verb == GambitVerb.Retarget)
        {
            if ((slot.Flags & GambitFlags.SpendHeat) != 0 && actor.Heat < SimConst.SpendHeatCost)
            {
                return false;
            }

            return true;
        }

        AbilityDef ability = AbilityCatalog.Get(slot.AbilityId);
        if (ability.Id == AbilityCatalog.ShieldBash && !actor.HasBulwarkBash)
        {
            return false;
        }

        if (actor.Mp < ability.MpCost)
        {
            return false;
        }

        if (actor.Silenced && ability.ChantTicks > 0)
        {
            return false;
        }

        if ((slot.Flags & GambitFlags.SpendHeat) != 0 && actor.Heat < SimConst.SpendHeatCost)
        {
            return false;
        }

        ResolveTargets(slot, view, actor, out int part, out int ally);
        if (ally < 0)
        {
            if ((uint)part >= (uint)view.Boss.Parts.Length)
            {
                return false;
            }

            BossPartState target = view.Boss.Parts[part];
            if (!target.Active || target.Hp <= 0)
            {
                return false;
            }
        }
        else if ((uint)ally >= (uint)view.Heroes.Length || !view.Heroes[ally].IsAlive)
        {
            return false;
        }

        if ((slot.Flags & GambitFlags.NoOverwrite) != 0 && WouldRestartL2(view, part, ability))
        {
            return false;
        }

        if ((slot.Flags & GambitFlags.BurstOnly) != 0 && !LandsInBurst(view, actor, part, ability))
        {
            return false;
        }

        return true;
    }

    private static bool WouldRestartL2(in GambitView view, int part, AbilityDef ability)
    {
        if ((uint)part >= (uint)view.Boss.Parts.Length || ability.Property == ChainProperty.None)
        {
            return false;
        }

        BossPartState state = view.Boss.Parts[part];
        if (state.Tier != 2 || view.Tick >= state.ChainExpires)
        {
            return false;
        }

        return ResonanceTable.LookupL3(state.Resonance, ability.Property) == ApexId.None;
    }

    private static bool LandsInBurst(in GambitView view, HeroState actor, int part, AbilityDef ability)
    {
        if ((uint)part >= (uint)view.Boss.Parts.Length)
        {
            return false;
        }

        BossPartState state = view.Boss.Parts[part];
        if (!state.BurstActive)
        {
            return false;
        }

        int fastCast = Formulas.TotalFastCastBp(actor.Gear.FastCastBp, actor.Race == RaceId.AethelBorn ? SimConst.AethelFastCastBp : 0, 0);
        int chant = Formulas.EffectiveChantTicks(ability.ChantTicks, fastCast);
        int resolve = view.Tick + chant;
        if (resolve >= state.BurstExpires)
        {
            return false;
        }

        if (ability.Kind == AbilityKind.Magical)
        {
            return (state.BurstMask & ElementMaps.Mask(ability.Element)) != 0;
        }

        if (ability.UsesWeaponLoadout)
        {
            return (state.BurstMask & ElementMask.Physical) != 0;
        }

        return false;
    }

    private static void ResolveTargets(in GambitSlot slot, in GambitView view, HeroState actor, out int part, out int ally)
    {
        ally = -1;
        part = actor.CurrentTargetPart;
        AbilityDef ability = slot.AbilityId == 0 ? AbilityCatalog.Get(AbilityCatalog.Attack) : AbilityCatalog.Get(slot.AbilityId);
        if (slot.TargetKind == GambitTargetKind.Default && ability.Kind == AbilityKind.Healing)
        {
            ally = PickAlly(view, actor, AllyPick.LowestHp, null);
            return;
        }

        if (slot.TargetKind == GambitTargetKind.Self)
        {
            ally = view.Actor;
            return;
        }

        if (slot.TargetKind == GambitTargetKind.Part)
        {
            part = slot.TargetArg;
            return;
        }

        if (slot.TargetKind == GambitTargetKind.Ally)
        {
            ally = PickAlly(view, actor, slot.Ally, slot.TargetText);
        }
    }

    private static bool Predicate(in GambitCondition condition, in GambitView view)
    {
        HeroState actor = view.Heroes[view.Actor];
        int value = ValueOf(condition, view, actor);
        if (condition.Predicate is GambitPredicate.HasBuff or GambitPredicate.HasDebuff or GambitPredicate.Casting
            or GambitPredicate.Disabled or GambitPredicate.TargetingMe or GambitPredicate.Primed
            or GambitPredicate.Ko or GambitPredicate.Always or GambitPredicate.Resonance
            or GambitPredicate.Window or GambitPredicate.MpForAbility or GambitPredicate.GlobalBurst
            or GambitPredicate.Aquifer)
        {
            return value != 0;
        }

        return Compare(condition.Compare, value, condition.Threshold);
    }

    private static int ValueOf(in GambitCondition condition, in GambitView view, HeroState actor)
    {
        switch (condition.Predicate)
        {
            case GambitPredicate.Always:
                return 1;
            case GambitPredicate.Aquifer:
                return 0;
            case GambitPredicate.Resonance:
                return BurstIncludes(PartOf(condition, view, actor), (ElementId)condition.Arg0, view.Tick) ? 1 : 0;
            case GambitPredicate.Window:
                return WindowIs(PartOf(condition, view, actor), condition.Arg0, condition.Arg1, view.Tick) ? 1 : 0;
            case GambitPredicate.WindowLeft:
                return WindowLeft(PartOf(condition, view, actor), view.Tick);
            case GambitPredicate.BurstLeft:
                return BurstLeft(PartOf(condition, view, actor), view.Tick);
            case GambitPredicate.Casting:
                return IsCasting(condition, view, actor) ? 1 : 0;
            case GambitPredicate.CastResolvesIn:
                return ResolvesIn(condition, view, actor);
            case GambitPredicate.HpPercent:
                return HpPercent(condition, view, actor);
            case GambitPredicate.MpPercent:
                return actor.MaxMp <= 0 ? 0 : actor.Mp * 100 / actor.MaxMp;
            case GambitPredicate.Mp:
                return actor.Mp;
            case GambitPredicate.MpForAbility:
                return actor.Mp >= AbilityCatalog.Get(condition.Arg0).MpCost ? 1 : 0;
            case GambitPredicate.Ap:
                return view.Boss.Ap.WholeAp;
            case GambitPredicate.HasBuff:
                return HasBuff(condition, view, actor) ? 1 : 0;
            case GambitPredicate.HasDebuff:
                return HasDebuff(condition, view, actor) ? 1 : 0;
            case GambitPredicate.Disabled:
            {
                BossPartState part = PartOf(condition, view, actor);
                return !part.Active || part.Hp <= 0 ? 1 : 0;
            }
            case GambitPredicate.TargetingMe:
                return ArgMax(PartOf(condition, view, actor), view.Heroes) == view.Actor ? 1 : 0;
            case GambitPredicate.MyEnmityRank:
                return Rank(PartOf(condition, view, actor), view.Heroes, view.Actor);
            case GambitPredicate.TankMargin:
                return Margin(PartOf(condition, view, actor), view.Heroes);
            case GambitPredicate.Heat:
                return actor.Heat;
            case GambitPredicate.Primed:
                return actor.Heat >= SimConst.HeatCap ? 1 : 0;
            case GambitPredicate.Deferred:
                return actor.DeferStartedTick <= 0 ? 0 : view.Tick - actor.DeferStartedTick;
            case GambitPredicate.ReadyIn:
                return ReadyIn(PickAlly(view, actor, condition.Ally, condition.Text), view);
            case GambitPredicate.Ko:
            {
                int ally = PickAlly(view, actor, condition.Ally, condition.Text);
                return ally >= 0 && !view.Heroes[ally].IsAlive ? 1 : 0;
            }
            case GambitPredicate.CountBelowHp:
                return CountBelow(view.Heroes, condition.Arg0);
            case GambitPredicate.ChainParticipants:
                return Bits(PartOf(condition, view, actor).Contributors);
            case GambitPredicate.AetherDensity:
                return view.AetherDensity;
            case GambitPredicate.Tick:
                return view.Tick;
            case GambitPredicate.GlobalBurst:
                if (condition.Arg0 < 0)
                {
                    return view.GlobalBurstMask != 0 ? 1 : 0;
                }

                return (view.GlobalBurstMask & (int)ElementMaps.Mask((ElementId)condition.Arg0)) != 0 ? 1 : 0;
            default:
                return 0;
        }
    }

    private static BossPartState PartOf(in GambitCondition condition, in GambitView view, HeroState actor)
    {
        int index = actor.CurrentTargetPart;
        if (condition.Subject == GambitSubject.Boss)
        {
            index = condition.SubjectArg >= 0 ? condition.SubjectArg : 0;
        }

        if ((uint)index >= (uint)view.Boss.Parts.Length)
        {
            index = 0;
        }

        return view.Boss.Parts[index];
    }

    private static bool BurstIncludes(BossPartState part, ElementId element, int tick)
    {
        return part.BurstActive && tick < part.BurstExpires && (part.BurstMask & ElementMaps.Mask(element)) != 0;
    }

    private static bool WindowIs(BossPartState part, int kind, int id, int tick)
    {
        if (part.Tier == 0 || tick >= part.ChainExpires)
        {
            return false;
        }

        if (kind == 0)
        {
            return part.Tier == 1 && (int)part.Property == id;
        }

        return part.Tier == 2 && (int)part.Resonance == id;
    }

    private static int WindowLeft(BossPartState part, int tick)
    {
        if (part.Tier == 0 || tick >= part.ChainExpires)
        {
            return 0;
        }

        return part.ChainExpires - tick;
    }

    private static int BurstLeft(BossPartState part, int tick)
    {
        if (!part.BurstActive || tick >= part.BurstExpires)
        {
            return 0;
        }

        return part.BurstExpires - tick;
    }

    private static bool IsCasting(in GambitCondition condition, in GambitView view, HeroState actor)
    {
        if (condition.Subject == GambitSubject.Ally)
        {
            int ally = PickAlly(view, actor, condition.Ally, condition.Text);
            if (ally < 0 || !view.Heroes[ally].Casting)
            {
                return false;
            }

            return condition.Arg0 < 0 || view.Heroes[ally].CastAbilityId == condition.Arg0;
        }

        if (!view.Boss.Casting)
        {
            return false;
        }

        if (condition.Subject == GambitSubject.Boss && condition.SubjectArg >= 0 && view.Boss.CastPart != condition.SubjectArg)
        {
            return false;
        }

        if (condition.Subject == GambitSubject.Target && view.Boss.CastPart != actor.CurrentTargetPart)
        {
            return false;
        }

        return condition.Arg0 < 0 || view.Boss.CastAbilityId == condition.Arg0;
    }

    private static int ResolvesIn(in GambitCondition condition, in GambitView view, HeroState actor)
    {
        if (!IsCasting(condition, view, actor))
        {
            return int.MaxValue / 4;
        }

        int resolve = condition.Subject == GambitSubject.Ally
            ? view.Heroes[PickAlly(view, actor, condition.Ally, condition.Text)].CastResolveTick
            : view.Boss.CastResolveTick;
        int left = resolve - view.Tick;
        return left < 0 ? 0 : left;
    }

    private static int HpPercent(in GambitCondition condition, in GambitView view, HeroState actor)
    {
        if (condition.Subject == GambitSubject.Self)
        {
            return actor.MaxHp <= 0 ? 0 : actor.Hp * 100 / actor.MaxHp;
        }

        if (condition.Subject == GambitSubject.Ally)
        {
            int ally = PickAlly(view, actor, condition.Ally, condition.Text);
            if (ally < 0 || view.Heroes[ally].MaxHp <= 0)
            {
                return 0;
            }

            return view.Heroes[ally].Hp * 100 / view.Heroes[ally].MaxHp;
        }

        BossPartState part = PartOf(condition, view, actor);
        return part.MaxHp <= 0 ? 0 : part.Hp * 100 / part.MaxHp;
    }

    private static bool HasBuff(in GambitCondition condition, in GambitView view, HeroState actor)
    {
        string name = condition.Text ?? "";
        if (condition.Subject == GambitSubject.Self || condition.Subject == GambitSubject.Ally)
        {
            HeroState hero = condition.Subject == GambitSubject.Self
                ? actor
                : AllyOrActor(view, actor, condition);
            if (name.Equals("Keratin Bastion", StringComparison.OrdinalIgnoreCase))
            {
                return hero.PhysicalHitsMitigated && view.Tick < hero.PhysDtExpires;
            }

            if (name.Equals("Phase Sanctuary", StringComparison.OrdinalIgnoreCase))
            {
                return hero.AllDtBp > 0 && view.Tick < hero.AllDtExpires;
            }

            if (name.Equals("Concentration", StringComparison.OrdinalIgnoreCase))
            {
                return hero.ConcentrationBuff > 0 && view.Tick < hero.ConcentrationBuffExpires;
            }

            if (name.Equals("Timeline Stalk", StringComparison.OrdinalIgnoreCase))
            {
                return hero.RecoveryCut > 0 && view.Tick < hero.RecoveryCutExpires;
            }

            return false;
        }

        var buffs = view.Boss.Beneficial;
        for (int i = 0; i < buffs.Count; i++)
        {
            if (buffs[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasDebuff(in GambitCondition condition, in GambitView view, HeroState actor)
    {
        string name = condition.Text ?? "";
        BossPartState part = PartOf(condition, view, actor);
        if (name.Equals("Slow", StringComparison.OrdinalIgnoreCase) || name.Equals("Induration", StringComparison.OrdinalIgnoreCase))
        {
            return view.Tick < view.Boss.SlowExpires && view.Boss.SlowBp > 0;
        }

        if (name.Equals("Shatter", StringComparison.OrdinalIgnoreCase) || name.Equals("DEF Shatter", StringComparison.OrdinalIgnoreCase))
        {
            return view.Boss.ShatterBp > 0 && view.Tick < view.Boss.ShatterExpires;
        }

        if (name.Equals("Burn III", StringComparison.OrdinalIgnoreCase))
        {
            return part.BurnIii && part.BurnPulse > 0;
        }

        if (name.Equals("Burn", StringComparison.OrdinalIgnoreCase))
        {
            return part.BurnPulse > 0;
        }

        return false;
    }

    private static HeroState AllyOrActor(in GambitView view, HeroState actor, in GambitCondition condition)
    {
        int ally = PickAlly(view, actor, condition.Ally, condition.Text);
        return ally < 0 ? actor : view.Heroes[ally];
    }

    private static int ReadyIn(int ally, in GambitView view)
    {
        if (ally < 0)
        {
            return int.MaxValue / 4;
        }

        HeroState hero = view.Heroes[ally];
        if (hero.Casting)
        {
            int left = hero.CastResolveTick - view.Tick;
            return left < 0 ? 0 : left;
        }

        int gain = Formulas.AgiCentiPerTick(hero.Agi, 0, 0);
        return ApGauge.TicksUntilReady(hero.Ap.Centi, gain);
    }

    private static int CountBelow(HeroState[] heroes, int percent)
    {
        int count = 0;
        for (int i = 0; i < heroes.Length; i++)
        {
            int hp = heroes[i].MaxHp <= 0 ? 0 : heroes[i].Hp * 100 / heroes[i].MaxHp;
            if (hp < percent)
            {
                count++;
            }
        }

        return count;
    }

    private static int PickAlly(in GambitView view, HeroState actor, AllyPick pick, string? name)
    {
        int best = -1;
        int bestScore = int.MaxValue;
        for (int i = 0; i < view.Heroes.Length; i++)
        {
            if (i == view.Actor)
            {
                continue;
            }

            HeroState hero = view.Heroes[i];
            switch (pick)
            {
                case AllyPick.Named:
                    if (name != null && hero.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }

                    break;
                case AllyPick.Tank:
                    if (hero.IsAnchor)
                    {
                        return i;
                    }

                    break;
                case AllyPick.Casting:
                    if (hero.Casting && (best < 0 || i < best))
                    {
                        best = i;
                    }

                    break;
                case AllyPick.Targeted:
                    return ArgMax(view.Boss.Parts[actor.CurrentTargetPart < view.Boss.Parts.Length ? actor.CurrentTargetPart : 0], view.Heroes);
                case AllyPick.LowestMp:
                {
                    int score = hero.MaxMp <= 0 ? 0 : hero.Mp * 100 / hero.MaxMp;
                    if (best < 0 || score < bestScore || (score == bestScore && i < best))
                    {
                        best = i;
                        bestScore = score;
                    }

                    break;
                }
                default:
                {
                    int score = hero.MaxHp <= 0 ? 0 : hero.Hp * 100 / hero.MaxHp;
                    if (best < 0 || score < bestScore || (score == bestScore && i < best))
                    {
                        best = i;
                        bestScore = score;
                    }

                    break;
                }
            }
        }

        return best;
    }

    private static int ArgMax(BossPartState part, HeroState[] heroes)
    {
        int best = -1;
        for (int i = 0; i < part.Enmity.Length && i < heroes.Length; i++)
        {
            if (!heroes[i].IsAlive || part.Enmity[i].Total <= 0)
            {
                continue;
            }

            if (best < 0 || Beats(part.Enmity[i], i, part.Enmity[best], best))
            {
                best = i;
            }
        }

        return best;
    }

    private static bool Beats(in EnmitySlot candidate, int candidateSlot, in EnmitySlot current, int currentSlot)
    {
        if (candidate.Total != current.Total)
        {
            return candidate.Total > current.Total;
        }

        if (candidate.Ce != current.Ce)
        {
            return candidate.Ce > current.Ce;
        }

        return candidateSlot < currentSlot;
    }

    private static int Rank(BossPartState part, HeroState[] heroes, int actor)
    {
        if ((uint)actor >= (uint)part.Enmity.Length || !heroes[actor].IsAlive)
        {
            return heroes.Length;
        }

        int better = 0;
        for (int i = 0; i < part.Enmity.Length && i < heroes.Length; i++)
        {
            if (i == actor || !heroes[i].IsAlive || part.Enmity[i].Total <= 0)
            {
                continue;
            }

            if (Beats(part.Enmity[i], i, part.Enmity[actor], actor))
            {
                better++;
            }
        }

        return better + 1;
    }

    private static int Margin(BossPartState part, HeroState[] heroes)
    {
        int tank = -1;
        for (int i = 0; i < heroes.Length; i++)
        {
            if (heroes[i].IsAnchor && heroes[i].IsAlive)
            {
                tank = i;
                break;
            }
        }

        if (tank < 0)
        {
            tank = 0;
        }

        int tankTotal = (uint)tank < (uint)part.Enmity.Length ? part.Enmity[tank].Total : 0;
        int other = 0;
        for (int i = 0; i < part.Enmity.Length && i < heroes.Length; i++)
        {
            if (i == tank || !heroes[i].IsAlive)
            {
                continue;
            }

            if (part.Enmity[i].Total > other)
            {
                other = part.Enmity[i].Total;
            }
        }

        return tankTotal - other;
    }

    private static int Bits(int mask)
    {
        int count = 0;
        while (mask != 0)
        {
            count += mask & 1;
            mask >>= 1;
        }

        return count;
    }

    private static bool Compare(GambitCompare compare, int value, int threshold) => compare switch
    {
        GambitCompare.Less => value < threshold,
        GambitCompare.LessOrEqual => value <= threshold,
        GambitCompare.Greater => value > threshold,
        GambitCompare.GreaterOrEqual => value >= threshold,
        GambitCompare.Equal => value == threshold,
        _ => value != 0,
    };
}
