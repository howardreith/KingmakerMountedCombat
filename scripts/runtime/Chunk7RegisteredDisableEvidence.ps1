# The closed owner-seed registered-callback diagnostic. Read-only evidence checks;
# this proves service disable/re-enable, never an absent DLL or removable Horses.
Set-StrictMode -Version Latest
function Assert-KmcChunk7DisableEqual($Before,$After,[string]$Name){
 if((ConvertTo-KmcChunk7Canonical $Before)-cne(ConvertTo-KmcChunk7Canonical $After)){throw ($Name+' changed across registered disable/removal.')}
}
function Assert-KmcChunk7DisableOwner($Owner,$Original,[string[]]$Roles,[int]$Pair,[string]$State){
 if($Owner.id-isnot[string]-or$Owner.id-cnotmatch'\A[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\z'-or
    (ConvertTo-KmcChunk7PairIdN $Owner.id)-cne$Original.ownerId-or$Owner.generation-ne$Original.generation-or
    -not(Test-KmcChunk7OwnerActorMatch $Owner.riderId $Roles[$Pair*2])-or-not(Test-KmcChunk7OwnerActorMatch $Owner.mountId $Roles[$Pair*2+1])-or$Owner.state-cne$State){throw 'Registered disable replaced an original owner, generation or role.'}
 Assert-KmcChunk7Boolean $Owner.retainsObligations $true
}
function Assert-KmcChunk7DisableSnapshot($Request,$Value,[string]$Label,[bool]$HasTarget,[string]$TargetId){
 Assert-KmcExactProperties $Value @('world','paused','turnBased','persistenceEnabled','controls','slots','nativePrepares','unrelatedActors','healthyAllocationId','snapshotCount','nativeWrites','authorizedWrites','pairStates','participationA','participationB','aCanRelease','bCanRelease') 'registered disable snapshot'
 $w=$Value.world;$roles=Get-KmcChunk7OwnedPairIds $Request
 Assert-KmcBootstrapInventoryString $w.label $Label
 foreach($key in @('frame','gameTicks')){Assert-KmcOrdinaryInteger $w.$key 0 ([long]::MaxValue)}
 Assert-KmcOrdinaryInteger $w.round -1 1000000
 if($null-ne$w.currentActor){$null=ConvertFrom-KmcChunk7ActorUniqueId $w.currentActor}
 Assert-KmcChunk7ExactIds @($w.actors) $roles
 foreach($actor in $w.actors){$null=Get-KmcChunk7CombatActor $w $actor.id
  if($actor.prepared-isnot[bool]){throw 'Registered disable actor preparation is not a native Boolean.'}
  foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $actor.position.$axis)){throw 'Registered disable position is not finite.'}}
 }
 foreach($key in @('paused','turnBased','persistenceEnabled','aCanRelease','bCanRelease')){if($Value.$key-isnot[bool]){throw 'Registered disable state Boolean is missing or mistyped.'}}
 foreach($key in @('snapshotCount','nativeWrites','authorizedWrites')){Assert-KmcOrdinaryInteger $Value.$key 0 0}
 if($Value.nativePrepares-isnot[Array]-or$Value.nativePrepares.Count-ne4){throw 'Registered disable must observe all four Prepare counters.'}
 if($HasTarget){Assert-KmcManualSeedObjectIdentity $Value.healthyAllocationId}elseif($null-ne$Value.healthyAllocationId){throw 'Removal refusal cannot claim a not-yet-installed combat allocation trace.'}
 Assert-KmcChunk7ExactIds $Value.nativePrepares $roles 'actorId'
 foreach($row in $Value.nativePrepares){Assert-KmcOrdinaryInteger $row.count 0 ([int]::MaxValue)}
 $unrelated=@($Request.ownedPairs.partyIds|Where-Object{$_-cnotin$roles});if($HasTarget){$unrelated+=,$TargetId}
 if($Value.unrelatedActors-isnot[Array]){throw 'Registered disable lacks native unrelated/target actor observations.'}
 $actual=@();foreach($row in $Value.unrelatedActors){
  Assert-KmcExactProperties $row @('native','prepared','inCombat') 'registered disable unrelated actor'
  Assert-KmcChunk7PersistenceNativeActor $row.native
  foreach($key in @('prepared','inCombat')){if($row.$key-isnot[bool]){throw 'Unrelated native state is not Boolean.'}}
  $actual+=,$row.native.Id
 }
 if((@($actual|Sort-Object -CaseSensitive)-join'|')-cne(@($unrelated|Sort-Object -CaseSensitive)-join'|')){throw 'Registered disable omitted or substituted an unrelated native actor or target.'}
 $c=$Value.controls
 foreach($key in @('Registered','Enabled','SerializationSuspended')){if($c.$key-isnot[bool]){throw 'Registered disable control Boolean is missing or mistyped.'}}
 Assert-KmcChunk7Boolean $c.Registered $true;Assert-KmcChunk7Boolean $c.SerializationSuspended $false
 Assert-KmcChunk7NativeCounters $c @('ExactFactCount','DuplicateFactCount','ManagedHotbarSlotCount','TargetSelectionStartCount','TargetSelectionEndCount','NativeCastRequestCount','NativeRefusalCount','DispatchAcceptedCount','DispatchRejectedCount','NativePrimaryShellPrepareCount','ActivationRecordCount','RiderPrimaryRelationshipEndCount')
 Assert-KmcOrdinaryInteger $c.DuplicateFactCount 0 0
 if($Value.slots-isnot[Array]-or$Value.pairStates-isnot[Array]-or$Value.pairStates.Count-ne2-or$w.owners-isnot[Array]){throw 'Registered disable live inventory is not a bounded explicit array.'}
}
function Assert-KmcChunk7DisableDebt($Before,$After,[string[]]$Roles){
 Assert-KmcChunk7SameCombatBudget $Before.world $After.world $Roles
 foreach($key in @('gameTicks','round')){if($Before.world.$key-ne$After.world.$key){throw 'Registered disable changed the native clock or round.'}}
 foreach($key in @('nativePrepares','unrelatedActors','healthyAllocationId')){Assert-KmcChunk7DisableEqual $Before.$key $After.$key $key}
 foreach($id in $Roles){$a=Get-KmcChunk7CombatActor $Before.world $id;$b=Get-KmcChunk7CombatActor $After.world $id
  Assert-KmcChunk7Boolean $b.prepared $a.prepared
  # Pair contexts may detach; actual observed native Prepare counts cannot grow.
  if($null-ne$a.native-and$null-ne$b.native){foreach($key in @('actor','actorObject','grantSequence')){Assert-KmcChunk7DisableEqual $a.native.$key $b.native.$key ('native '+$key)}}
 }
 foreach($key in @('snapshotCount','nativeWrites','authorizedWrites')){Assert-KmcOrdinaryInteger $Before.$key 0 0;Assert-KmcOrdinaryInteger $After.$key 0 0}
 foreach($key in @('TargetSelectionStartCount','TargetSelectionEndCount','NativeCastRequestCount','NativeRefusalCount','DispatchAcceptedCount','DispatchRejectedCount','NativePrimaryShellPrepareCount')){Assert-KmcChunk7DisableEqual $Before.controls.$key $After.controls.$key ('control '+$key)}
}
function Assert-KmcChunk7DisableDecay($Before,$After,[double]$Elapsed,[string[]]$Axes){
 foreach($axis in $Axes){$oldRaw=$Before.$axis;$newRaw=$After.$axis
  if(-not(Test-KmcChunk7FiniteNumber $oldRaw)-or-not(Test-KmcChunk7FiniteNumber $newRaw)){throw ('Disabled native '+$axis+' is not a finite JSON number.')}
  $old=[double]$oldRaw;$new=[double]$newRaw
  if($old-lt0-or$new-lt0-or$new-gt($old+0.000001)-or($old-$new)-gt($Elapsed+0.003)){throw ('Disabled native '+$axis+' changed beyond elapsed game time.')}
 }
}
function Assert-KmcChunk7DisableNativeProgression($Request,$Observations,$Disable,[string[]]$Roles){
 $a=$Disable.retry;$b=$Disable.disabledLaterFrame;$ticks=[long]$b.world.gameTicks-[long]$a.world.gameTicks
 if($ticks-le0-or$ticks-gt[TimeSpan]::TicksPerSecond-or$b.world.frame-le$a.world.frame-or$b.world.round-ne$a.world.round){throw 'Disabled native frame lacks a bounded advancing clock and stable round.'}
 $elapsed=$ticks/[double][TimeSpan]::TicksPerSecond
 foreach($spec in @(@('06008355','Kingmaker.EntitySystem.Entities.UnitEntityData.JoinCombat','DisableJoinBefore','DisableJoinAfter'),@('0600936F','Kingmaker.Controllers.Combat.UnitCombatPrepareController.Tick','DisablePrepareTickBefore','DisablePrepareTickAfter'),@('0600C3BE','Kingmaker.Controllers.Combat.UnitCombatState+Cooldowns.Clear','ClearBefore','ClearAfter'))){
  $hooks=@($Observations.allocationTrace.observerHooks|Where-Object{$_.token-ceq$spec[0]-and$_.method-ceq$spec[1]-and$_.moduleMvid-ceq'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-and$_.prefix-ceq$spec[2]-and$_.postfix-ceq$spec[3]})
  if($hooks.Count-ne1){throw 'Disabled native frame lacks an exact installed observation hook.'}
 }
 foreach($key in @('nativePrepares','healthyAllocationId')){Assert-KmcChunk7DisableEqual $a.$key $b.$key $key}
 foreach($key in @('TargetSelectionStartCount','TargetSelectionEndCount','NativeCastRequestCount','NativeRefusalCount','DispatchAcceptedCount','DispatchRejectedCount','NativePrimaryShellPrepareCount')){Assert-KmcChunk7DisableEqual $a.controls.$key $b.controls.$key ('disabled control '+$key)}
 foreach($key in @('snapshotCount','nativeWrites','authorizedWrites')){Assert-KmcOrdinaryInteger $b.$key 0 0}
 foreach($i in @(0,1,3)){$before=Get-KmcChunk7CombatActor $a.world $Roles[$i];$after=Get-KmcChunk7CombatActor $b.world $Roles[$i]
  Assert-KmcChunk7Boolean $after.prepared $before.prepared;Assert-KmcChunk7Boolean $after.native.inCombat $before.native.inCombat
  Assert-KmcChunk7DisableEqual $before.reactions $after.reactions 'native reaction count';Assert-KmcChunk7DisableEqual $before.native.actorObject $after.native.actorObject 'native actor object'
  Assert-KmcChunk7DisableEqual $before.native.grantSequence $after.native.grantSequence 'native grant sequence';Assert-KmcChunk7DisableEqual $before.native.reactionsPerRound $after.native.reactionsPerRound 'native reactions per round'
  Assert-KmcChunk7DisableDecay $before $after $elapsed @('standard','move','swift','reactionCooldown')
  Assert-KmcChunk7DisableDecay $before.native $after.native $elapsed @('initiativeCooldown')
 }
 $beforeB=Get-KmcChunk7CombatActor $a.world $Roles[2];$afterB=Get-KmcChunk7CombatActor $b.world $Roles[2]
 foreach($v in @(@($beforeB.native.inCombat,$false),@($beforeB.prepared,$false),@($afterB.native.inCombat,$true),@($afterB.prepared,$true))){Assert-KmcChunk7Boolean $v[0] $v[1]}
 Assert-KmcChunk7DisableEqual $beforeB.native.actorObject $afterB.native.actorObject 'pending native actor object';Assert-KmcChunk7DisableEqual $beforeB.reactions $afterB.reactions 'pending reaction count'
 Assert-KmcChunk7DisableEqual $beforeB.native.grantSequence $afterB.native.grantSequence 'pending native grant sequence';Assert-KmcChunk7DisableEqual $beforeB.native.reactionsPerRound $afterB.native.reactionsPerRound 'pending reactions per round'
 Assert-KmcChunk7DisableDecay $beforeB $afterB $elapsed @('reactionCooldown')
 if(-not(Test-KmcChunk7FiniteNumber $afterB.native.initiativeCooldown)-or[double]$afterB.native.initiativeCooldown-lt0-or[double]$afterB.native.initiativeCooldown-gt6){throw 'Pending rider has an invalid native initiative cooldown.'}
 Assert-KmcChunk7Boolean $afterB.native.waitingInitiative $true
 foreach($axis in @('standard','move','swift')){if([double]$beforeB.$axis-lt$(if($axis-ceq'move'){2.9}else{5.9})-or[double]$afterB.$axis-gt0.001){throw 'Native pending rider preparation did not explain cleared spent debt.'}}
 if($a.unrelatedActors.Count-ne$b.unrelatedActors.Count){throw 'Disabled frame lost unrelated native actors.'}
 foreach($old in $a.unrelatedActors){$rows=@($b.unrelatedActors|Where-Object{$_.native.Id-ceq$old.native.Id});if($rows.Count-ne1){throw 'Disabled frame substituted an unrelated native actor.'};$next=$rows[0]
  Assert-KmcChunk7Boolean $next.prepared $old.prepared;Assert-KmcChunk7Boolean $next.inCombat $old.inCombat
  foreach($key in @('ReactionsRemaining','LastSurpriseTicks')){Assert-KmcChunk7DisableEqual $old.native.$key $next.native.$key ('unrelated '+$key)}
  Assert-KmcChunk7DisableDecay $old.native $next.native $elapsed @('Standard','Move','Swift','Initiative','Reaction')
 }
 Assert-KmcOrdinaryInteger $Disable.traceSequenceBefore 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $Disable.traceSequenceAfter ($Disable.traceSequenceBefore+1) ([int]::MaxValue)
 $events=@($Disable.nativeTransition);$full=@($Observations.allocationTrace.events)
 if($events.Count-ne($Disable.traceSequenceAfter-$Disable.traceSequenceBefore)-or$full.Count-lt$Disable.traceSequenceAfter){throw 'Native disable transition is not a complete bounded trace slice.'}
 for($i=0;$i-lt$events.Count;$i++){Assert-KmcOrdinaryInteger $events[$i].sequence ($Disable.traceSequenceBefore+$i+1) ($Disable.traceSequenceBefore+$i+1);Assert-KmcChunk7DisableEqual $events[$i] $full[$Disable.traceSequenceBefore+$i] 'native disable trace'}
 if(@($events|Where-Object{$_.boundary-cin@('prepare-before','prepare-after')-and$_.state.actor-cin$Roles}).Count-ne0){throw 'Disabled native frame unexpectedly entered TurnController.Prepare.'}
 $names=@('disable-join-before','disable-join-after','disable-prepare-tick-before','clear-before','clear-after','disable-prepare-tick-after');$matched=@()
 foreach($name in $names){$rows=@($events|Where-Object{$_.boundary-ceq$name-and$_.state.actor-ceq$Roles[2]});if($rows.Count-ne1){throw ('Native disable lacks exact rider '+$name)};$matched+=,$rows[0]}
 for($i=1;$i-lt$matched.Count;$i++){if($matched[$i].sequence-le$matched[$i-1].sequence){throw 'Native rider rejoin/preparation/clear order differs.'}}
 foreach($event in $matched){Assert-KmcOrdinaryInteger $event.frame $b.world.frame $b.world.frame;Assert-KmcOrdinaryInteger $event.gameTicks $a.world.gameTicks $b.world.gameTicks
  if($event.gameTicks-le0-or$event.state.inCombat-isnot[bool]-or$event.state.prepared-isnot[bool]){throw 'Native causal event has invalid clock or Boolean state.'}
  foreach($axis in @('standard','move','swift','initiativeCooldown')){if(-not(Test-KmcChunk7FiniteNumber $event.state.$axis)){throw 'Native causal resource is not a finite JSON number.'}}
 }
 if($matched[0].frame-le$a.world.frame-or$matched[0].state.inCombat-ne$false-or$matched[1].state.inCombat-ne$true-or
  $matched[2].state.prepared-ne$false-or$matched[3].state.prepared-ne$true-or$matched[3].detail-cne'disablePrepareDepth=1'-or$matched[4].detail-cne'disablePrepareDepth=1'-or
  [double]$matched[3].state.standard-lt5.9-or[double]$matched[3].state.move-lt2.9-or[double]$matched[3].state.swift-lt5.9-or
  [double]$matched[4].state.standard-lt0-or[double]$matched[4].state.move-lt0-or[double]$matched[4].state.swift-lt0-or
  [double]$matched[4].state.standard-gt0.001-or[double]$matched[4].state.move-gt0.001-or[double]$matched[4].state.swift-gt0.001-or$matched[5].state.prepared-ne$true){throw 'Native rejoin, Prepare.Tick and Cooldowns.Clear did not exactly account for rider debt.'}
 foreach($axis in @('standard','move','swift')){if(-not(Test-KmcChunk7FiniteNumber $matched[5].state.$axis)-or[double]$matched[5].state.$axis-lt0-or[double]$matched[5].state.$axis-gt0.001){throw 'Native Prepare.Tick exited without the observed cleared rider debt.'}}
 if(-not(Test-KmcChunk7FiniteNumber $matched[5].state.initiativeCooldown)-or[double]$matched[5].state.initiativeCooldown-le0-or[double]$matched[5].state.initiativeCooldown-gt6-or$matched[5].state.waitingInitiative-isnot[bool]-or-not$matched[5].state.waitingInitiative){throw 'Native Prepare.Tick exit has invalid initiative state.'}
 for($i=1;$i-lt$matched.Count;$i++){if($matched[$i].gameTicks-lt$matched[$i-1].gameTicks){throw 'Native causal clock regressed within one frame.'}}
 for($i=0;$i-le3;$i++){Assert-KmcChunk7DisableDecay $beforeB $matched[$i].state $elapsed @('standard','move','swift')}
 for($i=1;$i-le3;$i++){Assert-KmcChunk7DisableDecay $matched[$i-1].state $matched[$i].state $elapsed @('standard','move','swift')}
 Assert-KmcChunk7DisableDecay $matched[5].state $afterB.native $elapsed @('initiativeCooldown')
 foreach($event in $matched){Assert-KmcChunk7DisableEqual $event.state.actorObject $beforeB.native.actorObject 'native rider clear actor object'}
}
function Assert-KmcChunk7DisablePairStates($Value,$MountedStart,[string[]]$Roles,[bool]$Unmounted,[bool]$Held){
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 for($i=0;$i-lt2;$i++){
  $original=@($MountedStart.owners|Where-Object riderId -CEQ $Roles[$i*2])[0]
  $rows=@($Value.pairStates|Where-Object pairId -CEQ $original.ownerId);if($rows.Count-ne1){throw 'Registered disable lost an exact pair observation.'};$p=$rows[0]
  Assert-KmcExactProperties $p @('pairId','generation','riderId','mountId','relationshipState','noPreparedPairResidue','commandsRetained','unsettledShell','currentOwnerId','currentGeneration','currentOwnerState','lastTransition') 'registered disable pair state'
  if(-not$seen.Add($p.pairId)-or$p.generation-ne$original.generation-or-not(Test-KmcChunk7OwnerActorMatch $p.riderId $Roles[$i*2])-or-not(Test-KmcChunk7OwnerActorMatch $p.mountId $Roles[$i*2+1])){throw 'Registered disable pair identity differs from its original mounted start.'}
  $state=if($Unmounted){'Unmounted'}else{'Mounted'};Assert-KmcBootstrapInventoryString $p.relationshipState $state
  foreach($key in @('noPreparedPairResidue','commandsRetained','unsettledShell')){if($p.$key-isnot[bool]){throw 'Registered disable pair residue observation is not Boolean.'}}
  if($Unmounted){Assert-KmcChunk7Boolean $p.noPreparedPairResidue $true;Assert-KmcChunk7Boolean $p.commandsRetained $false;Assert-KmcChunk7Boolean $p.unsettledShell $false}
  $owners=@($Value.world.owners|Where-Object{(ConvertTo-KmcChunk7PairIdN $_.id)-ceq$original.ownerId})
  if($null-eq$p.currentOwnerId){
   if(-not$Unmounted-or$i-ne0-or$owners.Count-ne0-or$null-ne$p.currentGeneration-or$null-ne$p.currentOwnerState){throw 'Registered disable discarded unresolved owner custody.'}
   Assert-KmcChunk7Boolean $Value.aCanRelease $true
   if($null-ne$Value.participationA){throw 'A retired owner still records unresolved participation.'}
  }else{
   if($p.currentOwnerId-cne$original.ownerId-or$p.currentGeneration-ne$original.generation-or$owners.Count-ne1){throw 'Registered disable replaced retained owner identity.'}
   $ownerState=if($Unmounted){'Detached'}else{'Active'};Assert-KmcBootstrapInventoryString $p.currentOwnerState $ownerState
   Assert-KmcChunk7DisableOwner $owners[0] $original $Roles $i $ownerState
  }
  if($Unmounted){$t=$p.lastTransition
   Assert-KmcExactProperties $t @('succeeded','state','trigger','errors','movementAuthorityResidual','presentationResidual') 'registered disable transition'
   Assert-KmcBootstrapInventoryString $t.state 'Unmounted';Assert-KmcBootstrapInventoryString $t.trigger 'ModDisabled'
   foreach($key in @('movementAuthorityResidual','presentationResidual')){Assert-KmcChunk7Boolean $t.$key $false}
   Assert-KmcChunk7Boolean $t.succeeded (-not($Held-and$i-eq1))
   if($t.errors-isnot[Array]-or$t.errors.Count-ne$(if($Held-and$i-eq1){1}else{0})){throw 'Registered disable transition errors are not exactly isolated to the selected failure.'}
   if($Held-and$i-eq1-and($t.errors[0]-isnot[string]-or-not$t.errors[0].Contains('C7 selected pair B ModDisabled subscriber failure'))){throw 'Registered disable retained a different failure.'}
  }
 }
 if($Value.world.owners.Count-ne@($Value.pairStates|Where-Object{$null-ne$_.currentOwnerId}).Count){throw 'Registered disable retained a foreign owner.'}
 if($Unmounted){Assert-KmcChunk7Boolean $Value.bCanRelease $false}
}
function Assert-KmcChunk7PermanentHorseRemovalRefusal($Request,$Observations){
 $o=$Observations;$v=$o.removalRefusal;$roles=Get-KmcChunk7OwnedPairIds $Request
 Assert-KmcExactProperties $v @('contract','assessment','beginAccepted','state','status','assessmentDelta','refusalDelta','cleanupSaveDelta','permanentHorses','before','after','dismountVisits') 'permanent Horse removal refusal'
 Assert-KmcBootstrapInventoryString $v.contract 'owner-seed-permanent-horse-removal-refusal-v1'
 Assert-KmcChunk7Boolean $v.beginAccepted $false;Assert-KmcBootstrapInventoryString $v.state 'Refused'
 if($v.status-isnot[string]-or[string]::IsNullOrWhiteSpace($v.status)){throw 'Removal refusal lacks the actual service status.'}
 foreach($kv in @(@('assessmentDelta',2),@('refusalDelta',1),@('cleanupSaveDelta',0))){Assert-KmcOrdinaryInteger $v.($kv[0]) $kv[1] $kv[1]}
 Assert-KmcExactProperties $v.assessment @('safe','mounted','world','reasons','permanentReferences') 'actual removal assessment'
 Assert-KmcChunk7Boolean $v.assessment.safe $false;Assert-KmcChunk7Boolean $v.assessment.mounted $true
 $world=$v.assessment.world;$trueFields=@('WorldLoaded','DefaultMode','SaveAllowed')
 $falseFields=@('PartyInCombat','AnyPartyMemberInCombat','ActiveMountedCommand','StockAttackIntent','PairedActivation','CombatRestorationPending','LoadInFlight','LoadingWorld','LoadingProcess','SaveSuspended','ActiveSaveScope','SaveDraining','WorldHoldReleasePending','ResetToMainMenuPending','InspectionFailed')
 Assert-KmcExactProperties $world ($trueFields+$falseFields+@('InspectionFailure')) 'actual removal world facts'
 foreach($key in $trueFields){Assert-KmcChunk7Boolean $world.$key $true};foreach($key in $falseFields){Assert-KmcChunk7Boolean $world.$key $false}
 if($null-ne$world.InspectionFailure){throw 'Horse refusal is masked by a world-inspection failure.'}
 foreach($key in @('reasons','permanentReferences')){if($v.assessment.$key-isnot[Array]-or$v.assessment.$key.Count-eq0){throw 'Horse refusal lacks actual permanent-reference reasons.'};foreach($reason in $v.assessment.$key){if($reason-isnot[string]-or[string]::IsNullOrWhiteSpace($reason)){throw 'Removal reasons must be actual nonempty scalar strings.'}}}
 if($v.permanentHorses-isnot[Array]-or$v.permanentHorses.Count-ne2-or$v.dismountVisits-isnot[Array]-or$v.dismountVisits.Count-ne0){throw 'Horse removal refusal changed pair state or omitted a permanent Horse.'}
 for($i=0;$i-lt2;$i++){
  $horse=@($v.permanentHorses|Where-Object riderId -CEQ $roles[$i*2]);if($horse.Count-ne1){throw 'Removal assessment omitted an original rider.'};$horse=$horse[0]
  Assert-KmcExactProperties $horse @('riderId','horseId','unitBlueprintGuid','featureBlueprintGuid','riderOwnsFeature','reciprocalPetOwnership') 'permanent native Horse reference'
  Assert-KmcBootstrapInventoryString $horse.horseId $roles[$i*2+1];Assert-KmcBootstrapInventoryString $horse.unitBlueprintGuid '4016c7db400ab721ff125aef9e65e202';Assert-KmcBootstrapInventoryString $horse.featureBlueprintGuid '7db7c50677e39f09feef56f3831fc723'
  foreach($key in @('riderOwnsFeature','reciprocalPetOwnership')){Assert-KmcChunk7Boolean $horse.$key $true}
  if(@($v.assessment.permanentReferences|Where-Object{$_.Contains('['+$horse.horseId+']')}).Count-eq0){throw 'Actual removal assessment did not identify both exact Horse units.'}
 }
 Assert-KmcChunk7DisableSnapshot $Request $v.before 'removal-refusal-before' $false $o.targetId
 Assert-KmcChunk7DisableSnapshot $Request $v.after 'removal-refusal-after' $false $o.targetId
 Assert-KmcChunk7DisableDebt $v.before $v.after $roles
 if($v.before.world.frame-ne$v.after.world.frame){throw 'Removal assessment crossed an unobserved native frame.'}
 foreach($key in @('paused','turnBased','persistenceEnabled','controls','slots','pairStates','participationA','participationB','aCanRelease','bCanRelease')){Assert-KmcChunk7DisableEqual $v.before.$key $v.after.$key ('refusal '+$key)}
 foreach($snap in @($v.before,$v.after)){Assert-KmcChunk7Boolean $snap.turnBased $false}
 Assert-KmcChunk7DisableEqual $v.before.world.owners $v.after.world.owners 'refusal owner graph'
 foreach($snap in @($v.before,$v.after)){Assert-KmcChunk7Boolean $snap.persistenceEnabled $true;Assert-KmcChunk7Boolean $snap.controls.Enabled $true
  Assert-KmcOrdinaryInteger $snap.controls.ExactFactCount 6 6;Assert-KmcOrdinaryInteger $snap.controls.ManagedHotbarSlotCount 4 4
  foreach($key in @('NativeCastRequestCount','NativeRefusalCount')){Assert-KmcOrdinaryInteger $snap.controls.$key 0 0}
  $expectedSlots=@($o.mountedStart.slots|ForEach-Object{[pscustomobject]@{ActorId=$_.actorId;Index=$_.index;Kind=$_.kind}})
  Assert-KmcChunk7DisableEqual $snap.slots $expectedSlots 'refusal original live slots'
  Assert-KmcChunk7DisablePairStates $snap $o.mountedStart $roles $false $false
 }
}
function Assert-KmcChunk7RegisteredDisableSequence($Request,$Observations){
 $o=$Observations;$d=$o.registeredDisable;$roles=Get-KmcChunk7OwnedPairIds $Request;$originalB=@($o.mountedStart.owners|Where-Object riderId -CEQ $roles[2])[0]
 Assert-KmcExactProperties $d @('contract','callback','injectedPairId','before','visits','traceSequenceBefore','firstAccepted','held','injectionCount','retryAccepted','retry','retrySameUpdate','disabledLaterFrame','nativeTransition','traceSequenceAfter','reenableAccepted','reenabled','complete') 'registered disable local failure'
 Assert-KmcBootstrapInventoryString $d.contract 'owner-seed-registered-disable-local-failure-v1';Assert-KmcBootstrapInventoryString $d.callback 'UnityModManager.ModEntry.OnToggle';Assert-KmcBootstrapInventoryString $d.injectedPairId $originalB.ownerId
 foreach($key in @('retryAccepted','retrySameUpdate','reenableAccepted','complete')){Assert-KmcChunk7Boolean $d.$key $true};Assert-KmcChunk7Boolean $d.firstAccepted $false;Assert-KmcOrdinaryInteger $d.injectionCount 1 1
 $labels=@('registered-disable-before','registered-disable-refused','registered-disable-retry-accepted','registered-disabled-later-frame','registered-reenabled-before-native-teardown');$snaps=@($d.before,$d.held,$d.retry,$d.disabledLaterFrame,$d.reenabled)
 for($i=0;$i-lt$snaps.Count;$i++){$s=$snaps[$i]
  Assert-KmcChunk7DisableSnapshot $Request $s $labels[$i] $true $o.targetId
  Assert-KmcChunk7Boolean $s.paused $false;Assert-KmcChunk7Boolean $s.turnBased $true
  if($i-lt3){Assert-KmcChunk7DisableDebt $d.before $s $roles}
  elseif($i-eq3){Assert-KmcChunk7DisableNativeProgression $Request $o $d $roles}
  else{Assert-KmcChunk7DisableDebt $d.disabledLaterFrame $s $roles}
  Assert-KmcChunk7DisablePairStates $s $o.mountedStart $roles ($i-gt0) ($i-eq1)
  Assert-KmcChunk7DisableEqual $d.before.participationB $s.participationB 'pending survivor participation'
  $enabled=$i-lt2-or$i-eq4;Assert-KmcChunk7Boolean $s.persistenceEnabled $enabled;Assert-KmcChunk7Boolean $s.controls.Enabled $enabled
  if($i-in@(2,3)){foreach($key in @('ExactFactCount','ManagedHotbarSlotCount')){Assert-KmcOrdinaryInteger $s.controls.$key 0 0};if($s.slots.Count-ne0){throw 'Disabled services retain live native controls.'}}
  if($i-gt0-and$s.slots.Count-ne0){throw 'An unmounted registered-disable pair retains live native slot bindings.'}
 }
 Assert-KmcOrdinaryInteger $d.before.controls.NativeCastRequestCount 4 4;Assert-KmcOrdinaryInteger $d.before.controls.NativeRefusalCount 0 0
 Assert-KmcOrdinaryInteger $d.before.controls.ExactFactCount 6 6;Assert-KmcOrdinaryInteger $d.before.controls.ManagedHotbarSlotCount 4 4
 $expectedSlots=@($o.mountedStart.slots|ForEach-Object{[pscustomobject]@{ActorId=$_.actorId;Index=$_.index;Kind=$_.kind}});Assert-KmcChunk7DisableEqual $d.before.slots $expectedSlots 'pre-disable original live slots'
 if($d.before.world.frame-ne$d.held.world.frame-or$d.before.world.frame-ne$d.retry.world.frame-or$d.disabledLaterFrame.world.frame-le$d.retry.world.frame-or$d.reenabled.world.frame-ne$d.disabledLaterFrame.world.frame){throw 'Registered disable lost same-update retry or its later native disabled frame.'}
 if($d.before.participationA.Activation.Finalized-isnot[bool]-or-not$d.before.participationA.Activation.Finalized-or$d.before.participationA.RiderId-cne$roles[0]-or$d.before.participationA.MountId-cne$roles[1]){throw 'Healthy pair A did not complete its actual activation before disable.'}
 Assert-KmcChunk7DisableEqual $d.before.participationB $o.pendingSurvivor.participation 'native pending input'
 Assert-KmcChunk7SameCombatBudget $o.pendingSurvivor.after $d.before.world $roles
 if($d.visits-isnot[Array]-or$d.visits.Count-ne4){throw 'Actual registered callbacks must visit both original owners in both phases.'}
 foreach($phase in @('first-disable','retry-disable')){foreach($i in 0..1){$owner=@($o.mountedStart.owners|Where-Object riderId -CEQ $roles[$i*2])[0];$rows=@($d.visits|Where-Object{$_.phase-ceq$phase-and$_.pairId-ceq$owner.ownerId})
  if($rows.Count-ne1){throw 'Registered callback omitted or duplicated an owner visit.'};$row=$rows[0]
  Assert-KmcExactProperties $row @('phase','frame','trigger','pairId','generation','riderId','mountId','observedOwnerId','observedGeneration','relationshipState') 'registered disable actual owner visit'
  Assert-KmcBootstrapInventoryString $row.trigger 'ModDisabled'
  if($row.generation-ne$owner.generation-or$row.frame-ne$d.before.world.frame-or-not(Test-KmcChunk7OwnerActorMatch $row.riderId $roles[$i*2])-or-not(Test-KmcChunk7OwnerActorMatch $row.mountId $roles[$i*2+1])){throw 'Registered callback visit changed original identity or native frame.'}
 if($null-eq$row.observedOwnerId){$heldPair=@($d.held.pairStates|Where-Object pairId -CEQ $owner.ownerId);if($i-ne0-or$phase-cne'retry-disable'-or$null-ne$row.observedGeneration-or-not$d.held.aCanRelease-or$heldPair.Count-ne1-or$null-ne$heldPair[0].currentOwnerId){throw 'Registered callback visit lost unresolved owner custody.'}}
  elseif($row.observedOwnerId-cne$owner.ownerId-or$row.observedGeneration-ne$owner.generation){throw 'Registered callback visited a replacement owner.'}
  if($row.relationshipState-cnotin@('Mounted','Unmounted','Dismounting')){throw 'Registered callback visited an invalid relationship state.'}
 }}
}
function Assert-KmcChunk7RegisteredDisableCombat($Request,$Observations){
 if(-not(Test-KmcChunk7OwnedPairsRegisteredDisable $Request)){throw 'Registered disable evidence requires its closed owner-seed selection.'}
 Assert-KmcChunk7OwnedPairsSelection $Request
 if((Get-KmcChunk7OwnedLoadCount $Request)-ne1-or(Get-KmcChunk7OwnedWriteCount $Request)-ne0){throw 'Registered disable retains one native load and zero writes.'}
 $o=$Observations;$roles=Get-KmcChunk7OwnedPairIds $Request
 Assert-KmcChunk7OwnerSeedDerivedCombatStart $Request $o
 foreach($key in @('r1','firstActionMount','localFailure')){if($null-ne$o.PSObject.Properties[$key]){throw 'Registered disable cannot substitute another combat cleanup or remount proof.'}}
 if(@($o.snapshots|Where-Object{$_.label-clike'*-native-refresh'}).Count-ne0-or$o.inputs-isnot[Array]-or$o.inputs.Count-ne4-or@($o.inputs|Where-Object{$_.label-cnotin@('rider-primary','mount-primary')}).Count-ne0){throw 'Registered disable admits only the two original pairs'' measured ordinary primaries, without refresh/remount inputs.'}
 if($o.activations-isnot[Array]-or$o.activations.Count-ne2){throw 'Registered disable lacks its two actual mounted activations.'}
 $commands=[Collections.Generic.HashSet[long]]::new()
 for($i=0;$i-lt2;$i++){$a=@($o.activations|Where-Object pair -EQ $i);if($a.Count-ne1){throw 'Registered disable repeated or omitted an activation.'};$a=$a[0];$r=$roles[$i*2];$m=$roles[$i*2+1]
  if($a.before.currentActor-cne$r){throw 'Registered disable principal identity differs.'};Assert-KmcOrdinaryInteger $a.sequence 1 ([long]::MaxValue)
  $move=$a.movement;Assert-KmcChunk7Boolean $move.clicked $true;Assert-KmcChunk7Boolean $move.terminal $true
  if($move.selectedId-cne$r-or$move.executorId-cne$m-or$move.command-eq0){throw 'Registered disable transport escaped exact native mount custody.'}
  $before=Get-KmcChunk7CombatActor $move.before $m;$after=Get-KmcChunk7CombatActor $move.after $m;$dist=0.0
  foreach($axis in @('x','y','z')){foreach($pos in @($before.position,$after.position)){if(-not(Test-KmcChunk7FiniteNumber $pos.$axis)){throw 'Registered disable transport is not finite.'}};$dist+=[Math]::Pow([double]$after.position.$axis-[double]$before.position.$axis,2)}
  if(-not(Test-KmcChunk7FiniteNumber $move.distance)-or[Math]::Sqrt($dist)-le0.02-or[Math]::Abs([Math]::Sqrt($dist)-$move.distance)-gt0.0001-or$after.move-le$before.move){throw 'Registered disable mount did not move and spend its own native budget.'}
  Assert-KmcChunk7SameCombatBudget $move.before $move.after @($roles|Where-Object{$_-cne$m})
  foreach($member in 0..1){$label=@('rider-primary','mount-primary')[$member];$id=$roles[$i*2+$member];$attack=@($o.inputs|Where-Object{$_.pair-eq$i-and$_.label-ceq$label});if($attack.Count-ne1){throw 'Registered disable lacks exactly one native primary per actor.'}
   $command=Assert-KmcChunk7CombatAttack $attack[0] $o.allocationTrace $id $r $o.targetId $roles;if(-not$commands.Add($command)){throw 'Registered disable reused a native primary expenditure.'}
   if($i-eq0){$end=@($o.nativeCompletion.calls|Where-Object{$_.kind-ceq'end'-and$_.complete-eq$true-and$_.before.actorId-ceq$id-and$_.before.frame-ge$a.before.frame-and$_.after.frame-le$a.afterEnd.frame});if($end.Count-ne1-or(Get-KmcChunk7CombatActor $a.afterEnd $id).standard-lt5.9){throw 'Healthy pair A did not end each native actor context exactly once.'}}
  }
  if($i-eq0){$c=$a.tbChargeRefusal;Assert-KmcChunk7Boolean $c.featureEnabled $true;Assert-KmcChunk7Boolean $c.available $false;if($c.castDelta-ne0-or[string]::IsNullOrWhiteSpace($c.reason)){throw 'Registered disable A admitted unsupported TB Charge.'};Assert-KmcChunk7SameCombatBudget $c.before $c.after $roles}
  elseif($null-ne$a.PSObject.Properties['afterEnd']-or$null-ne$a.PSObject.Properties['tbChargeRefusal']){throw 'Registered disable B escaped its actual pending-survivor boundary.'}
 }
 $pending=$o.pendingSurvivor;$state=$pending.participation
 if($pending.stimulus-cne'UnitEntityData.LeaveCombat then native RemoveUnit if still registered'-or$state.Activation-ne$null-or$state.RiderId-ne$null-or$state.MountId-ne$null-or$state.PendingSplitId-cne$roles[3]-or$state.PendingSplitRound-ne$pending.after.round-or$state.Boundary-ne$null-or$state.Partner-ne$null){throw 'Registered disable lacks the real native B pending-only survivor.'}
 Assert-KmcChunk7Boolean $state.BoundaryIsCurrent $false;Assert-KmcChunk7SameCombatBudget $pending.before $pending.after @($roles[0],$roles[1])
 $m=Get-KmcChunk7CombatActor $pending.before $roles[3];$n=Get-KmcChunk7CombatActor $pending.after $roles[3]
 foreach($kv in @(@('standard',6.0),@('move',3.0),@('swift',6.0))){if([Math]::Abs([double]$n.($kv[0])-[Math]::Max([double]$m.($kv[0]),$kv[1]))-gt0.001){throw 'Registered disable pending mount differs from native removal forfeiture.'}}
 foreach($key in @('reactions','reactionCooldown')){Assert-KmcChunk7DisableEqual $m.$key $n.$key ('pending '+$key)};Assert-KmcChunk7Boolean $n.prepared $true
 $removal=@($o.allocationTrace.events|Where-Object{$_.state.actor-ceq$roles[2]-and$_.boundary-cin@('remove-unit-before','remove-unit-after')-and$_.frame-ge$pending.before.frame-and$_.frame-le$pending.after.frame}|Sort-Object sequence)
 if($removal.Count-ne2-or$removal[0].boundary-cne'remove-unit-before'-or$removal[1].boundary-cne'remove-unit-after'){throw 'Registered disable lacks one exact native rider removal window.'}
 $window=@($o.allocationTrace.events|Where-Object{$_.sequence-gt$removal[0].sequence-and$_.sequence-lt$removal[1].sequence}|Sort-Object sequence);$forfeit=@($window|Where-Object{$_.boundary-cin@('completion-force-to-end-before','completion-force-to-end-after')})
 if($forfeit.Count-ne4-or@($forfeit|Where-Object{$_.detail-cne'setCooldowns=True'}).Count-ne0-or(@($forfeit|ForEach-Object{$_.boundary.Substring(24)+':'+$_.state.actor})-join'|')-cne('before:'+$roles[2]+'|before:'+$roles[3]+'|after:'+$roles[3]+'|after:'+$roles[2])){throw 'Registered disable native forfeit is missing, repeated or not nested.'}
 if(@($window|Where-Object{$_.state.actor-ceq$roles[3]-and$_.boundary-cin@('cost-before','cost-after','actor-cost-before','actor-cost-after','admission-before','admission-after')}).Count-ne0){throw 'Registered disable rider removal admitted or charged pending mount work.'}
 foreach($id in @($roles[2],$roles[3])){if(@($window|Where-Object{$_.state.actor-ceq$id-and$_.boundary-ceq'turn-end-after'}).Count-ne1){throw 'Registered disable native removal did not end each forfeited actor exactly once.'}}
 Assert-KmcChunk7PermanentHorseRemovalRefusal $Request $o;Assert-KmcChunk7RegisteredDisableSequence $Request $o
 foreach($key in @('pairResidueAbsent','nativeCombatEnded','initiativeInputsRestored','firstPairAiRestored','controlsSettled','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$key $true}
 foreach($key in @('ownedHorseRemoved','nativeProgressionRequiresExternalWorkingRestoration','nativeSaveWrittenByThisDriver','delegatedPersistenceCleanup')){Assert-KmcChunk7Boolean $o.cleanup.$key $false}
}
