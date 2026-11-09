### Task 1: Baseline, harness and the no-override guard

**Files:** M14 harness/targets/runner/verifier, `tools/Check-NoStylingOverrides.ps1`, extend `Verify-NormalAssembly.ps1`. Evidence in a fresh `reports/visual-overhaul-execution/`. No product change.

- [ ] Confirm M13 is integrated; record main HEAD, status, refs/stashes/worktrees and owner-file hashes; create the isolated checkout via `superpowers:using-git-worktrees`. Reconcile M13's final component contracts with this plan; stop for owner review if they differ materially.
- [ ] Write `Check-NoStylingOverrides.ps1`: fails on `AddTheme*Override`, `Color(`/`Colors.` literals used for styling, `new StyleBox*`, `FontFile`/`SystemFont` construction or `CustomMinimumSize` used as styling in `game/**`. Allowlist existing occurrences found at baseline with file:line and owner-visible justification; the allowlist may only shrink. Prove it fails on a planted violation.
- [ ] Capture the baseline screenshot matrix (all current screens x five sizes) and a control inventory (Name, type, visible, rect) per screen. These are the "before" frames and the reachability baseline.
- [ ] Run baseline queued build, full suite, layout checks and M13 acceptance; record exact counts. Exercise opted-in vs restored ordinary DLL metadata as in M13 Task 1.
