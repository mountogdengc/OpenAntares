# Queue-owned foreground job. Invoke only through runner.py exec; no nested queue acquisition.
param(
 [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9_-]+$')][string]$RunName,
 [ValidateSet('MultiFleet','FleetPanel','PlaytestFeedback','PopulationForecast','ReviewCorrections','PlayerFlow','Baseline','Theme','CommandBar','Status','Console','Fleets','Workspaces','Ledger','Map','Copy','Contrast','All')][string]$Case='All',
 [switch]$Development
)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location -LiteralPath $root
$godot='F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
$assembly=Join-Path $root '.godot/mono/temp/bin/Debug/OpenAntares.dll'
$targets=Join-Path $root 'tests/OpenAntares.Game.Validation/VisualOverhaulValidation.targets'
$base=Join-Path $root 'reports/visual-overhaul-execution'
$control=Join-Path $base ($RunName+'-control')
$selected=if($Case -eq 'All'){@('Theme','CommandBar','Status','Console','Fleets','Workspaces','Ledger','Map','Copy','Contrast')}else{@($Case)}
foreach($folder in @($control)+@($selected|ForEach-Object{Join-Path $base ($RunName+'-'+$_)})) {
 if(Test-Path -LiteralPath $folder){throw 'Retain prior evidence; choose a fresh RunName'}
}
New-Item -ItemType Directory -Path $control | Out-Null
$records=[Collections.Generic.List[object]]::new()
$failure=$null;$normalOk=$false
function Artifact([string]$file) {
 return @{path=[IO.Path]::GetRelativePath($root,$file).Replace('\','/');sha256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()}
}
try {
 & ./tools/Check-NoStylingOverrides.ps1 -SourceRoot $root -ReportPath (Join-Path $control 'style-guard.json') *> (Join-Path $control 'style-guard.log')
 if($LASTEXITCODE -ne 0){throw 'Styling boundary failed before native build'}
 dotnet build OpenAntares.csproj --no-restore --nologo --disable-build-servers -t:Rebuild "-p:CustomAfterMicrosoftCommonTargets=$targets" *> (Join-Path $control 'opt-in-build-queue.log')
 if($LASTEXITCODE -ne 0){throw 'Queued M14 opt-in build failed'}
 $compiled=(Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash.ToLowerInvariant()
 foreach($name in $selected) {
  $output=Join-Path $base ($RunName+'-'+$name)
  New-Item -ItemType Directory -Path $output | Out-Null
  $record=[ordered]@{case=$name;scope=if($Development){'Development'}else{'Recorded case'};exitCode=$null;assemblyStable=$false;normalRestored=$false;error=$null}
  $record.styleGuard=Artifact (Join-Path $control 'style-guard.json')
  $record.baselineAllowance=Artifact (Join-Path $base 'approval-20261108/styling-allowlist.json')
  $records.Add([pscustomobject]@{folder=$output;record=$record})
  try {
   if((Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash.ToLowerInvariant() -ne $compiled){throw 'Another build changed this M14 assembly'}
   & ./tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath $assembly -ReportPath (Join-Path $output 'opt-in-metadata.json') *> (Join-Path $output 'opt-in-metadata.log')
   if($LASTEXITCODE -ne 1){throw 'M14 opt-in negative metadata control failed'}
   $opt=Get-Content -LiteralPath (Join-Path $output 'opt-in-metadata.json') -Raw|ConvertFrom-Json
   if(@($opt.presentScenarioTypes).Count -ne 1 -or $opt.presentScenarioTypes[0] -ne 'VisualOverhaulValidation'){throw 'Wrong opt-in scenario inventory; common twelve-case target must remain separate'}
   $capture=@('-B','tools/ConflictValidation/Verify-VisualOverhaulEvidence.py','--capture-source',(Join-Path $output 'source.json'),'--assembly',$assembly,'--root',$root)
   if($Development){$capture+='--development'}
   python @capture *> (Join-Path $output 'source-capture.log')
   if($LASTEXITCODE -ne 0){throw 'Source/DLL capture failed; uncommitted development is not final acceptance'}
   $record.source=Get-Content -LiteralPath (Join-Path $output 'source.json') -Raw|ConvertFrom-Json
   $record.assembly=Artifact (Join-Path $output 'validation-assembly.dll')
   $record.optInMetadata=Artifact (Join-Path $output 'opt-in-metadata.json')
   $frames=72000
   Write-Output "Native M14 $name ($($record.scope))"
   & $godot --path $root --quit-after $frames --scene res://tests/OpenAntares.Game.Validation/VisualOverhaulValidation.tscn --log-file (Join-Path $output 'godot.log') -- "--validation-output=$output" "--visual-case=$name" 2>&1|Tee-Object -FilePath (Join-Path $output 'console.log')
   $record.exitCode=$LASTEXITCODE
   $record.assemblyStable=(Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash.ToLowerInvariant() -eq $compiled
   if($record.exitCode -ne 0){throw "Native M14 $name exited $($record.exitCode)"}
   if(!$record.assemblyStable){throw 'M14 assembly changed during native execution'}
   $record.result=Artifact (Join-Path $output 'results.json')
  } catch {$record.error=$_.ToString();Write-Output $_}
  finally {
   foreach($entry in @(@('engineLog','godot.log'),@('consoleLog','console.log'))) {
    $file=Join-Path $output $entry[1]
    if(Test-Path -LiteralPath $file){$record[$entry[0]]=Artifact $file}
   }
   $record|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $output 'native-process.json') -Encoding utf8
  }
 }
} catch {$failure=$_.ToString();Write-Output $_}
finally {
 # Never accept a native case until ordinary code has been rebuilt and inspected.
 dotnet build OpenAntares.csproj --no-restore --nologo --disable-build-servers -t:Rebuild *> (Join-Path $control 'normal-build-queue.log')
 if($LASTEXITCODE -eq 0) {
  & ./tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath $assembly -ReportPath (Join-Path $control 'normal-metadata.json') *> (Join-Path $control 'normal-metadata.log')
  $normalOk=$LASTEXITCODE -eq 0
 }
 $passed=$null -eq $failure -and $normalOk -and $records.Count -eq $selected.Count
 foreach($entry in $records) {
  $r=$entry.record;$out=$entry.folder;$r.normalRestored=$normalOk
  if(Test-Path -LiteralPath (Join-Path $control 'normal-metadata.json')) {
   Copy-Item -LiteralPath (Join-Path $control 'normal-metadata.json') -Destination (Join-Path $out 'normal-metadata.json')
   $r.normalMetadata=Artifact (Join-Path $out 'normal-metadata.json')
  }
  $r|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $out 'execution.json') -Encoding utf8
  $verify=@('-B','tools/ConflictValidation/Verify-VisualOverhaulEvidence.py',(Join-Path $out 'execution.json'),'--root',$root)
  if($Development){$verify+='--development'}
  python @verify *> (Join-Path $out 'verification.json')
  if($LASTEXITCODE -ne 0 -or $null -ne $r.error){$passed=$false}
 }
 @{passed=$passed;scope=if($Development){'Development only; not milestone acceptance'}else{'Selected recorded cases; milestone coverage remains separate'};accepted=($passed -and !$Development);selectedCases=$selected;normalRestored=$normalOk;error=$failure;records=@($records|ForEach-Object{$_.record})}|ConvertTo-Json -Depth 14|Set-Content -LiteralPath (Join-Path $base ($RunName+'-summary.json')) -Encoding utf8
}
if($Case -eq 'All' -and $passed) {
 $suiteArgs=@('-B','tools/ConflictValidation/Verify-VisualOverhaulEvidence.py',(Join-Path $base ($RunName+'-summary.json')),'--root',$root,'--suite')
 if($Development){$suiteArgs+='--development'}
 python @suiteArgs *> (Join-Path $base ($RunName+'-suite-verification.json'))
 if($LASTEXITCODE -ne 0){$passed=$false}
}
if(!$passed){throw 'M14 selected checks failed or evidence is incomplete; inspect retained records'}
Write-Output "M14 selected checks passed; development=$Development, ordinaryRestored=$normalOk. Full milestone acceptance is separate."
