# Sphere51a Configuration & Testing Guide

## Status

✅ **Integration Complete** - All core hooks are in place and functional
✅ **Build Successful** - 0 errors, 0 warnings
✅ **Weapons Config Created** - `Data/Sphere51a/weapons_timing.json` exists
✅ **Documentation Complete** - README, ARCHITECTURE, and ADMIN_GUIDE ready

**Next Step**: Enable and test the system

---

## Quick Enable (3 Steps)

### Step 1: Configure ModernUO

ModernUO uses `ServerConfiguration.GetSetting()` to load settings. Configuration can be provided via:

**Option A: Environment Variables** (Recommended for testing)
```bash
# Windows
set sphere__enableSphere51aStyle=true
set sphere__useGlobalPulse=true
set sphere__independentTimers=true

# Linux/Mac
export sphere__enableSphere51aStyle=true
export sphere__useGlobalPulse=true
export sphere__independentTimers=true
```

**Option B: appsettings.json** (Create in Distribution folder)
```json
{
  "sphere": {
    "enableSphere51aStyle": true,
    "useGlobalPulse": true,
    "independentTimers": true,
    "globalTickMs": 50,
    "combatIdleTimeoutMs": 5000,
    "weaponTimingConfigPath": "Data/Sphere51a/weapons_timing.json"
  }
}
```

**Option C: modernuo.json** (If exists in your setup)
```json
{
  "sphere": {
    "enableSphere51aStyle": true,
    "useGlobalPulse": true,
    "independentTimers": true
  }
}
```

**Note**: The current codebase loads via `ServerConfiguration.GetSetting("sphere.enableSphere51aStyle", false)` which supports all above methods.

---

### Step 2: Verify Files Exist

Check that required files are present:

```bash
# From ModernUO-main directory
ls -la Data/Sphere51a/weapons_timing.json
ls -la Distribution/ModernUO.dll
ls -la Distribution/Assemblies/UOContent.dll
ls -la Distribution/Server.dll
```

Expected:
- ✅ `Data/Sphere51a/weapons_timing.json` (created during integration)
- ✅ All DLLs built with hooks included

---

### Step 3: Start Server

```bash
cd Distribution
./ModernUO  # or ModernUO.exe on Windows
```

**Watch for startup messages**:
```
[Sphere-Config] Sphere 51a Style Enabled: True
[INFO] Sphere configuration initialized - Enabled: True
[INFO] Sphere 51a system initialized successfully - Active Provider: WeaponTimingProvider
```

If you see **`Enabled: False`**, configuration was not loaded. Check environment variables or config file.

---

## Verification Tests

### Test 1: Check System Status

**In-game command**:
```
[VerifyWeaponTiming
```

**Expected Output**:
```
=== Weapon Timing Verification ===
Sphere51a Enabled: True
Integration Mode: CoreHooks
Provider: WeaponTimingProvider
Weapon: <your equipped weapon>
Dexterity: <your dex>
Attack Interval: <calculated delay>ms
Animation Hit Offset: <offset>ms
Animation Duration: <duration>ms
Next Swing Time: <timestamp>
Sphere State: Cast=false, Delay=false, Bandage=false, Wand=false, Swing=false
Active Combatant: <Yes/No> (Total: <count>)
```

**Success Criteria**:
- ✅ "Sphere51a Enabled: True"
- ✅ "Integration Mode: CoreHooks"
- ✅ "Provider: WeaponTimingProvider"

**If you see**:
- ❌ "Sphere51a Enabled: False" → Configuration not loaded
- ❌ "Provider: None" → System failed to initialize
- ❌ "Integration Mode: None" → Hooks not working

---

### Test 2: Check Combat Pulse

**In-game command**:
```
[VerifyCombatTick
```

**Expected Output**:
```
=== CombatPulse Status ===
Enabled: True
Tick Interval: 50ms
Active Combatants: <count>
Idle Timeout: 5000ms
Total Ticks: <number>
```

**Success Criteria**:
- ✅ "Enabled: True"
- ✅ "Tick Interval: 50ms"
- ✅ "Total Ticks" increases over time

---

### Test 3: Monitor Performance

**In-game command**:
```
[Perf
```

**Expected Output**:
```
=== CombatPulse Metrics ===
Total Ticks: <number>
Active Combatants: <count>
Avg Tick Time: <time>ms
Max Tick Time: <time>ms
99th Percentile: <time>ms
Target: ≤5ms per tick
Status: EXCELLENT/GOOD/WARNING
```

**Success Criteria**:
- ✅ Avg Tick Time: <2ms (excellent)
- ✅ 99th Percentile: <10ms (excellent)
- ✅ Status: EXCELLENT or GOOD

---

### Test 4: Combat Functionality

**Manual Test**:
1. Equip a weapon
2. Target a mobile (dummy, monster, or another player)
3. Attack and observe:
   - Animation plays immediately
   - Damage applies at correct timing
   - Swing delay matches expected interval

**Check with**:
```
[VerifyWeaponTiming
```
Note the "Attack Interval" value, then time swings manually to verify.

---

### Test 5: Load Test (Optional)

**Synthetic Load Test**:
```
[SphereLoadTest 2 50 75
```
(2 minutes, 50 combatants, 75% attack frequency)

**Monitor during test**:
```
[Perf
```

**Expected**:
- Avg Tick Time remains <2ms
- No server lag
- Performance stable throughout

---

## Configuration Options Reference

### Essential Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `enableSphere51aStyle` | bool | false | Master toggle - enables entire system |
| `useGlobalPulse` | bool | true | Use centralized 50ms tick (recommended) |
| `independentTimers` | bool | true | Separate timers for swing/spell/bandage/wand |
| `globalTickMs` | int | 50 | Combat pulse interval (25/50/100) |
| `combatIdleTimeoutMs` | int | 5000 | Idle timeout before cleanup (ms) |
| `weaponTimingConfigPath` | string | Data/Sphere51a/weapons_timing.json | Path to weapon config |

### Advanced Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `enableDebugLogging` | bool | false | Detailed debug logs (performance impact) |
| `enableShadowLogging` | bool | false | Compare Sphere vs ModernUO timing |
| `spellCancelSwing` | bool | true | Casting cancels weapon swing |
| `swingCancelSpell` | bool | true | Attacking cancels spell cast |
| `allowMovementDuringCast` | bool | true | No movement lock during casting |

---

## Troubleshooting

### Issue: "Sphere51a Enabled: False"

**Cause**: Configuration not loaded by ServerConfiguration

**Solutions**:

1. **Check environment variables** (Windows):
   ```cmd
   echo %sphere__enableSphere51aStyle%
   ```
   Should show "true" (not empty)

2. **Check config file exists**:
   ```bash
   ls appsettings.json
   # or
   ls modernuo.json
   ```

3. **Verify setting name** - Must be exactly:
   - `sphere.enableSphere51aStyle` (dot notation)
   - `sphere__enableSphere51aStyle` (environment variable with double underscore)

4. **Try direct code change** (temporary test):
   Edit `SphereConfiguration.cs` line 327:
   ```csharp
   Enabled = Server.ServerConfiguration.GetSetting("sphere.enableSphere51aStyle", true); // Changed false to true
   ```
   Rebuild and test.

---

### Issue: "Integration Mode: None"

**Cause**: Core hooks not present or not functioning

**Check**:

1. **Verify BaseWeapon.cs hooks exist**:
   ```bash
   grep -n "Sphere 51a" Projects/UOContent/Items/Weapons/BaseWeapon.cs
   ```
   Should show 4 matches (2 hooks with comments)

2. **Verify Mobile.cs hooks exist**:
   ```bash
   grep -n "Sphere 51a" Projects/Server/Mobiles/Mobile.cs
   ```
   Should show 3 matches (delegate + hook + comment)

3. **Rebuild** if hooks are present:
   ```bash
   dotnet build
   ```

4. **Verify DLLs updated**:
   ```bash
   ls -la Distribution/Server.dll
   ls -la Distribution/Assemblies/UOContent.dll
   ```
   Check timestamps are recent (after rebuild)

---

### Issue: "Provider: None"

**Cause**: SphereInitializer failed to create WeaponTimingProvider

**Check**:

1. **Server logs** for errors:
   ```
   Logs/Console.log
   ```
   Look for "Sphere 51a system" messages

2. **Weapons config exists**:
   ```bash
   ls -la Data/Sphere51a/weapons_timing.json
   ```

3. **Config is valid JSON**:
   ```bash
   cat Data/Sphere51a/weapons_timing.json | python -m json.tool
   ```

---

### Issue: High CPU Usage

**Diagnosis**:
```
[Perf
```

**If Avg Tick Time >5ms**:

1. **Increase tick interval**:
   ```json
   {"sphere": {"globalTickMs": 100}}
   ```

2. **Check active combatants**:
   ```
   [VerifyCombatTick
   ```
   If >200, investigate stuck combatants

3. **Disable debug logging**:
   ```json
   {"sphere": {"enableDebugLogging": false}}
   ```

---

## Testing Checklist

Use this checklist to verify complete functionality:

### Basic Functionality
- [ ] Server starts without errors
- [ ] `[VerifyWeaponTiming]` shows "Enabled: True"
- [ ] `[VerifyCombatTick]` shows "Enabled: True"
- [ ] `[Perf]` shows reasonable metrics (<5ms avg)

### Combat Testing
- [ ] Weapon swings work normally
- [ ] Attack intervals feel correct
- [ ] No double-swings occur
- [ ] Animations play smoothly
- [ ] Damage applies correctly

### Performance Testing
- [ ] `[SphereLoadTest 2 50 75]` completes successfully
- [ ] Performance remains stable under load
- [ ] CPU usage acceptable
- [ ] No server lag

### Advanced Features
- [ ] Independent timers work (can swing immediately after spell)
- [ ] Action cancellation works (spell cancels swing, etc.)
- [ ] Movement during casting allowed
- [ ] Multiple weapon types have correct timing

---

## Performance Tuning

### Optimal Settings by Server Size

**Small (1-50 players)**:
```json
{
  "sphere": {
    "enableSphere51aStyle": true,
    "useGlobalPulse": true,
    "independentTimers": true,
    "globalTickMs": 25,
    "combatIdleTimeoutMs": 3000
  }
}
```

**Medium (50-150 players)** - Recommended:
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

**Large (150+ players)**:
```json
{
  "sphere": {
    "enableSphere51aStyle": true,
    "useGlobalPulse": true,
    "independentTimers": true,
    "globalTickMs": 100,
    "combatIdleTimeoutMs": 5000
  }
}
```

---

## Quick Disable

If you need to disable Sphere51a:

**Option 1: Environment Variable**:
```bash
# Windows
set sphere__enableSphere51aStyle=false

# Linux/Mac
export sphere__enableSphere51aStyle=false
```

**Option 2: Config File**:
```json
{
  "sphere": {
    "enableSphere51aStyle": false
  }
}
```

Then restart server. System will pass all calls through to ModernUO.

---

## Next Steps After Enabling

1. **Monitor Performance** (first 24 hours):
   - Check `[Perf]` every 2-4 hours
   - Watch for player reports
   - Monitor CPU usage

2. **Tune if Needed**:
   - Adjust `globalTickMs` based on performance
   - Tune weapon speeds in `weapons_timing.json`
   - Enable/disable specific features

3. **Gather Feedback**:
   - Ask players about combat feel
   - Compare to expected Sphere 0.51a behavior
   - Use `[SphereShadowReport]` to validate timing

4. **Review Documentation**:
   - **[README.md](README.md)** - System overview
   - **[ADMIN_GUIDE.md](ADMIN_GUIDE.md)** - Complete admin guide
   - **[ARCHITECTURE.md](ARCHITECTURE.md)** - Technical details

---

## Support

**If you encounter issues**:

1. Check this guide's troubleshooting section
2. Review server logs: `Logs/Console.log`
3. Run all diagnostic commands
4. See [ADMIN_GUIDE.md](ADMIN_GUIDE.md) for detailed troubleshooting

**For help, provide**:
- Output from `[VerifyWeaponTiming]`
- Output from `[Perf]`
- Configuration method used (env vars / file)
- Relevant log excerpts

---

**Status**: Ready to enable and test!

**Estimated time**: 5-10 minutes for basic testing
