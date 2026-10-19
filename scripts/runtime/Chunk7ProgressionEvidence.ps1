Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7ProgressionOwnedEvidence.ps1')
function Test-KmcChunk7ProgressionScenario([string]$Scenario){$Scenario-ceq'chunk7-native-progression'}
function Get-KmcChunk7ProgressionRows {@('chunk7-native-progression','C7-progression-cold-source','C7-progression-native-operation','C7-progression-cleanup','C7-progression-runtime','C7-progression-interrupted')}
function Get-KmcChunk7ProgressionCases {@('horse-native-level','horse-native-dismiss','horse-native-retrain-horse','horse-native-retrain-mammoth')+(Get-KmcChunk7OwnedProgressionCases)}
function Assert-KmcProgressionObject($Value){Assert-KmcOrdinaryInteger $Value ([int]::MinValue) ([int]::MaxValue);if($Value-eq0){throw 'Native progression object identity is zero.'}}
function Assert-KmcProgressionText($Value){if($Value-isnot[string]-or[string]::IsNullOrEmpty($Value)){throw 'Native progression text must be a nonempty JSON string.'}}
function Assert-KmcProgressionTexts($Row,[string[]]$Names){foreach($name in $Names){Assert-KmcProgressionText $Row.$name}}
function Assert-KmcProgressionSnapshot($Row,$Identity,[long]$Xp,[long]$Level,[long]$Money){
    Assert-KmcOrdinaryGuid $Row.actorId
    foreach($key in @('nativeMode','phase')){if($null-ne$Row.$key){Assert-KmcProgressionText $Row.$key}}
    if($null-ne$Row.petId){Assert-KmcOrdinaryGuid $Row.petId}
    if($null-ne$Row.selectedClass){if($Row.selectedClass-isnot[string]-or$Row.selectedClass-cnotmatch'^[a-f0-9]{32}$'){throw 'Native selected class must be an exact GUID string.'}}
    Assert-KmcOrdinarySnapshot $Row $Identity $Xp $Level $Money
}
function Assert-KmcChunk7ProgressionSelection($Request){
    if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
    $has=$null-ne$Request.PSObject.Properties['progression']
    if(-not(Test-KmcChunk7ProgressionScenario $Request.scenario)){if($has){throw 'Progression selection is forbidden under another family.'};return}
    if(-not$has){throw 'Exact native progression selection missing.'};$s=$Request.progression
    $properties=@('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','inventoryRunId','inventoryArtifactSha256','actorId','petId','controlActorId','controlPetId','raceGuid','presetGuid','skeletonName','gender','leftHanded','bodySize','minimumRangerLevel','expectedNativeWriteCount','nativeWriteLeaf');if(Test-KmcChunk7OwnedProgressionCase $s.case){$properties+=@('ownedSource')};Assert-KmcExactProperties $s $properties 'native progression selection'
    if($s.case-isnot[string]-or$s.case-cnotin(Get-KmcChunk7ProgressionCases)){throw 'Unknown native progression case.'}
    foreach($key in @('sourceRunId','inventoryRunId')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.$key-ceq$Request.runId){throw 'Progression requires distinct completed source runs.'}}
    foreach($key in @('sourceArtifactSha256','sourceArchiveSha256','inventoryArtifactSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Progression source hashes are missing.'}}
    $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($key in @('actorId','petId','controlActorId','controlPetId')){Assert-KmcOrdinaryGuid $s.$key;if(-not$ids.Add($s.$key)){throw 'Progression actor roles conflict.'}}
    if(Test-KmcChunk7OwnedProgressionCase $s.case){Assert-KmcChunk7OwnedProgressionSelection $Request}
    else{if($s.actorId-cne'd17c8fd0-6627-40c4-bf9a-024cf001c1f7'-or$s.controlActorId-cne'b6628a77-4962-47a4-a17c-88d9836fc9d5'-or$s.controlPetId-cne'd79a4f6c-b74e-4868-95bd-533899131acb'){throw 'Progression source actor/control roles differ from qualified Manual300.'}}
    $write=(Get-KmcChunk7ProgressionOperation $s.case)-cne'horse-native-dismiss';$respec=(Get-KmcChunk7ProgressionOperation $s.case)-cin@('horse-native-retrain-horse','horse-native-retrain-mammoth')
    Assert-KmcOrdinaryInteger $s.expectedNativeWriteCount ([int]$write) ([int]$write)
    if($write){Assert-KmcOrdinaryInteger $s.minimumRangerLevel 4 20
        if(Test-KmcChunk7OwnedProgressionCase $s.case){if($null-ne$s.nativeWriteLeaf){throw 'Owned progression Manual number must be observed and reserved natively.'}}
        elseif($s.nativeWriteLeaf-isnot[string]-or$s.nativeWriteLeaf-cne'Auto_1.zks'){throw 'Historical native completion autosave leaf differs.'}}
    else{Assert-KmcOrdinaryInteger $s.minimumRangerLevel 0 0;if($null-ne$s.nativeWriteLeaf){throw 'Native dismissal has no write authority.'}}
    if($respec){
        foreach($key in @('raceGuid','presetGuid')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{32}$'){throw 'Exact observed native respec body pin missing.'}}
        if($s.skeletonName-isnot[string]-or[string]::IsNullOrWhiteSpace($s.skeletonName)-or$s.skeletonName.Length-gt160-or$s.gender-isnot[string]-or$s.gender-cnotin@('Male','Female')-or$s.leftHanded-isnot[bool]-or$s.bodySize-isnot[string]-or$s.bodySize-cne'Medium'){throw 'Respec must preserve the exact declared Medium body.'}
    }else{foreach($key in @('raceGuid','presetGuid','skeletonName','gender','leftHanded','bodySize')){if($null-ne$s.$key){throw 'This operation cannot reconstruct a native body.'}}}
}
function Assert-KmcChunk7ProgressionLoadDescriptor($Request){
    Assert-KmcChunk7ProgressionSelection $Request
    if(-not(Test-KmcChunk7ProgressionScenario $Request.scenario)){throw 'Progression source descriptor belongs to another family.'}
    $d=$Request.persistenceLoad
    Assert-KmcExactProperties $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'progression manual source'
    Assert-KmcProgressionTexts $d @('internalName','fileName','sha256','gameId','gameName','area')
    if(Test-KmcChunk7OwnedProgressionCase $Request.progression.case){Assert-KmcChunk7OwnedProgressionDescriptor $Request;return}
    if($d.internalName-cne'KMC_C7_WORLD'-or$d.fileName-cne'Manual_300_KMC_C7_WORLD.zks'-or$d.sha256-cne$Request.progression.sourceArchiveSha256-or
       $d.sha256-ceq$Request.fixture.baseline.sha256-or$d.sha256-ceq$Request.fixture.working.sha256-or$d.gameId-cne$Request.fixture.working.gameId-or
       $d.gameName-cne$Request.fixture.working.gameName-or$d.area-cne$Request.fixture.working.area){throw 'Exact native Manual300 source differs.'}
    Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1
}
function Assert-KmcProgressionAuthority($Row,[string]$Root,[long]$Generation,[long]$Writes,[bool]$Writable){
    if($Row.root-isnot[string]-or$Row.root-cne$Root){throw 'Progression authority root differs.'}
    Assert-KmcChunk7Boolean $Row.exactScope $true;Assert-KmcOrdinaryInteger $Row.generation 1 ([int]::MaxValue)
    if($Row.generation-ne$Generation){throw 'Progression authority generation changed.'}
    if($Writable){Assert-KmcOrdinaryInteger $Row.maximumNativeWrites 1 1}elseif($null-ne$Row.maximumNativeWrites){throw 'Dismissal acquired write authority.'}
    foreach($key in @('maximumNativeLoads','admittedNativeLoads','authorizedLoads')){Assert-KmcOrdinaryInteger $Row.$key 1 1}
    foreach($key in @('admittedNativeWrites','authorizedWrites')){Assert-KmcOrdinaryInteger $Row.$key $Writes $Writes}
    foreach($key in @('unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites')){Assert-KmcOrdinaryInteger $Row.$key 0 0}
}
function Assert-KmcProgressionColdRestore($Probe,$Archive,$Request){
    Assert-KmcProgressionTexts $Probe @('contract','prepareMetric')
    foreach($key in @('complete','installedBeforeWorldLoad')){Assert-KmcChunk7Boolean $Probe.$key $true}
    Assert-KmcOrdinaryInteger $Probe.maximumEvents 2048 2048
    foreach($key in @('dropped','errors')){Assert-KmcOrdinaryInteger $Probe.$key 0 0}
    if($Probe.contract-cne'chunk7-native-restoration-entry-observer-v1'-or$Probe.prepareMetric-cne'native-method-entries-including-production-refused-attempts'-or
       $Probe.hooks-isnot[Array]-or$Probe.hooks.Count-ne6-or$Probe.events-isnot[Array]-or$Probe.events.Count-gt2048){throw 'Whole-source cold observation incomplete.'}
    foreach($entry in @(@('TurnBased.Controllers.TurnController','Prepare','06000C3C'),@('Kingmaker.Controllers.Combat.UnitCombatState','NewRound','0600939D'))){
        $hooks=@($Probe.hooks|Where-Object{$_.declaringType-ceq$entry[0]-and$_.method-ceq$entry[1]-and$_.token-ceq$entry[2]-and$_.moduleMvid-ceq'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})
        if($hooks.Count-ne1){throw 'Exact installed prepare/round hook missing.'}
    }
    foreach($method in @('RestoreActor','RestoreDebt','TryRestoreArchiveCombat','RestoreArchivePresentation')){
        $h=@($Probe.hooks|Where-Object{$_.method-ceq$method-and$_.moduleMvid-ceq$Request.dllMvid});if($h.Count-ne1){throw 'Cold observer is not pinned to the exact product method.'}
    }
    $actors=@($Archive.Pairs|ForEach-Object{$_.Rider;$_.Mount});$ids=@($actors|ForEach-Object{$_.Id})
    foreach($hook in $Probe.hooks){Assert-KmcProgressionTexts $hook @('declaringType','method','token','moduleMvid')}
    foreach($event in $Probe.events){Assert-KmcProgressionText $event.kind;Assert-KmcOrdinaryInteger $event.frame 0 ([int]::MaxValue);if($event.PSObject.Properties['actorId']){Assert-KmcOrdinaryGuid $event.actorId};if($event.PSObject.Properties['restorePhase']){Assert-KmcProgressionText $event.restorePhase}}
    if(@($Probe.events|Where-Object{$_.PSObject.Properties['actorId']-and$_.actorId-cin$ids-and$_.kind-cin@('prepare-before','round-before','restore-actor-before','restore-actor-after')}).Count){throw 'Exploration restoration replayed preparation, round or combat actor state.'}
    foreach($actor in $actors){
        foreach($kind in @('restore-debt-before','restore-debt-after')){
            $rows=@($Probe.events|Where-Object{$_.kind-ceq$kind-and$_.actorId-ceq$actor.Id});if($rows.Count-ne1){throw 'Cold native actor debt did not restore exactly once.'}
            if($kind-ceq'restore-debt-after'-and($rows[0].restorePhase-cne'validated-before-native-mutation'-or(ConvertTo-KmcChunk7Canonical $rows[0].debt)-cne(ConvertTo-KmcChunk7Canonical $actor))){throw 'Immediate native restored debt differs from validated source.'}
        }
    }
    $present=@($Probe.events|Where-Object kind -CEQ 'all-owner-presentation-restored')
    if($present.Count-ne1-or$present[0].restorePhase-cne'all-pair-presentation-restored'){throw 'Actual complete all-owner presentation transition missing.'}
    $snap=$present[0].snapshot
    Assert-KmcChunk7Boolean $snap.playerInCombat $false
    if($snap.turnBased-isnot[bool]-or$null-ne$snap.combat-or$snap.participations-isnot[Array]-or$snap.participations.Count-ne0-or$snap.owners-isnot[Array]-or$snap.owners.Count-ne2){throw 'Cold world invented combat or lost owners.'}
    foreach($owner in $snap.owners){Assert-KmcProgressionTexts $owner @('ownerId','riderId','mountId','state')}
    foreach($pair in $Archive.Pairs){$owner=@($snap.owners|Where-Object ownerId -CEQ $pair.PairId);if($owner.Count-ne1-or$owner[0].riderId-cne$pair.Rider.Id-or$owner[0].mountId-cne$pair.Mount.Id-or$owner[0].state-cne'Active'){throw 'Cold world replaced an owner.'};Assert-KmcOrdinaryInteger $owner[0].generation $pair.Generation $pair.Generation}
    if((ConvertTo-KmcChunk7Canonical @($snap.nativeControls|Sort-Object ActorId,Index))-cne(ConvertTo-KmcChunk7Canonical @($Archive.Slots|Sort-Object ActorId,Index))){throw 'Native control restoration differs.'}
}
function Assert-KmcProgressionActor($Row,[string]$Id){
    Assert-KmcOrdinaryGuid $Row.actorId;Assert-KmcProgressionText $Row.kind
    if($Row.actorId-cne$Id){throw 'Native progression snapshot replaced its target.'}
    Assert-KmcProgressionObject $Row.object;Assert-KmcOrdinaryInteger $Row.xp 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $Row.level 0 20
    Assert-KmcOrdinaryInteger $Row.money;Assert-KmcOrdinaryInteger $Row.gameTimeTicks 0
    if($null-ne$Row.petId){Assert-KmcOrdinaryGuid $Row.petId;Assert-KmcOrdinaryInteger $Row.petLevel 1 20}elseif($null-ne$Row.petLevel){throw 'Absent pet has an invented native level.'}
}
function Assert-KmcProgressionRespec($Probe,$Selection,$Before){
    foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $Probe.$key $true}
    foreach($key in @('nativeRespecEntries','nativeRespecExits','nativePreviewEntries','nativeFinishEntries','nativeFinishExits','nativeAdvanceCalls','nativeRebinds')){Assert-KmcOrdinaryInteger $Probe.$key 1 1}
    foreach($key in @('diagnosticMoneyWrites','diagnosticXpWrites','diagnosticRestCalls','diagnosticTimeAdvances')){Assert-KmcOrdinaryInteger $Probe.$key 0 0}
    if($Probe.errors-isnot[Array]-or$Probe.errors.Count-ne0-or$Probe.events-isnot[Array]-or$Probe.events.Count-gt192-or$Probe.hooks-isnot[Array]-or$Probe.hooks.Count-ne7){throw 'Native retrain observer incomplete.'}
    Assert-KmcOrdinaryInteger $Probe.nativePetUpdates 1 32;Assert-KmcChunk7Boolean $Probe.petUpdatePending $false
    Assert-KmcOrdinaryGuid $Probe.originalPetId;Assert-KmcOrdinaryGuid $Probe.reboundPetId;Assert-KmcChunk7Boolean $Probe.originalPetRemoved $true
    if($Probe.originalPetId-cne$Before.petId-or$Probe.reboundPetId-cin@($Selection.actorId,$Selection.petId,$Selection.controlActorId,$Selection.controlPetId)){throw 'Retrain did not prove an exact new sourced pet and original removal.'}
    Assert-KmcOrdinaryInteger $Probe.initialMoney $Before.money $Before.money;Assert-KmcOrdinaryInteger $Probe.nativeCost 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $Probe.nativeScrollRefund 0 ([int]::MaxValue)
    if($Probe.initialMoney-lt$Probe.nativeCost){throw 'Native retrain was not affordable before action.'}
    $contracts=@(@('Kingmaker.Player','RespecCompanion','06000DE7'),@('Kingmaker.UI.LevelUp.CharacterBuildController','HandleLevelUpStart','060039BC'),@('Kingmaker.Designers.EventConditionActionSystem.Actions.RespecCompanion','FinishRespecialization','06008ED5'),@('Kingmaker.Game','AdvanceGameTime','06000CC6'),@('Kingmaker.Controllers.Rest.RestController','ApplyRest','060091EC'),@('Kingmaker.UI.LevelUp.CharacterBuildController','HandleUnitChangedAfterRespec','060039D7'),@('Kingmaker.UnitLogic.FactLogic.AddPet','TryUpdatePet','06002510'))
    for($i=0;$i-lt$contracts.Count;$i++){$h=$Probe.hooks[$i];Assert-KmcProgressionTexts $h @('type','method','token','moduleMvid');if($h.type-cne$contracts[$i][0]-or$h.method-cne$contracts[$i][1]-or$h.token-cne$contracts[$i][2]-or$h.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Native retrain hook contract differs.'}}
    $r=@{};$frame=-1;$allowed=@('native-respec-enter','native-respec-preview','native-respec-ui-open','native-respec-finish-enter','native-respec-time-advance','native-respec-rest-before','native-respec-rest-after','native-respec-finish-exit','native-respec-original-rebound','native-respec-pet-update')
    foreach($e in $Probe.events){
        Assert-KmcProgressionText $e.kind;Assert-KmcOrdinaryGuid $e.actorId
        if($e.kind-cnotin$allowed-or$e.actorId-cne$Selection.actorId){throw 'Unknown or foreign native retrain event.'}
        Assert-KmcProgressionObject $e.actorObject;Assert-KmcOrdinaryInteger $e.frame 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $e.gameTicks 0
        if($e.actorObject-ne$Before.object-or$e.frame-lt$frame){throw 'Native retrain actor/frame binding changed.'};$frame=$e.frame
        if($e.kind-cnotin@('native-respec-rest-before','native-respec-rest-after','native-respec-pet-update')){if($r.ContainsKey($e.kind)){throw 'Repeated native retrain boundary.'};$r[$e.kind]=$e}
    }
    foreach($kind in $allowed|Where-Object{$_-cnotin@('native-respec-rest-before','native-respec-rest-after','native-respec-pet-update')}){if(-not$r.ContainsKey($kind)){throw 'Native retrain boundary absent.'}}
    $start=$r['native-respec-enter'];$preview=$r['native-respec-preview'];$finish=$r['native-respec-finish-enter'];$end=$r['native-respec-finish-exit'];$rebound=$r['native-respec-original-rebound']
    Assert-KmcOrdinaryInteger $start.xp $Before.xp $Before.xp;Assert-KmcOrdinaryInteger $start.money $Before.money $Before.money
    Assert-KmcOrdinaryInteger $start.cost $Probe.nativeCost $Probe.nativeCost;Assert-KmcOrdinaryInteger $start.scrollRefund $Probe.nativeScrollRefund $Probe.nativeScrollRefund;Assert-KmcOrdinaryInteger $start.uses 0 ([int]::MaxValue)
    foreach($row in @($start,$rebound)){Assert-KmcProgressionObject $row.descriptorObject;Assert-KmcProgressionObject $row.viewObject}
    Assert-KmcProgressionObject $preview.previewObject;Assert-KmcProgressionObject $preview.descriptorObject
    Assert-KmcOrdinaryGuid $start.petId;if($start.petId-cne$Before.petId){throw 'Native retrain entry has another original pet.'}
    if($preview.previewObject-eq$Before.object-or$preview.descriptorObject-eq$start.descriptorObject-or$preview.nativeCallbackToken-isnot[string]-or$preview.nativeCallbackToken-cnotmatch'^060[0-9A-F]{5}$'-or
       $preview.frame-ne$start.frame-or$r['native-respec-ui-open'].frame-ne$start.frame-or$finish.frame-lt$start.frame-or$rebound.frame-ne$finish.frame-or$end.frame-ne$finish.frame){throw 'Native vacuum preview/rebind does not follow exact native callbacks.'}
    Assert-KmcOrdinaryInteger $finish.money ($Before.money+$Probe.nativeScrollRefund) ($Before.money+$Probe.nativeScrollRefund)
    Assert-KmcOrdinaryInteger $end.money ($Before.money+$Probe.nativeScrollRefund-$Probe.nativeCost) ($Before.money+$Probe.nativeScrollRefund-$Probe.nativeCost)
    Assert-KmcOrdinaryInteger $end.uses ($start.uses+1) ($start.uses+1);Assert-KmcOrdinaryInteger $end.nativeElapsedTicks 864000000000 864000000000
    Assert-KmcOrdinaryInteger $r['native-respec-time-advance'].deltaTicks 864000000000 864000000000
    if($r['native-respec-time-advance'].frame-ne$finish.frame-or$end.gameTicks-$finish.gameTicks-ne864000000000){throw 'Native retrain time differs from one observed native day.'}
    if($finish.nativeRestTargets-isnot[Array]){throw 'Native retrain rest roster absent.'};Assert-KmcOrdinaryInteger $Probe.expectedRestCount 1 24
    foreach($key in @('observedRestEntries','observedRestExits')){Assert-KmcOrdinaryInteger $Probe.$key $Probe.expectedRestCount $Probe.expectedRestCount}
    if($finish.nativeRestTargets.Count-ne$Probe.expectedRestCount){throw 'Native rest target count differs.'}
    $targets=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($target in $finish.nativeRestTargets){
        Assert-KmcOrdinaryGuid $target.id;Assert-KmcProgressionObject $target.descriptorObject
        if(-not$targets.Add($target.id)){throw 'Native rest roster duplicates an actor.'}
        foreach($kind in @('native-respec-rest-before','native-respec-rest-after')){
            $rows=@($Probe.events|Where-Object{$_.kind-ceq$kind-and$_.id-ceq$target.id});if($rows.Count-ne1-or$rows[0].frame-ne$finish.frame){throw 'Native rest was missing, repeated or outside the native finish.'}
            Assert-KmcProgressionObject $rows[0].descriptorObject;if($rows[0].descriptorObject-ne$target.descriptorObject){throw 'Native rest descriptor changed.'}
            foreach($resource in @($target,$rows[0])){
                foreach($key in @('move','standard','swift','initiative')){if(-not(Test-KmcChunk7FiniteNumber $resource.$key)){throw 'Native rest resource observation is not finite.'}}
                if($resource.prepared-isnot[bool]){throw 'Native rest preparation observation is not Boolean.'}
                foreach($key in @('reactions','reactionAllowance')){Assert-KmcOrdinaryInteger $resource.$key 0 1024}
            }
        }
    }
    if(@($Probe.events|Where-Object kind -CEQ 'native-respec-rest-before').Count-ne$targets.Count-or@($Probe.events|Where-Object kind -CEQ 'native-respec-rest-after').Count-ne$targets.Count){throw 'Native rest included undeclared actors.'}
    Assert-KmcOrdinaryGuid $rebound.oldPetId;Assert-KmcChunk7Boolean $rebound.oldPetRemoved $true
    if($rebound.oldPetId-cne$Before.petId-or$null-ne$rebound.oldPetMasterId-or$rebound.oldPetShouldBeDestroyed-isnot[bool]-or$rebound.oldPetDestroyed-isnot[bool]-or
       -not($rebound.oldPetShouldBeDestroyed-or$rebound.oldPetDestroyed)){throw 'Native rebind omitted exact detached old-pet destruction.'}
    Assert-KmcOrdinaryInteger $rebound.level 1 1
    $newPet=$null;$newObject=0;$newDescriptor=0;$newView=0;$updates=0;$hasRebound=$false
    foreach($e in $Probe.events){
        if($e.kind-ceq'native-respec-original-rebound'){
            $hasRebound=$true
            if($null-eq$e.petId){foreach($key in @('petObject','petDescriptorObject','petViewObject')){Assert-KmcOrdinaryInteger $e.$key 0 0}}
            else{Assert-KmcOrdinaryGuid $e.petId;foreach($key in @('petObject','petDescriptorObject','petViewObject')){Assert-KmcProgressionObject $e.$key};if($e.petId-cne$newPet-or$e.petObject-ne$newObject-or$e.petDescriptorObject-ne$newDescriptor-or$e.petViewObject-ne$newView){throw 'Rebind pet lacks an earlier exact native source update.'}}
        }
        if($e.kind-cne'native-respec-pet-update'){continue}
        $updates++;Assert-KmcProgressionObject $e.sourceObject;Assert-KmcProgressionObject $e.ownerDescriptorObject;Assert-KmcChunk7Boolean $e.afterRebind $hasRebound
        if($e.ownerDescriptorObject-ne$rebound.descriptorObject){throw 'Native pet source belongs to another descriptor.'}
        if($null-eq$e.petId){foreach($key in @('petObject','petDescriptorObject','petViewObject')){Assert-KmcOrdinaryInteger $e.$key 0 0};continue}
        Assert-KmcOrdinaryGuid $e.petId;foreach($key in @('petObject','petDescriptorObject','petViewObject')){Assert-KmcProgressionObject $e.$key}
        if($e.petId-cne$Probe.reboundPetId-or($null-ne$newPet-and($e.petId-cne$newPet-or$e.petObject-ne$newObject-or$e.petDescriptorObject-ne$newDescriptor-or$e.petViewObject-ne$newView))){throw 'Observed native pet identity/descriptor/view was replaced.'}
        $newPet=$e.petId;$newObject=$e.petObject;$newDescriptor=$e.petDescriptorObject;$newView=$e.petViewObject
    }
    if($updates-ne$Probe.nativePetUpdates-or$null-eq$newPet){throw 'Terminal companion has no exact observed native AddPet result.'}
    [pscustomobject]@{preview=$preview;rebound=$rebound;moneyAfter=$end.money;petId=$newPet;petObject=$newObject}
}
function Assert-KmcProgressionChoices($Rows,$Request,$Before,$Respec){
    $s=$Request.progression;$retrain=$null-ne$Respec
    if($Rows-isnot[Array]-or$Rows.Count-lt4-or$Rows.Count-gt1500){throw 'Native progression choices are missing or oversized.'}
    $identity=$Rows[0];$initial=$identity
    if($identity.event-cne$(if($retrain){'native-respec-build-admission'}else{'native-existing-horse-level-admission'})){throw 'Native progression readiness admission missing.'}
    $xp=$Before.xp;$level=if($retrain){0}else{$Before.level};$money=$Before.money
    Assert-KmcProgressionSnapshot $identity $identity $xp $level $money
    if($identity.actorId-cne$s.actorId){throw 'Native choice admission selected another actor.'}
    Assert-KmcChunk7Boolean $identity.nativeCanLevelUp $true
    if($retrain){if($identity.actorObject-ne$Respec.preview.previewObject-or$identity.descriptorObject-ne$Respec.preview.descriptorObject-or$identity.nativeMode-cne'Respec'){throw 'Native choice preview is not the observed respec preview.'}}
    elseif($identity.actorObject-ne$Before.object-or$identity.petId-cne$s.petId){throw 'Native existing Horse readiness identity differs.'}
    $accepted=0;$commits=0;$pendingChoice=$null;$pendingCommit=$null;$finished=$false;$rebound=$false;$lastFrame=-1;$lastTime=-1;$horse=$false;$bond=$false
    foreach($row in $Rows){
        Assert-KmcProgressionTexts $row @('event','runId')
        Assert-KmcOrdinaryInteger $row.frame 0 ([int]::MaxValue)
        if($row.runId-cne$Request.runId-or$row.frame-lt$lastFrame-or-not(Test-KmcChunk7FiniteNumber $row.elapsedSeconds)-or$row.elapsedSeconds-lt$lastTime-or$row.elapsedSeconds-gt720){throw 'Native progression choice ordering differs.'}
        $lastFrame=$row.frame;$lastTime=$row.elapsedSeconds
        if([object]::ReferenceEquals($row,$initial)){continue};if($finished){throw 'Native progression choices continued after completion.'}
        switch -CaseSensitive ($row.event){
            'native-level-open' {if($null-ne$pendingChoice-or$null-ne$pendingCommit){throw 'Overlapping native builder level.'};Assert-KmcProgressionSnapshot $row $identity $xp $level $money;Assert-KmcChunk7Boolean $row.nativeCanLevelUp $true;Assert-KmcOrdinaryInteger $row.nextLevel ($level+1) ($level+1)}
            'native-choice-requested' {
                Assert-KmcProgressionText $row.kind
                if($null-ne$pendingChoice-or$null-ne$pendingCommit-or$row.kind-cnotin@('portrait','race','gender','native-race-preset','native-handedness','class','attribute-purchase','racial-attribute','level-attribute','feature','skill','name','alignment','voice','native-total-phase')-or$row.choice-isnot[string]-or[string]::IsNullOrEmpty($row.choice)){throw 'Invalid or overlapping native choice.'}
                Assert-KmcProgressionSnapshot $row.before $identity $xp $level $money;$pendingChoice=$row
            }
            'native-choice' {
                Assert-KmcProgressionTexts $row @('kind','choice')
                if($null-ne$row.nativeSelectionGuid){Assert-KmcProgressionText $row.nativeSelectionGuid}
                if($null-eq$pendingChoice-or$row.kind-cne$pendingChoice.kind-or$row.choice-cne$pendingChoice.choice-or$row.nativeSelectionGuid-cne$pendingChoice.nativeSelectionGuid-or(ConvertTo-KmcChunk7Canonical $row.before)-cne(ConvertTo-KmcChunk7Canonical $pendingChoice.before)){throw 'Native choice lost exact request binding.'}
                Assert-KmcChunk7Boolean $row.nativeChoiceAccepted $true;Assert-KmcProgressionSnapshot $row.before $identity $xp $level $money;Assert-KmcProgressionSnapshot $row.after $identity $xp $level $money
                if($row.kind-ceq'class'-and$row.choice-cne'cda0615668a6df14eb36ba19ee881af6'){throw 'Native progression class changed.'}
                if($row.kind-ceq'feature'){
                    if($row.nativeSelectionGuid-isnot[string]-or$row.nativeSelectionGuid-cnotmatch'^[a-f0-9]{32}$'){throw 'Native feature selection identity absent.'}
                    if($row.nativeSelectionGuid-ceq'b705c5184a96a84428eeb35ae2517a14'-and$row.choice.StartsWith('ee63330662126374e8785cc901941ac7/',[StringComparison]::Ordinal)){$bond=$true}
                    $expected=if((Get-KmcChunk7ProgressionOperation $s.case)-ceq'horse-native-retrain-mammoth'){'6adc3aab7cde56b40aa189a797254271/'}else{'7db7c50677e39f09feef56f3831fc723/'}
                    if($row.nativeSelectionGuid-ceq'ee63330662126374e8785cc901941ac7'-and$row.choice.StartsWith($expected,[StringComparison]::Ordinal)){$horse=$true}
                }elseif($null-ne$row.nativeSelectionGuid){throw 'Nonfeature choice invented selection identity.'}
                if($retrain){foreach($pin in @(@('race','raceGuid'),@('native-race-preset','presetGuid'),@('gender','gender'))){if($row.kind-ceq$pin[0]-and$row.choice-cne$s.($pin[1])){throw 'Retrain body choice differs from observed native pins.'}}}
                $pendingChoice=$null;$accepted++
            }
            'native-total-next-requested' {
                if($null-ne$pendingChoice-or$null-ne$pendingCommit){throw 'Total overlaps an unfinished choice.'};Assert-KmcProgressionSnapshot $row $identity $xp $level $money
                Assert-KmcChunk7Boolean $row.nativeCanLevelUp $true;Assert-KmcChunk7Boolean $row.nativeIsComplete $true
                if($row.phase-cne'Total'-or$row.selectedClass-cne'cda0615668a6df14eb36ba19ee881af6'){throw 'Native Total/selected class gate skipped.'}
                foreach($key in @('attributePoints','distributionPoints','skillRemaining','pendingFeatures','pendingSpellGroups')){Assert-KmcOrdinaryInteger $row.$key 0 0}
                Assert-KmcOrdinaryInteger $row.skillTotal 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $row.skillSpent $row.skillTotal $row.skillTotal
                Assert-KmcOrdinaryInteger $row.nextLevel ($level+1) ($level+1);$pendingCommit=$row
            }
            'native-respec-builder-rebound' {
                if(-not$retrain-or$rebound-or$commits-ne0-or$null-eq$pendingCommit-or$row.actorObject-ne$Before.object-or$row.descriptorObject-ne$Respec.rebound.descriptorObject){throw 'Native preview adoption is unobserved or repeated.'}
                Assert-KmcProgressionSnapshot $row $row $xp 1 $Respec.moneyAfter;Assert-KmcChunk7Boolean $row.nativeRespecRebound $true;$identity=$row;$rebound=$true
            }
            'native-level-committed' {
                if($null-eq$pendingCommit-or(ConvertTo-KmcChunk7Canonical $row.before)-cne(ConvertTo-KmcChunk7Canonical $pendingCommit)-or($retrain-and-not$rebound)){throw 'Native level commit lacks its exact Total/observed rebind.'}
                $commits++;$level++;$money=if($retrain){$Respec.moneyAfter}else{$Before.money}
                Assert-KmcOrdinaryInteger $row.commitCount $commits $commits;Assert-KmcOrdinaryInteger $row.moneyExpected $money $money
                Assert-KmcProgressionSnapshot $row.after $identity $xp $level $money;$pendingCommit=$null
            }
            'native-builder-complete' {
                if($null-ne$pendingChoice-or$null-ne$pendingCommit-or$commits-lt1){throw 'Native builder completed before choices settled.'}
                Assert-KmcProgressionSnapshot $row.actor $identity $xp $level $money;Assert-KmcChunk7Boolean $row.actor.nativeCanLevelUp $false;Assert-KmcChunk7Boolean $row.ordinaryChoicesComplete $true
                foreach($key in @('nativeFieldAdmissionProven','nativeSaveSettlementProven','poseQualified')){Assert-KmcChunk7Boolean $row.$key $false}
                Assert-KmcOrdinaryInteger $row.actor.rangerLevel $s.minimumRangerLevel 20;Assert-KmcOrdinaryGuid $row.actor.petId
                $pet=if((Get-KmcChunk7ProgressionOperation $s.case)-ceq'horse-native-retrain-mammoth'){'e7aa96d15a45238438ae4cfb476f6bb9'}else{'4016c7db400ab721ff125aef9e65e202'}
                if($row.actor.petBlueprint-isnot[string]-or$row.actor.petBlueprint-cne$pet){throw 'Native selected companion blueprint differs.'}
                Assert-KmcProgressionObject $row.actor.petObject;Assert-KmcOrdinaryInteger $row.actor.petLevel 1 20;Assert-KmcOrdinaryInteger $row.actor.petExperience 0 ([int]::MaxValue)
                if($retrain-and($row.actor.petId-cne$Respec.petId-or$row.actor.petObject-ne$Respec.petObject)){throw 'Terminal rebuilt companion differs from its actual native AddPet source.'}
                if(-not$retrain){Assert-KmcChunk7Boolean $row.actor.originalHorseRetained $true;if($row.actor.petId-cne$s.petId-or$row.actor.petLevel-le$Before.petLevel){throw 'Existing native Horse did not advance without replacement.'}}
                $finished=$true;$terminal=$row.actor
            }
            default {throw 'Unknown native progression choice event.'}
        }
    }
    if(-not$finished-or$accepted-lt1-or$accepted-gt512-or$commits-gt20-or($retrain-and(-not$horse-or-not$bond))){throw 'Native progression choices/companion chain incomplete.'}
    [pscustomobject]@{commits=$commits;terminal=$terminal;initialLevel=$(if($retrain){0}else{$Before.level})}
}
function Assert-KmcProgressionCompletion($Probe,$Before,[long]$Commits,[long]$InitialLevel){
    foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $Probe.$key $true}
    if($Probe.errors-isnot[Array]-or$Probe.errors.Count-ne0-or$Probe.hooks-isnot[Array]-or$Probe.hooks.Count-ne3-or$Probe.events-isnot[Array]-or$Probe.events.Count-ne(2*$Commits+2)){throw 'Native progression completion observer incomplete.'}
    $contracts=@(@('Kingmaker.Controllers.LevelUpAutoSaveController','HandleLevelUpComplete','0600906B'),@('Kingmaker.Game','SaveGame','06000CE4'),@('Kingmaker.EntitySystem.Persistence.SaveManager','PrepareSave','06008025'))
    for($i=0;$i-lt3;$i++){$h=$Probe.hooks[$i];Assert-KmcProgressionTexts $h @('type','method','token','moduleMvid');if($h.type-cne$contracts[$i][0]-or$h.method-cne$contracts[$i][1]-or$h.token-cne$contracts[$i][2]-or$h.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Exact native completion hook differs.'}}
    foreach($key in @('completionEntries','completionExits')){Assert-KmcOrdinaryInteger $Probe.$key $Commits $Commits}
    foreach($key in @('saveRequests','preparedWrites')){Assert-KmcOrdinaryInteger $Probe.$key 1 1}
    $inside=$null;$entries=0;$exits=0;$requests=0;$writes=0;$saveObject=0;$frame=-1
    foreach($row in $Probe.events){
        Assert-KmcProgressionText $row.kind;Assert-KmcOrdinaryGuid $row.actorId;if($row.kind-cne'native-level-autosave-prepared'){Assert-KmcOrdinaryGuid $row.completionActorId}
        Assert-KmcOrdinaryInteger $row.frame 0 ([int]::MaxValue);Assert-KmcProgressionObject $row.actorObject
        if($row.actorId-cne$Before.actorId-or$row.actorObject-ne$Before.object-or$row.frame-lt$frame){throw 'Native completion actor/frame differs.'};$frame=$row.frame
        switch -CaseSensitive ($row.kind){
            'native-level-completion-enter' {
                if($null-ne$inside){throw 'Overlapping native completion.'};$entries++;$inside=$row
                Assert-KmcChunk7Boolean $row.insideObservedCompletion $true;Assert-KmcChunk7Boolean $row.isChargen ($entries-lt$Commits)
                Assert-KmcOrdinaryInteger $row.characterLevel ($InitialLevel+$entries) ($InitialLevel+$entries)
            }
            'native-level-autosave-request' {
                if($null-eq$inside-or$entries-ne$Commits-or$row.completionActorId-cne$Before.actorId-or$row.frame-ne$inside.frame){throw 'Autosave is outside the exact terminal native completion.'}
                foreach($key in @('insideObservedCompletion','inState','autoLevelup')){Assert-KmcChunk7Boolean $row.$key $true}
                Assert-KmcChunk7Boolean $row.isChargen $false;Assert-KmcProgressionObject $row.saveObject
                if($row.saveType-isnot[string]-or$row.saveType-cne'Auto'){throw 'Native completion requested another save category.'};$requests++;$saveObject=$row.saveObject
            }
            'native-level-autosave-prepared' {
                Assert-KmcProgressionObject $row.saveObject;Assert-KmcChunk7Boolean $row.autoLevelup $true
                Assert-KmcProgressionTexts $row @('saveLeaf','saveType');if($requests-ne1-or$row.saveObject-eq$saveObject-or$row.saveLeaf-cne'Auto_1.zks'-or$row.saveType-cne'Auto'){throw 'Native PrepareSave lost its exact admitted save.'};$writes++
            }
            'native-level-completion-exit' {
                if($null-eq$inside-or$row.frame-ne$inside.frame-or$row.completionActorId-cne$Before.actorId){throw 'Native completion exit lost callback ownership.'}
                Assert-KmcChunk7Boolean $row.insideObservedCompletion $true;Assert-KmcChunk7Boolean $row.isChargen $inside.isChargen;$inside=$null;$exits++
            }
            default {throw 'Unknown native progression completion event.'}
        }
    }
    if($null-ne$inside-or$entries-ne$Commits-or$exits-ne$Commits-or$requests-ne1-or$writes-ne1){throw 'Native completion/one-write boundary did not settle exactly.'}
}
function Assert-KmcProgressionDismissal($Ui,$Before){
    Assert-KmcChunk7Boolean $Ui.respec $false;Assert-KmcChunk7Boolean $Ui.confirmationSent $true;Assert-KmcChunk7Boolean $Ui.selectorChoiceSent $false
    if($null-ne$Ui.nativeRespec){throw 'Dismissal acquired a retrain observer.'}
    $probe=$Ui.nativeDismissal
    foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $probe.$key $true}
    foreach($key in @('nativeEntries','nativeExits')){Assert-KmcOrdinaryInteger $probe.$key 1 1}
    if($probe.errors-isnot[Array]-or$probe.errors.Count-ne0-or$probe.events-isnot[Array]-or$probe.events.Count-ne2){throw 'Exact native dismissal observer incomplete.'}
    $player=$null;$frame=-1
    for($i=0;$i-lt2;$i++){
        $row=$probe.events[$i];Assert-KmcProgressionObject $row.actorObject;Assert-KmcProgressionObject $row.playerObject;Assert-KmcOrdinaryInteger $row.frame 0 ([int]::MaxValue)
        Assert-KmcProgressionText $row.kind;Assert-KmcOrdinaryGuid $row.actorId
        if($row.kind-cne@('native-dismiss-enter','native-dismiss-exit')[$i]-or$row.actorId-cne$Before.actorId-or$row.actorObject-ne$Before.object){throw 'Native dismissal replaced its target or boundary.'}
        foreach($key in @('isInState','isInGame','destroyed')){if($row.$key-isnot[bool]){throw 'Dismissal state must be native Boolean.'}}
        Assert-KmcChunk7Boolean $row.insideExpectedNativeConfirmation $true
        if($i-eq0){$player=$row.playerObject;$frame=$row.frame;Assert-KmcChunk7Boolean $row.destroyed $false}
        elseif($player-ne$row.playerObject-or$frame-ne$row.frame){throw 'Native dismissal escaped its one confirmation callback.'}
    }
}
function Assert-KmcProgressionUi($Ui,$Selection,$Before){
    foreach($key in @('groupOwned','selectorOwned','dialogOwned')){Assert-KmcChunk7Boolean $Ui.$key $false}
    Assert-KmcOrdinaryInteger $Ui.stage 3 3
    $respec=(Get-KmcChunk7ProgressionOperation $Selection.case)-cne'horse-native-dismiss';Assert-KmcChunk7Boolean $Ui.respec $respec
    $expected=if($respec){@('native-respec-selector-opened','native-respec-target-selected','native-respec-preview-opened')}else{@('native-dismiss-selector-opened','native-dismiss-confirmation-opened','native-dismiss-confirmed')}
    if($Ui.events-isnot[Array]-or$Ui.events.Count-ne3){throw 'Native progression UI is incomplete or repeated.'};$frame=-1
    for($i=0;$i-lt3;$i++){$e=$Ui.events[$i];Assert-KmcProgressionText $e.kind;Assert-KmcOrdinaryGuid $e.actorId;Assert-KmcOrdinaryInteger $e.frame 0 ([int]::MaxValue);if($e.kind-cne$expected[$i]-or$e.actorId-cne$Selection.actorId-or$e.frame-lt$frame){throw 'Native progression UI action order/target differs.'};$frame=$e.frame}
    if($respec){Assert-KmcChunk7Boolean $Ui.selectorChoiceSent $true;Assert-KmcChunk7Boolean $Ui.confirmationSent $false;if($null-ne$Ui.nativeDismissal){throw 'Retrain acquired a dismissal observer.'};return Assert-KmcProgressionRespec $Ui.nativeRespec $Selection $Before}
    Assert-KmcProgressionDismissal $Ui $Before
}
function Assert-KmcProgressionPairCustody($Custody,$Archive,$Selection){
    foreach($key in @('ownerId','riderId','mountId')){Assert-KmcOrdinaryGuid $Custody.$key}
    $target=@($Archive.Pairs|Where-Object{$_.Rider.Id-ceq$Selection.actorId})[0];$control=@($Archive.Pairs|Where-Object{$_.Rider.Id-ceq$Selection.controlActorId})[0]
    if($Custody.ownerId-cne([Guid]$target.PairId).ToString('D')-or$Custody.riderId-cne$Selection.actorId-or$Custody.mountId-cne$Selection.petId){throw 'Progression custody lost the original owner.'}
    Assert-KmcOrdinaryInteger $Custody.generation $target.Generation $target.Generation;Assert-KmcChunk7Boolean $Custody.targetSettled $true
    foreach($key in @('manualActorAllocationWrites','manualPreparationCalls','manualCompanionRemovals')){Assert-KmcOrdinaryInteger $Custody.$key 0 0}
    if($Custody.cleanupErrors-isnot[Array]-or$Custody.cleanupErrors.Count-ne0-or$Custody.observations-isnot[Array]-or$Custody.observations.Count-lt2-or$Custody.observations.Count-gt96){throw 'Progression custody proof incomplete.'}
    $labels=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal);$controlRider=$null;$controlMount=$null
    foreach($row in $Custody.observations){
        Assert-KmcProgressionTexts $row @('label','ownerState','relationshipState')
        if($row.label-cnotin@('before-native-progression','after-native-progression','native-target-cleanup-settled','native-terminal-save-settled','original-progression-pair-settled')-or-not$labels.Add($row.label)){throw 'Unknown/repeated progression custody observation.'}
        foreach($key in @('originalOwnerPresent','retainsObligations','saveReady','teardownReady','noPreparedPairResidue','nativeCommandsSettled','nativeControlsSettled')){if($row.$key-isnot[bool]){throw 'Native owner custody flags must be Boolean.'}}
        Assert-KmcOrdinaryInteger $row.currentGeneration 0 ([long]::MaxValue)
        if($row.unrelatedOwners-isnot[Array]-or$row.unrelatedOwners.Count-ne1){throw 'Independent control owner missing.'};$c=$row.unrelatedOwners[0]
        Assert-KmcOrdinaryGuid $c.id;Assert-KmcProgressionTexts $c @('state','relationshipState')
        if($c.id-cne([Guid]$control.PairId).ToString('D')-or$c.state-cne'Active'-or$c.relationshipState-cne'Mounted'){throw 'Progression changed the independent mounted pair.'}
        Assert-KmcOrdinaryInteger $c.generation $control.Generation $control.Generation;Assert-KmcProgressionObject $c.riderObject;Assert-KmcProgressionObject $c.mountObject
        if($null-eq$controlRider){$controlRider=$c.riderObject;$controlMount=$c.mountObject}elseif($controlRider-ne$c.riderObject-or$controlMount-ne$c.mountObject){throw 'Control pair native objects changed.'}
        if($row.label-ceq'before-native-progression'){
            if($row.ownerState-cne'Active'-or$row.relationshipState-cne'Mounted'){throw 'Target was not natively mounted before progression.'};Assert-KmcChunk7Boolean $row.originalOwnerPresent $true;Assert-KmcOrdinaryInteger $row.currentGeneration $target.Generation $target.Generation
        }
        if($row.label-cin@('native-target-cleanup-settled','original-progression-pair-settled')){
            Assert-KmcChunk7Boolean $row.originalOwnerPresent $false;Assert-KmcChunk7Boolean $row.retainsObligations $false
            foreach($key in @('teardownReady','noPreparedPairResidue','nativeCommandsSettled','nativeControlsSettled')){Assert-KmcChunk7Boolean $row.$key $true}
            if($row.relationshipState-cne'Unmounted'){throw 'Original mounted pair did not finish cleanup.'}
        }
    }
    foreach($required in @('before-native-progression','original-progression-pair-settled')){if(-not$labels.Contains($required)){throw 'Progression missed a required original pair boundary.'}}
    if((Get-KmcChunk7ProgressionOperation $Selection.case)-cne'horse-native-level'-and-not$labels.Contains('native-target-cleanup-settled')){throw 'Native dismissal/retrain did not trigger complete original owner cleanup.'}
    if((Get-KmcChunk7ProgressionOperation $Selection.case)-cne'horse-native-dismiss'-and-not$labels.Contains('native-terminal-save-settled')){throw 'Native terminal save did not retain control custody.'}
}
function Assert-KmcProgressionManualProof($Session,$Request){
    $p=$Session.manualSave;$w=$Session.nativeWrite
    if($null-ne$Session.nativeCompletion-or$null-eq$p-or$null-eq$w){throw 'Owned progression Manual cannot claim an Auto completion or omit its save proof.'}
    Assert-KmcBootstrapInventoryString $p.contract 'manual-seed-native-save-v1'
    foreach($key in @('installed','installationAttempted','complete')){Assert-KmcChunk7Boolean $p.$key $true}
    foreach($key in @('requests','prepares','starts','stages','completions','callbacks')){Assert-KmcOrdinaryInteger $p.$key 1 1}
    if($p.errors-isnot[Array]-or$p.errors.Count-ne0-or$p.events-isnot[Array]-or$p.events.Count-ne6-or$p.hooks-isnot[Array]-or$p.hooks.Count-ne3){throw 'Owned progression Manual worker proof is incomplete.'}
    $methods=@('SaveRoutine','PrepareSave','RecordCompletedSave');$tokens=@('06008029','06008025')
    for($i=0;$i-lt3;$i++){$h=$p.hooks[$i];Assert-KmcBootstrapInventoryString $h.method $methods[$i]
        if($i-lt2){Assert-KmcBootstrapInventoryString $h.type 'Kingmaker.EntitySystem.Persistence.SaveManager';Assert-KmcBootstrapInventoryString $h.token $tokens[$i];Assert-KmcBootstrapInventoryString $h.moduleMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}
        else{Assert-KmcBootstrapInventoryString $h.type 'KingmakerMountedCombat.Integration.MountedPersistenceService';Assert-KmcBootstrapInventoryString $h.moduleMvid $Request.dllMvid}}
    Assert-KmcOrdinaryInteger $w.nativeManualNumber 1 ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $w.snapshotBefore 0 ([int]::MaxValue)
    $leaf='Manual_'+([int]$w.nativeManualNumber).ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_PROGRESSION.zks'
    Assert-KmcBootstrapInventoryString $w.leaf $leaf;Assert-KmcBootstrapInventoryString $w.name 'KMC_C7_PROGRESSION'
    Assert-KmcProgressionObject $w.requestedObject;Assert-KmcProgressionObject $w.writtenObject
    if($w.requestedObject-eq$w.writtenObject){throw 'Native Manual request and prepared descriptor alias.'}
    Assert-KmcOrdinaryInteger $w.manualCallbackCount 1 1
    $kinds=@('native-save-request','native-save-prepared','snapshot-starting','snapshot-staged','native-worker-completed','native-save-callback')
    $last=-1
    for($i=0;$i-lt6;$i++){$e=$p.events[$i];Assert-KmcBootstrapInventoryString $e.kind $kinds[$i];Assert-KmcOrdinaryInteger $e.sequence ($i+1) ($i+1)
        Assert-KmcOrdinaryInteger $e.frame $last ([int]::MaxValue);$last=$e.frame
        Assert-KmcOrdinaryInteger $e.requested $w.requestedObject $w.requestedObject
        $descriptor=if($i-eq0){$w.requestedObject}else{$w.writtenObject};Assert-KmcOrdinaryInteger $e.descriptor $descriptor $descriptor
        Assert-KmcBootstrapInventoryString $e.internalName 'KMC_C7_PROGRESSION';Assert-KmcBootstrapInventoryString $e.type 'Manual'
        if($i-gt0){Assert-KmcBootstrapInventoryString $e.fileName $leaf;Assert-KmcBootstrapInventoryString $e.path $w.path}
        $snap=if($i-lt3){0}else{1};$complete=if($i-lt5){0}else{1}
        Assert-KmcOrdinaryInteger $e.snapshotCount ($w.snapshotBefore+$snap) ($w.snapshotBefore+$snap)
        Assert-KmcOrdinaryInteger $e.completedSaveCount ($w.completedWritesBefore+$complete) ($w.completedWritesBefore+$complete)
    }
    $worker=$p.events[4];Assert-KmcBootstrapInventoryString $worker.iteratorType 'Kingmaker.EntitySystem.Persistence.SaveManager+<SaveRoutine>d__46'
    Assert-KmcBootstrapInventoryString $worker.workerStatus 'RanToCompletion';Assert-KmcOrdinaryInteger $worker.workerId 1 ([int]::MaxValue)
    foreach($key in @('nativeWorkerMatches','workerCompleted')){Assert-KmcChunk7Boolean $worker.$key $true}
    foreach($key in @('workerFaulted','workerCanceled')){Assert-KmcChunk7Boolean $worker.$key $false}
}
function Assert-KmcProgressionWrite($Write,$Request,$Source){
    Assert-KmcProgressionTexts $Write @('path','leaf','type','operation','sha256','name','gameId','gameName','area')
    $s=$Request.progression;$fixture=Get-KmcChunk7ProgressionFixture $Request;$root=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($Request.evidenceRoot))) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    $owned=Test-KmcChunk7OwnedProgressionCase $s.case
    $leaf=if($owned){'Manual_'+([int]$Write.nativeManualNumber).ToString([Globalization.CultureInfo]::InvariantCulture)+'_KMC_C7_PROGRESSION.zks'}else{'Auto_1.zks'}
    $saveType=if($owned){'Manual'}else{'Auto'}
    if($Write.path-isnot[string]-or$Write.path-cne(Join-Path $root $leaf)-or$Write.leaf-cne$leaf-or$Write.type-cne$saveType-or$Write.operation-cne'None'-or
       $Write.sha256-isnot[string]-or$Write.sha256-cnotmatch'^[a-f0-9]{64}$'-or$Write.sha256-ceq$s.sourceArchiveSha256-or$Write.name-isnot[string]-or[string]::IsNullOrWhiteSpace($Write.name)-or
       $Write.gameId-cne$fixture.gameId-or$Write.gameName-cne$fixture.gameName-or$Write.area-cne$fixture.area){throw 'Native progression Auto1 identity differs.'}
    Assert-KmcOrdinaryInteger $Write.length 1 268435456;Assert-KmcOrdinaryInteger $Write.schema 3 3
    foreach($key in @('snapshotCount','completedWrites')){Assert-KmcOrdinaryInteger $Write.$key 1 1}
    Assert-KmcChunk7Boolean $Write.nativeAutoLevelup (-not$owned);Assert-KmcChunk7Boolean $Write.nativeCallbackSupplied $owned
    $a=$Write.archive;Assert-KmcExactProperties $a @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Pairs','Participations','Slots','Combat') 'progression native output archive'
    Assert-KmcProgressionTexts $a @('CampaignId','AreaId','Policy','RulesId')
    Assert-KmcOrdinaryInteger $a.SchemaVersion 3 3;Assert-KmcOrdinaryInteger $a.GameTimeTicks $Source.GameTimeTicks
    if($a.CampaignId-cne$Source.CampaignId-or$a.AreaId-cne$Source.AreaId-or$a.Policy-cne$Source.Policy-or$a.RulesId-cne$Source.RulesId-or$a.Pairs-isnot[Array]-or$a.Pairs.Count-ne$(if((Get-KmcChunk7ProgressionOperation $s.case)-ceq'horse-native-level'){2}else{1})-or
       $a.Participations-isnot[Array]-or$a.Participations.Count-ne0-or$null-ne$a.Combat-or$a.Slots-isnot[Array]-or$a.Slots.Count-gt4){throw 'Progression native archive invented ownership/combat or policy.'}
    Assert-KmcOrdinaryInteger $Write.pairs $a.Pairs.Count $a.Pairs.Count
    $owners=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($pair in $a.Pairs){
        Assert-KmcExactProperties $pair @('PairId','Generation','ProfileId','Rider','Mount') 'progression native output pair'
        Assert-KmcProgressionTexts $pair @('PairId','ProfileId')
        $original=@($Source.Pairs|Where-Object PairId -CEQ $pair.PairId)
        if($original.Count-ne1-or-not$owners.Add($pair.PairId)-or$pair.ProfileId-cne$original[0].ProfileId-or$pair.Rider.Id-cne$original[0].Rider.Id-or$pair.Mount.Id-cne$original[0].Mount.Id){throw 'Progression autosave invented a mounted registration.'}
        Assert-KmcOrdinaryInteger $pair.Generation $original[0].Generation $original[0].Generation
        Assert-KmcChunk7PersistenceNativeActor $pair.Rider;Assert-KmcChunk7PersistenceNativeActor $pair.Mount
        if((Get-KmcChunk7ProgressionOperation $s.case)-cne'horse-native-level'-and$pair.Rider.Id-ceq$s.actorId){throw 'Retraining retained its replaced mounted owner.'}
    }
    if(@($a.Pairs|Where-Object{$_.Rider.Id-ceq$s.controlActorId}).Count-ne1){throw 'Native progression save lost the independent control pair.'}
    $slots=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($slot in $a.Slots){Assert-KmcExactProperties $slot @('ActorId','Index','Kind') 'progression native output slot';Assert-KmcOrdinaryInteger $slot.Index 0 127;Assert-KmcOrdinaryInteger $slot.Kind 2 3
        Assert-KmcOrdinaryGuid $slot.ActorId
        if($slot.ActorId-cnotin@($s.actorId,$s.controlActorId)-or-not$slots.Add($slot.ActorId+':'+$slot.Index)){throw 'Progression autosave control slot conflicts.'}}
    $prior=@($Source.Slots|Where-Object ActorId -CEQ $s.controlActorId|Sort-Object Index);$after=@($a.Slots|Where-Object ActorId -CEQ $s.controlActorId|Sort-Object Index)
    if((ConvertTo-KmcChunk7Canonical $prior)-cne(ConvertTo-KmcChunk7Canonical $after)){throw 'Native progression altered independent control slots.'}
}
function Assert-KmcChunk7ProgressionEvidence($Request,$Artifact,[string]$Status='PASS'){
    Assert-KmcChunk7ProgressionLoadDescriptor $Request;Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-progression-evidence'
    Assert-KmcOrdinaryInteger $Artifact.schemaVersion 1 1
    if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'Progression PASS requires its native PASS artifact.'};if($Artifact.status-cne'PASS'){return}
    $o=$Artifact.observations;$s=$Request.progression;$write=(Get-KmcChunk7ProgressionOperation $s.case)-cne'horse-native-dismiss';$respec=(Get-KmcChunk7ProgressionOperation $s.case)-cin@('horse-native-retrain-horse','horse-native-retrain-mammoth')
    if((ConvertTo-KmcChunk7Canonical $o.selection)-cne(ConvertTo-KmcChunk7Canonical $s)-or(ConvertTo-KmcChunk7Canonical $o.loadedDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)){throw 'Actual progression selection/source differs.'}
    Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue)
    if(-not(Test-KmcChunk7FiniteNumber $o.elapsedSeconds)-or$o.elapsedSeconds-lt0-or$o.elapsedSeconds-gt945){throw 'Native progression exceeded its bounded completion window.'}
    foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','inventoryRunId','inventoryArtifactSha256')){Assert-KmcProgressionText $o.source.$key;if($o.source.$key-cne$s.$key){throw 'Progression source/inventory receipt differs.'}}
    foreach($key in @('sameCandidate','nativeSourceGraphExact')){Assert-KmcChunk7Boolean $o.source.$key $true}
    Assert-KmcOrdinaryGuid $o.source.inventoryActor.id;if($o.source.inventoryActor.id-cne$s.actorId){throw 'Progression target lacks exact source inventory.'}
    $archiveRequest=if(Test-KmcChunk7OwnedProgressionCase $s.case){Get-KmcChunk7ProgressionOwnedRequest $Request}else{[pscustomobject]@{scenario='chunk7-persistence-load';fixture=$Request.fixture;persistenceCase='multi-exploration-manual'}}
    Assert-KmcChunk7PersistenceArchive $o.loadedArchive $archiveRequest
    $target=@($o.loadedArchive.Pairs|Where-Object{$_.Rider.Id-ceq$s.actorId});$control=@($o.loadedArchive.Pairs|Where-Object{$_.Rider.Id-ceq$s.controlActorId})
    if($target.Count-ne1-or$control.Count-ne1-or$target[0].Mount.Id-cne$s.petId-or$control[0].Mount.Id-cne$s.controlPetId){throw 'Progression target/control source roles differ.'}
    if(Test-KmcChunk7OwnedProgressionCase $s.case){foreach($pair in $o.loadedArchive.Pairs){Assert-KmcBootstrapInventoryString $pair.ProfileId 'medium-humanoid-horse-v1'}}
    Assert-KmcProgressionColdRestore $o.coldRestoration $o.loadedArchive $Request
    if($o.beforeOwners.owners-isnot[Array]-or$o.beforeOwners.owners.Count-ne2){throw 'Progression admission omitted an owner.'}
    foreach($owner in $o.beforeOwners.owners){Assert-KmcProgressionTexts $owner @('ownerId','state','riderId','mountId')}
    foreach($pair in $o.loadedArchive.Pairs){$owner=@($o.beforeOwners.owners|Where-Object ownerId -CEQ $pair.PairId);if($owner.Count-ne1-or$owner[0].state-cne'Active'-or$owner[0].riderId-cne$pair.Rider.Id-or$owner[0].mountId-cne$pair.Mount.Id){throw 'Admitted owner differs from source.'};Assert-KmcOrdinaryInteger $owner[0].generation $pair.Generation $pair.Generation}
    $root=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($Request.evidenceRoot))) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    Assert-KmcProgressionAuthority $o.authorityBefore $root $o.authorityBefore.generation 0 $write
    Assert-KmcProgressionAuthority $o.cleanup.authority $root $o.authorityBefore.generation ([int]$write) $write
    $body=$o.nativeBodyBefore
    foreach($key in @('OriginalSizeOrdinal','EffectiveSizeOrdinal')){Assert-KmcOrdinaryInteger $body.$key 4 4}
    foreach($key in @('RiderPolymorphed','MountPolymorphed')){Assert-KmcChunk7Boolean $body.$key $false}
    if($respec){foreach($key in @('NativeRigAvailable','NativeRaceAndPresetIdentityExact','NativeSkeletonIsExactPreset')){Assert-KmcChunk7Boolean $body.$key $true}
        Assert-KmcProgressionTexts $body @('RaceGuid','PresetGuid','SkeletonName','Gender')
        if($body.RaceGuid-cne$s.raceGuid-or$body.PresetGuid-cne$s.presetGuid-or$body.SkeletonName-cne$s.skeletonName-or$body.Gender-cne$s.gender){throw 'Retrain native body differs from observed explicit pins.'};Assert-KmcChunk7Boolean $body.LeftHanded $s.leftHanded}
    $session=$o.session;$expectedCase=switch -CaseSensitive(Get-KmcChunk7ProgressionOperation $s.case){'horse-native-level'{'ExistingHorseLevel'}'horse-native-dismiss'{'DismissCustomCompanion'}'horse-native-retrain-horse'{'RespecHorse'}'horse-native-retrain-mammoth'{'RespecMammoth'}}
    Assert-KmcProgressionText $session.case
    if($session.case-cne$expectedCase-or$session.errors-isnot[Array]-or$session.errors.Count-ne0-or$session.events-isnot[Array]-or$session.events.Count-gt1600){throw 'Native session case/errors/events differ.'}
    foreach($key in @('started','settled','success','fixtureRollbackOwnedByOuterTransaction')){Assert-KmcChunk7Boolean $session.$key $true}
    Assert-KmcChunk7Boolean $session.remainingWorldCleanupProven $false;Assert-KmcOrdinaryInteger $session.stage 2 2
    foreach($key in @('manualMoneyWrites','manualXpWrites','manualCompanionRemovals','manualNativeRestCalls','manualNativeTimeAdvances')){Assert-KmcOrdinaryInteger $session.$key 0 0;Assert-KmcOrdinaryInteger $o.cleanup.$key 0 0}
    $nativeRows=@($session.events|Where-Object{$null-eq$_.PSObject.Properties['event']});foreach($row in $nativeRows){Assert-KmcProgressionText $row.kind};$beforeRows=@($nativeRows|Where-Object kind -CEQ 'native-progression-admitted')
    if($beforeRows.Count-ne1){throw 'Native progression live admission missing.'};$before=$beforeRows[0];Assert-KmcProgressionActor $before $s.actorId
    if($before.petId-cne$s.petId){throw 'Native admission target pet differs.'}
    Assert-KmcProgressionPairCustody $session.pairCustody $o.loadedArchive $s
    Assert-KmcChunk7Boolean $session.nativeAutomaticTargetCleanupObserved ((Get-KmcChunk7ProgressionOperation $s.case)-cne'horse-native-level')
    $respecProof=$null
    if((Get-KmcChunk7ProgressionOperation $s.case)-ceq'horse-native-level'){if($null-ne$session.nativeUi){throw 'Level-only case opened progression selection UI.'}}
    else{$respecProof=Assert-KmcProgressionUi $session.nativeUi $s $before}
    if($write){
        $choiceRows=@($session.events|Where-Object{$null-ne$_.PSObject.Properties['event']});$choices=Assert-KmcProgressionChoices $choiceRows $Request $before $respecProof
        if(Test-KmcChunk7OwnedProgressionCase $s.case){Assert-KmcProgressionManualProof $session $Request}
        else{Assert-KmcProgressionCompletion $session.nativeCompletion $before $choices.commits $choices.initialLevel
            Assert-KmcOrdinarySaveDescriptorBinding $session.nativeCompletion $session.nativeWrite}
        Assert-KmcProgressionWrite $session.nativeWrite $Request $o.loadedArchive
        $terminal=@($nativeRows|Where-Object kind -CEQ 'native-progression-builder-complete');if($nativeRows.Count-ne2-or$terminal.Count-ne1){throw 'Native builder/session boundary incomplete.'};Assert-KmcProgressionActor $terminal[0] $s.actorId
        if($terminal[0].object-ne$before.object-or$terminal[0].xp-ne$before.xp-or$terminal[0].level-ne$choices.terminal.level-or$terminal[0].petId-cne$choices.terminal.petId-or$terminal[0].money-ne$choices.terminal.money){throw 'Native session terminal progression differs from accepted choices.'}
    }else{
        if($null-ne$session.nativeCompletion-or$null-ne$session.nativeWrite-or$session.events.Count-ne2){throw 'Dismissal granted save or level choice authority.'}
        $terminal=@($nativeRows|Where-Object kind -CEQ 'native-dismissal-complete');if($terminal.Count-ne1){throw 'Native dismissal completion missing.'};Assert-KmcProgressionActor $terminal[0] $s.actorId
        if($terminal[0].object-ne$before.object-or$terminal[0].money-ne$before.money-or$terminal[0].xp-ne$before.xp){throw 'Dismissal fabricated XP/money or actor identity.'}
    }
    foreach($key in @('custodySettled','remainingWorldCleanupProven','workersSettled','nativeProcessesSettled','selectionRestoredToSurvivingActors','pauseRestored','nativeAcquiredStateRetained','sourceUnchanged')){Assert-KmcChunk7Boolean $o.cleanup.$key $true}
    Assert-KmcOrdinaryInteger $o.cleanup.ownersRemaining 0 0
    if(Test-KmcChunk7OwnedProgressionCase $s.case){Assert-KmcChunk7OwnedProgressionRoster $Request $o $before $terminal[0]}
}
function Assert-KmcChunk7ProgressionArtifact($Request,$Manifest,[string]$Status='PASS'){
    $records=@($Manifest.artifacts|Where-Object{$_.kind-ceq'chunk7-progression-evidence'-or$_.relativePath-ceq'chunk7-progression-evidence.json'})
    if(-not(Test-KmcChunk7ProgressionScenario $Request.scenario)){if($records.Count){throw 'Progression evidence under another scenario.'};return}
    if($records.Count-eq0-and$Status-ceq'FAIL'){return}
    $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-progression-evidence.json' 'chunk7-progression-evidence'
    Assert-KmcChunk7ProgressionEvidence $Request $artifact $Status
}
