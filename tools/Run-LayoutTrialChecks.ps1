param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$trialRoot = Split-Path -Parent $PSScriptRoot
$trialDotnet = 'C:/Program Files/dotnet/dotnet.exe'
$trialGodot = 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
$trialOutput = [IO.Path]::GetFullPath((Join-Path $trialRoot $OutputDirectory))
if (-not $trialOutput.StartsWith($trialRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Evidence must be inside this checkout.' }
if (Test-Path -LiteralPath $trialOutput) { throw 'Use a new evidence directory.' }
New-Item -ItemType Directory -Path $trialOutput | Out-Null
Set-Location -LiteralPath $trialRoot
$trialFiles = @('project.godot','game/Screens/GameScreen.cs','game/Components/ColonyPanel.cs','game/Components/ResearchBar.cs','game/theme/openantares_theme.tres','tests/OpenAntares.Game.Validation/LayoutTrialValidation.cs','tools/Run-LayoutTrialChecks.ps1')
@{sourceRoot=$trialRoot;head=(& git rev-parse HEAD);files=@(foreach($trialFile in $trialFiles){@{path=$trialFile;sha256=(Get-FileHash -LiteralPath $trialFile -Algorithm SHA256).Hash}})} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $trialOutput 'source.json') -Encoding utf8
$trialExit = 1
try {
    & $trialDotnet restore OpenAntares.sln --disable-parallel *> (Join-Path $trialOutput 'restore.log')
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed; see restore.log.' }
    & $trialDotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers *> (Join-Path $trialOutput 'baseline-build.log')
    if ($LASTEXITCODE -ne 0) { throw 'Ordinary build failed.' }
    $trialTargets = Join-Path $trialRoot 'tests/OpenAntares.Game.Validation/LayoutValidation.targets'
    & $trialDotnet build OpenAntares.csproj --no-restore --nologo --disable-build-servers "-p:CustomAfterMicrosoftCommonTargets=$trialTargets" *> (Join-Path $trialOutput 'validation-build.log')
    if ($LASTEXITCODE -ne 0) { throw 'Validation build failed.' }
    & (Join-Path $trialRoot 'tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1') -AssemblyPath '.godot/mono/temp/bin/Debug/OpenAntares.dll' -ReportPath (Join-Path $trialOutput 'opt-in-metadata.json') *> (Join-Path $trialOutput 'opt-in-metadata.log')
    if ($LASTEXITCODE -ne 1) { throw 'Metadata negative control failed to detect the trial harness.' }
    & $trialGodot --headless --path $trialRoot --quit-after 7200 --scene res://tests/OpenAntares.Game.Validation/LayoutTrialValidation.tscn --log-file (Join-Path $trialOutput 'godot.log') -- "--validation-output=$trialOutput" *> (Join-Path $trialOutput 'console.log')
    if ($LASTEXITCODE -ne 0) { throw 'Headless controls failed; see results.json and console.log.' }
    $trialDiagnostics = @(Select-String -LiteralPath (Join-Path $trialOutput 'console.log') -Pattern '^ERROR:|^SCRIPT ERROR:|^WARNING:.*leaked')
    if ($trialDiagnostics.Count -gt 0) { throw 'Godot diagnostics require inspection; see console.log.' }
    if (-not (Test-Path -LiteralPath (Join-Path $trialOutput 'results.json'))) { throw 'Missing result evidence.' }
    $trialResult = Get-Content -LiteralPath (Join-Path $trialOutput 'results.json') -Raw | ConvertFrom-Json
    if (-not $trialResult.passed) { throw 'Headless controls reported failure.' }
    $trialExit = 0
} catch { Write-Output $_; $trialExit = 1 }
finally {
    & $trialDotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild *> (Join-Path $trialOutput 'normal-rebuild.log')
    if ($LASTEXITCODE -ne 0) { Write-Output 'Normal rebuild failed.'; $trialExit = 1 }
    else {
        & (Join-Path $trialRoot 'tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1') -AssemblyPath '.godot/mono/temp/bin/Debug/OpenAntares.dll' -ReportPath (Join-Path $trialOutput 'normal-metadata.json') *> (Join-Path $trialOutput 'normal-metadata.log')
        if ($LASTEXITCODE -ne 0) { $trialExit = 1 }
    }
}
Write-Output "Layout check exit $trialExit; evidence: $trialOutput"
exit $trialExit
