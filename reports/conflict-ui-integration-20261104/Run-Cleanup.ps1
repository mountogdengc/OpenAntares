param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $root
$output=[IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
if(!$output.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $output)){throw 'Use a fresh in-checkout evidence directory'}
New-Item -ItemType Directory -Path $output | Out-Null
$targets=Join-Path $root 'tests/OpenAntares.Game.Validation/Validation.targets'
$godot='F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
$code=1
try {
 dotnet build OpenAntares.csproj --no-restore --nologo --disable-build-servers -t:Rebuild "-p:CustomAfterMicrosoftCommonTargets=$targets" *> "$output/validation-build.log"
 if($LASTEXITCODE -ne 0){throw 'Cleanup opt-in build failed'}
 & ./tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath .godot/mono/temp/bin/Debug/OpenAntares.dll -ReportPath "$output/opt-in-metadata.json" *> "$output/opt-in-metadata.log"
 if($LASTEXITCODE -ne 1){throw 'Opt-in negative control failed'}
 python tools/ConflictValidation/Verify-Evidence.py --capture-source "$output/source.json" --assembly .godot/mono/temp/bin/Debug/OpenAntares.dll
 if($LASTEXITCODE -ne 0){throw 'Source capture failed'}
 & $godot --headless --path $root --quit-after 1200 --scene res://tests/OpenAntares.Game.Validation/ConflictCleanupValidation.tscn --log-file "$output/godot.log" *> "$output/console.log"
 if($LASTEXITCODE -ne 0){throw 'Legacy badge cleanup regression failed'}
 if(!(Select-String -LiteralPath "$output/console.log" -SimpleMatch 'PASS: Conflict badge has screen ownership for rules8/9/11/12.')){throw 'Required cleanup assertion missing'}
 $code=0
} catch { Write-Output $_ }
finally {
 dotnet build OpenAntares.sln --no-restore --nologo --disable-build-servers -t:Rebuild *> "$output/normal-rebuild.log"
 if($LASTEXITCODE -ne 0){$code=1}
 else {
  & ./tests/OpenAntares.Game.Validation/Verify-NormalAssembly.ps1 -AssemblyPath .godot/mono/temp/bin/Debug/OpenAntares.dll -ReportPath "$output/normal-metadata.json" *> "$output/normal-metadata.log"
  if($LASTEXITCODE -ne 0){$code=1}
 }
 $diagnostics=@(Select-String -LiteralPath "$output/console.log","$output/godot.log" -Pattern '^\s*(ERROR:|SCRIPT ERROR:|WARNING:)')
 if($diagnostics.Count -gt 0){$code=1}
 $source=Get-Content -LiteralPath "$output/source.json" -Raw | ConvertFrom-Json
 $stable=@($source.files|Where-Object{(Get-FileHash -LiteralPath $_.path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $_.sha256}).Count -eq 0
 if(!$stable){$code=1}
 @{passed=($code -eq 0);exitCode=$code;sourceStable=$stable;diagnostics=@($diagnostics|ForEach-Object{$_.Line});rules=@(8,9,11,12)}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "$output/result.json"
}
exit $code
