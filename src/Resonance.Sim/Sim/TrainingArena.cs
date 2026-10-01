using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;

namespace Resonance.Sim.Sim;

/// <summary>
/// The 02 §2.15 training-arena party, boss, loadouts, scripted actions, and published d10000 rolls.
/// Rolls the log does not print (boss crits, later double-attack checks, the last Frostfang) are
/// failing or non-crit values so the printed results stay intact.
/// </summary>
public static class TrainingArena
{
    public static (BattleSimulator Sim, ScriptedRng Rng) CreateGolden()
    {
        var rng = new ScriptedRng(GoldenRolls());
        return (Create(rng), rng);
    }

    public static BattleSimulator Create(IRng rng)
    {
        var korrith = Hero(0, "Korrith Vael-Dun", RaceId.VethKari, 9_240, 300, 273, 710, 108, 240, 220, 108, 9, new GearMods
        {
            IdleDef = 80,
            IdleEnmityBp = 1_000,
            WsStr = 40,
        });
        var mirrim = Hero(1, "Mirrim Ash-Pounce", RaceId.SylvariMor, 4_480, 225, 415, 270, 110, 180, 308, 243, 18, new GearMods
        {
            IdleEva = 20,
            WsDex = 50,
            WsDoubleAttackBp = 1_000,
        });
        var zeph = Hero(2, "Zeph Tri-Lumen", RaceId.KithLir, 3_710, 540, 170, 161, 606, 260, 305, 200, 13, new GearMods
        {
            IdleDef = 40,
            FastCastBp = 3_000,
            MidInt = 60,
            MidMbdBp = 2_000,
            MidCritDamageBp = 2_000,
        });
        zeph.EarlyWindowCartography = true;
        var seraphine = Hero(3, "Seraphine Vol-Ivory", RaceId.AethelBorn, 4_750, 990, 162, 252, 373, 330, 200, 150, 13, new GearMods
        {
            IdleMeva = 30,
            FastCastBp = 3_000,
        });

        HeroState[] heroes = [korrith, mirrim, zeph, seraphine];
        var boss = new BossState
        {
            Name = "Carapace Engine Mk. II",
            Atk = 520,
            Def = 420,
            Intel = 380,
            Meva = 300,
            Acc = 300,
            Eva = 140,
            Agi = 12,
            Concentration = 40,
            Ap = new ApGauge { Centi = ApGauge.InitialCenti(12, ambushed: false) },
            Parts =
            [
                Part("Core", 120_000, heroes.Length),
                Part("Weapon Arm", 45_000, heroes.Length),
                Part("Shield", 30_000, heroes.Length),
            ],
        };

        var plans = new Queue<BattleAction>[heroes.Length];
        for (int i = 0; i < plans.Length; i++)
        {
            plans[i] = new Queue<BattleAction>();
        }

        plans[0].Enqueue(Act(AbilityCatalog.LatticeProvoke, BattleSimulator.Core));
        plans[0].Enqueue(Act(AbilityCatalog.KeratinBastion, BattleSimulator.Core));
        plans[0].Enqueue(Act(AbilityCatalog.LatticeProvoke, BattleSimulator.Core));
        plans[0].Enqueue(Act(AbilityCatalog.SeismicMaul, BattleSimulator.Core));

        plans[1].Enqueue(Act(AbilityCatalog.TalonLance, BattleSimulator.Core));
        plans[1].Enqueue(Act(AbilityCatalog.FrostfangPounce, BattleSimulator.Core));
        plans[1].Enqueue(Act(AbilityCatalog.TimelineStalk, -1));
        plans[1].Enqueue(Act(AbilityCatalog.TalonLance, BattleSimulator.WeaponArm));
        plans[1].Enqueue(Act(AbilityCatalog.FrostfangPounce, BattleSimulator.WeaponArm));
        plans[1].Enqueue(Act(AbilityCatalog.TimelineStalk, -1));
        plans[1].Enqueue(Act(AbilityCatalog.TalonLance, BattleSimulator.Core));
        plans[1].Enqueue(Act(AbilityCatalog.FrostfangPounce, BattleSimulator.Core));

        plans[2].Enqueue(Act(AbilityCatalog.BlizzardII, BattleSimulator.Core));
        plans[2].Enqueue(Act(AbilityCatalog.BlizzardII, BattleSimulator.Core));

        plans[3].Enqueue(new BattleAction(AbilityCatalog.CircuitBenediction, -1, 2));
        plans[3].Enqueue(new BattleAction(AbilityCatalog.CureCascade, -1, 1));
        plans[3].Enqueue(Act(AbilityCatalog.PhaseSanctuary, -1));

        var bossPlan = new Queue<BattleAction>();
        bossPlan.Enqueue(Act(AbilityCatalog.PistonSweep, BattleSimulator.WeaponArm));
        bossPlan.Enqueue(Act(AbilityCatalog.OverpressureLance, BattleSimulator.Core));

        return new BattleSimulator(heroes, boss, plans, bossPlan, rng);
    }

    public static Dictionary<RngStream, IReadOnlyList<int>> GoldenRolls()
    {
        return new Dictionary<RngStream, IReadOnlyList<int>>
        {
            [RngStream.Hit] =
            [
                3_121, // Mirrim Talon Lance
                8_870, // Piston Sweep
                1_450, // Frostfang Pounce
                5_530, // Talon Lance (arm)
                2_006, // Frostfang (arm)
                4_410, // Seismic Maul
                3_888, // Talon Lance (core)
                4_200, // Frostfang (core); roll not printed, must hit
                2_210, // Overpressure Lance
            ],
            [RngStream.Crit] =
            [
                6_204, // Talon Lance
                9_999, // Piston Sweep crit omitted by the log; 6.85% fails
                9_120, // Frostfang
                4_102, // Blizzard II crit
                1_502, // Talon Lance crit
                8_800, // Frostfang
                3_380, // Seismic Maul
                6_410, // Talon Lance
                7_310, // second Blizzard II
                9_000, // last Frostfang crit omitted; must miss the 17.9% check
                9_999, // Overpressure Lance crit omitted; 5% fails
            ],
            [RngStream.MultiAttack] =
            [
                7_713, // Talon Lance double attack
                9_999, // later weapon skills, omitted, must fail 10%
                9_999,
                9_999,
                9_999,
                9_999,
            ],
        };
    }

    private static HeroState Hero(
        int slot,
        string name,
        RaceId race,
        int hp,
        int mp,
        int atk,
        int def,
        int intel,
        int meva,
        int acc,
        int eva,
        int agi,
        GearMods gear)
    {
        return new HeroState
        {
            Slot = slot,
            Name = name,
            Race = race,
            MaxHp = hp,
            Hp = hp,
            MaxMp = mp,
            Mp = mp,
            Atk = atk,
            Def = def,
            Intel = intel,
            Meva = meva,
            Acc = acc,
            Eva = eva,
            Agi = agi,
            Gear = gear,
            Ap = new ApGauge { Centi = ApGauge.InitialCenti(agi, ambushed: false) },
        };
    }

    private static BossPartState Part(string name, int hp, int heroes)
    {
        return new BossPartState
        {
            Name = name,
            MaxHp = hp,
            Hp = hp,
            Enmity = new EnmitySlot[heroes],
        };
    }

    private static BattleAction Act(int ability, int part) => new(ability, part, -1);
}
