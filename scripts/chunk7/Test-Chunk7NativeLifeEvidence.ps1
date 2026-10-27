$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
. (Join-Path $PSScriptRoot 'Test-Chunk7CombatEvidence.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7NativeLifeEvidence.ps1')
$passed=0;$request=Copy-Value $request;$request.scenario='chunk7-two-pair-native-life-cleanup';$traceEvents=@();$attackOrdinal=0
function LifeActor([string]$Id,[int]$Damage=0,[bool]$Conscious=$true){[pscustomobject]@{actorId=$Id;life=$(if($Conscious){'Conscious'}else{'Unconscious'});conscious=$Conscious;dead=$false;damage=$Damage;hp=30;constitution=12;temporaryHp=0;mainCharacter=$false;essential=$false;immortal=$false;allowDyingCondition=$true}}
function LifeState([int]$Frame,[int]$Round=2,[int]$Damage=0,[bool]$Conscious=$true){
 $w=Snap $Frame;$w.round=$Round;$w.currentActor=$ids[2];$w.actors[0].standard=6.0;$w.actors[1].standard=6.0;$w.actors[3].standard=3.0
 [pscustomobject]@{world=$w;rider=(LifeActor $ids[2] $Damage $Conscious);mount=(LifeActor $ids[3]);healthyRider=(LifeActor $ids[0]);healthyMount=(LifeActor $ids[1]);pairState='Mounted';splitReleaseRound=-1;partnerContextActor=$ids[3];activationIdentity='exact-b:2';activationSplit=$false;presentationResidue=$false;grants=@($ids|ForEach-Object{[pscustomobject]@{actorId=$_;count=2}})}
}
$first=Attack 0 0 50 'life-healthy-rider-before-damage';$affected=Attack 1 1 60 'life-affected-mount-before-damage';$last=Attack 0 0 81 'life-healthy-rider-after-cleanup'
$before=LifeState 66;$after=LifeState 66 2 32;$delivery=LifeState 68 2 32 $false
$delivery.world.owners[1].state='Detached';$delivery.pairState='Unmounted';$delivery.activationSplit=$true;$delivery.splitReleaseRound=2
$renewed=Copy-Value $delivery;$renewed.world.frame=80;$renewed.world.round=3;$renewed.world.currentActor=$ids[0];foreach($i in @(0,1)){$renewed.world.actors[$i].standard=0.0;$renewed.grants[$i].count=3}
$independent=Copy-Value $renewed;$independent.world.frame=90;$independent.world.currentActor=$ids[3];$independent.grants[3].count=3;$independent.world.actors[3].standard=0.0;$independent.world.actors[0].standard=6.0;$independent.world.actors[1].standard=6.0;$independent.partnerContextActor=$null
$endedBefore=LifeState 56;$endedAfter=LifeState 87 3 32 $false
$events=@();$names=@('native-tick-before','native-set-before','production-callback-before','production-callback-after','native-set-after','native-tick-after')
for($i=0;$i-lt6;$i++){$w=Copy-Value $before.world;$w.frame=67;if($i-ge3){$w.owners[1].state='Detached'};$events+=[pscustomobject]@{sequence=$i+1;boundary=$names[$i];actorId=$ids[2];frame=67;gameTicks=6700000;round=2;nativeTickDepth=1;nativeSetDepth=$(if($i-in@(0,5)){0}else{1});nativeSetSequence=$(if($i-eq0){0}else{1});previousLife=$(if($i-in@(2,3)){'Conscious'}else{$null});currentLife=$(if($i-lt2){'Conscious'}else{'Unconscious'});damage=32;world=$w;ledgerSequence=$(if($i-ge3){6}else{5})}}
$probe=[pscustomobject]@{actorId=$ids[2];nativeModuleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';nativeTickToken='06009162';nativeSetToken='06009164';callbackToken='06000400';callbackMvid=$request.dllMvid;callbackType='KingmakerMountedCombat.Integration.MountedLifecycleSubscriber';callbackName='HandleUnitLifeStateChanged';tickDepth=0;setDepth=0;completedNativeTick=$true;events=$events;errors=@()}
$life=[pscustomobject]@{initialRound=1;damageRound=2;subjectId=$ids[2];survivorId=$ids[3];healthyActorIds=@($ids[0],$ids[1]);stimulus='one labelled native RuleDealDamage; stock UnitLifeController tick delivers existing lifecycle subscriber';sameCallRoundBoundaryLifeDeliveryProven=$false;complete=$true;
 healthyAttackBefore=$first;affectedAttackBefore=$affected;healthyAttackAfter=$last;healthyEndedBeforeDamage=$endedBefore;healthyEndedAfterCleanup=$endedAfter;
 beforeDamage=$before;afterDamage=$after;afterDelivery=$delivery;healthyRenewed=$renewed;independentSurvivor=$independent;sourceId=$target;nativeDamageDispatches=1;requestedDamage=32;nativeDamageBeforeDifficulty=32;nativeDamage=32;difficulty=1.0;deathThreshold=42;
 traceSequenceBeforeDamage=8;traceSequenceAfterDelivery=8;ledgerOffset=5;lifeTransitionOffset=2;deliveryProbe=$probe;
 nativeEffects=[pscustomobject]@{dropped=0;events=@([pscustomobject]@{kind='damage-before';identity=999;frame=66;actor=$target;target=$ids[2];targetDamage=0},[pscustomobject]@{kind='damage-after';identity=999;frame=66;actor=$target;target=$ids[2];targetDamage=32;damage=32;beforeDifficulty=32})};
 deliveries=@([pscustomobject]@{sequence=6;boundary='UnitIncapacitated';source='IUnitLifeStateChanged.HandleUnitLifeStateChanged';stateBefore='Mounted';stateAfter='Unmounted';trigger='Incapacitated';attempted=$true;succeeded=$true;errors=@()});lifeTransitions=@([pscustomobject]@{sequence=3;actorId=$ids[2];previousLife='Conscious';currentLife='Unconscious'})}
$v=Copy-Value $good;$v.scenario=$request.scenario;$v.evidenceKind='chunk7-native-life-evidence';$v.observations.scenario=$request.scenario;$v.observations.naturalUnconsciousStimulusProven=$true;$v.observations.inputs=@($v.observations.inputs|Where-Object label -CIN @('mount-a','mount-b'));$v.observations.allocationTrace.events=$traceEvents
$v.observations.nativeRounds.events=@([pscustomobject]@{round=2},[pscustomobject]@{round=3});$v.observations.nativeCompletion.calls=@()
foreach($frame in @(55,86)){foreach($id in @($ids[0],$ids[1])){$v.observations.nativeCompletion.calls+=,[pscustomobject]@{kind='end';complete=$true;before=[pscustomobject]@{actorId=$id;frame=$frame};after=[pscustomobject]@{frame=$frame}}}}
$v.observations|Add-Member NoteProperty nativeLife $life
# Native N+1: the unconscious rider B still receives one native Prepare and an empty turn that Tick
# ends natively (no consciousness filter in ChooseNextUnit/StartTurn/Prepare); derived from the order.
function NextRoundEvent([string]$Boundary,[string]$Actor,[string]$Status,[double]$Standard,$Ledger,$Paired){
 $e=[pscustomobject]@{sequence=@($v.observations.allocationTrace.events).Count+1;boundary=$Boundary;frame=88;round=3;turnStatus=$Status;command=$null;commandActor=$null;commandType=$null;actionType=$null;detail=$null;
  state=[pscustomobject]@{actor=$Actor;standard=$Standard;move=0.0;swift=0.0;reactions=1;reactionCooldown=0.0;pairedGrantIdentity=$Paired;retained=$(if($null-eq$Ledger){$null}else{[pscustomobject]@{GrantIdentity=$Ledger}})}}
 $v.observations.allocationTrace.events+=,$e
}
# Real b194 a1 shapes. Rider B's empty native N+1 turn is seq 921-974 (42 own events): its ledger
# entry still names pair B's previous activation at prepare-before and is native from clear-before
# on; the paired-only completion probe adds completion-end-* around the native End. An unpaired
# companion has no completion probe events (a1 8265 round 2, seq 757-810); unconscious, native Tick
# ends its turn with no end input, so its shape is the rider's without completion-* events.
$realRiderTurn=@(@('prepare-before','None',0.0),@('clear-before','None',0.0),@('clear-after','None',0.0),@('round-state-before','None',0.0),@('round-state-after','None',0.0),
 @('round-handler-before','None',0.0),@('round-handler-after','None',0.0),@('round-handler-before','None',0.0),@('round-handler-after','None',0.0),@('ai-round-before','None',0.0),@('ai-round-after','None',0.0),
 @('native-confusion-before','None',0.0),@('native-confusion-after','None',0.0))+@(1..12|ForEach-Object{,@('ready-handler-before','Preparing',0.0);,@('ready-handler-after','Preparing',0.0)})+
 @(@('prepare-after','Preparing',0.0),@('turn-end-before','Ending',0.0),@('completion-end-before','Ending',0.0),@('turn-end-after','Ended',6.0),@('completion-end-after','Ended',6.0))
if($realRiderTurn.Count-ne42){throw 'Synthetic rider turn no longer mirrors the real a1 shape.'}
$realControlTurn=@($realRiderTurn|Where-Object{-not$_[0].StartsWith('completion-',[StringComparison]::Ordinal)})
function EmptyNativeTurn([string]$Actor,$Ledger,$Rows){$first=$true;foreach($row in $Rows){$id=if($null-ne$Ledger-and$first){'exact-b:2'}else{$Ledger};$first=$false;NextRoundEvent $row[0] $Actor $row[1] $row[2] $id $null}}
$comparedTurn=@($realControlTurn|Where-Object{-not$_[0].StartsWith('ready-handler-',[StringComparison]::Ordinal)}|ForEach-Object{$_[0]})
function EmptyTurnRecord([string]$Actor,$Ledger,[long]$Start){[pscustomobject]@{actorId=$Actor;round=3;prepareBefore=1;prepareAfter=1;ends=1;forcedEnds=0;forbidden=0;
 sequence=$comparedTurn;grantIdentities=@($Ledger);pairedIdentities=0;prepareSequence=$Start;statusAfterPrepare='Preparing';standardBefore=0.0;standardAfterEnd=6.0;conscious=$false;dead=$false}}
$controlId='8265f8cf-0969-4ec7-a266-2448de91c8c4'
$v.observations.allocationTrace|Add-Member NoteProperty forceToEnd @()
NextRoundEvent 'prepare-before' $ids[0] 'None' 0.0 'exact-a:3' 'exact-a:3';NextRoundEvent 'prepare-after' $ids[0] 'Preparing' 0.0 'exact-a:3' 'exact-a:3'
$riderStart=[long](@($v.observations.allocationTrace.events).Count+1);EmptyNativeTurn $ids[2] 'native:rider-b-n1' $realRiderTurn
$controlDamageSequence=[long]@($v.observations.allocationTrace.events).Count
$controlStart=[long](@($v.observations.allocationTrace.events).Count+1);EmptyNativeTurn $controlId $null $realControlTurn
NextRoundEvent 'prepare-before' $ids[3] 'None' 0.0 'native:survivor-n1' $null;NextRoundEvent 'prepare-after' $ids[3] 'Preparing' 0.0 'native:survivor-n1' $null
$life.independentSurvivor.grants[2].count=3
$life|Add-Member NoteProperty nativeNextRoundOrder @($ids[0],$ids[2],$target,$controlId,$ids[3],$ids[1]);$life|Add-Member NoteProperty riderSlotsBeforeRenewal 0;$life|Add-Member NoteProperty expectedRiderSlots 1
$life|Add-Member NoteProperty incapacitatedRiderTurn (EmptyTurnRecord $ids[2] 'native:rider-b-n1' $riderStart)
$life|Add-Member NoteProperty unconsciousControl ([pscustomobject]@{contract='unmounted-unconscious-native-turn-control-v1';survivorSlot=4;available=$true;
 candidates=@([pscustomobject]@{actorId=$controlId;slot=3;mainCharacter=$false;pet=$false;essential=$false;immortal=$false;allowDyingCondition=$true;conscious=$true;inCombat=$true;paired=$false;eligible=$true});
 actorId=$controlId;slot=3;grantBefore=2;traceSequenceAtDamage=$controlDamageSequence;before=(LifeActor $controlId);requestedDamage=32;nativeDamage=32;afterDamage=(LifeActor $controlId 32);
 turn=(EmptyTurnRecord $controlId $null $controlStart);grantAfter=3})
$lifeGood=Copy-Value $v
# Derivation-only shape: no eligible companion, admitted only when the request declares the control not required.
$x=Copy-Value $lifeGood;$x.observations.allocationTrace.events=@($x.observations.allocationTrace.events|Where-Object{-not($null-ne$_.PSObject.Properties['round']-and$_.state.actor-ceq$controlId)})
$i=0;foreach($ev in $x.observations.allocationTrace.events){$i++;$ev.sequence=$i}
$x.observations.nativeLife.nativeNextRoundOrder=@($ids[0],$ids[2],$target,$ids[3],$ids[1])
$x.observations.nativeLife.incapacitatedRiderTurn.prepareSequence=@($x.observations.allocationTrace.events|Where-Object{$null-ne$_.PSObject.Properties['round']-and$_.state.actor-ceq$ids[2]})[0].sequence
$x.observations.nativeLife.unconsciousControl=[pscustomobject]@{contract='unmounted-unconscious-native-turn-control-v1';survivorSlot=3;candidates=@();available=$false}
$lifeNoControl=$x;$requestNoControl=Copy-Value $request;$requestNoControl|Add-Member NoteProperty nativeLifeControlRequired $false
Check 'complete synthetic native life cleanup chain with its unmounted unconscious control' {Assert-KmcChunk7NativeLifeEvidence $request $lifeGood 'PASS'}
Check 'derivation-only proof admitted when the request declares the control not required' {Assert-KmcChunk7NativeLifeEvidence $requestNoControl $lifeNoControl 'PASS'}
Check 'rider slot before the healthy renewal is derived and accepted' {$x=Copy-Value $lifeGood;$l=$x.observations.nativeLife;$l.nativeNextRoundOrder=@($ids[2],$ids[0],$target,$controlId,$ids[3],$ids[1]);$l.riderSlotsBeforeRenewal=1;$l.expectedRiderSlots=0;$l.healthyRenewed.grants[2].count=3
 $l.unconsciousControl.survivorSlot=4;$ev=@($x.observations.allocationTrace.events);$a=@($ev|Where-Object{$null-ne$_.PSObject.Properties['round']-and$_.round-eq3-and$_.state.actor-ceq$ids[0]});$r=@($ev|Where-Object{$null-ne$_.PSObject.Properties['round']-and$_.round-eq3-and$_.state.actor-ceq$ids[2]});$s=$a[0].sequence;$i=0
 foreach($e in $r){$e.sequence=$s+$i;$i++};foreach($e in $a){$e.sequence=$s+$i;$i++};$x.observations.allocationTrace.events=@($ev|Sort-Object sequence);$l.incapacitatedRiderTurn.prepareSequence=$s;Assert-KmcChunk7NativeLifeEvidence $request $x 'PASS'}
function RejectLife([string]$Name,[string]$Reason,[scriptblock]$Mutate,$Base=$lifeGood,$Req=$request){
 $x=Copy-Value $Base;& $Mutate $x;$refused=$false
 try{Assert-KmcChunk7NativeLifeEvidence $Req $x 'PASS'}catch{if($_.Exception.Message-cne$Reason){throw "$Name refused for another reason: $($_.Exception.Message)"};$refused=$true}
 if(-not$refused){throw "$Name was admitted."};$script:passed++;Write-Host "PASS refuses $Name"
}
function RiderTurnEvents($x,[string]$Boundary){@($x.observations.allocationTrace.events|Where-Object{$null-ne$_.PSObject.Properties['round']-and$_.round-eq3-and$_.state.actor-ceq$ids[2]-and$_.boundary-ceq$Boundary})}
function ControlTurnEvents($x,[string]$Boundary){@($x.observations.allocationTrace.events|Where-Object{$null-ne$_.PSObject.Properties['round']-and$_.round-eq3-and$_.state.actor-ceq$controlId-and$_.boundary-ceq$Boundary})}
function AppendLike($x,$Source,[string]$Boundary){$e=Copy-Value $Source;$e.boundary=$Boundary;$e.sequence=@($x.observations.allocationTrace.events).Count+1;$x.observations.allocationTrace.events+=,$e}
$turnMismatch='Unconscious unit did not receive exactly one empty native Prepare ended natively.'
RejectLife 'rider grant without its derived slot' 'Native next-round grants were duplicated, omitted or delayed.' {param($x)$x.observations.nativeLife.independentSurvivor.grants[2].count=2}
RejectLife 'second native rider grant' 'Native next-round grants were duplicated, omitted or delayed.' {param($x)$x.observations.nativeLife.independentSurvivor.grants[2].count=4}
RejectLife 'derived slots inconsistent with the order' 'Native next-round rider slots differ from the recorded native order.' {param($x)$x.observations.nativeLife.riderSlotsBeforeRenewal=1}
RejectLife 'recorded order not followed by native preparations' 'Native N+1 rider preparation does not follow the recorded order.' {param($x)$x.observations.nativeLife.nativeNextRoundOrder=@($ids[2],$ids[0],$target,$controlId,$ids[3],$ids[1]);$x.observations.nativeLife.riderSlotsBeforeRenewal=1;$x.observations.nativeLife.expectedRiderSlots=0}
RejectLife 'rider turn ended by ForceToEnd' $turnMismatch {param($x)$x.observations.allocationTrace.forceToEnd=@([pscustomobject]@{eventSequence=200;actor=$ids[2];frame=88;round=3});$x.observations.nativeLife.incapacitatedRiderTurn.forcedEnds=1}
RejectLife 'ForceToEnd observation missing from the trace' 'Native life trace lacks its ForceToEnd observation.' {param($x)$x.observations.allocationTrace.PSObject.Properties.Remove('forceToEnd')}
RejectLife 'rider turn with a paired grant identity' $turnMismatch {param($x)foreach($e in @($x.observations.allocationTrace.events|Where-Object{$null-ne$_.PSObject.Properties['round']-and$_.round-eq3-and$_.state.actor-ceq$ids[2]})){$e.state.pairedGrantIdentity='exact-b:3'};$x.observations.nativeLife.incapacitatedRiderTurn.pairedIdentities=$realRiderTurn.Count-1}
RejectLife 'rider turn with a KMC ledger grant' $turnMismatch {param($x)foreach($e in @($x.observations.allocationTrace.events|Where-Object{$null-ne$_.PSObject.Properties['round']-and$_.round-eq3-and$_.state.actor-ceq$ids[2]})){$e.state.retained.GrantIdentity='exact-b:3'};$x.observations.nativeLife.incapacitatedRiderTurn.grantIdentities=@('exact-b:3')}
RejectLife 'rider ledger not native after prepare-before' $turnMismatch {param($x)(RiderTurnEvents $x 'clear-before')[0].state.retained.GrantIdentity='exact-b:2';$x.observations.nativeLife.incapacitatedRiderTurn.grantIdentities=@('exact-b:2','native:rider-b-n1')}
RejectLife 'rider turn with a native command' 'Unconscious native turn forbidden differs from its trace.' {param($x)AppendLike $x (RiderTurnEvents $x 'clear-before')[0] 'command-end-before'}
RejectLife 'rider turn with an admission' 'Unconscious native turn forbidden differs from its trace.' {param($x)AppendLike $x (RiderTurnEvents $x 'clear-before')[0] 'admission-before'}
RejectLife 'rider turn with a native cost' 'Unconscious native turn forbidden differs from its trace.' {param($x)AppendLike $x (RiderTurnEvents $x 'clear-before')[0] 'cost-before'}
RejectLife 'rider turn not left Preparing' 'Unconscious native turn record differs from its trace.' {param($x)(RiderTurnEvents $x 'prepare-after')[0].turnStatus='Acting'}
RejectLife 'rider native End did not set Standard' 'Unconscious native turn record differs from its trace.' {param($x)(RiderTurnEvents $x 'turn-end-after')[0].state.standard=5.0}
RejectLife 'rider conscious during its native turn' 'Native evidence boolean is missing, mistyped or differs.' {param($x)$x.observations.nativeLife.incapacitatedRiderTurn.conscious=$true}
RejectLife 'rider turn record differs from the trace' 'Unconscious native turn record differs from its trace.' {param($x)$x.observations.nativeLife.incapacitatedRiderTurn.sequence=@('prepare-before','prepare-after','turn-end-after')}
RejectLife 'required control declared unavailable' 'Required unmounted unconscious control is unavailable.' {param($x)} $lifeNoControl
RejectLife 'control declared unavailable although a companion is eligible' 'Native evidence boolean is missing, mistyped or differs.' {param($x)$x.observations.nativeLife.unconsciousControl.candidates=@([pscustomobject]@{actorId=$controlId;slot=-1;mainCharacter=$false;pet=$false;essential=$false;immortal=$false;allowDyingCondition=$true;conscious=$true;inCombat=$true;paired=$false;eligible=$true})} $lifeNoControl $requestNoControl
RejectLife 'control turn differs from the rider turn' 'Unmounted unconscious control differs from the incapacitated rider native turn.' {param($x)foreach($e in @(ControlTurnEvents $x 'clear-before')+@(ControlTurnEvents $x 'clear-after')){$e.boundary='round-state'+$e.boundary.Substring(5)};$c=$x.observations.nativeLife.unconsciousControl.turn;$c.sequence=@($c.sequence|ForEach-Object{if($_-clike'clear-*'){'round-state'+$_.Substring(5)}else{$_}})}
RejectLife 'control not the first eligible companion' 'Unconscious control is not the first eligible companion in native order.' {param($x)$x.observations.nativeLife.unconsciousControl.actorId='5dd7bd1d-ae4c-4df7-9687-50fceb027344'}
RejectLife 'control turn ended by ForceToEnd' $turnMismatch {param($x)$x.observations.allocationTrace.forceToEnd=@([pscustomobject]@{eventSequence=200;actor=$controlId;frame=88;round=3});$x.observations.nativeLife.unconsciousControl.turn.forcedEnds=1}
RejectLife 'control candidates out of native slot order' 'Unconscious control candidates are not in native slot order.' {param($x)$c=$x.observations.nativeLife.unconsciousControl;$c.candidates=@($c.candidates)+@([pscustomobject]@{actorId='5dd7bd1d-ae4c-4df7-9687-50fceb027344';slot=-1;mainCharacter=$false;pet=$false;essential=$false;immortal=$false;allowDyingCondition=$true;conscious=$true;inCombat=$true;paired=$false;eligible=$false})}
RejectLife 'control damage not equal to its damage delta' 'Unconscious control damage is not one bounded living stimulus.' {param($x)$x.observations.nativeLife.unconsciousControl.nativeDamage=31}
Check 'control damage first spends temporary HP, exactly accounted' {$x=Copy-Value $lifeGood;$c=$x.observations.nativeLife.unconsciousControl;$c.before.temporaryHp=5;$c.afterDamage.damage=27+4;$c.before.damage=4;$c.afterDamage.temporaryHp=0;Assert-KmcChunk7NativeLifeEvidence $request $x 'PASS'}
RejectLife 'control temporary HP absorption not accounted' 'Unconscious control damage is not one bounded living stimulus.' {param($x)$c=$x.observations.nativeLife.unconsciousControl;$c.before.temporaryHp=5;$c.afterDamage.temporaryHp=0}
RejectLife 'control temporary HP grown by damage' 'Unconscious control damage is not one bounded living stimulus.' {param($x)$c=$x.observations.nativeLife.unconsciousControl;$c.afterDamage.temporaryHp=3;$c.afterDamage.damage=35}
RejectLife 'control grants not matching its native preparations' 'Unconscious control grants differ from its observed native preparations.' {param($x)$x.observations.nativeLife.unconsciousControl.grantAfter=4}
RejectLife 'control turn after the surviving mount' 'Unconscious control native turn does not precede the surviving mount.' {param($x)$ev=@($x.observations.allocationTrace.events);$c=@($ev|Where-Object{$null-ne$_.PSObject.Properties['round']-and$_.state.actor-ceq$controlId});$m=@($ev|Where-Object{$null-ne$_.PSObject.Properties['round']-and$_.round-eq3-and$_.state.actor-ceq$ids[3]});$s=$c[0].sequence;$i=0
 foreach($e in $m){$e.sequence=$s+$i;$i++};foreach($e in $c){$e.sequence=$s+$i;$i++};$x.observations.allocationTrace.events=@($ev|Sort-Object sequence);$x.observations.nativeLife.unconsciousControl.turn.prepareSequence=$s+2}
RejectLife 'unavailable control carrying a stimulus' 'Unavailable unconscious control carries a stimulus.' {param($x)$x.observations.nativeLife.unconsciousControl|Add-Member NoteProperty requestedDamage 32} $lifeNoControl $requestNoControl
$requestTextControl=Copy-Value $requestNoControl;$requestTextControl.nativeLifeControlRequired='false'
RejectLife 'non-boolean control requirement' 'Native life control requirement must be a boolean.' {param($x)} $lifeNoControl $requestTextControl
$cases=@(
 @('direct life callback without native tick',{$args[0].observations.nativeLife.deliveryProbe.events[2].nativeTickDepth=0}),
 @('direct SetLifeState without tick',{$args[0].observations.nativeLife.deliveryProbe.events[1].nativeTickDepth=0}),
 @('wrong native life token',{$args[0].observations.nativeLife.deliveryProbe.nativeSetToken='06000001'}),
 @('foreign production callback assembly',{$args[0].observations.nativeLife.deliveryProbe.callbackMvid='11111111-1111-4111-8111-111111111111'}),
 @('observer retained callback custody',{$args[0].observations.nativeLife.deliveryProbe.setDepth=1}),
 @('life observer missing callback',{$args[0].observations.nativeLife.deliveryProbe.events=@($args[0].observations.nativeLife.deliveryProbe.events|Where-Object boundary -CNE 'production-callback-before')}),
 @('native tick reordered',{$args[0].observations.nativeLife.deliveryProbe.events[4].boundary='production-callback-after'}),
 @('native callback wrong actor',{$args[0].observations.nativeLife.deliveryProbe.events[2].actorId=$ids[0]}),
 @('native callback round drift',{$args[0].observations.nativeLife.deliveryProbe.events[2].round=3}),
 @('same-call life delivery overclaim',{$args[0].observations.nativeLife.sameCallRoundBoundaryLifeDeliveryProven=$true}),
 @('main character native damage',{$args[0].observations.nativeLife.beforeDamage.rider.mainCharacter=$true}),
 @('incapacitation policy absent',{$args[0].observations.nativeLife.beforeDamage.rider.allowDyingCondition=$false}),
 @('native damage repeated',{$args[0].observations.nativeLife.nativeDamageDispatches=2}),
 @('damage hits death threshold',{$args[0].observations.nativeLife.afterDamage.rider.damage=42}),
 @('negative difficulty',{$args[0].observations.nativeLife.difficulty=-1.0}),
 @('damage before native rule differs',{$args[0].observations.nativeLife.nativeDamageBeforeDifficulty=31}),
 @('missing real damage rule',{$args[0].observations.nativeLife.nativeEffects.events=@()}),
 @('raw damage rule actor substituted',{$args[0].observations.nativeLife.nativeEffects.events[1].target=$ids[0]}),
 @('native rule identity changed',{$args[0].observations.nativeLife.nativeEffects.events[1].identity=998}),
 @('another actor health changed',{$args[0].observations.nativeLife.afterDelivery.healthyMount.damage=1}),
 @('native lifecycle ledger missing',{$args[0].observations.nativeLife.deliveries=@()}),
 @('native lifecycle ledger failed',{$args[0].observations.nativeLife.deliveries[0].succeeded=$false}),
 @('ledger outside observed callback',{$args[0].observations.nativeLife.deliveryProbe.events[3].ledgerSequence=5}),
 @('foreign actor life transition',{$args[0].observations.nativeLife.lifeTransitions[0].actorId=$ids[0]}),
 @('split round restarted',{$args[0].observations.nativeLife.afterDelivery.splitReleaseRound=3}),
 @('detached owner lost',{$args[0].observations.nativeLife.deliveryProbe.events[3].world.owners=@($args[0].observations.nativeLife.deliveryProbe.events[3].world.owners[0])}),
 @('same-length healthy mount substitution',{$args[0].observations.nativeLife.deliveryProbe.events[3].world.owners[0].mountId=$ids[3].Replace('-','')}),
 @('healthy pair debt changed',{$args[0].observations.nativeLife.deliveryProbe.events[3].world.actors[0].standard=0.0}),
 @('surviving mount debt dropped',{$args[0].observations.nativeLife.deliveryProbe.events[3].world.actors[3].standard=0.0}),
 @('extra preparation inside cleanup',{$args[0].observations.nativeLife.afterDelivery.grants[3].count=3}),
 @('native survivor delayed one round',{$args[0].observations.nativeLife.independentSurvivor.world.round=4}),
 @('survivor duplicate native grant',{$args[0].observations.nativeLife.independentSurvivor.grants[3].count=4}),
 @('incapacitated rider re-prepared',{$args[0].observations.nativeLife.healthyRenewed.grants[2].count=3}),
 @('native survivor still paired',{$args[0].observations.nativeLife.independentSurvivor.partnerContextActor=$ids[3]}),
 @('healthy continuation action absent',{$args[0].observations.nativeLife.healthyAttackAfter.outcome.completedAttacks=0}),
 @('healthy full native end absent',{$args[0].observations.nativeCompletion.calls=@($args[0].observations.nativeCompletion.calls|Select-Object -Skip 1)}),
 @('native round callback absent',{$args[0].observations.nativeRounds.events=@([pscustomobject]@{round=2})}),
 @('cleanup not settled',{$args[0].observations.cleanup.controlsSettled=$false})
)
foreach($case in $cases){$bad=Copy-Value $lifeGood;& $case[1] $bad;Reject $case[0] {Assert-KmcChunk7NativeLifeEvidence $request $bad 'PASS'}}
# 7E X1 death proper is read by its own branch on the owner-seed case only (its full synthetic chain
# and mutations are in Test-Chunk7OwnedPairsEvidence.ps1); the incapacitation chain cannot claim it.
Check 'a non-owned native life request is never the death case' {if(Test-KmcChunk7NativeDeathCase $request){throw 'Legacy request selected the death branch.'}}
RejectLife 'incapacitation evidence claiming a death outcome' 'Native incapacitation evidence claims a death outcome.' {param($x)$x.observations.nativeLife|Add-Member NoteProperty outcome 'death'}
RejectLife 'incapacitation evidence claiming a natural death stimulus' 'Native incapacitation evidence claims a death outcome.' {param($x)$x.observations|Add-Member NoteProperty naturalDeathStimulusProven $true}
Check 'native life closed rows' {if(@(Get-KmcChunk7NativeLifeRuntimeRows).Count-ne14){throw 'Wrong life row count.'}}
Reject 'unknown life scenario' {Get-KmcChunk7NativeLifeRows 'chunk7-two-pair-life-anything'}
Check 'native life partial FAIL preserved' {$bad=Copy-Value $lifeGood;$bad.status='FAIL';$bad.errors=@('fixture native life policy unavailable');$bad.observations=[pscustomobject]@{};Assert-KmcChunk7NativeLifeEvidence $request $bad 'FAIL'}
Reject 'native life FAIL cannot fulfill PASS' {$bad=Copy-Value $lifeGood;$bad.status='FAIL';$bad.errors=@('fixture unavailable');Assert-KmcChunk7NativeLifeEvidence $request $bad 'PASS'}
$lifeRoot=Join-Path $chunk7AnalysisRoot ('life-reader-artifact-fixture-'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($lifeRoot)|Out-Null;$lifePath=Join-Path $lifeRoot 'chunk7-native-life-evidence.json';$request.evidenceRoot=$lifeRoot
[IO.File]::WriteAllText($lifePath,($lifeGood|ConvertTo-Json -Depth 90),[Text.UTF8Encoding]::new($false))
$lifeManifest=[pscustomobject]@{artifacts=@([pscustomobject]@{relativePath='chunk7-native-life-evidence.json';kind='chunk7-native-life-evidence';length=(Get-Item -LiteralPath $lifePath).Length;sha256=(Get-KmcSha256 $lifePath)})}
Check 'native life manifest exact artifact' {Assert-KmcChunk7NativeLifeArtifact $request $lifeManifest}
Reject 'native life artifact hash drift' {$m=Copy-Value $lifeManifest;$m.artifacts[0].sha256='0'*64;Assert-KmcChunk7NativeLifeArtifact $request $m}
Reject 'foreign life artifact presence' {$r=Copy-Value $request;$r.scenario='chunk7-two-pair-combat-rider-first';Assert-KmcChunk7NativeLifeArtifact $r $lifeManifest}
Reject 'missing mandatory life artifact' {Assert-KmcChunk7NativeLifeArtifact $request ([pscustomobject]@{artifacts=@()})}
Check 'interrupted life artifact may be absent' {Assert-KmcChunk7NativeLifeArtifact $request ([pscustomobject]@{artifacts=@()}) 'FAIL'}
# b193 x1s1: owner rows carry the registry's "N"-form rider key, so the producer's healthy-owner
# lookup must match by GUID, never by raw string equality with the hyphenated UniqueId.
Check 'producer healthy-owner lookup matches the N-form owner key' {
 $src=[IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7CombatNativeLife.cs'))
 if($src-match'\(string\)x\["riderId"\]\s*==\s*ar\.UniqueId'){throw 'Healthy-owner lookup compares the N-form owner key with the hyphenated UniqueId.'}
 if(([regex]::Matches($src,'OwnerActorMatch\(\(string\)x\["riderId"\], ar\)')).Count-ne2){throw 'Healthy-owner lookup does not use the GUID match on both snapshots.'}
 if($src-notmatch'Guid\.TryParse\(ownerValue, out var owner\) && Guid\.TryParse\(actor\.UniqueId, out var id\) \? owner == id'){throw 'Owner actor match is not a GUID comparison.'}
}
Check 'producer counts paired identities by value, not by token type' {
 $src=[IO.File]::ReadAllText((Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk7CombatNativeLife.cs'))
 if($src-match'pairedGrantIdentity"\]\.Type\s*!=\s*JTokenType\.Null'){throw 'Paired identities are counted by token type; a null string is a String-typed JValue.'}
 if($src-notmatch'\["pairedIdentities"\] = window\.Count\(e => \(string\)e\["state"\]\?\["pairedGrantIdentity"\] != null\)'){throw 'Paired identities are not counted by value.'}
}
Write-Host ('RESULT '+$passed+' passed; 0 failed (synthetic native life reader checks only)')
