using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Dialogue;

namespace NotFireEmblem.Campaign;

/// <summary>
/// Where the party is between chapters (session 18, the Act II hub): Brookhollow's village hall in Act I, the palace
/// lodgings in Calder and the harbour at Solmere in Act II. The hub screen shows its picture, plays its music and
/// gives Osmund its lines. Read from the briefing scene's background, so the place always matches the briefing.
/// </summary>
public sealed record HubPlace(string Id, string Name, string Background, string Music, string ShopLine, string IdleLine)
{
    public static readonly HubPlace Brookhollow = new("brookhollow", "Brookhollow", "village_hall", "hub",
        "The trader's prices are robbery, my lord, but polite robbery.",
        "What shall we attend to, my lord?");

    public static readonly HubPlace Calder = new("calder", "Calder, the palace lodgings", "calder_lodgings", "hub_calder",
        "The Royal Market charges palace prices, my lord. For palace steel, I grant.",
        "The guard at our door sends his regards, my lord. What shall we attend to?");

    public static readonly HubPlace Solmere = new("solmere", "Solmere, the Free Cities", "solmere_harbor", "hub_solmere",
        "In Solmere everything has a price, my lord. Including the haggling.",
        "Salt, money and lawyers, my lord. What shall we attend to?");

    public static IReadOnlyList<HubPlace> All { get; } = new[] { Brookhollow, Calder, Solmere };

    /// <summary>The place a briefing scene is set in (its first @bg); Brookhollow when there's no briefing.</summary>
    public static HubPlace For(DialogueScene? briefing)
    {
        string? bg = briefing?.Lines.OfType<BackgroundLine>().FirstOrDefault()?.Id;
        return bg switch
        {
            { } b when b.StartsWith("calder") => Calder,
            { } b when b.StartsWith("solmere") => Solmere,
            _ => Brookhollow,
        };
    }
}
