/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: ModuleConfig.cs
 *
 * Description: Module-local configuration management.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.IO;
using System.Text.Json;
using Server.Logging;

namespace Server.Modules.Sphere51a.Core;

public static class ModuleConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ModuleConfig));

    /// <summary>
    /// Path to the configuration file.
    /// </summary>
    public static readonly string ConfigPath = Path.Combine(
        "Projects", "UOContent", "Modules", "Sphere51a", "Configuration", "config.json"
    );

    private static Sphere51aConfig _config;

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    public static Sphere51aConfig Config => _config ??= LoadConfig();

    /// <summary>
    /// Loads configuration from file, with defaults if file doesn't exist.
    /// </summary>
    public static Sphere51aConfig LoadConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<Sphere51aConfig>(json);

                if (config != null)
                {
                    logger.Information("Loaded Sphere51a configuration from file");
                    return config;
                }
            }

            // Create default config
            var defaultConfig = new Sphere51aConfig();
            SaveConfig(defaultConfig);
            logger.Information("Created default Sphere51a configuration");
            return defaultConfig;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to load Sphere51a configuration, using defaults");
            return new Sphere51aConfig();
        }
    }

    /// <summary>
    /// Saves configuration to file.
    /// </summary>
    public static void SaveConfig(Sphere51aConfig config)
    {
        try
        {
            var directory = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(ConfigPath, json);
            logger.Debug("Saved Sphere51a configuration to file");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to save Sphere51a configuration");
        }
    }

    /// <summary>
    /// Migrates settings from global ServerConfiguration if available.
    /// </summary>
    public static void MigrateFromGlobalConfig()
    {
        try
        {
            // Try to get values from global config
            var enabled = Server.ServerConfiguration.GetSetting("sphere.enableSphere51aStyle", false);
            var useGlobalPulse = Server.ServerConfiguration.GetSetting("sphere.useGlobalPulse", true);
            var globalTickMs = Server.ServerConfiguration.GetSetting("sphere.globalTickMs", 50);
            var idleTimeoutMs = Server.ServerConfiguration.GetSetting("sphere.combatIdleTimeoutMs", 30000);
            var independentTimers = Server.ServerConfiguration.GetSetting("sphere.independentTimers", false);
            var weaponConfigPath = Server.ServerConfiguration.GetSetting("sphere.weaponTimingConfigPath", "Data/Sphere51a/weapons_timing.json");

            var config = new Sphere51aConfig
            {
                Enabled = enabled,
                UseGlobalPulse = useGlobalPulse,
                GlobalTickMs = globalTickMs,
                CombatIdleTimeoutMs = idleTimeoutMs,
                IndependentTimers = independentTimers,
                WeaponTimingConfigPath = weaponConfigPath
            };

            SaveConfig(config);
            logger.Information("Migrated Sphere51a settings from global configuration");
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to migrate from global configuration");
        }
    }
}

/// <summary>
/// Configuration class for Sphere51a module.
/// </summary>
public class Sphere51aConfig
{
    public bool Enabled { get; set; } = false;
    public bool UseGlobalPulse { get; set; } = true;
    public int GlobalTickMs { get; set; } = 50;
    public int CombatIdleTimeoutMs { get; set; } = 30000;
    public bool IndependentTimers { get; set; } = false;
    public string WeaponTimingConfigPath { get; set; } = "Data/Sphere51a/weapons_timing.json";
}
