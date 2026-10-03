using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Gambits;

namespace Resonance.Sim.Sim;

public static class PartyPreset
{
    public const int IceLattice = 0;
    public const int ShatterChoir = 1;
    public const int Guardbreak = 2;
    public const int Count = 3;

    public static string Name(int preset) => preset switch
    {
        ShatterChoir => "Shatter Choir",
        Guardbreak => "Guardbreak",
        _ => "Ice Lattice",
    };

    public static string Summary(int preset) => preset switch
    {
        ShatterChoir => "Korrith, Saeli, Aurel, Seraphine. At 64% Core HP or higher, Blunt into Slashing distorts the Core and purges Kiln Guard. Below 64%, Slashing into Wind shatters DEF. Under 60%, Light on that Fragmentation is a Solar Apex.",
        Guardbreak => "Korrith, Kaelis, Zeph, Seraphine. Blunt into Darkness purges Kiln Guard. From 36% to 44% Core HP, Blunt into Fire burns the Core. From 28% to 34%, Ice on an open Distortion is an Umbral Zero.",
        _ => "Korrith, Mirrim, Zeph, Seraphine. At 55% Core HP or higher, Piercing into Ice indurates the Core and Blizzard spends that burst. Below 55%, Talon Lance keeps Piercing open and Hex Lance conducts it.",
    };
}

/// <summary>
/// Grey-box Carapace Engine fight. Three default parties, one boss, part resists that
/// reward different chain routes.
/// </summary>
public static class CarapaceEncounter
{
    public const ulong ShowcaseSeed = 20261004UL;

    public static BattleSimulator Create(ulong seed) => Create(PartyPreset.IceLattice, new EncounterRng(seed));

    public static BattleSimulator Create(int preset, ulong seed) => Create(preset, new EncounterRng(seed));

    public static BattleSimulator Create(IRng rng) => Create(PartyPreset.IceLattice, rng);

    public static BattleSimulator Create(int preset, IRng rng)
    {
        HeroState[] heroes = preset switch
        {
            PartyPreset.ShatterChoir => ShatterChoir(),
            PartyPreset.Guardbreak => Guardbreak(),
            _ => IceLattice(),
        };

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
            ResistBp = SharedResist(),
            Beneficial = [SimConst.KilnGuardName],
            Parts =
            [
                Part("Core", 120_000, heroes.Length, CoreResist(), 0,
                    "Frenzy Plating raises DEF and MEVA. Liquefaction burn and a Solar Apex VE reset cut through it."),
                Part("Weapon Arm", 45_000, heroes.Length, ArmResist(), 0,
                    "Weak to Darkness. Kiln Guard adds boss physical damage until Distortion purges it."),
                Part("Shield", 30_000, heroes.Length, ShieldResist(), 280,
                    "Resists Ice. Weak to Wind. High DEF, so Fragmentation's shatter is the way in."),
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

    private static HeroState[] IceLattice()
    {
        HeroState korrith = Tank();
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
            1 IF Boss.Core [Window: Piercing] AND Boss [HP% >= 55] AND Self [MP >= Cost: Frostfang Pounce] -> USE [Frostfang Pounce] ON Core
            2 IF NOT Self [HasBuff: Timeline Stalk] AND Self [MP% >= 50] -> USE [Timeline Stalk]
            3 IF Field [Always] -> USE [Talon Lance] ON Core
            """);

        HeroState zeph = Zeph();
        zeph.Deck = Deck(
            """
            1 IF Boss.Core [Window: Piercing] AND Boss [HP% < 55] AND Self [MP >= Cost: Hex Lance] -> CAST [Hex Lance] ON Core
            2 IF Target [Resonance: Lightning] AND Self [MP >= Cost: Hex Lance] -> CAST [Hex Lance] ON Core
            3 IF Target [Resonance: Ice] AND Self [MP >= Cost: Blizzard II] -> CAST [Blizzard II] ON Core
            4 IF Self [MP% >= 85] AND Boss [HP% >= 55] AND Self [MP >= Cost: Blizzard II] -> CAST [Blizzard II] ON Core
            5 IF Field [Always] -> USE [Attack] ON Core
            """);

        return [korrith, mirrim, zeph, Support("Zeph Tri-Lumen")];
    }

    private static HeroState[] ShatterChoir()
    {
        HeroState korrith = Tank();
        korrith.Deck = Deck(
            """
            1 IF Boss [CastResolvesIn < 700] -> USE [Shield Bash]
            2 IF Boss [TankMargin < 2000] -> USE [Lattice Provoke]
            3 IF NOT Self [HasBuff: Keratin Bastion] -> USE [Keratin Bastion]
            4 IF Boss.Core [Window: Slashing] AND Boss [HP% >= 64] -> USE [Seismic Maul] ON Core
            5 IF Boss.Core [WindowLeft == 0] -> USE [Seismic Maul] ON Core
            6 IF Field [Always] -> USE [Attack] ON Core
            """,
            bulwark: true);

        HeroState saeli = Hero(1, "Saeli Thorn-Vesper", RaceId.SylvariMor, 4_480, 225, 399, 270, 110, 180, 323, 234, 18);
        saeli.CadenceRefundAp = SimConst.SaeliCadenceRefundAp;
        saeli.Idle = new GearMods { IdleEva = 20 };
        saeli.Weapon = new GearMods { WsStr = 110, WsDex = 70, WsDoubleAttackBp = 1_300, WsDamageBp = 3_900 };
        saeli.Kit = [AbilityCatalog.RendPulse, AbilityCatalog.NeedleFlicker, AbilityCatalog.WindTalonArc, AbilityCatalog.Attack];
        saeli.Deck = Deck(
            """
            1 IF Boss.Core [Window: Fragmentation] AND Boss [HP% < 60] -> USE [Attack] ON Core
            2 IF Boss.Core [Window: Slashing] AND Boss [HP% < 64] AND Self [MP >= Cost: Wind Talon Arc] -> USE [Wind Talon Arc] ON Core
            3 IF Boss.Core [Window: Blunt] -> USE [Rend Pulse] ON Core
            4 IF Field [Always] -> USE [Rend Pulse] ON Core
            """);

        HeroState aurel = Hero(2, "Aurel Nine-Vesper", RaceId.AethelBorn, 4_370, 440, 180, 207, 508, 220, 290, 190, 14);
        aurel.Idle = new GearMods { IdleDef = 20 };
        aurel.FastCast = new GearMods { FastCastBp = 3_000 };
        aurel.MidCast = new GearMods { MidInt = 45, MidMbdBp = 3_500, MidCritDamageBp = 1_800 };
        aurel.Kit = [AbilityCatalog.PhotonSermon, AbilityCatalog.SolarFilament, AbilityCatalog.GaleQuanta, AbilityCatalog.Attack];
        aurel.Deck = Deck(
            """
            1 IF Boss.Core [Window: Fragmentation] AND Boss [HP% < 60] AND Self [MP >= Cost: Photon Sermon] -> CAST [Photon Sermon] ON Core
            2 IF Boss.Core [Window: Wind] AND Self [MP >= Cost: Photon Sermon] -> CAST [Photon Sermon] ON Core
            3 IF Boss.Core [Window: Slashing] AND Boss [HP% < 64] AND Self [MP% >= 50] -> CAST [Gale Quanta] ON Core
            4 IF Boss.Core [Resonance: Wind] AND Self [MP% >= 50] AND Self [MP >= Cost: Gale Quanta] -> CAST [Gale Quanta] ON Core
            5 IF Field [Always] -> USE [Attack] ON Core
            """);

        return [korrith, saeli, aurel, Support("Aurel Nine-Vesper")];
    }

    private static HeroState[] Guardbreak()
    {
        HeroState korrith = Tank();
        korrith.Deck = Deck(
            """
            1 IF Boss [CastResolvesIn < 700] -> USE [Shield Bash]
            2 IF Boss [TankMargin < 2000] -> USE [Lattice Provoke]
            3 IF NOT Self [HasBuff: Keratin Bastion] -> USE [Keratin Bastion]
            4 IF Boss.Core [Window: Slashing] -> USE [Seismic Maul] ON Core
            5 IF Boss.Core [WindowLeft == 0] -> USE [Seismic Maul] ON Core
            6 IF Field [Always] -> USE [Attack] ON Core
            """,
            bulwark: true);

        HeroState kaelis = Hero(1, "Kaelis Moon-Ravel", RaceId.SylvariMor, 3_680, 360, 485, 207, 180, 200, 319, 247, 16);
        kaelis.Idle = new GearMods { IdleEva = 20 };
        kaelis.Weapon = new GearMods { WsStr = 140, WsDex = 40, WsDoubleAttackBp = 1_000, WsDamageBp = 4_000 };
        kaelis.Kit = [AbilityCatalog.CrescentSever, AbilityCatalog.UmbralPierce, AbilityCatalog.RavelExecution, AbilityCatalog.Attack];
        kaelis.Deck = Deck(
            """
            1 IF Boss.Core [Window: Blunt] AND Boss [HP% < 44] AND Boss [HP% >= 36] -> USE [Attack] ON Core
            2 IF Boss.Core [Window: Blunt] AND Boss [HP% >= 44] AND Self [MP >= Cost: Umbral Pierce] -> USE [Umbral Pierce] ON Core
            3 IF Boss.Core [Window: Distortion] AND Boss [HP% < 34] AND Boss [HP% >= 28] -> USE [Attack] ON Core
            4 IF Field [Always] -> USE [Crescent Sever] ON Core
            """);

        HeroState zeph = Zeph();
        zeph.Deck = Deck(
            """
            1 IF Boss.Core [Window: Blunt] AND Boss [HP% < 44] AND Self [MP >= Cost: Pyre Lattice] -> CAST [Pyre Lattice] ON Core
            2 IF Boss.Core [Window: Liquefaction] AND Self [MP% >= 40] AND Self [MP >= Cost: Pyre Lattice] -> CAST [Pyre Lattice] ON Core
            3 IF Boss.Core [Window: Distortion] AND Boss [HP% < 34] AND Self [MP >= Cost: Blizzard II] -> CAST [Blizzard II] ON Core
            4 IF Field [Always] -> USE [Attack] ON Core
            """);

        return [korrith, kaelis, zeph, Support("Zeph Tri-Lumen")];
    }

    private static HeroState Tank()
    {
        HeroState korrith = Hero(0, "Korrith Vael-Dun", RaceId.VethKari, 9_240, 300, 273, 710, 108, 240, 220, 108, 9);
        korrith.HasBulwarkBash = true;
        korrith.Idle = new GearMods { IdleDef = 80, IdleEnmityBp = 1_000 };
        korrith.Weapon = new GearMods { WsStr = 81 };
        korrith.Kit = [AbilityCatalog.ShieldBash, AbilityCatalog.LatticeProvoke, AbilityCatalog.KeratinBastion, AbilityCatalog.SeismicMaul, AbilityCatalog.Attack];
        return korrith;
    }

    private static HeroState Zeph()
    {
        HeroState zeph = Hero(2, "Zeph Tri-Lumen", RaceId.KithLir, 3_710, 540, 170, 161, 606, 260, 305, 200, 13);
        zeph.EarlyWindowCartography = true;
        zeph.Idle = new GearMods { IdleDef = 40 };
        zeph.FastCast = new GearMods { FastCastBp = 3_000 };
        zeph.MidCast = new GearMods { MidInt = 60, MidMbdBp = 4_000, MidCritDamageBp = 2_000 };
        zeph.Kit = [AbilityCatalog.BlizzardII, AbilityCatalog.PyreLattice, AbilityCatalog.HexLance, AbilityCatalog.Attack];
        return zeph;
    }

    private static HeroState Support(string focus)
    {
        HeroState seraphine = Hero(3, "Seraphine Vol-Ivory", RaceId.AethelBorn, 4_750, 990, 162, 252, 373, 330, 200, 150, 13);
        seraphine.Idle = new GearMods { IdleMeva = 30 };
        seraphine.FastCast = new GearMods { FastCastBp = 3_000 };
        seraphine.MidCast = new GearMods { MidHealingPotencyBp = 5_000, MidInt = 40 };
        seraphine.Kit = [AbilityCatalog.ChoirAegis, AbilityCatalog.CureCascade, AbilityCatalog.CircuitBenediction, AbilityCatalog.PhaseSanctuary, AbilityCatalog.Attack];
        seraphine.Deck = Deck(
            $"""
            1 IF Ally(Lowest HP%) [HP% < 55] AND Self [MP >= Cost: Cure Cascade] -> CAST [Cure Cascade] ON Ally(Lowest HP%)
            2 IF NOT Ally({focus}) [HasBuff: Concentration] AND Self [MP% >= 40] AND Self [MP >= Cost: Circuit Benediction] -> CAST [Circuit Benediction] ON Ally({focus})
            3 IF Boss [Casting] AND Self [MP% >= 50] AND Self [MP >= Cost: Phase Sanctuary] -> CAST [Phase Sanctuary]
            4 IF Ally(Lowest HP%) [HP% < 75] AND Self [MP >= Cost: Cure Cascade] -> CAST [Cure Cascade] ON Ally(Lowest HP%)
            5 IF Field [Always] -> USE [Attack]
            """);
        return seraphine;
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

    private static BossPartState Part(string name, int hp, int heroes, int[] resist, int defBonus, string pressure)
    {
        return new BossPartState
        {
            Name = name,
            MaxHp = hp,
            Hp = hp,
            Enmity = new EnmitySlot[heroes],
            ResistBp = resist,
            DefBonus = defBonus,
            Pressure = pressure,
        };
    }

    private static int[] SharedResist()
    {
        var resist = new int[8];
        resist[(int)ElementId.Ice] = 1_000;
        resist[(int)ElementId.Fire] = 3_000;
        return resist;
    }

    private static int[] CoreResist()
    {
        var resist = new int[8];
        resist[(int)ElementId.Ice] = 1_000;
        resist[(int)ElementId.Fire] = -1_000;
        resist[(int)ElementId.Light] = -1_000;
        return resist;
    }

    private static int[] ArmResist()
    {
        var resist = new int[8];
        resist[(int)ElementId.Darkness] = -2_500;
        resist[(int)ElementId.Ice] = 1_500;
        resist[(int)ElementId.Fire] = 1_000;
        return resist;
    }

    private static int[] ShieldResist()
    {
        var resist = new int[8];
        resist[(int)ElementId.Ice] = 4_000;
        resist[(int)ElementId.Fire] = 2_000;
        resist[(int)ElementId.Wind] = -2_500;
        resist[(int)ElementId.Darkness] = 1_000;
        return resist;
    }
}
