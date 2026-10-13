using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using NotFireEmblem.Battle;
using NotFireEmblem.Combat;
using NotFireEmblem.Core;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.UI;

/// <summary>
/// The battle HUD: objective/turn panel, unit and terrain info, action menu, combat forecast, phase banner,
/// controls hint and the end-of-battle overlay. Built in code; it only displays what it is given.
/// </summary>
public partial class BattleHud : CanvasLayer
{
    private static readonly Color PanelBg = new(0.08f, 0.09f, 0.13f, 0.85f);
    private static readonly Color Accent = new(1f, 0.85f, 0.35f);

    private Label _objective = null!;
    private PanelContainer _unitPanel = null!;
    private Label _unitTitle = null!;
    private ProgressBar _unitHp = null!;
    private Label _unitHpText = null!;
    private Label _unitStats = null!;
    private Label _unitItems = null!;
    private PanelContainer _terrainPanel = null!;
    private Label _terrainText = null!;
    private PanelContainer _menuPanel = null!;
    private VBoxContainer _menuList = null!;
    private readonly List<Button> _menuButtons = new();
    private readonly List<string> _menuOptions = new();
    private PanelContainer _forecastPanel = null!;
    private GridContainer _forecastGrid = null!;
    private Label _forecastHint = null!;
    private Label _banner = null!;
    private ColorRect _flash = null!;
    private Control _result = null!;
    private Label _resultLabel = null!;

    /// <summary>Raised when a menu entry is clicked with the mouse.</summary>
    public event Action<int>? MenuClicked;

    public event Action? RestartRequested;

    public override void _Ready()
    {
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // Objective / turn (top-left)
        var objPanel = Panel(root, Control.LayoutPreset.TopLeft, new Vector2(16, 16));
        _objective = new Label();
        objPanel.AddChild(_objective);

        // Unit info (bottom-left)
        _unitPanel = Panel(root, Control.LayoutPreset.BottomLeft, new Vector2(16, -34));
        var unitBox = new VBoxContainer();
        _unitPanel.AddChild(unitBox);
        _unitTitle = new Label();
        _unitTitle.AddThemeFontSizeOverride("font_size", 20);
        unitBox.AddChild(_unitTitle);
        var hpRow = new HBoxContainer();
        unitBox.AddChild(hpRow);
        _unitHp = new ProgressBar { CustomMinimumSize = new Vector2(180, 14), ShowPercentage = false, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        _unitHp.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color(0.3f, 0.85f, 0.4f) });
        hpRow.AddChild(_unitHp);
        _unitHpText = new Label();
        hpRow.AddChild(_unitHpText);
        _unitStats = new Label();
        _unitStats.AddThemeFontSizeOverride("font_size", 14);
        unitBox.AddChild(_unitStats);
        _unitItems = new Label();
        _unitItems.AddThemeFontSizeOverride("font_size", 14);
        unitBox.AddChild(_unitItems);
        _unitPanel.Visible = false;

        // Terrain (bottom-right)
        _terrainPanel = Panel(root, Control.LayoutPreset.BottomRight, new Vector2(-16, -34));
        _terrainText = new Label();
        _terrainPanel.AddChild(_terrainText);

        // Action menu (right)
        _menuPanel = Panel(root, Control.LayoutPreset.CenterRight, new Vector2(-40, 0));
        _menuList = new VBoxContainer();
        _menuPanel.AddChild(_menuList);
        _menuPanel.Visible = false;

        // Forecast (top-centre)
        _forecastPanel = Panel(root, Control.LayoutPreset.CenterTop, new Vector2(0, 16));
        var forecastBox = new VBoxContainer();
        _forecastPanel.AddChild(forecastBox);
        _forecastGrid = new GridContainer { Columns = 3 };
        _forecastGrid.AddThemeConstantOverride("h_separation", 24);
        forecastBox.AddChild(_forecastGrid);
        _forecastHint = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1, 1, 1, 0.7f) };
        _forecastHint.AddThemeFontSizeOverride("font_size", 13);
        forecastBox.AddChild(_forecastHint);
        _forecastPanel.Visible = false;

        // Controls hint (bottom-centre)
        var hint = new Label
        {
            Text = "Z / LMB: select   X / RMB: back   WASD / arrows: cursor   Q / E: rotate   wheel / +/-: zoom   Tab: next unit   G: danger zone   B: battle scenes   T: end turn",
            Modulate = new Color(1, 1, 1, 0.65f),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        hint.AddThemeFontSizeOverride("font_size", 13);
        hint.AddThemeConstantOverride("outline_size", 4);
        hint.AddThemeColorOverride("font_outline_color", Colors.Black);
        root.AddChild(hint);
        hint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        hint.Position += new Vector2(0, -6);
        hint.GrowHorizontal = Control.GrowDirection.Both;

        // Phase banner (centre)
        _banner = new Label { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Visible = false };
        _banner.AddThemeFontSizeOverride("font_size", 56);
        _banner.AddThemeConstantOverride("outline_size", 12);
        _banner.AddThemeColorOverride("font_outline_color", Colors.Black);
        root.AddChild(_banner);
        _banner.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // Crit flash
        _flash = new ColorRect { Color = new Color(1, 1, 1, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(_flash);
        _flash.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // Battle result overlay
        _result = new ColorRect { Color = new Color(0, 0, 0, 0.6f), Visible = false };
        root.AddChild(_result);
        _result.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var resultBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _result.AddChild(resultBox);
        resultBox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _resultLabel = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _resultLabel.AddThemeFontSizeOverride("font_size", 64);
        resultBox.AddChild(_resultLabel);
        var restart = new Button { Text = "Play Again", SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(200, 44) };
        restart.Pressed += () => RestartRequested?.Invoke();
        resultBox.AddChild(restart);
    }

    private static PanelContainer Panel(Control parent, Control.LayoutPreset preset, Vector2 offset)
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = PanelBg,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8,
        });
        parent.AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(preset);
        // Grow away from the anchored edge so content never spills off-screen.
        panel.GrowHorizontal = preset switch
        {
            Control.LayoutPreset.TopRight or Control.LayoutPreset.BottomRight or Control.LayoutPreset.CenterRight => Control.GrowDirection.Begin,
            Control.LayoutPreset.CenterTop or Control.LayoutPreset.CenterBottom or Control.LayoutPreset.Center => Control.GrowDirection.Both,
            _ => Control.GrowDirection.End,
        };
        panel.GrowVertical = preset switch
        {
            Control.LayoutPreset.BottomLeft or Control.LayoutPreset.BottomRight or Control.LayoutPreset.CenterBottom => Control.GrowDirection.Begin,
            Control.LayoutPreset.CenterLeft or Control.LayoutPreset.CenterRight or Control.LayoutPreset.Center => Control.GrowDirection.Both,
            _ => Control.GrowDirection.End,
        };
        panel.Position += offset;
        return panel;
    }

    // ------------------------------------------------------------------ objective

    public void UpdateObjective(BattleState s)
    {
        int enemies = s.LivingOf(Faction.Enemy).Count();
        _objective.Text = $"{s.Map.Name}\nTurn {s.Turn} · {s.Phase} Phase\nGoal: Rout the enemy ({enemies} left)";
    }

    // ------------------------------------------------------------------ unit / terrain info

    public void ShowUnit(BattleState s, BattleUnit? unit)
    {
        if (unit is null || unit.IsDefeated)
        {
            _unitPanel.Visible = false;
            return;
        }
        var db = s.Db;
        var stats = s.Stats(unit);
        var cls = db.Class(unit.State.ClassId);
        _unitPanel.Visible = true;
        _unitTitle.Text = $"{unit.Name}{(unit.IsBoss ? " (Boss)" : "")} — {cls.Name} Lv {unit.State.Level}";
        _unitTitle.Modulate = UnitView.ColorFor(unit.Faction).Lightened(0.35f);
        _unitHp.MaxValue = stats.HP;
        _unitHp.Value = unit.CurrentHp;
        _unitHpText.Text = $"  HP {unit.CurrentHp}/{stats.HP}";
        _unitStats.Text =
            $"Str {stats.Str,2}  Mag {stats.Mag,2}  Dex {stats.Dex,2}  Spd {stats.Spd,2}\n" +
            $"Lck {stats.Lck,2}  Def {stats.Def,2}  Res {stats.Res,2}  Mov {stats.Mov,2}";
        var items = unit.State.Inventory.Select((id, i) =>
        {
            var w = db.TryWeapon(id);
            string name = w?.Name ?? id;
            string marker = i == unit.State.EquippedIndex ? "▶ " : "   ";
            return w is null ? marker + name : $"{marker}{name}  Mt {w.Might} Hit {w.Hit} Rng {RangeText(w)}";
        });
        var worn = new[] { db.TryArmor(unit.State.ArmorId), db.TryArmor(unit.State.AccessoryId) }.OfType<ArmorDef>()
            .Select(a => $"   {a.Name}  {BonusText(a.Bonuses)}");
        var lines = items.Concat(worn).ToList();
        _unitItems.Text = lines.Count == 0 ? "(no items)" : string.Join("\n", lines);
    }

    public void ShowTerrain(BattleState s, GridPos p)
    {
        if (!s.Grid.InBounds(p))
            return;
        var cell = s.Grid[p];
        var t = cell.Terrain;
        _terrainText.Text = $"{t.Name}  (height {cell.Height})\nAvo +{t.Avoid}  Def +{t.Def}" + (t.HealPercent > 0 ? $"  Heal {t.HealPercent}%" : "");
    }

    private static string BonusText(StatBlock bonuses) =>
        string.Join(" ", StatBlock.All.Where(s => bonuses[s] != 0).Select(s => $"{s} {bonuses[s]:+0;-0}"));

    private static string RangeText(WeaponDef w) => w.MinRange == w.MaxRange ? $"{w.MinRange}" : $"{w.MinRange}-{w.MaxRange}";

    // ------------------------------------------------------------------ action menu

    public void ShowMenu(IReadOnlyList<string> options, int selected)
    {
        foreach (var child in _menuList.GetChildren())
            child.QueueFree();
        _menuButtons.Clear();
        _menuOptions.Clear();
        _menuOptions.AddRange(options);
        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            var button = new Button
            {
                Text = options[i],
                FocusMode = Control.FocusModeEnum.None,
                CustomMinimumSize = new Vector2(140, 36),
                Alignment = HorizontalAlignment.Left,
            };
            button.Pressed += () => MenuClicked?.Invoke(index);
            _menuList.AddChild(button);
            _menuButtons.Add(button);
        }
        _menuPanel.MouseFilter = Control.MouseFilterEnum.Stop;
        _menuPanel.Visible = true;
        SetMenuSelection(selected);
    }

    public void SetMenuSelection(int selected)
    {
        for (int i = 0; i < _menuButtons.Count; i++)
        {
            _menuButtons[i].Modulate = i == selected ? Accent : Colors.White;
            _menuButtons[i].Text = (i == selected ? "▶ " : "   ") + _menuOptions[i];
        }
    }

    public void HideMenu() => _menuPanel.Visible = false;

    // ------------------------------------------------------------------ forecast

    public void ShowForecast(CombatForecast f, string hint)
    {
        foreach (var child in _forecastGrid.GetChildren())
            child.QueueFree();

        void Row(string left, string mid, string right, Color? color = null)
        {
            foreach (var (text, align) in new[] { (left, HorizontalAlignment.Right), (mid, HorizontalAlignment.Center), (right, HorizontalAlignment.Left) })
            {
                var label = new Label { Text = text, HorizontalAlignment = align, CustomMinimumSize = new Vector2(align == HorizontalAlignment.Center ? 50 : 150, 0) };
                if (color is { } c && align != HorizontalAlignment.Center)
                    label.Modulate = c;
                if (align == HorizontalAlignment.Center)
                    label.Modulate = new Color(1, 1, 1, 0.6f);
                _forecastGrid.AddChild(label);
            }
        }

        static string Arrow(int advantage) => advantage > 0 ? " ▲" : advantage < 0 ? " ▼" : "";
        static string Dmg(CombatantForecast c) => !c.CanStrike ? "--" : c.StrikeCount > 1 ? $"{c.Damage} ×{c.StrikeCount}" : $"{c.Damage}";
        static string Pct(CombatantForecast c, int v) => c.CanStrike ? $"{v}" : "--";

        var a = f.Attacker;
        var d = f.Defender;
        Row(a.Name, "", d.Name, Accent);
        Row((a.WeaponName ?? "-") + Arrow(a.TriangleAdvantage) + (a.Effective ? " !" : ""), "", (d.WeaponName ?? "-") + Arrow(d.TriangleAdvantage) + (d.Effective ? " !" : ""));
        Row($"{a.Hp}/{a.MaxHp}", "HP", $"{d.Hp}/{d.MaxHp}");
        Row(Dmg(a), "Dmg", Dmg(d));
        Row(Pct(a, a.Hit), "Hit", Pct(d, d.Hit));
        Row(Pct(a, a.Crit), "Crit", Pct(d, d.Crit));
        _forecastHint.Text = hint;
        _forecastHint.Visible = hint.Length > 0;
        _forecastPanel.Visible = true;
    }

    public void HideForecast() => _forecastPanel.Visible = false;

    public void SetBattleScenes(bool on) =>
        _ = ShowBanner(on ? "Battle scenes: ON" : "Battle scenes: OFF", Colors.White, 0.4f);

    // ------------------------------------------------------------------ banners and effects

    public async Task ShowBanner(string text, Color color, float hold = 0.7f)
    {
        _banner.Text = text;
        _banner.Modulate = color with { A = 0f };
        _banner.Visible = true;
        var tween = CreateTween();
        tween.TweenProperty(_banner, "modulate:a", 1f, 0.2f);
        tween.TweenInterval(hold);
        tween.TweenProperty(_banner, "modulate:a", 0f, 0.25f);
        await ToSignal(tween, Tween.SignalName.Finished);
        _banner.Visible = false;
    }

    public void FlashScreen()
    {
        _flash.Color = new Color(1, 1, 1, 0.7f);
        CreateTween().TweenProperty(_flash, "color:a", 0f, 0.25f);
    }

    public void ShowResult(BattleOutcome outcome)
    {
        _resultLabel.Text = outcome == BattleOutcome.Victory ? "Victory!" : "Defeat";
        _resultLabel.Modulate = outcome == BattleOutcome.Victory ? Accent : new Color(1f, 0.4f, 0.35f);
        _result.Visible = true;
    }
}
