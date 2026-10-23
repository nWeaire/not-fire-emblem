using NotFireEmblem.Units;

namespace NotFireEmblem.Tests;

public class AbilityTextTests
{
    [Fact]
    public void Art_ListsItsModifiers_Effectiveness_AndUses()
    {
        var sunder = new ArtDef("sunder", "Sunder", WeaponType.Sword, 3, -5, 5, 0, 2, EffectiveAgainst: new[] { "Armored" });
        var info = AbilityText.Art(sunder);
        Assert.Equal("Sunder", info.Title);
        Assert.Contains("A sword technique: Might +3, Hit -5, Crit +5.", info.Body);
        Assert.Contains("Effective against Armored units: triple might.", info.Body);
        Assert.Contains("2 uses a battle.", info.Body);

        var sky = AbilityText.Art(new ArtDef("sky_lance", "Sky Lance", WeaponType.Lance, 2, 0, 0, 0, 1, NoCounter: true));
        Assert.Contains("can't strike back", sky.Body);
        Assert.Contains("Once a battle.", sky.Body);
    }

    [Fact]
    public void Spell_And_CrestSkill_SayWhoTheyReach()
    {
        Assert.Contains("to the caster", AbilityText.Spell(new SpellDef("prayer", "Prayer", 10, 0, 0, 3)).Body);
        Assert.Contains("an ally 1-3 tiles away", AbilityText.Spell(new SpellDef("physic", "Physic", 8, 1, 3, 2)).Body);
        var rally = AbilityText.Crest(CrestSkill.Rally);
        Assert.Equal("Rally (crest skill)", rally.Title);
        Assert.StartsWith("Allies within 2 get +2 to all stats", rally.Body);
    }
}
