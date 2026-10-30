# Ship design and refitting validation

Run date:2026-10-30; approved plan dated2026-10-29. **Implementation/review correction complete; four Minor deviations remain open. Full-spec acceptance is not claimed. Final Task17 formal gate registration is pending.**

The isolated codex/ship-design branch implements immutable ship recipes, exact-design construction/assembly, manual research unlocks and paid same-frame refitting. Current new games use rules10, writer11 and56 explicit schema/rules pairs. Standard Escort reproduces the old2400/100/24/4/300 profile. Existing rules1..9 retain their embedded content and original outcomes.

Final corrected product source:a11f9b8cc454550f03b8d452070eca9f9ca71fd6. Initial combined product source:4002ea31b550fd4a9763f8e308d13c472b962b7d. Reviewed implementation/harness head:4d4e2ae6d384779295944459f5b34ae6e4902311. Accepted planning base:4c58403b107063a616645905b6f6f86f86676ca6. Primary main remains at that base, clean. No integration, push, release or cleanup.

## Executed combined baseline

Full simulation suite2026/2026; focused compatibility and atomicity32/32. Native scenarios ran sequentially with unique attempt folders. Selected passing runs have0 compiler warnings/errors,0 actual engine diagnostics, real opt-in detection of all10 scenario classes plus2 helpers, and ordinary rebuild/exclusion of all12 types.

| Native scenario | Validation records | Separate provenance | Selected attempt |
| --- | ---: | ---: | --- |
| ShipDesign | 1837 | 0 | 02 |
| Combat | 3307 | 105 | 01 |
| Fleet | 1680 | 39 | 01 |
| Scout | 240 | 9 | 01 |
| Crisis | 482 | 0 | 02 |
| Colony | 682 | 0 | 02 |
| Rival | 1194 | 0 | 02 |
| Species | 1194 | 0 | 02 |
| Expansion | 275 | 0 | 01 |
| Legacy | 493 | 0 | 01 |

Total:11384 native validation records;153 separate provenance entries. These are heterogeneous assertion/geometry/control records, not11384 independent simulation tests. The initial sweep had six passes and four old current-version assertion failures. Those four harness expectations were updated to current10/current11 technologies; all explicit older sources/oracles remain unchanged. Five affected scenarios were rerun, including the strengthened earned queue case. All failed attempts remain.

## Genuine and supplemental sources

Four untouched actual Main New Game Small42 Solo campaigns, one per Harl/Peldra/Sulan/Wenth, use plague/ameba Off and Sentries Standard. Actual controls earn Measurement Methods1200, Industrial Coordination2400 and Vector Drives3600, save an advanced Fast Escort, pay two2800 hulls through a finite queue and exact-design request, scout the generated pocket, fight, withdraw, return and repair normally, then pay two700 refits. A later trip uses changed300 speed and an actual volley records48 attack. Total research paid7200; construction5600; refits1400. Every payment is reconciled against earned output and retained reserve; live outcome injections0.

The strengthened case temporarily moves production workers to support through actual controls while researching, restores ordinary production and saves only after an unpaid interval pins active queue token1. Checkpoint reserves are400/600, below2800. Actual Main Save/Load at catalogue, unfinished active queue, live request and active refit matches three legal future turns against uninterrupted continuation. Corrected campaign lengths are47/37/47/47 turns, not a balance deadline. A separate labelled pre-session paused-refit source uses actual Main Save/Load, Resume and three equal futures; it is not a fifth earned campaign.

Combat separately continues12 original genuine rules9 turn0 sources through ordinary Main Load after checking current10 setup. The old-engine archive locks72 genuine stage sources/144 checkpoints,11 labelled supplements, and1 action source/2 checkpoints:84 sources/168 checkpoints total. No frozen expectation was regenerated under M10. Raw manifest hashes and all260 manifest-listed archive files remain exact.

Labelled prepared cases additionally cover32 active/256 retained designs,16-row catalogue pages,32-row exact idle pages with36 candidates, damaged/busy/paused/archived refit work, late invalidation, failures, two-human privacy, extreme counters and strict corrupt-save rejection. Prepared resources/research/hulls are declared before the session; they do not replace earned campaign evidence.

## Acceptance evidence index

Passing automated evidence is distinct from full required acceptance. The four open Minor deviations leave SHP-AC-001,002,007,010,011 Implemented, Unverified; the other six functional criteria are Verified for local automated scope. SHP-AC-012 remains design approval only under ADR-022. M10-I is Implemented, Unverified overall. Parent REQ rows and human acceptance are unchanged.

| Criterion | Scope | Primary evidence classes / approval | Disposition |
| --- | --- | --- | --- |
| SHP-AC-001 | Content/setup | ShipDesignContentTests, ShipDesignStateTests, ShipDesignGenerationTests | Implemented, Unverified — M2 |
| SHP-AC-002 | Immutable catalogue and references | ShipDesignCatalogueTests, ShipDesignQueryTests | Implemented, Unverified — M1,M3 |
| SHP-AC-003 | Exact component math and loadouts | ShipDesignMathTests | Verified for local automated scope |
| SHP-AC-004 | Research availability, old gates, no free upgrades | ShipDesignContentTests, ShipDesignCatalogueTests, ShipDesignGenerationTests | Verified for local automated scope |
| SHP-AC-005 | Typed paid production and finite queue | ShipDesignProductionTests, ShipDesignSaveTests | Verified for local automated scope |
| SHP-AC-006 | Exact request identity/officer accounting | ShipDesignRequestTests, ShipDesignSaveTests | Verified for local automated scope |
| SHP-AC-007 | Refit eligibility, prices, tokens, locks | RefitCommandTests, ShipDesignAtomicityTests | Implemented, Unverified — M3 |
| SHP-AC-008 | Late paid resolution and report ledger | RefitResolutionTests, ShipDesignAtomicityTests | Verified for local automated scope |
| SHP-AC-009 | Equipped integration/privacy/peaceful rival | ShipLoadoutIntegrationTests, ShipDesignQueryTests, ShipDesignGenerationTests | Verified for local automated scope |
| SHP-AC-010 | Strict56 pairs,84/168 old futures and rollback | ShipDesignSaveTests, ShipDesignCompatibilityTests, ShipDesignAtomicityTests | Implemented, Unverified — M1 |
| SHP-AC-011 | Native controls and scale | ShipDesignGraphicalValidation, ShipDesignCampaignChecks | Implemented, Unverified — M3,M4 |
| SHP-AC-012 | Full design approval | ADR-022 | Verified for design only |

## Review and correction disposition

One fresh whole-branch review: gpt-6-astra/high, read-only, no delegated reviewers. Base4c58403..4d4e2ae; five review-focus conditions and every ledger ruling supplied. The [raw review](ship-design-execution/task-17/fresh-review-raw.md) and [severity triage](ship-design-execution/task-17/review-triage.json) record no Critical, one Important and four Minor findings. I1 failed in all four actual direct/queued800/1920 cases, then passed with the committed archived target displayed as a disabled selected row. Post-correction Ship Design1955 records and Colony682 records pass at corrected sourcea11f9b8, with0 build/engine diagnostics and actual10+2 exclusions. The full suite passes2026/2026 after the fix. Only those affected native scenarios were rerun after correction; the other eight native results remain scoped to the initial combined source. No second review occurred.

## Preservation

Audit01 verifies primary10720 tracked files byte-exact and clean; execution10624 old files outside authorized diffs exact at that checkpoint; all four protected files exact per checkout; the exact peaceful-request paragraph text;6736 original QA saves exact; both locked84/168 archives; all9 worktrees/eight prior heads; unrelated refs/stashes; all17 parent rows. Parent totals remain12 Verified,3 Implemented/Unverified,1 In Progress,1 Deferred. Fresh QA and all failed attempts are retained. Final disposition includes a second audit after documentation changes.

## Limits and handoff

Automated Windows x64/Godot4.7.2 .NET control signals, canonical state comparisons and geometry establish local implemented behavior. Supplementary PNGs explicitly embed popups and inherit canvas scaling; they are not native-window pixels. Physical mouse/keyboard input, reading comfort, accessibility, player comprehension, aesthetics, balance, long campaigns, other platforms, security/dependency audit, exports and release packaging remain unverified. Optional tactical combat controls and all excluded gameplay remain later scope.

Keep the isolated branch, all prior worktrees, branches, QA, raw evidence and ignored SDD workspace. Main integration requires the owner's separate selection; none is performed here.

## Rulings made during execution

Ruling: native create_worktree rejects the actual F: repository because it is outside this non-repository chat workspace — use the plan-authorized Git fallback in the existing C: .worktrees directory and retain it — cost if wrong: manual registration only.

Ruling: preserve this plan workspace, all raw QA and worktree after final review under the owner preservation instruction, overriding skill cleanup defaults — cost if wrong: additional disk use.

Task1: Ruling: add a separate old-engine action archive after the first immutable freeze because explicit Fleet/Officer/Assembly fingerprint transcripts were missing — preserve the original72-source/144-checkpoint archive unchanged and capture one extra source/two checkpoints with the still-unchanged9 engine — cost if wrong: four extra fixture files and replay maintenance.

Task2: Ruling: place the four new technology definitions in the established technologies.json rather than a second technologies section in ship_design.json — existing current-content fixture/UI assemblers require one top-level section per source pack; retain strict module-prerequisite validation and all seven old definitions unchanged — cost if wrong: one additional existing content file is part of Task2. Failed Suite154/1757 retained; no frozen oracle changed.

Task2: Ruling: update two preexisting content-test setup adapters to exclude the new optional section from the original economy comparison and rename its declared binding together with the role — strict current-content validation requires coherent fixtures; gameplay assertions and all frozen sources remain unchanged — cost if wrong: two existing test files are part of this content task.
Task2 gates: new content22/22, fullSuite1757/1757, all old future archives unchanged. Default remains9/writer10. RED and failed intermediate attempts retained.

Task4: Ruling: adapt the owned combat-view projection through VesselLoadouts now, alongside the new own-loadout facts — projecting a valid Frigate180 through the old role100 profile would throw before the required immutable view resolver can be used; Task10 still owns the remaining behavioral call-site audit — cost if wrong: one query adapter lands earlier.

Task5: Ruling: introduce the nullable AssemblySlot.Design field before Task7 so the Task5 shared reference index can count retained historical request identities exactly, without inferring them from current ships — quota acceptance, fulfillment, view/save adapters and validation remain owned by Tasks7/12 — cost if wrong: one small state field lands earlier; null is omitted from old JSON fingerprints.

Task6: Ruling: retain own catalogue facts in GovernorPlanningQueries now, ahead of the comprehensive Task10 context audit — the required Task6 custom-build forecast cannot resolve a pinned recipe from a role-only reconstructed owner; no foreign facts enter the frame — cost if wrong: a narrow context adapter lands earlier.

Task7: Ruling: factor quota normalization and exact target/recipe lookup into internal Fleets/AssemblyDesigns.cs under the approved file-organization discretion — one shared adapter prevents planner, price and binding identity drift; legacy fingerprints retain their original serialized material — cost if wrong: one small additional helper file. Core tests18/18 and fullSuite1865/1865 passed. Historical completed slots retain original design identity; Task9 still owns the actual paid-refit continuation.

Task8: Ruling: an idle port still contributing to an Active assembly request requires an explicit contribution pause before starting a refit — this implements the approved prohibition on implicit assembly takeover; a paused contribution/request may retain its queue behind the manual refit — cost if wrong: one extra explicit pause action. No new quarantine restriction is added to zero-cargo warship work; existing output/health rules remain authoritative.

Task9: Ruling: extend the permitted GovernorPlanningQueries frame with own jobs, equipped ships, fleet Hold and combat membership facts now — provisional refit forecasts need these facts before Task10 comprehensive adapter audit; no world or foreign catalogue reference enters the context — cost if wrong: one context adapter lands earlier. Late unaccounted reserve or committed recipe/job mutation fails the whole resolution rather than inventing a balancing receipt; insufficient snapshot reserve simply continues the job.

Task10: Ruling: add optional copied equipment facts to CombatShipFrame and validate historical receipts from those embedded recipe/stats facts rather than a live catalogue row — required to preserve past volleys after a paid refit and legal catalogue deletion; old <=9 frames omit the field — cost if wrong: the existing CombatViews/ReceiptValidator codecs must include the strict versioned field in Task12. Task10 reload wording depends on Task12 writer11: verify clones/new behavior now, retain all old actual reload futures, and run actual new10 reload continuations in Task12 before activation.

Task12: Ruling: extend GovernorReportEntry and its preparation/validator alongside the strict codec with copied before/after equipment facts — historical governor targets must survive legal deletion without an undisclosed live catalogue reference, as required by the approved spec and Task9 handoff — cost if wrong: four narrow development adapters land before Task13. Existing current-writer assertions advance to11 and unsupported-next-version inputs to12/11; frozen raw source/checkpoint expectations and original rule semantics remain unchanged. Task11 hooks use an explicit internal callback threaded through the same ordinary Ready transaction, without ambient/global mutable state.

Task13: Ruling: report all bounded pending refit events, including accepted/cancelled/resumed commands, once on the next boundary and move them into retained history — the required owner report must explain both paid and unpaid work, not only resolver-produced completions. Update the Task11 insufficient-reserve assertion to require exactly an unpaid accepted event and still forbid any completed/payment event — cost if wrong: the new10 report includes additional own command facts; old1..9 reports/futures remain unchanged. Catalogue paging is pure, 16 rows with an explicit previous-filter argument for reset; one shared own reference index is built per projected catalogue.

Task14: Ruling: add an optional pure DesignPreview.Explanation populated by the existing checked math exception, with used/capacity space in that one math source — the actual native invalid7/6 check proved the generic rejection did not explain the selected excess; no UI-side stat formula is introduced — cost if wrong: three small simulation query/math/view adapters are part of the UI task. Native red01 reached the real equipment preview; red02 text-entry fixture omitted TextChanged; corrected red03 reached missing7/6 feedback. Green02 exposed the test helper excluding internal AcceptDialog buttons; use the real GetOkButton API for Cancel. All attempts retained. Green03:103 checks,10 native geometry records,10 explicitly embedded PNGs; actual10+2 opt-in and0+0 ordinary metadata,0 engine diagnostics/build0 warnings/errors. Representative800/1920 PNGs inspected; inherited canvas scaling and all human-input/readability limits remain unverified.

Task15: Ruling: add one ShipDesignUi typed-metadata/own-label helper, adapt GovernorReportPanel to the copied facts introduced in Task12, and add immutable ProductionEquipment to the owner-only ColonyReportView projection — actual native paid construction exposed the old role/building lookup in both report renderers; historical labels must survive catalogue deletion — cost if wrong: one small UI helper and three narrow report/query adapters extend the listed files. No new persistent schema/state field or hidden-world lookup. Red03 exercised eight behavior failures across both sizes (paid completion label, exact request option, refit fee preview, locked research cost). Green03 compile missed explicit null Design for civilian quota; failed build and finally rebuild retained; subsequent runner rebuilt ordinary successfully.

Task15: Ruling: bind a new Start preview to the already-persisted own NextRefitId allocation, carrying that counter in the permitted view/context — regression OldStartPreviewCannotRecreateACancelledJobInTheSamePlanningTurn proved an old token could otherwise allocate a replacement after same-turn Cancel; old Cancel/Resume checks alone missed it — cost if wrong: a new unrelated own job allocation may require refreshing another uncommitted Start preview; foreign changes remain excluded, and active-job no-ops ignore the new allocation counter. No state/schema field added. Also adapt AssemblyRequestDialog (the actual quota-picker owner omitted from the Task15 file list) rather than place quota logic in FleetCommandDialog.

Task16: Ruling: retain the12 Combat scenarios by loading their original frozen genuine rules9 turn0 sources through ordinary Main Load after separately testing current10 setup — a current New Game can no longer honestly create rules9 after activation; preserve all original combat expectations/source bytes — cost if wrong: scenario provenance now distinguishes retained source continuation from current New Game. Current-version-only assertions advance to10; no raw future oracle is edited.

Task16: Ruling: exclude ShipDesignOnly technologies from the existing peaceful rival research selector — the regression proved the otherwise unchanged greedy policy would divert into focused_emitters after its seven productive technologies; M10 gives it no warship/refit decisions — cost if wrong: rivals leave those four human equipment technologies unresearched. Old1..9 technology availability remains unchanged. Split long graphical provenance methods into a partial ShipDesignCampaignChecks source (same runtime class,10+2 exclusion unchanged) and extend ShipDesign frame ceiling for the4 actual campaigns; no product time limit or economic deadline.

Task17: Ruling: update the four observed current-setup-only native assertions from9 to10 and current technology count7 to11, leaving every explicit old fixture and expected old outcome unchanged — the ten-scenario sweep produced exactly these four stale-current assertion failures — cost if wrong: four harness adapters change their current-default expectation. Strengthened queue assertion RED proves the old generated case saved enabled-but-unselected work; revised ordinary manual workforce policy earns research with production workers temporarily on support, restores production, then saves after an unpaid active interval. No resource/state injection or product change; actual typed command/future equality remains mandatory.

Final: Ruling: physical input, reading comfort, accessibility, comprehension and aesthetics remain unverified — signal/geometry checks cannot establish those owner acceptance facts — cost if wrong: comfort or input defects may remain until human QA.

Final: Ruling: general balance, optimal loadouts and long-campaign pacing remain unverified — four bounded earned campaigns prove mechanics rather than strategic balance — cost if wrong: later balance work may be required.

Final: Ruling: Linux/macOS, ARM and cross-platform determinism remain unverified — only Windows execution was authorized/performed — cost if wrong: platform-specific defects may remain.

Final: Ruling: security/dependency health remains unverified — implementation tests with no-restore are not a security/dependency audit — cost if wrong: vulnerabilities or dependency issues may remain.

Final: Ruling: export/distribution/release readiness remains unverified — no export/package/release work is authorized — cost if wrong: packaging defects may remain.

Final: Ruling: the extra own-design fleet filter remains optional and unimplemented — SHP-005 permits it rather than requiring it; existing role/design labels and exact request selectors remain — cost if wrong: additional manual filtering is needed.

Final: Ruling: reviewer did not independently rehash preservation bytes — executor hash audits and immutable manifests provide that evidence, with a fresh post-disposition audit required — cost if wrong: preservation assurance depends on the executor audit implementation.

Final: Ruling: automatic upgrades, cross-frame conversion, empire warfare, peaceful-mode redesign and refit healing remain excluded — retained contracts and deferred owner scope govern them — cost if wrong: these gameplay features remain unavailable until separately authorized.

## Deferred minors

M1: weapon-bank canonical ordering is not enforced in catalogue/persisted recipes.

M2: optional nonstarting aggregate overflow is detected at selection/save rather than content load.

M3: direct component summaries and component-change comparison are absent.

M4: the promised colony-side designer shortcut is absent; Fleet Command remains the entry.

These are retained under the executing-plans one-pass workflow. SHP-AC-001,002,007,010,011 remain Implemented, Unverified for full required scope; no full-spec acceptance is claimed.
