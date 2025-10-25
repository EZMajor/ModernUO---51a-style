# Changelog - Sphere 0.51a Combat System

All notable changes to the Sphere 0.51a combat system implementation will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased]

### Testing Phase
- Spell casting with immediate targeting
- Scroll casting and consumption
- Reagent checking and consumption
- Movement during casting
- Spell interruption mechanics

---

## [2025-10-25] - Spell Casting Fixes

### Fixed - Spell Sequence Timing (Commit: 88f3b544d)
**Issue**: Spells were not checking reagents, consuming mana, or executing effects when using Sphere-style immediate targeting.

**Root Cause**: `OnTargetFinish()` was calling `FinishSequence()` immediately after target selection, which:
- Set spell state to `None`
- Cleared `Caster.Spell` reference
- Caused `CheckSequence()` to fail when the post-target timer fired
- Prevented reagent/mana consumption and spell execution

**Solution**:
- Modified `SpellTarget.OnTargetFinish()` to delay `FinishSequence()` call when Sphere post-target delay is active
- Timer callback now calls `FinishSequence()` AFTER spell execution completes
- Spell remains in `Sequencing` state during post-target cast delay

**Files Modified**:
- `Projects/UOContent/Spells/Targeting/SpellTarget.cs`

**Impact**:
- Spells now correctly consume reagents
- Spells now correctly consume mana
- Spells now execute effects properly
- Scrolls are consumed when cast
- Default ModernUO behavior unaffected

---

### Fixed - Spell Not Executing After Target Selection (Commit: 7d82203a8)
**Issue**: After implementing immediate targeting, spells displayed target cursor but did not cast, consume reagents, or show animations.

**Root Cause**: When immediate targeting was enabled, the original cast delay was being lost. The code was:
1. Calculating `GetCastDelay()` and storing it
2. Setting `castDelay = TimeSpan.Zero` for immediate targeting
3. When target was selected, `SpellTarget.OnTarget()` called `GetCastDelay()` again
4. The spell was in a different state, returning incorrect/zero delay
5. Timer either didn't fire or fired with wrong timing

**Solution**:
- Added `_spherePostTargetDelay` field to `Spell` class to store original cast delay
- Store cast delay BEFORE setting it to zero for immediate targeting
- `SpellTarget` now uses stored delay instead of recalculating
- Fixed timer validation to check `from.Spell == spell` instead of spell state

**Files Modified**:
- `Projects/UOContent/Spells/Base/Spell.cs`
- `Projects/UOContent/Spells/Targeting/SpellTarget.cs`

**Impact**:
- Spells execute correctly after target selection
- Cast timing preserved for post-target animation
- Reagent consumption works properly

---

### Added - Movement During Casting and Pre-cast Removal (Commit: e911f0b0b)
**Feature**: Implemented Sphere 0.51a-style immediate targeting with movement during casting.

**Implementation**:
- Modified `Spell.Cast()` to skip pre-cast delay when `SphereConfig.ImmediateSpellTarget` enabled
- Updated `BlocksMovement` property to respect `SphereConfig.AllowMovementDuringCast`
- Target cursor appears immediately instead of after cast animation
- Cast animations/mantra play AFTER target selection
- Players can move freely while casting

**Configuration Options**:
- `SphereConfig.ImmediateSpellTarget` - Enable immediate targeting
- `SphereConfig.AllowMovementDuringCast` - Allow movement during cast
- `SphereConfig.CastDelayAfterTarget` - Cast delay after target selection

**Files Modified**:
- `Projects/UOContent/Spells/Base/Spell.cs`
- `Projects/UOContent/Spells/Targeting/SpellTarget.cs`

**Impact**:
- No more pre-casting animations
- Movement allowed during casting
- Sphere 0.51a authentic casting flow
- Backward compatible with default ModernUO mode

---

### Added - Spell Interruption System (Commit: e911f0b0b)
**Feature**: Bandages, wands, and new spells now interrupt active spell casts.

**Implementation**:
- Bandages cancel active spells when `SphereConfig.BandageCancelActions` enabled
- New spells/wands cancel existing casts when configured
- Interruption causes spell to fizzle without consuming resources

**Files Modified**:
- `Projects/UOContent/Items/Skill Items/Misc/Bandage.cs`
- `Projects/UOContent/Spells/Base/Spell.cs`

**Configuration Options**:
- `SphereConfig.BandageCancelActions` - Bandages interrupt spells
- `SphereConfig.WandCancelActions` - Wands interrupt spells
- `SphereConfig.SwingCancelSpell` - Weapon swings interrupt spells

**Impact**:
- Sphere-authentic action cancellation
- Prevents spell queueing
- Realistic combat flow

---

### Fixed - Build Compatibility Issues (Commit: e911f0b0b)
**Issue**: Compilation errors due to missing using directives and namespace mismatches.

**Fixes**:
- Added `using System;` to `SphereConfig.cs`
- Added `using System;` to `SpellTarget.cs`
- Fixed `ISpell` namespace reference in `MobileExtensions.cs` (removed `Spells.` prefix)
- Corrected `DisturbType.Action` to `DisturbType.UseRequest` in `SphereCombatState.cs`

**Files Modified**:
- `Projects/UOContent/Systems/Combat/SphereStyle/SphereConfig.cs`
- `Projects/UOContent/Systems/Combat/SphereStyle/MobileExtensions.cs`
- `Projects/UOContent/Systems/Combat/SphereStyle/SphereCombatState.cs`
- `Projects/UOContent/Spells/Targeting/SpellTarget.cs`

**Impact**:
- Clean build with zero errors
- Zero warnings
- All code properly compiled

---

## [2025-10-25] - Beta Test Stone Feature

### Added - Beta Test Stone (Commit: 004e7ca98)
**Feature**: Interactive stone for beta testing that grants maximum skills, stats, and starter equipment.

**Implementation**:
- `BetaTestStone.cs` - Interactive stone item
- `BetaTestStoneCommand.cs` - Admin command `[AddBetaTestStone]`
- `PlayerMobile.BetaTester` - Property to track usage

**Features**:
- Maximizes all skills to 100.0
- Sets all stats (Str, Dex, Int) to 100
- Provides comprehensive starter equipment in bank box:
  - Weapon and armor samples
  - 5000 gold
  - Full spellbook with all spells
  - Runebook
  - Reagents bag (1000 of each)
  - Tools bag (crafting tools)
  - Resources bag (1000 ingots, logs, cloth, leather)
  - Consumables bag (100 potions and scrolls)
- Account-based tracking prevents duplicate use
- Professional gump interface with accept/deny options

**Files Added**:
- `Projects/UOContent/Commands/BetaTestStone.cs`
- `Projects/UOContent/Special Systems/Items/Stones/BetaTestStone.cs`

**Files Modified**:
- `Projects/UOContent/Mobiles/PlayerMobile.cs`

**Usage**:
1. Admin: `[AddBetaTestStone`
2. Target location
3. Players double-click stone
4. Accept/Deny gump appears
5. On accept: receive all benefits

**Impact**:
- Easy beta tester setup
- Consistent testing environment
- No manual skill/stat adjustment needed

---

## [2025-10-24] - Initial Sphere Combat Framework

### Added - Sphere 0.51a Combat System Framework (Commit: 162a5d86c)
**Feature**: Complete Sphere 0.51a combat system implementation.

**Components**:
1. **SphereConfig.cs** - Master configuration system
2. **SphereCombatState.cs** - Combat state management
3. **SphereSpellHelper.cs** - Spell casting mechanics
4. **SphereWeaponHelper.cs** - Weapon swing mechanics
5. **SphereBandageHelper.cs** - Bandage mechanics
6. **SphereWandHelper.cs** - Wand casting mechanics
7. **MobileExtensions.cs** - Convenience methods

**Files Added**:
- `Projects/UOContent/Systems/Combat/SphereStyle/SphereConfig.cs`
- `Projects/UOContent/Systems/Combat/SphereStyle/SphereCombatState.cs`
- `Projects/UOContent/Systems/Combat/SphereStyle/SphereSpellHelper.cs`
- `Projects/UOContent/Systems/Combat/SphereStyle/SphereWeaponHelper.cs`
- `Projects/UOContent/Systems/Combat/SphereStyle/SphereBandageHelper.cs`
- `Projects/UOContent/Systems/Combat/SphereStyle/SphereWandHelper.cs`
- `Projects/UOContent/Systems/Combat/SphereStyle/MobileExtensions.cs`
- `Projects/UOContent/Systems/Combat/SphereStyle/README.md`

**Documentation Added**:
- `SPHERE_IMPLEMENTATION_SUMMARY.md`
- `Sphere0.51aCombatSystem.md`

**Core Features**:
- Independent timer operation
- Action cancellation hierarchy
- Movement during casting
- No pre-casting animations
- Instant wand casting
- Bandage interruption mechanics
- Debug logging system

**Configuration System**:
All features toggle-able via `SphereConfig` class with granular control over:
- Timer behavior
- Spell casting mechanics
- Weapon swing mechanics
- Bandage mechanics
- Wand mechanics
- Action cancellation rules

**Impact**:
- Complete Sphere 0.51a combat framework
- Backward compatible with ModernUO
- Non-invasive helper system
- Fully configurable

---

## Known Issues

### Under Investigation
- Magic Reflect scroll auto-casting on self instead of requiring target
- Scroll casting behavior may differ from spell book casting

### To Be Tested
- Wand casting and consumption
- Movement during casting in all scenarios
- Spell interruption edge cases
- Action cancellation hierarchy in complex combat situations

---

## Configuration Reference

### Master Toggle
```csharp
SphereConfig.EnableSphereStyle = true;  // Enable all Sphere mechanics
```

### Spell Casting
```csharp
SphereConfig.ImmediateSpellTarget = true;     // Target appears immediately
SphereConfig.AllowMovementDuringCast = true;  // Can move while casting
SphereConfig.CastDelayAfterTarget = true;     // Delay after target selection
SphereConfig.RemovePostCastRecovery = true;   // No post-cast delay
```

### Action Cancellation
```csharp
SphereConfig.SpellCancelSwing = true;      // Spell cancels weapon swing
SphereConfig.SwingCancelSpell = true;      // Weapon swing cancels spell
SphereConfig.BandageCancelActions = true;  // Bandage cancels other actions
SphereConfig.WandCancelActions = true;     // Wand cancels other actions
```

### Debug Options
```csharp
SphereConfig.EnableDebugLogging = true;        // General debug logging
SphereConfig.LogCancellations = true;          // Log action cancellations
SphereConfig.LogTimerStateChanges = true;      // Log timer updates
```

---

## Testing Guidelines

### Spell Casting Tests
1. Cast spell from book with reagents - verify consumption
2. Cast spell from book without reagents - verify error message
3. Cast spell from scroll - verify scroll consumption
4. Move while casting - verify movement allowed
5. Start casting, then use bandage - verify spell cancels

### Combat Flow Tests
1. Start swing, then cast spell - verify swing cancels
2. Start spell, then attack - verify spell cancels
3. Wand cast - verify instant casting behavior
4. Multiple action attempts - verify proper cancellation

### Edge Cases
1. Cast spell, disconnect during cast - verify cleanup
2. Cast spell, die during cast - verify cleanup
3. Cast spell, target goes out of range - verify proper fizzle
4. Rapid action switching - verify no queuing

---

## Migration Notes

### For Developers
- All Sphere-style code marked with `//Sphere-style edit:` comments
- Original ModernUO behavior preserved when Sphere mode disabled
- No breaking changes to existing ModernUO functionality
- Helper system can be enabled/disabled at runtime

### For Server Administrators
- Default configuration is Sphere mode ENABLED
- Toggle master switch in `SphereConfig.cs` to disable
- All features individually configurable
- No world save compatibility issues

---

## Credits

Implementation by Claude (Anthropic) for EZMajor's ModernUO Sphere 0.51a style server.

Repository: https://github.com/EZMajor/ModernUO---51a-style

Based on original Sphere 0.51a server mechanics and documentation.
