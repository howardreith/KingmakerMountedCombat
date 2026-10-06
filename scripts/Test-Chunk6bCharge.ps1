[CmdletBinding()]
param()
# Synthetic acceptance/refusal regression for the Chunk 6B charge-delivery reader
# (scripts/runtime/Chunk6bChargeEvidence.ps1). Never launches the game; no native qualification.
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Test-Chunk6bFixtureData.ps1')
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6bChargeEvidence.ps1')
# Reuse the historical native relationship proof fixture without executing its tests.
$tokens=$null;$parseErrors=$null
$causalAst=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-Chunk6aCausalProtocol.ps1'),[ref]$tokens,[ref]$parseErrors)
if($parseErrors.Count-ne0){throw 'Causal fixture parse failed'}
foreach($name in @('Copy-Value','Put-Value','New-CommandProof')){
 $definition=@($causalAst.FindAll({param($node)$node-is[Management.Automation.Language.FunctionDefinitionAst]-and$node.Name-ceq$name},$true))
 if($definition.Count-ne1){throw 'Causal fixture function differs'}
 . ([scriptblock]::Create($definition[0].Extent.Text))
}
$script:checks=0
$script:fixtureMode='RT'
$kmc=ChargeKmcAbilityGuid
$stock=ChargeStockAbilityGuid
function Copy-Case($x){ $x|ConvertTo-Json -Depth 40|ConvertFrom-Json }
# Injected sections must round-trip exactly as the artifact did, so the reader sees the same object shape.
function Json($x){ $x|ConvertTo-Json -Depth 30|ConvertFrom-Json }
function New-Identity([bool]$Present,[bool]$Setting){
 [ordered]@{setting=$Setting;kmcChargePresent=$Present;kmcChargeBlueprint=$(if($Present){$kmc}else{$null})
  kmcChargeActionType=$(if($Present){'Standard'}else{$null});kmcChargeFullRound=$(if($Present){$true}else{$null})
  kmcChargeRequiresFullRound=$(if($Present){$true}else{$null});kmcChargeMinRange=$(if($Present){4.65}else{$null})
  kmcChargeComponent=$(if($Present){'KingmakerMountedCombat.Integration.MountedChargeAbilityLogic'}else{$null})
  kmcChargeIsStockLogic=$false;stockChargePresent=$true;stockChargeIsStockLogic=$true;abilityCount=12}
}
function New-Actor([string]$Id,[double]$Standard,[double]$Move){ [ordered]@{id=$Id;standard=$Standard;move=$Move;swift=0;position=@(0,0,0);commands=1;raw=@();queue=@();previous=$null;group=0} }
function New-State([string]$Kind,[double]$Distance,[double]$RiderStandard,[bool]$Charging){
 [ordered]@{kind=$Kind;frame=100;nativeSeconds=10.0;rider=(New-Actor 'rider' $RiderStandard 0);mount=(New-Actor 'mount' 0 0)
  relationship='Mounted';distanceToTarget=$Distance;riderDistanceToTarget=$Distance;mountCharging=$Charging
  mountSpeedOverride=$(if($Charging){10.16}else{$null});mountMoving=$Charging;mountCombatSpeedMps=5.08
  riderStateCharging=$Charging;mountStateCharging=$false;chargeBuffPresent=$Charging;chargeBuffRounds=6.0
  pairCommandActive=$Charging;pairMovement='mountMove=0->0'
  turn=$(if($script:fixtureMode-ceq'TB'){[ordered]@{unit='rider';isRider=$true;status='Acting';acting=$true;timeMoved=0.0;timeMovedInForceMode=0.0}}else{$null})}
}
function New-Rules([int]$NonOpportunity,[int]$Opportunity,[bool]$Charge){
 $events=@()
 for($i=0;$i-lt$NonOpportunity;$i++){ $events+=,([ordered]@{sequence=$i+1;frame=120+$i;actorId='rider';targetId='target';attackOfOpportunity=$false;firstAttack=$true;fullAttack=$false;charge=$Charge;attackNumber=1;attacksCount=1;weaponGuid='w'}) }
 for($i=0;$i-lt$Opportunity;$i++){ $events+=,([ordered]@{sequence=$NonOpportunity+$i+1;frame=140+$i;actorId='rider';targetId='target';attackOfOpportunity=$true;firstAttack=$true;fullAttack=$false;charge=$false;attackNumber=1;attacksCount=1;weaponGuid='w'}) }
 [ordered]@{riderAttackRules=($NonOpportunity+$Opportunity);riderResolved=$NonOpportunity;mountResolved=0;mountAttackRules=0
  riderNonOpportunityAttackRules=$NonOpportunity;mountNonOpportunityAttackRules=0;pairNonOpportunityAttackRules=$NonOpportunity
  riderOpportunityAttackRules=$Opportunity;mountOpportunityAttackRules=0;pairOpportunityAttackRules=$Opportunity
  pairAttackRolls=$NonOpportunity;pairOpportunityAttackRolls=$Opportunity;pairDamageRules=$NonOpportunity;pairForcedD20=0;pairDamage=4
  firstPairActorId='rider';lastPairActorId='rider';lastRiderAttackType='Standard';lastRiderAttackDoNotProvoke=$false
  attackRuleEvents=$events;attackRollEvents=@()}
}
# The charge transaction as the command records it. A charge that struck shows the whole lawful order;
# one that was terminated stops where it was stopped and never reaches AttackStarted.
function New-Transaction([bool]$Struck){
 $steps=if($Struck){'CarrierAdmitted|CarrierOwnershipProven|InitialRevalidation|LeaseApplied|RepathRevalidation|TransitionRevalidation|CarrierReleasedForAttack|CarrierReleaseProven|Arrived|AttackStartRevalidation|AttackStarted'}else{'CarrierAdmitted|CarrierOwnershipProven|InitialRevalidation|LeaseApplied|RepathRevalidation'}
 [ordered]@{chargeMode=$true;revalidationCount=$(if($Struck){4}else{2})
  revalidationPhases=$(if($Struck){'BeforeRepath=valid|BeforeRepath=valid|BeforeAttackTransition=valid|BeforeAttackStart=valid'}else{'BeforeRepath=valid|BeforeRepath=valid'})
  revalidationFailed=$false;revalidationFailurePhase=$null;revalidationFailureReason=$null;revalidationFailureCode=$null
  sequence=('lawful=True;carrierOwned=False;steps='+$steps+';violations=')
  sequenceLawful=$true;carrierReleaseProven=$Struck;leaseRestored=$true
  leaseDescription='applied=True;rolledBack=False;restored=True';cleanupComplete=$true;cleanupDebt='';cleanupDebtAtEnd=''
  leaseApplicationFailed=$false;leaseApplicationFailure=$null;leaseApplicationFailedStep=$null;leaseRolledBackOnFailure=$false
  repathObservations='repath=1;target-moved';finished=$true;result=$(if($Struck){'Success'}else{'Interrupted'})}
}
function New-Lease{
 [ordered]@{applied=$true;buffApplied=$true;buffOutstanding=$false;chargingBefore=$false;speedOverrideBefore=$null;speedOverrideApplied=10.16
  buffAcquisitionStarted=$true;buffAcquisitionObserved=$true;buffCallbackDebt=$null;buffRuleDispatchSettled=$true
  buffNative=@{identity=100;collection=200;inCollection=$false;active=$false;disposed=$true;turnedOn=$false;activating=$false;deactivating=$false;recalculating=$false;listening=0;statModifiers=0;attachedModifiers=0;componentCount=0;componentData=$false;storedFacts=0;storedModifiers=0;parentContext=$false;currentContext=$false}
  buffChildren=@{schema=1;rider='rider';rootIdentity=100;rootCollection=200;surface='native-base';retiring=$true;scopeSettled=$true;drained=$true;fault=$null;failures=@();facts=@()}
  buffComponentTypes=@('Kingmaker.UnitLogic.FactLogic.AddStatBonus','Kingmaker.UnitLogic.FactLogic.AddCondition','Kingmaker.Designers.Mechanics.Facts.AttackOfOpportunityAttackBonus')
  buffCondition=@{componentObserved=$true;condition=40;additions=1;removals=1;contributions=0;nativeExceptions=0;fault=$null;drained=$true;operations=@(
   @{addition=$true;before=2;after=3;mutationObserved=$true;completed=$true;returnedNormally=$true},
   @{addition=$false;before=6;after=5;mutationObserved=$true;completed=$true;returnedNormally=$true})}
  mountCombatSpeedMps=5.08;chargingAppliedExactly=$true;chargingObservedThroughout=$true;forceModeAfterApply=$true
  forcedPathCount=2;riderAgentTouched=$false;restored=$true;chargingRestoredExactly=$true;speedOverrideRestoredExactly=$true
  riderChargingRestoredExactly=$true;forceModeAtRestore=$true;riderChargingBefore=$false;observations=@('applied:x')}
}
function New-Intervention([string]$Kind){
 [ordered]@{kind=$Kind;frame=130;nativeSeconds=0.4;mountDistanceAtIntervention=4.0;pairCommandActiveBefore=$true
  riderInCombatBefore=$true;riderStandardBefore=6.0;before=(New-State 'intervention-before' 5.0 6.0 $true)
  after=(New-State 'intervention-after' 5.0 6.0 $true);riderInCombatAfter=$($Kind-cne'native-combat-end')
  riderStandardAfter=$(if($Kind-ceq'native-combat-end'){0.0}else{5.8})}
}
# The body removed mid-approach through the diagnostic service own bounded destroy, with the service
# itself retained for the fixture own cleanup.
function New-TargetLoss{
 [ordered]@{contract='native-target-body-removed-mid-approach-shared-service-retained';lostTargetId='target'
  destroyConfirmed=$true;serviceRetained=$true;serviceDisposedByRow=$false
  serviceState='Removed';targetEntityRemoved=$true;riderInCombat=$true;mountInCombat=$true;frame=131
  before=(New-State 'target-removal-before' 5.0 6.0 $true)}
}
# One real native RuleDealDamage into the exact nonlethal unconscious window, and the exact damage
# returned afterwards.
function New-Incapacity([string]$Kind){
 $subjectId=if($Kind-ceq'mount'){'mount'}else{'rider'}
 $otherKind=if($Kind-ceq'mount'){'rider'}else{'mount'}
 # Two captures, as the fixture records them. In the frame of the damage rule the engine has not run
 # its life controller yet, so the subject still reads Conscious with its damage already past its hit
 # points; by the time the fixture restores the damage the life state is written and the incapacity
 # has dissolved the pair. The measured preview.175 rows differ in exactly those fields.
 $state=[ordered]@{}
 $state[$Kind]=[ordered]@{id=$subjectId;conscious=$true;dead=$false;finallyDead=$false;damage=31;hitPoints=30;constitution=14;temporaryHitPoints=0;allowDyingCondition=$true;immortal=$false;essential=$false;mainCharacter=$false;lifeState='Conscious'}
 $state[$otherKind]=[ordered]@{id=$otherKind;conscious=$true;dead=$false;finallyDead=$false;damage=0;hitPoints=30;constitution=14;temporaryHitPoints=0;allowDyingCondition=$true;immortal=$false;essential=$false;mainCharacter=$false;lifeState='Conscious'}
 $state['relationship']='Mounted'
 $settledState=[ordered]@{}
 $settledState[$Kind]=[ordered]@{id=$subjectId;conscious=$false;dead=$false;finallyDead=$false;damage=31;hitPoints=30;constitution=14;temporaryHitPoints=0;allowDyingCondition=$true;immortal=$false;essential=$false;mainCharacter=$false;lifeState='Unconscious'}
 $settledState[$otherKind]=[ordered]@{id=$otherKind;conscious=$true;dead=$false;finallyDead=$false;damage=0;hitPoints=30;constitution=14;temporaryHitPoints=0;allowDyingCondition=$true;immortal=$false;essential=$false;mainCharacter=$false;lifeState='Conscious'}
 $settledState['relationship']='Unmounted'
 $events=@([ordered]@{kind='native-life-state';actor=$subjectId;frame=132;lifeState='Unconscious';detail='Conscious'
   nativeSource=@([ordered]@{type='Kingmaker.Controllers.Units.UnitLifeController';method='SetLifeState';token='06009164';assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})},
  [ordered]@{kind='native-command-act';actor='rider';frame=133;lifeState='Conscious';detail='Kingmaker.UnitLogic.Commands.UnitUseAbility';nativeSource=$null})
 [ordered]@{contract='one-native-ruledeal-damage-to-incapacitation-window-mid-charge';subjectKind=$Kind;subjectId=$subjectId
  window='present';stateBefore=[ordered]@{relationship='Mounted'};frame=130
  measurement=[ordered]@{difficulty=1.0;hitPoints=30;constitution=14;temporaryHitPoints=0;damageBefore=0
   desiredDamage=31;deathThreshold=44;needed=31;requestedDamage=32;projectedDamage=32}
  sourceId='target';damageDispatchCount=1;nativeDamage=32;nativeDamageBeforeDifficulty=32;difficultyUnchanged=$true
  stateAfterDamage=$state;damageAfter=32
  lifeEventsAtSettle=[ordered]@{events=$events}
  restore=[ordered]@{contract='fixture-returns-exactly-the-damage-it-dealt';damageAtRestoreStart=32;damageToRestore=0
   stateBeforeRestore=$settledState;frame=160;restored=$true;damageAfterRestore=0;conscious=$true
   lifeState='Conscious';stateAfterRestore=[ordered]@{relationship='Unmounted'};lifeEvents=[ordered]@{events=$events}}}
}
function New-TerminatedRow([string]$Case,[string]$Kind){
 $incapacity=$Kind-cin@('native-rider-incapacity','native-mount-incapacity')
 [ordered]@{name=$Case;status='PASS';evidence=[ordered]@{
  level='NATIVE DELIVERY';mode=$script:fixtureMode;case=$Case;mounted=(-not$incapacity)
  before=[ordered]@{identity=(New-Identity $true $true);state=(New-State 'before' 9.0 0 $false)
   available=$true;unavailableReason=$null;canTarget=$true;minRangeMeters=4.65;approachDistance=99.0
   requireFullRound=$true;commandType='Standard';geometry=@{straightRoute=$true;landingBlocked=$false};pairCommandState=[ordered]@{frame=100}}
  input=[ordered]@{clicked=$true;hoverPure=$true;frame=110;shell=[ordered]@{present=$true};shellCount=1
   feedback='Mounted charge accepted: the Horse carries the charge.';rejectionCodes=@();chargeAdmitted=0;chargeRefused=0;lastRefusal=$null
   after=(New-State 'input-after' 9.0 0 $false)}
  samples=@()
  after=$(if($incapacity){$s=(New-State 'after' 5.0 4.0 $false);$s['relationship']='Unmounted';$s}else{New-State 'after' 5.0 4.0 $false})
  identityAfter=(New-Identity (-not$incapacity) $true)
  movement=[ordered]@{mountDistance=4.0;riderDistance=4.0;peakSpeedMps=10.3;mountCombatSpeedMps=5.08
   chargingObserved=$true;chargeModeObserved=$true;riderChargeStateObserved=$true}
  economy=[ordered]@{riderStandardMax=6.0;riderMoveMax=0.0;mountStandardMax=0.0;mountMoveMax=0.0
   riderStandardNow=4.0;riderMoveNow=0.0;mountStandardNow=0.0;mountMoveNow=0.0}
  lease=(New-Lease)
  transaction=(New-Transaction $false)
  nonDelivery=$null;strandedShellInterrupted=$false;clickRetries=0;clickRetryObservations=@()
  targetLoss=$(if($Kind-ceq'native-target-removed'){New-TargetLoss}else{$null})
  # An incapacity dissolves the pair, so those rows are written after the relationship is gone.
  incapacity=$(if($Kind-cin@('native-rider-incapacity','native-mount-incapacity')){New-Incapacity $(if($Kind-ceq'native-mount-incapacity'){'mount'}else{'rider'})}else{$null})
  intervention=(New-Intervention $Kind)
  delivery=[ordered]@{chargeAdmitted=1;chargeRefused=0;lastRefusal=$null;feedback='Mounted charge accepted: the Horse carries the charge.';rejectionCodes=@()}
  rules=(New-Rules 0 0 $false)
  attackRules=0;attackRulesOpportunity=0
  pairCommandState=[ordered]@{frame=200}
  terminal=[ordered]@{action='RiderMelee';actorId='rider';resourceOwnerId='rider';targetId='target';result='Interrupted';childAttackStartCount=0;singleAttackMode=$true;nativeFullAttack=$false;nativePlannedAttackCount=1;nativeCompletedAttackCount=0;repathCount=0}
 }}
}
# A charge that failed immediately after AddToQueueFirst: the one diagnostics-only seam fired, every
# native owner was resolved, and the pair never ran.
function New-AdmissionFaultRow{
 $case='C6B-CHARGE-exception-cleanup'
 [ordered]@{name=$case;status='PASS';evidence=[ordered]@{
  level='NATIVE DELIVERY';mode=$script:fixtureMode;case=$case;mounted=$true
  before=[ordered]@{identity=(New-Identity $true $true);state=(New-State 'before' 9.0 0 $false)
   available=$true;unavailableReason=$null;kmcAvailabilityReason='Mounted Charge is available.';canTarget=$true
   minRangeMeters=4.65;approachDistance=99.0
   geometry=[ordered]@{straightRoute=$true;landingBlocked=$false;mountDistanceToTarget=9.0}
   requireFullRound=$true;commandType='Standard';pairCommandState=[ordered]@{frame=100}}
  input=[ordered]@{clicked=$true;hoverPure=$true;frame=110;shell=[ordered]@{present=$true};shellCount=1
   feedback='Mounted charge admission failed: InvalidOperationException.';rejectionCodes=@('CommandAdmissionFailure')
   chargeAdmitted=0;chargeRefused=1;lastRefusal='Mounted charge admission failed: InvalidOperationException.'
   after=(New-State 'input-after' 9.0 6.0 $false)}
  samples=@()
  after=(New-State 'after' 9.0 6.0 $false)
  identityAfter=(New-Identity $true $true)
  movement=[ordered]@{mountDistance=0.0;riderDistance=0.0;peakSpeedMps=0.0;mountCombatSpeedMps=5.08
   chargingObserved=$false;chargeModeObserved=$false;riderChargeStateObserved=$false}
  economy=[ordered]@{riderStandardMax=6.0;riderMoveMax=0.0;mountStandardMax=0.0;mountMoveMax=0.0
   riderStandardNow=6.0;riderMoveNow=0.0;mountStandardNow=0.0;mountMoveNow=0.0}
  lease=$null;transaction=$null;landingBlockers=$null;landingBlocker=$null;targetLoss=$null;targetMove=$null;incapacity=$null
  nonDelivery=$null;strandedShellInterrupted=$false;clickRetries=0;clickRetryObservations=@()
  admissionFault=[ordered]@{contract='post-queue-charge-admission-fault-compensated-exactly'
   armed=$true;fired=$true;seamClearedAfterFire=$true;frame=140;compensationCount=1
   atClick=[ordered]@{frame=111;armed=$true;firedByClick=$false;seamStillArmed=$true;riderCommandsEmpty=$false
    mountCommandsEmpty=$true;state=(New-State 'admission-fault-after-click' 9.0 0 $false)}
   atSettle=(New-State 'admission-fault-at-settle' 9.0 6.0 $false)
   compensation='reason=injected;ran=True;complete=True;failures=;unmet=';compensationComplete=$true
   commandResident=$false;leaseRestored=$true;activeCommandCleared=$true;unmetPostconditions=''
   faultedCleanupOwner=$false;riderCommandsEmpty=$true;mountCommandsEmpty=$true
   afterClick=(New-State 'admission-fault-after-click' 9.0 6.0 $false)}
  intervention=$null
  delivery=[ordered]@{chargeAdmitted=0;chargeRefused=1;lastRefusal='Mounted charge admission failed: InvalidOperationException.'
   feedback='Mounted charge admission failed: InvalidOperationException.';rejectionCodes=@('CommandAdmissionFailure')}
  rules=(New-Rules 0 0 $false)
  attackRules=0;attackRulesOpportunity=0
  pairCommandState=[ordered]@{frame=200}
  terminal=$null
 }}
}
function New-Row([string]$Case){
 if($Case-cin@('C6B-CHARGE-action-failed-before-rule','C6B-CHARGE-action-failed-after-rule')){
  $registered=$Case-ceq'C6B-CHARGE-action-failed-after-rule'
  $r=New-AdmissionFaultRow;$r.name=$Case;$e=$r.evidence;$e.case=$Case
  $e.delivery.chargeAdmitted=0;$e.delivery.chargeRefused=$(if($registered){1}else{0})
  $e['ownership']=@{owned=$false};$owner=New-DrainedOwner;$owner.lease=$null;$owner.committed=$false
  $owner['rider']='rider';$owner['mount']='mount';$owner['target']='target';$owner['processObservationError']=$null
  $e.economy.riderStandardMax=0;$e.economy.riderStandardNow=0
  $owner['nativeActionFailed']=$true;$owner['nativeActionInProgress']=$false;$owner['processObservationPending']=$false
  $owner['processObserved']=$registered;$owner['processCount']=$(if($registered){1}else{0});$owner['shellProcessAssigned']=$false
  $e['lastDrainedOwnership']=$owner
  $e['nativeActionFault']=@{armed=$true;fired=$true;seamCleared=$true;boundary=$(if($registered){'after-rule'}else{'before-rule'});ownerAtFault=@{
   identity=17;rider='rider';mount='mount';target='target';owned=$true;committed=$false;nativeActionInProgress=$true;nativeRuleObserved=$true;nativeExecutorObserved=$true;
   shellProcessAssigned=$false;ruleProcessAssigned=$registered;processObserved=$registered;processCount=$(if($registered){1}else{0})}}
  $e.nativeActionFault['shellAtFault']=@{present=$true;abilityGuid=$kmc;executorId='rider';targetId='target'}
  $live=$e.nativeActionFault.ownerAtFault
  foreach($field in @('shellIdentity','ruleIdentity','executorIdentity','contextIdentity')){$live[$field]=123;$owner[$field]=123}
  $live['processes']=@();$owner['processes']=@()
  if($registered){$live.processes=@(@{identity=124;contextIdentity=123;ended=$false});$owner.processes=@(@{identity=124;contextIdentity=123;ended=$true})}
  $e.after['riderCommandsEmpty']=$true;$e.after['mountCommandsEmpty']=$true
  return $r
 }
 if($Case-ceq'C6B-CHARGE-child-cleanup'){
  $r=New-TerminatedRow $Case 'native-child-fact-cleanup';$owner=New-DrainedOwner;$lease=$owner.lease
  $lease.buffComponentTypes=@('Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions')+@($lease.buffComponentTypes)+@('Kingmaker.UnitLogic.FactLogic.AddContextStatBonus','Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig')
  $lease.buffChildren.surface='native-cotw-1.14.4c-2.1'
  $guids=@('6683a35444eb42ddbd21f87c3441a50a','b0439659723f4a8da680965c78a8fbf5','30f90becaaac51f41bf56641966c4121','3f032a3cd54e57649a0cdad0434bf221','61aff33f69d84391b49782fb976cf870')
  $nodes=@();$children=@()
  for($i=0;$i-lt5;$i++){
   $ench=$i-in@(2,3);$kind=if($ench){'enchantment'}else{'buff'};$native=Copy-Case $lease.buffNative;$native.identity=101+$i
   if($ench){$native.collection=300;$native.componentCount=1;$native.disposed=$false}
   $fx=$null
   if($ench){$fx=@{scopes=0;pendingRoots=0;pendingCopies=0;retiring=$true;attached=$false;drained=$true;fault=$null;failures=@();roots=0;copies=0;rootFacts=@();acquisitions=@()}}
   if($i-eq2){$fx.roots=1;$fx.copies=2;$fx.rootFacts=@(@{generation=1;returned=$true;custodyUncertain=$false;controllerResidueAbsent=$true});$fx.acquisitions=@(1,2,3|ForEach-Object{@{generation=$_;returned=$true;completionRequested=$true;failure=$null}})}
   $nodes+=@{id=($i+1);parent=$(if($ench){2}else{0});blueprint=$guids[$i];kind=$kind;created=$true;acquiring=$false;acquired=$true;identityUncertain=$false;removalAttempted=$true;fault=$null;settled=$true;drained=$true;native=$native;visuals=$fx}
   if(-not$ench){$children+=@{blueprint=$guids[$i];identity=$native.identity;storedByParent=($i-lt4);active=$true;sameContextParent=$true}}
  }
  $lease.buffChildren.facts=$nodes;$r.evidence.lease=$lease;$r.evidence['lastDrainedOwnership']=$owner
  $r.evidence['boundary']=@{kind='native-child-fact-cleanup';ownerBefore=@{owned=$true;identity=17};ownerAfter=@{owned=$false};lastDrained=$owner;childStimulus=@{contract='native-lifetime-stimulus-no-feat-or-action-grant';rider='rider';rootIdentity=100;allAcquired=$true;children=$children;ownerWithChildren=@{identity=17}}}
  return $r
 }
 if($Case-ceq'C6B-CHARGE-view-replaced'){
  $r=New-TerminatedRow $Case 'native-view-replacement'
  $r.evidence.mounted=$false;$r.evidence.after.relationship='Unmounted';$r.evidence.identityAfter=New-Identity $false $true
  $effects=@{size=4;speed=5.08;stats=@(10,12,14);facts=@('f');bodyPolymorphed=$false;inspectionOverride=$false;asksOverride=$false;
   sourceBones=@();boneReplaced=$false;boneDefault=$false;handSet=0;equipment=@(@{primary=@{identity=1;blueprint='w'};secondary=$null});limbs=@();stockLimbs=@();polymorphHands=$false;polymorphLimbs=$false}
  $v=@{actor='rider';blueprint='00d8fbe9cf61dc24298be8d95500c84b';name='BeastShapeIBuff';prefab='0dc0f602a83a2034ba5842f73c0012c1';
   components=@('Kingmaker.UnitLogic.Buffs.Polymorph','Kingmaker.Blueprints.Classes.Spells.SpellDescriptorComponent','Kingmaker.Designers.Mechanics.Buffs.BuffMovementSpeed','Kingmaker.Designers.Mechanics.Buffs.ReplaceAsksList','Kingmaker.Designers.Mechanics.Facts.ReplaceSourceBone');
   applyCalls=1;removeCalls=1;restored=$true;factDisposed=$true;listenersRemaining=0;originalRetired=$true;replacementRetired=$true;effectsBefore=$effects;
   before=@{frame=100;view=10;bound=$true;polymorph=$false;buffCount=0;effects=$effects};afterApply=@{frame=130;view=11;bound=$true;polymorph=$true;buffCount=1};
   afterRestore=@{frame=160;view=12;bound=$true;polymorph=$false;buffCount=0;effects=$effects};attachments=@()}
  $v.components+=@('Kingmaker.UnitLogic.FactLogic.SpecificBuffImmunity')*8
  $v['sizeImmunities']=@('4f139d125bb602f48bfaec3d3e1937cb','b0793973c61a19744a8630468e8f4174','c84fbb4414925f344b894e9511626296',
   '17206974f2a2c164db26d1af7fac57d5','3fca5d38053677044a7ffd9a872d3a0a','4ce640f9800d444418779a214598d0a3',
   '6ba82f2c8a7146e6b4880cbe7f8534e8','c5d35ba066ae4a079a7d86a316d3ef38')
  foreach($i in 0..1){$token=if($i-eq0){'06002a08'}else{'06002a09'};$v.attachments+=@{frame=130+$i;actor='rider';view=11+$i;nativeSource=@(@{token=$token;assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},@{token='06007e9d';assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},@{token='0600835c';assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})}}
  $r.evidence['boundary']=@{kind='native-view-replacement';ownerBefore=@{owned=$true;identity=17};ownerAfter=@{owned=$false};lastDrained=(New-DrainedOwner);relationshipAfter='Unmounted';presentationResidue=$false;nativeView=$v}
  return $r
 }
 if($Case-cin@('C6B-CHARGE-mount-dead','C6B-CHARGE-rider-dead')){
  $kind=if($Case-ceq'C6B-CHARGE-mount-dead'){'mount'}else{'rider'}
  $r=New-TerminatedRow $Case ('native-'+$kind+'-death')
  $r.evidence.mounted=$false;$r.evidence.after.relationship='Unmounted';$r.evidence.identityAfter=New-Identity $false $true
  $r.evidence['death']=New-DeathFacts $kind
  return $r
 }
 if($Case-ceq'C6B-CHARGE-lease-application-failed'){
  $r=New-TerminatedRow $Case 'injected-lease-fault'
  $r.evidence['leaseFault']=@{armed=$true;fired=$true;seamCleared=$true;ownerAtFault=@{committed=$true;identity=17};stateAtFault=(New-State 'fault' 9 6 $true)}
  $r.evidence.lease.applied=$false;$r.evidence.lease['applyRolledBack']=$true;$r.evidence.lease['applyFailedStep']='forced-path';$r.evidence.lease['forcedPathAppliedBeforeFailure']=$true
  $r.evidence.transaction.leaseApplicationFailed=$true;$r.evidence.transaction.leaseApplicationFailedStep='forced-path';$r.evidence.transaction.leaseRolledBackOnFailure=$true
  return $r
 }
 if($Case-ceq'C6B-CHARGE-relationship-invalidated'){
  $r=New-TerminatedRow $Case 'native-ownership-loss'
  $original=@{riderPetId='mount';masterId='rider';mountIsPet=$true}
  $r.evidence.mounted=$false;$r.evidence.after.relationship='Unmounted';$r.evidence.identityAfter=New-Identity $false $true
  $r.evidence['boundary']=@{kind='native-ownership-loss';ownerBefore=@{owned=$true;identity=17};ownerAfter=@{owned=$false};lastDrained=(New-DrainedOwner);relationshipAfter='Unmounted';nativeOwnership=@{before=$original;detached=@{riderPetId=$null;masterId=$null;mountIsPet=$false};method=@{token='06001F17';method='SetMaster';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'};restoration=@{pass=$true;count=1;vacantReciprocalReferences=$true;inputsUnchanged=$true;after=$original}}}
  return $r
 }
 if($Case-cin @('C6B-CHARGE-feature-disabled','C6B-CHARGE-dismounted','C6B-CHARGE-mode-changed','C6B-CHARGE-new-landing-blocker')){
  $kinds=@{'C6B-CHARGE-feature-disabled'='feature-disabled';'C6B-CHARGE-dismounted'='native-dismount-request';'C6B-CHARGE-mode-changed'='native-mode-change';'C6B-CHARGE-new-landing-blocker'='native-new-landing-blocker'}
  $r=New-TerminatedRow $Case $kinds[$Case]
  $r.evidence['boundary']=[ordered]@{kind=$kinds[$Case];ownerBefore=@{owned=$true;identity=17};ownerAfter=@{owned=$false};lastDrained=(New-DrainedOwner);controlAfter=(New-Identity $false $false);nativeSettingAfter=$true;tbInitialized=$true;modeRestored=$true;clicked=$true;relationshipAfter='Unmounted';placed=$true}
  if($Case-ceq'C6B-CHARGE-feature-disabled'){$r.evidence.identityAfter=New-Identity $false $false}
  if($Case-ceq'C6B-CHARGE-dismounted'){
   $r.evidence.mounted=$false;$r.evidence.after.relationship='Unmounted';$r.evidence.identityAfter=New-Identity $false $true
   $proof=New-CommandProof $true $false 0
   foreach($identity in @($proof.identity)+@($proof.samples|ForEach-Object{$_.identity})){$identity.targetId='rider';$identity.abilityGuid='3af2b81f4d72bbb30501fa730fcdf36e'}
   foreach($state in @($proof.preClick.state)+@($proof.samples|ForEach-Object{$_.state})){
    $state.rider.standard=6.0;$state.rider.move=0.0;$state.rider.swift=0.0
    $state.mount.standard=0.0;$state.mount.move=0.0;$state.mount.swift=0.0
   }
   $terminal=@($proof.samples|Where-Object boundary -CEQ 'terminal')[0]
   $terminal.state.rider.standard=4.0;$terminal.state.rider.move=1.75
   foreach($event in $proof.resourceWindow.events){$event.state.standard=5.0;$event.state.swift=0.0}
   $r.evidence.boundary['dismountProof']=$proof
   $r.evidence.boundary['beforeRequest']=New-State 'before-dismount' 8 6 $true
   $r.evidence.economy.riderMoveMax=2.75;$r.evidence.economy.riderMoveNow=1.75
  }
  if($Case-ceq'C6B-CHARGE-new-landing-blocker'){
   $r.evidence.transaction.revalidationFailed=$true
   $r.evidence.transaction.revalidationFailureCode='NoPath';$r.evidence.transaction.revalidationFailureReason='Another creature blocks the charge landing point.';$r.evidence.transaction.revalidationFailurePhase='BeforeAttackTransition'
   $r.evidence.boundary['landingClearBefore']=$true
   $r.evidence.boundary['blocker']=@{placed=$true;blockerId='blocker-1';contract='diagnostic-blocker-occupies-the-exact-charge-landing-point'}
   $r.evidence.boundary['blockersAfter']=@(@{actorId='blocker-1';distanceToLanding=0.1;threshold=1.8})
   $r.evidence.boundary['spawnFrame']=$r.evidence.intervention.frame
   $r.evidence.boundary['awakeFrame']=$r.evidence.intervention.frame+1
   $r.evidence.boundary['awakeBlocker']='blocker-1'
   $r.evidence.after.frame=160
  }
  return $r
 }
 if($Case-ceq'C6B-CHARGE-interrupted'){ return New-TerminatedRow $Case 'native-command-interrupt' }
 if($Case-ceq'C6B-CHARGE-combat-ended'){ return New-TerminatedRow $Case 'native-combat-end' }
 if($Case-ceq'C6B-CHARGE-target-lost'){ return New-TerminatedRow $Case 'native-target-removed' }
 if($Case-ceq'C6B-CHARGE-rider-incapacitated'){ return New-TerminatedRow $Case 'native-rider-incapacity' }
 if($Case-ceq'C6B-CHARGE-mount-incapacitated'){ return New-TerminatedRow $Case 'native-mount-incapacity' }
 if($Case-ceq'C6B-CHARGE-exception-cleanup'-and$script:fixtureMode-cne'TB'){ return New-AdmissionFaultRow }
 # Turn-based delivery is implemented behind increment 6B.3's bounded seam, so the turn-based positive
 # row is a delivery like its real-time counterpart; every other turn-based row is still a refusal.
 # One flag, read everywhere the mode used to be read, so those places cannot disagree.
 $tbRefusal=$script:fixtureMode-ceq'TB'
 $refusal=$tbRefusal-or$Case-cin @('C6B-CHARGE-beyond-maximum','C6B-CHARGE-below-minimum','C6B-CHARGE-spent-standard','C6B-CHARGE-stock-rejected','C6B-CHARGE-obstructed-line','C6B-CHARGE-blocked-clearance','C6B-CHARGE-cancelled')
 # The cancellation row is on offer and then released: nothing happens, but the charge was genuinely
 # available and targetable, which is what makes the row mean anything.
 $offered=$Case-ceq'C6B-CHARGE-cancelled'
 if($Case-ceq'C6B-CHARGE-default-off'){
  return [ordered]@{name=$Case;status='PASS';evidence=[ordered]@{level='NATIVE DELIVERY';mode=$script:fixtureMode;case=$Case;mounted=$true
   settingOff=(New-Identity $false $false);settingOn=(New-Identity $true $true);abilityGuid=$kmc}}
 }
 $distance=if($Case-ceq'C6B-CHARGE-beyond-maximum'){18.0}elseif($Case-ceq'C6B-CHARGE-below-minimum'){3.5}else{9.0}
 $riderStandard=if($Case-ceq'C6B-CHARGE-spent-standard'){6.0}else{0.0}
 $input=[ordered]@{clicked=(-not$refusal);hoverPure=$true;frame=110
  shell=[ordered]@{present=(-not$refusal);abilityGuid=$kmc;executorId='rider';targetId='target'};shellCount=$(if($refusal){0}else{1})
  requestWindow=@{beforeSequence=10;afterSequence=11;records=@(@{sequence=11;activationId=7;phase='CastRequested';kind='MountedCharge';ability=$kmc;caster='rider';target='target';frame=110;accepted=$null;reason='native-cast-requested'})}
  feedback=$(if($Case-ceq'C6B-CHARGE-stock-rejected'){'Charge is not yet supported while mounted.'}else{'Mounted charge accepted: the Horse carries the charge.'})
  rejectionCodes=@();chargeAdmitted=$(if($refusal){0}else{1});chargeRefused=$(if($refusal){1}else{0});lastRefusal=$null
  after=(New-State 'input-after' $distance $riderStandard (-not$refusal))}
 if($offered){
  $input['selectedAfterSet']=$true;$input['selectedAfterCancel']=$false
 }
 if($Case-ceq'C6B-CHARGE-stock-rejected'){
  $input['stockBlueprint']=$stock;$input['stockAvailable']=$false;$input['stockCanTarget']=$false
  $input['safetyFeedback']='Charge is not yet supported while mounted.'
  $input['before']=(New-State 'stock-before' $distance 0 $false);$input['after']=(New-State 'stock-after' $distance 0 $false)
 }
 $row=[ordered]@{name=$Case;status='PASS';evidence=[ordered]@{
  level='NATIVE DELIVERY';mode=$script:fixtureMode;case=$Case;mounted=$true
  before=[ordered]@{identity=(New-Identity $true $true);state=(New-State 'before' $distance $riderStandard $false)
   available=$(if(($refusal-and-not$offered)-or$tbRefusal){$false}else{$true});unavailableReason=$null
   kmcAvailabilityReason=$(if($tbRefusal){'Mounted Charge is not yet supported in turn-based mode.'}else{'Mounted Charge is available.'})
   canTarget=$(if(($refusal-and-not$offered)-or$tbRefusal){$false}else{$true});minRangeMeters=4.65;approachDistance=99.0
   geometry=[ordered]@{straightRoute=$(if($Case-ceq'C6B-CHARGE-obstructed-line'){$false}else{$true});landingBlocked=$($Case-ceq'C6B-CHARGE-blocked-clearance');mountDistanceToTarget=$distance}
   requireFullRound=$true;commandType='Standard';pairCommandState=[ordered]@{frame=100}}
  input=$input
  samples=@()
  after=(New-State 'after' $distance $riderStandard $false)
  identityAfter=(New-Identity $true $true)
  movement=[ordered]@{mountDistance=$(if($refusal){0.0}else{6.8});riderDistance=$(if($refusal){0.0}else{6.8})
   peakSpeedMps=$(if($refusal){0.0}else{10.3});mountCombatSpeedMps=5.08;chargingObserved=(-not$refusal)
   chargeModeObserved=(-not$refusal);riderChargeStateObserved=(-not$refusal)}
  economy=[ordered]@{riderStandardMax=$(if($refusal){0.0}else{6.0});riderMoveMax=$(if($refusal-or$script:fixtureMode-ceq'RT'){0.0}else{3.0})
   mountStandardMax=0.0;mountMoveMax=0.0;riderStandardNow=$riderStandard;riderMoveNow=0.0;mountStandardNow=0.0;mountMoveNow=0.0}
  lease=$(if($refusal){$null}else{New-Lease})
  transaction=$(if($refusal){$null}else{New-Transaction $true})
  admissionFault=$null;landingBlockers=$null;landingBlocker=$null;targetLoss=$null;incapacity=$null
  nonDelivery=$null;strandedShellInterrupted=$false;clickRetries=0;clickRetryObservations=@()
  targetMove=$(if($Case-ceq'C6B-CHARGE-target-moved'){New-TargetMove}else{$null})
  delivery=[ordered]@{chargeAdmitted=$(if($refusal){0}else{1});chargeRefused=0;lastRefusal=$null
   feedback=$(if($Case-ceq'C6B-CHARGE-stock-rejected'){'Charge is not yet supported while mounted.'}else{'Mounted charge accepted: the Horse carries the charge.'});rejectionCodes=@()}
  rules=(New-Rules $(if($refusal){0}else{1}) 0 $true)
  attackRules=$(if($refusal){0}else{1});attackRulesOpportunity=0
  pairCommandState=[ordered]@{frame=200}
  terminal=$(if($refusal){$null}else{[ordered]@{action='RiderMelee';actorId='rider';resourceOwnerId='rider';targetId='target';result='Success';childAttackStartCount=1;singleAttackMode=$true;nativeFullAttack=$false;nativePlannedAttackCount=1;nativeCompletedAttackCount=1;repathCount=$(if($Case-ceq'C6B-CHARGE-target-moved'){1}else{0})}})
 }}
 if($Case-ceq'C6B-CHARGE-blocked-clearance'){
  # The landing point one weapon reach short of the target, occupied by a named awake actor.
  $row.evidence['landingBlockers']=@([ordered]@{actorId='blocker-1';blueprint='bp-mammoth';playerFaction=$false
   corpulence=1.75;distanceToLanding=0.11;threshold=1.8;position=@(5.25,0,0)})
  $row.evidence['landingBlocker']=[ordered]@{contract='diagnostic-blocker-occupies-the-exact-charge-landing-point'
   separation=3.75;weaponReach=1.5;mountCorpulence=0.5;targetCorpulence=1.75;targetPosition=@(9,0,0)
   wantedLanding=@(5.25,0,0);placed=$true;blockerId='blocker-1';blockerPosition=@(5.25,0,0)
   blockerCorpulence=1.75;blockerDistanceToLanding=0.11;blockerAvoidanceDisabled=$false;brainLeased=$true}
 }
 if($Case-ceq'C6B-CHARGE-beyond-maximum'){
  $row.evidence.before['targetCode']='OutsideSupportedRange';$row.evidence.before['targetReason']='The charge target is farther than the maximum charge distance of 15.24 m.'
  $row.evidence['rangeFixture']=@{applied=@{actor='mount';present=$true;slowed=$true;nativeComponent='Kingmaker.UnitLogic.FactLogic.AddCondition';blueprint='own-slow';speedBefore=5.08;speedNow=2.54;maximumNow=15.24};beforeRestore=@{blueprint='own-slow'};afterRestore=@{actor='mount';blueprint='own-slow';restored=$true;present=$false;slowed=$false;speedNow=5.08}}
 }
 if($Case-ceq'C6B-CHARGE-duplicate'){
  $row.evidence['boundary']=@{kind='native-duplicate-request';availableBefore=$false;admittedBefore=1;admittedAfter=1;ownerBefore=@{owned=$true;state='Active';identity=17;target='target'};ownerAfterRequest=@{identity=17;state='Active'};lastDrained=(New-DrainedOwner);input=@{abilityGuid=$kmc;clickedTargetId='target';resolvedTargetId='target';dispatchAcceptedDelta=0}}
 }
 $row
}
function New-DrainedOwner { [ordered]@{identity=17;state='FullyDrained';attempts=1;committed=$true;commandTerminal=$true;riderSlotReleased=$true;riderContainerReleased=$true;schedulerAbsent=$true;carrierDrained=$true;leaseDrained=$true;lease=(New-Lease);shellTerminal=$true;shellContainerReleased=$true;processEnded=$true;manualTargetOwned=$false;manualTargetReleased=$true;debt=''} }
function New-DeathFacts([string]$Kind){
 $states=@{}
 foreach($phase in @('before','afterDamage','terminated','encounterExit','afterPolicyRestore')){
  $s=[ordered]@{frame=130;gameTicks=100000000;relationship='Unmounted';presentationResidue=$false;ownership=@{owned=$false};lastDrained=(New-DrainedOwner)
   boundaries=@{pending=$false};playerInCombat=$false;riderInCombat=$false;mountInCombat=$false;controllerInitialized=$false;trackedAllocations=0;pairedIdentity=$false;partnerContext=$false;riderGrants=0;mountGrants=0}
  foreach($actor in @('rider','mount')){
   $s[$actor]=[ordered]@{id=$actor;conscious=$true;dead=$false;finallyDead=$false;directlyControllable=$true;inState=$true;enabledRenderers=1;commandsEmpty=$true;hp=30;constitution=14;damage=0;characterLevel=5;nonLethalDamage=0}
  }
  if($phase-cin@('before','afterDamage')){$s.relationship='Mounted';$s.ownership=@{owned=$true;identity=17};$s.playerInCombat=$true;$s.riderInCombat=$true;$s.mountInCombat=$true}
  if($phase-cne'before'){$s[$Kind].damage=46}
  if($phase-cin@('terminated','encounterExit','afterPolicyRestore')){
   $s.frame=132;$s[$Kind].dead=$true;$s[$Kind].conscious=$false;$s[$Kind].finallyDead=($Kind-ceq'rider');$s[$Kind].directlyControllable=$false
  }
  if($phase-cin@('encounterExit','afterPolicyRestore')){
   $s.frame=140
   if($Kind-ceq'mount'){$s.mount.dead=$false;$s.mount.conscious=$true;$s.mount.directlyControllable=$true;$s.mount.damage=25}
  }
  if($phase-ceq'afterPolicyRestore'){$s.frame=150;$s.gameTicks=103000000}
  $states[$phase]=$s
 }
 $policy=[ordered]@{trueDeath=$false;damageToParty=1.0;riseAfterCombat=@{persisted='true'};deathDoor=@{persisted='false'}}
 $effective=Json $policy;$effective.trueDeath=($Kind-ceq'rider')
 $events=@(@{kind='native-life-state';actor=$Kind;lifeState='Dead';frame=132;nativeSource=@(@{token='06009164';assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})})
 if($Kind-ceq'mount'){$events+=@(@{kind='native-life-state';actor=$Kind;lifeState='Conscious';frame=138;nativeSource=@('0600918e','06009191','06009164'|ForEach-Object {@{token=$_;assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}})})}
 [ordered]@{subjectKind=$Kind;subject=$Kind;source='target';damageDispatches=1;requestedDamage=46;nativeDamage=46;nativeDamageBeforeDifficulty=46;damageToParty=1.0;deathThreshold=44
  before=$states.before;afterDamage=$states.afterDamage;terminated=$states.terminated;encounterExit=$states.encounterExit;afterPolicyRestore=$states.afterPolicyRestore
  enemyDamageDispatches=1;enemyDamageSource='main';enemyBefore=@{id='target';dead=$false};enemyAfter=@{id='target';dead=$true};enemyLifeTransitions=1;enemyDamageFrame=133;enemyNativeDamage=60
  allocationTrace=@{dropped=0;observationErrors=0;observerHooks=@('exact')};policy=@{permanentDeathFixture=($Kind-ceq'rider');before=$policy;effective=$effective;restoration=@{restored=$true;state=$policy}};lifeEvents=@{events=$events}}
}
# The native target move that changes the charge geometry mid-approach.
function New-TargetMove{
 [ordered]@{contract='native-target-move-forces-charge-revalidation-and-repath';targetId='target'
  targetOrigin=@(9,0,0);attempts=@([ordered]@{point=@(9,0,3);movedFromOrigin=3.0;riderDistance=9.5;withinFixtureBounds=$true})
  destination=@(9,0,3);issued=$true;targetCommandsEmptyBefore=$true;mountDistanceAtStimulus=4.2;frame=130
  moveFinished=$true;moveResult='Success';targetMovedDistance=3.0;targetPositionAtSettle=@(9,0,3);mountDistanceAtSettle=2.1}
}
function New-Artifact([string]$Mode='RT'){
 $script:fixtureMode=$Mode
 $rows=@(Get-KmcChunk6bChargeRows $Mode|ForEach-Object { New-Row $_ })
 foreach($row in $rows){
  if($row.evidence.Contains('delivery')-and$row.evidence.delivery.chargeAdmitted-gt0){
   $row.evidence['ownership']=@{owned=$false};$row.evidence['lastDrainedOwnership']=New-DrainedOwner
  }
 }
 $artifact=Copy-Case ([ordered]@{schemaVersion=39;evidenceKind='phase3d-horse-scenario-evidence';scenario='chunk6b-charge-rt';status='PASS'
  rows=$rows
  observations=[ordered]@{initialSelection=@('main');cleanup=@{selectionRestored=$true;equipmentSetRestored=$true;settingRestored=$true;pairedSchedulerSettingRestored=$true;targetClean=$true;chunk4OtherTargetReleased=$true;modeRestored=$true;unmountedHorseAiLeaseRestored=$true;combatMountRiderAiLeaseRestored=$true;relationshipState='Unmounted';playerInCombat=$false;nativeTurnBased=$false;nativeControllerInitialized=$false;nativeFinalDeathSelectionExclusion=$(if($Mode-ceq'RT'){'rider'}else{$null});expectedSelection=@('main');actualSelection=@('main')};chunk6bCharge=[ordered]@{contract='chunk6b-pair-charge-delivery';mode=$Mode;cases=@(Get-KmcChunk6bChargeRows);abilityGuid=$kmc;stockChargeBlueprint=$stock;beyondMaximumReachable=$false;spawnEnvelopeMinimum=3.0;spawnEnvelopeMaximum=20.0;settingBefore=$false;settingAfter=$false;settingRestored=$true}}
  subscenarioPassCount=$rows.Count;subscenarioFailCount=0;errors=@()})
 if($Mode-ceq'RT'){
  $artifact.observations.chunk6bCharge|Add-Member -NotePropertyName fixtureOrigin -NotePropertyValue ([pscustomobject]@{x=0.0;y=0.0;z=0.0})
  foreach($row in $artifact.rows|Where-Object name -CNE 'C6B-CHARGE-default-off'){
   $artifact.observations.chunk6bCharge|Add-Member -NotePropertyName ('positioning-'+$row.name) -NotePropertyValue (New-ChargeFixtureReturn $row.name)
  }
 }
 $artifact
}
# The obstructed-line row as it is recorded in a fixture area where every swept direction at the lawful
# distance offered a clear native line.
function New-ObstructedLimitationArtifact {
 $a=New-Artifact 'RT'
 $row=@($a.rows|Where-Object {$_.name-ceq'C6B-CHARGE-obstructed-line'})[0]
 $row.evidence=(Json ([ordered]@{level='NATIVE DELIVERY';mode='RT';case='C6B-CHARGE-obstructed-line';mounted=$true
  limitation='no-obstructed-line-in-fixture-area';obstructedLineReachable=$false
  placement=[ordered]@{origin=@(0,0,0);wantedDistance=9.0;wants='obstructed-straight-line'
   attempts=@(
    [ordered]@{point=@(9,0,0);nativeTrace=@(9,0,0);straightRoute=$true;landingBlockedEstimate=$false;riderDistance=9.0;withinFixtureBounds=$true},
    [ordered]@{point=@(0,0,9);nativeTrace=@(0,0,9);straightRoute=$true;landingBlockedEstimate=$false;riderDistance=9.1;withinFixtureBounds=$true})}}))
 $a
}

$request=[pscustomobject]@{scenario='chunk6b-charge-rt'}
function Accept([string]$Name,[scriptblock]$Body){ & $Body; $script:checks++; Write-Host ('PASS '+$Name) }
function Reject([string]$Name,[scriptblock]$Body){ $failed=$false; try { & $Body } catch { $failed=$true }; if(-not$failed){ throw ('Invalid evidence accepted: '+$Name) }; $script:checks++; Write-Host ('PASS refuses '+$Name) }
function Mutate([string]$Name,[scriptblock]$Change){ $a=New-Artifact; & $Change $a; Reject $Name { Assert-KmcChunk6bChargeEvidence $request $a 'PASS' } }
function Row($Artifact,[string]$Case){ @($Artifact.rows|Where-Object {$_.name-ceq$Case})[0].evidence }
function RejectWith([string]$Name,[string]$Expected,[scriptblock]$Body){
 $message=$null; try { & $Body } catch { $message=[string]$_.Exception.Message }
 if($null-eq$message){ throw ('Invalid evidence accepted: '+$Name) }
 if($message-notlike('*'+$Expected+'*')){ throw ('Rejected for the wrong reason: '+$Name+' -> '+$message) }
 $script:checks++; Write-Host ('PASS refuses '+$Name)
}
function MutateWith([string]$Name,[string]$Expected,[scriptblock]$Change){
 $a=New-Artifact; & $Change $a; RejectWith $Name $Expected { Assert-KmcChunk6bChargeEvidence $request $a 'PASS' }
}
# The limitation artifacts for the two rows the fixture area may not be able to present.
function New-ClearanceLimitationArtifact {
 $a=New-Artifact 'RT'
 $row=@($a.rows|Where-Object {$_.name-ceq'C6B-CHARGE-blocked-clearance'})[0]
 $row.evidence=(Json ([ordered]@{level='NATIVE DELIVERY';mode='RT';case='C6B-CHARGE-blocked-clearance';mounted=$true
  limitation='no-blocked-landing-in-fixture-area';blockedClearanceReachable=$false
  blocker=[ordered]@{contract='diagnostic-blocker-occupies-the-exact-charge-landing-point';separation=3.75
   weaponReach=1.5;mountCorpulence=0.5;targetCorpulence=1.75;targetPosition=@(9,0,0);wantedLanding=@(5.25,0,0)
   placed=$false;reason='the native graph offers no walkable landing point'}
  placement=[ordered]@{origin=@(0,0,0);wantedDistance=9.0;attempts=@([ordered]@{point=@(9,0,0);straightRoute=$true})}}))
 $a
}
function New-IncapacityLimitationArtifact([string]$Case,[string]$Kind){
 $a=New-Artifact 'RT'
 $row=@($a.rows|Where-Object {$_.name-ceq$Case})[0]
 $row.evidence=(Json ([ordered]@{level='NATIVE DELIVERY';mode='RT';case=$Case;mounted=$true
  limitation='native-incapacity-window-absent'
  incapacity=[ordered]@{contract='one-native-ruledeal-damage-to-incapacitation-window-mid-charge';subjectKind=$Kind
   subjectId=$Kind;window='absent';windowReason='native difficulty leaves no safe incapacity window below death'
   measurement=[ordered]@{difficulty=4.0;hitPoints=30;constitution=14;temporaryHitPoints=0;damageBefore=0;desiredDamage=31;deathThreshold=44;needed=31;requestedDamage=8;projectedDamage=32}}
  placement=[ordered]@{origin=@(0,0,0);wantedDistance=9.0;attempts=@([ordered]@{point=@(9,0,0);straightRoute=$true})}}))
 $a
}

Accept 'a lawful pair-owned mounted charge delivery' { Assert-KmcChunk6bChargeEvidence $request (New-Artifact) 'PASS' }
# A native attack of opportunity alongside the one deliberate charge attack is the engine's own reflex.
$withOpportunity=New-Artifact; (Row $withOpportunity 'C6B-CHARGE-positive').rules=(Json (New-Rules 1 1 $true)); (Row $withOpportunity 'C6B-CHARGE-positive').attackRules=2; (Row $withOpportunity 'C6B-CHARGE-positive').attackRulesOpportunity=1
Accept 'a native opportunity attack during the charge' { Assert-KmcChunk6bChargeEvidence $request $withOpportunity 'PASS' }
# The native charge buff may lawfully outlive the transaction for its own duration.
$buffRemains=New-Artifact; (Row $buffRemains 'C6B-CHARGE-positive').after.chargeBuffPresent=$true
Accept 'the native charge buff outliving the transaction' { Assert-KmcChunk6bChargeEvidence $request $buffRemains 'PASS' }

Mutate 'wrong schema' {param($a) $a.schemaVersion=33}
Mutate 'another delivery contract' {param($a) $a.observations.chunk6bCharge.contract='other'}
Mutate 'another ability in the contract' {param($a) $a.observations.chunk6bCharge.abilityGuid=$stock}
Mutate 'an unrestored mod setting' {param($a) $a.observations.chunk6bCharge.settingRestored=$false}
Mutate 'a setting left on' {param($a) $a.observations.chunk6bCharge.settingAfter=$true}
Mutate 'a missing required row' {param($a) $a.rows=@($a.rows[0],$a.rows[1]);$a.subscenarioPassCount=2}
Mutate 'a duplicate row' {param($a) $a.rows=@($a.rows[0],$a.rows[0],$a.rows[2],$a.rows[3],$a.rows[4]);$a.subscenarioPassCount=5}
Mutate 'an unknown row' {param($a) $a.rows[1].name='C6B-CHARGE-other';$a.rows[1].evidence.case='C6B-CHARGE-other'}
Mutate 'a failure-only row claimed PASS' {param($a) $a.rows+=@([pscustomobject]@{name='phase3d-horse-runtime-exception';status='PASS';evidence=$null});$a.subscenarioPassCount=$a.rows.Count}
Mutate 'row counts that differ' {param($a) $a.subscenarioPassCount=$a.rows.Count+1}
Mutate 'an unmounted row' {param($a) (Row $a 'C6B-CHARGE-positive').mounted=$false}
Mutate 'the stock Charge replaced' {param($a) (Row $a 'C6B-CHARGE-positive').before.identity.stockChargePresent=$false}
Mutate 'the stock Charge logic changed' {param($a) (Row $a 'C6B-CHARGE-positive').before.identity.stockChargeIsStockLogic=$false}
Mutate 'a charge that is not a full-round action' {param($a) (Row $a 'C6B-CHARGE-positive').before.identity.kmcChargeFullRound=$false}
Mutate 'a charge that is not a standard action' {param($a) (Row $a 'C6B-CHARGE-positive').before.identity.kmcChargeActionType='Free'}
Mutate 'a charge carrying the stock component' {param($a) (Row $a 'C6B-CHARGE-positive').before.identity.kmcChargeIsStockLogic=$true}
Mutate 'a charge with another component' {param($a) (Row $a 'C6B-CHARGE-positive').before.identity.kmcChargeComponent='Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge'}
Mutate 'a charge ability leased while the setting is off' {param($a) (Row $a 'C6B-CHARGE-default-off').settingOff.kmcChargePresent=$true}
Mutate 'a charge ability absent once the setting is on' {param($a) (Row $a 'C6B-CHARGE-default-off').settingOn.kmcChargePresent=$false}
Mutate 'a default-off row that started with the setting on' {param($a) (Row $a 'C6B-CHARGE-default-off').settingOff.setting=$true}
Mutate 'an impure hover' {param($a) (Row $a 'C6B-CHARGE-positive').input.hoverPure=$false}
Mutate 'a click that was not admitted' {param($a) (Row $a 'C6B-CHARGE-positive').input.clicked=$false}
Mutate 'two native shells' {param($a) (Row $a 'C6B-CHARGE-positive').input.shellCount=2}
Accept 'stale shared feedback cannot reject an exact accepted native request' {$a=New-Artifact;(Row $a 'C6B-CHARGE-positive').input.rejectionCodes=@('WrongTurn');Assert-KmcChunk6bChargeEvidence $request $a 'PASS'}
Mutate 'an exact refused native charge request' {param($a) (Row $a 'C6B-CHARGE-positive').input.requestWindow.records[0].phase='CastRefused'}
Mutate 'missing native charge request window' {param($a) (Row $a 'C6B-CHARGE-positive').input.requestWindow=$null}
Mutate 'an incomplete native charge request window' {param($a) (Row $a 'C6B-CHARGE-positive').input.requestWindow.beforeSequence=9}
Mutate 'a foreign native charge request actor' {param($a) (Row $a 'C6B-CHARGE-positive').input.requestWindow.records[0].caster='foreign'}
Mutate 'a stale native charge request frame' {param($a) (Row $a 'C6B-CHARGE-positive').input.requestWindow.records[0].frame=109}
Mutate 'another native charge shell target' {param($a) (Row $a 'C6B-CHARGE-positive').input.shell.targetId='foreign'}
Mutate 'a charge with two input activations' {param($a) $w=(Row $a 'C6B-CHARGE-positive').input.requestWindow;$w.afterSequence=12;$w.records+=Copy-Case $w.records[0];$w.records[1].sequence=12;$w.records[1].activationId=8}
Mutate 'a blocker not admitted to the native awake collection' {param($a) (Row $a 'C6B-CHARGE-new-landing-blocker').boundary.awakeBlocker='foreign'}
Mutate 'a blocker observed only before native spawn returned' {param($a) $b=(Row $a 'C6B-CHARGE-new-landing-blocker').boundary;$b.awakeFrame=$b.spawnFrame}
Mutate 'a cleaned charge retaining its native manual attack intent' {param($a) (Row $a 'C6B-CHARGE-relationship-invalidated').boundary.lastDrained.manualTargetOwned=$true}
Mutate 'a view fixture omitting loaded native immunity listeners' {param($a) (Row $a 'C6B-CHARGE-view-replaced').boundary.nativeView.components=@('Kingmaker.UnitLogic.Buffs.Polymorph')}
Mutate 'a view fixture with another size immunity' {param($a) (Row $a 'C6B-CHARGE-view-replaced').boundary.nativeView.sizeImmunities[0]='foreign'}
Mutate 'a view fixture retaining a native listener' {param($a) (Row $a 'C6B-CHARGE-view-replaced').boundary.nativeView.listenersRemaining=1}
Mutate 'a charge the controller never admitted' {param($a) (Row $a 'C6B-CHARGE-positive').delivery.chargeAdmitted=0}
Mutate 'a lawful charge with no post-settlement delivery section' {param($a) (Row $a 'C6B-CHARGE-positive').delivery=$null}
Mutate 'a charge admitted twice' {param($a) (Row $a 'C6B-CHARGE-positive').delivery.chargeAdmitted=2}
Mutate 'a charge the controller refused at delivery' {param($a) (Row $a 'C6B-CHARGE-positive').delivery.chargeRefused=1}
Mutate 'a delivered charge with no rejection-code context' {param($a) (Row $a 'C6B-CHARGE-positive').delivery.PSObject.Properties.Remove('rejectionCodes')}
Mutate 'a delivered charge leaving a refusal reason behind' {param($a) (Row $a 'C6B-CHARGE-positive').delivery.lastRefusal='Mounted Charge requires the rider standard action.'}
Mutate 'a refusal that reached the controller delivery' {param($a) (Row $a 'C6B-CHARGE-below-minimum').delivery.chargeAdmitted=1}
Mutate 'a refusal refused at the controller delivery' {param($a) (Row $a 'C6B-CHARGE-below-minimum').delivery.chargeRefused=1}
Mutate 'a refusal with no post-settlement delivery section' {param($a) (Row $a 'C6B-CHARGE-below-minimum').delivery=$null}
Mutate 'a charge where the mount never moved' {param($a) (Row $a 'C6B-CHARGE-positive').movement.mountDistance=0.2}
Mutate 'a charge at walking speed' {param($a) (Row $a 'C6B-CHARGE-positive').movement.peakSpeedMps=5.1}
Mutate 'a charge where the mount never charged' {param($a) (Row $a 'C6B-CHARGE-positive').movement.chargingObserved=$false}
Mutate 'a charge with no lease' {param($a) (Row $a 'C6B-CHARGE-positive').lease=$null}
Mutate 'a lease that was not restored' {param($a) (Row $a 'C6B-CHARGE-positive').lease.restored=$false}
Mutate 'a lease that did not restore the charging flag' {param($a) (Row $a 'C6B-CHARGE-positive').lease.chargingRestoredExactly=$false}
Mutate 'a lease that did not restore the speed override' {param($a) (Row $a 'C6B-CHARGE-positive').lease.speedOverrideRestoredExactly=$false}
Mutate 'a lease that left the rider charging' {param($a) (Row $a 'C6B-CHARGE-positive').lease.riderChargingRestoredExactly=$false}
Mutate 'a lease that touched the rider agent' {param($a) (Row $a 'C6B-CHARGE-positive').lease.riderAgentTouched=$true}
Mutate 'a lease that forced no path' {param($a) (Row $a 'C6B-CHARGE-positive').lease.forcedPathCount=0}
Mutate 'a lease that did not double the mount speed' {param($a) (Row $a 'C6B-CHARGE-positive').lease.speedOverrideApplied=6.0}
Mutate 'a lease without the charge buff' {param($a) (Row $a 'C6B-CHARGE-positive').lease.buffApplied=$false}
Mutate 'a charge that spent no rider standard action' {param($a) (Row $a 'C6B-CHARGE-positive').economy.riderStandardMax=0}
Mutate 'a mount charged a standard action' {param($a) (Row $a 'C6B-CHARGE-positive').economy.mountStandardMax=6.0}
Mutate 'a mount charged a move action' {param($a) (Row $a 'C6B-CHARGE-positive').economy.mountMoveMax=3.0}
Mutate 'a charge with two deliberate attacks' {param($a) (Row $a 'C6B-CHARGE-positive').rules=(Json (New-Rules 2 0 $true));(Row $a 'C6B-CHARGE-positive').attackRules=2}
Mutate 'a charge with no attack' {param($a) (Row $a 'C6B-CHARGE-positive').rules=(Json (New-Rules 0 0 $true));(Row $a 'C6B-CHARGE-positive').attackRules=0}
Mutate 'an attack without the native charge rule' {param($a) (Row $a 'C6B-CHARGE-positive').rules=(Json (New-Rules 1 0 $false))}
Mutate 'a mount-initiated attack' {param($a) (Row $a 'C6B-CHARGE-positive').rules.mountAttackRules=1}
Mutate 'a charge terminal with two child attacks' {param($a) (Row $a 'C6B-CHARGE-positive').terminal.childAttackStartCount=2}
Mutate 'a charge terminal that was a full attack' {param($a) (Row $a 'C6B-CHARGE-positive').terminal.nativeFullAttack=$true}
Mutate 'charge residue on the mount agent' {param($a) (Row $a 'C6B-CHARGE-positive').after.mountCharging=$true}
Mutate 'charge residue on the rider state' {param($a) (Row $a 'C6B-CHARGE-positive').after.riderStateCharging=$true}
Mutate 'a speed override left behind' {param($a) (Row $a 'C6B-CHARGE-positive').after.mountSpeedOverride=10.16}
Mutate 'an active pair command left behind' {param($a) (Row $a 'C6B-CHARGE-positive').after.pairCommandActive=$true}
Mutate 'a targetable target inside the minimum distance' {param($a) (Row $a 'C6B-CHARGE-below-minimum').before.canTarget=$true}
Mutate 'a minimum-range row whose target was not inside the minimum' {param($a) (Row $a 'C6B-CHARGE-below-minimum').before.state.distanceToTarget=9.0}
Mutate 'a refusal that admitted a shell' {param($a) (Row $a 'C6B-CHARGE-below-minimum').input.shellCount=1}
Mutate 'a refusal that moved the pair' {param($a) (Row $a 'C6B-CHARGE-below-minimum').movement.mountDistance=4.0}
Mutate 'a refusal that spent an action' {param($a) (Row $a 'C6B-CHARGE-below-minimum').economy.riderStandardMax=6.0}
Mutate 'a refusal that attacked' {param($a) (Row $a 'C6B-CHARGE-below-minimum').attackRules=1}
Mutate 'a refusal that applied a lease' {param($a) (Row $a 'C6B-CHARGE-below-minimum').lease=(Json (New-Lease))}
Mutate 'a refusal that left charge state behind' {param($a) (Row $a 'C6B-CHARGE-below-minimum').after.mountCharging=$true}
Mutate 'a repeated charge with an unspent standard action' {param($a) (Row $a 'C6B-CHARGE-spent-standard').before.state.rider.standard=0}
Mutate 'a repeated charge that was available and targetable' {param($a) (Row $a 'C6B-CHARGE-spent-standard').before.available=$true;(Row $a 'C6B-CHARGE-spent-standard').before.canTarget=$true}
Mutate 'a stock rejection row that clicked another ability' {param($a) (Row $a 'C6B-CHARGE-stock-rejected').input.stockBlueprint=$kmc}
Mutate 'an interrupted charge that was never admitted' {param($a) (Row $a 'C6B-CHARGE-interrupted').delivery.chargeAdmitted=0}
Mutate 'an interrupted charge that never carried the pair' {param($a) (Row $a 'C6B-CHARGE-interrupted').movement.mountDistance=0.4}
Mutate 'an interrupted charge at walking speed' {param($a) (Row $a 'C6B-CHARGE-interrupted').movement.peakSpeedMps=5.1}
Mutate 'an interrupted charge that delivered an attack' {param($a) (Row $a 'C6B-CHARGE-interrupted').rules=(Json (New-Rules 1 0 $true)); (Row $a 'C6B-CHARGE-interrupted').attackRules=1}
Mutate 'an interrupted charge whose terminal started a child attack' {param($a) (Row $a 'C6B-CHARGE-interrupted').terminal.childAttackStartCount=1}
Mutate 'an interrupted charge that did not restore its lease' {param($a) (Row $a 'C6B-CHARGE-interrupted').lease.restored=$false}
Mutate 'an interrupted charge that left charge residue' {param($a) (Row $a 'C6B-CHARGE-interrupted').after.mountCharging=$true}
Mutate 'an interrupted charge with no intervention record' {param($a) (Row $a 'C6B-CHARGE-interrupted').intervention=$null}
Mutate 'an interrupted charge whose intervention had no live pair command' {param($a) (Row $a 'C6B-CHARGE-interrupted').intervention.pairCommandActiveBefore=$false}
Mutate 'an interrupted charge that intervened before the pair moved' {param($a) (Row $a 'C6B-CHARGE-interrupted').intervention.mountDistanceAtIntervention=0.2}
Mutate 'an interrupted charge whose shell had not paid' {param($a) (Row $a 'C6B-CHARGE-interrupted').intervention.riderStandardBefore=0.0}
Mutate 'an interrupted charge that refunded the rider standard action' {param($a) (Row $a 'C6B-CHARGE-interrupted').intervention.riderStandardAfter=0.0}
Mutate 'an interrupted charge that left combat instead' {param($a) (Row $a 'C6B-CHARGE-interrupted').intervention.riderInCombatAfter=$false}
Mutate 'a combat-end charge that never left combat' {param($a) (Row $a 'C6B-CHARGE-combat-ended').intervention.riderInCombatAfter=$true}
Mutate 'a combat-end charge with no recorded cooldown after' {param($a) (Row $a 'C6B-CHARGE-combat-ended').intervention.riderStandardAfter='unknown'}
Mutate 'an interrupted charge recorded as a combat end' {param($a) (Row $a 'C6B-CHARGE-interrupted').intervention.kind='native-combat-end'}
Mutate 'a combat-end charge recorded as an interrupt' {param($a) (Row $a 'C6B-CHARGE-combat-ended').intervention.kind='native-command-interrupt'}
Mutate 'a combat-end charge that never started in combat' {param($a) (Row $a 'C6B-CHARGE-combat-ended').intervention.riderInCombatBefore=$false}
Mutate 'a combat-end charge that charged the mount' {param($a) (Row $a 'C6B-CHARGE-combat-ended').economy.mountMoveMax=3.0}
Mutate 'a stock Charge that stayed available and unrefused' {param($a) (Row $a 'C6B-CHARGE-stock-rejected').input.stockAvailable=$true;(Row $a 'C6B-CHARGE-stock-rejected').input.feedback='something else'}

# A FAIL artifact may carry a failed row and need not be complete.
$failed=New-Artifact; $failed.status='FAIL'; $failed.rows=@($failed.rows[0]); $failed.rows[0].status='FAIL'; $failed.subscenarioPassCount=0; $failed.subscenarioFailCount=1
Accept 'a failed artifact retained without a verdict' { Assert-KmcChunk6bChargeEvidence $request $failed 'FAIL' }

# Turn-based mode: the delivery and refusal core only, on the rider own unmoved turn.
# The obstructed charge line: refused by the line alone, with neither the landing point nor the minimum
# charge distance able to explain it.
Accept 'the obstructed-line row recorded as an area limitation' { Assert-KmcChunk6bChargeEvidence $request (New-ObstructedLimitationArtifact) 'PASS' }
function MutateLimitation([string]$Name,[scriptblock]$Change){ $a=New-ObstructedLimitationArtifact; & $Change $a; Reject $Name { Assert-KmcChunk6bChargeEvidence $request $a 'PASS' } }
MutateLimitation 'an obstructed-line limitation with an unknown name' {param($a) (Row $a 'C6B-CHARGE-obstructed-line').limitation='something-else'}
MutateLimitation 'an obstructed-line limitation claiming the geometry was reachable' {param($a) (Row $a 'C6B-CHARGE-obstructed-line').obstructedLineReachable=$true}
MutateLimitation 'an obstructed-line limitation with no placement sweep' {param($a) (Row $a 'C6B-CHARGE-obstructed-line').placement.attempts=@()}
MutateLimitation 'an obstructed-line limitation that passed over an obstructed candidate' {param($a) (Row $a 'C6B-CHARGE-obstructed-line').placement.attempts[0].straightRoute=$false}
Mutate 'an obstructed-line row whose line was straight after all' {param($a) (Row $a 'C6B-CHARGE-obstructed-line').before.geometry.straightRoute=$true}
Mutate 'an obstructed-line row with no recorded geometry' {param($a) (Row $a 'C6B-CHARGE-obstructed-line').before.PSObject.Properties.Remove('geometry')}
Mutate 'an obstructed-line row whose landing was blocked as well' {param($a) (Row $a 'C6B-CHARGE-obstructed-line').before.geometry.landingBlocked=$true}
Mutate 'an obstructed-line row inside the minimum charge distance' {param($a) (Row $a 'C6B-CHARGE-obstructed-line').before.state.distanceToTarget=3.0}
Mutate 'a targetable target whose charge line is obstructed' {param($a) (Row $a 'C6B-CHARGE-obstructed-line').before.canTarget=$true}
Mutate 'a lawful charge whose line was not a straight native route' {param($a) (Row $a 'C6B-CHARGE-positive').before.geometry.straightRoute=$false}

# Cancellation before commitment: on offer, selected, released, and nothing taken.
Mutate 'a cancelled charge that was never on offer' {param($a) (Row $a 'C6B-CHARGE-cancelled').before.available=$false}
Mutate 'a cancelled charge whose target was never targetable' {param($a) (Row $a 'C6B-CHARGE-cancelled').before.canTarget=$false}
Mutate 'a cancelled charge over an obstructed line' {param($a) (Row $a 'C6B-CHARGE-cancelled').before.geometry.straightRoute=$false}
Mutate 'a cancelled charge that was never selected' {param($a) (Row $a 'C6B-CHARGE-cancelled').input.selectedAfterSet=$false}
Mutate 'a cancelled charge still selected after the cancel' {param($a) (Row $a 'C6B-CHARGE-cancelled').input.selectedAfterCancel=$true}
Mutate 'a cancelled charge that was clicked after all' {param($a) (Row $a 'C6B-CHARGE-cancelled').input.clicked=$true}
Mutate 'a cancelled charge whose hover changed live state' {param($a) (Row $a 'C6B-CHARGE-cancelled').input.hoverPure=$false}
Mutate 'a cancelled charge that moved the pair' {param($a) (Row $a 'C6B-CHARGE-cancelled').movement.mountDistance=4.0}
Mutate 'a cancelled charge that spent an action' {param($a) (Row $a 'C6B-CHARGE-cancelled').economy.riderStandardMax=6.0}
Mutate 'a cancelled charge that admitted a shell' {param($a) (Row $a 'C6B-CHARGE-cancelled').input.shellCount=1}

# ---------------------------------------------------------------------------------------------------
# The charge transaction: the order is load-bearing, and these are the orderings the repair batch of
# 2026-10-05 exists for. The first three use RejectWith, because a mutation that tripped some earlier
# check would prove nothing about the ordering it claims to test.
MutateWith 'a charge that released its carrier before the transition revalidation' 'released the carrier before the transition revalidation' {param($a)
 (Row $a 'C6B-CHARGE-positive').transaction.sequence='lawful=True;carrierOwned=False;steps=CarrierAdmitted|CarrierOwnershipProven|InitialRevalidation|LeaseApplied|CarrierReleasedForAttack|TransitionRevalidation|CarrierReleaseProven|Arrived|AttackStartRevalidation|AttackStarted;violations='}
MutateWith 'a charge that revalidated the attack start before proving its carrier release' 'revalidated the attack start before proving the carrier release' {param($a)
 (Row $a 'C6B-CHARGE-positive').transaction.sequence='lawful=True;carrierOwned=False;steps=CarrierAdmitted|CarrierOwnershipProven|InitialRevalidation|LeaseApplied|TransitionRevalidation|CarrierReleasedForAttack|Arrived|AttackStartRevalidation|CarrierReleaseProven|AttackStarted;violations='}
MutateWith 'a charge that applied its lease before proving exact carrier ownership' 'applied the charge lease before proving exact carrier ownership' {param($a)
 (Row $a 'C6B-CHARGE-positive').transaction.sequence='lawful=True;carrierOwned=False;steps=CarrierAdmitted|InitialRevalidation|LeaseApplied|CarrierOwnershipProven|TransitionRevalidation|CarrierReleasedForAttack|CarrierReleaseProven|Arrived|AttackStartRevalidation|AttackStarted;violations='}
Mutate 'a charge with no transaction record' {param($a) (Row $a 'C6B-CHARGE-positive').transaction=$null}
Mutate 'a charge whose own step order was unlawful' {param($a) (Row $a 'C6B-CHARGE-positive').transaction.sequenceLawful=$false}
Mutate 'a charge with no recorded step order' {param($a) (Row $a 'C6B-CHARGE-positive').transaction.sequence=''}
Mutate 'a charge whose step order is missing the transition revalidation' {param($a) (Row $a 'C6B-CHARGE-positive').transaction.sequence='lawful=True;carrierOwned=False;steps=CarrierAdmitted|CarrierOwnershipProven|InitialRevalidation|LeaseApplied|CarrierReleasedForAttack|CarrierReleaseProven|Arrived|AttackStartRevalidation|AttackStarted;violations='}
Mutate 'a charge whose command was not in charge mode' {param($a) (Row $a 'C6B-CHARGE-positive').transaction.chargeMode=$false}
Mutate 'a transaction whose lease was never restored' {param($a) (Row $a 'C6B-CHARGE-positive').transaction.leaseRestored=$false}
Mutate 'a transaction with incomplete lease cleanup' {param($a) (Row $a 'C6B-CHARGE-positive').transaction.cleanupComplete=$false}
Mutate 'a transaction that left cleanup debt' {param($a) (Row $a 'C6B-CHARGE-positive').transaction.cleanupDebt='charge-buff'}
Mutate 'a transaction that left cleanup debt at the end' {param($a) (Row $a 'C6B-CHARGE-positive').transaction.cleanupDebtAtEnd='forced-path'}
Mutate 'a transaction whose lease application failed' {param($a) (Row $a 'C6B-CHARGE-positive').transaction.leaseApplicationFailed=$true}
Mutate 'a transaction that never revalidated its conditions' {param($a) (Row $a 'C6B-CHARGE-positive').transaction.revalidationCount=0}
Mutate 'a transaction with no recorded revalidation phases' {param($a) (Row $a 'C6B-CHARGE-positive').transaction.revalidationPhases=''}
Mutate 'a charge that struck without proving its carrier release' {param($a) (Row $a 'C6B-CHARGE-positive').transaction.carrierReleaseProven=$false}
Mutate 'a charge that struck after a failed revalidation' {param($a) (Row $a 'C6B-CHARGE-positive').transaction.revalidationFailed=$true}
Mutate 'a charge that struck with too few revalidations' {param($a) (Row $a 'C6B-CHARGE-positive').transaction.revalidationCount=2}
Mutate 'a terminated charge that started a native attack anyway' {param($a) (Row $a 'C6B-CHARGE-interrupted').transaction.sequence='lawful=True;carrierOwned=False;steps=CarrierAdmitted|CarrierOwnershipProven|InitialRevalidation|LeaseApplied|AttackStarted;violations='}
Mutate 'an interrupted charge with no transaction record' {param($a) (Row $a 'C6B-CHARGE-interrupted').transaction=$null}

# The clearance gate, which is distinct from the obstructed line.
Mutate 'a clearance row whose landing point was clear after all' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').before.geometry.landingBlocked=$false}
Mutate 'a clearance row whose line was obstructed as well' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').before.geometry.straightRoute=$false}
Mutate 'a clearance row inside the minimum charge distance' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').before.state.distanceToTarget=3.0}
Mutate 'a targetable target whose charge landing point is occupied' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').before.canTarget=$true}
Mutate 'a clearance row that named no blocking actor' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').landingBlockers=@()}
Mutate 'a clearance blocker standing outside the clearance threshold' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').landingBlockers[0].distanceToLanding=2.5}
Mutate 'a clearance blocker with no identity' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').landingBlockers[0].actorId=''}
Mutate 'a clearance row that moved the pair' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').movement.mountDistance=4.0}
Mutate 'a clearance row that admitted a shell' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').input.shellCount=1}
Accept 'the clearance row recorded as an area limitation' { Assert-KmcChunk6bChargeEvidence $request (New-ClearanceLimitationArtifact) 'PASS' }
function MutateClearanceLimitation([string]$Name,[scriptblock]$Change){ $a=New-ClearanceLimitationArtifact; & $Change $a; Reject $Name { Assert-KmcChunk6bChargeEvidence $request $a 'PASS' } }
MutateClearanceLimitation 'a clearance limitation with an unknown name' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').limitation='something-else'}
MutateClearanceLimitation 'a clearance limitation claiming the geometry was reachable' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').blockedClearanceReachable=$true}
MutateClearanceLimitation 'a clearance limitation with no blocker placement attempt' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').PSObject.Properties.Remove('blocker')}
MutateClearanceLimitation 'a clearance limitation whose blocker was placed after all' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').blocker.placed=$true}
MutateClearanceLimitation 'a clearance limitation with no reason' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').blocker.reason=''}
MutateClearanceLimitation 'a clearance limitation with no landing point it tried' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').blocker.PSObject.Properties.Remove('wantedLanding')}
MutateClearanceLimitation 'the obstructed-line limitation claimed on the clearance row' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').limitation='no-obstructed-line-in-fixture-area'}

# Native child acquisition before parent storage and exact lifetime cleanup.
Mutate 'child cleanup stimulus acquired no children' {param($a) (Row $a 'C6B-CHARGE-child-cleanup').boundary.childStimulus.allAcquired=$false}
Mutate 'child cleanup stimulus names a different rider' {param($a) (Row $a 'C6B-CHARGE-child-cleanup').boundary.childStimulus.rider='other'}
Mutate 'child cleanup stimulus loses the unstored child window' {param($a) (Row $a 'C6B-CHARGE-child-cleanup').boundary.childStimulus.children[2].storedByParent=$true}
Mutate 'child cleanup stimulus names a different native acquisition' {param($a) (Row $a 'C6B-CHARGE-child-cleanup').boundary.childStimulus.children[0].identity=999}
Mutate 'child cleanup retains uncertain FX custody' {param($a) (Row $a 'C6B-CHARGE-child-cleanup').boundary.lastDrained.lease.buffChildren.facts[2].visuals.rootFacts[0].custodyUncertain=$true}
Mutate 'child cleanup exercised no native FX acquisition' {param($a)
 $fx=(Row $a 'C6B-CHARGE-child-cleanup').boundary.lastDrained.lease.buffChildren.facts[2].visuals
 $fx.roots=0;$fx.copies=0;$fx.rootFacts=@();$fx.acquisitions=@()
}

# The post-queue admission fault and its compensation.
Mutate 'an admission-fault row with no fault record' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault=$null}
Mutate 'an admission fault that was never armed' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.armed=$false}
Mutate 'an admission fault that never fired' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.fired=$false}
Mutate 'an admission fault whose at-click record claims it had already fired' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.atClick.seamStillArmed=$false}
Mutate 'an admission fault that compensated twice' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.compensationCount=2}
MutateWith 'an admission compensation that did not complete' 'did not complete' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.compensationComplete=$false}
MutateWith 'an admission compensation with an unmet postcondition' 'unmet postcondition' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.unmetPostconditions='lease-restored-or-absent'}
Mutate 'a compensated charge still resident on the rider' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.commandResident=$true}
Mutate 'a compensated charge that did not restore its lease' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.leaseRestored=$false}
Mutate 'a compensated charge whose controller kept its reference' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.activeCommandCleared=$false}
MutateWith 'a compensated charge with a retained faulted cleanup owner' 'faulted charge cleanup owner' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.faultedCleanupOwner=$true}
Mutate 'a compensated charge that left a rider command behind' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.riderCommandsEmpty=$false}
Mutate 'a compensated charge that left a mount command behind' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.mountCommandsEmpty=$false}
Mutate 'a faulted admission that admitted a charge after all' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').delivery.chargeAdmitted=1}
Mutate 'a faulted admission that recorded no refusal' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').delivery.chargeRefused=0}
Mutate 'a faulted admission that delivered an attack' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').attackRules=1}
Mutate 'a faulted admission that published a lease' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').lease=(Json (New-Lease))}
Mutate 'a faulted admission that published a transaction' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').transaction=(Json (New-Transaction $false))}
Mutate 'a faulted admission that carried the pair' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').movement.mountDistance=4.0}
Mutate 'a faulted admission that charged the mount' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').economy.mountMoveMax=3.0}
Mutate 'a faulted admission that left charge residue' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').after.mountCharging=$true}
Mutate 'a faulted admission that left a speed override behind' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').after.mountSpeedOverride=10.16}
Mutate 'an admission-fault charge that was never on offer' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').before.available=$false}
Mutate 'an admission-fault charge over an obstructed line' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').before.geometry.straightRoute=$false}

# The moving target and its revalidation.
Mutate 'a moving-target row with no target move' {param($a) (Row $a 'C6B-CHARGE-target-moved').targetMove=$null}
Mutate 'a moving-target row that never issued its move' {param($a) (Row $a 'C6B-CHARGE-target-moved').targetMove.issued=$false}
Mutate 'a moving-target row whose target never moved' {param($a) (Row $a 'C6B-CHARGE-target-moved').targetMove.targetMovedDistance=0.2}
Mutate 'a moving-target row naming another contract' {param($a) (Row $a 'C6B-CHARGE-target-moved').targetMove.contract='something-else'}
MutateWith 'a moving-target charge that reached its target without repathing' 'without repathing' {param($a) (Row $a 'C6B-CHARGE-target-moved').terminal.repathCount=0}
# The third lawful outcome preview.174 measured: the charge reached, started its one native attack, and
# the engine interrupted the strike before it resolved.
$unresolved=New-Artifact
(Row $unresolved 'C6B-CHARGE-target-moved').rules=(Json (New-Rules 0 0 $true))
(Row $unresolved 'C6B-CHARGE-target-moved').attackRules=0
(Row $unresolved 'C6B-CHARGE-target-moved').terminal.nativeCompletedAttackCount=0
(Row $unresolved 'C6B-CHARGE-target-moved').terminal.result='Interrupt'
Accept 'a moving-target charge whose started attack the engine interrupted' { Assert-KmcChunk6bChargeEvidence $request $unresolved 'PASS' }
function MutateUnresolvedMoved([string]$Name,[string]$Expected,[scriptblock]$Change){
 $a=New-Artifact
 (Row $a 'C6B-CHARGE-target-moved').rules=(Json (New-Rules 0 0 $true))
 (Row $a 'C6B-CHARGE-target-moved').attackRules=0
 (Row $a 'C6B-CHARGE-target-moved').terminal.nativeCompletedAttackCount=0
 (Row $a 'C6B-CHARGE-target-moved').terminal.result='Interrupt'
 & $Change $a; RejectWith $Name $Expected { Assert-KmcChunk6bChargeEvidence $request $a 'PASS' }
}
MutateUnresolvedMoved 'an unresolved moving-target attack that produced a deliberate rule' 'still produced a deliberate attack rule' {param($a) (Row $a 'C6B-CHARGE-target-moved').rules=(Json (New-Rules 1 0 $true))}
MutateUnresolvedMoved 'an unresolved moving-target attack that did not end interrupted' 'did not end interrupted' {param($a) (Row $a 'C6B-CHARGE-target-moved').terminal.result='Success'}
MutateWith 'a moving-target charge that started two native attacks' 'started more than one native attack' {param($a) (Row $a 'C6B-CHARGE-target-moved').terminal.childAttackStartCount=2}
MutateWith 'a resolved moving-target charge with no deliberate attack rule' 'exactly one deliberate attack rule' {param($a)
 $r=(Json (New-Rules 1 0 $true));$r.pairNonOpportunityAttackRules=0;(Row $a 'C6B-CHARGE-target-moved').rules=$r}
Mutate 'a moving-target charge with two deliberate attacks' {param($a) (Row $a 'C6B-CHARGE-target-moved').rules=(Json (New-Rules 2 0 $true))}
Mutate 'a moving-target charge whose attack lacked the native charge rule' {param($a) (Row $a 'C6B-CHARGE-target-moved').rules=(Json (New-Rules 1 0 $false))}
MutateWith 'a moving-target charge that neither reached its target nor failed a revalidation' 'neither reached its target nor failed a revalidation' {param($a)
 (Row $a 'C6B-CHARGE-target-moved').rules=(Json (New-Rules 0 0 $true));(Row $a 'C6B-CHARGE-target-moved').attackRules=0
 (Row $a 'C6B-CHARGE-target-moved').terminal.childAttackStartCount=0;(Row $a 'C6B-CHARGE-target-moved').terminal.nativeCompletedAttackCount=0
 (Row $a 'C6B-CHARGE-target-moved').transaction=(Json (New-Transaction $false))}
MutateWith 'a moving-target charge that did not name its revalidation failure' 'did not name its revalidation failure' {param($a)
 (Row $a 'C6B-CHARGE-target-moved').rules=(Json (New-Rules 0 0 $true));(Row $a 'C6B-CHARGE-target-moved').attackRules=0
 (Row $a 'C6B-CHARGE-target-moved').terminal.childAttackStartCount=0;(Row $a 'C6B-CHARGE-target-moved').terminal.nativeCompletedAttackCount=0
 $t=(Json (New-Transaction $false));$t.revalidationFailed=$true;(Row $a 'C6B-CHARGE-target-moved').transaction=$t}
Mutate 'a moving-target charge that charged the mount' {param($a) (Row $a 'C6B-CHARGE-target-moved').economy.mountStandardMax=6.0}
Mutate 'a moving-target charge with no lease' {param($a) (Row $a 'C6B-CHARGE-target-moved').lease=$null}
Mutate 'a moving-target charge that did not restore its lease' {param($a) (Row $a 'C6B-CHARGE-target-moved').lease.restored=$false}
Mutate 'a moving-target charge that left charge residue' {param($a) (Row $a 'C6B-CHARGE-target-moved').after.riderStateCharging=$true}

# The lost target, and the service the withdrawn row disposed.
Mutate 'a target-lost row with no loss record' {param($a) (Row $a 'C6B-CHARGE-target-lost').targetLoss=$null}
Mutate 'a target-lost row naming another contract' {param($a) (Row $a 'C6B-CHARGE-target-lost').targetLoss.contract='something-else'}
Mutate 'a target-lost row that recorded no removed body' {param($a) (Row $a 'C6B-CHARGE-target-lost').targetLoss.lostTargetId=''}
# A mid-charge destroy that removed the body and could not yet verify itself is lawful, because the
# diagnostic leases are still held on purpose; preview.175 measured exactly that.
$deferredDestroy=New-Artifact
(Row $deferredDestroy 'C6B-CHARGE-target-lost').targetLoss.destroyConfirmed=$false
(Row $deferredDestroy 'C6B-CHARGE-target-lost').targetLoss.serviceState='DestroyRequested'
Accept 'a target-lost row whose destroy removed the body and deferred its verification' { Assert-KmcChunk6bChargeEvidence $request $deferredDestroy 'PASS' }
Mutate 'a target-lost row with no destroy result at all' {param($a) (Row $a 'C6B-CHARGE-target-lost').targetLoss.PSObject.Properties.Remove('destroyConfirmed')}
MutateWith 'a target-lost row whose service never entered a destroy state' 'not in a destroy state' {param($a) (Row $a 'C6B-CHARGE-target-lost').targetLoss.serviceState='Active'}
Mutate 'a target-lost row whose body was still in state' {param($a) (Row $a 'C6B-CHARGE-target-lost').targetLoss.targetEntityRemoved=$false}
MutateWith 'a target-lost row that disposed the shared diagnostic target service' 'disposed the shared diagnostic target service' {param($a) (Row $a 'C6B-CHARGE-target-lost').targetLoss.serviceRetained=$false}
MutateWith 'a target-lost row that recorded disposing the service itself' 'disposed the shared diagnostic target service' {param($a) (Row $a 'C6B-CHARGE-target-lost').targetLoss.serviceDisposedByRow=$true}
MutateWith 'a target-lost row whose pair had already left combat' 'had already left combat' {param($a) (Row $a 'C6B-CHARGE-target-lost').targetLoss.riderInCombat=$false}
Mutate 'a target-lost row whose mount had already left combat' {param($a) (Row $a 'C6B-CHARGE-target-lost').targetLoss.mountInCombat=$false}
Mutate 'a target-lost row that left combat instead' {param($a) (Row $a 'C6B-CHARGE-target-lost').intervention.riderInCombatAfter=$false}
Mutate 'a target-lost row that delivered an attack' {param($a) (Row $a 'C6B-CHARGE-target-lost').rules=(Json (New-Rules 1 0 $true));(Row $a 'C6B-CHARGE-target-lost').attackRules=1}

# The two incapacity rows: one real native damage rule, an exact window, and the damage returned.
foreach($pair in @(
  @{row='C6B-CHARGE-rider-incapacitated';kind='rider';other='mount'},
  @{row='C6B-CHARGE-mount-incapacitated';kind='mount';other='rider'})){
 $row=[string]$pair.row;$kind=[string]$pair.kind;$other=[string]$pair.other
 Mutate ($kind+' incapacity with no incapacity record') {param($a) (Row $a $row).incapacity=$null}
 Mutate ($kind+' incapacity naming another contract') {param($a) (Row $a $row).incapacity.contract='something-else'}
 MutateWith ($kind+' incapacity claimed without a window') 'without a window' {param($a) (Row $a $row).incapacity.window='absent'}
 MutateWith ($kind+' incapacity of the other actor') 'incapacitated the other actor' {param($a) (Row $a $row).incapacity.subjectKind=$other}
 Mutate ($kind+' incapacity with no subject identity') {param($a) (Row $a $row).incapacity.subjectId=''}
 MutateWith ($kind+' incapacity dispatching two damage rules') 'more than one native damage rule' {param($a) (Row $a $row).incapacity.damageDispatchCount=2}
 MutateWith ($kind+' incapacity that changed the native difficulty') 'native difficulty unchanged' {param($a) (Row $a $row).incapacity.difficultyUnchanged=$false}
 Mutate ($kind+' incapacity with no window measurement') {param($a) (Row $a $row).incapacity.measurement=$null}
 Mutate ($kind+' incapacity whose window field is not a number') {param($a) (Row $a $row).incapacity.measurement.deathThreshold='unknown'}
 MutateWith ($kind+' incapacity whose window was not past the hit points') 'not past the subject hit points' {param($a) (Row $a $row).incapacity.measurement.desiredDamage=20}
 MutateWith ($kind+' incapacity whose window reached the death threshold') 'reached the death threshold' {param($a) (Row $a $row).incapacity.measurement.desiredDamage=44}
 MutateWith ($kind+' incapacity whose damage rule dealt something else') 'did not deal what was requested' {param($a) (Row $a $row).incapacity.nativeDamageBeforeDifficulty=8}
 MutateWith ($kind+' incapacity that never reached the window') 'did not reach the incapacity window' {param($a) (Row $a $row).incapacity.damageAfter=10}
 MutateWith ($kind+' incapacity that reached the death threshold') 'reached the death threshold' {param($a) (Row $a $row).incapacity.damageAfter=50}
 MutateWith ($kind+' incapacity whose damage never passed the hit points') 'did not pass the subject hit points' {param($a) (Row $a $row).incapacity.stateAfterDamage.$kind.damage=10}
 MutateWith ($kind+' incapacity whose subject damage is not a number') 'damage against its hit points' {param($a) (Row $a $row).incapacity.stateAfterDamage.$kind.damage='lots'}
 MutateWith ($kind+' incapacity that killed its subject') 'killed rather than incapacitated' {param($a) (Row $a $row).incapacity.stateAfterDamage.$kind.dead=$true}
 MutateWith ($kind+' incapacity whose pair was already dissolved when the damage landed') 'no longer the mounted pair when the damage landed' {param($a) (Row $a $row).incapacity.stateAfterDamage.relationship='Unmounted'}
 MutateWith ($kind+' incapacity that affected the other actor too') 'the other actor of the pair was affected' {param($a) (Row $a $row).incapacity.stateAfterDamage.$other.conscious=$false}
 MutateWith ($kind+' incapacity whose subject stayed conscious') 'never unconscious' {param($a) (Row $a $row).incapacity.restore.stateBeforeRestore.$kind.conscious=$true}
 MutateWith ($kind+' incapacity whose subject life state never changed') 'never unconscious' {param($a) (Row $a $row).incapacity.restore.stateBeforeRestore.$kind.lifeState='Conscious'}
 MutateWith ($kind+' incapacity that killed its subject before the restoration') 'killed rather than incapacitated' {param($a) (Row $a $row).incapacity.restore.stateBeforeRestore.$kind.finallyDead=$true}
 MutateWith ($kind+' incapacity whose subject damage drifted before the restoration') 'damage changed between the rule and the restoration' {param($a) (Row $a $row).incapacity.restore.stateBeforeRestore.$kind.damage=20}
 MutateWith ($kind+' incapacity that affected the other actor by the restoration') 'the other actor of the pair was affected' {param($a) (Row $a $row).incapacity.restore.stateBeforeRestore.$other.dead=$true}
 MutateWith ($kind+' incapacity that held the pair while its subject was unconscious') 'still held the mounted pair' {param($a) (Row $a $row).incapacity.restore.stateBeforeRestore.relationship='Mounted'}
 MutateWith ($kind+' incapacity with no state before its restoration') 'no pair state before its restoration' {param($a) (Row $a $row).incapacity.restore.stateBeforeRestore=$null}
 MutateWith ($kind+' incapacity with no subject in the state before its restoration') 'no pair state before its restoration' {param($a) (Row $a $row).incapacity.restore.stateBeforeRestore.PSObject.Properties.Remove($kind)}
 MutateWith ($kind+' incapacity with no native life-state boundary') 'exactly one native life-state boundary' {param($a) (Row $a $row).incapacity.lifeEventsAtSettle.events=@()}
 MutateWith ($kind+' incapacity whose boundary was not Conscious to Unconscious') 'not Conscious to Unconscious' {param($a) (Row $a $row).incapacity.lifeEventsAtSettle.events[0].detail='Unconscious'}
 MutateWith ($kind+' incapacity whose boundary came from elsewhere') 'native life controller' {param($a) (Row $a $row).incapacity.lifeEventsAtSettle.events[0].nativeSource=@()}
 MutateWith ($kind+' incapacity with no restoration') 'no restoration of the damage it dealt' {param($a) (Row $a $row).incapacity.restore=$null}
 MutateWith ($kind+' incapacity that did not restore the damage it dealt') 'did not restore the damage it dealt' {param($a) (Row $a $row).incapacity.restore.restored=$false}
 MutateWith ($kind+' incapacity that restored another damage value') 'restored another damage value' {param($a) (Row $a $row).incapacity.restore.damageAfterRestore=7}
 MutateWith ($kind+' incapacity that left its subject unconscious') 'left its subject unconscious' {param($a) (Row $a $row).incapacity.restore.conscious=$false}
 Accept ($kind+' incapacity recorded as an absent window') { Assert-KmcChunk6bChargeEvidence $request (New-IncapacityLimitationArtifact $row $kind) 'PASS' }
 $limitation=New-IncapacityLimitationArtifact $row $kind
 (Row $limitation $row).incapacity.window='present'
 RejectWith ($kind+' incapacity limitation whose window was present after all') 'does not record the window as absent' { Assert-KmcChunk6bChargeEvidence $request $limitation 'PASS' }
 $noReason=New-IncapacityLimitationArtifact $row $kind
 (Row $noReason $row).incapacity.windowReason=''
 RejectWith ($kind+' incapacity limitation with no reason') 'names no reason' { Assert-KmcChunk6bChargeEvidence $request $noReason 'PASS' }
 $dealt=New-IncapacityLimitationArtifact $row $kind
 (Row $dealt $row).incapacity|Add-Member -NotePropertyName nativeDamage -NotePropertyValue 32
 RejectWith ($kind+' incapacity limitation that dealt damage anyway') 'dealt damage after all' { Assert-KmcChunk6bChargeEvidence $request $dealt 'PASS' }
}

# The defects preview.173 measured, each refused by its own mutation.
MutateWith 'a lease that still owns the native charge buff' 'still owns the native charge buff' {param($a) (Row $a 'C6B-CHARGE-positive').lease.buffOutstanding=$true}
Mutate 'a terminated lease that still owns the native charge buff' {param($a) (Row $a 'C6B-CHARGE-interrupted').lease.buffOutstanding=$true}
Mutate 'a moving-target lease that never applied the native charge buff' {param($a) (Row $a 'C6B-CHARGE-target-moved').lease.buffApplied=$false}
Mutate 'a moving-target lease that still owns the native charge buff' {param($a) (Row $a 'C6B-CHARGE-target-moved').lease.buffOutstanding=$true}
MutateWith 'a row that records its own non-delivery' 'did not deliver' {param($a) (Row $a 'C6B-CHARGE-positive').nonDelivery='the charge was not admitted: shellStarted=False'}
MutateWith 'an incapacity row that did not start from the mounted pair' 'did not start from the mounted pair' {param($a) (Row $a 'C6B-CHARGE-rider-incapacitated').before.state.relationship='Unmounted'}
MutateWith 'an incapacity row that left the pair mounted' 'left the pair mounted' {param($a) (Row $a 'C6B-CHARGE-mount-incapacitated').after.relationship='Mounted'}
MutateWith 'an incapacity row that kept the mounted-only charge ability' 'presence differs from the setting' {param($a) (Row $a 'C6B-CHARGE-rider-incapacitated').identityAfter=(Json (New-Identity $true $true))}
MutateWith 'an incapacity row that left the mount charging' 'left residue: mountCharging' {param($a) (Row $a 'C6B-CHARGE-rider-incapacitated').after.mountCharging=$true}
MutateWith 'an incapacity row that left a speed override behind' 'left a mount speed override behind' {param($a) (Row $a 'C6B-CHARGE-mount-incapacitated').after.mountSpeedOverride=10.16}
MutateWith 'an ordinary terminated row whose mount was still moving' 'left residue: mountMoving' {param($a) (Row $a 'C6B-CHARGE-interrupted').after.mountMoving=$true}
$movingIncapacity=New-Artifact
(Row $movingIncapacity 'C6B-CHARGE-rider-incapacitated').after.mountMoving=$true
Accept 'an incapacity row whose mount was still moving under the engine' { Assert-KmcChunk6bChargeEvidence $request $movingIncapacity 'PASS' }
MutateWith 'an ordinary row that lost the charge ability' 'presence differs from the setting' {param($a) (Row $a 'C6B-CHARGE-interrupted').identityAfter=(Json (New-Identity $false $true))}
MutateWith 'an ordinary row that was not the mounted pair' 'was not the mounted pair' {param($a) (Row $a 'C6B-CHARGE-interrupted').mounted=$false}
MutateWith 'a row whose stranded shell the fixture had to interrupt' 'had to interrupt itself' {param($a) (Row $a 'C6B-CHARGE-target-moved').strandedShellInterrupted=$true}
MutateWith 'a row that needed a re-click' 're-click' {param($a) (Row $a 'C6B-CHARGE-target-moved').clickRetries=1}
MutateWith 'an admission seam still armed after the attempt settled' 'stayed armed after the attempt settled' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.seamClearedAfterFire=$false}
Mutate 'an admission-fault row with no state at its click' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.atClick=$null}
MutateWith 'an admission seam already gone at the click' 'already gone at the click' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.atClick.seamStillArmed=$false}
Mutate 'a clearance row with no placed blocker' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').landingBlocker=$null}
MutateWith 'a clearance row claiming a blocked landing without placing its blocker' 'without having placed its blocker' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').landingBlocker.placed=$false}
Mutate 'a clearance blocker naming another contract' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').landingBlocker.contract='something-else'}
Mutate 'a placed clearance blocker with no identity' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').landingBlocker.blockerId=''}
Mutate 'a clearance blocker with no weapon-reach separation' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').landingBlocker.separation=0}
MutateWith 'a placed clearance blocker the gate never counted' 'not among the actors the clearance gate counted' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').landingBlocker.blockerId='someone-else'}

foreach($kind in @('mount','rider')){
 $deathRow='C6B-CHARGE-'+$kind+'-dead'
 Mutate ($kind+' death without exact damage') {param($a) (Row $a $deathRow).death.damageDispatches=2}
 Mutate ($kind+' death without native life attribution') {param($a) (Row $a $deathRow).death.lifeEvents.events[0].nativeSource=@()}
 Mutate ($kind+' death stranded charge owner') {param($a) (Row $a $deathRow).death.terminated.ownership.owned=$true}
 Mutate ($kind+' death replayed preparation') {param($a) (Row $a $deathRow).death.afterPolicyRestore.riderGrants=1}
 Mutate ($kind+' death unrestored policy') {param($a) (Row $a $deathRow).death.policy.restoration.restored=$false}
 Mutate ($kind+' death changed survivor') {param($a) $other=if($kind-ceq'rider'){'mount'}else{'rider'};(Row $a $deathRow).death.afterPolicyRestore.$other.damage=1}
}
Mutate 'mount recovery from wrong native source' {param($a) (Row $a 'C6B-CHARGE-mount-dead').death.lifeEvents.events[1].nativeSource=@()}
Mutate 'mount resurrected before encounter completion' {param($a) (Row $a 'C6B-CHARGE-mount-dead').death.lifeEvents.events[1].frame=130}
Mutate 'permanent rider death resurrected' {param($a) (Row $a 'C6B-CHARGE-rider-dead').death.afterPolicyRestore.rider.conscious=$true}
Mutate 'death disappeared into a new owner' {param($a) (Row $a 'C6B-CHARGE-mount-dead').death.terminated.lastDrained.identity=18}
Mutate 'final death cleanup excluded another actor' {param($a) $a.observations.cleanup.nativeFinalDeathSelectionExclusion='foreign'}
Mutate 'view fixture used another authored buff' {param($a) (Row $a 'C6B-CHARGE-view-replaced').boundary.nativeView.blueprint='foreign'}
Mutate 'view replacement was a fabricated lifecycle event' {param($a) (Row $a 'C6B-CHARGE-view-replaced').boundary.nativeView.attachments[0].nativeSource=@()}
Mutate 'view restoration did not replace the view' {param($a) (Row $a 'C6B-CHARGE-view-replaced').boundary.nativeView.afterRestore.view=11}
Mutate 'view restoration left an obsolete view alive' {param($a) (Row $a 'C6B-CHARGE-view-replaced').boundary.nativeView.originalRetired=$false}
Mutate 'view restoration left a native listener' {param($a) (Row $a 'C6B-CHARGE-view-replaced').boundary.nativeView.listenersRemaining=1}
Mutate 'view restoration left a source bone' {param($a) (Row $a 'C6B-CHARGE-view-replaced').boundary.nativeView.afterRestore.effects.sourceBones=@('Locator_HeadCenterFX_00')}
Mutate 'view restoration changed an equipped item' {param($a) (Row $a 'C6B-CHARGE-view-replaced').boundary.nativeView.afterRestore.effects.equipment[0].primary.identity=2}
Mutate 'view restoration retained the KMC anchor' {param($a) (Row $a 'C6B-CHARGE-view-replaced').boundary.presentationResidue=$true}
Mutate 'Dismount with no exact native cost proof' {param($a) (Row $a 'C6B-CHARGE-dismounted').boundary.dismountProof=$null}
Mutate 'Dismount proof naming another action' {param($a) (Row $a 'C6B-CHARGE-dismounted').boundary.dismountProof.identity.abilityGuid='foreign'}
Mutate 'Dismount native Move billed twice' {param($a) (Row $a 'C6B-CHARGE-dismounted').boundary.dismountProof.resourceWindow.events[1].state.move=5.75}
Mutate 'Dismount refunded the charge Standard' {param($a) (Row $a 'C6B-CHARGE-dismounted').boundary.dismountProof.resourceWindow.events[1].state.standard=0}
Mutate 'Dismount charged movement before the separate action' {param($a) (Row $a 'C6B-CHARGE-dismounted').boundary.beforeRequest.rider.move=3}
Mutate 'final death cleanup lost its witness' {param($a) $a.observations.cleanup.nativeFinalDeathSelectionExclusion=$null}
Mutate 'final death cleanup selection differs' {param($a) $a.observations.cleanup.actualSelection=@('rider')}
Accept 'final death native surviving main selection fallback' {$a=New-Artifact;$a.observations.initialSelection=@('rider');Assert-KmcChunk6bChargeEvidence $request $a 'PASS'}
Accept 'charge cleanup timeout remains a truthful failure artifact' {$a=New-Artifact;$a.rows=@();$a.rows+=@{name='phase3d-horse-tranche-cleanup-deadline';status='FAIL';evidence=@{}};$a.subscenarioPassCount=0;$a.subscenarioFailCount=1;Assert-KmcChunk6bChargeEvidence $request $a 'FAIL'}

$tbRequest=[pscustomobject]@{scenario='chunk6b-charge-tb'}
function MutateTb([string]$Name,[scriptblock]$Change){ $a=New-Artifact 'TB'; & $Change $a; Reject ('TB: '+$Name) { Assert-KmcChunk6bChargeEvidence $tbRequest $a 'PASS' } }
Accept 'TB turn-based charge refused while increment 6B.3 is deferred' { Assert-KmcChunk6bChargeEvidence $tbRequest (New-Artifact 'TB') 'PASS' }
MutateTb 'a turn-based artifact read as real time' {param($a) $a.observations.chunk6bCharge.mode='RT'}
MutateTb 'a turn-based charge with no recorded turn' {param($a) (Row $a 'C6B-CHARGE-positive').before.state.turn=$null}
MutateTb 'a turn-based charge off the rider own turn' {param($a) (Row $a 'C6B-CHARGE-positive').before.state.turn.isRider=$false}
MutateTb 'a turn-based charge that was still available' {param($a) (Row $a 'C6B-CHARGE-positive').before.available=$true}
MutateTb 'a turn-based charge that was still targetable' {param($a) (Row $a 'C6B-CHARGE-positive').before.canTarget=$true}
MutateTb 'a turn-based refusal with another reason' {param($a) (Row $a 'C6B-CHARGE-positive').before.kmcAvailabilityReason='Mounted Charge requires combat.'}
MutateTb 'a turn-based artifact carrying a real-time only row' {param($a) $a.rows=@($a.rows)+@((Json (New-Row 'C6B-CHARGE-interrupted'))); $a.subscenarioPassCount=$a.rows.Count}
Accept 'TB failed artifact retained without a verdict' { $f=New-Artifact 'TB'; $f.status='FAIL'; $f.rows=@($f.rows[0]); $f.rows[0].status='FAIL'; $f.subscenarioPassCount=0; $f.subscenarioFailCount=1; Assert-KmcChunk6bChargeEvidence $tbRequest $f 'FAIL' }
Mutate 'in-flight native rule dispatch after positive delivery' {param($a) (Row $a 'C6B-CHARGE-positive').lease.buffRuleDispatchSettled=$false}
Mutate 'condition remains after interrupted charge' {param($a) (Row $a 'C6B-CHARGE-interrupted').lease.buffCondition.contributions=1}
Mutate 'lease application rollback omits condition cleanup' {param($a) (Row $a 'C6B-CHARGE-lease-application-failed').lastDrainedOwnership.lease.buffCondition.operations[1].mutationObserved=$false}
Mutate 'old unqualified preview177 schema' {param($a) $a.schemaVersion=34}
Mutate 'old preview181 action-fence schema' {param($a) $a.schemaVersion=37}
Mutate 'replaced process after native action failure' {param($a) (Row $a 'C6B-CHARGE-action-failed-after-rule').lastDrainedOwnership.processes[0].identity=999}
Mutate 'different process context after native action failure' {param($a) (Row $a 'C6B-CHARGE-action-failed-after-rule').lastDrainedOwnership.processes[0].contextIdentity=999}
Mutate 'false terminal process after native action failure' {param($a) (Row $a 'C6B-CHARGE-action-failed-after-rule').lastDrainedOwnership.processes[0].ended=$false}
foreach($faultRow in @('C6B-CHARGE-action-failed-before-rule','C6B-CHARGE-action-failed-after-rule')){
 Mutate ($faultRow+' lost action owner') {param($a) (Row $a $faultRow).nativeActionFault.ownerAtFault.identity=99}
 Mutate ($faultRow+' another target') {param($a) (Row $a $faultRow).nativeActionFault.ownerAtFault.target='foreign'}
 Mutate ($faultRow+' pretended commitment') {param($a) (Row $a $faultRow).lastDrainedOwnership.committed=$true}
 Mutate ($faultRow+' stuck native action fence') {param($a) (Row $a $faultRow).lastDrainedOwnership.nativeActionInProgress=$true}
 Mutate ($faultRow+' observation debt discarded') {param($a) (Row $a $faultRow).lastDrainedOwnership.processObservationPending=$true}
 Mutate ($faultRow+' stale observation error') {param($a) (Row $a $faultRow).lastDrainedOwnership.processObservationError='unresolved'}
 Mutate ($faultRow+' live process discarded') {param($a) (Row $a $faultRow).lastDrainedOwnership.processEnded=$false}
 Mutate ($faultRow+' process count unobserved') {param($a) (Row $a $faultRow).lastDrainedOwnership.processCount='0'}
 Mutate ($faultRow+' replaced shell process') {param($a) (Row $a $faultRow).lastDrainedOwnership.shellProcessAssigned=$true}
 Mutate ($faultRow+' stranded shell') {param($a) (Row $a $faultRow).lastDrainedOwnership.shellContainerReleased=$false}
 Mutate ($faultRow+' fabricated cost') {param($a) (Row $a $faultRow).economy.riderStandardMax=6}
 Mutate ($faultRow+' string boolean') {param($a) (Row $a $faultRow).lastDrainedOwnership.nativeActionInProgress='false'}
}
Accept 'maximum-range refusal records blocked navigation without claiming a traversable charge' {
 $a=New-Artifact
 (Row $a 'C6B-CHARGE-beyond-maximum').before.geometry.straightRoute=$false
 Assert-KmcChunk6bChargeEvidence $request $a 'PASS'
}
Mutate 'maximum-range target rejected for navigation instead of range' {param($a)
 (Row $a 'C6B-CHARGE-beyond-maximum').before.targetCode='NoPath'
 (Row $a 'C6B-CHARGE-beyond-maximum').before.targetReason='The charge line to the target is obstructed.'
}
Mutate 'preview182 missing native charge activation identity' {param($a)
 foreach($record in (Row $a 'C6B-CHARGE-positive').input.requestWindow.records){$record.ability='<none>'}
}
Mutate 'preview183 schema lacks independent fixture return' {param($a)$a.schemaVersion=38}
Mutate 'native row omits its original fixture staging point' {param($a)$a.observations.chunk6bCharge.fixtureOrigin=$null}
Mutate 'native row hides accumulated fixture drift' {param($a)$a.observations.chunk6bCharge.'positioning-C6B-CHARGE-blocked-clearance'.before.mountPosition.x=7.0}
Write-Host ("CHUNK 6B CHARGE READER PASS=$($script:checks) FAIL=0; synthetic acceptance and refusal only, no native qualification")
