# Closed inherited schema-1 fixture route. Legacy Working readers remain exact.
Set-StrictMode -Version Latest
function Assert-KmcOwnedSchema1Object($Value,[string[]]$Keys,[string]$Description){
 if($null-eq$Value-or$Value-is[Array]-or$Value.GetType().FullName-cne'System.Management.Automation.PSCustomObject'){throw ($Description+' must be one JSON object.')}
 Assert-KmcExactProperties $Value $Keys $Description
}
function Assert-KmcOwnedSchema1NativeActor($Value){
 Assert-KmcOwnedSchema1Object $Value @('Id','Standard','Move','Swift','Initiative','Reaction','ReactionsRemaining','LastSurpriseTicks') 'owned native actor'
 Assert-KmcChunk7PersistenceNativeActor $Value
}
function Test-KmcOwnedSchema1Case([string]$Case){$Case-cin@('owned-schema1-manual','owned-schema1-resave-manual')}
function Test-KmcOwnedSchema1ColdRequest($Request){
 $has=if($Request-is[Collections.IDictionary]){$Request.Contains('persistenceCase')}else{$null-ne$Request.PSObject.Properties['persistenceCase']}
 $Request.scenario-ceq'chunk7-owned-two-pair-campaign'-and$has-and$Request.persistenceCase-ceq'owned-schema1-resave-manual'
}
function Assert-KmcOwnedSchema1Request($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 Assert-KmcChunk7OwnedPairsSelection $Request
 if(-not(Test-KmcChunk7OwnedPairsScenario $Request.scenario)-or-not(Test-KmcOwnedSchema1Case $Request.persistenceCase)){throw 'Owned schema-1 requires its closed inherited family.'}
 $cold=Test-KmcOwnedSchema1ColdRequest $Request
 $seed=Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request
 $seedCold=Test-KmcChunk7OwnedPairsSeedCheckpointLoad $Request
 Assert-KmcBootstrapInventoryString $Request.ownedPairs.case $(if($cold){if($seedCold){'owner-seed-checkpoint-load'}else{'persistence-load'}}else{if($seed){'owner-seed-checkpoint-save'}else{'persistence-save'}})
 $has=$null-ne$Request.PSObject.Properties['schema1DerivationSha256']
 if($cold){if(-not$has-or$Request.schema1DerivationSha256-isnot[string]-or$Request.schema1DerivationSha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Owned schema-1 cold input requires its exact metadata-copy receipt.'}}
 elseif($has){throw 'Owned schema-1 native source cannot select a historical derivative.'}
}
function Assert-KmcOwnedSchema1Archive($Archive,$Request,[bool]$Migrated=$false){
 Assert-KmcOwnedSchema1Object $Archive @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Pairs','Participations','Slots','Combat') 'owned schema-1 current graph'
 Assert-KmcOrdinaryInteger $Archive.SchemaVersion 3 3;Assert-KmcOrdinaryInteger $Archive.GameTimeTicks 0
 foreach($p in @(@('CampaignId',$Request.persistenceLoad.gameId),@('AreaId',$Request.persistenceLoad.area),@('Policy','rider-principal-distinct-native-v1'),@('RulesId','crpg-transport-v1'))){Assert-KmcBootstrapInventoryString $Archive.($p[0]) $p[1]}
 if($Archive.Pairs-isnot[Array]-or$Archive.Pairs.Count-ne1-or$Archive.Participations-isnot[Array]-or$Archive.Participations.Count-ne0-or$null-ne$Archive.Combat){throw 'Historical projection cannot discard another owner, participation or combat.'}
 $p=$Archive.Pairs[0];Assert-KmcOwnedSchema1Object $p @('PairId','Generation','ProfileId','Rider','Mount') 'owned schema-1 pair'
 if($p.PairId-isnot[string]-or$p.PairId-cnotmatch'^[a-f0-9]{32}$'-or$p.PairId-ceq('0'*32)){throw 'Owned schema-1 owner identity is invalid.'}
 Assert-KmcOrdinaryInteger $p.Generation 1;if($Migrated){Assert-KmcOrdinaryInteger $p.Generation 1 1}
 Assert-KmcBootstrapInventoryString $p.ProfileId 'medium-humanoid-horse-v1'
 Assert-KmcBootstrapInventoryString $p.Rider.Id $Request.ownedPairs.riderAId;Assert-KmcBootstrapInventoryString $p.Mount.Id $Request.ownedPairs.mountAId
 foreach($a in @($p.Rider,$p.Mount)){Assert-KmcOwnedSchema1NativeActor $a;foreach($key in @('Standard','Move','Swift','Initiative','Reaction')){if($a.$key-ne0){throw 'Owned schema-1 source retains native debt.'}}}
 if($Archive.Slots-isnot[Array]-or$Archive.Slots.Count-gt128){throw 'Historical slots are not bounded.'}
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($s in $Archive.Slots){Assert-KmcOwnedSchema1Object $s @('ActorId','Index','Kind') 'historical control slot';Assert-KmcOrdinaryGuid $s.ActorId;Assert-KmcOrdinaryInteger $s.Index 0 127;Assert-KmcOrdinaryInteger $s.Kind 1 4
  if(-not$seen.Add(([Guid]$s.ActorId).ToString('N')+':'+$s.Index)){throw 'Historical slots duplicate an actor/index.'}
 }
}
function ConvertTo-KmcOwnedSchema1Metadata($Archive,$Request){
 Assert-KmcOwnedSchema1Archive $Archive $Request
 # Schema 1 has no owner-generation fields. The unchanged decoder derives its
 # historical stable owner; source custody remains explicit in the receipt.
 $p=$Archive.Pairs[0]
 return [pscustomobject][ordered]@{SchemaVersion=1;CampaignId=$Archive.CampaignId;AreaId=$Archive.AreaId;GameTimeTicks=$Archive.GameTimeTicks;Policy=$Archive.Policy;RulesId=$Archive.RulesId;Mounted=$true;ProfileId=$p.ProfileId;Rider=$p.Rider;Mount=$p.Mount;Slots=$Archive.Slots}
}
function Assert-KmcOwnedSchema1Field($Field,$Request,[string]$Phase){
 Assert-KmcOwnedSchema1Object $Field @('phase','gameTicks','actors') 'owned schema-1 whole field';Assert-KmcBootstrapInventoryString $Field.phase $Phase;Assert-KmcOrdinaryInteger $Field.gameTicks 0
 if($Field.actors-isnot[Array]-or$Field.actors.Count-ne$Request.ownedPairs.partyIds.Count){throw 'Owned schema-1 full actor roster differs.'}
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($a in $Field.actors){Assert-KmcOwnedSchema1Object $a @('native','prepared','inCombat','commandsEmpty','queuedCommands') 'owned schema-1 native allocation'
  Assert-KmcOwnedSchema1NativeActor $a.native
  if($a.native.Id-cnotin$Request.ownedPairs.partyIds-or-not$seen.Add($a.native.Id)){throw 'Owned schema-1 actor is duplicated or foreign.'}
  if($a.prepared-isnot[bool]){throw 'Native prepared state must be observed.'};Assert-KmcChunk7Boolean $a.inCombat $false;Assert-KmcChunk7Boolean $a.commandsEmpty $true;Assert-KmcOrdinaryInteger $a.queuedCommands 0 0
  foreach($key in @('Standard','Move','Swift','Initiative','Reaction')){if($a.native.$key-ne0){throw 'Owned schema-1 field retains native action debt.'}}
 }
}
function Assert-KmcOwnedSchema1Snapshot($Snapshot,$Archive,$Request,[string]$Label,[int]$Writes,[bool]$Cold){
 Assert-KmcBootstrapInventoryString $Snapshot.label $Label
 Assert-KmcChunk7NativeCounters $Snapshot @('semanticRestores','presentationRestores','nativePostLoads','snapshots','nativeCastRequests','gameTicks')
 Assert-KmcChunk7NativeCounters $Snapshot.controls @('DuplicateFactCount','ManagedHotbarSlotCount');Assert-KmcChunk7Boolean $Snapshot.controls.SerializationSuspended $false
 $seedSource=-not$Cold-and(Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request)
 $restores=if($Cold){2}elseif($seedSource){4}else{0};$presentation=if($Cold){1}elseif($seedSource){2}else{0};$casts=if($Cold){0}else{1}
 if($Snapshot.semanticRestores-ne$restores-or$Snapshot.presentationRestores-ne$presentation-or$Snapshot.nativePostLoads-ne$restores-or$Snapshot.snapshots-ne$Writes-or$Snapshot.nativeCastRequests-ne$casts-or$Snapshot.controls.DuplicateFactCount-ne0-or$Snapshot.controls.ManagedHotbarSlotCount-ne$Archive.Slots.Count){throw 'Owned schema-1 native restoration/write/input/control counts differ.'}
 if($Cold){Assert-KmcBootstrapInventoryString $Snapshot.archivePhase 'all-pair-presentation-restored'}
 Assert-KmcChunk7Schema1Owners $Snapshot.owners $Archive
 if($Snapshot.actors-isnot[Array]-or$Snapshot.actors.Count-ne4){throw 'Both inherited pair roles must remain observed.'}
 $ids=Get-KmcChunk7OwnedPairIds $Request;$seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
 foreach($row in $Snapshot.actors){Assert-KmcOwnedSchema1NativeActor $row.native;Assert-KmcChunk7Boolean $row.inCombat $false
  if($row.native.Id-cnotin$ids-or-not$seen.Add($row.native.Id)){throw 'Owned schema-1 snapshot changed a selected actor.'}
 }
 foreach($a in @($Archive.Pairs[0].Rider,$Archive.Pairs[0].Mount)){$row=@($Snapshot.actors|Where-Object{$_.native.Id-ceq$a.Id});Assert-KmcChunk7Schema1Debt $row[0].native $a $Snapshot.gameTicks $Archive.GameTimeTicks}
}
function Assert-KmcOwnedSchema1Observation($Request,$Artifact){
 Assert-KmcOwnedSchema1Request $Request;Assert-KmcChunk7InheritedEvidence $Request $Artifact.observations
 Assert-KmcBootstrapInventoryString $Artifact.status 'PASS';$o=$Artifact.observations;$cold=Test-KmcOwnedSchema1ColdRequest $Request
 Assert-KmcBootstrapInventoryString $o.checkpoint $Request.persistenceCase;Assert-KmcBootstrapInventoryString $o.inheritedManualSha256 $Request.ownedPairs.sourceArchiveSha256
 if($null-ne$o.sourceWorkingSha256){throw 'Inherited schema-1 cannot claim Working identity.'}
 Assert-KmcOrdinaryInteger $o.nativeSchema 3 3;Assert-KmcOrdinaryInteger $o.nativeCapacity 2 2;Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue)
 Assert-KmcChunk7Boolean $o.cold $cold;foreach($name in @('combatDebtProof','ordinaryRespecProven','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $o.$name $false}
 Assert-KmcChunk7OwnedOutputEvidence $Request $o
 $v=$o.ownedSchema1;$keys=@('contract','syntheticMetadata','sourceArchiveSchema','fields');if($cold){$keys+=@('sourceCheckpointRunId','sourceCheckpointArtifactSha256','originalNativeArchiveSha256','sourceField')}
 Assert-KmcOwnedSchema1Object $v $keys 'owned schema-1 provenance';Assert-KmcBootstrapInventoryString $v.contract 'owned-schema1-fixture-v1';Assert-KmcChunk7Boolean $v.syntheticMetadata $cold
 Assert-KmcOrdinaryInteger $v.sourceArchiveSchema $(if($cold){1}else{3}) $(if($cold){1}else{3})
 $phases=if($cold){@('cold-restored','before-resave','resave-settled')}else{@('source-before-save','source-settled')}
 if($v.fields-isnot[Array]-or$v.fields.Count-ne$phases.Count){throw 'Owned schema-1 full-field preservation observations are incomplete.'}
 $prior=if($cold){$v.sourceField}else{$v.fields[0]};if($cold){Assert-KmcOwnedSchema1Field $prior $Request 'source-settled'}
 for($i=0;$i-lt$phases.Count;$i++){Assert-KmcOwnedSchema1Field $v.fields[$i] $Request $phases[$i]
  if($v.fields[$i].gameTicks-lt$prior.gameTicks-or(ConvertTo-KmcChunk7Canonical $v.fields[$i].actors)-cne(ConvertTo-KmcChunk7Canonical $prior.actors)){throw 'Owned schema-1 operation changed native allocations/prepared state or reversed time.'};$prior=$v.fields[$i]
 }
 if($o.movements-isnot[Array]-or$o.movements.Count-ne0-or$o.inputs-isnot[Array]){throw 'Historical migration must not replay movement.'}
 $write=$o.nativeWrite;$archive=$write.archive;Assert-KmcOwnedSchema1Archive $archive $Request $cold
 Assert-KmcBootstrapInventoryString $write.nativeType 'Manual';Assert-KmcBootstrapInventoryString $write.operation 'None';Assert-KmcChunk7Boolean $write.nativeCallback $true;Assert-KmcOrdinaryInteger $write.snapshotCount 1 1;Assert-KmcOrdinaryInteger $write.length 1 268435456
 if($write.sha256-isnot[string]-or$write.sha256-cnotmatch'^[a-f0-9]{64}$'){throw 'Native schema-1 output hash is invalid.'}
 Assert-KmcBootstrapInventoryString $o.saveRequested.nativeType 'Manual';Assert-KmcBootstrapInventoryString $o.saveRequested.nativeName $write.nativeName;Assert-KmcOrdinaryInteger $o.saveRequested.snapshotCount 0 0
 foreach($key in @('nativeSaveAllowed','newSlot')){Assert-KmcChunk7Boolean $o.saveRequested.$key $true}
 if($cold){
  if($o.inputs.Count-ne0){throw 'Native historical restoration replayed Mount input.'}
  Assert-KmcOrdinaryInteger $write.sourceSchema 3 3
  foreach($key in @('sourceCheckpointRunId','sourceCheckpointArtifactSha256')){$selectionKey=if($key-ceq'sourceCheckpointRunId'){'checkpointSourceRunId'}else{'checkpointSourceArtifactSha256'};Assert-KmcBootstrapInventoryString $v.$key $Request.ownedPairs.$selectionKey}
  if($v.originalNativeArchiveSha256-isnot[string]-or$v.originalNativeArchiveSha256-cnotmatch'^[a-f0-9]{64}$'-or$v.originalNativeArchiveSha256-ceq$Request.persistenceLoad.sha256){throw 'Synthetic historical input must be distinct from its recorded native schema-3 source.'}
  $m=$o.schema1Migration;$old=$m.nativeInputMetadata;$imported=$m.migratedArchive;Assert-KmcOwnedSchema1Archive $imported $Request $true
  Assert-KmcOwnedSchema1Object $old @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Mounted','ProfileId','Rider','Mount','Slots') 'owned native historical metadata'
  if((ConvertTo-KmcChunk7Canonical $old)-cne(ConvertTo-KmcChunk7Canonical (ConvertTo-KmcOwnedSchema1Metadata $imported $Request))){throw 'Historical decoder changed source identity, allocations or any representable slot.'}
  Assert-KmcBootstrapInventoryString $imported.Pairs[0].PairId (Get-KmcChunk7Schema1Owner $old)
  Assert-KmcOrdinaryInteger $m.inputSchema 1 1;Assert-KmcOrdinaryInteger $m.inputLength $Request.persistenceLoad.length $Request.persistenceLoad.length
  Assert-KmcBootstrapInventoryString $m.inputSha256 $Request.persistenceLoad.sha256;Assert-KmcBootstrapInventoryString $m.derivationReceiptSha256 $Request.schema1DerivationSha256
  if((ConvertTo-KmcChunk7Canonical $o.loadedDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)-or(ConvertTo-KmcChunk7Canonical $o.loadedArchive)-cne(ConvertTo-KmcChunk7Canonical $imported)){throw 'Cold input descriptor or decoded graph differs.'}
  foreach($key in @('CampaignId','AreaId','Policy','RulesId','Pairs','Participations','Slots','Combat')){if((ConvertTo-KmcChunk7Canonical $archive.$key)-cne(ConvertTo-KmcChunk7Canonical $imported.$key)){throw 'Native resave changed the migrated graph.'}}
  Assert-KmcChunk7Schema1World $m.restoredWorld $imported;Assert-KmcChunk7Schema1World $m.resavedWorld $archive;Assert-KmcChunk7Schema1Probe $o.coldRestoration $imported $Request
  $specs=@(@('schema1-native-restored-before-resave',0,$imported),@('schema1-native-resave-complete',1,$archive))
 }else{
  if(Test-KmcChunk7OwnedPairsSeedCheckpointSave $Request){
   Assert-KmcChunk7OwnerSeedCheckpointStart $Request $o $archive
   $specs=@(,@('native-write-complete',1,$archive))
  }else{
  if($o.inputs.Count-ne1){throw 'The one-owner source requires exactly one ordinary native Mount.'};$row=$o.inputs[0]
  foreach($pair in @(@('label','mount-mammoth'),@('casterId',$Request.ownedPairs.riderAId),@('targetId',$Request.ownedPairs.mountAId),@('abilityGuid','f053faad986631688defa003cd7bda0e'))){Assert-KmcBootstrapInventoryString $row.($pair[0]) $pair[1]}
  Assert-KmcOrdinaryInteger $row.nativeCastDelta 1 1;Assert-KmcOrdinaryInteger $row.nativeRefusalDelta 0 0;Assert-KmcOrdinaryInteger $row.generationAtInit 0
  if($row.control-isnot[string]-or[string]::IsNullOrEmpty($row.control)-or$row.selectionAfterInput-isnot[string]-or$row.selectionAfterInput-ceq$row.casterId){throw 'Source Mount lost its native control/selection observation.'}
  Assert-KmcChunk7Boolean $row.clicked $true;Assert-KmcChunk7Boolean $row.terminal $true
  $specs=@(,@('native-write-complete',1,$archive))
  }
 }
 foreach($spec in $specs){$rows=@($o.snapshots|Where-Object label -CEQ $spec[0]);if($rows.Count-ne1){throw 'Historical native boundary snapshot is absent or duplicate.'};Assert-KmcOwnedSchema1Snapshot $rows[0] $spec[2] $Request $spec[0] $spec[1] $cold}
 foreach($key in @('pairsClear','controlsSettled','saveCustodyClear','selectionRestored','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$key $true}
 foreach($key in @('sourceHorseRemoved','nativeProgressionRequiresExternalWorkingRestoration','ordinaryRespecProven')){Assert-KmcChunk7Boolean $o.cleanup.$key $false}
 if($o.cleanup.world.owners-isnot[Array]-or$o.cleanup.world.owners.Count-ne0){throw 'Historical fixture cleanup retained mounted ownership.'}
}
