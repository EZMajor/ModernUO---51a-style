/*************************************************************************
 * ModernUO - Sphere 51a Test Core
 * File: TestCore.cs
 *
 * Description: Wrapper for minimal server initialization in testing environments.
 *              Provides access to Core.SetupMinimal() for test scenarios.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Server.Logging;

namespace Server.Modules.Sphere51a.Testing;

/// <summary>
/// Wrapper for minimal server core initialization.
/// Provides caching and monitoring for test environment setup.
/// </summary>
public static class TestCore
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TestCore));

    // Resource caching for optimized test runs
    private static readonly ConcurrentDictionary<string, object> _resourceCache = new();
    private static readonly object _initializationLock = new();
    private static bool _resourcesInitialized;
    private static DateTime _lastInitializationTime;

    /// <summary>
    /// Initializes a minimal server environment for testing.
    /// This calls Core.SetupMinimal() and provides caching for faster subsequent runs.
    /// </summary>
    /// <param name="applicationAssembly">The application assembly</param>
    /// <param name="process">The current process</param>
    /// <returns>True if initialization successful</returns>
    public static bool SetupMinimal(System.Reflection.Assembly applicationAssembly, System.Diagnostics.Process process)
    {
        try
        {
            // Check if we can use cached resources (within 5 minutes)
            var timeSinceLastInit = DateTime.UtcNow - _lastInitializationTime;
            if (_resourcesInitialized && timeSinceLastInit < TimeSpan.FromMinutes(5))
            {
                logger.Information("Using cached test resources (last init: {Time} ago)", timeSinceLastInit);
                return true;
            }

            lock (_initializationLock)
            {
                // Double-check after acquiring lock
                if (_resourcesInitialized && timeSinceLastInit < TimeSpan.FromMinutes(5))
                {
                    return true;
                }

                logger.Information("Initializing minimal test core...");

                // Call the actual Core.SetupMinimal() method
                global::Server.Core.SetupMinimal(applicationAssembly, process);

                // Mark resources as initialized for caching
                _resourcesInitialized = true;
                _lastInitializationTime = DateTime.UtcNow;

                // Cache key components for status tracking
                _resourceCache["CoreInitialized"] = true;
                _resourceCache["Sphere51aModule"] = Sphere51aModule.IsInitialized;

                logger.Information("Minimal test core initialized successfully");
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to initialize minimal test core");
            return false;
        }
    }

    /// <summary>
    /// Cleans up test core resources.
    /// </summary>
    public static void Cleanup()
    {
        try
        {
            logger.Information("Cleaning up test core...");

            // Flush any pending audit logs
            if (Modules.Sphere51a.Combat.Audit.CombatAuditSystem.IsInitialized)
            {
                Modules.Sphere51a.Combat.Audit.CombatAuditSystem.FlushBuffer().Wait();
            }

            // Clear cached resources (force re-initialization on next run)
            _resourceCache.Clear();
            _resourcesInitialized = false;

            logger.Information("Test core cleanup complete");
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Error during test core cleanup");
        }
    }

    /// <summary>
    /// Gets the status of cached resources.
    /// </summary>
    public static (bool IsInitialized, TimeSpan TimeSinceLastInit, int CachedResourceCount) GetCacheStatus()
    {
        var timeSinceLastInit = DateTime.UtcNow - _lastInitializationTime;
        return (_resourcesInitialized, timeSinceLastInit, _resourceCache.Count);
    }
}
