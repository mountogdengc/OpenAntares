"""Windows Native execution ledger/brief bridge; preserves all evidence."""
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PLAN = "docs/superpowers/plans/2026-10-25-scout-survival-and-ameba.md"
WORK = ROOT / ".superpowers/sdd/2026-10-25-scout-survival-and-ameba"
WORK.mkdir(parents=True, exist_ok=True)
LEDGER = WORK / "progress.md"

def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()

def append(text):
    with LEDGER.open("a", encoding="utf-8", newline="\n") as stream:
        stream.write(text + "\n")

if not LEDGER.exists():
    append("# SDD ledger — plan: " + PLAN)
    append("\nApproved separate plan: owner approved after b9a39d7; Native inline, all15 tasks, one final fresh whole-branch review; no per-task agents. Execution branch codex/scout-survival, baseline b9a39d7. Primary42e1282 and four owner files retained.\n")
    append("Ruling: Use this Python Windows bridge for task-start/task-done/brief extraction because the supplied skill scripts require unavailable bash — same BASE/brief/test/result ledger contract — cost if wrong: bookkeeping compatibility, not gameplay.\n")
    append("Ruling: Preserve this plan workspace and every prior evidence directory rather than skill-suggested deletion — explicit project/owner preservation governs — cost if wrong: retained disk space.\n")
    append("## Pre-flight shared interfaces\n| Producers → consumers | Contract | Finding |\n| --- | --- | --- |")
    rows = [
        ("1→2..15", "unchanged-engine futures before product changes", "capture gate first"),
        ("2→3..14", "rules7/content/predicates, default6", "old1..6 explicit"),
        ("3→4..11", "state/immutable records/command declarations/fixtures", "declarations precede consumers"),
        ("4→5/8/14", "generation/independent streams/scout defaults", "no main RNG/ID change"),
        ("5→6/7/9/10/12", "private base projection/effective action tokens", "avoid public projection recursion"),
        ("6→7/8/10/12", "shared command/risk/ack/retreat authority", "no direct mutation shortcut"),
        ("7→8/10/12", "consent/actual legal departures/atomic preparation", "cargo holds block; no skipped grace"),
        ("8→9/11/13", "consumed IDs/context/delayed awards", "transport must skip consumed only"),
        ("9→11/12/13", "bounded history/retired immutable facts", "no live destroyed reference"),
        ("10→11/14/15", "own-view AI whitelist/budgets", "no study or free replacement"),
        ("11→12..15", "writer8/29 pairs/genuine old futures", "capture exists before switch"),
        ("12→13", "existing Crisis UI helper", "independent UI gate avoids cycle"),
        ("13→14/15", "7th harness/metadata/actual controls", "sequential shared assembly"),
        ("14→15", "rules7/Standard defaults/generated loops", "activation after controls"),
    ]
    for source, contract, finding in rows:
        append(f"| {source} | {contract} | {finding} |")
    append("\n## Tasks\n" + "\n".join(f"- [ ] Task {n}" for n in range(1,16)))

action = sys.argv[1]
number = int(sys.argv[2])
basefile = WORK / f"task{number}-base.txt"
if action == "start":
    if re.search(rf"Task {number}: complete\b", LEDGER.read_text(encoding="utf-8")):
        raise SystemExit(f"Task {number} already complete; do not repeat")
    plan = (ROOT / PLAN).read_text(encoding="utf-8")
    body = re.split(rf"^### Task {number}: ", plan, flags=re.M)[1]
    body = re.split(r"^### Task \d+: |^## Coverage", body, maxsplit=1, flags=re.M)[0]
    brief = WORK / f"task{number}-brief.md"
    brief.write_text(f"# Task {number}: " + body, encoding="utf-8")
    if not basefile.exists():
        basefile.write_text(git("rev-parse", "HEAD"), encoding="utf-8")
    append(f"\nTask {number}: started BASE {basefile.read_text().strip()}; brief {brief.name}")
    print(f"BASE {basefile.read_text().strip()}\n{brief.read_text(encoding='utf-8')}")
elif action == "done":
    command = sys.argv[3:]
    if command and command[0] == "--":
        command = command[1:]
    log = WORK / f"task{number}-completion.log"
    with log.open("w", encoding="utf-8") as stream:
        result = subprocess.run(command, cwd=ROOT, stdout=stream, stderr=subprocess.STDOUT)
    print("\n".join(log.read_text(encoding="utf-8").splitlines()[-18:]))
    if result.returncode:
        raise SystemExit(result.returncode)
    append(f"Task {number}: complete (commits {basefile.read_text().strip()[:7]}..{git('rev-parse','--short','HEAD')}, tests: {' '.join(command)} → exit0; {log.name})")
    text = LEDGER.read_text(encoding="utf-8").replace(f"- [ ] Task {number}\n", f"- [x] Task {number}\n")
    LEDGER.write_text(text, encoding="utf-8")
    (ROOT / "reports/scout-execution/progress.md").write_text(text, encoding="utf-8")
