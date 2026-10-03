using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Sim;

namespace Resonance.Sim.Tests;

public class PilotGapTests
{
    [Fact]
    public void Failed_double_attack_draws_no_extra_hit_roll()
    {
        var rng = new ScriptedRng(new Dictionary<RngStream, IReadOnlyList<int>>
        {
            [RngStream.Hit] = [0],
            [RngStream.Crit] = [9_999],
            [RngStream.MultiAttack] = [5_000],
        });
        BattleSimulator sim = Weapon(RaceId.VethKari, rng, AbilityCatalog.Shear, out _);
        sim.Heroes[0].Gear = new GearMods { WsDoubleAttackBp = 1_000 };
        sim.Heroes[0].Atk = 1_000;
        sim.RunToEnd();
        Assert.True(rng.IsExhausted);
        Assert.Contains("Double Attack roll 5000", Transcript(sim), StringComparison.Ordinal);
        Assert.DoesNotContain("extra hit", Transcript(sim), StringComparison.Ordinal);
        Assert.Equal(120_000 - 1_000, sim.Boss.Parts[0].Hp);
    }

    [Fact]
    public void Double_attack_deals_half_the_pre_crit_hit()
    {
        var rng = new ScriptedRng(new Dictionary<RngStream, IReadOnlyList<int>>
        {
            [RngStream.Hit] = [0, 0],
            [RngStream.Crit] = [9_999, 9_999],
            [RngStream.MultiAttack] = [0],
        });
        BattleSimulator sim = Weapon(RaceId.VethKari, rng, AbilityCatalog.Shear, out _);
        sim.Heroes[0].Gear = new GearMods { WsDoubleAttackBp = 10_000 };
        sim.Heroes[0].Atk = 1_000;
        sim.RunToEnd();
        Assert.True(rng.IsExhausted);
        Assert.Contains("extra hit 1 500", Transcript(sim), StringComparison.Ordinal);
        Assert.Equal(120_000 - 1_500, sim.Boss.Parts[0].Hp);
        Assert.Equal(1, sim.Boss.Parts[0].Contributors);
    }

    [Fact]
    public void Saeli_cadence_refunds_1800()
    {
        BattleSimulator sim = Weapon(
            RaceId.SylvariMor,
            new ScriptedRng(new Dictionary<RngStream, IReadOnlyList<int>>
            {
                [RngStream.Hit] = [0],
                [RngStream.Crit] = [0],
                [RngStream.MultiAttack] = [9_999],
            }),
            AbilityCatalog.TalonLance,
            out _);
        sim.Heroes[0].CadenceRefundAp = SimConst.SaeliCadenceRefundAp;
        sim.Heroes[0].Gear = new GearMods { WsDoubleAttackBp = 1_000 };
        sim.RunToEnd();
        Assert.Contains("Cadence Surge refunds 1800", Transcript(sim), StringComparison.Ordinal);
    }

    [Fact]
    public void Early_window_cartography_adds_damage_and_900_ticks()
    {
        BattleSimulator early = Spell(earlyWindow: true, out BossPartState earlyCore);
        BattleSimulator late = Spell(earlyWindow: false, out BossPartState lateCore);
        early.RunUntil(1);
        late.RunUntil(1);
        Assert.Equal(1_250, DamageAt(early));
        Assert.Equal(1_000, DamageAt(late));
        Assert.Equal(100_000 + SimConst.EarlyWindowExtension, earlyCore.BurstExpires);
        Assert.Equal(100_000 + SimConst.KithExtensionTicks, lateCore.BurstExpires);
    }

    [Fact]
    public void Thurga_leaves_30_heat_and_ash_penetration_stacks()
    {
        BattleSimulator thurga = Weapon(
            RaceId.AshDravan,
            new ScriptedRng(new Dictionary<RngStream, IReadOnlyList<int>>
            {
                [RngStream.Hit] = [0],
                [RngStream.Crit] = [9_999],
                [RngStream.MultiAttack] = [9_999],
            }),
            AbilityCatalog.SeismicMaul,
            out _);
        thurga.Heroes[0].Heat = 100;
        thurga.Heroes[0].HeatAfterPrime = SimConst.ThurgaHeatRemainder;
        thurga.Heroes[0].Atk = 200;
        thurga.RunUntil(1);
        Assert.Equal(30, thurga.Heroes[0].Heat);
        Assert.Equal(thurga.Tick + SimConst.HeatDecayTicks, thurga.Heroes[0].HeatDecayTick);
        Assert.Contains("forced Liquefaction", Transcript(thurga), StringComparison.Ordinal);

        int plain = KindlingDamage(RaceId.VethKari, epen: 0);
        int racial = KindlingDamage(RaceId.AshDravan, epen: 0);
        int stacked = KindlingDamage(RaceId.AshDravan, epen: 40);
        Assert.Equal(800, plain);
        Assert.Equal(950, racial);
        Assert.Equal(1_000, stacked);
    }

    [Fact]
    public void Ash_dravan_is_immune_to_burn_and_another_hero_is_not()
    {
        BattleSimulator ash = BurnVictim(RaceId.AshDravan);
        ash.RunUntil(1);
        Assert.Equal(8_000, ash.Heroes[0].Hp);
        Assert.Contains("immune to Burn", Transcript(ash), StringComparison.Ordinal);

        BattleSimulator other = BurnVictim(RaceId.VethKari);
        other.RunUntil(1);
        Assert.Equal(7_960, other.Heroes[0].Hp);
        Assert.Contains("Burn 40", Transcript(other), StringComparison.Ordinal);
    }

    [Fact]
    public void Heat_gains_from_a_hit_then_decays_after_1000_ticks()
    {
        var hero = new HeroState
        {
            Slot = 0,
            Name = "Ash",
            Race = RaceId.AshDravan,
            MaxHp = 8_000,
            Hp = 8_000,
            MaxMp = 200,
            Mp = 200,
            Agi = 100,
            Ap = new ApGauge { Centi = 0 },
        };
        var core = new BossPartState
        {
            Name = "Core",
            MaxHp = 120_000,
            Hp = 120_000,
            Enmity = new EnmitySlot[1],
        };
        var boss = new BossState
        {
            Name = "Dummy",
            Intel = 1_000,
            Agi = 100,
            Ap = new ApGauge { Centi = SimConst.ReadyCenti - 10_000 },
            Parts = [core],
        };
        var heroPlan = new Queue<BattleAction>();
        for (int i = 0; i < 30; i++)
        {
            heroPlan.Enqueue(new BattleAction(AbilityCatalog.LatticeProvoke, 0, -1));
        }

        var bossPlan = new Queue<BattleAction>();
        bossPlan.Enqueue(new BattleAction(AbilityCatalog.Kindling, 0, -1));
        var sim = new BattleSimulator(
            [hero],
            boss,
            [heroPlan],
            bossPlan,
            new ScriptedRng(new Dictionary<RngStream, IReadOnlyList<int>>
            {
                [RngStream.Hit] = [0],
                [RngStream.Crit] = [9_999],
            }));
        sim.RunUntil(1);
        int gain = Formulas.HeatGain(1_000, 8_000, fire: true);
        Assert.Equal(gain, hero.Heat);
        Assert.True(gain > 0);
        int decayAt = hero.HeatDecayTick;
        Assert.Equal(1_001, decayAt);
        sim.RunUntil(decayAt);
        Assert.Equal(gain - SimConst.HeatDecay, hero.Heat);
        Assert.Contains("Heat decays", Transcript(sim), StringComparison.Ordinal);
    }

    [Fact]
    public void Natural_prime_adds_25_percent_detonation()
    {
        int primed = Detonation(primed: true);
        int natural = Detonation(primed: false);
        Assert.Equal(ThermalBattery.BonusDetonation(natural), primed);
        Assert.True(primed > natural);
    }

    private static int Detonation(bool primed)
    {
        BattleSimulator sim = Weapon(
            RaceId.AshDravan,
            new ScriptedRng(new Dictionary<RngStream, IReadOnlyList<int>>
            {
                [RngStream.Hit] = [0],
                [RngStream.Crit] = [9_999],
                [RngStream.MultiAttack] = [9_999],
            }),
            AbilityCatalog.CinderBrand,
            out BossPartState core);
        core.Tier = 1;
        core.Property = ChainProperty.Blunt;
        core.ChainExpires = 100_000;
        sim.Heroes[0].Atk = 1_000;
        sim.Heroes[0].Heat = primed ? 100 : 0;
        sim.Heroes[0].Gear = new GearMods { WsDoubleAttackBp = 1_000 };
        sim.RunUntil(1);
        string text = Transcript(sim);
        if (primed)
        {
            Assert.Contains("primed +25%", text, StringComparison.Ordinal);
            return NumberAfter(text, "primed +25% detonation ");
        }

        return NumberAfter(text, "Liquefaction detonation ");
    }

    private static int KindlingDamage(RaceId race, int epen)
    {
        BattleSimulator sim = Weapon(
            race,
            new ScriptedRng(new Dictionary<RngStream, IReadOnlyList<int>>
            {
                [RngStream.Hit] = [0],
                [RngStream.Crit] = [9_999],
            }),
            AbilityCatalog.Kindling,
            out _);
        sim.Heroes[0].Epen = epen;
        sim.Heroes[0].Intel = 1_000;
        sim.Boss.ResistBp[(int)ElementId.Fire] = 2_000;
        sim.RunUntil(1);
        return DamageAt(sim);
    }

    private static BattleSimulator BurnVictim(RaceId race)
    {
        BattleSimulator sim = Weapon(
            race,
            new ScriptedRng(new Dictionary<RngStream, IReadOnlyList<int>>()),
            AbilityCatalog.LatticeProvoke,
            out _);
        sim.Heroes[0].BurnPulse = 40;
        sim.Heroes[0].BurnNextTick = 1;
        sim.Heroes[0].BurnExpires = 2_000;
        return sim;
    }

    private static BattleSimulator Spell(bool earlyWindow, out BossPartState core)
    {
        BattleSimulator sim = Weapon(
            RaceId.KithLir,
            new ScriptedRng(new Dictionary<RngStream, IReadOnlyList<int>>
            {
                [RngStream.Crit] = [9_999],
            }),
            AbilityCatalog.GlacierSnap,
            out core);
        sim.Heroes[0].EarlyWindowCartography = earlyWindow;
        sim.Heroes[0].Intel = 1_000;
        core.BurstActive = true;
        core.BurstOpened = 0;
        core.BurstExpires = 100_000;
        core.BurstMask = ElementMask.Ice;
        return sim;
    }

    private static BattleSimulator Weapon(RaceId race, IRng rng, int ability, out BossPartState core)
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
            Acc = 500,
            Agi = 100,
            Intel = 1_000,
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
        plan.Enqueue(new BattleAction(ability, 0, -1));
        return new BattleSimulator([hero], boss, [plan], new Queue<BattleAction>(), rng);
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

            return NumberAfter(evt.Text, "Dmg ");
        }

        throw new Xunit.Sdk.XunitException(Transcript(sim));
    }

    private static int NumberAfter(string text, string marker)
    {
        int start = text.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new Xunit.Sdk.XunitException(text);
        }

        start += marker.Length;
        int end = start;
        while (end < text.Length && char.IsDigit(text[end]))
        {
            end++;
        }

        return int.Parse(text[start..end]);
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
