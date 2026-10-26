import re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
path=ROOT/'PROJECT_STATUS.md';text=path.read_text(encoding='utf-8')
text=text.replace('Updated2026-10-25 America/Denver.','Updated2026-10-26 America/Denver.')
start=text.index('Current continuation:');end=text.index('\n\n## Current product',start)
text=text[:start]+'''Current continuation: the remaining scout review M1 setup detail is fixed on isolated `codex/scout-polish`, validated source907207f, from preserved scout-survival47e4b38. [Follow-up validation](reports/SCOUT_POLISH_2026-10-26.md) records actual RED→GREEN, Scout237, full Suite1398/1398, normal build/metadata and owner preservation. SCT-AC-013 and M7-I are now Verified for local automated scope; no known review finding remains open. The full approved design/15-task implementation, one independent review and its three Important corrections remain complete; the new follow-up did not commission a second review. [Earlier full evidence](reports/SCOUT_VALIDATION_2026-10-25.md), [raw review/disposition](reports/scout-execution/final-review.md) and [ledger](reports/scout-execution/progress.md) retain their historical M1 deferral. Do not repeat completed work or re-ask design, plan or Native approval.'''+text[end:]
text=text.replace('This review branch defaults','This follow-up branch defaults')
text=re.sub(r'^\| M7-I \|.*$', '| M7-I | Verified | Local automated scope; remaining review M1 corrected and verified2026-10-26 |',text,flags=re.M)
start=text.index('Fresh final simulation suite:');end=text.index('\n\n## Preservation',start)
text=text[:start]+'''Latest follow-up:1398 simulation tests and237 actual Scout native records pass,0 diagnostics; normal builds exclude all7 scenarios and both helpers. Five custom-map boundaries verify the omitted-habitat explanation, usable Start, independent Off, private candidate locations and deterministic previews. The other six native scenarios retain their prior-source evidence from the full review/correction run; no fresh rerun is claimed for them.24 untouched generated campaigns/720 Off-Observe parity boundaries and all49 frozen rules6 baselines/196 old-engine futures retain their verified simulation coverage. Rare threshold fixtures remain separately labelled.'''+text[end:]
path.write_text(text,encoding='utf-8')

for name in ['docs/SPECIFICATIONS.md','docs/ACCEPTANCE_CRITERIA.md','docs/IMPLEMENTATION_PLAN.md','reports/VALIDATION_REPORT.md']:
    path=ROOT/name;text=path.read_text(encoding='utf-8');marker='<!-- Scout M1 closure2026-10-26 -->'
    if marker in text:text=text.split(marker)[0].rstrip()+'\n'
    link='../reports/SCOUT_POLISH_2026-10-26.md' if name.startswith('docs/') else 'SCOUT_POLISH_2026-10-26.md'
    text+=f'''\n{marker}
## Scout M1 follow-up closure — 2026-10-26

The remaining omitted-habitat setup explanation is corrected on codex/scout-polish, validated source907207f, with actual native RED→GREEN and five custom-map boundaries. [Follow-up evidence]({link}): Scout237, full Suite1398/1398,0 diagnostics, ordinary build0 warnings/errors and actual7-scenario/2-helper exclusions. SCT-AC-013/M7-I are now Verified for their local automated scope; earlier M1 deferral statements are historical. No known review finding remains open. Other six native scenarios retain their prior-source evidence; no second independent review or fresh other-six run is claimed. Primary main42e1282/four owner edits/seven worktrees/old evidence remain preserved; no integration/push/release/cleanup. Parent REQ-017/17-row totals and all human/platform/security/export/release limits remain unchanged. Next is owner disposition of the completed follow-up branch.
'''
    if name=='docs/IMPLEMENTATION_PLAN.md':text=re.sub(r'^\| M7-I \|.*$','| M7-I | Implement approved scout/ameba slice | Approved M7-P;49 frozen rules6 baselines/196 futures | Functional criteria, retained gates, single review/corrections plus M1 follow-up | Verified for local automated scope;2026-10-26 M1 closure |',text,flags=re.M)
    path.write_text(text,encoding='utf-8')

path=ROOT/'AGENTS.md';text=path.read_text(encoding='utf-8')
text+='''
Current M7 polish continuation2026-10-26: user continued in this chat; the remaining review M1 custom-map setup explanation is fixed on codex/scout-polish, validated source907207f from preserved scout-survival47e4b38. Read reports/SCOUT_POLISH_2026-10-26.md and reports/scout-polish-20261026/summary.json. Actual missing-message RED→GREEN, Scout237 and Suite1398 pass; ordinary build0 warnings/errors excludes all7 scenarios/both helpers. SCT-AC-013/M7-I Verified for local automated scope; no known open review finding. Prior M1 deferral is historical, not a current task. No second independent review; other six native gates remain prior-source evidence. Main42e1282/four owner hashes/seven worktrees/every branch and evidence retained; no integration/push/release/cleanup. Parent counts and human/delivery limits unchanged. Next is owner disposition of the completed follow-up branch; do not repeat work or infer a new subsystem.
'''
path.write_text(text,encoding='utf-8')
print('Updated current status and acceptance; preserved historical review evidence and parent requirement rows.')
