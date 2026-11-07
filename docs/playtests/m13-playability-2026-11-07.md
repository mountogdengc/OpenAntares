**Current packet ready for owner observations after automated work,2026-11-07:** the sixth fix passes the full development8/40/120 audit and four-size report regression, with ordinary build restored. Human acceptance remains Pending. Strict committed-source gates and commit authorization are separate. Earlier blocking/proposal text below is historical; use the current QA index here and preserve all originals. The source evidence is `task10-sixth-final-all-01`, manifestSHA `c9bd68c63fdf655e1ce46f03cdebc38eb54aaf72663afb6fae512b49f45315da`.

Current actual-Main QA names (latest fixed-source audit):

- harl/Standard turn 0: `qa-m13-20261107-154839-0765705-campaign-harl-Standard-t0`
- harl/Standard turn 25: `qa-m13-20261107-154936-4919310-campaign-harl-Standard-t25`
- harl/Standard turn 80: `qa-m13-20261107-155110-3145580-campaign-harl-Standard-t80`

**Blocked,2026-11-07:** do not begin owner acceptance yet. A required Wenth/Solo turn7 report reproducibly triggers a native rendering fault. The five scoped product fixes are used; a single-fix amendment5→6 is awaiting approval. This prepared packet is retained for use after the crash and automated gates are resolved. It is not a human pass or an instruction to bypass the blocker.

# M13 owner-operated playtest packet

Prepared for review; human acceptance remains Pending. Run this after automated work and the ordinary build finish. Native control signals establish functional automation only. No human answer is inferred from silence. This packet unlocks human disposition of PLY-AC-007/011/012, not merge/release authority.

## Prerequisites and build

Close any OpenAntares process from this checkout before building. Keep existing saves/logs untouched. Use a fresh `owner-m13-<date>-<time>` name for every personal save. Work only on QA campaigns or new games. Current isolated root is:

`C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/m13-playability`

Record the source reported by `git rev-parse HEAD` and `git status --short`; at packet preparation the product is uncommitted on baseline60044af. A later authorized source commit must be recorded here/on your report before final recorded acceptance.

```powershell
$sourceRoot='C:/Users/david/OneDrive/Documents/ChatGPT/OpenAnteres/.worktrees/m13-playability'
& 'F:/Projects-PCGames/OpenAntares/tools/Test-Queued.ps1' -SourceRoot $sourceRoot -Suite build
```

Wait for exit0 and empty ordinary validation namespace in the automated handoff. Launch the ordinary game, first at800x600:

```powershell
$godot='F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
$ownerLog=Join-Path $sourceRoot ('reports/playability-execution/owner-m13-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fffffff')+'.log')
& $godot --path $sourceRoot --resolution 800x600 --log-file $ownerLog
```

Repeat with1920x1080 and a fresh `$ownerLog`. Keep ANGLE,60FPS,VSync and original theme defaults. Use physical mouse/keyboard for these observations; do not substitute emitted control events. Preserve the explicit log even on failure.

## Retained actual-Main QA saves

The Load list already contains the following Main-written saves (names without.json). Originals remain under `C:/Users/david/AppData/Roaming/Godot/app_userdata/OpenAntares/saves`. Their byte-identical evidence copies/provenance live in `reports/playability-execution/task08-campaign-green-01-Campaigns/campaigns/harl-Standard`. Load them, then Save As a fresh owner name before advancing. Do not overwrite original QA names.

- harl/Standard turn 0: `qa-m13-20261107-101213-6355950-campaign-harl-Standard-t0`
- harl/Standard turn 25: `qa-m13-20261107-101309-9716377-campaign-harl-Standard-t25`
- harl/Standard turn 80: `qa-m13-20261107-101441-3829943-campaign-harl-Standard-t80`

## Numbered journeys — repeat at both sizes

1. Setup: inspect the four species, Small/Standard default and collapsed Advanced options. Expand Advanced, enter a nondefault seed and toggle hazards, then inspect the visible settings review. Enter an invalid seed and press Start: the editable seed/error must be visible. Correct it and start once. Expected: exactly one campaign, reviewed settings retained and dismissible first-turn guidance. Load a retained QA save: no new-game introduction or stale popup.
2. Galaxy: choose a known system using the map and keyboard list. Select a square owned stationary-vessel marker; use the explicit chooser where several are present. Expected: clear star/ship identity, full names accessible even when labels are suppressed, selected context readable, and no move/found/Ready action from inspection. Foreign/transit markers must not become owned order targets.
3. Vessel: select a destination, inspect route/ETA or unavailable explanation, then open existing order review. Cancel first; issue one legal movement order only when you understand the consequence. Expected: previews/cancel issue nothing; the explicit action issues once and reports actual outcome/rejection. Tab/Shift+Tab,Enter/Space must not cause duplicate orders.
4. Colony: open home colony and inspect identity, species-specific support units, workers, production, research and current project forecast. Adjust one workforce control then its inverse. Browse research and construction choices. Expected: capacity bonuses read2 whole units (not0.02), while production100 reads1 point; summaries agree with detailed values; costs/prerequisites/unlocks/conditional estimates are understandable; existing quick choices take effect according to their visible action. Check a scrolled control after resize: focused field/action remains reachable without double scrolling.
5. Menus: Fleet→Officers→close, Fleet→Designs→editor→cancel→close, available refit/conflict/combat screens and no-contact Diplomacy. Expected: one-level return to invoking menu/action, no lost selection or command from browsing/closing. Game menu retains Save/Load/Latest report only; no unsupported Settings/audio controls.
6. Readiness: read Required/Consider guidance with no research target/project if naturally present. Collapse/expand Consider with mouse and Space/Enter, then resize; its state must persist while response notices remain available. Press End Turn once. Expected: optional inactivity does not block; actual required response rejection remains exact and visible. Dismiss/review do not automatically answer, acknowledge or retry. Empty response notices say “No response notices. End Turn will check readiness.” It is not an all-clear guarantee. If no response rejection naturally occurs, mark this subcase Not observed; do not invent a pass.
7. Report: from a real turn report follow Fleet, then Officers; close twice. Expected: Fleet first, then the original report position/focus. Follow a system/colony link; use visible Return to report. Expected: same report and valid prior position, no extra turn or acknowledgement. Close it; Back/Escape returns to the stated workspace.
8. Save/load: Save As a unique owner name, note turn/species/selected values and reopen through Game→Load. Expected: campaign values/orders remain; popup, selection return stack, edited drafts and first-turn hint are reset. Continue three turns with ordinary legal actions and check no hang, duplicate action or engine diagnostic. Original QA save remains untouched.
9. Readability and understanding: report clipped/unreachable text, unclear disabled actions, fatigue from scrolling and any action whose consequence surprised you. State whether each journey was understandable without assistance. A functional automated pass cannot answer this question.

## Report back

For each step/subcase: source commit and dirty status; engine version; resolution and input method; species/seed/Standard or Solo/checkpoint; action; expected and actual; Pass/Fail/Not observed; critical/major/minor severity; screenshot, explicit log and fresh QA-save paths; whether you understood the consequence. Include the full list of unresolved critical/major findings. No feedback means Pending.

After report-back, reproduce and triage findings within the approved maximum five unique post-audit presentation fixes (five used; any additional unique fix requires revised scope). A sixth fix or engine/content/schema/balance change needs revised scope. Human acceptance requires no unresolved critical/major usability defect in these journeys. Automated source-capture and compatibility gates remain separate.
