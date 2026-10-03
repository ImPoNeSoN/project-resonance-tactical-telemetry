using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Sim;

namespace Resonance.Sim.Tests;

public class ChainVarietyTests
{
    [Fact]
    public void Parts_publish_different_resistances()
    {
        BattleSimulator sim = CarapaceEncounter.Create(PartyPreset.ShatterChoir, 1UL);
        BossPartState core = sim.Boss.Parts[BattleSimulator.Core];
        BossPartState arm = sim.Boss.Parts[BattleSimulator.WeaponArm];
        BossPartState shield = sim.Boss.Parts[BattleSimulator.Shield];
        Assert.True(shield.ResistBp![(int)ElementId.Ice] > core.ResistBp![(int)ElementId.Ice]);
        Assert.True(shield.ResistBp[(int)ElementId.Wind] < 0);
        Assert.True(arm.ResistBp![(int)ElementId.Darkness] < 0);
        Assert.True(core.ResistBp[(int)ElementId.Fire] < 0);
        Assert.True(shield.DefBonus > 0);
        Assert.Contains(SimConst.KilnGuardName, sim.Boss.Beneficial);
        Assert.Contains("Fragmentation", shield.Pressure, StringComparison.Ordinal);
    }

    [Fact]
    public void Fragmentation_counts_one_detonation()
    {
        BattleSimulator sim = Script(AbilityCatalog.Shear, AbilityCatalog.GaleCut);
        sim.RunToEnd();
        Assert.Equal(1, sim.Detonations(ResonanceId.Fragmentation));
        Assert.Equal(0, sim.Level3Detonations);
    }

    private static BattleSimulator Script(params int[] abilities)
    {
        var hero = new HeroState
        {
            Slot = 0,
            Name = "Tester",
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
                [RngStream.Hit] = [0, 0, 0],
                [RngStream.Crit] = [9_999, 9_999, 9_999],
            }));
    }
}
