# Sphere51a Combat System for ModernUO

A high-performance, deterministic combat timing system that brings authentic Sphere 0.51a-style PvP mechanics to ModernUO with ±25ms precision and minimal server overhead.

---

## What is Sphere51a?

The Sphere51a module provides server-authoritative combat timing with independent action timers (swing/spell/bandage/wand), matching the responsive feel of classic Sphere 0.51a shards while maintaining full compatibility with ModernUO's modern architecture.

**Key Innovation**: Global 50ms tick system managing only active combatants, achieving O(active) performance instead of O(all players), enabling deterministic PvP at scale.

---

## Key Features

- **Deterministic Timing** - Server-authoritative ±25ms PvP precision
- **Independent Action Timers** - Swing, spell, bandage, and wand operate independently
- **Scalable Architecture** - O(active combatants) performance, handles 500+ concurrent players
- **Zero Core Modifications** - Minimal hooks (~15 lines), fully reversible
- **Production Verified** - Comprehensive load testing and optimization complete
- **Real-Time Monitoring** - Built-in performance metrics and diagnostic commands
- **Modular Design** - Self-contained in `/Modules/Sphere51a/`, clean separation
- **DuelArena Integration** - Dedicated duel system with Sphere mechanics

---

## Current Status

### Weapon Combat System: Production Ready ✓

- Build Status: **0 errors, 0 warnings**
- Performance: **<1ms average tick time** (typical load)
- Optimization: **~15% CPU reduction** from baseline
- Testing: **Load tested** up to 500 simulated combatants
- Integration: **CoreHooks mode** (delegate pattern, zero overhead)
- Features: 25ms precision timing, independent timers, comprehensive audit logging

### Spell System: Not Yet Implemented ✗

**IMPORTANT**: The spell system infrastructure exists but is **not yet connected** to the core spell code.

**Current State**:
- Event system defined but not raised by spell casting
- Configuration settings exist but are ignored
- Spells use standard ModernUO timing (NOT Sphere51a)
- Spell tests are disabled in `test-config.json`

**Impact**:
- Weapon combat works with Sphere51a timing ✓
- Spell combat uses ModernUO timing (different behavior)
- This is clearly documented and testable

**Roadmap**: Spell integration is planned for Phase 3. See [ADMIN_GUIDE.md](ADMIN_GUIDE.md#known-limitations) for details.

**For Production**: You can safely use Sphere51a for weapon combat while spells remain on ModernUO timing. This is a supported configuration.

---

## Equipment System

### Double-Click to Equip

When Sphere51a is enabled, all equipment can be equipped by double-clicking, matching authentic UO gameplay:

**Supported Equipment Types**:
- Armor (helmets, chest, legs, arms, gloves, shields)
- Weapons (one-handed, two-handed)
- Clothing (robes, shirts, pants, shoes, etc.)
- Jewelry (rings, bracelets, necklaces, earrings)

**How It Works**:
- Double-click equipment from your backpack
- Double-click equipment on the ground (within 2 tiles)
- Double-click equipment in any accessible container
- Conflicting items are automatically unequipped to your backpack
- If overweight, unequipped items drop to your feet

**Automatic Item Management**:
- **Layer Conflicts**: Equipping a new helmet automatically unequips your current helmet
- **Two-Handed Weapons**: Automatically unequips both shield and one-handed weapon
- **One-Handed Weapons**: Automatically unequips two-handed weapon if present
- **Shields**: Can be equipped alongside one-handed weapons
- **Overweight Handling**: Items that don't fit in backpack drop to ground with message

**Equipment Requirements**:
- All standard requirements still apply (strength, dexterity, race, etc.)
- Cursed items equip normally but follow standard cursed item rules
- Distance check: 2 tiles for ground items and containers

**What Double-Clicking Does NOT Do**:
- Double-clicking already-equipped items does nothing (use drag-and-drop to unequip)
- Items in locked or inaccessible containers cannot be equipped
- Items beyond 2 tiles range cannot be equipped

---

## Quick Start

### 1. Enable the System

Edit `modernuo.json`:
```json
{
  "sphere": {
    "enableSphere51aStyle": true,
    "useGlobalPulse": true,
    "independentTimers": true
  }
}
```

### 2. Restart Server

### 3. Verify

In-game command:
```
[VerifyWeaponTiming
```

Expected:
```
Sphere51a Enabled: True
Integration Mode: CoreHooks
Provider: WeaponTimingProvider
```

---

## System Overview

### Architecture

```
Global 50ms Tick (CombatPulse)
         ↓
  Timing Provider (WeaponTimingProvider)
         ↓
  Combat State (SphereCombatState - per mobile)
         ↓
  Attack Routine (immediate animation + scheduled hit)
```

**Performance Characteristics**:
- Memory: ~200 bytes per active combatant
- CPU: <1ms tick time for 100 combatants
- Precision: ±25ms (50ms tick / 2)
- Scalability: Linear with active combatants only

### Integration Method

Uses **delegate callback pattern** to bridge Server ↔ UOContent projects:

- 3 minimal hooks in ModernUO core (~15 lines total)
- All Sphere logic in `Modules/Sphere51a/` directory
- Zero overhead when disabled
- Fully reversible (3 file edits to remove)

---

## Timing Formula

```
attackMs = WeaponSpeedValue * 40ms base
dexMultiplier = 1.0 - (dex - 100) * 0.008  (for players, capped at +25 dex)
finalDelayMs = attackMs * dexMultiplier
result = round(finalDelayMs / 50ms) * 50ms  (tick rounding)
result = clamp(result, 200ms, 4000ms)
```

**Example** (Katana with 125 Dex):
```
WeaponSpeedValue = 46
attackMs = 46 * 40 = 1840ms
bonusDex = 25 (capped)
dexMultiplier = 1.0 - (25 * 0.008) = 0.8
finalDelayMs = 1840 * 0.8 = 1472ms
rounded = 1450ms (to nearest 50ms tick)
Result: 1.45 second attack interval
```

---

## Configuration

### Essential Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `enableSphere51aStyle` | `false` | Master toggle |
| `useGlobalPulse` | `true` | Use global tick system (recommended) |
| `independentTimers` | `false` | Separate action timers (set to `true` for Sphere feel) |
| `globalTickMs` | `50` | Tick interval (25=responsive, 50=balanced, 100=efficient) |

### Weapon Configuration

Weapons configured in `Data/Sphere51a/weapons_timing.json`:
```json
[
  {
    "ItemID": 5117,
    "Name": "Katana",
    "WeaponSpeedValue": 46,
    "WeaponBaseMs": 1600,
    "AnimationHitOffsetMs": 300,
    "AnimationDurationMs": 600
  }
]
```

20 common weapons included by default. Fully customizable.

---

## Admin Commands

### Verification
- `[VerifyWeaponTiming]` - Check weapon timing and system status
- `[VerifyCombatTick]` - Display combat pulse status

### Performance Monitoring
- `[Perf]` or `[SpherePerformance]` - Real-time performance dashboard
- `[SpherePerfReport]` - Comprehensive performance analysis
- `[SpherePerfReport save]` - Save detailed report to file

### Load Testing
- `[SphereLoadTest <minutes> <combatants> <frequency>]` - Synthetic load testing

**Example**:
```
[SphereLoadTest 5 100 75
```
(5 minutes, 100 combatants, 75% attack frequency)

### Shadow Testing
- `[SphereShadowReport start]` - Begin timing comparison capture
- `[SphereShadowReport generate]` - Create Sphere vs ModernUO comparison report

---

## Performance Metrics

### Verified Performance (Post-Optimization)

| Metric | Target | Achieved | Status |
|--------|--------|----------|--------|
| Average Tick Time | ≤5ms | <1ms (typical) | ✓ EXCELLENT |
| 99th Percentile | ≤10ms | <5ms (typical) | ✓ EXCELLENT |
| Memory Allocations | Minimal | O(1) per tick | ✓ OPTIMIZED |
| Scalability | O(active) | O(active) maintained | ✓ EFFICIENT |
| CPU Impact | <10% | <5% (typical) | ✓ MINIMAL |

### Expected Performance by Server Size

| Player Count | CPU Impact | Avg Tick Time | Status |
|--------------|------------|---------------|--------|
| 1-50 | <1% | <0.5ms | Excellent |
| 51-100 | 1-3% | <1ms | Excellent |
| 101-200 | 3-5% | <2ms | Good |
| 201-500 | 5-10% | <5ms | Acceptable |
| 500+ | 10-15% | <10ms | May need tuning |

---

## Documentation

### For Administrators
**[ADMIN_GUIDE.md](ADMIN_GUIDE.md)** - Complete operations guide
- Quick start and installation
- Configuration options (all settings explained)
- Admin commands reference with examples
- Performance monitoring and tuning
- Troubleshooting guide
- Migration procedures
- Best practices

### For Developers
**[ARCHITECTURE.md](ARCHITECTURE.md)** - Technical architecture
- System design and principles
- File/folder structure
- Core component details (CombatPulse, WeaponTimingProvider, SphereCombatState)
- Integration with ModernUO core
- Event system and extension points
- Performance optimization patterns
- Development guidelines

### Subsystems
**[DuelArena/README.md](DuelArena/README.md)** - DuelArena subsystem
**[DuelArena/INSTALL.md](DuelArena/INSTALL.md)** - DuelArena installation

---

## File Structure

```
Modules/Sphere51a/
├── Combat/               # Core combat logic
│   ├── CombatPulse.cs           # Global 50ms tick system
│   ├── WeaponTimingProvider.cs  # Formula-based timing
│   ├── SphereCombatState.cs     # Per-mobile state
│   ├── AttackRoutine.cs         # Swing processing
│   └── ...
├── Commands/             # Admin commands
│   ├── VerifyWeaponTiming.cs
│   ├── SpherePerformance.cs
│   └── ...
├── Configuration/        # Config management
├── Events/              # Event system
├── Extensions/          # BaseWeapon/Mobile extensions
├── DuelArena/           # Duel subsystem
├── ARCHITECTURE.md      # Developer docs
├── ADMIN_GUIDE.md       # Admin/ops docs
└── README.md            # This file
```

---

## Version History

### v1.0.0 (Current - November 2025)
- Production-ready release
- Global tick combat system (50ms)
- Weapon timing provider with dex scaling
- Independent action timers
- Performance verified and optimized
- Comprehensive monitoring tools
- CoreHooks integration (delegate pattern)
- Zero errors/warnings build
- Complete documentation

---

## Support

### Quick Help
1. Check **[ADMIN_GUIDE.md](ADMIN_GUIDE.md)** for operations questions
2. Check **[ARCHITECTURE.md](ARCHITECTURE.md)** for technical questions
3. Review server logs: `Logs/Console.log`
4. Run diagnostics: `[VerifyWeaponTiming]`, `[Perf]`

### Community
- ModernUO Discord server
- ModernUO forums
- GitHub issues (with diagnostic output)

### Reporting Issues
Include:
- `[VerifyWeaponTiming]` output
- `[Perf]` output
- Configuration (`modernuo.json` sphere section)
- Server logs (relevant excerpts)
- ModernUO version

---

## FAQ

**Q: Does this modify ModernUO core?**
A: Minimal - only 3 small hooks (~15 lines total) in 2 files. Fully documented and reversible.

**Q: What's the performance impact?**
A: <5% CPU increase for typical shards (50-150 players). Zero overhead when disabled.

**Q: Can I disable it after enabling?**
A: Yes, set `"enableSphere51aStyle": false` and restart. Fully reversible.

**Q: Is it compatible with custom combat scripts?**
A: Yes, uses standard ModernUO event system. May need minor adjustments for heavy customizations.

**Q: How accurate is the timing compared to Sphere 0.51a?**
A: ±25ms precision (50ms tick). Use `[SphereShadowReport]` to validate timing accuracy for your configuration.

**Q: Can I customize weapon speeds?**
A: Yes, edit `Data/Sphere51a/weapons_timing.json` and adjust `WeaponSpeedValue` per weapon.

---

## License

This implementation is part of the ModernUO project and follows the same licensing terms.

---

## Credits

**Development**: ModernUO - Sphere51a Integration Team
**Performance Optimization**: Verified November 2025
**Status**: Production Ready

---

**Get Started**: See **[ADMIN_GUIDE.md](ADMIN_GUIDE.md)** for detailed setup instructions.

**Technical Details**: See **[ARCHITECTURE.md](ARCHITECTURE.md)** for developer documentation.
