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
 nativeEffects=[pscustomobject]@{dropped=0;events=@([pscustomobject]@{kind='damage-before';identity=999;actor=$target;target=$ids[2];targetDamage=0},[pscustomobject]@{kind='damage-after';identity=999;actor=$target;target=$ids[2];targetDamage=32;damage=32;beforeDifficulty=32})};
 deliveries=@([pscustomobject]@{sequence=6;boundary='UnitIncapacitated';source='IUnitLifeStateChanged.HandleUnitLifeStateChanged';stateBefore='Mounted';stateAfter='Unmounted';trigger='Incapacitated';attempted=$true;succeeded=$true;errors=@()});lifeTransitions=@([pscustomobject]@{sequence=3;actorId=$ids[2];previousLife='Conscious';currentLife='Unconscious'})}
$v=Copy-Value $good;$v.scenario=$request.scenario;$v.evidenceKind='chunk7-native-life-evidence';$v.observations.scenario=$request.scenario;$v.observations.naturalUnconsciousStimulusProven=$true;$v.observations.inputs=@($v.observations.inputs|Where-Object label -CIN @('mount-a','mount-b'));$v.observations.allocationTrace.events=$traceEvents
$v.observations.nativeRounds.events=@([pscustomobject]@{round=2},[pscustomobject]@{round=3});$v.observations.nativeCompletion.calls=@()
foreach($frame in @(55,86)){foreach($id in @($ids[0],$ids[1])){$v.observations.nativeCompletion.calls+=,[pscustomobject]@{kind='end';complete=$true;before=[pscustomobject]@{actorId=$id;frame=$frame};after=[pscustomobject]@{frame=$frame}}}}
$v.observations|Add-Member NoteProperty nativeLife $life;$lifeGood=Copy-Value $v
Check 'complete synthetic native life cleanup chain' {Assert-KmcChunk7NativeLifeEvidence $request $lifeGood 'PASS'}
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
Check 'native life closed rows' {if(@(Get-KmcChunk7NativeLifeRuntimeRows).Count-ne12){throw 'Wrong life row count.'}}
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
Write-Host ('RESULT '+$passed+' passed; 0 failed (synthetic native life reader checks only)')
