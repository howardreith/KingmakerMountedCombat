# Original strict reader for the bounded native world fixture; no staging or native writes.
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7OwnedPairsEvidence.ps1')
function Get-KmcChunk7WorldRows {
    @('chunk7-two-pair-exploration','C7-world-fixture','C7-world-native-acquisition','C7-world-owner-seed-setup','C7-world-two-native-mounts','C7-world-selection-routing',
      'C7-world-independent-dismount','C7-world-cycles','C7-world-sampling','C7-world-cleanup','C7-world-runtime','C7-world-visual','C7-world-interrupted','C7-world-owner-seed-manual','C7-small-third-horse-native-setup','C7-small-third-horse-native-manual','C7-f09-fourth-horse-native-setup','C7-f09-fourth-horse-native-manual',
      'C7-six-native-mounts','C7-six-selection-routing','C7-six-cycles','C7-six-sampling')
}
function Assert-KmcChunk7FourthHorseZeroWrite($Observations){
    Assert-KmcChunk7Boolean $Observations.cleanup.nativeSaveWritten $false
    foreach($key in @('nativeWrite','thirdHorseManual','saveRequested')){
        if($null-ne$Observations.PSObject.Properties[$key]){throw 'Fourth-Horse diagnostic requested or reported a native save.'}
    }
}
function Assert-KmcChunk7ThirdHorseSetupEvidence($Request,$Observations){
    Assert-KmcChunk7OwnedPairsDescriptor $Request
    Assert-KmcChunk7InheritedSnapshot $Request $Observations.inheritedFixture 'admitted'
    $fourth=Test-KmcChunk7OwnedPairsFourthHorse $Request
    $s=$Observations.thirdHorseSetup
    Assert-KmcExactProperties $s @('contract','sourceRunId','sourceArchiveSha256','disclosure','fixtureSetupOnly','sixPairQualified','saveRequests','nativeCastRequests','targetRiderId','before','events','xpAssistance','nativeBuild','nativeCommitCount','autoLevelupOverrideCount','after','horseId','nativeHorse','fieldActorIds') 'third-Horse native setup'
    Assert-KmcBootstrapInventoryString $s.contract $(if($fourth){'three-horse-source-fourth-native-horse-setup-v1'}else{'small-source-third-native-horse-setup-v1'})
    Assert-KmcBootstrapInventoryString $s.sourceRunId $Request.ownedPairs.sourceRunId
    Assert-KmcBootstrapInventoryString $s.sourceArchiveSha256 $Request.ownedPairs.sourceArchiveSha256
    Assert-KmcChunk7Boolean $s.fixtureSetupOnly $true;Assert-KmcChunk7Boolean $s.sixPairQualified $false
    foreach($name in @('saveRequests','nativeCastRequests')){Assert-KmcOrdinaryInteger $s.$name 0 0}
    Assert-KmcOrdinaryInteger $s.autoLevelupOverrideCount 0 4
    Assert-KmcOrdinaryInteger $s.nativeCommitCount 4 4
    Assert-KmcBootstrapInventoryString $s.targetRiderId $(if($fourth){'5dd7bd1d-ae4c-4df7-9687-50fceb027344'}else{'fd213210-71cd-41b0-b35b-68e1b18e9b43'})
    if((ConvertTo-KmcChunk7Canonical $s.before)-cne(ConvertTo-KmcChunk7Canonical $Observations.inheritedFixture.arrangement)){throw 'Third-Horse setup did not begin at exact admitted Small progression.'}
    Assert-KmcExactProperties $s.xpAssistance @('nativeApi','before','after','delta','targetLevel','characterLevelUnchanged','moneyUnchanged') 'third-Horse XP assistance'
    Assert-KmcBootstrapInventoryString $s.xpAssistance.nativeApi 'UnitProgressionData.GainExperience(42000, false)'
    Assert-KmcOrdinaryInteger $s.xpAssistance.before 9000 9000;Assert-KmcOrdinaryInteger $s.xpAssistance.after 51000 51000
    Assert-KmcOrdinaryInteger $s.xpAssistance.delta 42000 42000;Assert-KmcOrdinaryInteger $s.xpAssistance.targetLevel 8 8
    Assert-KmcChunk7Boolean $s.xpAssistance.characterLevelUnchanged $true;Assert-KmcChunk7Boolean $s.xpAssistance.moneyUnchanged $true
    if($s.events-isnot[Array]-or@($s.events|Where-Object event -CEQ 'native-level-committed').Count-ne4-or
       @($s.events|Where-Object event -CEQ 'native-builder-complete').Count-ne1){throw 'Third-Horse setup lacks exactly four native commits and terminal UI completion.'}
    if(@($s.events|Where-Object event -CEQ 'native-auto-levelup-plan-observed').Count-ne$s.autoLevelupOverrideCount){throw 'Third-Horse native auto-levelup UI overrides differ from the actual observed plans.'}
    foreach($event in $s.events){if($event.runId-cne$Request.runId){throw 'Third-Horse UI event belongs to another run.'}}
    $commits=@($s.events|Where-Object event -CEQ 'native-level-committed')
    for($index=0;$index-lt4;$index++){
        $commit=$commits[$index]
        Assert-KmcOrdinaryInteger $commit.commitCount ($index+1) ($index+1)
        foreach($phase in @('before','after')){
            Assert-KmcBootstrapInventoryString $commit.$phase.actorId $s.targetRiderId
            Assert-KmcOrdinaryInteger $commit.$phase.xp 51000 51000
        }
        Assert-KmcOrdinaryInteger $commit.before.level (4+$index) (4+$index)
        Assert-KmcOrdinaryInteger $commit.after.level (5+$index) (5+$index)
    }
    $terminal=@($s.events|Where-Object event -CEQ 'native-builder-complete')[0]
    Assert-KmcBootstrapInventoryString $terminal.actor.actorId $s.targetRiderId
    Assert-KmcOrdinaryInteger $terminal.actor.level 8 8
    if([Array]::IndexOf($s.events,$terminal)-lt[Array]::IndexOf($s.events,$commits[3])){throw 'Third-Horse UI completion preceded the final native commit.'}
    Assert-KmcBootstrapInventoryString $s.nativeBuild.actorId $s.targetRiderId
    Assert-KmcOrdinaryInteger $s.nativeBuild.level 8 8;Assert-KmcOrdinaryInteger $s.nativeBuild.xp 51000 51000
    Assert-KmcOrdinaryInteger $s.nativeBuild.rangerLevel 4 4
    $count=if($fourth){8}else{7}
    if($s.before.actors-isnot[Array]-or$s.after.actors-isnot[Array]-or$s.before.actors.Count-ne$count-or$s.after.actors.Count-ne$count-or
       $s.before.money-ne$s.after.money-or$s.before.experienceRatePercent-ne$s.after.experienceRatePercent){throw 'Third-Horse setup changed native roster, money or XP scaling.'}
    foreach($prior in $s.before.actors){
        $after=@($s.after.actors|Where-Object actorId -CEQ $prior.actorId)
        if($after.Count-ne1){throw 'Third-Horse setup lost an original actor.'}
        if($prior.actorId-cne$s.targetRiderId){if((ConvertTo-KmcChunk7Canonical $prior)-cne(ConvertTo-KmcChunk7Canonical $after[0])){throw 'Third-Horse setup changed an unrelated actor.'}}
        else{Assert-KmcOrdinaryInteger $after[0].experience 51000 51000;Assert-KmcOrdinaryInteger $after[0].characterLevel 8 8}
    }
    Assert-KmcOrdinaryGuid $s.horseId
    if($s.horseId-cin$Request.ownedPairs.partyIds){throw 'Third Horse aliases an inherited native actor.'}
    $horse=$s.nativeHorse
    Assert-KmcExactProperties $horse @('riderPetId','mountId','mountMasterId','blueprint','featureGuid','sourceGuid','rank') 'third native Horse'
    foreach($key in @('riderPetId','mountId')){Assert-KmcBootstrapInventoryString $horse.$key $s.horseId}
    Assert-KmcBootstrapInventoryString $horse.mountMasterId $s.targetRiderId
    Assert-KmcBootstrapInventoryString $horse.blueprint '4016c7db400ab721ff125aef9e65e202'
    foreach($key in @('featureGuid','sourceGuid')){if($horse.$key-isnot[string]-or$horse.$key-cnotmatch'^[a-f0-9]{32}$'){throw 'Third Horse lacks native sourced feature identity.'}}
    Assert-KmcOrdinaryInteger $horse.rank 1 20
    if($s.fieldActorIds-isnot[Array]-or(ConvertTo-KmcChunk7Canonical @($s.fieldActorIds|Sort-Object -CaseSensitive))-cne
       (ConvertTo-KmcChunk7Canonical @(@($Request.ownedPairs.partyIds)+@($s.horseId)|Sort-Object -CaseSensitive))){throw 'Third-Horse native field is not exactly the source seven plus one new pet.'}
    Assert-KmcChunk7Boolean $Observations.cleanup.horseRemoved $false
    Assert-KmcChunk7Boolean $Observations.cleanup.inheritedPetsPreserved $true
    Assert-KmcChunk7Boolean $Observations.cleanup.pairResidueAbsent $true
    Assert-KmcChunk7Boolean $Observations.cleanup.nativeProgressionRequiresExternalWorkingRestoration $false
    if($fourth){
      if(-not(Test-KmcChunk7OwnedPairsFourthSave $Request)){Assert-KmcChunk7FourthHorseZeroWrite $Observations}
      if(@($Observations.cleanup.partyAfter).Count-ne9){throw 'Fourth-Horse cleanup lost the ninth native field actor.'}
      $prior=@($Observations.inheritedFixture.pairs)
      if($prior.Count-ne3-or$prior[2].riderId-cne'fd213210-71cd-41b0-b35b-68e1b18e9b43'-or$prior[2].mountId-cne'da28f0a7-6eb0-4392-b2e0-f401a889b3aa'){throw 'Fourth-Horse diagnostic lacks its exact third inherited Horse.'}
      foreach($key in @('controlsSettled','settingsAndSelectionRestored','nativeProcessesSettled','cleanupCustodySettled')){Assert-KmcChunk7Boolean $Observations.cleanup.$key $true}
    }
}
function Assert-KmcChunk7ThirdHorseManualEvidence($Request,$Observations){
    Assert-KmcChunk7ThirdHorseSetupEvidence $Request $Observations
    $o=$Observations;$s=$o.thirdHorseSetup;$m=$o.thirdHorseManual;$w=$o.nativeWrite;$q=$o.saveRequested
    Assert-KmcExactProperties $m @('contract','disclosedAssistance','acquisitionProof','setupCompletedBeforeSave','boundary','arrangementAfterSave','headerStarting','headerStaged','workerObservedRunning','completionSequence','fieldActorIds') 'third-Horse Manual receipt'
    $fourth=Test-KmcChunk7OwnedPairsFourthSave $Request
    $name=Get-KmcChunk7OwnedManualOutputName $Request
    Assert-KmcBootstrapInventoryString $m.contract $(if($fourth){'three-horse-source-fourth-native-horse-manual-v1'}else{'small-third-horse-manual-producer-v1'})
    foreach($key in @('disclosedAssistance','setupCompletedBeforeSave')){Assert-KmcChunk7Boolean $m.$key $true}
    Assert-KmcChunk7Boolean $m.acquisitionProof $false
    if($m.workerObservedRunning-isnot[bool]){throw 'Third-Horse native worker observation is missing.'}
    Assert-KmcOrdinaryInteger $m.completionSequence 1 1
    Assert-KmcChunk7Boolean $o.cleanup.nativeSaveWritten $true
    Assert-KmcExactProperties $q @('nativeType','nativeName','frame','gameTicks','snapshotCount','nativeSaveAllowed','newSlot','levelUpSave','api') 'third-Horse native Manual request'
    Assert-KmcBootstrapInventoryString $q.nativeType 'Manual';Assert-KmcBootstrapInventoryString $q.nativeName $name
    Assert-KmcBootstrapInventoryString $q.api 'Game.SaveGame(SaveManager.CreateNewSave(name))'
    Assert-KmcOrdinaryInteger $q.snapshotCount 0 0
    foreach($key in @('nativeSaveAllowed','newSlot')){Assert-KmcChunk7Boolean $q.$key $true};Assert-KmcChunk7Boolean $q.levelUpSave $false
    Assert-KmcChunk7OwnedOutputEvidence $Request $o
    $leaf=Get-KmcChunk7OwnedOutputLeaf $Request $o
    Assert-KmcExactProperties $w @('path','sha256','length','lastWriteTimeUtcTicks','fileName','nativeType','nativeName','gameId','gameName','area','nativeCallback','operation','snapshotCount','readKind','sourceSchema','archive') 'third-Horse native Manual write'
    if($w.sha256-isnot[string]-or$w.sha256-cnotmatch'^[a-f0-9]{64}$'-or$w.sha256-ceq$Request.persistenceLoad.sha256){throw 'Third-Horse native Manual hash is missing or aliases the source.'}
    Assert-KmcOrdinaryInteger $w.length 1 268435456;Assert-KmcOrdinaryInteger $w.lastWriteTimeUtcTicks 1 ([DateTime]::MaxValue.Ticks)
    foreach($pair in @(@('fileName',$leaf),@('nativeType','Manual'),@('nativeName',$name),@('gameId',$Request.persistenceLoad.gameId),@('gameName',$Request.persistenceLoad.gameName),@('area',$Request.persistenceLoad.area),@('operation','None'),@('readKind','Current'))){Assert-KmcBootstrapInventoryString $w.($pair[0]) $pair[1]}
    Assert-KmcChunk7Boolean $w.nativeCallback $true;Assert-KmcOrdinaryInteger $w.snapshotCount 1 1;Assert-KmcOrdinaryInteger $w.sourceSchema 3 3
    $a=$w.archive
    Assert-KmcOrdinaryInteger $a.SchemaVersion 3 3
    Assert-KmcBootstrapInventoryString $a.CampaignId $Request.persistenceLoad.gameId;Assert-KmcBootstrapInventoryString $a.AreaId $Request.persistenceLoad.area
    foreach($key in @('Pairs','Participations','Slots')){if($a.$key-isnot[Array]-or$a.$key.Count-ne0){throw ('Third-Horse unmounted save carries unexpected '+$key+'.')}}
    if($null-ne$a.Combat){throw 'Third-Horse unmounted save carries combat ownership.'}
    if((ConvertTo-KmcChunk7Canonical $m.boundary)-cne(ConvertTo-KmcChunk7Canonical $m.arrangementAfterSave)){throw 'Native Manual changed the third-Horse field arrangement.'}
    if($m.boundary.actors-isnot[Array]-or$m.boundary.actors.Count-ne$(if($fourth){9}else{8})){throw 'Horse save boundary lacks the exact field roster.'}
    $ids=@(@($Request.ownedPairs.partyIds)+@($s.horseId)|Sort-Object -CaseSensitive)
    if((ConvertTo-KmcChunk7Canonical @($m.boundary.actors.actorId|Sort-Object -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical $ids)-or
       (ConvertTo-KmcChunk7Canonical @($m.fieldActorIds|Sort-Object -CaseSensitive))-cne(ConvertTo-KmcChunk7Canonical $ids)){throw 'Third-Horse Manual field does not retain the exact source seven and native third Horse.'}
    $sourceRows=@($m.boundary.actors|Where-Object{$_.actorId-cin$Request.ownedPairs.partyIds}|Sort-Object actorId -CaseSensitive)
    if((ConvertTo-KmcChunk7Canonical $sourceRows)-cne(ConvertTo-KmcChunk7Canonical @($s.after.actors|Sort-Object actorId -CaseSensitive))){throw 'Third-Horse Manual changed source actor progression before its native save boundary.'}
    $newHorse=@($m.boundary.actors|Where-Object actorId -CEQ $s.horseId)
    if($newHorse.Count-ne1-or$newHorse[0].blueprint-cne$s.nativeHorse.blueprint){throw 'Third-Horse Manual lost the exact new native Horse blueprint.'}
    $r=@($m.boundary.actors|Where-Object actorId -CEQ $s.targetRiderId)
    if($r.Count-ne1-or$r[0].characterLevel-ne8-or$r[0].experience-ne51000){throw 'Third-Horse save boundary lost the native Ranger progression.'}
    foreach($h in @(@($m.headerStarting,'native-header-starting',0),@($m.headerStaged,'native-header-staged',1))){
        $v=$h[0];Assert-KmcExactProperties $v @('label','frame','requestedIsSelectedDescriptor','preparedIsCopy','preparedLeaf','preparedName','preparedType','preparedPath','snapshots','activeOwners','nativeWrites','serializationBusy') 'third-Horse native header'
        Assert-KmcBootstrapInventoryString $v.label $h[1];Assert-KmcBootstrapInventoryString $v.preparedLeaf $leaf
        Assert-KmcBootstrapInventoryString $v.preparedName $name;Assert-KmcBootstrapInventoryString $v.preparedType 'Manual'
        Assert-KmcBootstrapInventoryString $v.preparedPath $w.path;Assert-KmcOrdinaryInteger $v.snapshots $h[2] $h[2]
        Assert-KmcOrdinaryInteger $v.activeOwners 0 0;Assert-KmcOrdinaryInteger $v.nativeWrites 0 1
        foreach($key in @('requestedIsSelectedDescriptor','preparedIsCopy')){Assert-KmcChunk7Boolean $v.$key $true}
        if($v.serializationBusy-isnot[bool]){throw 'Third-Horse header lacks native serialization state.'}
    }
    if($m.headerStarting.frame-lt$q.frame-or$m.headerStaged.frame-lt$m.headerStarting.frame){throw 'Third-Horse native save headers precede the request.'}
}
function Assert-KmcChunk7ArtifactEnvelope($Request,$Artifact,[string]$Kind) {
    Assert-KmcExactProperties $Artifact @('schemaVersion','evidenceKind','runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','createdAtUtc','status','observations','errors') 'Chunk 7 artifact'
    foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')) {
        if($Artifact.$field-isnot[string]-or$Artifact.$field-cne$Request.$field){throw "Chunk 7 artifact identity differs: $field"}
    }
    $date=[DateTimeOffset]::MinValue
    if(-not(Test-KmcExactJsonInteger $Artifact.schemaVersion)-or$Artifact.schemaVersion-ne1-or$Artifact.evidenceKind-cne$Kind-or$Artifact.status-cnotin@('PASS','FAIL')-or
        $Artifact.errors-isnot[Array]-or-not[DateTimeOffset]::TryParse([string]$Artifact.createdAtUtc,[ref]$date)){throw 'Chunk 7 artifact envelope differs.'}
    if($Artifact.status-ceq'PASS'-and@($Artifact.errors).Count-ne0){throw 'PASS artifact contains errors.'}
}
function Read-KmcChunk7ManifestArtifact($Request,$Manifest,[string]$Leaf,[string]$Kind) {
    $records=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq$Leaf-or$_.kind-ceq$Kind})
    if($records.Count-ne1-or$records[0].relativePath-cne$Leaf-or$records[0].kind-cne$Kind){throw 'Exact native artifact manifest binding is required.'}
    $path=Assert-KmcChildPath (Join-Path $Request.evidenceRoot $Leaf) $Request.evidenceRoot 'Chunk 7 artifact'
    Assert-KmcNotReparsePoint $path 'Chunk 7 artifact'; Assert-KmcNotHardLink $path 'Chunk 7 artifact'
    $before=Get-Item -LiteralPath $path -Force
    if($before.Length-ne[long]$records[0].length-or(Get-KmcSha256 $path)-cne$records[0].sha256){throw 'Native artifact differs from manifest bytes.'}
    Assert-KmcManifestJsonMembersUnique $path 'Chunk 7 native artifact'
    $artifact=Read-KmcJson $path
    if((Get-KmcSha256 $path)-cne$records[0].sha256-or(Get-Item -LiteralPath $path).LastWriteTimeUtc.Ticks-ne$before.LastWriteTimeUtc.Ticks){throw 'Native artifact changed during read.'}
    Assert-KmcChunk7ArtifactEnvelope $Request $artifact $Kind
    return $artifact
}
function Test-KmcChunk7FiniteNumber($Value) {
    if($Value-isnot[byte]-and$Value-isnot[int16]-and$Value-isnot[int32]-and$Value-isnot[int64]-and$Value-isnot[single]-and$Value-isnot[double]-and$Value-isnot[decimal]){return $false}
    return -not[double]::IsNaN([double]$Value)-and-not[double]::IsInfinity([double]$Value)
}
function Assert-KmcChunk7Boolean($Value,[bool]$Expected) {if($Value-isnot[bool]-or$Value-ne$Expected){throw 'Native evidence boolean is missing, mistyped or differs.'}}
# Native counters require actual integer tokens; identity hashes may be signed.
function Assert-KmcChunk7WorldCounters($Value,[string[]]$Fields) {
    foreach($field in $Fields){if(-not(Test-KmcExactJsonInteger $Value.$field)-or$Value.$field-lt0){throw ('Native world counter is missing, mistyped or negative: '+$field)}}
}
function Assert-KmcChunk7WorldInt32($Value) {
    if(-not(Test-KmcExactJsonInteger $Value)-or$Value-lt[int32]::MinValue-or$Value-gt[int32]::MaxValue){throw 'Native signed Int32 observation is missing, mistyped or outside its native bound.'}
}
# Native identity forms (observed in 20261017-c7b-world21-ownerseed-b2s3-twopair, and fixed by
# the producer): a pair-owner record keeps its rider/mount ids as the registry key, which
# MountedPairRegistry.Key normalizes to the compact lowercase Guid "N" form, while actor
# UniqueIds (party rows, selection, door/command executors) are the lowercase hyphenated Guid
# "D" form. Each side must match its own exact native pattern before the two are compared by
# Guid value; uppercase, braces, whitespace, the other side's form or a non-Guid are refused.
function ConvertFrom-KmcChunk7OwnerActorId($Value) {
    if($Value-isnot[string]-or$Value-cnotmatch'\A[0-9a-f]{32}\z'){throw 'Pair-owner actor identity is not the native compact lowercase Guid (N) form.'}
    [Guid]::ParseExact($Value,'N')
}
function ConvertFrom-KmcChunk7ActorUniqueId($Value) {
    if($Value-isnot[string]-or$Value-cnotmatch'\A[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\z'){throw 'Native actor UniqueId is not the lowercase hyphenated Guid (D) form.'}
    [Guid]::ParseExact($Value,'D')
}
# A pair (owner) id itself appears natively in exactly two forms: the world capture's
# owner.Id.ToString() ("D") and the archive's SavedMountedPair.PairId owner.Id.ToString("N").
# Only those two lowercase forms are accepted; the result is the "N" text for comparison.
function ConvertTo-KmcChunk7PairIdN($Value) {
    if($Value-is[string]-and$Value-cmatch'\A[0-9a-f]{32}\z'){return $Value}
    if($Value-is[string]-and$Value-cmatch'\A[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\z'){return [Guid]::ParseExact($Value,'D').ToString('N')}
    throw 'Pair id is not a native lowercase Guid in the capture (D) or archive (N) form.'
}
function Test-KmcChunk7OwnerActorMatch($OwnerValue,$ActorValue) {
    (ConvertFrom-KmcChunk7OwnerActorId $OwnerValue)-eq(ConvertFrom-KmcChunk7ActorUniqueId $ActorValue)
}
# -GuidValue: $Rows are pair-owner records (field in "N" form) and $Expected are actor
# UniqueIds ("D" form); the sets are compared by Guid value after both exact-form checks.
function Assert-KmcChunk7ExactIds($Rows,[string[]]$Expected,[string]$Field='id',[switch]$GuidValue) {
    $actual=@($Rows|ForEach-Object{if($GuidValue){(ConvertFrom-KmcChunk7OwnerActorId $_.$Field).ToString('N')}else{[string]$_.$Field}}|Sort-Object -CaseSensitive)
    $wanted=@($Expected|ForEach-Object{if($GuidValue){(ConvertFrom-KmcChunk7ActorUniqueId $_).ToString('N')}else{$_}}|Sort-Object -CaseSensitive)
    if(($actual-join'|')-cne($wanted-join'|')){throw 'Native actor identity set differs.'}
}
function Assert-KmcChunk7Resources($Before,$After,[string[]]$ActorIds) {
    Assert-KmcChunk7ExactIds @($Before.actors) $ActorIds
    Assert-KmcChunk7ExactIds @($After.actors) $ActorIds
    if(-not(Test-KmcExactJsonInteger $Before.gameTicks)-or-not(Test-KmcExactJsonInteger $After.gameTicks)){throw 'Native game time must be integer ticks.'}
    $elapsed=([double]$After.gameTicks-[double]$Before.gameTicks)/10000000.0
    if([double]::IsNaN($elapsed)-or[double]::IsInfinity($elapsed)-or$elapsed-lt0-or$elapsed-gt30){throw 'Native resource window time invalid.'}
    foreach($a in $Before.actors) {
        $b=@($After.actors|Where-Object id -CEQ $a.id)[0]
        foreach($field in @('move','standard','swift')) {
            if(-not(Test-KmcChunk7FiniteNumber $a.$field)-or-not(Test-KmcChunk7FiniteNumber $b.$field)){throw 'Native action window requires finite JSON numbers.'}
            $x=[double]$a.$field; $y=[double]$b.$field
            if([double]::IsNaN($x)-or[double]::IsInfinity($x)-or[double]::IsNaN($y)-or[double]::IsInfinity($y)-or$x-lt0-or$y-lt0-or
                [Math]::Abs($y-[Math]::Max([double]0,($x-$elapsed)))-gt0.03){throw 'Exploration control changed native action spending.'}
        }
        foreach($field in @('initiative','reactionAllowance')){Assert-KmcChunk7WorldInt32 $a.$field;Assert-KmcChunk7WorldInt32 $b.$field}
        if($a.reactionAllowance-ne$b.reactionAllowance){throw 'Exploration changed native reaction expenditure.'}
        if($a.prepared-isnot[bool]-or$b.prepared-isnot[bool]-or$a.prepared-ne$b.prepared-or$a.initiative-ne$b.initiative){throw 'Exploration changed native preparation/initiative.'}
    }
}
function Assert-KmcChunk7InputSequence($Rows,$Specs,[string[]]$ActorIds,$Commands,$Shells) {
    if(@($Rows).Count-ne@($Specs).Count){throw 'Exact native relationship input sequence incomplete.'}
    for($i=0;$i-lt@($Specs).Count;$i++) {
        $v=$Rows[$i];$s=$Specs[$i]
        Assert-KmcChunk7WorldCounters $v @('nativeCastDelta','nativeRefusalDelta','generationAtInit')
        Assert-KmcChunk7WorldInt32 $v.commandObject
        Assert-KmcChunk7Boolean $v.clicked $true;Assert-KmcChunk7Boolean $v.shellTerminal $true;Assert-KmcChunk7Boolean $v.processTerminal $true
        if($v.label-cne$s[0]-or$v.casterId-cne$s[1]-or$v.targetId-cne$s[2]-or$v.abilityGuid-cne$s[3]-or
            $v.nativeCastDelta-ne1-or$v.nativeRefusalDelta-ne0-or$v.commandObject-eq0-or-not$Commands.Add([long]$v.commandObject)-or
            [string]::IsNullOrEmpty($v.shellControl)-or-not$Shells.Add([string]$v.shellControl)-or$v.generationAtInit-lt0-or
            $v.selectionAfterInput-ceq$v.casterId-or$v.selectionAfterInput-cnotin$ActorIds){throw 'Native input command/shell/selection identity differs.'}
        Assert-KmcChunk7Resources $v.resourcesBefore $v.resourcesAfter $ActorIds
    }
}
function Get-KmcChunk7Subscriptions($Snapshot) {
    Assert-KmcChunk7Boolean $Snapshot.executing $false
    Assert-KmcChunk7WorldCounters $Snapshot @('uniqueOwnedSubscribers','ownedInterfaceEntries')
    foreach($entry in $Snapshot.entries){Assert-KmcChunk7WorldInt32 $entry.identity}
    $entries=@($Snapshot.entries|ForEach-Object{[string]$_.interface+'|'+[string]$_.type+'|'+[string]$_.identity}|Sort-Object -CaseSensitive)
    if($entries.Count-ne$Snapshot.ownedInterfaceEntries-or@($Snapshot.entries|Select-Object -ExpandProperty identity -Unique).Count-ne$Snapshot.uniqueOwnedSubscribers){throw 'Subscription counts differ from exact rows.'}
    return ($entries-join';')
}
function Assert-KmcChunk7Measurements($Rows) {
    $labels=@('unmounted-0','mounted-1','mounted-2','unmounted-after-cycles');$active=@(0,1,2,0)
    if(@($Rows).Count-ne4){throw 'All four comparable measurement windows are required.'}
    $subscriptions=$null
    for($i=0;$i-lt4;$i++) {
        $m=$Rows[$i]
        Assert-KmcChunk7WorldCounters $m @('warmupFrames','sampleFrames')
        Assert-KmcChunk7WorldInt32 $m.startFrame;Assert-KmcChunk7WorldInt32 $m.endFrame
        if($m.label-cne$labels[$i]-or$m.warmupFrames-ne30-or$m.sampleFrames-ne120-or
            @($m.serviceTicks).Count-ne120-or@($m.frameSeconds).Count-ne120-or$m.endFrame-$m.startFrame-lt119){throw 'Measurement sample window differs.'}
        if(-not(Test-KmcExactJsonInteger $m.stopwatchFrequency)-or$m.stopwatchFrequency-le0){throw 'Measurement clock frequency invalid.'}
        foreach($t in $m.serviceTicks){if(-not(Test-KmcExactJsonInteger $t)-or$t-lt0){throw 'Service timer sample invalid.'}}
        foreach($t in $m.frameSeconds){if(-not(Test-KmcChunk7FiniteNumber $t)-or$t-le0){throw 'Frame timer sample invalid.'}}
        foreach($field in @('managedHeapBefore','managedHeapAfter','managedHeapDelta','kmcLogMessages','kmcLogCharacters')){if(-not(Test-KmcExactJsonInteger $m.$field)){throw 'Measurement count invalid.'}}
        if($m.managedHeapBefore-lt0-or$m.managedHeapAfter-lt0-or$m.managedHeapDelta-ne($m.managedHeapAfter-$m.managedHeapBefore)-or$m.kmcLogMessages-lt0-or$m.kmcLogCharacters-lt0){throw 'Heap/log observations inconsistent.'}
        $sortedTicks=@($m.serviceTicks|Sort-Object);$sortedFrames=@($m.frameSeconds|Sort-Object)
        $expected=@{serviceMeanMs=([double]($m.serviceTicks|Measure-Object -Average).Average*1000.0/$m.stopwatchFrequency);
            serviceP50Ms=([double]$sortedTicks[59]*1000.0/$m.stopwatchFrequency);serviceP95Ms=([double]$sortedTicks[113]*1000.0/$m.stopwatchFrequency);
            frameP50Ms=([double]$sortedFrames[59]*1000.0);frameP95Ms=([double]$sortedFrames[113]*1000.0)}
        foreach($key in $expected.Keys){if(-not(Test-KmcChunk7FiniteNumber $m.$key)-or[Math]::Abs([double]$m.$key-$expected[$key])-gt0.001){throw 'Timing summary differs from raw samples.'}}
        if(@($m.collectionsBefore).Count-ne3-or@($m.collectionsAfter).Count-ne3){throw 'Native GC observations missing.'}
        for($g=0;$g-lt3;$g++){if(-not(Test-KmcExactJsonInteger $m.collectionsBefore[$g])-or-not(Test-KmcExactJsonInteger $m.collectionsAfter[$g])-or
            $m.collectionsBefore[$g]-lt0-or$m.collectionsAfter[$g]-lt$m.collectionsBefore[$g]){throw 'Native GC observations invalid.'}}
        Assert-KmcChunk7Boolean $m.forcedCollection $false
        foreach($owner in @($m.ownersBefore,$m.ownersAfter)) {
            Assert-KmcChunk7WorldCounters $owner @('active','owners','boundaries','failedConstructions','participants','liveControlBoundaries','preparedOrViewResidue','allocationActors')
            if($owner.active-ne$active[$i]-or$owner.owners-ne$active[$i]-or$owner.boundaries-lt1-or$owner.boundaries-gt2-or
                $owner.participants-ne$owner.boundaries-or$owner.failedConstructions-ne0-or$owner.liveControlBoundaries-ne0-or$owner.preparedOrViewResidue-ne$active[$i]){throw 'Comparable window contains unexpected ownership, commands or view residue.'}
        }
        $before=Get-KmcChunk7Subscriptions $m.subscriptionsBefore;$after=Get-KmcChunk7Subscriptions $m.subscriptionsAfter
        if($before-cne$after-or($null-ne$subscriptions-and$before-cne$subscriptions)){throw 'Settled windows accumulated or lost owned native subscriptions.'}
        $subscriptions=$before
    }
    # Timing values are observations, not an invented universal performance threshold.
}
function Assert-KmcChunk7DoorEvidence($Door,$Owners,[string[]]$DeclaredRiders=@('b6628a77-4962-47a4-a17c-88d9836fc9d5','d17c8fd0-6627-40c4-bf9a-024cf001c1f7')) {
    Assert-KmcExactProperties $Door @('contract','interactions','setupInteractions','fixture','firstPairReturn','restoration','completed') 'Chunk 7 native door proof'
    if($Door.contract-cne'two-pair-native-door-click-selection-change'-or$Door.interactions-isnot[Array]-or$Door.interactions.Count-ne2){throw 'Exactly two captured native door interactions required.'}
    Assert-KmcChunk7Boolean $Door.completed $true
    Assert-KmcChunk7WorldCounters $Door @('setupInteractions')
    $f=$Door.fixture
    # Only the current fixture shape, with its clickability, restriction, trigger and unlock
    # records, is admitted: no retained artifact carries a native door proof in the earlier
    # shape, so none needs it (census of runtime-evidence, 2026-10-23).
    $base=@('name','object','position','originalOpen','originalEnabled','originalCut','disableOnOpen','radius')
    Assert-KmcExactProperties $f ($base+(Get-KmcChunk7DoorClickFields|ForEach-Object{if($_-ceq'alreadyUnlocked'){'originalAlreadyUnlocked'}else{$_}})) 'captured door fixture'
    Assert-KmcChunk7WorldInt32 $f.object
    if($f.object-eq0-or$f.name-isnot[string]-or[string]::IsNullOrWhiteSpace($f.name)-or$f.name.Length-gt512){throw 'Native door identity is missing.'}
    # An open door is closed as setup before each input; a door captured closed only before the second.
    if($f.originalOpen-isnot[bool]){throw 'Door fixture flags must be native booleans.'}
    Assert-KmcChunk7Boolean $f.originalCut (-not$f.originalOpen)
    foreach($key in @('interactable','availableInteractions','revealed','hasVisibleTrap','trapActive','originalAlreadyUnlocked','hasDialog','dataPresent','viewTriggerActions')){if($f.$key-isnot[bool]){throw 'Door clickability flags must be native booleans.'}}
    Assert-KmcOrdinaryInteger $f.restrictionCount 0 64;Assert-KmcOrdinaryInteger $f.viewTriggerCount 0 64
    # Every captured door: unrestricted, no door-view trigger actions, persistent data present.
    if(-not(Test-KmcChunk7DoorGate $f -Fixture)){throw 'Captured door carries a restriction, door-view trigger actions or no persistent data.'}
    if(-not$f.originalOpen){
        if(-not(Test-KmcChunk7DoorClosedClickable $f)){throw 'A closed door start must be one ordinary player input could open.'}
        Assert-KmcChunk7Boolean $f.originalEnabled $true
    }
    if($Door.setupInteractions-ne$(if($f.originalOpen){2}else{1})){throw 'Fixture preparation must be distinct from the two native player inputs.'}
    foreach($v in @($f.originalEnabled,$f.disableOnOpen)){if($v-isnot[bool]){throw 'Door fixture flags must be native booleans.'}}
    foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $f.position.$axis)){throw 'Door position is not finite.'}}
    if(-not(Test-KmcChunk7FiniteNumber $f.radius)-or$f.radius-le0){throw 'Native interaction radius is invalid.'}
    $riders=@($DeclaredRiders);if($riders.Count-ne2-or$riders[0]-ceq$riders[1]){throw 'Exact distinct declared door riders required.'};foreach($id in $riders){Assert-KmcOrdinaryGuid $id}
    $commands=[Collections.Generic.HashSet[long]]::new()
    for($i=0;$i-lt2;$i++) {
        $v=$Door.interactions[$i];$owner=@($Owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $riders[$i]})
        Assert-KmcExactProperties $v @('pair','riderId','mountId','ownerId','generation','commandObject','executorId','distanceBefore','radius','selectionAfterInput','accepted','unrelatedIdleAtInput','outcome','terminal','ownerContinuity','unrelatedIdleAtCompletion','distanceMoved') 'native mounted door input'
        Assert-KmcChunk7WorldCounters $v @('pair','generation');Assert-KmcChunk7WorldInt32 $v.commandObject
        if($owner.Count-ne1-or$v.pair-ne$i-or-not(Test-KmcChunk7OwnerActorMatch $owner[0].riderId $v.riderId)-or-not(Test-KmcChunk7OwnerActorMatch $owner[0].mountId $v.mountId)-or
            $v.ownerId-cne$owner[0].ownerId-or$v.generation-ne$owner[0].generation-or$v.executorId-cne$v.riderId-or
            $v.selectionAfterInput-cne$riders[1-$i]-or$v.commandObject-eq0-or-not$commands.Add([long]$v.commandObject)){throw 'Door command lost its captured pair, generation, executor or changed selection.'}
        foreach($flag in @('accepted','unrelatedIdleAtInput','terminal','ownerContinuity','unrelatedIdleAtCompletion')){Assert-KmcChunk7Boolean $v.$flag $true}
        foreach($field in @('distanceBefore','radius','distanceMoved')){if(-not(Test-KmcChunk7FiniteNumber $v.$field)){throw 'Native door movement observation must be finite.'}}
        if($v.radius-ne$f.radius-or$v.distanceBefore-le($f.radius+0.5)-or$v.distanceBefore-gt28-or$v.distanceMoved-lt0.5){throw 'Door input did not prove bounded distant approach.'}
        $o=$v.outcome
        Assert-KmcExactProperties $o @('Result','RiderId','MountId','InteractionCount','DelegatedMoveStartCount','DelegatedMoveTickCount','DoorStateChanged','RiderPathSuppressed','MountMoveSlotRestored','TerminalReason') 'native door terminal outcome'
        Assert-KmcChunk7WorldCounters $o @('InteractionCount','DelegatedMoveStartCount','DelegatedMoveTickCount')
        if($o.Result-cne'Success'-or$o.RiderId-cne$v.riderId-or$o.MountId-cne$v.mountId-or$o.InteractionCount-ne1-or$o.DelegatedMoveStartCount-ne1-or$null-ne$o.TerminalReason){throw 'Door outcome did not report exactly one successful native interaction and approach.'}
        foreach($flag in @('DoorStateChanged','RiderPathSuppressed','MountMoveSlotRestored')){Assert-KmcChunk7Boolean $o.$flag $true}
    }
    $r=$Door.firstPairReturn
    Assert-KmcExactProperties $r @('command','executorId','terminal','result','distanceToStart') 'native first-pair return'
    Assert-KmcChunk7WorldInt32 $r.command;Assert-KmcChunk7Boolean $r.terminal $true
    $first=@($Owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $riders[0]})[0]
    if($r.command-eq0-or-not$commands.Add([long]$r.command)-or-not(Test-KmcChunk7OwnerActorMatch $first.mountId $r.executorId)-or$r.result-cne'Success'-or
        -not(Test-KmcChunk7FiniteNumber $r.distanceToStart)-or$r.distanceToStart-lt0-or$r.distanceToStart-gt1.25){throw 'First pair did not return through its own native mount command.'}
    $s=$Door.restoration
    Assert-KmcExactProperties $s @('exact','commandsSettled','open','enabled','cutEnabled','observedAlreadyUnlockedBeforeRestore','alreadyUnlocked') 'native door restoration'
    foreach($flag in @('exact','commandsSettled')){Assert-KmcChunk7Boolean $s.$flag $true}
    Assert-KmcChunk7Boolean $s.open $f.originalOpen;Assert-KmcChunk7Boolean $s.enabled $f.originalEnabled;Assert-KmcChunk7Boolean $s.cutEnabled $f.originalCut
    Assert-KmcChunk7Boolean $s.alreadyUnlocked $f.originalAlreadyUnlocked
    # Before restoration the flag is either unchanged, or set by native interaction on an
    # unrestricted door that was captured locked-free and not yet marked unlocked.
    if($s.observedAlreadyUnlockedBeforeRestore-isnot[bool]-or
       -not($s.observedAlreadyUnlockedBeforeRestore-eq$f.originalAlreadyUnlocked-or
            ($s.observedAlreadyUnlockedBeforeRestore-and$f.restrictionCount-eq0-and-not$f.originalAlreadyUnlocked))){throw 'Observed unlock state before restoration is inconsistent with native interaction.'}
}

# The probe's one door predicate (Chunk7NativeDoorProbe.DoorFacts), mirrored for readers.
# The term lists are tied to DoorFacts.ClosedClickable and DoorFacts.Eligible by a source test.
function Get-KmcChunk7DoorClickFields {@('interactable','availableInteractions','revealed','hasVisibleTrap','trapActive','alreadyUnlocked','restrictionCount','hasDialog','dataPresent','viewTriggerCount','viewTriggerActions')}
function Get-KmcChunk7DoorClosedClickableTerms {@('interactable','availableInteractions','revealed','!hasVisibleTrap','!trapActive','!hasDialog')}
function Get-KmcChunk7DoorGateTerms {@('active','dataPresent','disableCutWhenOpen','cutPresent','restrictionCount==0','!viewTriggerActions')}
function Test-KmcChunk7DoorTerms($Value,[string[]]$Terms){
    foreach($term in $Terms){
        if($term.EndsWith('==0',[StringComparison]::Ordinal)){if($Value.($term.Substring(0,$term.Length-3))-ne0){return $false};continue}
        $negated=$term.StartsWith('!',[StringComparison]::Ordinal);$flag=[bool]$Value.($term.TrimStart('!'))
        if($flag-eq$negated){return $false}
    }
    $true
}
function Test-KmcChunk7DoorClosedClickable($Value){Test-KmcChunk7DoorTerms $Value (Get-KmcChunk7DoorClosedClickableTerms)}
# A captured fixture records no active/cut flags (the probe requires them before capture); rows do.
function Test-KmcChunk7DoorGate($Value,[switch]$Fixture){
    $terms=@(Get-KmcChunk7DoorGateTerms);if($Fixture){$terms=@($terms|Where-Object{$_-cnotin@('active','disableCutWhenOpen','cutPresent')})}
    Test-KmcChunk7DoorTerms $Value $terms
}
# Bounded nearest-first native door rows. A legacy row (before the closed-door predicate)
# keeps its open-only eligibility; a current row carries the clickability flags.
function Assert-KmcChunk7NativeDoorRows($Rows,$Mounts,[int]$CandidateCount,[string]$Label,[switch]$CurrentOnly){
    if($Rows-isnot[Array]-or$Rows.Count-ne[Math]::Min(32,$CandidateCount)){throw ($Label+' rows do not match the bounded native candidate count.')}
    $base=@('name','position','active','open','disableCutWhenOpen','cutPresent','proximityRadius','mountDistances')
    # One shape per inventory. A legacy row (no clickability flags) is admitted only when it is
    # ineligible whatever those flags would say, so it can never hide a qualifying closed door.
    $legacyRows=@($Rows|Where-Object{$null-eq$_.PSObject.Properties['revealed']}).Count
    if($legacyRows-ne0-and$legacyRows-ne$Rows.Count){throw ($Label+' inventory mixes legacy and current row shapes.')}
    $prior=-1.0
    foreach($row in $Rows){
        $legacy=$legacyRows-ne0
        if($legacy-and$CurrentOnly){throw ($Label+' row lacks its clickability flags.')}
        Assert-KmcExactProperties $row $(if($legacy){$base+@('eligible')}else{$base+(Get-KmcChunk7DoorClickFields)+@('eligible')}) $Label
        if($row.name-isnot[string]-or[string]::IsNullOrWhiteSpace($row.name)-or$row.name.Length-gt256-or
           $row.mountDistances-isnot[Array]-or$row.mountDistances.Count-ne2-or
           -not(Test-KmcChunk7FiniteNumber $row.proximityRadius)-or[double]$row.proximityRadius-le0){throw ($Label+' identity or radius is malformed.')}
        foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $row.position.$axis)){throw ($Label+' position is nonfinite.')}}
        for($i=0;$i-lt2;$i++){
            $start=$Mounts[$i].position
            $distance=[Math]::Sqrt([Math]::Pow([double]$start.x-[double]$row.position.x,2)+[Math]::Pow([double]$start.z-[double]$row.position.z,2))
            if(-not(Test-KmcChunk7FiniteNumber $row.mountDistances[$i])-or
               [Math]::Abs($distance-[double]$row.mountDistances[$i])-gt0.001){throw ($Label+' distance differs from mounted actor geometry.')}
        }
        $sum=[double]$row.mountDistances[0]+[double]$row.mountDistances[1]
        if($sum+0.001-lt$prior){throw ($Label+' inventory is not nearest-first.')};$prior=$sum
        foreach($key in @('active','open','disableCutWhenOpen','cutPresent','eligible')){if($row.$key-isnot[bool]){throw ($Label+' state is not Boolean.')}}
        $distancesOk=@($row.mountDistances|Where-Object{[double]$_-le[double]$row.proximityRadius+0.5-or[double]$_-gt28.0}).Count-eq0
        if($legacy){
            if($row.active-and$row.disableCutWhenOpen-and$row.cutPresent-and$distancesOk){throw ($Label+' legacy row could hide a qualifying door; its clickability flags are required.')}
            $eligible=$false
        }
        else{
            foreach($key in @('interactable','availableInteractions','revealed','hasVisibleTrap','trapActive','alreadyUnlocked','hasDialog','dataPresent','viewTriggerActions')){if($row.$key-isnot[bool]){throw ($Label+' clickability flag is not Boolean.')}}
            Assert-KmcOrdinaryInteger $row.restrictionCount 0 64;Assert-KmcOrdinaryInteger $row.viewTriggerCount 0 64
            $eligible=(Test-KmcChunk7DoorGate $row)-and($row.open-or(Test-KmcChunk7DoorClosedClickable $row))-and$distancesOk
        }
        Assert-KmcChunk7Boolean $row.eligible $eligible
    }
}

function Assert-KmcChunk7QuietWindow($Window,$Move,$ExpectedOwners,[string[]]$ActorIds,[ValidateSet(2,6)][int]$Residents=2) {
    Assert-KmcExactProperties $Window @('contract','label','startFrame','endFrame','startSeconds','endSeconds','minimumFrames','maximumFrames','minimumSeconds','maximumSeconds','telemetryBefore','telemetryAfter','measurementBeforePresent','measurementAfterPresent','fullyInert','performanceProof','allocationRateProof','remainingObservers','gateReleased','before','after') 'Bounded optional-capture quiet window'
    foreach($field in @('startFrame','endFrame','minimumFrames','maximumFrames')){Assert-KmcChunk7WorldInt32 $Window.$field}
    foreach($field in @('startSeconds','endSeconds','minimumSeconds','maximumSeconds')){if(-not(Test-KmcChunk7FiniteNumber $Window.$field)-or$Window.$field-lt0){throw 'Quiet window requires finite native clock observations.'}}
    if($Window.contract-cne'chunk7-optional-capture-quiet-window-v1'-or$Window.label-cne$Move.label-or
       $Window.minimumFrames-ne12-or$Window.maximumFrames-ne1800-or$Window.minimumSeconds-ne0.25-or$Window.maximumSeconds-ne15.0-or
       $Window.startFrame-lt0-or$Window.endFrame-$Window.startFrame-lt12-or$Window.endFrame-$Window.startFrame-gt1800-or
       $Window.endSeconds-$Window.startSeconds-lt0.25-or$Window.endSeconds-$Window.startSeconds-gt15.0){throw 'Quiet movement lacks its exact finite multi-frame window.'}
    foreach($flag in @('measurementBeforePresent','measurementAfterPresent','fullyInert','performanceProof','allocationRateProof')){Assert-KmcChunk7Boolean $Window.$flag $false}
    Assert-KmcChunk7Boolean $Window.gateReleased $true
    if($Window.remainingObservers-isnot[Array]-or($Window.remainingObservers-join'|')-cne'guarded-host-heartbeat|exact-native-world-guard|production-lifecycle-records|native-command-settlement-polling|entry-exit-snapshots'){throw 'Quiet evidence must name the retained safety/entry-exit observers.'}
    foreach($t in @($Window.telemetryBefore,$Window.telemetryAfter)) {
        Assert-KmcExactProperties $t @('lastSampleSeconds','sampleSequence','fileCreated','samplingIntervalSeconds','clockSeconds') 'Exact optional telemetry owner state'
        Assert-KmcChunk7WorldCounters $t @('sampleSequence')
        if($t.fileCreated-isnot[bool]){throw 'Optional telemetry creation state must be boolean.'}
        foreach($field in @('lastSampleSeconds','samplingIntervalSeconds','clockSeconds')){if(-not(Test-KmcChunk7FiniteNumber $t.$field)-or$t.$field-lt0){throw 'Optional telemetry timer must be a finite number.'}}
        if($t.samplingIntervalSeconds-le0-or$t.samplingIntervalSeconds-gt1-or$t.lastSampleSeconds-gt$t.clockSeconds){throw 'Optional telemetry timer/interval is inconsistent.'}
    }
    $a=$Window.telemetryBefore;$b=$Window.telemetryAfter
    foreach($field in @('lastSampleSeconds','sampleSequence','fileCreated','samplingIntervalSeconds')){if($a.$field-ne$b.$field){throw 'Optional telemetry sampling advanced while its exact writer was gated.'}}
    $requiredSeconds=[Math]::Max(0.25,2.0*$a.samplingIntervalSeconds)
    if($Window.endSeconds-$Window.startSeconds-lt$requiredSeconds-or$b.clockSeconds-$a.clockSeconds-lt$requiredSeconds){throw 'Unchanged telemetry needs both real clocks to span the exact captured sampling bound, not merely zero rows.'}
    for($phase=0;$phase-lt2;$phase++) {
        $w=if($phase-eq0){$Window.before}else{$Window.after}
        $label=if($phase-eq0){'quiet-before-'}else{'quiet-after-'}
        $frame=if($phase-eq0){$Window.startFrame}else{$Window.endFrame}
        Assert-KmcChunk7WorldCounters $w @('frame','boundaryCount','participantCount')
        if($w.label-cne($label+$Move.label)-or$w.frame-ne$frame-or$w.boundaryCount-ne$Residents-or$w.participantCount-ne$Residents-or
           $w.selection-isnot[Array]-or$w.selection.Count-ne1-or$w.selection[0]-cne$Move.selectionAfterInput-or$w.owners-isnot[Array]){throw 'Quiet entry/exit did not observe the exact selected world boundary.'}
        $active=@($w.owners|Where-Object state -CEQ 'Active')
        if($active.Count-ne@($ExpectedOwners).Count){throw 'Quiet movement changed active owner cardinality.'}
        foreach($expected in $ExpectedOwners) {
            $owned=@($active|Where-Object ownerId -CEQ $expected.ownerId)
            if($owned.Count-ne1){throw 'Quiet movement lost or duplicated an exact owner.'}
            Assert-KmcChunk7WorldCounters $owned[0] @('generation')
            if($owned[0].riderId-cne$expected.riderId-or$owned[0].mountId-cne$expected.mountId-or$owned[0].generation-ne$expected.generation-or$owned[0].relationshipState-cne'Mounted'){throw 'Quiet movement rebound actors/generation or lost mounted presentation.'}
        }
    }
    Assert-KmcChunk7Resources $Window.before.resources $Window.after.resources $ActorIds
    if((Get-KmcChunk7Subscriptions $Window.before.subscriptions)-cne(Get-KmcChunk7Subscriptions $Window.after.subscriptions)){throw 'Quiet window grew or lost native subscriptions.'}
}

function Assert-KmcChunk7WorldEvidence($Request,$Artifact,[string]$Status) {
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'chunk7-world-evidence'
    if((Get-KmcChunk7SelectedScenario $Request)-cne'chunk7-two-pair-exploration'){throw 'Chunk 7 world evidence under another scenario.'}
    if($Status-ceq'PASS'-and$Artifact.status-cne'PASS'){throw 'PASS world result requires PASS artifact.'}
    if($Artifact.status-cne'PASS'){return}
    if(Test-KmcChunk7SixPairStress $Request){Assert-KmcChunk7SixPairStressEvidence $Request $Artifact.observations;return}
    if((Test-KmcChunk7OwnedPairsScenario $Request.scenario)-and((Test-KmcChunk7OwnedPairsSmallThirdHorseSave $Request)-or(Test-KmcChunk7OwnedPairsFourthSave $Request))){
        Assert-KmcChunk7ThirdHorseManualEvidence $Request $Artifact.observations;return
    }
    if((Test-KmcChunk7OwnedPairsScenario $Request.scenario)-and(Test-KmcChunk7OwnedPairsSmallThirdHorse $Request)){
        Assert-KmcChunk7ThirdHorseSetupEvidence $Request $Artifact.observations;return
    }
    if((Test-KmcChunk7OwnedPairsScenario $Request.scenario)-and(Test-KmcChunk7OwnedPairsFourthHorse $Request)){
        Assert-KmcChunk7ThirdHorseSetupEvidence $Request $Artifact.observations;return
    }
    # Owner-seed-derived Manual producer: setup, two native Mounts and its one native Manual only.
    if((Test-KmcChunk7OwnedPairsScenario $Request.scenario)-and(Test-KmcChunk7OwnedPairsSeedManual $Request)){Assert-KmcChunk7OwnerSeedManualWorldEvidence $Request $Artifact.observations;return}
    $o=$Artifact.observations;$owned=Test-KmcChunk7OwnedPairsScenario $Request.scenario;$seed=$owned-and(Test-KmcChunk7OwnedPairsOwnerSeed $Request)
    if($seed){
        # Owner-seed kind: the disclosed fixture setup supplies both Horses; no inherited or Working provenance.
        $roles=Assert-KmcChunk7OwnerSeedWorldSetup $Request $o;$first=$roles[0];$mammoth=$roles[1];$second=$roles[2];$horse=$roles[3];$actors=@($roles)
        $original=@(@($Request.ownedPairs.partyIds)+@($mammoth,$horse))
        foreach($snapshot in @($o.snapshots)+@($o.cycles)){foreach($owner in $snapshot.owners){Assert-KmcChunk7WorldCounters $owner @('generation')}}
    }elseif($owned){
        Assert-KmcChunk7InheritedEvidence $Request $o
        $roles=Get-KmcChunk7OwnedPairIds $Request;$first=$roles[0];$mammoth=$roles[1];$second=$roles[2];$horse=$roles[3];$actors=@($roles);$original=@($Request.ownedPairs.partyIds)
        Assert-KmcChunk7Boolean $o.capability.archiveNativeEnabled $true;Assert-KmcChunk7WorldCounters $o.capability @('nativeWriterSchema','nativePairCapacity')
        if($o.capability.nativeWriterSchema-ne3-or$o.capability.nativePairCapacity-lt2-or$null-ne$o.capability.workingSha256){throw 'Inherited world capability differs or claims Working provenance.'}
        Assert-KmcBootstrapInventoryString $o.capability.inheritedManualSha256 $Request.ownedPairs.sourceArchiveSha256
        foreach($party in @($o.partyBefore,$o.partyWithHorse)){
            Assert-KmcChunk7ExactIds @($party) $original
            for($i=0;$i-lt2;$i++){$r=@($party|Where-Object id -CEQ $roles[$i*2]);$m=@($party|Where-Object id -CEQ $roles[$i*2+1]);if($r.Count-ne1-or$m.Count-ne1-or$r[0].petId-isnot[string]-or$m[0].masterId-isnot[string]-or$m[0].blueprint-isnot[string]-or$r[0].petId-cne$m[0].id-or$m[0].masterId-cne$r[0].id-or$m[0].blueprint-cne'4016c7db400ab721ff125aef9e65e202'){throw 'Inherited ordinary Horse reciprocity differs.'}}
        }
        foreach($snapshot in @($o.snapshots)+@($o.cycles)){foreach($owner in $snapshot.owners){Assert-KmcChunk7WorldCounters $owner @('generation')}}
    }else{
    $o=$Artifact.observations; Assert-KmcChunk7Boolean $o.capability.archiveNativeEnabled $true; $first='b6628a77-4962-47a4-a17c-88d9836fc9d5';$mammoth='d79a4f6c-b74e-4868-95bd-533899131acb';$second='d17c8fd0-6627-40c4-bf9a-024cf001c1f7'
    Assert-KmcChunk7WorldCounters $o.capability @('nativeWriterSchema','nativePairCapacity')
    $original=@($first,$mammoth,$second)
    if($o.capability.archiveNativeEnabled-ne$true-or$o.capability.nativeWriterSchema-ne3-or$o.capability.nativePairCapacity-lt2-or
        $o.capability.workingSha256-cne'5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'-or
        $o.capability.workingSha256-cne$Request.fixture.working.sha256){throw 'World native capability or fixture receipt differs.'}
    Assert-KmcChunk7ExactIds @($o.partyBefore) $original
    $horseRows=@($o.partyWithHorse|Where-Object blueprint -CEQ '4016c7db400ab721ff125aef9e65e202')
    if($horseRows.Count-ne1-or$horseRows[0].masterId-cne$second){throw 'Exact native Horse acquisition identity differs.'}
    $horse=[string]$horseRows[0].id;[Guid]$parsed=[Guid]::Empty
    if(-not[Guid]::TryParse($horse,[ref]$parsed)-or$horse-cin$original){throw 'Created Horse ID invalid or reused.'}
    $actors=@($original)+@($horse);Assert-KmcChunk7ExactIds @($o.partyWithHorse) $actors
    foreach($party in @($o.partyBefore,$o.partyWithHorse)) {
        $r=@($party|Where-Object id -CEQ $first)[0];$m=@($party|Where-Object id -CEQ $mammoth)[0]
        if($r.petId-cne$mammoth-or$m.masterId-cne$first-or$m.blueprint-cne'e7aa96d15a45238438ae4cfb476f6bb9'){throw 'Original Mammoth ownership changed.'}
    }
    $a=$o.acquisition
    Assert-KmcChunk7WorldCounters $a @('nativeRangerCommitCount','huntersBondSelectionLevel','rangerCompanionSelectionLevel','horseFactRankAtCommit')
    Assert-KmcChunk7Boolean $a.horsePresentAtNativeCommit $true
    foreach($snapshot in @($o.snapshots)+@($o.cycles)){foreach($owner in $snapshot.owners){Assert-KmcChunk7WorldCounters $owner @('generation')}}
    if($a.nativeRangerCommitCount-ne4-or$a.huntersBondSelectionLevel-ne4-or$a.rangerCompanionSelectionLevel-ne4-or$a.horseFactRankAtCommit-ne1-or
        $a.horsePresentAtNativeCommit-ne$true-or[string]::IsNullOrEmpty($a.horseFeatureSourceGuid)){throw 'Native Ranger acquisition proof incomplete.'}
    }
    $inputSpecs=@(@('mount-mammoth',$first,$mammoth,'f053faad986631688defa003cd7bda0e'),@('mount-horse',$second,$horse,'f053faad986631688defa003cd7bda0e'),
        @('dismount-mammoth',$first,$first,'3af2b81f4d72bbb30501fa730fcdf36e'),@('dismount-horse',$second,$second,'3af2b81f4d72bbb30501fa730fcdf36e'))
    $commands=New-Object 'Collections.Generic.HashSet[long]';$shells=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    Assert-KmcChunk7InputSequence $o.inputs $inputSpecs $actors $commands $shells
    $both=@($o.snapshots|Where-Object label -CEQ 'both-mounted');$after=@($o.snapshots|Where-Object label -CEQ 'movement-complete')
    if($both.Count-ne1-or$after.Count-ne1){throw 'Two-pair native world windows missing.'}
    $owners=@($both[0].owners|Where-Object state -CEQ 'Active')
    if($owners.Count-ne2-or$owners[0].ownerId-ceq$owners[1].ownerId){throw 'Two distinct active native owners required.'}
    Assert-KmcChunk7ExactIds $owners @($first,$second) 'riderId' -GuidValue;Assert-KmcChunk7ExactIds $owners @($mammoth,$horse) 'mountId' -GuidValue
    foreach($owner in $owners) {
        [Guid]$id=[Guid]::Empty
        if(-not[Guid]::TryParse([string]$owner.ownerId,[ref]$id)-or$owner.relationshipState-cne'Mounted'-or$owner.generation-lt1){throw 'Active world ownership identity invalid.'}
        $same=@($after[0].owners|Where-Object ownerId -CEQ $owner.ownerId)
        if($same.Count-ne1-or$same[0].riderId-cne$owner.riderId-or$same[0].mountId-cne$owner.mountId-or$same[0].generation-ne$owner.generation-or$same[0].state-cne'Active'){throw 'Selection/movement changed exact pair ownership.'}
    }
    # A door is not-applicable only for the owner-seed kind, by its exact observed scan shape;
    # door behaviour is then never treated as tested. Every other kind requires the door proof.
    $doorTested=$true
    if($null-ne$o.door-and$null-ne$o.door.PSObject.Properties['applicable']){
        if(-not$seed){throw 'A not-applicable door is accepted only for owner-seed exploration.'}
        Assert-KmcChunk7DoorNotApplicable $o.door $owners @($o.movements);$doorTested=$false
        if(@($o.snapshots|Where-Object label -CEQ 'door-complete').Count-ne0){throw 'A not-applicable door cannot carry a door completion window.'}
    }
    if($doorTested){
    Assert-KmcChunk7DoorEvidence $o.door $owners @($first,$second)
    $doorAfter=@($o.snapshots|Where-Object label -CEQ 'door-complete')
    if($doorAfter.Count-ne1-or@($doorAfter[0].owners).Count-ne2){throw 'Two-pair door completion window missing.'}
    }
    if($doorTested){foreach($owner in $owners){$same=@($doorAfter[0].owners|Where-Object ownerId -CEQ $owner.ownerId);if($same.Count-ne1-or$same[0].generation-ne$owner.generation-or$same[0].riderId-cne$owner.riderId-or$same[0].mountId-cne$owner.mountId-or$same[0].state-cne'Active'){throw 'Door work changed captured ownership.'}}}
    $split=@($o.snapshots|Where-Object label -CEQ 'first-dismounted-second-retained')
    if($split.Count-ne1){throw 'Independent native Dismount window missing.'}
    $retained=@($split[0].owners|Where-Object state -CEQ 'Active');$expected=@($owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $second})[0]
    if($retained.Count-ne1-or$retained[0].ownerId-cne$expected.ownerId-or$retained[0].generation-ne$expected.generation-or
        $retained[0].riderId-cne$expected.riderId-or$retained[0].mountId-cne$expected.mountId){throw 'Dismount changed the other pair owner.'}
    $moveSpecs=@(@('mammoth',$first,$mammoth),@('horse',$second,$horse),@('unmounted-control',$first,$first))
    if(@($o.movements).Count-ne3){throw 'Three exact native movement windows required.'}
    for($i=0;$i-lt3;$i++) {
        $v=$o.movements[$i];$s=$moveSpecs[$i]
        $quietOwners=if($i-lt2){$owners}else{@($owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $second})}
        Assert-KmcChunk7QuietWindow $v.optionalCaptureWindow $v @($quietOwners) $actors
        Assert-KmcChunk7Boolean $v.commandTerminal $true
        Assert-KmcChunk7WorldInt32 $v.commandObject
        foreach($command in @($v.commandsBefore)+@($v.commandsAfter)){Assert-KmcChunk7WorldInt32 $command.moveObject}
        foreach($point in @($v.start,$v.end,$v.requestedGoal)){foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $point.$axis)){throw 'Movement requires finite observed coordinates.'}}}
        if($v.label-cne$s[0]-or$v.selectedId-cne$s[1]-or$v.executorId-cne$s[2]-or$v.commandObject-eq0-or$v.commandTerminal-ne$true-or
            $v.selectionAfterInput-ceq$v.selectedId-or$v.selectionAfterInput-cnotin$actors){throw 'Native movement route identity differs.'}
        Assert-KmcChunk7ExactIds @($v.commandsBefore) $actors;Assert-KmcChunk7ExactIds @($v.commandsAfter) $actors
        foreach($before in $v.commandsBefore) {
            $post=@($v.commandsAfter|Where-Object id -CEQ $before.id)[0]
            if($before.id-ceq$v.executorId){if($post.moveObject-ne$v.commandObject-or$post.moveObject-eq$before.moveObject){throw 'Movement executor did not receive the exact new command.'}}
            elseif($post.moveObject-ne$before.moveObject){throw 'Native routing changed another actor command.'}
        }
        $distance=0.0;$residual=0.0
        foreach($axis in @('x','y','z')){$distance+=[Math]::Pow(([double]$v.end.$axis-[double]$v.start.$axis),2);$residual+=[Math]::Pow(([double]$v.end.$axis-[double]$v.requestedGoal.$axis),2)}
        if([double]::IsNaN($distance)-or[double]::IsInfinity($distance)-or[Math]::Sqrt($distance)-lt0.75-or
            [double]::IsNaN($residual)-or[double]::IsInfinity($residual)-or[Math]::Sqrt($residual)-gt1.0){throw 'Native movement did not meet bounded endpoint.'}
    }
    Assert-KmcChunk7Measurements $o.measurements
    $cycleSpecs=@();for($n=1;$n-le3;$n++){foreach($spec in $inputSpecs){$cycleSpecs+=,@(('cycle-'+$n+'-'+$spec[0]),$spec[1],$spec[2],$spec[3])}}
    Assert-KmcChunk7InputSequence $o.cycleInputs $cycleSpecs $actors $commands $shells
    if(@($o.cycles).Count-ne6){throw 'Three complete native mounted/settled lifecycle cycles required.'}
    $previous=@{};foreach($owner in $owners){$previous[$owner.riderId]=[long]$owner.generation}
    $allOwners=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach($owner in $owners){[void]$allOwners.Add([string]$owner.ownerId)}
    $baselineSubscriptions=Get-KmcChunk7Subscriptions $o.measurements[0].subscriptionsBefore
    for($n=1;$n-le3;$n++) {
        $mounted=$o.cycles[($n-1)*2];$settled=$o.cycles[($n-1)*2+1]
        if($mounted.label-cne('cycle-'+$n+'-mounted')-or$settled.label-cne('cycle-'+$n+'-settled')-or
            @($mounted.owners).Count-ne2-or@($settled.owners).Count-ne0){throw 'Cycle ownership windows missing or unsettled.'}
        Assert-KmcChunk7ExactIds $mounted.owners @($first,$second) 'riderId' -GuidValue
        foreach($owner in $mounted.owners) {
            [Guid]$id=[Guid]::Empty
            if(-not[Guid]::TryParse([string]$owner.ownerId,[ref]$id)-or-not$allOwners.Add([string]$owner.ownerId)-or
                $owner.generation-le$previous[$owner.riderId]-or$owner.state-cne'Active'-or$owner.relationshipState-cne'Mounted'-or
                -not(Test-KmcChunk7OwnerActorMatch $owner.mountId $(if(Test-KmcChunk7OwnerActorMatch $owner.riderId $first){$mammoth}else{$horse}))){throw 'Remount reused stale ownership/generation or wrong actor.'}
            $previous[$owner.riderId]=[long]$owner.generation
        }
        foreach($window in @($mounted,$settled)) {
            Assert-KmcChunk7WorldCounters $window @('boundaryCount','participantCount')
            if($window.boundaryCount-ne2-or$window.participantCount-ne2-or(Get-KmcChunk7Subscriptions $window.subscriptions)-cne$baselineSubscriptions){throw 'Lifecycle accumulated service boundaries or subscriptions.'}
        }
    }
    $admission=$o.factRemovalAdmission
    foreach($field in @('owners','activePairs','frame','gameTicks')){if(-not(Test-KmcExactJsonInteger $admission.$field)-or$admission.$field-lt0){throw 'Fact-removal admission counter is missing or mistyped.'}}
    if($admission.owners-ne0-or$admission.activePairs-ne0-or$admission.frame-lt$o.measurements[3].endFrame-or$admission.commands-isnot[Array]){throw 'Native progression removal ran before final sampling or settled ownership.'}
    foreach($field in @('preparationPending','serializationBusy','pendingNativeWrites')){Assert-KmcChunk7Boolean $admission.$field $false}
    $issued=@($o.inputs)+@($o.cycleInputs)+@($o.movements)
    $doorCommands=if($doorTested){@($o.door.interactions|ForEach-Object{[string]$_.commandObject})+@([string]$o.door.firstPairReturn.command)}else{@()}
    Assert-KmcChunk7ExactIds $admission.commands @(@($issued|ForEach-Object{[string]$_.commandObject})+$doorCommands) 'commandObject'
    $settledCommandIds=[Collections.Generic.HashSet[long]]::new()
    foreach($command in $admission.commands){
        Assert-KmcChunk7WorldInt32 $command.commandObject
        if(-not(Test-KmcExactJsonInteger $command.commandObject)-or$command.commandObject-eq0-or-not$settledCommandIds.Add([long]$command.commandObject)-or$command.hasExecutionProcess-isnot[bool]){throw 'Fact-removal command identity or native process observation differs.'}
        Assert-KmcChunk7Boolean $command.finished $true;Assert-KmcChunk7Boolean $command.processEnded $true
    }
    $c=$o.cleanup
    foreach($field in @('nativeSaveWritten','ordinaryRespecProven')){Assert-KmcChunk7Boolean $c.$field $false}
    foreach($field in @('combatOrPersistenceProof','thirdUnrelatedActorProven')){Assert-KmcChunk7Boolean $o.$field $false}
    foreach($field in @('pauseBefore','pauseAfter','movementBefore','movementAfter','pairedBefore','pairedAfter')){if($c.$field-isnot[bool]){throw 'Restored native settings require actual booleans.'}}
    foreach($pair in $c.pairBoundaries){Assert-KmcChunk7Boolean $pair.residueAbsent $true;Assert-KmcChunk7Boolean $pair.ownerPresent $false}
    if($seed){
        Assert-KmcChunk7OwnerSeedCleanup $c
        Assert-KmcChunk7Boolean $o.combatOrPersistenceProof $false;Assert-KmcChunk7Boolean $o.thirdUnrelatedActorProven $false
    }elseif($owned){
        foreach($field in @('pairResidueAbsent','controlsSettled','settingsAndSelectionRestored','nativeProcessesSettled','cleanupCustodySettled','inheritedPetsPreserved')){Assert-KmcChunk7Boolean $c.$field $true}
        foreach($field in @('horseRemoved','nativeSaveWritten','ordinaryRespecProven','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $c.$field $false}
        Assert-KmcChunk7Boolean $o.combatOrPersistenceProof $false;Assert-KmcChunk7Boolean $o.thirdUnrelatedActorProven $false
    }else{
    foreach($field in @('pairResidueAbsent','horseRemoved','controlsSettled','settingsAndSelectionRestored','nativeProcessesSettled','cleanupCustodySettled','nativeProgressionRequiresExternalWorkingRestoration')){Assert-KmcChunk7Boolean $c.$field $true}
    if($c.pairResidueAbsent-ne$true-or$c.horseRemoved-ne$true-or$c.controlsSettled-ne$true-or$c.settingsAndSelectionRestored-ne$true-or
        $c.nativeSaveWritten-ne$false-or$c.ordinaryRespecProven-ne$false-or$c.nativeProgressionRequiresExternalWorkingRestoration-ne$true-or
        $o.combatOrPersistenceProof-ne$false-or$o.thirdUnrelatedActorProven-ne$false){throw 'Exploration cleanup or evidence scope differs.'}
    }
    Assert-KmcChunk7ExactIds @($c.partyAfter) $original
    if(($c.selectionBefore-join'|')-cne($c.selectionAfter-join'|')-or$c.pauseBefore-ne$c.pauseAfter-or$c.movementBefore-ne$c.movementAfter-or$c.pairedBefore-ne$c.pairedAfter){throw 'Settings/selection restoration differs.'}
    if(@($c.world.owners|Where-Object state -CNE 'Retired').Count-ne0-or@($c.pairBoundaries).Count-lt2){throw 'World cleanup left retained ownership.'}
    foreach($pair in $c.pairBoundaries){if($pair.state-cne'Unmounted'-or$pair.residueAbsent-ne$true-or$pair.ownerPresent-ne$false){throw 'Pair cleanup incomplete.'}}
}
function Assert-KmcChunk7WorldArtifact($Request,$Manifest,[string]$Status) {
    $present=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq'chunk7-world-evidence.json'-or$_.kind-ceq'chunk7-world-evidence'})
    if((Get-KmcChunk7SelectedScenario $Request)-cne'chunk7-two-pair-exploration'){if($present.Count-ne0){throw 'Another scenario supplied Chunk 7 world evidence.'};return}
    if($present.Count-eq0-and$Status-cne'PASS'){return}
    $artifact=Read-KmcChunk7ManifestArtifact $Request $Manifest 'chunk7-world-evidence.json' 'chunk7-world-evidence'
    Assert-KmcChunk7WorldEvidence $Request $artifact $Status
    if($artifact.status-ceq'PASS') {
        $shots=@($Manifest.artifacts|Where-Object{$_.relativePath-ceq'movement-visuals/chunk7-two-pair-mounted.png'-and$_.kind-ceq'screenshot'})
        if((Test-KmcChunk7SixPairStress $Request)-or(Test-KmcChunk7OwnedPairsSmallThirdHorse $Request)-or(Test-KmcChunk7OwnedPairsSmallThirdHorseSave $Request)-or(Test-KmcChunk7OwnedPairsFourthHorse $Request)){if($shots.Count-ne0){throw 'The selected fixture case cannot claim a two-pair mounted screenshot.'}}
        elseif($shots.Count-ne1){throw 'Two-pair screenshot is not immutably manifested.'}
    }
}
function Assert-KmcExactObservedLoaderDependency($Assemblies,$Dependencies,[string]$RelativePath,[string]$Name,[string]$FullName,[string]$Mvid,[string]$Sha256) {
    $expected=[IO.Path]::GetFullPath((Join-Path $Dependencies.gameRoot $RelativePath))
    $receipt=@($Dependencies.files|Where-Object relativePath -CEQ $RelativePath)
    $loaded=@($Assemblies|Where-Object name -CEQ $Name)
    if($receipt.Count-ne1-or$receipt[0].relativePath-isnot[string]-or$receipt[0].sha256-isnot[string]-or$receipt[0].sha256-cne$Sha256-or
        $loaded.Count-ne1-or$loaded[0].name-isnot[string]-or$loaded[0].fullName-isnot[string]-or$loaded[0].mvid-isnot[string]-or
        $loaded[0].sha256-isnot[string]-or$loaded[0].dynamic-isnot[bool]-or$loaded[0].dynamic-or
        $loaded[0].fullName-cne$FullName-or$loaded[0].mvid-cne$Mvid-or$loaded[0].sha256-cne$Sha256-or
        $loaded[0].location-isnot[string]-or-not[IO.Path]::IsPathRooted($loaded[0].location)-or
        -not[IO.Path]::GetFullPath($loaded[0].location).Equals($expected,[StringComparison]::OrdinalIgnoreCase)) {
        throw ('The exact installed loader contract was not pinned in both dependency and native observations: '+$Name)
    }
}
function Assert-KmcBaseGameNativeInventory($Request,$Artifact,$Dependencies,[switch]$PassThru) {
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'runtime-loader-inventory'
    if($Artifact.status-cne'PASS'-or$Artifact.observations.capturedInNativeProcess-ne$true-or
        -not(Test-KmcExactJsonInteger $Artifact.observations.processId)-or$Artifact.observations.processId-le0){throw 'Live loader inventory is incomplete.'}
    Assert-KmcChunk7Boolean $Artifact.observations.capturedInNativeProcess $true
    $mods=@($Artifact.observations.modEntries)
    foreach($entry in $mods){Assert-KmcChunk7Boolean $entry.active $true;Assert-KmcChunk7Boolean $entry.enabled $true}
    if($mods.Count-ne1-or$mods[0].id-cne'KingmakerMountedCombat'-or$mods[0].active-ne$true-or$mods[0].enabled-ne$true){throw 'Base-only run loaded another UMM entry or KMC was inactive/disabled.'}
    $assemblies=@($Artifact.observations.loadedAssemblies)
    if($assemblies.Count-lt4-or$assemblies.Count-gt2048){throw 'Loaded assembly inventory is incomplete or outside the bounded contract.'}
    $managedRoot=[IO.Path]::GetFullPath((Join-Path $Dependencies.gameRoot 'Kingmaker_Data/Managed')).TrimEnd('\')
    $loaderRoot=Join-Path $managedRoot 'UnityModManager'
    $kmcPath=[IO.Path]::GetFullPath((Join-Path $Dependencies.modsRoot 'KingmakerMountedCombat/KingmakerMountedCombat.dll'))
    $dynamicAssemblies=New-Object 'Collections.Generic.List[object]'
    $inMemoryAssemblies=New-Object 'Collections.Generic.List[object]'
    $kmcLoadedFromNativeCache=$false
    foreach($assembly in $assemblies) {
        Assert-KmcExactProperties $assembly @('name','fullName','dynamic','location','mvid','sha256') 'Loaded native assembly'
        if($assembly.dynamic-isnot[bool]-or$assembly.name-isnot[string]-or[string]::IsNullOrWhiteSpace($assembly.name)-or
            $assembly.fullName-isnot[string]-or[string]::IsNullOrWhiteSpace($assembly.fullName)){throw 'Loaded assembly identity or dynamic flag is missing.'}
        if($assembly.name-ceq'HarmonySharedState') {
            # Exact installed Harmony 2.3.6.0 (owner UMM reinstall 2026-10-16; 2.3.1.1
            # before) creates this module with Cecil (GetOrCreateSharedStateType
            # 06000095, CreateModule 06000E95 assigns Guid.NewGuid) and loads bytes
            # through ReflectionHelper.Load (06002DA1/06002DA2). Mono reports IsDynamic=false,
            # an empty Location and a generated MVID. This is an identity-shape
            # exception under a pinned creator, not observed creation or content proof.
            $memoryMvid=[Guid]::Empty
            if($assembly.dynamic-or$assembly.fullName-cne'HarmonySharedState, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'-or
                $assembly.location-isnot[string]-or$assembly.location-cne''-or$null-ne$assembly.sha256-or$assembly.mvid-isnot[string]-or
                -not[Guid]::TryParseExact($assembly.mvid,'D',[ref]$memoryMvid)-or$memoryMvid-eq[Guid]::Empty-or
                $memoryMvid.ToString('D')-cne$assembly.mvid-or@($assemblies|Where-Object name -CEQ 'HarmonySharedState').Count-ne1-or
                @($assemblies|Where-Object mvid -CEQ $assembly.mvid).Count-ne1){throw 'In-memory Harmony shared state differs from its exact installed identity shape.'}
            Assert-KmcExactObservedLoaderDependency $assemblies $Dependencies 'Kingmaker_Data/Managed/UnityModManager/0Harmony.dll' '0Harmony'                 '0Harmony, Version=2.3.6.0, Culture=neutral, PublicKeyToken=null' '4dbb7cd6-acb5-43ef-8d7e-6fc104af5bdc'                 'df043f05c6e43602fa8e8f38f52a3a2cf962aab7a65a9d85573ea25f01a63e80'
            $inMemoryAssemblies.Add([pscustomobject]@{name=[string]$assembly.name;fullName=[string]$assembly.fullName;mvid=[string]$assembly.mvid;
                creatorName='0Harmony';creatorMvid='4dbb7cd6-acb5-43ef-8d7e-6fc104af5bdc';creatorSha256='df043f05c6e43602fa8e8f38f52a3a2cf962aab7a65a9d85573ea25f01a63e80';
                identityShapeVerified=$true;creationObserved=$false;contentCertified=$false})
            continue
        }
        if($assembly.dynamic) {
            # Runtime-generated assemblies have no file-backed byte identity.
            # Preserve every observed identity without inventing a stock/dynamic allowlist.
            if($null-ne$assembly.location-or$null-ne$assembly.mvid-or$null-ne$assembly.sha256){throw 'Dynamic assembly claims an unavailable file-backed identity.'}
            $dynamicAssemblies.Add([pscustomobject]@{name=[string]$assembly.name;fullName=[string]$assembly.fullName})
            continue
        }
        $parsedMvid=[Guid]::Empty
        if($assembly.location-isnot[string]-or[string]::IsNullOrWhiteSpace($assembly.location)-or
            -not[IO.Path]::IsPathRooted($assembly.location)-or$assembly.sha256-isnot[string]-or
            $assembly.sha256-cnotmatch'^[0-9a-f]{64}$'-or$assembly.mvid-isnot[string]-or
            -not[Guid]::TryParseExact($assembly.mvid,'D',[ref]$parsedMvid)-or$parsedMvid-eq[Guid]::Empty-or
            $parsedMvid.ToString('D')-cne$assembly.mvid){throw 'File-backed assembly requires an absolute location and exact observed SHA256/MVID.'}
        $location=[IO.Path]::GetFullPath([string]$assembly.location)
        $directory=[IO.Path]::GetDirectoryName($location).TrimEnd('\')
        if($assembly.name-ceq'KingmakerMountedCombat') {
            if(-not$location.Equals($kmcPath,[StringComparison]::OrdinalIgnoreCase)) {
                # Installed UMM 0.33.2.0 ModEntry.Load 06000111 (0600009A in 0.28.2.0) uses a canonical UInt16
                # cache suffix in this same mod directory. Candidate bytes/MVID
                # are still checked below; another path or cache shape is refused.
                $leaf=[IO.Path]::GetFileName($location);$cacheMatch=[regex]::Match($leaf,'^KingmakerMountedCombat\.dll\.(0|[1-9][0-9]{0,4})\.cache$')
                if(-not$directory.Equals([IO.Path]::GetDirectoryName($kmcPath),[StringComparison]::OrdinalIgnoreCase)-or
                    -not$cacheMatch.Success-or[int]$cacheMatch.Groups[1].Value-gt65535){throw 'Loaded KMC assembly is outside its exact staged candidate or native UMM cache path.'}
                Assert-KmcExactObservedLoaderDependency $assemblies $Dependencies 'Kingmaker_Data/Managed/UnityModManager/UnityModManager.dll' 'UnityModManager'                     'UnityModManager, Version=0.33.2.0, Culture=neutral, PublicKeyToken=null' '914c93ba-b0bd-409f-9d38-84e575289dc2'                     'f6f7e18aff88910b01d23e3ce0829f3b4188c6784b0fcea56f9b3af8aa14d0a9'
                $kmcLoadedFromNativeCache=$true
            }
        }
        elseif(-not$directory.Equals($managedRoot,[StringComparison]::OrdinalIgnoreCase)-and
            -not$directory.Equals($loaderRoot,[StringComparison]::OrdinalIgnoreCase)) {
            throw 'File-backed assembly is outside the exact installed Managed/UMM directories and staged KMC path.'
        }
    }
    $kmc=@($assemblies|Where-Object name -CEQ 'KingmakerMountedCombat')
    if($kmc.Count-ne1-or$kmc[0].dynamic-or$kmc[0].sha256-cne$Request.dllSha256-or$kmc[0].mvid-cne$Request.dllMvid){throw 'Loaded KMC assembly differs from candidate.'}
    foreach($relative in @('Kingmaker_Data/Managed/Assembly-CSharp.dll','Kingmaker_Data/Managed/UnityModManager/UnityModManager.dll','Kingmaker_Data/Managed/UnityModManager/0Harmony12.dll')) {
        $dependency=@($Dependencies.files|Where-Object relativePath -CEQ $relative)
        if($dependency.Count-ne1){throw 'Dependency receipt role missing.'}
        $expectedPath=[IO.Path]::GetFullPath((Join-Path $Dependencies.gameRoot $relative))
        $loaded=@($assemblies|Where-Object{-not$_.dynamic-and$_.location-and[IO.Path]::GetFullPath([string]$_.location)-ieq$expectedPath})
        if($loaded.Count-ne1-or$loaded[0].sha256-cne$dependency[0].sha256){throw 'Required pinned native loader assembly was not observed loaded.'}
    }
    if($PassThru) {
        return [pscustomobject][ordered]@{schemaVersion=1;fileBackedAssemblyCount=$assemblies.Count-$dynamicAssemblies.Count-$inMemoryAssemblies.Count;
            dynamicAssemblyCount=$dynamicAssemblies.Count;dynamicAssemblies=@($dynamicAssemblies.ToArray());dynamicAssembliesQualified=$false;
            inMemoryAssemblyCount=$inMemoryAssemblies.Count;inMemoryAssemblies=@($inMemoryAssemblies.ToArray());
            inMemoryAssemblyCreationObserved=$false;inMemoryAssemblyContentCertified=$false;kmcLoadedFromNativeCache=$kmcLoadedFromNativeCache;
            completeStockAssemblyAllowlistProven=$false;
            limitation='File-backed assemblies retain observed hashes/MVIDs and exact allowed directories; staged KMC or its pinned UMM cache retains exact candidate bytes. HarmonySharedState is accepted only with its exact in-memory identity shape and pinned installed creator; its creation and content were not observed. Other Managed/UMM binaries and runtime-generated assemblies remain inventoried, not certified by a stock assembly allowlist.'}
    }
}
