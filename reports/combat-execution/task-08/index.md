# Task 08 — finite return-and-repair

Repair captures only already-stationary authorized ships before resolution. Arrival stores Repair/Hold without healing; each eligible full interval restores ceil(max hull /4) and20 cohesion, capped independently. Both full ends finite authority in Hold. A persisted last-tick guard and phase guard prevent duplicate healing. Lost destinations clear authority without retargeting or changing committed transit.

Ordinary movement and explicit fleet orders override selected return/repair authority, including M8 Hold that would otherwise be a no-op. Unrelated commands preserve it. Cargo quarantine/inspection and existing finite request authority remain authoritative. Repair preserves health, cargo, production, population and officer records, with no XP or request resend/reopening.

Persistent per-ship hull-loss/repair totals and empire repair totals retain actual capped values. Alternative max101 yields26 hull per full tick.30 independent returns complete the same clock; completed construction provenance/credits remain unchanged even with its new-hull marker retained.

Behavioral RED/GREEN covers no arrival tick,46/30 ->71/50 ->96/70 ->100/90 ->100/100 at16, Hold/cancel, snapshot-only completion eligibility, duplicate tick, medical-port race, cargo infection retention and destination loss. Focused repair/request/transport44/44; full Suite1619/1619, zero compiler warnings. All raw retries/gates retained. Full turn wiring remains Task9; strict new fields belong in Task12 codec.
