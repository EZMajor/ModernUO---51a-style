using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Server.Modules.Sphere51a.Testing;

namespace Server;

public class Application
{
    public static void Main(string[] args)
    {
        // Check for test shard mode FIRST (highest priority)
        var isTestShard = args.Contains("--test-shard");
        if (isTestShard)
        {
            RunTestShardMode().Wait();
            return;
        }

        // Check for test mode BEFORE initializing Core
        var isTestMode = args.Contains("--test-mode") || args.Contains("-t");
        var isQuickTest = args.Contains("--quick-test");

        // --quick-test implies test mode
        if (isQuickTest)
        {
            isTestMode = true;
        }

        if (isTestMode)
        {
            // Configure enhanced logging for test mode
            Server.Logging.LogFactory.ConfigureForTestMode();

            // Prepare test environment (backup/generate configs with Sphere51a enabled)
            TestConfigurationManager.PrepareTestEnvironment();

            // Set headless mode to prevent event loop from starting
            Core.HeadlessMode = true;
        }

        // Initialize Core (loads assemblies, world, etc.)
        if (isQuickTest)
        {
            // Use minimal test core for fast testing
            if (!Modules.Sphere51a.Testing.TestCore.SetupMinimal(Assembly.GetEntryAssembly(), Process.GetCurrentProcess()))
            {
                Console.Error.WriteLine("Failed to initialize minimal test core");
                Environment.Exit(2);
            }
        }
        else
        {
            Core.Setup(Assembly.GetEntryAssembly(), Process.GetCurrentProcess());
        }

        // After assemblies are loaded and world is initialized, run test mode if requested
        if (args.Contains("--test-mode") || args.Contains("-t"))
        {
            try
            {
                // Run headless testing framework
                RunTestMode(args);
            }
            finally
            {
                // Always cleanup test environment (restore backups, delete generated files)
                TestConfigurationManager.CleanupTestEnvironment();
            }
            return;
        }

        // Normal server continues (event loop already started by Core.Setup)
    }

    private static async Task RunTestShardMode()
    {
        try
        {
            Console.WriteLine("==========================================");
            Console.WriteLine("  Live Test Shard Mode");
            Console.WriteLine("==========================================");
            Console.WriteLine();

            // Initialize Core for test shard (full initialization)
            Core.Setup(Assembly.GetEntryAssembly(), Process.GetCurrentProcess());

            // Create and run the test shard coordinator
            var coordinator = new TestShardCoordinator();
            await coordinator.RunAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error in test shard mode: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(2); // Fatal error
        }
    }

    private static void RunTestMode(string[] args)
    {
        try
        {
            // Print test framework banner
            Modules.Sphere51a.Testing.TestFramework.PrintBanner();

            // Parse test arguments
            var testArgs = Modules.Sphere51a.Testing.TestFrameworkArguments.Parse(args);

            // Execute tests
            var exitCode = Modules.Sphere51a.Testing.TestFramework.Execute(testArgs);

            // Exit with appropriate code
            Environment.Exit(exitCode);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error in test mode: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(2); // Fatal error
        }
    }
}
