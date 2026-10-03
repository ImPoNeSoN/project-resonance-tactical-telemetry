using Resonance.Sim.Core;

namespace Resonance.Sim.Gambits;

public enum GambitSubject
{
    Self,
    Target,
    Boss,
    Ally,
    Party,
    Field,
}

public enum AllyPick
{
    LowestHp,
    LowestMp,
    Tank,
    Casting,
    Targeted,
    Named,
}

public enum GambitPredicate
{
    Always,
    Resonance,
    Window,
    WindowLeft,
    BurstLeft,
    Casting,
    CastResolvesIn,
    HpPercent,
    Ap,
    HasBuff,
    HasDebuff,
    Disabled,
    TargetingMe,
    MyEnmityRank,
    TankMargin,
    MpPercent,
    Mp,
    MpForAbility,
    Heat,
    Primed,
    Aquifer,
    Deferred,
    ReadyIn,
    Ko,
    CountBelowHp,
    ChainParticipants,
    AetherDensity,
    Tick,
    GlobalBurst,
}

public enum GambitCompare
{
    None,
    Less,
    LessOrEqual,
    Greater,
    GreaterOrEqual,
    Equal,
}

public enum GambitVerb
{
    Cast,
    Defer,
    Wait,
    Retarget,
}

public enum GambitTargetKind
{
    Default,
    Self,
    Part,
    Ally,
}

[Flags]
public enum GambitFlags
{
    None = 0,
    SpendHeat = 1,
    NoOverwrite = 2,
    BurstOnly = 4,
}

public struct GambitCondition
{
    public bool Not;
    public GambitSubject Subject;
    public int SubjectArg;
    public AllyPick Ally;
    public string? Text;
    public GambitPredicate Predicate;
    public int Arg0;
    public int Arg1;
    public GambitCompare Compare;
    public int Threshold;
}

public struct GambitSlot
{
    public GambitCondition C0;
    public GambitCondition C1;
    public GambitCondition C2;
    public byte Count;
    public GambitVerb Verb;
    public int AbilityId;
    public int Number;
    public GambitTargetKind TargetKind;
    public AllyPick Ally;
    public int TargetArg;
    public string? TargetText;
    public GambitFlags Flags;

    public readonly GambitCondition Condition(int index) => index switch
    {
        0 => C0,
        1 => C1,
        _ => C2,
    };
}

public sealed class GambitProgram
{
    public GambitSlot[] Slots { get; init; } = [];

    public string Source { get; init; } = "";
}

public struct GambitCompileOptions
{
    public int MaxSlots;
    public bool BulwarkBash;

    public static GambitCompileOptions Standard(bool bulwarkBash = false) => new()
    {
        MaxSlots = SimConst.GambitStartingSlots,
        BulwarkBash = bulwarkBash,
    };

    public static GambitCompileOptions ForRank(int rank, bool bulwarkBash = false)
    {
        int slots = SimConst.GambitStartingSlots;
        if (rank >= 10)
        {
            slots++;
        }

        if (rank >= 20)
        {
            slots++;
        }

        if (rank >= 30)
        {
            slots++;
        }

        if (rank >= 40)
        {
            slots++;
        }

        if (slots > SimConst.GambitMaxSlots)
        {
            slots = SimConst.GambitMaxSlots;
        }

        return new GambitCompileOptions { MaxSlots = slots, BulwarkBash = bulwarkBash };
    }
}

public sealed class GambitCompileResult
{
    public bool Ok { get; init; }

    public GambitProgram Program { get; init; } = new();

    public string[] Errors { get; init; } = [];
}

public enum GambitOutcome
{
    Act,
    Defer,
    Wait,
}

public struct GambitDecision
{
    public GambitOutcome Outcome;
    public int AbilityId;
    public int TargetPart;
    public int TargetHero;
    public int DeferTicks;
    public int Slot;
    public bool SpendHeat;
}
