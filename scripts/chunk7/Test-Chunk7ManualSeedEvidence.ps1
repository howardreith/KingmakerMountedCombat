[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixturePath,[string]$OutputDirectory,[switch]$FixturesOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('FixturePath')){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/BootstrapInventoryEvidenceFixture.json'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/manual-seed-reader-'+[Guid]::NewGuid().ToString('N'))}
$seedReader=[IO.Path]::GetFullPath($ReaderDirectory);$seedFixture=[IO.Path]::GetFullPath($FixturePath);$seedOutput=[IO.Path]::GetFullPath($OutputDirectory)
. (Join-Path $seedReader 'RuntimeHarness.Common.ps1')
function Copy-SeedJson($Value){return ,(ConvertTo-Json -InputObject $Value -Depth 100 -Compress|ConvertFrom-Json)}
function New-ManualSeedFixture {
 $old=[IO.File]::ReadAllText($seedFixture)|ConvertFrom-Json
 $f=[pscustomobject]@{request=(Copy-SeedJson $old.request);artifact=(Copy-SeedJson $old.artifact);source=$old.source;inventory=$old.artifact;sourceRequest=$old.sourceRequest;inventoryRequest=$old.request}
 $r=$f.request;$a=$f.artifact;$o=$a.observations
 $r.runId='synthetic-manual-seed';$r.scenario='chunk7-bootstrap-manual-seed';$r.evidenceRoot=Join-Path (Split-Path $old.request.evidenceRoot -Parent) $r.runId
 $s=[pscustomobject]@{case='native-save-only';sourceRunId=$r.bootstrapInventory.sourceRunId;sourceArtifactSha256=$r.bootstrapInventory.sourceArtifactSha256;sourceArchiveSha256=$r.bootstrapInventory.sourceArchiveSha256;inventoryRunId=$old.request.runId;inventoryArtifactSha256=('e'*64);baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=1;expectedManualNumber=1}
 $r.PSObject.Properties.Remove('bootstrapInventory');$r|Add-Member -NotePropertyName manualSeed -NotePropertyValue $s
 $a.runId=$r.runId;$a.scenario=$r.scenario;$a.evidenceKind='chunk7-manual-seed-evidence';$o.selection=Copy-SeedJson $s;$o.processId=3000
 $o|Add-Member -NotePropertyName sourceColdInventory -NotePropertyValue (Copy-SeedJson $f.inventory.observations)
 $root=Join-Path (Join-Path (Join-Path (Split-Path (Split-Path $r.evidenceRoot -Parent) -Parent) 'runtime-staging') ('persistence-'+$r.runId)) 'Saved Games'
 foreach($row in @($o.beforeLoad,$o.loadedWorld,$o.settledWorld,$o.cleanup)){$row.authority.root=$root;$row.authority.maximumNativeWrites=1}
 $o.fixtureReadiness.isolatedSaveDescriptors.root=$root
 $o.settledWorld.frame=32;$o.settledWorld.snapshots=1;$o.settledWorld.completedSaves=1;$o.settledWorld.authority.admittedNativeWrites=1;$o.settledWorld.authority.authorizedWrites=1
 $o.cleanup.authority.admittedNativeWrites=1;$o.cleanup.authority.authorizedWrites=1
 $o|Add-Member -NotePropertyName savedWorld -NotePropertyValue (Copy-SeedJson $o.settledWorld);$o.savedWorld.frame=31
 $o.nativeLoad.identities.ids=@(1..16);$o.nativeLoad.identities.retainedCount=16
 $leaf='Manual_1_KMC_C7_NATIVE_SEED.zks';$path=Join-Path $root $leaf
 $o|Add-Member -NotePropertyName nativeNumbering -NotePropertyValue ([pscustomobject]@{method='FindUnusedSaveNumber';token='06008024';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';frame=24;nativeSaveType='Manual';number=1;fileName=$leaf;sourceDescriptors=(Copy-SeedJson $o.fixtureReadiness.isolatedSaveDescriptors.descriptors);outputInitiallyAbsent=$true;slotProjectionEnabled=$false})
 $o|Add-Member -NotePropertyName saveRequested -NotePropertyValue ([pscustomobject]@{frame=25;descriptor=11;name='KMC_C7_NATIVE_SEED';type='Manual';nativeSaveAllowed=$true;newSlot=$true;snapshotCount=0;authority=(Copy-SeedJson $o.loadedWorld.authority)})
 $o|Add-Member -NotePropertyName nativeWrite -NotePropertyValue ([pscustomobject]@{path=$path;fileName=$leaf;internalName='KMC_C7_NATIVE_SEED';type='Manual';sha256=('f'*64);length=200;lastWriteTimeUtcTicks=638960000000000010;gameId=$r.persistenceLoad.gameId;gameName=$r.persistenceLoad.gameName;area=$r.persistenceLoad.area;requestedDescriptor=11;writtenDescriptor=12;completionSequence=1;callbackCount=1;operationState='None';hasFileOnDisk=$true;isActuallySaved=$true;autoLevelup=$false;nativeWorkerSettled=$true;snapshotCount=1;slotProjectionEnabled=$false})
 $kinds=@('native-save-request','native-save-prepared','snapshot-starting','snapshot-staged','native-worker-completed','native-save-callback');$events=@()
 for($i=0;$i-lt6;$i++){$events+=,[pscustomobject]@{sequence=($i+1);kind=$kinds[$i];frame=(25+$i);game=1;saveManager=2;player=4;requested=11;descriptor=$(if($i-eq0){11}else{12});fileName=$(if($i-eq0){$null}else{$leaf});internalName='KMC_C7_NATIVE_SEED';type='Manual';gameId=$(if($i-eq0){$null}else{$r.persistenceLoad.gameId});gameName=$r.persistenceLoad.gameName;area=$r.persistenceLoad.area;path=$(if($i-eq0){$null}else{$path});snapshotCount=$(if($i-lt3){0}else{1});completedSaveCount=$(if($i-lt5){0}else{1})}}
 foreach($kv in @{scope=13;iterator=14;iteratorType='Kingmaker.EntitySystem.Persistence.SaveManager+<SaveRoutine>d__46';worker=15;workerId=1;nativeWorkerMatches=$true;workerStatus='RanToCompletion';workerCompleted=$true;workerFaulted=$false;workerCanceled=$false}.GetEnumerator()){$events[4]|Add-Member -NotePropertyName $kv.Key -NotePropertyValue $kv.Value}
 $hooks=@([pscustomobject]@{type='Kingmaker.EntitySystem.Persistence.SaveManager';method='SaveRoutine';token='06008029';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},[pscustomobject]@{type='Kingmaker.EntitySystem.Persistence.SaveManager';method='PrepareSave';token='06008025';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},[pscustomobject]@{type='KingmakerMountedCombat.Integration.MountedPersistenceService';method='RecordCompletedSave';token='06001234';moduleMvid=$r.dllMvid})
 $o|Add-Member -NotePropertyName nativeSave -NotePropertyValue ([pscustomobject]@{contract='manual-seed-native-save-v1';installed=$true;installationAttempted=$true;complete=$true;requests=1;prepares=1;starts=1;stages=1;completions=1;callbacks=1;events=$events;hooks=$hooks;errors=@()})
 $o|Add-Member -NotePropertyName settledDescriptors -NotePropertyValue @([pscustomobject]@{descriptor=16;fileName='Auto_1.zks';internalName=$r.persistenceLoad.internalName;type='Auto';gameId=$r.persistenceLoad.gameId;gameName=$r.persistenceLoad.gameName;area=$r.persistenceLoad.area;isActuallySaved=$true;hasFileOnDisk=$true;operationState='None';autoLevelup=$false},[pscustomobject]@{descriptor=12;fileName=$leaf;internalName='KMC_C7_NATIVE_SEED';type='Manual';gameId=$r.persistenceLoad.gameId;gameName=$r.persistenceLoad.gameName;area=$r.persistenceLoad.area;isActuallySaved=$true;hasFileOnDisk=$true;operationState='None';autoLevelup=$false})
 $arrangement=[pscustomobject]@{money=$o.nativeProfileInventory.ordinaryAcquisitionPrerequisites.nativeMoney;experienceRatePercent=$o.fixtureReadiness.experienceRatePercent;actors=@([pscustomobject]@{actorId=$o.loadedWorld.mainActorId;blueprint=$o.loadedWorld.party[0].blueprint;experience=0;characterLevel=1;classes=@([pscustomobject]@{classGuid=$o.fixtureReadiness.mainProgression.classes[0].classGuid;level=1})})}
 $o|Add-Member -NotePropertyName beforeSaveArrangement -NotePropertyValue $arrangement;$o|Add-Member -NotePropertyName afterSaveArrangement -NotePropertyValue (Copy-SeedJson $arrangement)
 return $f
}
function Set-ManualSeedLabels($F,[scriptblock]$Map){
 $o=$F.artifact.observations
 foreach($key in @('game','saveManager','menuPlayer')){$o.beforeLoad.$key=&$Map $o.beforeLoad.$key}
 foreach($w in @($o.loadedWorld,$o.savedWorld,$o.settledWorld)){foreach($key in @('game','saveManager','player','state','areaState','mainState')){$w.$key=&$Map $w.$key};foreach($a in $w.party){foreach($key in @('object','descriptor','view')){$a.$key=&$Map $a.$key}}}
 foreach($e in $o.nativeLoad.events){foreach($key in @('game','saveManager','player')){$e.$key=&$Map $e.$key}}
 $o.nativeLoad.identities.ids=@($o.nativeLoad.identities.ids|ForEach-Object{&$Map $_})
 $o.saveRequested.descriptor=&$Map $o.saveRequested.descriptor
 foreach($key in @('requestedDescriptor','writtenDescriptor')){$o.nativeWrite.$key=&$Map $o.nativeWrite.$key}
 foreach($e in $o.nativeSave.events){foreach($key in @('game','saveManager','player','requested','descriptor','scope','iterator','worker')){if($null-ne$e.PSObject.Properties[$key]){$e.$key=&$Map $e.$key}}}
 foreach($d in $o.settledDescriptors){$d.descriptor=&$Map $d.descriptor}
}
function Check-ManualSeed($f){Assert-KmcChunk7ManualSeedEvidence $f.request $f.artifact $f.source $f.inventory}
if($FixturesOnly){return}
[void][IO.Directory]::CreateDirectory($seedOutput);$script:passes=0;$script:failures=0
function Good([string]$Name,[scriptblock]$Action){try{&$Action;$script:passes++;Write-Host ('PASS '+$Name)}catch{$script:failures++;Write-Host ('FAIL '+$Name+': '+$_)}}
function Bad([string]$Name,[scriptblock]$Action){$rejected=$false;try{&$Action}catch{$rejected=$true};if($rejected){$script:passes++;Write-Host ('PASS rejects '+$Name)}else{$script:failures++;Write-Host ('FAIL accepted '+$Name)}}
Good 'exact native Manual derivative without arrangement' {$f=New-ManualSeedFixture;Check-ManualSeed $f}
Good 'native prepared descriptor differs from original request' {$f=New-ManualSeedFixture;if($f.artifact.observations.nativeWrite.requestedDescriptor-eq$f.artifact.observations.nativeWrite.writtenDescriptor){throw 'fixture invalid'};Check-ManualSeed $f}
Good 'native labels above cardinality bound' {$f=New-ManualSeedFixture;Set-ManualSeedLabels $f {param($v)[int]($v+1000)};Check-ManualSeed $f}
Good 'native negative signed labels' {$f=New-ManualSeedFixture;Set-ManualSeedLabels $f {param($v)[int](-$v)};Check-ManualSeed $f}
Good 'native Int32 endpoint labels' {$f=New-ManualSeedFixture;Set-ManualSeedLabels $f {param($v)if($v-eq1){[int]::MinValue}else{[int]([int]::MaxValue-$v)}};Check-ManualSeed $f}
$cases=[ordered]@{
 'zero object label'={param($f)Set-ManualSeedLabels $f {param($v)if($v-eq1){0}else{$v}}}
 'overflow object label'={param($f)Set-ManualSeedLabels $f {param($v)if($v-eq1){2147483648}else{$v}}}
 'worker aliases old menu player'={param($f)$f.artifact.observations.nativeSave.events[4].worker=$f.artifact.observations.beforeLoad.menuPlayer}

 'old source run reused'={param($f)$f.request.manualSeed.inventoryRunId=$f.request.manualSeed.sourceRunId}
 'second write allowed'={param($f)$f.request.manualSeed.expectedNativeWriteCount=2}
 'second load allowed'={param($f)$f.request.manualSeed.expectedNativeLoadCount=2}
 'guessed Manual number'={param($f)$f.request.manualSeed.expectedManualNumber=300}
 'not base only'={param($f)$f.request.manualSeed.baseGameOnly=$false}
 'foreign cold candidate'={param($f)$f.inventory.dllMvid='11111111-1111-4111-8111-111111111111'}
 'cold receipt FAIL'={param($f)$f.inventory.status='FAIL'}
 'source cold observations substituted'={param($f)$f.artifact.observations.sourceColdInventory.loadedWorld.gameId=$f.request.fixture.working.gameId}
 'same cold process'={param($f)$f.artifact.observations.processId=$f.inventory.observations.processId}
 'one-save mode'={param($f)$f.artifact.observations.fixtureReadiness.saveSettings.onlyOneSave=$true}
 'observed native numbering differs'={param($f)$f.artifact.observations.nativeNumbering.number=2}
 'output already existed'={param($f)$f.artifact.observations.nativeNumbering.outputInitiallyAbsent=$false}
 'slot projection'={param($f)$f.artifact.observations.nativeNumbering.slotProjectionEnabled=$true}
 'native write category'={param($f)$f.artifact.observations.nativeWrite.type='Auto'}
 'alias native input'={param($f)$f.artifact.observations.nativeWrite.sha256=$f.request.persistenceLoad.sha256}
 'foreign output path'={param($f)$f.artifact.observations.nativeWrite.path='C:\Virtual\foreign.zks'}
 'missing settled input'={param($f)$f.artifact.observations.settledDescriptors=@($f.artifact.observations.settledDescriptors[1])}
 'foreign native descriptor'={param($f)$f.artifact.observations.settledDescriptors[0].fileName='Auto_2.zks'}
 'prepared descriptor not observed'={param($f)$f.artifact.observations.nativeWrite.writtenDescriptor=17}
 'requested prepared alias'={param($f)$f.artifact.observations.nativeWrite.writtenDescriptor=11}
 'worker aliases world'={param($f)$f.artifact.observations.nativeSave.events[4].worker=5}
 'foreign completion hook'={param($f)$f.artifact.observations.nativeSave.hooks[2].moduleMvid='11111111-1111-4111-8111-111111111111'}
 'foreign native token'={param($f)$f.artifact.observations.nativeSave.hooks[0].token='06008030'}
 'foreign callback manager'={param($f)$f.artifact.observations.nativeSave.events[5].saveManager=1}
 'callback before worker'={param($f)$f.artifact.observations.nativeSave.events[5].frame=28}
 'native worker mismatched'={param($f)$f.artifact.observations.nativeSave.events[4].nativeWorkerMatches=$false}
 'faulted worker'={param($f)$f.artifact.observations.nativeSave.events[4].workerFaulted=$true}
 'canceled worker'={param($f)$f.artifact.observations.nativeSave.events[4].workerCanceled=$true}
 'wrong iterator'={param($f)$f.artifact.observations.nativeSave.events[4].iteratorType='Other'}
 'second callback'={param($f)$f.artifact.observations.nativeSave.callbacks=2}
 'snapshot skipped'={param($f)$f.artifact.observations.nativeSave.events[3].snapshotCount=0}
 'save not completed'={param($f)$f.artifact.observations.savedWorld.completedSaves=0}
 'native preparation after save'={param($f)$f.artifact.observations.noPreparation.events=@([pscustomobject]@{kind='prepare-before'})}
 'native write grants debt'={param($f)$f.artifact.observations.settledWorld.semanticRestores=1}
 'resource grant'={param($f)$f.artifact.observations.resourceWrites=1}
 'XP grant'={param($f)$f.artifact.observations.afterSaveArrangement.actors[0].experience=1}
 'money grant'={param($f)$f.artifact.observations.afterSaveArrangement.money=100}
 'XP rate changes'={param($f)$f.artifact.observations.afterSaveArrangement.experienceRatePercent=200}
 'class rank changes'={param($f)$f.artifact.observations.afterSaveArrangement.actors[0].classes[0].level=2}
 'source changed'={param($f)$f.artifact.observations.cleanup.sourceUnchanged=$false}
 'registry released early'={param($f)$f.artifact.observations.nativeLoad.identities.released=$true}
 'identity registry fault'={param($f)$f.artifact.observations.nativeLoad.identities.faults=1}
 'unfinished observer cleanup'={param($f)$f.artifact.observations.cleanup.observersReleased=$false}
}
foreach($pair in $cases.GetEnumerator()){Bad $pair.Key {$f=New-ManualSeedFixture;&$pair.Value $f;Check-ManualSeed $f}}
foreach($path in @('nativeSave.requests','nativeSave.events.4.workerId','saveRequested.descriptor','nativeNumbering.number','nativeWrite.completionSequence','savedWorld.snapshots','beforeSaveArrangement.money')){
 Bad ('numeric string '+$path){$f=New-ManualSeedFixture;$o=$f.artifact.observations;$keys=$path.Split('.');for($i=0;$i-lt$keys.Length-1;$i++){if($o-is[Array]){$o=$o[[int]$keys[$i]]}else{$o=$o.($keys[$i])}};$o.($keys[-1])=[string]$o.($keys[-1]);Check-ManualSeed $f}
}
foreach($path in @('nativeSave.contract','nativeSave.events.4.iteratorType','nativeSave.events.5.gameId','nativeNumbering.method','nativeWrite.type','nativeWrite.path','saveRequested.name')){
 Bad ('scalar array '+$path){$f=New-ManualSeedFixture;$o=$f.artifact.observations;$keys=$path.Split('.');for($i=0;$i-lt$keys.Length-1;$i++){if($o-is[Array]){$o=$o[[int]$keys[$i]]}else{$o=$o.($keys[$i])}};$o.($keys[-1])=@($o.($keys[-1]));Check-ManualSeed $f}
}
foreach($phase in @('beforeLoad','loadedWorld','savedWorld','settledWorld','cleanup')){Bad ('wrong quota '+$phase){$f=New-ManualSeedFixture;$f.artifact.observations.$phase.authority.maximumNativeWrites=2;Check-ManualSeed $f}}
@{schemaVersion=1;pass=$script:passes;fail=$script:failures;readerSha256=(Get-FileHash -LiteralPath (Join-Path $seedReader 'Chunk7ManualSeedEvidence.ps1')).Hash.ToLowerInvariant();fixtureSha256=(Get-FileHash -LiteralPath $seedFixture).Hash.ToLowerInvariant();nativeExecution=$false;archiveAccess=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $seedOutput 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:passes+' FAIL='+$script:failures)
if($script:failures-ne0){throw 'Manual seed strict reader failed'}
