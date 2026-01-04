namespace LibrisMaleficarum.Domain.ValueObjects;

/// <summary>
/// Defines the types of entities that can be created within a world.
/// </summary>
public enum EntityType
{
    /// <summary>
    /// A character within the world (player character, NPC, villain, etc.).
    /// </summary>
    Character,

    /// <summary>
    /// A generic location within the world (not otherwise categorized).
    /// </summary>
    Location,

    /// <summary>
    /// A campaign or adventure arc.
    /// </summary>
    Campaign,

    /// <summary>
    /// A game session or play session.
    /// </summary>
    Session,

    /// <summary>
    /// A faction, organization, guild, or political group.
    /// </summary>
    Faction,

    /// <summary>
    /// An item, artifact, or piece of equipment.
    /// </summary>
    Item,

    /// <summary>
    /// A quest, mission, or objective.
    /// </summary>
    Quest,

    /// <summary>
    /// An event, occurrence, or historical moment.
    /// </summary>
    Event,

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
    /// Any other entity type not covered by predefined categories.
    /// </summary>
    Other
}
