using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using NotFireEmblem.Combat;
using NotFireEmblem.Core;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>
/// The player-phase UI state machine: Browse → MoveSelect → (Moving) → ActionMenu → TargetSelect, plus MapMenu.
/// It previews moves visually and only touches game state by building a command and handing it to the BattleController.
/// </summary>
public partial class PlayerInputController : Node
{
    private enum Mode { Disabled, Browse, MoveSelect, Moving, ActionMenu, TargetSelect, MapMenu }

    private const string AttackOption = "Attack";
    private const string WaitOption = "Wait";
    private const string EndTurnOption = "End Turn";
    private const string CancelOption = "Cancel";

    private BattleController _b = null!;
    private Mode _mode = Mode.Disabled;

    private BattleUnit? _selected;
    private ReachMap? _reach;
    private GridPos _dest;
    private BattleUnit? _inspected;

    private readonly List<string> _menu = new();
    private int _menuIndex;

    private readonly List<BattleUnit> _targets = new();
    private int _targetIndex;
    private readonly List<WeaponDef> _weapons = new();
    private int _weaponIndex;

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

        var atCursor = S.UnitAt(_b.Cursor.Pos);
        bool cursorOnReadyUnit = atCursor is { Faction: Faction.Player, ActionState: ActionState.Ready };
        var firstReady = ReadyUnits().FirstOrDefault();
        if (!cursorOnReadyUnit && firstReady is not null)
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
        if (_mode is Mode.Disabled or Mode.Moving)
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
            if (_mode is Mode.Browse or Mode.MoveSelect or Mode.TargetSelect)
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
                if (screenDir.Y != 0 && _menu.Count > 0)
                {
                    _menuIndex = (_menuIndex + screenDir.Y + _menu.Count) % _menu.Count;
                    _b.Hud.SetMenuSelection(_menuIndex);
                }
                break;
            case Mode.TargetSelect:
                int step = screenDir == GridPos.Right || screenDir == GridPos.Down ? 1 : -1;
                _targetIndex = (_targetIndex + step + _targets.Count) % _targets.Count;
                UpdateTarget();
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
                ChooseMenu(_menuIndex);
                break;
            case Mode.TargetSelect:
                var target = _targets[_targetIndex];
                var weapon = _weapons[_weaponIndex];
                Submit(new ActCommand(_selected!.Id, _dest, new AttackAction(target.Id, new WeaponAttack(weapon.Id))));
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
                var view = _b.ViewOf(_selected!.Id);
                if (view is not null)
                    view.Position = _b.CellWorld(_selected.Pos);
                _b.Hud.HideMenu();
                Select(_selected);
                MoveCursor(_dest);
                break;
            case Mode.TargetSelect:
                _b.Hud.HideForecast();
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
        else if (_mode == Mode.TargetSelect && _weapons.Count > 1)
        {
            _weaponIndex = (_weaponIndex + 1) % _weapons.Count;
            UpdateTarget();
        }
    }

    private void OnMenuClicked(int index)
    {
        if (_mode is not (Mode.ActionMenu or Mode.MapMenu) || index < 0 || index >= _menu.Count)
            return;
        _menuIndex = index;
        ChooseMenu(index);
    }

    // ------------------------------------------------------------------ browse & move

    private void ConfirmBrowse()
    {
        var unit = S.UnitAt(_b.Cursor.Pos);
        if (unit is { Faction: Faction.Player, ActionState: ActionState.Ready })
        {
            Select(unit);
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
            OpenMenu(Mode.MapMenu, new[] { EndTurnOption, CancelOption });
        }
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
        var target = S.UnitAt(cell);
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
        if (target is not null && TargetsFrom(unit, dest).Contains(target))
            OpenTargetSelect(target);
        else
            OpenActionMenu();
    }

    private GridPos? BestStandCell(BattleUnit target)
    {
        var unit = _selected!;
        var weapons = CombatCalculator.UsableWeapons(S.Db, unit.State).ToList();
        GridPos? best = null;
        int bestCost = int.MaxValue;
        foreach (var cell in _reach!.StopCells)
        {
            int distance = cell.DistanceTo(target.Pos);
            if (!weapons.Any(w => CombatCalculator.InRange(w, distance)))
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

    // ------------------------------------------------------------------ menus & targeting

    private void OpenActionMenu()
    {
        var options = new List<string>();
        if (TargetsFrom(_selected!, _dest).Any())
            options.Add(AttackOption);
        options.Add(WaitOption);
        OpenMenu(Mode.ActionMenu, options);
        ShowAttackRangeFrom(_selected!, _dest);
        _b.Hud.ShowUnit(S, _selected);
    }

    private void OpenMenu(Mode mode, IReadOnlyList<string> options)
    {
        _menu.Clear();
        _menu.AddRange(options);
        _menuIndex = 0;
        _mode = mode;
        _b.Hud.ShowMenu(_menu, _menuIndex);
    }

    private void ChooseMenu(int index)
    {
        switch (_menu[index])
        {
            case AttackOption:
                _b.Hud.HideMenu();
                OpenTargetSelect(null);
                break;
            case WaitOption:
                Submit(new ActCommand(_selected!.Id, _dest, new WaitAction()));
                break;
            case EndTurnOption:
                Submit(new EndPhaseCommand());
                break;
            case CancelOption:
                OnCancel();
                break;
        }
    }

    private void OpenTargetSelect(BattleUnit? preferred)
    {
        _targets.Clear();
        _targets.AddRange(TargetsFrom(_selected!, _dest).OrderBy(t => t.Pos.X).ThenBy(t => t.Pos.Y));
        if (_targets.Count == 0)
        {
            OpenActionMenu();
            return;
        }
        _targetIndex = preferred is null ? 0 : Math.Max(0, _targets.IndexOf(preferred));
        _weapons.Clear();
        _mode = Mode.TargetSelect;
        _b.Hud.HideMenu();
        _b.RangeOverlay.ShowCells(_targets.Select(t => (t.Pos, CellOverlay.AttackColor)).ToList());
        UpdateTarget();
    }

    private void UpdateTarget()
    {
        var unit = _selected!;
        var target = _targets[_targetIndex];
        var previous = _weapons.Count > 0 ? _weapons[_weaponIndex] : null;
        int distance = _dest.DistanceTo(target.Pos);
        _weapons.Clear();
        _weapons.AddRange(CombatCalculator.UsableWeapons(S.Db, unit.State).Where(w => CombatCalculator.InRange(w, distance)));
        string? equipped = unit.State.EquippedItemId;
        _weaponIndex = Math.Max(0, previous is not null && _weapons.Contains(previous)
            ? _weapons.IndexOf(previous)
            : _weapons.FindIndex(w => w.Id == equipped));

        _b.Cursor.MoveTo(target.Pos);
        _b.Camera.Focus((_b.CellWorld(_dest) + _b.CellWorld(target.Pos)) / 2f);
        _b.ViewOf(unit.Id)?.FaceToward(_b.CellWorld(target.Pos));
        var forecast = CombatCalculator.Forecast(S, unit, _dest, target, new WeaponAttack(_weapons[_weaponIndex].Id));
        string hint = (_weapons.Count > 1 ? "Tab: switch weapon   " : "") + (_targets.Count > 1 ? "Arrows: switch target   " : "") + "Z: attack   X: back";
        _b.Hud.ShowForecast(forecast, hint);
        _b.Hud.ShowUnit(S, target);
        _b.Hud.ShowTerrain(S, target.Pos);
    }

    private IEnumerable<BattleUnit> TargetsFrom(BattleUnit unit, GridPos from) => BattleQueries.TargetsFrom(S, unit, from);

    private void Submit(IBattleCommand command)
    {
        _b.Hud.HideMenu();
        _b.Hud.HideForecast();
        ClearOverlays();
        _mode = Mode.Disabled;
        _b.SubmitPlayerCommand(command);
    }

    // ------------------------------------------------------------------ cursor, hover & overlays

    private IEnumerable<BattleUnit> ReadyUnits() =>
        S.LivingOf(Faction.Player).Where(u => u.ActionState == ActionState.Ready).OrderBy(u => u.Id);

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
            _b.Hud.ShowUnit(S, S.UnitAt(cell));
        }
        else if (_mode == Mode.MoveSelect && _selected is not null && _reach is not null)
        {
            _b.Hud.ShowUnit(S, S.UnitAt(cell) ?? _selected);
            GridPos? end = _reach.CanStop(cell) ? cell
                : S.UnitAt(cell) is { } t && t.Faction.IsHostileTo(_selected.Faction) ? BestStandCell(t) : null;
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
        var cells = move.Select(p => (p, CellOverlay.MoveColor)).Concat(attack.Select(p => (p, CellOverlay.AttackColor))).ToList();
        _b.RangeOverlay.ShowCells(cells);
        _b.PathOverlay.Clear();
    }

    private void ShowAttackRangeFrom(BattleUnit unit, GridPos from)
    {
        var (min, max) = BattleQueries.WeaponRange(S, unit);
        var cells = RangeService.CellsInRange(S.Grid, from, Math.Max(1, min), max).Select(p => (p, CellOverlay.AttackColor)).ToList();
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
