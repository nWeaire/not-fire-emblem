using System;

namespace NotFireEmblem.Units;

/// <summary>An immutable set of the ten unit stats. Used for bases, growths, gains and caps alike.</summary>
public readonly record struct StatBlock(int HP, int Str, int Mag, int Dex, int Spd, int Lck, int Def, int Res, int Mov, int Cha = 0)
{
    public static readonly Stat[] All = Enum.GetValues<Stat>();

    public int this[Stat stat] => stat switch
    {
        Stat.HP => HP,
        Stat.Str => Str,
        Stat.Mag => Mag,
        Stat.Dex => Dex,
        Stat.Spd => Spd,
        Stat.Lck => Lck,
        Stat.Def => Def,
        Stat.Res => Res,
        Stat.Mov => Mov,
        Stat.Cha => Cha,
        _ => throw new ArgumentOutOfRangeException(nameof(stat)),
    };

    public StatBlock With(Stat stat, int value) => stat switch
    {
        Stat.HP => this with { HP = value },
        Stat.Str => this with { Str = value },
        Stat.Mag => this with { Mag = value },
        Stat.Dex => this with { Dex = value },
        Stat.Spd => this with { Spd = value },
        Stat.Lck => this with { Lck = value },
        Stat.Def => this with { Def = value },
        Stat.Res => this with { Res = value },
        Stat.Mov => this with { Mov = value },
        Stat.Cha => this with { Cha = value },
        _ => throw new ArgumentOutOfRangeException(nameof(stat)),
    };

    public static StatBlock FromArray(int[] values)
    {
        var block = default(StatBlock);
        for (int i = 0; i < All.Length && i < values.Length; i++)
            block = block.With(All[i], values[i]);
        return block;
    }

    public int[] ToArray()
    {
        var values = new int[All.Length];
        for (int i = 0; i < All.Length; i++)
            values[i] = this[All[i]];
        return values;
    }

    public static StatBlock Combine(StatBlock a, StatBlock b, Func<int, int, int> op)
    {
        var block = default(StatBlock);
        foreach (var s in All)
            block = block.With(s, op(a[s], b[s]));
        return block;
    }

    public static StatBlock operator +(StatBlock a, StatBlock b) => Combine(a, b, (x, y) => x + y);

    public static StatBlock operator -(StatBlock a, StatBlock b) => Combine(a, b, (x, y) => x - y);

    /// <summary>Clamps each stat to [0, cap]. A cap of 0 means "uncapped".</summary>
    public StatBlock ClampTo(StatBlock caps) => Combine(this, caps, (v, cap) => Math.Max(0, cap > 0 ? Math.Min(v, cap) : v));
}
