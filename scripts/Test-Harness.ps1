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
    param([Parameter(Mandatory = $true)][string]$UniqueId, [Parameter(Mandatory = $true)][int]$SizeOrdinal)
    return [ordered]@{
        uniqueId=$UniqueId;sizeOrdinal=$SizeOrdinal;inCombat=$false;stockAgentEnabled=$true;avoidanceDisabled=$false
        agentOverrideType=$null;overrideComponentCount=0
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
    $cleanup = if ($WithCleanup) {
        [ordered]@{trigger='Manual';result='PASS';succeeded=$true;state='Unmounted';movementAuthorityResidual=$false;presentationResidual=$false;errors=@()}
    } else {
        [ordered]@{trigger=$null;result=$null;succeeded=$null;state=$null;movementAuthorityResidual=$null;presentationResidual=$null;errors=@()}
    }
    $mounted = $Phase -ceq 'mounted-next-frame'
    return [ordered]@{
        schemaVersion=1;runId=[string]$Request.runId;scenario=[string]$Request.scenario;row=$Row;phase=$Phase
        utcTimestamp=[DateTimeOffset]::UtcNow.ToUniversalTime().ToString('o');branch=[string]$Request.branch;commit=[string]$Request.commit
        productVersion=[string]$Request.productVersion;dllSha256=[string]$Request.dllSha256;dllMvid=[string]$Request.dllMvid
        sequence=$Sequence;frame=[int]($Sequence + 1);relationshipState=$RelationshipState;rowStatus=$RowStatus
        assertionPassCount=$AssertionPassCount;assertionFailCount=$AssertionFailCount;cleanup=$cleanup
        partyCombat=$false;riderCombat=$false;mountCombat=$false;turnBased=$false;paused=$false;currentGameMode='Default'
        rider=(New-TestLifecycleUnitEvidence 'rider-id' 4);mount=(New-TestLifecycleUnitEvidence 'mount-id' 6)
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

        $incompleteSuiteRecords = & $newSuiteRecords @($suiteRows[0..6])
        [void](Write-TestLifecycleEvidence -EvidenceRoot $suiteRequest.evidenceRoot -Request $suiteRequest -Records $incompleteSuiteRecords)
        $suiteManifest = Read-KmcJson (Join-Path $suiteRequest.evidenceRoot 'runtime-artifacts.json')
        $threw=$false
        try { Assert-KmcLifecycleScenarioEvidence -Request $suiteRequest -Manifest $suiteManifest -Status 'PASS' -SubscenarioResults $suiteSubresults } catch { $threw=$true }
        Assert-Test $threw 'lifecycle-suite PASS accepted fewer than the exact eight ordered rows'
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
