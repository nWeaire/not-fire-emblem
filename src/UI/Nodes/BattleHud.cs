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
    private static readonly Color Accent = UiKit.Accent;

    private Label _objective = null!;
    private PanelContainer _unitPanel = null!;
    private Control _unitPortrait = null!;
    private AnimatedSprite2D _unitPortraitSprite = null!;
    private Vector2 _portraitOffset;
    private Label _unitTitle = null!;
    private ProgressBar _unitHp = null!;
    private Label _unitHpText = null!;
    private Label _unitClass = null!;
    private VBoxContainer _unitSections = null!;
    private PanelContainer _terrainPanel = null!;
    private Label _terrainText = null!;
    private PanelContainer _menuPanel = null!;
    private VBoxContainer _menuList = null!;
    private readonly List<Button> _menuButtons = new();
    private readonly List<string> _menuOptions = new();
    private readonly List<AbilityInfo?> _menuAbilities = new();
    private PanelContainer _abilityPanel = null!;
    private Label _abilityTitle = null!;
    private Label _abilityBody = null!;
    private PanelContainer _forecastPanel = null!;
    private GridContainer _forecastGrid = null!;
    private Label _forecastHint = null!;
    private Label _banner = null!;
    private ColorRect _flash = null!;
    private Control _result = null!;
    private Label _resultLabel = null!;
    private VBoxContainer _resultButtons = null!;
    private Label _unitStatus = null!;
    private HBoxContainer _unitCrest = null!;
    private PanelContainer _levelPanel = null!;
    private Label _levelText = null!;
    private VBoxContainer _levelStats = null!;
    private VBoxContainer _toasts = null!;
    private PanelContainer _captionPanel = null!;
    private Label _caption = null!;

    /// <summary>Raised when a menu entry is clicked with the mouse.</summary>
    public event Action<int>? MenuClicked;

    /// <summary>Plays story scenes requested by map triggers.</summary>
    public DialogueBox Dialogue { get; private set; } = null!;

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
        var unitRow = new HBoxContainer();
        unitRow.AddThemeConstantOverride("separation", 10);
        _unitPanel.AddChild(unitRow);
        // The unit's battle-screen sprite, so it's clear who's under the cursor.
        _unitPortrait = new Control { CustomMinimumSize = new Vector2(112, 112), ClipContents = true };
        _unitPortraitSprite = new AnimatedSprite2D
        {
            Position = new Vector2(56, 56),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
        _unitPortrait.AddChild(_unitPortraitSprite);
        unitRow.AddChild(_unitPortrait);
        _unitCrest = new HBoxContainer();
        unitRow.AddChild(_unitCrest);
        var unitBox = new VBoxContainer();
        unitRow.AddChild(unitBox);
        _unitTitle = new Label();
        _unitTitle.AddThemeFontSizeOverride("font_size", UiKit.Px(22));
        unitBox.AddChild(_unitTitle);
        _unitClass = UiKit.Label("", 15, UiKit.Muted);
        unitBox.AddChild(_unitClass);
        var hpRow = new HBoxContainer();
        unitBox.AddChild(hpRow);
        _unitHp = new ProgressBar { CustomMinimumSize = new Vector2(180, 14), ShowPercentage = false, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        _unitHp.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color(0.3f, 0.85f, 0.4f) });
        hpRow.AddChild(_unitHp);
        _unitHpText = new Label();
        hpRow.AddChild(_unitHpText);
        // Weapons, items and spells: short sections, each only when the unit has something in it.
        _unitSections = new VBoxContainer();
        _unitSections.AddThemeConstantOverride("separation", 2);
        unitBox.AddChild(_unitSections);
        _unitStatus = new Label { Modulate = new Color(0.75f, 0.9f, 1f) };
        _unitStatus.AddThemeFontSizeOverride("font_size", UiKit.Px(13));
        unitBox.AddChild(_unitStatus);
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

        // Ability box (top-right): what the highlighted art, spell or crest skill does.
        _abilityPanel = Panel(root, Control.LayoutPreset.TopRight, new Vector2(-16, 16));
        var abilityBox = new VBoxContainer();
        abilityBox.AddThemeConstantOverride("separation", 4);
        _abilityPanel.AddChild(abilityBox);
        _abilityTitle = new Label { Modulate = Accent };
        abilityBox.AddChild(_abilityTitle);
        _abilityBody = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(320, 0) };
        _abilityBody.AddThemeFontSizeOverride("font_size", UiKit.Px(16));
        abilityBox.AddChild(_abilityBody);
        _abilityPanel.Visible = false;

        // Forecast (top-centre)
        _forecastPanel = Panel(root, Control.LayoutPreset.CenterTop, new Vector2(0, 16));
        var forecastBox = new VBoxContainer();
        _forecastPanel.AddChild(forecastBox);
        _forecastGrid = new GridContainer { Columns = 3 };
        _forecastGrid.AddThemeConstantOverride("h_separation", 24);
        forecastBox.AddChild(_forecastGrid);
        _forecastHint = new Label { HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1, 1, 1, 0.7f) };
        _forecastHint.AddThemeFontSizeOverride("font_size", UiKit.Px(13));
        forecastBox.AddChild(_forecastHint);
        _forecastPanel.Visible = false;

        // Objective tour caption (top-centre, narrow enough to clear the objective panel; long captions wrap)
        _captionPanel = Panel(root, Control.LayoutPreset.CenterTop, new Vector2(0, 16));
        var captionBox = new VBoxContainer();
        _captionPanel.AddChild(captionBox);
        _caption = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(UiKit.Px(320), 0),
        };
        _caption.AddThemeFontSizeOverride("font_size", UiKit.Px(20));
        captionBox.AddChild(_caption);
        var captionHint = new Label { Text = "Z: next", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1, 1, 1, 0.6f) };
        captionHint.AddThemeFontSizeOverride("font_size", UiKit.Px(13));
        captionBox.AddChild(captionHint);
        _captionPanel.Visible = false;

        // Controls hint (bottom-centre)
        var hint = new Label
        {
            Text = "Z: select  X: back  WASD: cursor  Q/E: rotate  +/-: zoom  Tab: next  G: danger  B: scenes  T: end turn  Hold Space: 4×",
            Modulate = new Color(1, 1, 1, 0.65f),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        hint.AddThemeFontSizeOverride("font_size", UiKit.Px(13));
        hint.AddThemeConstantOverride("outline_size", 4);
        hint.AddThemeColorOverride("font_outline_color", Colors.Black);
        root.AddChild(hint);
        hint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        hint.Position += new Vector2(0, -6);
        hint.GrowHorizontal = Control.GrowDirection.Both;

        // Phase banner (centre)
        _banner = new Label { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Visible = false };
        _banner.AddThemeFontSizeOverride("font_size", UiKit.Px(56));
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
        _resultLabel.AddThemeFontSizeOverride("font_size", UiKit.Px(64));
        resultBox.AddChild(_resultLabel);
        _resultButtons = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
        _resultButtons.AddThemeConstantOverride("separation", 8);
        resultBox.AddChild(_resultButtons);

        // Level-up panel (centre)
        _levelPanel = Panel(root, Control.LayoutPreset.Center, Vector2.Zero);
        var levelBox = new VBoxContainer();
        levelBox.AddThemeConstantOverride("separation", 2);
        _levelPanel.AddChild(levelBox);
        _levelText = new Label();
        _levelText.AddThemeFontSizeOverride("font_size", UiKit.Px(18));
        levelBox.AddChild(_levelText);
        _levelStats = new VBoxContainer();
        _levelStats.AddThemeConstantOverride("separation", 2);
        levelBox.AddChild(_levelStats);
        _levelPanel.Visible = false;

        // Toasts (top-right): items, gold, joins
        _toasts = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(_toasts);
        _toasts.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        _toasts.GrowHorizontal = Control.GrowDirection.Begin;
        _toasts.Position += new Vector2(-16, 16);

        Dialogue = new DialogueBox();
        root.AddChild(Dialogue);
    }

    private static PanelContainer Panel(Control parent, Control.LayoutPreset preset, Vector2 offset)
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", UiKit.PanelStyle(margin: 10));
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

    /// <summary>The objective tour's caption; null hides it.</summary>
    public void ShowCaption(string? text)
    {
        _captionPanel.Visible = text is not null;
        _caption.Text = text ?? "";
    }

    public void UpdateObjective(BattleState s)
    {
        _objective.Text = $"{s.Map.Name}\nTurn {s.Turn} · {s.Phase} Phase" + (s.Map.Fog ? " · Fog" : "") + $"\nGoal: {ObjectiveEvaluator.Describe(s)}";
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
        _unitTitle.Text = unit.Name + (unit.IsBoss ? "  (Boss)" : "");
        _unitTitle.Modulate = UnitView.ColorFor(unit.Faction).Lightened(0.35f);
        _unitClass.Text = $"Lv {unit.State.Level} · {cls.Name}";
        ShowPortrait(unit);
        _unitHp.MaxValue = stats.HP;
        _unitHp.Value = unit.CurrentHp;
        _unitHpText.Text = $"  {unit.CurrentHp}/{stats.HP}";

        foreach (var child in _unitSections.GetChildren())
            child.QueueFree();
        var weapons = new List<(string Id, string Text, bool Equipped)>();
        var items = new List<(string Id, string Text)>();
        for (int i = 0; i < unit.State.Inventory.Count; i++)
        {
            string id = unit.State.Inventory[i];
            if (db.TryWeapon(id) is { } w)
                weapons.Add((id, w.Name, i == unit.State.EquippedIndex));
            else
                items.Add((id, db.TryItem(id)?.Name ?? id));
        }
        var spells = Abilities.Spells(db, unit.State).Select(sp => $"{sp.Name} {unit.UsesLeft.GetValueOrDefault(sp.Id)}/{sp.Uses}")
            .Concat(Abilities.Arts(db, unit.State).Select(a => $"{a.Name} {unit.UsesLeft.GetValueOrDefault(a.Id)}/{a.Uses}"))
            .Concat(unit.UsesLeft.Where(kv => kv.Key.StartsWith("crest:")).Select(kv => $"{kv.Key[6..]} {kv.Value}"))
            .ToList();
        if (weapons.Count > 0)
        {
            _unitSections.AddChild(UiKit.Caps("Weapons", 12));
            foreach (var (id, text, equipped) in weapons)
                _unitSections.AddChild(IconLine(db, id, (equipped ? "▶ " : "   ") + text, equipped ? UiKit.Text : UiKit.Muted));
        }
        if (items.Count > 0)
        {
            _unitSections.AddChild(UiKit.Caps("Items", 12));
            foreach (var (id, text) in items)
                _unitSections.AddChild(IconLine(db, id, "   " + text, UiKit.Text));
        }
        if (spells.Count > 0)
        {
            _unitSections.AddChild(UiKit.Caps("Spells & arts", 12));
            _unitSections.AddChild(UiKit.Label("   " + string.Join(", ", spells), 15));
        }

        var status = new List<string>();
        if (s.IsDominated(unit))
            status.Add("Dominated");
        if (unit.CrestBearer)
            status.Add("Crest-bearer");
        if (unit.StunnedPhases > 0)
            status.Add("Stunned");
        if (unit.TempBonusSource is { } buff)
            status.Add(buff);
        _unitStatus.Text = string.Join(" · ", status);
        _unitStatus.Visible = status.Count > 0;

        foreach (var child in _unitCrest.GetChildren())
            child.QueueFree();
        if (unit.State.Def.Id == "rowan" && s.Options.CrestTier > 0)
            _unitCrest.AddChild(new CrestEmblem("rowan", s.Options.CrestTier, radiant: true, size: 64));
        else if (unit.CrestBearer && CrestEmblem.BearerCrest(unit.Name) is { } crest)
            _unitCrest.AddChild(new CrestEmblem(crest.Motif, crest.Tier, radiant: false, size: 64));
        _unitCrest.Visible = _unitCrest.GetChildCount() > 0;
    }

    /// <summary>Plays the unit's battle-screen idle in the unit panel (enemies face right, the player's side left).</summary>
    private void ShowPortrait(BattleUnit unit)
    {
        var frames = SpriteLibrary.Battle(unit);
        _unitPortrait.Visible = frames is not null;
        if (frames is null)
            return;
        if (_unitPortraitSprite.SpriteFrames != frames)
        {
            _unitPortraitSprite.SpriteFrames = frames;
            _unitPortraitSprite.Play("idle");
            // Fit the drawn pixels (not the padded frame) into the box, centred, in whole-pixel steps.
            var texture = frames.GetFrameTexture("idle", 0);
            var used = texture.GetImage().GetUsedRect();
            float scale = Mathf.Max(1f, Mathf.Floor(104f / Mathf.Max(used.Size.X, used.Size.Y) * 2f) / 2f);
            _unitPortraitSprite.Scale = new Vector2(scale, scale);
            _portraitOffset = texture.GetSize() / 2f - ((Vector2)used.Position + (Vector2)used.Size / 2f);
        }
        _unitPortraitSprite.FlipH = unit.Faction == Faction.Player;
        _unitPortraitSprite.Offset = _portraitOffset with { X = _unitPortraitSprite.FlipH ? -_portraitOffset.X : _portraitOffset.X };
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

    /// <summary>An inventory line in the unit panel: the item's icon (when there's art) and its name.</summary>
    private static Control IconLine(ContentDatabase db, string itemId, string text, Color color)
    {
        var label = UiKit.Label(text, 15, color);
        if (ItemIcons.Rect(db, itemId, 18) is not { } icon)
            return label;
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);
        row.AddChild(icon);
        row.AddChild(label);
        return row;
    }

    private static string RangeText(WeaponDef w) => w.MinRange == w.MaxRange ? $"{w.MinRange}" : $"{w.MinRange}-{w.MaxRange}";

    // ------------------------------------------------------------------ action menu

    /// <param name="abilities">Per option, the ability it uses (null for plain actions): explained in the ability box while selected.</param>
    public void ShowMenu(IReadOnlyList<string> options, int selected, IReadOnlyList<AbilityInfo?>? abilities = null)
    {
        foreach (var child in _menuList.GetChildren())
            child.QueueFree();
        _menuButtons.Clear();
        _menuOptions.Clear();
        _menuOptions.AddRange(options);
        _menuAbilities.Clear();
        _menuAbilities.AddRange(options.Select((_, i) => abilities is not null && i < abilities.Count ? abilities[i] : null));
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
        ShowAbility(selected >= 0 && selected < _menuAbilities.Count ? _menuAbilities[selected] : null);
    }

    public void HideMenu()
    {
        _menuPanel.Visible = false;
        ShowAbility(null);
    }

    /// <summary>The ability box (top-right); null hides it.</summary>
    public void ShowAbility(AbilityInfo? info)
    {
        _abilityPanel.Visible = info is not null;
        if (info is null)
            return;
        _abilityTitle.Text = info.Title;
        _abilityBody.Text = info.Body;
    }

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
        static string Dmg(CombatantForecast c) => !c.CanStrike ? "--" : c.Harmless ? "—" : c.StrikeCount > 1 ? $"{c.Damage} ×{c.StrikeCount}" : $"{c.Damage}";
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

    public void HideForecast()
    {
        _forecastPanel.Visible = false;
        ShowAbility(null);
    }

    /// <summary>A plain text preview in the forecast panel (heal amounts, talk and trade targets...).</summary>
    public void ShowPreview(string text)
    {
        foreach (var child in _forecastGrid.GetChildren())
            child.QueueFree();
        _forecastHint.Text = text;
        _forecastHint.Visible = true;
        _forecastPanel.Visible = true;
    }

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

    public void ShowResult(BattleOutcome outcome, IReadOnlyList<(string Label, Action OnPressed)> buttons)
    {
        _resultLabel.Text = outcome == BattleOutcome.Victory ? "Victory!" : "Defeat";
        _resultLabel.Modulate = outcome == BattleOutcome.Victory ? Accent : new Color(1f, 0.4f, 0.35f);
        foreach (var child in _resultButtons.GetChildren())
            child.QueueFree();
        foreach (var (label, onPressed) in buttons)
            _resultButtons.AddChild(UiKit.Button(label, onPressed, 220));
        _result.Visible = true;
        if (_resultButtons.GetChildCount() > 0)
            _resultButtons.GetChild<Button>(0).CallDeferred(Control.MethodName.GrabFocus);
    }

    /// <summary>A short message in the top-right corner that fades out.</summary>
    public void Toast(string text, Color? color = null)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Right, Modulate = color ?? Colors.White };
        label.AddThemeFontSizeOverride("font_size", UiKit.Px(18));
        label.AddThemeConstantOverride("outline_size", 6);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        _toasts.AddChild(label);
        var tween = label.CreateTween();
        tween.TweenInterval(2.2f);
        tween.TweenProperty(label, "modulate:a", 0f, 0.5f);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    /// <summary>Shows the stats gained on a level-up for a moment.</summary>
    public async Task ShowLevelUp(string name, string className, LevelUp level, StatBlock statsAfter)
    {
        _levelText.Text = $"LEVEL UP!   {name} — {className} Lv {level.Level}\n";
        LevelStats.Fill(_levelStats, statsAfter, level, 18);
        _levelPanel.Visible = true;
        _levelPanel.Modulate = Colors.White with { A = 0f };
        // A little pop as it appears (session 17 polish), with the gold sparkles on the map.
        _levelPanel.PivotOffset = _levelPanel.Size / 2f;
        _levelPanel.Scale = new Vector2(0.85f, 0.85f);
        var tween = CreateTween();
        tween.TweenProperty(_levelPanel, "modulate:a", 1f, 0.15f);
        tween.Parallel().TweenProperty(_levelPanel, "scale", Vector2.One, 0.25f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenInterval(1.6f);
        tween.TweenProperty(_levelPanel, "modulate:a", 0f, 0.25f);
        await ToSignal(tween, Tween.SignalName.Finished);
        _levelPanel.Visible = false;
    }
}
