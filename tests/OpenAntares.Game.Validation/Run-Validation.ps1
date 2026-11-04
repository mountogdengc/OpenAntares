param(
    [Parameter(Mandatory = $true)][string]$Godot,
    [string]$OutputDirectory = 'reports/first-playable-2026-10-22',
    [ValidateSet('Legacy','Expansion','Species','Rival','Colony','Crisis','Scout','Fleet','Combat','ShipDesign','Diplomacy','EmpireConflict')][string]$Scenario = 'Legacy',
    [switch]$EarnedConflictCampaigns
)
$ErrorActionPreference = 'Stop'
if($EarnedConflictCampaigns -and $Scenario -ne 'EmpireConflict'){throw 'EarnedConflictCampaigns applies only to EmpireConflict.'}
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$godotPath = (Resolve-Path -LiteralPath $Godot).Path
$targetsPath = Join-Path $PSScriptRoot 'Validation.targets'
Push-Location -LiteralPath $repoRoot
try {
    $outputPath = [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
    if (-not $outputPath.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Validation output must be inside this repository.'
    }
    if ((Test-Path -LiteralPath $outputPath) -and @(Get-ChildItem -LiteralPath $outputPath -Force).Count -gt 0) {
        throw 'This output directory already contains evidence. Choose a fresh output directory.'
    }
    New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
    $nativeExit=$null
    $sourcePath=Join-Path $outputPath 'source.json'
    try {
        python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 600 -- dotnet build OpenAntares.csproj --no-restore --nologo --disable-build-servers -t:Rebuild "-p:CustomAfterMicrosoftCommonTargets=$targetsPath"
        if ($LASTEXITCODE -ne 0) { throw 'Validation build failed.' }
        & (Join-Path $PSScriptRoot 'Verify-NormalAssembly.ps1') -AssemblyPath '.godot/mono/temp/bin/Debug/OpenAntares.dll' -ReportPath (Join-Path $outputPath 'opt-in-metadata.json') *> (Join-Path $outputPath 'opt-in-metadata.log')
        if ($LASTEXITCODE -ne 1) { throw 'Metadata negative control did not detect the real opt-in assembly.' }
        python tools/ConflictValidation/Verify-Evidence.py --capture-source $sourcePath --assembly '.godot/mono/temp/bin/Debug/OpenAntares.dll'
        if($LASTEXITCODE -ne 0){throw 'Native source/assembly capture failed.'}
        $scene = switch ($Scenario) { 'EmpireConflict' { 'res://tests/OpenAntares.Game.Validation/EmpireConflictGraphicalValidation.tscn' } 'Diplomacy' { 'res://tests/OpenAntares.Game.Validation/DiplomacyGraphicalValidation.tscn' } 'ShipDesign' { 'res://tests/OpenAntares.Game.Validation/ShipDesignGraphicalValidation.tscn' } 'Combat' { 'res://tests/OpenAntares.Game.Validation/CombatGraphicalValidation.tscn' } 'Fleet' { 'res://tests/OpenAntares.Game.Validation/FleetGraphicalValidation.tscn' } 'Scout' { 'res://tests/OpenAntares.Game.Validation/ScoutGraphicalValidation.tscn' } 'Crisis' { 'res://tests/OpenAntares.Game.Validation/CrisisGraphicalValidation.tscn' } 'Colony' { 'res://tests/OpenAntares.Game.Validation/ColonyGraphicalValidation.tscn' } 'Rival' { 'res://tests/OpenAntares.Game.Validation/RivalGraphicalValidation.tscn' } 'Expansion' { 'res://tests/OpenAntares.Game.Validation/ExpansionGraphicalValidation.tscn' } 'Species' { 'res://tests/OpenAntares.Game.Validation/SpeciesGraphicalValidation.tscn' } default { 'res://tests/OpenAntares.Game.Validation/GraphicalValidation.tscn' } }
                $frameLimit=if($Scenario -in @('Expansion','Combat','ShipDesign','EmpireConflict')){72000}else{7200}
        $validationArguments=@("--validation-output=$outputPath")
        if($EarnedConflictCampaigns){$validationArguments+='--validation-earned-conflict'}
        & $godotPath --path $repoRoot --quit-after $frameLimit --scene $scene --log-file (Join-Path $outputPath 'godot.log') -- @validationArguments 2>&1 | Tee-Object -FilePath (Join-Path $outputPath 'console.log')
        $nativeExit=$LASTEXITCODE
        if ($LASTEXITCODE -ne 0) { throw "Graphical validation failed with exit $LASTEXITCODE." }
        if (-not (Test-Path -LiteralPath (Join-Path $outputPath 'results.json'))) { throw 'No validation results were written.' }
    }
    finally {
        # Remove the opt-in validation class from the ordinary game assembly, even after failure.
        python F:/Tools/CodexTestRunner/runner.py exec --project openantares --timeout 600 -- dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild
        if ($LASTEXITCODE -ne 0) { throw 'Normal game rebuild failed; rebuild the solution before playing.' }
        & (Join-Path $PSScriptRoot 'Verify-NormalAssembly.ps1') -AssemblyPath '.godot/mono/temp/bin/Debug/OpenAntares.dll' -ReportPath (Join-Path $outputPath 'normal-metadata.json') *> (Join-Path $outputPath 'normal-metadata.log')
        if ($LASTEXITCODE -ne 0) { throw 'Normal assembly contains graphical validation types.' }
        if(Test-Path -LiteralPath $sourcePath){
            $source=Get-Content -LiteralPath $sourcePath -Raw | ConvertFrom-Json
            $stable=@($source.files|Where-Object{!(Test-Path -LiteralPath (Join-Path $repoRoot $_.path)) -or (Get-FileHash -LiteralPath (Join-Path $repoRoot $_.path) -Algorithm SHA256).Hash.ToLowerInvariant() -ne $_.sha256}).Count -eq 0
            $diagnostics=@()
            if(Test-Path -LiteralPath (Join-Path $outputPath 'godot.log')){$diagnostics=@(Get-Content -LiteralPath (Join-Path $outputPath 'godot.log')|Where-Object{$_ -match '^\s*(ERROR:|SCRIPT ERROR:|WARNING:)'})}else{$diagnostics=@('Engine log missing')}
            if(Test-Path -LiteralPath (Join-Path $outputPath 'console.log')){$diagnostics+=@(Get-Content -LiteralPath (Join-Path $outputPath 'console.log')|Where-Object{$_ -match '^\s*(ERROR:|SCRIPT ERROR:|WARNING:)'})}else{$diagnostics+=@('Engine console log missing')}
            [ordered]@{scenario=$Scenario;earnedConflictCampaigns=$EarnedConflictCampaigns.IsPresent;exitCode=$nativeExit;diagnostics=@($diagnostics);sourceStable=$stable;sourceSha256=(Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant();assemblySha256=$source.assembly.sha256}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $outputPath 'execution.json') -Encoding utf8
            if(!$stable){throw 'Validation source changed during execution.'}
            if($diagnostics.Count -gt 0){throw 'Engine diagnostics prevent acceptance; inspect console.log and godot.log.'}
        }
    }
}
finally { Pop-Location }
