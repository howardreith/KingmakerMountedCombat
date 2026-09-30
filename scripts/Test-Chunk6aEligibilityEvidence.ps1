param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aPrimaryClaimEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
function Copy-Eligibility($Value){$Value|ConvertTo-Json -Depth 100|ConvertFrom-Json}
$original=Join-Path $lab 'runtime-evidence/c6a-envelope-a-obstruction/phase3d-horse-scenario-evidence.json';$originalHash=(Get-FileHash $original).Hash
$artifact=Get-Content -Raw $original|ConvertFrom-Json
$nativeSizePath=Join-Path $lab 'runtime-evidence/c6a-eligibility136-b-size-form/phase3d-horse-scenario-evidence.json'
$nativeSizeHash='d24133bb58936006acc45df27a3aa1fc2d6dd372920deb434ed6c036cc326e29'
if(-not(Test-Path -LiteralPath $nativeSizePath -PathType Leaf)-or(Get-FileHash -Algorithm SHA256 -LiteralPath $nativeSizePath).Hash.ToLowerInvariant()-cne$nativeSizeHash){throw 'Exact preview.136 size/form evidence is missing or changed'}
$nativeSizeArtifact=Get-Content -Raw -LiteralPath $nativeSizePath|ConvertFrom-Json
$nativeControlPath=Join-Path $lab 'runtime-evidence/c6a-eligibility137-c-lost-control/phase3d-horse-scenario-evidence.json'
$nativeControlHash='465279de2109e91163e4c423d540750750ba3044a15d0c8e4e33707ee7383451'
if(-not(Test-Path -LiteralPath $nativeControlPath -PathType Leaf)-or(Get-FileHash -Algorithm SHA256 -LiteralPath $nativeControlPath).Hash.ToLowerInvariant()-cne$nativeControlHash){throw 'Exact preview.137 lost-control evidence is missing or changed'}
$nativeControlArtifact=Get-Content -Raw -LiteralPath $nativeControlPath|ConvertFrom-Json
$nativeIncapacityPath=Join-Path $lab 'runtime-evidence/c6a-incapacity138-d-rider-incapacitated/phase3d-horse-scenario-evidence.json'
$nativeIncapacityHash='0d186aa46473ae287704609796cf3520506fc42204efbf14b7cc6aa19045c22e'
if(-not(Test-Path -LiteralPath $nativeIncapacityPath -PathType Leaf)-or(Get-FileHash -Algorithm SHA256 -LiteralPath $nativeIncapacityPath).Hash.ToLowerInvariant()-cne$nativeIncapacityHash){throw 'Exact preview.138 rider-incapacity failure evidence is missing or changed'}
$nativeIncapacityArtifact=Get-Content -Raw -LiteralPath $nativeIncapacityPath|ConvertFrom-Json
$nativeMountIncapacityPath=Join-Path $lab 'runtime-evidence/c6a-incapacity139-e-mount-incapacitated/phase3d-horse-scenario-evidence.json'
$nativeMountIncapacityHash='af7f83dbb3edf6f7030db8ccd82ff4b1373c15003f42e194901854e429d1bd72'
if(-not(Test-Path -LiteralPath $nativeMountIncapacityPath -PathType Leaf)-or(Get-FileHash -Algorithm SHA256 -LiteralPath $nativeMountIncapacityPath).Hash.ToLowerInvariant()-cne$nativeMountIncapacityHash){throw 'Exact preview.139 mount-incapacity failure evidence is missing or changed'}
$nativeMountIncapacityArtifact=Get-Content -Raw -LiteralPath $nativeMountIncapacityPath|ConvertFrom-Json
function New-EligibilityProof([string]$Contract){
 $proof=Copy-Eligibility $artifact.observations.chunk6aObstruction.commandProof;$proof.contract=$Contract;$terminal=$proof.samples[-1]
 $start=Copy-Eligibility $proof.preClick.state.geometry;$geometry=Copy-Eligibility $start
 $dx=[double]$geometry.horsePosition.x-[double]$geometry.riderPosition.x;$dz=[double]$geometry.horsePosition.z-[double]$geometry.riderPosition.z;$length=[Math]::Sqrt($dx*$dx+$dz*$dz)
 $geometry.riderPosition.x=[double]$geometry.riderPosition.x+0.5*$dx/$length;$geometry.riderPosition.z=[double]$geometry.riderPosition.z+0.5*$dz/$length
 $geometry.horizontalDistance=$length-0.5;$geometry.centerDistance=$length-0.5;$terminal.state.geometry=Copy-Eligibility $geometry
 [pscustomobject]@{proof=$proof;start=$start;geometry=$geometry;terminal=$terminal;trigger=[pscustomobject]@{approachObserved=$true;riderReallyMoving=$true;gameTicks=$terminal.gameTicks;frame=$terminal.frame;commandObject=$proof.identity.commandObject;moveSlotObject=$proof.identity.commandObject;started=$false;acted=$false;finished=$false;geometry=$geometry;riderDisplacement=0.5}}
}
function New-Command($Proof,[bool]$Terminal,[bool]$Started){[pscustomobject]@{id=$Proof.identity.commandObject;type='Kingmaker.UnitLogic.Commands.UnitUseAbility';executor=$Proof.identity.casterId;started=$Started;acted=$false;finished=$Terminal;result=$(if($Terminal){$Proof.nativeResult}else{'None'})}}

$sizeBase=New-EligibilityProof 'unacted-native-size-form-change-no-cost-or-transition';$sizeProof=$sizeBase.proof;$sizeCommand=New-Command $sizeProof $false $false;$buffObject=501
$sizeBefore=[pscustomobject]@{riderId=$sizeProof.identity.casterId;mountId=$sizeProof.identity.targetId;buffName='EnlargePersonBuff';buffGuid='11111111111111111111111111111111';changeUnitSizeComponents=1;buffCount=0;buffObjects=@();riderSize=4;mountSize=5;riderPolymorphObject=0;mountPolymorphObject=0}
$sizeActive=Copy-Eligibility $sizeBefore;$sizeActive.buffCount=1;$sizeActive.buffObjects=@($buffObject);$sizeActive.riderSize=5
$sizeCase=[pscustomobject]@{contract='native-size-fact-invalidates-exact-mount-approach';restored=$true;noResidue=$true;stimulusCount=1;removalCount=1;diagnosticInterruptCount=0;
 start=$sizeBase.start;trigger=$sizeBase.trigger;commandProof=$sizeProof;terminal=(New-Command $sizeProof $true $true);stateBefore=$sizeBefore;stateAfterApplication=(Copy-Eligibility $sizeActive);stateBeforeRemoval=(Copy-Eligibility $sizeActive);stateAfterRestoration=(Copy-Eligibility $sizeBefore);
 stimulus=[pscustomobject]@{contract='one-authored-enlarge-person-buff-through-native-rulebook';count=1;buffObject=$buffObject;before=(Copy-Eligibility $sizeBefore);after=(Copy-Eligibility $sizeActive);commandBefore=(Copy-Eligibility $sizeCommand);commandAfter=(Copy-Eligibility $sizeCommand);frameBefore=$sizeBase.terminal.frame;frameAfter=$sizeBase.terminal.frame;gameTicksBefore=$sizeBase.terminal.gameTicks;gameTicksAfter=$sizeBase.terminal.gameTicks};
 removal=[pscustomobject]@{contract='remove-only-exact-owned-native-buff-after-command-terminal';count=1;buffObject=$buffObject;frame=$sizeBase.terminal.frame;gameTicks=$sizeBase.terminal.gameTicks;after=(Copy-Eligibility $sizeBefore)}}

$controlBase=New-EligibilityProof 'unacted-native-lost-direct-control-no-cost-or-transition';$controlProof=$controlBase.proof;$controlCommand=New-Command $controlProof $false $false
function New-ControlState([bool]$Direct,[bool]$Panicked,[bool]$Frightened){[pscustomobject]@{riderId=$controlProof.identity.casterId;mountId=$controlProof.identity.targetId;directlyControllable=$Direct;inGame=$true;panicked=$Panicked;frightened=$Frightened;frightenedImmune=$false;visibleConsciousEnemies=1;visibleEnemyIds=@('enemy-1')}}
function New-Lease([string]$Phase){
 $immediate=$Phase-ceq'immediate';$lost=$Phase-ceq'lost'
 [pscustomobject]@{contract='owned-native-frightened-fact-awaits-unit-fear-controller';actorId=$controlProof.identity.casterId;blueprintId='22222222222222222222222222222222';templateId='33333333333333333333333333333333';factObject=601;templateComponentObject=602;activeComponentObject=603;componentType='Kingmaker.UnitLogic.FactLogic.AddCondition';condition='Frightened';activeFactCount=$(if($Phase-ceq'removed'){0}else{1});conditionActive=$($Phase-cne'removed');conditionImmune=$false;panicked=$(-not$immediate);directlyControllable=$immediate;onTurnOnToken='06002448';onTurnOffToken='06002449';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';disposed=$($Phase-ceq'removed')}
}
$lossBefore=[pscustomobject]@{conditionActive=$true;panicked=$false;directlyControllable=$true;commandObject=$controlProof.identity.commandObject;commandStarted=$false;commandActed=$false;commandFinished=$false;commandResult='None';commandProcessObject=0;moveSlotObject=$controlProof.identity.commandObject;moveSlotType='Kingmaker.UnitLogic.Commands.UnitUseAbility';commandsEmpty=$false;visibleConsciousEnemies=@('enemy-1')}
$lossAfter=Copy-Eligibility $lossBefore;$lossAfter.panicked=$true;$lossAfter.directlyControllable=$false;$lossAfter.commandFinished=$true;$lossAfter.commandResult=$controlProof.nativeResult;$lossAfter.moveSlotObject=777;$lossAfter.moveSlotType='Kingmaker.UnitLogic.Commands.UnitMoveTo'
$restoreBefore=Copy-Eligibility $lossAfter;$restoreBefore.conditionActive=$false
$restoreAfter=Copy-Eligibility $restoreBefore;$restoreAfter.panicked=$false;$restoreAfter.directlyControllable=$true;$restoreAfter.moveSlotObject=0;$restoreAfter.moveSlotType=$null;$restoreAfter.commandsEmpty=$true
$fearProbe=[pscustomobject]@{contract='installed-unit-fear-controller-removes-and-restores-direct-control';riderId=$controlProof.identity.casterId;commandObject=$controlProof.identity.commandObject;complete=$true;lossObserved=$true;restorationObserved=$true;lossCount=1;restorationCount=1;errors=@();observerHooks=@([pscustomobject]@{method='Kingmaker.Controllers.Units.UnitFearController.TickOnUnit';token='06009138';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';prefix='Before';postfix='After'});events=@([pscustomobject]@{ordinal=1;before=$lossBefore;after=$lossAfter;lossTransition=$true;restorationTransition=$false},[pscustomobject]@{ordinal=2;before=$restoreBefore;after=$restoreAfter;lossTransition=$false;restorationTransition=$true})}
$controlBefore=New-ControlState $true $false $false;$controlImmediate=New-ControlState $true $false $true;$controlLost=New-ControlState $false $true $true;$controlRemoved=New-ControlState $false $true $false;$controlRestored=New-ControlState $true $false $false
$controlCase=[pscustomobject]@{contract='native-fear-controller-invalidates-exact-mount-approach';restored=$true;noResidue=$true;stimulusCount=1;removalCount=1;diagnosticInterruptCount=0;
 start=$controlBase.start;trigger=$controlBase.trigger;commandProof=$controlProof;terminal=(New-Command $controlProof $true $false);stateBefore=$controlBefore;stateAtControlLoss=$controlLost;leaseBeforeRemoval=(New-Lease 'lost');leaseAfterRemoval=(New-Lease 'removed');stateAfterFactRemoval=$controlRemoved;fearController=$fearProbe;
 stimulus=[pscustomobject]@{contract='one-owned-native-frightened-fact';count=1;commandBefore=(Copy-Eligibility $controlCommand);lease=(New-Lease 'immediate');immediateState=$controlImmediate;frame=$controlBase.terminal.frame;gameTicks=$controlBase.terminal.gameTicks};
 restoration=[pscustomobject]@{contract='native-fear-controller-restores-control-after-owned-fact-removal';removalCount=1;state=$controlRestored;allocationEvents=@();noCostOrPreparationCallbacks=$true;frame=$controlBase.terminal.frame;gameTicks=$controlBase.terminal.gameTicks}}

function Set-IncapacityProducerClaims($Proof,$LifeEvents,[string]$Kind) {
 $subjectId=if($Kind-ceq'rider'){$Proof.identity.casterId}else{$Proof.identity.targetId}
 $clear=@($Proof.resourceWindow.events|Where-Object {$_.state.actor-cin@($Proof.identity.casterId,$Proof.mountId)-and[string]$_.boundary-cmatch'^(clear-|combat-clear)'})
 if($clear.Count-ne4){throw 'Test fixture lacks the exact native clear quartet.'}
 $life=@($LifeEvents.events)[0]
 $beforeCandidates=@()
 foreach($event in @($Proof.resourceWindow.events|Where-Object {$_.state.actor-ceq$subjectId-and[int]$_.sequence-lt[int]$clear[0].sequence})){$beforeCandidates+=@([pscustomobject]@{sequence=[int]$event.sequence;rank=1})}
 foreach($sample in @($Proof.samples|Where-Object {[int]$_.allocationSequence-lt[int]$clear[0].sequence})){$beforeCandidates+=@([pscustomobject]@{sequence=[int]$sample.allocationSequence;rank=2})}
 if([int]$Proof.preClick.allocationSequence-lt[int]$clear[0].sequence){$beforeCandidates+=@([pscustomobject]@{sequence=[int]$Proof.preClick.allocationSequence;rank=0})}
 $before=@($beforeCandidates|Sort-Object sequence,rank)[-1];$terminal=@($Proof.samples)[-1]
 $disposition=[pscustomobject]@{contract='native-incapacity-combat-removal-clears-initiative-only';declaredCommandContract=('unacted-native-'+$Kind+'-incapacitated-no-cost-or-transition');subjectId=$subjectId;lifeEventFrame=$life.frame;lifeEventGameTicks=$life.gameTicks;
  boundaries=@('combat-clear-before','clear-before','clear-after','combat-clear-after');sequences=@($clear|ForEach-Object {$_.sequence});
  actionResourcesUnchanged=$true;actionResourcesBridged=$true;actionBridgeBeforeSequence=$before.sequence;actionBridgeAfterSequence=$terminal.allocationSequence;
  reactionResourcesUnchanged=$true;initiativeCooldownCleared=$true;initiativeOrderCleared=$true;preparedCleared=$true;pass=$true;errors=@()}
 $Proof.resourceWindow|Add-Member -NotePropertyName nativeIncapacityClearDisposition -NotePropertyValue $disposition -Force
 $Proof.resourceWindow|Add-Member -NotePropertyName noNativeCostOrPreparationCallbacks -NotePropertyValue $true -Force
 $Proof.resourceWindow.noCostOrPreparationCallbacks=$true;$Proof.resourceWindow.endpointsConserved=$true;$Proof.resourceWindow.pass=$true
 $Proof.resourceWindow.reactionResources|Add-Member -NotePropertyName incapacityClearActor -NotePropertyValue $subjectId -Force
 $Proof.resourceWindow.reactionResources.pass=$true;$Proof.resourceWindow.reactionResources.errors=@()
 $Proof.pass=$true;$Proof.errors=@()
}
function Set-SyntheticIncapacityClear($Proof,$LifeEvents,[string]$Kind) {
 $tick=[long]@($LifeEvents.events)[0].gameTicks;$frame=[int]@($LifeEvents.events)[0].frame
 $Proof.preClick.gameTicks=$tick;$Proof.preClick.allocationSequence=0
 for($i=0;$i-lt$Proof.samples.Count;$i++){
  $sample=$Proof.samples[$i];$sample.gameTicks=$tick;$sample.frame=$frame;$sample.allocationSequence=$(if($i-eq$Proof.samples.Count-1){4}else{0})
  foreach($actor in @('rider','mount')){
   $baseline=$Proof.preClick.state.$actor
   foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','prepared')){
    $sample.state.$actor.$field=$baseline.$field
   }
  }
 }
 $subject=$Proof.preClick.state.$Kind
 $s0=Copy-Eligibility $subject;$s0|Add-Member -NotePropertyName inCombat -NotePropertyValue $false -Force;$s0|Add-Member -NotePropertyName waitingInitiative -NotePropertyValue $false -Force
 $s1=Copy-Eligibility $s0
 $s2=Copy-Eligibility $s0;$s2.initiativeCooldown=0.0
 $s3=Copy-Eligibility $s2;$s3.initiativeOrder=0;$s3.prepared=$false
 $names=@('combat-clear-before','clear-before','clear-after','combat-clear-after');$states=@($s0,$s1,$s2,$s3);$events=@()
 for($i=0;$i-lt4;$i++){$events+=@([pscustomobject]@{sequence=$i+1;boundary=$names[$i];frame=$frame;gameTicks=$tick;state=$states[$i]})}
 $Proof.resourceWindow.events=$events
 $terminal=$Proof.samples[-1]
 $terminal.state.$Kind.initiativeCooldown=0.0;$terminal.state.$Kind.initiativeOrder=0;$terminal.state.$Kind.prepared=$false;$terminal.state.$Kind|Add-Member -NotePropertyName waitingInitiative -NotePropertyValue $false -Force
 Set-IncapacityProducerClaims $Proof $LifeEvents $Kind
}
function New-IncapacityActor([string]$Id,[int]$Hp,[int]$Constitution,[int]$Damage,[bool]$Conscious){
 [pscustomobject]@{id=$Id;inState=$true;inGame=$true;directlyControllable=$true;lifeState=$(if($Conscious){'Conscious'}else{'Unconscious'});conscious=$Conscious;dead=$false;finallyDead=$false;damage=$Damage;hitPoints=$Hp;temporaryHitPoints=0;constitution=$Constitution;allowDyingCondition=$true;immortal=$false;essential=$false;mainCharacter=$false}
}
function New-IncapacityCase([string]$Kind){
 $contract='unacted-native-'+$Kind+'-incapacitated-no-cost-or-transition';$base=New-EligibilityProof $contract;$proof=$base.proof
 $riderBefore=New-IncapacityActor $proof.identity.casterId 100 14 0 $true;$mountBefore=New-IncapacityActor $proof.identity.targetId 12 15 0 $true
 $before=[pscustomobject]@{rider=$riderBefore;mount=$mountBefore;relationship='Unmounted';generation=$proof.identity.generationAtInit}
 $afterDamage=Copy-Eligibility $before;$damageSubject=$afterDamage.$Kind;$damageSubject.damage=$(if($Kind-ceq'rider'){102}else{14});$after=Copy-Eligibility $afterDamage;$subject=$after.$Kind;$subject.lifeState='Unconscious';$subject.conscious=$false
 $beforeSubject=$before.$Kind;$otherKind=if($Kind-ceq'rider'){'mount'}else{'rider'};$requested=$(if($Kind-ceq'rider'){510}else{70});$nativeDamage=$(if($Kind-ceq'rider'){102}else{14});$ruleObject=$(if($Kind-ceq'rider'){701}else{702})
 $source=@([pscustomobject]@{type='Kingmaker.Controllers.Units.UnitLifeController';method='SetLifeState';token='06009164';assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},[pscustomobject]@{type='Kingmaker.Controllers.Units.UnitLifeController';method='TickOnUnit';token='06009162';assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})
 $lifeEvents=[pscustomobject]@{events=@([pscustomobject]@{kind='native-life-state';actor=$beforeSubject.id;lifeState='Unconscious';detail='Conscious';frame=$base.terminal.frame;gameTicks=$base.terminal.gameTicks;currentActor=$null;commandRunning=$false;standard=$proof.preClick.state.$Kind.standard;move=$proof.preClick.state.$Kind.move;damage=$nativeDamage;nativeSource=$source})}
 Set-SyntheticIncapacityClear $proof $lifeEvents $Kind
 [pscustomobject]@{contract='native-life-state-invalidates-exact-mount-approach';subjectKind=$Kind;subjectId=$beforeSubject.id;otherId=$before.$otherKind.id;subjectRemainsIncapacitated=$true;externalRestorationRequired=$true;otherActorUnchanged=$true;noResidue=$true;stimulusCount=1;damageDispatchCount=1;diagnosticInterruptCount=0;
  start=$base.start;trigger=$base.trigger;commandProof=$proof;terminal=(New-Command $proof $true ($Kind-ceq'mount'));stateBefore=$before;stateAfterDamage=(Copy-Eligibility $afterDamage);stateAfterTerminal=(Copy-Eligibility $after);
  stimulus=[pscustomobject]@{contract='one-native-ruledeal-damage-to-incapacitation-window';subjectKind=$Kind;subjectId=$beforeSubject.id;sourceId='enemy-source';count=1;damageDispatchCount=1;difficulty=0.2;hitPoints=$beforeSubject.hitPoints;constitution=$beforeSubject.constitution;temporaryHitPoints=0;damageBefore=0;desiredDamage=$beforeSubject.hitPoints+1;deathThreshold=$beforeSubject.hitPoints+$beforeSubject.constitution;requestedDamage=$requested;projectedDamage=$nativeDamage;nativeDamage=$nativeDamage;nativeDamageBeforeDifficulty=$requested;ruleObject=$ruleObject;before=(Copy-Eligibility $before);after=(Copy-Eligibility $afterDamage);commandBefore=(New-Command $proof $false $false);frameBefore=$base.terminal.frame;frameAfter=$base.terminal.frame;gameTicksBefore=$base.terminal.gameTicks;gameTicksAfter=$base.terminal.gameTicks};
  nativeLifeEvents=$lifeEvents}
}
$riderIncapacityCase=New-IncapacityCase 'rider';$mountIncapacityCase=New-IncapacityCase 'mount'
$nativeRiderIncapacityCase=Copy-Eligibility $nativeIncapacityArtifact.observations.chunk6aPendingIncapacity
Set-IncapacityProducerClaims $nativeRiderIncapacityCase.commandProof $nativeRiderIncapacityCase.nativeLifeEvents 'rider'
$nativeMountIncapacityCase=Copy-Eligibility $nativeMountIncapacityArtifact.observations.chunk6aPendingIncapacity
Set-IncapacityProducerClaims $nativeMountIncapacityCase.commandProof $nativeMountIncapacityCase.nativeLifeEvents 'mount'
$script:checks=0
function Reject-Size([scriptblock]$Mutate,[string]$Reason){$copy=Copy-Eligibility $sizeCase;&$Mutate $copy;$rejected=$false;try{Assert-KmcSizeFormChange $copy}catch{if($_.Exception.Message.IndexOf($Reason,[StringComparison]::OrdinalIgnoreCase)-lt0){throw};$rejected=$true};if(-not$rejected){throw 'Invalid size/form proof admitted'};$script:checks++}
function Reject-Control([scriptblock]$Mutate,[string]$Reason){$copy=Copy-Eligibility $controlCase;&$Mutate $copy;$rejected=$false;try{Assert-KmcLostDirectControl $copy}catch{if($_.Exception.Message.IndexOf($Reason,[StringComparison]::OrdinalIgnoreCase)-lt0){throw};$rejected=$true};if(-not$rejected){throw 'Invalid direct-control proof admitted'};$script:checks++}
function Reject-Incapacity([string]$Kind,[scriptblock]$Mutate,[string]$Reason){$copy=Copy-Eligibility $(if($Kind-ceq'rider'){$riderIncapacityCase}else{$mountIncapacityCase});&$Mutate $copy;$rejected=$false;try{Assert-KmcPendingIncapacity ('chunk6a-'+$Kind+'-incapacitated') $copy $Kind}catch{if($_.Exception.Message.IndexOf($Reason,[StringComparison]::OrdinalIgnoreCase)-lt0){throw};$rejected=$true};if(-not$rejected){throw 'Invalid incapacity proof admitted'};$script:checks++}
function Reject-NativeIncapacity([scriptblock]$Mutate,[string]$Reason){$copy=Copy-Eligibility $nativeRiderIncapacityCase;&$Mutate $copy;$rejected=$false;try{Assert-KmcPendingIncapacity 'chunk6a-rider-incapacitated' $copy 'rider'}catch{if($_.Exception.Message.IndexOf($Reason,[StringComparison]::OrdinalIgnoreCase)-lt0){throw};$rejected=$true};if(-not$rejected){throw 'Invalid native incapacity resource proof admitted'};$script:checks++}
function Reject-NativeMountIncapacity([scriptblock]$Mutate,[string]$Reason){$copy=Copy-Eligibility $nativeMountIncapacityCase;&$Mutate $copy;$rejected=$false;try{Assert-KmcPendingIncapacity 'chunk6a-mount-incapacitated' $copy 'mount'}catch{if($_.Exception.Message.IndexOf($Reason,[StringComparison]::OrdinalIgnoreCase)-lt0){throw};$rejected=$true};if(-not$rejected){throw 'Invalid native mount-incapacity resource proof admitted'};$script:checks++}
Assert-KmcSizeFormChange $sizeCase;$script:checks++
Assert-KmcLostDirectControl $controlCase;$script:checks++
Assert-KmcPendingIncapacity 'chunk6a-rider-incapacitated' $riderIncapacityCase 'rider';$script:checks++
Assert-KmcPendingIncapacity 'chunk6a-mount-incapacitated' $mountIncapacityCase 'mount';$script:checks++
Assert-KmcPendingIncapacity 'chunk6a-rider-incapacitated' $nativeRiderIncapacityCase 'rider';$script:checks++
Assert-KmcPendingIncapacity 'chunk6a-mount-incapacitated' $nativeMountIncapacityCase 'mount';$script:checks++

function Assert-Envelope([string]$Scenario,[string]$Row,[string]$Observation,$Case){
 $envelope=Copy-Eligibility $artifact
 $envelope.rows=@($envelope.rows|Where-Object {$_.name-cne'CM02-obstruction'})+@([pscustomobject]@{name=$Row;status='PASS';assertionPassCount=1;assertionFailCount=0;errors=@()})
 [void]$envelope.observations.PSObject.Properties.Remove('chunk6aDoorFixture');[void]$envelope.observations.PSObject.Properties.Remove('chunk6aObstruction')
 $envelope.observations|Add-Member -NotePropertyName $Observation -NotePropertyValue (Copy-Eligibility $Case) -Force
 Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario=$Scenario}) $envelope 'PASS';$script:checks++
}
Assert-Envelope 'chunk6a-size-form-change' 'CM02-size-form-change' 'chunk6aSizeFormChange' $sizeCase
Assert-Envelope 'chunk6a-lost-direct-control' 'CM02-lost-direct-control' 'chunk6aLostDirectControl' $controlCase
Assert-Envelope 'chunk6a-rider-incapacitated' 'CM02-rider-incapacitated' 'chunk6aPendingIncapacity' $riderIncapacityCase
Assert-Envelope 'chunk6a-mount-incapacitated' 'CM02-mount-incapacitated' 'chunk6aPendingIncapacity' $mountIncapacityCase
Assert-Envelope 'chunk6a-mount-incapacitated' 'CM02-mount-incapacitated' 'chunk6aPendingIncapacity' $nativeMountIncapacityCase
Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario='chunk6a-size-form-change'}) $nativeSizeArtifact 'PASS';$script:checks++
Assert-KmcChunk6aCombatMountEvidence ([pscustomobject]@{scenario='chunk6a-lost-direct-control'}) $nativeControlArtifact 'PASS';$script:checks++

Reject-Size {param($c)$c.commandProof.resourceWindow.reactionResources.pass=$false} 'Reaction resource contract'
Reject-Size {param($c)$c.terminal.started=$false} 'terminal snapshot'
Reject-Control {param($c)$c.commandProof.samples[-1].state.rider.reactionCooldown=[double]$c.commandProof.samples[-1].state.rider.reactionCooldown+0.5} 'reaction allowance or cooldown'
Reject-Control {param($c)$c.terminal.started=$true} 'terminal snapshot'
Reject-Control {param($c)$c.fearController.events[0].after.commandStarted=$true} 'loss boundary differs'
Reject-Incapacity 'rider' {param($c)$c.terminal.started=$true} 'terminal snapshot'
Reject-Incapacity 'mount' {param($c)$c.nativeLifeEvents.events[0].nativeSource[1].token='06000000'} 'life event is missing or differs'
Reject-NativeMountIncapacity {param($c)$c.terminal.started=$false} 'terminal snapshot'
Reject-NativeMountIncapacity {param($c)$clear=@($c.commandProof.resourceWindow.events|Where-Object {[string]$_.boundary-cmatch'^(clear-|combat-clear)'});$terminal=$c.commandProof.samples[-1];$terminal.allocationSequence=[int]$clear[3].sequence-1;$c.commandProof.resourceWindow.nativeIncapacityClearDisposition.actionBridgeAfterSequence=$terminal.allocationSequence} 'action bridge sequence'
Reject-Incapacity 'rider' {param($c)$c.stateAfterTerminal.rider.dead=$true} 'nonlethal Unconscious'
Reject-Incapacity 'mount' {param($c)$c.stimulus.damageDispatchCount=2} 'dispatch count differs'
Reject-Incapacity 'rider' {param($c)$c.stateAfterTerminal.mount.damage++} 'independent actor'
Reject-Incapacity 'mount' {param($c)$c.commandProof.resourceWindow.reactionResources.pass=$false} 'Reaction resource contract'
Reject-NativeIncapacity {param($c)$clear=@($c.commandProof.resourceWindow.events|Where-Object {[string]$_.boundary-cmatch'^(clear-|combat-clear)'});$clear[0].state.actor=$c.commandProof.mountId} 'clear actor, order, or sequence'
Reject-NativeIncapacity {param($c)$clear=@($c.commandProof.resourceWindow.events|Where-Object {[string]$_.boundary-cmatch'^(clear-|combat-clear)'});$clear[2].state.reactions++} 'changed reaction allowance or cooldown'
Reject-NativeIncapacity {param($c)$clear=@($c.commandProof.resourceWindow.events|Where-Object {[string]$_.boundary-cmatch'^(clear-|combat-clear)'});$clear[2].state.reactionCooldown=[double]$clear[2].state.reactionCooldown+0.5} 'changed reaction allowance or cooldown'
Reject-NativeIncapacity {param($c)$clear=@($c.commandProof.resourceWindow.events|Where-Object {[string]$_.boundary-cmatch'^(clear-|combat-clear)'});$clear[2].state.standard=[double]$clear[2].state.standard+1} 'changed Standard, Move, or Swift'
Reject-NativeIncapacity {param($c)$c.nativeLifeEvents.events[0].nativeSource[0].token='06000000'} 'life event is missing or differs'
Reject-NativeIncapacity {param($c)$clear=@($c.commandProof.resourceWindow.events|Where-Object {[string]$_.boundary-cmatch'^(clear-|combat-clear)'});$clear[1].boundary='clear-after'} 'clear actor, order, or sequence'
Reject-NativeIncapacity {param($c)$clear=@($c.commandProof.resourceWindow.events|Where-Object {[string]$_.boundary-cmatch'^(clear-|combat-clear)'});$clear[2].state.initiativeCooldown=$clear[1].state.initiativeCooldown} 'initiative-cooldown clear'
Reject-NativeIncapacity {param($c)$clear=@($c.commandProof.resourceWindow.events|Where-Object {[string]$_.boundary-cmatch'^(clear-|combat-clear)'});$clear[3].state.initiativeOrder=$clear[2].state.initiativeOrder} 'initiative-order clear'
Reject-NativeIncapacity {param($c)$clear=@($c.commandProof.resourceWindow.events|Where-Object {[string]$_.boundary-cmatch'^(clear-|combat-clear)'});foreach($event in $clear){$event.state.standard=1.0};$c.nativeLifeEvents.events[0].standard=1.0} 'not reconciled with surrounding command observations'
Reject-NativeIncapacity {param($c)$clear=@($c.commandProof.resourceWindow.events|Where-Object {[string]$_.boundary-cmatch'^(clear-|combat-clear)'});foreach($event in $clear){$event.state.swift=1.0}} 'not reconciled with surrounding command observations'
Reject-NativeIncapacity {param($c)$clear=@($c.commandProof.resourceWindow.events|Where-Object {[string]$_.boundary-cmatch'^(clear-|combat-clear)'});$sample=$c.commandProof.samples[3];$sample.allocationSequence=$clear[1].sequence;$sample.gameTicks=$clear[1].gameTicks;$sample.state.rider.initiativeOrder++} 'initiative ordering'
Reject-Size {param($c)$c.stimulus.after.riderSize=4} 'equal-or-larger'
Reject-Size {param($c)$c.stimulus.after.changeUnitSizeComponents=2} 'identity differs'
Reject-Size {param($c)$c.removal.buffObject++} 'exact terminal command'
Reject-Size {param($c)$c.commandProof.samples[3].identity.commandObject++} 'Mixed eligibility command identity'
Reject-Control {param($c)$c.stimulus.lease.onTurnOnToken='06000000'} 'Fact identity differs'
Reject-Control {param($c)$c.stateAtControlLoss.directlyControllable=$true} 'did not remove direct control'
Reject-Control {param($c)$c.fearController.observerHooks[0].token='06000000'} 'hook identity differs'
Reject-Control {param($c)$c.fearController.events[0].after.commandFinished=$false} 'loss boundary differs'
Reject-Control {param($c)$c.restoration.state.panicked=$true} 'did not restore'
Reject-Control {param($c)$c.restoration.allocationEvents=@([pscustomobject]@{boundary='cost-before'})} 'native cost'

foreach($case in @(@('CM02-size-form-change','chunk6a-size-form-change'),@('CM02-lost-direct-control','chunk6a-lost-direct-control'),@('CM02-rider-incapacitated','chunk6a-rider-incapacitated'),@('CM02-mount-incapacitated','chunk6a-mount-incapacitated'))){
 if(@(Get-KmcSaveBackedRuntimeScenarios|Where-Object {$_-ceq$case[1]}).Count-ne1){throw 'Eligibility scenario registration missing or duplicate'};$script:checks++
 if(@(Get-KmcPhase3dHorseRuntimeRows|Where-Object {$_-ceq$case[0]}).Count-ne1){throw 'Eligibility row registration missing or duplicate'};$script:checks++
 $binding=[pscustomobject]@{scenario=$case[1];rows=@($case[0]);evidenceLeaf='phase3d-horse-scenario-evidence.json'}
 if(-not(Assert-KmcIsolatedScenarioRows $case[0] $binding)){throw 'Eligibility isolated mapping missing'};$script:checks++
 Assert-KmcChunk6aPrimaryClaim $case[0] $binding;$script:checks++
}
$dispatcherSource=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk6aCombatMountScenario.cs')
$dispatchStart=$dispatcherSource.IndexOf('internal static bool IsChunk6aCombatMountScenario')
$dispatchEnd=$dispatcherSource.IndexOf('private bool IsChunk6aCombatMount',$dispatchStart)
if($dispatchStart-lt0-or$dispatchEnd-le$dispatchStart){throw 'Chunk 6A in-game dispatcher contract is missing'};$script:checks++
$dispatchBlock=$dispatcherSource.Substring($dispatchStart,$dispatchEnd-$dispatchStart)
foreach($name in @('Chunk6aSizeFormChangeScenario','Chunk6aLostDirectControlScenario','Chunk6aRiderIncapacitatedScenario','Chunk6aMountIncapacitatedScenario')){
 if([regex]::Matches($dispatchBlock,[regex]::Escape($name)).Count-ne1){throw "Eligibility in-game dispatcher omitted or duplicated $name"};$script:checks++
}
$source=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk6aEligibilityChangeScenario.cs')
$sizeSource=$source.Substring($source.IndexOf('private void TickChunk6aSizeFormChange()'),$source.IndexOf('private void TickChunk6aLostDirectControl()')-$source.IndexOf('private void TickChunk6aSizeFormChange()'))
$controlSource=$source.Substring($source.IndexOf('private void TickChunk6aLostDirectControl()'),$source.IndexOf('private JObject CaptureChunk6aSizeState')-$source.IndexOf('private void TickChunk6aLostDirectControl()'))
foreach($pair in @(@($sizeSource,'Chunk6aSizeFormRow','chunk6aSizeOriginal = CaptureChunk6aSizeState','chunk6a-size-form-change-click'),@($controlSource,'Chunk6aLostDirectControlRow','chunk6aControlOriginal = CaptureChunk6aDirectControlState','chunk6a-lost-direct-control-click'))){
 $selection=$pair[0].IndexOf('EnsureChunk6aRiderSelection('+$pair[1]+')');$baseline=$pair[0].IndexOf($pair[2]);$probe=$pair[0].IndexOf('new NativeRelationshipCommandProbe');$click=$pair[0].IndexOf($pair[3])
 if($selection-lt0-or$selection-ge$baseline-or$baseline-ge$probe-or$probe-ge$click){throw 'Eligibility selection/baseline/click order differs'};$script:checks++
}
if([regex]::Matches($sizeSource,'rider[.]Buffs[.]AddBuff[(]').Count-ne1-or$sizeSource.IndexOf('FinishUnacted("unacted-native-size-form-change-no-cost-or-transition")')-gt$sizeSource.IndexOf('chunk6aSizeBuff.Remove()')){throw 'Size native stimulus/removal order differs'};$script:checks++
if($controlSource.IndexOf('new NativeFearControlProbe')-gt$controlSource.IndexOf('new NativePendingMountFearLease')-or$controlSource.IndexOf('FinishUnacted("unacted-native-lost-direct-control-no-cost-or-transition")')-gt$controlSource.IndexOf('chunk6aFearLease.Dispose()')){throw 'Fear observer/stimulus/removal order differs'};$script:checks++
$fearSource=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/NativePendingMountFear.cs')
foreach($forbidden in @('IsPanicked =','Cooldowns.Clear','TurnController.Prepare','EndTurn','StandardAction =','MoveAction =','SwiftAction =')){if($fearSource.Contains($forbidden)){throw "Fear fixture writes forbidden production state: $forbidden"}};$script:checks++
foreach($pin in @('0x06009138','OnTurnOn','OnTurnOff','UnitCondition.Frightened')){if(-not$fearSource.Contains($pin)){throw "Fear fixture omitted exact native pin: $pin"}};$script:checks++
$incapacitySource=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk6aPendingIncapacityScenario.cs')
$incapacityTick=$incapacitySource.Substring($incapacitySource.IndexOf('private void TickChunk6aPendingIncapacity()'),$incapacitySource.IndexOf('private JObject CaptureChunk6aPendingIncapacityState')-$incapacitySource.IndexOf('private void TickChunk6aPendingIncapacity()'))
$selection=$incapacityTick.IndexOf('EnsureChunk6aRiderSelection(Chunk6aIncapacityRow)');$baseline=$incapacityTick.IndexOf('chunk6aIncapacityOriginal = CaptureChunk6aPendingIncapacityState()');$probe=$incapacityTick.IndexOf('new NativeRelationshipCommandProbe');$click=$incapacityTick.IndexOf('TryNativeAbilityTargetClick')
if($selection-lt0-or$selection-ge$baseline-or$baseline-ge$probe-or$probe-ge$click){throw 'Pending incapacity selection/baseline/click order differs'};$script:checks++
$observer=$incapacityTick.IndexOf('new PairedConditionObserver(rider, horse, true)');$damage=$incapacityTick.IndexOf('Rulebook.Trigger(new RuleDealDamage');$finish=$incapacityTick.IndexOf('FinishUnacted(Chunk6aIncapacityProofContract, lifeEvents)')
if($observer-lt0-or$observer-ge$damage-or$damage-ge$finish-or[regex]::Matches($incapacityTick,'Rulebook[.]Trigger[(]new RuleDealDamage').Count-ne1){throw 'Pending incapacity observer/damage/terminal order differs'};$script:checks++
foreach($pin in @('Chunk6aRiderIncapacitatedScenario','Chunk6aMountIncapacitatedScenario','06009164','06009162','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7','unacted-native-rider-incapacitated-no-cost-or-transition','unacted-native-mount-incapacitated-no-cost-or-transition','FinishUnacted(Chunk6aIncapacityProofContract, lifeEvents)','externalRestorationRequired','expectedTerminalStarted = Chunk6aMountIncapacitatedOnly','(bool)terminal["started"] == expectedTerminalStarted')){if(-not$incapacitySource.Contains($pin)){throw "Pending incapacity source omitted exact pin: $pin"}};$script:checks++
foreach($forbidden in @('.Damage =','Cooldowns.Clear','TurnController.Prepare','ForceUnitConscious','StandardAction =','MoveAction =','SwiftAction =','.Interrupt(')){if($incapacityTick.Contains($forbidden)){throw "Pending incapacity stimulus writes forbidden state: $forbidden"}};$script:checks++
$unactedSource=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/NativeRelationshipUnactedEvidence.cs')
$reactionSource=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/NativeRelationshipReactionEvidence.cs')
foreach($pin in @('native-incapacity-combat-removal-clears-initiative-only','ExactNativeIncapacityLifeEvent','combat-clear-before','clear-before','clear-after','combat-clear-after','IncapacityActionBridge','(int)terminal["allocationSequence"] >= (int)clearEvents[3]["sequence"]','actionResourcesBridged','noNativeCostOrPreparationCallbacks')){if(-not$unactedSource.Contains($pin)){throw "Unacted incapacity resource proof omitted exact pin: $pin"}};$script:checks++
foreach($pin in @('incapacityClearActor','ClearInitiativeOnly','LeaveCombat','combat-clear-before','combat-clear-after')){if(-not$reactionSource.Contains($pin)){throw "Reaction incapacity resource proof omitted exact pin: $pin"}};$script:checks++
$project=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/KingmakerMountedCombat.csproj')
if([regex]::Matches($project,[regex]::Escape('Diagnostics\Chunk6aPendingIncapacityScenario.cs')).Count-ne1){throw 'Pending incapacity source compile registration missing or duplicate'};$script:checks++
if((Get-FileHash $original).Hash-cne$originalHash){throw 'Original obstruction artifact changed'}
if((Get-FileHash -Algorithm SHA256 -LiteralPath $nativeSizePath).Hash.ToLowerInvariant()-cne$nativeSizeHash){throw 'Exact preview.136 size/form evidence changed'}
if((Get-FileHash -Algorithm SHA256 -LiteralPath $nativeControlPath).Hash.ToLowerInvariant()-cne$nativeControlHash){throw 'Exact preview.137 lost-control evidence changed'}
if((Get-FileHash -Algorithm SHA256 -LiteralPath $nativeIncapacityPath).Hash.ToLowerInvariant()-cne$nativeIncapacityHash){throw 'Exact preview.138 rider-incapacity failure evidence changed'}
if((Get-FileHash -Algorithm SHA256 -LiteralPath $nativeMountIncapacityPath).Hash.ToLowerInvariant()-cne$nativeMountIncapacityHash){throw 'Exact preview.139 mount-incapacity failure evidence changed'}
Write-Host "ELIGIBILITY PASS=$script:checks FAIL=0; offline contracts and immutable native artifact replay only, no qualification."
