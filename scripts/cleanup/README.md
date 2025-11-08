# Sphere51a-ModernUO Cleanup Scripts

This folder contains cleanup utilities for resetting the Sphere51a-ModernUO server to various states.

## Available Scripts

### 1. Full Cleanup (`full-cleanup.bat` / `full-cleanup.ps1`)
**Complete reset to fresh installation state**

Removes:
- [YES] **All configuration files** (modernuo.json, expansion.json, Sphere51a config)
- [YES] All runtime data (saves, accounts, logs, backups)
- [YES] All runtime files (log files, temp files, databases)
- [YES] All Distribution assemblies (built binaries)
- [YES] **Optional:** Build artifacts (bin/obj directories)

**When to use:**
- Testing fresh installation
- Need all prompts to appear (Data Path, IP, Expansion, Maps, Sphere51a)
- Resetting server to factory defaults
- Before creating baseline or release

**Result:** Next startup will show ALL configuration prompts

---

### 2. Runtime Cleanup (`runtime-cleanup.bat` / `runtime-cleanup.ps1`)
**Clean runtime artifacts, keep configuration**

Removes:
- [YES] Runtime data (saves, accounts, logs, backups)
- [YES] Runtime files (log files, temp files, databases)
- [NO] **Keeps** configuration files (prompts won't appear)
- [NO] **Keeps** Distribution assemblies (no rebuild needed)

**When to use:**
- Daily development cleanup
- Testing with same configuration
- Clearing old saves/accounts without reconfiguring
- Regular maintenance

**Result:** Next startup uses existing configuration (no prompts)

---

## Usage

### Windows (Recommended)
```batch
# Full cleanup
full-cleanup.bat

# Runtime cleanup only
runtime-cleanup.bat
```

### PowerShell Direct
```powershell
# Full cleanup
powershell -ExecutionPolicy Bypass -File full-cleanup.ps1

# Runtime cleanup only
powershell -ExecutionPolicy Bypass -File runtime-cleanup.ps1
```

---

## What Gets Cleaned

### Configuration Files (Full Cleanup Only)
```
Configuration/
├── modernuo.json         ← Server settings (Data Path, IP/Port, Sphere51a)
├── expansion.json        ← Expansion and map selection
└── email-settings.json   ← Email configuration

Projects/UOContent/Modules/Sphere51a/Configuration/
└── config.json          ← Sphere51a module configuration
```

### Runtime Data (Both Scripts)
```
Distribution/
├── Logs/                ← Server logs
├── Saves/               ← World saves
├── Accounts/            ← Player accounts
└── Backups/             ← Backup files
```

### Runtime Files (Both Scripts)
- `*.log` - Log files
- `*.tmp`, `*.temp` - Temporary files
- `*.bak` - Backup files
- `*.db`, `*.sqlite` - Database files
- `*.pid`, `*.lock` - Process files
- `.DS_Store`, `Thumbs.db` - OS files

### Distribution Assemblies (Full Cleanup Only)
```
Distribution/
├── Assemblies/          ← Built DLLs and plugins
├── Data/                ← Runtime data files
├── *.dll                ← Server binaries
├── *.exe                ← Server executable
└── *.config             ← Runtime configs
```

### Build Artifacts (Optional - Full Cleanup)
```
Projects/
├── **/bin/              ← Build output
└── **/obj/              ← Intermediate files
```

---

## Configuration Prompts

After **Full Cleanup**, the next server startup will show these prompts in order:

1. **Data Path Prompt**
   ```bash
   Please enter the absolute path to your ClassicUO or Ultima Online data:
   ```

2. **IP/Port Prompt**
   ```bash
   Please enter the IP and ports to listen:
   ```

3. **Expansion Prompt**
   ```bash
   Choose expansion: [list of expansions]
   ```

4. **Maps Prompt**
   ```bash
   Select maps to use: [list of maps]
   ```

5. **Sphere51a Combat System Prompt**
   ```bash
   Choose your combat system:
     1. ModernUO Style (default UO mechanics)
     2. Sphere 51a Style (classic Sphere mechanics)
   [1] or 2>
   ```

6. **Account Creation**
   ```bash
   Do you want to create the owner account now? (y/n):
   ```

After **Runtime Cleanup**, no prompts will appear (uses existing configuration).

---

## Troubleshooting

### Problem: Prompts don't appear after Full Cleanup
**Cause:** Configuration files still exist
**Solution:** Manually verify these files are deleted:
- `Configuration/modernuo.json`
- `Configuration/expansion.json`
- `Projects/UOContent/Modules/Sphere51a/Configuration/config.json`

### Problem: Script shows "Execution Policy" error
**Cause:** PowerShell execution policy restricts scripts
**Solution:** Run with bypass flag:
```powershell
powershell -ExecutionPolicy Bypass -File full-cleanup.ps1
```

### Problem: Need to rebuild after cleanup
**Cause:** Chose to clean build artifacts (bin/obj)
**Solution:** Rebuild the solution:
```batch
dotnet build
```

### Problem: Server won't start after cleanup
**Cause:** Distribution assemblies were deleted
**Solution:** Rebuild to regenerate assemblies:
```batch
dotnet build
cd Distribution
dotnet ModernUO.dll
```

---

## Notes

- **Full Cleanup** is destructive - backs up important saves before running!
- **Runtime Cleanup** is safe for daily use
- Both scripts skip `.git`, `node_modules`, and other protected directories
- Scripts provide detailed output showing what's being removed
- Optional build cleanup requires confirmation (prevents accidental rebuilds)

---

## Quick Reference

| Goal | Script | Config Deleted? | Rebuild Required? |
|------|--------|-----------------|-------------------|
| Fresh install test | `full-cleanup.bat` | YES | Only if bin/obj cleaned |
| Daily cleanup | `runtime-cleanup.bat` | NO | NO |
| Remove old saves | `runtime-cleanup.bat` | NO | NO |
| Test all prompts | `full-cleanup.bat` | YES | Only if bin/obj cleaned |
| Factory reset | `full-cleanup.bat` (clean all) | YES | YES |

---

## Support

For issues or questions:
- Repository: https://github.com/EZMajor/ModernUO---51a-style
- ModernUO Docs: https://docs.modernuo.com

---

**Last Updated:** 2025-01-07
**Version:** 1.0.0
