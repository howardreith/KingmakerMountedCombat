Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7ProgressionOwnedEvidence.ps1')
function Test-KmcChunk7ProgressionScenario([string]$Scenario){$Scenario-ceq'chunk7-native-progression'}
function Test-KmcChunk7ProgressionColdSelection($Selection){$null-ne$Selection-and$Selection.case-is[string]-and$Selection.case-cin@('small-horse-native-retrain-cold','small-horse-native-level-cold','medium-horse-native-retrain-cold','medium-horse-native-retrain-field')}
function Get-KmcChunk7ProgressionRows {@('chunk7-native-progression','C7-progression-cold-source','C7-progression-native-operation','C7-progression-cold-continuation','C7-progression-mammoth-field','C7-progression-cleanup','C7-progression-runtime','C7-progression-interrupted')}
function Get-KmcChunk7ProgressionCases {@('horse-native-level','horse-native-dismiss','horse-native-retrain-horse','horse-native-retrain-mammoth')+(Get-KmcChunk7OwnedProgressionCases)}
function Assert-KmcProgressionObject($Value){Assert-KmcOrdinaryInteger $Value ([int]::MinValue) ([int]::MaxValue);if($Value-eq0){throw 'Native progression object identity is zero.'}}
function Assert-KmcProgressionText($Value){if($Value-isnot[string]-or[string]::IsNullOrEmpty($Value)){throw 'Native progression text must be a nonempty JSON string.'}}
function Convert-KmcProgressionRegistryActorId($Value){
    $parsed=[Guid]::Empty
    if($Value-isnot[string]-or-not[Guid]::TryParseExact($Value,'N',[ref]$parsed)-or$parsed-eq[Guid]::Empty-or$parsed.ToString('N')-cne$Value){throw 'Progression registry actor ID is not canonical.'}
    $parsed.ToString('D')
}
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
    $properties=@('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','inventoryRunId','inventoryArtifactSha256','actorId','petId','controlActorId','controlPetId','raceGuid','presetGuid','skeletonName','gender','leftHanded','bodySize','minimumRangerLevel','expectedNativeWriteCount','nativeWriteLeaf');if(Test-KmcChunk7OwnedProgressionCase $s.case){$properties+=@('ownedSource')};if($s.case-cin@('small-horse-native-retrain-cold','medium-horse-native-retrain-cold','medium-horse-native-retrain-field')){$properties+=@('formerPetId')};if($s.case-ceq'small-horse-native-level'-or(Test-KmcChunk7MediumProgressionCase $s.case)){$properties+=@('fixtureExperienceGrant')};Assert-KmcExactProperties $s $properties 'native progression selection'
    if($s.case-isnot[string]-or($s.case-cnotin(Get-KmcChunk7ProgressionCases)-and-not(Test-KmcChunk7ProgressionColdSelection $s))){throw 'Unknown native progression case.'}
    foreach($key in @('sourceRunId','inventoryRunId')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.$key-ceq$Request.runId){throw 'Progression requires distinct completed source runs.'}}
    foreach($key in @('sourceArtifactSha256','sourceArchiveSha256','inventoryArtifactSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Progression source hashes are missing.'}}
    $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($key in @('actorId','petId','controlActorId','controlPetId')){Assert-KmcOrdinaryGuid $s.$key;if(-not$ids.Add($s.$key)){throw 'Progression actor roles conflict.'}}
    if(Test-KmcChunk7ProgressionColdSelection $s){
        if($s.case-cin@('small-horse-native-retrain-cold','medium-horse-native-retrain-cold','medium-horse-native-retrain-field')){Assert-KmcOrdinaryGuid $s.formerPetId;if(-not$ids.Add($s.formerPetId)){throw 'Former native pet repeats another cold role.'}}
        if($s.sourceRunId-ceq$s.inventoryRunId){throw 'Retrain producer and mounted inventory runs must differ.'}
        Assert-KmcOrdinaryInteger $s.expectedNativeWriteCount 0 0;Assert-KmcOrdinaryInteger $s.minimumRangerLevel 0 0
        foreach($key in @('nativeWriteLeaf','raceGuid','presetGuid','skeletonName','gender','leftHanded','bodySize')){if($null-ne$s.$key){throw 'Cold continuation cannot declare choices, body changes or writes.'}}
        return
    }
    if(Test-KmcChunk7OwnedProgressionCase $s.case){Assert-KmcChunk7OwnedProgressionSelection $Request}
    else{if($s.actorId-cne'd17c8fd0-6627-40c4-bf9a-024cf001c1f7'-or$s.controlActorId-cne'b6628a77-4962-47a4-a17c-88d9836fc9d5'-or$s.controlPetId-cne'd79a4f6c-b74e-4868-95bd-533899131acb'){throw 'Progression source actor/control roles differ from qualified Manual300.'}}
    $write=(Get-KmcChunk7ProgressionOperation $s.case)-cne'horse-native-dismiss';$respec=(Get-KmcChunk7ProgressionOperation $s.case)-cin@('horse-native-retrain-horse','horse-native-retrain-mammoth')
    if($s.case-ceq'small-horse-native-level'){Assert-KmcOrdinaryInteger $s.fixtureExperienceGrant 6000 6000;Assert-KmcOrdinaryInteger $s.minimumRangerLevel 4 4}
    if(Test-KmcChunk7MediumProgressionCase $s.case){Assert-KmcOrdinaryInteger $s.fixtureExperienceGrant 96000 96000;Assert-KmcOrdinaryInteger $s.minimumRangerLevel 10 10}
    Assert-KmcOrdinaryInteger $s.expectedNativeWriteCount ([int]$write) ([int]$write)
    if($write){Assert-KmcOrdinaryInteger $s.minimumRangerLevel 4 20
        if(Test-KmcChunk7OwnedProgressionCase $s.case){if($null-ne$s.nativeWriteLeaf){throw 'Owned progression Manual number must be observed and reserved natively.'}}
        elseif($s.nativeWriteLeaf-isnot[string]-or$s.nativeWriteLeaf-cne'Auto_1.zks'){throw 'Historical native completion autosave leaf differs.'}}
    else{Assert-KmcOrdinaryInteger $s.minimumRangerLevel 0 0;if($null-ne$s.nativeWriteLeaf){throw 'Native dismissal has no write authority.'}}
    if($respec){
        foreach($key in @('raceGuid','presetGuid')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{32}$'){throw 'Exact observed native respec body pin missing.'}}
        $expectedSize=if(Test-KmcChunk7SmallProgressionCase $s.case){'Small'}else{'Medium'}
        if($s.skeletonName-isnot[string]-or[string]::IsNullOrWhiteSpace($s.skeletonName)-or$s.skeletonName.Length-gt160-or$s.gender-isnot[string]-or$s.gender-cnotin@('Male','Female')-or$s.leftHanded-isnot[bool]-or$s.bodySize-isnot[string]-or$s.bodySize-cne$expectedSize){throw 'Respec must preserve the exact declared native body.'}
    }else{foreach($key in @('raceGuid','presetGuid','skeletonName','gender','leftHanded','bodySize')){if($null-ne$s.$key){throw 'This operation cannot reconstruct a native body.'}}}
}
function Assert-KmcChunk7ProgressionLoadDescriptor($Request){
    Assert-KmcChunk7ProgressionSelection $Request
    if(-not(Test-KmcChunk7ProgressionScenario $Request.scenario)){throw 'Progression source descriptor belongs to another family.'}
    $d=$Request.persistenceLoad
    Assert-KmcExactProperties $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'progression manual source'
    Assert-KmcProgressionTexts $d @('internalName','fileName','sha256','gameId','gameName','area')
    if(Test-KmcChunk7ProgressionColdSelection $Request.progression){
        Assert-KmcBootstrapInventoryString $d.internalName 'KMC_C7_PROGRESSION'
        if($d.fileName-isnot[string]-or$d.fileName-cnotmatch'^Manual_([1-9][0-9]*)_KMC_C7_PROGRESSION[.]zks$'){throw 'Cold retrain output is not an engine-numbered Manual.'}
        $number=0;$match=[regex]::Match($d.fileName,'^Manual_([1-9][0-9]*)_KMC_C7_PROGRESSION[.]zks$')
        if(-not[int]::TryParse($match.Groups[1].Value,[ref]$number)-or$number-le0-or$d.fileName-cne('Manual_'+$number+'_KMC_C7_PROGRESSION.zks')){throw 'Cold retrain Manual number is noncanonical.'}
        foreach($pair in @(@('gameId','3f7381f7-aba6-43bf-bae6-39c6a66a7180'),@('gameName','Testy the Modless'),@('area','b10e86516b6437d45a629931c30936fe'),@('sha256',$Request.progression.sourceArchiveSha256))){Assert-KmcBootstrapInventoryString $d.($pair[0]) $pair[1]}
        if($d.sha256-ceq$Request.fixture.baseline.sha256-or$d.sha256-ceq$Request.fixture.working.sha256){throw 'Cold retrain Manual aliases a protected fixture.'}
        Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1
        return
    }
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
function Assert-KmcProgressionColdRestore($Probe,$Archive,$Request,[int]$ExpectedOwners=2){
    Assert-KmcProgressionTexts $Probe @('contract','prepareMetric')
    foreach($key in @('complete','installedBeforeWorldLoad')){Assert-KmcChunk7Boolean $Probe.$key $true}
    Assert-KmcOrdinaryInteger $Probe.maximumEvents 2048 2048
    foreach($key in @('dropped','errors')){Assert-KmcOrdinaryInteger $Probe.$key 0 0}
    if($Probe.contract-cne'chunk7-native-restoration-entry-observer-v1'-or$Probe.prepareMetric-cne'native-method-entries-including-production-refused-attempts'-or
       $Probe.hooks-isnot[Array]-or$Probe.hooks.Count-ne6-or$Probe.events-isnot[Array]-or$Probe.events.Count-gt2048){throw 'Whole-source cold observation incomplete.'}
    foreach($entry in @(@('TurnBased.Controllers.TurnController','Prepare','06000C3C'),@('Kingmaker.Controllers.Combat.UnitCombatState','OnNewRound','0600939D'))){
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
    if($snap.turnBased-isnot[bool]-or$null-ne$snap.combat-or$snap.participations-isnot[Array]-or$snap.participations.Count-ne0-or$snap.owners-isnot[Array]-or$snap.owners.Count-ne$ExpectedOwners){throw 'Cold world invented combat or lost owners.'}
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
    if($Probe.errors-isnot[Array]-or$Probe.errors.Count-ne0-or$Probe.events-isnot[Array]-or$Probe.events.Count-gt192-or$Probe.hooks-isnot[Array]-or$Probe.hooks.Count-ne8){throw 'Native retrain observer incomplete.'}
    Assert-KmcOrdinaryInteger $Probe.nativePetUpdates 1 32;Assert-KmcChunk7Boolean $Probe.petUpdatePending $false
    Assert-KmcOrdinaryInteger $Probe.nativeStartingGoldCredits 1 1;Assert-KmcOrdinaryInteger $Probe.nativeMoneyCredits 1 1;Assert-KmcChunk7Boolean $Probe.moneyCreditPending $false
    Assert-KmcOrdinaryGuid $Probe.originalPetId;Assert-KmcOrdinaryGuid $Probe.reboundPetId;Assert-KmcChunk7Boolean $Probe.originalPetRemoved $true
    if($Probe.originalPetId-cne$Before.petId-or$Probe.reboundPetId-cin@($Selection.actorId,$Selection.petId,$Selection.controlActorId,$Selection.controlPetId)){throw 'Retrain did not prove an exact new sourced pet and original removal.'}
    Assert-KmcOrdinaryInteger $Probe.initialMoney $Before.money $Before.money;Assert-KmcOrdinaryInteger $Probe.nativeCost 0 ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $Probe.nativeStartingGold 0 ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $Probe.initialScrollRefund 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $Probe.nativeScrollRefund 0 ([int]::MaxValue)
    if($Probe.initialMoney-lt$Probe.nativeCost){throw 'Native retrain was not affordable before action.'}
    $contracts=@(@('Kingmaker.Player','RespecCompanion','06000DE7'),@('Kingmaker.UI.LevelUp.CharacterBuildController','HandleLevelUpStart','060039BC'),@('Kingmaker.Designers.EventConditionActionSystem.Actions.RespecCompanion','FinishRespecialization','06008ED5'),@('Kingmaker.Player','GainMoney','06000DD0'),@('Kingmaker.Game','AdvanceGameTime','06000CC6'),@('Kingmaker.Controllers.Rest.RestController','ApplyRest','060091EC'),@('Kingmaker.UI.LevelUp.CharacterBuildController','HandleUnitChangedAfterRespec','060039D7'),@('Kingmaker.UnitLogic.FactLogic.AddPet','TryUpdatePet','06002510'))
    for($i=0;$i-lt$contracts.Count;$i++){$h=$Probe.hooks[$i];Assert-KmcProgressionTexts $h @('type','method','token','moduleMvid');if($h.type-cne$contracts[$i][0]-or$h.method-cne$contracts[$i][1]-or$h.token-cne$contracts[$i][2]-or$h.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Native retrain hook contract differs.'}}
    $r=@{};$frame=-1;$allowed=@('native-respec-enter','native-respec-preview','native-respec-ui-open','native-respec-starting-gold','native-respec-refund-credit','native-respec-finish-enter','native-respec-time-advance','native-respec-rest-before','native-respec-rest-after','native-respec-finish-exit','native-respec-original-rebound','native-respec-pet-update')
    foreach($e in $Probe.events){
        Assert-KmcProgressionText $e.kind;Assert-KmcOrdinaryGuid $e.actorId
        if($e.kind-cnotin$allowed-or$e.actorId-cne$Selection.actorId){throw 'Unknown or foreign native retrain event.'}
        Assert-KmcProgressionObject $e.actorObject;Assert-KmcOrdinaryInteger $e.frame 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $e.gameTicks 0
        if($e.actorObject-ne$Before.object-or$e.frame-lt$frame){throw 'Native retrain actor/frame binding changed.'};$frame=$e.frame
        if($e.kind-cnotin@('native-respec-rest-before','native-respec-rest-after','native-respec-pet-update')){if($r.ContainsKey($e.kind)){throw 'Repeated native retrain boundary.'};$r[$e.kind]=$e}
    }
    foreach($kind in $allowed|Where-Object{$_-cnotin@('native-respec-rest-before','native-respec-rest-after','native-respec-pet-update')}){if(-not$r.ContainsKey($kind)){throw 'Native retrain boundary absent.'}}
    $start=$r['native-respec-enter'];$preview=$r['native-respec-preview'];$gold=$r['native-respec-starting-gold'];$credit=$r['native-respec-refund-credit'];$finish=$r['native-respec-finish-enter'];$end=$r['native-respec-finish-exit'];$rebound=$r['native-respec-original-rebound']
    Assert-KmcOrdinaryInteger $start.xp $Before.xp $Before.xp;Assert-KmcOrdinaryInteger $start.money $Before.money $Before.money
    Assert-KmcOrdinaryInteger $start.cost $Probe.nativeCost $Probe.nativeCost;Assert-KmcOrdinaryInteger $start.scrollRefund $Probe.initialScrollRefund $Probe.initialScrollRefund;Assert-KmcOrdinaryInteger $start.uses 0 ([int]::MaxValue)
    foreach($row in @($start,$rebound)){Assert-KmcProgressionObject $row.descriptorObject;Assert-KmcProgressionObject $row.viewObject}
    Assert-KmcProgressionObject $preview.previewObject;Assert-KmcProgressionObject $preview.descriptorObject
    Assert-KmcOrdinaryGuid $start.petId;if($start.petId-cne$Before.petId){throw 'Native retrain entry has another original pet.'}
    if($preview.previewObject-eq$Before.object-or$preview.descriptorObject-eq$start.descriptorObject-or$preview.nativeCallbackToken-isnot[string]-or$preview.nativeCallbackToken-cnotmatch'^060[0-9A-F]{5}$'-or
       $preview.frame-ne$start.frame-or$r['native-respec-ui-open'].frame-ne$start.frame-or$finish.frame-lt$start.frame-or$rebound.frame-ne$finish.frame-or$end.frame-ne$finish.frame){throw 'Native vacuum preview/rebind does not follow exact native callbacks.'}
    Assert-KmcOrdinaryInteger $gold.amount $Probe.nativeStartingGold $Probe.nativeStartingGold
    Assert-KmcOrdinaryInteger $gold.moneyBefore $Before.money $Before.money
    Assert-KmcOrdinaryInteger $gold.moneyAfter ($Before.money+$gold.amount) ($Before.money+$gold.amount)
    Assert-KmcOrdinaryInteger $credit.amount $Probe.nativeScrollRefund $Probe.nativeScrollRefund
    Assert-KmcOrdinaryInteger $credit.moneyBefore ($Before.money+$gold.amount) ($Before.money+$gold.amount)
    Assert-KmcOrdinaryInteger $credit.moneyAfter ($Before.money+$gold.amount+$credit.amount) ($Before.money+$gold.amount+$credit.amount)
    if($gold.frame-ne$finish.frame-or$gold.gameTicks-ne$finish.gameTicks-or$credit.frame-ne$finish.frame-or$credit.gameTicks-ne$finish.gameTicks-or
       [Array]::IndexOf($Probe.events,$gold)-le[Array]::IndexOf($Probe.events,$r['native-respec-ui-open'])-or
       [Array]::IndexOf($Probe.events,$gold)-ge[Array]::IndexOf($Probe.events,$credit)-or
       [Array]::IndexOf($Probe.events,$credit)-ge[Array]::IndexOf($Probe.events,$finish)){
        throw 'Native starting gold and refund are outside their exact order before finish.'
    }
    Assert-KmcOrdinaryInteger $finish.money ($Before.money+$Probe.nativeStartingGold+$Probe.nativeScrollRefund) ($Before.money+$Probe.nativeStartingGold+$Probe.nativeScrollRefund)
    Assert-KmcOrdinaryInteger $end.money ($Before.money+$Probe.nativeStartingGold+$Probe.nativeScrollRefund-$Probe.nativeCost) ($Before.money+$Probe.nativeStartingGold+$Probe.nativeScrollRefund-$Probe.nativeCost)
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
    $accepted=0;$commits=0;$pendingChoice=$null;$pendingCommit=$null;$finished=$false;$rebound=$false;$lastFrame=-1;$lastTime=-1;$horse=$false;$bond=$false;$mammothRank=0
    foreach($row in $Rows){
        Assert-KmcProgressionTexts $row @('event','runId')
        Assert-KmcOrdinaryInteger $row.frame 0 ([int]::MaxValue)
        if($row.runId-cne$Request.runId-or$row.frame-lt$lastFrame-or-not(Test-KmcChunk7FiniteNumber $row.elapsedSeconds)-or$row.elapsedSeconds-lt$lastTime-or$row.elapsedSeconds-gt720){throw 'Native progression choice ordering differs.'}
        $lastFrame=$row.frame;$lastTime=$row.elapsedSeconds
        if([object]::ReferenceEquals($row,$initial)){continue};if($finished){throw 'Native progression choices continued after completion.'}
        switch -CaseSensitive ($row.event){
            'native-retrain-mammoth-rank-seven' {
                if($s.case-cne'medium-horse-native-retrain-mammoth'-or$mammothRank-ne0-or$null-ne$pendingChoice-or$null-ne$pendingCommit-or$commits-ne10-or$level-ne10){throw 'Unexpected, early or repeated native Mammoth upgrade observation.'}
                Assert-KmcOrdinaryInteger $row.rangerLevel 10 10;Assert-KmcOrdinaryInteger $row.companionRank 7 20
                foreach($pin in @(@('riderSize','Medium'),@('mountSize','Large'),@('upgradeGuid','6a23d16a4476af644af89d91f9f96790'),@('featureGuid','6adc3aab7cde56b40aa189a797254271'),@('petGuid','e7aa96d15a45238438ae4cfb476f6bb9'))){Assert-KmcBootstrapInventoryString $row.($pin[0]) $pin[1]}
                $mammothRank++
            }
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
    if(-not$finished-or$accepted-lt1-or$accepted-gt512-or$commits-gt20-or($retrain-and(-not$horse-or-not$bond))-or
       ($s.case-ceq'medium-horse-native-retrain-mammoth'-and$mammothRank-ne1)){throw 'Native progression choices/companion chain incomplete.'}
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
    Assert-KmcOrdinaryGuid $Custody.ownerId
    $target=@($Archive.Pairs|Where-Object{$_.Rider.Id-ceq$Selection.actorId})[0];$control=@($Archive.Pairs|Where-Object{$_.Rider.Id-ceq$Selection.controlActorId})[0]
    if($Custody.ownerId-cne([Guid]$target.PairId).ToString('D')-or(Convert-KmcProgressionRegistryActorId $Custody.riderId)-cne$Selection.actorId-or(Convert-KmcProgressionRegistryActorId $Custody.mountId)-cne$Selection.petId){throw 'Progression custody lost the original owner.'}
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
function Assert-KmcChunk7RetrainColdState($Row,$Selection,$Saved,$Owner,$Expected){
    $medium=$Selection.case-cin@('medium-horse-native-retrain-cold','medium-horse-native-retrain-field')
    $properties=@('targetId','newPetId','targetLevel','targetXp','targetBlueprint','newPetLevel','newPetBlueprint','nativeBody','oldPetAbsent','targetUnpaired','controlRiderId','controlMountId','controlOwnerId','controlGeneration','nativeMoney','slots','actors','playerInCombat','snapshots','completedSaves','nativeCasts')
    if($medium){$properties+=@('nativeMammoth')}
    Assert-KmcExactProperties $Row $properties 'retrain cold state'
    foreach($pair in @(@('targetId',$Selection.actorId),@('newPetId',$Selection.petId),@('controlRiderId',$Selection.controlActorId),@('controlMountId',$Selection.controlPetId),@('controlOwnerId',$Owner.ownerId))){Assert-KmcBootstrapInventoryString $Row.($pair[0]) $pair[1]}
    foreach($key in @('oldPetAbsent','targetUnpaired')){Assert-KmcChunk7Boolean $Row.$key $true}
    Assert-KmcChunk7Boolean $Row.playerInCombat $false
    if($Row.actors-isnot[Array]-or$Row.actors.Count-ne4){throw 'Cold retrain lost its four exact native actor observations.'}
    $actorIds=@($Selection.actorId,$Selection.petId,$Selection.controlActorId,$Selection.controlPetId)
    $actorSeen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($actor in $Row.actors){Assert-KmcExactProperties $actor @('id','inCombat','prepared') 'retrain cold actor'
        Assert-KmcOrdinaryGuid $actor.id;Assert-KmcChunk7Boolean $actor.inCombat $false
        if($actor.prepared-isnot[bool]-or$actor.prepared-or$actor.id-cnotin$actorIds-or-not$actorSeen.Add($actor.id)){throw 'Cold retrain substituted native actor or preparation state.'}
    }
    foreach($key in @('snapshots','completedSaves','nativeCasts')){Assert-KmcOrdinaryInteger $Row.$key 0 0}
    Assert-KmcOrdinaryInteger $Row.controlGeneration $Owner.generation $Owner.generation
    foreach($key in @('targetLevel','targetXp','newPetLevel','nativeMoney')){Assert-KmcOrdinaryInteger $Row.$key $Expected.$key $Expected.$key}
    Assert-KmcProgressionText $Row.targetBlueprint;Assert-KmcProgressionText $Row.newPetBlueprint
    if($Row.targetBlueprint-cne$Expected.targetBlueprint-or$Row.newPetBlueprint-cne$Expected.newPetBlueprint-or
       (ConvertTo-KmcChunk7Canonical $Row.nativeBody)-cne(ConvertTo-KmcChunk7Canonical $Expected.nativeBody)){
        throw 'Cold retrain native body or blueprint differs from accepted producer.'
    }
    if($medium){
        $m=$Row.nativeMammoth
        Assert-KmcExactProperties $m @('rangerLevel','companionRank','featureGuid','featureSourceGuid','upgradeGuid','petGuid','riderSize','mountSize') 'cold native Mammoth upgrade'
        Assert-KmcOrdinaryInteger $m.rangerLevel 10 10;Assert-KmcOrdinaryInteger $m.companionRank 7 7
        foreach($pin in @(@('featureGuid','6adc3aab7cde56b40aa189a797254271'),@('upgradeGuid','6a23d16a4476af644af89d91f9f96790'),@('petGuid','e7aa96d15a45238438ae4cfb476f6bb9'),@('riderSize','Medium'),@('mountSize','Large'))){Assert-KmcBootstrapInventoryString $m.($pin[0]) $pin[1]}
        if($m.featureSourceGuid-isnot[string]-or$m.featureSourceGuid-cnotmatch'^[a-f0-9]{32}$'){throw 'Cold Mammoth lost its native sourced feature.'}
    }
    if($Row.slots-isnot[Array]-or$Row.slots.Count-ne2){throw 'Cold retrain lost exact native control slots.'}
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($slot in $Row.slots){Assert-KmcExactProperties $slot @('actorId','index','kind') 'retrain cold slot'
        Assert-KmcBootstrapInventoryString $slot.actorId $Selection.controlActorId
        Assert-KmcOrdinaryInteger $slot.index 0 255;Assert-KmcOrdinaryInteger $slot.kind 0 255
        $match=@($Saved.Slots|Where-Object{$_.ActorId-ceq$slot.actorId-and$_.Index-eq$slot.index-and$_.Kind-eq$slot.kind})
        if($match.Count-ne1-or-not$seen.Add($slot.actorId+':'+$slot.index)){throw 'Cold retrain substituted or duplicated a native control slot.'}
    }
}
function Assert-KmcChunk7RetrainColdInterval($Observations){
    Assert-KmcOrdinaryInteger $Observations.coldInitialFrame 1 ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $Observations.coldContinuationFrame ($Observations.coldInitialFrame+5) ([int]::MaxValue)
    if((ConvertTo-KmcChunk7Canonical $Observations.coldInitial)-cne(ConvertTo-KmcChunk7Canonical $Observations.coldContinuation)){
        throw 'Read-only cold continuation changed mounted/native state after restoration.'
    }
}
function Assert-KmcChunk7RetrainColdObserver($Events,$Selection){
    if($Events-isnot[Array]){throw 'Cold retrain restoration events are missing.'}
    $allIds=@($Selection.actorId,$Selection.petId,$Selection.controlActorId,$Selection.controlPetId)
    if(@($Events|Where-Object{$_.PSObject.Properties['actorId']-and$_.actorId-cin$allIds-and$_.kind-cin@('prepare-before','round-before')}).Count){
        throw 'Cold retrain replayed native preparation or round for a target or surviving owner.'
    }
    foreach($kind in @('restore-debt-before','restore-debt-after')){
        if(@($Events|Where-Object{$_.PSObject.Properties['actorId']-and$_.actorId-cin$allIds-and$_.kind-ceq$kind}).Count-ne2){
            throw 'Cold retrain restored native actor debt more than once or to a retired target.'
        }
    }
}
function Assert-KmcChunk7ProgressionColdEvidence($Request,$Artifact,[string]$Status='PASS'){
    Assert-KmcChunk7ProgressionLoadDescriptor $Request
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-progression-evidence'
    if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'Retrain cold PASS requires its native PASS artifact.'}
    if($Artifact.status-cne'PASS'){return}
    $s=$Request.progression;$o=$Artifact.observations
    if((ConvertTo-KmcChunk7Canonical $o.selection)-cne(ConvertTo-KmcChunk7Canonical $s)-or
       (ConvertTo-KmcChunk7Canonical $o.loadedDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)){
        throw 'Cold retrain selected another native source or actor role.'
    }
    $source=Get-KmcChunk7ProgressionColdSource $s $Request.fixture $Request
    $producer=Read-KmcJson $source.artifactPath
    $saved=$producer.observations.session.nativeWrite.archive
    Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue)
    if($o.processId-eq$source.sourceProcessId-or$o.source.sourceProcessId-ne$source.sourceProcessId-or
       (ConvertTo-KmcChunk7Canonical $o.loadedArchive)-cne(ConvertTo-KmcChunk7Canonical $saved)-or
       $o.source.sourceRunId-cne$s.sourceRunId-or$o.source.sourceArtifactSha256-cne$s.sourceArtifactSha256-or
       $o.source.sourceArchiveSha256-cne$s.sourceArchiveSha256-or$o.source.inventoryRunId-cne$s.inventoryRunId-or
       $o.source.inventoryArtifactSha256-cne$s.inventoryArtifactSha256){throw 'Separate cold consumer did not restore exact producer bytes and process lineage.'}
    Assert-KmcChunk7Boolean $o.source.sameCandidate $true
    if($saved.Pairs-isnot[Array]-or$saved.Pairs.Count-ne1-or$saved.Pairs[0].Rider.Id-cne$s.controlActorId-or
       $saved.Pairs[0].Mount.Id-cne$s.controlPetId-or$saved.Pairs[0].ProfileId-cne'medium-humanoid-horse-v1'-or
       $saved.Slots-isnot[Array]-or$saved.Slots.Count-ne2-or@($saved.Slots|Where-Object ActorId -CNE $s.controlActorId).Count-ne0){
        throw 'Cold retrain accepted another mounted graph or control slots.'
    }
    Assert-KmcProgressionColdRestore $o.coldRestoration $saved $Request 1
    Assert-KmcChunk7RetrainColdObserver $o.coldRestoration.events $s
    $owners=@($o.beforeOwners.owners)
    if($owners.Count-ne1-or$owners[0].ownerId-cne$saved.Pairs[0].PairId-or
       (Convert-KmcProgressionRegistryActorId $owners[0].riderId)-cne$s.controlActorId-or
       (Convert-KmcProgressionRegistryActorId $owners[0].mountId)-cne$s.controlPetId-or$owners[0].state-cne'Active'){
        throw 'Cold retrain live registry does not retain exactly its independent control pair.'
    }
    Assert-KmcOrdinaryInteger $owners[0].generation $saved.Pairs[0].Generation $saved.Pairs[0].Generation
    $terminal=@($producer.observations.session.events|Where-Object{$null-ne$_.PSObject.Properties['kind']-and$_.kind-ceq'native-progression-builder-complete'})
    if($terminal.Count-ne1){throw 'Cold retrain producer lacks one accepted terminal native actor.'}
    $body=$producer.observations.nativeBodyBefore
    $expected=[pscustomobject]@{targetLevel=$terminal[0].level;targetXp=$terminal[0].xp;newPetLevel=$terminal[0].petLevel;
        nativeMoney=$terminal[0].money;targetBlueprint=$producer.observations.source.inventoryActor.blueprint;newPetBlueprint=$(if($s.case-cin@('medium-horse-native-retrain-cold','medium-horse-native-retrain-field')){'e7aa96d15a45238438ae4cfb476f6bb9'}else{$body.MountBlueprintGuid});
        nativeBody=[pscustomobject]@{raceGuid=$body.RaceGuid;presetGuid=$body.PresetGuid;skeletonName=$body.SkeletonName;
            gender=$body.Gender;leftHanded=$body.LeftHanded;originalSizeOrdinal=$body.OriginalSizeOrdinal;
            effectiveSizeOrdinal=$body.EffectiveSizeOrdinal}}
    Assert-KmcExactProperties $o.producerTerminal @('targetLevel','targetXp','newPetLevel','nativeMoney','targetBlueprint','newPetBlueprint','nativeBody') 'retrain cold producer terminal'
    foreach($key in @('targetLevel','targetXp','newPetLevel','nativeMoney')){Assert-KmcOrdinaryInteger $o.producerTerminal.$key $expected.$key $expected.$key}
    Assert-KmcBootstrapInventoryString $o.producerTerminal.targetBlueprint $expected.targetBlueprint
    Assert-KmcBootstrapInventoryString $o.producerTerminal.newPetBlueprint $expected.newPetBlueprint
    if((ConvertTo-KmcChunk7Canonical $o.producerTerminal.nativeBody)-cne(ConvertTo-KmcChunk7Canonical $expected.nativeBody)-or
       $o.producerTerminal.targetLevel-ne$expected.targetLevel-or$o.producerTerminal.targetXp-ne$expected.targetXp-or
       $o.producerTerminal.newPetLevel-ne$expected.newPetLevel-or$o.producerTerminal.nativeMoney-ne$expected.nativeMoney-or
       $o.producerTerminal.newPetBlueprint-cne$expected.newPetBlueprint-or
       $o.producerTerminal.targetBlueprint-cne$expected.targetBlueprint){throw 'Cold retrain claimed terminal native state differs from accepted producer.'}
    foreach($phase in @('coldInitial','coldContinuation')){
        Assert-KmcChunk7RetrainColdState $o.$phase $s $saved $owners[0] $expected
    }
    Assert-KmcChunk7RetrainColdInterval $o
    $root=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($Request.evidenceRoot))) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    Assert-KmcProgressionAuthority $o.authorityBefore $root $o.authorityBefore.generation 0 $false
    Assert-KmcProgressionAuthority $o.cleanup.authority $root $o.authorityBefore.generation 0 $false
    foreach($key in @('custodySettled','remainingWorldCleanupProven','workersSettled','nativeProcessesSettled','selectionRestoredToSurvivingActors','pauseRestored','nativeAcquiredStateRetained','sourceUnchanged')){Assert-KmcChunk7Boolean $o.cleanup.$key $true}
    Assert-KmcOrdinaryInteger $o.cleanup.ownersRemaining 0 0
    foreach($key in @('manualMoneyWrites','manualXpWrites','manualCompanionRemovals','manualNativeRestCalls','manualNativeTimeAdvances')){Assert-KmcOrdinaryInteger $o.cleanup.$key 0 0}
}
function Assert-KmcChunk7MammothFieldEvidence($Request,$Artifact,[string]$Status='PASS'){
    Assert-KmcChunk7ProgressionColdEvidence $Request $Artifact $Status
    if($Artifact.status-cne'PASS'){return}
    Assert-KmcChunk7MammothFieldOutcome $Request.progression $Artifact.observations
}
function Assert-KmcChunk7MammothFieldOutcome($s,$o){
    if($s.case-cne'medium-horse-native-retrain-field'){throw 'Mammoth field reader accepted another case.'}
    $inputs=@($o.fieldInputs)
    if($inputs.Count-ne3){throw 'Mammoth field did not retain exactly Mount, ground and Dismount inputs.'}
    $mount=$inputs[0];$ground=$inputs[1];$dismount=$inputs[2]
    foreach($item in @($mount,$dismount)){
        Assert-KmcChunk7Boolean $item.clicked $true
        Assert-KmcChunk7Boolean $item.shellTerminal $true
        Assert-KmcChunk7Boolean $item.processTerminal $true
        Assert-KmcOrdinaryInteger $item.nativeCastDelta 1 1
        Assert-KmcOrdinaryInteger $item.nativeRefusalDelta 0 0
        Assert-KmcBootstrapInventoryString $item.casterId $s.actorId
        Assert-KmcProgressionText $item.abilityGuid
        Assert-KmcProgressionText $item.shellControl
        Assert-KmcOrdinaryInteger $item.frame 1 ([int]::MaxValue)
        Assert-KmcOrdinaryInteger $item.settledFrame $item.frame ([int]::MaxValue)
    }
    Assert-KmcBootstrapInventoryString $mount.label 'mount-mammoth'
    Assert-KmcBootstrapInventoryString $mount.targetId $s.petId
    Assert-KmcBootstrapInventoryString $mount.abilityGuid 'f053faad986631688defa003cd7bda0e'
    Assert-KmcOrdinaryInteger $mount.generationAtInit 0 0
    Assert-KmcBootstrapInventoryString $dismount.label 'dismount-mammoth'
    Assert-KmcBootstrapInventoryString $dismount.targetId $s.actorId
    Assert-KmcBootstrapInventoryString $dismount.abilityGuid '3af2b81f4d72bbb30501fa730fcdf36e'
    Assert-KmcOrdinaryInteger $dismount.generationAtInit $o.fieldMounted.targetGeneration $o.fieldMounted.targetGeneration
    if($mount.shellControl-cnotmatch ('^shell:[1-9][0-9]*:mount:'+[regex]::Escape($s.actorId)+'$')-or
       $dismount.shellControl-cnotmatch ('^shell:[1-9][0-9]*:dismount:'+[regex]::Escape($s.actorId)+'$')-or
       $mount.shellControl-ceq$dismount.shellControl){throw 'Mammoth field native shell identities differ.'}
    Assert-KmcBootstrapInventoryString $ground.label 'mammoth-ground'
    Assert-KmcBootstrapInventoryString $ground.selectedId $s.actorId
    Assert-KmcBootstrapInventoryString $ground.executorId $s.petId
    foreach($key in @('nativeCallEntered','nativeCallReturned','commandValidated')){Assert-KmcChunk7Boolean $ground.$key $true}
    foreach($state in @($o.fieldMounted,$o.fieldDismounted)){
        Assert-KmcBootstrapInventoryString $state.controlOwnerId $o.beforeOwners.owners[0].ownerId
        Assert-KmcOrdinaryInteger $state.controlGeneration $o.beforeOwners.owners[0].generation $o.beforeOwners.owners[0].generation
        foreach($key in @('saveSnapshots','completedSaves')){Assert-KmcOrdinaryInteger $state.$key 0 0}
    }
    if($o.fieldMounted.targetOwnerId-isnot[string]-or$o.fieldMounted.targetOwnerId-cnotmatch'^[a-f0-9]{32}$'-or
       $o.fieldMounted.targetOwnerId-ceq$o.fieldMounted.controlOwnerId){throw 'Native Mammoth Mount lacks an independent owner.'}
    Assert-KmcOrdinaryInteger $o.fieldMounted.targetGeneration 1 ([long]::MaxValue)
    Assert-KmcBootstrapInventoryString $o.fieldMounted.targetProfileId 'medium-humanoid-mammoth-v1'
    Assert-KmcBootstrapInventoryString $o.fieldMounted.releasedOwnerId $o.fieldMounted.targetOwnerId
    Assert-KmcBootstrapInventoryString $o.fieldMounted.releasedOwnerState 'Active'
    Assert-KmcBootstrapInventoryString $o.fieldMounted.releasedBoundaryState 'Mounted'
    Assert-KmcOrdinaryInteger $o.fieldMounted.owners 2 2
    Assert-KmcOrdinaryInteger $o.fieldMounted.nativeCasts 1 1
    $boundary=$o.fieldDismountBoundary
    Assert-KmcBootstrapInventoryString $boundary.shellResult 'Success'
    foreach($key in @('shellFinished','processEnded','riderPetIsMammoth','mountMasterIsRider')){Assert-KmcChunk7Boolean $boundary.$key $true}
    foreach($key in @('targetOwnerId','targetGeneration','targetState')){if($null-ne$boundary.$key){throw 'Native Dismount retained active Mammoth ownership at terminal delivery.'}}
    Assert-KmcBootstrapInventoryString $boundary.releasedOwnerId $o.fieldMounted.targetOwnerId
    Assert-KmcBootstrapInventoryString $boundary.releasedBoundaryState 'Unmounted'
    Assert-KmcBootstrapInventoryString $boundary.controlOwnerId $o.fieldMounted.controlOwnerId
    Assert-KmcOrdinaryInteger $boundary.controlGeneration $o.fieldMounted.controlGeneration $o.fieldMounted.controlGeneration
    Assert-KmcBootstrapInventoryString $boundary.riderPetId $s.petId
    Assert-KmcBootstrapInventoryString $boundary.mountMasterId $s.actorId
    Assert-KmcOrdinaryInteger $boundary.activeOwners 1 1
    Assert-KmcOrdinaryInteger $boundary.owners 1 2
    if($boundary.owners-eq2){
        Assert-KmcBootstrapInventoryString $boundary.releasedOwnerState 'Detached'
        Assert-KmcBootstrapInventoryString $boundary.targetCustodyOwnerId $o.fieldMounted.targetOwnerId
        Assert-KmcBootstrapInventoryString $boundary.targetCustodyState 'Detached'
        Assert-KmcBootstrapInventoryString $boundary.mountCustodyOwnerId $boundary.targetCustodyOwnerId
        Assert-KmcBootstrapInventoryString $boundary.mountCustodyState 'Detached'
    }elseif($null-ne$boundary.targetCustodyOwnerId-or$null-ne$boundary.targetCustodyState-or
           $null-ne$boundary.mountCustodyOwnerId-or$null-ne$boundary.mountCustodyState){throw 'Retired Mammoth retained indexed custody.'}
    else{Assert-KmcBootstrapInventoryString $boundary.releasedOwnerState 'Retired'}
    Assert-KmcOrdinaryInteger $boundary.nativeCasts 2 2
    Assert-KmcOrdinaryInteger $boundary.nativeRefusals 0 0
    Assert-KmcOrdinaryInteger $boundary.dispatchAccepted 2 2
    Assert-KmcOrdinaryInteger $boundary.dispatchRejected 0 0
    Assert-KmcProgressionText $boundary.shellLifecycle
    if($boundary.shellLifecycle-cnotmatch ('DeliverConsumed;control='+[regex]::Escape($dismount.shellControl)+';predicate=consumed-success;kind=Dismount')){
        throw 'Mammoth Dismount lacks exact successful native shell delivery.'
    }
    if($null-ne$o.fieldDismounted.targetOwnerId-or$null-ne$o.fieldDismounted.targetGeneration-or
       $null-ne$o.fieldDismounted.targetProfileId){throw 'Native Mammoth Dismount retained its target owner.'}
    Assert-KmcOrdinaryInteger $o.fieldDismounted.owners 1 1
    Assert-KmcOrdinaryInteger $o.fieldDismounted.nativeCasts 2 2
    Assert-KmcBootstrapInventoryString $o.fieldDismounted.releasedOwnerId $o.fieldMounted.targetOwnerId
    Assert-KmcBootstrapInventoryString $o.fieldDismounted.releasedOwnerState 'Retired'
    Assert-KmcBootstrapInventoryString $o.fieldDismounted.releasedBoundaryState 'Unmounted'
    foreach($key in @('commandFinished','createdByPlayer')){Assert-KmcChunk7Boolean $o.fieldMovement.$key $true}
    Assert-KmcBootstrapInventoryString $o.fieldMovement.result 'Success'
    Assert-KmcBootstrapInventoryString $o.fieldMovement.executorId $s.petId
    Assert-KmcBootstrapInventoryString $o.fieldMovement.ownerId $o.fieldMounted.targetOwnerId
    Assert-KmcOrdinaryInteger $o.fieldMovement.ownerGeneration $o.fieldMounted.targetGeneration $o.fieldMounted.targetGeneration
    if($o.fieldMovement.distance-isnot[ValueType]-or[double]$o.fieldMovement.distance-lt0.5-or[double]$o.fieldMovement.distance-gt5){throw 'Native Mammoth field travel did not meet the bounded distance.'}
    Assert-KmcOrdinaryInteger $o.fieldMovement.startFrame $ground.frame $ground.frame
    Assert-KmcOrdinaryInteger $o.fieldMovement.endFrame $ground.frame ([int]::MaxValue)
    if($mount.frame-ge$ground.frame-or$ground.frame-ge$dismount.frame-or
       $o.fieldMovement.endFrame-ge$dismount.frame-or
       $o.fieldMounted.frame-lt$mount.settledFrame-or$o.fieldMounted.frame-ge$ground.frame-or
       $boundary.frame-lt$dismount.settledFrame-or$o.fieldDismounted.frame-lt$boundary.frame){
        throw 'Mammoth field input and settled-owner chronology differs.'
    }
}
function Assert-KmcChunk7LevelColdEvidence($Request,$Artifact,[string]$Status='PASS'){
    Assert-KmcChunk7ProgressionLoadDescriptor $Request
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-progression-evidence'
    if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'Small level cold PASS requires its native PASS artifact.'}
    if($Artifact.status-cne'PASS'){return}
    $s=$Request.progression;$o=$Artifact.observations
    if((ConvertTo-KmcChunk7Canonical $o.selection)-cne(ConvertTo-KmcChunk7Canonical $s)-or
       (ConvertTo-KmcChunk7Canonical $o.loadedDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)){
        throw 'Small level cold selected another native source or actor role.'
    }
    $source=Get-KmcChunk7ProgressionColdSource $s $Request.fixture $Request
    $producer=Read-KmcJson $source.artifactPath;$saved=$producer.observations.session.nativeWrite.archive
    Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue)
    if($o.processId-eq$source.sourceProcessId-or$o.source.sourceProcessId-ne$source.sourceProcessId-or
       (ConvertTo-KmcChunk7Canonical $o.loadedArchive)-cne(ConvertTo-KmcChunk7Canonical $saved)-or
       $o.source.sourceRunId-cne$s.sourceRunId-or$o.source.sourceArtifactSha256-cne$s.sourceArtifactSha256-or
       $o.source.sourceArchiveSha256-cne$s.sourceArchiveSha256-or$o.source.inventoryRunId-cne$s.inventoryRunId-or
       $o.source.inventoryArtifactSha256-cne$s.inventoryArtifactSha256){throw 'Small level cold lacks exact producer bytes and separate process lineage.'}
    Assert-KmcChunk7Boolean $o.source.sameCandidate $true
    if($saved.Pairs-isnot[Array]-or$saved.Pairs.Count-ne2-or
       @($saved.Pairs|Where-Object{$_.Rider.Id-ceq$s.actorId-and$_.Mount.Id-ceq$s.petId-and$_.ProfileId-ceq'small-default-halfling-horse-v1'}).Count-ne1-or
       @($saved.Pairs|Where-Object{$_.Rider.Id-ceq$s.controlActorId-and$_.Mount.Id-ceq$s.controlPetId-and$_.ProfileId-ceq'medium-humanoid-horse-v1'}).Count-ne1){throw 'Small level cold accepted another mounted graph.'}
    Assert-KmcProgressionColdRestore $o.coldRestoration $saved $Request 2
    $ids=@($s.actorId,$s.petId,$s.controlActorId,$s.controlPetId)
    $events=$o.coldRestoration.events
    if($events-isnot[Array]-or@($events|Where-Object{$_.PSObject.Properties['actorId']-and$_.actorId-cin$ids-and$_.kind-cin@('prepare-before','round-before')}).Count){throw 'Small level cold replayed native preparation or round.'}
    foreach($kind in @('restore-debt-before','restore-debt-after')){if(@($events|Where-Object{$_.PSObject.Properties['actorId']-and$_.actorId-cin$ids-and$_.kind-ceq$kind}).Count-ne4){throw 'Small level cold did not restore four native debts exactly once.'}}
    $owners=@($o.beforeOwners.owners)
    if($owners.Count-ne2){throw 'Small level cold did not retain both original mounted owners.'}
    foreach($savedPair in $saved.Pairs){
        $owner=@($owners|Where-Object ownerId -CEQ $savedPair.PairId)
        if($owner.Count-ne1-or(Convert-KmcProgressionRegistryActorId $owner[0].riderId)-cne$savedPair.Rider.Id-or
           (Convert-KmcProgressionRegistryActorId $owner[0].mountId)-cne$savedPair.Mount.Id-or$owner[0].state-cne'Active'){
            throw 'Small level cold reassigned a saved mounted owner.'
        }
        Assert-KmcOrdinaryInteger $owner[0].generation $savedPair.Generation $savedPair.Generation
    }
    $terminal=@($producer.observations.session.events|Where-Object{$null-ne$_.PSObject.Properties['kind']-and$_.kind-ceq'native-progression-builder-complete'})
    $builder=@($producer.observations.session.events|Where-Object{$null-ne$_.PSObject.Properties['event']-and$_.event-ceq'native-builder-complete'})
    if($terminal.Count-ne1-or$builder.Count-ne1){throw 'Small level producer lacks its accepted native terminal and builder outcome.'}
    $expected=[pscustomobject]@{targetLevel=$terminal[0].level;targetXp=$terminal[0].xp;petLevel=$terminal[0].petLevel;money=$terminal[0].money}
    Assert-KmcExactProperties $o.producerTerminal @('targetLevel','targetXp','petLevel','money') 'Small level cold producer terminal'
    foreach($key in @('targetLevel','targetXp','petLevel','money')){Assert-KmcOrdinaryInteger $o.producerTerminal.$key $expected.$key $expected.$key}
    Assert-KmcOrdinaryInteger $expected.targetLevel 5 5;Assert-KmcOrdinaryInteger $expected.targetXp 15000 15000
    $inventory=Read-KmcJson $source.inventory.path
    $rows=$inventory.observations.inheritedFixture.arrangement.actors
    $control=@($rows|Where-Object actorId -CEQ $s.controlActorId);$controlPet=@($rows|Where-Object actorId -CEQ $s.controlPetId)
    if($control.Count-ne1-or$controlPet.Count-ne1){throw 'Small level cold lacks exact independent control actor source.'}
    foreach($phase in @('coldInitial','coldContinuation')){
        $state=$o.$phase
        Assert-KmcExactProperties $state @('targetLevel','targetXp','petLevel','petXp','controlLevel','controlXp','controlPetLevel','controlPetXp','money','nativeLevelOutcome','owners','fieldRoster','slots','actors','snapshots','completedSaves','nativeCasts') 'Small level cold state'
        foreach($key in @('targetLevel','targetXp','petLevel','money')){Assert-KmcOrdinaryInteger $state.$key $expected.$key $expected.$key}
        Assert-KmcOrdinaryInteger $state.petXp $builder[0].actor.petExperience $builder[0].actor.petExperience
        foreach($pair in @(@('controlLevel',$control[0].characterLevel),@('controlXp',$control[0].experience),@('controlPetLevel',$controlPet[0].characterLevel),@('controlPetXp',$controlPet[0].experience))){Assert-KmcOrdinaryInteger $state.($pair[0]) $pair[1] $pair[1]}
        foreach($key in @('snapshots','completedSaves','nativeCasts')){Assert-KmcOrdinaryInteger $state.$key 0 0}
        if((ConvertTo-KmcChunk7Canonical $state.nativeLevelOutcome)-cne(ConvertTo-KmcChunk7Canonical $producer.observations.nativeLevelOutcome)-or
           (ConvertTo-KmcChunk7Canonical $o.producerNativeLevelOutcome)-cne(ConvertTo-KmcChunk7Canonical $producer.observations.nativeLevelOutcome)){
            throw 'Small level cold lost producer native Ranger or sourced Horse progression.'
        }
        if((ConvertTo-KmcChunk7Canonical $state.owners)-cne(ConvertTo-KmcChunk7Canonical $o.beforeOwners.owners)){throw 'Small level cold changed mounted owner custody.'}
        if($state.fieldRoster-isnot[Array]-or$state.fieldRoster.Count-ne7-or$rows-isnot[Array]-or$rows.Count-ne7){throw 'Small level cold lacks its seven-actor native field roster.'}
        $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach($actor in $state.fieldRoster){
            Assert-KmcExactProperties $actor @('actorId','blueprint','characterLevel','experience') 'Small level cold field actor'
            $prior=@($rows|Where-Object actorId -CEQ $actor.actorId)
            if($prior.Count-ne1-or-not$seen.Add($actor.actorId)-or$actor.blueprint-cne$prior[0].blueprint){throw 'Small level cold substituted a native field actor.'}
            if($actor.actorId-ceq$s.actorId){Assert-KmcOrdinaryInteger $actor.characterLevel $expected.targetLevel $expected.targetLevel;Assert-KmcOrdinaryInteger $actor.experience $expected.targetXp $expected.targetXp}
            elseif($actor.actorId-ceq$s.petId){Assert-KmcOrdinaryInteger $actor.characterLevel $expected.petLevel $expected.petLevel;Assert-KmcOrdinaryInteger $actor.experience $builder[0].actor.petExperience $builder[0].actor.petExperience}
            else{Assert-KmcOrdinaryInteger $actor.characterLevel $prior[0].characterLevel $prior[0].characterLevel;Assert-KmcOrdinaryInteger $actor.experience $prior[0].experience $prior[0].experience}
        }
        if($state.slots-isnot[Array]-or$state.slots.Count-ne$saved.Slots.Count){throw 'Small level cold changed native control slot count.'}
        foreach($slot in $state.slots){if(@($saved.Slots|Where-Object{$_.ActorId-ceq$slot.actorId-and$_.Index-eq$slot.index-and$_.Kind-eq$slot.kind}).Count-ne1){throw 'Small level cold substituted a native control slot.'}}
        if($state.actors-isnot[Array]-or$state.actors.Count-ne4){throw 'Small level cold lost an actor.'}
        foreach($actor in $state.actors){if($actor.id-cnotin$ids-or$actor.inCombat-ne$false-or$actor.prepared-ne$false){throw 'Small level cold entered combat or fabricated preparation.'}}
    }
    Assert-KmcChunk7RetrainColdInterval $o
    $root=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($Request.evidenceRoot))) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    Assert-KmcProgressionAuthority $o.authorityBefore $root $o.authorityBefore.generation 0 $false
    Assert-KmcProgressionAuthority $o.cleanup.authority $root $o.authorityBefore.generation 0 $false
    foreach($key in @('custodySettled','remainingWorldCleanupProven','workersSettled','nativeProcessesSettled','selectionRestoredToSurvivingActors','pauseRestored','nativeAcquiredStateRetained','sourceUnchanged')){Assert-KmcChunk7Boolean $o.cleanup.$key $true}
    Assert-KmcOrdinaryInteger $o.cleanup.ownersRemaining 0 0
    foreach($key in @('manualMoneyWrites','manualXpWrites','manualCompanionRemovals','manualNativeRestCalls','manualNativeTimeAdvances')){Assert-KmcOrdinaryInteger $o.cleanup.$key 0 0}
}
function Assert-KmcChunk7PaidExperienceSetup($Request,$Observations,$Before){
    $s=$Request.progression;$setup=$Observations.fixtureExperienceSetup
    $medium=Test-KmcChunk7MediumProgressionCase $s.case
    Assert-KmcExactProperties $setup @('contract','methodToken','methodModuleMvid','sourceXp','sourceLevel','nextThreshold','nativeGrant','afterXp','afterLevel','petXpBefore','petXpAfter','petLevelBefore','petLevelAfter','controlXpBefore','controlXpAfter','controlPetXpBefore','controlPetXpAfter','moneyBefore','moneyAfter','snapshotsBefore','snapshotsAfter','completedSavesBefore','completedSavesAfter','castsBefore','castsAfter','sourceInitiallyUnready','targetReadyAfterGrant','ordinaryLevelChoicesStarted') 'Small later-level fixture setup'
    Assert-KmcBootstrapInventoryString $setup.contract $(if($medium){'medium-native-mammoth-eligibility-setup-v1'}else{'small-native-level-eligibility-setup-v1'})
    Assert-KmcBootstrapInventoryString $setup.methodToken '06001F80'
    Assert-KmcBootstrapInventoryString $setup.methodModuleMvid '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'
    $fixture=if($medium){$Observations.source.mediumColdFixture}else{$Observations.source.smallColdFixture}
    $rows=$fixture.arrangement.actors
    $target=@($rows|Where-Object actorId -CEQ $s.actorId);$pet=@($rows|Where-Object actorId -CEQ $s.petId)
    $control=@($rows|Where-Object actorId -CEQ $s.controlActorId);$controlPet=@($rows|Where-Object actorId -CEQ $s.controlPetId)
    if($target.Count-ne1-or$pet.Count-ne1-or$control.Count-ne1-or$controlPet.Count-ne1){throw 'Small XP setup lacks four exact source actors.'}
    foreach($item in @(@('sourceXp',$target[0].experience),@('sourceLevel',$target[0].characterLevel),@('petXpBefore',$pet[0].experience),@('petLevelBefore',$pet[0].characterLevel),@('controlXpBefore',$control[0].experience),@('controlPetXpBefore',$controlPet[0].experience),@('moneyBefore',$fixture.arrangement.money))){Assert-KmcOrdinaryInteger $setup.($item[0]) $item[1] $item[1]}
    Assert-KmcOrdinaryInteger $setup.sourceXp 9000 9000;Assert-KmcOrdinaryInteger $setup.sourceLevel 4 4
    Assert-KmcOrdinaryInteger $setup.nextThreshold $(if($medium){105000}else{15000}) $(if($medium){105000}else{15000})
    Assert-KmcOrdinaryInteger $setup.nativeGrant $s.fixtureExperienceGrant $s.fixtureExperienceGrant
    Assert-KmcOrdinaryInteger $setup.afterXp $Before.xp $Before.xp
    Assert-KmcOrdinaryInteger $setup.afterLevel $Before.level $Before.level
    if($setup.nextThreshold-$setup.sourceXp-ne$setup.nativeGrant-or$setup.afterXp-ne$setup.nextThreshold-or$setup.afterLevel-ne$setup.sourceLevel){throw 'Paid fixture XP changed beyond its one native eligibility grant.'}
    foreach($pair in @(@('petXpBefore','petXpAfter'),@('petLevelBefore','petLevelAfter'),@('controlXpBefore','controlXpAfter'),@('controlPetXpBefore','controlPetXpAfter'),@('moneyBefore','moneyAfter'),@('snapshotsBefore','snapshotsAfter'),@('completedSavesBefore','completedSavesAfter'),@('castsBefore','castsAfter'))){
        Assert-KmcOrdinaryInteger $setup.($pair[0]) 0 ([int]::MaxValue)
        Assert-KmcOrdinaryInteger $setup.($pair[1]) $setup.($pair[0]) $setup.($pair[0])
    }
    foreach($key in @('snapshotsBefore','completedSavesBefore','castsBefore')){Assert-KmcOrdinaryInteger $setup.$key 0 0}
    Assert-KmcChunk7Boolean $setup.sourceInitiallyUnready $true
    Assert-KmcChunk7Boolean $setup.targetReadyAfterGrant $true
    Assert-KmcChunk7Boolean $setup.ordinaryLevelChoicesStarted $false
}
function Assert-KmcChunk7SmallLevelOutcome($Request,$Observations,$Choices){
    $outcome=$Observations.nativeLevelOutcome;$s=$Request.progression
    Assert-KmcExactProperties $outcome @('rangerLevel','horseFeatureGuid','horseFeatureSourceGuid','horseRankGuid','horseRank','petBlueprintGuid','controlRangerLevel','controlHorseFeatureSourceGuid','controlHorseRank') 'Small native level outcome'
    Assert-KmcOrdinaryInteger $outcome.rangerLevel 5 5
    Assert-KmcOrdinaryInteger $outcome.rangerLevel $Choices.terminal.rangerLevel $Choices.terminal.rangerLevel
    Assert-KmcBootstrapInventoryString $outcome.horseFeatureGuid '7db7c50677e39f09feef56f3831fc723'
    Assert-KmcBootstrapInventoryString $outcome.horseRankGuid '1670990255e4fe948a863bafd5dbda5d'
    $pair=@($Observations.source.smallColdFixture.pairs|Where-Object riderId -CEQ $s.actorId)
    $pet=@($Observations.source.smallColdFixture.arrangement.actors|Where-Object actorId -CEQ $s.petId)
    if($pair.Count-ne1-or$pet.Count-ne1){throw 'Small native level outcome lacks its original sourced Horse facts.'}
    Assert-KmcBootstrapInventoryString $outcome.horseFeatureSourceGuid $pair[0].sourceGuid
    Assert-KmcBootstrapInventoryString $outcome.petBlueprintGuid $pet[0].blueprint
    Assert-KmcOrdinaryInteger $outcome.horseRank $pair[0].rank 20
    $controlPair=@($Observations.source.smallColdFixture.pairs|Where-Object riderId -CEQ $s.controlActorId)
    $control=@($Observations.source.smallColdFixture.arrangement.actors|Where-Object actorId -CEQ $s.controlActorId)
    if($controlPair.Count-ne1-or$control.Count-ne1){throw 'Small native level outcome lacks the neighboring sourced Horse.'}
    $ranger=@($control[0].classes|Where-Object classGuid -CEQ 'cda0615668a6df14eb36ba19ee881af6')
    if($ranger.Count-ne1){throw 'Small native level outcome lacks exact control Ranger class provenance.'}
    Assert-KmcOrdinaryInteger $outcome.controlRangerLevel $ranger[0].level $ranger[0].level
    Assert-KmcBootstrapInventoryString $outcome.controlHorseFeatureSourceGuid $controlPair[0].sourceGuid
    Assert-KmcOrdinaryInteger $outcome.controlHorseRank $controlPair[0].rank $controlPair[0].rank
}
function Assert-KmcChunk7ProgressionEvidence($Request,$Artifact,[string]$Status='PASS'){
    if(Test-KmcChunk7ProgressionColdSelection $Request.progression){if($Request.progression.case-ceq'small-horse-native-level-cold'){Assert-KmcChunk7LevelColdEvidence $Request $Artifact $Status}elseif($Request.progression.case-ceq'medium-horse-native-retrain-field'){Assert-KmcChunk7MammothFieldEvidence $Request $Artifact $Status}else{Assert-KmcChunk7ProgressionColdEvidence $Request $Artifact $Status};return}
    Assert-KmcChunk7ProgressionLoadDescriptor $Request;Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-progression-evidence'
    Assert-KmcOrdinaryInteger $Artifact.schemaVersion 1 1
    if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'Progression PASS requires its native PASS artifact.'};if($Artifact.status-cne'PASS'){return}
    $o=$Artifact.observations;$s=$Request.progression;$write=(Get-KmcChunk7ProgressionOperation $s.case)-cne'horse-native-dismiss';$respec=(Get-KmcChunk7ProgressionOperation $s.case)-cin@('horse-native-retrain-horse','horse-native-retrain-mammoth')
    if((ConvertTo-KmcChunk7Canonical $o.selection)-cne(ConvertTo-KmcChunk7Canonical $s)-or(ConvertTo-KmcChunk7Canonical $o.loadedDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)){throw 'Actual progression selection/source differs.'}
    Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue)
    if(-not(Test-KmcChunk7FiniteNumber $o.elapsedSeconds)-or$o.elapsedSeconds-lt0-or$o.elapsedSeconds-gt945){throw 'Native progression exceeded its bounded completion window.'}
    foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','inventoryRunId','inventoryArtifactSha256')){Assert-KmcProgressionText $o.source.$key;if($o.source.$key-cne$s.$key){throw 'Progression source/inventory receipt differs.'}}
    foreach($key in @('sameCandidate','nativeSourceGraphExact')){Assert-KmcChunk7Boolean $o.source.$key $true}
    $sourceActorId=if(Test-KmcChunk7PaidProgressionCase $s.case){$o.source.inventoryActor.actorId}else{$o.source.inventoryActor.id}
    Assert-KmcOrdinaryGuid $sourceActorId;if($sourceActorId-cne$s.actorId){throw 'Progression target lacks exact source inventory.'}
    $archiveRequest=if(Test-KmcChunk7OwnedProgressionCase $s.case){Get-KmcChunk7ProgressionOwnedRequest $Request}else{[pscustomobject]@{scenario='chunk7-persistence-load';fixture=$Request.fixture;persistenceCase='multi-exploration-manual'}}
    Assert-KmcChunk7PersistenceArchive $o.loadedArchive $archiveRequest
    $target=@($o.loadedArchive.Pairs|Where-Object{$_.Rider.Id-ceq$s.actorId});$control=@($o.loadedArchive.Pairs|Where-Object{$_.Rider.Id-ceq$s.controlActorId})
    if($target.Count-ne1-or$control.Count-ne1-or$target[0].Mount.Id-cne$s.petId-or$control[0].Mount.Id-cne$s.controlPetId){throw 'Progression target/control source roles differ.'}
    if(Test-KmcChunk7OwnedProgressionCase $s.case){foreach($pair in $o.loadedArchive.Pairs){
        $expectedProfile=if((Test-KmcChunk7SmallProgressionCase $s.case)-and$pair.Rider.Id-ceq$s.actorId){'small-default-halfling-horse-v1'}else{'medium-humanoid-horse-v1'}
        Assert-KmcBootstrapInventoryString $pair.ProfileId $expectedProfile
    }}
    Assert-KmcProgressionColdRestore $o.coldRestoration $o.loadedArchive $Request
    if($o.beforeOwners.owners-isnot[Array]-or$o.beforeOwners.owners.Count-ne2){throw 'Progression admission omitted an owner.'}
    foreach($owner in $o.beforeOwners.owners){Assert-KmcProgressionTexts $owner @('ownerId','state','riderId','mountId')}
    foreach($pair in $o.loadedArchive.Pairs){$owner=@($o.beforeOwners.owners|Where-Object ownerId -CEQ $pair.PairId);if($owner.Count-ne1-or$owner[0].state-cne'Active'-or(Convert-KmcProgressionRegistryActorId $owner[0].riderId)-cne$pair.Rider.Id-or(Convert-KmcProgressionRegistryActorId $owner[0].mountId)-cne$pair.Mount.Id){throw 'Admitted owner differs from source.'};Assert-KmcOrdinaryInteger $owner[0].generation $pair.Generation $pair.Generation}
    $root=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($Request.evidenceRoot))) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
    Assert-KmcProgressionAuthority $o.authorityBefore $root $o.authorityBefore.generation 0 $write
    Assert-KmcProgressionAuthority $o.cleanup.authority $root $o.authorityBefore.generation ([int]$write) $write
    $body=$o.nativeBodyBefore
    $targetSize=if(Test-KmcChunk7SmallProgressionCase $s.case){3}else{4}
    foreach($key in @('OriginalSizeOrdinal','EffectiveSizeOrdinal')){Assert-KmcOrdinaryInteger $body.$key $targetSize $targetSize}
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
    if($s.case-ceq'small-horse-native-level'-or(Test-KmcChunk7MediumProgressionCase $s.case)){Assert-KmcChunk7PaidExperienceSetup $Request $o $before}
    elseif($null-ne$o.PSObject.Properties['fixtureExperienceSetup']){throw 'Non-level progression carried a fixture XP grant.'}
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
        if($s.case-ceq'small-horse-native-level'){Assert-KmcChunk7SmallLevelOutcome $Request $o $choices}
        elseif($null-ne$o.PSObject.Properties['nativeLevelOutcome']){throw 'Non-Small level claimed a native Small progression outcome.'}
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
