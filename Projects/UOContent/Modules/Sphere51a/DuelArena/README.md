# DuelArena System

A complete, self-contained duel arena system for ModernUO shards featuring automated dueling mechanics, protected regions, and comprehensive player management. This system is controlled by the Sphere 51a master toggle and requires it to be enabled for operation.

## Features

### Core Features
- **Single 8x8 Duel Pit**: Compact, efficient arena design
- **Protected Region**: No karma loss, murder counts, or criminal flags during duels
- **Automated Duel Management**: Full lifecycle from invitation to completion
- **Multiple Duel Types**:
  - Money 1v1: Wagered gold duels
  - Loot 1v1: Winner takes all loot
  - Money 2v2: Team-based wagered duels
  - Loot 2v2: Team-based loot duels

### Arena Features
- **Professional Design**: Stone walls with proper corner pieces (IDs: 220/SE, 223/NW)
- **Spawn System**: Automated player positioning at opposite corners
- **Duel Stone**: Single stone positioned on east side for easy access
- **Visual Markers**: Floor tiles and spawn indicators
- **Portable Structure**: Complete arena can be placed anywhere via deed

### Player Features
- **Invitation System**: Interactive gump-based invitations
- **Ready Check**: Countdown system before duel starts
- **Statistics Tracking**: Kills, deaths, wins, losses per player
- **Loot Phase**: Optional post-duel looting period
- **Auto-Resurrection**: Players are resurrected and healed after duels
- **Equipment Restoration**: Stats, buffs, and equipment preserved

### Administrative Features
- **Configuration Gump**: Easy setup via in-game interface
- **Multiple Commands**: Flexible placement and management options
- **Idle Management**: Configurable timeouts to prevent arena camping
- **Region Protection**: Prevents interference during duels
- **Serialization**: Full save/load support for persistence

## Installation

See [INSTALL.md](INSTALL.md) for detailed installation instructions.

**Quick Install:**
1. Copy the `DuelArena` folder to `Projects/UOContent/Engines/`
2. Build your shard: `dotnet build`
3. Start your server

## Commands

### Player Commands
- `[duelstats` - View your duel statistics

### GameMaster Commands
- `[configduelstone` - Configure duel stone settings (target stone after command)

### Administrator Commands
- `[add duelarena` - Place a duel arena deed in backpack
- `[add duelstone` - Place a standalone duel stone at current location

## Architecture

### Folder Structure
```
DuelArena/
├── Core/                       # Core duel logic
├── Configuration/              # Arena configuration
├── Regions/                    # Protected region
├── Gumps/                      # UI components
├── Extensions/                 # Death handling
├── Items/
│   ├── DuelStones/            # Clickable stone
│   └── Arena/                 # Arena structure
├── Migrations/                 # Serialization
└── DuelArenaSystem.cs         # Entry point
```

## Portability

This system is designed to be **100% portable**:
- No external UOContent dependencies
- Single self-contained folder
- Drop-in installation
- No configuration files required
- Works on any ModernUO shard
- Optional features degrade gracefully

## Technical Details

### Duel State Machine
```
Waiting → Countdown → InProgress → Ending → LootPhase → Completed
```

### Dependencies
**Required (Core ModernUO)**: Server, Server.Items, Server.Mobiles, Server.Regions, Server.Gumps, Server.Network, Server.Targeting, ModernUO.Serialization

**Optional**: Server.Misc.Titles (for fame awards)

## License
This code is provided as-is for use in ModernUO shards. Modify and distribute freely.
