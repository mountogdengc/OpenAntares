# Ship-design deferred-item follow-up — 2026-10-30

All four authorized deferred findings are closed on retained `codex/ship-design`, product source `37963156f7d624acf3e7be1b4d43e5f8002ed1df`. M10-I and all eleven functional SHP acceptance criteria are **Verified for local automated scope**. Design approval remains separately scoped to ADR-022. No known open implementation-review finding remains. Human, platform and delivery acceptance remains unverified.

Owner authority: “Go ahead and finish the four deferred items.” This continues `1b46a77`; the previous review and deferral records remain unchanged as historical evidence. No additional independent reviewer, main integration, push, release or cleanup occurred.

| Finding | Result and regression evidence |
| --- | --- |
| M1 Canonical banks | Immutable recipes sort banks by ordinal ID at creation, copying and load. Reversed mixed banks produce the same saved recipe; the same-name retry changes no bytes/readiness/counters. Earlier rules10 saves with unsorted lists remain readable and normalize on load; exact bank multisets, required fields and enum spellings stay validated. |
| M2 Early overflow | Content loading checks every capacity-fitting one/two-bank combination, including optional frames, engines and armor, using wide integer intermediates. Overflowing cost, speed or summed attack returns contextual file/definition/field errors. Basic packs without advanced parts still load. |
| M3 Equipment inspection | Catalogue and owned vessel inspection show engine, armor/None and counted weapon banks, including archived installed designs. Refit review shows current/target recipes and exact removed/installed counts. Standard Escort, two-pulse Gun, mixed-bank Frigate and archived equipment pass at800×600 and1920×1080; Apply remains reachable. |
| M4 Colony entry | Construction has a Ship designs button opening the existing catalogue/editor. Saving via this path preserves the current project, reserve and queue, immediately offers the new design, and changes the project only after explicit picker selection. Both sizes pass with reachable Save. |

Verification used the existing checked-out .NET environment (SDK10.0.114, net8 target/runtime8.0.33), Godot4.7.2 .NET on Windows x64, and the evidence-preserving gate wrapper. No dependency restore or audit is implied.

| Gate | Exit/result | Retained evidence |
| --- | --- | --- |
| Clean starting suite | 0;2026/2026 | `ship-design-execution/gates/polish-baseline-01.*` |
| M1/M2 RED | 1;four intended missing-behavior failures | `ship-design-execution/gates/polish-m1-m2-red-01.*` |
| Diagnostic reruns | 1 each; retained and resolved | `polish-m1-m2-green-01/02.*`: first a solo all-ready test fixture, then a save-shape assumption exposed by normalization |
| Final full suite | 0;2030 passed,0 failed/skipped | `ship-design-execution/gates/polish-suite-green-01.*` |
| UI RED | 1;six intended failures across both sizes | `ship-design-execution/polish/ui-red-01/results.json` |
| ShipDesign GREEN | 0;2017 records, four genuine earned species campaigns | `ship-design-execution/polish/ui-green-01/results.json` |
| Colony GREEN | 0;682 records | `ship-design-execution/polish/colony-green-01/results.json` |

Both final native runs have0 engine diagnostics,0 build warnings/errors and real opt-in10-scenario/2-helper detection followed by ordinary rebuild with0/0 excluded types present. New comparison/catalogue text and embedded screenshots were inspected. UI signals, native bounds and focus checks do not establish physical input, readability/accessibility or aesthetics. The other eight native scenarios remain earlier-source evidence; they were not rerun for this bounded follow-up. Old frozen continuation and all56 compatibility-pair checks pass in the full simulation suite.

Preservation audit `ship-design-execution/polish/preservation-audit-02.json` confirms10720 primary tracked files exact, main clean at4c58403, all9 worktrees and prior branches/stashes retained, four protected owner files per checkout and peaceful paragraph exact,6736 original QA saves exact, and both frozen archives (260 manifest-listed files) unchanged. All17 parent requirement rows/totals remain12 Verified/3 Implemented, Unverified/1 In Progress/1 Deferred. Historical reports, failed runs and ignored SDD workspace remain.

The machine record is [summary.json](ship-design-execution/polish/summary.json). No new scope rulings or deferred findings were introduced. No second review was dispatched, consistent with the retained execution preference. Full human/platform/balance/security/export/release acceptance remains open. Next: owner disposition of the completed local branch; preserve its worktree and evidence.
