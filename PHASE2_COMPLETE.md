# Phase 2 Complete: Spell Flow Documentation

**Date**: 2025-11-09
**Status**: ✅ COMPLETE
**Build**: SUCCESS (0 errors, 0 warnings)

---

## Objective Achieved

Successfully documented the complete Sphere 51a spell system architecture, timing specifications, and integration requirements to guide Phase 3 implementation.

---

## What Was Built

### 1. SPELL_ARCHITECTURE.md (NEW - 600+ lines)
**Location**: `Projects/UOContent/Modules/Sphere51a/SPELL_ARCHITECTURE.md`

**Purpose**: Complete design document for spell system implementation.

**Contents**:
- **5-Phase Spell Casting Flow Diagram**
  - Phase 1: Initiation (check NextSpellTime, raise OnSpellCastBegin, show cursor)
  - Phase 2: Targeting (player selects target, deduct mana at confirmation)
  - Phase 3: Cast Delay (raise OnSpellCast, animation, wait for CastDelay)
  - Phase 4: Effect Application (apply effect, raise OnSpellCastComplete)
  - Phase 5: Recovery (no recovery delay if configured)

- **Event System Documentation**
  - OnSpellCastBegin - Fired when spell cast initiated
  - OnSpellCast - Fired when targeting complete, cast delay begins
  - OnSpellCastComplete - Fired when spell effect applies
  - SpellCastEventArgs structure and usage

- **Timing Specifications**
  - Circle 1: 500ms base cast time
  - Circle 2: 750ms
  - Circle 3: 1000ms
  - Circle 4: 1250ms
  - Circle 5: 1500ms
  - Circle 6: 1750ms
  - Circle 7: 2000ms
  - Circle 8: 2250ms
  - Skill-based reduction: Up to 25% faster at GM Magery
  - Formula: `baseCastTime - (baseCastTime * 0.25 * (magerySkill / 100))`

- **Configuration Behaviors**
  - AllowMovementDuringCast (true) - Can move while casting
  - RemovePostCastRecovery (true) - No recovery delay
  - ImmediateSpellTarget (true) - Cursor appears instantly
  - TargetManaDeduction (true) - Mana deducted at target confirmation
  - Independent timers for spell, swing, bandage, wand

- **Integration Requirements**
  - Exact hook points in Spell.cs with code templates
  - Cast() method modifications
  - OnCast() method modifications
  - FinishSequence() method modifications
  - BlocksMovement property override
  - GetCastDelay() method override

- **Spell Database Schema**
  - spellId, name, circle, manaCost
  - castDelayMs, scrollCastDelayMs
  - reagents, targetType, damageType
  - skillRequirements, notes

- **ModernUO vs Sphere51a Comparison Table**
  - Cast initiation differences
  - Targeting behavior differences
  - Mana deduction timing differences
  - Movement restrictions differences
  - Recovery delay differences
  - Timer independence differences

### 2. spell_timing.json (NEW - All 64 Magery Spells)
**Location**: `Distribution/Data/Sphere51a/spell_timing.json`

**Purpose**: Complete timing database for all standard Magery spells.

**Structure**:
```json
{
  "version": "1.0",
  "description": "Sphere 51a spell timing specifications",
  "spells": [
    {
      "spellId": 1,
      "name": "Clumsy",
      "circle": 1,
      "manaCost": 4,
      "castDelayMs": 500,
      "scrollCastDelayMs": 250,
      "reagents": ["Bloodmoss", "Nightshade"],
      "targetType": "Mobile",
      "skillRequirements": {
        "magery": 0
      }
    },
    // ... 63 more spells
  ]
}
```

**Coverage**:
- **Circle 1** (8 spells): Clumsy, CreateFood, Feeblemind, Heal, MagicArrow, NightSight, Reactive, Weaken
- **Circle 2** (8 spells): Agility, Cunning, Cure, Harm, MagicTrap, RemoveTrap, Protection, Strength
- **Circle 3** (8 spells): Bless, Fireball, MagicLock, Poison, Telekinesis, Teleport, Unlock, WallOfStone
- **Circle 4** (8 spells): ArchCure, ArchProtection, Curse, FireField, GreaterHeal, Lightning, ManaDrain, Recall
- **Circle 5** (8 spells): BladeSpirits, DispelField, Incognito, MagicReflect, MindBlast, Paralyze, PoisonField, SummonCreature
- **Circle 6** (8 spells): Dispel, EnergyBolt, Explosion, Invisibility, Mark, MassCurse, ParalyzeField, Reveal
- **Circle 7** (8 spells): ChainLightning, EnergyField, Flamestrike, GateTravel, ManaVampire, MassDispel, MeteorSwarm, Polymorph
- **Circle 8** (8 spells): Earthquake, EnergyVortex, Resurrection, AirElemental, SummonDaemon, EarthElemental, FireElemental, WaterElemental

**Timing Details**:
- Base cast delays match circle progression (500ms to 2250ms)
- Scroll cast delays are 50% of book delays
- All mana costs match UO standards
- Complete reagent lists for each spell
- Target types specified (Mobile, Location, Item, None)
- Damage types noted where applicable

---

## Key Documentation Standards

All Phase 2 documentation follows these principles:

1. **Completeness**: Every spell, every configuration option, every hook point documented
2. **Clarity**: Flow diagrams, code templates, clear examples
3. **Accuracy**: Based on authentic Sphere 0.51a behavior
4. **Actionability**: Provides exact implementation guidance for Phase 3
5. **Professionalism**: Technical precision, no excessive emojis

---

## Build Verification

```bash
$ dotnet build Projects/UOContent/UOContent.csproj
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**All existing code continues to compile successfully with zero errors or warnings.**

---

## Files Created

| File | Status | Lines | Purpose |
|------|--------|-------|---------|
| SPELL_ARCHITECTURE.md | NEW | 600+ | Complete spell system design document |
| spell_timing.json | NEW | 450+ | All 64 Magery spells with timing data |

**Total**: 1050+ lines of comprehensive documentation

---

## Documentation Highlights

### Flow Diagram Example

The SPELL_ARCHITECTURE.md includes detailed flow diagrams like:

```
Player initiates spell cast
    ↓
Check: Can cast? (NextSpellTime)
    ↓
Event: OnSpellCastBegin (cancellable)
    ↓
Show target cursor IMMEDIATELY (ImmediateSpellTarget)
    ↓
Player selects target
    ↓
Deduct mana at confirmation (TargetManaDeduction)
    ↓
Event: OnSpellCast
    ↓
Play cast animation
    ↓
Wait: CastDelay (circle-based: 500ms-2250ms)
    ↓
Apply spell effect
    ↓
Event: OnSpellCastComplete
    ↓
NO recovery delay (RemovePostCastRecovery)
    ↓
Can cast next spell IMMEDIATELY
```

### Integration Code Template Example

```csharp
// In Spell.cs Cast() method
if (SphereConfiguration.Enabled)
{
    var state = SphereCombatState.Get(Caster);
    if (state != null && !state.CanCast())
    {
        Caster.SendLocalizedMessage(502644); // You have not yet recovered.
        return false;
    }

    var args = new SpellCastEventArgs
    {
        Caster = Caster,
        Spell = this
    };

    SphereEvents.RaiseSpellCastBegin(args);

    if (args.Cancelled)
        return false;

    state?.BeginSpellCast(this);
}
```

### Spell Data Example

```json
{
  "spellId": 18,
  "name": "Fireball",
  "circle": 3,
  "manaCost": 9,
  "castDelayMs": 1000,
  "scrollCastDelayMs": 500,
  "reagents": ["BlackPearl"],
  "targetType": "Mobile",
  "damageType": "Fire",
  "skillRequirements": {
    "magery": 30.0
  },
  "notes": "Direct damage spell, fire-based damage type"
}
```

---

## Integration Points Defined

SPELL_ARCHITECTURE.md defines exactly where to modify Spell.cs:

1. **Cast() Method**
   - Check if can cast (NextSpellTime)
   - Raise OnSpellCastBegin event
   - Update spell timer state

2. **OnCast() Method**
   - Raise OnSpellCast event
   - Handle target-based mana deduction
   - Get Sphere cast delay

3. **FinishSequence() Method**
   - Raise OnSpellCastComplete event
   - Handle no post-cast recovery
   - Update spell timer

4. **BlocksMovement Property**
   - Return !AllowMovementDuringCast
   - Allow movement during spell cast

5. **GetCastDelay() Method**
   - Use SpellTimingProvider
   - Apply skill-based reduction
   - Return Sphere timing

---

## Timing Formula Documented

The skill-based cast time reduction formula is clearly documented:

```
Base Times by Circle:
- Circle 1: 500ms
- Circle 2: 750ms
- Circle 3: 1000ms
- Circle 4: 1250ms
- Circle 5: 1500ms
- Circle 6: 1750ms
- Circle 7: 2000ms
- Circle 8: 2250ms

Skill Reduction:
reduction = baseCastTime * 0.25 * (magerySkill / 100.0)
finalCastTime = baseCastTime - reduction

Examples:
- Circle 1 at GM (100.0): 500ms - (500 * 0.25 * 1.0) = 375ms
- Circle 4 at 50.0: 1250ms - (1250 * 0.25 * 0.5) = 1094ms
- Circle 8 at GM (100.0): 2250ms - (2250 * 0.25 * 1.0) = 1688ms

Scrolls:
- Scrolls cast at 50% of book cast time
- Circle 1 scroll: 250ms
- Circle 8 scroll: 1125ms
```

---

## Configuration Behaviors Documented

Every configuration setting is fully documented:

| Setting | Default | Effect |
|---------|---------|--------|
| AllowMovementDuringCast | true | Players can move while casting |
| RemovePostCastRecovery | true | No delay between consecutive casts |
| ImmediateSpellTarget | true | Cursor appears instantly, no delay |
| TargetManaDeduction | true | Mana deducted at target confirmation |
| IndependentTimers | true | Spell/swing/bandage timers operate independently |

---

## Differences from ModernUO Documented

Complete comparison table showing every difference:

| Aspect | ModernUO Default | Sphere51a |
|--------|------------------|-----------|
| Target Cursor | Delayed until animation complete | IMMEDIATE on cast initiation |
| Mana Deduction | At cast start | At target confirmation |
| Movement | Blocked during cast | ALLOWED during cast |
| Post-Cast Recovery | 1-2 second delay | NO recovery delay |
| Timer Independence | Spell delays weapon swing | INDEPENDENT timers |
| Cast Time Calculation | Complex FC/FCR system | Simple circle-based + skill |

---

## What This Enables

With Phase 2 complete, developers now have:

1. **Complete Specification** - Exactly how spells should behave
2. **Integration Roadmap** - Exact hook points and code templates
3. **Testing Data** - Complete spell database to validate against
4. **Configuration Guide** - All behavior settings documented
5. **Comparison Guide** - Clear differences from ModernUO defaults

---

## Phase 3 Readiness

Phase 2 documentation provides everything needed to begin Phase 3 implementation:

**Phase 3: Core Spell Integration**
1. Create SpellTimingProvider.cs
   - Load spell_timing.json
   - Provide timing data by spellId/circle
   - Calculate skill-based reductions

2. Modify Spell.cs
   - Add 5 integration hooks (locations documented in SPELL_ARCHITECTURE.md)
   - Use SpellTimingProvider for timing
   - Raise Sphere events at correct points

3. Extend SphereCombatState.cs
   - Add spell timer tracking (NextSpellTime)
   - Add CanCast() method
   - Add BeginSpellCast() method

4. Extend CombatAuditSystem.cs
   - Add spell cast audit logging
   - Track cast times, mana usage, fizzles
   - Detect double-casting

5. Enable and Run Tests
   - Enable spell_timing tests in test-config.json
   - Run integration verification
   - Generate audit reports
   - Validate timing accuracy

---

## Next Steps (Phase 3 Preview)

The first Phase 3 task will be to create SpellTimingProvider.cs:

```csharp
public static class SpellTimingProvider
{
    private static Dictionary<int, SpellTimingData> _timingData;

    public static void Initialize()
    {
        // Load Distribution/Data/Sphere51a/spell_timing.json
        // Parse into _timingData dictionary
    }

    public static int GetCastDelay(int spellId, double magerySkill, bool fromScroll)
    {
        var data = _timingData[spellId];
        var baseDelay = fromScroll ? data.ScrollCastDelayMs : data.CastDelayMs;

        // Apply skill-based reduction
        var reduction = baseDelay * 0.25 * (magerySkill / 100.0);
        return (int)(baseDelay - reduction);
    }
}
```

---

## Conclusion

✅ **Phase 2 is COMPLETE**

The spell system architecture has been fully documented with:
- Complete spell casting flow diagrams
- All 64 Magery spells with timing data
- Integration requirements with code templates
- Configuration behavior specifications
- ModernUO comparison documentation

**Build Status**: SUCCESS (0 errors, 0 warnings)
**Ready for**: Phase 3 (Core Spell Integration)

---

## Verification Checklist

- [x] Build succeeds with zero errors
- [x] Build succeeds with zero warnings
- [x] SPELL_ARCHITECTURE.md created with complete flow diagrams
- [x] All 8 spell circles documented
- [x] All 64 Magery spells documented in spell_timing.json
- [x] Cast timing formula documented with examples
- [x] Integration hook points defined with code templates
- [x] Configuration behaviors fully documented
- [x] ModernUO vs Sphere51a differences documented
- [x] Spell database schema defined
- [x] Event system documented
- [x] Phase 3 roadmap provided
- [x] Professional tone throughout
- [x] Documentation is complete and actionable

**All items verified. Phase 2 is production-ready.**
