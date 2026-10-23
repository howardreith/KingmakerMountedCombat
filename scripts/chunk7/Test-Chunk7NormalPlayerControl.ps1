[CmdletBinding()]
param([string]$OutputDirectory)
# Offline checks of the request-free normal-player control: the closed request
# descriptor, the runner-built observer request read by the observer's actual
# C# request type, and the closed reader of the observer's own result. No game.
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('normal-player-'+[Guid]::NewGuid().ToString('N'))}
$OutputDirectory=Assert-KmcChildPath $OutputDirectory $chunk7AnalysisRoot 'normal-player checks'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve prior normal-player checks.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$results=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false,[string]$Pattern=''){
 if($Refuse-and-not$Pattern){throw ('Refusal check lacks its expected message: '+$Name)}
 $errorText=$null;$ok=$false
 try{&$Body;$ok=-not$Refuse}catch{$errorText=$_.Exception.Message;$ok=$Refuse-and$errorText-cmatch$Pattern}
 $results.Add([pscustomobject]@{name=$Name;pass=$ok;error=$errorText})
 Write-Output ($(if($ok){'PASS '}else{'FAIL '})+$Name+$(if(-not$ok){': '+$errorText}else{''}))
}
function Clone($Value){$Value|ConvertTo-Json -Depth 60|ConvertFrom-Json}

# --- Request -----------------------------------------------------------------
$base=Read-KmcJson (Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261022-c7b-world190-b190s1-sixmeasure/runtime-request.json')
$load=(Read-KmcJson (Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261022-c7b-world185-b185s1-sixthhorse-cold/runtime-request.json')).persistenceLoad
$fakeObserver=Join-Path $OutputDirectory 'observer.zip';[IO.File]::WriteAllText($fakeObserver,'observer bytes');[IO.File]::WriteAllText($fakeObserver+'.manifest.json','{}')
$observerManifest=[pscustomobject]@{version='0.1.0-observer.6';commit=('b'*40);dllSha256=('c'*64);dllMvid='11111111-2222-3333-4444-555555555555'}
$source=[pscustomobject]@{qualificationRunId='20261022-c7b-world185-b185s1-sixthhorse-cold';archiveProducerRunId='20261022-c7b-world184-b184s1-sixthhorse-manual'}
$request=Clone $base;$request.scenario='chunk7-normal-player-control';$request.PSObject.Properties.Remove('ownedPairs');$request.persistenceLoad=Clone $load
$request|Add-Member normalPlayer ([pscustomobject](New-KmcChunk7NormalPlayerSection $source $fakeObserver $observerManifest|ConvertTo-Json -Depth 8|ConvertFrom-Json))
Check 'closed request descriptor accepts the request-free control on Manual308' {Assert-KmcChunk7NormalPlayerDescriptor $request}
$descriptorRefusal='Normal-player request differs from the closed request-free control'
foreach($m in @(
 @('a KMC automation request',{param($r)$r.normalPlayer.kmcAutomationRequest=$true}),
 @('another archive',{param($r)$r.persistenceLoad.sha256=('0'*64)}),
 @('a different driven rider',{param($r)$r.normalPlayer.riderId=$r.normalPlayer.neighborRiderId}),
 @('a foreign source run',{param($r)$r.normalPlayer.sourceRunId='other'}),
 @('a malformed observer identity',{param($r)$r.normalPlayer.observer.packageSha256='x'}))){
 Check ('closed descriptor refuses '+$m[0]) {$r=Clone $request;&$m[1] $r;Assert-KmcChunk7NormalPlayerDescriptor $r} $true $descriptorRefusal
}
Check 'closed descriptor refuses an extra selection' {$r=Clone $request;$r|Add-Member ownedPairs $null;Assert-KmcChunk7NormalPlayerDescriptor $r} $true 'normal-player request'
Check 'closed descriptor refuses an unclosed fixture descriptor' {$r=Clone $request;$r.persistenceLoad|Add-Member extra 1;Assert-KmcChunk7NormalPlayerDescriptor $r} $true 'normal-player fixture descriptor'
Check 'closed descriptor refuses another fixture campaign name' {$r=Clone $request;$r.persistenceLoad.gameName='Other';Assert-KmcChunk7NormalPlayerDescriptor $r} $true $descriptorRefusal
Check 'closed descriptor refuses another scenario' {$r=Clone $request;$r.scenario='chunk7-owned-two-pair-campaign';Assert-KmcChunk7NormalPlayerDescriptor $r} $true 'belongs only to its own scenario'

# --- Observer request through the observer's actual C# request type ----------
# The package manifest is the run candidate's: the request carries the same DLL identity.
$manifest=[pscustomobject]@{commit=('a'*40);version='0.1.0-chunk7b-preview.94';dllSha256=[string]$request.dllSha256;dllMvid=[string]$request.dllMvid;packageSha256=('e'*64)}
$observerRequest=New-KmcChunk7NormalPlayerObserverRequest $request ('f'*64) $OutputDirectory (Join-Path $OutputDirectory 'profile') 'C:\Game\Mods' $manifest
$probeRoot=Join-Path $OutputDirectory 'probe';[void][IO.Directory]::CreateDirectory($probeRoot)
function Save-Probe([string]$Name,$Value){$path=Join-Path $probeRoot ($Name+'.json');[IO.File]::WriteAllText($path,($Value|ConvertTo-Json -Depth 20),[Text.UTF8Encoding]::new($false));$path}
$probes=[ordered]@{'valid'=Save-Probe 'valid' $observerRequest}
$v=Clone $observerRequest;$v.archive.sha256=('0'*64);$probes['wrong-archive']=Save-Probe 'wrong-archive' $v
$v=Clone $observerRequest;$v.normalPlayer.mountAbilityGuid=('0'*32);$probes['wrong-ability']=Save-Probe 'wrong-ability' $v
$v=Clone $observerRequest;$v.normalPlayer.neighborMountId=$v.normalPlayer.mountId;$probes['aliased-roles']=Save-Probe 'aliased-roles' $v
$v=Clone $observerRequest;$v.schemaVersion=1;$probes['removal-schema-with-section']=Save-Probe 'removal-schema-with-section' $v
$v=Clone $observerRequest;$v|Add-Member surprise 1;$probes['unknown-member']=Save-Probe 'unknown-member' $v
$repoObserver=Join-Path $repo 'tools/KmcRemovalObserver'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputDirectory 'request-probe.exe'
&$compiler /nologo /target:exe /langversion:7.3 "/reference:$json" "/out:$exe" (Join-Path $repoObserver 'ObserverRequest.cs') (Join-Path $repoObserver 'ObserverIdentity.cs') (Join-Path $chunk7TestSource 'NormalPlayerRequestTemplate.cs')
if($LASTEXITCODE-ne0){throw 'Observer request type compilation failed.'}
Copy-Item -LiteralPath $json -Destination (Join-Path $OutputDirectory 'Newtonsoft.Json.dll')
# The game's Newtonsoft build initializes a Unity vector converter on first use;
# only the managed Unity metadata is loaded, nothing native is invoked.
foreach($leaf in @('UnityEngine.dll','UnityEngine.CoreModule.dll')){Copy-Item -LiteralPath (Join-Path (Split-Path $json) $leaf) -Destination (Join-Path $OutputDirectory $leaf)}
$lines=@(&$exe @($probes.Values))
function Verdict([string]$Name){$line=@($lines|Where-Object{$_.StartsWith($Name+'.json ')});if($line.Count-ne1){throw ('No probe verdict for '+$Name)};$line[0]}
Check 'the runner-built observer request is valid to the observer as schema 3' {if((Verdict 'valid')-cne'valid.json VALID normalPlayer=True'){throw (Verdict 'valid')}}
foreach($pair in @(@('wrong-archive','not the cold-qualified six-Horse fixture'),@('wrong-ability','native KMC Mount and Dismount'),@('aliased-roles','not distinct'),@('removal-schema-with-section','exactly schema 3'))){
 Check ('the observer refuses its request with '+$pair[0]) {$text=Verdict $pair[0];if($text-notmatch'INVALID'-or$text-notmatch[regex]::Escape($pair[1])){throw $text}}
}
Check 'the observer refuses an unknown request member' {if((Verdict 'unknown-member')-notmatch'REFUSED'){throw (Verdict 'unknown-member')}}

# --- Observer result reader --------------------------------------------------
$n=$request.normalPlayer;$profile=Join-Path $OutputDirectory 'profile'
$observerRequestHash='9'*64;$process=4242;$notBefore=[DateTimeOffset]::Parse('2026-10-22T20:00:00Z')
function Presence([string]$Label){[pscustomobject][ordered]@{label=$Label;kmcRuntimeArguments=@();observerDllSha256=[string]$n.observer.dllSha256;observerMvid=[string]$n.observer.dllMvid;modEntries=@('KingmakerMountedCombat:active','KmcRemovalObserver:active');kmcAssemblies=1
 kmcLocation='C:\Game\Mods\KingmakerMountedCombat\KingmakerMountedCombat.dll';kmcDllSha256=[string]$request.dllSha256;kmcMvid=[string]$request.dllMvid;kmcHarmonyOwners=@('KingmakerMountedCombat.Feasibility')
 rootPresent=$true;automationHostField='null';staticAutomationHost='null';measurementSession='null';movementTelemetry='null';overlayObjects=0
 effectiveSettings=[pscustomobject]@{EnableDiagnosticOverlay=$false;EnableMountedCharge=$false;EnablePairedActivation=$true}}}
$good=[pscustomobject][ordered]@{schemaVersion=1;evidenceKind='kmc-normal-player-observer';runId=[string]$request.runId;transactionToken=[string]$request.transactionToken
 processId=$process;startedAtUtc='2026-10-22T20:00:10Z';completedAtUtc='2026-10-22T20:03:00Z';status='PASS';observerVersion=[string]$observerRequest.observer.version
 observerAssembly='KmcRemovalObserver';requestSha256=$observerRequestHash;stage=7
 checks=@(Get-KmcChunk7NormalPlayerChecks|ForEach-Object{[pscustomobject]@{name=$_;passed=$true}});checkPassCount=@(Get-KmcChunk7NormalPlayerChecks).Count;checkFailCount=0;errors=@()
 observations=[pscustomobject][ordered]@{
  routing=[pscustomobject]@{savePath=(Join-Path $profile 'Saved Games');stashFolder=(Join-Path $profile 'Areas');observedSavePath=(Join-Path $profile 'Saved Games');observedStashFolder=(Join-Path $profile 'Areas')}
  presenceAtMenu=(Presence 'menu');profileBefore=[pscustomobject]@{saves=@()};world=[pscustomobject]@{gameId=[string]$load.gameId;area=[string]$load.area}
  controlsBefore=[pscustomobject]@{label='before'};presenceLoaded=(Presence 'loaded')
  frameWindows=@([pscustomobject]@{label='idle-unmounted';meanMs=11.0},[pscustomobject]@{label='idle-mounted';meanMs=11.1})
  inputs=@(
   [pscustomobject]@{label='mount';casterId=$n.riderId;targetId=$n.mountId;clicked=$true;commandCreated=$true;result='Success';attachedTo=$n.mountId;riderMountDistance=0.1;neighborUnchanged=$true},
   [pscustomobject]@{label='mounted-move';moverId=$n.riderId;riderDisplacement=3.9;mountDisplacement=3.9;attachedTo=$n.mountId;neighborUnchanged=$true},
   [pscustomobject]@{label='dismount';casterId=$n.riderId;targetId=$n.riderId;clicked=$true;commandCreated=$true;result='Success';attachedTo=$null;neighborUnchanged=$true},
   [pscustomobject]@{label='unmounted-move';moverId=$n.riderId;riderDisplacement=3.8;mountDisplacement=1.0;attachedTo=$null;neighborUnchanged=$true})
  controlsAfter=[pscustomobject]@{label='after'};presenceAfter=(Presence 'after')
  archive=[pscustomobject]@{path=(Join-Path (Join-Path $profile 'Saved Games') ([string]$load.fileName));sha256Before=[string]$load.sha256;sha256After=('7'*64);changedOnDisk=$true};profileAfter=[pscustomobject]@{saves=@()}
  log=[pscustomobject]@{kmcErrors=0;kmcLines=1}}}
$obsReq=Clone $observerRequest;$obsReq.profileRoot=$profile
function Read-Result($Value){Assert-KmcChunk7NormalPlayerObserverResult $request $obsReq $Value $observerRequestHash $process $notBefore}
Check 'closed reader accepts an exact request-free normal-player PASS' {Read-Result $good}
Check 'closed reader accepts an honest FAIL whose status follows its failed check' {$r=Clone $good;$r.status='FAIL';$r.checks[0].passed=$false;$r.checkPassCount--;$r.checkFailCount=1;$r.errors=@('Check failed');Read-Result $r}
foreach($m in @(
 @('rejected Mount input',{param($r)$r.observations.inputs[0].commandCreated=$false},'rejected, unresolved or touched the neighbor'),
 @('an unresolved Dismount shell',{param($r)$r.observations.inputs[2].result='Failure'},'rejected, unresolved or touched the neighbor'),
 @('wrong-target attachment',{param($r)$r.observations.inputs[0].attachedTo=$n.neighborMountId},'did not attach the rider to its own Horse'),
 @('a moved neighbor pair',{param($r)$r.observations.inputs[1].neighborUnchanged=$false},'did not move the exact pair together'),
 @('a mounted move without the Horse',{param($r)$r.observations.inputs[1].mountDisplacement=0.2},'did not move the exact pair together'),
 @('an unmounted move still attached',{param($r)$r.observations.inputs[3].attachedTo=$n.mountId},'Unmounted movement authority'),
 @('a live automation host',{param($r)$r.observations.presenceLoaded.staticAutomationHost='present'},'ordinary no-request path'),
 @('a KMC automation argument',{param($r)$r.observations.presenceAtMenu.kmcRuntimeArguments=@('-kmcRuntimeRequest')},'ordinary no-request path'),
 @('a diagnostic overlay',{param($r)$r.observations.presenceAfter.overlayObjects=1},'ordinary no-request path'),
 @('a third loaded mod',{param($r)$r.observations.presenceAtMenu.modEntries=@('KingmakerMountedCombat:active','KmcRemovalObserver:active','Other:active')},'ordinary no-request path'),
 @('a substituted observer assembly',{param($r)$r.observations.presenceLoaded.observerDllSha256=('0'*64)},'ordinary no-request path'),
 @('a non-candidate KMC assembly',{param($r)$r.observations.presenceLoaded.kmcDllSha256=('0'*64)},'ordinary no-request path'),
 @('another starting archive',{param($r)$r.observations.archive.sha256Before=('0'*64)},'fixture changed or KMC logged'),
 @('an archive outside the owned copy',{param($r)$r.observations.archive.path='C:\Users\Someone\Saved Games\x.zks'},'not the owned fixture copy'),
 @('a KMC error',{param($r)$r.observations.log.kmcErrors=1},'fixture changed or KMC logged'),
 @('reordered inputs',{param($r)$r.observations.inputs=@($r.observations.inputs[1],$r.observations.inputs[0],$r.observations.inputs[2],$r.observations.inputs[3])},'exact ordered native sequence'),
 @('a missing check',{param($r)$r.checks=@($r.checks|Select-Object -Skip 1);$r.checkPassCount--},'differs from its exact declared set'),
 @('PASS over a failed check',{param($r)$r.checks[0].passed=$false;$r.checkPassCount--;$r.checkFailCount=1},'does not follow its own checks'),
 @('another process',{param($r)$r.processId=1},'does not bind this run'),
 @('another observer request',{param($r)$r.requestSha256=('8'*64)},'does not bind this run'),
 @('routing outside the owned profile',{param($r)$r.observations.routing.observedSavePath='C:\Users\Someone\Saved Games'},'inside the owned profile'))){
 Check ('closed reader refuses '+$m[0]) {$r=Clone $good;&$m[1] $r;Read-Result $r} $true $m[2]
}
# --- Runner wiring: the guarded runner's own parameter set admits the scenario ---
Check 'the guarded runner Scenario parameter admits the normal-player control' {
 $ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repo 'scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1'),[ref]$null,[ref]$null)
 $param=@($ast.ParamBlock.Parameters|Where-Object{$_.Name.VariablePath.UserPath-ceq'Scenario'})
 $set=@($param[0].Attributes|Where-Object{$_.TypeName.Name-ceq'ValidateSet'})
 if($set.Count-ne1-or@($set[0].PositionalArguments|Where-Object{$_.Value-ceq'chunk7-normal-player-control'}).Count-ne1){throw 'Runner Scenario ValidateSet lacks the normal-player control.'}
 if(@(Get-KmcSaveBackedRuntimeScenarios)-cnotcontains'chunk7-normal-player-control'){throw 'Save-backed allowlist lacks the normal-player control.'}
}# --- Archive members: the real batch191 load rewrote only the native header ---
. (Join-Path $repo 'scripts/runtime/PersistenceValidationFixtures.ps1')
$b191Copy='C:\Dev\KingmakerMountedCombatLab\runtime-staging\persistence-20261023-c7b-world191-b191s1-normalplayer\Saved Games\Manual_308_KMC_C7_SIX_HORSE_WORLD.zks'
$sourcePath=(Get-KmcChunk7SixPairCheckpointSource $request.fixture).path
Check 'the real batch191 native load changed only the fixture header member' {
 if((Get-KmcSha256 $b191Copy)-cne'7de070b0ad379ab4a04c96a901812d2aa1976c55faf1220372095de76c41fc24'){throw 'batch191 copy changed'}
 $m=Assert-KmcChunk7NormalPlayerArchiveMembers $sourcePath $b191Copy;if((@($m.changed)-join',')-cne'header.json'){throw ('unexpected '+(@($m.changed)-join','))}
}
Check 'an unchanged source compared with itself passes the member check' {[void](Assert-KmcChunk7NormalPlayerArchiveMembers $sourcePath $sourcePath)}# --- KMC's own one-session UMM log ----------------------------------------
$fakeGame=Join-Path $OutputDirectory 'game';$logDir=Join-Path $fakeGame 'Kingmaker_Data/Managed/UnityModManager';[void][IO.Directory]::CreateDirectory($logDir)
$quiet=@('[Manager] Initialize.','[Manager] Version: 0.33.2.0.','[KingmakerMountedCombat] Version ''0.1.0-chunk7b-preview.94''. Loading.',
 '[KingmakerMountedCombat] Kingmaker Mounted Combat 0.1.0-chunk7b-preview.94 loaded with transient private-alpha mounted melee services.',
 '[KingmakerMountedCombat] Private-alpha services and native mounted abilities enabled; diagnostic overlay=False.','[KingmakerMountedCombat] Active.')
function Log-Summary([string[]]$Lines){[IO.File]::WriteAllText((Join-Path $logDir 'Log.txt'),($Lines-join"`r`n"),[Text.UTF8Encoding]::new($false));Get-KmcChunk7NormalPlayerUmmLogSummary $fakeGame ([DateTimeOffset]::UtcNow.AddMinutes(-5)) '0.1.0-chunk7b-preview.94'}
Check 'an ordinary quiet no-request UMM session passes' {if(-not(Log-Summary $quiet).passed){throw 'quiet log refused'}}
foreach($m in @(@('an accepted automation request',@($quiet+'[KingmakerMountedCombat] Runtime automation request accepted: x / y')),
 @('a KMC error line',@($quiet+'[KingmakerMountedCombat] [Error] something failed')),@('a KMC exception line',@($quiet+'[KingmakerMountedCombat] [Exception] System.NullReferenceException: x')),
 @('a second session',@($quiet+'[Manager] Version: 0.33.2.0.')),@('a missing ordinary load line',@($quiet|Where-Object{$_-notmatch' loaded with '})),
 @('another product version',@($quiet|ForEach-Object{$_.Replace('preview.94 loaded','preview.93 loaded')})),@('a visible overlay',@($quiet|ForEach-Object{$_.Replace('overlay=False','overlay=True')})))){
 Check ('the UMM session check refuses '+$m[0]) {if((Log-Summary $m[1]).passed){throw 'accepted'}}
}
Check 'the UMM session check refuses a log older than the run' {[IO.File]::WriteAllText((Join-Path $logDir 'Log.txt'),($quiet-join"`r`n"));(Get-Item (Join-Path $logDir 'Log.txt')).LastWriteTimeUtc=[DateTime]::UtcNow.AddHours(-2);Get-KmcChunk7NormalPlayerUmmLogSummary $fakeGame ([DateTimeOffset]::UtcNow.AddMinutes(-5)) '0.1.0-chunk7b-preview.94'} $true 'not written by this run'
$failures=@($results|Where-Object{-not$_.pass}).Count
[ordered]@{nativeInvoked=$false;checks=$results.Count;failures=$failures;results=$results}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Output ('NORMAL-PLAYER CONTROL PASS='+($results.Count-$failures)+' FAIL='+$failures)
if($failures-ne0){throw 'Normal-player control checks failed.'}
