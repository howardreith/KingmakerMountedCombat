# Load-only current-format children of accepted native historical resaves.
Set-StrictMode -Version Latest
function Assert-KmcChunk7CurrentMigrationObservation($Request,$Artifact){
 Assert-KmcChunk7OwnedPairsDescriptor $Request
 if(-not(Test-KmcChunk7OwnedPairsSeedCheckpointLoad $Request)-or-not(Test-KmcChunk7OwnerSeedCurrentCheckpoint $Request.persistenceCase)){throw 'Current-format continuation belongs only to its selected owner-seed cold child.'}
 if($null-ne$Request.PSObject.Properties['schema1DerivationSha256']-or$null-ne$Request.PSObject.Properties['schema2DerivationSha256']){throw 'Current-format input must not select a historical metadata derivative.'}
 Assert-KmcBootstrapInventoryString $Artifact.status 'PASS';$o=$Artifact.observations;$s=$Request.ownedPairs
 Assert-KmcChunk7InheritedEvidence $Request $o
 Assert-KmcBootstrapInventoryString $o.checkpoint $Request.persistenceCase
 Assert-KmcBootstrapInventoryString $o.inheritedManualSha256 $s.sourceArchiveSha256
 if($null-ne$o.sourceWorkingSha256){throw 'Current-format inherited input cannot claim Working identity.'}
 foreach($spec in @(@('nativeSchema',3),@('nativeCapacity',2))){Assert-KmcOrdinaryInteger $o.($spec[0]) $spec[1] $spec[1]}
 Assert-KmcOrdinaryInteger $o.loadedSourceSchema 3 3
 Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue);Assert-KmcChunk7Boolean $o.cold $true
 foreach($field in @('ordinaryRespecProven','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $o.$field $false}
 foreach($field in @('nativeWrite','saveRequested','ownedOutputAdmission','ownedOutputCompletion','schema1Migration','schema2Migration','ownedSchema1','mountedStart','ownerSeedCheckpointSource','ownerSeedCheckpointPreparation')){if($null-ne$o.PSObject.Properties[$field]){throw 'Current-format load-only child supplied producer or historical-migration evidence.'}}
 if($o.inputs-isnot[Array]-or$o.inputs.Count-ne0){throw 'Current-format continuation replayed native relationship input.'}
 $m=$o.currentFormatMigration
 Assert-KmcOwnedSchema1Object $m @('contract','parentCheckpoint','parentRunId','parentArtifactSha256','parentProcessId','inputSchema','inputSha256','inputLength','parentNativeWriteSha256','loadedEqualsParentNativeWrite','restoredWorld') 'current-format migration lineage'
 Assert-KmcBootstrapInventoryString $m.contract 'owner-seed-current-format-load-v1'
 Assert-KmcBootstrapInventoryString $m.parentCheckpoint (Get-KmcChunk7OwnerSeedCurrentParentCase $Request.persistenceCase)
 Assert-KmcBootstrapInventoryString $m.parentRunId $s.checkpointSourceRunId;Assert-KmcBootstrapInventoryString $m.parentArtifactSha256 $s.checkpointSourceArtifactSha256
 Assert-KmcOrdinaryInteger $m.parentProcessId 1 ([int]::MaxValue);if($m.parentProcessId-eq$o.processId){throw 'Current-format child must cold-load in a process separate from its native resave parent.'}
 Assert-KmcOrdinaryInteger $m.inputSchema 3 3;Assert-KmcOrdinaryInteger $m.inputLength $Request.persistenceLoad.length $Request.persistenceLoad.length
 foreach($field in @('inputSha256','parentNativeWriteSha256')){Assert-KmcBootstrapInventoryString $m.$field $Request.persistenceLoad.sha256}
 Assert-KmcChunk7Boolean $m.loadedEqualsParentNativeWrite $true
 if((ConvertTo-KmcChunk7Canonical $o.loadedDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)){throw 'Current-format child substituted its exact native resave descriptor.'}
 $archive=$o.loadedArchive
 if($Request.persistenceCase-ceq'owned-schema2-pending-current-manual'){
  Assert-KmcChunk7CombatPersistenceObservation $Request $Artifact
  Assert-KmcChunk7NativeWorldRestore $m.restoredWorld $archive $true
  return
 }
 Assert-KmcChunk7Boolean $o.combatDebtProof $false
 Assert-KmcOwnedSchema1Archive $archive $Request $true
 Assert-KmcChunk7Schema1World $m.restoredWorld $archive
 Assert-KmcChunk7Schema1Probe $o.coldRestoration $archive $Request
 $labels=@('schema1-current-restored-before-continuation','schema1-current-continuation-complete')
 foreach($label in $labels){$snapshot=@($o.snapshots|Where-Object label -CEQ $label);if($snapshot.Count-ne1){throw 'Current-format single-owner load omitted its native continuation boundary.'};Assert-KmcOwnedSchema1Snapshot $snapshot[0] $archive $Request $label 0 $true}
 if($o.movements-isnot[Array]-or$o.movements.Count-ne1){throw 'Current-format single-owner load requires exactly one real native continuation.'}
 $move=$o.movements[0]
 foreach($spec in @(@('label','schema1-current-first-pair'),@('selectedId',$s.riderAId),@('executorId',$s.mountAId))){Assert-KmcBootstrapInventoryString $move.($spec[0]) $spec[1]}
 foreach($field in @('terminal','otherCommandsUnchanged')){Assert-KmcChunk7Boolean $move.$field $true}
 foreach($point in @($move.start,$move.end,$move.goal)){Assert-KmcOwnedSchema1Object $point @('x','y','z') 'native continuation position';foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $point.$axis)){throw 'Native continuation position is nonnumeric or nonfinite.'}}}
 $distance=[Math]::Sqrt([Math]::Pow([double]$move.end.x-$move.start.x,2)+[Math]::Pow([double]$move.end.y-$move.start.y,2)+[Math]::Pow([double]$move.end.z-$move.start.z,2))
 $residual=[Math]::Sqrt([Math]::Pow([double]$move.end.x-$move.goal.x,2)+[Math]::Pow([double]$move.end.y-$move.goal.y,2)+[Math]::Pow([double]$move.end.z-$move.goal.z,2))
 if(-not(Test-KmcChunk7FiniteNumber $move.distance)-or-not(Test-KmcChunk7FiniteNumber $move.residual)-or[Math]::Abs($distance-$move.distance)-gt0.0001-or[Math]::Abs($residual-$move.residual)-gt0.0001-or$distance-lt0.75-or$residual-gt1){throw 'Current-format native movement missed its measured endpoint.'}
 foreach($field in @('pairsClear','controlsSettled','saveCustodyClear','selectionRestored','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}
 foreach($field in @('sourceHorseRemoved','nativeProgressionRequiresExternalWorkingRestoration','ordinaryRespecProven')){Assert-KmcChunk7Boolean $o.cleanup.$field $false}
 if($o.cleanup.world.owners-isnot[Array]-or$o.cleanup.world.owners.Count-ne0){throw 'Current-format continuation cleanup retained native custody.'}
}
function Assert-KmcChunk7CurrentMigrationParent($Request,$ParentArtifact,$ColdArtifact){
 Assert-KmcChunk7CurrentMigrationObservation $Request $ColdArtifact
 $p=$ParentArtifact.observations;$c=$ColdArtifact.observations;$m=$c.currentFormatMigration
 Assert-KmcBootstrapInventoryString $ParentArtifact.runId $Request.ownedPairs.checkpointSourceRunId;Assert-KmcBootstrapInventoryString $ParentArtifact.status 'PASS'
 Assert-KmcBootstrapInventoryString $p.checkpoint (Get-KmcChunk7OwnerSeedCurrentParentCase $Request.persistenceCase);Assert-KmcChunk7Boolean $p.cold $true
 Assert-KmcOrdinaryInteger $p.processId $m.parentProcessId $m.parentProcessId
 Assert-KmcOrdinaryInteger $p.nativeWrite.sourceSchema 3 3
 Assert-KmcBootstrapInventoryString $p.nativeWrite.sha256 $Request.persistenceLoad.sha256;Assert-KmcOrdinaryInteger $p.nativeWrite.length $Request.persistenceLoad.length $Request.persistenceLoad.length
 if((ConvertTo-KmcChunk7Canonical $p.nativeWrite.archive)-cne(ConvertTo-KmcChunk7Canonical $c.loadedArchive)){throw 'Current-format load did not restore its accepted parent native resave graph exactly.'}
 if($Request.persistenceCase-ceq'owned-schema1-current-manual'){Assert-KmcOrdinaryInteger $p.schema1Migration.inputSchema 1 1}else{Assert-KmcOrdinaryInteger $p.loadedSourceSchema 2 2}
}
