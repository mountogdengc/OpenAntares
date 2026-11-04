$ErrorActionPreference='Stop'
$root='F:/Projects-PCGames/OpenAntares'
Set-Location $root
$base='reports/conflict-ui-integration-20261104/postmerge'
$gates=[ordered]@{mainMerge=(& git rev-parse HEAD);sourceRoot=$root}
function Save-Gates {$gates|ConvertTo-Json -Depth 6|Set-Content -LiteralPath "$base/gates.json"}
function QueueId([string]$path) {
 $id=$null
 foreach($line in Get-Content -LiteralPath $path){try{$record=$line|ConvertFrom-Json;if($record.results){$id=Split-Path -Leaf $record.results}}catch{}}
 if(!$id){throw "Missing queue identity in $path"};return $id
}
Save-Gates
foreach($suite in @('build','default')) {
 $before=@(Get-ChildItem reports/native-tests -Directory|ForEach-Object{$_.FullName})
 if($suite -eq 'build'){& ./tools/Test-Queued.ps1 -SourceRoot $root -Suite build 2>&1|Tee-Object -FilePath "$base/$suite-queue.log"}
 else{& ./tools/Test-Queued.ps1 -SourceRoot $root 2>&1|Tee-Object -FilePath "$base/$suite-queue.log"}
 if($LASTEXITCODE -ne 0){throw "Postmerge $suite failed"}
 $fresh=@(Get-ChildItem reports/native-tests -Directory|Where-Object{$_.FullName -notin $before})
 if($fresh.Count -ne 1){throw 'Expected one new actual-main result'}
 $result=Get-Content -LiteralPath (Join-Path $fresh[0].FullName 'result.json') -Raw|ConvertFrom-Json
 if(!$result.passed -or $result.sourceRoot.Replace('\','/') -ne $root){throw 'Main result/source mismatch'}
 $gates[$suite]=@{passed=$true;folder=[IO.Path]::GetRelativePath($root,$fresh[0].FullName).Replace('\','/');queueJob=(QueueId "$base/$suite-queue.log")};Save-Gates
}
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 600 -- pwsh -NoProfile -File tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath .godot/mono/temp/bin/Debug/OpenAntares.dll -ReportPath "$base/normal-metadata.json" 2>&1|Tee-Object -FilePath "$base/normal-queue.log"
if($LASTEXITCODE -ne 0){throw 'Main ordinary assembly contains validation code'}
$gates.normalQueueJob=QueueId "$base/normal-queue.log";$gates.passed=$true;Save-Gates
