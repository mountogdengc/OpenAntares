param(
    [Parameter(Mandatory = $true)][string]$Godot,
    [string]$OutputDirectory = 'reports/first-playable-2026-10-22',
    [ValidateSet('Legacy','Expansion','Species','Rival','Colony','Crisis','Scout','Fleet')][string]$Scenario = 'Legacy'
)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$godotPath = (Resolve-Path -LiteralPath $Godot).Path
$targetsPath = Join-Path $PSScriptRoot 'Validation.targets'
Push-Location -LiteralPath $repoRoot
try {
    $outputPath = [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
    if (-not $outputPath.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Validation output must be inside this repository.'
    }
    if (Test-Path -LiteralPath (Join-Path $outputPath 'results.json')) {
        throw 'This output directory already has a run record. Choose another output directory.'
    }
    New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
    try {
        & dotnet build OpenAntares.csproj --no-restore --nologo --disable-build-servers "-p:CustomAfterMicrosoftCommonTargets=$targetsPath"
        if ($LASTEXITCODE -ne 0) { throw 'Validation build failed.' }
        & (Join-Path $PSScriptRoot 'Verify-NormalAssembly.ps1') -AssemblyPath '.godot/mono/temp/bin/Debug/OpenAntares.dll' -ReportPath (Join-Path $outputPath 'opt-in-metadata.json') *> (Join-Path $outputPath 'opt-in-metadata.log')
        if ($LASTEXITCODE -ne 1) { throw 'Metadata negative control did not detect the real opt-in assembly.' }
        $scene = switch ($Scenario) { 'Fleet' { 'res://tests/OpenAntares.Game.Validation/FleetGraphicalValidation.tscn' } 'Scout' { 'res://tests/OpenAntares.Game.Validation/ScoutGraphicalValidation.tscn' } 'Crisis' { 'res://tests/OpenAntares.Game.Validation/CrisisGraphicalValidation.tscn' } 'Colony' { 'res://tests/OpenAntares.Game.Validation/ColonyGraphicalValidation.tscn' } 'Rival' { 'res://tests/OpenAntares.Game.Validation/RivalGraphicalValidation.tscn' } 'Expansion' { 'res://tests/OpenAntares.Game.Validation/ExpansionGraphicalValidation.tscn' } 'Species' { 'res://tests/OpenAntares.Game.Validation/SpeciesGraphicalValidation.tscn' } default { 'res://tests/OpenAntares.Game.Validation/GraphicalValidation.tscn' } }
        & $godotPath --path $repoRoot --quit-after 7200 --scene $scene --log-file (Join-Path $outputPath 'godot.log') -- "--validation-output=$outputPath"
        if ($LASTEXITCODE -ne 0) { throw "Graphical validation failed with exit $LASTEXITCODE." }
        if (-not (Test-Path -LiteralPath (Join-Path $outputPath 'results.json'))) { throw 'No validation results were written.' }
    }
    finally {
        # Remove the opt-in validation class from the ordinary game assembly, even after failure.
        & dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild
        if ($LASTEXITCODE -ne 0) { throw 'Normal game rebuild failed; rebuild the solution before playing.' }
        & (Join-Path $PSScriptRoot 'Verify-NormalAssembly.ps1') -AssemblyPath '.godot/mono/temp/bin/Debug/OpenAntares.dll' -ReportPath (Join-Path $outputPath 'normal-metadata.json') *> (Join-Path $outputPath 'normal-metadata.log')
        if ($LASTEXITCODE -ne 0) { throw 'Normal assembly contains graphical validation types.' }
    }
}
finally { Pop-Location }
