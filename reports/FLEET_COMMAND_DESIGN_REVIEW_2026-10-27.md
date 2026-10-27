# Fleet Command and admirals design review — 2026-10-27

Documentation-only design entry on isolated `codex/fleet-command-design` from integrated main c554797. [Full draft](../docs/superpowers/specs/2026-10-27-fleet-command-and-admirals-design.md), [ADR-017](../docs/DECISIONS/ADR-017-fleet-command-and-admirals-design-entry.md), [owner direction](../docs/design-notes/2026-10-27-fleet-command-and-admirals.md). M8-D/FLT-AC-012 In Progress, plan/implementation/functional acceptance Not Started. The named officers/bonuses/service/XP and cultural-default direction are owner statements; numeric mechanics, unlocks, recovery/Wenth treatment and research relationship remain proposals for full review.

One fresh read-only builder check returned four questions; [raw report](fleet-design/builder-questions.md) is retained. No per-task agents or second review were commissioned. The drafter resolved each with explicit proposed behavior:

| Question | Disposition in the draft |
| --- | --- |
| Fleet cap can block production/safety | Only named fleets count. Independent ships are legal; emergency separations create independent ships. Requests reserve named-fleet capacity before accepting construction. Safety and ordinary hull completion never require an extra counted fleet. |
| Queue-only authority can leak into template work | Separate temporary finite-explicit-queue permission while original construction is off; no switch/template change or fallthrough. Already-enabled governors keep existing authority. One selector per colony. |
| Missing quota can be mistaken for completion | Every authorized slot must be Arrived/full quota fulfilled. Missing blocks completion/XP until an explicit replacement or reduced-quota amendment. |
| Founding consumes an occupied flagship | Successful founding posts the officer at the new colony, retains XP and Wenth source identity, makes no recovery/loss roll, and Holds surviving fleet or retires an empty one. |

Additional drafter checks make four new-hull requests and one four-hull request grant the same4 assembly XP, distinguish successful recovery without a destination from death, reserve rally-fleet membership, and preserve existing AI's sole order controller. These are written/reference checks, not executed gameplay tests.

Main remains c554797/rules7/writer8 and the four owner files remain byte-exact against this turn's before hashes; primary status/index and all seven worktrees/retained branches/QA/recovery remain. The source worktree moved from the retained scout-polish branch to the new design branch; no worktree was created/deleted. [Document checks/preservation hashes](fleet-design/document-checks.json) record actual results. The1398 simulation and seven native/4491-record main results are historical evidence from the completed M7 merge; no fresh C# build/test/runtime or officer validation is claimed. REQ-017/17-row totals and human/platform/security/export/release limits are unchanged.

Material proposed calls for owner review: finite requests versus ongoing reinforcement; slowest-speed coherent fleets; capped passive specialties and event XP; immediate doctrine editing with research improving equipment; provisional culture defaults; mortal recovery versus the Wenth whole-body link; bounded commissioning and modest AI scope. Full gameplay approval precedes a separate plan; no product/default/writer change or main merge/push/release/cleanup here.


## Owner-confirmed doctrine/research decision — 2026-10-27

The owner selected: “Let’s keep doctrine editable from the start and have research improve recovery equipment.” Doctrine changes therefore need no technology unlock; research improves equipment. This settles only that policy, superseding the earlier open research-gate alternative. It does not approve the full M8 design, cultural defaults, recovery probabilities/delays, ability/XP values, technology cost/prerequisites, AI or rules8/writer9. M8-D/FLT-AC-012 remain In Progress; plan/implementation remain Not Started. No code or primary-main change.

The [scoped decision checks](fleet-design/doctrine-decision-checks.json) verify the current wording, unchanged parent counts, documentation-only changes and exact primary owner-file preservation. No additional builder review or C# build/test/runtime was needed or claimed for recording this explicit choice.


## Owner-confirmed specialties and promotions — 2026-10-27

The owner selected Navigator, Surveyor and Organizer and the10/30 XP promotion thresholds. This supersedes the initial Recovery specialist, ameba-only Surveyor direction and20/60 thresholds. The active proposal now gives Surveyor first-survey research and Organizer a scoped request-ship cost reduction, matching the reviewed role descriptions. Exact5/10 percent bonuses,1/2 research,2/1/8 event XP, proposed30 XP cap and accounting details remain drafter proposals for full review. Recovery probabilities/delays and cultural defaults also remain pending; no recovery-specialty modifier remains. The already-confirmed editable doctrine/recovery-equipment policy is unchanged. This scoped choice does not approve the full M8 design, separate plan or implementation.

[Scoped specialty/threshold checks](fleet-design/specialty-decision-checks.json) cover active wording, reference arithmetic and owner preservation. The original builder report/document checks remain historical; no fresh builder review or C# gameplay validation is claimed for this amendment.
