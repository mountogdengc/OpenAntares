param([string]$SourceRoot=(Split-Path -Parent $PSScriptRoot),[string]$ReportPath)
$ErrorActionPreference='Stop'
$guardArgs=@('-B',(Join-Path $PSScriptRoot 'Check-NoStylingOverrides.py'),'--root',$SourceRoot,'--allowlist',(Join-Path $PSScriptRoot 'styling-allowlist.json'))
if($ReportPath){$guardArgs+=@('--report',$ReportPath)}
python @guardArgs
exit $LASTEXITCODE
