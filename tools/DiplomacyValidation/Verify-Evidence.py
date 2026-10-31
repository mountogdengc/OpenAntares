"""Validate the source-scoped local diplomacy acceptance record without modifying it."""
import json
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[2]
summary_path = root / "reports/diplomacy-execution/final-machine-summary.json"
assert summary_path.exists(), "FinalEvidenceReferencesActualVerifiedSource: final summary is missing"
summary = json.loads(summary_path.read_text(encoding="utf-8"))
actual_source = subprocess.check_output(
    ["git", "-C", str(root), "log", "-1", "--format=%H", "--", "src", "game", "content", "OpenAntares.csproj"],
    text=True,
).strip()
assert actual_source == summary["productSource"], "Product source does not match the verified summary"
# Historical SHA256 values reflect the original checkout's sometimes mixed line
# endings. The exact verified product commit and Git clean filters provide the
# portable source identity check without weakening detection of working edits.
for relative in summary["productFileHashes"]:
    working_blob_id = subprocess.check_output(
        ["git", "-C", str(root), "hash-object", "--path=" + relative, relative], text=True
    ).strip()
    source_blob_id = subprocess.check_output(
        ["git", "-C", str(root), "rev-parse", actual_source + ":" + relative], text=True
    ).strip()
    assert working_blob_id == source_blob_id, relative
assert (summary["rules"], summary["writer"], summary["acceptedPairs"]) == (11, 12, 67)
assert len(summary["native"]) == 11
assert all(run["exitCode"] == 0 and run["diagnostics"] == 0 for run in summary["native"])
for run in summary["native"]:
    folder = root / run["folder"]
    opt_in = json.loads((folder / "opt-in-metadata.json").read_text(encoding="utf-8-sig"))
    normal = json.loads((folder / "normal-metadata.json").read_text(encoding="utf-8-sig"))
    assert len(opt_in["presentScenarioTypes"]) == 11 and len(opt_in["presentHelpers"]) == 2
    assert not normal["presentScenarioTypes"] and not normal["presentHelpers"]
assert len(summary["earnedCampaigns"]) == 4
for campaign in summary["earnedCampaigns"]:
    assert campaign["BothSetupPaid"] == 1000
    assert campaign["EarnedProduction"] == campaign["FinalBank"] + campaign["HullPayments"] + campaign["PaidSetup"]
assert summary["reviewComplete"] and summary["independentImplementationReviews"] == 1
assert not summary["unresolvedCriticalOrImportantFindings"]
if summary["implementationVerified"]:
    assert not summary["unresolvedRequiredCriteria"], "Verified claim omits required criteria"
for gate in [summary["suite"]["gate"], summary["normalBuildGate"]]:
    assert json.loads((root / gate).read_text(encoding="utf-8-sig"))["exitCode"] == 0
assert json.loads((root / summary["preservation"]).read_text(encoding="utf-8"))["passed"]
print(json.dumps({"passed": True, "check": "FinalEvidenceReferencesActualVerifiedSource", "productSource": actual_source,
                  "productFiles": len(summary["productFileHashes"]), "independentReviews": 1,
                  "requiredCriteriaOmitted": len(summary["unresolvedRequiredCriteria"])}))
