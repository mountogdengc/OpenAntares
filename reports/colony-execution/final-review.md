# Independent whole-branch review

Reviewer: gpt-6-astra, xhigh, one fresh read-only context, no reviewer subagents. Reviewed e77b598aac6e3e6f9db0ced025d94d6f3f0e9097..9487b93a6e4146fba9c8f0226c6bdb2bf359648d. This retained report transcribes the returned findings and assessment; executor dispositions are recorded separately.

### Strengths

- Reviewed the complete changed product code, harness code, and tests in separate passes at9487b93 against e77b598. The checkout remained unchanged; no builds, tests, edits, or subagents were run.
- The simulation architecture is sound: immutable viewer projections feed deterministic, scoped planning; ordinary command validation remains authoritative; Ready prepares a private candidate and preserves the exact original state on failure.
- Manual changes pause only the affected automation channel. Queue tokens, active bindings, finite vessel orders, snapshot revisions, reserve conservation, and next-boundary selection are coherently implemented.
- Compatibility is unusually well protected: sixteen explicit schema/rules pairs, strict required rules5 fields, rejected legacy injections, frozen genuine continuation traces, and no economic RNG consumption for appearance.
- Retained evidence is substantial. Independently checked all five results files: zero failed rows, successful top-level results, and all195 listed QA paths present. Logs record982 passing simulation tests, a clean normal rebuild, no engine diagnostics, and the five-harness inclusion/exclusion checks. No product or executable harness changes follow tested source47eb9cb; later changes are documentation.
- Representative retained PNGs support functional rendering, structural portrait variety, reachable dialogs, and the reported manual-override behavior. These do not establish human acceptance.

The three implementation rulings are justified: copying the immutable appearance descriptor in Task2 belongs with observation equality/validation; inheritance belongs in ColonizationResolver at actual creation, preserving read-only preview/same-turn economics; pinning explicit rules4 RivalFixtures protects historical contracts with separate current5 coverage.

### Issues

#### Critical

None found.

#### Important

**1. Replacement previews forecast the project being cancelled.**

Locations: GovernorPlanningQueries.cs:23, ColonyOrdersPanel.cs:27, EmpireManagementDialog.cs:29. Both replacement checkboxes change the command's ReplaceActive flag, but Preview has no replacement input. The proposed colony retains ProjectId, ProjectKind, binding and origin. The planner retains the old target and its workforce needs. Individual text even says active work retained before a replacement sentence; bulk labels retained-work forecasts replaced. A six-population Wenth Verdant with foundations, Habitat Extension, manual daughter seed and technologies previews Research3/1/2; replacement/empty queue actually selects Discovery Center2/1/3 next preparation. Clear target/binding/origin on an immutable proposed view, retain reserve/queue and invoke the shared planner. Distinguish immediate clearing from next-Ready selection in both controls. Add active population-carrying-vessel regressions; existing UI only checks clearing/reserve.

#### Minor

**2. Pending report summaries present preparation-time conditions as current unresolved conditions.** GovernorReportPanel.cs:22 counts stored issues as unresolved after manual changes. management-manual-override.png has current Attention1 but Planning digest0 unresolved. Label historical conditions or separately derive current count; retain action/override history.

**3. Referenced-template deletion does not identify affected colonies.** DevelopmentCommandProcessor.cs:28 rejects atomically but only says colonies “or” inheritance. At thirty worlds the player must locate dependencies manually; COL-002 asks affected colonies shown. Return deterministic own colony IDs and distinct inheritance blocker; resolve names in UI and assert diagnostic contents.

**4. Attention has no distinct effect for action/completion categories.** GovernorReportPanel.cs:22, TurnReportDialog.cs:106, ColonyList.cs:37 only distinguish Off from other levels for governor_actions/completions. Attention badge consumes issue codes exclusively, so Attention equals Summary for these categories. Give relevant records a visible nonblocking attention treatment and cover Summary→Attention; no crisis machinery.

### Recommendations

Use the authorized regression-first pass for issue1, affected Colony gate/full Suite; explicitly dispose of Minors. Preserve transaction/compatibility boundaries. Expand dense new methods into conventional formatting when next touched; they currently make auditing harder.

### Declined to judge

1. Physical mouse/keyboard/controller/OS focus — tests use in-process controls/signals, no physical session.
2. Comfortable small-screen reading/accessibility/screen-reader/uncoached comprehension — screenshots/rectangles do not establish human outcomes; deferral applies.
3. Subjective portrait aesthetics/owner visual approval — rendering/structural variation does not establish personal acceptance.
4. General balance/enjoyment/optimal strategy/long-campaign pacing — exact economics/bounded scenarios do not establish these.
5. Linux/macOS/ARM runtime equality/cross-machine pixels — Windows-only execution; descriptor vectors/integer rules reviewed.
6. Comprehensive hostile-input/security fuzzing/dependency audit/clean installation/export/release — no fresh delivery/security audit in this read-only review.
7. Revolts/plagues/destructive crises/emergency acknowledgements/culture/fleet/scout/warfare/diplomacy/peaceful mode/multiplayer transport/victory/broader strategy — explicitly deferred beyond this bounded milestone.
8. Combined-main integration/exact post-integration owner preservation — not in reviewed range; requires planned fresh integration audit/gates.

### Assessment

**Ready to merge? With fixes.** Simulation, privacy, persistence and failure atomicity are well structured and backed by retained evidence. Correct the misleading replacement forecast before integration; the remaining findings are smaller presentation gaps requiring explicit disposition.
