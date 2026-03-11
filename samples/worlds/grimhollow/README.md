# Grimhollow — Sample World

A dark fantasy world shrouded in perpetual twilight, where ancient evils stir beneath crumbling empires and forgotten gods whisper from the shadows. Perfect for gritty TTRPG campaigns.

## Entity Hierarchy

```text
Grimhollow (World)
│
├── The Grimhollow Continent (Continent)
│   ├── The Iron Dominion (Country)
│   │   ├── Blackmoor Marshes (Region)
│   │   │   ├── Ironhold (City)
│   │   │   │   ├── Ironhold Castle (Building)
│   │   │   │   ├── King Aldric the Ironheart (Character)
│   │   │   │   └── The Shadow Assassin (Character)
│   │   │   └── Marshaven (City)
│   │   │       └── Merchant Gilda Goldweave (Character)
│   │   ├── Crystalpeak Mountains (Region)
│   │   │   └── Silverdale (City)
│   │   │       ├── The Temple of Fractured Light (Building)
│   │   │       └── Queen Elara of the Silver Peak (Character)
│   │   └── The Iron Guard (Faction)
│   ├── The Sylvan Reach (Country)
│   │   ├── The Whispering Woods (Region)
│   │   │   ├── Thornwall (City)
│   │   │   │   ├── The Hollow Bough Tavern (Building)
│   │   │   │   └── Captain Thorne Blackbriar (Character)
│   │   │   └── Elder Whisper (Character)
│   │   └── The Sylvan Council (Faction)
│   └── The Shadow Court (Faction)
│
└── The Shadow War (Campaign)
    ├── Defend Ironhold (Quest)
    ├── Find the Lost Artifact (Quest)
    └── Investigate the Marshes (Quest)
```

## Entity Type Breakdown

| Entity Type | Count |
|-------------|------:|
| Building    |     3 |
| Campaign    |     1 |
| Character   |     6 |
| City        |     4 |
| Continent   |     1 |
| Country     |     2 |
| Faction     |     3 |
| Quest       |     3 |
| Region      |     3 |
| **Total**   |**26** |

## Usage

Validate the world data against the schema:

```bash
libris world validate --source ./samples/worlds/grimhollow
```

Import the world into a running Libris Maleficarum instance:

```bash
libris world import --source ./samples/worlds/grimhollow --api-url <url> --token <token>
```

## File Structure

```text
grimhollow/
├── world.json                              # World metadata (name, description)
└── entities/
    ├── buildings/
    │   ├── ironhold-castle.json
    │   ├── silverdale-temple.json
    │   └── thornwall-tavern.json
    ├── campaigns/
    │   └── the-shadow-war.json
    ├── characters/
    │   ├── captain-thorne.json
    │   ├── elder-whisper.json
    │   ├── king-aldric.json
    │   ├── merchant-gilda.json
    │   ├── queen-elara.json
    │   └── shadow-assassin.json
    ├── cities/
    │   ├── ironhold.json
    │   ├── marshaven.json
    │   ├── silverdale.json
    │   └── thornwall.json
    ├── continents/
    │   └── grimhollow-continent.json
    ├── countries/
    │   ├── iron-dominion.json
    │   └── sylvan-reach.json
    ├── factions/
    │   ├── iron-guard.json
    │   ├── shadow-court.json
    │   └── sylvan-council.json
    ├── quests/
    │   ├── defend-ironhold.json
    │   ├── find-the-lost-artifact.json
    │   └── investigate-the-marshes.json
    └── regions/
        ├── blackmoor-marshes.json
        ├── crystalpeak-mountains.json
        └── whispering-woods.json
```
