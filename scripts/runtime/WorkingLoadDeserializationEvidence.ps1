# Original, bounded evidence validation. This grants no load, resolution,
# compatibility or restoration authority. The runtime result remains separate.
function Assert-KmcWorkingObservationObject($Value,[string[]]$Names,[string]$Label) {
    if($Value -isnot [pscustomobject]){throw "$Label must be an actual JSON object."}
    Assert-KmcExactProperties $Value $Names $Label
}
function Assert-KmcWorkingObservationInt($Value,[long]$Min,[long]$Max,[string]$Label) {
    if(-not(Test-KmcManifestJsonInteger $Value)-or[decimal]$Value-lt$Min-or[decimal]$Value-gt$Max){throw "$Label must be an exact bounded integer."}
}
function Assert-KmcWorkingObservationBool($Value,[string]$Label) { if($Value-isnot[bool]){throw "$Label must be a Boolean."} }
function Assert-KmcWorkingObservationText($Value,[int]$Max,[bool]$Nullable,[string]$Label) {
    if($null-eq$Value-and$Nullable){return}
    if($Value-isnot[string]-or$Value.Length-gt$Max-or$Value-cmatch'[\x00-\x1f\x7f-\x9f]'){throw "$Label must be bounded scalar text."}
    for($i=0;$i-lt$Value.Length;$i++) {
        if([char]::IsSurrogate($Value[$i])) {
            if(-not[char]::IsHighSurrogate($Value[$i])-or$i+1-ge$Value.Length-or-not[char]::IsLowSurrogate($Value[$i+1])){throw "$Label has an incomplete surrogate."}
            $i++
        }
    }
}
function Assert-KmcWorkingLoadDeserializationValue($Artifact,$Request,[switch]$Pending) {
    Assert-KmcWorkingObservationObject $Artifact @('schemaVersion','evidenceKind','runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','createdAtUtc','status','observations','errors') 'Working observation artifact'
    Assert-KmcWorkingObservationInt $Artifact.schemaVersion 1 1 'artifact schema'
    $kind=if($Pending){'working-load-deserialization-pending'}else{'working-load-deserialization-evidence'}
    foreach($name in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')) {
        if($Artifact.$name-isnot[string]-or$Request.$name-isnot[string]-or$Artifact.$name-cne$Request.$name){throw "Working observation $name provenance differs."}
    }
    if($Artifact.evidenceKind-isnot[string]-or$Artifact.evidenceKind-cne$kind-or$Artifact.status-isnot[string]-or$Artifact.status-cnotin@('PASS','FAIL')-or($Pending-and$Artifact.status-cne'FAIL')){throw 'Working observation kind/status is invalid.'}
    if($Request.scenario-cne'export-mounted-contracts'-or$Request.observeWorkingDeserialization-isnot[bool]-or-not$Request.observeWorkingDeserialization){throw 'Working observation request is outside the exact opt-in.'}
    if($Artifact.dllSha256-cnotmatch'^[0-9a-f]{64}$'-or$Artifact.dllMvid-cnotmatch'^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'){throw 'Working observation binary pins are malformed.'}
    $date=[DateTimeOffset]::MinValue
    if($Artifact.createdAtUtc-isnot[string]-or-not[DateTimeOffset]::TryParse($Artifact.createdAtUtc,[ref]$date)){throw 'Working observation timestamp is malformed.'}
    if($Artifact.errors-isnot[Array]-or$Artifact.errors.Count-gt1){throw 'Working observation errors are not the bounded array.'}
    foreach($errorText in $Artifact.errors){Assert-KmcWorkingObservationText $errorText 256 $false 'observation error'}
    $o=$Artifact.observations
    $extra=if($Pending){@('firstPendingAtSeconds','pollDeadlineSeconds')}else{@('nativeLoadCount','nativeWriteCount','runtimeStatusAtPublication')}
    Assert-KmcWorkingObservationObject $o (@('formatVersion','source','gameMvid','jsonMvid','observationOnly','nativeResolutionProven','atomicityProven','afterLoadCallbackObserved','fixtureIdentityVerified','nativeBusyAtCapture','observerReleased','loaderObserved','taskObserved','taskSettled','taskSucceeded','retainedNativeIdentityCount','faults','events')+$extra) 'Working observations'
    Assert-KmcWorkingObservationInt $o.formatVersion 1 1 'observation version'
    Assert-KmcWorkingObservationObject $o.source @('internalName','fileName','sha256') 'Working observation source'
    foreach($name in @('internalName','fileName','sha256')){if($o.source.$name-isnot[string]-or$o.source.$name-cne$Request.fixture.working.$name){throw "Working observation source $name differs."}}
    if($o.source.internalName-cne'KMC_AUTOMATION_WORKING'-or$o.source.sha256-cnotmatch'^[0-9a-f]{64}$'-or$o.source.fileName-cnotmatch'^Manual_[0-9]+_KMC_AUTOMATION_WORKING\.zks$'){throw 'Working observation source is not the exact qualified Working descriptor.'}
    if($o.gameMvid-isnot[string]-or$o.gameMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'-or$o.jsonMvid-isnot[string]-or$o.jsonMvid-cne'4f213745-d60e-425f-b763-df319c826ddf'){throw 'Working observation native contract changed.'}
    foreach($name in @('observationOnly','nativeResolutionProven','atomicityProven','afterLoadCallbackObserved','fixtureIdentityVerified','nativeBusyAtCapture','observerReleased','loaderObserved','taskObserved','taskSettled','taskSucceeded')){Assert-KmcWorkingObservationBool $o.$name $name}
    if(-not$o.observationOnly-or$o.nativeResolutionProven-or$o.atomicityProven-or($o.taskSucceeded-and-not$o.taskSettled)-or($o.taskSettled-and-not$o.taskObserved)-or($o.taskObserved-and-not$o.loaderObserved)){throw 'Working observation claims unsupported authority or task facts.'}
    Assert-KmcWorkingObservationInt $o.retainedNativeIdentityCount 0 128 'retained identity count'
    if($o.observerReleased-and($o.nativeBusyAtCapture-or$o.retainedNativeIdentityCount-ne0-or($o.loaderObserved-and-not$o.taskSucceeded))){throw 'Working observation release lacks its exact task/native custody boundary.'}
    if($Pending){if($o.observerReleased){throw 'Pending Working evidence cannot claim verified release.'};foreach($name in $extra){$n=$o.$name;if(($n-isnot[double]-and$n-isnot[decimal]-and-not(Test-KmcManifestJsonInteger $n))-or[double]::IsNaN([double]$n)-or[double]::IsInfinity([double]$n)-or$n-lt0){throw 'Working pending times must be finite nonnegative numeric scalars.'}};if($o.pollDeadlineSeconds-le0){throw 'Working pending deadline is absent.'}}
    else {
        Assert-KmcWorkingObservationInt $o.nativeLoadCount 0 1 'Working native load count'; Assert-KmcWorkingObservationInt $o.nativeWriteCount 0 0 'Working native write count'
        if($o.nativeLoadCount-ne0-and-not$o.taskSucceeded){throw 'An issued native load cannot release on missing observer provenance.'}
        if(-not$o.observerReleased-or$o.runtimeStatusAtPublication-isnot[string]-or$o.runtimeStatusAtPublication-cne$Artifact.status){throw 'Final Working observation lacks release/status binding.'}
        if($Artifact.status-ceq'PASS'-and(-not$o.afterLoadCallbackObserved-or-not$o.fixtureIdentityVerified-or-not$o.taskSucceeded-or$o.nativeLoadCount-ne1)){throw 'Working observation PASS lacks original load completion.'}
    }
    $knownFaults=@('ForeignOwner','ForeignCallback','BadOrder','Duplicate','ReadFailure','EventLimit','IdentityLimit','BusyRelease','WrongReleaseThread','UnpatchFailure')
    if($o.faults-isnot[Array]-or$o.faults.Count-gt$knownFaults.Count){throw 'Working fault array is malformed.'}
    $faults=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($fault in $o.faults){if($fault-isnot[string]-or$fault-cnotin$knownFaults-or-not$faults.Add($fault)){throw 'Working fault token is malformed or duplicated.'}}
    if($o.events-isnot[Array]-or$o.events.Count-gt32){throw 'Working event array exceeds its fixed bound.'}
    $states=@{Player=@{serializer=$false;returned=$false;assigned=$false;caught=$false;value=0};Party=@{serializer=$false;returned=$false;assigned=$false;caught=$false;value=0}}
    $seenLoader=$false;$seenTask=$false;$seenCross=$false;$seenBefore=$false;$seenAfter=$false;$seq=0;$beforeScene=$null;$beforeSceneIdentity=$null
    foreach($event in $o.events){
        Assert-KmcWorkingObservationObject $event @('sequence','threadId','phase','lane','valueIdentity','scene','serializer') 'Working event'
        $seq++;Assert-KmcWorkingObservationInt $event.sequence $seq $seq 'event sequence';Assert-KmcWorkingObservationInt $event.threadId 1 ([int]::MaxValue) 'thread identity';Assert-KmcWorkingObservationInt $event.valueIdentity -1 128 'event identity'
        if($event.phase-isnot[string]-or$event.phase-cnotin@('loader-created','task-returned','serializer-returned','object-returned','object-assigned','native-caught-after-log','cross-scene-assigned','scene-postload-before','scene-postload-after')){throw 'Working event phase is outside the closed protocol.'}
        if($event.valueIdentity-eq0-and$event.phase-cnotin@('object-returned','object-assigned','native-caught-after-log')){throw 'Working nonnull native observation has a null identity.'}
        $lanePhase=$event.phase-cin@('serializer-returned','object-returned','object-assigned','native-caught-after-log')
        if($lanePhase){if($event.lane-isnot[string]-or$event.lane-cnotin@('Player','Party')-or-not$seenLoader){throw 'Working lane event lacks exact loader/lane.'};$s=$states[$event.lane]}
        elseif($null-ne$event.lane){throw 'Working non-lane event has a lane.'}
        switch -CaseSensitive ($event.phase){
            'loader-created' {if($seenLoader-or$seq-ne1){throw 'Working loader order changed.'};$seenLoader=$true}
            'task-returned' {if(-not$seenLoader-or$seenTask){throw 'Working task order changed.'};$seenTask=$true}
            'serializer-returned' {if($s.serializer-or$s.caught){throw 'Working serializer order changed.'};$s.serializer=$true}
            'object-returned' {if(-not$s.serializer-or$s.returned-or$s.caught){throw 'Working return order changed.'};$s.returned=$true;$s.value=$event.valueIdentity}
            'object-assigned' {if(-not$s.returned-or$s.assigned-or$s.caught-or$event.valueIdentity-ne$s.value){throw 'Working assignment differs from its observed return.'};$s.assigned=$true}
            'native-caught-after-log' {if($s.caught-or$event.valueIdentity-ne0){throw 'Working caught path duplicated.'};$s.caught=$true}
            'cross-scene-assigned' {if(-not$states.Player.assigned-or-not$states.Party.assigned-or$seenCross-or$event.valueIdentity-ne$states.Party.value){throw 'Working cross-scene assignment lacks both native returns.'};$seenCross=$true}
            'scene-postload-before' {if(-not$seenTask-or$seenBefore){throw 'Working scene observation lacks actual Task provenance.'};$seenBefore=$true;$beforeSceneIdentity=$event.valueIdentity}
            'scene-postload-after' {if(-not$seenBefore-or$seenAfter-or$event.valueIdentity-ne$beforeSceneIdentity){throw 'Working scene after lacks its exact before identity.'};$seenAfter=$true}
        }
        $scenePhase=$event.phase-cin@('cross-scene-assigned','scene-postload-before','scene-postload-after')
        if(-not$scenePhase-and$null-ne$event.scene){throw 'Working scene data is attached to another phase.'}
        if($null-ne$event.scene){
            Assert-KmcWorkingObservationScene $event.scene
            if($event.scene.sceneIdentity-ne$event.valueIdentity-or$event.scene.sceneIdentity-eq0-or$event.scene.playerIdentity-eq0){throw 'Working scene snapshot differs from its nonnull observed references.'}
            if($event.phase-ceq'cross-scene-assigned'-and$event.scene.playerIdentity-ne$states.Player.value){throw 'Working cross-scene assignment differs from the two observed native returns.'}
            if($event.phase-ceq'scene-postload-before'){$beforeScene=$event.scene}
            elseif($event.phase-ceq'scene-postload-after'-and$null-ne$beforeScene){foreach($name in @('playerIdentity','sceneIdentity')){if($event.scene.$name-ne$beforeScene.$name){throw 'Working scene after replaced a retained before reference.'}}}
        }
        if($event.phase-cne'serializer-returned'-and$null-ne$event.serializer){throw 'Working serializer data is attached to another phase.'}
        if($null-ne$event.serializer){Assert-KmcWorkingObservationSerializer $event.serializer;if($event.serializer.identity-ne$event.valueIdentity){throw 'Working serializer snapshot differs from its observed reference.'}}
        if(($scenePhase-and$null-eq$event.scene)-or($event.phase-ceq'serializer-returned'-and$null-eq$event.serializer)){if(-not$faults.Contains('ReadFailure')){throw 'Missing snapshot lacks observed read failure.'}}
    }
    if($seenLoader-ne$o.loaderObserved-or$seenTask-ne$o.taskObserved){throw 'Working raw task/loader events disagree with custody facts.'}
    if($seenBefore-and-not$o.taskSucceeded){throw 'Working scene boundary requires successful exact outer task completion.'}
}
function Assert-KmcWorkingObservationScene($s){
    Assert-KmcWorkingObservationObject $s @('playerIdentity','sceneIdentity','currentSceneIdentity','mainId','mainIdComplete','entityCount','endEntityCount','complete','atomicityProven','matchingPrefixCount','prefix') 'Working scene'
    foreach($n in @('playerIdentity','sceneIdentity','currentSceneIdentity')){Assert-KmcWorkingObservationInt $s.$n -1 128 $n}
    foreach($n in @('entityCount','endEntityCount')){Assert-KmcWorkingObservationInt $s.$n 0 ([int]::MaxValue) $n}
    foreach($n in @('mainIdComplete','complete','atomicityProven')){Assert-KmcWorkingObservationBool $s.$n $n}
    if($s.atomicityProven){throw 'Working list snapshot cannot prove atomicity.'}
    Assert-KmcWorkingObservationText $s.mainId 128 $true 'main ID'
    if($s.mainIdComplete-and[string]::IsNullOrEmpty($s.mainId)){throw 'Absent main ID is not complete.'}
    if($s.prefix-isnot[Array]-or$s.prefix.Count-ne[Math]::Min([int]$s.entityCount,16)){throw 'Working entity prefix is not the bounded observed prefix.'}
    $matches=0
    foreach($e in $s.prefix){
        Assert-KmcWorkingObservationObject $e @('identity','id','type','textComplete','holdingSceneMatches','mainIdMatches') 'Working entity'
        Assert-KmcWorkingObservationInt $e.identity -1 128 'entity identity';Assert-KmcWorkingObservationText $e.id 128 $true 'entity ID';Assert-KmcWorkingObservationText $e.type 192 $true 'entity type'
        Assert-KmcWorkingObservationBool $e.textComplete 'entity text completeness';Assert-KmcWorkingObservationBool $e.holdingSceneMatches 'holding scene equality'
        $comparable=$s.mainIdComplete-and$e.identity-ne0-and-not[string]::IsNullOrEmpty($e.id)
        if($comparable){if($e.mainIdMatches-isnot[bool]-or$e.mainIdMatches-ne($s.mainId-ceq$e.id)){throw 'Working literal comparison differs from raw bounded IDs.'};if($e.mainIdMatches){$matches++}}
        elseif($null-ne$e.mainIdMatches){throw 'Working unavailable IDs cannot yield a comparison.'}
    }
    Assert-KmcWorkingObservationInt $s.matchingPrefixCount $matches $matches 'matching prefix count'
    if($s.complete-and(-not$s.mainIdComplete-or$s.entityCount-gt16-or$s.endEntityCount-ne$s.entityCount-or@($s.prefix|Where-Object{$_.identity-eq0-or-not$_.textComplete}).Count-ne0)){throw 'Working complete flag exceeds bounded read facts.'}
}
function Assert-KmcWorkingObservationSerializer($s){
    Assert-KmcWorkingObservationObject $s @('identity','converterCount','contractResolverType','existingReferenceResolverType','converterTypes','complete','collectionsEnablementAvailable','collectionsEnabled') 'Working serializer'
    Assert-KmcWorkingObservationInt $s.identity -1 128 'serializer identity';Assert-KmcWorkingObservationInt $s.converterCount 0 ([int]::MaxValue) 'converter count'
    foreach($n in @('contractResolverType','existingReferenceResolverType')){Assert-KmcWorkingObservationText $s.$n 192 $true $n}
    foreach($n in @('complete','collectionsEnablementAvailable')){Assert-KmcWorkingObservationBool $s.$n $n}
    if($s.converterTypes-isnot[Array]-or$s.converterTypes.Count-ne[Math]::Min([int]$s.converterCount,32)){throw 'Working converter prefix exceeds its fixed bound.'}
    foreach($name in $s.converterTypes){Assert-KmcWorkingObservationText $name 192 $true 'converter type'}
    if($s.collectionsEnablementAvailable){Assert-KmcWorkingObservationBool $s.collectionsEnabled 'converter enablement';if(@($s.converterTypes)-cnotcontains'Kingmaker.EntitySystem.Persistence.JsonUtility.CollectionConverters.CollectionsConverter'){throw 'Converter flag has no observed exact type.'}}
    elseif($null-ne$s.collectionsEnabled){throw 'Unobserved converter flag must remain unavailable.'}
    if($s.complete-and($s.converterCount-gt32-or@($s.converterTypes|Where-Object{$null-eq$_}).Count-ne0)){throw 'Working serializer completeness exceeds bounded reads.'}
}
function Assert-KmcWorkingLoadDeserializationArtifact($Request,$Game) {
    $selected=@($Request.PSObject.Properties.Name)-ccontains'observeWorkingDeserialization'
    $finalValue=$null
    foreach($kind in @('working-load-deserialization-pending','working-load-deserialization-evidence')) {
        $artifactPath=Join-Path $Request.evidenceRoot ($kind+'.json')
        if(-not(Test-Path -LiteralPath $artifactPath -PathType Leaf)){continue}
        if(-not$selected){throw 'Working observation sidecar exists without the exact request opt-in.'}
        Assert-KmcNotReparsePoint $artifactPath 'Working observation artifact';Assert-KmcNotHardLink $artifactPath 'Working observation artifact'
        if((Get-Item -LiteralPath $artifactPath).Length-gt262144){throw 'Working observation artifact exceeds its byte bound.'}
        Assert-KmcManifestJsonMembersUnique $artifactPath 'Working observation artifact'
        $value=Read-KmcJson $artifactPath
        $isPending=$kind-ceq'working-load-deserialization-pending'
        Assert-KmcWorkingLoadDeserializationValue $value $Request -Pending:$isPending
        if(-not$isPending){$finalValue=$value}
    }
    if($selected-and$Game.status-ceq'PASS'){
        if($null-eq$finalValue){throw 'Selected Working observation PASS has no final artifact.'}
        if($finalValue.status-cne'PASS'){throw 'Selected Working observation disagrees with runtime PASS.'}
    }
}
