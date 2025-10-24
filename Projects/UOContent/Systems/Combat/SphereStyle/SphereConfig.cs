/*************************************************************************
 * ModernUO - Sphere 0.51a Combat System
 * File: SphereConfig.cs
 *
 * Description: Configuration system for Sphere 0.51a-style combat mechanics.
 *              Provides centralized control over combat behavior toggles and
 *              timing parameters to match Sphere 0.51a server behavior.
 *
 * Reference: Sphere0.51aCombatSystem.md
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

namespace Server.Systems.Combat.SphereStyle;

/// <summary>
/// Centralized configuration for Sphere 0.51a-style combat mechanics.
/// </summary>
/// <remarks>
/// This configuration class controls the behavior of combat, spellcasting,
/// and action timing to match Sphere 0.51a mechanics. All values are
/// configurable to allow fine-tuning or reverting to ModernUO defaults.
/// </remarks>
public static class SphereConfig
{
    /// <summary>
    /// Master toggle for Sphere 0.51a combat system.
    /// When false, all Sphere-style modifications are disabled.
    /// </summary>
    public static bool EnableSphereStyle { get; set; } = true;

    #region Timer Independence

    /// <summary>
    /// Enable independent timer operation (no shared recovery delays).
    /// Sphere-style: true (NextSwingTime, NextSpellTime, NextBandageTime operate independently)
    /// ModernUO default: false (shared recovery logic)
    /// </summary>
    public static bool IndependentTimers { get; set; } = true;

    /// <summary>
    /// Remove global recovery delay after actions.
    /// Sphere-style: true (no RecoveryDelay)
    /// ModernUO default: false (RecoveryDelay exists)
    /// </summary>
    public static bool RemoveGlobalRecovery { get; set; } = true;

    #endregion

    #region Action Cancellation

    /// <summary>
    /// Starting a spell cast cancels pending weapon swing.
    /// Sphere-style: true
    /// </summary>
    public static bool SpellCancelSwing { get; set; } = true;

    /// <summary>
    /// Beginning an attack cancels active spell cast.
    /// Sphere-style: true
    /// </summary>
    public static bool SwingCancelSpell { get; set; } = true;

    /// <summary>
    /// Bandage use cancels both swing and cast.
    /// Sphere-style: true
    /// </summary>
    public static bool BandageCancelActions { get; set; } = true;

    /// <summary>
    /// Wand use cancels both swing and cast.
    /// Sphere-style: true
    /// </summary>
    public static bool WandCancelActions { get; set; } = true;

    /// <summary>
    /// Weapon swings are disabled during spell casting.
    /// Sphere-style: true
    /// </summary>
    public static bool DisableSwingDuringCast { get; set; } = true;

    /// <summary>
    /// Weapon swings are disabled during cast delay (post-target selection).
    /// Sphere-style: true
    /// </summary>
    public static bool DisableSwingDuringCastDelay { get; set; } = true;

    #endregion

    #region Spellcasting Mechanics

    /// <summary>
    /// Allow movement during spell casting (no movement lock).
    /// Sphere-style: true
    /// ModernUO default: false (BlocksMovement during cast)
    /// </summary>
    public static bool AllowMovementDuringCast { get; set; } = true;

    /// <summary>
    /// Remove post-cast recovery delay.
    /// Sphere-style: true (NextSpellTime not set after cast completion)
    /// ModernUO default: false (GetCastRecovery() adds delay)
    /// </summary>
    public static bool RemovePostCastRecovery { get; set; } = true;

    /// <summary>
    /// Spell initiates immediately with target cursor (no pre-cast delay).
    /// Sphere-style: true
    /// </summary>
    public static bool ImmediateSpellTarget { get; set; } = true;

    /// <summary>
    /// Cast delay occurs between target selection and effect application.
    /// Sphere-style: true
    /// ModernUO default: true (already implemented)
    /// </summary>
    public static bool CastDelayAfterTarget { get; set; } = true;

    /// <summary>
    /// Mana deduction occurs at target confirmation, not cast start.
    /// Sphere-style: true
    /// ModernUO default: false (mana checked at cast start)
    /// </summary>
    public static bool TargetManaDeduction { get; set; } = true;

    /// <summary>
    /// Enable damage-based spell fizzle after target selection.
    /// Sphere-style: configurable (often disabled)
    /// ModernUO default: true
    /// </summary>
    public static bool DamageBasedFizzle { get; set; } = false;

    /// <summary>
    /// Fizzle triggers only from defined actions, not movement/damage.
    /// Sphere-style: true
    /// </summary>
    public static bool RestrictedFizzleTriggers { get; set; } = true;

    #endregion

    #region Weapon Swing Mechanics

    /// <summary>
    /// Apply damage immediately upon hit confirmation (no delay).
    /// Sphere-style: true
    /// ModernUO default: false (may defer to event tick)
    /// </summary>
    public static bool ImmediateDamageApplication { get; set; } = true;

    /// <summary>
    /// Swing timer resets on interrupt (no queued swings).
    /// Sphere-style: true
    /// </summary>
    public static bool ResetSwingOnInterrupt { get; set; } = true;

    /// <summary>
    /// Use Sphere-style swing speed calculation.
    /// Sphere-style: true (BaseSpeed / (Dexterity / 100))
    /// ModernUO default: false (complex formula with stamina, bonuses)
    /// </summary>
    public static bool SphereSwingSpeedCalculation { get; set; } = false;

    /// <summary>
    /// Melee and ranged weapons use identical swing logic.
    /// Sphere-style: true
    /// </summary>
    public static bool UnifiedMeleeRangedLogic { get; set; } = true;

    #endregion

    #region Bandaging

    /// <summary>
    /// Bandage operates on independent timer (not blocked by other actions).
    /// Sphere-style: true
    /// </summary>
    public static bool IndependentBandageTimer { get; set; } = true;

    #endregion

    #region Wand Behavior

    /// <summary>
    /// Wand use executes instantly (no cast delay).
    /// Sphere-style: true
    /// ModernUO default: false (short cast delay)
    /// </summary>
    public static bool InstantWandCast { get; set; } = true;

    #endregion

    #region Animation and Synchronization

    /// <summary>
    /// Server authoritative on timing (ignore client animation states).
    /// Sphere-style: true
    /// </summary>
    public static bool ServerAuthoritativeTiming { get; set; } = true;

    /// <summary>
    /// Disable forced animation locks during combat.
    /// Sphere-style: true
    /// </summary>
    public static bool DisableAnimationLocks { get; set; } = true;

    /// <summary>
    /// Disable equipment locks during combat actions.
    /// Sphere-style: true
    /// </summary>
    public static bool DisableEquipmentLocks { get; set; } = true;

    #endregion

    #region Action Queuing

    /// <summary>
    /// Disable action queuing (actions must complete before starting new ones).
    /// Sphere-style: true (no queued swings/casts)
    /// </summary>
    public static bool DisableActionQueuing { get; set; } = true;

    #endregion

    #region Timing Constants (Sphere 0.51a values)

    /// <summary>
    /// Minimum swing speed in seconds.
    /// Sphere-style: weapon-dependent, typically no hard minimum
    /// ModernUO default: 1.25 seconds
    /// </summary>
    public static double MinimumSwingSpeed { get; set; } = 0.5;

    /// <summary>
    /// Maximum swing speed in seconds.
    /// Sphere-style: weapon-dependent
    /// </summary>
    public static double MaximumSwingSpeed { get; set; } = 10.0;

    /// <summary>
    /// Spell cast minimum delay in seconds.
    /// Sphere-style: spell-dependent, typically no hard minimum
    /// ModernUO default: 0.25 seconds
    /// </summary>
    public static double MinimumCastDelay { get; set; } = 0.0;

    #endregion

    #region Debugging and Logging

    /// <summary>
    /// Enable detailed combat logging for debugging.
    /// </summary>
    public static bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// Log action cancellations to console.
    /// </summary>
    public static bool LogActionCancellations { get; set; } = false;

    /// <summary>
    /// Log timer state changes to console.
    /// </summary>
    public static bool LogTimerStateChanges { get; set; } = false;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Check if Sphere-style combat is enabled for the specified feature.
    /// </summary>
    public static bool IsEnabled() => EnableSphereStyle;

    /// <summary>
    /// Log a debug message if debug logging is enabled.
    /// </summary>
    public static void DebugLog(string message)
    {
        if (EnableDebugLogging)
        {
            Console.WriteLine($"[Sphere-Combat] {message}");
        }
    }

    /// <summary>
    /// Log an action cancellation if logging is enabled.
    /// </summary>
    public static void LogCancellation(Mobile mobile, string action, string reason)
    {
        if (LogActionCancellations)
        {
            Console.WriteLine($"[Sphere-Combat] {mobile.Name} - {action} cancelled: {reason}");
        }
    }

    /// <summary>
    /// Log a timer state change if logging is enabled.
    /// </summary>
    public static void LogTimerChange(Mobile mobile, string timerName, long oldValue, long newValue)
    {
        if (LogTimerStateChanges)
        {
            Console.WriteLine($"[Sphere-Combat] {mobile.Name} - {timerName}: {oldValue} -> {newValue}");
        }
    }

    #endregion
}
