using System;
using System.IO;
using System.Linq;
using Server.Logging;

namespace Server.Modules.Sphere51a.Testing;

/// <summary>
/// Creates disposable test shard environments for live testing.
/// Generates isolated copies of ModernUO with test-specific configurations.
/// </summary>
public static class BuildTestEnvironment
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BuildTestEnvironment));

    /// <summary>
    /// Creates a complete disposable test shard environment.
    /// </summary>
    /// <param name="sourceDistribution">Path to source Distribution directory (default: "Distribution")</param>
    /// <returns>Path to the created test shard directory</returns>
    public static string CreateTestShard(string sourceDistribution = "Distribution")
    {
        var baseDir = global::Server.Core.BaseDirectory;
        var sourcePath = Path.Combine(baseDir, sourceDistribution);

        if (!Directory.Exists(sourcePath))
        {
            throw new DirectoryNotFoundException($"Source distribution not found: {sourcePath}");
        }

        var testShardPath = Path.Combine(baseDir, "Build", "TestShard");
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var uniqueTestShardPath = $"{testShardPath}_{timestamp}";

        logger.Information("Creating test shard environment at: {Path}", uniqueTestShardPath);

        try
        {
            // Clean up any existing test shards (keep last 3 for debugging)
            CleanupOldTestShards(testShardPath);

            // Create fresh test shard directory
            Directory.CreateDirectory(uniqueTestShardPath);
            logger.Debug("Created test shard directory");

            // Copy ModernUO binaries
            CopyBinaries(sourcePath, uniqueTestShardPath);

            // Copy UOContent assemblies
            CopyUOContentAssemblies(uniqueTestShardPath);

            // Generate test configurations
            TestConfigurationGenerator.GenerateAll(uniqueTestShardPath);

            logger.Information("Test shard environment created successfully");
            return uniqueTestShardPath;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to create test shard environment");

            // Clean up on failure
            if (Directory.Exists(uniqueTestShardPath))
            {
                Directory.Delete(uniqueTestShardPath, true);
            }

            throw;
        }
    }

    /// <summary>
    /// Cleans up old test shard directories, keeping only the most recent ones.
    /// </summary>
    private static void CleanupOldTestShards(string baseTestShardPath)
    {
        try
        {
            var baseDir = Path.GetDirectoryName(baseTestShardPath);
            if (!Directory.Exists(baseDir)) return;

            var testShardDirs = Directory.GetDirectories(baseDir, "TestShard_*")
                .OrderByDescending(d => Directory.GetCreationTime(d))
                .Skip(3) // Keep 3 most recent
                .ToArray();

            foreach (var oldDir in testShardDirs)
            {
                try
                {
                    Directory.Delete(oldDir, true);
                    logger.Debug("Cleaned up old test shard: {Path}", Path.GetFileName(oldDir));
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Failed to cleanup old test shard: {Path}", oldDir);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Error during test shard cleanup");
        }
    }

    /// <summary>
    /// Copies ModernUO binaries to the test shard.
    /// </summary>
    private static void CopyBinaries(string sourcePath, string testShardPath)
    {
        logger.Debug("Copying ModernUO binaries...");

        var binariesToCopy = new[]
        {
            "ModernUO.dll",
            "ModernUO.exe",
            "ModernUO.runtimeconfig.json",
            "ModernUO.deps.json"
        };

        foreach (var binary in binariesToCopy)
        {
            var sourceFile = Path.Combine(sourcePath, binary);
            if (File.Exists(sourceFile))
            {
                var destFile = Path.Combine(testShardPath, binary);
                File.Copy(sourceFile, destFile, true);
                logger.Debug("Copied: {File}", binary);
            }
            else
            {
                logger.Warning("Binary not found: {File}", binary);
            }
        }

        // Copy configuration directory structure
        var sourceConfig = Path.Combine(sourcePath, "Configuration");
        if (Directory.Exists(sourceConfig))
        {
            var destConfig = Path.Combine(testShardPath, "Configuration");
            CopyDirectory(sourceConfig, destConfig);
            logger.Debug("Copied configuration directory");
        }

        // Copy Data directory (for Sphere51a data files)
        var sourceData = Path.Combine(sourcePath, "Data");
        if (Directory.Exists(sourceData))
        {
            var destData = Path.Combine(testShardPath, "Data");
            CopyDirectory(sourceData, destData);
            logger.Debug("Copied data directory");
        }
    }

    /// <summary>
    /// Copies UOContent assemblies to the test shard.
    /// </summary>
    private static void CopyUOContentAssemblies(string testShardPath)
    {
        logger.Debug("Copying UOContent assemblies...");

        var assembliesDir = Path.Combine(testShardPath, "Assemblies");
        Directory.CreateDirectory(assembliesDir);

        var sourceAssemblies = Path.Combine(global::Server.Core.BaseDirectory, "Distribution", "Assemblies");
        if (Directory.Exists(sourceAssemblies))
        {
            var assembliesToCopy = new[]
            {
                "UOContent.dll",
                "UOContent.deps.json",
                "UOContent.pdb"
            };

            foreach (var assembly in assembliesToCopy)
            {
                var sourceFile = Path.Combine(sourceAssemblies, assembly);
                if (File.Exists(sourceFile))
                {
                    var destFile = Path.Combine(assembliesDir, assembly);
                    File.Copy(sourceFile, destFile, true);
                    logger.Debug("Copied assembly: {File}", assembly);
                }
            }
        }
        else
        {
            logger.Warning("Source assemblies directory not found: {Path}", sourceAssemblies);
        }
    }

    /// <summary>
    /// Recursively copies a directory.
    /// </summary>
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, destSubDir);
        }
    }

    /// <summary>
    /// Cleans up a test shard environment after testing is complete.
    /// </summary>
    public static void CleanupTestShard(string testShardPath)
    {
        if (string.IsNullOrEmpty(testShardPath) || !Directory.Exists(testShardPath))
        {
            return;
        }

        try
        {
            // Give a moment for processes to fully terminate
            System.Threading.Thread.Sleep(1000);

            Directory.Delete(testShardPath, true);
            logger.Information("Cleaned up test shard: {Path}", testShardPath);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to cleanup test shard: {Path}", testShardPath);
        }
    }
}
