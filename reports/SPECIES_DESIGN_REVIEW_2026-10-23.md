# Species design draft review - 2026-10-23

The owner authorized a species-mechanics draft and subsequently allowed creative changes when needed. The [candidate specification](../docs/superpowers/specs/2026-10-23-species-mechanics-design.md) is proposed for owner review, not approved gameplay. ADR-007 records that scope. Existing source/content, roster and faction-survey edits remain unchanged.

## Builder check

A fresh independent strategy-game/simulation builder read the candidate and relevant established contracts, considering consequential questions needed before implementation planning. The final check, including refined worker factors and native/sealed capacity, returned **“nothing important left.”** It was read-only and did not implement or approve the design. The author clarified observation refresh after turn increment and save-valid blocked production targets from the existing contracts.

## Author recommendations for owner review

- Keep the four names/bodies, including shark-like Sulan and a single-organism Wenth; treat ordinary population units as productive cohorts, Wenth units as lobes.
- Start Peldra supported at 4/1/1, with expensive support, 3x worker output and slow growth.
- Make Harl rapid recovery/small expeditions useful; do not simulate mortality or archive loss.
- Give Sulan native capacity 12 versus sealed 8, with 24/36-point expeditions and coarse surveyed habitats, replacing a full terraforming requirement.
- Give Wenth 2x worker output, base capacity 6 and full-capacity fission into a three-lobe seed; capacity extension raises the maturity threshold.
- Recommend visible Harl default and new-campaign rules 3/schema 4 while retaining rules-1/2 saves unchanged.

All are proposals. Full owner acceptance/amendment must precede a separate implementation plan; neither the builder result nor arithmetic grants that approval.

## Verification scope

The [arithmetic record](species-design-arithmetic-2026-10-23.json) checks opening outputs, growth-to-capacity traces, rounding order, flat-building behavior, conserved cargo, post-transfer support, habitat capacities and role costs. This is an independent calculation of proposed fixtures, not simulation execution, comparative balance testing or a playtest. No application build/test rerun is claimed for documentation-only work. Integrated gameplay's earlier 281/275/466 passes remain dated evidence for unchanged source.

The documentation/preservation audit checks local links, stable requirement/acceptance IDs, seven ADRs, eight species contract sections, ten proposed acceptance rows, status totals and exact original owner-file hashes. Gameplay/core content changes and an implementation plan are absent. Human acceptance remains Deferred, and review outcomes are pending.

Final audit: 46 Markdown files / 285 local links / 17 requirements / seven ADRs / eight species sections / ten future acceptance rows, zero errors. All 23 design-arithmetic checks pass; source/content/test diff is empty and both owner-file hashes match. Machine record: reports/species-design-documentation-check-2026-10-23.json.
