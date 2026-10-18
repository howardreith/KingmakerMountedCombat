# Generated JSON evidence for the actual native preparation reader. No native
# action, archive write or fixture qualification is represented by this helper.
function Set-KmcOwnerSeedNativePreparationFixture($Value,[bool]$Pending){
 $r=$Value.request;$o=$Value.artifact.observations;$m=$o.mountedStart;$s=$r.ownedPairs
 if($Pending){
  $old='abcdefabcdef4def8defabcdefabcdef';$m.owners[1].ownerId=$old;$m.loadedArchive.Pairs[1].PairId=$old
  $m.owners[1].generation=$o.inputs[-1].generationAtInit;$m.loadedArchive.Pairs[1].Generation=$m.owners[1].generation
 }
 $capture={param($label,$casts,$second)
  $controls=Copy-Value $o.snapshots[0].controls;$controls.ManagedHotbarSlotCount=if($second){4}else{2};$controls.DuplicateFactCount=0;$controls.SerializationSuspended=$false
  [pscustomobject][ordered]@{label=$label;frame=$(if($casts){94}else{90});gameTicks=([long]$m.loadedArchive.GameTimeTicks+$(if($casts){5000000L}else{0L}))
   owners=@($m.owners|Where-Object{$second-or$_.riderId-ceq$s.riderAId}|ForEach-Object{[pscustomobject][ordered]@{ownerId=$_.ownerId;generation=$_.generation;riderId=$_.riderId;mountId=$_.mountId;state='Active'}})
   actors=@($m.loadedArchive.Pairs|ForEach-Object{$_.Rider;$_.Mount}|ForEach-Object{[pscustomobject][ordered]@{native=(Copy-Value $_);prepared=$false;inCombat=$false}})
   controls=$controls;semanticRestores=4;presentationRestores=2;nativePostLoads=4;snapshots=0;nativeCastRequests=$casts;archivePhase='all-pair-presentation-restored'}
 }
 $before=&$capture 'owner-seed-before-dismount-second-pair' 0 $true;$after=&$capture 'owner-seed-after-dismount-second-pair' 1 $false
 $input=[pscustomobject][ordered]@{label='owner-seed-native-dismount-second-pair';before=(Copy-Value $before);casterId=$s.riderBId;targetId=$s.riderBId;abilityGuid='3af2b81f4d72bbb30501fa730fcdf36e';clicked=$true;nativeCastDelta=1;nativeRefusalDelta=0;generationAtInit=$m.owners[1].generation;control='synthetic-preparation-control';selectionAfterInput=$s.riderAId;terminal=$true;after=(Copy-Value $after)}
 $input.before.label='input-before';$input.after.label='input-after'
 foreach($field in @('secondRiderOwnerAbsent','secondMountOwnerAbsent','secondPairUnmounted','secondPairNoPreparedResidue','secondPairCanRelease','actorsIdle')){$after|Add-Member $field $true}
 foreach($field in @('unsettledPreparation','unsettledTransition','secondPairCommandsRetained','worldSerializationBusy','saveBusy')){$after|Add-Member $field $false}
 $prep=[pscustomobject][ordered]@{contract='owner-seed-checkpoint-native-preparation-v1';checkpoint=$r.persistenceCase;before=$before
  slotsBefore=@($m.slots|ForEach-Object{[pscustomobject][ordered]@{ActorId=$_.actorId;Index=$_.index;Kind=$_.kind}});input=(Copy-Value $input);after=$after
  slotsAfter=@($m.slots|Where-Object actorId -CEQ $s.riderAId|ForEach-Object{[pscustomobject][ordered]@{ActorId=$_.actorId;Index=$_.index;Kind=$_.kind}})}
 $o|Add-Member ownerSeedCheckpointPreparation $prep -Force
 $o.snapshots=@($o.snapshots|Where-Object label -CNE 'before-native-save');$captured=Copy-Value $after;$captured.label='before-native-save';$o.snapshots=@($captured)+@($o.snapshots)
 if($Pending){
  $mount=Copy-Value $o.inputs[-1];$o.inputs=@($input,$mount);$pair=$o.nativeWrite.archive.Pairs|Where-Object{$_.Rider.Id-ceq$s.riderBId}
  $prep|Add-Member remountedSecondOwner ([pscustomobject][ordered]@{ownerId=(ConvertTo-KmcChunk7PairIdN $pair.PairId);generation=$pair.Generation;riderId=$s.riderBId;mountId=$s.mountBId})
 }else{$o.inputs=@($input)}
}
