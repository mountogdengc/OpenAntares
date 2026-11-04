"""Read retained queue evidence and verify it still describes this checkout."""
import hashlib
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path

root = Path(__file__).resolve().parents[1]
evidence = root / "reports/ui-layout-trial"
final = evidence / "final-02"
queue = Path("F:/Tools/CodexTestRunner/state/results/openantares")
portable = queue / "0b12df0e956b4e6dbd698b9e5093b80a"
native = queue / "009a01e39e6440c7aca381f76f19b770"
preparation = queue / "293575d3ea384d7d8dd98c9b508219fc"


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def require(condition, text):
    if not condition:
        raise RuntimeError(text)


require(read(native / "result.json")["exit_code"] == 0, "Strict layout queue job did not pass")
result = read(final / "results.json")
require(result["passed"] and result["checks"] and all(c["passed"] for c in result["checks"]), "Layout assertion failure/missing evidence")
source = read(final / "source.json")
require(Path(source["sourceRoot"]).resolve() == root, "Layout evidence belongs to another checkout")
for entry in source["files"]:
    actual = hashlib.sha256((root / entry["path"]).read_bytes()).hexdigest()
    require(actual.lower() == entry["sha256"].lower(), "Changed since native check: " + entry["path"])
for name in ("baseline-build.log", "validation-build.log", "normal-rebuild.log"):
    log = (final / name).read_text(encoding="utf-8-sig")
    require("Build succeeded." in log and "0 Warning(s)" in log and "0 Error(s)" in log, "Build evidence failed: " + name)
console = (final / "console.log").read_text(encoding="utf-8-sig")
require(not re.search(r"^ERROR:|^SCRIPT ERROR:|^WARNING:.*leaked", console, re.M), "Unresolved Godot diagnostics")
normal = read(final / "normal-metadata.json")
require(normal["presentScenarioTypes"] == [] and normal["presentHelpers"] == [], "Validation leaked into ordinary game")
require(read(final / "opt-in-metadata.json")["presentScenarioTypes"] == ["LayoutTrialValidation"], "Missing real metadata negative control")
portable_result = read(portable / "result.json")
require(portable_result["status"] == "passed", "Portable suite did not pass")
step = portable_result["steps"][0]
require(step["passed"] == 2134 and step["failed"] == 0 and step["skipped"] == 0, "Unexpected portable coverage")
snapshot = read(portable / "snapshot.json")
for name, expected in snapshot["files"].items():
    require(hashlib.sha256((root / name).read_bytes()).hexdigest() == expected, "Changed since portable snapshot: " + name)
trx = ET.parse(portable / "core.trx")
counters = trx.find(".//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}Counters")
require(counters is not None and counters.get("passed") == "2134" and counters.get("failed") == "0", "TRX counters do not support result")
require(read(preparation / "result.json")["exit_code"] == 0, "Asset preparation/startup failed")
startup = (evidence / "preparation-01/startup-console.log").read_text(encoding="utf-8-sig")
require(not re.search(r"^ERROR:|^SCRIPT ERROR:|Exception:", startup, re.M), "Startup diagnostics remain")
summary = {
    "sourceRoot": str(root),
    "baseline": source["head"],
    "layoutChecks": len(result["checks"]),
    "layoutJob": native.name,
    "layoutSourceFilesMatch": len(source["files"]),
    "portablePassed": 2134,
    "portableFailed": 0,
    "portableSkipped": 0,
    "portableJob": portable.name,
    "portableDigest": snapshot["digest"],
    "portableSourceFilesMatch": len(snapshot["files"]),
    "preparationJob": preparation.name,
    "ordinaryScenarioTypes": normal["presentScenarioTypes"],
    "ordinaryHelpers": normal["presentHelpers"],
    "retainedQaSavePaths": result["retainedQaSavePaths"],
    "review": "One fresh review; corrections and a subsequent control-lifetime fix checked by retained automation",
    "integration": "Isolated trial; uncommitted, no main merge/push/release/cleanup",
    "limits": [
        "Headless geometry and in-process control signals do not prove rendering, OS input or comfort.",
        "Portable profile explicitly excludes six Windows/native-evidence cases.",
        "Historical graphical harness toolbar/detail-navigation assumptions require adaptation before adoption.",
        "Startup preparation precedes final normal rebuild; final retained scenario separately exercises actual Main New Game and Game-menu Save/Load on matching final source.",
    ],
}
(evidence / "summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
print(json.dumps(summary, indent=2))
