param([Parameter(Mandatory=$true)][string]$RunName,[string]$Scenarios='Rival,Legacy,Expansion,Species,Colony,Crisis,Scout,Fleet,Combat,ShipDesign,Diplomacy,EmpireConflict',[switch]$Earned,[switch]$ContinueOnFailure)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $root
$base='reports/conflict-ui-integration-20261104'
$records=@()
foreach($scenario in $Scenarios.Split(',')) {
 $folder="$base/$RunName-$scenario"
 $options=@{Godot='F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe';Scenario=$scenario;OutputDirectory=$folder}
 if($Earned -and $scenario -eq 'EmpireConflict'){$options.EarnedConflictCampaigns=$true}
 try { & ./tests/OpenAntares.Game.Validation/Run-Validation.ps1 @options; $code=$LASTEXITCODE } catch { Write-Output $_; $code=1 }
 $records+=@{scenario=$scenario;exitCode=$code;folder=$folder}
 ConvertTo-Json -InputObject @($records) -Depth 5 | Set-Content "$base/$RunName-matrix.json"
 if($code -ne 0 -and !$ContinueOnFailure){break}
}
if(@($records|Where-Object{$_.exitCode -ne 0}).Count -gt 0){exit 1}
