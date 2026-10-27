# OpenAntares project status

Updated2026-10-27 America/Denver. OpenAntares is an original turn-based space4X with C#/Godot .NET presentation and an independent deterministic simulation. The approved classic Master of Orion II love-letter direction and fanciful aliens respectful of real peoples/beliefs remain authoritative.

Completed M7 integration: the owner authorized “merge that”; completed scout survival/encounter avoidance, stationary space-ameba danger and the remaining setup explanation follow-up are now locally integrated into main. Validated product source `a52dfc61c9a16ec10e0091b52783029afac73e38`; the handoff commit updates documentation/evidence only. [Fresh main integration report](reports/SCOUT_INTEGRATION_2026-10-27.md) records1398 simulation tests and all seven native scenarios/4491 records, normal build/exclusions, checkout preservation and owner-edit recovery. No known review finding remains open. Do not repeat completed work or re-ask design, plan or Native approval.

Current continuation: the owner described central fleet/shipbuilding command, named admirals with bonuses/service records/simple experience and ability unlocks, plus culture-based loss doctrine. The owner has confirmed doctrine editable from the start and research improving recovery equipment; Navigator, Surveyor and Organizer and10/30 XP promotions are also confirmed; do not re-ask those choices. [M8 draft](docs/superpowers/specs/2026-10-27-fleet-command-and-admirals-design.md), [ADR-017](docs/DECISIONS/ADR-017-fleet-command-and-admirals-design-entry.md) and [fresh builder dispositions/document checks](reports/FLEET_COMMAND_DESIGN_REVIEW_2026-10-27.md) record a proposed bounded first slice. M8-D/FLT-AC-012 In Progress; M8-P/M8-I and functional acceptance Not Started. All new formulas/defaults/rules8-writer9 are proposals awaiting full gameplay review. Broader automation notes are preserved as future directions. No code/content/default/schema or primary-main change occurred.

## Current product and scope

Main defaults to rules7, explicit ameba Standard/Off, Harl/One Peldra/plague Standard, writer8 and29 explicit schema/rules pairs. Observe is the neutral default, Avoid opt-in; ameba withdrawal is separate and enabled. One stationary habitat, harmless Warning/100% prompt departure, explicit risky Study/late escape and real vessel/cargo loss follow the approved specification. Ordinary peaceful rival/exploration rules, committed travel, private own knowledge and atomic command/turn publication remain. Old saves retain embedded rules/content and unchanged futures.

Roaming creatures, colony attacks, sensors/pursuit, tactical combat, warship doctrine, broader diplomacy/culture, multiplayer and victory remain future scope. Peaceful mode remains an owner backlog decision. No artwork dependency was introduced.

## Status and evidence

| Milestone | Status | Scope |
| --- | --- | --- |
| M1 First Playable full human acceptance | Deferred | Owner-authorized deferral; technical work may proceed |
| M2 exploration/colonization, M3 species, M4 rivals, M5 colonies, M6 plague | Verified | Previously integrated local automated/design scope; human/delivery limits remain |
| M7-D / SCT-AC-012 | Verified | Full amended design approval ADR-016 |
| M7-P | Verified | Owner-approved15-task plan; retained Native inline execution |
| M7-I | Verified | Local automated scope; review corrections/M1 closure and fresh main verification complete |
| M8-D / FLT-AC-012 | In Progress | Complete proposed Fleet Command/admiral design and fresh builder dispositions; owner review pending |
| M8-P / M8-I | Not Started | Separate plan follows full design approval; no implementation |

Parent requirement totals remain17:12 Verified,3 Implemented, Unverified,1 In Progress (REQ-017),1 Deferred. This milestone does not close REQ-017 or change the register's17 rows.

Fresh main:1398/1398 simulation tests; Scout 237; Legacy 493; Expansion 275; Species 1194; Rival 1158; Colony 670; Crisis 464; all pass with0 diagnostics. Each ordinary rebuild has0 warnings/errors and excludes all7 scenarios/both helpers. Five custom-map boundaries cover the omitted-habitat explanation, usable Start, independent Off, private candidate locations and deterministic previews.24 generated campaigns/720 Off-Observe parity boundaries and all49 frozen rules6 baselines/196 old-engine futures retain verified simulation coverage. Rare threshold fixtures remain separately labelled. [Earlier criterion mapping](reports/SCOUT_VALIDATION_2026-10-25.md), [review/disposition](reports/scout-execution/final-review.md), [M1 follow-up](reports/SCOUT_POLISH_2026-10-26.md) remain historical; their prior integration deferrals are superseded by the owner's merge authorization.

## Preservation and integration

Local main fast-forward integration is complete. The three separate owner files retain their original byte-exact whole-file hashes; the original peaceful-mode paragraph remains byte-exact and uncommitted in the primary checkout; it is deliberately absent from this clean design branch. All seven worktrees, branches, old saves, recovery/QA and failed/successful evidence remain. Recovery branch `codex/scout-premerge-20261027` retains42e1282; the status-only stash and private exact backups remain available. The paragraph is already restored; do not blindly reapply the stash. Byte-preserving archive attributes fix the reproduced checkout newline issue;425/425 frozen/archive hashes match an actual checkout. No remote push, release, cleanup or history rewrite occurred. [Preservation details](reports/SCOUT_INTEGRATION_2026-10-27.md).

M7 authority: [ADR-016](docs/DECISIONS/ADR-016-approved-scout-survival-and-ameba-design.md), [spec](docs/superpowers/specs/2026-10-25-scout-survival-and-encounter-avoidance-design.md), [plan](docs/superpowers/plans/2026-10-25-scout-survival-and-ameba.md).

## Remaining limits and next action

Physical input/focus, comfortable reading/accessibility/comprehension, aesthetics, balance/long campaigns, other platforms, hostile-input/security/dependency audits, clean install/export and release remain unverified or deferred. Automated local checks do not establish these.

Review/amend the complete proposed M8 Fleet Command/admiral gameplay rules before a separate implementation plan. Preserve all owner edits, branches/worktrees and evidence. No implementation, remote push, release or cleanup is inferred.
