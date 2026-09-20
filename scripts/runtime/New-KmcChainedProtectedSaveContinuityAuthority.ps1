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
    [Parameter(Mandatory = $true)][string]$ParentAuthorityPath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedParentAuthorityEpochId,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedParentAuthoritySha256,
    [Parameter(Mandatory = $true)][ValidateLength(2,65536)][string]$AuthorizedTransitionsJson
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
if (Test-Path -LiteralPath $activeLockPath) { throw 'An active or stale KMC runtime lock blocks chained authority creation.' }

$qualificationPath = Assert-KmcChildPath (Join-Path $fullStateRoot 'fixture-qualification.json') $fullStateRoot 'fixture qualification'
$pair = Assert-KmcFixturePair -SaveRoot $fullSaveRoot -QualificationPath $qualificationPath
if ((Get-KmcSha256 $qualificationPath) -cne $ExpectedCurrentQualificationSha256 -or
    [string]$pair.baseline.sha256 -cne $ExpectedBaselineSha256 -or
    [string]$pair.working.sha256 -cne $ExpectedRevisedWorkingSha256 -or
    $ExpectedSupersededWorkingSha256 -ceq $ExpectedRevisedWorkingSha256) {
    throw 'Schema-v2 authority fixture or qualification pins do not match current state.'
}
$parentArguments = @{
    Path=$ParentAuthorityPath;StateRoot=$fullStateRoot;SaveRoot=$fullSaveRoot;QualificationPath=$qualificationPath
    ExpectedEpochId=$ExpectedParentAuthorityEpochId;ExpectedAuthoritySha256=$ExpectedParentAuthoritySha256
    ExpectedCurrentQualificationSha256=$ExpectedCurrentQualificationSha256
    ExpectedPriorSaveTransactionStatePath=$PriorSaveTransactionStatePath
    ExpectedPriorSaveTransactionRunId=$ExpectedPriorSaveTransactionRunId
    ExpectedPriorSaveTransactionStateSha256=$ExpectedPriorSaveTransactionStateSha256
    ExpectedPriorSaveMetadataDigest=$ExpectedPriorSaveMetadataDigest
    ExpectedSupersededWorkingSha256=$ExpectedSupersededWorkingSha256;CurrentPair=$pair
}
$parent = Read-KmcHistoricalProtectedSaveContinuityAuthorityV1 @parentArguments
$parentBefore = Get-Item -LiteralPath $parent.path -Force
$transitions = @(ConvertFrom-KmcProtectedSaveTransitionSpecificationsJson -Json $AuthorizedTransitionsJson)
$saveMetadataBefore = Get-KmcSaveMetadataInventory $fullSaveRoot
$authorizedAtUtc = [DateTimeOffset]::UtcNow.ToUniversalTime().ToString('o')
$recordArguments = @{
    CurrentPair=$pair;ParentAuthority=$parent;CurrentInventory=$saveMetadataBefore;SaveRoot=$fullSaveRoot
    QualificationPath=$qualificationPath;CurrentQualificationSha256=$ExpectedCurrentQualificationSha256
    EpochId=$EpochId;AuthorizedAtUtc=$authorizedAtUtc;Transitions=$transitions
}
$candidate = New-KmcChainedProtectedSaveContinuityAuthorityRecord @recordArguments
[void](Assert-KmcChainedProtectedSaveContinuityLiveState -Record $candidate -SaveRoot $fullSaveRoot -LiveInventory $saveMetadataBefore)
$stateBefore = Get-KmcDirectoryManifest $fullStateRoot
$authorityRoot = Assert-KmcChildPath (Join-Path $fullStateRoot 'protected-save-authorities') $fullStateRoot 'protected-save authority root'
if (-not (Test-Path -LiteralPath $authorityRoot -PathType Container)) { throw 'Schema-v2 authority requires the immutable parent authority root.' }
Assert-KmcNotReparsePoint $authorityRoot 'protected-save authority root'
$authorityPath = Assert-KmcChildPath (Join-Path $authorityRoot ($EpochId + '.json')) $authorityRoot 'schema-v2 protected-save authority'
if (Test-Path -LiteralPath $authorityPath) { throw 'Schema-v2 protected-save authority epoch already exists.' }

$WhatIfPreference = $requestedWhatIf
if (-not $PSCmdlet.ShouldProcess($authorityPath, 'append the exact user-attested schema-v2 protected-save continuity epoch')) {
    $WhatIfPreference = $false
    Assert-KmcNoGameProcesses
    $pairAfter = Assert-KmcFixturePair -SaveRoot $fullSaveRoot -QualificationPath $qualificationPath
    $parentArguments.CurrentPair = $pairAfter
    $parentAfter = Read-KmcHistoricalProtectedSaveContinuityAuthorityV1 @parentArguments
    $metadataAfter = Get-KmcSaveMetadataInventory $fullSaveRoot
    $recordArguments.CurrentPair = $pairAfter
    $recordArguments.ParentAuthority = $parentAfter
    $recordArguments.CurrentInventory = $metadataAfter
    $candidateAfter = New-KmcChainedProtectedSaveContinuityAuthorityRecord @recordArguments
    [void](Assert-KmcChainedProtectedSaveContinuityLiveState -Record $candidateAfter -SaveRoot $fullSaveRoot -LiveInventory $metadataAfter)
    if (($candidate | ConvertTo-Json -Depth 30 -Compress) -cne ($candidateAfter | ConvertTo-Json -Depth 30 -Compress)) {
        throw 'Schema-v2 protected-save authority inputs changed during WhatIf validation.'
    }
    Assert-KmcSaveMetadataInventoriesEqual -Before $saveMetadataBefore -After $metadataAfter -Description 'schema-v2 authority WhatIf save metadata'
    $parentAfterMetadata = Get-Item -LiteralPath $parent.path -Force
    if ((Get-KmcSha256 $parent.path) -cne $ExpectedParentAuthoritySha256 -or
        $parentAfterMetadata.Length -ne $parentBefore.Length -or
        $parentAfterMetadata.LastWriteTimeUtc.Ticks -ne $parentBefore.LastWriteTimeUtc.Ticks -or
        (Get-KmcDirectoryManifest $fullStateRoot).digest -cne $stateBefore.digest -or
        (Test-Path -LiteralPath $activeLockPath) -or (Test-Path -LiteralPath $authorityPath)) {
        throw 'Schema-v2 authority WhatIf changed historical authority, runtime state, or lock state.'
    }
    Write-Host 'Schema-v2 protected-save continuity authority WhatIf PASS; exact chained transitions validated without mutation.'
    return
}

$WhatIfPreference = $false
$ConfirmPreference = 'None'
Assert-KmcNoGameProcesses
if (Test-Path -LiteralPath $activeLockPath) { throw 'A runtime lock appeared before schema-v2 authority creation.' }
if (Test-Path -LiteralPath $authorityPath) { throw 'Schema-v2 protected-save authority epoch appeared before its durable write.' }
$authorityLeaf = [IO.Path]::GetFileName($authorityPath)
$authorityDebris = @(Get-ChildItem -LiteralPath $authorityRoot -Force | Where-Object {
    $_.Name -cmatch ('^\.' + [Regex]::Escape($authorityLeaf) + '\.[0-9a-f]{32}\.tmp$')
})
if ($authorityDebris.Count -ne 0) { throw 'Schema-v2 protected-save authority has unresolved create-new atomic debris.' }

$pairFinal = Assert-KmcFixturePair -SaveRoot $fullSaveRoot -QualificationPath $qualificationPath
$parentArguments.CurrentPair = $pairFinal
$parentFinal = Read-KmcHistoricalProtectedSaveContinuityAuthorityV1 @parentArguments
$metadataFinal = Get-KmcSaveMetadataInventory $fullSaveRoot
$recordArguments.CurrentPair = $pairFinal
$recordArguments.ParentAuthority = $parentFinal
$recordArguments.CurrentInventory = $metadataFinal
$finalRecord = New-KmcChainedProtectedSaveContinuityAuthorityRecord @recordArguments
[void](Assert-KmcChainedProtectedSaveContinuityLiveState -Record $finalRecord -SaveRoot $fullSaveRoot -LiveInventory $metadataFinal)
if (($candidate | ConvertTo-Json -Depth 30 -Compress) -cne ($finalRecord | ConvertTo-Json -Depth 30 -Compress)) {
    throw 'Schema-v2 authority inputs changed before its durable write.'
}
Assert-KmcSaveMetadataInventoriesEqual -Before $saveMetadataBefore -After $metadataFinal -Description 'schema-v2 authority pre-write save metadata'
$parentFinalMetadata = Get-Item -LiteralPath $parent.path -Force
if ((Get-KmcSha256 $parent.path) -cne $ExpectedParentAuthoritySha256 -or
    $parentFinalMetadata.Length -ne $parentBefore.Length -or
    $parentFinalMetadata.LastWriteTimeUtc.Ticks -ne $parentBefore.LastWriteTimeUtc.Ticks) {
    throw 'Immutable parent authority changed before schema-v2 durable creation.'
}
Assert-KmcNoGameProcesses
if (Test-Path -LiteralPath $activeLockPath) { throw 'A runtime lock appeared immediately before schema-v2 authority write.' }
Write-KmcJsonCreateNewDurable -Path $authorityPath -Value $finalRecord
$authoritySha256 = Get-KmcSha256 $authorityPath
$pinSetSha256 = [string]$finalRecord.currentProtectedSavePinsSha256
$validated = Read-KmcChainedProtectedSaveContinuityAuthority `
    -Path $authorityPath -StateRoot $fullStateRoot -SaveRoot $fullSaveRoot -QualificationPath $qualificationPath `
    -ExpectedEpochId $EpochId -ExpectedAuthoritySha256 $authoritySha256 `
    -ExpectedCurrentQualificationSha256 $ExpectedCurrentQualificationSha256 `
    -ExpectedPriorSaveTransactionStatePath $PriorSaveTransactionStatePath `
    -ExpectedPriorSaveTransactionRunId $ExpectedPriorSaveTransactionRunId `
    -ExpectedPriorSaveTransactionStateSha256 $ExpectedPriorSaveTransactionStateSha256 `
    -ExpectedPriorSaveMetadataDigest $ExpectedPriorSaveMetadataDigest `
    -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
    -ExpectedProtectedSavePinSetSha256 $pinSetSha256
Assert-KmcSaveMetadataInventoriesEqual -Before $saveMetadataBefore -After $validated.saveMetadata -Description 'schema-v2 authority committed save metadata'
$parentAfterCommit = Get-Item -LiteralPath $parent.path -Force
if ((Get-KmcSha256 $parent.path) -cne $ExpectedParentAuthoritySha256 -or
    $parentAfterCommit.Length -ne $parentBefore.Length -or
    $parentAfterCommit.LastWriteTimeUtc.Ticks -ne $parentBefore.LastWriteTimeUtc.Ticks) {
    throw 'Immutable parent authority changed during schema-v2 durable creation.'
}
Assert-KmcNoGameProcesses
if (Test-Path -LiteralPath $activeLockPath) { throw 'Schema-v2 authority creation left an unexpected runtime lock.' }
Write-Host 'Schema-v2 protected-save continuity authority PASS.'
Write-Host "AUTHORITY=$authorityPath"
Write-Host "SHA256=$authoritySha256"
Write-Host "PIN_SET_SHA256=$pinSetSha256"
Write-Host "INVENTORY=$([string]$validated.saveMetadata.digest)"
Write-Host "TRANSITIONS=$(@($finalRecord.authorizedProtectedTransitions | ForEach-Object currentPath) -join ',')"
