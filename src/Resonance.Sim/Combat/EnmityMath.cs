namespace Resonance.Sim.Combat;

public struct EnmitySlot
{
    public int NormalVe;
    public int HeavyVe;
    public int Ce;

    public readonly int Ve => NormalVe + HeavyVe;

    public readonly int Total => Ve + Ce;
}

public static class EnmityMath
{
    public const int Cap = 30_000;

    public static void AddVolatile(ref EnmitySlot slot, int amount, bool heavy)
    {
        if (amount <= 0)
        {
            return;
        }

        if (heavy)
        {
            slot.HeavyVe += amount;
        }
        else
        {
            slot.NormalVe += amount;
        }

        int overflow = slot.Ve - Cap;
        if (overflow <= 0)
        {
            return;
        }

        if (heavy)
        {
            slot.HeavyVe -= overflow;
            if (slot.HeavyVe < 0)
            {
                slot.NormalVe += slot.HeavyVe;
                slot.HeavyVe = 0;
            }
        }
        else
        {
            slot.NormalVe -= overflow;
            if (slot.NormalVe < 0)
            {
                slot.HeavyVe += slot.NormalVe;
                slot.NormalVe = 0;
            }
        }

        if (slot.NormalVe < 0)
        {
            slot.NormalVe = 0;
        }

        if (slot.HeavyVe < 0)
        {
            slot.HeavyVe = 0;
        }
    }

    public static void AddCumulative(ref EnmitySlot slot, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        slot.Ce += amount;
        if (slot.Ce > Cap)
        {
            slot.Ce = Cap;
        }
    }

    public static void Decay(ref EnmitySlot slot)
    {
        slot.NormalVe = Formulas.DecayVolatile(slot.NormalVe, heavyBucket: false);
        slot.HeavyVe = Formulas.DecayVolatile(slot.HeavyVe, heavyBucket: true);
    }

    public static bool HasThreat(EnmitySlot[] table)
    {
        for (int i = 0; i < table.Length; i++)
        {
            if (table[i].Total > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// argmax(VE + CE). Ties: higher CE, then lower slot. KO'd heroes are skipped.
    /// Returns -1 when every living hero is at 0.
    /// </summary>
    public static int ArgMax(EnmitySlot[] table, bool[] alive)
    {
        int best = -1;
        for (int i = 0; i < table.Length; i++)
        {
            if (!alive[i] || table[i].Total <= 0)
            {
                continue;
            }

            if (best < 0 || Beats(table[i], i, table[best], best))
            {
                best = i;
            }
        }

        return best;
    }

    private static bool Beats(in EnmitySlot candidate, int candidateSlot, in EnmitySlot current, int currentSlot)
    {
        if (candidate.Total != current.Total)
        {
            return candidate.Total > current.Total;
        }

        if (candidate.Ce != current.Ce)
        {
            return candidate.Ce > current.Ce;
        }

        return candidateSlot < currentSlot;
    }
}
