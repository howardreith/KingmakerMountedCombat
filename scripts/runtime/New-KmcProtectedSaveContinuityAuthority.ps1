[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory = $true)][string]$SaveRoot,
    [Parameter(Mandatory = $true)][string]$StateRoot,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$EpochId,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedCurrentQualificationSha256,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedBaselineSha256,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedSupersededWorkingSha256,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedRevisedWorkingSha256,
    [Parameter(Mandatory = $true)][string]$PriorSaveTransactionStatePath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedPriorSaveTransactionRunId,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveTransactionStateSha256,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveMetadataDigest,
    [Parameter(Mandatory = $true)][string]$AutoSaveName,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedAutoSaveSha256,
    [Parameter(Mandatory = $true)][long]$ExpectedAutoSaveLength,
    [Parameter(Mandatory = $true)][long]$ExpectedAutoSaveLastWriteTimeUtcTicks,
    [Parameter(Mandatory = $true)][string]$QuickSaveName,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedQuickSaveSha256,
    [Parameter(Mandatory = $true)][long]$ExpectedQuickSaveLength,
    [Parameter(Mandatory = $true)][long]$ExpectedQuickSaveLastWriteTimeUtcTicks
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$requestedWhatIf = [bool]$WhatIfPreference
$WhatIfPreference = $false

$fullSaveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
$fullStateRoot = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
if (-not (Test-Path -LiteralPath $fullSaveRoot -PathType Container)) { throw 'Kingmaker save root is missing.' }
if (-not (Test-Path -LiteralPath $fullStateRoot -PathType Container)) { throw 'Runtime-state root is missing.' }
Assert-KmcNotReparsePoint $fullSaveRoot 'Kingmaker save root'
Assert-KmcNotReparsePoint $fullStateRoot 'runtime-state root'
Assert-KmcPathsDoNotOverlap -First $fullSaveRoot -Second $fullStateRoot -Description 'KMC save and runtime-state roots'
Assert-KmcNoGameProcesses
$activeLockPath = Assert-KmcChildPath (Join-Path $fullStateRoot 'active-transaction.lock') $fullStateRoot 'runtime lock'
if (Test-Path -LiteralPath $activeLockPath) { throw 'An active or stale KMC runtime lock blocks protected-save authority creation.' }

$qualificationPath = Assert-KmcChildPath (Join-Path $fullStateRoot 'fixture-qualification.json') $fullStateRoot 'fixture qualification'
$pair = Assert-KmcFixturePair -SaveRoot $fullSaveRoot -QualificationPath $qualificationPath
if ((Get-KmcSha256 $qualificationPath) -cne $ExpectedCurrentQualificationSha256 -or
    [string]$pair.baseline.sha256 -cne $ExpectedBaselineSha256 -or
    [string]$pair.working.sha256 -cne $ExpectedRevisedWorkingSha256 -or
    $ExpectedSupersededWorkingSha256 -ceq $ExpectedRevisedWorkingSha256) {
    throw 'Protected-save authority fixture or qualification pins do not match current state.'
}
$priorArguments = @{
    Path=$PriorSaveTransactionStatePath;StateRoot=$fullStateRoot;SaveRoot=$fullSaveRoot
    ExpectedRunId=$ExpectedPriorSaveTransactionRunId;ExpectedStateSha256=$ExpectedPriorSaveTransactionStateSha256
    ExpectedInventoryDigest=$ExpectedPriorSaveMetadataDigest;ExpectedBaselineSha256=$ExpectedBaselineSha256
    ExpectedSupersededWorkingSha256=$ExpectedSupersededWorkingSha256;CurrentPair=$pair
}
$priorAuthority = Read-KmcPriorSaveTransactionAuthority @priorArguments
$saveMetadataBefore = Get-KmcSaveMetadataInventory $fullSaveRoot
$authorizedAtUtc = [DateTimeOffset]::UtcNow.ToUniversalTime().ToString('o')
$recordArguments = @{
    CurrentPair=$pair;PriorAuthority=$priorAuthority;CurrentInventory=$saveMetadataBefore;SaveRoot=$fullSaveRoot
    QualificationPath=$qualificationPath;CurrentQualificationSha256=$ExpectedCurrentQualificationSha256
    EpochId=$EpochId;AuthorizedAtUtc=$authorizedAtUtc
    AutoSaveName=$AutoSaveName;AutoSaveSha256=$ExpectedAutoSaveSha256
    AutoSaveLength=$ExpectedAutoSaveLength;AutoSaveLastWriteTimeUtcTicks=$ExpectedAutoSaveLastWriteTimeUtcTicks
    QuickSaveName=$QuickSaveName;QuickSaveSha256=$ExpectedQuickSaveSha256
    QuickSaveLength=$ExpectedQuickSaveLength;QuickSaveLastWriteTimeUtcTicks=$ExpectedQuickSaveLastWriteTimeUtcTicks
}
$candidate = New-KmcProtectedSaveContinuityAuthorityRecord @recordArguments
$stateBefore = Get-KmcDirectoryManifest $fullStateRoot
$authorityRoot = Assert-KmcChildPath (Join-Path $fullStateRoot 'protected-save-authorities') $fullStateRoot 'protected-save authority root'
$authorityPath = Assert-KmcChildPath (Join-Path $authorityRoot ($EpochId + '.json')) $authorityRoot 'protected-save authority'
if (Test-Path -LiteralPath $authorityPath) { throw 'Protected-save authority epoch already exists.' }

$WhatIfPreference = $requestedWhatIf
if (-not $PSCmdlet.ShouldProcess($authorityPath, 'record the exact user-attested Auto/Quick protected-save continuity epoch')) {
    $WhatIfPreference = $false
    Assert-KmcNoGameProcesses
    $pairAfter = Assert-KmcFixturePair -SaveRoot $fullSaveRoot -QualificationPath $qualificationPath
    $priorAfter = Read-KmcPriorSaveTransactionAuthority @priorArguments
    $metadataAfter = Get-KmcSaveMetadataInventory $fullSaveRoot
    $recordArguments.CurrentPair = $pairAfter
    $recordArguments.PriorAuthority = $priorAfter
    $recordArguments.CurrentInventory = $metadataAfter
    $candidateAfter = New-KmcProtectedSaveContinuityAuthorityRecord @recordArguments
    if (($candidate | ConvertTo-Json -Depth 30 -Compress) -cne ($candidateAfter | ConvertTo-Json -Depth 30 -Compress)) {
        throw 'Protected-save authority inputs changed during WhatIf validation.'
    }
    Assert-KmcSaveMetadataInventoriesEqual -Before $saveMetadataBefore -After $metadataAfter -Description 'protected-save authority WhatIf save metadata'
    if ((Get-KmcDirectoryManifest $fullStateRoot).digest -cne $stateBefore.digest -or
        (Test-Path -LiteralPath $activeLockPath) -or (Test-Path -LiteralPath $authorityPath)) {
        throw 'Protected-save authority WhatIf changed runtime state.'
    }
    Write-Host 'Protected-save continuity authority WhatIf PASS; exact F0-to-current Working/Auto/Quick transition validated without mutation.'
    return
}

$WhatIfPreference = $false
$ConfirmPreference = 'None'
Assert-KmcNoGameProcesses
if (Test-Path -LiteralPath $activeLockPath) { throw 'A runtime lock appeared before protected-save authority creation.' }
if (-not (Test-Path -LiteralPath $authorityRoot -PathType Container)) {
    New-Item -ItemType Directory -Path $authorityRoot | Out-Null
}
Assert-KmcNotReparsePoint $authorityRoot 'protected-save authority root'
if (Test-Path -LiteralPath $authorityPath) { throw 'Protected-save authority epoch appeared before its durable write.' }
$authorityLeaf = [IO.Path]::GetFileName($authorityPath)
$authorityDebris = @(Get-ChildItem -LiteralPath $authorityRoot -Force | Where-Object {
    $_.Name -cmatch ('^\.' + [Regex]::Escape($authorityLeaf) + '\.[0-9a-f]{32}\.tmp$')
})
if ($authorityDebris.Count -ne 0) { throw 'Protected-save authority has unresolved create-new atomic debris.' }

$pairFinal = Assert-KmcFixturePair -SaveRoot $fullSaveRoot -QualificationPath $qualificationPath
$priorFinalArguments = @{}
foreach ($key in $priorArguments.Keys) { $priorFinalArguments[$key] = $priorArguments[$key] }
$priorFinalArguments.CurrentPair = $pairFinal
$priorFinal = Read-KmcPriorSaveTransactionAuthority @priorFinalArguments
$metadataFinal = Get-KmcSaveMetadataInventory $fullSaveRoot
$recordArguments.CurrentPair = $pairFinal
$recordArguments.PriorAuthority = $priorFinal
$recordArguments.CurrentInventory = $metadataFinal
$finalRecord = New-KmcProtectedSaveContinuityAuthorityRecord @recordArguments
if (($candidate | ConvertTo-Json -Depth 30 -Compress) -cne ($finalRecord | ConvertTo-Json -Depth 30 -Compress)) {
    throw 'Protected-save authority inputs changed before its durable write.'
}
Assert-KmcSaveMetadataInventoriesEqual -Before $saveMetadataBefore -After $metadataFinal -Description 'protected-save authority pre-write save metadata'
Assert-KmcNoGameProcesses
if (Test-Path -LiteralPath $activeLockPath) { throw 'A runtime lock appeared immediately before protected-save authority write.' }
Write-KmcJsonCreateNewDurable -Path $authorityPath -Value $finalRecord
$authoritySha256 = Get-KmcSha256 $authorityPath
$validated = Read-KmcProtectedSaveContinuityAuthority `
    -Path $authorityPath -StateRoot $fullStateRoot -SaveRoot $fullSaveRoot -QualificationPath $qualificationPath `
    -ExpectedEpochId $EpochId -ExpectedAuthoritySha256 $authoritySha256 `
    -ExpectedCurrentQualificationSha256 $ExpectedCurrentQualificationSha256 `
    -ExpectedPriorSaveTransactionStatePath $PriorSaveTransactionStatePath `
    -ExpectedPriorSaveTransactionRunId $ExpectedPriorSaveTransactionRunId `
    -ExpectedPriorSaveTransactionStateSha256 $ExpectedPriorSaveTransactionStateSha256 `
    -ExpectedPriorSaveMetadataDigest $ExpectedPriorSaveMetadataDigest `
    -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
    -ExpectedAutoSaveName $AutoSaveName -ExpectedAutoSaveSha256 $ExpectedAutoSaveSha256 `
    -ExpectedQuickSaveName $QuickSaveName -ExpectedQuickSaveSha256 $ExpectedQuickSaveSha256
Assert-KmcSaveMetadataInventoriesEqual -Before $saveMetadataBefore -After $validated.saveMetadata -Description 'protected-save authority committed save metadata'
Assert-KmcNoGameProcesses
if (Test-Path -LiteralPath $activeLockPath) { throw 'Protected-save authority creation left an unexpected runtime lock.' }
Write-Host 'Protected-save continuity authority PASS.'
Write-Host "AUTHORITY=$authorityPath"
Write-Host "SHA256=$authoritySha256"
Write-Host "INVENTORY=$([string]$validated.saveMetadata.digest)"
Write-Host "PROTECTED=$AutoSaveName/$ExpectedAutoSaveSha256,$QuickSaveName/$ExpectedQuickSaveSha256"
