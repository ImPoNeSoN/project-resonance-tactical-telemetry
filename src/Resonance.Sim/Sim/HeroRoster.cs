using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Gambits;

namespace Resonance.Sim.Sim;

public enum HeroRole
{
    Tank,
    Healer,
    Dps,
}

public readonly struct PartyCheck
{
    public PartyCheck(int count, bool distinct, bool hasTank, bool hasHealer)
    {
        Count = count;
        Distinct = distinct;
        HasTank = hasTank;
        HasHealer = hasHealer;
    }

    public int Count { get; }

    public bool Distinct { get; }

    public bool HasTank { get; }

    public bool HasHealer { get; }

    public bool CanStart => Count == 4 && Distinct;

    public string Status
    {
        get
        {
            if (!Distinct)
            {
                return "Party must be four distinct heroes.";
            }

            if (Count != 4)
            {
                return $"Select 4 heroes. {Count} selected.";
            }

            string text = "Party of 4.";
            if (!HasTank)
            {
                text += " No tank.";
            }

            if (!HasHealer)
            {
                text += " No healer.";
            }

            return text;
        }
    }
}

/// <summary>
/// The six heroes a VS-2 party is drawn from. Mirrim Ash-Pounce is the one left off:
/// she is the second Sylvari-Mor physical linker. Saeli keeps Slashing, Wind, and a
/// Piercing opener. Kaelis keeps Darkness.
/// </summary>
public static class HeroRoster
{
    public const int Count = 6;
    public const int Korrith = 0;
    public const int Saeli = 1;
    public const int Kaelis = 2;
    public const int Zeph = 3;
    public const int Aurel = 4;
    public const int Seraphine = 5;

    public static int[] Ids { get; } = [Korrith, Saeli, Kaelis, Zeph, Aurel, Seraphine];

    public static bool Contains(int id) => id >= 0 && id < Count;

    public static string Name(int id) => id switch
    {
        Saeli => "Saeli Thorn-Vesper",
        Kaelis => "Kaelis Moon-Ravel",
        Zeph => "Zeph Tri-Lumen",
        Aurel => "Aurel Nine-Vesper",
        Seraphine => "Seraphine Vol-Ivory",
        _ => "Korrith Vael-Dun",
    };

    public static string RaceName(int id) => id switch
    {
        Saeli or Kaelis => "Sylvari-Mor",
        Zeph => "Kith-Lir",
        Aurel or Seraphine => "Aethel-Born",
        _ => "Veth-Kari",
    };

    public static HeroRole Role(int id) => id switch
    {
        Seraphine => HeroRole.Healer,
        Korrith => HeroRole.Tank,
        _ => HeroRole.Dps,
    };

    public static string RoleName(int id) => Role(id) switch
    {
        HeroRole.Tank => "Tank",
        HeroRole.Healer => "Healer",
        _ => "DPS",
    };

    public static string KitSummary(int id) => id switch
    {
        Saeli => "Rend Pulse, Needle Flicker, Wind Talon Arc",
        Kaelis => "Crescent Sever, Umbral Pierce, Ravel Execution",
        Zeph => "Blizzard II, Pyre Lattice, Hex Lance",
        Aurel => "Photon Sermon, Gale Quanta, Solar Filament",
        Seraphine => "Choir Aegis, Cure Cascade, Circuit Benediction, Phase Sanctuary",
        _ => "Shield Bash, Lattice Provoke, Keratin Bastion, Seismic Maul",
    };

    public static int[] PresetIds(int preset) => preset switch
    {
        PartyPreset.ShatterChoir => [Korrith, Saeli, Aurel, Seraphine],
        PartyPreset.Guardbreak => [Korrith, Kaelis, Zeph, Seraphine],
        _ => [Korrith, Saeli, Zeph, Seraphine],
    };

    public static int MatchPreset(IReadOnlyList<int> ids)
    {
        if (ids.Count != 4)
        {
            return -1;
        }

        int[] sorted = Sorted(ids);
        for (int preset = 0; preset < PartyPreset.Count; preset++)
        {
            int[] known = Sorted(PresetIds(preset));
            if (sorted[0] == known[0] && sorted[1] == known[1] && sorted[2] == known[2] && sorted[3] == known[3])
            {
                return preset;
            }
        }

        return -1;
    }

    public static string Label(IReadOnlyList<int> ids)
    {
        int match = MatchPreset(ids);
        if (match >= 0)
        {
            return PartyPreset.Name(match);
        }

        var text = new System.Text.StringBuilder();
        for (int i = 0; i < ids.Count; i++)
        {
            if (i > 0)
            {
                text.Append(", ");
            }

            string name = Name(ids[i]);
            int space = name.IndexOf(' ');
            text.Append(space < 0 ? name : name[..space]);
        }

        return text.ToString();
    }

    public static PartyCheck Check(IReadOnlyList<int> ids)
    {
        bool distinct = true;
        bool tank = false;
        bool healer = false;
        var seen = new HashSet<int>();
        for (int i = 0; i < ids.Count; i++)
        {
            if (!Contains(ids[i]) || !seen.Add(ids[i]))
            {
                distinct = false;
                continue;
            }

            if (Role(ids[i]) == HeroRole.Tank)
            {
                tank = true;
            }

            if (Role(ids[i]) == HeroRole.Healer)
            {
                healer = true;
            }
        }

        return new PartyCheck(ids.Count, distinct, tank, healer);
    }

    public static int[][] Combinations()
    {
        var rows = new List<int[]>();
        for (int a = 0; a < Count; a++)
        {
            for (int b = a + 1; b < Count; b++)
            {
                for (int c = b + 1; c < Count; c++)
                {
                    for (int d = c + 1; d < Count; d++)
                    {
                        rows.Add([Ids[a], Ids[b], Ids[c], Ids[d]]);
                    }
                }
            }
        }

        return rows.ToArray();
    }

    public static HeroState[] PresetParty(int preset)
    {
        HeroState[] heroes = CustomParty(PresetIds(preset));
        switch (preset)
        {
            case PartyPreset.ShatterChoir:
                Find(heroes, Korrith).Deck = Deck(KorrithShatter, bulwark: true);
                Find(heroes, Seraphine).Deck = Deck(SeraphineFor("Aurel Nine-Vesper"));
                break;
            case PartyPreset.Guardbreak:
                Find(heroes, Korrith).Deck = Deck(KorrithGuardbreak, bulwark: true);
                Find(heroes, Zeph).Deck = Deck(ZephGuardbreak);
                Find(heroes, Seraphine).Deck = Deck(SeraphineFor("Zeph Tri-Lumen"));
                break;
            default:
                Find(heroes, Saeli).Deck = Deck(SaeliIce);
                Find(heroes, Zeph).Deck = Deck(ZephIce);
                Find(heroes, Seraphine).Deck = Deck(SeraphineFor("Zeph Tri-Lumen"));
                break;
        }

        return heroes;
    }

    public static HeroState[] CustomParty(IReadOnlyList<int> ids)
    {
        int[] ordered = Sorted(ids);
        var heroes = new HeroState[ordered.Length];
        for (int i = 0; i < ordered.Length; i++)
        {
            heroes[i] = Create(ordered[i], i);
        }

        return heroes;
    }

    public static HeroState Create(int id, int slot)
    {
        HeroState hero = id switch
        {
            Saeli => SaeliBody(slot),
            Kaelis => KaelisBody(slot),
            Zeph => ZephBody(slot),
            Aurel => AurelBody(slot),
            Seraphine => SeraphineBody(slot),
            _ => KorrithBody(slot),
        };
        hero.Deck = DefaultDeck(id);
        return hero;
    }

    private static GambitProgram DefaultDeck(int id) => id switch
    {
        Saeli => Deck(SaeliShatter),
        Kaelis => Deck(KaelisGuardbreak),
        Zeph => Deck(ZephDefault),
        Aurel => Deck(AurelShatter),
        Seraphine => Deck(SeraphineDefault),
        _ => Deck(KorrithIce, bulwark: true),
    };

    private static HeroState Find(HeroState[] heroes, int id)
    {
        string name = Name(id);
        for (int i = 0; i < heroes.Length; i++)
        {
            if (heroes[i].Name == name)
            {
                return heroes[i];
            }
        }

        throw new InvalidOperationException(name);
    }

    private static int[] Sorted(IReadOnlyList<int> ids)
    {
        var copy = new int[ids.Count];
        for (int i = 0; i < ids.Count; i++)
        {
            copy[i] = ids[i];
        }

        for (int i = 1; i < copy.Length; i++)
        {
            int value = copy[i];
            int j = i;
            while (j > 0 && copy[j - 1] > value)
            {
                copy[j] = copy[j - 1];
                j--;
            }

            copy[j] = value;
        }

        return copy;
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

    private const string KorrithIce = """
        1 IF Target [CastResolvesIn < 200] AND NOT Target [StunImmune] AND NOT Target [Stunned] -> USE [Shield Bash]
        2 IF Boss [AP >= 9000] AND NOT Boss [Casting] AND NOT Target [StunImmune] -> USE [Shield Bash]
        3 IF Boss [TankMargin < 2000] -> USE [Lattice Provoke]
        4 IF NOT Self [HasBuff: Keratin Bastion] -> USE [Keratin Bastion]
        5 IF Boss.WeaponArm [MyEnmityRank > 1] -> USE [Lattice Provoke] ON WeaponArm
        6 IF Field [Always] -> USE [Attack]
        """;

    private const string KorrithGuardbreak = """
        1 IF Target [CastResolvesIn < 200] AND NOT Target [StunImmune] AND NOT Target [Stunned] -> USE [Shield Bash]
        2 IF Boss [AP >= 9000] AND NOT Boss [Casting] AND NOT Target [StunImmune] -> USE [Shield Bash]
        3 IF Boss [TankMargin < 2000] -> USE [Lattice Provoke]
        4 IF NOT Self [HasBuff: Keratin Bastion] -> USE [Keratin Bastion]
        5 IF Boss.Core [Window: Slashing] -> USE [Seismic Maul] ON Core
        6 IF Boss.Core [WindowLeft == 0] -> USE [Seismic Maul] ON Core
        """;

    private const string KorrithShatter = """
        1 IF Target [CastResolvesIn < 80] AND NOT Target [StunImmune] AND NOT Target [Stunned] -> USE [Shield Bash]
        2 IF Boss [AP >= 9300] AND NOT Boss [Casting] AND NOT Target [StunImmune] -> USE [Shield Bash]
        3 IF Boss [TankMargin < 2000] -> USE [Lattice Provoke]
        4 IF NOT Self [HasBuff: Keratin Bastion] -> USE [Keratin Bastion]
        5 IF Boss.Core [Window: Slashing] AND Boss [HP% >= 64] -> USE [Seismic Maul] ON Core
        6 IF Boss.Core [WindowLeft == 0] -> USE [Seismic Maul] ON Core
        """;

    private const string SaeliShatter = """
        1 IF Boss.Core [Window: Fragmentation] AND Boss [HP% < 60] -> USE [Attack] ON Core
        2 IF Boss.Core [Window: Slashing] AND Boss [HP% < 64] AND Self [MP >= Cost: Wind Talon Arc] -> USE [Wind Talon Arc] ON Core
        3 IF Boss.Core [Window: Blunt] -> USE [Rend Pulse] ON Core
        4 IF Field [Always] -> USE [Rend Pulse] ON Core
        """;

    private const string SaeliIce = """
        1 IF Boss [HP% < 28] -> USE [Rend Pulse] ON Core
        2 IF Field [Always] -> USE [Needle Flicker] ON Core
        """;

    private const string ZephIce = """
        1 IF Boss [HP% < 40] AND Self [MP% >= 60] AND NOT Target [Resonance: Lightning] -> CAST [Hex Lance] ON Core
        2 IF Boss.Core [Window: Piercing] AND Boss [HP% >= 80] AND Self [MP% >= 90] -> CAST [Blizzard II] ON Core
        3 IF Field [Always] -> USE [Attack] ON Core
        """;

    private const string KaelisGuardbreak = """
        1 IF Boss.Core [Window: Blunt] AND Boss [HP% < 44] AND Boss [HP% >= 36] -> USE [Attack] ON Core
        2 IF Boss.Core [Window: Blunt] AND Boss [HP% >= 44] AND Self [MP >= Cost: Umbral Pierce] -> USE [Umbral Pierce] ON Core
        3 IF Boss.Core [Window: Distortion] AND Boss [HP% < 34] AND Boss [HP% >= 28] -> USE [Attack] ON Core
        4 IF Field [Always] -> USE [Crescent Sever] ON Core
        """;

    private const string ZephGuardbreak = """
        1 IF Boss.Core [Window: Blunt] AND Boss [HP% < 44] AND Self [MP >= Cost: Pyre Lattice] -> CAST [Pyre Lattice] ON Core
        2 IF Boss.Core [Window: Liquefaction] AND Self [MP% >= 40] AND Self [MP >= Cost: Pyre Lattice] -> CAST [Pyre Lattice] ON Core
        3 IF Boss.Core [Window: Distortion] AND Boss [HP% < 34] AND Self [MP >= Cost: Blizzard II] -> CAST [Blizzard II] ON Core
        4 IF Field [Always] -> USE [Attack] ON Core
        """;

    private const string ZephDefault = """
        1 IF Boss.Core [Window: Piercing] AND Self [MP >= Cost: Hex Lance] -> CAST [Hex Lance] ON Core
        2 IF Target [Resonance: Lightning] AND Self [MP >= Cost: Hex Lance] -> CAST [Hex Lance] ON Core
        3 IF Target [Resonance: Ice] AND Self [MP >= Cost: Blizzard II] -> CAST [Blizzard II] ON Core
        4 IF Boss.Core [Window: Liquefaction] AND Self [MP% >= 40] AND Self [MP >= Cost: Pyre Lattice] -> CAST [Pyre Lattice] ON Core
        5 IF Self [MP% >= 85] AND Self [MP >= Cost: Blizzard II] -> CAST [Blizzard II] ON Core
        6 IF Field [Always] -> USE [Attack] ON Core
        """;

    private const string AurelShatter = """
        1 IF Boss.Core [Window: Fragmentation] AND Boss [HP% < 60] AND Self [MP >= Cost: Photon Sermon] -> CAST [Photon Sermon] ON Core
        2 IF Boss.Core [Window: Wind] AND Self [MP >= Cost: Photon Sermon] -> CAST [Photon Sermon] ON Core
        3 IF Boss.Core [Window: Slashing] AND Boss [HP% < 64] AND Self [MP% >= 50] -> CAST [Gale Quanta] ON Core
        4 IF Boss.Core [Resonance: Wind] AND Self [MP% >= 50] AND Self [MP >= Cost: Gale Quanta] -> CAST [Gale Quanta] ON Core
        5 IF Field [Always] -> USE [Attack] ON Core
        """;

    private static string SeraphineFor(string focus) => $"""
        1 IF Ally(Lowest HP%) [HP% < 55] AND Self [MP >= Cost: Cure Cascade] -> CAST [Cure Cascade] ON Ally(Lowest HP%)
        2 IF Ally(Lowest HP%) [HP% < 70] AND NOT Ally(Lowest HP%) [HasBuff: Choir Aegis] AND Self [MP >= Cost: Choir Aegis] -> CAST [Choir Aegis] ON Ally(Lowest HP%)
        3 IF NOT Ally(Highest Threat) [HasBuff: Choir Aegis] AND Self [MP% >= 35] AND Self [MP >= Cost: Choir Aegis] -> CAST [Choir Aegis] ON Ally(Highest Threat)
        4 IF NOT Ally({focus}) [HasBuff: Concentration] AND Self [MP% >= 40] AND Self [MP >= Cost: Circuit Benediction] -> CAST [Circuit Benediction] ON Ally({focus})
        5 IF Boss [Casting] AND Self [MP% >= 50] AND Self [MP >= Cost: Phase Sanctuary] -> CAST [Phase Sanctuary]
        6 IF Ally(Lowest HP%) [HP% < 75] AND Self [MP >= Cost: Cure Cascade] -> CAST [Cure Cascade] ON Ally(Lowest HP%)
        """;

    private const string SeraphineDefault = """
        1 IF Ally(Lowest HP%) [HP% < 55] AND Self [MP >= Cost: Cure Cascade] -> CAST [Cure Cascade] ON Ally(Lowest HP%)
        2 IF Ally(Lowest HP%) [HP% < 70] AND NOT Ally(Lowest HP%) [HasBuff: Choir Aegis] AND Self [MP >= Cost: Choir Aegis] -> CAST [Choir Aegis] ON Ally(Lowest HP%)
        3 IF NOT Ally(Highest Threat) [HasBuff: Choir Aegis] AND Self [MP% >= 35] AND Self [MP >= Cost: Choir Aegis] -> CAST [Choir Aegis] ON Ally(Highest Threat)
        4 IF NOT Ally(Lowest MP%) [HasBuff: Concentration] AND Self [MP% >= 40] AND Self [MP >= Cost: Circuit Benediction] -> CAST [Circuit Benediction] ON Ally(Lowest MP%)
        5 IF Boss [Casting] AND Self [MP% >= 50] AND Self [MP >= Cost: Phase Sanctuary] -> CAST [Phase Sanctuary]
        6 IF Ally(Lowest HP%) [HP% < 75] AND Self [MP >= Cost: Cure Cascade] -> CAST [Cure Cascade] ON Ally(Lowest HP%)
        """;

    private static HeroState KorrithBody(int slot)
    {
        HeroState korrith = Body(slot, "Korrith Vael-Dun", RaceId.VethKari, 9_240, 300, 273, 710, 108, 240, 220, 108, 9);
        korrith.HasBulwarkBash = true;
        korrith.Idle = new GearMods { IdleDef = 80, IdleEnmityBp = 1_000 };
        korrith.Weapon = new GearMods { WsStr = 81 };
        korrith.Kit = [AbilityCatalog.ShieldBash, AbilityCatalog.LatticeProvoke, AbilityCatalog.KeratinBastion, AbilityCatalog.SeismicMaul, AbilityCatalog.Attack];
        return korrith;
    }

    private static HeroState SaeliBody(int slot)
    {
        HeroState saeli = Body(slot, "Saeli Thorn-Vesper", RaceId.SylvariMor, 4_480, 225, 399, 270, 110, 180, 323, 234, 18);
        saeli.CadenceRefundAp = SimConst.SaeliCadenceRefundAp;
        saeli.Idle = new GearMods { IdleEva = 20 };
        saeli.Weapon = new GearMods { WsStr = 110, WsDex = 70, WsDoubleAttackBp = 1_300, WsDamageBp = 3_900 };
        saeli.Kit = [AbilityCatalog.RendPulse, AbilityCatalog.NeedleFlicker, AbilityCatalog.WindTalonArc, AbilityCatalog.Attack];
        return saeli;
    }

    private static HeroState KaelisBody(int slot)
    {
        HeroState kaelis = Body(slot, "Kaelis Moon-Ravel", RaceId.SylvariMor, 3_680, 360, 485, 207, 180, 200, 319, 247, 16);
        kaelis.Idle = new GearMods { IdleEva = 20 };
        kaelis.Weapon = new GearMods { WsStr = 140, WsDex = 40, WsDoubleAttackBp = 1_000, WsDamageBp = 4_000 };
        kaelis.Kit = [AbilityCatalog.CrescentSever, AbilityCatalog.UmbralPierce, AbilityCatalog.RavelExecution, AbilityCatalog.Attack];
        return kaelis;
    }

    private static HeroState ZephBody(int slot)
    {
        HeroState zeph = Body(slot, "Zeph Tri-Lumen", RaceId.KithLir, 3_710, 540, 170, 161, 606, 260, 305, 200, 13);
        zeph.EarlyWindowCartography = true;
        zeph.Idle = new GearMods { IdleDef = 40 };
        zeph.FastCast = new GearMods { FastCastBp = 3_000 };
        zeph.MidCast = new GearMods { MidInt = 60, MidMbdBp = 4_000, MidCritDamageBp = 2_000 };
        zeph.Kit = [AbilityCatalog.BlizzardII, AbilityCatalog.PyreLattice, AbilityCatalog.HexLance, AbilityCatalog.Attack];
        return zeph;
    }

    private static HeroState AurelBody(int slot)
    {
        HeroState aurel = Body(slot, "Aurel Nine-Vesper", RaceId.AethelBorn, 4_370, 440, 180, 207, 508, 220, 290, 190, 14);
        aurel.Idle = new GearMods { IdleDef = 20 };
        aurel.FastCast = new GearMods { FastCastBp = 3_000 };
        aurel.MidCast = new GearMods { MidInt = 45, MidMbdBp = 3_500, MidCritDamageBp = 1_800 };
        aurel.Kit = [AbilityCatalog.PhotonSermon, AbilityCatalog.SolarFilament, AbilityCatalog.GaleQuanta, AbilityCatalog.Attack];
        return aurel;
    }

    private static HeroState SeraphineBody(int slot)
    {
        HeroState seraphine = Body(slot, "Seraphine Vol-Ivory", RaceId.AethelBorn, 4_750, 990, 162, 252, 373, 330, 200, 150, 13);
        seraphine.Idle = new GearMods { IdleMeva = 30 };
        seraphine.FastCast = new GearMods { FastCastBp = 3_000 };
        seraphine.MidCast = new GearMods { MidHealingPotencyBp = 5_000, MidInt = 40 };
        seraphine.Kit = [AbilityCatalog.ChoirAegis, AbilityCatalog.CureCascade, AbilityCatalog.CircuitBenediction, AbilityCatalog.PhaseSanctuary, AbilityCatalog.Attack];
        return seraphine;
    }

    private static HeroState Body(
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
}
