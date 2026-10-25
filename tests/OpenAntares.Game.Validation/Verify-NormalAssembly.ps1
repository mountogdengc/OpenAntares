param([Parameter(Mandatory=$true)][string]$AssemblyPath)
$ErrorActionPreference='Stop'
$resolved=(Resolve-Path -LiteralPath $AssemblyPath).Path
$stream=[IO.File]::OpenRead($resolved)
$reader=[System.Reflection.PortableExecutable.PEReader]::new($stream)
try {
    $metadata=[System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($reader)
    $forbidden=@('GraphicalValidation','ExpansionGraphicalValidation','SpeciesGraphicalValidation','RivalGraphicalValidation','ColonyGraphicalValidation','CrisisGraphicalValidation')
    $found=@(foreach($handle in $metadata.TypeDefinitions){$definition=$metadata.GetTypeDefinition($handle);$name=$metadata.GetString($definition.Name);if($forbidden -contains $name){$name}})
    if($found.Count -gt 0){Write-Output ('Harness types present: '+($found -join ', '));exit 1}
    Write-Output 'All six graphical validation types are absent from the ordinary assembly.'
} finally {$reader.Dispose();$stream.Dispose()}
exit 0
