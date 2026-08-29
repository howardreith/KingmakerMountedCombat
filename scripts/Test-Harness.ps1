[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')

$repoRoot = Get-KmcRepositoryRoot
$currentProductVersion = [string]((Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'version.json') | ConvertFrom-Json).productVersion)
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
        'Manual_3_PERSONAL.zks','Manual_4_KBP.zks','Manual_5_KMG.zks','Auto_1.zks','Quick_1.zks','Quick_3.zks'
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

function New-TestPreparedChainedProtectedSaveEpoch {
    param([Parameter(Mandatory = $true)][string]$Name)
    $parentEpoch = New-TestAuthorizedProtectedSaveEpoch $Name
    $fixture = $parentEpoch.fixture
    $legacyScript = Join-Path $repoRoot 'scripts\runtime\New-KmcProtectedSaveContinuityAuthority.ps1'
    & $legacyScript `
        -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -EpochId $parentEpoch.epochId `
        -ExpectedCurrentQualificationSha256 $parentEpoch.qualificationSha256 `
        -ExpectedBaselineSha256 $fixture.baselineSha256 `
        -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
        -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
        -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath `
        -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
        -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 `
        -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
        -AutoSaveName $parentEpoch.autoName -ExpectedAutoSaveSha256 $parentEpoch.autoSha256 `
        -ExpectedAutoSaveLength $parentEpoch.autoLength -ExpectedAutoSaveLastWriteTimeUtcTicks $parentEpoch.autoTicks `
        -QuickSaveName $parentEpoch.quickName -ExpectedQuickSaveSha256 $parentEpoch.quickSha256 `
        -ExpectedQuickSaveLength $parentEpoch.quickLength -ExpectedQuickSaveLastWriteTimeUtcTicks $parentEpoch.quickTicks `
        -Confirm:$false | Out-Null
    $parentAuthorityPath = Join-Path (Join-Path $fixture.stateRoot 'protected-save-authorities') ($parentEpoch.epochId + '.json')
    $parentAuthority = Get-Item -LiteralPath $parentAuthorityPath -Force
    $parentAuthoritySha256 = Get-KmcSha256 $parentAuthorityPath

    $metadataOnlyPath = Join-Path $fixture.saveRoot 'Quick_3.zks'
    $metadataOnlyPrior = Get-Item -LiteralPath $metadataOnlyPath -Force
    $metadataOnlyPriorLength = [long]$metadataOnlyPrior.Length
    $metadataOnlyPriorTicks = [long]$metadataOnlyPrior.LastWriteTimeUtc.Ticks
    $knownPath = $parentEpoch.quickPath
    $knownPrior = Get-Item -LiteralPath $knownPath -Force
    $knownPriorLength = [long]$knownPrior.Length
    $knownPriorTicks = [long]$knownPrior.LastWriteTimeUtc.Ticks
    [IO.File]::AppendAllText($knownPath, '-explicit-user-known-prior-transition')
    [IO.File]::AppendAllText($metadataOnlyPath, '-explicit-user-metadata-only-transition')
    $knownCurrent = Get-Item -LiteralPath $knownPath -Force
    $metadataOnlyCurrent = Get-Item -LiteralPath $metadataOnlyPath -Force
    $transitions = @(
        [ordered]@{
            priorPath=$parentEpoch.quickName;priorLength=$knownPriorLength
            priorLastWriteTimeUtcTicks=$knownPriorTicks;priorSha256=$parentEpoch.quickSha256
            priorHashStatus='AVAILABLE-PARENT-CONTENT-PIN';currentPath=$parentEpoch.quickName
            currentLength=[long]$knownCurrent.Length;currentLastWriteTimeUtcTicks=[long]$knownCurrent.LastWriteTimeUtc.Ticks
            currentSha256=(Get-KmcSha256 $knownPath);transitionReason='explicit user-attested external Kingmaker activity'
        },
        [ordered]@{
            priorPath='Quick_3.zks';priorLength=$metadataOnlyPriorLength
            priorLastWriteTimeUtcTicks=$metadataOnlyPriorTicks;priorSha256=$null
            priorHashStatus='UNAVAILABLE-SCHEMA-V1-METADATA-ONLY';currentPath='Quick_3.zks'
            currentLength=[long]$metadataOnlyCurrent.Length;currentLastWriteTimeUtcTicks=[long]$metadataOnlyCurrent.LastWriteTimeUtc.Ticks
            currentSha256=(Get-KmcSha256 $metadataOnlyPath);transitionReason='explicit user-attested external Kingmaker activity'
        }
    )
    return [pscustomobject]@{
        fixture=$fixture;parentEpoch=$parentEpoch
        epochId=('chained-epoch-' + $Name)
        parentAuthorityPath=$parentAuthorityPath;parentAuthoritySha256=$parentAuthoritySha256
        parentAuthorityLength=[long]$parentAuthority.Length
        parentAuthorityTicks=[long]$parentAuthority.LastWriteTimeUtc.Ticks
        metadataOnlyPath=$metadataOnlyPath;knownPath=$knownPath
        transitions=$transitions
        transitionsJson=(ConvertTo-Json -InputObject $transitions -Depth 10 -Compress)
    }
}

function New-TestCommittedChainedProtectedSaveEpoch {
    param([Parameter(Mandatory = $true)][string]$Name)
    $prepared = New-TestPreparedChainedProtectedSaveEpoch $Name
    $fixture = $prepared.fixture
    $scriptPath = Join-Path $repoRoot 'scripts\runtime\New-KmcChainedProtectedSaveContinuityAuthority.ps1'
    & $scriptPath `
        -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -EpochId $prepared.epochId `
        -ExpectedCurrentQualificationSha256 $prepared.parentEpoch.qualificationSha256 `
        -ExpectedBaselineSha256 $fixture.baselineSha256 `
        -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
        -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
        -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath `
        -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
        -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 `
        -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
        -ParentAuthorityPath $prepared.parentAuthorityPath `
        -ExpectedParentAuthorityEpochId $prepared.parentEpoch.epochId `
        -ExpectedParentAuthoritySha256 $prepared.parentAuthoritySha256 `
        -AuthorizedTransitionsJson $prepared.transitionsJson -Confirm:$false | Out-Null
    $authorityPath = Join-Path (Join-Path $fixture.stateRoot 'protected-save-authorities') ($prepared.epochId + '.json')
    $authority = Read-KmcJson $authorityPath
    $prepared | Add-Member -NotePropertyName authorityPath -NotePropertyValue $authorityPath
    $prepared | Add-Member -NotePropertyName authoritySha256 -NotePropertyValue (Get-KmcSha256 $authorityPath)
    $prepared | Add-Member -NotePropertyName protectedSavePinSetSha256 -NotePropertyValue ([string]$authority.currentProtectedSavePinsSha256)
    return $prepared
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

function New-TestCombatLifecycleBoundaryExercise {
    param([Parameter(Mandatory = $true)][string]$Row,[Parameter(Mandatory = $true)][bool]$Observed)
    if (-not $Observed) {
        return [ordered]@{observed=$false;row=$Row;actorRole=$null;actorId=$null;invocationPath=$null;relationshipStateAfterBoundary=$null;deliveries=@()}
    }
    $role='pair';$actorId=$null;$path=$null;$state='Unmounted';$deliveries=@()
    switch -CaseSensitive ($Row) {
        'mounted-pair-combat-start-retained' {$path='IPartyCombatHandler.HandlePartyCombatStateChanged(true)';$state='Mounted';$deliveries=@([ordered]@{boundary='CombatStarted';source='IPartyCombatHandler.HandlePartyCombatStateChanged(true)';stateBefore='Mounted';stateAfter='Mounted';cleanupTrigger=$null;cleanupAttempted=$false;cleanupSucceeded=$true})}
        'mounted-pair-combat-end-retained' {$path='IPartyCombatHandler.HandlePartyCombatStateChanged(true/false)';$state='Mounted';$deliveries=@([ordered]@{boundary='CombatStarted';source='IPartyCombatHandler.HandlePartyCombatStateChanged(true)';stateBefore='Mounted';stateAfter='Mounted';cleanupTrigger=$null;cleanupAttempted=$false;cleanupSucceeded=$true},[ordered]@{boundary='CombatEnded';source='IPartyCombatHandler.HandlePartyCombatStateChanged(false)';stateBefore='Mounted';stateAfter='Mounted';cleanupTrigger=$null;cleanupAttempted=$false;cleanupSucceeded=$true})}
        'mounted-pair-rider-death-cleanup' {$role='rider';$actorId='rider-id';$path='IUnitHandler.HandleUnitDeath';$deliveries=@([ordered]@{boundary='UnitDeath';source='IUnitHandler.HandleUnitDeath';stateBefore='Mounted';stateAfter='Unmounted';cleanupTrigger='Death';cleanupAttempted=$true;cleanupSucceeded=$true})}
        'mounted-pair-mount-death-cleanup' {$role='mount';$actorId='mount-id';$path='IUnitHandler.HandleUnitDeath';$deliveries=@([ordered]@{boundary='UnitDeath';source='IUnitHandler.HandleUnitDeath';stateBefore='Mounted';stateAfter='Unmounted';cleanupTrigger='Death';cleanupAttempted=$true;cleanupSucceeded=$true})}
        'mounted-pair-rider-incapacitated-cleanup' {$role='rider';$actorId='rider-id';$path='relationship.Dismount(Incapacitated)'}
        'mounted-pair-mount-incapacitated-cleanup' {$role='mount';$actorId='mount-id';$path='relationship.Dismount(Incapacitated)'}
        'mounted-pair-rider-native-incapacitated-cleanup' {$role='rider';$actorId='rider-id';$path='UnitEntityData.Damage -> UnitLifeController.TickOnUnit -> IUnitLifeStateChanged.HandleUnitLifeStateChanged';$deliveries=@([ordered]@{boundary='UnitIncapacitated';source='IUnitLifeStateChanged.HandleUnitLifeStateChanged';stateBefore='Mounted';stateAfter='Unmounted';cleanupTrigger='Incapacitated';cleanupAttempted=$true;cleanupSucceeded=$true;cleanupErrors=@()})}
        'mounted-pair-mount-native-incapacitated-cleanup' {$role='mount';$actorId='mount-id';$path='UnitEntityData.Damage -> UnitLifeController.TickOnUnit -> IUnitLifeStateChanged.HandleUnitLifeStateChanged';$deliveries=@([ordered]@{boundary='UnitIncapacitated';source='IUnitLifeStateChanged.HandleUnitLifeStateChanged';stateBefore='Mounted';stateAfter='Unmounted';cleanupTrigger='Incapacitated';cleanupAttempted=$true;cleanupSucceeded=$true;cleanupErrors=@()})}
        'mounted-pair-companion-removal-cleanup' {$role='mount';$actorId='mount-id';$path='IPartyHandler.HandleCompanionRemoved';$deliveries=@([ordered]@{boundary='PartyRemoved';source='IPartyHandler.HandleCompanionRemoved';stateBefore='Mounted';stateAfter='Unmounted';cleanupTrigger='CompanionInvalidated';cleanupAttempted=$true;cleanupSucceeded=$true})}
        'mounted-pair-view-destroyed-cleanup' {$role='rider';$actorId='rider-id';$path='IUnitHandler.HandleUnitDestroyed';$deliveries=@([ordered]@{boundary='ViewDetachedOrUnitDestroyed';source='IUnitHandler.HandleUnitDestroyed';stateBefore='Mounted';stateAfter='Unmounted';cleanupTrigger='ViewDetached';cleanupAttempted=$true;cleanupSucceeded=$true})}
        'mounted-pair-exception-cleanup' {$path='relationship.Dismount(Exception)'}
        default { throw "Unknown test combat lifecycle row: $Row" }
    }
    return [ordered]@{observed=$true;row=$Row;actorRole=$role;actorId=$actorId;invocationPath=$path;relationshipStateAfterBoundary=$state;deliveries=@($deliveries)}
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
    $claimLimit = Get-KmcLifecycleClaimLimit $Row $(if($Row -cin (Get-KmcNativeIncapacitationRuntimeRows)){7}else{0})
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
    $isNativeIncapacitation=@(Get-KmcNativeIncapacitationRuntimeRows | Where-Object { $_ -ceq $Row }).Count -eq 1
    $isCombatLifecycle=$isNativeIncapacitation -or @(Get-KmcCombatLifecycleRuntimeRows | Where-Object { $_ -ceq $Row }).Count -eq 1
    $record=[ordered]@{
        schemaVersion=$(if($isNativeIncapacitation){7}elseif($isCombatLifecycle){3}else{2});runId=[string]$Request.runId;scenario=[string]$Request.scenario;row=$Row;phase=$Phase
        utcTimestamp=[DateTimeOffset]::UtcNow.ToUniversalTime().ToString('o');branch=[string]$Request.branch;commit=[string]$Request.commit
        productVersion=[string]$Request.productVersion;dllSha256=[string]$Request.dllSha256;dllMvid=[string]$Request.dllMvid
        sequence=$Sequence;frame=$frame;relationshipState=$RelationshipState
        triggerScope=[ordered]@{
            expectedCleanupTrigger=$expectedTrigger;invocationPath=$invocationPath;nativeDeliveryObserved=($isNativeIncapacitation -and $Phase -cin @('cleanup-next-frame','row-finish','engine-finalization'))
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
    }
    if ($isCombatLifecycle) {
        $record.pose=[ordered]@{
            profileId=$(if($mounted){'medium-humanoid-mammoth-v1'}else{$null})
            boneInventory=$(if($mounted){'Pelvis,L_Up_leg,L_leg,L_foot,R_Up_leg,R_leg,R_foot'}else{$null})
            configured=$mounted;healthy=$mounted;frameApplied=$mounted
            baselineRestoreVerified=$(if($mounted){$false}elseif($Phase -ceq 'pre-mount'){$Sequence -gt 0}else{$restored})
            componentCount=$(if($mounted){1}else{0});boneCount=$(if($mounted){7}else{0});applicationFrameCount=$(if($mounted){1}else{0})
            footTargetClampCount=0;maximumFootTargetResidualWorldUnits=0.0;maximumKneeTargetResidualWorldUnits=0.0
            maximumSegmentLengthResidualWorldUnits=0.0;maximumApplyMicroseconds=$(if($mounted){1.0}else{0.0})
            averageApplyMicroseconds=$(if($mounted){1.0}else{0.0});failure=$null
        }
        $observed=$Phase -cin @('cleanup-next-frame','row-finish','engine-finalization')
        $record.boundaryExercise=New-TestCombatLifecycleBoundaryExercise -Row $Row -Observed:$observed
    }
    if ($isNativeIncapacitation) {
        $observed=$Phase -cin @('cleanup-next-frame','row-finish','engine-finalization')
        $role=if($Row -ceq 'mounted-pair-rider-native-incapacitated-cleanup'){'rider'}else{'mount'}
        $record.actorLifeTransition=[ordered]@{
            actorRole=$role;actorId=$(if($role -ceq 'rider'){'rider-id'}else{'mount-id'})
            mutationProperty='UnitEntityData.Damage';mutationIssued=$observed
            lifeStateBefore='Conscious';lifeStateAfter=$(if($observed){'Conscious'}else{$null})
            consciousBefore=$true;awakeBefore=$true;inAwakeUnitsBefore=$true
            consciousAfter=$observed;awakeAfter=$true;inAwakeUnitsAfter=$true;deadAfter=$false;finallyDeadAfter=$false
            damageBefore=0;requestedDamage=101;damageAfter=$(if($observed){93}else{0});damageImmediatelyAfterMutation=$(if($observed){101}else{0})
            hitPoints=100;constitution=14;nativeDeliveryCount=$(if($observed){1}else{0})
            nativeLifeObservationCount=$(if($observed){1}else{0});nativeObservedActorId=$(if($observed){if($role -ceq 'rider'){'rider-id'}else{'mount-id'}}else{$null})
            nativePreviousLifeState=$(if($observed){'Conscious'}else{$null});nativeCurrentLifeState=$(if($observed){'Unconscious'}else{$null})
            postDeliveryRecoveryObserved=$observed
        }
    }
    $record.recordErrors=@($RecordErrors)
    return $record
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
        mountPathError=0;mountPathErrorLog=$null;mountPathPointCount=2;mountPathLength=5.0
        astarPathPresent=$true;astarGraphUpdatesQueued=$false;unityFrameCount=120;tileHandlerLastUpdateFrame=119
        unityFrameStrictlyAfterTileHandlerLastUpdate=$true;synchronizationPhase='Update'
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
        'mounted-distance-door-interaction' { return @('door-mounted','dismounted') }
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
    $distanceDoor = $Row -ceq 'mounted-distance-door-interaction'
    $formation = $Row -ceq 'mounted-pair-party-formation' -or $poseDoorway
    $doorway = $Row -ceq 'mounted-pair-doorway' -or $poseDoorway -or $distanceDoor
    $stopStart = $Row -ceq 'mounted-pair-stop-start'
    $turns = $Row -ceq 'mounted-pair-turns-and-corners' -or $poseTurnStop
    $selection = $Row -ceq 'mounted-pair-selection'
    $pause = $Row -ceq 'mounted-pair-pause-unpause'
    $cancel = $Row -ceq 'mounted-pair-destination-cancel'
    $waypointCount = if ($poseIdle -or $poseEquipment -or $uiPresentation) { 0 }
        elseif ($distanceDoor) { 1 }
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
        unmountedDoorControlPassed=$doorway
        doorFixtureLeaseCaptured=$distanceDoor;doorFixtureOriginalOpen=$distanceDoor
        doorFixtureOriginalEnabled=$false;doorFixtureDisableOnOpen=$distanceDoor
        doorFixtureTemporaryEnableUsed=$distanceDoor;doorFixtureRestored=$distanceDoor
        doorDisableNavmeshCutWhenOpen=$distanceDoor;doorNavmeshCutPresent=$distanceDoor;doorNavmeshCutEnabled=$false
        doorInitialNavmeshCutRequiresUpdate=$(if($distanceDoor){$true}else{$null})
        doorFinalNavmeshCutRequiresUpdate=$false;doorTraversalReadinessQualified=$distanceDoor
        doorTraversalReadinessObservationCount=$(if($distanceDoor){2}else{0})
        doorTraversalReadinessElapsedSeconds=$(if($distanceDoor){0.25}else{0.0})
        doorApproachSkipped=$false;stopCommandIssuedCount=$(if($stopStart -or $cancel -or $poseTurnStop){1}else{0})
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

function New-TestCombatEvidenceRecord {
    param([Parameter(Mandatory = $true)]$Request)
    $rider = 'combat-rider'
    $mount = 'combat-mount'
    $target = 'combat-target'
    $isMammoth = [string]$Request.scenario -cin @('mounted-mammoth-primary-hit-rt','mounted-mammoth-primary-hit-tb')
    $isHumanPlay = [string]$Request.scenario -cin @(
        'mounted-rider-melee-human-play-path-rt','mounted-rider-melee-human-play-path-tb')
    $isReach = [string]$Request.scenario -cin @(
        'mounted-rider-melee-hit-rt','mounted-rider-melee-hit-tb',
        'mounted-mammoth-primary-hit-rt','mounted-mammoth-primary-hit-tb',
        'mounted-rider-melee-human-play-path-rt','mounted-rider-melee-human-play-path-tb')
    $isMovementToAttack = [string]$Request.scenario -cin @(
        'mounted-rider-melee-move-to-attack-rt','mounted-rider-melee-move-to-attack-tb')
    $isCancellation = [string]$Request.scenario -cin @(
        'mounted-rider-melee-command-cancel-rt','mounted-rider-melee-command-cancel-tb')
    $isInterruption = [string]$Request.scenario -cin @(
        'mounted-rider-melee-command-interrupt-rt','mounted-rider-melee-command-interrupt-tb')
    $isCombatEnd = [string]$Request.scenario -cin @(
        'mounted-rider-melee-combat-end-rt','mounted-rider-melee-combat-end-tb')
    $isTermination = $isCancellation -or $isInterruption -or $isCombatEnd
    $isApproach = $isMovementToAttack -or $isTermination
    $isTurnBased = [string]$Request.scenario -cin @(
        'mounted-rider-melee-hit-tb','mounted-mammoth-primary-hit-tb','mounted-rider-melee-move-to-attack-tb',
        'mounted-rider-melee-command-cancel-tb','mounted-rider-melee-command-interrupt-tb',
        'mounted-rider-melee-combat-end-tb','mounted-rider-melee-human-play-path-tb')
    $isMiss = [string]$Request.scenario -ceq 'mounted-rider-melee-miss-rt'
    $requiresDurability = $isMammoth -or $isApproach -or ($isHumanPlay -and $isTurnBased)
    $actor = if ($isMammoth) { $mount } else { $rider }
    $actorRole = if ($isMammoth) { 'mount' } else { 'rider' }
    $action = if ($isMammoth) { 'MountPrimaryNatural' } else { 'RiderMelee' }
    $record = [ordered]@{
        schemaVersion=$(if ($isHumanPlay) { if ($isTurnBased) { 52 } else { 48 } } elseif ($isCombatEnd) { if ($isTurnBased) { 41 } else { 40 } } elseif ($isTermination) { if ($isTurnBased) { 39 } else { 38 } } elseif ($isMovementToAttack) { if ($isTurnBased) { 35 } else { 34 } } elseif ($isReach) { if ($isTurnBased) { 43 } else { 42 } } elseif ($isTurnBased) { 27 } else { 26 });artifactKind='combat-scenario-evidence';runId=[string]$Request.runId
        scenario=[string]$Request.scenario;row=[string]$Request.scenario;rowIndex=0;sequence=0;frame=30
        utcTimestamp=[DateTimeOffset]::UtcNow.ToUniversalTime().ToString('o');branch=[string]$Request.branch
        commit=[string]$Request.commit;productVersion=[string]$Request.productVersion
        dllSha256=[string]$Request.dllSha256;dllMvid=[string]$Request.dllMvid;status='PASS';mode=$(if ($isTurnBased) { 'turn-based' } else { 'real-time' })
        action=$action;expectedActor=$actorRole;riderId=$rider;mountId=$mount;targetId=$target;clickAccepted=$true
        targetProvisioning=[ordered]@{
            targetBlueprintId='e7aa96d15a45238438ae4cfb476f6bb9';runtimeGroupId=('KMC.RuntimeHostile.'+[string]$Request.runId)
            blueprintEmptyHandWeaponBlueprintId='11111111111111111111111111111111';targetNativeSingleAttackWeaponBlueprintId='11111111111111111111111111111111'
            targetNativeSingleAttackSlot='PrimaryHand';targetPrimaryMainAttacks=1;targetSecondaryMainAttacks=0
            additionalLimbCountBefore=0;additionalLimbCountAfter=0;noWeaponProvisioningMutation=$true
            targetPrimaryHandHasItem=$false;targetWeaponUsesEmptyHandFallback=$true
            targetNativeSingleAttackWeaponIsNatural=$true;targetNativeSingleAttackWeaponIsMelee=$true
            noLoot=$true;rawAiDisabled=$true;sleeplessBefore=$false;sleeplessLeaseAcquired=$true
            temporaryHitPointsBefore=0;temporaryHitPointsAfterProvisioning=$(if ($requiresDurability) { 128 } else { 0 })
            durabilityLeaseAmount=$(if ($requiresDurability) { 128 } else { 0 });durabilityLeaseAcquired=$requiresDurability
            bidirectionalHostility=$true;noExperienceReward=$true
        }
        targetLife=[ordered]@{
            immediatelyAfterCreation=[ordered]@{
                observed=$true;lifeState='Conscious';conscious=$true;dead=$false;finallyDead=$false
                damage=0;nonLethalDamage=0;hitPoints=100;constitution=14;forceKill=$false;markedForDeath=$false
            }
            atActivation=[ordered]@{
                observed=$true;lifeState='Conscious';conscious=$true;dead=$false;finallyDead=$false
                damage=0;nonLethalDamage=0;hitPoints=100;constitution=14;forceKill=$false;markedForDeath=$false
            }
            lastObserved=[ordered]@{
                observed=$true;lifeState='Conscious';conscious=$true;dead=$false;finallyDead=$false
                damage=$(if ($isMiss -or $isTermination) { 0 } else { 10 });nonLethalDamage=0;hitPoints=100;constitution=14
                forceKill=$false;markedForDeath=$false
            }
            transitionCount=0
            firstTransition=[ordered]@{
                observed=$false;previousLifeState=$null;currentLifeState=$null
                snapshot=[ordered]@{
                    observed=$false;lifeState=$null;conscious=$false;dead=$false;finallyDead=$false
                    damage=0;nonLethalDamage=0;hitPoints=0;constitution=0;forceKill=$false;markedForDeath=$false
                }
            }
        }
        targetIncomingRules=[ordered]@{
            dispatchMarkerSet=$true;attackRuleCount=$(if ($isTermination) { 0 } else { 1 });damageRuleCount=$(if ($isMiss -or $isTermination) { 0 } else { 1 })
            preDispatchAttackRuleCount=0;preDispatchDamageRuleCount=0
            firstAttack=[ordered]@{
                observed=(-not $isTermination);beforeExpectedDispatch=$false;initiatorId=$(if ($isTermination) { $null } else { $actor })
                initiatorBlueprintId=$(if ($isTermination) { $null } else { '22222222222222222222222222222222' })
                initiatorIsPlayerFaction=(-not $isTermination);initiatorIsPlayersEnemy=$false
                initiatorGroupId=$(if ($isTermination) { $null } else { 'player-group' });initiatorGroupIsPlayerParty=(-not $isTermination)
                initiatorSharesRiderGroup=(-not $isTermination);initiatorSharesMountGroup=(-not $isTermination)
                initiatorDirectlyControllable=(-not $isTermination);initiatorEffectiveAiEnabled=$false
                initiatorRawAiEnabled=$false;initiatorCommandsEmpty=$false
                weaponBlueprintId=$(if ($isTermination) { $null } else { '33333333333333333333333333333333' })
                isAttackOfOpportunity=$false;isCharge=$false
            }
            firstDamage=$(if ($isMiss -or $isTermination) {
                [ordered]@{
                    observed=$false;beforeExpectedDispatch=$false;initiatorId=$null;initiatorBlueprintId=$null
                    initiatorIsPlayerFaction=$false;initiatorIsPlayersEnemy=$false;damage=0;isFake=$false;isDot=$false
                    attackRollPresent=$false;weaponBlueprintId=$null;sourceAbilityBlueprintId=$null;sourceAreaBlueprintId=$null
                }
            } else {
                [ordered]@{
                    observed=$true;beforeExpectedDispatch=$false;initiatorId=$actor
                    initiatorBlueprintId='22222222222222222222222222222222'
                    initiatorIsPlayerFaction=$true;initiatorIsPlayersEnemy=$false;damage=10;isFake=$false;isDot=$false
                    attackRollPresent=$true;weaponBlueprintId='33333333333333333333333333333333'
                    sourceAbilityBlueprintId=$null;sourceAreaBlueprintId=$null
                }
            })
        }
        nonPairPartyAiLease=[ordered]@{
            acquired=$true;groupId='player-group';groupIsPlayerParty=$true;riderSharesGroup=$true
            mountSharesGroup=$true;memberCount=1;activeValidationPassed=$true;restored=$true;lastError=$null
            members=@([ordered]@{
                unitId='combat-non-pair';blueprintId='44444444444444444444444444444444'
                directlyControllable=$true;inState=$true;commandsEmptyBefore=$true
                rawAiBefore=$true;effectiveAiBefore=$true;commandsEmptyDuring=$true
                rawAiDuring=$false;effectiveAiDuring=$false;commandsEmptyAfter=$true
                rawAiAfter=$true;effectiveAiAfter=$true
            })
        }
        targetBrainLease=[ordered]@{
            brainActiveBefore=$true;leaseAcquired=$true;effectiveAiEnabledDuring=$true
            validationCount=7;violationObserved=$false;suppressedAtClick=$true;suppressedAtOutcome=$true
            brainActiveAfterRelease=$true;leaseReleased=$true
        }
        pairApproachRadius=4.0;targetDistanceAtClick=$(if ($isApproach) { 6.0 } else { 3.9 })
        riderPositionAtClick=[ordered]@{x=0.0;y=0.0;z=0.0}
        mountPositionAtClick=[ordered]@{x=0.1;y=0.0;z=0.0}
        targetPositionAtClick=[ordered]@{x=$(if ($isApproach) { 6.1 } else { 4.0 });y=0.0;z=0.0}
        combatEntry=[ordered]@{
            memoryQueued=$true;playerGroupMemoryContainsTarget=$true;targetGroupMemoryContainsRider=$true
            riderInCombat=$true;mountInCombat=$true;targetInCombat=$true;playerInCombat=$true
            riderPrepared=$true;riderAwake=$true;targetAwake=$true;defaultGameMode=$true
            riderInitiative=$(if ($isMammoth) { 4.99591351 } else { 0.0 })
            actionActorId=$actor;actionActorPrepared=$true;actionActorCanActInCombat=$true;actionActorInitiative=0.0
            gameDeltaTime=0.01
            memoryRemovedAtCleanup=$true
            nativeJoin=[ordered]@{
                riderInGame=$true;mountInGame=$true;targetInGame=$true
                riderConscious=$true;mountConscious=$true;targetConscious=$true
                riderIgnoredByCombat=$false;mountIgnoredByCombat=$false;targetIgnoredByCombat=$false
                playerGroupContainsRider=$true;playerGroupContainsMount=$true;targetGroupContainsTarget=$true
                playerGroupEnemiesContainsTarget=$true;targetGroupEnemiesContainsRider=$true
                riderNotInFogOfWar=$true;targetNotInFogOfWar=$true
                riderNotInStealthAmbush=$true;targetNotInStealthAmbush=$true
            }
        }
        dispatch=[ordered]@{
            originalPaused=$true;unpausedForRealTime=(-not $isTurnBased);pausedAtClick=$false
            equipmentControllerAvailable=$true;equipmentUpdateScheduled=$false;pauseRestored=$true
        }
        resources=[ordered]@{
            riderStandardBefore=0.0;riderStandardAfter=$(if ($isMammoth -or $isTermination) { 0.0 } else { 5.5 });riderMoveBefore=$(if ($isHumanPlay -and $isTurnBased) { 0.5 } else { 0.0 });riderMoveAfter=$(if (($isApproach -or $isHumanPlay) -and $isTurnBased) { 3.0 } else { 0.0 })
            mountStandardBefore=0.0;mountStandardAfter=$(if ($isMammoth) { 5.5 } else { 0.0 });mountMoveBefore=0.0;mountMoveAfter=0.0
        }
        command=[ordered]@{
            action=$action;actorId=$actor;targetId=$target;result=$(if ($isTermination) { 'Interrupt' } else { 'Success' });childAttackStartCount=$(if ($isTermination) { 0 } else { 1 })
            repathCount=0;riderStandardCharged=(-not $isMammoth);nativeAttackRuleObserved=(-not $isTermination);terminalReason=$(if ($isTermination) { 'Interrupt' } else { 'completed' })
            pairRangeSatisfiedAtStart=(-not $isTermination);pairDistanceAtStart=$(if ($isTermination) { 0.0 } else { 3.9 });pairApproachRadiusAtStart=$(if ($isTermination) { 0.0 } else { 4.0 })
            nativeExecutorDistanceAtStart=$(if ($isTermination) { 0.0 } else { 4.1 });nativeAdmissionRadiusAtStart=$(if ($isTermination) { 0.0 } else { 4.101 });nativeAdmissionAdjusted=(-not $isTermination)
        }
        rules=[ordered]@{
            forcedD20=$(if ($isTermination) { $null } elseif ($isMiss) { 1 } else { 20 });forcedD20Count=$(if ($isTermination) { 0 } else { 1 });attackRuleCount=$(if ($isTermination) { 0 } else { 1 });attackRollCount=$(if ($isTermination) { 0 } else { 1 })
            damageRuleCount=$(if ($isMiss -or $isTermination) { 0 } else { 1 });unexpectedPairAttackCount=0
            totalDamage=$(if ($isMiss -or $isTermination) { 0 } else { 10 });lastInitiatorId=$(if ($isTermination) { $null } else { $actor });lastTargetId=$(if ($isTermination) { $null } else { $target })
            lastAttackResult=$(if ($isTermination) { $null } elseif ($isMiss) { 'Miss' } else { 'Hit' });lastAttackHit=$(if ($isTermination) { $null } else { -not $isMiss })
        }
        movement=[ordered]@{
            authoritativeMover='mount';repathCount=0;riderDisplacementAtOutcome=$(if ($isApproach) { 1.0 } else { 0.0 });mountDisplacementAtOutcome=$(if ($isApproach) { 1.0 } else { 0.0 })
            targetDisplacementAtOutcome=0.0;riderStockAgentEnabledAtEnd=$true;mountStockAgentEnabledAtEnd=$true
            riderAvoidanceDisabledAtEnd=$false;mountAvoidanceDisabledAtEnd=$false
        }
        pose=[ordered]@{
            profileId='medium-humanoid-mammoth-v1';healthyAtOutcome=$true;configuredAtEnd=$false
            attachmentLeaseAtEnd=$false;residueAtEnd=$false
        }
        cleanup=[ordered]@{
            targetRemoved=$true;targetEntityRemoved=$true;runtimeGroupRemoved=$true;runtimeFactionRemoved=$true
            durabilityLeaseReleased=$true;brainLeaseReleased=$true;sleeplessLeaseReleased=$true;nonPairPartyAiLeaseRestored=$true
            relationshipClean=$true;combatCleared=$true;relationshipState='Unmounted'
            residualState=$false;presentationResidual=$false
        }
        selection=@($rider);assertionPassCount=25;assertionFailCount=0;errors=@()
    }
    $record.dispatch.actionActorCanActInCombat = $true
    $record.dispatch.actionActorHandsBusy = $false
    $record.command.commandOwnerId = $actor
    $record.command.resourceOwnerId = $actor
    $record.command.actionStandardCharged = -not $isTermination
    $record.command.riderStandardCharged = -not $isMammoth -and -not $isTermination
    $record.command.attackWeaponBlueprintId = '33333333333333333333333333333333'
    $record.command.attackWeaponIsNatural = $isMammoth
    $record.command.attackWeaponIsRanged = $false
    $record.command.attackWeaponSlot = if ($isMammoth) { 'PrimaryHand' } else { 'EquippedMelee' }
    if ($isReach) {
        $record.reach = [ordered]@{
            riderWeaponBlueprintId='33333333333333333333333333333333'
            mountWeaponBlueprintId='55555555555555555555555555555555'
            riderWeaponRange=2.0;mountWeaponRange=2.0;mountCorpulence=1.0;targetCorpulence=1.0
            riderStoppingRadius=4.0;mountStoppingRadius=4.0;initialDistance=6.0
            riderProbeRadiusAtInitial=4.0;mountProbeRadiusAtInitial=4.0
            riderOutsideAtInitial=$true;mountOutsideAtInitial=$true;dispatchDistance=3.9
            riderWithinAtDispatch=$true;mountWithinAtDispatch=$true
            riderCanAttackTarget=$true;mountCanAttackTarget=$true
            targetCanAttackRider=$true;targetCanAttackMount=$true
            inputsUnchangedAtDispatch=$true;actionRadiusMatches=$true
        }
    }
    if ($isHumanPlay) {
        $record.admission = [ordered]@{
            armedThroughPlayerFacingCombatController=$true;overlayActivationWorldClickSuppressed=$true
            armedActionRetainedAfterOverlayClick=$true;directClickedUnitView=$true
            feedback='Mounted pair command accepted: RiderMelee.';rejectionCodes=@()
        }
    }
    if ($isApproach) {
        $record.movementToAttack = [ordered]@{
            requestedTargetDistance=6.0;approachRequiredAtStart=$true;delegatedMoveStartCount=1
            delegatedMoveTickCount=$(if ($isTurnBased -and -not $isTermination) { 12 } else { 0 });delegatedMoveExecutorId=$mount;delegatedMoveExecutorIsExactMount=$true
            wrapperCommandRetainedThroughoutApproach=$true;delegatedMoveNeverQueuedOnMount=$true
            delegatedMoveOwnedByMountMoveSlot=$true;mountMoveSlotUnreplacedThroughoutApproach=$true
            mountQueueEmptyThroughoutApproach=$true;delegatedMoveFinishedSuccessfully=(-not $isTermination)
            mountMoveSlotRestoredAfterApproach=$true;delegatedMoveDrivenByStockController=(-not $isTurnBased)
            delegatedMoveDrivenByRiderTurnAdapter=$isTurnBased;delegatedMoveProgressObservationCount=12
            riderStockAgentSuppressedThroughoutApproach=$true;mountStockAgentAuthoritativeThroughoutApproach=$true
            poseHealthyThroughoutApproach=$true;commandObservationCount=12;runtimeObservationCount=12
            selectionRetainedDuringApproach=$true;uiCoherentDuringApproach=$true
            initialPairDistance=6.0;pairDistanceAtAttackStart=$(if ($isTermination) { 0.0 } else { 3.9 })
            riderDisplacementAtAttackStart=$(if ($isTermination) { 0.0 } else { 2.0 });mountDisplacementAtAttackStart=$(if ($isTermination) { 0.0 } else { 2.0 })
            targetDisplacementAtAttackStart=0.0
        }
    }
    if ($isTermination) {
        $record.commandTermination = [ordered]@{
            kind=$(if ($isCancellation) { 'player-stop' } elseif ($isInterruption) { 'native-wrapper-interrupt' } else { 'party-combat-end' })
            trigger=$(if ($isCancellation) { 'SelectionManagerBase.Stop' } elseif ($isInterruption) { 'UnitCommands.InterruptAll' } else { 'IPartyCombatHandler.HandlePartyCombatStateChanged(false)' })
            delivered=$true;repeatedIdempotently=$true;wrapperPresentBefore=$true;delegatedMovePresentBefore=$true
            riderQueueEmptyBefore=$true;mountQueueEmptyBefore=$true;childAttackNotStartedBefore=$true
            pairDistanceAtTrigger=5.0;riderDisplacementAtTrigger=1.0;mountDisplacementAtTrigger=1.0;targetDisplacementAtTrigger=0.0
            wrapperAbsentAfter=$true;delegatedMoveAbsentAfter=$true;riderQueueEmptyAfter=$true;mountQueueEmptyAfter=$true
            mountAgentStoppedAfter=$true;activeCommandClearedAfter=$true;relationshipPreservedAfter=$true
            selectionRetainedAfter=$true;uiCoherentAfter=$true
        }
        if ($isCombatEnd) {
            $record.commandTermination.lifecycleDeliveryCount = 2
            $record.commandTermination.lifecycleDeliveriesExact = $true
        }
    }
    if ($isTurnBased) {
        $record.turnBased = [ordered]@{
            requested=$true;originalEnabled=$false;temporaryEnabled=$true;originalRawCacheHadValue=$true
            enabledAtMount=(-not $isHumanPlay);controllerInitialized=$true;rosterContainsRider=$true
            rosterContainsMount=$true;rosterContainsTarget=$true;expectedTurnActor=$actorRole
            nativeActionActorTurnStarted=$true;currentTurnUnitIdAtDispatch=$actor
            currentTurnActingAtDispatch=$true;roundNumberAtDispatch=1
            currentTurnUnitIdAtOutcome=$actor;currentTurnActingAtOutcome=(-not $isMammoth)
            actionActorTurnEndedAfterCommand=$isMammoth
            restoreDeliveryCompleted=$true;modeRestored=$true;persistedValueUnchanged=$true
        }
        if ($isHumanPlay) {
            $record.turnBased.pairMountedBeforeEnable = $true
            $record.turnBased.pairRetainedAfterEnable = $true
            $record.turnBased.pairRetainedAfterRealtimeRestore = $true
            $record.turnBased.mountAiLeaseReassertionArmedCount = 1
            $record.turnBased.mountAiLeaseReassertionAttemptCount = 1
            $record.turnBased.mountAiLeaseReassertionMutationCount = 1
            $record.turnBased.mountAiLeaseReassertionSuccessCount = 1
            $record.turnBased.mountAiLeaseReassertionResult = 'reasserted'
            $record.turnBased.riderUiLeaseRestoreArmedCount = 1
            $record.turnBased.riderUiLeaseRestoreAttemptCount = 1
            $record.turnBased.riderUiLeaseRestoreMutationCount = 1
            $record.turnBased.riderUiLeaseRestoreSuccessCount = 1
            $record.turnBased.riderUiLeaseRestoreResult = 'reselected-rider'
            $presentation = 'mode=Default;turnBased=True;riderViewExact=True;riderViewActiveSelf=True;riderViewActiveInHierarchy=True;riderParent=KMC_RiderPositionAnchor;riderSibling=0;riderRendererCount=3;riderEnabledRendererCount=3;mountViewExact=True;mountViewActiveSelf=True;mountViewActiveInHierarchy=True;poseLease=True;attachmentLease=True;replacementReleased=False;riderSelected=True;observationScope=full-ui;actionBarOwner=' + $rider + ';actionBarActive=True;actionBarEnabled=True;actionBarActiveSelf=True;actionBarActiveInHierarchy=True;actionBarReactiveActive=True;actionBarCanUseAbilities=True;actionBarSectionShown=True;portraitOwnerCount=2;portraitActiveOwnerCount=1;portraitActive=True;portraitSelected=True;cameraOn=False;cameraOwner=' + $rider + ';selectedUnit=' + $rider + ';turnUnit=' + $rider + ';turnStatus=Acting;turnUnitDirectlyControllable=True;turnCanMove=True;turnCanEndNoActing=False;pointerInGui=False;pointerControllerAvailable=True;pointerMode=Default;riderCommands=0;mountCommands=0;riderAiEnabled=False;mountAiEnabled=False'
            $record.turnBased.presentationAfterEnable = $presentation
            $record.turnBased.presentationAfterRealtimeRestore = $presentation.Replace('turnBased=True','turnBased=False')
            $record.turnBased.presentationDuringMammothTurn = $presentation.Replace('actionBarOwner=' + $rider,'actionBarOwner=' + $mount).Replace('selectedUnit=' + $rider,'selectedUnit=' + $mount).Replace('turnUnit=' + $rider,'turnUnit=' + $mount)
            $record.turnBased.presentationAfterNativeMammothGroundInput = $record.turnBased.presentationDuringMammothTurn.Replace('turnCanMove=True','turnCanMove=False').Replace('turnCanEndNoActing=False','turnCanEndNoActing=True')
            $record.turnBased.nativeMammothTurnStarted = $true
            $record.turnBased.nativeMammothTurnUiObserved = $true
            $record.turnBased.nativeMammothGroundInputStarted = $true
            $record.turnBased.nativeMammothGroundInputCompleted = $true
            $record.turnBased.nativeMammothGroundSelectionRetained = $true
            $record.turnBased.nativeMammothGroundUiObservedAfterInput = $true
            $record.turnBased.nativeMammothGroundCommandFinished = $true
            $record.turnBased.nativeMammothGroundCommandResult = 'Success'
            $record.turnBased.nativeMammothGroundRawMoveSlotState = 'empty'
            $record.turnBased.nativeMammothGroundInterruptSource = '<not-interrupted>'
            $record.turnBased.nativeMammothPhysicalPointerQualification = 'manual-required'
            $record.turnBased.nativeMammothGroundEnoughCloseAtTerminal = $true
            $record.turnBased.nativeMammothGroundAgentReallyMovingAtTerminal = $false
            $record.turnBased.nativeMammothGroundAgentWantsToMoveAtTerminal = $false
            $record.turnBased.mammothNativeGroundDisplacement = 1.5
            $record.turnBased.mammothNativeGroundRemainingDistance = 0.0
            $record.turnBased.mammothNativeMoveBefore = 0.0
            $record.turnBased.mammothNativeMoveAfter = 1.5
            $record.turnBased.riderMoveBeforeMammothNativeGroundInput = 0.0
            $record.turnBased.riderMoveAfterMammothNativeGroundInput = 0.0
            $record.groundMovement = [ordered]@{
                requested=$true;destination=[ordered]@{x=2.0;y=0.0;z=0.0};result='Success'
                driveCount=12;executorId=$mount;executorIsExactMount=$true
                usedRiderTurnAdapter=$true;slotRestored=$true
                riderMoveBefore=0.0;riderMoveAfter=2.0;mountMoveBefore=0.0;mountMoveAfter=0.0
                riderDisplacement=2.0;mountDisplacement=2.0;targetDisplacement=0.0
                pairRetained=$true;selectionRetained=$true;poseHealthy=$true
            }
        }
    }
    return $record
}

function New-TestMovementDoorReadinessRecord {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][long]$Sequence
    )
    return [ordered]@{
        schemaVersion=1;runId=[string]$Request.runId;scenario=[string]$Request.scenario
        row='mounted-distance-door-interaction';branch=[string]$Request.branch;commit=[string]$Request.commit
        productVersion=[string]$Request.productVersion;dllSha256=[string]$Request.dllSha256;dllMvid=[string]$Request.dllMvid
        sequence=$Sequence;utcTimestamp=[DateTimeOffset]::UtcNow.ToUniversalTime().ToString('o')
        kind='door-traversal-readiness';door='Area/Door';doorOpen=$true;disableNavmeshCutWhenOpen=$true
        navmeshCutPresent=$true;navmeshCutEnabled=$false;initialNavmeshCutRequiresUpdate=$true
        finalNavmeshCutRequiresUpdate=$false;astarPathPresent=$true;astarGraphUpdatesQueued=$true
        unityFrameCount=120;tileHandlerLastUpdateFrame=119;unityFrameStrictlyAfterTileHandlerLastUpdate=$true
        observationCount=2;elapsedSeconds=0.25;ready=$true
    }
}

function New-TestMovementPathReplacementRecord {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][long]$Sequence
    )
    return [ordered]@{
        schemaVersion=1;runId=[string]$Request.runId;scenario=[string]$Request.scenario
        row='mounted-distance-door-interaction';branch=[string]$Request.branch;commit=[string]$Request.commit
        productVersion=[string]$Request.productVersion;dllSha256=[string]$Request.dllSha256;dllMvid=[string]$Request.dllMvid
        sequence=$Sequence;utcTimestamp=[DateTimeOffset]::UtcNow.ToUniversalTime().ToString('o')
        kind='navigation-path-replacement';replacementIndex=1;previousPathId=8;newPathId=9
        previousPathFirstObservedFrame=120;replacementObservedFrame=121;tileHandlerLastUpdateFrame=120
        previousPathFirstObservedNotNewerThanTileUpdateFrame=$true
        astarPathPresent=$true;astarGraphUpdatesQueued=$false;agentRepathNeeded=$false
        pathFailed=$false;pathError=$false;commandReferenceRetained=$true
        commandType='Kingmaker.UnitLogic.Commands.UnitMoveTo'
    }
}

function Write-TestCombatEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$EvidenceRoot,
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)]$Record,
        [switch]$OmitManifestRecord,
        [string]$ManifestKind='combat-evidence'
    )
    New-Item -ItemType Directory -Path $EvidenceRoot -Force | Out-Null
    $path = Join-Path $EvidenceRoot 'combat-scenario-evidence.jsonl'
    [IO.File]::WriteAllText($path, ($Record | ConvertTo-Json -Compress -Depth 15) + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
    $artifacts = if ($OmitManifestRecord) { @() } else {
        @([ordered]@{relativePath='combat-scenario-evidence.jsonl';kind=$ManifestKind;length=(Get-Item -LiteralPath $path).Length;sha256=(Get-KmcSha256 $path)})
    }
    return New-TestArtifactManifest -EvidenceRoot $EvidenceRoot -RunId $Request.runId -Scenario $Request.scenario -Artifacts $artifacts
}

function New-TestCombatControlEvidenceRecords {
    param([Parameter(Mandatory = $true)]$Request)
    $rows = @(Get-KmcCombatControlRuntimeRows)
    $paths = @{
        'mounted-rider-melee-invalid-target' = 'ClickUnitHandler.OnClick -> MountedCombatController.TryHandleUnitClick -> MountedCombatActionEvaluator.Evaluate'
        'mounted-rider-melee-target-death' = 'UnitEntityData.Damage -> mounted command liveness -> UnitCommand.Interrupt'
        'mounted-rider-melee-cleanup' = 'MountedRelationshipCoordinator.Dismount(Exception) -> MountedCombatController.HandleDismounting'
        'non-mounted-melee-control' = 'MountedCombatController.Arm/TryHandleUnitClick -> NotHandled stock delegation'
    }
    $records = New-Object 'Collections.Generic.List[object]'
    for ($index = 0; $index -lt $rows.Count; $index++) {
        $row = [string]$rows[$index]
        $observations = [ordered]@{
            controlKind=$row;riderArmed=$false;mountArmed=$false;riderInvalidRejected=$false
            mountInvalidRejected=$false;armedCleared=$false;activeCommandAbsent=$false
            combatActionsHidden=$false;armRejectedUnmounted=$false;controllerNotHandledUnmounted=$false
            riderAgentUnchangedNonMounted=$false;mountAgentUnchangedNonMounted=$false;commandAccepted=$false
            targetDamageBefore=0;targetDamageRequested=0;targetDamageAfter=0
            targetLifeTransitionObserved=$false;targetDeadOrFinallyDead=$false;commandInterrupted=$false
            cleanupTrigger=$(if ($row -ceq 'mounted-rider-melee-cleanup') { 'Exception' } else { 'none' })
            firstCleanupSucceeded=$false;repeatedCleanupSucceeded=$false;childAttackStartCount=0
            attackRuleCount=0;attackRollCount=0;damageRuleCount=0;unexpectedPairAttackCount=0
            forcedD20Count=0;relationshipPreservedAfterTargetDeath=$false;resourcesUnchanged=$true
        }
        switch -CaseSensitive ($row) {
            'mounted-rider-melee-invalid-target' {
                $observations.riderArmed=$true;$observations.mountArmed=$true
                $observations.riderInvalidRejected=$true;$observations.mountInvalidRejected=$true
                $observations.armedCleared=$true;$observations.activeCommandAbsent=$true
            }
            'mounted-rider-melee-target-death' {
                $observations.commandAccepted=$true;$observations.targetDamageRequested=115
                $observations.targetDamageAfter=115;$observations.targetLifeTransitionObserved=$true
                $observations.targetDeadOrFinallyDead=$true;$observations.commandInterrupted=$true
                $observations.relationshipPreservedAfterTargetDeath=$true
            }
            'mounted-rider-melee-cleanup' {
                $observations.commandAccepted=$true;$observations.commandInterrupted=$true
                $observations.firstCleanupSucceeded=$true;$observations.repeatedCleanupSucceeded=$true
            }
            'non-mounted-melee-control' {
                $observations.activeCommandAbsent=$true;$observations.combatActionsHidden=$true
                $observations.armRejectedUnmounted=$true;$observations.controllerNotHandledUnmounted=$true
                $observations.riderAgentUnchangedNonMounted=$true;$observations.mountAgentUnchangedNonMounted=$true
            }
        }
        $records.Add([ordered]@{
            schemaVersion=1;artifactKind='combat-core-control-evidence';runId=[string]$Request.runId
            scenario=[string]$Request.scenario;row=$row;rowIndex=$index;sequence=$index;frame=(20+$index)
            utcTimestamp=[DateTimeOffset]::UtcNow.ToUniversalTime().ToString('o');branch=[string]$Request.branch
            commit=[string]$Request.commit;productVersion=[string]$Request.productVersion
            dllSha256=[string]$Request.dllSha256;dllMvid=[string]$Request.dllMvid;status='PASS'
            riderId='combat-control-rider';mountId='combat-control-mount';targetId=('combat-control-target-'+$index)
            mountedAtExercise=($row -cne 'non-mounted-melee-control');productionPath=[string]$paths[$row]
            observations=$observations
            resources=[ordered]@{
                riderStandardBefore=0.0;riderStandardAfter=0.0;riderMoveBefore=0.0;riderMoveAfter=0.0
                mountStandardBefore=0.0;mountStandardAfter=0.0;mountMoveBefore=0.0;mountMoveAfter=0.0
            }
            cleanup=[ordered]@{
                targetRemoved=$true;relationshipClean=$true;combatCleared=$true;agentsRestored=$true
                pauseRestored=$true;runtimeLockOrDeploymentCreated=$false;residualState=$false
            }
            assertionPassCount=12;assertionFailCount=0;errors=@()
        })
    }
    return $records.ToArray()
}

function Write-TestCombatControlEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$EvidenceRoot,
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)]$Records
    )
    New-Item -ItemType Directory -Path $EvidenceRoot -Force | Out-Null
    $path = Join-Path $EvidenceRoot 'combat-scenario-evidence.jsonl'
    $jsonLines = @($Records | ForEach-Object { $_ | ConvertTo-Json -Compress -Depth 15 })
    [IO.File]::WriteAllText($path, ($jsonLines -join [Environment]::NewLine) + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
    $artifacts = @([ordered]@{
        relativePath='combat-scenario-evidence.jsonl';kind='combat-evidence'
        length=(Get-Item -LiteralPath $path).Length;sha256=(Get-KmcSha256 $path)
    })
    return New-TestArtifactManifest -EvidenceRoot $EvidenceRoot -RunId $Request.runId -Scenario $Request.scenario -Artifacts $artifacts
}

function Copy-TestJsonValue {
    param([Parameter(Mandatory = $true)]$Value)
    return ($Value | ConvertTo-Json -Compress -Depth 20 | ConvertFrom-Json)
}

function Remove-TestCombatWakeLeaseFields {
    param([Parameter(Mandatory = $true)]$Record)
    if ($null -ne $Record.combatEntry) {
        $Record.combatEntry.PSObject.Properties.Remove('targetAwake')
    }
    $Record.targetProvisioning.PSObject.Properties.Remove('sleeplessBefore')
    $Record.targetProvisioning.PSObject.Properties.Remove('sleeplessLeaseAcquired')
    $Record.cleanup.PSObject.Properties.Remove('sleeplessLeaseReleased')
    Remove-TestCombatLifeFields $Record
    Remove-TestCombatNativeJoinFields $Record
}

function Remove-TestCombatNativeJoinFields {
    param([Parameter(Mandatory = $true)]$Record)
    if ($null -ne $Record.combatEntry) {
        $Record.combatEntry.PSObject.Properties.Remove('nativeJoin')
    }
}

function Remove-TestCombatLifeFields {
    param([Parameter(Mandatory = $true)]$Record)
    $Record.PSObject.Properties.Remove('targetLife')
    Remove-TestCombatIncomingRuleFields $Record
}

function Remove-TestCombatIncomingRuleFields {
    param([Parameter(Mandatory = $true)]$Record)
    $Record.PSObject.Properties.Remove('targetIncomingRules')
    Remove-TestCombatNonPairPartyAiLeaseFields $Record
}

function Remove-TestCombatIncomingActorContextFields {
    param([Parameter(Mandatory = $true)]$Record)
    foreach ($name in @(
        'initiatorGroupId','initiatorGroupIsPlayerParty','initiatorSharesRiderGroup','initiatorSharesMountGroup',
        'initiatorDirectlyControllable','initiatorEffectiveAiEnabled','initiatorRawAiEnabled','initiatorCommandsEmpty')) {
        $Record.targetIncomingRules.firstAttack.PSObject.Properties.Remove($name)
    }
    Remove-TestCombatNonPairPartyAiLeaseFields $Record
}

function Remove-TestCombatNonPairPartyAiLeaseFields {
    param([Parameter(Mandatory = $true)]$Record)
    $Record.PSObject.Properties.Remove('nonPairPartyAiLease')
    if ($null -ne $Record.cleanup) {
        $Record.cleanup.PSObject.Properties.Remove('nonPairPartyAiLeaseRestored')
    }
    Remove-TestCombatDurabilityLeaseFields $Record
}

function Remove-TestCombatDurabilityLeaseFields {
    param([Parameter(Mandatory = $true)]$Record)
    foreach ($name in @(
        'temporaryHitPointsBefore','temporaryHitPointsAfterProvisioning',
        'durabilityLeaseAmount','durabilityLeaseAcquired')) {
        $Record.targetProvisioning.PSObject.Properties.Remove($name)
    }
    foreach ($name in @('riderDisplacementAtOutcome','mountDisplacementAtOutcome','targetDisplacementAtOutcome')) {
        $Record.movement.PSObject.Properties.Remove($name)
    }
    $Record.cleanup.PSObject.Properties.Remove('durabilityLeaseReleased')
    Remove-TestCombatBrainLeaseFields $Record

    if ([long]$Record.schemaVersion -lt 20 -and
        $null -ne $Record.dispatch.PSObject.Properties['actionActorCanActInCombat']) {
        $Record.dispatch | Add-Member -NotePropertyName riderCanActInCombat -NotePropertyValue $Record.dispatch.actionActorCanActInCombat
        $Record.dispatch | Add-Member -NotePropertyName riderHandsBusy -NotePropertyValue $Record.dispatch.actionActorHandsBusy
        $Record.dispatch.PSObject.Properties.Remove('actionActorCanActInCombat')
        $Record.dispatch.PSObject.Properties.Remove('actionActorHandsBusy')
        foreach ($name in @(
            'commandOwnerId','resourceOwnerId','actionStandardCharged','attackWeaponBlueprintId',
            'attackWeaponIsNatural','attackWeaponIsRanged','attackWeaponSlot')) {
            $Record.command.PSObject.Properties.Remove($name)
        }
    }
    if ([long]$Record.schemaVersion -lt 21 -and
        $null -ne $Record.PSObject.Properties['turnBased'] -and
        $null -ne $Record.turnBased.PSObject.Properties['nativeActionActorTurnStarted']) {
        $Record.turnBased | Add-Member -NotePropertyName nativeRiderTurnStarted -NotePropertyValue $Record.turnBased.nativeActionActorTurnStarted
        $Record.turnBased.PSObject.Properties.Remove('expectedTurnActor')
        $Record.turnBased.PSObject.Properties.Remove('nativeActionActorTurnStarted')
        $Record.turnBased.PSObject.Properties.Remove('actionActorTurnEndedAfterCommand')
    }
}

function Remove-TestCombatBrainLeaseFields {
    param([Parameter(Mandatory = $true)]$Record)
    $Record.PSObject.Properties.Remove('targetBrainLease')
    if ($null -ne $Record.cleanup) {
        $Record.cleanup.PSObject.Properties.Remove('brainLeaseReleased')
    }
    Remove-TestCombatActionActorReadinessFields $Record
}

function Remove-TestCombatActionActorReadinessFields {
    param([Parameter(Mandatory = $true)]$Record)
    $Record.PSObject.Properties.Remove('reach')
    if ($null -ne $Record.combatEntry) {
        foreach ($name in @(
            'actionActorId','actionActorPrepared','actionActorCanActInCombat','actionActorInitiative')) {
            $Record.combatEntry.PSObject.Properties.Remove($name)
        }
    }
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
        Assert-Test ([int]$validated.schemaVersion -eq 1) 'schema-v1 protected-save authority no longer validates through the compatibility entry point'
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

    Invoke-HarnessTest 'schema-v2 chained authority WhatIf is pure and preserves immutable schema-v1 history' {
        $prepared = New-TestPreparedChainedProtectedSaveEpoch 'schema-v2-whatif'
        $fixture = $prepared.fixture
        $scriptPath = Join-Path $repoRoot 'scripts\runtime\New-KmcChainedProtectedSaveContinuityAuthority.ps1'
        $stateBefore = Get-KmcDirectoryManifest $fixture.stateRoot
        $savesBefore = Get-KmcSaveMetadataInventory $fixture.saveRoot
        $parentBefore = Get-Item -LiteralPath $prepared.parentAuthorityPath -Force
        $output = @(& $scriptPath `
            -SaveRoot $fixture.saveRoot -StateRoot $fixture.stateRoot -EpochId $prepared.epochId `
            -ExpectedCurrentQualificationSha256 $prepared.parentEpoch.qualificationSha256 `
            -ExpectedBaselineSha256 $fixture.baselineSha256 `
            -ExpectedSupersededWorkingSha256 $fixture.supersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $fixture.revisedWorkingSha256 `
            -PriorSaveTransactionStatePath $fixture.priorSaveTransactionStatePath `
            -ExpectedPriorSaveTransactionRunId $fixture.priorSaveTransactionRunId `
            -ExpectedPriorSaveTransactionStateSha256 $fixture.priorSaveTransactionStateSha256 `
            -ExpectedPriorSaveMetadataDigest $fixture.priorSaveMetadataDigest `
            -ParentAuthorityPath $prepared.parentAuthorityPath `
            -ExpectedParentAuthorityEpochId $prepared.parentEpoch.epochId `
            -ExpectedParentAuthoritySha256 $prepared.parentAuthoritySha256 `
            -AuthorizedTransitionsJson $prepared.transitionsJson -WhatIf 6>&1)
        Assert-Test (($output -join "`n") -like '*Schema-v2 protected-save continuity authority WhatIf PASS*') 'schema-v2 authority WhatIf did not report PASS'
        Assert-KmcSaveMetadataInventoriesEqual -Before $savesBefore -After (Get-KmcSaveMetadataInventory $fixture.saveRoot) -Description 'schema-v2 authority WhatIf saves'
        Assert-Test ((Get-KmcDirectoryManifest $fixture.stateRoot).digest -ceq $stateBefore.digest) 'schema-v2 authority WhatIf changed runtime state'
        Assert-Test (-not (Test-Path -LiteralPath (Join-Path (Join-Path $fixture.stateRoot 'protected-save-authorities') ($prepared.epochId + '.json')))) 'schema-v2 authority WhatIf created its epoch'
        $parentAfter = Get-Item -LiteralPath $prepared.parentAuthorityPath -Force
        Assert-Test ((Get-KmcSha256 $prepared.parentAuthorityPath) -ceq $prepared.parentAuthoritySha256 -and
            $parentAfter.Length -eq $parentBefore.Length -and
            $parentAfter.LastWriteTimeUtc.Ticks -eq $parentBefore.LastWriteTimeUtc.Ticks) 'schema-v2 authority WhatIf changed the immutable schema-v1 parent'
    }

    Invoke-HarnessTest 'schema-v2 chain preserves history, replaces exact pins, and agrees before and under lock' {
        $epoch = New-TestCommittedChainedProtectedSaveEpoch 'schema-v2-commit'
        $fixture = $epoch.fixture
        $continuityArguments = @{
            SaveRoot=$fixture.saveRoot;StateRoot=$fixture.stateRoot;QualificationPath=$fixture.qualificationPath
            ExpectedCurrentQualificationSha256=$epoch.parentEpoch.qualificationSha256
            ExpectedSupersededWorkingSha256=$fixture.supersededWorkingSha256
            PriorSaveTransactionStatePath=$fixture.priorSaveTransactionStatePath
            ExpectedPriorSaveTransactionRunId=$fixture.priorSaveTransactionRunId
            ExpectedPriorSaveTransactionStateSha256=$fixture.priorSaveTransactionStateSha256
            ExpectedPriorSaveMetadataDigest=$fixture.priorSaveMetadataDigest
            ProtectedSaveContinuityAuthorityPath=$epoch.authorityPath
            ExpectedProtectedSaveContinuityEpochId=$epoch.epochId
            ExpectedProtectedSaveContinuityAuthoritySha256=$epoch.authoritySha256
            ExpectedProtectedSavePinSetSha256=$epoch.protectedSavePinSetSha256
        }
        $preflight = Assert-KmcQualifiedWorkingProtectedSaveContinuity @continuityArguments
        Assert-Test ([int]$preflight.schemaVersion -eq 2) 'schema-v2 compatibility entry point returned the wrong schema'
        $runtimeShapeArguments = @{}
        foreach ($key in $continuityArguments.Keys) { $runtimeShapeArguments[$key] = $continuityArguments[$key] }
        $runtimeShapeArguments.ExpectedProtectedAutoSaveName = ''
        $runtimeShapeArguments.ExpectedProtectedAutoSaveSha256 = ''
        $runtimeShapeArguments.ExpectedProtectedQuickSaveName = ''
        $runtimeShapeArguments.ExpectedProtectedQuickSaveSha256 = ''
        $runtimeShape = Assert-KmcQualifiedWorkingProtectedSaveContinuity @runtimeShapeArguments
        Assert-Test ([int]$runtimeShape.schemaVersion -eq 2 -and
            [string]$runtimeShape.protectedSavePinSetSha256 -ceq $epoch.protectedSavePinSetSha256) 'schema-v2 compatibility entry point rejected the runtime launcher explicit-empty legacy parameter shape'
        $record = $preflight.record
        Assert-Test ([string]$record.parentAuthority.path -ceq [IO.Path]::GetFullPath($epoch.parentAuthorityPath)) 'schema-v2 parent path is not exact'
        Assert-Test ([string]$record.parentAuthority.epochId -ceq $epoch.parentEpoch.epochId) 'schema-v2 parent epoch is not exact'
        Assert-Test ([string]$record.parentAuthority.sha256 -ceq $epoch.parentAuthoritySha256) 'schema-v2 parent hash is not exact'
        Assert-Test (@($record.authorizedProtectedTransitions).Count -eq 2) 'schema-v2 authority does not contain exactly two authorized transitions'
        $metadataOnly = @($record.authorizedProtectedTransitions | Where-Object { [string]$_.currentPath -ceq 'Quick_3.zks' })
        $known = @($record.authorizedProtectedTransitions | Where-Object { [string]$_.currentPath -ceq $epoch.parentEpoch.quickName })
        Assert-Test ($metadataOnly.Count -eq 1 -and $null -eq $metadataOnly[0].priorSha256 -and
            [string]$metadataOnly[0].priorHashStatus -ceq 'UNAVAILABLE-SCHEMA-V1-METADATA-ONLY' -and
            [long]$metadataOnly[0].priorLength -eq [long]$epoch.transitions[1].priorLength -and
            [long]$metadataOnly[0].priorLastWriteTimeUtcTicks -eq [long]$epoch.transitions[1].priorLastWriteTimeUtcTicks) 'schema-v2 metadata-only prior evidence is not exact'
        Assert-Test ($known.Count -eq 1 -and [string]$known[0].priorSha256 -ceq $epoch.parentEpoch.quickSha256 -and
            [string]$known[0].currentSha256 -ceq [string]$epoch.transitions[0].currentSha256) 'schema-v2 known prior/current replacement hashes are not exact'
        $parentRecord = Read-KmcJson $epoch.parentAuthorityPath
        $superseded = @($parentRecord.authorizedProtectedTransitions | Where-Object { [string]$_.fileName -ceq $epoch.parentEpoch.quickName })
        Assert-Test ($superseded.Count -eq 1 -and [string]$superseded[0].currentSha256 -ceq $epoch.parentEpoch.quickSha256) 'schema-v2 chain did not preserve the superseded protected pin in immutable history'
        Assert-Test (@($record.writableSaveNames).Count -eq 1 -and [string]@($record.writableSaveNames)[0] -ceq 'KMC_AUTOMATION_WORKING') 'schema-v2 authority grants more than Working-only write authority'
        Assert-Test (@($record.currentProtectedSavePins | Where-Object { [string]$_.path -in @('Quick_3.zks',$epoch.parentEpoch.quickName) }).Count -eq 2) 'schema-v2 quicksaves are not retained as protected content pins'

        $lock = Open-KmcRuntimeLock -StateRoot $fixture.stateRoot -RunId 'schema-v2-under-lock'
        try { $underLock = Assert-KmcQualifiedWorkingProtectedSaveContinuity @continuityArguments }
        finally { Close-KmcRuntimeLock $lock }
        Assert-Test (($preflight.record | ConvertTo-Json -Depth 30 -Compress) -ceq ($underLock.record | ConvertTo-Json -Depth 30 -Compress)) 'schema-v2 preflight and under-lock validation disagree'

        $quickBytes = [IO.File]::ReadAllBytes($epoch.metadataOnlyPath)
        $quickTicks = (Get-Item -LiteralPath $epoch.metadataOnlyPath -Force).LastWriteTimeUtc
        $before = Get-KmcSaveMetadataInventory $fixture.saveRoot
        [IO.File]::AppendAllText($epoch.metadataOnlyPath, '-forbidden-protected-write')
        $threw = $false
        try { Assert-KmcSaveWriteAllowlist -Before $before -After (Get-KmcSaveMetadataInventory $fixture.saveRoot) -WorkingPath $fixture.workingPath | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'schema-v2 protected quicksave was treated as writable'
        [IO.File]::WriteAllBytes($epoch.metadataOnlyPath, $quickBytes)
        [IO.File]::SetLastWriteTimeUtc($epoch.metadataOnlyPath, $quickTicks)

        $workingBytes = [IO.File]::ReadAllBytes($fixture.workingPath)
        $workingTicks = (Get-Item -LiteralPath $fixture.workingPath -Force).LastWriteTimeUtc
        $before = Get-KmcSaveMetadataInventory $fixture.saveRoot
        [IO.File]::AppendAllText($fixture.workingPath, '-authorized-working-only-write')
        $allowlist = Assert-KmcSaveWriteAllowlist -Before $before -After (Get-KmcSaveMetadataInventory $fixture.saveRoot) -WorkingPath $fixture.workingPath
        Assert-Test ([bool]$allowlist.workingChanged -and @($allowlist.changedPaths).Count -eq 1) 'Working-only write authorization rejected exact Working mutation'
        [IO.File]::WriteAllBytes($fixture.workingPath, $workingBytes)
        [IO.File]::SetLastWriteTimeUtc($fixture.workingPath, $workingTicks)
        [void](Assert-KmcQualifiedWorkingProtectedSaveContinuity @continuityArguments)
        $parentAfter = Get-Item -LiteralPath $epoch.parentAuthorityPath -Force
        Assert-Test ((Get-KmcSha256 $epoch.parentAuthorityPath) -ceq $epoch.parentAuthoritySha256 -and
            $parentAfter.Length -eq $epoch.parentAuthorityLength -and
            $parentAfter.LastWriteTimeUtc.Ticks -eq $epoch.parentAuthorityTicks) 'schema-v2 creation or validation mutated historical authority'
    }

    Invoke-HarnessTest 'schema-v2 rejects incomplete, false, renamed, replaced, linked, or extra transition evidence' {
        $prepared = New-TestPreparedChainedProtectedSaveEpoch 'schema-v2-rejections'
        $fixture = $prepared.fixture
        $pair = Assert-KmcFixturePair -SaveRoot $fixture.saveRoot -QualificationPath $fixture.qualificationPath
        $parentArguments = @{
            Path=$prepared.parentAuthorityPath;StateRoot=$fixture.stateRoot;SaveRoot=$fixture.saveRoot
            QualificationPath=$fixture.qualificationPath;ExpectedEpochId=$prepared.parentEpoch.epochId
            ExpectedAuthoritySha256=$prepared.parentAuthoritySha256
            ExpectedCurrentQualificationSha256=$prepared.parentEpoch.qualificationSha256
            ExpectedPriorSaveTransactionStatePath=$fixture.priorSaveTransactionStatePath
            ExpectedPriorSaveTransactionRunId=$fixture.priorSaveTransactionRunId
            ExpectedPriorSaveTransactionStateSha256=$fixture.priorSaveTransactionStateSha256
            ExpectedPriorSaveMetadataDigest=$fixture.priorSaveMetadataDigest
            ExpectedSupersededWorkingSha256=$fixture.supersededWorkingSha256;CurrentPair=$pair
        }
        $parent = Read-KmcHistoricalProtectedSaveContinuityAuthorityV1 @parentArguments
        $inventory = Get-KmcSaveMetadataInventory $fixture.saveRoot
        $recordArguments = @{
            CurrentPair=$pair;ParentAuthority=$parent;CurrentInventory=$inventory;SaveRoot=$fixture.saveRoot
            QualificationPath=$fixture.qualificationPath;CurrentQualificationSha256=$prepared.parentEpoch.qualificationSha256
            EpochId=$prepared.epochId;AuthorizedAtUtc='2026-08-15T00:00:00.0000000+00:00'
        }
        $goodTransitions = @(ConvertFrom-KmcProtectedSaveTransitionSpecificationsJson -Json $prepared.transitionsJson)
        $goodRecord = New-KmcChainedProtectedSaveContinuityAuthorityRecord @recordArguments -Transitions $goodTransitions
        [void](Assert-KmcChainedProtectedSaveContinuityLiveState -Record $goodRecord -SaveRoot $fixture.saveRoot -LiveInventory $inventory)
        $assertRejected = {
            param([scriptblock]$Action,[string]$Message)
            $rejected = $false
            try { & $Action | Out-Null } catch { $rejected = $true }
            Assert-Test $rejected $Message
        }
        $copySpecs = { return @((ConvertTo-Json -InputObject @($prepared.transitions) -Depth 10 -Compress) | ConvertFrom-Json) }

        $specs = & $copySpecs
        $knownIndex = if ([string]$specs[0].priorHashStatus -ceq 'AVAILABLE-PARENT-CONTENT-PIN') { 0 } else { 1 }
        $specs[$knownIndex].priorSha256 = $null
        $specs[$knownIndex].priorHashStatus = 'UNAVAILABLE-SCHEMA-V1-METADATA-ONLY'
        & $assertRejected { ConvertFrom-KmcProtectedSaveTransitionSpecificationsJson -Json (ConvertTo-Json -InputObject @($specs) -Depth 10 -Compress) } 'schema-v2 accepted metadata-only prior status where the parent has a known hash'

        $wrongParent = @{}
        foreach ($key in $parentArguments.Keys) { $wrongParent[$key] = $parentArguments[$key] }
        $wrongParent.ExpectedAuthoritySha256 = '0' * 64
        & $assertRejected { Read-KmcHistoricalProtectedSaveContinuityAuthorityV1 @wrongParent } 'schema-v2 accepted metadata-only evidence without the exact parent authority hash'

        foreach ($field in @('priorLength','priorLastWriteTimeUtcTicks')) {
            $specs = & $copySpecs
            $metadataIndex = if ([string]$specs[0].priorPath -ceq 'Quick_3.zks') { 0 } else { 1 }
            $specs[$metadataIndex].$field = [long]$specs[$metadataIndex].$field + 1
            $parsed = @(ConvertFrom-KmcProtectedSaveTransitionSpecificationsJson -Json (ConvertTo-Json -InputObject @($specs) -Depth 10 -Compress))
            & $assertRejected { New-KmcChainedProtectedSaveContinuityAuthorityRecord @recordArguments -Transitions $parsed } "schema-v2 accepted mismatched metadata-only prior $field"
        }
        $specs = & $copySpecs
        $specs[0].currentSha256 = $null
        & $assertRejected { ConvertFrom-KmcProtectedSaveTransitionSpecificationsJson -Json (ConvertTo-Json -InputObject @($specs) -Depth 10 -Compress) } 'schema-v2 accepted a null current hash'

        $specs = & $copySpecs
        $knownIndex = if ([string]$specs[0].priorHashStatus -ceq 'AVAILABLE-PARENT-CONTENT-PIN') { 0 } else { 1 }
        $specs[$knownIndex].priorSha256 = '0' * 64
        $parsed = @(ConvertFrom-KmcProtectedSaveTransitionSpecificationsJson -Json (ConvertTo-Json -InputObject @($specs) -Depth 10 -Compress))
        & $assertRejected { New-KmcChainedProtectedSaveContinuityAuthorityRecord @recordArguments -Transitions $parsed } 'schema-v2 accepted an incorrect known prior hash'

        $specs = & $copySpecs
        $specs[0].currentLength = [long]$specs[0].currentLength + 1
        $parsed = @(ConvertFrom-KmcProtectedSaveTransitionSpecificationsJson -Json (ConvertTo-Json -InputObject @($specs) -Depth 10 -Compress))
        & $assertRejected { New-KmcChainedProtectedSaveContinuityAuthorityRecord @recordArguments -Transitions $parsed } 'schema-v2 accepted a current length mismatch'
        $specs = & $copySpecs
        $specs[0].currentLastWriteTimeUtcTicks = [long]$specs[0].currentLastWriteTimeUtcTicks + 1
        $parsed = @(ConvertFrom-KmcProtectedSaveTransitionSpecificationsJson -Json (ConvertTo-Json -InputObject @($specs) -Depth 10 -Compress))
        & $assertRejected { New-KmcChainedProtectedSaveContinuityAuthorityRecord @recordArguments -Transitions $parsed } 'schema-v2 accepted a current timestamp mismatch'
        $specs = & $copySpecs
        $specs[0].currentPath = 'Quick_2.zks'
        & $assertRejected { ConvertFrom-KmcProtectedSaveTransitionSpecificationsJson -Json (ConvertTo-Json -InputObject @($specs) -Depth 10 -Compress) } 'schema-v2 accepted a renamed current path'

        $specs = & $copySpecs
        $specs[0].currentSha256 = '0' * 64
        $parsed = @(ConvertFrom-KmcProtectedSaveTransitionSpecificationsJson -Json (ConvertTo-Json -InputObject @($specs) -Depth 10 -Compress))
        $wrongHashRecord = New-KmcChainedProtectedSaveContinuityAuthorityRecord @recordArguments -Transitions $parsed
        & $assertRejected { Assert-KmcChainedProtectedSaveContinuityLiveState -Record $wrongHashRecord -SaveRoot $fixture.saveRoot -LiveInventory $inventory } 'schema-v2 accepted a current content-hash mismatch'

        & $assertRejected { New-KmcChainedProtectedSaveContinuityAuthorityRecord @recordArguments -Transitions @($goodTransitions[0]) } 'schema-v2 accepted omission of one authorized transition'

        $knownBytes = [IO.File]::ReadAllBytes($prepared.knownPath)
        $knownTicks = (Get-Item -LiteralPath $prepared.knownPath -Force).LastWriteTimeUtc
        $replacement = [byte[]]$knownBytes.Clone()
        $replacement[0] = $replacement[0] -bxor 1
        [IO.File]::WriteAllBytes($prepared.knownPath, $replacement)
        [IO.File]::SetLastWriteTimeUtc($prepared.knownPath, $knownTicks)
        & $assertRejected { Assert-KmcChainedProtectedSaveContinuityLiveState -Record $goodRecord -SaveRoot $fixture.saveRoot -LiveInventory (Get-KmcSaveMetadataInventory $fixture.saveRoot) } 'schema-v2 accepted same-metadata byte replacement'
        [IO.File]::WriteAllBytes($prepared.knownPath, $knownBytes)
        [IO.File]::SetLastWriteTimeUtc($prepared.knownPath, $knownTicks)

        $linkBytes = [IO.File]::ReadAllBytes($prepared.metadataOnlyPath)
        $linkTicks = (Get-Item -LiteralPath $prepared.metadataOnlyPath -Force).LastWriteTimeUtc
        $linkTarget = Join-Path $fixture.root 'schema-v2-hardlink-target.bin'
        [IO.File]::WriteAllBytes($linkTarget, $linkBytes)
        [IO.File]::SetLastWriteTimeUtc($linkTarget, $linkTicks)
        [IO.File]::Delete($prepared.metadataOnlyPath)
        New-Item -ItemType HardLink -Path $prepared.metadataOnlyPath -Target $linkTarget | Out-Null
        & $assertRejected { Assert-KmcChainedProtectedSaveContinuityLiveState -Record $goodRecord -SaveRoot $fixture.saveRoot -LiveInventory (Get-KmcSaveMetadataInventory $fixture.saveRoot) } 'schema-v2 accepted hard-link substitution'
        [IO.File]::Delete($prepared.metadataOnlyPath)
        [IO.File]::Delete($linkTarget)
        [IO.File]::WriteAllBytes($prepared.metadataOnlyPath, $linkBytes)
        [IO.File]::SetLastWriteTimeUtc($prepared.metadataOnlyPath, $linkTicks)

        [IO.File]::AppendAllText((Join-Path $fixture.saveRoot 'Manual_3_PERSONAL.zks'), '-unlisted-third-transition')
        $driftInventory = Get-KmcSaveMetadataInventory $fixture.saveRoot
        $driftArguments = @{}
        foreach ($key in $recordArguments.Keys) { $driftArguments[$key] = $recordArguments[$key] }
        $driftArguments.CurrentInventory = $driftInventory
        & $assertRejected { New-KmcChainedProtectedSaveContinuityAuthorityRecord @driftArguments -Transitions $goodTransitions } 'schema-v2 accepted an unlisted third changed path'
        $parentAfter = Get-Item -LiteralPath $prepared.parentAuthorityPath -Force
        Assert-Test ((Get-KmcSha256 $prepared.parentAuthorityPath) -ceq $prepared.parentAuthoritySha256 -and
            $parentAfter.Length -eq $prepared.parentAuthorityLength -and
            $parentAfter.LastWriteTimeUtc.Ticks -eq $prepared.parentAuthorityTicks) 'schema-v2 rejection tests mutated historical authority'
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

    Invoke-HarnessTest 'qualification-suite inventory admits stable between-suite drift and freezes exact in-suite state' {
        $root=Join-Path $testRoot 'suite-inventory'
        $saves=Join-Path $root 'saves';$mods=Join-Path $root 'mods'
        New-Item -ItemType Directory -Path $saves,$mods|Out-Null
        [IO.File]::WriteAllText((Join-Path $saves 'Foreign.zks'),'foreign-one')
        $bagOfTricks=Join-Path $mods 'BagOfTricks'
        New-Item -ItemType Directory -Path $bagOfTricks|Out-Null
        [IO.File]::WriteAllText((Join-Path $bagOfTricks 'Settings.xml'),'first')
        [IO.Directory]::SetLastWriteTimeUtc($bagOfTricks,[datetime]'2026-01-01T00:00:00Z')
        Start-Sleep -Milliseconds 1000
        $suiteOneSave=Get-KmcQualificationTreeInventory -Root $saves -Scope save-root
        $suiteOneMods=Get-KmcQualificationTreeInventory -Root $mods -Scope mods-root
        [void](Assert-KmcQualificationTreeInventorySchema -Inventory $suiteOneSave -ExpectedScope save-root -ExpectedRoot $saves -Description 'suite-one saves')
        [void](Assert-KmcQualificationTreeInventorySchema -Inventory $suiteOneMods -ExpectedScope mods-root -ExpectedRoot $mods -Description 'suite-one Mods')
        Start-Sleep -Milliseconds 1000
        $suiteOneSaveSecond=Get-KmcQualificationTreeInventory -Root $saves -Scope save-root
        $suiteOneModsSecond=Get-KmcQualificationTreeInventory -Root $mods -Scope mods-root
        [void](Assert-KmcQualificationTreeInventoriesEqual -Expected $suiteOneSave -Actual $suiteOneSaveSecond -Description 'stable double-scan saves')
        try { [void](Assert-KmcQualificationTreeInventoriesEqual -Expected $suiteOneMods -Actual $suiteOneModsSecond -Description 'stable double-scan Mods') }
        catch {
            $changed=@(Get-KmcQualificationInventoryDifferences -Before $suiteOneMods -After $suiteOneModsSecond)
            throw "stable double-scan Mods changed at: $($changed -join ', ')."
        }
        [IO.File]::WriteAllText((Join-Path $saves 'Foreign.zks'),'foreign-two')
        [IO.File]::WriteAllText((Join-Path $bagOfTricks 'Settings.xml'),'second')
        [IO.Directory]::SetLastWriteTimeUtc($bagOfTricks,[datetime]'2026-01-01T00:00:01Z')
        Assert-TestThrows { Assert-KmcQualificationTreeInventoriesEqual -Expected $suiteOneSave -Actual (Get-KmcQualificationTreeInventory -Root $saves -Scope save-root) -Description 'in-suite saves' } 'in-suite foreign save drift was accepted'
        Assert-TestThrows { Assert-KmcQualificationTreeInventoriesEqual -Expected $suiteOneMods -Actual (Get-KmcQualificationTreeInventory -Root $mods -Scope mods-root) -Description 'in-suite Mods' } 'in-suite foreign Mods drift was accepted'
        Start-Sleep -Milliseconds 1000
        $suiteTwoSave=Get-KmcQualificationTreeInventory -Root $saves -Scope save-root
        $suiteTwoMods=Get-KmcQualificationTreeInventory -Root $mods -Scope mods-root
        Start-Sleep -Milliseconds 1000
        [void](Assert-KmcQualificationTreeInventoriesEqual -Expected $suiteTwoSave -Actual (Get-KmcQualificationTreeInventory -Root $saves -Scope save-root) -Description 'new stable suite saves')
        [void](Assert-KmcQualificationTreeInventoriesEqual -Expected $suiteTwoMods -Actual (Get-KmcQualificationTreeInventory -Root $mods -Scope mods-root) -Description 'new stable suite Mods')
        Assert-Test ([string]$suiteOneSave.digest-cne[string]$suiteTwoSave.digest -and [string]$suiteOneMods.digest-cne[string]$suiteTwoMods.digest) 'between-suite drift did not produce a new exact admission identity'
    }

    Invoke-HarnessTest 'suite admission double-scans before append-only commit and WhatIf remains read-only' {
        $source=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\New-KmcQualificationSuiteSnapshot.ps1')
        $saveFirst=$source.IndexOf('$saveFirst=Get-KmcQualificationTreeInventory',[StringComparison]::Ordinal)
        $sleep=$source.IndexOf('Start-Sleep -Milliseconds $StabilityIntervalMilliseconds',[StringComparison]::Ordinal)
        $saveSecond=$source.IndexOf('$saveSecond=Get-KmcQualificationTreeInventory',[StringComparison]::Ordinal)
        $equality=$source.IndexOf("-Description 'qualification-suite double-scan save inventory'",[StringComparison]::Ordinal)
        $shouldProcess=$source.IndexOf('$PSCmdlet.ShouldProcess($snapshotPath',[StringComparison]::Ordinal)
        $write=$source.IndexOf('Write-KmcJsonCreateNewDurable -Path $snapshotPath',[StringComparison]::Ordinal)
        Assert-Test ($source.Contains('$requestedWhatIf=[bool]$WhatIfPreference') -and $source.Contains('$WhatIfPreference=$false') -and
            $saveFirst-ge0-and$sleep-gt$saveFirst-and$saveSecond-gt$sleep-and$equality-gt$saveSecond-and$shouldProcess-gt$equality-and$write-gt$shouldProcess) 'suite admission does not double-scan and validate before ShouldProcess and append-only write'
        Assert-Test ($source.Contains("foreignSavesWritable=`$false") -and $source.Contains("foreignModsWritable=`$false") -and
            $source.Contains("writableSaveNames=@('KMC_AUTOMATION_WORKING')")) 'suite snapshot grants foreign write authority'
    }

    Invoke-HarnessTest 'qualification-suite inventory rejects add remove rename replacement metadata and links' {
        $root=Join-Path $testRoot 'suite-drift-kinds';New-Item -ItemType Directory -Path $root|Out-Null
        $file=Join-Path $root 'Foreign.zks';[IO.File]::WriteAllText($file,'abcd')
        $original=Get-KmcQualificationTreeInventory -Root $root -Scope save-root
        [IO.File]::WriteAllText((Join-Path $root 'Added.zks'),'x')
        Assert-TestThrows { Assert-KmcQualificationTreeInventoriesEqual $original (Get-KmcQualificationTreeInventory $root save-root) 'added path' } 'added path passed'
        Remove-Item -LiteralPath (Join-Path $root 'Added.zks')
        Move-Item -LiteralPath $file -Destination (Join-Path $root 'Renamed.zks')
        Assert-TestThrows { Assert-KmcQualificationTreeInventoriesEqual $original (Get-KmcQualificationTreeInventory $root save-root) 'rename' } 'rename passed'
        Move-Item -LiteralPath (Join-Path $root 'Renamed.zks') -Destination $file
        $ticks=(Get-Item $file).LastWriteTimeUtc.Ticks;[IO.File]::WriteAllText($file,'wxyz');(Get-Item $file).LastWriteTimeUtc=[DateTime]::new($ticks,[DateTimeKind]::Utc)
        Assert-TestThrows { Assert-KmcQualificationTreeInventoriesEqual $original (Get-KmcQualificationTreeInventory $root save-root) 'same length and timestamp replacement' } 'same-length hash replacement passed'
        [IO.File]::WriteAllText($file,'abcd');(Get-Item $file).LastWriteTimeUtc=[DateTime]::new($ticks+10000000,[DateTimeKind]::Utc)
        Assert-TestThrows { Assert-KmcQualificationTreeInventoriesEqual $original (Get-KmcQualificationTreeInventory $root save-root) 'timestamp drift' } 'timestamp drift passed'
        Remove-Item -LiteralPath $file
        Assert-TestThrows { Assert-KmcQualificationTreeInventoriesEqual $original (Get-KmcQualificationTreeInventory $root save-root) 'removed path' } 'removed path passed'
        [IO.File]::WriteAllText($file,'abcd')
        $hard=Join-Path $root 'Hard.zks';New-Item -ItemType HardLink -Path $hard -Target $file|Out-Null
        Assert-TestThrows { Get-KmcQualificationTreeInventory -Root $root -Scope save-root } 'hard link passed suite inventory'
    }

    Invoke-HarnessTest 'A/B identity cannot cross qualification-suite snapshots' {
        [void](Assert-KmcSameQualificationSuiteIdentity -First ([pscustomobject]@{suiteId='suite-a';snapshotSha256=('a'*64)}) -Second ([pscustomobject]@{suiteId='suite-a';snapshotSha256=('a'*64)}))
        Assert-TestThrows { Assert-KmcSameQualificationSuiteIdentity -First ([pscustomobject]@{suiteId='suite-a';snapshotSha256=('a'*64)}) -Second ([pscustomobject]@{suiteId='suite-b';snapshotSha256=('a'*64)}) } 'A/B suite-ID mismatch passed'
        Assert-TestThrows { Assert-KmcSameQualificationSuiteIdentity -First ([pscustomobject]@{suiteId='suite-a';snapshotSha256=('a'*64)}) -Second ([pscustomobject]@{suiteId='suite-a';snapshotSha256=('b'*64)}) } 'A/B snapshot-hash mismatch passed'
        Assert-Test ((Get-KmcQualificationSuiteDriftDisposition -ExternalStateExact $false -PermanentFixtureExact $true -TransactionActive $false -PriorProcessRestorationProven $true)-ceq'close-suite-and-restart-fresh-ab') 'between-run drift does not force an automatic fresh-suite A/B restart'
        Assert-Test ((Get-KmcQualificationSuiteDriftDisposition -ExternalStateExact $false -PermanentFixtureExact $true -TransactionActive $true -PriorProcessRestorationProven $false)-ceq'stop-unproven-active-transaction-drift') 'active-transaction drift was treated as ordinary between-suite activity'
        Assert-Test ((Get-KmcQualificationSuiteDriftDisposition -ExternalStateExact $true -PermanentFixtureExact $false -TransactionActive $false -PriorProcessRestorationProven $true)-ceq'stop-kmc-fixture-drift') 'KMC fixture drift was admitted by suite restart'
    }


    Invoke-HarnessTest 'qualification-suite historical authority hashes are immutable' {
        $state=Join-Path $testRoot 'suite-history-state';$authorityRoot=Join-Path $state 'protected-save-authorities';New-Item -ItemType Directory -Path $authorityRoot -Force|Out-Null
        $one=Join-Path $authorityRoot 'one.json';$two=Join-Path $authorityRoot 'two.json';[IO.File]::WriteAllText($one,'one');[IO.File]::WriteAllText($two,'two')
        $history=[pscustomobject]@{protectedSaveAuthorities=@([pscustomobject]@{classification='historical-suite-authority';path=$one;sha256=Get-KmcSha256 $one;epochId='one';schemaVersion=1},[pscustomobject]@{classification='historical-transition-authority';path=$two;sha256=Get-KmcSha256 $two;epochId='two';schemaVersion=2});modsAuthorities=@([pscustomobject]@{classification='historical-suite-authority';digest=('a'*64);description='immutable historical Mods digest'})}
        [void](Assert-KmcQualificationSuiteHistoricalAuthorities -History $history -StateRoot $state)
        [IO.File]::WriteAllText($one,'eno')
        Assert-TestThrows { Assert-KmcQualificationSuiteHistoricalAuthorities -History $history -StateRoot $state } 'modified historical authority passed its immutable hash'
    }

    Invoke-HarnessTest 'combined transaction durably binds one qualification-suite snapshot' {
        $root=Join-Path $testRoot 'suite-transaction-binding';$state=Join-Path $root 'state';$mods=Join-Path $root 'mods';$saves=Join-Path $root 'saves'
        New-Item -ItemType Directory -Path $state,$mods,$saves|Out-Null
        $lock=Open-KmcRuntimeLock -StateRoot $state -RunId suite-bound-transaction
        try{
            $path=New-KmcRunTransactionState -Lock $lock -Mode save-backed-v3-suite -LiveModsRoot $mods -SaveRoot $saves -StateRoot $state -ModsBefore (Get-KmcDirectoryManifest $mods) -SavesBefore (Get-KmcSaveMetadataInventory $saves) -QualificationSuiteSnapshotPath (Join-Path $state 'qualification-suite-snapshots\suite-a.json') -QualificationSuiteId suite-a -QualificationSuiteSnapshotSha256 ('a'*64)
            $record=Read-KmcRunTransactionState -StatePath $path -Lock $lock
            Assert-Test ([long]$record.schemaVersion-eq2 -and [string]$record.mode-ceq'save-backed-v3-suite' -and [string]$record.qualificationSuiteId-ceq'suite-a' -and [string]$record.qualificationSuiteSnapshotSha256-ceq('a'*64)) 'combined transaction lost suite binding'
        }finally{Close-KmcRuntimeLock $lock}
        $legacyLock=Open-KmcRuntimeLock -StateRoot $state -RunId historical-schema-one
        try{
            $legacyPath=New-KmcRunTransactionState -Lock $legacyLock -Mode save-backed-v2 -LiveModsRoot $mods -SaveRoot $saves -StateRoot $state -ModsBefore (Get-KmcDirectoryManifest $mods) -SavesBefore (Get-KmcSaveMetadataInventory $saves)
            Assert-Test ([long](Read-KmcRunTransactionState -StatePath $legacyPath -Lock $legacyLock).schemaVersion-eq1) 'historical combined transaction schema was rewritten'
        }finally{Close-KmcRuntimeLock $legacyLock}
        $incompleteLock=Open-KmcRuntimeLock -StateRoot $state -RunId incomplete-suite-binding
        try{Assert-TestThrows { New-KmcRunTransactionState -Lock $incompleteLock -Mode save-backed-v3-suite -LiveModsRoot $mods -SaveRoot $saves -StateRoot $state -ModsBefore (Get-KmcDirectoryManifest $mods) -SavesBefore (Get-KmcSaveMetadataInventory $saves) -QualificationSuiteId suite-a } 'incomplete suite binding was accepted'}finally{Close-KmcRuntimeLock $incompleteLock}
    }

    Invoke-HarnessTest 'qualified Working recovery is exact KMC-only and WhatIf-pure' {
        $root=Join-Path $testRoot 'qualified-working-recovery';$saves=Join-Path $root 'saves';$state=Join-Path $root 'state';$backup=Join-Path $root 'backups';$staging=Join-Path $root 'staging'
        New-Item -ItemType Directory -Path $saves,$state,$backup,$staging|Out-Null
        $baselinePath=Join-Path $saves 'Manual_1_KMC_AUTOMATION_BASELINE.zks';$workingPath=Join-Path $saves 'Manual_2_KMC_AUTOMATION_WORKING.zks';$foreignPath=Join-Path $saves 'Manual_3_KBP_AUTOMATION_WORKING.zks'
        New-TestSaveArchive -Path $baselinePath -Name 'KMC_AUTOMATION_BASELINE';New-TestSaveArchive -Path $workingPath -Name 'KMC_AUTOMATION_WORKING';[IO.File]::WriteAllText($foreignPath,'foreign-owned')
        $qualificationPath=Join-Path $state 'fixture-qualification.json';$pair=Assert-KmcFixturePair -SaveRoot $saves -QualificationPath $qualificationPath -InitializeQualification
        $lock=Open-KmcRuntimeLock -StateRoot $state -RunId 'qualified-backup-source'
        try{$saveState=Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $pair -SaveRoot $saves -StateRoot $state -BackupRoot $backup -StagingRoot $staging -Scenario fixture-intake;[void](Restore-KmcWorkingSaveTransaction -Lock $lock -StatePath $saveState -SaveRoot $saves -BackupRoot $backup -StagingRoot $staging)}finally{Close-KmcRuntimeLock $lock}
        $authority=[pscustomobject]@{baseline=$pair.baseline;working=$pair.working}
        New-TestSaveArchive -Path $workingPath -Name 'KMC_AUTOMATION_WORKING' -ExtraEntry
        $driftHash=Get-KmcSha256 $workingPath;$foreignHash=Get-KmcSha256 $foreignPath
        $whatIf=Invoke-KmcQualifiedWorkingFixtureRecovery -RunId recovery-whatif -SaveRoot $saves -StateRoot $state -BackupRoot $backup -QualificationPath $qualificationPath -HistoricalAuthority $authority -WhatIf
        Assert-Test ([string]$whatIf.status-ceq'what-if' -and (Get-KmcSha256 $workingPath)-ceq$driftHash -and (Get-KmcSha256 $foreignPath)-ceq$foreignHash -and -not(Test-Path (Join-Path $state 'fixture-recoveries'))) 'fixture-recovery WhatIf mutated state'
        $result=Invoke-KmcQualifiedWorkingFixtureRecovery -RunId recovery-commit -SaveRoot $saves -StateRoot $state -BackupRoot $backup -QualificationPath $qualificationPath -HistoricalAuthority $authority -Confirm:$false
        $recovered=Assert-KmcFixturePair -SaveRoot $saves -QualificationPath $qualificationPath
        Assert-Test ([string]$result.status-ceq'recovered' -and [string]$recovered.working.sha256-ceq[string]$pair.working.sha256 -and (Get-KmcSha256 $foreignPath)-ceq$foreignHash -and (Test-Path -LiteralPath $result.quarantinePath)) 'qualified recovery did not restore only exact Working'
        New-TestSaveArchive -Path $baselinePath -Name 'KMC_AUTOMATION_BASELINE' -ExtraEntry
        Assert-TestThrows { Invoke-KmcQualifiedWorkingFixtureRecovery -RunId baseline-must-stop -SaveRoot $saves -StateRoot $state -BackupRoot $backup -QualificationPath $qualificationPath -HistoricalAuthority $authority -Confirm:$false } 'changed Baseline was automatically repaired without a qualified Baseline backup contract'
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

    Invoke-HarnessTest 'manual review fixture payload is path-free and read-only' {
        $payloadRoot = Join-Path $testRoot 'manual-review-payload-saves'
        New-Item -ItemType Directory -Path $payloadRoot | Out-Null
        New-TestSaveArchive -Path (Join-Path $payloadRoot 'Manual_1_KMC_AUTOMATION_BASELINE.zks') -Name 'KMC_AUTOMATION_BASELINE'
        New-TestSaveArchive -Path (Join-Path $payloadRoot 'Manual_2_KMC_AUTOMATION_WORKING.zks') -Name 'KMC_AUTOMATION_WORKING'
        $payload = New-KmcRuntimeFixturePayload (Get-KmcValidatedFixturePair $payloadRoot) -ReadOnly
        Assert-Test (@($payload.Keys).Count -eq 3) 'read-only fixture payload property count differs'
        Assert-Test (@($payload.baseline.Keys + $payload.working.Keys | Where-Object { $_ -in @('path','kind','schemaVersion') }).Count -eq 0) 'read-only fixture payload disclosed a host path or guard-only field'
        Assert-Test ([string]$payload.writeAuthorization.mode -ceq 'read-only' -and
            $null -eq $payload.writeAuthorization.allowedInternalName -and
            $null -eq $payload.writeAuthorization.allowedFileName -and
            $payload.writeAuthorization.baselineImmutable) 'manual review fixture payload is not exact read-only authorization'
    }

    Invoke-HarnessTest 'manual review launcher delegates only to the guarded transactional runtime path' {
        $manualLauncherSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\Invoke-KingmakerManualReview.ps1')
        $runtimeLauncherSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\Invoke-KingmakerRuntimeScenario.ps1')
        $manualSessionSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeManualReviewSession.cs')
        Assert-Test ($manualLauncherSource.Contains("Scenario = 'manual-visual-review'") -and
            $manualLauncherSource.Contains("Invoke-KingmakerRuntimeScenario.ps1") -and
            $manualLauncherSource.Contains('$requestedWhatIf = [bool]$WhatIfPreference') -and
            $manualLauncherSource.Contains('$WhatIfPreference = $false') -and
            $manualLauncherSource.Contains('QualificationSuiteSnapshotPath') -and
            $manualLauncherSource.Contains('ExpectedQualificationSuiteId') -and
            $manualLauncherSource.Contains('ExpectedQualificationSuiteSnapshotSha256') -and
            $manualLauncherSource.Contains('ExpectedPackageSha256') -and
            $manualLauncherSource.Contains('ExpectedPackageManifestSha256') -and
            $manualLauncherSource.Contains('ExpectedDllSha256') -and
            $manualLauncherSource.Contains('ExpectedBranch') -and
            $manualLauncherSource.Contains('ExpectedCommit') -and
            $manualLauncherSource.Contains("if (`$requestedWhatIf) { `$invoke['WhatIf'] = `$true }") -and
            -not $manualLauncherSource.Contains('Start-Process') -and -not $manualLauncherSource.Contains('Stop-Process')) 'manual launcher does not exclusively delegate to the guarded runtime launcher'
        Assert-Test ($runtimeLauncherSource.Contains('New-KmcRuntimeFixturePayload $preflightPair -ReadOnly:$isManualReview') -and
            $runtimeLauncherSource.Contains("'waiting-for-manual-review-ready'") -and
            $runtimeLauncherSource.Contains("'manual-review-ready'") -and
            $runtimeLauncherSource.Contains('Kingmaker process attribution changed during manual review') -and
            $runtimeLauncherSource.Contains('Restore-KmcRuntimeTransactions') -and
            $runtimeLauncherSource.Contains("visualAcceptance='PENDING'") -and
            -not $runtimeLauncherSource.Contains('Stop-Process')) 'guarded runtime launcher lacks exact interactive READY, pending-acceptance, wait, or restoration boundaries'
        Assert-Test ($manualSessionSource.Contains('if (!ValidateReadOnlyBoundary())') -and
            $manualSessionSource.Contains('saveAuthorization.AuthorizedWriteCount != 0') -and
            $manualSessionSource.Contains('game.Player.MainCharacter.Value == null') -and
            $manualSessionSource.Contains('relationship.MountAutomationPair()') -and
            $manualSessionSource.Contains('VisualAcceptance = "PENDING"') -and
            $manualSessionSource.Contains('ManualReviewBoundaryDecision.BeginProcessTeardown') -and
            $manualSessionSource.Contains('ManualReviewFixtureBoundary.Invalid') -and
            $manualSessionSource.Contains('Application.Quit();')) 'in-game manual review session lacks exact read-only, mount, pending-acceptance, or failure-quit behavior'
    }

    Invoke-HarnessTest 'manual review request READY and restored-result validators bind exact read-only evidence' {
        $manualEvidence = Join-Path $runtimeEvidenceTestRoot 'manual-review-validator'
        New-Item -ItemType Directory -Path $manualEvidence | Out-Null
        $manualFixture = [ordered]@{
            baseline = [ordered]@{internalName='KMC_AUTOMATION_BASELINE';fileName='Manual_1_KMC_AUTOMATION_BASELINE.zks';sha256=('a'*64);length=101L;lastWriteTimeUtcTicks=638907120000000000L;gameId='11111111-2222-3333-4444-555555555555';gameName='KMC Fixture';area='0123456789abcdef0123456789abcdef'}
            working = [ordered]@{internalName='KMC_AUTOMATION_WORKING';fileName='Manual_2_KMC_AUTOMATION_WORKING.zks';sha256=('b'*64);length=202L;lastWriteTimeUtcTicks=638907120010000000L;gameId='11111111-2222-3333-4444-555555555555';gameName='KMC Fixture';area='0123456789abcdef0123456789abcdef'}
            writeAuthorization = [ordered]@{mode='read-only';allowedInternalName=$null;allowedFileName=$null;baselineImmutable=$true}
        }
        $manualRequest = [ordered]@{
            schemaVersion=2;runId='manual-review-validator';scenario='manual-visual-review';branch='codex/mounted-combat-phase2-alpha';
            commit=('c'*40);productVersion=$currentProductVersion;dllSha256=('d'*64);dllMvid='aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';
            transactionToken=('e'*64);evidenceRoot=$manualEvidence;fixture=$manualFixture
            qualificationSuite=[ordered]@{suiteId='manual-suite';snapshotSha256=('f'*64)}
        }
        $manualRequestPath = Join-Path $manualEvidence 'runtime-request.json'
        Write-KmcJsonAtomic $manualRequestPath $manualRequest
        $requestValidated = $false
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $manualRequestPath
        $requestValidated = $true
        Assert-Test $requestValidated 'read-only manual review request validator did not pass exact schema-v2 evidence'

        $manualManifest = [ordered]@{schemaVersion=2;branch=$manualRequest.branch;commit=$manualRequest.commit;version=$manualRequest.productVersion;dllSha256=$manualRequest.dllSha256;dllMvid=$manualRequest.dllMvid;worktreeClean=$true;qualificationEligible=$true}
        $manualManifestPath = Join-Path $manualEvidence 'package.manifest.json'
        Write-KmcJsonAtomic $manualManifestPath $manualManifest
        $ready = [ordered]@{
            schemaVersion=1;evidenceKind='manual-visual-review-ready';runId=$manualRequest.runId;scenario=$manualRequest.scenario;status='READY';
            branch=$manualRequest.branch;commit=$manualRequest.commit;productVersion=$manualRequest.productVersion;dllSha256=$manualRequest.dllSha256;dllMvid=$manualRequest.dllMvid;
            transactionToken=$manualRequest.transactionToken;readyAtUtc='2026-08-15T14:00:01Z';loadedModId='KingmakerMountedCombat';gameVersion='2.1.7b';
            processId=4242;currentGameMode='Default';loadedAreaGuid=$manualFixture.working.area;fixtureIdentityVerified=$true;
            workingInternalName='KMC_AUTOMATION_WORKING';workingFileName=$manualFixture.working.fileName;saveWriteMode='read-only';
            loadRequestCount=1;saveRequestCount=0;authorizedLoadCount=1;authorizedWriteCount=0;unauthorizedLoadCount=0;unauthorizedWriteCount=0;
            relationshipState='Mounted';movementExperimentEnabled=$true;riderId='rider-id';mountId='mount-id';mountBlueprintGuid='e7aa96d15a45238438ae4cfb476f6bb9';
            selectedUnitIds=@('rider-id');actionLabel='Dismount';actionVisible=$true;actionEnabled=$true;poseProfileId='medium-humanoid-mammoth-v1';
            poseHealthy=$true;poseFrameApplied=$true;poseBoneCount=7;poseComponentCount=1;visualAcceptance='PENDING'
        }
        $readyPath = Join-Path $manualEvidence 'manual-review-ready.json'
        Write-KmcJsonAtomic $readyPath $ready
        $readyValidated = $false
        & (Join-Path $PSScriptRoot 'runtime\Test-KmcManualReviewReady.ps1') -ReadyPath $readyPath -RequestPath $manualRequestPath -PackageManifestPath $manualManifestPath -ExpectedProcessId 4242 -NotBeforeUtc ([DateTimeOffset]'2026-08-15T14:00:00Z')
        $readyValidated = $true
        Assert-Test $readyValidated 'manual review READY validator did not pass exact evidence'

        $manualResult = [ordered]@{
            schemaVersion=1;evidenceKind='manual-visual-review-session';runId=$manualRequest.runId;scenario=$manualRequest.scenario;status='PASS';
            branch=$manualRequest.branch;commit=$manualRequest.commit;productVersion=$manualRequest.productVersion;dllSha256=$manualRequest.dllSha256;dllMvid=$manualRequest.dllMvid;
            transactionToken=$manualRequest.transactionToken;startedAtUtc='2026-08-15T14:00:00Z';completedAtUtc='2026-08-15T14:05:00Z';
            reviewReady=$true;readyAtUtc=$ready.readyAtUtc;readyEvidenceSha256=(Get-KmcSha256 $readyPath);visualAcceptance='PENDING';processExited=$true;
            modsRestored=$true;saveProtectionPassed=$true;baselineImmutable=$true;workingRestored=$true;saveWriteAllowlistPassed=$true;
            restoredSaveInventoryDigest=('f'*64);errors=@()
        }
        $manualResultPath = Join-Path $manualEvidence 'manual-review-result.json'
        Write-KmcJsonAtomic $manualResultPath $manualResult
        $resultValidated = $false
        & (Join-Path $PSScriptRoot 'runtime\Test-KmcManualReviewResult.ps1') -ResultPath $manualResultPath -RequestPath $manualRequestPath
        $resultValidated = $true
        Assert-Test $resultValidated 'manual review restored-result validator did not pass exact evidence'

        $ready.visualAcceptance = 'ACCEPTED'
        [IO.File]::WriteAllText($readyPath, ($ready | ConvertTo-Json -Depth 20), (New-Object Text.UTF8Encoding($false)))
        $rejected = $false
        try { & (Join-Path $PSScriptRoot 'runtime\Test-KmcManualReviewReady.ps1') -ReadyPath $readyPath -RequestPath $manualRequestPath -PackageManifestPath $manualManifestPath -ExpectedProcessId 4242 -NotBeforeUtc ([DateTimeOffset]'2026-08-15T14:00:00Z') | Out-Null }
        catch { $rejected = $true }
        Assert-Test $rejected 'manual review READY validator accepted fabricated visual acceptance'
    }

    Invoke-HarnessTest 'runtime request bytes are bound to the launched process' {
        $launcherSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\Invoke-KingmakerRuntimeScenario.ps1')
        $hostSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeAutomationHost.cs')
        Assert-Test ($launcherSource.Contains("'-kmcRuntimeRequestSha256',`$requestHash")) 'launcher does not pass the exact request-file SHA-256'
        Assert-Test ($hostSource.Contains('RequestHashArgument = "-kmcRuntimeRequestSha256"')) 'in-process host does not require the request SHA-256 argument'
        Assert-Test ($hostSource.Contains('ComputeSha256(requestBytes)')) 'in-process host does not hash the exact bytes it deserializes'
    }

    Invoke-HarnessTest 'runtime launcher suite pins fail closed before approval, lock, staging, and evidence' {
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
        $v2PinNames = @($pinNames | Where-Object {
            $_ -cnotin @('ExpectedProtectedAutoSaveName','ExpectedProtectedAutoSaveSha256','ExpectedProtectedQuickSaveName','ExpectedProtectedQuickSaveSha256')
        }) + @('ExpectedProtectedSavePinSetSha256')
        $v2PinArguments = @{}
        foreach ($key in $allPinArguments.Keys) {
            if ($key -cnotin @('ExpectedProtectedAutoSaveName','ExpectedProtectedAutoSaveSha256','ExpectedProtectedQuickSaveName','ExpectedProtectedQuickSaveSha256')) {
                $v2PinArguments[$key] = $allPinArguments[$key]
            }
        }
        $v2PinArguments.BoundContinuityPinNames = $v2PinNames
        $v2PinArguments.ExpectedProtectedSavePinSetSha256 = '2' * 64
        [void](Assert-KmcRuntimeContinuityPinCombination @v2PinArguments)
        $dualModeArguments = @{}
        foreach ($key in $allPinArguments.Keys) { $dualModeArguments[$key] = $allPinArguments[$key] }
        $dualModeArguments.BoundContinuityPinNames = @($pinNames + 'ExpectedProtectedSavePinSetSha256')
        $dualModeArguments.ExpectedProtectedSavePinSetSha256 = '2' * 64
        $threw = $false
        try { Assert-KmcRuntimeContinuityPinCombination @dualModeArguments | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'save-backed runtime pin gate accepted simultaneous schema-v1 and schema-v2 pin modes'
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

        $artifactPinNames = @('ExpectedPackageSha256','ExpectedPackageManifestSha256','ExpectedDllSha256','ExpectedBranch','ExpectedCommit')
        $artifactPinArguments = @{
            IsManualReview=$true;BoundArtifactPinNames=$artifactPinNames
            ExpectedPackageSha256='3'*64;ExpectedPackageManifestSha256='4'*64;ExpectedDllSha256='5'*64
            ExpectedBranch='codex/mounted-combat-phase2-alpha';ExpectedCommit='6'*40
        }
        [void](Assert-KmcManualReviewArtifactPinCombination @artifactPinArguments)
        $missingArtifactArguments = @{}
        foreach ($key in $artifactPinArguments.Keys) { $missingArtifactArguments[$key] = $artifactPinArguments[$key] }
        $missingArtifactArguments.BoundArtifactPinNames = @($artifactPinNames | Where-Object { $_ -cne 'ExpectedDllSha256' })
        $threw = $false
        try { Assert-KmcManualReviewArtifactPinCombination @missingArtifactArguments | Out-Null } catch { $threw = $true }
        Assert-Test $threw 'manual-review artifact gate accepted a syntactically missing DLL pin'
        $threw = $false
        try {
            Assert-KmcManualReviewArtifactPinCombination -IsManualReview $false `
                -BoundArtifactPinNames @('ExpectedCommit') -ExpectedCommit ('6'*40) | Out-Null
        } catch { $threw = $true }
        Assert-Test $threw 'non-manual runtime gate accepted a manual-review artifact pin'

        $launcherSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\Invoke-KingmakerRuntimeScenario.ps1')
        foreach ($pinName in @($pinNames + 'ExpectedProtectedSavePinSetSha256')) {
            Assert-Test ($launcherSource -cmatch ('\$' + [regex]::Escape($pinName) + '(?:\s|,)')) "runtime launcher does not expose $pinName"
        }
        foreach ($suitePinName in @('QualificationSuiteSnapshotPath','ExpectedQualificationSuiteId','ExpectedQualificationSuiteSnapshotSha256')) {
            Assert-Test ($launcherSource -cmatch ('\$' + [regex]::Escape($suitePinName) + '(?:\s|,)')) "runtime launcher does not expose $suitePinName"
        }
        $pinGateIndex = $launcherSource.IndexOf('if($isSaveBacked -and ($boundSuitePinNames.Count-ne3', [StringComparison]::Ordinal)
        $artifactPinGateIndex = $launcherSource.IndexOf('[void](Assert-KmcManualReviewArtifactPinCombination', [StringComparison]::Ordinal)
        $validateSourceIndex = $launcherSource.IndexOf("& (Join-Path `$repoRoot 'scripts\Validate-Source.ps1')", [StringComparison]::Ordinal)
        $shouldProcessIndex = $launcherSource.IndexOf("if(-not `$PSCmdlet.ShouldProcess", [StringComparison]::Ordinal)
        $lockIndex = $launcherSource.IndexOf('    $lock=Open-KmcRuntimeLock', [StringComparison]::Ordinal)
        $combinedStateIndex = $launcherSource.IndexOf('    $combinedStatePath=New-KmcRunTransactionState', [StringComparison]::Ordinal)
        $enterSaveIndex = $launcherSource.IndexOf('        [void](Enter-KmcWorkingSaveTransaction', [StringComparison]::Ordinal)
        $enterModsIndex = $launcherSource.IndexOf('    [void](Enter-KmcModsTransaction', [StringComparison]::Ordinal)
        $continuityCalls = @([regex]::Matches(
            $launcherSource,
            '(?m)^\s*\$(?:preflightContinuity|whatIfContinuity|lockedContinuity)=Assert-KmcQualificationSuiteContinuity'))
        $postRestorationAuditIndex = $launcherSource.IndexOf('[void](Assert-KmcQualificationSuiteContinuity', $combinedStateIndex, [StringComparison]::Ordinal)
        Assert-Test ($pinGateIndex -ge 0 -and $pinGateIndex -lt $validateSourceIndex -and $pinGateIndex -lt $shouldProcessIndex) `
            'runtime launcher does not reject incomplete/no-save pin combinations before validation or ShouldProcess'
        Assert-Test ($artifactPinGateIndex -gt $pinGateIndex -and $artifactPinGateIndex -lt $validateSourceIndex -and
            $launcherSource.Contains("(Get-KmcSha256 `$PackagePath)-cne`$ExpectedPackageSha256") -and
            $launcherSource.Contains("(Get-KmcSha256 `$packageManifestPath)-cne`$ExpectedPackageManifestSha256")) `
            'runtime launcher does not bind manual package/manifest/DLL/branch/commit pins before approval'
        Assert-Test ($continuityCalls.Count -eq 3) 'runtime launcher does not perform exactly preflight, WhatIf, and locked continuity proofs'
        Assert-Test ($continuityCalls[0].Index -lt $shouldProcessIndex -and
            $continuityCalls[1].Index -gt $shouldProcessIndex -and $continuityCalls[1].Index -lt $lockIndex -and
            $continuityCalls[2].Index -gt $lockIndex -and $continuityCalls[2].Index -lt $combinedStateIndex) `
            'runtime launcher continuity proofs are not ordered before approval, during WhatIf, and under lock before durable state'
        Assert-Test ($combinedStateIndex -gt $continuityCalls[2].Index -and
            $enterSaveIndex -gt $combinedStateIndex -and $enterModsIndex -gt $enterSaveIndex) `
            'runtime launcher can stage durable run state, Mods, or Working before locked continuity succeeds'
        Assert-Test ($postRestorationAuditIndex -gt $enterModsIndex -and
            $launcherSource.IndexOf("Qualification-suite post-restoration audit failed", $postRestorationAuditIndex, [StringComparison]::Ordinal) -gt $postRestorationAuditIndex) `
            'runtime launcher does not re-prove the exact suite snapshot after restoration and before evidence credit'
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

    Invoke-HarnessTest 'combat lifecycle source preserves valid combat entry and fails closed on invalidation' {
        $subscriberSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedLifecycleSubscriber.cs')
        $engineSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeLifecycleScenarioEngine.cs')
        Assert-Test ($subscriberSource.Contains('if (inCombat) { Observe(NativeLifecycleBoundary.CombatStarted') -and
            -not $subscriberSource.Contains('Cleanup(NativeLifecycleBoundary.CombatStarted')) 'combat start does not retain a valid mounted pair'
        Assert-Test ($subscriberSource.Contains('else { combat.Cancel("party combat ended"); Observe(NativeLifecycleBoundary.CombatEnded')) 'combat end does not cancel active combat work while retaining the pair'
        Assert-Test ($subscriberSource.Contains('Cleanup(NativeLifecycleBoundary.UnitIncapacitated') -and
            $subscriberSource.Contains('Cleanup(NativeLifecycleBoundary.UnitDeath') -and
            $subscriberSource.Contains('Cleanup(NativeLifecycleBoundary.PartyRemoved') -and
            $subscriberSource.Contains('Cleanup(NativeLifecycleBoundary.ViewDetachedOrUnitDestroyed')) 'pair invalidation is missing an exact fail-closed cleanup boundary'
        Assert-Test ($engineSource.Contains('"combat-lifecycle-suite"') -and
            $engineSource.Contains('? 7') -and
            $engineSource.Contains(': IsCombatLifecycleRow(currentRow ?? lastEvidenceRow) ? 3 : 2') -and
            $engineSource.Contains('BoundaryExercise = IsCombatLifecycleRow') -and
            $engineSource.Contains('UnitEntityData.Damage -> UnitLifeController.TickOnUnit -> IUnitLifeStateChanged.HandleUnitLifeStateChanged') -and
            $subscriberSource.Contains('NativePairLifeStateObservation')) 'combat lifecycle diagnostics do not preserve schema-v2-v6 history while binding native schema-v7 cleanup diagnostics'
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

    Invoke-HarnessTest 'active mounted command isolates exact-pair stock opportunity attacks' {
        $patchSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPatchController.cs')
        $controllerSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedCombatController.cs')
        $policySource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MountedCombatAction.cs')
        Assert-Test ($patchSource.Contains('PatchExact(typeof(UnitCombatState), "AttackOfOpportunity", 0x060093A1, new[] { typeof(UnitEntityData), typeof(bool) }, nameof(PatchMethods.AttackOfOpportunityPrefix));')) 'opportunity isolation does not bind the exact Kingmaker method token and signature'
        Assert-Test ($patchSource.Contains('PatchBridge.Combat.ShouldSuppressStockOpportunityAttack(__instance?.Unit, target)') -and
            $patchSource.Contains('__result = false;')) 'opportunity prefix does not fail closed through the mounted combat controller'
        Assert-Test ($controllerSource.Contains('MountedOpportunityIsolationPolicy.ShouldSuppressStockOpportunityAttack(') -and
            $controllerSource.Contains('relationship.State == RelationshipState.Mounted,') -and
            $controllerSource.Contains('HasActiveCommand,') -and
            $controllerSource.Contains('attacker != null && attacker == relationship.Rider,') -and
            $controllerSource.Contains('attacker != null && attacker == relationship.Mount,')) 'opportunity isolation is not constrained to an active exact mounted-pair command'
        Assert-Test ($policySource.Contains('(attackerIsExactRider || attackerIsExactMount)') -and
            -not $patchSource.Contains('PatchExact(typeof(UnitCombatState), "Disengage"')) 'opportunity isolation changed the broad engagement lifecycle instead of the exact attack emission seam'
    }

    Invoke-HarnessTest 'explicit mounted opportunity feature remains absent and default-off' {
        $productionSource = @(
            Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat') -Recurse -File -Filter '*.cs' |
                Sort-Object FullName |
                ForEach-Object { [IO.File]::ReadAllText($_.FullName) }) -join "`n"
        $patchSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPatchController.cs'))
        Assert-Test (-not $productionSource.Contains('new UnitAttackOfOpportunity') -and
            -not $productionSource.Contains('IsAttackOfOpportunity = true') -and
            -not $patchSource.Contains('PatchExact(typeof(UnitCombatState), "Engage"') -and
            -not $patchSource.Contains('PatchExact(typeof(UnitCombatState), "Disengage"') -and
            -not $patchSource.Contains('PatchExact(typeof(UnitCombatState), "ShouldAttackOnDisengage"')) `
            'production synthesizes mounted opportunities or patches broad engagement ownership despite the default-off stretch disposition'
    }

    Invoke-HarnessTest 'basic mounted charge feature remains absent and default-off' {
        $productionSource = @(
            Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat') -Recurse -File -Filter '*.cs' |
                Sort-Object FullName |
                ForEach-Object { [IO.File]::ReadAllText($_.FullName) }) -join "`n"
        $patchSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPatchController.cs'))
        Assert-Test (-not $productionSource.Contains('AbilityCustomCharge') -and
            -not $productionSource.Contains('IsCharge = true') -and
            -not $productionSource.Contains('ChargeBuff') -and
            -not $productionSource.Contains('IsCharging = true') -and
            -not $patchSource.Contains('PatchExact(typeof(UnitAttack), "set_IsCharge"') -and
            -not $patchSource.Contains('PatchExact(typeof(AbilityCustomCharge)')) `
            'production enables a charge surface or patches stock charge ownership despite the default-off stretch disposition'
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
        $v2Request=[pscustomobject]@{runId='recompute-test';scenario='fixture-intake';branch='codex/mounted-combat-feasibility';commit=('0'*40);productVersion=$currentProductVersion;dllSha256=('a'*64);dllMvid=[Guid]::Empty.ToString();transactionToken=('b'*64);evidenceRoot=$recomputeEvidence;fixture=$fixture}
        $recomputeManifestHash = New-TestArtifactManifest -EvidenceRoot $recomputeEvidence -RunId $v2Request.runId -Scenario $v2Request.scenario
        $game=[pscustomobject]@{status='PASS';fixture=$fixture;evidenceManifestSha256=$recomputeManifestHash;subscenarioTotal=99;subscenarioPassCount=0;subscenarioFailCount=99;assertionPassCount=0;assertionFailCount=99;subscenarioResults=@([pscustomobject]@{name='observe-mount-diagnostic-availability';status='PASS';assertionPassCount=4;assertionFailCount=0;errors=@()})}
        $final=New-KmcRuntimeResultV2 -Request $v2Request -ValidatedGameResult $game -StartedAtUtc ([DateTimeOffset]::UtcNow) -ModsRestored $true -BaselineImmutable $true -WorkingRestored $true -SaveWriteAllowlistPassed $true -RestoredSaveInventoryDigest ('c'*64) -GameResultSha256 ('d'*64)
        Assert-Test ([int]$final.subscenarioTotal -eq 1 -and [int]$final.subscenarioPassCount -eq 1 -and [int]$final.subscenarioFailCount -eq 0) 'final result copied untrusted aggregate subscenario totals'
        Assert-Test ([int]$final.assertionPassCount -eq 4 -and [int]$final.assertionFailCount -eq 0 -and [string]$final.status -ceq 'PASS') 'final result did not recompute assertion totals and status'
        Assert-Test ([string]$final.evidenceManifestSha256 -ceq $recomputeManifestHash) 'final result did not echo the structurally validated game-result evidence manifest hash'
    }

    Invoke-HarnessTest 'schema-v2 fallback creates and binds a validated orchestration artifact manifest' {
        $fallbackEvidence = Join-Path $runtimeEvidenceTestRoot 'fallback-evidence'
        $fallbackRequest=[pscustomobject]@{runId='fallback-test';scenario='fixture-intake';branch='codex/mounted-combat-feasibility';commit=('0'*40);productVersion=$currentProductVersion;dllSha256=('a'*64);dllMvid=[Guid]::Empty.ToString();transactionToken=('b'*64);evidenceRoot=$fallbackEvidence;fixture=[ordered]@{baseline=[ordered]@{};working=[ordered]@{};writeAuthorization=[ordered]@{}}}
        $final=New-KmcRuntimeResultV2 -Request $fallbackRequest -ValidatedGameResult $null -StartedAtUtc ([DateTimeOffset]::UtcNow) -ModsRestored $true -BaselineImmutable $true -WorkingRestored $true -SaveWriteAllowlistPassed $true -RestoredSaveInventoryDigest ('c'*64) -GameResultSha256 $null -Errors @('synthetic missing game result')
        $manifestPath = Join-Path $fallbackEvidence 'runtime-artifacts.json'
        Assert-Test ([string]$final.status -ceq 'FAIL') 'missing game result did not force final FAIL'
        Assert-Test ((Get-KmcSha256 $manifestPath) -ceq [string]$final.evidenceManifestSha256) 'fallback result did not bind the independently created orchestration manifest'
        $manifest = Read-KmcJson $manifestPath
        Assert-Test ($manifest.artifacts -is [Array] -and @($manifest.artifacts).Count -eq 0) 'fallback orchestration manifest is not an exact empty artifact array'
    }

    Invoke-HarnessTest 'combat fallback preserves original launcher error without weakening strict evidence validation' {
        $fallbackEvidence = Join-Path $runtimeEvidenceTestRoot 'combat-fallback-evidence'
        $fallbackFixture = [ordered]@{
            baseline=[ordered]@{internalName='KMC_AUTOMATION_BASELINE';fileName='Manual_1_KMC_AUTOMATION_BASELINE.zks';sha256=('11'*32);length=1;lastWriteTimeUtcTicks=1;gameId='11111111-2222-3333-4444-555555555555';gameName='KMC Test Campaign';area='0123456789abcdef0123456789abcdef'}
            working=[ordered]@{internalName='KMC_AUTOMATION_WORKING';fileName='Manual_2_KMC_AUTOMATION_WORKING.zks';sha256=('22'*32);length=1;lastWriteTimeUtcTicks=1;gameId='11111111-2222-3333-4444-555555555555';gameName='KMC Test Campaign';area='0123456789abcdef0123456789abcdef'}
            writeAuthorization=[ordered]@{mode='working-only';allowedInternalName='KMC_AUTOMATION_WORKING';allowedFileName='Manual_2_KMC_AUTOMATION_WORKING.zks';baselineImmutable=$true}
        }
        $fallbackRequest=[pscustomobject]@{runId='combat-fallback-test';scenario='combat-core-control-suite';branch='codex/mounted-combat-phase2-alpha';commit=('0'*40);productVersion=$currentProductVersion;dllSha256=('a'*64);dllMvid=[Guid]::Empty.ToString();transactionToken=('b'*64);evidenceRoot=$fallbackEvidence;fixture=$fallbackFixture}
        $originalError = 'synthetic attributed launcher failure'
        $final=New-KmcRuntimeResultV2 -Request $fallbackRequest -ValidatedGameResult $null -StartedAtUtc ([DateTimeOffset]::UtcNow) -ModsRestored $false -BaselineImmutable $false -WorkingRestored $false -SaveWriteAllowlistPassed $false -RestoredSaveInventoryDigest ('c'*64) -GameResultSha256 $null -Errors @($originalError)
        Assert-Test ([string]$final.status -ceq 'FAIL' -and @($final.errors).Count -eq 1 -and [string]$final.errors[0] -ceq $originalError) 'combat fallback masked or replaced the original launcher error'
        $manifestPath = Join-Path $fallbackEvidence 'runtime-artifacts.json'
        $manifest = Read-KmcJson $manifestPath
        Assert-Test ($manifest.artifacts -is [Array] -and @($manifest.artifacts).Count -eq 0 -and
            (Get-KmcSha256 $manifestPath) -ceq [string]$final.evidenceManifestSha256) 'combat fallback did not bind one exact empty incomplete manifest'
        $strictRejected = $false
        try { Get-KmcValidatedOrchestrationArtifactManifestHash $fallbackRequest | Out-Null }
        catch { $strictRejected = $true }
        Assert-Test $strictRejected 'ordinary combat artifact validation accepted the incomplete fallback manifest'
    }

    Invoke-HarnessTest 'runtime launcher durably records its first attributed error before process-exit handling' {
        $launcherSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\Invoke-KingmakerRuntimeScenario.ps1')
        $catchIndex = $launcherSource.IndexOf("stage='launcher-error-waiting-for-process-exit'", [StringComparison]::Ordinal)
        $finallyIndex = $launcherSource.IndexOf('finally{', $catchIndex, [StringComparison]::Ordinal)
        Assert-Test ($catchIndex -ge 0 -and $finallyIndex -gt $catchIndex -and
            $launcherSource.Contains('launcherErrorAtUtc') -and $launcherSource.Contains('launcherErrors')) 'launcher does not preserve the original caught error before bounded exit handling'
    }

    Invoke-HarnessTest 'runtime launcher waits boundedly for process identity metadata before exact validation' {
        $launcherSource = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'runtime\Invoke-KingmakerRuntimeScenario.ps1')
        $captureIndex = $launcherSource.IndexOf('$capturedProcessPath=$null', [StringComparison]::Ordinal)
        $metadataDeadlineIndex = $launcherSource.IndexOf('while([DateTimeOffset]::UtcNow-lt$launchDeadline-and[string]::IsNullOrWhiteSpace($capturedProcessPath))', [StringComparison]::Ordinal)
        $exactPathIndex = $launcherSource.IndexOf('[string]::Equals($capturedProcessPath,[IO.Path]::GetFullPath($gameExecutable)', [StringComparison]::Ordinal)
        $exactHashIndex = $launcherSource.IndexOf('(Get-KmcSha256 $capturedProcessPath)-cne$expectedGameExecutableHash', [StringComparison]::Ordinal)
        $waitingIndex = $launcherSource.IndexOf("'waiting-for-game-result'", [StringComparison]::Ordinal)
        Assert-Test ($captureIndex -ge 0 -and $metadataDeadlineIndex -gt $captureIndex -and
            $exactPathIndex -gt $metadataDeadlineIndex -and $exactHashIndex -gt $exactPathIndex -and
            $waitingIndex -gt $exactHashIndex -and -not $launcherSource.Contains('$process.Path.Equals(')) 'launcher does not boundedly await process metadata before exact path/hash admission'
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
        productVersion = $currentProductVersion; dllSha256 = ('ab' * 32)
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
        qualificationSuite=[ordered]@{suiteId='schema-v2-suite';snapshotSha256=('9'*64)}
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

    Invoke-HarnessTest 'runtime request schema accepts exact combat and native-incapacitation lifecycle rows' {
        foreach ($lifecycleScenario in @('combat-lifecycle-suite') + @(Get-KmcCombatLifecycleRuntimeRows) + @(Get-KmcNativeIncapacitationRuntimeRows)) {
            $v2Request.scenario = $lifecycleScenario
            $v2Request.runId = 'schema-v2-' + $lifecycleScenario
            $v2Request.evidenceRoot = Join-Path $runtimeEvidenceTestRoot $v2Request.runId
            Write-KmcJsonAtomic $v2RequestPath $v2Request
            & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $v2RequestPath
        }
        $v2Request.scenario = 'mounted-pair-create-and-clear'
        $v2Request.runId = 'schema-v2-test'
        $v2Request.evidenceRoot = Join-Path $runtimeEvidenceTestRoot 'schema-v2-test'
        Write-KmcJsonAtomic $v2RequestPath $v2Request
    }

    $combatRequestPath = Join-Path $testRoot 'runtime-request-combat.json'

    Invoke-HarnessTest 'combat target source uses isolated group exact native primary raw AI no-loot and zero weapon mutation' {
        $targetSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\DiagnosticCombatTargetService.cs'))
        $targetLifecycleSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\DiagnosticCombatTargetLifecycle.cs'))
        $nonPairLeaseSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\DiagnosticNonPairPartyAiLease.cs'))
        $scopedAiLeaseSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\ScopedDiagnosticAiLease.cs'))
        $engineSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeCombatScenarioEngine.cs'))
        $controllerSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedCombatController.cs'))
        $commandSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPairAttackCommand.cs'))
        $singleAttackSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPairSingleAttack.cs'))
        $ruleProbeSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\MountedCombatRuleProbe.cs'))
        $nativeModeProbeSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\NativeModeTransitionProbe.cs'))
        $patchControllerSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPatchController.cs'))
        $spatialPolicySource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MountedCombatSpatialPolicy.cs'))
        $resolverSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\NativeSingleAttackWeaponResolver.cs'))
        $policySource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MountedCombatAction.cs'))
        $stabilizationPolicySource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MountedStabilizationPolicy.cs'))
        $relationshipSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\GameMountedRelationshipService.cs'))
        $pairRuntimeSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\KingmakerMountedPairRuntime.cs'))
        $lifecycleSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedLifecycleSubscriber.cs'))
        $detachIndex = $targetSource.IndexOf('target.GroupId = runtimeGroupId;', [StringComparison]::Ordinal)
        $factionIndex = $targetSource.IndexOf('target.Descriptor.SwitchFactions(runtimeFaction, true);', [StringComparison]::Ordinal)
        $groupIndex = $targetSource.IndexOf('runtimeGroup = target.Group;', [StringComparison]::Ordinal)
        $groupSnapshotIndex = $targetSource.IndexOf('var groupsBeforeSpawn = groupsController.Groups.Where', [StringComparison]::Ordinal)
        $spawnIndex = $targetSource.IndexOf('target = game.EntityCreator.SpawnUnit', [StringComparison]::Ordinal)
        Assert-Test ($groupSnapshotIndex -ge 0 -and $spawnIndex -gt $groupSnapshotIndex -and
            $detachIndex -gt $spawnIndex -and $factionIndex -gt $detachIndex -and $groupIndex -gt $factionIndex) 'diagnostic target can contaminate its spawn group faction cache before dedicated-group isolation'
        Assert-Test ($targetSource.Contains('groupsBeforeSpawn.Contains(spawnGroup)') -and
            $targetSource.Contains('spawnGroup.IsPlayerParty || !spawnGroup.Empty()')) 'diagnostic target can dispose an unowned or non-empty pre-existing spawn group'
        Assert-Test ($targetSource.Contains('!(bool)AiBackingField.GetValue(target)') -and
            -not $targetSource.Contains('!target.IsAIEnabled')) 'diagnostic target still relies on the always-true non-controllable IsAIEnabled facade instead of its pinned backing state'
        Assert-Test ($targetSource.Contains('target.Inventory == null || !target.Inventory.HasLoot') -and
            -not $targetSource.Contains('target.Inventory.Items.Count == 0')) 'diagnostic target confuses stock non-loot body inventory with loot-bearing inventory'
        Assert-Test ($targetSource.Contains('expectedTarget == null || expectedTarget != target') -and
            $targetSource.Contains('target.IsInFogOfWar = false;') -and
            $targetSource.Contains('target.View.SetVisible(true, true);') -and
            $targetSource.Contains('TargetVisibleForPlayer = target.IsVisibleForPlayer;')) 'diagnostic player-click visibility is not exact-target-only, explicit, and independently verified'
        Assert-Test ($targetSource.Contains('combatMemoryObserverGroup = rider.Group;') -and
            $targetSource.Contains('combatMemoryTargetGroup = target.Group;') -and
            $targetSource.Contains('targetSleeplessBefore = target.Sleepless;') -and
            $targetSource.Contains('target.Sleepless = true;') -and
            $targetSource.Contains('TargetSleeplessLeaseAcquired = !targetSleeplessBefore && targetSleeplessLeaseActive;') -and
            $targetSource.Contains('!targetSleeplessLeaseActive || !combatMemoryTarget.Sleepless') -and
            $targetSource.Contains('current.Sleepless = targetSleeplessBefore;') -and
            $targetSource.Contains('TargetSleeplessLeaseReleased = current.Sleepless == targetSleeplessBefore;') -and
            $targetSource.Contains('combatMemoryObserverGroup.Memory.Add(combatMemoryTarget)') -and
            $targetSource.Contains('combatMemoryTargetGroup.Memory.Add(combatMemoryObserver)') -and
            $targetSource.Contains('observedTarget.LastDetectTime = game.TimeController.GameTime;') -and
            $targetSource.Contains('observedRider.LastDetectTime = game.TimeController.GameTime;') -and
            $targetSource.Contains('if (!combatMemoryObserver.IsAwake)') -and
            $targetSource.Contains('combatMemoryObserver.Wake();') -and
            $targetSource.Contains('if (!combatMemoryTarget.IsAwake)') -and
            $targetSource.Contains('combatMemoryTarget.Wake();') -and
            $targetSource.Contains('combatMemoryObserverGroup.Memory.Remove(combatMemoryTarget);') -and
            $targetSource.Contains('combatMemoryTargetGroup.Memory.Remove(combatMemoryObserver);') -and
            -not $targetSource.Contains('memoryController.AddToMemory(') -and
            $engineSource.Contains('targetService.RefreshBidirectionalCombatMemoryLease()') -and
            $engineSource.Contains('";targetAwake=" + (target != null && target.IsAwake)') -and
            $engineSource.Contains('";targetInFog=" + (target != null && target.IsInFogOfWar)') -and
            $engineSource.Contains('";targetFactionPeaceful=" + (target?.Faction != null && target.Faction.Peaceful)')) 'diagnostic target does not own a deterministic exact-group native combat-memory/sleepless lease with bounded acquisition, refresh, timeout evidence, and symmetric cleanup'
        Assert-Test ($targetSource.Contains('var blueprintPrimary = blueprint.Body?.EmptyHandWeapon;') -and
            $targetSource.Contains('var nativePrimary = NativeSingleAttackWeaponResolver.Resolve(target);') -and
            $targetSource.Contains('NoWeaponProvisioningMutation = AdditionalLimbCountAfter == AdditionalLimbCountBefore') -and
            -not $targetSource.Contains('AddAdditionalLimb(')) 'diagnostic target does not resolve its stock empty-hand weapon through native single-attack order without body mutation'
        $sourceWeaponIndex = $targetSource.IndexOf('var blueprintPrimary = blueprint.Body?.EmptyHandWeapon;', [StringComparison]::Ordinal)
        $runtimeFactionCreateIndex = $targetSource.IndexOf('runtimeFaction = ScriptableObject.CreateInstance<BlueprintFaction>();', [StringComparison]::Ordinal)
        Assert-Test ($sourceWeaponIndex -ge 0 -and $runtimeFactionCreateIndex -gt $sourceWeaponIndex) 'diagnostic target mutates transient Unity state before validating the exact Mammoth empty-hand weapon source'
        Assert-Test ($resolverSource.Contains('Rulebook.Trigger(new RuleCalculateAttacksCount(unit))') -and
            $resolverSource.Contains('NativeSingleAttackSlotPolicy.Select(') -and
            $resolverSource.Contains('body.PrimaryHand != null && body.PrimaryHand.HasWeapon') -and
            $resolverSource.Contains('body.SecondaryHand != null && body.SecondaryHand.HasWeapon')) 'native single-attack resolver is not bound to exact hand attack counts and HasWeapon semantics'
        $primaryPolicyIndex = $policySource.IndexOf('primaryHasWeapon && primaryMainAttacks > 0', [StringComparison]::Ordinal)
        $secondaryPolicyIndex = $policySource.IndexOf('secondaryHasWeapon && secondaryMainAttacks > 0', [StringComparison]::Ordinal)
        $limbPolicyIndex = $policySource.IndexOf('additionalLimbHasWeapon[index]', [StringComparison]::Ordinal)
        Assert-Test ($primaryPolicyIndex -ge 0 -and $secondaryPolicyIndex -gt $primaryPolicyIndex -and $limbPolicyIndex -gt $secondaryPolicyIndex) 'project single-attack policy diverges from native primary-secondary-additional ordering'
        Assert-Test ($controllerSource.Contains('NativeSingleAttackWeaponResolver.Resolve(mount)') -and
            $controllerSource.Contains('NativePrimaryNaturalAttackPolicy.IsExact(') -and
            $policySource.Contains('kind == NativeSingleAttackSlotKind.PrimaryHand') -and
            $policySource.Contains('kind == NativeSingleAttackSlotKind.AdditionalLimb && additionalLimbIndex == 0') -and
            $commandSource.Contains('NativePrimaryNaturalAttackPolicy.IsExact(') -and
            $commandSource.Contains('childAttack.PlannedAttack.Hand != expectedMountPrimary.Slot') -and
            $commandSource.Contains('childAttack.PlannedAttack.Weapon != expectedMountPrimary.Weapon') -and
            $commandSource.Contains('? expectedMountPrimary?.Kind.ToString()') -and
            -not $commandSource.Contains('mount.Body.AdditionalLimbs.FirstOrDefault')) 'mount primary action does not retain and verify the exact primary-hand or first-additional-limb native natural attack across click and child initialization'
        Assert-Test ($commandSource.Contains('var requiresApproach = !childAttack.IsPairEnoughClose;') -and
            $commandSource.Contains('if (!childAttack.TryPrepareNativeStartAdmission())') -and
            $singleAttackSource.Contains('GeometryUtils.MechanicsDistance(mount.Position, target.Position)') -and
            $singleAttackSource.Contains('GeometryUtils.MechanicsDistance(rider.Position, target.Position)') -and
            $singleAttackSource.Contains('MountedCombatSpatialPolicy.TryCalculateNativeExecutorAdmissionRadius(') -and
            $patchControllerSource.Contains('attack.TryCalculateNativeApproachRadius(unit, out radius)') -and
            $spatialPolicySource.Contains('MaximumNativeExecutorRadiusAdjustment = 0.75f') -and
            $spatialPolicySource.Contains('NativeAdmissionEpsilon = 0.001f') -and
            -not $commandSource.Contains('var requiresApproach = !childAttack.IsUnitEnoughClose;')) 'mounted reach does not gate approach on the Mammoth origin before a bounded exact native rider-executor admission bridge'
        $placementRefreshIndex = $engineSource.IndexOf('RetainDiagnosticTargetPlacementAtDispatch()', [StringComparison]::Ordinal)
        $placementCaptureIndex = $engineSource.IndexOf('targetDistanceAtClick = HorizontalDistance(mountPositionAtClick, targetPositionAtClick);', [StringComparison]::Ordinal)
        Assert-Test ($placementRefreshIndex -ge 0 -and $placementCaptureIndex -gt $placementRefreshIndex -and
            $engineSource.Contains('MountedCombatSpatialPolicy.RequiresDiagnosticTargetPlacementRefresh(') -and
            $engineSource.Contains('target.Translocate(refreshedPoint, null);') -and
            $engineSource.Contains('MountedCombatSpatialPolicy.IsBoundedDiagnosticTargetDistance(')) 'combat diagnostic does not repair and revalidate exact observed actor-specific target-placement drift before evidence capture'
        Assert-Test ($commandSource.Contains('MountedPairLivenessSnapshot.IsTargetConsciousnessAdmissible(') -and
            $commandSource.Contains('transaction.ChildAttackStartCount)') -and
            $commandSource.Contains('targetState != null && !targetState.IsFinallyDead') -and
            $commandSource.Contains('attackTarget != null && attackTarget.IsInState')) 'in-flight liveness does not admit target incapacitation only after the exact child starts while preserving final-death and in-state gates'
        Assert-Test ($ruleProbeSource.Contains('IGlobalRulebookHandler<RuleAttackWithWeapon>') -and
            $ruleProbeSource.Contains('IGlobalRulebookHandler<RuleAttackRoll>') -and
            $ruleProbeSource.Contains('IGlobalRulebookHandler<RuleRollDice>') -and
            $ruleProbeSource.Contains('IGlobalRulebookHandler<RuleDealDamage>') -and
            $ruleProbeSource.Contains('subscription = EventBus.Subscribe(this);') -and
            -not $ruleProbeSource.Contains('IRulebookHandler<')) 'combat Rulebook probe is not registered through the exact global Rulebook subscriber surface'
        Assert-Test ($commandSource.Contains('childAttack.IsRunning &&') -and
            $commandSource.Contains('IsActed)') -and
            $commandSource.Contains('HasAnimation = false;') -and
            $commandSource.Contains('return ResultType.None;') -and
            -not $commandSource.Contains('SetIsActed(true);')) 'mounted Standard wrapper bypasses the native false-to-true acted transition or ticks its child before that transition'
        $terminalPolicyIndex = $singleAttackSource.IndexOf('NativeSingleAttackTerminalPolicy.ShouldAwaitNativeAnimation(', [StringComparison]::Ordinal)
        $nativeAttackTickIndex = $singleAttackSource.IndexOf('base.OnTick();', $terminalPolicyIndex, [StringComparison]::Ordinal)
        Assert-Test ($terminalPolicyIndex -ge 0 -and $nativeAttackTickIndex -gt $terminalPolicyIndex -and
            $policySource.Contains('public static bool ShouldAwaitNativeAnimation(') -and
            $policySource.Contains('attackCount == 1') -and
            $policySource.Contains('completedAttackCount == attackCount') -and
            $policySource.Contains('!hasPlannedAttack') -and
            $singleAttackSource.Contains('CombatController.IsInTurnBasedCombat()') -and
            $singleAttackSource.Contains('Result == ResultType.Success') -and
            $singleAttackSource.Contains('LastAttackRule != null') -and
            $singleAttackSource.Contains('GetAttackIndex()') -and
            -not $singleAttackSource.Contains('ForceFinishForTurnBased(')) 'mounted child can enter native UnitAttack nonexistent-next-attack interruption after exact turn-based terminal success'
        Assert-Test ($targetSource.Contains('groupsController.Groups.Remove(runtimeGroup);') -and
            $targetSource.Contains('runtimeGroup.Dispose();') -and
            $targetSource.Contains('!runtimeGroup.Empty()')) 'project-owned transient combat group is not removed only after exact empty-group proof'
        $nonPairLeaseAcquireIndex = $targetSource.IndexOf('nonPairPartyAiLease.Acquire(rider, mount);', [StringComparison]::Ordinal)
        $nonPairTargetSpawnIndex = $targetSource.IndexOf('target = game.EntityCreator.SpawnUnit', [StringComparison]::Ordinal)
        $nonPairTargetRemovalIndex = $targetSource.IndexOf('var nonPairPartyAiClean = targetRemoved && groupRemoved && RuntimeFactionRemoved', [StringComparison]::Ordinal)
        Assert-Test ($nonPairLeaseAcquireIndex -ge 0 -and $nonPairTargetSpawnIndex -gt $nonPairLeaseAcquireIndex -and
            $nonPairTargetRemovalIndex -gt $nonPairTargetSpawnIndex -and
            $targetSource.Contains('nonPairPartyAiLease.RestoreAndVerify()') -and
            $targetSource.Contains('!nonPairPartyAiLease.ValidateActive()') -and
            $nonPairLeaseSource.Contains('group.Count') -and
            $nonPairLeaseSource.Contains('group[index]') -and
            $nonPairLeaseSource.Contains('unit.Commands.Empty') -and
            $nonPairLeaseSource.Contains('unit.IsDirectlyControllable') -and
            $nonPairLeaseSource.Contains('AiBackingField.GetValue(unit)') -and
            -not $nonPairLeaseSource.Contains('.Commands.Interrupt') -and
            -not $nonPairLeaseSource.Contains('.Commands.Clear') -and
            $scopedAiLeaseSource.Contains('CommandsEmptyBefore') -and
            $scopedAiLeaseSource.Contains('RawAiDuring') -and
            $scopedAiLeaseSource.Contains('public void Restore(IEnumerable<TUnit> currentUnits)')) 'diagnostic combat does not lease the exact non-pair player group before target creation, preserve empty commands, validate raw/effective AI suppression, and restore after target removal'
        Assert-Test ($targetSource.Contains('runtimeFactionDestroyPending = true;') -and
            $targetSource.Contains('runtimeFactionDestroyPending = false;') -and
            $targetSource.Contains('UnityEngine.Object.Destroy(runtimeFaction);')) 'runtime faction destruction is not retained and verified across the deferred Unity destruction boundary'
        $prepareTargetIndex = $targetSource.IndexOf('public bool PrepareForPlayerClick(UnitEntityData expectedTarget)', [StringComparison]::Ordinal)
        $targetInterruptIndex = $targetSource.IndexOf('target.Commands.InterruptAll();', $prepareTargetIndex, [StringComparison]::Ordinal)
        $targetFinishedDrainIndex = $targetSource.IndexOf('target.Commands.RemoveFinishedAndUpdateQueue();', $prepareTargetIndex, [StringComparison]::Ordinal)
        $targetStopIndex = $targetSource.IndexOf('target.View.AgentASP.Stop();', $prepareTargetIndex, [StringComparison]::Ordinal)
        $targetStoppedGateIndex = $targetSource.IndexOf('TargetAgentStoppedAtClick = !target.View.AgentASP.WantsToMove', [StringComparison]::Ordinal)
        $targetCleanupRetryIndex = $targetSource.IndexOf('if (State == DiagnosticCombatTargetState.DestroyRequested)', [StringComparison]::Ordinal)
        $targetCleanupConfirmIndex = $targetSource.IndexOf('return lifecycle.ConfirmRemoved(lifecycle.TargetId, true);', [StringComparison]::Ordinal)
        Assert-Test ($prepareTargetIndex -ge 0 -and $targetInterruptIndex -gt $prepareTargetIndex -and
            $targetFinishedDrainIndex -gt $targetInterruptIndex -and $targetStopIndex -gt $targetFinishedDrainIndex -and
            $targetStoppedGateIndex -gt $targetStopIndex -and
            $targetSource.Contains('TargetCommandsEmptyAtClick = target.Commands.Empty;') -and
            $targetSource.Contains('TargetAgentEnabledAtClick = target.View.AgentASP.enabled;') -and
            $targetSource.Contains('target.View.AgentASP.Speed == 0f') -and
            $targetSource.Contains('target.View.AgentASP.Velocity.sqrMagnitude == 0f') -and
            $targetCleanupRetryIndex -ge 0 -and $targetCleanupConfirmIndex -gt $targetCleanupRetryIndex) 'diagnostic target does not clear and prove the exact residual movement path before click or retry deferred zero-residue lifecycle confirmation'
        $combatValidatorSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'scripts\runtime\RuntimeHarness.Common.ps1'))
        $prepareClickIndex = $engineSource.IndexOf('targetService.PrepareForPlayerClick(target)', [StringComparison]::Ordinal)
        $nativeClickIndex = $engineSource.IndexOf('new ClickUnitHandler().OnClick(', [StringComparison]::Ordinal)
        Assert-Test ($prepareClickIndex -ge 0 -and $nativeClickIndex -gt $prepareClickIndex -and
            $engineSource.Contains('if (!clickAccepted || combat.ArmedAction != MountedCombatActionKind.None || !combat.HasActiveCommand)')) 'combat runtime does not prepare exact target visibility and stop immediately after a rejected Harmony click'
        Assert-Test ($controllerSource.Contains('DescribeActiveCommandReadiness()') -and
            $controllerSource.Contains('commands.Standard == command') -and
            $controllerSource.Contains('commands.Queue.Contains(command)') -and
            $controllerSource.Contains('actionActor.AreHandsBusyWithAnimation') -and
            $controllerSource.Contains('handsEquipment.IsUpdateScheduledFor(actionActor)') -and
            $controllerSource.Contains('actionActor.CombatState.HasCooldownForCommand(command)') -and
            $engineSource.Contains('Command readiness: " + combat.DescribeActiveCommandReadiness()')) 'combat timeout does not preserve exact native command admission and start-gate evidence'
        Assert-Test ($controllerSource.Contains('actionActor.Commands.Run(command);') -and
            $controllerSource.Contains('command.Executor != actionActor') -and
            $controllerSource.Contains('!actionActor.Commands.Contains(command) &&') -and
            $controllerSource.Contains('!actionActor.Commands.Queue.Contains(command)')) 'combat click accepts a command that the exact action actor native UnitCommands neither owns nor queues'
        $pauseCaptureIndex = $engineSource.IndexOf('originalPause = Game.Instance.IsPaused;', [StringComparison]::Ordinal)
        $memoryQueueIndex = $engineSource.IndexOf('targetService.QueueBidirectionalCombatMemory(rider, target)', [StringComparison]::Ordinal)
        $realTimeUnpauseIndex = $engineSource.IndexOf('game.IsPaused = false;', [StringComparison]::Ordinal)
        $nativeJoinIndex = $engineSource.IndexOf('new DiagnosticNativeCombatJoinReadinessSnapshot(', [StringComparison]::Ordinal)
        $nativeEntryIndex = $engineSource.IndexOf('new DiagnosticCombatEntryReadinessSnapshot(', [StringComparison]::Ordinal)
        $nativeDispatchIndex = $engineSource.IndexOf('new DiagnosticCombatDispatchReadinessSnapshot(', [StringComparison]::Ordinal)
        $nativeClickAfterPauseIndex = $engineSource.IndexOf('new ClickUnitHandler().OnClick(', [StringComparison]::Ordinal)
        Assert-Test ($pauseCaptureIndex -ge 0 -and $memoryQueueIndex -gt $pauseCaptureIndex -and
            $realTimeUnpauseIndex -gt $memoryQueueIndex -and $nativeJoinIndex -gt $realTimeUnpauseIndex -and
            $nativeEntryIndex -gt $nativeJoinIndex -and
            $nativeDispatchIndex -gt $nativeEntryIndex -and
            $nativeClickAfterPauseIndex -gt $nativeDispatchIndex -and
            $engineSource.Contains('if (!dispatchReadiness.AllPassed)') -and
            $engineSource.Contains('RestorePause();') -and
            -not $engineSource.Contains('target.JoinCombat();') -and
            -not $engineSource.Contains('rider.JoinCombat();') -and
            -not $engineSource.Contains('mount.JoinCombat();')) 'real-time combat does not lease native memory, await native combat entry, unpause, await exact dispatch readiness, and restore pause without manual JoinCombat'
        Assert-Test ($engineSource.Contains('MemoryEnemiesContain(rider, target)') -and
            $engineSource.Contains('MemoryEnemiesContain(target, rider)') -and
            $engineSource.Contains('(bool)riderState.IsIgnoredByCombat') -and
            $engineSource.Contains('(bool)mountState.IsIgnoredByCombat') -and
            $engineSource.Contains('(bool)targetState.IsIgnoredByCombat') -and
            $engineSource.Contains('nativeJoinReadiness?.FailureSummary') -and
            $engineSource.Contains('Every exact native UnitCombatJoinController eligibility gate remained healthy at dispatch.')) 'native combat-entry diagnosis does not bind the exact in-game, conscious, ignored, group, enemy-list, fog, and ambush gates before dispatch'
        $realTimeModeProbeIndex = $engineSource.IndexOf('realTimeBaselineModeProbe = new NativeModeTransitionProbe(false);', [StringComparison]::Ordinal)
        $realTimeModeDispatchIndex = $engineSource.IndexOf('realTimeBaselineModeProbe.DispatchTemporaryValueIfRequired();', [StringComparison]::Ordinal)
        $modeProbeIndex = $engineSource.IndexOf('turnBasedModeProbe = new NativeModeTransitionProbe(true);', [StringComparison]::Ordinal)
        $modeDispatchIndex = $engineSource.IndexOf('turnBasedModeProbe.DispatchTemporaryValueIfRequired();', [StringComparison]::Ordinal)
        $turnBasedMountIndex = $engineSource.IndexOf('private void AwaitTurnBasedModeAndMount()', [StringComparison]::Ordinal)
        $turnBasedMountEndIndex = $engineSource.IndexOf('private void ResolveAndMountPair()', $turnBasedMountIndex, [StringComparison]::Ordinal)
        $turnBasedMountBody = if ($turnBasedMountIndex -ge 0 -and $turnBasedMountEndIndex -gt $turnBasedMountIndex) {
            $engineSource.Substring($turnBasedMountIndex, $turnBasedMountEndIndex - $turnBasedMountIndex)
        } else { '' }
        $turnRosterIndex = $engineSource.IndexOf('turnRosterContainsTarget = ContainsTurnRosterUnit(turnController, target);', [StringComparison]::Ordinal)
        $nativeActionActorTurnIndex = $engineSource.IndexOf('turnController.StartTurn(AttackActor);', [StringComparison]::Ordinal)
        $turnDispatchIndex = $engineSource.IndexOf('turnBasedReadiness = CaptureTurnBasedReadiness(turnController);', $nativeActionActorTurnIndex, [StringComparison]::Ordinal)
        $nativeClickIndex = $engineSource.IndexOf('new ClickUnitHandler().OnClick(', $turnDispatchIndex, [StringComparison]::Ordinal)
        $actingAfterDispatchIndex = $engineSource.IndexOf('if (IsTurnBasedRow && !nativeActionActorTurnActingObservedAfterDispatch)', [StringComparison]::Ordinal)
        $beginCleanupIndex = $engineSource.IndexOf('private void BeginCleanup()', [StringComparison]::Ordinal)
        $transitionRestoreIndex = $engineSource.IndexOf('RestoreTurnBasedTransitionLease();', $beginCleanupIndex, [StringComparison]::Ordinal)
        $awaitRealtimeRestoreIndex = $engineSource.IndexOf('private void AwaitTurnBasedRealtimeRestore()', $transitionRestoreIndex, [StringComparison]::Ordinal)
        $describeRealtimeRestoreIndex = $engineSource.IndexOf('private string DescribeTurnBasedRestoreState()', $awaitRealtimeRestoreIndex, [StringComparison]::Ordinal)
        $awaitRealtimeRestoreBody = if ($awaitRealtimeRestoreIndex -ge 0 -and $describeRealtimeRestoreIndex -gt $awaitRealtimeRestoreIndex) {
            $engineSource.Substring($awaitRealtimeRestoreIndex, $describeRealtimeRestoreIndex - $awaitRealtimeRestoreIndex)
        } else { '' }
        $nativeRealtimePauseIndex = $engineSource.IndexOf('game.CurrentMode == GameModeType.Pause && game.IsPaused && !originalPause', $awaitRealtimeRestoreIndex, [StringComparison]::Ordinal)
        $nativeRealtimePauseObservedIndex = $engineSource.IndexOf('nativeRealtimePauseObserved = true;', $nativeRealtimePauseIndex, [StringComparison]::Ordinal)
        $realtimeTransitionUnpauseIndex = $engineSource.IndexOf('game.IsPaused = false;', $nativeRealtimePauseObservedIndex, [StringComparison]::Ordinal)
        $realtimeUnpauseRequestedIndex = $engineSource.IndexOf('realtimeUnpauseRequested = true;', $realtimeTransitionUnpauseIndex, [StringComparison]::Ordinal)
        $realtimePresentationIndex = $engineSource.IndexOf('presentationAfterRealtimeRestore = relationship.CapturePresentationObservation();', $awaitRealtimeRestoreIndex, [StringComparison]::Ordinal)
        $relationshipCleanupIndex = $engineSource.IndexOf('private void BeginRelationshipCleanup()', $realtimePresentationIndex, [StringComparison]::Ordinal)
        $cleanupDestroyIndex = $engineSource.IndexOf('targetRemoved = targetService.DestroyAndVerify();', $relationshipCleanupIndex, [StringComparison]::Ordinal)
        $modeRestoreIndex = $engineSource.IndexOf('RestoreTurnBasedMode();', $cleanupDestroyIndex, [StringComparison]::Ordinal)
        $cameraCaptureIndex = $engineSource.IndexOf('cameraFollowerSnapshot = CombatCameraFollowerSnapshot.TryCapture(', [StringComparison]::Ordinal)
        $cameraFollowIndex = $engineSource.IndexOf('Game.Instance.CameraController.Follower.Follow(rider)', $cameraCaptureIndex, [StringComparison]::Ordinal)
        $mountPairIndex = $engineSource.IndexOf('var mounted = relationship.MountAutomationPair();', $cameraFollowIndex, [StringComparison]::Ordinal)
        $cameraRestoreIndex = $engineSource.IndexOf('RestoreCameraFollower();', $modeRestoreIndex, [StringComparison]::Ordinal)
        Assert-Test ($realTimeModeProbeIndex -ge 0 -and $realTimeModeDispatchIndex -gt $realTimeModeProbeIndex -and
            $modeProbeIndex -gt $realTimeModeDispatchIndex -and $modeDispatchIndex -gt $modeProbeIndex -and
            $turnBasedMountIndex -gt $modeDispatchIndex -and $turnRosterIndex -gt $turnBasedMountIndex -and
            $nativeActionActorTurnIndex -gt $turnRosterIndex -and $turnDispatchIndex -gt $nativeActionActorTurnIndex -and
            $nativeClickIndex -gt $turnDispatchIndex -and $actingAfterDispatchIndex -gt $nativeClickIndex -and
            $beginCleanupIndex -gt $turnDispatchIndex -and $transitionRestoreIndex -gt $beginCleanupIndex -and
            $awaitRealtimeRestoreIndex -gt $transitionRestoreIndex -and
            $nativeRealtimePauseIndex -gt $awaitRealtimeRestoreIndex -and
            $nativeRealtimePauseObservedIndex -gt $nativeRealtimePauseIndex -and
            $realtimeTransitionUnpauseIndex -gt $nativeRealtimePauseObservedIndex -and
            $realtimeUnpauseRequestedIndex -gt $realtimeTransitionUnpauseIndex -and
            $realtimePresentationIndex -gt $realtimeUnpauseRequestedIndex -and
            $relationshipCleanupIndex -gt $realtimePresentationIndex -and $cleanupDestroyIndex -gt $relationshipCleanupIndex -and
            $modeRestoreIndex -gt $cleanupDestroyIndex -and $cameraCaptureIndex -gt $realTimeModeDispatchIndex -and
            $cameraFollowIndex -gt $cameraCaptureIndex -and $mountPairIndex -gt $cameraFollowIndex -and
            $cameraRestoreIndex -gt $modeRestoreIndex -and
            $nativeModeProbeSource.Contains('public NativeModeTransitionProbe(bool temporaryValue)') -and
            $nativeModeProbeSource.Contains('TemporaryValue = requestedTemporaryValue ?? !OriginalValue;') -and
            $nativeModeProbeSource.Contains('public bool TransitionRequired => OriginalValue != TemporaryValue;') -and
            $nativeModeProbeSource.Contains('public bool TemporaryValueIsCurrent => setting.CurrentValue == TemporaryValue;') -and
            $nativeModeProbeSource.Contains('public bool CurrentValue => setting.CurrentValue;') -and
            $nativeModeProbeSource.Contains('public bool? CurrentRawCacheValue => (bool?)cachedField.GetValue(setting);') -and
            $nativeModeProbeSource.Contains('public void DispatchTemporaryValueIfRequired()') -and
            $turnBasedMountBody.Contains('!turnBasedModeProbe.TemporaryValueIsCurrent') -and
            -not $turnBasedMountBody.Contains('!CombatController.IsInTurnBasedCombat()') -and
            $engineSource.Contains('CombatController.IsInTurnBasedCombat()') -and
            $engineSource.Contains('turnController != null && turnController.Initialized') -and
            $engineSource.Contains('foreach (var unit in controller.SortedUnits)') -and
            $engineSource.Contains('currentTurn?.Unit == AttackActor') -and
            $spatialPolicySource.Contains('public static bool CanIssueRiderAction(') -and
            $spatialPolicySource.Contains('public static bool CanIssueAction(') -and
            $spatialPolicySource.Contains('(currentUnitIsExactActor && (actorTurnIsPreparing || actorTurnIsActing))') -and
            $controllerSource.Contains('var actionActorTurn = MountedPairTurnPolicy.CanIssueAction(') -and
            $controllerSource.Contains('turn.Status == TurnBased.Controllers.TurnController.TurnStatus.Preparing') -and
            $controllerSource.Contains('turn != null && turn.IsActing') -and
            $engineSource.Contains('MountedPairTurnPolicy.CanIssueAction(') -and
            $engineSource.Contains('currentTurn.Status == TurnBased.Controllers.TurnController.TurnStatus.Preparing') -and
            $engineSource.Contains('currentTurn != null && currentTurn.IsActing') -and
            $engineSource.Contains('currentTurnActingAtDispatch = true;') -and
            $engineSource.Contains('currentTurnActingAtOutcome = currentTurn != null && currentTurn.IsActing') -and
            $controllerSource.Contains('turn.ForceToEnd(false);') -and
            $engineSource.Contains('step = CombatEngineStep.AwaitTurnBasedRealtimeRestore;') -and
            $engineSource.Contains('game.CurrentMode != GameModeType.Default') -and
            $engineSource.Contains('DescribeTurnBasedRestoreState()') -and
            $engineSource.Contains('nativeRealtimePauseObserved && realtimeUnpauseRequested') -and
            $engineSource.Contains('relationship.NativeTurnBasedExitUiLeaseRestoreAttemptCount == 0') -and
            $engineSource.Contains('relationship.NativeTurnBasedExitUiLeaseRestoreMutationCount == 1') -and
            $engineSource.Contains('relationship.NativeTurnBasedExitUiLeaseRestoreResult, "reselected-rider"') -and
            -not $awaitRealtimeRestoreBody.Contains('game.Player.IsInCombat') -and
            $engineSource.Contains('IsMammothPrimaryRow || IsApproachRow || IsMountedBeforeModeTransitionRow') -and
            $engineSource.Contains('private string presentationAfterRealtimeRestore = "<not-observed>";') -and
            $lifecycleSource.Contains('service.ObserveNativeTurnBasedModeChanged(enabled);') -and
            $relationshipSource.Contains('NativeTurnBasedExitAiLeasePolicy.Classify(') -and
            $relationshipSource.Contains('controller != null && controller.Initialized,') -and
            $relationshipSource.Contains('runtime.ReassertMountAiLeaseAfterNativeTurnBasedExit()') -and
            $stabilizationPolicySource.Contains('NativeTurnBasedExitAiLeaseDisposition.AwaitNativeControllerClear') -and
            $stabilizationPolicySource.Contains('!relationshipMounted || !mountAiLeaseOwned') -and
            $relationshipSource.Contains('NativeTurnBasedExitUiLeasePolicy.Classify(') -and
            $relationshipSource.Contains('selection.SelectUnit(rider.View, true, true, false);') -and
            $stabilizationPolicySource.Contains('NativeTurnBasedExitUiLeaseDisposition.AwaitNativeRealtimeBoundary') -and
            $stabilizationPolicySource.Contains('!relationshipMounted || !exactCapturedRiderView') -and
            $pairRuntimeSource.Contains('mount.IsAIEnabled = false;') -and
            $pairRuntimeSource.Contains('return !(bool)MammothAiBackingField.GetValue(mount);') -and
            $engineSource.Contains('IsRiderUiOwnershipCoherent(presentationAfterTurnBasedEnable, false)') -and
            $engineSource.Contains('IsRiderUiOwnershipCoherent(presentationAfterRealtimeRestore, false)') -and
            $engineSource.Contains('observation.IndexOf("cameraOn=" + expectedCameraOn') -and
            $engineSource.Contains('settingCurrent=') -and
            $engineSource.Contains(';rawCache=') -and
            $engineSource.Contains(';controllerInitialized=') -and
            $engineSource.Contains('turnBasedRestoreDeliveryCompleted &&') -and
            $engineSource.Contains('turnBasedOriginalEnabled,') -and
            $engineSource.Contains('turnBasedTemporaryEnabled,') -and
            $engineSource.Contains('turnBasedOriginalRawCacheHadValue,') -and
            $engineSource.Contains('turnBasedRestoreDeliveryCompleted,') -and
            $engineSource.Contains('private sealed class CombatCameraFollowerSnapshot') -and
            $engineSource.Contains('exactUnit.FieldType != typeof(UnitEntityData)') -and
            $engineSource.Contains('cameraFollowerRestored = cameraFollowerSnapshot.IsCurrent;') -and
            $engineSource.Contains('realTimeBaselineModeProbe.DispatchRestoreAndRestoreRawCache();') -and
            $engineSource.Contains('turnBasedPersistedUnchanged &&') -and
            $engineSource.Contains('realTimePersistedUnchanged;')) 'combat rows do not lease exact native mode and camera state, observe a bounded Default-mode TB-to-RT checkpoint before relationship cleanup, admit the native action-actor turn, and restore every captured lease after cleanup'
        Assert-Test ($engineSource.Contains('CleanupTimeoutSeconds = 10.0d') -and
            $engineSource.Contains('rowClock.Elapsed.TotalSeconds - cleanupStartedAtSeconds < CleanupTimeoutSeconds')) 'combat cleanup does not retain an independent bounded drain after a row deadline'
        Assert-Test ($engineSource.Contains('SchemaVersion = IsHumanPlayRow') -and
            $engineSource.Contains('? (IsTurnBasedRow ? 52 : 48)') -and
            $engineSource.Contains(': IsCommandTerminationRow') -and
            $engineSource.Contains('? IsCombatEndTerminationRow') -and
            $engineSource.Contains('? (IsTurnBasedRow ? 41 : 40)') -and
            $engineSource.Contains(': (IsTurnBasedRow ? 39 : 38)') -and
            $engineSource.Contains(': IsMovementToAttackRow') -and
            $engineSource.Contains('? (IsTurnBasedRow ? 35 : 34)') -and
            $engineSource.Contains(': (IsTurnBasedRow ? 27 : 26)') -and
            $engineSource.Contains('Mode = IsTurnBasedRow ? "turn-based" : "real-time"') -and
            $engineSource.Contains('TurnBased = IsTurnBasedRow') -and
            $engineSource.Contains('CombatEntry = CombatEntryEvidence.From(') -and
            $engineSource.Contains('DiagnosticCombatActionActorReadinessSnapshot') -and
            $engineSource.Contains('actionActor.CombatState.Cooldown.Initiative') -and
            $targetLifecycleSource.Contains('actorInitiative <= MaximumPreparedInitiative + InitiativeTolerance') -and
            $targetLifecycleSource.Contains('(turnBased || Math.Abs(actorInitiative) <= InitiativeTolerance)') -and
            $engineSource.Contains('TerminalReason = value.TerminalReason') -and
            $engineSource.Contains('Dispatch = CombatDispatchEvidence.From(') -and
            $combatValidatorSource.Contains("'memoryQueued','playerGroupMemoryContainsTarget','targetGroupMemoryContainsRider'") -and
            $combatValidatorSource.Contains("'targetAwake'") -and
            $combatValidatorSource.Contains("'defaultGameMode','memoryRemovedAtCleanup'") -and
            $combatValidatorSource.Contains("'actionActorId','actionActorPrepared','actionActorCanActInCombat','actionActorInitiative'") -and
            $combatValidatorSource.Contains("[string]`$record.combatEntry.actionActorId -cne `$expectedActorId") -and
            $combatValidatorSource.Contains("-not `$turnBasedScenario -and [Math]::Abs(`$actionActorInitiative) -gt 0.000001") -and
            $combatValidatorSource.Contains("@('actionActorCanActInCombat','actionActorHandsBusy')") -and
            $combatValidatorSource.Contains("'equipmentControllerAvailable','equipmentUpdateScheduled','pauseRestored'") -and
            $combatValidatorSource.Contains("[string]`$record.command.terminalReason -cne 'completed'") -and
            $combatValidatorSource.Contains("'controllerInitialized','rosterContainsRider','rosterContainsMount','rosterContainsTarget'") -and
            $combatValidatorSource.Contains("[string]`$record.turnBased.currentTurnUnitIdAtDispatch -cne `$expectedActorId") -and
            $combatValidatorSource.Contains("`$record.turnBased.persistedValueUnchanged -ne `$true") -and
            $ruleProbeSource.Contains('LastAttackHit = evt.IsHit;') -and
            $engineSource.Contains('IsNativeAcMissReason(ruleProbe.LastAttackResult)') -and
            $combatValidatorSource.Contains("'lastAttackHit'") -and
            $combatValidatorSource.Contains("`$record.rules.lastAttackHit -ne `$false") -and
            $combatValidatorSource.Contains("@('Miss','DodgeAC','ArmorAC','ShieldAC')") -and
            $combatValidatorSource.Contains("'sleeplessBefore','sleeplessLeaseAcquired'") -and
            $combatValidatorSource.Contains("'sleeplessLeaseReleased'") -and
            $combatValidatorSource.Contains("'temporaryHitPointsBefore','temporaryHitPointsAfterProvisioning'") -and
            $combatValidatorSource.Contains("'durabilityLeaseAmount','durabilityLeaseAcquired'") -and
            $combatValidatorSource.Contains("'durabilityLeaseReleased'") -and
            $combatValidatorSource.Contains("'brainActiveBefore','leaseAcquired','effectiveAiEnabledDuring','violationObserved'") -and
            $combatValidatorSource.Contains("'suppressedAtClick','suppressedAtOutcome','brainActiveAfterRelease','leaseReleased'") -and
            $combatValidatorSource.Contains("'brainLeaseReleased'") -and
            $combatValidatorSource.Contains("'riderDisplacementAtOutcome','mountDisplacementAtOutcome','targetDisplacementAtOutcome'") -and
            $combatValidatorSource.Contains("'playerGroupEnemiesContainsTarget','targetGroupEnemiesContainsRider'") -and
            $combatValidatorSource.Contains("'riderIgnoredByCombat','mountIgnoredByCombat','targetIgnoredByCombat'") -and
            $engineSource.Contains('TargetLife = CombatTargetLifeEvidence.From(targetService)') -and
            $targetSource.Contains('IUnitLifeStateChanged') -and
            $targetSource.Contains('LifeImmediatelyAfterCreation = DiagnosticTargetLifeSnapshot.Capture(target);') -and
            $targetSource.Contains('LifeAtActivation = DiagnosticTargetLifeSnapshot.Capture(target);') -and
            $engineSource.Contains('targetService.CaptureCurrentLife(target)') -and
            $targetSource.Contains('FirstLifeTransition = new DiagnosticTargetLifeTransition(') -and
            $combatValidatorSource.Contains("'immediatelyAfterCreation','atActivation','lastObserved','transitionCount','firstTransition'") -and
            $engineSource.Contains('TargetIncomingRules = CombatTargetIncomingRulesEvidence.From(targetService)') -and
            $targetSource.Contains('IGlobalRulebookHandler<RuleAttackWithWeapon>') -and
            $targetSource.Contains('IGlobalRulebookHandler<RuleDealDamage>') -and
            $targetSource.Contains('PreDispatchIncomingAttackRuleCount++') -and
            $targetSource.Contains('PreDispatchIncomingDamageRuleCount++') -and
            $combatValidatorSource.Contains("'dispatchMarkerSet','attackRuleCount','damageRuleCount','preDispatchAttackRuleCount'") -and
            $combatValidatorSource.Contains('initiatorDirectlyControllable') -and
            $engineSource.Contains('NonPairPartyAiLease = CombatNonPairPartyAiLeaseEvidence.From(targetService)') -and
            $engineSource.Contains('NonPairPartyAiLeaseRestored = targetNonPairPartyAiLeaseRestored') -and
            $engineSource.Contains('TargetBrainLease = CombatTargetBrainLeaseEvidence.From(targetService)') -and
            $combatValidatorSource.Contains("'acquired','groupId','groupIsPlayerParty','riderSharesGroup','mountSharesGroup','memberCount'") -and
            $combatValidatorSource.Contains("'commandsEmptyBefore','rawAiBefore','effectiveAiBefore'") -and
            $combatValidatorSource.Contains('command-preserving non-pair party AI suppression') -and
            $combatValidatorSource.Contains('zero pre-dispatch interference') -and
            $combatValidatorSource.Contains("'commandOwnerId','resourceOwnerId','actionStandardCharged'") -and
            $combatValidatorSource.Contains("'attackWeaponBlueprintId','attackWeaponIsNatural','attackWeaponIsRanged','attackWeaponSlot'") -and
            $commandSource.Contains('Executor == actionActor') -and
            $commandSource.Contains('CommandOwnerId = Executor?.UniqueId') -and
            $commandSource.Contains('ResourceOwnerId = actionActor.UniqueId') -and
            $commandSource.Contains('retainedAttackWeaponBlueprintId = childAttack.PlannedAttack.Weapon.Blueprint.AssetGuid;') -and
            $commandSource.Contains('AttackWeaponBlueprintId = retainedAttackWeaponBlueprintId') -and
            $commandSource.Contains('mount.Commands.Run(delegatedMove);') -and
            $commandSource.Contains('mount.Commands.GetCommand(UnitCommand.CommandType.Move)') -and
            $commandSource.Contains('IsExactRawMoveSlotLifecycle(') -and
            $commandSource.Contains('mount.Commands.Queue.Count == 0') -and
            $commandSource.Contains('commands.RemoveFinishedAndUpdateQueue();') -and
            -not $commandSource.Contains('mount.Commands.InterruptMove()') -and
            $commandSource.Contains('WrapperCommandRetainedThroughoutApproach = wrapperCommandRetainedThroughoutApproach') -and
            $engineSource.Contains('MountedCombatApproachSnapshot(') -and
            $engineSource.Contains('MovementToAttack = IsApproachRow') -and
            $combatValidatorSource.Contains("'requestedTargetDistance','approachRequiredAtStart','delegatedMoveStartCount'") -and
            $combatValidatorSource.Contains('one retained rider wrapper and one manually driven Mammoth approach')) 'schema-v26-v29 combat evidence does not bind actor-specific readiness, command/resource ownership, movement-to-attack continuity, retained exact weapon identity, target durability and brain lease, native IsHit, target and AI isolation, turn identity, cleanup, and restoration'
        Assert-Test ($targetSource.Contains('DiagnosticDurabilityTemporaryHitPoints = 128') -and
            $targetSource.Contains('temporaryHitPoints.AddModifier(') -and
            $targetSource.Contains('(Fact)null') -and
            $targetSource.Contains('ModifierDescriptor.UntypedStackable') -and
            $targetSource.Contains('targetDurabilityModifier.Remove()') -and
            $targetSource.Contains('temporaryHitPoints.ModifiedValue == TargetTemporaryHitPointsBefore') -and
            $engineSource.Contains('IsMammothPrimaryRow || IsApproachRow || IsMountedBeforeModeTransitionRow)')) 'Mammoth, mounted-approach, and exact TB human-transition diagnostic targets do not acquire and exactly release their bounded scenario-only temporary-hit-point durability lease'
        $durabilityAcquireIndex = $targetSource.IndexOf('AcquireTargetDurabilityLease(target, requireDurabilityLease);', [StringComparison]::Ordinal)
        $targetActivationIndex = $targetSource.IndexOf('lifecycle.Activate("pending:" + runId, safety.AllPassed && durabilityPolicyPassed)', [StringComparison]::Ordinal)
        $durabilityReleaseIndex = $targetSource.IndexOf('var durabilityLeaseClean = ReleaseTargetDurabilityLease(current);', [StringComparison]::Ordinal)
        $targetDestroyIndex = $targetSource.IndexOf('current.Destroy();', [StringComparison]::Ordinal)
        $outcomeLifeCaptureIndex = $engineSource.IndexOf('targetService.CaptureCurrentLife(target)', [StringComparison]::Ordinal)
        $terminalActionAssertionIndex = $engineSource.IndexOf('assertions.Check(outcome.Action == AttackAction', [StringComparison]::Ordinal)
        Assert-Test ($durabilityAcquireIndex -ge 0 -and $targetActivationIndex -gt $durabilityAcquireIndex -and
            $durabilityReleaseIndex -ge 0 -and $targetDestroyIndex -gt $durabilityReleaseIndex -and
            $outcomeLifeCaptureIndex -ge 0 -and $terminalActionAssertionIndex -gt $outcomeLifeCaptureIndex) 'Mammoth diagnostic durability acquisition precedes activation, exact release precedes target destruction, and outcome life is captured before terminal assertions'
        $brainAcquireIndex = $targetSource.IndexOf('AcquireTargetBrainLease(target);', [StringComparison]::Ordinal)
        $brainValidateIndex = $targetSource.IndexOf('ValidateTargetBrainLeaseActive(target)', [StringComparison]::Ordinal)
        $brainReleaseIndex = $targetSource.IndexOf('brainLeaseClean = ReleaseTargetBrainLease(current);', [StringComparison]::Ordinal)
        $targetStopIndex = $targetSource.IndexOf('current.View?.StopMoving();', [StringComparison]::Ordinal)
        Assert-Test ($brainAcquireIndex -gt $durabilityAcquireIndex -and
            $brainValidateIndex -gt $brainAcquireIndex -and $targetActivationIndex -gt $brainValidateIndex -and
            $targetSource.Contains('!ValidateTargetBrainLeaseActive(combatMemoryTarget)') -and
            $targetSource.Contains('TargetBrainSuppressedAtClick = ValidateTargetBrainLeaseActive(target);') -and
            $targetSource.Contains('TargetBrainSuppressedAtOutcome = ValidateTargetBrainLeaseActive(target);') -and
            $targetSource.Contains('current.IsBrainActive = targetBrainActiveBefore;') -and
            $targetStopIndex -ge 0 -and $brainReleaseIndex -gt $targetStopIndex -and
            $targetDestroyIndex -gt $brainReleaseIndex -and
            -not $targetSource.Contains('IsDirectlyControllable =')) 'diagnostic target brain lease is not exact, continuously validated, target-only, reversible, and released immediately before target destruction'
        foreach ($field in @('TargetEntityRemoved','RuntimeGroupRemoved','RuntimeFactionRemoved')) {
            $jsonField = [char]::ToLowerInvariant($field[0]) + $field.Substring(1)
            Assert-Test ($engineSource.Contains("$field =") -and
                $combatValidatorSource.Contains("'$jsonField'")) "combat cleanup evidence does not bind exact $field state"
        }
        Assert-Test ($engineSource.Contains('SleeplessLeaseReleased = targetSleeplessLeaseReleased') -and
            $combatValidatorSource.Contains("'sleeplessLeaseReleased'")) 'combat cleanup evidence does not bind exact target sleepless-lease restoration'
        Assert-Test ($engineSource.Contains('NonPairPartyAiLeaseRestored = targetNonPairPartyAiLeaseRestored') -and
            $combatValidatorSource.Contains("'nonPairPartyAiLeaseRestored'")) 'combat cleanup evidence does not bind exact non-pair party AI restoration'
        Assert-Test ($engineSource.Contains('DurabilityLeaseReleased = targetDurabilityLeaseReleased') -and
            $combatValidatorSource.Contains("'durabilityLeaseReleased'")) 'combat cleanup evidence does not bind exact target durability-lease restoration'
        Assert-Test ($engineSource.Contains('BrainLeaseReleased = targetBrainLeaseReleased') -and
            $combatValidatorSource.Contains("'brainLeaseReleased'")) 'combat cleanup evidence does not bind exact target brain-lease restoration'
        Assert-Test ($engineSource.Contains('TargetProvisioning = targetProvisioning ?? new CombatTargetProvisioningEvidence()') -and
            $combatValidatorSource.Contains("'targetProvisioning'") -and
            $combatValidatorSource.Contains("'noWeaponProvisioningMutation'") -and
            $combatValidatorSource.Contains("'targetNativeSingleAttackSlot'")) 'combat evidence does not bind exact native weapon selection and zero provisioning mutation'
    }

    Invoke-HarnessTest 'core combat-control source is exact ordered production-path and residue-closed' {
        $controlSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeCombatControlScenarioEngine.cs'))
        $commandSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPairAttackCommand.cs'))
        $controllerSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedCombatController.cs'))
        $hostSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeAutomationHost.cs'))
        $projectSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\KingmakerMountedCombat.csproj'))
        $rowsIndex = $controlSource.IndexOf('private static readonly string[] Rows', [StringComparison]::Ordinal)
        $invalidIndex = $controlSource.IndexOf('InvalidTargetRow,', $rowsIndex, [StringComparison]::Ordinal)
        $deathIndex = $controlSource.IndexOf('TargetDeathRow,', $rowsIndex, [StringComparison]::Ordinal)
        $cleanupIndex = $controlSource.IndexOf('CleanupRow,', $rowsIndex, [StringComparison]::Ordinal)
        $nonMountedIndex = $controlSource.IndexOf('NonMountedRow', $rowsIndex, [StringComparison]::Ordinal)
        Assert-Test ($invalidIndex -ge 0 -and $deathIndex -gt $invalidIndex -and
            $cleanupIndex -gt $deathIndex -and $nonMountedIndex -gt $cleanupIndex) 'core combat-control row order is not exact'
        Assert-Test ($controlSource.Contains('new ClickUnitHandler().OnClick(null, Vector3.zero, 0, false, false)') -and
            $controlSource.Contains('combat.TryHandleUnitClick(') -and
            $controlSource.Contains('MountedCombatClickResult.NotHandled')) 'invalid and non-mounted controls bypass the production click/controller seams'
        Assert-Test ($controlSource.Contains('target.Damage = observations.TargetDamageRequested;') -and
            $controlSource.Contains('relationship.Dismount(CleanupTrigger.Exception)') -and
            $controlSource.Contains('combat.Cancel("control cleanup repeat")')) 'target-death or repeated exception cleanup does not use the bounded production seam'
        $targetInvalidationIndex = $commandSource.IndexOf('TryInterruptForTargetInvalidationBeforeChildAttack()', [StringComparison]::Ordinal)
        $livePairIndex = $commandSource.IndexOf('RequireLiveExactPair();', $targetInvalidationIndex, [StringComparison]::Ordinal)
        Assert-Test ($targetInvalidationIndex -ge 0 -and $livePairIndex -gt $targetInvalidationIndex -and
            $commandSource.Contains('transaction.CancelTargetInvalidationBeforeChildAttack(attackTarget.UniqueId)') -and
            $commandSource.Contains('transaction.ChildAttackStartCount != 0')) 'pre-child target invalidation does not cancel before generic exact-pair fault handling'
        $acceptedTargetIndex = $controlSource.IndexOf('combat.HasActivePreChildCommandForTarget(target)', [StringComparison]::Ordinal)
        $cleanupMutationIndex = $controlSource.IndexOf('relationship.Dismount(CleanupTrigger.Exception)', [StringComparison]::Ordinal)
        $targetMutationIndex = $controlSource.IndexOf('target.Damage = observations.TargetDamageRequested;', [StringComparison]::Ordinal)
        Assert-Test ($commandSource.Contains('HasAcceptedTargetBeforeChildAttack(UnitEntityData exactTarget)') -and
            $controllerSource.Contains('activeCommand.HasAcceptedTargetBeforeChildAttack(exactTarget)') -and
            $controlSource.Contains('case ControlStep.AwaitPreChildCommand:') -and
            $acceptedTargetIndex -ge 0 -and $cleanupMutationIndex -gt $acceptedTargetIndex -and
            $targetMutationIndex -gt $acceptedTargetIndex) 'target-death and cleanup controls can mutate before the exact active command admits its target'
        Assert-Test ($controlSource.Contains('outcomeAtExerciseStart = combat.LastOutcome;') -and
            $controlSource.Contains('ReferenceEquals(combat.LastOutcome, outcomeAtExerciseStart)')) 'non-mounted control does not distinguish unchanged historical outcome evidence from a new command outcome'
        Assert-Test ($controllerSource.Contains('activeCommand = command;') -and
            $controllerSource.Contains('LastOutcome = null;') -and
            $controlSource.Contains('assertions.Check(combat.LastOutcome == null,') -and
            -not $controlSource.Contains('assertions.Check(ReferenceEquals(combat.LastOutcome, outcomeAtExerciseStart),')) 'active command admission does not require the controller-cleared terminal outcome after a prior completed row'
        Assert-Test ($controlSource.Contains('observations.AttackRuleCount == 0') -and
            $controlSource.Contains('observations.AttackRollCount == 0') -and
            $controlSource.Contains('observations.DamageRuleCount == 0') -and
            $controlSource.Contains('observations.UnexpectedPairAttackCount == 0') -and
            $controlSource.Contains('observations.ForcedD20Count == 0')) 'core controls do not fail closed on any unexpected attack chain'
        $controlHostIndex = $hostSource.IndexOf('RuntimeCombatControlScenarioEngine.SupportsScenario(request.Scenario)', [StringComparison]::Ordinal)
        $attackHostIndex = $hostSource.IndexOf('RuntimeCombatScenarioEngine.SupportsScenario(request.Scenario)', [StringComparison]::Ordinal)
        Assert-Test ($controlHostIndex -ge 0 -and $attackHostIndex -gt $controlHostIndex -and
            $hostSource.Contains('combatControlEngine.Dispose()')) 'runtime host does not isolate or dispose the control engine before attack schemas'
        $publisherStart = $hostSource.IndexOf('private static string PublishRuntimeArtifactManifest(RuntimeRequest request)', [StringComparison]::Ordinal)
        $publisherEnd = $hostSource.IndexOf('private static void AddRuntimeArtifactIfPresent', $publisherStart, [StringComparison]::Ordinal)
        $publisherSource = if ($publisherStart -ge 0 -and $publisherEnd -gt $publisherStart) {
            $hostSource.Substring($publisherStart, $publisherEnd - $publisherStart)
        } else { '' }
        Assert-Test ($publisherSource.Contains('WriteJsonReplacingAtomic(manifestPath, manifest);') -and
            $hostSource.Contains('File.Replace(temporary, path, null, true);') -and
            $hostSource.Contains('Runtime artifact manifest is a reparse point.') -and
            $publisherSource -notmatch 'if\s*\(File\.Exists\(manifestPath\)\)\s*\{\s*return ComputeSha256\(manifestPath\)') 'game finalization can silently reuse a stale orchestration-created artifact manifest'
        Assert-Test $projectSource.Contains('Diagnostics\RuntimeCombatControlScenarioEngine.cs') 'combat-control engine is absent from the exact production project'
    }

    $combatRequest = Copy-TestJsonValue $v2Request
    $combatRequest.runId = 'combat-evidence-test'
    $combatRequest.scenario = 'mounted-rider-melee-hit-rt'
    $combatRequest.evidenceRoot = Join-Path $runtimeEvidenceTestRoot $combatRequest.runId
    Write-KmcJsonAtomic $combatRequestPath $combatRequest
    $combatRecord = New-TestCombatEvidenceRecord $combatRequest
    $combatManifestHash = Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $combatRecord
    $combatManifest = Read-KmcJson (Join-Path $combatRequest.evidenceRoot 'runtime-artifacts.json')
    $combatSubresult = [ordered]@{name=$combatRequest.scenario;status='PASS';assertionPassCount=25;assertionFailCount=0;errors=@()}

    Invoke-HarnessTest 'runtime request and combat evidence accept exact stationary rider hit' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $combatRequestPath
        Assert-KmcCombatScenarioEvidence -Request $combatRequest -Manifest $combatManifest -Status 'PASS' -SubscenarioResults @($combatSubresult)
    }

    Invoke-HarnessTest 'reach capture is isolated to exact stationary rider and Mammoth qualification rows' {
        $engineSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeCombatScenarioEngine.cs'))
        Assert-Test (($engineSource | Select-String -Pattern 'if \(IsReachQualificationRow\)\s*\{\s*CaptureInitialReachEvidence\(\);\s*\}' -AllMatches).Matches.Count -eq 1 -and
            ($engineSource | Select-String -Pattern 'if \(IsReachQualificationRow\)\s*\{\s*CaptureDispatchReachEvidence\(\);\s*\}' -AllMatches).Matches.Count -eq 1 -and
            $engineSource.Contains('string.Equals(currentRow, RiderHitRealTime, StringComparison.Ordinal)') -and
            $engineSource.Contains('string.Equals(currentRow, RiderHitTurnBased, StringComparison.Ordinal)') -and
            $engineSource.Contains('string.Equals(currentRow, MammothPrimaryHitRealTime, StringComparison.Ordinal)') -and
            $engineSource.Contains('string.Equals(currentRow, MammothPrimaryHitTurnBased, StringComparison.Ordinal)')) `
            'reach capture can contaminate a historical movement, miss, termination, or lifecycle evidence schema'
    }

    $controlRequestPath = Join-Path $testRoot 'runtime-request-combat-control.json'
    $controlRequest = Copy-TestJsonValue $combatRequest
    $controlRequest.runId = 'combat-core-control-evidence-test'
    $controlRequest.scenario = 'combat-core-control-suite'
    $controlRequest.evidenceRoot = Join-Path $runtimeEvidenceTestRoot $controlRequest.runId
    Write-KmcJsonAtomic $controlRequestPath $controlRequest
    $controlRecords = @(New-TestCombatControlEvidenceRecords $controlRequest)
    [void](Write-TestCombatControlEvidence -EvidenceRoot $controlRequest.evidenceRoot -Request $controlRequest -Records $controlRecords)
    $controlManifest = Read-KmcJson (Join-Path $controlRequest.evidenceRoot 'runtime-artifacts.json')
    $controlSubresults = @($controlRecords | ForEach-Object {
        [ordered]@{name=[string]$_.row;status='PASS';assertionPassCount=12;assertionFailCount=0;errors=@()}
    })

    Invoke-HarnessTest 'runtime request and exact four-row core combat controls pass' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $controlRequestPath
        Assert-KmcCombatScenarioEvidence -Request $controlRequest -Manifest $controlManifest -Status 'PASS' -SubscenarioResults $controlSubresults
    }

    Invoke-HarnessTest 'core combat controls reject missing extra reordered or cross-identity rows' {
        $cases = @(
            { param($rows) return @($rows | Select-Object -First 3) },
            { param($rows) return @($rows + (Copy-TestJsonValue $rows[3])) },
            { param($rows) $swap=$rows[0];$rows[0]=$rows[1];$rows[1]=$swap;return $rows },
            { param($rows) $rows[2].riderId='different-rider';return $rows },
            { param($rows) $rows[3].targetId=[string]$rows[2].targetId;return $rows }
        )
        foreach ($mutate in $cases) {
            $candidate = @(Copy-TestJsonValue $controlRecords)
            $candidate = @(& $mutate $candidate)
            [void](Write-TestCombatControlEvidence -EvidenceRoot $controlRequest.evidenceRoot -Request $controlRequest -Records $candidate)
            $candidateManifest = Read-KmcJson (Join-Path $controlRequest.evidenceRoot 'runtime-artifacts.json')
            $rejected = $false
            try { Assert-KmcCombatScenarioEvidence -Request $controlRequest -Manifest $candidateManifest -Status 'PASS' -SubscenarioResults $controlSubresults }
            catch { $rejected = $true }
            Assert-Test $rejected 'combat-control validator accepted a missing, extra, reordered, or identity-mismatched row'
        }
    }

    Invoke-HarnessTest 'core combat controls reject behavior resource rule cleanup and production contradictions' {
        $cases = @(
            { param($rows) $rows[0].observations.riderInvalidRejected=$false;return $rows },
            { param($rows) $rows[1].observations.attackRuleCount=1;return $rows },
            { param($rows) $rows[2].resources.riderStandardAfter=1.0;return $rows },
            { param($rows) $rows[2].observations.cleanupTrigger='Manual';return $rows },
            { param($rows) $rows[3].productionPath='stock';return $rows },
            { param($rows) $rows[3].cleanup.residualState=$true;return $rows },
            { param($rows) $rows[3].mountedAtExercise=$true;return $rows }
        )
        foreach ($mutate in $cases) {
            $candidate = @(Copy-TestJsonValue $controlRecords)
            $candidate = @(& $mutate $candidate)
            [void](Write-TestCombatControlEvidence -EvidenceRoot $controlRequest.evidenceRoot -Request $controlRequest -Records $candidate)
            $candidateManifest = Read-KmcJson (Join-Path $controlRequest.evidenceRoot 'runtime-artifacts.json')
            $rejected = $false
            try { Assert-KmcCombatScenarioEvidence -Request $controlRequest -Manifest $candidateManifest -Status 'PASS' -SubscenarioResults $controlSubresults }
            catch { $rejected = $true }
            Assert-Test $rejected 'combat-control validator accepted a behavior, resource, rule, cleanup, or production contradiction'
        }
    }

    [void](Write-TestCombatControlEvidence -EvidenceRoot $controlRequest.evidenceRoot -Request $controlRequest -Records $controlRecords)

    $turnBasedRequestPath = Join-Path $testRoot 'runtime-request-combat-turn-based.json'
    $turnBasedRequest = Copy-TestJsonValue $combatRequest
    $turnBasedRequest.runId = 'combat-evidence-turn-based-test'
    $turnBasedRequest.scenario = 'mounted-rider-melee-hit-tb'
    $turnBasedRequest.evidenceRoot = Join-Path $runtimeEvidenceTestRoot $turnBasedRequest.runId
    Write-KmcJsonAtomic $turnBasedRequestPath $turnBasedRequest
    $turnBasedRecord = New-TestCombatEvidenceRecord $turnBasedRequest
    [void](Write-TestCombatEvidence -EvidenceRoot $turnBasedRequest.evidenceRoot -Request $turnBasedRequest -Record $turnBasedRecord)
    $turnBasedManifest = Read-KmcJson (Join-Path $turnBasedRequest.evidenceRoot 'runtime-artifacts.json')
    $turnBasedSubresult = [ordered]@{name=$turnBasedRequest.scenario;status='PASS';assertionPassCount=25;assertionFailCount=0;errors=@()}

    Invoke-HarnessTest 'runtime request and schema-v27 evidence accept exact native stationary rider turn' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $turnBasedRequestPath
        Assert-KmcCombatScenarioEvidence -Request $turnBasedRequest -Manifest $turnBasedManifest -Status 'PASS' -SubscenarioResults @($turnBasedSubresult)
    }

    $humanPlayRequestPath = Join-Path $testRoot 'runtime-request-combat-human-play.json'
    $humanPlayRequest = Copy-TestJsonValue $combatRequest
    $humanPlayRequest.runId = 'combat-evidence-human-play-test'
    $humanPlayRequest.scenario = 'mounted-rider-melee-human-play-path-rt'
    $humanPlayRequest.evidenceRoot = Join-Path $runtimeEvidenceTestRoot $humanPlayRequest.runId
    Write-KmcJsonAtomic $humanPlayRequestPath $humanPlayRequest
    $humanPlayRecord = New-TestCombatEvidenceRecord $humanPlayRequest
    [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayRequest.evidenceRoot -Request $humanPlayRequest -Record $humanPlayRecord)
    $humanPlayManifest = Read-KmcJson (Join-Path $humanPlayRequest.evidenceRoot 'runtime-artifacts.json')
    $humanPlaySubresult = [ordered]@{name=$humanPlayRequest.scenario;status='PASS';assertionPassCount=25;assertionFailCount=0;errors=@()}

    Invoke-HarnessTest 'runtime request and schema-v48 evidence accept the ordinary RT player-click rider melee path' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $humanPlayRequestPath
        Assert-KmcCombatScenarioEvidence -Request $humanPlayRequest -Manifest $humanPlayManifest -Status 'PASS' -SubscenarioResults @($humanPlaySubresult)
    }

    Invoke-HarnessTest 'historical schema-v44 human-play evidence semantics remain valid' {
        $historical = Copy-TestJsonValue $humanPlayRecord
        $historical.schemaVersion = 44
        [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayRequest.evidenceRoot -Request $humanPlayRequest -Record $historical)
        $historicalManifest = Read-KmcJson (Join-Path $humanPlayRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $humanPlayRequest -Manifest $historicalManifest -Status 'PASS' -SubscenarioResults @($humanPlaySubresult)
        [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayRequest.evidenceRoot -Request $humanPlayRequest -Record $humanPlayRecord)
    }

    $humanPlayTurnRequestPath = Join-Path $testRoot 'runtime-request-combat-human-play-turn-based.json'
    $humanPlayTurnRequest = Copy-TestJsonValue $humanPlayRequest
    $humanPlayTurnRequest.runId = 'combat-evidence-human-play-turn-based-test'
    $humanPlayTurnRequest.scenario = 'mounted-rider-melee-human-play-path-tb'
    $humanPlayTurnRequest.evidenceRoot = Join-Path $runtimeEvidenceTestRoot $humanPlayTurnRequest.runId
    Write-KmcJsonAtomic $humanPlayTurnRequestPath $humanPlayTurnRequest
    $humanPlayTurnRecord = New-TestCombatEvidenceRecord $humanPlayTurnRequest
    [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayTurnRequest.evidenceRoot -Request $humanPlayTurnRequest -Record $humanPlayTurnRecord)
    $humanPlayTurnManifest = Read-KmcJson (Join-Path $humanPlayTurnRequest.evidenceRoot 'runtime-artifacts.json')
    $humanPlayTurnSubresult = [ordered]@{name=$humanPlayTurnRequest.scenario;status='PASS';assertionPassCount=25;assertionFailCount=0;errors=@()}

    Invoke-HarnessTest 'runtime request and schema-v52 evidence accept native Mammoth terminal-source observations with an explicit physical-pointer manual gate' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $humanPlayTurnRequestPath
        Assert-KmcCombatScenarioEvidence -Request $humanPlayTurnRequest -Manifest $humanPlayTurnManifest -Status 'PASS' -SubscenarioResults @($humanPlayTurnSubresult)
    }

    Invoke-HarnessTest 'historical schema-v51 native Mammoth terminal-source semantics remain valid' {
        $historical = Copy-TestJsonValue $humanPlayTurnRecord
        $historical.schemaVersion = 51
        $historical.turnBased.PSObject.Properties.Remove('nativeMammothPhysicalPointerQualification')
        [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayTurnRequest.evidenceRoot -Request $humanPlayTurnRequest -Record $historical)
        $historicalManifest = Read-KmcJson (Join-Path $humanPlayTurnRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $humanPlayTurnRequest -Manifest $historicalManifest -Status 'PASS' -SubscenarioResults @($humanPlayTurnSubresult)
        [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayTurnRequest.evidenceRoot -Request $humanPlayTurnRequest -Record $humanPlayTurnRecord)
    }

    Invoke-HarnessTest 'historical schema-v50 native Mammoth terminal evidence semantics remain valid' {
        $historical = Copy-TestJsonValue $humanPlayTurnRecord
        $historical.schemaVersion = 50
        $historical.turnBased.PSObject.Properties.Remove('nativeMammothPhysicalPointerQualification')
        foreach ($name in @('nativeMammothGroundInterruptSource','nativeMammothGroundEnoughCloseAtTerminal',
            'nativeMammothGroundAgentReallyMovingAtTerminal','nativeMammothGroundAgentWantsToMoveAtTerminal')) {
            $historical.turnBased.PSObject.Properties.Remove($name)
        }
        [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayTurnRequest.evidenceRoot -Request $humanPlayTurnRequest -Record $historical)
        $historicalManifest = Read-KmcJson (Join-Path $humanPlayTurnRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $humanPlayTurnRequest -Manifest $historicalManifest -Status 'PASS' -SubscenarioResults @($humanPlayTurnSubresult)
        [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayTurnRequest.evidenceRoot -Request $humanPlayTurnRequest -Record $humanPlayTurnRecord)
    }

    Invoke-HarnessTest 'historical schema-v49 native Mammoth control evidence semantics remain valid' {
        $historical = Copy-TestJsonValue $humanPlayTurnRecord
        $historical.schemaVersion = 49
        $historical.turnBased.PSObject.Properties.Remove('nativeMammothPhysicalPointerQualification')
        foreach ($name in @('presentationAfterNativeMammothGroundInput','nativeMammothGroundUiObservedAfterInput',
            'nativeMammothGroundCommandFinished','nativeMammothGroundCommandResult',
            'nativeMammothGroundRawMoveSlotState','mammothNativeGroundRemainingDistance',
            'nativeMammothGroundInterruptSource','nativeMammothGroundEnoughCloseAtTerminal',
            'nativeMammothGroundAgentReallyMovingAtTerminal','nativeMammothGroundAgentWantsToMoveAtTerminal')) {
            $historical.turnBased.PSObject.Properties.Remove($name)
        }
        [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayTurnRequest.evidenceRoot -Request $humanPlayTurnRequest -Record $historical)
        $historicalManifest = Read-KmcJson (Join-Path $humanPlayTurnRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $humanPlayTurnRequest -Manifest $historicalManifest -Status 'PASS' -SubscenarioResults @($humanPlayTurnSubresult)
        [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayTurnRequest.evidenceRoot -Request $humanPlayTurnRequest -Record $humanPlayTurnRecord)
    }

    Invoke-HarnessTest 'historical schema-v47 human-play evidence semantics remain valid' {
        $historical = Copy-TestJsonValue $humanPlayTurnRecord
        $historical.schemaVersion = 47
        $historical.turnBased.PSObject.Properties.Remove('nativeMammothPhysicalPointerQualification')
        foreach ($name in @('presentationDuringMammothTurn','presentationAfterNativeMammothGroundInput',
            'nativeMammothTurnStarted','nativeMammothTurnUiObserved',
            'nativeMammothGroundInputStarted','nativeMammothGroundInputCompleted','nativeMammothGroundSelectionRetained',
            'nativeMammothGroundUiObservedAfterInput','nativeMammothGroundCommandFinished',
            'nativeMammothGroundCommandResult','nativeMammothGroundRawMoveSlotState',
            'nativeMammothGroundInterruptSource','nativeMammothGroundEnoughCloseAtTerminal',
            'nativeMammothGroundAgentReallyMovingAtTerminal','nativeMammothGroundAgentWantsToMoveAtTerminal',
            'mammothNativeGroundDisplacement','mammothNativeGroundRemainingDistance','mammothNativeMoveBefore','mammothNativeMoveAfter',
            'riderMoveBeforeMammothNativeGroundInput','riderMoveAfterMammothNativeGroundInput')) {
            $historical.turnBased.PSObject.Properties.Remove($name)
        }
        [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayTurnRequest.evidenceRoot -Request $humanPlayTurnRequest -Record $historical)
        $historicalManifest = Read-KmcJson (Join-Path $humanPlayTurnRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $humanPlayTurnRequest -Manifest $historicalManifest -Status 'PASS' -SubscenarioResults @($humanPlayTurnSubresult)
        [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayTurnRequest.evidenceRoot -Request $humanPlayTurnRequest -Record $humanPlayTurnRecord)
    }

    Invoke-HarnessTest 'schema-v52 evidence rejects native-control transition lease-isolation completion-observation movement or manual-boundary contradictions' {
        $cases = @(
            { param($record) $record.turnBased.pairRetainedAfterEnable=$false;return $record },
            { param($record) $record.turnBased.presentationAfterEnable=$record.turnBased.presentationAfterEnable.Replace('actionBarOwner=combat-rider','actionBarOwner=combat-mount');return $record },
            { param($record) $record.turnBased.presentationAfterRealtimeRestore=$record.turnBased.presentationAfterRealtimeRestore.Replace('riderViewActiveInHierarchy=True','riderViewActiveInHierarchy=False');return $record },
            { param($record) $record.turnBased.enabledAtMount=$true;return $record },
            { param($record) $record.turnBased.mountAiLeaseReassertionArmedCount=0;return $record },
            { param($record) $record.turnBased.mountAiLeaseReassertionAttemptCount=2;return $record },
            { param($record) $record.turnBased.mountAiLeaseReassertionMutationCount=0;return $record },
            { param($record) $record.turnBased.mountAiLeaseReassertionSuccessCount=0;return $record },
            { param($record) $record.turnBased.mountAiLeaseReassertionResult='reassertion-failed';return $record },
            { param($record) $record.turnBased.riderUiLeaseRestoreArmedCount=0;return $record },
            { param($record) $record.turnBased.riderUiLeaseRestoreAttemptCount=2;return $record },
            { param($record) $record.turnBased.riderUiLeaseRestoreMutationCount=0;return $record },
            { param($record) $record.turnBased.riderUiLeaseRestoreSuccessCount=0;return $record },
            { param($record) $record.turnBased.riderUiLeaseRestoreResult='selection-restore-failed';return $record },
            { param($record) $record.groundMovement.executorId='wrong-mount';return $record },
            { param($record) $record.groundMovement.mountMoveAfter=1.0;return $record },
            { param($record) $record.groundMovement.usedRiderTurnAdapter=$false;return $record },
            { param($record) $record.groundMovement.slotRestored=$false;return $record },
            { param($record) $record.admission.overlayActivationWorldClickSuppressed=$false;return $record },
            { param($record) $record.admission.armedActionRetainedAfterOverlayClick=$false;return $record },
            { param($record) $record.admission.directClickedUnitView=$false;return $record },
            { param($record) $record.admission.rejectionCodes=@('OutsideSupportedRange');return $record },
            { param($record) $record.turnBased.nativeMammothTurnUiObserved=$false;return $record },
            { param($record) $record.turnBased.presentationDuringMammothTurn=$record.turnBased.presentationDuringMammothTurn.Replace('actionBarCanUseAbilities=True','actionBarCanUseAbilities=False');return $record },
            { param($record) $record.turnBased.presentationDuringMammothTurn=$record.turnBased.presentationDuringMammothTurn.Replace('selectedUnit=combat-mount','selectedUnit=combat-rider');return $record },
            { param($record) $record.turnBased.nativeMammothGroundInputCompleted=$false;return $record },
            { param($record) $record.turnBased.nativeMammothGroundUiObservedAfterInput=$false;return $record },
            { param($record) $record.turnBased.nativeMammothGroundCommandFinished=$false;return $record },
            { param($record) $record.turnBased.nativeMammothGroundCommandResult='Interrupted';return $record },
            { param($record) $record.turnBased.nativeMammothGroundRawMoveSlotState='replacement:wrong';return $record },
            { param($record) $record.turnBased.nativeMammothGroundInterruptSource='Kingmaker.View.UnitEntityView.OnMovementInterrupted';return $record },
            { param($record) $record.turnBased.nativeMammothPhysicalPointerQualification='automated';return $record },
            { param($record) $record.turnBased.nativeMammothGroundEnoughCloseAtTerminal=$false;return $record },
            { param($record) $record.turnBased.nativeMammothGroundAgentReallyMovingAtTerminal=$true;return $record },
            { param($record) $record.turnBased.nativeMammothGroundAgentWantsToMoveAtTerminal=$true;return $record },
            { param($record) $record.turnBased.presentationAfterNativeMammothGroundInput=$record.turnBased.presentationAfterNativeMammothGroundInput.Replace('actionBarCanUseAbilities=True','actionBarCanUseAbilities=False');return $record },
            { param($record) $record.turnBased.mammothNativeGroundDisplacement=0.0;return $record },
            { param($record) $record.turnBased.mammothNativeMoveAfter=0.0;return $record },
            { param($record) $record.turnBased.riderMoveAfterMammothNativeGroundInput=1.0;return $record }
        )
        foreach ($mutate in $cases) {
            $candidate = Copy-TestJsonValue $humanPlayTurnRecord
            $candidate = & $mutate $candidate
            [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayTurnRequest.evidenceRoot -Request $humanPlayTurnRequest -Record $candidate)
            $candidateManifest = Read-KmcJson (Join-Path $humanPlayTurnRequest.evidenceRoot 'runtime-artifacts.json')
            $threw = $false
            try { Assert-KmcCombatScenarioEvidence -Request $humanPlayTurnRequest -Manifest $candidateManifest -Status 'PASS' -SubscenarioResults @($humanPlayTurnSubresult) }
            catch { $threw = $true }
            Assert-Test $threw 'schema-v52 validator accepted a native-control transition lease-isolation completion-observation movement ownership or manual-boundary contradiction'
        }
        [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayTurnRequest.evidenceRoot -Request $humanPlayTurnRequest -Record $humanPlayTurnRecord)
        $humanPlayTurnManifest = Read-KmcJson (Join-Path $humanPlayTurnRequest.evidenceRoot 'runtime-artifacts.json')
    }

    Invoke-HarnessTest 'schema-v52 preserves an exact pointer-over-UI pre-action FAIL with observation sentinels and no fabricated action-actor identity' {
        $failureRecord = Copy-TestJsonValue $humanPlayTurnRecord
        $failureRecord.status = 'FAIL'
        $failureRecord.assertionPassCount = 40
        $failureRecord.assertionFailCount = 1
        $failureRecord.errors = @('native Mammoth command ended before rider action admission')
        $failureRecord.turnBased.presentationAfterEnable = '<not-observed>'
        $failureRecord.turnBased.presentationAfterNativeMammothGroundInput = '<not-observed>'
        $failureRecord.turnBased.presentationDuringMammothTurn = $failureRecord.turnBased.presentationDuringMammothTurn.Replace('pointerInGui=False','pointerInGui=True')
        $failureRecord.turnBased.nativeMammothTurnUiObserved = $false
        $failureRecord.turnBased.nativeMammothGroundInputStarted = $false
        $failureRecord.turnBased.nativeMammothGroundInputCompleted = $false
        $failureRecord.combatEntry.actionActorId = $null
        $failureRecord.combatEntry.actionActorPrepared = $false
        $failureRecord.combatEntry.actionActorCanActInCombat = $false
        $failureRecord.combatEntry.actionActorInitiative = [single]::MaxValue
        [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayTurnRequest.evidenceRoot -Request $humanPlayTurnRequest -Record $failureRecord)
        $failureManifest = Read-KmcJson (Join-Path $humanPlayTurnRequest.evidenceRoot 'runtime-artifacts.json')
        $failureSubresult = [ordered]@{
            name=$humanPlayTurnRequest.scenario;status='FAIL';assertionPassCount=40;assertionFailCount=1
            errors=@('native Mammoth command ended before rider action admission')
        }
        Assert-KmcCombatScenarioEvidence -Request $humanPlayTurnRequest -Manifest $failureManifest -Status 'FAIL' -SubscenarioResults @($failureSubresult)
        [void](Write-TestCombatEvidence -EvidenceRoot $humanPlayTurnRequest.evidenceRoot -Request $humanPlayTurnRequest -Record $humanPlayTurnRecord)
        $humanPlayTurnManifest = Read-KmcJson (Join-Path $humanPlayTurnRequest.evidenceRoot 'runtime-artifacts.json')
    }

    $missRequestPath = Join-Path $testRoot 'runtime-request-combat-miss.json'
    $missRequest = Copy-TestJsonValue $combatRequest
    $missRequest.runId = 'combat-evidence-miss-test'
    $missRequest.scenario = 'mounted-rider-melee-miss-rt'
    $missRequest.evidenceRoot = Join-Path $runtimeEvidenceTestRoot $missRequest.runId
    Write-KmcJsonAtomic $missRequestPath $missRequest
    $missRecord = New-TestCombatEvidenceRecord $missRequest
    [void](Write-TestCombatEvidence -EvidenceRoot $missRequest.evidenceRoot -Request $missRequest -Record $missRecord)
    $missManifest = Read-KmcJson (Join-Path $missRequest.evidenceRoot 'runtime-artifacts.json')
    $missSubresult = [ordered]@{name=$missRequest.scenario;status='PASS';assertionPassCount=25;assertionFailCount=0;errors=@()}

    Invoke-HarnessTest 'runtime request and combat evidence accept exact stationary rider miss' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $missRequestPath
        Assert-KmcCombatScenarioEvidence -Request $missRequest -Manifest $missManifest -Status 'PASS' -SubscenarioResults @($missSubresult)
    }

    $mammothRequestPath = Join-Path $testRoot 'runtime-request-combat-mammoth.json'
    $mammothRequest = Copy-TestJsonValue $combatRequest
    $mammothRequest.runId = 'combat-evidence-mammoth-test'
    $mammothRequest.scenario = 'mounted-mammoth-primary-hit-rt'
    $mammothRequest.evidenceRoot = Join-Path $runtimeEvidenceTestRoot $mammothRequest.runId
    Write-KmcJsonAtomic $mammothRequestPath $mammothRequest
    $mammothRecord = New-TestCombatEvidenceRecord $mammothRequest
    [void](Write-TestCombatEvidence -EvidenceRoot $mammothRequest.evidenceRoot -Request $mammothRequest -Record $mammothRecord)
    $mammothManifest = Read-KmcJson (Join-Path $mammothRequest.evidenceRoot 'runtime-artifacts.json')
    $mammothSubresult = [ordered]@{name=$mammothRequest.scenario;status='PASS';assertionPassCount=25;assertionFailCount=0;errors=@()}

    Invoke-HarnessTest 'runtime request and schema-v26 evidence accept exact stationary Mammoth primary with independent rider initiative' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $mammothRequestPath
        Assert-KmcCombatScenarioEvidence -Request $mammothRequest -Manifest $mammothManifest -Status 'PASS' -SubscenarioResults @($mammothSubresult)
    }

    Invoke-HarnessTest 'mounted reach validator rejects a Mammoth action outside its independent boundary' {
        $candidate = Copy-TestJsonValue $mammothRecord
        $candidate.reach.mountWithinAtDispatch = $false
        [void](Write-TestCombatEvidence -EvidenceRoot $mammothRequest.evidenceRoot -Request $mammothRequest -Record $candidate)
        $candidateManifest = Read-KmcJson (Join-Path $mammothRequest.evidenceRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcCombatScenarioEvidence -Request $mammothRequest -Manifest $candidateManifest -Status 'PASS' -SubscenarioResults @($mammothSubresult) }
        catch { $threw = $true }
        Assert-Test $threw 'mounted reach validator accepted a Mammoth action outside its exact boundary'
        [void](Write-TestCombatEvidence -EvidenceRoot $mammothRequest.evidenceRoot -Request $mammothRequest -Record $mammothRecord)
        $mammothManifest = Read-KmcJson (Join-Path $mammothRequest.evidenceRoot 'runtime-artifacts.json')
    }

    $mammothTurnRequestPath = Join-Path $testRoot 'runtime-request-combat-mammoth-turn-based.json'
    $mammothTurnRequest = Copy-TestJsonValue $mammothRequest
    $mammothTurnRequest.runId = 'combat-evidence-mammoth-turn-based-test'
    $mammothTurnRequest.scenario = 'mounted-mammoth-primary-hit-tb'
    $mammothTurnRequest.evidenceRoot = Join-Path $runtimeEvidenceTestRoot $mammothTurnRequest.runId
    Write-KmcJsonAtomic $mammothTurnRequestPath $mammothTurnRequest
    $mammothTurnRecord = New-TestCombatEvidenceRecord $mammothTurnRequest
    [void](Write-TestCombatEvidence -EvidenceRoot $mammothTurnRequest.evidenceRoot -Request $mammothTurnRequest -Record $mammothTurnRecord)
    $mammothTurnManifest = Read-KmcJson (Join-Path $mammothTurnRequest.evidenceRoot 'runtime-artifacts.json')
    $mammothTurnSubresult = [ordered]@{name=$mammothTurnRequest.scenario;status='PASS';assertionPassCount=25;assertionFailCount=0;errors=@()}

    Invoke-HarnessTest 'runtime request and schema-v27 evidence accept exact native Mammoth primary turn' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $mammothTurnRequestPath
        Assert-KmcCombatScenarioEvidence -Request $mammothTurnRequest -Manifest $mammothTurnManifest -Status 'PASS' -SubscenarioResults @($mammothTurnSubresult)
    }

    $moveAttackRequestPath = Join-Path $testRoot 'runtime-request-combat-move-attack.json'
    $moveAttackRequest = Copy-TestJsonValue $combatRequest
    $moveAttackRequest.runId = 'combat-evidence-move-attack-test'
    $moveAttackRequest.scenario = 'mounted-rider-melee-move-to-attack-rt'
    $moveAttackRequest.evidenceRoot = Join-Path $runtimeEvidenceTestRoot $moveAttackRequest.runId
    Write-KmcJsonAtomic $moveAttackRequestPath $moveAttackRequest
    $moveAttackRecord = New-TestCombatEvidenceRecord $moveAttackRequest
    [void](Write-TestCombatEvidence -EvidenceRoot $moveAttackRequest.evidenceRoot -Request $moveAttackRequest -Record $moveAttackRecord)
    $moveAttackManifest = Read-KmcJson (Join-Path $moveAttackRequest.evidenceRoot 'runtime-artifacts.json')
    $moveAttackSubresult = [ordered]@{name=$moveAttackRequest.scenario;status='PASS';assertionPassCount=25;assertionFailCount=0;errors=@()}

    Invoke-HarnessTest 'runtime request and schema-v34 evidence accept durable exact raw-slot stock-driven real-time rider movement-to-attack' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $moveAttackRequestPath
        Assert-KmcCombatScenarioEvidence -Request $moveAttackRequest -Manifest $moveAttackManifest -Status 'PASS' -SubscenarioResults @($moveAttackSubresult)
    }

    $moveAttackTurnRequestPath = Join-Path $testRoot 'runtime-request-combat-move-attack-turn-based.json'
    $moveAttackTurnRequest = Copy-TestJsonValue $moveAttackRequest
    $moveAttackTurnRequest.runId = 'combat-evidence-move-attack-turn-based-test'
    $moveAttackTurnRequest.scenario = 'mounted-rider-melee-move-to-attack-tb'
    $moveAttackTurnRequest.evidenceRoot = Join-Path $runtimeEvidenceTestRoot $moveAttackTurnRequest.runId
    Write-KmcJsonAtomic $moveAttackTurnRequestPath $moveAttackTurnRequest
    $moveAttackTurnRecord = New-TestCombatEvidenceRecord $moveAttackTurnRequest
    [void](Write-TestCombatEvidence -EvidenceRoot $moveAttackTurnRequest.evidenceRoot -Request $moveAttackTurnRequest -Record $moveAttackTurnRecord)
    $moveAttackTurnManifest = Read-KmcJson (Join-Path $moveAttackTurnRequest.evidenceRoot 'runtime-artifacts.json')
    $moveAttackTurnSubresult = [ordered]@{name=$moveAttackTurnRequest.scenario;status='PASS';assertionPassCount=25;assertionFailCount=0;errors=@()}

    Invoke-HarnessTest 'runtime request and schema-v35 evidence accept durable exact raw-slot rider-turn-driven movement-to-attack' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $moveAttackTurnRequestPath
        Assert-KmcCombatScenarioEvidence -Request $moveAttackTurnRequest -Manifest $moveAttackTurnManifest -Status 'PASS' -SubscenarioResults @($moveAttackTurnSubresult)
    }

    $terminationFixtures = @{}
    foreach ($terminationScenario in @(
        'mounted-rider-melee-command-cancel-rt',
        'mounted-rider-melee-command-cancel-tb',
        'mounted-rider-melee-command-interrupt-rt',
        'mounted-rider-melee-command-interrupt-tb',
        'mounted-rider-melee-combat-end-rt',
        'mounted-rider-melee-combat-end-tb')) {
        $terminationRequestPath = Join-Path $testRoot ("runtime-request-{0}.json" -f $terminationScenario)
        $terminationRequest = Copy-TestJsonValue $combatRequest
        $terminationRequest.runId = 'combat-evidence-' + $terminationScenario + '-test'
        $terminationRequest.scenario = $terminationScenario
        $terminationRequest.evidenceRoot = Join-Path $runtimeEvidenceTestRoot $terminationRequest.runId
        Write-KmcJsonAtomic $terminationRequestPath $terminationRequest
        $terminationRecord = New-TestCombatEvidenceRecord $terminationRequest
        [void](Write-TestCombatEvidence -EvidenceRoot $terminationRequest.evidenceRoot -Request $terminationRequest -Record $terminationRecord)
        $terminationManifest = Read-KmcJson (Join-Path $terminationRequest.evidenceRoot 'runtime-artifacts.json')
        $terminationSubresult = [ordered]@{name=$terminationScenario;status='PASS';assertionPassCount=25;assertionFailCount=0;errors=@()}
        $terminationFixtures[$terminationScenario] = [pscustomobject]@{
            RequestPath=$terminationRequestPath
            Request=$terminationRequest
            Record=$terminationRecord
            Manifest=$terminationManifest
            Subresult=$terminationSubresult
        }

        Invoke-HarnessTest ("runtime request and exact command-termination evidence accept {0}" -f $terminationScenario) {
            & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeRequest.ps1') -RequestPath $terminationRequestPath
            Assert-KmcCombatScenarioEvidence -Request $terminationRequest -Manifest $terminationManifest -Status 'PASS' -SubscenarioResults @($terminationSubresult)
        }
    }

    Invoke-HarnessTest 'command termination source binds exact cancellation interruption combat-end and post-state gates' {
        $engineSource = Get-Content -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeCombatScenarioEngine.cs') -Raw
        $controllerSource = Get-Content -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedCombatController.cs') -Raw
        Assert-Test -Condition ($engineSource.Contains('selection.Stop();') -and
            $engineSource.Contains('riderCommands.InterruptAll();') -and
            $engineSource.Contains('lifecycle.HandlePartyCombatStateChanged(false);') -and
            $engineSource.Contains('item.Boundary == NativeLifecycleBoundary.CombatEnded') -and
            $engineSource.Contains('terminationLifecycleDeliveryCount == 2 && terminationLifecycleDeliveriesExact') -and
            $engineSource.Contains('riderDisplacement < 0.75f || mountDisplacement < 0.75f') -and
            $engineSource.Contains('currentPairDistance <= pairApproachRadius + MountedCombatSpatialPolicy.RangeTolerance') -and
            $engineSource.Contains('riderCommands.GetCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard) == null') -and
            $engineSource.Contains('mountCommands.GetCommand(Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Move) == null') -and
            $engineSource.Contains('mountAgentStoppedAfterTermination = agent != null && !agent.WantsToMove && !agent.IsReallyMoving') -and
            $engineSource.Contains('relationshipPreservedAfterTermination && selectionRetainedAfterTermination') -and
            $engineSource.Contains('outcome.ChildAttackStartCount == 0 && !outcome.NativeAttackRuleObserved') -and
            $engineSource.Contains('!outcome.ActionStandardCharged && !outcome.RiderStandardCharged') -and
            $engineSource.Contains('IsCommandTerminationRow ? (int?)null') -and
            $engineSource.Contains('ruleProbe.ForcedD20Count == 0 && ruleProbe.AttackRuleCount == 0') -and
            $controllerSource.Contains('finishedCommandPendingSweep = command;') -and
            $controllerSource.Contains('if (commands.Queue.Count != 0)') -and
            $controllerSource.Contains('commands.RemoveFinishedAndUpdateQueue();')) -Message `
            'command termination source does not bind the exact cancellation, interruption, combat-end delivery, progress, command-slot, agent, ownership, resource, zero-rule, relationship, selection, and UI gates'
    }

    Invoke-HarnessTest 'combat-end termination validator requires its exact repeated lifecycle ledger delivery' {
        $fixture = $terminationFixtures['mounted-rider-melee-combat-end-tb']
        $mutations = @(
            @{name='missing lifecycle count';apply={param($value) $value.commandTermination.PSObject.Properties.Remove('lifecycleDeliveryCount')}},
            @{name='wrong lifecycle count';apply={param($value) $value.commandTermination.lifecycleDeliveryCount=1}},
            @{name='inexact lifecycle ledger';apply={param($value) $value.commandTermination.lifecycleDeliveriesExact=$false}},
            @{name='wrong combat-end kind';apply={param($value) $value.commandTermination.kind='native-wrapper-interrupt'}},
            @{name='wrong combat-end trigger';apply={param($value) $value.commandTermination.trigger='UnitCommands.InterruptAll'}}
        )
        foreach ($mutation in $mutations) {
            $value = Copy-TestJsonValue $fixture.Record
            & $mutation.apply $value
            [void](Write-TestCombatEvidence -EvidenceRoot $fixture.Request.evidenceRoot -Request $fixture.Request -Record $value)
            $manifest = Read-KmcJson (Join-Path $fixture.Request.evidenceRoot 'runtime-artifacts.json')
            $threw = $false
            try { Assert-KmcCombatScenarioEvidence -Request $fixture.Request -Manifest $manifest -Status 'PASS' -SubscenarioResults @($fixture.Subresult) }
            catch { $threw = $true }
            Assert-Test $threw ("combat-end termination validator accepted mutation: " + [string]$mutation.name)
        }
    }

    Invoke-HarnessTest 'command termination validator rejects cancellation interruption ownership resource and cleanup mutations' {
        $baseFixture = $terminationFixtures['mounted-rider-melee-command-cancel-tb']
        $mutations = @(
            @{name='missing termination evidence';apply={param($value) $value.PSObject.Properties.Remove('commandTermination')}},
            @{name='wrong kind';apply={param($value) $value.commandTermination.kind='native-wrapper-interrupt'}},
            @{name='wrong trigger';apply={param($value) $value.commandTermination.trigger='UnitCommands.InterruptAll'}},
            @{name='not delivered';apply={param($value) $value.commandTermination.delivered=$false}},
            @{name='not idempotent';apply={param($value) $value.commandTermination.repeatedIdempotently=$false}},
            @{name='wrapper absent before';apply={param($value) $value.commandTermination.wrapperPresentBefore=$false}},
            @{name='move absent before';apply={param($value) $value.commandTermination.delegatedMovePresentBefore=$false}},
            @{name='rider queue occupied before';apply={param($value) $value.commandTermination.riderQueueEmptyBefore=$false}},
            @{name='mount queue occupied before';apply={param($value) $value.commandTermination.mountQueueEmptyBefore=$false}},
            @{name='child started before';apply={param($value) $value.commandTermination.childAttackNotStartedBefore=$false}},
            @{name='insufficient rider movement';apply={param($value) $value.commandTermination.riderDisplacementAtTrigger=0.74}},
            @{name='insufficient mount movement';apply={param($value) $value.commandTermination.mountDisplacementAtTrigger=0.74}},
            @{name='trigger inside range';apply={param($value) $value.commandTermination.pairDistanceAtTrigger=4.05}},
            @{name='target moved';apply={param($value) $value.commandTermination.targetDisplacementAtTrigger=0.051}},
            @{name='wrapper remains after';apply={param($value) $value.commandTermination.wrapperAbsentAfter=$false}},
            @{name='move remains after';apply={param($value) $value.commandTermination.delegatedMoveAbsentAfter=$false}},
            @{name='rider queue remains after';apply={param($value) $value.commandTermination.riderQueueEmptyAfter=$false}},
            @{name='mount queue remains after';apply={param($value) $value.commandTermination.mountQueueEmptyAfter=$false}},
            @{name='mount agent moving after';apply={param($value) $value.commandTermination.mountAgentStoppedAfter=$false}},
            @{name='active wrapper remains';apply={param($value) $value.commandTermination.activeCommandClearedAfter=$false}},
            @{name='relationship lost';apply={param($value) $value.commandTermination.relationshipPreservedAfter=$false}},
            @{name='selection lost';apply={param($value) $value.commandTermination.selectionRetainedAfter=$false}},
            @{name='UI incoherent';apply={param($value) $value.commandTermination.uiCoherentAfter=$false}},
            @{name='successful terminal result';apply={param($value) $value.command.result='Success'}},
            @{name='child attack started';apply={param($value) $value.command.childAttackStartCount=1}},
            @{name='native attack observed';apply={param($value) $value.command.nativeAttackRuleObserved=$true}},
            @{name='attack rule emitted';apply={param($value) $value.rules.attackRuleCount=1}},
            @{name='attack roll emitted';apply={param($value) $value.rules.attackRollCount=1}},
            @{name='damage emitted';apply={param($value) $value.rules.damageRuleCount=1}},
            @{name='deterministic roll armed';apply={param($value) $value.rules.forcedD20=20}},
            @{name='unrelated forced roll observed';apply={param($value) $value.rules.forcedD20Count=1}},
            @{name='rider Standard charged';apply={param($value) $value.resources.riderStandardAfter=1.0}},
            @{name='action Standard charged flag';apply={param($value) $value.command.actionStandardCharged=$true}},
            @{name='rider Standard charged flag';apply={param($value) $value.command.riderStandardCharged=$true}},
            @{name='Mammoth Standard charged';apply={param($value) $value.resources.mountStandardAfter=1.0}},
            @{name='turn rider Move uncharged';apply={param($value) $value.resources.riderMoveAfter=0.0}},
            @{name='terminated move reported post-arrival tick';apply={param($value) $value.movementToAttack.delegatedMoveTickCount=1}},
            @{name='delegated move reported successful';apply={param($value) $value.movementToAttack.delegatedMoveFinishedSuccessfully=$true}},
            @{name='attack-start distance populated';apply={param($value) $value.movementToAttack.pairDistanceAtAttackStart=3.9}}
        )
        foreach ($mutation in $mutations) {
            $candidate = Copy-TestJsonValue $baseFixture.Record
            & $mutation.apply $candidate
            [void](Write-TestCombatEvidence -EvidenceRoot $baseFixture.Request.evidenceRoot -Request $baseFixture.Request -Record $candidate)
            $candidateManifest = Read-KmcJson (Join-Path $baseFixture.Request.evidenceRoot 'runtime-artifacts.json')
            $threw = $false
            try { Assert-KmcCombatScenarioEvidence -Request $baseFixture.Request -Manifest $candidateManifest -Status 'PASS' -SubscenarioResults @($baseFixture.Subresult) }
            catch { $threw = $true }
            Assert-Test $threw ("command termination validator accepted mutation: " + [string]$mutation.name)
        }

        $interruptFixture = $terminationFixtures['mounted-rider-melee-command-interrupt-rt']
        $crossScenario = Copy-TestJsonValue $baseFixture.Record
        $crossScenario.runId = $interruptFixture.Request.runId
        $crossScenario.scenario = $interruptFixture.Request.scenario
        $crossScenario.row = $interruptFixture.Request.scenario
        [void](Write-TestCombatEvidence -EvidenceRoot $interruptFixture.Request.evidenceRoot -Request $interruptFixture.Request -Record $crossScenario)
        $crossManifest = Read-KmcJson (Join-Path $interruptFixture.Request.evidenceRoot 'runtime-artifacts.json')
        $crossThrew = $false
        try { Assert-KmcCombatScenarioEvidence -Request $interruptFixture.Request -Manifest $crossManifest -Status 'PASS' -SubscenarioResults @($interruptFixture.Subresult) }
        catch { $crossThrew = $true }
        Assert-Test $crossThrew 'command termination validator accepted cancellation evidence under the interruption row'
    }

    Invoke-HarnessTest 'historical schema-v36 and schema-v37 termination evidence semantics remain valid' {
        foreach ($scenario in @('mounted-rider-melee-command-cancel-rt','mounted-rider-melee-command-cancel-tb')) {
            $fixture = $terminationFixtures[$scenario]
            $legacy = Copy-TestJsonValue $fixture.Record
            $legacy.schemaVersion = if ($scenario -ceq 'mounted-rider-melee-command-cancel-tb') { 37 } else { 36 }
            $legacy.resources.riderStandardAfter = 5.5
            $legacy.command.actionStandardCharged = $true
            $legacy.command.riderStandardCharged = $true
            $legacy.rules.forcedD20 = 20
            if ($legacy.schemaVersion -eq 37) { $legacy.movementToAttack.delegatedMoveTickCount = 12 }
            [void](Write-TestCombatEvidence -EvidenceRoot $fixture.Request.evidenceRoot -Request $fixture.Request -Record $legacy)
            $manifest = Read-KmcJson (Join-Path $fixture.Request.evidenceRoot 'runtime-artifacts.json')
            Assert-KmcCombatScenarioEvidence -Request $fixture.Request -Manifest $manifest -Status 'PASS' -SubscenarioResults @($fixture.Subresult)
        }
    }

    Invoke-HarnessTest 'movement-to-attack validator rejects mover command resource range and continuity mutations' {
        $mutations = @(
            @{name='missing movement evidence';apply={param($value) $value.PSObject.Properties.Remove('movementToAttack')}},
            @{name='approach not required';apply={param($value) $value.movementToAttack.approachRequiredAtStart=$false}},
            @{name='duplicate delegated move';apply={param($value) $value.movementToAttack.delegatedMoveStartCount=2}},
            @{name='delegated move did not tick';apply={param($value) $value.movementToAttack.delegatedMoveTickCount=0}},
            @{name='wrong delegated executor';apply={param($value) $value.movementToAttack.delegatedMoveExecutorId='combat-rider'}},
            @{name='wrapper command replaced';apply={param($value) $value.movementToAttack.wrapperCommandRetainedThroughoutApproach=$false}},
            @{name='delegated move entered mount queue';apply={param($value) $value.movementToAttack.delegatedMoveNeverQueuedOnMount=$false}},
            @{name='delegated move absent from Mammoth Move slot';apply={param($value) $value.movementToAttack.delegatedMoveOwnedByMountMoveSlot=$false}},
            @{name='Mammoth Move slot replaced';apply={param($value) $value.movementToAttack.mountMoveSlotUnreplacedThroughoutApproach=$false}},
            @{name='Mammoth command queue changed';apply={param($value) $value.movementToAttack.mountQueueEmptyThroughoutApproach=$false}},
            @{name='delegated move did not finish';apply={param($value) $value.movementToAttack.delegatedMoveFinishedSuccessfully=$false}},
            @{name='Mammoth Move slot not restored';apply={param($value) $value.movementToAttack.mountMoveSlotRestoredAfterApproach=$false}},
            @{name='wrong turn drive mode';apply={param($value) $value.movementToAttack.delegatedMoveDrivenByStockController=$true}},
            @{name='no observed movement progress';apply={param($value) $value.movementToAttack.delegatedMoveProgressObservationCount=0}},
            @{name='rider stock pathfinding active';apply={param($value) $value.movementToAttack.riderStockAgentSuppressedThroughoutApproach=$false}},
            @{name='Mammoth pathfinding unavailable';apply={param($value) $value.movementToAttack.mountStockAgentAuthoritativeThroughoutApproach=$false}},
            @{name='approach pose unhealthy';apply={param($value) $value.movementToAttack.poseHealthyThroughoutApproach=$false}},
            @{name='selection changed';apply={param($value) $value.movementToAttack.selectionRetainedDuringApproach=$false}},
            @{name='UI changed';apply={param($value) $value.movementToAttack.uiCoherentDuringApproach=$false}},
            @{name='initial target in range';apply={param($value) $value.targetDistanceAtClick=4.0;$value.movementToAttack.initialPairDistance=4.0}},
            @{name='attack started outside range';apply={param($value) $value.movementToAttack.pairDistanceAtAttackStart=4.051}},
            @{name='mount did not approach';apply={param($value) $value.movementToAttack.mountDisplacementAtAttackStart=0.0}},
            @{name='target moved';apply={param($value) $value.movementToAttack.targetDisplacementAtAttackStart=0.051}},
            @{name='durability lease absent';apply={param($value) $value.targetProvisioning.temporaryHitPointsAfterProvisioning=0;$value.targetProvisioning.durabilityLeaseAmount=0;$value.targetProvisioning.durabilityLeaseAcquired=$false}},
            @{name='target killed before outcome';apply={param($value) $value.targetLife.lastObserved.lifeState='Dead';$value.targetLife.lastObserved.conscious=$false;$value.targetLife.lastObserved.dead=$true;$value.targetLife.lastObserved.finallyDead=$true;$value.targetLife.transitionCount=1;$value.targetLife.firstTransition.observed=$true;$value.targetLife.firstTransition.previousLifeState='Conscious';$value.targetLife.firstTransition.currentLifeState='Dead';$value.targetLife.firstTransition.snapshot=$value.targetLife.lastObserved}},
            @{name='Mammoth Move charged';apply={param($value) $value.resources.mountMoveAfter=2.0}},
            @{name='rider Move not charged in turn mode';apply={param($value) $value.resources.riderMoveAfter=0.0}}
        )
        foreach ($mutation in $mutations) {
            $candidate = Copy-TestJsonValue $moveAttackTurnRecord
            & $mutation.apply $candidate
            [void](Write-TestCombatEvidence -EvidenceRoot $moveAttackTurnRequest.evidenceRoot -Request $moveAttackTurnRequest -Record $candidate)
            $candidateManifest = Read-KmcJson (Join-Path $moveAttackTurnRequest.evidenceRoot 'runtime-artifacts.json')
            $threw = $false
            try { Assert-KmcCombatScenarioEvidence -Request $moveAttackTurnRequest -Manifest $candidateManifest -Status 'PASS' -SubscenarioResults @($moveAttackTurnSubresult) }
            catch { $threw = $true }
            Assert-Test $threw ("movement-to-attack validator accepted mutation: " + [string]$mutation.name)
        }
    }

    Invoke-HarnessTest 'historical schema-v32 and schema-v33 movement evidence remains valid without the later durability lease' {
        $legacyMove32 = Copy-TestJsonValue $moveAttackRecord
        $legacyMove32.schemaVersion = 32
        $legacyMove32.targetProvisioning.temporaryHitPointsAfterProvisioning = 0
        $legacyMove32.targetProvisioning.durabilityLeaseAmount = 0
        $legacyMove32.targetProvisioning.durabilityLeaseAcquired = $false
        [void](Write-TestCombatEvidence -EvidenceRoot $moveAttackRequest.evidenceRoot -Request $moveAttackRequest -Record $legacyMove32)
        $legacyMove32Manifest = Read-KmcJson (Join-Path $moveAttackRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $moveAttackRequest -Manifest $legacyMove32Manifest -Status 'PASS' -SubscenarioResults @($moveAttackSubresult)

        $legacyMove33 = Copy-TestJsonValue $moveAttackTurnRecord
        $legacyMove33.schemaVersion = 33
        $legacyMove33.targetProvisioning.temporaryHitPointsAfterProvisioning = 0
        $legacyMove33.targetProvisioning.durabilityLeaseAmount = 0
        $legacyMove33.targetProvisioning.durabilityLeaseAcquired = $false
        [void](Write-TestCombatEvidence -EvidenceRoot $moveAttackTurnRequest.evidenceRoot -Request $moveAttackTurnRequest -Record $legacyMove33)
        $legacyMove33Manifest = Read-KmcJson (Join-Path $moveAttackTurnRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $moveAttackTurnRequest -Manifest $legacyMove33Manifest -Status 'PASS' -SubscenarioResults @($moveAttackTurnSubresult)
    }

    Invoke-HarnessTest 'historical schema-v28 through schema-v31 movement evidence shapes remain valid' {
        $legacyMove30 = Copy-TestJsonValue $moveAttackRecord
        $legacyMove30.schemaVersion = 30
        $legacyMove30.targetProvisioning.temporaryHitPointsAfterProvisioning = 0
        $legacyMove30.targetProvisioning.durabilityLeaseAmount = 0
        $legacyMove30.targetProvisioning.durabilityLeaseAcquired = $false
        [void](Write-TestCombatEvidence -EvidenceRoot $moveAttackRequest.evidenceRoot -Request $moveAttackRequest -Record $legacyMove30)
        $legacyMove30Manifest = Read-KmcJson (Join-Path $moveAttackRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $moveAttackRequest -Manifest $legacyMove30Manifest -Status 'PASS' -SubscenarioResults @($moveAttackSubresult)

        $legacyMove31 = Copy-TestJsonValue $moveAttackTurnRecord
        $legacyMove31.schemaVersion = 31
        $legacyMove31.targetProvisioning.temporaryHitPointsAfterProvisioning = 0
        $legacyMove31.targetProvisioning.durabilityLeaseAmount = 0
        $legacyMove31.targetProvisioning.durabilityLeaseAcquired = $false
        [void](Write-TestCombatEvidence -EvidenceRoot $moveAttackTurnRequest.evidenceRoot -Request $moveAttackTurnRequest -Record $legacyMove31)
        $legacyMove31Manifest = Read-KmcJson (Join-Path $moveAttackTurnRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $moveAttackTurnRequest -Manifest $legacyMove31Manifest -Status 'PASS' -SubscenarioResults @($moveAttackTurnSubresult)

        $legacyMove28 = Copy-TestJsonValue $moveAttackRecord
        $legacyMove28.schemaVersion = 28
        $legacyMove28.targetProvisioning.temporaryHitPointsAfterProvisioning = 0
        $legacyMove28.targetProvisioning.durabilityLeaseAmount = 0
        $legacyMove28.targetProvisioning.durabilityLeaseAcquired = $false
        $legacyMove28.movementToAttack.delegatedMoveTickCount = 12
        foreach ($name in @(
            'delegatedMoveOwnedByMountMoveSlot','mountMoveSlotUnreplacedThroughoutApproach',
            'mountQueueEmptyThroughoutApproach','delegatedMoveFinishedSuccessfully',
            'mountMoveSlotRestoredAfterApproach','delegatedMoveDrivenByStockController',
            'delegatedMoveDrivenByRiderTurnAdapter','delegatedMoveProgressObservationCount')) {
            $legacyMove28.movementToAttack.PSObject.Properties.Remove($name)
        }
        [void](Write-TestCombatEvidence -EvidenceRoot $moveAttackRequest.evidenceRoot -Request $moveAttackRequest -Record $legacyMove28)
        $legacyMove28Manifest = Read-KmcJson (Join-Path $moveAttackRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $moveAttackRequest -Manifest $legacyMove28Manifest -Status 'PASS' -SubscenarioResults @($moveAttackSubresult)

        $legacyMove29 = Copy-TestJsonValue $moveAttackTurnRecord
        $legacyMove29.schemaVersion = 29
        $legacyMove29.targetProvisioning.temporaryHitPointsAfterProvisioning = 0
        $legacyMove29.targetProvisioning.durabilityLeaseAmount = 0
        $legacyMove29.targetProvisioning.durabilityLeaseAcquired = $false
        $legacyMove29.movementToAttack.delegatedMoveTickCount = 12
        foreach ($name in @(
            'delegatedMoveOwnedByMountMoveSlot','mountMoveSlotUnreplacedThroughoutApproach',
            'mountQueueEmptyThroughoutApproach','delegatedMoveFinishedSuccessfully',
            'mountMoveSlotRestoredAfterApproach','delegatedMoveDrivenByStockController',
            'delegatedMoveDrivenByRiderTurnAdapter','delegatedMoveProgressObservationCount')) {
            $legacyMove29.movementToAttack.PSObject.Properties.Remove($name)
        }
        [void](Write-TestCombatEvidence -EvidenceRoot $moveAttackTurnRequest.evidenceRoot -Request $moveAttackTurnRequest -Record $legacyMove29)
        $legacyMove29Manifest = Read-KmcJson (Join-Path $moveAttackTurnRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $moveAttackTurnRequest -Manifest $legacyMove29Manifest -Status 'PASS' -SubscenarioResults @($moveAttackTurnSubresult)
    }

    Invoke-HarnessTest 'historical schema-v24 and schema-v25 evidence remain valid without action-actor entry fields' {
        $legacyMammoth24 = Copy-TestJsonValue $mammothRecord
        $legacyMammoth24.schemaVersion = 24
        $legacyMammoth24.combatEntry.riderInitiative = 0.0
        Remove-TestCombatActionActorReadinessFields $legacyMammoth24
        [void](Write-TestCombatEvidence -EvidenceRoot $mammothRequest.evidenceRoot -Request $mammothRequest -Record $legacyMammoth24)
        $legacyMammothManifest24 = Read-KmcJson (Join-Path $mammothRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $mammothRequest -Manifest $legacyMammothManifest24 -Status 'PASS' -SubscenarioResults @($mammothSubresult)

        $legacyMammothTurn25 = Copy-TestJsonValue $mammothTurnRecord
        $legacyMammothTurn25.schemaVersion = 25
        $legacyMammothTurn25.combatEntry.riderInitiative = 0.0
        Remove-TestCombatActionActorReadinessFields $legacyMammothTurn25
        [void](Write-TestCombatEvidence -EvidenceRoot $mammothTurnRequest.evidenceRoot -Request $mammothTurnRequest -Record $legacyMammothTurn25)
        $legacyMammothTurnManifest25 = Read-KmcJson (Join-Path $mammothTurnRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $mammothTurnRequest -Manifest $legacyMammothTurnManifest25 -Status 'PASS' -SubscenarioResults @($mammothTurnSubresult)
    }

    Invoke-HarnessTest 'historical schema-v22 and schema-v23 evidence remain valid without target brain-lease fields' {
        $legacyMammoth22 = Copy-TestJsonValue $mammothRecord
        $legacyMammoth22.schemaVersion = 22
        $legacyMammoth22.combatEntry.riderInitiative = 0.0
        Remove-TestCombatBrainLeaseFields $legacyMammoth22
        [void](Write-TestCombatEvidence -EvidenceRoot $mammothRequest.evidenceRoot -Request $mammothRequest -Record $legacyMammoth22)
        $legacyMammothManifest22 = Read-KmcJson (Join-Path $mammothRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $mammothRequest -Manifest $legacyMammothManifest22 -Status 'PASS' -SubscenarioResults @($mammothSubresult)

        $legacyMammothTurn23 = Copy-TestJsonValue $mammothTurnRecord
        $legacyMammothTurn23.schemaVersion = 23
        $legacyMammothTurn23.combatEntry.riderInitiative = 0.0
        Remove-TestCombatBrainLeaseFields $legacyMammothTurn23
        [void](Write-TestCombatEvidence -EvidenceRoot $mammothTurnRequest.evidenceRoot -Request $mammothTurnRequest -Record $legacyMammothTurn23)
        $legacyMammothTurnManifest23 = Read-KmcJson (Join-Path $mammothTurnRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $mammothTurnRequest -Manifest $legacyMammothTurnManifest23 -Status 'PASS' -SubscenarioResults @($mammothTurnSubresult)
    }

    Invoke-HarnessTest 'historical schema-v20 and schema-v21 Mammoth evidence remain valid' {
        $legacyMammoth = Copy-TestJsonValue $mammothRecord
        $legacyMammoth.schemaVersion = 20
        $legacyMammoth.combatEntry.riderInitiative = 0.0
        Remove-TestCombatDurabilityLeaseFields $legacyMammoth
        [void](Write-TestCombatEvidence -EvidenceRoot $mammothRequest.evidenceRoot -Request $mammothRequest -Record $legacyMammoth)
        $legacyMammothManifest = Read-KmcJson (Join-Path $mammothRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $mammothRequest -Manifest $legacyMammothManifest -Status 'PASS' -SubscenarioResults @($mammothSubresult)

        $legacyMammothTurn = Copy-TestJsonValue $mammothTurnRecord
        $legacyMammothTurn.schemaVersion = 21
        $legacyMammothTurn.combatEntry.riderInitiative = 0.0
        Remove-TestCombatDurabilityLeaseFields $legacyMammothTurn
        [void](Write-TestCombatEvidence -EvidenceRoot $mammothTurnRequest.evidenceRoot -Request $mammothTurnRequest -Record $legacyMammothTurn)
        $legacyMammothTurnManifest = Read-KmcJson (Join-Path $mammothTurnRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $mammothTurnRequest -Manifest $legacyMammothTurnManifest -Status 'PASS' -SubscenarioResults @($mammothTurnSubresult)
    }

    Invoke-HarnessTest 'Mammoth primary validator rejects actor command resource weapon duplicate and turn mutations' {
        $mutations = @(
            @{name='rider actor';apply={param($value) $value.command.actorId='combat-rider'}},
            @{name='rider command owner';apply={param($value) $value.command.commandOwnerId='combat-rider'}},
            @{name='rider resource owner';apply={param($value) $value.command.resourceOwnerId='combat-rider'}},
            @{name='action cost absent';apply={param($value) $value.command.actionStandardCharged=$false}},
            @{name='rider charged flag';apply={param($value) $value.command.riderStandardCharged=$true}},
            @{name='rider Standard consumed';apply={param($value) $value.resources.riderStandardAfter=5.5}},
            @{name='Mammoth Standard unconsumed';apply={param($value) $value.resources.mountStandardAfter=0.0}},
            @{name='Mammoth Move consumed';apply={param($value) $value.resources.mountMoveAfter=3.0}},
            @{name='wrong action-actor entry identity';apply={param($value) $value.combatEntry.actionActorId='combat-rider'}},
            @{name='action actor not prepared';apply={param($value) $value.combatEntry.actionActorPrepared=$false}},
            @{name='action actor cannot act';apply={param($value) $value.combatEntry.actionActorCanActInCombat=$false}},
            @{name='Mammoth real-time initiative not ready';apply={param($value) $value.combatEntry.actionActorInitiative=1.0}},
            @{name='rider initiative outside native prepared range';apply={param($value) $value.combatEntry.riderInitiative=6.01}},
            @{name='wrong rule initiator';apply={param($value) $value.rules.lastInitiatorId='combat-rider'}},
            @{name='wrong incoming initiator';apply={param($value) $value.targetIncomingRules.firstAttack.initiatorId='combat-rider'}},
            @{name='duplicate rider attack';apply={param($value) $value.rules.unexpectedPairAttackCount=1}},
            @{name='wrong weapon';apply={param($value) $value.command.attackWeaponBlueprintId='44444444444444444444444444444444'}},
            @{name='non-natural weapon';apply={param($value) $value.command.attackWeaponIsNatural=$false}},
            @{name='ranged weapon';apply={param($value) $value.command.attackWeaponIsRanged=$true}},
            @{name='wrong slot';apply={param($value) $value.command.attackWeaponSlot='EquippedMelee'}},
            @{name='pre-existing temporary HP';apply={param($value) $value.targetProvisioning.temporaryHitPointsBefore=1}},
            @{name='wrong temporary HP after';apply={param($value) $value.targetProvisioning.temporaryHitPointsAfterProvisioning=127}},
            @{name='wrong durability amount';apply={param($value) $value.targetProvisioning.durabilityLeaseAmount=127}},
            @{name='durability acquisition absent';apply={param($value) $value.targetProvisioning.durabilityLeaseAcquired=$false}},
            @{name='durability release absent';apply={param($value) $value.cleanup.durabilityLeaseReleased=$false}},
            @{name='target brain prior inactive';apply={param($value) $value.targetBrainLease.brainActiveBefore=$false}},
            @{name='target brain lease absent';apply={param($value) $value.targetBrainLease.leaseAcquired=$false}},
            @{name='target effective AI claim false';apply={param($value) $value.targetBrainLease.effectiveAiEnabledDuring=$false}},
            @{name='target brain validation count too low';apply={param($value) $value.targetBrainLease.validationCount=4}},
            @{name='target brain violation';apply={param($value) $value.targetBrainLease.violationObserved=$true}},
            @{name='target brain unsuppressed at click';apply={param($value) $value.targetBrainLease.suppressedAtClick=$false}},
            @{name='target brain unsuppressed at outcome';apply={param($value) $value.targetBrainLease.suppressedAtOutcome=$false}},
            @{name='target brain prior state not restored';apply={param($value) $value.targetBrainLease.brainActiveAfterRelease=$false}},
            @{name='target brain lease not released';apply={param($value) $value.targetBrainLease.leaseReleased=$false}},
            @{name='target brain cleanup absent';apply={param($value) $value.cleanup.brainLeaseReleased=$false}},
            @{name='target life transition';apply={param($value) $value.targetLife.transitionCount=1}},
            @{name='rider displacement';apply={param($value) $value.movement.riderDisplacementAtOutcome=0.051}},
            @{name='Mammoth displacement';apply={param($value) $value.movement.mountDisplacementAtOutcome=0.051}},
            @{name='target displacement';apply={param($value) $value.movement.targetDisplacementAtOutcome=0.051}}
        )
        foreach ($mutation in $mutations) {
            $candidate = Copy-TestJsonValue $mammothRecord
            & $mutation.apply $candidate
            [void](Write-TestCombatEvidence -EvidenceRoot $mammothRequest.evidenceRoot -Request $mammothRequest -Record $candidate)
            $candidateManifest = Read-KmcJson (Join-Path $mammothRequest.evidenceRoot 'runtime-artifacts.json')
            $threw = $false
            try { Assert-KmcCombatScenarioEvidence -Request $mammothRequest -Manifest $candidateManifest -Status 'PASS' -SubscenarioResults @($mammothSubresult) }
            catch { $threw = $true }
            Assert-Test $threw ("Mammoth primary validator accepted mutation: " + [string]$mutation.name)
        }

        $turnMutations = @(
            @{name='wrong expected actor';apply={param($value) $value.turnBased.expectedTurnActor='rider'}},
            @{name='native actor turn absent';apply={param($value) $value.turnBased.nativeActionActorTurnStarted=$false}},
            @{name='rider dispatch turn';apply={param($value) $value.turnBased.currentTurnUnitIdAtDispatch='combat-rider'}},
            @{name='dispatch not Acting';apply={param($value) $value.turnBased.currentTurnActingAtDispatch=$false}},
            @{name='Mammoth turn not ended';apply={param($value) $value.turnBased.actionActorTurnEndedAfterCommand=$false}},
            @{name='Mammoth still Acting';apply={param($value) $value.turnBased.currentTurnActingAtOutcome=$true}}
        )
        foreach ($mutation in $turnMutations) {
            $candidate = Copy-TestJsonValue $mammothTurnRecord
            & $mutation.apply $candidate
            [void](Write-TestCombatEvidence -EvidenceRoot $mammothTurnRequest.evidenceRoot -Request $mammothTurnRequest -Record $candidate)
            $candidateManifest = Read-KmcJson (Join-Path $mammothTurnRequest.evidenceRoot 'runtime-artifacts.json')
            $threw = $false
            try { Assert-KmcCombatScenarioEvidence -Request $mammothTurnRequest -Manifest $candidateManifest -Status 'PASS' -SubscenarioResults @($mammothTurnSubresult) }
            catch { $threw = $true }
            Assert-Test $threw ("Mammoth primary turn validator accepted mutation: " + [string]$mutation.name)
        }

        $boundedTurnInitiative = Copy-TestJsonValue $mammothTurnRecord
        $boundedTurnInitiative.combatEntry.actionActorInitiative = 3.0
        $boundedTurnInitiative.combatEntry.riderInitiative = 5.0
        [void](Write-TestCombatEvidence -EvidenceRoot $mammothTurnRequest.evidenceRoot -Request $mammothTurnRequest -Record $boundedTurnInitiative)
        $boundedTurnManifest = Read-KmcJson (Join-Path $mammothTurnRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $mammothTurnRequest -Manifest $boundedTurnManifest -Status 'PASS' -SubscenarioResults @($mammothTurnSubresult)
    }

    Invoke-HarnessTest 'combat miss validator accepts only exact native AC-selected miss reasons' {
        foreach ($reason in @('Miss','DodgeAC','ArmorAC','ShieldAC')) {
            $candidate = Copy-TestJsonValue $missRecord
            $candidate.rules.lastAttackResult = $reason
            [void](Write-TestCombatEvidence -EvidenceRoot $missRequest.evidenceRoot -Request $missRequest -Record $candidate)
            $candidateManifest = Read-KmcJson (Join-Path $missRequest.evidenceRoot 'runtime-artifacts.json')
            Assert-KmcCombatScenarioEvidence -Request $missRequest -Manifest $candidateManifest -Status 'PASS' -SubscenarioResults @($missSubresult)
        }
    }

    Invoke-HarnessTest 'historical schema-v4 and schema-v5 combat evidence remain valid' {
        $legacyRealTime = Copy-TestJsonValue $combatRecord
        $legacyRealTime.schemaVersion = 4
        Remove-TestCombatWakeLeaseFields $legacyRealTime
        $legacyRealTime.rules.PSObject.Properties.Remove('lastAttackHit')
        [void](Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $legacyRealTime)
        $legacyRealTimeManifest = Read-KmcJson (Join-Path $combatRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $combatRequest -Manifest $legacyRealTimeManifest -Status 'PASS' -SubscenarioResults @($combatSubresult)

        $legacyTurnBased = Copy-TestJsonValue $turnBasedRecord
        $legacyTurnBased.schemaVersion = 5
        Remove-TestCombatWakeLeaseFields $legacyTurnBased
        $legacyTurnBased.rules.PSObject.Properties.Remove('lastAttackHit')
        [void](Write-TestCombatEvidence -EvidenceRoot $turnBasedRequest.evidenceRoot -Request $turnBasedRequest -Record $legacyTurnBased)
        $legacyTurnBasedManifest = Read-KmcJson (Join-Path $turnBasedRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $turnBasedRequest -Manifest $legacyTurnBasedManifest -Status 'PASS' -SubscenarioResults @($turnBasedSubresult)
    }

    Invoke-HarnessTest 'historical schema-v6 and schema-v7 combat evidence remain valid' {
        $legacyRealTime = Copy-TestJsonValue $combatRecord
        $legacyRealTime.schemaVersion = 6
        Remove-TestCombatWakeLeaseFields $legacyRealTime
        [void](Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $legacyRealTime)
        $legacyRealTimeManifest = Read-KmcJson (Join-Path $combatRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $combatRequest -Manifest $legacyRealTimeManifest -Status 'PASS' -SubscenarioResults @($combatSubresult)

        $legacyTurnBased = Copy-TestJsonValue $turnBasedRecord
        $legacyTurnBased.schemaVersion = 7
        Remove-TestCombatWakeLeaseFields $legacyTurnBased
        [void](Write-TestCombatEvidence -EvidenceRoot $turnBasedRequest.evidenceRoot -Request $turnBasedRequest -Record $legacyTurnBased)
        $legacyTurnBasedManifest = Read-KmcJson (Join-Path $turnBasedRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $turnBasedRequest -Manifest $legacyTurnBasedManifest -Status 'PASS' -SubscenarioResults @($turnBasedSubresult)
    }

    Invoke-HarnessTest 'historical schema-v8 and schema-v9 combat evidence remain valid' {
        $legacyRealTime = Copy-TestJsonValue $combatRecord
        $legacyRealTime.schemaVersion = 8
        Remove-TestCombatLifeFields $legacyRealTime
        Remove-TestCombatNativeJoinFields $legacyRealTime
        [void](Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $legacyRealTime)
        $legacyRealTimeManifest = Read-KmcJson (Join-Path $combatRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $combatRequest -Manifest $legacyRealTimeManifest -Status 'PASS' -SubscenarioResults @($combatSubresult)

        $legacyTurnBased = Copy-TestJsonValue $turnBasedRecord
        $legacyTurnBased.schemaVersion = 9
        Remove-TestCombatLifeFields $legacyTurnBased
        Remove-TestCombatNativeJoinFields $legacyTurnBased
        [void](Write-TestCombatEvidence -EvidenceRoot $turnBasedRequest.evidenceRoot -Request $turnBasedRequest -Record $legacyTurnBased)
        $legacyTurnBasedManifest = Read-KmcJson (Join-Path $turnBasedRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $turnBasedRequest -Manifest $legacyTurnBasedManifest -Status 'PASS' -SubscenarioResults @($turnBasedSubresult)
    }

    Invoke-HarnessTest 'historical schema-v10 and schema-v11 combat evidence remain valid' {
        $legacyRealTime = Copy-TestJsonValue $combatRecord
        $legacyRealTime.schemaVersion = 10
        Remove-TestCombatLifeFields $legacyRealTime
        [void](Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $legacyRealTime)
        $legacyRealTimeManifest = Read-KmcJson (Join-Path $combatRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $combatRequest -Manifest $legacyRealTimeManifest -Status 'PASS' -SubscenarioResults @($combatSubresult)

        $legacyTurnBased = Copy-TestJsonValue $turnBasedRecord
        $legacyTurnBased.schemaVersion = 11
        Remove-TestCombatLifeFields $legacyTurnBased
        [void](Write-TestCombatEvidence -EvidenceRoot $turnBasedRequest.evidenceRoot -Request $turnBasedRequest -Record $legacyTurnBased)
        $legacyTurnBasedManifest = Read-KmcJson (Join-Path $turnBasedRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $turnBasedRequest -Manifest $legacyTurnBasedManifest -Status 'PASS' -SubscenarioResults @($turnBasedSubresult)
    }

    Invoke-HarnessTest 'historical schema-v12 and schema-v13 combat evidence remain valid' {
        $legacyRealTime = Copy-TestJsonValue $combatRecord
        $legacyRealTime.schemaVersion = 12
        Remove-TestCombatIncomingRuleFields $legacyRealTime
        [void](Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $legacyRealTime)
        $legacyRealTimeManifest = Read-KmcJson (Join-Path $combatRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $combatRequest -Manifest $legacyRealTimeManifest -Status 'PASS' -SubscenarioResults @($combatSubresult)

        $legacyTurnBased = Copy-TestJsonValue $turnBasedRecord
        $legacyTurnBased.schemaVersion = 13
        Remove-TestCombatIncomingRuleFields $legacyTurnBased
        [void](Write-TestCombatEvidence -EvidenceRoot $turnBasedRequest.evidenceRoot -Request $turnBasedRequest -Record $legacyTurnBased)
        $legacyTurnBasedManifest = Read-KmcJson (Join-Path $turnBasedRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $turnBasedRequest -Manifest $legacyTurnBasedManifest -Status 'PASS' -SubscenarioResults @($turnBasedSubresult)
    }

    Invoke-HarnessTest 'historical schema-v14 and schema-v15 combat evidence remain valid' {
        $legacyRealTime = Copy-TestJsonValue $combatRecord
        $legacyRealTime.schemaVersion = 14
        Remove-TestCombatIncomingActorContextFields $legacyRealTime
        [void](Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $legacyRealTime)
        $legacyRealTimeManifest = Read-KmcJson (Join-Path $combatRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $combatRequest -Manifest $legacyRealTimeManifest -Status 'PASS' -SubscenarioResults @($combatSubresult)

        $legacyTurnBased = Copy-TestJsonValue $turnBasedRecord
        $legacyTurnBased.schemaVersion = 15
        Remove-TestCombatIncomingActorContextFields $legacyTurnBased
        [void](Write-TestCombatEvidence -EvidenceRoot $turnBasedRequest.evidenceRoot -Request $turnBasedRequest -Record $legacyTurnBased)
        $legacyTurnBasedManifest = Read-KmcJson (Join-Path $turnBasedRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $turnBasedRequest -Manifest $legacyTurnBasedManifest -Status 'PASS' -SubscenarioResults @($turnBasedSubresult)
    }

    Invoke-HarnessTest 'historical schema-v16 and schema-v17 combat evidence remain valid' {
        $legacyRealTime = Copy-TestJsonValue $combatRecord
        $legacyRealTime.schemaVersion = 16
        Remove-TestCombatNonPairPartyAiLeaseFields $legacyRealTime
        [void](Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $legacyRealTime)
        $legacyRealTimeManifest = Read-KmcJson (Join-Path $combatRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $combatRequest -Manifest $legacyRealTimeManifest -Status 'PASS' -SubscenarioResults @($combatSubresult)

        $legacyTurnBased = Copy-TestJsonValue $turnBasedRecord
        $legacyTurnBased.schemaVersion = 17
        Remove-TestCombatNonPairPartyAiLeaseFields $legacyTurnBased
        [void](Write-TestCombatEvidence -EvidenceRoot $turnBasedRequest.evidenceRoot -Request $turnBasedRequest -Record $legacyTurnBased)
        $legacyTurnBasedManifest = Read-KmcJson (Join-Path $turnBasedRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $turnBasedRequest -Manifest $legacyTurnBasedManifest -Status 'PASS' -SubscenarioResults @($turnBasedSubresult)
    }

    Invoke-HarnessTest 'combat miss validator rejects hit damage and identity mutations' {
        $mutations = @(
            @{name='forced hit';apply={param($value) $value.rules.forcedD20=20}},
            @{name='damage event';apply={param($value) $value.rules.damageRuleCount=1}},
            @{name='positive damage';apply={param($value) $value.rules.totalDamage=1}},
            @{name='native hit true';apply={param($value) $value.rules.lastAttackHit=$true}},
            @{name='native hit missing';apply={param($value) $value.rules.PSObject.Properties.Remove('lastAttackHit')}},
            @{name='native hit null';apply={param($value) $value.rules.lastAttackHit=$null}},
            @{name='hit result';apply={param($value) $value.rules.lastAttackResult='Hit'}},
            @{name='critical-hit result';apply={param($value) $value.rules.lastAttackResult='CriticalHit'}},
            @{name='unknown result';apply={param($value) $value.rules.lastAttackResult='Unknown'}},
            @{name='mirror-image result';apply={param($value) $value.rules.lastAttackResult='MirrorImage'}},
            @{name='concealment result';apply={param($value) $value.rules.lastAttackResult='Concealment'}},
            @{name='parried result';apply={param($value) $value.rules.lastAttackResult='Parried'}},
            @{name='wrong initiator';apply={param($value) $value.rules.lastInitiatorId='combat-mount'}},
            @{name='duplicate roll';apply={param($value) $value.rules.attackRollCount=2}},
            @{name='incoming attack wrong initiator';apply={param($value) $value.targetIncomingRules.firstAttack.initiatorId='combat-mount'}},
            @{name='pre-dispatch incoming attack';apply={param($value) $value.targetIncomingRules.preDispatchAttackRuleCount=1;$value.targetIncomingRules.firstAttack.beforeExpectedDispatch=$true}},
            @{name='incoming damage observed on miss';apply={param($value) $value.targetIncomingRules.damageRuleCount=1}}
        )
        foreach ($mutation in $mutations) {
            $candidate = Copy-TestJsonValue $missRecord
            & $mutation.apply $candidate
            [void](Write-TestCombatEvidence -EvidenceRoot $missRequest.evidenceRoot -Request $missRequest -Record $candidate)
            $candidateManifest = Read-KmcJson (Join-Path $missRequest.evidenceRoot 'runtime-artifacts.json')
            $threw = $false
            try { Assert-KmcCombatScenarioEvidence -Request $missRequest -Manifest $candidateManifest -Status 'PASS' -SubscenarioResults @($missSubresult) }
            catch { $threw = $true }
            Assert-Test $threw ("combat miss validator accepted mutation: " + [string]$mutation.name)
        }
    }

    Invoke-HarnessTest 'schema-v13 preserves a structured pre-combat turn-based FAIL' {
        $failureRecord = Copy-TestJsonValue $turnBasedRecord
        $failureRecord.schemaVersion = 13
        Remove-TestCombatIncomingRuleFields $failureRecord
        $failureRecord.status = 'FAIL'
        $failureRecord.assertionPassCount = 10
        $failureRecord.assertionFailCount = 1
        $failureRecord.errors = @('bounded pre-combat mode deadline')
        $failureRecord.turnBased.enabledAtMount = $false
        $failureRecord.turnBased.controllerInitialized = $false
        $failureRecord.turnBased.rosterContainsRider = $false
        $failureRecord.turnBased.rosterContainsMount = $false
        $failureRecord.turnBased.rosterContainsTarget = $false
        $failureRecord.turnBased.nativeRiderTurnStarted = $false
        $failureRecord.turnBased.currentTurnUnitIdAtDispatch = $null
        $failureRecord.turnBased.currentTurnActingAtDispatch = $false
        $failureRecord.turnBased.roundNumberAtDispatch = -1
        $failureRecord.turnBased.currentTurnUnitIdAtOutcome = $null
        $failureRecord.turnBased.currentTurnActingAtOutcome = $false
        [void](Write-TestCombatEvidence -EvidenceRoot $turnBasedRequest.evidenceRoot -Request $turnBasedRequest -Record $failureRecord)
        $failureManifest = Read-KmcJson (Join-Path $turnBasedRequest.evidenceRoot 'runtime-artifacts.json')
        $failureSubresult = [ordered]@{
            name=$turnBasedRequest.scenario;status='FAIL';assertionPassCount=10;assertionFailCount=1
            errors=@('bounded pre-combat mode deadline')
        }
        Assert-KmcCombatScenarioEvidence -Request $turnBasedRequest -Manifest $failureManifest -Status 'FAIL' -SubscenarioResults @($failureSubresult)
    }

    Invoke-HarnessTest 'schema-v12 preserves an exact observed target death transition' {
        $failureRecord = Copy-TestJsonValue $missRecord
        $failureRecord.schemaVersion = 12
        Remove-TestCombatIncomingRuleFields $failureRecord
        $failureRecord.status = 'FAIL'
        $failureRecord.assertionPassCount = 20
        $failureRecord.assertionFailCount = 1
        $failureRecord.errors = @('target life changed before native combat entry')
        $failureRecord.combatEntry.nativeJoin.targetConscious = $false
        $failureRecord.targetLife.lastObserved.lifeState = 'Dead'
        $failureRecord.targetLife.lastObserved.conscious = $false
        $failureRecord.targetLife.lastObserved.dead = $true
        $failureRecord.targetLife.lastObserved.finallyDead = $true
        $failureRecord.targetLife.lastObserved.damage = 120
        $failureRecord.targetLife.transitionCount = 1
        $failureRecord.targetLife.firstTransition.observed = $true
        $failureRecord.targetLife.firstTransition.previousLifeState = 'Conscious'
        $failureRecord.targetLife.firstTransition.currentLifeState = 'Dead'
        $failureRecord.targetLife.firstTransition.snapshot = Copy-TestJsonValue $failureRecord.targetLife.lastObserved
        [void](Write-TestCombatEvidence -EvidenceRoot $missRequest.evidenceRoot -Request $missRequest -Record $failureRecord)
        $failureManifest = Read-KmcJson (Join-Path $missRequest.evidenceRoot 'runtime-artifacts.json')
        $failureSubresult = [ordered]@{
            name=$missRequest.scenario;status='FAIL';assertionPassCount=20;assertionFailCount=1
            errors=@('target life changed before native combat entry')
        }
        Assert-KmcCombatScenarioEvidence -Request $missRequest -Manifest $failureManifest -Status 'FAIL' -SubscenarioResults @($failureSubresult)
    }

    Invoke-HarnessTest 'schema-v16 preserves exact pre-dispatch third-party attack and damage actor context' {
        $failureRecord = Copy-TestJsonValue $missRecord
        $failureRecord.schemaVersion = 16
        Remove-TestCombatNonPairPartyAiLeaseFields $failureRecord
        $failureRecord.status = 'FAIL'
        $failureRecord.assertionPassCount = 20
        $failureRecord.assertionFailCount = 1
        $failureRecord.errors = @('target received third-party damage before expected rider dispatch')
        $failureRecord.combatEntry.nativeJoin.targetConscious = $false
        $failureRecord.targetLife.lastObserved.lifeState = 'Dead'
        $failureRecord.targetLife.lastObserved.conscious = $false
        $failureRecord.targetLife.lastObserved.dead = $true
        $failureRecord.targetLife.lastObserved.finallyDead = $true
        $failureRecord.targetLife.lastObserved.damage = 15
        $failureRecord.targetLife.transitionCount = 1
        $failureRecord.targetLife.firstTransition.observed = $true
        $failureRecord.targetLife.firstTransition.previousLifeState = 'Conscious'
        $failureRecord.targetLife.firstTransition.currentLifeState = 'Dead'
        $failureRecord.targetLife.firstTransition.snapshot = Copy-TestJsonValue $failureRecord.targetLife.lastObserved
        $failureRecord.targetIncomingRules.dispatchMarkerSet = $false
        $failureRecord.targetIncomingRules.attackRuleCount = 1
        $failureRecord.targetIncomingRules.damageRuleCount = 1
        $failureRecord.targetIncomingRules.preDispatchAttackRuleCount = 1
        $failureRecord.targetIncomingRules.preDispatchDamageRuleCount = 1
        $failureRecord.targetIncomingRules.firstAttack.beforeExpectedDispatch = $true
        $failureRecord.targetIncomingRules.firstAttack.initiatorId = 'combat-third-party'
        $failureRecord.targetIncomingRules.firstAttack.initiatorBlueprintId = '44444444444444444444444444444444'
        $failureRecord.targetIncomingRules.firstAttack.initiatorEffectiveAiEnabled = $true
        $failureRecord.targetIncomingRules.firstAttack.initiatorRawAiEnabled = $true
        $failureRecord.targetIncomingRules.firstDamage.observed = $true
        $failureRecord.targetIncomingRules.firstDamage.beforeExpectedDispatch = $true
        $failureRecord.targetIncomingRules.firstDamage.initiatorId = 'combat-third-party'
        $failureRecord.targetIncomingRules.firstDamage.initiatorBlueprintId = '44444444444444444444444444444444'
        $failureRecord.targetIncomingRules.firstDamage.initiatorIsPlayerFaction = $true
        $failureRecord.targetIncomingRules.firstDamage.damage = 15
        $failureRecord.targetIncomingRules.firstDamage.attackRollPresent = $true
        $failureRecord.targetIncomingRules.firstDamage.weaponBlueprintId = '55555555555555555555555555555555'
        [void](Write-TestCombatEvidence -EvidenceRoot $missRequest.evidenceRoot -Request $missRequest -Record $failureRecord)
        $failureManifest = Read-KmcJson (Join-Path $missRequest.evidenceRoot 'runtime-artifacts.json')
        $failureSubresult = [ordered]@{
            name=$missRequest.scenario;status='FAIL';assertionPassCount=20;assertionFailCount=1
            errors=@('target received third-party damage before expected rider dispatch')
        }
        Assert-KmcCombatScenarioEvidence -Request $missRequest -Manifest $failureManifest -Status 'FAIL' -SubscenarioResults @($failureSubresult)
    }

    Invoke-HarnessTest 'turn-based combat validator rejects mode roster turn and restoration mutations' {
        $mutations = @(
            @{name='wrong evidence schema';apply={param($value) $value.schemaVersion=4;$value.PSObject.Properties.Remove('turnBased')}},
            @{name='wrong mode';apply={param($value) $value.mode='real-time'}},
            @{name='real-time unpause claim';apply={param($value) $value.dispatch.unpausedForRealTime=$true}},
            @{name='original mode enabled';apply={param($value) $value.turnBased.originalEnabled=$true}},
            @{name='temporary mode disabled';apply={param($value) $value.turnBased.temporaryEnabled=$false}},
            @{name='mode absent at mount';apply={param($value) $value.turnBased.enabledAtMount=$false}},
            @{name='controller uninitialized';apply={param($value) $value.turnBased.controllerInitialized=$false}},
            @{name='rider absent from roster';apply={param($value) $value.turnBased.rosterContainsRider=$false}},
            @{name='mount absent from roster';apply={param($value) $value.turnBased.rosterContainsMount=$false}},
            @{name='target absent from roster';apply={param($value) $value.turnBased.rosterContainsTarget=$false}},
            @{name='native rider turn not started';apply={param($value) $value.turnBased.nativeActionActorTurnStarted=$false}},
            @{name='wrong dispatch turn identity';apply={param($value) $value.turnBased.currentTurnUnitIdAtDispatch='combat-mount'}},
            @{name='dispatch turn not acting';apply={param($value) $value.turnBased.currentTurnActingAtDispatch=$false}},
            @{name='negative round';apply={param($value) $value.turnBased.roundNumberAtDispatch=-1}},
            @{name='wrong outcome turn identity';apply={param($value) $value.turnBased.currentTurnUnitIdAtOutcome='combat-target'}},
            @{name='outcome turn not acting';apply={param($value) $value.turnBased.currentTurnActingAtOutcome=$false}},
            @{name='restore callback incomplete';apply={param($value) $value.turnBased.restoreDeliveryCompleted=$false}},
            @{name='mode not restored';apply={param($value) $value.turnBased.modeRestored=$false}},
            @{name='persisted mode changed';apply={param($value) $value.turnBased.persistedValueUnchanged=$false}}
        )
        foreach ($mutation in $mutations) {
            $candidate = Copy-TestJsonValue $turnBasedRecord
            & $mutation.apply $candidate
            [void](Write-TestCombatEvidence -EvidenceRoot $turnBasedRequest.evidenceRoot -Request $turnBasedRequest -Record $candidate)
            $candidateManifest = Read-KmcJson (Join-Path $turnBasedRequest.evidenceRoot 'runtime-artifacts.json')
            $threw = $false
            try { Assert-KmcCombatScenarioEvidence -Request $turnBasedRequest -Manifest $candidateManifest -Status 'PASS' -SubscenarioResults @($turnBasedSubresult) }
            catch { $threw = $true }
            Assert-Test $threw ("turn-based combat validator accepted mutation: " + [string]$mutation.name)
        }
    }

    Invoke-HarnessTest 'combat validator retains non-qualifying schema-v1 evidence compatibility' {
        $legacyRecord = Copy-TestJsonValue $combatRecord
        $legacyRecord.schemaVersion = 1
        $legacyRecord.PSObject.Properties.Remove('reach')
        Remove-TestCombatWakeLeaseFields $legacyRecord
        $legacyRecord.PSObject.Properties.Remove('combatEntry')
        $legacyRecord.PSObject.Properties.Remove('dispatch')
        [void](Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $legacyRecord)
        $legacyManifest = Read-KmcJson (Join-Path $combatRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $combatRequest -Manifest $legacyManifest -Status 'FAIL'
    }

    Invoke-HarnessTest 'combat validator retains non-qualifying schema-v2 evidence compatibility' {
        $legacyRecord = Copy-TestJsonValue $combatRecord
        $legacyRecord.schemaVersion = 2
        $legacyRecord.PSObject.Properties.Remove('reach')
        Remove-TestCombatWakeLeaseFields $legacyRecord
        $legacyRecord.PSObject.Properties.Remove('combatEntry')
        [void](Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $legacyRecord)
        $legacyManifest = Read-KmcJson (Join-Path $combatRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $combatRequest -Manifest $legacyManifest -Status 'FAIL'
    }

    Invoke-HarnessTest 'combat validator retains non-qualifying schema-v3 evidence compatibility' {
        $legacyRecord = Copy-TestJsonValue $combatRecord
        $legacyRecord.schemaVersion = 3
        $legacyRecord.PSObject.Properties.Remove('reach')
        Remove-TestCombatWakeLeaseFields $legacyRecord
        $legacyRecord.command.PSObject.Properties.Remove('terminalReason')
        [void](Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $legacyRecord)
        $legacyManifest = Read-KmcJson (Join-Path $combatRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcCombatScenarioEvidence -Request $combatRequest -Manifest $legacyManifest -Status 'FAIL'
    }

    Invoke-HarnessTest 'combat validator rejects duplicate rules action cost and actor mutations' {
        $mutations = @(
            @{name='duplicate attack';apply={param($value) $value.rules.attackRuleCount=2}},
            @{name='duplicate damage';apply={param($value) $value.rules.damageRuleCount=2}},
            @{name='unexpected pair attack';apply={param($value) $value.rules.unexpectedPairAttackCount=1}},
            @{name='native hit false';apply={param($value) $value.rules.lastAttackHit=$false}},
            @{name='wrong command actor';apply={param($value) $value.command.actorId='combat-mount'}},
            @{name='non-completed terminal reason';apply={param($value) $value.command.terminalReason='InvalidOperationException: target-conscious'}},
            @{name='memory not queued';apply={param($value) $value.combatEntry.memoryQueued=$false}},
            @{name='player memory absent';apply={param($value) $value.combatEntry.playerGroupMemoryContainsTarget=$false}},
            @{name='target memory absent';apply={param($value) $value.combatEntry.targetGroupMemoryContainsRider=$false}},
            @{name='native combat absent';apply={param($value) $value.combatEntry.targetInCombat=$false}},
            @{name='target not awake';apply={param($value) $value.combatEntry.targetAwake=$false}},
            @{name='native join rider not in game';apply={param($value) $value.combatEntry.nativeJoin.riderInGame=$false}},
            @{name='native join target unconscious';apply={param($value) $value.combatEntry.nativeJoin.targetConscious=$false}},
            @{name='native join target ignored';apply={param($value) $value.combatEntry.nativeJoin.targetIgnoredByCombat=$true}},
            @{name='native join player enemy list absent';apply={param($value) $value.combatEntry.nativeJoin.playerGroupEnemiesContainsTarget=$false}},
            @{name='native join target ambush';apply={param($value) $value.combatEntry.nativeJoin.targetNotInStealthAmbush=$false}},
            @{name='native join Boolean coercion';apply={param($value) $value.combatEntry.nativeJoin.targetInGame='true'}},
            @{name='initiative unprepared';apply={param($value) $value.combatEntry.riderPrepared=$false}},
            @{name='initiative pending';apply={param($value) $value.combatEntry.riderInitiative=1.0}},
            @{name='game delta stopped';apply={param($value) $value.combatEntry.gameDeltaTime=0.0}},
            @{name='memory cleanup residue';apply={param($value) $value.combatEntry.memoryRemovedAtCleanup=$false}},
            @{name='pair start range absent';apply={param($value) $value.command.pairRangeSatisfiedAtStart=$false}},
            @{name='pair start outside radius';apply={param($value) $value.command.pairDistanceAtStart=4.051}},
            @{name='native executor outside admission';apply={param($value) $value.command.nativeExecutorDistanceAtStart=4.2}},
            @{name='native admission expansion escape';apply={param($value) $value.command.nativeAdmissionRadiusAtStart=4.751}},
            @{name='native adjustment flag mismatch';apply={param($value) $value.command.nativeAdmissionAdjusted=$false}},
            @{name='dispatch stayed paused';apply={param($value) $value.dispatch.pausedAtClick=$true}},
            @{name='dispatch initiative unavailable';apply={param($value) $value.dispatch.actionActorCanActInCombat=$false}},
            @{name='dispatch hands busy';apply={param($value) $value.dispatch.actionActorHandsBusy=$true}},
            @{name='dispatch equipment pending';apply={param($value) $value.dispatch.equipmentUpdateScheduled=$true}},
            @{name='pause not restored';apply={param($value) $value.dispatch.pauseRestored=$false}},
            @{name='missing rider Standard cost';apply={param($value) $value.resources.riderStandardAfter=0.0}},
            @{name='mount Standard cost';apply={param($value) $value.resources.mountStandardAfter=5.0}},
            @{name='delegated movement';apply={param($value) $value.command.repathCount=1;$value.movement.repathCount=1}},
            @{name='insufficient pair radius';apply={param($value) $value.pairApproachRadius=0.17;$value.targetDistanceAtClick=0.06}},
            @{name='target placement drift';apply={param($value) $value.targetDistanceAtClick=1.0}},
            @{name='reach evidence absent';apply={param($value) $value.PSObject.Properties.Remove('reach')}},
            @{name='reach rider blueprint malformed';apply={param($value) $value.reach.riderWeaponBlueprintId='not-a-blueprint'}},
            @{name='reach rider radius formula mismatch';apply={param($value) $value.reach.riderStoppingRadius=4.1}},
            @{name='reach Mammoth radius formula mismatch';apply={param($value) $value.reach.mountStoppingRadius=4.1}},
            @{name='reach initial distance inside rider boundary';apply={param($value) $value.reach.initialDistance=4.05}},
            @{name='reach initial rider probe admitted';apply={param($value) $value.reach.riderOutsideAtInitial=$false}},
            @{name='reach initial Mammoth probe admitted';apply={param($value) $value.reach.mountOutsideAtInitial=$false}},
            @{name='reach dispatch distance mismatch';apply={param($value) $value.reach.dispatchDistance=3.8}},
            @{name='reach rider targetability absent';apply={param($value) $value.reach.riderCanAttackTarget=$false}},
            @{name='reach Mammoth targetability absent';apply={param($value) $value.reach.mountCanAttackTarget=$false}},
            @{name='reach target cannot attack rider';apply={param($value) $value.reach.targetCanAttackRider=$false}},
            @{name='reach target cannot attack Mammoth';apply={param($value) $value.reach.targetCanAttackMount=$false}},
            @{name='reach inputs mutated';apply={param($value) $value.reach.inputsUnchangedAtDispatch=$false}},
            @{name='reach action radius mismatch';apply={param($value) $value.reach.actionRadiusMatches=$false}},
            @{name='reach rider outside at dispatch';apply={param($value) $value.reach.riderWithinAtDispatch=$false}},
            @{name='pose failure';apply={param($value) $value.pose.healthyAtOutcome=$false}},
            @{name='target native weapon source';apply={param($value) $value.targetProvisioning.blueprintEmptyHandWeaponBlueprintId='22222222222222222222222222222222'}},
            @{name='target native slot';apply={param($value) $value.targetProvisioning.targetNativeSingleAttackSlot='AdditionalLimb'}},
            @{name='target native source classification';apply={param($value) $value.targetProvisioning.targetWeaponUsesEmptyHandFallback=$false}},
            @{name='target native type';apply={param($value) $value.targetProvisioning.targetNativeSingleAttackWeaponIsNatural=$false}},
            @{name='target weapon mutation';apply={param($value) $value.targetProvisioning.additionalLimbCountAfter=1;$value.targetProvisioning.noWeaponProvisioningMutation=$false}},
            @{name='target provisioning loot';apply={param($value) $value.targetProvisioning.noLoot=$false}},
            @{name='target unexpectedly sleepless before lease';apply={param($value) $value.targetProvisioning.sleeplessBefore=$true}},
            @{name='target sleepless lease absent';apply={param($value) $value.targetProvisioning.sleeplessLeaseAcquired=$false}},
            @{name='target sleepless lease coercion';apply={param($value) $value.targetProvisioning.sleeplessLeaseAcquired='true'}},
            @{name='target life absent';apply={param($value) $value.PSObject.Properties.Remove('targetLife')}},
            @{name='target life creation unobserved';apply={param($value) $value.targetLife.immediatelyAfterCreation.observed=$false}},
            @{name='target life creation dead';apply={param($value) $value.targetLife.immediatelyAfterCreation.lifeState='Dead';$value.targetLife.immediatelyAfterCreation.conscious=$false;$value.targetLife.immediatelyAfterCreation.dead=$true}},
            @{name='target life activation unconscious';apply={param($value) $value.targetLife.atActivation.lifeState='Unconscious';$value.targetLife.atActivation.conscious=$false}},
            @{name='target life inconsistent projection';apply={param($value) $value.targetLife.lastObserved.dead=$true}},
            @{name='target life transition count coercion';apply={param($value) $value.targetLife.transitionCount='0'}},
            @{name='target life transition sentinel mismatch';apply={param($value) $value.targetLife.firstTransition.observed=$true}},
            @{name='target incoming rules absent';apply={param($value) $value.PSObject.Properties.Remove('targetIncomingRules')}},
            @{name='target dispatch marker absent';apply={param($value) $value.targetIncomingRules.dispatchMarkerSet=$false}},
            @{name='target pre-dispatch attack interference';apply={param($value) $value.targetIncomingRules.preDispatchAttackRuleCount=1;$value.targetIncomingRules.firstAttack.beforeExpectedDispatch=$true}},
            @{name='target incoming attack duplicate';apply={param($value) $value.targetIncomingRules.attackRuleCount=2}},
            @{name='target incoming attack wrong initiator';apply={param($value) $value.targetIncomingRules.firstAttack.initiatorId='combat-mount'}},
            @{name='target incoming attack missing group';apply={param($value) $value.targetIncomingRules.firstAttack.initiatorGroupId=$null}},
            @{name='target incoming attack outside party group';apply={param($value) $value.targetIncomingRules.firstAttack.initiatorGroupIsPlayerParty=$false}},
            @{name='target incoming attack different rider group';apply={param($value) $value.targetIncomingRules.firstAttack.initiatorSharesRiderGroup=$false}},
            @{name='target incoming attack indirect actor';apply={param($value) $value.targetIncomingRules.firstAttack.initiatorDirectlyControllable=$false}},
            @{name='target pre-dispatch damage interference';apply={param($value) $value.targetIncomingRules.preDispatchDamageRuleCount=1;$value.targetIncomingRules.firstDamage.beforeExpectedDispatch=$true}},
            @{name='target incoming damage wrong initiator';apply={param($value) $value.targetIncomingRules.firstDamage.initiatorId='combat-mount'}},
            @{name='non-pair party AI lease absent';apply={param($value) $value.PSObject.Properties.Remove('nonPairPartyAiLease')}},
            @{name='non-pair party AI lease not acquired';apply={param($value) $value.nonPairPartyAiLease.acquired=$false}},
            @{name='non-pair party AI lease wrong group';apply={param($value) $value.nonPairPartyAiLease.groupId='different-player-group'}},
            @{name='non-pair party AI lease non-player group';apply={param($value) $value.nonPairPartyAiLease.groupIsPlayerParty=$false}},
            @{name='non-pair party AI lease rider group mismatch';apply={param($value) $value.nonPairPartyAiLease.riderSharesGroup=$false}},
            @{name='non-pair party AI lease mount group mismatch';apply={param($value) $value.nonPairPartyAiLease.mountSharesGroup=$false}},
            @{name='non-pair party AI lease member count mismatch';apply={param($value) $value.nonPairPartyAiLease.memberCount=2}},
            @{name='non-pair party AI lease active validation failed';apply={param($value) $value.nonPairPartyAiLease.activeValidationPassed=$false}},
            @{name='non-pair party AI lease restore failed';apply={param($value) $value.nonPairPartyAiLease.restored=$false}},
            @{name='non-pair party AI lease unexpected error';apply={param($value) $value.nonPairPartyAiLease.lastError='AI lease drift'}},
            @{name='non-pair party AI lease member is rider';apply={param($value) $value.nonPairPartyAiLease.members[0].unitId='combat-rider'}},
            @{name='non-pair party AI lease member indirect';apply={param($value) $value.nonPairPartyAiLease.members[0].directlyControllable=$false}},
            @{name='non-pair party AI lease member absent';apply={param($value) $value.nonPairPartyAiLease.members[0].inState=$false}},
            @{name='non-pair party AI lease command before acquisition';apply={param($value) $value.nonPairPartyAiLease.members[0].commandsEmptyBefore=$false}},
            @{name='non-pair party AI lease command during lease';apply={param($value) $value.nonPairPartyAiLease.members[0].commandsEmptyDuring=$false}},
            @{name='non-pair party AI lease raw AI active during lease';apply={param($value) $value.nonPairPartyAiLease.members[0].rawAiDuring=$true}},
            @{name='non-pair party AI lease effective AI active during lease';apply={param($value) $value.nonPairPartyAiLease.members[0].effectiveAiDuring=$true}},
            @{name='non-pair party AI lease command after restore';apply={param($value) $value.nonPairPartyAiLease.members[0].commandsEmptyAfter=$false}},
            @{name='non-pair party AI lease raw AI restore mismatch';apply={param($value) $value.nonPairPartyAiLease.members[0].rawAiAfter=$false}},
            @{name='non-pair party AI lease effective AI restore mismatch';apply={param($value) $value.nonPairPartyAiLease.members[0].effectiveAiAfter=$false}},
            @{name='target residue';apply={param($value) $value.cleanup.targetRemoved=$false}},
            @{name='target entity residue';apply={param($value) $value.cleanup.targetEntityRemoved=$false}},
            @{name='target group residue';apply={param($value) $value.cleanup.runtimeGroupRemoved=$false}},
            @{name='target faction residue';apply={param($value) $value.cleanup.runtimeFactionRemoved=$false}},
            @{name='target sleepless lease residue';apply={param($value) $value.cleanup.sleeplessLeaseReleased=$false}},
            @{name='target sleepless cleanup coercion';apply={param($value) $value.cleanup.sleeplessLeaseReleased='true'}},
            @{name='non-pair party AI lease cleanup residue';apply={param($value) $value.cleanup.nonPairPartyAiLeaseRestored=$false}},
            @{name='non-pair party AI lease cleanup coercion';apply={param($value) $value.cleanup.nonPairPartyAiLeaseRestored='true'}}
        )
        foreach ($mutation in $mutations) {
            $candidate = Copy-TestJsonValue $combatRecord
            & $mutation.apply $candidate
            [void](Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $candidate)
            $candidateManifest = Read-KmcJson (Join-Path $combatRequest.evidenceRoot 'runtime-artifacts.json')
            $threw = $false
            try { Assert-KmcCombatScenarioEvidence -Request $combatRequest -Manifest $candidateManifest -Status 'PASS' -SubscenarioResults @($combatSubresult) }
            catch { $threw = $true }
            Assert-Test $threw ("combat validator accepted mutation: " + [string]$mutation.name)
        }
    }

    Invoke-HarnessTest 'combat artifact must be exact manifested immutable content' {
        [void](Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $combatRecord -OmitManifestRecord)
        $emptyManifest = Read-KmcJson (Join-Path $combatRequest.evidenceRoot 'runtime-artifacts.json')
        $unmanifestedRejected = $false
        try { Assert-KmcKnownRuntimeArtifactsManifested $combatRequest.evidenceRoot $emptyManifest }
        catch { $unmanifestedRejected = $true }
        Assert-Test $unmanifestedRejected 'known combat artifact was accepted without a manifest record'

        $combatManifestHash = Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $combatRecord
        Add-Content -LiteralPath (Join-Path $combatRequest.evidenceRoot 'combat-scenario-evidence.jsonl') -Value ' '
        $hashRejected = $false
        try { Get-KmcValidatedOrchestrationArtifactManifestHash $combatRequest | Out-Null }
        catch { $hashRejected = $true }
        Assert-Test $hashRejected 'combat evidence byte mutation passed manifest validation'

        $combatManifestHash = Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $combatRecord -ManifestKind 'scenario-evidence'
        $wrongKindManifest = Read-KmcJson (Join-Path $combatRequest.evidenceRoot 'runtime-artifacts.json')
        $kindRejected = $false
        try { Assert-KmcCombatScenarioEvidence -Request $combatRequest -Manifest $wrongKindManifest -Status 'PASS' -SubscenarioResults @($combatSubresult) }
        catch { $kindRejected = $true }
        Assert-Test $kindRejected 'combat artifact passed under the wrong manifest kind'
    }

    $combatManifestHash = Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $combatRecord

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

    Invoke-HarnessTest 'combat-lifecycle-suite binds exact superseding boundary semantics and preserves schema-v2 history' {
        $combatLifecycleRows=@(Get-KmcCombatLifecycleRuntimeRows)
        $combatLifecycleRequest=[pscustomobject][ordered]@{
            runId='combat-lifecycle-suite-test';scenario='combat-lifecycle-suite';branch=$v2Request.branch;commit=$v2Request.commit
            productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
            evidenceRoot=(Join-Path $runtimeEvidenceTestRoot 'combat-lifecycle-suite-test')
        }
        $records=New-Object 'Collections.Generic.List[object]';$sequence=0
        foreach($row in $combatLifecycleRows) {
            $records.Add((New-TestLifecycleEvidenceRecord $combatLifecycleRequest ($sequence++) $row 'pre-mount' 'Unmounted'))
            $records.Add((New-TestLifecycleEvidenceRecord $combatLifecycleRequest ($sequence++) $row 'mounted-next-frame' 'Mounted'))
            $records.Add((New-TestLifecycleEvidenceRecord $combatLifecycleRequest ($sequence++) $row 'cleanup-next-frame' 'Unmounted' -WithCleanup))
            $records.Add((New-TestLifecycleEvidenceRecord $combatLifecycleRequest ($sequence++) $row 'row-finish' 'Unmounted' -WithCleanup -RowStatus 'PASS' -AssertionPassCount 1 -AssertionFailCount 0))
        }
        $records.Add((New-TestLifecycleEvidenceRecord $combatLifecycleRequest $sequence $combatLifecycleRows[-1] 'engine-finalization' 'Unmounted' -WithCleanup))
        $valid=$records.ToArray()
        $subresults=@($combatLifecycleRows|ForEach-Object{[pscustomobject][ordered]@{name=$_;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}})
        [void](Write-TestLifecycleEvidence -EvidenceRoot $combatLifecycleRequest.evidenceRoot -Request $combatLifecycleRequest -Records $valid)
        $manifest=Read-KmcJson (Join-Path $combatLifecycleRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcLifecycleScenarioEvidence -Request $combatLifecycleRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults $subresults

        $candidate=Copy-TestJsonValue $valid
        $candidate[0].schemaVersion=2
        Assert-TestLifecycleEvidenceRejected $combatLifecycleRequest $candidate $subresults 'combat lifecycle accepted historical schema-v2 semantics'

        $candidate=Copy-TestJsonValue $valid
        $cleanupRecord=@($candidate|Where-Object{[string]$_.row -ceq 'mounted-pair-combat-start-retained' -and [string]$_.phase -ceq 'cleanup-next-frame'})[0]
        $cleanupRecord.boundaryExercise.relationshipStateAfterBoundary='Unmounted'
        Assert-TestLifecycleEvidenceRejected $combatLifecycleRequest $candidate $subresults 'combat-start retention accepted an Unmounted boundary state'

        $candidate=Copy-TestJsonValue $valid
        $deathRecord=@($candidate|Where-Object{[string]$_.row -ceq 'mounted-pair-mount-death-cleanup' -and [string]$_.phase -ceq 'cleanup-next-frame'})[0]
        $deathRecord.boundaryExercise.actorRole='rider'
        Assert-TestLifecycleEvidenceRejected $combatLifecycleRequest $candidate $subresults 'mount death accepted rider actor ownership'

        $candidate=Copy-TestJsonValue $valid
        $endRecord=@($candidate|Where-Object{[string]$_.row -ceq 'mounted-pair-combat-end-retained' -and [string]$_.phase -ceq 'cleanup-next-frame'})[0]
        $endRecord.boundaryExercise.deliveries=@($endRecord.boundaryExercise.deliveries[0])
        Assert-TestLifecycleEvidenceRejected $combatLifecycleRequest $candidate $subresults 'combat end accepted a missing end delivery'

        $candidate=Copy-TestJsonValue $valid
        $pendingCleanup=@($candidate|Where-Object{[string]$_.row -ceq 'mounted-pair-exception-cleanup' -and [string]$_.phase -ceq 'cleanup-next-frame'})[0]
        $pendingCleanup.boundaryExercise=New-TestCombatLifecycleBoundaryExercise -Row 'mounted-pair-exception-cleanup' -Observed:$false
        Assert-TestLifecycleEvidenceRejected $combatLifecycleRequest $candidate $subresults 'combat lifecycle accepted pending evidence after cleanup'

        $candidate=Copy-TestJsonValue $valid
        $prePose=@($candidate|Where-Object{[string]$_.phase -ceq 'pre-mount'})[0]
        $prePose.pose.profileId='medium-humanoid-mammoth-v1'
        Assert-TestLifecycleEvidenceRejected $combatLifecycleRequest $candidate $subresults 'combat lifecycle accepted an active pose identity before mount'

        $candidate=Copy-TestJsonValue $valid
        $mountedPose=@($candidate|Where-Object{[string]$_.phase -ceq 'mounted-next-frame'})[0]
        $mountedPose.pose.profileId=$null
        Assert-TestLifecycleEvidenceRejected $combatLifecycleRequest $candidate $subresults 'combat lifecycle accepted a missing mounted Mammoth pose identity'

        $candidate=Copy-TestJsonValue $valid
        $restoredPose=@($candidate|Where-Object{[string]$_.phase -ceq 'cleanup-next-frame'})[0]
        $restoredPose.pose.baselineRestoreVerified=$false
        Assert-TestLifecycleEvidenceRejected $combatLifecycleRequest $candidate $subresults 'combat lifecycle accepted unverified pose restoration after cleanup'

        $historical=Copy-TestJsonValue $validLifecycleRecords
        Assert-Test ([long]$historical[0].schemaVersion -eq 2 -and $null -eq $historical[0].PSObject.Properties['boundaryExercise']) 'historical schema-v2 lifecycle evidence shape was rewritten'
    }

    Invoke-HarnessTest 'native incapacitation rows bind real actor transition and exact EventBus cleanup' {
        foreach($row in @(Get-KmcNativeIncapacitationRuntimeRows)) {
            $nativeRequest=[pscustomobject][ordered]@{
                runId=('native-incap-test-' + $row);scenario=$row;branch=$v2Request.branch;commit=$v2Request.commit
                productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
                evidenceRoot=(Join-Path $runtimeEvidenceTestRoot ('native-incap-test-' + $row))
            }
            $records=@(
                (New-TestLifecycleEvidenceRecord $nativeRequest 0 $row 'pre-mount' 'Unmounted'),
                (New-TestLifecycleEvidenceRecord $nativeRequest 1 $row 'mounted-next-frame' 'Mounted'),
                (New-TestLifecycleEvidenceRecord $nativeRequest 2 $row 'cleanup-next-frame' 'Unmounted' -WithCleanup),
                (New-TestLifecycleEvidenceRecord $nativeRequest 3 $row 'row-finish' 'Unmounted' -WithCleanup -RowStatus 'PASS' -AssertionPassCount 4 -AssertionFailCount 0),
                (New-TestLifecycleEvidenceRecord $nativeRequest 4 $row 'engine-finalization' 'Unmounted' -WithCleanup))
            $subresult=[pscustomobject][ordered]@{name=$row;status='PASS';assertionPassCount=4;assertionFailCount=0;errors=@()}
            [void](Write-TestLifecycleEvidence -EvidenceRoot $nativeRequest.evidenceRoot -Request $nativeRequest -Records $records)
            $manifest=Read-KmcJson (Join-Path $nativeRequest.evidenceRoot 'runtime-artifacts.json')
            Assert-KmcLifecycleScenarioEvidence -Request $nativeRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($subresult)

            $omittedEmptyCleanupErrors=Copy-TestJsonValue $records
            foreach($successfulRecord in @($omittedEmptyCleanupErrors|Where-Object{[string]$_.phase -cin @('cleanup-next-frame','row-finish','engine-finalization')})) {
                [void]$successfulRecord.boundaryExercise.deliveries[0].PSObject.Properties.Remove('cleanupErrors')
            }
            [void](Write-TestLifecycleEvidence -EvidenceRoot $nativeRequest.evidenceRoot -Request $nativeRequest -Records $omittedEmptyCleanupErrors)
            $omittedManifest=Read-KmcJson (Join-Path $nativeRequest.evidenceRoot 'runtime-artifacts.json')
            Assert-KmcLifecycleScenarioEvidence -Request $nativeRequest -Manifest $omittedManifest -Status 'PASS' -SubscenarioResults @($subresult)

            $candidate=Copy-TestJsonValue $records
            $candidate[2].boundaryExercise.deliveries[0].cleanupErrors=@('impossible successful cleanup error')
            Assert-TestLifecycleEvidenceRejected $nativeRequest $candidate @($subresult) 'native incapacitation accepted cleanup errors on a successful delivery'

            $candidate=Copy-TestJsonValue $records
            $candidate[2].actorLifeTransition.lifeStateAfter='Dead'
            Assert-TestLifecycleEvidenceRejected $nativeRequest $candidate @($subresult) 'native incapacitation accepted a Dead outcome'

            $candidate=Copy-TestJsonValue $records
            $candidate[2].actorLifeTransition.nativeCurrentLifeState='Dead'
            Assert-TestLifecycleEvidenceRejected $nativeRequest $candidate @($subresult) 'native incapacitation accepted a native Conscious-to-Dead callback'

            $candidate=Copy-TestJsonValue $records
            $candidate[2].actorLifeTransition.damageImmediatelyAfterMutation=100
            Assert-TestLifecycleEvidenceRejected $nativeRequest $candidate @($subresult) 'native incapacitation accepted an inexact immediate damage write'

            $candidate=Copy-TestJsonValue $records
            $candidate[2].actorLifeTransition.nativeDeliveryCount=2
            Assert-TestLifecycleEvidenceRejected $nativeRequest $candidate @($subresult) 'native incapacitation accepted duplicate EventBus delivery'

            $candidate=Copy-TestJsonValue $records
            $candidate[2].boundaryExercise.deliveries[0].source='synthetic-handler'
            Assert-TestLifecycleEvidenceRejected $nativeRequest $candidate @($subresult) 'native incapacitation accepted a synthetic lifecycle source'

            $candidate=Copy-TestJsonValue $records
            $candidate[1].actorLifeTransition.mutationIssued=$true
            Assert-TestLifecycleEvidenceRejected $nativeRequest $candidate @($subresult) 'native incapacitation accepted mutation before the mounted evidence boundary'

            $cleanupFailed=Copy-TestJsonValue $records
            foreach($failedRecord in @($cleanupFailed|Where-Object{[string]$_.phase -cin @('cleanup-next-frame','row-finish','engine-finalization')})) {
                $failedRecord.actorLifeTransition.nativeLifeObservationCount=2
                $failedRecord.boundaryExercise.deliveries[0].stateAfter='Faulted'
                $failedRecord.boundaryExercise.deliveries[0].cleanupSucceeded=$false
                $failedRecord.boundaryExercise.deliveries[0].cleanupErrors=@('RestoreMovementAuthority: diagnostic failure')
            }
            $cleanupFailed[3].rowStatus='FAIL';$cleanupFailed[3].assertionPassCount=47;$cleanupFailed[3].assertionFailCount=2
            $cleanupFailed[3].recordErrors=@('Native cleanup faulted before retry.')
            $cleanupFailed[4].recordErrors=@('native cleanup diagnostic')
            $cleanupFailedSubresult=[pscustomobject][ordered]@{name=$row;status='FAIL';assertionPassCount=47;assertionFailCount=2;errors=@('Native cleanup faulted before retry.')}
            [void](Write-TestLifecycleEvidence -EvidenceRoot $nativeRequest.evidenceRoot -Request $nativeRequest -Records $cleanupFailed)
            $cleanupFailedManifest=Read-KmcJson (Join-Path $nativeRequest.evidenceRoot 'runtime-artifacts.json')
            Assert-KmcLifecycleScenarioEvidence -Request $nativeRequest -Manifest $cleanupFailedManifest -Status 'FAIL' -SubscenarioResults @($cleanupFailedSubresult)

            $failed=Copy-TestJsonValue $records
            foreach($failedRecord in @($failed|Where-Object{[string]$_.phase -cin @('cleanup-next-frame','row-finish','engine-finalization')})) {
                $failedRecord.actorLifeTransition.lifeStateAfter='Conscious'
                $failedRecord.actorLifeTransition.consciousAfter=$true
                $failedRecord.actorLifeTransition.damageAfter=101
                $failedRecord.actorLifeTransition.nativeDeliveryCount=0
                $failedRecord.actorLifeTransition.nativeLifeObservationCount=0
                $failedRecord.actorLifeTransition.nativeObservedActorId=$null
                $failedRecord.actorLifeTransition.nativePreviousLifeState=$null
                $failedRecord.actorLifeTransition.nativeCurrentLifeState=$null
                $failedRecord.actorLifeTransition.postDeliveryRecoveryObserved=$false
                $failedRecord.triggerScope.nativeDeliveryObserved=$false
                $failedRecord.boundaryExercise=New-TestCombatLifecycleBoundaryExercise -Row $row -Observed:$false
                $failedRecord.cleanup.trigger='Exception'
            }
            $failed[3].rowStatus='FAIL';$failed[3].assertionPassCount=44;$failed[3].assertionFailCount=1
            $failed[3].recordErrors=@('Lifecycle row exceeded its 15 second monotonic deadline.')
            $failed[4].recordErrors=@('native probe timeout')
            $failedSubresult=[pscustomobject][ordered]@{
                name=$row;status='FAIL';assertionPassCount=44;assertionFailCount=1
                errors=@('Lifecycle row exceeded its 15 second monotonic deadline.')
            }
            [void](Write-TestLifecycleEvidence -EvidenceRoot $nativeRequest.evidenceRoot -Request $nativeRequest -Records $failed)
            $failedManifest=Read-KmcJson (Join-Path $nativeRequest.evidenceRoot 'runtime-artifacts.json')
            Assert-KmcLifecycleScenarioEvidence -Request $nativeRequest -Manifest $failedManifest -Status 'FAIL' -SubscenarioResults @($failedSubresult)
        }
    }

    $boundaryRow = 'mounted-pair-load-safety'
    $boundaryRequest = [pscustomobject][ordered]@{
        schemaVersion=2;runId='boundary-individual-test';scenario=$boundaryRow;branch=$v2Request.branch;commit=$v2Request.commit
        productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
        transactionToken=$v2Request.transactionToken;evidenceRoot=(Join-Path $runtimeEvidenceTestRoot 'boundary-individual-test')
        fixture=$v2Fixture;qualificationSuite=$v2Request.qualificationSuite
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
    Invoke-HarnessTest 'native lifecycle observation detail is optional bounded text' {
        $nativeLifecycle = [pscustomobject][ordered]@{
            baselineSequence=100;deliveryCount=1;deliveries=@([pscustomobject][ordered]@{
                sequence=101;boundary='GameModeStarted';source='IGameModeHandler.OnGameModeStart(FullScreenUi)'
                stateBefore='Mounted';stateAfter='Mounted';cleanupTrigger=$null
                cleanupAttempted=$false;cleanupSucceeded=$true
                detail='mode=FullScreenUi;relationship=Mounted;riderViewExact=True'
            })
        }
        Assert-KmcBoundaryNativeLifecycleEvidence $nativeLifecycle
        $nativeLifecycle.deliveries[0].detail = 42
        $threw = $false
        try { Assert-KmcBoundaryNativeLifecycleEvidence $nativeLifecycle } catch { $threw = $true }
        Assert-Test $threw 'native lifecycle validator accepted a non-string observation detail'
        $nativeLifecycle.deliveries[0].detail = 'x' * 8193
        $threw = $false
        try { Assert-KmcBoundaryNativeLifecycleEvidence $nativeLifecycle } catch { $threw = $true }
        Assert-Test $threw 'native lifecycle validator accepted an unbounded observation detail'
    }
    Invoke-HarnessTest 'PASS native lifecycle boundary rows require independent delivery and restoration evidence' {
        foreach ($nativeRow in @(Get-KmcNativeLifecycleBoundaryRuntimeRows)) {
            $nativeRequest = [pscustomobject][ordered]@{
                schemaVersion=2;runId=('native-boundary-' + $nativeRow);scenario=$nativeRow;branch=$v2Request.branch;commit=$v2Request.commit
                productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
                transactionToken=$v2Request.transactionToken;evidenceRoot=(Join-Path $runtimeEvidenceTestRoot ('native-boundary-' + $nativeRow))
                fixture=$v2Fixture;qualificationSuite=$v2Request.qualificationSuite
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
            transactionToken=$v2Request.transactionToken;evidenceRoot=$liveRoot;fixture=$v2Fixture;qualificationSuite=$v2Request.qualificationSuite
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
            fixture=$v2Fixture;qualificationSuite=$v2Request.qualificationSuite
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
            fixture=$v2Fixture;qualificationSuite=$v2Request.qualificationSuite
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
    Invoke-HarnessTest 'PASS distance-door validator requires ordinary input, exact door identity, and strict post-open traversal' {
        $doorRow = 'mounted-distance-door-interaction'
        $doorRequest = [pscustomobject][ordered]@{
            runId='distance-door-evidence-test';scenario=$doorRow;branch=$v2Request.branch;commit=$v2Request.commit
            productVersion=$v2Request.productVersion;dllSha256=$v2Request.dllSha256;dllMvid=$v2Request.dllMvid
            evidenceRoot=(Join-Path $runtimeEvidenceTestRoot 'distance-door-evidence-test')
        }
        $doorSubresult = [pscustomobject][ordered]@{name=$doorRow;status='PASS';assertionPassCount=20;assertionFailCount=0;errors=@()}
        $doorTelemetry = @((New-TestMovementTelemetryRecord $doorRequest $doorRow 0))
        $doorRowRecord = New-TestMovementRowRecord $doorRequest $doorRow 3
        $doorRowRecord.unexpectedRepathCount = 1
        $doorScenario = @(
            (New-TestMovementDoorReadinessRecord $doorRequest 0),
            (New-TestMovementPathProbeRecord $doorRequest $doorRow 1 'DoorFar' $true),
            (New-TestMovementPathReplacementRecord $doorRequest 2),
            $doorRowRecord)
        [void](Write-TestMovementEvidence $doorRequest.evidenceRoot $doorRequest $doorTelemetry $doorScenario)
        $manifest = Read-KmcJson (Join-Path $doorRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcMovementScenarioEvidence -Request $doorRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($doorSubresult)

        $attributedReplacements = New-Object 'Collections.Generic.List[object]'
        for ($replacementIndex = 1; $replacementIndex -le 5; $replacementIndex++) {
            $replacement = New-TestMovementPathReplacementRecord $doorRequest (1 + $replacementIndex)
            $replacement.replacementIndex = $replacementIndex
            $replacement.previousPathId = 7 + $replacementIndex
            $replacement.newPathId = 8 + $replacementIndex
            $replacement.previousPathFirstObservedFrame = 100 + (10 * $replacementIndex)
            $replacement.tileHandlerLastUpdateFrame = $replacement.previousPathFirstObservedFrame
            $replacement.replacementObservedFrame = $replacement.tileHandlerLastUpdateFrame + 2
            $attributedReplacements.Add($replacement)
        }
        $attributedRowRecord = New-TestMovementRowRecord $doorRequest $doorRow 7
        $attributedRowRecord.unexpectedRepathCount = 5
        $attributedScenario = @(
            (New-TestMovementDoorReadinessRecord $doorRequest 0),
            (New-TestMovementPathProbeRecord $doorRequest $doorRow 1 'DoorFar' $true)) +
            @($attributedReplacements.ToArray()) + @($attributedRowRecord)
        [void](Write-TestMovementEvidence $doorRequest.evidenceRoot $doorRequest $doorTelemetry $attributedScenario)
        $attributedManifest = Read-KmcJson (Join-Path $doorRequest.evidenceRoot 'runtime-artifacts.json')
        Assert-KmcMovementScenarioEvidence -Request $doorRequest -Manifest $attributedManifest -Status 'PASS' -SubscenarioResults @($doorSubresult)

        foreach ($mutation in @('missing-control','missing-door','wrong-waypoint','wrong-target','non-strict','extra-interaction-leg',
            'missing-fixture-lease','wrong-original-state','missing-temporary-enable','fixture-not-restored',
            'missing-readiness','pending-cut','missing-astar','invalid-graph-queue','readiness-row-mismatch',
            'readiness-frame-coherence','invalid-tile-frame','missing-replacement','replacement-count-mismatch',
            'replacement-frame-coherence','replacement-command-lost','replacement-path-error')) {
            $rowRecord = New-TestMovementRowRecord $doorRequest $doorRow 3
            $rowRecord.unexpectedRepathCount = 1
            $readiness = New-TestMovementDoorReadinessRecord $doorRequest 0
            $probe = New-TestMovementPathProbeRecord $doorRequest $doorRow 1 'DoorFar' $true
            $replacement = New-TestMovementPathReplacementRecord $doorRequest 2
            switch ($mutation) {
                'missing-control' { $rowRecord.unmountedDoorControlPassed = $false }
                'missing-door' { $rowRecord.door = $null }
                'wrong-waypoint' { $rowRecord.waypointCount = 2; $rowRecord.endpointQualifiedWaypointCount = 2 }
                'wrong-target' { $probe.requested.x = 3.5; $probe.endpoint.x = 3.5 }
                'non-strict' { $probe.strictDoor = $false }
                'extra-interaction-leg' { $rowRecord.screenshots = @(New-TestMovementScreenshotRecords 'mounted-pair-doorway' $true) }
                'missing-fixture-lease' { $rowRecord.doorFixtureLeaseCaptured = $false }
                'wrong-original-state' { $rowRecord.doorFixtureOriginalEnabled = $true }
                'missing-temporary-enable' { $rowRecord.doorFixtureTemporaryEnableUsed = $false }
                'fixture-not-restored' { $rowRecord.doorFixtureRestored = $false }
                'pending-cut' { $readiness.finalNavmeshCutRequiresUpdate = $true }
                'missing-astar' { $readiness.astarPathPresent = $false }
                'invalid-graph-queue' { $readiness.astarGraphUpdatesQueued = $null }
                'readiness-row-mismatch' { $readiness.door = 'Area/OtherDoor' }
                'readiness-frame-coherence' { $readiness.unityFrameStrictlyAfterTileHandlerLastUpdate = $false }
                'invalid-tile-frame' { $readiness.tileHandlerLastUpdateFrame = 'not-a-frame' }
                'replacement-count-mismatch' { $rowRecord.unexpectedRepathCount = 2 }
                'replacement-frame-coherence' { $replacement.previousPathFirstObservedNotNewerThanTileUpdateFrame = $false }
                'replacement-command-lost' { $replacement.commandReferenceRetained = $false }
                'replacement-path-error' { $replacement.pathError = $true }
            }
            if ($mutation -ceq 'missing-readiness') {
                $probe.sequence = 0L
                $replacement.sequence = 1L
                $rowRecord.sequence = 2L
                $records = @($probe,$replacement,$rowRecord)
            }
            elseif ($mutation -ceq 'missing-replacement') {
                $rowRecord.sequence = 2L
                $records = @($readiness,$probe,$rowRecord)
            }
            else {
                $records = @($readiness,$probe,$replacement,$rowRecord)
            }
            [void](Write-TestMovementEvidence $doorRequest.evidenceRoot $doorRequest $doorTelemetry $records)
            $mutatedManifest = Read-KmcJson (Join-Path $doorRequest.evidenceRoot 'runtime-artifacts.json')
            $threw = $false
            try { Assert-KmcMovementScenarioEvidence -Request $doorRequest -Manifest $mutatedManifest -Status 'PASS' -SubscenarioResults @($doorSubresult) } catch { $threw = $true }
            Assert-Test $threw "PASS distance-door evidence accepted mutation $mutation"
        }

        $unattributedReplacements = New-Object 'Collections.Generic.List[object]'
        for ($replacementIndex = 1; $replacementIndex -le 3; $replacementIndex++) {
            $replacement = New-TestMovementPathReplacementRecord $doorRequest (1 + $replacementIndex)
            $replacement.replacementIndex = $replacementIndex
            $replacement.previousPathId = 7 + $replacementIndex
            $replacement.newPathId = 8 + $replacementIndex
            $replacement.previousPathFirstObservedFrame = 200 + (10 * $replacementIndex)
            $replacement.tileHandlerLastUpdateFrame = $replacement.previousPathFirstObservedFrame - 1
            $replacement.replacementObservedFrame = $replacement.previousPathFirstObservedFrame + 1
            $replacement.previousPathFirstObservedNotNewerThanTileUpdateFrame = $false
            $unattributedReplacements.Add($replacement)
        }
        $unattributedRowRecord = New-TestMovementRowRecord $doorRequest $doorRow 5
        $unattributedRowRecord.unexpectedRepathCount = 3
        $unattributedScenario = @(
            (New-TestMovementDoorReadinessRecord $doorRequest 0),
            (New-TestMovementPathProbeRecord $doorRequest $doorRow 1 'DoorFar' $true)) +
            @($unattributedReplacements.ToArray()) + @($unattributedRowRecord)
        [void](Write-TestMovementEvidence $doorRequest.evidenceRoot $doorRequest $doorTelemetry $unattributedScenario)
        $unattributedManifest = Read-KmcJson (Join-Path $doorRequest.evidenceRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcMovementScenarioEvidence -Request $doorRequest -Manifest $unattributedManifest -Status 'PASS' -SubscenarioResults @($doorSubresult) } catch { $threw = $true }
        Assert-Test $threw 'PASS distance-door evidence accepted excessive healthy but non-frame-attributed path replacements'
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
    Invoke-HarnessTest 'movement telemetry rejects invalid TileHandler frame identity or relation' {
        foreach ($mutation in @('invalid-frame','incoherent-relation')) {
            $telemetry = New-TestMovementTelemetryRecord $movementRequest $movementRow 0
            if ($mutation -ceq 'invalid-frame') { $telemetry.tileHandlerLastUpdateFrame = 'not-a-frame' }
            else { $telemetry.unityFrameStrictlyAfterTileHandlerLastUpdate = $false }
            $scenario = @((New-TestMovementPathProbeRecord $movementRequest $movementRow 0),(New-TestMovementRowRecord $movementRequest $movementRow 1))
            [void](Write-TestMovementEvidence $movementRequest.evidenceRoot $movementRequest @($telemetry) $scenario)
            $manifest = Read-KmcJson (Join-Path $movementRequest.evidenceRoot 'runtime-artifacts.json')
            $threw = $false
            try { Assert-KmcMovementScenarioEvidence -Request $movementRequest -Manifest $manifest -Status 'PASS' -SubscenarioResults @($movementSubresult) } catch { $threw = $true }
            Assert-Test $threw "movement telemetry accepted TileHandler frame mutation $mutation"
        }
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
        Assert-Test ($telemetryProducerNames.Count -eq 222 -and $telemetryFixtureNames.Count -eq 222 -and
            @($telemetryProducerNames | Where-Object { [Array]::IndexOf($telemetryFixtureNames, $_) -lt 0 }).Count -eq 0 -and
            @($telemetryFixtureNames | Where-Object { [Array]::IndexOf($telemetryProducerNames, $_) -lt 0 }).Count -eq 0) 'movement telemetry fixture/validator field set is not the exact 222-field producer schema'

        $rowPayloadMarker = $engineSource.IndexOf('kind = "movement-row-result"', [StringComparison]::Ordinal)
        $rowStart = $engineSource.LastIndexOf('WriteEvidence(new', $rowPayloadMarker, [StringComparison]::Ordinal)
        $rowEnd = $engineSource.IndexOf('            });', $rowStart, [StringComparison]::Ordinal)
        $rowBlock = $engineSource.Substring($rowStart, $rowEnd - $rowStart)
        $rowOwnedNames = @('schemaVersion','runId','scenario','row','branch','commit','productVersion','dllSha256','dllMvid','sequence','utcTimestamp')
        $rowPayloadNames = @([regex]::Matches($rowBlock, '(?m)^\s{16}([A-Za-z_]\w*)\s*(?:=|,)') |
            ForEach-Object { $_.Groups[1].Value })
        $rowProducerNames = @($rowOwnedNames + $rowPayloadNames)
        $rowFixtureNames = @((New-TestMovementRowRecord $movementRequest $movementRow 1).Keys | ForEach-Object { [string]$_ })
        Assert-Test ($rowPayloadNames.Count -eq 212 -and $rowProducerNames.Count -eq 223 -and $rowFixtureNames.Count -eq 223 -and
            @($rowProducerNames | Where-Object { [Array]::IndexOf($rowFixtureNames, $_) -lt 0 }).Count -eq 0 -and
            @($rowFixtureNames | Where-Object { [Array]::IndexOf($rowProducerNames, $_) -lt 0 }).Count -eq 0) 'movement row fixture/validator field set is not the exact 223-field producer schema'
        $runtimeSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\KingmakerMountedPairRuntime.cs'))
        Assert-Test ($runtimeSource.Contains('riderAvoidanceWasDisabled = riderStockAgent.AvoidanceDisabled;') -and
            $runtimeSource.Contains('riderStockAgent.AvoidanceDisabled = false;') -and
            $runtimeSource.IndexOf('avoidanceLeaseOwned = false;', $runtimeSource.IndexOf('riderStockAgent.AvoidanceDisabled = false;', [StringComparison]::Ordinal), [StringComparison]::Ordinal) -gt
                $runtimeSource.IndexOf('riderStockAgent.AvoidanceDisabled = false;', [StringComparison]::Ordinal) -and
            $runtimeSource.Contains('AvoidanceRestorationExpectation.Matches(')) 'runtime does not release its counted avoidance lease once before validating captured state plus native consciousness'
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

    Invoke-HarnessTest 'private-alpha stabilization is pair-local, view-safe, and preserves native action ownership' {
        $lifecycleSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedLifecycleSubscriber.cs'))
        $runtimeSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\KingmakerMountedPairRuntime.cs'))
        $attachmentSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\ScopedTransformAttachmentLease.cs'))
        $stabilizationSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MountedStabilizationPolicy.cs'))
        $combatSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedCombatController.cs'))
        $relationshipSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\GameMountedRelationshipService.cs'))
        $doorSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedDoorInteractionCommand.cs'))
        $playerActionSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPlayerActionController.cs'))
        $overlaySource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPlayerActionOverlay.cs'))
        $patchSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPatchController.cs'))
        $turnPolicySource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Domain\MountedCombatSpatialPolicy.cs'))
        $ledgerSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\NativeLifecycleDeliveryLedger.cs'))
        $combatEngineSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeCombatScenarioEngine.cs'))
        $movementEngineSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeMovementScenarioEngine.cs'))
        $movementTelemetrySource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\MovementTelemetryWriter.cs'))
        $automationHostSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\RuntimeAutomationHost.cs'))
        $movementValidatorSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'scripts\runtime\RuntimeHarness.Common.ps1'))

        Assert-Test ($stabilizationSource.Contains('string.Equals(exactModeName, "FullScreenUi", StringComparison.Ordinal)') -and
            $stabilizationSource.Contains('string.Equals(exactModeName, "EscMode", StringComparison.Ordinal)') -and
            $stabilizationSource.Contains('return MountedGameModeDisposition.PreserveNonWorldUi;') -and
            $lifecycleSource.Contains('MountedGameModePolicy.CanRetainMountedRelationship(gameMode.ToString())')) `
            'character, map, or menu modes are still treated as generic cleanup boundaries'
        Assert-Test ($ledgerSource.Contains('public string Detail { get; set; }') -and
            $lifecycleSource.Contains('Observe(boundary, source, service.CapturePresentationObservation(false))') -and
            $lifecycleSource.Contains('Cleanup(boundary, source, CleanupTrigger.GameModeBoundary, service.CapturePresentationObservation(false))') -and
            $runtimeSource.Contains('observationScope=') -and
            $runtimeSource.Contains('includeUiOwnership ? "full-ui" : "mode-lightweight"') -and
            $runtimeSource.Contains('actionBarOwner=') -and
            $runtimeSource.Contains('actionBarReactiveActive=') -and
            $runtimeSource.Contains('actionBarCanUseAbilities=') -and
            $runtimeSource.Contains('turnUnitDirectlyControllable=') -and
            $runtimeSource.Contains('pointerInGui=') -and
            $runtimeSource.Contains('riderCommands=') -and
            $runtimeSource.Contains('mountCommands=') -and
            $runtimeSource.Contains('portraitOwnerCount=') -and
            $runtimeSource.Contains('portraitActiveOwnerCount=') -and
            $runtimeSource.Contains('cameraOwner=') -and
            -not $lifecycleSource.Contains('source + ";" + service.CapturePresentationObservation()')) `
            'presentation telemetry changes canonical native lifecycle source identities'
        Assert-Test ($lifecycleSource.Contains('MountedViewAttachmentPolicy.Classify(') -and
            $lifecycleSource.Contains('CleanupTrigger.ViewReplaced') -and
            $runtimeSource.Contains('ReleaseReplacementRiderViewFromOwnedAnchor') -and
            $attachmentSource.Contains('public bool ReleaseInheritedReplacement(') -and
            $attachmentSource.Contains('setParent(replacement, originalParent, true);')) `
            'polymorph/view replacement does not release the stock replacement before KMC anchor cleanup'
        $replacementRelease = $runtimeSource.IndexOf('ReleaseReplacementRiderViewFromOwnedAnchor', [StringComparison]::Ordinal)
        $poseRestore = $runtimeSource.IndexOf('poseAdapter.Deconfigure();', $replacementRelease, [StringComparison]::Ordinal)
        $attachmentRestore = $runtimeSource.IndexOf('riderAttachmentLease.Restore();', $poseRestore, [StringComparison]::Ordinal)
        Assert-Test ($replacementRelease -ge 0 -and $poseRestore -gt $replacementRelease -and $attachmentRestore -gt $poseRestore) `
            'stock replacement release is not ordered before old-view pose and attachment restoration'
        Assert-Test ($patchSource.Contains('PatchExact(typeof(ClickGroundHandler), "RunCommand", 0x060093DC') -and
            $patchSource.Contains('nameof(PatchMethods.GroundCommandPrefix), nameof(PatchMethods.GroundCommandPostfix)') -and
            $patchSource.Contains('TryAdmitGroundCommand(unit)') -and
            $combatSource.Contains('CompleteGroundCommandAdmission') -and
            $combatSource.Contains('activeRiderTurnGroundMove') -and
            $combatSource.Contains('DriveRiderTurnGroundMovement();') -and
            $combatSource.Contains('command.TickApproaching();') -and
            $combatSource.Contains('command.Tick();') -and
            $combatSource.Contains('LastGroundMoveUsedRiderTurnAdapter = true;') -and
            $combatSource.Contains('LastGroundMoveSlotRestored = commands != null') -and
            $combatSource.Contains('MountedPairTurnPolicy.CanDriveRiderGroundMovement(') -and
            $turnPolicySource.Contains('public static bool CanDriveRiderGroundMovement(') -and
            $combatSource.Contains('requestedUnit != relationship.Rider') -and
            $combatSource.IndexOf('Cancel("ground command");', [StringComparison]::Ordinal) -gt
                $combatSource.IndexOf('requestedUnit != relationship.Rider', [StringComparison]::Ordinal) -and
            -not $patchSource.Contains('PatchBridge.Combat?.Cancel("ground command")') -and
            $turnPolicySource.Contains('public static bool CanAdmitRiderGroundMovement(')) `
            'turn-based rider ground clicks do not retain exact Mammoth Move-slot ownership and rider-turn accounting'
        Assert-Test ($turnPolicySource.Contains('public static bool ShouldPreserveIndependentMountTurn(') -and
            $stabilizationSource.Contains('public static class MountedTurnSelectionPolicy') -and
            $relationshipSource.Contains('MountedTurnSelectionPolicy.Classify(') -and
            $relationshipSource.Contains('MountedSelectionDisposition.PreserveNativeMountTurn') -and
            $relationshipSource.Contains('MountedTurnSelectionPolicy.CanUseNativeMountTurnGroundCommand(') -and
            -not $patchSource.Contains('CombatController), "StartTurn"') -and
            -not $patchSource.Contains('StartTurnPostfix') -and
            -not $combatSource.Contains('ShouldEndMountTurn')) `
            'private-alpha stabilization still suppresses the Mammoth native turn'
        Assert-Test ($patchSource.Contains('PatchExact(typeof(UnitCommands), "Run", 0x060026B2') -and
            $patchSource.Contains('nameof(PatchMethods.UnitCommandRunPrefix)') -and
            $combatSource.Contains('MountedStockAttackPolicy.ShouldReject(') -and
            $combatSource.Contains('command.GetType() == typeof(UnitAttack)') -and
            $stabilizationSource.Contains('Mounted ranged attacks are not supported in this private alpha.') -and
            $stabilizationSource.Contains('relationshipMounted && (ownerIsExactRider || ownerIsExactMount) &&') -and
            $stabilizationSource.Contains('commandIsExactStockUnitAttack;')) `
            'mounted stock attack rejection is not exact-pair-local at the native UnitCommands admission seam'
        Assert-Test ($stabilizationSource.Contains('public static class MountedInteractionRoutingPolicy') -and
            $stabilizationSource.Contains('relationshipMounted && commandOwnerIsExactRider &&') -and
            $combatSource.Contains('command.GetType() == typeof(UnitInteractWithObject)') -and
            $combatSource.Contains('stockInteraction.Interaction.GetType() == typeof(StandardDoor)') -and
            $patchSource.Contains('TryRouteMountedDoorInteraction(__instance, ref cmd)') -and
            $doorSource.Contains('new UnitMoveTo(door.transform.position, GetDoorApproachRadius())') -and
            $doorSource.Contains('mount.Commands.Run(delegatedMove);') -and
            $doorSource.Contains('door.Interact(rider);') -and
            $doorSource.Contains('interactionCount != 0') -and
            $doorSource.Contains('mountMoveSlotRestored = mount.Commands != null') -and
            -not $doorSource.Contains('rider.Commands.Run(') -and
            -not $doorSource.Contains('door.Interact(mount)')) `
            'distant door routing is not exact StandardDoor-only with Mammoth path and rider interaction ownership'
        Assert-Test ($movementEngineSource.Contains('string.Equals(currentRow, "mounted-distance-door-interaction", StringComparison.Ordinal)') -and
            $movementEngineSource.Contains('new ClickMapObjectHandler().OnClick(') -and
            $movementEngineSource.Contains('clickAccepted && combat.HasActiveDoorInteraction') -and
            $movementEngineSource.Contains('outcome.InteractionCount == 1 && outcome.DelegatedMoveStartCount == 1') -and
            $movementEngineSource.Contains('outcome.DoorStateChanged && outcome.RiderPathSuppressed && outcome.MountMoveSlotRestored') -and
            $movementEngineSource.Contains('MountedDistanceDoorFixturePolicy.CanTemporarilyEnable(') -and
            $movementEngineSource.Contains('RestoreDistanceDoorFixtureLease()') -and
            $movementEngineSource.Contains('MountedDistanceDoorFixturePolicy.IsExactlyRestored(') -and
            $stabilizationSource.Contains('public static class MountedDistanceDoorTraversalReadinessPolicy') -and
            $movementEngineSource.Contains('distanceDoorNavmeshCut.RequiresUpdate()') -and
            $movementEngineSource.Contains('MountedDistanceDoorTraversalReadinessPolicy.IsReady(') -and
            $movementEngineSource.Contains('kind = "door-traversal-readiness"') -and
            $movementEngineSource.Contains('astarGraphUpdatesQueued = astarPath == null ? (bool?)null : astarPath.IsAnyGraphUpdatesQueued') -and
            $movementTelemetrySource.Contains('astarGraphUpdatesQueued = astarPath == null ? (bool?)null : astarPath.IsAnyGraphUpdatesQueued') -and
            $movementEngineSource.Contains('tileHandlerLastUpdateFrame = Pathfinding.Util.TileHandler.LastUpdateFrame') -and
            $movementTelemetrySource.Contains('tileHandlerLastUpdateFrame = Pathfinding.Util.TileHandler.LastUpdateFrame') -and
            $movementEngineSource.Contains('kind = "navigation-path-replacement"') -and
            $movementEngineSource.Contains('previousPathFirstObservedNotNewerThanTileUpdateFrame') -and
            $movementEngineSource.Contains('commandReferenceRetained = currentCommandAtReplacement != null') -and
            $movementEngineSource.Contains('DoorTraversalReadinessTimeoutSeconds') -and
            -not $movementEngineSource.Contains('TileHandlerHelper') -and
            -not $movementEngineSource.Contains('.ForceUpdate()') -and
            $movementEngineSource.Contains('string.Equals(row, "mounted-distance-door-interaction", StringComparison.Ordinal)') -and
            $movementEngineSource.Contains('BeginExactNavigation(NavigationMode.Normal, doorFarPoint, true, "door-mounted")') -and
            $automationHostSource.Contains('new RuntimeMovementScenarioEngine(') -and
            $automationHostSource.Contains('request, relationship, playerAction, combat, diagnosticSettings,') -and
            $automationHostSource.Contains('logger, request.EvidenceRoot);')) `
            'distance-door runtime proof bypasses ordinary map-object input, exact one-shot interaction, or strict post-open traversal'
        Assert-Test ($movementEngineSource.Contains('var tileFrameAttributedRefresh =') -and
            $movementEngineSource.Contains('navigationUnattributedRepaths <= MaximumUnexpectedRepaths') -and
            $movementEngineSource.Contains('rowUnattributedRepaths <= MaximumUnexpectedRepaths * Math.Max(1, rowWaypointCount)') -and
            $movementEngineSource.Contains('unexpectedRepathCount = rowUnexpectedRepaths') -and
            $movementValidatorSource.Contains('$rowUnattributedPathReplacements = @($rowPathReplacements | Where-Object') -and
            $movementValidatorSource.Contains('excessive unattributed path replacements')) `
            'distance-door TileHandler refresh classification does not preserve raw telemetry and independently reject excessive unattributed churn'
        Assert-Test ($combatEngineSource.Contains('playerAction.ArmCombatActionFromOverlay(AttackAction)') -and
            $combatEngineSource.Contains('ArmedThroughPlayerFacingCombatController = humanPlayArmedThroughPlayerAction') -and
            $combatEngineSource.Contains('OverlayActivationWorldClickSuppressed = humanPlayPropagatedWorldClickSuppressed') -and
            $combatEngineSource.Contains('? (IsTurnBasedRow ? 52 : 48)') -and
            $combatEngineSource.Contains('ObserveNativeMammothTurnControls(turnController)') -and
            $combatEngineSource.Contains('IsNativeTurnUiStructurallyInteractable(') -and
            $combatEngineSource.Contains('ClickGroundHandler.MoveSelectedUnitsToPoint(nativeMammothGroundDestination, false);') -and
            $combatEngineSource.Contains('nativeMammothGroundCommand.Executor == mount') -and
            $combatEngineSource.Contains('nativeMammothPhysicalPointerQualification = "manual-required"') -and
            $combatEngineSource.Contains('presentationAfterTurnBasedEnable = "<not-observed>"') -and
            $combatEngineSource.Contains('presentationAfterNativeMammothGroundInput = "<not-observed>"') -and
            $automationHostSource.Contains('request, relationship, playerAction, combat, lifecycle')) `
            'human-play qualification bypasses the exact player-facing combat-action controller before its native unit click'
        Assert-Test ($patchSource.Contains('PatchExact(typeof(UnitCombatCooldownsController), "TickOnUnit", 0x0600934A') -and
            $patchSource.Contains('nameof(PatchMethods.CombatCooldownPrefix), nameof(PatchMethods.CombatCooldownPostfix)') -and
            $patchSource.Contains('RuntimeAutomationHost.ObserveCombatCooldownTick(') -and
            $automationHostSource.Contains('active?.combatEngine?.ObserveCombatCooldownTick(') -and
            $combatEngineSource.Contains('step != CombatEngineStep.AwaitCombatFrame || unit == null || unit != AttackActor') -and
            $combatEngineSource.Contains('initiativeTickObservation.Observe(') -and
            -not $patchSource.Contains('Cooldown.Initiative =')) `
            'initiative diagnosis does not remain an exact actor-scoped observation-only native cooldown probe'
        Assert-Test ($patchSource.Contains('PatchExact(typeof(UnitCommand), "Interrupt", 0x060027AC') -and
            $patchSource.Contains('nameof(PatchMethods.CommandInterruptPrefix)') -and
            $patchSource.Contains('PatchBridge.Combat?.ObserveCommandInterrupt(__instance);') -and
            $combatSource.Contains('ReferenceEquals(command, observedNativeMountTurnMove)') -and
            $combatSource.Contains('new System.Diagnostics.StackTrace(1, false)') -and
            $combatSource.Contains('DescribeNativeMountTurnMoveInterruptState(command as UnitMoveTo)') -and
            $combatSource.Contains('commandApproachRadius=') -and
            $combatSource.Contains('agentApproachRadius=') -and
            $combatSource.Contains('mechanicsToTarget=') -and
            $combatSource.Contains('targetToPathEnd=') -and
            $combatSource.Contains('turnUnitExact=') -and
            $patchSource.Contains('PatchExact(typeof(UnitMovementAgent), "CompleteMovement", 0x060018B0, Type.EmptyTypes, nameof(PatchMethods.CompleteMovementPrefix));') -and
            $patchSource.Contains('PatchExact(typeof(UnitCommand), "get_IsUnitEnoughClose", 0x06002784, Type.EmptyTypes, null, nameof(PatchMethods.IsUnitEnoughClosePostfix));') -and
            $patchSource.Contains('TryCompleteNativeMountTurnMoveAtReachedPathEnd(__instance)') -and
            $patchSource.Contains('ShouldTreatNativeMountTurnMoveAsEnoughClose(__instance)') -and
            $combatSource.Contains('MountedTurnGroundCompletionPolicy.CanBridgeReachedPathEnd(') -and
            $combatSource.Contains('command.GetType() == typeof(UnitMoveTo)') -and
            $combatSource.Contains('agent.Stop();') -and
            $combatEngineSource.Contains('combat.BeginNativeMountTurnMoveObservation(nativeMammothGroundCommand);') -and
            $combatEngineSource.Contains('nativeMammothGroundInterruptSource = combat.LastNativeMountTurnMoveInterruptSource;')) `
            'native Mammoth terminal diagnosis mutates or observes commands beyond the one exact armed stock move'
        Assert-Test ($stabilizationSource.Contains('public sealed class MountedOverlayWorldInputGuard') -and
            $stabilizationSource.Contains('private const int MaximumPropagationFrameDelta = 2;') -and
            $overlaySource.Contains('ArmCombatActionFromOverlay(MountedCombatActionKind.RiderMelee)') -and
            $playerActionSource.Contains('combat.MarkPlayerFacingOverlayActivation(Time.frameCount);') -and
            $combatSource.Contains('overlayWorldInputGuard.TryConsumePropagatedWorldClick(Time.frameCount)') -and
            $combatSource.IndexOf('TrySuppressPropagatedOverlayWorldClick()', [StringComparison]::Ordinal) -lt
                $combatSource.IndexOf('var action = ArmedAction;', [StringComparison]::Ordinal) -and
            $combatSource.IndexOf('TrySuppressPropagatedOverlayWorldClick()',
                $combatSource.IndexOf('public bool TryAdmitGroundCommand', [StringComparison]::Ordinal),
                [StringComparison]::Ordinal) -lt
                $combatSource.IndexOf('Cancel("ground command");', [StringComparison]::Ordinal)) `
            'overlay combat-button activation can leak a same-click unit/ground command into the world'
        Assert-Test (-not $patchSource.Contains('Renderer.enabled') -and
            -not $patchSource.Contains('GameObject.SetActive') -and
            -not $runtimeSource.Contains('renderer.enabled = true')) `
            'stabilization introduced a broad renderer or GameObject enabling patch'
        Assert-Test ($stabilizationSource.Contains('MountedCleanupFeedbackPolicy') -and
            $stabilizationSource.Contains('case CleanupTrigger.SaveRequested:') -and
            $stabilizationSource.Contains('case CleanupTrigger.AreaUnloading:') -and
            $stabilizationSource.Contains('case CleanupTrigger.ViewReplaced:') -and
            $playerActionSource.Contains('MountedCleanupFeedbackPolicy.Describe(transition.Trigger.Value)')) `
            'intentional save, area, and body/view cleanup does not retain its exact player-facing reason'
    }

    $v2ResultPath = Join-Path $testRoot 'runtime-result-v2.json'
    $v2Final = New-KmcRuntimeResultV2 -Request ([pscustomobject]$v2Request) -ValidatedGameResult ([pscustomobject]$v2GameResult) -StartedAtUtc $gameStarted -ModsRestored $true -BaselineImmutable $true -WorkingRestored $true -SaveWriteAllowlistPassed $true -RestoredSaveInventoryDigest ('33'*32) -GameResultSha256 (Get-KmcSha256 $v2GameResultPath)
    Write-KmcJsonAtomic $v2ResultPath $v2Final
    Invoke-HarnessTest 'runtime result schema accepts recomputed restored save-backed PASS' {
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeResult.ps1') -ResultPath $v2ResultPath -RequestPath $v2RequestPath
    }

    Invoke-HarnessTest 'runtime final-result validator accepts exact manifested combat evidence' {
        $combatManifestHash = Write-TestCombatEvidence -EvidenceRoot $combatRequest.evidenceRoot -Request $combatRequest -Record $combatRecord
        $combatGameResult = Copy-TestJsonValue $v2GameResult
        foreach ($name in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')) {
            $combatGameResult.$name = $combatRequest.$name
        }
        $combatGameResult.evidenceManifestSha256 = $combatManifestHash
        $combatGameResult.subscenarioTotal = 1
        $combatGameResult.subscenarioPassCount = 1
        $combatGameResult.subscenarioFailCount = 0
        $combatGameResult.assertionPassCount = 25
        $combatGameResult.assertionFailCount = 0
        $combatGameResult.subscenarioResults = @($combatSubresult)
        $combatGameResultPath = Join-Path $combatRequest.evidenceRoot 'runtime-game-result.json'
        Write-KmcJsonAtomic $combatGameResultPath $combatGameResult
        $combatResult = New-KmcRuntimeResultV2 -Request ([pscustomobject]$combatRequest) -ValidatedGameResult ([pscustomobject]$combatGameResult) -StartedAtUtc $gameStarted -ModsRestored $true -BaselineImmutable $true -WorkingRestored $true -SaveWriteAllowlistPassed $true -RestoredSaveInventoryDigest ('44'*32) -GameResultSha256 (Get-KmcSha256 $combatGameResultPath)
        $combatResultPath = Join-Path $testRoot 'runtime-result-combat.json'
        Write-KmcJsonAtomic $combatResultPath $combatResult
        & (Join-Path $PSScriptRoot 'runtime\Test-RuntimeResult.ps1') -ResultPath $combatResultPath -RequestPath $combatRequestPath
    }

    Invoke-HarnessTest 'horse native-asset audit uses exact Kingmaker view-load and summoned-pony contracts' {
        $horseAuditSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\HorseNativeAssetAuditService.cs')
        Assert-Test ($horseAuditSource.Contains('new[] { typeof(bool) }, null);') -and
            $horseAuditSource.Contains('load.Invoke(prefab, new object[] { false })') -and
            -not $horseAuditSource.Contains('BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null')) `
            'horse audit does not bind the exact WeakResourceLink Load(Boolean) runtime contract'
        Assert-Test ($horseAuditSource.Contains('SummonedPonyBlueprintGuid = "3f95557fc806db741b500a5735990841"') -and
            $horseAuditSource.Contains('SummonedPonyPrefabGuid = "447d2907feec82545b3773fbb4709588"') -and
            $horseAuditSource.Contains('"pony-reference-scan-complete"')) `
            'horse audit does not distinguish the exact summoned pony from a completed negative reverse-reference scan'
        Assert-Test ($horseAuditSource.Contains('["portraitDiscovery"] = new JObject()') -and
            $horseAuditSource.Contains('"exact-native-horse-portrait-absent"') -and
            $horseAuditSource.Contains('"native-portrait-search-complete"')) `
            'horse audit does not preserve the bounded native Horse/Pony portrait and icon search contract'
        Assert-Test ($horseAuditSource.Contains('["schemaVersion"] = 3') -and
            $horseAuditSource.Contains('["kmcRuntimeBlueprints"] = new JArray()') -and
            $horseAuditSource.Contains('runtimeValues.Contains(item.Value)') -and
            $horseAuditSource.Contains('ReferenceEquals(item.Value, expectation.Value)') -and
            $horseAuditSource.Contains('"kmc-runtime-blueprints-exact-self-owned"') -and
            $horseAuditSource.Contains('"reserved-kmc-guids-unclaimed-by-stock"') -and
            $horseAuditSource.Contains('NativeMountedControlService.MountAbilityGuid')) `
            'horse audit does not distinguish reference-identical KMC runtime definitions from its stock portrait/asset projection'
        $horseBlueprintSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\HorseCompanionBlueprintService.cs')
        $nativeControlSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\NativeMountedControlService.cs')
        Assert-Test ($horseBlueprintSource.Contains('AssertReservedGuidAbsent(library, blueprintList, UnitGuid);') -and
            $horseBlueprintSource.Contains('AssertReservedGuidAbsent(library, blueprintList, FeatureGuid);') -and
            $horseBlueprintSource.Contains('AssertReservedGuidAbsent(library, blueprintList, UpgradeGuid);') -and
            $horseBlueprintSource.Contains('AssertReservedGuidAbsent(library, blueprintList, PortraitGuid);') -and
            $nativeControlSource.Contains('AssertGuidAbsent(library, guid);')) `
            'KMC Horse/native-control production registration no longer rejects pre-existing deterministic-GUID collisions'
    }

    Invoke-HarnessTest 'horse native-controls UX repairs are exact-pair scoped and preserve historical schema' {
        $patchSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedPatchController.cs'))
        $animationSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedAnimationAdapter.cs'))
        $ikSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\MountedDollRoomIkAdapter.cs'))
        $horseBlueprintSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\HorseCompanionBlueprintService.cs'))
        $horseScenarioSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\HorseCompanionUnmountedScenarioEngine.cs'))
        $runtimeLauncherSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'scripts\runtime\Invoke-KingmakerRuntimeScenario.ps1'))
        $projectSource = [IO.File]::ReadAllText((Join-Path $repoRoot 'src\KingmakerMountedCombat\KingmakerMountedCombat.csproj'))

        Assert-Test ($patchSource.Contains('PatchExact(typeof(UnitAnimationManager), "Tick", 0x06001605, Type.EmptyTypes, nameof(PatchMethods.AnimationTickPrefix));') -and
            $patchSource.Contains('PatchExact(typeof(AttackHandInfo), "CreateAnimationHandleForAttack", 0x0600265A, new[] { typeof(IEnumerable<AttackHandInfo>) }, null, nameof(PatchMethods.AttackAnimationPostfix));') -and
            $patchSource.Contains('PatchBridge.Animation?.RestoreExactDelegatedMountLocomotion(__instance);') -and
            $patchSource.Contains('PatchBridge.Animation?.SupplyExactHorsePrimaryAnimation(__instance);') -and
            $animationSource.Contains('combat.TryGetExactRiderTurnDelegatedMoveForAnimation(mount, out move, out source)') -and
            $animationSource.Contains('combat.TryGetExactHorsePrimaryAnimationContext(attack, out command, out horse)') -and
            $animationSource.Contains('.OfType<UnitAnimationActionSpecialAttack>()') -and
            $animationSource.Contains('if (actions.Length != 1)') -and
            $animationSource.Contains('attack.AnimationHandle = handle;') -and
            -not $animationSource.Contains('catch')) `
            'Horse locomotion or primary-animation repair is not exact-token, exact-context, single-native-action, or exception-transparent'

        Assert-Test ($patchSource.Contains('PatchExact(typeof(IKController), "SetupIkSystem", 0x0600156C, new[] { typeof(Character) }, nameof(PatchMethods.DollRoomIkSetupPrefix));') -and
            $patchSource.Contains('PatchExact(typeof(IKController), "SetupFbbik", 0x0600156D, Type.EmptyTypes, nameof(PatchMethods.DollRoomFbbikPrefix), nameof(PatchMethods.DollRoomFbbikPostfix));') -and
            $ikSource.Contains('relationship.State != RelationshipState.Mounted') -and
            $ikSource.Contains('(!ReferenceEquals(unit, rider) && !ReferenceEquals(unit, mount))') -and
            $ikSource.Contains('controller.CharacterUnitEntity = unit.View;') -and
            $ikSource.Contains('(ReferenceEquals(unit, rider) || ReferenceEquals(unit, mount))') -and
            -not $ikSource.Contains('catch')) `
            'DollRoom IK attribution repair is not exact mounted-pair scoped or still hides a stock exception'

        Assert-Test ($horseBlueprintSource.Contains('internal const string PortraitGuid = "6874a165bf8bda3531ee4e2abc10c899";') -and
            $horseBlueprintSource.Contains('new PortraitData(null, small, medium, large)') -and
            $horseBlueprintSource.Contains('ImageConversion.LoadImage(texture, bytes, true)') -and
            $projectSource.Contains('EmbeddedResource Include="Assets\HorsePortraitLarge.png"') -and
            $projectSource.Contains('EmbeddedResource Include="Assets\HorsePortraitMedium.png"') -and
            $projectSource.Contains('EmbeddedResource Include="Assets\HorsePortraitSmall.png"') -and
            $projectSource.Contains('EmbeddedResource Include="Assets\HorseIcon.png"')) `
            'Horse portrait surfaces are not bound to the exact original embedded KMC portrait/icon set'

        Assert-Test ($horseScenarioSource.Contains('Game.Instance?.SelectedAbilityHandler') -and
            $horseScenarioSource.Contains('handler.SetAbility(data);') -and
            $horseScenarioSource.Contains('handler.GetPriority(targetObject, targetPosition);') -and
            $horseScenarioSource.Contains('handler.GetTarget(targetObject, targetPosition, data);') -and
            $horseScenarioSource.Contains('handler.OnClick(targetObject, targetPosition, 0, false, false);') -and
            $horseScenarioSource.Contains('handler.DropAbility();') -and
            $horseScenarioSource.Contains('dollRoomPhaseStartedAtSeconds') -and
            $horseScenarioSource.Contains('observations["mountedRiderOutcome"] = CaptureMountedOutcome(') -and
            $horseScenarioSource.Contains('mountedRiderOutcome,') -and
            $horseScenarioSource.Contains('IncludesNativeControlsUx);') -and
            $horseScenarioSource.Contains('if (includeAnimation)') -and
            $runtimeLauncherSource.Contains("'horse-native-controls-ux-suite'")) `
            'focused Horse UX scenario bypasses the native selected-ability path, lacks bounded DollRoom observation, or changes historical schema-v4 output'
    }

    Invoke-HarnessTest 'horse native-asset audit validator binds exact manifested evidence and subscenario totals' {
        $horseRoot = Join-Path $runtimeEvidenceTestRoot 'horse-native-audit-validator'
        New-Item -ItemType Directory -Path $horseRoot -Force | Out-Null
        $horseRequest = [pscustomobject]@{
            runId='horse-native-audit-validator';scenario='horse-native-asset-audit';branch='codex/mounted-combat-phase3-horse'
            commit=('1'*40);productVersion='0.1.0-phase3a-dev.2';evidenceRoot=$horseRoot
        }
        $horseBody = [ordered]@{
            disableHands=$false;emptyHandWeapon=$null;primaryHand=$null;secondaryHand=$null
            additionalLimbs=@();additionalSecondaryLimbs=@()
        }
        $horseView = [ordered]@{
            rootName='HorseRiding';viewType='Kingmaker.View.UnitEntityView'
            rootLocalPosition=[ordered]@{x=0;y=0;z=0};rootLocalRotation=[ordered]@{x=0;y=0;z=0;w=1};rootLocalScale=[ordered]@{x=1;y=1;z=1}
            transformCount=122;transformNames=@('Chest','L_Stirrup','R_Stirrup');importantTransforms=@();boneNames=@();meshNames=@();materialNames=@()
            componentTypes=@('Kingmaker.View.UnitMovementAgent');colliders=@([ordered]@{type='UnityEngine.CapsuleCollider'})
            movementAgents=@([ordered]@{type='Kingmaker.View.UnitMovementAgent'});animatorControllers=@();animationClips=@('Idle','Walk','Run')
            animationActions=@('Idle|Synthetic');viewCorpulence=0.75;selectionRelatedComponents=@()
        }
        $horseRecord = [ordered]@{
            name='CR1_HorseRiding';assetGuid='9e9e75c484e68734487e609714565202';type='Kingmaker.Blueprints.BlueprintUnit'
            size='Large';sizeValue=5;prefabAssetId='5e0b93738ad54dd4ba101b3513ac4590';prefabResourceName='HorseRiding.prefab'
            strength=16;dexterity=14;constitution=15;intelligence=2;wisdom=12;charisma=6;speedFeet=50
            componentTypes=@('Kingmaker.UnitLogic.FactLogic.AddClassLevels');body=$horseBody;view=$horseView
        }
        $ponyRecord = [ordered]@{
            name='PonySummoned';assetGuid='3f95557fc806db741b500a5735990841';type='Kingmaker.Blueprints.BlueprintUnit'
            size='Medium';sizeValue=4;prefabAssetId='447d2907feec82545b3773fbb4709588';prefabResourceName='Pony_02'
            strength=13;dexterity=13;constitution=14;intelligence=2;wisdom=11;charisma=4;speedFeet=40
            componentTypes=@('Kingmaker.UnitLogic.FactLogic.AddClassLevels');body=$horseBody;view=$horseView
        }
        $kmcGuidByRole = [ordered]@{
            'horse-unit'='4016c7db400ab721ff125aef9e65e202'
            'horse-feature'='7db7c50677e39f09feef56f3831fc723'
            'horse-upgrade'='98e651899e6278d938de77af1d69bd32'
            'horse-portrait'='6874a165bf8bda3531ee4e2abc10c899'
            'mount-ability'='f053faad986631688defa003cd7bda0e'
            'dismount-ability'='3af2b81f4d72bbb30501fa730fcdf36e'
            'rider-primary-ability'='27364df661b3c121eabb97a31aa73a83'
            'mount-primary-ability'='f88a50d6fdbebbd709c3e323d2f52f5e'
        }
        $kmcRuntimeBlueprints = @($kmcGuidByRole.GetEnumerator() | ForEach-Object {
            [ordered]@{
                role=[string]$_.Key;assetGuid=[string]$_.Value;matchingGuidCount=1;exactReferenceCount=1
                foreignCollisionCount=0;exactSelfOwned=$true
                blueprint=[ordered]@{name=('KMC_'+$_.Key);assetGuid=[string]$_.Value;type='Kingmaker.Blueprints.BlueprintScriptableObject'}
            }
        })
        $reserved = @($kmcGuidByRole.Values | ForEach-Object {
            [ordered]@{assetGuid=[string]$_;resolved=$false;blueprint=$null}
        })
        $horseArtifact = [ordered]@{
            schemaVersion=3;evidenceKind='horse-asset-audit';runId=$horseRequest.runId;scenario=$horseRequest.scenario
            branch=$horseRequest.branch;commit=$horseRequest.commit;productVersion=$horseRequest.productVersion;createdAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
            loadedBlueprintCount=100;stockBlueprintCount=92;resourceNameCount=100;kmcRuntimeBlueprints=$kmcRuntimeBlueprints
            reservedGuidCollisions=$reserved;exactHorse=$horseRecord
            ponyDiscovery=[ordered]@{resourceMatches=@([ordered]@{assetId='pony';resourceName='Pony.prefab'});candidateUnits=@($horseRecord,$ponyRecord);ponyCandidateUnits=@($ponyRecord);reverseReferences=@();reverseReferenceTruncated=$false}
            portraitDiscovery=[ordered]@{blueprintPortraitCount=1;namedHorsePonyBlueprintPortraits=@();horsePonyUnitPortraitOwners=@();horsePonyIconOwners=@();exactNativeHorsePortrait=$null}
            stockCompanionBaseline=[ordered]@{feature=[ordered]@{};unit=[ordered]@{};upgrade=[ordered]@{};addPet=[ordered]@{}}
            companionSelections=@([ordered]@{assetGuid='selection'});ranger=[ordered]@{class='RangerClass'};paladin=[ordered]@{class='PaladinClass'}
            assertions=@([ordered]@{name='synthetic-contract';status='PASS';detail='Synthetic validator contract.'})
            assertionPassCount=1;assertionFailCount=0;errors=@();status='PASS'
        }
        $horsePath = Join-Path $horseRoot 'horse-native-asset-audit.json'
        Write-KmcJsonDurable -Path $horsePath -Value $horseArtifact
        $horseArtifactRecord = [ordered]@{
            relativePath='horse-native-asset-audit.json';kind='horse-asset-audit'
            length=(Get-Item -LiteralPath $horsePath).Length;sha256=(Get-KmcSha256 $horsePath)
        }
        [void](New-TestArtifactManifest -EvidenceRoot $horseRoot -RunId $horseRequest.runId -Scenario $horseRequest.scenario -Artifacts @($horseArtifactRecord))
        $horseManifest = Read-KmcJson (Join-Path $horseRoot 'runtime-artifacts.json')
        $horseSubresult = [pscustomobject]@{name='horse-native-asset-audit';status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()}
        Assert-KmcHorseNativeAssetAuditEvidence -Request $horseRequest -Manifest $horseManifest -Status PASS -SubscenarioResults @($horseSubresult)
    }

    Invoke-HarnessTest 'horse companion registration validator binds exact production snapshots and lease restoration' {
        $horseBlueprintSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Integration\HorseCompanionBlueprintService.cs')
        $horseRegistrationAuditSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\HorseCompanionBlueprintRegistrationAuditService.cs')
        Assert-Test ($horseBlueprintSource.Contains('DisableHands = true,') -and
            $horseBlueprintSource.Contains('AdditionalLimbs = new[] { bite, hoof, hoof },') -and
            -not $horseBlueprintSource.Contains('AdditionalLimbs = new[] { hoof, hoof },') -and
            $horseRegistrationAuditSource.Contains('body != null && body.DisableHands && body.EmptyHandWeapon == null') -and
            $horseRegistrationAuditSource.Contains('body.AdditionalLimbs != null && body.AdditionalLimbs.Length == 3')) `
            'horse companion does not retain the exact no-hands Bite/Hoof/Hoof topology that stock UnitAttack enumerates once each'
        $registrationRoot = Join-Path $runtimeEvidenceTestRoot 'horse-companion-registration-validator'
        New-Item -ItemType Directory -Path $registrationRoot -Force | Out-Null
        $registrationRequest = [pscustomobject]@{
            runId='horse-companion-registration-validator';scenario='horse-companion-blueprint-registration';branch='codex/mounted-combat-phase3-horse'
            commit=('2'*40);productVersion=$currentProductVersion;evidenceRoot=$registrationRoot
        }
        $initial = [ordered]@{
            state=1;failure=$null;unitGuid='4016c7db400ab721ff125aef9e65e202';featureGuid='7db7c50677e39f09feef56f3831fc723'
            upgradeGuid='98e651899e6278d938de77af1d69bd32';rangerSelectionGuid='ee63330662126374e8785cc901941ac7'
            rangerOriginalOptionCount=7;rangerCurrentOptionCount=8;rangerAppendOwned=$true;rangerSelectionDesired=$true
            nativeViewAssetId='5e0b93738ad54dd4ba101b3513ac4590';companionClassGuid=('3'*32)
            initialClassLevels=0;stockMammothInitialClassLevels=0;stockDogInitialClassLevels=0
            stockMammothAllowDyingConditionComponent=$true;stockDogAllowDyingConditionComponent=$true;horseAllowDyingConditionComponent=$true
            levelRankGuid='1670990255e4fe948a863bafd5dbda5d';upgradeLevel=4;biteGuid=('4'*32);biteName='Bite1d4'
            hoofGuid='b0e472a49ff2a294f93faa3ab757a4a5';hoofName='Hoof1d4';naturalAttackCount=3
            unitComponentCount=2;upgradeComponentCount=2;strength=16;dexterity=13;constitution=15
            intelligence=2;wisdom=12;charisma=6;speedFeet=50;size='Large'
        }
        $disabled = [ordered]@{}
        $reenabled = [ordered]@{}
        foreach ($pair in $initial.GetEnumerator()) { $disabled[$pair.Key]=$pair.Value; $reenabled[$pair.Key]=$pair.Value }
        $disabled.rangerCurrentOptionCount=7;$disabled.rangerAppendOwned=$false;$disabled.rangerSelectionDesired=$false
        $requiredAssertions = @(
            'registration-state','initialized-blueprint-library','exact-library-identities','add-pet-contract',
            'companion-class-contract','native-dying-condition-contract','native-view-size-speed','base-ability-scores','natural-attack-loadout',
            'rank-four-upgrade','localization-contract','ranger-append','exact-disable-restore','exact-reenable-append'
        )
        $assertions = @($requiredAssertions | ForEach-Object { [ordered]@{name=$_;status='PASS';detail="Synthetic exact contract for $_."} })
        $registrationArtifact = [ordered]@{
            schemaVersion=1;evidenceKind='horse-companion-blueprint-registration';runId=$registrationRequest.runId
            scenario=$registrationRequest.scenario;branch=$registrationRequest.branch;commit=$registrationRequest.commit
            productVersion=$registrationRequest.productVersion;createdAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
            initial=$initial;selectionDisabled=$disabled;selectionReenabled=$reenabled;assertions=$assertions
            assertionPassCount=$assertions.Count;assertionFailCount=0;errors=@();status='PASS'
        }
        $registrationPath = Join-Path $registrationRoot 'horse-companion-blueprint-registration.json'
        Write-KmcJsonDurable -Path $registrationPath -Value $registrationArtifact
        $registrationRecord = [ordered]@{
            relativePath='horse-companion-blueprint-registration.json';kind='horse-companion-blueprint-registration'
            length=(Get-Item -LiteralPath $registrationPath).Length;sha256=(Get-KmcSha256 $registrationPath)
        }
        [void](New-TestArtifactManifest -EvidenceRoot $registrationRoot -RunId $registrationRequest.runId -Scenario $registrationRequest.scenario -Artifacts @($registrationRecord))
        $registrationManifest = Read-KmcJson (Join-Path $registrationRoot 'runtime-artifacts.json')
        $registrationSubresult = [pscustomobject]@{
            name='horse-companion-blueprint-registration';status='PASS';assertionPassCount=$assertions.Count;assertionFailCount=0;errors=@()
        }
        Assert-KmcHorseCompanionBlueprintRegistrationEvidence -Request $registrationRequest -Manifest $registrationManifest -Status PASS -SubscenarioResults @($registrationSubresult)

        $registrationArtifact.selectionDisabled.rangerCurrentOptionCount = 8
        Write-KmcJsonAtomic -Path $registrationPath -Value $registrationArtifact
        $registrationRecord.length=(Get-Item -LiteralPath $registrationPath).Length
        $registrationRecord.sha256=(Get-KmcSha256 $registrationPath)
        [void](New-TestArtifactManifest -EvidenceRoot $registrationRoot -RunId $registrationRequest.runId -Scenario $registrationRequest.scenario -Artifacts @($registrationRecord))
        $mutatedManifest = Read-KmcJson (Join-Path $registrationRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcHorseCompanionBlueprintRegistrationEvidence -Request $registrationRequest -Manifest $mutatedManifest -Status PASS -SubscenarioResults @($registrationSubresult) }
        catch { $threw = $true }
        Assert-Test $threw 'horse companion registration validator accepted a false seven-option restore snapshot'
    }

    Invoke-HarnessTest 'horse companion unmounted validator binds runtime behavior and exact cleanup' {
        $unmountedEngineSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src\KingmakerMountedCombat\Diagnostics\HorseCompanionUnmountedScenarioEngine.cs')
        Assert-Test ($unmountedEngineSource.Contains('private const float TargetDistance = 4.0f;') -and
            $unmountedEngineSource.Contains('FindWalkablePoint(owner.Position, TargetDistance, 0.45f)') -and
            -not $unmountedEngineSource.Contains('FindWalkablePoint(horse.Position, TargetDistance, 0.45f)')) `
            'horse companion combat target is not derived from the exact owner-relative placement authority'
        Assert-Test ($unmountedEngineSource.Contains('private const double LifecycleTimeoutSeconds = 60.0;') -and
            $unmountedEngineSource.Contains('var lifecyclePhase = step == EngineStep.AwaitMountedLifecycleTargetRemoval ||') -and
            $unmountedEngineSource.Contains('step == EngineStep.AwaitLifecycleCombatEntry ||') -and
            $unmountedEngineSource.Contains('step == EngineStep.AwaitDeath ||') -and
            $unmountedEngineSource.Contains('step == EngineStep.AwaitDirectDamage ||') -and
            $unmountedEngineSource.Contains('HorseCompanionScenarioDeadlinePolicy.Evaluate(') -and
            $unmountedEngineSource.Contains('lifecycleStartedAtSeconds = clock.Elapsed.TotalSeconds;') -and
            $unmountedEngineSource.IndexOf('lifecycleStartedAtSeconds = clock.Elapsed.TotalSeconds;', [StringComparison]::Ordinal) -lt
                $unmountedEngineSource.IndexOf('step = EngineStep.AwaitDeath;', [StringComparison]::Ordinal) -and
            $unmountedEngineSource.Contains('lifecycleExpired ? "lifecycle-deadline" : "bounded-deadline"')) `
            'horse companion lifecycle verification does not have an independent exact bounded deadline'
        Assert-Test ($unmountedEngineSource.Contains('private const double RealTimeAttackTimeoutSeconds = 20.0;') -and
            $unmountedEngineSource.Contains('observations["realTimeAttackAtDispatch"] = CaptureRealTimeAttackState(realTimeAttack);') -and
            $unmountedEngineSource.Contains('observations["realTimeAttackAtDeadline"] = diagnostic;') -and
            $unmountedEngineSource.Contains('observations["realTimePreDispatchStandardType"] = preDispatchStandard?.GetType().FullName;') -and
            $unmountedEngineSource.Contains('horse.Commands.InterruptAll(false);') -and
            $unmountedEngineSource.Contains('expectedDispatchStarted && ReferenceEquals(horse.Commands.Standard, realTimeAttack)') -and
            $unmountedEngineSource.Contains('ReferenceEquals(horse.Commands.Standard, command)') -and
            $unmountedEngineSource.Contains('observations["realTimeForcedD20Count"] = ruleProbe.ForcedD20Count;') -and
            $unmountedEngineSource.Contains('observations["realTimeUnexpectedPairAttackCount"] = ruleProbe.UnexpectedPairAttackCount;') -and
            $unmountedEngineSource.Contains('observations["turnBasedForcedD20Count"] = ruleProbe.ForcedD20Count;') -and
            $unmountedEngineSource.Contains('observations["turnBasedUnexpectedPairAttackCount"] = ruleProbe.UnexpectedPairAttackCount;') -and
            $unmountedEngineSource.Contains('ruleProbe.DamageRuleCount == 1 && ruleProbe.ForcedD20Count >= 1 &&')) `
            'horse companion RT attack leaf lost its bounded native-merge repair or exact command-identity diagnostic'
        Assert-Test ($unmountedEngineSource.Contains('private const double TurnBasedTurnAcquisitionTimeoutSeconds = 20.0;') -and
            $unmountedEngineSource.Contains('private const double TurnBasedAttackTimeoutSeconds = 20.0;') -and
            $unmountedEngineSource.Contains('private const double MountedAlphaAdmissionTimeoutSeconds = 20.0;') -and
            $unmountedEngineSource.Contains('step = EngineStep.AwaitMountedAlphaAdmission;') -and
            $unmountedEngineSource.Contains('private void AwaitMountedAlphaAdmission()') -and
            $unmountedEngineSource.Contains('var availability = playerAction.GetAvailability();') -and
            $unmountedEngineSource.Contains('availability.Action == MountedPlayerActionKind.Mount') -and
            $unmountedEngineSource.Contains('"target-selected-mount-admission-deadline"') -and
            ([regex]::Matches($unmountedEngineSource, [regex]::Escape('if (Game.Instance.IsPaused) { Game.Instance.IsPaused = false; }'))).Count -eq 5 -and
            ([regex]::Matches($unmountedEngineSource, [regex]::Escape('horse.Commands.InterruptAll(false);'))).Count -ge 2 -and
            $unmountedEngineSource.Contains('var nativeTurn = controller.CurrentTurn;') -and
            $unmountedEngineSource.Contains('turnBasedNativeTurnStableFrames++') -and
            $unmountedEngineSource.Contains('if (turnBasedNativeTurnStableFrames < 2)') -and
            $unmountedEngineSource.Contains('mountedNativeTurnStableFrames++') -and
            $unmountedEngineSource.Contains('if (mountedNativeTurnStableFrames < 2)') -and
            $unmountedEngineSource.Contains('if (turnBasedStartTurnRequestCount < 2)') -and
            $unmountedEngineSource.Contains('turnBasedStableReadyFrames++') -and
            $unmountedEngineSource.Contains('if (turnBasedStableReadyFrames < 2)') -and
            $unmountedEngineSource.Contains('game.IsPaused || horse.Commands == null || !horse.Commands.Empty ||') -and
            $unmountedEngineSource.Contains('game.HandsEquipmentController.IsUpdateScheduledFor(horse) || !horse.HasStandardAction() ||') -and
            $unmountedEngineSource.Contains('ReferenceEquals(controller.CurrentTurn, turn) &&') -and
            $unmountedEngineSource.Contains('ReferenceEquals(horse.Commands.Standard, turnBasedAttack)') -and
            -not $unmountedEngineSource.Contains('exactHealthyPendingCommand') -and
            -not $unmountedEngineSource.Contains('mountedPostMoveTurnReassertions == 0') -and
            ([regex]::Matches($unmountedEngineSource, [regex]::Escape('controller.StartTurn(horse);'))).Count -eq 4 -and
            $unmountedEngineSource.Contains('controller.StartTurn(horse);') -and
            $unmountedEngineSource.Contains('turnBasedAttack.Result == UnitCommand.ResultType.Success') -and
            $unmountedEngineSource.Contains('observations["turnBasedAttackAtDeadline"] = CaptureTurnBasedAttackState(turnBasedAttack);') -and
            $unmountedEngineSource.Contains('observations["turnBasedAttackAtTerminal"] = terminal;')) `
            'horse companion TB attack leaf lost exact stock readiness, Standard-slot admission, success, or bounded diagnostics'
        $unmountedRoot = Join-Path $runtimeEvidenceTestRoot 'horse-companion-unmounted-validator'
        New-Item -ItemType Directory -Path $unmountedRoot -Force | Out-Null
        $unmountedRequest = [pscustomobject]@{
            runId='horse-companion-unmounted-validator';scenario='horse-companion-unmounted-suite';branch='codex/mounted-combat-phase3-horse'
            commit=('5'*40);productVersion=$currentProductVersion;dllSha256=('6'*64)
            dllMvid='11111111-2222-3333-4444-555555555555';evidenceRoot=$unmountedRoot
        }
        $required = @(
            'eligible-owner','native-ranger-level-up-commit','feature-activation','creation-and-ownership','party-control-surface',
            'rank-progression-and-upgrade','native-view-size-statistics','horse-selection','stock-movement-command',
            'unmounted-party-movement','transient-combat-target','bite-and-hoof-full-attack','expected-attack-boundary',
            'real-time-natural-attack','turn-based-roster','turn-based-horse-control','turn-based-natural-attack',
            'stock-lifecycle-admission','ordinary-stock-damage-lifecycle','death-ownership','death-and-recovery',
            'direct-damage-control-disposition','direct-damage-control-recovery',
            'respec-runtime-cleanup','respec-and-uninstall-surface',
            'entity-and-target-restoration','mode-pause-selection-restoration','non-horse-isolation'
        )
        $unmountedAssertions = @($required | ForEach-Object { [ordered]@{name=$_;status='PASS';detail="Synthetic exact contract for $_."} })
        $biteGuid = ('7'*32)
        $newHorseLifeSnapshot = {
            param([string]$LifeState,[bool]$Conscious,[bool]$Dead,[int]$Damage,[bool]$InAwakeUnits)
            [ordered]@{
                lifeState=$LifeState;isConscious=$Conscious;isDead=$Dead;stateIsDead=$Dead;isFinallyDead=$false
                damage=$Damage;nonLethalDamage=0;hitPoints=40;temporaryHitPoints=0;constitution=19;negativeHitPointThreshold=59
                allowDyingCondition=$true;masterAllowDyingCondition=$true;immortality=$false;regeneration=$false
                ferocity=$false;halfOrcFerocity=$false;dualCompanionPartPresent=$false;dualCompanionPartDead=$false
                dualCompanionPairId=$null;isInState=$true;inStateUnits=$true;inAwakeUnits=$InAwakeUnits
                isAwake=$InAwakeUnits;isSleeping=(-not $InAwakeUnits);awakeTimer=$(if($InAwakeUnits){1.0}else{-1.0})
                sleepless=$false;viewPresent=$true;viewActive=$true;animatorPresent=$true;animatorLayerCount=1
                animatorStateFullPathHash=1;animatorStateShortNameHash=2;animatorStateNormalizedTime=0.5
                animatorInTransition=$false;ownerPetExact=$true;masterExact=$true;ownerPetId='horse';masterId='owner'
                controllableRosterContainsHorse=$true;controllableRosterCount=2;groupIsPlayerParty=$true
            }
        }
        $lifeConscious = & $newHorseLifeSnapshot 'Conscious' $true $false 0 $true
        $lifeDead = & $newHorseLifeSnapshot 'Dead' $false $true 60 $true
        $lifeDirectImmediate = & $newHorseLifeSnapshot 'Conscious' $true $false 60 $true
        $lifeDirectAfter = & $newHorseLifeSnapshot 'Conscious' $true $false 60 $false
        $stockLifecycleAttacks = @(
            [ordered]@{sequence=1;result='Success';attackRules=1;attackRolls=1;damageRules=1;forcedD20Count=1;damage=30;horseDamageAfter=30;horseLifeStateAfter='Conscious'},
            [ordered]@{sequence=2;result='Success';attackRules=1;attackRolls=1;damageRules=1;forcedD20Count=1;damage=30;horseDamageAfter=60;horseLifeStateAfter='Dead'}
        )
        $unmountedObservations = [ordered]@{
            originalPause=$false;originalTurnBased=$false;originalSelectionCount=1
            saveLoadAutomationScope='CONTRACT-ONLY: synthetic guarded boundary.';ownerId='owner';ownerBlueprintGuid=('8'*32)
            nativeRangerCommitCount=4;huntersBondSelectionLevel=4;rangerCompanionSelectionLevel=4
            horseFactRankAtCommit=1;horsePresentAtNativeCommit=$true;horseFeatureSourceGuid=('3'*32)
            horseId='horse';horseBlueprintGuid='4016c7db400ab721ff125aef9e65e202';characterLevel=1;expectedCharacterLevel=2
            experience=9000;expectedExperience=9000;rank=1;upgradeRank=0
            activationDefaultBuildContextPresent=$false;activationCharacterLevelAfterNativeTry=1;activationExperienceAfterNativeTry=9000
            deferredNativeAttempts=0;defaultBuildContextWaitFrames=0;lastDeferredDefaultBuildContextPresent=$false
            deferredCharacterLevelBefore=1;deferredCharacterLevelAfter=1;deferredExperienceBefore=9000;deferredExperienceAfter=9000
            nativeClassProgressionSynchronized=$false;nativeManualLevelingReady=$true
            nativeProgressionDisposition='native-manual-leveling-ready';deferredProgressionSynchronized=$true
            runtimeSize='Large';speedFeet=50;hitPoints=40;armorClass=18;movementDisplacement=1.8
            movementRemainingDistance=0.1;ownerDisplacementDuringHorseMove=0.0;targetOwnerDistance=4.0;targetHorseDistance=2.0
            fullAttackWeaponGuids=@($biteGuid,'b0e472a49ff2a294f93faa3ab757a4a5','b0e472a49ff2a294f93faa3ab757a4a5')
            realTimePreDispatchStandardType='Kingmaker.UnitLogic.Commands.UnitAttack';realTimePreDispatchStandardRunning=$true
            realTimePreDispatchStandardAiActionPresent=$true;realTimePreDispatchStandardTargetExact=$true
            realTimeAttackAtDispatch=[ordered]@{plannedWeaponGuid=$biteGuid;commandReferenceInStandardSlot=$true;commandContained=$true;commandCanStart=$true}
            realTimeAttackWeaponGuid=$biteGuid;realTimeAttackRules=1;realTimeAttackRolls=1;realTimeDamageRules=1
            realTimeForcedD20Count=4;realTimeUnexpectedPairAttackCount=0;realTimeDamage=8
            turnBasedAttackWeaponGuid=$biteGuid;turnBasedAttackRules=1;turnBasedAttackRolls=1;turnBasedDamageRules=1
            turnBasedForcedD20Count=4;turnBasedUnexpectedPairAttackCount=0;turnBasedDamage=7
            turnBasedPostDispatchStartTurnRequestCount=0
            targetCleanupExact=$true;lethalDamage=60;recoveredDamage=0;finalPause=$false;finalTurnBased=$false;finalSelectionCount=1
            unrelatedPartyPetsPreserved=$true;relationshipState='Unmounted';horseRemoved=$true;targetRemoved=$true
            stockLifecycleBefore=$lifeConscious;stockLifecycleAttacks=$stockLifecycleAttacks;stockLifecycleAttackCount=2
            maximumStockLifecycleAttacks=40
            stockLifecycleAttackRules=2;stockLifecycleAttackRolls=2;stockLifecycleDamageRules=2
            stockLifecycleForcedD20Count=2;stockLifecycleRuleDamage=60;stockLifecycleTransitionEventCount=1
            stockLifecycleTransitionActorId='horse';stockLifecycleTransitionPreviousLifeState='Conscious'
            stockLifecycleTransitionCurrentLifeState='Dead';stockLifecycleAfter=$lifeDead;stockLifecycleRecovery=$lifeConscious
            directDamageBefore=$lifeConscious;directDamageImmediatelyAfterMutation=$lifeDirectImmediate
            directDamageDisposition='direct-mutation-left-native-awake-schedule-without-life-event'
            directDamageTransitionEventCount=0;directDamageAfterObservation=$lifeDirectAfter
            directDamageTimeline=@([ordered]@{secondsSinceMutation=1.0;lifeState='Conscious';damage=60;inAwakeUnits=$false;isAwake=$false;isSleeping=$true;awakeTimer=-1.0})
            directDamageRecovery=$lifeConscious
        }
        $unmountedArtifact = [ordered]@{
            schemaVersion=4;evidenceKind='horse-companion-unmounted';runId=$unmountedRequest.runId;scenario=$unmountedRequest.scenario
            branch=$unmountedRequest.branch;commit=$unmountedRequest.commit;productVersion=$unmountedRequest.productVersion
            dllSha256=$unmountedRequest.dllSha256;dllMvid=$unmountedRequest.dllMvid;createdAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
            status='PASS';assertions=$unmountedAssertions;observations=$unmountedObservations
            assertionPassCount=$unmountedAssertions.Count;assertionFailCount=0;errors=@()
        }
        $unmountedPath = Join-Path $unmountedRoot 'horse-companion-unmounted.json'
        Write-KmcJsonDurable -Path $unmountedPath -Value $unmountedArtifact
        $unmountedRecord = [ordered]@{
            relativePath='horse-companion-unmounted.json';kind='horse-companion-unmounted'
            length=(Get-Item -LiteralPath $unmountedPath).Length;sha256=(Get-KmcSha256 $unmountedPath)
        }
        [void](New-TestArtifactManifest -EvidenceRoot $unmountedRoot -RunId $unmountedRequest.runId -Scenario $unmountedRequest.scenario -Artifacts @($unmountedRecord))
        $unmountedManifest = Read-KmcJson (Join-Path $unmountedRoot 'runtime-artifacts.json')
        $unmountedSubresult = [pscustomobject]@{
            name='horse-companion-unmounted-suite';status='PASS';assertionPassCount=$unmountedAssertions.Count;assertionFailCount=0;errors=@()
        }
        Assert-KmcHorseCompanionUnmountedEvidence -Request $unmountedRequest -Manifest $unmountedManifest -Status PASS -SubscenarioResults @($unmountedSubresult)

        $unmountedArtifact.observations.maximumStockLifecycleAttacks = 39
        Write-KmcJsonAtomic -Path $unmountedPath -Value $unmountedArtifact
        $unmountedRecord.length=(Get-Item -LiteralPath $unmountedPath).Length
        $unmountedRecord.sha256=(Get-KmcSha256 $unmountedPath)
        [void](New-TestArtifactManifest -EvidenceRoot $unmountedRoot -RunId $unmountedRequest.runId -Scenario $unmountedRequest.scenario -Artifacts @($unmountedRecord))
        $mutatedUnmountedManifest = Read-KmcJson (Join-Path $unmountedRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcHorseCompanionUnmountedEvidence -Request $unmountedRequest -Manifest $mutatedUnmountedManifest -Status PASS -SubscenarioResults @($unmountedSubresult) }
        catch { $threw = $true }
        Assert-Test $threw 'horse companion unmounted validator accepted an inexact stock lifecycle attack budget'

        $unmountedArtifact.observations.maximumStockLifecycleAttacks = 40
        $unmountedArtifact.observations.realTimePreDispatchStandardTargetExact = $false
        Write-KmcJsonAtomic -Path $unmountedPath -Value $unmountedArtifact
        $unmountedRecord.length=(Get-Item -LiteralPath $unmountedPath).Length
        $unmountedRecord.sha256=(Get-KmcSha256 $unmountedPath)
        [void](New-TestArtifactManifest -EvidenceRoot $unmountedRoot -RunId $unmountedRequest.runId -Scenario $unmountedRequest.scenario -Artifacts @($unmountedRecord))
        $mutatedUnmountedManifest = Read-KmcJson (Join-Path $unmountedRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcHorseCompanionUnmountedEvidence -Request $unmountedRequest -Manifest $mutatedUnmountedManifest -Status PASS -SubscenarioResults @($unmountedSubresult) }
        catch { $threw = $true }
        Assert-Test $threw 'horse companion unmounted validator accepted an inexact pre-dispatch target identity'

        $unmountedArtifact.observations.realTimePreDispatchStandardTargetExact = $true
        $unmountedArtifact.observations.experience = 8999
        Write-KmcJsonAtomic -Path $unmountedPath -Value $unmountedArtifact
        $unmountedRecord.length=(Get-Item -LiteralPath $unmountedPath).Length
        $unmountedRecord.sha256=(Get-KmcSha256 $unmountedPath)
        [void](New-TestArtifactManifest -EvidenceRoot $unmountedRoot -RunId $unmountedRequest.runId -Scenario $unmountedRequest.scenario -Artifacts @($unmountedRecord))
        $mutatedUnmountedManifest = Read-KmcJson (Join-Path $unmountedRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcHorseCompanionUnmountedEvidence -Request $unmountedRequest -Manifest $mutatedUnmountedManifest -Status PASS -SubscenarioResults @($unmountedSubresult) }
        catch { $threw = $true }
        Assert-Test $threw 'horse companion unmounted validator accepted an inexact native manual-leveling XP handoff'

        $unmountedArtifact.observations.experience = 9000
        $unmountedArtifact.observations.targetOwnerDistance = 2.99
        Write-KmcJsonAtomic -Path $unmountedPath -Value $unmountedArtifact
        $unmountedRecord.length=(Get-Item -LiteralPath $unmountedPath).Length
        $unmountedRecord.sha256=(Get-KmcSha256 $unmountedPath)
        [void](New-TestArtifactManifest -EvidenceRoot $unmountedRoot -RunId $unmountedRequest.runId -Scenario $unmountedRequest.scenario -Artifacts @($unmountedRecord))
        $mutatedUnmountedManifest = Read-KmcJson (Join-Path $unmountedRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcHorseCompanionUnmountedEvidence -Request $unmountedRequest -Manifest $mutatedUnmountedManifest -Status PASS -SubscenarioResults @($unmountedSubresult) }
        catch { $threw = $true }
        Assert-Test $threw 'horse companion unmounted validator accepted an owner-relative target below the diagnostic floor'

        $unmountedArtifact.observations.targetOwnerDistance = 4.0
        $unmountedArtifact.observations.realTimeForcedD20Count = 0
        Write-KmcJsonAtomic -Path $unmountedPath -Value $unmountedArtifact
        $unmountedRecord.length=(Get-Item -LiteralPath $unmountedPath).Length
        $unmountedRecord.sha256=(Get-KmcSha256 $unmountedPath)
        [void](New-TestArtifactManifest -EvidenceRoot $unmountedRoot -RunId $unmountedRequest.runId -Scenario $unmountedRequest.scenario -Artifacts @($unmountedRecord))
        $mutatedUnmountedManifest = Read-KmcJson (Join-Path $unmountedRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcHorseCompanionUnmountedEvidence -Request $unmountedRequest -Manifest $mutatedUnmountedManifest -Status PASS -SubscenarioResults @($unmountedSubresult) }
        catch { $threw = $true }
        Assert-Test $threw 'horse companion unmounted validator accepted zero forced D20 observations'

        $unmountedArtifact.observations.realTimeForcedD20Count = 4
        $unmountedArtifact.observations.turnBasedUnexpectedPairAttackCount = 1
        Write-KmcJsonAtomic -Path $unmountedPath -Value $unmountedArtifact
        $unmountedRecord.length=(Get-Item -LiteralPath $unmountedPath).Length
        $unmountedRecord.sha256=(Get-KmcSha256 $unmountedPath)
        [void](New-TestArtifactManifest -EvidenceRoot $unmountedRoot -RunId $unmountedRequest.runId -Scenario $unmountedRequest.scenario -Artifacts @($unmountedRecord))
        $mutatedUnmountedManifest = Read-KmcJson (Join-Path $unmountedRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcHorseCompanionUnmountedEvidence -Request $unmountedRequest -Manifest $mutatedUnmountedManifest -Status PASS -SubscenarioResults @($unmountedSubresult) }
        catch { $threw = $true }
        Assert-Test $threw 'horse companion unmounted validator accepted a duplicate pair attack observation'

        $unmountedArtifact.observations.turnBasedUnexpectedPairAttackCount = 0
        $unmountedArtifact.observations.turnBasedPostDispatchStartTurnRequestCount = 1
        Write-KmcJsonAtomic -Path $unmountedPath -Value $unmountedArtifact
        $unmountedRecord.length=(Get-Item -LiteralPath $unmountedPath).Length
        $unmountedRecord.sha256=(Get-KmcSha256 $unmountedPath)
        [void](New-TestArtifactManifest -EvidenceRoot $unmountedRoot -RunId $unmountedRequest.runId -Scenario $unmountedRequest.scenario -Artifacts @($unmountedRecord))
        $mutatedUnmountedManifest = Read-KmcJson (Join-Path $unmountedRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcHorseCompanionUnmountedEvidence -Request $unmountedRequest -Manifest $mutatedUnmountedManifest -Status PASS -SubscenarioResults @($unmountedSubresult) }
        catch { $threw = $true }
        Assert-Test $threw 'horse companion unmounted validator accepted a post-dispatch turn restart'

        $unmountedArtifact.observations.turnBasedPostDispatchStartTurnRequestCount = 0
        $unmountedArtifact.observations.horseRemoved = $false
        Write-KmcJsonAtomic -Path $unmountedPath -Value $unmountedArtifact
        $unmountedRecord.length=(Get-Item -LiteralPath $unmountedPath).Length
        $unmountedRecord.sha256=(Get-KmcSha256 $unmountedPath)
        [void](New-TestArtifactManifest -EvidenceRoot $unmountedRoot -RunId $unmountedRequest.runId -Scenario $unmountedRequest.scenario -Artifacts @($unmountedRecord))
        $mutatedUnmountedManifest = Read-KmcJson (Join-Path $unmountedRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcHorseCompanionUnmountedEvidence -Request $unmountedRequest -Manifest $mutatedUnmountedManifest -Status PASS -SubscenarioResults @($unmountedSubresult) }
        catch { $threw = $true }
        Assert-Test $threw 'horse companion unmounted validator accepted residual horse state'
    }

    Invoke-HarnessTest 'horse mounted alpha validator binds target action profile routing attacks and cleanup' {
        $mountedRoot = Join-Path $runtimeEvidenceTestRoot 'horse-mounted-alpha-validator'
        New-Item -ItemType Directory -Path $mountedRoot -Force | Out-Null
        $mountedRequest = [pscustomobject]@{
            runId='horse-mounted-alpha-validator';scenario='horse-mounted-alpha-suite';branch='codex/mounted-combat-phase3-horse'
            commit=('9'*40);productVersion=$currentProductVersion;dllSha256=('a'*64)
            dllMvid='22222222-3333-4444-5555-666666666666';evidenceRoot=$mountedRoot
        }
        $baseRequired = @(
            'eligible-owner','native-ranger-level-up-commit','feature-activation','creation-and-ownership','party-control-surface',
            'rank-progression-and-upgrade','native-view-size-statistics','horse-selection','stock-movement-command',
            'unmounted-party-movement','transient-combat-target','bite-and-hoof-full-attack','expected-attack-boundary',
            'real-time-natural-attack','turn-based-roster','turn-based-horse-control','turn-based-natural-attack',
            'stock-lifecycle-admission','ordinary-stock-damage-lifecycle','death-ownership','death-and-recovery',
            'direct-damage-control-disposition','direct-damage-control-recovery',
            'respec-runtime-cleanup','respec-and-uninstall-surface',
            'entity-and-target-restoration','mode-pause-selection-restoration','non-horse-isolation'
        )
        $mountedRequired = @($baseRequired + @(
            'target-selected-mount-action','independent-horse-mounted-profile','horse-pose-calibration',
            'mounted-real-time-command-routing','mounted-real-time-movement',
            'mounted-transient-combat-target','horse-pair-retained-in-turn-based-transition',
            'mounted-rider-turn-ground-admission','mounted-turn-based-rider-movement',
            'mounted-rider-primary-admission','mounted-rider-primary-outcome',
            'mounted-horse-primary-admission','mounted-horse-primary-outcome',
            'mounted-explicit-dismount-dispatch','mounted-explicit-dismount-restoration'
        ))
        $mountedAssertions = @($mountedRequired | ForEach-Object { [ordered]@{name=$_;status='PASS';detail="Synthetic exact mounted contract for $_."} })
        $biteGuid = ('7'*32)
        $newHorseLifeSnapshot = {
            param([string]$LifeState,[bool]$Conscious,[bool]$Dead,[int]$Damage,[bool]$InAwakeUnits)
            [ordered]@{
                lifeState=$LifeState;isConscious=$Conscious;isDead=$Dead;stateIsDead=$Dead;isFinallyDead=$false
                damage=$Damage;nonLethalDamage=0;hitPoints=40;temporaryHitPoints=0;constitution=19;negativeHitPointThreshold=59
                allowDyingCondition=$true;masterAllowDyingCondition=$true;immortality=$false;regeneration=$false
                ferocity=$false;halfOrcFerocity=$false;dualCompanionPartPresent=$false;dualCompanionPartDead=$false
                dualCompanionPairId=$null;isInState=$true;inStateUnits=$true;inAwakeUnits=$InAwakeUnits
                isAwake=$InAwakeUnits;isSleeping=(-not $InAwakeUnits);awakeTimer=$(if($InAwakeUnits){1.0}else{-1.0})
                sleepless=$false;viewPresent=$true;viewActive=$true;animatorPresent=$true;animatorLayerCount=1
                animatorStateFullPathHash=1;animatorStateShortNameHash=2;animatorStateNormalizedTime=0.5
                animatorInTransition=$false;ownerPetExact=$true;masterExact=$true;ownerPetId='horse';masterId='owner'
                controllableRosterContainsHorse=$true;controllableRosterCount=2;groupIsPlayerParty=$true
            }
        }
        $lifeConscious = & $newHorseLifeSnapshot 'Conscious' $true $false 0 $true
        $lifeDead = & $newHorseLifeSnapshot 'Dead' $false $true 60 $true
        $lifeDirectImmediate = & $newHorseLifeSnapshot 'Conscious' $true $false 60 $true
        $lifeDirectAfter = & $newHorseLifeSnapshot 'Conscious' $true $false 60 $false
        $stockLifecycleAttacks = @(
            [ordered]@{sequence=1;result='Success';attackRules=1;attackRolls=1;damageRules=1;forcedD20Count=1;damage=30;horseDamageAfter=30;horseLifeStateAfter='Conscious'},
            [ordered]@{sequence=2;result='Success';attackRules=1;attackRolls=1;damageRules=1;forcedD20Count=1;damage=30;horseDamageAfter=60;horseLifeStateAfter='Dead'}
        )
        $mountedObservations = [ordered]@{
            originalPause=$false;originalTurnBased=$false;originalSelectionCount=1
            saveLoadAutomationScope='CONTRACT-ONLY: synthetic guarded boundary.';ownerId='owner';ownerBlueprintGuid=('8'*32)
            nativeRangerCommitCount=4;huntersBondSelectionLevel=4;rangerCompanionSelectionLevel=4
            horseFactRankAtCommit=1;horsePresentAtNativeCommit=$true;horseFeatureSourceGuid=('3'*32)
            horseId='horse';horseBlueprintGuid='4016c7db400ab721ff125aef9e65e202';characterLevel=1;expectedCharacterLevel=2
            experience=9000;expectedExperience=9000;rank=1;upgradeRank=0
            activationDefaultBuildContextPresent=$false;activationCharacterLevelAfterNativeTry=1;activationExperienceAfterNativeTry=9000
            deferredNativeAttempts=0;defaultBuildContextWaitFrames=0;lastDeferredDefaultBuildContextPresent=$false
            deferredCharacterLevelBefore=1;deferredCharacterLevelAfter=1;deferredExperienceBefore=9000;deferredExperienceAfter=9000
            nativeClassProgressionSynchronized=$false;nativeManualLevelingReady=$true
            nativeProgressionDisposition='native-manual-leveling-ready';deferredProgressionSynchronized=$true
            runtimeSize='Large';speedFeet=50;hitPoints=40;armorClass=18;movementDisplacement=1.8
            movementRemainingDistance=0.1;ownerDisplacementDuringHorseMove=0.0;targetOwnerDistance=4.0;targetHorseDistance=2.0
            fullAttackWeaponGuids=@($biteGuid,'b0e472a49ff2a294f93faa3ab757a4a5','b0e472a49ff2a294f93faa3ab757a4a5')
            realTimePreDispatchStandardType='Kingmaker.UnitLogic.Commands.UnitAttack';realTimePreDispatchStandardRunning=$true
            realTimePreDispatchStandardAiActionPresent=$true;realTimePreDispatchStandardTargetExact=$true
            realTimeAttackAtDispatch=[ordered]@{plannedWeaponGuid=$biteGuid;commandReferenceInStandardSlot=$true;commandContained=$true;commandCanStart=$true}
            realTimeAttackWeaponGuid=$biteGuid;realTimeAttackRules=1;realTimeAttackRolls=1;realTimeDamageRules=1
            realTimeForcedD20Count=4;realTimeUnexpectedPairAttackCount=0;realTimeDamage=8
            turnBasedAttackWeaponGuid=$biteGuid;turnBasedAttackRules=1;turnBasedAttackRolls=1;turnBasedDamageRules=1
            turnBasedForcedD20Count=4;turnBasedUnexpectedPairAttackCount=0;turnBasedDamage=7
            turnBasedPostDispatchStartTurnRequestCount=0
            targetCleanupExact=$true;lethalDamage=60;recoveredDamage=0;finalPause=$false;finalTurnBased=$false;finalSelectionCount=1
            unrelatedPartyPetsPreserved=$true;relationshipState='Unmounted';horseRemoved=$true;targetRemoved=$true
            stockLifecycleBefore=$lifeConscious;stockLifecycleAttacks=$stockLifecycleAttacks;stockLifecycleAttackCount=2
            maximumStockLifecycleAttacks=40
            stockLifecycleAttackRules=2;stockLifecycleAttackRolls=2;stockLifecycleDamageRules=2
            stockLifecycleForcedD20Count=2;stockLifecycleRuleDamage=60;stockLifecycleTransitionEventCount=1
            stockLifecycleTransitionActorId='horse';stockLifecycleTransitionPreviousLifeState='Conscious'
            stockLifecycleTransitionCurrentLifeState='Dead';stockLifecycleAfter=$lifeDead;stockLifecycleRecovery=$lifeConscious
            directDamageBefore=$lifeConscious;directDamageImmediatelyAfterMutation=$lifeDirectImmediate
            directDamageDisposition='direct-mutation-left-native-awake-schedule-without-life-event'
            directDamageTransitionEventCount=0;directDamageAfterObservation=$lifeDirectAfter
            directDamageTimeline=@([ordered]@{secondsSinceMutation=1.0;lifeState='Conscious';damage=60;inAwakeUnits=$false;isAwake=$false;isSleeping=$true;awakeTimer=-1.0})
            directDamageRecovery=$lifeConscious
        }
        $mountedObservations.unmountedTargetCleanupExact = $true
        $mountedObservations.mountTargetArmDelta = 1
        $mountedObservations.mountTargetClickDelta = 1
        $mountedObservations.mountTargetFeedback = 'Mounted exact Horse.'
        $mountedObservations.horseProfileId = 'medium-humanoid-horse-v1'
        $mountedObservations.horsePoseProfileId = 'medium-humanoid-horse-v1'
        $mountedObservations.horseSourceAnchor = 'Chest'
        $mountedObservations.horsePresentationAtMount = 'poseLease=True;attachmentLease=True'
        $mountedObservations.horsePoseCalibration = [ordered]@{
            candidateCount=3;candidateId='horse-human-review-20260829-c'
            dev23PelvisPositionOffset=[ordered]@{x=0.0;y=0.02;z=-0.02}
            selectedPelvisPositionOffset=[ordered]@{x=0.0;y=-0.17;z=-0.02}
            dev23LeftFootTargetFromThigh=[ordered]@{x=-0.305;y=-0.46;z=0.044}
            selectedLeftFootTargetFromThigh=[ordered]@{x=-0.15;y=-0.62;z=0.11}
            dev23RightFootTargetFromThigh=[ordered]@{x=0.305;y=-0.46;z=0.044}
            selectedRightFootTargetFromThigh=[ordered]@{x=0.15;y=-0.62;z=0.11}
            dev23LeftKneeHintFromThigh=[ordered]@{x=-0.38;y=-0.12;z=0.26}
            selectedLeftKneeHintFromThigh=[ordered]@{x=-0.16;y=-0.16;z=0.16}
            dev23RightKneeHintFromThigh=[ordered]@{x=0.38;y=-0.12;z=0.26}
            selectedRightKneeHintFromThigh=[ordered]@{x=0.16;y=-0.16;z=0.16}
            crossedStirrupAssignment=$false;pelvisFromChestMountLocal=[ordered]@{x=0;y=0.2;z=0}
            leftFootFromAssignedStirrupMountLocal=[ordered]@{x=0.1;y=0.1;z=0.1}
            rightFootFromAssignedStirrupMountLocal=[ordered]@{x=-0.1;y=0.1;z=0.1}
            leftFootToAssignedStirrup=0.2;rightFootToAssignedStirrup=0.2
            poseApplicationFrameCount=4;footTargetClampCount=0;maximumFootTargetResidualWorldUnits=0.001
            maximumKneeTargetResidualWorldUnits=0.001;maximumSegmentLengthResidualWorldUnits=0.00001
            maximumApplyMicroseconds=100.0;averageApplyMicroseconds=50.0
        }
        $mountedObservations.mountedRealTimeRiderDisplacement = 1.8
        $mountedObservations.mountedRealTimeHorseDisplacement = 1.8
        $mountedObservations.mountedRealTimeRemaining = 0.1
        $mountedObservations.horsePresentationAfterTurnBasedRestore = 'turnBased=False;poseLease=True;attachmentLease=True'
        $mountedObservations.mountedTurnRiderDisplacement = 1.2
        $mountedObservations.mountedTurnHorseDisplacement = 1.2
        $mountedObservations.mountedTurnTargetDisplacement = 0.0
        $mountedObservations.mountedTurnDriveCount = 4
        $mountedObservations.mountedTurnPostDispatchReassertions = 0
        $mountedObservations.mountedRiderOutcome = [ordered]@{
            action='RiderMelee';actorId='owner';commandOwnerId='owner';resourceOwnerId='owner';targetId='mounted-target'
            result='Success';childAttackStartCount=1;repathCount=1;attackWeaponBlueprintId=('b'*32)
            attackWeaponIsNatural=$false;attackWeaponIsRanged=$false;attackWeaponSlot='EquippedMelee';delegatedMoveExecutorId='horse'
            delegatedMoveExecutorIsExactMount=$true;riderStandardCharged=$true;actionStandardCharged=$true;terminalReason=$null
        }
        $mountedObservations.mountedRiderAttackRules = 1
        $mountedObservations.mountedRiderAttackRolls = 1
        $mountedObservations.mountedRiderDamageRules = 1
        $mountedObservations.mountedHorseOutcome = [ordered]@{
            action='MountPrimaryNatural';actorId='horse';commandOwnerId='horse';resourceOwnerId='horse';targetId='mounted-target'
            result='Success';childAttackStartCount=1;repathCount=0;attackWeaponBlueprintId=$biteGuid
            attackWeaponIsNatural=$true;attackWeaponIsRanged=$false;attackWeaponSlot='AdditionalLimb';delegatedMoveExecutorId=$null
            delegatedMoveExecutorIsExactMount=$false;riderStandardCharged=$false;actionStandardCharged=$true;terminalReason=$null
        }
        $mountedObservations.mountedHorseAttackRules = 1
        $mountedObservations.mountedHorseAttackRolls = 1
        $mountedObservations.mountedHorseDamageRules = 1
        $mountedObservations.mountedTargetCleanupExact = $true
        $mountedArtifact = [ordered]@{
            schemaVersion=4;evidenceKind='horse-mounted-alpha';runId=$mountedRequest.runId;scenario=$mountedRequest.scenario
            branch=$mountedRequest.branch;commit=$mountedRequest.commit;productVersion=$mountedRequest.productVersion
            dllSha256=$mountedRequest.dllSha256;dllMvid=$mountedRequest.dllMvid;createdAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
            status='PASS';assertions=$mountedAssertions;observations=$mountedObservations
            assertionPassCount=$mountedAssertions.Count;assertionFailCount=0;errors=@()
        }
        $mountedPath = Join-Path $mountedRoot 'horse-mounted-alpha.json'
        Write-KmcJsonDurable -Path $mountedPath -Value $mountedArtifact
        $mountedRecord = [ordered]@{
            relativePath='horse-mounted-alpha.json';kind='horse-mounted-alpha'
            length=(Get-Item -LiteralPath $mountedPath).Length;sha256=(Get-KmcSha256 $mountedPath)
        }
        [void](New-TestArtifactManifest -EvidenceRoot $mountedRoot -RunId $mountedRequest.runId -Scenario $mountedRequest.scenario -Artifacts @($mountedRecord))
        $mountedManifest = Read-KmcJson (Join-Path $mountedRoot 'runtime-artifacts.json')
        $mountedSubresult = [pscustomobject]@{
            name='horse-mounted-alpha-suite';status='PASS';assertionPassCount=$mountedAssertions.Count;assertionFailCount=0;errors=@()
        }
        Assert-KmcHorseCompanionUnmountedEvidence -Request $mountedRequest -Manifest $mountedManifest -Status PASS -SubscenarioResults @($mountedSubresult)

        $mountedArtifact.observations.mountedTurnPostDispatchReassertions = 1
        Write-KmcJsonAtomic -Path $mountedPath -Value $mountedArtifact
        $mountedRecord.length=(Get-Item -LiteralPath $mountedPath).Length
        $mountedRecord.sha256=(Get-KmcSha256 $mountedPath)
        [void](New-TestArtifactManifest -EvidenceRoot $mountedRoot -RunId $mountedRequest.runId -Scenario $mountedRequest.scenario -Artifacts @($mountedRecord))
        $mountedManifest = Read-KmcJson (Join-Path $mountedRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcHorseCompanionUnmountedEvidence -Request $mountedRequest -Manifest $mountedManifest -Status PASS -SubscenarioResults @($mountedSubresult) }
        catch { $threw = $true }
        Assert-Test $threw 'horse mounted alpha validator accepted a post-dispatch rider-turn restart'

        $mountedArtifact.observations.mountedTurnPostDispatchReassertions = 0
        $mountedArtifact.observations.mountedHorseOutcome.resourceOwnerId = 'owner'
        Write-KmcJsonAtomic -Path $mountedPath -Value $mountedArtifact
        $mountedRecord.length=(Get-Item -LiteralPath $mountedPath).Length
        $mountedRecord.sha256=(Get-KmcSha256 $mountedPath)
        [void](New-TestArtifactManifest -EvidenceRoot $mountedRoot -RunId $mountedRequest.runId -Scenario $mountedRequest.scenario -Artifacts @($mountedRecord))
        $mountedManifest = Read-KmcJson (Join-Path $mountedRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcHorseCompanionUnmountedEvidence -Request $mountedRequest -Manifest $mountedManifest -Status PASS -SubscenarioResults @($mountedSubresult) }
        catch { $threw = $true }
        Assert-Test $threw 'horse mounted alpha validator accepted rider resource ownership for Horse primary'
    }

    Invoke-HarnessTest 'horse native-controls UX validator binds physical targeting animation pose and cleanup' {
        $nativeRoot = Join-Path $runtimeEvidenceTestRoot 'horse-native-controls-ux-validator'
        New-Item -ItemType Directory -Path $nativeRoot -Force | Out-Null
        $nativeRequest = [pscustomobject]@{
            runId='horse-native-controls-ux-validator';scenario='horse-native-controls-ux-suite'
            branch='codex/mounted-combat-phase3c-native-controls';commit=('c'*40)
            productVersion=$currentProductVersion;dllSha256=('d'*64)
            dllMvid='33333333-4444-5555-6666-777777777777';evidenceRoot=$nativeRoot
        }
        $nativeRequired = @(
            'original-horse-portrait-and-icon','native-mount-ability-present-no-slot-overwrite',
            'native-control-disable-reenable','native-control-save-load-presence',
            'native-saddle-up-invalid-target','native-saddle-up-target-valid-horse',
            'native-mounted-control-surface','inventory-horse-preview-no-ik-exception',
            'mounted-turn-based-rider-movement','human-input-tb-rider-primary-rider-turn',
            'human-input-tb-target-click-admitted','human-input-tb-horse-primary-horse-turn',
            'horse-primary-animation-tb','human-input-rt-rider-primary','human-input-rt-horse-primary',
            'mounted-rider-primary-outcome','mounted-horse-primary-outcome','native-dismount-ability',
            'entity-and-target-restoration','mode-pause-selection-restoration','non-horse-isolation'
        )
        $nativeAssertions = @($nativeRequired | ForEach-Object {
            [ordered]@{name=$_;status='PASS';detail="Synthetic exact native UX contract for $_."}
        })
        $newControlSnapshot = {
            param([bool]$Suspended,[int]$FactCount)
            [ordered]@{
                registered=$true;enabled=$true;serializationSuspended=$Suspended;exactFactCount=$FactCount
                duplicateFactCount=0;managedHotbarSlotCount=0;targetSelectionStartCount=1
                targetSelectionEndCount=1;nativeCastRequestCount=1;nativeRefusalCount=0
                dispatchAcceptedCount=1;dispatchRejectedCount=0
            }
        }
        $newClick = {
            param([string]$Ability,[string]$Caster,[string]$Clicked,[string]$Resolved,[bool]$Accepted)
            [ordered]@{
                abilityGuid=$Ability;casterId=$Caster;clickedTargetId=$Clicked;resolvedTargetId=$Resolved
                priority='Ability';clicked=$Accepted;targetSelectionStartDelta=1;targetSelectionEndDelta=1
                nativeCastRequestDelta=$(if($Accepted){1}else{0});nativeRefusalDelta=$(if($Accepted){0}else{1})
                dispatchAcceptedDelta=$(if($Accepted){1}else{0});dispatchRejectedDelta=0
            }
        }
        $newOutcome = {
            param([string]$Action,[string]$Actor,[bool]$Natural,[bool]$Animation)
            [ordered]@{
                action=$Action;actorId=$Actor;commandOwnerId=$Actor;resourceOwnerId=$Actor
                targetId='target';result='Success';childAttackStartCount=1;repathCount=0
                attackWeaponBlueprintId=('7'*32);attackWeaponIsNatural=$Natural;attackWeaponIsRanged=$false
                attackWeaponSlot=$(if($Natural){'AdditionalLimb'}else{'EquippedMelee'})
                delegatedMoveExecutorId='horse';delegatedMoveExecutorIsExactMount=$true
                riderStandardCharged=(-not $Natural);actionStandardCharged=$true;terminalReason=$null
                attackAnimationHandleCreated=$Animation
                attackAnimationActionName=$(if($Animation){'SpecialAttack'}else{$null})
                attackAnimationActionType=$(if($Animation){'SpecialAttack'}else{$null})
                attackAnimationActed=$Animation;attackAnimationFinished=$Animation;attackAnimationInterrupted=$false
            }
        }
        $nativeObservations = [ordered]@{
            ownerId='owner';horseId='horse'
            nativeControlsBeforeMount=(& $newControlSnapshot $false 1)
            nativeControlsDuringSaveScope=(& $newControlSnapshot $true 0)
            nativeControlsAfterSaveScope=(& $newControlSnapshot $false 1)
            nativeControlsMounted=(& $newControlSnapshot $false 5)
            nativeControlsAfterDismount=(& $newControlSnapshot $false 1)
            nativeMountInvalidTarget=(& $newClick ('1'*32) 'owner' 'owner' 'owner' $false)
            nativeMountValidHorse=(& $newClick ('1'*32) 'owner' 'horse' 'horse' $true)
            nativeTbRiderPrimaryClick=(& $newClick ('2'*32) 'owner' 'target' 'target' $true)
            nativeTbHorsePrimaryClick=(& $newClick ('3'*32) 'horse' 'target' 'target' $true)
            nativeRtRiderPrimaryClick=(& $newClick ('2'*32) 'owner' 'target' 'target' $true)
            nativeRtHorsePrimaryClick=(& $newClick ('3'*32) 'owner' 'target' 'target' $true)
            nativeDismountClick=(& $newClick ('4'*32) 'owner' 'owner' 'owner' $true)
            mountedTurnRiderOutcome=(& $newOutcome 'RiderMelee' 'owner' $false $false)
            mountedTurnHorseOutcome=(& $newOutcome 'MountPrimaryNatural' 'horse' $true $true)
            mountedRiderOutcome=(& $newOutcome 'RiderMelee' 'owner' $false $false)
            mountedHorseOutcome=(& $newOutcome 'MountPrimaryNatural' 'horse' $true $true)
            unmountedHorseBlueprintSpeedFeet=50;mountedHorseBlueprintSpeedFeet=50
            unmountedHorseAgentMaxSpeed=4.0;mountedHorseAgentMaxSpeed=4.0
            unmountedHorseAverageWorldSpeed=3.5;mountedRealTimeAverageWorldSpeed=3.4
            horseProfileId='medium-humanoid-horse-v1';horsePoseProfileId='medium-humanoid-horse-v1'
            horseSourceAnchor='Chest';relationshipState='Unmounted';horseRemoved=$true;targetRemoved=$true
            unrelatedPartyPetsPreserved=$true
            horsePoseCalibration=[ordered]@{
                candidateCount=3;candidateId='horse-human-review-20260829-c'
                selectedPelvisPositionOffset=[ordered]@{x=0.0;y=-0.17;z=-0.02}
                selectedLeftFootTargetFromThigh=[ordered]@{x=-0.15;y=-0.62;z=0.11}
                selectedRightFootTargetFromThigh=[ordered]@{x=0.15;y=-0.62;z=0.11}
                leftFootToAssignedStirrup=0.2;rightFootToAssignedStirrup=0.2
            }
        }
        $nativeArtifact = [ordered]@{
            schemaVersion=5;evidenceKind='horse-native-controls-ux';runId=$nativeRequest.runId
            scenario=$nativeRequest.scenario;branch=$nativeRequest.branch;commit=$nativeRequest.commit
            productVersion=$nativeRequest.productVersion;dllSha256=$nativeRequest.dllSha256
            dllMvid=$nativeRequest.dllMvid;createdAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
            status='PASS';assertions=$nativeAssertions;observations=$nativeObservations
            assertionPassCount=$nativeAssertions.Count;assertionFailCount=0;errors=@()
        }
        $nativePath = Join-Path $nativeRoot 'horse-native-controls-ux.json'
        Write-KmcJsonDurable -Path $nativePath -Value $nativeArtifact
        $nativeRecord = [ordered]@{
            relativePath='horse-native-controls-ux.json';kind='horse-native-controls-ux'
            length=(Get-Item -LiteralPath $nativePath).Length;sha256=(Get-KmcSha256 $nativePath)
        }
        [void](New-TestArtifactManifest -EvidenceRoot $nativeRoot -RunId $nativeRequest.runId -Scenario $nativeRequest.scenario -Artifacts @($nativeRecord))
        $nativeManifest = Read-KmcJson (Join-Path $nativeRoot 'runtime-artifacts.json')
        $nativeSubresult = [pscustomobject]@{
            name='horse-native-controls-ux-suite';status='PASS'
            assertionPassCount=$nativeAssertions.Count;assertionFailCount=0;errors=@()
        }
        Assert-KmcHorseNativeControlsUxEvidence -Request $nativeRequest -Manifest $nativeManifest -Status PASS -SubscenarioResults @($nativeSubresult)

        $nativeArtifact.observations.mountedTurnHorseOutcome.attackAnimationHandleCreated = $false
        Write-KmcJsonAtomic -Path $nativePath -Value $nativeArtifact
        $nativeRecord.length=(Get-Item -LiteralPath $nativePath).Length
        $nativeRecord.sha256=(Get-KmcSha256 $nativePath)
        [void](New-TestArtifactManifest -EvidenceRoot $nativeRoot -RunId $nativeRequest.runId -Scenario $nativeRequest.scenario -Artifacts @($nativeRecord))
        $nativeManifest = Read-KmcJson (Join-Path $nativeRoot 'runtime-artifacts.json')
        $threw = $false
        try { Assert-KmcHorseNativeControlsUxEvidence -Request $nativeRequest -Manifest $nativeManifest -Status PASS -SubscenarioResults @($nativeSubresult) }
        catch { $threw = $true }
        Assert-Test $threw 'Horse native-controls UX validator accepted a missing TB Horse animation handle'
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
