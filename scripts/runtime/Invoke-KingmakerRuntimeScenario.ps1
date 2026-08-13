[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [ValidateSet(
        'mod-load-smoke','export-mounted-contracts','export-candidate-mount-rigs','observe-mount-diagnostic-availability',
        'mounted-pair-create-and-clear','mounted-pair-double-mount-rejected','mounted-pair-invalid-pair-rejected',
        'mounted-pair-cleanup-idempotent','mounted-pair-death-cleanup','mounted-pair-combat-start-cleanup',
        'mounted-pair-area-unload-cleanup','mounted-pair-mod-disable-cleanup','mounted-pair-open-ground',
        'mounted-pair-stop-start','mounted-pair-turns-and-corners','mounted-pair-doorway','mounted-pair-selection',
        'mounted-pair-party-formation','mounted-pair-pause-unpause','mounted-pair-destination-cancel',
        'mounted-pair-turn-based-entry-cleanup','mounted-pair-realtime-entry-cleanup','mounted-pair-save-safety',
        'mounted-pair-load-safety','mounted-pair-area-transition-safety','fixture-intake','lifecycle-suite',
        'movement-suite','boundary-suite','phase-1-runtime-suite'
    )][string]$Scenario='mod-load-smoke',
    [ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
    [ValidateRange(360,900)][int]$TimeoutSeconds=360,
    [switch]$SaveAccessAllowed,
    [string]$PackagePath,
    [string]$SteamPath='C:\Program Files (x86)\Steam\steam.exe'
)

$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$requestedWhatIf=[bool]$WhatIfPreference
$WhatIfPreference=$false
$repoRoot=Get-KmcRepositoryRoot
$labRoot=Get-KmcLabRoot
$intake=Get-Content -Raw -LiteralPath (Join-Path $labRoot 'environment-intake.json')|ConvertFrom-Json
$fingerprintPath=Join-Path $repoRoot 'planning\ENVIRONMENT-FINGERPRINT.json'
$fingerprint=Read-KmcJson $fingerprintPath
$runtimeState=Join-Path $labRoot 'runtime-state'
$runtimeBackups=Join-Path $labRoot 'runtime-backups'
$runtimeStaging=Join-Path $labRoot 'runtime-staging'
$runtimeEvidence=Join-Path $labRoot 'runtime-evidence'
$liveMods=[string]$intake.requestedLayout.kingmakerModsRoot
$saveRoot=[string]$intake.requestedLayout.kingmakerSaveRoot
$gameExecutable=Join-Path ([string]$intake.requestedLayout.kingmakerInstallDir) 'Kingmaker.exe'
$expectedGameExecutableHash=[string](@($fingerprint.kingmaker.files|Where-Object role -eq 'executable')[0].sha256)
$isSaveBacked=[string]$Scenario -cne 'mod-load-smoke'

if($isSaveBacked -and -not $SaveAccessAllowed){
    throw 'A save-backed Phase 1 scenario requires the explicit -SaveAccessAllowed operator gate; it authorizes only the exact qualified Working fixture.'
}
if(-not $isSaveBacked -and $SaveAccessAllowed){
    throw 'The schema-v1 mod-load-smoke scenario is an exact no-save run and rejects -SaveAccessAllowed.'
}
if($isSaveBacked -and @(Get-KmcSaveBackedRuntimeScenarios|Where-Object { $_ -ceq $Scenario }).Count -ne 1){
    throw 'The requested scenario is outside the save-backed Phase 1 allowlist.'
}
if([string]::IsNullOrWhiteSpace($PackagePath)){
    $version=Read-KmcJson (Join-Path $repoRoot 'version.json')
    $PackagePath=Join-Path (Join-Path $labRoot 'artifacts') ("KingmakerMountedCombat-{0}-diagnostic.zip"-f $version.productVersion)
}
$PackagePath=[IO.Path]::GetFullPath($PackagePath)
$packageManifestPath=$PackagePath+'.manifest.json'
& (Join-Path $repoRoot 'scripts\Validate-Source.ps1')
& (Join-Path $repoRoot 'scripts\Validate-Package.ps1') -PackagePath $PackagePath
$manifest=Assert-KmcPackageManifest $PackagePath $packageManifestPath
Assert-KmcNoGameProcesses
if(Test-Path -LiteralPath (Join-Path $runtimeState 'active-transaction.lock')){throw 'A stale or active KMC runtime transaction sentinel exists.'}

# Save-backed preflight reads only the two exact canonical fixtures. It happens
# before ShouldProcess so WhatIf proves descriptor validation without granting a
# load or mutation solely from a filename or scenario name.
$qualificationPath=Assert-KmcChildPath (Join-Path $runtimeState 'fixture-qualification.json') $runtimeState 'fixture qualification'
$preflightPair=$null
$fixturePayload=$null
if($isSaveBacked){
    $preflightPair=Assert-KmcFixturePair -SaveRoot $saveRoot -QualificationPath $qualificationPath
    $fixturePayload=New-KmcRuntimeFixturePayload $preflightPair
}
$beforeRoots=@(
    (Get-KmcDirectoryManifest $runtimeState),(Get-KmcDirectoryManifest $runtimeBackups),
    (Get-KmcDirectoryManifest $runtimeStaging),(Get-KmcDirectoryManifest $runtimeEvidence),
    (Get-KmcDirectoryManifest $liveMods)
)
$beforeSaves=Get-KmcSaveMetadataInventory $saveRoot
$WhatIfPreference=$requestedWhatIf
$action=if($isSaveBacked){"run guarded KMC $Scenario against Working fixture only"}else{'run guarded KMC mod-load-smoke'}
if(-not $PSCmdlet.ShouldProcess('Steam App 640820, exact live Kingmaker Mods, and guarded KMC save policy',$action)){
    $WhatIfPreference=$false
    $afterRoots=@(
        (Get-KmcDirectoryManifest $runtimeState),(Get-KmcDirectoryManifest $runtimeBackups),
        (Get-KmcDirectoryManifest $runtimeStaging),(Get-KmcDirectoryManifest $runtimeEvidence),
        (Get-KmcDirectoryManifest $liveMods)
    )
    for($index=0;$index-lt$afterRoots.Count;$index++){
        if($afterRoots[$index].digest-cne$beforeRoots[$index].digest){throw 'WhatIf purity failed: an external tree changed.'}
    }
    if((Get-KmcSaveMetadataInventory $saveRoot).digest-cne$beforeSaves.digest){throw 'WhatIf purity failed: save metadata changed.'}
    Assert-KmcNoGameProcesses
    Write-Host 'Runtime WhatIf purity PASS; exact fixture descriptors were validated when required, and no evidence, lock, transaction, Mods, process, game, or save mutation occurred.'
    return
}
$WhatIfPreference=$false
$ConfirmPreference='None'
$steamSafety=Assert-KmcSteamSafety $SteamPath
$actualRunId=if([string]::IsNullOrWhiteSpace($RunId)){[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')+'-'+$Scenario}else{$RunId}
$evidenceRoot=Assert-KmcChildPath (Join-Path $runtimeEvidence $actualRunId) $runtimeEvidence 'runtime evidence directory'
if(Test-Path -LiteralPath $evidenceRoot){throw "Runtime evidence ID already exists: $actualRunId"}
$startedAt=[DateTimeOffset]::UtcNow
$requestPath=Join-Path $evidenceRoot 'runtime-request.json'
$gameResultPath=Join-Path $evidenceRoot 'runtime-game-result.json'
$finalResultPath=Join-Path $evidenceRoot 'runtime-result.json'
$orchestrationPath=Join-Path $evidenceRoot 'orchestration.json'
$lock=$null
$request=$null
$combinedStatePath=$null
$process=$null
$launchIssued=$false
$processExited=$false
$modsRestored=$false
$saveProtection=$false
$baselineImmutable=$false
$workingRestored=$false
$saveWriteAllowlistPassed=$false
$restoredSaveInventoryDigest='0'*64
$gamePassed=$false
$validatedGameResult=$null
$gameResultHash=$null
$final=$null
$errors=New-Object 'System.Collections.Generic.List[string]'
New-Item -ItemType Directory -Path $evidenceRoot|Out-Null
try{
    $lock=Open-KmcRuntimeLock $runtimeState $actualRunId
    $request=[ordered]@{
        schemaVersion=$(if($isSaveBacked){2}else{1})
        runId=$actualRunId
        scenario=$Scenario
        branch=[string]$manifest.branch
        commit=[string]$manifest.commit
        productVersion=[string]$manifest.version
        dllSha256=[string]$manifest.dllSha256
        dllMvid=[string]$manifest.dllMvid
        transactionToken=[string]$lock.Token
        evidenceRoot=$evidenceRoot
    }
    if($isSaveBacked){$request['fixture']=$fixturePayload}else{$request['saveAccessAllowed']=$false;$request['saveName']=$null}

    # Re-run the exact fixture qualification while holding the runtime lock, then
    # freeze Working before Mods staging. Enter-KmcWorkingSaveTransaction performs
    # a third immediate descriptor check before its backup becomes authoritative.
    if($isSaveBacked){
        $lockedPair=Assert-KmcFixturePair -SaveRoot $saveRoot -QualificationPath $qualificationPath
        $lockedPayload=New-KmcRuntimeFixturePayload $lockedPair
        if(($lockedPayload|ConvertTo-Json -Depth 10 -Compress)-cne($fixturePayload|ConvertTo-Json -Depth 10 -Compress)){
            throw 'KMC fixture identity changed between preflight and locked transaction entry.'
        }
    }
    $combinedStatePath=New-KmcRunTransactionState -Lock $lock -Mode $(if($isSaveBacked){'save-backed-v2'}else{'no-save-v1'}) -LiveModsRoot $liveMods -SaveRoot $saveRoot -StateRoot $runtimeState -ModsBefore $beforeRoots[4] -SavesBefore $beforeSaves
    if($isSaveBacked){
        [void](Enter-KmcWorkingSaveTransaction -Lock $lock -Pair $lockedPair -SaveRoot $saveRoot -StateRoot $runtimeState -BackupRoot $runtimeBackups -StagingRoot $runtimeStaging)
    }
    [void](Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $liveMods -PackagePath $PackagePath -StateRoot $runtimeState -BackupRoot $runtimeBackups -StagingRoot $runtimeStaging)
    Write-KmcJsonAtomic $requestPath $request
    & (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeRequest.ps1') -RequestPath $requestPath -PackageManifestPath $packageManifestPath
    $orchestration=[ordered]@{
        schemaVersion=2;runId=$actualRunId;scenario=$Scenario;status='IN PROGRESS';stage='transactions-staged';
        startedAtUtc=$startedAt.ToString('o');steamSafety=$steamSafety;combinedTransactionState=$combinedStatePath;
        protectedSaveDigestBefore=$beforeSaves.digest;liveModsDigestBefore=$beforeRoots[4].digest;saveBacked=$isSaveBacked
    }
    Write-KmcJsonAtomic $orchestrationPath $orchestration
    Assert-KmcNoGameProcesses
    $requestHash=Get-KmcSha256 $requestPath
    $arguments=@('-applaunch','640820','-kmcRuntimeRequest',('"'+$requestPath+'"'),'-kmcRuntimeToken',[string]$lock.Token,'-kmcRuntimeRequestSha256',$requestHash)
    [void](Start-Process -FilePath $SteamPath -ArgumentList $arguments -PassThru)
    $launchIssued=$true
    $launchDeadline=[DateTimeOffset]::UtcNow.AddSeconds(60)
    while([DateTimeOffset]::UtcNow-lt$launchDeadline-and$null-eq$process){
        if(@(Get-KmcSuspiciousWindows).Count-ne0){throw 'Unexpected Steam/account UI appeared during launch.'}
        $new=@(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue)
        if($new.Count-gt1){throw 'More than one Kingmaker process appeared.'}
        if($new.Count-eq1){$process=$new[0];break}
        Start-Sleep -Milliseconds 250
    }
    if($null-eq$process){throw 'Steam launch was issued but no uniquely attributable Kingmaker process appeared; restoration is intentionally blocked pending recovery.'}
    $process.Refresh()
    if(-not $process.Path.Equals([IO.Path]::GetFullPath($gameExecutable),[StringComparison]::OrdinalIgnoreCase)-or
        (Get-KmcSha256 $process.Path)-cne$expectedGameExecutableHash-or
        $process.StartTime.ToUniversalTime()-lt$startedAt.UtcDateTime.AddSeconds(-5)){
        throw 'Captured Kingmaker process identity/path/hash/start time is unexpected.'
    }
    $orchestration.stage='waiting-for-game-result'
    $orchestration['kingmakerProcessId']=$process.Id
    Write-KmcJsonAtomic $orchestrationPath $orchestration
    $deadline=[DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
    while(-not(Test-Path -LiteralPath $gameResultPath -PathType Leaf)){
        $process.Refresh()
        if($process.HasExited){$processExited=$true;throw 'Kingmaker exited before committing its atomic game result.'}
        $all=@(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue)
        if($all.Count-ne1-or$all[0].Id-ne$process.Id){throw 'Kingmaker process attribution changed during the run.'}
        if(@(Get-KmcSuspiciousWindows).Count-ne0){throw 'Unexpected Steam/account UI appeared during the run.'}
        if([DateTimeOffset]::UtcNow-ge$deadline){throw 'Runtime game result timed out; Kingmaker is intentionally left running and restoration is blocked.'}
        Start-Sleep -Milliseconds 250
    }
    $candidateHash=Get-KmcSha256 $gameResultPath
    $gameResultHash=$candidateHash
    & (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeGameResult.ps1') -GameResultPath $gameResultPath -RequestPath $requestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $process.Id -NotBeforeUtc $startedAt
    $validatedGameResult=Read-KmcJson $gameResultPath
    $gamePassed=[string]$validatedGameResult.status -ceq 'PASS'
    if(-not$gamePassed){$errors.Add('Game reported FAIL: '+(@($validatedGameResult.errors) -join '; '))}
}
catch{$errors.Add($_.Exception.Message)}
finally{
    if($null-ne$process){
        try{
            $exitDeadline=[DateTimeOffset]::UtcNow.AddSeconds(30)
            do{
                $process.Refresh()
                if($process.HasExited){$processExited=$true;break}
                if(@(Get-KmcSuspiciousWindows).Count-ne0){$errors.Add('Unexpected Steam/account UI appeared during exit wait.');break}
                Start-Sleep -Milliseconds 250
            }while([DateTimeOffset]::UtcNow-lt$exitDeadline)
            if(-not$processExited){$errors.Add('Kingmaker did not exit within the bounded grace period; restoration remains blocked.')}
        }catch{$errors.Add('Process exit verification failed: '+$_.Exception.Message)}
    }elseif(-not$launchIssued){$processExited=$true}
    else{$errors.Add('Launch was issued without a captured process; late-launch ambiguity blocks restoration.')}
    if($processExited){
        try{
            $expectedExitedProcessId=if($null-eq$process){0}else{$process.Id}
            if(-not(Wait-KmcStableNoKingmakerProcess -ExpectedProcessId $expectedExitedProcessId)){
                $processExited=$false
                $errors.Add('Kingmaker did not reach a stable no-process state after attributed exit.')
            }
        }catch{$processExited=$false;$errors.Add('Stable post-exit verification failed: '+$_.Exception.Message)}
    }
    if($null-ne$combinedStatePath-and(Test-Path -LiteralPath $combinedStatePath)-and$processExited){
        try{
            $restoration=Restore-KmcRuntimeTransactions -Lock $lock -CombinedStatePath $combinedStatePath -StateRoot $runtimeState -BackupRoot $runtimeBackups -StagingRoot $runtimeStaging
            $modsRestored=[bool]$restoration.modsRestored
            $saveProtection=[bool]$restoration.saveProtectionPassed
            $baselineImmutable=[bool]$restoration.baselineImmutable
            $workingRestored=[bool]$restoration.workingRestored
            $saveWriteAllowlistPassed=[bool]$restoration.saveWriteAllowlistPassed
            $restoredSaveInventoryDigest=[string]$restoration.restoredSaveInventoryDigest
            foreach($restorationError in @($restoration.errors)){$errors.Add([string]$restorationError)}
        }catch{$errors.Add('Combined external-state restoration failed: '+$_.Exception.Message)}
    }elseif($processExited){
        try{
            $modsRestored=(Get-KmcDirectoryManifest $liveMods).digest-ceq$beforeRoots[4].digest
            $currentSaves=Get-KmcSaveMetadataInventory $saveRoot
            $restoredSaveInventoryDigest=[string]$currentSaves.digest
            $saveProtection=$currentSaves.digest-ceq$beforeSaves.digest
            $baselineImmutable=$saveProtection;$workingRestored=$saveProtection;$saveWriteAllowlistPassed=$saveProtection
            if(-not$modsRestored-or-not$saveProtection){$errors.Add('External state differs after a run that created no combined transaction state.')}
        }catch{$errors.Add('Unmutated external-state verification failed: '+$_.Exception.Message)}
    }else{$errors.Add('Kingmaker process state is ambiguous; external-state restoration was intentionally not attempted.')}
    try{if($processExited){[void](Assert-KmcSteamSafety $SteamPath)}}catch{$errors.Add('Steam postflight safety failed: '+$_.Exception.Message)}
    if($null-ne$lock){
        try{
            if($modsRestored-and$saveProtection-and$processExited){Close-KmcRuntimeLock $lock}else{Abandon-KmcRuntimeLock $lock}
        }catch{$errors.Add('Runtime lock finalization failed: '+$_.Exception.Message)}
    }
    $errorArray=@($errors|ForEach-Object{[string]$_})
    if($null-ne$request){
        if($isSaveBacked){
            $final=New-KmcRuntimeResultV2 -Request $request -ValidatedGameResult $validatedGameResult -StartedAtUtc $startedAt -ModsRestored $modsRestored -BaselineImmutable $baselineImmutable -WorkingRestored $workingRestored -SaveWriteAllowlistPassed $saveWriteAllowlistPassed -RestoredSaveInventoryDigest $restoredSaveInventoryDigest -GameResultSha256 $gameResultHash -Errors $errorArray
        }
        else{
            $status=if($gamePassed-and$modsRestored-and$saveProtection-and$errorArray.Count-eq0){'PASS'}else{'FAIL'}
            $final=[ordered]@{
                schemaVersion=1;runId=$actualRunId;scenario=$Scenario;status=$status;branch=[string]$manifest.branch;
                commit=[string]$manifest.commit;productVersion=[string]$manifest.version;dllSha256=[string]$manifest.dllSha256;
                dllMvid=[string]$manifest.dllMvid;transactionToken=[string]$lock.Token;startedAtUtc=$startedAt.ToString('o');
                completedAtUtc=[DateTimeOffset]::UtcNow.ToString('o');modsRestored=$modsRestored;saveProtectionPassed=$saveProtection;
                gameResultSha256=$gameResultHash;errors=$errorArray
            }
        }
        try{Write-KmcJsonAtomic $finalResultPath $final}catch{Write-Error ('Final runtime evidence write failed: '+$_.Exception.Message)}
    }
    if(Test-Path -LiteralPath $orchestrationPath){
        try{
            $orchestration=Read-KmcJson $orchestrationPath
            $orchestration.status=if($null-ne$final){[string]$final.status}else{'FAIL'}
            $orchestration.stage=if($modsRestored-and$saveProtection){'restored'}else{'restoration-blocked'}
            $orchestration|Add-Member completedAtUtc ([DateTimeOffset]::UtcNow.ToString('o')) -Force
            Write-KmcJsonAtomic $orchestrationPath $orchestration
        }catch{Write-Error ('Orchestration evidence update failed: '+$_.Exception.Message)}
    }
}
if(-not(Test-Path -LiteralPath $requestPath -PathType Leaf)-or-not(Test-Path -LiteralPath $finalResultPath -PathType Leaf)){
    throw "Runtime scenario failed before complete request/result evidence was written. Evidence: $evidenceRoot"
}
& (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeResult.ps1') -ResultPath $finalResultPath -RequestPath $requestPath
if((Read-KmcJson $finalResultPath).status-cne'PASS'){throw "Runtime scenario failed. Evidence: $finalResultPath"}
Write-Host "Runtime scenario PASS: $finalResultPath"
