Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7OrdinaryChoiceEvidence.ps1')
# Closed original observer reader. Synthetic acceptance is not native proof.
function Test-KmcChunk7OrdinaryScenario([string]$Scenario) {$Scenario -ceq 'chunk7-ordinary-acquisition'}
function Get-KmcChunk7OrdinaryRows {
    @('chunk7-ordinary-acquisition','C7-ordinary-fixture','C7-ordinary-native-choices','C7-ordinary-native-autosave',
      'C7-ordinary-native-field-inventory','C7-ordinary-cleanup','C7-ordinary-runtime','C7-ordinary-interrupted')
}
function Assert-KmcOrdinaryInteger($Value,[long]$Minimum=0,[long]$Maximum=[long]::MaxValue) {
    if(-not(Test-KmcExactJsonInteger $Value)-or$Value-lt$Minimum-or$Value-gt$Maximum){throw 'Ordinary native counter is missing, mistyped or out of range.'}
}
function Assert-KmcOrdinaryGuid($Value) {
    $parsed=[Guid]::Empty
    if($Value-isnot[string]-or-not[Guid]::TryParseExact($Value,'D',[ref]$parsed)-or$parsed-eq[Guid]::Empty-or$parsed.ToString('D')-cne$Value){throw 'Ordinary native actor ID is not canonical.'}
}
function Assert-KmcChunk7OrdinarySelection($Request) {
    if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
    $has=$null-ne$Request.PSObject.Properties['ordinaryAcquisition']
    if(-not(Test-KmcChunk7OrdinaryScenario $Request.scenario)){if($has){throw 'Ordinary selection is forbidden under another scenario.'};return}
    if(-not$has){throw 'Exact ordinary acquisition selection is missing.'}
    $s=$Request.ordinaryAcquisition
    Assert-KmcExactProperties $s @('case','inventoryRunId','inventoryArtifactSha256','smallRaceGuid','smallPresetGuid','skeletonName','gender','leftHanded','existingActorId','minimumRangerLevel','expectedNativeWriteCount','nativeWriteLeaf') 'ordinary acquisition selection'
    if($s.case-isnot[string]-or$s.case-cnotin@('paid-small-ranger-horse','existing-ranger-horse')-or
       $s.inventoryRunId-isnot[string]-or$s.inventoryRunId-cnotmatch'^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$'-or$s.inventoryRunId-ceq$Request.runId-or
       $s.inventoryArtifactSha256-isnot[string]-or$s.inventoryArtifactSha256-cnotmatch'^[a-f0-9]{64}$'-or
       $s.nativeWriteLeaf-isnot[string]-or$s.nativeWriteLeaf-cne'Auto_1.zks'){throw 'Ordinary selection identity differs.'}
    Assert-KmcOrdinaryInteger $s.minimumRangerLevel 4 20;Assert-KmcOrdinaryInteger $s.expectedNativeWriteCount 1 1
    if($s.case-ceq'paid-small-ranger-horse'){
        foreach($field in @('smallRaceGuid','smallPresetGuid')){if($s.$field-isnot[string]-or$s.$field-cnotmatch'^[a-f0-9]{32}$'){throw 'Observed native Small blueprint pin is required.'}}
        if($s.skeletonName-isnot[string]-or[string]::IsNullOrWhiteSpace($s.skeletonName)-or$s.skeletonName.Length-gt160-or
           $s.gender-isnot[string]-or$s.gender-cnotin@('Male','Female')-or$s.leftHanded-isnot[bool]-or$null-ne$s.existingActorId){throw 'Exact Small body selection differs.'}
    }else{
        Assert-KmcOrdinaryGuid $s.existingActorId
        if($s.existingActorId-cne'd17c8fd0-6627-40c4-bf9a-024cf001c1f7'){throw 'Existing ordinary fixture actor differs.'}
        foreach($field in @('smallRaceGuid','smallPresetGuid','skeletonName','gender','leftHanded')){if($null-ne$s.$field){throw 'Existing actor acquisition cannot alter its native body.'}}
    }
}
function Assert-KmcOrdinarySnapshot($Row,$Identity,[long]$Xp,[long]$Level,[long]$Money) {
    foreach($field in @('actorObject','descriptorObject')){
        Assert-KmcOrdinaryInteger $Row.$field ([int]::MinValue) ([int]::MaxValue)
        if($Row.$field-eq0-or$Row.$field-ne$Identity.$field){throw 'Ordinary progression replaced native actor/descriptor identity.'}
    }
    Assert-KmcOrdinaryInteger $Row.level 0 20;Assert-KmcOrdinaryInteger $Row.xp 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $Row.money
    Assert-KmcOrdinaryInteger $Row.nativeControllerObject ([int]::MinValue) ([int]::MaxValue)
    Assert-KmcOrdinaryInteger $Row.rangerLevel 0 20
    if($Row.actorId-cne$Identity.actorId-or$Row.xp-ne$Xp-or$Row.level-ne$Level-or$Row.money-ne$Money){throw 'Ordinary choice changed actor, XP, level or money outside native commit.'}
    if($Row.nativeCanLevelUp-isnot[bool]-or$Row.nativeIsComplete-isnot[bool]){throw 'Native readiness booleans are required.'}
}
function Assert-KmcOrdinaryAuthority($Row,[string]$Root,[long]$Generation,[long]$Writes) {
    if($Row.root-isnot[string]-or$Row.root-cne$Root){throw 'Ordinary authority root differs.'}
    Assert-KmcChunk7Boolean $Row.exactScope $true
    Assert-KmcOrdinaryInteger $Row.generation 1
    if($Row.generation-ne$Generation){throw 'Ordinary exact authority generation changed.'}
    Assert-KmcOrdinaryInteger $Row.maximumNativeWrites 1 1
    Assert-KmcOrdinaryInteger $Row.authorizedLoads 1 1
    foreach($field in @('admittedNativeWrites','authorizedWrites')){Assert-KmcOrdinaryInteger $Row.$field $Writes $Writes}
    foreach($field in @('unauthorizedLoads','unauthorizedWrites','fatalViolations','suppressedWrites')){Assert-KmcOrdinaryInteger $Row.$field 0 0}
}
function Assert-KmcChunk7OrdinaryEvidence($Request,$Artifact,[string]$Status='PASS') {
    Assert-KmcChunk7OrdinarySelection $Request
    if(-not(Test-KmcChunk7OrdinaryScenario $Request.scenario)){throw 'Ordinary artifact belongs to another scenario.'}
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-ordinary-acquisition-evidence'
    Assert-KmcOrdinaryInteger $Artifact.schemaVersion 1 1
    if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS ordinary result requires PASS artifact.'}
    if($Artifact.status-cne'PASS'){return}
    $o=$Artifact.observations;$s=$Request.ordinaryAcquisition;$paid=$s.case-ceq'paid-small-ranger-horse'
    if($o.case-cne$s.case){throw 'Ordinary observed case differs.'}
    foreach($p in $s.PSObject.Properties){if((ConvertTo-KmcChunk7Canonical $o.selection.($p.Name))-cne(ConvertTo-KmcChunk7Canonical $p.Value)){throw 'Ordinary observed selection differs.'}}
    Assert-KmcOrdinaryInteger $o.processId 1 ([int]::MaxValue)
    if(-not(Test-KmcChunk7FiniteNumber $o.elapsedSeconds)-or$o.elapsedSeconds-lt0-or$o.elapsedSeconds-gt900){throw 'Ordinary elapsed observation invalid.'}
    Assert-KmcChunk7Boolean $o.nativeProgressionRequiresExternalWorkingRestoration $true
    foreach($field in @('vendorDialogueProven','freshMainCampaignProven','smallPoseQualified','mountedCombatProven')){Assert-KmcChunk7Boolean $o.$field $false}
    $inventory=$o.inventoryProvenance
    if($inventory.runId-cne$s.inventoryRunId-or$inventory.sha256-cne$s.inventoryArtifactSha256){throw 'Ordinary exact inventory provenance differs.'}
    Assert-KmcOrdinaryInteger $inventory.length 1 16777216
    Assert-KmcChunk7Boolean $inventory.sameCandidate $true;Assert-KmcChunk7Boolean $inventory.sameWorking $true
    $root=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($Request.evidenceRoot))) ('runtime-staging\persistence-'+$Request.runId+'\Saved Games')
    Assert-KmcOrdinaryAuthority $o.authority $root $o.authority.generation 0
    Assert-KmcOrdinaryAuthority $o.cleanup.authority $root $o.authority.generation 1
    $original=@('b6628a77-4962-47a4-a17c-88d9836fc9d5','d79a4f6c-b74e-4868-95bd-533899131acb','d17c8fd0-6627-40c4-bf9a-024cf001c1f7')
    if($o.originalParty-isnot[Array]-or($o.originalParty-join'|')-cne($original-join'|')){throw 'Ordinary qualified actor set differs.'}
    $choiceProof=Get-KmcChunk7OrdinaryChoiceProof -RunId $Request.runId -Selection $s -Paid $paid -InitialActorIds $original -Rows $o.choices -BeforeMoney $o.before.money -BuilderComplete $o.builderComplete -ProductVersion $Request.productVersion
    $identity=$choiceProof.identity;$level=$choiceProof.finalLevel;$expectedMoney=$choiceProof.expectedMoney
    $accepted=$choiceProof.acceptedChoiceCount;$commits=$choiceProof.nativeCommitCount
    Assert-KmcOrdinaryInteger $o.acceptedChoiceCount $accepted $accepted;Assert-KmcOrdinaryInteger $o.nativeCommitCount $commits $commits
    $probe=$o.nativeObserver
    foreach($field in @('installationAttempted','installed','complete')){Assert-KmcChunk7Boolean $probe.$field $true}
    if($probe.errors-isnot[Array]-or$probe.errors.Count-ne0-or$probe.hooks-isnot[Array]-or$probe.hooks.Count-ne3-or$probe.events-isnot[Array]-or$probe.events.Count-ne(2*$commits+2)){throw 'Native ordinary observer is incomplete.'}
    $contracts=@(@('Kingmaker.Controllers.LevelUpAutoSaveController','HandleLevelUpComplete','0600906B'),@('Kingmaker.Game','SaveGame','06000CE4'),@('Kingmaker.EntitySystem.Persistence.SaveManager','PrepareSave','06008025'))
    for($i=0;$i-lt3;$i++){$h=$probe.hooks[$i];if($h.type-cne$contracts[$i][0]-or$h.method-cne$contracts[$i][1]-or$h.token-cne$contracts[$i][2]-or$h.moduleMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Exact native ordinary hook contract differs.'}}
    Assert-KmcOrdinaryInteger $probe.completionEntries $commits $commits;Assert-KmcOrdinaryInteger $probe.completionExits $commits $commits
    Assert-KmcOrdinaryInteger $probe.saveRequests 1 1;Assert-KmcOrdinaryInteger $probe.preparedWrites 1 1
    $inside=$null;$entries=0;$exits=0;$requests=0;$writes=0;$saveObject=0;$lastFrame=-1
    foreach($row in $probe.events){
        if($row.kind-isnot[string]){throw 'Native autosave callback kind must be a scalar string.'}
        Assert-KmcOrdinaryInteger $row.frame 0 ([int]::MaxValue)
        Assert-KmcOrdinaryInteger $row.actorObject ([int]::MinValue) ([int]::MaxValue)
        if($row.actorId-cne$identity.actorId-or$row.actorObject-ne$identity.actorObject-or$row.frame-lt$lastFrame){throw 'Native autosave observer actor/frame binding differs.'};$lastFrame=$row.frame
        switch -CaseSensitive ($row.kind){
            'native-level-completion-enter' {
                if($null-ne$inside){throw 'Overlapping native completion callbacks.'};$entries++;$inside=$row
                Assert-KmcChunk7Boolean $row.insideObservedCompletion $true
                Assert-KmcChunk7Boolean $row.isChargen ($entries-lt$commits)
                Assert-KmcOrdinaryInteger $row.characterLevel ($identity.level+$entries) ($identity.level+$entries)
            }
            'native-level-autosave-request' {
                if($null-eq$inside-or$entries-ne$commits-or$row.completionActorId-cne$identity.actorId-or$row.frame-ne$inside.frame){throw 'Native autosave did not originate inside the exact terminal completion.'}
                Assert-KmcChunk7Boolean $row.insideObservedCompletion $true;Assert-KmcChunk7Boolean $row.isChargen $false;Assert-KmcChunk7Boolean $row.inState $true
                Assert-KmcChunk7Boolean $row.autoLevelup $true;if($row.saveType-cne'Auto'){throw 'Native ordinary completion requested a different save category.'}
                $requests++;Assert-KmcOrdinaryInteger $row.saveObject ([int]::MinValue) ([int]::MaxValue)
                if($row.saveObject-eq0){throw 'Native autosave object identity missing.'};$saveObject=$row.saveObject
            }
            'native-level-autosave-prepared' {
                Assert-KmcOrdinaryInteger $row.saveObject ([int]::MinValue) ([int]::MaxValue)
                if($row.saveObject-eq0){throw 'Native prepared object identity missing.'}
                $writes++;if($requests-ne1-or$row.saveObject-eq$saveObject-or$row.saveLeaf-cne'Auto_1.zks'){throw 'Native PrepareSave lost exact descriptor identity.'}
                Assert-KmcChunk7Boolean $row.autoLevelup $true;if($row.saveType-cne'Auto'){throw 'Native prepared category differs.'}
            }
            'native-level-completion-exit' {
                if($null-eq$inside-or$row.frame-ne$inside.frame-or$row.completionActorId-cne$identity.actorId){throw 'Native completion exit lost exact callback custody.'}
                Assert-KmcChunk7Boolean $row.isChargen $inside.isChargen
                Assert-KmcChunk7Boolean $row.insideObservedCompletion $true;$inside=$null;$exits++
            }
            default {throw 'Unknown native autosave observer event.'}
        }
    }
    if($null-ne$inside-or$entries-ne$commits-or$exits-ne$commits-or$requests-ne1-or$writes-ne1){throw 'Native completion/autosave events do not settle exactly once.'}
    $w=$o.nativeWrite;Assert-KmcOrdinarySaveDescriptorBinding $probe $w
    if($w.path-isnot[string]-or$w.path-cne(Join-Path $root 'Auto_1.zks')-or$w.leaf-cne'Auto_1.zks'-or$w.type-cne'Auto'-or$w.operation-cne'None'-or
       $w.sha256-isnot[string]-or$w.sha256-cnotmatch'^[a-f0-9]{64}$'-or$w.gameId-cne$Request.fixture.working.gameId-or$w.gameName-cne$Request.fixture.working.gameName-or$w.area-cne$Request.fixture.working.area){throw 'Exact completed native ordinary archive identity differs.'}
    Assert-KmcOrdinaryInteger $w.length 1 268435456;Assert-KmcOrdinaryInteger $w.schema 3 3
    foreach($field in @('authorizedWrites','admittedWrites','completedWrites','snapshotCount')){Assert-KmcOrdinaryInteger $w.$field 1 1}
    Assert-KmcChunk7Boolean $w.autoLevelup $true;Assert-KmcChunk7Boolean $w.nativeCallbackSupplied $false;Assert-KmcChunk7Boolean $w.saveBeforeFieldSelection $paid
    $f=$o.field
    Assert-KmcOrdinaryInteger $f.actorObject ([int]::MinValue) ([int]::MaxValue)
    if($f.actorId-cne$identity.actorId-or$f.actorObject-ne$identity.actorObject-or$f.petId-cne$o.builderComplete.petId-or$f.horseBlueprint-cne'4016c7db400ab721ff125aef9e65e202'-or
       $f.featureBlueprint-cne'7db7c50677e39f09feef56f3831fc723'-or$f.featureSourceGuid-isnot[string]-or$f.featureSourceGuid-cnotmatch'^[a-f0-9]{32}$'){throw 'Native ordinary field actor/feature/pet differs.'}
    Assert-KmcOrdinaryInteger $f.rank 1 20
    foreach($field in @('reciprocal','featureSourcePresent','ordinaryFieldAdmissionProven')){Assert-KmcChunk7Boolean $f.$field $true}
    if($f.profileInventory.captureStatus-cne'PASS'-or$f.profileInventory.gameplayAssemblyMvid-cne'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Native field rig inventory is missing.'}
    Assert-KmcOrdinaryInteger $f.profileInventory.schemaVersion 1 1
    Assert-KmcChunk7Boolean $f.profileInventory.profileAdmissionChanged $false
    if($paid){
        $b=$f.bodyObservation
        Assert-KmcOrdinaryInteger $b.OriginalSizeOrdinal 3 3;Assert-KmcOrdinaryInteger $b.EffectiveSizeOrdinal 3 3
        foreach($field in @('NativeRaceAndPresetIdentityExact','NativeSkeletonIsExactPreset','NativeRigAvailable')){Assert-KmcChunk7Boolean $b.$field $true}
        foreach($field in @('RiderPolymorphed','MountPolymorphed')){Assert-KmcChunk7Boolean $b.$field $false}
        if($b.RaceGuid-cne$s.smallRaceGuid-or$b.PresetGuid-cne$s.smallPresetGuid-or$b.Gender-cne$s.gender-or$b.SkeletonName-cne$s.skeletonName){throw 'Field Small body differs from admitted native choices.'}
        Assert-KmcChunk7Boolean $b.LeftHanded $s.leftHanded
        Assert-KmcChunk7Boolean $o.fieldSelection.manualPartyMutation $false;Assert-KmcOrdinaryInteger $o.fieldSelection.nativeCap 6 6
        Assert-KmcChunk7Boolean $o.fieldSelection.openActiveSelf $true
        Assert-KmcChunk7Boolean $o.fieldSelection.readyActiveInHierarchy $true
        Assert-KmcChunk7Boolean $o.fieldSelection.closedActiveSelf $false
        if(-not(Test-KmcChunk7FiniteNumber $o.fieldSelection.readyAlpha)-or$o.fieldSelection.readyAlpha-ne1){throw 'Native party selector did not finish its opening animation before input.'}
        if($o.fieldSelection.actorId-cne$identity.actorId-or$o.fieldSelection.action-cne'native-ShowPartySelection'){throw 'Native paid companion party admission missing.'}
    }
    $c=$o.cleanup
    foreach($field in @('custodySettled','acquiredNativeStateRetained','nativeBuilderClosed','nativeWorkerSettled','selectionRestored','pauseRestored')){Assert-KmcChunk7Boolean $c.$field $true}
    foreach($field in @('manualRefunds','manualFactRemovals','manualCompanionRemovals')){Assert-KmcOrdinaryInteger $c.$field 0 0}
    Assert-KmcOrdinaryInteger $c.after.money $expectedMoney $expectedMoney
    Assert-KmcChunk7ExactIds @($o.before.originalActors) $original;Assert-KmcChunk7ExactIds @($c.after.originalActors) $original
    foreach($a in $o.before.originalActors){
        $b=@($c.after.originalActors|Where-Object id -CEQ $a.id)[0]
        foreach($row in @($a,$b)){
            Assert-KmcOrdinaryInteger $row.object ([int]::MinValue) ([int]::MaxValue)
            if($row.object-eq0){throw 'Original native actor object identity is absent.'}
            Assert-KmcOrdinaryInteger $row.xp 0 ([int]::MaxValue)
            Assert-KmcOrdinaryInteger $row.level 0 20
        }
        foreach($field in @('object','xp')){if($b.$field-ne$a.$field){throw 'Ordinary acquisition changed an original actor identity/XP.'}}
        Assert-KmcChunk7Boolean $b.inState $true;Assert-KmcChunk7Boolean $b.inGame $true
        if($a.id-cne$identity.actorId){if($b.level-ne$a.level-or$b.petId-cne$a.petId){throw 'Ordinary acquisition changed unrelated progression/companion.'}}
        elseif($b.level-ne$level-or$b.petId-cne$f.petId){throw 'Acquired original actor was rolled back or replaced.'}
    }
    foreach($party in @($f.parties,$c.after)){
        if($party.partyCharacters-isnot[Array]-or$party.partyCharacters.Count-gt6-or@($party.partyCharacters|Where-Object{$_-ceq$identity.actorId}).Count-ne1){throw 'Acquired actor is not one native field party member.'}
        foreach($id in $o.before.partyCharacters){if(@($party.partyCharacters|Where-Object{$_-ceq$id}).Count-ne1){throw 'Original native party character was removed.'}}
    }
}
function Assert-KmcChunk7OrdinaryArtifact($Request,$Manifest,[string]$Status='PASS') {
    $records=@($Manifest.artifacts|Where-Object{$_.kind-ceq'chunk7-ordinary-acquisition-evidence'-or$_.relativePath-ceq'chunk7-ordinary-acquisition-evidence.json'})
    if(-not(Test-KmcChunk7OrdinaryScenario $Request.scenario)){if($records.Count-ne0){throw 'Ordinary artifact under another scenario.'};return}
    if($records.Count-eq0-and$Status-ceq'FAIL'){return}
    $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-ordinary-acquisition-evidence.json' 'chunk7-ordinary-acquisition-evidence'
    Assert-KmcChunk7OrdinaryEvidence $Request $artifact $Status
}

# Native SaveRoutine keeps Requested and prepares a distinct Type/Name copy.
# The native service completion ties that exact pair to one completed sequence.
function Assert-KmcOrdinarySaveDescriptorBinding($Probe,$Write){
    $requests=@($Probe.events|Where-Object {$_.kind-is[string]-and$_.kind-ceq'native-level-autosave-request'})
    $prepared=@($Probe.events|Where-Object {$_.kind-is[string]-and$_.kind-ceq'native-level-autosave-prepared'})
    if($requests.Count-ne1-or$prepared.Count-ne1){throw 'Native save descriptor pair must be unique.'}
    $r=$requests[0];$p=$prepared[0]
    foreach($value in @($r.saveObject,$p.saveObject,$p.requestedSaveObject,$Write.requestedObject,$Write.writtenObject)){
        Assert-KmcOrdinaryInteger $value ([int]::MinValue) ([int]::MaxValue)
        if($value-eq0){throw 'Native save descriptor identity is absent.'}
    }
    if($r.saveObject-eq$p.saveObject-or$p.requestedSaveObject-ne$r.saveObject-or$Write.requestedObject-ne$r.saveObject-or$Write.writtenObject-ne$p.saveObject){throw 'Native request, prepared copy and completed descriptor bindings differ.'}
    foreach($value in @($r.saveName,$p.saveName,$Write.name)){if($value-isnot[string]-or[string]::IsNullOrEmpty($value)){throw 'Native save name must be a nonempty scalar string.'}}
    if($p.saveName-cne$r.saveName-or$Write.name-cne$r.saveName){throw 'Native request/copy Type-Name contract differs.'}
    Assert-KmcChunk7Boolean $p.accepted $true;Assert-KmcChunk7Boolean $p.insideObservedCompletion $false
    if($null-ne$p.completionActorId){throw 'Native prepared copy still claims synchronous completion custody.'}
    if($Probe.events[-1].kind-cne'native-level-autosave-prepared'-or$Probe.events[-2].kind-cne'native-level-completion-exit'){throw 'Native preparation must follow the final completion exit.'}
    Assert-KmcOrdinaryInteger $Write.completedWritesBefore 0 ([int]::MaxValue-1)
    Assert-KmcOrdinaryInteger $Write.completionSequence ([long]$Write.completedWritesBefore+1) ([long]$Write.completedWritesBefore+1)
}
