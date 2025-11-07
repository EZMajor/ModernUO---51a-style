using System;

namespace Server.Engines.DuelArena;

public static class DuelArenaSystem
{
    public static void Configure()
    {
        DuelSystem.Initialize();
        Console.WriteLine("DuelArena v1.0.0 configured");
    }

    public static void Initialize()
    {
        // Post-world-load initialization if needed
    }
}
