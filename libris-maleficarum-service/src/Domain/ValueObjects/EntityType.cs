namespace LibrisMaleficarum.Domain.ValueObjects;

/// <summary>
/// Defines the types of entities that can be created within a world.
/// </summary>
public enum EntityType
{
    // Geographic types
    /// <summary>
    /// A continent-level geographic region.
    /// </summary>
    Continent,

    /// <summary>
    /// A country-level geographic region.
    /// </summary>
    Country,

    /// <summary>
    /// A region-level geographic area within a country.
    /// </summary>
    Region,

    /// <summary>
    /// A city, town, or settlement.
    /// </summary>
    City,

    /// <summary>
    /// A building or structure within a settlement.
    /// </summary>
    Building,

    /// <summary>
    /// A room or interior space within a building.
    /// </summary>
    Room,

    /// <summary>
    /// A generic location within the world (not otherwise categorized).
    /// </summary>
    Location,

    // Character & faction types
    /// <summary>
    /// A character within the world (player character, NPC, villain, etc.).
    /// </summary>
    Character,

    /// <summary>
    /// A faction, organization, guild, or political group.
    /// </summary>
    Faction,

    // Event & quest types
    /// <summary>
    /// An event, occurrence, or historical moment.
    /// </summary>
    Event,

    /// <summary>
    /// A quest, mission, or objective.
    /// </summary>
    Quest,

    // Item types
    /// <summary>
    /// An item, artifact, or piece of equipment.
    /// </summary>
    Item,

    // Campaign types
    /// <summary>
    /// A campaign or adventure arc.
    /// </summary>
    Campaign,

    /// <summary>
    /// A game session or play session.
    /// </summary>
    Session,

    // Container types - organizational top-level categories
    /// <summary>
    /// General organizational container for any entity types.
    /// </summary>
    Folder,

    /// <summary>
    /// Container for location entities.
    /// </summary>
    Locations,

    /// <summary>
    /// Container for character and NPC entities.
    /// </summary>
    People,

    /// <summary>
    /// Container for event entities.
    /// </summary>
    Events,

    /// <summary>
    /// Container for historical records.
    /// </summary>
    History,

    /// <summary>
    /// Container for lore and world-building content.
    /// </summary>
    Lore,

    /// <summary>
    /// Container for creature and monster entities.
    /// </summary>
    Bestiary,

    /// <summary>
    /// Container for item and artifact entities.
    /// </summary>
    Items,

    /// <summary>
    /// Container for adventure and campaign entities.
    /// </summary>
    Adventures,

    /// <summary>
    /// Container for geographic entities.
    /// </summary>
    Geographies,

    // Regional types with custom properties
    /// <summary>
    /// A geographic region with climate, terrain, population, and area properties.
    /// </summary>
    GeographicRegion,

    /// <summary>
    /// A political region with government type, member states, and established date.
    /// </summary>
    PoliticalRegion,

    /// <summary>
    /// A cultural region with languages, religions, and cultural traits.
    /// </summary>
    CulturalRegion,

    /// <summary>
    /// A military region with garrison size, command structure, and strategic importance.
    /// </summary>
    MilitaryRegion,

    // Other (catch-all)
    /// <summary>
    /// Any other entity type not covered by predefined categories.
    /// </summary>
    Other
}

