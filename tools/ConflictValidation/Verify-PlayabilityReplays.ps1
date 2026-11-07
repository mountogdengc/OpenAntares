param(
 [Parameter(Mandatory=$true)][string]$MatrixPath,
 [Parameter(Mandatory=$true)][string]$OutputDirectory
)
# Run this foreground worker through the installed shared queue. It never launches a GUI.
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location -LiteralPath $root
$matrixFile=(Resolve-Path -LiteralPath $MatrixPath).Path
$output=[IO.Path]::GetFullPath((Join-Path $root $OutputDirectory))
if(!$output.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Replay evidence must remain inside the tested checkout'}
if(Test-Path -LiteralPath $output){throw 'Retain prior replay evidence; choose a fresh output directory'}
New-Item -ItemType Directory -Path $output|Out-Null
$matrix=Get-Content -LiteralPath $matrixFile -Raw|ConvertFrom-Json
if(!$matrix.passed -or @($matrix.campaigns).Count -ne 8){throw 'Eight complete native campaign profiles are required before independent replay'}
$records=[Collections.Generic.List[object]]::new()
$errorText=$null
try {
 & dotnet build tools/ConflictCampaignProbe/ConflictCampaignProbe.csproj --nologo --disable-build-servers *> (Join-Path $output 'probe-build.log')
 if($LASTEXITCODE -ne 0){throw 'Existing authoritative probe build failed'}
 $assembly=Join-Path $root 'tools/ConflictCampaignProbe/bin/Debug/net8.0/ConflictCampaignProbe.dll'
 $compiled=(Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash.ToLowerInvariant()
 foreach($campaign in $matrix.campaigns){foreach($checkpoint in $campaign.checkpoints){
  $main=Join-Path $root $checkpoint.mainSave.path
  $future=Join-Path $root $checkpoint.future.path
  $replay=Get-Content -LiteralPath (Join-Path $root $checkpoint.replay.path) -Raw|ConvertFrom-Json
  if((Get-FileHash -LiteralPath $main -Algorithm SHA256).Hash.ToLowerInvariant() -ne $checkpoint.mainSave.sha256 -or (Get-FileHash -LiteralPath $future -Algorithm SHA256).Hash.ToLowerInvariant() -ne $checkpoint.future.sha256){throw 'A retained Main save or future proof changed'}
  $entry=[ordered]@{species=$campaign.species;mode=$campaign.mode;turn=$checkpoint.turn;mainSaveSha256=$checkpoint.mainSave.sha256;futureSha256=$checkpoint.future.sha256;method=$replay.method;externalApplicable=($replay.method -eq 'existing-probe');passed=$false;exitCode=$null}
  if($replay.method -eq 'existing-probe'){
   $log=Join-Path $output ($campaign.species+'-'+$campaign.mode+'-'+$checkpoint.turn+'.log')
   & dotnet $assembly --verify-save-proof $main $future *> $log
   $entry.exitCode=$LASTEXITCODE;$entry.passed=$LASTEXITCODE -eq 0
   $entry.log=@{path=[IO.Path]::GetRelativePath($root,$log).Replace('\','/');sha256=(Get-FileHash -LiteralPath $log -Algorithm SHA256).Hash.ToLowerInvariant()}
  } elseif($replay.method -eq 'native-extended-core-replay'){
   # Existing decoder deliberately has no crisis/scouting command cases. The separately
   # recorded native closed-allowlist replay is required; never label it external coverage.
   $entry.passed=$replay.passed -and $replay.boundaries -eq 3 -and $replay.mainSaveSha256 -eq $checkpoint.mainSave.sha256 -and $replay.futureSha256 -eq $checkpoint.future.sha256
  } else {throw 'Unknown authoritative replay method'}
  $records.Add([pscustomobject]$entry)
  if(!$entry.passed){throw 'Canonical replay failed; preserve this attempt'}
 }}
 if((Get-FileHash -LiteralPath $assembly -Algorithm SHA256).Hash.ToLowerInvariant() -ne $compiled){throw 'Probe assembly changed during execution'}
} catch {$errorText=$_.ToString();Write-Output $_}
finally {
 $passed=$null -eq $errorText -and $records.Count -eq 40 -and @($records|Where-Object{!$_.passed}).Count -eq 0
 @{passed=$passed;scope='Independent unchanged probe for supported tapes; closed-allowlist native authority for existing crisis/scouting commands';sourceSha256=$matrix.sourceSha256;matrixSha256=(Get-FileHash -LiteralPath $matrixFile -Algorithm SHA256).Hash.ToLowerInvariant();scriptSha256=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant();probeAssemblySha256=$compiled;records=@($records);error=$errorText}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'result.json') -Encoding utf8
}
if(!$passed){throw 'Independent replay evidence is incomplete or failed'}
