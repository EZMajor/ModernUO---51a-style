/*************************************************************************
 * ModernUO - Sphere 0.51a Combat System
 * File: SphereBandageHelper.cs
 *
 * Description: Helper methods for integrating Sphere 0.51a bandaging
 *              mechanics. Handles bandage timing, action cancellation,
 *              and independent timer management.
 *
 * Reference: Sphere0.51aCombatSystem.md - Section 2.3 Item and Skill Interaction
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;

namespace Server.Systems.Combat.SphereStyle;

/// <summary>
/// Helper methods for Sphere 0.51a bandaging mechanics.
/// </summary>
/// <remarks>
/// Integrates with Bandage system to provide Sphere-style:
/// - Bandage cancels swing and cast
/// - Independent bandage timer
/// - No interference with other actions (potion use)
/// </remarks>
public static class SphereBandageHelper
{
    /// <summary>
    /// Validates if bandaging can proceed according to Sphere 0.51a rules.
    /// Sphere-style edit: Enhanced bandage validation with Sphere state checks.
    /// </summary>
    /// <param name="healer">The mobile applying the bandage.</param>
    /// <param name="patient">The mobile being healed.</param>
    /// <param name="canBandage">Original canBandage value.</param>
    /// <returns>True if bandaging should proceed; false otherwise.</returns>
    public static bool ValidateBandage(Mobile healer, Mobile patient, bool canBandage)
    {
        if (!SphereConfig.IsEnabled())
            return canBandage; // Use ModernUO default behavior

        // Start with original validation
        if (!canBandage)
            return false;

        // Sphere-style edit: Check Sphere-specific conditions
        if (!healer.SphereCanBandage())
        {
            SphereConfig.DebugLog($"{healer.Name} - Bandage blocked by Sphere state");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Handles bandage initiation according to Sphere 0.51a rules.
    /// Sphere-style edit: Begins bandage tracking and cancels swing/cast if configured.
    /// </summary>
    /// <param name="healer">The mobile applying the bandage.</param>
    /// <param name="patient">The mobile being healed.</param>
    public static void OnBandageBegin(Mobile healer, Mobile patient)
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: Begin bandage (will cancel swing and cast if configured)
        healer.SphereBeginBandage();

        SphereConfig.DebugLog($"{healer.Name} - Bandaging initiated on {patient?.Name ?? "self"}");
    }

    /// <summary>
    /// Handles bandage completion according to Sphere 0.51a rules.
    /// Sphere-style edit: Ends bandage tracking and sets next bandage time.
    /// </summary>
    /// <param name="healer">The mobile applying the bandage.</param>
    /// <param name="patient">The mobile being healed.</param>
    /// <param name="delay">The delay until next bandage.</param>
    public static void OnBandageComplete(Mobile healer, Mobile patient, TimeSpan delay)
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: End bandage tracking
        healer.SphereEndBandage();

        // Sphere-style edit: Set next bandage time (independent timer)
        if (SphereConfig.IndependentBandageTimer)
        {
            healer.SphereSetNextBandageTime(delay);
            SphereConfig.DebugLog($"{healer.Name} - Next bandage time: {delay.TotalSeconds}s");
        }

        SphereConfig.DebugLog($"{healer.Name} - Bandaging completed on {patient?.Name ?? "self"}");
    }

    /// <summary>
    /// Handles bandage interruption according to Sphere 0.51a rules.
    /// Sphere-style edit: Processes interruption with Sphere-specific logic.
    /// </summary>
    /// <param name="healer">The mobile applying the bandage.</param>
    /// <param name="reason">The reason for interruption.</param>
    public static void InterruptBandage(Mobile healer, string reason)
    {
        if (!SphereConfig.IsEnabled())
            return;

        // Sphere-style edit: End bandage tracking
        healer.SphereEndBandage();

        SphereConfig.LogCancellation(healer, "Bandaging", reason);
        SphereConfig.DebugLog($"{healer.Name} - Bandaging interrupted: {reason}");
    }

    /// <summary>
    /// Checks if a mobile's bandage timer is ready.
    /// Sphere-style edit: Uses independent NextBandageTime tracking.
    /// </summary>
    /// <param name="healer">The mobile to check.</param>
    /// <returns>True if bandage is ready; false otherwise.</returns>
    public static bool IsBandageReady(Mobile healer)
    {
        if (!SphereConfig.IsEnabled())
            return true; // Default to ModernUO behavior

        // Check if enough time has passed since last bandage
        var state = healer.GetSphereState();
        var ready = Core.TickCount - state.NextBandageTime >= 0;

        if (!ready)
        {
            SphereConfig.DebugLog($"{healer.Name} - Bandage not ready (cooldown remaining)");
        }

        return ready;
    }

    /// <summary>
    /// Gets the bandage delay with Sphere-style calculation if enabled.
    /// </summary>
    /// <param name="healer">The healer.</param>
    /// <param name="patient">The patient.</param>
    /// <param name="originalDelay">The original delay from ModernUO.</param>
    /// <returns>The adjusted delay.</returns>
    public static TimeSpan GetBandageDelay(Mobile healer, Mobile patient, TimeSpan originalDelay)
    {
        if (!SphereConfig.IsEnabled())
            return originalDelay;

        // Currently uses ModernUO default calculation
        // Can be extended with Sphere-specific bandage timing if needed
        return originalDelay;
    }

    /// <summary>
    /// Processes bandage effects according to Sphere 0.51a rules.
    /// </summary>
    /// <param name="healer">The healer.</param>
    /// <param name="patient">The patient.</param>
    /// <param name="healAmount">The amount healed.</param>
    public static void ProcessBandageEffects(Mobile healer, Mobile patient, int healAmount)
    {
        if (!SphereConfig.IsEnabled())
            return;

        SphereConfig.DebugLog($"{healer.Name} - Healed {patient?.Name ?? "unknown"} for {healAmount} HP");
    }

    /// <summary>
    /// Checks if bandaging should be interrupted by movement.
    /// Sphere-style edit: Sphere allows movement during bandaging.
    /// </summary>
    /// <param name="healer">The healer.</param>
    /// <param name="defaultInterrupt">Default interrupt behavior.</param>
    /// <returns>True if movement should interrupt; false otherwise.</returns>
    public static bool CheckMovementInterrupt(Mobile healer, bool defaultInterrupt)
    {
        if (!SphereConfig.IsEnabled())
            return defaultInterrupt;

        // Sphere typically allows movement during bandaging
        // This can be configured if different behavior is desired
        return defaultInterrupt;
    }
}
