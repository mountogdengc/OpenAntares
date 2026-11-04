param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$trialRoot = Split-Path -Parent $PSScriptRoot
$trialGodot = 'F:/Projects-PCGames/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
$trialOutput = [IO.Path]::GetFullPath((Join-Path $trialRoot $OutputDirectory))
if (-not $trialOutput.StartsWith($trialRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Evidence must be inside this checkout.' }
if (Test-Path -LiteralPath $trialOutput) { throw 'Use a new evidence directory.' }
New-Item -ItemType Directory -Path $trialOutput | Out-Null
Set-Location -LiteralPath $trialRoot
& $trialGodot --headless --editor --import --path $trialRoot --quit --log-file (Join-Path $trialOutput 'import-godot.log') *> (Join-Path $trialOutput 'import-console.log')
if ($LASTEXITCODE -ne 0) { throw 'Asset preparation failed.' }
& $trialGodot --headless --path $trialRoot --quit-after 60 --log-file (Join-Path $trialOutput 'startup-godot.log') -- --new-game=small:42 *> (Join-Path $trialOutput 'startup-console.log')
if ($LASTEXITCODE -ne 0) { throw 'Main-scene startup failed.' }
$trialDiagnostics = @(Select-String -LiteralPath (Join-Path $trialOutput 'startup-console.log') -Pattern '^ERROR:|^SCRIPT ERROR:|Exception:' )
if ($trialDiagnostics.Count -gt 0) { throw 'Startup diagnostics require inspection.' }
Write-Output "Headless import and Main-scene startup passed; no rendered or physical-input claim. Evidence: $trialOutput"
