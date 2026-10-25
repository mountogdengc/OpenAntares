param([string]$PrimaryRoot = 'F:/Projects-PCGames/OpenAntares')

$ErrorActionPreference = 'Stop'
$taskRepo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskBase = '42e1282b9f38fd8d51d4c99ed0f7481437efcee1'
$taskSpecPath = Join-Path $taskRepo 'docs/superpowers/specs/2026-10-25-scout-survival-and-encounter-avoidance-design.md'
$taskOutput = Join-Path $PSScriptRoot 'document-checks.json'
$taskUtf8 = [System.Text.UTF8Encoding]::new($false)
function Assert-Task($Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Read-TaskGit([string]$Root, [string[]]$Arguments) {
    $taskResult = @(& git -C $Root @Arguments)
    if ($LASTEXITCODE -ne 0) { throw "Git read failed: $($Arguments -join ' ')" }
    return $taskResult
}
[System.IO.File]::WriteAllText($taskOutput, '{"status":"In Progress"}', $taskUtf8)
$taskSpec = Get-Content -LiteralPath $taskSpecPath -Raw
$taskRequirementIds = @([regex]::Matches($taskSpec, '(?m)^## \d+\. (SCT-\d{3}):') | ForEach-Object { $_.Groups[1].Value })
$taskAcceptanceIds = @([regex]::Matches($taskSpec, '(?m)^\| (SCT-AC-\d{3}) ') | ForEach-Object { $_.Groups[1].Value })
Assert-Task (($taskRequirementIds -join ',') -eq 'SCT-001,SCT-002,SCT-003,SCT-004,SCT-005,SCT-006,SCT-007') 'Requirement IDs differ'
Assert-Task (($taskAcceptanceIds -join ',') -eq 'SCT-AC-001,SCT-AC-002,SCT-AC-003,SCT-AC-004,SCT-AC-005,SCT-AC-006,SCT-AC-007,SCT-AC-008,SCT-AC-009,SCT-AC-010,SCT-AC-011,SCT-AC-012,SCT-AC-013,SCT-AC-014,SCT-AC-015') 'Acceptance IDs differ'
$taskRegister = Get-Content -LiteralPath (Join-Path $taskRepo 'docs/SPECIFICATIONS.md') -Raw
$taskOldRegister = (Read-TaskGit $taskRepo @('show', "${taskBase}:docs/SPECIFICATIONS.md")) -join "`n"
$taskRows = @([regex]::Matches($taskRegister, '(?m)^\| REQ-\d{3} \|[^\r\n]+') | ForEach-Object { $_.Value })
$taskOldRows = @([regex]::Matches($taskOldRegister, '(?m)^\| REQ-\d{3} \|[^\r\n]+') | ForEach-Object { $_.Value })
Assert-Task ($taskRows.Count -eq 17 -and ($taskRows -join "`n") -eq ($taskOldRows -join "`n")) 'Parent rows changed'
$taskCounts = [ordered]@{}
foreach ($taskRow in $taskRows) {
    $taskStatusCell = $taskRow.Split('|')[4].Trim()
    if (-not $taskCounts.Contains($taskStatusCell)) { $taskCounts[$taskStatusCell] = 0 }
    $taskCounts[$taskStatusCell]++
}
Assert-Task ($taskCounts['Verified'] -eq 12 -and $taskCounts['Implemented, Unverified'] -eq 3 -and $taskCounts['In Progress'] -eq 1 -and $taskCounts['Deferred'] -eq 1) 'Parent status counts differ'
$taskPairs = @('1,1','2,1')
foreach ($taskSchema in 3..8) { foreach ($taskRules in 1..($taskSchema - 1)) { $taskPairs += "$taskSchema,$taskRules" } }
Assert-Task ($taskPairs.Count -eq 29 -and @($taskPairs | Sort-Object -Unique).Count -eq 29) 'Pair arithmetic differs'
$taskTravel = @(
    @{dx=300; dy=0; speed=300; expected=1},
    @{dx=0; dy=300; speed=300; expected=1},
    @{dx=301; dy=0; speed=300; expected=2},
    @{dx=300; dy=0; speed=200; expected=2}
)
foreach ($taskCase in $taskTravel) {
    $taskDuration = 1L
    while ($taskDuration * $taskDuration * $taskCase.speed * $taskCase.speed -lt $taskCase.dx * $taskCase.dx + $taskCase.dy * $taskCase.dy) { $taskDuration++ }
    Assert-Task ($taskDuration -eq $taskCase.expected) 'Independent route arithmetic differs'
}
$taskAmebaCases = @(
    @{speed=300; coefficient=100; maximum=95; expected=95},
    @{speed=300; coefficient=50; maximum=90; expected=75},
    @{speed=200; coefficient=100; maximum=95; expected=50},
    @{speed=200; coefficient=50; maximum=90; expected=50},
    @{speed=100; coefficient=100; maximum=95; expected=5},
    @{speed=100; coefficient=50; maximum=90; expected=25},
    @{speed=600; coefficient=100; maximum=95; expected=95},
    @{speed=600; coefficient=50; maximum=90; expected=90}
)
foreach ($taskCase in $taskAmebaCases) {
    $taskPercent = [math]::Min($taskCase.maximum, [math]::Max(5, 50 + [math]::Floor([decimal]($taskCase.coefficient * ($taskCase.speed - 200)) / 200)))
    Assert-Task ($taskPercent -eq $taskCase.expected) 'Independent ameba probability arithmetic differs'
}
Assert-Task ((75 * 95) -eq 7125) 'Combined study/escape fraction differs'
Assert-Task ((600 / 100) -eq 6) 'Study research reward conversion differs'
$taskLinkCount = 0
$taskDocs = @($taskSpecPath, (Join-Path $taskRepo 'docs/DECISIONS/ADR-015-scout-survival-design-entry.md'), (Join-Path $taskRepo 'reports/SCOUT_DESIGN_REVIEW_2026-10-25.md'))
foreach ($taskDoc in $taskDocs) {
    foreach ($taskMatch in [regex]::Matches((Get-Content -LiteralPath $taskDoc -Raw), '\[[^\]]*\]\(([^)]+)\)')) {
        $taskTarget = $taskMatch.Groups[1].Value.Split('#')[0]
        if ($taskTarget -match '^https?://' -or $taskTarget.Length -eq 0) { continue }
        $taskTarget = [System.Uri]::UnescapeDataString($taskTarget)
        Assert-Task (Test-Path -LiteralPath (Join-Path (Split-Path $taskDoc) $taskTarget)) "Missing local link in ${taskDoc}: $taskTarget"
        $taskLinkCount++
    }
}
$taskTrackedPaths = @(Read-TaskGit $taskRepo @('diff','--name-only',$taskBase))
$taskUntrackedPaths = @(Read-TaskGit $taskRepo @('ls-files','--others','--exclude-standard'))
$taskChangedPaths = @($taskTrackedPaths + $taskUntrackedPaths | Sort-Object -Unique)
foreach ($taskChangedPath in $taskChangedPaths) {
    Assert-Task ($taskChangedPath -match '\.md$|^reports/scout-design/(owner-baseline\.json|document-checks\.json|verify-documents\.ps1)$') "Non-documentation change: $taskChangedPath"
}
$taskBaseline = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'owner-baseline.json') -Raw | ConvertFrom-Json
$taskOwnerHashes = [ordered]@{}
foreach ($taskProperty in $taskBaseline.PSObject.Properties) {
    $taskHash = (Get-FileHash -LiteralPath (Join-Path $PrimaryRoot $taskProperty.Name) -Algorithm SHA256).Hash
    Assert-Task ($taskHash -eq $taskProperty.Value) "Primary owner file changed: $($taskProperty.Name)"
    $taskOwnerHashes[$taskProperty.Name] = $taskHash.ToLowerInvariant()
}
$taskPrimaryStatus = @(Read-TaskGit $PrimaryRoot @('status','--porcelain=v1'))
$taskExpectedStatus = @(' M PROJECT_STATUS.md',' M docs/SPECIES_ROSTER.md',' M docs/research/faction-survey.md','?? docs/FEATURE_REQUESTS.md')
Assert-Task (($taskPrimaryStatus -join "`n") -eq ($taskExpectedStatus -join "`n")) 'Primary status differs'
$taskPrimaryIndex = @(Read-TaskGit $PrimaryRoot @('diff','--cached','--name-only'))
Assert-Task ($taskPrimaryIndex.Count -eq 0) 'Primary index changed'
$taskPrimaryHead = @(Read-TaskGit $PrimaryRoot @('rev-parse','HEAD'))[0]
Assert-Task ($taskPrimaryHead -eq $taskBase) 'Primary main moved'
$taskWorktrees = @(Read-TaskGit $PrimaryRoot @('worktree','list','--porcelain'))
$taskWorktreePaths = @($taskWorktrees | Where-Object { $_ -like 'worktree *' })
Assert-Task ($taskWorktreePaths.Count -eq 7) 'Worktree count differs'
foreach ($taskBranch in @('main','codex/colony-crises','codex/colony-crisis-design','codex/colony-development-design','codex/rival-expansion','codex/rival-polish','codex/species-mechanics')) {
    $null = Read-TaskGit $PrimaryRoot @('rev-parse','--verify',"refs/heads/$taskBranch")
}
$taskBranchName = @(Read-TaskGit $taskRepo @('branch','--show-current'))[0]
Assert-Task ($taskBranchName -eq 'codex/scout-survival-design') 'Draft branch differs'
$taskResult = [ordered]@{
    status = 'Verified for document consistency and preservation only'
    baseline = $taskBase
    branch = $taskBranchName
    specificationSha256 = (Get-FileHash -LiteralPath $taskSpecPath -Algorithm SHA256).Hash.ToLowerInvariant()
    childRequirements = $taskRequirementIds
    acceptanceCriteria = $taskAcceptanceIds
    parentRowsUnchanged = $true
    parentStatusCounts = $taskCounts
    localLinksChecked = $taskLinkCount
    proposedCompatibilityPairs = $taskPairs.Count
    independentTravelExamples = $taskTravel.Count
    independentAmebaArithmeticExamples = $taskAmebaCases.Count + 2
    ownerSelectedAmebaDanger = $true
    changedPaths = $taskChangedPaths
    ownerFilesByteExact = $true
    ownerHashes = $taskOwnerHashes
    primaryStatus = $taskPrimaryStatus
    primaryIndexEmpty = $true
    primaryHeadUnchanged = $true
    preservedWorktreePaths = $taskWorktreePaths
    retainedPriorBranchesChecked = $true
    builderVerdict = 'One modifiability ambiguity resolved against explicit warning/single-reward invariants; no other material questions'
    gameplayDesignApproved = $false
    productImplementationStarted = $false
    applicationBuildTestsRun = $false
    genuineRules6FutureCaptureRun = $false
    integrationPushReleaseCleanup = $false
}
[System.IO.File]::WriteAllText($taskOutput, ((($taskResult | ConvertTo-Json -Depth 8) -replace "`r`n", "`n") + "`n"), $taskUtf8)
$taskResult | ConvertTo-Json -Depth 8
