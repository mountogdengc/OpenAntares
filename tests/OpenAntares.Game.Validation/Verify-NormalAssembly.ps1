param([Parameter(Mandatory=$true)][string]$AssemblyPath,[string]$ReportPath)
$ErrorActionPreference='Stop'
$resolved=(Resolve-Path -LiteralPath $AssemblyPath).Path
$stream=[IO.File]::OpenRead($resolved)
$reader=$null
try {
    $reader=[System.Reflection.PortableExecutable.PEReader]::new($stream)
    $metadata=[System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($reader)
    $forbidden=@('GraphicalValidation','ExpansionGraphicalValidation','SpeciesGraphicalValidation','RivalGraphicalValidation','ColonyGraphicalValidation','CrisisGraphicalValidation','ScoutGraphicalValidation','FleetGraphicalValidation','CombatGraphicalValidation','ShipDesignGraphicalValidation','DiplomacyGraphicalValidation','LayoutTrialValidation')
    $helpers=@('FrozenContent','ScoutUiControlChecks')
    $names=@(foreach($handle in $metadata.TypeDefinitions){$definition=$metadata.GetTypeDefinition($handle);$metadata.GetString($definition.Name)})
    $found=@($names|Where-Object{$forbidden -contains $_})
    $foundHelpers=@($names|Where-Object{$helpers -contains $_})
    if($ReportPath){[pscustomobject]@{expectedScenarioTypes=$forbidden;presentScenarioTypes=$found;expectedHelpers=$helpers;presentHelpers=$foundHelpers}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath $ReportPath -Encoding utf8}
    if($found.Count -gt 0){Write-Output ('Harness types present: '+($found -join ', '));exit 1}
    if($foundHelpers.Count -gt 0){Write-Output ('Harness helpers present: '+($foundHelpers -join ', '));exit 1}
    Write-Output 'All twelve validation types and both test helpers are absent from the ordinary assembly.'
} finally {if($null -ne $reader){$reader.Dispose()};$stream.Dispose()}
exit 0

