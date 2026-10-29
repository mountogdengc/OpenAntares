# Combat local integration — 2026-10-29

The owner selected option1 to merge the completed combat work into main locally. Main fast-forwarded without conflicts from `dc190937ab94a464ba81823d25738164db538e73` to `fa11f96e2e35973d533c0a85630348461bae07a9`. The integration documentation commit follows that source. Recovery branch `codex/combat-premerge-20261029` retains premerge main; `codex/fleet-combat` and all eight worktrees remain.

Main now uses rules9, writer10 and46 explicit pairs. Automatic sentry battles, fleet orders, persistent damage/cohesion, withdrawal and finite own-port repair are integrated. Optional tactical controls remain later scope. Product source is unchanged from `4176996fbda516849a9f5e24691c451e89960019`.

## Fresh main verification

| Gate | Result | Evidence |
| --- | --- | --- |
| Full simulation suite | 1733 passed,0 failed/skipped | [Command/exit](combat-integration-20261029/gates/suite-01.json), [log](combat-integration-20261029/gates/suite-01.log) |
| Native Godot Combat | 3271 checks,93 separate provenance entries,12 genuine generated paid campaigns;0 engine diagnostics | [Results](combat-integration-20261029/combat-01/results.json), [runner command/exit](combat-integration-20261029/gates/combat-01.json), [log](combat-integration-20261029/gates/combat-01.log) |
| Real opt-in negative control | All9 scenario types and2 helpers detected; expected rejection exit1 accepted by runner | [Metadata](combat-integration-20261029/combat-01/opt-in-metadata.json) |
| Ordinary solution rebuild | 0 warnings/errors; all9 scenarios and2 helpers absent | [Metadata](combat-integration-20261029/combat-01/normal-metadata.json), runner log above |
| Generated branch equality | All72 start/pending/active/return/repair/cleared saves across12 campaigns byte-exact; four actual Main reload stages and three future turns per stage pass | [Summary/comparisons](combat-integration-20261029/summary.json) |

Commands ran from `F:/Projects-PCGames/OpenAntares` on Windows x64 using .NET SDK10.0.113/net8.0 and Godot4.7.2 .NET/GL Compatibility. Exact executable paths, elapsed times and exits are retained in the gates. Builds/tests use `--no-restore`; this does not establish a fresh dependency/security audit. Historical Scout latest reports were restored byte-exactly; new timestamped reports were retained under the integration run with SHA256 relocation records.

The other eight graphical scenarios retain their separately scoped [branch evidence](COMBAT_VALIDATION_2026-10-28.md); they were not rerun during integration. The one fresh implementation review and one correction pass close all three Important findings; there are no open findings/deferred Minors and no second review. The original sixteen tasks remain complete. M9-I/functional CBT criteria remain Verified for local automated scope; design/plan approval and human acceptance scopes remain separate.

## Preservation and limits

[Before manifest](combat-integration-20261029/before.json), [QA/branch manifest](combat-integration-20261029/qa-and-branch-before.json), [machine summary](combat-integration-20261029/summary.json) and [closing audit](combat-integration-20261029/closing-preservation.json) record exact preservation. The four protected whole owner files and peaceful-mode paragraph match premerge bytes/text. All pre-existing raw evidence/fixtures and6,667 existing QA saves remain; the historical validation index receives only an explicit new integration entry. The original combat branch and its2,402 raw combat evidence files remain unchanged. All eight registered worktrees, unrelated refs, recovery material and stashes remain. No cleanup or remote operation occurred.

Parent REQ-017 and all17 parent requirement rows/totals remain unchanged. Physical input, player comprehension, comfortable reading/accessibility, aesthetics, balance/long campaigns, cross-platform determinism, dependency/security audits and export/install/release remain unverified. Native signals/handlers and embedded screenshots retain their stated automated limits. No new dependency or product code was introduced during integration.

Next: choose the next bounded milestone before new design or implementation. Nothing was pushed or released.
