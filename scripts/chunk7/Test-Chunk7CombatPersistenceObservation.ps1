param([string]$BaselineReaderPath)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
. (Join-Path $PSScriptRoot 'Test-Chunk7CombatPersistenceEvidence.ps1')
if($BaselineReaderPath){. $BaselineReaderPath}
$passed=0
function Set-Field($Value,[string]$Name,$Data){$Value|Add-Member NoteProperty $Name $Data -Force}
function Test-Trace(){[pscustomobject]@{dropped=0;observationErrors=0;identityRegistry=[pscustomobject]@{contract='retained-native-object-identity-v1';capacity=16000;faults=0};events=@();observerHooks=@(@('06000C3C','06009120','0600838F','06000C46','0600939D')|ForEach-Object{[pscustomobject]@{token=$_;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}})}}
function Add-TraceEvent($Trace,[string]$Boundary,[string]$Actor,[int]$Command,[int]$Frame=3,[double]$Standard=0.0){
    $Trace.events+= [pscustomobject]@{sequence=$Trace.events.Count+1;boundary=$Boundary;frame=$Frame;command=$Command;commandActor=$Actor;commandType='KingmakerMountedCombat.Integration.MountedPairAttackCommand';acted=$true;actionType='Standard';state=[pscustomobject]@{actor=$Actor;standard=$Standard;move=0.0;swift=0.0;reactions=1;reactionCooldown=0.0}}
}
function DriverSnapshot($Archive,[string]$Label,[int]$Frame=3){[pscustomobject]@{label=$Label;frame=$Frame;gameTicks=$Archive.GameTimeTicks;round=$Archive.Combat.Round;currentActor=$(if($null-ne$Archive.Combat.Current){$Archive.Combat.Current.ActorId}else{$null});nativeActors=@($Archive.Combat.Actors|Where-Object{$_.Native.Id-cin$ids}|ForEach-Object{Copy-Test $_.Native});grants=@($ids|ForEach-Object{[pscustomobject]@{actorId=$_;count=1}});positions=@($ids|ForEach-Object{[pscustomobject]@{actorId=$_;x=0.0;y=0.0;z=0.0}});participations=@($Archive.Participations|ForEach-Object{Copy-Test $_.State})}}
function SourceCheckpoint($Archive,$Request){
    $t=Test-Trace;$attacks=@();$rt=$Request.persistenceCase-ceq'multi-rt-debt-manual';$legacy=$Request.persistenceCase-ceq'legacy-mounted-pending-tb-manual';$zero=$Request.persistenceCase-ceq'zero-mounted-tb-retained-manual'
    $spenders=if($legacy){@($mammoth)}elseif($rt){@($first,$horse)}elseif($zero){$ids}else{@($first,$mammoth,$second)}
    foreach($id in $spenders){$command=100+$attacks.Count;$caster=if($id-cin@($first,$mammoth)){$first}else{$second};$before=[pscustomobject]@{actors=@($ids|ForEach-Object{[pscustomobject]@{id=$_;standard=0.0;move=0.0;swift=0.0;reactions=1;reactionCooldown=0.0}})};$after=Copy-Test $before;@($after.actors|Where-Object id -CEQ $id)[0].standard=6.0
        $attacks+=[pscustomobject]@{label='native-primary-fixture';casterId=$caster;executorId=$caster;selectionAfterInput=$(if($caster-ceq$first){$second}else{$first});nativeCastDelta=1;nativeRefusalDelta=0;command=300+$command;clicked=$true;before=$before;after=$after;outcome=[pscustomobject]@{actorId=$id;resourceOwnerId=$id;targetId='99999999-9999-4999-8999-999999999999';result='Success';completedAttacks=1;nativeAttackRule=$true}}
        Add-TraceEvent $t 'cost-before' $id $command 2 0
        if(-not$rt){Add-TraceEvent $t 'actor-cost-before' $id $command 2 0;Add-TraceEvent $t 'actor-cost-after' $id $command 2 6}
        Add-TraceEvent $t 'cost-after' $id $command 2 6
    }
    if($zero){for($i=0;$i-lt2;$i++){$attacks+=[pscustomobject]@{label='checkpoint-native-dismount-'+$i;nativeCastDelta=1;nativeRefusalDelta=0;clicked=$true}}}
    $source=[pscustomobject]@{inputs=$attacks}
    if($legacy){Set-Field $source 'pendingSurvivor' ([pscustomobject]@{stimulus='UnitEntityData.LeaveCombat then native RemoveUnit if still registered';participation=(Copy-Test $Archive.Participations[0].State)})}
    # Completed A's native End, one second of later-turn game time before the save.
    $calls=@(if(-not$rt-and-not$zero-and-not$legacy){[pscustomobject]@{kind='end';complete=$true;before=[pscustomobject]@{actorId=$first};after=[pscustomobject]@{gameTicks=$Archive.GameTimeTicks-10000000L;resources=[pscustomobject]@{standard=6.0}}}})
    [pscustomobject]@{ready=$true;provenance='source-native-setup';actorIds=$ids;targetId='99999999-9999-4999-8999-999999999999';targetGroupId='KMC.RuntimeHostile.'+$Request.runId+':two-pair-target';errors=@();checkpoint=[pscustomobject]@{label='native-checkpoint-ready'};source=$source;trace=$t;nativeCompletion=[pscustomobject]@{errors=@();calls=$calls}}
}
function Continuation($Archive,$Request,[bool]$Cold,$Source){
    $kind=switch($Request.persistenceCase){'multi-rt-debt-manual'{'RealTimeDebts'};'multi-tb-debt-manual'{'TurnBasedDebts'};'zero-mounted-tb-retained-manual'{'ZeroMountedRetained'};'legacy-mounted-pending-tb-manual'{'MountedPendingOnly'}}
    $t=if($Cold){Test-Trace}else{Copy-Test $Source.trace};$offset=$t.events.Count
    $before=DriverSnapshot $Archive 'continuation-before-input';$after=DriverSnapshot $Archive 'continuation-complete' 9;$rows=@($before)
    $driver=[pscustomobject]@{archiveValidated=$true;archiveRound=$Archive.Combat.Round;coldBinding=(DriverSnapshot $Archive 'read-only-cold-bind')};$calls=@()
    if($kind-cin@('RealTimeDebts','TurnBasedDebts')){
        $moveBefore=DriverSnapshot $Archive 'continuation-move-before' 4;$moveAfter=DriverSnapshot $Archive 'continuation-move-after' 5;$moveAfter.positions[3].z=0.5
        Set-Field $driver 'movementCommand' 500;Set-Field $driver 'movementExecutorId' $horse;Set-Field $driver 'movement' ([pscustomobject]@{terminal=$true;executorId=$horse;command=500;distance=0.5;before=$moveBefore;after=$moveAfter})
        Add-TraceEvent $t 'admission-after' $horse 500 4
        if($kind-ceq'RealTimeDebts'){$ready=DriverSnapshot $Archive 'native-rt-readiness' 4;$ready.gameTicks+=60000000L;$ready.nativeActors[0].Standard=0.0;$ready.nativeActors[3].Standard=0.0;$rows+=@($ready);$after.gameTicks=$ready.gameTicks}
        else{$partial=DriverSnapshot $Archive 'saved-partner-movement-no-prepare' 6;$rows+=@($partial);foreach($id in @($second,$horse)){$calls+=[pscustomobject]@{kind='end';complete=$true;before=[pscustomobject]@{actorId=$id;allocationSequence=$offset+1}}}}
    }else{
        if($kind-ceq'ZeroMountedRetained'){$a=DriverSnapshot $Archive 'duplicate-detached-cleanup-before' 4;$a.round++;$b=Copy-Test $a;$b.label='duplicate-detached-cleanup-after';$rows+=[pscustomobject]@{stimulus='repeat cleanup at actual native StartRound return; no round/resource mutation';before=$a;after=$b}}
        $mounts=if($kind-ceq'MountedPendingOnly'){@($mammoth)}else{@($mammoth,$horse)}
        foreach($id in $mounts){$snap=DriverSnapshot $Archive ('independent-native-mount-'+$id) 5;$snap.round++;$snap.currentActor=$id;@($snap.grants|Where-Object actorId -CEQ $id)[0].count=2;$rows+=@($snap);Add-TraceEvent $t 'prepare-before' $id 0 5}
    }
    $rows+=@($after)
    [pscustomobject]@{ready=$true;continuationComplete=$true;kind=$kind;provenance=$(if($Cold){'cold-native-archive-binding'}else{'source-native-setup'});actorIds=$ids;targetId=$Source.targetId;targetGroupId=$Source.targetGroupId;continuationTraceOffset=$offset;continuation=$rows;trace=$t;driver=$driver;nativeCompletion=[pscustomobject]@{errors=@();calls=$calls}}
}
function RestoreProbe($Archive,$Request){
    $h=Copy-Test $hooks;foreach($name in @('TryRestoreArchiveCombat','RestoreArchivePresentation')){$h+=[pscustomobject]@{method=$name;token='06002000';moduleMvid=$Request.dllMvid}}
    $e=@();foreach($actor in $Archive.Combat.Actors){foreach($kind in @('restore-actor-before','restore-actor-after','restore-debt-before','restore-debt-after')){$e+=[pscustomobject]@{kind=$kind;actorId=$actor.Native.Id;debt=(Copy-Test $actor.Native);restorePhase='validated-before-native-mutation'}}}
    $e+=[pscustomobject]@{kind='shared-world-restored';snapshot=(NativeWorld $Archive $false)};$e+=[pscustomobject]@{kind='all-owner-presentation-restored';snapshot=(NativeWorld $Archive $true)}
    [pscustomobject]@{complete=$true;installedBeforeWorldLoad=$true;contract='chunk7-native-restoration-entry-observer-v1';maximumEvents=2048;dropped=0;errors=0;prepareMetric='native-method-entries-including-production-refused-attempts';hooks=$h;events=$e}
}
function CompleteCombatArtifact($Fixture,[bool]$Cold){
    $a=Copy-Test $Fixture.archive;$q=Copy-Test $Fixture.request;$q.scenario=if($Cold){'chunk7-persistence-load'}else{'chunk7-persistence-save'}
    if($Cold){Set-Field $q 'persistenceLoad' (Copy-Test $coldRequest.persistenceLoad)}
    $source=SourceCheckpoint $a $q;$continuation=Continuation $a $q $Cold $source;$v=Copy-Test $o;$v.checkpoint=$q.persistenceCase;$v.cold=$Cold;$v.combatDebtProof=$true;$v.movements=@()
    $v.snapshots[0].label=if($Cold){'cold-restored-before-input'}else{'native-write-complete'};$v.snapshots[0].owners=(NativeWorld $a $true).owners;$v.snapshots[0].controls.ManagedHotbarSlotCount=$a.Slots.Count
    if($Cold){$v.inputs=@();$v.snapshots[0].semanticRestores=5;$v.snapshots[0].presentationRestores=$a.Pairs.Count;$v.snapshots[0].nativePostLoads=5;$v.snapshots[0].snapshots=0;$v.snapshots[0].nativeCastRequests=0;$v.snapshots[0].archivePhase='all-pair-presentation-restored'
        Set-Field $v 'loadedArchive' $a;Set-Field $v 'loadedDescriptor' (Copy-Test $q.persistenceLoad);Set-Field $v 'loadedSourceSchema' $(if($q.persistenceCase-ceq'legacy-mounted-pending-tb-manual'){2}else{3});Set-Field $v 'coldRestoration' (RestoreProbe $a $q)
    }else{$initial=Copy-Test $v.snapshots[0];$initial.label='before-native-save';foreach($owner in $initial.owners){$owner.state='Active'};$v.snapshots=@($initial)+$v.snapshots
        $v.nativeWrite.archive=$a;Set-Field $v.nativeWrite 'sourceSchema' 3;Set-Field $v.nativeWrite 'nativeMetadata' (Copy-Test $a);Set-Field $v 'combatCheckpoint' $source
        if($q.persistenceCase-ceq'legacy-mounted-pending-tb-manual'){$v.inputs=@($v.inputs[0]);$v.nativeWrite.sourceSchema=2;$v.nativeWrite.nativeMetadata=Copy-Test $schema2;Set-Field $v 'legacyWriterScope' ([pscustomobject]@{selectedOnlyForCase=$q.persistenceCase;sourceSchema=2;owners=1;selectedCapabilityRestored=$true;restorationEnabledOnColdProcess=$true})}
    }
    if($Cold-and$q.persistenceCase-ceq'legacy-mounted-pending-tb-manual'){
        $v.nativeWrite.archive=Copy-Test $a;Set-Field $v.nativeWrite 'nativeMetadata' (Copy-Test $a);Set-Field $v.nativeWrite 'sourceSchema' 3
        Set-Field $v.nativeWrite 'nativeName' 'KMC_C7_SCHEMA2';$v.nativeWrite.sha256='9'*64
        Set-Field $v.saveRequested 'nativeName' 'KMC_C7_SCHEMA2';Set-Field $v.saveRequested 'gameTicks' $a.GameTimeTicks
        Set-Field $v.snapshots[0] 'frame' 2;Set-Field $v.snapshots[0] 'gameTicks' $a.GameTimeTicks
        $resaved=Copy-Test $v.snapshots[0];$resaved.label='schema2-native-resave-before-continuation';$resaved.frame=3;$resaved.snapshots=1
        $v.snapshots+=@($resaved)
        Set-Field $v 'schema2Migration' ([pscustomobject]@{inputSchema=2;inputSha256=$q.persistenceLoad.sha256;inputLength=$q.persistenceLoad.length;migratedArchive=(Copy-Test $a);inputUnchanged=$true;resavedWorld=(NativeWorld $a $true)})
    }
    Set-Field $v 'combatContinuation' $continuation
    $cleanup=[pscustomobject]@{cleanupComplete=$true;cleanupErrors=@();targetId=$source.targetId;targetGroupId=$source.targetGroupId;driver=[pscustomobject]@{coldCleanup=[pscustomobject]@{targetId=$source.targetId;targetGroupId=$source.targetGroupId;targetRemoved=$true;groupRemoved=$true;nativeCombatEnded=$true;borrowedFactionDestroyed=$false;resourceFieldsWritten=$false}}}
    Set-Field $v 'checkpointCleanup' $cleanup
    [pscustomobject]@{request=$q;artifact=(New-Artifact $q $v)}
}
$complete=@();foreach($fixture in @($tb,$rt,$zero,$legacy)){foreach($isCold in @($false,$true)){$spec=CompleteCombatArtifact $fixture $isCold;$complete+=@($spec);Check ($spec.request.persistenceCase+' complete '+$spec.request.scenario) {Assert-KmcChunk7PersistenceObservation $spec.request $spec.artifact 'PASS'}}}
$sourceTb=$complete[0];$coldTb=$complete[1];$sourceRt=$complete[2];$coldZero=$complete[5];$coldLegacy=$complete[7]
Check 'source TB rider approach charged to its own mount move budget' {$spec=Copy-Test $sourceTb;$i=$spec.artifact.observations.combatCheckpoint.source.inputs[2];$i.before.actors[3].move=0.09;$i.after.actors[3].move=0.74;Assert-KmcChunk7PersistenceObservation $spec.request $spec.artifact 'PASS'}
foreach($mutation in @('missing-cost','duplicate-cost','wrong-cost-actor','wrong-action','no-actual-spend','neighbour-debt','outcome-other-owner','observer-incomplete','wrong-native-hook','target-substitution','missing-mount','snapshot-twice','worker-incomplete','source-format','live-owner-generation','completed-A-missing-end','completed-A-partial-end','completed-A-overdecayed','completed-A-refunded','approach-own-mount-refund','approach-own-mount-overbudget','approach-own-mount-standard','approach-foreign-mount-move','approach-mount-attack-moves-rider')){
    Reject ('source raw proof '+$mutation){$spec=Copy-Test $sourceTb;$v=$spec.artifact.observations;$checkpoint=$v.combatCheckpoint;switch($mutation){
      'missing-cost' {$checkpoint.trace.events=@($checkpoint.trace.events|Where-Object boundary -CNE 'cost-after')}
      'duplicate-cost' {$copy=Copy-Test $checkpoint.trace.events[-1];$copy.sequence=$checkpoint.trace.events.Count+1;$checkpoint.trace.events+=@($copy)}
      'wrong-cost-actor' {$checkpoint.trace.events[0].commandActor=$horse}
      'wrong-action' {$checkpoint.trace.events[0].actionType='Move'}
      'no-actual-spend' {$checkpoint.trace.events[3].state.standard=0.0}
      'neighbour-debt' {$checkpoint.source.inputs[0].after.actors[1].standard=6.0}
      'outcome-other-owner' {$checkpoint.source.inputs[0].outcome.resourceOwnerId=$horse}
      'observer-incomplete' {$checkpoint.trace.dropped=1}
      'wrong-native-hook' {$checkpoint.trace.observerHooks[0].token='06000C3D'}
      'target-substitution' {$checkpoint.targetGroupId='KMC.RuntimeHostile.other:two-pair-target'}
      'missing-mount' {$v.inputs=@($v.inputs[0])}
      'snapshot-twice' {$v.nativeWrite.snapshotCount=2}
      'worker-incomplete' {$v.nativeWrite.operation='Saving'}
      'source-format' {$v.nativeWrite.sourceSchema=2}
      'live-owner-generation' {$v.snapshots[-1].owners[0].generation=99}
      'approach-own-mount-refund' {$checkpoint.source.inputs[2].before.actors[3].move=0.5;$checkpoint.source.inputs[2].after.actors[3].move=0.2}
      'approach-own-mount-overbudget' {$checkpoint.source.inputs[2].after.actors[3].move=7.0}
      'approach-own-mount-standard' {$checkpoint.source.inputs[2].after.actors[3].standard=1.0}
      'approach-foreign-mount-move' {$checkpoint.source.inputs[0].after.actors[3].move=0.7}
      'approach-mount-attack-moves-rider' {$checkpoint.source.inputs[1].after.actors[0].move=0.7}
      'completed-A-missing-end' {$checkpoint.nativeCompletion.calls=@()}
      'completed-A-partial-end' {$checkpoint.nativeCompletion.calls[0].after.resources.standard=3.0}
      'completed-A-overdecayed' {foreach($doc in @($v.nativeWrite.archive,$v.nativeWrite.nativeMetadata)){foreach($row in @($doc.Combat.Actors|Where-Object{$_.Native.Id-ceq$first})+@($doc.Pairs|Where-Object{$_.Rider.Id-ceq$first}|ForEach-Object{[pscustomobject]@{Native=$_.Rider}})){$row.Native.Standard=2.0}}}
      'completed-A-refunded' {foreach($row in @($v.nativeWrite.archive.Combat.Actors|Where-Object{$_.Native.Id-ceq$first})+@($v.nativeWrite.archive.Pairs|Where-Object{$_.Rider.Id-ceq$first}|ForEach-Object{[pscustomobject]@{Native=$_.Rider}})){$row.Native.Standard=0.0}}
    };Assert-KmcChunk7PersistenceObservation $spec.request $spec.artifact 'PASS'}
}
foreach($mutation in @('hidden-prepare','whole-world-omitted','active-before-presentation','presentation-unlinked','missing-native-actor','changed-allocation','changed-owner-role','extra-restore','wrong-debt','replayed-native-cast','missing-postload','fake-movement','wrong-command','movement-reprepare','lost-native-end','wrong-source','cleanup-target','cleanup-custody')){
    Reject ('cold raw proof '+$mutation){$spec=Copy-Test $coldTb;$v=$spec.artifact.observations;switch($mutation){
      'hidden-prepare' {$v.coldRestoration.events+= [pscustomobject]@{kind='prepare-before';actorId=$first}}
      'whole-world-omitted' {$v.coldRestoration.events=@($v.coldRestoration.events|Where-Object kind -CNE 'shared-world-restored')}
      'active-before-presentation' {$p=$v.coldRestoration.events[-2].snapshot.participations[0];$p.MountedPairId=$v.coldRestoration.events[-1].snapshot.participations[0].MountedPairId}
      'presentation-unlinked' {$v.coldRestoration.events[-1].snapshot.participations[0].MountedPairId=$null}
      'missing-native-actor' {$v.coldRestoration.events[-1].snapshot.actors=@($v.coldRestoration.events[-1].snapshot.actors|Select-Object -First 4)}
      'changed-allocation' {$v.coldRestoration.events[-2].snapshot.combat.Allocations[0].GrantId='other'}
      'changed-owner-role' {$v.coldRestoration.events[-1].snapshot.owners[0].mountId=$horse}
      'extra-restore' {$v.coldRestoration.events+=@((Copy-Test $v.coldRestoration.events[0]))}
      'wrong-debt' {$v.coldRestoration.events[3].debt.Standard=0.0}
      'replayed-native-cast' {$v.snapshots[0].nativeCastRequests=1}
      'missing-postload' {$v.snapshots[0].nativePostLoads=4}
      'fake-movement' {$v.combatContinuation.driver.movement.after.positions[3].z=0.0}
      'wrong-command' {$v.combatContinuation.driver.movement.command=999}
      'movement-reprepare' {$v.combatContinuation.continuation[1].grants[3].count=2}
      'lost-native-end' {$v.combatContinuation.nativeCompletion.calls=@($v.combatContinuation.nativeCompletion.calls[0])}
      'wrong-source' {$v.loadedDescriptor.sha256='7'*64}
      'cleanup-target' {$v.checkpointCleanup.driver.coldCleanup.targetId=$horse}
      'cleanup-custody' {$v.checkpointCleanup.cleanupComplete=$false}
    };Assert-KmcChunk7PersistenceObservation $spec.request $spec.artifact 'PASS'}
}
Reject 'RT readiness before actual native debt elapsed' {$spec=Copy-Test $sourceRt;$v=$spec.artifact.observations.combatContinuation;$v.continuation[1].gameTicks=$v.continuation[0].gameTicks+10000000L;Assert-KmcChunk7PersistenceObservation $spec.request $spec.artifact 'PASS'}
Reject 'zero mounted repeated cleanup restarts release round' {$spec=Copy-Test $coldZero;$spec.artifact.observations.combatContinuation.continuation[1].after.participations[0].SplitReleaseRound=4;Assert-KmcChunk7PersistenceObservation $spec.request $spec.artifact 'PASS'}
Reject 'pending mount misses first native independent round' {$spec=Copy-Test $coldLegacy;$spec.artifact.observations.combatContinuation.continuation[1].round=5;Assert-KmcChunk7PersistenceObservation $spec.request $spec.artifact 'PASS'}
foreach($mutation in @('array-input-hash','array-output-type','array-output-name','array-output-operation','array-output-hash','array-request-type','array-request-name','missing-write','missing-migration','changed-input','mutated-input','wrong-output-schema','wrong-output-name','repeated-snapshot','missing-callback','reused-input-hash','dropped-pending','changed-release','changed-owner','refunded-debt','free-reaction','hidden-prepare','late-resave','replayed-cast','changed-allocation')){
    Reject ('schema2 cold resave '+$mutation){$spec=Copy-Test $coldLegacy;$v=$spec.artifact.observations
        switch($mutation){
            'array-input-hash' {$v.schema2Migration.inputSha256=@($v.schema2Migration.inputSha256)}
            'array-output-type' {$v.nativeWrite.nativeType=@($v.nativeWrite.nativeType)}
            'array-output-name' {$v.nativeWrite.nativeName=@($v.nativeWrite.nativeName)}
            'array-output-operation' {$v.nativeWrite.operation=@($v.nativeWrite.operation)}
            'array-output-hash' {$v.nativeWrite.sha256=@($v.nativeWrite.sha256)}
            'array-request-type' {$v.saveRequested.nativeType=@($v.saveRequested.nativeType)}
            'array-request-name' {$v.saveRequested.nativeName=@($v.saveRequested.nativeName)}
            'missing-write' {$v.PSObject.Properties.Remove('nativeWrite')}
            'missing-migration' {$v.PSObject.Properties.Remove('schema2Migration')}
            'changed-input' {$v.schema2Migration.inputSha256='8'*64}
            'mutated-input' {$v.schema2Migration.inputUnchanged=$false}
            'wrong-output-schema' {$v.nativeWrite.sourceSchema=2}
            'wrong-output-name' {$v.nativeWrite.nativeName='KMC_C7_WORLD'}
            'repeated-snapshot' {$v.nativeWrite.snapshotCount=2}
            'missing-callback' {$v.nativeWrite.nativeCallback=$false}
            'reused-input-hash' {$v.nativeWrite.sha256=$spec.request.persistenceLoad.sha256}
            'dropped-pending' {$v.nativeWrite.archive.Participations=@()}
            'changed-release' {$v.nativeWrite.archive.Participations[0].State.PendingSplitRound++}
            'changed-owner' {$id='77777777777747778777777777777777';$v.nativeWrite.archive.Pairs[0].PairId=$id;$v.nativeWrite.archive.Participations[0].OwnerId=$id;$v.nativeWrite.archive.Participations[0].MountedPairId=$id}
            'refunded-debt' {$v.nativeWrite.archive.Pairs[0].Mount.Standard=0.0;@($v.nativeWrite.archive.Combat.Actors|Where-Object {$_.Native.Id-ceq$mammoth})[0].Native.Standard=0.0}
            'free-reaction' {$v.nativeWrite.archive.Pairs[0].Mount.ReactionsRemaining++;@($v.nativeWrite.archive.Combat.Actors|Where-Object {$_.Native.Id-ceq$mammoth})[0].Native.ReactionsRemaining++}
            'hidden-prepare' {$v.coldRestoration.events+= [pscustomobject]@{kind='prepare-before';actorId=$mammoth}}
            'late-resave' {$v.snapshots[-1].frame=4}
            'replayed-cast' {$v.snapshots[-1].nativeCastRequests=1}
            'changed-allocation' {$v.nativeWrite.archive.Combat.Allocations[0].GrantId='other-grant'}
        }
        if($v.PSObject.Properties['nativeWrite']){$v.nativeWrite.nativeMetadata=Copy-Test $v.nativeWrite.archive}
        Assert-KmcChunk7PersistenceObservation $spec.request $spec.artifact 'PASS'
    }
}
# Exercise the actual artifact output selection with only manifest/file I/O
# substituted. Child scope prevents the substitutions escaping to source tests.
& {
    $artifactRequest=Copy-Test $coldLegacy.request;$boundArtifact=Copy-Test $coldLegacy.artifact
    $artifactLeaf='Manual_301_KMC_C7_SCHEMA2.zks'
    $boundArtifact.observations.nativeWrite|Add-Member NoteProperty path (Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$artifactRequest.runId+'/Saved Games/'+$artifactLeaf)) -Force
    $archiveChecks=[Collections.Generic.List[object]]::new()
    $manifest=[pscustomobject]@{artifacts=@([pscustomobject]@{relativePath='chunk7-persistence-evidence.json';kind='chunk7-persistence-evidence'})}
    function Read-KmcChunk7ManifestArtifact {param($Request,$Manifest,$Leaf,$Kind);return $boundArtifact}
    function Assert-KmcNotReparsePoint {param($Path,$Description)}
    function Assert-KmcNotHardLink {param($Path,$Description)}
    function Assert-KmcChargeArchiveSnapshot {param($Path,$Hash,$Length,$Metadata);$archiveChecks.Add([pscustomobject]@{path=$Path;hash=$Hash;length=$Length;metadata=$Metadata})}
    Check 'schema2 cold resave artifact checks exact separate output' {
        Assert-KmcChunk7PersistenceArtifact $artifactRequest $manifest 'PASS'
        if($archiveChecks.Count-ne1-or$archiveChecks[0].path-cne$boundArtifact.observations.nativeWrite.path-or$archiveChecks[0].hash-cne('9'*64)-or
            (ConvertTo-KmcChunk7Canonical $archiveChecks[0].metadata)-cne(ConvertTo-KmcChunk7Canonical $boundArtifact.observations.nativeWrite.archive)){throw 'Schema2 native output archive was not checked exactly.'}
    }
    Reject 'schema2 resave artifact refuses an array-valued output path' {
        $originalPath=$boundArtifact.observations.nativeWrite.path
        try{$boundArtifact.observations.nativeWrite.path=@($originalPath);Assert-KmcChunk7PersistenceArtifact $artifactRequest $manifest 'PASS'}
        finally{$boundArtifact.observations.nativeWrite.path=$originalPath}
    }
    Reject 'schema2 resave artifact cannot alias its read-only input' {
        $originalPath=$boundArtifact.observations.nativeWrite.path
        try{$boundArtifact.observations.nativeWrite.path=$originalPath.Replace($artifactLeaf,'Manual_300_KMC_C7_WORLD.zks');Assert-KmcChunk7PersistenceArtifact $artifactRequest $manifest 'PASS'}
        finally{$boundArtifact.observations.nativeWrite.path=$originalPath}
    }
}
Write-Host ('CHUNK7 COMBAT PERSISTENCE OBSERVATION PASS='+$passed+' FAIL=0; synthetic raw proof and source/cold adapters only.')
