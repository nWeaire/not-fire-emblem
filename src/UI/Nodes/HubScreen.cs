using System;
using System.Linq;
using Godot;
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
    private enum Tab { Party, Shop, Smithy, Promotion, Supports, Save }

    private ContentDatabase Db => GameSessionNode.Instance.Content;
    private GameSession S => GameSessionNode.Instance.Session!;

    private Tab _tab = Tab.Party;
    private string? _unitId;
    private VBoxContainer _content = null!;
    private Label _status = null!;
    private Label _osmund = null!;
    private Label _header = null!;
    private HBoxContainer _crestRow = null!;
    private HBoxContainer _tabs = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(UiKit.Backdrop(new Color(0.18f, 0.14f, 0.1f), new Color(0.07f, 0.06f, 0.05f)));
        if (GameSessionNode.Instance.Session is null)
        {
            CampaignFlow.Advance();
            return;
        }

        var margin = new MarginContainer();
        AddChild(UiKit.Fill(margin));
        foreach (var side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", 24);
        var root = new HBoxContainer();
        root.AddThemeConstantOverride("separation", 20);
        margin.AddChild(root);

        // Left: Osmund and the party's standing.
        var left = new VBoxContainer { CustomMinimumSize = new Vector2(260, 0) };
        left.AddThemeConstantOverride("separation", 10);
        root.AddChild(left);
        var portraitPanel = UiKit.Panel();
        left.AddChild(portraitPanel);
        var portrait = new TextureRect
        {
            Texture = DialogueBox.Portrait("osmund", "neutral"),
            CustomMinimumSize = new Vector2(220, 220),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
        };
        portraitPanel.AddChild(portrait);
        left.AddChild(UiKit.Label("Osmund Thorne, Steward", 16, UiKit.Accent));
        _osmund = UiKit.Label("", 15);
        _osmund.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _osmund.CustomMinimumSize = new Vector2(250, 0);
        left.AddChild(_osmund);
        _crestRow = new HBoxContainer();
        _crestRow.AddThemeConstantOverride("separation", 12);
        left.AddChild(_crestRow);
        _header = UiKit.Label("", 15, UiKit.Muted);
        _header.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _header.CustomMinimumSize = new Vector2(250, 0);
        left.AddChild(_header);
        left.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
        left.AddChild(UiKit.Button("March On ▶", CampaignFlow.LeaveHub, 250));
        left.AddChild(UiKit.Button("Quit to Title", () =>
        {
            GameSessionNode.Instance.EndSession();
            GameSessionNode.Instance.GoTo(GameSessionNode.TitleScene);
        }, 250));

        // Right: tabs and their content.
        var right = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        right.AddThemeConstantOverride("separation", 8);
        root.AddChild(right);
        _tabs = new HBoxContainer();
        right.AddChild(_tabs);
        var panel = UiKit.Panel();
        panel.SizeFlagsVertical = SizeFlags.ExpandFill;
        right.AddChild(panel);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        panel.AddChild(scroll);
        _content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _content.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_content);
        _status = UiKit.Label("", 15, UiKit.Accent);
        right.AddChild(_status);

        _unitId = S.ActiveRoster.FirstOrDefault()?.Def.Id;
        Refresh();
        if (DevTools.AutoAdvance)
            GetTree().CreateTimer(0.5).Timeout += CampaignFlow.LeaveHub;
    }

    private void Refresh()
    {
        foreach (var child in _crestRow.GetChildren())
            child.QueueFree();
        if (S.CrestTier > 0)
            _crestRow.AddChild(new CrestEmblem("rowan", S.CrestTier, radiant: true, size: 72));
        var crestLabel = UiKit.Label(CrestName(S.CrestTier), 18, S.CrestTier > 0 ? UiKit.Text : UiKit.Muted);
        crestLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _crestRow.AddChild(crestLabel);
        _header.Text = $"{S.ChapterTitle}\nGold: {S.Gold}\nConvoy: {S.Convoy.Count}/{GameSession.ConvoyCapacity}\n{S.Mode} · {S.Difficulty}";
        _osmund.Text = OsmundLine();
        foreach (var child in _tabs.GetChildren())
            child.QueueFree();
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
            }, 120, open);
            if (tab == _tab)
                button.Modulate = UiKit.Accent;
            _tabs.AddChild(button);
        }
        foreach (var child in _content.GetChildren())
            child.QueueFree();
        switch (_tab)
        {
            case Tab.Party: BuildParty(); break;
            case Tab.Shop: BuildShop(); break;
            case Tab.Smithy: BuildSmithy(); break;
            case Tab.Promotion: BuildPromotion(); break;
            case Tab.Supports: BuildSupports(); break;
            case Tab.Save: BuildSave(); break;
        }
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
                row.AddChild(Small("Equip", () => Do(() => { u.EquippedIndex = slot; return null; }, $"{u.Def.Name} equips {w.Name}.")));
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
        Row("Buy", 17, UiKit.Accent);
        foreach (var itemId in HubService.ShopStock(Db, S).OrderBy(Db.ItemPrice))
        {
            int price = Db.ItemPrice(itemId);
            var row = Row($"   {Db.ItemName(itemId),-22} {Describe(itemId)}");
            row.AddChild(UiKit.Label($"{price}g", 15, UiKit.Accent));
            row.AddChild(Small("Buy", () => Do(() => HubService.Buy(Db, S, itemId), $"Bought {Db.ItemName(itemId)} (sent to the convoy)."), S.Gold >= price));
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
                ItemKind.StatBoost => $"{i.Stat} +{i.Amount}",
                _ => $"Promotes to tier {i.Tier}",
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
        Row($"Promotion needs level {PromotionService.MinLevel}, a seal, and a crest tier at least the new class's tier. Crest: tier {S.CrestTier}.", 14, UiKit.Muted);
        Row($"Seals held: {HubService.Seals(Db, S).Count()}", 14, UiKit.Muted);
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
