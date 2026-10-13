using NotFireEmblem.Grid;

namespace NotFireEmblem.Units;

/// <summary>A unit's battle-only wrapper: position, faction, turn state and current HP.</summary>
public sealed class BattleUnit
{
    public required string Id { get; init; }
    public required UnitState State { get; init; }
    public required Faction Faction { get; init; }
    public GridPos Pos { get; set; }
    /// <summary>Where the unit started the battle (anchors Boss movement).</summary>
    public GridPos Home { get; init; }
    public ActionState ActionState { get; set; } = ActionState.Ready;
    public int CurrentHp { get; set; }
    public bool IsBoss { get; init; }
    public AiProfile AiProfile { get; init; } = AiProfile.Aggressive;
    public bool IsDefeated { get; set; }

    public string Name => State.Def.Name;
    public bool IsLord => State.Def.IsLord;

    public override string ToString() => $"{Name}[{Id}]@{Pos}";
}
