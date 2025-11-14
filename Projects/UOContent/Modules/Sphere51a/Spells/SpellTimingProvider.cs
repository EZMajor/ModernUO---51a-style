/*************************************************************************
 * ModernUO - Sphere 51a Spell Timing Provider
 * File: SpellTimingProvider.cs
 *
 * Description: Provides spell timing calculations for Sphere51a system.
 *              Loads timing data from JSON configuration.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using Server.Logging;
using Server.Spells;

namespace Server.Modules.Sphere51a.Spells;

/// <summary>
/// Provides spell timing calculations for the Sphere51a combat system.
/// Loads timing data from JSON configuration with canonical Sphere51a values.
/// </summary>
public static class SpellTimingProvider
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SpellTimingProvider));

    private static readonly Dictionary<string, SpellTimingData> _spellTimings = new();
    private static bool _initialized;

    /// <summary>
    /// Gets whether the spell timing provider is initialized.
    /// </summary>
    public static bool IsInitialized => _initialized;

    /// <summary>
    /// Initializes the spell timing provider.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;

        try
        {
            LoadSpellTimings();
            _initialized = true;
            logger.Information("Spell timing provider initialized with {Count} spell timings", _spellTimings.Count);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to initialize spell timing provider");
        }
    }

    /// <summary>
    /// Gets the cast delay for a spell in milliseconds.
    /// </summary>
    /// <param name="spell">The spell to get timing for</param>
    /// <param name="skillValue">Caster's skill value</param>
    /// <param name="fromScroll">Whether casting from a scroll</param>
    /// <returns>Cast delay in milliseconds, or 0 if not found</returns>
    public static int GetCastDelay(Spell spell, double skillValue, bool fromScroll)
    {
        if (!_initialized || spell == null)
            return 0;

        var spellName = spell.GetType().Name.Replace("Spell", "");
        var spellKey = spellName.ToLowerInvariant();

        if (_spellTimings.TryGetValue(spellKey, out var timing))
        {
            return timing.CalculateDelay(skillValue, fromScroll);
        }

        // Log unknown spells for debugging
        logger.Debug("Unknown spell timing requested: {Spell}", spellName);
        return 0;
    }

    /// <summary>
    /// Loads spell timing data from JSON configuration.
    /// </summary>
    private static void LoadSpellTimings()
    {
        var configPath = Path.Combine(
            Server.Core.BaseDirectory,
            "Data",
            "Sphere51a",
            "spell_timing.json"
        );

        // Load canonical Sphere51a timing data if config doesn't exist
        if (!File.Exists(configPath))
        {
            LoadDefaultTimings();
            SaveDefaultTimings(configPath);
            return;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var wrapper = System.Text.Json.JsonSerializer.Deserialize<SpellTimingWrapper>(json, options);

            if (wrapper?.Spells != null && wrapper.Spells.Count > 0)
            {
                _spellTimings.Clear();

                foreach (var entry in wrapper.Spells)
                {
                    if (string.IsNullOrWhiteSpace(entry.Name))
                        continue;

                    // Use case-insensitive spell name as key
                    var spellKey = entry.Name.ToLowerInvariant();

                    // Map JSON properties to SpellTimingData
                    _spellTimings[spellKey] = new SpellTimingData
                    {
                        BaseDelayMs = entry.CastDelayMs,
                        // Store scroll delay for future use (currently handled in CalculateDelay via fromScroll parameter)
                        // ScrollDelayMs would go here if we add it to SpellTimingData
                    };

                    logger.Debug("Loaded spell timing: {Spell} = {Delay}ms (scroll: {ScrollDelay}ms)",
                        entry.Name, entry.CastDelayMs, entry.ScrollCastDelayMs);
                }

                logger.Information("Loaded {Count} spell timings from {Path}", _spellTimings.Count, configPath);
            }
            else
            {
                logger.Warning("Failed to parse spell timing JSON (no spells array found), using defaults");
                LoadDefaultTimings();
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error loading spell timings from {Path}, using defaults", configPath);
            LoadDefaultTimings();
        }
    }

    /// <summary>
    /// Loads canonical Sphere51a spell timing defaults.
    /// </summary>
    private static void LoadDefaultTimings()
    {
        _spellTimings.Clear();

        // Canonical Sphere51a delayed spell timings
        _spellTimings["Explosion"] = new SpellTimingData
        {
            BaseDelayMs = 2500,
            PerTileDelayMs = 100,
            MaxDelayMs = 5000
        };

        _spellTimings["ChainLightning"] = new SpellTimingData
        {
            BaseDelayMs = 1800,
            PerTargetDelayMs = 200,
            MaxDelayMs = 4000
        };

        _spellTimings["MeteorSwarm"] = new SpellTimingData
        {
            BaseDelayMs = 2500,
            PerTileDelayMs = 150,
            MaxDelayMs = 6000
        };

        _spellTimings["EnergyField"] = new SpellTimingData
        {
            BaseDelayMs = 1800
        };

        // Add more spells as needed
        _spellTimings["FireField"] = new SpellTimingData
        {
            BaseDelayMs = 1800
        };

        _spellTimings["PoisonField"] = new SpellTimingData
        {
            BaseDelayMs = 1800
        };

        _spellTimings["ParalyzeField"] = new SpellTimingData
        {
            BaseDelayMs = 1800
        };

        logger.Information("Loaded default Sphere51a spell timings");
    }

    /// <summary>
    /// Saves the default timings to a JSON file for customization.
    /// </summary>
    private static void SaveDefaultTimings(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = System.Text.Json.JsonSerializer.Serialize(_spellTimings, options);
            File.WriteAllText(path, json);

            logger.Information("Saved default spell timings to {Path}", path);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to save default spell timings");
        }
    }

    /// <summary>
    /// JSON deserialization wrapper for spell timing configuration.
    /// </summary>
    private class SpellTimingWrapper
    {
        public List<SpellTimingJsonEntry> Spells { get; set; } = new();
    }

    /// <summary>
    /// JSON entry for individual spell timing data.
    /// </summary>
    private class SpellTimingJsonEntry
    {
        public string Name { get; set; } = "";
        public int CastDelayMs { get; set; }
        public int ScrollCastDelayMs { get; set; }
        public int ManaCost { get; set; }
        public int SpellId { get; set; }
        public int Circle { get; set; }
    }

    /// <summary>
    /// Spell timing data structure.
    /// </summary>
    public class SpellTimingData
    {
        /// <summary>
        /// Base delay in milliseconds.
        /// </summary>
        public int BaseDelayMs { get; set; }

        /// <summary>
        /// Additional delay per tile (for area spells).
        /// </summary>
        public int PerTileDelayMs { get; set; }

        /// <summary>
        /// Additional delay per target (for multi-target spells).
        /// </summary>
        public int PerTargetDelayMs { get; set; }

        /// <summary>
        /// Maximum delay in milliseconds.
        /// </summary>
        public int MaxDelayMs { get; set; }

        /// <summary>
        /// Calculates the actual delay based on parameters.
        /// </summary>
        public int CalculateDelay(double skillValue, bool fromScroll, int tiles = 0, int targets = 0)
        {
            var delay = BaseDelayMs;

            // Add tile-based delay
            if (PerTileDelayMs > 0 && tiles > 0)
            {
                delay += PerTileDelayMs * tiles;
            }

            // Add target-based delay
            if (PerTargetDelayMs > 0 && targets > 1)
            {
                delay += PerTargetDelayMs * (targets - 1);
            }

            // Apply skill-based reduction (basic implementation)
            if (!fromScroll && skillValue > 0)
            {
                var skillReduction = Math.Min(skillValue / 10.0, 0.5); // Max 50% reduction
                delay = (int)(delay * (1.0 - skillReduction));
            }

            // Apply maximum delay cap
            if (MaxDelayMs > 0 && delay > MaxDelayMs)
            {
                delay = MaxDelayMs;
            }

            return Math.Max(delay, 0);
        }
    }
}
