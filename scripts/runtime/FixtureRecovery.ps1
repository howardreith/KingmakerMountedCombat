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
    # A private self-test reservation covers only disposable stand-ins, checked before any shared root is opened.
    Assert-KmcReservationCoversSurface -Path @($SaveRoot,$BackupRoot,$QualificationPath)
    $fullState=[IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    $statePath=Join-Path (Join-Path $fullState 'fixture-recoveries') ($RunId+'.json')
    $quarantinePath=Join-Path ([IO.Path]::GetFullPath($BackupRoot)) ('fixture-recoveries\'+$RunId+'\unexpected-KMC_AUTOMATION_WORKING.zks')
    $replacementQuarantine=Join-Path ([IO.Path]::GetFullPath($BackupRoot)) ('fixture-recoveries\'+$RunId+'\failed-qualified-replacement.zks')
    # An interrupted repair of this run (prepared journal and its own stale fixture-recovery lock) is continued, never
    # restarted. Kingmaker native-test reservation (docs/NATIVE-RESERVATION.md): taken over for that run and token, or
    # taken fresh, before the first shared read (the save root), previews included; released only after a verified close.
    # A terminal journal (recovered or rolled-back) whose own stale lock survives was interrupted before its close: it is
    # finished too, without another repair write.
    $pending=$null
    $staleLock=Join-Path $fullState 'active-transaction.lock'
    if(Test-Path -LiteralPath $statePath -PathType Leaf){
        $pending=Read-KmcJson $statePath
        $ownLock=$false;if(Test-Path -LiteralPath $staleLock -PathType Leaf){try{$ownLock=[string](Read-KmcStaleLockPayload $staleLock 'fixture-recovery').runId-ceq$RunId}catch{}}
        if([string]$pending.phase-ceq'prepared'){if(-not$ownLock){throw 'The prepared fixture repair of this run has no stale lock; it is left for the owner.'}}
        elseif([string]$pending.phase-cin@('recovered','rolled-back')-and$ownLock){}
        else{$pending=$null}
    }
    $lock=$null;$reservation=$null
    if($null-ne$pending){
        if([int]$pending.schemaVersion-ne1-or[string]$pending.runId-cne$RunId-or[string]$pending.token-cnotmatch'^[0-9a-f]{64}$'){throw 'The prepared fixture-repair journal is not this run''s.'}
        $reservation=Enter-KmcBoundRecoveryReservation -StateRoot $fullState -RunId $RunId -JournalPath @($statePath) -ExpectedLockToken ([string]$pending.token) -ExpectedPurpose 'fixture-recovery' -Purpose 'fixture-recovery'
    }
    else{$reservation=Enter-KmcNativeReservation -StateRoot $fullState -RunId $RunId -Purpose $(if($requestedWhatIf){'fixture-recovery-preview'}else{'fixture-recovery'})}
    $closing=$false;$journalCreated=$false;$moved=$false;$replaced=$false;$collision=$false;$resuming=($null-ne$pending)
    # The phase-specific terminal files, rechecked after confirmation and immediately before a close: recovered means the
    # exact qualified Working (bytes, length, timestamp) with the unexpected original quarantined; rolled-back means the
    # unexpected original back in place (bytes, length, timestamp) and nothing quarantined. A drift is an incident and
    # keeps the lock and the lease.
    $assertTerminalFiles={
        param([string]$Phase,$Lock,$Expected,$Journal)
        $item=if(Test-Path -LiteralPath $workingPath -PathType Leaf){Get-Item -LiteralPath $workingPath}else{$null}
        $held=Test-Path -LiteralPath $quarantinePath -PathType Leaf
        $observed=if($null-eq$item){'Working absent'}else{'Working '+(Get-KmcSha256 $workingPath)+' length '+$item.Length+' ticks '+$item.LastWriteTimeUtc.Ticks}
        $observed+='; quarantine '+$(if($held){Get-KmcSha256 $quarantinePath}else{'absent'})
        $ok=if($Phase-ceq'recovered'){
            $null-ne$item-and$item.Length-eq[long]$Expected.length-and$item.LastWriteTimeUtc.Ticks-eq[long]$Expected.lastWriteTimeUtcTicks-and(Get-KmcSha256 $workingPath)-ceq[string]$Expected.sha256-and
            $held-and(Get-KmcSha256 $quarantinePath)-ceq[string]$Journal.unexpectedSha256
        }else{
            $null-ne$item-and$item.Length-eq[long]$Journal.unexpectedLength-and$item.LastWriteTimeUtc.Ticks-eq[long]$Journal.unexpectedLastWriteTimeUtcTicks-and(Get-KmcSha256 $workingPath)-ceq[string]$Journal.unexpectedSha256-and-not$held
        }
        if(-not$ok){
            Write-KmcReservationIncident -Session $Lock.Reservation -Context ("fixture repair: the $Phase terminal files changed after confirmation") -Observed $observed -InitialDifferences "expected the $Phase state of run $RunId" -RestorationPerformed @($Lock.Reservation.Progress) -RemainingDifferences 'every file left as observed' -ResourcesReleased 'none: the KMC lock and the reservation stay'
            throw "The $Phase fixture state changed after confirmation ($observed); the repair refuses to close and keeps its lock and reservation."
        }
    }
    try{
    $audit=Get-KmcFixtureCandidateAudit $SaveRoot
    $qualification=Read-KmcJson $QualificationPath
    $expectedBaseline=$HistoricalAuthority.baseline;$expectedWorking=$HistoricalAuthority.working
    $workingPath=[IO.Path]::GetFullPath([string]$expectedWorking.path)
    if($null-ne$pending){
        # Continue the interrupted repair: the journal binds the run, the destinations derive from it, and nothing is
        # written before the unaffected saves, the quarantined bytes and the backup authority are proven.
        if(-not[string]::Equals([IO.Path]::GetFullPath([string]$pending.workingPath),$workingPath,[StringComparison]::OrdinalIgnoreCase)-or
            -not[string]::Equals([IO.Path]::GetFullPath([string]$pending.quarantinePath),$quarantinePath,[StringComparison]::OrdinalIgnoreCase)-or
            -not[string]::Equals([IO.Path]::GetFullPath([string]$pending.saveRoot).TrimEnd('\'),[IO.Path]::GetFullPath($SaveRoot).TrimEnd('\'),[StringComparison]::OrdinalIgnoreCase)){throw 'The prepared fixture-repair journal names other destinations.'}
        $now=Get-KmcQualificationTreeInventory -Root $SaveRoot -Scope save-root
        if((Get-KmcUnaffectedSaveDigest $now $workingPath)-cne[string]$pending.unaffectedSaveDigest){throw 'A save other than Working differs from before the interrupted repair; it is left for the owner.'}
        $candidate=Find-KmcQualifiedWorkingBackup -ExpectedWorking $expectedWorking -Qualification $qualification -StateRoot $StateRoot -BackupRoot $BackupRoot -SaveRoot $SaveRoot
        if([string]$candidate.backup.sha256-cne[string]$pending.qualifiedBackupSha256-or[string]$candidate.authorityPath-cne[string]$pending.qualifiedBackupAuthorityPath){throw 'The qualified backup authority differs from the interrupted repair.'}
        $quarantined=Test-Path -LiteralPath $quarantinePath -PathType Leaf
        if($quarantined){Assert-KmcRecoveryLeafNoLinks $quarantinePath 'fixture-repair quarantine';if((Get-KmcSha256 $quarantinePath)-cne[string]$pending.unexpectedSha256){throw 'The quarantined Working is not the repair''s unexpected Working.'}}
        $present=Test-Path -LiteralPath $workingPath -PathType Leaf
        $exact=$present-and(Get-KmcSha256 $workingPath)-ceq[string]$expectedWorking.sha256
        $untouched=$present-and(Get-KmcSha256 $workingPath)-ceq[string]$pending.unexpectedSha256
        $action=if([string]$pending.phase-ceq'recovered'){if($quarantined-and$exact){'close-recovered'}else{'ambiguous'}}
            elseif([string]$pending.phase-ceq'rolled-back'){if($untouched){'close-rolled-back'}else{'ambiguous'}}
            elseif($quarantined-and-not$present){'copy-qualified-backup'}elseif($quarantined-and$exact){'verify-replacement'}elseif(-not$quarantined-and$untouched){'nothing-moved'}else{'ambiguous'}
        if($action-ceq'ambiguous'){throw 'The interrupted repair''s Working and quarantine are ambiguous; every byte is left in place for the owner.'}
        $WhatIfPreference=$requestedWhatIf
        if(-not$PSCmdlet.ShouldProcess($workingPath,"continue interrupted KMC Working repair $RunId ($action)")){
            $WhatIfPreference=$false
            Exit-KmcUnlockedNativeReservation $reservation
            return [pscustomobject]@{status='what-if';path=$workingPath;resume=$action}
        }
        $WhatIfPreference=$false
        $lock=Adopt-KmcStaleRuntimeLock -StateRoot $StateRoot -ExpectedPurpose 'fixture-recovery' -ExpectedRunId $RunId -ExpectedLockToken ([string]$pending.token) -Reservation $reservation
        Invoke-KmcReservationTestHook 'fixture-before-continued-write'
        if($action-ceq'copy-qualified-backup'){
            # The plan is rechecked under the adopted lock after confirmation: Working must still be absent and the
            # quarantined original unchanged; the copy refuses an existing destination.
            if((Test-Path -LiteralPath $workingPath)-or(Get-KmcSha256 $quarantinePath)-cne[string]$pending.unexpectedSha256){
                Write-KmcReservationIncident -Session $lock.Reservation -Context 'fixture continuation refused: Working or its quarantine changed after confirmation' -Observed $(if(Test-Path -LiteralPath $workingPath){Get-KmcSha256 $workingPath}else{'Working absent'}) -RestorationPerformed 'none' -RemainingDifferences 'Working and the quarantined original left in place' -ResourcesReleased 'none: KMC lock and reservation kept'
                throw 'Working reappeared or its quarantine changed after confirmation; the continued repair refuses and leaves every byte in place.'
            }
            [void](Assert-KmcRuntimeLockOwner $lock)  # ownership pair immediately before the shared write
            [IO.File]::Copy([string]$candidate.backup.path,$workingPath,$false)
            [void](Assert-KmcRuntimeLockOwner $lock)  # ownership pair immediately before the shared timestamp write
            (Get-Item -LiteralPath $workingPath).LastWriteTimeUtc=[DateTime]::new([long]$expectedWorking.lastWriteTimeUtcTicks,[DateTimeKind]::Utc)
            Add-KmcReservationProgress $lock 'Working replaced by the qualified backup'
        }
        if($action-cin@('nothing-moved','close-rolled-back')){
            if(-not(Test-Path -LiteralPath $workingPath -PathType Leaf)-or(Get-KmcSha256 $workingPath)-cne[string]$pending.unexpectedSha256){throw 'Working changed after confirmation; the continued repair refuses.'}
        }
        else{
            $pair=Assert-KmcFixturePair -SaveRoot $SaveRoot -QualificationPath $QualificationPath
            [void](Assert-KmcPermanentFixtureIdentity -Pair $pair -HistoricalAuthorityRecord $HistoricalAuthority)
        }
        $after=Get-KmcQualificationTreeInventory -Root $SaveRoot -Scope save-root
        if((Get-KmcUnaffectedSaveDigest $after $workingPath)-cne[string]$pending.unaffectedSaveDigest){throw 'A save other than Working changed during the continued repair.'}
        [void](Assert-KmcRuntimeLockOwner $lock)
        if($action-cin@('copy-qualified-backup','verify-replacement','nothing-moved')){
            $pending.phase=$(if($action-ceq'nothing-moved'){'rolled-back'}else{'recovered'})
            $pending|Add-Member -NotePropertyName continuedAtUtc -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('o')) -Force
            Write-KmcJsonDurable $statePath $pending
        }
        & $assertTerminalFiles $(if($action-cin@('nothing-moved','close-rolled-back')){'rolled-back'}else{'recovered'}) $lock $expectedWorking $pending
        [void](Assert-KmcRuntimeLockOwner $lock)
        $closing=$true
        Close-KmcRuntimeLock $lock;$lock=$null
        return [pscustomobject]@{status=('continued-'+$action);path=$workingPath;statePath=$statePath}
    }
    if($audit.baselineCount-ne1-or$audit.workingCount-ne1-or@($audit.rejectedKmcLookingNames).Count-ne0){throw 'KMC fixture recovery requires exactly one canonical pair and zero near-matches.'}
    $baseline=Read-KmcFixtureHeader -Path $audit.baselinePaths[0] -Kind baseline -SaveRoot $SaveRoot
    if([string]$baseline.sha256-cne[string]$expectedBaseline.sha256-or[long]$baseline.length-ne[long]$expectedBaseline.length-or
        [long]$baseline.lastWriteTimeUtcTicks-ne[long]$expectedBaseline.lastWriteTimeUtcTicks-or
        [string]$baseline.name-cne'KMC_AUTOMATION_BASELINE'-or[string]$baseline.gameName-cne[string]$qualification.expectedGameName-or
        [string]$baseline.gameId-cne[string]$qualification.expectedGameId-or[string]$baseline.area-cne[string]$qualification.expectedArea){
        throw 'KMC Baseline differs; automatic recovery is forbidden because no qualified Baseline backup contract is present.'
    }
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
        Exit-KmcUnlockedNativeReservation $reservation
        return [pscustomobject]@{status='what-if';path=$workingPath;backupRunId=$candidate.runId}
    }
    $WhatIfPreference=$false
        $lock=Open-KmcRuntimeLock -StateRoot $StateRoot -RunId $RunId -Purpose 'fixture-recovery' -Reservation $reservation
        Assert-KmcReservationCoversSurface -Lock $lock -Path @($SaveRoot)
        [void](Assert-KmcQualificationTreeInventoriesEqual -Expected $before -Actual (Get-KmcQualificationTreeInventory -Root $SaveRoot -Scope save-root) -Description 'fixture-recovery under-lock save inventory')
        $recoveryRoot=Join-Path $fullState 'fixture-recoveries';if(-not(Test-Path -LiteralPath $recoveryRoot)){New-Item -ItemType Directory -Path $recoveryRoot|Out-Null}
        $quarantineRoot=Join-Path ([IO.Path]::GetFullPath($BackupRoot)) 'fixture-recoveries';if(-not(Test-Path -LiteralPath $quarantineRoot)){New-Item -ItemType Directory -Path $quarantineRoot|Out-Null}
        $runQuarantine=Join-Path $quarantineRoot $RunId;if(Test-Path -LiteralPath $runQuarantine){throw 'Fixture-recovery quarantine already exists.'};New-Item -ItemType Directory -Path $runQuarantine|Out-Null
        $state=[ordered]@{schemaVersion=1;runId=$RunId;token=[string]$lock.Token;phase='prepared';preparedAtUtc=[DateTimeOffset]::UtcNow.ToString('o');saveRoot=[IO.Path]::GetFullPath($SaveRoot);workingPath=$workingPath;unexpectedSha256=[string]$currentWorking.sha256;unexpectedLength=[long]$currentWorking.length;unexpectedLastWriteTimeUtcTicks=[long]$currentWorking.lastWriteTimeUtcTicks;quarantinePath=$quarantinePath;qualifiedBackupPath=[string]$candidate.backup.path;qualifiedBackupSha256=[string]$candidate.backup.sha256;qualifiedBackupAuthorityPath=[string]$candidate.authorityPath;qualifiedBackupAuthoritySha256=[string]$candidate.authoritySha256;expectedWorkingSha256=[string]$expectedWorking.sha256;expectedWorkingLength=[long]$expectedWorking.length;expectedWorkingLastWriteTimeUtcTicks=[long]$expectedWorking.lastWriteTimeUtcTicks;unaffectedSaveDigest=(Get-KmcUnaffectedSaveDigest $before $workingPath)}
        Register-KmcRunObligation -Lock $lock -Name 'fixture-recovery'
        Write-KmcJsonCreateNewDurable -Path $statePath -Value $state
        $journalCreated=$true
        [void](Assert-KmcRuntimeLockOwner $lock)  # ownership pair immediately before the shared move
        Move-Item -LiteralPath $workingPath -Destination $quarantinePath
        $moved=$true
        Add-KmcReservationProgress $lock ('unexpected Working quarantined at '+$quarantinePath+'; Working absent')
        Invoke-KmcReservationTestHook 'fixture-after-fresh-quarantine'
        [void](Assert-KmcRuntimeLockOwner $lock)  # ownership pair immediately before the shared write
        # No-overwrite installation: a Working that reappeared after the quarantine move is never overwritten; both files,
        # the transaction and the first incident are preserved.
        try{if(Test-Path -LiteralPath $workingPath){throw [IO.IOException]::new('Working reappeared')};[IO.File]::Copy([string]$candidate.backup.path,$workingPath,$false)}
        catch [IO.IOException]{
            $collision=$true
            Write-KmcReservationIncident -Session $lock.Reservation -Context 'fixture repair: Working reappeared before the qualified copy' -Observed ('Working '+$(if(Test-Path -LiteralPath $workingPath -PathType Leaf){Get-KmcSha256 $workingPath}else{'unreadable'})+'; quarantined original '+(Get-KmcSha256 $quarantinePath)) -RestorationPerformed @($lock.Reservation.Progress) -RemainingDifferences 'the reappeared Working and the quarantined original are both kept' -ResourcesReleased 'none: the KMC lock and the reservation stay'
            throw 'A Working save reappeared after the quarantine move; nothing was overwritten, both files and the transaction are kept for the owner.'
        }
        $replaced=$true
        [void](Assert-KmcRuntimeLockOwner $lock)  # ownership pair immediately before the shared timestamp write
        (Get-Item -LiteralPath $workingPath).LastWriteTimeUtc=[DateTime]::new([long]$expectedWorking.lastWriteTimeUtcTicks,[DateTimeKind]::Utc)
        Add-KmcReservationProgress $lock ('qualified Working installed at '+$workingPath)
        $pair=Assert-KmcFixturePair -SaveRoot $SaveRoot -QualificationPath $QualificationPath
        [void](Assert-KmcPermanentFixtureIdentity -Pair $pair -HistoricalAuthorityRecord $HistoricalAuthority)
        $after=Get-KmcQualificationTreeInventory -Root $SaveRoot -Scope save-root
        $changed=@(Get-KmcQualificationInventoryDifferences -Before $before -After $after)
        if($changed.Count-ne1-or[string]$changed[0]-cne[IO.Path]::GetFileName($workingPath)){throw 'Fixture recovery changed a non-Working save.'}
        [void](Assert-KmcRuntimeLockOwner $lock)
        $state.phase='recovered';$state['recoveredAtUtc']=[DateTimeOffset]::UtcNow.ToString('o');$state['restoredSaveInventoryDigest']=[string]$after.digest;Write-KmcJsonDurable $statePath $state
        & $assertTerminalFiles 'recovered' $lock $expectedWorking ([pscustomobject]$state)
        [void](Assert-KmcRuntimeLockOwner $lock)
        $closing=$true
        Close-KmcRuntimeLock $lock;$lock=$null
        return [pscustomobject]@{status='recovered';path=$workingPath;sha256=[string]$pair.working.sha256;backupRunId=$candidate.runId;statePath=$statePath;quarantinePath=$quarantinePath}
    }catch{
        $failure=$_.Exception.Message
        # Before any lock: nothing shared was mutated (a fresh reservation is released, a taken-over one left resumable).
        if($null-eq$lock){Exit-KmcUnlockedNativeReservation $reservation -PreserveError;throw}
        if($resuming-and-not$closing){if($lock.Stream.CanRead){try{Abandon-KmcRuntimeLock $lock}catch{}};throw "The continued fixture repair stopped; the lock and reservation stay for a retry or the owner: $failure"}
        if($collision){if($lock.Stream.CanRead){try{Abandon-KmcRuntimeLock $lock}catch{}};throw "Fixture recovery stopped without overwriting: $failure"}
        # A failed close (the repair is complete and verified) or a lost reservation is not a fixture-operation failure:
        # nothing more is written; the lock and reservation stay for the owner, with the incident recorded.
        $lost=-not(Test-KmcNativeReservationHeld $lock.Reservation)
        if($closing-or$lost){
            if($lost){Write-KmcReservationIncident -Session $lock.Reservation -Context 'fixture repair' -Observed $(try{[IO.File]::ReadAllText([string]$lock.Reservation.Convention.RecordPath)}catch{$null}) -InitialDifferences 'reservation lost during the fixture repair' -RestorationPerformed $(if($replaced){'Working replaced by the qualified backup'}elseif($moved){'unexpected Working quarantined'}else{'none'}) -RemainingDifferences $(if($moved-and-not$replaced){'Working absent; unexpected Working in quarantine'}else{'unknown'}) -ResourcesReleased 'none: KMC lock and reservation record left in place'}
            if($lock.Stream.CanRead){try{Abandon-KmcRuntimeLock $lock}catch{}}
            throw "Fixture recovery stopped without rollback (close or reservation ownership failed): $failure"
        }
        try{
            # Undo only the moves that happened: the replacement (when written) to its quarantine, then the original back.
            if($replaced-and(Test-Path -LiteralPath $workingPath)){[void](Assert-KmcRuntimeLockOwner $lock);Move-Item -LiteralPath $workingPath -Destination $replacementQuarantine}
            if($moved-and(Test-Path -LiteralPath $quarantinePath)){[void](Assert-KmcRuntimeLockOwner $lock);Move-Item -LiteralPath $quarantinePath -Destination $workingPath;(Get-Item -LiteralPath $workingPath).LastWriteTimeUtc=[DateTime]::new([long]$currentWorking.lastWriteTimeUtcTicks,[DateTimeKind]::Utc)}
            $restoredWorking=Read-KmcFixtureHeader -Path $workingPath -Kind working -SaveRoot $SaveRoot
            if([string]$restoredWorking.sha256-cne[string]$currentWorking.sha256-or[long]$restoredWorking.lastWriteTimeUtcTicks-ne[long]$currentWorking.lastWriteTimeUtcTicks){throw 'Working is not back to its exact pre-repair bytes.'}
            if((Get-KmcUnaffectedSaveDigest (Get-KmcQualificationTreeInventory -Root $SaveRoot -Scope save-root) $workingPath)-cne(Get-KmcUnaffectedSaveDigest $before $workingPath)){throw 'A save other than Working differs after rollback.'}
            if($journalCreated){ $rollback=Read-KmcJson $statePath;$rollback.phase='rolled-back';$rollback|Add-Member -NotePropertyName rollbackAtUtc -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('o')) -Force;$rollback|Add-Member -NotePropertyName failure -NotePropertyValue $failure -Force;Write-KmcJsonDurable $statePath $rollback }
            Close-KmcRuntimeLock $lock;$lock=$null
        }catch{if($null-ne$lock){try{Abandon-KmcRuntimeLock $lock}catch{}};throw "Fixture recovery failed and rollback could not be proven: $failure / $($_.Exception.Message)"}
        throw "Fixture recovery failed and the unexpected Working was restored: $failure"
    }
}

# A digest of every save-root entry other than Working (the saves a fixture repair must leave untouched).
function Get-KmcUnaffectedSaveDigest {
    param([Parameter(Mandatory = $true)]$Inventory, [Parameter(Mandatory = $true)][string]$WorkingPath)
    $leaf = [IO.Path]::GetFileName($WorkingPath)
    $lines = @($Inventory.entries | Where-Object { [string]$_.path -cne $leaf } | ForEach-Object { '{0}|{1}|{2}|{3}|{4}' -f $_.kind, $_.path, $_.length, $_.lastWriteTimeUtcTicks, $_.sha256 })
    return Get-KmcTextSha256 ($lines -join "`n")
}
