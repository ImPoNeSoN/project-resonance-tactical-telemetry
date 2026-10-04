using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;

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
        _ => "Korrith, Saeli, Zeph, Seraphine. At 55% Core HP or higher, Needle Flicker opens Piercing and Blizzard II closes it into Induration. Below 55%, Needle Flicker keeps Piercing open and Hex Lance conducts it.",
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

    public static BattleSimulator Create(HeroState[] heroes, ulong seed) => Create(heroes, new EncounterRng(seed));

    public static BattleSimulator Create(int preset, IRng rng) => Create(CreateParty(preset), rng);

    public static BattleSimulator Create(HeroState[] heroes, IRng rng)
    {

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
            PhaseHpBp = 5_000,
            EnrageTick = 80_000,
            HardEnrageTick = 120_000,
            FrenzyBonusBp = 4_000,
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

    public static HeroState[] CreateParty(int preset) => HeroRoster.PresetParty(preset);

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
