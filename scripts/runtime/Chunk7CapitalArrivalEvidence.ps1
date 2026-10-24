Set-StrictMode -Version Latest
# Matrix rows 58-59 (owner decision after b195 x4c). In a native capital only the main character and
# the main character's pet are directly controllable, so KMC retires (dismounts) a pair refused only by
# that rule and keeps the controllable pairs. Row 58 cold-loads a two-owner mounted Manual, makes one
# transfer into the reviewed capital pin and writes one native Manual there; row 59 cold-loads that
# Manual in a separate process. This is the only route admitted into a capital: every other case keeps
# Assert-KmcChunk7RouteAvoidsKnownCapitals and the in-game IsCapital refusal.
function Test-KmcChunk7CapitalArrivalCase([string]$Case){$Case-ceq'multi-exploration-capital-arrival-manual'}
function Test-KmcChunk7CapitalColdCase([string]$Case){$Case-ceq'multi-exploration-capital-arrival-cold-manual'}
function Test-KmcChunk7CapitalCase([string]$Case){(Test-KmcChunk7CapitalArrivalCase $Case)-or(Test-KmcChunk7CapitalColdCase $Case)}
function Get-KmcChunk7CapitalArrivalManualName {'KMC_C7_CAPITAL'}
# The single reviewed capital pin, from the same native observation the avoidance guard uses.
function Get-KmcChunk7CapitalArrivalPin {
    $pins=@(Get-KmcChunk7KnownCapitalAreaPins)
    if($pins.Count-ne1){throw 'Capital arrival requires exactly one reviewed capital pin.'}
    $pins[0]
}
function Get-KmcChunk7CapitalArrivalArea {[string](Get-KmcChunk7CapitalArrivalPin).area}
# Runner-side scoped admission: row 58 must name exactly the reviewed capital entry and area, once each.
function Assert-KmcChunk7CapitalArrivalRoute($EnterPoints,$Areas) {
    $pin=Get-KmcChunk7CapitalArrivalPin
    $enter=@($EnterPoints);$area=@($Areas)
    if($enter.Count-ne1-or$area.Count-ne1-or$enter[0]-isnot[string]-or$area[0]-isnot[string]-or
       $enter[0]-cne$pin.enterPoint-or$area[0]-cne$pin.area){throw 'Capital arrival admits only the reviewed capital entry point and area.'}
}
# Request-side scoped admission: the producer declares exactly that pin with no native autosave; the
# cold consumer declares no area transfer at all.
function Assert-KmcChunk7CapitalArrivalTarget($Request) {
    if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
    $case=if($null-ne$Request.PSObject.Properties['persistenceCase']){$Request.persistenceCase}else{$null}
    $has=$null-ne$Request.PSObject.Properties['persistenceAreaTarget']-and$null-ne$Request.persistenceAreaTarget
    if(Test-KmcChunk7CapitalArrivalCase $case){
        if(-not$has){throw 'Capital arrival requires its exact declared capital target.'}
        $t=$Request.persistenceAreaTarget;Assert-KmcExactProperties $t @('enterPoint','area','autoSaveMode') 'capital arrival target'
        Assert-KmcChunk7CapitalArrivalRoute @($t.enterPoint) @($t.area)
        if($t.autoSaveMode-isnot[string]-or$t.autoSaveMode-cne'None'){throw 'Capital arrival forbids a native transition autosave.'}
        return
    }
    if((Test-KmcChunk7CapitalColdCase $case)-and$has){throw 'The capital cold consumer declares no area transfer.'}
}
function Get-KmcChunk7CapitalFeedback([string]$Rider,[string]$Mount,[int]$Restored,[int]$Total) {
    'Dismounted on arrival because the companion is not directly controllable in this area: '+$Rider+' and '+$Mount+'. Mount again after leaving. '+$Restored+' of '+$Total+' mounted relationships restored without Mount or preparation.'
}
# The two-pair input of row 58, validated exactly as its x4s-shaped cold Manual (source case, no route, no write).
function Get-KmcChunk7CapitalInputRequest($Request) {
    $q=$Request|ConvertTo-Json -Depth 90|ConvertFrom-Json
    $q.persistenceCase=Get-KmcChunk7PersistenceSourceCase $Request.persistenceCase
    if($null-ne$q.PSObject.Properties['persistenceAreaTarget']){$q.PSObject.Properties.Remove('persistenceAreaTarget')}
    $q.ownedPairs.expectedNativeWriteCount=0
    $q
}
function Get-KmcChunk7CapitalSnapshot($Observations,[string]$Label) {
    $rows=@($Observations.snapshots|Where-Object label -CEQ $Label)
    if($rows.Count-ne1){throw ('Capital snapshot '+$Label+' is missing or duplicated.')}
    $rows[0]
}
# Native restoration entry observer: four hooks, no preparation or round, one debt restore per listed actor only.
function Assert-KmcChunk7CapitalRestorationProbe($Probe,$Request,[string[]]$RestoredIds,[string[]]$AllIds) {
    Assert-KmcChunk7NativeCounters $Probe @('maximumEvents','dropped','errors')
    Assert-KmcChunk7Boolean $Probe.complete $true;Assert-KmcChunk7Boolean $Probe.installedBeforeWorldLoad $true
    if($Probe.contract-cne'chunk7-native-restoration-entry-observer-v1'-or$Probe.maximumEvents-ne2048-or$Probe.dropped-ne0-or$Probe.errors-ne0-or
       $Probe.prepareMetric-cne'native-method-entries-including-production-refused-attempts'-or@($Probe.hooks).Count-ne4){throw 'Capital restoration observer incomplete or wrong.'}
    if(@($Probe.events|Where-Object{$_.actorId-cin$AllIds-and$_.kind-cin@('prepare-before','round-before','restore-actor-before')}).Count-ne0){throw 'Capital restoration entered native preparation, a round or a combat actor restore.'}
    foreach($id in $AllIds){foreach($kind in @('restore-debt-before','restore-debt-after')){
        $count=@($Probe.events|Where-Object{$_.actorId-ceq$id-and$_.kind-ceq$kind}).Count
        if($count-ne$(if($id-cin$RestoredIds){1}else{0})){throw 'Capital debt restore was not observed exactly once for each restored actor only.'}
    }}
    $prepare=@($Probe.hooks|Where-Object{$_.declaringType-ceq'TurnBased.Controllers.TurnController'-and$_.method-ceq'Prepare'-and$_.token-ceq'06000C3C'-and$_.moduleMvid-ceq'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})
    $round=@($Probe.hooks|Where-Object{$_.token-ceq'0600939D'-and$_.moduleMvid-ceq'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})
    if($prepare.Count-ne1-or$round.Count-ne1){throw 'Capital restoration hooks differ from the installed native contract.'}
    foreach($method in @('RestoreActor','RestoreDebt')){if(@($Probe.hooks|Where-Object{$_.declaringType-ceq'KingmakerMountedCombat.Integration.NativeCombatActorPersistence'-and$_.method-ceq$method-and$_.moduleMvid-ceq$Request.dllMvid}).Count-ne1){throw 'Capital restore observer is not bound to the exact candidate method.'}}
}
# Per-actor native debt at a capital snapshot: every archive actor exactly once, never above its saved
# debt and recovered by no more than the observed native clock (the generic exploration bound).
function Assert-KmcChunk7CapitalSnapshotDebt($Snapshot,$Archive) {
    if($Snapshot.actors-isnot[Array]){throw 'Capital snapshot actors are missing.'}
    if(-not(Test-KmcExactJsonInteger $Snapshot.gameTicks)-or$Snapshot.gameTicks-lt$Archive.GameTimeTicks){throw 'Capital snapshot clock precedes its archive.'}
    $elapsed=([double]$Snapshot.gameTicks-[double]$Archive.GameTimeTicks)/10000000.0
    if($elapsed-gt30){throw 'Capital debt comparison escaped its bounded observation window.'}
    foreach($a in @($Archive.Pairs|ForEach-Object{$_.Rider;$_.Mount})){
        $row=@($Snapshot.actors|Where-Object{$_.native.Id-ceq$a.Id})
        if($row.Count-ne1){throw 'Capital snapshot omitted or duplicated an archive actor.'}
        $b=$row[0].native;Assert-KmcChunk7PersistenceNativeActor $b;Assert-KmcChunk7Boolean $row[0].inCombat $false
        foreach($field in @('Standard','Move','Swift','Initiative','Reaction')){
            if([double]$b.$field-gt[double]$a.$field+0.001-or[double]$b.$field+$elapsed+0.001+($elapsed*0.1)-lt[double]$a.$field){throw 'Capital native debt changed beyond elapsed native clock progress.'}
        }
        if($a.ReactionsRemaining-ne$b.ReactionsRemaining-or$a.LastSurpriseTicks-ne$b.LastSurpriseTicks){throw 'Capital native reaction or surprise history changed.'}
    }
}
# Exactly one mounted owner, the main pair, at a capital snapshot.
function Assert-KmcChunk7CapitalOneOwner($Snapshot,$MainPair) {
    if($Snapshot.owners-isnot[Array]-or$Snapshot.owners.Count-ne1){throw 'Capital snapshot does not hold exactly one mounted owner.'}
    $o=$Snapshot.owners[0]
    if(([Guid]$o.ownerId)-ne([Guid]$MainPair.PairId)-or$o.generation-ne$MainPair.Generation-or$o.riderId-cne$MainPair.Rider.Id-or$o.mountId-cne$MainPair.Mount.Id-or$o.state-cne'Active'){throw 'Capital snapshot owner is not the exact main pair.'}
}
# The one-pair capital archive: exactly the main pair with its two controls, in the capital.
function Assert-KmcChunk7CapitalArchive($Archive,$Request,$MainPair,$MainSlots,$PolicyArchive) {
    Assert-KmcExactProperties $Archive @('SchemaVersion','CampaignId','AreaId','GameTimeTicks','Policy','RulesId','Pairs','Participations','Slots','Combat') 'capital native archive'
    if(-not(Test-KmcExactJsonInteger $Archive.SchemaVersion)-or$Archive.SchemaVersion-ne3-or$Archive.CampaignId-cne$Request.persistenceLoad.gameId-or
       $Archive.AreaId-cne(Get-KmcChunk7CapitalArrivalArea)-or-not(Test-KmcExactJsonInteger $Archive.GameTimeTicks)-or$Archive.GameTimeTicks-lt0-or
       $Archive.Policy-cne$PolicyArchive.Policy-or$Archive.RulesId-cne$PolicyArchive.RulesId-or$Archive.Pairs-isnot[Array]-or$Archive.Pairs.Count-ne1-or
       $Archive.Participations-isnot[Array]-or$Archive.Participations.Count-ne0-or$null-ne$Archive.Combat-or$Archive.Slots-isnot[Array]-or$Archive.Slots.Count-ne2){
        throw 'Capital archive is not exactly one mounted exploration pair in the capital.'
    }
    $pair=$Archive.Pairs[0];Assert-KmcExactProperties $pair @('PairId','Generation','ProfileId','Rider','Mount') 'capital native pair'
    foreach($key in @('PairId','ProfileId')){Assert-KmcBootstrapInventoryString $pair.$key $MainPair.$key}
    if($pair.Generation-ne$MainPair.Generation-or$pair.Rider.Id-cne$MainPair.Rider.Id-or$pair.Mount.Id-cne$MainPair.Mount.Id){throw 'Capital archive changed the main pair stable custody.'}
    foreach($actor in @($pair.Rider,$pair.Mount)){Assert-KmcChunk7PersistenceNativeActor $actor}
    if((ConvertTo-KmcChunk7Canonical @($Archive.Slots|Sort-Object Index))-cne(ConvertTo-KmcChunk7Canonical @($MainSlots|Sort-Object Index))){throw 'Capital archive controls differ from the main pair bindings.'}
}
function Assert-KmcChunk7CapitalWorldRow($Row,[string]$Label,[bool]$Capital) {
    Assert-KmcBootstrapInventoryString $Row.label $Label
    Assert-KmcBootstrapInventoryString $Row.area $(if($Capital){Get-KmcChunk7CapitalArrivalArea}else{[string]$Row.area})
    Assert-KmcChunk7Boolean $Row.capital $Capital;Assert-KmcBootstrapInventoryString $Row.mode 'Default'
    if($Row.views-isnot[Array]-or$Row.views.Count-ne4){throw 'Capital world row must name the four pair actors.'}
}
# The arrival portion of row 58 exactly as observed natively (batch196 r58): counters, UMM line, observer,
# main-pair authority, main-only controls, and the companion pair natively out of the scene (inGame false)
# with its original views and no authority. Shared by the full reader and the r58 evidence check.
function Assert-KmcChunk7CapitalArrivalShape($Request,$c,$loaded,$ColdEvents) {
    $roles=@(Get-KmcChunk7OwnedPairIds $Request);$all=@($roles)
    $main=@($loaded.Pairs|Where-Object{$_.Rider.Id-ceq$roles[0]})[0];$retired=@($loaded.Pairs|Where-Object{$_.Rider.Id-ceq$roles[2]})[0]
    $pin=Get-KmcChunk7CapitalArrivalPin
    foreach($pair in @(@('contract','chunk7-capital-arrival-v1'),@('enterPoint',$pin.enterPoint),@('destinationArea',$pin.area),@('sourceArea',$Request.persistenceLoad.area),@('autoSaveMode','None'),@('mainPairId',$main.PairId),@('retiredPairId',$retired.PairId))){Assert-KmcBootstrapInventoryString $c.($pair[0]) $pair[1]}
    foreach($key in @('destinationIsCapital','dispatchReturned','suspensionObserved')){Assert-KmcChunk7Boolean $c.$key $true};Assert-KmcChunk7Boolean $c.nativeAutosaveRequested $false
    Assert-KmcOrdinaryInteger $c.transferRequests 1 1;Assert-KmcOrdinaryInteger $c.loadingTicks 1 ([int]::MaxValue)
    foreach($key in @('retiredRiderName','retiredMountName')){if($c.$key-isnot[string]-or[string]::IsNullOrWhiteSpace($c.$key)-or$c.$key.Length-gt256-or$c.$key-match'[\x00-\x1f\x7f]'){throw 'Retired pair names must be exact bounded scalars.'}}
    # The UMM summary names the dismounted pair and the exact restored count.
    Assert-KmcBootstrapInventoryString $c.umm (Get-KmcChunk7CapitalFeedback $c.retiredRiderName $c.retiredMountName 1 2)
    # The arrival observer saw no new native preparation, round or debt restore after the cold start.
    Assert-KmcChunk7CapitalRestorationProbe $c.restoration $Request $all $all
    if((ConvertTo-KmcChunk7Canonical $c.restoration.events)-cne(ConvertTo-KmcChunk7Canonical $ColdEvents)){throw 'Capital arrival recorded native restoration events beyond the cold start.'}
    $before=$c.before;Assert-KmcChunk7CapitalWorldRow $before 'before-native-transfer' $false;Assert-KmcBootstrapInventoryString $before.area $Request.persistenceLoad.area
    foreach($key in @('suspensions','resumes','retired','failedPairs','refusedTransfers')){Assert-KmcOrdinaryInteger $before.$key 0 0};Assert-KmcOrdinaryInteger $before.activeOwners 2 2
    foreach($view in $before.views){Assert-KmcChunk7Boolean $view.inGame $true;Assert-KmcChunk7Boolean $view.directlyControllable $true}
    $after=$c.after;Assert-KmcChunk7CapitalWorldRow $after 'after-native-transfer' $true
    foreach($pair in @(@('suspensions',1),@('resumes',1),@('retired',1),@('failedPairs',0),@('refusedTransfers',0),@('activeOwners',1))){Assert-KmcOrdinaryInteger $after.($pair[0]) $pair[1] $pair[1]}
    Assert-KmcChunk7Boolean $after.saveAllowed $true;Assert-KmcBootstrapInventoryString $after.feedback $c.umm
    Assert-KmcChunk7CapitalOneOwner $after $main
    $mainSlots=@($loaded.Slots|Where-Object ActorId -CEQ $roles[0])
    if((ConvertTo-KmcChunk7Canonical @($after.slots|Sort-Object Index))-cne(ConvertTo-KmcChunk7Canonical @($mainSlots|Sort-Object Index))){throw 'Capital arrival controls are not exactly the main pair bindings.'}
    foreach($view in $after.views){
        $baseline=@($before.views|Where-Object id -CEQ $view.id)
        if($baseline.Count-ne1-or$view.viewId-ne$baseline[0].viewId-or$view.viewId-ne$view.baselineViewId-or$view.viewId-eq0){throw 'Capital arrival replaced an original native view.'}
        $isMain=$view.id-cin@($roles[0],$roles[1])
        # Observed natively: the main pair stays in the scene; the companion pair is out of it.
        Assert-KmcChunk7Boolean $view.directlyControllable $isMain;Assert-KmcChunk7Boolean $view.inGame $isMain
        if($view.id-ceq$roles[0]){if($view.riderAgents-ne1-or$view.agentOverride-eq0){throw 'Main rider lacks its exact restored movement authority.'}}
        elseif($view.riderAgents-ne0-or$view.agentOverride-ne0){throw 'A non-rider or retired actor retains movement authority.'}
    }
    $mainRider=@($before.views|Where-Object id -CEQ $roles[0])[0];$mainRiderAfter=@($after.views|Where-Object id -CEQ $roles[0])[0]
    if($mainRiderAfter.agentOverride-eq$mainRider.agentOverride){throw 'Capital arrival reused the prior movement authority.'}
}
# The recorded out-of-scene custody of the companion pair, in one closed membership shape.
function Assert-KmcChunk7CapitalOutOfSceneRecord($Record,$Request,[bool]$ViewsRequired) {
    $roles=@(Get-KmcChunk7OwnedPairIds $Request)
    Assert-KmcExactProperties $Record @('ids','shape','actors','admittedNativeRemoteCompanions') 'capital out-of-scene record'
    Assert-KmcChunk7CapitalNativeRemoteRecord $Record.admittedNativeRemoteCompanions $Request
    if($Record.ids-isnot[Array]-or($Record.ids-join'|')-cne($roles[2]+'|'+$roles[3])){throw 'Capital out-of-scene record is not exactly the companion pair.'}
    if($Record.shape-isnot[string]-or$Record.shape-cnotin@('field','remote','absent')){throw 'Capital out-of-scene membership shape is not closed.'}
    if($Record.actors-isnot[Array]-or$Record.actors.Count-ne2){throw 'Capital out-of-scene record must describe both units.'}
    for($i=0;$i-lt2;$i++){$a=$Record.actors[$i]
        Assert-KmcExactProperties $a @('id','inGame','inState','directlyControllable','viewPresent','liveRigValidated') 'capital out-of-scene actor'
        Assert-KmcBootstrapInventoryString $a.id $roles[2+$i];Assert-KmcChunk7Boolean $a.inGame $false;Assert-KmcChunk7Boolean $a.directlyControllable $false;Assert-KmcChunk7Boolean $a.liveRigValidated $false
        if($a.inState-isnot[bool]-or$a.viewPresent-isnot[bool]){throw 'Capital out-of-scene actor state must be recorded booleans.'}
        if($ViewsRequired){Assert-KmcChunk7Boolean $a.viewPresent $true}
    }
}
# Batch197 r58: native capital mode also moves the unpaired story companions to the remote party while
# they stay in the game. The field checks an admitted actor's admission replaces, by name (the product's
# Chunk7CapitalRemoteCompanionRule.SkippedChecks).
function Get-KmcChunk7CapitalRemoteSkippedChecks {@('field-party-membership','field-character-membership','not-remote-companion')}
function Get-KmcChunk7CapitalRemoteRecordFields {@('id','inParty','character','remote','inGame','inState','directlyControllable','viewPresent','mainCharacter','pairMember','skippedChecks')}
# The recorded admitted native remote companions: exact flags, ordinal order, producer roster only, never
# the main character or any KMC pair actor.
function Assert-KmcChunk7CapitalNativeRemoteRecord($Admitted,$Request) {
    if($Admitted-isnot[Array]){throw 'Capital native remote companions must be a recorded array.'}
    $pairs=@(@(Get-KmcChunk7OwnedPairIds $Request)+@(Get-KmcChunk7InheritedPairIds $Request));$main=$Request.ownedPairs.riderAId;$prior=$null
    foreach($a in $Admitted){
        Assert-KmcExactProperties $a (Get-KmcChunk7CapitalRemoteRecordFields) 'capital native remote companion'
        Assert-KmcOrdinaryGuid $a.id
        if($a.id-cnotin@($Request.ownedPairs.partyIds)){throw 'Capital native remote companion is outside the producer roster.'}
        if($a.id-ceq$main){throw 'Capital native remote companion is the main character.'}
        if($a.id-cin$pairs){throw 'Capital native remote companion is a KMC paired actor.'}
        if($null-ne$prior-and[string]::CompareOrdinal($prior,$a.id)-ge0){throw 'Capital native remote companions repeat or are not in ordinal order.'};$prior=$a.id
        foreach($pair in @(@('inParty',$false),@('character',$false),@('remote',$true),@('inGame',$true),@('directlyControllable',$false),@('mainCharacter',$false),@('pairMember',$false))){Assert-KmcChunk7Boolean $a.($pair[0]) $pair[1]}
        if($a.inState-isnot[bool]-or$a.viewPresent-isnot[bool]){throw 'Capital native remote companion state must be recorded booleans.'}
        if($a.skippedChecks-isnot[Array]-or($a.skippedChecks-join'|')-cne((Get-KmcChunk7CapitalRemoteSkippedChecks)-join'|')){throw 'Capital native remote companion skipped checks are not exactly named.'}
    }
}
# The admission derived independently from a capital world row's roster: every remote actor other than the
# out-of-scene pair must be an unpaired, non-main producer-roster companion, out of the field, in the game and
# uncontrollable, in the reviewed capital. Pair membership includes the loaded archive's pair records.
function Get-KmcChunk7CapitalNativeRemoteCompanions($Row,$Request,$Archive) {
    $roles=@(Get-KmcChunk7OwnedPairIds $Request);$main=$Request.ownedPairs.riderAId
    $pairs=@(@($roles)+@(Get-KmcChunk7InheritedPairIds $Request)+@(@($Archive.Pairs)|ForEach-Object{$_.Rider.Id;$_.Mount.Id}))
    if($Row.roster-isnot[Array]){throw 'Capital world roster is missing or incomplete.'}
    $out=[Collections.Generic.List[object]]::new()
    $remote=@($Row.roster|Where-Object{$_.remote-eq$true-and$_.id-cnotin@($roles[2],$roles[3])})
    [string[]]$order=@($remote|ForEach-Object{[string]$_.id});[Array]::Sort($order,[StringComparer]::Ordinal)
    foreach($r in @(foreach($id in $order){@($remote|Where-Object id -CEQ $id)[0]})){
        if($Row.capital-isnot[bool]-or-not$Row.capital-or$Row.area-cne(Get-KmcChunk7CapitalArrivalArea)){throw ('Native remote companion outside a capital: '+$r.id)}
        if($r.id-cnotin@($Request.ownedPairs.partyIds)){throw ('Capital native field roster gained an actor outside the producer roster: '+$r.id)}
        if($r.id-ceq$main){throw ('The main character moved to the remote party: '+$r.id)}
        if($r.id-cin$pairs){throw ('A KMC paired actor moved to the remote party: '+$r.id)}
        if($r.inParty-ne$false-or$r.character-ne$false){throw ('A remote companion is still in the native field roster: '+$r.id)}
        if($r.inGame-ne$true){throw ('A remote companion is out of the game: '+$r.id)}
        if($r.directlyControllable-ne$false){throw ('A remote companion is directly controllable: '+$r.id)}
        $out.Add([pscustomobject][ordered]@{id=$r.id;inParty=$r.inParty;character=$r.character;remote=$r.remote;inGame=$r.inGame;inState=$r.inState;directlyControllable=$r.directlyControllable;viewPresent=$r.viewPresent;mainCharacter=$false;pairMember=$false;skippedChecks=@(Get-KmcChunk7CapitalRemoteSkippedChecks)})
    }
    $out.ToArray()
}
# Native roster membership of every captured field actor at a capital world row, consistent with the record.
function Assert-KmcChunk7CapitalRoster($Row,$Request,[AllowNull()][string]$Shape,$Admitted=@(),$Archive=$null) {
    $roles=@(Get-KmcChunk7OwnedPairIds $Request);$ids=@($Request.ownedPairs.partyIds)
    if($Row.roster-isnot[Array]-or$Row.roster.Count-ne$ids.Count){throw 'Capital world roster is missing or incomplete.'}
    Assert-KmcChunk7CapitalNativeRemoteRecord @($Admitted) $Request
    $derived=if([string]::IsNullOrEmpty($Shape)){@()}else{@(Get-KmcChunk7CapitalNativeRemoteCompanions $Row $Request $Archive)}
    $remoteIds=@($derived|ForEach-Object{$_.id})
    foreach($id in $ids){
        $r=@($Row.roster|Where-Object id -CEQ $id);if($r.Count-ne1){throw 'Capital world roster omits or repeats an actor.'};$r=$r[0]
        Assert-KmcExactProperties $r @('id','inParty','character','remote','inGame','inState','directlyControllable','viewPresent') 'capital roster actor'
        # An admitted native remote companion was judged by the derivation above; its field checks are the named skips.
        if($id-cin$remoteIds){continue}
        if($id-cin@($roles[2],$roles[3])-and-not[string]::IsNullOrEmpty($Shape)){
            Assert-KmcChunk7Boolean $r.inGame $false;Assert-KmcChunk7Boolean $r.directlyControllable $false
            Assert-KmcChunk7Boolean $r.inParty ($Shape-ceq'field');Assert-KmcChunk7Boolean $r.character ($Shape-ceq'field'-and$id-ceq$roles[2]);Assert-KmcChunk7Boolean $r.remote ($Shape-ceq'remote'-and$id-ceq$roles[2])
        }else{
            # Every other captured actor keeps the fixture's exact roster contract (in the field, in the game, not remote).
            Assert-KmcChunk7Boolean $r.inParty $true;Assert-KmcChunk7Boolean $r.remote $false;Assert-KmcChunk7Boolean $r.inGame $true
        }
    }
    # The recorded admission must equal the admission derived from this row's roster, exactly.
    if((ConvertTo-KmcChunk7Canonical @($derived))-cne(ConvertTo-KmcChunk7Canonical @($Admitted))){throw 'Recorded capital native remote companions differ from the observed roster.'}
}
# Inherited custody normalized from the capital's out-of-scene membership back to the full field contract.
function ConvertFrom-KmcChunk7CapitalInheritedSnapshot($Request,$Snapshot) {
    $s=$Snapshot|ConvertTo-Json -Depth 40|ConvertFrom-Json
    if($null-eq$s.PSObject.Properties['capitalOutOfScene']){return $s}
    $roles=@(Get-KmcChunk7OwnedPairIds $Request);$record=$s.capitalOutOfScene
    Assert-KmcChunk7CapitalOutOfSceneRecord $record $Request $false
    $out=@($roles[2],$roles[3]);$field=$record.shape-ceq'field'
    # Batch197 r58: the recorded native remote companions are out of the field and remote, exactly.
    $remote=@(@($record.admittedNativeRemoteCompanions)|ForEach-Object{$_.id})
    $expected=@($s.partyIds|Where-Object{($field-or$_-cnotin$out)-and$_-cnotin$remote}|Sort-Object -CaseSensitive)
    if((ConvertTo-KmcChunk7Canonical @($s.currentPartyIds))-cne(ConvertTo-KmcChunk7Canonical $expected)){throw 'Capital inherited field roster differs beyond the out-of-scene pair and admitted native remote companions.'}
    if((@($s.currentCharacterIds)-ccontains$roles[2])-ne$field){throw 'Capital inherited character roster differs from its out-of-scene shape.'}
    foreach($id in $remote){if(@($s.currentCharacterIds)-ccontains$id){throw 'Capital inherited character roster keeps an admitted native remote companion.'}}
    [string[]]$remoteExpected=@(@($(if($record.shape-ceq'remote'){$roles[2]}))+@($remote));[Array]::Sort($remoteExpected,[StringComparer]::Ordinal)
    if((ConvertTo-KmcChunk7Canonical @($s.remoteCapturedIds))-cne(ConvertTo-KmcChunk7Canonical @($remoteExpected))){throw 'Capital remote companions differ from the out-of-scene shape and admitted native remote companions.'}
    $s.currentPartyIds=@($s.partyIds|Sort-Object -CaseSensitive)
    $s.currentCharacterIds=@(@($s.currentCharacterIds)+@($(if(-not$field){$roles[2]}))+@($remote)|Sort-Object -CaseSensitive)
    $s.remoteCapturedIds=@();$s.PSObject.Properties.Remove('capitalOutOfScene');$s
}
function Assert-KmcChunk7CapitalInheritedEvidence($Request,$Observations,[bool]$AdmittedInCapital) {
    Assert-KmcChunk7OwnedPairsDescriptor $Request
    $admitted=$Observations.inheritedFixture;$cleanup=$Observations.inheritedCleanup
    if(($null-ne$admitted.PSObject.Properties['capitalOutOfScene'])-ne$AdmittedInCapital-or$null-eq$cleanup.PSObject.Properties['capitalOutOfScene']){throw 'Capital inherited custody lacks or invents its out-of-scene record.'}
    $a=ConvertFrom-KmcChunk7CapitalInheritedSnapshot $Request $admitted;$b=ConvertFrom-KmcChunk7CapitalInheritedSnapshot $Request $cleanup
    Assert-KmcChunk7InheritedSnapshot $Request $a 'admitted';Assert-KmcChunk7InheritedSnapshot $Request $b 'cleanup'
    foreach($key in @('partyIds','currentPartyIds','currentCharacterIds','remoteCapturedIds','arrangement','pairs','identities')){if((ConvertTo-KmcChunk7Canonical $a.$key)-cne(ConvertTo-KmcChunk7Canonical $b.$key)){throw 'Inherited native roster/facts/progression changed across execution and cleanup.'}}
    if($AdmittedInCapital-and(ConvertTo-KmcChunk7Canonical $admitted.capitalOutOfScene)-cne(ConvertTo-KmcChunk7Canonical $cleanup.capitalOutOfScene)){throw 'Capital out-of-scene custody changed across execution and cleanup.'}
    Assert-KmcChunk7Boolean $Observations.cleanup.inheritedPetsPreserved $true;Assert-KmcChunk7Boolean $Observations.cleanup.nativeProgressionRequiresExternalWorkingRestoration $false
}
# Row 58: cold two-pair load, one declared transfer into the reviewed capital, one Manual written there.
function Assert-KmcChunk7CapitalArrivalObservation($Request,$Artifact) {
    if($Artifact.status-cne'PASS'){throw 'Capital arrival artifact failed.'}
    if(-not(Test-KmcChunk7OwnedPairsScenario $Request.scenario)-or$Request.ownedPairs.case-cne'persistence-load'-or-not(Test-KmcChunk7CapitalArrivalCase $Request.persistenceCase)){throw 'Capital arrival reader belongs only to its owned cold producer.'}
    Assert-KmcChunk7OwnedPairsDescriptor $Request
    $o=$Artifact.observations
    $required=@('checkpoint','cold','sourceWorkingSha256','inheritedManualSha256','processId','snapshots','inputs','movements','nativeProgressionRequiresExternalWorkingRestoration','ordinaryRespecProven','combatDebtProof','nativeSchema','nativeCapacity','inheritedFixture','coldRestoration','loadedArchive','loadedDescriptor','capitalArrival','ownedOutputAdmission','saveRequested','ownedOutputCompletion','nativeWrite','inheritedCleanup','cleanup','elapsedSeconds','nativeShellLifecycle')
    Assert-KmcExactProperties $o $required 'capital arrival observations'
    Assert-KmcChunk7CapitalInheritedEvidence $Request $o $false
    Assert-KmcBootstrapInventoryString $o.checkpoint $Request.persistenceCase;Assert-KmcChunk7Boolean $o.cold $true
    if($null-ne$o.sourceWorkingSha256-or$o.nativeSchema-ne3-or-not(Test-KmcChunk7NativePairCapacity $o.nativeCapacity)-or-not(Test-KmcExactJsonInteger $o.processId)-or$o.processId-le0){throw 'Capital arrival capability or process identity differs.'}
    Assert-KmcBootstrapInventoryString $o.inheritedManualSha256 $Request.ownedPairs.sourceArchiveSha256
    foreach($key in @('nativeProgressionRequiresExternalWorkingRestoration','ordinaryRespecProven','combatDebtProof')){Assert-KmcChunk7Boolean $o.$key $false}
    if(@($o.inputs).Count-ne0-or@($o.movements).Count-ne0){throw 'Capital arrival issued a Mount, Dismount or movement input.'}
    # The cold two-pair start, exactly as the x4s Manual's ordinary cold consumer would read it.
    $sourceRequest=Get-KmcChunk7CapitalInputRequest $Request;$loaded=$o.loadedArchive
    if($null-eq$loaded-or$null-eq$loaded.PSObject.Properties['AreaId']-or$loaded.AreaId-ceq(Get-KmcChunk7CapitalArrivalArea)){throw 'Capital arrival input was already in the capital.'}
    Assert-KmcChunk7PersistenceArchive $loaded $sourceRequest
    if((ConvertTo-KmcChunk7Canonical $o.loadedDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)){throw 'Capital arrival loaded another Manual.'}
    $cold=Get-KmcChunk7CapitalSnapshot $o 'cold-restored-before-input';Assert-KmcChunk7PersistenceSnapshot $cold $loaded 'cold-restored-before-input'
    if($cold.semanticRestores-ne4-or$cold.presentationRestores-ne2-or$cold.nativePostLoads-ne4-or$cold.snapshots-ne0-or$cold.nativeCastRequests-ne0-or$cold.archivePhase-cne'all-pair-presentation-restored'){throw 'Capital arrival cold start replayed input or restored inexactly.'}
    $roles=Get-KmcChunk7OwnedPairIds $Request;$all=@($roles)
    $main=@($loaded.Pairs|Where-Object{$_.Rider.Id-ceq$roles[0]})[0];$retired=@($loaded.Pairs|Where-Object{$_.Rider.Id-ceq$roles[2]})[0]
    Assert-KmcChunk7CapitalRestorationProbe $o.coldRestoration $Request $all $all
    $c=$o.capitalArrival
    $fields=@('contract','enterPoint','sourceArea','destinationArea','autoSaveMode','destinationIsCapital','mainPairId','retiredPairId','retiredRiderName','retiredMountName','transferRequests','nativeAutosaveRequested','before','dispatchReturned','arrival','restoration','umm','after','loadingTicks','suspensionObserved','retiredOutOfScene')
    if($null-ne$c.PSObject.Properties['awaitingNativeReady']){$fields+='awaitingNativeReady'}
    Assert-KmcExactProperties $c $fields 'capital arrival row'
    Assert-KmcChunk7CapitalArrivalShape $Request $c $loaded $o.coldRestoration.events
    # Forensic records added after batch196 r58: the exact out-of-scene custody and every actor's roster membership.
    Assert-KmcChunk7CapitalOutOfSceneRecord $c.retiredOutOfScene $Request $true
    Assert-KmcChunk7CapitalRoster $c.before $Request $null;Assert-KmcChunk7CapitalRoster $c.after $Request $c.retiredOutOfScene.shape @($c.retiredOutOfScene.admittedNativeRemoteCompanions) $loaded
    if((ConvertTo-KmcChunk7Canonical $o.inheritedCleanup.capitalOutOfScene)-cne(ConvertTo-KmcChunk7Canonical $c.retiredOutOfScene)){throw 'Capital out-of-scene custody changed between arrival and cleanup.'}
    $mainSlots=@($loaded.Slots|Where-Object ActorId -CEQ $roles[0])
    $settled=Get-KmcChunk7CapitalSnapshot $o 'capital-arrival-settled';Assert-KmcChunk7CapitalOneOwner $settled $main
    # Every actor's native debt across the transfer, against the cold-loaded archive.
    Assert-KmcChunk7CapitalSnapshotDebt $settled $loaded
    if($settled.semanticRestores-ne4-or$settled.presentationRestores-ne2-or$settled.nativePostLoads-ne4-or$settled.snapshots-ne0-or$settled.nativeCastRequests-ne0){throw 'Capital arrival replayed restoration, a snapshot or native input.'}
    # The genuine native Manual written in the capital, read back with exactly one mounted pair.
    $w=$o.nativeWrite
    Assert-KmcExactProperties $w @('path','sha256','length','nativeType','nativeName','nativeCallback','operation','snapshotCount','area','saveAllowedAfterWrite','archive') 'capital native write'
    Assert-KmcBootstrapInventoryString $w.nativeType 'Manual';Assert-KmcBootstrapInventoryString $w.nativeName (Get-KmcChunk7CapitalArrivalManualName);Assert-KmcBootstrapInventoryString $w.operation 'None'
    Assert-KmcBootstrapInventoryString $w.area (Get-KmcChunk7CapitalArrivalArea);Assert-KmcChunk7Boolean $w.nativeCallback $true;Assert-KmcChunk7Boolean $w.saveAllowedAfterWrite $true
    Assert-KmcOrdinaryInteger $w.snapshotCount 1 1;Assert-KmcOrdinaryInteger $w.length 1 268435456
    if($w.sha256-isnot[string]-or$w.sha256-cnotmatch'^[0-9a-f]{64}$'-or$w.sha256-ceq$Request.persistenceLoad.sha256){throw 'Capital Manual identity is malformed or aliases its input.'}
    Assert-KmcChunk7CapitalArchive $w.archive $Request $main $mainSlots $loaded
    # PowerShell names are case-insensitive: the save row must never shadow $Request.
    $save=$o.saveRequested;Assert-KmcExactProperties $save @('nativeType','nativeName','gameTicks','snapshotCount','nativeSaveAllowed','newSlot') 'capital save request'
    Assert-KmcBootstrapInventoryString $save.nativeType 'Manual';Assert-KmcBootstrapInventoryString $save.nativeName (Get-KmcChunk7CapitalArrivalManualName);Assert-KmcOrdinaryInteger $save.snapshotCount 0 0
    Assert-KmcChunk7Boolean $save.nativeSaveAllowed $true;Assert-KmcChunk7Boolean $save.newSlot $true
    Assert-KmcChunk7OwnedOutputEvidence $Request $o
    $written=Get-KmcChunk7CapitalSnapshot $o 'native-write-complete';Assert-KmcChunk7CapitalOneOwner $written $main
    Assert-KmcChunk7CapitalSnapshotDebt $written $w.archive
    if($written.snapshots-ne1-or$written.nativeCastRequests-ne0){throw 'Capital Manual replayed native input or saved at another boundary.'}
    foreach($field in @('pairsClear','controlsSettled','saveCustodyClear','selectionRestored','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}
    Assert-KmcChunk7Boolean $o.cleanup.sourceHorseRemoved $false;Assert-KmcChunk7Boolean $o.cleanup.ordinaryRespecProven $false
    if(@($o.cleanup.world.owners).Count-ne0){throw 'Capital arrival cleanup retained native custody.'}
}
# Row 59: a separate process cold-loads row 58's capital Manual.
function Assert-KmcChunk7CapitalColdObservation($Request,$Artifact) {
    if($Artifact.status-cne'PASS'){throw 'Capital cold artifact failed.'}
    if(-not(Test-KmcChunk7OwnedPairsScenario $Request.scenario)-or$Request.ownedPairs.case-cne'persistence-load'-or-not(Test-KmcChunk7CapitalColdCase $Request.persistenceCase)){throw 'Capital cold reader belongs only to its owned cold consumer.'}
    Assert-KmcChunk7OwnedPairsDescriptor $Request
    $o=$Artifact.observations
    $required=@('checkpoint','cold','sourceWorkingSha256','inheritedManualSha256','processId','snapshots','inputs','movements','nativeProgressionRequiresExternalWorkingRestoration','ordinaryRespecProven','combatDebtProof','nativeSchema','nativeCapacity','inheritedFixture','coldRestoration','loadedArchive','loadedDescriptor','capitalColdBoundary','capitalCold','inheritedCleanup','cleanup','elapsedSeconds','nativeShellLifecycle')
    Assert-KmcExactProperties $o $required 'capital cold observations'
    Assert-KmcChunk7CapitalInheritedEvidence $Request $o $true
    Assert-KmcBootstrapInventoryString $o.checkpoint $Request.persistenceCase;Assert-KmcChunk7Boolean $o.cold $true
    if($null-ne$o.sourceWorkingSha256-or$o.nativeSchema-ne3-or-not(Test-KmcChunk7NativePairCapacity $o.nativeCapacity)-or-not(Test-KmcExactJsonInteger $o.processId)-or$o.processId-le0){throw 'Capital cold capability or process identity differs.'}
    Assert-KmcBootstrapInventoryString $o.inheritedManualSha256 $Request.ownedPairs.sourceArchiveSha256
    foreach($key in @('nativeProgressionRequiresExternalWorkingRestoration','ordinaryRespecProven','combatDebtProof')){Assert-KmcChunk7Boolean $o.$key $false}
    if(@($o.inputs).Count-ne0-or@($o.movements).Count-ne0){throw 'Capital cold consumer issued a Mount, Dismount or movement input.'}
    if((ConvertTo-KmcChunk7Canonical $o.loadedDescriptor)-cne(ConvertTo-KmcChunk7Canonical $Request.persistenceLoad)){throw 'Capital cold consumer loaded another Manual.'}
    $roles=Get-KmcChunk7OwnedPairIds $Request;$loaded=$o.loadedArchive
    if($loaded.Pairs-isnot[Array]-or$loaded.Pairs.Count-ne1){throw 'Capital cold archive does not hold exactly one mounted pair.'}
    $main=$loaded.Pairs[0]
    if($main.Rider.Id-cne$roles[0]-or$main.Mount.Id-cne$roles[1]){throw 'Capital cold archive pair is not the main character and its pet.'}
    $snapshot=Get-KmcChunk7CapitalSnapshot $o 'cold-restored-before-input';$c=$o.capitalCold
    if($snapshot.owners-isnot[Array]-or$snapshot.owners.Count-ne1){throw 'Capital snapshot does not hold exactly one mounted owner.'}
    $live=$snapshot.owners[0]
    if($live.riderId-cne$roles[0]-or$live.mountId-cne$roles[1]-or$live.state-cne'Active'-or([Guid]$live.ownerId)-ne([Guid]$main.PairId)-or$live.generation-ne$main.Generation){throw 'Capital cold archive pair differs from the live restored owner.'}
    $row=@($o.inheritedFixture.pairs|Where-Object riderId -CEQ $roles[0])
    if($row.Count-ne1-or$row[0].mountId-cne$roles[1]){throw 'Capital cold archive rider is outside inherited ownership.'}
    $liveSlots=@($c.world.slots)
    if($liveSlots.Count-ne2-or@($liveSlots|Where-Object ActorId -CNE $roles[0]).Count-ne0-or(@($liveSlots|ForEach-Object Kind|Sort-Object)-join',')-cne'2,3'){throw 'Capital cold live controls are not exactly the main pair bindings.'}
    # Independent references: the live owner (identity, generation), the inherited profile, the live
    # controls and the fixed archive policy. The archive is never its own reference.
    $reference=[pscustomobject]@{PairId=$main.PairId;Generation=$live.generation;ProfileId=$row[0].profileId;Rider=[pscustomobject]@{Id=$roles[0]};Mount=[pscustomobject]@{Id=$roles[1]}}
    Assert-KmcChunk7CapitalArchive $loaded $Request $reference $liveSlots ([pscustomobject]@{Policy='rider-principal-distinct-native-v1';RulesId='crpg-transport-v1'})
    Assert-KmcChunk7CapitalSnapshotDebt $snapshot $loaded
    Assert-KmcChunk7CapitalRestorationProbe $o.coldRestoration $Request @($roles[0],$roles[1]) @($roles)
    foreach($event in @($o.coldRestoration.events|Where-Object kind -CEQ 'restore-debt-after')){
        $saved=@($loaded.Pairs|ForEach-Object{$_.Rider;$_.Mount}|Where-Object Id -CEQ $event.actorId)
        if($saved.Count-ne1-or$event.restorePhase-cne'validated-before-native-mutation'-or(ConvertTo-KmcChunk7Canonical $event.debt)-cne(ConvertTo-KmcChunk7Canonical $saved[0])){throw 'Capital cold post-restore debt differs from the validated archive.'}
    }
    $mainSlots=@($loaded.Slots)
    if($snapshot.semanticRestores-ne2-or$snapshot.presentationRestores-ne1-or$snapshot.nativePostLoads-ne2-or$snapshot.snapshots-ne0-or$snapshot.nativeCastRequests-ne0-or
       $snapshot.archivePhase-cne'all-pair-presentation-restored'-or$snapshot.controls.DuplicateFactCount-ne0-or$snapshot.controls.ManagedHotbarSlotCount-ne2){throw 'Capital cold restore replayed input or restored another actor.'}
    Assert-KmcExactProperties $c @('contract','producerRunId','producerArtifactSha256','mainPairId','mainGeneration','unmountedRiderId','unmountedMountId','registryOwners','activeOwners','outOfScene','world') 'capital cold row'
    Assert-KmcChunk7CapitalOutOfSceneRecord $c.outOfScene $Request $false
    if((ConvertTo-KmcChunk7Canonical $o.inheritedFixture.capitalOutOfScene)-cne(ConvertTo-KmcChunk7Canonical $c.outOfScene)){throw 'Capital cold out-of-scene custody differs from its bind.'}
    Assert-KmcChunk7CapitalRoster $c.world $Request $c.outOfScene.shape @($c.outOfScene.admittedNativeRemoteCompanions) $loaded
    foreach($pair in @(@('contract','chunk7-capital-arrival-cold-v1'),@('producerRunId',$Request.ownedPairs.checkpointSourceRunId),@('producerArtifactSha256',$Request.ownedPairs.checkpointSourceArtifactSha256),@('mainPairId',$main.PairId),@('unmountedRiderId',$roles[2]),@('unmountedMountId',$roles[3]))){Assert-KmcBootstrapInventoryString $c.($pair[0]) $pair[1]}
    if($c.mainGeneration-ne$main.Generation){throw 'Capital cold owner generation differs.'}
    Assert-KmcOrdinaryInteger $c.registryOwners 1 1;Assert-KmcOrdinaryInteger $c.activeOwners 1 1
    $w=$c.world;Assert-KmcChunk7CapitalWorldRow $w 'cold-restored-in-capital' $true
    foreach($pair in @(@('suspensions',0),@('resumes',0),@('retired',0),@('failedPairs',0),@('refusedTransfers',0),@('activeOwners',1))){Assert-KmcOrdinaryInteger $w.($pair[0]) $pair[1] $pair[1]}
    Assert-KmcChunk7Boolean $w.saveAllowed $true;Assert-KmcChunk7CapitalOneOwner $w $main
    if((ConvertTo-KmcChunk7Canonical @($w.slots|Sort-Object Index))-cne(ConvertTo-KmcChunk7Canonical @($mainSlots|Sort-Object Index))){throw 'Capital cold controls are not exactly the main pair bindings.'}
    foreach($view in $w.views){
        $isMain=$view.id-cin@($roles[0],$roles[1]);Assert-KmcChunk7Boolean $view.directlyControllable $isMain;Assert-KmcChunk7Boolean $view.inGame $isMain
        # An out-of-scene unit may have no spawned view; its recorded presence must agree with the custody record.
        $present=if($isMain){$true}else{[bool]@($c.outOfScene.actors|Where-Object id -CEQ $view.id)[0].viewPresent}
        if(($view.viewId-ne0)-ne$present){throw 'Capital cold actor view presence differs from its recorded custody.'}
        if($view.id-ceq$roles[0]){if($view.riderAgents-ne1-or$view.agentOverride-eq0){throw 'Main rider lacks restored movement authority.'}}
        elseif($view.riderAgents-ne0-or$view.agentOverride-ne0){throw 'The unmounted pair or the main mount retains movement authority.'}
    }
    foreach($field in @('pairsClear','controlsSettled','saveCustodyClear','selectionRestored','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $o.cleanup.$field $true}
    Assert-KmcChunk7Boolean $o.cleanup.sourceHorseRemoved $false;Assert-KmcChunk7Boolean $o.cleanup.ordinaryRespecProven $false
    if(@($o.cleanup.world.owners).Count-ne0){throw 'Capital cold cleanup retained native custody.'}
}
# Separate-process proof: row 59 restored exactly the archive row 58 wrote and read back.
function Assert-KmcChunk7CapitalColdSource($Request,$ProducerArtifact,$ColdArtifact) {
    Assert-KmcChunk7CapitalColdObservation $Request $ColdArtifact
    $p=$ProducerArtifact;$prior=$p.observations;$cold=$ColdArtifact.observations
    Assert-KmcBootstrapInventoryString $p.runId $Request.ownedPairs.checkpointSourceRunId;Assert-KmcBootstrapInventoryString $p.scenario 'chunk7-owned-two-pair-campaign';Assert-KmcBootstrapInventoryString $p.status 'PASS'
    Assert-KmcBootstrapInventoryString $prior.checkpoint 'multi-exploration-capital-arrival-manual';Assert-KmcChunk7Boolean $prior.cold $true
    Assert-KmcOrdinaryInteger $prior.processId 1 ([int]::MaxValue);Assert-KmcOrdinaryInteger $cold.processId 1 ([int]::MaxValue)
    if($prior.processId-eq$cold.processId-or$prior.nativeWrite.length-ne$Request.persistenceLoad.length-or
       (ConvertTo-KmcChunk7Canonical $prior.nativeWrite.archive)-cne(ConvertTo-KmcChunk7Canonical $cold.loadedArchive)){throw 'Capital cold process did not restore the exact archive the capital Manual wrote.'}
    Assert-KmcBootstrapInventoryString $prior.nativeWrite.sha256 $Request.persistenceLoad.sha256
    foreach($key in @('partyIds','currentCharacterIds','remoteCapturedIds','arrangement')){if((ConvertTo-KmcChunk7Canonical $prior.inheritedCleanup.$key)-cne(ConvertTo-KmcChunk7Canonical $cold.inheritedFixture.$key)){throw 'Capital cold source changed full-field ancestry or progression.'}}
    for($i=0;$i-lt2;$i++){foreach($key in @('riderId','mountId','sourceGuid','rank','profileId')){if((ConvertTo-KmcChunk7Canonical $prior.inheritedCleanup.pairs[$i].$key)-cne(ConvertTo-KmcChunk7Canonical $cold.inheritedFixture.pairs[$i].$key)){throw 'Capital cold source changed inherited role or fact provenance.'}}}
    foreach($key in @('retiredPairId','mainPairId')){if($prior.capitalArrival.$key-isnot[string]){throw 'Capital producer row lacks its pair identities.'}}
    # The consumer's pinned lineage area is exactly the area the producer departed from.
    $departure=$prior.capitalArrival.sourceArea;$lineage=$Request.ownedPairs.sourceArea
    if($departure-isnot[string]-or$lineage-isnot[string]-or$departure-cne$lineage){throw 'Capital cold lineage area differs from the producer departure area.'}
    # Batch197 r58: the cold load keeps exactly the producer's admitted native remote companions, by identity.
    if((@($prior.capitalArrival.retiredOutOfScene.admittedNativeRemoteCompanions|ForEach-Object{$_.id})-join'|')-cne(@($cold.capitalCold.outOfScene.admittedNativeRemoteCompanions|ForEach-Object{$_.id})-join'|')){throw 'Capital cold native remote companions differ from the producer admission.'}
    Assert-KmcBootstrapInventoryString $cold.capitalCold.mainPairId $prior.capitalArrival.mainPairId
}
