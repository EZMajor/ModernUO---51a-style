/*************************************************************************
 * ModernUO - Sphere 51a Test Framework Entry Point
 * File: TestFramework.cs
 *
 * Description: Main entry point for headless testing framework.
 *              Initializes minimal server environment and executes tests.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Logging;

namespace Server.Modules.Sphere51a.Testing;

/// <summary>
/// Main entry point for Sphere51a headless testing framework.
/// </summary>
public static class TestFramework
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TestFramework));

    // Caching for optimized test runs
    private static readonly ConcurrentDictionary<string, object> _resourceCache = new();
    private static readonly object _initializationLock = new();
    private static bool _resourcesInitialized;
    private static DateTime _lastInitializationTime;

    /// <summary>
    /// Initializes and runs the test framework.
    /// Returns exit code for CI/CD integration.
    /// </summary>
    public static int Execute(TestFrameworkArguments args)
    {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMinutes(10); // 10 minute timeout for test execution

        try
        {
            // Print configuration
            Console.WriteLine();
            args.PrintSummary();

            // Validate arguments
            var validationErrors = args.Validate();
            if (validationErrors.Count > 0)
            {
                logger.Error("Argument validation failed:");
                foreach (var error in validationErrors)
                {
                    logger.Error("  - {Error}", error);
                }
                return 3; // Config error
            }

            // Load test configuration
            logger.Information("Loading test configuration...");
            var config = TestConfig.Load(args.ConfigPath);

            if (config == null)
            {
                logger.Error("Failed to load test configuration");
                return 3;
            }

            // Apply CLI overrides
            ApplyCommandLineOverrides(config, args);

            // Initialize minimal server environment
            if (!InitializeTestEnvironment())
            {
                logger.Error("Failed to initialize test environment");
                return 2; // Fatal error
            }

            // Verify integration status and warn about missing features
            DisplayIntegrationStatus(args);

            // Create and run test runner with timeout protection
            logger.Information("Starting test execution with {Timeout} timeout...", timeout);
            var runner = new TestRunner(config, args);

            var exitCode = 2; // Default to fatal error
            var runnerTask = Task.Run(() => runner.Run());
            var timeoutTask = Task.Delay(timeout);

            var completedTask = Task.WhenAny(runnerTask, timeoutTask).Result;

            if (completedTask == timeoutTask)
            {
                logger.Error("Test execution timed out after {Timeout}", timeout);
                logger.Error("This may indicate an infinite loop or deadlock in test code");

                // Try to save partial results
                SaveTimeoutDiagnosticInfo(config, args, startTime);

                return 4; // Timeout error
            }

            exitCode = runnerTask.Result;

            // Check for execution time warnings
            var executionTime = DateTime.UtcNow - startTime;
            if (executionTime > TimeSpan.FromMinutes(5))
            {
                logger.Warning("Test execution took {Time}, consider optimizing for faster runs", executionTime);
            }

            // Cleanup
            Cleanup();

            if (!args.AutoExit)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }

            return exitCode;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Fatal error in test framework");

            // Save crash diagnostic info
            SaveCrashDiagnosticInfo(args, startTime, ex);

            return 2;
        }
    }

    /// <summary>
    /// Saves diagnostic information when test execution times out.
    /// </summary>
    private static void SaveTimeoutDiagnosticInfo(TestConfig config, TestFrameworkArguments args, DateTime startTime)
    {
        try
        {
            var diagnosticPath = Path.Combine(
                global::Server.Core.BaseDirectory,
                "Distribution",
                "AuditReports",
                "Recovery",
                $"timeout-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(diagnosticPath));

            var diagnostic = new StringBuilder();
            diagnostic.AppendLine("Test Timeout Diagnostic Report");
            diagnostic.AppendLine("=============================");
            diagnostic.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss UTC}");
            diagnostic.AppendLine($"Start Time: {startTime:yyyy-MM-dd HH:mm:ss UTC}");
            diagnostic.AppendLine($"Duration: {DateTime.UtcNow - startTime}");
            diagnostic.AppendLine($"Scenario: {args.Scenario ?? "All"}");
            diagnostic.AppendLine($"Config Path: {args.ConfigPath}");
            diagnostic.AppendLine();
            diagnostic.AppendLine("Possible Causes:");
            diagnostic.AppendLine("- Infinite loop in test code");
            diagnostic.AppendLine("- Deadlock in combat system");
            diagnostic.AppendLine("- Memory exhaustion");
            diagnostic.AppendLine("- File I/O blocking");
            diagnostic.AppendLine();
            diagnostic.AppendLine("Recommendations:");
            diagnostic.AppendLine("- Run with --verbose for more details");
            diagnostic.AppendLine("- Check Distribution/AuditReports/Logs/ for recent logs");
            diagnostic.AppendLine("- Reduce test duration with --duration parameter");
            diagnostic.AppendLine("- Run individual scenarios to isolate issues");

            File.WriteAllText(diagnosticPath, diagnostic.ToString());
            logger.Information("Timeout diagnostic saved to: {Path}", diagnosticPath);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to save timeout diagnostic");
        }
    }

    /// <summary>
    /// Saves diagnostic information when test framework crashes.
    /// </summary>
    private static void SaveCrashDiagnosticInfo(TestFrameworkArguments args, DateTime startTime, Exception ex)
    {
        try
        {
            var diagnosticPath = Path.Combine(
                global::Server.Core.BaseDirectory,
                "Distribution",
                "AuditReports",
                "Recovery",
                $"crash-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(diagnosticPath));

            var diagnostic = new StringBuilder();
            diagnostic.AppendLine("Test Framework Crash Report");
            diagnostic.AppendLine("===========================");
            diagnostic.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss UTC}");
            diagnostic.AppendLine($"Start Time: {startTime:yyyy-MM-dd HH:mm:ss UTC}");
            diagnostic.AppendLine($"Duration: {DateTime.UtcNow - startTime}");
            diagnostic.AppendLine($"Scenario: {args.Scenario ?? "All"}");
            diagnostic.AppendLine($"Config Path: {args.ConfigPath}");
            diagnostic.AppendLine();
            diagnostic.AppendLine("Exception Details:");
            diagnostic.AppendLine(ex.ToString());

            File.WriteAllText(diagnosticPath, diagnostic.ToString());
            logger.Information("Crash diagnostic saved to: {Path}", diagnosticPath);
        }
        catch (Exception diagEx)
        {
            logger.Error(diagEx, "Failed to save crash diagnostic");
        }
    }

    /// <summary>
    /// Displays integration status for all Sphere51a systems.
    /// Warns about missing integrations that will cause tests to fail.
    /// </summary>
    private static void DisplayIntegrationStatus(TestFrameworkArguments args)
    {
        try
        {
            logger.Information("Checking Sphere51a integration status...");
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.WriteLine("  Integration Status Check");
            Console.WriteLine("═══════════════════════════════════════════════════");

            var status = IntegrationVerifier.GetIntegrationStatus();

            // Display status with color coding (if console supports it)
            Console.WriteLine();
            Console.WriteLine($"  Weapon Combat: {(status.WeaponIntegrationActive ? "✓ ACTIVE" : "✗ NOT IMPLEMENTED")}");
            Console.WriteLine($"    {status.WeaponStatusMessage}");
            Console.WriteLine();
            Console.WriteLine($"  Spell System:  {(status.SpellIntegrationActive ? "✓ ACTIVE" : "✗ NOT IMPLEMENTED")}");
            Console.WriteLine($"    {status.SpellStatusMessage}");
            Console.WriteLine();

            // Warn about disabled tests
            if (!status.SpellIntegrationActive)
            {
                Console.WriteLine("  ⚠ WARNING: Spell integration not implemented");
                Console.WriteLine("    - Spell timing tests will FAIL if enabled");
                Console.WriteLine("    - Spell tests are DISABLED in test-config.json");
                Console.WriteLine("    - See SPELL_ARCHITECTURE.md for requirements");
                Console.WriteLine();
            }

            if (!status.WeaponIntegrationActive)
            {
                Console.WriteLine("  ⚠ WARNING: Weapon integration not implemented");
                Console.WriteLine("    - Weapon timing tests will FAIL if enabled");
                Console.WriteLine();
            }

            if (status.AllIntegrationsActive)
            {
                Console.WriteLine("  ✓ All integrations active - full test suite available");
                Console.WriteLine();
            }
            else if (!status.AnyIntegrationActive)
            {
                Console.WriteLine("  ✗ CRITICAL: No integrations active");
                Console.WriteLine("    - This indicates Sphere51a is not connected to core systems");
                Console.WriteLine("    - All tests will fail");
                Console.WriteLine();
            }

            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.WriteLine();

            // Log to file as well
            logger.Information(status.ToString());
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Error checking integration status");
        }
    }

    /// <summary>
    /// Applies command-line argument overrides to configuration.
    /// </summary>
    private static void ApplyCommandLineOverrides(TestConfig config, TestFrameworkArguments args)
    {
        if (args.DurationSeconds.HasValue)
        {
            logger.Information("Overriding test duration: {Duration}s", args.DurationSeconds.Value);

            if (config.Scenarios.WeaponTiming != null)
                config.Scenarios.WeaponTiming.DurationSeconds = args.DurationSeconds.Value;

            if (config.Scenarios.SpellTiming != null)
                config.Scenarios.SpellTiming.DurationSeconds = args.DurationSeconds.Value;

            if (config.Scenarios.StressTest != null)
                config.Scenarios.StressTest.DurationSeconds = args.DurationSeconds.Value;
        }

        if (args.Verbose)
        {
            config.TestSettings.EnableDetailedLogging = true;
            logger.Information("Verbose logging enabled");
        }
    }

    /// <summary>
    /// Initializes minimal server environment for testing.
    /// Skips Network, Account, and AI systems.
    /// </summary>
    private static bool InitializeTestEnvironment()
    {
        try
        {
            logger.Information("Initializing test environment...");

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

                // Initialize core systems needed for testing
                // Note: ModernUO's Core.Setup has already initialized basic systems
                logger.Information("Core systems initialized");

                // Parallel initialization of independent systems
                var initTasks = new List<Task<bool>>();

                // Initialize Sphere51a Module (if not already done)
                var sphereTask = Task.Run(() =>
                {
                    try
                    {
                        if (!Sphere51aModule.IsInitialized)
                        {
                            logger.Information("Initializing Sphere51a module...");
                            Sphere51aModule.Initialize();
                            _resourceCache["Sphere51aModule"] = true;
                            return true;
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to initialize Sphere51a module");
                        return false;
                    }
                });
                initTasks.Add(sphereTask);

                // Initialize Combat Audit System (if configured)
                var auditTask = Task.Run(() =>
                {
                    try
                    {
                        var auditConfig = Configuration.SphereConfiguration.Audit;
                        if (auditConfig?.Enabled == true)
                        {
                            if (!Modules.Sphere51a.Combat.Audit.CombatAuditSystem.IsInitialized)
                            {
                                logger.Information("Initializing Combat Audit System...");
                                Modules.Sphere51a.Combat.Audit.CombatAuditSystem.Initialize(auditConfig);
                                _resourceCache["CombatAuditSystem"] = true;
                                return true;
                            }
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to initialize Combat Audit System");
                        return false;
                    }
                });
                initTasks.Add(auditTask);

                // Wait for all initialization tasks to complete
                var results = Task.WhenAll(initTasks).Result;

                if (results.All(r => r))
                {
                    _resourcesInitialized = true;
                    _lastInitializationTime = DateTime.UtcNow;
                    logger.Information("Test environment initialized successfully (with caching)");
                    return true;
                }
                else
                {
                    logger.Error("Some test environment components failed to initialize");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to initialize test environment");
            return false;
        }
    }

    /// <summary>
    /// Cleanup test environment.
    /// </summary>
    private static void Cleanup()
    {
        try
        {
            logger.Information("Cleaning up test environment...");

            // Flush audit logs if enabled
            if (Modules.Sphere51a.Combat.Audit.CombatAuditSystem.IsInitialized)
            {
                logger.Information("Flushing audit logs...");
                Modules.Sphere51a.Combat.Audit.CombatAuditSystem.FlushBuffer().Wait();
            }

            logger.Information("Cleanup complete");
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Error during cleanup");
        }
    }

    /// <summary>
    /// Prints test framework banner.
    /// </summary>
    public static void PrintBanner()
    {
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════╗");
        Console.WriteLine("║   Sphere51a Headless Testing Framework            ║");
        Console.WriteLine("║   ModernUO Combat & Magic Verification System     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }
}
