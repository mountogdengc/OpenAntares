# ADR-002: Deterministic commands and prototype economy

- **Status:** Established implemented contract, recovered 2026-10-22; balance and usability remain subject to playtest.
- **Sources:** [CLAUDE.md](../../CLAUDE.md), [FIRST_PLAYABLE_ECONOMY.md](../FIRST_PLAYABLE_ECONOMY.md).
- **Requirements:** REQ-004 through REQ-008, REQ-012, REQ-014.

## Context and choice

Use empire-issued, turn-tagged commands for every gameplay change; clone/validate/publish and retain the old state on rejection or resolution failure. All-human readiness triggers explicit snapshot phases. Integer hundredths, checked arithmetic, PCG32, stable IDs/order, and invariant parsing support deterministic results.

For First Playable, use local recurring support, shortage penalties without population loss, generic unlimited production/research reserves, lossless switching, one completion per owner/turn, and next-snapshot effects. These are prototype policies, not final balance findings.

## Alternatives and consequences

The economy spec explicitly compares support storage/transfers, population loss, target-bound/discarded progress, chained completion/queues, mid-resolution recalculation, and a single-player end-turn shortcut. Chosen policies keep the one-colony loop small and explainable while preserving future command boundaries.

Banking can make waiting attractive and eventual choices routine; evaluate this through playtest before changing policy. Multiple humans are represented/tested synthetically; no remote transport or playable AI is claimed. Local deterministic tests do not prove all-CPU portability.
