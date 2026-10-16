using NotFireEmblem.Audio;
using NotFireEmblem.Units;

namespace NotFireEmblem.Tests;

public class AudioCuesTests
{
    [Fact]
    public void PicksSoundsForWeaponsHitsAndPhases()
    {
        var db = TestContent.Create();
        var sword = UnitState.FromDef(new UnitDef("a", "A", "merc", 1, default, default, new[] { "sword" }, false));
        var bow = UnitState.FromDef(new UnitDef("b", "B", "archer", 1, default, default, new[] { "bow" }, false));
        sword.EquippedIndex = 0;
        bow.EquippedIndex = 0;
        Assert.Equal("sword_swing", AudioCues.Swing(db, sword));
        Assert.Equal("bow_shot", AudioCues.Swing(db, bow));
        Assert.Equal("miss", AudioCues.Impact(hit: false, crit: true));
        Assert.Equal("hit_heavy", AudioCues.Impact(hit: true, crit: true));
        Assert.Equal("hit", AudioCues.Impact(hit: true, crit: false));
        Assert.Equal("enemy_phase", AudioCues.PhaseMusic(Faction.Enemy));
        Assert.Equal("player_phase", AudioCues.PhaseMusic(Faction.Ally));
    }
}
