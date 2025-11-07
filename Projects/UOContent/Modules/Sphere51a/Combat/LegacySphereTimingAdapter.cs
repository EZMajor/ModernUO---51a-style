/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: LegacySphereTimingAdapter.cs
 *
 * Description: Adapter for legacy Sphere timing systems.
 *              Translates existing timing tables to ITimingProvider interface.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Items;
using Server.Logging;
using Server.Mobiles;
using Server.Modules.Sphere51a.Configuration;

namespace Server.Modules.Sphere51a.Combat;

/// <summary>
/// Adapter that translates legacy Sphere timing systems to the ITimingProvider interface.
/// Allows fallback to existing timing calculations during migration.
/// </summary>
public class LegacySphereTimingAdapter : ITimingProvider
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(LegacySphereTimingAdapter));

    /// <summary>
    /// Provider name for logging/debugging.
    /// </summary>
    public string ProviderName => "LegacySphereTimingAdapter";

    /// <summary>
    /// Gets the attack interval in milliseconds using legacy Sphere calculations.
    /// </summary>
    /// <param name="attacker">The mobile attacking</param>
    /// <param name="weapon">The weapon being used</param>
    /// <returns>Attack interval in milliseconds</returns>
    public int GetAttackIntervalMs(Mobile attacker, Item weapon)
    {
        if (attacker == null)
            return 700; // Minimum

        try
        {
            // Use our own Sphere51a timing calculation
            if (weapon is BaseWeapon baseWeapon)
            {
                // Get delay using Sphere51a calculation
                var delay = baseWeapon.GetDelay(attacker);
                return (int)delay.TotalMilliseconds;
            }

            return 1500; // Default fallback
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Failed to get attack interval for {attacker.Name}, using fallback");
            return 1500; // 1.5 second fallback
        }
    }

    /// <summary>
    /// Gets the animation hit offset using legacy defaults.
    /// </summary>
    /// <param name="weapon">The weapon being used</param>
    /// <returns>Animation hit offset in milliseconds</returns>
    public int GetAnimationHitOffsetMs(Item weapon)
    {
        // Use weapon class defaults from WeaponEntry
        if (weapon is BaseWeapon baseWeapon)
        {
            return GetWeaponClassDefaultOffset(baseWeapon);
        }

        return WeaponEntry.Default.AnimationHitOffsetMs;
    }

    /// <summary>
    /// Gets the animation duration using legacy defaults.
    /// </summary>
    /// <param name="weapon">The weapon being used</param>
    /// <returns>Animation duration in milliseconds</returns>
    public int GetAnimationDurationMs(Item weapon)
    {
        // Use weapon class defaults from WeaponEntry
        if (weapon is BaseWeapon baseWeapon)
        {
            return GetWeaponClassDefaultDuration(baseWeapon);
        }

        return WeaponEntry.Default.AnimationDurationMs;
    }

    /// <summary>
    /// Gets default animation hit offset based on weapon class.
    /// </summary>
    /// <param name="weapon">The weapon</param>
    /// <returns>Hit offset in milliseconds</returns>
    private int GetWeaponClassDefaultOffset(BaseWeapon weapon)
    {
        // Determine weapon class and return appropriate offset
        if (weapon is BaseRanged)
        {
            // Check if crossbow (slower) or bow
            return weapon is HeavyCrossbow || weapon is Crossbow || weapon is RepeatingCrossbow
                ? WeaponEntry.Defaults.Crossbow.AnimationHitOffsetMs
                : WeaponEntry.Defaults.Bow.AnimationHitOffsetMs;
        }

        // Two-handed weapons
        if (weapon.Layer == Layer.TwoHanded)
        {
            return WeaponEntry.Defaults.TwoHanded.AnimationHitOffsetMs;
        }

        // One-handed weapons - check animation for more precise classification
        var animation = weapon.Animation;
        if (animation == WeaponAnimation.Slash1H || animation == WeaponAnimation.Pierce1H ||
            animation == WeaponAnimation.Bash1H || animation == WeaponAnimation.Wrestle)
        {
            return WeaponEntry.Defaults.OneHandedSword.AnimationHitOffsetMs;
        }

        // Daggers and fast weapons
        var weaponType = weapon.Type;
        if (weaponType == WeaponType.Piercing)
        {
            return WeaponEntry.Defaults.Dagger.AnimationHitOffsetMs;
        }

        // Default to one-handed
        return WeaponEntry.Defaults.OneHandedSword.AnimationHitOffsetMs;
    }

    /// <summary>
    /// Gets default animation duration based on weapon class.
    /// </summary>
    /// <param name="weapon">The weapon</param>
    /// <returns>Duration in milliseconds</returns>
    private int GetWeaponClassDefaultDuration(BaseWeapon weapon)
    {
        // Determine weapon class and return appropriate duration
        if (weapon is BaseRanged)
        {
            // Check if crossbow (slower) or bow
            return weapon is HeavyCrossbow || weapon is Crossbow || weapon is RepeatingCrossbow
                ? WeaponEntry.Defaults.Crossbow.AnimationDurationMs
                : WeaponEntry.Defaults.Bow.AnimationDurationMs;
        }

        // Two-handed weapons
        if (weapon.Layer == Layer.TwoHanded)
        {
            return WeaponEntry.Defaults.TwoHanded.AnimationDurationMs;
        }

        // One-handed weapons - check animation for more precise classification
        var animation = weapon.Animation;
        if (animation == WeaponAnimation.Slash1H || animation == WeaponAnimation.Pierce1H ||
            animation == WeaponAnimation.Bash1H || animation == WeaponAnimation.Wrestle)
        {
            return WeaponEntry.Defaults.OneHandedSword.AnimationDurationMs;
        }

        // Daggers and fast weapons
        var weaponType = weapon.Type;
        if (weaponType == WeaponType.Piercing)
        {
            return WeaponEntry.Defaults.Dagger.AnimationDurationMs;
        }

        // Default to one-handed
        return WeaponEntry.Defaults.OneHandedSword.AnimationDurationMs;
    }
}
