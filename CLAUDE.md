**Combined UI/conflict locally integrated, 2026-11-04 — Verified for combined automated scope:** Owner-authorized main integration preserves the newer map-first UI and conflict controls/reports/focus/legacy cleanup. Tested source d49e63c;12 rendered scenarios,four earned Main campaigns,28 authoritative save/future replays,107 layout checks and legacy rules8/9/11/12 cleanup pass. Fresh actual-main solution build and Windows2402/2402 pass,0failed/skipped;all408 authoritative inputs match and ordinary validation namespace is empty. See [integration report](reports/EMPIRE_CONFLICT_UI_INTEGRATION_2026-11-04.md). All branches/worktrees,owner prose,raw evidence and the existing three-log exception are retained. No push/release/cleanup;physical input,human readability,accessibility,platform,balance,security and release remain separate. Earlier pending statements below are historical;17 parent rows/totals remain unchanged.

**Combined acceptance verified, 2026-11-04; authorized main merge pending:** The isolated newer map-first UI plus conflict source d49e63c passes the full Windows2402/2402 suite,107 layout checks,all12 graphical scenarios,four earned Main campaigns and the authoritative28-save future replay. The newer UI/renderer and conflict controls/reports/focus/legacy cleanup are combined. See [combined integration report](reports/EMPIRE_CONFLICT_UI_INTEGRATION_2026-11-04.md). Merge only this tested source; retain every branch/worktree,owner prose and raw evidence. No push/release/cleanup; human/platform/balance/security/release scopes remain separate. Earlier statuses below retain their historical scopes;17 parent rows/totals are unchanged.

# OpenAntares

**M12 implementation authorized, 2026-10-31:** Owner “Looks good, go ahead and implement the plan.” approves the separate22-task plan at5df255b. M12-P Verified; M12-I In Progress on isolated codex/empire-conflict. Execute Native continuously, no per-task agents, one final fresh whole-branch implementation review. Canonical ledger: .superpowers/sdd/2026-10-31-empire-conflict/progress.md. Freeze genuine rules11 before product changes. Preserve main/all10 prior worktrees/owner prose/QA/futures/raw evidence/refs/stashes/recovery. No merge/push/release/cleanup. Earlier plan-pending statements are historical.

**M12 full design approved; implementation plan ready, 2026-10-31:** Owner “Looks good, write the implementation plan.” accepts the complete empire-conflict design at76312d1; [ADR-026](docs/DECISIONS/ADR-026-approved-empire-conflict-design.md) records full gameplay/default approval. M12-D/ECF-AC-012 Verified for design only. The [separate22-task plan](docs/superpowers/plans/2026-10-31-empire-conflict.md) and [author self-review](reports/EMPIRE_CONFLICT_IMPLEMENTATION_PLAN_REVIEW_2026-10-31.md) are ready for concrete-plan review; M12-P In Progress, M12-I/functional ECF criteria Not Started. Native inline/no per-task agents/one final fresh whole-branch implementation reviewer retained; do not re-ask design defaults or execution method. Product remains873f320/rules11/writer12/67, proposed12/13/79 inactive. Documentation checks only; no execution checkout, genuine rules11 capture, product edit or gameplay build/test. Preserve all10 worktrees/branches/stashes/owner prose/exact peaceful paragraph/QA/futures/raw evidence/recovery and17 parent-row statuses/totals. No merge/push/release/cleanup. Next: review this concrete22-task plan before execution. Earlier proposal-pending paragraphs are historical.

**M12 empire-conflict proposal, 2026-10-31:** Owner selected empire conflict and delegated drafting choices. The [complete proposal](docs/superpowers/specs/2026-10-31-empire-conflict-design.md) and [fresh builder-check disposition](reports/EMPIRE_CONFLICT_GAMEPLAY_REVIEW_2026-10-31.md) are ready for review. Proposed limited war/peace, protected civilians, armed fleet battles, nonstacking50% retained-production blockades, paid military AI and ten-boundary truce are drafter defaults. M12-D/ECF-AC-012 In Progress; separate plan/implementation and ECF-AC-001..011 Not Started. Main product remains873f320, rules11/writer12/67 pairs; proposed12/13/79 inactive. Documentation checks only; no gameplay test run, future capture, separate implementation plan or product changes. Retain Native inline/no per-task agents/one final implementation reviewer after later approvals, all10 worktrees/branches/owner prose/peaceful paragraph/QA/futures/raw evidence/recovery and17 parent-row statuses/totals. No push/release/cleanup. Next: owner review of the complete proposal before a separate plan. Earlier subsystem checkpoints are historical.

**M11 locally integrated, 2026-10-31:** Owner “Looks good, merge diplomacy into main.” authorized the conflict-free fast-forward of `codex/diplomacy` (`0db760a`) into main from `ada622f`. Product `873f320` is unchanged; rules11/writer12/67 explicit pairs are active. Fresh main Suite2140/2140 and native Diplomacy pass; the native runner rebuilds ordinary code and verifies all11 scenario types and2 helpers absent. All66 recorded product files match the verified branch after Git line-ending normalization. One implementation review and its correction pass remain complete; no new review or gameplay change. Recovery: `codex/diplomacy-premerge-20261031`. All10 worktrees and branches retained. See [integration evidence](reports/DIPLOMACY_INTEGRATION_2026-10-31.md). Earlier branch-only/in-progress paragraphs are historical. Parent17-row statuses/totals and human/platform/balance/security/release limits remain unchanged. No push, release or cleanup. Next: choose the next bounded milestone.

**M11 completed on isolated branch,2026-10-31:** Diplomacy is implemented and locally validated on `codex/diplomacy`, product `873f320`: rules11/writer12/67 explicit pairs. M11-P and M11-I/DIP-AC-001..011 Verified for local automated scope; DIP-AC-012/design approval remains separate. Fresh Suite2140/2140; all11 native scenarios pass at9ed0cbd, affected Diplomacy/Rival/Colony rerun0 diagnostics at873f320; final normal rebuild0warnings/errors excludes11 scenarios/2 helpers. One fresh whole-branch review and one regression-first correction pass complete; no open required findings. Read [validation report](reports/DIPLOMACY_VALIDATION_2026-10-30.md), final-machine-summary and archived ledger/rulings. Main/owner prose/7456 original QA/all10 worktrees/prior refs/stashes remain preserved. Parent17-row statuses/totals and human/platform/balance/security/release limits unchanged. No merge/push/release/cleanup; retain branch and workspace for owner disposition. Earlier planning/in-progress paragraphs are historical; do not repeat completed work or request method approval again.

Open-source, turn-based space 4X game built in Godot 4.7 (.NET build, GL Compatibility renderer, Jolt physics). The setting, code, art, and systems are all original. Targets: Windows, Linux, macOS.

[docs/CHARTER.md](docs/CHARTER.md) is the source of truth for scope and design intent. Read it before starting a new system. If a request conflicts with the charter, point that out instead of silently following either one.

## Technology stack

| Concern | Choice |
| --- | --- |
| Language | **C# everywhere.** No GDScript, because a cross-language boundary loses typing and adds friction. |
| Engine | Godot 4.7 **.NET build** (the standard build can't run C#) |
| Content data | **JSON** files, editable without any programming tools |
| Tests | **xUnit**, run with `dotnet test`, no Godot needed |

C# web export isn't supported. That's accepted, because the web isn't a target.

## Project layout

```text
OpenAntares.sln
project.godot, OpenAntares.csproj    Godot project (root). Presentation layer only.
game/                                Presentation C# code, Main.tscn entry scene
game/theme/openantares_theme.tres    The single UI theme (all visual styling)
art/                                 Original assets (CC0); art/branding/ holds logo and title art
content/core/                        Base-game JSON content (techs, buildings, planet types, ...)
src/OpenAntares.Simulation/          Game rules and state. Plain .NET class library, NO Godot reference.
tests/OpenAntares.Simulation.Tests/  xUnit tests for the simulation library
docs/                                Charter and design docs
```

- The Godot project references the simulation library through `<ProjectReference>`. The simulation library must never reference GodotSharp. That is how the simulation/presentation split is enforced.
- Godot's root `.csproj` compiles every `.cs` file under the project folder by default. Exclude `src/**` and `tests/**` from it (`<Compile Remove=... />`) so they aren't compiled twice.
- Put a `.gdignore` file in `src/` and `tests/` so the Godot editor doesn't scan or import them (including their `bin/`/`obj/` folders).
- The simulation library and tests target the same .NET version as Godot's generated `.csproj`.

## Architecture rules

These come from the charter's Technical Principles. Treat them as hard constraints for every change.

### Keep the simulation separate from presentation

- Layering: **Simulation → Game State → Presentation**. Dependencies only point that way.
- Simulation code lives in `src/OpenAntares.Simulation/` and must not use Godot types: no `Node`s, scenes, `Vector2`, `GD.*`, input, or rendering. It runs headless under plain .NET.
- Presentation reads game state and sends commands. It never changes game state directly.
- Game state is plain, serializable data: no engine objects, delegates, or object references between entities. Refer to entities by stable IDs.

### Route every action through commands

- Every change to game state, whether from the local player, the AI, or a remote player, is a **command**: a serializable description of intent, tagged with the issuing empire.
- All commands go through the **same** validation and turn-processing path. AI empires get no shortcuts and no hidden access to state they shouldn't see.
- Never assume a single human player. There is no "the player" global inside the simulation, only empires, some of which are controlled by humans.
- Network transport (if it ever exists) only carries commands and state. It contains no game rules.

### Make turn processing deterministic

The same state, commands, and seed must produce the same result on every machine (Windows/Linux/macOS, x64 and ARM).

- **Use whole-number maths in the simulation.** Game state and rule calculations use integers or fixed-point values (e.g. store 125.0 as `1250` tenths). No `float`/`double` in simulation state or in any calculation that affects outcomes, because floating-point results can differ slightly across CPUs and would break cross-platform multiplayer and replays. Avoid trig and `Math.Sqrt` in rules. Use integer alternatives or lookup tables. Presentation may use floats freely.
- **Use the project's own seeded PRNG.** It's a small, fixed algorithm implemented in the simulation library, with its state stored in game state. Never use `System.Random` (its algorithm isn't guaranteed stable across .NET versions), Godot's `GD.Randf()`/`RandomNumberGenerator`, `Guid.NewGuid()`, or wall-clock or frame time.
- **Iterate in a defined order.** `Dictionary`/`HashSet` enumeration order isn't guaranteed. Sort by ID, or use ordered collections, whenever order can affect the outcome.
- **Never rely on `GetHashCode()`.** .NET randomizes string hashes per process, so never use hash codes for anything saved, compared across runs, or order-dependent.
- **Use invariant culture.** Parse and format numbers with `CultureInfo.InvariantCulture`, so content and saves behave the same in every locale.
- **Process turns in explicit, documented phases.** Don't use events, signals, or frame callbacks to drive the simulation.

### Build the interface in code, style it with one theme

- **UI is built in C#,** not in scene files. Create controls in code and compose screens from reusable C# component classes (e.g. a resource readout with a breakdown tooltip). Most 4X screens are generated from changing game data anyway.
- **Scene files are minimal.** `game/Main.tscn` is the entry point: a root node with a script that builds everything else. Only add another `.tscn` if the user asks for hands-on editor control of a specific screen, and keep it a thin shell that code fills in.
- **All styling lives in `game/theme/openantares_theme.tres`,** which is applied at the root so every control inherits it. That covers fonts, colours, `StyleBox`es, margins, and button states. The user restyles the game in Godot's theme editor, so code must not override it:
  - No hard-coded colours, fonts, font sizes, or styleboxes in C# (`AddThemeColorOverride` and similar are off-limits for styling).
  - For variants such as a header label, a warning text, or a positive/negative value, define a **theme type variation** in the theme file and set `ThemeTypeVariation` in code.
  - Name variations clearly and consistently, because they're the vocabulary the user sees in the theme editor.
- **Layout uses containers and anchors,** not absolute pixel positions, so screens adapt to different resolutions and aspect ratios.
- The theme file is shared with the user's editing. Read it fresh before changing it, and make minimal edits so their tweaks aren't lost.

### Put content in data files

- Technologies, buildings, weapons, ship components, species traits, governments, events, planet types, and similar content belong in JSON under `content/`, not in code.
- Ordinary mods (new content, balance changes) must be possible without editing or recompiling code.
- Keep logic in code only where a rule genuinely needs it. Prefer generic, data-parameterized mechanics over special cases keyed to one specific item.
- Reference content by string IDs from the data, not by enums baked into code.
- Validate content at load time and report clear errors (file, entry ID, field) instead of failing later.

### Explain the numbers

- Strategic transparency is a design pillar. When the simulation calculates an important value (output, growth, research, combat odds, diplomatic attitude), it should be able to return a breakdown of the contributing modifiers and their sources, not just the final number. Design calculations so the UI can show "why".

### Test the game logic

- Core simulation systems need xUnit tests in `tests/OpenAntares.Simulation.Tests/`: resource calculations, population growth, production, research progression, movement, combat math, and tech prerequisites.
- Add or update tests with every simulation change, and run `dotnet test` before committing. Determinism makes exact-result tests possible, so use them.
- Include determinism tests: running the same seed and commands twice must produce identical state.

## Scope discipline

- The current target is the **First Playable Milestone** in the charter: new game, galaxy generation, galaxy map, star and planet inspection, one colony, workforce allocation, production, research, end turn, save/load. It does not include AI, diplomacy, or combat.
- Don't build ahead of the milestone. Leave room for later systems (the command path, IDs, data-driven content), but don't implement them early.
- The strategic game comes before tactical combat and graphical polish.

## Content and licensing

- Code is MIT. Original assets are CC0. Docs are CC BY 4.0. Content JSON counts as code (MIT). See [LICENSING.md](LICENSING.md).
- Never add copyrighted assets, names, or text from commercial games. Don't copy a legacy game's interface or mechanics feature-for-feature.
- Any third-party file must keep its license, and that license must be recorded in LICENSING.md.
- Branding (logo `art/branding/icon.png`, title art `art/branding/OpenAntares-title.png`) lives in `art/branding/`. `icon.png` is the project icon.
