using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Sim;

namespace Resonance.Sim.Tests;

public class ResonanceChainTests
{
    [Fact]
    public void Matching_burst_spell_advances_an_open_chain()
    {
        BattleSimulator sim = Harness(
            RaceId.KithLir,
            rolls(crit: 9_999),
            out BossPartState core,
            AbilityCatalog.GlacierSnap);
        core.Tier = 1;
        core.Property = ChainProperty.Piercing;
        core.ChainExpires = 100_000;
        OpenIceBurst(core, bucket: 5_000);

        sim.RunToEnd();

        Assert.Equal(0, core.Tier);
        Assert.Equal(ResonanceId.None, core.Resonance);
        Assert.Equal(1_500, DamageAt(sim));
        Assert.Contains("Induration detonation 750", Transcript(sim), StringComparison.Ordinal);
        Assert.Contains("opens no resonance window", Transcript(sim), StringComparison.Ordinal);
        Assert.Equal(120_000 - 1_500 - 750, core.Hp);
        Assert.Equal(0, core.BurstOpened);
        Assert.Equal(100_600, core.BurstExpires);
        Assert.Equal(SimConst.IndurationSlowBp, sim.Boss.SlowBp);
    }

    [Fact]
    public void Matching_burst_with_no_matrix_route_restarts_the_chain()
    {
        BattleSimulator sim = Harness(
            RaceId.KithLir,
            rolls(crit: 9_999),
            out BossPartState core,
            AbilityCatalog.GlacierSnap);
        core.Tier = 2;
        core.Resonance = ResonanceId.Induration;
        core.ChainExpires = 100_000;
        OpenIceBurst(core, bucket: 5_000);
        int opened = core.BurstOpened;

        sim.RunToEnd();

        Assert.Equal(1, core.Tier);
        Assert.Equal(ChainProperty.Ice, core.Property);
        Assert.Equal(1_500, DamageAt(sim));
        Assert.Equal(120_000 - 1_500, core.Hp);
        Assert.Equal(opened, core.BurstOpened);
        Assert.Contains("chain restarts L1(Ice)", Transcript(sim), StringComparison.Ordinal);
    }

    [Fact]
    public void Liquefaction_burn_pulses_eight_times_from_its_own_start()
    {
        var actions = new List<int> { AbilityCatalog.Kindling };
        for (int i = 0; i < 120; i++)
        {
            actions.Add(AbilityCatalog.LatticeProvoke);
        }

        BattleSimulator sim = Harness(
            RaceId.VethKari,
            rolls(hit: 0, crit: 9_999),
            out BossPartState core,
            actions.ToArray());
        core.Tier = 1;
        core.Property = ChainProperty.Blunt;
        core.ChainExpires = 100_000;

        sim.RunUntil(1);
        int opened = sim.Tick;
        Assert.Equal(350, core.BurnPulse);
        Assert.Equal(opened + 500, core.BurnNextTick);
        Assert.Equal(opened + 4_000, core.BurnExpires);

        int pulses = 0;
        sim.RunToEnd();
        foreach (CombatEvent evt in sim.Events)
        {
            if (evt.Text.Contains("Burn 350", StringComparison.Ordinal))
            {
                pulses++;
            }
        }

        Assert.Equal(8, pulses);
    }

    [Fact]
    public void Fragmentation_shatters_defense_and_distortion_purges_two_buffs()
    {
        BattleSimulator sim = Harness(
            RaceId.VethKari,
            rolls(hit: 0, crit: 9_999),
            out BossPartState core,
            AbilityCatalog.GaleCut);
        sim.Boss.Def = 500;
        sim.Boss.Beneficial.Add("oldest");
        sim.Boss.Beneficial.Add("middle");
        sim.Boss.Beneficial.Add("newest");
        core.Tier = 1;
        core.Property = ChainProperty.Slashing;
        core.ChainExpires = 100_000;

        sim.RunToEnd();

        Assert.Equal(ResonanceId.Fragmentation, core.Resonance);
        Assert.Equal(2_500, sim.Boss.ShatterBp);
        Assert.Equal(375, sim.Boss.Def * (10_000 - sim.Boss.ShatterBp) / 10_000);

        var purge = Harness(
            RaceId.VethKari,
            rolls(hit: 0, crit: 9_999),
            out BossPartState gloom,
            AbilityCatalog.GloomBolt);
        gloom.Tier = 1;
        gloom.Property = ChainProperty.Blunt;
        gloom.ChainExpires = 100_000;
        purge.Boss.Beneficial.Add("oldest");
        purge.Boss.Beneficial.Add("middle");
        purge.Boss.Beneficial.Add("newest");
        purge.RunToEnd();
        Assert.Equal(["oldest"], purge.Boss.Beneficial);
        Assert.Equal(ResonanceId.Distortion, gloom.Resonance);
    }

    [Fact]
    public void Solar_apex_moves_volatile_enmity_onto_the_anchor()
    {
        BattleSimulator sim = TwoHeroes(out BossPartState core, out HeroState anchor, out HeroState other);
        core.Tier = 2;
        core.Resonance = ResonanceId.Fragmentation;
        core.ChainExpires = 100_000;
        core.Enmity[0].NormalVe = 400;
        core.Enmity[0].HeavyVe = 100;
        core.Enmity[1].NormalVe = 250;
        anchor.IsAnchor = true;

        sim.RunToEnd();

        Assert.Equal(0, core.Tier);
        Assert.Equal(750, core.Enmity[0].Ve);
        Assert.Equal(0, core.Enmity[0].HeavyVe);
        Assert.Equal(0, core.Enmity[1].Ve);
        _ = other;
    }

    [Fact]
    public void Primed_ash_dravan_forces_liquefaction_when_the_matrix_does_not()
    {
        BattleSimulator sim = Harness(
            RaceId.AshDravan,
            rolls(hit: 0, crit: 9_999, multi: 9_999),
            out BossPartState core,
            AbilityCatalog.SeismicMaul);
        sim.Heroes[0].Heat = 100;
        sim.Heroes[0].Atk = 200;

        sim.RunToEnd();

        Assert.Equal(0, sim.Heroes[0].Heat);
        Assert.Equal(ResonanceId.Liquefaction, core.Resonance);
        Assert.True(core.BurnPulse > 0);
        Assert.Contains("forced Liquefaction", Transcript(sim), StringComparison.Ordinal);
    }

    [Fact]
    public void Sylvari_cadence_refunds_on_a_crit_and_on_a_physical_link()
    {
        BattleSimulator crit = Harness(
            RaceId.SylvariMor,
            rolls(hit: 0, crit: 0, multi: 9_999),
            out _,
            AbilityCatalog.TalonLance);
        crit.RunToEnd();
        Assert.Contains("Cadence Surge refunds 1200", Transcript(crit), StringComparison.Ordinal);

        BattleSimulator link = Harness(
            RaceId.SylvariMor,
            rolls(hit: 0, crit: 9_999, multi: 9_999),
            out BossPartState core,
            AbilityCatalog.FrostfangPounce);
        core.Tier = 1;
        core.Property = ChainProperty.Piercing;
        core.ChainExpires = 100_000;
        link.RunToEnd();
        Assert.Equal(ResonanceId.Induration, core.Resonance);
        Assert.Contains("Cadence Surge refunds 1200", Transcript(link), StringComparison.Ordinal);
    }

    [Fact]
    public void Burst_closers_do_not_refresh_solar_apex_inside_one_window()
    {
        var queue = new List<int> { AbilityCatalog.Shear, AbilityCatalog.TalonLance, AbilityCatalog.PrismRay };
        for (int i = 0; i < 8; i++)
        {
            queue.Add(AbilityCatalog.GaleCut);
            queue.Add(AbilityCatalog.PrismRay);
            queue.Add(AbilityCatalog.Shear);
        }

        var script = new Dictionary<RngStream, IReadOnlyList<int>>
        {
            [RngStream.Hit] = Repeat(0, 40),
            [RngStream.Crit] = Repeat(9_999, 40),
            [RngStream.MultiAttack] = Repeat(9_999, 40),
        };
        BattleSimulator sim = Harness(RaceId.VethKari, script, out _, queue.ToArray());
        sim.Heroes[0].Agi = 40;
        sim.Heroes[0].Ap.Centi = ApGauge.InitialCenti(40, ambushed: false);
        sim.RunToEnd();

        var solarTicks = new List<int>();
        foreach (CombatEvent evt in sim.Events)
        {
            if (evt.Text.Contains("SolarApex true detonation", StringComparison.Ordinal))
            {
                solarTicks.Add(evt.Tick);
            }
        }

        // The same script used to refresh Solar inside one window (>= 8, span > 2,700).
        // A burst closer no longer opens the next window, so each later apex is a new
        // L1 rebuild after the previous 1,500-tick window has expired.
        Assert.Equal(new[] { 600, 2_600, 4_850 }, solarTicks);
        Assert.Equal(2_000, solarTicks[1] - solarTicks[0]);
        Assert.Equal(2_250, solarTicks[2] - solarTicks[1]);
    }

    private static IReadOnlyList<int> Repeat(int value, int count)
    {
        var list = new int[count];
        Array.Fill(list, value);
        return list;
    }

    private static Dictionary<RngStream, IReadOnlyList<int>> rolls(int hit = -1, int crit = -1, int multi = -1)
    {
        var map = new Dictionary<RngStream, IReadOnlyList<int>>();
        if (hit >= 0)
        {
            map[RngStream.Hit] = [hit];
        }

        if (crit >= 0)
        {
            map[RngStream.Crit] = [crit];
        }

        if (multi >= 0)
        {
            map[RngStream.MultiAttack] = [multi];
        }

        return map;
    }

    private static void OpenIceBurst(BossPartState part, int bucket)
    {
        part.BurstActive = true;
        part.BurstOpened = 0;
        part.BurstExpires = 100_000;
        part.BurstMask = ElementMask.Ice;
        part.BurstBucketBp = bucket;
        part.BurstsLanded = 0;
    }

    private static BattleSimulator Harness(RaceId race, Dictionary<RngStream, IReadOnlyList<int>> script, out BossPartState core, params int[] abilities)
    {
        var hero = new HeroState
        {
            Slot = 0,
            IsAnchor = true,
            Name = "Tester",
            Race = race,
            MaxHp = 8_000,
            Hp = 8_000,
            MaxMp = 500,
            Mp = 500,
            Atk = 0,
            Intel = 1_000,
            Acc = 500,
            Agi = 100,
            Ap = new ApGauge { Centi = SimConst.ReadyCenti - 10_000 },
        };
        core = new BossPartState
        {
            Name = "Core",
            MaxHp = 120_000,
            Hp = 120_000,
            Enmity = new EnmitySlot[1],
        };
        var boss = new BossState
        {
            Name = "Dummy",
            Agi = 1,
            Ap = new ApGauge { Centi = 0 },
            Parts = [core],
        };
        var plans = new Queue<BattleAction>[1];
        plans[0] = new Queue<BattleAction>();
        foreach (int ability in abilities)
        {
            plans[0].Enqueue(new BattleAction(ability, 0, -1));
        }

        return new BattleSimulator(new[] { hero }, boss, plans, new Queue<BattleAction>(), new ScriptedRng(script));
    }

    private static BattleSimulator TwoHeroes(out BossPartState core, out HeroState anchor, out HeroState other)
    {
        anchor = new HeroState
        {
            Slot = 0,
            IsAnchor = true,
            Name = "Anchor",
            Race = RaceId.VethKari,
            MaxHp = 5_000,
            Hp = 5_000,
            MaxMp = 100,
            Mp = 100,
            Intel = 200,
            Acc = 400,
            Agi = 100,
            Ap = new ApGauge { Centi = 0 },
        };
        other = new HeroState
        {
            Slot = 1,
            Name = "Other",
            Race = RaceId.KithLir,
            MaxHp = 4_000,
            Hp = 4_000,
            MaxMp = 200,
            Mp = 200,
            Intel = 1_000,
            Acc = 400,
            Agi = 100,
            Ap = new ApGauge { Centi = SimConst.ReadyCenti - 10_000 },
        };
        core = new BossPartState
        {
            Name = "Core",
            MaxHp = 120_000,
            Hp = 120_000,
            Enmity = new EnmitySlot[2],
        };
        var boss = new BossState
        {
            Name = "Dummy",
            Agi = 1,
            Ap = new ApGauge { Centi = 0 },
            Parts = [core],
        };
        var plans = new Queue<BattleAction>[]
        {
            new Queue<BattleAction>(),
            new Queue<BattleAction>(),
        };
        plans[1].Enqueue(new BattleAction(AbilityCatalog.PrismRay, 0, -1));
        return new BattleSimulator(
            new[] { anchor, other },
            boss,
            plans,
            new Queue<BattleAction>(),
            new ScriptedRng(rolls(hit: 0, crit: 9_999)));
    }

    private static int DamageAt(BattleSimulator sim)
    {
        foreach (CombatEvent evt in sim.Events)
        {
            int marker = evt.Text.IndexOf("Dmg ", StringComparison.Ordinal);
            if (marker < 0)
            {
                continue;
            }

            int start = marker + 4;
            int end = start;
            while (end < evt.Text.Length && char.IsDigit(evt.Text[end]))
            {
                end++;
            }

            return int.Parse(evt.Text[start..end]);
        }

        throw new Xunit.Sdk.XunitException(Transcript(sim));
    }

    private static string Transcript(BattleSimulator sim)
    {
        var lines = new List<string>();
        foreach (CombatEvent evt in sim.Events)
        {
            lines.Add(evt.Text);
        }

        return string.Join('\n', lines);
    }
}
