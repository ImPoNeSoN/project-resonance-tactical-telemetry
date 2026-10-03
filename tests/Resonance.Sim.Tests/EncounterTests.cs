using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Sim;

namespace Resonance.Sim.Tests;

public class EncounterTests
{
    [Fact]
    public void Conduction_shocks_and_delays_ap()
    {
        BattleSimulator sim = Script(RaceId.VethKari, out _, AbilityCatalog.TalonLance, AbilityCatalog.HexLance);
        sim.Heroes[0].Mp = 500;
        sim.RunToEnd();
        Assert.Contains("Conduction delays", Text(sim), StringComparison.Ordinal);
        Assert.Contains("Shock pulse", Text(sim), StringComparison.Ordinal);
    }

    [Fact]
    public void Tectonic_shear_lowers_meva()
    {
        BattleSimulator sim = Script(RaceId.VethKari, out _, AbilityCatalog.Shear, AbilityCatalog.EarthBolt);
        sim.RunToEnd();
        Assert.Equal(SimConst.TectonicMevaDownBp, sim.Boss.MevaDownBp);
        Assert.Contains("MEVA Down 2000bp", Text(sim), StringComparison.Ordinal);
    }

    [Fact]
    public void Radiance_regens_the_party_without_adding_ve()
    {
        BattleSimulator sim = Script(RaceId.VethKari, out BossPartState core, AbilityCatalog.Kindling, AbilityCatalog.PrismRay);
        sim.Heroes[0].Hp = 4_000;
        int ve = core.Enmity[0].Ve;
        sim.RunToEnd();
        Assert.Contains("Radiance regen", Text(sim), StringComparison.Ordinal);
        Assert.Contains("restored", Text(sim), StringComparison.Ordinal);
        Assert.True(sim.Heroes[0].Hp > 4_000);
        Assert.Equal(ve, core.Enmity[0].Ve);
    }

    [Fact]
    public void Umbral_zero_sets_ap_back_and_lowers_meva()
    {
        BattleSimulator sim = Script(RaceId.KithLir, out BossPartState core, AbilityCatalog.GloomBolt);
        core.Tier = 2;
        core.Resonance = ResonanceId.Induration;
        core.ChainExpires = 100_000;
        sim.Boss.Ap.Centi = 500_000;
        sim.RunUntil(1);
        Assert.Contains("Umbral Zero", Text(sim), StringComparison.Ordinal);
        Assert.Equal(SimConst.UmbralMevaDownBp, sim.Boss.MevaDownBp);
        Assert.True(sim.Boss.Ap.Centi < 500_000);
    }

    [Fact]
    public void Tempest_crown_interrupts_and_grants_ap()
    {
        var caster = new HeroState
        {
            Slot = 0,
            IsAnchor = true,
            Name = "Caster",
            Race = RaceId.VethKari,
            MaxHp = 8_000,
            Hp = 8_000,
            MaxMp = 500,
            Mp = 500,
            Atk = 400,
            Intel = 400,
            Acc = 500,
            Agi = 100,
            Ap = new ApGauge { Centi = SimConst.ReadyCenti - 10_000 },
        };
        var ally = new HeroState
        {
            Slot = 1,
            Name = "Ally",
            Race = RaceId.AethelBorn,
            MaxHp = 8_000,
            Hp = 8_000,
            Agi = 0,
            Ap = new ApGauge { Centi = 100_000 },
        };
        var core = new BossPartState
        {
            Name = "Core",
            MaxHp = 120_000,
            Hp = 120_000,
            Tier = 2,
            Resonance = ResonanceId.Conduction,
            ChainExpires = 100_000,
            Enmity = new EnmitySlot[2],
        };
        var boss = new BossState
        {
            Name = "Dummy",
            Agi = 1,
            Ap = new ApGauge { Centi = 0 },
            Parts = [core],
            Casting = true,
            CastAbilityId = AbilityCatalog.OverpressureLance,
            CastPart = 0,
            CastResolveTick = 50_000,
        };
        var plan = new Queue<BattleAction>();
        plan.Enqueue(new BattleAction(AbilityCatalog.GaleCut, 0, -1));
        var sim = new BattleSimulator(
            [caster, ally],
            boss,
            [plan, new Queue<BattleAction>()],
            new Queue<BattleAction>(),
            new ScriptedRng(new Dictionary<RngStream, IReadOnlyList<int>>
            {
                [RngStream.Hit] = [0],
                [RngStream.Crit] = [9_999],
            }));
        sim.RunUntil(1);
        Assert.False(sim.Boss.Casting);
        Assert.Contains("Tempest Crown interrupts", Text(sim), StringComparison.Ordinal);
        Assert.Equal(100_000 + (SimConst.TempestGrantAp * SimConst.CentiPerAp), ally.Ap.Centi);
        Assert.Contains("Ally Tempest Crown +2000 AP", Text(sim), StringComparison.Ordinal);
    }

    [Fact]
    public void Kiln_vent_burns_a_hero_and_a_dot_can_end_the_fight()
    {
        var hero = new HeroState
        {
            Slot = 0,
            Name = "Korrith Vael-Dun",
            Race = RaceId.VethKari,
            MaxHp = 8_000,
            Hp = 8_000,
            Agi = 1,
            Ap = new ApGauge { Centi = 0 },
        };
        var core = new BossPartState { Name = "Core", MaxHp = 120_000, Hp = 120_000, Enmity = new EnmitySlot[1] };
        var boss = new BossState
        {
            Name = "Carapace Engine Mk. II",
            Intel = 400,
            Agi = 100,
            Ap = new ApGauge { Centi = SimConst.ReadyCenti - 10_000 },
            Parts = [core, new BossPartState { Name = "Weapon Arm", MaxHp = 1_000, Hp = 1_000, Enmity = new EnmitySlot[1] }],
        };
        var bossPlan = new Queue<BattleAction>();
        bossPlan.Enqueue(new BattleAction(AbilityCatalog.KilnVent, 0, -1));
        var burned = new BattleSimulator(
            [hero],
            boss,
            [new Queue<BattleAction>()],
            bossPlan,
            new ScriptedRng(new Dictionary<RngStream, IReadOnlyList<int>>
            {
                [RngStream.Hit] = [0],
                [RngStream.Crit] = [9_999],
            }),
            scoreOutcome: true);
        burned.RunUntil(900);
        Assert.True(hero.BurnPulse > 0);
        Assert.Contains("Burn pulse", Text(burned), StringComparison.Ordinal);

        core.Hp = 20;
        core.BurnPulse = 50;
        core.BurnNextTick = burned.Tick + 500;
        core.BurnExpires = burned.Tick + 4_000;
        burned.RunToEnd();
        Assert.Equal(FightOutcome.Victory, burned.Outcome);
        Assert.Equal(0, core.Hp);
    }

    [Fact]
    public void Weapon_skill_loadout_is_worn_only_for_the_weapon_skill()
    {
        BattleSimulator split = Script(RaceId.VethKari, out _, AbilityCatalog.Shear);
        split.Heroes[0].SplitLoadouts = true;
        split.Heroes[0].Atk = 200;
        split.Heroes[0].Weapon = new GearMods { WsStr = 80 };
        split.RunUntil(1);
        int withStr = Damage(split);

        BattleSimulator bare = Script(RaceId.VethKari, out _, AbilityCatalog.Shear);
        bare.Heroes[0].SplitLoadouts = true;
        bare.Heroes[0].Atk = 200;
        bare.RunUntil(1);
        Assert.True(withStr > Damage(bare));
    }

    private static BattleSimulator Script(RaceId race, out BossPartState core, params int[] abilities)
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
            Atk = 400,
            Intel = 400,
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
        var plan = new Queue<BattleAction>();
        foreach (int ability in abilities)
        {
            plan.Enqueue(new BattleAction(ability, 0, -1));
        }

        return new BattleSimulator(
            [hero],
            boss,
            [plan],
            new Queue<BattleAction>(),
            new ScriptedRng(new Dictionary<RngStream, IReadOnlyList<int>>
            {
                [RngStream.Hit] = Repeat(0, 12),
                [RngStream.Crit] = Repeat(9_999, 12),
                [RngStream.MultiAttack] = Repeat(9_999, 12),
            }));
    }

    private static int[] Repeat(int value, int count)
    {
        var list = new int[count];
        Array.Fill(list, value);
        return list;
    }

    private static int Damage(BattleSimulator sim)
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

        throw new Xunit.Sdk.XunitException(Text(sim));
    }

    private static string Text(BattleSimulator sim)
    {
        var lines = new List<string>();
        foreach (CombatEvent evt in sim.Events)
        {
            lines.Add(evt.Text);
        }

        return string.Join('\n', lines);
    }
}
