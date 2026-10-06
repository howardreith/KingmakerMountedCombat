[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/Chunk6bPersistenceEvidence.ps1')
$script:passed=0
function CopyJson($x){ $x|ConvertTo-Json -Depth 80 -Compress|ConvertFrom-Json }
function Accept([string]$Label,[scriptblock]$Body){ & $Body; $script:passed++; Write-Host ('PASS '+$Label) }
function Refuse([string]$Label,[scriptblock]$Body){ $rejected=$false;try{& $Body}catch{$rejected=$true};if(-not$rejected){throw ('Accepted invalid artifact: '+$Label)};$script:passed++;Write-Host ('PASS refuses '+$Label) }
function Actor([string]$Id,[double]$Standard=0){ [ordered]@{Id=$Id;Standard=$Standard;Move=0;Swift=0;Initiative=0;Reaction=0;ReactionsRemaining=1;LastSurpriseTicks=0} }
function BuffProof { @{buffAcquisitionStarted=$true;buffAcquisitionObserved=$true;buffOutstanding=$false;buffCallbackDebt=$null;buffRuleDispatchSettled=$true
 buffNative=@{identity=100;collection=200;inCollection=$false;active=$false;disposed=$true;turnedOn=$false;activating=$false;deactivating=$false;recalculating=$false;listening=0;statModifiers=0;attachedModifiers=0;componentCount=0;componentData=$false;storedFacts=0;storedModifiers=0;parentContext=$false;currentContext=$false}
 buffChildren=@{schema=1;rider='rider';rootIdentity=100;rootCollection=200;surface='native-base';retiring=$true;scopeSettled=$true;drained=$true;fault=$null;failures=@();facts=@()}
 buffComponentTypes=@('Kingmaker.UnitLogic.FactLogic.AddStatBonus','Kingmaker.UnitLogic.FactLogic.AddCondition','Kingmaker.Designers.Mechanics.Facts.AttackOfOpportunityAttackBonus')
 buffCondition=@{componentObserved=$true;condition=40;drained=$true;contributions=0;fault=$null;additions=1;removals=1;nativeExceptions=0;operations=@(
  @{addition=$true;before=0;after=1;mutationObserved=$true;completed=$true;returnedNormally=$true},
  @{addition=$false;before=3;after=2;mutationObserved=$true;completed=$true;returnedNormally=$true})}} }
function Fixture([string]$Case,[bool]$Cold=$false){
 $scenario=if($Cold){'persistence-p04-load'}else{'persistence-p04-save'}
 $process=if($Cold){102}else{101}
 $request=[ordered]@{runId='owned-run';scenario=$scenario;persistenceCase=$Case;commit='source';dllSha256='dll';fixture=@{working=@{gameId='campaign';area='area'}}}
 $owner=[ordered]@{identity=17;rider='rider';mount='mount';target='target';state='FullyDrained';attempts=2;committed=$true;commandTerminal=$true;riderSlotReleased=$true;riderContainerReleased=$true;schedulerAbsent=$true;carrierDrained=$true;leaseDrained=$true;lease=(BuffProof);shellTerminal=$true;shellContainerReleased=$true;manualTargetOwned=$false;manualTargetReleased=$true;processEnded=$true;processObserved=$true;debt=''}
 $shell=@{acted=$true;finished=$true;type='UnitUseAbility';abilityGuid='d79eaec224a7a832e738eb81baef9d49';executor='rider';target='target'}
 $charge=[ordered]@{owner=@{owned=$false};lastDrained=$owner;shell=$shell;shellActionType='Standard';shellFullRound=$true;command=@{finished=$true};admitted=1;refused=0;mountCharging=$false;riderCharging=$false;mountSpeedOverride=$null;chargeBuffCount=0;riderStandardSlot=$null;mountMoveSlot=$null;shellProcessEnded=$true;faultFired=$Case-cin@('mounted-charge-failed','mounted-charge-drained');faultObserved=$Case-ceq'mounted-charge-drained';nativeStopSent=$Case-ceq'mounted-charge-cancelled';costMax=@{riderStandard=6;riderMove=0;mountStandard=0;mountMove=0};straightRoute=$true;landingBlocked=$false;workerRunning=$false;callback=$false;
  actors=@(@{id='rider';prepared=$true;inCombat=$true;native=(Actor 'rider' 5)},@{id='mount';prepared=$true;inCombat=$true;native=(Actor 'mount')});
  input=@{clicked=$true;availableForCast=$true;abilityGuid='d79eaec224a7a832e738eb81baef9d49'};
  readiness=@{prepared=$true;canAct=$true;riderCommandsEmpty=$true;mountCommandsEmpty=$true;available=$true;canTarget=$true};
  snapshot=@{owner=@{owned=$false};lastDrained=$owner;riderCommandsEmpty=$true;mountCommandsEmpty=$true;unresolvedAbilities=$false;gameTicks=100;faultObserved=$true}}
 $resolved=if(-not$Cold-and$Case-ceq'mounted-charge-settled'){1}else{0}
 if($Cold){$charge.lastDrained=$null;$charge.shell=$null;$charge.command=$null;$charge.admitted=0}
 $actual=[ordered]@{charge=$charge;resolved=$resolved;ordinaryAttacks=$resolved;forcedD20=0;riderRounds=0;riderCommandsEmpty=$true;mountCommandsEmpty=$true;unresolvedAbilities=$false;unresolvedProjectiles=$false;target='target';targetDamage=7;snapshotCount=1;rules=@{mountAttackRules=0;attackRuleEvents=@($(if($resolved-eq1){@{charge=$true;fullAttack=$false;actorId='rider';attackOfOpportunity=$false}}))};approach=@{moving=$false}}
 $snapshot=@{Rider=(Actor 'rider' 5);Mount=(Actor 'mount');Mounted=$true;CampaignId='campaign';AreaId='area';GameTimeTicks=100;Combat=@{TurnBased=$false;Current=$null;Roster=@();Paired=$null;Actors=@(@{Native=(Actor 'rider' 5);Prepared=$true;InCombat=$true},@{Native=(Actor 'mount');Prepared=$true;InCombat=$true})}}
 $rows=New-Object 'System.Collections.Generic.List[object]'
 function AddRow([string]$Kind,$Detail){$rows.Add((CopyJson ([ordered]@{runId='owned-run';scenario=$scenario;checkpoint=$Case;source='source';dll='dll';processId=$process;kind=$Kind;relationship='Mounted';rider=(Actor 'rider' 5);mount=(Actor 'mount');gameTicks=100;controls=@{DuplicateFactCount=0;NativeCastRequestCount=0};native=@{tbSetting=$false;tbInitialized=$false};persistence=@{semantics=2;presentation=1};detail=$Detail}))) }
 AddRow 'initial' $actual
 if(-not$Cold){
  AddRow 'charge-readiness' $actual
  AddRow 'charge-before-input' $actual;$rows[$rows.Count-1].rider.Standard=0
  AddRow 'charge-input' $actual;$rows[$rows.Count-1].detail.charge.owner=CopyJson @{owned=$true;identity=17}
  AddRow 'charge-commit-observed' $actual
  if($Case-cin@('mounted-charge-pending','mounted-charge-cancelled','mounted-charge-drained')){
   AddRow 'charge-live-before-boundary' $actual
   $live=$rows[$rows.Count-1].detail;$live.charge.command.finished=$false;$live.charge.mountCharging=$true;$live.approach.moving=$true;$live.snapshotCount=0
  }
  if($Case-ceq'mounted-charge-drained'){
   AddRow 'charge-cleanup-debt-held' $actual
   $held=$rows[$rows.Count-1].detail;$held.charge.owner=CopyJson @{owned=$true;state='FaultedCleanup';debt='mount-speed-override'};$held.snapshotCount=0;$held.charge.mountSpeedOverride=12
  }
  AddRow 'rt-before-save' $actual;AddRow 'rt-native-save-requested' $actual
 }
 AddRow $(if($Cold){'rt-cold-debt-restored'}else{'native-write-complete'}) @{snapshot=$snapshot;actual=$actual;sha256='archive-hash';length=123}
 AddRow 'rt-spent-attack-queued' $actual;AddRow 'rt-native-debt-wait' $actual
 foreach($i in 1,2){AddRow 'rt-later-attack' $actual;$rows[$rows.Count-1].detail.resolved=$resolved+$i;$rows[$rows.Count-1].detail.riderRounds=$i}
 AddRow 'usable-continuation-complete' $actual;$rows[$rows.Count-1].detail.resolved=$resolved+2
 [pscustomobject]@{request=(CopyJson $request);rows=@($rows.ToArray());game=[pscustomobject]@{processId=$process}}
}
function Run($f){Assert-KmcChargePersistenceRows $f.request $f.rows $f.game}
foreach($case in (Get-KmcChargePersistenceCases)){
 Accept ($case+' source facts') {Run (Fixture $case)}
 Accept ($case+' cold facts') {Run (Fixture $case $true)}
}
function Mutate([string]$Label,[scriptblock]$Change){$f=Fixture 'mounted-charge-drained'; & $Change $f; Refuse $Label {Run $f}}
foreach($post in @('commandTerminal','riderSlotReleased','riderContainerReleased','schedulerAbsent','carrierDrained','leaseDrained','shellTerminal','shellContainerReleased','processEnded','manualTargetReleased')){
 Mutate ('unresolved '+$post) {param($f) (ChargeSaveRow $f.rows 'native-write-complete').detail.actual.charge.lastDrained.$post=$false}
}
Mutate 'cleanup owner exchanged' {param($f) (ChargeSaveRow $f.rows 'native-write-complete').detail.actual.charge.lastDrained.identity=18}
Mutate 'native process never observed' {param($f) (ChargeSaveRow $f.rows 'native-write-complete').detail.actual.charge.lastDrained.processObserved=$false}
Mutate 'duplicated actor observations' {param($f) $c=(ChargeSaveRow $f.rows 'native-write-complete').detail.actual.charge;$c.actors[1]=$c.actors[0]}
Mutate 'duplicated snapshot actor' {param($f) $s=(ChargeSaveRow $f.rows 'native-write-complete').detail.snapshot;$s.Combat.Actors[1]=$s.Combat.Actors[0]}
Mutate 'root snapshot mount exchanged' {param($f) (ChargeSaveRow $f.rows 'native-write-complete').detail.snapshot.Mount.Id='other'}
Mutate 'duplicated Standard debt' {param($f) (ChargeSaveRow $f.rows 'native-write-complete').detail.actual.charge.costMax.riderStandard=12}
Mutate 'wrong native shell' {param($f) (ChargeSaveRow $f.rows 'charge-commit-observed').detail.charge.shell.abilityGuid='foreign'}
Mutate 'non-full-round charge' {param($f) (ChargeSaveRow $f.rows 'charge-commit-observed').detail.charge.shellFullRound=$false}
Mutate 'half-owned snapshot' {param($f) (ChargeSaveRow $f.rows 'native-write-complete').detail.actual.charge.snapshot.owner.owned=$true}
Mutate 'snapshot captured during fault' {param($f) (ChargeSaveRow $f.rows 'charge-cleanup-debt-held').detail.snapshotCount=1}
Mutate 'worker launched during fault' {param($f) (ChargeSaveRow $f.rows 'charge-cleanup-debt-held').detail.charge.workerRunning=$true}
Mutate 'no real speed residue held' {param($f) (ChargeSaveRow $f.rows 'charge-cleanup-debt-held').detail.charge.mountSpeedOverride=0}
Mutate 'action refund before capture' {param($f) (ChargeSaveRow $f.rows 'native-write-complete').rider.Standard=0}
Mutate 'mount charged for transport' {param($f) (ChargeSaveRow $f.rows 'native-write-complete').detail.actual.charge.costMax.mountMove=3}
Mutate 'buff residue' {param($f) (ChargeSaveRow $f.rows 'native-write-complete').detail.actual.charge.chargeBuffCount=1}
Mutate 'serialized transaction' {param($f) (ChargeSaveRow $f.rows 'native-write-complete').detail.snapshot|Add-Member chargeTransaction @{live=$true}}
Mutate 'preparation replay' {param($f) (ChargeSaveRow $f.rows 'native-write-complete').detail.actual.charge.actors[0].prepared=$false}
Mutate 'extra continuation attack' {param($f) (ChargeSaveRow $f.rows 'usable-continuation-complete').detail.resolved=3}
$cold=Fixture 'mounted-charge-drained' $true
(ChargeSaveRow $cold.rows 'rt-cold-debt-restored').detail.actual.charge.shell=CopyJson @{acted=$true}
Refuse 'cold charge shell resurrection' {Run $cold}
$source=Fixture 'mounted-charge-settled';$cold=Fixture 'mounted-charge-settled' $true
Accept 'exact fresh-process charge outcome' {Assert-KmcChargeColdOutcome $source.rows $cold.rows ([pscustomobject]@{sha256='archive-hash';length=123})}
Refuse 'cold archive exchanged' {Assert-KmcChargeColdOutcome $source.rows $cold.rows ([pscustomobject]@{sha256='different';length=123})}
foreach($field in @('Move','Swift','ReactionsRemaining')){
 $changed=CopyJson $cold.rows
 (ChargeSaveRow $changed 'rt-cold-debt-restored').detail.snapshot.Combat.Actors[1].Native.$field=7
 Refuse ('cold snapshot '+$field+' changed') {Assert-KmcChargeColdOutcome $source.rows $changed ([pscustomobject]@{sha256='archive-hash';length=123})}
}
$settled=Fixture 'mounted-charge-settled'
(ChargeSaveRow $settled.rows 'native-write-complete').detail.actual.rules.attackRuleEvents[0].charge=$false
Refuse 'settled attack without charge rule' {Run $settled}
(ChargeSaveRow $cold.rows 'rt-cold-debt-restored').detail.actual.targetDamage=0
Refuse 'cold lost charge damage' {Assert-KmcChargeColdOutcome $source.rows $cold.rows ([pscustomobject]@{sha256='archive-hash';length=123})}
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
foreach($case in (Get-KmcChargePersistenceCases)){
 foreach($file in @('src/KingmakerMountedCombat/Diagnostics/RuntimeProtocol.cs','scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1','scripts/runtime/Test-RuntimeRequest.ps1','scripts/runtime/PersistenceSaveFixtures.ps1')){
  Accept ($case+' exact registration '+$file) {if(-not[IO.File]::ReadAllText((Join-Path $repo $file)).Contains($case)){throw 'Exact registration missing'}}
 }
}
function LifecycleFixture([string]$Case){
 $base=Fixture 'mounted-charge-drained'
 $rows=New-Object 'System.Collections.Generic.List[object]'
 foreach($kind in @('initial','charge-readiness','charge-before-input','charge-input','charge-commit-observed','charge-live-before-boundary')){
  $row=CopyJson (ChargeSaveRow $base.rows $kind);$row.scenario='persistence-p07-save';$row.checkpoint=$Case
  $rows.Add($row)
 }
 $rows[1].rider.Standard=0
 $c=CopyJson (ChargeSaveRow $base.rows 'native-write-complete').detail.actual.charge
 foreach($entry in @{riderCommandsEmpty=$true;mountCommandsEmpty=$true;mountMoving=$false;mountPathPresent=$false;nativeBoundaries=@{pending=$false;fault=$null}}.GetEnumerator()){
  $c|Add-Member -NotePropertyName $entry.Key -NotePropertyValue $entry.Value
 }
 $d=[ordered]@{case=$Case;riderId='rider';mountId='mount';sameWorld=$true;sourceArea='area';area='area';loading=$false;loadingObserved=$false;relationship='Mounted';charge=$c;
  request=@{};riderAttacks=0;riderResolved=0;mountAttacks=0;enabled=$true;patchesInstalled=$true;resetPending=$false;resetDeferred=0;areaRefused=0;areaSuspensions=0;areaResumes=0;snapshotCount=0;fixtureReleased=$false;removalState='Idle';cleanupSaves=0;cleanupPath='cleanup-path';cleanupSha256=('a'*64);cleanupLeaf='Manual_301_KMC_CLEANUP.zks';cleanupBinding='bound';cleanupReferences=@();cleanupScannedMembers=1}
 function AddLife([string]$Kind,$Detail){
  $row=CopyJson $rows[0];$row.kind=$Kind;$row.detail=CopyJson $Detail;$rows.Add($row)
 }
 $d.charge.owner=CopyJson @{owned=$true;identity=17;state='Active';debt=''}
 AddLife 'charge-lifecycle-before' $d
 $d.charge.owner.state='FaultedCleanup';$d.charge.owner.debt='mount-speed-override';$d.charge.mountSpeedOverride=12
 if($Case-ceq'mounted-charge-area'){$d.areaRefused=1;$d.request=@{method='Game.ReloadArea:06000CD6';carryEligibleAtRetry=$false}}
 if($Case-ceq'mounted-charge-session'){$d.resetPending=$true;$d.resetDeferred=1;$d.request=@{method='Game.ResetToMainMenu:06000CDD'}}
 if($Case-ceq'mounted-charge-disable'){$d.request=@{disableAccepted=$false;unloadAccepted=$false;settledDisableAccepted=$true;reenableAccepted=$true}}
 if($Case-ceq'mounted-charge-removal'){$d.request=@{removalAccepted=$false;nativeStopSent=$true;nativeStopRefused=$true;nativeStopError='Mounted charge cleanup remains owned: mount-speed-override';retainedDebtAssessment=@{safe=$false;activeCommand=$false;ownedCharge=$true;worldActiveCommand=$true;reasons=@('active')}}}
 AddLife 'charge-lifecycle-held' $d;AddLife 'charge-lifecycle-debt-retained' $d
 AddLife 'charge-lifecycle-retry-ready' $d
 $d.charge.owner=CopyJson @{owned=$false};$d.charge.mountSpeedOverride=$null
 AddLife 'charge-lifecycle-cleanup-complete' $d
 AddLife 'charge-lifecycle-drained' $d
 if($Case-ceq'mounted-charge-area'){AddLife 'charge-lifecycle-area-retry' $d;$d.loadingObserved=$true;$d.relationship='Unmounted'}
 if($Case-ceq'mounted-charge-session'){$d.resetPending=$false;$d.area=$null;$d.relationship='Unmounted';$d.fixtureReleased=$true}
 if($Case-ceq'mounted-charge-disable'){$d.enabled=$false;$d.relationship='Unmounted';AddLife 'charge-lifecycle-disabled' $d;$d.enabled=$true}
 if($Case-ceq'mounted-charge-removal'){AddLife 'charge-removal-opening-write' @{path='opening';sha256=('b'*64);length=50};$d.removalState='Ready';$d.cleanupSaves=1;$d.snapshotCount=2;$d.relationship='Unmounted';$d.fixtureReleased=$true}
 AddLife 'charge-lifecycle-complete' $d
 if($Case-ceq'mounted-charge-session'){$rows[$rows.Count-1].rider=$null;$rows[$rows.Count-1].mount=$null}
 $base.request.scenario='persistence-p07-save';$base.request.persistenceCase=$Case
 [pscustomobject]@{request=$base.request;rows=@($rows.ToArray());game=$base.game}
}
function RunLife($f){Assert-KmcChargeLifecycleRows $f.request $f.rows $f.game}
foreach($case in (Get-KmcChargeLifecycleCases)){Accept ($case+' complete native boundary facts') {RunLife (LifecycleFixture $case)}}
function MutateLife([string]$Label,[scriptblock]$Change,[string]$Case='mounted-charge-disable'){
 $f=LifecycleFixture $Case;& $Change $f;Refuse $Label {RunLife $f}
}
MutateLife 'UMM unload succeeded over cleanup debt' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-held').detail.request.unloadAccepted=$true}
MutateLife 'double Standard at lifecycle commitment' {param($f)(ChargeSaveRow $f.rows 'charge-live-before-boundary').detail.charge.costMax.riderStandard=12}
MutateLife 'mount charged at lifecycle commitment' {param($f)(ChargeSaveRow $f.rows 'charge-live-before-boundary').detail.charge.costMax.mountMove=3}
MutateLife 'successful cleanup retry refunded Standard' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-cleanup-complete').rider.Standard=0}
MutateLife 'successful cleanup retry lost old world observation' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-cleanup-complete').detail.sameWorld=$false}
MutateLife 'removal ignored terminal cleanup debt' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-debt-retained').detail.request.retainedDebtAssessment.worldActiveCommand=$false} 'mounted-charge-removal'
MutateLife 'native world disposed before cleanup' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-debt-retained').detail.sameWorld=$false}
MutateLife 'snapshot during native cleanup fault' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-held').detail.snapshotCount=1}
MutateLife 'native worker during cleanup fault' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-held').detail.charge.workerRunning=$true}
MutateLife 'cleanup owner replaced across lifecycle' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-drained').detail.charge.lastDrained.identity=22}
MutateLife 'native controller boundary still queued' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-complete').detail.charge.nativeBoundaries.pending=$true}
MutateLife 'charge resumed after disable' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-complete').detail.riderAttacks=1}
MutateLife 'carrier forced path survives disable' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-drained').detail.charge.mountPathPresent=$true}
MutateLife 'rider command queue survives disable' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-drained').detail.charge.riderCommandsEmpty=$false}
MutateLife 'native action refunded while fault held' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-debt-retained').rider.Standard=0}
MutateLife 'area transfer never loaded' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-complete').detail.loadingObserved=$false} 'mounted-charge-area'
MutateLife 'session request never deferred' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-held').detail.resetPending=$false} 'mounted-charge-session'
MutateLife 'menu departure still has a world' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-complete').detail.area='area'} 'mounted-charge-session'
MutateLife 'removal accepted during live charge' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-held').detail.request.removalAccepted=$true} 'mounted-charge-removal'
MutateLife 'cleanup archive carries KMC reference' {param($f)(ChargeSaveRow $f.rows 'charge-lifecycle-complete').detail.cleanupReferences=@('charge-blueprint')} 'mounted-charge-removal'
function AbsenceFixture {
 $proof=@{runId='removal-source';observationsSha256=('a'*64);riderId='rider';mountId='mount';chargeBuffGuid=('b'*32)}
 $request=@{schemaVersion=2;chargeSource=$proof;kmcBlueprintGuids=@('4016c7db400ab721ff125aef9e65e202','7db7c50677e39f09feef56f3831fc723','98e651899e6278d938de77af1d69bd32','6874a165bf8bda3531ee4e2abc10c899','f053faad986631688defa003cd7bda0e','3af2b81f4d72bbb30501fa730fcdf36e','27364df661b3c121eabb97a31aa73a83','f88a50d6fdbebbd709c3e323d2f52f5e','d79eaec224a7a832e738eb81baef9d49')}
 $a=@{id='rider';matches=1;blueprint='native';viewPresent=$true;agentPresent=$true;raw=@();queue=@();standard=$null;move=$null;pathPresent=$false;moving=$false;mountCharging=$false;riderCharging=$false;speedOverride=$null;chargeBuffs=0;kmcAbilities=@()}
 $m=CopyJson $a;$m.id='mount'
 $facts=@{source=$proof;rider=$a;mount=$m;gameTicks=100;processes=@();attacks=@();dropped=0}
 CopyJson @{request=$request;observations=@{chargeLoaded=$facts;chargeSettled=$facts}}
}
Accept 'no-DLL exact charge-source native absence' {$a=AbsenceFixture;Assert-KmcChargeAbsentFacts $a.request $a.observations}
foreach($field in @('pathPresent','moving','mountCharging','riderCharging')){
 $a=AbsenceFixture;$a.observations.chargeLoaded.mount.$field=$true
 Refuse ('no-DLL '+$field+' residue') {Assert-KmcChargeAbsentFacts $a.request $a.observations}
}
foreach($field in @('raw','queue','kmcAbilities')){
 $a=AbsenceFixture;$a.observations.chargeSettled.rider.$field=@('residue')
 Refuse ('no-DLL '+$field+' residue') {Assert-KmcChargeAbsentFacts $a.request $a.observations}
}
$a=AbsenceFixture;$a.observations.chargeLoaded.rider.chargeBuffs=1
Refuse 'no-DLL native Charge buff survived' {Assert-KmcChargeAbsentFacts $a.request $a.observations}
$a=AbsenceFixture;$a.observations.chargeSettled.mount.speedOverride=12
Refuse 'no-DLL charge speed survived' {Assert-KmcChargeAbsentFacts $a.request $a.observations}
$a=AbsenceFixture;$a.observations.chargeLoaded.rider.matches=2
Refuse 'no-DLL rider was duplicated' {Assert-KmcChargeAbsentFacts $a.request $a.observations}
$a=AbsenceFixture;$a.observations.chargeSettled.processes=@(@{ended=$false})
Refuse 'no-DLL ability process continued' {Assert-KmcChargeAbsentFacts $a.request $a.observations}
$a=AbsenceFixture;$a.observations.chargeSettled.attacks=@(@{actor='rider'})
Refuse 'no-DLL charge attack resumed during load' {Assert-KmcChargeAbsentFacts $a.request $a.observations}
$a=AbsenceFixture;$a.observations.chargeLoaded.source.observationsSha256=('c'*64)
Refuse 'no-DLL immutable source exchanged' {Assert-KmcChargeAbsentFacts $a.request $a.observations}
$a=AbsenceFixture;$a.request.kmcBlueprintGuids=@($a.request.kmcBlueprintGuids|Where-Object {$_-cne'd79eaec224a7a832e738eb81baef9d49'})
Refuse 'no-DLL Mounted Charge omitted from inventory' {Assert-KmcChargeAbsentFacts $a.request $a.observations}
# Bind the actual source-proof reader to on-disk archive, artifact, game and
# restoration receipts. All files are fresh synthetic inputs in ignored obj.
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceSaveFixtures.ps1')
$script:chargeProofLab=Join-Path ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))) ('obj/charge-removal-proof/'+[Guid]::NewGuid().ToString('N'))
function Get-KmcLabRoot { $script:chargeProofLab }
$f=LifecycleFixture 'mounted-charge-removal'
$root=Join-Path $script:chargeProofLab 'runtime-evidence/owned-run'
$saves=Join-Path $script:chargeProofLab 'runtime-staging/persistence-owned-run/Saved Games'
[void][IO.Directory]::CreateDirectory($root);[void][IO.Directory]::CreateDirectory($saves)
Add-Type -AssemblyName System.IO.Compression
foreach($leaf in @('Manual_300_KMC_P01.zks','Manual_301_KMC_CLEANUP.zks')){
 $stream=[IO.File]::Open((Join-Path $saves $leaf),[IO.FileMode]::CreateNew)
 $archive=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
 try{
  foreach($entry in @{ 'header.json'=@{GameId='campaign'};'kmc-mounted-state'=@{Mounted=$false;Combat=$null;Rider=$null;Mount=$null;Slots=@()} }.GetEnumerator()){
   $writer=[IO.StreamWriter]::new($archive.CreateEntry($entry.Key).Open())
   try{$writer.Write(($entry.Value|ConvertTo-Json -Depth 20 -Compress))}finally{$writer.Dispose()}
  }
 }finally{$archive.Dispose();$stream.Dispose()}
}
$opening=(ChargeSaveRow $f.rows 'charge-removal-opening-write').detail
$opening.path=Join-Path $saves 'Manual_300_KMC_P01.zks';$opening.sha256=Get-KmcSha256 $opening.path;$opening.length=(Get-Item $opening.path).Length
$done=(ChargeSaveRow $f.rows 'charge-lifecycle-complete').detail
$done.cleanupPath=Join-Path $saves 'Manual_301_KMC_CLEANUP.zks';$done.cleanupSha256=Get-KmcSha256 $done.cleanupPath
$done.charge|Add-Member -NotePropertyName chargeBuffGuid -NotePropertyValue ('b'*32)
$f.request|Add-Member -NotePropertyName transactionToken -NotePropertyValue ('a'*64)
$observations=Join-Path $root 'persistence-observations.jsonl'
[IO.File]::WriteAllLines($observations,[string[]]@($f.rows|ForEach-Object {$_|ConvertTo-Json -Depth 80 -Compress}),[Text.UTF8Encoding]::new($false))
Write-KmcJsonAtomic (Join-Path $root 'runtime-request.json') $f.request
$manifest=@{runId='owned-run';artifacts=@(@{relativePath='persistence-observations.jsonl';kind='persistence-evidence';sha256=(Get-KmcSha256 $observations);length=(Get-Item $observations).Length})}
$manifestPath=Join-Path $root 'runtime-artifacts.json';Write-KmcJsonAtomic $manifestPath $manifest
$game=@{processId=$f.game.processId;status='PASS';evidenceManifestSha256=(Get-KmcSha256 $manifestPath)}
$gamePath=Join-Path $root 'runtime-game-result.json';Write-KmcJsonAtomic $gamePath $game
$result=@{runId='owned-run';scenario='persistence-p07-save';transactionToken=('a'*64);commit='source';dllSha256='dll';status='PASS';modsRestored=$true;workingRestored=$true;saveProtectionPassed=$true;baselineImmutable=$true;saveWriteAllowlistPassed=$true;errors=@();gameResultSha256=(Get-KmcSha256 $gamePath)}
$resultPath=Join-Path $root 'runtime-result.json';Write-KmcJsonAtomic $resultPath $result
Accept 'charge removal source binds exact archive and immutable receipt chain' {
 $proof=Get-KmcChargeRemovalSourceProof 'owned-run' $done.cleanupSha256 'source' 'dll'
 if($proof.riderId-cne'rider'-or$proof.mountId-cne'mount'-or$proof.observationsSha256-cne(Get-KmcSha256 $observations)){throw 'Source proof lost identity'}
}
foreach($field in @('runId','transactionToken','commit','dllSha256','status','modsRestored','workingRestored','saveProtectionPassed','baselineImmutable','saveWriteAllowlistPassed','gameResultSha256')){
 $bad=CopyJson $result;$bad.$field=if($bad.$field-is[bool]){$false}else{'foreign'};Write-KmcJsonAtomic $resultPath $bad
 Refuse ('source receipt changed '+$field) {[void](Get-KmcChargeRemovalSourceProof 'owned-run' $done.cleanupSha256 'source' 'dll')}
}
Write-KmcJsonAtomic $resultPath $result
[IO.File]::AppendAllText($observations,"`n",[Text.UTF8Encoding]::new($false))
Refuse 'source facts changed after manifest binding' {[void](Get-KmcChargeRemovalSourceProof 'owned-run' $done.cleanupSha256 'source' 'dll')}
Accept 'native buff cleanup preserves changing foreign condition contributions' {Assert-KmcChargeBuffDrained (CopyJson (BuffProof))}
foreach($field in @('buffAcquisitionStarted','buffAcquisitionObserved','buffRuleDispatchSettled')){
 $proof=CopyJson (BuffProof);$proof.$field=$false
 Refuse ('buff cleanup lacks '+$field) {Assert-KmcChargeBuffDrained $proof}
}
foreach($mutation in @(
 {param($p)$p.buffCondition.operations[1].mutationObserved=$false},
 {param($p)$p.buffCondition.operations[1].after=1},
 {param($p)$p.buffCondition.operations[1].before='3'},
 {param($p)$p.buffCondition.operations[1].after=2.5},
 {param($p)$p.buffCondition.contributions=1},
 {param($p)$p.buffCondition.removals=0},
 {param($p)$p.buffCondition.fault='ambiguous'},
 {param($p)$p.buffComponentTypes=@('Kingmaker.UnitLogic.FactLogic.AddStatBonus')}
)){
 $proof=CopyJson (BuffProof);& $mutation $proof
 Refuse ('unproven native buff ownership: '+$mutation.ToString()) {Assert-KmcChargeBuffDrained $proof}
}
function CotwProof {
 $proof=CopyJson (BuffProof)
 $proof.buffChildren.surface='native-cotw-1.14.4c-2.1'
 $proof.buffComponentTypes=@('Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions')+@($proof.buffComponentTypes)+@('Kingmaker.UnitLogic.FactLogic.AddContextStatBonus','Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig')
 $nodes=@()
 for($i=0;$i-lt3;$i++){
  $kind=if($i-eq0){'buff'}else{'enchantment'}
  $native=CopyJson $proof.buffNative;$native.identity=101+$i
  if($i-gt0){$native.collection=300;$native.componentCount=1;$native.disposed=$false}
  $fx=$null
  if($i-gt0){$fx=@{scopes=0;pendingRoots=0;pendingCopies=0;retiring=$true;attached=$false;drained=$true;fault=$null;failures=@();roots=0;copies=0;rootFacts=@();acquisitions=@()}}
  if($i-eq1){$fx.roots=1;$fx.copies=2;$fx.rootFacts=@(@{generation=1;returned=$true;custodyUncertain=$false;controllerResidueAbsent=$true});$fx.acquisitions=@(1,2,3|ForEach-Object{@{generation=$_;returned=$true;completionRequested=$true;failure=$null}})}
  $nodes+=@{id=($i+1);parent=$(if($i-eq0){0}else{1});blueprint=@('b0439659723f4a8da680965c78a8fbf5','30f90becaaac51f41bf56641966c4121','3f032a3cd54e57649a0cdad0434bf221')[$i];kind=$kind;created=$true;acquiring=$false;acquired=$true;identityUncertain=$false;removalAttempted=$true;fault=$null;settled=$true;drained=$true;native=$native;visuals=$fx}
 }
 $proof.buffChildren.facts=@($nodes)
 return CopyJson $proof
}
Accept 'exact augmented buff graph with native child enchantments and retired visual generations' {Assert-KmcChargeBuffDrained (CotwProof)}
foreach($mutation in @(
 {param($p)$p.buffNative.inCollection=$true},
 {param($p)$p.buffNative.active=$true},
 {param($p)$p.buffNative.active='false'},
 {param($p)$p.buffChildren.surface='uninspected'},
 {param($p)$p.buffChildren.rootIdentity=999},
 {param($p)$p.buffChildren.facts[0].native.collection=999},
 {param($p)$p.buffChildren.facts[0].native.listening=1},
 {param($p)$p.buffChildren.facts[0].native.attachedModifiers=1},
 {param($p)$p.buffChildren.facts[0].native.storedFacts=1},
 {param($p)$p.buffChildren.facts[0].native.deactivating=$true},
 {param($p)$p.buffChildren.facts[0].identityUncertain=$true},
 {param($p)$p.buffChildren.facts[1].parent=0},
 {param($p)$p.buffChildren.facts[1].native.currentContext=$true},
 {param($p)$p.buffChildren.facts[1].visuals.scopes=1},
 {param($p)$p.buffChildren.facts[1].visuals.pendingRoots=1},
 {param($p)$p.buffChildren.facts[1].visuals.pendingCopies=1},
 {param($p)$p.buffChildren.facts[1].visuals.attached=$true},
 {param($p)$p.buffChildren.facts[1].visuals.acquisitions[0].returned=$false},
 {param($p)$p.buffChildren.facts[1].visuals.acquisitions[2].returned=$false},
 {param($p)$p.buffChildren.facts[1].visuals.rootFacts[0].controllerResidueAbsent=$false},
 {param($p)$p.buffChildren.facts[1].visuals.rootFacts[0].custodyUncertain=$true},
 {param($p)$p.buffChildren.facts[1].visuals.rootFacts[0].custodyUncertain='false'},
 {param($p)$p.buffChildren.facts[1].visuals.fault='ambiguous-generation'},
 {param($p)$p.buffChildren.facts[1].visuals.roots=2},
 {param($p)$p.buffChildren.facts[1].visuals.copies='2'}
)){
 $proof=CotwProof;& $mutation $proof
 Refuse ('native lifetime residue: '+$mutation.ToString()) {Assert-KmcChargeBuffDrained $proof}
}
Write-Host ('CHARGE PERSISTENCE READER PASS='+$script:passed+' FAIL=0; synthetic artifacts only, no native qualification')
