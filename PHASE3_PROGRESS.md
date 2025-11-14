# Phase 3 Progress: Core Spell Integration

**Date**: 2025-11-09
**Status**: IN PROGRESS
**Build**: SUCCESS (0 errors, 0 warnings)

---

## Objective

Integrate Sphere 51a spell timing and behaviors into the core spell system by adding hooks to Spell.cs and supporting infrastructure.

---

## Work Completed So Far

### 1. SpellTimingProvider.cs (COMPLETE)
**Location**: `Projects/UOContent/Modules/Sphere51a/Spells/SpellTimingProvider.cs`

**Purpose**: Loads and provides spell timing data from spell_timing.json

**Features**:
- Loads all 64 Magery spells from Distribution/Data/Sphere51a/spell_timing.json
- Provides timing lookups by spell ID or name
- Calculates skill-based cast time reduction (up to 25% at GM Magery)
- Formula: `baseCastTime - (baseCastTime * 0.25 * (magerySkill / 100.0))`
- Supports both book and scroll casting (scrolls = 50% of book time)
- Thread-safe dictionary lookups

**Key Methods**:
```csharp
public static int GetCastDelay(int spellId, double magerySkill, bool fromScroll)
public static int GetCastDelay(string spellName, double magerySkill, bool fromScroll)
public static int GetCastDelay(Spell spell, double magerySkill, bool fromScroll)
public static int GetBaseCastDelay(int circle, bool fromScroll)
```

**Status**: ✅ COMPLETE - Builds successfully

### 2. Sphere51aModule.cs Integration (COMPLETE)
**Location**: `Projects/UOContent/Modules/Sphere51a/Sphere51aModule.cs`

**Change**: Added SpellTimingProvider initialization at line 109

```csharp
// Initialize spell timing provider
Spells.SpellTimingProvider.Initialize();
```

**Status**: ✅ COMPLETE - Builds successfully

### 3. SphereCombatState.cs Verification (COMPLETE)
**Location**: `Projects/UOContent/Modules/Sphere51a/Combat/SphereCombatState.cs`

**Verified Existing Methods**:
- `CanCast()` - Checks if mobile can cast (NextSpellTime check)
- `BeginSpellCast(Spell spell)` - Marks spell casting started
- `EndSpellCast()` - Marks spell casting ended
- `SetNextSpellTime(TimeSpan delay)` - Sets next spell timer

**Status**: ✅ COMPLETE - All spell timer tracking methods already exist

---

## Work Remaining

### 4. Spell.cs Integration Hooks (PENDING)
**Location**: `Projects/UOContent/Spells/Base/Spell.cs` (977 lines - CORE FILE)

**Challenge**: This is a large, complex core file that requires careful modification to avoid breaking existing functionality.

**Required Integration Points** (from SPELL_ARCHITECTURE.md):

#### Hook Point 1: Cast() Method (Line ~462-580)
**Current Location**: After line 521 (`Caster.Spell = this;`)

**Required Addition**:
```csharp
// Sphere51a Integration: Raise OnSpellCastBegin event
if (Server.Modules.Sphere51a.Configuration.SphereConfiguration.Enabled)
{
    var state = Server.Modules.Sphere51a.Combat.SphereCombatState.GetOrCreate(Caster);
    if (state != null && !state.CanCast())
    {
        Caster.SendLocalizedMessage(502644); // You have not yet recovered.
        return false;
    }

    var args = new Server.Modules.Sphere51a.Events.SpellCastEventArgs
    {
        Caster = Caster,
        Spell = this
    };

    Server.Modules.Sphere51a.Events.SphereEvents.RaiseSpellCastBegin(args);

    if (args.Cancelled)
        return false;

    state?.BeginSpellCast(this);
}
```

#### Hook Point 2: OnCast() Method
**Required**: Raise OnSpellCast event when targeting completes

**Status**: NOT YET LOCATED - Need to find OnCast() method

####  Hook Point 3: FinishSequence() Method (Line 140)
**Current Code** (Line 140-150):
```csharp
public virtual void FinishSequence()
{
    State = SpellState.None;

    if (Caster.Spell == this)
    {
        Caster.Spell = null;
    }

    Caster.Delta(MobileDelta.Flags); // Remove paralyze
}
```

**Required Addition**: Raise OnSpellCastComplete event and handle no post-cast recovery

#### Hook Point 4: BlocksMovement Property (Line 67)
**Current Code**: `public virtual bool BlocksMovement => IsCasting;`

**Required Change**: Check Sphere51a AllowMovementDuringCast configuration

#### Hook Point 5: GetCastDelay() Method (Line 684)
**Current Code**: Complex FC/FCR calculation (lines 684-724)

**Required Change**: Return Sphere51a timing when enabled

**Example**:
```csharp
public virtual TimeSpan GetCastDelay()
{
    // Sphere51a Integration: Use Sphere timing if enabled
    if (Server.Modules.Sphere51a.Configuration.SphereConfiguration.Enabled &&
        Server.Modules.Sphere51a.Spells.SpellTimingProvider.IsInitialized)
    {
        var magerySkill = Caster.Skills[CastSkill].Value;
        var fromScroll = Scroll != null && !(Scroll is BaseWand);
        var sphereDelayMs = Server.Modules.Sphere51a.Spells.SpellTimingProvider.GetCastDelay(
            this, magerySkill, fromScroll
        );

        if (sphereDelayMs > 0)
        {
            return TimeSpan.FromMilliseconds(sphereDelayMs);
        }
    }

    // Standard ModernUO timing (existing code continues...)
    if (Scroll is BaseWand)
    {
        return Core.ML ? CastDelayBase : TimeSpan.Zero;
    }
    // ... rest of existing method
}
```

---

### 5. Spell Audit Logging (PENDING)
**Location**: `Projects/UOContent/Modules/Sphere51a/Combat/Audit/CombatAuditSystem.cs`

**Required**:
- Add spell cast audit logging methods
- Track cast times, mana usage, fizzles
- Detect double-casting
- Log to audit buffer

**Status**: NOT STARTED

---

### 6. Testing (PENDING)
**Location**: `Projects/UOContent/Modules/Sphere51a/Configuration/test-config.json`

**Required**:
- Enable spell_timing tests (currently disabled)
- Run integration verification
- Generate audit reports
- Validate timing accuracy

**Status**: BLOCKED - Requires Spell.cs integration first

---

## Challenges Identified

### 1. Spell.cs Complexity
- 977 lines of complex spell casting logic
- Core file affecting all spells in the game
- Risk of breaking existing functionality
- Requires extensive testing

### 2. Namespace Collisions
- `Server.Modules.Sphere51a.Core` vs `Server.Core`
- Must use `global::Server.Core.TickCount` syntax
- Already fixed in SpellTimingProvider.cs

### 3. Configuration Toggle
- All hooks must check `SphereConfiguration.Enabled`
- Must not affect behavior when Sphere51a is disabled
- Opt-in system must be preserved

---

## Recommended Next Steps

### Option A: Minimal Integration (Fastest)
1. Add only Hook Point 5 (GetCastDelay) to Spell.cs
2. This alone will give Sphere51a timing for spell casts
3. Test with spell timing tests
4. Add remaining hooks incrementally

### Option B: Full Integration (Complete)
1. Add all 5 hook points to Spell.cs in one pass
2. Add spell audit logging
3. Enable and run tests
4. Fix any issues found

### Option C: Partial Class Approach (Cleanest)
1. Create Spell.Sphere51a.cs as a partial class
2. Move Sphere integration logic to partial class
3. Keep Spell.cs changes minimal
4. Better separation of concerns

---

## Files Modified So Far

| File | Status | Lines | Purpose |
|------|--------|-------|---------|
| SpellTimingProvider.cs | NEW | 280+ | Spell timing data provider |
| Sphere51aModule.cs | MODIFIED | +2 | Initialize SpellTimingProvider |
| SphereCombatState.cs | VERIFIED | 0 | Existing spell timer methods confirmed |

**Build Status**: ✅ SUCCESS (0 errors, 0 warnings)

---

## Configuration Status

**Sphere51a Enabled**: ✅ YES
- Distribution/Configuration/modernuo.json: `"sphere51a.enabled": "True"`
- Distribution/Projects/UOContent/Modules/Sphere51a/Configuration/config.json: `"Enabled": true"`

**Spell Tests Disabled**: ✅ CORRECT (until integration complete)
- Projects/UOContent/Modules/Sphere51a/Configuration/test-config.json: `"spell_timing.enabled": false`

---

## Next Session Tasks

1. **Add GetCastDelay() hook to Spell.cs** (Hook Point 5)
   - Simplest starting point
   - Immediate benefit (Sphere timing)
   - Low risk

2. **Test the timing change**
   - Enable spell_timing tests
   - Run integration verification
   - Check if events are needed or if timing alone suffices

3. **Add remaining hooks if tests pass**
   - OnSpellCastBegin (Hook Point 1)
   - OnSpellCast (Hook Point 2)
   - OnSpellCastComplete (Hook Point 3)
   - BlocksMovement (Hook Point 4)

4. **Add spell audit logging**

5. **Full validation testing**

---

## Current Blocker

**Spell.cs integration requires careful planning due to**:
- File complexity (977 lines)
- Core system file (affects all spells)
- Multiple integration points needed
- Risk of breaking existing functionality

**Recommendation**: Start with Hook Point 5 (GetCastDelay) only as proof of concept.
