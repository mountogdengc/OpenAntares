"""Fail closed when the approved conflict milestone is missing evidence.

This checks recorded artifacts and their identity; it does not turn a simulation
rehearsal into native, physical-input, balance or release acceptance.
"""
import argparse
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
import subprocess
import re
import shutil

SCENARIOS = {"Legacy", "Expansion", "Species", "Rival", "Colony", "Crisis",
             "Scout", "Fleet", "Combat", "ShipDesign", "Diplomacy", "EmpireConflict"}
STAGES = {"pending-declaration", "queued-peace-answer", "pending-attack",
          "active-battle", "queued-withdrawal", "effective-blockade", "truce"}
SOURCE_ROOTS = ("src", "game", "content", "tests/OpenAntares.Game.Validation",
                "tools/ConflictCampaignProbe", "tools/ConflictValidation",
                "OpenAntares.csproj", "project.godot")


def tracked_sources(root):
    if not (root / ".git").exists():
        raise OSError("Source root is not an actual Git checkout.")
    return set(subprocess.check_output(["git", "-C", str(root), "ls-files", "--", *SOURCE_ROOTS], text=True, stderr=subprocess.PIPE).splitlines())


def capture_source(root, output, assembly):
    root=root.resolve()
    output=output.resolve()
    if not output.is_relative_to(root) or output.exists():
        raise ValueError("Source evidence must use a fresh path inside this checkout.")
    paths=tracked_sources(root)
    if not paths:
        raise ValueError("No authoritative tracked sources found.")
    commit=subprocess.check_output(["git","-C",str(root),"rev-parse","HEAD"],text=True).strip()
    if subprocess.run(["git","-C",str(root),"diff","--quiet","HEAD","--",*SOURCE_ROOTS]).returncode != 0:
        raise ValueError("Commit the reviewed source before native acceptance capture.")
    untracked=subprocess.check_output(["git","-C",str(root),"ls-files","--others","--exclude-standard","--",*SOURCE_ROOTS],text=True).splitlines()
    if any(Path(name).suffix in {".cs",".csproj",".py",".ps1",".targets",".tscn",".json"} for name in untracked):
        raise ValueError("Untracked implementation or validation source is not valid acceptance evidence.")
    rows=[]
    for name in sorted(paths):
        data=(root/name).read_bytes()
        rows.append({"path":name,"sha256":hashlib.sha256(data).hexdigest(),"canonicalSha256":hashlib.sha256(data.replace(b"\r\n",b"\n")).hexdigest()})
    output.parent.mkdir(parents=True,exist_ok=True)
    retained=output.parent/"validation-assembly.dll"
    if retained.exists():
        raise ValueError("Retain earlier assembly evidence; choose a fresh output directory.")
    shutil.copyfile(assembly,retained)
    result={"commit":commit,"files":rows,"assembly":{"path":retained.relative_to(root).as_posix(),"sha256":hashlib.sha256(retained.read_bytes()).hexdigest()}}
    output.write_text(json.dumps(result,indent=2),encoding="utf-8")
    return {"path":output.relative_to(root).as_posix(),"sha256":hashlib.sha256(output.read_bytes()).hexdigest(),"files":len(rows)}


def verify(summary, root):
    errors = []
    root = root.resolve()

    def artifact(item, label, json_content=True):
        if not isinstance(item, dict) or not item.get("path") or not re.fullmatch(r"[0-9a-f]{64}", str(item.get("sha256", ""))):
            errors.append("Missing hashed artifact: " + label)
            return None
        path = (root / item["path"]).resolve()
        if not path.is_relative_to(root) or not path.is_file():
            errors.append("Missing or out-of-root artifact: " + label)
            return None
        raw = path.read_bytes()
        if hashlib.sha256(raw).hexdigest() != item["sha256"]:
            errors.append("Artifact digest mismatch: " + label)
            return None
        if not json_content:
            return raw
        try:
            document=json.loads(raw.decode("utf-8-sig"))
            if not isinstance(document,dict):
                raise ValueError("Expected an object.")
            return document
        except (ValueError, UnicodeError):
            errors.append("Invalid JSON artifact: " + label)
            return None

    def source_identity(source, label, current=False):
        if not isinstance(source, dict) or not re.fullmatch(r"[0-9a-f]{40}", str(source.get("commit", ""))):
            errors.append("Missing committed source identity: " + label)
            return
        try:
            expected = set(subprocess.check_output(["git", "-C", str(root), "ls-tree", "-r", "--name-only", source["commit"], "--", *SOURCE_ROOTS], text=True, stderr=subprocess.PIPE).splitlines())
        except (OSError, subprocess.CalledProcessError):
            errors.append("Recorded source commit unavailable: " + label)
            return
        rows = source.get("files", [])
        paths = [row.get("path") for row in rows]
        if not expected or not expected.issubset(set(paths)) or len(paths) != len(set(paths)):
            errors.append("Incomplete authoritative source coverage: " + label)
        for row in rows:
            if row.get("path") not in expected:
                continue
            try:
                blob = subprocess.check_output(["git", "-C", str(root), "show", source["commit"] + ":" + row["path"]], stderr=subprocess.PIPE)
            except subprocess.CalledProcessError:
                errors.append("Missing source blob: " + row["path"])
                continue
            canonical = hashlib.sha256(blob.replace(b"\r\n", b"\n")).hexdigest()
            if row.get("canonicalSha256") != canonical:
                errors.append("Source commit mismatch: " + row["path"])
            if current:
                actual = artifact(row, "current source " + row["path"], False)
                if actual is not None and hashlib.sha256(actual.replace(b"\r\n", b"\n")).hexdigest() != canonical:
                    errors.append("Current source differs from product commit: " + row["path"])
    if summary.get("status") != "Verified":
        errors.append("Milestone is not Verified.")
    if summary.get("unresolvedRequiredCriteria", ["missing"]):
        errors.append("Required criteria remain unresolved or are unrecorded.")
    if summary.get("independentImplementationReviews") != 1:
        errors.append("Exactly one independent implementation review is required.")
    if summary.get("unresolvedMaterialFindings", ["missing"]):
        errors.append("Material review findings remain unresolved or are unrecorded.")
    if (summary.get("defaultRules"), summary.get("writerSchema"), summary.get("supportedPairs")) != (12, 13, 79):
        errors.append("Final activation/writer/compatibility identity is incomplete.")
    manifest = summary.get("productSource", {}).get("files", [])
    try:
        expected = tracked_sources(root)
        recorded = [item.get("path") for item in manifest]
        if not expected or not expected.issubset(set(recorded)) or len(recorded) != len(set(recorded)):
            errors.append("Incomplete authoritative source coverage for the current checkout.")
    except (OSError, subprocess.CalledProcessError):
        errors.append("Authoritative source coverage cannot be established.")
    source_identity(summary.get("productSource"), "current product", True)
    if not manifest:
        errors.append("Actual product source manifest is missing.")
    for item in manifest:
        path = (root / item.get("path", "")).resolve()
        if not path.is_relative_to(root.resolve()) or not path.is_file():
            errors.append("Missing or out-of-root source: " + item.get("path", ""))
        elif hashlib.sha256(path.read_bytes()).hexdigest() != item.get("sha256"):
            errors.append("Product source mismatch: " + item["path"])
    criteria = summary.get("criteria", [])
    if {c.get("id") for c in criteria} != {f"ECF-AC-{n:03}" for n in range(1, 13)} or len(criteria) != 12:
        errors.append("All twelve approved criteria must be recorded exactly once.")
    for criterion in criteria:
        if criterion.get("status") != "Verified" or not criterion.get("evidence"):
            errors.append("Unverified criterion: " + str(criterion.get("id")))
        for item in criterion.get("evidence", []):
            path = (root / item.get("path", "")).resolve()
            if not path.is_relative_to(root.resolve()) or not path.is_file():
                errors.append("Missing criterion evidence: " + item.get("path", ""))
            elif hashlib.sha256(path.read_bytes()).hexdigest() != item.get("sha256"):
                errors.append("Criterion evidence mismatch: " + item["path"])
    native = summary.get("nativeMatrix", [])
    if len(native) != 12 or {r.get("scenario") for r in native} != SCENARIOS:
        errors.append("Complete twelve-scenario native matrix is missing.")
    for run in native:
        if run.get("exitCode") != 0 or run.get("diagnostics") != [] or run.get("method") != "native" or not run.get("source") or not run.get("evidence"):
            errors.append("Native run is incomplete: " + str(run.get("scenario")))
        results = artifact(run.get("evidence"), "native results " + str(run.get("scenario")))
        source = artifact(run.get("source"), "native source " + str(run.get("scenario")))
        execution = artifact(run.get("execution"), "native execution " + str(run.get("scenario")))
        normal = artifact(run.get("normalMetadata"), "ordinary assembly metadata")
        negative = artifact(run.get("optInMetadata"), "real opt-in metadata")
        log = artifact(run.get("engineLog"), "native engine log", False)
        if source is not None:
            source_identity(source, "native " + str(run.get("scenario")))
            assembly = artifact(source.get("assembly"), "executed opt-in assembly", False)
            if assembly is None:
                errors.append("Executed native assembly is unrecorded.")
        if results is not None and (results.get("passed") is not True or results.get("error") is not None or results.get("scenario") != run.get("scenario") or not results.get("checks") or any(c.get("passed") is not True for c in results["checks"])):
            errors.append("Native assertions did not pass: " + str(run.get("scenario")))
        source_reference=run.get("source") if isinstance(run.get("source"),dict) else {}
        if execution is not None and (execution.get("scenario") != run.get("scenario") or execution.get("exitCode") != 0 or execution.get("diagnostics") != [] or execution.get("sourceStable") is not True or execution.get("sourceSha256") != source_reference.get("sha256") or execution.get("assemblySha256") != (source or {}).get("assembly", {}).get("sha256") or run.get("scenario")=="EmpireConflict" and execution.get("earnedConflictCampaigns") is not True):
            errors.append("Native execution/source/assembly linkage failed.")
        if normal is not None and any(normal.get(key) != [] for key in ("presentScenarioTypes", "presentHelpers", "validationNamespaceTypes")):
            errors.append("Ordinary assembly still contains validation code.")
        if negative is not None and (len(negative.get("presentScenarioTypes", [])) != 12 or len(negative.get("presentHelpers", [])) != 2):
            errors.append("Real opt-in negative metadata control is incomplete.")
        if log is not None and re.search(rb"(?m)^(?:ERROR:|SCRIPT ERROR:|WARNING:)", log):
            errors.append("Native engine diagnostics are present.")
    earned = summary.get("earnedCampaigns", [])
    if len(earned) != 4 or {c.get("species") for c in earned} != {"harl", "peldra", "sulan", "wenth"}:
        errors.append("Four earned species campaigns are missing.")
    for campaign in earned:
        if not campaign.get("passed") or campaign.get("injectedAssets") is not False or not campaign.get("paidBothMilitaries") or not campaign.get("mainSaves") or not campaign.get("expiryWitnessed"):
            errors.append("Campaign lacks genuine paid/Main/expiry evidence: " + str(campaign.get("species")))
        stages = campaign.get("stages", [])
        if len(stages) != 7 or {s.get("name") for s in stages} != STAGES or any(s.get("equalFutureBoundaries", 0) < 3 or not s.get("mainSave") for s in stages):
            errors.append("Seven Main stages and three equal futures are missing: " + str(campaign.get("species")))
        proof = artifact(campaign.get("evidence"), "earned Main campaign " + str(campaign.get("species")))
        if proof is not None and (proof.get("passed") is not True or proof.get("method") != "Actual Main New Game; real conflict controls and Ready; actual Main Save/Load" or proof.get("species") != campaign.get("species") or proof.get("injectedAssets") is not False or proof.get("mainSaves") is not True or proof.get("expiryWitnessed") is not True or proof.get("verifiedStages") is None or set(proof["verifiedStages"]) != STAGES):
            errors.append("Earned Main campaign assertions are incomplete.")
        conflict_sources={r.get("source",{}).get("sha256") for r in native if r.get("scenario")=="EmpireConflict" and isinstance(r.get("source"),dict)}
        if proof is not None and proof.get("sourceSha256") not in conflict_sources:
            errors.append("Earned Main campaign source does not match its native run.")
        for stage in stages:
            screenshot=artifact(stage.get("screenshot"), "actual Main stage screenshot", False)
            if screenshot is not None and not screenshot.startswith(b"\x89PNG\r\n\x1a\n"):
                errors.append("Main stage screenshot is not a PNG.")
            save = artifact(stage.get("mainSave"), "actual Main save " + str(stage.get("name")))
            future = artifact(stage.get("futureEvidence"), "uninterrupted versus loaded futures")
            if save is not None and (save.get("schema_version") != 13 or save.get("rules_version", save.get("state", {}).get("rules_version")) != 12):
                errors.append("Main save has the wrong writer/rules identity.")
            main_reference=stage.get("mainSave") if isinstance(stage.get("mainSave"),dict) else {}
            if future is not None and (future.get("stage") != stage.get("name") or future.get("equalFutureBoundaries", 0) < 3 or len(future.get("hashes", [])) < 3 or future.get("mainSaveSha256") != main_reference.get("sha256")):
                errors.append("Main save and future proof do not match.")
            if save is not None and future is not None:
                command=["dotnet","run","--project",str(root/"tools/ConflictCampaignProbe/ConflictCampaignProbe.csproj"),"-c","Release","--","--verify-save-proof",str(root/main_reference["path"]),str(root/stage["futureEvidence"]["path"])]
                try:
                    subprocess.run(command,cwd=root,check=True,capture_output=True,text=True,timeout=180)
                except (OSError,subprocess.CalledProcessError,subprocess.TimeoutExpired):
                    errors.append("Authoritative save load or recorded future replay failed: " + str(stage.get("name")))
    return errors


class NegativeControls(unittest.TestCase):
    def setUp(self):
        # Synthetic fixtures have an explicit trusted inventory; no external Git
        # process or real checkout is needed to test rejection semantics.
        original=globals()["tracked_sources"]
        globals()["tracked_sources"]=lambda root: {"required.cs"}
        self.addCleanup(lambda: globals().__setitem__("tracked_sources",original))
    def complete_looking(self, root):
        (root / "source.cs").write_text("real unrelated source", encoding="utf-8")
        artifact={"path":"source.cs","sha256":hashlib.sha256((root / "source.cs").read_bytes()).hexdigest()}
        return {"status":"Verified","unresolvedRequiredCriteria":[],"independentImplementationReviews":1,"unresolvedMaterialFindings":[],"defaultRules":12,"writerSchema":13,"supportedPairs":79,"productSource":{"files":[artifact]},"criteria":[{"id":f"ECF-AC-{n:03}","status":"Verified","evidence":[artifact]} for n in range(1,13)],"nativeMatrix":[{"scenario":name,"exitCode":0,"diagnostics":[],"method":"native","source":"stale-source","evidence":"does-not-exist.json"} for name in SCENARIOS],"earnedCampaigns":[{"species":species,"passed":True,"injectedAssets":False,"paidBothMilitaries":True,"mainSaves":True,"expiryWitnessed":True,"stages":[{"name":name,"equalFutureBoundaries":3,"mainSave":"missing-main-save.json"} for name in STAGES]} for species in ("harl","peldra","sulan","wenth")]}

    def test_complete_looking_nonexistent_acceptance_artifacts_fail(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory)
            self.assertTrue(verify(self.complete_looking(root),root),"Truthful-looking metadata is not actual native/Main evidence.")

    def test_source_subset_cannot_claim_complete_product(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory)
            globals()["tracked_sources"]=lambda root: {"source.cs","required.cs"}
            self.assertTrue(any("source coverage" in error for error in verify(self.complete_looking(root),root)))

    def test_missing_summary_fails(self):
        self.assertTrue(verify({}, Path.cwd()))

    def test_unresolved_cannot_pass(self):
        errors = verify({"status": "Verified", "unresolvedRequiredCriteria": ["native"]}, Path.cwd())
        self.assertTrue(any("unresolved" in e for e in errors))

    def test_actual_source_mismatch_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "source.cs").write_text("actual", encoding="utf-8")
            errors = verify({"productSource": {"files": [{"path": "source.cs", "sha256": "0" * 64}]}}, root)
            self.assertIn("Product source mismatch: source.cs", errors)

    def test_engine_rehearsal_is_not_native(self):
        errors = verify({"nativeMatrix": [{"scenario": "EmpireConflict", "exitCode": 0, "diagnostics": [], "method": "engine-free"}]}, Path.cwd())
        self.assertIn("Native run is incomplete: EmpireConflict", errors)

    def test_injected_campaign_is_not_earned(self):
        errors = verify({"earnedCampaigns": [{"species": "harl", "passed": True, "injectedAssets": True}]}, Path.cwd())
        self.assertTrue(any("genuine paid" in e for e in errors))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("summary", nargs="?")
    parser.add_argument("--root", default=".")
    parser.add_argument("--self-test", action="store_true")
    parser.add_argument("--capture-source")
    parser.add_argument("--assembly")
    args = parser.parse_args()
    if args.self_test:
        unittest.main(argv=[__file__])
    if args.capture_source:
        if not args.assembly:
            parser.error("--assembly is required for native source capture")
        print(json.dumps(capture_source(Path(args.root),Path(args.capture_source),Path(args.assembly)),indent=2))
        raise SystemExit(0)
    if not args.summary:
        parser.error("summary path is required")
    problems = verify(json.loads(Path(args.summary).read_text(encoding="utf-8-sig")), Path(args.root))
    print(json.dumps({"passed": not problems, "errors": problems}, indent=2))
    raise SystemExit(1 if problems else 0)
