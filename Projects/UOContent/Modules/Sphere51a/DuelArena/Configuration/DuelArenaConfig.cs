/*************************************************************************
 * ModernUO - Duel Arena Configuration                                  *
 * File: DuelArenaConfig.cs                                             *
 *                                                                       *
 * Configuration system for the Duel Arena system.                      *
 * Reads from the Sphere 51a master toggle.                             *
 *************************************************************************/

namespace Server.Engines.DuelArena;

/// <summary>
/// Configuration for the Duel Arena system.
/// Controlled by the sphere.enableSphere51aStyle setting.
/// </summary>
public static class DuelArenaConfig
{
    /// <summary>
    /// Gets or sets whether the Duel Arena system is enabled.
    /// This is controlled by the Sphere 51a master toggle.
    /// </summary>
    public static bool Enabled { get; private set; }

    /// <summary>
    /// Loads the configuration by checking if Sphere51a module is loaded.
    /// Called during server startup.
    /// </summary>
    static DuelArenaConfig()
    {
        // Check if Sphere51a module is loaded via module registry
        Enabled = Server.Modules.Sphere51a.Core.ModuleRegistry.IsModuleLoaded("Sphere51a");
    }
}
