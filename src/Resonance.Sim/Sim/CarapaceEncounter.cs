using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Gambits;

namespace Resonance.Sim.Sim;

/// <summary>
/// Grey-box Carapace Engine fight. Same four heroes and the same part HP as §2.15,
/// with default gambit decks, split loadouts, and a looping boss script.
/// </summary>
public static class CarapaceEncounter
{
    public const ulong ShowcaseSeed = 20261004UL;

    public static BattleSimulator Create(ulong seed) => Create(new EncounterRng(seed));

    public static BattleSimulator Create(IRng rng)
    {
        HeroState korrith = Hero(0, "Korrith Vael-Dun", RaceId.VethKari, 9_240, 300, 273, 710, 108, 240, 220, 108, 9);
        korrith.HasBulwarkBash = true;
        korrith.Idle = new GearMods { IdleDef = 80, IdleEnmityBp = 1_000 };
        korrith.Weapon = new GearMods { WsStr = 81 };
        korrith.Kit = [AbilityCatalog.ShieldBash, AbilityCatalog.LatticeProvoke, AbilityCatalog.KeratinBastion, AbilityCatalog.SeismicMaul, AbilityCatalog.Attack];
        korrith.Deck = Deck(
            """
            1 IF Boss [CastResolvesIn < 700] -> USE [Shield Bash]
            2 IF Boss [TankMargin < 2000] -> USE [Lattice Provoke]
            3 IF NOT Self [HasBuff: Keratin Bastion] -> USE [Keratin Bastion]
            4 IF Boss.WeaponArm [MyEnmityRank > 1] -> USE [Lattice Provoke] ON WeaponArm
            5 IF Field [Always] -> USE [Attack]
            """,
            bulwark: true);

        HeroState mirrim = Hero(1, "Mirrim Ash-Pounce", RaceId.SylvariMor, 4_480, 225, 415, 270, 110, 180, 308, 243, 18);
        mirrim.Idle = new GearMods { IdleEva = 20 };
        mirrim.Weapon = new GearMods { WsDex = 124, WsDoubleAttackBp = 1_000, WsDamageBp = 4_000 };
        mirrim.Kit = [AbilityCatalog.TalonLance, AbilityCatalog.FrostfangPounce, AbilityCatalog.TimelineStalk, AbilityCatalog.Attack];
        mirrim.Deck = Deck(
            """
            1 IF Target [Window: Piercing] AND Self [MP >= Cost: Frostfang Pounce] -> USE [Frostfang Pounce]
            2 IF NOT Self [HasBuff: Timeline Stalk] AND Self [MP% >= 50] -> USE [Timeline Stalk]
            3 IF Field [Always] -> USE [Talon Lance]
            """);

        HeroState zeph = Hero(2, "Zeph Tri-Lumen", RaceId.KithLir, 3_710, 540, 170, 161, 606, 260, 305, 200, 13);
        zeph.EarlyWindowCartography = true;
        zeph.Idle = new GearMods { IdleDef = 40 };
        zeph.FastCast = new GearMods { FastCastBp = 3_000 };
        zeph.MidCast = new GearMods { MidInt = 60, MidMbdBp = 4_000, MidCritDamageBp = 2_000 };
        zeph.Kit = [AbilityCatalog.BlizzardII, AbilityCatalog.PyreLattice, AbilityCatalog.HexLance, AbilityCatalog.Attack];
        zeph.Deck = Deck(
            """
            1 IF Target [Resonance: Ice] AND Self [MP >= Cost: Blizzard II] -> CAST [Blizzard II]
            2 IF Target [Resonance: Fire] AND Self [MP >= Cost: Pyre Lattice] -> CAST [Pyre Lattice]
            3 IF Target [Resonance: Lightning] AND Self [MP >= Cost: Hex Lance] -> CAST [Hex Lance]
            4 IF Self [MP% >= 85] AND Self [MP >= Cost: Blizzard II] -> CAST [Blizzard II]
            5 IF Field [Always] -> USE [Attack]
            """);

        HeroState seraphine = Hero(3, "Seraphine Vol-Ivory", RaceId.AethelBorn, 4_750, 990, 162, 252, 373, 330, 200, 150, 13);
        seraphine.Idle = new GearMods { IdleMeva = 30 };
        seraphine.FastCast = new GearMods { FastCastBp = 3_000 };
        seraphine.MidCast = new GearMods { MidHealingPotencyBp = 5_000, MidInt = 40 };
        seraphine.Kit = [AbilityCatalog.CureCascade, AbilityCatalog.CircuitBenediction, AbilityCatalog.PhaseSanctuary, AbilityCatalog.Attack];
        seraphine.Deck = Deck(
            """
            1 IF Ally(Lowest HP%) [HP% < 55] AND Self [MP >= Cost: Cure Cascade] -> CAST [Cure Cascade] ON Ally(Lowest HP%)
            2 IF NOT Ally(Zeph Tri-Lumen) [HasBuff: Concentration] AND Self [MP% >= 40] AND Self [MP >= Cost: Circuit Benediction] -> CAST [Circuit Benediction] ON Ally(Zeph Tri-Lumen)
            3 IF Boss [Casting] AND Self [MP% >= 50] AND Self [MP >= Cost: Phase Sanctuary] -> CAST [Phase Sanctuary]
            4 IF Ally(Lowest HP%) [HP% < 75] AND Self [MP >= Cost: Cure Cascade] -> CAST [Cure Cascade] ON Ally(Lowest HP%)
            5 IF Field [Always] -> USE [Attack]
            """);

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
            ResistBp = Resist(),
            Parts =
            [
                Part("Core", 120_000, heroes.Length),
                Part("Weapon Arm", 45_000, heroes.Length),
                Part("Shield", 30_000, heroes.Length),
            ],
        };

        var script = new BossScript
        {
            PhaseHpBp = 3_500,
            EnrageTick = 80_000,
            HardEnrageTick = 120_000,
            FrenzyBonusBp = 2_200,
            Phase1 = [AbilityCatalog.PistonSweep, AbilityCatalog.OverpressureLance, AbilityCatalog.KilnVent],
            Phase1Part = [BattleSimulator.WeaponArm, BattleSimulator.Core, BattleSimulator.Core],
            Phase2 = [AbilityCatalog.KilnVent, AbilityCatalog.OverpressureLance, AbilityCatalog.PistonSweep, AbilityCatalog.KilnVent],
            Phase2Part = [BattleSimulator.Core, BattleSimulator.Core, BattleSimulator.WeaponArm, BattleSimulator.Core],
        };

        var plans = new Queue<BattleAction>[heroes.Length];
        for (int i = 0; i < plans.Length; i++)
        {
            plans[i] = new Queue<BattleAction>();
        }

        return new BattleSimulator(heroes, boss, plans, new Queue<BattleAction>(), rng, script, scoreOutcome: true);
    }

    private static GambitProgram Deck(string text, bool bulwark = false)
    {
        GambitCompileResult result = GambitCompiler.Compile(text, GambitCompileOptions.Standard(bulwark));
        if (!result.Ok)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors));
        }

        return result.Program;
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
        int agi)
    {
        return new HeroState
        {
            Slot = slot,
            IsAnchor = slot == 0,
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
            SplitLoadouts = true,
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

    private static int[] Resist()
    {
        var resist = new int[8];
        resist[(int)ElementId.Ice] = 1_000;
        resist[(int)ElementId.Fire] = 3_000;
        return resist;
    }
}
