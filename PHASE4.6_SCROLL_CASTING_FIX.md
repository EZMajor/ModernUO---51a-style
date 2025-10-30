# Phase 4.6: Scroll Casting Fix - Completion Report

**Duration:** ~2 hours
**Completed:** 10/30/2025 8:00 PM
**Status:** [COMPLETE]

---

## Issue Summary

After Phase 4.5 fixed the double fizzle bug for spellbook casting, a critical issue remained where casting Spell B from a SCROLL while Spell A was in post-target cast delay caused Spell A to fizzle immediately when Spell B's cursor appeared (before any target was selected).

### Test Results Before Fix:

| Spell A Source | Spell B Source | Result |
|----------------|----------------|--------|
| Spellbook | Spellbook | [PASS] Works correctly |
| Scroll | Spellbook | [PASS] Works correctly |
| Spellbook | Scroll | [FAIL] Spell A fizzles |
| Scroll | Scroll | [FAIL] Spell A fizzles |

**Pattern:** Issue only occurred when Spell B was cast from a SCROLL

---

## Root Cause Analysis

### Investigation Process

1. Added debug output to identify where `Caster.Spell` was being cleared
2. User testing revealed `Caster.Spell` became null when Lightning (scroll) was cast
3. Debug output showed `Disturb(DisturbType.UseRequest)` was being called on Flame Strike
4. Traced call stack: Scroll double-click → Mobile.Use() → OnCasterUsingObject() → Disturb()

### The Problem

When a player double-clicks a scroll while another spell is in post-target cast delay:

```
1. Mobile.Use(scroll) is called
2. This triggers OnCasterUsingObject() on the active spell (Spell A)
3. OnCasterUsingObject() calls Disturb(DisturbType.UseRequest)
4. Disturb() clears Caster.Spell = null
5. Spell A's CheckSequence() fails because Caster.Spell != this (null != SpellA)
6. Spell A fizzles incorrectly
```

The issue is that `OnCasterUsingObject()` didn't account for Sphere immediate target mode where spells in post-target cast delay should NOT be disturbed by using items (scrolls, wands, potions, etc.).

---

## Solution Implementation

### File Modified

**Projects/UOContent/Spells/Base/Spell.cs** (Lines 182-202)

### Changes Made

Modified `OnCasterUsingObject()` method to check if Sphere immediate target mode is enabled and if the spell is in post-target cast delay:

```csharp
public virtual bool OnCasterUsingObject(IEntity entity)
{
    //Sphere-style edit: In Sphere immediate target mode, don't disturb spells in post-target cast delay
    // This allows the spell to complete even when using items (like scrolls) afterward
    var sphereImmediateTargetMode = Systems.Combat.SphereStyle.SphereConfig.IsEnabled() &&
                                   Systems.Combat.SphereStyle.SphereConfig.ImmediateSpellTarget;

    if (sphereImmediateTargetMode && Caster.SphereIsInCastDelay())
    {
        // Spell is in post-target cast delay - allow it to complete
        return true;
    }

    if (State == SpellState.Sequencing)
    {
        Disturb(DisturbType.UseRequest);
    }

    return true;
}
```

### How It Works

1. **Checks if Sphere immediate target mode is enabled**
2. **Checks if caster is in post-target cast delay** using `SphereIsInCastDelay()`
3. **If yes, returns immediately WITHOUT calling Disturb()**
4. **Allows spell to complete** even when player uses scrolls, wands, or potions

---

## Test Results After Fix

### User Verification

All test combinations now work correctly:

| Spell A Source | Spell B Source | Result |
|----------------|----------------|--------|
| Spellbook | Spellbook | [PASS] Works correctly |
| Scroll | Spellbook | [PASS] Works correctly |
| Spellbook | Scroll | [PASS] Fixed - now works |
| Scroll | Scroll | [PASS] Fixed - now works |

### Debug Output Verification

Test scenario: Flame Strike (spellbook) → Lightning (scroll), no target selected

```
1. Flame Strike cast and target selected
2. OnCasterUsingObject: NOT disturbing Flame Strike (in post-target cast delay) ← Fix working!
3. Lightning (scroll) shows cursor
4. Caster.Spell still correctly set to FlameStrikeSpell
5. FinishSequence clears Caster.Spell normally
6. Flame Strike completes and hits target successfully
```

---

## Sphere 0.51a Behavior Achieved

In Sphere 0.51a combat mechanics:

1. **Spell casting doesn't lock movement** - players can move while targeting
2. **Post-target cast delay is a "safe zone"** - spell will complete unless:
   - Another spell's target is SELECTED (not just cursor shown)
   - Caster dies
   - Specific allowed interruption triggers
3. **Using items during cast delay doesn't disturb** - scrolls, wands, potions can be used
4. **Independent action queuing** - new spells queue without canceling active spell until target is selected

This fix ensures scroll casting behaves identically to spellbook casting in Sphere immediate target mode.

---

## Compilation Status

- [PASS] All 7 projects compiled successfully
- [PASS] 0 errors, 0 warnings
- [PASS] Build time: 14.64 seconds
- [PASS] All debug output removed for production

---

## Debug Output Cleanup

All debug messages added during troubleshooting have been removed:

- [x] Removed from OnCasterUsingObject()
- [x] Removed from FinishSequence()
- [x] Removed from Disturb()
- [x] Removed from CheckSequence()
- [x] Removed from Cast() (Spell.cs)
- [x] Removed from OnTarget() (SpellTarget.cs)

---

## Impact Analysis

### What Changed

- **Single method modified:** OnCasterUsingObject() in Spell.cs
- **Behavior change:** Spells in post-target cast delay no longer disturbed by using items
- **Scope:** Only affects Sphere immediate target mode (controlled by config)
- **Backward compatibility:** ModernUO default behavior unchanged

### What Didn't Change

- No changes to spell queueing logic
- No changes to target selection logic
- No changes to disturbance system (Disturb method)
- No changes to spell state management
- No changes to resource consumption

---

## Related Issues Fixed

This fix completes the Sphere 0.51a spell casting implementation:

- **Phase 4.5:** Fixed double fizzle for spellbook casting
- **Phase 4.6:** Fixed scroll casting disturbance issue (this phase)

Both issues stemmed from the same root cause: improper handling of Sphere immediate target mode's post-target cast delay state.

---

## Success Criteria

- [x] All four test combinations work correctly
- [x] Scroll casting matches spellbook casting behavior
- [x] Spell A completes when Spell B cursor appears
- [x] Spell A only fizzles when Spell B target is SELECTED
- [x] User verification confirms fix working
- [x] All debug output removed
- [x] Project compiles successfully
- [x] Professional documentation complete

---

## Next Steps

With Phase 4.6 complete, scroll casting now works identically to spellbook casting in Sphere immediate target mode. The spell queueing system is fully functional and matches Sphere 0.51a behavior.

### Ready for Phase 4: Performance Optimization

Now that all core mechanics are working correctly:
- Timer independence (Phase 1)
- Spell integration (Phase 2)
- Action hierarchy (Phase 3)
- Double fizzle fix (Phase 4.5)
- Scroll casting fix (Phase 4.6)

We can proceed to Phase 4 performance optimization with confidence that the core behavior is correct.

---

## Files Modified Summary

```
Projects/UOContent/Spells/Base/Spell.cs
├── OnCasterUsingObject() - Added Sphere cast delay check
├── FinishSequence() - Debug output removed
├── Disturb() - Debug output removed
├── CheckSequence() - Debug output removed
└── Cast() - Debug output removed

Projects/UOContent/Spells/Targeting/SpellTarget.cs
└── OnTarget() - Debug output removed
```

---

## Conclusion

Phase 4.6 successfully resolves the scroll casting disturbance issue by preventing `OnCasterUsingObject()` from disturbing spells in post-target cast delay when Sphere immediate target mode is enabled. The fix is minimal, focused, and maintains backward compatibility with ModernUO default behavior.

All spell casting scenarios now work correctly, matching Sphere 0.51a combat mechanics.

**Status:** [COMPLETE]
**Verified:** User testing confirms all scenarios working
**Ready:** Phase 4 Performance Optimization can begin

---

## Last Updated

**Date:** 10/30/2025 8:00 PM
**Phase:** 4.6 Complete
**Next Phase:** Phase 4 Performance Optimization
