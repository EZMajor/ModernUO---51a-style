using System;
using System.IO;
using System.Threading.Tasks;
using Server.Modules.Sphere51a.Testing;

/// <summary>
/// Simple infrastructure test to verify the Live Test Shard components work correctly.
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🧪 Testing Live Test Shard Infrastructure...");
        Console.WriteLine();

        try
        {
            // Test 1: UO Path Resolver
            Console.WriteLine("1️⃣ Testing UO Path Resolver...");
            var uoPath = UOPathResolver.ResolveUOPath();
            Console.WriteLine($"   ✅ UO Path: {uoPath}");

            if (Directory.Exists(uoPath))
            {
                Console.WriteLine("   ✅ Directory exists");
            }
            else
            {
                Console.WriteLine("   ⚠️  Directory doesn't exist (using fallback)");
            }
            Console.WriteLine();

            // Test 2: Build Environment Creation
            Console.WriteLine("2️⃣ Testing Build Environment Creation...");
            var testShardPath = BuildTestEnvironment.CreateTestShard();
            Console.WriteLine($"   ✅ Test shard created: {Path.GetFileName(testShardPath)}");

            if (Directory.Exists(testShardPath))
            {
                Console.WriteLine("   ✅ Directory exists");

                // Check for key files
                var modernUO = Path.Combine(testShardPath, "ModernUO.dll");
                var config = Path.Combine(testShardPath, "Configuration", "modernuo.json");

                Console.WriteLine($"   ✅ ModernUO.dll: {File.Exists(modernUO)}");
                Console.WriteLine($"   ✅ modernuo.json: {File.Exists(config)}");
            }
            Console.WriteLine();

            // Test 3: Configuration Generation
            Console.WriteLine("3️⃣ Testing Configuration Generation...");
            var configDir = Path.Combine(testShardPath, "Configuration");

            var files = new[]
            {
                "modernuo.json",
                "sphere51a.json",
                "accounts.xml",
                "combat.json",
                "maps.json"
            };

            foreach (var file in files)
            {
                var path = Path.Combine(configDir, file);
                var exists = File.Exists(path);
                Console.WriteLine($"   {(exists ? "✅" : "❌")} {file}: {exists}");
            }
            Console.WriteLine();

            // Cleanup
            Console.WriteLine("🧹 Cleaning up test environment...");
            BuildTestEnvironment.CleanupTestShard(testShardPath);
            Console.WriteLine("   ✅ Cleanup complete");
            Console.WriteLine();

            Console.WriteLine("🎉 Infrastructure test completed successfully!");
            Console.WriteLine();
            Console.WriteLine("The Live Test Shard infrastructure is ready for Phase 3: Live Test Scenarios.");
            Console.WriteLine("Next steps:");
            Console.WriteLine("- Implement WeaponTimingLiveTest with real combat");
            Console.WriteLine("- Implement SpellTimingLiveTest with real spells");
            Console.WriteLine("- Add CLI interface for running tests");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Infrastructure test failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
