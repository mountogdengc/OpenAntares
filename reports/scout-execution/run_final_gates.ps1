param([Parameter(Mandatory=$true)][string]$Godot)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $root
try {
    $commands=@()
    foreach($scenario in @('Scout','Legacy','Expansion','Species','Rival','Colony','Crisis')) {
        $folder='reports/scout-execution/task15-'+$scenario.ToLower()+'-20261025'
        $log='.superpowers/sdd/2026-10-25-scout-survival-and-ameba/task15-'+$scenario.ToLower()+'.log'
        $started=[DateTime]::UtcNow.ToString('o')
        $code=0
        try { & ./tests/OpenAntares.Game.Validation/Run-Validation.ps1 -Godot $Godot -Scenario $scenario -OutputDirectory $folder *> $log; $code=$LASTEXITCODE }
        catch { $code=1; $_ | Out-String | Add-Content -LiteralPath $log }
        Copy-Item -LiteralPath $log -Destination (Join-Path $folder 'process.log')
        $commands+=@{scenario=$scenario;exitCode=$code;startedUtc=$started;finishedUtc=[DateTime]::UtcNow.ToString('o');artifact=$folder;command="Run-Validation.ps1 -Godot '$Godot' -Scenario $scenario -OutputDirectory $folder"}
        ConvertTo-Json -InputObject $commands -Depth 5 | Set-Content reports/scout-execution/task15-commands.json
        if($code -ne 0){Get-Content -LiteralPath $log -Tail 16; exit $code}
        Write-Output "$scenario exit0"
    }
}
finally { Pop-Location }
