Set-StrictMode -Version Latest
# Parser fixture only: a synthetic CM05-forced-detach observation shaped exactly like the
# Chunk4NativeLifeScenario projection. Used by the reader tests; never a substitute for
# native gameplay and never read by the harness.
function New-KmcForcedDetachResource([string]$actor,[int]$object,[double]$standard,[double]$move){
 [ordered]@{actor=$actor;actorObject=$object;grantSequence=2;inCombat=$true;grantSequenceKind='observed-native-Prepare-entries';pairedGrantIdentity='activation';
  standard=$standard;move=$move;swift=0.0;initiativeCooldown=0.0;initiative=12.0;initiativeOrder=12.0;reactionCooldown=0.0;reactions=1;reactionsPerRound=1;prepared=$true;canAct=$true}
}
function New-KmcForcedDetachLife([string]$actor,[string]$lifeState,[bool]$conscious,[bool]$dead,[bool]$finallyDead){
 [ordered]@{actor=$actor;lifeState=$lifeState;conscious=$conscious;dead=$dead;finallyDead=$finallyDead}
}
function New-KmcForcedDetachBoundary([string]$name,[int]$frame,[long]$ticks,[int]$sequence,[string]$state,[bool]$pairCommand,[int]$forced,[long]$lifecycle,$subjectLife,$survivorLife,[string]$subject,$transition,$result){
 [ordered]@{name=$name;frame=$frame;gameTicks=$ticks;allocationSequence=$sequence;traceComplete=$true;turnBased=$true;inCombat=($state-ceq'Mounted');
  relationshipState=$state;generation=4;pairCommand=$pairCommand;riderCommandsEmpty=(-not$pairCommand);mountCommandsEmpty=(-not$pairCommand);shellCount=1;processBindings=1;
  controls=[ordered]@{targetSelectionStart=2;targetSelectionEnd=2;nativeCastRequest=1;nativeRefusal=0;dispatchAccepted=1;dispatchRejected=0;activationCount=6};
  ledger=[ordered]@{admittedMount=1;acceptedMount=1;admittedDismount=0;acceptedDismount=0;refusedVoluntary=0;forcedDetach=$forced;duplicateSuppressed=0;concurrentSuppressed=0};
  lifecycleSequence=$lifecycle;rider=(New-KmcForcedDetachResource 'rider' 11 6.0 0.0);mount=(New-KmcForcedDetachResource 'mount' 12 6.0 0.0);
  subjectLife=$subjectLife;survivorLife=$survivorLife;lastTransition=$transition;lastTransitionResult=$result}
}
function New-KmcForcedDetachDelivery([long]$sequence,[string]$boundary,[string]$trigger,[string]$source,[string]$stateBefore,[string]$stateAfter,[bool]$attempted,[bool]$succeeded){
 [ordered]@{sequence=$sequence;boundary=$boundary;source=$source;detail=$null;stateBefore=$stateBefore;stateAfter=$stateAfter;cleanupTrigger=$trigger;cleanupAttempted=$attempted;cleanupSucceeded=$succeeded;cleanupErrors=@()}
}
function New-KmcForcedDetachEvents([int]$start){
 @([ordered]@{sequence=($start+1);boundary='turn-end-before';frame=11;state=[ordered]@{actor='other1'}},
   [ordered]@{sequence=($start+2);boundary='turn-end-after';frame=11;state=[ordered]@{actor='other1'}})
}
# $Scenario selects the subject, the native life result and the lifecycle delivery that may end the pair.
function New-KmcForcedDetachFixture([string]$Scenario='chunk4-rider-death-tb',[int]$BeforeFrame=10,[int]$BeforeSequence=0,$AllocationEvents=$null){
 $incapacitation=$Scenario-ceq'chunk4-rider-incapacitation-tb'
 $subject=if($Scenario-ceq'chunk4-mount-death-tb'){'mount'}else{'rider'}
 $survivor=if($subject-ceq'rider'){'mount'}else{'rider'}
 $permanent=$Scenario-ceq'chunk4-rider-death-tb'
 $events=if($null-eq$AllocationEvents){@(New-KmcForcedDetachEvents $BeforeSequence)}else{@($AllocationEvents)}
 $afterSequence=$BeforeSequence+$events.Count
 $trigger=if($incapacitation){'Incapacitated'}else{'Death'}
 $boundary=if($incapacitation){'UnitIncapacitated'}else{'UnitDeath'}
 $source=if($incapacitation){'IUnitLifeStateChanged.HandleUnitLifeStateChanged'}else{'IUnitHandler.HandleUnitDeath'}
 $mountTransition=[ordered]@{kind='VoluntaryMount';controlIdentity='shell-1';riderId='rider';mountId='mount';generationBefore=3;settled=$true;accepted=$true;trigger=$null}
 $mountResult=[ordered]@{succeeded=$true;state='Mounted';trigger=$null;errors=@();movementAuthorityResidual=$false;presentationResidual=$false}
 $detachTransition=[ordered]@{kind='ForcedDetach';controlIdentity='cleanup:rider:mount:4';riderId='rider';mountId='mount';generationBefore=4;settled=$true;accepted=$true;trigger=$trigger}
 $detachResult=[ordered]@{succeeded=$true;state='Unmounted';trigger=$trigger;errors=@();movementAuthorityResidual=$false;presentationResidual=$false}
 $aliveSubject=New-KmcForcedDetachLife $subject 'Conscious' $true $false $false
 $aliveSurvivor=New-KmcForcedDetachLife $survivor 'Conscious' $true $false $false
 $endedSubject=if($incapacitation){New-KmcForcedDetachLife $subject 'Unconscious' $false $false $false}else{New-KmcForcedDetachLife $subject 'Dead' $false $true $permanent}
 $before=New-KmcForcedDetachBoundary 'before-damage' $BeforeFrame 10000000 $BeforeSequence 'Mounted' $true 0 3 $aliveSubject $aliveSurvivor $subject $mountTransition $mountResult
 $after=New-KmcForcedDetachBoundary 'after-cleanup' ($BeforeFrame+1) 10200000 $afterSequence 'Unmounted' $false 1 4 $endedSubject (New-KmcForcedDetachLife $survivor 'Conscious' $true $false $false) $subject $detachTransition $detachResult
 [ordered]@{level='NATIVE INTEGRATION';caseId='CM05-forced-detach';contract='forced-detach-records-once-pays-no-voluntary-move';mode='TB';scenario=$Scenario;
  riderId='rider';mountId='mount';subject=$subject;survivor=$survivor;nativeStimulus='labelled-native-damage-effect';before=$before;after=$after;
  allocationEvents=$events;deliveries=@(New-KmcForcedDetachDelivery 4 $boundary $trigger $source 'Mounted' 'Unmounted' $true $true)}
}
