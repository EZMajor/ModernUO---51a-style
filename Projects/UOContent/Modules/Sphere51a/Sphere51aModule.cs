/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: Sphere51aModule.cs
 *
 * Description: Main module loader for Sphere 51a combat mechanics.
 *              Initializes the modular system and subscribes to events.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.IO;
using Server.Logging;
using Server.Modules.Sphere51a.Combat;
using Server.Modules.Sphere51a.Configuration;
using Server.Modules.Sphere51a.Core;
using Server.Modules.Sphere51a.DuelArena;
using Server.Modules.Sphere51a.Events;
using Server.Modules.Sphere51a.Items;

namespace Server.Modules.Sphere51a;

/// <summary>
/// Main module class for Sphere 51a combat mechanics.
/// Provides drop-in modular functionality without core file modifications.
/// </summary>
public static class Sphere51aModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(Sphere51aModule));

    /// <summary>
    /// Module version information.
    /// </summary>
    public static readonly Version Version = new(1, 0, 0);

    /// <summary>
    /// Whether the module has been initialized.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// Whether initialization has been attempted (to prevent multiple attempts).
    /// </summary>
    private static bool _initializationAttempted;

    /// <summary>
    /// Initializes the Sphere 51a module.
    /// Called during server startup after assemblies are loaded.
    /// </summary>
    public static void Initialize()
    {
        if (_initializationAttempted)
        {
            logger.Debug("Sphere 51a module initialization already attempted, skipping");
            return;
        }

        _initializationAttempted = true;

        try
        {
            logger.Information("Initializing Sphere 51a Module v{Version}", Version);

            // Load module configuration
            var config = ModuleConfig.Config;

            // Migrate from global config if needed
            if (!File.Exists(ModuleConfig.ConfigPath))
            {
                ModuleConfig.MigrateFromGlobalConfig();
            }

            // Register with module registry
            ModuleRegistry.Register("Sphere51a", new Sphere51aModuleInstance());

            // TODO: Harmony/RuntimePatcher removed - migrate to partial classes and EventSink
            // See Docs/HarmonyRemoval.md for migration plan
            // Previous hooks: BaseWeapon.GetDelay(), BaseWeapon.OnSwing(), Mobile.NextCombatTime

            // Initialize the new global tick system
            SphereInitializer.Initialize();

            // Initialize legacy subsystems if needed
            if (!SphereConfiguration.UseGlobalPulse)
            {
                // Subscribe to combat events for legacy system
                SubscribeToEvents();

                // Initialize legacy subsystems
                SphereCombatSystem.Initialize();
            }

            SphereDuelArena.Initialize();
            SphereBetaTestStone.Initialize();

            IsInitialized = true;

            logger.Information("Sphere 51a Module initialized successfully");
            logger.Information("Combat System: {System}",
                SphereConfiguration.UseGlobalPulse ? "Global Pulse" : "Legacy Timers");
            logger.Information("Sphere 51a Style: {Enabled}",
                SphereConfiguration.Enabled ? "ENABLED" : "DISABLED");

        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to initialize Sphere 51a module");
            // Don't set IsInitialized = true on failure, allowing retry
            throw;
        }
    }

    /// <summary>
    /// Subscribes to all combat-related events.
    /// </summary>
    private static void SubscribeToEvents()
    {
        // Weapon combat events
        SphereEvents.OnWeaponSwing += SphereCombatSystem.HandleWeaponSwing;
        SphereEvents.OnWeaponSwingComplete += SphereCombatSystem.HandleWeaponSwingComplete;

        // Spell casting events
        SphereEvents.OnSpellCast += SphereCombatSystem.HandleSpellCast;
        SphereEvents.OnSpellCastBegin += SphereCombatSystem.HandleSpellCastBegin;
        SphereEvents.OnSpellCastComplete += SphereCombatSystem.HandleSpellCastComplete;
        SphereEvents.OnSpellBlocksMovement += SphereCombatSystem.HandleSpellBlocksMovement;

        // Bandage events
        SphereEvents.OnBandageUse += SphereCombatSystem.HandleBandageUse;
        SphereEvents.OnBandageUseComplete += SphereCombatSystem.HandleBandageUseComplete;

        // Wand events
        SphereEvents.OnWandUse += SphereCombatSystem.HandleWandUse;
        SphereEvents.OnWandUseComplete += SphereCombatSystem.HandleWandUseComplete;

        // Combat state events
        SphereEvents.OnCombatEnter += SphereCombatSystem.HandleCombatEnter;
        SphereEvents.OnCombatExit += SphereCombatSystem.HandleCombatExit;

        logger.Debug("Subscribed to {Count} event types", 11);
    }

    /// <summary>
    /// Configures the module during the Configure phase.
    /// </summary>
    public static void Configure()
    {
        if (!IsInitialized)
        {
            logger.Warning("Cannot configure Sphere 51a module - not initialized");
            return;
        }

        try
        {
            // Load configuration settings now that ServerConfiguration is available
            SphereConfiguration.Configure();

            // Configure subsystems
            SphereCombatSystem.Configure();
            SphereDuelArena.Configure();
            SphereBetaTestStone.Configure();

            logger.Debug("Sphere 51a module configured");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to configure Sphere 51a module");
        }
    }

    /// <summary>
    /// Performs final initialization during the Initialize phase.
    /// </summary>
    public static void InitializePhase()
    {
        if (!IsInitialized)
        {
            logger.Warning("Cannot initialize Sphere 51a module - not initialized");
            return;
        }

        try
        {
            // Final initialization of subsystems
            SphereCombatSystem.InitializePhase();
            SphereDuelArena.InitializePhase();

            logger.Debug("Sphere 51a module initialization phase complete");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to complete Sphere 51a module initialization");
        }
    }

    /// <summary>
    /// Gets the module status for diagnostics.
    /// </summary>
    public static string GetStatus()
    {
        return $@"
Sphere 51a Module Status:
- Version: {Version}
- Initialized: {IsInitialized}
- Enabled: {SphereConfiguration.Enabled}
- Combat System: {SphereCombatSystem.GetStatus()}
- Duel Arena: {SphereDuelArena.GetStatus()}
- Beta Test Stone: {SphereBetaTestStone.GetStatus()}
".Trim();
    }

    /// <summary>
    /// Resets module state for testing purposes.
    /// </summary>
    internal static void ResetForTesting()
    {
        IsInitialized = false;
        _initializationAttempted = false;
    }
}

/// <summary>
/// Instance class for module registry.
/// </summary>
public class Sphere51aModuleInstance
{
    public Version Version => Sphere51aModule.Version;
    public bool IsInitialized => Sphere51aModule.IsInitialized;
    public bool IsLoaded => ModuleRegistry.IsModuleLoaded("Sphere51a");

    public string GetStatus() => Sphere51aModule.GetStatus();
}
