param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9_-]+$')][string]$Name,
    [string]$OutputRoot='reports/conflict-execution/gates',
    [Parameter(Mandatory=$true)][string]$Command,
    [Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments
)
$ErrorActionPreference='Stop'
$repoRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$gateHash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($repoRoot.ToUpperInvariant())))
$gateMutex=[Threading.Mutex]::new($false,('Local\OpenAntaresConflictGate_'+$gateHash))
$gateOwned=$gateMutex.WaitOne(0)
if(-not $gateOwned){$gateMutex.Dispose();throw 'Another validation gate is active in this checkout; run gates sequentially.'}
try {
$outputPath=[IO.Path]::GetFullPath((Join-Path $repoRoot $OutputRoot))
if(-not $outputPath.StartsWith($repoRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Gate output must stay inside this checkout.'}
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$log=Join-Path $outputPath ($Name+'.log')
if(Test-Path -LiteralPath $log){throw 'Use a new attempt name; never overwrite a gate.'}
$reports=Join-Path $repoRoot 'reports'
$saved=@{}
Get-ChildItem -LiteralPath $reports -Recurse -File -Filter '*latest*.json' | ForEach-Object {$saved[$_.FullName]=[IO.File]::ReadAllBytes($_.FullName)}
$oldScout=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
Get-ChildItem -LiteralPath (Join-Path $reports 'scout-execution') -File -Filter 'task14-*.json' | ForEach-Object {[void]$oldScout.Add($_.FullName)}
$restored=[Collections.Generic.List[string]]::new()
$relocated=[Collections.Generic.List[object]]::new()
$timer=[Diagnostics.Stopwatch]::StartNew();$resultCode=1
Push-Location -LiteralPath $repoRoot
try {
    try { & $Command @Arguments *> $log; $resultCode=$LASTEXITCODE }
    catch { $_ | Out-String | Add-Content -LiteralPath $log; $resultCode=1 }
    finally {
        foreach($path in $saved.Keys){
            $current=if(Test-Path -LiteralPath $path){[IO.File]::ReadAllBytes($path)}else{[byte[]]::new(0)}
            if(-not [Linq.Enumerable]::SequenceEqual([byte[]]$current,[byte[]]$saved[$path])){[IO.File]::WriteAllBytes($path,$saved[$path]);$restored.Add([IO.Path]::GetRelativePath($repoRoot,$path))}
        }
        Get-ChildItem -LiteralPath (Join-Path $reports 'scout-execution') -File -Filter 'task14-*.json' | Where-Object {-not $oldScout.Contains($_.FullName) -and $_.Name -notlike '*-latest.json'} | ForEach-Object {
            $source=$_.FullName
            $target=[IO.Path]::GetFullPath((Join-Path $repoRoot ('reports/conflict-execution/test-outputs/'+$Name+'/'+$_.Name)))
            if(-not $source.StartsWith($repoRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or -not $target.StartsWith($repoRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $target)){throw 'Unsafe or reused raw-report target.'}
            New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
            $digest=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($source))).ToLowerInvariant()
            Move-Item -LiteralPath $source -Destination $target
            $relocated.Add([pscustomobject]@{original=[IO.Path]::GetRelativePath($repoRoot,$source);retained=[IO.Path]::GetRelativePath($repoRoot,$target);sha256=$digest})
        }
    }
}
finally {Pop-Location}
$record=[pscustomobject]@{command=@($Command)+$Arguments;exitCode=$resultCode;seconds=[Math]::Round($timer.Elapsed.TotalSeconds,3);utc=[DateTime]::UtcNow.ToString('O');preservedHistoricalLatestReports=$restored;rawReportRelocations=$relocated}
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outputPath ($Name+'.json')) -Encoding utf8NoBOM
$record | Select-Object exitCode,seconds,utc | ConvertTo-Json -Compress
Get-Content -LiteralPath $log -Tail 10
exit $resultCode

} finally {$gateMutex.ReleaseMutex();$gateMutex.Dispose()}
