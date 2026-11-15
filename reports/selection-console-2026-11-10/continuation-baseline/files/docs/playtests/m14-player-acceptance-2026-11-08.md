# M13 + M14 player acceptance

The goal is to find out whether you can understand and enjoy running your empire through the new interface. You do not need to inspect logs, count controls or repeat automated checks.

**Build:** M14 visual overhaul with M13 campaign polish included. The normal game is built and ready; you do not need to build anything.

**Open the game:** stop the old run and press F5 in the same OpenAntares Godot project.

**Before starting:** use the Windows game window. Its normal size is 1920×1080; smaller windows down to 800×600 are supported. No additional setup or test tools are needed. If the game was already running when these changes were applied, stop that run in Godot and press F5 again in the same OpenAntares project.

Use this M14 build for the playthrough. It includes M13's campaign polish. Existing campaigns are retained. A new small campaign is the easiest place to begin; choose any species you like. Save under a new name whenever you want to keep a stopping point.

For each journey, mark **Works for me**, **Confusing**, or **Broken**. A short description of where you got stuck is enough. Screenshots are useful when something looks wrong, but optional.

| Journey | What you are trying to accomplish | What a successful experience feels like |
| --- | --- | --- |
| 1. Begin a campaign | Choose your species and settings, then find your home system. | The choices make sense, the main screen is readable, and you know what you are looking at. |
| 2. Read your empire | Find your population, saved production and research. Work out what the numbers in parentheses mean. | You can distinguish what you have now from the expected change next turn. Tooltips answer your questions. |
| 3. Run a colony | Open Colonies. Move one worker to another role using a drag or the keyboard. Open that colony and choose construction. | The worker moves once, the new output is clear, and any rejected action explains why. You can return to the map without losing your place. |
| 4. Choose research | Open Research, compare available choices, choose a technology, and return to your previous screen. | You understand what you selected and how research progress is shown. Leaving research unselected clearly means saving it. |
| 5. Explore | On the galaxy map, click the flag beside your home system, then select a fleet or independent ship from the right panel. Hold Shift and left-click other rows to add or remove them. Hover over another system to preview, then click it to set course immediately. Try the mouse wheel and Fit controls. | One flag opens the individual fleets at that system. Hovering never gives orders; red means the current rules reject that move. Clicking a system sets course for all selected fleets together. Plain-clicking a row selects only that fleet; Shift-clicking toggles it. Right-click the map to clear selection and inspect normally. Zoom and panning do not issue orders. |
| 6. Navigate freely | Visit Fleets and its Scouts tab, Designs, Diplomacy and its Contacts tab. Press the same bottom key again or use Escape to leave. Press Galaxy to return to the map; pressing it again centers home. | You always know where you are. Text remains readable, buttons are reachable, and returning makes sense. |
| 7. Advance your campaign | Read the End Turn notices and continue for several turns. Follow a report to the affected world or ship. | The report appears in the right panel. Close report is near End Turn. Urgent decisions stand out from suggestions, and report links take you to what changed. |
| 8. Keep playing | Save, choose Game → Main menu, and Resume campaign. Then try loading your save. Try the screen-effect options in the Game menu. | Resume preserves your campaign. Loading restores the saved campaign. Display preferences do not alter gameplay. |

When you have multiple named fleets together, right-click one to split selected ships into a new fleet. Drag one named fleet onto another to merge into the destination fleet. An independent ship can be named through right-click → Form fleet. Existing restrictions still apply.

If an encounter or colony emergency occurs naturally, try reviewing it, backing out and reopening it before deciding. Leaving the screen should not count as answering the decision.

Please tell me:

- Which journey you were on and what you expected to happen.
- What actually happened, or which wording or layout was confusing.
- Whether you could recover and continue playing.
- Whether the overall interface now feels clear enough to play, and the one thing you would change first.

Human acceptance remains pending until you have played. Automated rendering and interaction checks do not answer whether this interface works for you.

After you report back, I will use your findings to decide whether the interface is ready or needs a further UI change. Your playthrough is the remaining acceptance step; this does not automatically merge or release the work.

## Owner playtest observations — 2026-11-09

Owner confirmed fleet selection/split/merge, colony production/worker readability, worker drag with immediate projected-output update, construction choice, research choice and repeated-key closing, End Turn report readability and close placement, save/Main menu/Resume/load restoration, three further understandable turns, and wheel zoom with easy system/flag hits. These are the owner’s reports for the journeys played, not a claim of complete campaign, combat/crisis, accessibility or platform acceptance.

Follow-up: fleet flags must not highlight/select their system. Numpad8/2/4/6 pans the galaxy map; numpad+ and− zoom around map center. Number-row keys are unchanged; typing, open popups and other workspaces suppress these controls. Restart the current Godot run with F5 after saving the campaign. Check the flag independently, then the numpad controls; report which action differs from the expected behavior.
