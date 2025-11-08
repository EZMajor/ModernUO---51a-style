using System;
using System.Runtime.CompilerServices;
using Server.Modules.Sphere51a;

// Skips initializing stackalloc with zeros.
[module: SkipLocalsInit]

/// <summary>
/// Initialize method called by AssemblyHandler.Invoke("Initialize")
/// </summary>
namespace Server
{
    public static class UOContentInitialization
    {
        public static void Initialize()
        {
            Console.WriteLine("UOContentInitialization.Initialize() called - initializing Sphere51a module");
            // Initialize Sphere51a module
            Server.Modules.Sphere51a.Sphere51aModule.Initialize();
            Console.WriteLine("Sphere51a module initialization completed");
        }
    }
}
