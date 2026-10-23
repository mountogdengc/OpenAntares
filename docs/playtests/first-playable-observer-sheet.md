# First Playable observer sheet

For AC-014 and tasks 3-5 of the approved validation plan. Use a participant unfamiliar with the source. This sheet is a procedure and empty observation form, not a completed playtest or acceptance evidence.

**Current disposition:** Deferred by the owner under ADR-005. Retained for later use; this is not an immediate task for the owner and does not block exploration/colonization design.

## Before the session

- Observer: record a participant alias, date, build/commit plus local changes, OS, screen/window dimensions, size and seed. Do not collect unnecessary identifying information.
- Start with Small, seed 42; other sizes can be checked separately. Preserve existing saves. Use a unique `qa-<date>-<alias>` save name.
- Let the participant read the interface. Record where they get stuck before giving any control help. Keep the source/specification and this observer guidance out of their view.
- Do not explain the economy or tell them the correct allocation. This is an understanding check, not a test of whether they can follow coaching.

## Participant tasks and observer prompts

1. **Start and inspect.** Ask them to start a game, find their colony, inspect another system, and return. Record how they recognized their colony and what planet information they used.
2. **Predict an allocation.** Before End Turn, ask: "What do you expect to happen to support, population growth, production and research? What on screen tells you that?" Write their prediction verbatim before advancing.
3. **Compare choices.** Ask them to compare two workforce assignments for two goals: grow the colony sooner, and complete a chosen building or technology sooner. Ask which they prefer for each goal and what they give up. Record whether their preference changes and why.
4. **Observe a shortage.** Let them try an allocation with insufficient support. Ask what they expect to lose and whether growth continues. Record whether the warning and forecast explain it before the turn; compare their explanation afterward.
5. **Complete goals.** Let them choose a construction project and research target and advance until each completes and population grows. Ask: "What changed, when does it start helping, and what would you do next?" Include the following turn; record use of the completion follow-up controls.
6. **Save and resume.** Ask them to save unfinished progress, load that save through the UI, explain whether their choices/progress survived, and continue. Repeat after a completion if practical.
7. **Reflect.** Ask which choice was hardest to understand, what they clicked repeatedly, and whether they saw more than one sensible plan. Record specific examples rather than an overall satisfaction score alone.

## Observation record

Session alias/date:

Build/working-tree description, OS/window, size/seed:

Save names:

Control help given (with the point where the participant first got stuck):

| Moment | Prediction or explanation in participant's words | Observed result / confusion | Repeated clicks or screen switching |
| --- | --- | --- | --- |
| Initial allocation | | | |
| Growth goal vs completion goal | | | |
| Support shortage | | | |
| Construction completion/follow-up | | | |
| Research completion/follow-up | | | |
| Next-turn effects | | | |
| Save/reload and continuation | | | |

## Observer disposition

For each finding, record **blocker** (action/state fails), **clarity** (consequence cannot be understood), or **later** (outside First Playable). Include steps, size/seed/save, expected/actual result, evidence, and next action. Do not change balance merely because a participant chose a different defensible plan.

Findings and any retest evidence:

Remaining functional/platform/legacy exceptions:

First Playable decision: **still open / closed**, with reasons and owner review:

Closure requires the functional evidence plus understandable choices and no unresolved blocker/clarity findings. A filled sheet alone is not an automatic pass. Preserve the raw observations; add the conclusion to the main playtest record and update `PROJECT_STATUS.md`.
