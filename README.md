# RougeLite101

A 2D roguelike game built with Unity featuring procedural dungeon generation and manual tilemap room design.

## Project Overview

RougeLite101 is a Unity-based roguelike game that combines procedural dungeon generation with handcrafted room design. The game features a player controller, enemy AI systems, spell casting, and a modular room-based dungeon system.

## Recent Updates

### v0.2 - Inventory & Progression Systems

- **Inventory System**: Complete item management with drag-and-drop UI
- **Weapon Hub**: Centralized weapon pickup and equip system
- **Stat Modifiers**: Items that modify player stats (health, mana, etc.)
- **Skill Tree**: Character progression with unlockable skills
- **Leveling System**: Experience gain and stat upgrades
- **Structure Randomizer**: Enhanced dungeon generation with randomized layouts
- **Improved Dungeon Manager**: Better room spawning and enemy management

### v0.1 - Core Systems

- Basic dungeon generation with room templates
- Player controller with movement and combat
- Enemy AI with pathfinding
- Spell casting system
- Health and damage systems

## Documentation Structure

This project's documentation is organized into the following categories:

### 📚 [Documentation/](./Documentation/)

- **[Level Design/](./Documentation/Level%20Design/)** - Room creation and level design guides
- **[Quick References/](./Documentation/Quick%20References/)** - Quick reference cards and cheat sheets
- **[System Architecture/](./Documentation/System%20Architecture/)** - Technical documentation and system overviews

## Getting Started

### Prerequisites

- Unity 2022.3 LTS or later
- Basic knowledge of Unity 2D development
- Understanding of tilemap systems

### Quick Start

1. Open the project in Unity
2. Load the main scene
3. Press Play to test the dungeon generation system
4. Use the Level Design documentation to create new rooms

## Key Features

### 🏰 Dungeon System

- **Procedural Generation**: Automatic room selection and progression
- **Structure Randomizer**: Randomized room layouts and enemy placements
- **Manual Room Design**: Create handcrafted rooms using tilemaps
- **Theme-based Organization**: Group rooms by themes (Forest, Cave, etc.)
- **Modular Architecture**: Easy to add new rooms and themes

### 🎮 Gameplay Systems

- **Player Controller**: 2D movement with input system integration
- **Enemy AI**: Multiple enemy types with different behaviors
- **Spell System**: Casting system with cooldowns and UI
- **Health & Combat**: Damage system with knockback effects
- **Weapon System**: Bows, swords, and weapon pickup mechanics
- **Inventory System**: Item management, equipping, and stat modifiers
- **Skill Tree & Leveling**: Character progression with skill unlocks
- **Player Dash**: Dash ability with input and visual effects

### 🎯 Progression Systems

- **AI Director**: Dynamic combat pacing and difficulty scaling
- **Meta Progression**: Persistent upgrades across runs
- **Run State Management**: Track run progress and rewards
- **Save System**: Auto-save and manual save functionality

### 🛠️ Development Tools

- **Room Creation Helper**: Automated room structure setup
- **Room Template Validator**: Validation tools for room prefabs
- **Error Detection**: Comprehensive error checking and auto-fixing
- **Inventory UI**: Drag-and-drop item management
- **Weapon HUD**: Real-time weapon status display
- **Cursor Manager**: Custom cursor system

## Room Creation Workflow

For detailed room creation instructions, see:

- [Manual Tilemap Room Guide](./Documentation/Level%20Design/Manual_Tilemap_Room_Guide.md) - Complete step-by-step guide
- [Tilemap Quick Reference](./Documentation/Quick%20References/Tilemap_Quick_Reference.md) - Quick reference for experienced users

### Quick Room Creation

1. Open `Tools → Dungeon → Room Creation Helper`
2. Enter room name and configure settings
3. Click "Create Room Structure"
4. Paint tiles using the Tile Palette
5. Save as prefab and add to theme

## Project Structure

```text
RougeLite101/
├── Assets/
│   ├── Scripts/
│   │   ├── Dungeon/          # Dungeon generation system
│   │   ├── Player/           # Player controller and systems
│   │   ├── Enemies/          # Enemy AI and behaviors
│   │   ├── UI/               # User interface systems
│   │   ├── Director/         # AI Director and progression systems
│   │   ├── SaveSystem/       # Save/load functionality
│   │   ├── PickUpSystem/     # Item pickup mechanics
│   │   ├── Model(ScriptableObject)/  # Data models and configurations
│   │   ├── Modifiers/        # Stat modifiers and rewards
│   │   ├── Misc/             # Utility scripts
│   │   └── Editor/           # Development tools
│   ├── Prefabs/
│   │   └── Rooms/            # Room prefabs organized by theme
│   ├── ScriptableObjects/
│   │   ├── Themes/           # Theme configurations
│   │   ├── Inventory/        # Item definitions
│   │   └── PlayerStatsModifier/  # Stat modifier configurations
│   └── Sprites/
│       └── Tilemap/          # Tilemap assets and tiles
└── Documentation/            # Project documentation
    ├── Level Design/         # Room creation guides
    ├── Quick References/     # Quick reference materials
    └── System Architecture/  # Technical documentation
```

## Contributing

When adding new features or rooms:

1. Follow the existing code structure and naming conventions
2. Update relevant documentation
3. Test integration with the existing dungeon system
4. Use the provided validation tools

## Development Team

- Level Design: Room creation and tilemap design
- Programming: System architecture and gameplay mechanics
- Art: Sprite creation and visual design

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

### Third-Party Assets

This project may include third-party assets and libraries. Please refer to their respective licenses:

- **Unity Engine**: [Unity Terms of Service](https://unity3d.com/legal/terms-of-service)
- **Tilemap Assets**: Please check individual asset licenses in the project

### Contributing

By contributing to this project, you agree that your contributions will be licensed under the same MIT License.

---

For detailed information on specific systems, please refer to the documentation in the respective folders.