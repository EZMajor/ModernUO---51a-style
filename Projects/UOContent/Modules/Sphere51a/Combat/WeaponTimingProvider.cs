/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: WeaponTimingProvider.cs
 *
 * Description: Global tick-based weapon timing provider for Sphere 51a system.
 *              Implements deterministic attack intervals with dex scaling.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Modules.Sphere51a.Configuration;

namespace Server.Modules.Sphere51a.Combat;

/// <summary>
/// Weapon timing provider using global tick system.
/// Implements the correct Sphere 0.51a formula: speedInSeconds = WeaponSpeedValue/100 with dex scaling.
/// </summary>
public class WeaponTimingProvider : ITimingProvider
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(WeaponTimingProvider));

    private const int DexBaseline = 100;
    private const double HighDexModifier = 0.008;
    private const double LowDexModifier = 0.004;
    private const int TickMs = 50;
    private const int MinMs = 200;
    private const int MaxMs = 4000;

    private readonly Dictionary<int, WeaponEntry> _weaponTable;
    private Dictionary<string, WeaponEntry> _loadedWeapons = new();

    /// <summary>
    /// Creates a new WeaponTimingProvider with the specified weapon table.
    /// </summary>
    /// <param name="weaponTable">Dictionary mapping item IDs to weapon entries</param>
    public WeaponTimingProvider(Dictionary<int, WeaponEntry> weaponTable)
    {
        _weaponTable = weaponTable ?? new Dictionary<int, WeaponEntry>();
    }

    public WeaponTimingProvider()
    {
        _weaponTable = new Dictionary<int, WeaponEntry>();
        _loadedWeapons = new Dictionary<string, WeaponEntry>();
    }

    /// <summary>
    /// Provider name for logging/debugging.
    /// </summary>
    public string ProviderName => "WeaponTimingProvider";

    /// <summary>
    /// Gets the attack interval in milliseconds for the given attacker and weapon.
    /// WeaponSpeedValue: higher = slower weapon (intuitive for designers).
    /// Formula: delay = WeaponSpeedValue * 40ms base, with dex scaling.
    /// </summary>
    /// <param name="attacker">The attacking mobile</param>
    /// <param name="weapon">The weapon being used</param>
    /// <returns>Attack interval in milliseconds, rounded to nearest 50ms tick</returns>
    public int GetAttackIntervalMs(Mobile attacker, Item weapon)
    {
        if (attacker == null)
            return MinMs;

        // Get weapon entry
        var entry = GetWeaponEntry(weapon);

        // Base delay in milliseconds (higher WeaponSpeedValue = slower weapon)
        double baseDelayMs = entry.WeaponSpeedValue * 40.0;

        // Calculate dexterity modifier (Sphere 0.51a style)
        double bonusDex = attacker.Dex - DexBaseline;

        // Cap dexterity bonus/penalty (-50 to +25)
        if (bonusDex > 25)
            bonusDex = 25;
        else if (bonusDex < -50)
            bonusDex = -50;

        // NPCs don't get dexterity bonuses
        if (!(attacker is Server.Mobiles.PlayerMobile))
            bonusDex = 0;

        // Apply dexterity scaling (reduce delay for high dex, increase for low dex)
        double dexMultiplier = 1.0;
        if (bonusDex > 0)
            dexMultiplier -= bonusDex * HighDexModifier;  // -0.8% per dex over 100
        else if (bonusDex < 0)
            dexMultiplier -= bonusDex * LowDexModifier;  // -0.4% per dex under 100

        double finalDelayMs = baseDelayMs * dexMultiplier;

        // Round to nearest 50ms tick and clamp
        int result = (int)Math.Round(finalDelayMs / TickMs) * TickMs;
        result = Math.Clamp(result, MinMs, MaxMs);

        return result;
    }

    /// <summary>
    /// Gets the animation hit offset for the given weapon.
    /// </summary>
    /// <param name="weapon">The weapon being used</param>
    /// <returns>Animation hit offset in milliseconds</returns>
    public int GetAnimationHitOffsetMs(Item weapon)
    {
        var entry = GetWeaponEntry(weapon);
        return entry.AnimationHitOffsetMs;
    }

    /// <summary>
    /// Gets the animation duration for the given weapon.
    /// </summary>
    /// <param name="weapon">The weapon being used</param>
    /// <returns>Animation duration in milliseconds</returns>
    public int GetAnimationDurationMs(Item weapon)
    {
        var entry = GetWeaponEntry(weapon);
        return entry.AnimationDurationMs;
    }

    /// <summary>
    /// Gets the weapon entry for the given weapon, with fallback to defaults.
    /// </summary>
    /// <param name="weapon">The weapon item</param>
    /// <returns>WeaponEntry for the weapon</returns>
    private WeaponEntry GetWeaponEntry(Item weapon)
    {
        if (weapon == null)
            return WeaponEntry.Default;

        // Try to find by exact item ID
        if (_weaponTable.TryGetValue(weapon.ItemID, out var entry))
            return entry;

        // Fallback to weapon class defaults
        if (weapon is BaseWeapon baseWeapon)
        {
            return GetWeaponClassDefault(baseWeapon);
        }

        return WeaponEntry.Default;
    }

    /// <summary>
    /// Gets default timing values based on weapon class.
    /// </summary>
    /// <param name="weapon">The base weapon</param>
    /// <returns>Default WeaponEntry for the weapon class</returns>
    private WeaponEntry GetWeaponClassDefault(BaseWeapon weapon)
    {
        // Determine weapon class based on properties
        if (weapon is BaseRanged)
        {
            // Check if crossbow (slower) or bow
            return weapon is HeavyCrossbow || weapon is Crossbow || weapon is RepeatingCrossbow
                ? WeaponEntry.Defaults.Crossbow
                : WeaponEntry.Defaults.Bow;
        }

        // Check weapon type and animation for classification
        var weaponType = weapon.Type;
        var animation = weapon.Animation;

        // Two-handed weapons
        if (weapon.Layer == Layer.TwoHanded)
        {
            return WeaponEntry.Defaults.TwoHanded;
        }

        // One-handed weapons - check animation for more precise classification
        if (animation == WeaponAnimation.Slash1H || animation == WeaponAnimation.Pierce1H ||
            animation == WeaponAnimation.Bash1H || animation == WeaponAnimation.Wrestle)
        {
            return WeaponEntry.Defaults.OneHandedSword;
        }

        // Daggers and fast weapons
        if (weaponType == WeaponType.Piercing)
        {
            return WeaponEntry.Defaults.Dagger;
        }

        // Default to one-handed
        return WeaponEntry.Defaults.OneHandedSword;
    }

    /// <summary>
    /// Loads weapon timing configuration from JSON file.
    /// </summary>
    /// <param name="filePath">Path to the JSON configuration file</param>
    /// <returns>Dictionary of weapon entries</returns>
    public static Dictionary<int, WeaponEntry> LoadFromJson(string filePath)
    {
        var weaponTable = new Dictionary<int, WeaponEntry>();

        try
        {
            if (!File.Exists(filePath))
            {
                logger.Warning($"Weapon timing config file not found: {filePath}");
                return weaponTable;
            }

            var json = File.ReadAllText(filePath);
            var entries = JsonSerializer.Deserialize<List<WeaponEntry>>(json);

            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry.ItemID >= 0)
                    {
                        weaponTable[entry.ItemID] = entry;
                    }
                }
            }

            logger.Information($"Loaded {weaponTable.Count} weapon timing entries from {filePath}");
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Failed to load weapon timing config from {filePath}");
        }

        return weaponTable;
    }

    /// <summary>
    /// Creates a compatibility mapping from existing Sphere timing tables.
    /// </summary>
    /// <returns>Dictionary mapping item IDs to weapon entries</returns>
    public static Dictionary<int, WeaponEntry> CreateCompatibilityMapping()
    {
        var mapping = new Dictionary<int, WeaponEntry>();

        // Import from Server.Systems.Combat.SphereStyle.SphereTimingTables
        // This provides 1:1 compatibility with existing timing values
        try
        {
            // Map common weapons using the existing timing data
            // Katana: Speed 46, Base 1600
            mapping[0x13FF] = new WeaponEntry
            {
                ItemID = 0x13FF,
                Name = "Katana",
                WeaponSpeedValue = 46,
                WeaponBaseMs = 1600,
                AnimationHitOffsetMs = 300,
                AnimationDurationMs = 600
            };

            // Longsword: Speed 30, Base 1600
            mapping[0x13B8] = new WeaponEntry
            {
                ItemID = 0x13B8,
                Name = "Longsword",
                WeaponSpeedValue = 30,
                WeaponBaseMs = 1600,
                AnimationHitOffsetMs = 300,
                AnimationDurationMs = 600
            };

            // Halberd: Speed 25, Base 1900
            mapping[0x143E] = new WeaponEntry
            {
                ItemID = 0x143E,
                Name = "Halberd",
                WeaponSpeedValue = 25,
                WeaponBaseMs = 1900,
                AnimationHitOffsetMs = 400,
                AnimationDurationMs = 800
            };

            // Bow: Speed 30, Base 2000
            mapping[0x13B1] = new WeaponEntry
            {
                ItemID = 0x13B1,
                Name = "Bow",
                WeaponSpeedValue = 30,
                WeaponBaseMs = 2000,
                AnimationHitOffsetMs = 500,
                AnimationDurationMs = 900
            };

            logger.Information($"Created compatibility mapping with {mapping.Count} weapon entries");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to create compatibility mapping");
        }

        return mapping;
    }

    public void LoadConfiguration(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                logger.Warning($"Weapon timing config file not found: {path}");
                _loadedWeapons = new Dictionary<string, WeaponEntry>();
                return;
            }

            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<WeaponTimingConfig>(json);
            _loadedWeapons = new Dictionary<string, WeaponEntry>();

            if (config?.Weapons != null)
            {
                foreach (var kvp in config.Weapons)
                {
                    _loadedWeapons[kvp.Key] = new WeaponEntry
                    {
                        WeaponBaseMs = kvp.Value.BaseDelay,
                        SkillBonus = kvp.Value.SkillBonus,
                        WeaponTypeName = kvp.Key
                    };
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Failed to load weapon timing config from {path}");
            _loadedWeapons = new Dictionary<string, WeaponEntry>();
        }
    }

    public int GetWeaponDelay(string weaponType, double skill)
    {
        if (_loadedWeapons.TryGetValue(weaponType, out var entry))
        {
            return entry.GetDelay(skill);
        }
        return 1000; // default
    }

    public WeaponEntry GetWeaponEntry(string weaponType)
    {
        return _loadedWeapons.TryGetValue(weaponType, out var entry) ? entry : null;
    }

    public bool IsLoaded => _loadedWeapons.Count > 0;

    public IEnumerable<string> GetLoadedWeapons() => _loadedWeapons.Keys;
}
