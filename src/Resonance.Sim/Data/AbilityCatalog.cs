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
            New(LatticeProvoke, "Lattice Provoke", AbilityKind.Enmity, ChainProperty.None, ElementId.None, 4_000, 0, 0, 2_200, 300, 0, SupportEffect.None),
            New(SeismicMaul, "Seismic Maul", AbilityKind.Physical, ChainProperty.Blunt, ElementId.None, 10_000, 0, 0, 0, 700, 22_000, SupportEffect.None),
            New(KeratinBastion, "Keratin Bastion", AbilityKind.Support, ChainProperty.None, ElementId.None, 4_000, 0, 30, 400, 900, 0, SupportEffect.KeratinBastion),
            New(TalonLance, "Talon Lance", AbilityKind.Physical, ChainProperty.Piercing, ElementId.None, 10_000, 0, 0, 0, 500, 20_000, SupportEffect.None),
            New(FrostfangPounce, "Frostfang Pounce", AbilityKind.ElementalPhysical, ChainProperty.Ice, ElementId.Ice, 10_000, 0, 30, 0, 450, 18_000, SupportEffect.None),
            New(TimelineStalk, "Timeline Stalk", AbilityKind.Support, ChainProperty.None, ElementId.None, 4_000, 0, 20, 0, 200, 0, SupportEffect.TimelineStalk),
            New(BlizzardII, "Blizzard II", AbilityKind.Magical, ChainProperty.Ice, ElementId.Ice, 10_000, 1_000, 180, 300, 600, 30_000, SupportEffect.None),
            New(CircuitBenediction, "Circuit Benediction", AbilityKind.Support, ChainProperty.None, ElementId.None, 4_000, 400, 90, 300, 100, 1_500, SupportEffect.CircuitBenediction),
            New(CureCascade, "Cure Cascade", AbilityKind.Healing, ChainProperty.None, ElementId.None, 10_000, 900, 140, 0, 0, 32_000, SupportEffect.None),
            New(PhaseSanctuary, "Phase Sanctuary", AbilityKind.Support, ChainProperty.None, ElementId.None, 14_000, 1_500, 260, 1_400, 200, 0, SupportEffect.PhaseSanctuary),
            New(PistonSweep, "Piston Sweep", AbilityKind.Physical, ChainProperty.None, ElementId.None, 10_000, 0, 0, 0, 0, 16_000, SupportEffect.None),
            New(OverpressureLance, "Overpressure Lance", AbilityKind.Magical, ChainProperty.Fire, ElementId.Fire, 10_000, 1_200, 0, 0, 0, 22_000, SupportEffect.None),
            New(GlacierSnap, "Glacier Snap", AbilityKind.Magical, ChainProperty.Ice, ElementId.Ice, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None),
            New(Kindling, "Kindling", AbilityKind.Magical, ChainProperty.Fire, ElementId.Fire, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None),
            New(GaleCut, "Gale Cut", AbilityKind.Magical, ChainProperty.Wind, ElementId.Wind, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None),
            New(PrismRay, "Prism Ray", AbilityKind.Magical, ChainProperty.Light, ElementId.Light, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None),
            New(GloomBolt, "Gloom Bolt", AbilityKind.Magical, ChainProperty.Darkness, ElementId.Darkness, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None),
            New(Shear, "Shear", AbilityKind.Physical, ChainProperty.Slashing, ElementId.None, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None),
            New(CinderBrand, "Cinder Brand", AbilityKind.ElementalPhysical, ChainProperty.Fire, ElementId.Fire, 10_000, 0, 0, 0, 0, 10_000, SupportEffect.None),
            New(ShieldBash, "Shield Bash", AbilityKind.Physical, ChainProperty.Blunt, ElementId.None, 4_000, 0, 0, 0, 400, 8_000, SupportEffect.None, SimConst.ShieldBashInterruptBp),
            New(Attack, "Attack", AbilityKind.Physical, ChainProperty.None, ElementId.None, 10_000, 0, 0, 0, 100, 10_000, SupportEffect.None),
            New(PyreLattice, "Pyre Lattice", AbilityKind.Magical, ChainProperty.Fire, ElementId.Fire, 14_000, 1_400, 320, 700, 1_000, 44_000, SupportEffect.None),
            New(HexLance, "Hex Lance", AbilityKind.Magical, ChainProperty.Lightning, ElementId.Lightning, 10_000, 600, 140, 200, 500, 24_000, SupportEffect.None),
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
        int flatInterruptBp = 0)
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
        };
    }
}
