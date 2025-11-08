/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: SphereConfiguration.cs
 *
 * Description: Configuration system for modular Sphere 51a combat mechanics.
 *              Provides centralized control over combat behavior toggles.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using Server.Logging;
using Server.Mobiles;
using Server.Modules.Sphere51a.Combat.Audit;
using Server.Modules.Sphere51a.Core;

namespace Server.Modules.Sphere51a.Configuration;

/// <summary>
/// Centralized configuration for Sphere 51a-style combat mechanics.
/// </summary>
public static class SphereConfiguration
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SphereConfiguration));

    /// <summary>
    /// Master toggle for Sphere 51a combat system.
    /// When false, all Sphere-style modifications are disabled.
    /// </summary>
    public static bool Enabled { get; set; }

    /// <summary>
    /// Whether the configuration has been initialized.
    /// </summary>
    public static bool IsInitialized { get; private set; }

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

    /// <summary>
    /// Percentage of mana to deduct at target confirmation when using dual mana deduction.
    /// Sphere-style: 50 (50% partial, 50% remaining on success)
    /// </summary>
    public static int PartialManaPercent { get; set; } = 50;

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

    /// <summary>
    /// Clear hands (unequip weapons/shields) when casting spells.
    /// Sphere-style: false (weapons stay equipped during casting)
    /// ModernUO default: true (ClearHandsOnCast removes items)
    /// </summary>
    public static bool ClearHandsOnCast { get; set; } = false;

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

    #region Global Combat System

    /// <summary>
    /// Enable the global CombatPulse system instead of per-mobile timers.
    /// Provides deterministic ±25ms PvP precision and better scalability.
    /// </summary>
    public static bool UseGlobalPulse { get; set; } = true;

    /// <summary>
    /// Global combat tick interval in milliseconds (default: 50ms = 20 Hz).
    /// </summary>
    public static int GlobalTickMs { get; set; } = 50;

    /// <summary>
    /// Combat idle timeout in milliseconds before unregistering combatants (default: 5 seconds).
    /// </summary>
    public static int CombatIdleTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Path to weapon timing configuration JSON file.
    /// </summary>
    public static string WeaponTimingConfigPath { get; set; } = "Data/Sphere51a/weapons_timing.json";

    #endregion

    #region Debugging and Logging

    /// <summary>
    /// Enable detailed combat logging for debugging.
    /// Maps to Audit.Level >= AuditLevel.Debug
    /// </summary>
    public static bool EnableDebugLogging
    {
        get => Audit != null && Audit.Level >= AuditLevel.Debug;
        set
        {
            if (Audit != null)
            {
                Audit.Level = value ? AuditLevel.Debug : AuditLevel.Standard;
            }
        }
    }

    /// <summary>
    /// Log action cancellations to console.
    /// Maps to Audit.Level >= AuditLevel.Detailed
    /// </summary>
    public static bool LogActionCancellations
    {
        get => Audit != null && Audit.Level >= AuditLevel.Detailed;
        set
        {
            if (Audit != null && value)
            {
                Audit.Level = AuditLevel.Detailed;
            }
        }
    }

    /// <summary>
    /// Log timer state changes to console.
    /// Maps to Audit.Level >= AuditLevel.Detailed
    /// </summary>
    public static bool LogTimerStateChanges
    {
        get => Audit != null && Audit.Level >= AuditLevel.Detailed;
        set
        {
            if (Audit != null && value)
            {
                Audit.Level = AuditLevel.Detailed;
            }
        }
    }

    /// <summary>
    /// Enable shadow mode logging to compare new vs legacy timing systems.
    /// Maps to Audit.EnableShadowMode
    /// </summary>
    public static bool EnableShadowLogging
    {
        get => Audit?.EnableShadowMode ?? false;
        set
        {
            if (Audit != null)
            {
                Audit.EnableShadowMode = value;
            }
        }
    }

    #endregion

    #region Combat Audit System

    /// <summary>
    /// Combat audit system configuration.
    /// Provides real-time monitoring, verification, and persistent logging.
    /// </summary>
    public static AuditConfig Audit { get; private set; }

    #endregion

    /// <summary>
    /// Initializes the configuration system from ModuleConfig.
    /// This is called after ModuleConfig has been loaded/created.
    /// </summary>
    public static void Initialize()
    {
        if (IsInitialized)
        {
            logger.Warning("Sphere configuration already initialized");
            return;
        }

        // Read from ModuleConfig (the single source of truth)
        var config = ModuleConfig.Config;
        if (config != null)
        {
            Enabled = config.Enabled;

            // Initialize audit configuration
            Audit = config.Audit ?? new AuditConfig();
            Audit.Validate();

            logger.Information("Sphere configuration initialized from ModuleConfig - Enabled: {Enabled}, Audit: {AuditEnabled}",
                Enabled, Audit.Enabled);
        }
        else
        {
            // Fallback if config hasn't been loaded yet
            Enabled = false;
            Audit = new AuditConfig { Enabled = false };
            logger.Warning("ModuleConfig not loaded yet, defaulting to disabled");
        }

        IsInitialized = true;
    }

    #region Helper Methods

    /// <summary>
    /// Log a debug message if debug logging is enabled.
    /// </summary>
    public static void DebugLog(string message)
    {
        if (EnableDebugLogging)
        {
            logger.Debug("[Sphere] {Message}", message);
        }
    }

    /// <summary>
    /// Log an action cancellation if logging is enabled.
    /// </summary>
    public static void LogCancellation(Mobile mobile, string action, string reason)
    {
        if (LogActionCancellations)
        {
            logger.Information("[Sphere] {Name} - {Action} cancelled: {Reason}",
                mobile.Name, action, reason);
        }
    }

    /// <summary>
    /// Log a timer state change if logging is enabled.
    /// </summary>
    public static void LogTimerChange(Mobile mobile, string timerName, long oldValue, long newValue)
    {
        if (LogTimerStateChanges)
        {
            logger.Debug("[Sphere] {Name} - {Timer}: {Old} -> {New}",
                mobile.Name, timerName, oldValue, newValue);
        }
    }

    /// <summary>
    /// Validate and get the partial mana percentage for dual mana deduction.
    /// Ensures PartialManaPercent is within valid range [0, 100].
    /// </summary>
    public static int GetValidPartialManaPercent()
    {
        if (PartialManaPercent < 0)
        {
            DebugLog("PartialManaPercent is negative, clamping to 0");
            return 0;
        }

        if (PartialManaPercent > 100)
        {
            DebugLog("PartialManaPercent exceeds 100, clamping to 100");
            return 100;
        }

        return PartialManaPercent;
    }

    /// <summary>
    /// Calculate partial mana to deduct at target confirmation.
    /// Uses validated PartialManaPercent to ensure proper calculations.
    /// </summary>
    public static int CalculatePartialMana(int totalMana)
    {
        var validPercent = GetValidPartialManaPercent();
        return (totalMana * validPercent) / 100;
    }

    #endregion
}
