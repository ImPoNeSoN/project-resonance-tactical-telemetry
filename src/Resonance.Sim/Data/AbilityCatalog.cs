using Resonance.Sim.Core;

namespace Resonance.Sim.Data;

public static class AbilityCatalog
{
    public const int LatticeProvoke = 1;
    public const int SeismicMaul = 2;
    public const int KeratinBastion = 3;
    public const int TalonLance = 4;
    public const int FrostfangPounce = 5;
    public const int TimelineStalk = 6;
    public const int BlizzardII = 7;
    public const int CircuitBenediction = 8;
    public const int CureCascade = 9;
    public const int PhaseSanctuary = 10;
    public const int PistonSweep = 11;
    public const int OverpressureLance = 12;
    public const int GlacierSnap = 20;
    public const int Kindling = 21;
    public const int GaleCut = 22;
    public const int PrismRay = 23;
    public const int GloomBolt = 24;
    public const int Shear = 25;
    public const int CinderBrand = 26;
    public const int ShieldBash = 30;
    public const int Attack = 31;
    public const int PyreLattice = 32;
    public const int HexLance = 33;
    public const int KilnVent = 40;
    public const int EarthBolt = 42;
    public const int RendPulse = 50;
    public const int NeedleFlicker = 51;
    public const int WindTalonArc = 52;
    public const int PhotonSermon = 53;
    public const int SolarFilament = 54;
    public const int GaleQuanta = 55;
    public const int CrescentSever = 56;
    public const int UmbralPierce = 57;
    public const int RavelExecution = 58;

    private static readonly Dictionary<int, AbilityDef> ById = Build();
    private static readonly Dictionary<string, AbilityDef> ByName = IndexNames();

    public static AbilityDef Get(int id) => ById[id];

    public static bool TryGet(string name, out AbilityDef ability)
    {
        string key = Normalize(name);
        return ByName.TryGetValue(key, out ability!);
    }

    public static string Normalize(string name)
    {
        var chars = new char[name.Length];
        int n = 0;
        bool space = false;
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (c == ' ' || c == '\t')
            {
                space = n > 0;
                continue;
            }

            if (space)
            {
                chars[n++] = ' ';
                space = false;
            }

            chars[n++] = char.ToUpperInvariant(c);
        }

        return new string(chars, 0, n);
    }

    private static Dictionary<int, AbilityDef> Build()
    {
        AbilityDef[] all =
        [
            New(LatticeProvoke, "Lattice Provoke", AbilityKind.Enmity, ChainProperty.None, ElementId.None, 4_000, 0, 0, 2_200, 300, 0, SupportEffect.None, "Enmity tool. Deals no damage."),
            New(SeismicMaul, "Seismic Maul", AbilityKind.Physical, ChainProperty.Blunt, ElementId.None, 10_000, 0, 0, 0, 700, 22_000, SupportEffect.None, "Blunt weapon skill."),
            New(KeratinBastion, "Keratin Bastion", AbilityKind.Support, ChainProperty.None, ElementId.None, 4_000, 0, 30, 400, 900, 0, SupportEffect.KeratinBastion, "Self mitigation."),
            New(TalonLance, "Talon Lance", AbilityKind.Physical, ChainProperty.Piercing, ElementId.None, 10_000, 0, 0, 0, 500, 20_000, SupportEffect.None, "Piercing opener."),
            New(FrostfangPounce, "Frostfang Pounce", AbilityKind.ElementalPhysical, ChainProperty.Ice, ElementId.Ice, 10_000, 0, 30, 0, 450, 18_000, SupportEffect.None, "Ice link that closes Piercing into Induration."),
            New(TimelineStalk, "Timeline Stalk", AbilityKind.Support, ChainProperty.None, ElementId.None, 4_000, 0, 20, 0, 200, 0, SupportEffect.TimelineStalk, "Self buff that cheapens the next recovery."),
            New(BlizzardII, "Blizzard II", AbilityKind.Magical, ChainProperty.Ice, ElementId.Ice, 10_000, 1_000, 180, 300, 600, 30_000, SupportEffect.None, "Ice spell. Spends an Induration burst window."),
            New(CircuitBenediction, "Circuit Benediction", AbilityKind.Support, ChainProperty.None, ElementId.None, 4_000, 400, 90, 300, 100, 1_500, SupportEffect.CircuitBenediction, "Buffs one ally."),
            New(CureCascade, "Cure Cascade", AbilityKind.Healing, ChainProperty.None, ElementId.None, 10_000, 900, 140, 0, 0, 32_000, SupportEffect.None, "Heals one ally."),
            New(PhaseSanctuary, "Phase Sanctuary", AbilityKind.Support, ChainProperty.None, ElementId.None, 14_000, 1_500, 260, 1_400, 200, 0, SupportEffect.PhaseSanctuary, "Protects the whole party."),
            New(PistonSweep, "Piston Sweep", AbilityKind.Physical, ChainProperty.None, ElementId.None, 10_000, 0, 0, 0, 0, 16_000, SupportEffect.None, "Boss physical sweep with no chain property."),
            New(OverpressureLance, "Overpressure Lance", AbilityKind.Magical, ChainProperty.Fire, ElementId.Fire, 10_000, 1_200, 0, 0, 0, 22_000, SupportEffect.None, "Boss fire chant."),
            New(GlacierSnap, "Glacier Snap", AbilityKind.Magical, ChainProperty.Ice, ElementId.Ice, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None, "Ice spell."),
            New(Kindling, "Kindling", AbilityKind.Magical, ChainProperty.Fire, ElementId.Fire, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None, "Fire spell."),
            New(GaleCut, "Gale Cut", AbilityKind.Magical, ChainProperty.Wind, ElementId.Wind, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None, "Wind spell."),
            New(PrismRay, "Prism Ray", AbilityKind.Magical, ChainProperty.Light, ElementId.Light, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None, "Light spell."),
            New(GloomBolt, "Gloom Bolt", AbilityKind.Magical, ChainProperty.Darkness, ElementId.Darkness, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None, "Darkness spell."),
            New(Shear, "Shear", AbilityKind.Physical, ChainProperty.Slashing, ElementId.None, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None, "Slashing weapon skill."),
            New(CinderBrand, "Cinder Brand", AbilityKind.ElementalPhysical, ChainProperty.Fire, ElementId.Fire, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None, "Fire elemental-physical."),
            New(ShieldBash, "Shield Bash", AbilityKind.Physical, ChainProperty.Blunt, ElementId.None, 4_000, 0, 0, 0, 400, 8_000, SupportEffect.None, "Interrupts only the part that is casting.", SimConst.ShieldBashInterruptBp),
            New(Attack, "Attack", AbilityKind.Physical, ChainProperty.None, ElementId.None, 10_000, 0, 0, 0, 100, 10_000, SupportEffect.None, "Basic weapon swing with no chain property."),
            New(PyreLattice, "Pyre Lattice", AbilityKind.Magical, ChainProperty.Fire, ElementId.Fire, 14_000, 1_400, 320, 700, 1_000, 44_000, SupportEffect.None, "Heavy fire spell. Spends a Liquefaction burst window."),
            New(HexLance, "Hex Lance", AbilityKind.Magical, ChainProperty.Lightning, ElementId.Lightning, 10_000, 600, 140, 200, 500, 24_000, SupportEffect.None, "Lightning spell. Closes Piercing into Conduction."),
            New(KilnVent, "Kiln Vent", AbilityKind.Magical, ChainProperty.Fire, ElementId.Fire, 10_000, 800, 0, 0, 200, 15_000, SupportEffect.None, "Fire chant that burns the part it hits.", appliesBurn: true),
            New(EarthBolt, "Earth Bolt", AbilityKind.Magical, ChainProperty.Earth, ElementId.Earth, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None, "Earth spell."),
            New(RendPulse, "Rend Pulse", AbilityKind.Physical, ChainProperty.Slashing, ElementId.None, 10_000, 0, 0, 0, 450, 19_000, SupportEffect.None, "Slashing. Closes Blunt into Distortion and purges Kiln Guard."),
            New(NeedleFlicker, "Needle Flicker", AbilityKind.Physical, ChainProperty.Piercing, ElementId.None, 4_000, 0, 0, 0, 250, 11_000, SupportEffect.None, "Fast piercing hit."),
            New(WindTalonArc, "Wind Talon Arc", AbilityKind.ElementalPhysical, ChainProperty.Wind, ElementId.Wind, 10_000, 0, 25, 0, 500, 21_000, SupportEffect.None, "Wind elemental-physical. Closes Slashing into Fragmentation."),
            New(PhotonSermon, "Photon Sermon", AbilityKind.Magical, ChainProperty.Light, ElementId.Light, 10_000, 900, 160, 300, 500, 31_000, SupportEffect.None, "Light spell. On an open Fragmentation this is a Solar Apex."),
            New(SolarFilament, "Solar Filament", AbilityKind.Magical, ChainProperty.Light, ElementId.Light, 14_000, 1_600, 300, 800, 900, 46_000, SupportEffect.None, "Heavy light spell with the same Light links as Photon Sermon."),
            New(GaleQuanta, "Gale Quanta", AbilityKind.Magical, ChainProperty.Wind, ElementId.Wind, 10_000, 500, 120, 200, 400, 22_000, SupportEffect.None, "Wind spell. Closes Slashing into Fragmentation."),
            New(CrescentSever, "Crescent Sever", AbilityKind.Physical, ChainProperty.Slashing, ElementId.None, 10_000, 0, 0, 0, 600, 26_000, SupportEffect.None, "Slashing. Closes Blunt into Distortion and purges Kiln Guard."),
            New(UmbralPierce, "Umbral Pierce", AbilityKind.ElementalPhysical, ChainProperty.Darkness, ElementId.Darkness, 10_000, 0, 30, 0, 600, 24_000, SupportEffect.None, "Darkness link. Closes Blunt into Distortion (Kiln Guard purge) and Induration into Umbral Zero."),
            New(RavelExecution, "Ravel Execution", AbilityKind.Physical, ChainProperty.Piercing, ElementId.None, 14_000, 0, 0, 0, 1_500, 52_000, SupportEffect.None, "Tier-3 finisher. Closes Slashing into Fragmentation and takes the physical burst bonus inside that window."),
        ];

        var map = new Dictionary<int, AbilityDef>(all.Length);
        foreach (var ability in all)
        {
            map.Add(ability.Id, ability);
        }

        return map;
    }

    private static Dictionary<string, AbilityDef> IndexNames()
    {
        var map = new Dictionary<string, AbilityDef>(ById.Count);
        foreach (AbilityDef ability in ById.Values)
        {
            map[Normalize(ability.Name)] = ability;
        }

        return map;
    }

    private static AbilityDef New(
        int id,
        string name,
        AbilityKind kind,
        ChainProperty property,
        ElementId element,
        int recovery,
        int chant,
        int mp,
        int ve,
        int ce,
        int multiplier,
        SupportEffect effect,
        string description,
        int flatInterruptBp = 0,
        bool appliesBurn = false)
    {
        return new AbilityDef
        {
            Id = id,
            Name = name,
            Kind = kind,
            Property = property,
            Element = element,
            RecoveryAp = recovery,
            ChantTicks = chant,
            MpCost = mp,
            BaseVe = ve,
            BaseCe = ce,
            MultiplierBp = multiplier,
            Effect = effect,
            FlatInterruptBp = flatInterruptBp,
            AppliesBurn = appliesBurn,
            Description = description,
        };
    }
}
