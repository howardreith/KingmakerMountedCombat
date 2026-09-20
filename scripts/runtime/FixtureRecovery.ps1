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
        [long]$currentWorking.lastWriteTimeUtcTicks-eq[long]$expectedWorking.lastWriteTimeUtcTicks){return [pscustomobject]@{status='already-exact';path=$workingPath;sha256=[string]$currentWorking.sha256}}
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
    $lock=$null;$statePath=$null;$quarantinePath=$null;$replacementQuarantine=$null
    try{
        $lock=Open-KmcRuntimeLock -StateRoot $StateRoot -RunId $RunId -Purpose 'fixture-recovery'
        [void](Assert-KmcQualificationTreeInventoriesEqual -Expected $before -Actual (Get-KmcQualificationTreeInventory -Root $SaveRoot -Scope save-root) -Description 'fixture-recovery under-lock save inventory')
        $recoveryRoot=Join-Path ([IO.Path]::GetFullPath($StateRoot)) 'fixture-recoveries';if(-not(Test-Path -LiteralPath $recoveryRoot)){New-Item -ItemType Directory -Path $recoveryRoot|Out-Null}
        $quarantineRoot=Join-Path ([IO.Path]::GetFullPath($BackupRoot)) 'fixture-recoveries';if(-not(Test-Path -LiteralPath $quarantineRoot)){New-Item -ItemType Directory -Path $quarantineRoot|Out-Null}
        $runQuarantine=Join-Path $quarantineRoot $RunId;if(Test-Path -LiteralPath $runQuarantine){throw 'Fixture-recovery quarantine already exists.'};New-Item -ItemType Directory -Path $runQuarantine|Out-Null
        $quarantinePath=Join-Path $runQuarantine 'unexpected-KMC_AUTOMATION_WORKING.zks';$replacementQuarantine=Join-Path $runQuarantine 'failed-qualified-replacement.zks'
        $statePath=Join-Path $recoveryRoot ($RunId+'.json')
        $state=[ordered]@{schemaVersion=1;runId=$RunId;token=[string]$lock.Token;phase='prepared';preparedAtUtc=[DateTimeOffset]::UtcNow.ToString('o');saveRoot=[IO.Path]::GetFullPath($SaveRoot);workingPath=$workingPath;unexpectedSha256=[string]$currentWorking.sha256;unexpectedLength=[long]$currentWorking.length;unexpectedLastWriteTimeUtcTicks=[long]$currentWorking.lastWriteTimeUtcTicks;quarantinePath=$quarantinePath;qualifiedBackupPath=[string]$candidate.backup.path;qualifiedBackupSha256=[string]$candidate.backup.sha256;qualifiedBackupAuthorityPath=[string]$candidate.authorityPath;qualifiedBackupAuthoritySha256=[string]$candidate.authoritySha256;expectedWorkingSha256=[string]$expectedWorking.sha256;expectedWorkingLength=[long]$expectedWorking.length;expectedWorkingLastWriteTimeUtcTicks=[long]$expectedWorking.lastWriteTimeUtcTicks}
        Write-KmcJsonCreateNewDurable -Path $statePath -Value $state
        Move-Item -LiteralPath $workingPath -Destination $quarantinePath
        Copy-Item -LiteralPath ([string]$candidate.backup.path) -Destination $workingPath
        (Get-Item -LiteralPath $workingPath).LastWriteTimeUtc=[DateTime]::new([long]$expectedWorking.lastWriteTimeUtcTicks,[DateTimeKind]::Utc)
        $pair=Assert-KmcFixturePair -SaveRoot $SaveRoot -QualificationPath $QualificationPath
        [void](Assert-KmcPermanentFixtureIdentity -Pair $pair -HistoricalAuthorityRecord $HistoricalAuthority)
        $after=Get-KmcQualificationTreeInventory -Root $SaveRoot -Scope save-root
        $changed=@(Get-KmcQualificationInventoryDifferences -Before $before -After $after)
        if($changed.Count-ne1-or[string]$changed[0]-cne[IO.Path]::GetFileName($workingPath)){throw 'Fixture recovery changed a non-Working save.'}
        $state.phase='recovered';$state['recoveredAtUtc']=[DateTimeOffset]::UtcNow.ToString('o');$state['restoredSaveInventoryDigest']=[string]$after.digest;Write-KmcJsonDurable $statePath $state
        Close-KmcRuntimeLock $lock;$lock=$null
        return [pscustomobject]@{status='recovered';path=$workingPath;sha256=[string]$pair.working.sha256;backupRunId=$candidate.runId;statePath=$statePath;quarantinePath=$quarantinePath}
    }catch{
        $failure=$_.Exception.Message
        if($null-ne$lock){
            try{
                if(Test-Path -LiteralPath $workingPath){Move-Item -LiteralPath $workingPath -Destination $replacementQuarantine}
                if($null-ne$quarantinePath-and(Test-Path -LiteralPath $quarantinePath)){Move-Item -LiteralPath $quarantinePath -Destination $workingPath;(Get-Item -LiteralPath $workingPath).LastWriteTimeUtc=[DateTime]::new([long]$currentWorking.lastWriteTimeUtcTicks,[DateTimeKind]::Utc)}
                if($null-ne$statePath-and(Test-Path -LiteralPath $statePath)){ $rollback=Read-KmcJson $statePath;$rollback.phase='rolled-back';$rollback|Add-Member -NotePropertyName rollbackAtUtc -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('o')) -Force;$rollback|Add-Member -NotePropertyName failure -NotePropertyValue $failure -Force;Write-KmcJsonDurable $statePath $rollback }
                Close-KmcRuntimeLock $lock;$lock=$null
            }catch{if($null-ne$lock){try{Abandon-KmcRuntimeLock $lock}catch{}};throw "Fixture recovery failed and rollback could not be proven: $failure / $($_.Exception.Message)"}
        }
        throw "Fixture recovery failed and the unexpected Working was restored: $failure"
    }
}
