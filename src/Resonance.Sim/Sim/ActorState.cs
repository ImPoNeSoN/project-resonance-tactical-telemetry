using Resonance.Sim.Combat;
using Resonance.Sim.Core;
using Resonance.Sim.Data;
using Resonance.Sim.Gambits;

namespace Resonance.Sim.Sim;

public sealed class HeroState
{
    public int Slot;
    public string Name = "";
    public RaceId Race;
    public int MaxHp;
    public int Hp;
    public int MaxMp;
    public int Mp;
    public int Atk;
    public int Def;
    public int Intel;
    public int Meva;
    public int Acc;
    public int Eva;
    public int Agi;
    public int Concentration;
    public int Epen;
    public bool EarlyWindowCartography;
    public ApGauge Ap;
    public GearMods Gear = new();
    public bool Casting;
    public int CastAbilityId;
    public int CastTargetPart = -1;
    public int CastTargetHero = -1;
    public int CastResolveTick;
    public int CastStartTick;
    public int RecoveryCut;
    public int RecoveryCutExpires;
    public int PhysDtBp;
    public int PhysDtExpires;
    public bool PhysicalHitsMitigated;
    public int AllDtBp;
    public int AllDtExpires;
    public int ConcentrationBuff;
    public int ConcentrationBuffExpires;
    public int RegenPerPulse;
    public int RegenExpires;
    public int RegenNextTick;
    public int Absorb;
    public int Heat;
    public int HeatDecayTick;
    public int HeatAfterPrime;
    public int CadenceRefundAp;
    public bool HasBulwarkBash;
    public bool Silenced;
    public int CurrentTargetPart;
    public int DeferUntilTick;
    public int DeferStartedTick;
    public int WaitDtBp;
    public int BurnPulse;
    public int BurnNextTick;
    public int BurnExpires;
    public GambitProgram? Deck;
    public bool IsAnchor;
    public bool SplitLoadouts;
    public GearMods Idle = new();
    public GearMods FastCast = new();
    public GearMods MidCast = new();
    public GearMods Weapon = new();
    public int[] Kit = [];
    public int RadiancePulse;
    public int RadianceNextTick;
    public int RadianceExpires;

    public bool IsAlive => Hp > 0;

    public int ConcentrationNow(int tick)
    {
        int bonus = tick < ConcentrationBuffExpires ? ConcentrationBuff : 0;
        return Concentration + bonus;
    }
}

public sealed class BossPartState
{
    public string Name = "";
    public int MaxHp;
    public int Hp;
    public bool Active = true;
    public int Tier;
    public ChainProperty Property = ChainProperty.None;
    public ResonanceId Resonance = ResonanceId.None;
    public int ChainExpires;
    public bool BurstActive;
    public int BurstOpened;
    public int BurstExpires;
    public int BurstExtension;
    public int BurstsLanded;
    public ElementMask BurstMask;
    public int BurstBucketBp;
    public int BurstTrueBp;
    public int BurnPulse;
    public int BurnNextTick;
    public int BurnExpires;
    public bool BurnIii;
    public int Contributors;
    public int ShockPulse;
    public int ShockNextTick;
    public int ShockExpires;
    public EnmitySlot[] Enmity = [];

    /// <summary>Null uses the boss-wide table. The golden fight leaves this null.</summary>
    public int[]? ResistBp;

    public int DefBonus;

    /// <summary>Player-facing reason this part rewards a chain route.</summary>
    public string Pressure = "";
}

public sealed class BossState
{
    public string Name = "";
    public int Atk;
    public int Def;
    public int Intel;
    public int Meva;
    public int Acc;
    public int Eva;
    public int Agi;
    public int Concentration;
    public ApGauge Ap;
    public bool Casting;
    public int CastAbilityId;
    public int CastPart;
    public int CastTargetHero = -1;
    public int CastResolveTick;
    public int CastStartTick;
    public int SlowBp;
    public int SlowExpires;
    public bool PounceAvailable = true;
    public int ShatterBp;
    public int ShatterExpires;
    public int MevaDownBp;
    public int MevaDownExpires;
    public int FrenzyBp;
    public int[] ResistBp = new int[8];
    public List<string> Beneficial = [];
    public BossPartState[] Parts = [];

    public int SlowNow(int tick) => tick < SlowExpires ? SlowBp : 0;
}

public readonly struct CombatEvent
{
    public CombatEvent(int tick, string text)
    {
        Tick = tick;
        Text = text;
    }

    public int Tick { get; }
    public string Text { get; }
}
