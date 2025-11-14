/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LogFactory.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.IO;
using Serilog;
using Serilog.Core;

namespace Server.Logging;

public static class LogFactory
{
    private static Logger serilogLogger;

    static LogFactory()
    {
        // Initialize with default console-only configuration
        serilogLogger = new LoggerConfiguration()
            .WriteTo.Async(a => a.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}"
            ))
#if DEBUG
            .MinimumLevel.Debug()
#endif
            .CreateLogger();
    }

    /// <summary>
    /// Reconfigures logging for test mode with file output.
    /// </summary>
    public static void ConfigureForTestMode()
    {
        var baseDir = Directory.GetCurrentDirectory();
        var logDirectory = Path.Combine(baseDir, "Distribution", "AuditReports", "Logs");
        Directory.CreateDirectory(logDirectory);

        var logFile = Path.Combine(logDirectory, $"test-run-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.log");

        // For test mode, keep console logging but also enable file logging via custom sink
        serilogLogger = new LoggerConfiguration()
            .WriteTo.Async(a => a.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}"
            ))
            .WriteTo.Async(a => new TestFileSink(logFile))
            .MinimumLevel.Debug() // Always debug level for tests
            .CreateLogger();

        Console.WriteLine($"[TestLog] Test logging configured. Log file: {logFile}");
    }

    /// <summary>
    /// Custom Serilog sink for test file logging.
    /// </summary>
    private class TestFileSink : Serilog.Core.ILogEventSink
    {
        private readonly string _logFile;
        private readonly object _lock = new();

        public TestFileSink(string logFile)
        {
            _logFile = logFile;
        }

        public void Emit(Serilog.Events.LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            var timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = logEvent.Level.ToString().ToUpperInvariant().PadRight(3);
            var source = logEvent.Properties.ContainsKey("SourceContext") ?
                logEvent.Properties["SourceContext"].ToString() : "Unknown";
            var exception = logEvent.Exception != null ? $"\n{logEvent.Exception}" : "";

            var line = $"[{timestamp} {level}] {message} <s:{source}>{exception}\n";

            lock (_lock)
            {
                File.AppendAllText(_logFile, line);
            }
        }
    }

    public static ILogger GetLogger(Type declaringType) => new SerilogLogger(serilogLogger.ForContext(declaringType));
}
