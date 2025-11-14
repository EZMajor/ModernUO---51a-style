using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Server.Logging;

namespace TestRunner;

/// <summary>
/// Standalone test runner utility for local development and debugging.
/// Provides easy access to Sphere51a test scenarios without full server startup.
/// </summary>
public static class Program
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(Program));

    public static async Task Main(string[] args)
    {
        Console.WriteLine("==========================================");
        Console.WriteLine("  Sphere51a Test Runner Utility");
        Console.WriteLine("==========================================");
        Console.WriteLine();

        try
        {
            var options = ParseArguments(args);

            if (options.ShowHelp)
            {
                ShowHelp();
                return;
            }

            if (options.ListScenarios)
            {
                await ListScenariosAsync();
                return;
            }

            await RunTestAsync(options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    private static TestOptions ParseArguments(string[] args)
    {
        var options = new TestOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--scenario":
                case "-s":
                    if (i + 1 < args.Length)
                        options.Scenario = args[++i];
                    break;

                case "--duration":
                case "-d":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var duration))
                        options.Duration = duration;
                    break;

                case "--quick":
                case "-q":
                    options.UseQuickMode = true;
                    break;

                case "--verbose":
                case "-v":
                    options.Verbose = true;
                    break;

                case "--list":
                case "-l":
                    options.ListScenarios = true;
                    break;

                case "--profile":
                case "-p":
                    options.ProfileMode = true;
                    break;

                case "--help":
                case "-h":
                case "/?":
                    options.ShowHelp = true;
                    break;
            }
        }

        return options;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Usage: TestRunner [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -s, --scenario <name>    Run specific test scenario (default: weapon_timing)");
        Console.WriteLine("  -d, --duration <secs>    Test duration in seconds (default: 30)");
        Console.WriteLine("  -q, --quick              Use quick test mode (faster startup)");
        Console.WriteLine("  -v, --verbose            Enable verbose logging");
        Console.WriteLine("  -l, --list               List available test scenarios");
        Console.WriteLine("  -p, --profile            Enable performance profiling");
        Console.WriteLine("  -h, --help               Show this help");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  TestRunner --scenario weapon_timing --duration 60 --verbose");
        Console.WriteLine("  TestRunner --quick --list");
        Console.WriteLine("  TestRunner --profile --scenario weapon_timing");
    }

    private static async Task ListScenariosAsync()
    {
        Console.WriteLine("Available Test Scenarios:");
        Console.WriteLine("=========================");

        // This would normally query the test framework, but for now we'll show known scenarios
        var scenarios = new[]
        {
            ("weapon_timing", "Weapon Swing Timing Test", "Tests weapon swing timing accuracy"),
            ("spell_timing", "Spell Timing Test", "Tests spell casting timing accuracy"),
            ("stress_test", "Combat Stress Test", "Tests combat system under load")
        };

        foreach (var (id, name, description) in scenarios)
        {
            Console.WriteLine($"  {id,-15} {name,-25} {description}");
        }

        Console.WriteLine();
        Console.WriteLine("Use --scenario <id> to run a specific test.");
    }

    private static async Task RunTestAsync(TestOptions options)
    {
        var startTime = DateTime.Now;
        Console.WriteLine($"Starting test execution at {startTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Scenario: {options.Scenario}");
        Console.WriteLine($"Duration: {options.Duration} seconds");
        Console.WriteLine($"Mode: {(options.UseQuickMode ? "Quick" : "Standard")}");
        Console.WriteLine($"Verbose: {options.Verbose}");
        Console.WriteLine($"Profile: {options.ProfileMode}");
        Console.WriteLine();

        // Build command line arguments for the actual test runner
        var testArgs = new List<string>();

        if (options.UseQuickMode)
            testArgs.Add("--quick-test");
        else
            testArgs.Add("--test-mode");

        testArgs.Add("--scenario");
        testArgs.Add(options.Scenario);

        testArgs.Add("--duration");
        testArgs.Add(options.Duration.ToString());

        if (options.Verbose)
            testArgs.Add("--verbose");

        // Find the application executable
        var exePath = GetApplicationExecutable();
        if (string.IsNullOrEmpty(exePath))
        {
            Console.Error.WriteLine("Could not find ModernUO application executable.");
            Console.Error.WriteLine("Make sure the project is built and Distribution/ModernUO.dll exists.");
            Environment.Exit(1);
        }

        // Set up the process
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{exePath}\" {string.Join(" ", testArgs)}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        // Performance profiling
        Stopwatch stopwatch = null;
        if (options.ProfileMode)
        {
            stopwatch = Stopwatch.StartNew();
            Console.WriteLine("Performance profiling enabled...");
        }

        try
        {
            Console.WriteLine("Launching test process...");
            Console.WriteLine($"Command: dotnet \"{exePath}\" {string.Join(" ", testArgs)}");
            Console.WriteLine();

            process.Start();

            // Read output asynchronously
            var outputTask = Task.Run(() => ReadStream(process.StandardOutput));
            var errorTask = Task.Run(() => ReadStream(process.StandardError));

            // Wait for completion with timeout
            var timeout = TimeSpan.FromMinutes(15); // 15 minute timeout
            var completed = await Task.WhenAny(
                Task.Run(() => process.WaitForExit()),
                Task.Delay(timeout)
            );

            if (completed != await Task.WhenAny(Task.Run(() => process.WaitForExit()), Task.Delay(timeout)))
            {
                Console.Error.WriteLine($"Test execution timed out after {timeout.TotalMinutes} minutes");
                process.Kill();
                Environment.Exit(1);
            }

            // Wait for output reading to complete
            var output = await outputTask;
            var error = await errorTask;

            // Display results
            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine("  Test Execution Complete");
            Console.WriteLine("==========================================");
            Console.WriteLine($"Exit Code: {process.ExitCode}");
            Console.WriteLine($"Duration: {duration.TotalSeconds:F1} seconds");

            if (options.ProfileMode && stopwatch != null)
            {
                stopwatch.Stop();
                Console.WriteLine($"Process Time: {stopwatch.Elapsed.TotalSeconds:F1} seconds");
            }

            // Show test results summary
            await ShowTestResultsAsync();

            Environment.Exit(process.ExitCode);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error running test: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static string GetApplicationExecutable()
    {
        // Look for the built application
        var possiblePaths = new[]
        {
            "Distribution/ModernUO.dll",
            "Projects/Application/bin/Release/net9.0/ModernUO.dll",
            "Projects/Application/bin/Debug/net9.0/ModernUO.dll"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
        }

        return null;
    }

    private static async Task<string> ReadStream(StreamReader reader)
    {
        var output = new System.Text.StringBuilder();
        char[] buffer = new char[4096];
        int bytesRead;

        while ((bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            var chunk = new string(buffer, 0, bytesRead);
            Console.Write(chunk); // Echo to console
            output.Append(chunk);
        }

        return output.ToString();
    }

    private static async Task ShowTestResultsAsync()
    {
        try
        {
            var reportPath = "Distribution/AuditReports/Latest_Weapon Swing Timing TestSummary.md";

            if (File.Exists(reportPath))
            {
                Console.WriteLine();
                Console.WriteLine("Test Results Summary:");
                Console.WriteLine("====================");

                var content = await File.ReadAllTextAsync(reportPath);
                var lines = content.Split('\n');

                // Extract key information
                foreach (var line in lines)
                {
                    if (line.Contains("Status:") || line.Contains("Accuracy:") ||
                        line.Contains("Total Swings:") || line.Contains("Duration:"))
                    {
                        Console.WriteLine($"  {line.Trim()}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"Full report: {reportPath}");
            }

            // Show recent log files
            var logDir = "Distribution/AuditReports/Logs";
            if (Directory.Exists(logDir))
            {
                var recentLogs = Directory.GetFiles(logDir, "*.log")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .Take(3);

                if (recentLogs.Any())
                {
                    Console.WriteLine("Recent Log Files:");
                    foreach (var log in recentLogs)
                    {
                        Console.WriteLine($"  {log}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not read test results: {ex.Message}");
        }
    }

    private class TestOptions
    {
        public string Scenario { get; set; } = "weapon_timing";
        public int Duration { get; set; } = 30;
        public bool UseQuickMode { get; set; }
        public bool Verbose { get; set; }
        public bool ListScenarios { get; set; }
        public bool ProfileMode { get; set; }
        public bool ShowHelp { get; set; }
    }
}
