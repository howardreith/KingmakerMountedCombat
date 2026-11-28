Set-StrictMode -Version Latest

function Find-KmcQualifiedWorkingBackup {
    param(
        [Parameter(Mandatory = $true)]$ExpectedWorking,
        [Parameter(Mandatory = $true)]$Qualification,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$BackupRoot,
        [Parameter(Mandatory = $true)][string]$SaveRoot
    )
    $transactionRoot=Assert-KmcChildPath (Join-Path ([IO.Path]::GetFullPath($StateRoot)) 'save-transactions') ([IO.Path]::GetFullPath($StateRoot)) 'save transaction authority root'
    $backupParent=Assert-KmcChildPath (Join-Path ([IO.Path]::GetFullPath($BackupRoot)) 'save-transactions') ([IO.Path]::GetFullPath($BackupRoot)) 'save transaction backup root'
    if (-not (Test-Path -LiteralPath $transactionRoot -PathType Container) -or -not (Test-Path -LiteralPath $backupParent -PathType Container)) {
        throw 'No project-owned qualified Working backup authority exists.'
    }
    Assert-KmcNotReparsePoint $transactionRoot 'save transaction authority root'
    Assert-KmcNotReparsePoint $backupParent 'save transaction backup root'
    $candidates=New-Object 'Collections.Generic.List[object]'
    foreach($file in @(Get-ChildItem -LiteralPath $transactionRoot -File -Filter '*.json' -Force|Sort-Object Name -Descending)){
        Assert-KmcRecoveryLeafNoLinks $file.FullName 'save transaction backup authority'
        $state=Read-KmcJson $file.FullName
        if([long]$state.schemaVersion-ne2-or[string]$state.phase-cne'restored'-or
            [string]$state.workingSha256-cne[string]$ExpectedWorking.sha256-or
            [long]$state.workingLength-ne[long]$ExpectedWorking.length-or
            [long]$state.workingLastWriteTimeUtcTicks-ne[long]$ExpectedWorking.lastWriteTimeUtcTicks){continue}
        if(-not[string]::Equals([IO.Path]::GetFullPath([string]$state.saveRoot).TrimEnd('\'),[IO.Path]::GetFullPath($SaveRoot).TrimEnd('\'),[StringComparison]::OrdinalIgnoreCase)-or
            -not[string]::Equals([IO.Path]::GetFullPath([string]$state.workingPath),[IO.Path]::GetFullPath([string]$ExpectedWorking.path),[StringComparison]::OrdinalIgnoreCase)-or
            [string]$state.backupSha256-cne[string]$ExpectedWorking.sha256-or[long]$state.backupLength-ne[long]$ExpectedWorking.length-or
            [string]$state.expectedGameName-cne[string]$Qualification.expectedGameName-or
            [string]$state.expectedGameId-cne[string]$Qualification.expectedGameId-or[string]$state.expectedArea-cne[string]$Qualification.expectedArea-or
            $state.baselineImmutable-ne$true-or$state.saveWriteAllowlistPassed-ne$true){continue}
        $backupPath=Assert-KmcChildPath ([string]$state.backupPath) $backupParent 'qualified Working backup'
        Assert-KmcRecoveryLeafNoLinks $backupPath 'qualified Working backup'
        $backup=Read-KmcFixtureHeader -Path $backupPath -Kind working -SaveRoot (Split-Path -Parent $backupPath) -PermittedFileNamePattern '^KMC_AUTOMATION_WORKING\.original\.zks$'
        if([string]$backup.sha256-cne[string]$ExpectedWorking.sha256-or[long]$backup.length-ne[long]$ExpectedWorking.length-or
            [string]$backup.name-cne'KMC_AUTOMATION_WORKING'-or[string]$backup.gameName-cne[string]$Qualification.expectedGameName-or
            [string]$backup.gameId-cne[string]$Qualification.expectedGameId-or[string]$backup.area-cne[string]$Qualification.expectedArea){continue}
        $candidates.Add([pscustomobject]@{authorityPath=$file.FullName;authoritySha256=Get-KmcSha256 $file.FullName;runId=[string]$state.runId;backup=$backup})
    }
    if($candidates.Count-eq0){throw 'No exact project-owned previously qualified Working backup exists.'}
    return $candidates[0]
}

function Invoke-KmcQualifiedWorkingFixtureRecovery {
    [CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
    param(
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$BackupRoot,
        [Parameter(Mandatory = $true)][string]$QualificationPath,
        [Parameter(Mandatory = $true)]$HistoricalAuthority
    )
    $requestedWhatIf=[bool]$WhatIfPreference
    $WhatIfPreference=$false
    Assert-KmcNoGameProcesses
    # An interrupted repair of this run (its journal exists) is finished through its own stale fixture-recovery lock.
    $resumeStatePath=Join-Path (Join-Path ([IO.Path]::GetFullPath($StateRoot)) 'fixture-recoveries') ($RunId+'.json')
    if(Test-Path -LiteralPath $resumeStatePath -PathType Leaf){
        return Resume-KmcQualifiedWorkingFixtureRecovery -RunId $RunId -SaveRoot $SaveRoot -StateRoot $StateRoot -BackupRoot $BackupRoot -QualificationPath $QualificationPath -HistoricalAuthority $HistoricalAuthority -WhatIf:$requestedWhatIf
    }
    # The neutral reservation precedes the first shared read (the save root). -WhatIf never acquires it (read-only).
    $reservation=$null
    if(-not$requestedWhatIf){$reservation=Enter-KmcNativeReservation -StateRoot $StateRoot -RunId $RunId -Purpose 'fixture-recovery'}
    try{
    $audit=Get-KmcFixtureCandidateAudit $SaveRoot
    if($audit.baselineCount-ne1-or$audit.workingCount-ne1-or@($audit.rejectedKmcLookingNames).Count-ne0){throw 'KMC fixture recovery requires exactly one canonical pair and zero near-matches.'}
    $qualification=Read-KmcJson $QualificationPath
    $expectedBaseline=$HistoricalAuthority.baseline;$expectedWorking=$HistoricalAuthority.working
    $baseline=Read-KmcFixtureHeader -Path $audit.baselinePaths[0] -Kind baseline -SaveRoot $SaveRoot
    if([string]$baseline.sha256-cne[string]$expectedBaseline.sha256-or[long]$baseline.length-ne[long]$expectedBaseline.length-or
        [long]$baseline.lastWriteTimeUtcTicks-ne[long]$expectedBaseline.lastWriteTimeUtcTicks-or
        [string]$baseline.name-cne'KMC_AUTOMATION_BASELINE'-or[string]$baseline.gameName-cne[string]$qualification.expectedGameName-or
        [string]$baseline.gameId-cne[string]$qualification.expectedGameId-or[string]$baseline.area-cne[string]$qualification.expectedArea){
        throw 'KMC Baseline differs; automatic recovery is forbidden because no qualified Baseline backup contract is present.'
    }
    $workingPath=[IO.Path]::GetFullPath([string]$expectedWorking.path)
    if(-not[string]::Equals($workingPath,[IO.Path]::GetFullPath([string]$audit.workingPaths[0]),[StringComparison]::OrdinalIgnoreCase)){throw 'Canonical Working path differs from historical authority.'}
    $currentWorking=Read-KmcFixtureHeader -Path $workingPath -Kind working -SaveRoot $SaveRoot
    if([string]$currentWorking.sha256-ceq[string]$expectedWorking.sha256-and[long]$currentWorking.length-eq[long]$expectedWorking.length-and
        [long]$currentWorking.lastWriteTimeUtcTicks-eq[long]$expectedWorking.lastWriteTimeUtcTicks){Exit-KmcUnlockedNativeReservation $reservation;return [pscustomobject]@{status='already-exact';path=$workingPath;sha256=[string]$currentWorking.sha256}}
    $candidate=Find-KmcQualifiedWorkingBackup -ExpectedWorking $expectedWorking -Qualification $qualification -StateRoot $StateRoot -BackupRoot $BackupRoot -SaveRoot $SaveRoot
    $before=Get-KmcQualificationTreeInventory -Root $SaveRoot -Scope save-root
    $WhatIfPreference=$requestedWhatIf
    if(-not$PSCmdlet.ShouldProcess($workingPath,"quarantine unexpected KMC Working and restore exact qualified backup $($candidate.runId)")){
        $WhatIfPreference=$false
        $after=Get-KmcQualificationTreeInventory -Root $SaveRoot -Scope save-root
        [void](Assert-KmcQualificationTreeInventoriesEqual -Expected $before -Actual $after -Description 'fixture-recovery WhatIf save inventory')
        return [pscustomobject]@{status='what-if';path=$workingPath;backupRunId=$candidate.runId}
    }
    $WhatIfPreference=$false
    $lock=$null;$statePath=$null;$quarantinePath=$null;$replacementQuarantine=$null;$closing=$false
    try{
        $lock=Open-KmcRuntimeLock -StateRoot $StateRoot -RunId $RunId -Purpose 'fixture-recovery' -Reservation $reservation
        [void](Assert-KmcQualificationTreeInventoriesEqual -Expected $before -Actual (Get-KmcQualificationTreeInventory -Root $SaveRoot -Scope save-root) -Description 'fixture-recovery under-lock save inventory')
        $recoveryRoot=Join-Path ([IO.Path]::GetFullPath($StateRoot)) 'fixture-recoveries';if(-not(Test-Path -LiteralPath $recoveryRoot)){New-Item -ItemType Directory -Path $recoveryRoot|Out-Null}
        $quarantineRoot=Join-Path ([IO.Path]::GetFullPath($BackupRoot)) 'fixture-recoveries';if(-not(Test-Path -LiteralPath $quarantineRoot)){New-Item -ItemType Directory -Path $quarantineRoot|Out-Null}
        $runQuarantine=Join-Path $quarantineRoot $RunId;if(Test-Path -LiteralPath $runQuarantine){throw 'Fixture-recovery quarantine already exists.'};New-Item -ItemType Directory -Path $runQuarantine|Out-Null
        $quarantinePath=Join-Path $runQuarantine 'unexpected-KMC_AUTOMATION_WORKING.zks';$replacementQuarantine=Join-Path $runQuarantine 'failed-qualified-replacement.zks'
        $statePath=Join-Path $recoveryRoot ($RunId+'.json')
        $state=[ordered]@{schemaVersion=1;runId=$RunId;token=[string]$lock.Token;phase='prepared';preparedAtUtc=[DateTimeOffset]::UtcNow.ToString('o');saveRoot=[IO.Path]::GetFullPath($SaveRoot);workingPath=$workingPath;unexpectedSha256=[string]$currentWorking.sha256;unexpectedLength=[long]$currentWorking.length;unexpectedLastWriteTimeUtcTicks=[long]$currentWorking.lastWriteTimeUtcTicks;quarantinePath=$quarantinePath;qualifiedBackupPath=[string]$candidate.backup.path;qualifiedBackupSha256=[string]$candidate.backup.sha256;qualifiedBackupAuthorityPath=[string]$candidate.authorityPath;qualifiedBackupAuthoritySha256=[string]$candidate.authoritySha256;expectedWorkingSha256=[string]$expectedWorking.sha256;expectedWorkingLength=[long]$expectedWorking.length;expectedWorkingLastWriteTimeUtcTicks=[long]$expectedWorking.lastWriteTimeUtcTicks}
        Write-KmcJsonCreateNewDurable -Path $statePath -Value $state
        # The save-root inventory before the repair, so an interrupted repair can be verified (foreign saves exact).
        Write-KmcJsonCreateNewDurable -Path (Join-Path $recoveryRoot ($RunId+'.before.json')) -Value $before
        [void](Assert-KmcRuntimeLockOwner $lock)
        Move-Item -LiteralPath $workingPath -Destination $quarantinePath
        Copy-Item -LiteralPath ([string]$candidate.backup.path) -Destination $workingPath
        (Get-Item -LiteralPath $workingPath).LastWriteTimeUtc=[DateTime]::new([long]$expectedWorking.lastWriteTimeUtcTicks,[DateTimeKind]::Utc)
        $pair=Assert-KmcFixturePair -SaveRoot $SaveRoot -QualificationPath $QualificationPath
        [void](Assert-KmcPermanentFixtureIdentity -Pair $pair -HistoricalAuthorityRecord $HistoricalAuthority)
        $after=Get-KmcQualificationTreeInventory -Root $SaveRoot -Scope save-root
        $changed=@(Get-KmcQualificationInventoryDifferences -Before $before -After $after)
        if($changed.Count-ne1-or[string]$changed[0]-cne[IO.Path]::GetFileName($workingPath)){throw 'Fixture recovery changed a non-Working save.'}
        [void](Assert-KmcRuntimeLockOwner $lock)
        $state.phase='recovered';$state['recoveredAtUtc']=[DateTimeOffset]::UtcNow.ToString('o');$state['restoredSaveInventoryDigest']=[string]$after.digest;Write-KmcJsonDurable $statePath $state
        $closing=$true
        Close-KmcRuntimeLock $lock;$lock=$null
        return [pscustomobject]@{status='recovered';path=$workingPath;sha256=[string]$pair.working.sha256;backupRunId=$candidate.runId;statePath=$statePath;quarantinePath=$quarantinePath}
    }catch{
        $failure=$_.Exception.Message
        # A close or ownership failure is not a fixture-operation failure: the repair is complete and verified, or the
        # reservation is lost. Nothing more is written (no rollback); the lock and reservation stay for review.
        if($closing-or($null-ne$lock-and$null-ne$lock.Reservation-and-not(Test-KmcNativeReservationHeld $lock.Reservation))){
            if($null-ne$lock-and$lock.Stream.CanRead){try{Abandon-KmcRuntimeLock $lock}catch{}}
            throw "Fixture recovery stopped without rollback (close or reservation ownership failed): $failure"
        }
        if($null-ne$lock){
            try{
                [void](Assert-KmcRuntimeLockOwner $lock)
                if(Test-Path -LiteralPath $workingPath){Move-Item -LiteralPath $workingPath -Destination $replacementQuarantine}
                if($null-ne$quarantinePath-and(Test-Path -LiteralPath $quarantinePath)){Move-Item -LiteralPath $quarantinePath -Destination $workingPath;(Get-Item -LiteralPath $workingPath).LastWriteTimeUtc=[DateTime]::new([long]$currentWorking.lastWriteTimeUtcTicks,[DateTimeKind]::Utc)}
                if($null-ne$statePath-and(Test-Path -LiteralPath $statePath)){ $rollback=Read-KmcJson $statePath;$rollback.phase='rolled-back';$rollback|Add-Member -NotePropertyName rollbackAtUtc -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('o')) -Force;$rollback|Add-Member -NotePropertyName failure -NotePropertyValue $failure -Force;Write-KmcJsonDurable $statePath $rollback }
                Close-KmcRuntimeLock $lock;$lock=$null
            }catch{if($null-ne$lock){try{Abandon-KmcRuntimeLock $lock}catch{}};throw "Fixture recovery failed and rollback could not be proven: $failure / $($_.Exception.Message)"}
        }
        throw "Fixture recovery failed and the unexpected Working was restored: $failure"
    }
    }finally{
        # A refusal before the lock existed releases the reservation (nothing shared was mutated).
        if($null-ne$reservation-and($null-eq(Get-Variable -Name lock -ValueOnly -ErrorAction SilentlyContinue))-and-not(Test-Path -LiteralPath (Join-Path ([IO.Path]::GetFullPath($StateRoot)) 'active-transaction.lock'))){Exit-KmcUnlockedNativeReservation $reservation}
    }
}

# Finishes an interrupted qualified-Working repair through its own stale fixture-recovery lock and journal: the stale
# lock and the reservation of exactly that run are adopted, the repair is completed or confirmed (Working equal to the
# qualified backup, every other save exactly as before the repair; a rolled-back repair: the unexpected Working back
# in place), and only then are the lock and reservation released. Anything ambiguous keeps both for review.
function Resume-KmcQualifiedWorkingFixtureRecovery {
    param(
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$BackupRoot,
        [Parameter(Mandatory = $true)][string]$QualificationPath,
        [Parameter(Mandatory = $true)]$HistoricalAuthority,
        [switch]$WhatIf
    )
    $recoveryRoot=Join-Path ([IO.Path]::GetFullPath($StateRoot)) 'fixture-recoveries'
    $statePath=Join-Path $recoveryRoot ($RunId+'.json');$beforePath=Join-Path $recoveryRoot ($RunId+'.before.json')
    Assert-KmcRecoveryLeafNoLinks $statePath 'fixture-recovery journal'
    $state=Read-KmcJson $statePath
    if([int]$state.schemaVersion-ne1-or[string]$state.runId-cne$RunId-or[string]$state.phase-cnotin@('prepared','recovered','rolled-back')-or
        -not[string]::Equals([IO.Path]::GetFullPath([string]$state.saveRoot).TrimEnd('\'),[IO.Path]::GetFullPath($SaveRoot).TrimEnd('\'),[StringComparison]::OrdinalIgnoreCase)-or
        -not[string]::Equals([IO.Path]::GetFullPath([string]$state.workingPath),[IO.Path]::GetFullPath([string]$HistoricalAuthority.working.path),[StringComparison]::OrdinalIgnoreCase)){
        throw 'The fixture-recovery journal does not match this run, save root and historical Working.'
    }
    if(-not(Test-Path -LiteralPath $beforePath -PathType Leaf)){throw 'The interrupted fixture recovery has no recorded pre-repair save inventory; it is left for review.'}
    if(-not(Test-Path -LiteralPath (Join-Path ([IO.Path]::GetFullPath($StateRoot)) 'active-transaction.lock') -PathType Leaf)){throw 'The interrupted fixture recovery has no stale lock; it is left for review.'}
    if($WhatIf){return [pscustomobject]@{status='what-if-resume';runId=$RunId;phase=[string]$state.phase}}
    $resumedPhase=[string]$state.phase
    $lock=Adopt-KmcStaleRuntimeLock -StateRoot $StateRoot -ExpectedPurpose 'fixture-recovery'
    try{
        if([string]$lock.RunId-cne$RunId-or[string]$lock.Token-cne[string]$state.token){throw 'The stale fixture-recovery lock belongs to another run or token.'}
        $before=Read-KmcJson $beforePath
        $workingPath=[string]$state.workingPath;$quarantinePath=[string]$state.quarantinePath
        $replacementQuarantine=Join-Path (Split-Path -Parent $quarantinePath) 'failed-qualified-replacement.zks'
        $expected=[pscustomobject]@{sha256=[string]$state.expectedWorkingSha256;length=[long]$state.expectedWorkingLength;ticks=[long]$state.expectedWorkingLastWriteTimeUtcTicks}
        function Test-ResumeWorking([string]$Sha,[long]$Length,[long]$Ticks){
            if(-not(Test-Path -LiteralPath $workingPath -PathType Leaf)){return $false}
            $item=Get-Item -LiteralPath $workingPath
            return ((Get-KmcSha256 $workingPath)-ceq$Sha-and$item.Length-eq$Length-and$item.LastWriteTimeUtc.Ticks-eq$Ticks)
        }
        if([string]$state.phase-ceq'rolled-back'){
            if(-not(Test-ResumeWorking ([string]$state.unexpectedSha256) ([long]$state.unexpectedLength) ([long]$state.unexpectedLastWriteTimeUtcTicks))){throw 'The rolled-back repair does not have the unexpected Working back in place.'}
        }elseif(-not(Test-ResumeWorking $expected.sha256 $expected.length $expected.ticks)){
            if([string]$state.phase-ceq'recovered'){throw 'A recovered repair no longer has the exact qualified Working.'}
            # prepared: finish the repair from the journal's own quarantine and qualified backup.
            if(-not(Test-Path -LiteralPath $quarantinePath -PathType Leaf)){
                if(Test-ResumeWorking ([string]$state.unexpectedSha256) ([long]$state.unexpectedLength) ([long]$state.unexpectedLastWriteTimeUtcTicks)){
                    [void](Assert-KmcRuntimeLockOwner $lock)
                    Move-Item -LiteralPath $workingPath -Destination $quarantinePath
                }else{throw 'The interrupted repair has neither its quarantined Working nor the unexpected Working in place.'}
            }
            if((Get-KmcSha256 $quarantinePath)-cne[string]$state.unexpectedSha256){throw 'The quarantined Working is not the journal''s unexpected Working.'}
            $backupPath=[string]$state.qualifiedBackupPath
            if((Get-KmcSha256 $backupPath)-cne[string]$state.qualifiedBackupSha256){throw 'The qualified backup differs from the journal.'}
            [void](Assert-KmcRuntimeLockOwner $lock)
            if(Test-Path -LiteralPath $workingPath){
                if(Test-Path -LiteralPath $replacementQuarantine){throw 'A failed replacement is already quarantined; the partial Working is left for review.'}
                Move-Item -LiteralPath $workingPath -Destination $replacementQuarantine
            }
            Copy-Item -LiteralPath $backupPath -Destination $workingPath
            (Get-Item -LiteralPath $workingPath).LastWriteTimeUtc=[DateTime]::new($expected.ticks,[DateTimeKind]::Utc)
            if(-not(Test-ResumeWorking $expected.sha256 $expected.length $expected.ticks)){throw 'The resumed repair did not produce the exact qualified Working.'}
        }
        $after=Get-KmcQualificationTreeInventory -Root $SaveRoot -Scope save-root
        $changed=@(Get-KmcQualificationInventoryDifferences -Before $before -After $after)
        $workingLeaf=[IO.Path]::GetFileName($workingPath)
        if(@($changed|Where-Object{[string]$_-cne$workingLeaf}).Count-ne0){throw 'A non-Working save differs from before the repair.'}
        if([string]$state.phase-cne'rolled-back'){
            $pair=Assert-KmcFixturePair -SaveRoot $SaveRoot -QualificationPath $QualificationPath
            [void](Assert-KmcPermanentFixtureIdentity -Pair $pair -HistoricalAuthorityRecord $HistoricalAuthority)
        }
        [void](Assert-KmcRuntimeLockOwner $lock)
        if([string]$state.phase-ceq'prepared'){
            $state.phase='recovered';$state|Add-Member -NotePropertyName recoveredAtUtc -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('o')) -Force
            $state|Add-Member -NotePropertyName restoredSaveInventoryDigest -NotePropertyValue ([string]$after.digest) -Force
            $state|Add-Member -NotePropertyName resumedAtUtc -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('o')) -Force
            Write-KmcJsonDurable $statePath $state
        }
        Close-KmcRuntimeLock $lock;$lock=$null
        return [pscustomobject]@{status=('resumed-'+$resumedPhase);path=$workingPath;runId=$RunId;statePath=$statePath}
    }finally{if($null-ne$lock-and$lock.Stream.CanRead){Abandon-KmcRuntimeLock $lock}}
}
