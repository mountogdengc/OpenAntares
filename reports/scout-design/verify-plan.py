"""Planning evidence only: independent PCG reference, source inventory and doc checks.

Does not import/build/run OpenAntares or capture a campaign's future behavior.
"""
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
PLAN = ROOT / "docs/superpowers/plans/2026-10-25-scout-survival-and-ameba.md"
MASK64, MASK32 = (1 << 64) - 1, (1 << 32) - 1


class ReferencePcg:
    def __init__(self, seed, sequence):
        self.state = 0
        self.increment = ((sequence << 1) | 1) & MASK64
        self.calls = 0
        self.next()
        self.state = (self.state + seed) & MASK64
        self.next()
        self.calls = 0

    def next(self):
        old = self.state
        self.state = (old * 6364136223846793005 + self.increment) & MASK64
        shifted = (((old >> 18) ^ old) >> 27) & MASK32
        rotation = old >> 59
        self.calls += 1
        return ((shifted >> rotation) | (shifted << ((-rotation) & 31))) & MASK32

    def bounded(self, bound):
        threshold = ((-bound) & MASK32) % bound
        while True:
            value = self.next()
            if value >= threshold:
                return value % bound


def write(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


reference = ReferencePcg(42, 54)
assert [reference.next() for _ in range(6)] == [
    0xA15C02B7, 0x7B47F409, 0xBA1D3330, 0x83D2F293, 0xBFA4784B, 0xCBED606E
]
vectors = {}
for label, sequence in [("placement", 0x4841424954415401), ("owner1", 1 ^ 0x4553434150450001), ("owner10", 10 ^ 0x4553434150450001)]:
    rng = ReferencePcg(42 ^ 0x414D454241000001, sequence)
    initial = {"state": rng.state, "increment": rng.increment}
    uints = [rng.next() for _ in range(4)]
    rng = ReferencePcg(42 ^ 0x414D454241000001, sequence)
    bound = 2 if label == "placement" else 100
    bounded = [rng.bounded(bound) for _ in range(4)]
    vectors[label] = {"initial": initial, "next_uint32": uints, "bound": bound,
                      "next_int": bounded, "raw_draws": rng.calls, "final_state": rng.state}
threshold_states = {}
rng = ReferencePcg(42 ^ 0x414D454241000001, 1 ^ 0x4553434150450001)
for index in range(100000):
    before = rng.state
    draw = rng.bounded(100)
    if draw in (49, 50, 74, 75, 94, 95) and str(draw) not in threshold_states:
        threshold_states[str(draw)] = {"state_before": before, "increment": rng.increment,
                                       "previous_bounded_roll_count": index, "draw": draw}
    if len(threshold_states) == 6:
        break
assert len(threshold_states) == 6
write("reference-vectors.json", {"method": "Independent Python reference; not execution of game code", "seed": 42, "vectors": vectors, "threshold_states": threshold_states})

primary = Path("F:/Projects-PCGames/OpenAntares")
summary_path = primary / "reports/crisis-execution/main-integration-summary.json"
summary = json.loads(summary_path.read_text(encoding="utf-8-sig"))
catalog = []
for scenario in ("Crisis", "Colony"):
    entry = summary["scenarios"][scenario]
    evidence_path = primary / entry["runtimeFolder"] / "results.json"
    assert hashlib.sha256(evidence_path.read_bytes()).hexdigest() == entry["resultsSha256"]
    data = json.loads(evidence_path.read_text(encoding="utf-8-sig"))
    for source in data["retainedQaSavePaths"]:
        path = Path(source)
        if "generated" not in path.name:
            continue
        raw = path.read_bytes()
        save = json.loads(raw.decode("utf-8-sig"))
        assert (save["schema_version"], save["rules_version"]) == (7, 6)
        state = save["state"]
        catalog.append({"scenario": scenario, "source": str(path),
                        "sha256": hashlib.sha256(raw).hexdigest(),
                        "turn": state["turn"], "crises_enabled": state["crisis"]["enabled"],
                        "colonies": len(state["colonies"]), "vessels": len(state["vessels"]),
                        "evidence": str(evidence_path), "evidence_sha256": entry["resultsSha256"],
                        "source_product_commit": summary["mainHeadBeforeIntegrationReport"]})
write("rules6-source-catalog.json", {"future_capture_executed": False, "source_count": len(catalog), "sources": catalog})
assert len(catalog) == 32
if "--references-only" in sys.argv:
    print(json.dumps({"vectors": vectors, "located_hashed_rules6_sources": len(catalog), "future_capture_executed": False}, indent=2))
    raise SystemExit(0)

text = PLAN.read_text(encoding="utf-8-sig")
approved = subprocess.check_output(["git", "show", "738c68e:docs/superpowers/specs/2026-10-25-scout-survival-and-encounter-avoidance-design.md"], cwd=ROOT)
assert hashlib.sha256(approved).hexdigest() == "aee687d783c041f0625c6c08216f9d7e706de6830f3b032b963d1a7f48ba8956"
current_spec = (ROOT / "docs/superpowers/specs/2026-10-25-scout-survival-and-encounter-avoidance-design.md").read_text(encoding="utf-8-sig")
assert approved.decode("utf-8").split("## 1.", 1)[1].split("## 9.", 1)[0] == current_spec.split("## 1.", 1)[1].split("## 9.", 1)[0]
tasks = re.findall(r"^### Task (\d+): (.+)$", text, re.M)
assert [int(number) for number, _ in tasks] == list(range(1, 16))
for block in re.split(r"^### Task \d+: ", text, flags=re.M)[1:]:
    for required in ("**Files:**", "**Interfaces:**", "- [ ] **RED", "- [ ] **Implement", "- [ ] **GREEN", "- [ ] **Commit"):
        assert required in block, (block[:70], required)
    assert "Focused(" in block or "Runtime(" in block
    assert "Assert." in block
for n in range(1, 8):
    assert f"| SCT-{n:03d} |" in text
for n in range(1, 16):
    assert f"| SCT-AC-{n:03d} |" in text
links = 0
for doc in (PLAN, ROOT / "docs/DECISIONS/ADR-016-approved-scout-survival-and-ameba-design.md"):
    for target in re.findall(r"\[[^\]]+\]\(([^)]+)\)", doc.read_text(encoding="utf-8-sig")):
        path = target.split("#")[0]
        if path and not re.match(r"https?://", path):
            assert (doc.parent / path).exists(), (doc.name, path)
            links += 1
existing = re.findall(r"^\| Modify \| `([^`]+)` \|", text, re.M)
for path in existing:
    assert (ROOT / path).is_file(), path
assert "[Fact]" in text and "16 generated" in text
assert "Genuine rules6 future capture has not run" in text
assert "No per-task agents" in text
write("plan-checks.json", {"plan_sha256": hashlib.sha256(PLAN.read_bytes()).hexdigest(),
                          "tasks": len(tasks), "linked_local_files": links, "existing_targets_checked": len(existing),
                          "requirements_mapped": 7, "acceptance_criteria_mapped": 15,
                          "pcg_reference_vectors": vectors, "located_hashed_rules6_sources": len(catalog),
                          "self_review": "Coverage, step granularity, interfaces, five review-focus cases and proportion checked inline; no plan reviewer dispatched",
                          "design_approved": True, "plan_approved": False, "implementation_started": False,
                          "gameplay_contract_unchanged_since_approval": True,
                          "application_build_tests_run": False, "genuine_rules6_future_capture_run": False})
print(json.dumps({"tasks": len(tasks), "links": links, "existing_targets": len(existing),
                  "requirements": 7, "criteria": 15, "hashed_sources": len(catalog), "future_capture_run": False}, indent=2))
