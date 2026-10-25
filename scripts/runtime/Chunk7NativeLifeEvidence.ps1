# The native life case is separate from the diagnostic duplicate-cleanup R1 case.
Set-StrictMode -Version Latest
function Test-KmcChunk7NativeLifeScenario([string]$Scenario){$Scenario-ceq'chunk7-two-pair-native-life-cleanup'}
# 7E X1 death proper: the owner-seed case that crosses HP+Constitution on the same native life engine.
function Test-KmcChunk7NativeDeathCase($Request){(Test-KmcChunk7OwnedPairsScenario $Request.scenario)-and(Get-KmcChunk7OwnedPairsCase $Request)-ceq'owner-seed-native-death-cleanup'}
function Get-KmcChunk7NativeLifeRows([string]$Scenario){
 if(-not(Test-KmcChunk7NativeLifeScenario $Scenario)){throw 'Unknown native life case.'}
 @($Scenario,'C7-combat-fixture','C7-combat-native-mounts','C7-combat-restored-mounts','C7-combat-native-orders','C7-life-safe-native-damage','C7-life-native-callback','C7-life-healthy-neighbour','C7-life-independent-survivor','C7-life-prone-native-heal','C7-life-prone-stand-up-opportunity','C7-combat-cleanup','C7-combat-runtime','C7-combat-interrupted')
}
function Get-KmcChunk7NativeLifeRuntimeRows{Get-KmcChunk7NativeLifeRows 'chunk7-two-pair-native-life-cleanup'}
function Assert-KmcChunk7NativeLifeArtifact($Request,$Manifest,[string]$Status='PASS'){
 $present=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq'chunk7-native-life-evidence.json'-or$_.kind-ceq'chunk7-native-life-evidence'})
 if(-not(Test-KmcChunk7NativeLifeScenario (Get-KmcChunk7SelectedScenario $Request))){if($present.Count-ne0){throw 'Foreign scenario supplied native life evidence.'};return}
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
 $a=@($Before.owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $Rider});$b=@($After.owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $Rider})
 if($a.Count-ne1-or$b.Count-ne1-or$a[0].state-cne'Active'-or$b[0].state-cne'Active'-or-not(Test-KmcChunk7OwnerActorMatch $a[0].mountId $Mount)-or-not(Test-KmcChunk7OwnerActorMatch $b[0].mountId $Mount)-or
    (ConvertTo-KmcChunk7Canonical $a[0])-cne(ConvertTo-KmcChunk7Canonical $b[0])){throw 'Native life cleanup changed healthy owner identity or generation.'}
}
function Assert-KmcChunk7LifeActor($Actor,[string]$Id,[bool]$Conscious){
 if($Actor.actorId-cne$Id){throw 'Native life actor identity differs.'}
 foreach($f in @('damage','hp','constitution','temporaryHp')){if(-not(Test-KmcExactJsonInteger $Actor.$f)-or$Actor.$f-lt0){throw 'Native life stat observation is invalid.'}}
 Assert-KmcChunk7Boolean $Actor.conscious $Conscious;Assert-KmcChunk7Boolean $Actor.dead $false
 if($Actor.life-cne$(if($Conscious){'Conscious'}else{'Unconscious'})){throw 'Native life state label differs.'}
}
# Native 2.1.7b gives a living unconscious unit one native Prepare and an empty turn that Tick
# ends natively. The affected rider's N+1 slots derive from the native order at damage time.
function Get-KmcChunk7LifeRiderSlots($Order,$Ids){
 $order=@($Order);if($order.Count-lt3-or@($order|Select-Object -Unique).Count-ne$order.Count){throw 'Native next-round order is missing or duplicated.'}
 $ia=[Array]::IndexOf($order,$Ids[0]);$ibr=[Array]::IndexOf($order,$Ids[2]);$ibm=[Array]::IndexOf($order,$Ids[3])
 if($ia-lt0-or$ibr-lt0-or$ibm-lt0-or$ibr-ge$ibm){throw 'Native next-round order lacks the exact rider-first affected pair.'}
 [pscustomobject]@{beforeRenewal=[long]$(if($ibr-lt$ia){1}else{0});betweenRenewalAndSurvivor=[long]$(if($ia-lt$ibr-and$ibr-lt$ibm){1}else{0});survivorSlot=$ibm}
}
# The death control is chosen after the native death delivery. Native SetLifeState(Dead) has run
# UnitEntityData.LeaveCombat for the rider by then, so the control reads the native SortedUnits
# without the dead rider: exactly the recorded damage-time order with that one rider removed
# (b202 d1: survivor 5 -> 4, candidates 2 -> 1 and 4 -> 3). Its survivor and candidate slots index
# this order. Contract v2 also records the order the producer read, which must equal it.
function Get-KmcChunk7DeathControlOrder($Order,[string]$DeadRider){
 $order=@($Order);if([Array]::IndexOf($order,$DeadRider)-lt0){throw 'Native next-round order lacks the dead rider at damage time.'}
 ,[string[]]@($order|Where-Object{$_-cne$DeadRider})
}
# One unit's N+1 turn on the trace's own hooks for every unit: native Prepare entry to the first
# turn-end-after (End). Paired-only completion probes and UI ready handlers are excluded, so the
# rider and an unpaired control compare on identical boundaries. ForceToEnd is the trace's separate
# diagnostic list. The ledger identity counts after prepare-before, whose entry still names the
# previous round's activation.
function Get-KmcChunk7UnconsciousNativeTurn($Trace,[string]$Actor,[long]$Round){
 $events=@($Trace.events|Where-Object{$null-ne$_.PSObject.Properties['round']-and$_.round-eq$Round-and$null-ne$_.PSObject.Properties['state']-and$null-ne$_.state-and$_.state.actor-ceq$Actor})
 $start=-1;for($i=0;$i-lt$events.Count;$i++){if($events[$i].boundary-ceq'prepare-before'){$start=$i;break}}
 $stop=-1;if($start-ge0){for($i=$start;$i-lt$events.Count;$i++){if($events[$i].boundary-ceq'turn-end-after'){$stop=$i;break}}}
 $window=@(if($stop-ge0){$events[$start..$stop]})
 $prepared=@($window|Where-Object boundary -CEQ 'prepare-after')|Select-Object -First 1;$ended=if($window.Count){$window[$window.Count-1]}else{$null}
 $forbidden=@($events|Where-Object{$b=[string]$_.boundary;@('admission-','cost-','actor-cost-','command-','completion-force-to-end-','chunk7-end-input-')|Where-Object{$b.StartsWith($_,[StringComparison]::Ordinal)}}).Count
 $identities=[Collections.Generic.List[object]]::new();for($i=1;$i-lt$window.Count;$i++){$r=$window[$i].state.retained;$id=if($r-is[pscustomobject]){[string]$r.GrantIdentity}else{$null};if(-not$identities.Contains($id)){$identities.Add($id)}}
 $forceList=@(if($null-ne$Trace.PSObject.Properties['forceToEnd']){$Trace.forceToEnd});$forced=@($forceList|Where-Object{$null-ne$_-and$_.actor-ceq$Actor-and$_.round-eq$Round}).Count
 [pscustomobject]@{prepareBefore=[long]@($events|Where-Object boundary -CEQ 'prepare-before').Count;prepareAfter=[long]@($events|Where-Object boundary -CEQ 'prepare-after').Count
  ends=[long]@($events|Where-Object boundary -CEQ 'turn-end-before').Count;forcedEnds=[long]$forced;forbidden=[long]$forbidden
  sequence=@($window|ForEach-Object{[string]$_.boundary}|Where-Object{-not$_.StartsWith('ready-handler-',[StringComparison]::Ordinal)-and-not$_.StartsWith('completion-',[StringComparison]::Ordinal)})
  grantIdentities=@($identities);pairedIdentities=[long]@($window|Where-Object{$null-ne$_.state.pairedGrantIdentity}).Count
  prepareSequence=$(if($window.Count){$window[0].sequence}else{$null})
  statusAfterPrepare=$(if($prepared){[string]$prepared.turnStatus}else{$null});standardBefore=$(if($prepared){$prepared.state.standard}else{$null});standardAfterEnd=$(if($ended){$ended.state.standard}else{$null})}
}
function Assert-KmcChunk7UnconsciousNativeTurn($Recorded,$Trace,[string]$Actor,[long]$Round,[bool]$NativeLedger){
 Assert-KmcExactProperties $Recorded @('actorId','round','prepareBefore','prepareAfter','ends','forcedEnds','forbidden','sequence','grantIdentities','pairedIdentities','prepareSequence','statusAfterPrepare','standardBefore','standardAfterEnd','conscious','dead') 'unconscious native turn'
 if($Recorded.actorId-cne$Actor-or$Recorded.round-ne$Round){throw 'Unconscious native turn identity or round differs.'}
 $d=Get-KmcChunk7UnconsciousNativeTurn $Trace $Actor $Round
 foreach($f in @('prepareBefore','prepareAfter','ends','forcedEnds','forbidden','pairedIdentities','prepareSequence')){if(-not(Test-KmcExactJsonInteger $Recorded.$f)-or$Recorded.$f-ne$d.$f){throw "Unconscious native turn $f differs from its trace."}}
 if((ConvertTo-KmcChunk7Canonical @($Recorded.sequence))-cne(ConvertTo-KmcChunk7Canonical @($d.sequence))-or(ConvertTo-KmcChunk7Canonical @($Recorded.grantIdentities))-cne(ConvertTo-KmcChunk7Canonical @($d.grantIdentities))-or
    $Recorded.statusAfterPrepare-cne$d.statusAfterPrepare-or$Recorded.standardBefore-ne$d.standardBefore-or$Recorded.standardAfterEnd-ne$d.standardAfterEnd){throw 'Unconscious native turn record differs from its trace.'}
 Assert-KmcChunk7Boolean $Recorded.conscious $false;Assert-KmcChunk7Boolean $Recorded.dead $false
 $ids=@($Recorded.grantIdentities)
 if($Recorded.prepareBefore-ne1-or$Recorded.prepareAfter-ne1-or$Recorded.ends-ne1-or$Recorded.forcedEnds-ne0-or$Recorded.forbidden-ne0-or$Recorded.pairedIdentities-ne0-or$Recorded.statusAfterPrepare-cne'Preparing'-or
    -not(Test-KmcChunk7FiniteNumber $Recorded.standardAfterEnd)-or$Recorded.standardAfterEnd-ne6-or
    $(if($NativeLedger){$ids.Count-ne1-or$ids[0]-isnot[string]-or-not$ids[0].StartsWith('native:',[StringComparison]::Ordinal)}else{@($ids|Where-Object{$null-ne$_-and-not([string]$_).StartsWith('native:',[StringComparison]::Ordinal)}).Count-ne0})){throw 'Unconscious unit did not receive exactly one empty native Prepare ended natively.'}
}function Assert-KmcChunk7NativeLifeEvidence($Request,$Artifact,[string]$Status){
 Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-native-life-evidence'
 if(-not(Test-KmcChunk7NativeLifeScenario (Get-KmcChunk7SelectedScenario $Request))){throw 'Native life artifact has a foreign case.'}
 if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS life result requires a PASS artifact.'};if($Artifact.status-cne'PASS'){return}
 $o=$Artifact.observations;$ids=@($o.actorIds);$life=$o.nativeLife;$probe=$life.deliveryProbe
 $owned=Test-KmcChunk7OwnedPairsScenario $Request.scenario
 if($owned){Assert-KmcChunk7OwnedActionFixture $Request $o}else{
 if($o.scenario-cne$Request.scenario-or$o.workingSha256-cne$Request.fixture.working.sha256-or$o.workingSha256-cne'5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'-or
    $ids.Count-ne4-or@($ids|Select-Object -Unique).Count-ne4-or$ids[0]-cne'b6628a77-4962-47a4-a17c-88d9836fc9d5'-or$ids[1]-cne'd79a4f6c-b74e-4868-95bd-533899131acb'-or$ids[2]-cne'd17c8fd0-6627-40c4-bf9a-024cf001c1f7'-or
    $o.targetId-cin$ids-or$o.targetGroupId-cne('KMC.RuntimeHostile.'+$Request.runId+':two-pair-target')){throw 'Native life fixture binding differs.'}
  }
 [Guid]$g=[Guid]::Empty;if(-not[Guid]::TryParse($ids[3],[ref]$g)-or-not[Guid]::TryParse($o.targetId,[ref]$g)){throw 'Native dynamic actor identity invalid.'}
 # 7E X1 death proper runs the same engine on the owner seed; its chain is read by its own branch.
 $death=Test-KmcChunk7NativeDeathCase $Request
 Assert-KmcChunk7Boolean $o.fullRealtimeCombatProof $false;Assert-KmcChunk7Boolean $o.naturalUnconsciousStimulusProven (-not$death)
 if($death){Assert-KmcChunk7Boolean $o.naturalDeathStimulusProven $true;if($null-eq$life.PSObject.Properties['outcome']-or$life.outcome-cne'death'){throw 'Native death evidence does not name its death outcome.'}}
 elseif($null-ne$o.PSObject.Properties['naturalDeathStimulusProven']-or$null-ne$life.PSObject.Properties['outcome']){throw 'Native incapacitation evidence claims a death outcome.'}
 Assert-KmcChunk7Boolean $life.sameCallRoundBoundaryLifeDeliveryProven $false;Assert-KmcChunk7Boolean $life.complete $true
 if($life.stimulus-cne$(if($death){'one labelled native RuleDealDamage past HP+Constitution; stock UnitLifeController tick kills natively and delivers existing lifecycle subscribers'}else{'one labelled native RuleDealDamage; stock UnitLifeController tick delivers existing lifecycle subscriber'})-or
    $life.subjectId-cne$ids[2]-or$life.survivorId-cne$ids[3]-or(ConvertTo-KmcChunk7Canonical $life.healthyActorIds)-cne(ConvertTo-KmcChunk7Canonical @($ids[0],$ids[1]))-or
    -not(Test-KmcExactJsonInteger $life.initialRound)-or$life.initialRound-lt0-or$life.damageRound-ne$life.initialRound+1){throw 'Native life scope, actor or actual N+1 round differs.'}
 if(-not$owned){if($o.acquisition.nativeRangerCommitCount-ne4-or$o.acquisition.horseFactRankAtCommit-ne1-or$o.acquisition.horsePresentAtNativeCommit-ne$true){throw 'Native Horse acquisition is incomplete.'}}
 Assert-KmcChunk7CheckpointTrace $o.allocationTrace $Request
 if(@($o.nativeCompletion.errors).Count-ne0-or$o.nativeCompletion.traceComplete-ne$true-or@($o.nativeRounds.errors).Count-ne0-or$o.nativeRounds.token-cne'06000BD3'-or$o.nativeRounds.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Native lifecycle context/round observation is incomplete.'}
 foreach($round in @($life.damageRound,($life.damageRound+1))){if(@($o.nativeRounds.events|Where-Object round -EQ $round).Count-ne1){throw 'Life continuation lacks an exact native round boundary.'}}
 $order=@($o.nativeInitialOrder);if(@($order|Select-Object -Unique).Count-ne$order.Count-or$o.targetId-cnotin$order){throw 'Life case has no exact unrelated native roster actor.'}
 foreach($i in @(0,2)){if([Array]::IndexOf($order,$ids[$i])-lt0-or[Array]::IndexOf($order,$ids[$i+1])-le[Array]::IndexOf($order,$ids[$i])){throw 'Life fixture is not actual rider-first order.'}}
 # 7E X1: the owner-seed-derived case starts from both pairs restored mounted by the cold load,
 # so it carries the exact mounted-start admission and no relationship input; the others mount natively.
 $derived=$owned-and(Test-KmcChunk7OwnedPairsSeedDerivedCombat $Request)
 if($derived){Assert-KmcChunk7OwnerSeedDerivedCombatStart $Request $o
  if(@($o.inputs|Where-Object{$_.label-cin@('mount-a','mount-b','r1-native-dismount-a','first-action-mount-a')}).Count-ne0-or$null-ne$o.PSObject.Properties['firstActionMount']){throw 'Owner-seed-derived native life admits no relationship input.'}
 }else{
 if($null-ne$o.PSObject.Properties['mountedStart']-or$null-ne$o.PSObject.Properties['ownerSeedDerivedSource']){throw 'Only the owner-seed-derived life case records a restored mounted start.'}
 $mounts=@($o.inputs|Where-Object{$_.label-cin@('mount-a','mount-b')});if($mounts.Count-ne2){throw 'Native life fixture lacks two native Mount inputs.'}
 for($i=0;$i-lt2;$i++){$m=$mounts[$i];if($m.casterId-cne$ids[2*$i]-or$m.targetId-cne$ids[2*$i+1]-or$m.abilityGuid-cne'f053faad986631688defa003cd7bda0e'-or$m.nativeCastDelta-ne1-or$m.nativeRefusalDelta-ne0-or$m.generationAtInit-lt0){throw 'Life native mount command binding differs.'};foreach($f in @('clicked','terminal','processTerminal')){Assert-KmcChunk7Boolean $m.$f $true}}
 }
 $commands=[Collections.Generic.HashSet[long]]::new()
 foreach($row in @(@($life.healthyAttackBefore,$ids[0],$ids[0]),@($life.affectedAttackBefore,$ids[3],$ids[2]),@($life.healthyAttackAfter,$ids[0],$ids[0]))){
  $cost=Assert-KmcChunk7CombatAttack $row[0] $o.allocationTrace $row[1] $row[2] $o.targetId $ids
  if(-not$commands.Add($cost)){throw 'Native life case reused actor expenditure.'}
 }
 if($death){if(-not$derived){throw 'Native death proper runs only on the owner-seed-derived mounted start.'};Assert-KmcChunk7NativeDeathChain $Request $o $life $ids;return}
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
 if($damageBefore.Count-ne1-or$damageAfter.Count-ne1-or$damageBefore[0].identity-eq0-or$damageBefore[0].identity-ne$damageAfter[0].identity-or
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
 $affectedBefore=@($cb.world.owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $ids[2]});$affectedAfter=@($ca.world.owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $ids[2]})
 if($affectedBefore.Count-ne1-or$affectedAfter.Count-ne1-or$affectedBefore[0].state-cne'Active'-or$affectedAfter[0].state-cne'Detached'-or
    $affectedBefore[0].id-cne$affectedAfter[0].id-or-not(Test-KmcChunk7OwnerActorMatch $affectedAfter[0].mountId $ids[3])){throw 'Actual native life cleanup lost detached actor custody.'}
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
 $slots=Get-KmcChunk7LifeRiderSlots $life.nativeNextRoundOrder $ids
 # Bind the recorded order to the native N+1 preparations actually observed for the three actors.
 $prepares=@{};foreach($e in @($o.allocationTrace.events|Where-Object{$null-ne$_.PSObject.Properties['round']-and$_.round-eq([long]$life.damageRound+1)-and$_.boundary-ceq'prepare-before'})){$a=[string]$e.state.actor;if(-not$prepares.ContainsKey($a)){$prepares[$a]=[long]$e.sequence}}
 foreach($pair in @(@($ids[0],$ids[3]),@($ids[2],$ids[3]))){if(-not$prepares.ContainsKey($pair[0])-or-not$prepares.ContainsKey($pair[1])-or$prepares[$pair[0]]-ge$prepares[$pair[1]]){throw 'Native N+1 preparations do not follow the recorded order.'}}
 if(($prepares[$ids[2]]-lt$prepares[$ids[0]])-ne([bool]$slots.beforeRenewal)){throw 'Native N+1 rider preparation does not follow the recorded order.'}
 if(-not(Test-KmcExactJsonInteger $life.riderSlotsBeforeRenewal)-or$life.riderSlotsBeforeRenewal-ne$slots.beforeRenewal-or
    -not(Test-KmcExactJsonInteger $life.expectedRiderSlots)-or$life.expectedRiderSlots-ne$slots.betweenRenewalAndSurvivor){throw 'Native next-round rider slots differ from the recorded native order.'}
 foreach($i in 0..3){$baseline=Get-KmcChunk7LifeGrant $before $ids[$i];$fresh=Get-KmcChunk7LifeGrant $renewed $ids[$i];$last=Get-KmcChunk7LifeGrant $independent $ids[$i]
  $freshExpected=if($i-lt2){1}elseif($i-eq2){$slots.beforeRenewal}else{0};$lastExpected=if($i-ne2){1}else{$slots.beforeRenewal+$slots.betweenRenewalAndSurvivor}
  if($fresh-ne$baseline+$freshExpected-or$last-ne$baseline+$lastExpected){throw 'Native next-round grants were duplicated, omitted or delayed.'}}
 $nextRound=[long]$life.damageRound+1;$lifeTrace=$o.allocationTrace;if($null-eq$lifeTrace.PSObject.Properties['forceToEnd']-or$lifeTrace.forceToEnd-isnot[Array]){throw 'Native life trace lacks its ForceToEnd observation.'}
 Assert-KmcChunk7UnconsciousNativeTurn $life.incapacitatedRiderTurn $lifeTrace $ids[2] $nextRound $true
 if($slots.beforeRenewal+$slots.betweenRenewalAndSurvivor-ne1){throw 'Rider-first native order must give the incapacitated rider exactly one native N+1 slot.'}
 $control=$life.unconsciousControl
 if($control.contract-cne'unmounted-unconscious-native-turn-control-v1'-or-not(Test-KmcExactJsonInteger $control.survivorSlot)-or$control.survivorSlot-ne$slots.survivorSlot){throw 'Unmounted unconscious control contract differs.'}
 $rows=@($control.candidates);$eligible=@()
 foreach($row in $rows){
  $healerRow=$null-ne$row.PSObject.Properties['healer']
  Assert-KmcExactProperties $row @(@('actorId','slot','mainCharacter','pet','essential','immortal','allowDyingCondition','conscious','inCombat','paired','eligible')+@(if($healerRow){'healer'})) 'unconscious control candidate'
  if($healerRow-and$row.healer-isnot[bool]){throw 'Unconscious control healer flag is mistyped.'}
  if($row.actorId-cin$ids-or-not(Test-KmcExactJsonInteger $row.slot)-or$row.slot-ne[Array]::IndexOf(@($life.nativeNextRoundOrder),$row.actorId)){throw 'Unconscious control candidate identity or slot differs.'}
  $gate=-not$row.mainCharacter-and-not$row.pet-and-not$row.essential-and-not$row.immortal-and$row.allowDyingCondition-and$row.conscious-and$row.inCombat-and-not$row.paired-and$row.slot-ge0-and$row.slot-lt$slots.survivorSlot-and-not($healerRow-and$row.healer)
  Assert-KmcChunk7Boolean $row.eligible $gate;if($gate){$eligible+=$row}
 }
 if($rows.Count-ne@($rows|ForEach-Object{$_.actorId}|Select-Object -Unique).Count){throw 'Unconscious control candidates duplicated.'}
 for($i=1;$i-lt$rows.Count;$i++){if($rows[$i].slot-lt$rows[$i-1].slot){throw 'Unconscious control candidates are not in native slot order.'}}
 Assert-KmcChunk7Boolean $control.available ($eligible.Count-gt0)
 # The control is required unless the request explicitly declares it not required.
 $controlRequired=$true;if($null-ne$Request.PSObject.Properties['nativeLifeControlRequired']){if($Request.nativeLifeControlRequired-isnot[bool]){throw 'Native life control requirement must be a boolean.'};$controlRequired=$Request.nativeLifeControlRequired}
 if($controlRequired-and-not$control.available){throw 'Required unmounted unconscious control is unavailable.'}
 if($control.available){
  $grantBound=$null-ne$control.PSObject.Properties['traceSequenceAtGrantAfter']
  Assert-KmcExactProperties $control @(@('contract','survivorSlot','candidates','available','actorId','slot','grantBefore','traceSequenceAtDamage','before','requestedDamage','nativeDamage','afterDamage','turn','grantAfter')+@(if($grantBound){'traceSequenceAtGrantAfter'})) 'unconscious control'
  # The prone fold (row 57) keeps the trace running past N+1: its control grant count must carry its own trace bound.
  if(($owned-and(Get-KmcChunk7OwnedPairsCase $Request)-ceq'owner-seed-native-life-cleanup')-and-not$grantBound){throw 'The owner-seed incapacitation control lacks its grant trace bound.'}
  $grantLimit=if($grantBound){if(-not(Test-KmcExactJsonInteger $control.traceSequenceAtGrantAfter)-or$control.traceSequenceAtGrantAfter-lt$control.traceSequenceAtDamage){throw 'Unconscious control grant trace bound is invalid.'};[long]$control.traceSequenceAtGrantAfter}else{[long]::MaxValue}
  if($control.actorId-cne$eligible[0].actorId-or$control.slot-ne$eligible[0].slot){throw 'Unconscious control is not the first eligible companion in native order.'}
  Assert-KmcChunk7LifeActor $control.before $control.actorId $true;Assert-KmcChunk7LifeActor $control.afterDamage $control.actorId $true
  foreach($f in @('mainCharacter','essential','immortal')){Assert-KmcChunk7Boolean $control.before.$f $false};Assert-KmcChunk7Boolean $control.before.allowDyingCondition $true
  if(-not(Test-KmcExactJsonInteger $control.requestedDamage)-or$control.requestedDamage-le0-or-not(Test-KmcExactJsonInteger $control.nativeDamage)-or$control.nativeDamage-le0-or
     $control.afterDamage.damage-ne$control.before.damage+$control.nativeDamage-($control.before.temporaryHp-$control.afterDamage.temporaryHp)-or$control.afterDamage.temporaryHp-gt$control.before.temporaryHp-or
     $control.afterDamage.damage-lt$control.before.hp+1-or$control.afterDamage.damage-ge$control.before.hp+$control.before.constitution){throw 'Unconscious control damage is not one bounded living stimulus.'}
  Assert-KmcChunk7UnconsciousNativeTurn $control.turn $lifeTrace $control.actorId $nextRound $false
  if((ConvertTo-KmcChunk7Canonical @($control.turn.sequence))-cne(ConvertTo-KmcChunk7Canonical @($life.incapacitatedRiderTurn.sequence))-or
     $control.turn.standardBefore-ne$life.incapacitatedRiderTurn.standardBefore-or$control.turn.standardAfterEnd-ne$life.incapacitatedRiderTurn.standardAfterEnd){throw 'Unmounted unconscious control differs from the incapacitated rider native turn.'}
  if(-not$prepares.ContainsKey($ids[3])-or$control.turn.prepareSequence-ge$prepares[$ids[3]]){throw 'Unconscious control native turn does not precede the surviving mount.'}
  foreach($f in @('grantBefore','grantAfter','traceSequenceAtDamage')){if(-not(Test-KmcExactJsonInteger $control.$f)-or$control.$f-lt0){throw 'Unconscious control grant record is invalid.'}}
  $controlPrepares=@($lifeTrace.events|Where-Object{$_.sequence-gt$control.traceSequenceAtDamage-and$_.sequence-le$grantLimit-and$_.boundary-ceq'prepare-before'-and$null-ne$_.state-and$_.state.actor-ceq$control.actorId}).Count
  if($control.grantAfter-ne$control.grantBefore+$controlPrepares-or$control.traceSequenceAtDamage-ge$control.turn.prepareSequence){throw 'Unconscious control grants differ from its observed native preparations.'}
 }else{foreach($f in @('actorId','slot','grantBefore','traceSequenceAtDamage','before','requestedDamage','nativeDamage','afterDamage','turn','grantAfter')){if($null-ne$control.PSObject.Properties[$f]){throw 'Unavailable unconscious control carries a stimulus.'}}}
 # 7E row 57 fold: the owner-seed incapacitation case carries the healed rider's native prone stand-up; no other case may.
 $proneCase=$owned-and(Get-KmcChunk7OwnedPairsCase $Request)-ceq'owner-seed-native-life-cleanup'
 $healerFlags=@($rows|Where-Object{$null-ne$_.PSObject.Properties['healer']})
 if($proneCase){Assert-KmcChunk7ProneStandUp $(if($null-ne$o.PSObject.Properties['proneStandUp']){$o.proneStandUp}else{$null}) $o $life $ids $control $rows}
 elseif($null-ne$o.PSObject.Properties['proneStandUp']-or$healerFlags.Count-ne0){throw 'Only the owner-seed incapacitation case carries the prone stand-up fold.'}
 foreach($id in @($ids[0],$ids[1])){$actor=Get-KmcChunk7CombatActor $renewed.world $id;if($actor.standard-ne0-or$actor.move-ne0){throw 'Healthy native renewal lacks fresh native expenditure.'}}
 $survivor=Get-KmcChunk7CombatActor $independent.world $ids[3];if($survivor.standard-ne0-or$survivor.move-ne0-or@($independent.world.owners|Where-Object{$_.state-ceq'Active'-and(Test-KmcChunk7OwnerActorMatch $_.mountId $ids[3])}).Count-ne0){throw 'Surviving mount lacks an independent native allocation.'}
 Assert-KmcChunk7LifeOwner $before.world $renewed.world $ids[0] $ids[1]
 foreach($window in @(@($life.healthyAttackBefore,$life.healthyEndedBeforeDamage),@($life.healthyAttackAfter,$life.healthyEndedAfterCleanup))){
  foreach($id in @($ids[0],$ids[1])){$end=@($o.nativeCompletion.calls|Where-Object{$_.kind-ceq'end'-and$_.complete-eq$true-and$_.before.actorId-ceq$id-and$_.before.frame-ge$window[0].before.frame-and$_.after.frame-le$window[1].world.frame})
   if($end.Count-ne1-or(Get-KmcChunk7CombatActor $window[1].world $id).standard-lt5.9){throw 'Healthy native actor contexts did not fully end before and after local cleanup.'}}
 }
 if($owned){foreach($field in @('pairResidueAbsent','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$field $true};Assert-KmcChunk7Boolean $o.cleanup.ownedHorseRemoved $false;Assert-KmcChunk7Boolean $o.cleanup.nativeProgressionRequiresExternalWorkingRestoration $false}else{foreach($field in @('pairResidueAbsent','ownedHorseRemoved','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}}
 Assert-KmcChunk7Boolean $o.cleanup.nativeSaveWrittenByThisDriver $false;Assert-KmcChunk7Boolean $o.cleanup.delegatedPersistenceCleanup $false
}
# ---- 7E row 57 fold: C11 prone stand-up attack of opportunity (owner-seed-native-life-cleanup) -------
# From round N+2: a party actor outside both pairs that uses the scroll without Use Magic Device heals the
# unconscious rider B with one native Cure Light Wounds scroll click (at most two attempts, each on its own
# turn, one in-place spend each); then, on rider B's own native turn, one ordinary native ground order is its
# action attempt: the native stand-up spends its Move and the threatening target makes exactly one native
# attack of opportunity. No refund, no other actor's spend, no pair command authority.
function Assert-KmcChunk7ProneStandUp($P,$o,$life,$ids,$control,$controlRows){
 if($null-eq$P-or$P.contract-cne'owner-seed-prone-stand-up-v1'-or$P.riderId-cne$ids[2]-or$P.healAttemptsAllowed-ne2){throw 'The owner-seed incapacitation case lacks its prone stand-up measurement.'}
 Assert-KmcChunk7Boolean $P.complete $true
 $rows=@($P.healerCandidates);$eligible=@()
 foreach($r in $rows){
  Assert-KmcExactProperties $r @('actorId','pet','conscious','inGame','controllable','needsUseMagicDevice','commandsEmpty','inCombat','controlStaticallyIneligible','eligible') 'prone healer candidate'
  if($r.actorId-cin$ids){throw 'A pair actor was offered as the prone healer.'}
  foreach($f in @('pet','conscious','inGame','controllable','needsUseMagicDevice','commandsEmpty','inCombat','controlStaticallyIneligible','eligible')){if($r.$f-isnot[bool]){throw 'Prone healer candidate flag is mistyped.'}}
  $g=-not$r.pet-and$r.conscious-and$r.inGame-and$r.controllable-and-not$r.needsUseMagicDevice-and$r.commandsEmpty-and-not$r.inCombat
  if($r.eligible-ne$g){throw 'Prone healer eligibility differs from its recorded native facts.'};if($r.eligible){$eligible+=$r}
 }
 # Candidates that can never be the unconscious control come first (so the healer does not take its place).
 for($i=1;$i-lt$rows.Count;$i++){if($rows[$i].controlStaticallyIneligible-and-not$rows[$i-1].controlStaticallyIneligible){throw 'Prone healer candidates are not ordered control-ineligible first.'}}
 if($eligible.Count-lt1-or$P.healerId-cne$eligible[0].actorId){throw 'The prone healer is not the first eligible party actor.'}
 $hf=$P.healerAtFold;if($null-eq$hf-or$hf.inCombat-ne$true-or$hf.conscious-ne$true-or$hf.canAct-ne$true){throw 'The prone healer was not a conscious acting combatant at the fold.'}
 $hr=@($controlRows|Where-Object actorId -CEQ $P.healerId)
 foreach($r in @($controlRows)){if($null-eq$r.PSObject.Properties['healer']-or$r.healer-ne($r.actorId-ceq$P.healerId)){throw 'Unconscious control candidates do not mark the prone healer exactly.'}}
 if($hr.Count-gt1-or($hr.Count-eq1-and$hr[0].eligible)-or($control.available-and$control.actorId-ceq$P.healerId)){throw 'The prone healer was used as the unconscious control.'}
 $s=$P.healerScroll
 if($null-eq$s-or$s.casterId-cne$P.healerId-or$s.scrollUnits-ne4-or$s.scrollBlueprint-cne'cd635d5720937b044a354dba17abad8d'-or$s.scrollAbility.blueprint-cne'5590652e1c2225c4ca30c4a699ab3649'-or$s.scrollAbility.caster-cne$P.healerId){throw 'The prone heal does not use the healer''s one disposable native scroll stack.'}
 if($null-eq$s.scroll-or$s.scroll.ownedItemReleased-ne$true-or$s.scroll.disposed-ne$true-or$null-eq$s.castTrace-or$s.castTrace.dropped-ne0-or$s.castTrace.faults-ne0-or$null-eq$s.originalSlots){throw 'The healer''s scroll stack was not released or its cast observer was incomplete.'}
 $attempts=@($P.healAttempts);if($attempts.Count-lt1-or$attempts.Count-gt2){throw 'The prone heal took other than one or two native attempts.'}
 $allRules=@();$item=$null;$lastRound=-1
 for($i=0;$i-lt$attempts.Count;$i++){$a=$attempts[$i];$last=$i-eq$attempts.Count-1
  if(-not(Test-KmcExactJsonInteger $a.round)-or$a.round-lt[long]$life.damageRound+2-or$a.round-le$lastRound){throw 'A prone heal attempt ran before round N+2 or twice in one round.'};$lastRound=$a.round
  Assert-KmcChunk7Boolean $a.clicked $true;Assert-KmcChunk7Boolean $a.selected $true;Assert-KmcChunk7Boolean $a.canTarget $true
  if($a.admittedShellCount-ne1-or$a.target-cne$ids[2]-or$a.ability.blueprint-cne'5590652e1c2225c4ca30c4a699ab3649'-or$a.ability.caster-cne$P.healerId-or$a.ability.sourceItemBlueprint-cne'cd635d5720937b044a354dba17abad8d'){throw 'A prone heal attempt is not one native scroll click by the healer on rider B.'}
  if($null-eq$item){$item=$a.ability.sourceItem}elseif($a.ability.sourceItem-ne$item){throw 'Prone heal attempts used different stacks.'}
  if($a.riderBefore.life.conscious-ne$false-or$a.riderBefore.life.dead-ne$false){throw 'A prone heal attempt did not start on the unconscious living rider.'}
  # Each attempt is the healer's own declared command to the non-pair AI lease, sequentially (b206: an undeclared command broke the lease).
  if($null-eq$a.PSObject.Properties['declaredLeaseCommands']-or-not(Test-KmcExactJsonInteger $a.declaredLeaseCommands)-or$a.declaredLeaseCommands-ne$i+1){throw 'A prone heal attempt was not the healer''s next sequential declared lease command.'}
  $ev=@($a.castEvents);$sp=@($ev|Where-Object{$_.kind-ceq'item-spend-after'-and$_.actor-ceq$P.healerId-and$_.identity-eq$item})
  if($sp.Count-ne1-or$sp[0].result-ne$true-or$a.itemsBefore.exactSlot-ne$true-or$a.itemsAfter.exactSlot-ne$true-or$a.itemsBefore.count-lt2-or$a.itemsAfter.count-ne$a.itemsBefore.count-1){throw 'A prone heal attempt did not spend the exact stack exactly once in place.'}
  if($a.riderConsciousAfter-isnot[bool]-or$a.riderConsciousAfter-ne$last-or$a.riderAfter.life.conscious-ne$last-or$a.riderAfter.life.dead-ne$false){throw 'Prone heal attempts do not end exactly when rider B is conscious.'}
  if($last-and@($ev|Where-Object{$_.kind-ceq'heal'-and$_.target-ceq$ids[2]-and$_.value-gt0}).Count-lt1){throw 'The restoring heal shows no native heal on rider B.'}
  if($a.riderAfter.life.damage-gt$a.riderBefore.life.damage){throw 'A heal attempt damaged rider B.'}
  $rules=@($a.rules);foreach($e in $rules){if($e.opportunity-ne$true-or$e.actor-cne$o.targetId-or$e.target-cne$P.healerId){throw 'A prone heal attempt drew an attack other than the target''s native attack of opportunity on the healer.'}}
  if($rules.Count-gt1){throw 'A prone heal attempt drew more than one attack.'};$allRules+=$rules
 }
 if($P.healedRound-ne$attempts[-1].round){throw 'The recorded heal round differs from the restoring attempt.'}
 $st=$P.standUp
 if($null-eq$st-or-not(Test-KmcExactJsonInteger $st.round)-or$st.round-lt$P.healedRound-or$st.round-gt$P.healedRound+1){throw 'Rider B''s stand-up is not on its own native turn after the heal.'}
 $r=$st.rider
 if($r.life.conscious-ne$true-or$r.prone-ne$true-or$r.engagedByTarget-ne$true-or$r.remembersTarget-ne$true-or$r.pairState-cne'Unmounted'-or$r.registryActive-ne$false-or$r.pairCommandA-ne$false-or$r.pairCommandB-ne$false){throw 'Rider B was not a conscious, prone, threatened, unpaired native actor before its stand-up.'}
 $g=$st.gates
 if($g.reactions-ne1-or$g.canAttackOfOpportunity-ne$true-or$g.canActInCombat-ne$true-or$g.waitingInitiative-ne$false-or$g.surprised-ne$false-or$g.canAct-ne$true-or$g.threatHand-ne$true-or$g.engagesSubject-ne$true-or$g.subjectRemembersTarget-ne$true-or$g.prone-ne$false-or$g.conscious-ne$true){throw 'The target did not hold one available native attack of opportunity threatening rider B.'}
 Assert-KmcChunk7Boolean $st.clicked $true;Assert-KmcChunk7Boolean $st.fiveFootStep $false;Assert-KmcChunk7Boolean $st.singleActionMove $false
 $sr=@($st.rules)
 if($sr.Count-ne1-or$sr[0].kind-cne'attack'-or$sr[0].opportunity-ne$true-or$sr[0].actor-cne$o.targetId-or$sr[0].target-cne$ids[2]-or$sr[0].ranged-ne$false-or$sr[0].round-ne$st.round){throw 'The native stand-up did not draw exactly one attack of opportunity from the target on rider B.'}
 if($st.targetAfter.reactions-ne0){throw 'The target''s attack-of-opportunity count did not drop to zero.'}
 $re=$st.reIncapacitatedByAttack;if($re-isnot[bool]){throw 'The stand-up record lacks its re-incapacitation flag.'}
 if($st.riderAfter.life.damage-lt$r.life.damage-or($re-and$st.riderAfter.life.damage-le$r.life.damage)-or(-not$re-and$st.riderAfter.prone-ne$false)-or$st.riderAfter.life.dead-ne$false){throw 'Rider B did not stand up natively, was healed by the attack, or died.'}
 $rb=Get-KmcChunk7CombatActor $st.before $ids[2];$ra=Get-KmcChunk7CombatActor $st.after $ids[2]
 if([double]$ra.move-le[double]$rb.move){throw 'The native stand-up did not spend rider B''s Move.'}
 foreach($f in @('standard','swift')){if([double]$ra.$f-gt[double]$rb.$f+0.001){throw 'Rider B spent another action on its stand-up.'}}
 Assert-KmcChunk7NeverSpentCombatBudget $st.before $st.after @($ids[0],$ids[1],$ids[3])
 if($st.riderAfter.pairState-cne'Unmounted'-or$st.riderAfter.registryActive-ne$false-or$st.riderAfter.pairCommandA-ne$false-or$st.riderAfter.pairCommandB-ne$false){throw 'The stand-up created pair command authority or re-paired rider B.'}
 $cap=$P.rules
 if($null-eq$cap-or$cap.dropped-ne0-or@($cap.errors).Count-ne0){throw 'Prone stand-up rule observation dropped or failed.'}
 if((ConvertTo-Json -InputObject @($cap.events) -Depth 10 -Compress)-cne(ConvertTo-Json -InputObject @(@($allRules)+@($sr)) -Depth 10 -Compress)){throw 'The prone rule observer recorded attacks outside its windows.'}
}
# ---- 7E X1 death proper (owner-seed-native-death-cleanup) ---------------------------------------
# A dead actor record: life label Dead, dead, not conscious; the extended record adds finallyDead,
# inCombat and inState as observed.
function Assert-KmcChunk7DeadLifeActor($Actor,[string]$Id,[switch]$Extended){
 if($Actor.actorId-cne$Id){throw 'Native death actor identity differs.'}
 foreach($f in @('damage','hp','constitution','temporaryHp')){if(-not(Test-KmcExactJsonInteger $Actor.$f)-or$Actor.$f-lt0){throw 'Native death stat observation is invalid.'}}
 Assert-KmcChunk7Boolean $Actor.conscious $false;Assert-KmcChunk7Boolean $Actor.dead $true
 if($Actor.life-cne'Dead'-or$Actor.damage-lt$Actor.hp+$Actor.constitution){throw 'Native death state label or threshold differs.'}
 if($Extended){foreach($f in @('finallyDead','inCombat','inState')){if($Actor.$f-isnot[bool]){throw 'Native death actor flag is missing or mistyped.'}};Assert-KmcChunk7Boolean $Actor.inCombat $false}
}
# Native death detaches through the stock RemoveUnit inside SetLifeState (before OnUnitDeath): the paired
# allocation is retired, only the surviving mount's pending exclusion for the damage round remains, and no
# split release round exists. Every condition is recorded separately and recomputed from its own values.
function Assert-KmcChunk7DeathDetach($d,$life,$ids,$before,$trace){
 if($null-eq$d){throw 'Native death detach conditions were not recorded.'}
 Assert-KmcExactProperties $d @('contract','observedRound','damageRound','pairState','activationIdentity','activationSplit','splitReleaseRound','participationRecorded','participationRiderId','participationMountId','participationActivation','pendingSplitMountId','pendingSplitRound','partnerContextActor','noPreparedPairResidue','grantsBefore','grantsAfter','conditions') 'native death detach'
 $names=@('observedInDamageRound','pairUnmounted','activationRetiredByRemoval','noSplitReleaseRound','survivorPendingForDamageRound','partnerContextReleased','noPreparedPairResidue','grantsUnchanged')
 Assert-KmcExactProperties $d.conditions $names 'native death detach conditions'
 foreach($n in $names){if($d.conditions.$n-isnot[bool]){throw "Native death detach condition $n is missing or mistyped."};if(-not$d.conditions.$n){throw "Native death detach condition $n failed."}}
 if($d.contract-cne'native-death-actor-removal-detach-v1'-or-not(Test-KmcExactJsonInteger $d.observedRound)-or$d.observedRound-ne$life.damageRound-or$d.damageRound-ne$life.damageRound){throw 'Native death detach was not observed in the damage round.'}
 foreach($f in @('activationSplit','participationRecorded','participationActivation','noPreparedPairResidue')){if($d.$f-isnot[bool]){throw "Native death detach $f is missing or mistyped."}}
 foreach($f in @('damageRound','splitReleaseRound','pendingSplitRound')){if(-not(Test-KmcExactJsonInteger $d.$f)){throw "Native death detach $f is missing or mistyped."}}
 if($d.pairState-cne'Unmounted'-or$null-ne$d.activationIdentity-or$d.activationSplit-or$d.splitReleaseRound-ne-1-or-not$d.participationRecorded-or$null-ne$d.participationRiderId-or
    $null-ne$d.participationMountId-or$d.participationActivation-or$d.pendingSplitMountId-cne$ids[3]-or$d.pendingSplitRound-ne$life.damageRound-or$null-ne$d.partnerContextActor-or-not$d.noPreparedPairResidue){throw 'Native death detach is not the native actor-removal shape (retired activation, pending survivor for the damage round, no split release).'}
 $g0=@($d.grantsBefore);$g1=@($d.grantsAfter)
 if($g0.Count-ne4-or$g1.Count-ne4){throw 'Native death detach grant observation is incomplete.'}
 for($i=0;$i-lt4;$i++){if(-not(Test-KmcExactJsonInteger $g0[$i])-or$g0[$i]-ne$g1[$i]-or$g0[$i]-ne(Get-KmcChunk7LifeGrant $before $ids[$i])){throw 'Native death detach changed or misrecorded actor grant counts.'}}
 if(@($trace.events|Where-Object{$_.sequence-gt$life.traceSequenceBeforeDamage-and$_.sequence-le$life.traceSequenceAfterDelivery-and$_.boundary-ceq'remove-unit-before'-and$null-ne$_.state-and$_.state.actor-ceq$ids[2]-and$_.round-eq$life.damageRound}).Count-lt1){throw 'Native death detach lacks the native RemoveUnit of the dead rider.'}
}
# The smallest whole native request whose difficulty-scaled damage reaches HP+Constitution+1.
# The producer divides by the native float widened to double; the JSON value is that float's shortest form.
function Get-KmcChunk7DeathRequest($Actor,$Difficulty){[long][Math]::Ceiling(([double]($Actor.hp+$Actor.constitution-$Actor.damage+$Actor.temporaryHp)+1)/[double][single]$Difficulty)}
# A dead unit's N+1 on the trace's own hooks: its trace events in that round up to the capture's own
# trace position are recounted exactly; there must be no native Prepare, end, forced end, admission,
# cost or command. Native Prepare, end and forced end are also refused over the whole round.
function Get-KmcChunk7DeadNativeTurn($Trace,[string]$Actor,[long]$Round,[long]$Bound=[long]::MaxValue){
 $events=@($Trace.events|Where-Object{$null-ne$_.PSObject.Properties['round']-and$_.round-eq$Round-and$null-ne$_.PSObject.Properties['state']-and$null-ne$_.state-and$_.state.actor-ceq$Actor-and$_.sequence-le$Bound})
 $forbidden=@($events|Where-Object{$b=[string]$_.boundary;@('admission-','cost-','actor-cost-','command-','completion-force-to-end-','chunk7-end-input-')|Where-Object{$b.StartsWith($_,[StringComparison]::Ordinal)}}).Count
 $forceList=@(if($null-ne$Trace.PSObject.Properties['forceToEnd']){$Trace.forceToEnd})
 [pscustomobject]@{events=[long]$events.Count;prepareBefore=[long]@($events|Where-Object boundary -CEQ 'prepare-before').Count;prepareAfter=[long]@($events|Where-Object boundary -CEQ 'prepare-after').Count
  ends=[long]@($events|Where-Object boundary -CEQ 'turn-end-before').Count;forcedEnds=[long]@($forceList|Where-Object{$null-ne$_-and$_.actor-ceq$Actor-and$_.round-eq$Round}).Count;forbidden=[long]$forbidden}
}
function Assert-KmcChunk7DeadNativeTurn($Recorded,$Trace,[string]$Actor,[long]$Round){
 Assert-KmcExactProperties $Recorded @('actorId','round','traceSequenceAtCapture','events','prepareBefore','prepareAfter','ends','forcedEnds','forbidden','conscious','dead','finallyDead','inCombat') 'dead native turn'
 if($Recorded.actorId-cne$Actor-or$Recorded.round-ne$Round){throw 'Dead native turn identity or round differs.'}
 $last=[long](@($Trace.events|ForEach-Object{[long]$_.sequence})|Measure-Object -Maximum).Maximum
 if(-not(Test-KmcExactJsonInteger $Recorded.traceSequenceAtCapture)-or$Recorded.traceSequenceAtCapture-lt1-or$Recorded.traceSequenceAtCapture-gt$last){throw 'Dead native turn capture position is outside its trace.'}
 $d=Get-KmcChunk7DeadNativeTurn $Trace $Actor $Round ([long]$Recorded.traceSequenceAtCapture);$whole=Get-KmcChunk7DeadNativeTurn $Trace $Actor $Round
 if($whole.prepareBefore-ne0-or$whole.prepareAfter-ne0-or$whole.ends-ne0-or$whole.forcedEnds-ne0){throw 'Dead unit received native next-round activity.'}
 foreach($f in @('events','prepareBefore','prepareAfter','ends','forcedEnds','forbidden')){if(-not(Test-KmcExactJsonInteger $Recorded.$f)-or$Recorded.$f-ne$d.$f){throw "Dead native turn $f differs from its trace."}}
 Assert-KmcChunk7Boolean $Recorded.conscious $false;Assert-KmcChunk7Boolean $Recorded.dead $true;Assert-KmcChunk7Boolean $Recorded.inCombat $false
 if($Recorded.finallyDead-isnot[bool]){throw 'Dead native turn finallyDead is missing or mistyped.'}
 if($Recorded.prepareBefore-ne0-or$Recorded.prepareAfter-ne0-or$Recorded.ends-ne0-or$Recorded.forcedEnds-ne0-or$Recorded.forbidden-ne0){throw 'Dead unit received native next-round activity.'}
}
# The death chain after the shared fixture, start and attack checks of Assert-KmcChunk7NativeLifeEvidence.
function Assert-KmcChunk7NativeDeathChain($Request,$o,$life,$ids){
 $before=$life.beforeDamage;$after=$life.afterDamage;$delivery=$life.afterDelivery;$probe=$life.deliveryProbe
 Assert-KmcChunk7LifeActor $before.rider $ids[2] $true;Assert-KmcChunk7LifeActor $after.rider $ids[2] $true
 foreach($f in @('mainCharacter','essential','immortal')){Assert-KmcChunk7Boolean $before.rider.$f $false}
 foreach($name in @('nativeDeathPolicy','nativeDeathPolicyAtDamage')){$p=$life.$name;Assert-KmcExactProperties $p @('trueDeath','deathDoorCondition','damageToParty') 'native death policy readout'
  if($p.trueDeath-isnot[bool]-or$p.deathDoorCondition-isnot[bool]-or-not(Test-KmcChunk7FiniteNumber $p.damageToParty)-or$p.damageToParty-ne$life.difficulty){throw 'Native death policy readout is invalid or differs from the stimulus difficulty.'}}
 if((ConvertTo-KmcChunk7Canonical $life.nativeDeathPolicy)-cne(ConvertTo-KmcChunk7Canonical $life.nativeDeathPolicyAtDamage)){throw 'Native death policy changed before the stimulus.'}
 if($life.nativeDamageDispatches-ne1-or$life.sourceId-cne$o.targetId-or$life.nativeDamageBeforeDifficulty-ne$life.requestedDamage-or$life.nativeDamage-le0-or
    -not(Test-KmcExactJsonInteger $life.requestedDamage)-or$life.requestedDamage-le0-or-not(Test-KmcChunk7FiniteNumber $life.difficulty)-or$life.difficulty-le0-or
    $life.deathThreshold-ne$before.rider.hp+$before.rider.constitution-or$after.rider.damage-lt$life.deathThreshold-or
    $life.requestedDamage-ne(Get-KmcChunk7DeathRequest $before.rider ([double]$life.difficulty))-or
    $after.world.frame-ne$before.world.frame-or$before.world.round-ne$life.damageRound-or$delivery.world.round-ne$life.damageRound){throw 'Native damage is not one bounded death stimulus.'}
 foreach($f in @('hp','constitution')){if($before.rider.$f-ne$after.rider.$f){throw 'Native damage fixture changed life policy stats.'}}
 Assert-KmcChunk7SameCombatBudget $before.world $after.world $ids
 foreach($name in @('mount','healthyRider','healthyMount')){if((ConvertTo-KmcChunk7Canonical $before.$name)-cne(ConvertTo-KmcChunk7Canonical $after.$name)-or(ConvertTo-KmcChunk7Canonical $before.$name)-cne(ConvertTo-KmcChunk7Canonical $delivery.$name)){throw 'Targeted native damage changed another actor life state.'}}
 if($life.nativeEffects.dropped-ne0){throw 'Native damage rule observations were dropped.'}
 $damageBefore=@($life.nativeEffects.events|Where-Object{$_.kind-ceq'damage-before'-and$_.actor-ceq$o.targetId-and$_.target-ceq$ids[2]})
 $damageAfter=@($life.nativeEffects.events|Where-Object{$_.kind-ceq'damage-after'-and$_.actor-ceq$o.targetId-and$_.target-ceq$ids[2]})
 if($damageBefore.Count-ne1-or$damageAfter.Count-ne1-or$damageBefore[0].identity-eq0-or$damageBefore[0].identity-ne$damageAfter[0].identity-or
    $damageBefore[0].targetDamage-ne$before.rider.damage-or$damageAfter[0].targetDamage-ne$after.rider.damage-or$damageAfter[0].damage-ne$life.nativeDamage-or$damageAfter[0].beforeDifficulty-ne$life.requestedDamage){throw 'Death stimulus lacks one exact native damage rule observation.'}
 # The exact native chain: Tick -> SetLifeState(Dead) -> OnUnitDeath -> HandleUnitDeath, then the life-state event.
 if($probe.actorId-cne$ids[2]-or$probe.nativeModuleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-or$probe.nativeTickToken-cne'06009162'-or$probe.nativeSetToken-cne'06009164'-or
    $probe.callbackMvid-cne$Request.dllMvid-or$probe.callbackToken-cnotmatch'^06[0-9A-F]{6}$'-or$probe.callbackType-cne'KingmakerMountedCombat.Integration.MountedLifecycleSubscriber'-or$probe.callbackName-cne'HandleUnitLifeStateChanged'-or
    $probe.deathCallbackMvid-cne$Request.dllMvid-or$probe.deathCallbackToken-cnotmatch'^06[0-9A-F]{6}$'-or$probe.deathCallbackToken-ceq$probe.callbackToken-or
    $probe.deathCallbackType-cne'KingmakerMountedCombat.Integration.MountedLifecycleSubscriber'-or$probe.deathCallbackName-cne'HandleUnitDeath'-or
    $probe.tickDepth-ne0-or$probe.setDepth-ne0-or@($probe.errors).Count-ne0){throw 'Exact native death observer identity or closure differs.'}
 Assert-KmcChunk7Boolean $probe.completedNativeTick $true
 if($null-ne$life.PSObject.Properties['notDeadAfterNativeTick']){throw 'Native tick completed without killing the rider.'}
 $events=@($probe.events);$sequence=0
 foreach($e in $events){if($e.sequence-ne(++$sequence)-or$e.actorId-cne$ids[2]-or$e.round-ne$life.damageRound-or$e.frame-lt$after.world.frame){throw 'Death observer sequence/actor/round differs.'}}
 $names=@('native-tick-before','native-set-before','production-death-callback-before','production-death-callback-after','production-callback-before','production-callback-after','native-set-after','native-tick-after')
 if((ConvertTo-KmcChunk7Canonical @($events.boundary))-cne(ConvertTo-KmcChunk7Canonical $names)){throw 'No exact native Tick to SetLifeState to death and life callback chain.'}
 for($i=0;$i-lt8;$i++){if($events[$i].nativeTickDepth-ne1-or$events[$i].nativeSetDepth-ne$(if($i-in@(0,7)){0}else{1})-or$events[$i].frame-ne$events[0].frame-or($i-ge1-and$events[$i].nativeSetSequence-ne$events[1].nativeSetSequence)){throw 'Death callback was direct, reordered or outside the same native tick.'}}
 foreach($i in 2..5){if($events[$i].currentLife-cne'Dead'){throw 'Death callback did not observe the actual native Dead state.'}}
 if($null-ne$events[2].previousLife-or$null-ne$events[3].previousLife-or$events[4].previousLife-cne'Conscious'-or$events[5].previousLife-cne'Conscious'){throw 'Death callbacks did not report the actual native transition.'}
 $db=$events[2];$da=$events[3];$lb=$events[4];$la=$events[5]
 # The whole tick, including native LeaveCombat before the callbacks, keeps the healthy pair's owner and expenditure.
 Assert-KmcChunk7LifeOwner $events[0].world $events[7].world $ids[0] $ids[1];Assert-KmcChunk7SameCombatBudget $events[0].world $events[7].world @($ids[0],$ids[1])
 $forcedRoundN=@(@($o.allocationTrace.forceToEnd)|Where-Object{$null-ne$_-and$_.actor-ceq$ids[2]-and$_.round-eq$life.damageRound}).Count
 if(-not(Test-KmcExactJsonInteger $life.riderDamageRoundForceToEnd)-or$life.riderDamageRoundForceToEnd-gt$forcedRoundN){throw 'Recorded damage-round ForceToEnd of the rider differs from its trace.'}
 foreach($w in @(@($db,$da),@($lb,$la))){Assert-KmcChunk7LifeOwner $w[0].world $w[1].world $ids[0] $ids[1];Assert-KmcChunk7SameCombatBudget $w[0].world $w[1].world @($ids[0],$ids[1],$ids[3])}
 $owners=@(foreach($e in @($db,$da,$lb,$la)){,@($e.world.owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $ids[2]})})
 if(@($owners|Where-Object{@($_).Count-ne1}).Count-ne0){throw 'Actual native death cleanup lost detached actor custody.'}
 $o1=@($owners[0])[0];$o2=@($owners[1])[0];$o3=@($owners[2])[0];$o4=@($owners[3])[0]
 if($o1.state-cne'Active'-or$o2.state-cne'Detached'-or$o3.state-cne'Detached'-or$o4.state-cne'Detached'-or$o2.id-cne$o1.id-or$o3.id-cne$o1.id-or$o4.id-cne$o1.id-or
    -not(Test-KmcChunk7OwnerActorMatch $o2.mountId $ids[3])){throw 'Actual native death cleanup lost detached actor custody.'}
 Assert-KmcChunk7Boolean $o2.retainsObligations $true
 # Exactly one cleanup from Mounted, the native death delivery inside the death callback; every other
 # delivery leaves the state unchanged and any repeated cleanup is an idempotent success from Unmounted.
 $deliveries=@($life.deliveries);$expected=[long]$life.ledgerOffset
 foreach($d in $deliveries){$expected++;Assert-KmcExactProperties $d @('sequence','boundary','source','stateBefore','stateAfter','trigger','attempted','succeeded','errors') 'native death delivery'
  if(-not(Test-KmcExactJsonInteger $d.sequence)-or$d.sequence-ne$expected-or$d.attempted-isnot[bool]-or$d.succeeded-isnot[bool]){throw 'Native death deliveries are not the consecutive window after the offset.'}}
 $cleanups=@($deliveries|Where-Object{$_.attempted-eq$true-and$_.stateBefore-ceq'Mounted'})
 if($cleanups.Count-ne1-or$cleanups[0].boundary-cne'UnitDeath'-or$cleanups[0].source-cne'IUnitHandler.HandleUnitDeath'-or$cleanups[0].trigger-cne'Death'-or$cleanups[0].stateAfter-cne'Unmounted'-or
    $cleanups[0].succeeded-ne$true-or@($cleanups[0].errors).Count-ne0-or$db.ledgerSequence-ge$cleanups[0].sequence-or$da.ledgerSequence-lt$cleanups[0].sequence){throw 'Native lifecycle ledger does not bind one successful death cleanup inside the native death callback.'}
 foreach($d in $deliveries){if([object]::ReferenceEquals($d,$cleanups[0])){continue}
  if($d.stateBefore-cne$d.stateAfter-or($d.attempted-and($d.stateBefore-cne'Unmounted'-or$d.succeeded-ne$true-or@($d.errors).Count-ne0))){throw 'A repeated native death delivery was not an idempotent success from the unmounted state.'}}
 $changes=@($life.lifeTransitions)
 if($changes.Count-ne1-or$changes[0].sequence-le$life.lifeTransitionOffset-or$changes[0].actorId-cne$ids[2]-or$changes[0].previousLife-cne'Conscious'-or$changes[0].currentLife-cne'Dead'){throw 'Native life transition is not the one exact rider death.'}
 Assert-KmcChunk7DeathDetach $(if($null-ne$life.PSObject.Properties['deathDetach']){$life.deathDetach}else{$null}) $life $ids $before $o.allocationTrace
 if($delivery.pairState-cne'Unmounted'-or$delivery.splitReleaseRound-ne-1-or$null-ne$delivery.partnerContextActor-or$null-ne$delivery.activationIdentity){throw 'Death detach changed release round or exact native context custody.'}
 Assert-KmcChunk7Boolean $delivery.activationSplit $false;Assert-KmcChunk7Boolean $delivery.presentationResidue $false
 Assert-KmcChunk7DeadLifeActor $delivery.rider $ids[2];Assert-KmcChunk7DeadLifeActor $life.deadRiderAfterDelivery $ids[2] -Extended
 if($life.traceSequenceAfterDelivery-lt$life.traceSequenceBeforeDamage-or@($o.allocationTrace.events|Where-Object{$_.sequence-gt$life.traceSequenceBeforeDamage-and$_.sequence-le$life.traceSequenceAfterDelivery-and$_.boundary-ceq'prepare-before'-and$_.state.actor-cin$ids}).Count-ne0){throw 'Death delivery generated an extra native preparation.'}
 foreach($id in $ids){if((Get-KmcChunk7LifeGrant $before $id)-ne(Get-KmcChunk7LifeGrant $delivery $id)){throw 'Native death cleanup changed actor grant counts.'}}
 # N+1: the healthy pair renews, the dead rider has no native slot, the surviving mount gets one fresh turn.
 $renewed=$life.healthyRenewed;$independent=$life.independentSurvivor
 if($renewed.world.round-ne$life.damageRound+1-or$renewed.world.currentActor-cne$ids[0]-or$independent.world.round-ne$life.damageRound+1-or$independent.world.currentActor-cne$ids[3]-or$null-ne$independent.partnerContextActor){throw 'Native healthy/survivor continuation missed original next round.'}
 Assert-KmcChunk7DeadLifeActor $independent.rider $ids[2]
 $slots=Get-KmcChunk7LifeRiderSlots $life.nativeNextRoundOrder $ids
 if(-not(Test-KmcExactJsonInteger $life.riderSlotsBeforeRenewal)-or$life.riderSlotsBeforeRenewal-ne0-or-not(Test-KmcExactJsonInteger $life.expectedRiderSlots)-or$life.expectedRiderSlots-ne0){throw 'A dead rider must have no native next-round slot.'}
 $nextRound=[long]$life.damageRound+1
 $prepares=@{};foreach($e in @($o.allocationTrace.events|Where-Object{$null-ne$_.PSObject.Properties['round']-and$_.round-eq$nextRound-and$_.boundary-ceq'prepare-before'})){$a=[string]$e.state.actor;if(-not$prepares.ContainsKey($a)){$prepares[$a]=[long]$e.sequence}}
 if(-not$prepares.ContainsKey($ids[0])-or-not$prepares.ContainsKey($ids[3])-or$prepares[$ids[0]]-ge$prepares[$ids[3]]-or$prepares.ContainsKey($ids[2])){throw 'Native N+1 preparations do not follow the recorded order or prepare the dead rider.'}
 foreach($i in 0..3){$baseline=Get-KmcChunk7LifeGrant $before $ids[$i];$fresh=Get-KmcChunk7LifeGrant $renewed $ids[$i];$last=Get-KmcChunk7LifeGrant $independent $ids[$i]
  $freshExpected=if($i-lt2){1}else{0};$lastExpected=if($i-ne2){1}else{0}
  if($fresh-ne$baseline+$freshExpected-or$last-ne$baseline+$lastExpected){throw 'Native next-round grants were duplicated, omitted or delayed.'}}
 $lifeTrace=$o.allocationTrace;if($null-eq$lifeTrace.PSObject.Properties['forceToEnd']-or$lifeTrace.forceToEnd-isnot[Array]){throw 'Native death trace lacks its ForceToEnd observation.'}
 Assert-KmcChunk7DeadNativeTurn $life.deadRiderTurn $lifeTrace $ids[2] $nextRound
 # Unmounted control: an unpaired mortal companion killed by the same native route behaves identically.
 $control=$life.deathControl
 $controlOrder=Get-KmcChunk7DeathControlOrder $life.nativeNextRoundOrder $ids[2];$controlSurvivor=[Array]::IndexOf($controlOrder,[string]$ids[3])
 $recordsOrder=$control.contract-ceq'unmounted-dead-native-turn-control-v2'
 if(($control.contract-cne'unmounted-dead-native-turn-control-v1'-and-not$recordsOrder)-or-not(Test-KmcExactJsonInteger $control.survivorSlot)-or$control.survivorSlot-ne$controlSurvivor){throw 'Unmounted death control contract differs.'}
 $readOrder=$control.PSObject.Properties['nativeOrderAfterDeath']
 if($recordsOrder){$read=@(if($null-ne$readOrder){$readOrder.Value});if($null-eq$readOrder-or$readOrder.Value-isnot[Array]-or$read.Count-ne$controlOrder.Count){throw 'Death control did not read the native order without the dead rider.'}
  for($i=0;$i-lt$read.Count;$i++){if($read[$i]-isnot[string]-or$read[$i]-cne$controlOrder[$i]){throw 'Death control did not read the native order without the dead rider.'}}}
 elseif($null-ne$readOrder){throw 'Unmounted death control contract differs.'}
 $rows=@($control.candidates);$eligible=@()
 foreach($row in $rows){
  Assert-KmcExactProperties $row @('actorId','slot','mainCharacter','pet','essential','immortal','allowDyingCondition','conscious','inCombat','paired','eligible') 'death control candidate'
  if($row.actorId-cin$ids-or-not(Test-KmcExactJsonInteger $row.slot)-or$row.slot-ne[Array]::IndexOf($controlOrder,[string]$row.actorId)){throw 'Death control candidate identity or slot differs.'}
  $gate=-not$row.mainCharacter-and-not$row.pet-and-not$row.essential-and-not$row.immortal-and$row.conscious-and$row.inCombat-and-not$row.paired-and$row.slot-ge0-and$row.slot-lt$controlSurvivor
  Assert-KmcChunk7Boolean $row.eligible $gate;if($gate){$eligible+=$row}
 }
 if($rows.Count-ne@($rows|ForEach-Object{$_.actorId}|Select-Object -Unique).Count){throw 'Death control candidates duplicated.'}
 for($i=1;$i-lt$rows.Count;$i++){if($rows[$i].slot-lt$rows[$i-1].slot){throw 'Death control candidates are not in native slot order.'}}
 Assert-KmcChunk7Boolean $control.available ($eligible.Count-gt0)
 $controlRequired=$true;if($null-ne$Request.PSObject.Properties['nativeLifeControlRequired']){if($Request.nativeLifeControlRequired-isnot[bool]){throw 'Native life control requirement must be a boolean.'};$controlRequired=$Request.nativeLifeControlRequired}
 if($controlRequired-and-not$control.available){throw 'Required unmounted death control is unavailable.'}
 if($control.available){
  Assert-KmcExactProperties $control @(@('contract','survivorSlot')+@(if($recordsOrder){'nativeOrderAfterDeath'})+@('candidates','available','actorId','slot','grantBefore','traceSequenceAtDamage','before','requestedDamage','nativeDamage','afterDamage','turn','grantAfter')) 'death control'
  if($control.actorId-cne$eligible[0].actorId-or$control.slot-ne$eligible[0].slot){throw 'Death control is not the first eligible companion in native order.'}
  Assert-KmcChunk7LifeActor $control.before $control.actorId $true
  foreach($f in @('mainCharacter','essential','immortal')){Assert-KmcChunk7Boolean $control.before.$f $false}
  if(-not(Test-KmcExactJsonInteger $control.requestedDamage)-or$control.requestedDamage-ne(Get-KmcChunk7DeathRequest $control.before ([double]$life.difficulty))-or-not(Test-KmcExactJsonInteger $control.nativeDamage)-or$control.nativeDamage-le0-or
     $control.afterDamage.actorId-cne$control.actorId-or$control.afterDamage.damage-ne$control.before.damage+$control.nativeDamage-($control.before.temporaryHp-$control.afterDamage.temporaryHp)-or$control.afterDamage.temporaryHp-gt$control.before.temporaryHp-or
     $control.afterDamage.damage-lt$control.before.hp+$control.before.constitution){throw 'Death control damage is not one bounded death stimulus.'}
  Assert-KmcChunk7DeadNativeTurn $control.turn $lifeTrace $control.actorId $nextRound
  foreach($f in @('grantBefore','grantAfter','traceSequenceAtDamage')){if(-not(Test-KmcExactJsonInteger $control.$f)-or$control.$f-lt0){throw 'Death control grant record is invalid.'}}
  $controlPrepares=@($lifeTrace.events|Where-Object{$_.sequence-gt$control.traceSequenceAtDamage-and$_.boundary-ceq'prepare-before'-and$null-ne$_.state-and$_.state.actor-ceq$control.actorId}).Count
  if($controlPrepares-ne0-or$control.grantAfter-ne$control.grantBefore){throw 'Dead control received native preparation after its death.'}
 }else{foreach($f in @('actorId','slot','grantBefore','traceSequenceAtDamage','before','requestedDamage','nativeDamage','afterDamage','turn','grantAfter')){if($null-ne$control.PSObject.Properties[$f]){throw 'Unavailable death control carries a stimulus.'}}}
 # 7E row 57 fold: the owner-seed incapacitation case carries the healed rider's native prone stand-up; no other case may.
 $proneCase=$owned-and(Get-KmcChunk7OwnedPairsCase $Request)-ceq'owner-seed-native-life-cleanup'
 $healerFlags=@($rows|Where-Object{$null-ne$_.PSObject.Properties['healer']})
 if($proneCase){Assert-KmcChunk7ProneStandUp $(if($null-ne$o.PSObject.Properties['proneStandUp']){$o.proneStandUp}else{$null}) $o $life $ids $control $rows}
 elseif($null-ne$o.PSObject.Properties['proneStandUp']-or$healerFlags.Count-ne0){throw 'Only the owner-seed incapacitation case carries the prone stand-up fold.'}
 foreach($id in @($ids[0],$ids[1])){$actor=Get-KmcChunk7CombatActor $renewed.world $id;if($actor.standard-ne0-or$actor.move-ne0){throw 'Healthy native renewal lacks fresh native expenditure.'}}
 $survivor=Get-KmcChunk7CombatActor $independent.world $ids[3];if($survivor.standard-ne0-or$survivor.move-ne0-or@($independent.world.owners|Where-Object{$_.state-ceq'Active'-and(Test-KmcChunk7OwnerActorMatch $_.mountId $ids[3])}).Count-ne0){throw 'Surviving mount lacks an independent native allocation.'}
 Assert-KmcChunk7LifeOwner $before.world $renewed.world $ids[0] $ids[1]
 foreach($window in @(@($life.healthyAttackBefore,$life.healthyEndedBeforeDamage),@($life.healthyAttackAfter,$life.healthyEndedAfterCleanup))){
  foreach($id in @($ids[0],$ids[1])){$end=@($o.nativeCompletion.calls|Where-Object{$_.kind-ceq'end'-and$_.complete-eq$true-and$_.before.actorId-ceq$id-and$_.before.frame-ge$window[0].before.frame-and$_.after.frame-le$window[1].world.frame})
   if($end.Count-ne1-or(Get-KmcChunk7CombatActor $window[1].world $id).standard-lt5.9){throw 'Healthy native actor contexts did not fully end before and after local cleanup.'}}
 }
 foreach($field in @('pairResidueAbsent','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}
 Assert-KmcChunk7Boolean $o.cleanup.ownedHorseRemoved $false;Assert-KmcChunk7Boolean $o.cleanup.nativeProgressionRequiresExternalWorkingRestoration $false
 Assert-KmcChunk7Boolean $o.cleanup.nativeSaveWrittenByThisDriver $false;Assert-KmcChunk7Boolean $o.cleanup.delegatedPersistenceCleanup $false
 Assert-KmcChunk7DeathCleanup $(if($null-ne$life.PSObject.Properties['deathCleanup']){$life.deathCleanup}else{$null}) $ids $(if($control.available){$control.actorId}else{$null})
}
# Cleanup postconditions as observed after the fixture's own cleanup settled: pair B retired with
# no residue, nothing unsettled, both pair actors out of the registry and of combat, the surviving
# mount alive, and the selection restored except units observed dead in the same capture.
function Assert-KmcChunk7DeathCleanup($c,$ids,$ControlId){
 if($null-eq$c){throw 'Native death cleanup postconditions were not observed.'}
 Assert-KmcExactProperties $c @('pairState','pairOwnerReleased','noPreparedPairResidue','presentationResidue','shellUnsettled','relationshipTransitionUnsettled','preparationUnsettled',
  'riderRegistryActive','mountRegistryActive','rider','mount','control','selectionBefore','selectableBefore','selectionAfter') 'native death cleanup'
 if($c.pairState-cne'Unmounted'){throw 'Native death cleanup did not retire pair B.'}
 foreach($f in @('pairOwnerReleased','noPreparedPairResidue')){Assert-KmcChunk7Boolean $c.$f $true}
 foreach($f in @('presentationResidue','shellUnsettled','relationshipTransitionUnsettled','preparationUnsettled','riderRegistryActive','mountRegistryActive')){Assert-KmcChunk7Boolean $c.$f $false}
 foreach($row in @(@($c.rider,$ids[2]),@($c.mount,$ids[3]))){$a=$row[0]
  if($null-eq$a-or$a.actorId-cne$row[1]){throw 'Native death cleanup actor identity differs.'}
  foreach($f in @('conscious','dead','finallyDead','inCombat','inState')){if($a.$f-isnot[bool]){throw 'Native death cleanup actor flag is missing or mistyped.'}}
  Assert-KmcChunk7Boolean $a.inCombat $false;Assert-KmcChunk7Boolean $a.inState $true
  if($a.finallyDead-and-not$a.dead){throw 'Native death cleanup actor is finally dead but not dead.'}}
 # The rider is either still natively dead or was natively raised after combat; both are recorded as observed.
 if($c.rider.dead){if($c.rider.life-cne'Dead'-or$c.rider.conscious){throw 'Native death cleanup rider state is inconsistent.'}}
 elseif($c.rider.finallyDead-or$c.rider.life-cne'Conscious'-or-not$c.rider.conscious){throw 'Native death cleanup rider state is inconsistent.'}
 if($c.mount.dead-or-not$c.mount.conscious-or$c.mount.life-cne'Conscious'){throw 'Native death cleanup lost the surviving mount.'}
 if($null-ne$ControlId){if($null-eq$c.control-or$c.control.actorId-cne$ControlId-or$c.control.dead-isnot[bool]-or$c.control.inCombat-isnot[bool]-or$c.control.inCombat){throw 'Native death cleanup lacks the observed death control.'}}
 elseif($null-ne$c.control){throw 'Native death cleanup records an unavailable control.'}
 $beforeIds=@($c.selectionBefore);$selectable=@($c.selectableBefore);$afterIds=@($c.selectionAfter)
 # The captured selection must be non-empty and still wholly selectable (in state with a view), so the
 # restore check cannot pass on an empty set.
 if($beforeIds.Count-lt1-or(ConvertTo-KmcChunk7Canonical $beforeIds)-cne(ConvertTo-KmcChunk7Canonical $selectable)){throw 'Native death cleanup captured no selection or lost a captured unit.'}
 if(@($selectable|Where-Object{$_-cnotin$beforeIds}).Count-ne0-or@($afterIds|Where-Object{$_-cnotin$selectable}).Count-ne0-or@($afterIds|Select-Object -Unique).Count-ne$afterIds.Count){throw 'Native death cleanup selection is not a subset of the captured selection.'}
 $deadIds=@(@($c.rider,$c.control)|Where-Object{$null-ne$_-and$_.dead-eq$true}|ForEach-Object{[string]$_.actorId})
 foreach($id in $selectable){if($id-cnotin$afterIds-and$id-cnotin$deadIds){throw 'Native death cleanup did not restore the captured selection.'}}
}
