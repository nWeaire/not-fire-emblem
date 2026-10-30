using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using NotFireEmblem.Combat;
using NotFireEmblem.Core;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;
using NotFireEmblem.UI;

namespace NotFireEmblem.Battle;

/// <summary>
/// The player-phase UI state machine: Browse → MoveSelect → (Moving) → ActionMenu → TargetSelect / PickUnit /
/// SubMenu, plus MapMenu. It previews moves visually and only touches game state by building a command and handing
/// it to the BattleController. The action menu only offers what <see cref="ActionRules"/> allows from that cell.
/// </summary>
public partial class PlayerInputController : Node
{
    private enum Mode { Disabled, Browse, MoveSelect, Moving, ActionMenu, TargetSelect, PickUnit, SubMenu, MapMenu }

    private BattleController _b = null!;
    private Mode _mode = Mode.Disabled;

    /// <summary>The art picked straight from the action menu: target select opens with it chosen.</summary>
    private ArtAttack? _artPreference;

    private BattleUnit? _selected;
    private ReachMap? _reach;
    private GridPos _dest;
    private BattleUnit? _inspected;

    private readonly List<(string Label, Action Choose)> _menu = new();
    private int _menuIndex;
    /// <summary>What Cancel does from the current sub-menu or unit pick.</summary>
    private Action? _back;

    // Attack targeting
    private readonly List<BattleUnit> _targets = new();
    private int _targetIndex;
    private readonly List<ActionChoice> _choices = new();
    private int _choiceIndex;

    // Generic unit picking (heal, talk, trade, crest)
    private Action<BattleUnit>? _pickAction;
    private Func<BattleUnit, string>? _pickPreview;

    private GridPos? _lastMouseCell;
    private bool _dangerShown;

    private BattleState S => _b.State;

    public void Init(BattleController battle)
    {
        _b = battle;
        _b.Hud.MenuClicked += OnMenuClicked;
    }

    /// <summary>Starts (or resumes) player input for the Player phase.</summary>
    public void Begin()
    {
        // Undo any visual move preview that never turned into a command.
        foreach (var unit in S.Living)
        {
            var view = _b.ViewOf(unit.Id);
            if (view is not null)
                view.Position = _b.CellWorld(unit.Pos);
        }

        _selected = null;
        _reach = null;
        _inspected = null;
        _b.Hud.HideMenu();
        _b.Hud.HideForecast();
        ClearOverlays();
        RefreshDanger();
        _mode = Mode.Browse;

        var atCursor = VisibleUnitAt(_b.Cursor.Pos);
        bool cursorOnActiveUnit = atCursor is { Faction: Faction.Player } && atCursor.ActionState != ActionState.Done;
        var firstReady = ReadyUnits().FirstOrDefault();
        if (!cursorOnActiveUnit && firstReady is not null)
            MoveCursor(firstReady.Pos);
        else
            RefreshHover();
    }

    public void Disable()
    {
        _mode = Mode.Disabled;
        _b.Hud.HideMenu();
        ClearOverlays();
    }

    // ------------------------------------------------------------------ input

    public override void _UnhandledInput(InputEvent e)
    {
        if (_mode is Mode.Disabled or Mode.Moving || _b.Hud.Dialogue.IsPlaying)
            return;

        if (e.IsActionPressed(InputSetup.CamRotateLeft)) { _b.Camera.RotateStep(1); Handled(); return; }
        if (e.IsActionPressed(InputSetup.CamRotateRight)) { _b.Camera.RotateStep(-1); Handled(); return; }
        if (e.IsActionPressed(InputSetup.ZoomIn)) { _b.Camera.Zoom(-1); Handled(); return; }
        if (e.IsActionPressed(InputSetup.ZoomOut)) { _b.Camera.Zoom(1); Handled(); return; }

        if (e is InputEventMouseMotion motion)
        {
            OnMouseMove(motion.Position);
            return;
        }
        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
        {
            // Make sure the cursor is on the clicked cell before the confirm below.
            if (_mode is Mode.Browse or Mode.MoveSelect or Mode.TargetSelect or Mode.PickUnit)
            {
                _lastMouseCell = null;
                OnMouseMove(click.Position);
            }
        }

        if (ReadDirection(e) is { } dir) { OnDirection(dir); Handled(); return; }
        if (e.IsActionPressed(InputSetup.Confirm)) { OnConfirm(); Handled(); return; }
        if (e.IsActionPressed(InputSetup.Cancel)) { OnCancel(); Handled(); return; }
        if (e.IsActionPressed(InputSetup.NextUnit)) { OnNext(); Handled(); return; }
        if (e.IsActionPressed(InputSetup.EndTurn) && _mode == Mode.Browse) { Submit(new EndPhaseCommand()); Handled(); return; }
        if (e.IsActionPressed(InputSetup.ToggleDanger)) { _dangerShown = !_dangerShown; RefreshDanger(); Handled(); return; }
        if (e.IsActionPressed(InputSetup.ToggleBattleScenes))
        {
            _b.UseBattleScenes = !_b.UseBattleScenes;
            _b.Hud.SetBattleScenes(_b.UseBattleScenes);
            Handled();
        }
    }

    private void Handled() => GetViewport().SetInputAsHandled();

    private static GridPos? ReadDirection(InputEvent e)
    {
        if (e.IsActionPressed(InputSetup.CursorUp, true)) return GridPos.Up;
        if (e.IsActionPressed(InputSetup.CursorDown, true)) return GridPos.Down;
        if (e.IsActionPressed(InputSetup.CursorLeft, true)) return GridPos.Left;
        if (e.IsActionPressed(InputSetup.CursorRight, true)) return GridPos.Right;
        return null;
    }

    private void OnMouseMove(Vector2 screen)
    {
        if (PickCell(screen) is not { } cell || cell == _lastMouseCell)
            return;
        _lastMouseCell = cell;
        switch (_mode)
        {
            case Mode.Browse:
            case Mode.MoveSelect:
                MoveCursor(cell, follow: false);
                break;
            case Mode.TargetSelect:
                int index = _targets.FindIndex(t => t.Pos == cell);
                if (index >= 0 && index != _targetIndex)
                {
                    _targetIndex = index;
                    UpdateTarget();
                }
                break;
            case Mode.PickUnit:
                int pick = _targets.FindIndex(t => PickPos(t) == cell);
                if (pick >= 0 && pick != _targetIndex)
                {
                    _targetIndex = pick;
                    UpdatePick();
                }
                break;
        }
    }

    private GridPos? PickCell(Vector2 screen)
    {
        var camera = _b.Camera.Camera;
        var from = camera.ProjectRayOrigin(screen);
        var query = PhysicsRayQueryParameters3D.Create(from, from + camera.ProjectRayNormal(screen) * 500f);
        var hit = _b.GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0 || hit["collider"].AsGodotObject() is not Node node || !node.HasMeta(MapView.CellMeta))
            return null;
        var v = node.GetMeta(MapView.CellMeta).AsVector2I();
        return new GridPos(v.X, v.Y);
    }

    private void OnDirection(GridPos screenDir)
    {
        switch (_mode)
        {
            case Mode.Browse:
            case Mode.MoveSelect:
                MoveCursor(_b.Cursor.Pos + _b.Camera.ScreenToGrid(screenDir));
                break;
            case Mode.ActionMenu:
            case Mode.MapMenu:
            case Mode.SubMenu:
                if (screenDir.Y != 0 && _menu.Count > 0)
                {
                    _menuIndex = (_menuIndex + screenDir.Y + _menu.Count) % _menu.Count;
                    _b.Hud.SetMenuSelection(_menuIndex);
                }
                break;
            case Mode.TargetSelect:
            case Mode.PickUnit:
                int step = screenDir == GridPos.Right || screenDir == GridPos.Down ? 1 : -1;
                _targetIndex = (_targetIndex + step + _targets.Count) % _targets.Count;
                if (_mode == Mode.TargetSelect)
                    UpdateTarget();
                else
                    UpdatePick();
                break;
        }
    }

    private void OnConfirm()
    {
        switch (_mode)
        {
            case Mode.Browse:
                ConfirmBrowse();
                break;
            case Mode.MoveSelect:
                ConfirmMove();
                break;
            case Mode.ActionMenu:
            case Mode.MapMenu:
            case Mode.SubMenu:
                ChooseMenu(_menuIndex);
                break;
            case Mode.TargetSelect:
                Submit(new ActCommand(_selected!.Id, _dest, new AttackAction(_targets[_targetIndex].Id, _choices[_choiceIndex])));
                break;
            case Mode.PickUnit:
                _pickAction!(_targets[_targetIndex]);
                break;
        }
    }

    private void OnCancel()
    {
        switch (_mode)
        {
            case Mode.Browse:
                _inspected = null;
                ClearOverlays();
                break;
            case Mode.MoveSelect:
                var unit = _selected!;
                _selected = null;
                _reach = null;
                ClearOverlays();
                _mode = Mode.Browse;
                MoveCursor(unit.Pos);
                break;
            case Mode.ActionMenu:
                _b.Hud.HideMenu();
                if (_selected!.ActionState == ActionState.Moved)
                {
                    // Already committed to this cell by a trade: back to browsing.
                    Begin();
                    break;
                }
                var view = _b.ViewOf(_selected.Id);
                if (view is not null)
                    view.Position = _b.CellWorld(_selected.Pos);
                Select(_selected);
                MoveCursor(_dest);
                break;
            case Mode.TargetSelect:
            case Mode.PickUnit:
            case Mode.SubMenu:
                _b.Hud.HideForecast();
                _b.Hud.HideMenu();
                var back = _back;
                _back = null;
                if (back is not null)
                    back();
                else
                    OpenActionMenu();
                break;
            case Mode.MapMenu:
                _b.Hud.HideMenu();
                _mode = Mode.Browse;
                RefreshHover();
                break;
        }
    }

    private void OnNext()
    {
        if (_mode == Mode.Browse)
        {
            var ready = ReadyUnits().ToList();
            if (ready.Count == 0)
                return;
            int current = ready.FindIndex(u => u.Pos == _b.Cursor.Pos);
            MoveCursor(ready[(current + 1) % ready.Count].Pos);
        }
        else if (_mode == Mode.TargetSelect && _choices.Count > 1)
        {
            _choiceIndex = (_choiceIndex + 1) % _choices.Count;
            UpdateTarget();
        }
    }

    private void OnMenuClicked(int index)
    {
        if (_mode is not (Mode.ActionMenu or Mode.MapMenu or Mode.SubMenu) || index < 0 || index >= _menu.Count)
            return;
        _menuIndex = index;
        ChooseMenu(index);
    }

    // ------------------------------------------------------------------ browse & move

    private BattleUnit? VisibleUnitAt(GridPos p) =>
        S.UnitAt(p) is { } u && S.CanSee(Faction.Player, u) ? u : null;

    private void ConfirmBrowse()
    {
        var unit = VisibleUnitAt(_b.Cursor.Pos);
        if (unit is { Faction: Faction.Player, ActionState: ActionState.Ready })
        {
            Select(unit);
        }
        else if (unit is { Faction: Faction.Player, ActionState: ActionState.Moved })
        {
            // Traded already: act from where it stands.
            _selected = unit;
            _reach = null;
            _dest = unit.Pos;
            OpenActionMenu();
        }
        else if (unit is not null && unit.Faction != Faction.Player)
        {
            _inspected = _inspected == unit ? null : unit;
            if (_inspected is null)
                ClearOverlays();
            else
                ShowRange(_inspected, S.Reach(_inspected));
        }
        else if (unit is null)
        {
            OpenMenu(Mode.MapMenu, new List<(string, Action)>
            {
                ("End Turn", () => Submit(new EndPhaseCommand())),
                ("Settings", OpenSettings),
                ("Cancel", OnCancel),
            });
        }
    }

    /// <summary>The settings (volumes, text speed, battle scenes) over the battle; input returns to the map after.</summary>
    private void OpenSettings()
    {
        _b.Hud.HideMenu();
        _mode = Mode.Disabled;
        var modal = new Modal("Settings");
        modal.Body.AddChild(new SettingsPanel());
        modal.Body.AddChild(UiKit.Button("Close", modal.Close, 280));
        modal.Closed += () => _mode = Mode.Browse;
        _b.Hud.AddChild(modal);
    }

    private void Select(BattleUnit unit)
    {
        _selected = unit;
        _reach = S.Reach(unit);
        _inspected = null;
        _mode = Mode.MoveSelect;
        ShowRange(unit, _reach);
        RefreshHover();
    }

    private void ConfirmMove()
    {
        var unit = _selected!;
        var cell = _b.Cursor.Pos;
        if (cell == unit.Pos || _reach!.CanStop(cell))
        {
            Run(() => MoveThen(cell, null));
            return;
        }
        // Clicking an enemy in range: walk to the cheapest cell that can hit it, then go straight to targeting.
        var target = VisibleUnitAt(cell);
        if (target is not null && target.Faction.IsHostileTo(unit.Faction) && BestStandCell(target) is { } stand)
            Run(() => MoveThen(stand, target));
    }

    private async Task MoveThen(GridPos dest, BattleUnit? target)
    {
        var unit = _selected!;
        _mode = Mode.Moving;
        _dest = dest;
        ClearOverlays();
        var view = _b.ViewOf(unit.Id);
        if (view is not null && dest != unit.Pos)
            await view.MoveAlong(_reach!.PathTo(dest).Select(_b.CellWorld).ToList());
        if (_mode != Mode.Moving)
            return;
        MoveCursor(dest);
        if (target is not null && ActionRules.AttackTargets(S, unit, dest).Contains(target))
            OpenTargetSelect(target);
        else
            OpenActionMenu();
    }

    private GridPos? BestStandCell(BattleUnit target)
    {
        var unit = _selected!;
        var choices = CombatCalculator.AttackChoices(S.Db, unit).ToList();
        GridPos? best = null;
        int bestCost = int.MaxValue;
        foreach (var cell in _reach!.StopCells)
        {
            int distance = cell.DistanceTo(target.Pos);
            if (!choices.Any(c => CombatCalculator.InRange(S.Db, c, distance)))
                continue;
            int cost = _reach.CostTo(cell);
            if (cost < bestCost)
            {
                best = cell;
                bestCost = cost;
            }
        }
        return best;
    }

    // ------------------------------------------------------------------ the action menu

    private void OpenActionMenu()
    {
        var unit = _selected!;
        var from = _dest;
        var options = new List<(string, Action)>();
        // The ability each entry uses (arts, a lone healing spell), explained in the ability box when highlighted.
        var abilities = new Dictionary<int, AbilityInfo>();
        _artPreference = null;
        var attackTargets = ActionRules.AttackTargets(S, unit, from).ToList();
        if (attackTargets.Count > 0)
            options.Add(("Attack", () => OpenTargetSelect(null)));
        // Each ready combat art gets its own entry (session 9: Tab inside Attack was too hidden to find).
        foreach (var art in CombatCalculator.AttackChoices(S.Db, unit).OfType<ArtAttack>()
                     .Where(a => attackTargets.Any(t => CombatCalculator.InRange(S.Db, a, from.DistanceTo(t.Pos))))
                     .GroupBy(a => a.ArtId).Select(g => g.First()))
        {
            var def = S.Db.Art(art.ArtId);
            abilities[options.Count] = AbilityText.Art(def);
            options.Add(($"{def.Name} ({unit.UsesLeft.GetValueOrDefault(art.ArtId)}/{def.Uses})", () =>
            {
                _artPreference = art;
                OpenTargets(null, art.WeaponId, back: OpenActionMenu);
            }));
        }
        var healSpells = ActionRules.ReadySpells(S, unit).Where(sp => ActionRules.HealTargets(S, unit, from, sp.Id).Any()).ToList();
        if (healSpells.Count == 1)
            abilities[options.Count] = AbilityText.Spell(healSpells[0]);
        if (healSpells.Count > 0)
            options.Add(("Heal", () => OpenHeal(healSpells)));
        var talk = EventDirector.TalkTargets(S, unit, from).ToList();
        if (talk.Count > 0)
            options.Add(("Talk", () => OpenPick(talk, t => $"Talk to {t.Name}", t => Submit(new ActCommand(unit.Id, from, new TalkAction(t.Id))))));
        if (ActionRules.CanVisit(S, unit, from))
            options.Add(("Visit", () => Submit(new ActCommand(unit.Id, from, new VisitAction()))));
        if (ActionRules.CanSeize(S, unit, from))
            options.Add(("Seize", () => Submit(new ActCommand(unit.Id, from, new SeizeAction()))));
        if (ActionRules.CanEscape(S, unit, from))
            options.Add(("Escape", () => Submit(new ActCommand(unit.Id, from, new EscapeAction()))));
        var locks = ActionRules.Unlockable(S, unit, from).ToList();
        if (locks.Contains(from))
            options.Add(("Open Chest", () => Submit(new ActCommand(unit.Id, from, new UnlockAction(from)))));
        var doors = locks.Where(c => c != from).ToList();
        if (doors.Count == 1)
            options.Add(("Unlock", () => Submit(new ActCommand(unit.Id, from, new UnlockAction(doors[0])))));
        else if (doors.Count > 1)
            options.Add(("Unlock", () => OpenSubMenu(doors.Select(d => (DoorLabel(from, d),
                (Action)(() => Submit(new ActCommand(unit.Id, from, new UnlockAction(d)))))).ToList(), OpenActionMenu)));
        // Anyone armed can batter a locked door instead (three strikes); it's listed after Unlock, which is quicker.
        var breakable = ActionRules.BreakableDoors(S, unit, from).ToList();
        string StrikeLabel(GridPos d) => $"{ActionRules.DoorHits - S.DoorStrikes.GetValueOrDefault(d)} to go";
        if (breakable.Count == 1)
            options.Add(($"Break Door ({StrikeLabel(breakable[0])})", () => Submit(new ActCommand(unit.Id, from, new BreakDoorAction(breakable[0])))));
        else if (breakable.Count > 1)
            options.Add(("Break Door", () => OpenSubMenu(breakable.Select(d => ($"{DoorLabel(from, d)} ({StrikeLabel(d)})",
                (Action)(() => Submit(new ActCommand(unit.Id, from, new BreakDoorAction(d)))))).ToList(), OpenActionMenu)));
        var marks = ActionRules.StealTargets(S, unit, from).ToList();
        if (marks.Count > 0)
            options.Add(("Steal", () => OpenPick(marks, t => $"Steal from {t.Name}", OpenSteal)));
        var crest = ActionRules.ReadyCrestSkills(S, unit, from).ToList();
        if (crest.Count > 0)
            options.Add(("Crest", () => OpenCrest(crest)));
        var items = ActionRules.UsableItems(S, unit).ToList();
        if (items.Count > 0)
            options.Add(("Item", () => OpenItems(items)));
        var partners = ActionRules.TradePartners(S, unit, from).ToList();
        if (partners.Count > 0)
            options.Add(("Trade", () => OpenPick(partners, p => $"Trade with {p.Name}", OpenTrade)));
        options.Add(("Wait", () => Submit(new ActCommand(unit.Id, from, new WaitAction()))));

        _back = null;
        OpenMenu(Mode.ActionMenu, options, options.Select((_, i) => abilities.GetValueOrDefault(i)).ToList());
        ShowAttackRangeFrom(unit, from);
        _b.Hud.ShowUnit(S, unit);
    }

    private void OpenMenu(Mode mode, IReadOnlyList<(string Label, Action Choose)> options, IReadOnlyList<AbilityInfo?>? abilities = null)
    {
        _menu.Clear();
        _menu.AddRange(options);
        _menuIndex = 0;
        _mode = mode;
        _b.Hud.ShowMenu(_menu.Select(m => m.Label).ToList(), _menuIndex, abilities);
    }

    private void OpenSubMenu(IReadOnlyList<(string Label, Action Choose)> options, Action back, IReadOnlyList<AbilityInfo?>? abilities = null)
    {
        _b.Hud.HideMenu();
        OpenMenu(Mode.SubMenu, options, abilities);
        _back = back;
    }

    private void ChooseMenu(int index) => _menu[index].Choose();

    private void OpenHeal(List<SpellDef> spells)
    {
        var unit = _selected!;
        void PickFor(SpellDef spell) => OpenPick(ActionRules.HealTargets(S, unit, _dest, spell.Id).ToList(),
            t => $"{spell.Name} {t.Name}: +{System.Math.Min(spell.Power + S.Stats(unit).Mag / 2, S.Stats(t).HP - t.CurrentHp)} HP   (uses {unit.UsesLeft.GetValueOrDefault(spell.Id)})",
            t => Submit(new ActCommand(unit.Id, _dest, new HealAction(t.Id, spell.Id))));
        if (spells.Count == 1)
            PickFor(spells[0]);
        else
            OpenSubMenu(spells.Select(sp => ($"{sp.Name} ({unit.UsesLeft.GetValueOrDefault(sp.Id)})", (Action)(() => PickFor(sp)))).ToList(), OpenActionMenu,
                spells.Select(sp => (AbilityInfo?)AbilityText.Spell(sp)).ToList());
    }

    /// <summary>"North door" etc., from the acting unit's cell.</summary>
    private static string DoorLabel(GridPos from, GridPos door) =>
        (door.X - from.X, door.Y - from.Y) switch { (0, -1) => "North door", (1, 0) => "East door", (0, 1) => "South door", _ => "West door" };

    /// <summary>Steal: pick one of the target's items it isn't holding.</summary>
    private void OpenSteal(BattleUnit target)
    {
        var unit = _selected!;
        var from = _dest;
        OpenSubMenu(ActionRules.Stealable(target).Select(id => (S.Db.ItemName(id),
            (Action)(() => Submit(new ActCommand(unit.Id, from, new StealAction(target.Id, id)))))).ToList(), OpenActionMenu);
    }

    private void OpenItems(List<ItemDef> items)
    {
        var unit = _selected!;
        OpenSubMenu(items.Select(i => (i.Kind == ItemKind.Heal ? $"{i.Name} (heal {(i.Amount > 0 ? i.Amount : 99)})" : $"{i.Name} ({i.Stat} +{i.Amount})",
            (Action)(() => Submit(new ActCommand(unit.Id, _dest, new UseItemAction(i.Id)))))).ToList(), OpenActionMenu);
    }

    private void OpenCrest(List<CrestSkill> skills)
    {
        var unit = _selected!;
        var options = new List<(string, Action)>();
        foreach (var skill in skills)
        {
            string label = $"{skill} ({unit.UsesLeft.GetValueOrDefault(CrestSkills.UseKey(skill))})";
            options.Add((label, () =>
            {
                if (skill is CrestSkill.Rally or CrestSkill.Mend)
                {
                    Submit(new ActCommand(unit.Id, _dest, new CrestAction(skill, null)));
                    return;
                }
                var targets = ActionRules.CrestTargets(S, unit, _dest, skill) ?? new List<BattleUnit>();
                OpenPick(targets, t => $"{skill}: {t.Name}", t => Submit(new ActCommand(unit.Id, _dest, new CrestAction(skill, t.Id))),
                    () => OpenCrest(skills));
            }));
        }
        OpenSubMenu(options, OpenActionMenu, skills.Select(k => (AbilityInfo?)AbilityText.Crest(k)).ToList());
    }

    private void OpenTrade(BattleUnit partner)
    {
        var unit = _selected!;
        var options = new List<(string, Action)>();
        bool myRoom = unit.State.Inventory.Count < UnitState.MaxInventory;
        bool theirRoom = partner.State.Inventory.Count < UnitState.MaxInventory;
        foreach (var item in unit.State.Inventory.Distinct())
            if (theirRoom)
                options.Add(($"Give {S.Db.ItemName(item)}", () => Submit(new TradeCommand(unit.Id, _dest, partner.Id, new[] { item }, Array.Empty<string>()))));
        foreach (var item in partner.State.Inventory.Distinct())
            if (myRoom)
                options.Add(($"Take {S.Db.ItemName(item)}", () => Submit(new TradeCommand(unit.Id, _dest, partner.Id, Array.Empty<string>(), new[] { item }))));
        if (unit.State.Inventory.Count > 0 && partner.State.Inventory.Count > 0)
            options.Add(($"Swap {S.Db.ItemName(unit.State.Inventory[0])} ↔ {S.Db.ItemName(partner.State.Inventory[0])}",
                () => Submit(new TradeCommand(unit.Id, _dest, partner.Id, new[] { unit.State.Inventory[0] }, new[] { partner.State.Inventory[0] }))));
        options.Add(("Cancel", OpenActionMenu));
        OpenSubMenu(options, OpenActionMenu);
    }

    // ------------------------------------------------------------------ attack targeting

    /// <summary>The weapon picked for this attack; target select and the forecast only offer it (and its arts).</summary>
    private string? _weaponLock;

    /// <summary>
    /// Attack, step 1: which weapon. Only weapons that can reach an enemy from here are offered (with
    /// <paramref name="preferred"/>, ones that reach that enemy); with just one, the list is skipped.
    /// </summary>
    private void OpenTargetSelect(BattleUnit? preferred)
    {
        var unit = _selected!;
        var targets = preferred is null ? ActionRules.AttackTargets(S, unit, _dest).ToList() : new List<BattleUnit> { preferred };
        var weapons = CombatCalculator.UsableWeapons(S.Db, unit.State)
            .Where(w => targets.Any(t => CombatCalculator.InRange(w, _dest.DistanceTo(t.Pos)))).ToList();
        if (weapons.Count <= 1)
        {
            OpenTargets(preferred, weapons.FirstOrDefault()?.Id, back: null);
            return;
        }
        // The equipped weapon first, then inventory order.
        weapons = weapons.OrderBy(w => w.Id == unit.State.EquippedItemId ? 0 : 1).ToList();
        _b.Hud.HideForecast();
        OpenSubMenu(weapons.Select(w => (WeaponLabel(w), (Action)(() => OpenTargets(preferred, w.Id, back: () => OpenTargetSelect(preferred))))).ToList(),
            OpenActionMenu);
    }

    private static string WeaponLabel(WeaponDef w) =>
        $"{w.Name}   Mt {w.Might}  Hit {w.Hit}  Rng {(w.MinRange == w.MaxRange ? $"{w.MinRange}" : $"{w.MinRange}-{w.MaxRange}")}";

    /// <summary>Attack, step 2: which enemy, among those <paramref name="weaponId"/> can reach.</summary>
    private void OpenTargets(BattleUnit? preferred, string? weaponId, Action? back)
    {
        _weaponLock = weaponId;
        var weapon = weaponId is null ? null : S.Db.TryWeapon(weaponId);
        _targets.Clear();
        _targets.AddRange(ActionRules.AttackTargets(S, _selected!, _dest)
            .Where(t => weapon is null || CombatCalculator.InRange(weapon, _dest.DistanceTo(t.Pos)))
            .OrderBy(t => t.Pos.X).ThenBy(t => t.Pos.Y));
        if (_targets.Count == 0)
        {
            OpenActionMenu();
            return;
        }
        _targetIndex = preferred is null ? 0 : Math.Max(0, _targets.IndexOf(preferred));
        _choices.Clear();
        _back = back;
        _mode = Mode.TargetSelect;
        _b.Hud.HideMenu();
        _b.RangeOverlay.ShowCells(_targets.Select(t => (t.Pos, CellOverlay.AttackColor)).ToList());
        UpdateTarget();
    }

    private void UpdateTarget()
    {
        var unit = _selected!;
        var target = _targets[_targetIndex];
        var previous = _choices.Count > 0 ? _choices[_choiceIndex] : null;
        int distance = _dest.DistanceTo(target.Pos);
        _choices.Clear();
        _choices.AddRange(CombatCalculator.AttackChoices(S.Db, unit)
            .Where(c => CombatCalculator.InRange(S.Db, c, distance) && (_weaponLock is null || c.WeaponId == _weaponLock)));
        string? equipped = _weaponLock ?? unit.State.EquippedItemId;
        _choiceIndex = Math.Max(0, previous is not null && _choices.Contains(previous)
            ? _choices.IndexOf(previous)
            : _artPreference is not null && _choices.Contains(_artPreference)
            ? _choices.IndexOf(_artPreference)
            : _choices.FindIndex(c => c is WeaponAttack && c.WeaponId == equipped));

        _b.Cursor.MoveTo(target.Pos);
        _b.Camera.Focus((_b.CellWorld(_dest) + _b.CellWorld(target.Pos)) / 2f);
        _b.ViewOf(unit.Id)?.FaceToward(_b.CellWorld(target.Pos));
        var choice = _choices[_choiceIndex];
        var forecast = CombatCalculator.Forecast(S, unit, _dest, target, choice);
        string uses = choice is ArtAttack art ? $"   ({unit.UsesLeft.GetValueOrDefault(art.ArtId)} uses left)" : "";
        string hint = (_choices.Count > 1 ? "Tab: switch art   " : "") + (_targets.Count > 1 ? "Arrows: switch target   " : "") + "Z: attack   X: back" + uses;
        if (S.HasFriendBeside(unit, _dest))
            hint = $"Ally beside you: +{BattleState.AdjacentHit} Hit, +{BattleState.AdjacentAvoid} Avoid\n" + hint;
        _b.Hud.ShowForecast(forecast, hint);
        _b.Hud.ShowAbility(choice is ArtAttack chosenArt ? AbilityText.Art(S.Db.Art(chosenArt.ArtId)) : null);
        _b.Hud.ShowUnit(S, target);
        _b.Hud.ShowTerrain(S, target.Pos);
    }

    // ------------------------------------------------------------------ generic unit picking

    private void OpenPick(List<BattleUnit> targets, Func<BattleUnit, string> preview, Action<BattleUnit> action, Action? back = null)
    {
        if (targets.Count == 0)
        {
            OpenActionMenu();
            return;
        }
        _targets.Clear();
        _targets.AddRange(targets.OrderBy(t => PickPos(t).X).ThenBy(t => PickPos(t).Y));
        _targetIndex = 0;
        _pickAction = action;
        _pickPreview = preview;
        _back = back ?? OpenActionMenu;
        _mode = Mode.PickUnit;
        _b.Hud.HideMenu();
        _b.RangeOverlay.ShowCells(_targets.Select(t => (PickPos(t), CellOverlay.HealColor)).ToList());
        UpdatePick();
    }

    /// <summary>Where a pick target stands: the acting unit itself (a self-heal) is already on its destination.</summary>
    private GridPos PickPos(BattleUnit t) => t == _selected ? _dest : t.Pos;

    private void UpdatePick()
    {
        var target = _targets[_targetIndex];
        _b.Cursor.MoveTo(PickPos(target));
        _b.Camera.Focus(_b.CellWorld(PickPos(target)));
        if (target != _selected)
            _b.ViewOf(_selected!.Id)?.FaceToward(_b.CellWorld(target.Pos));
        _b.Hud.ShowPreview(_pickPreview!(target) + (_targets.Count > 1 ? "\nArrows: switch   Z: confirm   X: back" : "\nZ: confirm   X: back"));
        _b.Hud.ShowUnit(S, target);
    }

    private void Submit(IBattleCommand command)
    {
        _b.Hud.HideMenu();
        _b.Hud.HideForecast();
        ClearOverlays();
        _mode = Mode.Disabled;
        _back = null;
        _b.SubmitPlayerCommand(command);
    }

    // ------------------------------------------------------------------ cursor, hover & overlays

    private IEnumerable<BattleUnit> ReadyUnits() =>
        S.LivingOf(Faction.Player).Where(u => u.ActionState != ActionState.Done).OrderBy(u => u.Id);

    private void MoveCursor(GridPos p, bool follow = true)
    {
        if (!S.Grid.InBounds(p))
            return;
        _b.Cursor.MoveTo(p);
        if (follow)
            _b.Camera.Focus(_b.CellWorld(p));
        RefreshHover();
    }

    private void RefreshHover()
    {
        var cell = _b.Cursor.Pos;
        _b.Hud.ShowTerrain(S, cell);
        if (_mode == Mode.Browse)
        {
            _b.Hud.ShowUnit(S, VisibleUnitAt(cell));
        }
        else if (_mode == Mode.MoveSelect && _selected is not null && _reach is not null)
        {
            _b.Hud.ShowUnit(S, VisibleUnitAt(cell) ?? _selected);
            GridPos? end = _reach.CanStop(cell) ? cell
                : VisibleUnitAt(cell) is { } t && t.Faction.IsHostileTo(_selected.Faction) ? BestStandCell(t) : null;
            if (end is { } e && e != _selected.Pos)
                _b.PathOverlay.ShowCells(_reach.PathTo(e).Select(p => (p, CellOverlay.PathColor)).ToList());
            else
                _b.PathOverlay.Clear();
        }
    }

    private void ShowRange(BattleUnit unit, ReachMap reach)
    {
        var move = reach.StopCells.ToHashSet();
        var attack = BattleQueries.AttackCells(S, unit, reach);
        attack.ExceptWith(move);
        // Cells only this unit can set off (Aldric's seal plinths) show in yellow, in reach or not.
        var goals = unit.Faction == Faction.Player ? EventDirector.EnterGoals(S, unit).ToHashSet() : new HashSet<GridPos>();
        var cells = move.Where(p => !goals.Contains(p)).Select(p => (p, CellOverlay.MoveColor))
            .Concat(attack.Where(p => !goals.Contains(p)).Select(p => (p, CellOverlay.AttackColor)))
            .Concat(goals.Select(p => (p, CellOverlay.ObjectiveColor))).ToList();
        _b.RangeOverlay.ShowCells(cells);
        _b.PathOverlay.Clear();
    }

    private void ShowAttackRangeFrom(BattleUnit unit, GridPos from)
    {
        var (min, max) = BattleQueries.WeaponRange(S, unit);
        var cells = max <= 0
            ? new List<(GridPos, Color)>()
            : RangeService.CellsInRange(S.Grid, from, Math.Max(1, min), max).Select(p => (p, CellOverlay.AttackColor)).ToList();
        _b.RangeOverlay.ShowCells(cells);
        _b.PathOverlay.Clear();
    }

    private void RefreshDanger()
    {
        if (_dangerShown)
            _b.DangerOverlay.ShowCells(BattleQueries.DangerZone(S, Faction.Player).Select(p => (p, CellOverlay.DangerColor)).ToList());
        else
            _b.DangerOverlay.Clear();
    }

    private void ClearOverlays()
    {
        _b.RangeOverlay.Clear();
        _b.PathOverlay.Clear();
    }

    private static async void Run(Func<Task> flow)
    {
        try
        {
            await flow();
        }
        catch (Exception e)
        {
            GD.PushError($"Player input flow failed: {e}");
        }
    }
}
