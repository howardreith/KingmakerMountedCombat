$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Test-Chunk7PersistenceEvidence.ps1')
$loadRequest=Copy-Test $coldRequest;$loadRequest.persistenceCase='multi-exploration-three-loads-manual'
# SYNTHETIC subscriber fixtures, selected by ReplayWorld's -Subscribers. Each names its own rows literally and none
# is read from the reader's reviewed contracts, so a fixture and the reader can only agree by both being right.
# They are not native evidence.
#   historical  what chunk7b previews up to .99 held (the rows this file has always built; the default).
#   current     what a chunk7-repeated-worlds-v3 producer holds.
function ReplayWorld([int]$Load,[int]$AreaOrdinal,[bool]$Mounted=$true,[string]$Subscribers='historical'){
    $actors=@();for($i=0;$i-lt4;$i++){$actor=$native[$i];$idBase=$Load*100+$i*10;$rider=$i-cin@(0,2);$component=if($Mounted-and$rider){$idBase+6+$AreaOrdinal*1000}else{0}
        $actors+=[pscustomobject]@{id=$actor.Id;object=$idBase+1;descriptor=$idBase+2;view=$idBase+3;viewObject=$idBase+4;inState=$true;commandsEmpty=$true;overrides=@(if($Mounted-and$rider){$component});agentOverride=$component;native=(Copy-Test $actor)}}
    $nativeOwners=@();if($Mounted){$nativeOwners=@($owners|ForEach-Object{$v=Copy-Test $_;$v.riderId=([Guid]$v.riderId).ToString('N');$v.mountId=([Guid]$v.mountId).ToString('N');$v|Add-Member NoteProperty retainsObligations $true;$v})}
    # The synthetic archive's Horse owners, counted from its own profile ids rather than by the reader.
    $horseFacts=@($archive.Pairs|Where-Object{$_.ProfileId-clike'*-horse-v1'}).Count
    if($horseFacts-lt1-or$horseFacts-gt2){throw 'Synthetic replay archive needs one or two exact Horse feature facts.'}
    # Row order is the one this file has always built: lifecycle, Horse facts, then the other process-owned subscribers.
    # Other test files import this function alone, so its fixtures live inside it.
    $historicalLifecycle=@('IAreaLoadingStagesHandler','IPartyLeaveAreaHandler','IPartyHandler','IGameModeHandler','IPartyCombatHandler','IUnitHandler','IUnitViewAttachedUIHandler','IUnitLifeStateChanged','IUnitCombatHandler','IUnitFinallyDeadHandler','IInGameHandler','ITurnBasedModeEnabledHandler','ISceneHandler')
    $controlInterfaces=@('IAbilityTargetSelectionUIHandler','IClickActionHandler')
    $groups=switch -CaseSensitive ($Subscribers){
        'historical' {@(@('MountedLifecycleSubscriber',-999,$historicalLifecycle),@('NativeMountedControlService',-998,$controlInterfaces))}
        'current' {@(@('MountedLifecycleSubscriber',-999,@($historicalLifecycle+@('IUnitFallProneHandler','ITeleportHandler'))),@('NativeMountedControlService',-998,$controlInterfaces),
            @('MountedCombatDefenseService',-997,@('IUnitNewCombatRoundHandler','IUnitCombatHandler')),@('RidingDogTackService',-996,@('IUnitViewAttachedUIHandler','ISceneHandler')))}
        default {throw ('Unknown synthetic subscriber fixture: '+$Subscribers)}
    }
    $subscriberEntries=@()
    for($group=0;$group-lt$groups.Count;$group++){
        if($group-eq1){for($i=0;$i-lt$horseFacts;$i++){$subscriberEntries+=[pscustomobject]@{interface='Kingmaker.PubSubSystem.ISceneHandler';type='KingmakerMountedCombat.Integration.HorseCompanionAddPet';identity=$Load*1000+11+$i}}}
        foreach($interface in $groups[$group][2]){$subscriberEntries+=[pscustomobject]@{interface='Kingmaker.PubSubSystem.'+$interface;type='KingmakerMountedCombat.Integration.'+$groups[$group][0];identity=$groups[$group][1]}}
    }
    $subscriptions=[pscustomobject]@{assemblyMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';executing=$false;uniqueOwnedSubscribers=$groups.Count+$horseFacts;ownedInterfaceEntries=$subscriberEntries.Count;entries=$subscriberEntries}
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
function New-ReplayArtifact($Req,[bool]$Areas,[string]$Subscribers='historical'){
    $loads=if($Areas){1}else{3};$worlds=@(ReplayWorld 1 0 $true $Subscribers);$qs=@();$retentionEvents=@()
    if($Areas){for($i=1;$i-le3;$i++){
        $before=Copy-Test $worlds[$worlds.Count-1];$qs+=[pscustomobject]@{kind='area-transfer';ordinal=$i;enterPoint=$Req.persistenceReplayTargets[$i-1].enterPoint;area=$Req.persistenceReplayTargets[$i-1].area;autoSaveMode='None';before=$before}
        $suspended=ReplayWorld 1 ($i-1) $false $Subscribers;$suspended.areaSuspensions=$i;$suspended.subscriptions.executing=$true
        $retentionEvents+=[pscustomobject]@{kind='area-suspended';before=$i-1;after=$i;state=$suspended}
        $worlds+=ReplayWorld 1 $i $true $Subscribers
        $retentionEvents+=[pscustomobject]@{kind='area-restored';before=($i-1)*2;after=$i*2;state=(Copy-Test $worlds[$worlds.Count-1])}
    }}else{for($i=2;$i-le3;$i++){
        $before=Copy-Test $worlds[$worlds.Count-1];$qs+=[pscustomobject]@{kind='native-load';ordinal=$i;before=$before;fileName=$Req.persistenceLoad.fileName;sha256=$Req.persistenceLoad.sha256;callbackObserved=$true;callbackNativeLoads=$i;callbackFrame=$i*100-1}
        $disposal=ReplayWorld ($i-1) 0 $false $Subscribers;$disposal.nativeDisposals=$i-1;$disposal.nativePostLoads=0
        $retentionEvents+=[pscustomobject]@{kind='native-world-cleanup-before-disposal';before=$i-2;after=$i-1;state=$disposal};$worlds+=ReplayWorld $i 0 $true $Subscribers
    }}
    $probe=[pscustomobject]@{contract='chunk7-repeated-world-retention-v1';complete=$true;installedBeforeWorldLoad=$true;maximumEvents=24;errors=0;dropped=0;hooks=@(foreach($m in @('PrepareForNativeWorldDisposal','SuspendAreaPair','RestoreAreaPair')){[pscustomobject]@{method=$m;declaringType='KingmakerMountedCombat.Integration.MountedPersistenceService';token='06003000';moduleMvid=$Req.dllMvid}});events=$retentionEvents}
    $settled=ReplayWorld $loads $(if($Areas){3}else{0}) $false $Subscribers
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
# F5 r27 (chunk7-repeated-worlds-v2): weak-reference subscriber serials replace address-derived hash identities.
function Get-SerialInventories($R){@(@($R.worlds|ForEach-Object{$_.subscriptions})+@($R.requests|ForEach-Object{$_.before.subscriptions})+@($R.retention.events|ForEach-Object{$_.state.subscriptions})+@($R.settled.subscriptions))}
function ConvertTo-SerialReplay($Artifact){
    $a=Copy-Test $Artifact;$r=$a.observations.repeatedWorlds;$map=[ordered]@{}
    foreach($inv in Get-SerialInventories $r){foreach($row in $inv.entries){$k=[string]$row.identity;if(-not$map.Contains($k)){$map[$k]=$map.Count+1};$row|Add-Member NoteProperty serial ([long]$map[$k])}}
    $r.contract='chunk7-repeated-worlds-v2';$r|Add-Member NoteProperty subscriberIdentity 'weak-reference-serial-v1'
    $r|Add-Member NoteProperty subscriberSerials ([pscustomobject]@{contract='weak-reference-serial-v1';capacity=256;assigned=$map.Count;faults=0});$r.finalRetention=Copy-Test $r.retention;$a
}
function Get-HorseRows($Subscriptions){@($Subscriptions.entries|Where-Object type -CEQ 'KingmakerMountedCombat.Integration.HorseCompanionAddPet')}
$serialLoads=ConvertTo-SerialReplay $threeLoad;$serialAreas=ConvertTo-SerialReplay $threeAreas
Check 'v2 serial subscribers: three native loads' {Assert-KmcChunk7PersistenceObservation $loadRequest $serialLoads 'PASS'}
Check 'v2 serial subscribers: three area transfers' {Assert-KmcChunk7PersistenceObservation $areaRequest $serialAreas 'PASS'}
foreach($version in @('0.1.0-chunk7b-preview.95','0.1.0-chunk7b-preview.96')){
    foreach($areas in @($false,$true)){
        $q=Copy-Test $(if($areas){$areaRequest}else{$loadRequest});$q.productVersion=$version
        $a=Copy-Test $(if($areas){$serialAreas}else{$serialLoads});$a.productVersion=$version
        Check ("$version serial evidence accepted, areas=$areas") {Assert-KmcChunk7PersistenceObservation $q $a 'PASS'}
        Reject ("$version refuses complete legacy schema downgrade, areas=$areas") {
            $v=Copy-Test $a;$r=$v.observations.repeatedWorlds;$r.contract='chunk7-repeated-worlds-v1'
            $r.PSObject.Properties.Remove('subscriberIdentity');$r.PSObject.Properties.Remove('subscriberSerials')
            foreach($inv in Get-SerialInventories $r){foreach($row in $inv.entries){$row.PSObject.Properties.Remove('serial')}}
            $r.finalRetention=Copy-Test $r.retention
            Assert-KmcChunk7PersistenceObservation $q $v 'PASS'
        }
    }
}
Check 'preview.94 hash-only evidence keeps its original path' {
    $q=Copy-Test $loadRequest;$q.productVersion='0.1.0-chunk7b-preview.94'
    $a=Copy-Test $threeLoad;$a.productVersion=$q.productVersion
    Assert-KmcChunk7PersistenceObservation $q $a 'PASS'
}
Check 'v2: a new Horse subscriber that repeats a collected one''s hash (F5 r27) is not stale' {
    $a=Copy-Test $serialLoads;$r=$a.observations.repeatedWorlds;$old=(Get-HorseRows $r.worlds[1].subscriptions)[0].identity
    foreach($inv in @($r.worlds[2].subscriptions,$r.settled.subscriptions)){(Get-HorseRows $inv)[0].identity=$old}
    Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'
}
Reject 'v1: the same hash repeat stays refused on its original hash path' {$a=Copy-Test $threeLoad;$r=$a.observations.repeatedWorlds;$old=(Get-HorseRows $r.worlds[1].subscriptions)[0].identity;foreach($inv in @($r.worlds[2].subscriptions,$r.settled.subscriptions)){(Get-HorseRows $inv)[0].identity=$old};Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
foreach($mutation in @('stale-serial','stale-serial-and-identity','process-serial-changed','mixed-inventory','serial-two-identities','serial-two-identities-across-worlds','serial-zero','serial-text','serial-beyond-bound','registry-fault','registry-missing','registry-assigned-short','registry-extra-field','identity-marker','v1-contract-with-serials','v2-contract-without-serials','duplicate-serial-row','unique-count')){
    Reject ('v2 '+$mutation) {$a=Copy-Test $serialLoads;$r=$a.observations.repeatedWorlds;$h1=(Get-HorseRows $r.worlds[1].subscriptions)[0];$h0=(Get-HorseRows $r.worlds[0].subscriptions)[0]
        switch($mutation){
          'stale-serial' {$h1.serial=$h0.serial;$h1.identity=$h0.identity}
          'stale-serial-and-identity' {foreach($inv in @($r.worlds[1].subscriptions)+@($r.requests[1].before.subscriptions)){(Get-HorseRows $inv)[0].serial=$h0.serial;(Get-HorseRows $inv)[0].identity=$h0.identity}}
          'process-serial-changed' {foreach($row in @($r.worlds[1].subscriptions.entries|Where-Object type -CEQ 'KingmakerMountedCombat.Integration.MountedLifecycleSubscriber')){$row.serial=99;$row.identity=-997};$r.subscriberSerials.assigned=99}
          'mixed-inventory' {$r.worlds[1].subscriptions.entries[0].PSObject.Properties.Remove('serial')}
          'serial-two-identities' {$h1.identity=12345}
          'serial-two-identities-across-worlds' {foreach($row in @($r.settled.subscriptions.entries|Where-Object type -CEQ 'KingmakerMountedCombat.Integration.NativeMountedControlService')){$row.identity=-996}}
          'serial-zero' {$h1.serial=0}
          'serial-text' {$h1.serial='5'}
          'serial-beyond-bound' {$h1.serial=257}
          'registry-fault' {$r.subscriberSerials.faults=1}
          'registry-missing' {$r.PSObject.Properties.Remove('subscriberSerials')}
          'registry-assigned-short' {$r.subscriberSerials.assigned=2}
          'registry-extra-field' {$r.subscriberSerials|Add-Member NoteProperty retained 1}
          'identity-marker' {$r.subscriberIdentity='runtime-hash-v1'}
          'v1-contract-with-serials' {$r.contract='chunk7-repeated-worlds-v1'}
          'v2-contract-without-serials' {foreach($inv in Get-SerialInventories $r){foreach($row in $inv.entries){$row.PSObject.Properties.Remove('serial')}}}
          'duplicate-serial-row' {$r.worlds[1].subscriptions.entries+=Copy-Test $h1;$r.worlds[1].subscriptions.ownedInterfaceEntries++}
          'unique-count' {$r.worlds[1].subscriptions.uniqueOwnedSubscribers=5}
        };$r.finalRetention=Copy-Test $r.retention;Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
}
Reject 'v2 area transfer replaced the Horse subscriber serial' {$a=Copy-Test $serialAreas;$r=$a.observations.repeatedWorlds;(Get-HorseRows $r.worlds[1].subscriptions)[0].serial=200;$r.subscriberSerials.assigned=200;Assert-KmcChunk7PersistenceObservation $areaRequest $a 'PASS'}
Check 'v2 area transfer keeps the Horse serial even if its hash label changed' {$a=Copy-Test $serialAreas;$r=$a.observations.repeatedWorlds;$s=(Get-HorseRows $r.worlds[0].subscriptions)[0].serial
    foreach($inv in Get-SerialInventories $r){foreach($row in @(Get-HorseRows $inv|Where-Object serial -EQ $s)){$row.identity=4242}};$r.finalRetention=Copy-Test $r.retention;Assert-KmcChunk7PersistenceObservation $areaRequest $a 'PASS'}
# b195 x4c: the reviewed capital pins are refused before launch and by the request reader.
Check 'route refusal names the reviewed capital evidence' {$c=@(Get-KmcChunk7KnownCapitalAreaPins);if($c.Count-ne1-or$c[0].area-cne'ead426a6c23d39548a670ee515d77df4'-or$c[0].enterPoint-cne'fd225a93828d26449803a4847947f8c1'-or$c[0].evidenceRunId-cne'20261019-c7b-world48-ownerseed-b23s3-areaauto'){throw 'Capital pin list differs.'}}
foreach($leg in 0..2){
 Reject ('repeated route entering the capital at leg '+($leg+1)) {$q=Copy-Test $areaRequest;$q.persistenceReplayTargets[$leg].enterPoint='fd225a93828d26449803a4847947f8c1';$q.persistenceReplayTargets[$leg].area='ead426a6c23d39548a670ee515d77df4';Assert-KmcChunk7RepeatedWorldRequest $q}
}
Reject 'repeated route naming only the capital area' {$q=Copy-Test $areaRequest;$q.persistenceReplayTargets[1].area='ead426a6c23d39548a670ee515d77df4';Assert-KmcChunk7RepeatedWorldRequest $q}
Check 'runner refuses known capital pins before intake' {$r=[IO.File]::ReadAllText((Join-Path $repo 'scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1'));if($r-notmatch'Assert-KmcChunk7RouteAvoidsKnownCapitals \$PersistenceReplayAreaEnterPoints \$PersistenceReplayAreaAreas'){throw 'Runner lacks the prelaunch capital refusal.'};try{Assert-KmcChunk7RouteAvoidsKnownCapitals @('fd225a93828d26449803a4847947f8c1') @('b10e86516b6437d45a629931c30936fe');throw 'admitted'}catch{if($_.Exception.Message-notmatch'known capital entry point'){throw}}}
# b194 x4c review: KMC Horse owners count every Horse-mounted profile, exactly as the product does.
function HorsePair([string]$Profile){[pscustomobject]@{ProfileId=$Profile}}
function HorseSubscriptions([int]$Count){
 $s=Copy-Test (ReplayWorld 1 0).subscriptions;$horse='KingmakerMountedCombat.Integration.HorseCompanionAddPet'
 $entries=@($s.entries|Where-Object type -CNE $horse)
 for($i=0;$i-lt$Count;$i++){$entries+=[pscustomobject]@{interface='Kingmaker.PubSubSystem.ISceneHandler';type=$horse;identity=7000+$i}}
 $s.entries=$entries;$s.uniqueOwnedSubscribers=2+$Count;$s.ownedInterfaceEntries=15+$Count;$s
}
# Horse owners are counted under a reviewed subscriber contract. These historical cases keep their original
# expectations under the historical contract.
$r1=Get-KmcChunk7ReplaySubscriberContract 'kmc-global-subscribers-r1';$r2=Get-KmcChunk7ReplaySubscriberContract 'kmc-global-subscribers-r2'
Check 'KMC Horse owners: Small Halfling Horse plus Medium Horse is two' {if((Get-KmcChunk7KmcHorseOwnerCount @((HorsePair 'small-default-halfling-horse-v1'),(HorsePair 'medium-humanoid-horse-v1')) $r1)-ne2){throw 'Small plus Medium Horse owners differ from two.'}}
Check 'KMC Horse owners: two Medium Horses is two' {if((Get-KmcChunk7KmcHorseOwnerCount @((HorsePair 'medium-humanoid-horse-v1'),(HorsePair 'medium-humanoid-horse-v1')) $r1)-ne2){throw 'Two Medium Horse owners differ from two.'}}
Check 'KMC Horse owners: Mammoth plus Horse is one' {if((Get-KmcChunk7KmcHorseOwnerCount @((HorsePair 'medium-humanoid-mammoth-v1'),(HorsePair 'medium-humanoid-horse-v1')) $r1)-ne1){throw 'Mammoth plus Horse owners differ from one.'}}
Check 'KMC Horse owners: unknown or malformed profiles do not count' {if((Get-KmcChunk7KmcHorseOwnerCount @((HorsePair 'small-other-horse-v9'),(HorsePair 'MEDIUM-HUMANOID-HORSE-V1'),[pscustomobject]@{ProfileId=1},$null) $r1)-ne0){throw 'Unknown profiles counted as KMC Horse owners.'}}
Check 'two Horse subscribers pass for a Small plus Medium archive' {$count=Get-KmcChunk7KmcHorseOwnerCount @((HorsePair 'small-default-halfling-horse-v1'),(HorsePair 'medium-humanoid-horse-v1')) $r1;Assert-KmcChunk7RepeatedSubscriptionBoundary $null (HorseSubscriptions 2) $false $count $r1}
Check 'one Horse subscriber passes for a Mammoth plus Horse archive' {$count=Get-KmcChunk7KmcHorseOwnerCount @((HorsePair 'medium-humanoid-mammoth-v1'),(HorsePair 'medium-humanoid-horse-v1')) $r1;Assert-KmcChunk7RepeatedSubscriptionBoundary $null (HorseSubscriptions 1) $false $count $r1}
Check 'two Horse subscribers refused for a Mammoth plus Horse archive' {$count=Get-KmcChunk7KmcHorseOwnerCount @((HorsePair 'medium-humanoid-mammoth-v1'),(HorsePair 'medium-humanoid-horse-v1')) $r1;if($count-ne1){throw 'Mammoth plus Horse count differs from one.'};$refused=$false;try{Assert-KmcChunk7RepeatedSubscriptionBoundary $null (HorseSubscriptions 2) $false $count $r1}catch{if($_.Exception.Message-notmatch'subscriber'){throw};$refused=$true};if(-not$refused){throw 'Two Horse subscribers were admitted for one KMC Horse owner.'}}
# The historical Horse owner set stays the two reviewed ids. A source-text pin on the profile catalog stood here; it
# stopped matching in November 2026 when the catalog gained the Gnome, female and build-variant Small profiles
# (23de1eb1, be906d8f), and it had told
# the reader to follow them. The current set now belongs to r2 and is compared with the compiled product's own
# profiles by Test-Chunk7ReplaySubscriberContract.ps1.
Check 'historical Horse owner profiles stay the two reviewed ids and the current revision only adds to them' {
 if(($r1.horseProfileIds-join',')-cne'medium-humanoid-horse-v1,small-default-halfling-horse-v1'){throw 'Historical reader Horse profile set differs.'}
 if(@($r1.horseProfileIds|Where-Object{$_-cnotin$r2.horseProfileIds}).Count-ne0){throw 'The current revision dropped a historical Horse profile.'}
}

# ---- Reviewed subscriber contracts (known-issues T02) ---------------------------------------------------------
# Every artifact below is SYNTHETIC. "historical" rows are the fixtures above; "current" rows are built from the
# literal current fixture at the top of this file. Nothing here is native execution evidence.
function RejectAs([string]$Name,[string]$Pattern,[scriptblock]$Body){$message=$null;try{& $Body}catch{$message=$_.Exception.Message};if($null-eq$message){throw ('Unexpected acceptance: '+$Name)};if($message-notmatch$Pattern){throw ('Refused for another reason: '+$Name+' :: '+$message)};$script:passed++;Write-Host ('PASS refuses '+$Name)}
$kmcType='KingmakerMountedCombat.Integration.';$eventName='Kingmaker.PubSubSystem.'
function ConvertTo-CurrentReplay($Artifact){$a=ConvertTo-SerialReplay $Artifact;$r=$a.observations.repeatedWorlds;$r.contract='chunk7-repeated-worlds-v3';$r|Add-Member NoteProperty subscriberContract 'kmc-global-subscribers-r2';$a}
function With-Producer($Request,$Artifact,[string]$Version){$q=Copy-Test $Request;$q.productVersion=$Version;$a=Copy-Test $Artifact;$a.productVersion=$Version;@($q,$a)}
# Applies one change to every inventory of the artifact, so a refusal cannot come from two worlds merely disagreeing.
function Edit-Inventories($Artifact,[scriptblock]$Change){$a=Copy-Test $Artifact;$r=$a.observations.repeatedWorlds;foreach($inventory in Get-SerialInventories $r){& $Change $inventory};$r.finalRetention=Copy-Test $r.retention;$a}
function Rows($Inventory,[string]$Type){@($Inventory.entries|Where-Object type -CEQ ($kmcType+$Type))}
function Remove-Rows($Inventory,[string]$Type,[string]$Interface=''){$Inventory.entries=@($Inventory.entries|Where-Object{-not($_.type-ceq($kmcType+$Type)-and($Interface-eq''-or$_.interface-ceq($eventName+$Interface)))});$Inventory.ownedInterfaceEntries=$Inventory.entries.Count}
function Add-Row($Inventory,[string]$Type,[string]$Interface,$Instance){$row=Copy-Test $Instance;$row.type=$kmcType+$Type;$row.interface=$eventName+$Interface;$Inventory.entries=@($Inventory.entries)+@($row);$Inventory.ownedInterfaceEntries=$Inventory.entries.Count}
function Set-Instance($Row,[long]$Serial,[int]$Identity){$Row.serial=$Serial;$Row.identity=$Identity}
$currentLoads=ConvertTo-CurrentReplay (New-ReplayArtifact $loadRequest $false 'current');$currentAreas=ConvertTo-CurrentReplay (New-ReplayArtifact $areaRequest $true 'current')
Check 'reviewed revisions: r1 is implicit, r2 is declarable from 0.2.11 and adds exactly the fall and teleport lifecycle handlers' {
 $l1=@(@($r1.processOwned|Where-Object type -CEQ ($kmcType+'MountedLifecycleSubscriber'))[0].interfaces);$l2=@(@($r2.processOwned|Where-Object type -CEQ ($kmcType+'MountedLifecycleSubscriber'))[0].interfaces)
 $added=@($l2|Where-Object{$_-cnotin$l1}|Sort-Object -CaseSensitive)-join','
 if($null-ne$r1.firstRelease-or$r2.firstRelease-ne11-or$l1.Count-ne13-or@($l1|Where-Object{$_-cnotin$l2}).Count-ne0-or$added-cne($eventName+'ITeleportHandler,'+$eventName+'IUnitFallProneHandler')){throw 'Reviewed revision table differs.'}
}
Check 'SYNTHETIC current contract (v3 declaring r2): three native loads' {Assert-KmcChunk7PersistenceObservation $loadRequest $currentLoads 'PASS'}
Check 'SYNTHETIC current contract (v3 declaring r2): three area transfers' {Assert-KmcChunk7PersistenceObservation $areaRequest $currentAreas 'PASS'}
foreach($version in @('0.2.11','0.2.12')){Check ("SYNTHETIC current contract accepted for producer $version") {$p=With-Producer $loadRequest $currentLoads $version;Assert-KmcChunk7PersistenceObservation $p[0] $p[1] 'PASS'}}
foreach($version in @('0.2.10','0.2.0','0.1.0-chunk7b-preview.96','0.1.0-chunk7b-preview.103')){RejectAs ("the current contract claimed by producer $version, which cannot carry it") 'Unsupported repeated-world evidence: producer' {$p=With-Producer $loadRequest $currentLoads $version;Assert-KmcChunk7PersistenceObservation $p[0] $p[1] 'PASS'}}
# Selection is by declared identity only. Each refusal below must be the explicit unsupported one.
RejectAs 'v3 without a declared subscriber contract' 'v3 must declare its subscriber contract revision' {$a=Copy-Test $currentLoads;$a.observations.repeatedWorlds.PSObject.Properties.Remove('subscriberContract');Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'v3 declaring a revision that is not a string' 'v3 must declare its subscriber contract revision' {$a=Copy-Test $currentLoads;$a.observations.repeatedWorlds.subscriberContract=2;Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'v3 declaring an unknown revision' 'no reviewed subscriber contract revision kmc-global-subscribers-r9' {$a=Copy-Test $currentLoads;$a.observations.repeatedWorlds.subscriberContract='kmc-global-subscribers-r9';Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'v3 declaring the implicit historical revision' 'implicit historical contract and cannot be declared' {$a=Copy-Test $currentLoads;$a.observations.repeatedWorlds.subscriberContract='kmc-global-subscribers-r1';Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'an unknown evidence contract' 'Unsupported repeated-world evidence contract: chunk7-repeated-worlds-v4' {$a=Copy-Test $currentLoads;$a.observations.repeatedWorlds.contract='chunk7-repeated-worlds-v4';Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'an evidence contract that is not a string' 'Unsupported repeated-world evidence contract' {$a=Copy-Test $currentLoads;$a.observations.repeatedWorlds.contract=3;Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'v2 evidence declaring a subscriber contract' 'cannot declare a subscriber contract revision' {$a=Copy-Test $serialLoads;$a.observations.repeatedWorlds|Add-Member NoteProperty subscriberContract 'kmc-global-subscribers-r2';Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
# A valid inventory of one contract is never accepted under the other because some count happens to fit.
RejectAs 'current rows relabelled as the historical v2 contract' 'reviewed contract kmc-global-subscribers-r1' {$a=Copy-Test $currentLoads;$r=$a.observations.repeatedWorlds;$r.contract='chunk7-repeated-worlds-v2';$r.PSObject.Properties.Remove('subscriberContract');Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'historical rows relabelled as the current contract' 'reviewed contract kmc-global-subscribers-r2' {$a=Copy-Test $serialLoads;$r=$a.observations.repeatedWorlds;$r.contract='chunk7-repeated-worlds-v3';$r|Add-Member NoteProperty subscriberContract 'kmc-global-subscribers-r2';Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
# The historical contract describes chunk7b previews up to .99 and nothing later.
Check 'historical contract accepted for chunk7b preview.99, the last build it describes' {$p=With-Producer $loadRequest $serialLoads '0.1.0-chunk7b-preview.99';Assert-KmcChunk7PersistenceObservation $p[0] $p[1] 'PASS'}
foreach($version in @('0.1.0-chunk7b-preview.100','0.1.0-chunk7b-preview.103')){RejectAs ("historical-looking rows from $version, whose subscriber set no contract describes") 'no reviewed contract describes' {$p=With-Producer $loadRequest $serialLoads $version;Assert-KmcChunk7PersistenceObservation $p[0] $p[1] 'PASS'}}
RejectAs 'historical evidence from a release-scheme producer' 'Unrecognised checkpoint producer revision' {$p=With-Producer $loadRequest $serialLoads '0.2.11';Assert-KmcChunk7PersistenceObservation $p[0] $p[1] 'PASS'}
# Exact membership: missing, duplicate and foreign handlers.
RejectAs 'r2 missing handler' 'does not list exactly its reviewed interfaces' {$a=Edit-Inventories $currentLoads {param($i)Remove-Rows $i 'MountedLifecycleSubscriber' 'ITeleportHandler'};Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'r2 with the historical 13-interface lifecycle' 'MountedLifecycleSubscriber does not list exactly' {$a=Edit-Inventories $currentLoads {param($i)Remove-Rows $i 'MountedLifecycleSubscriber' 'ITeleportHandler';Remove-Rows $i 'MountedLifecycleSubscriber' 'IUnitFallProneHandler'};Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'r2 foreign handler beside the reviewed ones' 'does not list exactly its reviewed interfaces' {$a=Edit-Inventories $currentLoads {param($i)Add-Row $i 'MountedLifecycleSubscriber' 'IAttackHandler' @(Rows $i 'MountedLifecycleSubscriber')[0]};Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'r2 foreign handler replacing a reviewed one at the same count' 'NativeMountedControlService does not list exactly' {$a=Edit-Inventories $currentLoads {param($i)@(Rows $i 'NativeMountedControlService'|Where-Object interface -CEQ ($eventName+'IClickActionHandler'))[0].interface=$eventName+'IAttackHandler'};Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'r2 duplicate handler held by a second instance' 'does not list exactly its reviewed interfaces' {$a=Edit-Inventories $currentLoads {param($i)$row=Copy-Test @(Rows $i 'MountedLifecycleSubscriber')[0];Set-Instance $row 200 -900;$i.entries=@($i.entries)+@($row);$i.ownedInterfaceEntries=$i.entries.Count;$i.uniqueOwnedSubscribers++};$a.observations.repeatedWorlds.subscriberSerials.assigned=200;Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
Reject 'r2 exact duplicate handler row' {$a=Edit-Inventories $currentLoads {param($i)$i.entries=@($i.entries)+@(Copy-Test @(Rows $i 'MountedLifecycleSubscriber')[0]);$i.ownedInterfaceEntries=$i.entries.Count};Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'r2 subscriber type outside the reviewed set' 'a subscriber type outside the reviewed set' {$a=Edit-Inventories $currentLoads {param($i)$row=Copy-Test @(Rows $i 'MountedLifecycleSubscriber')[0];Set-Instance $row 200 -900;$row.type=$kmcType+'DivineMountSpellResistance';$row.interface=$eventName+'IUnitGainLevelHandler';$i.entries=@($i.entries)+@($row);$i.ownedInterfaceEntries=$i.entries.Count;$i.uniqueOwnedSubscribers++};$a.observations.repeatedWorlds.subscriberSerials.assigned=200;Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'r2 missing process-owned subscriber' 'RidingDogTackService does not list exactly' {$a=Edit-Inventories $currentLoads {param($i)Remove-Rows $i 'RidingDogTackService';$i.uniqueOwnedSubscribers--};Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'historical: a foreign handler replacing a reviewed one at the same count' 'kmc-global-subscribers-r1: .*MountedLifecycleSubscriber does not list exactly' {$a=Edit-Inventories $threeLoad {param($i)@(Rows $i 'MountedLifecycleSubscriber'|Where-Object interface -CEQ ($eventName+'ISceneHandler'))[0].interface=$eventName+'IAttackHandler'};Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
# Process ownership: one distinct instance per process-owned subscriber, unchanged across worlds.
RejectAs 'r2 lifecycle rows split across two instances' 'MountedLifecycleSubscriber is not one process-owned instance' {$a=Edit-Inventories $currentLoads {param($i)Set-Instance @(Rows $i 'MountedLifecycleSubscriber')[3] 200 -900;$i.uniqueOwnedSubscribers++};$a.observations.repeatedWorlds.subscriberSerials.assigned=200;Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'r2 two process-owned subscribers under one instance' 'shares its instance with another process-owned subscriber' {$a=Edit-Inventories $currentLoads {param($i)$owner=@(Rows $i 'MountedLifecycleSubscriber')[0];foreach($row in Rows $i 'MountedCombatDefenseService'){Set-Instance $row $owner.serial $owner.identity};$i.uniqueOwnedSubscribers--};Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'r2 Horse fact labelled as a process-owned instance' 'Horse fact subscribers are not exactly one ISceneHandler instance per Horse owner' {$a=Edit-Inventories $currentLoads {param($i)$owner=@(Rows $i 'RidingDogTackService')[0];foreach($row in Rows $i 'HorseCompanionAddPet'){Set-Instance $row $owner.serial $owner.identity};$i.uniqueOwnedSubscribers--};Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'r2 process-owned subscriber replaced after a native load' 'retained stale fact subscribers or changed process-owned subscribers' {$a=Copy-Test $currentLoads;$r=$a.observations.repeatedWorlds;foreach($row in Rows $r.worlds[1].subscriptions 'MountedCombatDefenseService'){Set-Instance $row 200 -900};$r.subscriberSerials.assigned=200;Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
# Per-world identity: a Horse fact belongs to one native world; an area transfer keeps it.
RejectAs 'r2 stale Horse fact kept across a native load' 'retained stale fact subscribers or changed process-owned subscribers' {$a=Copy-Test $currentLoads;$r=$a.observations.repeatedWorlds;$old=@(Rows $r.worlds[0].subscriptions 'HorseCompanionAddPet')[0];foreach($inventory in @($r.worlds[1].subscriptions)+@($r.requests[1].before.subscriptions)){Set-Instance @(Rows $inventory 'HorseCompanionAddPet')[0] $old.serial $old.identity};Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'r2 Horse fact replaced across an area transfer' 'retained stale fact subscribers or changed process-owned subscribers' {$a=Copy-Test $currentAreas;$r=$a.observations.repeatedWorlds;@(Rows $r.worlds[1].subscriptions 'HorseCompanionAddPet')[0].serial=200;$r.subscriberSerials.assigned=200;Assert-KmcChunk7PersistenceObservation $areaRequest $a 'PASS'}
# Serial and hash consistency under the current contract.
RejectAs 'r2 one inventory mixing serial and hash-only rows' 'mixes serial and hash-only rows' {$a=Copy-Test $currentLoads;$a.observations.repeatedWorlds.worlds[1].subscriptions.entries[0].PSObject.Properties.Remove('serial');Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'r2 hash-only rows under the serial contract' 'shape differs from its repeated-world contract' {$a=Copy-Test $currentLoads;$r=$a.observations.repeatedWorlds;foreach($inventory in Get-SerialInventories $r){foreach($row in $inventory.entries){$row.PSObject.Properties.Remove('serial')}};$r.finalRetention=Copy-Test $r.retention;Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
RejectAs 'r2 one serial naming two identities across worlds' 'One subscriber serial carries two native identities' {$a=Copy-Test $currentLoads;$r=$a.observations.repeatedWorlds;foreach($row in Rows $r.settled.subscriptions 'RidingDogTackService'){$row.identity=-901};Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
# Aggregates come from the reviewed set: totals that only describe themselves are refused.
Reject 'r2 rows reported under the historical entry total' {$a=Edit-Inventories $currentLoads {param($i)$i.ownedInterfaceEntries=16};Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
Reject 'r2 rows reported under the historical subscriber total' {$a=Edit-Inventories $currentLoads {param($i)$i.uniqueOwnedSubscribers=3};Assert-KmcChunk7PersistenceObservation $loadRequest $a 'PASS'}
# Horse owners under each contract.
Check 'Horse owners follow the selected contract: a Gnome build variant on a Horse counts under r2 only' {$pairs=@((HorsePair 'small-gnome-female-thin-horse-v1'),(HorsePair 'medium-humanoid-mammoth-v1'));if((Get-KmcChunk7KmcHorseOwnerCount $pairs $r2)-ne1-or(Get-KmcChunk7KmcHorseOwnerCount $pairs $r1)-ne0){throw 'Horse owners do not follow the selected contract.'}}
Check 'Riding Dog profiles are never Horse owners' {if((Get-KmcChunk7KmcHorseOwnerCount @((HorsePair 'small-halfling-male-ridingdog-v1'),(HorsePair 'small-gnome-female-fat-ridingdog-v1')) $r2)-ne0){throw 'A Riding Dog pair counted as a Horse owner.'}}
RejectAs 'Horse owners counted without a reviewed contract' 'counted under a reviewed subscriber contract' {Get-KmcChunk7KmcHorseOwnerCount @((HorsePair 'medium-humanoid-horse-v1')) $null}
RejectAs 'a subscriber boundary read without a reviewed contract' 'requires its reviewed subscriber contract' {Assert-KmcChunk7RepeatedSubscriptionBoundary $null (HorseSubscriptions 1) $false 1 $null}
Write-Host ('CHUNK7 REPEATED WORLD READER PASS='+$passed+' FAIL=0; synthetic contract fixtures only, native route unqualified.')
