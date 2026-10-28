## Task 17 — Combined regression, one final independent review and handoff

**Files:** final reports/manifests, progress ledger, PROJECT_STATUS/AGENTS/specification/acceptance/implementation tracking only; scoped regression fixes if the single review finds defects.

- [ ] Run Suite/Build and all8 native scenarios sequentially: Legacy, Expansion, Species, Rival, Colony, Crisis, Scout, Fleet, each a fresh `reports/fleet-execution/final/<scenario-attempt>` directory. Preserve per-run results/commands/engine and process logs, QA saves, screenshots/raw hashes, source commit and loaded schema/rules. Normal-rebuild/exclusion checks must run from actual assemblies, not source grep. Audit diagnostics from both log sources even if assertions pass. Record source changes before deciding which affected gates need a new run.
- [ ] Run Normal; confirm0 warnings/errors and actual exclusion of8 scenario types plus all helpers. Reproduce raw archive SHA256 after a real index checkout and replay rules1..7 futures. Verify main c554797/four owner SHA256/status/empty index, seven worktrees and retained branches/QA/recovery. A failed preservation/check is unresolved work, not a documentation warning to waive.
- [ ] Complete FLT-AC-001..011 evidence map against exact approved design, using generated/rare/scale provenance separately. Request **one fresh whole-branch implementation review** through the requesting-code-review workflow, authorized by retained Native method, only after combined validation. Give reviewer approved spec/plan/full base-to-head diff and evidence; no per-task reviewers or automatic second review. Save actual findings/dispositions verbatim. For each substantive finding, reproduce a behavioral regression RED, make minimum correction, run focused+Suite/affected native gates and relevant late rollback/compatibility checks. No code change is needed if no actionable finding exists.
- [ ] Self-check corrected branch against every finding/criterion. Record exact final source/commands/counts, historical versus fresh evidence, unresolved/deferred items and human/platform/security/export limits in `reports/FLEET_COMMAND_VALIDATION_2026-10-27.md`, `reports/fleet-execution/final-summary.json`, `reports/fleet-execution/final-review.md`, `reports/VALIDATION_REPORT.md` and ledger. No automatic milestone closure if a functional criterion/finding remains unresolved. Parent REQ-017/counts remain unchanged. Commit only explicit corrected files and named documentation/evidence paths after verification.
- [ ] Hand off completed isolated branch with what changed, tests and remaining limits. Update PROJECT_STATUS with actual M8-D/P/I/FLT statuses and one next action; keep main/owner edits intact. Stop for owner disposition before integration; no merge/push/release/cleanup inferred. Do not request another design/Native approval or repeat a completed task.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- PROJECT_STATUS.md AGENTS.md docs/SPECIFICATIONS.md docs/ACCEPTANCE_CRITERIA.md docs/IMPLEMENTATION_PLAN.md reports/VALIDATION_REPORT.md reports/FLEET_COMMAND_VALIDATION_2026-10-27.md .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "docs: record fleet validation and final review"
  ```

## Frozen implementation details

### Recovery stream v1 vectors

Use existing `Pcg32.FromSeed(galaxySeed ^ 0x4F46464943455201UL, (ulong)owner.Value ^ 0x5245434F56455201UL)`. These distinct ASCII-derived domains identify officer/recovery version1 and do not consume another stream. Existing PCG reference seeding and unchecked64-bit wrap are unchanged. Independent integer arithmetic generated the following planning vectors; they are **not a product test run**. Task2 must compare these to actual Pcg32, including seeded State/Increment and4 successive NextUInt32 values (then independently NextInt(100) modulo-rejection behavior).

| Galaxy seed | Owner | Initial State | Increment | NextUInt32 sequence |
| --- | --- | --- | --- | --- |
|0|1|1783446105968468059|11856436985063973889|50353123,3417059036,1585724331,4047853392|
|42|1|10822750475600051645|11856436985063973889|1038021008,2897589185,1792309901,4126569821|
|42|2|12114079671261706449|11856436985063973895|4047523869,1149529208,2343501704,871191071|
|18446744073709551615|1|8606524431442022100|11856436985063973889|3245324364,1208278847,3848589321,447352628|

Only actual eligible mortal RecoverCommand flagship losses draw, sorted EmpireId/OfficerId. Stream is present under rules8 even amebaOff; no losses means no draws. Store algorithm version1/state/odd increment/draw count. Draw49/50 and69/70 are labelled threshold supplements, not claims these four sequences hit every threshold. Preview/reload/Wenth/LastWatch never draws.

### Initial original profiles and content placement

Use `fleet_command.json` for ordinary values, profiles and culture default keys, and declare species `command_embodiment` as `mortal` or `colony_body` in rules8 embedded content. Culture reference keys explicitly map existing culture names; no code branch on species ID/name. No changes to the owner's roster or biological prose.

| Existing culture / embodiment | Navigator profile | Surveyor profile | Organizer profile |
| --- | --- | --- | --- |
| Estates / mortal | `estates_navigator`: Vara Eshen, they/them; deliberate route planner | `estates_surveyor`: Oren Tal, he/him; patient observer | `estates_organizer`: Mira Vell, she/her; careful coordinator |
| League of Schools / mortal | `schools_navigator`: Tavi Sen, they/them; methodical pilot | `schools_surveyor`: Relo Iven, he/him; curious recorder | `schools_organizer`: Sera Venn, she/her; practical scheduler |
| Harbor Houses / mortal | `harbors_navigator`: Neri Vale, she/her; cautious pilot | `harbors_surveyor`: Taren Oss, they/them; attentive chart maker | `harbors_organizer`: Olan Riss, he/him; steady coordinator |
| Seed Kinship / colony_body | `kinship_navigator`: source-colony body's displayed name | `kinship_surveyor`: source-colony body's displayed name | `kinship_organizer`: source-colony body's displayed name |

These are original presentation text within builder latitude, not hidden modifiers or new biology. Mortal per-specialty name catalog starts with the row's authored name; deterministic decimal suffix2,3,… after reuse, driven by stored checked per-profile commission ordinal (no suffix for first). Candidates have fixed specialty/profile, no RNG/rerolls; after commissioning a candidate, next same-specialty catalog entry is offered only within active/lifetime bounds. No duplicate career reuse. Wenth name is `<source body's colony name> — Command appointment <local officer ID>`; no limb persona or independent recruit. Default doctrine all RecoverCommand, Wenth label Re-establish command link. Personality text never changes authorization. Alternative content supplies its own validated catalog/default/embodiment.

### Plan coverage and handoff

| Approved criterion | Owning tasks |
| --- | --- |
| FLT-AC-001 window/direct authority |3/4/7/15/16|
| FLT-AC-002 fleet/cohort/founding |3/4/9/10/12/13|
| FLT-AC-003 finite requests/safety/priority |6/7/8/9/10/13/15/16|
| FLT-AC-004 cargo/manual/governor authority |5/6/7/8/9/10|
| FLT-AC-005 standing orders/knowledge/consent |3/4/9/10/11/15|
| FLT-AC-006 officers/identity/availability |2/3/5/10/12/13/15|
| FLT-AC-007 XP/bonuses/dedup |4/8/10/11/14|
| FLT-AC-008 culture/loss/equipment |2/5/10/12/13/15|
| FLT-AC-009 transactions/readiness |4/5/7/9/10/11/12/13|
| FLT-AC-010 saves/legacy/RNG |1/2/3/13/14/17|
| FLT-AC-011 integration/scale/regressions |14/15/16/17|
| FLT-AC-012 full design approval |ADR-018; already Verified for design, no runtime claim|

Self-review evidence is in [implementation-plan review](../../../reports/FLEET_COMMAND_IMPLEMENTATION_PLAN_REVIEW_2026-10-27.md). This document is the concrete separate plan for owner review. Its task boxes remain unchecked; no implementation, baseline-future capture, new gameplay tests or native fleet validation ran while writing it.
