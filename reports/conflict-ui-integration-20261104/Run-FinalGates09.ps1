$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $root
$base='reports/conflict-ui-integration-20261104'
$gates=[ordered]@{source=(& git rev-parse HEAD);layout="$base/layout-final09";cleanup="$base/cleanup-final09"}
function Save-Gates { $gates|ConvertTo-Json -Depth 7|Set-Content -LiteralPath "$base/final-gates09.json" }
function Queue-JobFromLog([string]$path) {
 $found=$null
 foreach($line in Get-Content -LiteralPath $path){try{$item=$line|ConvertFrom-Json;if($item.results){$found=Split-Path -Leaf $item.results}}catch{}}
 if(!$found){throw "Missing queue job record in $path"}
 return $found
}
function Native-Suite([string]$suite) {
 $before=@(Get-ChildItem reports/native-tests -Directory|ForEach-Object{$_.FullName})
 $log="$base/final09-$suite-queue.log"
 if($suite -eq 'build'){& 'F:/Projects-PCGames/OpenAntares/tools/Test-Queued.ps1' -SourceRoot $root -Suite build 2>&1|Tee-Object -FilePath $log}
 else{& 'F:/Projects-PCGames/OpenAntares/tools/Test-Queued.ps1' -SourceRoot $root 2>&1|Tee-Object -FilePath $log}
 if($LASTEXITCODE -ne 0){throw "Native $suite gate failed"}
 $new=@(Get-ChildItem reports/native-tests -Directory|Where-Object{$_.FullName -notin $before})
 if($new.Count -ne 1){throw "Expected exactly one fresh $suite run"}
 $result=Get-Content -LiteralPath (Join-Path $new[0].FullName 'result.json') -Raw|ConvertFrom-Json
 if(!$result.passed -or $result.sourceRoot -ne $root){throw "Native $suite scope/result mismatch"}
 $gates[$suite]=@{folder=[IO.Path]::GetRelativePath($root,$new[0].FullName).Replace('\','/');queueJob=(Queue-JobFromLog $log);passed=$true}
 Save-Gates
}
Save-Gates
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 900 -- pwsh -NoProfile -File tools/Run-LayoutTrialChecks.ps1 -OutputDirectory $gates.layout 2>&1|Tee-Object -FilePath "$base/final09-layout-queue.log"
if($LASTEXITCODE -ne 0){throw 'Final layout controls failed'}
$gates.layoutJob=Queue-JobFromLog "$base/final09-layout-queue.log";Save-Gates
Native-Suite build
Native-Suite default
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 900 -- pwsh -NoProfile -File "$base/Run-Cleanup.ps1" -OutputDirectory $gates.cleanup 2>&1|Tee-Object -FilePath "$base/final09-cleanup-queue.log"
if($LASTEXITCODE -ne 0){throw 'Final legacy badge cleanup failed'}
$gates.cleanupJob=Queue-JobFromLog "$base/final09-cleanup-queue.log";Save-Gates
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 600 -- python tools/ConflictValidation/Verify-Evidence.py --self-test 2>&1|Tee-Object -FilePath "$base/final09-verifier-self-tests-queue.log"
if($LASTEXITCODE -ne 0){throw 'Verifier negative controls failed'}
$gates.verifierSelfTestsJob=Queue-JobFromLog "$base/final09-verifier-self-tests-queue.log";Save-Gates
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 900 -- python "$base/Build-Summary.py" --suite $gates.default.folder --suite-job $gates.default.queueJob --layout $gates.layout --rival-budget-equivalence 2>&1|Tee-Object -FilePath "$base/final09-summary-queue.log"
if($LASTEXITCODE -ne 0){throw 'Final combined summary rejected incomplete/current-source evidence'}
$gates.summaryJob=Queue-JobFromLog "$base/final09-summary-queue.log";Save-Gates
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 1800 -- python tools/ConflictValidation/Verify-Evidence.py "$base/final-summary.json" --root . 2>&1|Tee-Object -FilePath "$base/final09-evidence-replay-queue.log"
if($LASTEXITCODE -ne 0){throw 'Final authoritative evidence/source/Main-save replay failed'}
$gates.evidenceReplayJob=Queue-JobFromLog "$base/final09-evidence-replay-queue.log";$gates.passed=$true;Save-Gates
