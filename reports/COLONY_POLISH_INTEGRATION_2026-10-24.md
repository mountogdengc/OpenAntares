# Colony correction integration — 2026-10-24

All three requested presentation issues are fixed and integrated locally into main. Governor summaries state historical conditions accurately; template deletion identifies owned colonies/inheritance; routine Attention differs visibly from Summary and Off. A review-discovered duplicate vessel notice is corrected while preserving both navigation actions. All twelve COL child criteria and M5-I are Verified for local automated/design scope. Parent REQ-017/totals and human/release limits are unchanged.

## Source and review

Before: `fa99cf81604e878e6bd951a85bb6658b2f7c3faf`. Single fresh read-only gpt-6-astra/xhigh review of fa99cf8..df75799: [raw report](colony-polish-execution/final-review.md). One Important duplicate notice and one Minor evidence sentence, both verified/corrected in one pass at `4a2e4302e070c22ec0f38893f99dc85e1ca12afc`. All-completion-wording regression witnessed RED then GREEN; actual Choose next project and View vessel actions both pass. No re-review, open finding or newly deferred Minor.

Authorized fast-forward head: `fc29d0b56073752e24bd35e99f5739d8100e73a3`. Final documentation/evidence-only handoff commit follows; exact SHA is in main history/chat final audit. Product/harness source remains identical to tested functional source.

## Fresh verification

| Gate | Result | Evidence |
| --- | --- | --- |
| Post-correction branch Suite |988 passed;0 failed/skipped|[summary](colony-polish-review-20261024-run1/summary.json)|
| Branch Colony |665 assertions+4 provenance;33 QA;65 PNG|[results](colony-polish-execution/review-green/results.json)|
| Branch Rival |1146+12 provenance;64 QA;22 PNG|[results](colony-polish-review-20261024-run1/rival/results.json)|
| Branch Species |1194;74 QA;29 PNG|[results](colony-polish-review-20261024-run1/species/results.json)|
| Branch Expansion |275;20 QA;10 PNG|[results](colony-polish-review-20261024-run1/expansion/results.json)|
| Branch Legacy |466+27 geometry;11 QA;16 PNG|[results](colony-polish-review-20261024-run1/legacy/results.json)|
| Fresh integrated main Suite |988 passed;0 failed/skipped|[log](colony-polish-main-20261024-run1/simulation.log)|
| Fresh main Colony |665+4 provenance;33 QA;65 PNG|[summary](colony-polish-main-20261024-run1/summary.json)|
| Ordinary builds/metadata |Rebuild0 warnings/errors;all five harnesses excluded after real opt-in negative control|[runner](colony-polish-main-20261024-run1/colony-runner.log), [metadata](colony-polish-main-20261024-run1/colony/normal-metadata.log)|

All202 branch QA paths exist/142 PNG; fresh main33 QA paths exist/65 PNG. Engine diagnostics0. Branch Colony ran on identical product/harness files immediately before source commit; four other scenarios afterward. Main Suite/Colony ran freshly after integration; other four remain branch evidence. Fresh vessel Attention800 PNG inspected; failed runs/logs/saves/screenshots retained verbatim.

Windows Native .NET8/Godot4.7.2 .NET console. Commands: full `dotnet test tests/OpenAntares.Simulation.Tests/OpenAntares.Simulation.Tests.csproj --no-restore --nologo --disable-build-servers`; existing `Run-Validation.ps1 -Scenario Colony|Rival|Species|Expansion|Legacy` into unique directories; runner finally ordinary `dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild` and `Verify-NormalAssembly.ps1`. `summarize-correction.py` derives counts and checks rows/QA paths/diagnostics/metadata. No clean dependency audit is implied by --no-restore.

## Exact owner preservation

[Fresh pre-integration audit](colony-polish-execution/preservation-before-integration.json): exact four original owner changes, empty index, exact hashes, seven worktrees. Exact four-file backup retained in chat `.colony-polish-preservation-20261024-204739`. Only status was stashed: retained recovery `29db8cd8e96c7cc80303bacd78b66497cfbe9329`. Exact original paragraph restored as bytes, excluded from authored indexed status via clean blob. No broad stash application.

Other original SHA256 values retained:

- docs/SPECIES_ROSTER.md: `843fd758b497094b864b965ad7fff126464bc8fe93187b526bf7d27f0f853034`
- docs/research/faction-survey.md: `814d22e56cacd52885d947e16bc3e4e6123e4a35cf60297b8af58cf3a99463ba`
- untracked docs/FEATURE_REQUESTS.md: `002df223faba5fc118a5a300675449fd7469ed3932a93a4029ee30f2afab9f40`

Final audit checks exactly original four changes, empty index, exact owner paragraph/no other authored status delta, other three hashes, retained branches/stashes/seven worktrees, and correction-branch ancestry. Final SHA/audit: chat `.colony-polish-integration-final.json`, with authored commit in Git history. No owner change is included in an authored commit.

## Rulings and limits

[Ledger](colony-polish-execution/progress.md) retains every ruling and cost: sorted valid fixtures (invalid state unsupported), finish-gate sequencing (documentation order only), retained scratch/worktrees/recovery/QA (disk use). Reviewer declined-item rulings retain unsupported invalid storage, physical device/focus limitations, human reading/accessibility/comprehension, aesthetics/balance, Windows-only runtime, separate fuzz/security/dependency/install/export/release gates, and bounded unchanged/future-system scope. Costs: unsupported invalid inputs and undiscovered human/platform/delivery/other-system defects. Declined main integration/preservation is closed by fresh gates/audit.

No new content/art/dependency/rules/schema/AI/feature scope, push/release/history rewrite/cleanup. Crises/plagues/revolts/culture/destruction, fleet/scout doctrine, warfare/diplomacy, peaceful mode, multiplayer and victory remain future scope. Next: choose/specify a bounded milestone with the owner; preserve all work and evidence.
