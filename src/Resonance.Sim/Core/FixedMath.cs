using System.Numerics;

namespace Resonance.Sim.Core;

public static class FixedMath
{
    public static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    public static int ClampBp(int value) => Clamp(value, 0, SimConst.Bp);

    public static long CeilDiv(long numerator, long denominator)
    {
        if (denominator <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator));
        }

        if (numerator <= 0)
        {
            return 0;
        }

        return (numerator + denominator - 1) / denominator;
    }

    public static int MulBp(int value, int bp) => (int)((long)value * bp / SimConst.Bp);

    public static int FloorOnce(Int128 numerator, int basisFactors)
    {
        Int128 den = 1;
        for (int i = 0; i < basisFactors; i++)
        {
            den *= SimConst.Bp;
        }

        if (den == 0)
        {
            return 0;
        }

        Int128 value = numerator / den;
        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }

        if (value < int.MinValue)
        {
            return int.MinValue;
        }

        return (int)value;
    }

    /// <summary>Round half up for a non-negative rational.</summary>
    public static int RoundHalfUp(long numerator, long denominator)
    {
        if (denominator <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator));
        }

        if (numerator <= 0)
        {
            return 0;
        }

        return (int)((numerator + (denominator / 2)) / denominator);
    }
}
