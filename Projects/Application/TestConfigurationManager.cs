using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Server.Json;

namespace Server;

/// <summary>
/// Manages server configuration files for test mode execution.
/// Handles backup, generation, and restoration of configs to ensure
/// tests run with Sphere51a enabled and no interactive prompts.
/// </summary>
public static class TestConfigurationManager
{
    private static readonly List<string> _backedUpFiles = new();
    private static readonly List<string> _generatedFiles = new();
    private static bool _environmentPrepared;

    /// <summary>
    /// Prepares the test environment by ensuring required configuration files exist
    /// with Sphere51a enabled. Backs up existing configs before modification.
    /// </summary>
    public static void PrepareTestEnvironment()
    {
        if (_environmentPrepared)
        {
            Console.WriteLine("[TestConfig] Environment already prepared, skipping");
            return;
        }

        try
        {
            Console.WriteLine("[TestConfig] Preparing test environment...");

            // Use current directory since Core.BaseDirectory isn't initialized yet
            var baseDir = Directory.GetCurrentDirectory();
            var configDir = Path.Combine(baseDir, "Configuration");
            Directory.CreateDirectory(configDir);

            // Ensure modernuo.json exists with Sphere51a enabled
            var modernuoPath = Path.Combine(configDir, "modernuo.json");
            EnsureModernUOConfig(modernuoPath);

            // Ensure expansion.json exists
            var expansionPath = Path.Combine(configDir, "expansion.json");
            EnsureExpansionConfig(expansionPath);

            _environmentPrepared = true;
            Console.WriteLine("[TestConfig] Test environment prepared successfully");
            Console.WriteLine($"[TestConfig] Backed up {_backedUpFiles.Count} file(s)");
            Console.WriteLine($"[TestConfig] Generated {_generatedFiles.Count} file(s)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[TestConfig] ERROR preparing test environment: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Cleans up the test environment by restoring backed up configs
    /// and deleting generated files.
    /// </summary>
    public static void CleanupTestEnvironment()
    {
        if (!_environmentPrepared)
        {
            return;
        }

        try
        {
            Console.WriteLine("[TestConfig] Cleaning up test environment...");

            // Restore backed up files
            foreach (var backupPath in _backedUpFiles)
            {
                var originalPath = backupPath.Replace(".testbak", "");
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, originalPath, true);
                    File.Delete(backupPath);
                    Console.WriteLine($"[TestConfig] Restored: {Path.GetFileName(originalPath)}");
                }
            }

            // Delete generated files
            foreach (var generatedPath in _generatedFiles)
            {
                if (File.Exists(generatedPath))
                {
                    File.Delete(generatedPath);
                    Console.WriteLine($"[TestConfig] Deleted: {Path.GetFileName(generatedPath)}");
                }
            }

            _backedUpFiles.Clear();
            _generatedFiles.Clear();
            _environmentPrepared = false;

            Console.WriteLine("[TestConfig] Test environment cleaned up successfully");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[TestConfig] ERROR during cleanup: {ex.Message}");
        }
    }

    private static void EnsureModernUOConfig(string path)
    {
        if (File.Exists(path))
        {
            // Backup existing config
            var backupPath = path + ".testbak";
            File.Copy(path, backupPath, true);
            _backedUpFiles.Add(backupPath);
            Console.WriteLine($"[TestConfig] Backed up existing: {Path.GetFileName(path)}");

            // Modify existing config to enable Sphere51a
            ModifyExistingConfig(path);
        }
        else
        {
            // Generate new config
            GenerateModernUOConfig(path);
            _generatedFiles.Add(path);
            Console.WriteLine($"[TestConfig] Generated: {Path.GetFileName(path)}");
        }
    }

    private static void ModifyExistingConfig(string path)
    {
        try
        {
            // Read existing config
            var json = File.ReadAllText(path);
            var doc = System.Text.Json.JsonDocument.Parse(json);

            // Check if sphere51a.enabled needs to be set
            bool needsModification = false;
            if (doc.RootElement.TryGetProperty("settings", out var settings))
            {
                if (!settings.TryGetProperty("sphere51a.enabled", out var enabled) ||
                    enabled.GetString() != "true")
                {
                    needsModification = true;
                }
            }
            else
            {
                needsModification = true;
            }

            if (needsModification)
            {
                // Regenerate with Sphere51a enabled
                GenerateModernUOConfig(path);
                Console.WriteLine($"[TestConfig] Modified to enable Sphere51a: {Path.GetFileName(path)}");
            }
            else
            {
                Console.WriteLine($"[TestConfig] Sphere51a already enabled: {Path.GetFileName(path)}");
            }
        }
        catch
        {
            // If parsing fails, regenerate
            GenerateModernUOConfig(path);
        }
    }

    private static void GenerateModernUOConfig(string path)
    {
        // Create minimal config with Sphere51a enabled
        var config = new
        {
            assemblyDirectories = new[] { "./Assemblies" },
            dataDirectories = new[] { @"E:\Ultima Code\Electronic Arts\Ultima Online Classic" },
            listeners = new[] { "0.0.0.0:2593" },
            settings = new Dictionary<string, string>
            {
                ["accountHandler.enableAutoAccountCreation"] = "True",
                ["expansion"] = "EJ",
                ["sphere51a.enabled"] = "true", // KEY: Enable Sphere51a for tests
                ["core.enableIdleCPU"] = "False",
                ["pingServer.enabled"] = "False", // Disable for headless mode
                ["crashGuard.enabled"] = "True",
                ["autosave.enabled"] = "False" // Disable autosave in tests
            }
        };

        // Serialize using System.Text.Json
        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        };
        var jsonString = System.Text.Json.JsonSerializer.Serialize(config, options);
        File.WriteAllText(path, jsonString);
    }

    private static void EnsureExpansionConfig(string path)
    {
        if (File.Exists(path))
        {
            // Backup existing
            var backupPath = path + ".testbak";
            File.Copy(path, backupPath, true);
            _backedUpFiles.Add(backupPath);
            Console.WriteLine($"[TestConfig] Backed up existing: {Path.GetFileName(path)}");
        }
        else
        {
            // Generate new expansion config
            GenerateExpansionConfig(path);
            _generatedFiles.Add(path);
            Console.WriteLine($"[TestConfig] Generated: {Path.GetFileName(path)}");
        }
    }

    private static void GenerateExpansionConfig(string path)
    {
        var config = new
        {
            id = 11, // Endless Journey
            name = "Endless Journey",
            enabledMaps = new[] { "Felucca", "Trammel", "Ilshenar", "Malas", "Tokuno" }
        };

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        };
        var jsonString = System.Text.Json.JsonSerializer.Serialize(config, options);
        File.WriteAllText(path, jsonString);
    }
}
