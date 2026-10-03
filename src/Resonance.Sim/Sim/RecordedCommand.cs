namespace Resonance.Sim.Sim;

/// <summary>
/// A manual command queued for replay. <see cref="ApplyAtTick"/> is the first tick
/// whose actions may see it. A command queued after tick T is stamped T + 1.
/// A command queued before any tick is stamped 0.
/// </summary>
public readonly struct RecordedCommand
{
    public RecordedCommand(int applyAtTick, int heroSlot, int abilityId, int targetPart, int targetHero)
    {
        ApplyAtTick = applyAtTick;
        HeroSlot = heroSlot;
        AbilityId = abilityId;
        TargetPart = targetPart;
        TargetHero = targetHero;
    }

    public int ApplyAtTick { get; }

    public int HeroSlot { get; }

    public int AbilityId { get; }

    public int TargetPart { get; }

    public int TargetHero { get; }
}
