namespace Resonance.Sim.Data;

public enum RaceId
{
    VethKari,
    SylvariMor,
    KithLir,
    AshDravan,
    AethelBorn,
}

public enum ChainProperty
{
    Blunt = 0,
    Piercing = 1,
    Slashing = 2,
    Fire = 3,
    Ice = 4,
    Wind = 5,
    Earth = 6,
    Lightning = 7,
    Water = 8,
    Light = 9,
    Darkness = 10,
    None = -1,
}

public enum ElementId
{
    Fire = 0,
    Ice = 1,
    Wind = 2,
    Earth = 3,
    Lightning = 4,
    Water = 5,
    Light = 6,
    Darkness = 7,
    None = -1,
}

[Flags]
public enum ElementMask
{
    None = 0,
    Fire = 1 << 0,
    Ice = 1 << 1,
    Wind = 1 << 2,
    Earth = 1 << 3,
    Lightning = 1 << 4,
    Water = 1 << 5,
    Light = 1 << 6,
    Darkness = 1 << 7,
    Physical = 1 << 8,
}

public enum ResonanceId
{
    None = 0,
    Liquefaction,
    Induration,
    Fragmentation,
    Distortion,
    Conduction,
    TectonicShear,
    Radiance,
}

public enum ApexId
{
    None = 0,
    SolarApex,
    UmbralZero,
    MagmaCore,
    TempestCrown,
}

public enum AbilityKind
{
    None,
    Physical,
    ElementalPhysical,
    Magical,
    Healing,
    Support,
    Enmity,
}

public enum SupportEffect
{
    None,
    KeratinBastion,
    TimelineStalk,
    CircuitBenediction,
    PhaseSanctuary,
    ChoirAegis,
}

public static class ElementMaps
{
    public static ElementId FromProperty(ChainProperty property) => property switch
    {
        ChainProperty.Fire => ElementId.Fire,
        ChainProperty.Ice => ElementId.Ice,
        ChainProperty.Wind => ElementId.Wind,
        ChainProperty.Earth => ElementId.Earth,
        ChainProperty.Lightning => ElementId.Lightning,
        ChainProperty.Water => ElementId.Water,
        ChainProperty.Light => ElementId.Light,
        ChainProperty.Darkness => ElementId.Darkness,
        _ => ElementId.None,
    };

    public static ElementMask Mask(ElementId element) => element switch
    {
        ElementId.Fire => ElementMask.Fire,
        ElementId.Ice => ElementMask.Ice,
        ElementId.Wind => ElementMask.Wind,
        ElementId.Earth => ElementMask.Earth,
        ElementId.Lightning => ElementMask.Lightning,
        ElementId.Water => ElementMask.Water,
        ElementId.Light => ElementMask.Light,
        ElementId.Darkness => ElementMask.Darkness,
        _ => ElementMask.None,
    };

    public static string Name(ChainProperty property) => property.ToString();
}

public sealed class AbilityDef
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required AbilityKind Kind { get; init; }
    public ChainProperty Property { get; init; } = ChainProperty.None;
    public ElementId Element { get; init; } = ElementId.None;
    public int RecoveryAp { get; init; }
    public int ChantTicks { get; init; }
    public int MpCost { get; init; }
    public int BaseVe { get; init; }
    public int BaseCe { get; init; }
    public int MultiplierBp { get; init; }
    public SupportEffect Effect { get; init; }
    public int FlatInterruptBp { get; init; }
    public bool AppliesBurn { get; init; }

    /// <summary>
    /// Short tactical note for the grey-box ability panel. Numbers that already live on this
    /// record (MP, AP, multiplier, threat) are formatted from those fields, not repeated here.
    /// </summary>
    public string Description { get; init; } = "";
    public bool UsesWeaponLoadout => Kind is AbilityKind.Physical or AbilityKind.ElementalPhysical;
    public bool DealsDamage => Kind is AbilityKind.Physical or AbilityKind.ElementalPhysical or AbilityKind.Magical;
}

public sealed class GearMods
{
    public int IdleDef { get; init; }
    public int IdleMeva { get; init; }
    public int IdleEva { get; init; }
    public int IdleEnmityBp { get; init; }
    public int FastCastBp { get; init; }
    public int MidInt { get; init; }
    public int MidMbdBp { get; init; }
    public int MidCritDamageBp { get; init; }
    public int MidHealingPotencyBp { get; init; }
    public int WsStr { get; init; }
    public int WsDex { get; init; }
    public int WsDoubleAttackBp { get; init; }
    public int WsTripleAttackBp { get; init; }
    public int WsDamageBp { get; init; }
    public int WsCritBp { get; init; }
    public int WsCritDamageBp { get; init; }
}

public readonly struct BattleAction
{
    public BattleAction(int abilityId, int targetPart, int targetHero)
    {
        AbilityId = abilityId;
        TargetPart = targetPart;
        TargetHero = targetHero;
    }

    public int AbilityId { get; }
    public int TargetPart { get; }
    public int TargetHero { get; }
}
