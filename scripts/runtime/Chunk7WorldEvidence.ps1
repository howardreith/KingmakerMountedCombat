# Original strict reader for the bounded native world fixture; no staging or native writes.
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Chunk7OwnedPairsEvidence.ps1')
function Get-KmcChunk7WorldRows {
    @('chunk7-two-pair-exploration','C7-world-fixture','C7-world-native-acquisition','C7-world-owner-seed-setup','C7-world-two-native-mounts','C7-world-selection-routing',
      'C7-world-independent-dismount','C7-world-cycles','C7-world-sampling','C7-world-cleanup','C7-world-runtime','C7-world-visual','C7-world-interrupted','C7-world-owner-seed-manual','C7-small-third-horse-native-setup','C7-small-third-horse-native-manual','C7-f09-fourth-horse-native-setup','C7-f09-fourth-horse-native-manual',
      'C7-world-owner-surface-mount','C7-world-owner-surface-legs','C7-world-turn-based-door',
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
# Sprint 2 (DOOR-01/SPACE-01) v3 door blocks. A bounded goal search records every candidate with the native values
# that decided it; the producer's first admitted candidate is the goal.
function Assert-KmcChunk7GoalSearchShape($Search,[string]$Label){
    if($null-eq$Search){throw ($Label+' goal search is missing.')}
    Assert-KmcExactProperties $Search @('start','direction','distance','candidates','selectedIndex') ($Label+' goal search')
    Assert-KmcChunk7DoorPoint $Search.start ($Label+' search start');Assert-KmcChunk7DoorPoint $Search.direction ($Label+' search direction')
    if(-not(Test-KmcChunk7FiniteNumber $Search.distance)-or$Search.distance-le0){throw ($Label+' goal search distance is invalid.')}
    if($Search.candidates-isnot[Array]-or$Search.candidates.Count-lt1-or$Search.candidates.Count-gt12){throw ($Label+' goal search candidates are missing or unbounded.')}
    Assert-KmcOrdinaryInteger $Search.selectedIndex -1 11
    for($i=0;$i-lt$Search.candidates.Count;$i++){$c=$Search.candidates[$i]
        if($c.index-ne$i-or$c.walkable-isnot[bool]-or$c.reason-isnot[string]){throw ($Label+' goal search candidate is malformed.')}
        if($i-lt$Search.candidates.Count-1-and$c.reason-ceq'admitted'){throw ($Label+' goal search continued past an admitted candidate.')}}
    $last=$Search.candidates[$Search.candidates.Count-1]
    if(($Search.selectedIndex-ge0)-ne($last.reason-ceq'admitted')-or($Search.selectedIndex-ge0-and$Search.selectedIndex-ne$Search.candidates.Count-1)){throw ($Label+' goal search selection disagrees with its candidates.')}
}
# v4 (Sprint 2 completion): the owned settlement fields on the terminal outcome. The settled body footprint is clear
# either as found (no recovery) or after exactly one completed owned recovery that left it clear; refusal, an
# unrecoverable footprint, an interrupted/replaced/stalled recovery and a prevention refusal all fail the row.
function Get-KmcChunk7DoorSettlementKeys {@('PreventionRefused','OccupancySettled','OccupancySettleGameSeconds','MountCorpulence','FootprintClear','FootprintRingInside','FootprintRingReachable','RecoveryDisposition','RecoveryTerminal','RecoveryCandidatesEvaluated','RecoveryCandidateReasons','RecoveryDestinationDistance','FootprintClearAfterRecovery','RecoveryCancelReason')}
function Assert-KmcChunk7DoorSettlement($Outcome){
    Assert-KmcChunk7Boolean $Outcome.PreventionRefused $false;Assert-KmcChunk7Boolean $Outcome.OccupancySettled $true
    # v5 outcomes bound the settlement wait on its operational seconds (campaign GameTime is frozen inside a turn-based turn and is
    # recorded as observed); earlier outcomes carry only the campaign figure, which was the wait's basis then.
    $operational=$null-ne$Outcome.PSObject.Properties['OccupancySettleOperationalSeconds']
    if(-not(Test-KmcChunk7FiniteNumber $Outcome.OccupancySettleGameSeconds)-or$Outcome.OccupancySettleGameSeconds-lt0-or(-not$operational-and$Outcome.OccupancySettleGameSeconds-gt3.5)){throw 'Door settlement time is invalid or exceeded its bound.'}
    if($operational-and(-not(Test-KmcChunk7FiniteNumber $Outcome.OccupancySettleOperationalSeconds)-or$Outcome.OccupancySettleOperationalSeconds-lt0-or$Outcome.OccupancySettleOperationalSeconds-gt3.5)){throw 'Door settlement operational time is invalid or exceeded its bound.'}
    if(-not(Test-KmcChunk7FiniteNumber $Outcome.MountCorpulence)-or$Outcome.MountCorpulence-le0-or$Outcome.MountCorpulence-gt3){throw 'Door settlement mount corpulence is invalid.'}
    Assert-KmcOrdinaryInteger $Outcome.FootprintRingInside 0 8;Assert-KmcOrdinaryInteger $Outcome.FootprintRingReachable 0 8;Assert-KmcOrdinaryInteger $Outcome.RecoveryCandidatesEvaluated 0 80
    if($Outcome.FootprintClear-isnot[bool]-or$Outcome.RecoveryDisposition-isnot[string]-or$Outcome.RecoveryTerminal-isnot[string]-or$Outcome.RecoveryCandidateReasons-isnot[string]){throw 'Door settlement fields are mistyped.'}
    if($Outcome.RecoveryDisposition-ceq'NotNeeded'){
        Assert-KmcChunk7Boolean $Outcome.FootprintClear $true
        if($Outcome.FootprintRingInside-ne8-or$Outcome.FootprintRingReachable-ne8-or$Outcome.OccupancyRecovery-cne'none'-or$Outcome.RecoveryTerminal-cne'None'-or$null-ne$Outcome.FootprintClearAfterRecovery-or$Outcome.RecoveryCandidatesEvaluated-ne0){throw 'A clear door footprint reports recovery facts.'}
    } elseif($Outcome.RecoveryDisposition-ceq'Admitted'){
        Assert-KmcChunk7Boolean $Outcome.FootprintClear $false;Assert-KmcChunk7Boolean $Outcome.FootprintClearAfterRecovery $true
        if($Outcome.OccupancyRecovery-cne'repath'-or$Outcome.RecoveryTerminal-cne'Completed'-or$Outcome.RecoveryCandidatesEvaluated-lt1){throw 'Door occupancy recovery did not complete through one owned native move.'}
        if(-not(Test-KmcChunk7FiniteNumber $Outcome.RecoveryDestinationDistance)-or$Outcome.RecoveryDestinationDistance-le0-or$Outcome.RecoveryDestinationDistance-gt2.5){throw 'Door occupancy recovery destination left the recovery radius.'}
        if($Outcome.RecoveryCandidateReasons-cnotmatch'(^|,)admitted=1(,|$)'){throw 'Door occupancy recovery did not record exactly one admitted candidate.'}
    } else { throw ('Door interaction left the pair without a clear settled footprint: '+$Outcome.RecoveryDisposition+'/'+$Outcome.RecoveryTerminal+'.') }
    if($null-ne$Outcome.RecoveryCancelReason){throw 'A completed door settlement reports a cancellation.'}
}
function Assert-KmcChunk7DoorV4Blocks($Door,$Fixture,[string[]]$Riders){
    $u=$Door.unmountedControl
    if($null-eq$u-or$u.applicable-isnot[bool]){throw 'Door unmounted-control record is missing or malformed.'}
    if($u.applicable){
        Assert-KmcExactProperties $u @('applicable','actorId','accepted','commandType','commandObject','distanceBefore','routedByKmc','result','doorOpenAfter','distanceAfter','actorInsideNavmesh') 'door unmounted control'
        Assert-KmcOrdinaryGuid $u.actorId;if($u.actorId-cin$Riders){throw 'The unmounted door control actor is a declared rider.'}
        Assert-KmcChunk7Boolean $u.accepted $true;Assert-KmcChunk7Boolean $u.routedByKmc $false;Assert-KmcChunk7Boolean $u.doorOpenAfter $true;Assert-KmcChunk7Boolean $u.actorInsideNavmesh $true
        Assert-KmcChunk7WorldInt32 $u.commandObject
        if($u.commandObject-eq0-or$u.commandType-cne'Kingmaker.UnitLogic.Commands.UnitInteractWithObject'-or$u.result-cne'Success'){throw 'The unmounted control did not open the door through the native interaction command.'}
        foreach($field in @('distanceBefore','distanceAfter')){if(-not(Test-KmcChunk7FiniteNumber $u.$field)-or$u.$field-lt0){throw 'Unmounted control distance is invalid.'}}
        if($u.distanceBefore-le($Fixture.radius+0.5)){throw 'The unmounted control began inside the door''s immediate range.'}
    } else { Assert-KmcExactProperties $u @('applicable','reason') 'door unmounted control (not applicable)';if($u.reason-isnot[string]){throw 'Unmounted control reason is missing.'} }
    $n=$Door.endpointNeighbor
    if($null-eq$n-or$n.applicable-isnot[bool]){throw 'Door endpoint-neighbor record is missing or malformed.'}
    if($n.applicable){
        Assert-KmcExactProperties $n @('applicable','goalSearch','goal','moveCommand','moveResult','positionAtFirstClick','distanceToApproachPointAtFirstClick','occupancyAfterFirstInteraction') 'door endpoint neighbor'
        Assert-KmcChunk7GoalSearchShape $n.goalSearch 'endpoint neighbor';Assert-KmcChunk7DoorPoint $n.goal 'endpoint neighbor goal';Assert-KmcChunk7DoorPoint $n.positionAtFirstClick 'endpoint neighbor position'
        Assert-KmcChunk7WorldInt32 $n.moveCommand
        if($n.moveCommand-eq0-or$n.moveResult-cne'Success'){throw 'The endpoint neighbor did not reach its goal through its own native move.'}
        if(-not(Test-KmcChunk7FiniteNumber $n.distanceToApproachPointAtFirstClick)-or$n.distanceToApproachPointAtFirstClick-le($Fixture.radius+0.5)-or$n.distanceToApproachPointAtFirstClick-gt($Fixture.radius+3.5)){throw 'The endpoint neighbor did not stand just outside the door approach while the first pair clicked.'}
        $oc=$n.occupancyAfterFirstInteraction
        Assert-KmcExactProperties $oc @('mountPosition','riderPosition','mountInsideNavmesh','riderInsideNavmesh','mountDistanceToApproachPoint','mountDistanceToDoor','doorOpen','cutEnabled','mountCanMove','mountCommandsEmpty') 'endpoint neighbor occupancy'
        foreach($flag in @('mountInsideNavmesh','doorOpen','mountCanMove','mountCommandsEmpty')){Assert-KmcChunk7Boolean $oc.$flag $true}
    } else {
        foreach($k in @('applicable','reason')){if($null-eq$n.PSObject.Properties[$k]){throw ('Door endpoint neighbor (not applicable) is missing '+$k+'.')}}
        if($n.reason-isnot[string]){throw 'Endpoint neighbor reason is missing.'}
        if($null-ne$n.PSObject.Properties['goalSearch']-and$null-eq$n.goalSearch.PSObject.Properties['skipped']){Assert-KmcChunk7GoalSearchShape $n.goalSearch 'endpoint neighbor'}
    }
    $s=$Door.stopControl
    if($null-eq$s-or$s.applicable-isnot[bool]){throw 'Door Stop-control record is missing or malformed.'}
    if($s.applicable){
        Assert-KmcExactProperties $s @('applicable','commandObject','distanceBefore','mountPositionAtStop','commandStartedAtStop','distanceMovedAtStop','stopFrame','commandResult','doorOpenAfter','interactionCount','outcomeResult','mountMoveSlotRestored','mountCommandsEmpty','riderCommandsEmpty','pendingSettlement','framesToSettle','mountPositionAfter') 'door Stop control'
        Assert-KmcChunk7WorldInt32 $s.commandObject;Assert-KmcChunk7DoorPoint $s.mountPositionAtStop 'Stop position';Assert-KmcChunk7DoorPoint $s.mountPositionAfter 'Stop settled position'
        Assert-KmcChunk7Boolean $s.commandStartedAtStop $false;Assert-KmcChunk7Boolean $s.doorOpenAfter $false;Assert-KmcChunk7Boolean $s.mountMoveSlotRestored $true
        Assert-KmcChunk7Boolean $s.mountCommandsEmpty $true;Assert-KmcChunk7Boolean $s.riderCommandsEmpty $true;Assert-KmcChunk7Boolean $s.pendingSettlement $false
        Assert-KmcOrdinaryInteger $s.interactionCount 0 0;Assert-KmcOrdinaryInteger $s.framesToSettle 0 600
        if($s.commandObject-eq0-or$s.commandResult-cne'Interrupt'-or$s.outcomeResult-cne'Interrupt'){throw 'The native Stop did not end the mounted door approach as an interrupt.'}
        foreach($field in @('distanceBefore','distanceMovedAtStop')){if(-not(Test-KmcChunk7FiniteNumber $s.$field)-or$s.$field-lt0){throw 'Stop control distance is invalid.'}}
    } else { if($null-eq$s.PSObject.Properties['reason']-or$s.reason-isnot[string]){throw 'Door Stop control (not applicable) is missing its reason.'} }
    # v4 (r08 correction): after the probe's inputs each pair returned toward its pre-setup start through an ordinary
    # move, so the engine's later steps see the captured layout. Optional for v4 artifacts written before the correction.
    if($null-ne$Door.PSObject.Properties['layoutRestore']){
        $l=$Door.layoutRestore
        if($null-eq$l-or$l.applicable-isnot[bool]-or$l.moves-isnot[Array]-or$l.moves.Count-ne2){throw 'Door layout-restore record is missing or malformed.'}
        $anyMoved=$false
        for($i=0;$i-lt2;$i++){$m=$l.moves[$i]
            if($m.pair-ne$i-or$m.applicable-isnot[bool]){throw 'Door layout-restore move is malformed.'}
            Assert-KmcChunk7DoorPoint $m.preSetupStart 'layout restore start';Assert-KmcChunk7DoorPoint $m.positionBefore 'layout restore position before';Assert-KmcChunk7DoorPoint $m.positionAfter 'layout restore position after'
            foreach($field in @('distanceToStartBefore','distanceToStartAfter')){if(-not(Test-KmcChunk7FiniteNumber $m.$field)-or$m.$field-lt0){throw 'Layout restore distance is invalid.'}}
            if($m.applicable){
                Assert-KmcExactProperties $m @('pair','preSetupStart','positionBefore','distanceToStartBefore','goalSearch','goal','command','applicable','positionAfter','distanceToStartAfter','result') 'door layout restore move'
                Assert-KmcChunk7GoalSearchShape $m.goalSearch ('layout restore pair '+$i);Assert-KmcChunk7DoorPoint $m.goal 'layout restore goal';Assert-KmcChunk7WorldInt32 $m.command
                if($m.command-eq0-or$m.result-cne'Success'-or$m.distanceToStartAfter-gt$m.distanceToStartBefore){throw 'Door layout restore did not bring the pair back toward its pre-setup start through its own native move.'}
                $anyMoved=$true
            } else {
                if($null-eq$m.PSObject.Properties['reason']-or$m.reason-isnot[string]){throw 'Door layout restore (not applicable) is missing its reason.'}
                if($null-ne$m.PSObject.Properties['goalSearch']){Assert-KmcChunk7GoalSearchShape $m.goalSearch ('layout restore pair '+$i)}
            }
        }
        if($l.applicable-ne$anyMoved){throw 'Door layout-restore applicability disagrees with its moves.'}
    }
}
function Assert-KmcChunk7DoorV3Blocks($Door,$Fixture,[switch]$V4){
    $a=$Door.approach
    Assert-KmcExactProperties $a @('approachPoint','nativeTargetNode','losObjectId','mapObjectIsDoorObject') 'door approach record'
    Assert-KmcChunk7DoorPoint $a.approachPoint 'door approach point';Assert-KmcChunk7DoorPoint $a.nativeTargetNode 'door native target node'
    Assert-KmcChunk7WorldInt32 $a.losObjectId;if($a.losObjectId-eq0-or$a.mapObjectIsDoorObject-isnot[bool]){throw 'Door approach record lost the native LOS object or map-object identity.'}
    $l=$Door.longApproach
    if($null-eq$l-or$l.applicable-isnot[bool]){throw 'Door long-approach record is missing or malformed.'}
    $longKeys=@('applicable','goalSearch','goal','startDistanceToDoor','setupCommand','setupResult','setupEnd','distanceToDoorAfterSetup');$longSkippedKeys=@('applicable','goalSearch','reason')
    if($V4){$longKeys+=@('requestedDistances');$longSkippedKeys+=@('requestedDistances')
        if($l.requestedDistances-isnot[Array]-or$l.requestedDistances.Count-lt1-or$l.requestedDistances.Count-gt3){throw 'Door long approach requested distances are missing or unbounded.'}
        foreach($d in $l.requestedDistances){if(-not(Test-KmcChunk7FiniteNumber $d)-or$d-le0-or$d-gt28){throw 'Door long approach requested distance is outside the click bound.'}}}
    if($l.applicable){
        Assert-KmcExactProperties $l $longKeys 'door long approach'
        Assert-KmcChunk7GoalSearchShape $l.goalSearch 'long approach';Assert-KmcChunk7DoorPoint $l.goal 'long approach goal';Assert-KmcChunk7DoorPoint $l.setupEnd 'long approach setup end'
        Assert-KmcChunk7WorldInt32 $l.setupCommand
        if($l.setupCommand-eq0-or$l.setupResult-cne'Success'-or-not(Test-KmcChunk7FiniteNumber $l.startDistanceToDoor)-or-not(Test-KmcChunk7FiniteNumber $l.distanceToDoorAfterSetup)-or
            $l.distanceToDoorAfterSetup-le$l.startDistanceToDoor-or$l.distanceToDoorAfterSetup-le($Fixture.radius+4)){throw 'Door long approach did not move the first pair farther from the door through its own native move.'}
        # The first click then starts from (about) the setup end: a meaningful approach, not an immediate interaction.
        if([Math]::Abs([double]$Door.interactions[0].distanceBefore-[double]$l.distanceToDoorAfterSetup)-gt1.0){throw 'Door long approach setup does not match the first click distance.'}
    } else { Assert-KmcExactProperties $l $longSkippedKeys 'door long approach (not applicable)';if($l.reason-isnot[string]){throw 'Door long approach reason is missing.'} }
    $m=$Door.clickWhileMoving
    if($null-eq$m-or$m.applicable-isnot[bool]){throw 'Door click-while-moving record is missing or malformed.'}
    if($m.applicable){
        Assert-KmcExactProperties $m @('applicable','goalSearch','goal','moveCommand','mountPositionAtClick','moveRunningAtClick','moveFinishedAtAdmission','moveResult','replacedMoveCount') 'door click while moving'
        Assert-KmcChunk7GoalSearchShape $m.goalSearch 'click while moving';Assert-KmcChunk7DoorPoint $m.goal 'click while moving goal';Assert-KmcChunk7DoorPoint $m.mountPositionAtClick 'click while moving mount position'
        Assert-KmcChunk7WorldInt32 $m.moveCommand;Assert-KmcChunk7Boolean $m.moveRunningAtClick $true;Assert-KmcChunk7Boolean $m.moveFinishedAtAdmission $true
        Assert-KmcOrdinaryInteger $m.replacedMoveCount 1 64
        if(($m.moveCommand -eq 0) -or ($m.moveResult -cne 'Interrupt')){throw 'The door click did not replace the running mount move natively.'}
        Assert-KmcChunk7Boolean $Door.interactions[1].clickWhileMoving $true
    } else { if($l.applicable-and$Door.interactions[1].clickWhileMoving-ne$false){throw 'Click-while-moving flag disagrees with its record.'}
        foreach($k in @('applicable','reason')){if($null-eq$m.PSObject.Properties[$k]){throw 'Door click-while-moving (not applicable) is missing '+$k+'.'}};if($m.reason-isnot[string]){throw 'Door click-while-moving reason is missing.'} }
}
function Assert-KmcChunk7DoorOccupancy($Occupancy,$Fixture,$Outcome=$null){
    Assert-KmcExactProperties $Occupancy @('mountPosition','riderPosition','mountInsideNavmesh','riderInsideNavmesh','mountDistanceToApproachPoint','mountDistanceToDoor','doorOpen','cutEnabled','mountCanMove','mountCommandsEmpty') 'door occupancy'
    Assert-KmcChunk7DoorPoint $Occupancy.mountPosition 'occupancy mount position';Assert-KmcChunk7DoorPoint $Occupancy.riderPosition 'occupancy rider position'
    foreach($flag in @('mountInsideNavmesh','doorOpen','mountCanMove','mountCommandsEmpty')){Assert-KmcChunk7Boolean $Occupancy.$flag $true}
    foreach($flag in @('riderInsideNavmesh','cutEnabled')){if($Occupancy.$flag-isnot[bool]){throw 'Door occupancy flags must be native booleans.'}}
    foreach($field in @('mountDistanceToApproachPoint','mountDistanceToDoor')){if(-not(Test-KmcChunk7FiniteNumber $Occupancy.$field)-or$Occupancy.$field-lt0){throw 'Door occupancy distance is invalid.'}}
    # v4 (reader correction after r05, 2026-10-30): the occupancy is recorded after the owned settlement, so a completed
    # recovery legitimately moved the mount by its recorded destination distance (at most the recovery radius) from the
    # interaction point; v3 evidence has no recovery and keeps the exact interaction-radius bound.
    $allowance=0.01
    if($null-ne$Outcome-and$Outcome.RecoveryDisposition-ceq'Admitted'-and(Test-KmcChunk7FiniteNumber $Outcome.RecoveryDestinationDistance)-and$Outcome.RecoveryDestinationDistance-gt0-and$Outcome.RecoveryDestinationDistance-le2.5){$allowance+=[double]$Outcome.RecoveryDestinationDistance}
    if($Occupancy.mountDistanceToApproachPoint-gt($Fixture.radius+$allowance)){throw 'The pair did not remain within the native interaction radius (plus its recorded recovery distance) after the door changed state.'}
}
# Sprint 2 (MOVE-01) speed legs: optional like the route-goal marker; evidence without the contract keeps its verdict.
function Assert-KmcChunk7Sprint2Legs($Observations){
    $marker=$null-ne$Observations.PSObject.Properties['speedLegContract'];$legs=$null-ne$Observations.PSObject.Properties['speedLegs']
    if($marker-ne$legs){throw 'Sprint 2 speed leg contract and legs must be recorded together.'}
    if(-not$marker){return}
    # v2 (Sprint 2 completion): the motion interval is aligned to the path (SpeedLegSampler) and the v1 figure is kept beside it.
    # v3 (Sprint 2 completion, review correction A): mounted ground input is routed to the mount, so a carried rider has no
    # locomotion command of its own; each leg records its carried riders passively (owner, own Move slot, path, attachment).
    # v4: the goal search refuses routes whose sampled navmesh height leaves the start level, and the native state at the leg's end is recorded.
    # v5 (review 2026-10-31): a failed leg is recorded (accepted false, failure) and the later legs continue for diagnosis; the
    # aggregate speedLegsAccepted stays false until every applicable leg is accepted; the first native interrupt of each issued
    # speed-leg Move is observed before cleanup (speedLegInterruptions) through the declared run-scoped observer.
    if($Observations.speedLegContract-cnotin@('sprint2-speed-legs/v1','sprint2-speed-legs/v2','sprint2-speed-legs/v3','sprint2-speed-legs/v4','sprint2-speed-legs/v5','sprint2-speed-legs/v6')){throw 'Sprint 2 speed leg contract differs.'}
    $legsV2=$Observations.speedLegContract-cin@('sprint2-speed-legs/v2','sprint2-speed-legs/v3','sprint2-speed-legs/v4','sprint2-speed-legs/v5','sprint2-speed-legs/v6');$legsV3=$Observations.speedLegContract-cin@('sprint2-speed-legs/v3','sprint2-speed-legs/v4','sprint2-speed-legs/v5','sprint2-speed-legs/v6')
    $legsV4=$Observations.speedLegContract-cin@('sprint2-speed-legs/v4','sprint2-speed-legs/v5','sprint2-speed-legs/v6');$legsV5=$Observations.speedLegContract-cin@('sprint2-speed-legs/v5','sprint2-speed-legs/v6')
    foreach($key in @('speedLegsAccepted','speedLegInterruptions','speedLegInterruptObserver')){if(($null-ne$Observations.PSObject.Properties[$key])-ne$legsV5){throw ('Sprint 2 speed legs '+$key+' belongs to the v5 contract exactly.')}}
    if($legsV5){
        Assert-KmcChunk7Boolean $Observations.speedLegsAccepted $true
        $ob=$Observations.speedLegInterruptObserver;Assert-KmcExactProperties $ob @('scope','method','token','moduleMvid','callerCapture','recordLimit','counts') 'speed leg interrupt observer'
        if($ob.scope-isnot[string]-or$ob.scope-cnotlike'first native UnitCommand.Interrupt of each issued speed-leg Move command*'-or$ob.method-cne'Kingmaker.UnitLogic.Commands.Base.UnitCommand.Interrupt'-or$ob.token-cne'060027AC'){throw 'Sprint 2 speed leg interrupt observer scope or target differs.'}
        Assert-KmcOrdinaryInteger $ob.recordLimit 8 8
        if($Observations.speedLegInterruptions-isnot[Array]){throw 'Sprint 2 speed leg interruptions must be recorded as an array.'}
        foreach($ir in @($Observations.speedLegInterruptions)){
            if($null-ne$ir.PSObject.Properties['observerError']){Assert-KmcExactProperties $ir @('legLabel','moverId','commandObject','observerError') 'speed leg interrupt (observer error)';continue}
            Assert-KmcExactProperties $ir @('phase','legLabel','moverId','role','commandObject','frame','gameTicks','gameSecondsSinceClick','wallSecondsSinceClick','commandStarted','commandActed','commandFinishedBefore','resultBefore','position','target','flatDistanceToTarget','pathLengthSoFar','agent','gameMode','paused','inCombat','unitInCombat','turnBased','loading','area','unitCanMove','unitCanAct','callers','callersTruncated','nearbyMapObjects') 'speed leg interrupt'
            if($ir.callers-isnot[string]-or$ir.callersTruncated-isnot[bool]-or$ir.phase-cne'speed-leg'){throw 'Sprint 2 speed leg interrupt lacks its caller chain or phase.'}
            Assert-KmcOrdinaryGuid $ir.moverId;Assert-KmcChunk7WorldInt32 $ir.commandObject}
    }
    $rows=@($Observations.speedLegs);if($rows.Count-lt1-or$rows.Count-gt4){throw 'Sprint 2 speed legs are missing or unbounded.'}
    $expected=@('mounted-party','mounted-solo','horse-solo','rider-solo')
    for($i=0;$i-lt$rows.Count;$i++){$leg=$rows[$i]
        if($leg.label-cne$expected[$i]){throw ('Sprint 2 speed leg order differs at '+$i+'.')}
        if($leg.applicable-isnot[bool]){throw 'Sprint 2 speed leg applicability must be a native boolean.'}
        if(-not$leg.applicable){Assert-KmcExactProperties $leg @('label','selection','primaryMoverId','distance','applicable','goalSearch','reason') 'speed leg (not applicable)';continue}
        $legKeys=@('label','selection','primaryMoverId','distance','applicable','goalSearch','start','goal','environment','selectedIds','factsBeforeClick','movers','gameSeconds');if($legsV3){$legKeys+='carried'};if($legsV4){$legKeys+='endState'}
        if($legsV5){$legKeys+=@('accepted','interruptRecords');foreach($k in @('failure','continuationStopped')){if($null-ne$leg.PSObject.Properties[$k]){$legKeys+=$k}}}
        Assert-KmcExactProperties $leg $legKeys 'speed leg'
        if($legsV5){
            # A PASS verdict needs every applicable leg accepted; a leg that failed carries its retained reason and is never waived.
            Assert-KmcChunk7Boolean $leg.accepted $true
            # v6: a bearing refused at nine metres may be admitted at seven or five; the recorded distance is the admitted meaningful length.
            if(-not(Test-KmcChunk7FiniteNumber $leg.distance)-or$leg.distance-lt5-or$leg.distance-gt9){throw ('Sprint 2 speed leg '+$leg.label+' distance is outside the meaningful five-to-nine-metre range.')}
            if($null-ne$leg.PSObject.Properties['failure']){throw ('Sprint 2 speed leg '+$leg.label+' records a failure: '+$leg.failure)}
            Assert-KmcOrdinaryInteger $leg.interruptRecords 0 8
            if($leg.interruptRecords-ne@(@($Observations.speedLegInterruptions)|Where-Object{$_.legLabel-ceq$leg.label}).Count){throw ('Sprint 2 speed leg '+$leg.label+' interrupt record count differs from the observer records.')}}
        if($legsV4){$es=$leg.endState;Assert-KmcExactProperties $es @('gameMode','paused','inCombat','loading','area','results') 'speed leg end state'
            if($es.gameMode-isnot[string]-or$es.gameMode-cne'Default'){throw ('Sprint 2 speed leg '+$leg.label+' ended outside the default game mode: '+$es.gameMode)}
            foreach($key in @('paused','inCombat','loading')){Assert-KmcChunk7Boolean $es.$key $false}
            if($es.area-isnot[string]-or$es.area-cne[string]$leg.environment.area){throw ('Sprint 2 speed leg '+$leg.label+' ended in another area.')}
            foreach($mv in @($leg.movers)){if([string]$es.results.($mv.id)-cne[string]$mv.result){throw ('Sprint 2 speed leg '+$leg.label+' end-state result differs from the mover row.')}}}
        Assert-KmcChunk7GoalSearchShape $leg.goalSearch ('speed leg '+$leg.label)
        if(@($leg.selectedIds).Count-lt1-or($leg.label-ceq'mounted-party'-and@($leg.selectedIds).Count-lt2)){throw 'Sprint 2 speed leg selection is empty or not a party.'}
        if(-not(Test-KmcChunk7FiniteNumber $leg.gameSeconds)-or$leg.gameSeconds-le0){throw 'Sprint 2 speed leg game time is invalid.'}
        $movers=@($leg.movers);if($movers.Count-lt1){throw 'Sprint 2 speed leg has no movers.'}
        $primary=@($movers|Where-Object{$_.id-ceq$leg.primaryMoverId});if($primary.Count-ne1){throw 'Sprint 2 speed leg primary mover is not recorded exactly once.'}
        foreach($mv in $movers){
            $moverKeys=@('id','role','commandObject','executorId','speedLimitAtClick','movementTypeAtClick','start','end','result','pathLength','motionGameSeconds','effectiveSpeedMps','samples','movingSamples','meanAgentSpeedMps','maximumAgentSpeedMps','meanEntitySpeedMps','maximumEntitySpeedMps','gaits','accelerated','movementTypeAtEnd','goalResidual','factsAfter')
            if($legsV2){$moverKeys+=@('samplingContract','firstMotionGameSeconds','unalignedMotionGameSeconds','effectiveSpeedMpsUnaligned')}
            Assert-KmcExactProperties $mv $moverKeys 'speed leg mover'
            if($mv.role-cnotin@('mount','rider','pet','on-foot')){throw 'Sprint 2 speed leg mover role is unknown.'}
            foreach($field in @('pathLength','motionGameSeconds','effectiveSpeedMps','meanAgentSpeedMps','maximumAgentSpeedMps','meanEntitySpeedMps','maximumEntitySpeedMps','goalResidual')){if(-not(Test-KmcChunk7FiniteNumber $mv.$field)-or$mv.$field-lt0){throw ('Sprint 2 speed leg '+$field+' is invalid.')}}
            if($mv.accelerated-isnot[bool]){throw 'Sprint 2 speed leg acceleration flag must be a native boolean.'}
            if($legsV2){
                if($mv.samplingContract-cne'aligned-interval/v2'){throw 'Sprint 2 speed leg sampling contract differs.'}
                foreach($field in @('unalignedMotionGameSeconds','effectiveSpeedMpsUnaligned')){if(-not(Test-KmcChunk7FiniteNumber $mv.$field)-or$mv.$field-lt0){throw ('Sprint 2 speed leg '+$field+' is invalid.')}}
                # A mover that moved opened its interval at or after the click; the aligned interval covers the path, so it is never shorter than the v1 one.
                if($mv.pathLength-gt0){if(-not(Test-KmcChunk7FiniteNumber $mv.firstMotionGameSeconds)-or$mv.firstMotionGameSeconds-lt0-or$mv.motionGameSeconds-lt$mv.unalignedMotionGameSeconds){throw 'Sprint 2 speed leg aligned interval is inconsistent with its samples.'}}
                elseif($null-ne$mv.firstMotionGameSeconds){throw 'Sprint 2 speed leg mover that never moved reports a first motion.'}}}
        $p=$primary[0]
        if($p.result-cne'Success'-or$p.pathLength-lt($leg.distance*0.5)-or$p.effectiveSpeedMps-le0-or$p.movingSamples-lt1){throw ('Sprint 2 speed leg '+$leg.label+' did not complete a measured native route.')}
        if($legsV3){Assert-KmcChunk7SpeedLegCarried $leg $movers}
    }
}
# v3: every mover in the mount role carries exactly one recorded rider; a carried rider is proven from pair ownership and
# passive position evidence (its stock agent disabled, no new Move command of its own, attached through the leg, its own
# sampled path beside the mount's), never from a second locomotion command.
function Assert-KmcChunk7SpeedLegCarried($Leg,$Movers){
    if($Leg.carried-isnot[Array]){throw ('Sprint 2 speed leg '+$Leg.label+' carried riders must be recorded as an array.')}
    $carried=@($Leg.carried)
    foreach($c in $carried){
        Assert-KmcExactProperties $c @('riderId','mountId','ownerId','generation','relationshipState','riderMoveCommandAtClick','riderNewMoveCommand','riderAgentEnabled','start','startOffsetToMount','end','endOffsetToMount','maximumOffsetToMount','pathLength','attachedThroughout','riderMoveCommandAtEnd','riderCommandsEmptyAtEnd') ('speed leg '+$Leg.label+' carried rider')
        Assert-KmcOrdinaryGuid $c.riderId;Assert-KmcOrdinaryGuid $c.mountId;Assert-KmcOrdinaryGuid $c.ownerId;Assert-KmcOrdinaryInteger $c.generation 1 1000000000
        if($c.relationshipState-cne'Mounted'){throw ('Sprint 2 speed leg '+$Leg.label+' carried rider was not in the Mounted relationship state.')}
        foreach($field in @('startOffsetToMount','endOffsetToMount','maximumOffsetToMount','pathLength')){if(-not(Test-KmcChunk7FiniteNumber $c.$field)-or$c.$field-lt0){throw ('Sprint 2 speed leg carried rider '+$field+' is invalid.')}}
        foreach($field in @('riderMoveCommandAtClick','riderMoveCommandAtEnd')){if(-not(Test-KmcExactJsonInteger $c.$field)){throw 'Sprint 2 speed leg carried rider Move slot identity is mistyped.'}}
        foreach($point in @($c.start,$c.end)){foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $point.$axis)){throw 'Sprint 2 speed leg carried rider position is nonfinite.'}}}
        Assert-KmcChunk7Boolean $c.riderNewMoveCommand $false;Assert-KmcChunk7Boolean $c.riderAgentEnabled $false;Assert-KmcChunk7Boolean $c.attachedThroughout $true
        if($c.riderCommandsEmptyAtEnd-isnot[bool]){throw 'Sprint 2 speed leg carried rider command state must be a native boolean.'}
        $mount=@($Movers|Where-Object{$_.id-ceq$c.mountId-and$_.role-ceq'mount'});if($mount.Count-ne1){throw ('Sprint 2 speed leg '+$Leg.label+' carried rider names a mount that is not a mover in the mount role.')}
        if(@($Movers|Where-Object{$_.id-ceq$c.riderId}).Count-ne0){throw ('Sprint 2 speed leg '+$Leg.label+' carried rider also carries its own locomotion command; mounted ground input is routed to the mount.')}
        if($c.maximumOffsetToMount-gt1.0-or$c.endOffsetToMount-gt1.0){throw ('Sprint 2 speed leg '+$Leg.label+' carried rider left its mount.')}
        if($mount[0].pathLength-gt0-and$c.pathLength-lt$mount[0].pathLength*0.5){throw ('Sprint 2 speed leg '+$Leg.label+' carried rider did not travel with its mount.')}
    }
    foreach($mv in @($Movers|Where-Object role -CEQ 'mount')){if(@($carried|Where-Object mountId -CEQ $mv.id).Count-ne1){throw ('Sprint 2 speed leg '+$Leg.label+' mover in the mount role carries no recorded rider.')}}
    if(@($carried|ForEach-Object riderId|Select-Object -Unique).Count-ne$carried.Count){throw 'Sprint 2 speed leg records a carried rider twice.'}
}

function Assert-KmcChunk7DoorEvidence($Door,$Owners,[string[]]$DeclaredRiders=@('b6628a77-4962-47a4-a17c-88d9836fc9d5','d17c8fd0-6627-40c4-bf9a-024cf001c1f7')) {
    # v3 (Sprint 2): the long approach, click-while-moving and occupancy records are required together with the contract.
    # v4 (Sprint 2 completion): the unmounted control, the endpoint neighbor, the Stop control, the unstarted-while-moving
    # samples and the owned settlement fields (settled cut, body footprint, recovery) on top of v3.
    # v5 (review 2026-10-31, turn-based clock): the outcome carries the supervision clock's elapsed operational seconds beside
    # the observed campaign seconds; the settlement bound applies to the operational figure (campaign time is frozen inside a turn).
    $doorV5=$Door.contract-ceq'two-pair-native-door-click-selection-change-v5'
    $doorV4=$Door.contract-cin@('two-pair-native-door-click-selection-change-v4','two-pair-native-door-click-selection-change-v5')
    $doorV3=$Door.contract-cin@('two-pair-native-door-click-selection-change-v3','two-pair-native-door-click-selection-change-v4','two-pair-native-door-click-selection-change-v5')
    $doorKeys=@('contract','interactions','setupInteractions','cutObservations','fixture','firstPairReturn','restoration','completed')
    if($doorV3){$doorKeys+=@('approach','longApproach','clickWhileMoving')}
    if($doorV4){$doorKeys+=@('unmountedControl','endpointNeighbor','stopControl');if($null-ne$Door.PSObject.Properties['layoutRestore']){$doorKeys+=@('layoutRestore')}}
    Assert-KmcExactProperties $Door $doorKeys 'Chunk 7 native door proof'
    # v1 (through F4): the first pair returned to its captured start. v2 (F4 r30 correction): it returns to a goal
    # clear of every other unit's native avoidance footprint, recorded with its search (Assert-KmcChunk7DoorReturnV2).
    # Old evidence keeps its v1 path and verdict.
    if($Door.contract-cnotin@('two-pair-native-door-click-selection-change','two-pair-native-door-click-selection-change-v2','two-pair-native-door-click-selection-change-v3','two-pair-native-door-click-selection-change-v4','two-pair-native-door-click-selection-change-v5')-or$Door.interactions-isnot[Array]-or$Door.interactions.Count-ne2){throw 'Exactly two captured native door interactions required.'}
    $doorV2=$Door.contract-cin@('two-pair-native-door-click-selection-change-v2','two-pair-native-door-click-selection-change-v3','two-pair-native-door-click-selection-change-v4','two-pair-native-door-click-selection-change-v5')
    Assert-KmcChunk7Boolean $Door.completed $true
    Assert-KmcChunk7WorldCounters $Door @('setupInteractions')
    $f=$Door.fixture
    # Only the current fixture shape, with its clickability, restriction, trigger and unlock
    # records, is admitted: no retained artifact carries a native door proof in the earlier
    # shape, so none needs it (census of runtime-evidence, 2026-10-23).
    $base=@('name','object','position','originalOpen','originalEnabled','originalCut','cutMode','disableOnOpen','radius')
    Assert-KmcExactProperties $f ($base+(Get-KmcChunk7DoorClickFields|ForEach-Object{if($_-ceq'alreadyUnlocked'){'originalAlreadyUnlocked'}else{$_}})) 'captured door fixture'
    Assert-KmcChunk7WorldInt32 $f.object
    if($f.object-eq0-or$f.name-isnot[string]-or[string]::IsNullOrWhiteSpace($f.name)-or$f.name.Length-gt512){throw 'Native door identity is missing.'}
    # An open door is closed as setup before each input; a door captured closed only before the second.
    if($f.originalOpen-isnot[bool]){throw 'Door fixture flags must be native booleans.'}
    # StandardDoor writes its cut only when DisableNavmeshCutWhenOpen is set: a toggling cut is off while
    # open and on while closed; the door never writes a static cut, which must stay unchanged.
    if($f.originalCut-isnot[bool]-or$f.cutMode-isnot[string]-or$f.cutMode-cnotin@('toggles','static')){throw 'Door fixture cut mode is malformed.'}
    if($f.cutMode-ceq'toggles'){Assert-KmcChunk7Boolean $f.originalCut (-not$f.originalOpen)}
    Assert-KmcChunk7DoorCutObservations $Door.cutObservations $f
    foreach($key in @('interactable','availableInteractions','revealed','hasVisibleTrap','trapActive','originalAlreadyUnlocked','hasDialog','dataPresent','viewTriggerActions')){if($f.$key-isnot[bool]){throw 'Door clickability flags must be native booleans.'}}
    Assert-KmcOrdinaryInteger $f.restrictionCount 0 64;Assert-KmcOrdinaryInteger $f.viewTriggerCount 0 64
    # Every captured door: unrestricted, no door-view trigger actions, persistent data present.
    if(-not(Test-KmcChunk7DoorGate $f -Fixture)){throw 'Captured door carries a restriction, door-view trigger actions or no persistent data.'}
    if(-not$f.originalOpen){
        if(-not(Test-KmcChunk7DoorClosedClickable $f)){throw 'A closed door start must be one ordinary player input could open.'}
        Assert-KmcChunk7Boolean $f.originalEnabled $true
    }
    # Setup closes: one at capture when the door was open, one before the second pair's click; v4 adds one after the
    # unmounted control opened the door (when applicable) and one before the Stop control's click.
    $expectedSetup=$(if($f.originalOpen){2}else{1})
    if($doorV4){$expectedSetup+=1;if($Door.unmountedControl.applicable-eq$true){$expectedSetup+=1}}
    if($Door.setupInteractions-ne$expectedSetup){throw 'Fixture preparation must be distinct from the native player inputs.'}
    foreach($v in @($f.originalEnabled,$f.disableOnOpen)){if($v-isnot[bool]){throw 'Door fixture flags must be native booleans.'}}
    foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $f.position.$axis)){throw 'Door position is not finite.'}}
    if(-not(Test-KmcChunk7FiniteNumber $f.radius)-or$f.radius-le0){throw 'Native interaction radius is invalid.'}
    $riders=@($DeclaredRiders);if($riders.Count-ne2-or$riders[0]-ceq$riders[1]){throw 'Exact distinct declared door riders required.'};foreach($id in $riders){Assert-KmcOrdinaryGuid $id}
    $commands=[Collections.Generic.HashSet[long]]::new()
    for($i=0;$i-lt2;$i++) {
        $v=$Door.interactions[$i];$owner=@($Owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $riders[$i]})
        $rowKeys=@('pair','riderId','mountId','ownerId','generation','commandObject','executorId','distanceBefore','radius','selectionAfterInput','accepted','unrelatedIdleAtInput','outcome','terminal','ownerContinuity','unrelatedIdleAtCompletion','distanceMoved')
        if($doorV3){$rowKeys+=@('clickWhileMoving','occupancy')}
        if($doorV4){$rowKeys+=@('movingPolls','startedWhileMoving','unstartedWhileMoving')}
        Assert-KmcExactProperties $v $rowKeys 'native mounted door input'
        if($doorV3){if($v.clickWhileMoving-isnot[bool]){throw 'Door click-while-moving flag must be a native boolean.'};Assert-KmcChunk7DoorOccupancy $v.occupancy $f $(if($doorV4){$v.outcome}else{$null})}
        if($doorV4){
            # The routed command stayed unstarted on every poll on which the mount moved: the native nine-second
            # interrupt (started commands only) cannot apply to the approach, whatever its length.
            Assert-KmcOrdinaryInteger $v.movingPolls 1 100000;Assert-KmcChunk7Boolean $v.startedWhileMoving $false;Assert-KmcChunk7Boolean $v.unstartedWhileMoving $true}
        Assert-KmcChunk7WorldCounters $v @('pair','generation');Assert-KmcChunk7WorldInt32 $v.commandObject
        if($owner.Count-ne1-or$v.pair-ne$i-or-not(Test-KmcChunk7OwnerActorMatch $owner[0].riderId $v.riderId)-or-not(Test-KmcChunk7OwnerActorMatch $owner[0].mountId $v.mountId)-or
            $v.ownerId-cne$owner[0].ownerId-or$v.generation-ne$owner[0].generation-or$v.executorId-cne$v.riderId-or
            $v.selectionAfterInput-cne$riders[1-$i]-or$v.commandObject-eq0-or-not$commands.Add([long]$v.commandObject)){throw 'Door command lost its captured pair, generation, executor or changed selection.'}
        foreach($flag in @('accepted','unrelatedIdleAtInput','terminal','ownerContinuity','unrelatedIdleAtCompletion')){Assert-KmcChunk7Boolean $v.$flag $true}
        foreach($field in @('distanceBefore','radius','distanceMoved')){if(-not(Test-KmcChunk7FiniteNumber $v.$field)){throw 'Native door movement observation must be finite.'}}
        if($v.radius-ne$f.radius-or$v.distanceBefore-le($f.radius+0.5)-or$v.distanceBefore-gt28-or$v.distanceMoved-lt0.5){throw 'Door input did not prove bounded distant approach.'}
        $o=$v.outcome
        $outcomeKeys=@('Result','RiderId','MountId','InteractionCount','DelegatedMoveStartCount','DelegatedMoveTickCount','DoorStateChanged','RiderPathSuppressed','MountMoveSlotRestored','TerminalReason')
        if($doorV3){$outcomeKeys+=@('ApproachesWithoutProgress','ApproachElapsedGameSeconds','DistanceAtInteraction','LineOfSightAtInteraction','OccupancyInsideNavmesh','OccupancyRecovery','OccupancyNearestNodeDistance')}
        if($doorV4){$outcomeKeys+=Get-KmcChunk7DoorSettlementKeys}
        if($doorV5){$outcomeKeys+=@('ApproachElapsedOperationalSeconds','OccupancySettleOperationalSeconds')}
        Assert-KmcExactProperties $o $outcomeKeys 'native door terminal outcome'
        if($doorV5){foreach($field in @('ApproachElapsedOperationalSeconds','OccupancySettleOperationalSeconds')){if(-not(Test-KmcChunk7FiniteNumber $o.$field)-or$o.$field-lt0){throw ('Door outcome '+$field+' is invalid.')}}
            # Real time: the supervision clock and the campaign clock run together (both the engine's per-frame gameplay timestep), so
            # the two approach figures agree closely; a half second admits ticks the action controller skipped around the start and end.
            if([Math]::Abs([double]$o.ApproachElapsedOperationalSeconds-[double]$o.ApproachElapsedGameSeconds)-gt0.5){throw 'Real-time door approach: the operational and campaign elapsed seconds disagree.'}
            if([Math]::Abs([double]$o.OccupancySettleOperationalSeconds-[double]$o.OccupancySettleGameSeconds)-gt0.5){throw 'Real-time door settlement: the operational and campaign elapsed seconds disagree.'}}
        Assert-KmcChunk7WorldCounters $o @('InteractionCount','DelegatedMoveStartCount','DelegatedMoveTickCount')
        # v3: the command retries a finished approach natively (bounded by progress), so approach starts are at least one.
        $approachStartsAdmitted=if($doorV3){$o.DelegatedMoveStartCount-ge1-and$o.DelegatedMoveStartCount-le4}else{$o.DelegatedMoveStartCount-eq1}
        if($o.Result-cne'Success'-or$o.RiderId-cne$v.riderId-or$o.MountId-cne$v.mountId-or$o.InteractionCount-ne1-or-not$approachStartsAdmitted-or$null-ne$o.TerminalReason){throw 'Door outcome did not report exactly one successful native interaction and approach.'}
        foreach($flag in @('DoorStateChanged','RiderPathSuppressed','MountMoveSlotRestored')){Assert-KmcChunk7Boolean $o.$flag $true}
        if($doorV3){
            Assert-KmcOrdinaryInteger $o.ApproachesWithoutProgress 0 2
            foreach($field in @('ApproachElapsedGameSeconds','DistanceAtInteraction','OccupancyNearestNodeDistance')){if(-not(Test-KmcChunk7FiniteNumber $o.$field)-or$o.$field-lt0){throw ('Door outcome '+$field+' is invalid.')}}
            if($o.DistanceAtInteraction-gt($f.radius+0.01)){throw 'Door interaction was delivered outside the native proximity radius.'}
            Assert-KmcChunk7Boolean $o.LineOfSightAtInteraction $true;Assert-KmcChunk7Boolean $o.OccupancyInsideNavmesh $true
            if($doorV4){Assert-KmcChunk7DoorSettlement $o}
            elseif($o.OccupancyRecovery-cne'none'){throw 'Door interaction needed an occupancy recovery; the pair did not stay on the navmesh.'}
        }
    }
    if($doorV3){Assert-KmcChunk7DoorV3Blocks $Door $f -V4:$doorV4}
    if($doorV4){Assert-KmcChunk7DoorV4Blocks $Door $f $riders}
    $r=$Door.firstPairReturn
    $first=@($Owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $riders[0]})[0]
    if($doorV2){
        Assert-KmcExactProperties $r @('command','executorId','terminal','result','goal','commandTarget','position','distanceToGoal','distanceToStart','goalSearch') 'native first-pair return'
        $second=@($Owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $riders[1]})[0]
        Assert-KmcChunk7DoorReturnV2 $r $f $first $second
    } else {
        Assert-KmcExactProperties $r @('command','executorId','terminal','result','distanceToStart') 'native first-pair return'
    }
    Assert-KmcChunk7WorldInt32 $r.command;Assert-KmcChunk7Boolean $r.terminal $true
    $returnDistance=if($doorV2){$r.distanceToGoal}else{$r.distanceToStart}
    if($r.command-eq0-or-not$commands.Add([long]$r.command)-or-not(Test-KmcChunk7OwnerActorMatch $first.mountId $r.executorId)-or$r.result-cne'Success'-or
        -not(Test-KmcChunk7FiniteNumber $returnDistance)-or$returnDistance-lt0-or$returnDistance-gt1.25){throw 'First pair did not return through its own native mount command.'}
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

# DoorReturnGoalPolicy (src/KingmakerMountedCombat/Diagnostics/DoorReturnGoalPolicy.cs), mirrored for the reader.
function Get-KmcChunk7DoorReturnOffset([int]$Index){
    $rings=@(0.5,1.0,1.5,2.0,2.5,3.0,3.5,4.0,5.0)
    if($Index-lt0-or$Index-ge(1+$rings.Count*16)){throw 'Door return candidate index is outside the policy.'}
    if($Index-eq0){return @{ring=0.0;bearing=0.0;dx=0.0;dz=0.0}}
    $ring=$rings[[int][Math]::Floor(($Index-1)/16)];$bearing=22.5*(($Index-1)%16);$rad=$bearing*[Math]::PI/180.0
    @{ring=$ring;bearing=$bearing;dx=[Math]::Sin($rad)*$ring;dz=[Math]::Cos($rad)*$ring}
}
function Get-KmcChunk7DoorReturnReason([bool]$Walkable,[double]$Clamp,[double]$Trace,[double]$DoorDistance,[double]$DoorRadius,[double]$ScanRadius,[double]$Slack){
    if(-not$Walkable){return 'no-walkable-native-node'}
    if($Clamp-gt0.1){return 'off-navmesh'}
    if($Trace-gt0.05){return 'navmesh-trace-blocked'}
    if($DoorDistance-le($DoorRadius+0.5)){return 'inside-door-approach'}
    if($DoorDistance-gt$ScanRadius){return 'outside-door-scan-radius'}
    if($Slack-lt0){return 'inside-unit-footprint'}
    'admitted'
}
function Get-KmcChunk7HorizontalDistance($A,$B){[Math]::Sqrt([Math]::Pow([double]$A.x-[double]$B.x,2)+[Math]::Pow([double]$A.z-[double]$B.z,2))}
function Assert-KmcChunk7DoorPoint($Point,[string]$Label){
    if($null-eq$Point){throw ($Label+' is missing.')}
    Assert-KmcExactProperties $Point @('x','y','z') $Label
    foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $Point.$axis)){throw ($Label+' is not finite.')}}
}
# v2 first-pair return: the recorded search is recomputed from its own recorded inputs (policy order, ring geometry,
# clamp residual, door distance, per-unit clearance slack and nearest blocker within 1e-3 m of the float32 values, reason
# from the recorded values); the trace residual is a native observation held to its policy bound through the reason. The
# search must consider both actors of the other pair and neither actor of the returning pair, stop at its first
# admissible candidate, and the native command must aim at, and the mount end its return near, that goal.
function Assert-KmcChunk7DoorReturnV2($Return,$Fixture,$FirstOwner,$SecondOwner){
    $eps=0.001;$s=$Return.goalSearch
    if($null-eq$s){throw 'Door return goal search is missing.'}
    Assert-KmcExactProperties $s @('contract','start','moverId','moverCorpulence','moverPosition','margin','doorPosition','doorRadius','scanRadius','unitRadius','units','candidates','selectedIndex') 'door return goal search'
    if($s.contract-cne'door-return-goal-clear-of-units/v1'){throw 'Door return goal search contract differs.'}
    if($s.margin-ne0.5-or$s.scanRadius-ne28-or$s.unitRadius-ne11-or$s.doorRadius-ne$Fixture.radius){throw 'Door return goal search constants differ from the policy or fixture.'}
    foreach($pair in @(@($s.start,'return search start'),@($s.moverPosition,'return mover position'),@($s.doorPosition,'return door position'),@($Return.goal,'return goal'),@($Return.commandTarget,'return command target'),@($Return.position,'return end position'))){Assert-KmcChunk7DoorPoint $pair[0] $pair[1]}
    if((Get-KmcChunk7HorizontalDistance $s.doorPosition $Fixture.position)-gt$eps){throw 'Door return search used another door position.'}
    if($s.moverId-isnot[string]-or-not(Test-KmcChunk7OwnerActorMatch $FirstOwner.mountId $s.moverId)){throw 'Door return search mover is not the first pair mount.'}
    if(-not(Test-KmcChunk7FiniteNumber $s.moverCorpulence)-or$s.moverCorpulence-le0){throw 'Door return mover corpulence is invalid.'}
    if($s.units-isnot[Array]-or$s.units.Count-lt2-or$s.units.Count-gt256){throw 'Door return search units are missing or unbounded.'}
    $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($u in $s.units){
        Assert-KmcExactProperties $u @('id','position','corpulence') 'door return search unit'
        [void](ConvertFrom-KmcChunk7ActorUniqueId $u.id);Assert-KmcChunk7DoorPoint $u.position 'door return search unit position'
        if(-not$ids.Add($u.id)-or-not(Test-KmcChunk7FiniteNumber $u.corpulence)-or$u.corpulence-le0){throw 'Door return search unit is duplicated or has no native corpulence.'}
        if((Test-KmcChunk7OwnerActorMatch $FirstOwner.mountId $u.id)-or(Test-KmcChunk7OwnerActorMatch $FirstOwner.riderId $u.id)){throw 'Door return search counted the returning pair as an obstacle.'}
    }
    foreach($key in @('riderId','mountId')){if(@($s.units|Where-Object{Test-KmcChunk7OwnerActorMatch $SecondOwner.$key $_.id}).Count-ne1){throw 'Door return search did not consider the other pair.'}}
    if(-not(Test-KmcExactJsonInteger $s.selectedIndex)-or$s.selectedIndex-lt0-or$s.candidates-isnot[Array]-or$s.candidates.Count-ne($s.selectedIndex+1)){throw 'Door return search did not stop at its first admitted goal.'}
    for($i=0;$i-lt$s.candidates.Count;$i++){
        $c=$s.candidates[$i];$o=Get-KmcChunk7DoorReturnOffset $i
        $walk=$c.walkable-is[bool]-and$c.walkable
        $props=@('index','ring','bearing','requested','walkable','reason');if($walk){$props+=@('clamped','clampResidual','traceResidual','doorDistance','minimumSlack','nearestBlockerId')}
        Assert-KmcExactProperties $c $props 'door return goal candidate'
        if($c.walkable-isnot[bool]-or-not(Test-KmcExactJsonInteger $c.index)-or$c.index-ne$i-or-not(Test-KmcChunk7FiniteNumber $c.ring)-or-not(Test-KmcChunk7FiniteNumber $c.bearing)-or
           [Math]::Abs([double]$c.ring-$o.ring)-gt$eps-or[Math]::Abs([double]$c.bearing-$o.bearing)-gt$eps){throw 'Door return candidate order differs from the policy.'}
        Assert-KmcChunk7DoorPoint $c.requested 'door return requested point'
        if([Math]::Abs([double]$c.requested.x-([double]$s.start.x+$o.dx))-gt$eps-or[Math]::Abs([double]$c.requested.z-([double]$s.start.z+$o.dz))-gt$eps-or
           [Math]::Abs([double]$c.requested.y-[double]$s.start.y)-gt$eps){throw 'Door return requested point differs from the policy ring.'}
        if($walk){
            Assert-KmcChunk7DoorPoint $c.clamped 'door return clamped point'
            foreach($field in @('clampResidual','traceResidual','doorDistance','minimumSlack')){if(-not(Test-KmcChunk7FiniteNumber $c.$field)){throw 'Door return candidate value is not finite.'}}
            if($c.traceResidual-lt0-or[Math]::Abs((Get-KmcChunk7HorizontalDistance $c.requested $c.clamped)-$c.clampResidual)-gt$eps-or
               [Math]::Abs((Get-KmcChunk7HorizontalDistance $c.clamped $s.doorPosition)-$c.doorDistance)-gt$eps){throw 'Door return candidate residual or door distance does not recompute.'}
            $min=[double]::PositiveInfinity
            foreach($u in $s.units){$v=(Get-KmcChunk7HorizontalDistance $c.clamped $u.position)-([double]$s.moverCorpulence+[double]$u.corpulence+[double]$s.margin);if($v-lt$min){$min=$v}}
            if([Math]::Abs($min-[double]$c.minimumSlack)-gt$eps){throw 'Door return candidate clearance does not recompute.'}
            $named=@($s.units|Where-Object id -CEQ $c.nearestBlockerId)
            if($named.Count-ne1-or[Math]::Abs((Get-KmcChunk7HorizontalDistance $c.clamped $named[0].position)-([double]$s.moverCorpulence+[double]$named[0].corpulence+[double]$s.margin)-$min)-gt$eps){throw 'Door return candidate nearest blocker does not recompute.'}
            $reason=Get-KmcChunk7DoorReturnReason $true $c.clampResidual $c.traceResidual $c.doorDistance $s.doorRadius $s.scanRadius $c.minimumSlack
        } else {$reason='no-walkable-native-node'}
        if($c.reason-cne$reason){throw 'Door return candidate reason differs from its recorded values.'}
        if(($i-eq$s.selectedIndex)-ne($reason-ceq'admitted')){throw 'Door return search admitted a goal other than its first admissible candidate.'}
    }
    $chosen=$s.candidates[$s.selectedIndex]
    foreach($axis in @('x','y','z')){if([Math]::Abs([double]$Return.goal.$axis-[double]$chosen.clamped.$axis)-gt$eps){throw 'Door return goal is not the admitted candidate.'}}
    if((Get-KmcChunk7HorizontalDistance $Return.commandTarget $Return.goal)-gt0.3){throw 'Native return command did not aim at the admitted goal.'}
    if(-not(Test-KmcChunk7FiniteNumber $Return.distanceToGoal)-or-not(Test-KmcChunk7FiniteNumber $Return.distanceToStart)-or
       [Math]::Abs((Get-KmcChunk7HorizontalDistance $Return.position $Return.goal)-$Return.distanceToGoal)-gt$eps-or
       [Math]::Abs((Get-KmcChunk7HorizontalDistance $Return.position $s.start)-$Return.distanceToStart)-gt$eps){throw 'Door return distances do not recompute from the recorded end position.'}
}

# GroundGoalFootprintPolicy route moves (src/KingmakerMountedCombat/Diagnostics/GroundGoalFootprintPolicy.cs and
# Chunk7WorldScenarioEngine.RouteGoal.cs), mirrored for the reader: the declared 2 m route first, then the
# NativeGroundStepDestinationPolicy bearing offsets (Unity Quaternion.Euler(0, offset, 0), clockwise seen from +y).
function Get-KmcChunk7RouteReason([bool]$Walkable,[double]$Clamp,[double]$Trace,[double]$Slack){
    if(-not$Walkable){return 'no-walkable-native-node'}
    if($Clamp-gt0.1){return 'off-navmesh'}
    if($Trace-gt0.05){return 'navmesh-trace-blocked'}
    if($Slack-lt0){return 'inside-unit-footprint'}
    'admitted'
}
# Every search is recomputed from its own recorded inputs (policy order and route geometry, clamp residual, per-unit
# clearance slack and nearest blocker within 1e-3 m of the float32 values, reason); the trace residual is a native
# observation held to its bound through the reason. Each search must exclude exactly the moving pair, consider every
# other scenario actor, stop at its first admissible candidate, and its movement must start where it did and aim at it.
function Assert-KmcChunk7WorldRouteGoals($Searches,$Movements,$Specs,$Actors){
    $eps=0.001;$offsets=@(0.0,22.5,-22.5,45.0,-45.0,67.5,-67.5,90.0,-90.0,112.5,-112.5,135.0,-135.0,157.5,-157.5,180.0)
    $directions=@{"mammoth"=@(1.0,0.0);"horse"=@(0.0,1.0);"unmounted-control"=@(-1.0,0.0)}
    if($Searches-isnot[Array]-or$Searches.Count-ne$Specs.Count-or$Movements.Count-ne$Specs.Count){throw 'Route goal searches do not cover exactly the native movements.'}
    for($m=0;$m-lt$Specs.Count;$m++){
        $s=$Searches[$m];$spec=$Specs[$m];$v=$Movements[$m]
        Assert-KmcExactProperties $s @('contract','label','selectedId','moverId','start','direction','distance','margin','unitRadius','moverCorpulence','units','candidates','selectedIndex') 'route goal search'
        if($s.contract-cne'world-route-goal-clear-of-units/v1'){throw 'Route goal search contract differs.'}
        if($s.label-cne$spec[0]-or$s.selectedId-cne$spec[1]-or$s.moverId-cne$spec[2]){throw 'Route goal search is not its movement''s exact route.'}
        if($s.distance-ne2-or$s.margin-ne0.5-or$s.unitRadius-ne11){throw 'Route goal search constants differ from the policy.'}
        if(-not(Test-KmcChunk7FiniteNumber $s.moverCorpulence)-or$s.moverCorpulence-le0){throw 'Route goal mover corpulence is invalid.'}
        foreach($pair in @(@($s.start,'route search start'),@($s.direction,'route direction'))){Assert-KmcChunk7DoorPoint $pair[0] $pair[1]}
        $d=$directions[[string]$s.label]
        if([Math]::Abs([double]$s.direction.x-$d[0])-gt1e-6-or[Math]::Abs([double]$s.direction.z-$d[1])-gt1e-6-or[double]$s.direction.y-ne0){throw 'Route goal search does not start from its declared direction.'}
        if($s.units-isnot[Array]-or$s.units.Count-gt256){throw 'Route goal search units are missing or unbounded.'}
        $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach($u in $s.units){
            Assert-KmcExactProperties $u @('id','position','corpulence') 'route goal search unit'
            if($u.id-isnot[string]){throw 'Route goal search unit identity is invalid.'}
            Assert-KmcChunk7DoorPoint $u.position 'route goal search unit position'
            if(-not$ids.Add($u.id)-or-not(Test-KmcChunk7FiniteNumber $u.corpulence)-or$u.corpulence-le0){throw 'Route goal search unit is duplicated or has no native corpulence.'}
            if($u.id-ceq$spec[1]-or$u.id-ceq$spec[2]){throw 'Route goal search counted the moving pair as an obstacle.'}
        }
        foreach($a in @($Actors|Where-Object{$_-cne$spec[1]-and$_-cne$spec[2]})){if(-not$ids.Contains([string]$a)){throw 'Route goal search did not consider every other scenario actor.'}}
        if(-not(Test-KmcExactJsonInteger $s.selectedIndex)-or$s.selectedIndex-lt0-or$s.candidates-isnot[Array]-or$s.candidates.Count-ne($s.selectedIndex+1)-or$s.candidates.Count-gt$offsets.Count){throw 'Route goal search did not stop at its first admitted goal.'}
        for($i=0;$i-lt$s.candidates.Count;$i++){
            $c=$s.candidates[$i];$offset=$offsets[$i]
            $walk=$c.walkable-is[bool]-and$c.walkable
            $props=@('index','bearingOffset','requested','walkable','reason');if($walk){$props+=@('clamped','clampResidual','traceResidual','minimumSlack','nearestBlockerId')}
            Assert-KmcExactProperties $c $props 'route goal candidate'
            if($c.walkable-isnot[bool]-or-not(Test-KmcExactJsonInteger $c.index)-or$c.index-ne$i-or-not(Test-KmcChunk7FiniteNumber $c.bearingOffset)-or[Math]::Abs([double]$c.bearingOffset-$offset)-gt$eps){throw 'Route goal candidate order differs from the policy.'}
            Assert-KmcChunk7DoorPoint $c.requested 'route requested point'
            $rad=$offset*[Math]::PI/180.0;$hx=$d[0]*[Math]::Cos($rad)+$d[1]*[Math]::Sin($rad);$hz=-$d[0]*[Math]::Sin($rad)+$d[1]*[Math]::Cos($rad)
            if([Math]::Abs([double]$c.requested.x-([double]$s.start.x+2.0*$hx))-gt$eps-or[Math]::Abs([double]$c.requested.z-([double]$s.start.z+2.0*$hz))-gt$eps-or
               [Math]::Abs([double]$c.requested.y-[double]$s.start.y)-gt$eps){throw 'Route requested point differs from the policy route.'}
            if($walk){
                Assert-KmcChunk7DoorPoint $c.clamped 'route clamped point'
                foreach($field in @('clampResidual','traceResidual','minimumSlack')){if(-not(Test-KmcChunk7FiniteNumber $c.$field)){throw 'Route goal candidate value is not finite.'}}
                if($c.traceResidual-lt0-or[Math]::Abs((Get-KmcChunk7HorizontalDistance $c.requested $c.clamped)-$c.clampResidual)-gt$eps){throw 'Route goal candidate clamp residual does not recompute.'}
                if($s.units.Count-eq0){throw 'Route goal candidate clearance has no recorded unit.'}
                $min=[double]::PositiveInfinity
                foreach($u in $s.units){$x=(Get-KmcChunk7HorizontalDistance $c.clamped $u.position)-([double]$s.moverCorpulence+[double]$u.corpulence+[double]$s.margin);if($x-lt$min){$min=$x}}
                if([Math]::Abs($min-[double]$c.minimumSlack)-gt$eps){throw 'Route goal candidate clearance does not recompute.'}
                $named=@($s.units|Where-Object id -CEQ $c.nearestBlockerId)
                if($named.Count-ne1-or[Math]::Abs((Get-KmcChunk7HorizontalDistance $c.clamped $named[0].position)-([double]$s.moverCorpulence+[double]$named[0].corpulence+[double]$s.margin)-$min)-gt$eps){throw 'Route goal candidate nearest blocker does not recompute.'}
                $reason=Get-KmcChunk7RouteReason $true $c.clampResidual $c.traceResidual $c.minimumSlack
            } else {$reason='no-walkable-native-node'}
            if($c.reason-cne$reason){throw 'Route goal candidate reason differs from its recorded values.'}
            if(($i-eq$s.selectedIndex)-ne($reason-ceq'admitted')){throw 'Route goal search admitted a goal other than its first admissible candidate.'}
        }
        $chosen=$s.candidates[$s.selectedIndex]
        foreach($axis in @('x','y','z')){
            if([Math]::Abs([double]$v.requestedGoal.$axis-[double]$chosen.requested.$axis)-gt$eps){throw 'Native movement did not aim at its admitted route goal.'}
            if([Math]::Abs([double]$v.start.$axis-[double]$s.start.$axis)-gt$eps){throw 'Native movement did not start where its route goal search did.'}
        }
    }
}

# The settled cut state at each lease boundary, in order, consistent with the fixture's cut mode.
function Assert-KmcChunk7DoorCutObservations($Observations,$Fixture){
    $expected=@(@('captured',$Fixture.originalOpen),@('input-ready-0',$false),@('after-click-0',$true),@('input-ready-1',$false),@('after-click-1',$true),@('restored',$Fixture.originalOpen))
    if($Observations-isnot[Array]-or$Observations.Count-ne$expected.Count){throw 'Door cut observations must cover each lease boundary exactly once.'}
    for($i=0;$i-lt$expected.Count;$i++){
        $o=$Observations[$i];Assert-KmcExactProperties $o @('boundary','open','cutEnabled') 'door cut observation'
        if($o.boundary-isnot[string]-or$o.boundary-cne$expected[$i][0]-or$o.open-isnot[bool]-or$o.cutEnabled-isnot[bool]){throw 'Door cut observation boundary or state is malformed.'}
        Assert-KmcChunk7Boolean $o.open ([bool]$expected[$i][1])
        if($Fixture.cutMode-ceq'toggles'){if($o.cutEnabled-eq$o.open){throw 'A toggling door cut did not follow its native open state.'}}
        elseif($o.cutEnabled-ne$Fixture.originalCut){throw 'A static door cut changed during the lease.'}
    }
}
# The probe's one door predicate (Chunk7NativeDoorProbe.DoorFacts), mirrored for readers.
# The term lists are tied to DoorFacts.ClosedClickable and DoorFacts.Eligible by a source test.
function Get-KmcChunk7DoorClickFields {@('interactable','availableInteractions','revealed','hasVisibleTrap','trapActive','alreadyUnlocked','restrictionCount','hasDialog','dataPresent','viewTriggerCount','viewTriggerActions')}
function Get-KmcChunk7DoorClosedClickableTerms {@('interactable','availableInteractions','revealed','!hasVisibleTrap','!trapActive','!hasDialog')}
function Get-KmcChunk7DoorGateTerms {@('active','dataPresent','cutPresent','restrictionCount==0','!viewTriggerActions')}
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
    $terms=@(Get-KmcChunk7DoorGateTerms);if($Fixture){$terms=@($terms|Where-Object{$_-cnotin@('active','cutPresent')})}
    Test-KmcChunk7DoorTerms $Value $terms
}
# Bounded nearest-first native door rows. A legacy row (before the closed-door predicate)
# is admitted only when it could not qualify open or closed; a current row carries the clickability flags.
function Assert-KmcChunk7NativeDoorRows($Rows,$Mounts,[int]$CandidateCount,[string]$Label,[switch]$CurrentOnly){
    if($Rows-isnot[Array]-or$Rows.Count-ne[Math]::Min(32,$CandidateCount)){throw ($Label+' rows do not match the bounded native candidate count.')}
    $base=@('name','position','active','open','disableCutWhenOpen','cutPresent','proximityRadius','mountDistances')
    # Review 2026-10-31: a current row may carry the native combat-usability flag (notInCombat) beside eligibility; one or
    # two mounted actors decide the distances (the owner-surface inventory has one).
    $mountCount=@($Mounts).Count;if($mountCount-lt1-or$mountCount-gt2){throw ($Label+' inventory needs one or two mounted actors.')}
    $flagged=@($Rows|Where-Object{$null-ne$_.PSObject.Properties['notInCombat']}).Count
    if($flagged-ne0-and$flagged-ne$Rows.Count){throw ($Label+' inventory mixes rows with and without the combat-usability flag.')}
    # One shape per inventory. A legacy row (no clickability flags) is admitted only when it is
    # ineligible whatever those flags would say, so it can never hide a qualifying closed door.
    $legacyRows=@($Rows|Where-Object{$null-eq$_.PSObject.Properties['revealed']}).Count
    if($legacyRows-ne0-and$legacyRows-ne$Rows.Count){throw ($Label+' inventory mixes legacy and current row shapes.')}
    $prior=-1.0
    foreach($row in $Rows){
        $legacy=$legacyRows-ne0
        if($legacy-and$CurrentOnly){throw ($Label+' row lacks its clickability flags.')}
        Assert-KmcExactProperties $row $(if($legacy){$base+@('eligible')}else{$base+(Get-KmcChunk7DoorClickFields)+@('eligible')+$(if($flagged-ne0){@('notInCombat')}else{@()})}) $Label
        if($flagged-ne0-and$row.notInCombat-isnot[bool]){throw ($Label+' combat-usability flag is not Boolean.')}
        if($row.name-isnot[string]-or[string]::IsNullOrWhiteSpace($row.name)-or$row.name.Length-gt256-or
           $row.mountDistances-isnot[Array]-or$row.mountDistances.Count-ne$mountCount-or
           -not(Test-KmcChunk7FiniteNumber $row.proximityRadius)-or[double]$row.proximityRadius-le0){throw ($Label+' identity or radius is malformed.')}
        foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $row.position.$axis)){throw ($Label+' position is nonfinite.')}}
        for($i=0;$i-lt$mountCount;$i++){
            $start=$Mounts[$i].position
            $distance=[Math]::Sqrt([Math]::Pow([double]$start.x-[double]$row.position.x,2)+[Math]::Pow([double]$start.z-[double]$row.position.z,2))
            if(-not(Test-KmcChunk7FiniteNumber $row.mountDistances[$i])-or
               [Math]::Abs($distance-[double]$row.mountDistances[$i])-gt0.001){throw ($Label+' distance differs from mounted actor geometry.')}
        }
        $sum=0.0;foreach($d in $row.mountDistances){$sum+=[double]$d}
        if($sum+0.001-lt$prior){throw ($Label+' inventory is not nearest-first.')};$prior=$sum
        foreach($key in @('active','open','disableCutWhenOpen','cutPresent','eligible')){if($row.$key-isnot[bool]){throw ($Label+' state is not Boolean.')}}
        $distancesOk=@($row.mountDistances|Where-Object{[double]$_-le[double]$row.proximityRadius+0.5-or[double]$_-gt28.0}).Count-eq0
        if($legacy){
            # A static (non-toggling) cut no longer makes a door ineligible, so only inactivity, a missing
            # cut or the distance bounds keep a legacy row from hiding a qualifying door.
            if($row.active-and$row.cutPresent-and$distancesOk){throw ($Label+' legacy row could hide a qualifying door; its clickability flags are required.')}
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

# Sprint 2 (MOVE-01 on the owner's surface): the pre-owned single-pair speed mode. A disposable copy of the owner's own
# registered save loads through the missing-metadata path; the one pre-owned pair is bound exactly (nothing built, granted,
# levelled or acquired; no second pair; zero writes); the ordinary native Mount, the four aligned speed legs, the ordinary
# native Dismount and cleanup are the whole observation. Every leg must have run on this surface for the row to PASS.
function Get-KmcChunk7OwnerSurfaceFields {@('contract','seedId','seedSha256','riderId','horseId','profileId','horseBlueprint','horseFeatureSourceGuid','preOwnedPair','fixtureSetupOnly','acquisitionProof','producerAncestry','nativeLoadVerifiedByHost','loadedArchiveKind','loadedArchiveCanRestore','mutations','secondPair','selectionChangeActorId','area','gameName','gameId','partyIds','legsCompleted','legsApplicable')}
function Assert-KmcChunk7OwnerSurfaceSpeedEvidence($Request,$Observations){
    Assert-KmcChunk7OwnedPairsDescriptor $Request
    if(-not(Test-KmcChunk7OwnedPairsOwnerSurfaceSpeed $Request)){throw 'Owner-surface speed evidence belongs only to its own selection.'}
    $s=$Request.ownedPairs;$o=$Observations;$d=$Request.persistenceLoad;$rider=[string]$s.riderAId;$horse=[string]$s.mountAId
    foreach($key in @('inheritedFixture','inheritedCleanup','acquisition','ownerSeedSetup','ownerSeed','door','routeGoalContract','routeGoalSearches','failurePhase','setupComplete')){if($null-ne$o.PSObject.Properties[$key]){throw ('Owner-surface evidence cannot carry '+$key+'.')}}
    foreach($key in @('cycles','measurements','movements','cycleInputs','groundInputs')){if($null-eq$o.PSObject.Properties[$key]-or@($o.$key).Count-ne0){throw ('Owner-surface speed runs no '+$key+'.')}}
    $u=$o.ownerSurface;Assert-KmcExactProperties $u (Get-KmcChunk7OwnerSurfaceFields) 'owner-surface pre-owned pair identity'
    Assert-KmcBootstrapInventoryString $u.contract 'owner-surface-pre-owned-pair-speed-v1'
    foreach($pair in @(@('seedId',$s.seedId),@('seedSha256',$s.seedSha256),@('riderId',$rider),@('horseId',$horse),@('profileId','medium-humanoid-horse-v1'),@('horseBlueprint','4016c7db400ab721ff125aef9e65e202'),@('loadedArchiveKind','Missing'),@('area',$d.area),@('gameName',$d.gameName),@('gameId',$d.gameId))){Assert-KmcBootstrapInventoryString $u.($pair[0]) $pair[1]}
    if($u.horseFeatureSourceGuid-isnot[string]-or$u.horseFeatureSourceGuid-cnotmatch'^[a-f0-9]{32}$'){throw 'Owner-surface Horse feature source is not a blueprint id.'}
    if($u.mutations-isnot[string]-or$u.mutations-cnotlike'none:*'){throw 'Owner-surface speed must declare no mutation.'}
    foreach($key in @('fixtureSetupOnly','acquisitionProof','producerAncestry','loadedArchiveCanRestore','secondPair')){Assert-KmcChunk7Boolean $u.$key $false}
    Assert-KmcChunk7Boolean $u.nativeLoadVerifiedByHost $true
    Assert-KmcOrdinaryGuid $u.selectionChangeActorId;if($u.selectionChangeActorId-cnotin@($s.partyIds)){throw 'Owner-surface selection change actor is outside the field roster.'}
    Assert-KmcChunk7ExactIds @(@($u.partyIds)|ForEach-Object{[pscustomobject]@{id=$_}}) @($s.partyIds)
    Assert-KmcOrdinaryInteger $u.legsCompleted 4 4;Assert-KmcOrdinaryInteger $u.legsApplicable 4 4
    $q=$u.preOwnedPair
    Assert-KmcExactProperties $q @('riderId','horseId','petIsKmcHorse','petReciprocal','petInField','horseFeature','horseRank','characterLevel','rangerLevel','experience','readyLevel','source') 'owner pre-owned pair facts'
    Assert-KmcBootstrapInventoryString $q.riderId $rider;Assert-KmcBootstrapInventoryString $q.horseId $horse
    foreach($key in @('petIsKmcHorse','petReciprocal','petInField','horseFeature','horseRank')){Assert-KmcChunk7Boolean $q.$key $true}
    foreach($key in @('characterLevel','rangerLevel','experience','readyLevel')){Assert-KmcOrdinaryInteger $q.$key 0 1000000000}
    $c=$o.capability;Assert-KmcExactProperties $c @('archiveNativeEnabled','nativeWriterSchema','nativePairCapacity','workingSha256','inheritedManualSha256','ownerSeedSha256') 'owner-surface capability'
    Assert-KmcChunk7Boolean $c.archiveNativeEnabled $true;Assert-KmcChunk7WorldCounters $c @('nativeWriterSchema','nativePairCapacity')
    if($c.nativeWriterSchema-ne3-or$c.nativePairCapacity-lt2-or$null-ne$c.workingSha256-or$null-ne$c.inheritedManualSha256){throw 'Owner-surface capability differs or claims Working/inherited provenance.'}
    Assert-KmcBootstrapInventoryString $c.ownerSeedSha256 $s.seedSha256
    Assert-KmcChunk7ExactIds @($o.partyBefore) @($s.partyIds)
    $r=@($o.partyBefore|Where-Object id -CEQ $rider);$m=@($o.partyBefore|Where-Object id -CEQ $horse)
    if($r.Count-ne1-or$m.Count-ne1-or$r[0].petId-cne$horse-or$m[0].masterId-cne$rider-or$m[0].blueprint-cne'4016c7db400ab721ff125aef9e65e202'){throw 'The owner pair is not the reciprocal pre-owned KMC Horse pair in the loaded roster.'}
    $actors=@($rider,$horse);$selectionActors=@($rider,$horse,[string]$u.selectionChangeActorId)|Select-Object -Unique
    # Inputs: exactly the ordinary native Mount and Dismount of the owner's pair (the resources windows name the two pair actors).
    $inputSpecs=@(@('mount-owner-pair',$rider,$horse,'f053faad986631688defa003cd7bda0e'),@('dismount-owner-pair',$rider,$rider,'3af2b81f4d72bbb30501fa730fcdf36e'))
    $rows=@($o.inputs);if($rows.Count-ne2){throw 'Owner-surface speed issues exactly two native relationship inputs.'}
    $commands=New-Object 'Collections.Generic.HashSet[long]';$shells=New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    for($i=0;$i-lt2;$i++){$v=$rows[$i];$sp=$inputSpecs[$i]
        Assert-KmcChunk7WorldCounters $v @('nativeCastDelta','nativeRefusalDelta','generationAtInit');Assert-KmcChunk7WorldInt32 $v.commandObject
        Assert-KmcChunk7Boolean $v.clicked $true;Assert-KmcChunk7Boolean $v.shellTerminal $true;Assert-KmcChunk7Boolean $v.processTerminal $true;Assert-KmcChunk7Boolean $v.explorationResourcesConserved $true
        if($v.label-cne$sp[0]-or$v.casterId-cne$sp[1]-or$v.targetId-cne$sp[2]-or$v.abilityGuid-cne$sp[3]-or$v.nativeCastDelta-ne1-or$v.nativeRefusalDelta-ne0-or$v.commandObject-eq0-or-not$commands.Add([long]$v.commandObject)-or
            [string]::IsNullOrEmpty($v.shellControl)-or-not$shells.Add([string]$v.shellControl)-or$v.generationAtInit-lt0-or$v.selectionAfterInput-cnotin$selectionActors){throw 'Owner-surface native input command/shell/selection identity differs.'}
        Assert-KmcChunk7Resources $v.resourcesBefore $v.resourcesAfter $actors}
    # Ownership windows: unmounted at start, exactly one active owner while mounted, none afterwards.
    foreach($pair in @(@('owner-surface-start',0),@('owner-surface-mounted',1),@('owner-surface-dismounted',0),@('owner-surface-legs-complete',0))){
        $w=@($o.snapshots|Where-Object label -CEQ $pair[0]);if($w.Count-ne1){throw ('Owner-surface window missing: '+$pair[0])}
        $active=@($w[0].owners|Where-Object state -CEQ 'Active');if($active.Count-ne$pair[1]){throw ('Owner-surface window '+$pair[0]+' has '+$active.Count+' active owners.')}
        foreach($owner in $w[0].owners){Assert-KmcChunk7WorldCounters $owner @('generation')}
        if($pair[1]-eq1){$a=$active[0];[Guid]$id=[Guid]::Empty
            if(-not[Guid]::TryParse([string]$a.ownerId,[ref]$id)-or$a.relationshipState-cne'Mounted'-or$a.generation-lt1-or-not(Test-KmcChunk7OwnerActorMatch $a.riderId $rider)-or-not(Test-KmcChunk7OwnerActorMatch $a.mountId $horse)){throw 'Owner-surface active owner is not the owner pair.'}}
    }
    # The four aligned legs on this surface: mounted with the party, mounted alone, the Horse alone, the rider on foot.
    if($null-eq$o.PSObject.Properties['speedLegs']){throw 'Owner-surface speed recorded no speed legs.'}
    if($o.speedLegContract-cnotin@('sprint2-speed-legs/v3','sprint2-speed-legs/v4','sprint2-speed-legs/v5','sprint2-speed-legs/v6')){throw 'Owner-surface speed requires the v3 or later speed legs (carried riders recorded from pair ownership).'}
    Assert-KmcChunk7Sprint2Legs $o
    # Review 2026-10-31 (v5 legs): the one bounded read-only door inventory of the owner's surface area, taken at the start
    # from the Horse's position (the combat-usability flag beside eligibility). Nothing was acquired, closed or clicked.
    if(($null-ne$o.PSObject.Properties['doorInventory'])-ne($o.speedLegContract-cin@('sprint2-speed-legs/v5','sprint2-speed-legs/v6'))){throw 'Owner-surface door inventory belongs to the v5 legs exactly.'}
    if($null-ne$o.PSObject.Properties['doorInventory']){$inv=$o.doorInventory
        Assert-KmcExactProperties $inv @('boundary','candidateCount','rowLimit','radius','mounts','truncated','rows','interactionTested') 'owner-surface door inventory'
        Assert-KmcBootstrapInventoryString $inv.boundary 'owner-surface-start';Assert-KmcOrdinaryInteger $inv.candidateCount 0 100000;Assert-KmcOrdinaryInteger $inv.rowLimit 32 32
        if(-not(Test-KmcChunk7FiniteNumber $inv.radius)-or[double]$inv.radius-ne28.0){throw 'Owner-surface door inventory radius differs.'}
        Assert-KmcChunk7Boolean $inv.truncated ($inv.candidateCount-gt32);Assert-KmcChunk7Boolean $inv.interactionTested $false
        if($inv.mounts-isnot[Array]-or$inv.mounts.Count-ne1-or$inv.mounts[0].id-cne$horse){throw 'Owner-surface door inventory was not taken from the owner''s Horse.'}
        Assert-KmcChunk7NativeDoorRows $inv.rows $inv.mounts ([int]$inv.candidateCount) 'owner-surface door inventory' -CurrentOnly
        if(@($inv.rows|Where-Object{$null-eq$_.PSObject.Properties['notInCombat']}).Count-ne0){throw 'Owner-surface door inventory rows lack the combat-usability flag.'}}
    $legs=@($o.speedLegs);if($legs.Count-ne4){throw 'Owner-surface speed requires all four aligned legs.'}
    $primary=@($horse,$horse,$horse,$rider)
    $mountedOwner=@(@($o.snapshots|Where-Object label -CEQ 'owner-surface-mounted')[0].owners|Where-Object state -CEQ 'Active')
    for($i=0;$i-lt4;$i++){$leg=$legs[$i]
        if(-not$leg.applicable){throw ('Owner-surface leg '+$leg.label+' was not applicable on this surface; the comparison is incomplete.')}
        if($leg.primaryMoverId-cne$primary[$i]){throw ('Owner-surface leg '+$leg.label+' measured another mover.')}
        Assert-KmcBootstrapInventoryString $leg.environment.area $d.area
        $movers=@($leg.movers);$roles=@($movers|Where-Object{$_.id-ceq$leg.primaryMoverId})[0].role
        $expectedRole=if($i-le1){'mount'}elseif($i-eq2){'pet'}else{'on-foot'};if($roles-cne$expectedRole){throw ('Owner-surface leg '+$leg.label+' primary mover role differs: '+$roles)}
        foreach($mv in $movers){if($mv.id-cnotin@($s.partyIds)-or$mv.executorId-cne$mv.id){throw ('Owner-surface leg '+$leg.label+' moved an actor outside the owner roster or through another executor.')}}
        $riderRows=@($movers|Where-Object id -CEQ $rider);$horseRows=@($movers|Where-Object id -CEQ $horse);$carried=@($leg.carried)
        if($i-le1){
            # Mounted legs: the ground click is routed to the Horse; the rider has no locomotion command of its own and is
            # proven carried from pair ownership and its position beside the Horse (Assert-KmcChunk7SpeedLegCarried).
            if($riderRows.Count-ne0){throw ('Owner-surface mounted leg '+$leg.label+' carries an independent rider locomotion command; mounted ground input is routed to the Horse.')}
            if($horseRows.Count-ne1-or$horseRows[0].role-cne'mount'){throw ('Owner-surface mounted leg '+$leg.label+' did not move the Horse as the pair''s mount.')}
            if($carried.Count-ne1-or$carried[0].riderId-cne$rider-or$carried[0].mountId-cne$horse){throw ('Owner-surface mounted leg '+$leg.label+' did not carry the owner''s rider on its Horse.')}
            if($mountedOwner.Count-ne1-or[string]$carried[0].ownerId-cne[string]$mountedOwner[0].ownerId-or$carried[0].generation-ne$mountedOwner[0].generation){throw ('Owner-surface mounted leg '+$leg.label+' was carried under another owner or generation than the mounted window.')}
            if($i-eq1-and$movers.Count-ne1){throw 'The mounted-solo leg must move the Horse alone.'}
        }else{
            if($carried.Count-ne0){throw ('Owner-surface unmounted leg '+$leg.label+' reports a carried rider.')}
            if($movers.Count-ne1){throw ('Owner-surface solo leg '+$leg.label+' moved more than its one actor.')}
            if($i-eq2-and($horseRows.Count-ne1-or$horseRows[0].role-cne'pet')){throw 'The horse-solo leg did not move the unmounted Horse as the owner''s pet.'}
            if($i-eq3-and($riderRows.Count-ne1-or$riderRows[0].role-cne'on-foot')){throw 'The rider-solo leg did not move the rider on foot.'}
        }}
    if(@($legs[0].selectedIds).Count-lt2){throw 'The party-paced leg did not select the party.'}
    # Cleanup: the owner's pair is preserved (never removed), no save written, settings and selection restored.
    $c=$o.cleanup
    foreach($field in @('pairResidueAbsent','controlsSettled','settingsAndSelectionRestored','nativeProcessesSettled','cleanupCustodySettled','ownerSurfacePairPreserved')){Assert-KmcChunk7Boolean $c.$field $true}
    foreach($field in @('horseRemoved','inheritedPetsPreserved','nativeProgressionRequiresExternalWorkingRestoration','ordinaryRespecProven','nativeSaveWritten')){Assert-KmcChunk7Boolean $c.$field $false}
    foreach($field in @('combatOrPersistenceProof','thirdUnrelatedActorProven')){Assert-KmcChunk7Boolean $o.$field $false}
    foreach($field in @('pauseBefore','pauseAfter','movementBefore','movementAfter','pairedBefore','pairedAfter')){if($c.$field-isnot[bool]){throw 'Restored native settings require actual booleans.'}}
    Assert-KmcChunk7ExactIds @($c.partyAfter) @($s.partyIds)
    $pr=@($c.partyAfter|Where-Object id -CEQ $rider);$pm=@($c.partyAfter|Where-Object id -CEQ $horse)
    if($pr.Count-ne1-or$pm.Count-ne1-or$pr[0].petId-cne$horse-or$pm[0].masterId-cne$rider){throw 'The owner pair lost its reciprocal custody by cleanup.'}
    if(($c.selectionBefore-join'|')-cne($c.selectionAfter-join'|')-or$c.pauseBefore-ne$c.pauseAfter-or$c.movementBefore-ne$c.movementAfter-or$c.pairedBefore-ne$c.pairedAfter){throw 'Settings/selection restoration differs.'}
    if(@($c.world.owners|Where-Object state -CNE 'Retired').Count-ne0-or@($c.pairBoundaries).Count-lt1){throw 'World cleanup left retained ownership.'}
    foreach($pair in $c.pairBoundaries){if($pair.state-cne'Unmounted'-or$pair.residueAbsent-ne$true-or$pair.ownerPresent-ne$false){throw 'Pair cleanup incomplete.'}}
    $admission=$o.factRemovalAdmission
    foreach($field in @('owners','activePairs','frame','gameTicks')){if(-not(Test-KmcExactJsonInteger $admission.$field)-or$admission.$field-lt0){throw 'Fact-removal admission counter is missing or mistyped.'}}
    if($admission.owners-ne0-or$admission.activePairs-ne0-or$admission.commands-isnot[Array]){throw 'Owner-surface admission ran before settled ownership.'}
    foreach($field in @('preparationPending','serializationBusy','pendingNativeWrites')){Assert-KmcChunk7Boolean $admission.$field $false}
    $legCommands=@();foreach($leg in $legs){foreach($mv in @($leg.movers)){$legCommands+=@([string]$mv.commandObject)}}
    Assert-KmcChunk7ExactIds $admission.commands @(@($rows|ForEach-Object{[string]$_.commandObject})+$legCommands) 'commandObject'
    foreach($command in $admission.commands){Assert-KmcChunk7WorldInt32 $command.commandObject;Assert-KmcChunk7Boolean $command.finished $true;Assert-KmcChunk7Boolean $command.processEnded $true}
}
# Sprint 2 (DOOR-01 / SPACE-01 in native turn-based combat): the explicit opt-in stage on owner-seed exploration. The real
# turn, the refused off-turn clicks (before and during the live approach) that mutate nothing, the one on-turn interaction
# from inside the native range, the rider's exactly-once Standard spending with no refund and no mount Standard spend, no
# off-turn mount movement, no stall classification, the encounter's end, the released leases and the exact door restoration.
function Get-KmcChunk7TurnBasedRejectionFields {@('label','riderId','mountId','accepted','doorCommandCreated','secondPairDoorInteraction','feedback','rejectionCodes','kmcWrongTurnObserved','retainedClickCountBefore','retainedClickCountAfter','commandsBefore','commandsAfter','turnUnchanged','firstPairLiveBefore','firstPairCommandIntact')}
function Assert-KmcChunk7TurnBasedRejection($Row,[string]$Label,[string]$SecondRider,[string]$SecondMount,[bool]$LiveBefore){
    Assert-KmcExactProperties $Row (Get-KmcChunk7TurnBasedRejectionFields) ('turn-based off-turn rejection '+$Label)
    Assert-KmcBootstrapInventoryString $Row.label $Label;Assert-KmcBootstrapInventoryString $Row.riderId $SecondRider;Assert-KmcBootstrapInventoryString $Row.mountId $SecondMount
    foreach($key in @('doorCommandCreated','secondPairDoorInteraction')){Assert-KmcChunk7Boolean $Row.$key $false}
    foreach($key in @('turnUnchanged','firstPairCommandIntact')){Assert-KmcChunk7Boolean $Row.$key $true}
    Assert-KmcChunk7Boolean $Row.firstPairLiveBefore $LiveBefore
    if($Row.accepted-isnot[bool]-or$Row.kmcWrongTurnObserved-isnot[bool]){throw 'Turn-based rejection flags must be native booleans.'}
    if($Row.rejectionCodes-isnot[Array]){throw 'Turn-based rejection codes must be recorded.'}
    if(($Row.commandsBefore|ConvertTo-Json -Depth 8 -Compress)-cne($Row.commandsAfter|ConvertTo-Json -Depth 8 -Compress)){throw 'The refused off-turn click changed the second pair''s native command slots.'}
    Assert-KmcOrdinaryInteger $Row.retainedClickCountBefore 0 100000;Assert-KmcOrdinaryInteger $Row.retainedClickCountAfter 0 100000
    if($Row.retainedClickCountAfter-ne$Row.retainedClickCountBefore){throw 'The refused off-turn click was counted as a retained same-door click.'}
}
function Assert-KmcChunk7TurnBasedDoorEvidence($Request,$Turn,$Door,$Owners,[string[]]$Riders,[string[]]$Mounts){
    $t=$Turn;if($null-eq$t){throw 'Turn-based door evidence is missing.'}
    Assert-KmcExactProperties $t @('contract','scope','turns','endedTurns','setupInteractions','fixture','setup','control','encounter','riderTurn','rejectionBeforeClick','interaction','rejectionDuringApproach','encounterEnd','servicesReleased','afterCombat','restoration','completed') 'turn-based door control'
    # v3 (review 2026-10-31): the shared door selection required combat usability before any preparation and the outcome
    # carries the supervision clock's operational seconds (Assert-KmcChunk7DoorSettlement bounds them); otherwise v2.
    if($t.contract-cnotin@('owner-seed-turn-based-door-control-v2','owner-seed-turn-based-door-control-v3')){throw 'Turn-based door control contract differs.'}
    Assert-KmcChunk7Boolean $t.completed $true
    if($t.contract-ceq'owner-seed-turn-based-door-control-v3'){$v3o=$t.interaction.outcome
        if($null-eq$v3o.PSObject.Properties['ApproachElapsedOperationalSeconds']-or$null-eq$v3o.PSObject.Properties['OccupancySettleOperationalSeconds']){throw 'A v3 turn-based door outcome lacks its operational seconds.'}
        # The supervision clock advanced inside the acting turn: a delegated approach that started has positive, bounded operational seconds.
        foreach($field in @('ApproachElapsedOperationalSeconds','OccupancySettleOperationalSeconds')){if(-not(Test-KmcChunk7FiniteNumber $v3o.$field)-or$v3o.$field-lt0-or$v3o.$field-gt60){throw ('Turn-based door outcome '+$field+' is invalid.')}}
        if($v3o.DelegatedMoveStartCount-ge1-and$v3o.ApproachElapsedOperationalSeconds-le0){throw 'Turn-based door approach recorded no operational seconds although a delegated move started.'}}
    # v2 scope labels: the off-turn click proves cross-pair isolation (not the same controller's pending-recovery preservation,
    # which stays offline coverage); zero stalls on a continuously acting turn is not an exhaustion/off-turn wait exercise.
    $sc=$t.scope;Assert-KmcExactProperties $sc @('offTurnRejection','stallClassification','movementCost') 'turn-based door scope labels'
    if($sc.offTurnRejection-isnot[string]-or$sc.offTurnRejection-cnotlike'cross-pair-isolation:*not native proof*'-or$sc.stallClassification-isnot[string]-or$sc.stallClassification-cnotlike'*not a native exercise*'){throw 'Turn-based door scope labels overstate the off-turn or stall evidence.'}
    Assert-KmcBootstrapInventoryString $sc.movementCost 'paired-approach-charged-to-own-mount-move/v1'
    $first=$Riders[0];$second=$Riders[1];$firstMount=$Mounts[0];$secondMount=$Mounts[1]
    $f=$t.fixture;Assert-KmcExactProperties $f @('name','object','position','approachPoint','radius','originalOpen','originalEnabled','originalCut','cutMode','notInCombat','disableOnOpen','restrictionCount','originalAlreadyUnlocked') 'turn-based door fixture'
    Assert-KmcBootstrapInventoryString $f.name $Door.fixture.name
    if($f.cutMode-cne$Door.fixture.cutMode-or$f.originalOpen-ne$Door.fixture.originalOpen-or$f.originalCut-ne$Door.fixture.originalCut){throw 'The turn-based stage captured a different door state than the real-time probe released.'}
    Assert-KmcChunk7Boolean $f.notInCombat $false;Assert-KmcOrdinaryInteger $f.restrictionCount 0 0
    if(-not(Test-KmcChunk7FiniteNumber $f.radius)-or$f.radius-le0){throw 'Turn-based door radius is invalid.'}
    Assert-KmcOrdinaryInteger $t.setupInteractions 0 3
    # The stage's native geometry setup (recorded; not applicable when the pair already stood at the standoff).
    if($t.setup.applicable-isnot[bool]){throw 'Turn-based setup applicability must be a native boolean.'}
    if($t.setup.applicable){Assert-KmcChunk7WorldInt32 $t.setup.command;if($t.setup.result-cne'Success'){throw 'The turn-based setup move did not complete.'}}
    if(-not(Test-KmcChunk7FiniteNumber $t.setup.distanceToApproachPointBeforeEncounter)-or$t.setup.distanceToApproachPointBeforeEncounter-le$f.radius+0.5){throw 'The first pair did not start the turn-based approach outside the door''s immediate range.'}
    $e=$t.encounter
    Assert-KmcBootstrapInventoryString $e.contract 'owner-seed-turn-based-door-encounter-v1';Assert-KmcBootstrapInventoryString $e.basis 'owner-seed-disposable-copy-turn-based-door'
    Assert-KmcBootstrapInventoryString $e.seedSha256 $Request.ownedPairs.seedSha256;Assert-KmcChunk7Boolean $e.turnBasedAtStart $true
    Assert-KmcOrdinaryGuid $e.targetId;Assert-KmcOrdinaryInteger $e.roundAtStart 1 1000
    $order=@($e.nativeInitialOrder);if($order.Count-lt3-or$order-cnotcontains$first-or$order-cnotcontains$second-or$order-cnotcontains$e.targetId){throw 'Native initiative order lacks both riders and the target.'}
    if([Array]::IndexOf($order,$first)-ge[Array]::IndexOf($order,$second)){throw 'The rider-first arrangement did not place the first rider before the second.'}
    if(@($e.initiativeInputs).Count-ne4){throw 'Initiative inputs must name the four pair actors.'}
    # Every observed native roster turn belongs to a rider, the target or an unmounted member; never a mounted partner.
    $turns=@($t.turns);if($turns.Count-lt1){throw 'No native turn was observed.'}
    foreach($row in $turns){if($row.isMount-ne$false-or$row.actorId-cin@($firstMount,$secondMount)){throw 'A mounted partner received an independent native roster turn.'}}
    if(@($turns|Where-Object actorId -CEQ $first).Count-lt1){throw 'The first rider never received a native turn.'}
    foreach($row in @($t.endedTurns)){Assert-KmcBootstrapInventoryString $row.api 'Game.PauseBind (native End Turn binding)';if($row.actorId-cin@($firstMount,$secondMount)){throw 'A mount turn was ended.'}}
    # The rider's acting turn: recorded before any input.
    $rt=$t.riderTurn;Assert-KmcExactProperties $rt @('round','turn','status','acting','timeMoved','remainingTime','cooldowns','mountPosition','distanceToApproachPoint','surprised') 'turn-based rider turn'
    if($rt.status-cnotin@('Preparing','Acting')-and$rt.acting-ne$true){throw 'The rider turn was not ready for input.'}
    if(-not(Test-KmcChunk7FiniteNumber $rt.distanceToApproachPoint)-or$rt.distanceToApproachPoint-le$f.radius+0.5){throw 'The rider turn began inside the door''s immediate range.'}
    # Off-turn input before the click and during the live approach: refused, nothing mutated, the live operation intact.
    Assert-KmcChunk7TurnBasedRejection $t.rejectionBeforeClick 'before-first-pair-click' $second $secondMount $false
    Assert-KmcChunk7TurnBasedRejection $t.rejectionDuringApproach 'during-first-pair-approach' $second $secondMount $true
    # The one on-turn interaction.
    $i=$t.interaction
    Assert-KmcExactProperties $i @('riderId','mountId','ownerId','generation','accepted','commandObject','executorId','round','turn','distanceBefore','radius','cooldownsAtClick','timeMovedAtClick','selectionAfterInput','feedback','rejectionCodes',
        'cooldownsAtActionEnd','timeMovedAtActionEnd','remainingTimeAtActionEnd','mountPositionAtActionEnd','distanceMovedAtActionEnd','movementObservationAtActionEnd','allocation','commandResult','commandActed',
        'outcome','terminal','distanceMoved','doorOpenAfter','cooldownsAfter','timeMovedAfter','maximumTimeMovedWhileLive','livePolls','movingPolls','offTurnMovingPolls','startedWhileMoving','unstartedWhileMoving','turnChangedWhileLive','turnStillRiderAfter','roundAfter','mountPositionAfter','mountInsideNavmesh',
        'settlementPolls','settlementMovingPolls','settlementOffTurnMovingPolls','turnChangedDuringSettlement','cooldownDeltas','movementCost') 'turn-based door interaction'
    Assert-KmcBootstrapInventoryString $i.riderId $first;Assert-KmcBootstrapInventoryString $i.mountId $firstMount;Assert-KmcBootstrapInventoryString $i.executorId $first;Assert-KmcBootstrapInventoryString $i.selectionAfterInput $second
    $owner=@($Owners|Where-Object{Test-KmcChunk7OwnerActorMatch $_.riderId $first});if($owner.Count-ne1-or[string]$i.ownerId-cne[string]$owner[0].ownerId-or$i.generation-ne$owner[0].generation){throw 'The turn-based interaction belongs to another owner or generation.'}
    Assert-KmcChunk7WorldInt32 $i.commandObject;Assert-KmcChunk7Boolean $i.accepted $true;Assert-KmcChunk7Boolean $i.terminal $true;Assert-KmcChunk7Boolean $i.doorOpenAfter $true
    foreach($key in @('startedWhileMoving','turnChangedWhileLive')){Assert-KmcChunk7Boolean $i.$key $false}
    foreach($key in @('unstartedWhileMoving','mountInsideNavmesh')){Assert-KmcChunk7Boolean $i.$key $true}
    Assert-KmcOrdinaryInteger $i.movingPolls 1 100000;Assert-KmcOrdinaryInteger $i.offTurnMovingPolls 0 0;Assert-KmcOrdinaryInteger $i.livePolls 1 1000000
    if($i.round-ne$rt.round){throw 'The interaction was issued on another round than the observed rider turn.'}
    if(-not(Test-KmcChunk7FiniteNumber $i.distanceBefore)-or$i.distanceBefore-le$i.radius+0.5-or-not(Test-KmcChunk7FiniteNumber $i.distanceMoved)-or$i.distanceMoved-lt0.5){throw 'The turn-based approach did not start outside the native range or did not move the mount.'}
    $oc=$i.outcome;if($null-eq$oc){throw 'Turn-based interaction outcome is missing.'}
    if($oc.Result-cne'Success'-or$oc.RiderId-cne$first-or$oc.MountId-cne$firstMount-or$oc.InteractionCount-ne1-or$oc.DelegatedMoveStartCount-lt1-or$oc.DoorStateChanged-ne$true-or$oc.MountMoveSlotRestored-ne$true){throw 'The turn-based door command did not prove one approach, one interaction and restored mount movement.'}
    if(-not(Test-KmcChunk7FiniteNumber $oc.DistanceAtInteraction)-or$oc.DistanceAtInteraction-gt$i.radius+0.01-or$oc.LineOfSightAtInteraction-ne$true){throw 'The turn-based interaction was not delivered from inside the native radius with line of sight.'}
    Assert-KmcOrdinaryInteger $oc.ApproachesWithoutProgress 0 0
    Assert-KmcChunk7DoorSettlement $oc
    # Native spending and movement cost (v2, review correction B). Raw cooldown rows at three boundaries: the click, the
    # action end (first poll after the native action finished) and after the owned settlement. The recorded deltas must be
    # the raw differences; the installed paired contract decides: a rider's turn-based approach is native movement charged
    # once to its own mount's Move budget (equal to the pair's allocation movement time, never more than the game time that
    # passed, inside the six-second budget), the rider keeps its own Move, the Standard is spent exactly once (+6) at the
    # interaction, nothing is refunded afterwards beyond native decay, and no unowned movement follows the action.
    $ck=$i.cooldownsAtClick;$ac=$i.cooldownsAtActionEnd;$af=$i.cooldownsAfter
    foreach($row in @($ck,$ac,$af)){if($null-eq$row){throw 'A turn-based cooldown boundary row is missing.'}
        foreach($role in @('rider','mount','secondRider','secondMount')){foreach($key in @('standard','move','swift')){if(-not(Test-KmcChunk7FiniteNumber $row.$role.$key)-or$row.$role.$key-lt0){throw ('Turn-based cooldown row '+$role+'.'+$key+' is not finite and nonnegative.')}}
            if(-not(Test-KmcExactJsonInteger $row.$role.gameTicks)-or$row.$role.gameTicks-lt0){throw 'Turn-based cooldown row lacks its game ticks.'}}}
    $approach=([double]$ac.rider.gameTicks-[double]$ck.rider.gameTicks)/1e7;$settle=([double]$af.rider.gameTicks-[double]$ac.rider.gameTicks)/1e7
    if($approach-lt0-or$settle-lt0){throw 'Turn-based cooldown boundaries are out of order.'}
    if(-not(Test-KmcChunk7FiniteNumber $i.distanceMovedAtActionEnd)-or$i.distanceMovedAtActionEnd-lt0.5){throw 'The mount had not travelled by the action end.'}
    foreach($key in @('timeMovedAtActionEnd','remainingTimeAtActionEnd')){if($null-ne$i.$key-and(-not(Test-KmcChunk7FiniteNumber $i.$key)-or$i.$key-lt0)){throw ('Turn-based '+$key+' is invalid.')}}
    foreach($axis in @('x','y','z')){if(-not(Test-KmcChunk7FiniteNumber $i.mountPositionAtActionEnd.$axis)){throw 'Turn-based action-end mount position is nonfinite.'}}
    if($i.commandResult-cne'Success'){throw 'The routed door command did not end in native Success.'};Assert-KmcChunk7Boolean $i.commandActed $true
    $mc=$i.movementCost;Assert-KmcExactProperties $mc @('contract','approachGameSeconds','settlementGameSeconds','mountMoveCharge','riderMoveDelta','riderStandardDelta','mountStandardDelta','mountMoveAfterSettlement','riderStandardAfterSettlement','allocationTime','allocationStepMetres','recoveryAdmitted','decayBound','lawful') 'turn-based movement cost'
    Assert-KmcBootstrapInventoryString $mc.contract 'paired-approach-charged-to-own-mount-move/v1';Assert-KmcChunk7Boolean $mc.lawful $true
    $charge=[double]$ac.mount.move-[double]$ck.mount.move
    $raw=@(@('mountMoveCharge',$charge),@('riderMoveDelta',([double]$ac.rider.move-[double]$ck.rider.move)),@('riderStandardDelta',([double]$ac.rider.standard-[double]$ck.rider.standard)),@('mountStandardDelta',([double]$ac.mount.standard-[double]$ck.mount.standard)),
        @('mountMoveAfterSettlement',([double]$af.mount.move-[double]$ac.mount.move)),@('riderStandardAfterSettlement',([double]$af.rider.standard-[double]$ac.rider.standard)),@('approachGameSeconds',$approach),@('settlementGameSeconds',$settle))
    foreach($pair in $raw){if(-not(Test-KmcChunk7FiniteNumber $mc.($pair[0]))-or[Math]::Abs([double]$mc.($pair[0])-[double]$pair[1])-gt0.0005){throw ('Turn-based movement cost '+$pair[0]+' differs from the raw cooldown rows.')}}
    # The pair's own allocation observation (the installed paired movement accounting) at the action end.
    if($i.movementObservationAtActionEnd-isnot[string]-or$i.movementObservationAtActionEnd-cnotlike'mountMove=*'){throw 'The pair''s own movement allocation observation is missing: the approach was not accounted through the paired movement contract.'}
    $al=$i.allocation;if($null-eq$al){throw 'The pair''s movement allocation observation did not parse.'}
    Assert-KmcExactProperties $al @('mountMoveBefore','mountMoveAfter','mountTime','mountStepMetres','nativePrepared','round','physicalDeltaRequested','physicalDeltaAllowed') 'turn-based movement allocation'
    foreach($field in @('mountMoveBefore','mountMoveAfter','mountTime','mountStepMetres','physicalDeltaRequested','physicalDeltaAllowed')){if(-not(Test-KmcChunk7FiniteNumber $al.$field)-or$al.$field-lt0){throw ('Turn-based movement allocation '+$field+' is invalid.')}}
    if($al.nativePrepared-isnot[bool]){throw 'Turn-based movement allocation preparation flag must be a native boolean.'};Assert-KmcOrdinaryInteger $al.round 1 1000
    if([Math]::Abs([double]$mc.allocationTime-[double]$al.mountTime)-gt0.0005-or[Math]::Abs([double]$mc.allocationStepMetres-[double]$al.mountStepMetres)-gt0.0005){throw 'Turn-based movement cost allocation figures differ from the allocation observation.'}
    # The contract itself, on the raw values.
    if($charge-le0.01){throw 'Free transport: the mount travelled on the rider''s turn but no Move time was charged.'}
    # (Native turn-based GameTime does not advance inside a turn, so the recorded game seconds are data, not a bound.)
    if([double]$ac.mount.move-gt6.001){throw 'The mount''s Move cooldown exceeded one turn''s six-second budget.'}
    if([Math]::Abs([double]$ac.rider.move-[double]$ck.rider.move)-gt0.01-or[double]$ac.rider.move-gt0.01){throw 'Rider transport tax: the carried rider''s own Move was charged for the mount''s approach.'}
    if($charge-gt[double]$al.mountTime+0.05){throw 'Double movement charge: the mount''s Move delta exceeds the allocation''s movement time.'}
    if([double]$al.mountStepMetres-le0-and[Math]::Abs($charge-[double]$al.mountTime)-gt0.05){throw 'The mount''s Move charge differs from the allocation''s movement time.'}
    if([Math]::Abs([double]$al.mountMoveAfter-[double]$ac.mount.move)-gt0.05){throw 'The allocation ledger differs from the mount''s native Move cooldown at the action end.'}
    $standard=[double]$ac.rider.standard-[double]$ck.rider.standard
    if($standard-lt5.5-or$standard-gt6.05){throw 'The rider''s Standard action was not spent exactly once (+6) at the turn-based door interaction.'}
    if([Math]::Abs([double]$ac.mount.standard-[double]$ck.mount.standard)-gt0.01){throw 'The mount spent a Standard action on the rider''s door interaction.'}
    $decay=1.1*$settle+0.01;$afterMove=[double]$af.mount.move-[double]$ac.mount.move;$afterStandard=[double]$af.rider.standard-[double]$ac.rider.standard
    if($afterMove-lt-$decay-or$afterStandard-lt-$decay){throw 'A native cooldown was refunded after the turn-based door action beyond native decay.'}
    if($afterStandard-gt0.01){throw 'The rider''s Standard was spent again after the door action.'}
    $recovered=$oc.RecoveryDisposition-ceq'Admitted';if($mc.recoveryAdmitted-ne$recovered){throw 'Turn-based movement cost recovery flag differs from the settlement outcome.'}
    Assert-KmcOrdinaryInteger $i.settlementPolls 0 1000000;Assert-KmcOrdinaryInteger $i.settlementMovingPolls 0 1000000;Assert-KmcOrdinaryInteger $i.settlementOffTurnMovingPolls 0 0;Assert-KmcChunk7Boolean $i.turnChangedDuringSettlement $false
    if($recovered){if([double]$af.mount.move-gt6.001-or$i.settlementMovingPolls-lt1){throw 'The owned recovery move left the budget or was never observed moving.'}}
    elseif($afterMove-gt0.01-or$i.settlementMovingPolls-ne0){throw 'The mount moved or was charged after the door action without an admitted owned recovery.'}
    $dl=$i.cooldownDeltas
    foreach($role in @('rider','mount','secondRider','secondMount')){foreach($key in @('standard','move','swift')){if(-not(Test-KmcChunk7FiniteNumber $dl.$role.$key)-or[Math]::Abs([double]$dl.$role.$key-([double]$af.$role.$key-[double]$ck.$role.$key))-gt0.0005){throw ('Turn-based cooldown delta differs from its raw rows: '+$role+'.'+$key)}}}
    foreach($pair in @(@('rider','swift'),@('mount','swift'),@('mount','standard'))){if([Math]::Abs([double]$dl.($pair[0]).($pair[1]))-gt0.01){throw ('Turn-based door interaction changed an unrelated first-pair cooldown: '+$pair[0]+'.'+$pair[1])}}
    if($dl.rider.standard-le0.5-or$dl.rider.standard-gt6.05){throw 'The rider''s Standard action was not spent exactly once by the turn-based door interaction.'}
    foreach($role in @('secondRider','secondMount')){foreach($key in @('standard','move','swift')){if([Math]::Abs([double]$dl.$role.$key)-gt0.01){throw 'The second pair''s native cooldowns changed during the first pair''s door interaction.'}}}
    # The native control (an unmounted member's own stock click on its earlier turn) when the initiative order offered one.
    $k=$t.control;if($k.applicable-isnot[bool]){throw 'Turn-based control applicability must be a native boolean.'}
    if($k.applicable){Assert-KmcChunk7Boolean $k.routedByKmc $false;Assert-KmcChunk7WorldInt32 $k.commandObject;if($k.commandType-cne'Kingmaker.UnitLogic.Commands.UnitInteractWithObject'-or$k.result-cne'Success'-or$k.doorOpenAfter-ne$true){throw 'The native control did not open the door through its stock interaction.'}
        if($k.actorId-cin@($first,$second,$firstMount,$secondMount)){throw 'The native control must be an unmounted member outside both pairs.'}}
    elseif($k.reason-isnot[string]-or[string]::IsNullOrWhiteSpace($k.reason)){throw 'A not-applicable native control must say why.'}
    # The encounter ended by removing the target; every lease released; the pairs mounted, idle and without retained commands.
    Assert-KmcChunk7Boolean $t.encounterEnd.targetRemoved $true
    $sr=$t.servicesReleased;foreach($key in @('initiativeRestored','aiLeaseRestored','targetServiceDisposed','realTimeModeReselected')){Assert-KmcChunk7Boolean $sr.$key $true}
    $ac=$t.afterCombat;foreach($key in @('turnBased','playerInCombat','controllerInitialized')){Assert-KmcChunk7Boolean $ac.$key $false};Assert-KmcChunk7Boolean $ac.actorsIdle $true
    $pairs=@($ac.pairs);if($pairs.Count-ne2){throw 'After-combat pair rows missing.'}
    foreach($p in $pairs){foreach($key in @('retainedPairCommands','activeDoorInteraction','pendingSettlement')){Assert-KmcChunk7Boolean $p.$key $false};Assert-KmcChunk7Boolean $p.mounted $true}
    $rs=$t.restoration;Assert-KmcExactProperties $rs @('exact','commandsSettled','open','enabled','cutEnabled','observedAlreadyUnlockedBeforeRestore','alreadyUnlocked') 'turn-based door restoration'
    Assert-KmcChunk7Boolean $rs.exact $true;Assert-KmcChunk7Boolean $rs.commandsSettled $true
    if($rs.open-ne$f.originalOpen-or$rs.enabled-ne$f.originalEnabled-or$rs.cutEnabled-ne$f.originalCut-or$rs.alreadyUnlocked-ne$f.originalAlreadyUnlocked){throw 'The turn-based door lease did not restore the door exactly.'}
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
    # Sprint 2 (MOVE-01 on the owner's surface): the pre-owned single-pair speed mode has its own evidence shape.
    if((Test-KmcChunk7OwnedPairsScenario $Request.scenario)-and(Test-KmcChunk7OwnedPairsOwnerSurfaceSpeed $Request)){Assert-KmcChunk7OwnerSurfaceSpeedEvidence $Request $Artifact.observations;return}
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
    # Sprint 2 (DOOR-01 turn-based): the explicit opt-in stage follows the real-time door probe with both pairs mounted.
    $turnBasedSelected=$seed-and(Test-KmcChunk7OwnedPairsTurnBasedDoor $Request)
    if($turnBasedSelected){
        if(-not$doorTested){throw 'The turn-based door stage requires the real-time door probe to have run on the same door.'}
        Assert-KmcChunk7TurnBasedDoorEvidence $Request $o.turnBasedDoor $o.door $owners @($first,$second) @($mammoth,$horse)
        $tbAfter=@($o.snapshots|Where-Object label -CEQ 'turn-based-door-complete')
        if($tbAfter.Count-ne1-or@($tbAfter[0].owners|Where-Object state -CEQ 'Active').Count-ne2){throw 'Turn-based door completion window missing or a pair lost its owner.'}
        foreach($owner in $owners){$same=@($tbAfter[0].owners|Where-Object ownerId -CEQ $owner.ownerId);if($same.Count-ne1-or$same[0].generation-ne$owner.generation-or$same[0].riderId-cne$owner.riderId-or$same[0].mountId-cne$owner.mountId-or$same[0].state-cne'Active'){throw 'The turn-based door stage changed captured ownership.'}}
    }elseif($null-ne$o.PSObject.Properties['turnBasedDoor']-or@($o.snapshots|Where-Object label -CEQ 'turn-based-door-complete').Count-ne0){throw 'Turn-based door evidence belongs only to its explicit selection.'}
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
    # v2 route moves (F5 hardening): the producer writes observations.routeGoalContract at fixture setup and records
    # observations.routeGoalSearches, one per movement. The marker and the searches are required together; evidence with
    # neither (written before the marker existed) keeps its v1 fixed-goal path and verdict.
    $routeMarker=$null-ne$o.PSObject.Properties['routeGoalContract'];$routeSearches=$null-ne$o.PSObject.Properties['routeGoalSearches']
    if($routeMarker-ne$routeSearches){throw 'World route goal marker and route goal searches must be recorded together.'}
    if($routeMarker){
        if($o.routeGoalContract-cne'world-route-goal-clear-of-units/v1'){throw 'World route goal contract differs.'}
        Assert-KmcChunk7WorldRouteGoals $o.routeGoalSearches @($o.movements) $moveSpecs $actors
    }
    Assert-KmcChunk7Sprint2Legs $o
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
    # v3 door (Sprint 2): the long-approach setup move and the click-while-moving lateral move are issued native commands
    # of the probe; the speed legs' ground moves are issued by the scenario. All are retained for the removal admission.
    if($doorTested-and$o.door.contract-cin@('two-pair-native-door-click-selection-change-v3','two-pair-native-door-click-selection-change-v4','two-pair-native-door-click-selection-change-v5')){
        if($o.door.longApproach.applicable){$doorCommands+=@([string]$o.door.longApproach.setupCommand)}
        if($o.door.clickWhileMoving.applicable){$doorCommands+=@([string]$o.door.clickWhileMoving.moveCommand)}
    }
    # v4: the unmounted control's native interaction, the endpoint neighbor's move and the Stop control's door command.
    if($doorTested-and$o.door.contract-cin@('two-pair-native-door-click-selection-change-v4','two-pair-native-door-click-selection-change-v5')){
        if($o.door.unmountedControl.applicable){$doorCommands+=@([string]$o.door.unmountedControl.commandObject)}
        if($o.door.endpointNeighbor.applicable){$doorCommands+=@([string]$o.door.endpointNeighbor.moveCommand)}
        if($null-ne$o.door.stopControl.PSObject.Properties['commandObject']){$doorCommands+=@([string]$o.door.stopControl.commandObject)}
        if($null-ne$o.door.PSObject.Properties['layoutRestore']){foreach($m in @($o.door.layoutRestore.moves)){if($m.applicable){$doorCommands+=@([string]$m.command)}}}
    }
    $legCommands=@();if($null-ne$o.PSObject.Properties['speedLegs']){foreach($leg in @($o.speedLegs)){if($leg.applicable){foreach($mv in @($leg.movers)){$legCommands+=@([string]$mv.commandObject)}}}}
    # The turn-based door stage's issued native commands: its setup move, the native control's interaction and the rider's door command.
    if($turnBasedSelected){$t=$o.turnBasedDoor;if($t.setup.applicable){$legCommands+=@([string]$t.setup.command)};if($t.control.applicable){$legCommands+=@([string]$t.control.commandObject)};$legCommands+=@([string]$t.interaction.commandObject)}
    Assert-KmcChunk7ExactIds $admission.commands @(@($issued|ForEach-Object{[string]$_.commandObject})+$doorCommands+$legCommands) 'commandObject'
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
        if((Test-KmcChunk7SixPairStress $Request)-or(Test-KmcChunk7OwnedPairsSmallThirdHorse $Request)-or(Test-KmcChunk7OwnedPairsSmallThirdHorseSave $Request)-or(Test-KmcChunk7OwnedPairsFourthHorse $Request)-or(Test-KmcChunk7OwnedPairsOwnerSurfaceSpeed $Request)){if($shots.Count-ne0){throw 'The selected fixture case cannot claim a two-pair mounted screenshot.'}}
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
function Assert-KmcBaseGameNativeInventory($Request,$Artifact,$Dependencies,[AllowEmptyCollection()][object[]]$RetainedDependencies=@(),[switch]$PassThru) {
    Assert-KmcChunk7ArtifactEnvelope $Request $Artifact 'runtime-loader-inventory'
    if($Artifact.status-cne'PASS'-or$Artifact.observations.capturedInNativeProcess-ne$true-or
        -not(Test-KmcExactJsonInteger $Artifact.observations.processId)-or$Artifact.observations.processId-le0){throw 'Live loader inventory is incomplete.'}
    Assert-KmcChunk7Boolean $Artifact.observations.capturedInNativeProcess $true
    $mods=@($Artifact.observations.modEntries)
    foreach($entry in $mods){Assert-KmcChunk7Boolean $entry.active $true;Assert-KmcChunk7Boolean $entry.enabled $true}
    # Sprint 1 UPG-01: explicitly retained dependencies are the only other admitted UMM entries, each active and enabled.
    $retained=@($RetainedDependencies|Where-Object{$null-ne$_})
    $expectedIds=@(@('KingmakerMountedCombat')+@($retained|ForEach-Object{[string]$_.id})|Sort-Object)
    $actualIds=@($mods|ForEach-Object{[string]$_.id}|Sort-Object)
    if(($actualIds-join '|')-cne($expectedIds-join '|')-or@($mods|Where-Object{$_.active-ne$true-or$_.enabled-ne$true}).Count-ne0){throw 'Base-only run loaded another UMM entry, missed a retained dependency, or an entry was inactive/disabled.'}
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
            # Sprint 1 UPG-01: a retained dependency's own assembly, loaded from its staged folder or the loader's cache copy there.
            $dependency=@($retained|Where-Object{$directory.Equals([IO.Path]::GetFullPath((Join-Path $Dependencies.modsRoot ([string]$_.id))).TrimEnd('\'),[StringComparison]::OrdinalIgnoreCase)})
            if($dependency.Count-ne1){throw 'File-backed assembly is outside the exact installed Managed/UMM directories, the staged KMC path and the declared retained dependency folders.'}
            $leaf=[IO.Path]::GetFileName($location);$dllName=[string]$dependency[0].dllName
            $cacheMatch=[regex]::Match($leaf,'^'+[regex]::Escape($dllName)+'\.(0|[1-9][0-9]{0,4})\.cache$')
            if(($leaf-cne$dllName-and-not$cacheMatch.Success)-or[string]$assembly.sha256-cne[string]$dependency[0].dllSha256){throw 'A retained dependency assembly differs from its declared staged bytes.'}
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
