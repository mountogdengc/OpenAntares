$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $root
$base='reports/conflict-ui-integration-20261104'
python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 900 -- pwsh -NoProfile -File tools/Run-LayoutTrialChecks.ps1 -OutputDirectory "$base/layout01"
if($LASTEXITCODE -ne 0){throw 'Layout controls failed'}
$records=@()
foreach($scenario in @('EmpireConflict','Legacy','Expansion','Species','Rival','Colony','Crisis','Scout','Fleet','Combat','ShipDesign','Diplomacy')){
  $folder="$base/attempt01-$scenario"
  try { & ./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' -Scenario $scenario -OutputDirectory $folder; $code=$LASTEXITCODE } catch { Write-Output $_; $code=1 }
  $records+=@{scenario=$scenario;exitCode=$code;folder=$folder}
  $records|ConvertTo-Json -Depth 4|Set-Content "$base/attempt01-matrix.json"
}
