[CmdletBinding()]
param()
# Synthetic acceptance/refusal regression for the Chunk 6B charge-delivery reader
# (scripts/runtime/Chunk6bChargeEvidence.ps1). Never launches the game; no native qualification.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/Chunk6bChargeEvidence.ps1')
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
 [ordered]@{applied=$true;buffApplied=$true;chargingBefore=$false;speedOverrideBefore=$null;speedOverrideApplied=10.16
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
 $state=[ordered]@{}
 $state[$Kind]=[ordered]@{id=$subjectId;conscious=$false;dead=$false;finallyDead=$false;damage=31;hitPoints=30;constitution=14;temporaryHitPoints=0;allowDyingCondition=$true;immortal=$false;essential=$false;mainCharacter=$false;lifeState='Unconscious'}
 $state[$otherKind]=[ordered]@{id=$otherKind;conscious=$true;dead=$false;finallyDead=$false;damage=0;hitPoints=30;constitution=14;temporaryHitPoints=0;allowDyingCondition=$true;immortal=$false;essential=$false;mainCharacter=$false;lifeState='Conscious'}
 $state['relationship']='Mounted'
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
   stateBeforeRestore=[ordered]@{relationship='Mounted'};frame=160;restored=$true;damageAfterRestore=0;conscious=$true
   lifeState='Conscious';stateAfterRestore=[ordered]@{relationship='Mounted'};lifeEvents=[ordered]@{events=$events}}}
}
function New-TerminatedRow([string]$Case,[string]$Kind){
 [ordered]@{name=$Case;status='PASS';evidence=[ordered]@{
  level='NATIVE DELIVERY';mode=$script:fixtureMode;case=$Case;mounted=$true
  before=[ordered]@{identity=(New-Identity $true $true);state=(New-State 'before' 9.0 0 $false)
   available=$true;unavailableReason=$null;canTarget=$true;minRangeMeters=4.65;approachDistance=99.0
   requireFullRound=$true;commandType='Standard';pairCommandState=[ordered]@{frame=100}}
  input=[ordered]@{clicked=$true;hoverPure=$true;frame=110;shell=[ordered]@{present=$true};shellCount=1
   feedback='Mounted charge accepted: the Horse carries the charge.';rejectionCodes=@();chargeAdmitted=0;chargeRefused=0;lastRefusal=$null
   after=(New-State 'input-after' 9.0 0 $false)}
  samples=@()
  after=(New-State 'after' 5.0 4.0 $false)
  identityAfter=(New-Identity $true $true)
  movement=[ordered]@{mountDistance=4.0;riderDistance=4.0;peakSpeedMps=10.3;mountCombatSpeedMps=5.08
   chargingObserved=$true;chargeModeObserved=$true;riderChargeStateObserved=$true}
  economy=[ordered]@{riderStandardMax=6.0;riderMoveMax=0.0;mountStandardMax=0.0;mountMoveMax=0.0
   riderStandardNow=4.0;riderMoveNow=0.0;mountStandardNow=0.0;mountMoveNow=0.0}
  lease=(New-Lease)
  transaction=(New-Transaction $false)
  targetLoss=$(if($Kind-ceq'native-target-removed'){New-TargetLoss}else{$null})
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
  lease=$null;transaction=$null;landingBlockers=$null;targetLoss=$null;targetMove=$null;incapacity=$null
  admissionFault=[ordered]@{contract='post-queue-charge-admission-fault-compensated-exactly'
   armed=$true;fired=$true;seamClearedAfterClick=$true;frame=111;compensationCount=1
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
 if($Case-ceq'C6B-CHARGE-interrupted'){ return New-TerminatedRow $Case 'native-command-interrupt' }
 if($Case-ceq'C6B-CHARGE-combat-ended'){ return New-TerminatedRow $Case 'native-combat-end' }
 if($Case-ceq'C6B-CHARGE-target-lost'){ return New-TerminatedRow $Case 'native-target-removed' }
 if($Case-ceq'C6B-CHARGE-rider-incapacitated'){ return New-TerminatedRow $Case 'native-rider-incapacity' }
 if($Case-ceq'C6B-CHARGE-mount-incapacitated'){ return New-TerminatedRow $Case 'native-mount-incapacity' }
 if($Case-ceq'C6B-CHARGE-exception-cleanup'-and$script:fixtureMode-cne'TB'){ return New-AdmissionFaultRow }
 # Turn-based: every measured row is a refusal while increment 6B.3 is deferred.
 $refusal=$script:fixtureMode-ceq'TB'-or$Case-cin @('C6B-CHARGE-below-minimum','C6B-CHARGE-spent-standard','C6B-CHARGE-stock-rejected','C6B-CHARGE-obstructed-line','C6B-CHARGE-blocked-clearance','C6B-CHARGE-cancelled')
 # The cancellation row is on offer and then released: nothing happens, but the charge was genuinely
 # available and targetable, which is what makes the row mean anything.
 $offered=$Case-ceq'C6B-CHARGE-cancelled'
 if($Case-ceq'C6B-CHARGE-default-off'){
  return [ordered]@{name=$Case;status='PASS';evidence=[ordered]@{level='NATIVE DELIVERY';mode=$script:fixtureMode;case=$Case;mounted=$true
   settingOff=(New-Identity $false $false);settingOn=(New-Identity $true $true);abilityGuid=$kmc}}
 }
 $distance=if($Case-ceq'C6B-CHARGE-below-minimum'){3.5}else{9.0}
 $riderStandard=if($Case-ceq'C6B-CHARGE-spent-standard'){6.0}else{0.0}
 $input=[ordered]@{clicked=(-not$refusal);hoverPure=$true;frame=110
  shell=[ordered]@{present=(-not$refusal)};shellCount=$(if($refusal){0}else{1})
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
   available=$(if(($refusal-and-not$offered)-or$script:fixtureMode-ceq'TB'){$false}else{$true});unavailableReason=$null
   kmcAvailabilityReason=$(if($script:fixtureMode-ceq'TB'){'Mounted Charge is not yet supported in turn-based mode.'}else{'Mounted Charge is available.'})
   canTarget=$(if(($refusal-and-not$offered)-or$script:fixtureMode-ceq'TB'){$false}else{$true});minRangeMeters=4.65;approachDistance=99.0
   geometry=[ordered]@{straightRoute=$(if($Case-ceq'C6B-CHARGE-obstructed-line'){$false}else{$true});landingBlocked=$($Case-ceq'C6B-CHARGE-blocked-clearance');mountDistanceToTarget=$distance}
   requireFullRound=$true;commandType='Standard';pairCommandState=[ordered]@{frame=100}}
  input=$input
  samples=@()
  after=(New-State 'after' $distance $riderStandard $false)
  identityAfter=(New-Identity $true $true)
  movement=[ordered]@{mountDistance=$(if($refusal){0.0}else{6.8});riderDistance=$(if($refusal){0.0}else{6.8})
   peakSpeedMps=$(if($refusal){0.0}else{10.3});mountCombatSpeedMps=5.08;chargingObserved=(-not$refusal)
   chargeModeObserved=(-not$refusal);riderChargeStateObserved=(-not$refusal)}
  economy=[ordered]@{riderStandardMax=$(if($refusal){0.0}else{6.0});riderMoveMax=$(if($refusal){0.0}else{3.0})
   mountStandardMax=0.0;mountMoveMax=0.0;riderStandardNow=$riderStandard;riderMoveNow=0.0;mountStandardNow=0.0;mountMoveNow=0.0}
  lease=$(if($refusal){$null}else{New-Lease})
  transaction=$(if($refusal){$null}else{New-Transaction $true})
  admissionFault=$null;landingBlockers=$null;targetLoss=$null;incapacity=$null
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
  $row.evidence['landingBlockers']=@([ordered]@{actorId='party-cleric';blueprint='bp-cleric';playerFaction=$true
   corpulence=0.5;distanceToLanding=0.42;threshold=0.8;position=@(6,0,0)})
 }
 $row
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
 Copy-Case ([ordered]@{schemaVersion=34;evidenceKind='phase3d-horse-scenario-evidence';scenario='chunk6b-charge-rt';status='PASS'
  rows=$rows
  observations=[ordered]@{chunk6bCharge=[ordered]@{contract='chunk6b-pair-charge-delivery';mode=$Mode;cases=@(Get-KmcChunk6bChargeRows);abilityGuid=$kmc;stockChargeBlueprint=$stock;beyondMaximumReachable=$false;spawnEnvelopeMinimum=3.0;spawnEnvelopeMaximum=20.0;settingBefore=$false;settingAfter=$false;settingRestored=$true}}
  subscenarioPassCount=$rows.Count;subscenarioFailCount=0;errors=@()})
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
  placement=[ordered]@{origin=@(0,0,0);wants='clear-straight-line-blocked-landing';minimumSweptDistance=5.0;maximumSweptDistance=14.0
   attempts=@(
    [ordered]@{point=@(6,0,0);wantedDistance=6.0;mountDistance=6.0;nativeTrace=@(6,0,0);straightRoute=$true;landingBlockerCount=0;riderDistance=6.0;withinFixtureBounds=$true},
    [ordered]@{point=@(0,0,7);wantedDistance=7.0;mountDistance=7.0;nativeTrace=@(0,0,6);straightRoute=$false;landingBlockerCount=2;riderDistance=7.1;withinFixtureBounds=$true})}}))
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
Mutate 'a rejection code on the lawful charge' {param($a) (Row $a 'C6B-CHARGE-positive').input.rejectionCodes=@('WrongTurn')}
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
Mutate 'a clearance blocker standing outside the clearance threshold' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').landingBlockers[0].distanceToLanding=1.2}
Mutate 'a clearance blocker with no identity' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').landingBlockers[0].actorId=''}
Mutate 'a clearance row that moved the pair' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').movement.mountDistance=4.0}
Mutate 'a clearance row that admitted a shell' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').input.shellCount=1}
Accept 'the clearance row recorded as an area limitation' { Assert-KmcChunk6bChargeEvidence $request (New-ClearanceLimitationArtifact) 'PASS' }
function MutateClearanceLimitation([string]$Name,[scriptblock]$Change){ $a=New-ClearanceLimitationArtifact; & $Change $a; Reject $Name { Assert-KmcChunk6bChargeEvidence $request $a 'PASS' } }
MutateClearanceLimitation 'a clearance limitation with an unknown name' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').limitation='something-else'}
MutateClearanceLimitation 'a clearance limitation claiming the geometry was reachable' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').blockedClearanceReachable=$true}
MutateClearanceLimitation 'a clearance limitation with no placement sweep' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').placement.attempts=@()}
MutateClearanceLimitation 'a clearance limitation that passed over a usable blocked landing' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').placement.attempts[0].landingBlockerCount=1}
MutateClearanceLimitation 'the obstructed-line limitation claimed on the clearance row' {param($a) (Row $a 'C6B-CHARGE-blocked-clearance').limitation='no-obstructed-line-in-fixture-area'}

# The post-queue admission fault and its compensation.
Mutate 'an admission-fault row with no fault record' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault=$null}
Mutate 'an admission fault that was never armed' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.armed=$false}
Mutate 'an admission fault that never fired' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.fired=$false}
Mutate 'an admission seam left armed after the click' {param($a) (Row $a 'C6B-CHARGE-exception-cleanup').admissionFault.seamClearedAfterClick=$false}
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
MutateWith 'a moving-target charge that struck without repathing' 'struck without repathing' {param($a) (Row $a 'C6B-CHARGE-target-moved').terminal.repathCount=0}
Mutate 'a moving-target charge with two deliberate attacks' {param($a) (Row $a 'C6B-CHARGE-target-moved').rules=(Json (New-Rules 2 0 $true))}
Mutate 'a moving-target charge whose attack lacked the native charge rule' {param($a) (Row $a 'C6B-CHARGE-target-moved').rules=(Json (New-Rules 1 0 $false))}
MutateWith 'a moving-target charge that neither struck nor failed a revalidation' 'neither struck nor failed a revalidation' {param($a)
 (Row $a 'C6B-CHARGE-target-moved').rules=(Json (New-Rules 0 0 $true));(Row $a 'C6B-CHARGE-target-moved').attackRules=0
 (Row $a 'C6B-CHARGE-target-moved').transaction=(Json (New-Transaction $false))}
MutateWith 'a moving-target charge that did not name its revalidation failure' 'did not name its revalidation failure' {param($a)
 (Row $a 'C6B-CHARGE-target-moved').rules=(Json (New-Rules 0 0 $true));(Row $a 'C6B-CHARGE-target-moved').attackRules=0
 $t=(Json (New-Transaction $false));$t.revalidationFailed=$true;(Row $a 'C6B-CHARGE-target-moved').transaction=$t}
Mutate 'a moving-target charge that charged the mount' {param($a) (Row $a 'C6B-CHARGE-target-moved').economy.mountStandardMax=6.0}
Mutate 'a moving-target charge with no lease' {param($a) (Row $a 'C6B-CHARGE-target-moved').lease=$null}
Mutate 'a moving-target charge that did not restore its lease' {param($a) (Row $a 'C6B-CHARGE-target-moved').lease.restored=$false}
Mutate 'a moving-target charge that left charge residue' {param($a) (Row $a 'C6B-CHARGE-target-moved').after.riderStateCharging=$true}

# The lost target, and the service the withdrawn row disposed.
Mutate 'a target-lost row with no loss record' {param($a) (Row $a 'C6B-CHARGE-target-lost').targetLoss=$null}
Mutate 'a target-lost row naming another contract' {param($a) (Row $a 'C6B-CHARGE-target-lost').targetLoss.contract='something-else'}
Mutate 'a target-lost row that recorded no removed body' {param($a) (Row $a 'C6B-CHARGE-target-lost').targetLoss.lostTargetId=''}
Mutate 'a target-lost row that did not confirm its destroy' {param($a) (Row $a 'C6B-CHARGE-target-lost').targetLoss.destroyConfirmed=$false}
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
 MutateWith ($kind+' incapacity whose subject stayed conscious') 'never unconscious' {param($a) (Row $a $row).incapacity.stateAfterDamage.$kind.conscious=$true}
 MutateWith ($kind+' incapacity that killed its subject') 'killed rather than incapacitated' {param($a) (Row $a $row).incapacity.stateAfterDamage.$kind.dead=$true}
 MutateWith ($kind+' incapacity that affected the other actor too') 'the other actor of the pair was affected' {param($a) (Row $a $row).incapacity.stateAfterDamage.$other.conscious=$false}
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
Write-Host ("CHUNK 6B CHARGE READER PASS=$($script:checks) FAIL=0; synthetic acceptance and refusal only, no native qualification")
