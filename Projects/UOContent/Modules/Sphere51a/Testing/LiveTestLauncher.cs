using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Server.Logging;
using Server.Modules.Sphere51a.Testing.IPC;

namespace Server.Modules.Sphere51a.Testing;

/// <summary>
/// Launches and manages live test shard child processes.
/// Handles IPC communication and graceful shutdown.
/// </summary>
public class LiveTestLauncher : IDisposable
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(LiveTestLauncher));

    private Process _testProcess;
    private NamedPipeProtocol _pipe;
    private string _testShardPath;
    private bool _isDisposed;

    /// <summary>
    /// Result of a test shard launch attempt.
    /// </summary>
    public enum LaunchResult
    {
        Success,
        Failed,
        Timeout,
        AlreadyRunning
    }

    /// <summary>
    /// Launches a test shard and establishes IPC communication.
    /// </summary>
    /// <param name="testShardPath">Path to the prepared test shard directory</param>
    /// <param name="timeoutSeconds">Timeout for launch process</param>
    /// <returns>Launch result indicating success or failure mode</returns>
    public async Task<LaunchResult> LaunchTestShardAsync(string testShardPath, int timeoutSeconds = 60)
    {
        if (_testProcess != null && !_testProcess.HasExited)
        {
            logger.Warning("Test shard already running");
            return LaunchResult.AlreadyRunning;
        }

        _testShardPath = testShardPath;

        try
        {
            logger.Information("Launching test shard from: {Path}", testShardPath);

            // Create and configure the test process
            var startInfo = CreateProcessStartInfo(testShardPath);

            _testProcess = new Process { StartInfo = startInfo };
            _testProcess.EnableRaisingEvents = true;
            _testProcess.Exited += OnTestProcessExited;

            // Start the process
            var started = _testProcess.Start();
            if (!started)
            {
                logger.Error("Failed to start test shard process");
                return LaunchResult.Failed;
            }

            logger.Information("Test shard process started (PID: {Pid})", _testProcess.Id);

            // Establish IPC connection
            var ipcResult = await EstablishIPCConnectionAsync(timeoutSeconds);
            if (ipcResult != LaunchResult.Success)
            {
                await CleanupFailedLaunchAsync();
                return ipcResult;
            }

            // Wait for READY signal from test shard
            var readyResult = await WaitForReadySignalAsync(timeoutSeconds);
            if (readyResult != LaunchResult.Success)
            {
                await CleanupFailedLaunchAsync();
                return readyResult;
            }

            logger.Information("Test shard launched and ready");
            return LaunchResult.Success;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Exception during test shard launch");
            await CleanupFailedLaunchAsync();
            return LaunchResult.Failed;
        }
    }

    /// <summary>
    /// Runs a test scenario on the launched test shard.
    /// </summary>
    /// <param name="testId">ID of the test scenario to run</param>
    /// <param name="durationSeconds">Test duration in seconds</param>
    /// <param name="parameters">Additional test parameters</param>
    /// <returns>Test completion payload with results</returns>
    public async Task<TestCompletePayload> RunTestAsync(string testId, int durationSeconds, TestParameters parameters = null)
    {
        if (_pipe == null || !_pipe.IsConnected)
        {
            throw new InvalidOperationException("Test shard not connected");
        }

        try
        {
            logger.Information("Running test: {TestId} for {Duration}s", testId, durationSeconds);

            // Send test execution request
            var runPayload = new RunTestPayload
            {
                TestId = testId,
                DurationSeconds = durationSeconds,
                Parameters = parameters ?? new TestParameters()
            };

            var runMessage = TestShardMessage.Create(Server.Modules.Sphere51a.Testing.IPC.MessageType.RunTest, runPayload);
            await _pipe.SendMessageAsync(runMessage);

            // Wait for completion or progress updates
            return await WaitForTestCompletionAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Exception during test execution");
            throw;
        }
    }

    /// <summary>
    /// Shuts down the test shard gracefully.
    /// </summary>
    /// <param name="force">Force immediate termination if graceful shutdown fails</param>
    /// <param name="timeoutSeconds">Timeout for shutdown process</param>
    public async Task ShutdownAsync(bool force = false, int timeoutSeconds = 30)
    {
        if (_testProcess == null || _testProcess.HasExited)
        {
            logger.Debug("Test shard already stopped");
            return;
        }

        try
        {
            logger.Information("Shutting down test shard (PID: {Pid})", _testProcess.Id);

            // Try graceful shutdown first
            if (_pipe != null && _pipe.IsConnected && !force)
            {
                try
                {
                    var shutdownMessage = TestShardMessage.Create(Server.Modules.Sphere51a.Testing.IPC.MessageType.Shutdown);
                    await _pipe.SendMessageAsync(shutdownMessage);

                    // Wait for process to exit gracefully
                    var exited = await WaitForExitAsync(timeoutSeconds);
                    if (exited)
                    {
                        logger.Information("Test shard shut down gracefully");
                        return;
                    }

                    logger.Warning("Graceful shutdown timeout, forcing termination");
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Error during graceful shutdown, forcing termination");
                }
            }

            // Force termination
            _testProcess.Kill();
            await WaitForExitAsync(5); // Short timeout for forced kill
            logger.Information("Test shard forcefully terminated");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error during test shard shutdown");
        }
        finally
        {
            CleanupResources();
        }
    }

    /// <summary>
    /// Checks if the test shard is currently running.
    /// </summary>
    public bool IsRunning => _testProcess != null && !_testProcess.HasExited;

    /// <summary>
    /// Gets the process ID of the test shard.
    /// </summary>
    public int ProcessId => _testProcess?.Id ?? 0;

    /// <summary>
    /// Gets the path to the test shard directory.
    /// </summary>
    public string TestShardPath => _testShardPath;

    private ProcessStartInfo CreateProcessStartInfo(string testShardPath)
    {
        var exePath = Path.Combine(testShardPath, "ModernUO.dll");

        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException("ModernUO.dll not found in test shard", exePath);
        }

        return new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{exePath}\" --test-shard",
            WorkingDirectory = testShardPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
    }

    private async Task<LaunchResult> EstablishIPCConnectionAsync(int timeoutSeconds)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            _pipe = await NamedPipeProtocol.CreateClientAsync();
            logger.Debug("IPC connection established");
            return LaunchResult.Success;
        }
        catch (TimeoutException)
        {
            logger.Error("IPC connection timeout after {Timeout}s", timeoutSeconds);
            return LaunchResult.Timeout;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to establish IPC connection");
            return LaunchResult.Failed;
        }
    }

    private async Task<LaunchResult> WaitForReadySignalAsync(int timeoutSeconds)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            var message = await _pipe.ReceiveMessageAsync();

            if (message.Type == Server.Modules.Sphere51a.Testing.IPC.MessageType.Ready)
            {
                logger.Information("Test shard signaled ready");
                return LaunchResult.Success;
            }
            else
            {
                logger.Error("Unexpected message type during ready handshake: {Type}", message.Type);
                return LaunchResult.Failed;
            }
        }
        catch (TimeoutException)
        {
            logger.Error("Ready signal timeout after {Timeout}s", timeoutSeconds);
            return LaunchResult.Timeout;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error waiting for ready signal");
            return LaunchResult.Failed;
        }
    }

    private async Task<TestCompletePayload> WaitForTestCompletionAsync()
    {
        while (true)
        {
            var message = await _pipe.ReceiveMessageAsync();

            switch (message.Type)
            {
                case Server.Modules.Sphere51a.Testing.IPC.MessageType.TestProgress:
                    // Could emit progress events here
                    var progress = System.Text.Json.JsonSerializer.Deserialize<TestProgressPayload>(message.Payload);
                    logger.Debug("Test progress: {Phase} - {Percent}%", progress.Phase, progress.ProgressPercent);
                    break;

                case Server.Modules.Sphere51a.Testing.IPC.MessageType.TestComplete:
                    var result = System.Text.Json.JsonSerializer.Deserialize<TestCompletePayload>(message.Payload);
                    logger.Information("Test completed: {Passed}", result.Passed);
                    return result;

                case Server.Modules.Sphere51a.Testing.IPC.MessageType.TestFailed:
                    var error = System.Text.Json.JsonSerializer.Deserialize<ErrorPayload>(message.Payload);
                    logger.Error("Test failed: {Message}", error.Message);
                    throw new Exception($"Test execution failed: {error.Message}");

                case Server.Modules.Sphere51a.Testing.IPC.MessageType.Error:
                    var errorPayload = System.Text.Json.JsonSerializer.Deserialize<ErrorPayload>(message.Payload);
                    logger.Error("Test shard error: {Message}", errorPayload.Message);
                    throw new Exception($"Test shard error: {errorPayload.Message}");

                default:
                    logger.Warning("Unexpected message type during test execution: {Type}", message.Type);
                    break;
            }
        }
    }

    private async Task<bool> WaitForExitAsync(int timeoutSeconds)
    {
        if (_testProcess == null || _testProcess.HasExited)
        {
            return true;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            await _testProcess.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task CleanupFailedLaunchAsync()
    {
        if (_testProcess != null && !_testProcess.HasExited)
        {
            try
            {
                _testProcess.Kill();
                await WaitForExitAsync(5);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Error cleaning up failed launch");
            }
        }

        CleanupResources();
    }

    private void CleanupResources()
    {
        if (_pipe != null)
        {
            _pipe.Dispose();
            _pipe = null;
        }

        if (_testProcess != null)
        {
            _testProcess.Dispose();
            _testProcess = null;
        }
    }

    private void OnTestProcessExited(object sender, EventArgs e)
    {
        logger.Information("Test shard process exited (PID: {Pid})", _testProcess?.Id ?? 0);

        if (_testProcess?.ExitCode != 0)
        {
            logger.Warning("Test shard exited with code: {Code}", _testProcess.ExitCode);
        }

        CleanupResources();
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        try
        {
            // Shutdown asynchronously but don't wait
            _ = ShutdownAsync(force: true, timeoutSeconds: 5);
        }
        catch
        {
            // Ignore errors during disposal
        }
    }
}
