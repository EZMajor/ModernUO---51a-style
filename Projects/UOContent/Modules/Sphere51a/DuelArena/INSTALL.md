# DuelArena System - Installation Guide

## Overview
The DuelArena system is a self-contained, portable duel system for ModernUO shards. It provides a single 8x8 arena with duel stone functionality for player-vs-player dueling.

## Requirements
- **ModernUO** with SerializationGenerator support (v0.9.0 or higher recommended)
- **.NET 9.0** or higher
- **Sphere 51a master toggle enabled** (`sphere.enableSphere51aStyle = true` in configuration)
- Core ModernUO libraries (Server.dll)

## Installation Steps

### 1. Copy Files
Copy the entire `DuelArena` folder to your shard's `Projects/UOContent/Engines/` directory.

```
YourShard/
└── Projects/
    └── UOContent/
        └── Engines/
            └── DuelArena/          <-- Copy this folder here
                ├── DuelArena.csproj    # Separate project file
                ├── DuelArenaLoader.cs  # Bootstrap loader
                ├── DuelArenaSystem.cs  # Module entry point
                ├── Assemblies/         # Compiled DLL goes here
                ├── Core/
                ├── Configuration/
                ├── Regions/
                ├── Gumps/
                ├── Extensions/
                ├── Items/
                │   ├── DuelStones/
                │   └── Arena/
                └── Migrations/
```

### 2. Build the DuelArena Module
First, build the separate DuelArena.dll:

```bash
cd Projects/UOContent/Engines/DuelArena
dotnet build
```

This will create `DuelArena.dll` in the `Assemblies/` folder.

### 3. Build Your Main Shard
Compile your main shard as normal:

```bash
dotnet build
```

or

```bash
dotnet publish -c Release
```

### 4. Start Your Server
Start your server. The DuelArena system will automatically detect and load the module on startup.

## Verification

After starting your server, verify the installation by using the admin commands:

```
[duelstats         - View duel statistics (Player access)
[configduelstone   - Configure a duel stone (GameMaster)
[add duelarena     - Place a duel arena deed in backpack (Administrator)
[add duelstone     - Place a standalone duel stone at location (Administrator)
```

## Quick Start

### Place a Duel Arena

1. Log in as an Administrator
2. Type `[add duelarena`
3. Follow the prompts to select a location
4. The arena will be created automatically with:
   - 10x10 footprint (8x8 interior)
   - Stone walls with proper corner pieces
   - Floor tiles
   - Spawn markers
   - One duel stone on the east side

### Configure Duel Settings

Use `[configduelstone` and target the duel stone to access the configuration gump where you can set:
- Duel type (Money1v1, Loot1v1, etc.)
- Entry cost
- Idle timers
- Ladder settings

## Troubleshooting

### Build Errors
- Ensure you're using ModernUO v0.9.0 or higher
- Verify all files copied correctly
- Check that SerializationGenerator is enabled in your project

### Runtime Errors
- Check server console for error messages
- Verify the DuelArenaSystem.Configure() is being called on startup
- Ensure no namespace conflicts with existing systems

## Uninstallation

To remove the DuelArena system:

1. Stop your server
2. Delete the `Projects/UOContent/Engines/DuelArena/` folder
3. Rebuild your shard
4. Start your server

Note: This will remove all placed arenas and duel stones from your world.

## Support

For issues or questions:
- Check the README.md for detailed system documentation
- Review the source code comments
- Ensure you're using a compatible ModernUO version

## Compatibility

This system has no external dependencies outside of core ModernUO libraries. It will work on any ModernUO shard without modification.

Optional features:
- **Titles System**: If your shard has `Server.Misc.Titles`, fame will be awarded automatically. Otherwise, this feature is safely skipped.
