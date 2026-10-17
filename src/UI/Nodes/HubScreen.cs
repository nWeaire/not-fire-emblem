using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NotFireEmblem.Audio;
using NotFireEmblem.Campaign;
using NotFireEmblem.Combat;
using NotFireEmblem.Core;
using NotFireEmblem.Units;

namespace NotFireEmblem.UI;

/// <summary>
/// The Brookhollow hub (STORY §3 Osmund, IMPLEMENTATION §5.7 hub screens): Party (stats, inventory, convoy and
/// equipment), Shop, Smithy, Promotion, Supports, Save, and March On. Every change goes through HubService.
/// </summary>
public partial class HubScreen : Control
{
    private enum Tab { Party, Shop, Smithy, Promotion, Supports, Save, Settings }

    private ContentDatabase Db => GameSessionNode.Instance.Content;
    private GameSession S => GameSessionNode.Instance.Session!;

    private static readonly Color GoldColor = new(0.95f, 0.79f, 0.3f);

    private Tab _tab = Tab.Party;
    private string? _unitId;
    private VBoxContainer _content = null!;
    private Label _status = null!;
    private Label _osmund = null!;
    private Label _gold = null!;
    private Label _chapter = null!;
    private Label _convoy = null!;
    private HBoxContainer _crestRow = null!;
    private VBoxContainer _tabs = null!;
    private VBoxContainer _right = null!;

    /// <summary>
    /// The layout (session 7, "option A"): a top bar with the gold, chapter, convoy and crest beside Title and March On;
    /// the places down the left with Osmund beneath; the chosen place's screen on the right, wrapping and scrolling so
    /// nothing runs off the window at any size.
    /// </summary>
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AudioDirector.PlayMusic("hub");
        AddChild(UiKit.Backdrop(new Color(0.18f, 0.14f, 0.1f), new Color(0.07f, 0.06f, 0.05f)));
        if (GameSessionNode.Instance.Session is null)
        {
            CampaignFlow.Advance();
            return;
        }

        var margin = new MarginContainer();
        AddChild(UiKit.Fill(margin));
        foreach (var side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", 16);
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        margin.AddChild(root);

        // Top bar: what you have and where you're going.
        var bar = UiKit.Panel();
        root.AddChild(bar);
        var barRow = new HBoxContainer();
        barRow.AddThemeConstantOverride("separation", 18);
        bar.AddChild(barRow);
        _gold = UiKit.Label("", 24, GoldColor);
        _gold.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        barRow.AddChild(_gold);
        _chapter = UiKit.Label("", 16, UiKit.Muted);
        _chapter.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _chapter.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _chapter.ClipText = true;
        barRow.AddChild(_chapter);
        _convoy = UiKit.Label("", 16, UiKit.Muted);
        _convoy.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        barRow.AddChild(_convoy);
        _crestRow = new HBoxContainer();
        _crestRow.AddThemeConstantOverride("separation", 8);
        barRow.AddChild(_crestRow);
        barRow.AddChild(UiKit.Button("Title", () =>
        {
            GameSessionNode.Instance.EndSession();
            GameSessionNode.Instance.GoTo(GameSessionNode.TitleScene);
        }, 100));
        barRow.AddChild(UiKit.Button("March On ▶", CampaignFlow.LeaveHub, 170));

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 14);
        root.AddChild(body);

        // Left: the places, then Osmund.
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(250, 0) };
        left.AddThemeConstantOverride("separation", 8);
        body.AddChild(left);
        _tabs = new VBoxContainer();
        _tabs.AddThemeConstantOverride("separation", 6);
        left.AddChild(_tabs);
        left.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
        var osmundPanel = UiKit.Panel();
        left.AddChild(osmundPanel);
        var osmundBox = new HBoxContainer();
        osmundBox.AddThemeConstantOverride("separation", 8);
        osmundPanel.AddChild(osmundBox);
        osmundBox.AddChild(new TextureRect
        {
            Texture = DialogueBox.Portrait("osmund", "neutral"),
            CustomMinimumSize = new Vector2(64, 64),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        });
        _osmund = UiKit.Label("", 14);
        _osmund.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _osmund.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _osmund.CustomMinimumSize = new Vector2(140, 0);
        osmundBox.AddChild(_osmund);

        // Right: the chosen place.
        var right = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        right.AddThemeConstantOverride("separation", 6);
        body.AddChild(right);
        _right = right;
        var panel = UiKit.Panel();
        panel.SizeFlagsVertical = SizeFlags.ExpandFill;
        right.AddChild(panel);
        // FollowFocus: keyboard and pad selection scroll the list to whatever is selected.
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        panel.AddChild(scroll);
        _content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _content.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_content);
        _status = UiKit.Label("", 15, UiKit.Accent);
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        right.AddChild(_status);

        _unitId = S.ActiveRoster.FirstOrDefault()?.Def.Id;
        BuildTour();
        Refresh();
        if (DevTools.AutoAdvance)
            GetTree().CreateTimer(0.5).Timeout += CampaignFlow.LeaveHub;
    }

    private void Refresh()
    {
        UiKit.ClearChildren(_crestRow);
        if (S.CrestTier > 0)
            _crestRow.AddChild(new CrestEmblem("rowan", S.CrestTier, radiant: true, size: 40));
        var crestLabel = UiKit.Label(CrestName(S.CrestTier), 16, S.CrestTier > 0 ? UiKit.Text : UiKit.Muted);
        crestLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _crestRow.AddChild(crestLabel);
        _gold.Text = $"{S.Gold} G";
        _chapter.Text = $"{S.ChapterTitle}  ·  {S.Mode}, {S.Difficulty}";
        _convoy.Text = $"Convoy {S.Convoy.Count}/{GameSession.ConvoyCapacity}";
        _osmund.Text = OsmundLine();
        _tourTargets.Clear();
        // Rebuilding frees the focused button: remember where the keyboard was and put it back after.
        int tabFocus = UiKit.FocusIndex(_tabs), contentFocus = UiKit.FocusIndex(_content);
        UiKit.ClearChildren(_tabs);
        foreach (var tab in Enum.GetValues<Tab>())
        {
            bool open = tab switch
            {
                Tab.Shop => S.Unlocks.Contains("shop"),
                Tab.Smithy => S.Unlocks.Contains("smithy"),
                Tab.Promotion => S.Unlocks.Contains("promotion"),
                Tab.Supports => S.Unlocks.Contains("supports"),
                _ => true,
            };
            int supports = tab == Tab.Supports ? S.AvailableSupports(Db).Count() : 0;
            var button = UiKit.Button(tab + (supports > 0 ? $" ({supports})" : "") + (open ? "" : " 🔒"), () =>
            {
                _tab = tab;
                _status.Text = "";
                Refresh();
            }, 0, open);
            button.Alignment = HorizontalAlignment.Left;
            if (tab == _tab)
                button.Modulate = UiKit.Accent;
            _tabs.AddChild(button);
            _tourTargets[$"tab:{tab}"] = button;
        }
        UiKit.ClearChildren(_content);
        switch (_tab)
        {
            case Tab.Party: BuildParty(); break;
            case Tab.Shop: BuildShop(); break;
            case Tab.Smithy: BuildSmithy(); break;
            case Tab.Promotion: BuildPromotion(); break;
            case Tab.Supports: BuildSupports(); break;
            case Tab.Save: BuildSave(); break;
            case Tab.Settings: _content.AddChild(new SettingsPanel()); break;
        }
        if (contentFocus >= 0)
            UiKit.RestoreFocus(_content, contentFocus);
        else
            UiKit.RestoreFocus(_tabs, Math.Max(0, tabFocus)); // on opening the hub, the first tab
        UpdateTour();
    }

    private void Do(Func<string?> action, string success)
    {
        string? error = action();
        _status.Text = error ?? success;
        _status.Modulate = error is null ? UiKit.Good : UiKit.Bad;
        Refresh();
    }

    private string OsmundLine() => _tab switch
    {
        Tab.Shop => "The trader's prices are robbery, my lord, but polite robbery.",
        Tab.Smithy => "Master Holt assures me the forge is 'happy'. I have chosen not to ask.",
        Tab.Promotion => "The crest decides what you are, my lord. The armourer decides whether you survive being it.",
        Tab.Supports => "Your companions have been talking. Occasionally to each other.",
        Tab.Save => "I keep a ledger of everything. You may as well, too.",
        _ => S.AvailableSupports(Db).Any()
            ? "Some of your companions wish a word with each other, my lord."
            : "What shall we attend to, my lord?",
    };

    private static string CrestName(int tier) => tier switch
    {
        0 => "Unmarked",
        1 => "Kindled Crest",
        2 => "Knight's Crest",
        3 => "Lord's Crest",
        _ => "Sovereign's Crest",
    };

    private HBoxContainer Row(string text, int size = 15, Color? color = null)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        var label = UiKit.Label(text, size, color);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        // Long lines wrap inside the panel instead of pushing it past the window edge.
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(80, 0);
        row.AddChild(label);
        _content.AddChild(row);
        return row;
    }

    private static Button Small(string text, Action onPressed, bool enabled = true) => UiKit.Button(text, onPressed, 90, enabled);

    // ------------------------------------------------------------------ party, inventory and equipment

    private void BuildParty()
    {
        var picker = new HFlowContainer();
        _content.AddChild(picker);
        foreach (var unit in S.ActiveRoster)
        {
            string id = unit.Def.Id;
            var button = UiKit.Button($"{unit.Def.Name} Lv{unit.Level}", () => { _unitId = id; Refresh(); }, 130);
            if (id == _unitId)
                button.Modulate = UiKit.Accent;
            picker.AddChild(button);
        }
        if (_unitId is null || S.Unit(_unitId) is not { } u)
            return;

        var stats = StatCalculator.Effective(Db, u);
        var cls = Db.Class(u.ClassId);
        Row($"{u.Def.Name} — {cls.Name} (tier {cls.Tier})   Lv {u.Level}   EXP {u.Exp}/100", 20, UiKit.Accent);
        Row($"HP {stats.HP}   Str {stats.Str}   Mag {stats.Mag}   Dex {stats.Dex}   Spd {stats.Spd}   Lck {stats.Lck}   Def {stats.Def}   Res {stats.Res}   Mov {stats.Mov}");
        var arts = Abilities.Arts(Db, u).Select(a => a.Name).Concat(Abilities.Spells(Db, u).Select(sp => sp.Name)).ToList();
        if (arts.Count > 0)
            Row("Arts & spells: " + string.Join(", ", arts), 14, UiKit.Muted);
        Row("Weapons: " + string.Join(", ", cls.Usable) + $"   ·   Armour up to {cls.MaxArmor}", 14, UiKit.Muted);

        Row("Inventory", 17, UiKit.Accent);
        for (int i = 0; i < u.Inventory.Count; i++)
        {
            int slot = i;
            string itemId = u.Inventory[i];
            var w = Db.TryWeapon(itemId);
            string text = (i == u.EquippedIndex ? "▶ " : "   ") + Db.ItemName(itemId)
                          + (w is null ? "" : $"   Mt {w.Might}  Hit {w.Hit}  Crt {w.Crit}  Wt {w.Weight}  Rng {w.MinRange}-{w.MaxRange}");
            var row = Row(text);
            if (w is not null && CombatCalculator.CanWield(Db, u, w) && i != u.EquippedIndex)
            {
                var equip = Small("Equip", () => Do(() => { u.EquippedIndex = slot; return null; }, $"{u.Def.Name} equips {w.Name}."));
                row.AddChild(equip);
                _tourTargets.TryAdd($"equip:{u.Def.Id}:{itemId}", equip);
            }
            row.AddChild(Small("To convoy", () => Do(() => HubService.TakeFromUnit(Db, S, u, slot), "Sent to the convoy.")));
        }
        if (u.Inventory.Count == 0)
            Row("   (empty)", 14, UiKit.Muted);

        Row("Worn", 17, UiKit.Accent);
        foreach (var (slot, worn) in new[] { (ArmorSlot.Body, u.ArmorId), (ArmorSlot.Accessory, u.AccessoryId) })
        {
            var armor = Db.TryArmor(worn);
            var row = Row($"   {slot}: {(armor is null ? "—" : $"{armor.Name}  {BonusText(armor.Bonuses)}  Wt {armor.Weight}")}");
            if (armor is not null)
                row.AddChild(Small("Take off", () => Do(() => HubService.TakeOff(S, u, slot), "Taken off.")));
        }

        Row($"Convoy ({S.Convoy.Count})", 17, UiKit.Accent);
        foreach (var itemId in S.Convoy.Distinct().OrderBy(Db.ItemName))
        {
            int count = S.Convoy.Count(x => x == itemId);
            var row = Row($"   {Db.ItemName(itemId)}{(count > 1 ? $" ×{count}" : "")}");
            if (Db.IsInventoryItem(itemId))
                row.AddChild(Small("Give", () => Do(() => HubService.GiveToUnit(Db, S, u, itemId), $"Given to {u.Def.Name}."),
                    u.Inventory.Count < UnitState.MaxInventory));
            if (Db.TryArmor(itemId) is { } armor)
                row.AddChild(Small("Wear", () => Do(() => HubService.WearFromConvoy(Db, S, u, itemId), $"{u.Def.Name} wears {armor.Name}."),
                    EquipmentService.CanEquipArmor(Db, u, armor)));
            if (Db.TryItem(itemId) is { Kind: ItemKind.StatBoost })
                row.AddChild(Small("Use", () => Do(() => HubService.UseBooster(Db, S, u, itemId), "Used.")));
        }
    }

    private static string BonusText(StatBlock bonuses) =>
        string.Join(" ", StatBlock.All.Where(s => bonuses[s] != 0).Select(s => $"{s} {bonuses[s]:+0;-0}"));

    // ------------------------------------------------------------------ shop

    private void BuildShop()
    {
        var shops = EquipmentService.OpenShops(Db, S.Chapter).Select(sh => sh.Name).ToList();
        Row("Open shops: " + (shops.Count == 0 ? "none yet" : string.Join(", ", shops)), 14, UiKit.Muted);
        // Who the purchase is for: weapons and items go straight into their pack (the convoy when it's full).
        Row("Buy for", 17, UiKit.Accent);
        var picker = new HFlowContainer();
        _content.AddChild(picker);
        foreach (var member in S.ActiveRoster)
        {
            string id = member.Def.Id;
            var button = UiKit.Button($"{member.Def.Name} ({member.Inventory.Count}/{UnitState.MaxInventory})", () => { _unitId = id; Refresh(); }, 130);
            if (id == _unitId)
                button.Modulate = UiKit.Accent;
            picker.AddChild(button);
        }
        var buyer = _unitId is null ? null : S.Unit(_unitId);
        Row("Buy", 17, UiKit.Accent);
        foreach (var itemId in HubService.ShopStock(Db, S).OrderBy(Db.ItemPrice))
        {
            int price = Db.ItemPrice(itemId);
            var row = Row($"   {Db.ItemName(itemId)}   {Describe(itemId)}");
            row.AddChild(UiKit.Label($"{price} G", 15, GoldColor));
            var buy = Small("Buy", () =>
            {
                int before = buyer?.Inventory.Count ?? 0;
                string? error = buyer is null ? HubService.Buy(Db, S, itemId) : HubService.BuyFor(Db, S, buyer, itemId);
                bool toPack = buyer is not null && buyer.Inventory.Count > before;
                Do(() => error, toPack ? $"Bought {Db.ItemName(itemId)} for {buyer!.Def.Name}." : $"Bought {Db.ItemName(itemId)} (sent to the convoy).");
            }, S.Gold >= price);
            row.AddChild(buy);
            _tourTargets.TryAdd($"buy:{itemId}", buy);
        }
        Row("Sell (from the convoy)", 17, UiKit.Accent);
        foreach (var itemId in S.Convoy.Distinct().OrderBy(Db.ItemName))
        {
            int price = HubService.SellPrice(Db, itemId);
            var row = Row($"   {Db.ItemName(itemId)}");
            row.AddChild(UiKit.Label($"{price}g", 15));
            row.AddChild(Small("Sell", () => Do(() => HubService.Sell(Db, S, itemId), $"Sold {Db.ItemName(itemId)}."), price > 0));
        }
    }

    private string Describe(string itemId)
    {
        if (Db.TryWeapon(itemId) is { } w)
            return $"{w.Type}  Mt {w.Might} Hit {w.Hit} Rng {w.MinRange}-{w.MaxRange}";
        if (Db.TryArmor(itemId) is { } a)
            return $"{a.Slot} {(a.Slot == ArmorSlot.Body ? a.Class.ToString() : "")}  {BonusText(a.Bonuses)}";
        if (Db.TryItem(itemId) is { } i)
            return i.Kind switch
            {
                ItemKind.Heal => i.Amount > 0 ? $"Heals {i.Amount} HP" : "Heals fully",
                _ => $"{i.Stat} +{i.Amount}",
            };
        return "";
    }

    // ------------------------------------------------------------------ smithy

    private void BuildSmithy()
    {
        Row("Brann reforges a weapon into the next tier for the difference in price.", 14, UiKit.Muted);
        foreach (var unit in S.ActiveRoster)
        {
            foreach (var option in HubService.ForgeOptions(Db, S, unit.Inventory))
            {
                var row = Row($"   {unit.Def.Name}: {Db.ItemName(option.FromId)} → {Db.ItemName(option.ToId)}");
                row.AddChild(UiKit.Label($"{option.Cost}g", 15, UiKit.Accent));
                row.AddChild(Small("Forge", () => Do(() => HubService.Forge(Db, S, unit, option), $"Brann forged a {Db.ItemName(option.ToId)}."), S.Gold >= option.Cost));
            }
        }
        foreach (var option in HubService.ForgeOptions(Db, S, S.Convoy))
        {
            var row = Row($"   Convoy: {Db.ItemName(option.FromId)} → {Db.ItemName(option.ToId)}");
            row.AddChild(UiKit.Label($"{option.Cost}g", 15, UiKit.Accent));
            row.AddChild(Small("Forge", () => Do(() => HubService.Forge(Db, S, null, option), $"Brann forged a {Db.ItemName(option.ToId)}."), S.Gold >= option.Cost));
        }
        if (_content.GetChildCount() <= 1)
            Row("   Nothing Brann can improve right now.", 14, UiKit.Muted);
    }

    // ------------------------------------------------------------------ promotion

    private void BuildPromotion()
    {
        Row($"Promotion needs level {PromotionService.MinLevel} and a crest tier at least the new class's tier. Crest: tier {S.CrestTier}.", 14, UiKit.Muted);
        foreach (var unit in S.ActiveRoster)
        {
            var cls = Db.Class(unit.ClassId);
            if (cls.PromotesTo.Count == 0)
            {
                Row($"   {unit.Def.Name} ({cls.Name}): no further class yet", 15, UiKit.Muted);
                continue;
            }
            foreach (var target in cls.PromotesTo)
            {
                string? why = HubService.CannotPromote(Db, S, unit, target);
                var row = Row($"   {unit.Def.Name} ({cls.Name} Lv {unit.Level}) → {Db.Class(target).Name}" + (why is null ? "" : $"   — {why}"));
                row.AddChild(Small("Promote", () => Do(() => HubService.Promote(Db, S, unit, target), $"{unit.Def.Name} is now a {Db.Class(target).Name}!"), why is null));
            }
        }
    }

    // ------------------------------------------------------------------ supports

    private void BuildSupports()
    {
        var available = S.AvailableSupports(Db).ToList();
        Row("Ready to watch", 17, UiKit.Accent);
        foreach (var (a, b, rank, sceneId) in available)
        {
            var row = Row($"   {Db.Unit(a).Name} & {Db.Unit(b).Name} — rank {Supports.RankNames[rank]}");
            row.AddChild(Small("Watch", () =>
            {
                GameSessionNode.Instance.PendingSupport = (a, b, rank);
                CampaignFlow.PlayScene(sceneId, StoryReturn.Hub);
            }));
        }
        if (available.Count == 0)
            Row("   Nothing new. Fight side by side to grow closer.", 14, UiKit.Muted);
        Row("Bonds", 17, UiKit.Accent);
        var roster = S.ActiveRoster.Select(u => u.Def.Id).ToList();
        for (int i = 0; i < roster.Count; i++)
        {
            for (int j = i + 1; j < roster.Count; j++)
            {
                if (Supports.Find(Db, roster[i], roster[j]) is not { } def)
                    continue;
                int rank = S.SupportRank(roster[i], roster[j]);
                int points = S.SupportPointsOf(roster[i], roster[j]);
                string next = rank >= 3 ? "max" : $"{points}/{def.Threshold(rank + 1)} to {Supports.RankNames[rank + 1]}";
                Row($"   {Db.Unit(roster[i]).Name} & {Db.Unit(roster[j]).Name}: {Supports.RankNames[rank]}   ({next})", 15);
            }
        }
    }

    // ------------------------------------------------------------------ Osmund's tour (first visit)

    private const string TourFlag = "tour_hub";
    private const string Lord = "rowan";

    /// <summary>One step: Osmund's line, what ends it (null: the Next button) and what to point at (first that exists).</summary>
    private sealed record TourStep(string Text, Func<bool>? Done, string[] Targets, bool LordSelected = false);

    private TourStep[] _steps = Array.Empty<TourStep>();
    private int _step = -1;
    private readonly Dictionary<string, Control> _tourTargets = new();
    private Control? _tourPanel;
    private Label _tourText = null!;
    private Button _tourNext = null!;

    private UnitState? LordState => S.Unit(Lord);

    private bool LordHas(string itemId) => LordState is { } u && (u.Inventory.Contains(itemId) || S.Convoy.Contains(itemId));

    /// <summary>
    /// The first visit: Osmund shows the places, then has Rowan buy and equip an Iron Sword in place of the old farm
    /// sword he started with. Steps wait for Next or for the player to do the thing; the target pulses.
    /// </summary>
    private void BuildTour()
    {
        if (S.Flags.Contains(TourFlag) || DevTools.AutoAdvance || LordState is null)
            return;
        _steps = new[]
        {
            new TourStep("Welcome to the hall, my lord. Between battles, all you need is here: the places down the left, " +
                         "your gold and the road onward along the top.", null, Array.Empty<string>()),
            new TourStep("That blade, however. A lord who carries a rusted farm sword will be treated as one. The trader " +
                         "has proper iron: the Shop, if you please.", () => _tab == Tab.Shop, new[] { "tab:Shop" }),
            new TourStep("The Iron Sword, for yourself. Bought for you, it goes straight into your pack.",
                         () => LordHas("iron_sword"), new[] { "buy:iron_sword", "tab:Shop" }, LordSelected: true),
            new TourStep("Now the Party screen, to take it in hand.", () => _tab == Tab.Party, new[] { "tab:Party" }),
            new TourStep("Equip the Iron Sword. The old one may stay in your pack, or go to the convoy. Sentiment weighs " +
                         "nothing there.", () => LordState?.EquippedItemId == "iron_sword",
                         new[] { $"equip:{Lord}:iron_sword", "tab:Party" }, LordSelected: true),
            new TourStep("Splendid. Promotion and Supports will matter in time, Save keeps a record, and Settings is there " +
                         "should the music offend. When you are ready, March On.", null, Array.Empty<string>()),
        };
        // An older save where Rowan already carries iron: just the summary.
        _step = LordState!.EquippedItemId == "iron_sword" ? _steps.Length - 1 : 0;

        // Above the chosen place's screen, so it never covers what Osmund asks you to press.
        var panel = UiKit.Panel();
        _right.AddChild(panel);
        _right.MoveChild(panel, 0);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        panel.AddChild(row);
        row.AddChild(new TextureRect
        {
            Texture = DialogueBox.Portrait("osmund", "neutral"),
            CustomMinimumSize = new Vector2(80, 80),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
        });
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", 6);
        row.AddChild(column);
        column.AddChild(UiKit.Label("Osmund", 16, UiKit.Accent));
        _tourText = UiKit.Label("", 16);
        _tourText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _tourText.CustomMinimumSize = new Vector2(200, 0);
        column.AddChild(_tourText);
        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 10);
        column.AddChild(buttons);
        _tourNext = UiKit.Button("Next ▶", NextTourStep, 140);
        buttons.AddChild(_tourNext);
        buttons.AddChild(UiKit.Button("Skip tour", EndTour, 140));
        _tourPanel = panel;
    }

    private void NextTourStep()
    {
        _step++;
        if (_step >= _steps.Length)
        {
            EndTour();
            return;
        }
        if (_steps[_step].LordSelected)
            _unitId = Lord;
        Refresh();
    }

    private void EndTour()
    {
        S.Flags.Add(TourFlag);
        _step = -1;
        _tourPanel?.QueueFree();
        _tourPanel = null;
        Refresh();
    }

    /// <summary>After every rebuild: move past finished steps, show the line, point at (and focus) what to press.</summary>
    private void UpdateTour()
    {
        if (_tourPanel is null || _step < 0)
            return;
        if (_steps[_step].Done is { } done && done())
        {
            Callable.From(NextTourStep).CallDeferred();
            return;
        }
        var step = _steps[_step];
        _tourText.Text = step.Text;
        _tourNext.Visible = step.Done is null;
        var target = step.Targets.Select(k => _tourTargets.GetValueOrDefault(k)).FirstOrDefault(c => c is not null);
        if (target is not null)
        {
            var pulse = target.CreateTween().SetLoops();
            pulse.TweenProperty(target, "modulate", new Color(1.6f, 1.35f, 0.6f), 0.45f);
            pulse.TweenProperty(target, "modulate", Colors.White, 0.45f);
            target.CallDeferred(Control.MethodName.GrabFocus);
        }
        else if (step.Done is null)
            _tourNext.CallDeferred(Control.MethodName.GrabFocus);
    }

    // ------------------------------------------------------------------ save

    private void BuildSave()
    {
        foreach (var (slot, label) in SaveService.List().Where(s => s.Slot > 0))
        {
            var row = Row($"   {SaveService.SlotName(slot)}: {label ?? "(empty)"}");
            row.AddChild(Small("Save", () => Do(() => SaveService.Save(slot, S), $"Saved to {SaveService.SlotName(slot)}.")));
        }
        Row("The game also autosaves whenever you arrive at the hub.", 14, UiKit.Muted);
    }
}
