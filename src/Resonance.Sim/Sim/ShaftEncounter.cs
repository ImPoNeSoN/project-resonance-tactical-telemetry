using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;

namespace Resonance.Sim.Sim;

/// <summary>
/// Grey-box trash and the floor elite. The bible names a trash concentration tier and no bestiary,
/// so these three bodies are the VS-1 stand-ins. Victory is still Core HP 0.
/// </summary>
public static class ShaftEncounter
{
    public static BattleSimulator CinderMite(int preset, ulong seed) => CinderMite(CarapaceEncounter.CreateParty(preset), seed);

    public static BattleSimulator CinderMite(HeroState[] heroes, ulong seed)
    {
        return Build(
            heroes,
            seed,
            "Cinder Mite",
            atk: 140,
            def: 80,
            intel: 120,
            meva: 60,
            acc: 180,
            eva: 40,
            agi: 10,
            concentration: 0,
            coreHp: 7_000,
            brandHp: 0,
            corePressure: "A kiln spark. Concentration 0, so a solid hit interrupts Mite Spit.",
            brandPressure: "",
            phaseHpBp: 4_000,
            enrageTick: 16_000,
            hardEnrageTick: 28_000,
            frenzyBonusBp: 1_000,
            abilities: [AbilityCatalog.MiteSpit, AbilityCatalog.Attack],
            parts: [BattleSimulator.Core, BattleSimulator.Core]);
    }

    public static BattleSimulator SlagSkitter(int preset, ulong seed) => SlagSkitter(CarapaceEncounter.CreateParty(preset), seed);

    public static BattleSimulator SlagSkitter(HeroState[] heroes, ulong seed)
    {
        return Build(
            heroes,
            seed,
            "Slag Skitter",
            atk: 180,
            def: 120,
            intel: 40,
            meva: 70,
            acc: 180,
            eva: 50,
            agi: 10,
            concentration: 0,
            coreHp: 9_000,
            brandHp: 0,
            corePressure: "A slag crawler. It only bites, and it has no concentration.",
            brandPressure: "",
            phaseHpBp: 4_000,
            enrageTick: 18_000,
            hardEnrageTick: 32_000,
            frenzyBonusBp: 1_200,
            abilities: [AbilityCatalog.SkitterLash],
            parts: [BattleSimulator.Core]);
    }

    public static BattleSimulator KilnWarden(int preset, ulong seed) => KilnWarden(CarapaceEncounter.CreateParty(preset), seed);

    public static BattleSimulator KilnWarden(HeroState[] heroes, ulong seed)
    {
        return Build(
            heroes,
            seed,
            "Kiln Warden",
            atk: 240,
            def: 180,
            intel: 160,
            meva: 100,
            acc: 200,
            eva: 60,
            agi: 10,
            concentration: 20,
            coreHp: 22_000,
            brandHp: 8_000,
            corePressure: "Elite core. Concentration 20. Overpressure Lance is the chant.",
            brandPressure: "The brand sweeps while the core chants. Destroying it is optional.",
            phaseHpBp: 4_000,
            enrageTick: 40_000,
            hardEnrageTick: 70_000,
            frenzyBonusBp: 1_800,
            abilities: [AbilityCatalog.PistonSweep, AbilityCatalog.OverpressureLance, AbilityCatalog.PistonSweep],
            parts: [BattleSimulator.WeaponArm, BattleSimulator.Core, BattleSimulator.WeaponArm]);
    }

    private static BattleSimulator Build(
        HeroState[] heroes,
        ulong seed,
        string name,
        int atk,
        int def,
        int intel,
        int meva,
        int acc,
        int eva,
        int agi,
        int concentration,
        int coreHp,
        int brandHp,
        string corePressure,
        string brandPressure,
        int phaseHpBp,
        int enrageTick,
        int hardEnrageTick,
        int frenzyBonusBp,
        int[] abilities,
        int[] parts)
    {
        var body = new List<BossPartState>
        {
            Part("Core", coreHp, heroes.Length, corePressure),
        };
        if (brandHp > 0)
        {
            body.Add(Part("Brand", brandHp, heroes.Length, brandPressure));
        }

        var boss = new BossState
        {
            Name = name,
            Atk = atk,
            Def = def,
            Intel = intel,
            Meva = meva,
            Acc = acc,
            Eva = eva,
            Agi = agi,
            Concentration = concentration,
            Ap = new ApGauge { Centi = ApGauge.InitialCenti(agi, ambushed: false) },
            ResistBp = Resist(),
            Parts = body.ToArray(),
        };

        var script = new BossScript
        {
            PhaseHpBp = phaseHpBp,
            EnrageTick = enrageTick,
            HardEnrageTick = hardEnrageTick,
            FrenzyBonusBp = frenzyBonusBp,
            Phase1 = abilities,
            Phase1Part = parts,
            Phase2 = abilities,
            Phase2Part = parts,
        };

        var plans = new Queue<BattleAction>[heroes.Length];
        for (int i = 0; i < plans.Length; i++)
        {
            plans[i] = new Queue<BattleAction>();
        }

        return new BattleSimulator(heroes, boss, plans, new Queue<BattleAction>(), new EncounterRng(seed), script, scoreOutcome: true);
    }

    private static BossPartState Part(string name, int hp, int heroes, string pressure)
    {
        return new BossPartState
        {
            Name = name,
            MaxHp = hp,
            Hp = hp,
            Enmity = new EnmitySlot[heroes],
            ResistBp = Resist(),
            Pressure = pressure,
        };
    }

    private static int[] Resist()
    {
        var resist = new int[8];
        resist[(int)ElementId.Fire] = -1_000;
        resist[(int)ElementId.Ice] = 1_000;
        return resist;
    }
}
