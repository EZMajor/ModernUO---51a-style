You are working inside Visual Studio Code on the repository:
https://github.com/EZMajor/ModernUO---51a-style

Step 1 — Reference Documents

Read and apply all guidance from:

Claude.md — coding standards, communication protocols, and C# best practices.

Sphere0.51aCombatSystem.md — defines the full Sphere 0.51a-style combat system logic, timing, and behavioural rules.

These two documents are authoritative for all implementation and architectural decisions.

Step 2 — Research and Preparation

Research the ModernUO codebase (reference upstream repo: https://github.com/modernuo/ModernUO) to identify relevant systems.

Focus on:

/Projects/Server/Combat/

/Projects/Server/Mobiles/

/Projects/Server/Items/Weapons/

/Projects/Server/Spells/

Determine how weapon swings, ranged attacks, spell-casting delays, wand usage, and bandaging are currently implemented.

Identify where Sphere 0.51a combat behaviour will extend or override ModernUO logic.

Step 3 — Implementation Requirements

Implement Sphere-style combat logic inside the ModernUO solution.

Use a dedicated structure such as /Scripts/Combat/SphereStyle/ or appropriate project folders.

Maintain compatibility with .NET 9 and ModernUO architecture.

Each edit or override must be clearly marked with:

//Sphere-style edit — <short description of modification>


Follow all naming, commenting, and structural standards from Claude.md.

Step 4 — Version Control and Workflow

Use proper Git version control at all times.

Commit after every major functional change (e.g., swing delay system, spell delay handling, interruption logic, wand interaction).

Write clear, descriptive commit messages, following this format:

[Sphere-Style] Implement <feature/logic> — short description
Example:
[Sphere-Style] Implement spell delay and swing cancel logic


Push all commits to the repository:
https://github.com/EZMajor/ModernUO---51a-style

Use GitHub Issues to plan and track work:

Create one issue per feature or subsystem (e.g., “Sphere-Style Spell Delay Integration”).

Reference issue numbers in commit messages (e.g., “Fixes #12”).

Close issues only when corresponding code is merged and tested.

Step 5 — Deliverables

Summary Document (Markdown):

Integration points between Sphere 0.51a combat and ModernUO.

Direct file path references for each system modified.

Brief description of changes and reasoning.

Code Implementation:

All Sphere-style edits committed and documented.

Follows Claude.md conventions for structure and clarity.

Version Control:

Frequent commits with proper messages.

Active issue tracking for planning and documentation.