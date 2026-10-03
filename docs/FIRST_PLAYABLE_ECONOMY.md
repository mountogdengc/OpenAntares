# First Playable Economy and Turn Specification

**Status: Proposed design for review. No implementation is claimed.**

This specification settles the economy and turn questions raised by the [design guide](DESIGN_GUIDE.md). It defines a small, deterministic colony loop with visible allocation tradeoffs, conserved production and research, and reproducible completion timing. The [charter](CHARTER.md) remains authoritative for scope, and [CLAUDE.md](../CLAUDE.md) supplies implementation constraints.

The design targets one starting colony, workforce allocation, one construction project, one empire research target, turn advancement, and save/load. It does not introduce transport, currency, maintenance, queues, AI, diplomacy, or combat. The formulas and content values below are proposed prototype rules, not findings from the research or final balance.

## Choices and rationale

| Question | Selected proposal | Alternatives and tradeoffs |
| --- | --- | --- |
| Population support | Local recurring output, consumed each turn; no storage. | Stockpiles and interplanetary transfers add logistics decisions but exceed the one-colony loop. |
| Shortages | Pause growth and reduce production and research proportionally. | Population loss creates stronger stakes but adds recovery and defeat behavior. A warning alone would make neglecting support too advantageous. |
| Earned progress | A colony production reserve and an empire research reserve; switching preserves both. | Progress tied to each target encourages commitment but needs more state and cancellation rules. Discarding progress creates avoidable mistakes. |
| Completion | At most one building per colony and one technology per empire per turn. | Chained completions fit queues but complicate timing and feedback. |
| New effects | Economic effects apply to the next turn's snapshot. | Recalculating midway through resolution risks processing-order differences. |
| Planet influence | Planet type sets per-worker output rates. | No planet effect keeps the fixture smaller but makes planet inspection meaningless. Richer modifiers (size, minerals, hazards) need the expansion design. |
| Ending the turn | An empire readiness command; resolve when all human empires are ready. | A session-level end-turn call is simpler but assumes one human player outside the command path. |

Unlimited reserves simplify accounting in this milestone. They permit deliberate saving and applying old research to newly available targets. This is an explicit prototype policy to evaluate, not a hidden exploit or a recommendation for final balance. No output is converted into money or another resource.

## State and units

Population is a whole number of workforce units. Each is assigned to support, production, or research; their nonnegative counts must sum to population. There is no idle workforce assignment in this prototype. Newly grown population is assigned to support and can be reassigned in the following planning period.

All economic quantities use integer hundredths: `100` represents one displayed support, production, or research point. Growth progress uses hundredths of a workforce unit. Population and workforce are distinct from this fractional growth counter.

| Owner | Persisted economy state |
| --- | --- |
| Colony | ID, planet ID, empire ID, population, workforce counts, growth progress, production reserve, optional project ID, completed building IDs. |
| Empire | ID, research reserve, optional research target ID, known technology IDs, controller kind (human in this milestone), ready flag. |
| Game | Turn number, stable entity IDs, PRNG state, rules version, and effective content definitions used by the save. |

Capacity and output bonuses are calculated from content and completed buildings, not independently stored totals. Each building can be built once per colony and each technology researched once per empire. Targets are content IDs; entity relationships use stable IDs. Forecasts, localized text, and view caches are not authoritative state.

Store population, capacity, and assignments as nonnegative 32-bit integers, and economic quantities as nonnegative signed 64-bit integers. Use checked arithmetic and 128-bit intermediates for multiplication before division. Validate that calculated outputs fit 64 bits before applying them. Overflow is an explicit validation error, never a wraparound or silent resource loss. IDs and iteration order are deterministic; resolving this economy consumes no random draws.

## Prototype content

These original working names and values provide an arithmetic fixture. They are editable data and subject to playtesting. No numeric value is imported from a legacy ruleset.

| Rule | Stored value | Display meaning |
| --- | --- | --- |
| Starting population | 6 | Six workforce units. |
| Starting workforce | 2 support, 2 production, 2 research | A balanced initial assignment. |
| Starting capacity | 10 | Ten workforce units. |
| Starting reserves and growth progress | 0 | No accumulated progress. |
| Required support per population | 100 | One support point per turn. |
| Base growth when fully supported | 10 | One tenth of a workforce unit per turn. |
| Maximum surplus growth bonus | 40 | Four tenths of a workforce unit per turn. |

Per-worker output comes from the colony's planet type, so planet inspection shows a world's real economic potential. The worked turns use the home planet type.

| Planet type ID | Working name | Support / production / research per assigned worker | Colonizable in fixture |
| --- | --- | --- | --- |
| `verdant_world` | Verdant World | 300 / 200 / 200 | Home colony. |
| `forge_world` | Forge World | 200 / 300 / 200 | Inspection only. |
| `crystal_world` | Crystal World | 200 / 200 / 300 | Inspection only. |
| `barren_rock` | Barren Rock | 100 / 200 / 100 | Inspection only. |

Other planet types exist for inspection and comparison; settling them belongs to later expansion work. A type with any per-worker rate of zero is valid content, but the starting colony's support rate must be positive.

| Building ID | Working name | Cost | Prerequisite | Effect |
| --- | --- | --- | --- | --- |
| `fabrication_hall` | Fabrication Hall | 1200 | None | Adds 100 production each turn. |
| `cultivation_hub` | Cultivation Hub | 1000 | `cultivation_methods` | Adds 200 support each turn. |
| `analysis_lab` | Analysis Lab | 1200 | `measurement_methods` | Adds 100 research each turn. |
| `habitat_extension` | Habitat Extension | 1600 | `habitat_methods` | Adds two population capacity. |

| Technology ID | Working name | Research cost | Prerequisite | Enables |
| --- | --- | --- | --- | --- |
| `cultivation_methods` | Cultivation Methods | 1200 | None | Cultivation Hub. |
| `measurement_methods` | Measurement Methods | 1200 | None | Analysis Lab. |
| `habitat_methods` | Habitat Methods | 1600 | None | Habitat Extension. |

No building requires upkeep in this fixture. Consequently the UI shows zero operating costs, not a fictional deduction. Building effects are generic flat output or capacity modifiers. Technologies unlock choices; they do not independently add the building bonus.

## Output and support formulas

For a turn snapshot, let `N` be population and `Ws`, `Wp`, and `Wr` the assigned counts. `Rs`, `Rp`, and `Rr` are the per-worker rates of the colony's planet type. `Bs`, `Bp`, and `Br` are the sum of completed-building support, production, and research bonuses. All outputs below are stored hundredths.

```text
supportProduced = Ws * Rs + Bs
supportRequired = N * 100
supportSurplus = max(0, supportProduced - supportRequired)
supportDeficit = max(0, supportRequired - supportProduced)
grossProduction = Wp * Rp + Bp
grossResearch = Wr * Rr + Br
```

For a colony with population greater than zero:

```text
supportSatisfied = min(supportProduced, supportRequired)
netProduction = floor(grossProduction * supportSatisfied / supportRequired)
netResearch = floor(grossResearch * supportSatisfied / supportRequired)
```

The shortage penalty applies to workforce and building output together. Production and research floor separately to hundredths. Report the difference between gross and net as the shortage deduction, including rounding. Support above requirement is used for growth only and expires after this turn; it never becomes a stockpile.

The valid prototype colony always has at least one population unit. This shortage rule never reduces population. General handling of an empty or destroyed colony belongs to a later system; a zero-population save is invalid under this rules version.

## Growth and capacity

If support has a deficit, growth earned this turn is zero and existing growth progress is retained. Otherwise:

```text
growthEarned = 10 + min(40, floor(supportSurplus / 10))
```

`growthEarned` is hundredths of a workforce unit, not economic points. For example, 300 stored surplus earns 30 bonus growth, giving 40 growth with the base contribution. Extra support beyond the bonus cap is visibly unused. At the fixture values, two surplus support workers already exceed the cap, so this state is common: the forecast must show the unused amount next to the workforce controls, not only in a detailed breakdown.

If the snapshot population is at capacity, earn no growth and keep growth progress at zero. Otherwise add earned growth to existing progress. Convert each complete 100 growth into a workforce unit, up to the snapshot capacity, and assign it to support. Retain the sub-100 remainder unless capacity is reached; on reaching capacity, discard remaining growth and report the capacity limit. Capacity therefore prevents saving births for a later upgrade.

A capacity building completed this turn does not change the snapshot capacity used for growth. Its extra room is available when the next turn begins. Newly grown workers produce nothing and require no extra support in the turn they are born; the following snapshot includes both their output and support requirement.

## Production reserve and projects

Each turn, add net production to the colony reserve even if no project is selected. If the selected project is legal and the resulting reserve meets its cost, subtract that cost, add its completed building ID, and clear the project. Keep all remaining reserve. Otherwise keep the project and reserve unchanged apart from the earned contribution.

Selecting, replacing, or clearing a project spends nothing. A reserve large enough to cover the cost still completes only at turn resolution. This permits a forecast and avoids completion as a side effect of a UI selection. No automatic next project is selected.

Project progress displayed against its cost is `min(reserve, cost)`; any additional amount is separately labeled as the expected reserve after completion. The reserve is generic industrial preparation that can be redirected between buildings. There is no additional per-project progress field.

For an unchanged project and current forecast output `q`, estimated turns are one if the reserve already covers the cost; stalled if `q` is zero and the cost is not covered; otherwise `ceil((cost - reserve) / q)`. Label this estimate as assuming current output continues, because population or future choices can change it.

## Research reserve and targets

Sum net research from colonies owned by each empire in stable colony-ID order and add it once to that empire's reserve. The first playable has one colony, but the calculation must not assume a global human player or mix empire ownership.

If the selected technology is legal and the reserve covers its cost, subtract the cost, add the technology to the known set, and clear the target. Keep any remainder. Research continues accumulating when no target is selected. Selection and switching spend nothing; completion occurs only at turn resolution. Do not auto-select a new target.

Research progress and estimates use the same reserve and timing convention as construction. A technology unlocked this turn can be used to select its building in the next planning period. Construction cannot start or complete that building during the turn that unlocked it.

## Commands and errors

The simulation accepts these intents, tagged with issuing empire and expected turn:

- Set workforce counts for an owned colony.
- Set or clear an owned colony's project.
- Set or clear the issuing empire's research target.
- Mark the issuing empire ready, or withdraw readiness, for the current turn.

Readiness is an ordinary command, not a session shortcut. The turn resolves once every human-controlled empire is ready; in the first playable that is the single starting empire, so marking ready resolves the turn immediately. Accepting a planning command from an empire clears that empire's readiness. Readiness is planning state and is saved. This keeps the command path free of a global player without defining multiplayer submission rules, timeouts, or AI turn order.

Validate ownership, IDs, expected turn, nonnegative counts, workforce sum, prerequisites, and not-already-completed targets before mutation. A repeat command setting the same valid choice is a no-op. Rejected commands return a structured reason and leave all state unchanged. Unknown projects, negative reserves, duplicate completed IDs, invalid content references, and stale turn commands are errors.

Content costs must be positive; rates and bonuses must be nonnegative and fit the stored types; starting capacity must cover starting population. Capacity additions must fit 32 bits, and economic bonuses must fit 64 bits. At load time, compute an upper bound for each output: maximum reachable capacity times the highest per-worker rate, plus the sum of every building's bonus of that kind. Reject content whose bound cannot fit the stored types. This bound grows linearly with content, unlike enumerating building combinations, so it remains practical when mods add buildings. Reserve accumulation is checked each turn. Validate prerequisite references and reject cycles. Only the modifier kinds specified here are supported; unsupported modifiers fail content loading.

Shortages are warnings, not invalid commands: they reduce output, so explain the deduction before the empire marks ready. A colony with no project or an empire with no research target loses nothing under the unlimited reserve policy, so it is shown as a banking status rather than a warning. A target cleared by completion is reported once in the turn report. If a later rule makes waiting costly, such as a reserve cap, the status becomes a warning at that point. Planning commands update accepted orders immediately, so saving during planning retains the player's latest choices.

## Resolution contract

Resolve from a validated immutable snapshot in these phases:

1. Capture population, workforce, completed effects, selected targets, reserves, and known technologies.
2. Calculate support, net outputs, and growth inputs for every colony in stable ID order.
3. Resolve growth using snapshot capacity and assign new workers to support.
4. Add production and empire research contributions; resolve at most one project per colony and one technology per empire. Eligibility uses snapshot prerequisites.
5. Validate the candidate state, increase the turn number once, and publish the candidate with a structured report.

If any phase fails, retain the preceding state and turn number. Do not publish a partially advanced turn. The initial planning period is turn 0; its first resolution produces turn 1. Reports identify this transition and the entities affected.

Forecast and resolution use the same pure economic calculation. A forecast returns net outputs, growth, expected population and assignments, completions, reserve remainders, and limiting factors without modifying state or consuming randomness. Presentation supplies labels and formatting.

### Breakdown lines

Every reported quantity (support produced and required, net production, net research, growth earned, and each reserve change) carries an ordered list of breakdown lines produced by the same calculation. Each line has:

| Field | Meaning |
| --- | --- |
| Kind | A stable string such as `workers`, `building`, `shortage`, `growth_base`, `growth_surplus`, `cap`, `cost_paid`, or `rounding`. |
| Source ID | The planet type, building, or technology ID responsible, or empty for rule-level lines. |
| Count | Optional multiplier, such as the number of assigned workers. |
| Amount | Signed stored integer contribution, in the quantity's units. |

Lines sum exactly to the reported total. Caps and floors appear as their own negative lines, so unused support above the growth cap and amounts lost to flooring are visible rather than implied. Lines are ordered by phase and then by source ID. Presentation maps kinds and IDs to localized text; it never recomputes amounts.

For the shortage example below, net production is reported as `workers` (3 × 200 = 600) followed by `shortage` (−86), totaling 514. The `shortage` line includes its rounding; a separate `rounding` line appears only where a rule floors a value that has no other deduction.

## Worked turns

All values in the following tables are stored integers. A reserve of 400 means four displayed points.

### Balanced development

Start at turn 0 with population 6, capacity 10, assignment 2/2/2, zero progress, Fabrication Hall selected, and Cultivation Methods selected. Support produced and required are both 600. Net production and research are each 400; growth earned is 10.

| State after resolution | Population | Growth progress | Production reserve | Research reserve | Completion |
| --- | --- | --- | --- | --- | --- |
| Turn 1 | 6 | 10 | 400 | 400 | None. |
| Turn 2 | 6 | 20 | 800 | 800 | None. |
| Turn 3 | 6 | 30 | 0 | 0 | Fabrication Hall and Cultivation Methods; both targets cleared. |
| Turn 4, no new targets | 6 | 40 | 500 | 400 | None; reserves accumulate. |

The hall's bonus contributes first during turn 3 to 4, not during the turn that completes it. Cultivation Hub is selectable after turn 3 but contributes no support merely because its prerequisite was researched.

### Allocation tradeoff

Each row resolves one turn from population 6, growth progress zero, reserves zero, and no buildings. Targets cost more than these outputs, so nothing completes.

| Support / production / research workers | Support produced / required | Net production | Net research | Growth progress after turn |
| --- | --- | --- | --- | --- |
| 2 / 2 / 2 | 600 / 600 | 400 | 400 | 10 |
| 3 / 1 / 2 | 900 / 600 | 200 | 400 | 40 |
| 2 / 3 / 1 | 600 / 600 | 600 | 200 | 10 |
| 1 / 3 / 2 | 300 / 600 | 300 | 200 | 0 |
| 0 / 3 / 3 | 0 / 600 | 0 | 0 | 0 |

At these values, under-supporting is unattractive; the meaningful early choice is faster growth versus immediate construction or research. This is intentional for an introductory prototype. Playtesting must check whether the choice remains interesting once buildings free workforce from support.

### Overflow and selection timing

Use the balanced assignment with a production reserve of 1000 and Fabrication Hall selected. Add 400, pay 1200, and retain 200; no hall bonus applies yet. With research reserve 1000 and Cultivation Methods selected, add 400, pay 1200, and retain 200.

Replacing the hall with another eligible project before resolution leaves the production reserve at 1000. Clearing it retains the entire 1400 after resolution. Selecting an affordable target never completes it until the next turn resolution.

### Birth and capacity

Start with population 6, assignment 3/1/2, capacity 10, and growth progress 80. The snapshot earns 40 growth, 200 production, and 400 research. The next state has population 7, growth progress 20, and assignment 4/1/2. Outputs for that resolved turn still use the old three support workers.

For a capacity example, start at population 9, capacity 10, assignment 4/3/2, growth progress 80, and no buildings. Support surplus is 300, so growth adds 40. Population becomes 10, assignment becomes 5/3/2, and growth progress resets to zero because capacity was reached. Production 600 and research 400 are based on the snapshot assignments.

If Habitat Extension completes while population is already 10 at capacity 10, no growth is earned that turn. Capacity becomes 12 for the next planning period; growth begins again on its next resolution.

### Shortage and rounding

At population 7, assignment 2/3/2, and no buildings, support satisfies 600 of 700. Net production is `floor(600 * 600 / 700) = 514`; net research is `floor(400 * 600 / 700) = 342`. Growth earned is zero. Existing growth progress, such as 20, remains 20. Report deductions of 86 production and 58 research hundredths.

The UI may display 5.14 production and 3.42 research. Those are formatting choices for exact stored integers, not floating-point simulation values.

## Save continuation and reports

Save complete planning state, including reserves, growth fractions, target IDs, completed content, deterministic ID allocation state, and PRNG state. Embed the effective content definitions and rule parameters used by this prototype. Version the rules implementation separately: embedded values cannot reproduce a formula that has changed in code.

Load only supported schema and rules versions, validate the embedded content and entity relationships, and rebuild derived views. World invariants include: population at least one and at most capacity; workforce counts summing to population; growth progress between 0 and 99, and exactly 0 when population equals capacity; nonnegative reserves; each colony's planet type defined in content; and no target already completed or known. Unsupported versions receive a clear compatibility error; migration is not implicit. A failed load preserves the current session. Writes must not replace a valid save with a partial file.

Reports include output breakdowns, shortage deductions, growth and capacity limits, new workforce assignments, completions, and reserve accounting. Warn when a cleared target needs another choice, but avoid repeating a modal warning every turn for intentional banking. The interface should expose gross output, deduction, net contribution, starting reserve, cost paid, and ending reserve from these same results.

## Verification and playtest acceptance

The implementation must reproduce every worked example. Also verify:

- Rejected commands and failed turn resolution leave state unchanged.
- Switching or clearing targets conserves reserves; known technologies and completed buildings cannot be selected again.
- No selection command completes a target; completion clears it and happens only once per owner per turn.
- Shortages preserve growth fractions and use the stated independent flooring for outputs.
- Births, capacity extensions, and output bonuses follow snapshot timing.
- Production and research satisfy `ending reserve = starting reserve + earned output - cost paid`.
- Repeating state, orders, rules, and seed gives identical authoritative state and report values, including across save/load continuation.
- Forecast values equal resolved values when planning state is unchanged.
- Empire aggregation never credits another empire's research, even in a synthetic multi-empire fixture.
- Breakdown lines sum to every reported total, in forecasts and resolved reports alike.
- A colony on a non-home planet type in a synthetic fixture uses that type's per-worker rates.
- A planning command clears readiness, and the turn does not resolve until every human empire is ready.

For a first playtest, verify that the player can explain the growth-versus-output tradeoff, redirect progress without confusion, recognize when a building starts helping, and continue after reload. Observe whether generic banking, flat bonuses, or eventual access to all four buildings turns play into a routine sequence. Adjust fixture values or reconsider reserve policy using those observations; do not add advanced systems to obscure the result.

## Boundaries and next handoff

This specification is ready for review as a concrete proposal. Approval should settle the shortage policy, generic reserves, snapshot timing, planet rates, readiness, breakdown lines, and prototype content together before an implementation plan is written.

Galaxy generation, home-world selection, and initial empire visibility still need their own specification. They must supply a valid starting colony on a `verdant_world` using the fixture above, place the other planet types for inspection, and use empire-specific inspection queries. The economy component can be tested headlessly before those presentation and generation systems are connected.
