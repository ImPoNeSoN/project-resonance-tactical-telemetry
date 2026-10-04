namespace Resonance.Sim.Core;

/// <summary>Centi-AP gauge. Debt floor −4,000 AP. Refunds cannot push a gauge above 10,000 AP (02 §2.2.2).</summary>
public struct ApGauge
{
    public int Centi;

    public readonly bool IsReady => Centi >= SimConst.ReadyCenti;

    public readonly int WholeAp => Centi / SimConst.CentiPerAp;

    public void Gain(int centiPerTick, int ticks)
    {
        if (ticks <= 0 || centiPerTick == 0)
        {
            return;
        }

        long next = (long)Centi + ((long)centiPerTick * ticks);
        Centi = next > int.MaxValue ? int.MaxValue : (int)next;
    }

    public void PayRecovery(int recoveryAp)
    {
        long next = (long)Centi - ((long)recoveryAp * SimConst.CentiPerAp);
        if (next < SimConst.DebtFloorCenti)
        {
            next = SimConst.DebtFloorCenti;
        }

        Centi = (int)next;
    }

    public void Delay(int ap)
    {
        long next = (long)Centi - ((long)ap * SimConst.CentiPerAp);
        if (next < SimConst.DebtFloorCenti)
        {
            next = SimConst.DebtFloorCenti;
        }

        Centi = (int)next;
    }

    /// <summary>Returns the AP actually granted. A gauge already at the ceiling loses the grant.</summary>
    public int Refund(int ap)
    {
        if (ap <= 0 || Centi >= SimConst.RefundCeilingCenti)
        {
            return 0;
        }

        int add = ap * SimConst.CentiPerAp;
        int room = SimConst.RefundCeilingCenti - Centi;
        if (add > room)
        {
            add = room;
        }

        Centi += add;
        return add / SimConst.CentiPerAp;
    }

    public static int InitialCenti(int agi, bool ambushed)
    {
        if (ambushed)
        {
            return 0;
        }

        int ap = agi * SimConst.InitialApPerAgi;
        if (ap > SimConst.InitialApCap)
        {
            ap = SimConst.InitialApCap;
        }

        return ap * SimConst.CentiPerAp;
    }

    public static int TicksUntilReady(int centi, int centiPerTick)
    {
        if (centi >= SimConst.ReadyCenti)
        {
            return 0;
        }

        if (centiPerTick <= 0)
        {
            return int.MaxValue / 4;
        }

        long need = SimConst.ReadyCenti - (long)centi;
        return (int)((need + centiPerTick - 1) / centiPerTick);
    }

    /// <summary>
    /// Progress toward the next action, in basis points. 10_000 means that action is due now.
    /// Ticks left at or beyond the span (an empty gauge, or AP debt) read as empty.
    /// </summary>
    public static int ChargeBp(int ticksUntilAction, int ticksInSpan)
    {
        if (ticksUntilAction <= 0)
        {
            return SimConst.Bp;
        }

        if (ticksInSpan <= 0 || ticksUntilAction >= ticksInSpan)
        {
            return 0;
        }

        long filled = (long)(ticksInSpan - ticksUntilAction) * SimConst.Bp;
        return (int)(filled / ticksInSpan);
    }

    /// <summary>
    /// Charge for the next thing this unit will do. While the gauge is filling, the span is
    /// the tick count from empty to ready at the current gain. While a chant is in progress,
    /// the span is that chant and the next action is its resolve tick.
    /// </summary>
    public static int NextActionChargeBp(
        int centi,
        int centiPerTick,
        bool casting,
        int castStartTick,
        int castResolveTick,
        int tick)
    {
        if (casting)
        {
            int span = castResolveTick - castStartTick;
            if (span < 1)
            {
                span = 1;
            }

            int left = castResolveTick - tick;
            if (left < 0)
            {
                left = 0;
            }

            return ChargeBp(left, span);
        }

        int full = TicksUntilReady(0, centiPerTick);
        int until = TicksUntilReady(centi, centiPerTick);
        return ChargeBp(until, full);
    }

    public static string Format(int centi)
    {
        int sign = centi < 0 ? -1 : 1;
        int abs = centi < 0 ? -centi : centi;
        int whole = abs / SimConst.CentiPerAp;
        int frac = abs % SimConst.CentiPerAp;
        if (frac == 0)
        {
            return sign < 0 ? $"-{whole}" : whole.ToString();
        }

        // One decimal matches the worked log (8.4, 0.4, 10,000.4).
        int tenth = frac / 10;
        return sign < 0 ? $"-{whole}.{tenth}" : $"{whole}.{tenth}";
    }
}
