using System.Collections.Generic;
using Godot;
using NotFireEmblem.Core;
using NotFireEmblem.Units;

namespace NotFireEmblem.UI;

/// <summary>
/// Item icons (session 17 polish; ART_NEEDED §6): assets/ui/icons/icon_&lt;kind&gt;.png, one per weapon type, item kind
/// and armour slot, picked from tools/art/takes/props/icon_*. Null when the art is missing, so callers keep their
/// text-only look.
/// </summary>
public static class ItemIcons
{
    private const string Root = "res://assets/ui/icons";
    private static readonly Dictionary<string, Texture2D?> Cache = new();

    public static Texture2D? For(ContentDatabase db, string itemId)
    {
        string? name = db.TryWeapon(itemId) is { } w
            ? w.Type switch
            {
                WeaponType.Sword => "sword",
                WeaponType.Lance => "lance",
                WeaponType.Axe => "axe",
                WeaponType.Bow => "bow",
                WeaponType.Fist => "claw",
                WeaponType.BlackMagic => "tome_red",
                WeaponType.WhiteMagic => "tome_white",
                _ => null,
            }
            : db.TryArmor(itemId) is { } a ? (a.Slot == ArmorSlot.Body ? "armor" : "ring")
            : db.TryItem(itemId) is { } i ? i.Kind switch
            {
                ItemKind.Heal => "potion",
                ItemKind.StatBoost => "booster",
                ItemKind.Key => "key",
                _ => null,
            }
            : null;
        return name is null ? null : Load(name);
    }

    private static Texture2D? Load(string name)
    {
        if (!Cache.TryGetValue(name, out var texture))
        {
            string path = $"{Root}/icon_{name}.png";
            texture = ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
            Cache[name] = texture;
        }
        return texture;
    }

    /// <summary>A small fixed-size picture of the icon for a row (nothing when there's no art).</summary>
    public static TextureRect? Rect(ContentDatabase db, string itemId, int px = 20) =>
        For(db, itemId) is { } texture
            ? new TextureRect
            {
                Texture = texture,
                CustomMinimumSize = new Vector2(UiKit.Px(px), UiKit.Px(px)),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            }
            : null;
}
