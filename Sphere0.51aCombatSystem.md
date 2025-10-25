Sphere 0.51a Combat System Specification
Document Purpose: Technical specification for implementing Sphere 0.51a combat and spellcasting timing within ModernUO framework.
Target Repository: https://github.com/modernuo/ModernUO
Status: Implementation Reference

1. Implementation Objective
Replace RunUO-style recovery and synchronization logic with Sphere 0.51a combat mechanics. All timers operate independently; no global cooldowns or action queuing.

2. Sphere 0.51a Core Mechanics
2.1 Combat Flow

Movement unrestricted during spellcasting.
Swing timer and cast timer operate independently without shared recovery.
Action cancellation rules:

Starting spell/bandage/wand cancels pending weapon swing.
Beginning attack cancels active spell cast.
Weapon swings disabled during casting or cast delay.


Swing speed calculation: Dexterity + weapon base speed only.
Swing timer resets on interrupt; no queued swings.
Damage applies immediately upon hit confirmation.
Melee and ranged weapons use identical swing logic.

2.2 Spellcasting

**Casting Sequence:**
1. Spell initiated → Target cursor appears immediately (no pre-target delay)
2. Target selected → Power words spoken, casting animation begins
3. Cast delay executes → Character performs casting animation with hand effects
4. Cast completes → Spell effect applies to target

**Timing Rules:**
- Target cursor appears instantly when spell is cast (no initial delay)
- Cast delay (power words + animation) occurs AFTER target selection
- Movement permitted during entire cast sequence (no movement locks)
- Mana deducted at target confirmation (before cast delay)
- No post-cast recovery delay

**Interruption:**
- Fizzle triggers only after target selection when interrupted by defined actions
- Damage does not interrupt casting (configurable)
- Starting a new action (swing/bandage/wand) cancels active cast

**Self-Cast vs Targeted Spells:**
- Beneficial self-cast spells (Magic Reflect, Bless, etc.) require targeting self
- All spells follow same sequence: cast → target → delay → effect
- No auto-cast spells; all require target confirmation

2.3 Item and Skill Interaction

Bandage use cancels swing or cast; operates on independent timer.
Wand use cancels swing or cast; executes instantly.
Potion use permitted anytime; does not cancel other actions.

2.4 System Rules

All timers independent: NextSwingTime, NextSpellTime, NextBandageTime.
No global cooldown or shared recovery logic.
Interrupts handled per-action, not globally.
Server authoritative on all timing.
No animation locks during casting (movement allowed).
Weapons/shields remain equipped during spellcasting (no forced unequip).


3. ModernUO Divergence Matrix
ComponentModernUO DefaultRequired ImplementationGlobal RecoveryRecoveryDelay or post-swing cooldownRemove; allow continuous action flowSwing vs CastSwing timer pauses during castingSeparate timers; cancel swing on cast startCast vs SwingCasting waits for swing completionCancel swing; prioritize castMovementSpell-based movement restrictionsRemove cast movement locksDamage TimingDelayed to event tickApply immediately on hit confirmationSpell FizzleTriggered by movement or damageRestrict to defined cancel actionsBandageGlobal delay blocks other actionsIndependent timer; cancels swing/castWandsRegular spell delayInstant-cast behaviorRanged vs MeleeSeparate delay logicUnified timing and cancel behaviorAction QueueQueued swing/cast actionsDisable queuing; reset on interruptTimer ControlShared global delay variablesIsolated per-system timers

4. Pre-Implementation Verification
4.1 Recovery Logic Audit

Locate and remove RecoveryDelay or equivalent global cooldown.
Verify no shared "next action" timers block action flow.

4.2 Timer Independence Validation

Confirm NextSwingTime, NextSpellTime, NextBandageTime do not cross-reset.
Test cancellation matrix:

Cast → Swing (swing cancelled)
Swing → Cast (cast cancelled)
Bandage/Wand → Both (both cancelled)



4.3 Interrupt Hierarchy
Priority Order:

Casting cancels swing.
Swing cancels active cast.
Bandage or wand cancels both.
Damage cancels cast only after target confirmation (configurable).

4.4 Movement and Animation

Verify walking/running permitted during casting.
Remove forced animation locks.
Confirm equipping items does not block spellcasting.

4.5 Damage Application

Damage applies same tick as hit confirmation.
Disable deferred hit events except projectile travel time.

4.6 Action Conflict Testing
Test Sequences:

Cast → Swing → Bandage → Wand → Potion
Swing → Cast → Interrupt
Cast → Move → Target → Complete

Verify no persistent recovery states or action queues.
4.7 Speed Calculation

Audit BaseWeapon.GetDelay() or equivalent.
Ensure calculation: BaseSpeed / (Dexterity / 100).
Remove skill or stat multipliers not in Sphere 51a.

4.8 Client Synchronization

Server authoritative; client animation timing ignored.
Discard client-side "busy" state enforcement.

4.9 Performance Validation

Timer cleanup on action cancellation.
Event handler memory leak prevention.
Consistent timing under high latency and player count.


5. Implementation Scope
5.1 Affected Files
Core Combat:

BaseWeapon.cs – Swing timing, cancellation, damage application.

Spellcasting:

Spell.cs – Cast initiation, target handling, delay logic.
SpellHelper.cs – Fizzle control, movement flags.

Items and Skills:

BandageContext.cs – Independent timer, cancel integration.
Server.Items.Wand.cs – Instant cast behavior.

Mobile State:

Mobile.cs / PlayerMobile.cs – Timer scheduling, interrupt hierarchy, movement flags.

5.2 Code Standards

Prefix modifications: // Sphere-style edit: [description]
Use isolated state tracking; avoid shared cooldown variables.
Maintain separation from original ModernUO logic for reversion capability.

5.3 Testing Protocol
Controlled Scenarios:

Dual-player combat with timestamp logging.
All action combinations: cast, swing, bandage, wand, potion.
Latency simulation (100ms, 250ms, 500ms).
Melee vs ranged parity testing.

Validation:

Timer independence confirmed.
Cancellation hierarchy verified.
No action queuing or recovery states.


6. Optional Configuration Flags
Implement toggles for divergent behavior:

DamageBasedFizzle – Enable/disable damage interrupting cast post-target.
TargetManaDeduction – Deduct mana on target vs cast start.
SpellReflectTiming – Immediate vs delayed reflect application.


7. Compliance Checklist
Before Deployment:

 All global recovery delays removed or bypassed.
 Timers operate independently without cross-reset.
 Cancellation hierarchy matches Section 4.3.
 Movement unrestricted during casting.
 Damage applies immediately on hit.
 No action queuing; timers reset on interrupt.
 Swing speed calculation verified.
 Client synchronization disabled.
 Performance tested under load.
 Code comments include // Sphere-style edit markers.


8. Regression Testing Requirements
Build automated tests for:

Independent timer operation.
Action cancellation matrix.
Movement during cast.
Damage timing.
Bandage/wand instant cancellation.
Potion use during other actions.

Maintain test suite for future ModernUO updates.

9. Documentation Standards
All implementations must include:

Inline XML documentation (/// <summary>).
Rationale for divergence from RunUO patterns.
Reference to this specification document.
Unit test coverage where applicable.

