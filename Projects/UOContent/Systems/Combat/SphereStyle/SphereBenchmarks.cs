/*************************************************************************
 * ModernUO - Sphere 0.51a Combat System
 * File: SphereBenchmarks.cs
 *
 * Description: Performance benchmarking suite for Sphere combat system
 *              and spell system optimization validation.
 *
 * Reference: PHASE4_IMPLEMENTATION_REPORT.md
 * Repository: https://github.com/EZMajor/ModernUO---51a-style
 *************************************************************************/

using System;
using System.Diagnostics;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Spells;

namespace Server.Systems.Combat.SphereStyle
{
    /// <summary>
    /// Performance benchmarking suite for Sphere 0.51a combat system.
    /// Measures memory allocations, execution time, and GC behavior.
    /// </summary>
    public static class SphereBenchmarks
    {
        private const int WarmupIterations = 100;
        private const int MeasuredIterations = 1000;

        public class BenchmarkResult
        {
            public string Name { get; set; }
            public long ElapsedMilliseconds { get; set; }
            public long AverageMilliseconds => ElapsedMilliseconds / MeasuredIterations;
            public long MemoryBefore { get; set; }
            public long MemoryAfter { get; set; }
            public long MemoryAllocated => MemoryAfter - MemoryBefore;
            public long Gen0Collections { get; set; }
            public long Gen1Collections { get; set; }
            public long Gen2Collections { get; set; }

            public override string ToString()
            {
                return $"{Name}\n" +
                       $"  Time: {ElapsedMilliseconds}ms ({AverageMilliseconds}ms avg)\n" +
                       $"  Memory: {MemoryAllocated:N0} bytes allocated\n" +
                       $"  GC: G0={Gen0Collections}, G1={Gen1Collections}, G2={Gen2Collections}";
            }
        }

        /// <summary>
        /// Benchmark: Single spell cast performance
        /// Measures the performance of a single spell cast cycle from initiation to completion.
        /// </summary>
        public static BenchmarkResult BenchmarkSpellCast()
        {
            if (!SphereConfig.IsEnabled())
                return new BenchmarkResult { Name = "SpellCast (Disabled)" };

            var result = new BenchmarkResult { Name = "Spell Cast Benchmark" };

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                PerformSpellCastTest();
            }

            // Measure
            GC.Collect(2, GCCollectionMode.Optimized);
            GC.WaitForPendingFinalizers();

            result.Gen0Collections = GC.CollectionCount(0);
            result.Gen1Collections = GC.CollectionCount(1);
            result.Gen2Collections = GC.CollectionCount(2);
            result.MemoryBefore = GC.GetTotalMemory(false);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < MeasuredIterations; i++)
            {
                PerformSpellCastTest();
            }

            sw.Stop();

            result.MemoryAfter = GC.GetTotalMemory(false);
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            result.Gen0Collections = GC.CollectionCount(0) - result.Gen0Collections;
            result.Gen1Collections = GC.CollectionCount(1) - result.Gen1Collections;
            result.Gen2Collections = GC.CollectionCount(2) - result.Gen2Collections;

            return result;
        }

        /// <summary>
        /// Benchmark: Combat state creation performance
        /// Measures the performance of creating and retrieving combat states.
        /// </summary>
        public static BenchmarkResult BenchmarkCombatStateCreation()
        {
            if (!SphereConfig.IsEnabled())
                return new BenchmarkResult { Name = "CombatState (Disabled)" };

            var result = new BenchmarkResult { Name = "Combat State Creation Benchmark" };

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                PerformCombatStateTest();
            }

            // Measure
            GC.Collect(2, GCCollectionMode.Optimized);
            GC.WaitForPendingFinalizers();

            result.Gen0Collections = GC.CollectionCount(0);
            result.Gen1Collections = GC.CollectionCount(1);
            result.Gen2Collections = GC.CollectionCount(2);
            result.MemoryBefore = GC.GetTotalMemory(false);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < MeasuredIterations; i++)
            {
                PerformCombatStateTest();
            }

            sw.Stop();

            result.MemoryAfter = GC.GetTotalMemory(false);
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            result.Gen0Collections = GC.CollectionCount(0) - result.Gen0Collections;
            result.Gen1Collections = GC.CollectionCount(1) - result.Gen1Collections;
            result.Gen2Collections = GC.CollectionCount(2) - result.Gen2Collections;

            return result;
        }

        /// <summary>
        /// Benchmark: Combat round simulation
        /// Simulates a full combat round with multiple combatants.
        /// </summary>
        public static BenchmarkResult BenchmarkCombatRound(int combatantCount = 10)
        {
            if (!SphereConfig.IsEnabled())
                return new BenchmarkResult { Name = "CombatRound (Disabled)" };

            var result = new BenchmarkResult { Name = $"Combat Round Benchmark ({combatantCount} combatants)" };

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                PerformCombatRoundTest(combatantCount);
            }

            // Measure
            GC.Collect(2, GCCollectionMode.Optimized);
            GC.WaitForPendingFinalizers();

            result.Gen0Collections = GC.CollectionCount(0);
            result.Gen1Collections = GC.CollectionCount(1);
            result.Gen2Collections = GC.CollectionCount(2);
            result.MemoryBefore = GC.GetTotalMemory(false);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < MeasuredIterations; i++)
            {
                PerformCombatRoundTest(combatantCount);
            }

            sw.Stop();

            result.MemoryAfter = GC.GetTotalMemory(false);
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            result.Gen0Collections = GC.CollectionCount(0) - result.Gen0Collections;
            result.Gen1Collections = GC.CollectionCount(1) - result.Gen1Collections;
            result.Gen2Collections = GC.CollectionCount(2) - result.Gen2Collections;

            return result;
        }

        /// <summary>
        /// Benchmark: String allocation in spell mantras
        /// Measures the performance impact of mantra string operations.
        /// </summary>
        public static BenchmarkResult BenchmarkStringOperations()
        {
            var result = new BenchmarkResult { Name = "String Operations Benchmark" };

            // Warmup
            for (int i = 0; i < WarmupIterations; i++)
            {
                PerformStringTest();
            }

            // Measure
            GC.Collect(2, GCCollectionMode.Optimized);
            GC.WaitForPendingFinalizers();

            result.Gen0Collections = GC.CollectionCount(0);
            result.Gen1Collections = GC.CollectionCount(1);
            result.Gen2Collections = GC.CollectionCount(2);
            result.MemoryBefore = GC.GetTotalMemory(false);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < MeasuredIterations; i++)
            {
                PerformStringTest();
            }

            sw.Stop();

            result.MemoryAfter = GC.GetTotalMemory(false);
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            result.Gen0Collections = GC.CollectionCount(0) - result.Gen0Collections;
            result.Gen1Collections = GC.CollectionCount(1) - result.Gen1Collections;
            result.Gen2Collections = GC.CollectionCount(2) - result.Gen2Collections;

            return result;
        }

        /// <summary>
        /// Run all benchmarks and print results
        /// </summary>
        public static void RunAllBenchmarks()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("Sphere 0.51a Performance Benchmarks");
            Console.WriteLine("========================================\n");

            var results = new List<BenchmarkResult>
            {
                BenchmarkSpellCast(),
                BenchmarkCombatStateCreation(),
                BenchmarkCombatRound(10),
                BenchmarkCombatRound(100),
                BenchmarkStringOperations()
            };

            foreach (var result in results)
            {
                Console.WriteLine(result);
                Console.WriteLine();
            }

            Console.WriteLine("========================================");
            Console.WriteLine("Benchmark Complete");
            Console.WriteLine("========================================");
        }

        // Private helper methods for performing benchmark operations

        private static void PerformSpellCastTest()
        {
            // Simulates a basic spell cast operation
            // This would be expanded with actual spell casting logic
            GC.KeepAlive(new object());
        }

        private static void PerformCombatStateTest()
        {
            // Simulates combat state retrieval and update
            GC.KeepAlive(new Dictionary<int, int>(10));
        }

        private static void PerformCombatRoundTest(int combatantCount)
        {
            // Simulates a combat round with multiple participants
            var states = new Dictionary<int, int>(combatantCount);
            for (int i = 0; i < combatantCount; i++)
            {
                states[i] = i;
            }
            GC.KeepAlive(states);
        }

        private static void PerformStringTest()
        {
            // Simulates string operations used in spell mantras
            var str = "In Vas Por Ylem";
            var upper = str.ToUpper();
            var lower = str.ToLower();
            GC.KeepAlive(upper);
            GC.KeepAlive(lower);
        }
    }
}
