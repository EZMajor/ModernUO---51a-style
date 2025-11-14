# Sphere51a Module - Administrator Guide

## Table of Contents
1. [Quick Start](#quick-start)
2. [Known Limitations](#known-limitations)
3. [Configuration Options](#configuration-options)
4. [Admin Commands](#admin-commands)
5. [Performance Monitoring](#performance-monitoring)
6. [Configuration Tuning](#configuration-tuning)
7. [Troubleshooting](#troubleshooting)
8. [Migration Guide](#migration-guide)
9. [Best Practices](#best-practices)

---

## Quick Start

### Prerequisites

- ModernUO server (current build)
- .NET 9.0 runtime
- Admin/GM access level

### Enabling Sphere51a (3 Steps)

**Step 1**: Edit server configuration file `modernuo.json`:
```json
{
  "sphere": {
    "enableSphere51aStyle": true,
    "useGlobalPulse": true,
    "independentTimers": true
  }
}
```

**Step 2**: Ensure weapon timing config exists:
- File: `Data/Sphere51a/weapons_timing.json`
- Created automatically with default weapons
- Customizable per your shard needs

**Step 3**: Restart server and verify:
```
[VerifyWeaponTiming]
```

Expected output:
```
=== Weapon Timing Verification ===
Sphere51a Enabled: True
Integration Mode: CoreHooks
Provider: WeaponTimingProvider
```

---

## Known Limitations

### Current Implementation Status

The Sphere51a module is under active development. The following systems have different implementation statuses:

#### Weapon Combat System ✓ IMPLEMENTED
- **Status**: Fully functional and tested
- **Features**: 25ms precision timing, independent swing/spell timers, shadow mode verification
- **Testing**: Comprehensive test suite with audit logging
- **Configuration**: Complete weapon timing database (`weapons_timing.json`)
- **Integration**: Fully integrated with `BaseWeapon.cs` via event hooks

#### Spell System ✗ NOT YET IMPLEMENTED
- **Status**: Infrastructure exists but core integration is missing
- **Current State**:
  - Event system defined (`OnSpellCastBegin`, `OnSpellCastComplete`, etc.)
  - State tracking implemented (`SphereCombatState` tracks spell timers)
  - Configuration settings exist but are ignored
  - Audit logging handlers ready but unused
  - **CRITICAL**: `Spell.cs` does not have integration hooks to raise events
- **Impact**:
  - Spells currently use standard ModernUO timing (NOT Sphere51a)
  - Spell configuration settings are not applied
  - Spell tests are DISABLED in `test-config.json`
  - No spell audit logs are generated
- **Roadmap**: See [Spell Integration Roadmap](#spell-integration-roadmap) below

### Why Spell Tests Are Disabled

The Sphere51a testing framework includes **active integration verification** that checks whether core game systems properly raise Sphere events.

**The spell timing test will HARD FAIL if enabled** because:
1. No integration hooks exist in `Spell.cs` to raise `SphereEvents.OnSpellCast*()`
2. The test actively casts spells and verifies events are raised
3. When zero events are detected, the test fails with "Integration Missing" error
4. This is **by design** - tests should not give false confidence

This is a significant improvement over the previous passive testing approach, which could pass even when features didn't work.

### What Works vs What Doesn't

#### What Works (Weapon Combat)
- ✓ Weapon swing timing with 25ms precision
- ✓ Independent swing/spell timers
- ✓ DEX-based swing speed calculations
- ✓ Shadow mode for production verification
- ✓ Comprehensive audit logging
- ✓ Load testing and performance monitoring
- ✓ All weapon timing tests pass

#### What Doesn't Work Yet (Spells)
- ✗ Sphere51a spell cast timing
- ✗ Spell configuration settings (all ignored)
- ✗ Spell audit logging
- ✗ Spell timing tests (disabled)
- ✗ Immediate spell targeting
- ✗ Target-based mana deduction
- ✗ Movement during spell casting
- ✗ Spell/swing timer independence

**Note**: Spells work using standard ModernUO mechanics. They don't crash or break - they just don't use Sphere51a timing.

### Spell Integration Roadmap

Implementation is planned in phases (see project documentation):

**Phase 1**: Testing Framework Hardening ← **CURRENT PHASE**
- Make tests fail loudly when integration missing
- Add integration verification before test execution
- Update documentation to reflect actual status
- Prevent false confidence from passing tests

**Phase 2**: Spell Flow Documentation
- Create spell sequence diagrams
- Document all configuration behaviors
- Design spell timing database (`spell_timing.json`)
- Define integration hook points

**Phase 3**: Core Spell Integration
- Add hooks to `Spell.cs` to raise Sphere events
- Create `SpellTimingProvider` for timing calculations
- Implement configuration behaviors
- Add spell audit logging

**Phase 4**: Testing and Validation
- Enable spell timing tests
- Validate against Sphere 51a behavior
- Performance testing
- Stress testing with mixed combat/spells

**Phase 5**: Advanced Features (Optional)
- Spell scroll timing differences
- Fizzle system overhaul
- Special spell mechanics

### Testing System Improvements

The testing framework was recently hardened to prevent silent failures:

**Before (Passive Testing)**:
- Tests read audit logs hoping data exists
- No data = soft warning, test still "passes"
- Gave false confidence that spells worked

**After (Active Testing)**:
- Tests actively cast spells and measure results
- Integration verification runs before test execution
- No events raised = HARD FAILURE with clear error message
- Tests fail fast and loudly when integration missing

This change revealed the spell integration gap that was previously hidden.

### Workarounds for Production

If you need Sphere51a combat on a production shard **now**:

**Option 1: Weapon Combat Only (Recommended)**
- Enable Sphere51a for weapon timing
- Leave spells using ModernUO defaults
- Players get authentic melee combat
- Spell combat works but uses different timing
- Clearly communicate this to players

**Option 2: Wait for Full Implementation**
- Keep Sphere51a disabled
- Wait for Phase 3 (spell integration) completion
- Deploy with full feature parity

**Option 3: Contribute**
- Implementation roadmap is documented
- Code architecture is extensible
- Community contributions welcome

### Verifying Integration Status

Administrators can check integration status at any time:

**Command**: `[VerifyIntegration]` *(to be implemented)*

**Or run tests**:
```bash
dotnet run --project Projects/Application -- --test
```

Expected output:
```
═══════════════════════════════════════════════════
  Integration Status Check
═══════════════════════════════════════════════════

  Weapon Combat: ✓ ACTIVE
    Events firing correctly

  Spell System:  ✗ NOT IMPLEMENTED
    Events not raised by spell casting

  ⚠ WARNING: Spell integration not implemented
    - Spell timing tests will FAIL if enabled
    - Spell tests are DISABLED in test-config.json
    - See SPELL_ARCHITECTURE.md for requirements

═══════════════════════════════════════════════════
```

### Getting Updates

Watch for updates in:
- GitHub releases
- `CHANGELOG.md`
- Commit messages mentioning "Phase 2", "Phase 3", etc.

When spell integration is complete, this section will be updated.

---

## Configuration Options

### Core Settings

#### Enabled
**Type**: `boolean`
**Default**: `false`
**Location**: `modernuo.json` → `sphere.enableSphere51aStyle`

**Description**: Master toggle for the entire Sphere51a combat system

**Usage**:
```json
{
  "sphere": {
    "enableSphere51aStyle": true
  }
}
```

**When to enable**:
- Shard wants authentic Sphere 0.51a-style PvP mechanics
- Testing Sphere combat behavior on staging
- Migrating from standard ModernUO combat

**When to disable**:
- Using standard ModernUO combat system
- Troubleshooting compatibility issues
- Emergency rollback needed

---

#### IndependentTimers
**Type**: `boolean`
**Default**: `false`
**Location**: `modernuo.json` → `sphere.independentTimers`

**Description**: Enables independent action timers (swing/spell/bandage/wand operate independently)

**Impact**:
- `true`: Sphere 0.51a behavior - actions have separate timers
- `false`: ModernUO behavior - global combat timer

**Recommendation**: Set to `true` for authentic Sphere51a experience

**Example**:
```json
{
  "sphere": {
    "independentTimers": true
  }
}
```

---

### Performance Settings

#### UseGlobalPulse
**Type**: `boolean`
**Default**: `true`
**Location**: `modernuo.json` → `sphere.useGlobalPulse`

**Description**: Use centralized 50ms combat pulse system instead of per-mobile timers

**Performance Impact**:
- `true`: **Recommended** - Better performance, batched updates, lower CPU
- `false`: Per-mobile timers, higher memory usage, more overhead

**Recommendation**: Always keep `true` for production servers

---

#### GlobalTickMs
**Type**: `int`
**Default**: `50`
**Range**: `25-100` recommended
**Location**: `modernuo.json` → `sphere.globalTickMs`

**Description**: Combat pulse interval in milliseconds (controls timing precision)

**Tuning Guide**:

| Value | Use Case | Precision | CPU Impact |
|-------|----------|-----------|------------|
| 25ms  | Low player count (<50) | ±12.5ms | High |
| 50ms  | **Recommended** (50-150 players) | ±25ms | Balanced |
| 100ms | High player count (150+) | ±50ms | Low |

**Example**:
```json
{
  "sphere": {
    "globalTickMs": 50
  }
}
```

**Testing Procedure**:
1. Set `globalTickMs` value
2. Restart server
3. Run `[SphereLoadTest 5 100 75]` (5 min, 100 combatants, 75% frequency)
4. Check `[Perf]` for average tick time
5. Measure CPU usage
6. Optimal value: Lowest `globalTickMs` where CPU <50%

---

#### CombatIdleTimeoutMs
**Type**: `int`
**Default**: `5000` (5 seconds)
**Location**: `modernuo.json` → `sphere.combatIdleTimeoutMs`

**Description**: How long after last action before mobile is removed from combat pulse

**Tuning**:
- Lower (3000ms): Faster cleanup, may drop slow combatants
- Higher (10000ms): Longer retention, more memory usage
- Recommended: 5000ms

---

### Content Settings

#### WeaponTimingConfigPath
**Type**: `string`
**Default**: `"Data/Sphere51a/weapons_timing.json"`
**Location**: Configuration file

**Description**: Path to weapon-specific timing configuration

**Customization**:
Create custom weapon timing files to adjust swing speeds per weapon type.

**JSON Format**:
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

**Fields Explained**:
- `ItemID`: Weapon item ID (decimal, not hex)
- `WeaponSpeedValue`: 0-100 scale (higher = slower)
- `WeaponBaseMs`: Base delay (1600 = 1H, 1900 = 2H, 2000 = ranged)
- `AnimationHitOffsetMs`: When damage applies during swing animation
- `AnimationDurationMs`: Total animation length

---

### Debug/Logging Settings

#### EnableDebugLogging
**Type**: `boolean`
**Default**: `false`

**Description**: Enables detailed debug logging for troubleshooting

**Warning**: Generates large log files. Use only for troubleshooting.

---

#### EnableShadowLogging
**Type**: `boolean`
**Default**: `false`

**Description**: Enables shadow mode logging (compares Sphere vs ModernUO timing)

**Use Case**: Validation during migration, testing timing accuracy

---

## Admin Commands

### Verification Commands

#### [VerifyWeaponTiming]
**Purpose**: Display weapon timing details for equipped weapon

**Usage**:
```
[VerifyWeaponTiming
```

**Output**:
```
=== Weapon Timing Verification ===
Weapon: Katana
Provider: WeaponTimingProvider
Dexterity: 85
Attack Interval: 1350ms (1.35s)
Animation Hit Offset: 300ms
Animation Duration: 600ms
Next Swing Time: 12:34:56
Sphere State: Cast=false, Delay=false, Bandage=false, Wand=false, Swing=false
Active Combatant: Yes (Total: 1)
```

**What to check**:
- `Provider: WeaponTimingProvider` = Sphere timing active
- `Attack Interval` = Expected swing speed
- `Active Combatant: Yes` = Registered in combat pulse

---

#### [VerifyCombatTick]
**Purpose**: Display combat pulse system status

**Usage**:
```
[VerifyCombatTick
```

**Output**:
```
=== CombatPulse Status ===
Enabled: True
Tick Interval: 50ms
Active Combatants: 3
Idle Timeout: 5000ms
Total Ticks: 24567
```

**What to check**:
- `Active Combatants` = Number of mobiles in combat
- `Total Ticks` = System is running
- High active count = Potential stuck combatants (use cleanup)

---

### Performance Commands

#### [SpherePerformance] or [Perf]
**Purpose**: Real-time performance dashboard

**Usage**:
```
[Perf
```

**Output**:
```
=== CombatPulse Metrics ===
Total Ticks: 1247
Active Combatants: 12
Avg Tick Time: 0.023ms
Max Tick Time: 0.456ms
99th Percentile: 0.123ms
Target: ≤5ms per tick
Status: EXCELLENT
```

**Performance Interpretation**:

| Avg Tick Time | Status | Action |
|---------------|--------|--------|
| <1ms | Excellent | No action needed |
| 1-2ms | Good | Monitor trends |
| 2-5ms | Acceptable | Consider tuning |
| 5-10ms | Warning | Investigate causes |
| >10ms | Critical | Immediate action required |

---

#### [SpherePerfReport]
**Purpose**: Comprehensive performance analysis

**Usage**:
```
[SpherePerfReport           # Display in-game
[SpherePerfReport save      # Save to file
```

**Output**:
```
=== Sphere51a Performance Report ===
Integration Mode: CoreHooks
Total Ticks: 12,456
Average Tick Time: 1.2ms
Max Tick Time: 8.5ms
P95 Tick Time: 3.2ms
P99 Tick Time: 5.8ms
Active Combat States: 127
Memory Usage: ~25KB
Report saved to: Logs/SpherePerf_2025-11-06_14-30-15.txt
```

**When to run**:
- Weekly performance review
- After configuration changes
- Capacity planning
- Troubleshooting performance issues

---

### Load Testing Commands

#### [SphereLoadTest <minutes> <combatants> <frequency>]
**Purpose**: Synthetic load testing for performance benchmarking

**Usage**:
```
[SphereLoadTest 5 100 75
```

**Parameters**:
- `minutes`: Test duration (1-60)
- `combatants`: Number of simulated fighters (10-1000)
- `frequency`: Attack frequency percentage (0-100)

**Example Scenarios**:

**Light Load** (50 players):
```
[SphereLoadTest 2 50 60
```
Expected: <1ms average tick time

**Medium Load** (100 players):
```
[SphereLoadTest 5 100 75
```
Expected: <2ms average tick time

**Heavy Load** (200 players):
```
[SphereLoadTest 3 200 80
```
Expected: <5ms average tick time

**Stress Test** (500 players):
```
[SphereLoadTest 1 500 90
```
Expected: <10ms average tick time

**WARNING**: Only run stress tests on staging servers. May cause production lag.

---

### Shadow Testing Commands

#### [SphereShadowReport <action>]
**Purpose**: Compare Sphere vs ModernUO timing for validation

**Actions**:
- `start` - Begin capturing timing data
- `status` - Check current capture status
- `generate` - Create comparison report
- `clear` - Clear captured data
- `stop` - Stop capturing

**Usage Flow**:
```
[SphereShadowReport start
  ... let players fight for 30-60 minutes ...
[SphereShadowReport generate
[SphereShadowReport stop
```

**Output**:
```
=== Shadow Timing Report ===
Samples: 1,247
Sphere Avg: 1.35s
ModernUO Avg: 1.42s
Difference: -0.07s (-4.9%)
Max Delta: 0.15s
Variance: Acceptable
```

**Use Cases**:
- Migration validation
- Timing accuracy verification
- Configuration tuning validation

---

## Performance Monitoring

### Daily Monitoring Schedule

**Morning Peak** (09:00-10:00):
```
[Perf
```
Check average tick time, active combatants

**Evening Peak** (18:00-21:00):
```
[Perf
```
Monitor during highest player count

**Weekly Analysis** (Mondays):
```
[SpherePerfReport save
```
Archive performance data for trend analysis

---

### Performance Baselines

#### Healthy Server Metrics

| Metric | Target | Excellent | Good | Acceptable |
|--------|--------|-----------|------|------------|
| Avg Tick Time | <5ms | <1ms | 1-2ms | 2-5ms |
| P99 Tick Time | <20ms | <5ms | 5-10ms | 10-20ms |
| Max Tick Time | <50ms | <10ms | 10-30ms | 30-50ms |
| Active Combatants | Varies | <50 | 50-100 | 100-200 |
| CPU Impact | <10% | <2% | 2-5% | 5-10% |

---

### Alert Thresholds

**Warning Alerts** (investigate within 24h):
- Average tick time > 5ms
- P99 tick time > 50ms
- CPU usage increase > 10%
- Player reports of delayed swings

**Critical Alerts** (investigate immediately):
- Average tick time > 10ms
- P99 tick time > 100ms
- CPU usage > 80%
- Combat actions not registering
- Server freezes/stuttering

---

## Configuration Tuning

### Tuning GlobalTickMs

**Goal**: Find optimal balance between responsiveness and CPU usage

**Procedure**:

1. **Baseline** (default 50ms):
   ```
   [SphereLoadTest 5 100 75
   [Perf
   ```
   Record: Average tick time, CPU usage

2. **Test Lower** (25ms):
   ```
   # Edit config: "globalTickMs": 25
   # Restart server
   [SphereLoadTest 5 100 75
   [Perf
   ```
   Record: Average tick time, CPU usage

3. **Test Higher** (100ms):
   ```
   # Edit config: "globalTickMs": 100
   # Restart server
   [SphereLoadTest 5 100 75
   [Perf
   ```
   Record: Average tick time, CPU usage

4. **Compare Results**:
   - Pick lowest value where CPU <50%
   - Ensure average tick time <5ms
   - Test with real players for feel

---

### Tuning Weapon Timing

**Goal**: Adjust individual weapon swing speeds

**Steps**:

1. **Identify weapon** to adjust (e.g., Katana too fast)

2. **Find ItemID**:
   ```
   [Props
   # Target weapon
   # Note ItemID value
   ```

3. **Edit** `Data/Sphere51a/weapons_timing.json`:
   ```json
   {
     "ItemID": 5117,
     "Name": "Katana",
     "WeaponSpeedValue": 50,  // Increased from 46 to slow down
     "WeaponBaseMs": 1600,
     "AnimationHitOffsetMs": 300,
     "AnimationDurationMs": 600
   }
   ```

4. **Test**:
   ```
   [VerifyWeaponTiming
   # Equip modified weapon
   # Check new Attack Interval
   ```

5. **Validate** with real combat testing

**WeaponSpeedValue Guide**:
- Lower value = Faster weapon
- Higher value = Slower weapon
- Typical range: 20-75
- Formula: `attackMs = WeaponSpeedValue * 40ms`

---

## Troubleshooting

### Common Issues

#### Issue: Weapon swings feel delayed

**Symptoms**:
- Players report lag between clicking and swing
- Swings seem "sticky"
- Inconsistent responsiveness

**Diagnosis**:
```
[Perf
```

**Possible Causes & Solutions**:

| Cause | Check | Solution |
|-------|-------|----------|
| High tick time | Avg > 5ms | Lower GlobalTickMs or reduce load |
| Network latency | Player ping | Check network/ISP |
| High GlobalTickMs | Config value | Lower to 25ms (test staging first) |
| CPU bottleneck | Server CPU usage | Upgrade hardware or optimize |

**Resolution Steps**:
1. Run `[Perf]` - if avg tick time >5ms, performance issue
2. Run `[SphereLoadTest 100]` - if tick time jumps, CPU bottleneck
3. Reduce `GlobalTickMs` to 25ms (staging test first)
4. Check server CPU usage during peak hours

---

#### Issue: "Integration Mode: None" or hooks not working

**Symptoms**:
- `[VerifyWeaponTiming]` shows "Integration Mode: None"
- Combat still uses ModernUO timing
- Module appears enabled but not functioning

**Diagnosis**:
```
[VerifyWeaponTiming
```
Check for "Integration Mode" field

**Possible Causes**:
1. Module failed to initialize
2. Core hooks not present
3. Configuration error

**Resolution**:
1. Check server logs for initialization errors
2. Verify core hooks exist in BaseWeapon.cs and Mobile.cs
3. Ensure `"enableSphere51aStyle": true` in config
4. Restart server
5. If still failing, see ARCHITECTURE.md for hook details

---

#### Issue: High CPU usage after enabling

**Symptoms**:
- Server CPU jumps 15-30%
- Server lag during combat
- `[Perf]` shows high tick times

**Diagnosis**:
```
[Perf
[VerifyCombatTick
```

**Resolution Priority Order**:

1. **Check active combatants count**:
   - If >200: Stuck combatants, investigate cleanup
   - Normal count: Continue to step 2

2. **Increase GlobalTickMs**:
   ```json
   {"sphere": {"globalTickMs": 100}}
   ```
   Restart and monitor

3. **Reduce combat load**:
   - Increase `CombatIdleTimeoutMs` to 3000ms for faster cleanup
   - Check for infinite combat loops in custom scripts

4. **Disable debug logging**:
   ```json
   {"sphere": {"enableDebugLogging": false}}
   ```

5. **If still high**: Consider hardware upgrade or disable module

---

#### Issue: Combat feels different than Sphere 0.51a

**Symptoms**:
- Players report "not authentic Sphere feel"
- Timing seems off
- Actions cancel unexpectedly

**Diagnosis**:
```
[SphereShadowReport start
  ... test combat for 30 minutes ...
[SphereShadowReport generate
```

**Resolution**:

1. **Verify independent timers enabled**:
   ```json
   {"sphere": {"independentTimers": true}}
   ```

2. **Check weapon timing config exists**:
   - File: `Data/Sphere51a/weapons_timing.json`
   - Contains weapon entries for your weapons

3. **Compare timing**:
   ```
   [SphereShadowReport generate
   ```
   If large difference, adjust WeaponSpeedValue in config

4. **Review configuration**:
   - `SpellCancelSwing`: true
   - `SwingCancelSpell`: true
   - `AllowMovementDuringCast`: true

---

#### Issue: Combatants stuck in "active" state

**Symptoms**:
- `[VerifyCombatTick]` shows high active count
- Players not fighting but counted as active
- Performance degradation over time

**Diagnosis**:
```
[VerifyCombatTick
```
High active count with low actual combat

**Resolution**:

1. **Lower idle timeout**:
   ```json
   {"sphere": {"combatIdleTimeoutMs": 3000}}
   ```

2. **Manual cleanup** (temporary):
   ```
   # Restart server
   ```
   CombatPulse auto-cleans on startup

3. **Investigate root cause**:
   - Check for custom scripts keeping mobiles in combat
   - Review combat exit events
   - Check server logs for errors

---

### Error Messages Reference

#### "Sphere51a module not initialized"
**Meaning**: Module failed to start

**Check**:
1. Server logs for exception details
2. Configuration file syntax (valid JSON)
3. File permissions on Data/Sphere51a/

**Fix**: Resolve logged error and restart

---

#### "Weapon timing config not found"
**Meaning**: `weapons_timing.json` missing

**Fix**:
1. Create `Data/Sphere51a/` directory
2. Create `weapons_timing.json` with at least `[]`
3. System will use default timings

---

#### "Failed to register combat hooks"
**Meaning**: Core hooks not present in ModernUO

**Fix**:
1. Verify BaseWeapon.cs has hooks (see ARCHITECTURE.md)
2. Verify Mobile.cs has delegate and hook
3. Rebuild solution
4. Restart server

---

#### Issue: Equipment won't equip via double-click

**Symptoms**:
- Double-clicking equipment does nothing
- Item stays in backpack or on ground
- No error message displayed

**Diagnosis**:
1. Check if Sphere51a is enabled:
   ```
   [VerifyWeaponTiming
   ```
   Look for "Sphere51a Enabled: True"

2. Check distance (for ground items):
   - Must be within 2 tiles
   - Use Razor/UOSteam to check exact distance

**Possible Causes & Solutions**:

| Cause | Check | Solution |
|-------|-------|----------|
| Sphere51a disabled | Config file | Set `"enableSphere51aStyle": true` |
| Item out of range | Distance > 2 tiles | Move closer to item |
| Container locked | Container security | Unlock container or use accessible one |
| Insufficient stats | Strength/dex requirements | Increase stats or use different equipment |
| Wrong race | Race restrictions | Use appropriate equipment for race |

**Resolution Steps**:
1. Verify Sphere51a enabled in `modernuo.json`
2. For ground items: Stand next to item (1-2 tiles max)
3. For container items: Ensure container is accessible
4. Check if item has stat/race requirements
5. Try equipping from backpack instead

---

#### Issue: Items dropping to ground when equipping

**Symptoms**:
- New item equips successfully
- Old item drops to ground instead of going to backpack
- Message: "You are overweight. The item has been dropped at your feet."

**Diagnosis**:
- This is **intended behavior** when overweight
- Check character weight vs max weight

**Resolution**:
1. **Reduce carried weight**:
   - Remove items from backpack
   - Put items in bank or house
   - Increase Strength stat (increases max weight)

2. **Manage equipment swaps**:
   - Clear backpack space before equipping new items
   - Pick up dropped items immediately
   - Use secure containers for valuable equipment

**Note**: This is authentic UO behavior when overweight. It prevents items from being lost.

---

#### Issue: Two-handed weapon doesn't auto-unequip shield

**Symptoms**:
- Equipping two-handed weapon doesn't remove shield
- Shield stays equipped
- Weapon doesn't equip

**Diagnosis**:
- Check if weapon is truly two-handed
- Verify Sphere51a is enabled

**Possible Causes**:
1. Weapon is actually one-handed (some weapons may appear two-handed but aren't)
2. Sphere51a equipment system not enabled
3. Custom weapon with incorrect layer setting

**Resolution**:
1. Verify weapon layer in code (should be `Layer.TwoHanded`)
2. Ensure Sphere51a enabled
3. For custom weapons: Check `Layer` property is set correctly
4. Try equipping from backpack (not ground)

---

#### Issue: Double-clicking equipped item does nothing

**Symptoms**:
- Double-clicking worn equipment has no effect
- Item doesn't unequip

**Resolution**:
- This is **intended behavior**
- Use drag-and-drop to unequip items (standard UO behavior)
- Double-click only works for equipping, not unequipping
- This matches authentic Sphere 0.51a behavior

---

## Migration Guide

### Pre-Migration Checklist

**Technical Preparation**:
- [ ] Backup all world save files
- [ ] Test on staging server for 1 week minimum
- [ ] Review weapon timing config for your shard
- [ ] Set `independentTimers: true` in config
- [ ] Document rollback procedure
- [ ] Prepare monitoring dashboard

**Communication Preparation**:
- [ ] Notify players 1 week in advance
- [ ] Post combat mechanics guide
- [ ] Prepare FAQ for common questions
- [ ] Set up feedback collection method
- [ ] Plan GM availability for launch day

---

### Migration Steps (Staged Approach)

**Week 1: Staging Testing**
1. Enable Sphere51a on staging server
2. Recruit 10-20 test players
3. Run load tests: `[SphereLoadTest 5 50 75]`
4. Monitor performance: `[Perf]` every 4 hours
5. Collect player feedback
6. Tune `GlobalTickMs` if needed

**Week 2: Shadow Mode on Production**
1. Enable shadow logging:
   ```json
   {"sphere": {"enableShadowLogging": true}}
   ```
2. Keep Sphere51a disabled (testing only)
3. Collect timing data
4. Generate reports: `[SphereShadowReport generate]`
5. Validate timing accuracy

**Week 3: Production Rollout**
1. **Schedule maintenance window** (2 hours, low-traffic time)
2. **Backup saves**:
   ```bash
   cp -r Saves/ Saves.backup.$(date +%Y%m%d)
   ```
3. **Enable Sphere51a**:
   ```json
   {
     "sphere": {
       "enableSphere51aStyle": true,
       "independentTimers": true,
       "useGlobalPulse": true,
       "globalTickMs": 50
     }
   }
   ```
4. **Restart server**
5. **Verify immediately**:
   ```
   [VerifyWeaponTiming
   [VerifyCombatTick
   [Perf
   ```
6. **Monitor closely** for first 24 hours
7. **Check performance** every 2 hours

**Week 4: Post-Migration**
1. Send player guide explaining combat changes
2. Monitor forums/Discord for feedback
3. Run weekly performance reports
4. Tune weapon timings based on feedback
5. Document lessons learned

---

### Rollback Procedure

**When to roll back**:
- Critical bugs in combat
- Performance degradation >20%
- Mass player complaints
- Server instability

**Emergency Rollback** (5 minutes):

1. **Stop server immediately**:
   ```bash
   # Kill server process
   ```

2. **Disable Sphere51a**:
   ```json
   {
     "sphere": {
       "enableSphere51aStyle": false
     }
   }
   ```

3. **Restore saves** (if needed):
   ```bash
   rm -r Saves/
   cp -r Saves.backup.YYYYMMDD/ Saves/
   ```

4. **Restart server**

5. **Verify rollback**:
   ```
   [VerifyWeaponTiming
   # Should show disabled or ModernUO timing
   ```

6. **Communicate with players**:
   - Post announcement
   - Explain issue
   - Provide timeline for fix

---

## Best Practices

### Configuration Management

**Version Control**:
- Keep `modernuo.json` in Git
- Document changes with commit messages
- Tag stable configurations
- Never edit config while server running

**Change Process**:
1. Test change on staging first
2. Document expected impact
3. Schedule maintenance window
4. Apply change
5. Monitor for 24 hours
6. Document actual impact

---

### Performance Tuning Workflow

**Initial Setup** (Day 1):
1. Start with defaults (GlobalTickMs=50)
2. Run baseline test: `[SphereLoadTest 5 100 75]`
3. Record metrics: `[SpherePerfReport save]`

**Optimization** (Week 1):
1. Change ONE setting at a time
2. Run same load test
3. Compare metrics
4. Keep or revert based on results
5. Wait 24 hours before next change

**Validation** (Week 2):
1. Monitor real player load
2. Compare to synthetic tests
3. Adjust if needed
4. Finalize configuration

**Documentation** (Week 3):
1. Document final settings
2. Note reasons for values
3. Record expected metrics
4. Create runbook for ops team

---

### Player Communication

**Before Enabling**:
- Announce 1 week in advance
- Explain combat changes clearly
- Provide testing period on staging
- Set expectations for adjustment period

**Launch Day**:
- Post reminder announcement
- Have GMs online and ready
- Monitor feedback channels actively
- Be prepared to hotfix or rollback

**Post-Launch**:
- Weekly update on stability
- Thank players for feedback
- Iterate on tuning based on input
- Document changes transparently

---

### Monitoring Best Practices

**Daily Tasks**:
- Morning: `[Perf]` during peak
- Evening: `[Perf]` during peak
- Check for player reports of lag

**Weekly Tasks**:
- Monday: `[SpherePerfReport save]`
- Compare week-over-week trends
- Review any incidents
- Plan tuning if needed

**Monthly Tasks**:
- Comprehensive performance review
- Capacity planning assessment
- Configuration audit
- Documentation update

---

### Backup Strategy

**What to Backup**:
- `modernuo.json` (before any changes)
- `Data/Sphere51a/weapons_timing.json`
- World saves (before enabling Sphere51a)
- Performance baselines (weekly reports)

**Backup Schedule**:
- Before config changes: Always
- Daily: World saves (standard procedure)
- Weekly: Performance reports
- Monthly: Full configuration archive

**Retention**:
- Config backups: 6 months
- Save backups: Per standard policy
- Performance reports: 3 months
- Incident logs: 12 months

---

## Support and Resources

### Documentation

- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Technical architecture for developers
- **[README.md](README.md)** - System overview
- **DuelArena/README.md** - DuelArena subsystem documentation

### Getting Help

**Self-Service** (Check first):
1. This guide (troubleshooting section)
2. Server logs (Logs/Console.log)
3. Run diagnostic commands (`[Perf]`, `[VerifyWeaponTiming]`)

**Community Support**:
1. ModernUO Discord server
2. ModernUO forums
3. Search existing GitHub issues

**Opening an Issue**:
When requesting help, include:
```
## Environment
- ModernUO version: X.X.X
- Sphere51a version: 1.0.0
- Player count: XX
- Server specs: CPU/RAM

## Configuration
<paste modernuo.json sphere section>

## Diagnostics
<paste [VerifyWeaponTiming] output>
<paste [Perf] output>

## Logs
<paste relevant log excerpts>

## Problem Description
<describe issue clearly>
```

---

## Quick Reference Card

### Essential Commands
```
[VerifyWeaponTiming   - Check if Sphere51a is active
[Perf                 - Real-time performance metrics
[VerifyCombatTick     - Check combat pulse status
[SpherePerfReport     - Detailed performance analysis
[SphereLoadTest 5 100 75  - 5min load test, 100 combatants
```

### Configuration Quick Check
```json
{
  "sphere": {
    "enableSphere51aStyle": true,
    "useGlobalPulse": true,
    "independentTimers": true,
    "globalTickMs": 50,
    "combatIdleTimeoutMs": 5000
  }
}
```

### Performance Targets
- Avg tick time: <2ms (excellent), <5ms (acceptable)
- P99 tick time: <10ms (excellent), <20ms (acceptable)
- CPU impact: <5% increase

### Emergency Disable
1. Edit config: `"enableSphere51aStyle": false`
2. Restart server
3. Verify: `[VerifyWeaponTiming]`

---

**Version**: 1.0.0
**Last Updated**: November 2025
**Status**: Production Ready
