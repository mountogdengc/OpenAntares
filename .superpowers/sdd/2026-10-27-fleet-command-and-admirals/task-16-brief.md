## Task 16 — Activate new campaigns and complete generated/scale acceptance

**Files:** Rules.CurrentVersion; ContentFiles/NewGameScreen adapters; `T/FleetIntegrationTests.cs`, generation tests; Fleet native scenario supplements and reports.

- [ ] Add `FleetIntegrationTests.NewDefaultUsesRulesEightWithoutChangingSetupChoices` expecting rules8/writer9 while Harl/One Peldra/plagueStandard/amebaStandard remain; old loaded rules7 stays7. Run a real generated3-colony/4-scout request via ordinary commands/UI (including legal colony founding), never forced state. Add separately labelled30-colony/100-vessel/20-fleet pre-session scale case with several finite requests and mixed manual/governor controls; scenarios must save/reload and continue.
- [ ] Run `Focused(FleetIntegrationTests)` for meaningful default RED and record generated Fleet native workflow failure if any.
- [ ] Set CurrentVersion8 and requireFleets only for new core loading; initialize new campaigns with approved content/roster, leave other setup choices unchanged. Extend generated all-species workflows to exercise request construction/arrival, manual takeover, culture/loss text and normal exploration. Rare loss thresholds/rank states remain labelled pre-session supplements. At scale find all idle ships, request build, pause one contribution, locate arrivals, inspect career, resume using central controls; test bounds and bounded list/history behavior without asserting an unmeasured performance target.
- [ ] Run integration/generation focused tests, Suite, Build and `Runtime(Fleet,reports/fleet-execution/generated-green-01)`; inspect both-size PNGs and actual save/future provenance. Re-run all genuine rules7/older compatibility tests after default activation. Record exact counts only from outputs, no acceptance inference from an artificial scale fixture.
- [ ] Commit explicit activation/integration/native supplement files/tests/evidence; M8-I remains In Progress until final gate/review disposition.

  Exact staging command for this task (review the displayed staged paths before committing; exclude any unrelated edit):

  ```powershell
  git add -- src/OpenAntares.Simulation/Rules.cs game/ContentFiles.cs game/Screens/NewGameScreen.cs tests/OpenAntares.Simulation.Tests/FleetIntegrationTests.cs tests/OpenAntares.Simulation.Tests/FleetGenerationTests.cs tests/OpenAntares.Game.Validation/FleetGraphicalValidation.cs .superpowers/sdd/2026-10-27-fleet-command-and-admirals reports/fleet-execution
  git diff --cached --name-only
  git commit -m "feat: enable approved fleet campaigns"
  ```
