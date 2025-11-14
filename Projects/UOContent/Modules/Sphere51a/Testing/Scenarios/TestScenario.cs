/*************************************************************************
 * ModernUO - Sphere 51a Test Scenario Base
 * File: TestScenario.cs
 *
 * Description: Abstract base class for test scenarios.
 *              Uses single-threaded Timer-based execution model.
 *
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Modules.Sphere51a.Testing.Reports;

namespace Server.Modules.Sphere51a.Testing.Scenarios;

/// <summary>
/// Abstract base class for all test scenarios.
/// Implements single-threaded Timer-based execution pattern.
/// </summary>
public abstract class TestScenario
{
    protected static readonly ILogger logger = LogFactory.GetLogger(typeof(TestScenario));

    /// <summary>
    /// Unique identifier for this scenario.
    /// </summary>
    public abstract string ScenarioId { get; }

    /// <summary>
    /// Human-readable name for this scenario.
    /// </summary>
    public abstract string ScenarioName { get; }

    /// <summary>
    /// Test configuration.
    /// </summary>
    public TestConfig Config { get; set; }

    /// <summary>
    /// Test start time.
    /// </summary>
    public DateTime StartTime { get; protected set; }

    /// <summary>
    /// Test end time.
    /// </summary>
    public DateTime EndTime { get; protected set; }

    /// <summary>
    /// Whether the test is currently running.
    /// </summary>
    public bool IsRunning { get; protected set; }

    /// <summary>
    /// Test results summary.
    /// </summary>
    public AuditResultSummary Results { get; protected set; }

    /// <summary>
    /// Test mobiles created during execution (for cleanup).
    /// </summary>
    protected List<Mobile> TestMobiles { get; } = new List<Mobile>();

    /// <summary>
    /// Timer for test execution.
    /// </summary>
    protected Timer ExecutionTimer { get; set; }

    /// <summary>
    /// Elapsed ticks since test start.
    /// </summary>
    protected long ElapsedTicks => IsRunning ? (global::Server.Core.TickCount - _startTick) : 0;
    private long _startTick;

    /// <summary>
    /// Initializes the test scenario with configuration.
    /// </summary>
    public virtual void Initialize(TestConfig config)
    {
        Config = config;
        Results = new AuditResultSummary
        {
            TestType = ScenarioName,
            BuildVersion = GetBuildVersion(),
            ExecutionMode = "Headless"
        };
    }

    /// <summary>
    /// Runs the test scenario to completion.
    /// Returns true if test passed, false otherwise.
    /// </summary>
    public bool Execute()
    {
        try
        {
            logger.Information("Starting test scenario: {Scenario}", ScenarioName);

            StartTime = DateTime.UtcNow;
            Results.StartTime = StartTime;
            _startTick = global::Server.Core.TickCount;
            IsRunning = true;

            // Setup phase
            if (!Setup())
            {
                logger.Error("Test scenario setup failed: {Scenario}", ScenarioName);
                IsRunning = false;
                return false;
            }

            // Warmup phase
            if (Config.TestSettings.WarmupSeconds > 0)
            {
                logger.Information("Warmup phase: {Duration}s", Config.TestSettings.WarmupSeconds);
                System.Threading.Thread.Sleep(Config.TestSettings.WarmupSeconds * 1000);
            }

            // Execute test using Timer pattern (single-threaded)
            RunTest();

            // Wait for completion (timer-based, not blocking)
            while (IsRunning)
            {
                System.Threading.Thread.Sleep(100);
            }

            // Teardown phase
            Teardown();

            EndTime = DateTime.UtcNow;
            Results.EndTime = EndTime;

            // Analyze results
            AnalyzeResults();

            logger.Information("Test scenario completed: {Scenario} - {Status}",
                ScenarioName,
                Results.Passed ? "PASSED" : "FAILED");

            return Results.Passed;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Fatal error during test scenario execution: {Scenario}", ScenarioName);
            IsRunning = false;
            return false;
        }
    }

    /// <summary>
    /// Setup phase: Create test mobiles and environment.
    /// </summary>
    protected abstract bool Setup();

    /// <summary>
    /// Execution phase: Run the actual test using Timer pattern.
    /// </summary>
    protected abstract void RunTest();

    /// <summary>
    /// Teardown phase: Clean up test mobiles and resources.
    /// </summary>
    protected virtual void Teardown()
    {
        // Stop execution timer if still running
        ExecutionTimer?.Stop();

        // Clean up all test mobiles
        foreach (var mobile in TestMobiles)
        {
            TestMobileFactory.Cleanup(mobile);
        }

        TestMobiles.Clear();
        logger.Debug("Test scenario cleanup completed: {Scenario}", ScenarioName);
    }

    /// <summary>
    /// Analysis phase: Compute statistics and determine pass/fail.
    /// </summary>
    protected abstract void AnalyzeResults();

    /// <summary>
    /// Stops the test execution.
    /// </summary>
    protected virtual void StopTest()
    {
        IsRunning = false;
        ExecutionTimer?.Stop();
    }

    /// <summary>
    /// Gets the build version string.
    /// </summary>
    protected string GetBuildVersion()
    {
        try
        {
            var version = typeof(global::Server.Core).Assembly.GetName().Version;
            return $"v{version.Major}.{version.Minor}.{version.Build}";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Populates environment information in results.
    /// </summary>
    protected void PopulateEnvironmentInfo()
    {
        Results.Environment = new EnvironmentInfo
        {
            Platform = Environment.OSVersion.Platform.ToString(),
            OSVersion = Environment.OSVersion.VersionString,
            ProcessorCount = Environment.ProcessorCount,
            DotNetVersion = Environment.Version.ToString(),
            ServerVersion = GetBuildVersion(),
            AuditSystemEnabled = Modules.Sphere51a.Combat.Audit.CombatAuditSystem.IsInitialized,
            AuditLevel = Modules.Sphere51a.Combat.Audit.CombatAuditSystem.Config?.Level.ToString() ?? "None",
            ShadowModeEnabled = Modules.Sphere51a.Combat.Audit.CombatAuditSystem.Config?.EnableShadowMode ?? false
        };
    }

    /// <summary>
    /// Gets baseline metrics for this scenario from config.
    /// </summary>
    protected BaselineMetrics GetBaseline()
    {
        if (Config?.Baselines == null)
            return null;

        // Map scenario ID to baseline key
        var baselineKey = ScenarioId switch
        {
            "weapon_timing" => "weapon_swing_timing",
            "spell_timing" => "spell_timing",
            "stress_test" => "stress_test",
            _ => null
        };

        if (baselineKey != null && Config.Baselines.TryGetValue(baselineKey, out var baseline))
        {
            return baseline;
        }

        return null;
    }

    /// <summary>
    /// Logs progress message if verbose mode enabled.
    /// </summary>
    protected void LogVerbose(string message, params object[] args)
    {
        if (Config?.TestSettings.EnableDetailedLogging == true)
        {
            logger.Debug(message, args);
        }
    }
}
