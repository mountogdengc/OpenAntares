# M14 visual overhaul — ready for player acceptance

2026-11-08. Active isolated checkout: `C:/Users/david/.codex/worktrees/m14-visual-overhaul/m13-playability`, branch `codex/ui-visual-overhaul`, uncommitted changes over M13 handoff800a601. Main remains60044afd. No commit, merge, push, release, worktree/branch removal or evidence cleanup occurred.

M14-I is **Implemented, Unverified** for owner acceptance. The bounded automated scopes below are **Verified**. No owner observation or full-milestone acceptance is inferred. The combined build contains M13 campaign polish and M14 presentation changes.

## What changed

- Original navy metal/chamfered console frames, original resource/role/worker icons and bundled OFL display/body/mono fonts, applied through the shared theme.
- Empire status strip with local production reserves, research, population, signed forecast changes, breakdown tooltips and research progress. Population uses published final colony outcomes, including embarkation; it does not confuse growth points with people.
- Persistent command keys, visible hotkey hints, gear menu, decisions count and End Turn guidance. Game menu IDs0/1/2 remain Save/Load/Latest report.
- System/Empire/Fleets context tabs, planet portraits/owners/actions and colony outliner. Selecting and inspecting issue no orders.
- Nonpaginated grouped ship Tree with counted groups and explicit vessel-ID leaves; request/design/fleet/officer/commitment/damage distinctions remain separate.
- Full-height embedded, borderless, nonexclusive management workspaces; Research and Colonies use normal routed controls. Scouts/Contacts live in their parent tabs, with compatible older-campaign access. Existing command-review confirmations retain their authority.
- Colony workforce figures with drag/keyboard reassignment through the existing command processor; detailed colony construction remains reachable.
- Map glow/backdrop, ownership rings, fleet flags/counts, selected-route ETA and known-data overlays. Range is disabled because the game exposes no finite-range rule. No star classes or extra randomness were invented.
- Optional static screen effect and reduced-effects preference stored outside campaign saves. Shorter instrument copy and conditional readiness wording.

## Scoped verification

All heavy work used the installed shared queue for `openantares` and the actual isolated source root. No Docker or host desktop automation was used. The queue-owned review script holds one lease across its foreground build, native check and normal restoration; it never acquires a second lock or nested queue lease.

| Check | Result and evidence |
| --- | --- |
| UI asset import | Passed; `m14-assets-import.log`, queue601cd6a12b5445b5a07f12bf4a1a3498. |
| Initial changed-screen playthrough | PlayerFlow01:86 checks/19 frames; source/DLL/restoration verifier passes, development-only. |
| Compact layout and interaction extension | PlayerFlow02 native128 checks/20 frames pass: five map/ledger sizes, primary workspaces at800/1920, worker keyboard/drop outcomes, Back/Esc, moved tabs, editing suppression, display preferences, contrast and ordinary End Turn comparison. Its final inventory verifier correctly fails after additional review-check source was added while normal restoration waited in the shared queue. It is retained as recorded-source development evidence, not promoted to current final acceptance. |
| Changed legacy UI adapters | Common/M13 adapters compile with0 warnings/errors in `m14-review-red02-control/changed-adapters-compile.log`; no common scenario or M13 suite ran. Replacing ItemList with Tree required these direct adapter updates. |
| One fresh review | `reports/M14_VISUAL_REVIEW_2026-11-08.md`: four Important and one Minor, no Critical. Exactly one reviewer; no second review. |
| R1–R4 correction checks | Review-red03 reproduces all four findings (five failed conditions). Review-green01 passes45 checks/5 frames, zero diagnostics, stable recorded DLL, complete source evidence and empty ordinary validation namespace. Queuec3b012b8053c4a7f95e0d89ea043397f. |
| Population outcome correction | Population-red01 fails the embarkation decrease. Population-green01 passes8 checks/1 frame, covering two embarking colonists, one birth, zero colonies and no state mutation; evidence verifier passes. Queueecf41d6bbf9b491ea7a4c05b3f2b59e0. |
| Styling boundary | Pass;23 frozen legacy occurrences remain versus26 baseline, no allowance growth or new styling overrides. |
| Final ordinary playtest build | Queue555fb4976237481aa8afa0b46c20a015;0 warnings/errors, forbidden scenario/helper/validation namespace arrays empty. `final-playtest-source.json` captures the exact final normal DLL and source inventory. A final nonfunctional edit preserved the legacy GovernorTemplateDialog line endings and removed diff whitespace warnings; no runtime test was repeated for whitespace. |
| Source scope | Git diff under src/content/schema is empty. Rules12/writer13/79 compatibility pairs and gameplay/save code are unchanged. Main and prior worktrees/branches/evidence remain retained. |

No fresh full Windows suite, unrelated twelve-scenario matrix, campaign/future replay, release/platform/security/balance or large-fleet performance claim is made. The owner explicitly superseded the draft's broad gates with change-scoped validation. Historical M13 results retain their original scope. The two early review-setup compile failures are retained: old scrollbar API in an adapted check, then two review-harness compile errors; neither is labeled a native pass.

## Six bounded corrections

| ID | Correction | Verification |
| --- | --- | --- |
| R1 | Restore ordinary Contacts entry on rules4–10 and Scouts access on rules7 without a Fleet Command controller. | Two labelled pre-session compatibility setups; actual visible route controls, no state mutation, RED→GREEN. |
| R2 | Relay guarded shortcut events from the focused embedded workspace viewport; retain text/popup/confirmation/repeat suppression. | Engine-dispatched C from actual fleet focus navigates; engine-dispatched typing in a design text field stays there; no commands, RED→GREEN. This is not physical OS-input acceptance. |
| R3 | Exclude per-ship combat/crisis identity from equivalence while retaining all relevant facts and exact leaves. | Two equivalent projected ships become one counted group; differing request stays separate; exact leaf IDs remain, RED→GREEN. These are labelled projection fixtures, not earned gameplay evidence. |
| R4 | Suppress report-origin restoration during intentional toolbar transitions while preserving ordinary Continue-to-origin. | Fleets→report→Galaxy leaves no orphan; ordinary Continue restores Fleets, RED→GREEN. |
| R5 | Remove unconditional readiness promises from absence-of-notices copy. | Regraded Important for player comprehension against the already-known authoritative readiness counterexample. Wording now says End Turn checks plans. No mirrored test was added for a copy-only correction. |
| R6 | Include published embarkation outcome in the existing-colony population delta. | Embarkation decrease, birth, zero-colony and state-preservation cases, RED→GREEN. |

Six of six bounded follow-up corrections used. No simulation/content change or seventh repair. No open required review finding; no deferred review minor remains after the readiness wording correction. Initial compile/layout iterations occurred during implementation, before this bounded correction pass.

## Rulings and limits

The adopted AC section is the M14 specification; the owner's testing rule supersedes UIX-AC-012's full-suite requirement. New code is verified by selected affected checks, not by repeating unrelated milestones. The real Window-backed controllers were retained and fitted to routed workspace rectangles rather than replaced with proxy controls. This preserves command/confirmation behavior, but physical focus/drag ergonomics and player comprehension still need the owner playthrough. Range remains unavailable rather than displaying invented rules. Fonts/assets are original or explicitly licensed; no commercial reference assets were copied. The ledger and all raw evidence are retained rather than cleaned up.

The final ordinary build source record is development/uncommitted evidence, not committed-source acceptance. All automated fixtures are distinguished from earned campaign evidence. Human look/comprehension, physical OS input, broad accessibility and cross-platform acceptance remain Pending or outside this bounded scope.

## Player handoff

Use `docs/playtests/m14-player-acceptance-2026-11-08.md`: eight player journeys, intended outcomes and plain report-back prompts. `reports/visual-overhaul-execution/Play M14.lnk` opens the normal build; the launcher checks that exact build and creates a unique retained log on every launch. No test tools or source inspection are requested from the player.

Next action: owner playthrough of M13+M14, then disposition of any concrete usability findings. No automatic main integration or release is authorized.

Owner handoff: normal M14 game launched for the requested combined playtest, process63244, with a unique retained owner-playtest log. Launch confirmation is process/window presence, not human acceptance.
