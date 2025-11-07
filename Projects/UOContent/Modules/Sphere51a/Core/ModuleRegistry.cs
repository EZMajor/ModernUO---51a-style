/*************************************************************************
 * ModernUO - Sphere 51a Combat System (Modular)
 * File: ModuleRegistry.cs
 *
 * Description: Global registry for module detection and management.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.Modules.Sphere51a.Core;

public static class ModuleRegistry
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ModuleRegistry));

    private static readonly Dictionary<string, object> _registeredModules = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a module with the global registry.
    /// </summary>
    public static void Register(string moduleName, object moduleInstance)
    {
        if (string.IsNullOrEmpty(moduleName))
        {
            throw new ArgumentException("Module name cannot be null or empty", nameof(moduleName));
        }

        if (moduleInstance == null)
        {
            throw new ArgumentNullException(nameof(moduleInstance));
        }

        _registeredModules[moduleName] = moduleInstance;
        logger.Information($"Module '{moduleName}' registered successfully");
    }

    /// <summary>
    /// Unregisters a module from the global registry.
    /// </summary>
    public static void Unregister(string moduleName)
    {
        if (string.IsNullOrEmpty(moduleName))
        {
            return;
        }

        if (_registeredModules.Remove(moduleName))
        {
            logger.Information($"Module '{moduleName}' unregistered successfully");
        }
    }

    /// <summary>
    /// Checks if a module is registered and loaded.
    /// </summary>
    public static bool IsModuleLoaded(string moduleName)
    {
        if (string.IsNullOrEmpty(moduleName))
        {
            return false;
        }

        return _registeredModules.ContainsKey(moduleName);
    }

    /// <summary>
    /// Gets a registered module instance.
    /// </summary>
    public static T GetModule<T>(string moduleName) where T : class
    {
        if (string.IsNullOrEmpty(moduleName))
        {
            return null;
        }

        if (_registeredModules.TryGetValue(moduleName, out var instance))
        {
            return instance as T;
        }

        return null;
    }

    /// <summary>
    /// Gets all registered module names.
    /// </summary>
    public static IEnumerable<string> GetRegisteredModules()
    {
        return _registeredModules.Keys;
    }

    /// <summary>
    /// Clears all registered modules (used for testing or shutdown).
    /// </summary>
    public static void Clear()
    {
        _registeredModules.Clear();
        logger.Information("Module registry cleared");
    }

    /// <summary>
    /// Resets registry state for testing purposes.
    /// </summary>
    internal static void ResetForTesting()
    {
        _registeredModules.Clear();
    }
}
