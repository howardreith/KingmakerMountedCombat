# Generated JSON evidence for the actual native preparation reader. No native
# action, archive write or fixture qualification is represented by this helper.
function New-KmcOwnerSeedControlFactsFixture([string[]]$Roles,[bool]$After){
 $guids=@('f053faad986631688defa003cd7bda0e','3af2b81f4d72bbb30501fa730fcdf36e','27364df661b3c121eabb97a31aa73a83','f88a50d6fdbebbd709c3e323d2f52f5e','d79eaec224a7a832e738eb81baef9d49')
 $rows=@(foreach($role in 0..3){foreach($kind in 1..5){
  $count=if($role-eq0-and$kind-in@(2,3,4)){1}elseif($role-eq2-and$(if($After){$kind-eq1}else{$kind-in@(2,3,4)})){1}else{0}
  [pscustomobject][ordered]@{actorId=$Roles[$role];kind=$kind;blueprintGuid=$guids[$kind-1];totalFacts=$count;activeFacts=$count;facts=@(if($count){[pscustomobject][ordered]@{active=$true;factBlueprintExact=$true;dataPresent=$true;dataBlueprintExact=$true;casterExact=$true;nativeLookupExact=$true}})}
 }})
 [pscustomobject][ordered]@{contract='owner-seed-control-facts-v1';rows=$rows;totalFacts=$(if($After){4}else{6});activeFacts=$(if($After){4}else{6})}
}
function Set-KmcOwnerSeedNativePreparationFixture($Value,[bool]$Pending){
 $r=$Value.request;$o=$Value.artifact.observations;$m=$o.mountedStart;$s=$r.ownedPairs
 if($Pending){
  $old='abcdefabcdef4def8defabcdefabcdef';$m.owners[1].ownerId=$old;$m.loadedArchive.Pairs[1].PairId=$old
  $m.owners[1].generation=$o.inputs[-1].generationAtInit;$m.loadedArchive.Pairs[1].Generation=$m.owners[1].generation
 }
 $capture={param($label,$casts,$second)
  $controls=Copy-Value $o.snapshots[0].controls;$controls.ManagedHotbarSlotCount=4;$controls.DuplicateFactCount=0;$controls.SerializationSuspended=$false
  $controls|Add-Member ExactFactCount $(if($second){6}else{4}) -Force
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
  slotsBefore=@($m.slots|ForEach-Object{[pscustomobject][ordered]@{ActorId=$_.actorId;Index=$_.index;Kind=$_.kind}});controlFactsBefore=(New-KmcOwnerSeedControlFactsFixture @(Get-KmcChunk7OwnedPairIds $r) $false);input=(Copy-Value $input);after=$after
  slotsAfter=@($m.slots|Where-Object actorId -CEQ $s.riderAId|ForEach-Object{[pscustomobject][ordered]@{ActorId=$_.actorId;Index=$_.index;Kind=$_.kind}});controlFactsAfter=(New-KmcOwnerSeedControlFactsFixture @(Get-KmcChunk7OwnedPairIds $r) $true)}
 $o|Add-Member ownerSeedCheckpointPreparation $prep -Force
 $o.snapshots=@($o.snapshots|Where-Object label -CNE 'before-native-save');$captured=Copy-Value $after;$captured.label='before-native-save';$o.snapshots=@($captured)+@($o.snapshots)
 if($Pending){
  $mount=Copy-Value $o.inputs[-1];$o.inputs=@($input,$mount);$pair=$o.nativeWrite.archive.Pairs|Where-Object{$_.Rider.Id-ceq$s.riderBId}
  $prep|Add-Member remountedSecondOwner ([pscustomobject][ordered]@{ownerId=(ConvertTo-KmcChunk7PairIdN $pair.PairId);generation=$pair.Generation;riderId=$s.riderBId;mountId=$s.mountBId})
 }else{$o.inputs=@($input)}
}
