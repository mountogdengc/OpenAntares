# ADR-001: C#/Godot .NET presentation and independent simulation

- **Status:** Established decision, recovered 2026-10-22; no new architecture approval implied.
- **Sources:** [CHARTER.md](../CHARTER.md), [CLAUDE.md](../../CLAUDE.md), project files.
- **Requirements:** REQ-003, REQ-010, REQ-011, REQ-013.

## Context and choice

Core strategic rules need headless testing, reusable state, and room for future AI/multiplayer. Keep C# throughout: Godot .NET presentation references a plain .NET simulation library that never references Godot. UI controls are composed in C#, thin scenes enter the application, and all visual styling uses one inherited theme. Content lives in JSON.

## Alternatives and consequences

Existing guidance rejects a GDScript/C# split because of typing and language-boundary friction. It accepts unavailable C# web export because web is outside the targets. No historical engine-selection comparison was found; adoption does not invent one.

Headless tests can execute without Godot, but graphical interaction still needs Godot checks. Root compile exclusions and `.gdignore` files prevent duplicate compilation/import. The current net8.0/Godot.NET.Sdk 4.7.2 setup is retained without dependency upgrades.
