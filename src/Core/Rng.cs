namespace NotFireEmblem.Core;

/// <summary>Random source for game logic. Injected so tests can script exact rolls.</summary>
public interface IRng
{
    /// <summary>Uniform integer in [0, n).</summary>
    int Next(int n);

    /// <summary>Single-roll check: true with exactly <paramref name="percent"/>% chance.</summary>
    bool Roll1(int percent) => Next(100) < percent;

    /// <summary>2RN check: the average of two rolls must be below the percentage.</summary>
    bool Roll2(int percent) => Next(100) + Next(100) < 2 * percent;
}

/// <summary>PCG32 (XSH-RR). Small, fast, deterministic and serializable via <see cref="State"/>.</summary>
public sealed class Rng : IRng
{
    private const ulong Multiplier = 6364136223846793005UL;
    private const ulong Increment = 1442695040888963407UL;

    public ulong State { get; set; }

    public Rng(ulong seed)
    {
        State = 0;
        NextUInt();
        State += seed;
        NextUInt();
    }

    public uint NextUInt()
    {
        ulong old = State;
        State = unchecked(old * Multiplier + Increment);
        uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
        int rot = (int)(old >> 59);
        return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
    }

    public int Next(int n)
    {
        if (n <= 1)
            return 0;
        // Rejection sampling to avoid modulo bias.
        uint bound = (uint)n;
        uint threshold = (uint)(-bound % bound);
        while (true)
        {
            uint r = NextUInt();
            if (r >= threshold)
                return (int)(r % bound);
        }
    }
}
