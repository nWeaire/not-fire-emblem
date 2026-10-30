using System.Collections.Generic;
using System.Linq;

namespace NotFireEmblem.Units;

/// <summary>A title and a few lines explaining an ability (the battle HUD's ability box, top right).</summary>
public sealed record AbilityInfo(string Title, string Body);

/// <summary>
/// Plain-language explanations of combat arts, healing spells and Rowan's crest skills, built from their numbers so
/// they never drift from the data (session 13, user request: explain every ability on every character).
/// </summary>
public static class AbilityText
{
    public static AbilityInfo Art(ArtDef art)
    {
        var lines = new List<string>();
        var mods = new[] { ("Might", art.MightMod), ("Hit", art.HitMod), ("Crit", art.CritMod) }
            .Where(m => m.Item2 != 0).Select(m => $"{m.Item1} {m.Item2:+0;-0}").ToList();
        lines.Add(mods.Count > 0 ? $"A {Type(art.Type)} technique: {string.Join(", ", mods)}." : $"A {Type(art.Type)} technique.");
        if (art.RangeBonus > 0)
            lines.Add($"Reaches {art.RangeBonus} tile{(art.RangeBonus == 1 ? "" : "s")} further than the weapon.");
        if (art.EffectiveAgainst is { Count: > 0 } targets)
            lines.Add($"Effective against {string.Join(" and ", targets)} units: {(art.Type == WeaponType.BlackMagic ? "double" : "triple")} might.");
        if (art.Brave)
            lines.Add("Strikes twice before the target can answer.");
        if (art.Pierce)
            lines.Add($"Ignores half the target's {(art.Type == WeaponType.BlackMagic ? "Res" : "Def")}.");
        if (art.NoCounter)
            lines.Add("The target can't strike back.");
        lines.Add(Uses(art.Uses));
        return new AbilityInfo(art.Name, string.Join("\n", lines));
    }

    public static AbilityInfo Spell(SpellDef spell)
    {
        string who = spell.MaxRange == 0 ? "the caster"
            : spell.MinRange == spell.MaxRange && spell.MaxRange == 1 ? "an adjacent ally"
            : $"an ally {spell.MinRange}-{spell.MaxRange} tiles away";
        return new AbilityInfo(spell.Name, $"Restores {spell.Power} + half the caster's Mag in HP to {who}.\n{Uses(spell.Uses)}");
    }

    public static AbilityInfo Crest(CrestSkill skill)
    {
        // CrestSkills.Describe reads "Ward: an adjacent ally gets ..."; the box shows the name as its title.
        string text = CrestSkills.Describe(skill);
        int colon = text.IndexOf(':');
        string body = colon >= 0 ? text[(colon + 1)..].Trim() : text;
        body = char.ToUpperInvariant(body[0]) + body[1..];
        return new AbilityInfo($"{skill} (crest skill)", $"{body}\n{Uses(CrestSkills.Uses(skill))}");
    }

    private static string Uses(int uses) => uses == 1 ? "Once a battle." : $"{uses} uses a battle.";

    private static string Type(WeaponType type) => type switch
    {
        WeaponType.BlackMagic => "magic",
        WeaponType.WhiteMagic => "light magic",
        _ => type.ToString().ToLowerInvariant(),
    };
}
