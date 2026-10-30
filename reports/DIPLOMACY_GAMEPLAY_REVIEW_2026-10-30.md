# Diplomacy design: fresh builder check and disposition

2026-10-30 America/Denver. Scope: one fresh read-only software-engineer builder check of the complete [M11 proposal](../docs/superpowers/specs/2026-10-30-diplomacy-design.md). This is a design check, not an implementation/code review. No product implementation or gameplay tests occurred.

The owner delegated starting choices. The reviewer returned four important planning questions; all are resolved as drafter decisions in the proposal:

| Question | Disposition |
| --- | --- |
| Does activation require another unreserved setup payment after earmarking? | No. Match the original authorization/earmark and require bank >= earmark; exact500 bank/500 reservation works. No double funding. |
| Can a queued answer be changed or revoked? | Revoke applies to Accept and Decline, returns to unanswered, immediately releases only the recipient's earmark, and permits a new answer before original expiry. Direct answer replacement rejects; no-op retries preserve readiness/counters. |
| How can a missing/foreign live trade hub pass input validation to terminate normally? | Activated agreements retain a validated historical hub/activation identity and permit pending link loss. The boundary ends it before output; pending offers/earmarks still require current owned references. Partner receives only generic loss information. |
| When do released earmarks become available to construction? | Command releases are immediate; boundary decline/expiry/failure releases precede ordinary completion. Same-boundary completion can spend them. Forecasts distinguish committed/known releases from conditional partner outcomes. |

No owner-only unresolved choice remains for writing the implementation plan after full design review. These corrections received local document consistency/link checks; no second reviewer or claim of a second fresh pass. Numeric defaults and strategic scope remain drafter recommendations pending owner review. DIP-AC-012/M11-D remain In Progress, separate plan/implementation Not Started. Active rules10/writer11/56 and prior M10 evidence retain their original scope.
