$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Test-Chunk7PersistenceEvidence.ps1')
$loadRequest=Copy-Test $coldRequest;$loadRequest.persistenceCase='multi-exploration-three-loads-manual'
function ReplayWorld([int]$Load,[int]$AreaOrdinal,[bool]$Mounted=$true){
    $actors=@();for($i=0;$i-lt4;$i++){$actor=$native[$i];$idBase=$Load*100+$i*10;$rider=$i-cin@(0,2);$component=if($Mounted-and$rider){$idBase+6+$AreaOrdinal*1000}else{0}
        $actors+=[pscustomobject]@{id=$actor.Id;object=$idBase+1;descriptor=$idBase+2;view=$idBase+3;viewObject=$idBase+4;inState=$true;commandsEmpty=$true;overrides=@(if($Mounted-and$rider){$component});agentOverride=$component;native=(Copy-Test $actor)}}
    $nativeOwners=@();if($Mounted){$nativeOwners=@($owners|ForEach-Object{$v=Copy-Test $_;$v.riderId=([Guid]$v.riderId).ToString('N');$v.mountId=([Guid]$v.mountId).ToString('N');$v|Add-Member NoteProperty retainsObligations $true;$v})}
    $horseFacts=@($archive.Pairs|Where-Object ProfileId -CEQ 'medium-humanoid-horse-v1').Count
    if($horseFacts-lt1-or$horseFacts-gt2){throw 'Synthetic replay archive needs one or two exact Horse feature facts.'}
    $subscriberEntries=@()
    foreach($interface in @('IAreaLoadingStagesHandler','IPartyLeaveAreaHandler','IPartyHandler','IGameModeHandler','IPartyCombatHandler','IUnitHandler','IUnitViewAttachedUIHandler','IUnitLifeStateChanged','IUnitCombatHandler','IUnitFinallyDeadHandler','IInGameHandler','ITurnBasedModeEnabledHandler','ISceneHandler')){
        $subscriberEntries+=[pscustomobject]@{interface='Kingmaker.PubSubSystem.'+$interface;type='KingmakerMountedCombat.Integration.MountedLifecycleSubscriber';identity=-999}
    }
    for($i=0;$i-lt$horseFacts;$i++){$subscriberEntries+=[pscustomobject]@{interface='Kingmaker.PubSubSystem.ISceneHandler';type='KingmakerMountedCombat.Integration.HorseCompanionAddPet';identity=$Load*1000+11+$i}}
    foreach($interface in @('IAbilityTargetSelectionUIHandler','IClickActionHandler')){$subscriberEntries+=[pscustomobject]@{interface='Kingmaker.PubSubSystem.'+$interface;type='KingmakerMountedCombat.Integration.NativeMountedControlService';identity=-998}}
    $subscriptions=[pscustomobject]@{assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';executing=$false;uniqueOwnedSubscribers=2+$horseFacts;ownedInterfaceEntries=15+$horseFacts;entries=$subscriberEntries}
    [pscustomobject]@{frame=100*$Load+50*$AreaOrdinal;gameTicks=10000000L;gameObject=-1;playerObject=$Load*1000+1;stateObject=1002;areaStateObject=$Load*1000+3+$AreaOrdinal*100;mainStateObject=$Load*1000+4+$AreaOrdinal*100;
      area=$(if($AreaOrdinal){@('b','c','d')[$AreaOrdinal-1]*32}else{'a'*32});playerInCombat=$false;boundaries=2;participants=2;failedConstructions=0;allocationActors=0;active=$(if($Mounted){2}else{0});owners=$nativeOwners;
      preparedOrViewResidue=$(if($Mounted){2}else{0});liveControlBoundaries=0;commandOwners=0;unsettledPreparation=$false;relationshipTransition=$false;serializationBusy=$false;pendingWrites=$false;saveScope=$false;saveFence=$false;
      nativeDisposals=$Load-1;areaSuspensions=$AreaOrdinal;areaResumes=2*$AreaOrdinal;semanticRestores=4*$Load;presentationRestores=2*$Load;nativePostLoads=4;nativeCastRequests=0;snapshotCount=0;duplicateFacts=0;nativeSlots=(Copy-Test $slots);actors=$actors;subscriptions=$subscriptions;
      label=$(if($AreaOrdinal){'area-'+$AreaOrdinal}else{'load-'+$Load});selectionRebinding=@([pscustomobject]@{id=$first;object=$Load*100+1})}
}
function ReplayRestoration([int]$Loads){
    $h=Copy-Test $hooks;$h[1].method='OnNewRound';foreach($method in @('TryRestoreArchiveCombat','RestoreArchivePresentation')){$h+=[pscustomobject]@{method=$method;declaringType='KingmakerMountedCombat.Integration.MountedPersistenceService';moduleMvid=$request.dllMvid;token='06002000'}}
    $es=@();for($epoch=0;$epoch-lt$Loads;$epoch++){
        foreach($actor in $native){foreach($kind in @('restore-debt-before','restore-debt-after')){$es+=[pscustomobject]@{kind=$kind;actorId=$actor.Id;frame=10+$epoch*100;gameTicks=10000000L;restorePhase='validated-before-native-mutation';debt=(Copy-Test $actor);prepared=$false;inCombat=$false}}}
        $es+=[pscustomobject]@{kind='all-owner-presentation-restored';frame=20+$epoch*100;restorePhase='all-pair-presentation-restored';snapshot=[pscustomobject]@{gameTicks=10000000L;turnBased=$false;playerInCombat=$false;actors=@($native|ForEach-Object{[pscustomobject]@{Native=(Copy-Test $_);InCombat=$false;Prepared=$false}});combat=$null;participations=@();owners=@($owners|ForEach-Object{Copy-Test $_});nativeControls=(Copy-Test $slots)}}
    }
    [pscustomobject]@{complete=$true;installedBeforeWorldLoad=$true;contract='chunk7-native-restoration-entry-observer-v1';maximumEvents=2048;dropped=0;errors=0;prepareMetric='native-method-entries-including-production-refused-attempts';hooks=$h;events=$es}
}
function New-ReplayArtifact($Req,[bool]$Areas){
    $loads=if($Areas){1}else{3};$worlds=@(ReplayWorld 1 0);$qs=@();$retentionEvents=@()
    if($Areas){for($i=1;$i-le3;$i++){
        $before=Copy-Test $worlds[$worlds.Count-1];$qs+=[pscustomobject]@{kind='area-transfer';ordinal=$i;enterPoint=$Req.persistenceReplayTargets[$i-1].enterPoint;area=$Req.persistenceReplayTargets[$i-1].area;autoSaveMode='None';before=$before}
        $suspended=ReplayWorld 1 ($i-1) $false;$suspended.areaSuspensions=$i;$suspended.subscriptions.executing=$true
        $retentionEvents+=[pscustomobject]@{kind='area-suspended';before=$i-1;after=$i;state=$suspended}
        $worlds+=ReplayWorld 1 $i
        $retentionEvents+=[pscustomobject]@{kind='area-restored';before=($i-1)*2;after=$i*2;state=(Copy-Test $worlds[$worlds.Count-1])}
    }}else{for($i=2;$i-le3;$i++){
        $before=Copy-Test $worlds[$worlds.Count-1];$qs+=[pscustomobject]@{kind='native-load';ordinal=$i;before=$before;fileName=$Req.persistenceLoad.fileName;sha256=$Req.persistenceLoad.sha256;callbackObserved=$true;callbackNativeLoads=$i;callbackFrame=$i*100-1}
        $disposal=ReplayWorld ($i-1) 0 $false;$disposal.nativeDisposals=$i-1;$disposal.nativePostLoads=0
        $retentionEvents+=[pscustomobject]@{kind='native-world-cleanup-before-disposal';before=$i-2;after=$i-1;state=$disposal};$worlds+=ReplayWorld $i 0
    }}
    $probe=[pscustomobject]@{contract='chunk7-repeated-world-retention-v1';complete=$true;installedBeforeWorldLoad=$true;maximumEvents=24;errors=0;dropped=0;hooks=@(foreach($m in @('PrepareForNativeWorldDisposal','SuspendAreaPair','RestoreAreaPair')){[pscustomobject]@{method=$m;declaringType='KingmakerMountedCombat.Integration.MountedPersistenceService';token='06003000';moduleMvid=$Req.dllMvid}});events=$retentionEvents}
    $settled=ReplayWorld $loads $(if($Areas){3}else{0}) $false
    $allStates=@($worlds)+@($qs|ForEach-Object{$_.before})+@($retentionEvents|ForEach-Object{$_.state})+@($settled)
    $labels=@(foreach($s in $allStates){foreach($f in @('gameObject','playerObject','stateObject','areaStateObject','mainStateObject')){$s.$f};foreach($a in $s.actors){foreach($f in @('object','descriptor','viewObject')){$a.$f}}});$labels=@($labels|Sort-Object -Unique)
    $r=[pscustomobject]@{contract='chunk7-repeated-worlds-v1';kind=$(if($Areas){'area-transfer'}else{'native-load'});requiredNativeLoads=$loads;requiredAreaTransfers=$(if($Areas){3}else{0});worlds=$worlds;requests=$qs;performanceProof=$false;diagnosticsOffProof=$false;allocationRateProof=$false;inputSha256=$Req.persistenceLoad.sha256;
      nativeLoadRequests=$loads;nativeWriteRequests=0;restoration=(ReplayRestoration $loads);retention=$probe;inputUnchanged=$true;finalRetention=(Copy-Test $probe);settled=$settled;observersReleased=$true;identityRetention=[pscustomobject]@{capacity=256;faults=0;retainedAfterRelease=0;released=$true;ids=$labels}}
    $v=Copy-Test $co;$v.checkpoint=$Req.persistenceCase;$v.movements=@();$v.inputs=@();$v.snapshots=@();$v.loadedDescriptor=Copy-Test $Req.persistenceLoad;$v.loadedArchive=Copy-Test $archive;$v.coldRestoration=Copy-Test $r.restoration;$v|Add-Member NoteProperty repeatedWorlds $r
    New-Artifact $Req $v
}
$threeLoad=New-ReplayArtifact $loadRequest $false
Check 'three exact immutable native loads and independent old-world cleanup' {Assert-KmcChunk7PersistenceObservation $loadRequest $threeLoad 'PASS'}
Check 'selected six-capacity candidate retains all existing repeated-world assertions' {$v=Copy-Test $threeLoad;$v.observations.nativeCapacity=6;Assert-KmcChunk7PersistenceObservation $loadRequest $v 'PASS'}
$areaRequest=Copy-Test $loadRequest;$areaRequest.persistenceCase='multi-exploration-three-area-transfers-manual'
$areaRequest|Add-Member NoteProperty persistenceReplayTargets @(for($i=0;$i-lt3;$i++){[pscustomobject]@{enterPoint=@('1','2','3')[$i]*32;area=@('b','c','d')[$i]*32;autoSaveMode='None'}})
$threeAreas=New-ReplayArtifact $areaRequest $true
Check 'three exact ordinary area transfers are distinct from native reloads' {Assert-KmcChunk7PersistenceObservation $areaRequest $threeAreas 'PASS'}
Check 'same-call native Destroy can enumerate exact old component before settled frame' {
    $a=Copy-Test $threeAreas;$r=$a.observations.repeatedWorlds;$r.retention.events[0].state.actors[0].overrides=@($r.worlds[0].actors[0].agentOverride);$r.finalRetention=Copy-Test $r.retention
    Assert-KmcChunk7PersistenceObservation $areaRequest $a 'PASS'
}
foreach($case in @('multi-exploration-three-loads-manual','multi-exploration-three-area-transfers-manual')){Check ('source map stays qualified manual source for '+$case){if((Get-KmcChunk7PersistenceSourceCase $case)-cne'multi-exploration-manual'){throw 'source map differs'}}}
Check 'historical source case remains unchanged' {if((Get-KmcChunk7PersistenceSourceCase 'multi-exploration-quick')-cne'multi-exploration-quick'){throw 'historical source changed'}}
foreach($mutation in @('save-family','missing-target','four-targets','same-area','same-next-area','auto-save','null-target','text-route')){
    Reject $mutation {$q=Copy-Test $areaRequest;switch($mutation){
      'save-family' {$q.scenario='chunk7-persistence-save'}
      'missing-target' {$q.persistenceReplayTargets=@($q.persistenceReplayTargets|Select-Object -First 2)}
      'four-targets' {$q.persistenceReplayTargets+=Copy-Test $q.persistenceReplayTargets[0]}
      'same-area' {$q.persistenceReplayTargets[0].area=$q.fixture.working.area}
      'same-next-area' {$q.persistenceReplayTargets[1].area=$q.persistenceReplayTargets[0].area}
      'auto-save' {$q.persistenceReplayTargets[0].autoSaveMode='AfterEntry'}
      'null-target' {$q.persistenceReplayTargets[0]=$null}
      'text-route' {$q.persistenceReplayTargets='three'}
    };Assert-KmcChunk7RepeatedWorldRequest $q}
}
# The runner holds its request as an ordered dictionary until serialization; the repeated-world assert must detect dictionary keys too.
function ConvertTo-RunnerRequest($Q){$r=[ordered]@{};foreach($p in $Q.PSObject.Properties){$r[$p.Name]=$p.Value};$r}
function New-RepeatedRunnerRequest{ConvertTo-RunnerRequest (Copy-Test $areaRequest)}
Check 'repeated-world request assert accepts the runner ordered-dictionary request' {Assert-KmcChunk7RepeatedWorldRequest (ConvertTo-RunnerRequest (Copy-Test $areaRequest))}
Reject 'ordered-dictionary-missing-targets' {$r=New-RepeatedRunnerRequest;$r.Remove('persistenceReplayTargets');Assert-KmcChunk7RepeatedWorldRequest $r}
Reject 'ordered-dictionary-save-family' {$r=New-RepeatedRunnerRequest;$r['scenario']='chunk7-persistence-save';Assert-KmcChunk7RepeatedWorldRequest $r}
 foreach($mutation in @('fourth-load','load-text','native-write','missing-callback','callback-text','callback-epoch','retained-player','foreign-state','retained-actor','changed-owner','generation-text','free-debt','extra-prepare','unrelated-prepare','extra-debt','missing-presentation','presentation-owner-form','world-owner-form','late-observer','dropped-events','wrong-hook','extra-owner','old-command','pending-worker','subscription-growth','subscription-type','stale-horse-subscriber','changed-process-subscriber','identity-collision','identity-release','identity-unregistered','false-allocation-proof','false-diagnostics-proof','input-change','unsafely-settled','retained-observer','stale-selection','repeat-area-not-load','missing-restored-slot','replayed-mount','extra-save','duplicate-fact','dispatch-actor-substitution','cleanup-actor-substitution','callback-actor-from-next-world','callback-game-substituted-with-state')){
    Reject $mutation {$a=Copy-Test $threeLoad;$r=$a.observations.repeatedWorlds;switch($mutation){
      'fourth-load' {$r.nativeLoadRequests=4}
      'load-text' {$r.nativeLoadRequests='3'}
      'native-write' {$r.nativeWriteRequests=1}
      'missing-callback' {$r.requests[0].callbackObserved=$false}
      'callback-text' {$r.requests[0].callbackObserved='true'}
      'callback-epoch' {$r.requests[0].callbackNativeLoads=3}
      'retained-player' {$r.worlds[1].playerObject=$r.worlds[0].playerObject}
      'foreign-state' {$r.worlds[1].stateObject=2002}
      'retained-actor' {$r.worlds[1].actors[0].object=$r.worlds[0].actors[0].object}
      'changed-owner' {$r.worlds[1].owners[0].riderId=$horse}
      'generation-text' {$r.worlds[1].owners[0].generation='1'}
      'free-debt' {$r.worlds[1].actors[0].native.Standard=0.0}
      'extra-prepare' {$r.restoration.events+=[pscustomobject]@{kind='prepare-before';actorId=$first}}
      'unrelated-prepare' {$r.restoration.events+=[pscustomobject]@{kind='prepare-before';actorId='99999999-9999-4999-8999-999999999999'}}
      'extra-debt' {$r.restoration.events[0].kind='restore-debt-after'}
      'missing-presentation' {$r.restoration.events=@($r.restoration.events|Where-Object kind -CNE 'all-owner-presentation-restored')}
      'presentation-owner-form' {$r.restoration.events[8].snapshot.owners[0].riderId=([Guid]$first).ToString('N')}
      'world-owner-form' {$r.worlds[0].owners[0].riderId=$first}
      'late-observer' {$r.restoration.installedBeforeWorldLoad=$false}
      'dropped-events' {$r.retention.dropped=1}
      'wrong-hook' {$r.retention.hooks[0].moduleMvid='99999999-9999-4999-8999-999999999999'}
      'extra-owner' {$r.retention.events[0].state.owners+=Copy-Test $owners[0]}
      'old-command' {$r.retention.events[0].state.commandOwners=1}
      'pending-worker' {$r.retention.events[0].state.pendingWrites=$true}
      'subscription-growth' {$r.worlds[2].subscriptions.entries+=Copy-Test $r.worlds[2].subscriptions.entries[0]}
      'subscription-type' {$r.worlds[2].subscriptions.ownedInterfaceEntries='1'}
       'stale-horse-subscriber' {@($r.worlds[1].subscriptions.entries|Where-Object type -CEQ 'KingmakerMountedCombat.Integration.HorseCompanionAddPet')[0].identity=@($r.worlds[0].subscriptions.entries|Where-Object type -CEQ 'KingmakerMountedCombat.Integration.HorseCompanionAddPet')[0].identity}
       'changed-process-subscriber' {@($r.worlds[1].subscriptions.entries|Where-Object type -CEQ 'KingmakerMountedCombat.Integration.MountedLifecycleSubscriber')[0].identity=77123}
      'identity-collision' {$r.identityRetention.faults=1}
      'identity-release' {$r.identityRetention.retainedAfterRelease=5}
      'identity-unregistered' {$r.identityRetention.ids=@($r.identityRetention.ids|Where-Object{$_-ne$r.worlds[1].playerObject})}
      'false-allocation-proof' {$r.allocationRateProof=$true}
      'false-diagnostics-proof' {$r.diagnosticsOffProof=$true}
      'input-change' {$r.inputUnchanged=$false}
      'unsafely-settled' {$r.settled.owners+=Copy-Test $owners[0]}
      'retained-observer' {$r.observersReleased=$false}
      'stale-selection' {$r.worlds[1].selectionRebinding[0].object=$r.worlds[0].selectionRebinding[0].object}
      'repeat-area-not-load' {$r.kind='area-transfer'}
      'missing-restored-slot' {$r.worlds[1].nativeSlots=@($r.worlds[1].nativeSlots|Select-Object -First 3)}
      'replayed-mount' {$r.worlds[1].nativeCastRequests=1}
      'extra-save' {$r.worlds[1].snapshotCount=1}
      'duplicate-fact' {$r.worlds[1].duplicateFacts=1}
      'dispatch-actor-substitution' {$r.requests[0].before.actors[0].object=$r.worlds[1].actors[0].object}
      'cleanup-actor-substitution' {$r.settled.actors[0].object=$r.worlds[0].actors[0].object}
      'callback-actor-from-next-world' {$r.retention.events[0].state.actors[0].object=$r.worlds[1].actors[0].object;$r.finalRetention=Copy-Test $r.retention}
      'callback-game-substituted-with-state' {$r.retention.events[0].state.gameObject=$r.worlds[1].stateObject;$r.finalRetention=Copy-Test $r.retention}
    };Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
}
 foreach($mutation in @('changed-player','replaced-view','stale-mover','same-area-state','missing-suspension','missing-neighbour-resume','debt-replay','hidden-autosave','unselected-target','retained-rider-override','foreign-pending-destroy','suspension-not-callback','settled-still-callback','replaced-area-horse-subscriber')){
    Reject $mutation {$a=Copy-Test $threeAreas;$r=$a.observations.repeatedWorlds;switch($mutation){
      'changed-player' {$r.worlds[1].playerObject++}
      'replaced-view' {$r.worlds[1].actors[0].view++}
      'stale-mover' {$r.worlds[1].actors[0].agentOverride=$r.worlds[0].actors[0].agentOverride;$r.worlds[1].actors[0].overrides=@($r.worlds[0].actors[0].agentOverride)}
      'same-area-state' {$r.worlds[1].areaStateObject=$r.worlds[0].areaStateObject}
      'missing-suspension' {$r.worlds[2].areaSuspensions=1}
      'missing-neighbour-resume' {$r.worlds[2].areaResumes=3}
      'debt-replay' {$r.worlds[2].semanticRestores=8}
      'hidden-autosave' {$r.nativeWriteRequests=1}
      'unselected-target' {$r.requests[0].area='f'*32}
      'retained-rider-override' {$r.settled.actors[0].overrides=@(123)}
      'foreign-pending-destroy' {$r.retention.events[0].state.actors[0].overrides=@(12345);$r.finalRetention=Copy-Test $r.retention}
      'suspension-not-callback' {$r.retention.events[0].state.subscriptions.executing=$false;$r.finalRetention=Copy-Test $r.retention}
      'settled-still-callback' {$r.worlds[1].subscriptions.executing=$true}
       'replaced-area-horse-subscriber' {@($r.worlds[1].subscriptions.entries|Where-Object type -CEQ 'KingmakerMountedCombat.Integration.HorseCompanionAddPet')[0].identity=76123}
    };Assert-KmcChunk7PersistenceObservation $areaRequest $a 'PASS'}
}
Write-Host ('CHUNK7 REPEATED WORLD READER PASS='+$passed+' FAIL=0; synthetic contract fixtures only, native route unqualified.')
