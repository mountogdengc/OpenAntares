# Owner design directions: delegation, crises and fleet intent

2026-10-24. **Status: recorded owner intent and drafter proposals; future mechanics remain Deferred.** This is not an approved implementation specification. Current M5 proposal is [colony development](../superpowers/specs/2026-10-24-colony-development-design.md); current product has no revolt/plague/combat system. Preserve FR-001 Peaceful mode as its existing separate uncommitted backlog item; this note does not alter it.

## Owner statements and priorities

- Building useful foundations on most planets is satisfying, especially buildings with output that does not consume dedicated population. After foundations, players should be able to give planets different purposes.
- Many colonies make repeated manual decisions unmanageable. Governors should handle ordinary work/problems and report; roughly five or ten colonies was an example of where delegation becomes attractive, not a fixed approved threshold.
- Planets should be enjoyable to inspect and visually vary even within the same type.
- Provide default templates and creation of custom templates. Industrial, Growth and Research are suggestions; the owner explicitly asks the designer not to ignore better solutions or treat examples as exhaustive decisions.
- Colony-loss threats and threats that can spread to other worlds need timely warnings and opportunities to contain/fix them. Revolts and plagues are concrete examples. Such emergencies should interrupt before the next turn by default; notification levels should be configurable.
- A serious problem should offer multiple approaches with different benefits/penalties. For disease, examples are seeking a cure, quarantining and hoping the colony survives, and destroying infected areas or the colony to stop spread. Reactions elsewhere in the empire may be negative or may differ by culture.
- Warships should follow player goals, strategies, targets and retreat/hold-line orders rather than requiring every movement of every craft to be managed.
- Scouts encountering danger should try to survive/escape. Encounter should not automatically mean an unavoidable tactical battle and loss. Speed, scanning/warning range and a chance to escape were suggested factors, not approved formulas.

## Proposed future crisis contract

These are drafter recommendations derived from the above, not new accepted species/culture rules:

1. Developing existential or spreading threats have understandable warning stages with at least one meaningful opportunity to respond. Show what is known, what could be lost/spread, uncertainty, time pressure, actions taken by governors and available responses. A report announcing irreversible loss afterward is not adequate warning for a preventable developing threat.
2. Governors act only inside explicit authority. Routine reversible containment may be delegated. Destruction, abandonment and comparable irreversible measures need a direct order or a separately and explicitly enabled doctrine. No culture label silently grants that permission.
3. Emergency acknowledgement is distinct from choosing a cure: the player may choose a response or knowingly continue, with renewed escalation if the threat materially worsens. Default acknowledgement is per affected human empire before its next Ready, saved across reload and never satisfied by an unrelated governor report. Exact state/timing and notification override safeguards require the crisis specification.
4. Local notification settings should support routine summaries, persistent attention and emergency interruption, with category customization. If a player explicitly reduces emergency interruption, explain the consequence; do not silently hide critical facts in an Off filter. Exact override behavior remains a future design decision.
5. Cure, quarantine and destruction must each have concrete costs, timing, failure/spread risks and effects on population, output and other worlds where relevant. None is automatically a universal best option. Do not invent trade/currency mechanics simply to populate a penalty column.
6. Reactions should depend on defined cultural values, circumstances and the actual measure taken. Possible reactions include horror at destruction, acceptance of collective sacrifice, anger at preventable neglect or resistance to prolonged isolation. Which cultures react how, their formulas and the political/diplomatic mechanisms are not approved by this discussion. Existing species identity must not be rewritten casually or mapped to disrespectful real-world caricatures.

No plague probabilities, transmission graph, revolt thresholds, quarantine effect, loss/bombardment rule, morale resource or culture modifier is selected here. They need a bounded full design, compatibility policy and acceptance examples before implementation.

## Proposed future fleet-intent contract

Player-authored standing orders should express objectives, preferred/forbidden targets, engagement criteria, retreat triggers and explicit hold-at-all-costs orders. Distinguish civilian/scout survival defaults from a combat fleet's deliberate risk. Provide actual reports explaining how orders were executed, blocked or overridden by higher-priority orders; never require hidden knowledge to obey them.

Scouting should include a detection/avoidance/escape opportunity where applicable. Candidate factors are warning distance, detection capability, relative speed and interception circumstances. No guaranteed escape or exact formula is selected. Save/resolve deterministic outcomes through ordinary simulation rules; do not introduce tactical UI as an automatic death trap. The relationship between detected threats, movement, interception and future combat must be defined before code.

Fleet grouping, warship construction/design, tactical versus strategic combat, retreat destinations, pursuit and doctrine conflicts are separate material decisions. This note authorizes none of them for M5. Preserve it when selecting that later milestone instead of treating the owner's examples as an exact finished design.
