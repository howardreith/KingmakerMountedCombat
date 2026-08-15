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

function Assert-TestThrows([scriptblock]$Body, [string]$Message) {
    $threw = $false
    try { & $Body | Out-Null } catch { $threw = $true }
    if (-not $threw) { throw $Message }
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

function New-TestPendingWorkingRequalification {
    param([Parameter(Mandatory = $true)][string]$Name)
    $root = Join-Path $testRoot ('working-requalification-' + $Name)
    $saveRoot = Join-Path $root 'saves'
    $fixtureStateRoot = Join-Path $root 'state'
    New-Item -ItemType Directory -Path $saveRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $fixtureStateRoot -Force | Out-Null
    $baselinePath = Join-Path $saveRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks'
    $workingPath = Join-Path $saveRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks'
    New-TestSaveArchive -Path $baselinePath -Name 'KMC_AUTOMATION_BASELINE'
    New-TestSaveArchive -Path $workingPath -Name 'KMC_AUTOMATION_WORKING'
    foreach ($foreignName in @(
        'Manual_3_PERSONAL.zks','Manual_4_KBP.zks','Manual_5_KMG.zks','Auto_1.zks','Quick_1.zks'
    )) {
        [IO.File]::WriteAllText((Join-Path $saveRoot $foreignName), "protected-$foreignName")
    }
    $qualificationPath = Join-Path $fixtureStateRoot 'fixture-qualification.json'
    $initialPair = Assert-KmcFixturePair `
        -SaveRoot $saveRoot `
        -QualificationPath $qualificationPath `
        -InitializeQualification
    $oldQualification = Read-KmcJson $qualificationPath
    $oldQualificationSha256 = Get-KmcSha256 $qualificationPath
    $priorSaveMetadata = Get-KmcSaveMetadataInventory $saveRoot
    $priorSaveTransactionRunId = 'prior-save-authority'
    $priorSaveTransactionStatePath = $null
    $priorLock = Open-KmcRuntimeLock -StateRoot $fixtureStateRoot -RunId $priorSaveTransactionRunId
    try {
        $priorSaveTransactionStatePath = Enter-KmcWorkingSaveTransaction `
            -Lock $priorLock `
            -Pair $initialPair `
            -SaveRoot $saveRoot `
            -StateRoot $fixtureStateRoot `
            -BackupRoot (Join-Path $root 'backups') `
            -StagingRoot (Join-Path $root 'staging') `
            -Scenario 'mounted-pair-open-ground'
        [void](Restore-KmcWorkingSaveTransaction `
            -Lock $priorLock `
            -StatePath $priorSaveTransactionStatePath `
            -SaveRoot $saveRoot `
            -BackupRoot (Join-Path $root 'backups') `
            -StagingRoot (Join-Path $root 'staging'))
    }
    finally { Close-KmcRuntimeLock $priorLock }
    Assert-KmcSaveMetadataInventoriesEqual `
        -Before $priorSaveMetadata `
        -After (Get-KmcSaveMetadataInventory $saveRoot) `
        -Description 'synthetic prior save-transaction authority restoration'
    $priorSaveTransactionStateSha256 = Get-KmcSha256 $priorSaveTransactionStatePath
    New-TestSaveArchive -Path $workingPath -Name 'KMC_AUTOMATION_WORKING' -ExtraEntry
    $revisedPair = Get-KmcValidatedFixturePair $saveRoot
    return [pscustomobject]@{
        root = $root
        saveRoot = $saveRoot
        stateRoot = $fixtureStateRoot
        qualificationPath = $qualificationPath
        baselinePath = $baselinePath
        workingPath = $workingPath
        initialPair = $initialPair
        revisedPair = $revisedPair
        oldQualification = $oldQualification
        oldQualificationSha256 = $oldQualificationSha256
        baselineSha256 = [string]$initialPair.baseline.sha256
        supersededWorkingSha256 = [string]$initialPair.working.sha256
        revisedWorkingSha256 = [string]$revisedPair.working.sha256
        priorSaveTransactionStatePath = $priorSaveTransactionStatePath
        priorSaveTransactionRunId = $priorSaveTransactionRunId
        priorSaveTransactionStateSha256 = $priorSaveTransactionStateSha256
        priorSaveMetadataDigest = [string]$priorSaveMetadata.digest
    }
}

function New-TestAuthorizedProtectedSaveEpoch {
    param([Parameter(Mandatory = $true)][string]$Name)
    $fixture = New-TestPendingWorkingRequalification $Name
    $guardPath = Join-Path $repoRoot 'scripts\runtime\Test-KmcFixtureGuard.ps1'
    & $guardPath `
        -SaveRoot $fixture.saveRoot `
        -StateRoot $fixture.stateRoot `
        -RequalifyWorking `
        -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 `
        -ExpectedBaselineSha256 $fixture.baselineSha256 `
        -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
        -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
        -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath `
        -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
        -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 `
        -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
        -Confirm:$false | Out-Null
    $autoPath = Join-Path $fixture.saveRoot 'Auto_1.zks'
    $quickPath = Join-Path $fixture.saveRoot 'Quick_1.zks'
    [IO.File]::AppendAllText($autoPath, '-user-authorized-auto-epoch')
    [IO.File]::AppendAllText($quickPath, '-user-authorized-quick-epoch')
    $auto = Get-Item -LiteralPath $autoPath -Force
    $quick = Get-Item -LiteralPath $quickPath -Force
    return [pscustomobject]@{
        fixture=$fixture
        epochId=('protected-epoch-' + $Name)
        qualificationSha256=(Get-KmcSha256 $fixture.qualificationPath)
        autoPath=$autoPath;autoName=$auto.Name;autoSha256=(Get-KmcSha256 $autoPath)
        autoLength=[long]$auto.Length;autoTicks=[long]$auto.LastWriteTimeUtc.Ticks
        quickPath=$quickPath;quickName=$quick.Name;quickSha256=(Get-KmcSha256 $quickPath)
        quickLength=[long]$quick.Length;quickTicks=[long]$quick.LastWriteTimeUtc.Ticks
    }
}

function Set-TestRuntimeLockOwnerDead {
    param([Parameter(Mandatory = $true)][string]$Path)
    $payload = Read-KmcJson $Path
    $payload.ownerProcessId = 2147483646
    Write-KmcJsonAtomic -Path $Path -Value $payload
}

function Select-TestObjectProperties {
    param(
        [Parameter(Mandatory = $true)]$Value,
        [Parameter(Mandatory = $true)][string[]]$Names
    )
    $selected = [ordered]@{}
    foreach ($name in $Names) { $selected[$name] = $Value.$name }
    return [pscustomobject]$selected
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
    $claimLimit = Get-KmcLifecycleClaimLimit $Row
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
            claimLimit=$claimLimit
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

function New-TestBoundaryRelationshipEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$Phase,
        [switch]$Suppressed,
        [switch]$RestoreHistory
    )
    $mounted = $Phase -cin @('mounted','pre-boundary') -and -not $Suppressed
    $rowStart = $Phase -ceq 'row-start'
    $cleanupLatch = $Phase -ceq 'cleanup-latch' -and -not $Suppressed
    $loading = $Phase -cin @('loading-start','loading-stop')
    $pairVisible = -not $Suppressed -and -not $loading
    $value = [ordered]@{
        state=$(if($mounted){'Mounted'}else{'Unmounted'})
        riderUniqueId=$(if($pairVisible){'boundary-rider'}else{$null});mountUniqueId=$(if($pairVisible){'boundary-mount'}else{$null})
        ownerReferencesPresent=$mounted;movementAgentPresent=$mounted
        riderStockAgentEnabled=$(if($pairVisible){(-not $mounted)}else{$null});mountStockAgentEnabled=$(if($pairVisible){$true}else{$null})
        riderAvoidanceDisabled=$(if($pairVisible){$mounted}else{$null});mountAvoidanceDisabled=$(if($pairVisible){$false}else{$null})
        riderOverridePresent=$(if($pairVisible){$mounted}else{$null});mountOverridePresent=$(if($pairVisible){$false}else{$null})
        riderForbidRotation=$(if($pairVisible){$mounted}else{$null});mountForbidRotation=$(if($pairVisible){$false}else{$null})
        riderMoveCommandPresent=$(if($pairVisible){$false}else{$null});mountMoveCommandPresent=$(if($pairVisible){$false}else{$null})
        riderMovementAgentComponentCount=$(if(-not $pairVisible){$null}elseif($mounted -or $cleanupLatch){1}else{0})
        mountMovementAgentComponentCount=$(if($pairVisible){0}else{$null})
        kmcRiderMovementAgentComponentCount=$(if($mounted -or $cleanupLatch){1}else{0})
        attachmentLeaseActive=$mounted;attachmentRestoreVerified=$(if($rowStart){[bool]$RestoreHistory}else{(-not $mounted)})
        attachmentResidue=$mounted;riderParentMatchesAttachment=$mounted
        attachmentParent=$(if($mounted){'KMC_RiderPositionAnchor'}else{$null});sourceAnchor=$(if($mounted){'Spine'}else{$null})
        kmcAnchorObjectCount=$(if($mounted -or $cleanupLatch){1}else{0})
        selectedUnitIds=[object[]]::new(0)
    }
    if (-not $loading -and -not $Suppressed) { $value.selectedUnitIds = [object[]]@('boundary-rider') }
    return $value
}

function New-TestBoundaryCleanupEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$Row,
        [Parameter(Mandatory = $true)][string]$Phase,
        [Parameter(Mandatory = $true)][long]$Frame,
        [switch]$Suppressed
    )
    $expectedTrigger = Get-KmcBoundaryExpectedCleanupTrigger $Row
    if ($Row -ceq 'native-mode-transition-cleanup') { $expectedTrigger = 'TurnBasedModeChanged' }
    $phases = @(Get-KmcBoundaryExpectedPhases $Row)
    $captured = -not $Suppressed -and [Array]::IndexOf($phases,$Phase) -ge [Array]::IndexOf($phases,'cleanup-latch')
    return [ordered]@{
        captured=$captured;captureFrame=$(if($captured){$Frame}else{$null});expectedTrigger=$expectedTrigger
        actualTrigger=$(if($captured){$expectedTrigger}else{$null});transitionSucceeded=$(if($captured){$true}else{$null})
        movementAuthorityResidual=$(if($captured){$false}else{$null});presentationResidual=$(if($captured){$false}else{$null})
        relationshipUnmounted=$(if($captured){$true}else{$null});ownerReferencesReleased=$(if($captured){$true}else{$null})
        movementAgentReleased=$(if($captured){$true}else{$null});stockAgentsRestored=$(if($captured){$true}else{$null})
        avoidanceRestored=$(if($captured){$true}else{$null});overridesRestored=$(if($captured){$true}else{$null})
        riderMovementAgentComponentsRestored=$(if($captured){$false}else{$null});forbidRotationRestored=$(if($captured){$true}else{$null})
        attachmentRestored=$(if($captured){$true}else{$null});selectionRestored=$(if($captured){$true}else{$null})
        moveCommandsRestored=$(if($captured){$true}else{$null});kmcAnchorObjectsAbsent=$(if($captured){$false}else{$null})
        allRestored=$(if($captured){$true}else{$null})
    }
}

function New-TestBoundaryFreshWorldEvidence {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][string]$Row,
        [Parameter(Mandatory = $true)][string]$Phase,
        [switch]$Suppressed
    )
    $observed = -not $Suppressed -and $Row -cin @('mounted-pair-load-safety','mounted-pair-area-transition-safety','native-area-clean-dismount') -and
        $Phase -cin @('fresh-world','row-result')
    $value = [ordered]@{
        observed=$observed
        gameId=$(if($observed){[string]$Request.fixture.working.gameId}else{$null})
        gameName=$(if($observed){[string]$Request.fixture.working.gameName}else{$null})
        area=$(if($observed){[string]$Request.fixture.working.area}else{$null})
    }
    foreach ($name in @('worldReady','pairResolved','gameIdMatches','gameNameMatches','areaMatches','relationshipClean',
        'stockAgentsEnabled','avoidanceOrdinary','overridesAbsent','riderMovementAgentComponentsAbsent','forbidRotationOrdinary',
        'attachmentResidueAbsent','selectionRestored','moveCommandsAbsent','kmcAnchorObjectsAbsent','allClean')) {
        $value[$name] = if ($observed) { $true } else { $null }
    }
    return $value
}

function New-TestBoundaryEvidenceRecord {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][string]$Row,
        [Parameter(Mandatory = $true)][string]$Phase,
        [Parameter(Mandatory = $true)][long]$Sequence,
        [Parameter(Mandatory = $true)][int]$RowIndex,
        [long]$AuthorizedLoadsBefore = 1L,
        [long]$CurrentWorkingLength = -1L,
        [long]$CurrentWorkingLastWriteTimeUtcTicks = -1L,
        [string]$CurrentWorkingSha256,
        [int]$AssertionPassCount = 12,
        [switch]$Suppressed
    )
    $phases = @(Get-KmcBoundaryExpectedPhases $Row)
    $phaseIndex = [Array]::IndexOf($phases,$Phase)
    $cleanupIndex = [Array]::IndexOf($phases,'cleanup-latch')
    $load = $Row -ceq 'mounted-pair-load-safety'
    $nativeSave = $Row -ceq 'native-save-clean-dismount'
    $nativeArea = $Row -ceq 'native-area-clean-dismount'
    $nativeMode = $Row -ceq 'native-mode-transition-cleanup'
    $nativeDisable = $Row -ceq 'presentation-residue-and-uninstall-safety'
    $nativeRow = $nativeSave -or $nativeArea -or $nativeMode -or $nativeDisable
    $area = $Row -ceq 'mounted-pair-area-transition-safety' -or $nativeArea
    $loadDispatched = -not $Suppressed -and $load -and $phaseIndex -ge $cleanupIndex
    $areaDispatched = -not $Suppressed -and $area -and $(if($nativeArea){$phaseIndex -ge $cleanupIndex}else{$Phase -cin @('loading-start','loading-stop','fresh-world','row-result')})
    $nativeDispatched = -not $Suppressed -and $nativeRow -and $phaseIndex -ge $cleanupIndex
    $saveDispatched = $nativeSave -and $nativeDispatched
    $preBoundaryIndex = [Array]::IndexOf($phases,'pre-boundary')
    $descriptorVerified = if (-not $Suppressed -and $Row -cin @('mounted-pair-save-safety','mounted-pair-load-safety','native-save-clean-dismount') -and
        $phaseIndex -ge $preBoundaryIndex) { $true } else { $null }
    $working = $Request.fixture.working
    $postInitialLength = [long]$working.length + 7L
    $postInitialTicks = [long]$working.lastWriteTimeUtcTicks + 7L
    $postInitialSha = ('5' * 64)
    $currentLength = if ($CurrentWorkingLength -ge 0L) { $CurrentWorkingLength } else { $postInitialLength }
    $currentTicks = if ($CurrentWorkingLastWriteTimeUtcTicks -ge 0L) { $CurrentWorkingLastWriteTimeUtcTicks } else { $postInitialTicks }
    $currentSha = if ([string]::IsNullOrEmpty($CurrentWorkingSha256)) { $postInitialSha } else { $CurrentWorkingSha256 }
    $postDispatchIdentityChanged = -not $Suppressed -and $load -and $Phase -cin @('loading-stop','fresh-world','row-result')
    $observedLength = if ($postDispatchIdentityChanged) { $currentLength + 1L } else { $currentLength }
    $observedTicks = if ($postDispatchIdentityChanged) { $currentTicks + 1L } else { $currentTicks }
    $observedSha = if ($postDispatchIdentityChanged) { ('6' * 64) } else { $currentSha }
    $source = if ($load -and $Phase -cin @('cleanup-latch','loading-start')) {
        'cached-immediate-pre-dispatch'
    }
    elseif ($area -and $Phase -ceq 'loading-start') {
        'cached-row-start'
    }
    else {
        switch -CaseSensitive ($Phase) {
            'pre-boundary' { 'immediate-pre-dispatch'; break }
            'cleanup-latch' { 'immediate-post-dispatch'; break }
            default { $Phase; break }
        }
    }
    $matchesPostInitial = $observedLength -eq $postInitialLength -and $observedTicks -eq $postInitialTicks -and
        [string]$observedSha -ceq $postInitialSha
    $loadDelta = if ($loadDispatched) { 1L } else { 0L }
    $loadingStart = -not $Suppressed -and ($load -or $area) -and $Phase -cin @('loading-start','loading-stop','fresh-world','row-result')
    $loadingStop = -not $Suppressed -and ($load -or $area) -and $Phase -cin @('loading-stop','fresh-world','row-result')
    $callback = -not $Suppressed -and (($load -and $Phase -cin @('loading-stop','fresh-world','row-result')) -or
        ($nativeSave -and $Phase -cin @('post-boundary','row-result')))
    $frame = [long]($Sequence + 1L)
    $rowResult = $Phase -ceq 'row-result'
    $recordErrors = [object[]]::new(0)
    if ($Suppressed) { $recordErrors = [object[]]@('Suppressed after a prior boundary failure.') }
    $expectedCleanup = Get-KmcBoundaryExpectedCleanupTrigger $Row
    if ($nativeMode) { $expectedCleanup = 'TurnBasedModeChanged' }
    $nativeDeliveries = New-Object 'Collections.Generic.List[object]'
    if ($nativeDispatched) {
        $boundary = if($nativeSave){'SaveRequest'}elseif($nativeArea){'AreaBeginUnload'}elseif($nativeMode){'TurnBasedEnabled'}else{'ModDisable'}
        $nativeSource = if($nativeSave){'SaveManager.SaveRoutine Harmony12 prefix'}elseif($nativeArea){'ISceneHandler.OnAreaBeginUnloading'}elseif($nativeMode){'ITurnBasedModeEnabledHandler.HandleTurnBasedModeStateChanged(True)'}else{'UnityModManager.ModEntry.OnToggle(false)/shutdown'}
        $nativeDeliveries.Add([ordered]@{sequence=101;boundary=$boundary;source=$nativeSource;stateBefore='Mounted';stateAfter='Unmounted';cleanupTrigger=$expectedCleanup;cleanupAttempted=$true;cleanupSucceeded=$true})
    }
    if ($nativeArea -and $Phase -cin @('fresh-world','row-result')) {
        $nativeDeliveries.Add([ordered]@{sequence=102;boundary='AreaScenesLoaded';source='IAreaLoadingStagesHandler.OnAreaScenesLoaded';stateBefore='Unmounted';stateAfter='Unmounted';cleanupTrigger=$null;cleanupAttempted=$false;cleanupSucceeded=$true})
        $nativeDeliveries.Add([ordered]@{sequence=103;boundary='AreaDidLoad';source='ISceneHandler.OnAreaDidLoad';stateBefore='Unmounted';stateAfter='Unmounted';cleanupTrigger=$null;cleanupAttempted=$false;cleanupSucceeded=$true})
        $nativeDeliveries.Add([ordered]@{sequence=104;boundary='AreaLoadingComplete';source='IAreaLoadingStagesHandler.OnAreaLoadingComplete';stateBefore='Unmounted';stateAfter='Unmounted';cleanupTrigger=$null;cleanupAttempted=$false;cleanupSucceeded=$true})
    }
    if ($nativeMode -and $Phase -cin @('post-boundary','row-result')) {
        $nativeDeliveries.Add([ordered]@{sequence=102;boundary='RealtimeEnabled';source='ITurnBasedModeEnabledHandler.HandleTurnBasedModeStateChanged(False)';stateBefore='Unmounted';stateAfter='Unmounted';cleanupTrigger='RealtimeModeChanged';cleanupAttempted=$true;cleanupSucceeded=$true})
    }
    $modeRestored = $nativeMode -and $Phase -cin @('post-boundary','row-result')
    $disableFinished = $nativeDisable -and $Phase -cin @('post-boundary','row-result')
    return [ordered]@{
        schemaVersion=2;artifactKind='boundary-scenario-evidence';runId=[string]$Request.runId;scenario=[string]$Request.scenario
        row=$Row;phase=$Phase;utcTimestamp=[DateTimeOffset]::UtcNow.ToUniversalTime().ToString('o')
        branch=[string]$Request.branch;commit=[string]$Request.commit;productVersion=[string]$Request.productVersion
        dllSha256=[string]$Request.dllSha256;dllMvid=[string]$Request.dllMvid;sequence=$Sequence;rowIndex=$RowIndex;frame=$frame
        executed=(-not $Suppressed);suppressed=[bool]$Suppressed
        rowStatus=$(if($rowResult){$(if($Suppressed){'FAIL'}else{'PASS'})}else{$null})
        assertionPassCount=$(if($rowResult){$(if($Suppressed){0}else{$AssertionPassCount})}else{$null})
        assertionFailCount=$(if($rowResult){$(if($Suppressed){1}else{0})}else{$null})
        triggerScope=[ordered]@{
            expectedCleanupTrigger=$expectedCleanup;invocationPath=(Get-KmcBoundaryInvocationPath $Row)
            nativeDeliveryObserved=(($loadDelta -gt 0L)-or$nativeDispatched);stockSaveRoutineInvoked=$saveDispatched;realWorkingSaveDispatched=$saveDispatched
            realWorkingLoadDispatched=$loadDispatched
            realAreaReloadDispatched=$areaDispatched;claimLimit=(Get-KmcBoundaryClaimLimit $Row)
        }
        workingIdentity=[ordered]@{
            internalName=[string]$working.internalName;fileName=[string]$working.fileName
            path=('C:\KmcHarnessSaves\' + [string]$working.fileName);gameId=[string]$working.gameId;gameName=[string]$working.gameName
            area=[string]$working.area;requestLength=[long]$working.length;requestLastWriteTimeUtcTicks=[long]$working.lastWriteTimeUtcTicks
            requestSha256=[string]$working.sha256;postInitialLoadLength=$postInitialLength
            postInitialLoadLastWriteTimeUtcTicks=$postInitialTicks;postInitialLoadSha256=$postInitialSha
            preDispatchLength=$(if($loadDispatched -or ($nativeSave -and $phaseIndex -ge $preBoundaryIndex)){$postInitialLength}else{$null})
            preDispatchLastWriteTimeUtcTicks=$(if($loadDispatched -or ($nativeSave -and $phaseIndex -ge $preBoundaryIndex)){$postInitialTicks}else{$null})
            preDispatchSha256=$(if($loadDispatched -or ($nativeSave -and $phaseIndex -ge $preBoundaryIndex)){$postInitialSha}else{$null})
            observedLength=$observedLength;observedLastWriteTimeUtcTicks=$observedTicks
            observedSha256=$observedSha;observedSource=$source;matchesPostInitialLoad=$matchesPostInitial
            descriptorVerified=$descriptorVerified
            descriptorInternalName=$(if($descriptorVerified){[string]$working.internalName}else{$null})
            descriptorFileName=$(if($descriptorVerified){[string]$working.fileName}else{$null})
            descriptorPath=$(if($descriptorVerified){('C:\KmcHarnessSaves\' + [string]$working.fileName)}else{$null})
            descriptorGameId=$(if($descriptorVerified){[string]$working.gameId}else{$null})
            descriptorGameName=$(if($descriptorVerified){[string]$working.gameName}else{$null})
            descriptorArea=$(if($descriptorVerified){[string]$working.area}else{$null})
            descriptorSaveType=$(if($descriptorVerified){'Manual'}else{$null})
            descriptorCompatibilityVersion=$(if($descriptorVerified){1}else{$null})
        }
        authorization=[ordered]@{
            authorizedLoadsBefore=$AuthorizedLoadsBefore;authorizedLoadsAfter=($AuthorizedLoadsBefore+$loadDelta);authorizedLoadsDelta=$loadDelta
            authorizedWritesBefore=0;authorizedWritesAfter=0;authorizedWritesDelta=0
            unauthorizedLoadsBefore=0;unauthorizedLoadsAfter=0;unauthorizedLoadsDelta=0
            unauthorizedWritesBefore=0;unauthorizedWritesAfter=0;unauthorizedWritesDelta=0
            baselineLoadsBefore=0;baselineLoadsAfter=0;baselineLoadsDelta=0
            fatalViolationsBefore=0;fatalViolationsAfter=0;fatalViolationsDelta=0
            suppressedWorkingWritesBefore=0;suppressedWorkingWritesAfter=$(if($saveDispatched){1}else{0});suppressedWorkingWritesDelta=$(if($saveDispatched){1}else{0})
            oneShotWorkingWriteSuppressionArmed=$false
        }
        loading=[ordered]@{observed=$loadingStart;startObserved=$loadingStart;stopObserved=$loadingStop;callbackObserved=$callback}
        relationship=(New-TestBoundaryRelationshipEvidence $Phase -Suppressed:$Suppressed -RestoreHistory:($RowIndex -gt 0))
        cleanup=(New-TestBoundaryCleanupEvidence $Row $Phase $frame -Suppressed:$Suppressed)
        freshWorld=(New-TestBoundaryFreshWorldEvidence $Request $Row $Phase -Suppressed:$Suppressed)
        nativeLifecycle=[ordered]@{baselineSequence=100;deliveryCount=$nativeDeliveries.Count;deliveries=$nativeDeliveries.ToArray()}
        nativeMode=[ordered]@{
            executed=$nativeMode;originalValue=$(if($nativeMode){$false}else{$null});temporaryValue=$(if($nativeMode){$true}else{$null});originalRawCacheHadValue=$(if($nativeMode){$true}else{$null})
            persistedValueBefore=$(if($nativeMode){'False'}else{$null});persistedValueAfter=$(if($modeRestored){'False'}else{$null});temporaryDeliveryAttempted=$(if($nativeMode){$nativeDispatched}else{$null})
            restoreDeliveryCompleted=$(if($nativeMode){$modeRestored}else{$null});persistedValueUnchanged=$(if($nativeMode){$modeRestored}else{$null})
        }
        modDisable=[ordered]@{
            executed=($nativeDisable -and $phaseIndex -ge $preBoundaryIndex);overlayPresentBeforeDisable=$(if($nativeDisable -and $phaseIndex -ge $preBoundaryIndex){$true}else{$null});overlayObjectCountBeforeDisable=$(if($nativeDisable -and $phaseIndex -ge $preBoundaryIndex){1}else{$null})
            disableCallbackSucceeded=$(if($nativeDisable -and $nativeDispatched){$true}else{$null});overlayReferenceAbsentImmediately=$(if($nativeDisable -and $nativeDispatched){$true}else{$null});overlayPresentOnDisabledFrame=$(if($disableFinished){$false}else{$null})
            overlayObjectCountOnDisabledFrame=$(if($disableFinished){0}else{$null});reenableCallbackSucceeded=$(if($disableFinished){$true}else{$null});overlayPresentAfterReenable=$(if($disableFinished){$true}else{$null})
            overlayObjectCountAfterReenable=$(if($disableFinished){1}else{$null})
        }
        recordErrors=$recordErrors
    }
}

function New-TestBoundaryPassRecords {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][string[]]$Rows
    )
    $records = New-Object 'Collections.Generic.List[object]'
    $sequence = 0L
    $loadsBefore = 1L
    $currentLength = [long]$Request.fixture.working.length + 7L
    $currentTicks = [long]$Request.fixture.working.lastWriteTimeUtcTicks + 7L
    $currentSha = ('5' * 64)
    for ($rowIndex = 0; $rowIndex -lt $Rows.Count; $rowIndex++) {
        $row = [string]$Rows[$rowIndex]
        foreach ($phase in @(Get-KmcBoundaryExpectedPhases $row)) {
            $records.Add((New-TestBoundaryEvidenceRecord $Request $row $phase ($sequence++) $rowIndex -AuthorizedLoadsBefore $loadsBefore `
                -CurrentWorkingLength $currentLength -CurrentWorkingLastWriteTimeUtcTicks $currentTicks -CurrentWorkingSha256 $currentSha))
        }
        if ($row -ceq 'mounted-pair-load-safety') {
            $loadsBefore++
            $currentLength++
            $currentTicks++
            $currentSha = ('6' * 64)
        }
    }
    return $records.ToArray()
}

function Write-TestBoundaryEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$EvidenceRoot,
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][array]$Records,
        [switch]$OmitManifestRecord,
        [string]$ManifestKind = 'boundary-evidence'
    )
    New-Item -ItemType Directory -Path $EvidenceRoot -Force | Out-Null
    $path = Join-Path $EvidenceRoot 'boundary-scenario-evidence.jsonl'
    $lines = @($Records | ForEach-Object { $_ | ConvertTo-Json -Compress -Depth 15 })
    [IO.File]::WriteAllText($path, ($lines -join [Environment]::NewLine) + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
    $artifacts = if ($OmitManifestRecord) { @() } else {
        @([ordered]@{relativePath='boundary-scenario-evidence.jsonl';kind=$ManifestKind;length=(Get-Item -LiteralPath $path).Length;sha256=(Get-KmcSha256 $path)})
    }
    return New-TestArtifactManifest -EvidenceRoot $EvidenceRoot -RunId $Request.runId -Scenario $Request.scenario -Artifacts $artifacts
}

function Assert-TestBoundaryEvidenceRejected {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][array]$Records,
        [Parameter(Mandatory = $true)][array]$SubscenarioResults,
        [Parameter(Mandatory = $true)][string]$FailureMessage
    )
    [void](Write-TestBoundaryEvidence $Request.evidenceRoot $Request $Records)
    $manifest = Read-KmcJson (Join-Path $Request.evidenceRoot 'runtime-artifacts.json')
    $threw = $false
    try { Assert-KmcBoundaryScenarioEvidence -Request $Request -Manifest $manifest -Status 'PASS' -SubscenarioResults $SubscenarioResults }
    catch { $threw = $true }
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
        'pose-idle' { return @('mounted-idle','pose-idle','dismounted') }
        'pose-walk-run' { return @('mounted-idle','pose-walk','pose-stopped','pose-run','dismounted') }
        'pose-turn-stop' { return @('mounted-idle','pose-stop-motion','pose-stopped','pose-turn','pose-reversal','pose-stopped','dismounted') }
        'pose-doorway-formation' {
            if ($DoorApproachSkipped) { return @('door-control','door-mounted','door-mounted','formation','formation','dismounted') }
            return @('door-control','door-control','door-mounted','door-mounted','formation','formation','dismounted')
        }
        'pose-equipment-variants' { return @('mounted-idle','pose-equipment','dismounted') }
        'ui-selection-portrait-actionbar' { return @('mounted-idle','ui-rider','ui-mount-normalized','ui-away','ui-back','dismounted') }
        'camera-follow-and-command-routing' { return @('mounted-idle','camera-moving','camera-away','camera-back','dismounted') }
        default { throw "No test screenshot contract exists for movement row $Row." }
    }
}

function New-TestMovementScreenshotRecords {
    param([Parameter(Mandatory = $true)][string]$Row, [bool]$DoorApproachSkipped = $false)
    $counts = @{}
    $records = New-Object 'Collections.Generic.List[object]'
    $rowToken = if ($Row.StartsWith('mounted-pair-', [StringComparison]::Ordinal)) { $Row.Substring('mounted-pair-'.Length) } else { $Row }
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
        poseConfigured=$true;poseHealthy=$true;poseFrameApplied=$true;poseBaselineRestoreVerified=$false
        poseComponentCount=1;poseBoneCount=7;poseProfileId='medium-humanoid-mammoth-v1'
        poseBoneInventory='Pelvis,L_Up_leg,L_leg,L_foot,R_Up_leg,R_leg,R_foot';poseFailure=$null
    }
    $after = [ordered]@{
        trigger='Manual';relationshipState='Unmounted';hasMountedResidual=$false;riderStockAgentEnabled=$true;mountStockAgentEnabled=$true
        riderAvoidanceDisabled=$false;mountAvoidanceDisabled=$false;riderOverridePresent=$false;mountOverridePresent=$false
        riderSelected=$true;mountSelected=$false;selectedUnitIds=@('movement-rider');paused=$false;riderForbidRotation=$false
        attachmentLeaseActive=$false;attachmentRestoreVerified=$true;attachmentResidue=$false;riderParentMatchesAttachment=$false
        riderParent='Area/Units/Rider';attachmentParent=$null;sourceAnchor=$null;attachmentRiskState='none'
        poseConfigured=$false;poseHealthy=$false;poseFrameApplied=$false;poseBaselineRestoreVerified=$true
        poseComponentCount=0;poseBoneCount=0;poseProfileId=$null;poseBoneInventory=$null;poseFailure=$null
    }
    $poseIdle = $Row -ceq 'pose-idle'
    $poseWalkRun = $Row -ceq 'pose-walk-run'
    $poseTurnStop = $Row -ceq 'pose-turn-stop'
    $poseDoorway = $Row -ceq 'pose-doorway-formation'
    $poseEquipment = $Row -ceq 'pose-equipment-variants'
    $uiPresentation = $Row -ceq 'ui-selection-portrait-actionbar'
    $cameraPresentation = $Row -ceq 'camera-follow-and-command-routing'
    $formation = $Row -ceq 'mounted-pair-party-formation' -or $poseDoorway
    $doorway = $Row -ceq 'mounted-pair-doorway' -or $poseDoorway
    $stopStart = $Row -ceq 'mounted-pair-stop-start'
    $turns = $Row -ceq 'mounted-pair-turns-and-corners' -or $poseTurnStop
    $selection = $Row -ceq 'mounted-pair-selection'
    $pause = $Row -ceq 'mounted-pair-pause-unpause'
    $cancel = $Row -ceq 'mounted-pair-destination-cancel'
    $waypointCount = if ($poseIdle -or $poseEquipment -or $uiPresentation) { 0 }
        elseif ($poseDoorway) { 4 }
        elseif ($doorway -or $turns) { 3 }
        elseif ($stopStart -or $poseWalkRun) { 2 }
        else { 1 }
    $endpointQualifiedWaypointCount = if ($cancel) { 0 } elseif ($stopStart) { 1 } elseif ($poseTurnStop) { 2 } else { $waypointCount }
    $equipmentEvidence = @()
    if ($poseEquipment) {
        $equipmentEvidence = @([ordered]@{index=0;isOriginal=$true;isEmpty=$true;primaryType=$null;primaryBlueprintGuid=$null;secondaryType=$null;secondaryBlueprintGuid=$null;oneHandedWeapon=$false;twoHandedWeapon=$false;shield=$false;poseHealthy=$true;poseFrameCount=12})
    }
    $uiEvidence = @()
    if ($uiPresentation) {
        $uiEvidence = @(
            [ordered]@{phase='rider-selected';expectedUnitId='movement-rider';isExactlySelected=$true;actionBarSelectedUnitId='movement-rider';actionBarActive=$true;actionBarOwned=$true;portraitControllerCount=1;portraitSelected=$true;selectionCircleCount=1;selectionCircleSelected=$true;error=$null},
            [ordered]@{phase='mount-selection-normalized-to-rider';expectedUnitId='movement-rider';isExactlySelected=$true;actionBarSelectedUnitId='movement-rider';actionBarActive=$true;actionBarOwned=$true;portraitControllerCount=1;portraitSelected=$true;selectionCircleCount=1;selectionCircleSelected=$true;error=$null},
            [ordered]@{phase='selection-away';expectedUnitId='movement-non-pair';isExactlySelected=$true;actionBarSelectedUnitId='movement-non-pair';actionBarActive=$true;actionBarOwned=$true;portraitControllerCount=1;portraitSelected=$true;selectionCircleCount=1;selectionCircleSelected=$true;error=$null},
            [ordered]@{phase='selection-back';expectedUnitId='movement-rider';isExactlySelected=$true;actionBarSelectedUnitId='movement-rider';actionBarActive=$true;actionBarOwned=$true;portraitControllerCount=1;portraitSelected=$true;selectionCircleCount=1;selectionCircleSelected=$true;error=$null}
        )
    }
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
        maximumCompletedLegFinalTargetDistanceWorldUnits=$(if($endpointQualifiedWaypointCount -eq 0){0.0}else{0.5})
        maximumCompletedLegBestTargetDistanceWorldUnits=$(if($endpointQualifiedWaypointCount -eq 0){0.0}else{0.4})
        maximumTurnDegrees=$(if($turns){90.0}elseif($poseWalkRun){86.0}else{0.0});nonPairInterferenceCount=0
        nonPairUnitId=$(if($selection -or $formation -or $uiPresentation -or $cameraPresentation){'movement-non-pair'}else{$null});mountFinalTargetDistanceWorldUnits=$(if($cancel){0.0}else{0.5})
        nonPairBestTargetDistanceWorldUnits=$(if($formation){0.4}else{0.0});nonPairFinalTargetDistanceWorldUnits=$(if($formation){0.5}else{0.0})
        minimumPairNonPairSeparationWorldUnits=$(if($formation){3.0}else{0.0});requiredPairNonPairSeparationWorldUnits=$(if($formation){2.0}else{0.0})
        unmountedDoorControlPassed=$doorway;doorApproachSkipped=$false;stopCommandIssuedCount=$(if($stopStart -or $cancel -or $poseTurnStop){1}else{0})
        restartCompleted=$stopStart;selectionMountNormalized=($selection -or $cameraPresentation);selectionSwitchedAway=$selection;selectionSwitchedBack=$selection
        formationSelectionNormalized=$formation;pauseEntered=$pause;pauseObservationSeconds=$(if($pause){1.1}else{0.0})
        pauseMaximumDriftWorldUnits=$(if($pause){0.01}else{0.0});pauseExited=$pause
        destinationCancelCommandAbsent=$cancel;destinationCancelRelationshipPreserved=$cancel
        poseProfileId='medium-humanoid-mammoth-v1';poseBoneInventory='Pelvis,L_Up_leg,L_leg,L_foot,R_Up_leg,R_leg,R_foot'
        poseObservationCount=12;poseHealthyObservationCount=12;poseFrameAppliedObservationCount=12;poseApplicationFrameCount=12
        poseFootTargetClampCount=0;poseMaximumFootTargetResidualWorldUnits=0.0;poseMaximumKneeTargetResidualWorldUnits=0.0
        poseMaximumSegmentLengthResidualWorldUnits=0.0;poseMaximumApplyMicroseconds=100.0;poseAverageApplyMicroseconds=50.0
        poseMaximumPelvisLocalFrameDeltaWorldUnits=0.01;poseMaximumLeftFootLocalFrameDeltaWorldUnits=0.01
        poseMaximumRightFootLocalFrameDeltaWorldUnits=0.01;poseMaximumComponentCount=1;poseMaximumBoneCount=7;poseFailure=$null
        walkMovingSampleCount=$(if($poseWalkRun){5}else{0});runMovingSampleCount=$(if($poseWalkRun){5}else{0})
        walkMaximumSpeedWorldUnitsPerSecond=$(if($poseWalkRun){1.0}else{0.0});runMaximumSpeedWorldUnitsPerSecond=$(if($poseWalkRun){2.0}else{0.0})
        equipmentSets=$equipmentEvidence;uiObservations=$uiEvidence;uiRiderPortraitSelected=$uiPresentation;uiRiderSelectionCircleSelected=$uiPresentation
        uiRiderActionBarOwned=$uiPresentation;uiMountNormalized=$uiPresentation;uiAwayOwned=$uiPresentation;uiBackOwned=$uiPresentation;uiOverlayRendered=$uiPresentation
        uiOverlayRepaintCountBefore=0;uiOverlayRepaintCountAfter=$(if($uiPresentation){1}else{0});uiOverlayLabel=$(if($uiPresentation){'Dismount'}else{$null});uiOverlayEnabled=$uiPresentation
        uiOverlayVisible=$uiPresentation;uiOverlayButtonActivationCount=0;uiObservationFailure=$null
        cameraFollowAccepted=$cameraPresentation;cameraObservationCount=$(if($cameraPresentation){12}else{0});cameraMinimumTargetResidualWorldUnits=$(if($cameraPresentation){0.1}else{1.0})
        cameraMaximumTargetResidualWorldUnits=$(if($cameraPresentation){1.0}else{1.0});cameraFinalTargetResidualWorldUnits=$(if($cameraPresentation){0.2}else{1.0})
        cameraMinimumRigResidualWorldUnits=$(if($cameraPresentation){4.0}else{1.0});cameraMaximumRigResidualWorldUnits=$(if($cameraPresentation){8.0}else{1.0})
        cameraAwayObserved=$cameraPresentation;cameraBackObserved=$cameraPresentation
        cleanupTrigger='Manual';cleanupSucceeded=$true;cleanupResult='state=Unmounted'
        cleanupResidual=$false;cleanupBefore=$before;cleanupAfter=$after
        selectionCoverage='SelectionManager.SelectedUnits only; active portrait and camera-follow state are not asserted.'
        poseCoverage='Exact supported seven-bone profile and baseline cleanup; subjective visual acceptability remains manual-review-only.'
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
    Invoke-HarnessTest 'offline-cloud bootstrap is one-way and never accepts an observed online state' {
        Assert-Test ((Get-KmcOfflineCloudEvidenceDisposition -CurrentSessionMessage '[AppID 640820] [offlineMode=true]' -HistoricalMessage $null) -ceq 'current-session') 'current offline-cloud evidence was rejected'
        Assert-Test ((Get-KmcOfflineCloudEvidenceDisposition -CurrentSessionMessage $null -HistoricalMessage '[AppID 640820] [offlineMode=true]' -AllowHistoricalBootstrap) -ceq 'historical-bootstrap-only') 'bounded historical bootstrap evidence was rejected'
        Assert-TestThrows { Get-KmcOfflineCloudEvidenceDisposition -CurrentSessionMessage $null -HistoricalMessage '[AppID 640820] [offlineMode=true]' } 'normal Steam safety accepted missing current-session cloud evidence'
        Assert-TestThrows { Get-KmcOfflineCloudEvidenceDisposition -CurrentSessionMessage '[AppID 640820] [offlineMode=false]' -HistoricalMessage '[AppID 640820] [offlineMode=true]' -AllowHistoricalBootstrap } 'bootstrap accepted an observed online cloud state'
        Assert-TestThrows { Get-KmcOfflineCloudEvidenceDisposition -CurrentSessionMessage $null -HistoricalMessage '[AppID 640820] [offlineMode=false]' -AllowHistoricalBootstrap } 'bootstrap accepted historical online cloud state'
    }
    Invoke-HarnessTest 'offline-cloud bootstrap is no-save-only and postflight remains strict' {
        $launcherSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\Invoke-KingmakerRuntimeScenario.ps1')
        Assert-Test ($launcherSource.Contains("if(`$BootstrapOfflineCloudEvidence -and `$isSaveBacked)")) 'launcher does not reject bootstrap for every save-backed scenario'
        Assert-Test ($launcherSource.Contains("throw '-BootstrapOfflineCloudEvidence is restricted to the no-save mod-load-smoke scenario.'")) 'launcher does not expose the exact no-save-only rejection'
        Assert-Test ($launcherSource.Contains('Assert-KmcSteamSafety $SteamPath -AllowMissingCurrentSessionCloudState:$BootstrapOfflineCloudEvidence')) 'launcher does not scope relaxed preflight evidence to the explicit bootstrap switch'
        Assert-Test ($launcherSource.Contains('try{if($processExited){[void](Assert-KmcSteamSafety $SteamPath)}}')) 'launcher postflight does not require strict current-session Steam evidence'
        Assert-Test (@([regex]::Matches($launcherSource, 'Assert-KmcSteamSafety \$SteamPath')).Count -eq 3) 'launcher Steam-safety call surface changed without updating the bootstrap proof'
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

    Invoke-HarnessTest 'Working requalification WhatIf is exact and pure' {
        $fixture = New-TestPendingWorkingRequalification 'whatif'
        $guardPath = Join-Path $repoRoot 'scripts\runtime\Test-KmcFixtureGuard.ps1'
        $qualificationBefore = Get-Item -LiteralPath $fixture.qualificationPath -Force
        $qualificationHashBefore = Get-KmcSha256 $fixture.qualificationPath
        $savesBefore = Get-KmcSaveMetadataInventory $fixture.saveRoot
        $output = @(& $guardPath `
            -SaveRoot $fixture.saveRoot `
            -StateRoot $fixture.stateRoot `
            -RequalifyWorking `
            -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 `
            -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
            -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath `
            -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
            -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 `
            -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
            -WhatIf 6>&1)
        $qualificationAfter = Get-Item -LiteralPath $fixture.qualificationPath -Force
        Assert-Test (($output -join "`n") -like '*Working fixture requalification WhatIf PASS*') 'requalification WhatIf did not report its exact pure result'
        Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $qualificationHashBefore) 'requalification WhatIf changed qualification bytes'
        Assert-Test ($qualificationAfter.Length -eq $qualificationBefore.Length -and
            $qualificationAfter.LastWriteTimeUtc.Ticks -eq $qualificationBefore.LastWriteTimeUtc.Ticks) 'requalification WhatIf changed qualification metadata'
        Assert-KmcSaveMetadataInventoriesEqual -Before $savesBefore -After (Get-KmcSaveMetadataInventory $fixture.saveRoot) -Description 'synthetic WhatIf save metadata'
        Assert-Test (-not (Test-Path -LiteralPath (Join-Path $fixture.stateRoot 'active-transaction.lock'))) 'requalification WhatIf left a runtime lock'
        $threw = $false
        try { Assert-KmcFixturePair -SaveRoot $fixture.saveRoot -QualificationPath $fixture.qualificationPath | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'requalification WhatIf silently admitted revised Working'
    }

    Invoke-HarnessTest 'Working requalification changes only the fingerprint and timestamp' {
        $fixture = New-TestPendingWorkingRequalification 'success'
        $guardPath = Join-Path $repoRoot 'scripts\runtime\Test-KmcFixtureGuard.ps1'
        $savesBefore = Get-KmcSaveMetadataInventory $fixture.saveRoot
        $baselineBefore = Read-KmcFixtureHeader -Path $fixture.baselinePath -Kind baseline -SaveRoot $fixture.saveRoot
        $foreignBefore = Get-KmcSha256 (Join-Path $fixture.saveRoot 'Manual_3_PERSONAL.zks')
        $oldQualification = Read-KmcJson $fixture.qualificationPath
        $output = @(& $guardPath `
            -SaveRoot $fixture.saveRoot `
            -StateRoot $fixture.stateRoot `
            -RequalifyWorking `
            -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 `
            -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
            -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath `
            -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
            -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 `
            -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
            -Confirm:$false 6>&1)
        Assert-Test (($output -join "`n") -like '*Working fixture requalification PASS*') 'requalification did not report PASS'
        $newQualification = Read-KmcJson $fixture.qualificationPath
        $allowedChanges = @('initialWorkingSha256','initialWorkingLength','initialWorkingLastWriteTimeUtcTicks','qualifiedAtUtc')
        foreach ($property in @($oldQualification.PSObject.Properties.Name)) {
            if ($property -notin $allowedChanges) {
                Assert-Test ((($oldQualification.$property | ConvertTo-Json -Depth 5 -Compress) -ceq
                    ($newQualification.$property | ConvertTo-Json -Depth 5 -Compress))) "protected qualification field changed: $property"
            }
        }
        Assert-Test ([string]$newQualification.initialWorkingSha256 -ceq [string]$fixture.revisedPair.working.sha256) 'revised Working SHA was not recorded'
        Assert-Test ([long]$newQualification.initialWorkingLength -eq [long]$fixture.revisedPair.working.length) 'revised Working length was not recorded'
        Assert-Test ([long]$newQualification.initialWorkingLastWriteTimeUtcTicks -eq [long]$fixture.revisedPair.working.lastWriteTimeUtcTicks) 'revised Working timestamp was not recorded'
        Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -cne $fixture.oldQualificationSha256) 'successful requalification did not replace qualification bytes'
        $validated = Assert-KmcFixturePair -SaveRoot $fixture.saveRoot -QualificationPath $fixture.qualificationPath
        Assert-Test ([string]$validated.working.sha256 -ceq $fixture.revisedWorkingSha256) 'normal guard did not revalidate revised Working'
        $baselineAfter = Read-KmcFixtureHeader -Path $fixture.baselinePath -Kind baseline -SaveRoot $fixture.saveRoot
        Assert-Test ([string]$baselineAfter.sha256 -ceq [string]$baselineBefore.sha256 -and
            [long]$baselineAfter.length -eq [long]$baselineBefore.length -and
            [long]$baselineAfter.lastWriteTimeUtcTicks -eq [long]$baselineBefore.lastWriteTimeUtcTicks) 'successful Working requalification changed Baseline'
        Assert-Test ((Get-KmcSha256 (Join-Path $fixture.saveRoot 'Manual_3_PERSONAL.zks')) -ceq $foreignBefore) 'successful Working requalification changed a foreign save'
        Assert-KmcSaveMetadataInventoriesEqual -Before $savesBefore -After (Get-KmcSaveMetadataInventory $fixture.saveRoot) -Description 'successful synthetic requalification save metadata'
        $normalOutput = @(& $guardPath -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot 6>&1)
        Assert-Test (($normalOutput -join "`n") -like '*KMC fixture guard PASS*') 'standalone normal guard did not revalidate successful Working requalification'
        $currentQualificationSha256 = Get-KmcSha256 $fixture.qualificationPath
        $continuityOutput = @(& $guardPath `
            -SaveRoot $fixture.saveRoot `
            -StateRoot $fixture.stateRoot `
            -AuditWorkingContinuity `
            -ExpectedCurrentQualificationSha256 $currentQualificationSha256 `
            -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
            -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath `
            -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
            -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 `
            -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
            -WhatIf 6>&1)
        Assert-Test (($continuityOutput -join "`n") -like '*Working fixture continuity audit WhatIf PASS*') 'standalone committed-fixture continuity audit did not report pure PASS'
        Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $currentQualificationSha256) 'continuity audit changed committed qualification bytes'
        Assert-Test (-not (Test-Path -LiteralPath (Join-Path $fixture.stateRoot 'active-transaction.lock'))) 'successful Working requalification left a runtime lock'
        $requalificationStates = @(Get-ChildItem -LiteralPath (Join-Path $fixture.stateRoot 'fixture-requalifications') -Filter '*.json' |
            Where-Object { $_.Name -notlike '*.prior.json' })
        Assert-Test ($requalificationStates.Count -eq 1) 'successful Working requalification did not retain exactly one durable state record'
        $requalificationState = Read-KmcJson $requalificationStates[0].FullName
        Assert-Test ([string]$requalificationState.phase -ceq 'committed') 'successful Working requalification state is not committed'
        Assert-Test ((Get-KmcSha256 ([string]$requalificationState.priorQualificationBackupPath)) -ceq $fixture.oldQualificationSha256) 'successful Working requalification did not retain exact prior qualification bytes'
        $committedStateHash = Get-KmcSha256 $requalificationStates[0].FullName
        $committedReplay = Invoke-KmcWorkingFixtureRequalificationRecovery `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath `
            -RunId ([string]$requalificationState.runId) -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 `
            -ExpectedBaselineSha256 $fixture.baselineSha256 -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256
        Assert-Test ([string]$committedReplay.disposition -ceq 'already-committed') 'committed no-lock recovery was not read-only and idempotent'
        Assert-Test ((Get-KmcSha256 $requalificationStates[0].FullName) -ceq $committedStateHash -and
            -not (Test-Path -LiteralPath (Join-Path $fixture.stateRoot 'active-transaction.lock'))) 'committed no-lock replay mutated state or created a lock'

        $continuityArguments = @{
            SaveRoot = $fixture.saveRoot
            StateRoot = $fixture.stateRoot
            QualificationPath = $fixture.qualificationPath
            ExpectedCurrentQualificationSha256 = $currentQualificationSha256
            ExpectedSupersededWorkingSha256 = $fixture.supersededWorkingSha256
            PriorSaveTransactionStatePath = $fixture.priorSaveTransactionStatePath
            ExpectedPriorSaveTransactionRunId = $fixture.priorSaveTransactionRunId
            ExpectedPriorSaveTransactionStateSha256 = $fixture.priorSaveTransactionStateSha256
            ExpectedPriorSaveMetadataDigest = $fixture.priorSaveMetadataDigest
        }
        $wrong = '0' * 64
        foreach ($mutation in @(
            @{ name='current qualification SHA'; key='ExpectedCurrentQualificationSha256'; value=$wrong },
            @{ name='superseded Working SHA'; key='ExpectedSupersededWorkingSha256'; value=$wrong },
            @{ name='prior state path'; key='PriorSaveTransactionStatePath'; value=$fixture.qualificationPath },
            @{ name='prior run ID'; key='ExpectedPriorSaveTransactionRunId'; value='wrong-prior-run' },
            @{ name='prior state SHA'; key='ExpectedPriorSaveTransactionStateSha256'; value=$wrong },
            @{ name='prior inventory digest'; key='ExpectedPriorSaveMetadataDigest'; value=$wrong }
        )) {
            $arguments = @{}
            foreach ($key in $continuityArguments.Keys) { $arguments[$key] = $continuityArguments[$key] }
            $arguments[[string]$mutation.key] = [string]$mutation.value
            $threw = $false
            try { Assert-KmcQualifiedWorkingPriorInventoryContinuity @arguments | Out-Null } catch { $threw = $true }
            Assert-Test $threw "runtime continuity accepted wrong $($mutation.name) pin"
        }
        [IO.File]::WriteAllText((Join-Path $fixture.saveRoot 'Auto_1.zks'), 'unauthorized-auto-after-qualification')
        [IO.File]::WriteAllText((Join-Path $fixture.saveRoot 'Quick_1.zks'), 'unauthorized-quick-after-qualification')
        $driftMessage = $null
        try { Assert-KmcQualifiedWorkingPriorInventoryContinuity @continuityArguments | Out-Null }
        catch { $driftMessage = $_.Exception.Message }
        Assert-Test ($driftMessage -like '*Save write allowlist violation: Auto_1.zks, Quick_1.zks*') `
            'runtime continuity admitted or failed to attribute Auto/Quick drift after committed requalification'
    }

    Invoke-HarnessTest 'Working requalification rejects foreign save drift through the standalone entry point' {
        $fixture = New-TestPendingWorkingRequalification 'foreign-prior-drift'
        $guardPath = Join-Path $repoRoot 'scripts\runtime\Test-KmcFixtureGuard.ps1'
        [IO.File]::WriteAllText((Join-Path $fixture.saveRoot 'Auto_1.zks'), 'unauthorized-auto-drift')
        [IO.File]::WriteAllText((Join-Path $fixture.saveRoot 'Quick_1.zks'), 'unauthorized-quick-drift')
        $qualificationBefore = Get-Item -LiteralPath $fixture.qualificationPath -Force
        $qualificationSha256Before = Get-KmcSha256 $fixture.qualificationPath
        $stateManifestBefore = Get-KmcDirectoryManifest $fixture.stateRoot
        $message = $null
        try {
            & $guardPath `
                -SaveRoot $fixture.saveRoot `
                -StateRoot $fixture.stateRoot `
                -RequalifyWorking `
                -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 `
                -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
                -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath `
                -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 `
                -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
                -Confirm:$false | Out-Null
        }
        catch { $message = $_.Exception.Message }
        $qualificationAfter = Get-Item -LiteralPath $fixture.qualificationPath -Force
        Assert-Test ($message -like '*Save write allowlist violation: Auto_1.zks, Quick_1.zks*') 'standalone requalification did not attribute exact Auto/Quick drift'
        Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $qualificationSha256Before -and
            $qualificationAfter.Length -eq $qualificationBefore.Length -and
            $qualificationAfter.LastWriteTimeUtc.Ticks -eq $qualificationBefore.LastWriteTimeUtc.Ticks) 'foreign-drift rejection changed qualification bytes or metadata'
        Assert-Test ((Get-KmcDirectoryManifest $fixture.stateRoot).digest -ceq $stateManifestBefore.digest -and
            -not (Test-Path -LiteralPath (Join-Path $fixture.stateRoot 'active-transaction.lock'))) 'foreign-drift rejection changed runtime state or created a lock'
    }

    Invoke-HarnessTest 'Prior save-inventory authority rejects wrong identity, nonterminal state, and malformed UTF-8' {
        $fixture = New-TestPendingWorkingRequalification 'prior-authority-mutations'
        $authorityArguments = @{
            Path = $fixture.priorSaveTransactionStatePath
            StateRoot = $fixture.stateRoot
            SaveRoot = $fixture.saveRoot
            ExpectedRunId = $fixture.priorSaveTransactionRunId
            ExpectedStateSha256 = $fixture.priorSaveTransactionStateSha256
            ExpectedInventoryDigest = $fixture.priorSaveMetadataDigest
            ExpectedBaselineSha256 = $fixture.baselineSha256
            ExpectedSupersededWorkingSha256 = $fixture.supersededWorkingSha256
            CurrentPair = $fixture.revisedPair
        }
        $wrong = '0' * 64
        foreach ($mutation in @(
            @{ name='path'; key='Path'; value=$fixture.qualificationPath },
            @{ name='run ID'; key='ExpectedRunId'; value='wrong-authority-run' },
            @{ name='state SHA'; key='ExpectedStateSha256'; value=$wrong },
            @{ name='inventory digest'; key='ExpectedInventoryDigest'; value=$wrong }
        )) {
            $arguments = @{}
            foreach ($key in $authorityArguments.Keys) { $arguments[$key] = $authorityArguments[$key] }
            $arguments[[string]$mutation.key] = [string]$mutation.value
            $threw = $false
            try { Read-KmcPriorSaveTransactionAuthority @arguments | Out-Null } catch { $threw = $true }
            Assert-Test $threw "prior save-inventory authority accepted wrong $($mutation.name)"
        }

        $terminalBytes = [IO.File]::ReadAllBytes($fixture.priorSaveTransactionStatePath)
        $terminalTimestamp = (Get-Item -LiteralPath $fixture.priorSaveTransactionStatePath -Force).LastWriteTimeUtc
        $nonterminal = Read-KmcJson $fixture.priorSaveTransactionStatePath
        $nonterminal.phase = 'prepared'
        Write-KmcJsonDurable -Path $fixture.priorSaveTransactionStatePath -Value $nonterminal
        $authorityArguments.ExpectedStateSha256 = Get-KmcSha256 $fixture.priorSaveTransactionStatePath
        $threw = $false
        try { Read-KmcPriorSaveTransactionAuthority @authorityArguments | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'prior save-inventory authority accepted a nonterminal transaction state'

        Write-KmcBytesDurableAtomic -Path $fixture.priorSaveTransactionStatePath -Bytes $terminalBytes
        [IO.File]::SetLastWriteTimeUtc($fixture.priorSaveTransactionStatePath, $terminalTimestamp)
        [IO.File]::WriteAllBytes($fixture.priorSaveTransactionStatePath, [byte[]](0x7b,0xff,0x7d))
        $authorityArguments.ExpectedStateSha256 = Get-KmcSha256 $fixture.priorSaveTransactionStatePath
        $message = $null
        try { Read-KmcPriorSaveTransactionAuthority @authorityArguments | Out-Null } catch { $message = $_.Exception.Message }
        Assert-Test ($message -like '*not strict UTF-8*') 'prior save-inventory authority did not reject malformed UTF-8 before JSON parsing'
    }

    Invoke-HarnessTest 'protected-save continuity authority WhatIf is pure and exact' {
        $epoch = New-TestAuthorizedProtectedSaveEpoch 'protected-authority-whatif'
        $fixture = $epoch.fixture
        $scriptPath = Join-Path $repoRoot 'scripts\runtime\New-KmcProtectedSaveContinuityAuthority.ps1'
        $stateBefore = Get-KmcDirectoryManifest $fixture.stateRoot
        $savesBefore = Get-KmcSaveMetadataInventory $fixture.saveRoot
        $output = @(& $scriptPath `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -EpochId $epoch.epochId `
            -ExpectedCurrentQualificationSha256 $epoch.qualificationSha256 `
            -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
            -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath `
            -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
            -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 `
            -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
            -AutoSaveName $epoch.autoName -ExpectedAutoSaveSha256 $epoch.autoSha256 `
            -ExpectedAutoSaveLength $epoch.autoLength -ExpectedAutoSaveLastWriteTimeUtcTicks $epoch.autoTicks `
            -QuickSaveName $epoch.quickName -ExpectedQuickSaveSha256 $epoch.quickSha256 `
            -ExpectedQuickSaveLength $epoch.quickLength -ExpectedQuickSaveLastWriteTimeUtcTicks $epoch.quickTicks `
            -WhatIf 6>&1)
        Assert-Test (($output -join "`n") -like '*Protected-save continuity authority WhatIf PASS*') 'protected-save authority WhatIf did not report PASS'
        Assert-KmcSaveMetadataInventoriesEqual -Before $savesBefore -After (Get-KmcSaveMetadataInventory $fixture.saveRoot) -Description 'protected-save authority WhatIf saves'
        Assert-Test ((Get-KmcDirectoryManifest $fixture.stateRoot).digest -ceq $stateBefore.digest) 'protected-save authority WhatIf changed runtime state'
        Assert-Test (-not (Test-Path -LiteralPath (Join-Path $fixture.stateRoot 'protected-save-authorities'))) 'protected-save authority WhatIf created its authority root'
    }

    Invoke-HarnessTest 'protected-save continuity authority binds exact protected bytes and every other entry' {
        $epoch = New-TestAuthorizedProtectedSaveEpoch 'protected-authority-commit'
        $fixture = $epoch.fixture
        $scriptPath = Join-Path $repoRoot 'scripts\runtime\New-KmcProtectedSaveContinuityAuthority.ps1'
        & $scriptPath `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -EpochId $epoch.epochId `
            -ExpectedCurrentQualificationSha256 $epoch.qualificationSha256 `
            -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
            -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath `
            -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
            -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 `
            -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
            -AutoSaveName $epoch.autoName -ExpectedAutoSaveSha256 $epoch.autoSha256 `
            -ExpectedAutoSaveLength $epoch.autoLength -ExpectedAutoSaveLastWriteTimeUtcTicks $epoch.autoTicks `
            -QuickSaveName $epoch.quickName -ExpectedQuickSaveSha256 $epoch.quickSha256 `
            -ExpectedQuickSaveLength $epoch.quickLength -ExpectedQuickSaveLastWriteTimeUtcTicks $epoch.quickTicks `
            -Confirm:$false | Out-Null
        $authorityPath = Join-Path (Join-Path $fixture.stateRoot 'protected-save-authorities') ($epoch.epochId + '.json')
        $authoritySha256 = Get-KmcSha256 $authorityPath
        $continuityArguments = @{
            SaveRoot=$fixture.saveRoot;StateRoot=$fixture.stateRoot;QualificationPath=$fixture.qualificationPath
            ExpectedCurrentQualificationSha256=$epoch.qualificationSha256
            ExpectedSupersededWorkingSha256=$fixture.supersededWorkingSha256
            PriorSaveTransactionStatePath=$fixture.priorSaveTransactionStatePath
            ExpectedPriorSaveTransactionRunId=$fixture.priorSaveTransactionRunId
            ExpectedPriorSaveTransactionStateSha256=$fixture.priorSaveTransactionStateSha256
            ExpectedPriorSaveMetadataDigest=$fixture.priorSaveMetadataDigest
            ProtectedSaveContinuityAuthorityPath=$authorityPath
            ExpectedProtectedSaveContinuityEpochId=$epoch.epochId
            ExpectedProtectedSaveContinuityAuthoritySha256=$authoritySha256
            ExpectedProtectedAutoSaveName=$epoch.autoName;ExpectedProtectedAutoSaveSha256=$epoch.autoSha256
            ExpectedProtectedQuickSaveName=$epoch.quickName;ExpectedProtectedQuickSaveSha256=$epoch.quickSha256
        }
        $validated = Assert-KmcQualifiedWorkingProtectedSaveContinuity @continuityArguments
        Assert-Test ([string]$validated.sha256 -ceq $authoritySha256) 'protected-save authority validation returned the wrong authority SHA-256'
        $wrongHashArguments = @{}
        foreach ($key in $continuityArguments.Keys) { $wrongHashArguments[$key] = $continuityArguments[$key] }
        $wrongHashArguments.ExpectedProtectedAutoSaveSha256 = '0' * 64
        $threw = $false
        try { Assert-KmcQualifiedWorkingProtectedSaveContinuity @wrongHashArguments | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'protected-save authority accepted a wrong caller-pinned autosave hash'
        $foreignPath = Join-Path $fixture.saveRoot 'Manual_3_PERSONAL.zks'
        $foreignTicks = (Get-Item -LiteralPath $foreignPath -Force).LastWriteTimeUtc
        [IO.File]::SetLastWriteTimeUtc($foreignPath, $foreignTicks.AddTicks(1))
        $threw = $false
        try { Assert-KmcQualifiedWorkingProtectedSaveContinuity @continuityArguments | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'protected-save authority accepted subsequent metadata drift in another protected save'
    }

    Invoke-HarnessTest 'Working requalification rejects every incorrect explicit pin' {
        $fixture = New-TestPendingWorkingRequalification 'wrong-pins'
        $wrong = '0' * 64
        $attempts = @(
            @{ qualification=$wrong; baseline=$fixture.baselineSha256; old=$fixture.supersededWorkingSha256; revised=$fixture.revisedWorkingSha256 },
            @{ qualification=$fixture.oldQualificationSha256; baseline=$wrong; old=$fixture.supersededWorkingSha256; revised=$fixture.revisedWorkingSha256 },
            @{ qualification=$fixture.oldQualificationSha256; baseline=$fixture.baselineSha256; old=$wrong; revised=$fixture.revisedWorkingSha256 },
            @{ qualification=$fixture.oldQualificationSha256; baseline=$fixture.baselineSha256; old=$fixture.supersededWorkingSha256; revised=$wrong }
        )
        foreach ($attempt in $attempts) {
            $threw = $false
            try {
                New-KmcWorkingFixtureRequalification `
                    -Pair $fixture.revisedPair `
                    -QualificationPath $fixture.qualificationPath `
                    -ExpectedExistingQualificationSha256 $attempt.qualification `
                    -ExpectedBaselineSha256 $attempt.baseline `
                    -ExpectedSupersededWorkingSha256 $attempt.old `
                    -ExpectedRevisedWorkingSha256 $attempt.revised | Out-Null
            }
            catch { $threw = $true }
            Assert-Test $threw 'Working requalification accepted an incorrect explicit pin'
            Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $fixture.oldQualificationSha256) 'incorrect requalification pin changed the durable qualification'
        }
    }

    Invoke-HarnessTest 'Working requalification rejects Baseline mutation and active transaction lock' {
        $guardPath = Join-Path $repoRoot 'scripts\runtime\Test-KmcFixtureGuard.ps1'
        $lockedFixture = New-TestPendingWorkingRequalification 'active-lock'
        $heldLock = Open-KmcRuntimeLock -StateRoot $lockedFixture.stateRoot -RunId 'held-requalification-test'
        try {
            $threw = $false
            try {
                & $guardPath `
                    -SaveRoot $lockedFixture.saveRoot `
                    -StateRoot $lockedFixture.stateRoot `
                    -RequalifyWorking `
                    -ExpectedExistingQualificationSha256 $lockedFixture.oldQualificationSha256 `
                    -ExpectedBaselineSha256 $lockedFixture.baselineSha256 `
                    -ExpectedSupersededWorkingSha256 $lockedFixture.supersededWorkingSha256 `
                    -ExpectedRevisedWorkingSha256 $lockedFixture.revisedWorkingSha256 `
                    -PriorSaveTransactionStatePath $lockedFixture.priorSaveTransactionStatePath `
                    -ExpectedPriorSaveTransactionRunId $lockedFixture.priorSaveTransactionRunId `
                    -ExpectedPriorSaveTransactionStateSha256 $lockedFixture.priorSaveTransactionStateSha256 `
                    -ExpectedPriorSaveMetadataDigest $lockedFixture.priorSaveMetadataDigest `
                    -Confirm:$false | Out-Null
            }
            catch { $threw = $true }
            Assert-Test $threw 'Working requalification accepted an actually held runtime transaction lock'
            [void](Assert-KmcRuntimeLockOwner $heldLock)
        }
        finally { Close-KmcRuntimeLock $heldLock }
        Assert-Test ((Get-KmcSha256 $lockedFixture.qualificationPath) -ceq $lockedFixture.oldQualificationSha256) 'lock rejection changed the durable qualification'

        $baselineFixture = New-TestPendingWorkingRequalification 'baseline-drift'
        New-TestSaveArchive -Path $baselineFixture.baselinePath -Name 'KMC_AUTOMATION_BASELINE' -ExtraEntry
        $threw = $false
        try {
            & $guardPath `
                -SaveRoot $baselineFixture.saveRoot `
                -StateRoot $baselineFixture.stateRoot `
                -RequalifyWorking `
                -ExpectedExistingQualificationSha256 $baselineFixture.oldQualificationSha256 `
                -ExpectedBaselineSha256 $baselineFixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $baselineFixture.supersededWorkingSha256 `
                -ExpectedRevisedWorkingSha256 $baselineFixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $baselineFixture.priorSaveTransactionStatePath `
                -ExpectedPriorSaveTransactionRunId $baselineFixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $baselineFixture.priorSaveTransactionStateSha256 `
                -ExpectedPriorSaveMetadataDigest $baselineFixture.priorSaveMetadataDigest `
                -Confirm:$false | Out-Null
        }
        catch { $threw = $true }
        Assert-Test $threw 'Working requalification admitted a mutated Baseline'
        Assert-Test ((Get-KmcSha256 $baselineFixture.qualificationPath) -ceq $baselineFixture.oldQualificationSha256) 'Baseline rejection changed the durable qualification'
    }

    Invoke-HarnessTest 'Working requalification rolls back an exact post-write failure under its held lock' {
        $fixture = New-TestPendingWorkingRequalification 'post-write-rollback'
        $qualificationBefore = Get-Item -LiteralPath $fixture.qualificationPath -Force
        $qualificationBytesBefore = [IO.File]::ReadAllBytes($fixture.qualificationPath)
        $saveMetadataBefore = Get-KmcSaveMetadataInventory $fixture.saveRoot
        $script:requalificationLockObserved = $false
        $script:concurrentRequalificationLockRejected = $false
        $postWriteProbe = {
            param($heldLock, $statePath)
            [void](Assert-KmcRuntimeLockOwner $heldLock)
            $script:requalificationLockObserved = Test-Path -LiteralPath $heldLock.Path -PathType Leaf
            try { Open-KmcRuntimeLock -StateRoot $fixture.stateRoot -RunId 'concurrent-requalification-test' | Out-Null }
            catch { $script:concurrentRequalificationLockRejected = $true }
            throw 'forced post-write validation failure'
        }
        $message = $null
        try {
            Invoke-KmcWorkingFixtureRequalificationTransaction `
                -SaveRoot $fixture.saveRoot `
                -StateRoot $fixture.stateRoot `
                -QualificationPath $fixture.qualificationPath `
                -RunId 'post-write-rollback-test' `
                -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 `
                -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
                -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath `
                -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 `
                -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
                -PostWriteProbe $postWriteProbe | Out-Null
        }
        catch { $message = $_.Exception.Message }
        Assert-Test ($message -like '*prior qualification was restored exactly*forced post-write validation failure*') 'forced post-write failure did not report exact rollback'
        Assert-Test ($script:requalificationLockObserved -and $script:concurrentRequalificationLockRejected) 'post-write validation was not covered by an exclusive held lock'
        $qualificationAfter = Get-Item -LiteralPath $fixture.qualificationPath -Force
        Assert-Test ([Linq.Enumerable]::SequenceEqual([byte[]]$qualificationBytesBefore, [byte[]][IO.File]::ReadAllBytes($fixture.qualificationPath))) 'rollback did not restore exact prior qualification bytes'
        Assert-Test ($qualificationAfter.Length -eq $qualificationBefore.Length -and
            $qualificationAfter.LastWriteTimeUtc.Ticks -eq $qualificationBefore.LastWriteTimeUtc.Ticks) 'rollback did not restore exact prior qualification metadata'
        Assert-KmcSaveMetadataInventoriesEqual -Before $saveMetadataBefore -After (Get-KmcSaveMetadataInventory $fixture.saveRoot) -Description 'post-write rollback save metadata'
        $normalAccepted = $true
        try { Assert-KmcFixturePair -SaveRoot $fixture.saveRoot -QualificationPath $fixture.qualificationPath | Out-Null } catch { $normalAccepted = $false }
        Assert-Test (-not $normalAccepted) 'rolled-back prior qualification admitted revised Working'
        Assert-Test (-not (Test-Path -LiteralPath (Join-Path $fixture.stateRoot 'active-transaction.lock'))) 'successful rollback left a runtime lock'
        $state = Read-KmcJson (Join-Path $fixture.stateRoot 'fixture-requalifications\post-write-rollback-test.json')
        Assert-Test ([string]$state.phase -ceq 'rolled-back') 'post-write failure state was not durably marked rolled-back'
    }

    Invoke-HarnessTest 'Working requalification records exact pre-marker write-attempt failure phases' {
        foreach ($rollbackFails in @($false,$true)) {
            $suffix = if ($rollbackFails) { 'rollback-fails' } else { 'rollback-succeeds' }
            $fixture = New-TestPendingWorkingRequalification ('pre-marker-' + $suffix)
            $runId = 'pre-marker-' + $suffix
            $arguments = @{
                SaveRoot=$fixture.saveRoot;StateRoot=$fixture.stateRoot;QualificationPath=$fixture.qualificationPath;RunId=$runId
                ExpectedExistingQualificationSha256=$fixture.oldQualificationSha256;ExpectedBaselineSha256=$fixture.baselineSha256
                ExpectedSupersededWorkingSha256=$fixture.supersededWorkingSha256;ExpectedRevisedWorkingSha256=$fixture.revisedWorkingSha256
                PriorSaveTransactionStatePath=$fixture.priorSaveTransactionStatePath;ExpectedPriorSaveTransactionRunId=$fixture.priorSaveTransactionRunId
                ExpectedPriorSaveTransactionStateSha256=$fixture.priorSaveTransactionStateSha256;ExpectedPriorSaveMetadataDigest=$fixture.priorSaveMetadataDigest
                AfterReplacementWriteBeforeStateProbe={ throw 'forced replacement write pre-marker failure' }
            }
            if ($rollbackFails) { $arguments['BeforeRollbackProbe'] = { throw 'forced pre-marker rollback failure' } }
            $message = $null
            try { Invoke-KmcWorkingFixtureRequalificationTransaction @arguments | Out-Null }
            catch { $message = $_.Exception.Message }
            Assert-Test ($message -like '*forced replacement write pre-marker failure*') 'pre-marker replacement failure was not surfaced'
            $statePath = Join-Path $fixture.stateRoot ('fixture-requalifications\' + $runId + '.json')
            $state = Read-KmcJson $statePath
            [void](Assert-KmcWorkingFixtureRequalificationStateSchema $state)
            $expectedPhase = if ($rollbackFails) { 'replacement-write-attempt-rollback-failed' } else { 'replacement-write-attempt-rolled-back' }
            Assert-Test ([string]$state.phase -ceq $expectedPhase) "pre-marker producer did not emit exact phase $expectedPhase"
            $lockPath = Join-Path $fixture.stateRoot 'active-transaction.lock'
            if ($rollbackFails) {
                Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $fixture.revisedWorkingSha256 -or
                    (Get-KmcSha256 $fixture.qualificationPath) -cne $fixture.oldQualificationSha256) 'pre-marker rollback failure unexpectedly retained only the prior qualification'
                Set-TestRuntimeLockOwnerDead $lockPath
                $recovered = Invoke-KmcWorkingFixtureRequalificationRecovery `
                    -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                    -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                    -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256
                Assert-Test ([string]$recovered.disposition -ceq 'prior-restored') 'pre-marker rollback-failed phase was not recoverable'
            }
            else {
                Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $fixture.oldQualificationSha256 -and
                    -not (Test-Path -LiteralPath $lockPath)) 'pre-marker rollback-success phase did not restore and release exactly'
            }
        }
    }

    Invoke-HarnessTest 'Working requalification normalizes a late committed-state failure before rollback' {
        $fixture = New-TestPendingWorkingRequalification 'late-commit-failure'
        $runId = 'late-commit-failure-test'
        $message = $null
        try {
            Invoke-KmcWorkingFixtureRequalificationTransaction `
                -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
                -AfterCommittedStateProbe { throw 'forced late committed-state failure' } | Out-Null
        }
        catch { $message = $_.Exception.Message }
        Assert-Test ($message -like '*prior qualification was restored exactly*forced late committed-state failure*') 'late committed-state failure was not rolled back exactly'
        $statePath = Join-Path $fixture.stateRoot ('fixture-requalifications\' + $runId + '.json')
        $state = Read-KmcJson $statePath
        [void](Assert-KmcWorkingFixtureRequalificationStateSchema $state)
        Assert-Test ([string]$state.phase -ceq 'rolled-back' -and
            $null -eq $state.PSObject.Properties['committedAtUtc']) 'late committed-state failure retained illegal committed fields in rolled-back state'
        Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $fixture.oldQualificationSha256 -and
            -not (Test-Path -LiteralPath (Join-Path $fixture.stateRoot 'active-transaction.lock'))) 'late committed-state rollback did not restore qualification and release lock'
    }

    Invoke-HarnessTest 'Working requalification rollback failure retains its runtime lock' {
        $fixture = New-TestPendingWorkingRequalification 'rollback-failure'
        $message = $null
        try {
            Invoke-KmcWorkingFixtureRequalificationTransaction `
                -SaveRoot $fixture.saveRoot `
                -StateRoot $fixture.stateRoot `
                -QualificationPath $fixture.qualificationPath `
                -RunId 'rollback-failure-test' `
                -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 `
                -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
                -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath `
                -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 `
                -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
                -PostWriteProbe { throw 'forced primary failure' } `
                -BeforeRollbackProbe { throw 'forced rollback failure' } | Out-Null
        }
        catch { $message = $_.Exception.Message }
        $retainedLockPath = Join-Path $fixture.stateRoot 'active-transaction.lock'
        Assert-Test ($message -like '*forced primary failure*forced rollback failure*active runtime lock was retained*') 'rollback failure did not surface both failures and lock retention'
        Assert-Test (Test-Path -LiteralPath $retainedLockPath -PathType Leaf) 'rollback failure did not retain its runtime lock'
        $rollbackStatePath = Join-Path $fixture.stateRoot 'fixture-requalifications\rollback-failure-test.json'
        $state = Read-KmcJson $rollbackStatePath
        Assert-Test ([string]$state.phase -ceq 'rollback-failed') 'rollback failure state was not durably marked rollback-failed'
        Set-TestRuntimeLockOwnerDead $retainedLockPath
        $qualificationBak = Join-Path $fixture.stateRoot ('.fixture-qualification.json.' + [Guid]::NewGuid().ToString('N') + '.bak')
        $stateBak = Join-Path (Split-Path -Parent $rollbackStatePath) ('.rollback-failure-test.json.' + [Guid]::NewGuid().ToString('N') + '.bak')
        Copy-Item -LiteralPath $fixture.qualificationPath -Destination $qualificationBak
        Copy-Item -LiteralPath $rollbackStatePath -Destination $stateBak
        $debrisPlan = Get-KmcWorkingFixtureRequalificationRecoveryPlan `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath `
            -RunId 'rollback-failure-test' -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 `
            -ExpectedBaselineSha256 $fixture.baselineSha256 -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256
        Assert-Test (@($debrisPlan.atomicDebris).Count -eq 2) 'bounded legacy atomic backups were not classified before recovery'
        $recovered = Invoke-KmcWorkingFixtureRequalificationRecovery `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath `
            -RunId 'rollback-failure-test' -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 `
            -ExpectedBaselineSha256 $fixture.baselineSha256 -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256
        Assert-Test ([string]$recovered.disposition -ceq 'prior-restored') 'rollback-failed recovery did not restore the prior qualification'
        Assert-Test (-not (Test-Path -LiteralPath $retainedLockPath) -and
            -not (Test-Path -LiteralPath $qualificationBak) -and -not (Test-Path -LiteralPath $stateBak)) 'rollback-failed recovery did not reconcile owned debris and clear its adopted lock'
        $recoveredState = Read-KmcJson $rollbackStatePath
        Assert-Test ([string]$recoveredState.phase -ceq 'recovered-rolled-back') 'rollback-failed recovery did not write an exact terminal state'
    }

    Invoke-HarnessTest 'Working requalification does not roll back after lock authority is lost' {
        $fixture = New-TestPendingWorkingRequalification 'lost-lock-authority'
        $message = $null
        try {
            Invoke-KmcWorkingFixtureRequalificationTransaction `
                -SaveRoot $fixture.saveRoot `
                -StateRoot $fixture.stateRoot `
                -QualificationPath $fixture.qualificationPath `
                -RunId 'lost-lock-authority-test' `
                -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 `
                -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
                -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath `
                -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 `
                -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
                -PostWriteProbe {
                    param($heldLock, $statePath)
                    $heldLock.Stream.Dispose()
                    throw 'forced lost-lock primary failure'
                } | Out-Null
        }
        catch { $message = $_.Exception.Message }
        $retainedLockPath = Join-Path $fixture.stateRoot 'active-transaction.lock'
        Assert-Test ($message -like '*forced lost-lock primary failure*runtime lock handle is not readable*retained*') 'lost lock authority did not surface primary, rollback, and retention errors'
        Assert-Test (Test-Path -LiteralPath $retainedLockPath -PathType Leaf) 'lost lock authority did not retain the runtime sentinel'
        $validated = Assert-KmcFixturePair -SaveRoot $fixture.saveRoot -QualificationPath $fixture.qualificationPath
        Assert-Test ([string]$validated.working.sha256 -ceq $fixture.revisedWorkingSha256) 'qualification was mutated during ambiguous lost-lock rollback'
        $state = Read-KmcJson (Join-Path $fixture.stateRoot 'fixture-requalifications\lost-lock-authority-test.json')
        Assert-Test ([string]$state.phase -ceq 'replacement-written') 'lost-lock rollback regressed the last authority-proven state'
        Set-TestRuntimeLockOwnerDead $retainedLockPath
        $recovered = Invoke-KmcWorkingFixtureRequalificationRecovery `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath `
            -RunId 'lost-lock-authority-test' -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 `
            -ExpectedBaselineSha256 $fixture.baselineSha256 -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256
        Assert-Test ([string]$recovered.disposition -ceq 'prior-restored') 'lost-lock rollback-failed state was not safely recovered'
    }

    Invoke-HarnessTest 'Working requalification rechecks authority after the rollback probe' {
        $fixture = New-TestPendingWorkingRequalification 'rollback-probe-authority'
        $runId = 'rollback-probe-authority-test'
        $message = $null
        try {
            Invoke-KmcWorkingFixtureRequalificationTransaction `
                -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
                -PostWriteProbe { throw 'force rollback authority probe' } `
                -BeforeRollbackProbe { param($heldLock,$ignored); $heldLock.Stream.Dispose() } | Out-Null
        }
        catch { $message = $_.Exception.Message }
        $lockPath = Join-Path $fixture.stateRoot 'active-transaction.lock'
        Assert-Test ($message -like '*force rollback authority probe*runtime lock handle is not readable*retained*') 'lost authority after rollback probe did not fail closed'
        Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -cne $fixture.oldQualificationSha256 -and
            (Test-Path -LiteralPath $lockPath -PathType Leaf)) 'rollback wrote the prior qualification after its probe lost authority'
        $statePath = Join-Path $fixture.stateRoot ('fixture-requalifications\' + $runId + '.json')
        $state = Read-KmcJson $statePath
        [void](Assert-KmcWorkingFixtureRequalificationStateSchema $state)
        Assert-Test ([string]$state.phase -ceq 'replacement-written') 'rollback-probe authority failure regressed the last authority-proven state'
        Set-TestRuntimeLockOwnerDead $lockPath
        $recovered = Invoke-KmcWorkingFixtureRequalificationRecovery `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
            -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256
        Assert-Test ([string]$recovered.disposition -ceq 'prior-restored') 'rollback-probe authority failure was not safely recoverable'
    }

    Invoke-HarnessTest 'Working requalification preserves prepared state when pre-write authority is lost' {
        $fixture = New-TestPendingWorkingRequalification 'prewrite-authority'
        $runId = 'prewrite-authority-test'
        $message = $null
        try {
            Invoke-KmcWorkingFixtureRequalificationTransaction `
                -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
                -BeforeReplacementProbe { param($heldLock,$ignored); $heldLock.Stream.Dispose(); throw 'forced prewrite authority loss' } | Out-Null
        }
        catch { $message = $_.Exception.Message }
        $lockPath = Join-Path $fixture.stateRoot 'active-transaction.lock'
        $statePath = Join-Path $fixture.stateRoot ('fixture-requalifications\' + $runId + '.json')
        Assert-Test ($message -like '*forced prewrite authority loss*runtime lock handle is not readable*') 'prewrite authority loss did not surface both errors'
        Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $fixture.oldQualificationSha256 -and
            (Test-Path -LiteralPath $lockPath -PathType Leaf)) 'prewrite authority loss changed qualification or removed its fail-closed lock'
        $state = Read-KmcJson $statePath
        [void](Assert-KmcWorkingFixtureRequalificationStateSchema $state)
        Assert-Test ([string]$state.phase -ceq 'prepared') 'prewrite authority loss regressed the last authority-proven state'
        Set-TestRuntimeLockOwnerDead $lockPath
        $recovered = Invoke-KmcWorkingFixtureRequalificationRecovery `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
            -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256
        Assert-Test ([string]$recovered.disposition -ceq 'prior-restored') 'prewrite authority-loss state was not safely recoverable'
    }

    Invoke-HarnessTest 'Working requalification rejects overlapping save and state roots' {
        $root = Join-Path $testRoot 'requalification-root-overlap'
        $child = Join-Path $root 'child'
        New-Item -ItemType Directory -Path $child -Force | Out-Null
        foreach ($pair in @(
            @($root, $root),
            @($root, $child),
            @($child, $root)
        )) {
            $threw = $false
            try { Assert-KmcPathsDoNotOverlap -First $pair[0] -Second $pair[1] -Description 'synthetic roots' } catch { $threw = $true }
            Assert-Test $threw 'Working requalification accepted equal or nested save/state roots'
        }

        $fixture = New-TestPendingWorkingRequalification 'overlap-helper'
        $nestedState = Join-Path $fixture.saveRoot 'nested-state'
        New-Item -ItemType Directory -Path $nestedState | Out-Null
        $nestedQualification = Join-Path $nestedState 'fixture-qualification.json'
        [IO.File]::WriteAllBytes($nestedQualification, [IO.File]::ReadAllBytes($fixture.qualificationPath))
        $nestedQualificationHash = Get-KmcSha256 $nestedQualification
        $threw = $false
        try {
            Invoke-KmcWorkingFixtureRequalificationTransaction `
                -SaveRoot $fixture.saveRoot -StateRoot $nestedState -QualificationPath $nestedQualification -RunId 'overlap-helper-test' `
                -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest | Out-Null
        }
        catch { $threw = $true }
        Assert-Test $threw 'transaction helper accepted overlapping save and state roots'
        Assert-Test (-not (Test-Path -LiteralPath (Join-Path $nestedState 'active-transaction.lock'))) 'overlap rejection created a runtime lock'
        Assert-Test ((Get-KmcSha256 $nestedQualification) -ceq $nestedQualificationHash) 'overlap rejection changed qualification bytes'

        $alternateQualification = Join-Path $fixture.stateRoot 'alternate-qualification.json'
        [IO.File]::WriteAllBytes($alternateQualification, [IO.File]::ReadAllBytes($fixture.qualificationPath))
        $threw = $false
        try {
            Invoke-KmcWorkingFixtureRequalificationTransaction `
                -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $alternateQualification -RunId 'alternate-path-test' `
                -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest | Out-Null
        }
        catch { $threw = $true }
        Assert-Test $threw 'transaction helper accepted a noncanonical qualification path'
        Assert-Test (-not (Test-Path -LiteralPath (Join-Path $fixture.stateRoot 'active-transaction.lock'))) 'qualification-path rejection created a runtime lock'
    }

    Invoke-HarnessTest 'reparse guard rejects a regular path beneath a junction ancestor' {
        $realParent = Join-Path $testRoot 'requalification-ancestor-real'
        $realState = Join-Path $realParent 'state'
        $junctionParent = Join-Path $testRoot 'requalification-ancestor-junction'
        New-Item -ItemType Directory -Path $realState -Force | Out-Null
        New-Item -ItemType Junction -Path $junctionParent -Target $realParent | Out-Null
        $aliasedState = Join-Path $junctionParent 'state'
        Assert-Test (Test-Path -LiteralPath $aliasedState -PathType Container) 'ancestor-junction fixture did not resolve to its regular child'
        $message = $null
        try { Assert-KmcNotReparsePoint $aliasedState 'synthetic aliased state root' } catch { $message = $_.Exception.Message }
        Assert-Test ($message -like '*resolves through a reparse point*requalification-ancestor-junction*') 'regular state path beneath a junction ancestor passed containment guard'
    }

    Invoke-HarnessTest 'Working requalification recovery clears a state-less crash without mutation' {
        $fixture = New-TestPendingWorkingRequalification 'recover-no-state'
        $runId = 'recover-no-state-test'
        $lock = Open-KmcRuntimeLock -StateRoot $fixture.stateRoot -RunId $runId -Purpose 'fixture-requalification'
        Abandon-KmcRuntimeLock $lock
        $lockPath = Join-Path $fixture.stateRoot 'active-transaction.lock'
        Set-TestRuntimeLockOwnerDead $lockPath
        $transactionRoot = Join-Path $fixture.stateRoot 'fixture-requalifications'
        New-Item -ItemType Directory -Path $transactionRoot | Out-Null
        [IO.File]::WriteAllBytes((Join-Path $transactionRoot ($runId + '.prior.json')), [IO.File]::ReadAllBytes($fixture.qualificationPath))
        $qualificationBefore = Get-Item -LiteralPath $fixture.qualificationPath -Force
        $saveBefore = Get-KmcSaveMetadataInventory $fixture.saveRoot
        $lockHashBefore = Get-KmcSha256 $lockPath
        $guardPath = Join-Path $repoRoot 'scripts\runtime\Test-KmcFixtureGuard.ps1'
        $whatIfOutput = @(& $guardPath `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -RecoverWorkingRequalification -RequalificationRunId $runId `
            -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
            -WhatIf 6>&1)
        Assert-Test (($whatIfOutput -join "`n") -like '*recovery WhatIf PASS*clear-prepared-lock*') 'state-less recovery WhatIf did not report its exact pure action'
        Assert-Test ((Get-KmcSha256 $lockPath) -ceq $lockHashBefore) 'recovery WhatIf changed the stale lock'
        $result = @(& $guardPath `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -RecoverWorkingRequalification -RequalificationRunId $runId `
            -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
            -Confirm:$false 6>&1)
        Assert-Test (($result -join "`n") -like '*recovery PASS: prior-restored*') 'state-less recovery did not pass with prior qualification retained'
        Assert-Test (-not (Test-Path -LiteralPath $lockPath)) 'state-less recovery did not clear its stale lock'
        $qualificationAfter = Get-Item -LiteralPath $fixture.qualificationPath -Force
        Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $fixture.oldQualificationSha256 -and
            $qualificationAfter.LastWriteTimeUtc.Ticks -eq $qualificationBefore.LastWriteTimeUtc.Ticks) 'state-less recovery changed prior qualification'
        Assert-KmcSaveMetadataInventoriesEqual -Before $saveBefore -After (Get-KmcSaveMetadataInventory $fixture.saveRoot) -Description 'state-less recovery saves'
        $statePath = Join-Path $transactionRoot ($runId + '.json')
        Assert-Test (Test-Path -LiteralPath $statePath -PathType Leaf) 'state-less recovery claimed no durable audit state'
        $terminal = Read-KmcJson $statePath
        [void](Assert-KmcWorkingFixtureRequalificationStateSchema $terminal)
        Assert-Test ([string]$terminal.phase -ceq 'recovered-rolled-back' -and
            [string]$terminal.recoveryAction -ceq 'prior-retained-state-less') 'state-less recovery audit state is not exact and truthful'
        $terminalHash = Get-KmcSha256 $statePath
        $replay = @(& $guardPath `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -RecoverWorkingRequalification -RequalificationRunId $runId `
            -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
            -Confirm:$false 6>&1)
        Assert-Test (($replay -join "`n") -like '*recovery PASS: already-recovered-prior*') 'completed state-less recovery was not terminally idempotent'
        Assert-Test ((Get-KmcSha256 $statePath) -ceq $terminalHash -and -not (Test-Path -LiteralPath $lockPath)) 'terminal state-less replay mutated state or recreated a lock'
    }

    Invoke-HarnessTest 'Working requalification recovery restores replacement-written crash' {
        $fixture = New-TestPendingWorkingRequalification 'recover-prepared'
        $runId = 'recover-prepared-test'
        try {
            Invoke-KmcWorkingFixtureRequalificationTransaction `
                -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
                -PostWriteProbe { throw 'synthetic crash primary' } -BeforeRollbackProbe { throw 'synthetic crash before rollback' } | Out-Null
        }
        catch { }
        $statePath = Join-Path $fixture.stateRoot ('fixture-requalifications\' + $runId + '.json')
        $state = Read-KmcJson $statePath
        $state.phase = 'replacement-written'
        $state = Select-TestObjectProperties $state @(Get-KmcWorkingFixtureRequalificationStatePropertyNames 'replacement-written')
        Write-KmcJsonDurable -Path $statePath -Value $state
        $lockPath = Join-Path $fixture.stateRoot 'active-transaction.lock'
        Set-TestRuntimeLockOwnerDead $lockPath
        $result = Invoke-KmcWorkingFixtureRequalificationRecovery `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
            -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256
        Assert-Test ([string]$result.disposition -ceq 'prior-restored') 'replacement-written crash did not restore prior qualification'
        Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $fixture.oldQualificationSha256) 'replacement-written crash recovery did not restore exact prior bytes'
        Assert-Test (-not (Test-Path -LiteralPath $lockPath)) 'replacement-written crash recovery did not clear stale lock'
        $normalAccepted = $true
        try { Assert-KmcFixturePair -SaveRoot $fixture.saveRoot -QualificationPath $fixture.qualificationPath | Out-Null } catch { $normalAccepted = $false }
        Assert-Test (-not $normalAccepted) 'replacement-written crash recovery admitted revised Working'
    }

    Invoke-HarnessTest 'Working requalification recovery restores a prepared-state replacement crash window' {
        $fixture = New-TestPendingWorkingRequalification 'recover-prepared-write-window'
        $runId = 'recover-prepared-write-window-test'
        try {
            Invoke-KmcWorkingFixtureRequalificationTransaction `
                -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
                -PostWriteProbe { throw 'synthetic crash after qualification replacement' } `
                -BeforeRollbackProbe { throw 'synthetic process termination before rollback' } | Out-Null
        }
        catch { }
        $statePath = Join-Path $fixture.stateRoot ('fixture-requalifications\' + $runId + '.json')
        $state = Read-KmcJson $statePath
        $state.phase = 'prepared'
        $state = Select-TestObjectProperties $state @(Get-KmcWorkingFixtureRequalificationStatePropertyNames 'prepared')
        Write-KmcJsonDurable -Path $statePath -Value $state
        $lockPath = Join-Path $fixture.stateRoot 'active-transaction.lock'
        Set-TestRuntimeLockOwnerDead $lockPath
        $result = Invoke-KmcWorkingFixtureRequalificationRecovery `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
            -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256
        Assert-Test ([string]$result.disposition -ceq 'prior-restored') 'prepared-state replacement window did not restore prior qualification'
        Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $fixture.oldQualificationSha256 -and
            -not (Test-Path -LiteralPath $lockPath)) 'prepared-state replacement window was not recovered exactly'
    }

    Invoke-HarnessTest 'Working requalification recovery resumes between prior bytes and timestamp restoration' {
        $fixture = New-TestPendingWorkingRequalification 'recover-between-bytes-and-time'
        $runId = 'recover-between-bytes-and-time-test'
        try {
            Invoke-KmcWorkingFixtureRequalificationTransaction `
                -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
                -PostWriteProbe { throw 'retain revised qualification for split-restore test' } `
                -BeforeRollbackProbe { throw 'defer split restore to recovery' } | Out-Null
        }
        catch { }
        $lockPath = Join-Path $fixture.stateRoot 'active-transaction.lock'
        Set-TestRuntimeLockOwnerDead $lockPath
        $message = $null
        try {
            Invoke-KmcWorkingFixtureRequalificationRecovery `
                -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -AfterRecoveryBytesBeforeTimestampProbe { throw 'synthetic crash between bytes and timestamp' } | Out-Null
        }
        catch { $message = $_.Exception.Message }
        Assert-Test ($message -like '*synthetic crash between bytes and timestamp*' -and
            $message -like '*retained the runtime lock*') 'split restore crash did not fail closed'
        $statePath = Join-Path $fixture.stateRoot ('fixture-requalifications\' + $runId + '.json')
        $state = Read-KmcJson $statePath
        Assert-Test ([string]$state.phase -ceq 'recovery-restore-prepared') 'split restore did not durably record its resumable phase before qualification mutation'
        $qualification = Get-Item -LiteralPath $fixture.qualificationPath -Force
        Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $fixture.oldQualificationSha256 -and
            $qualification.LastWriteTimeUtc.Ticks -ne [long]$state.priorQualificationLastWriteTimeUtcTicks) 'split restore fixture did not reach exact-prior-bytes/incomplete-metadata state'
        Set-TestRuntimeLockOwnerDead $lockPath
        $recovered = Invoke-KmcWorkingFixtureRequalificationRecovery `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
            -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256
        $qualification = Get-Item -LiteralPath $fixture.qualificationPath -Force
        Assert-Test ([string]$recovered.disposition -ceq 'prior-restored' -and
            $qualification.LastWriteTimeUtc.Ticks -eq [long]$state.priorQualificationLastWriteTimeUtcTicks -and
            -not (Test-Path -LiteralPath $lockPath)) 'split restore recovery did not repair exact prior metadata and clear its lock'
    }

    Invoke-HarnessTest 'Working requalification recovery accepts exact committed crash' {
        $fixture = New-TestPendingWorkingRequalification 'recover-committed'
        $guardPath = Join-Path $repoRoot 'scripts\runtime\Test-KmcFixtureGuard.ps1'
        [void]@(& $guardPath `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -RequalifyWorking `
            -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
            -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
            -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
            -Confirm:$false 6>&1)
        $statePath = @(Get-ChildItem -LiteralPath (Join-Path $fixture.stateRoot 'fixture-requalifications') -Filter '*.json' |
            Where-Object { $_.Name -notlike '*.prior.json' })[0].FullName
        $state = Read-KmcJson $statePath
        $runId = [string]$state.runId
        $lock = Open-KmcRuntimeLock -StateRoot $fixture.stateRoot -RunId $runId -Purpose 'fixture-requalification'
        $state.token = [string]$lock.Token
        Write-KmcJsonDurable -Path $statePath -Value $state
        Abandon-KmcRuntimeLock $lock
        $lockPath = Join-Path $fixture.stateRoot 'active-transaction.lock'
        Set-TestRuntimeLockOwnerDead $lockPath
        $result = Invoke-KmcWorkingFixtureRequalificationRecovery `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
            -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256
        Assert-Test ([string]$result.disposition -ceq 'committed') 'exact committed crash was not accepted'
        $validated = Assert-KmcFixturePair -SaveRoot $fixture.saveRoot -QualificationPath $fixture.qualificationPath
        Assert-Test ([string]$validated.working.sha256 -ceq $fixture.revisedWorkingSha256) 'committed crash recovery lost revised Working qualification'
        Assert-Test (-not (Test-Path -LiteralPath $lockPath)) 'committed crash recovery did not clear stale lock'
        $terminalHash = Get-KmcSha256 $statePath
        $replay = Invoke-KmcWorkingFixtureRequalificationRecovery `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
            -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256
        Assert-Test ([string]$replay.disposition -ceq 'already-recovered-committed') 'completed committed recovery was not terminally idempotent'
        Assert-Test ((Get-KmcSha256 $statePath) -ceq $terminalHash -and -not (Test-Path -LiteralPath $lockPath)) 'terminal committed replay mutated state or recreated a lock'
    }

    Invoke-HarnessTest 'Working requalification recovery rejects an unbound or wrong-kind stale sentinel' {
        $unbound = New-TestPendingWorkingRequalification 'recover-unbound-lock'
        $runId = 'recover-unbound-lock-test'
        $lock = Open-KmcRuntimeLock -StateRoot $unbound.stateRoot -RunId $runId
        Abandon-KmcRuntimeLock $lock
        $lockPath = Join-Path $unbound.stateRoot 'active-transaction.lock'
        Set-TestRuntimeLockOwnerDead $lockPath
        $lockHash = Get-KmcSha256 $lockPath
        $qualificationHash = Get-KmcSha256 $unbound.qualificationPath
        $message = $null
        try {
            Get-KmcWorkingFixtureRequalificationRecoveryPlan `
                -SaveRoot $unbound.saveRoot -StateRoot $unbound.stateRoot -QualificationPath $unbound.qualificationPath -RunId $runId `
                -ExpectedPriorQualificationSha256 $unbound.oldQualificationSha256 -ExpectedBaselineSha256 $unbound.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $unbound.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $unbound.revisedWorkingSha256 | Out-Null
        }
        catch { $message = $_.Exception.Message }
        Assert-Test ($message -like '*requalification recovery lock*property set is not exact*') 'unpurposed stale sentinel was treated as fixture requalification'
        Assert-Test ((Get-KmcSha256 $lockPath) -ceq $lockHash -and
            (Get-KmcSha256 $unbound.qualificationPath) -ceq $qualificationHash) 'unbound lock rejection mutated lock or qualification'
        Remove-Item -LiteralPath $lockPath -Force

        $bound = New-TestPendingWorkingRequalification 'recover-purpose-bound-earliest'
        $boundRunId = 'recover-purpose-bound-earliest-test'
        $boundLock = Open-KmcRuntimeLock -StateRoot $bound.stateRoot -RunId $boundRunId -Purpose 'fixture-requalification'
        Abandon-KmcRuntimeLock $boundLock
        $boundLockPath = Join-Path $bound.stateRoot 'active-transaction.lock'
        Set-TestRuntimeLockOwnerDead $boundLockPath
        $boundTmp = Join-Path $bound.stateRoot ('.fixture-qualification.json.' + [Guid]::NewGuid().ToString('N') + '.tmp')
        [IO.File]::WriteAllText($boundTmp, 'partial owned candidate from interrupted atomic write')
        $boundPlan = Get-KmcWorkingFixtureRequalificationRecoveryPlan `
            -SaveRoot $bound.saveRoot -StateRoot $bound.stateRoot -QualificationPath $bound.qualificationPath -RunId $boundRunId `
            -ExpectedPriorQualificationSha256 $bound.oldQualificationSha256 -ExpectedBaselineSha256 $bound.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $bound.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $bound.revisedWorkingSha256
        Assert-Test ([string]$boundPlan.action -ceq 'prepare-purpose-bound-lock' -and
            @($boundPlan.atomicDebris).Count -eq 1) 'earliest purpose-bound crash and its atomic temp were not recognized before backup/state creation'
        $boundResult = Invoke-KmcWorkingFixtureRequalificationRecovery `
            -SaveRoot $bound.saveRoot -StateRoot $bound.stateRoot -QualificationPath $bound.qualificationPath -RunId $boundRunId `
            -ExpectedPriorQualificationSha256 $bound.oldQualificationSha256 -ExpectedBaselineSha256 $bound.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $bound.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $bound.revisedWorkingSha256
        $boundTransactionRoot = Join-Path $bound.stateRoot 'fixture-requalifications'
        Assert-Test ([string]$boundResult.disposition -ceq 'prior-restored' -and
            (Test-Path -LiteralPath (Join-Path $boundTransactionRoot ($boundRunId + '.prior.json')) -PathType Leaf) -and
            (Test-Path -LiteralPath (Join-Path $boundTransactionRoot ($boundRunId + '.json')) -PathType Leaf) -and
            -not (Test-Path -LiteralPath $boundLockPath) -and -not (Test-Path -LiteralPath $boundTmp)) 'earliest purpose-bound crash did not reconcile temp, create exact durable recovery records, and clear its lock'

        $saveRace = New-TestPendingWorkingRequalification 'recover-purpose-save-race'
        $saveRaceRunId = 'recover-purpose-save-race-test'
        $saveRaceLock = Open-KmcRuntimeLock -StateRoot $saveRace.stateRoot -RunId $saveRaceRunId -Purpose 'fixture-requalification'
        Abandon-KmcRuntimeLock $saveRaceLock
        $saveRaceLockPath = Join-Path $saveRace.stateRoot 'active-transaction.lock'
        Set-TestRuntimeLockOwnerDead $saveRaceLockPath
        $saveBefore = Get-KmcSaveMetadataInventory $saveRace.saveRoot
        $message = $null
        try {
            Invoke-KmcWorkingFixtureRequalificationRecovery `
                -SaveRoot $saveRace.saveRoot -StateRoot $saveRace.stateRoot -QualificationPath $saveRace.qualificationPath -RunId $saveRaceRunId `
                -ExpectedPriorQualificationSha256 $saveRace.oldQualificationSha256 -ExpectedBaselineSha256 $saveRace.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $saveRace.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $saveRace.revisedWorkingSha256 `
                -AfterPurposeBoundBackupProbe {
                    [IO.File]::AppendAllText((Join-Path $saveRace.saveRoot 'Manual_3_PERSONAL.zks'), '-foreign-race')
                } | Out-Null
        }
        catch { $message = $_.Exception.Message }
        $saveRaceStatePath = Join-Path $saveRace.stateRoot ('fixture-requalifications\' + $saveRaceRunId + '.json')
        $saveRaceState = Read-KmcJson $saveRaceStatePath
        Assert-Test (($message -like '*save digest are invalid*' -or $message -like '*save metadata*') -and
            $message -like '*retained the runtime lock*') 'foreign save race during purpose-bound backup became a new baseline'
        Assert-Test ([string]$saveRaceState.saveMetadataDigestBefore -ceq [string]$saveBefore.digest -and
            [string]$saveRaceState.saveMetadataDigestBefore -cne [string](Get-KmcSaveMetadataInventory $saveRace.saveRoot).digest -and
            (Test-Path -LiteralPath $saveRaceLockPath -PathType Leaf)) 'purpose-bound recovery did not durably preserve first-owned save metadata after a race'

        $debrisRace = New-TestPendingWorkingRequalification 'recover-new-debris-race'
        $debrisRaceRunId = 'recover-new-debris-race-test'
        $debrisRaceLock = Open-KmcRuntimeLock -StateRoot $debrisRace.stateRoot -RunId $debrisRaceRunId -Purpose 'fixture-requalification'
        Abandon-KmcRuntimeLock $debrisRaceLock
        $debrisRaceLockPath = Join-Path $debrisRace.stateRoot 'active-transaction.lock'
        Set-TestRuntimeLockOwnerDead $debrisRaceLockPath
        $initialDebris = Join-Path $debrisRace.stateRoot ('.fixture-qualification.json.' + [Guid]::NewGuid().ToString('N') + '.tmp')
        [IO.File]::WriteAllText($initialDebris, 'initial bounded temp')
        $newDebris = Join-Path $debrisRace.stateRoot ('.fixture-qualification.json.' + [Guid]::NewGuid().ToString('N') + '.tmp')
        $message = $null
        try {
            Invoke-KmcWorkingFixtureRequalificationRecovery `
                -SaveRoot $debrisRace.saveRoot -StateRoot $debrisRace.stateRoot -QualificationPath $debrisRace.qualificationPath -RunId $debrisRaceRunId `
                -ExpectedPriorQualificationSha256 $debrisRace.oldQualificationSha256 -ExpectedBaselineSha256 $debrisRace.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $debrisRace.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $debrisRace.revisedWorkingSha256 `
                -AfterDebrisReconciliationProbe { [IO.File]::WriteAllText($newDebris, 'new debris after cleanup') } | Out-Null
        }
        catch { $message = $_.Exception.Message }
        Assert-Test ($message -like '*atomic debris changed during guarded reconciliation*' -and
            $message -like '*retained the runtime lock*') 'new debris during reconciliation was silently carried through PASS'
        Assert-Test (-not (Test-Path -LiteralPath $initialDebris) -and
            (Test-Path -LiteralPath $newDebris -PathType Leaf) -and
            (Test-Path -LiteralPath $debrisRaceLockPath -PathType Leaf)) 'debris convergence failure did not retain the exact new debris and fail-closed lock'

        $unknownDebris = New-TestPendingWorkingRequalification 'recover-unknown-debris'
        $unknownRunId = 'recover-unknown-debris-test'
        $unknownLock = Open-KmcRuntimeLock -StateRoot $unknownDebris.stateRoot -RunId $unknownRunId -Purpose 'fixture-requalification'
        Abandon-KmcRuntimeLock $unknownLock
        $unknownLockPath = Join-Path $unknownDebris.stateRoot 'active-transaction.lock'
        Set-TestRuntimeLockOwnerDead $unknownLockPath
        $unknownPath = Join-Path $unknownDebris.stateRoot '.fixture-qualification.json.not-a-guid.tmp'
        [IO.File]::WriteAllText($unknownPath, 'unrecognized debris')
        $message = $null
        try {
            Get-KmcWorkingFixtureRequalificationRecoveryPlan `
                -SaveRoot $unknownDebris.saveRoot -StateRoot $unknownDebris.stateRoot -QualificationPath $unknownDebris.qualificationPath -RunId $unknownRunId `
                -ExpectedPriorQualificationSha256 $unknownDebris.oldQualificationSha256 -ExpectedBaselineSha256 $unknownDebris.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $unknownDebris.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $unknownDebris.revisedWorkingSha256 | Out-Null
        }
        catch { $message = $_.Exception.Message }
        Assert-Test ($message -like '*unrecognized atomic debris*') 'unrecognized atomic debris entered guarded reconciliation'
        Assert-Test (Test-Path -LiteralPath $unknownLockPath -PathType Leaf) 'unknown debris rejection cleared its purpose-bound lock'

        foreach ($kind in @('lock-directory','state-directory','backup-directory')) {
            $fixture = New-TestPendingWorkingRequalification ('recover-wrong-kind-' + $kind)
            $kindRunId = 'recover-wrong-kind-' + $kind
            $kindLockPath = Join-Path $fixture.stateRoot 'active-transaction.lock'
            $transactionRoot = Join-Path $fixture.stateRoot 'fixture-requalifications'
            New-Item -ItemType Directory -Path $transactionRoot -Force | Out-Null
            if ($kind -ceq 'lock-directory') {
                New-Item -ItemType Directory -Path $kindLockPath | Out-Null
            }
            else {
                $kindLock = Open-KmcRuntimeLock -StateRoot $fixture.stateRoot -RunId $kindRunId
                Abandon-KmcRuntimeLock $kindLock
                Set-TestRuntimeLockOwnerDead $kindLockPath
                $wrongPath = if ($kind -ceq 'state-directory') {
                    Join-Path $transactionRoot ($kindRunId + '.json')
                } else { Join-Path $transactionRoot ($kindRunId + '.prior.json') }
                New-Item -ItemType Directory -Path $wrongPath | Out-Null
            }
            $threw = $false
            try {
                Get-KmcWorkingFixtureRequalificationRecoveryPlan `
                    -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $kindRunId `
                    -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                    -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 | Out-Null
            }
            catch { $threw = $true }
            Assert-Test $threw "wrong-kind $kind path was treated as absent or recoverable"
            Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $fixture.oldQualificationSha256) "wrong-kind $kind rejection changed qualification"
        }
    }

    Invoke-HarnessTest 'Working requalification recovery rejects linked containment and identity files' {
        $linkedLock = New-TestPendingWorkingRequalification 'recover-linked-lock'
        $lockTarget = Join-Path $linkedLock.stateRoot 'foreign-lock-target.json'
        Write-KmcJsonAtomic -Path $lockTarget -Value ([ordered]@{
            schemaVersion=1;runId='recover-linked-lock-test';token=('1' * 64);ownerProcessId=2147483646;createdAtUtc=[DateTime]::UtcNow.ToString('o')
        })
        $lockPath = Join-Path $linkedLock.stateRoot 'active-transaction.lock'
        New-Item -ItemType HardLink -Path $lockPath -Target $lockTarget | Out-Null
        $message = $null
        try {
            Get-KmcWorkingFixtureRequalificationRecoveryPlan `
                -SaveRoot $linkedLock.saveRoot -StateRoot $linkedLock.stateRoot -QualificationPath $linkedLock.qualificationPath -RunId 'recover-linked-lock-test' `
                -ExpectedPriorQualificationSha256 $linkedLock.oldQualificationSha256 -ExpectedBaselineSha256 $linkedLock.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $linkedLock.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $linkedLock.revisedWorkingSha256 | Out-Null
        }
        catch { $message = $_.Exception.Message }
        Assert-Test ($message -like '*hard link*') 'hard-linked stale lock was accepted for adoption'

        $linkedQualification = New-TestPendingWorkingRequalification 'recover-linked-qualification'
        $qualificationTarget = Join-Path $linkedQualification.stateRoot 'qualification-target.json'
        Move-Item -LiteralPath $linkedQualification.qualificationPath -Destination $qualificationTarget
        New-Item -ItemType HardLink -Path $linkedQualification.qualificationPath -Target $qualificationTarget | Out-Null
        $message = $null
        try {
            Get-KmcWorkingFixtureRequalificationRecoveryPlan `
                -SaveRoot $linkedQualification.saveRoot -StateRoot $linkedQualification.stateRoot -QualificationPath $linkedQualification.qualificationPath -RunId 'recover-linked-qualification-test' `
                -ExpectedPriorQualificationSha256 $linkedQualification.oldQualificationSha256 -ExpectedBaselineSha256 $linkedQualification.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $linkedQualification.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $linkedQualification.revisedWorkingSha256 | Out-Null
        }
        catch { $message = $_.Exception.Message }
        Assert-Test ($message -like '*hard link*') 'hard-linked qualification was accepted for recovery'

        $junctionFixture = New-TestPendingWorkingRequalification 'recover-junction-root'
        $junctionRunId = 'recover-junction-root-test'
        $junctionLock = Open-KmcRuntimeLock -StateRoot $junctionFixture.stateRoot -RunId $junctionRunId
        Abandon-KmcRuntimeLock $junctionLock
        Set-TestRuntimeLockOwnerDead (Join-Path $junctionFixture.stateRoot 'active-transaction.lock')
        $junctionTarget = Join-Path $junctionFixture.root 'outside-transaction-root'
        New-Item -ItemType Directory -Path $junctionTarget | Out-Null
        New-Item -ItemType Junction -Path (Join-Path $junctionFixture.stateRoot 'fixture-requalifications') -Target $junctionTarget | Out-Null
        $threw = $false
        try {
            Get-KmcWorkingFixtureRequalificationRecoveryPlan `
                -SaveRoot $junctionFixture.saveRoot -StateRoot $junctionFixture.stateRoot -QualificationPath $junctionFixture.qualificationPath -RunId $junctionRunId `
                -ExpectedPriorQualificationSha256 $junctionFixture.oldQualificationSha256 -ExpectedBaselineSha256 $junctionFixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $junctionFixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $junctionFixture.revisedWorkingSha256 | Out-Null
        }
        catch { $threw = $true }
        Assert-Test $threw 'reparse transaction-root ancestor was accepted for recovery reads or writes'
    }

    Invoke-HarnessTest 'Working requalification recovery enforces exact phase schemas and prior metadata' {
        $fixture = New-TestPendingWorkingRequalification 'recover-schema-mutations'
        $guardPath = Join-Path $repoRoot 'scripts\runtime\Test-KmcFixtureGuard.ps1'
        [void]@(& $guardPath `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -RequalifyWorking `
            -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
            -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
            -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
            -Confirm:$false 6>&1)
        $statePath = @(Get-ChildItem -LiteralPath (Join-Path $fixture.stateRoot 'fixture-requalifications') -Filter '*.json' |
            Where-Object { $_.Name -notlike '*.prior.json' })[0].FullName
        $originalStateBytes = [IO.File]::ReadAllBytes($statePath)
        $state = Read-KmcJson $statePath
        $runId = [string]$state.runId
        $lock = Open-KmcRuntimeLock -StateRoot $fixture.stateRoot -RunId $runId -Purpose 'fixture-requalification'
        $state.token = [string]$lock.Token
        Write-KmcJsonDurable -Path $statePath -Value $state
        $exactStateBytes = [IO.File]::ReadAllBytes($statePath)
        Abandon-KmcRuntimeLock $lock
        $lockPath = Join-Path $fixture.stateRoot 'active-transaction.lock'
        Set-TestRuntimeLockOwnerDead $lockPath

        $mutated = Read-KmcJson $statePath
        $mutated.PSObject.Properties.Remove('committedAtUtc')
        Write-KmcJsonDurable -Path $statePath -Value $mutated
        $threw = $false
        try {
            Get-KmcWorkingFixtureRequalificationRecoveryPlan `
                -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 | Out-Null
        }
        catch { $threw = $true }
        Assert-Test $threw 'committed phase accepted a missing required mutation field'
        Write-KmcBytesDurableAtomic -Path $statePath -Bytes $exactStateBytes
        $mutated = Read-KmcJson $statePath
        $mutated | Add-Member -NotePropertyName illegalPhaseField -NotePropertyValue 'forbidden'
        Write-KmcJsonDurable -Path $statePath -Value $mutated
        $threw = $false
        try {
            Get-KmcWorkingFixtureRequalificationRecoveryPlan `
                -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 | Out-Null
        }
        catch { $threw = $true }
        Assert-Test $threw 'committed phase accepted an extraneous mutation field'
        Write-KmcBytesDurableAtomic -Path $statePath -Bytes $exactStateBytes
        [void](Invoke-KmcWorkingFixtureRequalificationRecovery `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
            -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256)
        $terminalBytes = [IO.File]::ReadAllBytes($statePath)
        $terminalMutation = Read-KmcJson $statePath
        $terminalMutation.PSObject.Properties.Remove('recoverySaveMetadataDigest')
        Write-KmcJsonDurable -Path $statePath -Value $terminalMutation
        $threw = $false
        try {
            Get-KmcWorkingFixtureRequalificationRecoveryPlan `
                -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 | Out-Null
        }
        catch { $threw = $true }
        Assert-Test $threw 'terminal recovered-committed phase accepted a missing recovery proof field'
        Write-KmcBytesDurableAtomic -Path $statePath -Bytes $terminalBytes

        $priorFixture = New-TestPendingWorkingRequalification 'recover-prior-timestamp-drift'
        $priorRunId = 'recover-prior-timestamp-drift-test'
        try {
            Invoke-KmcWorkingFixtureRequalificationTransaction `
                -SaveRoot $priorFixture.saveRoot -StateRoot $priorFixture.stateRoot -QualificationPath $priorFixture.qualificationPath -RunId $priorRunId `
                -ExpectedExistingQualificationSha256 $priorFixture.oldQualificationSha256 -ExpectedBaselineSha256 $priorFixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $priorFixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $priorFixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $priorFixture.priorSaveTransactionStatePath -ExpectedPriorSaveTransactionRunId $priorFixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $priorFixture.priorSaveTransactionStateSha256 -ExpectedPriorSaveMetadataDigest $priorFixture.priorSaveMetadataDigest `
                -PostWriteProbe { throw 'force exact rollback for timestamp test' } | Out-Null
        }
        catch { }
        $priorStatePath = Join-Path $priorFixture.stateRoot ('fixture-requalifications\' + $priorRunId + '.json')
        $priorState = Read-KmcJson $priorStatePath
        $priorLock = Open-KmcRuntimeLock -StateRoot $priorFixture.stateRoot -RunId $priorRunId -Purpose 'fixture-requalification'
        $priorState.token = [string]$priorLock.Token
        Write-KmcJsonDurable -Path $priorStatePath -Value $priorState
        Abandon-KmcRuntimeLock $priorLock
        $priorLockPath = Join-Path $priorFixture.stateRoot 'active-transaction.lock'
        Set-TestRuntimeLockOwnerDead $priorLockPath
        $priorQualification = Get-Item -LiteralPath $priorFixture.qualificationPath -Force
        [IO.File]::SetLastWriteTimeUtc($priorFixture.qualificationPath, $priorQualification.LastWriteTimeUtc.AddSeconds(2))
        $driftPlan = Get-KmcWorkingFixtureRequalificationRecoveryPlan `
            -SaveRoot $priorFixture.saveRoot -StateRoot $priorFixture.stateRoot -QualificationPath $priorFixture.qualificationPath -RunId $priorRunId `
            -ExpectedPriorQualificationSha256 $priorFixture.oldQualificationSha256 -ExpectedBaselineSha256 $priorFixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $priorFixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $priorFixture.revisedWorkingSha256
        Assert-Test ([string]$driftPlan.action -ceq 'restore-prior' -and
            (Test-Path -LiteralPath $priorLockPath -PathType Leaf)) 'nonterminal prior timestamp drift was not classified for guarded repair'
        $recovered = Invoke-KmcWorkingFixtureRequalificationRecovery `
            -SaveRoot $priorFixture.saveRoot -StateRoot $priorFixture.stateRoot -QualificationPath $priorFixture.qualificationPath -RunId $priorRunId `
            -ExpectedPriorQualificationSha256 $priorFixture.oldQualificationSha256 -ExpectedBaselineSha256 $priorFixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $priorFixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $priorFixture.revisedWorkingSha256
        $repairedQualification = Get-Item -LiteralPath $priorFixture.qualificationPath -Force
        Assert-Test ([string]$recovered.disposition -ceq 'prior-restored' -and
            $repairedQualification.LastWriteTimeUtc.Ticks -eq [long]$priorState.priorQualificationLastWriteTimeUtcTicks -and
            -not (Test-Path -LiteralPath $priorLockPath)) 'nonterminal prior timestamp drift was not repaired exactly'
        [IO.File]::SetLastWriteTimeUtc($priorFixture.qualificationPath, $repairedQualification.LastWriteTimeUtc.AddSeconds(2))
        $threw = $false
        try {
            Get-KmcWorkingFixtureRequalificationRecoveryPlan `
                -SaveRoot $priorFixture.saveRoot -StateRoot $priorFixture.stateRoot -QualificationPath $priorFixture.qualificationPath -RunId $priorRunId `
                -ExpectedPriorQualificationSha256 $priorFixture.oldQualificationSha256 -ExpectedBaselineSha256 $priorFixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $priorFixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $priorFixture.revisedWorkingSha256 | Out-Null
        }
        catch { $threw = $true }
        Assert-Test $threw 'terminal no-lock prior timestamp drift was silently accepted or repaired'
    }

    Invoke-HarnessTest 'Working requalification recovery rechecks lock authority before each mutation' {
        foreach ($probePoint in @('restore','state-write')) {
            $fixture = New-TestPendingWorkingRequalification ('recover-authority-' + $probePoint)
            $runId = 'recover-authority-' + $probePoint
            try {
                Invoke-KmcWorkingFixtureRequalificationTransaction `
                    -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                    -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                    -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                    -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
                    -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
                    -PostWriteProbe { throw 'synthetic crash for recovery authority test' } `
                    -BeforeRollbackProbe { throw 'retain revised qualification for recovery authority test' } | Out-Null
            }
            catch { }
            $statePath = Join-Path $fixture.stateRoot ('fixture-requalifications\' + $runId + '.json')
            $state = Read-KmcJson $statePath
            $state.phase = 'replacement-written'
            $state = Select-TestObjectProperties $state @(Get-KmcWorkingFixtureRequalificationStatePropertyNames 'replacement-written')
            Write-KmcJsonDurable -Path $statePath -Value $state
            $stateHashBefore = Get-KmcSha256 $statePath
            $qualificationHashBefore = Get-KmcSha256 $fixture.qualificationPath
            $lockPath = Join-Path $fixture.stateRoot 'active-transaction.lock'
            Set-TestRuntimeLockOwnerDead $lockPath
            $message = $null
            try {
                $arguments = @{
                    SaveRoot=$fixture.saveRoot;StateRoot=$fixture.stateRoot;QualificationPath=$fixture.qualificationPath;RunId=$runId
                    ExpectedPriorQualificationSha256=$fixture.oldQualificationSha256;ExpectedBaselineSha256=$fixture.baselineSha256
                    ExpectedSupersededWorkingSha256=$fixture.supersededWorkingSha256;ExpectedRevisedWorkingSha256=$fixture.revisedWorkingSha256
                }
                if ($probePoint -ceq 'restore') {
                    $arguments['BeforeRestoreProbe'] = { param($heldLock,$ignored); $heldLock.Stream.Dispose() }
                }
                else {
                    $arguments['BeforeRecoveryStateWriteProbe'] = { param($heldLock,$ignored); $heldLock.Stream.Dispose() }
                }
                Invoke-KmcWorkingFixtureRequalificationRecovery @arguments | Out-Null
            }
            catch { $message = $_.Exception.Message }
            Assert-Test ($message -like '*retained the runtime lock*runtime lock handle is not readable*') "lost authority at $probePoint did not fail closed"
            Assert-Test (Test-Path -LiteralPath $lockPath -PathType Leaf) "lost authority at $probePoint did not retain the runtime sentinel"
            if ($probePoint -ceq 'restore') {
                Assert-Test ((Get-KmcSha256 $statePath) -ceq $stateHashBefore) 'lost authority before restore wrote any recovery state'
                Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $qualificationHashBefore) 'lost authority before restore changed qualification'
            }
            else {
                $preparedState = Read-KmcJson $statePath
                Assert-Test ([string]$preparedState.phase -ceq 'recovery-restore-prepared') 'lost authority before terminal state write did not retain the exact resumable restore phase'
                Assert-Test ((Get-KmcSha256 $fixture.qualificationPath) -ceq $fixture.oldQualificationSha256) 'state-write authority failure occurred before exact qualification restore'
            }
            Set-TestRuntimeLockOwnerDead $lockPath
            $recovered = Invoke-KmcWorkingFixtureRequalificationRecovery `
                -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256
            Assert-Test ([string]$recovered.disposition -ceq 'prior-restored') "recovery could not safely resume after $probePoint authority failure"
        }
    }

    Invoke-HarnessTest 'Working requalification revalidates canonical state before deleting debris' {
        $fixture = New-TestPendingWorkingRequalification 'recover-debris-race'
        $runId = 'recover-debris-race-test'
        try {
            Invoke-KmcWorkingFixtureRequalificationTransaction `
                -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                -ExpectedExistingQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
                -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
                -PostWriteProbe { throw 'retain fixture for debris race' } -BeforeRollbackProbe { throw 'retain lock for debris race' } | Out-Null
        }
        catch { }
        $statePath = Join-Path $fixture.stateRoot ('fixture-requalifications\' + $runId + '.json')
        $debrisPath = Join-Path (Split-Path -Parent $statePath) ('.' + [IO.Path]::GetFileName($statePath) + '.' + [Guid]::NewGuid().ToString('N') + '.bak')
        Copy-Item -LiteralPath $statePath -Destination $debrisPath
        $lockPath = Join-Path $fixture.stateRoot 'active-transaction.lock'
        Set-TestRuntimeLockOwnerDead $lockPath
        $message = $null
        try {
            Invoke-KmcWorkingFixtureRequalificationRecovery `
                -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -QualificationPath $fixture.qualificationPath -RunId $runId `
                -ExpectedPriorQualificationSha256 $fixture.oldQualificationSha256 -ExpectedBaselineSha256 $fixture.baselineSha256 `
                -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
                -AfterAdoptBeforeOwnedPlanProbe {
                    $value = Read-KmcJson $statePath
                    $value | Add-Member -NotePropertyName racedCanonicalMutation -NotePropertyValue 'must block debris deletion'
                    Write-KmcJsonDurable -Path $statePath -Value $value
                } | Out-Null
        }
        catch { $message = $_.Exception.Message }
        Assert-Test ($message -like '*property set is not exact*' -and
            $message -like '*retained the runtime lock*') 'canonical state race did not fail before reconciliation'
        Assert-Test ((Test-Path -LiteralPath $debrisPath -PathType Leaf) -and
            (Test-Path -LiteralPath $lockPath -PathType Leaf)) 'pre-adoption debris snapshot was deleted before owned canonical revalidation'
    }

    Invoke-HarnessTest 'exact save-metadata equality rejects a foreign mutation' {
        $root = Join-Path $testRoot 'requalification-save-metadata-mutation'
        New-Item -ItemType Directory -Path $root | Out-Null
        $foreign = Join-Path $root 'Manual_1_PERSONAL.zks'
        [IO.File]::WriteAllText($foreign, 'protected')
        $before = Get-KmcSaveMetadataInventory $root
        [IO.File]::AppendAllText($foreign, '-changed')
        $threw = $false
        try { Assert-KmcSaveMetadataInventoriesEqual -Before $before -After (Get-KmcSaveMetadataInventory $root) -Description 'synthetic foreign save' } catch { $threw = $true }
        Assert-Test $threw 'exact save-metadata equality accepted a foreign save mutation'
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

    Invoke-HarnessTest 'runtime launcher continuity pins fail closed before approval, lock, or staging' {
        $pinNames = @(
            'ExpectedCurrentQualificationSha256','ExpectedSupersededWorkingSha256','PriorSaveTransactionStatePath',
            'ExpectedPriorSaveTransactionRunId','ExpectedPriorSaveTransactionStateSha256','ExpectedPriorSaveMetadataDigest',
            'ProtectedSaveContinuityAuthorityPath','ExpectedProtectedSaveContinuityEpochId',
            'ExpectedProtectedSaveContinuityAuthoritySha256','ExpectedProtectedAutoSaveName','ExpectedProtectedAutoSaveSha256',
            'ExpectedProtectedQuickSaveName','ExpectedProtectedQuickSaveSha256'
        )
        $allPinArguments = @{
            IsSaveBacked = $true
            BoundContinuityPinNames = $pinNames
            ExpectedCurrentQualificationSha256 = 'a' * 64
            ExpectedSupersededWorkingSha256 = 'b' * 64
            PriorSaveTransactionStatePath = 'synthetic-prior-state.json'
            ExpectedPriorSaveTransactionRunId = 'synthetic-prior-run'
            ExpectedPriorSaveTransactionStateSha256 = 'c' * 64
            ExpectedPriorSaveMetadataDigest = 'd' * 64
            ProtectedSaveContinuityAuthorityPath = 'synthetic-protected-authority.json'
            ExpectedProtectedSaveContinuityEpochId = 'synthetic-protected-epoch'
            ExpectedProtectedSaveContinuityAuthoritySha256 = 'e' * 64
            ExpectedProtectedAutoSaveName = 'Auto_1.zks'
            ExpectedProtectedAutoSaveSha256 = 'f' * 64
            ExpectedProtectedQuickSaveName = 'Quick_1.zks'
            ExpectedProtectedQuickSaveSha256 = '1' * 64
        }
        [void](Assert-KmcRuntimeContinuityPinCombination @allPinArguments)
        $missingArguments = @{}
        foreach ($key in $allPinArguments.Keys) { $missingArguments[$key] = $allPinArguments[$key] }
        $missingArguments.BoundContinuityPinNames = @($pinNames | Where-Object { $_ -cne 'ExpectedPriorSaveMetadataDigest' })
        $threw = $false
        try { Assert-KmcRuntimeContinuityPinCombination @missingArguments | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'save-backed runtime pin gate accepted a syntactically missing pin'
        $noSaveArguments = @{
            IsSaveBacked = $false
            BoundContinuityPinNames = @('PriorSaveTransactionStatePath')
            PriorSaveTransactionStatePath = ''
        }
        $threw = $false
        try { Assert-KmcRuntimeContinuityPinCombination @noSaveArguments | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'no-save runtime pin gate accepted an explicitly bound empty continuity pin'

        $launcherSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\Invoke-KingmakerRuntimeScenario.ps1')
        foreach ($pinName in $pinNames) {
            Assert-Test ($launcherSource -cmatch ('\$' + [regex]::Escape($pinName) + '(?:\s|,)')) "runtime launcher does not expose $pinName"
        }
        $pinGateIndex = $launcherSource.IndexOf('[void](Assert-KmcRuntimeContinuityPinCombination', [StringComparison]::Ordinal)
        $validateSourceIndex = $launcherSource.IndexOf("& (Join-Path `$repoRoot 'scripts\Validate-Source.ps1')", [StringComparison]::Ordinal)
        $shouldProcessIndex = $launcherSource.IndexOf("if(-not `$PSCmdlet.ShouldProcess", [StringComparison]::Ordinal)
        $lockIndex = $launcherSource.IndexOf('    $lock=Open-KmcRuntimeLock', [StringComparison]::Ordinal)
        $combinedStateIndex = $launcherSource.IndexOf('    $combinedStatePath=New-KmcRunTransactionState', [StringComparison]::Ordinal)
        $enterSaveIndex = $launcherSource.IndexOf('        [void](Enter-KmcWorkingSaveTransaction', [StringComparison]::Ordinal)
        $enterModsIndex = $launcherSource.IndexOf('    [void](Enter-KmcModsTransaction', [StringComparison]::Ordinal)
        $continuityCalls = @([regex]::Matches(
            $launcherSource,
            '(?m)^\s*\$(?:preflightContinuity|whatIfContinuity|lockedContinuity)=Assert-KmcQualifiedWorkingProtectedSaveContinuity'))
        Assert-Test ($pinGateIndex -ge 0 -and $pinGateIndex -lt $validateSourceIndex -and $pinGateIndex -lt $shouldProcessIndex) `
            'runtime launcher does not reject incomplete/no-save pin combinations before validation or ShouldProcess'
        Assert-Test ($continuityCalls.Count -eq 3) 'runtime launcher does not perform exactly preflight, WhatIf, and locked continuity proofs'
        Assert-Test ($continuityCalls[0].Index -lt $shouldProcessIndex -and
            $continuityCalls[1].Index -gt $shouldProcessIndex -and $continuityCalls[1].Index -lt $lockIndex -and
            $continuityCalls[2].Index -gt $lockIndex -and $continuityCalls[2].Index -lt $combinedStateIndex) `
            'runtime launcher continuity proofs are not ordered before approval, during WhatIf, and under lock before durable state'
        Assert-Test ($combinedStateIndex -gt $continuityCalls[2].Index -and
            $enterSaveIndex -gt $combinedStateIndex -and $enterModsIndex -gt $enterSaveIndex) `
            'runtime launcher can stage durable run state, Mods, or Working before locked continuity succeeds'
        Assert-Test ($launcherSource.Contains('Recovery can restore an interrupted transaction, but never confers')) `
            'runtime launcher does not state that recovery never confers runtime admission'

        $commonSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
        $committedProbeIndex = $commonSource.IndexOf(
            'if ($null -ne $AfterCommittedStateProbe) { & $AfterCommittedStateProbe $lock $statePath }',
            [StringComparison]::Ordinal)
        $postCommitContinuityIndex = $commonSource.IndexOf(
            '$postCommitContinuity = Assert-KmcQualifiedWorkingPriorInventoryContinuity',
            $committedProbeIndex,
            [StringComparison]::Ordinal)
        $postCommitEqualityIndex = $commonSource.IndexOf(
            "-Description 'Working fixture requalification post-commit save metadata'",
            $postCommitContinuityIndex,
            [StringComparison]::Ordinal)
        $postCommitCloseIndex = $commonSource.IndexOf('Close-KmcRuntimeLock $lock', $postCommitEqualityIndex, [StringComparison]::Ordinal)
        Assert-Test ($committedProbeIndex -ge 0 -and $postCommitContinuityIndex -gt $committedProbeIndex -and
            $postCommitEqualityIndex -gt $postCommitContinuityIndex -and $postCommitCloseIndex -gt $postCommitEqualityIndex) `
            'requalification can close its lock after the committed-state probe without re-proving exact save continuity'
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

    Invoke-HarnessTest 'mounted rider grounding repair is exact-token, exact-pair, and runtime-probed' {
        $patchSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPatchController.cs')
        $serviceSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\GameMountedRelationshipService.cs')
        $engineSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeMovementScenarioEngine.cs')
        Assert-Test ($patchSource.Contains('PatchExact(typeof(UnitEntityView), "ForcePlaceAboveGround", 0x06001848, Type.EmptyTypes, nameof(PatchMethods.ForcePlaceAboveGroundPrefix));')) 'grounding repair does not pin the exact Kingmaker method token and parameter list'
        Assert-Test ($patchSource.Contains('!PatchBridge.Service.TrySuppressRiderGroundPlacement(__instance)')) 'grounding prefix does not delegate its exact-instance decision to the relationship service'
        Assert-Test ($serviceSource.Contains('MountedRiderGroundingPolicy.ShouldSuppress(') -and $serviceSource.Contains('RiderGroundPlacementSuppressionCount++;')) 'relationship service does not apply and count the exact active-rider policy'
        Assert-Test ($engineSource.Contains('rider.View.ForcePlaceAboveGround();') -and $engineSource.Contains('suppressionCountAfter == suppressionCountBefore + 1L')) 'camera qualification does not deterministically exercise the exact grounding repair'
        Assert-Test (-not $patchSource.Contains('PatchExact(typeof(UnitMoveController)')) 'grounding repair introduced a global UnitMoveController patch'
    }

    Invoke-HarnessTest 'lifecycle evidence is a durable pre-mount gate with bounded cleanup observation' {
        $source = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeLifecycleScenarioEngine.cs')
        $evidenceGate = $source.IndexOf('if (!TryWriteEvidence("pre-mount", null, null))', [StringComparison]::Ordinal)
        $mountCall = $source.IndexOf('mounted = relationship.MountAutomationPair();', [StringComparison]::Ordinal)
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
        $v2Request=[pscustomobject]@{runId='recompute-test';scenario='fixture-intake';branch='codex/mounted-combat-feasibility';commit=('0'*40);productVersion='0.1.0-phase2a-dev.12';dllSha256=('a'*64);dllMvid=[Guid]::Empty.ToString();transactionToken=('b'*64);evidenceRoot=$recomputeEvidence;fixture=$fixture}
        $recomputeManifestHash = New-TestArtifactManifest -EvidenceRoot $recomputeEvidence -RunId $v2Request.runId -Scenario $v2Request.scenario
        $game=[pscustomobject]@{status='PASS';fixture=$fixture;evidenceManifestSha256=$recomputeManifestHash;subscenarioTotal=99;subscenarioPassCount=0;subscenarioFailCount=99;assertionPassCount=0;assertionFailCount=99;subscenarioResults=@([pscustomobject]@{name='observe-mount-diagnostic-availability';status='PASS';assertionPassCount=4;assertionFailCount=0;errors=@()})}
        $final=New-KmcRuntimeResultV2 -Request $v2Request -ValidatedGameResult $game -StartedAtUtc ([DateTimeOffset]::UtcNow) -ModsRestored $true -BaselineImmutable $true -WorkingRestored $true -SaveWriteAllowlistPassed $true -RestoredSaveInventoryDigest ('c'*64) -GameResultSha256 ('d'*64)
        Assert-Test ([int]$final.subscenarioTotal -eq 1 -and [int]$final.subscenarioPassCount -eq 1 -and [int]$final.subscenarioFailCount -eq 0) 'final result copied untrusted aggregate subscenario totals'
        Assert-Test ([int]$final.assertionPassCount -eq 4 -and [int]$final.assertionFailCount -eq 0 -and [string]$final.status -ceq 'PASS') 'final result did not recompute assertion totals and status'
        Assert-Test ([string]$final.evidenceManifestSha256 -ceq $recomputeManifestHash) 'final result did not echo the structurally validated game-result evidence manifest hash'
    }

    Invoke-HarnessTest 'schema-v2 fallback creates and binds a validated orchestration artifact manifest' {
        $fallbackEvidence = Join-Path $runtimeEvidenceTestRoot 'fallback-evidence'
        $fallbackRequest=[pscustomobject]@{runId='fallback-test';scenario='fixture-intake';branch='codex/mounted-combat-feasibility';commit=('0'*40);productVersion='0.1.0-phase2a-dev.12';dllSha256=('a'*64);dllMvid=[Guid]::Empty.ToString();transactionToken=('b'*64);evidenceRoot=$fallbackEvidence;fixture=[ordered]@{baseline=[ordered]@{};working=[ordered]@{};writeAuthorization=[ordered]@{}}}
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
        productVersion = '0.1.0-phase2a-dev.12'; dllSha256 = ('ab' * 32)
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

    Invoke-HarnessTest 'runtime request schema accepts every exact native lifecycle row' {
        foreach ($nativeRow in @(
            'native-save-clean-dismount',
            'native-area-clean-dismount',
            'native-mode-transition-cleanup',
            'presentation-residue-and-uninstall-safety')) {
            $v2Request.scenario = $nativeRow
            $v2Request.runId = 'schema-v2-' + $nativeRow
            $v2Request.evidenceRoot = Join-Path $runtimeEvidenceTestRoot $v2Request.runId
            Write-KmcJsonAtomic $v2RequestPath $v2Request
            & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $v2RequestPath
        }
        $v2Request.scenario = 'mounted-pair-create-and-clear'
        $v2Request.runId = 'schema-v2-test'
        $v2Request.evidenceRoot = Join-Path $runtimeEvidenceTestRoot 'schema-v2-test'
        Write-KmcJsonAtomic $v2RequestPath $v2Request
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
        baselineLoadRequestCount=0;workingLoadRequestCount=1;workingSaveRequestCount=0;suppressedWorkingSaveRequestCount=0;unauthorizedLoadRequestCount=0;unauthorizedSaveRequestCount=0
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

    $boundaryRow = 'mounted-pair-load-safety'
    $boundaryRequest = [pscustomobject][ordered]@{
        schemaVersion=2;runId='boundary-individual-test';scenario=$boundaryRow;branch=$v2Request.branch;commit=$v2Request.commit
        productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
        transactionToken=$v2Request.transactionToken;evidenceRoot=(Join-Path $runtimeEvidenceTestRoot 'boundary-individual-test')
        fixture=$v2Fixture
    }
    $boundarySubresult = [pscustomobject][ordered]@{
        name=$boundaryRow;status='PASS';assertionPassCount=12;assertionFailCount=0;errors=@()
    }
    $boundaryGameAggregates = [pscustomobject][ordered]@{
        workingLoadRequestCount=2;workingSaveRequestCount=0;suppressedWorkingSaveRequestCount=0;unauthorizedLoadRequestCount=0
        unauthorizedSaveRequestCount=0;baselineLoadRequestCount=0
    }

    Invoke-HarnessTest 'PASS boundary validator accepts exact individual load evidence' {
        $records = New-TestBoundaryPassRecords $boundaryRequest @($boundaryRow)
        [void](Write-TestBoundaryEvidence $boundaryRequest.evidenceRoot $boundaryRequest $records)
        $manifest = Read-KmcJson (Join-Path $boundaryRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcBoundaryScenarioEvidence -Request $boundaryRequest -Manifest $manifest -Status 'PASS' `
            -SubscenarioResults @($boundarySubresult) -GameResult $boundaryGameAggregates
    }
    Invoke-HarnessTest 'PASS native lifecycle boundary rows require independent delivery and restoration evidence' {
        foreach ($nativeRow in @(Get-KmcNativeLifecycleBoundaryRuntimeRows)) {
            $nativeRequest = [pscustomobject][ordered]@{
                schemaVersion=2;runId=('native-boundary-' + $nativeRow);scenario=$nativeRow;branch=$v2Request.branch;commit=$v2Request.commit
                productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
                transactionToken=$v2Request.transactionToken;evidenceRoot=(Join-Path $runtimeEvidenceTestRoot ('native-boundary-' + $nativeRow))
                fixture=$v2Fixture
            }
            $nativeSubresult = [pscustomobject][ordered]@{name=$nativeRow;status='PASS';assertionPassCount=12;assertionFailCount=0;errors=@()}
            $nativeAggregates = [pscustomobject][ordered]@{
                workingLoadRequestCount=1;workingSaveRequestCount=0
                suppressedWorkingSaveRequestCount=$(if($nativeRow -ceq 'native-save-clean-dismount'){1}else{0})
                unauthorizedLoadRequestCount=0;unauthorizedSaveRequestCount=0;baselineLoadRequestCount=0
            }
            $nativeRecords = New-TestBoundaryPassRecords $nativeRequest @($nativeRow)
            [void](Write-TestBoundaryEvidence $nativeRequest.evidenceRoot $nativeRequest $nativeRecords)
            $nativeManifest = Read-KmcJson (Join-Path $nativeRequest.evidenceRoot 'runtime-artifacts.json')
            Assert-KmcBoundaryScenarioEvidence -Request $nativeRequest -Manifest $nativeManifest -Status 'PASS' `
                -SubscenarioResults @($nativeSubresult) -GameResult $nativeAggregates

            $terminal = @($nativeRecords | Where-Object { [string]$_.phase -ceq 'row-result' })[0]
            $terminal.nativeLifecycle.deliveries[0].cleanupSucceeded = $false
            Assert-TestBoundaryEvidenceRejected $nativeRequest $nativeRecords @($nativeSubresult) `
                "native boundary accepted a failed native cleanup delivery: $nativeRow"
        }
    }
    Invoke-HarnessTest 'PASS boundary scenario rejects missing and unmanifested evidence' {
        $records = New-TestBoundaryPassRecords $boundaryRequest @($boundaryRow)
        $path = Join-Path $boundaryRequest.evidenceRoot 'boundary-scenario-evidence.jsonl'
        [void](Write-TestBoundaryEvidence $boundaryRequest.evidenceRoot $boundaryRequest $records)
        [IO.File]::Delete($path)
        $manifestHash = New-TestArtifactManifest -EvidenceRoot $boundaryRequest.evidenceRoot -RunId $boundaryRequest.runId -Scenario $boundaryRequest.scenario
        $manifest = Read-KmcJson (Join-Path $boundaryRequest.evidenceRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcBoundaryScenarioEvidence -Request $boundaryRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($boundarySubresult) } catch { $threw = $true }
        Assert-Test $threw 'PASS boundary scenario accepted missing structured evidence'
        $failThrew = $false
        try { Assert-KmcBoundaryScenarioEvidence -Request $boundaryRequest -Manifest $manifest -Status 'FAIL' } catch { $failThrew = $true }
        Assert-Test $failThrew 'FAIL boundary scenario accepted missing structured evidence'

        [void](Write-TestBoundaryEvidence $boundaryRequest.evidenceRoot $boundaryRequest $records -OmitManifestRecord)
        $manifest = Read-KmcJson (Join-Path $boundaryRequest.evidenceRoot 'runtime-artifacts.json')
        $directThrew = $false
        try { Assert-KmcBoundaryScenarioEvidence -Request $boundaryRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($boundarySubresult) } catch { $directThrew = $true }
        $orchestrationThrew = $false
        try { Get-KmcValidatedOrchestrationArtifactManifestHash $boundaryRequest | Out-Null } catch { $orchestrationThrew = $true }
        Assert-Test ($directThrew -and $orchestrationThrew) 'boundary evidence existed outside the manifest without failing both validators'
    }
    Invoke-HarnessTest 'boundary manifest identity rejects the wrong artifact kind' {
        $records = New-TestBoundaryPassRecords $boundaryRequest @($boundaryRow)
        [void](Write-TestBoundaryEvidence $boundaryRequest.evidenceRoot $boundaryRequest $records -ManifestKind 'scenario-evidence')
        $manifest = Read-KmcJson (Join-Path $boundaryRequest.evidenceRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcBoundaryScenarioEvidence -Request $boundaryRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($boundarySubresult) } catch { $threw = $true }
        Assert-Test $threw 'boundary JSONL under the generic scenario-evidence kind passed validation'
    }
    Invoke-HarnessTest 'boundary validator rejects malformed and duplicate-property JSONL' {
        $path = Join-Path $boundaryRequest.evidenceRoot 'boundary-scenario-evidence.jsonl'
        [IO.File]::WriteAllText($path, '{' + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
        $artifact = [ordered]@{relativePath='boundary-scenario-evidence.jsonl';kind='boundary-evidence';length=(Get-Item $path).Length;sha256=(Get-KmcSha256 $path)}
        [void](New-TestArtifactManifest $boundaryRequest.evidenceRoot $boundaryRequest.runId $boundaryRequest.scenario @($artifact))
        $manifest = Read-KmcJson (Join-Path $boundaryRequest.evidenceRoot 'runtime-artifacts.json')
        $malformedThrew = $false
        try { Assert-KmcBoundaryScenarioEvidence -Request $boundaryRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($boundarySubresult) } catch { $malformedThrew = $true }
        Assert-Test $malformedThrew 'malformed boundary JSONL passed validation'

        $records = New-TestBoundaryPassRecords $boundaryRequest @($boundaryRow)
        $lines = @($records | ForEach-Object { $_ | ConvertTo-Json -Compress -Depth 15 })
        $lines[0] = $lines[0].Replace('"phase":"row-start"','"phase":"row-start","phase":"row-start"')
        [IO.File]::WriteAllText($path, ($lines -join [Environment]::NewLine) + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
        $artifact.length = (Get-Item $path).Length; $artifact.sha256 = Get-KmcSha256 $path
        [void](New-TestArtifactManifest $boundaryRequest.evidenceRoot $boundaryRequest.runId $boundaryRequest.scenario @($artifact))
        $manifest = Read-KmcJson (Join-Path $boundaryRequest.evidenceRoot 'runtime-artifacts.json')
        $duplicateThrew = $false
        try { Assert-KmcBoundaryScenarioEvidence -Request $boundaryRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($boundarySubresult) } catch { $duplicateThrew = $true }
        Assert-Test $duplicateThrew 'duplicate boundary JSON object property passed validation'
    }
    Invoke-HarnessTest 'boundary validator rejects identity type sequence and exact-property mutations' {
        foreach ($mutation in @('sequence','row-index','schema-type','artifact-kind','dll-hash','working-type','extra-property')) {
            $records = New-TestBoundaryPassRecords $boundaryRequest @($boundaryRow)
            switch ($mutation) {
                'sequence' { $records[1].sequence = 9 }
                'row-index' { $records[1].rowIndex = 1 }
                'schema-type' { $records[0].schemaVersion = 1.5 }
                'artifact-kind' { $records[0].artifactKind = 'self-reported-pass' }
                'dll-hash' { $records[0].dllSha256 = ('0' * 64) }
                'working-type' { $records[0].workingIdentity.observedLength = '101' }
                'extra-property' { $records[0].Add('untrustedPass',$true) }
            }
            Assert-TestBoundaryEvidenceRejected $boundaryRequest $records @($boundarySubresult) "boundary evidence accepted identity/shape mutation $mutation"
        }
    }
    Invoke-HarnessTest 'PASS boundary validator rejects self-reported semantic mutations' {
        foreach ($mutation in @('mounted-state','native-before-dispatch','stock-save','missing-load-dispatch','auth-delta',
            'auth-absolute','forbidden-auth-history','loading-observed','cleanup-residue','fresh-world','fresh-relationship',
            'selection-identity','global-component','working-identity','descriptor','predispatch-identity','identity-source',
            'phase-order','subresult')) {
            $records = New-TestBoundaryPassRecords $boundaryRequest @($boundaryRow)
            $subresult = [pscustomobject][ordered]@{name=$boundaryRow;status='PASS';assertionPassCount=12;assertionFailCount=0;errors=@()}
            switch ($mutation) {
                'mounted-state' { @($records | Where-Object phase -ceq 'mounted')[0].relationship.state = 'Unmounted' }
                'native-before-dispatch' { @($records | Where-Object phase -ceq 'pre-boundary')[0].triggerScope.nativeDeliveryObserved = $true }
                'stock-save' { @($records | Where-Object phase -ceq 'row-result')[0].triggerScope.stockSaveRoutineInvoked = $true }
                'missing-load-dispatch' {
                    $resultRecord = @($records | Where-Object phase -ceq 'row-result')[0]
                    $resultRecord.triggerScope.nativeDeliveryObserved = $false
                    $resultRecord.triggerScope.realWorkingLoadDispatched = $false
                }
                'auth-delta' {
                    $resultRecord = @($records | Where-Object phase -ceq 'row-result')[0]
                    $resultRecord.authorization.authorizedLoadsAfter = $resultRecord.authorization.authorizedLoadsBefore
                    $resultRecord.authorization.authorizedLoadsDelta = 0
                }
                'auth-absolute' {
                    $records[0].authorization.authorizedLoadsBefore = 99
                    $records[0].authorization.authorizedLoadsAfter = 99
                }
                'forbidden-auth-history' {
                    $records[0].authorization.authorizedWritesBefore = 1
                    $records[0].authorization.authorizedWritesAfter = 1
                }
                'loading-observed' { @($records | Where-Object phase -ceq 'loading-stop')[0].loading.observed = $false }
                'cleanup-residue' { @($records | Where-Object phase -ceq 'row-result')[0].cleanup.movementAuthorityResidual = $true }
                'fresh-world' { @($records | Where-Object phase -ceq 'fresh-world')[0].freshWorld.relationshipClean = $false }
                'fresh-relationship' { @($records | Where-Object phase -ceq 'fresh-world')[0].relationship.riderStockAgentEnabled = $false }
                'selection-identity' { @($records | Where-Object phase -ceq 'fresh-world')[0].relationship.selectedUnitIds = @('different-unit') }
                'global-component' { @($records | Where-Object phase -ceq 'fresh-world')[0].relationship.kmcRiderMovementAgentComponentCount = 1 }
                'working-identity' { @($records | Where-Object phase -ceq 'row-result')[0].workingIdentity.observedSha256 = ('3' * 64) }
                'descriptor' { @($records | Where-Object phase -ceq 'cleanup-latch')[0].workingIdentity.descriptorVerified = $false }
                'predispatch-identity' { @($records | Where-Object phase -ceq 'cleanup-latch')[0].workingIdentity.preDispatchLength = 102 }
                'identity-source' { @($records | Where-Object phase -ceq 'cleanup-latch')[0].workingIdentity.observedSource = 'immediate-post-dispatch' }
                'phase-order' {
                    $temporary = $records[4]; $records[4] = $records[5]; $records[5] = $temporary
                    for ($index=0;$index -lt $records.Count;$index++) { $records[$index].sequence=$index; $records[$index].frame=$index+1 }
                }
                'subresult' { $subresult.assertionPassCount = 11 }
            }
            Assert-TestBoundaryEvidenceRejected $boundaryRequest $records @($subresult) "boundary evidence accepted self-reported semantic mutation $mutation"
        }
    }
    Invoke-HarnessTest 'boundary live terminal identity is opt-in and final aggregates reconcile' {
        $liveRoot = Join-Path $runtimeEvidenceTestRoot 'boundary-live-identity-test'
        New-Item -ItemType Directory -Path $liveRoot -Force | Out-Null
        $livePath = Join-Path $liveRoot ([string]$v2Fixture.working.fileName)
        [IO.File]::WriteAllText($livePath, 'synthetic terminal Working bytes', (New-Object Text.UTF8Encoding($false)))
        $liveItem = Get-Item -LiteralPath $livePath -Force
        $liveSha256 = Get-KmcSha256 $livePath
        $liveRequest = [pscustomobject][ordered]@{
            schemaVersion=2;runId='boundary-live-identity-test';scenario=$boundaryRow;branch=$v2Request.branch;commit=$v2Request.commit
            productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
            transactionToken=$v2Request.transactionToken;evidenceRoot=$liveRoot;fixture=$v2Fixture
        }
        $records = New-TestBoundaryPassRecords $liveRequest @($boundaryRow)
        foreach ($record in $records) {
            $record.workingIdentity.path = $livePath
            if ($record.workingIdentity.descriptorVerified -eq $true) {
                $record.workingIdentity.descriptorPath = $livePath
            }
        }
        foreach ($record in @($records | Where-Object { [string]$_.phase -cin @('loading-stop','fresh-world','row-result') })) {
            $record.workingIdentity.observedLength = [long]$liveItem.Length
            $record.workingIdentity.observedLastWriteTimeUtcTicks = [long]$liveItem.LastWriteTimeUtc.Ticks
            $record.workingIdentity.observedSha256 = [string]$liveSha256
            $record.workingIdentity.matchesPostInitialLoad = $false
        }
        [void](Write-TestBoundaryEvidence $liveRoot $liveRequest $records)
        $manifest = Read-KmcJson (Join-Path $liveRoot 'runtime-artifacts.json')
        Assert-KmcBoundaryScenarioEvidence -Request $liveRequest -Manifest $manifest -Status 'PASS' `
            -SubscenarioResults @($boundarySubresult) -GameResult $boundaryGameAggregates -VerifyLiveWorkingIdentity `
            -ExpectedLiveWorkingPath $livePath

        $pathBindingThrew = $false
        try {
            Assert-KmcBoundaryScenarioEvidence -Request $liveRequest -Manifest $manifest -Status 'PASS' `
                -SubscenarioResults @($boundarySubresult) -GameResult $boundaryGameAggregates -VerifyLiveWorkingIdentity `
                -ExpectedLiveWorkingPath ($livePath + '.wrong')
        } catch { $pathBindingThrew = $true }
        Assert-Test $pathBindingThrew 'opt-in live Working validation trusted the self-reported path over the transaction-owned path'

        $badAggregates = $boundaryGameAggregates | ConvertTo-Json | ConvertFrom-Json
        $badAggregates.workingLoadRequestCount = 99
        $aggregateThrew = $false
        try {
            Assert-KmcBoundaryScenarioEvidence -Request $liveRequest -Manifest $manifest -Status 'PASS' `
                -SubscenarioResults @($boundarySubresult) -GameResult $badAggregates
        } catch { $aggregateThrew = $true }
        Assert-Test $aggregateThrew 'boundary evidence did not reconcile terminal authorization with the game aggregate'

        [IO.File]::AppendAllText($livePath, '-mutated', (New-Object Text.UTF8Encoding($false)))
        Assert-KmcBoundaryScenarioEvidence -Request $liveRequest -Manifest $manifest -Status 'PASS' `
            -SubscenarioResults @($boundarySubresult) -GameResult $boundaryGameAggregates
        $liveThrew = $false
        try {
            Assert-KmcBoundaryScenarioEvidence -Request $liveRequest -Manifest $manifest -Status 'PASS' `
                -SubscenarioResults @($boundarySubresult) -GameResult $boundaryGameAggregates -VerifyLiveWorkingIdentity `
                -ExpectedLiveWorkingPath $livePath
        } catch { $liveThrew = $true }
        Assert-Test $liveThrew 'opt-in live terminal Working validation accepted changed bytes'
        $launcherSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\Invoke-KingmakerRuntimeScenario.ps1')
        $postRestoreValidatorSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\Test-RuntimeResult.ps1')
        Assert-Test ([regex]::Matches($launcherSource, '(?<![A-Za-z])-VerifyLiveWorkingIdentity(?![A-Za-z])').Count -eq 1) `
            'live Working verification was not confined to one guarded launcher call site'
        Assert-Test ($launcherSource -cmatch '\$lockedWorkingPath=\[IO\.Path\]::GetFullPath\(\[string\]\$lockedPair\.working\.path\)' -and
            $launcherSource -cmatch '-ExpectedLiveWorkingPath \$lockedWorkingPath') `
            'live Working verification was not bound to the exact transaction-owned Working path'
        Assert-Test ($postRestoreValidatorSource -cnotmatch 'VerifyLiveWorkingIdentity') `
            'post-restoration runtime-result replay was coupled to live Working identity'
    }
    Invoke-HarnessTest 'boundary-suite accepts exact five-row evidence and rejects reordered rows/subresults' {
        $suiteRows = @(Get-KmcBoundaryRuntimeRows)
        $suiteRequest = [pscustomobject][ordered]@{
            schemaVersion=2;runId='boundary-suite-test';scenario='boundary-suite';branch=$v2Request.branch;commit=$v2Request.commit
            productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
            transactionToken=$v2Request.transactionToken;evidenceRoot=(Join-Path $runtimeEvidenceTestRoot 'boundary-suite-test')
            fixture=$v2Fixture
        }
        $suiteRecords = New-TestBoundaryPassRecords $suiteRequest $suiteRows
        $suiteSubresults = @($suiteRows | ForEach-Object { [pscustomobject][ordered]@{name=$_;status='PASS';assertionPassCount=12;assertionFailCount=0;errors=@()} })
        [void](Write-TestBoundaryEvidence $suiteRequest.evidenceRoot $suiteRequest $suiteRecords)
        $manifest = Read-KmcJson (Join-Path $suiteRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcBoundaryScenarioEvidence -Request $suiteRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults $suiteSubresults

        $reordered = @(
            @($suiteRecords | Where-Object row -ceq $suiteRows[1])
            @($suiteRecords | Where-Object row -ceq $suiteRows[0])
            @($suiteRecords | Where-Object { [Array]::IndexOf($suiteRows,[string]$_.row) -ge 2 })
        )
        for ($index=0;$index -lt $reordered.Count;$index++) { $reordered[$index].sequence=$index; $reordered[$index].frame=$index+1 }
        Assert-TestBoundaryEvidenceRejected $suiteRequest $reordered $suiteSubresults 'boundary-suite accepted reordered row evidence'

        $reorderedSubresults = @($suiteSubresults)
        $temporary = $reorderedSubresults[0]; $reorderedSubresults[0]=$reorderedSubresults[1]; $reorderedSubresults[1]=$temporary
        Assert-TestBoundaryEvidenceRejected $suiteRequest $suiteRecords $reorderedSubresults 'boundary-suite accepted reordered subscenario results'
    }
    Invoke-HarnessTest 'boundary FAIL evidence permits one executed failure then exact suppressed rows only' {
        $rows = @(Get-KmcBoundaryRuntimeRows)
        $failureRequest = [pscustomobject][ordered]@{
            schemaVersion=2;runId='boundary-failure-test';scenario='boundary-suite';branch=$v2Request.branch;commit=$v2Request.commit
            productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
            transactionToken=$v2Request.transactionToken;evidenceRoot=(Join-Path $runtimeEvidenceTestRoot 'boundary-failure-test')
            fixture=$v2Fixture
        }
        $failureRecords = New-Object 'Collections.Generic.List[object]'
        $failureRecords.Add((New-TestBoundaryEvidenceRecord $failureRequest $rows[0] 'row-start' 0 0))
        $failed = New-TestBoundaryEvidenceRecord $failureRequest $rows[0] 'row-result' 1 0
        $failed.rowStatus='FAIL';$failed.assertionPassCount=1;$failed.assertionFailCount=1;$failed.recordErrors=@('Synthetic boundary failure.')
        $failed.authorization.unauthorizedWritesAfter=1;$failed.authorization.unauthorizedWritesDelta=1
        $failureRecords.Add($failed)
        $subresults = New-Object 'Collections.Generic.List[object]'
        $subresults.Add([pscustomobject][ordered]@{name=$rows[0];status='FAIL';assertionPassCount=1;assertionFailCount=1;errors=@('Synthetic boundary failure.')})
        for ($index=1;$index -lt $rows.Count;$index++) {
            $record = New-TestBoundaryEvidenceRecord $failureRequest $rows[$index] 'row-result' ($index+1) $index -Suppressed
            $record.authorization.unauthorizedWritesBefore=1;$record.authorization.unauthorizedWritesAfter=1
            $failureRecords.Add($record)
            $subresults.Add([pscustomobject][ordered]@{name=$rows[$index];status='FAIL';assertionPassCount=0;assertionFailCount=1;errors=@('Suppressed after a prior boundary failure.')})
        }
        [void](Write-TestBoundaryEvidence $failureRequest.evidenceRoot $failureRequest $failureRecords.ToArray())
        $manifest = Read-KmcJson (Join-Path $failureRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcBoundaryScenarioEvidence -Request $failureRequest -Manifest $manifest -Status 'FAIL' -SubscenarioResults $subresults.ToArray()

        $failureRecords[2].executed=$true;$failureRecords[2].suppressed=$false
        Assert-TestBoundaryEvidenceRejected $failureRequest $failureRecords.ToArray() $subresults.ToArray() 'boundary-suite accepted an executed row after its first safety failure'
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
    Invoke-HarnessTest 'PASS movement validator carries only aligned LateUpdate position recovery' {
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),(New-TestMovementRowRecord $movementRequest $movementRow 1))
        $newAlignedPositionRecoveryCarry = {
            $value = New-TestMovementTelemetryRecord $movementRequest $movementRow 0
            $value.synchronizationPhase = 'LateUpdate'
            $value.latestCurrentAuthoritativeAnchorY = 2.05
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
            $value.latestAuthoritativePositionDeltaWorldUnits = 0.05
            $value.latestViewCurrentPositionResidualWorldUnits = 0.0
            $value.latestEntityRawCurrentPositionResidualWorldUnits = 0.05
            $value.latestEntityPreviousAuthoritativePositionResidualWorldUnits = 0.0
            $value.latestEntityPhaseAdjustedPositionResidualWorldUnits = 0.05
            $value.latestEntityRawPositionLagBoundWorldUnits = 0.05
            $value.latestEntityRawPositionLagExcessWorldUnits = 0.0
            $value.latestEntityPositionAuthorityAgeSteps = 0
            $value.latestPositionPhaseLagObserved = $false
            $value.latestPositionPhaseLagPermitted = $false
            $value.latestPositionPhaseLagViolation = $false
            $value.latestPositionRecoveryRequiredBeforeSample = $true
            $value.latestPositionRecoveryUpdateObserved = $false
            $value.latestPositionRecoverySatisfied = $false
            $value.latestPositionRecoveryViolation = $false
            $value.latestPositionRecoveryPendingAfterSample = $true
            $value.latestPositionStationaryAuthority = $false
            $value.latestStationaryPositionCorrectionViolation = $false
            $value.preCorrectionPositionResidualWorldUnits = 0.05
            $value.preCorrectionRawCurrentPositionResidualWorldUnits = 0.05
            $value.preCorrectionViewCurrentPositionResidualWorldUnits = 0.0
            $value.maximumLateUpdatePreCorrectionPositionResidualWorldUnits = 0.05
            $value.maximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits = 0.05
            $value.positionPhaseLagRecoveryRequiredRawCount = 0
            $value.positionPhaseLagRecoveryUpdateRawCount = 0
            $value.positionPhaseLagRecoverySatisfiedRawCount = 0
            $value.positionPhaseLagRecoveryRequiredEffectiveCount = 0
            $value.positionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount = 0
            $value.positionPhaseLagRecoverySatisfiedEffectiveCount = 0
            $value.outstandingPositionPhaseLagRecoveryCount = 1

            # The same telemetry sample has no yaw obligation; changing the
            # shared phase to LateUpdate must not manufacture one.
            $value.latestRecoveryRequiredBeforeSample = $false
            $value.latestRecoveryUpdateObserved = $false
            $value.latestRecoverySatisfied = $false
            $value.latestRecoveryViolation = $false
            $value.latestRecoveryPendingAfterSample = $false
            return $value
        }

        $telemetry = & $newAlignedPositionRecoveryCarry
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult)

        foreach ($mutation in @('fake-update','fake-satisfied','false-discharge','wrong-phase','visible-carry',
            'raw-second-lag','stale-update','duplicated-raw-required','outstanding-counter')) {
            $telemetry = & $newAlignedPositionRecoveryCarry
            switch ($mutation) {
                'fake-update' { $telemetry.latestPositionRecoveryUpdateObserved = $true }
                'fake-satisfied' { $telemetry.latestPositionRecoverySatisfied = $true }
                'false-discharge' { $telemetry.latestPositionRecoveryPendingAfterSample = $false }
                'wrong-phase' {
                    $telemetry.synchronizationPhase = 'InitialConfiguration'
                    foreach ($name in @('latestPreviousAuthoritativePositionSequence','latestPreviousAuthoritativeAnchorX',
                        'latestPreviousAuthoritativeAnchorY','latestPreviousAuthoritativeAnchorZ',
                        'latestPreviousAuthoritativePositionFrame','latestPreviousAuthoritativePositionPhase',
                        'latestEntityPreviousAuthoritativePositionResidualWorldUnits')) { $telemetry.$name = $null }
                    $telemetry.latestPreviousAuthoritativePositionReferenceKind = 'none'
                    $telemetry.latestPreviousAuthoritativePositionSameFrame = $false
                    $telemetry.latestPreviousAuthoritativePositionReferenceEligible = $false
                    $telemetry.latestPositionStationaryAuthority = $false
                    $telemetry.latestStationaryAuthority = $false
                }
                'visible-carry' {
                    $telemetry.latestViewCurrentPositionResidualWorldUnits = 0.100001
                    $telemetry.preCorrectionPositionResidualWorldUnits = 0.100001
                    $telemetry.preCorrectionRawCurrentPositionResidualWorldUnits = 0.100001
                    $telemetry.preCorrectionViewCurrentPositionResidualWorldUnits = 0.100001
                }
                'raw-second-lag' {
                    $telemetry.latestCurrentAuthoritativeAnchorY = 2.2
                    $telemetry.latestAuthoritativePositionDeltaWorldUnits = 0.2
                    $telemetry.latestEntityRawCurrentPositionResidualWorldUnits = 0.2
                    $telemetry.latestEntityPhaseAdjustedPositionResidualWorldUnits = 0.2
                    $telemetry.latestEntityRawPositionLagBoundWorldUnits = 0.2
                    $telemetry.latestEntityPositionAuthorityAgeSteps = 1
                    $telemetry.latestPositionPhaseLagObserved = $true
                    $telemetry.latestPositionPhaseLagViolation = $true
                    $telemetry.preCorrectionPositionResidualWorldUnits = 0.2
                    $telemetry.preCorrectionRawCurrentPositionResidualWorldUnits = 0.2
                }
                'stale-update' {
                    $telemetry.synchronizationPhase = 'Update'
                    foreach ($name in @('latestPreviousAuthoritativePositionSequence','latestPreviousAuthoritativeAnchorX',
                        'latestPreviousAuthoritativeAnchorY','latestPreviousAuthoritativeAnchorZ',
                        'latestPreviousAuthoritativePositionFrame','latestPreviousAuthoritativePositionPhase',
                        'latestEntityPreviousAuthoritativePositionResidualWorldUnits')) { $telemetry.$name = $null }
                    $telemetry.latestPreviousAuthoritativePositionReferenceKind = 'none'
                    $telemetry.latestPreviousAuthoritativePositionSameFrame = $false
                    $telemetry.latestPreviousAuthoritativePositionReferenceEligible = $false
                    $telemetry.latestCurrentAuthoritativeAnchorY = 2.2
                    $telemetry.latestAuthoritativePositionDeltaWorldUnits = 0.2
                    $telemetry.latestEntityRawCurrentPositionResidualWorldUnits = 0.2
                    $telemetry.latestEntityPhaseAdjustedPositionResidualWorldUnits = 0.2
                    $telemetry.latestEntityRawPositionLagBoundWorldUnits = 0.2
                    $telemetry.latestEntityPositionAuthorityAgeSteps = $null
                    $telemetry.latestPositionPhaseLagObserved = $true
                    $telemetry.latestPositionPhaseLagViolation = $true
                    $telemetry.latestPositionRecoveryUpdateObserved = $true
                    $telemetry.latestPositionRecoveryPendingAfterSample = $false
                    $telemetry.preCorrectionPositionResidualWorldUnits = 0.2
                    $telemetry.preCorrectionRawCurrentPositionResidualWorldUnits = 0.2
                }
                'duplicated-raw-required' { $telemetry.positionPhaseLagRecoveryRequiredRawCount = 1 }
                'outstanding-counter' { $telemetry.outstandingPositionPhaseLagRecoveryCount = 0 }
            }
            [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
            $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
            $threw=$false
            try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
            Assert-Test $threw "movement telemetry accepted unsafe aligned position-recovery carry mutation $mutation"
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
    Invoke-HarnessTest 'PASS movement validator carries only aligned LateUpdate yaw recovery' {
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),(New-TestMovementRowRecord $movementRequest $movementRow 1))
        $newAlignedYawRecoveryCarry = {
            $value = New-TestMovementTelemetryRecord $movementRequest $movementRow 0
            $value.synchronizationPhase = 'LateUpdate'

            # The same telemetry sample has no position obligation; changing
            # the shared phase to LateUpdate must not manufacture one.
            $value.latestPositionRecoveryRequiredBeforeSample = $false
            $value.latestPositionRecoveryUpdateObserved = $false
            $value.latestPositionRecoverySatisfied = $false
            $value.latestPositionRecoveryViolation = $false
            $value.latestPositionRecoveryPendingAfterSample = $false

            $value.latestAuthoritativeYawSequence = 2
            $value.latestPreviousAuthoritativeYawSequence = 1
            $value.latestPreviousAuthoritativeYawDegrees = 0.0
            $value.latestPreviousAuthoritativeFrame = 12
            $value.latestPreviousAuthoritativePhase = 'Update'
            $value.latestPreviousAuthoritativeReferenceKind = 'same-frame-update'
            $value.latestPreviousAuthoritativeSameFrame = $true
            $value.latestPreviousAuthoritativeReferenceEligible = $true
            $value.latestAuthoritativeYawDeltaDegrees = 8.0
            $value.latestViewCurrentYawResidualDegrees = 0.0
            $value.latestEntityRawCurrentYawResidualDegrees = 0.05
            $value.latestEntityPreviousAuthoritativeYawResidualDegrees = 0.0
            $value.latestEntityPhaseAdjustedYawResidualDegrees = 0.05
            $value.latestEntityRawLagBoundDegrees = 8.0
            $value.latestEntityRawLagExcessDegrees = 0.0
            $value.latestEntityYawAuthorityAgeSteps = 0
            $value.latestPhaseLagObserved = $false
            $value.latestPhaseLagPermitted = $false
            $value.latestPhaseLagViolation = $false
            $value.latestRecoveryRequiredBeforeSample = $true
            $value.latestRecoveryUpdateObserved = $false
            $value.latestRecoverySatisfied = $false
            $value.latestRecoveryViolation = $false
            $value.latestRecoveryPendingAfterSample = $true
            $value.latestStationaryAuthority = $false
            $value.latestStationaryYawCorrectionViolation = $false
            $value.preCorrectionRotationResidualDegrees = 0.05
            $value.maximumLateUpdatePreCorrectionRotationResidualDegrees = 0.05
            $value.maximumCalibratedEntityPhaseAdjustedYawResidualDegrees = 0.05
            $value.phaseLagRecoveryRequiredCount = 0
            $value.phaseLagRecoveryUpdateCount = 0
            $value.phaseLagRecoverySatisfiedCount = 0
            $value.phaseLagRecoveryRequiredRawCount = 0
            $value.phaseLagRecoveryUpdateRawCount = 0
            $value.phaseLagRecoverySatisfiedRawCount = 0
            $value.phaseLagRecoveryRequiredEffectiveCount = 0
            $value.phaseLagRecoveryUpdateOrBoundaryEffectiveCount = 0
            $value.phaseLagRecoverySatisfiedEffectiveCount = 0
            $value.outstandingPhaseLagRecoveryCount = 1
            return $value
        }

        $telemetry = & $newAlignedYawRecoveryCarry
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult)

        foreach ($mutation in @('fake-update','fake-satisfied','false-discharge','wrong-phase','visible-carry',
            'raw-second-lag','stale-update','duplicated-raw-required','outstanding-counter')) {
            $telemetry = & $newAlignedYawRecoveryCarry
            switch ($mutation) {
                'fake-update' { $telemetry.latestRecoveryUpdateObserved = $true }
                'fake-satisfied' { $telemetry.latestRecoverySatisfied = $true }
                'false-discharge' { $telemetry.latestRecoveryPendingAfterSample = $false }
                'wrong-phase' {
                    $telemetry.synchronizationPhase = 'InitialConfiguration'
                    foreach ($name in @('latestPreviousAuthoritativeYawSequence','latestPreviousAuthoritativeYawDegrees',
                        'latestPreviousAuthoritativeFrame','latestPreviousAuthoritativePhase',
                        'latestEntityPreviousAuthoritativeYawResidualDegrees')) { $telemetry.$name = $null }
                    $telemetry.latestPreviousAuthoritativeReferenceKind = 'none'
                    $telemetry.latestPreviousAuthoritativeSameFrame = $false
                    $telemetry.latestPreviousAuthoritativeReferenceEligible = $false
                    $telemetry.latestPositionStationaryAuthority = $false
                    $telemetry.latestStationaryAuthority = $false
                }
                'visible-carry' { $telemetry.latestViewCurrentYawResidualDegrees = 0.100001 }
                'raw-second-lag' {
                    $telemetry.latestEntityRawCurrentYawResidualDegrees = 0.2
                    $telemetry.latestEntityPhaseAdjustedYawResidualDegrees = 0.2
                    $telemetry.latestEntityYawAuthorityAgeSteps = 1
                    $telemetry.latestPhaseLagObserved = $true
                    $telemetry.latestPhaseLagViolation = $true
                    $telemetry.preCorrectionRotationResidualDegrees = 0.2
                }
                'stale-update' {
                    $telemetry.synchronizationPhase = 'Update'
                    foreach ($name in @('latestPreviousAuthoritativeYawSequence','latestPreviousAuthoritativeYawDegrees',
                        'latestPreviousAuthoritativeFrame','latestPreviousAuthoritativePhase',
                        'latestEntityPreviousAuthoritativeYawResidualDegrees')) { $telemetry.$name = $null }
                    $telemetry.latestPreviousAuthoritativeReferenceKind = 'none'
                    $telemetry.latestPreviousAuthoritativeSameFrame = $false
                    $telemetry.latestPreviousAuthoritativeReferenceEligible = $false
                    $telemetry.latestEntityRawCurrentYawResidualDegrees = 0.2
                    $telemetry.latestEntityPhaseAdjustedYawResidualDegrees = 0.2
                    $telemetry.latestEntityYawAuthorityAgeSteps = $null
                    $telemetry.latestPhaseLagObserved = $true
                    $telemetry.latestPhaseLagViolation = $true
                    $telemetry.latestRecoveryUpdateObserved = $true
                    $telemetry.latestRecoveryPendingAfterSample = $false
                    $telemetry.preCorrectionRotationResidualDegrees = 0.2
                }
                'duplicated-raw-required' { $telemetry.phaseLagRecoveryRequiredRawCount = 1 }
                'outstanding-counter' { $telemetry.outstandingPhaseLagRecoveryCount = 0 }
            }
            [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
            $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
            $threw=$false
            try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw=$true }
            Assert-Test $threw "movement telemetry accepted unsafe aligned yaw-recovery carry mutation $mutation"
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
    Invoke-HarnessTest 'PASS movement validator permits only corrected InitialConfiguration residuals before calibration' {
        $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),(New-TestMovementRowRecord $movementRequest $movementRow 1))
        $newCorrectedInitialConfiguration = {
            $value = New-TestMovementTelemetryRecord $movementRequest $movementRow 0
            $value.synchronizationPhase = 'InitialConfiguration'

            $value.latestViewCurrentPositionResidualWorldUnits = 10.0
            $value.latestEntityRawCurrentPositionResidualWorldUnits = 10.0
            $value.latestEntityPhaseAdjustedPositionResidualWorldUnits = 10.0
            $value.latestEntityRawPositionLagBoundWorldUnits = 0.0
            $value.latestEntityRawPositionLagExcessWorldUnits = 10.0
            $value.latestEntityPositionAuthorityAgeSteps = $null
            $value.latestPositionPhaseLagObserved = $false
            $value.latestPositionPhaseLagPermitted = $false
            $value.latestPositionPhaseLagViolation = $false
            $value.latestPositionRecoveryRequiredBeforeSample = $false
            $value.latestPositionRecoveryUpdateObserved = $false
            $value.latestPositionRecoverySatisfied = $false
            $value.latestPositionRecoveryViolation = $false
            $value.latestPositionRecoveryPendingAfterSample = $false
            $value.latestPositionStationaryAuthority = $false
            $value.latestStationaryPositionCorrectionViolation = $false
            $value.preCorrectionPositionResidualWorldUnits = 10.0
            $value.preCorrectionRawCurrentPositionResidualWorldUnits = 10.0
            $value.preCorrectionViewCurrentPositionResidualWorldUnits = 10.0
            $value.postCorrectionPositionResidualWorldUnits = 0.0
            $value.maximumPreCorrectionPositionResidualWorldUnits = 10.0
            $value.maximumPreCorrectionRawCurrentPositionResidualWorldUnits = 10.0
            $value.maximumInitialConfigurationPreCorrectionPositionResidualWorldUnits = 10.0

            $value.latestViewCurrentYawResidualDegrees = 170.0
            $value.latestFullViewCurrentRotationResidualDegrees = 170.0
            $value.latestEntityRawCurrentYawResidualDegrees = 170.0
            $value.latestEntityPhaseAdjustedYawResidualDegrees = 170.0
            $value.latestEntityRawLagBoundDegrees = 0.0
            $value.latestEntityRawLagExcessDegrees = 170.0
            $value.latestEntityYawAuthorityAgeSteps = $null
            $value.latestPhaseLagObserved = $false
            $value.latestPhaseLagPermitted = $false
            $value.latestPhaseLagViolation = $false
            $value.latestRecoveryRequiredBeforeSample = $false
            $value.latestRecoveryUpdateObserved = $false
            $value.latestRecoverySatisfied = $false
            $value.latestRecoveryViolation = $false
            $value.latestRecoveryPendingAfterSample = $false
            $value.latestStationaryAuthority = $false
            $value.latestStationaryYawCorrectionViolation = $false
            $value.preCorrectionRotationResidualDegrees = 170.0
            $value.postCorrectionRotationResidualDegrees = 0.0
            $value.maximumPreCorrectionRotationResidualDegrees = 170.0
            return $value
        }

        $telemetry = & $newCorrectedInitialConfiguration
        [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
        $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult)

        foreach ($mutation in @('uncorrected-position','uncorrected-rotation','calibrated-position','calibrated-rotation')) {
            $telemetry = & $newCorrectedInitialConfiguration
            switch ($mutation) {
                'uncorrected-position' {
                    $telemetry.postCorrectionPositionResidualWorldUnits = 0.100001
                    $telemetry.maximumPostCorrectionPositionResidualWorldUnits = 0.100001
                }
                'uncorrected-rotation' {
                    $telemetry.postCorrectionRotationResidualDegrees = 0.100001
                    $telemetry.maximumPostCorrectionRotationResidualDegrees = 0.100001
                }
                'calibrated-position' {
                    $telemetry.synchronizationPhase = 'Update'
                    $telemetry.latestPositionPhaseLagObserved = $true
                    $telemetry.latestPositionPhaseLagViolation = $true
                    $telemetry.latestPositionStationaryAuthority = $true
                    $telemetry.latestStationaryPositionCorrectionViolation = $true
                }
                'calibrated-rotation' {
                    $telemetry.synchronizationPhase = 'Update'
                    $telemetry.latestPhaseLagObserved = $true
                    $telemetry.latestPhaseLagViolation = $true
                    $telemetry.latestStationaryAuthority = $true
                    $telemetry.latestStationaryYawCorrectionViolation = $true
                }
            }
            [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
            $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
            $threw = $false
            try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) }
            catch { $threw = $true }
            Assert-Test $threw "movement telemetry accepted unsafe InitialConfiguration mutation $mutation"
        }
    }
    Invoke-HarnessTest 'presentation-suite requires exact pose UI camera path and cleanup evidence' {
        $suiteRows = @(Get-KmcPresentationRuntimeRows)
        $suiteRequest = [pscustomobject][ordered]@{
            runId='presentation-suite-evidence-test';scenario='presentation-suite';branch=$v2Request.branch;commit=$v2Request.commit
            productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
            evidenceRoot=(Join-Path $runtimeEvidenceTestRoot 'presentation-suite-evidence-test')
        }
        $telemetry = New-Object 'Collections.Generic.List[object]'
        $scenario = New-Object 'Collections.Generic.List[object]'
        $subresults = New-Object 'Collections.Generic.List[object]'
        $scenarioSequence = 0
        for ($index = 0; $index -lt $suiteRows.Count; $index++) {
            $row = $suiteRows[$index]
            $telemetry.Add((New-TestMovementTelemetryRecord $suiteRequest $row $index))
            if ($row -ceq 'pose-doorway-formation') {
                $scenario.Add((New-TestMovementPathProbeRecord $suiteRequest $row ($scenarioSequence++) 'DoorNear' $false))
                $scenario.Add((New-TestMovementPathProbeRecord $suiteRequest $row ($scenarioSequence++) 'DoorFar' $true))
                $scenario.Add((New-TestMovementPathProbeRecord $suiteRequest $row ($scenarioSequence++) 'DoorNear' $true))
                $scenario.Add((New-TestMovementPathProbeRecord $suiteRequest $row ($scenarioSequence++) 'Generic' $false))
            }
            else {
                $probeCount = switch ($row) {
                    'pose-walk-run' { 2; break }
                    'pose-turn-stop' { 3; break }
                    'camera-follow-and-command-routing' { 1; break }
                    default { 0; break }
                }
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

        $stationarySource = @($scenario.ToArray() | Where-Object { [string]$_.kind -ceq 'movement-row-result' -and [string]$_.row -ceq 'pose-idle' })[0]
        $stationaryRecord = $stationarySource | ConvertTo-Json -Depth 20 -Compress | ConvertFrom-Json
        $stationaryRecord.updateSynchronizationSampleCount = 0
        $stationaryRecord.updateSynchronizationCorrectionCount = 0
        Assert-KmcMovementScenarioRecord $stationaryRecord $suiteRequest ([long]$stationaryRecord.sequence) $suiteRows $true $manifest

        $movingSource = @($scenario.ToArray() | Where-Object { [string]$_.kind -ceq 'movement-row-result' -and [string]$_.row -ceq 'pose-walk-run' })[0]
        $movingRecord = $movingSource | ConvertTo-Json -Depth 20 -Compress | ConvertFrom-Json
        $movingRecord.updateSynchronizationSampleCount = 0
        $movingRecord.updateSynchronizationCorrectionCount = 0
        $threw = $false
        try { Assert-KmcMovementScenarioRecord $movingRecord $suiteRequest ([long]$movingRecord.sequence) $suiteRows $true $manifest } catch { $threw = $true }
        Assert-Test $threw 'PASS moving presentation row accepted LateUpdate-only synchronization coverage'

        $mutationCases = @(
            @('pose-idle','poseMaximumPelvisLocalFrameDeltaWorldUnits',0.150001),
            @('pose-idle','maximumTurnDegrees',1.0),
            @('pose-walk-run','runMovingSampleCount',0),
            @('pose-walk-run','maximumTurnDegrees',0.0),
            @('pose-turn-stop','stopCommandIssuedCount',0),
            @('pose-doorway-formation','formationSelectionNormalized',$false),
            @('pose-equipment-variants','equipmentSets',@()),
            @('ui-selection-portrait-actionbar','uiOverlayRendered',$false),
            @('camera-follow-and-command-routing','cameraBackObserved',$false)
        )
        foreach ($mutation in $mutationCases) {
            $record = @($scenario.ToArray() | Where-Object { [string]$_.kind -ceq 'movement-row-result' -and [string]$_.row -ceq [string]$mutation[0] })[0]
            $property = [string]$mutation[1]
            $original = $record[$property]
            try {
                $record[$property] = $mutation[2]
                $threw = $false
                try { Assert-KmcMovementScenarioRecord $record $suiteRequest ([long]$record.sequence) $suiteRows $true $manifest } catch { $threw = $true }
                Assert-Test $threw "PASS presentation row accepted semantic mutation $($mutation[0])/$property"
            }
            finally { $record[$property] = $original }
        }
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
        Assert-Test ($rowPayloadNames.Count -eq 198 -and $rowProducerNames.Count -eq 209 -and $rowFixtureNames.Count -eq 209 -and
            @($rowProducerNames | Where-Object { [Array]::IndexOf($rowFixtureNames, $_) -lt 0 }).Count -eq 0 -and
            @($rowFixtureNames | Where-Object { [Array]::IndexOf($rowProducerNames, $_) -lt 0 }).Count -eq 0) 'movement row fixture/validator field set is not the exact 209-field producer schema'
        $runtimeSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\KingmakerMountedPairRuntime.cs'))
        Assert-Test ($runtimeSource.Contains('riderAvoidanceWasDisabled = riderStockAgent.AvoidanceDisabled;') -and
            $runtimeSource.Contains('riderStockAgent.AvoidanceDisabled != riderAvoidanceWasDisabled')) 'runtime does not verify its counted avoidance lease restores the captured effective state'
        Assert-Test ($engineSource.Contains('Post-cleanup verification threw')) 'post-cleanup Unity observation exceptions are not recorded as failed evidence'
        Assert-Test ($engineSource.Contains('CompleteRemainingAsNotRun("Further movement was suppressed because post-cleanup verification could not prove restoration."')) 'destroyed-view cleanup failures can still loop instead of bounded finalization'
        Assert-Test ($engineSource.Contains('RiderStateRestored()') -and $engineSource.Contains('MountStateRestored()')) 'destroyed Unity view/agent checks are not fail-closed'
        Assert-Test ($engineSource.Contains('assertions.FailureCount != failuresBeforeCleanupVerification')) 'failed cleanup restoration checks do not suppress the remaining suite rows'
        $cleanupVerifyStart = $engineSource.IndexOf('private void VerifyCleanupAndFinishRow()', [StringComparison]::Ordinal)
        $cleanupVerifyEnd = $engineSource.IndexOf('private void FinishRowAfterCaptures()', $cleanupVerifyStart, [StringComparison]::Ordinal)
        $cleanupVerifyBlock = $engineSource.Substring($cleanupVerifyStart, $cleanupVerifyEnd - $cleanupVerifyStart)
        $postFrameCaptureIndex = $cleanupVerifyBlock.IndexOf('cleanupAfter = CleanupStateEvidence.Capture', [StringComparison]::Ordinal)
        $cleanupAssertionsIndex = $cleanupVerifyBlock.IndexOf('var failuresBeforeCleanupVerification', [StringComparison]::Ordinal)
        Assert-Test ($cleanupVerifyBlock.Contains('if (frameNumber <= cleanupFrame)') -and
            $postFrameCaptureIndex -gt 0 -and $cleanupAssertionsIndex -gt $postFrameCaptureIndex) 'movement row publishes transition-frame cleanup evidence instead of recapturing after deferred Unity destruction'
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
