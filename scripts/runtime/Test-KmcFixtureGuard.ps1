[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium', DefaultParameterSetName = 'Validate')]
param(
    [Parameter(Mandatory = $true)][string]$SaveRoot,
    [Parameter(Mandatory = $true)][string]$StateRoot,
    [Parameter(Mandatory = $true, ParameterSetName = 'Initialize')][switch]$InitializeQualification,
    [Parameter(Mandatory = $true, ParameterSetName = 'Requalify')][switch]$RequalifyWorking,
    [Parameter(Mandatory = $true, ParameterSetName = 'AuditContinuity')][switch]$AuditWorkingContinuity,
    [Parameter(Mandatory = $true, ParameterSetName = 'Recover')][switch]$RecoverWorkingRequalification,
    [Parameter(Mandatory = $true, ParameterSetName = 'Recover')]
    [ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RequalificationRunId,
    [Parameter(Mandatory = $true, ParameterSetName = 'Requalify')]
    [Parameter(Mandatory = $true, ParameterSetName = 'Recover')]
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedExistingQualificationSha256,
    [Parameter(Mandatory = $true, ParameterSetName = 'Requalify')]
    [Parameter(Mandatory = $true, ParameterSetName = 'Recover')]
    [Parameter(Mandatory = $true, ParameterSetName = 'AuditContinuity')]
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedBaselineSha256,
    [Parameter(Mandatory = $true, ParameterSetName = 'Requalify')]
    [Parameter(Mandatory = $true, ParameterSetName = 'Recover')]
    [Parameter(Mandatory = $true, ParameterSetName = 'AuditContinuity')]
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedSupersededWorkingSha256,
    [Parameter(Mandatory = $true, ParameterSetName = 'Requalify')]
    [Parameter(Mandatory = $true, ParameterSetName = 'Recover')]
    [Parameter(Mandatory = $true, ParameterSetName = 'AuditContinuity')]
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedRevisedWorkingSha256,
    [Parameter(Mandatory = $true, ParameterSetName = 'AuditContinuity')]
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedCurrentQualificationSha256,
    [Parameter(Mandatory = $true, ParameterSetName = 'Requalify')]
    [Parameter(Mandatory = $true, ParameterSetName = 'AuditContinuity')]
    [string]$PriorSaveTransactionStatePath,
    [Parameter(Mandatory = $true, ParameterSetName = 'Requalify')]
    [Parameter(Mandatory = $true, ParameterSetName = 'AuditContinuity')]
    [ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedPriorSaveTransactionRunId,
    [Parameter(Mandatory = $true, ParameterSetName = 'Requalify')]
    [Parameter(Mandatory = $true, ParameterSetName = 'AuditContinuity')]
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveTransactionStateSha256,
    [Parameter(Mandatory = $true, ParameterSetName = 'Requalify')]
    [Parameter(Mandatory = $true, ParameterSetName = 'AuditContinuity')]
    [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveMetadataDigest
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$requestedWhatIf = [bool]$WhatIfPreference
$WhatIfPreference = $false

$fullStateRoot = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
if (-not (Test-Path -LiteralPath $fullStateRoot -PathType Container)) {
    throw "Runtime state root is missing: $fullStateRoot"
}
Assert-KmcNotReparsePoint $fullStateRoot 'runtime state root'
# Kingmaker native-test reservation (docs/NATIVE-RESERVATION.md), before the first shared read (the save root), in every
# mode and for -WhatIf: initialization and the continuity audit hold a preparation reservation for the whole guard;
# requalification takes the reservation its transaction lock then uses; recovery takes over the stale run's own
# (identity-bound) or, with no lock, audits under a fresh one. Released after the last shared read, or with the lock.
Assert-KmcReservationCoversSurface -Path @($SaveRoot)
$liveRequalificationRunId = $null
$staleLockPath = Join-Path $fullStateRoot 'active-transaction.lock'
if ($RecoverWorkingRequalification -and (Test-Path -LiteralPath $staleLockPath -PathType Leaf)) {
    $requalificationJournal = Join-Path $fullStateRoot ('fixture-requalifications\' + $RequalificationRunId + '.json')
    if (-not (Test-Path -LiteralPath $requalificationJournal)) {
        # A pre-journal interruption replaced nothing yet: the qualification must still be exactly the pinned existing one,
        # and a prior-qualification backup, when it was already written, its exact copy. Checked before any takeover.
        $pinnedQualification = Join-Path $fullStateRoot 'fixture-qualification.json'
        if (-not (Test-Path -LiteralPath $pinnedQualification -PathType Leaf) -or (Get-KmcSha256 $pinnedQualification) -cne $ExpectedExistingQualificationSha256) {
            throw 'The pre-journal requalification recovery finds no exact pinned qualification; recovery refuses before any takeover.'
        }
        $priorBackup = Join-Path $fullStateRoot ('fixture-requalifications\' + $RequalificationRunId + '.prior.json')
        if ((Test-Path -LiteralPath $priorBackup) -and ((Test-Path -LiteralPath $priorBackup -PathType Container) -or (Get-KmcSha256 $priorBackup) -cne $ExpectedExistingQualificationSha256)) {
            throw 'The pre-journal requalification recovery finds a prior-qualification backup that is not the pinned qualification; recovery refuses before any takeover.'
        }
    }
    $nativeReservation = Enter-KmcBoundRecoveryReservation -StateRoot $fullStateRoot -RunId $RequalificationRunId -AlternativeJournal @($requalificationJournal) -PreJournalObligation 'fixture-requalification' -ExpectedPurpose 'fixture-requalification' -Purpose 'fixture-requalification'
}
elseif ($RequalifyWorking) {
    $liveRequalificationRunId = 'fixture-requalification-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')
    $nativeReservation = Enter-KmcNativeReservation -StateRoot $fullStateRoot -RunId $liveRequalificationRunId -Purpose 'fixture-requalification'
}
else { $nativeReservation = Enter-KmcNativeReservation -StateRoot $fullStateRoot -RunId ([DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ') + '-fixture-guard') -Purpose 'preparation-fixture-guard' }
$guardFailed = $false
try {
$fullSaveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
if ($RequalifyWorking -or $RecoverWorkingRequalification -or $AuditWorkingContinuity) {
    Assert-KmcPathsDoNotOverlap -First $fullSaveRoot -Second $fullStateRoot -Description 'KMC save and runtime-state roots'
}
$qualificationPath = Assert-KmcChildPath (Join-Path $fullStateRoot 'fixture-qualification.json') $fullStateRoot 'fixture qualification'
Assert-KmcNoGameProcesses
$activeLockPath = Assert-KmcChildPath (Join-Path $fullStateRoot 'active-transaction.lock') $fullStateRoot 'active runtime transaction lock'
if (($RequalifyWorking -or $AuditWorkingContinuity -or $InitializeQualification) -and (Test-Path -LiteralPath $activeLockPath)) {
    throw 'A stale or active KMC runtime transaction lock prevents Working fixture admission.'
}
$saveMetadataBefore = if ($RequalifyWorking -or $RecoverWorkingRequalification -or $AuditWorkingContinuity) { Get-KmcSaveMetadataInventory $SaveRoot } else { $null }
$audit = Get-KmcFixtureCandidateAudit $SaveRoot
Write-Host "Exact filename audit: baseline=$($audit.baselineCount); working=$($audit.workingCount)."
if ($audit.rejectedKmcLookingNames.Count -ne 0) {
    Write-Host "Rejected KMC-looking filenames: $($audit.rejectedKmcLookingNames -join ', ')"
}

$pair = Get-KmcValidatedFixturePair -SaveRoot $SaveRoot
$requalification = $null
$qualificationMetadataBefore = $null
$recoveryPlan = $null
$stateManifestBefore = $null
if ($AuditWorkingContinuity) {
    Assert-KmcRecoveryLeafNoLinks $qualificationPath 'current KMC fixture qualification'
    $qualificationMetadataBefore = Get-Item -LiteralPath $qualificationPath -Force
    if ((Get-KmcSha256 $qualificationPath) -cne $ExpectedCurrentQualificationSha256) {
        throw 'Current KMC fixture qualification SHA-256 differs from the explicit continuity-audit pin.'
    }
    $stateManifestBefore = Get-KmcDirectoryManifest $fullStateRoot
    $pair = Assert-KmcFixtureQualification -Pair $pair -QualificationPath $qualificationPath
    if ([string]$pair.baseline.sha256 -cne $ExpectedBaselineSha256 -or
        [string]$pair.working.sha256 -cne $ExpectedRevisedWorkingSha256 -or
        $ExpectedSupersededWorkingSha256 -ceq $ExpectedRevisedWorkingSha256) {
        throw 'Current fixture pair differs from the explicit continuity-audit Baseline or Working pins.'
    }
    $priorAuthority = Read-KmcPriorSaveTransactionAuthority `
        -Path $PriorSaveTransactionStatePath `
        -StateRoot $fullStateRoot `
        -SaveRoot $fullSaveRoot `
        -ExpectedRunId $ExpectedPriorSaveTransactionRunId `
        -ExpectedStateSha256 $ExpectedPriorSaveTransactionStateSha256 `
        -ExpectedInventoryDigest $ExpectedPriorSaveMetadataDigest `
        -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
        -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
        -CurrentPair $pair
    $continuity = Assert-KmcWorkingOnlyPriorInventoryTransition `
        -PriorAuthority $priorAuthority `
        -CurrentInventory $saveMetadataBefore `
        -CurrentPair $pair `
        -SaveRoot $fullSaveRoot `
        -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256

    Assert-KmcNoGameProcesses
    $pairAfter = Get-KmcValidatedFixturePair -SaveRoot $fullSaveRoot
    $pairAfter = Assert-KmcFixtureQualification -Pair $pairAfter -QualificationPath $qualificationPath
    $priorAuthorityAfter = Read-KmcPriorSaveTransactionAuthority `
        -Path $PriorSaveTransactionStatePath `
        -StateRoot $fullStateRoot `
        -SaveRoot $fullSaveRoot `
        -ExpectedRunId $ExpectedPriorSaveTransactionRunId `
        -ExpectedStateSha256 $ExpectedPriorSaveTransactionStateSha256 `
        -ExpectedInventoryDigest $ExpectedPriorSaveMetadataDigest `
        -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
        -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
        -CurrentPair $pairAfter
    $saveMetadataAfter = Get-KmcSaveMetadataInventory $fullSaveRoot
    [void](Assert-KmcWorkingOnlyPriorInventoryTransition `
        -PriorAuthority $priorAuthorityAfter `
        -CurrentInventory $saveMetadataAfter `
        -CurrentPair $pairAfter `
        -SaveRoot $fullSaveRoot `
        -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256)
    Assert-KmcSaveMetadataInventoriesEqual `
        -Before $saveMetadataBefore `
        -After $saveMetadataAfter `
        -Description 'Working fixture continuity-audit save metadata'
    $qualificationMetadataAfter = Get-Item -LiteralPath $qualificationPath -Force
    if ((Get-KmcSha256 $qualificationPath) -cne $ExpectedCurrentQualificationSha256 -or
        $qualificationMetadataAfter.Length -ne $qualificationMetadataBefore.Length -or
        $qualificationMetadataAfter.LastWriteTimeUtc.Ticks -ne $qualificationMetadataBefore.LastWriteTimeUtc.Ticks -or
        (Get-KmcDirectoryManifest $fullStateRoot).digest -cne $stateManifestBefore.digest -or
        (Test-Path -LiteralPath $activeLockPath)) {
        throw 'Working fixture continuity audit changed qualification, runtime state, or lock state.'
    }
    $WhatIfPreference = $false
    Exit-KmcUnlockedNativeReservation $nativeReservation  # the release completes before PASS is reported
    $label = if ($requestedWhatIf) { 'Working fixture continuity audit WhatIf PASS' } else { 'Working fixture continuity audit PASS' }
    Write-Host "$label; frozen prior inventory differs from current saves only at exact Working."
    Write-Host "Prior save transaction: $ExpectedPriorSaveTransactionRunId / $ExpectedPriorSaveTransactionStateSha256"
    Write-Host "Prior/current save metadata: $($continuity.priorInventoryDigest) -> $($continuity.currentInventoryDigest)"
    Write-Host "Authorized changed path: $($continuity.changedPath)"
    return
}
elseif ($RequalifyWorking) {
    $requalification = New-KmcWorkingFixtureRequalification `
        -Pair $pair `
        -QualificationPath $qualificationPath `
        -ExpectedExistingQualificationSha256 $ExpectedExistingQualificationSha256 `
        -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
        -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
        -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256
    $priorAuthority = Read-KmcPriorSaveTransactionAuthority `
        -Path $PriorSaveTransactionStatePath `
        -StateRoot $fullStateRoot `
        -SaveRoot $fullSaveRoot `
        -ExpectedRunId $ExpectedPriorSaveTransactionRunId `
        -ExpectedStateSha256 $ExpectedPriorSaveTransactionStateSha256 `
        -ExpectedInventoryDigest $ExpectedPriorSaveMetadataDigest `
        -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
        -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
        -CurrentPair $pair
    if ([long]$priorAuthority.priorWorkingLength -ne [long]$requalification.supersededWorkingLength -or
        [long]$priorAuthority.priorWorkingLastWriteTimeUtcTicks -ne [long]$requalification.supersededWorkingLastWriteTimeUtcTicks) {
        throw 'Prior save-transaction Working metadata differs from the superseded durable qualification.'
    }
    [void](Assert-KmcWorkingOnlyPriorInventoryTransition `
        -PriorAuthority $priorAuthority `
        -CurrentInventory $saveMetadataBefore `
        -CurrentPair $pair `
        -SaveRoot $fullSaveRoot `
        -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256)
    $qualificationMetadataBefore = Get-Item -LiteralPath $qualificationPath -Force
}
elseif ($RecoverWorkingRequalification) {
    $recoveryPlan = Get-KmcWorkingFixtureRequalificationRecoveryPlan `
        -SaveRoot $fullSaveRoot `
        -StateRoot $fullStateRoot `
        -QualificationPath $qualificationPath `
        -RunId $RequalificationRunId `
        -ExpectedPriorQualificationSha256 $ExpectedExistingQualificationSha256 `
        -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
        -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
        -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256
    $qualificationMetadataBefore = Get-Item -LiteralPath $qualificationPath -Force
    $stateManifestBefore = Get-KmcDirectoryManifest $fullStateRoot
}
$WhatIfPreference = $requestedWhatIf
if ($InitializeQualification -and -not (Test-Path -LiteralPath $qualificationPath)) {
    if (-not $PSCmdlet.ShouldProcess($qualificationPath, 'record immutable KMC fixture qualification')) {
        $WhatIfPreference = $false
        if ($requestedWhatIf) {
            Exit-KmcUnlockedNativeReservation $nativeReservation  # the release completes before PASS is reported
            Write-Host 'Fixture guard WhatIf PASS; both exact descriptors were read, no qualification was written, and no save was loaded or mutated.'
        }
        else { Write-Host 'Fixture qualification was declined; no qualification or save was mutated.' }
        return
    }
}
elseif ($RequalifyWorking) {
    if (-not $PSCmdlet.ShouldProcess(
        $qualificationPath,
        "supersede only the pinned KMC Working fingerprint $ExpectedSupersededWorkingSha256 with $ExpectedRevisedWorkingSha256")) {
        $WhatIfPreference = $false
        $pairAfterWhatIf = Get-KmcValidatedFixturePair -SaveRoot $SaveRoot
        [void](New-KmcWorkingFixtureRequalification `
            -Pair $pairAfterWhatIf `
            -QualificationPath $qualificationPath `
            -ExpectedExistingQualificationSha256 $ExpectedExistingQualificationSha256 `
            -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
            -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256)
        $priorAuthorityAfterWhatIf = Read-KmcPriorSaveTransactionAuthority `
            -Path $PriorSaveTransactionStatePath `
            -StateRoot $fullStateRoot `
            -SaveRoot $fullSaveRoot `
            -ExpectedRunId $ExpectedPriorSaveTransactionRunId `
            -ExpectedStateSha256 $ExpectedPriorSaveTransactionStateSha256 `
            -ExpectedInventoryDigest $ExpectedPriorSaveMetadataDigest `
            -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
            -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
            -CurrentPair $pairAfterWhatIf
        [void](Assert-KmcWorkingOnlyPriorInventoryTransition `
            -PriorAuthority $priorAuthorityAfterWhatIf `
            -CurrentInventory (Get-KmcSaveMetadataInventory $fullSaveRoot) `
            -CurrentPair $pairAfterWhatIf `
            -SaveRoot $fullSaveRoot `
            -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256)
        Assert-KmcSaveMetadataInventoriesEqual `
            -Before $saveMetadataBefore `
            -After (Get-KmcSaveMetadataInventory $SaveRoot) `
            -Description 'Working fixture requalification WhatIf save metadata'
        $qualificationAfterWhatIf = Get-Item -LiteralPath $qualificationPath -Force
        if ((Get-KmcSha256 $qualificationPath) -cne $ExpectedExistingQualificationSha256 -or
            $qualificationAfterWhatIf.Length -ne $qualificationMetadataBefore.Length -or
            $qualificationAfterWhatIf.LastWriteTimeUtc.Ticks -ne $qualificationMetadataBefore.LastWriteTimeUtc.Ticks) {
            throw 'Working fixture requalification WhatIf changed the durable qualification.'
        }
        if (Test-Path -LiteralPath $activeLockPath) {
            throw 'Working fixture requalification WhatIf created a runtime lock.'
        }
        if ($requestedWhatIf) {
            Exit-KmcUnlockedNativeReservation $nativeReservation  # the release completes before PASS is reported
            Write-Host 'Working fixture requalification WhatIf PASS; exact revised descriptors and prior pins were validated, and neither the qualification, lock, nor any save metadata changed.'
        }
        else { Write-Host 'Working fixture requalification was declined; no qualification, lock, or save was mutated.' }
        return
    }
    $liveResult = Invoke-KmcWorkingFixtureRequalificationTransaction `
        -SaveRoot $fullSaveRoot `
        -StateRoot $fullStateRoot `
        -QualificationPath $qualificationPath `
        -RunId $liveRequalificationRunId `
        -ExpectedExistingQualificationSha256 $ExpectedExistingQualificationSha256 `
        -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
        -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
        -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256 `
        -PriorSaveTransactionStatePath $PriorSaveTransactionStatePath `
        -ExpectedPriorSaveTransactionRunId $ExpectedPriorSaveTransactionRunId `
        -ExpectedPriorSaveTransactionStateSha256 $ExpectedPriorSaveTransactionStateSha256 `
        -ExpectedPriorSaveMetadataDigest $ExpectedPriorSaveMetadataDigest `
        -Reservation $nativeReservation
    $WhatIfPreference = $false
    $pair = $liveResult.pair
    Exit-KmcUnlockedNativeReservation $nativeReservation  # the release completes before PASS is reported
    Write-Host 'KMC fixture guard PASS.'
    Write-Host "Baseline: $($pair.baseline.fileName) / $($pair.baseline.sha256)"
    Write-Host "Working:  $($pair.working.fileName) / $($pair.working.sha256)"
    Write-Host "Campaign: $($pair.expectedGameName) / $($pair.expectedGameId) / $($pair.expectedArea)"
    Write-Host 'Writable save allowlist: KMC_AUTOMATION_WORKING only.'
    Exit-KmcUnlockedNativeReservation $nativeReservation  # the release completes before PASS is reported
    Write-Host "Working fixture requalification PASS: $($liveResult.supersededWorkingSha256) -> $($liveResult.revisedWorkingSha256)."
    Write-Host "Qualification SHA-256: $($liveResult.qualificationSha256)"
    Write-Host "Pinned prior save metadata digest: $ExpectedPriorSaveMetadataDigest"
    Write-Host "Requalification transaction save metadata unchanged: $($liveResult.saveMetadataDigest)"
    Write-Host "Durable requalification state: $($liveResult.transactionStatePath)"
    return
}
elseif ($RecoverWorkingRequalification) {
    if (-not $PSCmdlet.ShouldProcess(
        $qualificationPath,
        "recover exact stale Working fixture requalification $RequalificationRunId using action $($recoveryPlan.action)")) {
        $WhatIfPreference = $false
        [void](Get-KmcWorkingFixtureRequalificationRecoveryPlan `
            -SaveRoot $fullSaveRoot `
            -StateRoot $fullStateRoot `
            -QualificationPath $qualificationPath `
            -RunId $RequalificationRunId `
            -ExpectedPriorQualificationSha256 $ExpectedExistingQualificationSha256 `
            -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
            -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256)
        Assert-KmcSaveMetadataInventoriesEqual `
            -Before $saveMetadataBefore `
            -After (Get-KmcSaveMetadataInventory $fullSaveRoot) `
            -Description 'Working fixture requalification recovery WhatIf save metadata'
        $qualificationAfterWhatIf = Get-Item -LiteralPath $qualificationPath -Force
        if ((Get-KmcSha256 $qualificationPath) -cne [string]$recoveryPlan.qualificationSha256 -or
            $qualificationAfterWhatIf.Length -ne $qualificationMetadataBefore.Length -or
            $qualificationAfterWhatIf.LastWriteTimeUtc.Ticks -ne $qualificationMetadataBefore.LastWriteTimeUtc.Ticks -or
            (Get-KmcDirectoryManifest $fullStateRoot).digest -cne $stateManifestBefore.digest) {
            throw 'Working fixture requalification recovery WhatIf changed qualification or runtime state.'
        }
        if ($requestedWhatIf) {
            Exit-KmcUnlockedNativeReservation $nativeReservation  # the release completes before PASS is reported
            Write-Host "Working fixture requalification recovery WhatIf PASS; exact stale state supports $($recoveryPlan.action), and no lock, qualification, state, or save metadata changed."
        }
        else { Write-Host 'Working fixture requalification recovery was declined; no lock, qualification, state, or save was mutated.' }
        return
    }
    $recoveryResult = Invoke-KmcWorkingFixtureRequalificationRecovery `
        -SaveRoot $fullSaveRoot `
        -StateRoot $fullStateRoot `
        -QualificationPath $qualificationPath `
        -RunId $RequalificationRunId `
        -ExpectedPriorQualificationSha256 $ExpectedExistingQualificationSha256 `
        -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
        -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
        -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256 `
        -Reservation $(if ($RecoverWorkingRequalification -and $nativeReservation.Adopted) { $nativeReservation } else { $null })
    $WhatIfPreference = $false
    Exit-KmcUnlockedNativeReservation $nativeReservation  # the release completes before PASS is reported
    Write-Host "Working fixture requalification recovery PASS: $($recoveryResult.disposition)."
    Write-Host "Qualification SHA-256: $($recoveryResult.qualificationSha256)"
    Write-Host "Save metadata digest unchanged: $($recoveryResult.saveMetadataDigest)"
    if (-not [string]::IsNullOrWhiteSpace([string]$recoveryResult.statePath)) {
        Write-Host "Durable requalification state: $($recoveryResult.statePath)"
    }
    return
}
$WhatIfPreference = $false
if ($InitializeQualification) {
    # Immediately before initialization may write the qualification: still reserved, and still no local transaction.
    Invoke-KmcReservationTestHook 'guard-before-initialize'
    Assert-KmcNativeReservationHeld -Session $nativeReservation -Context 'fixture initialization'
    if (Test-Path -LiteralPath $activeLockPath) { throw 'A stale or active KMC runtime transaction lock appeared before fixture initialization.' }
}
$pair = Assert-KmcFixtureQualification -Pair $pair -QualificationPath $qualificationPath -InitializeQualification:$InitializeQualification
Exit-KmcUnlockedNativeReservation $nativeReservation  # the release completes before PASS is reported
Write-Host 'KMC fixture guard PASS.'
Write-Host "Baseline: $($pair.baseline.fileName) / $($pair.baseline.sha256)"
Write-Host "Working:  $($pair.working.fileName) / $($pair.working.sha256)"
Write-Host "Campaign: $($pair.expectedGameName) / $($pair.expectedGameId) / $($pair.expectedArea)"
Write-Host 'Writable save allowlist: KMC_AUTOMATION_WORKING only.'
}
catch { $guardFailed = $true; throw }
finally {
    # Preparation and audit reservations end here (after the last shared read); a transaction's reservation ends with
    # its lock (released on a verified close, kept with an abandoned lock). A failed release fails a successful guard.
    Exit-KmcUnlockedNativeReservation $nativeReservation -PreserveError:$guardFailed
}
