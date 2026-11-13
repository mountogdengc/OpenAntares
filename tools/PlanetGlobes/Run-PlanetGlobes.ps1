param(
    [string]$SourceRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$Godot = 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe',
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [ValidateSet('Builder','Clock','Prototype','Review','Brightness','Layers','Sidebar','Colony','Cities','Performance','Feature')][string]$Case = 'Feature'
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $SourceRoot).Path
$output = [IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
if (!$output.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Evidence directory must be inside SourceRoot' }
if (Test-Path -LiteralPath $output) { throw 'Use a fresh evidence directory' }
if (!(Test-Path -LiteralPath $Godot -PathType Leaf)) { throw 'Godot .NET executable missing' }
New-Item -ItemType Directory -Path $output | Out-Null
$targets = Join-Path $root 'tests/OpenAntares.Game.Validation/PlanetGlobeValidation.targets'
$assembly = Join-Path $root '.godot/mono/temp/bin/Debug/OpenAntares.dll'
function SourceHashes {
    @(Get-ChildItem (Join-Path $root 'game'),(Join-Path $root 'art/planets'),(Join-Path $root 'tests/OpenAntares.Game.Validation'),(Join-Path $root 'tools/PlanetGlobes') -Recurse -File |
        Where-Object { $_.Extension -in '.cs','.json','.tres','.gdshader','.gdshaderinc','.targets','.tscn','.ps1' } |
        ForEach-Object { [pscustomobject]@{path=$_.FullName;hash=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} }) +
        @([pscustomobject]@{path=(Join-Path $root 'project.godot');hash=(Get-FileHash -LiteralPath (Join-Path $root 'project.godot')).Hash})
}
$before = SourceHashes
$before | ConvertTo-Json | Set-Content (Join-Path $output 'source-before.json')
$failure = $null
$restored = $false
Push-Location -LiteralPath $root
try {
    dotnet build OpenAntares.csproj --nologo --disable-build-servers -t:Rebuild "-p:CustomAfterMicrosoftCommonTargets=$targets" *> (Join-Path $output 'opt-in-build.log')
    if ($LASTEXITCODE -ne 0) { throw 'Opt-in build failed' }
    pwsh -NoProfile -File tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath $assembly -ReportPath (Join-Path $output 'opt-in-metadata.json') *> (Join-Path $output 'opt-in-metadata.log')
    if ($LASTEXITCODE -ne 1) { throw 'Real opt-in assembly was not rejected by normal detector' }
    $hash = (Get-FileHash -LiteralPath $assembly).Hash
    & $Godot --path $root --scene res://tests/OpenAntares.Game.Validation/PlanetGlobeValidation.tscn --log-file (Join-Path $output 'godot.log') -- --validation-output=$output --globe-case=$Case *> (Join-Path $output 'console.log')
    if ($LASTEXITCODE -ne 0) { throw 'Native globe checks failed' }
    if ($hash -ne (Get-FileHash -LiteralPath $assembly).Hash) { throw 'Opt-in assembly changed during checks' }
    $result = Get-Content -LiteralPath (Join-Path $output 'results.json') -Raw | ConvertFrom-Json
    if (!$result.passed -or $result.case -ne $Case -or @($result.checks).Count -lt 2) { throw 'Missing/incomplete results' }
    foreach ($log in @('godot.log','console.log')) {
        if (Select-String -LiteralPath (Join-Path $output $log) -Pattern '(?im)^\s*(ERROR|WARNING|SCRIPT ERROR|SHADER ERROR):|shader compilation failed|Unhandled exception') { throw "Engine diagnostics in $log" }
    }
    [pscustomobject]@{path=$assembly;sha256=$hash;case=$Case;godot=$Godot} | ConvertTo-Json | Set-Content (Join-Path $output 'execution.json')
} catch { $failure = $_.ToString() }
finally {
    dotnet build OpenAntares.csproj --nologo --disable-build-servers -t:Rebuild *> (Join-Path $output 'ordinary-build.log')
    if ($LASTEXITCODE -eq 0) {
        pwsh -NoProfile -File tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath $assembly -ReportPath (Join-Path $output 'ordinary-metadata.json') *> (Join-Path $output 'ordinary-metadata.log')
        $restored = $LASTEXITCODE -eq 0
    }
    $after = SourceHashes
    $after | ConvertTo-Json | Set-Content (Join-Path $output 'source-after.json')
    if (($before | ConvertTo-Json -Compress) -ne ($after | ConvertTo-Json -Compress)) { $failure = 'Source changed during run' }
    if (!$restored) { $failure = "Ordinary assembly restoration failed. $failure" }
    [pscustomobject]@{passed=($null -eq $failure);ordinaryRestored=$restored;error=$failure;case=$Case} | ConvertTo-Json | Set-Content (Join-Path $output 'summary.json')
    Pop-Location
}
if ($failure) { throw $failure }
Write-Output "Planet globes $Case passed; ordinary assembly restored. Evidence: $output"
