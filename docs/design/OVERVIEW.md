# Libris Maleficarum Overview

**Libris Maleficarum** (Latin for "Book of Witchcraft") is an AI-enhanced campaign management and world-building platform for tabletop role-playing games (TTRPGs). The application enables game masters and players to create, organize, and manage rich, interconnected campaign worlds with AI-powered assistance.

## Purpose

Libris Maleficarum addresses the challenge of managing complex TTRPG campaign worlds by providing:

- **Hierarchical World Organization**: Structure campaigns as nested entities (worlds, continents, regions, countries, cities, characters, items, etc.) with unlimited depth
- **Multi-System Support**: Flexible schema system supporting D&D 5e, Pathfinder, custom systems, and future game systems
- **AI-Powered Assistance**: Intelligent agents that help generate content, maintain consistency, and provide contextual suggestions
- **Rich Asset Management**: Organize maps, character portraits, session recordings, and documents alongside campaign data
- **Collaborative Features**: Multi-user support with ownership controls and real-time synchronization

## Core Capabilities

### World Building

- Create hierarchical world structures with arbitrary nesting (continents → regions → countries → cities → locations)
- Define characters, monsters, items, organizations, and other campaign entities
- Organize content using semantic entity types (GeographicRegion, PoliticalRegion, etc.) and cross-cutting tags
- Attach rich descriptions, properties, and system-specific attributes to any entity

### Campaign Management

- Structure campaigns into scenarios, sessions, and scenes
- Track campaign progress, player characters, and session notes
- Link entities across the world (characters to locations, items to characters, etc.)
- Manage soft deletes with 30-day grace period and 90-day retention for recovery

### Asset Organization

- Upload and organize images, audio, video, and documents
- Automatic thumbnail generation and format optimization
- Secure storage with time-limited access tokens
- Version control for important assets (maps, official artwork)

### AI Assistance

- Conversational interface for world-building tasks
- Content generation with context awareness (understands campaign world)
- Consistency checking across related entities
- Intelligent suggestions based on campaign history and genre conventions

### Search and Discovery

- Full-text search across all campaign content
- Semantic search for conceptual queries ("find all coastal cities")
- Tag-based filtering and organization
- Hierarchical navigation with lazy-loading for performance

## User Experience

### Target Users

- **Game Masters**: Primary users who create and manage campaign worlds
- **Players**: Collaborative users who access shared campaign content
- **Content Creators**: Users building reusable world templates and assets

### Key Workflows

1. **Create New World**: Start with blank world or template, define root-level structure
1. **Build Hierarchy**: Add nested locations, characters, and campaign elements
1. **Enrich Content**: Attach descriptions, properties, and assets to entities
1. **Run Campaign**: Manage sessions, track progress, update entity states
1. **AI Collaboration**: Use chat interface to generate content and get suggestions
1. **Search & Navigate**: Find entities via search or hierarchical tree navigation

### Access Patterns

- Single-user focus: Users interact with one world at a time
- Tree-based navigation: Expand/collapse hierarchy nodes on demand
- Entity-centric editing: Focus on one entity at a time with contextual information
- Real-time updates: Changes propagate to collaborators immediately

## System Characteristics

### Scalability

- Support unlimited entities per world (tested to 100,000+ entities)
- Efficient querying via hierarchical partition keys (1-5 RU per operation)
- Hot partition prevention through entity-level partitioning
- Lazy-loading hierarchy prevents performance degradation with large worlds

### Flexibility

- System-agnostic core model (supports any TTRPG system)
- Schema-driven property validation (add new game systems without code changes)
- Extensible entity type system (add new entity types as needed)
- Tag-based cross-cutting organization (entities can belong to multiple logical groups)

### Security & Privacy

- User-owned worlds (OwnerId on all entities)
- Row-level security enforcement
- Private by default (no public world sharing in initial release)
- Secure asset access via time-limited tokens

### Multi-Tenancy

- World-scoped data isolation (WorldId partition key)
- Efficient queries within tenant boundaries
- Cost optimization per world (pay only for active usage)

## Future Capabilities

- Public world sharing and discovery
- Marketplace for world templates and assets
- Enhanced collaboration features (comments, annotations, shared editing)
- Advanced AI features (procedural generation, campaign plot suggestions)
- Mobile application support
- Offline mode for game sessions
