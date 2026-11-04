param(
 [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9_-]+$')][string]$RunName,
 [string]$OutputRoot='reports/conflict-ui-integration-20261104',
 [string]$Scenarios='Legacy,Expansion,Species,Rival,Colony,Crisis,Scout,Fleet,Combat,ShipDesign,Diplomacy,EmpireConflict',
 [switch]$EarnedConflictCampaigns
)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location -LiteralPath $root
$godot=(Resolve-Path -LiteralPath 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe').Path
$targets=Join-Path $root 'tests/OpenAntares.Game.Validation/Validation.targets'
$assembly=Join-Path $root '.godot/mono/temp/bin/Debug/OpenAntares.dll'
$scenes=@{
 Legacy='GraphicalValidation';Expansion='ExpansionGraphicalValidation';Species='SpeciesGraphicalValidation';Rival='RivalGraphicalValidation';Colony='ColonyGraphicalValidation';Crisis='CrisisGraphicalValidation';Scout='ScoutGraphicalValidation';Fleet='FleetGraphicalValidation';Combat='CombatGraphicalValidation';ShipDesign='ShipDesignGraphicalValidation';Diplomacy='DiplomacyGraphicalValidation';EmpireConflict='EmpireConflictGraphicalValidation'
}
$selected=@($Scenarios.Split(','))
if(@($selected|Select-Object -Unique).Count -ne $selected.Count -or @($selected|Where-Object{!$scenes.ContainsKey($_)}).Count -gt 0){throw 'Scenarios must be unique supported native cases'}
$base=[IO.Path]::GetFullPath((Join-Path $root $OutputRoot))
if(!$base.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Evidence must stay inside the active checkout'}
$control=Join-Path $base ($RunName+'-control')
foreach($folder in @($control)+@($selected|ForEach-Object{Join-Path $base ($RunName+'-'+$_)})){if(Test-Path -LiteralPath $folder){throw 'Use fresh output names; never overwrite prior evidence'}}
New-Item -ItemType Directory -Path $control -Force | Out-Null
$records=[Collections.Generic.List[object]]::new()
$overall=1;$normalOk=$false;$failure=$null
try {
 python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 600 -- dotnet build OpenAntares.csproj --no-restore --nologo --disable-build-servers -t:Rebuild "-p:CustomAfterMicrosoftCommonTargets=$targets" *> (Join-Path $control 'opt-in-build-queue.log')
 if($LASTEXITCODE -ne 0){throw 'Queued matrix opt-in build failed'}
 $compiledHash=(Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash.ToLowerInvariant()
 foreach($scenario in $selected){
  $output=Join-Path $base ($RunName+'-'+$scenario)
  New-Item -ItemType Directory -Path $output | Out-Null
  $record=[ordered]@{scenario=$scenario;folder=[IO.Path]::GetRelativePath($root,$output).Replace('\','/');exitCode=$null;error=$null;sourceStable=$false;diagnostics=@();earnedConflictCampaigns=($EarnedConflictCampaigns -and $scenario -eq 'EmpireConflict');assemblyStable=$false;assertionsPassed=$false}
  $records.Add($record)
  try {
   if((Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash.ToLowerInvariant() -ne $compiledHash){throw 'Another build changed the matrix assembly'}
   & ./tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath $assembly -ReportPath (Join-Path $output 'opt-in-metadata.json') *> (Join-Path $output 'opt-in-metadata.log')
   if($LASTEXITCODE -ne 1){throw 'Actual opt-in negative control failed'}
   $negative=Get-Content -LiteralPath (Join-Path $output 'opt-in-metadata.json') -Raw|ConvertFrom-Json
   if(@($negative.presentScenarioTypes).Count -ne 12 -or @($negative.presentHelpers).Count -ne 2){throw 'Actual twelve-case opt-in inventory is incomplete'}
   python tools/ConflictValidation/Verify-Evidence.py --capture-source (Join-Path $output 'source.json') --assembly $assembly *> (Join-Path $output 'source-capture.log')
   if($LASTEXITCODE -ne 0){throw 'Committed source and assembly capture failed'}
   $frameLimit=if($scenario -in @('Expansion','Rival','Combat','ShipDesign','EmpireConflict')){72000}else{7200}
   $arguments=@("--validation-output=$output")
   if($record.earnedConflictCampaigns){$arguments+='--validation-earned-conflict'}
   Write-Output "Native matrix scenario: $scenario"
   & $godot --path $root --quit-after $frameLimit --scene ('res://tests/OpenAntares.Game.Validation/'+$scenes[$scenario]+'.tscn') --log-file (Join-Path $output 'godot.log') -- @arguments 2>&1|Tee-Object -FilePath (Join-Path $output 'console.log')
   $record.exitCode=$LASTEXITCODE
   $record.assemblyStable=(Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash.ToLowerInvariant() -eq $compiledHash
   if($record.exitCode -ne 0){throw "Native scene exited $($record.exitCode)"}
   if(!$record.assemblyStable){throw 'Opt-in assembly changed during native execution'}
   $result=Get-Content -LiteralPath (Join-Path $output 'results.json') -Raw|ConvertFrom-Json
   $record.assertionsPassed=$result.passed -eq $true -and $null -eq $result.error -and $result.scenario -eq $scenario -and @($result.checks).Count -gt 0 -and @($result.checks|Where-Object{$_.passed -ne $true}).Count -eq 0
   if(!$record.assertionsPassed){throw 'Native assertions are incomplete or failed'}
  }catch{$record.error=$_.ToString();Write-Output $_}
  finally{
   $diagnostics=@()
   foreach($log in @('godot.log','console.log')){
    $path=Join-Path $output $log
    if(Test-Path -LiteralPath $path){$diagnostics+=@(Get-Content -LiteralPath $path|Where-Object{$_ -match '^\s*(ERROR:|SCRIPT ERROR:|WARNING:)'})}else{$diagnostics+="Missing $log"}
   }
   if(!$record.assemblyStable){$diagnostics+='Matrix opt-in assembly identity was not stable'}
   $record.diagnostics=@($diagnostics)
   # Retain observed runtime outcomes before restoration; this is not a passing acceptance record.
   $record|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $output 'native-process.json') -Encoding utf8
  }
 }
 $overall=0
}catch{$failure=$_.ToString();Write-Output $_}
finally{
 # One matrix is one opt-in session. Always restore ordinary code before accepting any case.
 python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 600 -- dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild *> (Join-Path $control 'normal-build-queue.log')
 if($LASTEXITCODE -eq 0){
  & ./tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath $assembly -ReportPath (Join-Path $control 'normal-metadata.json') *> (Join-Path $control 'normal-metadata.log')
  $normalOk=$LASTEXITCODE -eq 0
 }
 if(!$normalOk){$overall=1}
 foreach($record in $records){
  $output=Join-Path $root $record.folder
  $sourcePath=Join-Path $output 'source.json'
  if(Test-Path -LiteralPath $sourcePath){
   $source=Get-Content -LiteralPath $sourcePath -Raw|ConvertFrom-Json
   $record.sourceStable=@($source.files|Where-Object{!(Test-Path -LiteralPath (Join-Path $root $_.path)) -or (Get-FileHash -LiteralPath (Join-Path $root $_.path) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $_.sha256}).Count -eq 0
   [ordered]@{scenario=$record.scenario;earnedConflictCampaigns=$record.earnedConflictCampaigns;exitCode=$record.exitCode;diagnostics=@($record.diagnostics);sourceStable=$record.sourceStable;sourceSha256=(Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant();assemblySha256=$source.assembly.sha256;ordinaryRestoredAfterMatrix=$normalOk}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $output 'execution.json') -Encoding utf8
  }
  if($normalOk){
   Copy-Item -LiteralPath (Join-Path $control 'normal-metadata.json') -Destination (Join-Path $output 'normal-metadata.json')
   Copy-Item -LiteralPath (Join-Path $control 'normal-metadata.log') -Destination (Join-Path $output 'normal-metadata.log')
  }
  if($record.exitCode -ne 0 -or !$record.assertionsPassed -or !$record.sourceStable -or $record.diagnostics.Count -gt 0 -or $record.error){$overall=1}
 }
 if($records.Count -ne $selected.Count){$overall=1}
 [ordered]@{passed=($overall -eq 0);sourceRoot=$root;method='One queued opt-in build, separate actual graphical processes with per-case source/assembly/log evidence, mandatory queued ordinary restoration after the complete matrix';normalRestored=$normalOk;error=$failure;scenarios=$records.ToArray()}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $base ($RunName+'-matrix.json')) -Encoding utf8
}
exit $overall
