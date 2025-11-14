using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Server.Logging;
using Server.Modules.Sphere51a.Testing.IPC;

namespace Server;

/// <summary>
/// Server-side coordinator for live test shard execution.
/// Runs inside the test shard process and coordinates test execution via IPC.
/// </summary>
public class TestShardCoordinator
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TestShardCoordinator));

    private NamedPipeProtocol _pipe;
    private Dictionary<string, Type> _availableTests;
    private bool _isRunning;

    public TestShardCoordinator()
    {
        _availableTests = new Dictionary<string, Type>
        {
            ["weapon_timing"] = typeof(Server.Modules.Sphere51a.Testing.Scenarios.WeaponTimingLiveTest),
            ["spell_timing"] = typeof(Server.Modules.Sphere51a.Testing.Scenarios.SpellTimingLiveTest)
            // Add more test types here as they are implemented
        };
    }

    /// <summary>
    /// Runs the test shard coordinator.
    /// This is the main entry point when ModernUO is started with --test-shard.
    /// </summary>
    public async Task RunAsync()
    {
        logger.Information("Test Shard Coordinator starting...");

        try
        {
            // Initialize IPC server
            _pipe = await NamedPipeProtocol.CreateServerAsync();
            logger.Information("IPC server initialized, waiting for launcher connection");

            // Signal ready to launcher
            await _pipe.SendMessageAsync(TestShardMessage.Create(Server.Modules.Sphere51a.Testing.IPC.MessageType.Ready));
            logger.Information("Signaled ready to launcher");

            _isRunning = true;

            // Main coordination loop
            await RunCoordinationLoopAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Fatal error in test shard coordinator");
            await SendErrorAsync("Coordinator initialization failed", ex.Message);
        }
        finally
        {
            await CleanupAsync();
        }
    }

    private async Task RunCoordinationLoopAsync()
    {
        while (_isRunning)
        {
            try
            {
                var message = await _pipe.ReceiveMessageAsync();

                switch (message.Type)
                {
                    case Server.Modules.Sphere51a.Testing.IPC.MessageType.RunTest:
                        await HandleRunTestAsync(message);
                        break;

                    case Server.Modules.Sphere51a.Testing.IPC.MessageType.Shutdown:
                        logger.Information("Received shutdown command");
                        _isRunning = false;
                        break;

                    case Server.Modules.Sphere51a.Testing.IPC.MessageType.Heartbeat:
                        // Respond to heartbeat
                        await _pipe.SendMessageAsync(TestShardMessage.Create(Server.Modules.Sphere51a.Testing.IPC.MessageType.Heartbeat));
                        break;

                    default:
                        logger.Warning("Received unknown message type: {Type}", message.Type);
                        await SendErrorAsync("Unknown message type", $"Type: {message.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in coordination loop");
                await SendErrorAsync("Coordination loop error", ex.Message);

                // Continue running unless it's a fatal error
                if (ex is System.IO.IOException)
                {
                    logger.Error("IPC connection lost, shutting down");
                    _isRunning = false;
                }
            }
        }
    }

    private async Task HandleRunTestAsync(TestShardMessage message)
    {
        try
        {
            var runPayload = System.Text.Json.JsonSerializer.Deserialize<RunTestPayload>(message.Payload);

            if (string.IsNullOrEmpty(runPayload.TestId))
            {
                await SendErrorAsync("Invalid test request", "TestId is required");
                return;
            }

            logger.Information("Running test: {TestId} for {Duration}s", runPayload.TestId, runPayload.DurationSeconds);

            // Check if test is available
            if (!_availableTests.TryGetValue(runPayload.TestId, out var testType))
            {
                await SendErrorAsync("Test not found", $"Test '{runPayload.TestId}' is not available");
                return;
            }

            // Create and run the test
            var testInstance = await CreateTestInstanceAsync(testType, runPayload);
            if (testInstance == null)
            {
                await SendErrorAsync("Test creation failed", $"Failed to create test instance for '{runPayload.TestId}'");
                return;
            }

            // Run the test
            await testInstance.ExecuteAsync();

            logger.Information("Test execution completed: {TestId}", runPayload.TestId);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error handling test execution");
            await SendErrorAsync("Test execution error", ex.Message);
        }
    }

    private async Task<Server.Modules.Sphere51a.Testing.LiveTestModule> CreateTestInstanceAsync(Type testType, RunTestPayload payload)
    {
        try
        {
            // Create test instance
            var constructor = testType.GetConstructor(new[] { typeof(NamedPipeProtocol) });
            if (constructor == null)
            {
                logger.Error("Test type {Type} does not have required constructor", testType.Name);
                return null;
            }

            var testInstance = (Server.Modules.Sphere51a.Testing.LiveTestModule)constructor.Invoke(new object[] { _pipe });

            logger.Debug("Created test instance: {Type}", testType.Name);
            return testInstance;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to create test instance: {Type}", testType.Name);
            return null;
        }
    }

    private async Task SendErrorAsync(string code, string message)
    {
        try
        {
            var errorPayload = new Server.Modules.Sphere51a.Testing.IPC.ErrorPayload
            {
                Code = code,
                Message = message
            };

            var errorMessage = TestShardMessage.Create(Server.Modules.Sphere51a.Testing.IPC.MessageType.Error, errorPayload);
            await _pipe.SendMessageAsync(errorMessage);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to send error message");
        }
    }

    private async Task CleanupAsync()
    {
        logger.Information("Test Shard Coordinator shutting down...");

        if (_pipe != null)
        {
            try
            {
                _pipe.Dispose();
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Error disposing IPC pipe");
            }
        }

        // Give a moment for cleanup
        await Task.Delay(500);

        logger.Information("Test Shard Coordinator shutdown complete");

        // Exit the process
        Environment.Exit(0);
    }
}
