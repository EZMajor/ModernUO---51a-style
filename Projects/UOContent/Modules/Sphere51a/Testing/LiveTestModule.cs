using System;
using System.Threading.Tasks;
using Server.Logging;
using Server.Modules.Sphere51a.Testing.IPC;

namespace Server.Modules.Sphere51a.Testing;

/// <summary>
/// Base class for live test modules that run inside the test shard.
/// Provides common functionality for test execution, result collection, and IPC communication.
/// </summary>
public abstract class LiveTestModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(LiveTestModule));

    protected NamedPipeProtocol Pipe { get; private set; }
    protected TestResultCollector Results { get; private set; }
    protected bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets the unique identifier for this test scenario.
    /// </summary>
    public abstract string TestId { get; }

    /// <summary>
    /// Gets the display name for this test scenario.
    /// </summary>
    public virtual string TestName => TestId;

    /// <summary>
    /// Gets the description of what this test validates.
    /// </summary>
    public virtual string Description => $"{TestName} test scenario";

    /// <summary>
    /// Initializes the test module with IPC communication.
    /// </summary>
    /// <param name="pipe">The named pipe protocol for communication.</param>
    public void Initialize(NamedPipeProtocol pipe)
    {
        Pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
        Results = new TestResultCollector(TestId);
        IsInitialized = true;

        logger.Debug("Initialized test module: {TestId}", TestId);
    }

    /// <summary>
    /// Executes the test scenario.
    /// </summary>
    /// <returns>A task representing the test execution.</returns>
    public async Task ExecuteAsync()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("Test module not initialized");
        }

        var startTime = DateTime.UtcNow;
        logger.Information("Starting test execution: {TestId}", TestId);

        try
        {
            // Send progress update
            await SendProgressAsync(0, "Initializing test...");

            // Setup phase
            if (!await SetupAsync())
            {
                await SendTestFailedAsync("Setup failed");
                return;
            }

            await SendProgressAsync(10, "Setup complete, running test...");

            // Run the actual test
            await RunTestAsync();

            await SendProgressAsync(90, "Test execution complete, analyzing results...");

            // Analyze results
            await AnalyzeResultsAsync();

            await SendProgressAsync(95, "Analysis complete, finalizing...");

            // Send final results
            await SendTestCompleteAsync();

            var duration = DateTime.UtcNow - startTime;
            logger.Information("Test completed successfully: {TestId} in {Duration}", TestId, duration);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Test execution failed: {TestId}", TestId);
            await SendTestFailedAsync($"Test execution error: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets up the test environment and prerequisites.
    /// </summary>
    /// <returns>True if setup was successful, false otherwise.</returns>
    protected virtual Task<bool> SetupAsync()
    {
        // Default implementation - override in derived classes
        return Task.FromResult(true);
    }

    /// <summary>
    /// Runs the actual test logic.
    /// </summary>
    /// <returns>A task representing the test execution.</returns>
    protected abstract Task RunTestAsync();

    /// <summary>
    /// Analyzes the test results and determines pass/fail status.
    /// </summary>
    /// <returns>A task representing the analysis.</returns>
    protected virtual Task AnalyzeResultsAsync()
    {
        // Default implementation - override in derived classes
        Results.Passed = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleans up test resources.
    /// </summary>
    /// <returns>A task representing the cleanup.</returns>
    protected virtual Task CleanupAsync()
    {
        // Default implementation - override in derived classes
        return Task.CompletedTask;
    }

    /// <summary>
    /// Sends a progress update message.
    /// </summary>
    /// <param name="progressPercent">Progress percentage (0-100).</param>
    /// <param name="status">Current status message.</param>
    /// <returns>A task representing the message send.</returns>
    protected async Task SendProgressAsync(int progressPercent, string status)
    {
        try
        {
            var progress = new TestProgressPayload
            {
                Phase = TestName,
                ProgressPercent = Math.Clamp(progressPercent, 0, 100),
                Status = status,
                ElapsedSeconds = 0 // Could track this if needed
            };

            var message = TestShardMessage.Create(Server.Modules.Sphere51a.Testing.IPC.MessageType.TestProgress, progress);
            await Pipe.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to send progress update");
        }
    }

    /// <summary>
    /// Sends a test completion message with results.
    /// </summary>
    /// <returns>A task representing the message send.</returns>
    private async Task SendTestCompleteAsync()
    {
        try
        {
            var result = new TestCompletePayload
            {
                TestId = TestId,
                Passed = Results.Passed,
                ExecutionTimeSeconds = Results.ExecutionTime.TotalSeconds,
                Results = Results.GetTestResults(),
                FailureReasons = ((System.Collections.Generic.List<string>)Results.FailureReasons).ToArray(),
                Observations = ((System.Collections.Generic.List<string>)Results.Observations).ToArray()
            };

            var message = TestShardMessage.Create(Server.Modules.Sphere51a.Testing.IPC.MessageType.TestComplete, result);
            await Pipe.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to send test completion message");
            await SendTestFailedAsync("Failed to send results");
        }
    }

    /// <summary>
    /// Sends a test failure message.
    /// </summary>
    /// <param name="reason">The reason for test failure.</param>
    /// <returns>A task representing the message send.</returns>
    private async Task SendTestFailedAsync(string reason)
    {
        try
        {
            var error = new IPC.ErrorPayload
            {
                Code = "TEST_FAILED",
                Message = reason
            };

            var message = TestShardMessage.Create(Server.Modules.Sphere51a.Testing.IPC.MessageType.TestFailed, error);
            await Pipe.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to send test failure message");
        }
    }
}
