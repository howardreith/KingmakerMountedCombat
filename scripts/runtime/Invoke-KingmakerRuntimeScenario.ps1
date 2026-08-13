[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [ValidateSet('mod-load-smoke')][string]$Scenario='mod-load-smoke',
    [ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
    [ValidateRange(30,900)][int]$TimeoutSeconds=180,
    [switch]$SaveAccessAllowed,
    [string]$PackagePath,
    [string]$SteamPath='C:\Program Files (x86)\Steam\steam.exe'
)

$ErrorActionPreference='Stop'; Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$requestedWhatIf=[bool]$WhatIfPreference; $WhatIfPreference=$false
$repoRoot=Get-KmcRepositoryRoot; $labRoot=Get-KmcLabRoot
$intake=Get-Content -Raw -LiteralPath (Join-Path $labRoot 'environment-intake.json')|ConvertFrom-Json
$fingerprintPath=Join-Path $repoRoot 'planning\ENVIRONMENT-FINGERPRINT.json'; $fingerprint=Read-KmcJson $fingerprintPath
$runtimeState=Join-Path $labRoot 'runtime-state'; $runtimeBackups=Join-Path $labRoot 'runtime-backups'; $runtimeStaging=Join-Path $labRoot 'runtime-staging'; $runtimeEvidence=Join-Path $labRoot 'runtime-evidence'
$liveMods=[string]$intake.requestedLayout.kingmakerModsRoot; $saveRoot=[string]$intake.requestedLayout.kingmakerSaveRoot
$gameExecutable=Join-Path ([string]$intake.requestedLayout.kingmakerInstallDir) 'Kingmaker.exe'
$expectedGameExecutableHash=[string](@($fingerprint.kingmaker.files|Where-Object role -eq 'executable')[0].sha256)
if([string]::IsNullOrWhiteSpace($PackagePath)){ $version=Read-KmcJson (Join-Path $repoRoot 'version.json'); $PackagePath=Join-Path (Join-Path $labRoot 'artifacts') ("KingmakerMountedCombat-{0}-diagnostic.zip"-f $version.productVersion) }
$PackagePath=[IO.Path]::GetFullPath($PackagePath); $packageManifestPath=$PackagePath+'.manifest.json'
& (Join-Path $repoRoot 'scripts\Validate-Source.ps1')
& (Join-Path $repoRoot 'scripts\Validate-Package.ps1') -PackagePath $PackagePath
$manifest=Assert-KmcPackageManifest $PackagePath $packageManifestPath
Assert-KmcNoGameProcesses
if(Test-Path -LiteralPath (Join-Path $runtimeState 'active-transaction.lock')){throw 'A stale or active KMC runtime transaction sentinel exists.'}
if($SaveAccessAllowed){throw 'Save-backed runtime remains unavailable until exact KMC_AUTOMATION_BASELINE and KMC_AUTOMATION_WORKING identities are proven.'}
$beforeRoots=@(
    (Get-KmcDirectoryManifest $runtimeState),(Get-KmcDirectoryManifest $runtimeBackups),(Get-KmcDirectoryManifest $runtimeStaging),(Get-KmcDirectoryManifest $runtimeEvidence),(Get-KmcDirectoryManifest $liveMods)
)
$beforeSaves=Get-KmcProtectedSaveMetadata $saveRoot
$WhatIfPreference=$requestedWhatIf
if(-not $PSCmdlet.ShouldProcess('Steam App 640820 and exact live Kingmaker Mods','run guarded KMC mod-load-smoke')){
    $WhatIfPreference=$false
    $afterRoots=@((Get-KmcDirectoryManifest $runtimeState),(Get-KmcDirectoryManifest $runtimeBackups),(Get-KmcDirectoryManifest $runtimeStaging),(Get-KmcDirectoryManifest $runtimeEvidence),(Get-KmcDirectoryManifest $liveMods))
    for($index=0;$index-lt$afterRoots.Count;$index++){if($afterRoots[$index].digest-cne$beforeRoots[$index].digest){throw 'WhatIf purity failed: an external tree changed.'}}
    if((Get-KmcProtectedSaveMetadata $saveRoot).digest-cne$beforeSaves.digest){throw 'WhatIf purity failed: protected save metadata changed.'}
    Assert-KmcNoGameProcesses
    Write-Host 'Runtime WhatIf purity PASS; no evidence, lock, transaction, Mods, process, game, or save mutation occurred.'
    return
}
$WhatIfPreference=$false; $ConfirmPreference='None'
$steamSafety=Assert-KmcSteamSafety $SteamPath
$actualRunId=if([string]::IsNullOrWhiteSpace($RunId)){[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffffffZ')+'-'+$Scenario}else{$RunId}
$evidenceRoot=Assert-KmcChildPath (Join-Path $runtimeEvidence $actualRunId) $runtimeEvidence 'runtime evidence directory'
if(Test-Path -LiteralPath $evidenceRoot){throw "Runtime evidence ID already exists: $actualRunId"}
$startedAt=[DateTimeOffset]::UtcNow; $requestPath=Join-Path $evidenceRoot 'runtime-request.json'; $gameResultPath=Join-Path $evidenceRoot 'runtime-game-result.json'; $finalResultPath=Join-Path $evidenceRoot 'runtime-result.json'; $orchestrationPath=Join-Path $evidenceRoot 'orchestration.json'
$lock=$null; $transactionState=$null; $process=$null; $launchIssued=$false; $processExited=$false; $modsRestored=$false; $saveProtection=$false; $gamePassed=$false; $gameResultHash=$null
$errors=New-Object 'System.Collections.Generic.List[string]'
New-Item -ItemType Directory -Path $evidenceRoot|Out-Null
try{
    $lock=Open-KmcRuntimeLock $runtimeState $actualRunId
    $transactionState=Get-KmcTransactionStatePath $runtimeState $actualRunId
    $request=[ordered]@{schemaVersion=1;runId=$actualRunId;scenario=$Scenario;branch=[string]$manifest.branch;commit=[string]$manifest.commit;productVersion=[string]$manifest.version;dllSha256=[string]$manifest.dllSha256;dllMvid=[string]$manifest.dllMvid;transactionToken=[string]$lock.Token;evidenceRoot=$evidenceRoot;saveAccessAllowed=$false;saveName=$null}
    Write-KmcJsonAtomic $requestPath $request
    & (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeRequest.ps1') -RequestPath $requestPath -PackageManifestPath $packageManifestPath
    [void](Enter-KmcModsTransaction -Lock $lock -LiveModsRoot $liveMods -PackagePath $PackagePath -StateRoot $runtimeState -BackupRoot $runtimeBackups -StagingRoot $runtimeStaging)
    $orchestration=[ordered]@{schemaVersion=1;runId=$actualRunId;scenario=$Scenario;status='IN PROGRESS';stage='request-written';startedAtUtc=$startedAt.ToString('o');steamSafety=$steamSafety;transactionState=$transactionState;protectedSaveDigestBefore=$beforeSaves.digest}
    Write-KmcJsonAtomic $orchestrationPath $orchestration
    Assert-KmcNoGameProcesses
    $arguments=@('-applaunch','640820','-kmcRuntimeRequest',('"'+$requestPath+'"'),'-kmcRuntimeToken',[string]$lock.Token)
    [void](Start-Process -FilePath $SteamPath -ArgumentList $arguments -PassThru); $launchIssued=$true
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
    if(-not $process.Path.Equals([IO.Path]::GetFullPath($gameExecutable),[StringComparison]::OrdinalIgnoreCase)-or (Get-KmcSha256 $process.Path)-cne$expectedGameExecutableHash-or $process.StartTime.ToUniversalTime()-lt$startedAt.UtcDateTime.AddSeconds(-5)){throw 'Captured Kingmaker process identity/path/hash/start time is unexpected.'}
    $orchestration.stage='waiting-for-game-result';$orchestration.kingmakerProcessId=$process.Id;Write-KmcJsonAtomic $orchestrationPath $orchestration
    $deadline=[DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
    while(-not(Test-Path -LiteralPath $gameResultPath -PathType Leaf)){
        $process.Refresh();if($process.HasExited){$processExited=$true;throw 'Kingmaker exited before committing its atomic game result.'}
        $all=@(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue);if($all.Count-ne1-or$all[0].Id-ne$process.Id){throw 'Kingmaker process attribution changed during the run.'}
        if(@(Get-KmcSuspiciousWindows).Count-ne0){throw 'Unexpected Steam/account UI appeared during the run.'}
        if([DateTimeOffset]::UtcNow-ge$deadline){throw 'Runtime game result timed out; Kingmaker is intentionally left running and restoration is blocked.'}
        Start-Sleep -Milliseconds 250
    }
    $gameResultHash=Get-KmcSha256 $gameResultPath
    & (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeGameResult.ps1') -GameResultPath $gameResultPath -RequestPath $requestPath -FingerprintPath $fingerprintPath -ExpectedProcessId $process.Id -NotBeforeUtc $startedAt
    $gamePassed=$true
}
catch{$errors.Add($_.Exception.Message)}
finally{
    if($null-ne$process){
        try{
            $exitDeadline=[DateTimeOffset]::UtcNow.AddSeconds(30)
            do{$process.Refresh();if($process.HasExited){$processExited=$true;break};if(@(Get-KmcSuspiciousWindows).Count-ne0){$errors.Add('Unexpected Steam/account UI appeared during exit wait.');break};Start-Sleep -Milliseconds 250}while([DateTimeOffset]::UtcNow-lt$exitDeadline)
            if(-not$processExited){$errors.Add('Kingmaker did not exit within the bounded grace period; restoration remains blocked.')}
        }catch{$errors.Add('Process exit verification failed: '+$_.Exception.Message)}
    }elseif(-not$launchIssued){$processExited=$true}
    else{$errors.Add('Launch was issued without a captured process; late-launch ambiguity blocks restoration.')}
    if($processExited){
        try{
            $expectedExitedProcessId=if($null-eq$process){0}else{$process.Id}
            if(-not(Wait-KmcStableNoKingmakerProcess -ExpectedProcessId $expectedExitedProcessId)){$processExited=$false;$errors.Add('Kingmaker did not reach a stable no-process state after attributed exit.')}
        }catch{$processExited=$false;$errors.Add('Stable post-exit verification failed: '+$_.Exception.Message)}
    }
    if($null-ne$transactionState-and(Test-Path -LiteralPath $transactionState)-and$processExited){
        try{$restored=Restore-KmcModsTransaction -Lock $lock -StatePath $transactionState -LiveModsRoot $liveMods -BackupRoot $runtimeBackups -StagingRoot $runtimeStaging;$modsRestored=($restored.digest-ceq$beforeRoots[4].digest);if(-not$modsRestored){$errors.Add('Restored Mods digest differs from preflight.')}}catch{$errors.Add('Mods restoration failed: '+$_.Exception.Message)}
    }elseif($processExited){try{$modsRestored=((Get-KmcDirectoryManifest $liveMods).digest-ceq$beforeRoots[4].digest)}catch{$errors.Add('Unmutated Mods verification failed: '+$_.Exception.Message)}}else{$errors.Add('Kingmaker process state is ambiguous; Mods restoration was intentionally not attempted.')}
    if($processExited){try{$saveProtection=((Get-KmcProtectedSaveMetadata $saveRoot).digest-ceq$beforeSaves.digest);if(-not$saveProtection){$errors.Add('Protected save metadata changed during no-save smoke.')}}catch{$errors.Add('Save protection verification failed: '+$_.Exception.Message)}}
    try{if($processExited){[void](Assert-KmcSteamSafety $SteamPath)}}catch{$errors.Add('Steam postflight safety failed: '+$_.Exception.Message)}
    if($null-ne$lock){try{if($modsRestored-and$processExited){Close-KmcRuntimeLock $lock}else{Abandon-KmcRuntimeLock $lock}}catch{$errors.Add('Runtime lock finalization failed: '+$_.Exception.Message)} }
    $status = if ($gamePassed -and $modsRestored -and $saveProtection -and $errors.Count -eq 0) { 'PASS' } else { 'FAIL' }
    $finalToken = if ($null -ne $lock) { [string]$lock.Token } else { '0' * 64 }
    $final=[ordered]@{schemaVersion=1;runId=$actualRunId;scenario=$Scenario;status=$status;branch=[string]$manifest.branch;commit=[string]$manifest.commit;productVersion=[string]$manifest.version;dllSha256=[string]$manifest.dllSha256;dllMvid=[string]$manifest.dllMvid;transactionToken=$finalToken;startedAtUtc=$startedAt.ToString('o');completedAtUtc=[DateTimeOffset]::UtcNow.ToString('o');modsRestored=$modsRestored;saveProtectionPassed=$saveProtection;gameResultSha256=$gameResultHash;errors=@($errors)}
    try{Write-KmcJsonAtomic $finalResultPath $final}catch{Write-Error ('Final runtime evidence write failed: '+$_.Exception.Message)}
    if(Test-Path -LiteralPath $orchestrationPath){try{$orchestration=Read-KmcJson $orchestrationPath;$orchestration.status=$status;$orchestration.stage=$(if($modsRestored){'restored'}else{'restoration-blocked'});$orchestration|Add-Member completedAtUtc ([DateTimeOffset]::UtcNow.ToString('o')) -Force;Write-KmcJsonAtomic $orchestrationPath $orchestration}catch{Write-Error ('Orchestration evidence update failed: '+$_.Exception.Message)}}
}
& (Join-Path $repoRoot 'scripts\runtime\Test-RuntimeResult.ps1') -ResultPath $finalResultPath -RequestPath $requestPath
if((Read-KmcJson $finalResultPath).status-cne'PASS'){throw "Runtime scenario failed. Evidence: $finalResultPath"}
Write-Host "Runtime scenario PASS: $finalResultPath"
