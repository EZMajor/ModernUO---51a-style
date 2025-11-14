using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Server.Logging;

namespace Server.Modules.Sphere51a.Testing;

/// <summary>
/// Non-blocking UO files directory resolver that never freezes or hangs.
/// Uses environment variables, config files, auto-detection, and guaranteed fallback.
/// </summary>
public static class UOPathResolver
{
    private const string CONFIG_FILE = "uo_path_config.json";
    private const string ENV_VAR = "MODERNUO_UO_PATH";
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(UOPathResolver));

    /// <summary>
    /// Resolves the UO files directory using a 4-layer non-blocking approach.
    /// Never hangs, never crashes, always returns a valid path.
    /// </summary>
    public static string ResolveUOPath()
    {
        // 1. ENV VAR (fastest)
        var env = Environment.GetEnvironmentVariable(ENV_VAR);
        if (IsValidDirectory(env))
        {
            logger.Information("UO path resolved from environment variable: {Path}", env);
            return env;
        }

        // 2. Config file
        var cfg = LoadConfig();
        if (IsValidDirectory(cfg))
        {
            logger.Information("UO path resolved from config file: {Path}", cfg);
            return cfg;
        }

        // 3. Auto-detect (directory-only checks)
        var auto = AutoDetectFast();
        if (IsValidDirectory(auto))
        {
            SaveConfig(auto);
            logger.Information("UO path auto-detected and cached: {Path}", auto);
            return auto;
        }

        // 4. Fallback folder (never fails)
        var fallback = GetFallbackPath();
        logger.Warning("Using fallback UO path (no valid UO installation found): {Path}", fallback);
        return fallback;
    }

    /// <summary>
    /// Validates if a path is a valid directory (fast, non-blocking).
    /// </summary>
    private static bool IsValidDirectory(string path)
        => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);

    /// <summary>
    /// Auto-detects UO installation in common locations (directory existence only).
    /// </summary>
    private static string AutoDetectFast()
    {
        string[] candidates =
        {
            @"C:\Program Files (x86)\Electronic Arts\Ultima Online Classic",
            @"C:\Program Files\Electronic Arts\Ultima Online Classic",
            @"C:\Games\Ultima Online",
            @"C:\UO"
        };

        return candidates.FirstOrDefault(Directory.Exists);
    }

    /// <summary>
    /// Loads UO path from local config file.
    /// </summary>
    private static string LoadConfig()
    {
        var file = Path.Combine(global::Server.Core.BaseDirectory, CONFIG_FILE);
        if (!File.Exists(file)) return null;

        try
        {
            var json = File.ReadAllText(file);
            return JsonSerializer.Deserialize<UOConfig>(json)?.UOFilesDirectory;
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to load UO path config file");
            return null;
        }
    }

    /// <summary>
    /// Saves UO path to local config file for future use.
    /// </summary>
    private static void SaveConfig(string path)
    {
        var file = Path.Combine(global::Server.Core.BaseDirectory, CONFIG_FILE);
        var json = JsonSerializer.Serialize(
            new UOConfig { UOFilesDirectory = path },
            new JsonSerializerOptions { WriteIndented = true });

        try
        {
            File.WriteAllText(file, json);
            logger.Debug("UO path config saved: {Path}", file);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to save UO path config file");
        }
    }

    /// <summary>
    /// Creates and returns a guaranteed fallback directory that always exists.
    /// </summary>
    private static string GetFallbackPath()
    {
        var fallback = Path.Combine(global::Server.Core.BaseDirectory, "TestData", "EmptyUO");
        Directory.CreateDirectory(fallback);

        var markerFile = Path.Combine(fallback, "FALLBACK_MODE.txt");
        File.WriteAllText(markerFile, "Fallback UO dir used. No MUL files detected.");

        return fallback;
    }

    /// <summary>
    /// Configuration class for UO path storage.
    /// </summary>
    private class UOConfig
    {
        public string UOFilesDirectory { get; set; }
    }
}
