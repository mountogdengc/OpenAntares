# OpenAntares development workflow

Adapted 2026-10-22 from the owner's **OpenJRPG Project Workflow and Master Prompts** PDF, consolidated 2026-10-22: Workflow v1.0, Documentation Templates v1.0, Initialization v1.1, Autonomous Implementation v1.1, Status Template v1.0, Existing Project Adoption v1.0. The attached PDF is the process reference; OpenAntares's existing repository documents supply product decisions.

## Process and artifacts

| Phase | Purpose | Project record |
| --- | --- | --- |
| Project definition | Identity, users, scope, success | [PROJECT_CONCEPT.md](PROJECT_CONCEPT.md), authoritative [CHARTER.md](CHARTER.md) |
| Research/architecture | Preserve stack, boundaries, data and decisions | [ARCHITECTURE.md](ARCHITECTURE.md), [DECISIONS/](DECISIONS/README.md), existing research and specs |
| Functional specification | Precise behavior and failures with stable IDs | [SPECIFICATIONS.md](SPECIFICATIONS.md), existing subsystem specifications |
| Preparation | Constraints, dependency order, acceptance, Git inventory | [AGENTS.md](../AGENTS.md), [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md), [ACCEPTANCE_CRITERIA.md](ACCEPTANCE_CRITERIA.md) |
| Implementation | Bounded approved work, tested units | [MASTER_IMPLEMENTATION_PROMPT.md](MASTER_IMPLEMENTATION_PROMPT.md); currently gated |
| Validation/acceptance | Execute criteria, separate manual evidence | [VALIDATION_REPORT.md](../reports/VALIDATION_REPORT.md), [playtest record](playtests/first-playable-2026-10-19.md) |
| Integration/release | Inspect/review intended changes and merged result, identify baseline | Git state plus validation/status; no automatic cleanup/merge |
| Maintenance | Correct stale claims, track defects/deferred scope | [PROJECT_STATUS.md](../PROJECT_STATUS.md) and requirement statuses |

For this existing project, use [Project Adoption and Reconciliation](MASTER_PROJECT_ADOPTION_PROMPT.md) before implementation. Do not run new-project initialization or recreate foundations that already work. Reusable initialization/template content remains in the supplied PDF; these project documents contain the filled-in result rather than empty duplicate templates.

## Authority and evidence

Explicit owner choices govern; [CHARTER.md](CHARTER.md) remains the source of truth for scope and [CLAUDE.md](../CLAUDE.md) for detailed engineering constraints. Detailed established specifications retain formulas/contracts. New standard documents index them. Research, proposed guide entries, recovered memories, and an example from another project do not establish approved requirements.

Use stable REQ/AC IDs, dependency-ordered milestones, and these statuses: Not Started / In Progress / Implemented, Unverified / Verified / Blocked / Deferred. Record conflicts and materially unresolved decisions. Define before implementing; preserve working code and user changes; test before declaring completion; integrate before releasing.

Update `PROJECT_STATUS.md` after meaningful work and at session exit with inspected repository facts, current verification, blocking/nonblocking choices, and a concrete next action. Do not copy an old test count into a fresh-result claim. A successful build is not acceptance; player comprehension requires observations. Avoid empty documents and duplicate authorities.

The OpenJRPG-specific RPG features, package/extension policies and ADR-014/015 in the example adoption prompt do not apply unless separately adopted for OpenAntares. The project name stays **OpenAntares**; this is a workflow adaptation, not a product migration.
