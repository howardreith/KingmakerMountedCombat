[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')

$repoRoot = Get-KmcRepositoryRoot
$testParent = [IO.Path]::GetFullPath((Join-Path $repoRoot 'obj\harness-tests'))
$testRoot = Assert-KmcChildPath (Join-Path $testParent ([Guid]::NewGuid().ToString('N'))) $testParent 'harness test root'
$runtimeEvidenceParent = [IO.Path]::GetFullPath((Join-Path (Get-KmcLabRoot) 'runtime-evidence'))
$runtimeEvidenceTestRoot = Assert-KmcChildPath (Join-Path $runtimeEvidenceParent ('harness-test-' + [Guid]::NewGuid().ToString('N'))) $runtimeEvidenceParent 'harness runtime-evidence test root'
$passed = 0
$failed = 0

function Invoke-HarnessTest {
    param([string]$Name, [scriptblock]$Body)
    try {
        & $Body
        $script:passed++
        Write-Host "PASS $Name"
    }
    catch {
        $script:failed++
        Write-Host "FAIL $Name`: $($_.Exception.Message)"
    }
}

function Assert-Test([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function New-TestSaveArchive {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name,
        [string]$GameName = 'KMC Test Campaign',
        [string]$GameId = '11111111-2222-3333-4444-555555555555',
        [string]$Area = '0123456789abcdef0123456789abcdef',
        [switch]$ExtraEntry
    )
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $source = Join-Path $testRoot ('save-source-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $source | Out-Null
    try {
        $header = [ordered]@{ Name=$Name; GameName=$GameName; GameId=$GameId; Area=$Area; Type='Manual'; CompatibilityVersion=1 }
        Write-KmcJsonAtomic (Join-Path $source 'header.json') $header
        if ($ExtraEntry) { [IO.File]::WriteAllText((Join-Path $source 'extra.bin'), 'changed working fixture') }
        if (Test-Path -LiteralPath $Path) { [IO.File]::Delete($Path) }
        [IO.Compression.ZipFile]::CreateFromDirectory($source, $Path)
    }
    finally {
        if (Test-Path -LiteralPath $source) { Remove-Item -LiteralPath $source -Recurse -Force }
    }
}

function New-TestRawSaveArchive {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$HeaderJson
    )
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $source = Join-Path $testRoot ('raw-save-source-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $source | Out-Null
    try {
        [IO.File]::WriteAllText((Join-Path $source 'header.json'), $HeaderJson, (New-Object Text.UTF8Encoding($false)))
        if (Test-Path -LiteralPath $Path) { [IO.File]::Delete($Path) }
        [IO.Compression.ZipFile]::CreateFromDirectory($source, $Path)
    }
    finally {
        if (Test-Path -LiteralPath $source) { Remove-Item -LiteralPath $source -Recurse -Force }
    }
}

function New-TestArtifactManifest {
    param(
        [Parameter(Mandatory = $true)][string]$EvidenceRoot,
        [Parameter(Mandatory = $true)][string]$RunId,
        [Parameter(Mandatory = $true)][string]$Scenario,
        [array]$Artifacts = @()
    )
    New-Item -ItemType Directory -Path $EvidenceRoot -Force | Out-Null
    $path = Join-Path $EvidenceRoot 'runtime-artifacts.json'
    Write-KmcJsonDurable -Path $path -Value ([ordered]@{
        schemaVersion = 1
        runId = $RunId
        scenario = $Scenario
        createdAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        artifacts = @($Artifacts)
    })
    return Get-KmcSha256 $path
}

function New-TestLifecycleUnitEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$UniqueId,
        [Parameter(Mandatory = $true)][int]$SizeOrdinal,
        [switch]$MountedRider
    )
    return [ordered]@{
        uniqueId=$UniqueId;sizeOrdinal=$SizeOrdinal;inCombat=$false;stockAgentEnabled=$(if($MountedRider){$false}else{$true})
        avoidanceDisabled=$(if($MountedRider){$true}else{$false});forbidRotation=$(if($MountedRider){$true}else{$false})
        agentOverrideType=$(if($MountedRider){'KingmakerMountedCombat.Integration.RiderMovementAgent'}else{$null})
        overrideComponentCount=$(if($MountedRider){1}else{0})
        entityPosition=[ordered]@{x=1.0;y=2.0;z=3.0};entityRotationDegrees=45.0
        viewPosition=[ordered]@{x=1.0;y=2.0;z=3.0};viewRotation=[ordered]@{x=0.0;y=0.0;z=0.0;w=1.0}
        moveCommandType=$null;moveTarget=$null;activeCommandTypes=@();selected=$false
    }
}

function New-TestLifecycleEvidenceRecord {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][long]$Sequence,
        [Parameter(Mandatory = $true)][string]$Row,
        [Parameter(Mandatory = $true)][string]$Phase,
        [Parameter(Mandatory = $true)][string]$RelationshipState,
        [switch]$WithCleanup,
        [AllowNull()]$RowStatus,
        [AllowNull()]$AssertionPassCount,
        [AllowNull()]$AssertionFailCount,
        [string[]]$RecordErrors = @()
    )
    $expectedTrigger = Get-KmcLifecycleExpectedCleanupTrigger $Row
    $invocationPath = Get-KmcLifecycleInvocationPath $Row
    $cleanup = if ($WithCleanup) {
        [ordered]@{trigger=$expectedTrigger;result='PASS';succeeded=$true;state='Unmounted';movementAuthorityResidual=$false;presentationResidual=$false;errors=@()}
    } else {
        [ordered]@{trigger=$null;result=$null;succeeded=$null;state=$null;movementAuthorityResidual=$null;presentationResidual=$null;errors=@()}
    }
    $mounted = $Phase -ceq 'mounted-next-frame'
    $restored = $WithCleanup -and $Row -cne 'mounted-pair-invalid-pair-rejected'
    $frame = [int]($Sequence + 1)
    if ($Phase -ceq 'row-finish' -and $Row -cne 'mounted-pair-cleanup-idempotent') { $frame = [int]$Sequence }
    $originalParent = 'Scene/Units/Rider'
    $currentParent = if ($mounted) { 'Scene/Mount/KMC_RiderPositionAnchor' } else { $originalParent }
    return [ordered]@{
        schemaVersion=2;runId=[string]$Request.runId;scenario=[string]$Request.scenario;row=$Row;phase=$Phase
        utcTimestamp=[DateTimeOffset]::UtcNow.ToUniversalTime().ToString('o');branch=[string]$Request.branch;commit=[string]$Request.commit
        productVersion=[string]$Request.productVersion;dllSha256=[string]$Request.dllSha256;dllMvid=[string]$Request.dllMvid
        sequence=$Sequence;frame=$frame;relationshipState=$RelationshipState
        triggerScope=[ordered]@{
            expectedCleanupTrigger=$expectedTrigger;invocationPath=$invocationPath;nativeDeliveryObserved=$false
            claimLimit='Direct service/handler invocation only; native EventBus/UMM delivery was not exercised.'
        }
        rowStatus=$RowStatus
        assertionPassCount=$AssertionPassCount;assertionFailCount=$AssertionFailCount;cleanup=$cleanup
        partyCombat=$false;riderCombat=$false;mountCombat=$false;turnBased=$false;paused=$false;currentGameMode='Default'
        rider=(New-TestLifecycleUnitEvidence 'rider-id' 4 -MountedRider:$mounted);mount=(New-TestLifecycleUnitEvidence 'mount-id' 6)
        selection=[ordered]@{available=$true;riderSelected=$true;mountSelected=$false;selectedUnitIds=@('rider-id')}
        spine=[ordered]@{name='Spine';worldPosition=[ordered]@{x=1.0;y=2.0;z=3.0};worldRotation=[ordered]@{x=0.0;y=0.0;z=0.0;w=1.0}}
        anchor=[ordered]@{
            name=$(if($mounted){'Spine'}else{$null})
            expectedPosition=$(if($mounted){[ordered]@{x=1.0;y=2.5;z=3.0}}else{$null})
            expectedRotation=$(if($mounted){[ordered]@{x=0.0;y=0.0;z=0.0;w=1.0}}else{$null})
            currentPositionResidualWorldUnits=$(if($mounted){0.0}else{$null});currentRotationResidualDegrees=$(if($mounted){0.0}else{$null})
            preCorrectionPositionResidualWorldUnits=$(if($mounted){0.01}else{$null});preCorrectionRotationResidualDegrees=$(if($mounted){0.1}else{$null})
            postCorrectionPositionResidualWorldUnits=$(if($mounted){0.0}else{$null});postCorrectionRotationResidualDegrees=$(if($mounted){0.0}else{$null})
        }
        attachment=[ordered]@{
            leaseContract='parent+sibling+world-position+world-rotation+local-scale'
            leaseActive=$mounted;restoreVerified=$restored;residue=$mounted;riderParentMatchesAttachment=$mounted
            currentRiderParent=$currentParent;originalRiderParent=$originalParent;riderParentMatchesOriginal=(-not $mounted)
            currentRiderSiblingIndex=2;originalRiderSiblingIndex=2;riderSiblingIndexMatchesOriginal=$true
            currentRiderLocalScale=[ordered]@{x=1.0;y=1.0;z=1.0};originalRiderLocalScale=[ordered]@{x=1.0;y=1.0;z=1.0}
            riderLocalScaleMatchesOriginal=$true;attachmentParent=$(if($mounted){'KMC_RiderPositionAnchor'}else{$null})
            sourceAnchor=$(if($mounted){'Spine'}else{$null});riskState=$(if($mounted){'active and internally consistent'}else{'none'})
        }
        recordErrors=@($RecordErrors)
    }
}

function Write-TestLifecycleEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$EvidenceRoot,
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][array]$Records,
        [switch]$OmitManifestRecord
    )
    New-Item -ItemType Directory -Path $EvidenceRoot -Force | Out-Null
    $path = Join-Path $EvidenceRoot 'lifecycle-scenario-evidence.jsonl'
    $lines = @($Records | ForEach-Object { $_ | ConvertTo-Json -Compress -Depth 15 })
    [IO.File]::WriteAllText($path, ($lines -join [Environment]::NewLine) + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
    $artifacts = if ($OmitManifestRecord) { @() } else {
        @([ordered]@{relativePath='lifecycle-scenario-evidence.jsonl';kind='scenario-evidence';length=(Get-Item -LiteralPath $path).Length;sha256=(Get-KmcSha256 $path)})
    }
    return New-TestArtifactManifest -EvidenceRoot $EvidenceRoot -RunId $Request.runId -Scenario $Request.scenario -Artifacts $artifacts
}

function Assert-TestLifecycleEvidenceRejected {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][array]$Records,
        [Parameter(Mandatory = $true)][array]$SubscenarioResults,
        [Parameter(Mandatory = $true)][string]$FailureMessage
    )
    [void](Write-TestLifecycleEvidence -EvidenceRoot $Request.evidenceRoot -Request $Request -Records $Records)
    $manifest = Read-KmcJson (Join-Path $Request.evidenceRoot 'runtime-artifacts.json')
    $threw = $false
    try {
        Assert-KmcLifecycleScenarioEvidence -Request $Request -Manifest $manifest -Status 'PASS' -SubscenarioResults $SubscenarioResults
    }
    catch {
        $threw = $true
    }
    Assert-Test $threw $FailureMessage
}

function New-TestMovementTelemetryRecord {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][string]$Row,
        [Parameter(Mandatory = $true)][long]$Sequence
    )
    $vector = [ordered]@{x=1.0;y=2.0;z=3.0}
    return [ordered]@{
        schemaVersion=1;scenario=[string]$Request.scenario;row=$Row;runId=[string]$Request.runId;branch=[string]$Request.branch
        commit=[string]$Request.commit;productVersion=[string]$Request.productVersion;dllSha256=[string]$Request.dllSha256
        dllMvid=[string]$Request.dllMvid;sequence=$Sequence;utcTimestamp=[DateTimeOffset]::UtcNow.ToUniversalTime().ToString('o')
        riderId='movement-rider';mountId='movement-mount';relationshipState='Mounted';combat=$false;partyCombat=$false
        currentGameMode='Default';paused=$false;turnBased=$false;authoritativeMover='mount';requestedDestination=$vector
        riderStockAgentEnabled=$false;mountStockAgentEnabled=$true;riderAvoidanceDisabled=$true;mountAvoidanceDisabled=$false
        riderEntityPosition=$vector;mountEntityPosition=$vector;riderEntityOrientation=0.0;mountEntityOrientation=0.0
        riderViewPosition=$vector;mountViewPosition=$vector;riderViewRotation=$vector;mountViewRotation=$vector;anchor='KMC_RiderPositionAnchor'
        sourceAnchor='Spine';attachmentLeaseActive=$true;attachmentParent='KMC_RiderPositionAnchor';riderParentMatchesAttachment=$true
        attachmentRiskState='active and internally consistent';riderViewParent='KMC_RiderPositionAnchor'
        presentationPositionStrategy='Mammoth-root static point projected from Spine at lease acquisition'
        presentationRotationStrategy='upright authoritative-mount-root yaw plus configured rider yaw'
        expectedAnchorPosition=$vector;expectedAnchorRotation=$vector;residualPositionWorldUnits=0.0;residualRotationDegrees=0.0
        riderViewPositionResidualWorldUnits=0.0;riderEntityPositionResidualWorldUnits=0.0
        riderViewRotationResidualDegrees=0.0;riderEntityRotationResidualDegrees=0.0
        latestAuthoritativePositionSequence=1;latestCurrentAuthoritativeAnchorX=1.0;latestCurrentAuthoritativeAnchorY=2.0
        latestCurrentAuthoritativeAnchorZ=3.0;latestPreviousAuthoritativePositionSequence=$null
        latestPreviousAuthoritativeAnchorX=$null;latestPreviousAuthoritativeAnchorY=$null;latestPreviousAuthoritativeAnchorZ=$null
        latestPreviousAuthoritativePositionFrame=$null;latestPreviousAuthoritativePositionPhase=$null
        latestPreviousAuthoritativePositionReferenceKind='none';latestPreviousAuthoritativePositionSameFrame=$false
        latestPreviousAuthoritativePositionReferenceEligible=$false;latestAuthoritativePositionDeltaWorldUnits=0.0
        latestViewCurrentPositionResidualWorldUnits=0.0;latestEntityRawCurrentPositionResidualWorldUnits=0.0
        latestEntityPreviousAuthoritativePositionResidualWorldUnits=$null;latestEntityPhaseAdjustedPositionResidualWorldUnits=0.0
        latestEntityRawPositionLagBoundWorldUnits=0.0;latestEntityRawPositionLagExcessWorldUnits=0.0
        latestEntityPositionAuthorityAgeSteps=0;latestPositionPhaseLagObserved=$false;latestPositionPhaseLagPermitted=$false
        latestPositionPhaseLagViolation=$false;latestPositionRecoveryRequiredBeforeSample=$true
        latestPositionRecoveryUpdateObserved=$true;latestPositionRecoverySatisfied=$true;latestPositionRecoveryViolation=$false
        latestPositionRecoveryPendingAfterSample=$false;latestPositionStationaryAuthority=$true
        latestStationaryPositionCorrectionViolation=$false
        latestSynchronizationFrame=12;latestAuthoritativeYawSequence=1;latestCurrentAuthoritativeYawDegrees=8.0
        latestCurrentMountEntityAuthoritativeYawDegrees=8.0;latestMountEntityRootYawResidualDegrees=0.0
        latestPreviousAuthoritativeYawSequence=$null;latestPreviousAuthoritativeYawDegrees=$null;latestPreviousAuthoritativeFrame=$null
        latestPreviousAuthoritativePhase=$null;latestPreviousAuthoritativeReferenceKind='none';latestPreviousAuthoritativeSameFrame=$false
        latestPreviousAuthoritativeReferenceEligible=$false;latestAuthoritativeYawDeltaDegrees=0.0;latestViewCurrentYawResidualDegrees=0.0
        latestFullViewCurrentRotationResidualDegrees=0.0
        latestEntityRawCurrentYawResidualDegrees=0.0;latestEntityPreviousAuthoritativeYawResidualDegrees=$null
        latestEntityPhaseAdjustedYawResidualDegrees=0.0;latestEntityRawLagBoundDegrees=0.0;latestEntityRawLagExcessDegrees=0.0
        latestEntityYawAuthorityAgeSteps=0;latestPhaseLagObserved=$false;latestPhaseLagPermitted=$false;latestPhaseLagViolation=$false
        latestRecoveryRequiredBeforeSample=$true;latestRecoveryUpdateObserved=$true;latestRecoverySatisfied=$true
        latestRecoveryViolation=$false;latestRecoveryPendingAfterSample=$false;latestStationaryAuthority=$true
        latestStationaryYawCorrectionViolation=$false
        riderSelected=$true;mountSelected=$false;selectedUnitIds=@('movement-rider');riderCommandCount=0;mountCommandCount=1
        riderActiveCommandTypes=@();mountActiveCommandTypes=@('Kingmaker.UnitLogic.Commands.UnitMoveTo');mountIsReallyMoving=$true
        mountVelocity=$vector;mountSpeed=3.0;mountMoveDirection=$vector;mountPathId=1;mountPathFailed=$false;mountRepathNeeded=$false
        mountPathError=0;mountPathErrorLog=$null;mountPathPointCount=2;mountPathLength=5.0;synchronizationPhase='Update'
        synchronizationSampleCount=6;synchronizationCorrectionCount=2;initialConfigurationSynchronizationSampleCount=1
        initialConfigurationSynchronizationCorrectionCount=1;updateSynchronizationSampleCount=3;updateSynchronizationCorrectionCount=1
        lateUpdateSynchronizationSampleCount=2;lateUpdateSynchronizationCorrectionCount=0;preCorrectionPositionResidualWorldUnits=0.0
        preCorrectionRawCurrentPositionResidualWorldUnits=0.0;preCorrectionViewCurrentPositionResidualWorldUnits=0.0
        preCorrectionRotationResidualDegrees=0.01;postCorrectionPositionResidualWorldUnits=0.0;postCorrectionRotationResidualDegrees=0.0
        maximumPreCorrectionPositionResidualWorldUnits=1.0;maximumPreCorrectionRawCurrentPositionResidualWorldUnits=1.0
        maximumPreCorrectionRotationResidualDegrees=10.0
        maximumPostCorrectionPositionResidualWorldUnits=0.0;maximumPostCorrectionRotationResidualDegrees=0.0
        maximumInitialConfigurationPreCorrectionPositionResidualWorldUnits=1.0;maximumUpdatePreCorrectionPositionResidualWorldUnits=0.01
        maximumUpdatePreCorrectionRawCurrentPositionResidualWorldUnits=0.20
        maximumUpdatePreCorrectionRotationResidualDegrees=0.01;maximumUpdatePostCorrectionPositionResidualWorldUnits=0.0
        maximumUpdatePostCorrectionRotationResidualDegrees=0.0;maximumLateUpdatePreCorrectionPositionResidualWorldUnits=0.01
        maximumLateUpdatePreCorrectionRawCurrentPositionResidualWorldUnits=0.20
        maximumLateUpdatePreCorrectionRotationResidualDegrees=0.01;maximumLateUpdatePostCorrectionPositionResidualWorldUnits=0.0
        maximumLateUpdatePostCorrectionRotationResidualDegrees=0.0;maximumCalibratedViewCurrentPositionResidualWorldUnits=0.0
        maximumCalibratedEntityRawCurrentPositionResidualWorldUnits=0.20
        maximumCalibratedEntityPreviousAuthoritativePositionResidualWorldUnits=0.0
        maximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits=0.0;maximumAuthoritativePositionDeltaWorldUnits=0.20
        maximumEntityRawPositionLagExcessWorldUnits=0.0;entityRawPositionLagArithmeticCoherenceEpsilonWorldUnits=0.0001
        positionPhaseLagObservedCount=1;positionPhaseLagPermittedCount=1;positionPhaseLagSameFrameUpdateReferenceCount=1
        positionPhaseLagEligibleReferenceCount=1;positionPhaseLagViolationCount=0;positionPhaseLagRecoveryRequiredRawCount=1
        positionPhaseLagRecoveryUpdateRawCount=1;positionPhaseLagRecoverySatisfiedRawCount=1
        positionPhaseLagRecoveryRequiredEffectiveCount=1;positionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount=1
        positionPhaseLagRecoverySatisfiedEffectiveCount=1;positionPhaseLagRecoveryViolationCount=0
        stationaryPositionCorrectionViolationCount=0;outstandingPositionPhaseLagRecoveryCount=0
        maximumConsecutiveUnrecoveredPositionPhaseLagCount=1;maximumCalibratedViewCurrentYawResidualDegrees=0.0
        maximumCalibratedFullViewCurrentRotationResidualDegrees=0.0
        maximumCalibratedMountEntityRootYawResidualDegrees=0.0;maximumCalibratedEntityRawCurrentYawResidualDegrees=8.0
        maximumCalibratedEntityPreviousAuthoritativeYawResidualDegrees=0.0;maximumCalibratedEntityPhaseAdjustedYawResidualDegrees=0.0
        maximumAuthoritativeYawDeltaDegrees=8.0;maximumEntityRawLagExcessDegrees=0.0
        entityRawLagArithmeticCoherenceEpsilonDegrees=0.0001;phaseLagObservedCount=1
        phaseLagPermittedCount=1;phaseLagSameFrameUpdateReferenceCount=1;phaseLagEligibleReferenceCount=1;phaseLagViolationCount=0
        phaseLagRecoveryRequiredCount=1;phaseLagRecoveryUpdateCount=1;phaseLagRecoverySatisfiedCount=1
        phaseLagRecoveryRequiredRawCount=1;phaseLagRecoveryUpdateRawCount=1;phaseLagRecoverySatisfiedRawCount=1
        phaseLagRecoveryRequiredEffectiveCount=1;phaseLagRecoveryUpdateOrBoundaryEffectiveCount=1
        phaseLagRecoverySatisfiedEffectiveCount=1
        phaseLagRecoveryViolationCount=0;stationaryYawCorrectionViolationCount=0;outstandingPhaseLagRecoveryCount=0
        maximumConsecutiveUnrecoveredPhaseLagCount=1;stationaryBoundaryClosureAttemptCount=0
        stationaryBoundaryClosureSucceededCount=0;stationaryBoundaryClosureFailedCount=0
        yawPhaseLagStationaryBoundaryClosureCount=0;positionPhaseLagStationaryBoundaryClosureCount=0
        maximumResidualWorldUnits=0.0;maximumRotationResidualDegrees=0.0
    }
}

function New-TestMovementPathProbeRecord {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][string]$Row,
        [Parameter(Mandatory = $true)][long]$Sequence,
        [ValidateSet('Generic','DoorNear','DoorFar')][string]$Target = 'Generic',
        [bool]$StrictDoor = $false
    )
    $requested = switch ($Target) {
        'DoorNear' { [ordered]@{x=1.0;y=2.0;z=3.0}; break }
        'DoorFar' { [ordered]@{x=4.0;y=2.0;z=3.0}; break }
        default { [ordered]@{x=1.0;y=2.0;z=3.0}; break }
    }
    return [ordered]@{
        schemaVersion=1;runId=[string]$Request.runId;scenario=[string]$Request.scenario;row=$Row;branch=[string]$Request.branch
        commit=[string]$Request.commit;productVersion=[string]$Request.productVersion;dllSha256=[string]$Request.dllSha256
        dllMvid=[string]$Request.dllMvid;sequence=$Sequence;utcTimestamp=[DateTimeOffset]::UtcNow.ToUniversalTime().ToString('o')
        kind='path-probe';requested=$requested;endpoint=$requested
        pathLength=3.0;accepted=$true;strictDoor=$StrictDoor
    }
}

function Get-TestMovementScreenshotMilestones {
    param([Parameter(Mandatory = $true)][string]$Row, [bool]$DoorApproachSkipped = $false)
    switch ($Row) {
        'mounted-pair-doorway' {
            if ($DoorApproachSkipped) { return @('door-control','door-mounted','door-mounted','dismounted') }
            return @('door-control','door-control','door-mounted','door-mounted','dismounted')
        }
        'mounted-pair-open-ground' { return @('mounted-idle','moving','stopped','dismounted') }
        'mounted-pair-stop-start' { return @('mounted-idle','moving','stopped','restarted','dismounted') }
        'mounted-pair-turns-and-corners' { return @('mounted-idle','moving','corner','corner','dismounted') }
        'mounted-pair-selection' { return @('mounted-idle','selection','moving','dismounted') }
        'mounted-pair-party-formation' { return @('mounted-idle','formation','formation','dismounted') }
        'mounted-pair-pause-unpause' { return @('mounted-idle','moving','paused','dismounted') }
        'mounted-pair-destination-cancel' { return @('mounted-idle','moving','cancelled','dismounted') }
        default { throw "No test screenshot contract exists for movement row $Row." }
    }
}

function New-TestMovementScreenshotRecords {
    param([Parameter(Mandatory = $true)][string]$Row, [bool]$DoorApproachSkipped = $false)
    $counts = @{}
    $records = New-Object 'Collections.Generic.List[object]'
    $rowToken = $Row.Substring('mounted-pair-'.Length)
    foreach ($milestone in @(Get-TestMovementScreenshotMilestones $Row $DoorApproachSkipped)) {
        $count = if ($counts.ContainsKey($milestone)) { [int]$counts[$milestone] + 1 } else { 1 }
        $counts[$milestone] = $count
        $relativePath = 'movement-visuals/' + $rowToken + '-' + $milestone + '-' + $count.ToString('00') + '.png'
        $bytes = [Text.Encoding]::UTF8.GetBytes($relativePath)
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $hash = ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-','').ToLowerInvariant() }
        finally { $sha.Dispose() }
        $records.Add([ordered]@{milestone=$milestone;relativePath=$relativePath;length=[long]$bytes.LongLength;sha256=$hash})
    }
    return $records.ToArray()
}

function New-TestMovementRowRecord {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][string]$Row,
        [Parameter(Mandatory = $true)][long]$Sequence,
        [int]$AssertionPassCount = 20
    )
    $before = [ordered]@{
        trigger='Manual';relationshipState='Mounted';hasMountedResidual=$true;riderStockAgentEnabled=$false;mountStockAgentEnabled=$true
        riderAvoidanceDisabled=$true;mountAvoidanceDisabled=$false;riderOverridePresent=$true;mountOverridePresent=$false
        riderSelected=$true;mountSelected=$false;selectedUnitIds=@('movement-rider');paused=$false;riderForbidRotation=$true
        attachmentLeaseActive=$true;attachmentRestoreVerified=$false;attachmentResidue=$true;riderParentMatchesAttachment=$true
        riderParent='MastodonPet/KMC_RiderPositionAnchor';attachmentParent='KMC_RiderPositionAnchor';sourceAnchor='Spine'
        attachmentRiskState='active and internally consistent'
    }
    $after = [ordered]@{
        trigger='Manual';relationshipState='Unmounted';hasMountedResidual=$false;riderStockAgentEnabled=$true;mountStockAgentEnabled=$true
        riderAvoidanceDisabled=$false;mountAvoidanceDisabled=$false;riderOverridePresent=$false;mountOverridePresent=$false
        riderSelected=$true;mountSelected=$false;selectedUnitIds=@('movement-rider');paused=$false;riderForbidRotation=$false
        attachmentLeaseActive=$false;attachmentRestoreVerified=$true;attachmentResidue=$false;riderParentMatchesAttachment=$false
        riderParent='Area/Units/Rider';attachmentParent=$null;sourceAnchor=$null;attachmentRiskState='none'
    }
    $formation = $Row -ceq 'mounted-pair-party-formation'
    $doorway = $Row -ceq 'mounted-pair-doorway'
    $stopStart = $Row -ceq 'mounted-pair-stop-start'
    $turns = $Row -ceq 'mounted-pair-turns-and-corners'
    $selection = $Row -ceq 'mounted-pair-selection'
    $pause = $Row -ceq 'mounted-pair-pause-unpause'
    $cancel = $Row -ceq 'mounted-pair-destination-cancel'
    $waypointCount = if ($doorway -or $turns) { 3 } elseif ($stopStart) { 2 } else { 1 }
    $endpointQualifiedWaypointCount = if ($cancel) { 0 } elseif ($stopStart) { 1 } else { $waypointCount }
    return [ordered]@{
        schemaVersion=1;runId=[string]$Request.runId;scenario=[string]$Request.scenario;row=$Row;branch=[string]$Request.branch
        commit=[string]$Request.commit;productVersion=[string]$Request.productVersion;dllSha256=[string]$Request.dllSha256
        dllMvid=[string]$Request.dllMvid;sequence=$Sequence;utcTimestamp=[DateTimeOffset]::UtcNow.ToUniversalTime().ToString('o')
        kind='movement-row-result';status='PASS';assertionPassCount=$AssertionPassCount;assertionFailCount=0
        maximumPreCorrectionResidualWorldUnits=1.0;maximumInitialConfigurationResidualWorldUnits=1.0
        maximumUpdatePreCorrectionResidualWorldUnits=0.01;maximumLateUpdatePreCorrectionResidualWorldUnits=0.01
        maximumUpdatePreCorrectionRotationResidualDegrees=0.01;maximumLateUpdatePreCorrectionRotationResidualDegrees=0.01
        maximumPostCorrectionResidualWorldUnits=0.0;maximumPostCorrectionRotationResidualDegrees=0.0
        maximumRawCurrentPositionResidualWorldUnits=1.0;maximumUpdateRawCurrentPositionResidualWorldUnits=0.20
        maximumLateUpdateRawCurrentPositionResidualWorldUnits=0.20;maximumViewCurrentPositionResidualWorldUnits=0.0
        maximumEntityRawCurrentPositionResidualWorldUnits=0.20
        maximumEntityPreviousAuthoritativePositionResidualWorldUnits=0.0
        maximumEntityPhaseAdjustedPositionResidualWorldUnits=0.0;maximumAuthoritativePositionDeltaWorldUnits=0.20
        maximumEntityRawPositionLagExcessWorldUnits=0.0;entityRawPositionLagArithmeticCoherenceEpsilonWorldUnits=0.0001
        positionPhaseLagObservedCount=1;positionPhaseLagPermittedCount=1;positionPhaseLagSameFrameUpdateReferenceCount=1
        positionPhaseLagEligibleReferenceCount=1;positionPhaseLagViolationCount=0;positionPhaseLagRecoveryRequiredRawCount=0
        positionPhaseLagRecoveryUpdateRawCount=0;positionPhaseLagRecoverySatisfiedRawCount=0
        positionPhaseLagRecoveryRequiredEffectiveCount=1;positionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount=1
        positionPhaseLagRecoverySatisfiedEffectiveCount=1;positionPhaseLagRecoveryViolationCount=0
        stationaryPositionCorrectionViolationCount=0;outstandingPositionPhaseLagRecoveryCount=0
        maximumConsecutiveUnrecoveredPositionPhaseLagCount=1;maximumViewCurrentYawResidualDegrees=0.0
        maximumFullViewCurrentRotationResidualDegrees=0.0
        maximumMountEntityRootYawResidualDegrees=0.0
        maximumEntityRawCurrentYawResidualDegrees=8.0;maximumEntityPreviousAuthoritativeYawResidualDegrees=0.0
        maximumEntityPhaseAdjustedYawResidualDegrees=0.0;maximumAuthoritativeYawDeltaDegrees=8.0
        maximumEntityRawLagExcessDegrees=0.0;entityRawLagArithmeticCoherenceEpsilonDegrees=0.0001
        phaseLagObservedCount=1;phaseLagPermittedCount=1
        phaseLagSameFrameUpdateReferenceCount=1;phaseLagEligibleReferenceCount=1;phaseLagViolationCount=0
        phaseLagRecoveryRequiredCount=0;phaseLagRecoveryUpdateCount=0;phaseLagRecoverySatisfiedCount=0
        phaseLagRecoveryRequiredRawCount=0;phaseLagRecoveryUpdateRawCount=0;phaseLagRecoverySatisfiedRawCount=0
        phaseLagRecoveryRequiredEffectiveCount=1;phaseLagRecoveryUpdateOrBoundaryEffectiveCount=1
        phaseLagRecoverySatisfiedEffectiveCount=1
        phaseLagRecoveryViolationCount=0;stationaryYawCorrectionViolationCount=0;outstandingPhaseLagRecoveryCount=0
        maximumConsecutiveUnrecoveredPhaseLagCount=1;finalSynchronizationSnapshotCaptured=$true
        finalSynchronizationSnapshotStage='pre-dismount-after-captures';finalSynchronizationSnapshotFrame=15
        finalSynchronizationAgentFrame=15;finalSynchronizationSampleCount=21;finalSynchronizationOutstandingRecoveryCount=0
        finalSynchronizationOutstandingPositionRecoveryCount=0
        finalSynchronizationQualificationPassed=$true;finalSynchronizationMovementStoppedBeforeSnapshot=$true
        finalSynchronizationBoundaryPositionResidualWorldUnits=0.0;finalSynchronizationBoundaryViewPositionResidualWorldUnits=0.0
        finalSynchronizationBoundaryEntityPositionResidualWorldUnits=0.0
        finalSynchronizationBoundaryFullViewRotationResidualDegrees=0.0;finalSynchronizationBoundaryViewYawResidualDegrees=0.0
        finalSynchronizationBoundaryEntityCurrentYawResidualDegrees=0.0;finalSynchronizationBoundaryMountEntityRootYawResidualDegrees=0.0
        finalSynchronizationBoundaryAuthoritativePositionAdvanceWorldUnits=0.0
        finalSynchronizationBoundaryAuthoritativeYawAdvanceDegrees=0.0;finalSynchronizationBoundaryMovementCommandAbsent=$true
        finalSynchronizationBoundaryWantsToMove=$false;finalSynchronizationBoundaryIsReallyMoving=$false
        finalSynchronizationBoundaryClosureAttempted=$true;finalSynchronizationBoundaryClosureSucceeded=$true
        finalSynchronizationBoundaryClosureReason='closed-at-stationary-boundary';finalSynchronizationBoundaryYawPendingBefore=1
        finalSynchronizationBoundaryPositionPendingBefore=1;finalSynchronizationBoundaryYawClosedCount=1
        finalSynchronizationBoundaryPositionClosedCount=1;finalSynchronizationBoundaryYawPendingAfter=0
        finalSynchronizationBoundaryPositionPendingAfter=0;stationaryBoundaryClosureAttemptCount=1
        stationaryBoundaryClosureSucceededCount=1;stationaryBoundaryClosureFailedCount=0
        yawPhaseLagStationaryBoundaryClosureCount=1;positionPhaseLagStationaryBoundaryClosureCount=1
        synchronizationObservationCount=12;updateSynchronizationSampleCount=10;lateUpdateSynchronizationSampleCount=10
        updateSynchronizationCorrectionCount=1;lateUpdateSynchronizationCorrectionCount=1;maximumStationaryDriftWorldUnits=0.01
        maximumStuckSeconds=0.1;oscillationCount=0;unexpectedRepathCount=0;commandReplacementCount=0;selectionLossCount=0
        waypointCount=$waypointCount;endpointQualifiedWaypointCount=$endpointQualifiedWaypointCount
        maximumCompletedLegFinalTargetDistanceWorldUnits=$(if($cancel){0.0}else{0.5})
        maximumCompletedLegBestTargetDistanceWorldUnits=$(if($cancel){0.0}else{0.4})
        maximumTurnDegrees=$(if($turns){90.0}else{0.0});nonPairInterferenceCount=0
        nonPairUnitId=$(if($selection -or $formation){'movement-non-pair'}else{$null});mountFinalTargetDistanceWorldUnits=$(if($cancel){0.0}else{0.5})
        nonPairBestTargetDistanceWorldUnits=$(if($formation){0.4}else{0.0});nonPairFinalTargetDistanceWorldUnits=$(if($formation){0.5}else{0.0})
        minimumPairNonPairSeparationWorldUnits=$(if($formation){3.0}else{0.0});requiredPairNonPairSeparationWorldUnits=$(if($formation){2.0}else{0.0})
        unmountedDoorControlPassed=$doorway;doorApproachSkipped=$false;stopCommandIssuedCount=$(if($stopStart -or $cancel){1}else{0})
        restartCompleted=$stopStart;selectionMountNormalized=$selection;selectionSwitchedAway=$selection;selectionSwitchedBack=$selection
        formationSelectionNormalized=$formation;pauseEntered=$pause;pauseObservationSeconds=$(if($pause){1.1}else{0.0})
        pauseMaximumDriftWorldUnits=$(if($pause){0.01}else{0.0});pauseExited=$pause
        destinationCancelCommandAbsent=$cancel;destinationCancelRelationshipPreserved=$cancel
        cleanupTrigger='Manual';cleanupSucceeded=$true;cleanupResult='state=Unmounted'
        cleanupResidual=$false;cleanupBefore=$before;cleanupAfter=$after
        selectionCoverage='SelectionManager.SelectedUnits only; active portrait and camera-follow state are not asserted.'
        formationCoverage='Stock group-command recipients and corpulence clearance only; formation-slot persistence is not asserted.'
        door=$(if($doorway){'Area/Door'}else{$null});doorNear=[ordered]@{x=1.0;y=2.0;z=3.0};doorFar=[ordered]@{x=4.0;y=2.0;z=3.0}
        screenshots=@(New-TestMovementScreenshotRecords $Row $false);screenshotCaptureErrors=@();errors=@()
    }
}

function Write-TestMovementEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$EvidenceRoot,
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][array]$TelemetryRecords,
        [Parameter(Mandatory = $true)][array]$ScenarioRecords,
        [switch]$OmitTelemetryManifest,
        [switch]$OmitScenarioManifest
    )
    New-Item -ItemType Directory -Path $EvidenceRoot -Force | Out-Null
    $telemetryPath = Join-Path $EvidenceRoot 'movement-telemetry.jsonl'
    $scenarioPath = Join-Path $EvidenceRoot 'movement-scenario-evidence.jsonl'
    [IO.File]::WriteAllText($telemetryPath, (@($TelemetryRecords | ForEach-Object { $_ | ConvertTo-Json -Compress -Depth 20 }) -join [Environment]::NewLine) + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
    [IO.File]::WriteAllText($scenarioPath, (@($ScenarioRecords | ForEach-Object { $_ | ConvertTo-Json -Compress -Depth 20 }) -join [Environment]::NewLine) + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
    $artifacts = New-Object 'Collections.Generic.List[object]'
    if (-not $OmitTelemetryManifest) { $artifacts.Add([ordered]@{relativePath='movement-telemetry.jsonl';kind='telemetry';length=(Get-Item $telemetryPath).Length;sha256=(Get-KmcSha256 $telemetryPath)}) }
    if (-not $OmitScenarioManifest) { $artifacts.Add([ordered]@{relativePath='movement-scenario-evidence.jsonl';kind='scenario-evidence';length=(Get-Item $scenarioPath).Length;sha256=(Get-KmcSha256 $scenarioPath)}) }
    foreach ($rowRecord in @($ScenarioRecords | Where-Object { [string]$_.kind -ceq 'movement-row-result' })) {
        foreach ($screenshot in @($rowRecord.screenshots)) {
            $screenshotPath = Join-Path $EvidenceRoot ([string]$screenshot.relativePath).Replace('/', '\')
            $screenshotDirectory = Split-Path -Parent $screenshotPath
            New-Item -ItemType Directory -Path $screenshotDirectory -Force | Out-Null
            [IO.File]::WriteAllBytes($screenshotPath, [Text.Encoding]::UTF8.GetBytes([string]$screenshot.relativePath))
            $artifacts.Add([ordered]@{relativePath=[string]$screenshot.relativePath;kind='screenshot';length=(Get-Item $screenshotPath).Length;sha256=(Get-KmcSha256 $screenshotPath)})
        }
    }
    return New-TestArtifactManifest -EvidenceRoot $EvidenceRoot -RunId $Request.runId -Scenario $Request.scenario -Artifacts $artifacts.ToArray()
}

New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    $emptyRoot = Join-Path $testRoot 'empty-root'
    New-Item -ItemType Directory -Path $emptyRoot -Force | Out-Null
    Invoke-HarnessTest 'tree manifest represents an empty root' {
        $emptyManifest = Get-KmcDirectoryManifest $emptyRoot
        Assert-Test ($emptyManifest.fileCount -eq 0 -and $emptyManifest.directoryCount -eq 0 -and $emptyManifest.totalBytes -eq 0) 'empty tree totals are not exact'
        Assert-Test (-not [string]::IsNullOrWhiteSpace([string]$emptyManifest.digest)) 'empty tree digest is missing'
    }
    Invoke-HarnessTest 'protected-save metadata represents an empty root' {
        $emptySaveMetadata = Get-KmcProtectedSaveMetadata $emptyRoot
        Assert-Test ($emptySaveMetadata.fileCount -eq 0 -and $emptySaveMetadata.totalBytes -eq 0) 'empty save metadata totals are not exact'
        Assert-Test (-not [string]::IsNullOrWhiteSpace([string]$emptySaveMetadata.digest)) 'empty save metadata digest is missing'
    }
    Invoke-HarnessTest 'stable no-game-process wait accepts consecutive empty samples' {
        Assert-Test (Wait-KmcStableNoKingmakerProcess -StableSamples 2 -IntervalMilliseconds 1 -TimeoutSeconds 1) 'stable empty process interval was not accepted'
    }

    $manifestRoot = Join-Path $testRoot 'manifest'
    New-Item -ItemType Directory -Path (Join-Path $manifestRoot 'empty') -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $manifestRoot 'one.txt'), 'one')

    Invoke-HarnessTest 'tree manifest is deterministic' {
        $first = Get-KmcDirectoryManifest $manifestRoot
        $second = Get-KmcDirectoryManifest $manifestRoot
        Assert-Test ($first.digest -ceq $second.digest) 'identical trees produced different digests'
        Assert-Test ($first.fileCount -eq 1 -and $first.directoryCount -eq 1) 'tree counts are wrong'
    }

    Invoke-HarnessTest 'tree manifest detects mutation' {
        $before = Get-KmcDirectoryManifest $manifestRoot
        [IO.File]::WriteAllText((Join-Path $manifestRoot 'one.txt'), 'changed')
        $after = Get-KmcDirectoryManifest $manifestRoot
        Assert-Test ($before.digest -cne $after.digest) 'file mutation did not change digest'
    }

    $stateRoot = Join-Path $testRoot 'state'
    New-Item -ItemType Directory -Path $stateRoot -Force | Out-Null
    Invoke-HarnessTest 'stale lock is rejected' {
        $lockPath = Join-Path $stateRoot 'active-transaction.lock'
        [IO.File]::WriteAllText($lockPath, 'stale')
        $threw = $false
        try { Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'stale-test' | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'existing lock was accepted'
        Remove-Item -LiteralPath $lockPath -Force
    }

    Invoke-HarnessTest 'exclusive lock is held and released' {
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'lock-test'
        try {
            $payload = Assert-KmcRuntimeLockOwner $lock
            Assert-Test ([string]$payload.token -ceq [string]$lock.Token) 'lock token did not round-trip'
            $threw = $false
            try { [IO.File]::OpenWrite($lock.Path).Dispose() } catch { $threw = $true }
            Assert-Test $threw 'lock file was not exclusive'
        }
        finally {
            Close-KmcRuntimeLock $lock
        }
        Assert-Test (-not (Test-Path -LiteralPath (Join-Path $stateRoot 'active-transaction.lock'))) 'lock sentinel remained'
    }

    $packageSource = Join-Path $testRoot 'package-source\KingmakerMountedCombat'
    New-Item -ItemType Directory -Path $packageSource -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $packageSource 'Info.json'), '{}')
    [IO.File]::WriteAllText((Join-Path $packageSource 'KingmakerMountedCombat.dll'), 'fixture')
    $package = Join-Path $testRoot 'fixture.zip'
    Compress-Archive -LiteralPath $packageSource -DestinationPath $package

    $live = Join-Path $testRoot 'game\Mods'
    New-Item -ItemType Directory -Path (Join-Path $live 'ExistingMod') -Force | Out-Null
    [IO.File]::WriteAllText((Join-Path $live 'ExistingMod\payload.txt'), 'preserve-me')
    New-Item -ItemType Directory -Path (Join-Path $live 'EmptyMod') | Out-Null
    $backup = Join-Path $testRoot 'backups'
    $staging = Join-Path $testRoot 'staging'
    $original = Get-KmcDirectoryManifest $live
    $script:transactionState = $null

    Invoke-HarnessTest 'transaction stages exact live clone plus KMC overlay and restores exact tree' {
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'transaction-test'
        try {
            $script:transactionState = Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $live -PackagePath $package -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging
            $staged = Get-KmcDirectoryManifest $live
            Assert-Test ($staged.fileCount -eq ($original.fileCount + 3)) 'staged file count did not preserve the live payload plus two KMC files and ownership sentinel'
            Assert-Test ($staged.directoryCount -eq ($original.directoryCount + 1)) 'staged directory count did not preserve live directories plus the KMC root'
            Assert-Test ((Get-Content -Raw -LiteralPath (Join-Path $live 'ExistingMod\payload.txt')) -ceq 'preserve-me') 'existing live mod payload was not preserved in the staged clone'
            Assert-Test (Test-Path -LiteralPath (Join-Path $live 'EmptyMod') -PathType Container) 'empty live mod directory was not preserved in the staged clone'
            Assert-Test (Test-Path -LiteralPath (Join-Path $live 'KingmakerMountedCombat\Info.json')) 'KMC package root missing'
            $sentinel = Read-KmcLiveSentinel $live
            Assert-Test ([string]$sentinel.token -ceq [string]$lock.Token) 'live sentinel does not bind the lock token'
            $prepared = Read-KmcJson $script:transactionState
            Assert-Test ([int]$prepared.schemaVersion -eq 3) 'new Mods transaction did not use schema 3'
            Assert-Test ([string]$prepared.stagingMode -ceq 'live-clone-plus-kmc-overlay') 'new Mods transaction did not record its exact staging mode'
            Assert-Test ([string]$prepared.cloneBaseDigest -ceq [string]$prepared.beforeDigest) 'durable state does not prove an exact clone base digest'
            Assert-Test ([int]$prepared.cloneBaseFileCount -eq [int]$prepared.beforeFileCount) 'durable clone base file count differs from preflight'
            Assert-Test ([int]$prepared.cloneBaseDirectoryCount -eq [int]$prepared.beforeDirectoryCount) 'durable clone base directory count differs from preflight'
            Assert-Test ([long]$prepared.cloneBaseTotalBytes -eq [long]$prepared.beforeTotalBytes) 'durable clone base byte count differs from preflight'
            $restored = Restore-KmcModsTransaction -Lock $lock -StatePath $script:transactionState -LiveModsRoot $live -BackupRoot $backup -StagingRoot $staging
            Assert-Test ($restored.digest -ceq $original.digest) 'restored digest differs'
            $again = Restore-KmcModsTransaction -Lock $lock -StatePath $script:transactionState -LiveModsRoot $live -BackupRoot $backup -StagingRoot $staging
            Assert-Test ($again.digest -ceq $original.digest) 'idempotent restore differs'
        }
        finally {
            Close-KmcRuntimeLock $lock
        }
    }

    Invoke-HarnessTest 'restored transaction state is durable' {
        $state = Read-KmcJson $script:transactionState
        Assert-Test ([string]$state.phase -ceq 'restored') 'transaction did not durably record restored phase'
        Assert-Test ([string]$state.restoredDigest -ceq $original.digest) 'durable restored digest differs'
    }

    Invoke-HarnessTest 'original-moved crash window with absent live Mods is recoverable' {
        $missingLive = Join-Path $testRoot 'missing-live-game\Mods'
        New-Item -ItemType Directory -Path (Join-Path $missingLive 'ExistingMod') -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $missingLive 'ExistingMod\payload.txt'), 'preserve-missing-live')
        $missingOriginal = Get-KmcDirectoryManifest $missingLive
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'missing-live-test'
        try {
            $statePath = Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $missingLive -PackagePath $package -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging
            $state = Read-KmcJson $statePath
            Move-Item -LiteralPath $missingLive -Destination ([string]$state.stagedReady)
            $state.phase = 'original-moved'
            Write-KmcJsonAtomic $statePath $state
            Assert-Test (-not (Test-Path -LiteralPath $missingLive)) 'synthetic original-moved window retained a live Mods root'
            $restored = Restore-KmcModsTransaction -Lock $lock -StatePath $statePath -LiveModsRoot $missingLive -BackupRoot $backup -StagingRoot $staging
            Assert-Test ($restored.digest -ceq $missingOriginal.digest) 'original-moved crash recovery digest differs'
            Assert-Test ((Get-Content -Raw -LiteralPath (Join-Path $missingLive 'ExistingMod\payload.txt')) -ceq 'preserve-missing-live') 'original-moved crash recovery lost original content'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'recovery wrapper admits only proven absent-live original-moved state' {
        $recoverySource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\Recover-KingmakerRuntimeTransaction.ps1')
        $transactionCommonSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
        Assert-Test ($recoverySource.Contains('$liveModsExistedBefore=Test-Path')) 'recovery wrapper still unconditionally requires a live Mods manifest'
        Assert-Test ($recoverySource.Contains("[string]`$modsState.phase-cne'original-moved'")) 'recovery wrapper does not require the exact original-moved phase when live Mods is absent'
        Assert-Test ($recoverySource.Contains("Assert-KmcManifestMatchesState (Get-KmcDirectoryManifest `$recordedReady) `$modsState 'staged'")) 'recovery wrapper does not prove the unactivated staged tree when live Mods is absent'
        Assert-Test (-not $transactionCommonSource.Contains('throw new AggregateException')) 'Mods entry rollback still uses invalid PowerShell throw-new syntax'
        Assert-Test ($transactionCommonSource.Contains('if ($workingFile.LastWriteTimeUtc.Ticks -ne [long]$state.workingLastWriteTimeUtcTicks)')) 'unchanged Working restoration still rewrites an already exact timestamp'
    }

    Invoke-HarnessTest 'case-insensitive existing KMC collision is rejected before live mutation' {
        $collisionLive = Join-Path $testRoot 'collision-game\Mods'
        New-Item -ItemType Directory -Path (Join-Path $collisionLive 'kingmakermountedcombat') -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $collisionLive 'kingmakermountedcombat\unknown.txt'), 'unknown prior KMC tree')
        $collisionBefore = Get-KmcDirectoryManifest $collisionLive
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'collision-test'
        try {
            $threw = $false
            try { Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $collisionLive -PackagePath $package -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'case-insensitive existing KMC collision was accepted'
            Assert-Test ((Get-KmcDirectoryManifest $collisionLive).digest -ceq $collisionBefore.digest) 'collision rejection changed live Mods'
            Assert-Test (-not (Test-Path -LiteralPath (Get-KmcTransactionStatePath $stateRoot 'collision-test'))) 'collision rejection wrote transaction state'
            Assert-Test (-not (Test-Path -LiteralPath (Join-Path $backup 'collision-test'))) 'collision rejection created a backup run'
            Assert-Test (-not (Test-Path -LiteralPath (Join-Path $staging 'collision-test'))) 'collision rejection created a staging run'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'descendant reparse point is rejected before live mutation' {
        $reparseLive = Join-Path $testRoot 'reparse-game\Mods'
        $reparseMod = Join-Path $reparseLive 'ExistingMod'
        $reparseTarget = Join-Path $testRoot 'reparse-target'
        New-Item -ItemType Directory -Path $reparseMod -Force | Out-Null
        New-Item -ItemType Directory -Path $reparseTarget -Force | Out-Null
        $junction = Join-Path $reparseMod 'linked-content'
        New-Item -ItemType Junction -Path $junction -Target $reparseTarget | Out-Null
        Assert-Test (((Get-Item -LiteralPath $junction -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) 'test junction was not a detectable reparse point'
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'reparse-test'
        try {
            $threw = $false
            try { Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $reparseLive -PackagePath $package -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'descendant reparse point was accepted for cloning'
            Assert-Test (-not (Test-Path -LiteralPath (Get-KmcTransactionStatePath $stateRoot 'reparse-test'))) 'reparse rejection wrote transaction state'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'detectable descendant hard link is rejected before live mutation' {
        $hardLinkLive = Join-Path $testRoot 'hardlink-game\Mods'
        $hardLinkMod = Join-Path $hardLinkLive 'ExistingMod'
        New-Item -ItemType Directory -Path $hardLinkMod -Force | Out-Null
        $hardLinkSource = Join-Path $hardLinkMod 'source.txt'
        $hardLinkAlias = Join-Path $hardLinkMod 'alias.txt'
        [IO.File]::WriteAllText($hardLinkSource, 'linked payload')
        New-Item -ItemType HardLink -Path $hardLinkAlias -Target $hardLinkSource | Out-Null
        $linkTypeProperty = (Get-Item -LiteralPath $hardLinkAlias -Force).PSObject.Properties['LinkType']
        Assert-Test ($null -ne $linkTypeProperty -and [string]$linkTypeProperty.Value -ceq 'HardLink') 'test hard link was not detectable through the guarded PowerShell surface'
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'hardlink-test'
        try {
            $threw = $false
            try { Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $hardLinkLive -PackagePath $package -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'detectable descendant hard link was accepted for cloning'
            Assert-Test (-not (Test-Path -LiteralPath (Get-KmcTransactionStatePath $stateRoot 'hardlink-test'))) 'hard-link rejection wrote transaction state'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'clone and source manifest mismatches are rejected by entry proofs' {
        $mismatchSource = Join-Path $testRoot 'clone-mismatch-source'
        $mismatchClone = Join-Path $testRoot 'clone-mismatch-result'
        New-Item -ItemType Directory -Path (Join-Path $mismatchSource 'EmptyMod') -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $mismatchSource 'payload.txt'), 'before')
        $mismatchBefore = Get-KmcDirectoryManifest $mismatchSource
        $mismatchCopied = Copy-KmcDirectoryTreeExact -SourceRoot $mismatchSource -DestinationRoot $mismatchClone
        Assert-KmcDirectoryManifestsEqual $mismatchBefore $mismatchCopied 'synthetic exact clone'
        [IO.File]::AppendAllText((Join-Path $mismatchClone 'payload.txt'), '-clone-change')
        $cloneThrew = $false
        try { Assert-KmcDirectoryManifestsEqual $mismatchBefore (Get-KmcDirectoryManifest $mismatchClone) 'synthetic changed clone' } catch { $cloneThrew = $true }
        Assert-Test $cloneThrew 'changed clone manifest was accepted'
        [IO.File]::AppendAllText((Join-Path $mismatchSource 'payload.txt'), '-source-change')
        $sourceThrew = $false
        try { Assert-KmcDirectoryManifestsEqual $mismatchBefore (Get-KmcDirectoryManifest $mismatchSource) 'synthetic changed source' } catch { $sourceThrew = $true }
        Assert-Test $sourceThrew 'changed source manifest was accepted before activation'
    }

    Invoke-HarnessTest 'historical schema-2 Mods state remains recoverable' {
        $legacyLive = Join-Path $testRoot 'legacy-schema-game\Mods'
        New-Item -ItemType Directory -Path (Join-Path $legacyLive 'ExistingMod') -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $legacyLive 'ExistingMod\payload.txt'), 'legacy-preserve')
        $legacyOriginal = Get-KmcDirectoryManifest $legacyLive
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'legacy-schema-test'
        try {
            $legacyStatePath = Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $legacyLive -PackagePath $package -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging
            $legacyState = Read-KmcJson $legacyStatePath
            $legacyState.schemaVersion = 2
            foreach ($property in @('stagingMode','cloneBaseDigest','cloneBaseFileCount','cloneBaseDirectoryCount','cloneBaseTotalBytes')) {
                $legacyState.PSObject.Properties.Remove($property)
            }
            Write-KmcJsonAtomic $legacyStatePath $legacyState
            $restored = Restore-KmcModsTransaction -Lock $lock -StatePath $legacyStatePath -LiveModsRoot $legacyLive -BackupRoot $backup -StagingRoot $staging
            Assert-Test ($restored.digest -ceq $legacyOriginal.digest) 'historical schema-2 state did not restore the original Mods tree'
            Assert-Test ((Get-Content -Raw -LiteralPath (Join-Path $legacyLive 'ExistingMod\payload.txt')) -ceq 'legacy-preserve') 'schema-2 restoration lost the original existing mod payload'
            $restoredState = Read-KmcJson $legacyStatePath
            Assert-Test ([int]$restoredState.schemaVersion -eq 2 -and [string]$restoredState.phase -ceq 'restored') 'schema-2 restoration did not remain historical and durably restored'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'owned runtime additions are quarantined before exact restore' {
        $mutationLive = Join-Path $testRoot 'mutation-game\Mods'
        New-Item -ItemType Directory -Path (Join-Path $mutationLive 'ExistingMod') -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $mutationLive 'ExistingMod\payload.txt'), 'preserve-me')
        $mutationOriginal = Get-KmcDirectoryManifest $mutationLive
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'runtime-mutation-test'
        try {
            $statePath = Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $mutationLive -PackagePath $package -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging
            Assert-Test (Test-Path -LiteralPath (Join-Path $mutationLive 'ExistingMod\payload.txt')) 'runtime staged tree omitted the existing mod payload before mutation'
            [IO.File]::WriteAllText((Join-Path $mutationLive 'KingmakerMountedCombat\runtime-owned.cache'), 'runtime cache')
            [IO.File]::WriteAllText((Join-Path $mutationLive 'ExistingMod\runtime-owned.cache'), 'foreign mod runtime cache')
            $restored = Restore-KmcModsTransaction -Lock $lock -StatePath $statePath -LiveModsRoot $mutationLive -BackupRoot $backup -StagingRoot $staging
            Assert-Test ($restored.digest -ceq $mutationOriginal.digest) 'runtime-mutated transaction did not restore exact original'
            $state = Read-KmcJson $statePath
            Assert-Test ($state.stagedTreeChangedAtRuntime -eq $true) 'runtime mutation was not durably recorded'
            Assert-Test (Test-Path -LiteralPath (Join-Path ([string]$state.stagedAfter) 'KingmakerMountedCombat\runtime-owned.cache')) 'runtime mutation was not preserved in quarantine'
            Assert-Test (Test-Path -LiteralPath (Join-Path ([string]$state.stagedAfter) 'ExistingMod\runtime-owned.cache')) 'existing-mod runtime mutation was not preserved in quarantine'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'mismatched live ownership sentinel blocks restore' {
        $liveSentinelTest = Join-Path $testRoot 'sentinel-game\Mods'
        New-Item -ItemType Directory -Path (Join-Path $liveSentinelTest 'ExistingMod') -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $liveSentinelTest 'ExistingMod\payload.txt'), 'preserve-me')
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'sentinel-test'
        try {
            $state = Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $liveSentinelTest -PackagePath $package -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging
            $sentinelPath = Join-Path $liveSentinelTest '.kmc-runtime-sentinel.json'
            $sentinel = Read-KmcJson $sentinelPath
            $sentinel.token = ('0' * 64)
            Write-KmcJsonAtomic $sentinelPath $sentinel
            $threw = $false
            try { Restore-KmcModsTransaction -Lock $lock -StatePath $state -LiveModsRoot $liveSentinelTest -BackupRoot $backup -StagingRoot $staging | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'mismatched sentinel did not block restore'
            $sentinel.token = [string]$lock.Token
            Write-KmcJsonAtomic $sentinelPath $sentinel
            $restored = Restore-KmcModsTransaction -Lock $lock -StatePath $state -LiveModsRoot $liveSentinelTest -BackupRoot $backup -StagingRoot $staging
            Assert-Test (Test-Path -LiteralPath (Join-Path $liveSentinelTest 'ExistingMod\payload.txt')) 'owned recovery did not restore original'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'corrupted backup is rejected before live mutation' {
        $liveBackupTest = Join-Path $testRoot 'backup-game\Mods'
        New-Item -ItemType Directory -Path (Join-Path $liveBackupTest 'ExistingMod') -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $liveBackupTest 'ExistingMod\payload.txt'), 'preserve-me')
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'backup-test'
        try {
            $statePath = Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $liveBackupTest -PackagePath $package -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging
            $state = Read-KmcJson $statePath
            $backupPayload = Join-Path ([string]$state.originalBackup) 'ExistingMod\payload.txt'
            [IO.File]::AppendAllText($backupPayload, '-corrupt')
            $threw = $false
            try { Restore-KmcModsTransaction -Lock $lock -StatePath $statePath -LiveModsRoot $liveBackupTest -BackupRoot $backup -StagingRoot $staging | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'corrupted original backup was accepted'
            Assert-Test ($null -ne (Read-KmcLiveSentinel $liveBackupTest)) 'live staged tree changed before backup rejection'
            [IO.File]::WriteAllText($backupPayload, 'preserve-me')
            Restore-KmcModsTransaction -Lock $lock -StatePath $statePath -LiveModsRoot $liveBackupTest -BackupRoot $backup -StagingRoot $staging | Out-Null
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'stale lock adoption requires dead recorded owner' {
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'adoption-test'
        $path = $lock.Path
        Abandon-KmcRuntimeLock $lock
        $payload = Read-KmcJson $path
        $payload.ownerProcessId = 2147483646
        Write-KmcJsonAtomic $path $payload
        $adopted = Adopt-KmcStaleRuntimeLock $stateRoot
        try { Assert-Test ([string]$adopted.Token -ceq [string]$payload.token) 'adopted lock token changed' }
        finally { Close-KmcRuntimeLock $adopted }
    }

    $saves = Join-Path $testRoot 'saves'
    New-Item -ItemType Directory -Path $saves | Out-Null
    [IO.File]::WriteAllText((Join-Path $saves 'protected.zks'), 'not opened by inventory')
    Invoke-HarnessTest 'protected-save metadata detects mutation without hashing content' {
        $before = Get-KmcProtectedSaveMetadata $saves
        [IO.File]::AppendAllText((Join-Path $saves 'protected.zks'), 'changed')
        $after = Get-KmcProtectedSaveMetadata $saves
        Assert-Test ($before.digest -cne $after.digest) 'save metadata mutation was not detected'
    }

    $fixtureRoot = Join-Path $testRoot 'fixture-saves'
    New-Item -ItemType Directory -Path $fixtureRoot | Out-Null
    $fixtureBaseline = Join-Path $fixtureRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
    $fixtureWorking = Join-Path $fixtureRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
    $fixtureForeign = Join-Path $fixtureRoot 'Manual_3_PERSONAL.zks'
    $fixtureQualification = Join-Path $stateRoot 'fixture-test-qualification.json'
    New-TestSaveArchive -Path $fixtureBaseline -Name 'KMC_AUTOMATION_BASELINE'
    New-TestSaveArchive -Path $fixtureWorking -Name 'KMC_AUTOMATION_WORKING'
    [IO.File]::WriteAllText($fixtureForeign, 'not a zip and must never be opened')

    Invoke-HarnessTest 'fixture guard loads ZIP contracts in a cold PowerShell process' {
        $env:KMC_COLD_COMMON = Join-Path $repoRoot 'scripts\runtime\RuntimeHarness.Common.ps1'
        $env:KMC_COLD_FIXTURES = $fixtureRoot
        try {
            $childCommand = '$ErrorActionPreference=''Stop''; . $env:KMC_COLD_COMMON; $pair=Get-KmcValidatedFixturePair -SaveRoot $env:KMC_COLD_FIXTURES; if ($pair.baseline.name -cne ''KMC_AUTOMATION_BASELINE'' -or $pair.working.name -cne ''KMC_AUTOMATION_WORKING'') { throw ''cold-process fixture identity mismatch'' }'
            $childOutput = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -Command $childCommand 2>&1)
            Assert-Test ($LASTEXITCODE -eq 0) ("cold-process fixture guard failed: " + ($childOutput -join ' '))
        }
        finally {
            Remove-Item Env:KMC_COLD_COMMON -ErrorAction SilentlyContinue
            Remove-Item Env:KMC_COLD_FIXTURES -ErrorAction SilentlyContinue
        }
    }

    Invoke-HarnessTest 'standalone fixture guard rejects a concurrent game process before archive access' {
        $guardSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'scripts\runtime\Test-KmcFixtureGuard.ps1')
        $processGuard = $guardSource.IndexOf('Assert-KmcNoGameProcesses', [StringComparison]::Ordinal)
        $candidateAudit = $guardSource.IndexOf('Get-KmcFixtureCandidateAudit', [StringComparison]::Ordinal)
        Assert-Test ($processGuard -ge 0 -and $candidateAudit -gt $processGuard) 'fixture guard does not assert zero game processes before candidate/archive inspection'
    }

    Invoke-HarnessTest 'fixture guard opens only exact KMC candidates' {
        $pair = Assert-KmcFixturePair -SaveRoot $fixtureRoot -QualificationPath $fixtureQualification -InitializeQualification
        Assert-Test ([string]$pair.baseline.name -ceq 'KMC_AUTOMATION_BASELINE') 'baseline internal name differs'
        Assert-Test ([string]$pair.working.name -ceq 'KMC_AUTOMATION_WORKING') 'working internal name differs'
        Assert-Test ([string]$pair.expectedGameId -ceq '11111111-2222-3333-4444-555555555555') 'pair GameId differs'
        Assert-Test (Test-Path -LiteralPath $fixtureQualification -PathType Leaf) 'durable qualification was not written'
    }

    Invoke-HarnessTest 'fixture guard rejects near-match before archive open' {
        $nearRoot = Join-Path $testRoot 'near-match-saves'
        New-Item -ItemType Directory -Path $nearRoot | Out-Null
        New-TestSaveArchive -Path (Join-Path $nearRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks') -Name 'KMC_AUTOMATION_BASELINE'
        [IO.File]::WriteAllText((Join-Path $nearRoot 'Manual_2_KMC_AUTOMATION_WORKING_.zks'), 'intentionally unreadable near-match')
        $message = $null
        try { Assert-KmcFixturePair -SaveRoot $nearRoot -QualificationPath (Join-Path $stateRoot 'near.json') -InitializeQualification | Out-Null }
        catch { $message = $_.Exception.Message }
        Assert-Test ($message -like 'Exact KMC filename audit failed:*working=0*') 'near-match was not rejected by filename audit'
        Assert-Test ($message -like '*Manual_2_KMC_AUTOMATION_WORKING_.zks*') 'near-match rejection did not identify the exact filename'
    }

    Invoke-HarnessTest 'fixture guard rejects extra KMC-looking name before archive open' {
        $extraKmcRoot = Join-Path $testRoot 'extra-kmc-saves'
        New-Item -ItemType Directory -Path $extraKmcRoot | Out-Null
        New-TestSaveArchive -Path (Join-Path $extraKmcRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks') -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path (Join-Path $extraKmcRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks') -Name 'KMC_AUTOMATION_WORKING'
        [IO.File]::WriteAllText((Join-Path $extraKmcRoot 'Manual_3_KMC_AUTOMATION_WORKING_.zks'), 'intentionally unreadable extra near-match')
        $message = $null
        try { Assert-KmcFixturePair -SaveRoot $extraKmcRoot -QualificationPath (Join-Path $stateRoot 'extra-kmc.json') -InitializeQualification | Out-Null }
        catch { $message = $_.Exception.Message }
        Assert-Test ($message -like 'Exact KMC filename audit failed:*rejected KMC-looking names=Manual_3_KMC_AUTOMATION_WORKING_.zks*') 'extra KMC-looking archive was ignored'
    }

    Invoke-HarnessTest 'fixture guard rejects campaign identity mismatch' {
        $mismatchRoot = Join-Path $testRoot 'mismatch-saves'
        New-Item -ItemType Directory -Path $mismatchRoot | Out-Null
        New-TestSaveArchive -Path (Join-Path $mismatchRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks') -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path (Join-Path $mismatchRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks') -Name 'KMC_AUTOMATION_WORKING' -Area 'fedcba9876543210fedcba9876543210'
        $threw = $false
        try { Assert-KmcFixturePair -SaveRoot $mismatchRoot -QualificationPath (Join-Path $stateRoot 'mismatch.json') -InitializeQualification | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'mismatched fixture identity was accepted'
    }

    Invoke-HarnessTest 'fixture guard rejects duplicate or case-ambiguous header members' {
        $duplicateRoot = Join-Path $testRoot 'duplicate-header-saves'
        New-Item -ItemType Directory -Path $duplicateRoot | Out-Null
        $duplicateBaseline = Join-Path $duplicateRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $duplicateWorking = Join-Path $duplicateRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestRawSaveArchive -Path $duplicateBaseline -HeaderJson '{"Name":"WRONG","Name":"KMC_AUTOMATION_BASELINE","GameName":"KMC Test Campaign","GameId":"11111111-2222-3333-4444-555555555555","Area":"0123456789abcdef0123456789abcdef","Type":"Manual","CompatibilityVersion":1}'
        New-TestSaveArchive -Path $duplicateWorking -Name 'KMC_AUTOMATION_WORKING'
        $message = $null
        try { Get-KmcValidatedFixturePair $duplicateRoot | Out-Null } catch { $message = $_.Exception.Message }
        Assert-Test ($message -like '*duplicate or case-ambiguous JSON object member: Name*') 'duplicate exact-case header member was accepted'

        New-TestRawSaveArchive -Path $duplicateBaseline -HeaderJson '{"name":"WRONG","Name":"KMC_AUTOMATION_BASELINE","GameName":"KMC Test Campaign","GameId":"11111111-2222-3333-4444-555555555555","Area":"0123456789abcdef0123456789abcdef","Type":"Manual","CompatibilityVersion":1}'
        $message = $null
        try { Get-KmcValidatedFixturePair $duplicateRoot | Out-Null } catch { $message = $_.Exception.Message }
        Assert-Test ($message -like '*duplicate or case-ambiguous JSON object member: Name*') 'case-ambiguous header member was accepted'
    }

    Invoke-HarnessTest 'fixture guard rejects coercible non-primitive identity fields' {
        $wrongTypeRoot = Join-Path $testRoot 'wrong-type-header-saves'
        New-Item -ItemType Directory -Path $wrongTypeRoot | Out-Null
        $wrongTypeBaseline = Join-Path $wrongTypeRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $wrongTypeWorking = Join-Path $wrongTypeRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $wrongTypeWorking -Name 'KMC_AUTOMATION_WORKING'
        $invalidHeaders = @(
            '{"Name":["KMC_AUTOMATION_BASELINE"],"GameName":"KMC Test Campaign","GameId":"11111111-2222-3333-4444-555555555555","Area":"0123456789abcdef0123456789abcdef","Type":"Manual","CompatibilityVersion":1}',
            '{"Name":"KMC_AUTOMATION_BASELINE","GameName":["KMC Test Campaign"],"GameId":"11111111-2222-3333-4444-555555555555","Area":"0123456789abcdef0123456789abcdef","Type":"Manual","CompatibilityVersion":1}',
            '{"Name":"KMC_AUTOMATION_BASELINE","GameName":"KMC Test Campaign","GameId":["11111111-2222-3333-4444-555555555555"],"Area":"0123456789abcdef0123456789abcdef","Type":"Manual","CompatibilityVersion":1}',
            '{"Name":"KMC_AUTOMATION_BASELINE","GameName":"KMC Test Campaign","GameId":"11111111-2222-3333-4444-555555555555","Area":["0123456789abcdef0123456789abcdef"],"Type":"Manual","CompatibilityVersion":1}',
            '{"Name":"KMC_AUTOMATION_BASELINE","GameName":"KMC Test Campaign","GameId":"11111111-2222-3333-4444-555555555555","Area":"0123456789abcdef0123456789abcdef","Type":["Manual"],"CompatibilityVersion":1}',
            '{"Name":"KMC_AUTOMATION_BASELINE","GameName":"KMC Test Campaign","GameId":"11111111-2222-3333-4444-555555555555","Area":"0123456789abcdef0123456789abcdef","Type":"Manual","CompatibilityVersion":true}',
            '{"Name":"KMC_AUTOMATION_BASELINE","GameName":"KMC Test Campaign","GameId":"11111111-2222-3333-4444-555555555555","Area":"0123456789abcdef0123456789abcdef","Type":"Manual","CompatibilityVersion":"1"}'
        )
        foreach ($invalidHeader in $invalidHeaders) {
            New-TestRawSaveArchive -Path $wrongTypeBaseline -HeaderJson $invalidHeader
            $threw = $false
            try { Get-KmcValidatedFixturePair $wrongTypeRoot | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'a coercible non-primitive identity field was accepted'
        }
    }

    Invoke-HarnessTest 'fixture guard rejects hard-linked canonical candidates before archive access' {
        $hardLinkRoot = Join-Path $testRoot 'hard-link-saves'
        New-Item -ItemType Directory -Path $hardLinkRoot | Out-Null
        $hardLinkBaseline = Join-Path $hardLinkRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $hardLinkWorking = Join-Path $hardLinkRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $hardLinkBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-Item -ItemType HardLink -Path $hardLinkWorking -Target $hardLinkBaseline | Out-Null
        $message = $null
        try { Get-KmcValidatedFixturePair $hardLinkRoot | Out-Null } catch { $message = $_.Exception.Message }
        Assert-Test ($message -like '*is a hard link and cannot establish independent fixture identity*') 'hard-linked fixture candidates were accepted or opened'
    }

    Invoke-HarnessTest 'fixture qualification detects untransactional working drift' {
        New-TestSaveArchive -Path $fixtureWorking -Name 'KMC_AUTOMATION_WORKING' -ExtraEntry
        $threw = $false
        try { Assert-KmcFixturePair -SaveRoot $fixtureRoot -QualificationPath $fixtureQualification | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'untransactional working drift passed durable qualification'
        New-TestSaveArchive -Path $fixtureWorking -Name 'KMC_AUTOMATION_WORKING'
    }

    Invoke-HarnessTest 'fixture qualification detects baseline mutation' {
        $immutableRoot = Join-Path $testRoot 'immutable-saves'
        New-Item -ItemType Directory -Path $immutableRoot | Out-Null
        $immutableBaseline = Join-Path $immutableRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $immutableWorking = Join-Path $immutableRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        $immutableState = Join-Path $stateRoot 'immutable.json'
        New-TestSaveArchive -Path $immutableBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $immutableWorking -Name 'KMC_AUTOMATION_WORKING'
        Assert-KmcFixturePair -SaveRoot $immutableRoot -QualificationPath $immutableState -InitializeQualification | Out-Null
        New-TestSaveArchive -Path $immutableBaseline -Name 'KMC_AUTOMATION_BASELINE' -ExtraEntry
        $threw = $false
        try { Assert-KmcFixturePair -SaveRoot $immutableRoot -QualificationPath $immutableState | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'mutated baseline passed immutable qualification'
    }

    Invoke-HarnessTest 'save write allowlist permits only exact working path' {
        $allowRoot = Join-Path $testRoot 'allowlist-saves'
        New-Item -ItemType Directory -Path $allowRoot | Out-Null
        $allowWorking = Join-Path $allowRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        $allowForeign = Join-Path $allowRoot 'Manual_3_PERSONAL.zks'
        [IO.File]::WriteAllText($allowWorking, 'working')
        [IO.File]::WriteAllText($allowForeign, 'protected')
        $before = Get-KmcSaveMetadataInventory $allowRoot
        [IO.File]::AppendAllText($allowWorking, '-changed')
        $workingOnly = Assert-KmcSaveWriteAllowlist -Before $before -After (Get-KmcSaveMetadataInventory $allowRoot) -WorkingPath $allowWorking
        Assert-Test $workingOnly.workingChanged 'working mutation was not reported'
        [IO.File]::AppendAllText($allowForeign, '-changed')
        $threw = $false
        try { Assert-KmcSaveWriteAllowlist -Before $before -After (Get-KmcSaveMetadataInventory $allowRoot) -WorkingPath $allowWorking | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'protected save mutation passed the write allowlist'
    }

    Invoke-HarnessTest 'working-save transaction restores exact mutable fixture' {
        $transactionSaveRoot = Join-Path $testRoot 'transaction-saves'
        New-Item -ItemType Directory -Path $transactionSaveRoot | Out-Null
        $transactionBaseline = Join-Path $transactionSaveRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $transactionWorking = Join-Path $transactionSaveRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $transactionBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $transactionWorking -Name 'KMC_AUTOMATION_WORKING'
        $pair = Get-KmcValidatedFixturePair $transactionSaveRoot
        $originalHash = $pair.working.sha256
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'save-transaction-test'
        try {
            $saveState = Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $transactionSaveRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake
            New-TestSaveArchive -Path $transactionWorking -Name 'KMC_AUTOMATION_WORKING' -ExtraEntry
            $restored = Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveState -SaveRoot $transactionSaveRoot -BackupRoot $backup -StagingRoot $staging
            Assert-Test ($restored.baselineImmutable -and $restored.workingRestored -and $restored.saveWriteAllowlistPassed) 'save transaction result flags differ'
            Assert-Test ((Get-KmcSha256 $transactionWorking) -ceq $originalHash) 'Working fixture hash was not exactly restored'
            $again = Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveState -SaveRoot $transactionSaveRoot -BackupRoot $backup -StagingRoot $staging
            Assert-Test $again.workingRestored 'idempotent save restore failed'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'working-save transaction rejects drift after qualification before creating state' {
        $entryDriftRoot = Join-Path $testRoot 'entry-drift-saves'
        New-Item -ItemType Directory -Path $entryDriftRoot | Out-Null
        $entryDriftBaseline = Join-Path $entryDriftRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $entryDriftWorking = Join-Path $entryDriftRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $entryDriftBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $entryDriftWorking -Name 'KMC_AUTOMATION_WORKING'
        $pair = Get-KmcValidatedFixturePair $entryDriftRoot
        New-TestSaveArchive -Path $entryDriftWorking -Name 'KMC_AUTOMATION_WORKING' -ExtraEntry
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'save-entry-drift-test'
        try {
            $statePath = Get-KmcSaveTransactionStatePath $stateRoot $lock.RunId
            $threw = $false
            try { Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $entryDriftRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'Working drift between qualification and transaction entry was accepted'
            Assert-Test (-not (Test-Path -LiteralPath $statePath)) 'a transaction state was created for stale fixture identity'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'working-save transaction rejects corrupt backup before mutation' {
        $corruptSaveRoot = Join-Path $testRoot 'corrupt-save-backup'
        New-Item -ItemType Directory -Path $corruptSaveRoot | Out-Null
        $corruptBaseline = Join-Path $corruptSaveRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $corruptWorking = Join-Path $corruptSaveRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $corruptBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $corruptWorking -Name 'KMC_AUTOMATION_WORKING'
        $pair = Get-KmcValidatedFixturePair $corruptSaveRoot
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'corrupt-save-backup-test'
        try {
            $saveState = Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $corruptSaveRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake
            $state = Read-KmcJson $saveState
            [IO.File]::AppendAllText([string]$state.backupPath, 'corrupt')
            $liveBefore = Get-KmcSha256 $corruptWorking
            $threw = $false
            try { Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveState -SaveRoot $corruptSaveRoot -BackupRoot $backup -StagingRoot $staging | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'corrupt Working backup was accepted'
            Assert-Test ((Get-KmcSha256 $corruptWorking) -ceq $liveBefore) 'live Working changed before corrupt-backup rejection'
            [IO.File]::WriteAllBytes([string]$state.backupPath, [IO.File]::ReadAllBytes($corruptWorking))
            Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveState -SaveRoot $corruptSaveRoot -BackupRoot $backup -StagingRoot $staging | Out-Null
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'working-save transaction never repairs baseline drift' {
        $driftSaveRoot = Join-Path $testRoot 'baseline-drift-saves'
        New-Item -ItemType Directory -Path $driftSaveRoot | Out-Null
        $driftBaseline = Join-Path $driftSaveRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $driftWorking = Join-Path $driftSaveRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $driftBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $driftWorking -Name 'KMC_AUTOMATION_WORKING'
        $baselineOriginalBytes = [IO.File]::ReadAllBytes($driftBaseline)
        $baselineOriginalTime = (Get-Item -LiteralPath $driftBaseline).LastWriteTimeUtc
        $pair = Get-KmcValidatedFixturePair $driftSaveRoot
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'baseline-drift-test'
        try {
            $saveState = Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $driftSaveRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake
            New-TestSaveArchive -Path $driftBaseline -Name 'KMC_AUTOMATION_BASELINE' -ExtraEntry
            $driftHash = Get-KmcSha256 $driftBaseline
            $threw = $false
            try { Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveState -SaveRoot $driftSaveRoot -BackupRoot $backup -StagingRoot $staging | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'baseline drift did not fail the transaction'
            Assert-Test ((Get-KmcSha256 $driftBaseline) -ceq $driftHash) 'guard attempted to repair or overwrite baseline drift'
            [IO.File]::WriteAllBytes($driftBaseline, $baselineOriginalBytes)
            (Get-Item -LiteralPath $driftBaseline).LastWriteTimeUtc = $baselineOriginalTime
            Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveState -SaveRoot $driftSaveRoot -BackupRoot $backup -StagingRoot $staging | Out-Null
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'Working slot-family recovery plans and preserves every bounded artifact before exact restore' {
        $artifactSaveRoot = Join-Path $testRoot 'slot-family-artifacts'
        New-Item -ItemType Directory -Path $artifactSaveRoot | Out-Null
        $artifactBaseline = Join-Path $artifactSaveRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $artifactWorking = Join-Path $artifactSaveRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $artifactBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $artifactWorking -Name 'KMC_AUTOMATION_WORKING'
        $pair = Get-KmcValidatedFixturePair $artifactSaveRoot
        $before = Get-KmcSaveMetadataInventory $artifactSaveRoot
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'slot-family-artifact-test'
        try {
            $saveStatePath = Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $artifactSaveRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario boundary-suite
            New-TestSaveArchive -Path $artifactWorking -Name 'KMC_AUTOMATION_WORKING' -ExtraEntry
            foreach ($sidecar in @(($artifactWorking + '.abcdefgh.xyz'), ($artifactWorking + '.bcdefghi.uvw'))) {
                New-TestSaveArchive -Path $sidecar -Name 'KMC_AUTOMATION_WORKING'
            }
            [IO.File]::WriteAllBytes((Join-Path $artifactSaveRoot 'DotNetZip-abcdefgh.tmp'), [Text.Encoding]::UTF8.GetBytes('partial-one'))
            [IO.File]::WriteAllBytes((Join-Path $artifactSaveRoot 'DotNetZip-bcdefghi.tmp'), [Text.Encoding]::UTF8.GetBytes('partial-two'))

            $planned = Initialize-KmcWorkingSaveRecoveryPlan -Lock $lock -StatePath $saveStatePath -SaveRoot $artifactSaveRoot -StagingRoot $staging
            Assert-Test ([string]$planned.phase -ceq 'recovery-planned' -and @($planned.artifactPlan).Count -eq 5) 'complete bounded artifact delta was not durably planned'
            Assert-Test (-not (Test-Path -LiteralPath ([string]$planned.artifactQuarantineRoot))) 'artifact moves began before the recovery plan was durably persisted'
            $plannedHashes = @{}; foreach ($entry in @($planned.artifactPlan)) { $plannedHashes[[string]$entry.quarantinePath] = [string]$entry.sha256 }

            $restored = Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveStatePath -SaveRoot $artifactSaveRoot -BackupRoot $backup -StagingRoot $staging
            Assert-Test ($restored.baselineImmutable -and $restored.workingRestored -and $restored.saveWriteAllowlistPassed) 'slot-family restoration flags differ'
            Assert-Test ([string]$restored.restoredInventoryDigest -ceq [string]$before.digest) 'slot-family restoration did not reproduce the exact metadata digest'
            Assert-Test (@(Get-ChildItem -LiteralPath ([string]$planned.artifactQuarantineRoot) -File -Force).Count -eq 5) 'one or more owned artifacts were deleted instead of quarantined'
            foreach ($quarantine in $plannedHashes.Keys) {
                Assert-Test ((Get-KmcSha256 $quarantine) -ceq [string]$plannedHashes[$quarantine]) 'a quarantined artifact did not retain its planned bytes'
            }
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'Working slot-family recovery resumes after an unrecorded artifact move and is idempotent' {
        $resumeRoot = Join-Path $testRoot 'slot-family-interruption'
        New-Item -ItemType Directory -Path $resumeRoot | Out-Null
        $resumeBaseline = Join-Path $resumeRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $resumeWorking = Join-Path $resumeRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $resumeBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $resumeWorking -Name 'KMC_AUTOMATION_WORKING'
        $pair = Get-KmcValidatedFixturePair $resumeRoot
        $before = Get-KmcSaveMetadataInventory $resumeRoot
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'slot-family-interruption-test'
        try {
            $saveStatePath = Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $resumeRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake
            New-TestSaveArchive -Path $resumeWorking -Name 'KMC_AUTOMATION_WORKING' -ExtraEntry
            [IO.File]::WriteAllText((Join-Path $resumeRoot 'DotNetZip-abcdefgh.tmp'), 'interrupted-temp')
            $planned = Initialize-KmcWorkingSaveRecoveryPlan -Lock $lock -StatePath $saveStatePath -SaveRoot $resumeRoot -StagingRoot $staging
            New-Item -ItemType Directory -Path ([string]$planned.artifactQuarantineRoot) | Out-Null
            $firstArtifact = @($planned.artifactPlan)[0]
            Move-Item -LiteralPath ([string]$firstArtifact.sourcePath) -Destination ([string]$firstArtifact.quarantinePath)

            $restored = Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveStatePath -SaveRoot $resumeRoot -BackupRoot $backup -StagingRoot $staging
            Assert-Test ([string]$restored.restoredInventoryDigest -ceq [string]$before.digest) 'interrupted recovery did not restore the exact inventory'
            $again = Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveStatePath -SaveRoot $resumeRoot -BackupRoot $backup -StagingRoot $staging
            Assert-Test ($again.workingRestored -and [string]$again.restoredInventoryDigest -ceq [string]$before.digest) 'replayed completed recovery was not idempotent'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'Working slot-family recovery rejects unknown and foreign drift before any move' {
        $unknownRoot = Join-Path $testRoot 'slot-family-unknown'
        New-Item -ItemType Directory -Path $unknownRoot | Out-Null
        $unknownBaseline = Join-Path $unknownRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $unknownWorking = Join-Path $unknownRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        $foreign = Join-Path $unknownRoot 'Manual_9_PERSONAL.zks'
        New-TestSaveArchive -Path $unknownBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $unknownWorking -Name 'KMC_AUTOMATION_WORKING'
        [IO.File]::WriteAllText($foreign, 'protected-before')
        $pair = Get-KmcValidatedFixturePair $unknownRoot
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'slot-family-unknown-test'
        try {
            $saveStatePath = Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $unknownRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake
            New-TestSaveArchive -Path $unknownWorking -Name 'KMC_AUTOMATION_WORKING' -ExtraEntry
            $workingAfterHash = Get-KmcSha256 $unknownWorking
            [IO.File]::AppendAllText($foreign, '-drift')
            [IO.File]::WriteAllText((Join-Path $unknownRoot 'Quicksave_1_FOREIGN.zks'), 'unknown')
            $threw = $false
            try { Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveStatePath -SaveRoot $unknownRoot -BackupRoot $backup -StagingRoot $staging | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'unknown and foreign save drift was accepted'
            Assert-Test ((Get-KmcSha256 $unknownWorking) -ceq $workingAfterHash) 'Working was moved before unknown/foreign drift rejection'
            $state = Read-KmcJson $saveStatePath
            Assert-Test ($null -eq $state.PSObject.Properties['artifactPlan'] -and -not (Test-Path -LiteralPath ([string]$state.artifactQuarantineRoot))) 'a recovery plan or artifact move survived rejected preclassification'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'Working slot-family recovery rejects foreign-identity sidecars and excess artifacts before any move' {
        $boundedRoot = Join-Path $testRoot 'slot-family-bounds'
        New-Item -ItemType Directory -Path $boundedRoot | Out-Null
        $boundedBaseline = Join-Path $boundedRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $boundedWorking = Join-Path $boundedRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $boundedBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $boundedWorking -Name 'KMC_AUTOMATION_WORKING'
        $pair = Get-KmcValidatedFixturePair $boundedRoot
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'slot-family-bounds-test'
        try {
            $saveStatePath = Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $boundedRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake
            $foreignSidecar = $boundedWorking + '.abcdefgh.xyz'
            New-TestSaveArchive -Path $foreignSidecar -Name 'KMC_AUTOMATION_WORKING' -GameId 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'
            $threw = $false
            try { Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveStatePath -SaveRoot $boundedRoot -BackupRoot $backup -StagingRoot $staging | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'a Working sidecar with foreign campaign identity was accepted'
            Assert-Test (Test-Path -LiteralPath $foreignSidecar -PathType Leaf) 'foreign-identity sidecar was moved before rejection'
        }
        finally { Close-KmcRuntimeLock $lock }

        $excessRoot = Join-Path $testRoot 'slot-family-excess'
        New-Item -ItemType Directory -Path $excessRoot | Out-Null
        $excessBaseline = Join-Path $excessRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $excessWorking = Join-Path $excessRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $excessBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $excessWorking -Name 'KMC_AUTOMATION_WORKING'
        $pair = Get-KmcValidatedFixturePair $excessRoot
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'slot-family-excess-test'
        try {
            $saveStatePath = Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $excessRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario boundary-suite
            foreach ($leaf in @('DotNetZip-abcdefgh.tmp','DotNetZip-bcdefghi.tmp','DotNetZip-cdefghij.tmp')) { [IO.File]::WriteAllText((Join-Path $excessRoot $leaf), $leaf) }
            $threw = $false
            try { Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveStatePath -SaveRoot $excessRoot -BackupRoot $backup -StagingRoot $staging | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'artifact count beyond the two-load boundary-suite bound was accepted'
            Assert-Test (@(Get-ChildItem -LiteralPath $excessRoot -Filter 'DotNetZip-*.tmp' -File).Count -eq 3) 'excess artifacts were moved before bound rejection'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'Working slot-family recovery rejects an extra canonical slot before any move' {
        $duplicateRoot = Join-Path $testRoot 'slot-family-duplicate-canonical'
        New-Item -ItemType Directory -Path $duplicateRoot | Out-Null
        $duplicateBaseline = Join-Path $duplicateRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $duplicateWorking = Join-Path $duplicateRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $duplicateBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $duplicateWorking -Name 'KMC_AUTOMATION_WORKING'
        $pair = Get-KmcValidatedFixturePair $duplicateRoot
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'slot-family-duplicate-canonical-test'
        try {
            $saveStatePath = Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $duplicateRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake
            New-TestSaveArchive -Path $duplicateWorking -Name 'KMC_AUTOMATION_WORKING' -ExtraEntry
            $workingAfterHash = Get-KmcSha256 $duplicateWorking
            $extraCanonical = Join-Path $duplicateRoot 'Manual_3_KMC_AUTOMATION_WORKING.zks'
            New-TestSaveArchive -Path $extraCanonical -Name 'KMC_AUTOMATION_WORKING'
            $threw = $false
            try { Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveStatePath -SaveRoot $duplicateRoot -BackupRoot $backup -StagingRoot $staging | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'second canonical Working slot was inferred to be transaction-owned'
            Assert-Test ((Get-KmcSha256 $duplicateWorking) -ceq $workingAfterHash -and (Test-Path -LiteralPath $extraCanonical -PathType Leaf)) 'an artifact moved before duplicate-canonical rejection'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'Working recovery detects same-length same-time content drift' {
        $contentRoot = Join-Path $testRoot 'working-content-drift'
        New-Item -ItemType Directory -Path $contentRoot | Out-Null
        $contentBaseline = Join-Path $contentRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $contentWorking = Join-Path $contentRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $contentBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $contentWorking -Name 'KMC_AUTOMATION_WORKING'
        $pair = Get-KmcValidatedFixturePair $contentRoot
        $originalTime = (Get-Item -LiteralPath $contentWorking).LastWriteTimeUtc
        $originalBytes = [IO.File]::ReadAllBytes($contentWorking)
        $mutatedBytes = New-Object byte[] $originalBytes.Length
        [Array]::Copy($originalBytes, $mutatedBytes, $originalBytes.Length)
        $mutatedBytes[$mutatedBytes.Length - 1] = $mutatedBytes[$mutatedBytes.Length - 1] -bxor 1
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'working-content-drift-test'
        try {
            $saveStatePath = Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $contentRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake
            [IO.File]::WriteAllBytes($contentWorking, $mutatedBytes)
            (Get-Item -LiteralPath $contentWorking).LastWriteTimeUtc = $originalTime
            $planned = Initialize-KmcWorkingSaveRecoveryPlan -Lock $lock -StatePath $saveStatePath -SaveRoot $contentRoot -StagingRoot $staging
            Assert-Test (@($planned.artifactPlan | Where-Object kind -eq 'working-current').Count -eq 1) 'same-metadata Working substitution was omitted from the recovery plan'
            $restored = Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveStatePath -SaveRoot $contentRoot -BackupRoot $backup -StagingRoot $staging
            Assert-Test ((Get-KmcSha256 $contentWorking) -ceq [string]$pair.working.sha256) 'same-metadata Working substitution was not exactly restored'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'DotNetZip temp and sidecar suffix classification is case-insensitive' {
        $caseRoot = Join-Path $testRoot 'slot-family-case'
        New-Item -ItemType Directory -Path $caseRoot | Out-Null
        $caseBaseline = Join-Path $caseRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $caseWorking = Join-Path $caseRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $caseBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $caseWorking -Name 'KMC_AUTOMATION_WORKING'
        $pair = Get-KmcValidatedFixturePair $caseRoot
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'slot-family-case-test'
        try {
            $saveStatePath = Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $caseRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake
            New-TestSaveArchive -Path ($caseWorking + '.AbCdEfGh.XyZ') -Name 'KMC_AUTOMATION_WORKING'
            [IO.File]::WriteAllText((Join-Path $caseRoot 'DotNetZip-AbCdEfGh.TmP'), 'case-temp')
            $restored = Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveStatePath -SaveRoot $caseRoot -BackupRoot $backup -StagingRoot $staging
            Assert-Test $restored.workingRestored 'mixed-case DotNetZip artifacts were not recovered'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'Working slot-family recovery rejects hard-link and reparse artifacts before any move' {
        $linkRoot = Join-Path $testRoot 'slot-family-links'
        New-Item -ItemType Directory -Path $linkRoot | Out-Null
        $linkBaseline = Join-Path $linkRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $linkWorking = Join-Path $linkRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $linkBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $linkWorking -Name 'KMC_AUTOMATION_WORKING'
        $pair = Get-KmcValidatedFixturePair $linkRoot
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'slot-family-hardlink-test'
        try {
            $saveStatePath = Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $linkRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake
            $hardLink = Join-Path $linkRoot 'Manual_3_KMC_AUTOMATION_WORKING.zks'
            New-Item -ItemType HardLink -Path $hardLink -Target $linkWorking | Out-Null
            $threw = $false
            try { Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveStatePath -SaveRoot $linkRoot -BackupRoot $backup -StagingRoot $staging | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'hard-linked canonical Working artifact was accepted'
            Assert-Test (Test-Path -LiteralPath $hardLink -PathType Leaf) 'hard-linked artifact was moved before rejection'
        }
        finally { Close-KmcRuntimeLock $lock }

        $reparseRoot = Join-Path $testRoot 'slot-family-reparse'
        $reparseTarget = Join-Path $testRoot 'slot-family-reparse-target'
        New-Item -ItemType Directory -Path $reparseRoot,$reparseTarget | Out-Null
        $reparseBaseline = Join-Path $reparseRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $reparseWorking = Join-Path $reparseRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $reparseBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $reparseWorking -Name 'KMC_AUTOMATION_WORKING'
        $pair = Get-KmcValidatedFixturePair $reparseRoot
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'slot-family-reparse-test'
        try {
            $saveStatePath = Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $reparseRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake
            $junction = Join-Path $reparseRoot 'DotNetZip-abcdefgh.tmp'
            New-Item -ItemType Junction -Path $junction -Target $reparseTarget | Out-Null
            $threw = $false
            try { Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveStatePath -SaveRoot $reparseRoot -BackupRoot $backup -StagingRoot $staging | Out-Null } catch { $threw = $true }
            Assert-Test $threw 'reparse-point save artifact was accepted'
            Assert-Test ((Get-Item -LiteralPath $junction -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) 'reparse artifact was moved before rejection'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'aggregate runtime suite is excluded until its archive-write count is proven' {
        Assert-Test (@(Get-KmcSaveBackedRuntimeScenarios | Where-Object { $_ -ceq 'phase-1-runtime-suite' }).Count -eq 0) 'unsupported aggregate suite remains in the save-backed allowlist'
        $threw = $false
        try { Get-KmcRuntimeArchiveWriteLimit 'phase-1-runtime-suite' | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'unsupported aggregate suite received an inferred archive-write bound'
    }

    Invoke-HarnessTest 'combined runtime transaction restores Working and Mods under one lock' {
        $combinedLive = Join-Path $testRoot 'combined-game\Mods'
        New-Item -ItemType Directory -Path (Join-Path $combinedLive 'ExistingMod') -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $combinedLive 'ExistingMod\payload.txt'), 'preserve-combined')
        $combinedSaveRoot = Join-Path $testRoot 'combined-saves'
        New-Item -ItemType Directory -Path $combinedSaveRoot | Out-Null
        $combinedBaseline = Join-Path $combinedSaveRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $combinedWorking = Join-Path $combinedSaveRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $combinedBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $combinedWorking -Name 'KMC_AUTOMATION_WORKING'
        $combinedPair = Get-KmcValidatedFixturePair $combinedSaveRoot
        $combinedModsBefore = Get-KmcDirectoryManifest $combinedLive
        $combinedSavesBefore = Get-KmcSaveMetadataInventory $combinedSaveRoot
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'combined-transaction-test'
        try {
            $combinedState = New-KmcRunTransactionState -Lock $lock -Mode save-backed-v2 -LiveModsRoot $combinedLive -SaveRoot $combinedSaveRoot -StateRoot $stateRoot -ModsBefore $combinedModsBefore -SavesBefore $combinedSavesBefore
            Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $combinedPair -SaveRoot $combinedSaveRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake | Out-Null
            Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $combinedLive -PackagePath $package -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging | Out-Null
            New-TestSaveArchive -Path $combinedWorking -Name 'KMC_AUTOMATION_WORKING' -ExtraEntry
            $restoration = Restore-KmcRuntimeTransactions -Lock $lock -CombinedStatePath $combinedState -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging
            Assert-Test ($restoration.modsRestored -and $restoration.saveProtectionPassed -and $restoration.baselineImmutable -and $restoration.workingRestored -and $restoration.saveWriteAllowlistPassed) 'combined restoration did not prove every external-state invariant'
            Assert-Test ([string]$restoration.restoredModsDigest -ceq [string]$combinedModsBefore.digest) 'combined Mods digest differs after restoration'
            Assert-Test ([string]$restoration.restoredSaveInventoryDigest -ceq [string]$combinedSavesBefore.digest) 'combined save digest differs after restoration'
            $durable = Read-KmcRunTransactionState -StatePath $combinedState -Lock $lock
            Assert-Test ([string]$durable.phase -ceq 'restored') 'combined transaction was not durably marked restored'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'combined runtime transaction restores Mods independently when save recovery fails' {
        $independentLive = Join-Path $testRoot 'independent-game\Mods'
        New-Item -ItemType Directory -Path (Join-Path $independentLive 'ExistingMod') -Force | Out-Null
        [IO.File]::WriteAllText((Join-Path $independentLive 'ExistingMod\payload.txt'), 'preserve-independent')
        $independentSaveRoot = Join-Path $testRoot 'independent-saves'
        New-Item -ItemType Directory -Path $independentSaveRoot | Out-Null
        $independentBaseline = Join-Path $independentSaveRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
        $independentWorking = Join-Path $independentSaveRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $independentBaseline -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path $independentWorking -Name 'KMC_AUTOMATION_WORKING'
        $independentPair = Get-KmcValidatedFixturePair $independentSaveRoot
        $originalWorkingBytes = [IO.File]::ReadAllBytes($independentWorking)
        $independentModsBefore = Get-KmcDirectoryManifest $independentLive
        $independentSavesBefore = Get-KmcSaveMetadataInventory $independentSaveRoot
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'independent-restoration-test'
        try {
            $combinedState = New-KmcRunTransactionState -Lock $lock -Mode save-backed-v2 -LiveModsRoot $independentLive -SaveRoot $independentSaveRoot -StateRoot $stateRoot -ModsBefore $independentModsBefore -SavesBefore $independentSavesBefore
            $saveStatePath = Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $independentPair -SaveRoot $independentSaveRoot -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake
            Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $independentLive -PackagePath $package -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging | Out-Null
            $saveState = Read-KmcJson $saveStatePath
            [IO.File]::AppendAllText([string]$saveState.backupPath, 'corrupt')
            New-TestSaveArchive -Path $independentWorking -Name 'KMC_AUTOMATION_WORKING' -ExtraEntry
            $first = Restore-KmcRuntimeTransactions -Lock $lock -CombinedStatePath $combinedState -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging
            Assert-Test ($first.modsRestored -and -not $first.saveProtectionPassed -and @($first.errors).Count -eq 1) 'Mods were not independently restored after save-backup failure'
            Assert-Test ((Get-KmcDirectoryManifest $independentLive).digest -ceq $independentModsBefore.digest) 'live Mods remained staged after independent save recovery failure'
            [IO.File]::WriteAllBytes([string]$saveState.backupPath, $originalWorkingBytes)
            $second = Restore-KmcRuntimeTransactions -Lock $lock -CombinedStatePath $combinedState -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging
            Assert-Test ($second.modsRestored -and $second.saveProtectionPassed -and @($second.errors).Count -eq 0) 'repaired save backup did not permit idempotent combined recovery'
        }
        finally { Close-KmcRuntimeLock $lock }
    }

    Invoke-HarnessTest 'schema-v2 fixture payload exposes no save path and Working-only authorization' {
        $payloadRoot = Join-Path $testRoot 'payload-saves'
        New-Item -ItemType Directory -Path $payloadRoot | Out-Null
        New-TestSaveArchive -Path (Join-Path $payloadRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks') -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path (Join-Path $payloadRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks') -Name 'KMC_AUTOMATION_WORKING'
        $payload = New-KmcRuntimeFixturePayload (Get-KmcValidatedFixturePair $payloadRoot)
        Assert-Test (@($payload.Keys).Count -eq 3) 'fixture payload property count differs'
        Assert-Test (@($payload.baseline.Keys | Where-Object { $_ -in @('path','kind','schemaVersion') }).Count -eq 0) 'fixture payload disclosed a host save path or guard-only field'
        Assert-Test ([string]$payload.writeAuthorization.mode -ceq 'working-only' -and [string]$payload.writeAuthorization.allowedInternalName -ceq 'KMC_AUTOMATION_WORKING' -and $payload.writeAuthorization.baselineImmutable) 'fixture payload write authorization differs'
    }

    Invoke-HarnessTest 'runtime request bytes are bound to the launched process' {
        $launcherSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\Invoke-KingmakerRuntimeScenario.ps1')
        $hostSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeAutomationHost.cs')
        Assert-Test ($launcherSource.Contains("'-kmcRuntimeRequestSha256',`$requestHash")) 'launcher does not pass the exact request-file SHA-256'
        Assert-Test ($hostSource.Contains('RequestHashArgument = "-kmcRuntimeRequestSha256"')) 'in-process host does not require the request SHA-256 argument'
        Assert-Test ($hostSource.Contains('ComputeSha256(requestBytes)')) 'in-process host does not hash the exact bytes it deserializes'
    }

    Invoke-HarnessTest 'runtime update failures abort automation and always execute cleanup' {
        $mainSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Main.cs')
        $rootSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\CompositionRoot.cs')
        $hostSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeAutomationHost.cs')
        $serviceSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\GameMountedRelationshipService.cs')
        Assert-Test ($mainSource.Contains('root?.HandleUpdateFailure(exception);')) 'OnUpdate does not delegate every escaped failure to the fail-closed composition boundary'
        Assert-Test ($rootSource.Contains('runtimeAutomation?.Abort(exception);')) 'composition boundary does not abort active automation after an update failure'
        Assert-Test ($rootSource.Contains('relationship.Dismount(CleanupTrigger.Exception)')) 'composition boundary does not force update-failure relationship cleanup'
        Assert-Test ($rootSource.Contains('Always execute idempotent cleanup on a disable request')) 'repeated disable requests can bypass idempotent cleanup'
        Assert-Test ($hostSource.Contains('TryCompleteFailure(exception ??')) 'runtime abort does not commit a bounded FAIL result and quit'
        Assert-Test ($serviceSource.Contains('cleanupRetryRequired || coordinator.State == RelationshipState.Faulted')) 'frame validation does not detect a prior lifecycle cleanup failure'
        Assert-Test ($serviceSource.Contains('RetryFailedCleanupOrThrow();')) 'faulted lifecycle cleanup is not retried or escalated into the fail-closed update boundary'
    }

    Invoke-HarnessTest 'lifecycle evidence is a durable pre-mount gate with bounded cleanup observation' {
        $source = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeLifecycleScenarioEngine.cs')
        $evidenceGate = $source.IndexOf('if (!TryWriteEvidence("pre-mount", null, null))', [StringComparison]::Ordinal)
        $mountCall = $source.IndexOf('var mounted = relationship.MountAutomationPair();', [StringComparison]::Ordinal)
        Assert-Test ($evidenceGate -ge 0 -and $mountCall -gt $evidenceGate) 'valid-pair mounting is not gated by durable pre-mount evidence'
        Assert-Test ($source.Contains('stream.Flush(true);')) 'lifecycle JSONL records are not durably flushed before their handles close'
        Assert-Test ($source.Contains('Post-cleanup verification threw')) 'post-cleanup observation is not converted to a bounded failed row'
        Assert-Test ($source.Contains('var cleanUnmounted = result != null && result.Succeeded')) 'cleanup PASS is not derived from the exact successful Unmounted transition contract'
        Assert-Test ($source.Contains('result.State == RelationshipState.Unmounted')) 'cleanup PASS does not require Unmounted state'
        Assert-Test ($source.Contains('!result.MovementAuthorityResidual && !result.PresentationResidual')) 'cleanup PASS does not require zero owned residue'
    }

    Invoke-HarnessTest 'schema-v2 final result recomputes subscenario totals from validated game evidence' {
        $fixture = [ordered]@{
            baseline=[ordered]@{internalName='KMC_AUTOMATION_BASELINE';fileName='Manual_1_KMC_AUTOMATION_BASELINE.zks';sha256=('11'*32);length=1;lastWriteTimeUtcTicks=1;gameId='11111111-2222-3333-4444-555555555555';gameName='KMC Test Campaign';area='0123456789abcdef0123456789abcdef'}
            working=[ordered]@{internalName='KMC_AUTOMATION_WORKING';fileName='Manual_2_KMC_AUTOMATION_WORKING.zks';sha256=('22'*32);length=1;lastWriteTimeUtcTicks=1;gameId='11111111-2222-3333-4444-555555555555';gameName='KMC Test Campaign';area='0123456789abcdef0123456789abcdef'}
            writeAuthorization=[ordered]@{mode='working-only';allowedInternalName='KMC_AUTOMATION_WORKING';allowedFileName='Manual_2_KMC_AUTOMATION_WORKING.zks';baselineImmutable=$true}
        }
        $recomputeEvidence = Join-Path $runtimeEvidenceTestRoot 'recompute-evidence'
        $v2Request=[pscustomobject]@{runId='recompute-test';scenario='fixture-intake';branch='codex/mounted-combat-feasibility';commit=('0'*40);productVersion='0.0.1-feasibility';dllSha256=('a'*64);dllMvid=[Guid]::Empty.ToString();transactionToken=('b'*64);evidenceRoot=$recomputeEvidence;fixture=$fixture}
        $recomputeManifestHash = New-TestArtifactManifest -EvidenceRoot $recomputeEvidence -RunId $v2Request.runId -Scenario $v2Request.scenario
        $game=[pscustomobject]@{status='PASS';fixture=$fixture;evidenceManifestSha256=$recomputeManifestHash;subscenarioTotal=99;subscenarioPassCount=0;subscenarioFailCount=99;assertionPassCount=0;assertionFailCount=99;subscenarioResults=@([pscustomobject]@{name='observe-mount-diagnostic-availability';status='PASS';assertionPassCount=4;assertionFailCount=0;errors=@()})}
        $final=New-KmcRuntimeResultV2 -Request $v2Request -ValidatedGameResult $game -StartedAtUtc ([DateTimeOffset]::UtcNow) -ModsRestored $true -BaselineImmutable $true -WorkingRestored $true -SaveWriteAllowlistPassed $true -RestoredSaveInventoryDigest ('c'*64) -GameResultSha256 ('d'*64)
        Assert-Test ([int]$final.subscenarioTotal -eq 1 -and [int]$final.subscenarioPassCount -eq 1 -and [int]$final.subscenarioFailCount -eq 0) 'final result copied untrusted aggregate subscenario totals'
        Assert-Test ([int]$final.assertionPassCount -eq 4 -and [int]$final.assertionFailCount -eq 0 -and [string]$final.status -ceq 'PASS') 'final result did not recompute assertion totals and status'
        Assert-Test ([string]$final.evidenceManifestSha256 -ceq $recomputeManifestHash) 'final result did not echo the structurally validated game-result evidence manifest hash'
    }

    Invoke-HarnessTest 'schema-v2 fallback creates and binds a validated orchestration artifact manifest' {
        $fallbackEvidence = Join-Path $runtimeEvidenceTestRoot 'fallback-evidence'
        $fallbackRequest=[pscustomobject]@{runId='fallback-test';scenario='fixture-intake';branch='codex/mounted-combat-feasibility';commit=('0'*40);productVersion='0.0.1-feasibility';dllSha256=('a'*64);dllMvid=[Guid]::Empty.ToString();transactionToken=('b'*64);evidenceRoot=$fallbackEvidence;fixture=[ordered]@{baseline=[ordered]@{};working=[ordered]@{};writeAuthorization=[ordered]@{}}}
        $final=New-KmcRuntimeResultV2 -Request $fallbackRequest -ValidatedGameResult $null -StartedAtUtc ([DateTimeOffset]::UtcNow) -ModsRestored $true -BaselineImmutable $true -WorkingRestored $true -SaveWriteAllowlistPassed $true -RestoredSaveInventoryDigest ('c'*64) -GameResultSha256 $null -Errors @('synthetic missing game result')
        $manifestPath = Join-Path $fallbackEvidence 'runtime-artifacts.json'
        Assert-Test ([string]$final.status -ceq 'FAIL') 'missing game result did not force final FAIL'
        Assert-Test ((Get-KmcSha256 $manifestPath) -ceq [string]$final.evidenceManifestSha256) 'fallback result did not bind the independently created orchestration manifest'
        $manifest = Read-KmcJson $manifestPath
        Assert-Test ($manifest.artifacts -is [Array] -and @($manifest.artifacts).Count -eq 0) 'fallback orchestration manifest is not an exact empty artifact array'
    }

    $validPackageSource = Join-Path $testRoot 'valid-package\KingmakerMountedCombat'
    New-Item -ItemType Directory -Path $validPackageSource -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $repoRoot 'Info.json') -Destination (Join-Path $validPackageSource 'Info.json')
    Copy-Item -LiteralPath (Join-Path $repoRoot 'bin\Release\KingmakerMountedCombat.dll') -Destination (Join-Path $validPackageSource 'KingmakerMountedCombat.dll')
    $validPackage = Join-Path $testRoot 'valid-package.zip'
    Compress-Archive -LiteralPath $validPackageSource -DestinationPath $validPackage
    Invoke-HarnessTest 'package validator accepts exact owned payload' {
        & (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $validPackage
    }
    Invoke-HarnessTest 'package validator rejects extra payload' {
        $extraSource = Join-Path $testRoot 'extra-package\KingmakerMountedCombat'
        New-Item -ItemType Directory -Path $extraSource -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $repoRoot 'Info.json') -Destination (Join-Path $extraSource 'Info.json')
        Copy-Item -LiteralPath (Join-Path $repoRoot 'bin\Release\KingmakerMountedCombat.dll') -Destination (Join-Path $extraSource 'KingmakerMountedCombat.dll')
        [IO.File]::WriteAllText((Join-Path $extraSource 'foreign.dll'), 'not allowed')
        $extraPackage = Join-Path $testRoot 'extra-package.zip'
        Compress-Archive -LiteralPath $extraSource -DestinationPath $extraPackage
        $threw=$false
        try { & (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $extraPackage } catch { $threw=$true }
        Assert-Test $threw 'extra package payload passed allowlist'
    }
    Invoke-HarnessTest 'package validator rejects arbitrary DLL bytes' {
        $badSource = Join-Path $testRoot 'bad-package\KingmakerMountedCombat'
        New-Item -ItemType Directory -Path $badSource -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $repoRoot 'Info.json') -Destination (Join-Path $badSource 'Info.json')
        [IO.File]::WriteAllText((Join-Path $badSource 'KingmakerMountedCombat.dll'), 'arbitrary bytes')
        $badPackage = Join-Path $testRoot 'bad-package.zip'
        Compress-Archive -LiteralPath $badSource -DestinationPath $badPackage
        $threw=$false
        try { & (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $badPackage 2>$null | Out-Null } catch { $threw=$true }
        Assert-Test $threw 'arbitrary DLL bytes passed validation'
    }

    $requestPath = Join-Path $testRoot 'runtime-request.json'
    $request = [ordered]@{
        schemaVersion = 1; runId = 'schema-test'; scenario = 'mod-load-smoke'
        branch = 'codex/mounted-combat-feasibility'; commit = '0123456789abcdef0123456789abcdef01234567'
        productVersion = '0.0.1-feasibility'; dllSha256 = ('ab' * 32)
        dllMvid = '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'
        transactionToken = ('cd' * 32)
        evidenceRoot = (Join-Path $runtimeEvidenceTestRoot 'schema-test')
        saveAccessAllowed = $false; saveName = $null
    }
    Write-KmcJsonAtomic $requestPath $request
    Invoke-HarnessTest 'runtime request schema accepts exact no-save request' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $requestPath
    }

    Invoke-HarnessTest 'runtime request schema rejects protected save' {
        $request.saveAccessAllowed = $true
        $request.saveName = 'VALUED_SAVE'
        Write-KmcJsonAtomic $requestPath $request
        $threw = $false
        try { & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $requestPath } catch { $threw = $true }
        Assert-Test $threw 'protected save request passed validation'
        $request.saveAccessAllowed = $false
        $request.saveName = $null
        Write-KmcJsonAtomic $requestPath $request
    }

    $fingerprintPath = Join-Path $repoRoot 'planning\ENVIRONMENT-FINGERPRINT.json'
    $fingerprint = Read-KmcJson $fingerprintPath
    $gameAssembly = @($fingerprint.kingmaker.files | Where-Object role -eq 'gameplayAssembly')[0]
    $ummAssembly = @($fingerprint.kingmaker.files | Where-Object role -eq 'umm')[0]
    $harmonyAssembly = @($fingerprint.kingmaker.files | Where-Object role -eq 'harmony')[0]
    $gameResultPath = Join-Path $testRoot 'runtime-game-result.json'
    $gameStarted = [DateTimeOffset]::UtcNow.AddSeconds(-2)
    $gameResult = [ordered]@{
        schemaVersion=1; runId=$request.runId; scenario=$request.scenario; status='PASS'; branch=$request.branch; commit=$request.commit
        productVersion=$request.productVersion; dllSha256=$request.dllSha256; dllMvid=$request.dllMvid; transactionToken=$request.transactionToken
        startedAtUtc=$gameStarted.ToString('o'); completedAtUtc=[DateTimeOffset]::UtcNow.ToString('o'); loadedModId='KingmakerMountedCombat'
        gameVersion=[string]$fingerprint.kingmaker.displayVersion; gameAssemblySha256=[string]$gameAssembly.sha256; gameAssemblyMvid=[string]$gameAssembly.mvid
        ummVersion='0.28.2.0'; ummSha256=[string]$ummAssembly.sha256; harmony12Version='1.2.0.1'; harmony12Sha256=[string]$harmonyAssembly.sha256
        relationshipState='Unmounted'; movementExperimentEnabled=$false; processId=$PID; currentGameMode='None'; loadedAreaPresent=$false
        saveRequestCount=0; loadRequestCount=0; frameCount=10; elapsedSeconds=1.0; errors=@()
    }
    Write-KmcJsonAtomic $gameResultPath $gameResult
    Invoke-HarnessTest 'runtime game result accepts exact platform and no-save state' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeGameResult.ps1') -GameResultPath $gameResultPath -RequestPath $requestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $PID -NotBeforeUtc $gameStarted.AddSeconds(-1)
    }
    Invoke-HarnessTest 'runtime game result rejects platform mutation' {
        $gameResult.gameAssemblySha256 = ('00' * 32)
        Write-KmcJsonAtomic $gameResultPath $gameResult
        $threw=$false
        try { & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeGameResult.ps1') -GameResultPath $gameResultPath -RequestPath $requestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $PID -NotBeforeUtc $gameStarted.AddSeconds(-1) } catch { $threw=$true }
        Assert-Test $threw 'mutated game assembly identity passed validation'
        $gameResult.gameAssemblySha256 = [string]$gameAssembly.sha256
    }

    Invoke-HarnessTest 'runtime game result preserves structured FAIL but RequirePass rejects it' {
        $gameResult.status = 'FAIL'
        $gameResult.errors = @('synthetic runtime failure')
        Write-KmcJsonAtomic $gameResultPath $gameResult
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeGameResult.ps1') -GameResultPath $gameResultPath -RequestPath $requestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $PID -NotBeforeUtc $gameStarted.AddSeconds(-1)
        $threw=$false
        try { & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeGameResult.ps1') -GameResultPath $gameResultPath -RequestPath $requestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $PID -NotBeforeUtc $gameStarted.AddSeconds(-1) -RequirePass } catch { $threw=$true }
        Assert-Test $threw 'RequirePass accepted a structured runtime FAIL'
        $gameResult.status = 'PASS'
        $gameResult.errors = @()
        Write-KmcJsonAtomic $gameResultPath $gameResult
    }

    $v2RequestPath = Join-Path $testRoot 'runtime-request-v2.json'
    $v2Fixture = [ordered]@{
        baseline=[ordered]@{internalName='KMC_AUTOMATION_BASELINE';fileName='Manual_1_KMC_AUTOMATION_BASELINE.zks';sha256=('11'*32);length=100;lastWriteTimeUtcTicks=638907000000000000;gameId='11111111-2222-3333-4444-555555555555';gameName='KMC Test Campaign';area='0123456789abcdef0123456789abcdef'}
        working=[ordered]@{internalName='KMC_AUTOMATION_WORKING';fileName='Manual_2_KMC_AUTOMATION_WORKING.zks';sha256=('22'*32);length=101;lastWriteTimeUtcTicks=638907000000000001;gameId='11111111-2222-3333-4444-555555555555';gameName='KMC Test Campaign';area='0123456789abcdef0123456789abcdef'}
        writeAuthorization=[ordered]@{mode='working-only';allowedInternalName='KMC_AUTOMATION_WORKING';allowedFileName='Manual_2_KMC_AUTOMATION_WORKING.zks';baselineImmutable=$true}
    }
    $v2Request = [ordered]@{
        schemaVersion=2;runId='schema-v2-test';scenario='mounted-pair-create-and-clear';branch=$request.branch;commit=$request.commit
        productVersion=$request.productVersion;dllSha256=$request.dllSha256;dllMvid=$request.dllMvid;transactionToken=$request.transactionToken
        evidenceRoot=(Join-Path $runtimeEvidenceTestRoot 'schema-v2-test');fixture=$v2Fixture
    }
    Write-KmcJsonAtomic $v2RequestPath $v2Request
    Invoke-HarnessTest 'runtime request schema accepts exact save-backed fixture payload' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $v2RequestPath
    }

    $v2GameResultPath = Join-Path $testRoot 'runtime-game-result-v2.json'
    $lifecycleEvidencePath = Join-Path $v2Request.evidenceRoot 'lifecycle-scenario-evidence.jsonl'
    $lifecycleRow = 'mounted-pair-create-and-clear'
    $validLifecycleRecords = @(
        (New-TestLifecycleEvidenceRecord $v2Request 0 $lifecycleRow 'pre-mount' 'Unmounted'),
        (New-TestLifecycleEvidenceRecord $v2Request 1 $lifecycleRow 'mounted-next-frame' 'Mounted'),
        (New-TestLifecycleEvidenceRecord $v2Request 2 $lifecycleRow 'cleanup-next-frame' 'Unmounted' -WithCleanup),
        (New-TestLifecycleEvidenceRecord $v2Request 3 $lifecycleRow 'row-finish' 'Unmounted' -WithCleanup -RowStatus 'PASS' -AssertionPassCount 3 -AssertionFailCount 0),
        (New-TestLifecycleEvidenceRecord $v2Request 4 $lifecycleRow 'engine-finalization' 'Unmounted' -WithCleanup)
    )
    $v2EvidenceManifestHash = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $validLifecycleRecords
    $v2Subscenario = [ordered]@{name=$lifecycleRow;status='PASS';assertionPassCount=3;assertionFailCount=0;errors=@()}
    $v2GameResult = [ordered]@{
        schemaVersion=2;runId=$v2Request.runId;scenario=$v2Request.scenario;status='PASS';branch=$v2Request.branch;commit=$v2Request.commit
        productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid;transactionToken=$v2Request.transactionToken
        startedAtUtc=$gameStarted.ToString('o');completedAtUtc=[DateTimeOffset]::UtcNow.ToString('o');loadedModId='KingmakerMountedCombat'
        gameVersion=[string]$fingerprint.kingmaker.displayVersion;gameAssemblySha256=[string]$gameAssembly.sha256;gameAssemblyMvid=[string]$gameAssembly.mvid
        ummVersion='0.28.2.0';ummSha256=[string]$ummAssembly.sha256;harmony12Version='1.2.0.1';harmony12Sha256=[string]$harmonyAssembly.sha256
        relationshipState='Unmounted';movementExperimentEnabled=$false;processId=$PID;currentGameMode='Default';loadedAreaPresent=$true
        saveRequestCount=0;loadRequestCount=1;frameCount=10;elapsedSeconds=1.0;errors=@();fixture=$v2Fixture;fixtureIdentityVerified=$true
        baselineLoadRequestCount=0;workingLoadRequestCount=1;workingSaveRequestCount=0;unauthorizedLoadRequestCount=0;unauthorizedSaveRequestCount=0
        subscenarioTotal=1;subscenarioPassCount=1;subscenarioFailCount=0;assertionPassCount=3;assertionFailCount=0;evidenceManifestSha256=$v2EvidenceManifestHash;subscenarioResults=@($v2Subscenario)
    }
    Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
    Invoke-HarnessTest 'runtime game-result schema accepts exact save-backed PASS' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeGameResult.ps1') -GameResultPath $v2GameResultPath -RequestPath $v2RequestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $PID -NotBeforeUtc $gameStarted.AddSeconds(-1) -RequirePass
    }
    Invoke-HarnessTest 'runtime game-result artifact allowlist rejects lifecycle evidence under the wrong kind' {
        $manifestPath = Join-Path $v2Request.evidenceRoot 'runtime-artifacts.json'
        $manifest = Read-KmcJson $manifestPath
        $manifest.artifacts[0].kind = 'telemetry'
        Write-KmcJsonDurable -Path $manifestPath -Value $manifest
        $v2GameResult.evidenceManifestSha256 = Get-KmcSha256 $manifestPath
        Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
        $threw=$false
        try { & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeGameResult.ps1') -GameResultPath $v2GameResultPath -RequestPath $v2RequestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $PID -NotBeforeUtc $gameStarted.AddSeconds(-1) } catch { $threw=$true }
        Assert-Test $threw 'lifecycle JSONL under the telemetry kind passed the exact artifact allowlist'
        $manifest.artifacts[0].kind = 'scenario-evidence'
        Write-KmcJsonDurable -Path $manifestPath -Value $manifest
        $v2EvidenceManifestHash = Get-KmcSha256 $manifestPath
        $v2GameResult.evidenceManifestSha256 = $v2EvidenceManifestHash
        Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
    }
    Invoke-HarnessTest 'PASS lifecycle scenario rejects missing lifecycle evidence' {
        try {
            [IO.File]::Delete($lifecycleEvidencePath)
            $v2GameResult.evidenceManifestSha256 = New-TestArtifactManifest -EvidenceRoot $v2Request.evidenceRoot -RunId $v2Request.runId -Scenario $v2Request.scenario
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
            $threw=$false
            try { & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeGameResult.ps1') -GameResultPath $v2GameResultPath -RequestPath $v2RequestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $PID -NotBeforeUtc $gameStarted.AddSeconds(-1) } catch { $threw=$true }
            Assert-Test $threw 'PASS lifecycle scenario accepted a missing lifecycle artifact'
        }
        finally {
            $v2EvidenceManifestHash = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $validLifecycleRecords
            $v2GameResult.evidenceManifestSha256 = $v2EvidenceManifestHash
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
        }
    }
    Invoke-HarnessTest 'validators reject an unmanifested known lifecycle artifact' {
        try {
            $v2GameResult.evidenceManifestSha256 = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $validLifecycleRecords -OmitManifestRecord
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
            $gameThrew=$false
            try { & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeGameResult.ps1') -GameResultPath $v2GameResultPath -RequestPath $v2RequestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $PID -NotBeforeUtc $gameStarted.AddSeconds(-1) } catch { $gameThrew=$true }
            $orchestrationThrew=$false
            try { Get-KmcValidatedOrchestrationArtifactManifestHash ([pscustomobject]$v2Request) | Out-Null } catch { $orchestrationThrew=$true }
            Assert-Test ($gameThrew -and $orchestrationThrew) 'game or orchestration validator accepted an unmanifested known lifecycle artifact'
        }
        finally {
            $v2EvidenceManifestHash = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $validLifecycleRecords
            $v2GameResult.evidenceManifestSha256 = $v2EvidenceManifestHash
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
        }
    }
    Invoke-HarnessTest 'lifecycle validator rejects malformed JSONL' {
        try {
            [IO.File]::WriteAllText($lifecycleEvidencePath, '{' + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
            $badArtifact = [ordered]@{relativePath='lifecycle-scenario-evidence.jsonl';kind='scenario-evidence';length=(Get-Item -LiteralPath $lifecycleEvidencePath).Length;sha256=(Get-KmcSha256 $lifecycleEvidencePath)}
            $v2GameResult.evidenceManifestSha256 = New-TestArtifactManifest -EvidenceRoot $v2Request.evidenceRoot -RunId $v2Request.runId -Scenario $v2Request.scenario -Artifacts @($badArtifact)
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
            $threw=$false
            try { & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeGameResult.ps1') -GameResultPath $v2GameResultPath -RequestPath $v2RequestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $PID -NotBeforeUtc $gameStarted.AddSeconds(-1) } catch { $threw=$true }
            Assert-Test $threw 'malformed lifecycle JSONL passed validation'
        }
        finally {
            $v2EvidenceManifestHash = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $validLifecycleRecords
            $v2GameResult.evidenceManifestSha256 = $v2EvidenceManifestHash
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
        }
    }
    Invoke-HarnessTest 'lifecycle validator rejects noncontiguous sequence' {
        try {
            $validLifecycleRecords[2].sequence = 7
            $v2GameResult.evidenceManifestSha256 = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $validLifecycleRecords
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
            $threw=$false
            try { & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeGameResult.ps1') -GameResultPath $v2GameResultPath -RequestPath $v2RequestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $PID -NotBeforeUtc $gameStarted.AddSeconds(-1) } catch { $threw=$true }
            Assert-Test $threw 'noncontiguous lifecycle evidence sequence passed validation'
        }
        finally {
            $validLifecycleRecords[2].sequence = 2
            $v2EvidenceManifestHash = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $validLifecycleRecords
            $v2GameResult.evidenceManifestSha256 = $v2EvidenceManifestHash
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
        }
    }
    Invoke-HarnessTest 'PASS lifecycle validator rejects incomplete mounted-row phase coverage' {
        try {
            $incomplete = @(
                (New-TestLifecycleEvidenceRecord $v2Request 0 $lifecycleRow 'pre-mount' 'Unmounted'),
                (New-TestLifecycleEvidenceRecord $v2Request 1 $lifecycleRow 'mounted-next-frame' 'Mounted'),
                (New-TestLifecycleEvidenceRecord $v2Request 2 $lifecycleRow 'row-finish' 'Unmounted' -WithCleanup -RowStatus 'PASS' -AssertionPassCount 3 -AssertionFailCount 0),
                (New-TestLifecycleEvidenceRecord $v2Request 3 $lifecycleRow 'engine-finalization' 'Unmounted' -WithCleanup)
            )
            $v2GameResult.evidenceManifestSha256 = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $incomplete
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
            $threw=$false
            try { & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeGameResult.ps1') -GameResultPath $v2GameResultPath -RequestPath $v2RequestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $PID -NotBeforeUtc $gameStarted.AddSeconds(-1) } catch { $threw=$true }
            Assert-Test $threw 'incomplete mounted lifecycle row passed validation'
        }
        finally {
            $v2EvidenceManifestHash = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $validLifecycleRecords
            $v2GameResult.evidenceManifestSha256 = $v2EvidenceManifestHash
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
        }
    }
    Invoke-HarnessTest 'PASS lifecycle validator rejects trigger-scope and cleanup-trigger mutations' {
        try {
            $validLifecycleRecords[2].cleanup.trigger = 'Death'
            Assert-TestLifecycleEvidenceRejected $v2Request $validLifecycleRecords @($v2Subscenario) 'lifecycle evidence accepted a cleanup trigger outside the exact row map'
            $validLifecycleRecords[2].cleanup.trigger = 'Manual'

            $validLifecycleRecords[1].triggerScope.nativeDeliveryObserved = $true
            Assert-TestLifecycleEvidenceRejected $v2Request $validLifecycleRecords @($v2Subscenario) 'lifecycle evidence falsely claiming native delivery passed validation'
            $validLifecycleRecords[1].triggerScope.nativeDeliveryObserved = $false

            $validLifecycleRecords[1].triggerScope.invocationPath = 'lifecycle-handler-direct'
            Assert-TestLifecycleEvidenceRejected $v2Request $validLifecycleRecords @($v2Subscenario) 'manual lifecycle evidence passed with the wrong direct invocation path'
        }
        finally {
            $validLifecycleRecords[2].cleanup.trigger = 'Manual'
            $validLifecycleRecords[1].triggerScope.nativeDeliveryObserved = $false
            $validLifecycleRecords[1].triggerScope.invocationPath = 'relationship-service-direct'
            $v2EvidenceManifestHash = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $validLifecycleRecords
            $v2GameResult.evidenceManifestSha256 = $v2EvidenceManifestHash
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
        }
    }
    Invoke-HarnessTest 'PASS lifecycle validator rejects phase frame and relationship-state mutations' {
        try {
            $validLifecycleRecords[1].frame = $validLifecycleRecords[0].frame
            Assert-TestLifecycleEvidenceRejected $v2Request $validLifecycleRecords @($v2Subscenario) 'same-frame pre-mount and mounted lifecycle phases passed validation'
            $validLifecycleRecords[1].frame = 2

            $validLifecycleRecords[2].frame = $validLifecycleRecords[1].frame
            Assert-TestLifecycleEvidenceRejected $v2Request $validLifecycleRecords @($v2Subscenario) 'same-frame mounted and cleanup lifecycle phases passed validation'
            $validLifecycleRecords[2].frame = 3

            $validLifecycleRecords[1].relationshipState = 'Unmounted'
            Assert-TestLifecycleEvidenceRejected $v2Request $validLifecycleRecords @($v2Subscenario) 'mounted-next-frame evidence passed with Unmounted relationship state'
        }
        finally {
            $validLifecycleRecords[1].frame = 2
            $validLifecycleRecords[2].frame = 3
            $validLifecycleRecords[1].relationshipState = 'Mounted'
            $v2EvidenceManifestHash = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $validLifecycleRecords
            $v2GameResult.evidenceManifestSha256 = $v2EvidenceManifestHash
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
        }
    }
    Invoke-HarnessTest 'PASS lifecycle validator rejects mounted and restored authority mutations' {
        try {
            $validLifecycleRecords[1].rider.stockAgentEnabled = $true
            Assert-TestLifecycleEvidenceRejected $v2Request $validLifecycleRecords @($v2Subscenario) 'mounted lifecycle evidence passed with the rider stock agent enabled'
            $validLifecycleRecords[1].rider.stockAgentEnabled = $false

            $validLifecycleRecords[1].rider.overrideComponentCount = 2
            Assert-TestLifecycleEvidenceRejected $v2Request $validLifecycleRecords @($v2Subscenario) 'mounted lifecycle evidence passed with multiple rider override components'
            $validLifecycleRecords[1].rider.overrideComponentCount = 1

            $validLifecycleRecords[2].rider.avoidanceDisabled = $true
            Assert-TestLifecycleEvidenceRejected $v2Request $validLifecycleRecords @($v2Subscenario) 'cleanup lifecycle evidence passed with rider avoidance still leased'
            $validLifecycleRecords[2].rider.avoidanceDisabled = $false

            $validLifecycleRecords[2].mount.agentOverrideType = 'Foreign.Override'
            Assert-TestLifecycleEvidenceRejected $v2Request $validLifecycleRecords @($v2Subscenario) 'cleanup lifecycle evidence passed with a mount override residue'
        }
        finally {
            $validLifecycleRecords[1].rider.stockAgentEnabled = $false
            $validLifecycleRecords[1].rider.overrideComponentCount = 1
            $validLifecycleRecords[2].rider.avoidanceDisabled = $false
            $validLifecycleRecords[2].mount.agentOverrideType = $null
            $v2EvidenceManifestHash = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $validLifecycleRecords
            $v2GameResult.evidenceManifestSha256 = $v2EvidenceManifestHash
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
        }
    }
    Invoke-HarnessTest 'PASS lifecycle validator rejects attachment lease and restore mutations' {
        try {
            $validLifecycleRecords[1].attachment.leaseActive = $false
            Assert-TestLifecycleEvidenceRejected $v2Request $validLifecycleRecords @($v2Subscenario) 'mounted lifecycle evidence passed without an active attachment lease'
            $validLifecycleRecords[1].attachment.leaseActive = $true

            $validLifecycleRecords[2].attachment.restoreVerified = $false
            Assert-TestLifecycleEvidenceRejected $v2Request $validLifecycleRecords @($v2Subscenario) 'cleanup lifecycle evidence passed without verified scoped-lease restoration'
            $validLifecycleRecords[2].attachment.restoreVerified = $true

            $validLifecycleRecords[2].attachment.currentRiderParent = 'Scene/ForeignParent'
            Assert-TestLifecycleEvidenceRejected $v2Request $validLifecycleRecords @($v2Subscenario) 'cleanup lifecycle evidence passed with a changed rider parent'
            $validLifecycleRecords[2].attachment.currentRiderParent = 'Scene/Units/Rider'

            $validLifecycleRecords[2].attachment.riderLocalScaleMatchesOriginal = $false
            Assert-TestLifecycleEvidenceRejected $v2Request $validLifecycleRecords @($v2Subscenario) 'cleanup lifecycle evidence passed without restored rider local scale'
        }
        finally {
            $validLifecycleRecords[1].attachment.leaseActive = $true
            $validLifecycleRecords[2].attachment.restoreVerified = $true
            $validLifecycleRecords[2].attachment.currentRiderParent = 'Scene/Units/Rider'
            $validLifecycleRecords[2].attachment.riderLocalScaleMatchesOriginal = $true
            $v2EvidenceManifestHash = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $validLifecycleRecords
            $v2GameResult.evidenceManifestSha256 = $v2EvidenceManifestHash
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
        }
    }
    Invoke-HarnessTest 'lifecycle-suite requires the exact eight-row order and coverage' {
        $suiteRows = @(Get-KmcLifecycleRuntimeRows)
        $suiteRequest = [pscustomobject][ordered]@{
            runId='lifecycle-suite-test';scenario='lifecycle-suite';branch=$v2Request.branch;commit=$v2Request.commit
            productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
            evidenceRoot=(Join-Path $runtimeEvidenceTestRoot 'lifecycle-suite-test')
        }
        $newSuiteRecords = {
            param([string[]]$Rows)
            $records = New-Object 'Collections.Generic.List[object]'
            $sequence = 0
            foreach ($row in $Rows) {
                $records.Add((New-TestLifecycleEvidenceRecord $suiteRequest ($sequence++) $row 'pre-mount' 'Unmounted'))
                if ($row -cne 'mounted-pair-invalid-pair-rejected') {
                    $records.Add((New-TestLifecycleEvidenceRecord $suiteRequest ($sequence++) $row 'mounted-next-frame' 'Mounted'))
                }
                $records.Add((New-TestLifecycleEvidenceRecord $suiteRequest ($sequence++) $row 'cleanup-next-frame' 'Unmounted' -WithCleanup))
                $records.Add((New-TestLifecycleEvidenceRecord $suiteRequest ($sequence++) $row 'row-finish' 'Unmounted' -WithCleanup -RowStatus 'PASS' -AssertionPassCount 1 -AssertionFailCount 0))
            }
            $records.Add((New-TestLifecycleEvidenceRecord $suiteRequest $sequence $Rows[$Rows.Count - 1] 'engine-finalization' 'Unmounted' -WithCleanup))
            return $records.ToArray()
        }
        $suiteSubresults = @($suiteRows | ForEach-Object { [pscustomobject][ordered]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()} })
        $suiteRecords = & $newSuiteRecords $suiteRows
        [void](Write-TestLifecycleEvidence -EvidenceRoot $suiteRequest.evidenceRoot -Request $suiteRequest -Records $suiteRecords)
        $suiteManifest = Read-KmcJson (Join-Path $suiteRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcLifecycleScenarioEvidence -Request $suiteRequest -Manifest $suiteManifest -Status 'PASS' -SubscenarioResults $suiteSubresults

        $deathCleanupRecord = @($suiteRecords | Where-Object {
            [string]$_.row -ceq 'mounted-pair-death-cleanup' -and [string]$_.phase -ceq 'cleanup-next-frame'
        })[0]
        $deathCleanupRecord.cleanup.trigger = 'Manual'
        Assert-TestLifecycleEvidenceRejected $suiteRequest $suiteRecords $suiteSubresults 'lifecycle-suite accepted Manual in the Death cleanup row'
        $deathCleanupRecord.cleanup.trigger = 'Death'

        $reorderedSubresults = @($suiteSubresults)
        $firstSubresult = $reorderedSubresults[0]
        $reorderedSubresults[0] = $reorderedSubresults[1]
        $reorderedSubresults[1] = $firstSubresult
        Assert-TestLifecycleEvidenceRejected $suiteRequest $suiteRecords $reorderedSubresults 'lifecycle-suite accepted reordered subscenario results'

        $incompleteSuiteRecords = & $newSuiteRecords @($suiteRows[0..6])
        [void](Write-TestLifecycleEvidence -EvidenceRoot $suiteRequest.evidenceRoot -Request $suiteRequest -Records $incompleteSuiteRecords)
        $suiteManifest = Read-KmcJson (Join-Path $suiteRequest.evidenceRoot 'runtime-artifacts.json')
        $threw=$false
        try { Assert-KmcLifecycleScenarioEvidence -Request $suiteRequest -Manifest $suiteManifest -Status 'PASS' -SubscenarioResults $suiteSubresults } catch { $threw=$true }
        Assert-Test $threw 'lifecycle-suite PASS accepted fewer than the exact eight ordered rows'
    }

    $movementRow = 'mounted-pair-open-ground'
    $movementRequest = [pscustomobject][ordered]@{
        runId='movement-evidence-test';scenario=$movementRow;branch=$v2Request.branch;commit=$v2Request.commit
        productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
        evidenceRoot=(Join-Path $runtimeEvidenceTestRoot 'movement-evidence-test')
    }
    $movementSubresult = [pscustomobject][ordered]@{name=$movementRow;status='PASS';assertionPassCount=20;assertionFailCount=0;errors=@()}

    Invoke-HarnessTest 'PASS movement validator accepts exact telemetry, row evidence, and cleanup proof' {
        $telemetry = @((New-TestMovementTelemetryRecord $movementRequest $movementRow 0))
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),(New-TestMovementRowRecord $movementRequest $movementRow 1))
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest $telemetry $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult)
    }
    Invoke-HarnessTest 'PASS movement telemetry requires exact row-aware pause and game-mode coherence' {
        $pauseRow = 'mounted-pair-pause-unpause'
        $pauseRequest = [pscustomobject][ordered]@{
            runId='movement-pause-coherence-test';scenario=$pauseRow;branch=$v2Request.branch;commit=$v2Request.commit
            productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
            evidenceRoot=(Join-Path $runtimeEvidenceTestRoot 'movement-pause-coherence-test')
        }
        $pauseSubresult = [pscustomobject][ordered]@{name=$pauseRow;status='PASS';assertionPassCount=20;assertionFailCount=0;errors=@()}
        $pauseScenario = @((New-TestMovementPathProbeRecord $pauseRequest $pauseRow 0),(New-TestMovementRowRecord $pauseRequest $pauseRow 1))

        # Preserve both shapes emitted by the pause row: ordinary Default frames
        # are unpaused, while the bounded pause interval is exactly Pause/true.
        $defaultTelemetry = New-TestMovementTelemetryRecord $pauseRequest $pauseRow 0
        $pausedTelemetry = New-TestMovementTelemetryRecord $pauseRequest $pauseRow 1
        $pausedTelemetry.currentGameMode = 'Pause'
        $pausedTelemetry.paused = $true
        [void](Write-TestMovementEvidence $pauseRequest.evidenceRoot $pauseRequest @($defaultTelemetry,$pausedTelemetry) $pauseScenario)
        $manifest = Read-KmcJson (Join-Path $pauseRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcMovementScenarioEvidence -Request $pauseRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($pauseSubresult)

        $mutations = @(
            [pscustomobject]@{name='Pause in another movement row';request=$movementRequest;row=$movementRow;mode='Pause';paused=$true;subresult=$movementSubresult},
            [pscustomobject]@{name='Pause while unpaused';request=$pauseRequest;row=$pauseRow;mode='Pause';paused=$false;subresult=$pauseSubresult},
            [pscustomobject]@{name='Default while paused';request=$pauseRequest;row=$pauseRow;mode='Default';paused=$true;subresult=$pauseSubresult},
            [pscustomobject]@{name='non-contract game mode';request=$pauseRequest;row=$pauseRow;mode='Cutscene';paused=$false;subresult=$pauseSubresult}
        )
        foreach ($mutation in $mutations) {
            $telemetry = New-TestMovementTelemetryRecord $mutation.request $mutation.row 0
            $telemetry.currentGameMode = $mutation.mode
            $telemetry.paused = $mutation.paused
            $scenario = @((New-TestMovementPathProbeRecord $mutation.request $mutation.row 0),(New-TestMovementRowRecord $mutation.request $mutation.row 1))
            [void](Write-TestMovementEvidence $mutation.request.evidenceRoot $mutation.request @($telemetry) $scenario)
            $manifest = Read-KmcJson (Join-Path $mutation.request.evidenceRoot 'runtime-artifacts.json')
            $threw = $false
            try { Assert-KmcMovementScenarioEvidence -Request $mutation.request -Manifest $manifest -Status 'PASS' -SubscenarioResults @($mutation.subresult) } catch { $threw = $true }
            Assert-Test $threw "PASS movement telemetry accepted incoherent pause state: $($mutation.name)"
        }
    }
    Invoke-HarnessTest 'PASS movement validator requires both exact manifested JSONL artifacts' {
        $telemetry = @((New-TestMovementTelemetryRecord $movementRequest $movementRow 0))
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),(New-TestMovementRowRecord $movementRequest $movementRow 1))
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest $telemetry $scenario -OmitTelemetryManifest)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        $threw=$false
        try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
        Assert-Test $threw 'PASS movement evidence accepted an unmanifested telemetry artifact'
    }
    Invoke-HarnessTest 'movement telemetry validator rejects noncontiguous sequence and row identity mutation' {
        $telemetry = @((New-TestMovementTelemetryRecord $movementRequest $movementRow 4))
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),(New-TestMovementRowRecord $movementRequest $movementRow 1))
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest $telemetry $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        $threw=$false
        try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
        Assert-Test $threw 'movement telemetry accepted a noncontiguous sequence'

        $telemetry = @((New-TestMovementTelemetryRecord $movementRequest 'mounted-pair-selection' 0))
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest $telemetry $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        $threw=$false
        try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
        Assert-Test $threw 'individual movement telemetry accepted a foreign row identity'
    }
    Invoke-HarnessTest 'movement telemetry accepts signed orientations but rejects non-finite values' {
        $telemetry = New-TestMovementTelemetryRecord $movementRequest $movementRow 0
        $telemetry.riderEntityOrientation = -59.3691864
        $telemetry.mountEntityOrientation = -179.999
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),(New-TestMovementRowRecord $movementRequest $movementRow 1))
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult)

        $telemetry.mountEntityOrientation = 'NaN'
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        $threw=$false
        try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
        Assert-Test $threw 'movement telemetry accepted a non-finite signed orientation'
    }
    Invoke-HarnessTest 'movement telemetry accepts exact boolean path error state' {
        $telemetry = New-TestMovementTelemetryRecord $movementRequest $movementRow 0
        $telemetry.mountPathError = $false
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),(New-TestMovementRowRecord $movementRequest $movementRow 1))
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult)
    }
    Invoke-HarnessTest 'PASS movement validator rejects calibrated residual threshold mutation' {
        $telemetry = @((New-TestMovementTelemetryRecord $movementRequest $movementRow 0))
        $rowRecord = New-TestMovementRowRecord $movementRequest $movementRow 1
        $rowRecord.maximumUpdatePreCorrectionResidualWorldUnits = 0.100001
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),$rowRecord)
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest $telemetry $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        $threw=$false
        try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
        Assert-Test $threw 'movement evidence accepted a calibrated residual above 0.10 world units'
    }
    Invoke-HarnessTest 'PASS movement validator rejects calibrated rotation and attachment mutations' {
        $telemetry = New-TestMovementTelemetryRecord $movementRequest $movementRow 0
        $telemetry.maximumLateUpdatePreCorrectionRotationResidualDegrees = 0.100001
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),(New-TestMovementRowRecord $movementRequest $movementRow 1))
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        $threw=$false
        try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
        Assert-Test $threw 'movement telemetry accepted an ongoing rotation residual above 0.10 degrees'

        $telemetry = New-TestMovementTelemetryRecord $movementRequest $movementRow 0
        $rowRecord = New-TestMovementRowRecord $movementRequest $movementRow 1
        $rowRecord.maximumLateUpdatePreCorrectionRotationResidualDegrees = 0.100001
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),$rowRecord)
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        $threw=$false
        try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
        Assert-Test $threw 'movement row evidence accepted an ongoing rotation residual above 0.10 degrees'

        $rowRecord = New-TestMovementRowRecord $movementRequest $movementRow 1
        $rowRecord.cleanupAfter.attachmentRestoreVerified = $false
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),$rowRecord)
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        $threw=$false
        try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
        Assert-Test $threw 'movement cleanup evidence accepted an unverified attachment restoration'
    }
    Invoke-HarnessTest 'PASS movement validator preserves bounded raw position lag and rejects unsafe position-phase mutations' {
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),(New-TestMovementRowRecord $movementRequest $movementRow 1))
        $newPermittedPositionTelemetry = {
            $value = New-TestMovementTelemetryRecord $movementRequest $movementRow 0
            $value.synchronizationPhase = 'LateUpdate'
            $value.latestCurrentAuthoritativeAnchorY = 2.2
            $value.latestAuthoritativePositionSequence = 2
            $value.latestPreviousAuthoritativePositionSequence = 1
            $value.latestPreviousAuthoritativeAnchorX = 1.0
            $value.latestPreviousAuthoritativeAnchorY = 2.0
            $value.latestPreviousAuthoritativeAnchorZ = 3.0
            $value.latestPreviousAuthoritativePositionFrame = 12
            $value.latestPreviousAuthoritativePositionPhase = 'Update'
            $value.latestPreviousAuthoritativePositionReferenceKind = 'same-frame-update'
            $value.latestPreviousAuthoritativePositionSameFrame = $true
            $value.latestPreviousAuthoritativePositionReferenceEligible = $true
            $value.latestAuthoritativePositionDeltaWorldUnits = 0.2
            $value.latestViewCurrentPositionResidualWorldUnits = 0.0
            $value.latestEntityRawCurrentPositionResidualWorldUnits = 0.2
            $value.latestEntityPreviousAuthoritativePositionResidualWorldUnits = 0.0
            $value.latestEntityPhaseAdjustedPositionResidualWorldUnits = 0.0
            $value.latestEntityRawPositionLagBoundWorldUnits = 0.2
            $value.latestEntityRawPositionLagExcessWorldUnits = 0.0
            $value.latestEntityPositionAuthorityAgeSteps = 1
            $value.latestPositionPhaseLagObserved = $true
            $value.latestPositionPhaseLagPermitted = $true
            $value.latestPositionPhaseLagViolation = $false
            $value.latestPositionRecoveryRequiredBeforeSample = $false
            $value.latestPositionRecoveryUpdateObserved = $false
            $value.latestPositionRecoverySatisfied = $false
            $value.latestPositionRecoveryViolation = $false
            $value.latestPositionRecoveryPendingAfterSample = $true
            $value.latestPositionStationaryAuthority = $false
            $value.latestStationaryPositionCorrectionViolation = $false
            $value.preCorrectionPositionResidualWorldUnits = 0.0
            $value.preCorrectionRawCurrentPositionResidualWorldUnits = 0.2
            $value.preCorrectionViewCurrentPositionResidualWorldUnits = 0.0
            $value.positionPhaseLagRecoveryRequiredRawCount = 0
            $value.positionPhaseLagRecoveryUpdateRawCount = 0
            $value.positionPhaseLagRecoverySatisfiedRawCount = 0
            $value.positionPhaseLagRecoveryRequiredEffectiveCount = 0
            $value.positionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount = 0
            $value.positionPhaseLagRecoverySatisfiedEffectiveCount = 0
            $value.outstandingPositionPhaseLagRecoveryCount = 1
            $value.latestRecoveryRequiredBeforeSample = $false
            $value.latestRecoveryUpdateObserved = $false
            $value.latestRecoverySatisfied = $false
            $value.latestRecoveryViolation = $false
            $value.latestRecoveryPendingAfterSample = $false
            return $value
        }

        $telemetry = & $newPermittedPositionTelemetry
        $telemetry.maximumPreCorrectionRawCurrentPositionResidualWorldUnits = 25.0
        $telemetry.maximumUpdatePreCorrectionRawCurrentPositionResidualWorldUnits = 25.0
        $telemetry.maximumCalibratedEntityRawCurrentPositionResidualWorldUnits = 25.0
        $telemetry.maximumAuthoritativePositionDeltaWorldUnits = 25.0
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult)

        foreach ($mutation in @('partial-reference','stale-frame','age-two','three-dimensional-delta','visible-view',
            'previous-residual','adjusted-residual','raw-bound','raw-excess','recovery-state','stationary-lag',
            'reference-kind','entity-age')) {
            $telemetry = & $newPermittedPositionTelemetry
            switch ($mutation) {
                'partial-reference' { $telemetry.latestPreviousAuthoritativeAnchorZ = $null }
                'stale-frame' { $telemetry.latestPreviousAuthoritativePositionFrame = 11 }
                'age-two' { $telemetry.latestAuthoritativePositionSequence = 3 }
                'three-dimensional-delta' { $telemetry.latestAuthoritativePositionDeltaWorldUnits = 0.19 }
                'visible-view' {
                    $telemetry.latestViewCurrentPositionResidualWorldUnits = 0.100001
                    $telemetry.preCorrectionPositionResidualWorldUnits = 0.100001
                    $telemetry.preCorrectionRawCurrentPositionResidualWorldUnits = 0.2
                    $telemetry.preCorrectionViewCurrentPositionResidualWorldUnits = 0.100001
                }
                'previous-residual' { $telemetry.latestEntityPreviousAuthoritativePositionResidualWorldUnits = 0.100001 }
                'adjusted-residual' { $telemetry.latestEntityPhaseAdjustedPositionResidualWorldUnits = 0.2 }
                'raw-bound' { $telemetry.latestEntityRawPositionLagBoundWorldUnits = 0.19 }
                'raw-excess' { $telemetry.latestEntityRawPositionLagExcessWorldUnits = 0.000100001 }
                'recovery-state' { $telemetry.latestPositionRecoveryRequiredBeforeSample = $true }
                'stationary-lag' {
                    $telemetry.latestCurrentAuthoritativeAnchorY = 2.0
                    $telemetry.latestAuthoritativePositionDeltaWorldUnits = 0.0
                    $telemetry.latestEntityRawPositionLagBoundWorldUnits = 0.0
                    $telemetry.latestEntityRawPositionLagExcessWorldUnits = 0.2
                    $telemetry.latestPositionStationaryAuthority = $true
                }
                'reference-kind' { $telemetry.latestPreviousAuthoritativePositionReferenceKind = 'prior-frame-update' }
                'entity-age' { $telemetry.latestEntityPositionAuthorityAgeSteps = 2 }
            }
            [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
            $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
            $threw=$false
            try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
            Assert-Test $threw "movement telemetry accepted unsafe position-phase mutation $mutation"
        }

        foreach ($mutation in @('effective-residual','raw-lag-excess','stale-reference-count','raw-recovery',
            'effective-recovery','stationary-correction','outstanding-two','epsilon','boundary-count','null-aggregate','null-count')) {
            $telemetry = New-TestMovementTelemetryRecord $movementRequest $movementRow 0
            switch ($mutation) {
                'effective-residual' { $telemetry.maximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits = 0.100001 }
                'raw-lag-excess' { $telemetry.maximumEntityRawPositionLagExcessWorldUnits = 0.000100001 }
                'stale-reference-count' { $telemetry.positionPhaseLagSameFrameUpdateReferenceCount = 0 }
                'raw-recovery' { $telemetry.positionPhaseLagRecoverySatisfiedRawCount = 0 }
                'effective-recovery' { $telemetry.positionPhaseLagRecoverySatisfiedEffectiveCount = 0 }
                'stationary-correction' { $telemetry.stationaryPositionCorrectionViolationCount = 1 }
                'outstanding-two' { $telemetry.outstandingPositionPhaseLagRecoveryCount = 2 }
                'epsilon' { $telemetry.entityRawPositionLagArithmeticCoherenceEpsilonWorldUnits = 0.0002 }
                'boundary-count' { $telemetry.stationaryBoundaryClosureAttemptCount = 1 }
                'null-aggregate' { $telemetry.maximumEntityRawPositionLagExcessWorldUnits = $null }
                'null-count' { $telemetry.positionPhaseLagObservedCount = $null }
            }
            [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
            $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
            $threw=$false
            try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
            Assert-Test $threw "movement telemetry accepted unsafe position aggregate mutation $mutation"
        }
    }
    Invoke-HarnessTest 'PASS movement row validator reconciles synchronous stationary boundary closure' {
        $telemetry = New-TestMovementTelemetryRecord $movementRequest $movementRow 0
        $rowRecord = New-TestMovementRowRecord $movementRequest $movementRow 1
        $rowRecord.maximumRawCurrentPositionResidualWorldUnits = 25.0
        $rowRecord.maximumUpdateRawCurrentPositionResidualWorldUnits = 25.0
        $rowRecord.maximumEntityRawCurrentPositionResidualWorldUnits = 25.0
        $rowRecord.maximumAuthoritativePositionDeltaWorldUnits = 25.0
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),$rowRecord)
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult)

        $noPending = New-TestMovementRowRecord $movementRequest $movementRow 1
        foreach ($name in @('positionPhaseLagRecoveryRequiredRawCount','positionPhaseLagRecoveryUpdateRawCount',
            'positionPhaseLagRecoverySatisfiedRawCount','phaseLagRecoveryRequiredCount','phaseLagRecoveryUpdateCount',
            'phaseLagRecoverySatisfiedCount','phaseLagRecoveryRequiredRawCount','phaseLagRecoveryUpdateRawCount',
            'phaseLagRecoverySatisfiedRawCount')) { $noPending.$name = 1 }
        $noPending.finalSynchronizationBoundaryClosureAttempted = $false
        $noPending.finalSynchronizationBoundaryClosureReason = 'no-pending-recovery'
        $noPending.finalSynchronizationBoundaryYawPendingBefore = 0
        $noPending.finalSynchronizationBoundaryPositionPendingBefore = 0
        $noPending.finalSynchronizationBoundaryYawClosedCount = 0
        $noPending.finalSynchronizationBoundaryPositionClosedCount = 0
        $noPending.stationaryBoundaryClosureAttemptCount = 0
        $noPending.stationaryBoundaryClosureSucceededCount = 0
        $noPending.yawPhaseLagStationaryBoundaryClosureCount = 0
        $noPending.positionPhaseLagStationaryBoundaryClosureCount = 0
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),$noPending)
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult)

        foreach ($mutation in @('position-effective','position-outstanding','position-stationary','position-epsilon',
            'boundary-view-position','boundary-entity-position','boundary-position-split','authority-advance','yaw-authority-advance',
            'command-present','wants-to-move','really-moving','closure-repeat','closure-channel','closure-attempt',
            'closure-too-many','closure-reason','closure-pending-after','final-position-outstanding')) {
            $rowRecord = New-TestMovementRowRecord $movementRequest $movementRow 1
            switch ($mutation) {
                'position-effective' { $rowRecord.positionPhaseLagRecoverySatisfiedEffectiveCount = 0 }
                'position-outstanding' { $rowRecord.outstandingPositionPhaseLagRecoveryCount = 1 }
                'position-stationary' { $rowRecord.stationaryPositionCorrectionViolationCount = 1 }
                'position-epsilon' { $rowRecord.entityRawPositionLagArithmeticCoherenceEpsilonWorldUnits = 0.0002 }
                'boundary-view-position' {
                    $rowRecord.finalSynchronizationBoundaryViewPositionResidualWorldUnits = 0.100001
                    $rowRecord.finalSynchronizationBoundaryPositionResidualWorldUnits = 0.100001
                }
                'boundary-entity-position' {
                    $rowRecord.finalSynchronizationBoundaryEntityPositionResidualWorldUnits = 0.100001
                    $rowRecord.finalSynchronizationBoundaryPositionResidualWorldUnits = 0.100001
                }
                'boundary-position-split' { $rowRecord.finalSynchronizationBoundaryPositionResidualWorldUnits = 0.01 }
                'authority-advance' { $rowRecord.finalSynchronizationBoundaryAuthoritativePositionAdvanceWorldUnits = 0.0000011 }
                'yaw-authority-advance' { $rowRecord.finalSynchronizationBoundaryAuthoritativeYawAdvanceDegrees = 0.0000011 }
                'command-present' { $rowRecord.finalSynchronizationBoundaryMovementCommandAbsent = $false }
                'wants-to-move' { $rowRecord.finalSynchronizationBoundaryWantsToMove = $true }
                'really-moving' { $rowRecord.finalSynchronizationBoundaryIsReallyMoving = $true }
                'closure-repeat' {
                    $rowRecord.stationaryBoundaryClosureAttemptCount = 2
                    $rowRecord.stationaryBoundaryClosureSucceededCount = 2
                }
                'closure-channel' { $rowRecord.positionPhaseLagStationaryBoundaryClosureCount = 0 }
                'closure-attempt' { $rowRecord.finalSynchronizationBoundaryClosureAttempted = $false }
                'closure-too-many' {
                    $rowRecord.finalSynchronizationBoundaryPositionPendingBefore = 2
                    $rowRecord.finalSynchronizationBoundaryPositionClosedCount = 2
                }
                'closure-reason' { $rowRecord.finalSynchronizationBoundaryClosureReason = 'no-pending-recovery' }
                'closure-pending-after' { $rowRecord.finalSynchronizationBoundaryPositionPendingAfter = 1 }
                'final-position-outstanding' { $rowRecord.finalSynchronizationOutstandingPositionRecoveryCount = 1 }
            }
            $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),$rowRecord)
            [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
            $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
            $threw=$false
            try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
            Assert-Test $threw "movement row evidence accepted unsafe stationary-boundary mutation $mutation"
        }
    }
    Invoke-HarnessTest 'PASS movement validator preserves raw yaw lag but rejects unsafe phase-order mutations' {
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),(New-TestMovementRowRecord $movementRequest $movementRow 1))

        $telemetry = New-TestMovementTelemetryRecord $movementRequest $movementRow 0
        $telemetry.maximumCalibratedEntityRawCurrentYawResidualDegrees = 25.0
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult)

        foreach ($mutation in @('visible-view','full-view-quaternion','mount-coherence','raw-lag-excess','stale-reference-count',
            'unmarked-latest-lag','impossible-recovery-phase','partial-reference','epsilon')) {
            $telemetry = New-TestMovementTelemetryRecord $movementRequest $movementRow 0
            switch ($mutation) {
                'visible-view' { $telemetry.maximumCalibratedViewCurrentYawResidualDegrees = 0.100001 }
                'full-view-quaternion' { $telemetry.maximumCalibratedFullViewCurrentRotationResidualDegrees = 0.100001 }
                'mount-coherence' { $telemetry.maximumCalibratedMountEntityRootYawResidualDegrees = 0.100001 }
                'raw-lag-excess' { $telemetry.maximumEntityRawLagExcessDegrees = 0.000100001 }
                'stale-reference-count' { $telemetry.phaseLagSameFrameUpdateReferenceCount = 0 }
                'unmarked-latest-lag' {
                    $telemetry.latestAuthoritativeYawDeltaDegrees = 8.0
                    $telemetry.latestEntityRawCurrentYawResidualDegrees = 8.0
                    $telemetry.latestEntityPhaseAdjustedYawResidualDegrees = 0.0
                    $telemetry.latestEntityRawLagBoundDegrees = 8.0
                    $telemetry.latestEntityRawLagExcessDegrees = 0.0
                    $telemetry.latestEntityYawAuthorityAgeSteps = $null
                    $telemetry.latestPhaseLagObserved = $false
                    $telemetry.latestPhaseLagPermitted = $false
                    $telemetry.latestPhaseLagViolation = $false
                    $telemetry.latestStationaryAuthority = $false
                }
                'impossible-recovery-phase' { $telemetry.synchronizationPhase = 'LateUpdate' }
                'partial-reference' { $telemetry.latestPreviousAuthoritativeYawSequence = 1 }
                'epsilon' { $telemetry.entityRawLagArithmeticCoherenceEpsilonDegrees = 0.0002 }
            }
            [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
            $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
            $threw=$false
            try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
            Assert-Test $threw "movement telemetry accepted unsafe phase-order mutation $mutation"
        }

        foreach ($mutation in @('pending-recovery','missing-recovery','stationary-lag','adjusted-lag','full-view-quaternion',
            'final-snapshot','final-boundary-view','final-recovery','epsilon')) {
            $telemetry = New-TestMovementTelemetryRecord $movementRequest $movementRow 0
            $rowRecord = New-TestMovementRowRecord $movementRequest $movementRow 1
            switch ($mutation) {
                'pending-recovery' { $rowRecord.outstandingPhaseLagRecoveryCount = 1 }
                'missing-recovery' { $rowRecord.phaseLagRecoverySatisfiedEffectiveCount = 0 }
                'stationary-lag' { $rowRecord.stationaryYawCorrectionViolationCount = 1 }
                'adjusted-lag' { $rowRecord.maximumEntityPhaseAdjustedYawResidualDegrees = 0.100001 }
                'full-view-quaternion' { $rowRecord.maximumFullViewCurrentRotationResidualDegrees = 0.100001 }
                'final-snapshot' { $rowRecord.finalSynchronizationQualificationPassed = $false }
                'final-boundary-view' { $rowRecord.finalSynchronizationBoundaryFullViewRotationResidualDegrees = 0.100001 }
                'final-recovery' { $rowRecord.finalSynchronizationOutstandingRecoveryCount = 1 }
                'epsilon' { $rowRecord.entityRawLagArithmeticCoherenceEpsilonDegrees = 0.0002 }
            }
            $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),$rowRecord)
            [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
            $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
            $threw=$false
            try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
            Assert-Test $threw "movement row evidence accepted unsafe phase-order mutation $mutation"
        }
    }
    Invoke-HarnessTest 'PASS movement validator rejects cleanup-after residue mutation' {
        $telemetry = @((New-TestMovementTelemetryRecord $movementRequest $movementRow 0))
        $rowRecord = New-TestMovementRowRecord $movementRequest $movementRow 1
        $rowRecord.cleanupAfter.hasMountedResidual = $true
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),$rowRecord)
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest $telemetry $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        $threw=$false
        try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
        Assert-Test $threw 'movement evidence accepted cleanup-after mounted residue'
    }
    Invoke-HarnessTest 'movement row-result validator rejects subresult reconciliation mutation' {
        $telemetry = @((New-TestMovementTelemetryRecord $movementRequest $movementRow 0))
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),(New-TestMovementRowRecord $movementRequest $movementRow 1))
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest $telemetry $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        $mutated = [pscustomobject][ordered]@{name=$movementRow;status='PASS';assertionPassCount=19;assertionFailCount=0;errors=@()}
        $threw=$false
        try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($mutated) } catch { $threw=$true }
        Assert-Test $threw 'movement row-result accepted mismatched subscenario assertion totals'
    }
    Invoke-HarnessTest 'movement-suite requires exact eight-row coverage and order in both artifacts' {
        $suiteRows = @(Get-KmcMovementRuntimeRows)
        $suiteRequest = [pscustomobject][ordered]@{
            runId='movement-suite-evidence-test';scenario='movement-suite';branch=$v2Request.branch;commit=$v2Request.commit
            productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
            evidenceRoot=(Join-Path $runtimeEvidenceTestRoot 'movement-suite-evidence-test')
        }
        $telemetry = New-Object 'Collections.Generic.List[object]'
        $scenario = New-Object 'Collections.Generic.List[object]'
        $subresults = New-Object 'Collections.Generic.List[object]'
        $scenarioSequence = 0
        for ($index = 0; $index -lt $suiteRows.Count; $index++) {
            $row = $suiteRows[$index]
            $telemetry.Add((New-TestMovementTelemetryRecord $suiteRequest $row $index))
            if ($row -ceq 'mounted-pair-doorway') {
                $scenario.Add((New-TestMovementPathProbeRecord $suiteRequest $row ($scenarioSequence++) 'DoorNear' $false))
                $scenario.Add((New-TestMovementPathProbeRecord $suiteRequest $row ($scenarioSequence++) 'DoorFar' $true))
                $scenario.Add((New-TestMovementPathProbeRecord $suiteRequest $row ($scenarioSequence++) 'DoorNear' $true))
            }
            else {
                $probeCount = if ($row -ceq 'mounted-pair-stop-start') { 2 } elseif ($row -ceq 'mounted-pair-turns-and-corners') { 3 } else { 1 }
                for ($probeIndex = 0; $probeIndex -lt $probeCount; $probeIndex++) {
                    $scenario.Add((New-TestMovementPathProbeRecord $suiteRequest $row ($scenarioSequence++)))
                }
            }
            $scenario.Add((New-TestMovementRowRecord $suiteRequest $row ($scenarioSequence++)))
            $subresults.Add([pscustomobject][ordered]@{name=$row;status='PASS';assertionPassCount=20;assertionFailCount=0;errors=@()})
        }
        [void](Write-TestMovementEvidence $suiteRequest.evidenceRoot $suiteRequest $telemetry.ToArray() $scenario.ToArray())
        $manifest = Read-KmcJson (Join-Path $suiteRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcMovementScenarioEvidence -Request $suiteRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults $subresults.ToArray()

        $rowMutationCases = @(
            @('mounted-pair-open-ground','commandReplacementCount',1),
            @('mounted-pair-open-ground','selectionLossCount',1),
            @('mounted-pair-party-formation','nonPairInterferenceCount',1),
            @('mounted-pair-stop-start','stopCommandIssuedCount',0),
            @('mounted-pair-turns-and-corners','maximumTurnDegrees',74.9),
            @('mounted-pair-selection','selectionSwitchedBack',$false),
            @('mounted-pair-party-formation','formationSelectionNormalized',$false),
            @('mounted-pair-pause-unpause','pauseEntered',$false),
            @('mounted-pair-destination-cancel','destinationCancelCommandAbsent',$false),
            @('mounted-pair-doorway','unmountedDoorControlPassed',$false),
            @('mounted-pair-open-ground','waypointCount',2),
            @('mounted-pair-open-ground','endpointQualifiedWaypointCount',0),
            @('mounted-pair-open-ground','maximumCompletedLegFinalTargetDistanceWorldUnits',1.250001),
            @('mounted-pair-open-ground','maximumCompletedLegBestTargetDistanceWorldUnits',1.250001),
            @('mounted-pair-destination-cancel','endpointQualifiedWaypointCount',1)
        )
        foreach ($mutation in $rowMutationCases) {
            $record = @($scenario.ToArray() | Where-Object { [string]$_.kind -ceq 'movement-row-result' -and [string]$_.row -ceq [string]$mutation[0] })[0]
            $property = [string]$mutation[1]
            $original = $record[$property]
            try {
                $record[$property] = $mutation[2]
                $threw = $false
                try { Assert-KmcMovementScenarioRecord $record $suiteRequest ([long]$record.sequence) $suiteRows $true $manifest } catch { $threw = $true }
                Assert-Test $threw "PASS movement row accepted semantic mutation $($mutation[0])/$property"
            }
            finally { $record[$property] = $original }
        }

        $openGroundRecord = @($scenario.ToArray() | Where-Object { [string]$_.kind -ceq 'movement-row-result' -and [string]$_.row -ceq 'mounted-pair-open-ground' })[0]
        $originalScreenshots = @($openGroundRecord.screenshots)
        try {
            $openGroundRecord.screenshots = @($originalScreenshots[0..($originalScreenshots.Count - 2)])
            $threw = $false
            try { Assert-KmcMovementScenarioRecord $openGroundRecord $suiteRequest ([long]$openGroundRecord.sequence) $suiteRows $true $manifest } catch { $threw = $true }
            Assert-Test $threw 'PASS movement row accepted incomplete screenshot milestone/count coverage'
        }
        finally { $openGroundRecord.screenshots = $originalScreenshots }

        $pauseRecord = @($scenario.ToArray() | Where-Object { [string]$_.kind -ceq 'movement-row-result' -and [string]$_.row -ceq 'mounted-pair-pause-unpause' })[0]
        $originalPauseScreenshots = @($pauseRecord.screenshots)
        try {
            $mutatedOpenScreenshots = @($originalScreenshots)
            $mutatedPauseScreenshots = @($originalPauseScreenshots)
            $mutatedOpenScreenshots[1] = $originalPauseScreenshots[1]
            $mutatedPauseScreenshots[1] = $originalScreenshots[1]
            $openGroundRecord.screenshots = $mutatedOpenScreenshots
            $pauseRecord.screenshots = $mutatedPauseScreenshots
            $threw = $false
            try { Assert-KmcMovementScenarioRecord $openGroundRecord $suiteRequest ([long]$openGroundRecord.sequence) $suiteRows $true $manifest } catch { $threw = $true }
            Assert-Test $threw 'PASS movement rows accepted cross-row substitution of a shared screenshot milestone'
        }
        finally {
            $openGroundRecord.screenshots = $originalScreenshots
            $pauseRecord.screenshots = $originalPauseScreenshots
        }

        $doorStrictProbe = @($scenario.ToArray() | Where-Object { [string]$_.kind -ceq 'path-probe' -and [string]$_.row -ceq 'mounted-pair-doorway' })[1]
        try {
            $doorStrictProbe.strictDoor = $false
            [void](Write-TestMovementEvidence $suiteRequest.evidenceRoot $suiteRequest $telemetry.ToArray() $scenario.ToArray())
            $mutatedManifest = Read-KmcJson (Join-Path $suiteRequest.evidenceRoot 'runtime-artifacts.json')
            $threw = $false
            try { Assert-KmcMovementScenarioEvidence -Request $suiteRequest -Manifest $mutatedManifest -Status 'PASS' -SubscenarioResults $subresults.ToArray() } catch { $threw = $true }
            Assert-Test $threw 'PASS doorway accepted a non-strict same-geometry crossing probe'
        }
        finally { $doorStrictProbe.strictDoor = $true }

        [void](Write-TestMovementEvidence $suiteRequest.evidenceRoot $suiteRequest $telemetry.ToArray() $scenario.ToArray())
        $manifest = Read-KmcJson (Join-Path $suiteRequest.evidenceRoot 'runtime-artifacts.json')
        $orphanRelativePath = 'movement-visuals/orphan-manifested.png'
        $orphanPath = Join-Path $suiteRequest.evidenceRoot $orphanRelativePath.Replace('/', '\')
        [IO.File]::WriteAllBytes($orphanPath, [Text.Encoding]::UTF8.GetBytes($orphanRelativePath))
        try {
            $manifest.artifacts = @($manifest.artifacts) + @([pscustomobject][ordered]@{
                relativePath=$orphanRelativePath;kind='screenshot';length=(Get-Item $orphanPath).Length;sha256=(Get-KmcSha256 $orphanPath)
            })
            $threw = $false
            try { Assert-KmcMovementScenarioEvidence -Request $suiteRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults $subresults.ToArray() } catch { $threw = $true }
            Assert-Test $threw 'PASS movement evidence accepted an orphan manifested screenshot'
        }
        finally { if (Test-Path -LiteralPath $orphanPath -PathType Leaf) { Remove-Item -LiteralPath $orphanPath -Force } }

        [void](Write-TestMovementEvidence $suiteRequest.evidenceRoot $suiteRequest $telemetry.ToArray() $scenario.ToArray())
        $manifest = Read-KmcJson (Join-Path $suiteRequest.evidenceRoot 'runtime-artifacts.json')

        $reordered = @(
            @($scenario.ToArray() | Where-Object { [string]$_.row -ceq [string]$suiteRows[1] })
            @($scenario.ToArray() | Where-Object { [string]$_.row -ceq [string]$suiteRows[0] })
            @($scenario.ToArray() | Where-Object { [Array]::IndexOf($suiteRows, [string]$_.row) -ge 2 })
        )
        for ($index=0;$index -lt $reordered.Count;$index++) { $reordered[$index].sequence=$index }
        [void](Write-TestMovementEvidence $suiteRequest.evidenceRoot $suiteRequest $telemetry.ToArray() $reordered)
        $manifest = Read-KmcJson (Join-Path $suiteRequest.evidenceRoot 'runtime-artifacts.json')
        $threw=$false
        try { Assert-KmcMovementScenarioEvidence -Request $suiteRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults $subresults.ToArray() } catch { $threw=$true }
        Assert-Test $threw 'movement-suite PASS accepted reordered row evidence'
    }
    Invoke-HarnessTest 'movement source binds telemetry to rows and finalizes destroyed-view cleanup failures' {
        $writerSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\MovementTelemetryWriter.cs'))
        $engineSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeMovementScenarioEngine.cs'))
        $agentSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\RiderMovementAgent.cs'))
        $movementDomainSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MovementTelemetry.cs'))
        $commonSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'scripts\runtime\RuntimeHarness.Common.ps1'))
        Assert-Test ($writerSource.Contains('row = movementRow()')) 'movement telemetry is not bound to the active engine row'
        Assert-Test ($writerSource.Contains('System.Math.Max(riderViewPositionResidual.Value, riderEntityPositionResidual)') -and
            $writerSource.Contains('System.Math.Max(riderViewRotationResidual.Value, riderEntityRotationResidual)')) 'interval telemetry reports only cosmetic view residual instead of conservative entity/view residual'
        Assert-Test ($agentSource.Contains('positionPhaseTracker.Observe(') -and
            $agentSource.Contains('preCorrectionEntityPosition.x') -and
            $agentSource.Contains('positionObservation,') -and
            $agentSource.Contains('Quaternion.Angle(expectedRotation, Unit.transform.rotation)') -and
            $agentSource.Contains('yawPhaseTracker.Observe(') -and
            $movementDomainSource.Contains('MaximumCalibratedEntityRawCurrentPositionResidualWorldUnits') -and
            $movementDomainSource.Contains('MaximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits') -and
            $movementDomainSource.Contains('PhaseOrderPositionSafetyPassed') -and
            $movementDomainSource.Contains('telemetry.OutstandingPositionPhaseLagRecoveryCount == 0L') -and
            $movementDomainSource.Contains('MaximumCalibratedEntityRawCurrentYawResidualDegrees') -and
            $movementDomainSource.Contains('MaximumCalibratedFullViewCurrentRotationResidualDegrees') -and
            $movementDomainSource.Contains('MaximumCalibratedEntityPhaseAdjustedYawResidualDegrees') -and
            $movementDomainSource.Contains('telemetry.OutstandingPhaseLagRecoveryCount == 0L')) 'movement synchronization can pass by gating only the rider view while logical entity state lags'
        $finalFreeze = $engineSource.IndexOf('if (!FreezeFinalSynchronizationAtCleanupBoundary())', [StringComparison]::Ordinal)
        $cleanupDismount = $engineSource.IndexOf('var clean = BestEffortDismount(pendingCleanupTrigger);', [StringComparison]::Ordinal)
        Assert-Test ($finalFreeze -ge 0 -and $cleanupDismount -gt $finalFreeze -and
            $engineSource.Contains('finalSynchronizationSnapshotStage = finalSynchronizationSnapshotCaptured ? "pre-dismount-after-captures"')) 'movement cleanup can deconfigure before freezing the final post-capture synchronization snapshot'
        Assert-Test ($engineSource.Contains('finalSynchronizationBoundaryFullViewRotationResidual <= MaximumPostCorrectionRotationResidualDegrees') -and
            $engineSource.Contains('finalSynchronizationBoundaryAuthoritativePositionAdvance <= MovementPositionPhaseTracker.StationaryAuthorityEpsilonWorldUnits') -and
            $engineSource.Contains('rowOutstandingPositionPhaseLagRecoveryCount == 0L') -and
            $engineSource.Contains('rowOutstandingPhaseLagRecoveryCount == 0L')) 'final synchronization snapshot does not gate split position, full visible rotation, and zero pending recovery'
        Assert-Test ($movementDomainSource.Contains('RawLagArithmeticCoherenceEpsilonWorldUnits = 0.0001d') -and
            $movementDomainSource.Contains('RawLagArithmeticCoherenceEpsilonDegrees = 0.0001d')) 'position/yaw raw-lag arithmetic coherence epsilons are not named 0.0001 constants'
        Assert-Test (-not $engineSource.Contains('finalSynchronizationRecoveryWaitFrames') -and
            -not $engineSource.Contains('maximumCleanupSynchronizationRecoveryWaitFrames')) 'obsolete cleanup recovery-wait telemetry remains in the row producer'

        $writerStart = $writerSource.IndexOf('var sample = new', [StringComparison]::Ordinal)
        $writerEnd = $writerSource.IndexOf('            };', $writerStart, [StringComparison]::Ordinal)
        $writerBlock = $writerSource.Substring($writerStart, $writerEnd - $writerStart)
        $telemetryProducerNames = @([regex]::Matches($writerBlock, '(?m)^\s{16}([A-Za-z_]\w*)\s*(?:=|,)') |
            ForEach-Object { $_.Groups[1].Value })
        $telemetryFixtureNames = @((New-TestMovementTelemetryRecord $movementRequest $movementRow 0).Keys | ForEach-Object { [string]$_ })
        Assert-Test ($telemetryProducerNames.Count -eq 217 -and $telemetryFixtureNames.Count -eq 217 -and
            @($telemetryProducerNames | Where-Object { [Array]::IndexOf($telemetryFixtureNames, $_) -lt 0 }).Count -eq 0 -and
            @($telemetryFixtureNames | Where-Object { [Array]::IndexOf($telemetryProducerNames, $_) -lt 0 }).Count -eq 0) 'movement telemetry fixture/validator field set is not the exact 217-field producer schema'

        $rowPayloadMarker = $engineSource.IndexOf('kind = "movement-row-result"', [StringComparison]::Ordinal)
        $rowStart = $engineSource.LastIndexOf('WriteEvidence(new', $rowPayloadMarker, [StringComparison]::Ordinal)
        $rowEnd = $engineSource.IndexOf('            });', $rowStart, [StringComparison]::Ordinal)
        $rowBlock = $engineSource.Substring($rowStart, $rowEnd - $rowStart)
        $rowOwnedNames = @('schemaVersion','runId','scenario','row','branch','commit','productVersion','dllSha256','dllMvid','sequence','utcTimestamp')
        $rowPayloadNames = @([regex]::Matches($rowBlock, '(?m)^\s{16}([A-Za-z_]\w*)\s*(?:=|,)') |
            ForEach-Object { $_.Groups[1].Value })
        $rowProducerNames = @($rowOwnedNames + $rowPayloadNames)
        $rowFixtureNames = @((New-TestMovementRowRecord $movementRequest $movementRow 1).Keys | ForEach-Object { [string]$_ })
        Assert-Test ($rowPayloadNames.Count -eq 150 -and $rowProducerNames.Count -eq 161 -and $rowFixtureNames.Count -eq 161 -and
            @($rowProducerNames | Where-Object { [Array]::IndexOf($rowFixtureNames, $_) -lt 0 }).Count -eq 0 -and
            @($rowFixtureNames | Where-Object { [Array]::IndexOf($rowProducerNames, $_) -lt 0 }).Count -eq 0) 'movement row fixture/validator field set is not the exact 161-field producer schema'
        $runtimeSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\KingmakerMountedPairRuntime.cs'))
        Assert-Test ($runtimeSource.Contains('riderAvoidanceWasDisabled = riderStockAgent.AvoidanceDisabled;') -and
            $runtimeSource.Contains('riderStockAgent.AvoidanceDisabled != riderAvoidanceWasDisabled')) 'runtime does not verify its counted avoidance lease restores the captured effective state'
        Assert-Test ($engineSource.Contains('Post-cleanup verification threw')) 'post-cleanup Unity observation exceptions are not recorded as failed evidence'
        Assert-Test ($engineSource.Contains('CompleteRemainingAsNotRun("Further movement was suppressed because post-cleanup verification could not prove restoration."')) 'destroyed-view cleanup failures can still loop instead of bounded finalization'
        Assert-Test ($engineSource.Contains('RiderStateRestored()') -and $engineSource.Contains('MountStateRestored()')) 'destroyed Unity view/agent checks are not fail-closed'
        Assert-Test ($engineSource.Contains('assertions.FailureCount != failuresBeforeCleanupVerification')) 'failed cleanup restoration checks do not suppress the remaining suite rows'
        $beginRowStart = $engineSource.IndexOf('private void BeginRow()', [StringComparison]::Ordinal)
        $beginRowEnd = $engineSource.IndexOf('private void AdvanceCurrentRow()', $beginRowStart, [StringComparison]::Ordinal)
        $beginRowBlock = $engineSource.Substring($beginRowStart, $beginRowEnd - $beginRowStart)
        $rowResetIndex = $beginRowBlock.IndexOf('ResetRowMetrics();', [StringComparison]::Ordinal)
        $navigationResetIndex = $beginRowBlock.IndexOf('ResetNavigationMetrics();', [StringComparison]::Ordinal)
        Assert-Test ($rowResetIndex -ge 0 -and $navigationResetIndex -gt $rowResetIndex) 'movement row start can retain prior-navigation metrics on an early fixture failure'
        $issueStart = $engineSource.IndexOf('private void IssueSelectedMovementCommand()', [StringComparison]::Ordinal)
        $issueEnd = $engineSource.IndexOf('private void ObserveNavigation()', $issueStart, [StringComparison]::Ordinal)
        $issueBlock = $engineSource.Substring($issueStart, $issueEnd - $issueStart)
        $clickIndex = $issueBlock.IndexOf('ClickGroundHandler.MoveSelectedUnitsToPoint', [StringComparison]::Ordinal)
        $immediateObserveIndex = $issueBlock.IndexOf('ObserveUninvolvedCommands();', [StringComparison]::Ordinal)
        Assert-Test ($clickIndex -ge 0 -and $immediateObserveIndex -gt $clickIndex) 'formation routing does not observe accidental unselected recipients immediately after click dispatch'
        $observeStart = $engineSource.IndexOf('private void ObserveUninvolvedCommands()', [StringComparison]::Ordinal)
        $observeEnd = $engineSource.IndexOf('private Dictionary<UnitEntityData, object> CaptureUninvolvedMoveCommands', $observeStart, [StringComparison]::Ordinal)
        $observeBlock = $engineSource.Substring($observeStart, $observeEnd - $observeStart)
        Assert-Test ($observeBlock.Contains('TrackTouched(pair.Key);')) 'accidentally commanded unselected recipients are not enrolled in fail-closed stop/cleanup verification'
        Assert-Test ($engineSource.Contains('if (navigationMode != NavigationMode.StopEarly)') -and
            $engineSource.Contains('rowEndpointQualifiedWaypointCount++;') -and
            $engineSource.Contains('new NavigationEndpointDistanceTracker(ProgressClockHysteresis)') -and
            $engineSource.Contains('navigationEndpointDistance.Observe(mountFinalTargetDistance, suiteClock.Elapsed.TotalSeconds);') -and
            $engineSource.Contains('mountFinalTargetDistance <= ReachTolerance') -and
            $engineSource.Contains('navigationEndpointDistance.MinimumObservedDistance <= ReachTolerance') -and
            -not $engineSource.Contains('navigationBestDistance') -and
            $commonSource.Contains("'mounted-pair-stop-start' { 1L; break }") -and
            $commonSource.Contains("'mounted-pair-destination-cancel' { 0L; break }")) 'ordinary completed movement legs do not retain exact final/best 1.25 endpoint proof with bounded stop/cancel exemptions'
        $pollStart = $engineSource.IndexOf('private bool PollNavigation()', [StringComparison]::Ordinal)
        $pollEnd = $engineSource.IndexOf('private bool PollPathProbe()', $pollStart, [StringComparison]::Ordinal)
        $pollBlock = $engineSource.Substring($pollStart, $pollEnd - $pollStart)
        $boundaryIndex = $pollBlock.IndexOf('if (!ContinueAfterNavigationBoundary())', [StringComparison]::Ordinal)
        $deadlineIndex = $pollBlock.IndexOf('Authoritative mount movement exceeded its ', [StringComparison]::Ordinal)
        Assert-Test ($boundaryIndex -ge 0 -and $deadlineIndex -gt $boundaryIndex -and
            $engineSource.Contains('MovementNavigationBoundaryPolicy.SuppressesRemainingOutOfCombatRows(action);') -and
            $engineSource.Contains('BeginCleanup(CleanupTrigger.CombatStarted);') -and
            $engineSource.Contains('Selection snapshot was restored at the cleanup boundary before the proven active-combat controller rewrote it.')) 'proven CombatStarted cleanup can still age into a stale movement deadline or be mislabeled as mounted selection residue'
        $turnsStart = $engineSource.IndexOf('private void AdvanceTurnsAndCorners()', [StringComparison]::Ordinal)
        $turnsEnd = $engineSource.IndexOf('private void AdvanceDoorway()', $turnsStart, [StringComparison]::Ordinal)
        $turnsBlock = $engineSource.Substring($turnsStart, $turnsEnd - $turnsStart)
        Assert-Test ([regex]::Matches($turnsBlock, '(?s)BeginRadialNavigation\(.*?true\);').Count -eq 3 -and
            $engineSource.Contains('MovementRadialDistanceOrder.CreateLocalFirst(MinimumRadialDistance, 8.0f, MaximumRadialDistance)')) 'turns/corners does not use the unchanged bounded radial distances in local-first order for all three legs'
        Assert-Test ($engineSource.Contains('assertions.Check(rowSelectionLosses == 0,') -and
            $engineSource.Contains('selectionLossCount = rowSelectionLosses')) 'zero selection-loss is not both asserted generically and serialized for every movement row'
        Assert-Test ($engineSource.Contains('if (!probeDoorStrict && direct < MinimumRadialDistance - 1.0f)') -and
            $engineSource.Contains('.OrderByDescending(axis => Math.Abs(Vector3.Dot(axis, doorToMount)))') -and
            $engineSource.Contains('rowDoorApproachSkipped = PlanarDistance(mount.Position, doorNearPoint) < MinimumRadialDistance - 1.0f;')) 'bounded doorway candidate policy still rejects a near-side control or chooses an axis without mount-side quality ranking'
    }
    Invoke-HarnessTest 'runtime game-result schema preserves exact save-backed FAIL evidence' {
        try {
            $failedLifecycleRecords = @(
                (New-TestLifecycleEvidenceRecord $v2Request 0 $lifecycleRow 'pre-mount' 'Unmounted'),
                (New-TestLifecycleEvidenceRecord $v2Request 1 $lifecycleRow 'row-finish' 'Unmounted' -WithCleanup -RowStatus 'FAIL' -AssertionPassCount 0 -AssertionFailCount 1 -RecordErrors @('synthetic save-backed failure')),
                (New-TestLifecycleEvidenceRecord $v2Request 2 $lifecycleRow 'engine-finalization' 'Unmounted' -WithCleanup -RecordErrors @('synthetic save-backed failure'))
            )
            $v2GameResult.evidenceManifestSha256 = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $failedLifecycleRecords
            $v2GameResult.status='FAIL';$v2GameResult.errors=@('synthetic save-backed failure');$v2GameResult.fixtureIdentityVerified=$false
            $v2GameResult.loadRequestCount=0;$v2GameResult.workingLoadRequestCount=0
            $v2GameResult.subscenarioPassCount=0;$v2GameResult.subscenarioFailCount=1;$v2GameResult.assertionPassCount=0;$v2GameResult.assertionFailCount=1
            $v2GameResult.subscenarioResults=@([ordered]@{name=$lifecycleRow;status='FAIL';assertionPassCount=0;assertionFailCount=1;errors=@('synthetic save-backed failure')})
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
            & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeGameResult.ps1') -GameResultPath $v2GameResultPath -RequestPath $v2RequestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $PID -NotBeforeUtc $gameStarted.AddSeconds(-1)
            $threw=$false
            try { & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeGameResult.ps1') -GameResultPath $v2GameResultPath -RequestPath $v2RequestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $PID -NotBeforeUtc $gameStarted.AddSeconds(-1) -RequirePass } catch { $threw=$true }
            Assert-Test $threw 'RequirePass accepted a structured schema-v2 runtime FAIL'
        }
        finally {
            $v2EvidenceManifestHash = Write-TestLifecycleEvidence -EvidenceRoot $v2Request.evidenceRoot -Request $v2Request -Records $validLifecycleRecords
            $v2GameResult.evidenceManifestSha256=$v2EvidenceManifestHash
            $v2GameResult.status='PASS';$v2GameResult.errors=@();$v2GameResult.fixtureIdentityVerified=$true
            $v2GameResult.loadRequestCount=1;$v2GameResult.workingLoadRequestCount=1
            $v2GameResult.subscenarioPassCount=1;$v2GameResult.subscenarioFailCount=0;$v2GameResult.assertionPassCount=3;$v2GameResult.assertionFailCount=0
            $v2GameResult.subscenarioResults=@($v2Subscenario)
            Write-KmcJsonAtomic $v2GameResultPath $v2GameResult
        }
    }

    $v2ResultPath = Join-Path $testRoot 'runtime-result-v2.json'
    $v2Final = New-KmcRuntimeResultV2 -Request ([pscustomobject]$v2Request) -ValidatedGameResult ([pscustomobject]$v2GameResult) -StartedAtUtc $gameStarted -ModsRestored $true -BaselineImmutable $true -WorkingRestored $true -SaveWriteAllowlistPassed $true -RestoredSaveInventoryDigest ('33'*32) -GameResultSha256 (Get-KmcSha256 $v2GameResultPath)
    Write-KmcJsonAtomic $v2ResultPath $v2Final
    Invoke-HarnessTest 'runtime result schema accepts recomputed restored save-backed PASS' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeResult.ps1') -ResultPath $v2ResultPath -RequestPath $v2RequestPath
    }

    $resultPath = Join-Path $testRoot 'runtime-result.json'
    $result = [ordered]@{
        schemaVersion = 1; runId = $request.runId; scenario = $request.scenario; status = 'PASS'
        branch = $request.branch; commit = $request.commit; productVersion = $request.productVersion
        dllSha256 = $request.dllSha256; dllMvid = $request.dllMvid
        transactionToken = $request.transactionToken
        startedAtUtc = '2026-08-13T17:00:00Z'; completedAtUtc = '2026-08-13T17:00:01Z'
        modsRestored = $true; saveProtectionPassed = $true; gameResultSha256 = ('ef' * 32); errors = @()
    }
    Write-KmcJsonAtomic $resultPath $result
    Invoke-HarnessTest 'runtime result schema accepts restored PASS' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeResult.ps1') -ResultPath $resultPath -RequestPath $requestPath
    }

    Invoke-HarnessTest 'runtime result schema rejects unrestored state' {
        $result.modsRestored = $false
        Write-KmcJsonAtomic $resultPath $result
        $threw = $false
        try { & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeResult.ps1') -ResultPath $resultPath -RequestPath $requestPath } catch { $threw = $true }
        Assert-Test $threw 'unrestored runtime result passed validation'
    }
}
finally {
    if (Test-Path -LiteralPath $testRoot) {
        $resolved = [IO.Path]::GetFullPath($testRoot)
        if (-not $resolved.StartsWith($testParent + '\', [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Harness cleanup target escaped the test parent.'
        }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
    if (Test-Path -LiteralPath $runtimeEvidenceTestRoot) {
        $resolvedEvidence = [IO.Path]::GetFullPath($runtimeEvidenceTestRoot)
        if (-not $resolvedEvidence.StartsWith($runtimeEvidenceParent + '\', [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Harness runtime-evidence cleanup target escaped its test parent.'
        }
        Remove-Item -LiteralPath $resolvedEvidence -Recurse -Force
    }
}

Write-Host "TOTAL PASS=$passed FAIL=$failed"
if ($failed -ne 0) { exit 1 }
