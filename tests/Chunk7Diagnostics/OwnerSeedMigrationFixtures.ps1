# Generated reader fixtures only; these objects make no native or archive qualification claim.
function Set-KmcOwnerSeedSchema2DebtDiagnosticFixture($Value){
 $r=$Value.request;$o=$Value.artifact.observations;$p=$o.coldRestoration
 if(-not(Test-KmcChunk7CombatDebtDiagnosticRequest $r)-or@($p.hooks).Count-ne6-or
    $null-ne$o.PSObject.Properties['combatDebtChecks']-or$null-ne$p.PSObject.Properties['debtDiagnostics']){
  throw 'Generated debt fixture belongs only to the exact fresh owner-seed schema2 cold/resave selection.'
 }
 foreach($spec in @(@('Kingmaker.EntitySystem.Entities.UnitEntityData','JoinCombat','06008355','DebtJoinBefore','DebtJoinAfter'),@('Kingmaker.Controllers.Combat.UnitCombatPrepareController','Tick','0600936F','DebtPrepareBefore','DebtPrepareAfter'))){
  $p.hooks+=@([pscustomobject][ordered]@{declaringType=$spec[0];method=$spec[1];token=$spec[2];moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';prefix=$spec[3];postfix=$spec[4]})
 }
 # A passing paused synthetic world may have no admitted native method windows.
 # This does not invent JoinCombat/Prepare execution or causal native evidence.
 $p|Add-Member debtDiagnostics ([pscustomobject][ordered]@{contract='chunk7-native-debt-boundary-observer-v1';runId=$r.runId;events=@();maximumEvents=128;errors=0;dropped=0;openCalls=0;requestUnchanged=$true;complete=$true;scope='exact-schema2-cold-after-presentation';methodEntryMayBeRefusedByProductionPrefix=$true})
 $loaded=@($o.snapshots|Where-Object label -CEQ 'cold-restored-before-input')
 $resaved=@($o.snapshots|Where-Object label -CEQ 'schema2-native-resave-before-continuation')
 if($loaded.Count-ne1-or$resaved.Count-ne1){throw 'Synthetic debt fields require their existing exact cold/resave boundaries.'}
 $checks=@();$names=@('Standard','Move','Swift','Initiative','Reaction','ReactionsRemaining','LastSurpriseTicks')
 # Actual C# call sites: before Bind, ObserveCombatCold assertion, request-resave
 # assertion, and both imported/new archive assertions at native completion.
 for($j=0;$j-lt5;$j++){
  $archive=if($j-eq4){$o.nativeWrite.archive}else{$o.loadedArchive}
  $frame=if($j-lt3){$loaded[0].frame}else{$resaved[0].frame}
  $ticks=if($j-eq2){$o.saveRequested.gameTicks}elseif($j-lt2){$loaded[0].gameTicks}else{$resaved[0].gameTicks}
  $elapsed=[double]([decimal]$ticks-[decimal]$archive.GameTimeTicks)/10000000.0;$window=$elapsed-ge0-and$elapsed-le30
  $rows=@();$states=@()
  foreach($actor in $archive.Combat.Actors){
   $native=$actor.Native;$fields=@()
   for($k=0;$k-lt7;$k++){$name=$names[$k];$before=$native.$name;$actual=$native.$name
    $ok=if($k-lt5){$a=[double][single]$before;$b=[double][single]$actual;$b-le($a+0.001)-and($b+$elapsed*1.1+0.001)-ge$a}else{[decimal]$before-eq[decimal]$actual}
    $fields+=@([pscustomobject][ordered]@{field=$name;saved=$before;actual=$actual;passed=$ok})
   }
   $rows+=@([pscustomobject][ordered]@{actorId=$native.Id;saved=(Copy-Value $native);actual=(Copy-Value $native);fields=$fields;passed=(@($fields|Where-Object passed -EQ $false).Count-eq0)})
   $states+=@([pscustomobject][ordered]@{actorId=$native.Id;inCombat=$actor.InCombat;prepared=$actor.Prepared})
  }
  $checks+=@([pscustomobject][ordered]@{contract='chunk7-native-combat-debt-observation-v1';phase=$(if($j-eq0){'cold-observe-before-binding'}else{'assert-combat-debt'});frame=$frame;gameTicks=$ticks;savedGameTicks=$archive.GameTimeTicks;elapsedSeconds=$elapsed;windowAccepted=$window;actors=$rows;passed=($window-and@($rows|Where-Object passed -EQ $false).Count-eq0);paused=$true;archiveRestorePhase='all-pair-presentation-restored';restorationPending=$false;actorStates=$states})
 }
 $o|Add-Member combatDebtChecks $checks
}
function Set-KmcOwnerSeedSchema1Names($Value,[string]$InputName,[string]$InputLeaf,[string]$OutputName,[string]$OutputLeaf,[int]$OutputNumber){
 $r=$Value.request;$o=$Value.artifact.observations;$old=$r.persistenceLoad.fileName;$oldOutput=$o.ownedOutputAdmission.leaf
 $r.persistenceLoad.internalName=$InputName;$r.persistenceLoad.fileName=$InputLeaf
 if($o.PSObject.Properties['loadedDescriptor']){$o.loadedDescriptor=Copy-Value $r.persistenceLoad}
 foreach($row in @($o.ownedOutputAdmission.nativeInventory)+@($o.ownedOutputCompletion.nativeInventory)){
  if($row.fileName-ceq$old){$row.fileName=$InputLeaf;$row.nativeName=$InputName}elseif($row.fileName-ceq$oldOutput){$row.fileName=$OutputLeaf;$row.nativeName=$OutputName}
 }
 $o.ownedOutputAdmission.number=$OutputNumber;$o.ownedOutputAdmission.leaf=$OutputLeaf
 foreach($authority in @($o.ownedOutputAdmission.authority,$o.ownedOutputCompletion.authority)){$authority.number=$OutputNumber;$authority.leaf=$OutputLeaf;$authority.nativeName=$OutputName}
 $o.nativeWrite.path=$o.nativeWrite.path.Replace($oldOutput,$OutputLeaf);$o.nativeWrite.nativeName=$OutputName;$o.saveRequested.nativeName=$OutputName
}
function New-KmcOwnerSeedSchema1SourceFixture{
 $v=Copy-Value (Read-KmcJson (Join-Path $repo 'tests/Chunk7Diagnostics/OwnedSchema1EvidenceFixtures.json')).fixtures[0]
 $r=$v.request;$o=$v.artifact.observations;$base=New-KmcOwnerSeedCheckpointSaveFixture;$s=New-KmcOwnerSeedCheckpointSelection $r.ownedPairs 'owner-seed-checkpoint-save' $r.persistenceLoad.sha256 'synthetic-owner-seed-cold'
 $r.ownedPairs=$s;Set-KmcOwnerSeedDerivedLabels $o $s
 Set-KmcOwnerSeedSchema1Names $v 'KMC_C7_SEED_WORLD' 'Manual_303_KMC_C7_SEED_WORLD.zks' 'KMC_C7_SEED_CHECKPOINT' 'Manual_304_KMC_C7_SEED_CHECKPOINT.zks' 304
 $o.nativeWrite.archive.Slots=@($o.nativeWrite.archive.Slots|Where-Object ActorId -CEQ $s.riderAId)
 $o|Add-Member mountedStart (Copy-Value $base.artifact.observations.mountedStart) -Force
 $o|Add-Member ownerSeedCheckpointSource (Copy-Value $base.artifact.observations.ownerSeedCheckpointSource) -Force
 $o.ownerSeedCheckpointSource.checkpoint=$r.persistenceCase
 foreach($snapshot in $o.snapshots){$snapshot.semanticRestores=4;$snapshot.presentationRestores=2;$snapshot.nativePostLoads=4;$snapshot.nativeCastRequests=1;$snapshot.controls.ManagedHotbarSlotCount=2;$snapshot.controls|Add-Member ExactFactCount 4 -Force}
 Set-KmcOwnerSeedNativePreparationFixture $v $false
 $v.name='persistence-owner-seed-schema1-source';$v
}
function New-KmcOwnerSeedSchema1ColdFixture{
 $source=New-KmcOwnerSeedSchema1SourceFixture
 $v=Copy-Value (Read-KmcJson (Join-Path $repo 'tests/Chunk7Diagnostics/OwnedSchema1EvidenceFixtures.json')).fixtures[1]
 $r=$v.request;$o=$v.artifact.observations;$s=New-KmcOwnerSeedCheckpointSelection $r.ownedPairs 'owner-seed-checkpoint-load' $source.request.ownedPairs.sourceArchiveSha256 $source.request.runId;$s.expectedNativeWriteCount=1;$r.ownedPairs=$s;$o.processId=$source.artifact.observations.processId+1
 Set-KmcOwnerSeedDerivedLabels $o $s
 Set-KmcOwnerSeedSchema1Names $v 'KMC_C7_SEED_CHECKPOINT' 'Manual_304_KMC_C7_SEED_CHECKPOINT.zks' 'KMC_C7_SCHEMA1' 'Manual_305_KMC_C7_SCHEMA1.zks' 305
 $o.ownedSchema1.sourceCheckpointRunId=$s.checkpointSourceRunId;$o.ownedSchema1.sourceCheckpointArtifactSha256=$s.checkpointSourceArtifactSha256
 $o.ownedSchema1.originalNativeArchiveSha256=$source.artifact.observations.nativeWrite.sha256;$o.ownedSchema1.sourceField=Copy-Value $source.artifact.observations.ownedSchema1.fields[1]
 $slots=Copy-Value $source.artifact.observations.nativeWrite.archive.Slots
 foreach($archive in @($o.loadedArchive,$o.nativeWrite.archive,$o.schema1Migration.migratedArchive)){$archive.Slots=@($slots|ForEach-Object{Copy-Value $_})}
 $o.schema1Migration.nativeInputMetadata=ConvertTo-KmcOwnedSchema1Metadata $source.artifact.observations.nativeWrite.archive $r
 foreach($world in @($o.schema1Migration.restoredWorld,$o.schema1Migration.resavedWorld)+@($o.coldRestoration.events|Where-Object{$_.PSObject.Properties['snapshot']}|ForEach-Object{$_.snapshot})){$world.nativeControls=@($slots|ForEach-Object{Copy-Value $_})}
 foreach($snapshot in $o.snapshots){$snapshot.controls.ManagedHotbarSlotCount=2}
 $v.name='persistence-owner-seed-schema1-cold';$v
}
function New-KmcOwnerSeedSchema1Producer($Cold){
 $v=New-KmcOwnerSeedSchema1SourceFixture;$v.artifact.runId=$Cold.request.ownedPairs.checkpointSourceRunId;$v.artifact
}
function New-KmcOwnerSeedCurrentFixture([string]$Checkpoint){
 $parent=if($Checkpoint-ceq'owned-schema1-current-manual'){New-KmcOwnerSeedSchema1ColdFixture}else{New-KmcOwnerSeedCombatCheckpointLoadFixture 'legacy-mounted-pending-tb-manual'}
 $parent.artifact.observations.nativeWrite.sha256='b'*64
 $v=Copy-Value $parent;$r=$v.request;$o=$v.artifact.observations;$p=$parent.artifact.observations
 if($Checkpoint-ceq'owned-schema2-pending-current-manual'){
  # A separate current-format process uses the ordinary six-hook observer.
  # Parent-only historical cold diagnostics cannot be inherited by that child.
  $o.PSObject.Properties.Remove('combatDebtChecks');$o.coldRestoration.PSObject.Properties.Remove('debtDiagnostics')
  $o.coldRestoration.hooks=@($o.coldRestoration.hooks|Where-Object{$_.token-cnotin@('06008355','0600936F')})
 }
 $parentRun=$parent.request.runId;$r.runId=$parentRun+'.current';$v.artifact.runId=$r.runId;$r.evidenceRoot=$r.evidenceRoot.Replace($parentRun,$r.runId);$r.persistenceCase=$Checkpoint;$o.checkpoint=$Checkpoint
 $r.ownedPairs.checkpointSourceRunId=$parentRun;$r.ownedPairs.checkpointSourceArtifactSha256='d'*64;$r.ownedPairs.expectedNativeWriteCount=0
 $r.PSObject.Properties.Remove('schema2DerivationSha256')
 $r.persistenceLoad.internalName=$p.nativeWrite.nativeName;$r.persistenceLoad.fileName=Get-KmcChunk7OwnedOutputLeaf $parent.request $p;$r.persistenceLoad.sha256=$p.nativeWrite.sha256;$r.persistenceLoad.length=$p.nativeWrite.length
 $o.loadedDescriptor=Copy-Value $r.persistenceLoad;$o.loadedArchive=Copy-Value $p.nativeWrite.archive;$o.processId=$p.processId+1;$o|Add-Member loadedSourceSchema 3 -Force
 $world=if($Checkpoint-ceq'owned-schema1-current-manual'){$p.schema1Migration.resavedWorld}else{$p.schema2Migration.resavedWorld}
 $o|Add-Member currentFormatMigration ([pscustomobject][ordered]@{contract='owner-seed-current-format-load-v1';parentCheckpoint=$parent.request.persistenceCase;parentRunId=$parentRun;parentArtifactSha256=$r.ownedPairs.checkpointSourceArtifactSha256;parentProcessId=$p.processId;inputSchema=3;inputSha256=$r.persistenceLoad.sha256;inputLength=$r.persistenceLoad.length;parentNativeWriteSha256=$p.nativeWrite.sha256;loadedEqualsParentNativeWrite=$true;restoredWorld=(Copy-Value $world)}) -Force
 if($Checkpoint-ceq'owned-schema1-current-manual'){
  $r.PSObject.Properties.Remove('schema1DerivationSha256')
  $first=Copy-Value $o.snapshots[0];$first.label='schema1-current-restored-before-continuation';$first.gameTicks=[long]$o.loadedArchive.GameTimeTicks+1000L
  $last=Copy-Value $first;$last.label='schema1-current-continuation-complete';$last.gameTicks+=1000L;$o.snapshots=@($first,$last)
  $o.currentFormatMigration.restoredWorld.gameTicks=$first.gameTicks
  foreach($event in $o.coldRestoration.events){if($event.PSObject.Properties['snapshot']){$event.snapshot.gameTicks=$first.gameTicks}}
  $o.movements=@([pscustomobject][ordered]@{label='schema1-current-first-pair';selectedId=$r.ownedPairs.riderAId;executorId=$r.ownedPairs.mountAId;otherCommandsUnchanged=$true;start=[pscustomobject]@{x=0.0;y=0.0;z=0.0};goal=[pscustomobject]@{x=2.0;y=0.0;z=0.0};end=[pscustomobject]@{x=2.0;y=0.0;z=0.0};terminal=$true;distance=2.0;residual=0.0})
 }else{$o.loadedSourceSchema=3;$o.snapshots=@($o.snapshots|Where-Object label -CNE 'schema2-native-resave-before-continuation')}
 foreach($snapshot in $o.snapshots){$snapshot.snapshots=0;$snapshot.nativeCastRequests=0}
 foreach($field in @('nativeWrite','saveRequested','ownedOutputAdmission','ownedOutputCompletion','schema1Migration','schema2Migration','ownedSchema1')){$o.PSObject.Properties.Remove($field)}
 $v.name='persistence-owner-seed-current-'+$Checkpoint;$v
}
function New-KmcOwnerSeedCurrentParent($Current){
 $case=Get-KmcChunk7OwnerSeedCurrentParentCase $Current.request.persistenceCase
 $parent=if($case-ceq'owned-schema1-resave-manual'){New-KmcOwnerSeedSchema1ColdFixture}else{New-KmcOwnerSeedCombatCheckpointLoadFixture $case}
 $parent.artifact.observations.nativeWrite.sha256='b'*64
 $parent.artifact
}
