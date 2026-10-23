# ADR-003: JSON content and explicit save compatibility

- **Status:** Established implemented contract, recovered 2026-10-22.
- **Sources:** [FIRST_PLAYABLE_ECONOMY.md](../FIRST_PLAYABLE_ECONOMY.md), [spiral specification](../superpowers/specs/2026-10-05-spiral-galaxy-design.md), `ContentLoader`, `SaveGame`.
- **Requirements:** REQ-002, REQ-003, REQ-009, REQ-012.

## Context and choice

Define ordinary content in validated JSON referenced by existing string IDs; entities use stable integer IDs. Saves embed effective content, full planning/PRNG/ID state, schema version, and separate rules version. Validate before publication; flush a temporary file before replacing a save.

Write schema 2 and explicitly read schema 1. Retain old coordinates/planets and null galaxy metadata/regions rather than regenerate or infer them. Resave legacy layout in schema 2. Reject unsupported schema/rules versions; no implicit migration.

## Alternatives and consequences

The spiral spec explicitly rejects regenerating legacy galaxies or inferring regions from coordinates. Embedding data avoids depending on later edits to core files, but cannot embed changed C# formulas; hence separate rules compatibility checks. No broader historical storage-format comparison was found.

Local tests cover round trips, continuation, legacy layout, corrupt inputs, and failed writes/loads. The legacy fixture's in-game dialog path is still unverified. Package namespacing, versioned external extensions, and arbitrary code sandboxing from the OpenJRPG example were not adopted.


**Implementation follow-up, 2026-10-23:** The owner approved the concrete Native plan; the isolated feature branch implements ADR-006's schema-3/rules-1/2 paths and original schema-1/2 readers. Both genuine old-save dialog paths and continuation now have executed graphical evidence. Earlier adoption-time verification limits above remain historical; consult the expansion report and PROJECT_STATUS for current review/integration state.
