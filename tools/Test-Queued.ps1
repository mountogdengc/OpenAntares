param([string]$Suite = "default", [string]$SourceRoot = "")
$ErrorActionPreference = "Stop"
$queueRepoRoot = Split-Path -Parent $PSScriptRoot
$queueConfig = Join-Path $queueRepoRoot ".codex-tests.json"
$queueSettings = Get-Content -LiteralPath $queueConfig -Raw | ConvertFrom-Json
if ($queueSettings.execution -ne "native") { throw "This entry point requires the native queue configuration." }
if (-not $queueSettings.suites.PSObject.Properties[$Suite]) { throw "Unknown native suite: $Suite" }
if (-not $SourceRoot) { $SourceRoot = $queueRepoRoot }
$SourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
if (-not (Test-Path -LiteralPath (Join-Path $SourceRoot "OpenAntares.sln"))) { throw "SourceRoot must be the actual OpenAntares checkout." }
$queueWorker = Join-Path $PSScriptRoot "Run-NativeTests.py"
$queueTimeout = [int]$queueSettings.suites.$Suite.timeout
& python $queueSettings.runner exec --project $queueSettings.project --timeout $queueTimeout -- python $queueWorker --source-root $SourceRoot --config $queueConfig --suite $Suite
exit $LASTEXITCODE
