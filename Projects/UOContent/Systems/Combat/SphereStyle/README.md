# Sphere 0.51a Combat System for ModernUO

**Implementation Status:** Framework Complete - Integration Required
**Repository:** https://github.com/EZMajor/ModernUO---51a-style
**Reference Documentation:** [Sphere0.51aCombatSystem.md](../../../../../Sphere0.51aCombatSystem.md)

---

## Overview

This system implements Sphere 0.51a-style combat mechanics for ModernUO, providing independent timer operation, action cancellation hierarchy, and combat flow that matches the classic Sphere server behavior.

### Key Features

- **Independent Timers**: NextSwingTime, NextSpellTime, NextBandageTime, and NextWandTime operate independently without shared recovery delays
- **Action Cancellation Hierarchy**: Sphere-style cancellation rules (cast cancels swing, swing cancels cast, etc.)
- **Movement Freedom**: Unrestricted movement during spellcasting
- **Immediate Damage**: Damage applies immediately upon hit confirmation
- **No Recovery Delays**: Optional removal of post-cast recovery delays
- **Configurable**: Master toggle and granular configuration options

---

## Architecture

### Core Components

#### 1. **SphereConfig.cs**
Centralized configuration system controlling all Sphere-style behaviors.

**Key Settings:**
```csharp
SphereConfig.EnableSphereStyle = true;           // Master toggle
SphereConfig.IndependentTimers = true;           // Independent timer operation
SphereConfig.SpellCancelSwing = true;            // Spell cast cancels swing
SphereConfig.SwingCancelSpell = true;            // Swing cancels spell
SphereConfig.AllowMovementDuringCast = true;     // No movement lock while casting
SphereConfig.RemovePostCastRecovery = true;      // No post-cast delay
```

#### 2. **SphereCombatState.cs**
Manages Sphere combat state for each mobile, tracking independent timers and action states.

**Properties:**
- `NextSwingTime`: Next allowed weapon swing
- `NextSpellTime`: Next allowed spell cast
- `NextBandageTime`: Next allowed bandage use
- `NextWandTime`: Next allowed wand use
- `IsCasting`, `IsInCastDelay`, `HasPendingSwing`, `IsBandaging`: State flags

#### 3. **MobileExtensions.cs**
Extension methods for Mobile class providing convenient access to Sphere combat state.

**Key Methods:**
```csharp
mobile.SphereCanSwing()              // Check if can swing
mobile.SphereCanCast()               // Check if can cast
mobile.SphereBeginSpellCast(spell)   // Begin cast (cancels swing)
mobile.SphereBeginSwing()            // Begin swing (cancels spell)
mobile.SphereCancelSpell(reason)     // Cancel active spell
mobile.SphereCancelSwing(reason)     // Cancel pending swing
```

#### 4. **Helper Classes**

##### **SphereWeaponHelper.cs**
- Swing validation and cancellation
- Independent swing timers
- Optional Sphere-style swing speed calculation
- Immediate damage application support

##### **SphereSpellHelper.cs**
- Cast validation and cancellation
- Movement permission handling
- Post-cast recovery removal
- Independent spell timers
- Optional damage-based fizzle configuration

##### **SphereBandageHelper.cs**
- Bandage validation and cancellation
- Independent bandage timers
- Swing/cast cancellation on bandage use

##### **SphereWandHelper.cs**
- Wand validation and cancellation
- Instant-cast behavior
- Independent wand timers
- Swing/cast cancellation on wand use

---

## Integration Guide

### Phase 1: Helper Integration (Non-Breaking)

The current implementation provides helper methods that can be **called from** ModernUO code without modifying core files. This is the safest initial approach.

#### Example: BaseWeapon.OnSwing Integration

**Option A: Non-invasive (Recommended for testing)**
```csharp
// In BaseWeapon.OnSwing, add Sphere checks:
public virtual TimeSpan OnSwing(Mobile attacker, Mobile defender, double damageBonus = 1.0)
{
    var canSwing = true;

    // ... existing validation ...

    // Sphere-style edit: Enhanced validation
    if (SphereConfig.IsEnabled())
    {
        canSwing = SphereWeaponHelper.ValidateSwing(attacker, defender, canSwing);
    }

    if (canSwing && attacker.HarmfulCheck(defender))
    {
        // Sphere-style edit: Track swing initiation
        if (SphereConfig.IsEnabled())
        {
            SphereWeaponHelper.OnSwingBegin(attacker);
        }

        attacker.DisruptiveAction();
        // ... existing swing logic ...

        if (CheckHit(attacker, defender))
        {
            OnHit(attacker, defender, damageBonus);
        }
        else
        {
            OnMiss(attacker, defender);
        }
    }

    var delay = GetDelay(attacker);

    // Sphere-style edit: Track swing completion
    if (SphereConfig.IsEnabled())
    {
        SphereWeaponHelper.OnSwingComplete(attacker, delay);
        delay = SphereWeaponHelper.GetWeaponDelay(this, attacker, delay);
    }

    return delay;
}
```

#### Example: Spell.Cast Integration

```csharp
// In Spell.Cast, add Sphere checks:
public bool Cast()
{
    StartCastTime = Core.TickCount;

    // ... existing validation ...

    if (Caster.Mana >= requiredMana)
    {
        if (Caster.Spell == null && Caster.CheckSpellCast(this) && CheckCast() &&
            Caster.Region.OnBeginSpellCast(Caster, this))
        {
            State = SpellState.Casting;
            Caster.Spell = this;

            // Sphere-style edit: Track spell cast initiation
            if (SphereConfig.IsEnabled())
            {
                SphereSpellHelper.OnCastBegin(Caster, this);
            }

            // ... existing casting logic ...
        }
    }

    return false;
}
```

#### Example: Spell.BlocksMovement Override

```csharp
// In Spell.cs, modify BlocksMovement property:
public virtual bool BlocksMovement
{
    get
    {
        var defaultBlocks = IsCasting;

        // Sphere-style edit: Allow movement during cast if configured
        if (SphereConfig.IsEnabled())
        {
            return SphereSpellHelper.CheckBlocksMovement(Caster, this, defaultBlocks);
        }

        return defaultBlocks;
    }
}
```

---

### Phase 2: Full Integration Points

Once Phase 1 is tested and working, these are the recommended full integration points:

#### BaseWeapon.cs
**File:** `Projects/UOContent/Items/Weapons/BaseWeapon.cs`

**Methods to modify:**
1. `OnSwing(Mobile attacker, Mobile defender, double damageBonus = 1.0)` - Lines 861-926
2. `GetDelay(Mobile from)` - Lines 1368-1501
3. `OnHit(Mobile attacker, Mobile defender, double damageBonus = 1.0)` - Damage application

**Integration Points:**
```
Line 867-878:  Add Sphere validation checks
Line 887:      Add Sphere swing begin tracking
Line 915-922:  Add Sphere damage application markers
Line 925:      Add Sphere swing complete tracking
Line 1368+:    Add optional Sphere swing speed calculation
```

#### Spell.cs
**File:** `Projects/UOContent/Spells/Base/Spell.cs`

**Methods to modify:**
1. `Cast()` - Lines 462-594
2. `BlocksMovement` property - Line 67
3. `Disturb(DisturbType type, ...)` - Lines 396-431
4. `GetCastRecovery()` - Lines 662-682
5. `CastTimer.OnTick()` - Lines 929-975

**Integration Points:**
```
Line 67:       Override BlocksMovement with Sphere check
Line 520-530:  Add Sphere cast begin tracking
Line 565-577:  Add Sphere cast delay entry
Line 418:      Add Sphere disturb handling
Line 662-682:  Add Sphere recovery delay override
```

#### Bandage.cs
**File:** `Projects/UOContent/Items/Skill Items/Misc/Bandage.cs`

**Methods to modify:**
1. `BandageContext` constructor
2. `BandageContext.OnTick()` - Lines 140-542
3. Bandage delay calculation - Lines 448-530

**Integration Points:**
```
Line 140+:     Add Sphere bandage begin tracking
Line 448-530:  Add Sphere bandage delay calculation
OnTick:        Add Sphere bandage complete tracking
```

#### BaseWand.cs
**File:** `Projects/UOContent/Items/Wands/BaseWand.cs`

**Methods to modify:**
1. `OnDoubleClick(Mobile from)` - Lines 98-121
2. `GetUseDelay` property - Line 68
3. `ApplyDelayTo(Mobile from)` - Lines 82-92

**Integration Points:**
```
Line 98-121:   Add Sphere wand begin tracking
Line 68:       Add Sphere instant cast check
Line 82-92:    Add Sphere wand timer tracking
```

---

## Configuration Reference

### Master Controls

| Setting | Default | Description |
|---------|---------|-------------|
| `EnableSphereStyle` | `true` | Master toggle for entire system |
| `EnableDebugLogging` | `false` | Detailed combat logging |
| `LogActionCancellations` | `false` | Log when actions cancel each other |
| `LogTimerStateChanges` | `false` | Log timer modifications |

### Timer Independence

| Setting | Default | Description |
|---------|---------|-------------|
| `IndependentTimers` | `true` | Timers operate independently |
| `RemoveGlobalRecovery` | `true` | No shared recovery delays |

### Action Cancellation

| Setting | Default | Description |
|---------|---------|-------------|
| `SpellCancelSwing` | `true` | Starting spell cancels swing |
| `SwingCancelSpell` | `true` | Starting swing cancels spell |
| `BandageCancelActions` | `true` | Bandage cancels swing/cast |
| `WandCancelActions` | `true` | Wand cancels swing/cast |
| `DisableSwingDuringCast` | `true` | Can't swing while casting |
| `DisableSwingDuringCastDelay` | `true` | Can't swing during cast delay |

### Spellcasting

| Setting | Default | Description |
|---------|---------|-------------|
| `AllowMovementDuringCast` | `true` | No movement lock while casting |
| `RemovePostCastRecovery` | `true` | No post-cast delay |
| `ImmediateSpellTarget` | `true` | Target cursor appears immediately |
| `TargetManaDeduction` | `true` | Mana deducted at target confirmation |
| `DamageBasedFizzle` | `false` | Damage can interrupt spells |
| `RestrictedFizzleTriggers` | `true` | Only defined actions cause fizzle |

### Weapon Swings

| Setting | Default | Description |
|---------|---------|-------------|
| `ImmediateDamageApplication` | `true` | Damage applies on hit confirmation |
| `ResetSwingOnInterrupt` | `true` | Swing timer resets on interrupt |
| `SphereSwingSpeedCalculation` | `false` | Use Sphere speed formula |
| `MinimumSwingSpeed` | `0.5` | Minimum swing speed (seconds) |
| `MaximumSwingSpeed` | `10.0` | Maximum swing speed (seconds) |

### Item/Skill Usage

| Setting | Default | Description |
|---------|---------|-------------|
| `IndependentBandageTimer` | `true` | Bandage has independent timer |
| `InstantWandCast` | `true` | Wands cast instantly |

---

## Testing Procedures

### 1. Timer Independence Test

**Objective:** Verify timers operate independently

**Steps:**
1. Enable debug logging: `SphereConfig.EnableDebugLogging = true`
2. Cast a spell
3. Immediately attempt to swing weapon (should be blocked during cast)
4. Wait for cast to complete
5. Verify you can swing immediately (no post-cast recovery blocking swing)
6. While swing timer is active, verify you can cast again

**Expected Result:** Spell timer and swing timer operate independently

### 2. Action Cancellation Test

**Objective:** Verify cancellation hierarchy

**Steps:**
1. Begin weapon swing
2. Start casting spell before swing completes
3. Verify swing is cancelled
4. Begin new swing
5. Verify spell is cancelled

**Expected Result:** Actions cancel according to Sphere rules

### 3. Movement During Cast Test

**Objective:** Verify movement is allowed while casting

**Steps:**
1. Begin casting a spell
2. Attempt to move
3. Verify movement is NOT blocked
4. Complete spell cast

**Expected Result:** Can move freely during spell cast

### 4. Bandage Cancellation Test

**Objective:** Verify bandage cancels other actions

**Steps:**
1. Begin weapon swing
2. Start spell cast
3. Apply bandage
4. Verify both swing and spell are cancelled

**Expected Result:** Bandage cancels active actions

### 5. Wand Test

**Objective:** Verify instant wand casting

**Steps:**
1. Use wand
2. Verify no cast delay (instant)
3. Verify spell timer is NOT affected
4. Verify wand has its own cooldown timer

**Expected Result:** Wand works independently with instant cast

---

## Debugging

### Enable Detailed Logging

```csharp
// In game or at server startup
SphereConfig.EnableDebugLogging = true;
SphereConfig.LogActionCancellations = true;
SphereConfig.LogTimerStateChanges = true;
```

### Common Log Messages

```
[Sphere-Combat] PlayerName - Swing initiated
[Sphere-Combat] PlayerName - Spell cast started
[Sphere-Combat] PlayerName - Weapon swing cancelled: Spell cast started
[Sphere-Combat] PlayerName - NextSwingTime: 0 -> 2500
[Sphere-Combat] PlayerName - Swing blocked: Currently casting
```

### Troubleshooting

**Problem:** Sphere mode not working
**Solution:** Verify `SphereConfig.EnableSphereStyle = true`

**Problem:** Actions still sharing recovery
**Solution:** Verify `SphereConfig.IndependentTimers = true`

**Problem:** Movement blocked during cast
**Solution:** Verify `SphereConfig.AllowMovementDuringCast = true` and integration point in `Spell.BlocksMovement`

**Problem:** Post-cast recovery still present
**Solution:** Verify `SphereConfig.RemovePostCastRecovery = true` and integration in `CastTimer.OnTick()`

---

## Migration from ModernUO Default

### Step 1: Install Sphere System

Copy all files from `Systems/Combat/SphereStyle/` to your ModernUO installation.

### Step 2: Configure

Edit `SphereConfig.cs` or set values at runtime:
```csharp
SphereConfig.EnableSphereStyle = true;
```

### Step 3: Integrate Helper Calls

Add helper method calls to existing ModernUO code (see Integration Guide).

### Step 4: Test

Run comprehensive tests (see Testing Procedures) to verify behavior.

### Step 5: Tune

Adjust configuration settings to match desired Sphere behavior.

---

## Reverting to ModernUO Default

### Temporary Disable

```csharp
SphereConfig.EnableSphereStyle = false;
```

### Complete Removal

1. Remove helper method calls from integrated code
2. Delete `Systems/Combat/SphereStyle/` directory
3. Rebuild project

---

## Performance Considerations

### Memory Usage

- Uses `ConditionalWeakTable` for Sphere state storage (minimal overhead)
- State objects created on-demand per mobile
- Automatic garbage collection when mobile is destroyed

### CPU Usage

- All checks are behind `SphereConfig.IsEnabled()` guard
- Minimal impact when disabled
- Debug logging adds overhead (disable in production)

---

## Compatibility

### Supported ModernUO Versions

- .NET 9
- C# 12
- ModernUO main branch (as of implementation date)

### Known Conflicts

None currently identified. The system is designed to be non-invasive and toggleable.

---

## Future Enhancements

Potential additions for future versions:

1. **Sphere-style swing speed formula** (currently optional)
2. **Advanced fizzle configuration** (damage thresholds, spell circles)
3. **Potion throwing mechanics** (if different in Sphere)
4. **Special move timing** (Bushido, Ninjitsu, Chivalry)
5. **Pet combat timing** (if different from player)
6. **Archery timing adjustments** (flight time, reload)

---

## Support and Contribution

**Repository:** https://github.com/EZMajor/ModernUO---51a-style
**Issues:** Create GitHub issues for bugs or feature requests
**Documentation:** See [Sphere0.51aCombatSystem.md](../../../../../Sphere0.51aCombatSystem.md) for specification

---

## License

This implementation follows the ModernUO license. See project root for details.

---

## Credits

**Specification:** Sphere 0.51a Combat System Documentation
**Framework:** ModernUO Team
**Repository Maintainer:** EZMajor

---

## Change Log

### Version 1.0.0 - Initial Implementation
- Core Sphere combat state management
- Independent timer system
- Action cancellation hierarchy
- Movement during casting
- Post-cast recovery removal
- Bandage and wand integration
- Helper classes for all major systems
- Comprehensive configuration system
- Debug logging support
