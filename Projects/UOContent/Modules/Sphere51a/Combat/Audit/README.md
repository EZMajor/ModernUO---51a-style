# Sphere 51a Combat Audit System

The Sphere 51a Combat Audit System provides comprehensive monitoring and logging for combat actions in ModernUO servers. It tracks timing accuracy, validates combat mechanics, and maintains persistent audit trails for PvP fairness and debugging purposes.

## Features

- **Real-time Combat Tracking**: Monitors weapon swings, spell casts, bandage usage, and wand activations
- **Timing Validation**: Records expected vs. actual delays with variance analysis
- **Configurable Audit Levels**: Four levels (None, Standard, Detailed, Debug) for granular control
- **Shadow Mode Verification**: Compares timing providers without affecting gameplay
- **Performance Monitoring**: Automatic throttling to maintain server performance
- **Persistent Logging**: JSONL-formatted logs with configurable retention
- **Query API**: Retrieve combat history for specific mobiles or current buffer state

## Configuration

The audit system is configured via an `AuditConfig` instance with the following properties:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `enabled` | bool | true | Enables or disables the audit system |
| `level` | AuditLevel | Standard | Audit granularity level |
| `outputDirectory` | string | "Logs/CombatAudit" | Directory for log files |
| `bufferSize` | int | 10000 | Maximum entries in memory buffer |
| `flushIntervalMs` | int | 5000 | Buffer flush interval in milliseconds |
| `enableShadowMode` | bool | false | Enable shadow mode timing comparison |
| `retentionDays` | int | 7 | Log file retention period |
| `maxFileSizeMB` | int | 100 | Maximum log file size |
| `maxEntriesPerTick` | int | 250 | Maximum entries processed per tick |
| `autoThrottleThresholdMs` | double | 10.0 | Performance threshold for auto-throttling |
| `enableWeaponMetrics` | bool | true | Enable weapon performance metrics |
| `enableMobileHistory` | bool | true | Enable per-mobile action history |
| `mobileHistorySize` | int | 100 | Maximum history entries per mobile |

### Configuration Example

```json
{
  "enabled": true,
  "level": "Detailed",
  "outputDirectory": "Logs/CombatAudit",
  "bufferSize": 5000,
  "flushIntervalMs": 3000,
  "enableShadowMode": true,
  "retentionDays": 14,
  "autoThrottleThresholdMs": 15.0
}
```

## Usage

### Initialization

```csharp
var config = new AuditConfig
{
    Level = AuditLevel.Detailed,
    EnableShadowMode = true
};

CombatAuditSystem.Initialize(config);
```

### Shutdown

```csharp
CombatAuditSystem.Shutdown();
```

### Querying History

```csharp
// Get recent history for a mobile
var history = CombatAuditSystem.GetMobileHistory(mobile, TimeSpan.FromMinutes(5));

// Get current buffer snapshot
var buffer = CombatAuditSystem.GetBufferSnapshot();
```

## Log Format

Audit entries are stored as JSON Lines (JSONL) with the following structure:

```json
{
  "timestamp": 1234567890,
  "serial": "0x12345678",
  "name": "PlayerName",
  "actionType": "SwingStart",
  "expectedDelayMs": 1500.0,
  "actualDelayMs": 1520.0,
  "varianceMs": 20.0,
  "weaponId": 3937,
  "weaponName": "Katana",
  "dexterity": 85,
  "details": {
    "Defender": "0x87654321",
    "Cancelled": false
  },
  "auditLevel": "Standard"
}
```

### Action Types

- `SwingStart` / `SwingComplete`: Weapon attack timing
- `SpellCastStart` / `SpellCastComplete`: Spell casting timing
- `BandageStart` / `BandageComplete`: Bandage usage timing
- `WandStart` / `WandComplete`: Wand activation timing
- `ShadowComparison`: Shadow mode provider comparison

## Shadow Mode

When enabled, shadow mode executes multiple timing providers in parallel to validate accuracy without affecting gameplay. It compares results between the current `WeaponTimingProvider` and legacy Sphere timing calculations.

### Shadow Mode Features

- Automatic discrepancy detection (>10ms variance logged)
- Statistical reporting with per-weapon breakdowns
- CSV export functionality
- Memory-efficient comparison storage

### Generating Reports

```csharp
var report = ShadowModeVerifier.GenerateReport(TimeSpan.FromHours(1));
Console.WriteLine($"Total comparisons: {report.TotalComparisons}");
Console.WriteLine($"Average variance: {report.AvgVarianceMs:F2}ms");
Console.WriteLine($"Discrepancy rate: {report.DiscrepancyPercentage:F1}%");
```

## Performance Considerations

- **Memory Usage**: Buffer size and mobile history directly impact RAM consumption
- **Disk I/O**: Frequent flushing increases storage load; adjust intervals based on server capacity
- **CPU Overhead**: Higher audit levels and shadow mode increase processing requirements
- **Auto-Throttling**: Automatically reduces audit level when tick times exceed threshold
- **Retention Management**: Configure appropriate retention periods to prevent disk space issues

## API Reference

### CombatAuditSystem

- `Initialize(AuditConfig config)`: Initialize the audit system
- `Shutdown()`: Shutdown and flush remaining entries
- `GetMobileHistory(Mobile mobile, TimeSpan? window)`: Retrieve mobile combat history
- `GetBufferSnapshot()`: Get current buffer contents
- `FlushBuffer()`: Manually flush buffer to disk
- `CheckPerformanceThrottle(double tickTimeMs)`: Update throttling based on performance

### Properties

- `IsInitialized`: Whether the system is active
- `Config`: Current configuration
- `EffectiveLevel`: Current audit level (may be throttled)
- `BufferCount`: Number of entries in memory buffer
- `TotalEntriesRecorded`: Lifetime entry count
- `TotalEntriesFlushed`: Entries written to disk

### ShadowModeVerifier

- `GenerateReport(TimeSpan? window)`: Create statistical report
- `ExportToCSV(string filename)`: Export comparisons to CSV
- `GetRecentComparisons(TimeSpan? window)`: Retrieve recent comparisons
- `Clear()`: Clear stored comparisons

## Integration

The audit system integrates with Sphere combat events and should be initialized during server startup. It automatically registers event handlers for combat actions and requires no additional integration code for basic operation.
