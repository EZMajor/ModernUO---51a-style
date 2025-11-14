# Sphere51a Spell System Architecture

**Status**: Design Document (Implementation Pending - Phase 3)
**Version**: 1.0
**Last Updated**: 2025-11-08

---

## Table of Contents

1. [Overview](#overview)
2. [Spell Casting Flow](#spell-casting-flow)
3. [Event System](#event-system)
4. [Timing Specifications](#timing-specifications)
5. [Configuration Behaviors](#configuration-behaviors)
6. [Integration Requirements](#integration-requirements)
7. [Spell Database Schema](#spell-database-schema)
8. [Differences from ModernUO](#differences-from-modernuo)
9. [Implementation Checklist](#implementation-checklist)

---

## Overview

The Sphere51a spell system implements authentic Sphere 0.51a-style magic mechanics with precise timing control, independent action timers, and configurable casting behaviors.

### Key Principles

1. **Server-Authoritative Timing** - All spell timing controlled by server (±25ms precision)
2. **Independent Timers** - Spell casting doesn't reset swing timer and vice versa
3. **Immediate Targeting** - Target cursor appears instantly (no pre-cast delay)
4. **Target-Based Mana** - Mana deducted when target confirmed (not at cast start)
5. **Movement During Cast** - Players can move while casting (configurable)
6. **No Post-Cast Recovery** - Instant re-cast capability (configurable)

### Current Status

- **Event System**: ✓ Defined in `SphereEvents.cs`
- **State Tracking**: ✓ Implemented in `SphereCombatState.cs`
- **Configuration**: ✓ Settings defined in `SphereConfiguration.cs`
- **Audit Logging**: ✓ Handlers ready in `CombatAuditSystem.cs`
- **Core Integration**: ✗ NOT IMPLEMENTED (Phase 3 requirement)
- **Timing Provider**: ✗ NOT IMPLEMENTED (Phase 3 requirement)
- **Configuration File**: ✗ NOT CREATED (Phase 2 requirement)

---

## Spell Casting Flow

### Complete Sequence Diagram

```
Player Action: Click spell icon or say words of power
         ↓
┌────────────────────────────────────────────────────────────┐
│ Phase 1: INITIATION (Immediate)                            │
├────────────────────────────────────────────────────────────┤
│ 1. Check if can cast (Sphere timing check)                │
│    - Not in spell cooldown (NextSpellTime)                │
│    - Not currently casting (IsCasting)                     │
│    - Not in post-target delay (IsInCastDelay)             │
│                                                            │
│ 2. Raise: OnSpellCastBegin                                │
│    - Updates SphereCombatState                            │
│    - Logs audit entry (if enabled)                        │
│    - Cancels swing if SpellCancelSwing=true              │
│                                                            │
│ 3. Show target cursor IMMEDIATELY                         │
│    (No pre-cast delay in Sphere51a)                       │
└────────────────────────────────────────────────────────────┘
         ↓
┌────────────────────────────────────────────────────────────┐
│ Phase 2: TARGETING (Player interaction)                   │
├────────────────────────────────────────────────────────────┤
│ Player selects target or clicks ground                    │
│    - Movement allowed if AllowMovementDuringCast=true    │
│    - Can be interrupted by damage/actions                 │
│                                                            │
│ IF TargetManaDeduction = true:                            │
│    - Check mana NOW (not at start)                        │
│    - Deduct mana from caster                              │
│    - If insufficient → Fizzle                             │
│                                                            │
│ IF TargetManaDeduction = false:                           │
│    - Mana already deducted at spell start                │
│    - Fizzle returns partial mana (PartialManaPercent)    │
└────────────────────────────────────────────────────────────┘
         ↓
┌────────────────────────────────────────────────────────────┐
│ Phase 3: CAST DELAY (Timed execution)                     │
├────────────────────────────────────────────────────────────┤
│ 1. Raise: OnSpellCast (when targeting confirmed)          │
│    - Logs cast initiation                                 │
│    - Records expected delay                               │
│                                                            │
│ 2. Play cast animation                                     │
│    - Character gestures                                    │
│    - Words of power displayed                             │
│                                                            │
│ 3. Wait for CastDelay (from spell_timing.json)            │
│    - Delay based on spell circle and caster skill         │
│    - Movement allowed if configured                       │
│    - Fizzle on damage if DamageBasedFizzle=true          │
│                                                            │
│ 4. Check for interruption                                 │
│    - Damage received                                       │
│    - Paralyzed/Frozen                                     │
│    - Movement (if restricted)                             │
└────────────────────────────────────────────────────────────┘
         ↓
┌────────────────────────────────────────────────────────────┐
│ Phase 4: EFFECT APPLICATION (Instant)                     │
├────────────────────────────────────────────────────────────┤
│ 1. Apply spell effect to target                           │
│    - Damage, healing, buff, etc.                          │
│    - Check resistances                                     │
│    - Calculate success/failure                             │
│                                                            │
│ 2. Raise: OnSpellCastComplete                             │
│    - Updates SphereCombatState                            │
│    - Logs audit entry with results                        │
│    - Records actual timing                                │
│                                                            │
│ 3. Visual/Audio feedback                                  │
│    - Spell effect graphics                                │
│    - Sound effects                                         │
└────────────────────────────────────────────────────────────┘
         ↓
┌────────────────────────────────────────────────────────────┐
│ Phase 5: RECOVERY (Post-cast)                             │
├────────────────────────────────────────────────────────────┤
│ IF RemovePostCastRecovery = false (ModernUO default):     │
│    - Apply GetCastRecovery() delay                        │
│    - Player cannot cast immediately                       │
│                                                            │
│ IF RemovePostCastRecovery = true (Sphere51a):             │
│    - NO recovery delay                                     │
│    - NextSpellTime NOT set                                │
│    - Can cast next spell IMMEDIATELY                      │
│                                                            │
│ Note: Spell timer is INDEPENDENT of swing timer           │
│       Casting does NOT reset weapon swing cooldown        │
└────────────────────────────────────────────────────────────┘
```

### Key Timing Points

| Event | ModernUO Default | Sphere51a Mode |
|-------|------------------|----------------|
| Target Cursor Shown | After pre-cast delay | **IMMEDIATE** |
| Mana Deduction | At cast start | **At target confirmation** |
| Cast Delay Start | After targeting | After targeting (same) |
| Movement During Cast | **BLOCKED** | **ALLOWED** |
| Post-Cast Recovery | Delay applied | **NONE** |
| Spell/Swing Independence | Shared timer | **INDEPENDENT** |

---

## Event System

### Event Definitions

Located in `SphereEvents.cs`:

```csharp
// Raised when spell casting begins (before targeting)
public static event EventHandler<SpellCastEventArgs> OnSpellCastBegin;

// Raised when spell cast is confirmed (target selected)
public static event EventHandler<SpellCastEventArgs> OnSpellCast;

// Raised when spell effect completes
public static event EventHandler<SpellCastEventArgs> OnSpellCastComplete;

// Determines if spell blocks movement (configuration-based)
public static event Func<Mobile, Spell, bool> OnSpellBlocksMovement;
```

### Event Arguments

```csharp
public class SpellCastEventArgs : EventArgs
{
    public Mobile Caster { get; set; }      // Who is casting
    public Spell Spell { get; set; }        // What spell
    public IEntity Target { get; set; }     // Target (if applicable)
    public bool Cancelled { get; set; }     // Can cancel cast
    public long Timestamp { get; set; }     // When event occurred
}
```

### Event Raising Points

These events must be raised from `Spell.cs`:

1. **OnSpellCastBegin**
   - Called in: `Cast()` method (before showing target)
   - Purpose: Initialize spell state, check cooldowns
   - Can cancel: Yes (set `args.Cancelled = true`)

2. **OnSpellCast**
   - Called in: `OnCast()` or target confirmation
   - Purpose: Log cast initiation, start timing
   - Can cancel: No (cast committed)

3. **OnSpellCastComplete**
   - Called in: `FinishSequence()` or completion
   - Purpose: Update state, log results, calculate variance
   - Can cancel: No (already complete)

---

## Timing Specifications

### Base Cast Times by Circle

| Circle | Base Delay (ms) | Skill-Based Reduction | Notes |
|--------|----------------|----------------------|-------|
| 1 | 500 | Yes | Clumsy, Magic Arrow, etc. |
| 2 | 750 | Yes | Agility, Cunning, Harm, etc. |
| 3 | 1000 | Yes | Bless, Fireball, Teleport, etc. |
| 4 | 1250 | Yes | Curse, Lightning, Recall, etc. |
| 5 | 1500 | Yes | Blade Spirits, Mind Blast, etc. |
| 6 | 1750 | Yes | Explosion, Invisibility, etc. |
| 7 | 2000 | Yes | Flamestrike, Gate Travel, etc. |
| 8 | 2250 | Yes | Earthquake, Resurrection, etc. |

### Skill-Based Delay Formula

```csharp
// From Sphere 0.51a mechanics
double skillFactor = caster.Skills.Magery.Value / 1000.0; // 0.0 to 1.0
double reduction = baseDelay * 0.25 * skillFactor;        // Up to 25% reduction
double actualDelay = baseDelay - reduction;

// Example: Circle 3 (Fireball) at 100.0 Magery
// baseDelay = 1000ms
// skillFactor = 100.0 / 1000.0 = 0.10
// reduction = 1000 * 0.25 * 0.10 = 25ms
// actualDelay = 1000 - 25 = 975ms
```

### Spell-Specific Timings

Some spells have unique timing:

| Spell | Base Time | Special Timing |
|-------|-----------|----------------|
| Magic Arrow | 500ms | Fastest offensive spell |
| Heal | 500ms | Scales with Healing skill |
| Greater Heal | 1250ms | Longer than circle 4 average |
| Resurrection | 2250ms | Cannot be reduced |
| Polymorph | 2000ms | Special animation |

### Scroll Casting

Scrolls use **REDUCED** cast times:

```csharp
// Scrolls cast 50% faster
double scrollDelay = baseDelay * 0.5;

// Example: Fireball scroll
// baseDelay = 1000ms
// scrollDelay = 500ms (same as circle 1 spell)
```

---

## Configuration Behaviors

### AllowMovementDuringCast

**Type**: `boolean`
**Default**: `true` (Sphere51a)
**ModernUO**: `false`

**Behavior**:
```csharp
if (SphereConfiguration.AllowMovementDuringCast)
{
    // Player can move while casting
    // Movement does NOT interrupt cast
    // Only damage/paralysis interrupts
}
else
{
    // Movement cancels spell (ModernUO default)
}
```

**Implementation Hook**: Override `Spell.BlocksMovement` property

---

### RemovePostCastRecovery

**Type**: `boolean`
**Default**: `true` (Sphere51a)
**ModernUO**: `false`

**Behavior**:
```csharp
if (SphereConfiguration.RemovePostCastRecovery)
{
    // Skip GetCastRecovery() delay
    // Do NOT set NextSpellTime
    // Can cast next spell immediately
}
else
{
    // Apply standard recovery delay (ModernUO default)
    var recovery = GetCastRecovery();
    NextSpellTime = Core.TickCount + recovery;
}
```

**Implementation Hook**: `FinishSequence()` method

---

### ImmediateSpellTarget

**Type**: `boolean`
**Default**: `true` (Sphere51a)
**ModernUO**: `false`

**Behavior**:
```csharp
if (SphereConfiguration.ImmediateSpellTarget)
{
    // Show target cursor IMMEDIATELY
    // No pre-cast delay
    Caster.Target = new SpellTarget(this);
}
else
{
    // Delay before showing cursor (ModernUO default)
    Timer.DelayCall(TimeSpan.FromSeconds(0.5), () => {
        Caster.Target = new SpellTarget(this);
    });
}
```

**Implementation Hook**: `Cast()` method

---

### TargetManaDeduction

**Type**: `boolean`
**Default**: `true` (Sphere51a)
**ModernUO**: `false`

**Behavior**:
```csharp
if (SphereConfiguration.TargetManaDeduction)
{
    // Check and deduct mana when target confirmed
    if (Caster.Mana < ScaleMana(GetMana()))
    {
        Caster.SendLocalizedMessage(502625); // Insufficient mana
        return false;
    }
    Caster.Mana -= ScaleMana(GetMana());
}
else
{
    // Deduct mana at cast start (ModernUO default)
    // Already done in Cast() method
}
```

**Implementation Hook**: Target confirmation / `OnCast()` method

---

### DamageBasedFizzle

**Type**: `boolean`
**Default**: `false` (Sphere51a - restricted)
**ModernUO**: `true`

**Behavior**:
```csharp
if (!SphereConfiguration.DamageBasedFizzle)
{
    // Damage does NOT cause fizzle
    // Only paralysis, freezing, or death interrupts
}
else
{
    // Damage can cause fizzle (ModernUO default)
    // Chance based on damage amount
}
```

**Implementation Hook**: `OnCasterHurt()` override

---

### RestrictedFizzleTriggers

**Type**: `boolean`
**Default**: `true` (Sphere51a)
**ModernUO**: `false`

**Behavior**:
```csharp
if (SphereConfiguration.RestrictedFizzleTriggers)
{
    // Only specific actions cause fizzle:
    // - Being paralyzed
    // - Being frozen
    // - Death
    // - Disconnection

    // Does NOT fizzle on:
    // - Movement (if allowed)
    // - Taking damage (unless DamageBasedFizzle=true)
    // - Weapon swings
}
```

---

### PartialManaPercent

**Type**: `int` (0-100)
**Default**: `50` (Sphere51a)
**ModernUO**: `0`

**Behavior**:
```csharp
if (spellFizzled)
{
    // Return partial mana on fizzle
    int manaUsed = ScaleMana(GetMana());
    int manaReturned = (int)(manaUsed * (SphereConfiguration.PartialManaPercent / 100.0));
    Caster.Mana += manaReturned;
}
```

**Implementation Hook**: Fizzle handling in `Spell.cs`

---

### IndependentTimers

**Type**: `boolean`
**Default**: `true` (Sphere51a)
**ModernUO**: `false`

**Behavior**:
```csharp
// Spell timer is separate from swing timer
// Casting a spell does NOT reset NextSwingTime
// Swinging a weapon does NOT reset NextSpellTime

// SphereCombatState tracks:
private long _nextSwingTime;  // Independent
private long _nextSpellTime;  // Independent
private long _nextBandageTime; // Independent
private long _nextWandTime;    // Independent
```

---

## Integration Requirements

### Files Requiring Modification

#### 1. Spell.cs (Primary Integration)

**Location**: `Projects/UOContent/Spells/Base/Spell.cs`

**Required Changes**:

```csharp
using Server.Modules.Sphere51a.Events;
using Server.Modules.Sphere51a.Configuration;

public abstract class Spell : ISpell
{
    public virtual bool Cast()
    {
        // SPHERE INTEGRATION POINT 1: Check if can cast
        if (SphereConfiguration.Enabled)
        {
            var state = SphereCombatState.Get(Caster);
            if (state != null && !state.CanCast())
            {
                Caster.SendLocalizedMessage(502644); // You have not yet recovered
                return false;
            }
        }

        // Existing ModernUO checks...
        if (!CheckSequence())
            return false;

        // SPHERE INTEGRATION POINT 2: Raise OnSpellCastBegin
        if (SphereConfiguration.Enabled)
        {
            var args = new SpellCastEventArgs
            {
                Caster = Caster,
                Spell = this,
                Timestamp = Core.TickCount
            };

            SphereEvents.RaiseSpellCastBegin(args);

            if (args.Cancelled)
            {
                Caster.SendMessage("Your spell was cancelled.");
                return false;
            }

            // Update state
            var state = SphereCombatState.Get(Caster);
            state?.BeginSpellCast(this);
        }

        // Show target cursor
        if (SphereConfiguration.Enabled && SphereConfiguration.ImmediateSpellTarget)
        {
            // Immediate targeting (Sphere style)
            Caster.Target = new SpellTarget<IEntity>(this, TargetFlags);
        }
        else
        {
            // Standard ModernUO targeting
            // ... existing code ...
        }

        return true;
    }

    protected virtual void OnCast()
    {
        // SPHERE INTEGRATION POINT 3: Raise OnSpellCast
        if (SphereConfiguration.Enabled)
        {
            var args = new SpellCastEventArgs
            {
                Caster = Caster,
                Spell = this,
                Target = Target,
                Timestamp = Core.TickCount
            };

            SphereEvents.RaiseSpellCast(args);
        }

        // SPHERE INTEGRATION POINT 4: Target-based mana deduction
        if (SphereConfiguration.Enabled && SphereConfiguration.TargetManaDeduction)
        {
            int mana = ScaleMana(GetMana());
            if (Caster.Mana < mana)
            {
                Caster.SendLocalizedMessage(502625); // Insufficient mana
                DoFizzle();
                return;
            }
            Caster.Mana -= mana;
        }

        // Existing spell logic...
    }

    public virtual void FinishSequence()
    {
        // SPHERE INTEGRATION POINT 5: Raise OnSpellCastComplete
        if (SphereConfiguration.Enabled)
        {
            var args = new SpellCastEventArgs
            {
                Caster = Caster,
                Spell = this,
                Target = Target,
                Timestamp = Core.TickCount
            };

            SphereEvents.RaiseSpellCastComplete(args);

            // Update state
            var state = SphereCombatState.Get(Caster);
            state?.EndSpellCast();

            // SPHERE: No post-cast recovery if configured
            if (!SphereConfiguration.RemovePostCastRecovery)
            {
                var recovery = GetCastRecovery();
                state?.SetNextSpellTime(TimeSpan.FromMilliseconds(recovery));
            }
        }

        // Existing ModernUO logic...
    }

    public virtual bool BlocksMovement
    {
        get
        {
            if (SphereConfiguration.Enabled)
            {
                return !SphereConfiguration.AllowMovementDuringCast;
            }
            return true; // ModernUO default
        }
    }
}
```

**Total Changes**: ~60 lines of integration code across 4-5 methods

---

#### 2. SpellTimingProvider.cs (NEW FILE)

**Location**: `Projects/UOContent/Modules/Sphere51a/Combat/SpellTimingProvider.cs`

**Purpose**: Centralized spell timing calculations

```csharp
public static class SpellTimingProvider
{
    private static Dictionary<int, SpellTimingData> _spellTimings;

    public static void Initialize()
    {
        LoadSpellTimings();
    }

    private static void LoadSpellTimings()
    {
        var path = Path.Combine(Core.BaseDirectory, "Data/Sphere51a/spell_timing.json");
        // Load JSON configuration
        _spellTimings = JsonSerializer.Deserialize<Dictionary<int, SpellTimingData>>(json);
    }

    public static double GetCastDelay(Spell spell, Mobile caster)
    {
        var timing = GetSpellTiming(spell.SpellId);
        if (timing == null)
            return GetDefaultDelay(spell.Circle);

        double baseDelay = timing.CastDelayMs;

        // Apply skill reduction
        if (timing.AllowsSkillReduction)
        {
            double skillFactor = caster.Skills.Magery.Value / 1000.0;
            double reduction = baseDelay * 0.25 * skillFactor;
            baseDelay -= reduction;
        }

        return baseDelay;
    }

    public static int GetManaCost(Spell spell)
    {
        var timing = GetSpellTiming(spell.SpellId);
        return timing?.ManaCost ?? spell.GetMana();
    }

    private static SpellTimingData GetSpellTiming(int spellId)
    {
        return _spellTimings.TryGetValue(spellId, out var timing) ? timing : null;
    }
}
```

---

## Spell Database Schema

### File: spell_timing.json

**Location**: `Data/Sphere51a/spell_timing.json`

**Structure**:

```json
{
  "spells": [
    {
      "spellId": 1,
      "name": "Clumsy",
      "circle": 1,
      "manaCost": 4,
      "castDelayMs": 500,
      "minSkill": 0.0,
      "maxSkill": 100.0,
      "allowsSkillReduction": true,
      "allowsMovement": true,
      "postCastRecoveryMs": 0,
      "requiresTarget": true,
      "targetType": "Mobile",
      "reagents": ["Bloodmoss", "Nightshade"],
      "scrollCastDelayMs": 250,
      "damageType": null,
      "notes": "Lowers target's dexterity"
    },
    {
      "spellId": 4,
      "name": "Heal",
      "circle": 1,
      "manaCost": 4,
      "castDelayMs": 500,
      "minSkill": 0.0,
      "maxSkill": 100.0,
      "allowsSkillReduction": true,
      "allowsMovement": true,
      "postCastRecoveryMs": 0,
      "requiresTarget": true,
      "targetType": "Mobile",
      "reagents": ["Garlic", "Ginseng", "SpidersSilk"],
      "scrollCastDelayMs": 250,
      "damageType": null,
      "scalesWithHealing": true,
      "notes": "Healing amount scales with Healing skill"
    }
  ]
}
```

### Schema Definition

```typescript
interface SpellTimingData {
  spellId: number;              // Unique spell identifier
  name: string;                 // Spell name
  circle: number;               // Magic circle (1-8)
  manaCost: number;             // Base mana cost
  castDelayMs: number;          // Base cast delay in milliseconds
  minSkill: number;             // Minimum skill required (0.0-120.0)
  maxSkill: number;             // Skill for maximum effect (0.0-120.0)
  allowsSkillReduction: boolean;// Can reduce cast time with skill
  allowsMovement: boolean;      // Can move during cast (usually true)
  postCastRecoveryMs: number;   // Recovery delay (usually 0 for Sphere)
  requiresTarget: boolean;      // Needs target selection
  targetType: string;           // "Mobile", "Item", "Location", "None"
  reagents: string[];           // Required reagents
  scrollCastDelayMs: number;    // Scroll cast time (usually 50% of base)
  damageType: string | null;    // "Fire", "Cold", "Poison", "Energy", null
  notes: string;                // Implementation notes
}
```

---

## Differences from ModernUO

### Summary Table

| Feature | ModernUO Default | Sphere51a Mode | Impact |
|---------|-----------------|----------------|---------|
| **Target Cursor** | Delayed (after pre-cast) | **Immediate** | Faster spell initiation |
| **Mana Deduction** | At cast start | **At target selection** | Can conserve mana |
| **Movement** | Blocks casting | **Allows movement** | More mobile combat |
| **Post-Cast Recovery** | Delay applied | **None** | Faster spell chains |
| **Timer Independence** | Shared with melee | **Independent** | Can cast while swinging |
| **Fizzle Triggers** | Damage + movement | **Restricted (paralysis only)** | More reliable casting |
| **Partial Mana** | None on fizzle | **50% returned** | Less punishing |

### Behavioral Changes

#### Spell Chaining
```
ModernUO:
  Cast Fireball → 1.0s delay → 0.5s recovery → Wait → Cast Lightning
  Total: ~2.75s between spells

Sphere51a:
  Cast Fireball → 1.0s delay → Cast Lightning (immediate)
  Total: ~2.0s between spells (27% faster)
```

#### Mana Management
```
ModernUO:
  Click spell → Mana deducted → Select target → Fizzle → Mana LOST

Sphere51a:
  Click spell → Select target → Mana deducted → Fizzle → 50% mana RETURNED
  Advantage: Can cancel before mana loss
```

#### Combat Multitasking
```
ModernUO:
  Swing → Cast spell → Swing timer RESET → Wait for both

Sphere51a:
  Swing → Cast spell → Independent timers → Both continue
  Advantage: Can deal damage while casting
```

---

## Implementation Checklist

### Phase 3: Core Integration

- [ ] Add Sphere event hooks to `Spell.cs` (5 integration points)
- [ ] Create `SpellTimingProvider.cs` for timing calculations
- [ ] Create `spell_timing.json` with all 64 standard spells
- [ ] Implement `ImmediateSpellTarget` behavior
- [ ] Implement `TargetManaDeduction` behavior
- [ ] Implement `AllowMovementDuringCast` behavior
- [ ] Implement `RemovePostCastRecovery` behavior
- [ ] Implement `DamageBasedFizzle` behavior
- [ ] Implement `PartialManaPercent` on fizzle
- [ ] Implement independent timer tracking
- [ ] Add spell event handlers to `SphereCombatSystem.cs`
- [ ] Enable spell audit logging in `CombatAuditSystem.cs`
- [ ] Update `SphereCombatState.cs` spell timer methods

### Phase 4: Testing

- [ ] Enable spell timing tests in `test-config.json`
- [ ] Verify integration with `IntegrationVerifier`
- [ ] Run spell timing tests for all circles
- [ ] Validate timing accuracy (within ±25ms)
- [ ] Test spell/swing timer independence
- [ ] Test configuration behaviors (movement, mana, etc.)
- [ ] Generate audit reports proving accuracy
- [ ] Performance testing (500+ concurrent casters)

### Phase 5: Advanced Features

- [ ] Implement scroll-specific timing
- [ ] Add spell fizzle system improvements
- [ ] Add special spell mechanics (polymorph, etc.)
- [ ] Implement spell queuing (if desired)
- [ ] Add spell cooldown UI feedback

---

## Notes for Implementers

### Critical Points

1. **Event Timing**: Events must be raised at EXACT points in spell flow
2. **State Management**: `SphereCombatState` must track all spell timers
3. **Configuration Guards**: Always check `if (SphereConfiguration.Enabled)`
4. **Backwards Compatibility**: ModernUO behavior must work when Sphere disabled
5. **Null Safety**: Check for null Mobile, Spell, Target in all handlers

### Testing Strategy

1. **Unit Tests**: Individual configuration behaviors
2. **Integration Tests**: Event raising verification
3. **Timing Tests**: ±25ms accuracy validation
4. **Stress Tests**: 500+ concurrent casters
5. **Regression Tests**: Ensure ModernUO mode still works

### Common Pitfalls

- Forgetting to raise events in fizzle scenarios
- Not handling null targets in event args
- Raising events when Sphere disabled (wasted cycles)
- Incorrect timer calculations (use Core.TickCount not DateTime)
- Not cleaning up event subscriptions (memory leaks)

---

## Conclusion

This architecture document defines the complete Sphere51a spell system implementation. When Phase 3 begins, developers should:

1. Follow the integration points exactly as specified
2. Use the provided code templates as starting points
3. Test each configuration behavior independently
4. Validate event timing with active tests (not passive logs)
5. Generate audit reports to prove accuracy

The spell system is designed to be:
- **Modular**: Can be enabled/disabled via configuration
- **Performant**: Minimal overhead when disabled
- **Testable**: Active integration verification
- **Maintainable**: Clear separation of concerns
- **Authentic**: Matches Sphere 0.51a mechanics

**Status**: Design Complete ✓
**Ready for**: Phase 3 Implementation
