# Diplomacy implementation validation — 2026-10-30 plan, executed 2026-10-31

Implementation is on `codex/diplomacy`, isolated from main. Pre-review product source `9ed0cbda82e213ce540f9e5a89adf2cd9222ed02`, corrected product source `873f320b8f5027f4cd21045bfa38b18aa8252747`, uses rules11, writer12 and67 explicit accepted pairs. The one final independent implementation review and its single regression-first correction pass are complete. DIP-AC-001..011 and M11-I are Verified for local automated scope; DIP-AC-012 remains Verified for design only. No merge, push, release or cleanup has occurred.

## Fresh evidence

- Simulation suite: **2140/2140**; genuine compatibility archive replay passes. [Machine summary](diplomacy-execution/final-machine-summary.json) links exact gates, native runs and product hashes.
- All **11 native scenarios** pass with **0 engine diagnostics**. Every native wrapper inspects the actual opt-in assembly as its negative control and finally rebuilds ordinary code with zero warnings/errors, excluding all11 scenarios and the2 existing helpers.
- Four untouched actual Main New Game campaigns: seed42 Small, one Wenth rival, hazards Off, one per human species. The policy uses own views and own hypothetical forecasts; no assets, contacts, trust, research or banks are injected. Ordinary commands earn two founders, shared-system settlement/accord, five trust intervals, both500 setup payments, warmup/payout and later legal notices/founding. Diplomacy offers/answers/notices and Main Save/Load use actual Godot controls.
- Each campaign saves7 phases: pending offer, queued answer, blocked founder, trade earmark, warmup, accord notice and trade notice. Every planning save reloads through actual Main and matches at least3 full uninterrupted future boundaries. Command traces retain concrete action types/payloads. Separate budget-final and continuation-final saves distinguish the accounting turn from extra replay intervals.

## Earned production ledger (internal units)

Each row satisfies earned production = hull payments + own setup + retained bank. Both parties together paid1000, once; trade output is included in earned production. Budget and replay turns are separate.

| Species | Budget / replay through turn | Earned | Two founders | Own setup | Retained bank at budget turn |
| --- | --- | --- | --- | --- | --- |
| harl | 46 / 49 | 64000 | 3200 | 500 | 60300 |
| peldra | 46 / 49 | 33200 | 6400 | 500 | 26300 |
| sulan | 46 / 49 | 65800 | 7200 | 500 | 58100 |
| wenth | 46 / 49 | 72400 | 4800 | 500 | 67100 |

## Criterion map

| Criteria | Evidence |
| --- | --- |
| DIP-AC-001 | Contact/turn/privacy tests; genuine reciprocal observed contact, Solo native navigation. |
| DIP-AC-002 | Duration/AI/view tests; directed reason displays and earned cooperation. |
| DIP-AC-003 | Commands/offers/save/rollback tests; actual stale/Accept/Decline/Revoke/Withdraw/expiry-open controls. |
| DIP-AC-004 | Settlement tests and4 shared-system earned campaigns; actual founding Attention/link. Existing schema forbids transit plus founding intent; arrived/queued resolver supplement is explicitly labelled. |
| DIP-AC-005 | Reservation consumer audit, setup/economy tests,4 exact earned ledgers, native bank/queue/refit displays. |
| DIP-AC-006 | Duration/lost-hub tests and saved native notices/later founding. |
| DIP-AC-007 | Own-view command-only AI tests, stable/cooldown/crossed-offer behavior; genuine rival campaigns. |
| DIP-AC-008 | Strict codec/content tests,67 pairs,10 live-phase3-boundary continuations;24 genuine old rules10 sources/48 generated futures plus1 labelled Ready supplement and inherited archives. |
| DIP-AC-009 | All7 failure seams preserve original final-Ready input; valid2/3-human privacy supplements; query state/ID/RNG immutability. |
| DIP-AC-010 |4 earned species campaigns; all7 actual Main saved stages each. |
| DIP-AC-011 |11 native scenarios, fresh full suite, actual assembly negative control and normal exclusion; one final review and all required corrections complete. |
| DIP-AC-012 | Owner-approved full design in ADR-024; separate from functional acceptance. |

## Preservation and limits

The main checkout is clean and unchanged. Raw checks cover1590 primary files, all protected owner files (including the exact peaceful paragraph) and7456 pre-existing QA files. Original Git identities cover13479 historical report/fixture entries. All10 worktrees, earlier branch tips and stashes remain. The initial audit compared this implementation branch's advancing HEAD as though it must be frozen; corrected audit excludes only that authorized HEAD change and preserves the failed first audit.

Native evidence uses in-process signals, focus/scroll geometry and explicitly embedded supplemental captures. It does not establish physical input, comfortable reading, accessibility, aesthetics, multiplayer networking, broader balance, all platforms/exports, security review or release acceptance. Curated hazards-Off diplomacy progression is distinct from the labelled rare-state supplements. No exhaustive pre-run raw-byte hash of all historical report files was captured; Git identities are the stated preservation oracle for them. Parent17-row statuses/totals are unchanged.

The canonical execution ledger remains in `.superpowers/sdd/2026-10-30-diplomacy/progress.md`; final closure will archive its rulings and the one review without deleting this worktree or scratch workspace.

## Final review and correction disposition

The [one independent review](diplomacy-execution/final-review.md) found no Critical issues, seven Important issues and three initially Minor items. The valid-save crash, multi-contact pending-action discovery and nontechnical financial labels were re-graded Important by their player impact. All10 entered one regression-first correction pass; none remain open, and no second review was dispatched.

New simulation regressions failed10/10 before correction and pass10/10 afterward. Native display assertions failed before correction, then passed across all4 earned campaigns. The final suite passes2140/2140. Exact compatibility replay and final ordinary rebuild pass; the latter has zero warnings/errors and excludes all11 native scenarios and both existing helpers. The native incoming-report link also opens the specific contact after the offer has expired, without resurrecting it.

I1 refreshes only the AI's own private binding after earlier authorized own commands, retaining frozen public decisions and crossed-offer handling. I2 keeps current earmarks/conservative base forecasts and adds a clearly conditional own release alternative; a hidden partner decline cannot change that alternative or the own token. I3 carries only known own trade contribution into calculator-only governor/rival/assembly frames. I4 adds bounded once-only incoming/withdrawal results, owner-specific versus partner-generic failure reasons and contact-specific report actions. I5 provides exact conditional effective/payout dates. I6 makes own Accept/Decline and private hub/payment/output reviewable. I7 starts newly prepared AI offers' supported lifetimes at publication, including lifetime1. M1 uses safe derived calendar arithmetic; M2/M3 provide cultural context/contact summaries/readable production labels.

All11 scenarios passed at9ed0cbd; affected Diplomacy, Rival and Colony were rerun with zero diagnostics on the corrected873f320 product. The eight unaffected records retain their older source attribution in the machine summary. The full2140-test suite, compatibility and final ordinary build cover the corrected source. This is source-scoped evidence, not a claim that all11 native records were rerun after correction.

The final preservation audit confirms main unchanged/clean,1590 primary raw files and all protected owner prose,7456 original QA files,13479 historical index identities, prior refs/stashes and all10 worktrees. [Complete execution ledger](diplomacy-execution/execution-ledger.md) and [all rulings](diplomacy-execution/rulings.md) are retained. The source-scoped evidence assertion has its own missing-summary RED and final GREEN gates. All branch/worktree/QA/recovery/scratch artifacts remain; no integration, push, release or cleanup.
