# Phase 2 Implementation Guide: Complete Spellcasting Integration

**Start Date:** 10/29/2025 1:18 PM  
**Phase:** 2 of 5  
**Status:** IN PROGRESS  
**Duration:** Estimated 3-4 days

---

## Overview

Phase 2 implements Sphere 0.51a spellcasting mechanics with immediate target cursors, post-target cast delays, proper spell cancellation hierarchy, and restricted fizzle triggers.

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

## Implementation Checklist

### 1. Post-Target Cast Delay Mechanism
- [ ] Create new timer for post-target delay phase
- [ ] Move spell animations to post-target phase
- [ ] Move mantra to post-target phase
- [ ] Update paralyze/freeze handling
- [ ] Ensure movement allowed during delay

**Files to Modify:**
- `Projects/UOContent/Spells/Base/Spell.cs` (CastTimer class)
- `Projects/UOContent/Systems/Combat/SphereStyle/SphereSpellHelper.cs`

### 2. Mana Deduction Timing
- [ ] Remove mana deduction from Cast() method
- [ ] Move mana deduction to CheckSequence() method
- [ ] Keep reagent consumption at CheckSequence()
- [ ] Add mana deduction failure handling

**Files to Modify:**
- `Projects/UOContent/Spells/Base/Spell.cs` (Cast and CheckSequence methods)

### 3. Spell Replacement Logic
- [ ] Complete _replacedSpell tracking in Cast()
- [ ] Implement fizzle on replaced spell at target confirmation
- [ ] Ensure previous spell resources consumed on fizzle
- [ ] Handle multiple spell replacement scenarios

**Files to Modify:**
- `Projects/UOContent/Spells/Base/Spell.cs` (Cast and CheckSequence methods)

### 4. Movement During Casting
- [ ] Verify BlocksMovement returns false when configured
- [ ] Ensure paralyze not applied during targeting
- [ ] Test movement in targeting phase
- [ ] Test movement in post-target phase

**Files to Modify:**
- `Projects/UOContent/Spells/Base/Spell.cs` (Cast and OnCasterMoving)

### 5. Restricted Fizzle Triggers
- [ ] Verify fizzle only on specified triggers
- [ ] Disable damage-based fizzle
- [ ] Disable equipment-based fizzle
- [ ] Disable movement-based fizzle
- [ ] Test all trigger combinations

**Files to Modify:**
- `Projects/UOContent/Spells/Base/Spell.cs` (OnCasterHurt, OnCasterEquipping)
- `Projects/UOContent/Systems/Combat/SphereStyle/SphereSpellHelper.cs`

### 6. Configuration & Testing
- [ ] Verify all SphereConfig toggles work
- [ ] Run SphereTestHarness with Phase 2 tests
- [ ] Run SphereBenchmarks for performance baseline
- [ ] Document any issues or edge cases

---

## Modified Code Sections

### Spell.cs Changes Required

#### 1. Cast() Method
**Current Behavior:**
- Mana deducted at cast start
- Animations/mantra play immediately
- Paralyze applied immediately
- NextSpellTime set immediately

**New Behavior:**
- Mana NOT deducted at cast start
- Animations/mantra delayed to post-target
- Paralyze NOT applied during targeting
- NextSpellTime set AFTER post-target delay

#### 2. CastTimer Inner Class
**Current Behavior:**
- Single timer for pre-cast delay
- Moves directly from Casting to Sequencing

**New Behavior:**
- CastTimer remains for post-target delay
- Sequencing happens immediately on target selection
- New state tracking for different phases

#### 3. CheckSequence() Method
**Current Behavior:**
- Mana already deducted
- Reagents consumed
- Spell effect triggered

**New Behavior:**
- Mana deducted here
- Reagents consumed here
- Fizzle on replaced spell handled here
- Spell effect triggered

#### 4. OnCasterMoving() Method
**Current Behavior:**
- Returns false if IsCasting and BlocksMovement

**New Behavior:**
- Returns true during targeting phase
- Returns true during post-target delay
- Only blocks movement if configured

---

## Testing Scenarios

### Scenario 1: Basic Spell Cast
1. Player initiates spell cast
2. Cursor appears immediately
3. Player selects target
4. Mana/reagents consumed
5. Cast delay begins
6. Animation plays
7. Spell effect applied

### Scenario 2: Spell Cancellation
1. Player casts Spell A
2. Before target selection, cast Spell B
3. Spell A cursor closes
4. Spell B cursor shows
5. Player selects target for Spell B
6. Spell A fizzles (resources consumed)
7. Spell B executes normally

### Scenario 3: Movement During Cast
1. Player initiates spell cast
2. Cursor appears
3. Player moves (should NOT fizzle)
4. Player selects target
5. Player moves during post-target delay (should NOT fizzle)
6. Spell effect applied

### Scenario 4: Damage During Cast
1. Player initiates spell cast
2. Player takes damage
3. Cast continues (no fizzle)
4. Player selects target
5. Spell effect applied

### Scenario 5: Bandage Interrupt
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

---

## Success Criteria

- [x] All automated tests passing
- [ ] Immediate cursor on cast initiation
- [ ] Post-target cast delay working
- [ ] Mana deducted at target confirmation
- [ ] Spell cancellation hierarchy implemented
- [ ] Movement allowed without fizzle
- [ ] Fizzle only on specified triggers
- [ ] No performance regression vs Phase 1 baseline
- [ ] All 7 projects build successfully

---

## References

- **Main Plan:** Claude.md - Section "Phase 2: Complete Spellcasting Integration"
- **Sphere Rules:** Sphere0.51aCombatSystem.md - Section 2.2
- **Spell Code:** Projects/UOContent/Spells/Base/Spell.cs
- **Helper Methods:** Projects/UOContent/Systems/Combat/SphereStyle/SphereSpellHelper.cs
- **Configuration:** Projects/UOContent/Systems/Combat/SphereStyle/SphereConfig.cs
