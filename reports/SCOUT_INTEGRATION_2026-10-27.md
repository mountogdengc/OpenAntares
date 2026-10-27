# Scout and space-ameba local integration — 2026-10-27

The owner's “merge that” authorizes local integration of the completed scout/space-ameba work and the setup explanation follow-up. Main fast-forwarded from `42e1282b9f38fd8d51d4c99ed0f7481437efcee1` to validated source `a52dfc61c9a16ec10e0091b52783029afac73e38`. The subsequent handoff commit records documentation/evidence only. Main now defaults to rules7, writer8 and29 explicit schema/rules pairs. All approved gameplay decisions remain in force; no new subsystem or gameplay tuning was introduced during integration.

Fresh verification on the primary `F:/Projects-PCGames/OpenAntares` main checkout passes **1398/1398 simulation tests**,0 failed/skipped, and **all seven native scenarios / 4491 records**: Scout 237; Legacy 493; Expansion 275; Species 1194; Rival 1158; Colony 670; Crisis 464. Every runner's opt-in assembly exposes all7 scenarios; its actual normal rebuild has0 warnings/errors and excludes all7 scenarios and both helpers. Both engine and process logs have0 ERROR/WARNING diagnostics. [Machine summary](scout-integration-20261027/main/summary.json), [exact native commands/exits/timestamps](scout-integration-20261027/main/commands.json), [suite log](scout-integration-20261027/main/suite.log) retain actual results, artifact/PNG hashes and retained QA-save references. These seven runs are fresh main results, superseding the prior follow-up report's explicitly historical other-six coverage.

The fresh1920x1080 omitted-habitat setup and generated Warning screenshots, plus800x600 labelled Exposed screenshot, were visually inspected. The setup explanation/Start button and the scrollable encounter controls are present. This is layout evidence only; comfortable reading and physical interaction remain separate human checks.

## Checkout preservation correction

Before merging, an actual Git checkout of425 archived/frozen evidence files reproduced213 raw-SHA mismatches caused by automatic newline conversion. Scoped `.gitattributes` byte-preservation rules and re-staging the verified original bytes fixed the checkout; a second real checkout matches425/425 files with0 mismatches. Expected hashes, old futures and gameplay code were not changed to make the check pass. This correction is included in source `a52dfc6`. [RED](scout-integration-20261027/checkout-red.json), [GREEN](scout-integration-20261027/checkout-green.json), [reproducible audit](scout-integration-20261027/verify_checkout.py), [premerge1398-test gate](scout-integration-20261027/premerge-suite.log).

## Owner edits and recovery

All four original owner edits remain uncommitted. The species roster, faction research and untracked feature-request file retain their original whole-file SHA256 values. The peaceful-mode paragraph in PROJECT_STATUS remains byte-exact; surrounding public status was updated for this integration, and that paragraph was deliberately excluded from the handoff commit. [Preservation record](scout-integration-20261027/preservation.json) records the original hashes without copying owner prose into this report.

Recovery branch `codex/scout-premerge-20261027` retains42e1282; status-only stash `416ec74084983bb640a68f7fe90815585205400c` and private exact backups are retained. The owner paragraph is already restored; do not blindly apply the stash. All seven existing worktrees, scout-polish/scout-survival/design branches, old campaigns, failed/successful QA and review evidence remain. [Merge log](scout-integration-20261027/merge.log). No push, release, cleanup or history rewrite occurred.

## Acceptance and next action

M7-D/M7-P remain Verified for approved design/plan; M7-I and all functional SCT child criteria remain Verified for local automated scope after the earlier independent review/corrections and final M1 follow-up. No additional independent review was commissioned during integration. [Original criterion mapping](SCOUT_VALIDATION_2026-10-25.md), [raw review/disposition](scout-execution/final-review.md), [M1 closure](SCOUT_POLISH_2026-10-26.md) remain historical evidence. Compatibility coverage includes49 genuine frozen rules6 baselines/196 old-engine futures, plus24 generated campaigns/720 Off-Observe parity boundaries. Rare forced fixtures remain labelled.

Parent REQ-017 remains In Progress, with17 parent rows:12 Verified,3 Implemented, Unverified,1 In Progress,1 Deferred. M1 First Playable human acceptance remains Deferred. Native in-process controls and screenshots do not establish physical input/focus, uncoached comprehension, comfortable reading/accessibility, aesthetics, balance/long campaigns, other platforms, security/dependency review, clean install/export or release readiness.

Next action is selection and specification of the next bounded milestone before implementation. Peaceful mode remains the owner's future backlog decision; no new feature work is inferred from this merge.
