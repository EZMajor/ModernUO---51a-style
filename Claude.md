Project Overview
ModernUO is a modern Ultima Online server emulator built with .NET 9 and C# 12. This document guides AI-assisted development sessions for this codebase.
Repository: https://github.com/modernuo/ModernUO
Tech Stack: .NET 9, C# 12, nullable reference types, source generators

Claude Capabilities for This Project
Available Tools

File Operations: Create, read, and modify C# source files
Git Operations: Commit changes, create branches, manage workflow
GitHub Integration: Create/update issues, track work items
Web Search: Verify ModernUO patterns, C# standards, game dev best practices
Testing: Build and validate code changes

Workflow Integration
Claude commits after completing major coding tasks and uses GitHub issues for work planning and tracking.

Development Protocol
1. Planning Phase
Before writing any code:

Search for similar implementations in the codebase
Verify base classes, namespaces, and inheritance patterns
Check existing issues or create a new GitHub issue for the task
Document assumptions and dependencies

GitHub Issue Template:
Title: [Feature/Fix]: Brief description
Labels: enhancement/bug/refactor
Body:
- Objective: What needs to be done
- Location: Namespace and file path
- Dependencies: Required classes/systems
- Acceptance Criteria: Verification checklist
2. Code Generation Standards
Namespace Alignment
csharp// File: Projects/UOContent/Items/Weapons/Swords/Katana.cs
namespace Server.Items; // Matches folder structure
Modern C# Patterns
csharp[Constructible]
public partial class TeleportCrystal : Item
{
    [Constructible]
    public TeleportCrystal() : base(0x1F1C)
    {
        Weight = 1.0;
        Hue = 0x480;
        Name = "teleport crystal";
    }

    // Auto-serialization via source generator
    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public Point3D Destination { get; set; }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack
            return;
        }

        if (Destination == Point3D.Zero)
        {
            from.SendMessage("This crystal has not been attuned to a location.");
            return;
        }

        from.MoveToWorld(Destination, Map.Felucca);
        from.FixedParticles(0x376A, 9, 32, 5008, EffectLayer.Waist);
        from.PlaySound(0x1FE);
    }
}
```

**Key Requirements:**
- `[Constructible]` attribute on class and default constructor
- `partial` class declaration for source generators
- `[SerializableProperty(index)]` for auto-serialization
- Null-safe code with proper null checks
- XML documentation for public APIs

### 3. File Structure Patterns

**Item Implementation:**
```
Projects/UOContent/Items/
├── Weapons/
├── Armor/
├── Resources/
└── Misc/
    └── YourNewItem.cs
```

**Mobile Implementation:**
```
Projects/UOContent/Mobiles/
├── Monsters/
├── Vendors/
└── Animals/
    └── YourNewMobile.cs
```

**Command Implementation:**
```
Projects/UOContent/Commands/
└── YourCommand.cs
4. Quality Checklist
Before Committing:

 Namespace matches folder structure
 Uses [Constructible] and modern serialization
 Null safety enforced (nullable reference types enabled)
 No manual Serialize/Deserialize methods
 XML documentation on public members
 Error handling and validation present
 No magic numbers (use constants or configuration)
 Follows single responsibility principle
 No memory leaks or inefficient allocations
 Tested in local build

5. Git Workflow
Branch Strategy:
bash# Create feature branch from main
git checkout -b feature/teleport-crystal

# Commit after each complete feature
git add Projects/UOContent/Items/Misc/TeleportCrystal.cs
git commit -m "feat: Add teleport crystal item with location attuning"

# Push and reference issue
git push origin feature/teleport-crystal
```

**Commit Message Format:**
```
<type>: <description>

[optional body]

Closes #<issue-number>
Types: feat, fix, refactor, docs, test, perf, chore
Commit Triggers:

Complete feature implementation
Bug fix verified
Refactoring of significant scope completed
Breaking change introduced
Major documentation added

6. Game Development Best Practices
Performance:

Avoid allocations in frequently-called methods (Update, OnThink, etc.)
Use object pooling for short-lived objects
Cache frequently-accessed properties
Batch operations where possible

Architecture:

Prefer composition over inheritance
Use events for decoupled communication
Keep cyclomatic complexity low (<10)
Single responsibility per class
Immutable data where appropriate

Data Management:
csharp// Good: Configuration over hard-coding
public static class TeleportConfig
{
    public static TimeSpan CooldownDuration { get; } = TimeSpan.FromMinutes(5);
    public static int ManaCost { get; } = 20;
}

// Bad: Magic numbers
if (from.Mana < 20) // What does 20 mean?
    return;
Error Handling:
csharppublic override void OnDoubleClick(Mobile from)
{
    if (from == null)
    {
        return; // Fail gracefully
    }

    if (!IsChildOf(from.Backpack))
    {
        from.SendLocalizedMessage(1042001);
        return;
    }

    try
    {
        ExecuteTeleport(from);
    }
    catch (Exception ex)
    {
        from.SendMessage("An error occurred during teleportation.");
        Console.WriteLine($"TeleportCrystal error: {ex}");
    }
}
7. Common ModernUO Patterns
Timer Pattern:
csharpprivate class TeleportTimer : Timer
{
    private readonly Mobile _mobile;
    private readonly Point3D _destination;

    public TeleportTimer(Mobile mobile, Point3D destination) 
        : base(TimeSpan.FromSeconds(3))
    {
        _mobile = mobile;
        _destination = destination;
    }

    protected override void OnTick()
    {
        _mobile.MoveToWorld(_destination, _mobile.Map);
    }
}
Command Registration:
csharppublic static class TeleportCommand
{
    public static void Initialize()
    {
        CommandSystem.Register("teleport", AccessLevel.GameMaster, Teleport_OnCommand);
    }

    [Usage("teleport <x> <y> <z>")]
    [Description("Teleports to specified coordinates.")]
    private static void Teleport_OnCommand(CommandEventArgs e)
    {
        // Implementation
    }
}
Gump Pattern:
csharppublic class TeleportGump : Gump
{
    private readonly TeleportCrystal _crystal;

    public TeleportGump(TeleportCrystal crystal) : base(50, 50)
    {
        _crystal = crystal;
        
        Closable = true;
        Disposable = true;
        Dragable = true;
        Resizable = false;

        AddPage(0);
        AddBackground(0, 0, 300, 200, 9200);
        // Add gump elements
    }

    public override void OnResponse(NetState sender, RelayInfo info)
    {
        // Handle response
    }
}
8. Testing Strategy
Build Verification:
bash# Build the project
dotnet build Projects/Server/Server.csproj
dotnet build Projects/UOContent/UOContent.csproj

# Run if build succeeds
cd Distribution
./ModernUO
Manual Testing Checklist:

 Item creates without errors
 Serialization/deserialization works
 Visual effects display correctly
 Error messages display properly
 Edge cases handled (null checks, invalid input)
 Performance acceptable (no lag/stuttering)

9. Documentation Requirements
Class-Level:
csharp/// <summary>
/// A magical crystal that teleports the user to an attuned location.
/// </summary>
/// <remarks>
/// Players can attune the crystal by using it at a desired location.
/// The crystal has a cooldown period between uses.
/// </remarks>
[Constructible]
public partial class TeleportCrystal : Item
Property-Level:
csharp/// <summary>
/// Gets or sets the destination point for teleportation.
/// </summary>
[SerializableProperty(0)]
[CommandProperty(AccessLevel.GameMaster)]
public Point3D Destination { get; set; }
10. Response Format
Structure all responses as:

Verification Statement - Confirm namespace, base class, file path
Assumptions - List any unverified elements
Implementation - Complete, compilable code
Integration - File location, build commands, testing steps
Commit Plan - Branch name, commit message, issue reference

Tone: Technical, direct, professional. No filler, enthusiasm, or casual language.

Issue-Driven Development
Creating Issues
Before starting work:
bash# Use GitHub CLI or API to create issue
gh issue create --title "feat: Add teleport crystal item" \
  --body "Implement teleportable item with location attuning" \
  --label "enhancement"
Linking Work to Issues
In commits:
bashgit commit -m "feat: Add TeleportCrystal base implementation

- Implement double-click teleport behavior
- Add location attuning mechanism
- Include cooldown system

Relates to #42"
Closing issues:
bashgit commit -m "feat: Complete teleport crystal feature

Final testing completed, ready for merge.

Closes #42"
Issue Workflow

Plan - Create issue with requirements
Branch - Create feature branch referencing issue
Implement - Write code following standards
Commit - Commit with issue reference
Close - Final commit closes issue
Review - Link PR to issue for tracking


Anti-Patterns to Avoid
Never:

Use manual Serialize()/Deserialize() methods
Create ambiguous or duplicate class names
Hard-code magic numbers or strings
Allocate in tight loops or update methods
Use unverified external libraries
Assume base class methods without verification
Create deeply nested inheritance hierarchies
Use goto statements
Suppress nullability warnings without justification
Commit broken or untested code

Always:

Verify class existence before use
Use modern C# features (pattern matching, null coalescing, etc.)
Handle edge cases and null scenarios
Keep classes focused (SRP)
Profile performance-critical code
Document public APIs
Test before committing


Quick Reference
Common Base Classes:

Items: Item, BaseWeapon, BaseArmor, Container
Mobiles: Mobile, BaseCreature, BaseVendor
Spells: Spell, MagerySpell, NecromancySpell

Common Namespaces:

Server - Core server functionality
Server.Items - All items
Server.Mobiles - All mobiles/NPCs
Server.Spells - Magic system
Server.Commands - Admin commands
Server.Gumps - User interfaces
Server.Network - Networking

File Locations:

Core: Projects/Server/
Content: Projects/UOContent/
Generators: Projects/SerializationGenerator/


Session Checklist
At session start:

 Review related GitHub issues
 Search codebase for similar implementations
 Verify base classes and dependencies
 Create/update issue for current work

During development:

 Follow coding standards
 Maintain null safety
 Keep performance in mind
 Document as you go

Before committing:

 Build successfully
 Run basic tests
 Update issue with progress
 Write clear commit message
 Reference issue number

After major milestone:

 Commit changes
 Push to branch
 Update or close issue
 Document any remaining work