using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Server.Logging;

namespace Server.Modules.Sphere51a.Testing.IPC;

/// <summary>
/// Named pipe-based IPC protocol for communication between test launcher and test shard.
/// Provides reliable message passing with timeout protection.
/// </summary>
public class NamedPipeProtocol : IDisposable
{
    private const string PIPE_NAME = "test_shard_pipe";
    private const int BUFFER_SIZE = 4096;
    private const int CONNECT_TIMEOUT_MS = 10000; // 10 seconds
    private const int MESSAGE_TIMEOUT_MS = 5000;  // 5 seconds per message

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(NamedPipeProtocol));

    private NamedPipeServerStream _serverStream;
    private NamedPipeClientStream _clientStream;
    private StreamReader _reader;
    private StreamWriter _writer;
    private bool _isServer;
    private bool _isConnected;

    /// <summary>
    /// Creates a server-side pipe for the test shard to listen on.
    /// </summary>
    public static async Task<NamedPipeProtocol> CreateServerAsync()
    {
        var protocol = new NamedPipeProtocol { _isServer = true };

        try
        {
#pragma warning disable CA1416 // PipeTransmissionMode.Message is supported on Windows
            protocol._serverStream = new NamedPipeServerStream(
                PIPE_NAME,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous
            );
#pragma warning restore CA1416

            logger.Debug("Waiting for test launcher to connect...");
            await protocol._serverStream.WaitForConnectionAsync();

            protocol._reader = new StreamReader(protocol._serverStream);
            protocol._writer = new StreamWriter(protocol._serverStream) { AutoFlush = true };
            protocol._isConnected = true;

            logger.Information("Test launcher connected via named pipe");
            return protocol;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to create pipe server");
            protocol.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Creates a client-side pipe for the test launcher to connect with.
    /// </summary>
    public static async Task<NamedPipeProtocol> CreateClientAsync()
    {
        var protocol = new NamedPipeProtocol { _isServer = false };

        try
        {
            protocol._clientStream = new NamedPipeClientStream(
                ".",
                PIPE_NAME,
                PipeDirection.InOut,
                PipeOptions.Asynchronous
            );

            logger.Debug("Connecting to test shard...");
            await protocol._clientStream.ConnectAsync(CONNECT_TIMEOUT_MS);

            protocol._reader = new StreamReader(protocol._clientStream);
            protocol._writer = new StreamWriter(protocol._clientStream) { AutoFlush = true };
            protocol._isConnected = true;

            logger.Information("Connected to test shard via named pipe");
            return protocol;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to create pipe client");
            protocol.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Sends a message through the pipe.
    /// </summary>
    public async Task SendMessageAsync(TestShardMessage message)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Pipe is not connected");
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            await _writer.WriteLineAsync(json);
            logger.Debug("Sent message: {Type}", message.Type);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to send message: {Type}", message.Type);
            throw;
        }
    }

    /// <summary>
    /// Receives a message from the pipe with timeout.
    /// </summary>
    public async Task<TestShardMessage> ReceiveMessageAsync()
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Pipe is not connected");
        }

        try
        {
            using var cts = new CancellationTokenSource(MESSAGE_TIMEOUT_MS);
            var json = await _reader.ReadLineAsync(cts.Token);

            if (string.IsNullOrEmpty(json))
            {
                throw new IOException("Pipe connection closed");
            }

            var message = JsonSerializer.Deserialize<TestShardMessage>(json);
            logger.Debug("Received message: {Type}", message?.Type);
            return message;
        }
        catch (OperationCanceledException)
        {
            logger.Warning("Message receive timeout");
            throw new TimeoutException("Message receive timeout");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to receive message");
            throw;
        }
    }

    /// <summary>
    /// Sends a message and waits for a response.
    /// </summary>
    public async Task<TestShardMessage> SendRequestAsync(TestShardMessage request)
    {
        await SendMessageAsync(request);
        return await ReceiveMessageAsync();
    }

    /// <summary>
    /// Checks if the pipe is connected.
    /// </summary>
    public bool IsConnected => _isConnected && (
        (_isServer && _serverStream?.IsConnected == true) ||
        (!_isServer && _clientStream?.IsConnected == true)
    );

    /// <summary>
    /// Disposes the pipe connection.
    /// </summary>
    public void Dispose()
    {
        _isConnected = false;

        try
        {
            _writer?.Dispose();
            _reader?.Dispose();

            if (_isServer)
            {
                _serverStream?.Dispose();
            }
            else
            {
                _clientStream?.Dispose();
            }
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Error during pipe disposal");
        }
    }
}
