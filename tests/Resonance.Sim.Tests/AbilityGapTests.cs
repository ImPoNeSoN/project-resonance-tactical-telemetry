using System.Text;
using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Gambits;
using Resonance.Sim.Sim;

namespace Resonance.Sim.Tests;

public class AbilityGapTests
{
    [Fact]
    public void Default_decks_bash_an_open_chant_and_shield_an_unprotected_ally()
    {
        BattleSimulator sim = CarapaceEncounter.Create(PartyPreset.IceLattice, 1UL);
        HeroState[] heroes = Copy(sim);
        heroes[0].CurrentTargetPart = 0;
        sim.Boss.Casting = true;
        sim.Boss.CastPart = 0;
        sim.Boss.CastAbilityId = AbilityCatalog.OverpressureLance;
        sim.Boss.CastResolveTick = 150;
        GambitDecision open = GambitMachine.Evaluate(heroes[0].Deck!, View(heroes, sim.Boss, 0));
        Assert.Equal(AbilityCatalog.ShieldBash, open.AbilityId);

        sim.Boss.CastResolveTick = 5_000;
        GambitDecision earlyChant = GambitMachine.Evaluate(heroes[0].Deck!, View(heroes, sim.Boss, 0));
        Assert.NotEqual(AbilityCatalog.ShieldBash, earlyChant.AbilityId);

        sim.Boss.CastResolveTick = 150;
        sim.Boss.Parts[0].StunImmuneUntil = 10_000;
        GambitDecision immune = GambitMachine.Evaluate(heroes[0].Deck!, View(heroes, sim.Boss, 0));
        Assert.NotEqual(AbilityCatalog.ShieldBash, immune.AbilityId);

        sim.Boss.Casting = false;
        sim.Boss.Parts[0].StunImmuneUntil = 0;
        sim.Boss.Ap.Centi = 8_500 * SimConst.CentiPerAp;
        GambitDecision earlyGauge = GambitMachine.Evaluate(heroes[0].Deck!, View(heroes, sim.Boss, 0));
        Assert.NotEqual(AbilityCatalog.ShieldBash, earlyGauge.AbilityId);
        sim.Boss.Ap.Centi = 9_500 * SimConst.CentiPerAp;
        GambitDecision soon = GambitMachine.Evaluate(heroes[0].Deck!, View(heroes, sim.Boss, 0));
        Assert.Equal(AbilityCatalog.ShieldBash, soon.AbilityId);

        BattleSimulator choir = CarapaceEncounter.Create(PartyPreset.ShatterChoir, 1UL);
        HeroState[] choirHeroes = Copy(choir);
        choirHeroes[0].CurrentTargetPart = 0;
        choir.Boss.Casting = true;
        choir.Boss.CastPart = 0;
        choir.Boss.CastAbilityId = AbilityCatalog.OverpressureLance;
        choir.Boss.CastResolveTick = 50;
        GambitDecision choirOpen = GambitMachine.Evaluate(choirHeroes[0].Deck!, View(choirHeroes, choir.Boss, 0));
        Assert.Equal(AbilityCatalog.ShieldBash, choirOpen.AbilityId);
        choir.Boss.CastResolveTick = 150;
        GambitDecision choirEarly = GambitMachine.Evaluate(choirHeroes[0].Deck!, View(choirHeroes, choir.Boss, 0));
        Assert.NotEqual(AbilityCatalog.ShieldBash, choirEarly.AbilityId);
        choir.Boss.Casting = false;
        choir.Boss.Ap.Centi = 9_200 * SimConst.CentiPerAp;
        GambitDecision choirGauge = GambitMachine.Evaluate(choirHeroes[0].Deck!, View(choirHeroes, choir.Boss, 0));
        Assert.NotEqual(AbilityCatalog.ShieldBash, choirGauge.AbilityId);
        choir.Boss.Ap.Centi = 9_400 * SimConst.CentiPerAp;
        GambitDecision choirSoon = GambitMachine.Evaluate(choirHeroes[0].Deck!, View(choirHeroes, choir.Boss, 0));
        Assert.Equal(AbilityCatalog.ShieldBash, choirSoon.AbilityId);

        for (int i = 0; i < heroes.Length; i++)
        {
            heroes[i].Hp = heroes[i].MaxHp * 90 / 100;
        }

        heroes[3].Mp = heroes[3].MaxMp;
        heroes[0].Hp = heroes[0].MaxHp * 60 / 100;
        GambitDecision aegis = GambitMachine.Evaluate(heroes[3].Deck!, View(heroes, sim.Boss, 3));
        Assert.Equal(AbilityCatalog.ChoirAegis, aegis.AbilityId);
        Assert.Equal(0, aegis.TargetHero);

        heroes[0].Absorb = 400;
        heroes[0].AbsorbExpires = 5_000;
        sim.Boss.Parts[0].Enmity[1].Ce = 8_000;
        GambitDecision threat = GambitMachine.Evaluate(heroes[3].Deck!, View(heroes, sim.Boss, 3));
        Assert.Equal(AbilityCatalog.ChoirAegis, threat.AbilityId);
        Assert.Equal(1, threat.TargetHero);
    }

    [Fact]
    public void Shield_bash_stuns_then_halves_until_immunity()
    {
        var plan = new Queue<BattleAction>();
        for (int i = 0; i < 14; i++)
        {
            plan.Enqueue(new BattleAction(AbilityCatalog.ShieldBash, 0, -1));
        }

        BattleSimulator sim = BashSim(plan, casting: false);
        sim.RunToEnd();
        string log = Text(sim);
        Assert.Equal(1, Count(log, "stunned for 1500 ticks"));
        Assert.Contains("stunned for 750 ticks", log, StringComparison.Ordinal);
        Assert.Contains("stunned for 23 ticks", log, StringComparison.Ordinal);
        Assert.Contains("is stun immune until", log, StringComparison.Ordinal);
        Assert.Equal(23, sim.Boss.Parts[0].LastStunTicks);
        Assert.True(sim.Boss.Parts[0].StunDrUntil > sim.Tick);
        Assert.True(sim.Tick < sim.Boss.Parts[0].StunImmuneUntil);
        Assert.True(sim.Tick >= sim.Boss.Parts[0].StunExpires);
    }

    [Fact]
    public void A_repeat_after_immunity_halves_and_a_zero_duration_grants_immunity()
    {
        BattleSimulator halved = BashSim(OneBash(), casting: false);
        BossPartState part = halved.Boss.Parts[0];
        part.LastStunTicks = 4;
        part.StunDrUntil = 100_000;
        halved.RunUntil(2);
        Assert.Contains("stunned for 2 ticks", Text(halved), StringComparison.Ordinal);
        Assert.Equal(2, part.LastStunTicks);
        Assert.True(part.StunDrUntil > halved.Tick);

        BattleSimulator spent = BashSim(OneBash(), casting: false);
        BossPartState spentPart = spent.Boss.Parts[0];
        spentPart.LastStunTicks = 1;
        spentPart.StunDrUntil = 100_000;
        spent.RunUntil(2);
        Assert.Contains("stun diminished to nothing", Text(spent), StringComparison.Ordinal);
        Assert.Equal(0, spentPart.LastStunTicks);
        Assert.Equal(0, spentPart.StunDrUntil);
        Assert.True(spentPart.StunImmuneUntil > spent.Tick);
        Assert.Equal(0, spentPart.StunExpires);
    }

    [Fact]
    public void Shield_bash_cancels_a_chant_when_the_interrupt_roll_misses()
    {
        var plan = new Queue<BattleAction>();
        plan.Enqueue(new BattleAction(AbilityCatalog.ShieldBash, 0, -1));
        BattleSimulator sim = BashSim(plan, casting: true);
        int hp = sim.Heroes[0].Hp;
        sim.RunUntil(2);
        Assert.False(sim.Boss.Casting);
        Assert.Contains("Flat interrupt", Text(sim), StringComparison.Ordinal);
        Assert.Contains("Boss chant cancelled: Core stunned.", Text(sim), StringComparison.Ordinal);
        Assert.Contains("stunned for 1500 ticks", Text(sim), StringComparison.Ordinal);
        Assert.Equal(hp, sim.Heroes[0].Hp);
        Assert.DoesNotContain("Boss Overpressure Lance →", Text(sim), StringComparison.Ordinal);
    }

    [Fact]
    public void A_stunned_part_cannot_start_an_action()
    {
        var hero = AliveHero("Korrith");
        hero.Ap = new ApGauge { Centi = 0 };
        var core = new BossPartState { Name = "Core", MaxHp = 80_000, Hp = 80_000, Enmity = new EnmitySlot[1] };
        var arm = new BossPartState
        {
            Name = "Weapon Arm",
            MaxHp = 40_000,
            Hp = 40_000,
            Enmity = new EnmitySlot[1],
            StunExpires = 50_000,
        };
        var boss = new BossState
        {
            Name = "Dummy",
            Atk = 400,
            Acc = 300,
            Agi = 10,
            Ap = new ApGauge { Centi = SimConst.ReadyCenti },
            Parts = [core, arm],
        };
        var script = new BossScript
        {
            PhaseHpBp = 0,
            EnrageTick = 500_000,
            HardEnrageTick = 800_000,
            Phase1 = [AbilityCatalog.PistonSweep, AbilityCatalog.OverpressureLance],
            Phase1Part = [1, 0],
            Phase2 = [AbilityCatalog.PistonSweep],
            Phase2Part = [0],
        };
        var sim = new BattleSimulator(
            [hero],
            boss,
            [new Queue<BattleAction>()],
            new Queue<BattleAction>(),
            Rolls(hit: 0, crit: 9_999, interrupt: 9_999),
            script);
        sim.RunUntil(2);
        string log = Text(sim);
        Assert.Contains("Overpressure Lance", log, StringComparison.Ordinal);
        Assert.DoesNotContain("Piston Sweep", log, StringComparison.Ordinal);
        Assert.Equal(hero.MaxHp, hero.Hp);
    }

    [Fact]
    public void A_queued_action_fizzles_while_the_part_is_stunned()
    {
        var hero = AliveHero("Korrith");
        hero.Ap = new ApGauge { Centi = 0 };
        var core = new BossPartState
        {
            Name = "Core",
            MaxHp = 80_000,
            Hp = 80_000,
            Enmity = new EnmitySlot[1],
            StunExpires = 50_000,
        };
        var boss = new BossState
        {
            Name = "Dummy",
            Atk = 400,
            Acc = 300,
            Agi = 1,
            Ap = new ApGauge { Centi = SimConst.ReadyCenti - 100 },
            Parts = [core],
        };
        var bossPlan = new Queue<BattleAction>();
        bossPlan.Enqueue(new BattleAction(AbilityCatalog.PistonSweep, 0, -1));
        var sim = new BattleSimulator(
            [hero],
            boss,
            [new Queue<BattleAction>()],
            bossPlan,
            Rolls(hit: 0, crit: 9_999, interrupt: 9_999));
        sim.RunUntil(2);
        Assert.Contains("Boss Core is stunned and cannot Piston Sweep.", Text(sim), StringComparison.Ordinal);
        Assert.Equal(hero.MaxHp, hero.Hp);
    }

    [Fact]
    public void A_chant_fizzles_if_the_part_is_still_stunned()
    {
        var hero = AliveHero("Korrith");
        hero.Ap = new ApGauge { Centi = 0 };
        var core = new BossPartState
        {
            Name = "Core",
            MaxHp = 80_000,
            Hp = 80_000,
            Enmity = new EnmitySlot[1],
            StunExpires = 10_000,
        };
        var boss = new BossState
        {
            Name = "Dummy",
            Atk = 500,
            Acc = 400,
            Agi = 1,
            Ap = new ApGauge { Centi = 0 },
            Parts = [core],
            Casting = true,
            CastAbilityId = AbilityCatalog.OverpressureLance,
            CastPart = 0,
            CastTargetHero = 0,
            CastResolveTick = 100,
        };
        var sim = new BattleSimulator(
            [hero],
            boss,
            [new Queue<BattleAction>()],
            new Queue<BattleAction>(),
            Rolls(hit: 0, crit: 0, interrupt: 0));
        sim.RunUntil(100);
        Assert.False(sim.Boss.Casting);
        Assert.Contains("Boss Overpressure Lance fizzles. Core is stunned.", Text(sim), StringComparison.Ordinal);
        Assert.Equal(hero.MaxHp, hero.Hp);
    }

    [Fact]
    public void Execution_frame_raises_crit_damage_only_below_35_percent()
    {
        int below = FinisherDamage(34, crit: true);
        int atLine = FinisherDamage(35, crit: true);
        int above = FinisherDamage(50, crit: true);
        int belowNormal = FinisherDamage(34, crit: false);
        int aboveNormal = FinisherDamage(50, crit: false);

        Assert.True(below > atLine);
        Assert.Equal(atLine, above);
        Assert.Equal(belowNormal, aboveNormal);
        Assert.Equal(ExpectedFinisher(crit: true, frame: true), below);
        Assert.Equal(ExpectedFinisher(crit: true, frame: false), atLine);
        Assert.Equal(ExpectedFinisher(crit: false, frame: false), belowNormal);
    }

    [Fact]
    public void Execution_frame_forecast_follows_the_part_threshold()
    {
        BattleSimulator sim = CarapaceEncounter.Create(PartyPreset.Guardbreak, 1UL);
        HeroState kaelis = sim.Heroes[1];
        AbilityDef finisher = AbilityCatalog.Get(AbilityCatalog.RavelExecution);
        BossPartState core = sim.Boss.Parts[0];
        core.Hp = core.MaxHp * 34 / 100;
        string below = AbilityBrief.Format(finisher, kaelis, kaelis, sim.Boss, 0, sim.Tick, sim.Heroes);
        int framed = Formulas.WeaponCritMultiplierBp(kaelis.Weapon.WsCritDamageBp, executionFrame: true);
        Assert.Contains("Execution Frame: below 35% HP, crit damage +40% and the crit cap is 2.90×.", below, StringComparison.Ordinal);
        Assert.Contains("includes the frame", below, StringComparison.Ordinal);
        Assert.Contains(FramedHigh(kaelis, sim, framed), below, StringComparison.Ordinal);

        core.Hp = core.MaxHp * 35 / 100;
        string atLine = AbilityBrief.Format(finisher, kaelis, kaelis, sim.Boss, 0, sim.Tick, sim.Heroes);
        Assert.Contains("normal crit cap", atLine, StringComparison.Ordinal);
        Assert.DoesNotContain("includes the frame", atLine, StringComparison.Ordinal);
    }

    [Fact]
    public void Choir_aegis_absorbs_damage_without_stacking_and_then_expires()
    {
        AbilityDef aegis = AbilityCatalog.Get(AbilityCatalog.ChoirAegis);
        int pool = FixedMath.MulBp(400, aegis.MultiplierBp);
        BattleSimulator shielded = ShieldSim(withAegis: true);
        BattleSimulator bare = ShieldSim(withAegis: false);
        shielded.RunUntil(400);
        bare.RunUntil(400);
        HeroState covered = shielded.Heroes[0];
        HeroState open = bare.Heroes[0];
        Assert.Equal(pool, covered.Absorb + (open.MaxHp - open.Hp) - (covered.MaxHp - covered.Hp));
        Assert.True(covered.Hp > open.Hp);
        int saved = covered.Hp - open.Hp;
        Assert.Equal(saved, pool - covered.Absorb);
        Assert.InRange(saved, 1, pool);

        var refreshPlan = new Queue<BattleAction>();
        refreshPlan.Enqueue(new BattleAction(AbilityCatalog.ChoirAegis, -1, 0));
        refreshPlan.Enqueue(new BattleAction(AbilityCatalog.ChoirAegis, -1, 0));
        var healer = AliveHero("Seraphine");
        healer.Intel = 400;
        healer.Agi = 100;
        healer.Ap = new ApGauge { Centi = SimConst.ReadyCenti - 10_000 };
        healer.Mp = 500;
        healer.MaxMp = 500;
        var core = new BossPartState { Name = "Core", MaxHp = 50_000, Hp = 50_000, Enmity = new EnmitySlot[1] };
        var boss = new BossState
        {
            Name = "Dummy",
            Agi = 0,
            Ap = new ApGauge { Centi = 0 },
            Parts = [core],
        };
        var refresh = new BattleSimulator(
            [healer],
            boss,
            [refreshPlan],
            new Queue<BattleAction>(),
            Rolls(hit: 0, crit: 9_999, interrupt: 9_999));
        refresh.RunUntil(800);
        Assert.Equal(pool, healer.Absorb);
        Assert.Equal(500 - (aegis.MpCost * 2), healer.Mp);
        Assert.True(healer.AbsorbExpires > refresh.Tick);
        int expires = healer.AbsorbExpires;
        refresh.RunUntil(expires);
        Assert.Equal(0, healer.Absorb);
        Assert.Contains("Seraphine absorb expired.", Text(refresh), StringComparison.Ordinal);
    }

    private static string FramedHigh(HeroState actor, BattleSimulator sim, int critMultiplier)
    {
        AbilityDef ability = AbilityCatalog.Get(AbilityCatalog.RavelExecution);
        int attack = Formulas.EffectiveAttack(actor.Atk, 0, actor.Weapon.WsStr, actor.Weapon.WsDex, WeaponProperty.Piercing);
        int defense = sim.Boss.Def + sim.Boss.Parts[0].DefBonus;
        var request = new DamageRequest
        {
            Power = attack,
            MultiplierBp = ability.MultiplierBp,
            MitigationStat = defense,
            MitigationConstant = SimConst.PhysicalDrConstant,
            CritMultiplierBp = critMultiplier,
            WeaponSkillBp = actor.Weapon.WsDamageBp,
            BurstDiminishBp = SimConst.Bp,
        };
        return $"–{DamagePipeline.Resolve(request).Total} damage";
    }

    private static int FinisherDamage(int hpPercent, bool crit)
    {
        const int maxHp = 1_000_000;
        var hero = AliveHero("Kaelis");
        hero.Atk = 480;
        hero.Acc = 400;
        hero.Ap = new ApGauge { Centi = SimConst.ReadyCenti - 1_000 };
        var part = new BossPartState
        {
            Name = "Core",
            MaxHp = maxHp,
            Hp = maxHp * hpPercent / 100,
            Enmity = new EnmitySlot[1],
        };
        var boss = new BossState
        {
            Name = "Dummy",
            Def = 180,
            Eva = 0,
            Agi = 0,
            Ap = new ApGauge { Centi = 0 },
            Parts = [part],
        };
        var plan = new Queue<BattleAction>();
        plan.Enqueue(new BattleAction(AbilityCatalog.RavelExecution, 0, -1));
        var sim = new BattleSimulator(
            [hero],
            boss,
            [plan],
            new Queue<BattleAction>(),
            Rolls(hit: 0, crit: crit ? 0 : 9_999, interrupt: 9_999));
        int before = part.Hp;
        sim.RunUntil(2);
        return before - part.Hp;
    }

    private static int ExpectedFinisher(bool crit, bool frame)
    {
        int attack = Formulas.EffectiveAttack(480, 0, 0, 0, WeaponProperty.Piercing);
        var request = new DamageRequest
        {
            Power = attack,
            MultiplierBp = AbilityCatalog.Get(AbilityCatalog.RavelExecution).MultiplierBp,
            MitigationStat = 180,
            MitigationConstant = SimConst.PhysicalDrConstant,
            CritMultiplierBp = crit ? Formulas.WeaponCritMultiplierBp(0, frame) : SimConst.Bp,
            BurstDiminishBp = SimConst.Bp,
        };
        return DamagePipeline.Resolve(request).Total;
    }

    private static BattleSimulator ShieldSim(bool withAegis)
    {
        var hero = AliveHero("Seraphine");
        hero.Intel = 400;
        hero.Def = 120;
        hero.Agi = 1;
        hero.Ap = new ApGauge { Centi = SimConst.ReadyCenti - 100 };
        hero.Mp = 500;
        hero.MaxMp = 500;
        var plan = new Queue<BattleAction>();
        if (withAegis)
        {
            plan.Enqueue(new BattleAction(AbilityCatalog.ChoirAegis, -1, 0));
        }

        var core = new BossPartState { Name = "Core", MaxHp = 50_000, Hp = 50_000, Enmity = new EnmitySlot[1] };
        var boss = new BossState
        {
            Name = "Dummy",
            Atk = 420,
            Acc = 400,
            Agi = 1,
            Ap = new ApGauge { Centi = SimConst.ReadyCenti - 40_000 },
            Parts = [core],
        };
        var bossPlan = new Queue<BattleAction>();
        bossPlan.Enqueue(new BattleAction(AbilityCatalog.Attack, 0, -1));
        return new BattleSimulator(
            [hero],
            boss,
            [plan],
            bossPlan,
            Rolls(hit: 0, crit: 9_999, interrupt: 9_999));
    }

    private static Queue<BattleAction> OneBash()
    {
        var plan = new Queue<BattleAction>();
        plan.Enqueue(new BattleAction(AbilityCatalog.ShieldBash, 0, -1));
        return plan;
    }

    private static BattleSimulator BashSim(Queue<BattleAction> plan, bool casting)
    {
        var hero = AliveHero("Korrith");
        hero.Atk = 300;
        hero.Acc = 400;
        hero.Agi = 100;
        hero.Ap = new ApGauge { Centi = SimConst.ReadyCenti - 10_000 };
        var core = new BossPartState { Name = "Core", MaxHp = 200_000, Hp = 200_000, Enmity = new EnmitySlot[1] };
        var boss = new BossState
        {
            Name = "Dummy",
            Def = 100,
            Eva = 0,
            Agi = 0,
            Ap = new ApGauge { Centi = 0 },
            Parts = [core],
            Casting = casting,
            CastAbilityId = AbilityCatalog.OverpressureLance,
            CastPart = 0,
            CastTargetHero = 0,
            CastResolveTick = casting ? 8_000 : 0,
        };
        return new BattleSimulator(
            [hero],
            boss,
            [plan],
            new Queue<BattleAction>(),
            Rolls(hit: 0, crit: 9_999, interrupt: 9_999));
    }

    private static HeroState AliveHero(string name) => new()
    {
        Name = name,
        Race = RaceId.VethKari,
        MaxHp = 8_000,
        Hp = 8_000,
        MaxMp = 300,
        Mp = 300,
        Atk = 200,
        Def = 200,
        Acc = 300,
        Agi = 10,
        Ap = new ApGauge { Centi = 0 },
    };

    private static ScriptedRng Rolls(int hit, int crit, int interrupt) => new(new Dictionary<RngStream, IReadOnlyList<int>>
    {
        [RngStream.Hit] = Repeat(hit, 24),
        [RngStream.Crit] = Repeat(crit, 24),
        [RngStream.Interrupt] = Repeat(interrupt, 8),
    });

    private static int[] Repeat(int value, int count)
    {
        var rolls = new int[count];
        for (int i = 0; i < count; i++)
        {
            rolls[i] = value;
        }

        return rolls;
    }

    private static int Count(string log, string needle)
    {
        int count = 0;
        int from = 0;
        while (true)
        {
            int at = log.IndexOf(needle, from, StringComparison.Ordinal);
            if (at < 0)
            {
                return count;
            }

            count++;
            from = at + needle.Length;
        }
    }

    private static HeroState[] Copy(BattleSimulator sim)
    {
        var heroes = new HeroState[sim.Heroes.Count];
        for (int i = 0; i < heroes.Length; i++)
        {
            heroes[i] = sim.Heroes[i];
        }

        return heroes;
    }

    private static GambitView View(HeroState[] heroes, BossState boss, int actor) => new(heroes, boss, actor, 0, 0, 0);

    private static string Text(BattleSimulator sim)
    {
        var text = new StringBuilder();
        foreach (CombatEvent evt in sim.Events)
        {
            text.Append(evt.Text).Append('\n');
        }

        return text.ToString();
    }
}
