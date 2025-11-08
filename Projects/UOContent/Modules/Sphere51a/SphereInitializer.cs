/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: SphereInitializer.cs
 *
 * Description: Initialization system for Sphere 51a combat mechanics.
 *              Sets up timing providers and registers event handlers.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using Server.Items;
using Server.Logging;
using Server.Modules.Sphere51a.Combat;
using Server.Modules.Sphere51a.Configuration;
using Server.Modules.Sphere51a.Events;

namespace Server.Modules.Sphere51a;

/// <summary>
/// Initializes the Sphere 51a combat system and registers timing providers.
/// </summary>
public static class SphereInitializer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(SphereInitializer));

    /// <summary>
    /// Whether the initializer has been run.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// Whether initialization has been attempted (to prevent multiple attempts).
    /// </summary>
    private static bool _initializationAttempted;

    /// <summary>
    /// The currently active timing provider.
    /// </summary>
    public static ITimingProvider ActiveTimingProvider { get; private set; }

    /// <summary>
    /// Initializes the Sphere 51a system.
    /// Called during server startup.
    /// </summary>
    public static void Initialize()
    {
        if (_initializationAttempted)
        {
            logger.Debug("Sphere initializer already attempted, skipping");
            return;
        }

        _initializationAttempted = true;

        try
        {
            // Initialize configuration first
            SphereConfiguration.Initialize();

            if (!SphereConfiguration.Enabled)
            {
                logger.Information("Sphere 51a system disabled - skipping initialization");
                IsInitialized = true;
                return;
            }

            // Initialize combat systems
            InitializeCombatSystem();

            // Register event handlers
            RegisterEventHandlers();

            // Register commands
            RegisterCommands();

            IsInitialized = true;

            logger.Information("Sphere 51a system initialized successfully - Active Provider: {Provider}",
                ActiveTimingProvider?.ProviderName ?? "None");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to initialize Sphere 51a system");
            // Don't set IsInitialized = true on failure, allowing retry
            throw;
        }
    }

    /// <summary>
    /// Initializes the combat system and timing providers.
    /// </summary>
    private static void InitializeCombatSystem()
    {
        //Sphere 51a "Register callback to skip ModernUO combat scheduler when using independent timers"
        // Note: ShouldSkipCombatTime property not available in base ModernUO - functionality may be limited
        // Mobile.ShouldSkipCombatTime = (mobile) =>
        // {
        //     return SphereConfiguration.Enabled && SphereConfiguration.IndependentTimers;
        // };

        // Determine which timing provider to use
        if (SphereConfiguration.UseGlobalPulse)
        {
            // Use new global pulse system
            InitializeGlobalPulseSystem();
        }
        else
        {
            // Use legacy adapter
            InitializeLegacySystem();
        }

        // Log active configuration
        LogConfigurationStatus();
    }

    /// <summary>
    /// Initializes the global pulse combat system.
    /// </summary>
    private static void InitializeGlobalPulseSystem()
    {
        logger.Information("Initializing global pulse combat system");

        // Load weapon timing configuration
        Dictionary<int, WeaponEntry> weaponTable;
        try
        {
            var configPath = SphereConfiguration.WeaponTimingConfigPath;
            if (File.Exists(configPath))
            {
                weaponTable = WeaponTimingProvider.LoadFromJson(configPath);
                logger.Information($"Loaded weapon timing config from {configPath}");
            }
            else
            {
                // Create compatibility mapping from existing tables
                weaponTable = WeaponTimingProvider.CreateCompatibilityMapping();
                logger.Information("Using compatibility mapping for weapon timings");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to load weapon timing config, using defaults");
            weaponTable = new Dictionary<int, WeaponEntry>();
        }

        // Create and set active timing provider
        ActiveTimingProvider = new WeaponTimingProvider(weaponTable);

        // Initialize combat pulse
        CombatPulse.Initialize();

        logger.Information("Global pulse system initialized with {WeaponCount} weapon entries",
            weaponTable.Count);
    }

    /// <summary>
    /// Initializes the legacy timing system via adapter.
    /// </summary>
    private static void InitializeLegacySystem()
    {
        logger.Information("Initializing legacy timing system");

        // Create legacy adapter
        ActiveTimingProvider = new LegacySphereTimingAdapter();

        logger.Information("Legacy timing system initialized");
    }

    /// <summary>
    /// Registers event handlers for combat events.
    /// </summary>
    private static void RegisterEventHandlers()
    {
        // Register weapon swing event handler
        SphereEvents.OnWeaponSwing += HandleWeaponSwingEvent;

        // Register weapon swing complete event handler
        SphereEvents.OnWeaponSwingComplete += HandleWeaponSwingCompleteEvent;

        logger.Debug("Event handlers registered");
    }

    /// <summary>
    /// Registers Sphere commands.
    /// </summary>
    private static void RegisterCommands()
    {
        // Register verification commands
        Commands.VerifyWeaponTiming.Initialize();
        Commands.VerifyCombatTick.Initialize();
        Commands.SphereShadowReport.Initialize();
        Commands.SpherePerformance.Initialize();
        Commands.SphereLoadTest.Initialize();
        Commands.SpherePerfReport.Initialize();

        // Register audit system commands
        Commands.VerifyTimingAccuracy.Initialize();
        Commands.SphereCombatAudit.Initialize();

        logger.Debug("Commands registered");
    }

    /// <summary>
    /// Handles weapon swing events.
    /// </summary>
    private static void HandleWeaponSwingEvent(object sender, WeaponSwingEventArgs e)
    {
        if (!SphereConfiguration.Enabled || ActiveTimingProvider == null)
            return;

        var attacker = e.Attacker;
        var defender = e.Defender;
        var weapon = e.Weapon;

        // Validate swing
        if (!AttackRoutine.CanAttack(attacker))
        {
            e.Cancelled = true;
            SphereConfiguration.LogCancellation(attacker, "Weapon swing", "Timer not ready");
            return;
        }

        // If using global pulse, handle through AttackRoutine
        if (SphereConfiguration.UseGlobalPulse && weapon is BaseWeapon baseWeapon)
        {
            // Execute attack routine
            AttackRoutine.ExecuteAttack(attacker, defender, baseWeapon, ActiveTimingProvider);

            // Cancel the default event processing since we handled it
            e.Cancelled = true;
        }
    }

    /// <summary>
    /// Handles weapon swing complete events.
    /// </summary>
    private static void HandleWeaponSwingCompleteEvent(object sender, WeaponSwingEventArgs e)
    {
        if (!SphereConfiguration.Enabled)
            return;

        var attacker = e.Attacker;
        var weapon = e.Weapon;

        // Update timing if not using global pulse
        if (!SphereConfiguration.UseGlobalPulse)
        {
            var delay = ActiveTimingProvider.GetAttackIntervalMs(attacker, weapon);
            e.Delay = TimeSpan.FromMilliseconds(delay);
        }
    }

    /// <summary>
    /// Logs the current configuration status.
    /// </summary>
    private static void LogConfigurationStatus()
    {
        logger.Information("=== Sphere 51a Configuration ===");
        logger.Information("Enabled: {Enabled}", SphereConfiguration.Enabled);
        logger.Information("Use Global Pulse: {UseGlobalPulse}", SphereConfiguration.UseGlobalPulse);
        logger.Information("Global Tick Ms: {TickMs}", SphereConfiguration.GlobalTickMs);
        logger.Information("Combat Idle Timeout: {Timeout}ms", SphereConfiguration.CombatIdleTimeoutMs);
        logger.Information("Weapon Config Path: {Path}", SphereConfiguration.WeaponTimingConfigPath);
        logger.Information("Independent Timers: {Independent}", SphereConfiguration.IndependentTimers);
        logger.Information("Debug Logging: {Debug}", SphereConfiguration.EnableDebugLogging);
        logger.Information("Shadow Logging: {Shadow}", SphereConfiguration.EnableShadowLogging);
        logger.Information("================================");
    }

    /// <summary>
    /// Shuts down the Sphere system.
    /// </summary>
    public static void Shutdown()
    {
        if (!IsInitialized)
            return;

        try
        {
            // Shutdown combat pulse if active
            if (SphereConfiguration.UseGlobalPulse)
            {
                CombatPulse.Shutdown();
            }

            //Sphere 51a "Unregister combat time callback"
            // Note: ShouldSkipCombatTime property not available in base ModernUO
            // Mobile.ShouldSkipCombatTime = null;

            // Clear active provider
            ActiveTimingProvider = null;

            IsInitialized = false;

            logger.Information("Sphere 51a system shutdown");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during Sphere system shutdown");
        }
    }
}
