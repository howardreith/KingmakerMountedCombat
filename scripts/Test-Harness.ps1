[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')

$repoRoot = Get-KmcRepositoryRoot
$testParent = [IO.Path]::GetFullPath((Join-Path $repoRoot 'obj\harness-tests'))
$testRoot = Assert-KmcChildPath (Join-Path $testParent ([Guid]::NewGuid().ToString('N'))) $testParent 'harness test root'
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

New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
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

    Invoke-HarnessTest 'transaction stages only package and restores exact tree' {
        $lock = Open-KmcRuntimeLock -StateRoot $stateRoot -RunId 'transaction-test'
        try {
            $script:transactionState = Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $live -PackagePath $package -StateRoot $stateRoot -BackupRoot $backup -StagingRoot $staging
            $staged = Get-KmcDirectoryManifest $live
            Assert-Test ($staged.fileCount -eq 3) 'staged file count including ownership sentinel was not exact'
            Assert-Test (Test-Path -LiteralPath (Join-Path $live 'KingmakerMountedCombat\Info.json')) 'KMC package root missing'
            $sentinel = Read-KmcLiveSentinel $live
            Assert-Test ([string]$sentinel.token -ceq [string]$lock.Token) 'live sentinel does not bind the lock token'
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
        evidenceRoot = (Join-Path (Get-KmcLabRoot) 'runtime-evidence\schema-test')
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
}

Write-Host "TOTAL PASS=$passed FAIL=$failed"
if ($failed -ne 0) { exit 1 }
