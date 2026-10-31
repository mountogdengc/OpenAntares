# Production reservation call-site audit

Task5,2026-10-30. Raw banks stay total; pending earmarks reduce spendable bank only. The shared CalculateReserve adapter restores total Starting/Ending after ordinary Reserve calculates completion/estimate from spendable funds; Change retains output minus actual cost.

| Consumer | Treatment |
| --- | --- |
| EconomyCalculator building; VesselProduction; ShipDesignProduction | Shared CalculateReserve prevents every ordinary completion from spending earmarks. |
| RefitEconomy / RefitResolver | Reserve adapter and Covered/late payment guards subtract earmarks. |
| EmpireViewProjector / OwnedColonyView | Own ReservedProduction and SpendableProduction; raw total preserved. |
| AssemblyPlanner / RefitQueries | Cost/time estimates use own spendable production. |
| GovernorPlanningQueries / RivalPlanningQueries / RivalColonyPlanner | Hypothetical planning banks use owner spendable amounts; no global/foreign oracle. |
| ShipDesignQueries | Refit-ready label uses actual spendable bank, retains total bank for owner display. |
| TurnResolver | Assigns actual total Ending, not spendable subtotal. |
| CrisisQueries / CrisisCommandProcessor | Existing reserve destruction unchanged; reconciliation retires a now-unfunded pending offer/release, rather than shielding reserved production from a crisis. |
| State validation / clone / Saves | Raw total retained; outstanding earmarks must be covered and uniquely authorized. |

Ruling: existing plague emergency actions can destroy reserves/colonies, although the diplomacy design's combat-boundary discussion says the initial milestone has no colony destruction. Preserve established crises. A valid manual crisis command may retire an invalid pending trade commitment immediately, generating owner-private/generic-partner failure and releasing both earmarks without charge. A malformed loaded pending commitment still rejects; historical live hub loss remains valid until boundary termination. This follows expected changed-eligibility failure and avoids a save/Ready deadlock. No crisis immunity or hidden reserve restoration.
