param(
    [Parameter(Mandatory = $true)][string]$Godot,
    [string]$OutputDirectory = 'reports/first-playable-2026-10-22',
    [ValidateSet('Legacy','Expansion')][string]$Scenario = 'Legacy'
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
        $scene = if ($Scenario -eq 'Expansion') { 'res://tests/OpenAntares.Game.Validation/ExpansionGraphicalValidation.tscn' } else { 'res://tests/OpenAntares.Game.Validation/GraphicalValidation.tscn' }
        & $godotPath --path $repoRoot --quit-after 7200 --scene $scene --log-file (Join-Path $outputPath 'godot.log') -- "--validation-output=$outputPath"
        if ($LASTEXITCODE -ne 0) { throw "Graphical validation failed with exit $LASTEXITCODE." }
        if (-not (Test-Path -LiteralPath (Join-Path $outputPath 'results.json'))) { throw 'No validation results were written.' }
    }
    finally {
        # Remove the opt-in validation class from the ordinary game assembly, even after failure.
        & dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild
        if ($LASTEXITCODE -ne 0) { throw 'Normal game rebuild failed; rebuild the solution before playing.' }
    }
}
finally { Pop-Location }
