# Phase 2 Implementation Guide: Complete Spellcasting Integration

**Start Date:** 10/29/2025 1:18 PM  
**Planning Complete:** 10/29/2025 2:32 PM  
**Phase:** 2 of 5  
**Status:** READY FOR IMPLEMENTATION  
**Duration:** Estimated 3-4 days  
**Architecture Analysis:** COMPLETE

---

## Overview

Phase 2 implements Sphere 0.51a spellcasting mechanics with immediate target cursors, post-target cast delays, proper spell cancellation hierarchy, and restricted fizzle triggers.

The architecture analysis (10/29/2025 2:32 PM) confirms that most infrastructure is already in place. This guide documents the remaining implementation tasks required to complete Phase 2.

---

## Key Requirements from Sphere 0.51a

### Spell Casting Flow
1. **Immediate Target Cursor** - Cursor shows immediately on cast initiation (no pre-cast delay)
2. **Cast Delay** - Delay occurs AFTER target selection, not before
3. **Mantra & Animations** - Play during post-target cast delay phase
4. **Movement** - Allowed during targeting and post-target phases (no lock)
5. **No Post-Cast Recovery** - NextSpellTime set to zero after effect
6. **Mana Deduction** - At target confirmation (CheckSequence), not cast start

### Fizzle Rules (Restricted Triggers)
**Fizzle ON:**
- New spell cast (spell cancels active spell)
- Bandage use
- Wand use
- Paralyzed state
- Death

**Fizzle OFF (Movement Allowed):**
- Movement during any phase
- Damage taken
- Potion use
- Equipment changes
- Ranged/melee weapon use

### Spell Cancellation Hierarchy
1. **Spell Cast** → Cancels pending swing + active spell (shows fizzle)
2. **Weapon Swing** → Cancels active spell + pending swing
3. **Bandage Use** → Cancels both swing and spell
4. **Wand Use** → Cancels both, executes instantly
5. **Potion Use** → Cancels nothing

---

## Current Implementation Status

### Architecture Analysis Results (10/29/2025 2:32 PM)

#### SpellTarget.cs - [COMPLETE]
Status: All core infrastructure implemented and working

- [PASS] Immediate target cursor (no pre-cast delay) - Implemented
- [PASS] Post-target delay with animations and mantra - Implemented
- [PASS] Spell replacement logic with ReplacedSpell tracking - Implemented
- [PASS] Hand clearing and casting animations - Implemented during post-target delay
- [PASS] Timer.StartTimer for delayed spell effect execution - Implemented
- [PASS] OnTargetFinish handling for sequence completion - Implemented
- [PASS] Disturb logic for spell replacement - Implemented

**Finding:** SpellTarget.cs has the complete post-target cast delay mechanism implemented. No modifications needed for this file beyond testing and validation.

#### Spell.cs - [PARTIAL]
Status: Foundation in place, specific modifications needed

**Present:**
- [PASS] _spherePostTargetDelay field for storing post-target delay
- [PASS] _replacedSpell field for tracking replaced spells
- [PASS] _hasSelectedTarget flag for fizzle logic
- [PASS] Sphere-style conditionals in Cast() method
- [PASS] CheckSequence() method structure ready for mana deduction

**Needs Modification:**
- [ ] Remove mana check from Cast() when Sphere immediate targeting enabled
- [ ] Add mana deduction to CheckSequence() at target confirmation
- [ ] Modify Disturb() for restricted fizzle triggers
- [ ] Update OnCasterHurt() to respect restricted fizzle configuration

#### SphereSpellHelper.cs - [COMPLETE]
Status: All supporting methods present

- [PASS] CheckBlocksMovement() - Movement blocking logic
- [PASS] GetCastRecovery() - Recovery delay handling
- [PASS] OnCastBegin() - Cast initiation
- [PASS] ValidateCast() - Cast validation
- [PASS] OnSpellDisturb() - Disturbance handling
- [PASS] CheckDamageDisturb() - Damage fizzle logic

**Finding:** SphereSpellHelper has all necessary supporting methods. No new methods needed.

---

## Implementation Checklist

### 1. Mana Deduction Timing [PENDING]
**Status:** Architecture defined, implementation pending

- [ ] Modify Spell.Cast() method (lines around 641)
  - Remove mana check when `sphereImmediateTargetMode` is true
  - Keep mana check for non-Sphere mode
  - Ensure error messages remain for insufficient mana

- [ ] Modify Spell.CheckSequence() method
  - Add mana deduction at target confirmation
  - Keep existing reagent consumption logic
  - Handle mana failure with proper messaging
  - Ensure resource consumption on fizzle

**Files to Modify:**
- `Projects/UOContent/Spells/Base/Spell.cs` (Cast and CheckSequence methods)

**Testing:**
- Verify mana check happens at target confirmation
- Verify insufficient mana error message at confirmation
- Verify mana deducted only on successful cast
- Verify resources consumed on spell fizzle

### 2. Restricted Fizzle Triggers [PENDING]
**Status:** Architecture defined, implementation pending

- [ ] Modify Spell.Disturb() method
  - Add check for restricted fizzle triggers
  - Only allow fizzle on: DisturbType.NewCast, DisturbType.Bandage, DisturbType.Wand, DisturbType.Paralyzed, DisturbType.Kill
  - Skip fizzle animation and resource consumption for non-restricted triggers
  - Maintain backward compatibility with ModernUO mode

- [ ] Update Spell.OnCasterHurt() method
  - Check SphereConfig.RestrictedFizzleTriggers
  - Skip damage-based fizzle if restricted fizzle triggers enabled
  - Maintain existing protection spell logic

**Files to Modify:**
- `Projects/UOContent/Spells/Base/Spell.cs` (Disturb and OnCasterHurt methods)
- `Projects/UOContent/Systems/Combat/SphereStyle/SphereConfig.cs` (verify toggles)

**Testing:**
- Verify fizzle on spell cast (NewCast trigger)
- Verify NO fizzle on movement
- Verify NO fizzle on damage
- Verify NO fizzle on potion use
- Verify fizzle on bandage use
- Verify fizzle on wand use
- Verify fizzle on paralyzed/death states

### 3. Movement During Casting [PENDING]
**Status:** Architecture defined, implementation pending

- [ ] Verify BlocksMovement property
  - Returns false during targeting phase (immediate cursor mode)
  - Returns false during post-target delay (if configured)
  - Calls SphereSpellHelper.CheckBlocksMovement()
  - Respects SphereConfig.AllowMovementDuringCast

- [ ] Verify OnCasterMoving() method
  - Allows movement when BlocksMovement returns false
  - No fizzle on movement in either phase
  - Proper error messages only when movement blocked

**Files to Modify:**
- `Projects/UOContent/Spells/Base/Spell.cs` (OnCasterMoving validation)

**Testing:**
- Movement during targeting phase (should work)
- Movement during post-target delay (should work)
- No fizzle on movement
- No error messages when movement allowed

### 4. Post-Target Delay Mechanism [VERIFICATION]
**Status:** Architecture confirmed working, testing pending

- [ ] Verify SpellTarget.OnTarget() implementation
  - Calls SphereSpellHelper.OnCastBegin()
  - Triggers post-target delay timer
  - Executes spell effect after delay

- [ ] Verify CastTimer implementation
  - Starts post-target delay
  - Plays animations and mantra
  - Executes spell effect

- [ ] Verify casting animations
  - Animations play during post-target delay
  - Mantra says during post-target delay
  - Hand effects display correctly

**Files to Verify:**
- `Projects/UOContent/Spells/Base/Spell.cs` (CastTimer class)
- `Projects/UOContent/Spells/Targeting/SpellTarget.cs` (OnTarget method)

**Testing:**
- Immediate cursor on cast start
- Post-target delay with animations
- Mantra says during delay
- Spell effect after delay completes
- Various cast delays (instant, 0.5s, 1.0s, 1.5s)

### 5. Configuration & Testing [PENDING]
**Status:** Configuration defined, validation pending

- [ ] Verify SphereConfig toggles
  - ImmediateSpellTarget - Enable immediate cursor
  - CastDelayAfterTarget - Enable post-target delay
  - AllowMovementDuringCast - Allow movement
  - RemovePostCastRecovery - No post-cast recovery
  - RestrictedFizzleTriggers - Restrict fizzle actions
  - TargetManaDeduction - Deduct mana at target confirmation

- [ ] Run SphereTestHarness
  - All 10 tests should pass
  - Specific focus on Phase 2 tests
  - Test result verification

- [ ] Run SphereBenchmarks
  - Compare against Phase 1 baseline
  - Verify no performance regression
  - Document results

**Files to Modify:**
- `Projects/UOContent/Systems/Combat/SphereStyle/SphereConfig.cs` (verify all toggles)

**Testing:**
- All configuration toggles working correctly
- All 10 SphereTestHarness tests passing
- Performance baseline comparison successful
- No compilation errors or warnings

---

## Modified Code Sections

### Spell.cs Changes Required

#### 1. Cast() Method [IMPLEMENTATION PENDING]
**Current Behavior:**
- Mana deducted at cast start
- Animations/mantra play immediately
- Paralyze applied immediately
- NextSpellTime set immediately

**Required Changes:**
- Skip mana check when `sphereImmediateTargetMode` is true
- Skip animations/mantra (moved to post-target)
- Skip paralyze during targeting (only when post-target starts)
- Keep NextSpellTime setting logic
- Maintain error messages for insufficient mana (shown at confirmation)

#### 2. CheckSequence() Method [IMPLEMENTATION PENDING]
**Current Behavior:**
- Assumes mana already deducted
- Consumes reagents
- Triggers spell effect

**Required Changes:**
- Add mana deduction check at this point
- Keep reagent consumption
- Add mana failure handling
- Keep spell effect trigger
- Ensure resources consumed on failure

#### 3. Disturb() Method [IMPLEMENTATION PENDING]
**Current Behavior:**
- Shows fizzle on most disturbances
- Consumes resources on fizzle

**Required Changes:**
- Add DisturbType check for restricted fizzle triggers
- Only show fizzle on: NewCast, Bandage, Wand, Paralyzed, Kill
- Skip fizzle effects on: Movement, Hurt, EquipRequest, UseRequest
- Skip resource consumption for non-fizzle disturbances

#### 4. OnCasterHurt() Method [IMPLEMENTATION PENDING]
**Current Behavior:**
- Disturbs on damage

**Required Changes:**
- Check SphereConfig.RestrictedFizzleTriggers
- Skip disturbance if restricted fizzle triggers enabled
- Maintain existing protection spell logic

---

## Testing Scenarios

### Scenario 1: Basic Spell Cast [PENDING]
1. Player initiates spell cast
2. Cursor appears immediately
3. Player selects target
4. Mana/reagents consumed at confirmation
5. Cast delay begins
6. Animation plays
7. Spell effect applied

### Scenario 2: Spell Cancellation [PENDING]
1. Player casts Spell A
2. Before target selection, cast Spell B
3. Spell A cursor closes
4. Spell B cursor shows
5. Player selects target for Spell B
6. Spell A fizzles (resources consumed)
7. Spell B executes normally

### Scenario 3: Movement During Cast [PENDING]
1. Player initiates spell cast
2. Cursor appears
3. Player moves (should NOT fizzle)
4. Player selects target
5. Player moves during post-target delay (should NOT fizzle)
6. Spell effect applied

### Scenario 4: Damage During Cast [PENDING]
1. Player initiates spell cast
2. Player takes damage (should NOT fizzle)
3. Cast continues
4. Player selects target
5. Spell effect applied

### Scenario 5: Bandage Interrupt [PENDING]
1. Player initiates spell cast
2. Cursor appears
3. Player uses bandage
4. Spell fizzles (resources consumed)
5. Bandage begins healing

---

## Performance Considerations

- Post-target delay uses existing Timer infrastructure
- No new memory allocations in hot path
- Fizzle checks optimized with early returns
- Configuration toggles minimize runtime checks
- Compare against Phase 1 baseline via SphereBenchmarks

---

## Success Criteria

### Phase 2 Implementation Completeness:

- [x] Architecture analysis complete (10/29/2025 2:32 PM)
- [x] Implementation plan documented
- [x] Identified all required modifications
- [x] Created testing strategy
- [ ] Mana deduction timing implemented
- [ ] Restricted fizzle triggers implemented
- [ ] Movement validation complete
- [ ] Post-target delay tested
- [ ] All configuration toggles verified
- [ ] All 10 SphereTestHarness tests passing
- [ ] Performance baseline comparison complete
- [ ] All 7 projects build successfully
- [ ] Professional documentation updated
- [ ] Phase 2 completion documented
- [ ] Changes committed to git

---

## Implementation Order

**Recommended sequence for implementation:**

1. **Step 1:** Mana deduction timing (Spell.cs Cast/CheckSequence modifications)
2. **Step 2:** Restricted fizzle triggers (Spell.cs Disturb/OnCasterHurt modifications)
3. **Step 3:** Run SphereTestHarness tests
4. **Step 4:** Run SphereBenchmarks and compare baseline
5. **Step 5:** Document Phase 2 completion
6. **Step 6:** Commit changes to git

---

## References

- **Main Plan:** Claude.md - Phase 2 Section
- **Sphere Rules:** Sphere0.51aCombatSystem.md - Section 2.2
- **Spell Code:** Projects/UOContent/Spells/Base/Spell.cs
- **Target Code:** Projects/UOContent/Spells/Targeting/SpellTarget.cs
- **Helper Methods:** Projects/UOContent/Systems/Combat/SphereStyle/SphereSpellHelper.cs
- **Configuration:** Projects/UOContent/Systems/Combat/SphereStyle/SphereConfig.cs
- **Test Harness:** Projects/UOContent/Systems/Combat/SphereStyle/SphereTestHarness.cs
- **Benchmarks:** Projects/UOContent/Systems/Combat/SphereStyle/SphereBenchmarks.cs

---

## Status Summary

**Planning Phase:** COMPLETE (10/29/2025 2:32 PM)
- Architecture analyzed
- Implementation tasks identified
- Testing strategy defined
- Success criteria documented

**Implementation Phase:** PENDING
- Ready to begin implementation
- All prerequisites met
- Infrastructure in place
- Testing framework ready
