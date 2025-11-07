/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: WeaponEntry.cs
 *
 * Description: Weapon timing configuration entry for Sphere 51a system.
 *              Defines timing parameters for individual weapon types.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;

namespace Server.Modules.Sphere51a.Combat;

/// <summary>
/// Configuration entry for a weapon's timing parameters.
/// Used by WeaponTimingProvider for attack interval calculations.
/// </summary>
public class WeaponEntry
{
    /// <summary>
    /// Item ID of the weapon.
    /// </summary>
    public int ItemID { get; set; }

    /// <summary>
    /// Weapon speed value (represents 1/100th seconds, e.g. 25 = 0.25 seconds).
    /// Lower values = faster weapons.
    /// </summary>
    public int WeaponSpeedValue { get; set; }

    /// <summary>
    /// Base weapon milliseconds (one-handed: 1600, two-handed: 1900, bow: 2000).
    /// Kept for compatibility but not used in timing calculations.
    /// </summary>
    public int WeaponBaseMs { get; set; }

    /// <summary>
    /// Animation hit offset in milliseconds (when damage applies during swing).
    /// </summary>
    public int AnimationHitOffsetMs { get; set; }

    /// <summary>
    /// Total animation duration in milliseconds.
    /// </summary>
    public int AnimationDurationMs { get; set; }

    /// <summary>
    /// Weapon name for debugging/logging.
    /// </summary>
    public string Name { get; set; }

    public int BaseDelay
    {
        get => WeaponBaseMs;
        set => WeaponBaseMs = value;
    }

    public double SkillBonus { get; set; }

    public string WeaponTypeName { get; set; }

    public int GetDelay(double skill)
    {
        return WeaponBaseMs - (int)(SkillBonus * skill / 100);
    }

    /// <summary>
    /// Default weapon entry for unknown weapons.
    /// </summary>
    public static WeaponEntry Default => new()
    {
        ItemID = -1,
        WeaponSpeedValue = 50,
        WeaponBaseMs = 1600, // One-handed default
        AnimationHitOffsetMs = 300, // 1H sword default
        AnimationDurationMs = 600, // 1H sword default
        Name = "Default"
    };

    /// <summary>
    /// Creates weapon entries for common weapon classes with default timing values.
    /// WeaponSpeedValue: higher = slower weapon.
    /// </summary>
    public static class Defaults
    {
        // Fast weapons: 0.8-1.0 seconds
        public static WeaponEntry Dagger => new()
        {
            ItemID = -1,
            WeaponSpeedValue = 20,
            WeaponBaseMs = 1600,
            AnimationHitOffsetMs = 200,
            AnimationDurationMs = 400,
            Name = "Dagger"
        };

        // Medium weapons: 1.2-1.5 seconds
        public static WeaponEntry OneHandedSword => new()
        {
            ItemID = -1,
            WeaponSpeedValue = 35,
            WeaponBaseMs = 1600,
            AnimationHitOffsetMs = 300,
            AnimationDurationMs = 600,
            Name = "OneHandedSword"
        };

        // Slow weapons: 2.5-3.5 seconds
        public static WeaponEntry TwoHanded => new()
        {
            ItemID = -1,
            WeaponSpeedValue = 75,
            WeaponBaseMs = 1900,
            AnimationHitOffsetMs = 400,
            AnimationDurationMs = 800,
            Name = "TwoHanded"
        };

        // Ranged weapons: 1.8-2.0 seconds
        public static WeaponEntry Bow => new()
        {
            ItemID = -1,
            WeaponSpeedValue = 45,
            WeaponBaseMs = 2000,
            AnimationHitOffsetMs = 500,
            AnimationDurationMs = 900,
            Name = "Bow"
        };

        // Ranged weapons: 2.0-2.2 seconds
        public static WeaponEntry Crossbow => new()
        {
            ItemID = -1,
            WeaponSpeedValue = 50,
            WeaponBaseMs = 2000,
            AnimationHitOffsetMs = 600,
            AnimationDurationMs = 1100,
            Name = "Crossbow"
        };
    }
}
