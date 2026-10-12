# The native life case is separate from the diagnostic duplicate-cleanup R1 case.
Set-StrictMode -Version Latest
function Test-KmcChunk7NativeLifeScenario([string]$Scenario){$Scenario-ceq'chunk7-two-pair-native-life-cleanup'}
function Get-KmcChunk7NativeLifeRows([string]$Scenario){
 if(-not(Test-KmcChunk7NativeLifeScenario $Scenario)){throw 'Unknown native life case.'}
 @($Scenario,'C7-combat-fixture','C7-combat-native-mounts','C7-combat-native-orders','C7-life-safe-native-damage','C7-life-native-callback','C7-life-healthy-neighbour','C7-life-independent-survivor','C7-combat-cleanup','C7-combat-runtime','C7-combat-interrupted')
}
function Get-KmcChunk7NativeLifeRuntimeRows{Get-KmcChunk7NativeLifeRows 'chunk7-two-pair-native-life-cleanup'}
function Assert-KmcChunk7NativeLifeArtifact($Request,$Manifest,[string]$Status='PASS'){
 $present=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq'chunk7-native-life-evidence.json'-or$_.kind-ceq'chunk7-native-life-evidence'})
 if(-not(Test-KmcChunk7NativeLifeScenario $Request.scenario)){if($present.Count-ne0){throw 'Foreign scenario supplied native life evidence.'};return}
 if($present.Count-eq0-and$Status-cne'PASS'){return}
 $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-native-life-evidence.json' 'chunk7-native-life-evidence'
 Assert-KmcChunk7NativeLifeEvidence $Request $artifact $Status
}
function Get-KmcChunk7LifeGrant($State,[string]$Actor){
 $rows=@($State.grants|Where-Object actorId -CEQ $Actor)
 if($rows.Count-ne1-or-not(Test-KmcExactJsonInteger $rows[0].count)-or$rows[0].count-lt1){throw 'Native life grant observation is missing or invalid.'}
 [long]$rows[0].count
}
function Assert-KmcChunk7LifeOwner($Before,$After,[string]$Rider,[string]$Mount){
 $a=@($Before.owners|Where-Object riderId -CEQ $Rider);$b=@($After.owners|Where-Object riderId -CEQ $Rider)
 if($a.Count-ne1-or$b.Count-ne1-or$a[0].state-cne'Active'-or$b[0].state-cne'Active'-or$a[0].mountId-cne$Mount-or$b[0].mountId-cne$Mount-or
    (ConvertTo-KmcChunk7Canonical $a[0])-cne(ConvertTo-KmcChunk7Canonical $b[0])){throw 'Native life cleanup changed healthy owner identity or generation.'}
}
function Assert-KmcChunk7LifeActor($Actor,[string]$Id,[bool]$Conscious){
 if($Actor.actorId-cne$Id){throw 'Native life actor identity differs.'}
 foreach($f in @('damage','hp','constitution','temporaryHp')){if(-not(Test-KmcExactJsonInteger $Actor.$f)-or$Actor.$f-lt0){throw 'Native life stat observation is invalid.'}}
 Assert-KmcChunk7Boolean $Actor.conscious $Conscious;Assert-KmcChunk7Boolean $Actor.dead $false
 if($Actor.life-cne$(if($Conscious){'Conscious'}else{'Unconscious'})){throw 'Native life state label differs.'}
}
function Assert-KmcChunk7NativeLifeEvidence($Request,$Artifact,[string]$Status){
 Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-native-life-evidence'
 if(-not(Test-KmcChunk7NativeLifeScenario $Request.scenario)){throw 'Native life artifact has a foreign case.'}
 if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS life result requires a PASS artifact.'};if($Artifact.status-cne'PASS'){return}
 $o=$Artifact.observations;$ids=@($o.actorIds);$life=$o.nativeLife;$probe=$life.deliveryProbe
 if($o.scenario-cne$Request.scenario-or$o.workingSha256-cne$Request.fixture.working.sha256-or$o.workingSha256-cne'5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'-or
    $ids.Count-ne4-or@($ids|Select-Object -Unique).Count-ne4-or$ids[0]-cne'b6628a77-4962-47a4-a17c-88d9836fc9d5'-or$ids[1]-cne'd79a4f6c-b74e-4868-95bd-533899131acb'-or$ids[2]-cne'd17c8fd0-6627-40c4-bf9a-024cf001c1f7'-or
    $o.targetId-cin$ids-or$o.targetGroupId-cne('KMC.RuntimeHostile.'+$Request.runId+':two-pair-target')){throw 'Native life fixture binding differs.'}
 [Guid]$g=[Guid]::Empty;if(-not[Guid]::TryParse($ids[3],[ref]$g)-or-not[Guid]::TryParse($o.targetId,[ref]$g)){throw 'Native dynamic actor identity invalid.'}
 Assert-KmcChunk7Boolean $o.fullRealtimeCombatProof $false;Assert-KmcChunk7Boolean $o.naturalUnconsciousStimulusProven $true
 Assert-KmcChunk7Boolean $life.sameCallRoundBoundaryLifeDeliveryProven $false;Assert-KmcChunk7Boolean $life.complete $true
 if($life.stimulus-cne'one labelled native RuleDealDamage; stock UnitLifeController tick delivers existing lifecycle subscriber'-or
    $life.subjectId-cne$ids[2]-or$life.survivorId-cne$ids[3]-or(ConvertTo-KmcChunk7Canonical $life.healthyActorIds)-cne(ConvertTo-KmcChunk7Canonical @($ids[0],$ids[1]))-or
    -not(Test-KmcExactJsonInteger $life.initialRound)-or$life.initialRound-lt0-or$life.damageRound-ne$life.initialRound+1){throw 'Native life scope, actor or actual N+1 round differs.'}
 if($o.acquisition.nativeRangerCommitCount-ne4-or$o.acquisition.horseFactRankAtCommit-ne1-or$o.acquisition.horsePresentAtNativeCommit-ne$true){throw 'Native Horse acquisition is incomplete.'}
 Assert-KmcChunk7CheckpointTrace $o.allocationTrace $Request
 if(@($o.nativeCompletion.errors).Count-ne0-or$o.nativeCompletion.traceComplete-ne$true-or@($o.nativeRounds.errors).Count-ne0-or$o.nativeRounds.token-cne'06000BD3'-or$o.nativeRounds.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Native lifecycle context/round observation is incomplete.'}
 foreach($round in @($life.damageRound,($life.damageRound+1))){if(@($o.nativeRounds.events|Where-Object round -EQ $round).Count-ne1){throw 'Life continuation lacks an exact native round boundary.'}}
 $order=@($o.nativeInitialOrder);if(@($order|Select-Object -Unique).Count-ne$order.Count-or$o.targetId-cnotin$order){throw 'Life case has no exact unrelated native roster actor.'}
 foreach($i in @(0,2)){if([Array]::IndexOf($order,$ids[$i])-lt0-or[Array]::IndexOf($order,$ids[$i+1])-le[Array]::IndexOf($order,$ids[$i])){throw 'Life fixture is not actual rider-first order.'}}
 $mounts=@($o.inputs|Where-Object{$_.label-cin@('mount-a','mount-b')});if($mounts.Count-ne2){throw 'Native life fixture lacks two native Mount inputs.'}
 for($i=0;$i-lt2;$i++){$m=$mounts[$i];if($m.casterId-cne$ids[2*$i]-or$m.targetId-cne$ids[2*$i+1]-or$m.abilityGuid-cne'f053faad986631688defa003cd7bda0e'-or$m.nativeCastDelta-ne1-or$m.nativeRefusalDelta-ne0-or$m.generationAtInit-lt0){throw 'Life native mount command binding differs.'};foreach($f in @('clicked','terminal','processTerminal')){Assert-KmcChunk7Boolean $m.$f $true}}
 $commands=[Collections.Generic.HashSet[long]]::new()
 foreach($row in @(@($life.healthyAttackBefore,$ids[0],$ids[0]),@($life.affectedAttackBefore,$ids[3],$ids[2]),@($life.healthyAttackAfter,$ids[0],$ids[0]))){
  $cost=Assert-KmcChunk7CombatAttack $row[0] $o.allocationTrace $row[1] $row[2] $o.targetId $ids
  if(-not$commands.Add($cost)){throw 'Native life case reused actor expenditure.'}
 }
 $before=$life.beforeDamage;$after=$life.afterDamage;$delivery=$life.afterDelivery
 Assert-KmcChunk7LifeActor $before.rider $ids[2] $true;Assert-KmcChunk7LifeActor $after.rider $ids[2] $true;Assert-KmcChunk7LifeActor $delivery.rider $ids[2] $false
 foreach($f in @('mainCharacter','essential','immortal')){Assert-KmcChunk7Boolean $before.rider.$f $false};Assert-KmcChunk7Boolean $before.rider.allowDyingCondition $true
 if($life.nativeDamageDispatches-ne1-or$life.sourceId-cne$o.targetId-or$life.nativeDamageBeforeDifficulty-ne$life.requestedDamage-or$life.nativeDamage-le0-or
    -not(Test-KmcExactJsonInteger $life.requestedDamage)-or$life.requestedDamage-le0-or-not(Test-KmcChunk7FiniteNumber $life.difficulty)-or$life.difficulty-le0-or
    $life.deathThreshold-ne$before.rider.hp+$before.rider.constitution-or$after.rider.damage-lt$before.rider.hp+1-or$after.rider.damage-ge$life.deathThreshold-or
    $before.rider.damage+$life.requestedDamage*$life.difficulty-$before.rider.temporaryHp-ge$life.deathThreshold-or
    $after.world.frame-ne$before.world.frame-or$before.world.round-ne$life.damageRound-or$delivery.world.round-ne$life.damageRound){throw 'Native damage is not one bounded living incapacitation stimulus.'}
 foreach($f in @('hp','constitution')){if($before.rider.$f-ne$after.rider.$f){throw 'Native damage fixture changed life policy stats.'}}
 Assert-KmcChunk7SameCombatBudget $before.world $after.world $ids
 foreach($name in @('mount','healthyRider','healthyMount')){if((ConvertTo-KmcChunk7Canonical $before.$name)-cne(ConvertTo-KmcChunk7Canonical $after.$name)-or(ConvertTo-KmcChunk7Canonical $before.$name)-cne(ConvertTo-KmcChunk7Canonical $delivery.$name)){throw 'Targeted native damage changed another actor life state.'}}
 if($life.nativeEffects.dropped-ne0){throw 'Native damage rule observations were dropped.'}
 $damageBefore=@($life.nativeEffects.events|Where-Object{$_.kind-ceq'damage-before'-and$_.actor-ceq$o.targetId-and$_.target-ceq$ids[2]})
 $damageAfter=@($life.nativeEffects.events|Where-Object{$_.kind-ceq'damage-after'-and$_.actor-ceq$o.targetId-and$_.target-ceq$ids[2]})
 if($damageBefore.Count-ne1-or$damageAfter.Count-ne1-or$damageBefore[0].identity-le0-or$damageBefore[0].identity-ne$damageAfter[0].identity-or
    $damageBefore[0].targetDamage-ne$before.rider.damage-or$damageAfter[0].targetDamage-ne$after.rider.damage-or$damageAfter[0].damage-ne$life.nativeDamage-or$damageAfter[0].beforeDifficulty-ne$life.requestedDamage){throw 'Life stimulus lacks one exact native damage rule observation.'}
 if($probe.actorId-cne$ids[2]-or$probe.nativeModuleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-or$probe.nativeTickToken-cne'06009162'-or$probe.nativeSetToken-cne'06009164'-or
    $probe.callbackMvid-cne$Request.dllMvid-or$probe.callbackToken-cnotmatch'^06[0-9A-F]{6}$'-or$probe.callbackType-cne'KingmakerMountedCombat.Integration.MountedLifecycleSubscriber'-or$probe.callbackName-cne'HandleUnitLifeStateChanged'-or
    $probe.tickDepth-ne0-or$probe.setDepth-ne0-or@($probe.errors).Count-ne0){throw 'Exact native lifecycle observer identity or closure differs.'}
 Assert-KmcChunk7Boolean $probe.completedNativeTick $true
 $events=@($probe.events);$sequence=0
 foreach($e in $events){if($e.sequence-ne(++$sequence)-or$e.actorId-cne$ids[2]-or$e.round-ne$life.damageRound-or$e.frame-lt$after.world.frame){throw 'Life observer sequence/actor/round differs.'}}
 $names=@('native-tick-before','native-set-before','production-callback-before','production-callback-after','native-set-after','native-tick-after')
 if((ConvertTo-KmcChunk7Canonical @($events.boundary))-cne(ConvertTo-KmcChunk7Canonical $names)){throw 'No exact native Tick to SetLifeState to production callback chain.'}
 for($i=0;$i-lt6;$i++){if($events[$i].nativeTickDepth-ne1-or$events[$i].nativeSetDepth-ne$(if($i-in@(0,5)){0}else{1})-or$events[$i].frame-ne$events[0].frame){throw 'Life callback was direct, reordered or outside the same native tick.'}}
 foreach($i in 2..3){if($events[$i].previousLife-cne'Conscious'-or$events[$i].currentLife-cne'Unconscious'-or$events[$i].nativeSetSequence-ne$events[1].nativeSetSequence){throw 'Life callback did not report the actual native transition.'}}
 $cb=$events[2];$ca=$events[3]
 Assert-KmcChunk7LifeOwner $cb.world $ca.world $ids[0] $ids[1]
 Assert-KmcChunk7SameCombatBudget $cb.world $ca.world @($ids[0],$ids[1],$ids[3])
 $affectedBefore=@($cb.world.owners|Where-Object riderId -CEQ $ids[2]);$affectedAfter=@($ca.world.owners|Where-Object riderId -CEQ $ids[2])
 if($affectedBefore.Count-ne1-or$affectedAfter.Count-ne1-or$affectedBefore[0].state-cne'Active'-or$affectedAfter[0].state-cne'Detached'-or
    $affectedBefore[0].id-cne$affectedAfter[0].id-or$affectedAfter[0].mountId-cne$ids[3]){throw 'Actual native life cleanup lost detached actor custody.'}
 Assert-KmcChunk7Boolean $affectedAfter[0].retainsObligations $true
 $records=@($life.deliveries|Where-Object boundary -CEQ 'UnitIncapacitated');$changes=@($life.lifeTransitions)
 if($records.Count-ne1-or$changes.Count-ne1-or$records[0].sequence-le$life.ledgerOffset-or$ca.ledgerSequence-ne$records[0].sequence-or$cb.ledgerSequence-ge$records[0].sequence-or
    $records[0].source-cne'IUnitLifeStateChanged.HandleUnitLifeStateChanged'-or$records[0].stateBefore-cne'Mounted'-or$records[0].stateAfter-cne'Unmounted'-or$records[0].trigger-cne'Incapacitated'-or@($records[0].errors).Count-ne0-or
    $changes[0].sequence-le$life.lifeTransitionOffset-or$changes[0].actorId-cne$ids[2]-or$changes[0].previousLife-cne'Conscious'-or$changes[0].currentLife-cne'Unconscious'){throw 'Native lifecycle ledger does not bind this exact successful actor cleanup.'}
 Assert-KmcChunk7Boolean $records[0].attempted $true;Assert-KmcChunk7Boolean $records[0].succeeded $true
 if($delivery.pairState-cne'Unmounted'-or$delivery.splitReleaseRound-ne$life.damageRound-or$delivery.partnerContextActor-cnotin@($null,$ids[3])){throw 'Life detach changed release round or exact native context custody.'}
 Assert-KmcChunk7Boolean $delivery.activationSplit $true;Assert-KmcChunk7Boolean $delivery.presentationResidue $false
 if($life.traceSequenceAfterDelivery-lt$life.traceSequenceBeforeDamage-or@($o.allocationTrace.events|Where-Object{$_.sequence-gt$life.traceSequenceBeforeDamage-and$_.sequence-le$life.traceSequenceAfterDelivery-and$_.boundary-ceq'prepare-before'-and$_.state.actor-cin$ids}).Count-ne0){throw 'Life delivery generated an extra native preparation.'}
 foreach($id in $ids){if((Get-KmcChunk7LifeGrant $before $id)-ne(Get-KmcChunk7LifeGrant $delivery $id)){throw 'Native life cleanup changed actor grant counts.'}}
 $renewed=$life.healthyRenewed;$independent=$life.independentSurvivor
 if($renewed.world.round-ne$life.damageRound+1-or$renewed.world.currentActor-cne$ids[0]-or$independent.world.round-ne$life.damageRound+1-or$independent.world.currentActor-cne$ids[3]-or$null-ne$independent.partnerContextActor){throw 'Native healthy/survivor continuation missed original next round.'}
 Assert-KmcChunk7LifeActor $independent.rider $ids[2] $false
 foreach($i in 0..3){$baseline=Get-KmcChunk7LifeGrant $before $ids[$i];$fresh=Get-KmcChunk7LifeGrant $renewed $ids[$i];$last=Get-KmcChunk7LifeGrant $independent $ids[$i]
  if($fresh-ne$baseline+$(if($i-lt2){1}else{0})-or$last-ne$baseline+$(if($i-ne2){1}else{0})){throw 'Native next-round grants were duplicated, omitted or delayed.'}}
 foreach($id in @($ids[0],$ids[1])){$actor=Get-KmcChunk7CombatActor $renewed.world $id;if($actor.standard-ne0-or$actor.move-ne0){throw 'Healthy native renewal lacks fresh native expenditure.'}}
 $survivor=Get-KmcChunk7CombatActor $independent.world $ids[3];if($survivor.standard-ne0-or$survivor.move-ne0-or@($independent.world.owners|Where-Object{$_.state-ceq'Active'-and$_.mountId-ceq$ids[3]}).Count-ne0){throw 'Surviving mount lacks an independent native allocation.'}
 Assert-KmcChunk7LifeOwner $before.world $renewed.world $ids[0] $ids[1]
 foreach($window in @(@($life.healthyAttackBefore,$life.healthyEndedBeforeDamage),@($life.healthyAttackAfter,$life.healthyEndedAfterCleanup))){
  foreach($id in @($ids[0],$ids[1])){$end=@($o.nativeCompletion.calls|Where-Object{$_.kind-ceq'end'-and$_.complete-eq$true-and$_.before.actorId-ceq$id-and$_.before.frame-ge$window[0].before.frame-and$_.after.frame-le$window[1].world.frame})
   if($end.Count-ne1-or(Get-KmcChunk7CombatActor $window[1].world $id).standard-lt5.9){throw 'Healthy native actor contexts did not fully end before and after local cleanup.'}}
 }
 foreach($field in @('pairResidueAbsent','ownedHorseRemoved','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}
 Assert-KmcChunk7Boolean $o.cleanup.nativeSaveWrittenByThisDriver $false;Assert-KmcChunk7Boolean $o.cleanup.delegatedPersistenceCleanup $false
}
