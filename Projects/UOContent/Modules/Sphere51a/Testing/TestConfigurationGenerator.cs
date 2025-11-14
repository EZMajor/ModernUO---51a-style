using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Server.Logging;

namespace Server.Modules.Sphere51a.Testing;

/// <summary>
/// Auto-generates all configuration files needed for test shard execution.
/// Creates isolated configs with Sphere51a enabled and test-optimized settings.
/// </summary>
public static class TestConfigurationGenerator
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TestConfigurationGenerator));

    /// <summary>
    /// Generates all required configuration files for a test shard.
    /// </summary>
    public static void GenerateAll(string testShardPath)
    {
        logger.Information("Generating test configurations for: {Path}", testShardPath);

        var configDir = Path.Combine(testShardPath, "Configuration");
        Directory.CreateDirectory(configDir);

        // Generate core ModernUO configuration
        GenerateModernUOJson(configDir);

        // Generate expansion configuration
        GenerateExpansionJson(configDir);

        // Generate Sphere51a-specific configurations
        GenerateSphere51aJson(configDir);

        // Generate accounts configuration
        GenerateAccountsXml(configDir);

        // Generate additional test configs
        GenerateCombatJson(configDir);
        GenerateMapsJson(configDir);
        GenerateThrottlesJson(configDir);

        logger.Information("All test configurations generated successfully");
    }

    /// <summary>
    /// Generates modernuo.json with test-optimized settings.
    /// </summary>
    private static void GenerateModernUOJson(string configDir)
    {
        var uoPath = UOPathResolver.ResolveUOPath();

        var config = new
        {
            assemblyDirectories = new[] { "./Assemblies" },
            dataDirectories = new[] { "./Data" },
            listeners = new[] { "127.0.0.1:27001" }, // Test game port
            settings = new Dictionary<string, string>
            {
                // Core settings
                ["uoFilesDirectory"] = uoPath,
                ["expansion"] = "EJ", // Endless Journey
                ["clientExpansion"] = "11", // Endless Journey

                // Sphere51a integration
                ["sphere51a.enabled"] = "true",

                // Test-optimized settings (disable unnecessary features)
                ["accountHandler.enableAutoAccountCreation"] = "true",
                ["core.enableIdleCPU"] = "false",
                ["pingServer.enabled"] = "false",
                ["crashGuard.enabled"] = "true",
                ["autosave.enabled"] = "false", // No saves in tests
                ["console.enabled"] = "true",

                // Network settings for testing
                ["network.maxConnections"] = "10",
                ["network.receiveBufferSize"] = "8192",
                ["network.sendBufferSize"] = "8192",

                // Logging (minimal for tests)
                ["logger.level"] = "Warning",
                ["logger.enableConsole"] = "true",
                ["logger.enableFile"] = "false",

                // Timer settings
                ["timer.maxConcurrentThreads"] = "2",
                ["timer.threadCount"] = "1",

                // World settings (minimal)
                ["world.maxUpdateRange"] = "12",
                ["world.saveOnExit"] = "false"
            }
        };

        var path = Path.Combine(configDir, "modernuo.json");
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
        logger.Debug("Generated modernuo.json with UO path: {Path}", uoPath);
    }

    /// <summary>
    /// Generates expansion.json for test environment.
    /// </summary>
    private static void GenerateExpansionJson(string configDir)
    {
        var config = new
        {
            id = 11, // Endless Journey
            name = "Endless Journey",
            enabledMaps = new[]
            {
                "Felucca",
                "Trammel",
                "Ilshenar",
                "Malas",
                "Tokuno",
                "TerMur"
            }
        };

        var path = Path.Combine(configDir, "expansion.json");
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
        logger.Debug("Generated expansion.json");
    }

    /// <summary>
    /// Generates sphere51a.json with test-optimized combat settings.
    /// </summary>
    private static void GenerateSphere51aJson(string configDir)
    {
        var config = new
        {
            enabled = true,
            useGlobalPulse = true,
            globalTickMs = 50,
            combatIdleTimeoutMs = 30000,
            independentTimers = true,
            weaponTimingConfigPath = "./Data/Sphere51a/weapons_timing.json",
            audit = new
            {
                enabled = true,
                level = "Detailed",
                bufferSize = 1000,
                flushIntervalMs = 5000
            }
        };

        var path = Path.Combine(configDir, "sphere51a.json");
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
        logger.Debug("Generated sphere51a.json");
    }

    /// <summary>
    /// Generates accounts.xml with test admin account.
    /// </summary>
    private static void GenerateAccountsXml(string configDir)
    {
        var accountsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<accounts>
  <account username=""Admin"" password=""Admin"" accesslevel=""Administrator"">
    <characters />
  </account>
</accounts>";

        var path = Path.Combine(configDir, "accounts.xml");
        File.WriteAllText(path, accountsXml);
        logger.Debug("Generated accounts.xml with admin account");
    }

    /// <summary>
    /// Generates combat.json with test combat settings.
    /// </summary>
    private static void GenerateCombatJson(string configDir)
    {
        var config = new
        {
            sphereStyleCombat = true,
            weaponDamage = new
            {
                enabled = true,
                baseDamageModifier = 1.0
            },
            spellDamage = new
            {
                enabled = true,
                baseDamageModifier = 1.0
            },
            healing = new
            {
                enabled = true,
                bandageSpeedModifier = 1.0
            }
        };

        var path = Path.Combine(configDir, "combat.json");
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
        logger.Debug("Generated combat.json");
    }

    /// <summary>
    /// Generates maps.json with all maps enabled for testing.
    /// </summary>
    private static void GenerateMapsJson(string configDir)
    {
        var config = new
        {
            maps = new[]
            {
                new { id = 0, name = "Felucca", enabled = true },
                new { id = 1, name = "Trammel", enabled = true },
                new { id = 2, name = "Ilshenar", enabled = true },
                new { id = 3, name = "Malas", enabled = true },
                new { id = 4, name = "Tokuno", enabled = true },
                new { id = 5, name = "TerMur", enabled = true }
            }
        };

        var path = Path.Combine(configDir, "maps.json");
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
        logger.Debug("Generated maps.json");
    }

    /// <summary>
    /// Generates throttles.json with relaxed settings for testing.
    /// </summary>
    private static void GenerateThrottlesJson(string configDir)
    {
        var config = new
        {
            packetThrottles = new[]
            {
                new { packetId = 0x02, throttle = 0 }, // Movement
                new { packetId = 0x05, throttle = 0 }, // Attack
                new { packetId = 0x12, throttle = 0 }, // Combat actions
                new { packetId = 0xBF, throttle = 0 }  // Generic commands
            },
            actionThrottles = new
            {
                movementThrottle = 0,
                combatThrottle = 0,
                spellThrottle = 0,
                bandageThrottle = 0
            }
        };

        var path = Path.Combine(configDir, "throttles.json");
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
        logger.Debug("Generated throttles.json");
    }
}
