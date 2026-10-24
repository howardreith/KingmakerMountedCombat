Set-StrictMode -Version Latest
function Test-KmcChunk7FieldScenario([string]$Scenario){$Scenario-ceq'chunk7-ordinary-field-admission'}
function Get-KmcChunk7FieldRows {@('chunk7-ordinary-field-admission','C7-field-readonly-source','C7-field-native-party-placement','C7-field-cleanup','C7-field-runtime','C7-field-interrupted')}
function Assert-KmcChunk7FieldSelection($Request){
    if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
    $has=$null-ne$Request.PSObject.Properties['ordinaryField']
    if(-not(Test-KmcChunk7FieldScenario $Request.scenario)){if($has){throw 'Field selection is forbidden under another scenario.'};return}
    if(-not$has){throw 'Exact read-only field selection is missing.'};$s=$Request.ordinaryField
    Assert-KmcExactProperties $s @('case','sourceRunId','sourceArtifactSha256','sourceArchiveSha256','actorId','petId') 'ordinary field selection'
    if($s.case-isnot[string]-or$s.case-cne'paid-small-remote-field'-or$s.sourceRunId-isnot[string]-or
       $s.sourceRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.sourceRunId-ceq$Request.runId){throw 'Exact paid acquisition source/case is required.'}
    foreach($key in @('sourceArtifactSha256','sourceArchiveSha256')){if($s.$key-isnot[string]-or$s.$key-cnotmatch'^[a-f0-9]{64}$'){throw 'Field source immutable hash is missing.'}}
    Assert-KmcOrdinaryGuid $s.actorId;Assert-KmcOrdinaryGuid $s.petId
    if($s.actorId-ceq$s.petId){throw 'Field rider and pet cannot alias.'}
}
function Assert-KmcChunk7FieldLoadDescriptor($Request){
    Assert-KmcChunk7FieldSelection $Request
    if(-not(Test-KmcChunk7FieldScenario $Request.scenario)){throw 'Field archive descriptor is scoped to its exact case.'}
    $d=$Request.persistenceLoad
    Assert-KmcExactProperties $d @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') 'field archive descriptor'
    if($d.internalName-isnot[string]-or[string]::IsNullOrWhiteSpace($d.internalName)-or$d.internalName.Length-gt256-or$d.internalName-match'[\x00-\x1f\x7f]'-or
       $d.fileName-isnot[string]-or$d.fileName-cne'Auto_1.zks'-or$d.sha256-isnot[string]-or$d.sha256-cne$Request.ordinaryField.sourceArchiveSha256-or
       $d.sha256-ceq$Request.fixture.working.sha256-or$d.sha256-ceq$Request.fixture.baseline.sha256-or
       $d.gameId-cne$Request.fixture.working.gameId-or$d.gameName-cne$Request.fixture.working.gameName-or$d.area-cne$Request.fixture.working.area){throw 'Read-only paid archive differs from exact fixture identity.'}
    Assert-KmcOrdinaryInteger $d.length 1 268435456;Assert-KmcOrdinaryInteger $d.lastWriteTimeUtcTicks 1
}
function Assert-KmcChunk7FieldAuthority($Row,[string]$Root,[long]$Generation){
    if($Row.root-isnot[string]-or$Row.root-cne$Root-or$null-ne$Row.maximumNativeWrites){throw 'Field bridge has foreign root or write authority.'}
    Assert-KmcChunk7Boolean $Row.exactScope $true
    Assert-KmcOrdinaryInteger $Row.generation 1 ([int]::MaxValue)
    if($Row.generation-ne$Generation){throw 'Field bridge authority generation changed.'}
    foreach($key in @('maximumNativeLoads','admittedNativeLoads','authorizedLoads')){Assert-KmcOrdinaryInteger $Row.$key 1 1}
    foreach($key in @('admittedNativeWrites','authorizedWrites','unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites')){Assert-KmcOrdinaryInteger $Row.$key 0 0}
}
function Assert-KmcChunk7FieldSnapshot($Row,$Selection){
    if($Row.actorId-cne$Selection.actorId-or$Row.petId-cne$Selection.petId){throw 'Field snapshot actor identity differs.'}
    foreach($key in @('actorObject','descriptorObject','petObject','petDescriptorObject')){Assert-KmcOrdinaryInteger $Row.$key ([int]::MinValue) ([int]::MaxValue);if($Row.$key-eq0){throw 'Field native object is absent.'}}
    Assert-KmcOrdinaryInteger $Row.money;Assert-KmcOrdinaryInteger $Row.xp 0 ([int]::MaxValue)
    foreach($key in @('level','petLevel','rank')){Assert-KmcOrdinaryInteger $Row.$key 1 20}
    if($Row.featureSourceGuid-isnot[string]-or$Row.featureSourceGuid-cnotmatch'^[a-f0-9]{32}$'){throw 'Native sourced Horse feature is missing.'}
    Assert-KmcChunk7Boolean $Row.reciprocal $true
    foreach($key in @('riderInGame','petInGame','riderInState','petInState','riderAgentReady','petAgentReady')){if($Row.$key-isnot[bool]){throw 'Native field state must be boolean.'}}
    foreach($key in @('partyCharacters','remoteCompanions')){
        if($Row.$key-isnot[Array]-or$Row.$key.Count-gt128){throw 'Native membership list is missing or oversized.'}
        $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach($id in $Row.$key){Assert-KmcOrdinaryGuid $id;if(-not$ids.Add($id)){throw 'Native membership duplicates an actor.'}}
    }
    if($Row.partyCharacters.Count-lt1-or$Row.partyCharacters.Count-gt6){throw 'Native field party cap differs.'}
}
function Assert-KmcChunk7FieldEvidence($Request,$Artifact,[string]$Status='PASS'){
    Assert-KmcChunk7FieldLoadDescriptor $Request
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-ordinary-field-evidence'
    Assert-KmcOrdinaryInteger $Artifact.schemaVersion 1 1
    if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS field result requires a PASS artifact.'}
    if($Artifact.status-cne'PASS'){return}
    $o=$Artifact.observations;$s=$Request.ordinaryField
    if((ConvertTo-KmcChunk7Canonical $o.selection)-cne(ConvertTo-KmcChunk7Canonical $s)){throw 'Field observed explicit selection differs.'}
    Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue)
    if(-not(Test-KmcChunk7FiniteNumber $o.elapsedSeconds)-or$o.elapsedSeconds-lt0-or$o.elapsedSeconds-gt300){throw 'Bounded native field elapsed observation invalid.'}
    foreach($flag in @('sourceWasRemoteAutosave','sourceArchiveStillRemote')){Assert-KmcChunk7Boolean $o.$flag $true}
    foreach($flag in @('smallPoseQualified','mountedCombatProven','freshMainCampaignProven','manualPartyMutation')){Assert-KmcChunk7Boolean $o.$flag $false}
    $source=$o.source
    if($source.runId-cne$s.sourceRunId-or$source.artifactSha256-cne$s.sourceArtifactSha256-or$source.archiveSha256-cne$s.sourceArchiveSha256-or
       $source.archiveLeaf-cne'Auto_1.zks'-or$source.archiveName-cne$Request.persistenceLoad.internalName){throw 'Field source receipt differs from the explicitly qualified source.'}
    Assert-KmcOrdinaryInteger $source.archiveLength $Request.persistenceLoad.length $Request.persistenceLoad.length
    Assert-KmcChunk7Boolean $source.sameCandidate $true;Assert-KmcChunk7Boolean $source.unchangedArchive $true
    Assert-KmcChunk7OrdinarySelection ([pscustomobject]@{scenario='chunk7-ordinary-acquisition';runId=$s.sourceRunId;ordinaryAcquisition=$source.bodySelection})
    if($source.bodySelection.case-cne'paid-small-ranger-horse'){throw 'Field source was not a paid Small acquisition.'}
    $root=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($Request.evidenceRoot))) ('runtime-staging\persistence-'+$Request.runId+'\Saved Games')
    Assert-KmcChunk7FieldAuthority $o.authority $root $o.authority.generation
    Assert-KmcChunk7FieldAuthority $o.cleanup.authority $root $o.authority.generation
    Assert-KmcChunk7FieldSnapshot $o.before $s;Assert-KmcChunk7FieldSnapshot $o.field $s
    if($o.before.riderInGame-cne$false-or$s.actorId-cin$o.before.partyCharacters-or$s.actorId-cnotin$o.before.remoteCompanions){throw 'The cold acquisition archive was not an exact remote companion.'}
    foreach($flag in @('riderInGame','petInGame','riderInState','petInState','riderAgentReady','petAgentReady')){Assert-KmcChunk7Boolean $o.field.$flag $true}
    foreach($key in @('actorId','actorObject','descriptorObject','petId','petObject','petDescriptorObject','money','xp','level','petLevel','rank','featureSourceGuid','reciprocal')){
        if((ConvertTo-KmcChunk7Canonical $o.before.$key)-cne(ConvertTo-KmcChunk7Canonical $o.field.$key)){throw ('Field admission changed native identity/progression: '+$key)}
    }
    if($o.field.partyCharacters.Count-ne($o.before.partyCharacters.Count+1)-or$s.actorId-cnotin$o.field.partyCharacters-or$s.actorId-cin$o.field.remoteCompanions){throw 'Native party placement not observed.'}
    foreach($id in $o.before.partyCharacters){if($id-cnotin$o.field.partyCharacters){throw 'Native selector removed an original party character.'}}
    foreach($id in @('b6628a77-4962-47a4-a17c-88d9836fc9d5','d17c8fd0-6627-40c4-bf9a-024cf001c1f7')){if($id-cnotin$o.before.partyCharacters){throw 'Qualified original party actor missing.'}}
    $ui=$o.selector
    if($ui.action-cne'native-ShowPartySelection'-or$ui.actorId-cne$s.actorId){throw 'Native selector identity differs.'}
    Assert-KmcOrdinaryInteger $ui.nativeCap 6 6
    foreach($key in @('openCount','switchCount','goCount')){Assert-KmcOrdinaryInteger $ui.$key 1 1}
    foreach($flag in @('openActiveSelf','readyActiveInHierarchy','closedExactInstance')){Assert-KmcChunk7Boolean $ui.$flag $true}
    foreach($flag in @('closedActiveSelf','manualPartyMutation')){Assert-KmcChunk7Boolean $ui.$flag $false}
    if(-not(Test-KmcChunk7FiniteNumber $ui.readyAlpha)-or$ui.readyAlpha-ne1){throw 'Native selector was not ready before ordinary input.'}
    $b=$o.bodyObservation;$pins=$source.bodySelection
    foreach($flag in @('NativeRaceAndPresetIdentityExact','NativeSkeletonIsExactPreset','NativeRigAvailable')){Assert-KmcChunk7Boolean $b.$flag $true}
    foreach($flag in @('RiderPolymorphed','MountPolymorphed')){Assert-KmcChunk7Boolean $b.$flag $false}
    Assert-KmcOrdinaryInteger $b.OriginalSizeOrdinal 3 3;Assert-KmcOrdinaryInteger $b.EffectiveSizeOrdinal 3 3
    if($b.RaceGuid-cne$pins.smallRaceGuid-or$b.PresetGuid-cne$pins.smallPresetGuid-or$b.SkeletonName-cne$pins.skeletonName-or$b.Gender-cne$pins.gender){throw 'Observed field body differs from its native source selection.'}
    Assert-KmcChunk7Boolean $b.LeftHanded $pins.leftHanded
    if($o.profileInventory.captureStatus-cne'PASS'-or$o.profileInventory.gameplayAssemblyMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Exact native profile observation missing.'}
    Assert-KmcOrdinaryInteger $o.profileInventory.schemaVersion 1 1
    Assert-KmcChunk7Boolean $o.profileInventory.profileAdmissionChanged $false
    if($o.profileInventory.party-isnot[Array]-or$o.profileInventory.party.Count-ne2){throw 'Field rig inventory must bind the exact rider and pet.'}
    Assert-KmcChunk7ExactIds $o.profileInventory.party @($s.actorId,$s.petId)
    $nativeRider=@($o.profileInventory.party|Where-Object id -CEQ $s.actorId)[0]
    Assert-KmcChunk7Boolean $nativeRider.character.present $true
    foreach($flag in @('custodySettled','nativeWorkerSettled','selectorClosed','selectionRestored','pauseRestored','acquiredNativeStateRetained','sourceUnchanged')){Assert-KmcChunk7Boolean $o.cleanup.$flag $true}
    foreach($key in @('nativeWrites','manualPartyMutations','manualRefunds','manualFactRemovals','manualCompanionRemovals')){Assert-KmcOrdinaryInteger $o.cleanup.$key 0 0}
}
function Assert-KmcChunk7FieldArtifact($Request,$Manifest,[string]$Status='PASS'){
    $records=@($Manifest.artifacts|Where-Object{$_.kind-ceq'chunk7-ordinary-field-evidence'-or$_.relativePath-ceq'chunk7-ordinary-field-evidence.json'})
    if(-not(Test-KmcChunk7FieldScenario $Request.scenario)){if($records.Count){throw 'Field artifact under another scenario.'};return}
    if($records.Count-eq0-and$Status-ceq'FAIL'){return}
    $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-ordinary-field-evidence.json' 'chunk7-ordinary-field-evidence'
    Assert-KmcChunk7FieldEvidence $Request $artifact $Status
}
