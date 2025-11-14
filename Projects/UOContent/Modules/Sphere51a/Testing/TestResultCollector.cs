using System;
using System.Collections.Generic;
using System.Linq;
using Server.Logging;
using Server.Modules.Sphere51a.Testing.IPC;

namespace Server.Modules.Sphere51a.Testing;

/// <summary>
/// Collects and aggregates test results during live test execution.
/// Provides analysis and reporting capabilities for test outcomes.
/// </summary>
public class TestResultCollector
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(TestResultCollector));

    private readonly string _testId;
    private readonly DateTime _startTime;
    private readonly List<TimingMeasurement> _measurements;
    private readonly List<string> _failureReasons;
    private readonly List<string> _observations;

    /// <summary>
    /// Initializes a new test result collector.
    /// </summary>
    /// <param name="testId">The unique identifier for the test.</param>
    public TestResultCollector(string testId)
    {
        _testId = testId ?? throw new ArgumentNullException(nameof(testId));
        _startTime = DateTime.UtcNow;
        _measurements = new List<TimingMeasurement>();
        _failureReasons = new List<string>();
        _observations = new List<string>();
    }

    /// <summary>
    /// Gets or sets whether the test passed.
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// Gets the total execution time of the test.
    /// </summary>
    public TimeSpan ExecutionTime => DateTime.UtcNow - _startTime;

    /// <summary>
    /// Gets the list of failure reasons.
    /// </summary>
    public IReadOnlyList<string> FailureReasons => _failureReasons;

    /// <summary>
    /// Gets the list of test observations.
    /// </summary>
    public IReadOnlyList<string> Observations => _observations;

    /// <summary>
    /// Records a timing measurement.
    /// </summary>
    /// <param name="expectedDelayMs">The expected delay in milliseconds.</param>
    /// <param name="actualDelayMs">The actual delay in milliseconds.</param>
    /// <param name="weaponType">The weapon type (optional).</param>
    /// <param name="dexterity">The dexterity value (optional).</param>
    public void RecordMeasurement(double expectedDelayMs, double actualDelayMs, string weaponType = null, int dexterity = 0)
    {
        var measurement = new TimingMeasurement
        {
            WeaponType = weaponType ?? "Unknown",
            Dexterity = dexterity,
            ExpectedDelayMs = expectedDelayMs,
            ActualDelayMs = actualDelayMs,
            VarianceMs = actualDelayMs - expectedDelayMs,
            Timestamp = global::Server.Core.TickCount
        };

        _measurements.Add(measurement);
        logger.Debug("Recorded measurement: Expected={Expected:F1}ms, Actual={Actual:F1}ms, Variance={Variance:F1}ms",
            expectedDelayMs, actualDelayMs, measurement.VarianceMs);
    }

    /// <summary>
    /// Records a custom timing measurement.
    /// </summary>
    /// <param name="measurement">The timing measurement to record.</param>
    public void RecordMeasurement(TimingMeasurement measurement)
    {
        if (measurement == null) throw new ArgumentNullException(nameof(measurement));
        _measurements.Add(measurement);
    }

    /// <summary>
    /// Adds a failure reason to the test results.
    /// </summary>
    /// <param name="reason">The failure reason.</param>
    public void AddFailureReason(string reason)
    {
        if (!string.IsNullOrWhiteSpace(reason))
        {
            _failureReasons.Add(reason);
            logger.Warning("Added failure reason: {Reason}", reason);
        }
    }

    /// <summary>
    /// Adds an observation to the test results.
    /// </summary>
    /// <param name="observation">The observation to add.</param>
    public void AddObservation(string observation)
    {
        if (!string.IsNullOrWhiteSpace(observation))
        {
            _observations.Add(observation);
            logger.Information("Added observation: {Observation}", observation);
        }
    }

    /// <summary>
    /// Gets the complete test results in the format expected by the IPC protocol.
    /// </summary>
    /// <returns>The test results data structure.</returns>
    public TestResults GetTestResults()
    {
        if (_measurements.Count == 0)
        {
            logger.Warning("No measurements recorded for test: {TestId}", _testId);
            return new TestResults
            {
                Summary = new SummaryMetrics(),
                WeaponResults = Array.Empty<WeaponTimingResult>(),
                BaselineComparison = new BaselineComparison()
            };
        }

        var summary = CalculateSummaryMetrics();
        var weaponResults = CalculateWeaponResults();
        var baselineComparison = CalculateBaselineComparison(summary);

        return new TestResults
        {
            Summary = summary,
            WeaponResults = weaponResults,
            BaselineComparison = baselineComparison
        };
    }

    /// <summary>
    /// Calculates summary metrics from all measurements.
    /// </summary>
    /// <returns>The summary metrics.</returns>
    private SummaryMetrics CalculateSummaryMetrics()
    {
        var variances = _measurements.Select(m => Math.Abs(m.VarianceMs)).ToList();

        return new SummaryMetrics
        {
            TotalActions = _measurements.Count,
            AverageVarianceMs = variances.Average(),
            MaxVarianceMs = variances.Max(),
            MinVarianceMs = variances.Min(),
            WithinTargetCount = _measurements.Count(m => Math.Abs(m.VarianceMs) <= 25),
            OutlierCount = _measurements.Count(m => Math.Abs(m.VarianceMs) > 50),
            AccuracyPercent = (_measurements.Count(m => Math.Abs(m.VarianceMs) <= 25) / (double)_measurements.Count) * 100.0,
            OutlierPercent = (_measurements.Count(m => Math.Abs(m.VarianceMs) > 50) / (double)_measurements.Count) * 100.0
        };
    }

    /// <summary>
    /// Calculates weapon-specific results.
    /// </summary>
    /// <returns>Array of weapon timing results.</returns>
    private WeaponTimingResult[] CalculateWeaponResults()
    {
        return _measurements
            .GroupBy(m => m.WeaponType)
            .Select(g => CreateWeaponResult(g.Key, g.ToList()))
            .ToArray();
    }

    /// <summary>
    /// Creates a weapon timing result from a group of measurements.
    /// </summary>
    /// <param name="weaponName">The weapon name.</param>
    /// <param name="measurements">The measurements for this weapon.</param>
    /// <returns>The weapon timing result.</returns>
    private WeaponTimingResult CreateWeaponResult(string weaponName, List<TimingMeasurement> measurements)
    {
        var dexVariations = measurements
            .GroupBy(m => m.Dexterity)
            .Select(g => new DexVariationResult
            {
                Dexterity = g.Key,
                SwingCount = g.Count(),
                ExpectedDelayMs = g.Average(m => m.ExpectedDelayMs),
                ActualAvgDelayMs = g.Average(m => m.ActualDelayMs),
                VarianceMs = g.Average(m => Math.Abs(m.VarianceMs)),
                Passed = g.Count(m => Math.Abs(m.VarianceMs) <= 25) / (double)g.Count() >= 0.95
            })
            .OrderBy(dv => dv.Dexterity)
            .ToArray();

        return new WeaponTimingResult
        {
            WeaponName = weaponName,
            SwingCount = measurements.Count,
            ExpectedDelayMs = measurements.Average(m => m.ExpectedDelayMs),
            ActualAvgDelayMs = measurements.Average(m => m.ActualDelayMs),
            VarianceMs = measurements.Average(m => Math.Abs(m.VarianceMs)),
            Passed = measurements.Count(m => Math.Abs(m.VarianceMs) <= 25) / (double)measurements.Count >= 0.95,
            DexVariations = dexVariations
        };
    }

    /// <summary>
    /// Calculates baseline comparison (placeholder implementation).
    /// </summary>
    /// <param name="summary">The summary metrics.</param>
    /// <returns>The baseline comparison.</returns>
    private BaselineComparison CalculateBaselineComparison(SummaryMetrics summary)
    {
        // TODO: Implement actual baseline loading and comparison
        // For now, return a placeholder
        return new BaselineComparison
        {
            MeetsBaseline = summary.AccuracyPercent >= 95.0,
            BaselineAccuracyPercent = 98.5, // Example baseline
            DifferencePercent = summary.AccuracyPercent - 98.5,
            Trend = summary.AccuracyPercent >= 98.5 ? "Improved" : "Declined"
        };
    }

    /// <summary>
    /// Represents a single timing measurement.
    /// </summary>
    public class TimingMeasurement
    {
        /// <summary>
        /// The weapon type.
        /// </summary>
        public string WeaponType { get; set; }

        /// <summary>
        /// The dexterity value.
        /// </summary>
        public int Dexterity { get; set; }

        /// <summary>
        /// The expected delay in milliseconds.
        /// </summary>
        public double ExpectedDelayMs { get; set; }

        /// <summary>
        /// The actual delay in milliseconds.
        /// </summary>
        public double ActualDelayMs { get; set; }

        /// <summary>
        /// The variance in milliseconds (actual - expected).
        /// </summary>
        public double VarianceMs { get; set; }

        /// <summary>
        /// The timestamp when the measurement was taken.
        /// </summary>
        public long Timestamp { get; set; }
    }
}
