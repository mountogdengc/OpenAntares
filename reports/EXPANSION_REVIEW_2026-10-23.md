# Independent expansion review and correction pass

Reviewed range: `6ab206d3ca4811cc9163ee6ea3d5c00ff4bb7b6b..db531458ea1049ac6b105065f477478f0d64de07`, read-only independent reviewer, 2026-10-23. No Critical findings. One Important route-selection defect and two originally Minor presentation/save-validation issues. The implementer re-graded the latter two Important because valid embedded content must explain the actual tradeoff and malformed saves must not publish fictitious identities. All three entered one regression-first fix pass; no second review was dispatched.

| Finding | Reproduction and correction | Final evidence |
| --- | --- | --- |
| Queued route overrides replacement destination | Actual DestinationPicker selection after UI reload failed; retain explicit preview selection, use queued destination only as initial fallback, preserve map selection behavior | [RED results](expansion-review-route-red-2026-10-23/results.json); generated Small/Medium/Large A-to-B replacement, preview, save/reload and B-to-A replacement now pass in [final results](expansion-review-green-2026-10-23/results.json) |
| Expansion instructions hard-code prototype population/support | A valid embedded role with cargo 3/minimum 5 failed its blocked instruction assertion; render selected embedded requirements and owned cargo | [RED results](expansion-review-content-red-2026-10-23/results.json); `NondefaultContentInstructions` passes in final results and retains its screenshot |
| Historical observations accept impossible colony IDs | Missing-colony and wrong-planet save cases both incorrectly loaded; require referenced colony existence and planet association without requiring current ownership equality | [RED log](expansion-review-save-red.log), [focused GREEN](expansion-review-save-green.log); `HistoricalObservationRejectsImpossibleColonyReference` and `HistoricalObservationPreservesStaleOwnershipForExistingColony` pass in [full TRX](test-results/expansion-review-final.trx) |

Fresh final normal rebuild: exit 0, zero warnings/errors. Full simulation suite: **281/281**, no failures/skips. Expansion graphical scenario: **275 assertions**, 10 PNGs, 17 unique retained QA saves, exit 0. Legacy scenario: **466 assertions plus 27 popup geometry observations**, 16 PNGs, 11 unique retained QA saves, exit 0. Both final engine logs have no ERROR/WARNING diagnostics. Ordinary assembly excludes the opt-in test classes. Commands/environment and traceability remain in [the main validation report](EXPANSION_VALIDATION_2026-10-22.md).

The reviewer checked legacy fixture provenance/continuation, all-ready validation isolation, population accounting, filtered views, unknown-target rejection, deterministic founding and retained evidence. It did not rerun the full application suites; those fresh runs above are implementer execution after corrections.

## Review boundaries and implementer rulings

- Physical OS input, comprehension, comfortable small-window readability and final balance remain unverified; the owner-deferred participant session remains Deferred. In-process handlers and screenshots cannot establish human delivery/understanding.
- Cross-platform parity, exports, installation and release readiness remain unverified; evidence is local Windows only.
- Default-index repair/integration remain separate; the source/commit comparison is verified using the retained alternate index, but the inaccessible default lock and primary owner edits are preserved.
- Future conquest/destruction semantics remain out of scope. The bounded identity fix does not compare remembered ownership with current ownership; later strategic systems require an explicit compatibility policy.

No findings remain deferred as Minor after effect-based re-grading. All three corrections were reviewed by the implementer and verified through RED-to-GREEN regressions plus a green full suite. This is not a second independent review or approval to merge, push or remove evidence/worktrees.
