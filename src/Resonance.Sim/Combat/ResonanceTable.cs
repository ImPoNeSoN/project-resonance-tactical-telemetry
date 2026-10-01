using Resonance.Sim.Data;

namespace Resonance.Sim.Combat;

public readonly struct BurstProfile
{
    public BurstProfile(int bucketBonusBp, int trueBonusBp, ElementMask mask)
    {
        BucketBonusBp = bucketBonusBp;
        TrueBonusBp = trueBonusBp;
        Mask = mask;
    }

    public int BucketBonusBp { get; }
    public int TrueBonusBp { get; }
    public ElementMask Mask { get; }

    public bool MatchesMagic(ElementId element) => (Mask & ElementMaps.Mask(element)) != 0;

    public bool MatchesPhysical => (Mask & ElementMask.Physical) != 0;
}

public static class ResonanceTable
{
    private static readonly ResonanceId[,] Level2 = new ResonanceId[11, 11];
    private static readonly ApexId[,] Level3 = new ApexId[8, 11];

    static ResonanceTable()
    {
        SetL2(ChainProperty.Blunt, ChainProperty.Fire, ResonanceId.Liquefaction);
        SetL2(ChainProperty.Earth, ChainProperty.Fire, ResonanceId.Liquefaction);
        SetL2(ChainProperty.Piercing, ChainProperty.Ice, ResonanceId.Induration);
        SetL2(ChainProperty.Water, ChainProperty.Ice, ResonanceId.Induration);
        SetL2(ChainProperty.Slashing, ChainProperty.Wind, ResonanceId.Fragmentation);
        SetL2(ChainProperty.Slashing, ChainProperty.Piercing, ResonanceId.Fragmentation);
        SetL2(ChainProperty.Blunt, ChainProperty.Darkness, ResonanceId.Distortion);
        SetL2(ChainProperty.Blunt, ChainProperty.Slashing, ResonanceId.Distortion);
        SetL2(ChainProperty.Water, ChainProperty.Darkness, ResonanceId.Distortion);
        SetL2(ChainProperty.Water, ChainProperty.Lightning, ResonanceId.Conduction);
        SetL2(ChainProperty.Piercing, ChainProperty.Lightning, ResonanceId.Conduction);
        SetL2(ChainProperty.Slashing, ChainProperty.Earth, ResonanceId.TectonicShear);
        SetL2(ChainProperty.Earth, ChainProperty.Blunt, ResonanceId.TectonicShear);
        SetL2(ChainProperty.Fire, ChainProperty.Light, ResonanceId.Radiance);
        SetL2(ChainProperty.Wind, ChainProperty.Light, ResonanceId.Radiance);

        SetL3(ResonanceId.Fragmentation, ChainProperty.Light, ApexId.SolarApex);
        SetL3(ResonanceId.Radiance, ChainProperty.Slashing, ApexId.SolarApex);
        SetL3(ResonanceId.Distortion, ChainProperty.Ice, ApexId.UmbralZero);
        SetL3(ResonanceId.Induration, ChainProperty.Darkness, ApexId.UmbralZero);
        SetL3(ResonanceId.Liquefaction, ChainProperty.Earth, ApexId.MagmaCore);
        SetL3(ResonanceId.TectonicShear, ChainProperty.Fire, ApexId.MagmaCore);
        SetL3(ResonanceId.Fragmentation, ChainProperty.Lightning, ApexId.TempestCrown);
        SetL3(ResonanceId.Conduction, ChainProperty.Wind, ApexId.TempestCrown);
    }

    public static ResonanceId LookupL2(ChainProperty open, ChainProperty incoming)
    {
        if (open < 0 || incoming < 0)
        {
            return ResonanceId.None;
        }

        return Level2[(int)open, (int)incoming];
    }

    public static ApexId LookupL3(ResonanceId open, ChainProperty incoming)
    {
        if (open == ResonanceId.None || incoming < 0)
        {
            return ApexId.None;
        }

        return Level3[(int)open, (int)incoming];
    }

    public static BurstProfile Profile(ResonanceId resonance) => resonance switch
    {
        ResonanceId.Liquefaction => new BurstProfile(4_000, 0, ElementMask.Fire),
        ResonanceId.Induration => new BurstProfile(5_000, 0, ElementMask.Ice),
        ResonanceId.Fragmentation => new BurstProfile(6_000, 0, ElementMask.Wind | ElementMask.Physical),
        ResonanceId.Distortion => new BurstProfile(0, 7_000, ElementMask.Darkness),
        ResonanceId.Conduction => new BurstProfile(4_500, 0, ElementMask.Lightning),
        ResonanceId.TectonicShear => new BurstProfile(4_500, 0, ElementMask.Earth),
        ResonanceId.Radiance => new BurstProfile(4_000, 0, ElementMask.Light),
        _ => new BurstProfile(0, 0, ElementMask.None),
    };

    public static BurstProfile Profile(ApexId apex) => apex switch
    {
        // 02 §2.11.4: every Level 3 bonus is a true-damage component, not BurstBucket.
        ApexId.SolarApex => new BurstProfile(0, 12_000, ElementMask.Light | ElementMask.Wind | ElementMask.Physical),
        ApexId.UmbralZero => new BurstProfile(0, 11_000, ElementMask.Darkness | ElementMask.Ice),
        ApexId.MagmaCore => new BurstProfile(0, 11_000, ElementMask.Fire | ElementMask.Earth),
        ApexId.TempestCrown => new BurstProfile(0, 11_000, ElementMask.Wind | ElementMask.Lightning),
        _ => new BurstProfile(0, 0, ElementMask.None),
    };

    public static int CountL2Routes()
    {
        int count = 0;
        for (int r = 0; r < 11; r++)
        {
            for (int c = 0; c < 11; c++)
            {
                if (Level2[r, c] != ResonanceId.None)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public static int CountL3Routes()
    {
        int count = 0;
        for (int r = 1; r <= 7; r++)
        {
            for (int c = 0; c < 11; c++)
            {
                if (Level3[r, c] != ApexId.None)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static void SetL2(ChainProperty open, ChainProperty incoming, ResonanceId resonance)
    {
        Level2[(int)open, (int)incoming] = resonance;
    }

    private static void SetL3(ResonanceId open, ChainProperty incoming, ApexId apex)
    {
        Level3[(int)open, (int)incoming] = apex;
    }
}
