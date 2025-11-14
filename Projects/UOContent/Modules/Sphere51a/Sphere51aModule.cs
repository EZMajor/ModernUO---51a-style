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
using Server.Modules.Sphere51a.Combat.Audit;
using Server.Modules.Sphere51a.Configuration;
using Server.Modules.Sphere51a.Core;
using Server.Modules.Sphere51a.DuelArena;
using Server.Modules.Sphere51a.Events;
using Server.Modules.Sphere51a.Items;
using Server.Modules.Sphere51a.Testing;

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

            // CRITICAL: Handle configuration FIRST, before any other initialization
            // This prevents SphereConfiguration.Initialize() from creating defaults
            var config = ModuleConfig.LoadConfig();

            if (config == null)
            {
                // Read from ServerConfiguration (prompt happens during server startup)
                var enabled = ServerConfiguration.GetSetting("sphere51a.enabled", false);
                logger.Information("[Sphere-Config] No existing module config found, reading from ServerConfiguration: {Enabled}", enabled);

                // Create config from ServerConfiguration setting
                config = new Sphere51aConfig
                {
                    Enabled = enabled,
                    UseGlobalPulse = true,
                    GlobalTickMs = 50,
                    CombatIdleTimeoutMs = 30000,
                    IndependentTimers = false,
                    WeaponTimingConfigPath = "Data/Sphere51a/weapons_timing.json"
                };

                ModuleConfig.SaveConfig(config);
                logger.Information("[Sphere-Config] Sphere 51a module config created from ServerConfiguration - Enabled: {Enabled}", enabled);
            }

            // Set the loaded/created config BEFORE initializing subsystems
            ModuleConfig.SetConfig(config);

            // CRITICAL: Sync to SphereConfiguration BEFORE calling SphereInitializer
            // This ensures SphereConfiguration.Enabled matches the user's choice from ModuleConfig
            SphereConfiguration.Enabled = config.Enabled;
            logger.Information("[Sphere-Config] Synced config to SphereConfiguration.Enabled = {Enabled}", config.Enabled);

            // Initialize UO path resolver for testing framework
            UOPathResolver.ResolveUOPath();
            logger.Debug("UO path resolver initialized");

            // Register with module registry
            ModuleRegistry.Register("Sphere51a", new Sphere51aModuleInstance());

            // TODO: Harmony/RuntimePatcher removed - migrate to partial classes and EventSink
            // See Docs/HarmonyRemoval.md for migration plan
            // Previous hooks: BaseWeapon.GetDelay(), BaseWeapon.OnSwing(), Mobile.NextCombatTime

            // Initialize the new global tick system
            SphereInitializer.Initialize();

            // Initialize spell timing provider
            Spells.SpellTimingProvider.Initialize();

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

            // Initialize the combat audit system if enabled
            if (SphereConfiguration.Enabled && SphereConfiguration.Audit?.Enabled == true)
            {
                CombatAuditSystem.Initialize(SphereConfiguration.Audit);
                logger.Information("Combat Audit System initialized (Level: {Level}, Buffer: {BufferSize})",
                    SphereConfiguration.Audit.Level, SphereConfiguration.Audit.BufferSize);
            }
            else
            {
                logger.Debug("Combat Audit System disabled by configuration");
            }

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

        // Spell casting events - DISABLED: Conflicts with SphereSpellHandlers in global pulse mode
        // SphereEvents.OnSpellCast += SphereCombatSystem.HandleSpellCast;
        // SphereEvents.OnSpellCastBegin += SphereCombatSystem.HandleSpellCastBegin;
        // SphereEvents.OnSpellCastComplete += SphereCombatSystem.HandleSpellCastComplete;
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
    /// Shuts down the module and all subsystems.
    /// </summary>
    public static void Shutdown()
    {
        if (!IsInitialized)
            return;

        try
        {
            logger.Information("Shutting down Sphere 51a Module...");

            // Shutdown audit system first to ensure all data is flushed
            if (CombatAuditSystem.IsInitialized)
            {
                CombatAuditSystem.Shutdown();
            }

            // Shutdown other subsystems
            // (Add other shutdown calls here as needed)

            logger.Information("Sphere 51a Module shutdown complete");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during Sphere 51a module shutdown");
        }
    }

    /// <summary>
    /// Gets the module status for diagnostics.
    /// </summary>
    public static string GetStatus()
    {
        var auditStatus = CombatAuditSystem.IsInitialized
            ? $"Enabled (Level: {CombatAuditSystem.Config?.Level}, Buffer: {CombatAuditSystem.BufferCount}/{CombatAuditSystem.Config?.BufferSize})"
            : "Disabled";

        return $@"
Sphere 51a Module Status:
- Version: {Version}
- Initialized: {IsInitialized}
- Enabled: {SphereConfiguration.Enabled}
- Combat System: {SphereCombatSystem.GetStatus()}
- Duel Arena: {SphereDuelArena.GetStatus()}
- Beta Test Stone: {SphereBetaTestStone.GetStatus()}
- Audit System: {auditStatus}
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
