# Sphere51a-ModernUO Repository Cleanup Guide

## Overview

This repository uses a baseline commit system to maintain clean, consistent development sessions. The baseline commit (`f51ff3f37518a853d99ecbb409827e03d8b50500`) represents the clean ModernUO + Sphere51a integration state.

## Baseline Concept

The baseline commit serves as your "fresh install" reference point. Any files that exist beyond this baseline are considered:
- Build artifacts (bin/, obj/, *.dll, *.exe, *.pdb)
- Runtime files (logs, saves, accounts, backups)
- Temporary files (*.tmp, *.bak, etc.)
- Development artifacts that should be cleaned up

## Workflow

### Before Starting Development
```bash
# Verify repository is clean
verify.bat

# If not clean, run cleanup
cleanup.bat

# Or reset to fresh baseline (WARNING: deletes uncommitted changes)
fresh-start.bat
```

### During Development
1. Make your changes
2. Test the server (this may generate runtime files)
3. Run cleanup before committing:
   ```bash
   cleanup.bat
   verify.bat
   ```

### Before Publishing/Releasing
1. Run full cleanup: `cleanup.bat`
2. Verify clean state: `verify.bat`
3. Ensure only intended files remain

## Scripts

### verify.bat / scripts/verify-baseline.ps1
- Compares current state against baseline commit
- Shows all files that differ from clean state
- Returns exit code 0 if clean, 1 if dirty

### cleanup.bat / scripts/cleanup.ps1
- Automatically removes common artifacts:
  - Build files (bin/, obj/, *.dll, *.exe, *.pdb)
  - Log files (*.log, Logs/)
  - Runtime data (Saves/, Accounts/, Backups/)
  - Temp files (*.tmp, *.bak, *.swp)
- Asks for confirmation before removing
- Shows manual review items for files it can't auto-remove

### fresh-start.bat
- **DANGER**: Resets repository to baseline state
- Deletes all uncommitted changes
- Use only when you want to start completely fresh

## File Categories

The cleanup system categorizes extra files:

- **Build Artifacts**: Compiler outputs, should always be cleaned
- **Logs**: Server logs, cleaned between sessions
- **Runtime Data**: Save files, accounts, cleaned for fresh installs
- **Temp Files**: Temporary files, safe to remove
- **Other Files**: Require manual review

## Best Practices

1. **Always verify before committing**: `verify.bat`
2. **Clean before testing**: `cleanup.bat` then test
3. **Clean before publishing**: Ensure only intended files ship
4. **Use fresh-start sparingly**: Only when you need a guaranteed clean state
5. **Review "other files"**: Don't blindly commit unknown files

## Troubleshooting

### Script won't run
- Ensure PowerShell execution policy allows scripts
- Run: `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser`

### Files not being cleaned
- Check if files are in .gitignore (they won't be tracked)
- Some files may need manual removal
- Update cleanup patterns if new file types are identified

### Baseline commit changed
- Update the `$BaselineCommit` variable in scripts
- Update batch files if needed

## Integration with CI/CD

For automated builds, you can:
1. Run `verify.bat` in CI to ensure clean commits
2. Run `cleanup.bat` before packaging releases
3. Use the baseline commit as a reference for clean builds

## Maintenance

As the project evolves:
- Update cleanup patterns for new file types
- Update baseline commit when core integration changes
- Review and update .gitignore as needed
- Test cleanup scripts after major changes
