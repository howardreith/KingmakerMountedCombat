[CmdletBinding()]
param([string]$OutputDirectory)
# Offline checks of the F12 upgrade/rollback rehearsal (7E X5, Option A): the exact
# previous-package pin, the carried first-install folder state, the request section,
# the guarded ready-tree carry, the previous-package load request read by the observer's
# actual C# request type, the closed reader of its result, and the runner's admission.
# No game, lock, transaction or live Mods tree is touched.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('upgrade-rehearsal-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'upgrade rehearsal checks'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior upgrade rehearsal checks.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$results=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false,[string]$Pattern=''){
 if($Refuse-and-not$Pattern){throw ('Refusal check lacks its expected message: '+$Name)}
 $errorText=$null;$ok=$false
 try{&$Body;$ok=-not$Refuse}catch{$errorText=$_.Exception.Message;$ok=$Refuse-and$errorText-cmatch$Pattern}
 $results.Add([pscustomobject]@{name=$Name;pass=$ok;error=$errorText})
 Write-Output ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if(-not$ok){': '+$errorText}else{''}))
}
function Clone($Value){$Value|ConvertTo-Json -Depth 60|ConvertFrom-Json}
$realLab=Get-KmcLabRoot;$originalLabRoot=${function:Get-KmcLabRoot}
function Use-Lab([string]$Root){Set-Item -Path function:Get-KmcLabRoot -Value ([scriptblock]::Create("'"+$Root+"'"))}
function Restore-Lab{Set-Item -Path function:Get-KmcLabRoot -Value $originalLabRoot}
$pins=Get-KmcChunk7PreviousPackagePins

# --- Previous package: exact released preview.211 "-b" identity ----------------
Check 'the released preview.211 package qualifies with its exact identities' {
 $p=Get-KmcChunk7PreviousPackage
 if($p.packageSha256-cne'e89f431ae1303b217e987b2ff12171a05bac434d97809962c3596e2942874039'-or$p.manifestSha256-cne'ca54c37cc5de332bad4514d04b585ccf5b31977a57f44c6f0e19288a769d8ec3'-or
  $p.dllSha256-cne'11e993093d2042f615cfc9d6aecc33d630d2e41e8125dd680656b6a1057a48ea'-or$p.dllMvid-cne'0aa68ba0-f293-480e-8ecb-d7bdaeca73d0'-or$p.version-cne'0.1.0-chunk6d-preview.211'){throw 'Previous identity differs.'}
}
$fakeLab=Join-Path $OutputDirectory 'lab';$fakeArtifacts=Join-Path $fakeLab 'artifacts';[void][IO.Directory]::CreateDirectory($fakeArtifacts)
$realPackage=Join-Path $realLab ('artifacts/'+$pins.leaf)
function Stage-Package([scriptblock]$Mutate){
 Get-ChildItem -LiteralPath $fakeArtifacts|Remove-Item -Force
 Copy-Item -LiteralPath $realPackage -Destination (Join-Path $fakeArtifacts $pins.leaf);Copy-Item -LiteralPath ($realPackage+'.manifest.json') -Destination (Join-Path $fakeArtifacts ($pins.leaf+'.manifest.json'))
 if($Mutate){&$Mutate (Join-Path $fakeArtifacts $pins.leaf)}
}
Check 'an exact copy of the previous package also qualifies' {Stage-Package $null;Use-Lab $fakeLab;try{[void](Get-KmcChunk7PreviousPackage)}finally{Restore-Lab}}
Check 'the other closeout package cannot stand in for the release' {Stage-Package {param($p)Copy-Item -LiteralPath (Join-Path $realLab 'artifacts/KingmakerMountedCombat-0.1.0-chunk6d-preview.211-chunk6-closeout211-a-diagnostic.zip') -Destination $p -Force};Use-Lab $fakeLab;try{[void](Get-KmcChunk7PreviousPackage)}finally{Restore-Lab}} $true 'not the released preview.211 identity'
Check 'a changed previous manifest is refused' {Stage-Package {param($p)[IO.File]::AppendAllText($p+'.manifest.json',' ')};Use-Lab $fakeLab;try{[void](Get-KmcChunk7PreviousPackage)}finally{Restore-Lab}} $true 'not the released preview.211 identity'
Check 'a missing previous package is refused' {Stage-Package {param($p)Remove-Item -LiteralPath $p -Force};Use-Lab $fakeLab;try{[void](Get-KmcChunk7PreviousPackage)}finally{Restore-Lab}} $true 'previous package is missing'

$previous=Get-KmcChunk7PreviousPackage
$manifest=[pscustomobject]@{commit=('a'*40);version='0.1.0-chunk7e-final';dllSha256=('d'*64);dllMvid='57fc82b9-f41e-44b4-b228-1315816108a3';packageSha256=('e'*64)}

# --- Carry: the passed u1 run's classified KMC-folder runtime files --------------
# UMM's cache is a File.Copy of the loaded DLL, so a carried u1 cache holds preview.211's DLL bytes.
Add-Type -AssemblyName System.IO.Compression.FileSystem;$zipFile=[IO.Compression.ZipFile]::OpenRead($realPackage)
try{$dllEntry=@($zipFile.Entries|Where-Object{$_.FullName.Replace('\','/')-ceq'KingmakerMountedCombat/KingmakerMountedCombat.dll'})[0];$dllBytes=New-Object byte[] $dllEntry.Length;$in=$dllEntry.Open();try{$read=0;while($read-lt$dllBytes.Length){$read+=$in.Read($dllBytes,$read,$dllBytes.Length-$read)}}finally{$in.Dispose()}}finally{$zipFile.Dispose()}
$rid='f12-a';$u1='20261024-c7e-f12-u1-synthetic';$staging=Join-Path $fakeLab ('runtime-staging/'+$u1);$after=Join-Path $staging 'Mods-staged-after/KingmakerMountedCombat'
# u1's real b199 KMC lines as its UMM receipt, admitted for u1 and named for the synthetic run.
$carryUmmGame=Join-Path $OutputDirectory 'carry-umm';$carryUmmDir=Join-Path $carryUmmGame 'Kingmaker_Data/Managed/UnityModManager';[void][IO.Directory]::CreateDirectory($carryUmmDir)
[IO.File]::WriteAllText((Join-Path $carryUmmDir 'Log.txt'),((@('[Manager] Initialize.','[Manager] Version: 0.33.2.0.')+@((Read-KmcJson (Join-Path $realLab 'runtime-evidence/20261024-c7e-b199-x5-u1-previous/normal-player-umm-log.json')).firstKmcLines|ForEach-Object{[string]$_}))-join"`r`n"),[Text.UTF8Encoding]::new($false))
$carryReceipt=Get-KmcChunk7NormalPlayerUmmLogSummary $carryUmmGame ([DateTimeOffset]::UtcNow.AddMinutes(-5)) $pins.version @(Get-KmcChunk7PreviousPackageKnownDefects) $u1
function Stage-Carry([scriptblock]$Mutate,[scriptblock]$PostWrite){
 foreach($d in @('runtime-evidence','runtime-state','runtime-staging')){$x=Join-Path $fakeLab $d;if(Test-Path -LiteralPath $x){Remove-Item -LiteralPath $x -Recurse -Force}}
 [void][IO.Directory]::CreateDirectory($after);[void][IO.Directory]::CreateDirectory((Join-Path $fakeLab ('runtime-evidence/'+$u1)));[void][IO.Directory]::CreateDirectory((Join-Path $fakeLab 'runtime-state/transactions'))
 $cache=Join-Path $after 'KingmakerMountedCombat.dll.4242.cache';[IO.File]::WriteAllBytes($cache,$dllBytes)
 $f=[pscustomobject]@{
  request=[pscustomobject]@{runId=$u1;scenario='chunk7-normal-player-control';normalPlayer=[pscustomobject]@{upgradeRehearsal=[pscustomobject]@{rehearsalId=$rid;step='u1-previous-install';stagedKmc=[pscustomobject]@{role='previous';version=$pins.version;commit=$pins.commit;packageSha256=$pins.packageSha256;manifestSha256=$pins.manifestSha256;dllSha256=$pins.dllSha256;dllMvid=$pins.dllMvid}}}}
  result=[pscustomobject]@{runId=$u1;status='PASS';gameResultSha256=$null;normalPlayerUmmReceiptSha256=$null}
  receipt=(Clone $carryReceipt)
  game=[pscustomobject]@{evidenceKind='kmc-normal-player-observer';status='PASS';runId=$u1;observerResultSha256=$null
   upgradeRehearsal=[pscustomobject]@{rehearsalId=$rid;step='u1-previous-install';stagedKmc=[pscustomobject]@{packageSha256=$pins.packageSha256;dllSha256=$pins.dllSha256};loadedKmc=[pscustomobject]@{dllSha256=$pins.dllSha256;dllMvid=$pins.dllMvid}}}
  observer=[pscustomobject]@{status='PASS';runId=$u1;observations=[pscustomobject]@{presenceLoaded=[pscustomobject]@{kmcDllSha256=$pins.dllSha256;kmcMvid=$pins.dllMvid}}}
  state=[pscustomobject]@{schemaVersion=4;runId=$u1;phase='restored';stagingMode='base-game-only-kmc-observer';packageSha256=$pins.packageSha256;stagedAfter=(Join-Path $staging 'Mods-staged-after')
   runtimeWritesClassified=$true;runtimeUnexpectedWrites=@()
   runtimeExpectedWrites=@([pscustomobject]@{change='added';kind='file';path='KingmakerMountedCombat/KingmakerMountedCombat.dll.4242.cache';runtimeSha256=(Get-KmcSha256 $cache)},
    [pscustomobject]@{change='added';kind='file';path='KmcRemovalObserver/KmcRemovalObserver.dll.77.cache';runtimeSha256=('1'*64)})}}
 if($Mutate){&$Mutate $f}
 Write-KmcJsonAtomic (Join-Path $fakeLab ('runtime-evidence/'+$u1+'/runtime-request.json')) $f.request
 $ev=Join-Path $fakeLab ('runtime-evidence/'+$u1)
 Write-KmcJsonAtomic (Join-Path $ev 'observer-result.json') $f.observer
 $f.game.observerResultSha256=Get-KmcSha256 (Join-Path $ev 'observer-result.json');Write-KmcJsonAtomic (Join-Path $ev 'runtime-game-result.json') $f.game
 Write-KmcJsonAtomic (Join-Path $ev 'normal-player-umm-log.json') $f.receipt;$f.result.normalPlayerUmmReceiptSha256=Get-KmcSha256 (Join-Path $ev 'normal-player-umm-log.json')
 $f.result.gameResultSha256=Get-KmcSha256 (Join-Path $ev 'runtime-game-result.json');Write-KmcJsonAtomic (Join-Path $ev 'runtime-result.json') $f.result
 Write-KmcJsonAtomic (Join-Path $fakeLab ('runtime-state/transactions/'+$u1+'.json')) $f.state
 if($PostWrite){&$PostWrite $ev}
}
function Read-CarrySource([string]$Id=$rid){Use-Lab $fakeLab;try{Get-KmcChunk7UpgradeCarrySource $u1 $Id}finally{Restore-Lab}}
function In-Lab([scriptblock]$Body){Use-Lab $fakeLab;try{&$Body}finally{Restore-Lab}}
function Read-Carry([string]$Id=$rid){Use-Lab $fakeLab;try{Get-KmcChunk7UpgradeCarry $u1 $Id}finally{Restore-Lab}}
Check 'the passed u1 run carries exactly its classified UMM cache, flat and with its real length' {Stage-Carry $null;$c=Read-Carry
 if($c-isnot[Array]-or$c.Count-ne1-or$c[0]-is[Array]-or$c[0].name-cne'KingmakerMountedCombat.dll.4242.cache'-or$c[0].sha256-cne$pins.dllSha256-or[long]$c[0].length-ne$dllBytes.Length){throw 'Carry differs.'}}
foreach($m in @(
 @('a failed u1 run',{param($f)$f.result.status='FAIL'},'not a passed u1'),
 @('a non-u1 source step',{param($f)$f.request.normalPlayer.upgradeRehearsal.step='u2-upgrade'},'not a passed u1'),
 @('another staged package',{param($f)$f.request.normalPlayer.upgradeRehearsal.stagedKmc.packageSha256=('0'*64)},'not a passed u1'),
 @('an unrestored transaction',{param($f)$f.state.phase='staged'},'not the restored'),
 @('an unexpected runtime write',{param($f)$f.state.runtimeUnexpectedWrites=@([pscustomobject]@{path='x'})},'not the restored'),
 @('a KMC settings write outside the cache shape',{param($f)$f.state.runtimeExpectedWrites+=[pscustomobject]@{change='added';kind='file';path='KingmakerMountedCombat/Settings.xml';runtimeSha256=('2'*64)}},'outside the closed UMM cache shape'),
 @('a carried cache that is not the previous DLL',{param($f)$f.state.runtimeExpectedWrites[0].runtimeSha256=('3'*64)},'not a byte copy of the previous package DLL'),
 @('carried bytes that changed after classification',{param($f)[IO.File]::WriteAllText((Join-Path $after 'KingmakerMountedCombat.dll.4242.cache'),'changed')},'differs from its classified runtime write'),
 @('a game record of another step',{param($f)$f.game.upgradeRehearsal.step='u2-upgrade'},'game record does not bind a passed u1'),
 @('a game record that saw the candidate loaded',{param($f)$f.game.upgradeRehearsal.loadedKmc.dllSha256=('d'*64)},'game record does not bind a passed u1'),
 @('a failed game record',{param($f)$f.game.status='FAIL'},'game record does not bind a passed u1'),
 @('a game record of another rehearsal id',{param($f)$f.game.upgradeRehearsal.rehearsalId='f12-other'},'game record does not bind a passed u1'),
 @('a game record of another run',{param($f)$f.game.runId='another-run'},'game record does not bind a passed u1'),
 @('an observer result of another run',{param($f)$f.observer.runId='another-run'},'observer did not see exactly preview.211'),
 @('an observer that saw another assembly',{param($f)$f.observer.observations.presenceLoaded.kmcMvid='00000000-0000-0000-0000-000000000000'},'observer did not see exactly preview.211'),
 @('a failed observer result',{param($f)$f.observer.status='FAIL'},'observer did not see exactly preview.211'),
 @('a u1 of another rehearsal',{param($f)$f.request.normalPlayer.upgradeRehearsal.rehearsalId='f12-other'},'not a passed u1'),
 @('a u1 receipt that hides its admitted defect',{param($f)$f.receipt.admittedOldVersionKnownDefects=@()},'admits old-version defects other than'),
 @('a u1 receipt with an extra error line',{param($f)$f.receipt.errorLines=2;$f.receipt.errorLineTexts=@(@($f.receipt.errorLineTexts)+'[KingmakerMountedCombat] [Error] x')},'not an ordinary quiet no-request start'),
 @('a u1 receipt of another run',{param($f)$f.receipt.runId='another-run'},'does not name this run'),
 @('no carried KMC file',{param($f)$f.state.runtimeExpectedWrites=@($f.state.runtimeExpectedWrites[1])},'holds no, or an unbounded set'))){
 Check ('carry refuses '+$m[0]) {Stage-Carry $m[1];[void](Read-Carry)} $true $m[2]
}
Check 'carry refuses a game record that no longer matches the outer result' {Stage-Carry $null {param($ev)[IO.File]::AppendAllText((Join-Path $ev 'runtime-game-result.json'),' ')};[void](Read-CarrySource)} $true 'game record does not bind a passed u1'
Check 'carry refuses an observer result that no longer matches the game record' {Stage-Carry $null {param($ev)[IO.File]::AppendAllText((Join-Path $ev 'observer-result.json'),' ')};[void](Read-CarrySource)} $true 'game record does not bind a passed u1'
Check 'carry refuses a u1 receipt changed after its hash was recorded' {Stage-Carry $null {param($ev)[IO.File]::AppendAllText((Join-Path $ev 'normal-player-umm-log.json'),' ')};[void](Read-CarrySource)} $true 'does not match the SHA-256 its outer result records'
Check 'carry refuses a u1 result that records no receipt hash' {Stage-Carry $null {param($ev)$p=Join-Path $ev 'runtime-result.json';$x=Read-KmcJson $p;$x.PSObject.Properties.Remove('normalPlayerUmmReceiptSha256');Write-KmcJsonAtomic $p $x};[void](Read-CarrySource)} $true 'does not record its UMM session receipt SHA-256'
Check 'carry refuses a missing u1 game record' {Stage-Carry $null {param($ev)Remove-Item -LiteralPath (Join-Path $ev 'runtime-game-result.json')};[void](Read-CarrySource)} $true 'record is missing'
Check 'carry refuses a staged-after tree outside the lab staging root' {Stage-Carry {param($f)$f.state.stagedAfter='C:\Windows\Temp\elsewhere'};[void](Read-Carry)} $true 'staged-after tree'
Check 'carry refuses an invalid rehearsal id' {Stage-Carry $null;[void](Read-Carry 'bad id!')} $true 'rehearsal id is invalid'
Stage-Carry $null;$carry=Read-Carry;$carrySource=Read-CarrySource
Check 'the carry source pins four hashes: result, game record, observer result and state' {if($carrySource.runId-cne$u1-or@('resultSha256','gameResultSha256','observerResultSha256','stateSha256'|Where-Object{[string]$carrySource.$_-cnotmatch'^[0-9a-f]{64}$'}).Count-ne0){throw 'Carry source pins differ.'}}
Check 'the section refuses a nested carry list instead of flattening it' {[void](New-KmcChunk7UpgradeRehearsalSection 'u2-upgrade' $rid $previous $manifest $carrySource @(,@($carry)))} $true 'not a flat list'

# --- Request section ------------------------------------------------------------

$baseRequest=[pscustomobject]@{runId='20261024-c7e-f12-section';commit=$manifest.commit;productVersion=$manifest.version;dllSha256=$manifest.dllSha256;dllMvid=$manifest.dllMvid}
function Section-Request([string]$Step){$r=Clone $baseRequest;$s=New-KmcChunk7UpgradeRehearsalSection $Step $rid $previous $manifest $(if($Step-ceq'u1-previous-install'){$null}else{$carrySource}) $(if($Step-ceq'u1-previous-install'){@()}else{$carry})
 $r|Add-Member normalPlayer ([pscustomobject]@{upgradeRehearsal=(Clone ([pscustomobject]$s))});$r}
foreach($step in @(Get-KmcChunk7UpgradeRehearsalSteps)){Check ('the '+$step+' section is closed and exact') {In-Lab {Assert-KmcChunk7UpgradeRehearsalSection (Section-Request $step)}}}
foreach($m in @(
 @('the outer result',{param($ev)[IO.File]::AppendAllText((Join-Path $ev 'runtime-result.json'),' ')},'no longer matches: resultSha256'),
 @('the game record',{param($ev)[IO.File]::AppendAllText((Join-Path $ev 'runtime-game-result.json'),' ')},'game record does not bind a passed u1'),
 @('the observer result',{param($ev)[IO.File]::AppendAllText((Join-Path $ev 'observer-result.json'),' ')},'game record does not bind a passed u1'),
 @('the transaction state',{param($ev)[IO.File]::AppendAllText((Join-Path (Join-Path $fakeLab 'runtime-state/transactions') ($u1+'.json')),' ')},'no longer matches: stateSha256'))){
 Check ('the section re-derives the proof and refuses '+$m[0]+' changed after pinning') {$r=Section-Request 'u3-rollback';Stage-Carry $null $m[1];try{In-Lab {Assert-KmcChunk7UpgradeRehearsalSection $r}}finally{Stage-Carry $null}} $true $m[2]
}
Check 'the section refuses a hand-pinned FAIL u1 even when every pin matches its files' {
 Stage-Carry {param($f)$f.result.status='FAIL'}
 try{$paths=In-Lab {Get-KmcChunk7UpgradeCarryRecordPaths $u1}
  $r=Section-Request 'u2-upgrade';$c=$r.normalPlayer.upgradeRehearsal.carrySource
  $c.resultSha256=Get-KmcSha256 $paths.result;$c.gameResultSha256=Get-KmcSha256 $paths.game;$c.observerResultSha256=Get-KmcSha256 $paths.observer;$c.stateSha256=Get-KmcSha256 $paths.state
  In-Lab {Assert-KmcChunk7UpgradeRehearsalSection $r}}finally{Stage-Carry $null}} $true 'not a passed u1'
Check 'the section refuses a carried cache name the pinned u1 never wrote' {$r=Section-Request 'u2-upgrade';$r.normalPlayer.upgradeRehearsal.carriedFiles[0].name='KingmakerMountedCombat.dll.999.cache';In-Lab {Assert-KmcChunk7UpgradeRehearsalSection $r}} $true 'not exactly the files the pinned u1 wrote'
Check 'the section refuses a carried length the pinned u1 did not write' {$r=Section-Request 'u3-rollback';$r.normalPlayer.upgradeRehearsal.carriedFiles[0].length=1;In-Lab {Assert-KmcChunk7UpgradeRehearsalSection $r}} $true 'not exactly the files the pinned u1 wrote'
Check 'the section refuses a carry source naming another run' {$r=Section-Request 'u2-upgrade';$r.normalPlayer.upgradeRehearsal.carrySource.runId='other-run';In-Lab {Assert-KmcChunk7UpgradeRehearsalSection $r}} $true 'exact bounded u1 folder state'
Check 'the section refuses a first install with a carry source' {$r=Section-Request 'u1-previous-install';$r.normalPlayer.upgradeRehearsal.carrySource=[pscustomobject]@{runId=$u1};In-Lab {Assert-KmcChunk7UpgradeRehearsalSection $r}} $true 'carries nothing'
foreach($m in @(
 @('a first install that carries files',{$r=Section-Request 'u1-previous-install';$r.normalPlayer.upgradeRehearsal.carriedFiles=@([pscustomobject]@{name='KingmakerMountedCombat.dll.1.cache';length=1;sha256=('4'*64)});$r},'carries nothing'),
 @('an upgrade without carried state',{$r=Section-Request 'u2-upgrade';$r.normalPlayer.upgradeRehearsal.carriedFiles=@();$r},'exact bounded u1 folder state'),
 @('a rollback staging the candidate',{$r=Section-Request 'u3-rollback';$r.normalPlayer.upgradeRehearsal.stagedKmc.dllSha256=$manifest.dllSha256;$r},'exactly the released preview.211'),
 @('an upgrade staging another DLL',{$r=Section-Request 'u2-upgrade';$r.normalPlayer.upgradeRehearsal.stagedKmc.dllSha256=('5'*64);$r},'exactly the run candidate'),
 @('a carried settings file',{$r=Section-Request 'u2-upgrade';$r.normalPlayer.upgradeRehearsal.carriedFiles[0].name='Settings.xml';$r},'closed UMM cache shape'),
 @('a self-referencing carry source',{$r=Section-Request 'u3-rollback';$r.normalPlayer.upgradeRehearsal.carrySourceRunId=$r.runId;$r},'exact bounded u1 folder state'),
 @('an unknown step',{$r=Section-Request 'u2-upgrade';$r.normalPlayer.upgradeRehearsal.step='u4-other';$r},'contract or step differs'),
 @('a carried cache that is not preview.211',{$r=Section-Request 'u2-upgrade';$r.normalPlayer.upgradeRehearsal.carriedFiles[0].sha256=('7'*64);$r},'closed UMM cache shape'),
 @('an invalid rehearsal id',{$r=Section-Request 'u3-rollback';$r.normalPlayer.upgradeRehearsal.rehearsalId='';$r},'rehearsal id is invalid'),
 @('an extra section member',{$r=Section-Request 'u2-upgrade';$r.normalPlayer.upgradeRehearsal|Add-Member extra 1;$r},'upgrade rehearsal section'))){
 Check ('the section refuses '+$m[0]) {$r=&$m[1];In-Lab {Assert-KmcChunk7UpgradeRehearsalSection $r}} $true $m[2]
}

# --- Ready tree: carried files are laid exactly under the package, never alone ---
$runRoot=Join-Path $OutputDirectory 'stage'
function New-StageRun([string]$Name){
 $run=Join-Path $runRoot $Name;$clone=Join-Path $run 'Mods-live-clone';[void][IO.Directory]::CreateDirectory((Join-Path $clone 'OtherMod'));[IO.File]::WriteAllText((Join-Path $clone 'OtherMod/Info.json'),'{}')
 $pkg=Join-Path $run 'package-overlay/KingmakerMountedCombat';[void][IO.Directory]::CreateDirectory($pkg);[IO.File]::WriteAllText((Join-Path $pkg 'Info.json'),'{"Id":"KingmakerMountedCombat"}');[IO.File]::WriteAllText((Join-Path $pkg 'KingmakerMountedCombat.dll'),'candidate dll')
 $obs=Join-Path $run 'observer-overlay/KmcRemovalObserver';[void][IO.Directory]::CreateDirectory($obs);[IO.File]::WriteAllText((Join-Path $obs 'Info.json'),'{"Id":"KmcRemovalObserver"}');[IO.File]::WriteAllText((Join-Path $obs 'KmcRemovalObserver.dll'),'observer dll')
 [pscustomobject]@{run=$run;clone=$clone;package=$pkg;observer=$obs;ready=(Join-Path $run 'Mods-ready');before=(Get-KmcDirectoryManifest $clone)}
}
$i=0
function Build([object[]]$Files,[bool]$WithObserver=$true){$script:i++;$s=New-StageRun ('r'+$script:i)
 Use-Lab $fakeLab
 try{New-KmcBaseGameReadyTree -StagingRun $s.run -CloneRoot $s.clone -ReadyRoot $s.ready -PackageModRoot $s.package -Before $s.before -ObserverModRoot $(if($WithObserver){$s.observer}else{$null}) -KmcCarryFiles $Files -Confirm:$false}finally{Restore-Lab};$s}
Check 'two carried caches pass through the runner''s assignment shape, the section and the ready tree with their real lengths' {
 Stage-Carry {param($f)[IO.File]::WriteAllBytes((Join-Path $after 'KingmakerMountedCombat.dll.17.cache'),$dllBytes);$f.state.runtimeExpectedWrites+=[pscustomobject]@{change='added';kind='file';path='KingmakerMountedCombat/KingmakerMountedCombat.dll.17.cache';runtimeSha256=$pins.dllSha256}}
 Use-Lab $fakeLab;try{$two=Get-KmcChunk7UpgradeCarry $u1 $rid;$twoSource=Get-KmcChunk7UpgradeCarrySource $u1 $rid}finally{Restore-Lab}
 $files=@((New-KmcChunk7UpgradeRehearsalSection 'u2-upgrade' $rid $previous $manifest $twoSource $two).carriedFiles)
 if($two.Count-ne2-or$files.Count-ne2-or@($files|Where-Object{[long]$_.length-ne$dllBytes.Length}).Count-ne0){throw ('Two-file carry shape differs: '+$two.Count+'/'+$files.Count)}
 $s2=Build $two
 if(@(Get-ChildItem -LiteralPath (Join-Path $s2.ready 'KingmakerMountedCombat') -Filter '*.cache').Count-ne2){throw 'Ready tree did not receive both carried caches.'}
 Stage-Carry $null}
Check 'the ready tree refuses a carry source outside the lab staging root' {$x=Clone $carry[0];$out=Join-Path $OutputDirectory 'loose.cache';[IO.File]::WriteAllBytes($out,$dllBytes);$x.sourcePath=$out;[void](Build @($x))} $true 'carried KMC folder file'
Check 'the ready tree lays the carried cache beside the exact package files' {$s=Build $carry
 $names=@(Get-ChildItem -LiteralPath (Join-Path $s.ready 'KingmakerMountedCombat')|ForEach-Object Name|Sort-Object)
 if(($names-join'|')-cne'Info.json|KingmakerMountedCombat.dll|KingmakerMountedCombat.dll.4242.cache'-or(Get-KmcSha256 (Join-Path $s.ready 'KingmakerMountedCombat/KingmakerMountedCombat.dll.4242.cache'))-cne$carry[0].sha256){throw ('Ready KMC folder differs: '+($names-join','))}}
Check 'the ready tree without carry is unchanged' {$s=Build @();if(@(Get-ChildItem -LiteralPath (Join-Path $s.ready 'KingmakerMountedCombat')).Count-ne2){throw 'Unexpected KMC folder content.'}}
Check 'the ready tree refuses carried files without the observer co-staging' {[void](Build $carry $false)} $true 'Only the observer co-staged tree'
Check 'the ready tree refuses a carried file outside the cache shape' {$x=Clone $carry[0];$x.name='Settings.xml';[void](Build @($x))} $true 'closed UMM cache shape'
Check 'the ready tree refuses a carried file that changed' {$x=Clone $carry[0];$x.sha256=('6'*64);[void](Build @($x))} $true 'changed before staging'
Check 'the ready tree refuses a declared length that is not the real length' {$x=Clone $carry[0];$x.length=1;[void](Build @($x))} $true 'changed while staging'
Check 'the ready tree refuses a repeated carried file' {[void](Build @($carry[0],$carry[0]))} $true 'collides'
Check 'the ready tree refuses an unbounded carry' {[void](Build @($carry[0],$carry[0],$carry[0],$carry[0],$carry[0]))} $true 'unbounded'
# Behavioral: with the lock and process guards stubbed, the transaction refuses carry in
# every mode but the co-staging one before it creates any state, backup or staging path.
Check 'the Mods transaction refuses carry outside the co-staging mode before creating anything' {
 $roots=Join-Path $OutputDirectory 'tx';[void][IO.Directory]::CreateDirectory($roots)
 $lockGuard=${function:Assert-KmcRuntimeLockOwner};$processGuard=${function:Assert-KmcNoGameProcesses}
 Set-Item function:Assert-KmcRuntimeLockOwner {param($Lock)};Set-Item function:Assert-KmcNoGameProcesses {}
 try{
  $refused=$null
  try{[void](Enter-KmcModsTransaction -Lock ([pscustomobject]@{RunId='tx-run';Token=('a'*64)}) -LiveModsRoot (Join-Path $roots 'live') -PackagePath (Join-Path $roots 'p.zip') -StateRoot $roots -BackupRoot $roots -StagingRoot $roots -KmcCarryFiles $carry)}catch{$refused=$_.Exception.Message}
  if($refused-notmatch'Only the normal-player co-staging mode may carry'){throw ('Carry was not refused first: '+$refused)}
  if(@(Get-ChildItem -LiteralPath $roots -Force).Count-ne0){throw 'The refused transaction created state, backup or staging paths.'}
 }finally{Set-Item function:Assert-KmcRuntimeLockOwner $lockGuard;Set-Item function:Assert-KmcNoGameProcesses $processGuard}
}
$cacheName='KingmakerMountedCombat/'+$carry[0].name
$stagedRows=@([pscustomobject]@{kind='file';path='KingmakerMountedCombat/Info.json';length=10;sha256=('1'*64)},[pscustomobject]@{kind='file';path='KingmakerMountedCombat/KingmakerMountedCombat.dll';length=20;sha256=('2'*64)},
 [pscustomobject]@{kind='file';path=$cacheName;length=[long]$carry[0].length;sha256=[string]$carry[0].sha256})
function Runtime([object[]]$Rows){[pscustomobject]@{entries=$Rows}}
$newCache=[pscustomobject]@{kind='file';path='KingmakerMountedCombat/KingmakerMountedCombat.dll.999.cache';length=20;sha256=('2'*64)}
Check 'restoration names UMM''s removal of exactly the declared carried cache and admits the new candidate cache' {
 $c=Get-KmcModsRuntimeWriteClassification -StagedEntries $stagedRows -RuntimeManifest (Runtime @($stagedRows[0],$stagedRows[1],$newCache)) -CarriedFiles $carry
 if(@($c.unexpected).Count-ne0-or((@($c.expected)|ForEach-Object{$_.change+':'+$_.path}|Sort-Object)-join'|')-cne('added:KingmakerMountedCombat/KingmakerMountedCombat.dll.999.cache|removed-carried-cache:'+$cacheName)){throw ($c|ConvertTo-Json -Depth 4 -Compress)}}
Check 'restoration keeps an undeclared removal of the carried cache unexpected' {
 $c=Get-KmcModsRuntimeWriteClassification -StagedEntries $stagedRows -RuntimeManifest (Runtime @($stagedRows[0],$stagedRows[1],$newCache))
 if(@($c.unexpected|Where-Object{$_.change-ceq'removed'-and$_.path-ceq$cacheName}).Count-ne1){throw 'Undeclared removal was admitted.'}}
Check 'restoration keeps a declared name with other bytes unexpected' {
 $x=Clone $carry[0];$x.sha256=('9'*64)
 $c=Get-KmcModsRuntimeWriteClassification -StagedEntries $stagedRows -RuntimeManifest (Runtime @($stagedRows[0],$stagedRows[1])) -CarriedFiles @($x)
 if(@($c.unexpected|Where-Object{$_.change-ceq'removed'}).Count-ne1){throw 'Mismatched declared removal was admitted.'}}
Check 'restoration never admits removal of a package file even when carry is declared' {
 $c=Get-KmcModsRuntimeWriteClassification -StagedEntries $stagedRows -RuntimeManifest (Runtime @($stagedRows[1],$stagedRows[2])) -CarriedFiles $carry
 if(@($c.unexpected|Where-Object{$_.change-ceq'removed'-and$_.path-ceq'KingmakerMountedCombat/Info.json'}).Count-ne1){throw 'A package removal was admitted.'}}
Check 'restoration still refuses a changed carried cache' {
 $c=Get-KmcModsRuntimeWriteClassification -StagedEntries $stagedRows -RuntimeManifest (Runtime @($stagedRows[0],$stagedRows[1],[pscustomobject]@{kind='file';path=$cacheName;length=5;sha256=('8'*64)})) -CarriedFiles $carry
 if(@($c.unexpected|Where-Object{$_.change-ceq'changed'}).Count-ne1){throw 'A changed carried cache was admitted.'}}
# --- Previous-package load request, observer request and result ------------------
$base=Read-KmcJson (Join-Path $realLab 'runtime-evidence/20261022-c7b-world190-b190s1-sixmeasure/runtime-request.json')
$seed=Get-KmcChunk7UpgradeSeedSource $base.fixture
$fakeObserver=Join-Path $OutputDirectory 'observer.zip';[IO.File]::WriteAllText($fakeObserver,'observer bytes');[IO.File]::WriteAllText($fakeObserver+'.manifest.json','{}')
$observerManifest=[pscustomobject]@{version='0.1.0-observer.6';commit=('b'*40);dllSha256=('c'*64);dllMvid='11111111-2222-3333-4444-555555555555'}
$candidateManifest=[pscustomobject]@{commit=[string]$base.commit;version=[string]$base.productVersion;dllSha256=[string]$base.dllSha256;dllMvid=[string]$base.dllMvid;packageSha256=('e'*64)}
$u1Section=New-KmcChunk7UpgradeRehearsalSection 'u1-previous-install' $rid $previous $candidateManifest $null @()
$request=Clone $base;$request.scenario='chunk7-normal-player-control';$request.PSObject.Properties.Remove('ownedPairs');$request.persistenceLoad=Clone $seed.descriptor
$request|Add-Member normalPlayer ([pscustomobject](New-KmcChunk7NormalPlayerSection $null $fakeObserver $observerManifest $u1Section|ConvertTo-Json -Depth 8|ConvertFrom-Json))
Check 'the previous-package load descriptor is closed on the reference-free seed' {Assert-KmcChunk7NormalPlayerDescriptor $request}
foreach($m in @(@('driven roles',{param($r)$r.normalPlayer.riderId='x'}),@('the six-Horse fixture',{param($r)$r.persistenceLoad.sha256='b1e817a60c8bbe35b5947b4eab3b3f35060493ca20873d5de5fdf8a81efbc291'}),
 @('a source run',{param($r)$r.normalPlayer.sourceRunId='20261022-c7b-world185-b185s1-sixthhorse-cold'}))){
 Check ('the previous-package descriptor refuses '+$m[0]) {$r=Clone $request;&$m[1] $r;Assert-KmcChunk7NormalPlayerDescriptor $r} $true 'closed reference-free owner-seed load'
}
$observerRequest=New-KmcChunk7NormalPlayerObserverRequest $request ('f'*64) $OutputDirectory (Join-Path $OutputDirectory 'profile') 'C:\Game\Mods' $candidateManifest
Check 'the observer request binds the previous package and load-only mode' {
 if($observerRequest.normalPlayer.mode-cne'previous-package-load'-or$observerRequest.candidate.dllSha256-cne$pins.dllSha256-or$observerRequest.candidate.dllMvid-cne$pins.dllMvid-or
  $observerRequest.normalPlayer.kmcPackageSha256-cne$pins.packageSha256-or$null-ne$observerRequest.normalPlayer.riderId){throw 'Observer binding differs.'}}
$probeRoot=Join-Path $OutputDirectory 'probe';[void][IO.Directory]::CreateDirectory($probeRoot)
function Save-Probe([string]$Name,$Value){$path=Join-Path $probeRoot ($Name+'.json');[IO.File]::WriteAllText($path,($Value|ConvertTo-Json -Depth 20),[Text.UTF8Encoding]::new($false));$path}
$probes=[ordered]@{'previous-valid'=Save-Probe 'previous-valid' $observerRequest}
$load308=(Read-KmcJson (Join-Path $realLab 'runtime-evidence/20261022-c7b-world185-b185s1-sixthhorse-cold/runtime-request.json')).persistenceLoad
$v=Clone $observerRequest;$v.archive=Clone $load308;$probes['previous-on-308']=Save-Probe 'previous-on-308' $v
$v=Clone $observerRequest;$v.candidate.dllSha256=[string]$base.dllSha256;$probes['previous-with-candidate']=Save-Probe 'previous-with-candidate' $v
$v=Clone $observerRequest;$v.normalPlayer.riderId='rider';$probes['previous-with-roles']=Save-Probe 'previous-with-roles' $v
$v=Clone $observerRequest;$v.normalPlayer.mode='other';$probes['unknown-mode']=Save-Probe 'unknown-mode' $v
$repoObserver=Join-Path $repo 'tools/KmcRemovalObserver'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputDirectory 'request-probe.exe'
&$compiler /nologo /target:exe /langversion:7.3 "/reference:$json" "/out:$exe" (Join-Path $repoObserver 'ObserverRequest.cs') (Join-Path $repoObserver 'ObserverIdentity.cs') (Join-Path $chunk7TestSource 'NormalPlayerRequestTemplate.cs')
if($LASTEXITCODE-ne0){throw 'Observer request type compilation failed.'}
Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
foreach($leaf in @('UnityEngine.dll','UnityEngine.CoreModule.dll')){Copy-Item -LiteralPath (Join-Path (Split-Path $json) $leaf) -Destination (Join-Path $OutputDirectory $leaf)}
$lines=@(&$exe @($probes.Values|Where-Object{$_}))
function Verdict([string]$Name){$line=@($lines|Where-Object{$_.StartsWith($Name+'.json ')});if($line.Count-ne1){throw ('No probe verdict for '+$Name)};$line[0]}
Check 'the observer accepts the runner-built previous-package load request' {if((Verdict 'previous-valid')-cne'previous-valid.json VALID normalPlayer=True'){throw (Verdict 'previous-valid')}}
foreach($pair in @(@('previous-on-308','not the reference-free owner seed'),@('previous-with-candidate','not bound to the released preview.211'),@('previous-with-roles','load-only request names roles'),@('unknown-mode','not one of the closed modes'))){
 Check ('the observer refuses '+$pair[0]) {$text=Verdict $pair[0];if($text-notmatch'INVALID'-or$text-notmatch[regex]::Escape($pair[1])){throw $text}}
}
$profile=Join-Path $OutputDirectory 'profile';$hash='9'*64;$process=4242;$notBefore=[DateTimeOffset]::Parse('2026-10-24T10:00:00Z')
$n=$request.normalPlayer
function Presence([string]$Label){[pscustomobject][ordered]@{label=$Label;kmcRuntimeArguments=@();observerDllSha256=[string]$n.observer.dllSha256;observerMvid=[string]$n.observer.dllMvid;modEntries=@('KingmakerMountedCombat:active','KmcRemovalObserver:active');kmcAssemblies=1
 kmcLocation='C:\Game\Mods\KingmakerMountedCombat\KingmakerMountedCombat.dll';kmcDllSha256=$pins.dllSha256;kmcMvid=$pins.dllMvid;kmcHarmonyOwners=@('KingmakerMountedCombat.Feasibility')
 rootPresent=$true;automationHostField='null';staticAutomationHost='null';measurementSession='null';movementTelemetry='null';overlayObjects=0
 effectiveSettings=[pscustomobject]@{EnableDiagnosticOverlay=$false}}}
$checks=@(Get-KmcChunk7NormalPlayerChecks -PreviousPackageLoad)
$good=[pscustomobject][ordered]@{schemaVersion=1;evidenceKind='kmc-normal-player-observer';runId=[string]$request.runId;transactionToken=[string]$request.transactionToken
 processId=$process;startedAtUtc='2026-10-24T10:00:10Z';completedAtUtc='2026-10-24T10:02:00Z';status='PASS';observerVersion=[string]$observerRequest.observer.version
 observerAssembly='KmcRemovalObserver';requestSha256=$hash;stage=8
 checks=@($checks|ForEach-Object{[pscustomobject]@{name=$_;passed=$true}});checkPassCount=$checks.Count;checkFailCount=0;errors=@()
 observations=[pscustomobject][ordered]@{
  routing=[pscustomobject]@{savePath=(Join-Path $profile 'Saved Games');stashFolder=(Join-Path $profile 'Areas');observedSavePath=(Join-Path $profile 'Saved Games');observedStashFolder=(Join-Path $profile 'Areas')}
  presenceAtMenu=(Presence 'menu');profileBefore=[pscustomobject]@{saves=@()}
  world=[pscustomobject]@{gameId=[string]$seed.descriptor.gameId;area=[string]$seed.descriptor.area;inCombat=$false}
  presenceLoaded=(Presence 'loaded');frameWindows=@([pscustomobject]@{label='idle-loaded';meanMs=11.0});inputs=@();presenceAfter=(Presence 'after')
  archive=[pscustomobject]@{path=(Join-Path (Join-Path $profile 'Saved Games') ([string]$seed.descriptor.fileName));sha256Before=[string]$seed.descriptor.sha256;sha256After=('7'*64);changedOnDisk=$true};profileAfter=[pscustomobject]@{saves=@()}
  log=[pscustomobject]@{kmcErrors=0;kmcLines=1}}}
$obsReq=Clone $observerRequest;$obsReq.profileRoot=$profile
function Read-Result($Value,$ObserverRequest=$obsReq){Assert-KmcChunk7NormalPlayerObserverResult $request $ObserverRequest $Value $hash $process $notBefore}
Check 'the closed reader accepts an exact previous-package load PASS' {Read-Result $good}
foreach($m in @(
 @('an issued input',{param($r)$r.observations.inputs=@([pscustomobject]@{label='mount'})},'issued input'),
 @('the candidate assembly instead of preview.211',{param($r)$r.observations.presenceLoaded.kmcDllSha256=[string]$base.dllSha256},'ordinary no-request path'),
 @('the controls check set',{param($r)$names=@(Get-KmcChunk7NormalPlayerChecks);$r.checks=@($names|ForEach-Object{[pscustomobject]@{name=$_;passed=$true}});$r.checkPassCount=$names.Count},'exact declared set'),
 @('a combat start',{param($r)$r.observations.world.inCombat=$true},'out of combat'),
 @('a KMC error',{param($r)$r.observations.log.kmcErrors=1},'logged an error'),
 @('an archive outside the owned copy',{param($r)$r.observations.archive.path='C:\elsewhere.zks'},'owned seed copy'),
 @('controls observations',{param($r)$r.observations|Add-Member controlsBefore ([pscustomobject]@{label='before'})},'previous-package observations'))){
 Check ('the closed reader refuses '+$m[0]) {$r=Clone $good;&$m[1] $r;Read-Result $r} $true $m[2]
}
Check 'the closed reader refuses an observer request in the controls mode' {$o=Clone $obsReq;$o.normalPlayer.mode='controls';Read-Result $good $o} $true 'staged KMC package and mode'

# --- u2: the candidate over u1's folder state drives the ordinary controls --------
$u2Section=New-KmcChunk7UpgradeRehearsalSection 'u2-upgrade' $rid $previous $candidateManifest $carrySource $carry
$load308=(Read-KmcJson (Join-Path $realLab 'runtime-evidence/20261022-c7b-world185-b185s1-sixthhorse-cold/runtime-request.json')).persistenceLoad
$six=[pscustomobject]@{qualificationRunId='20261022-c7b-world185-b185s1-sixthhorse-cold';archiveProducerRunId='20261022-c7b-world184-b184s1-sixthhorse-manual'}
$u2=Clone $base;$u2.scenario='chunk7-normal-player-control';$u2.PSObject.Properties.Remove('ownedPairs');$u2.persistenceLoad=Clone $load308
$u2|Add-Member normalPlayer ([pscustomobject](New-KmcChunk7NormalPlayerSection $six $fakeObserver $observerManifest $u2Section|ConvertTo-Json -Depth 8|ConvertFrom-Json))
Check 'the u2 descriptor is the six-Horse control with its rehearsal section' {In-Lab {Assert-KmcChunk7NormalPlayerDescriptor $u2}}
$u2Obs=New-KmcChunk7NormalPlayerObserverRequest $u2 ('f'*64) $OutputDirectory $profile 'C:\Game\Mods' $candidateManifest
Check 'the u2 observer request drives the controls against the candidate package' {
 if($u2Obs.normalPlayer.mode-cne'controls'-or$u2Obs.candidate.dllSha256-cne[string]$base.dllSha256-or$u2Obs.normalPlayer.kmcPackageSha256-cne$candidateManifest.packageSha256-or[string]::IsNullOrEmpty([string]$u2Obs.normalPlayer.riderId)){throw 'u2 observer binding differs.'}}
$un=$u2.normalPlayer
function U2Presence([string]$Label){$x=Presence $Label;$x.kmcDllSha256=[string]$base.dllSha256;$x.kmcMvid=[string]$base.dllMvid;$x.observerDllSha256=[string]$un.observer.dllSha256;$x.observerMvid=[string]$un.observer.dllMvid;$x}
$controlChecks=@(Get-KmcChunk7NormalPlayerChecks)
$u2Good=[pscustomobject][ordered]@{schemaVersion=1;evidenceKind='kmc-normal-player-observer';runId=[string]$u2.runId;transactionToken=[string]$u2.transactionToken
 processId=$process;startedAtUtc='2026-10-24T10:00:10Z';completedAtUtc='2026-10-24T10:03:00Z';status='PASS';observerVersion=[string]$u2Obs.observer.version
 observerAssembly='KmcRemovalObserver';requestSha256=$hash;stage=7
 checks=@($controlChecks|ForEach-Object{[pscustomobject]@{name=$_;passed=$true}});checkPassCount=$controlChecks.Count;checkFailCount=0;errors=@()
 observations=[pscustomobject][ordered]@{
  routing=[pscustomobject]@{savePath=(Join-Path $profile 'Saved Games');stashFolder=(Join-Path $profile 'Areas');observedSavePath=(Join-Path $profile 'Saved Games');observedStashFolder=(Join-Path $profile 'Areas')}
  presenceAtMenu=(U2Presence 'menu');profileBefore=[pscustomobject]@{saves=@()};world=[pscustomobject]@{gameId=[string]$load308.gameId;area=[string]$load308.area}
  controlsBefore=[pscustomobject]@{label='before'};presenceLoaded=(U2Presence 'loaded')
  frameWindows=@([pscustomobject]@{label='idle-unmounted';meanMs=11.0},[pscustomobject]@{label='idle-mounted';meanMs=11.1})
  inputs=@(
   [pscustomobject]@{label='mount';casterId=$un.riderId;targetId=$un.mountId;clicked=$true;commandCreated=$true;result='Success';attachedTo=$un.mountId;riderMountDistance=0.1;neighborUnchanged=$true},
   [pscustomobject]@{label='mounted-move';moverId=$un.riderId;riderDisplacement=3.9;mountDisplacement=3.9;attachedTo=$un.mountId;neighborUnchanged=$true},
   [pscustomobject]@{label='dismount';casterId=$un.riderId;targetId=$un.riderId;clicked=$true;commandCreated=$true;result='Success';attachedTo=$null;neighborUnchanged=$true},
   [pscustomobject]@{label='unmounted-move';moverId=$un.riderId;riderDisplacement=3.8;mountDisplacement=1.0;attachedTo=$null;neighborUnchanged=$true})
  controlsAfter=[pscustomobject]@{label='after'};presenceAfter=(U2Presence 'after')
  archive=[pscustomobject]@{path=(Join-Path (Join-Path $profile 'Saved Games') ([string]$load308.fileName));sha256Before=[string]$load308.sha256;sha256After=('7'*64);changedOnDisk=$true};profileAfter=[pscustomobject]@{saves=@()}
  log=[pscustomobject]@{kmcErrors=0;kmcLines=1}}}
$u2ObsRead=Clone $u2Obs;$u2ObsRead.profileRoot=$profile
Check 'the closed reader accepts an exact u2 controls PASS on the candidate' {Assert-KmcChunk7NormalPlayerObserverResult $u2 $u2ObsRead $u2Good $hash $process $notBefore}
Check 'the u2 reader refuses preview.211 loaded in the upgrade step' {$r=Clone $u2Good;$r.observations.presenceLoaded.kmcDllSha256=$pins.dllSha256;Assert-KmcChunk7NormalPlayerObserverResult $u2 $u2ObsRead $r $hash $process $notBefore} $true 'ordinary no-request path'
Check 'the u2 reader refuses an observer request in the load-only mode' {$o=Clone $u2ObsRead;$o.normalPlayer.mode='previous-package-load';Assert-KmcChunk7NormalPlayerObserverResult $u2 $o $u2Good $hash $process $notBefore} $true 'staged KMC package and mode'
# --- UMM session log: the released preview.211's known base-game defect (u1 only) ---
# The real batch199 u1 receipt holds every KMC line of that session (10 of 10); its log
# is rebuilt from them, so the exact line preview.211 logged is the one under test.
$b199Receipt=Join-Path $realLab 'runtime-evidence/20261024-c7e-b199-x5-u1-previous/normal-player-umm-log.json'
$b199Request=Join-Path $realLab 'runtime-evidence/20261024-c7e-b199-x5-u1-previous/runtime-request.json'
$b199=Read-KmcJson $b199Receipt
$defectLine='[KingmakerMountedCombat] [Exception] Horse companion blueprint registration: InvalidOperationException - The exact Ranger companion AllFeatures surface no longer has seven stock options.'
$realU1Lines=@('[Manager] Initialize.','[Manager] Version: 0.33.2.0.')+@($b199.firstKmcLines|ForEach-Object{[string]$_})
$ummGame=Join-Path $OutputDirectory 'umm-game';$ummDir=Join-Path $ummGame 'Kingmaker_Data/Managed/UnityModManager';[void][IO.Directory]::CreateDirectory($ummDir)
function Umm-Summary([string[]]$Lines,$Admitted,[string]$Version=$pins.version){[IO.File]::WriteAllText((Join-Path $ummDir 'Log.txt'),($Lines-join"`r`n"),[Text.UTF8Encoding]::new($false))
 Get-KmcChunk7NormalPlayerUmmLogSummary $ummGame ([DateTimeOffset]::UtcNow.AddMinutes(-5)) $Version $Admitted ([string]$request.runId)}
$u1Admitted=@(Get-KmcChunk7UpgradeAdmittedLogDefects $u1Section)
$u3Section=New-KmcChunk7UpgradeRehearsalSection 'u3-rollback' $rid $previous $candidateManifest $carrySource $carry
Check 'the real b199 u1 receipt is the retained record with all ten KMC lines and its one exact defect line' {
 if((Get-KmcSha256 $b199Receipt)-cne'bb905e629d4d796cd7c4dbf148dcf0c759d1d7a5cd6cff67c6e423f8521acdfc'-or[int]$b199.kmcLines-ne10-or@($b199.firstKmcLines).Count-ne10-or
  $b199.passed-ne$false-or[int]$b199.errorLines-ne1-or@($b199.firstKmcLines|Where-Object{$_-ceq$defectLine}).Count-ne1){throw 'b199 receipt differs.'}}
Check 'u1 of the exact preview.211 package admits exactly the one pinned defect' {
 if($u1Admitted.Count-ne1-or[string]$u1Admitted[0].line-cne$defectLine-or[int]$u1Admitted[0].expectedOccurrences-ne1-or
  [string]$u1Admitted[0].sourceFixCommit-cne'81c191afec2fc3210b8777323447c0b4edb4ddb1'-or[string]$u1Admitted[0].id-cne'preview211-base-game-ranger-allfeatures-horse-registration'){throw 'u1 admission differs.'}}
Check 'the real b199 u1 request section admits the same defect' {$a=@(Get-KmcChunk7UpgradeAdmittedLogDefects (Read-KmcJson $b199Request).normalPlayer.upgradeRehearsal);if($a.Count-ne1-or[string]$a[0].line-cne$defectLine){throw 'b199 request admission differs.'}}
Check 'an unchanged JSON round-trip of the u1 section still admits the defect (control for the mutations below)' {if(@(Get-KmcChunk7UpgradeAdmittedLogDefects (Clone ([pscustomobject]$u1Section))).Count-ne1){throw 'control refused'}}
foreach($m in @(
 @('u1 naming another version',{$s=Clone ([pscustomobject]$u1Section);$s.stagedKmc.version='0.1.0-chunk6d-preview.210';$s}),
 @('u1 naming another commit',{$s=Clone ([pscustomobject]$u1Section);$s.stagedKmc.commit=('0'*40);$s}),
 @('u2 (the candidate)',{$u2Section}),@('no rehearsal',{$null}),
 @('a u2 section crafted with role previous and every preview.211 pin',{$s=Clone ([pscustomobject]$u1Section);$s.step='u2-upgrade';$s}),
 @('u1 naming another package hash',{$s=Clone ([pscustomobject]$u1Section);$s.stagedKmc.packageSha256=('0'*64);$s}),
 @('u1 naming another DLL',{$s=Clone ([pscustomobject]$u1Section);$s.stagedKmc.dllSha256=('0'*64);$s}),
 @('u1 naming another MVID',{$s=Clone ([pscustomobject]$u1Section);$s.stagedKmc.dllMvid='00000000-0000-0000-0000-000000000000';$s}),
 @('u1 naming another manifest',{$s=Clone ([pscustomobject]$u1Section);$s.stagedKmc.manifestSha256=('0'*64);$s}),
 @('u1 staging the candidate role',{$s=Clone ([pscustomobject]$u1Section);$s.stagedKmc.role='candidate';$s}))){
 Check ('no known defect is admitted for '+$m[0]) {if(@(Get-KmcChunk7UpgradeAdmittedLogDefects (& $m[1])|Where-Object{$null-ne$_}).Count-ne0){throw 'admitted'}}
}
Check 'u3 rollback of the exact preview.211 package admits the same one pinned defect' {$a=@(Get-KmcChunk7UpgradeAdmittedLogDefects $u3Section);if($a.Count-ne1-or[string]$a[0].line-cne$defectLine-or[string]$a[0].id-cne[string]$u1Admitted[0].id){throw 'u3 admission differs.'}}
Check 'an unchanged JSON round-trip of the u3 section still admits the defect (control for the mutations below)' {if(@(Get-KmcChunk7UpgradeAdmittedLogDefects (Clone ([pscustomobject]$u3Section))).Count-ne1){throw 'control refused'}}
foreach($m in @(
 @('package hash',{param($s)$s.stagedKmc.packageSha256=('0'*64)}),@('DLL',{param($s)$s.stagedKmc.dllSha256=('0'*64)}),
 @('MVID',{param($s)$s.stagedKmc.dllMvid='00000000-0000-0000-0000-000000000000'}),@('manifest',{param($s)$s.stagedKmc.manifestSha256=('0'*64)}),
 @('version',{param($s)$s.stagedKmc.version='0.1.0-chunk6d-preview.210'}),@('commit',{param($s)$s.stagedKmc.commit=('0'*40)}),
 @('candidate role',{param($s)$s.stagedKmc.role='candidate'}))){
 Check ('no known defect is admitted for u3 naming another '+$m[0]) {$s=Clone ([pscustomobject]$u3Section);&$m[1] $s;if(@(Get-KmcChunk7UpgradeAdmittedLogDefects $s|Where-Object{$null-ne$_}).Count-ne0){throw 'admitted'}}
}
$realU1=Umm-Summary $realU1Lines $u1Admitted
Check 'the real b199 u1 preview.211 start now passes u1 and names its admitted defect' {
 $d=@($realU1.admittedOldVersionKnownDefects)
 if($realU1.passed-ne$true-or[int]$realU1.errorLines-ne1-or[int]$realU1.knownOldVersionDefectLines-ne1-or$d.Count-ne1-or[int]$d[0].observedOccurrences-ne1-or
  [string]$d[0].line-cne$defectLine-or[string]$d[0].reference-cne'docs/CHUNK7-RANGER-BOOTSTRAP-REPAIR.md'-or[int]$realU1.schemaVersion-ne2){throw 'real u1 refused or unrecorded.'}}
Check 'the same real u1 start without the admission keeps the old refusal verdict' {$s=Umm-Summary $realU1Lines @();if($s.passed-or[int]$s.errorLines-ne1){throw 'accepted'}}
foreach($m in @(
 @('an altered defect text',@($realU1Lines|ForEach-Object{$_.Replace('seven stock options','eight stock options')}),$u1Admitted),
 @('a soft hyphen inside the defect line',@($realU1Lines|ForEach-Object{if($_-ceq$defectLine){$_.Replace('Horse companion',('Horse'+[char]0xAD+' companion'))}else{$_}}),$u1Admitted),
 @('the defect line plus a soft-hyphen variant',@($realU1Lines+$defectLine.Replace('Horse companion',('Horse'+[char]0xAD+' companion'))),$u1Admitted),
 @('more error lines than the receipt captures',@($realU1Lines+@(1..33|ForEach-Object{'[KingmakerMountedCombat] [Error] e'+$_})),$u1Admitted),
 @('a trailing space on the defect line',@($realU1Lines|ForEach-Object{if($_-ceq$defectLine){$_+' '}else{$_}}),$u1Admitted),
 @('two occurrences',@($realU1Lines+$defectLine),$u1Admitted),
 @('no occurrence',@($realU1Lines|Where-Object{$_-cne$defectLine}),$u1Admitted),
 @('an extra KMC error line',@($realU1Lines+'[KingmakerMountedCombat] [Error] something failed'),$u1Admitted),
 @('an extra KMC exception line',@($realU1Lines+'[KingmakerMountedCombat] [Exception] System.NullReferenceException: x'),$u1Admitted),
 @('an extra Exception: line',@($realU1Lines+'[KingmakerMountedCombat] Persistence: System.InvalidOperationException: y'),$u1Admitted),
 @('an accepted automation request',@($realU1Lines+'[KingmakerMountedCombat] Runtime automation request accepted: x / y'),$u1Admitted),
 @('the same line under u2',$realU1Lines,@(Get-KmcChunk7UpgradeAdmittedLogDefects $u2Section)),
 @('the same line with a non-preview.211 package',$realU1Lines,@(Get-KmcChunk7UpgradeAdmittedLogDefects $(& {$s=Clone ([pscustomobject]$u1Section);$s.stagedKmc.packageSha256=('0'*64);$s}))))){
 Check ('the u1 session check refuses '+$m[0]) {$s=Umm-Summary $m[1] $m[2];if($s.passed){throw 'accepted'}}
}
$u3Admitted=@(Get-KmcChunk7UpgradeAdmittedLogDefects $u3Section)
Check 'the real preview.211 start passes u3 and names the same admitted defect' {$s=Umm-Summary $realU1Lines $u3Admitted
 if($s.passed-ne$true-or@($s.admittedOldVersionKnownDefects).Count-ne1-or[int]@($s.admittedOldVersionKnownDefects)[0].observedOccurrences-ne1){throw 'u3 refused'}}
foreach($m in @(
 @('an altered defect text',@($realU1Lines|ForEach-Object{$_.Replace('seven stock options','eight stock options')}),$u3Admitted),
 @('two occurrences',@($realU1Lines+$defectLine),$u3Admitted),
 @('no occurrence',@($realU1Lines|Where-Object{$_-cne$defectLine}),$u3Admitted),
 @('an extra KMC error line',@($realU1Lines+'[KingmakerMountedCombat] [Error] something failed'),$u3Admitted),
 @('the same line with a non-preview.211 package',$realU1Lines,@(Get-KmcChunk7UpgradeAdmittedLogDefects $(& {$s=Clone ([pscustomobject]$u3Section);$s.stagedKmc.packageSha256=('0'*64);$s}))))){
 Check ('the u3 session check refuses '+$m[0]) {$s=Umm-Summary $m[1] $m[2];if($s.passed){throw 'accepted'}}
}
$candidateQuiet=@('[Manager] Initialize.','[Manager] Version: 0.33.2.0.',('[KingmakerMountedCombat] Kingmaker Mounted Combat '+$candidateManifest.version+' loaded with transient private-alpha mounted melee services.'),
 '[KingmakerMountedCombat] Private-alpha services and native mounted abilities enabled; diagnostic overlay=False.','[KingmakerMountedCombat] Active.')
Check 'a quiet u2 candidate start passes and shows the preview.211 defect line absent' {$s=Umm-Summary $candidateQuiet @(Get-KmcChunk7UpgradeAdmittedLogDefects $u2Section) $candidateManifest.version
 if($s.passed-ne$true-or[int]$s.knownOldVersionDefectLines-ne0-or@($s.admittedOldVersionKnownDefects).Count-ne0){throw 'u2 quiet refused'}}
Check 'a u2 candidate start that logs the preview.211 defect line refuses' {$s=Umm-Summary @($candidateQuiet+$defectLine) @(Get-KmcChunk7UpgradeAdmittedLogDefects $u2Section) $candidateManifest.version
 if($s.passed-or[int]$s.knownOldVersionDefectLines-ne1){throw 'accepted'}}
# Game record binding (checked before any file read) and the row-42 refusal.
$recordFor={param($Req,[string]$Status='PASS')$u=$Req.normalPlayer.upgradeRehearsal
 [pscustomobject]@{evidenceKind='kmc-normal-player-observer';status=$Status;kmcAutomationRequest=$false;observerResultSha256=$null
  upgradeRehearsal=[pscustomobject]@{rehearsalId=[string]$u.rehearsalId;step=[string]$u.step;stagedKmc=$u.stagedKmc;loadedKmc=[pscustomobject]@{dllSha256=[string]$u.stagedKmc.dllSha256;dllMvid=[string]$u.stagedKmc.dllMvid}}}}
$bindRoot=Join-Path $OutputDirectory 'bind';[void][IO.Directory]::CreateDirectory($bindRoot);[IO.File]::WriteAllText((Join-Path $bindRoot 'observer-result.json'),'{"status":"PASS"}')
function Bind-Request{$r=Clone $request;$r.evidenceRoot=$bindRoot;$r}
function Bind-Record([string]$Status='PASS'){$g=& $recordFor $request $Status;$g.observerResultSha256=Get-KmcSha256 (Join-Path $bindRoot 'observer-result.json');$g}
Write-KmcJsonAtomic (Join-Path $bindRoot 'normal-player-umm-log.json') $realU1
$bindReceipt=Join-Path $bindRoot 'normal-player-umm-log.json'
# The outer result records the receipt's SHA-256; the reader verifies the file against it.
function Bind-Result{[pscustomobject]@{normalPlayerUmmReceiptSha256=$(if(Test-Path -LiteralPath $bindReceipt){Get-KmcSha256 $bindReceipt}else{$null})}}
Check 'the run binding refuses a rehearsal record without its step' {Assert-KmcChunk7NormalPlayerRuntimeBinding (Bind-Request) ([pscustomobject]@{evidenceKind='kmc-normal-player-observer';upgradeRehearsal=$null}) 'PASS'} $true 'does not record the rehearsal step'
Check 'the run binding refuses a u1 PASS that observed the candidate loaded' {$g=Bind-Record;$g.upgradeRehearsal.loadedKmc.dllSha256=[string]$base.dllSha256;Assert-KmcChunk7NormalPlayerRuntimeBinding (Bind-Request) $g 'PASS'} $true 'does not record the rehearsal step'
Check 'the run binding refuses a u1 PASS with another loaded MVID' {$g=Bind-Record;$g.upgradeRehearsal.loadedKmc.dllMvid='00000000-0000-0000-0000-000000000000';Assert-KmcChunk7NormalPlayerRuntimeBinding (Bind-Request) $g 'PASS'} $true 'does not record the rehearsal step'
Check 'the run binding refuses a u1 PASS with no observed loaded assembly' {$g=Bind-Record;$g.upgradeRehearsal.loadedKmc=$null;Assert-KmcChunk7NormalPlayerRuntimeBinding (Bind-Request) $g 'PASS'} $true 'does not record the rehearsal step'
Check 'the run binding refuses a record naming another staged package' {$g=Bind-Record;$g.upgradeRehearsal.stagedKmc=[pscustomobject]@{packageSha256=('e'*64);dllSha256=[string]$base.dllSha256};Assert-KmcChunk7NormalPlayerRuntimeBinding (Bind-Request) $g 'PASS'} $true 'does not record the rehearsal step'
Check 'the run binding fully accepts an exact u1 PASS bound to its observer result' {Assert-KmcChunk7NormalPlayerRuntimeBinding (Bind-Request) (Bind-Record) 'PASS' (Bind-Result)}
Check 'the run binding refuses a u1 FAIL that recorded a different loaded assembly' {$g=Bind-Record 'FAIL';$g.upgradeRehearsal.loadedKmc.dllSha256=[string]$base.dllSha256;Assert-KmcChunk7NormalPlayerRuntimeBinding (Bind-Request) $g 'FAIL'} $true 'does not record the rehearsal step'
Check 'the run binding fully accepts a u1 FAIL that recorded no loaded presence' {$g=Bind-Record 'FAIL';$g.upgradeRehearsal.loadedKmc=$null;Assert-KmcChunk7NormalPlayerRuntimeBinding (Bind-Request) $g 'FAIL'}
# The external reader re-judges the runner's UMM receipt on every PASS by the same rule.
$bindReceipt=Join-Path $bindRoot 'normal-player-umm-log.json'
function Write-Receipt($Summary,[scriptblock]$Mutate){$x=Clone $Summary;if($Mutate){&$Mutate $x};Write-KmcJsonAtomic $bindReceipt $x}
function Bind-U2Request{$r=Clone $u2;$r.evidenceRoot=$bindRoot;$r}
function Bind-U2Record{$g=& $recordFor $u2;$g.observerResultSha256=Get-KmcSha256 (Join-Path $bindRoot 'observer-result.json');$g}
$u2Quiet=Umm-Summary $candidateQuiet @(Get-KmcChunk7UpgradeAdmittedLogDefects $u2Section) $candidateManifest.version
Check 'the reader binds the real b199 u1 start with its admitted preview.211 defect' {Write-Receipt $realU1 $null;Assert-KmcChunk7NormalPlayerRuntimeBinding (Bind-Request) (Bind-Record) 'PASS' (Bind-Result)}
Check 'the reader binds a quiet u2 candidate start' {Write-Receipt $u2Quiet $null;Assert-KmcChunk7NormalPlayerRuntimeBinding (Bind-U2Request) (Bind-U2Record) 'PASS' (Bind-Result)}
foreach($m in @(
 @('a u1 PASS without its UMM receipt',{Bind-Request},{Bind-Record},{Remove-Item -LiteralPath $bindReceipt -Force},'lacks its UMM session receipt'),
 @('a u1 receipt that hides its admitted defect',{Bind-Request},{Bind-Record},{Write-Receipt $realU1 {param($x)$x.admittedOldVersionKnownDefects=@()}},'admits old-version defects other than'),
 @('a u1 receipt naming another fix commit',{Bind-Request},{Bind-Record},{Write-Receipt $realU1 {param($x)@($x.admittedOldVersionKnownDefects)[0].sourceFixCommit=('0'*40)}},'admits old-version defects other than'),
 @('a u1 receipt with two occurrences marked passed',{Bind-Request},{Bind-Record},{Write-Receipt $realU1 {param($x)@($x.admittedOldVersionKnownDefects)[0].observedOccurrences=2;$x.errorLines=2;$x.knownOldVersionDefectLines=2;$x.errorLineTexts=@($defectLine,$defectLine)}},'not an ordinary quiet no-request start'),
 @('a u1 receipt whose error text differs',{Bind-Request},{Bind-Record},{Write-Receipt $realU1 {param($x)$x.errorLineTexts=@($defectLine.Replace('seven','eight'))}},'not an ordinary quiet no-request start'),
 @('a u1 receipt with an extra error line',{Bind-Request},{Bind-Record},{Write-Receipt $realU1 {param($x)$x.errorLines=2;$x.errorLineTexts=@($defectLine,'[KingmakerMountedCombat] [Error] x')}},'not an ordinary quiet no-request start'),
 @('a u1 receipt that says it failed',{Bind-Request},{Bind-Record},{Write-Receipt $realU1 {param($x)$x.passed=$false}},'not an ordinary quiet no-request start'),
 @('a u1 receipt of another run',{Bind-Request},{Bind-Record},{Write-Receipt $realU1 {param($x)$x.runId='another-run'}},'does not name this run'),
 @('a u1 receipt in the historical schema',{Bind-Request},{Bind-Record},{Write-Receipt $realU1 {param($x)$x.schemaVersion=1}},'admits old-version defects other than'),
 @('the u1 admission carried into a u2 receipt',{Bind-U2Request},{Bind-U2Record},{Write-Receipt $realU1 $null},'admits old-version defects other than'),
 @('a u2 receipt that logged the defect line marked passed',{Bind-U2Request},{Bind-U2Record},{Write-Receipt $u2Quiet {param($x)$x.errorLines=1;$x.errorLineTexts=@($defectLine);$x.knownOldVersionDefectLines=1}},'not an ordinary quiet no-request start'),
 @('a u2 receipt that counts the defect line but no error',{Bind-U2Request},{Bind-U2Record},{Write-Receipt $u2Quiet {param($x)$x.knownOldVersionDefectLines=1}},'not an ordinary quiet no-request start'))){
 Check ('the reader refuses '+$m[0]) {&$m[3];try{Assert-KmcChunk7NormalPlayerRuntimeBinding (& $m[1]) (& $m[2]) 'PASS' (Bind-Result)}finally{Write-Receipt $realU1 $null}} $true $m[4]
}
Check 'the reader accepts a u1 FAIL without any UMM receipt' {Remove-Item -LiteralPath $bindReceipt -Force;try{$g=Bind-Record 'FAIL';$g.upgradeRehearsal.loadedKmc=$null;Assert-KmcChunk7NormalPlayerRuntimeBinding (Bind-Request) $g 'FAIL'}finally{Write-Receipt $realU1 $null}}
Check 'the reader still accepts the historical schema-1 receipt of the passed b191 control' {
 $plain=Clone $u2;$plain.normalPlayer.PSObject.Properties.Remove('upgradeRehearsal');$plain.evidenceRoot=$bindRoot
 Copy-Item -LiteralPath (Join-Path $realLab 'runtime-evidence/20261023-c7b-world191-b191s2-normalplayer/normal-player-umm-log.json') -Destination $bindReceipt -Force
 try{$g=[pscustomobject]@{evidenceKind='kmc-normal-player-observer';status='PASS';kmcAutomationRequest=$false;observerResultSha256=(Get-KmcSha256 (Join-Path $bindRoot 'observer-result.json'))}
  Assert-KmcChunk7NormalPlayerRuntimeBinding $plain $g 'PASS' (Read-KmcJson (Join-Path $realLab 'runtime-evidence/20261023-c7b-world191-b191s2-normalplayer/runtime-result.json'))}finally{Write-Receipt $realU1 $null}}
Check 'the reader refuses a historical schema-1 receipt bound by a receipt hash' {
 $plain=Clone $u2;$plain.normalPlayer.PSObject.Properties.Remove('upgradeRehearsal');$plain.evidenceRoot=$bindRoot
 Copy-Item -LiteralPath (Join-Path $realLab 'runtime-evidence/20261023-c7b-world191-b191s2-normalplayer/normal-player-umm-log.json') -Destination $bindReceipt -Force
 try{$g=[pscustomobject]@{evidenceKind='kmc-normal-player-observer';status='PASS';kmcAutomationRequest=$false;observerResultSha256=(Get-KmcSha256 (Join-Path $bindRoot 'observer-result.json'))}
  Assert-KmcChunk7NormalPlayerRuntimeBinding $plain $g 'PASS' (Bind-Result)}finally{Write-Receipt $realU1 $null}} $true 'historical schema-1 UMM receipt cannot be bound'
# u3 binds with the same admission; the receipt is hash-bound into the outer result.
$u3Req=Clone $request;$u3Req.normalPlayer.upgradeRehearsal=Clone ([pscustomobject]$u3Section);$u3Req.evidenceRoot=$bindRoot
function Bind-U3Record{$g=& $recordFor $u3Req;$g.observerResultSha256=Get-KmcSha256 (Join-Path $bindRoot 'observer-result.json');$g}
$u3Receipt=Umm-Summary $realU1Lines $u3Admitted
Check 'the reader binds a u3 preview.211 start with the same admitted defect' {Write-Receipt $u3Receipt $null;try{Assert-KmcChunk7NormalPlayerRuntimeBinding $u3Req (Bind-U3Record) 'PASS' (Bind-Result)}finally{Write-Receipt $realU1 $null}}
Check 'the reader refuses a u3 receipt that hides its admitted defect' {Write-Receipt $u3Receipt {param($x)$x.admittedOldVersionKnownDefects=@()};try{Assert-KmcChunk7NormalPlayerRuntimeBinding $u3Req (Bind-U3Record) 'PASS' (Bind-Result)}finally{Write-Receipt $realU1 $null}} $true 'admits old-version defects other than'
Check 'the reader refuses a u3 receipt with an extra error line' {Write-Receipt $u3Receipt {param($x)$x.errorLines=2;$x.errorLineTexts=@($defectLine,'[KingmakerMountedCombat] [Error] x')};try{Assert-KmcChunk7NormalPlayerRuntimeBinding $u3Req (Bind-U3Record) 'PASS' (Bind-Result)}finally{Write-Receipt $realU1 $null}} $true 'not an ordinary quiet no-request start'
foreach($m in @(
 @('a receipt changed after its hash was recorded (PASS)','PASS',{param($r)[IO.File]::AppendAllText($bindReceipt,' ')},'does not match the SHA-256 its outer result records'),
 @('a receipt changed after its hash was recorded (FAIL)','FAIL',{param($r)[IO.File]::AppendAllText($bindReceipt,' ')},'does not match the SHA-256 its outer result records'),
 @('a receipt removed after its hash was recorded','FAIL',{param($r)Remove-Item -LiteralPath $bindReceipt -Force},'does not match the SHA-256 its outer result records'),
 @('a malformed recorded receipt hash','PASS',{param($r)$r.normalPlayerUmmReceiptSha256='X'},'does not match the SHA-256 its outer result records'),
 @('a PASS whose outer result records a null receipt hash','PASS',{param($r)$r.normalPlayerUmmReceiptSha256=$null},'does not record its UMM session receipt SHA-256'),
 @('a PASS whose outer result lacks the receipt hash','PASS',{param($r)$r.PSObject.Properties.Remove('normalPlayerUmmReceiptSha256')},'does not record its UMM session receipt SHA-256'))){
 Check ('the reader refuses '+$m[0]) {Write-Receipt $realU1 $null;$res=Bind-Result;&$m[2] $res;try{$g=Bind-Record $m[1];if($m[1]-ceq'FAIL'){$g.upgradeRehearsal.loadedKmc=$null};Assert-KmcChunk7NormalPlayerRuntimeBinding (Bind-Request) $g $m[1] $res}finally{Write-Receipt $realU1 $null}} $true $m[3]
}
Check 'the reader accepts a u1 FAIL whose recorded receipt hash matches' {Write-Receipt $realU1 $null;$g=Bind-Record 'FAIL';$g.upgradeRehearsal.loadedKmc=$null;Assert-KmcChunk7NormalPlayerRuntimeBinding (Bind-Request) $g 'FAIL' (Bind-Result)}
Check 'row 42 refuses a rehearsal step' {Assert-KmcChunk7NormalPlayerControlRow $u2 (& $recordFor $u2)} $true 'cannot stand in for the ordinary request-free control'
Check 'row 42 refuses a record that claims a rehearsal step' {$plain=Clone $u2;$plain.normalPlayer.PSObject.Properties.Remove('upgradeRehearsal');Assert-KmcChunk7NormalPlayerControlRow $plain (& $recordFor $u2)} $true 'cannot stand in for the ordinary request-free control'
Check 'row 42 admits an ordinary control record' {$plain=Clone $u2;$plain.normalPlayer.PSObject.Properties.Remove('upgradeRehearsal');Assert-KmcChunk7NormalPlayerControlRow $plain ([pscustomobject]@{evidenceKind='kmc-normal-player-observer';upgradeRehearsal=$null})}
# --- Row consumers on the real retained b200 rehearsal and b191 controls (read-only) ----
# Rows 55-56 consume one complete rehearsal; row 42 one ordinary control. The real records
# are only read; every mutation runs on a copy in a separate fake lab.
$x5Rid='x5-b200-20261024';$x5Ids=@('20261024-c7e-b200-x5-u1-previous','20261024-c7e-b200-x5-u2-upgrade','20261024-c7e-b200-x5-u3-rollback')
$x5Order=@(Get-KmcChunk7UpgradeRehearsalSteps)
function Get-ResultPin([string]$RunId){Get-KmcSha256 (Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$RunId+'/runtime-result.json'))}
function New-X5Pins([string[]]$Labels=$x5Order,[string[]]$Ids=$x5Ids){@(for($i=0;$i-lt$Ids.Count;$i++){[pscustomobject][ordered]@{step=$Labels[$i];runId=$Ids[$i];resultSha256=(Get-ResultPin $Ids[$i])}})}
$noReader={param($r,$q)}
Check 'rows 55-56 bind the real b200 rehearsal (full external reader on each step)' {
 $b=Assert-KmcChunk7UpgradeRehearsalComplete $x5Rid (New-X5Pins)
 if($b.rehearsalId-cne$x5Rid-or(@($b.steps|ForEach-Object{$_.step})-join',')-cne($x5Order-join',')-or(@($b.steps|ForEach-Object{$_.stagedRole})-join',')-cne'previous,candidate,previous'-or
  [string]$b.candidate.dllSha256-cne'ec20ee8f4e6824fb5530f386354fa1117da0920aff6c1a159c22b34a406ecc13'-or@($b.steps|Where-Object{[string]$_.ummReceiptSha256-cnotmatch'^[0-9a-f]{64}$'}).Count-ne0){throw 'Binding differs.'}
}
Check 'rows 55-56 run the external reader once per step, in order' {$seen=[Collections.Generic.List[string]]::new()
 [void](Assert-KmcChunk7UpgradeRehearsalComplete $x5Rid (New-X5Pins) {param($r,$q)$seen.Add((Split-Path -Leaf (Split-Path -Parent $r)))})
 if(($seen-join',')-cne($x5Ids-join',')){throw ('Reader calls: '+($seen-join','))}
}
Check 'rows 55-56 propagate a reader refusal' {Assert-KmcChunk7UpgradeRehearsalComplete $x5Rid (New-X5Pins) {param($r,$q)throw 'reader refused'}} $true 'reader refused'
foreach($m in @(
 @('a missing u3',{New-X5Pins $x5Order[0..1] $x5Ids[0..1]},'exactly u1, u2 and u3'),
 @('a missing u1',{New-X5Pins $x5Order[1..2] $x5Ids[1..2]},'exactly u1, u2 and u3'),
 @('a fourth step',{@(New-X5Pins)+@((New-X5Pins)[2])},'exactly u1, u2 and u3'),
 @('steps out of order',{New-X5Pins @($x5Order[0],$x5Order[2],$x5Order[1]) @($x5Ids[0],$x5Ids[2],$x5Ids[1])},'in that order'),
 @('a run labelled as another step',{New-X5Pins $x5Order @($x5Ids[0],$x5Ids[2],$x5Ids[1])},'mixes steps or rehearsal ids'),
 @('one run used twice',{New-X5Pins $x5Order @($x5Ids[0],$x5Ids[1],$x5Ids[1])},'three distinct runs'),
 @('the b199 u1 of another rehearsal id',{New-X5Pins $x5Order @('20261024-c7e-b199-x5-u1-previous',$x5Ids[1],$x5Ids[2])},'mixes steps or rehearsal ids|did not pass'),
 @('a tampered result pin',{$p=New-X5Pins;$p[1].resultSha256=('0'*64);$p},'does not match the SHA-256 its pin records'),
 @('a pin with an extra member',{$p=New-X5Pins;$p[0]|Add-Member -NotePropertyName extra -NotePropertyValue 1;$p},'upgrade rehearsal step pin'))){
 Check ('rows 55-56 refuse '+$m[0]) {Assert-KmcChunk7UpgradeRehearsalComplete $x5Rid (&$m[1]) $noReader} $true $m[2]
}
Check 'rows 55-56 refuse another rehearsal id than the runs record' {Assert-KmcChunk7UpgradeRehearsalComplete 'x5-b199-20261024' (New-X5Pins) $noReader} $true 'mixes steps or rehearsal ids'
# Copies of the three runs (and u1's transaction state) in a separate fake lab.
$x5Lab=Join-Path $OutputDirectory 'x5lab'
function Stage-X5([scriptblock]$Mutate){
 if(Test-Path -LiteralPath $x5Lab){Remove-Item -LiteralPath $x5Lab -Recurse -Force}
 [void][IO.Directory]::CreateDirectory((Join-Path $x5Lab 'runtime-evidence'));[void][IO.Directory]::CreateDirectory((Join-Path $x5Lab 'runtime-state/transactions'))
 foreach($id in $x5Ids){Copy-Item -LiteralPath (Join-Path $realLab ('runtime-evidence/'+$id)) -Destination (Join-Path $x5Lab 'runtime-evidence') -Recurse
  Copy-Item -LiteralPath (Join-Path $realLab ('runtime-state/transactions/'+$id+'.json')) -Destination (Join-Path $x5Lab 'runtime-state/transactions')}
 if($Mutate){&$Mutate}
}
function Edit-X5Json([string]$RunId,[string]$Leaf,[scriptblock]$Edit){
 $p=if($Leaf-ceq'state'){Join-Path $x5Lab ('runtime-state/transactions/'+$RunId+'.json')}else{Join-Path $x5Lab ('runtime-evidence/'+$RunId+'/'+$Leaf)}
 $v=Read-KmcJson $p;&$Edit $v;[IO.File]::WriteAllText($p,($v|ConvertTo-Json -Depth 60),[Text.UTF8Encoding]::new($false))
}
function Invoke-X5Fake([scriptblock]$Mutate){Stage-X5 $Mutate;Use-Lab $x5Lab;try{Assert-KmcChunk7UpgradeRehearsalComplete $x5Rid (New-X5Pins) $noReader}finally{Restore-Lab}}
# An unchanged copy passes every cross-run check and stops only where the carried files are
# re-derived from u1's staged-after tree, which the copy does not hold: so each mutation below
# is refused by its own check, not by the copy.
Check 'an unchanged fake-lab copy reaches the carried-file re-derivation' {Invoke-X5Fake $null} $true 'u1 staged-after tree'
foreach($m in @(
 @('a tampered u2 game record',{[IO.File]::AppendAllText((Join-Path $x5Lab ('runtime-evidence/'+$x5Ids[1]+'/runtime-game-result.json')),' ')},'not hash-bound result to game record'),
 @('a tampered u3 observer result',{[IO.File]::AppendAllText((Join-Path $x5Lab ('runtime-evidence/'+$x5Ids[2]+'/observer-result.json')),' ')},'not hash-bound result to game record'),
 @('a tampered u1 UMM receipt',{[IO.File]::AppendAllText((Join-Path $x5Lab ('runtime-evidence/'+$x5Ids[0]+'/normal-player-umm-log.json')),' ')},'does not match the SHA-256 its outer result records'),
 @('a u3 request of another rehearsal id',{Edit-X5Json $x5Ids[2] 'runtime-request.json' {param($v)$v.normalPlayer.upgradeRehearsal.rehearsalId='x5-other'}},'mixes steps or rehearsal ids'),
 @('a u2 whose outer result failed',{Edit-X5Json $x5Ids[1] 'runtime-result.json' {param($v)$v.status='FAIL'}},'step did not pass: u2-upgrade'),
 @('a u2 PASS over a failed observer result (hash chain re-bound)',{$d=Join-Path $x5Lab ('runtime-evidence/'+$x5Ids[1])
  Edit-X5Json $x5Ids[1] 'observer-result.json' {param($v)$v.status='FAIL'}
  Edit-X5Json $x5Ids[1] 'runtime-game-result.json' {param($v)$v.observerResultSha256=(Get-KmcSha256 (Join-Path $d 'observer-result.json'))}
  Edit-X5Json $x5Ids[1] 'runtime-result.json' {param($v)$v.gameResultSha256=(Get-KmcSha256 (Join-Path $d 'runtime-game-result.json'))}},'disagree on the verdict: '),
 @('a u3 started before u2 completed',{Edit-X5Json $x5Ids[2] 'runtime-result.json' {param($v)$v.startedAtUtc='2026-10-24T10:46:30.0000000+00:00'}},'run in that order'),
 @('a u3 on another candidate',{Edit-X5Json $x5Ids[2] 'runtime-result.json' {param($v)$v.commit=('a'*40)}},'more than one candidate'),
 @('a u2 whose carry source pins another u1 game record',{Edit-X5Json $x5Ids[1] 'runtime-request.json' {param($v)$v.normalPlayer.upgradeRehearsal.carrySource.gameResultSha256=('0'*64)}},'does not bind the same u1'),
 @('a u3 whose carry source names another run',{Edit-X5Json $x5Ids[2] 'runtime-request.json' {param($v)$v.normalPlayer.upgradeRehearsal.carrySourceRunId=$x5Ids[1]}},'does not bind the same u1'),
 @('a u1 transaction state changed after u2 and u3 pinned it',{[IO.File]::AppendAllText((Join-Path $x5Lab ('runtime-state/transactions/'+$x5Ids[0]+'.json')),' ')},'does not bind the same u1: \S+ stateSha256'))){
 Check ('rows 55-56 refuse '+$m[0]) {Invoke-X5Fake $m[1]} $true $m[2]
}
$r42Pass='20261023-c7b-world191-b191s2-normalplayer';$r42Fail='20261023-c7b-world191-b191s1-normalplayer'
Check 'the row-42 binder binds the real b191 ordinary control (full external reader)' {$b=Assert-KmcChunk7NormalPlayerRow42Binding $r42Pass (Get-ResultPin $r42Pass);if($b.row-ne42-or$b.runId-cne$r42Pass){throw 'Binding differs.'}}
Check 'the row-42 binder refuses the real b200 u2 rehearsal step' {[void](Assert-KmcChunk7NormalPlayerRow42Binding $x5Ids[1] (Get-ResultPin $x5Ids[1]) $noReader)} $true 'cannot stand in for the ordinary request-free control'
Check 'the row-42 binder refuses the real b200 u1 rehearsal step' {[void](Assert-KmcChunk7NormalPlayerRow42Binding $x5Ids[0] (Get-ResultPin $x5Ids[0]) $noReader)} $true 'cannot stand in for the ordinary request-free control'
Check 'the row-42 binder refuses the real failed b191 control' {[void](Assert-KmcChunk7NormalPlayerRow42Binding $r42Fail (Get-ResultPin $r42Fail) $noReader)} $true 'binds only a passed'
Check 'the row-42 binder refuses a tampered result pin' {[void](Assert-KmcChunk7NormalPlayerRow42Binding $r42Pass ('0'*64) $noReader)} $true 'does not match the SHA-256 its pin records'
Check 'the row-42 binder refuses a copy whose request names another evidence folder' {
 Stage-X5 {Copy-Item -LiteralPath (Join-Path $realLab ('runtime-evidence/'+$r42Pass)) -Destination (Join-Path $x5Lab 'runtime-evidence') -Recurse};Use-Lab $x5Lab
 try{[void](Assert-KmcChunk7NormalPlayerRow42Binding $r42Pass (Get-ResultPin $r42Pass) $noReader)}finally{Restore-Lab}} $true 'does not name the evidence folder'
# The runner's own post-stage check, executed from its source with stub state.
$runnerAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repo 'scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1'),[ref]$null,[ref]$null)
$stageCheck=@($runnerAst.FindAll({param($n)$n-is[Management.Automation.Language.IfStatementAst]-and$n.Extent.Text.StartsWith('if($null-ne$upgradeSection-and')-and$n.Extent.Text.Contains('$coStagedState.packageSha256')},$true))
Check 'the runner refuses a staged package other than the step''s declared package' {if($stageCheck.Count-ne1){throw 'Post-stage check not found.'}
 $upgradeSection=$u2Section;$coStagedState=[pscustomobject]@{packageSha256=$pins.packageSha256};& ([scriptblock]::Create($stageCheck[0].Extent.Text))} $true 'declared package'
Check 'the runner admits the step''s exact declared package' {$upgradeSection=$u1Section;$coStagedState=[pscustomobject]@{packageSha256=$pins.packageSha256};& ([scriptblock]::Create($stageCheck[0].Extent.Text))}

# --- Runner admission: refused before intake, approval, lock or launch ----------
$runner=Join-Path $repo 'scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1'
$lockSentinel=Join-Path $realLab 'runtime-state/active-transaction.lock'
function Invoke-Runner([string[]]$Arguments){
 $lockBefore=Test-Path -LiteralPath $lockSentinel;$ErrorActionPreference='Continue'
 $text=(& powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $runner @Arguments 2>&1|ForEach-Object{"$_"})-join"`n"
 $code=$LASTEXITCODE;if((Test-Path -LiteralPath $lockSentinel)-ne$lockBefore){throw 'A runner invocation changed the transaction sentinel.'}
 [pscustomobject]@{code=$code;text=($text-replace'\s+',' ')}
}
$np=@('-Scenario','chunk7-normal-player-control','-BaseGameOnly','-SaveAccessAllowed','-ObserverPackagePath',$fakeObserver,'-TimeoutSeconds','900','-WhatIf')
foreach($m in @(
 @('a rehearsal step outside the normal-player control',@('-Scenario','chunk7-owned-two-pair-campaign','-UpgradeRehearsalStep','u1-previous-install','-WhatIf'),'step of the request-free normal-player control only'),
 @('a step without its rehearsal id',($np+@('-UpgradeRehearsalStep','u1-previous-install')),'names its shared rehearsal id'),
 @('a first install with a carry source',($np+@('-UpgradeRehearsalStep','u1-previous-install','-UpgradeRehearsalId',$rid,'-UpgradeCarrySourceRunId',$u1)),'first install takes no carry source'),
 @('an upgrade without its first-install run',($np+@('-UpgradeRehearsalStep','u2-upgrade')),'first install takes no carry source'),
 @('a carry source without a step',($np+@('-UpgradeCarrySourceRunId',$u1)),'first install takes no carry source'))){
 Check ('the runner refuses '+$m[0]) {$r=Invoke-Runner $m[1];if($r.code-eq0){throw 'Runner accepted.'};throw $r.text} $true $m[2]
}

$failures=@($results|Where-Object{-not$_.pass}).Count
[ordered]@{nativeInvoked=$false;checks=$results.Count;failures=$failures;results=$results}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Output ('UPGRADE REHEARSAL PASS='+($results.Count-$failures)+' FAIL='+$failures)
if($failures-ne0){throw 'Upgrade rehearsal checks failed.'}
